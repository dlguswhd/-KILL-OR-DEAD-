// [KILL OR DEAD] Combat Core
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KillOrDead.Combat
{
    /// <summary>
    /// 표면 재질 -> WarFX 임팩트 프리팹 매핑 테이블.
    /// Assets/_Game/Data/ 아래에 에셋 하나 만들어두고 모든 무기가 공유한다.
    /// 생성: Project 우클릭 > Create > KILL OR DEAD > Impact Effect Library
    ///
    /// WarFX의 "+ Bullet Hole/Unlit" 프리팹을 쓰면 파편 튐과 탄흔이 한 프리팹에 다 들어있다.
    /// (Lit 버전은 built-in 서피스 셰이더라 URP에서 분홍색이 된다. Unlit을 쓸 것)
    /// </summary>
    [CreateAssetMenu(fileName = "ImpactEffectLibrary", menuName = "KILL OR DEAD/Impact Effect Library")]
    public class ImpactEffectLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public SurfaceType surface;

            [Tooltip("파편/스파크 + 탄흔. WarFX의 '+ Bullet Hole/Unlit' 프리팹 권장")]
            public GameObject effectPrefab;

            [Tooltip("탄흔을 따로 쓰고 싶을 때만. 위 프리팹에 이미 있으면 비워둔다.")]
            public GameObject decalPrefab;

            public AudioClip[] impactSounds;
        }

        [SerializeField] private Entry[] entries = new Entry[0];

        [Header("수명")]
        [Tooltip("이펙트를 강제로 지우기까지의 시간(초).\n" +
                 "WarFX 프리팹은 스스로 사라지므로 0을 권장한다.")]
        [SerializeField] private float effectLifetime = 0f;

        [Tooltip("따로 지정한 탄흔 프리팹의 수명(초). 0이면 스스로 사라질 때까지 둔다.")]
        [SerializeField] private float decalLifetime = 0f;

        [Header("개수 제한")]
        [Tooltip("화면에 동시에 존재할 수 있는 임팩트 최대 개수.\n" +
                 "넘으면 가장 오래된 것부터 지운다. 연사 시 프레임 드랍을 막는다.")]
        [Min(8)] [SerializeField] private int maxLiveEffects = 60;

        [Header("기타")]
        [Tooltip("매핑을 못 찾았을 때 쓸 재질")]
        [SerializeField] private SurfaceType fallbackSurface = SurfaceType.Concrete;

        [Tooltip("임팩트 사운드 볼륨")]
        [Range(0f, 1f)] [SerializeField] private float impactVolume = 0.8f;

        // 살아있는 이펙트 추적. 플레이 세션 사이에 남지 않도록 초기화한다.
        private static readonly Queue<GameObject> LiveEffects = new Queue<GameObject>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => LiveEffects.Clear();

        /// <summary>
        /// 피격 지점에 임팩트 이펙트와 탄흔을 만든다.
        /// WarFX 규약대로 표면 법선 방향을 바라보게 생성한다.
        /// </summary>
        public void Spawn(SurfaceType surface, Vector3 point, Vector3 normal, Transform parent = null)
        {
            if (!TryGetEntry(surface, out var entry))
                if (!TryGetEntry(fallbackSurface, out entry))
                    return;

            var rotation = normal.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(normal)
                : Quaternion.identity;

            if (entry.effectPrefab != null)
            {
                // 표면에서 살짝 띄워야 Z-파이팅이 안 생긴다.
                var fx = Instantiate(entry.effectPrefab, point + normal * 0.01f, rotation);
                if (parent != null) fx.transform.SetParent(parent, true);
                if (effectLifetime > 0f) Destroy(fx, effectLifetime);
                Register(fx);
            }

            if (entry.decalPrefab != null)
            {
                var decal = Instantiate(entry.decalPrefab, point + normal * 0.005f, rotation);
                if (parent != null) decal.transform.SetParent(parent, true);
                if (decalLifetime > 0f) Destroy(decal, decalLifetime);
                Register(decal);
            }

            if (entry.impactSounds != null && entry.impactSounds.Length > 0)
            {
                var clip = entry.impactSounds[UnityEngine.Random.Range(0, entry.impactSounds.Length)];
                if (clip != null) AudioSource.PlayClipAtPoint(clip, point, impactVolume);
            }
        }

        /// <summary> 개수 제한을 넘으면 가장 오래된 이펙트부터 지운다. </summary>
        private void Register(GameObject effect)
        {
            LiveEffects.Enqueue(effect);

            while (LiveEffects.Count > maxLiveEffects)
            {
                var oldest = LiveEffects.Dequeue();
                if (oldest != null) Destroy(oldest);
            }
        }

        private bool TryGetEntry(SurfaceType surface, out Entry result)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].surface != surface) continue;
                result = entries[i];
                return true;
            }
            result = default;
            return false;
        }
    }
}
