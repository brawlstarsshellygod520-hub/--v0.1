using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    // Step 1 is deliberately a scene scaffold, not a second gameplay implementation.
    public static class TutorialGreyboxSetup
    {
        public const string ScenePath = "Assets/FireRescue/Scenes/TutorialGreybox.unity";
        const string OriginalScenePath = "Assets/FireRescue/Scenes/ResidentialFire.unity";
        const string ReportPath = "Docs/Validation/2026-10-12-greybox-step1/scene-validation.txt";
        static readonly string[] Sections = { "Environment", "Actors", "Sources", "Objectives", "Presentation" };
        static double readyAt;

        [MenuItem("FireRescue/Tutorial/建立或開啟 Greybox 教學場景（不覆寫）")]
        public static void Open()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("請先退出 Play Mode，再開啟教學場景。");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureCreated();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // Only called after the menu's save prompt, or in a disposable batch session.
        // Existing scenes, including future manual edits, are never regenerated.
        static bool EnsureCreated()
        {
            if (File.Exists(ScenePath)) return false;
            // Batch startup has an untitled scene; Unity disallows additive creation there.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("TutorialGreybox").transform;
            foreach (var section in Sections)
            {
                var child = new GameObject(section).transform;
                child.SetParent(root, false);
            }

            var presentation = root.Find("Presentation");
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(presentation, false);
            cameraObject.transform.rotation = Quaternion.Euler(48f, 40f, 0f);
            cameraObject.transform.position = Vector3.up + cameraObject.transform.rotation * (Vector3.back * 18f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.34f, .36f, .39f);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 60f;

            var lightObject = new GameObject("Directional Light", typeof(Light));
            lightObject.transform.SetParent(presentation, false);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.shadows = LightShadows.Soft;

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("無法保存新的 TutorialGreybox 場景。");
            AssetDatabase.ImportAsset(ScenePath);
            return true;
        }

        // Batch entry point: compilation/import must settle before scene inspection.
        public static void BatchCreateAndValidate()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("此入口僅供 Unity batchmode 使用。");
            readyAt = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update += BeginBatch;
        }

        static void BeginBatch()
        {
            if (EditorApplication.timeSinceStartup < readyAt || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= BeginBatch;
            var report = new StringBuilder();
            var count = 0;
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
                report.AppendLine("PASS " + (++count) + " " + message);
            }

            try
            {
                report.AppendLine("Unity " + Application.unityVersion + "; target " + EditorUserBuildSettings.activeBuildTarget);
                report.AppendLine("Scope: step 1 scaffold only; no player, tools or tutorial gameplay.");
                var buildScenes = BuildScenesFingerprint();
                var originalHash = Hash(OriginalScenePath);
                var originalMetaHash = Hash(OriginalScenePath + ".meta");
                var created = EnsureCreated();
                report.AppendLine("Created new scene: " + created);
                Check(File.Exists(ScenePath), "TutorialGreybox scene exists");
                Check(File.Exists(ScenePath + ".meta"), "Unity-generated scene metadata exists");
                Check(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(ScenePath)), "Tutorial scene GUID resolves");

                var savedHash = Hash(ScenePath);
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var roots = scene.GetRootGameObjects();
                Check(roots.Length == 1 && roots[0].name == "TutorialGreybox", "Independent scene root");
                var root = roots[0].transform;
                Check(Sections.All(section => root.Find(section)), "All five organizational sections exist");
                Check(Sections.Where(section => section != "Presentation").All(section => root.Find(section).childCount == 0), "Gameplay and map sections remain empty for later phases");
                var components = roots.SelectMany(go => go.GetComponentsInChildren<Component>(true)).ToArray();
                Check(components.All(component => component), "Tutorial scene has no missing scripts");
                Check(!components.OfType<MonoBehaviour>().Any(), "No premature gameplay or input components");
                var cameras = components.OfType<Camera>().ToArray();
                Check(cameras.Length == 1 && cameras[0].CompareTag("MainCamera") && cameras[0].enabled, "One active MainCamera");
                Check(cameras[0].orthographic && Mathf.Abs(cameras[0].transform.eulerAngles.x - 48f) < .01f, "Angled orthographic camera scaffold");
                Check(components.OfType<AudioListener>().Count() == 1 && components.OfType<Light>().Count() == 1, "One listener and one light");
                Check(!AssetDatabase.GetDependencies(ScenePath).Any(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)), "Scaffold does not depend on formal models or prefabs");
                Check(!EnsureCreated() && Hash(ScenePath) == savedHash, "Repeated creation preserves existing scene bytes");

                var original = EditorSceneManager.OpenScene(OriginalScenePath, OpenSceneMode.Single);
                Check(original.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Component>(true)).All(component => component), "Original ResidentialFire loads without missing scripts");
                Check(Hash(OriginalScenePath) == originalHash && Hash(OriginalScenePath + ".meta") == originalMetaHash, "Original scene and GUID metadata unchanged");
                Check(BuildScenesFingerprint() == buildScenes, "Build scene order and enabled flags unchanged");
                report.AppendLine("TUTORIAL_STEP1_PASS checks=" + count);
                WriteReport(report);
                Debug.Log(report.ToString());
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                report.AppendLine("TUTORIAL_STEP1_FAIL " + exception.Message);
                WriteReport(report);
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        static string BuildScenesFingerprint() => string.Join("\n", EditorBuildSettings.scenes.Select(scene => scene.path + "|" + scene.enabled));
        static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream));
        }
        static void WriteReport(StringBuilder report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, report.ToString());
        }
    }
}
