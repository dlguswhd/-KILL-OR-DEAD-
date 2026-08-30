// [KILL OR DEAD] Editor Tool
#if UNITY_EDITOR
using System.Collections.Generic;
using KillOrDead.Attachments;
using KINEMATION.TacticalShooterPack.Scripts.Weapon;
using UnityEditor;
using UnityEngine;

namespace KillOrDead.EditorTools
{
    /// <summary>
    /// 무기에 부착물 슬롯을 빠르게 만드는 툴.
    ///
    /// 사용법:
    ///   1) 무기 프리팹을 프리팹 모드로 열거나 씬에 꺼낸다
    ///   2) 메뉴 > Tools > KILL OR DEAD > 부착물 슬롯 도구
    ///   3) "표준 슬롯 4종 생성"을 누르면 상부레일/총구/총열하부/측면레일이 만들어진다
    ///   4) 씬 뷰에서 각 슬롯을 실제 위치(레일 위, 총구 끝 등)로 옮긴다
    ///
    /// 슬롯의 Z축이 총구 방향, Y축이 위를 보게 두는 게 규칙이다.
    /// </summary>
    public class AttachmentSocketTool : EditorWindow
    {
        private GameObject _weapon;
        private Vector2 _scroll;

        [MenuItem("Tools/KILL OR DEAD/부착물 슬롯 도구")]
        public static void Open()
        {
            var window = GetWindow<AttachmentSocketTool>("부착물 슬롯");
            window.minSize = new Vector2(380f, 420f);
            window._weapon = Selection.activeGameObject;
        }

        private void OnSelectionChange()
        {
            if (Selection.activeGameObject != null &&
                Selection.activeGameObject.GetComponentInParent<TacticalShooterWeapon>() != null)
            {
                _weapon = Selection.activeGameObject.GetComponentInParent<TacticalShooterWeapon>().gameObject;
                Repaint();
            }
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("부착물 슬롯 도구", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "슬롯의 Z축이 총구 방향, Y축이 위를 향하게 두세요.\n" +
                "생성 후 씬 뷰에서 실제 레일 위치로 옮기면 됩니다.\n" +
                "노란 상자 = 빈 슬롯, 파란 상자 = 부착물 장착됨",
                MessageType.Info);

            EditorGUILayout.Space();
            _weapon = (GameObject)EditorGUILayout.ObjectField("무기", _weapon, typeof(GameObject), true);

            if (_weapon == null)
            {
                EditorGUILayout.HelpBox("무기 오브젝트를 넣어주세요.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            var weaponComponent = _weapon.GetComponentInParent<TacticalShooterWeapon>();
            if (weaponComponent == null)
                EditorGUILayout.HelpBox("TacticalShooterWeapon이 없습니다. 무기 프리팹이 맞나요?", MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("일괄 생성", EditorStyles.boldLabel);

            if (GUILayout.Button("표준 슬롯 4종 생성 (상부레일/총구/총열하부/측면레일)", GUILayout.Height(30f)))
                CreateStandardSockets();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("개별 생성", EditorStyles.boldLabel);

            DrawCreateButton("상부 레일 (조준경/레이저/전술등)", "rail_top", new[]
            {
                AttachmentSlotType.Optic, AttachmentSlotType.Laser, AttachmentSlotType.Light
            });
            DrawCreateButton("총구 (소음기/소염기)", "muzzle", new[] { AttachmentSlotType.Muzzle });
            DrawCreateButton("총열하부 (전방그립/총검)", "under", new[] { AttachmentSlotType.UnderBarrel });
            DrawCreateButton("좌측 레일 (레이저/전술등)", "rail_left", new[]
            {
                AttachmentSlotType.Laser, AttachmentSlotType.Light
            });
            DrawCreateButton("우측 레일 (레이저/전술등)", "rail_right", new[]
            {
                AttachmentSlotType.Laser, AttachmentSlotType.Light
            });
            DrawCreateButton("탄창", "magazine", new[] { AttachmentSlotType.Magazine });
            DrawCreateButton("개머리판", "stock", new[] { AttachmentSlotType.Stock });

            EditorGUILayout.Space();
            DrawExistingSockets();

            EditorGUILayout.EndScrollView();
        }

        private void DrawCreateButton(string label, string key, AttachmentSlotType[] types)
        {
            if (GUILayout.Button(label))
                CreateSocket(label.Split('(')[0].Trim(), key, new List<AttachmentSlotType>(types));
        }

        private void DrawExistingSockets()
        {
            var sockets = _weapon.GetComponentsInChildren<AttachmentSocket>(true);
            EditorGUILayout.LabelField($"현재 슬롯 ({sockets.Length}개)", EditorStyles.boldLabel);

            if (sockets.Length == 0)
            {
                EditorGUILayout.LabelField("  아직 없음", EditorStyles.miniLabel);
                return;
            }

            foreach (var socket in sockets)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  {socket.DisplayName}  [{socket.SocketKey}]");
                if (GUILayout.Button("선택", GUILayout.Width(50f)))
                {
                    Selection.activeGameObject = socket.gameObject;
                    SceneView.lastActiveSceneView?.FrameSelected();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("WeaponAttachmentSystem 붙이기"))
                EnsureAttachmentSystem();
        }

        private void CreateStandardSockets()
        {
            CreateSocket("상부 레일", "rail_top",
                new List<AttachmentSlotType> { AttachmentSlotType.Optic, AttachmentSlotType.Laser, AttachmentSlotType.Light },
                new Vector3(0f, 0.05f, 0f));
            CreateSocket("총구", "muzzle",
                new List<AttachmentSlotType> { AttachmentSlotType.Muzzle },
                new Vector3(0f, 0f, 0.35f));
            CreateSocket("총열하부", "under",
                new List<AttachmentSlotType> { AttachmentSlotType.UnderBarrel },
                new Vector3(0f, -0.04f, 0.15f));
            CreateSocket("좌측 레일", "rail_left",
                new List<AttachmentSlotType> { AttachmentSlotType.Laser, AttachmentSlotType.Light },
                new Vector3(-0.03f, 0f, 0.15f));

            EnsureAttachmentSystem();
        }

        private void CreateSocket(string displayName, string key, List<AttachmentSlotType> types,
                                  Vector3 localPosition = default)
        {
            var parent = _weapon.transform;

            // 이미 같은 키가 있으면 만들지 않는다
            foreach (var existing in parent.GetComponentsInChildren<AttachmentSocket>(true))
            {
                if (existing.SocketKey != key) continue;
                Debug.LogWarning($"[슬롯 도구] '{key}' 슬롯이 이미 있습니다.", existing);
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            var go = new GameObject($"Socket_{key}");
            Undo.RegisterCreatedObjectUndo(go, "슬롯 생성");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;

            var socket = go.AddComponent<AttachmentSocket>();

            var so = new SerializedObject(socket);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("socketKey").stringValue = key;

            var typesProperty = so.FindProperty("acceptedTypes");
            typesProperty.ClearArray();
            for (int i = 0; i < types.Count; i++)
            {
                typesProperty.InsertArrayElementAtIndex(i);
                typesProperty.GetArrayElementAtIndex(i).enumValueIndex = (int)types[i];
            }
            so.ApplyModifiedProperties();

            Selection.activeGameObject = go;
            Debug.Log($"[슬롯 도구] '{displayName}' 슬롯 생성. 씬 뷰에서 실제 위치로 옮겨주세요.", go);
        }

        private void EnsureAttachmentSystem()
        {
            var weaponComponent = _weapon.GetComponentInParent<TacticalShooterWeapon>();
            if (weaponComponent == null) return;

            if (weaponComponent.GetComponent<WeaponAttachmentSystem>() != null)
            {
                Debug.Log("[슬롯 도구] WeaponAttachmentSystem이 이미 붙어 있습니다.", weaponComponent);
                return;
            }

            Undo.AddComponent<WeaponAttachmentSystem>(weaponComponent.gameObject);
            Debug.Log("[슬롯 도구] WeaponAttachmentSystem 추가", weaponComponent);
        }
    }
}
#endif
