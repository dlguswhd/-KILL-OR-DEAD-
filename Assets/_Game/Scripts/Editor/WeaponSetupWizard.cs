// [KILL OR DEAD] Editor Tool
#if UNITY_EDITOR
using System.IO;
using KillOrDead.Combat;
using KillOrDead.Recoil;
using KillOrDead.Weapons;
using UnityEditor;
using UnityEngine;

namespace KillOrDead.EditorTools
{
    /// <summary>
    /// AK105(주무기)와 R08(권총)의 데이터 에셋을 한 번에 만들어주는 마법사.
    ///
    /// 만드는 것:
    ///   - ImpactEffectLibrary (WarFX 프리팹을 이름으로 찾아서 자동 연결)
    ///   - WeaponDamageProfile x2
    ///   - RecoilPattern x2 (배틀그라운드식 수치로 세팅)
    ///
    /// 메뉴 > Tools > KILL OR DEAD > 무기 기본 에셋 생성 (AK105 / R08)
    /// </summary>
    public static class WeaponSetupWizard
    {
        private const string DataRoot = "Assets/_Game/Data";

        [MenuItem("Tools/KILL OR DEAD/무기 기본 에셋 생성 (AK105 + R08)")]
        public static void CreateAll()
        {
            _prefabPaths = null;
            EnsureFolder(DataRoot);
            EnsureFolder($"{DataRoot}/Weapons");
            EnsureFolder($"{DataRoot}/Recoil");

            var library = CreateImpactLibrary();
            CreateAk105();
            CreateR08();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[무기 마법사] 에셋 생성 완료. Assets/_Game/Data/ 를 확인하세요.\n" +
                      "이제 무기 프리팹에 WeaponBallistics를 붙이고 Profile / Recoil Pattern / " +
                      "Impact Library를 연결하면 됩니다.", library);
            Selection.activeObject = library;
            EditorGUIUtility.PingObject(library);
        }

        // ── 임팩트 라이브러리 ─────────────────────────────────────────
        private static ImpactEffectLibrary CreateImpactLibrary()
        {
            string path = $"{DataRoot}/ImpactEffectLibrary.asset";
            var library = AssetDatabase.LoadAssetAtPath<ImpactEffectLibrary>(path);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<ImpactEffectLibrary>();
                AssetDatabase.CreateAsset(library, path);
            }

            var so = new SerializedObject(library);
            var entries = so.FindProperty("entries");
            entries.ClearArray();

            // WarFX의 Unlit 탄흔 프리팹 우선, 없으면 일반 임팩트로 대체.
            // Lit 버전은 built-in 서피스 셰이더라 URP에서 분홍색이 되므로 쓰지 않는다.
            AddEntry(entries, 0, SurfaceType.Concrete,
                "WFX_BImpact Concrete + Hole Unlit", "WFX_BImpact Concrete");
            AddEntry(entries, 1, SurfaceType.Metal,
                "WFX_BImpact Metal + Hole Unlit", "WFX_BImpact Metal");
            AddEntry(entries, 2, SurfaceType.Wood,
                "WFX_BImpact Wood + Hole Unlit", "WFX_BImpact Wood");
            AddEntry(entries, 3, SurfaceType.Dirt,
                "WFX_BImpact Dirt + Hole", "WFX_BImpact Dirt");
            AddEntry(entries, 4, SurfaceType.Sand,
                "WFX_BImpact Sand + Hole", "WFX_BImpact Sand");
            AddEntry(entries, 5, SurfaceType.Flesh,
                "WFX_BImpact SoftBody", "WFX_BImpact SoftBody");

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(library);
            return library;
        }

        private static void AddEntry(SerializedProperty array, int index, SurfaceType surface,
                                     string preferredPrefab, string fallbackPrefab)
        {
            array.InsertArrayElementAtIndex(index);
            var element = array.GetArrayElementAtIndex(index);

            element.FindPropertyRelative("surface").enumValueIndex = (int)surface;

            var prefab = FindPrefab(preferredPrefab) ?? FindPrefab(fallbackPrefab);
            element.FindPropertyRelative("effectPrefab").objectReferenceValue = prefab;
            element.FindPropertyRelative("decalPrefab").objectReferenceValue = null;

            if (prefab == null)
                Debug.LogWarning($"[무기 마법사] '{preferredPrefab}' 프리팹을 못 찾았습니다. " +
                                 $"{surface} 항목을 직접 연결해주세요.");
        }

        private static System.Collections.Generic.Dictionary<string, string> _prefabPaths;

        /// <summary>
        /// WarFX 프리팹을 이름으로 찾는다.
        /// 이름에 '+'와 공백이 섞여 있어 검색어로 바로 넣으면 잘 안 걸리므로,
        /// 접두사로 한 번에 긁어온 뒤 파일명으로 정확히 대조한다.
        /// </summary>
        private static GameObject FindPrefab(string exactName)
        {
            if (_prefabPaths == null)
            {
                _prefabPaths = new System.Collections.Generic.Dictionary<string, string>();
                foreach (var guid in AssetDatabase.FindAssets("WFX_BImpact t:Prefab"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = Path.GetFileNameWithoutExtension(path);
                    // 모바일(WFXMR_) 버전은 접두사가 달라 자동으로 걸러진다.
                    if (!_prefabPaths.ContainsKey(fileName)) _prefabPaths[fileName] = path;
                }
            }

            return _prefabPaths.TryGetValue(exactName, out var found)
                ? AssetDatabase.LoadAssetAtPath<GameObject>(found)
                : null;
        }

        // ── AK105 (주무기) ───────────────────────────────────────────
        private static void CreateAk105()
        {
            var profile = CreateOrLoad<WeaponDamageProfile>($"{DataRoot}/Weapons/DMG_AK105.asset");
            profile.weaponName = "AK105";
            profile.baseDamage = 100f;        // 적 HP 500 기준: 헤드샷 1발, 흉부 3발
            profile.pelletsPerShot = 1;
            profile.maxDistance = 300f;
            profile.damageFalloff = new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(0.4f, 1f), new Keyframe(1f, 0.55f));
            profile.hipSpreadDegrees = 3.0f;
            profile.aimSpreadDegrees = 0.15f;
            profile.spreadGrowthPerShot = 0.35f;
            profile.spreadRecoveryPerSecond = 7f;
            profile.impactForce = 25f;
            EditorUtility.SetDirty(profile);

            var recoil = CreateOrLoad<RecoilPattern>($"{DataRoot}/Recoil/RCL_AK105.asset");
            // AK 계열: 수직이 강하고, 중반부터 오른쪽으로 휜다
            recoil.verticalKick = 1.25f;
            recoil.verticalOverBurst = new AnimationCurve(
                new Keyframe(0f, 0.80f),
                new Keyframe(0.15f, 1.20f),
                new Keyframe(0.45f, 1.05f),
                new Keyframe(1f, 0.95f));
            recoil.horizontalKick = 0.40f;
            recoil.horizontalOverBurst = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.15f, -0.35f),
                new Keyframe(0.45f, 0.70f),
                new Keyframe(0.75f, 0.30f),
                new Keyframe(1f, 0.55f));
            recoil.horizontalRandomness = 0.22f;
            recoil.patternLength = 30;
            recoil.firstShotMultiplier = 1.25f;
            recoil.patternResetTime = 0.35f;
            recoil.kickSpeed = 95f;
            recoil.recoveryDelay = 0.14f;
            recoil.recoverySpeed = 13f;
            recoil.recoveryRatio = 0.75f;
            recoil.aimMultiplier = 0.7f;
            EditorUtility.SetDirty(recoil);
        }

        // ── R08 (권총) ───────────────────────────────────────────────
        private static void CreateR08()
        {
            var profile = CreateOrLoad<WeaponDamageProfile>($"{DataRoot}/Weapons/DMG_R08.asset");
            profile.weaponName = "R08";
            profile.baseDamage = 60f;         // 헤드샷 2발, 흉부 5발
            profile.pelletsPerShot = 1;
            profile.maxDistance = 100f;
            profile.damageFalloff = new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.45f));
            profile.hipSpreadDegrees = 3.5f;
            profile.aimSpreadDegrees = 0.3f;
            profile.spreadGrowthPerShot = 0.5f;
            profile.spreadRecoveryPerSecond = 9f;
            profile.impactForce = 18f;
            EditorUtility.SetDirty(profile);

            var recoil = CreateOrLoad<RecoilPattern>($"{DataRoot}/Recoil/RCL_R08.asset");
            // 권총: 한 발당 크게 튀지만 빠르게 되돌아온다
            recoil.verticalKick = 2.1f;
            recoil.verticalOverBurst = AnimationCurve.Constant(0f, 1f, 1f);
            recoil.horizontalKick = 0.30f;
            recoil.horizontalOverBurst = AnimationCurve.Constant(0f, 1f, 0f);
            recoil.horizontalRandomness = 0.35f;
            recoil.patternLength = 12;
            recoil.firstShotMultiplier = 1f;
            recoil.patternResetTime = 0.5f;
            recoil.kickSpeed = 130f;
            recoil.recoveryDelay = 0.08f;
            recoil.recoverySpeed = 22f;
            recoil.recoveryRatio = 0.9f;      // 권총은 거의 다 알아서 돌아온다
            recoil.aimMultiplier = 0.75f;
            EditorUtility.SetDirty(recoil);
        }

        // ── 유틸 ─────────────────────────────────────────────────────
        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
