using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    // A focused scene migration. Does not reimport or rebuild the user's house model.
    public static class ResidentialIntegrationBuilder
    {
        public const string Marker = "Residential Integration v1";
        public const string Evidence = "Docs/Validation/2026-10-25-residential-integration";
        public const string BuildPath = "Builds/ResidentialPrototype/FireRescue.exe";

        public static void BatchInstallAndValidate()
        {
            try
            {
                Install();
                string saved = File.ReadAllText(TutorialGreyboxSetup.ScenePath);
                Install();
                if (saved != File.ReadAllText(TutorialGreyboxSetup.ScenePath)) throw new Exception("Install is not idempotent.");
                var layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
                var door = layout.GetComponentInChildren<BreakableDoor>();
                var hose = layout.GetComponentInChildren<HoseConnection>();
                if (!door || door.MaximumDurability != 3 || !door.BlockingCollider || hose.MaximumLength != 3)
                    throw new Exception("Door/hose serialized settings failed reload.");
                if (door.transform.Find("Visual").GetComponentsInChildren<Collider>(true).Length != 0)
                    throw new Exception("Door Visual contains gameplay collision.");
                var manager = layout.GetComponentInChildren<RescueGameManager>();
                if (new SerializedObject(manager).FindProperty("objectFires").arraySize != 2)
                    throw new Exception("Object fire bindings missing after reload.");
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence + "/install.txt", "RESIDENTIAL_INSTALL_PASS\nSaved/reloaded references, 3m limit, door collision separation, two object fires, idempotent install.\n");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        [MenuItem("FireRescue/Greybox/整合破門與3m關卡（僅首次）")]
        public static void InstallMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Install();
        }

        static void Install()
        {
            var scene = EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            Transform layout = GameObject.Find(UserLevelOneBuilder.LayoutName).transform;
            if (layout.Find(Marker)) return;
            var field = layout.GetComponentInChildren<FireField>();
            var npc = layout.GetComponentInChildren<RescueNpc>();
            var manager = layout.GetComponentInChildren<RescueGameManager>();
            var window = layout.GetComponentInChildren<HoseWindow>();
            var fires = layout.GetComponentsInChildren<ObjectFire>();
            var ordinary = fires.Single(f => f.Kind == FireKind.Ordinary);
            var originalDoor = layout.Find("Imported Architecture/Door.004");
            if (!originalDoor || fires.Length != 2) throw new Exception("Unexpected source scene; stop migration.");
            var setup = Child(layout, Marker);
            originalDoor.gameObject.SetActive(false); // Preserve the imported open leaf; never edit its mesh.
            PrefabUtility.RecordPrefabInstancePropertyModifications(originalDoor.gameObject);
            var doorRoot = Child(setup, "Breakable bathroom door", new Vector3(-1.285f, 0, 2.403f));
            doorRoot.gameObject.layer = 8;
            var collider = doorRoot.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, .675f, 0); collider.size = new Vector3(.12f, 1.35f, 1.22f);
            var door = doorRoot.gameObject.AddComponent<BreakableDoor>(); door.Configure(3, collider);
            var visual = Child(doorRoot, "Visual");
            var intact = GameObject.CreatePrimitive(PrimitiveType.Cube);
            intact.name = "Intact panel"; intact.transform.SetParent(visual, false);
            intact.transform.localPosition = collider.center; intact.transform.localScale = collider.size;
            UnityEngine.Object.DestroyImmediate(intact.GetComponent<Collider>());
            intact.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(WindowHoseBuilder.Folder + "/TestWood.mat");
            visual.gameObject.AddComponent<BreakableDoorView>().Initialize(door, intact, null);

            // 2.04m source-to-window prefix + about .8m to the player. No hose length increase.
            Move(window.transform, new Vector3(-5.2f, 0, 3.6f));
            window.transform.rotation = Quaternion.Euler(0, 90, 0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(window.transform);
            Move(ordinary.transform, new Vector3(-5.2f, 0, 4.8f));
            Move(npc.transform, new Vector3(-6.25f, 0, 2.25f));

            var seeds = WaterGameplayBuilder.Fires.Select(p => { field.TryGetCell(p, out var c); return c; }).ToList();
            for (float z = 1.25f; z <= 3.75f; z += .5f)
            {
                var point = new Vector3(-4.25f, 0, z);
                if (field.TryGetCell(point, out var c) && field.GetCell(c.x, c.y).Fuel > 0)
                { seeds.Add(c); Child(setup, "Escape route fire", point); }
            }
            field.SetIgnitionSeeds(seeds.Distinct().ToArray()); field.ResetSimulation();
            var exit = new SerializedObject(npc).FindProperty("exit").objectReferenceValue as Transform;
            npc.Initialize(field, exit, fires);
            manager.Initialize(field, new[] { npc }, fires);
            foreach (var component in new Component[] { field, npc, manager })
            { EditorUtility.SetDirty(component); PrefabUtility.RecordPrefabInstancePropertyModifications(component); }
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed.");
        }

        public static void BatchBuildWindows()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BuildPath));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { TutorialGreyboxSetup.ScenePath },
                locationPathName = BuildPath, target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence + "/build.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors +
                "\nWarnings: " + report.summary.totalWarnings + "\nBytes: " + report.summary.totalSize + "\n" + Path.GetFullPath(BuildPath));
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        static void Move(Transform target, Vector3 position)
        { target.position = position; PrefabUtility.RecordPrefabInstancePropertyModifications(target); }
        static Transform Child(Transform parent, string name, Vector3 position = default)
        { var child = new GameObject(name).transform; child.SetParent(parent, false); child.localPosition = position; return child; }
    }
}
