// [KILL OR DEAD] Weapons
using UnityEngine;

namespace KillOrDead.Weapons
{
    /// <summary>
    /// 무기 한 정의 탄도/데미지 수치. TSP의 TacticalWeaponSettings(애니메이션/사운드)와
    /// 역할을 나눠 갖는다. 여기는 "얼마나 아프고 얼마나 멀리 나가는가"만 담당.
    /// 생성: Project 우클릭 > Create > KILL OR DEAD > Weapon Damage Profile
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponDamageProfile", menuName = "KILL OR DEAD/Weapon Damage Profile")]
    public class WeaponDamageProfile : ScriptableObject
    {
        [Header("식별")]
        public string weaponName = "New Weapon";

        [Header("데미지")]
        [Tooltip("부위 배수가 곱해지기 전의 기본 데미지.\n" +
                 "적 HP 500 기준: 100이면 헤드샷(x5) 1발, 60이면 헤드샷 2발.")]
        [Min(0f)] public float baseDamage = 60f;

        [Tooltip("샷건처럼 한 번에 여러 발이 나가는 경우. 소총/권총은 1")]
        [Min(1)] public int pelletsPerShot = 1;

        [Header("사거리")]
        [Tooltip("이 거리를 넘으면 판정 자체가 없다 (m)")]
        [Min(1f)] public float maxDistance = 300f;

        [Tooltip("가로축 = 거리 / maxDistance (0~1), 세로축 = 데미지 배율")]
        public AnimationCurve damageFalloff = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.35f, 1f),
            new Keyframe(1f, 0.5f));

        [Header("탄퍼짐 (도)")]
        [Tooltip("조준하지 않았을 때의 원뿔 반각")]
        [Min(0f)] public float hipSpreadDegrees = 2.5f;
        [Tooltip("조준했을 때의 원뿔 반각")]
        [Min(0f)] public float aimSpreadDegrees = 0.2f;
        [Tooltip("연속 사격 시 퍼짐이 늘어나는 양(발당, 도). 최대 hipSpread의 3배까지")]
        [Min(0f)] public float spreadGrowthPerShot = 0.35f;
        [Tooltip("사격을 멈췄을 때 퍼짐이 회복되는 속도(초당, 도)")]
        [Min(0f)] public float spreadRecoveryPerSecond = 6f;

        [Header("물리")]
        [Tooltip("맞은 리지드바디를 밀어내는 힘")]
        [Min(0f)] public float impactForce = 20f;

        /// <summary> 거리 감쇠를 적용한 실제 데미지 </summary>
        public float GetDamageAtDistance(float distance)
        {
            float t = maxDistance <= 0f ? 0f : Mathf.Clamp01(distance / maxDistance);
            return baseDamage * Mathf.Max(0f, damageFalloff.Evaluate(t));
        }
    }
}
