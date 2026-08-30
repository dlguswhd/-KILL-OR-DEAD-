// [KILL OR DEAD] Combat Core
using UnityEngine;

namespace KillOrDead.Combat
{
    /// <summary>
    /// 캐릭터 뼈에 붙는 부위별 피격 판정체.
    /// 콜라이더와 같은 오브젝트에 두고, 부모 어딘가의 IDamageable로 데미지를 넘긴다.
    /// </summary>
    [AddComponentMenu("KILL OR DEAD/Combat/Hitbox")]
    [DisallowMultipleComponent]
    public class Hitbox : MonoBehaviour
    {
        [Header("판정")]
        [SerializeField] private BodyPartType bodyPart = BodyPartType.Chest;

        [Tooltip("비워두면 부모 계층에서 자동으로 찾는다.")]
        [SerializeField] private MonoBehaviour ownerOverride;

        [Header("피격 연출")]
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Flesh;

        [Tooltip("맞았을 때 물리적으로 밀려날 리지드바디. 래그돌이면 자기 자신.")]
        [SerializeField] private Rigidbody attachedBody;

        private IDamageable _owner;
        private bool _resolved;

        public BodyPartType BodyPart => bodyPart;
        public SurfaceType SurfaceType => surfaceType;
        public IDamageable Owner
        {
            get { Resolve(); return _owner; }
        }

        private void Awake()
        {
            Resolve();
            if (attachedBody == null) attachedBody = GetComponent<Rigidbody>();
        }

        private void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            if (ownerOverride is IDamageable overridden)
            {
                _owner = overridden;
                return;
            }
            _owner = GetComponentInParent<IDamageable>();

            if (_owner == null)
                Debug.LogWarning($"[Hitbox] '{name}' 위쪽에 IDamageable이 없습니다. 데미지가 유실됩니다.", this);
        }

        /// <summary>
        /// 총알이 이 히트박스를 맞췄을 때 호출. 부위 정보를 채워 소유자에게 넘긴다.
        /// </summary>
        public void ReceiveHit(DamageInfo info)
        {
            Resolve();
            if (_owner == null) return;

            info.bodyPart = bodyPart;
            _owner.ApplyDamage(info);

            if (attachedBody != null && info.impactForce > 0f)
                attachedBody.AddForceAtPosition(info.direction * info.impactForce, info.hitPoint, ForceMode.Impulse);
        }

        /// <summary> 에디터 툴에서 부위를 세팅할 때 사용 </summary>
        public void SetBodyPart(BodyPartType part) => bodyPart = part;
        public void SetSurface(SurfaceType surface) => surfaceType = surface;
    }
}
