// [KILL OR DEAD] Editor Tool
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KillOrDead.EditorTools
{
    /// <summary>
    /// "Missing script" 때문에 프리팹 저장이 막힐 때 쓰는 도구.
    ///
    /// 유니티는 스크립트가 깨진 컴포넌트가 하나라도 있으면 프리팹 저장을 거부한다.
    /// 그런데 인스펙터에서 그 컴포넌트를 지우는 게 은근히 까다로워서, 여기서 한 번에 처리한다.
    ///
    /// 메뉴 > Tools > KILL OR DEAD > Missing 스크립트 정리
    /// </summary>
    public class MissingScriptCleaner : EditorWindow
    {
        private Vector2 _scroll;
        private readonly List<GameObject> _found = new List<GameObject>();
        private string _report = "아직 검사하지 않았습니다.";

        [MenuItem("Tools/KILL OR DEAD/Missing 스크립트 정리")]
        public static void Open()
        {
            var window = GetWindow<MissingScriptCleaner>("Missing 스크립트");
            window.minSize = new Vector2(420f, 320f);
            window.Scan();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Missing 스크립트 정리", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "열려 있는 씬 전체에서 스크립트가 깨진 컴포넌트를 찾아 지웁니다.\n" +
                "프리팹 에셋을 고치려면 프리팹을 더블클릭해 프리팹 모드로 연 뒤 실행하세요.",
                MessageType.Info);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("검사", GUILayout.Height(28f))) Scan();

            using (new EditorGUI.DisabledScope(_found.Count == 0))
            {
                if (GUILayout.Button($"전부 제거 ({_found.Count}개)", GUILayout.Height(28f)))
                    RemoveAll();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField(_report, EditorStyles.wordWrappedLabel);

            foreach (var go in _found)
            {
                if (go == null) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("  " + GetPath(go.transform));
                if (GUILayout.Button("선택", GUILayout.Width(50f)))
                {
                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(go);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private void Scan()
        {
            _found.Clear();
            var builder = new StringBuilder();

            // 프리팹 모드로 열려 있으면 그 프리팹 안을, 아니면 열린 씬 전체를 검사한다.
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                builder.AppendLine($"프리팹 모드 검사 중: {stage.assetPath}");
                Collect(stage.prefabContentsRoot);
            }
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;

                    builder.AppendLine($"씬 검사 중: {scene.name}");
                    foreach (var root in scene.GetRootGameObjects()) Collect(root);
                }
            }

            builder.AppendLine();
            builder.AppendLine(_found.Count == 0
                ? "깨진 스크립트가 없습니다."
                : $"깨진 스크립트를 가진 오브젝트 {_found.Count}개를 찾았습니다:");

            _report = builder.ToString();
            Repaint();
        }

        private void Collect(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                if (count > 0) _found.Add(transform.gameObject);
            }
        }

        private void RemoveAll()
        {
            int total = 0;
            foreach (var go in _found)
            {
                if (go == null) continue;
                Undo.RegisterCompleteObjectUndo(go, "Missing 스크립트 제거");
                total += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                EditorUtility.SetDirty(go);
            }

            Debug.Log($"[Missing 스크립트 정리] 컴포넌트 {total}개 제거 완료. " +
                      $"이제 프리팹을 저장할 수 있습니다.");

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null)
                EditorSceneManager.MarkAllScenesDirty();

            Scan();
        }

        private static string GetPath(Transform t)
        {
            var path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + " / " + path;
            }
            return path;
        }
    }
}
#endif
