using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    public static class DirectionalSprayBuilder
    {
        public const string Folder = "Assets/FireRescue/Settings/Sprays";
        public const string Evidence = "Docs/Validation/2026-10-26-directional-spray";
        const string Marker = "Directional Spray v1";

        [MenuItem("FireRescue/Spray/啟用方向噴射（僅首次）")]
        public static void InstallMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Install();
        }

        public static void BatchInstallAndValidate()
        {
            try
            {
                Install(); string first = File.ReadAllText(TutorialGreyboxSetup.ScenePath);
                Install();
                if (first != File.ReadAllText(TutorialGreyboxSetup.ScenePath)) throw new Exception("Installer overwrote the saved scene on repeat.");
                var layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
                var emitter = layout.GetComponentInChildren<SprayEmitter>();
                if (!emitter.UsesDirectionalSpray || !emitter.AutomaticSimulation) throw new Exception("Emitter mode did not survive reload.");
                var view = emitter.transform.Find("Visual").GetComponent<SprayParticleView>();
                if (!view || new SerializedObject(view).FindProperty("emitter").objectReferenceValue != emitter)
                    throw new Exception("Particle view lost its explicit gameplay reference.");
                var sources = layout.GetComponentsInChildren<FluidSource>();
                if (sources.Length != 3) throw new Exception("Unexpected active source count.");
                foreach (var source in sources) if (!source.Profile) throw new Exception("A source has no profile after save/reload.");
                if (emitter.Connection.MaximumLength != 3) throw new Exception("Hose length changed.");
                var occlusion = layout.GetComponent<SprayWallOcclusion>();
                if (!occlusion || new SerializedObject(emitter).FindProperty("wallOcclusion").objectReferenceValue != occlusion)
                    throw new Exception("Cutaway wall occlusion binding is missing.");
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence + "/install.txt", "DIRECTIONAL_SPRAY_INSTALL_PASS\nUnity " + Application.unityVersion +
                    "\nThree source profiles, directional mode, read-only particle view, 3m hose, save/reload, idempotence PASS\n");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void Install()
        {
            EnsureFolder("Assets/FireRescue/Settings"); EnsureFolder(Folder);
            var scene = EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            var layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
            if (!layout || !layout.activeInHierarchy) throw new Exception("Current residential layout is missing.");
            if (layout.transform.Find(Marker))
            {
                bool changed = InstallOcclusion(layout) | InstallWindowDepth(layout);
                if (changed) { AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene); }
                return;
            }
            foreach (var source in layout.GetComponentsInChildren<FluidSource>())
            {
                string path = Folder + "/" + source.Element + ".asset";
                var profile = AssetDatabase.LoadAssetAtPath<SprayProfile>(path);
                if (!profile)
                {
                    profile = ScriptableObject.CreateInstance<SprayProfile>();
                    profile.Configure(SpraySettings.DefaultFor(source.Element)); AssetDatabase.CreateAsset(profile, path);
                }
                Undo.RecordObject(source, "Assign spray profile"); source.SetSprayProfile(profile);
                EditorUtility.SetDirty(source); PrefabUtility.RecordPrefabInstancePropertyModifications(source);
            }
            var emitter = layout.GetComponentInChildren<SprayEmitter>();
            Undo.RecordObject(emitter, "Enable directional spray");
            emitter.EnableDirectionalSpray(true); emitter.SetGroundHeight(layout.GetComponentInChildren<FireField>().Origin.y);
            emitter.AutomaticSimulation = true;
            EditorUtility.SetDirty(emitter); PrefabUtility.RecordPrefabInstancePropertyModifications(emitter);
            var visual = emitter.transform.Find("Visual");
            var view = visual.GetComponent<SprayParticleView>() ?? Undo.AddComponent<SprayParticleView>(visual.gameObject);
            view.Initialize(emitter); EditorUtility.SetDirty(view); PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            var marker = new GameObject(Marker); marker.transform.SetParent(layout.transform, false);
            Undo.RegisterCreatedObjectUndo(marker, "Directional spray migration marker");
            InstallOcclusion(layout);
            InstallWindowDepth(layout);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save directional spray wiring.");
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'), Path.GetFileName(path));
        }

        static bool InstallOcclusion(GameObject layout)
        {
            if (layout.GetComponent<SprayWallOcclusion>()) return false;
            var occlusion = Undo.AddComponent<SprayWallOcclusion>(layout);
            // Explicit gameplay geometry only, never furniture or replaceable visuals.
            var walls = layout.transform.Find("Imported Architecture").GetComponentsInChildren<Collider>(true).ToList();
            foreach (var door in layout.GetComponentsInChildren<BreakableDoor>(true)) walls.Add(door.BlockingCollider);
            occlusion.Configure(walls.Distinct().ToArray(), layout.GetComponentsInChildren<HoseWindow>(true));
            var emitter = layout.GetComponentInChildren<SprayEmitter>();
            Undo.RecordObject(emitter, "Bind spray wall occlusion"); emitter.SetWallOcclusion(occlusion);
            EditorUtility.SetDirty(occlusion); EditorUtility.SetDirty(emitter);
            PrefabUtility.RecordPrefabInstancePropertyModifications(emitter);
            return true;
        }

        static bool InstallWindowDepth(GameObject layout)
        {
            bool changed = false;
            foreach (var window in layout.GetComponentsInChildren<HoseWindow>(true))
            {
                var serialized = new SerializedObject(window);
                var size = serialized.FindProperty("sprayOpeningSize");
                if (size.prefabOverride) continue; // Never overwrite a later Inspector tuning.
                // Frame lies on the outside face; the imported inside wall is 0.353m from it.
                size.vector3Value = new Vector3(1f, .61f, 1.1f);
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(window); PrefabUtility.RecordPrefabInstancePropertyModifications(window);
                changed = true;
            }
            return changed;
        }

        public static void BatchBuildWindows()
        {
            const string output = "Builds/DirectionalSpray/FireRescue.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { TutorialGreyboxSetup.ScenePath },
                locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence + "/build.txt", report.summary.result + "\nErrors=" + report.summary.totalErrors +
                " Warnings=" + report.summary.totalWarnings + "\n" + output);
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
