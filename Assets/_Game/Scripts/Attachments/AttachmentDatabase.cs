// [KILL OR DEAD] Attachments
using System.Collections.Generic;
using UnityEngine;

namespace KillOrDead.Attachments
{
    /// <summary>
    /// 게임에 존재하는 모든 부착물 목록.
    /// 저장 데이터의 ID를 실제 에셋으로 되돌릴 때, 그리고 상점/인벤토리 목록을 만들 때 쓴다.
    /// 생성: Project 우클릭 > Create > KILL OR DEAD > Attachment Database
    /// </summary>
    [CreateAssetMenu(fileName = "AttachmentDatabase", menuName = "KILL OR DEAD/Attachment Database")]
    public class AttachmentDatabase : ScriptableObject
    {
        [SerializeField] private List<AttachmentDefinition> attachments = new List<AttachmentDefinition>();

        private Dictionary<string, AttachmentDefinition> _lookup;

        public IReadOnlyList<AttachmentDefinition> All => attachments;

        public AttachmentDefinition GetById(string id)
        {
            BuildLookup();
            return !string.IsNullOrEmpty(id) && _lookup.TryGetValue(id, out var def) ? def : null;
        }

        /// <summary> 특정 종류의 부착물만 뽑는다. 모딩 UI에서 슬롯을 눌렀을 때 쓴다. </summary>
        public List<AttachmentDefinition> GetBySlotType(AttachmentSlotType slotType)
        {
            var result = new List<AttachmentDefinition>();
            foreach (var def in attachments)
                if (def != null && def.slotType == slotType) result.Add(def);
            return result;
        }

        /// <summary> 이 슬롯에 실제로 꽂을 수 있는 것만 뽑는다. </summary>
        public List<AttachmentDefinition> GetCompatible(AttachmentSocket socket)
        {
            var result = new List<AttachmentDefinition>();
            if (socket == null) return result;

            foreach (var def in attachments)
                if (def != null && socket.Accepts(def)) result.Add(def);
            return result;
        }

        private void BuildLookup()
        {
            if (_lookup != null && _lookup.Count == attachments.Count) return;

            _lookup = new Dictionary<string, AttachmentDefinition>(attachments.Count);
            foreach (var def in attachments)
            {
                if (def == null || string.IsNullOrEmpty(def.id)) continue;
                if (_lookup.ContainsKey(def.id))
                {
                    Debug.LogError($"[부착물 DB] ID가 중복됩니다: '{def.id}'. " +
                                   $"저장/불러오기가 깨지므로 하나를 바꿔주세요.", this);
                    continue;
                }
                _lookup[def.id] = def;
            }
        }

#if UNITY_EDITOR
        /// <summary> 에디터에서 프로젝트 전체의 부착물 에셋을 긁어와 목록을 채운다. </summary>
        [ContextMenu("프로젝트에서 부착물 전부 수집")]
        private void CollectAll()
        {
            attachments.Clear();
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(AttachmentDefinition)}");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var def = UnityEditor.AssetDatabase.LoadAssetAtPath<AttachmentDefinition>(path);
                if (def != null) attachments.Add(def);
            }
            _lookup = null;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[부착물 DB] {attachments.Count}개 수집 완료", this);
        }
#endif
    }
}
