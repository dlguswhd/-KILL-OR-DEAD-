// [KILL OR DEAD] Attachments
using UnityEngine;

namespace KillOrDead.Attachments
{
    /// <summary>
    /// 실제로 총에 붙은 부착물 하나. 부착물 프리팹의 루트에 붙인다.
    /// 레일처럼 자기 위에 또 슬롯을 가진 부착물이면 자식에 AttachmentSocket을 두면 된다.
    /// </summary>
    [AddComponentMenu("KILL OR DEAD/Attachments/Attachment Instance")]
    public class AttachmentInstance : MonoBehaviour
    {
        [Tooltip("조준점. 비워두면 자식에서 AttachmentAimPoint를 찾는다.")]
        [SerializeField] private Transform aimPoint;

        [Tooltip("레이저 사이트. 비워두면 자식에서 찾는다.")]
        [SerializeField] private TacticalLaser laser;

        [Tooltip("전술등. 비워두면 자식에서 찾는다.")]
        [SerializeField] private TacticalFlashlight flashlight;

        public AttachmentDefinition Definition { get; private set; }
        public AttachmentSocket ParentSocket { get; private set; }

        public Transform AimPoint => aimPoint;
        public TacticalLaser Laser => laser;
        public TacticalFlashlight Flashlight => flashlight;

        private void Awake() => ResolveReferences();

        private void ResolveReferences()
        {
            if (aimPoint == null)
            {
                var marker = GetComponentInChildren<AttachmentAimPoint>(true);
                if (marker != null) aimPoint = marker.transform;
            }
            if (laser == null) laser = GetComponentInChildren<TacticalLaser>(true);
            if (flashlight == null) flashlight = GetComponentInChildren<TacticalFlashlight>(true);
        }

        internal void Initialize(AttachmentDefinition definition, AttachmentSocket socket)
        {
            Definition = definition;
            ParentSocket = socket;
            ResolveReferences();

            if (definition != null && definition.overrideAimPoint && aimPoint == null)
            {
                Debug.LogWarning(
                    $"[부착물] '{definition.displayName}'이 조준점을 가져가도록 설정돼 있는데 " +
                    $"프리팹 안에 AttachmentAimPoint가 없습니다. 렌즈 중앙에 빈 오브젝트를 만들고 " +
                    $"AttachmentAimPoint를 붙여주세요.", this);
            }
        }
    }
}
