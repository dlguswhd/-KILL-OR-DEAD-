// [KILL OR DEAD] Attachments
using UnityEngine;

namespace KillOrDead.Attachments
{
    /// <summary>
    /// 조준경 프리팹 안에서 "여기가 눈이 가는 지점"을 표시하는 마커.
    ///
    /// 배치 요령: 렌즈 정중앙, 총열과 나란한 방향(Z축이 총구 방향)으로 두고
    /// 렌즈 뒤쪽으로 살짝 뺀다. 이 트랜스폼이 카메라와 정렬된다.
    /// </summary>
    [AddComponentMenu("KILL OR DEAD/Attachments/Attachment Aim Point")]
    public class AttachmentAimPoint : MonoBehaviour
    {
        [Tooltip("씬 뷰에서 조준선을 보여준다")]
        [SerializeField] private bool drawGizmo = true;

        private void OnDrawGizmos()
        {
            if (!drawGizmo) return;
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.006f);
            Gizmos.DrawRay(transform.position, transform.forward * 0.4f);
        }
    }
}
