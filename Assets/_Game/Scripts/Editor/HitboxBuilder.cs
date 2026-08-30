// [KILL OR DEAD] Editor Tool
#if UNITY_EDITOR
using System.Collections.Generic;
using KillOrDead.Combat;
using UnityEditor;
using UnityEngine;

namespace KillOrDead.EditorTools
{
    /// <summary>
    /// 휴머노이드 캐릭터의 뼈에 부위별 히트박스(콜라이더 + Hitbox)를 자동 생성한다.
    ///
    /// 사용법:
    ///   1) 씬에서 캐릭터를 선택 (Animator가 Humanoid로 설정돼 있어야 함)
    ///   2) 메뉴 > Tools > KILL OR DEAD > 히트박스 생성기
    ///   3) 생성 클릭
    ///
    /// 생성된 콜라이더는 "HB_머리" 같은 이름의 자식 오브젝트로 뼈 밑에 붙는다.
    /// 크기는 자동 추정이므로 씬 뷰에서 눈으로 보고 다듬는 걸 권장.
    /// </summary>
    public class HitboxBuilder : EditorWindow
    {
        private GameObject _target;
        private int _hitboxLayer;
        private bool _replaceExisting = true;
        private float _sizeScale = 1f;

        private const string HitboxPrefix = "HB_";

        [MenuItem("Tools/KILL OR DEAD/히트박스 생성기")]
        public static void Open()
        {
            var window = GetWindow<HitboxBuilder>("히트박스 생성기");
            window.minSize = new Vector2(360f, 260f);
            window._target = Selection.activeGameObject;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("휴머노이드 부위 히트박스 자동 생성", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Animator가 Humanoid로 설정된 캐릭터를 넣고 생성을 누르세요.\n" +
                "머리 / 흉부 / 복부 / 양팔 / 양다리 총 7부위가 만들어집니다.",
                MessageType.Info);

            EditorGUILayout.Space();
            _target = (GameObject)EditorGUILayout.ObjectField("대상 캐릭터", _target, typeof(GameObject), true);
            _hitboxLayer = EditorGUILayout.LayerField("히트박스 레이어", _hitboxLayer);
            _sizeScale = EditorGUILayout.Slider("콜라이더 크기 배율", _sizeScale, 0.5f, 2f);
            _replaceExisting = EditorGUILayout.Toggle("기존 히트박스 지우고 새로", _replaceExisting);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(_target == null))
            {
                if (GUILayout.Button("히트박스 생성", GUILayout.Height(32f)))
                    Build();
            }

            using (new EditorGUI.DisabledScope(_target == null))
            {
                if (GUILayout.Button("히트박스 전부 제거"))
                    Clear(_target);
            }
        }

        private void Build()
        {
            var animator = _target.GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
            {
                EditorUtility.DisplayDialog("실패",
                    "대상에서 Humanoid Animator를 찾지 못했습니다.\n" +
                    "모델 Import Settings > Rig > Animation Type을 Humanoid로 바꾸세요.", "확인");
                return;
            }

            if (_replaceExisting) Clear(_target);

            Undo.RegisterFullObjectHierarchyUndo(_target, "히트박스 생성");

            int created = 0;
            created += TryCapsule(animator, HumanBodyBones.Head, HumanBodyBones.Head, BodyPartType.Head, 0.11f);
            created += TryCapsule(animator, HumanBodyBones.Chest, HumanBodyBones.Neck, BodyPartType.Chest, 0.17f);
            created += TryCapsule(animator, HumanBodyBones.Hips, HumanBodyBones.Spine, BodyPartType.Abdomen, 0.15f);

            created += TryCapsule(animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, BodyPartType.LeftArm, 0.06f);
            created += TryCapsule(animator, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, BodyPartType.LeftArm, 0.055f);
            created += TryCapsule(animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, BodyPartType.RightArm, 0.06f);
            created += TryCapsule(animator, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, BodyPartType.RightArm, 0.055f);

            created += TryCapsule(animator, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, BodyPartType.LeftLeg, 0.09f);
            created += TryCapsule(animator, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, BodyPartType.LeftLeg, 0.075f);
            created += TryCapsule(animator, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, BodyPartType.RightLeg, 0.09f);
            created += TryCapsule(animator, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, BodyPartType.RightLeg, 0.075f);

            EditorUtility.SetDirty(_target);
            Debug.Log($"[히트박스 생성기] '{_target.name}'에 히트박스 {created}개 생성 완료. " +
                      $"씬 뷰에서 크기를 눈으로 확인하고 다듬으세요.", _target);
        }

        private int TryCapsule(Animator animator, HumanBodyBones fromBone, HumanBodyBones toBone,
                               BodyPartType part, float radius)
        {
            var from = animator.GetBoneTransform(fromBone);
            if (from == null) return 0;

            var to = animator.GetBoneTransform(toBone);

            float length;
            Vector3 localDirection;

            if (to != null && to != from)
            {
                Vector3 delta = to.position - from.position;
                length = delta.magnitude;
                localDirection = from.InverseTransformDirection(delta.normalized);
            }
            else
            {
                // 머리처럼 끝 뼈인 경우 구체에 가깝게
                length = radius * 2f;
                localDirection = Vector3.up;
            }

            if (length <= 0.001f) return 0;

            var go = new GameObject($"{HitboxPrefix}{part.ToKorean()}_{fromBone}");
            Undo.RegisterCreatedObjectUndo(go, "히트박스 생성");

            go.transform.SetParent(from, false);
            go.transform.localPosition = localDirection * (length * 0.5f);
            go.transform.localRotation = Quaternion.identity;
            go.layer = _hitboxLayer;

            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.direction = DominantAxis(localDirection);
            capsule.radius = radius * _sizeScale;
            capsule.height = Mathf.Max(length, radius * 2f) * _sizeScale;
            capsule.center = Vector3.zero;

            var hitbox = go.AddComponent<Hitbox>();
            hitbox.SetBodyPart(part);
            hitbox.SetSurface(SurfaceType.Flesh);

            return 1;
        }

        /// <summary> 로컬 방향에서 가장 큰 축을 CapsuleCollider.direction(0=X,1=Y,2=Z)으로 변환 </summary>
        private static int DominantAxis(Vector3 dir)
        {
            float x = Mathf.Abs(dir.x), y = Mathf.Abs(dir.y), z = Mathf.Abs(dir.z);
            if (x >= y && x >= z) return 0;
            if (y >= z) return 1;
            return 2;
        }

        private static void Clear(GameObject target)
        {
            var toDelete = new List<GameObject>();
            foreach (var hitbox in target.GetComponentsInChildren<Hitbox>(true))
            {
                if (hitbox.name.StartsWith(HitboxPrefix))
                    toDelete.Add(hitbox.gameObject);
            }

            foreach (var go in toDelete)
                Undo.DestroyObjectImmediate(go);

            if (toDelete.Count > 0)
                Debug.Log($"[히트박스 생성기] 히트박스 {toDelete.Count}개 제거", target);
        }
    }
}
#endif
