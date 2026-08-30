// [KILL OR DEAD] Debug/Test
using System.Collections;
using KillOrDead.Combat;
using UnityEngine;

namespace KillOrDead.Enemies
{
    /// <summary>
    /// 사격 판정 테스트용 더미. EnemyHealth와 함께 붙인다.
    /// 맞으면 HP가 깎이고, 0이 되면 쓰러졌다가 일정 시간 뒤 부활한다.
    /// 진짜 적 AI가 들어오기 전까지 조준/데미지/배수를 눈으로 확인하는 용도.
    /// </summary>
    [AddComponentMenu("KILL OR DEAD/Enemy/Target Dummy")]
    [RequireComponent(typeof(EnemyHealth))]
    public class TargetDummy : MonoBehaviour
    {
        [Header("사망 연출")]
        [Tooltip("사망 시 켤 래그돌 루트. 없으면 그냥 쓰러지는 회전만 한다.")]
        [SerializeField] private Transform ragdollRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private float fallDuration = 0.6f;

        [Header("부활")]
        [SerializeField] private bool autoRespawn = true;
        [SerializeField] private float respawnDelay = 3f;

        [Header("피격 표시")]
        [Tooltip("맞을 때마다 잠깐 색이 바뀔 렌더러들. 비워두면 자식 전체.")]
        [SerializeField] private Renderer[] flashRenderers;
        [SerializeField] private Color flashColor = new Color(1f, 0.3f, 0.2f);
        [SerializeField] private float flashDuration = 0.08f;

        [Header("데미지 숫자 로그")]
        [SerializeField] private bool logDamage = true;

        private EnemyHealth _health;
        private Quaternion _startRotation;
        private Vector3 _startPosition;
        private MaterialPropertyBlock _block;
        private Coroutine _flashRoutine;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (flashRenderers == null || flashRenderers.Length == 0)
                flashRenderers = GetComponentsInChildren<Renderer>();

            _block = new MaterialPropertyBlock();
            _startRotation = transform.rotation;
            _startPosition = transform.position;

            SetRagdoll(false);
        }

        private void OnEnable()
        {
            _health.OnDamaged += HandleDamaged;
            _health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            _health.OnDamaged -= HandleDamaged;
            _health.OnDeath -= HandleDeath;
        }

        private void HandleDamaged(DamageInfo info, float finalDamage, float remaining)
        {
            if (logDamage)
                Debug.Log($"<color=#ffcc00>[더미] {info.bodyPart.ToKorean()} " +
                          $"-{finalDamage:F0}</color>  남은 HP {remaining:F0}/{_health.MaxHealth:F0}", this);

            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            ApplyColor(flashColor);
            yield return new WaitForSeconds(flashDuration);
            ClearColor();
            _flashRoutine = null;
        }

        private void ApplyColor(Color color)
        {
            foreach (var r in flashRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, color);
                r.SetPropertyBlock(_block);
            }
        }

        private void ClearColor()
        {
            foreach (var r in flashRenderers)
            {
                if (r == null) continue;
                r.SetPropertyBlock(null);
            }
        }

        private void HandleDeath(DamageInfo info)
        {
            if (logDamage) Debug.Log($"<color=#ff4444>[더미] 처치됨 ({info.bodyPart.ToKorean()})</color>", this);
            StartCoroutine(DeathRoutine(info));
        }

        private IEnumerator DeathRoutine(DamageInfo info)
        {
            if (animator != null) animator.enabled = false;

            if (ragdollRoot != null)
            {
                SetRagdoll(true);
            }
            else
            {
                // 래그돌이 없으면 맞은 방향으로 쓰러지는 회전만
                var axis = Vector3.Cross(Vector3.up, info.direction).normalized;
                if (axis.sqrMagnitude < 0.001f) axis = transform.right;

                var from = transform.rotation;
                var to = Quaternion.AngleAxis(85f, axis) * from;

                float t = 0f;
                while (t < fallDuration)
                {
                    t += Time.deltaTime;
                    transform.rotation = Quaternion.Slerp(from, to, t / fallDuration);
                    yield return null;
                }
            }

            if (!autoRespawn) yield break;

            yield return new WaitForSeconds(respawnDelay);
            Respawn();
        }

        [ContextMenu("부활")]
        public void Respawn()
        {
            StopAllCoroutines();
            _flashRoutine = null;

            SetRagdoll(false);
            transform.SetPositionAndRotation(_startPosition, _startRotation);

            if (animator != null) animator.enabled = true;
            ClearColor();
            _health.ResetHealth();
        }

        private void SetRagdoll(bool enabled)
        {
            if (ragdollRoot == null) return;

            foreach (var body in ragdollRoot.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = !enabled;
                body.detectCollisions = true;
            }
        }
    }
}
