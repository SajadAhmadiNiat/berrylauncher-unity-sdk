#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif

namespace BerryShooter.EditorTools
{
    [InitializeOnLoad]
    public static class ShooterSetup
    {
        private const string ScenePath = "Assets/BerryShooter/Scenes/Main.unity";
        private const string IconPath = "Assets/BerryShooter/Resources/berry-icon.png";
        private const string ShaderPath = "Assets/BerryShooter/Resources/BSFlat.shader";
        private const string MaterialDir = "Assets/BerryShooter/Materials";
        private const string DoneKey = "BerryShooter.SetupDone.2.";

        static ShooterSetup()
        {
            EditorApplication.delayCall += () =>
            {
                string key = DoneKey + Application.dataPath;
                if (EditorPrefs.GetBool(key, false)) return;
                ApplySettings(false);
                EditorPrefs.SetBool(key, true);
            };
        }

        [MenuItem("Berry Shooter/1. Setup project settings")]
        public static void SetupMenu() { ApplySettings(true); }

        private static void ApplySettings(bool verbose)
        {
            PlayerSettings.companyName = "Berry";
            PlayerSettings.productName = "BerryShooter";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultIsNativeResolution = true;
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null)
            {
#if UNITY_2021_2_OR_NEWER
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
#else
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
#endif
            }
            AddAlwaysIncludedShader();
            EnsureSceneInBuild();
            if (verbose) EditorUtility.DisplayDialog("Berry Shooter", "Project settings are set.\n\nIf the game reads no keys later: Edit > Project Settings > Player > Other Settings > 'Active Input Handling' must be 'Input Manager (Old)' or 'Both'.", "OK");
        }

        private static void AddAlwaysIncludedShader()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null) return;
            UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (all == null || all.Length == 0) return;
            var graphics = all[0];
            var so = new SerializedObject(graphics);
            SerializedProperty list = so.FindProperty("m_AlwaysIncludedShaders");
            if (list == null) return;
            for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            so.ApplyModifiedProperties();
        }

        private static void EnsureSceneInBuild()
        {
            if (!File.Exists(ScenePath)) return;
            foreach (var s in EditorBuildSettings.scenes) if (s.path == ScenePath) return;
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes) { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static readonly Dictionary<string, Material> assetMaterials = new Dictionary<string, Material>();

        private static Material AssetMaterial(Color c, bool glow)
        {
            string hex = ColorUtility.ToHtmlStringRGB(c) + (glow ? "_glow" : "");
            Material m;
            if (assetMaterials.TryGetValue(hex, out m) && m != null) return m;
            Directory.CreateDirectory(MaterialDir);
            string path = MaterialDir + "/BS_" + hex + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
                if (shader == null) shader = Shader.Find("BerryShooter/Flat");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                m = new Material(shader);
                m.color = c;
                if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                if (m.HasProperty("_Emission")) m.SetFloat("_Emission", glow ? 1f : 0f);
                AssetDatabase.CreateAsset(m, path);
            }
            assetMaterials[hex] = m;
            return m;
        }

        [MenuItem("Berry Shooter/2. Create the full main scene (arena, light, camera, game object)")]
        public static void CreateScene()
        {
            if (!EditorUtility.DisplayDialog("Berry Shooter", "This builds the whole scene (arena floor, walls, cover blocks, grid, 8 spawn points, light, camera) and overwrites " + ScenePath + ". Continue?", "Build", "Cancel")) return;
            ApplySettings(false);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            assetMaterials.Clear();
            Transform arena = ArenaFactory.Build(null, AssetMaterial, true);
            arena.gameObject.isStatic = true;
            foreach (Transform t in arena) t.gameObject.isStatic = true;
            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0, 14, -30);
            cam.transform.rotation = Quaternion.Euler(24, 0, 0);
            var camera = cam.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.08f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 300f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.43f, 0.5f);
            RenderSettings.fog = false;
            var go = new GameObject("Berry Shooter");
            go.AddComponent<Bootstrap>();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            EnsureSceneInBuild();
            EditorUtility.DisplayDialog("Berry Shooter", "Scene saved: " + ScenePath + "\n\nPress Play to test it.", "OK");
        }

        [MenuItem("Berry Shooter/3. Build Windows (x64) + copy berry-game.json")]
        public static void Build()
        {
            ApplySettings(false);
            string root = Directory.GetParent(Application.dataPath).FullName;
            string outDir = Path.Combine(root, "Builds", "BerryShooter");
            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(outDir, "BerryShooter.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            string manifest = Path.Combine(root, "berry-game.json");
            if (File.Exists(manifest)) File.Copy(manifest, Path.Combine(outDir, "berry-game.json"), true);
            EditorUtility.RevealInFinder(outDir);
            Debug.Log("[BerryShooter] build: " + report.summary.result + " -> " + outDir);
        }

        [MenuItem("Berry Shooter/Open build folder")]
        public static void OpenBuild()
        {
            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds");
            if (Directory.Exists(dir)) EditorUtility.RevealInFinder(dir);
        }
    }
}
#endif
