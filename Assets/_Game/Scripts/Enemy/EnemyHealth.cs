// [KILL OR DEAD] Combat
using System;
using KillOrDead.Combat;
using UnityEngine;

namespace KillOrDead.Enemies
{
    /// <summary>
    /// 기획서 기준 적 체력.
    /// - HP는 부위별로 나누지 않고 하나로 공유한다 (기본 500)
    /// - 대신 맞은 부위에 따라 데미지 배수를 곱한다
    ///   머리 x5.0 / 흉부 x2.0 / 복부 x1.5 / 팔 x0.5 / 다리 x0.5
    /// </summary>
    [AddComponentMenu("KILL OR DEAD/Enemy/Enemy Health")]
    [DisallowMultipleComponent]
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Serializable]
        public struct HitMultipliers
        {
            [Tooltip("머리 - 치명상. 헤드샷 1~2발에 사망하도록 설계")]
            public float head;
            [Tooltip("흉부 - 면적이 넓고 주요 장기가 모여 있음")]
            public float chest;
            [Tooltip("복부 - 흉부보다 장기 중요도가 낮음")]
            public float abdomen;
            [Tooltip("팔 - 생명에 직결되지 않음")]
            public float arm;
            [Tooltip("다리 - 생명에 직결되지 않음")]
            public float leg;

            public static HitMultipliers Default => new HitMultipliers
            {
                head = 5.0f,
                chest = 2.0f,
                abdomen = 1.5f,
                arm = 0.5f,
                leg = 0.5f,
            };
        }

        [Header("체력")]
        [SerializeField] private float maxHealth = 500f;

        [Header("부위별 데미지 배수")]
        [SerializeField] private HitMultipliers multipliers = HitMultipliers.Default;

        [Tooltip("부위 정보가 없는(히트박스 없이 맞은) 피격에 쓸 배수")]
        [SerializeField] private float unknownPartMultiplier = 1f;

        [Header("디버그")]
        [SerializeField] private bool logHits = false;

        private float _health;
        private bool _dead;

        public float MaxHealth => maxHealth;
        public float Health => _health;
        public float HealthNormalized => maxHealth <= 0f ? 0f : Mathf.Clamp01(_health / maxHealth);
        public bool IsDead => _dead;

        /// <summary> (피격정보, 실제 적용된 데미지, 남은 체력) </summary>
        public event Action<DamageInfo, float, float> OnDamaged;
        /// <summary> (마지막 피격정보) </summary>
        public event Action<DamageInfo> OnDeath;

        private void Awake() => ResetHealth();

        public void ResetHealth()
        {
            _health = maxHealth;
            _dead = false;
        }

        public float GetMultiplier(BodyPartType part)
        {
            switch (part)
            {
                case BodyPartType.Head:     return multipliers.head;
                case BodyPartType.Chest:    return multipliers.chest;
                case BodyPartType.Abdomen:  return multipliers.abdomen;
                case BodyPartType.LeftArm:
                case BodyPartType.RightArm: return multipliers.arm;
                case BodyPartType.LeftLeg:
                case BodyPartType.RightLeg: return multipliers.leg;
                default:                    return unknownPartMultiplier;
            }
        }

        public void ApplyDamage(DamageInfo info)
        {
            if (_dead) return;

            float multiplier = GetMultiplier(info.bodyPart);
            float finalDamage = Mathf.Max(0f, info.baseDamage * multiplier);

            _health = Mathf.Max(0f, _health - finalDamage);

            if (logHits)
                Debug.Log($"[EnemyHealth] {name} {info.bodyPart.ToKorean()} 피격 " +
                          $"{info.baseDamage:F0} x{multiplier:F2} = {finalDamage:F0} / 남은 HP {_health:F0}", this);

            OnDamaged?.Invoke(info, finalDamage, _health);

            if (_health <= 0f) Die(info);
        }

        private void Die(DamageInfo info)
        {
            if (_dead) return;
            _dead = true;
            OnDeath?.Invoke(info);
        }

        /// <summary> 치트/디버그용 즉사 </summary>
        [ContextMenu("즉시 처치")]
        public void Kill() => ApplyDamage(new DamageInfo
        {
            baseDamage = maxHealth * 1000f,
            bodyPart = BodyPartType.Chest,
            hitPoint = transform.position,
            hitNormal = Vector3.up,
            direction = transform.forward,
        });
    }
}
