// [KILL OR DEAD] Weapons
using System.Collections.Generic;
using KillOrDead.Attachments;
using KillOrDead.Combat;
using KillOrDead.Recoil;
using KINEMATION.TacticalShooterPack.Scripts.Weapon;
using UnityEngine;

namespace KillOrDead.Weapons
{
    /// <summary>
    /// TSP 무기 프리팹에 붙이는 "실제 총알" 컴포넌트.
    ///
    /// TSP의 TacticalShooterWeapon은 머즐플래시/반동/사운드/탄약 감소까지만 하고
    /// 레이캐스트를 쏘지 않는다. 이 컴포넌트가 TacticalShooterWeapon.OnFired를 구독해
    /// 격발 순간마다 카메라 중심에서 레이를 쏘고 데미지와 임팩트를 처리한다.
    ///
    /// 붙이는 곳: 각 무기 프리팹의 TacticalShooterWeapon과 같은 GameObject
    /// </summary>
    [AddComponentMenu("KILL OR DEAD/Weapons/Weapon Ballistics")]
    public class WeaponBallistics : MonoBehaviour
    {
        [Header("필수")]
        [SerializeField] private WeaponDamageProfile profile;
        [SerializeField] private ImpactEffectLibrary impactLibrary;

        [Header("반동")]
        [Tooltip("이 무기의 배틀그라운드식 반동 패턴")]
        [SerializeField] private RecoilPattern recoilPattern;

        [Tooltip("비워두면 플레이어 루트에서 자동으로 찾는다. 적 무기는 비워두면 된다.")]
        [SerializeField] private CameraRecoil cameraRecoil;

        [Header("발사 지점")]
        [Tooltip("트레이서/디버그선의 시작점. 총구 오브젝트를 넣는다.\n" +
                 "데미지 판정 자체는 카메라 중심에서 나간다(FPS 표준).")]
        [SerializeField] private Transform muzzle;

        [Tooltip("비워두면 Camera.main을 쓴다.")]
        [SerializeField] private Camera shootCamera;

        [Header("레이어")]
        [Tooltip("총알이 맞을 수 있는 레이어. 플레이어 본인은 반드시 제외할 것.")]
        [SerializeField] private LayerMask hitMask = ~0;

        [Tooltip("트리거 콜라이더도 맞출지 여부. 보통 Ignore.")]
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("관통")]
        [Tooltip("한 발이 최대 몇 개의 대상을 뚫고 지나가는가. 1이면 관통 없음.")]
        [Min(1)] [SerializeField] private int maxPenetrations = 1;
        [Tooltip("대상 하나를 뚫을 때마다 남는 데미지 비율")]
        [Range(0f, 1f)] [SerializeField] private float penetrationDamageLoss = 0.5f;

        [Header("트레이서 (선택)")]
        [SerializeField] private GameObject tracerPrefab;
        [SerializeField] private float tracerSpeed = 400f;

        [Header("디버그")]
        [SerializeField] private bool drawDebugRay = false;
        [SerializeField] private float debugRayDuration = 2f;

        private TacticalShooterWeapon _weapon;
        private float _currentExtraSpread;
        private float _lastShotTime;
        private readonly List<IDamageable> _hitThisShot = new List<IDamageable>();
        private static RaycastHit[] _hitBuffer = new RaycastHit[16];

        /// <summary> 조준 중인지 여부. TSP 무기의 OnAimingChanged를 구독해 자동으로 갱신된다. </summary>
        public bool IsAiming { get; set; }

        public WeaponDamageProfile Profile => profile;

        /// <summary> 부착물로 합산된 수정치. WeaponAttachmentSystem이 넣어준다. </summary>
        public WeaponModStats ModStats { get; private set; } = WeaponModStats.Identity;

        /// <summary> 부착물 구성이 바뀔 때 WeaponAttachmentSystem이 호출한다. </summary>
        public void ApplyModStats(WeaponModStats stats) => ModStats = stats;

        private void Awake()
        {
            // 같은 오브젝트에 없으면 자식에서 찾는다.
            // (무기 프리팹을 빈 오브젝트로 한 번 감싸서 쓰는 구성도 허용하기 위함)
            _weapon = GetComponent<TacticalShooterWeapon>();
            if (_weapon == null) _weapon = GetComponentInChildren<TacticalShooterWeapon>(true);

            if (_weapon == null)
            {
                Debug.LogError($"[WeaponBallistics] '{name}' 아래에 TacticalShooterWeapon이 없습니다. " +
                               $"무기 프리팹에 붙였는지 확인하세요. 이 컴포넌트를 끕니다.", this);
                enabled = false;
                return;
            }

            if (_weapon.tacWeaponSettings == null)
            {
                Debug.LogError($"[WeaponBallistics] '{_weapon.name}'의 TacticalShooterWeapon에 " +
                               $"Tac Weapon Settings가 비어 있습니다. 이대로면 TSP가 실행 중에 터집니다.", _weapon);
            }

            if (muzzle == null) muzzle = transform;
        }

        private CameraRecoil ResolveRecoil()
        {
            // 무기는 플레이어 손에 런타임으로 생성되므로 Awake 시점엔 아직 부모가 없다.
            // 처음 쏠 때 찾는다.
            if (cameraRecoil == null) cameraRecoil = transform.root.GetComponentInChildren<CameraRecoil>();
            return cameraRecoil;
        }

        private void OnEnable()
        {
            if (_weapon == null) return;
            _weapon.OnFired += HandleFired;
            _weapon.OnAimingChanged += HandleAimingChanged;
            IsAiming = _weapon.IsAimingWeapon;
        }

        private void OnDisable()
        {
            if (_weapon == null) return;
            _weapon.OnFired -= HandleFired;
            _weapon.OnAimingChanged -= HandleAimingChanged;
        }

        private void HandleAimingChanged(bool aiming) => IsAiming = aiming;

        private void Update()
        {
            // 사격을 멈추면 탄퍼짐이 서서히 회복된다.
            if (profile == null) return;
            if (Time.time - _lastShotTime < 0.05f) return;

            _currentExtraSpread = Mathf.Max(0f,
                _currentExtraSpread - profile.spreadRecoveryPerSecond * Time.deltaTime);
        }

        private void HandleFired()
        {
            if (profile == null)
            {
                Debug.LogWarning($"[WeaponBallistics] '{name}'에 WeaponDamageProfile이 없어 총알이 나가지 않습니다.", this);
                return;
            }

            var cam = ResolveCamera();
            if (cam == null)
            {
                Debug.LogWarning("[WeaponBallistics] 발사에 쓸 카메라를 찾지 못했습니다.", this);
                return;
            }

            _lastShotTime = Time.time;

            // 배틀그라운드식 반동 - 조준점을 실제로 밀어올린다.
            // 부착물의 반동 배율이 여기에 곱해진다.
            if (recoilPattern != null)
            {
                var recoil = ResolveRecoil();
                if (recoil != null) recoil.ApplyShot(recoilPattern, IsAiming, ModStats.recoilMultiplier);
            }

            Vector3 origin = cam.transform.position;
            Vector3 forward = cam.transform.forward;

            // 부착물(그립 등)이 탄퍼짐을 줄여준다.
            float baseSpread = IsAiming
                ? profile.aimSpreadDegrees * ModStats.aimSpreadMultiplier
                : profile.hipSpreadDegrees * ModStats.hipSpreadMultiplier;
            float spread = baseSpread + _currentExtraSpread;

            for (int i = 0; i < profile.pelletsPerShot; i++)
            {
                Vector3 direction = ApplySpread(forward, spread);
                FireSingleRay(origin, direction, cam.transform.position);
            }

            // 연사 시 퍼짐 누적
            float cap = profile.hipSpreadDegrees * 3f;
            _currentExtraSpread = Mathf.Min(cap, _currentExtraSpread + profile.spreadGrowthPerShot);
        }

        /// <summary>
        /// 적 AI가 총을 쏠 때처럼 카메라 없이 직접 쏘고 싶을 때 사용.
        /// </summary>
        public void FireFrom(Vector3 origin, Vector3 direction, float spreadDegrees = 0f)
        {
            if (profile == null) return;
            for (int i = 0; i < profile.pelletsPerShot; i++)
                FireSingleRay(origin, ApplySpread(direction, spreadDegrees), origin);
        }

        private void FireSingleRay(Vector3 origin, Vector3 direction, Vector3 tracerRefPoint)
        {
            _hitThisShot.Clear();

            float remainingMultiplier = 1f;
            float traveled = 0f;
            Vector3 currentOrigin = origin;
            Vector3 finalPoint = origin + direction * profile.maxDistance;

            for (int penetration = 0; penetration < maxPenetrations; penetration++)
            {
                float remainingDistance = profile.maxDistance - traveled;
                if (remainingDistance <= 0f) break;

                int count = Physics.RaycastNonAlloc(
                    new Ray(currentOrigin, direction), _hitBuffer,
                    remainingDistance, hitMask, triggerInteraction);

                if (count == 0) break;

                // 가장 가까운 유효 히트 찾기
                RaycastHit best = default;
                float bestDist = float.MaxValue;
                bool found = false;

                for (int i = 0; i < count; i++)
                {
                    var h = _hitBuffer[i];
                    if (h.collider == null) continue;
                    // 자기 자신(무기/쏜 사람)은 무시
                    if (h.collider.transform.IsChildOf(transform.root)) continue;
                    if (h.distance >= bestDist) continue;

                    best = h;
                    bestDist = h.distance;
                    found = true;
                }

                if (!found) break;

                traveled += best.distance;
                finalPoint = best.point;

                ProcessHit(best, direction, traveled, remainingMultiplier);

                // 관통: 조금 앞에서 다시 시작
                currentOrigin = best.point + direction * 0.02f;
                remainingMultiplier *= (1f - penetrationDamageLoss);
                if (remainingMultiplier <= 0.01f) break;
            }

            SpawnTracer(tracerRefPoint, finalPoint);

            if (drawDebugRay)
                Debug.DrawLine(origin, finalPoint, Color.red, debugRayDuration);
        }

        private void ProcessHit(RaycastHit hit, Vector3 direction, float distance, float multiplier)
        {
            float damage = profile.GetDamageAtDistance(distance) * multiplier;

            var info = new DamageInfo
            {
                baseDamage = damage,
                bodyPart = BodyPartType.None,
                hitPoint = hit.point,
                hitNormal = hit.normal,
                direction = direction,
                instigator = transform.root.gameObject,
                distance = distance,
                impactForce = profile.impactForce,
            };

            var hitbox = hit.collider.GetComponent<Hitbox>();
            if (hitbox != null)
            {
                // 같은 발이 같은 대상을 두 번 때리지 않도록 (관통 시 앞뒤 콜라이더)
                var owner = hitbox.Owner;
                if (owner == null || !_hitThisShot.Contains(owner))
                {
                    if (owner != null) _hitThisShot.Add(owner);
                    hitbox.ReceiveHit(info);
                }
            }
            else
            {
                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null && !_hitThisShot.Contains(damageable))
                {
                    _hitThisShot.Add(damageable);
                    damageable.ApplyDamage(info);
                }

                var body = hit.collider.attachedRigidbody;
                if (body != null && profile.impactForce > 0f)
                    body.AddForceAtPosition(direction * profile.impactForce, hit.point, ForceMode.Impulse);
            }

            if (impactLibrary != null)
            {
                var surface = SurfaceIdentifier.Resolve(hit.collider);
                impactLibrary.Spawn(surface, hit.point, hit.normal);
            }
        }

        private void SpawnTracer(Vector3 from, Vector3 to)
        {
            if (tracerPrefab == null) return;

            Vector3 start = muzzle != null ? muzzle.position : from;
            Vector3 dir = to - start;
            if (dir.sqrMagnitude < 0.0001f) return;

            var tracer = Instantiate(tracerPrefab, start, Quaternion.LookRotation(dir));
            float life = tracerSpeed > 0f ? dir.magnitude / tracerSpeed : 0.1f;
            Destroy(tracer, Mathf.Max(0.05f, life));
        }

        private Vector3 ApplySpread(Vector3 forward, float spreadDegrees)
        {
            if (spreadDegrees <= 0f) return forward.normalized;

            // 원뿔 내부 균등 분포
            float angle = Random.Range(0f, spreadDegrees);
            float roll = Random.Range(0f, 360f);

            var rotation = Quaternion.AngleAxis(angle, Vector3.up);
            var rollRotation = Quaternion.AngleAxis(roll, Vector3.forward);

            return (Quaternion.LookRotation(forward) * rollRotation * rotation * Vector3.forward).normalized;
        }

        private Camera ResolveCamera()
        {
            if (shootCamera != null) return shootCamera;

            shootCamera = transform.root.GetComponentInChildren<Camera>();
            if (shootCamera == null) shootCamera = Camera.main;
            return shootCamera;
        }
    }
}
