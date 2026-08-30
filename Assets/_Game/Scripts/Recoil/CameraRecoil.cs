// [KILL OR DEAD] Recoil
using KINEMATION.KShooterCore.Runtime.Camera;
using KINEMATION.TacticalShooterPack.Scripts.Animation;
using UnityEngine;

namespace KillOrDead.Recoil
{
    /// <summary>
    /// 배틀그라운드식 반동. 플레이어 캐릭터 루트에 붙인다.
    ///
    /// TSP 기본 반동은 총과 카메라를 잠깐 흔들었다 제자리로 돌아오는 "연출"이라
    /// 조준점이 실제로는 안 움직인다. 이 컴포넌트는 조준점 자체를 밀어올린다.
    ///
    /// 동작 원리:
    ///   1) 격발하면 TacticalProceduralAnimation.pitchInput을 직접 줄인다 (= 총구가 올라감)
    ///   2) 올라간 양을 "복구 예산"에 쌓아둔다
    ///   3) 플레이어가 마우스를 내리면 그만큼 복구 예산에서 차감한다
    ///      -> 직접 잡은 반동은 나중에 다시 안 내려온다 (총구 처짐 방지)
    ///   4) 사격을 멈추고 잠시 지나면 남은 예산만큼만 총구가 스스로 내려온다
    ///
    /// 수평 반동은 TSP가 쓰지 않는 FPSCameraAnimator.lookInput.x 채널을 사용한다.
    /// </summary>
    [AddComponentMenu("KILL OR DEAD/Recoil/Camera Recoil")]
    [DefaultExecutionOrder(-50)]
    public class CameraRecoil : MonoBehaviour
    {
        [Header("참조 (비워두면 자동 탐색)")]
        [SerializeField] private TacticalProceduralAnimation proceduralAnimation;
        [SerializeField] private FPSCameraAnimator fpsCamera;

        [Header("전역 배율")]
        [Tooltip("모든 무기 반동에 곱해진다. 게임 전체 난이도 조절용")]
        [Min(0f)] [SerializeField] private float globalMultiplier = 1f;

        [Header("수평 반동 복구")]
        [Tooltip("수평 반동이 0으로 돌아오는 속도(도/초). 수평은 항상 완전히 복구된다")]
        [Min(0f)] [SerializeField] private float horizontalRecoverySpeed = 8f;
        [Tooltip("수평 반동이 적용되는 부드러움. 클수록 즉각적")]
        [Min(0f)] [SerializeField] private float horizontalSmoothing = 14f;

        [Header("디버그")]
        [SerializeField] private bool logShots = false;

        // ── 상태 ──
        private float _pendingKick;        // 아직 카메라에 안 먹인 반동 (도)
        private float _pendingRecovery;    // 되돌릴 수 있는 반동 잔량 (도)
        private float _horizontalTarget;   // 수평 반동 목표
        private float _horizontalCurrent;  // 수평 반동 현재값
        private float _lastPitch;          // 지난 프레임 pitchInput (플레이어 입력 감지용)
        private float _lastShotTime = -999f;
        private int _shotIndex;
        private RecoilPattern _activePattern;

        /// <summary> 현재 올라가 있는 반동 총량(도). HUD나 디버그에서 읽을 수 있다 </summary>
        public float CurrentRecoil => _pendingRecovery;
        public float HorizontalRecoil => _horizontalCurrent;

        private void Awake()
        {
            if (proceduralAnimation == null)
                proceduralAnimation = GetComponentInChildren<TacticalProceduralAnimation>();
            if (fpsCamera == null)
                fpsCamera = transform.root.GetComponentInChildren<FPSCameraAnimator>();

            if (proceduralAnimation == null)
                Debug.LogError("[반동] TacticalProceduralAnimation을 찾지 못했습니다. " +
                               "플레이어 캐릭터에 붙였는지 확인하세요.", this);
        }

        private void Start()
        {
            if (proceduralAnimation != null) _lastPitch = proceduralAnimation.pitchInput;
        }

        /// <summary>
        /// 한 발 쐈을 때 호출. WeaponBallistics가 격발 이벤트에서 불러준다.
        /// </summary>
        /// <param name="pattern">무기의 반동 패턴</param>
        /// <param name="isAiming">조준 중인가</param>
        /// <param name="weaponMultiplier">부착물 등으로 인한 반동 배율</param>
        public void ApplyShot(RecoilPattern pattern, bool isAiming, float weaponMultiplier = 1f)
        {
            if (pattern == null) return;

            // 한동안 안 쏘다가 다시 쏘면 패턴을 처음부터
            if (Time.time - _lastShotTime > pattern.patternResetTime) _shotIndex = 0;

            _activePattern = pattern;
            _lastShotTime = Time.time;

            float multiplier = globalMultiplier * weaponMultiplier;
            if (isAiming) multiplier *= pattern.aimMultiplier;

            float vertical = pattern.GetVerticalKick(_shotIndex) * multiplier;
            float horizontal = pattern.GetHorizontalKick(_shotIndex) * multiplier;

            _pendingKick += vertical;
            _horizontalTarget += horizontal;

            if (logShots)
                Debug.Log($"[반동] {_shotIndex + 1}발째  수직 {vertical:F2}°  수평 {horizontal:+0.00;-0.00}°", this);

            _shotIndex++;
        }

        private void LateUpdate()
        {
            if (proceduralAnimation == null) return;

            float dt = Time.deltaTime;
            var pattern = _activePattern;

            // ── 1) 플레이어가 마우스를 내렸는지 확인 ──────────────────
            // pitchInput이 늘어났다 = 아래를 보고 있다 = 반동을 직접 잡았다
            float playerDelta = proceduralAnimation.pitchInput - _lastPitch;
            if (playerDelta > 0f && _pendingRecovery > 0f)
            {
                // 잡은 만큼 자동 복구 예산에서 뺀다.
                // 이게 없으면 사격을 멈췄을 때 총구가 원래보다 아래로 처진다.
                _pendingRecovery = Mathf.Max(0f, _pendingRecovery - playerDelta);
            }

            // ── 2) 반동을 카메라에 먹인다 ────────────────────────────
            if (_pendingKick > 0f)
            {
                float step = (pattern != null && pattern.kickSpeed > 0f)
                    ? Mathf.Min(_pendingKick, pattern.kickSpeed * dt)
                    : _pendingKick;

                ApplyPitch(-step);            // 음수 = 위를 봄
                _pendingKick -= step;

                // 복구 가능한 양은 recoveryRatio 만큼만
                float ratio = pattern != null ? pattern.recoveryRatio : 1f;
                _pendingRecovery += step * ratio;
            }

            // ── 3) 사격을 멈추면 총구가 내려온다 ──────────────────────
            if (pattern != null && _pendingRecovery > 0f && _pendingKick <= 0f
                && Time.time - _lastShotTime >= pattern.recoveryDelay)
            {
                float step = Mathf.Min(_pendingRecovery, pattern.recoverySpeed * dt);
                ApplyPitch(step);             // 양수 = 아래를 봄
                _pendingRecovery -= step;
            }

            // ── 4) 수평 반동 ────────────────────────────────────────
            _horizontalTarget = Mathf.MoveTowards(_horizontalTarget, 0f, horizontalRecoverySpeed * dt);
            _horizontalCurrent = Mathf.Lerp(_horizontalCurrent, _horizontalTarget,
                1f - Mathf.Exp(-horizontalSmoothing * dt));

            // ── 5) 카메라에 반영 ────────────────────────────────────
            if (fpsCamera != null)
            {
                // TSP는 Update에서 lookInput.y를 채우므로, 우리가 방금 바꾼 pitch로 다시 덮어쓴다.
                fpsCamera.lookInput.y = proceduralAnimation.pitchInput;
                // lookInput.x는 TSP가 쓰지 않는 채널이라 수평 반동 전용으로 쓴다.
                fpsCamera.lookInput.x = _horizontalCurrent;
            }

            _lastPitch = proceduralAnimation.pitchInput;
        }

        private void ApplyPitch(float delta)
        {
            proceduralAnimation.pitchInput =
                Mathf.Clamp(proceduralAnimation.pitchInput + delta, -90f, 90f);
        }

        /// <summary> 리스폰 등에서 반동 상태를 완전히 초기화 </summary>
        public void ResetRecoil()
        {
            _pendingKick = 0f;
            _pendingRecovery = 0f;
            _horizontalTarget = 0f;
            _horizontalCurrent = 0f;
            _shotIndex = 0;
            if (fpsCamera != null) fpsCamera.lookInput.x = 0f;
            if (proceduralAnimation != null) _lastPitch = proceduralAnimation.pitchInput;
        }
    }
}
