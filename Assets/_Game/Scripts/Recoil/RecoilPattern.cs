// [KILL OR DEAD] Recoil
using UnityEngine;

namespace KillOrDead.Recoil
{
    /// <summary>
    /// 배틀그라운드식 반동 패턴.
    ///
    /// 핵심: 반동이 카메라를 "흔드는" 게 아니라 조준점을 실제로 밀어올린다.
    /// 플레이어가 마우스를 내려서 직접 잡아야 하고, 내린 만큼 자동 복구분이 차감된다.
    /// (그래서 사격을 멈춰도 총구가 원래 자리보다 아래로 처지지 않는다)
    ///
    /// 생성: Project 우클릭 > Create > KILL OR DEAD > Recoil Pattern
    /// </summary>
    [CreateAssetMenu(fileName = "RCL_New", menuName = "KILL OR DEAD/Recoil Pattern")]
    public class RecoilPattern : ScriptableObject
    {
        [Header("수직 반동")]
        [Tooltip("한 발당 총구가 올라가는 각도(도). AK급은 1.0~1.5 정도")]
        [Min(0f)] public float verticalKick = 1.2f;

        [Tooltip("연사가 진행될수록 반동이 어떻게 변하는가.\n" +
                 "가로축 = 발수 / patternLength, 세로축 = 배율.\n" +
                 "실총처럼 초반에 약하고 중반에 강해지게 두는 게 자연스럽다.")]
        public AnimationCurve verticalOverBurst = new AnimationCurve(
            new Keyframe(0f, 0.75f),
            new Keyframe(0.25f, 1.15f),
            new Keyframe(0.6f, 1.0f),
            new Keyframe(1f, 0.9f));

        [Header("수평 반동")]
        [Tooltip("한 발당 좌우로 밀리는 각도(도)")]
        [Min(0f)] public float horizontalKick = 0.35f;

        [Tooltip("연사 중 좌우로 휘는 방향. 세로축 -1(왼쪽) ~ +1(오른쪽).\n" +
                 "AK 계열은 초반에 거의 직선이다가 중반부터 오른쪽으로 휜다.")]
        public AnimationCurve horizontalOverBurst = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.2f, -0.2f),
            new Keyframe(0.5f, 0.6f),
            new Keyframe(1f, 0.3f));

        [Tooltip("수평 반동에 섞이는 무작위성(도). 0이면 완전히 외울 수 있는 패턴")]
        [Min(0f)] public float horizontalRandomness = 0.25f;

        [Header("패턴")]
        [Tooltip("패턴 한 바퀴가 몇 발인가. 보통 탄창 크기")]
        [Min(1)] public int patternLength = 30;

        [Tooltip("첫 발 반동 배율. 첫 발이 크게 튀는 총은 1보다 크게")]
        [Min(0f)] public float firstShotMultiplier = 1.3f;

        [Tooltip("이 시간 동안 안 쏘면 패턴이 처음으로 돌아간다(초)")]
        [Min(0f)] public float patternResetTime = 0.35f;

        [Header("반동이 올라가는 속도")]
        [Tooltip("초당 몇 도씩 밀려 올라가는가. 클수록 즉각적.\n" +
                 "0으로 두면 한 프레임에 즉시 적용된다. 60~120이 무난하다.")]
        [Min(0f)] public float kickSpeed = 90f;

        [Header("복구")]
        [Tooltip("마지막 격발 후 이 시간이 지나야 총구가 내려오기 시작한다(초)")]
        [Min(0f)] public float recoveryDelay = 0.12f;

        [Tooltip("총구가 내려오는 속도(도/초)")]
        [Min(0f)] public float recoverySpeed = 14f;

        [Tooltip("올라간 반동 중 몇 %가 자동으로 되돌아오는가.\n" +
                 "1이면 안 잡아도 원래 자리로 완전히 복귀(쉬움).\n" +
                 "0.6~0.8이 하드코어 슈터에 적당하다.")]
        [Range(0f, 1f)] public float recoveryRatio = 0.75f;

        [Header("자세별 배율")]
        [Tooltip("조준(우클릭) 중 반동 배율")]
        [Min(0f)] public float aimMultiplier = 0.7f;

        /// <summary> 몇 발째인지에 따른 수직 반동(도) </summary>
        public float GetVerticalKick(int shotIndex)
        {
            float t = patternLength <= 1 ? 0f : Mathf.Clamp01((float)shotIndex / (patternLength - 1));
            float kick = verticalKick * verticalOverBurst.Evaluate(t);
            if (shotIndex == 0) kick *= firstShotMultiplier;
            return Mathf.Max(0f, kick);
        }

        /// <summary> 몇 발째인지에 따른 수평 반동(도). 음수면 왼쪽 </summary>
        public float GetHorizontalKick(int shotIndex)
        {
            float t = patternLength <= 1 ? 0f : Mathf.Clamp01((float)shotIndex / (patternLength - 1));
            float kick = horizontalKick * horizontalOverBurst.Evaluate(t);
            if (horizontalRandomness > 0f)
                kick += Random.Range(-horizontalRandomness, horizontalRandomness);
            return kick;
        }
    }
}
