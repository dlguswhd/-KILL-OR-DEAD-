// [KILL OR DEAD] Attachments
using System.Collections.Generic;
using UnityEngine;

namespace KillOrDead.Attachments
{
    /// <summary>
    /// 부착물을 꽂는 지점. 무기 프리팹 안에 빈 오브젝트로 만들어 위치를 잡는다.
    /// 부착물 프리팹 안에도 둘 수 있고(레일 위의 옵틱 자리처럼), 그러면 계층이 생긴다.
    ///
    /// 이 오브젝트의 위치/회전이 곧 부착물이 놓일 자리다.
    /// Z축이 총구 방향, Y축이 위를 보게 두는 걸 권장.
    /// </summary>
    [AddComponentMenu("KILL OR DEAD/Attachments/Attachment Socket")]
    [DisallowMultipleComponent]
    public class AttachmentSocket : MonoBehaviour
    {
        [Header("식별")]
        [Tooltip("UI에 표시될 이름. 예: 상부 레일, 총구, 전방 그립")]
        [SerializeField] private string displayName = "슬롯";

        [Tooltip("저장/불러오기에 쓰는 고유 키. 같은 무기 안에서 겹치면 안 된다.")]
        [SerializeField] private string socketKey = "socket";

        [Header("허용 종류")]
        [Tooltip("이 슬롯에 꽂을 수 있는 부착물 종류들")]
        [SerializeField]
        private List<AttachmentSlotType> acceptedTypes = new List<AttachmentSlotType>
        {
            AttachmentSlotType.Optic
        };

        [Tooltip("특정 부착물만 허용하고 싶을 때 ID를 적는다. 비워두면 종류만 본다.")]
        [SerializeField] private List<string> whitelistIds = new List<string>();

        [Header("기본 장착")]
        [Tooltip("무기를 처음 만들 때 기본으로 달려 있을 부착물")]
        [SerializeField] private AttachmentDefinition defaultAttachment;

        [Header("씬 뷰")]
        [SerializeField] private bool drawGizmo = true;
        [SerializeField] private float gizmoSize = 0.02f;

        public string DisplayName => displayName;
        public string SocketKey => socketKey;
        public IReadOnlyList<AttachmentSlotType> AcceptedTypes => acceptedTypes;
        public AttachmentDefinition DefaultAttachment => defaultAttachment;

        /// <summary> 현재 꽂혀 있는 부착물. 없으면 null </summary>
        public AttachmentInstance Current { get; internal set; }
        public bool IsOccupied => Current != null;

        public bool Accepts(AttachmentDefinition definition)
        {
            if (definition == null) return false;
            if (!acceptedTypes.Contains(definition.slotType)) return false;
            if (whitelistIds.Count > 0 && !whitelistIds.Contains(definition.id)) return false;
            return true;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmo) return;

            Gizmos.color = IsOccupied
                ? new Color(0.3f, 0.8f, 1f, 0.8f)
                : new Color(1f, 0.85f, 0.2f, 0.8f);

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * gizmoSize);
            Gizmos.DrawRay(Vector3.zero, Vector3.forward * gizmoSize * 3f);
        }
    }
}
