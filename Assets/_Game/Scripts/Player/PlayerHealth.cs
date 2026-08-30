// [KILL OR DEAD] Combat
using System;
using System.Collections.Generic;
using KillOrDead.Combat;
using UnityEngine;

namespace KillOrDead.PlayerSystems
{
    /// <summary>
    /// 기획서 기준 플레이어 체력. 적과 달리 부위별로 HP가 따로 논다.
    ///
    ///   머리 30    : 소진 시 사망
    ///   흉부 100   : 50% 소진 시 이동속도 감소 / 소진 시 사망
    ///   복부 100   : 50% 소진 시 이동속도 감소 / 소진 시 사망
    ///   팔 150 x2  : 소진 시 장전속도 감소 / 상호작용 속도 감소 / 반동 증가
    ///   다리 150 x2: 소진 시 이속 감소 / 점프 높이 감소 / 달리기 불가
    ///
    /// 이미 HP가 0인 부위에 추가 피해가 들어오면, 데미지의 일정 비율이
    /// 나머지 부위 전체로 균등 분산된다.
    /// </summary>
    [AddComponentMenu("KILL OR DEAD/Player/Player Health")]
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Serializable]
        public struct PartHealthConfig
        {
            public float head;
            public float chest;
            public float abdomen;
            public float arm;   // 좌/우 각각 이 값
            public float leg;   // 좌/우 각각 이 값

            public static PartHealthConfig Default => new PartHealthConfig
            {
                head = 30f, chest = 100f, abdomen = 100f, arm = 150f, leg = 150f,
            };
        }

        [Serializable]
        public struct DebuffConfig
        {
            [Header("흉부/복부 50% 이하")]
            [Range(0.1f, 1f)] public float woundedTorsoMoveSpeed;

            [Header("다리 소진 (부위 1개당 누적 적용)")]
            [Range(0.1f, 1f)] public float brokenLegMoveSpeed;
            [Range(0.1f, 1f)] public float brokenLegJumpHeight;

            [Header("팔 소진 (부위 1개당 누적 적용)")]
            [Range(0.1f, 1f)] public float brokenArmReloadSpeed;
            [Range(0.1f, 1f)] public float brokenArmInteractSpeed;
            [Min(1f)]         public float brokenArmRecoilScale;

            public static DebuffConfig Default => new DebuffConfig
            {
                woundedTorsoMoveSpeed  = 0.75f,
                brokenLegMoveSpeed     = 0.6f,
                brokenLegJumpHeight    = 0.5f,
                brokenArmReloadSpeed   = 0.6f,
                brokenArmInteractSpeed = 0.6f,
                brokenArmRecoilScale   = 1.5f,
            };
        }

        [Header("부위별 최대 체력")]
        [SerializeField] private PartHealthConfig maxPartHealth = PartHealthConfig.Default;

        [Header("디버프 수치")]
        [SerializeField] private DebuffConfig debuffs = DebuffConfig.Default;

        [Header("데미지 분산")]
        [Tooltip("이미 HP 0인 부위를 또 맞았을 때, 데미지의 몇 %가 나머지 부위로 분산되는가")]
        [Range(0f, 1f)] [SerializeField] private float spilloverRatio = 0.5f;

        [Tooltip("분산 데미지를 이미 죽은(0인) 부위에도 나눌지 여부. 끄면 살아있는 부위에만 분산")]
        [SerializeField] private bool spilloverToDisabledParts = false;

        [Header("기타")]
        [Tooltip("부위 정보가 없는 피격은 이 부위로 처리")]
        [SerializeField] private BodyPartType unknownPartFallback = BodyPartType.Chest;
        [SerializeField] private bool logHits = true;

        private static readonly BodyPartType[] AllParts =
        {
            BodyPartType.Head, BodyPartType.Chest, BodyPartType.Abdomen,
            BodyPartType.LeftArm, BodyPartType.RightArm,
            BodyPartType.LeftLeg, BodyPartType.RightLeg,
        };

        private readonly Dictionary<BodyPartType, float> _current = new Dictionary<BodyPartType, float>();
        private bool _dead;

        public bool IsDead => _dead;

        /// <summary> (부위, 적용 데미지, 그 부위의 남은 HP) </summary>
        public event Action<BodyPartType, float, float> OnPartDamaged;
        /// <summary> HP가 0이 된 부위 </summary>
        public event Action<BodyPartType> OnPartDisabled;
        /// <summary> 사망 원인 부위 </summary>
        public event Action<BodyPartType> OnDeath;

        // ── 다른 시스템이 읽어가는 디버프 결과값 ──────────────────────────
        /// <summary> 이동속도 배율. TacticalShooterPlayer 이동에 곱해서 쓴다. </summary>
        public float MoveSpeedMultiplier { get; private set; } = 1f;
        /// <summary> 점프 높이 배율 </summary>
        public float JumpHeightMultiplier { get; private set; } = 1f;
        /// <summary> 장전 애니메이션 속도 배율 </summary>
        public float ReloadSpeedMultiplier { get; private set; } = 1f;
        /// <summary> 상호작용(문 열기, 줍기) 속도 배율 </summary>
        public float InteractSpeedMultiplier { get; private set; } = 1f;
        /// <summary> 반동 배율 (1보다 커짐) </summary>
        public float RecoilMultiplier { get; private set; } = 1f;
        /// <summary> 달리기 가능 여부. 다리가 하나라도 나가면 false </summary>
        public bool CanSprint { get; private set; } = true;

        private void Awake() => ResetHealth();

        public void ResetHealth()
        {
            _current.Clear();
            foreach (var part in AllParts)
                _current[part] = GetMaxHealth(part);

            _dead = false;
            RecalculateDebuffs();
        }

        public float GetMaxHealth(BodyPartType part)
        {
            switch (part)
            {
                case BodyPartType.Head:     return maxPartHealth.head;
                case BodyPartType.Chest:    return maxPartHealth.chest;
                case BodyPartType.Abdomen:  return maxPartHealth.abdomen;
                case BodyPartType.LeftArm:
                case BodyPartType.RightArm: return maxPartHealth.arm;
                case BodyPartType.LeftLeg:
                case BodyPartType.RightLeg: return maxPartHealth.leg;
                default:                    return 0f;
            }
        }

        public float GetHealth(BodyPartType part) => _current.TryGetValue(part, out var v) ? v : 0f;

        public float GetHealthNormalized(BodyPartType part)
        {
            float max = GetMaxHealth(part);
            return max <= 0f ? 0f : Mathf.Clamp01(GetHealth(part) / max);
        }

        public bool IsPartDisabled(BodyPartType part) => GetHealth(part) <= 0f;

        // ── 데미지 처리 ────────────────────────────────────────────────
        public void ApplyDamage(DamageInfo info)
        {
            if (_dead) return;

            var part = info.bodyPart;
            if (part == BodyPartType.None) part = unknownPartFallback;

            // 플레이어는 부위 배수를 쓰지 않는다. 들어온 데미지가 곧 부위 데미지.
            ApplyDamageToPart(part, info.baseDamage);

            if (!_dead) RecalculateDebuffs();
        }

        private void ApplyDamageToPart(BodyPartType part, float damage)
        {
            if (damage <= 0f) return;

            float before = GetHealth(part);

            // 이미 망가진 부위를 또 맞은 경우 -> 일정 비율을 나머지 부위로 분산
            if (before <= 0f)
            {
                SpillOver(part, damage * spilloverRatio);
                if (logHits)
                    Debug.Log($"[PlayerHealth] {part.ToKorean()}(이미 손상)에 {damage:F0} 피격 " +
                              $"-> {damage * spilloverRatio:F0} 만큼 다른 부위로 분산", this);
                return;
            }

            float after = Mathf.Max(0f, before - damage);
            _current[part] = after;

            if (logHits)
                Debug.Log($"[PlayerHealth] {part.ToKorean()} 피격 {damage:F0} " +
                          $"({before:F0} -> {after:F0} / {GetMaxHealth(part):F0})", this);

            OnPartDamaged?.Invoke(part, damage, after);

            if (after <= 0f)
            {
                OnPartDisabled?.Invoke(part);

                // 머리 / 흉부 / 복부 중 하나라도 소진되면 사망
                if (part.IsVital())
                {
                    Die(part);
                    return;
                }
            }
        }

        /// <summary> 나머지 부위 전체에 균등 분산. 분산 피해는 재분산되지 않는다. </summary>
        private void SpillOver(BodyPartType source, float totalDamage)
        {
            if (totalDamage <= 0f) return;

            var targets = new List<BodyPartType>(AllParts.Length);
            foreach (var p in AllParts)
            {
                if (p == source) continue;
                if (!spilloverToDisabledParts && GetHealth(p) <= 0f) continue;
                targets.Add(p);
            }
            if (targets.Count == 0) return;

            float each = totalDamage / targets.Count;

            foreach (var p in targets)
            {
                float before = GetHealth(p);
                if (before <= 0f) continue;

                float after = Mathf.Max(0f, before - each);
                _current[p] = after;
                OnPartDamaged?.Invoke(p, each, after);

                if (after <= 0f)
                {
                    OnPartDisabled?.Invoke(p);
                    if (p.IsVital()) { Die(p); return; }
                }
            }
        }

        private void Die(BodyPartType cause)
        {
            if (_dead) return;
            _dead = true;
            RecalculateDebuffs();

            if (logHits) Debug.Log($"[PlayerHealth] 사망 - 원인 부위: {cause.ToKorean()}", this);
            OnDeath?.Invoke(cause);
        }

        // ── 디버프 재계산 ──────────────────────────────────────────────
        private void RecalculateDebuffs()
        {
            float move = 1f, jump = 1f, reload = 1f, interact = 1f, recoil = 1f;
            bool canSprint = true;

            // 흉부/복부 50% 이하 -> 이동속도 감소
            if (GetHealthNormalized(BodyPartType.Chest) <= 0.5f)   move *= debuffs.woundedTorsoMoveSpeed;
            if (GetHealthNormalized(BodyPartType.Abdomen) <= 0.5f) move *= debuffs.woundedTorsoMoveSpeed;

            // 다리 소진 -> 이속/점프 감소, 달리기 불가
            foreach (var leg in new[] { BodyPartType.LeftLeg, BodyPartType.RightLeg })
            {
                if (!IsPartDisabled(leg)) continue;
                move *= debuffs.brokenLegMoveSpeed;
                jump *= debuffs.brokenLegJumpHeight;
                canSprint = false;
            }

            // 팔 소진 -> 장전/상호작용 느려지고 반동 증가
            foreach (var arm in new[] { BodyPartType.LeftArm, BodyPartType.RightArm })
            {
                if (!IsPartDisabled(arm)) continue;
                reload   *= debuffs.brokenArmReloadSpeed;
                interact *= debuffs.brokenArmInteractSpeed;
                recoil   *= debuffs.brokenArmRecoilScale;
            }

            MoveSpeedMultiplier    = move;
            JumpHeightMultiplier   = jump;
            ReloadSpeedMultiplier  = reload;
            InteractSpeedMultiplier = interact;
            RecoilMultiplier       = recoil;
            CanSprint              = canSprint && !_dead;
        }

        [ContextMenu("체력 전체 회복")]
        public void FullHeal() => ResetHealth();

        /// <summary> 특정 부위 회복 (붕대/의료키트용) </summary>
        public void Heal(BodyPartType part, float amount)
        {
            if (_dead || amount <= 0f) return;
            if (!_current.ContainsKey(part)) return;

            _current[part] = Mathf.Min(GetMaxHealth(part), _current[part] + amount);
            RecalculateDebuffs();
        }
    }
}
