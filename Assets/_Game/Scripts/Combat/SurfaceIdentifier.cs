// [KILL OR DEAD] Combat Core
using UnityEngine;

namespace KillOrDead.Combat
{
    /// <summary>
    /// 레벨 지오메트리에 붙여 재질을 알려주는 태그 컴포넌트.
    /// 붙어있지 않으면 ImpactEffectLibrary의 기본 재질(콘크리트)로 처리된다.
    /// </summary>
    [AddComponentMenu("KILL OR DEAD/Combat/Surface Identifier")]
    public class SurfaceIdentifier : MonoBehaviour
    {
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Concrete;
        public SurfaceType SurfaceType => surfaceType;

        /// <summary> 콜라이더에서 재질을 알아낸다. Hitbox > SurfaceIdentifier > 기본값 순. </summary>
        public static SurfaceType Resolve(Collider collider, SurfaceType fallback = SurfaceType.Concrete)
        {
            if (collider == null) return fallback;

            var hitbox = collider.GetComponent<Hitbox>();
            if (hitbox != null) return hitbox.SurfaceType;

            var identifier = collider.GetComponentInParent<SurfaceIdentifier>();
            return identifier != null ? identifier.surfaceType : fallback;
        }
    }
}
