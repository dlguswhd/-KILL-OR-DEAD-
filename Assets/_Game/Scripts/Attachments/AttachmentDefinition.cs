// [KILL OR DEAD] Attachments
using UnityEngine;

namespace KillOrDead.Attachments
{
    /// <summary>
    /// 부착물 한 종류의 데이터.
    /// 로우폴리 팩 프리팹 하나당 이 에셋 하나를 만든다.
    /// 생성: Project 우클릭 > Create > KILL OR DEAD > Attachment
    /// </summary>
    [CreateAssetMenu(fileName = "ATT_New", menuName = "KILL OR DEAD/Attachment")]
    public class AttachmentDefinition : ScriptableObject
    {
        [Header("식별")]
        [Tooltip("저장/구매에 쓰는 고유 ID. 절대 바꾸지 말 것. 예: opt_holo_a")]
        public string id = "att_new";
        [Tooltip("게임 안에 표시될 한글 이름")]
        public string displayName = "새 부착물";
        [TextArea(2, 4)] public string description;
        public Sprite icon;

        [Header("모델")]
        [Tooltip("Low Poly 팩의 부착물 프리팹")]
        public GameObject prefab;
        public AttachmentSlotType slotType = AttachmentSlotType.Optic;

        [Header("부착 위치 보정")]
        [Tooltip("로우폴리 부착물은 TSP 무기와 크기가 달라서 대부분 보정이 필요하다.\n" +
                 "1로 두고 씬에서 눈으로 맞춘 뒤 그 값을 여기 적으면 모든 무기에 일괄 적용된다.")]
        public float scaleMultiplier = 1f;
        public Vector3 localPositionOffset = Vector3.zero;
        public Vector3 localRotationOffset = Vector3.zero;

        [Header("조준경 설정 (Optic만)")]
        [Tooltip("이 부착물이 조준점을 가져가는가. 프리팹 안에 AttachmentAimPoint가 있어야 한다.")]
        public bool overrideAimPoint = false;
        [Tooltip("조준점이 여럿일 때 큰 쪽이 이긴다. 스코프 10, 도트 5, 아이언사이트 0")]
        public int aimPriority = 0;
        [Tooltip("조준 시 카메라 FOV. 낮을수록 확대. 0이면 무기 기본값 사용.\n" +
                 "도트 55, 저배율 40, 고배율 25 정도가 무난하다.")]
        public float aimFovOverride = 0f;

        [Header("왼손 위치 (전방그립 등)")]
        [Tooltip("켜면 이 부착물을 달았을 때 왼손 IK 위치가 바뀐다.\n" +
                 "값은 씬에서 눈으로 맞춰야 한다. 그립마다 5~10분 걸린다.")]
        public bool overrideLeftHandOffset = false;
        public Vector3 leftHandPosition = Vector3.zero;
        public Vector3 leftHandRotation = Vector3.zero;

        [Header("성능")]
        [Tooltip("반동 배율. 1보다 작으면 반동 감소")]
        [Min(0.1f)] public float recoilMultiplier = 1f;
        [Tooltip("조준 속도 배율. 무거운 스코프는 1보다 작게")]
        [Min(0.1f)] public float aimSpeedMultiplier = 1f;
        [Tooltip("비조준 탄퍼짐 배율")]
        [Min(0.1f)] public float hipSpreadMultiplier = 1f;
        [Tooltip("조준 탄퍼짐 배율")]
        [Min(0.1f)] public float aimSpreadMultiplier = 1f;
        [Tooltip("인체공학 가감. 그립 +10, 무거운 스코프 -8 같은 식")]
        public int ergonomicsDelta = 0;
        [Min(0f)] public float weightKg = 0.1f;

        [Header("소음")]
        [Tooltip("소음기인가. 켜면 TSP의 소음 발사음/머즐플래시로 바뀐다.")]
        public bool isSuppressor = false;
        [Tooltip("격발 소음 반경 배율. 기획서 의심도 시스템에 쓴다.\n" +
                 "기획서 기준 일반 격발 100m, 소음기 30m -> 소음기는 0.3")]
        [Min(0f)] public float noiseRadiusMultiplier = 1f;

        [Header("경제")]
        [Min(0)] public int price = 1000;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name.ToLowerInvariant();
            if (scaleMultiplier <= 0f) scaleMultiplier = 1f;
        }
    }
}
