using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    // Scene authoring only. The existing FireField, hose and reaction modules own gameplay.
    public static class WaterGameplayBuilder
    {
        public const string LessonName = "Water Gameplay Test";
        public const string Evidence = "Docs/Validation/2026-10-21-water-gameplay";
        public static readonly Vector3[] Fires =
        {
            new Vector3(2.25f, 0, .75f), new Vector3(2.75f, 0, .75f), new Vector3(2.25f, 0, 1.25f)
        };
        public static readonly Vector3 PushablePosition = new Vector3(3.5f, 0, -2.5f);
        static double readyAt;

        [MenuItem("FireRescue/Greybox/配置水機制測試（僅首次）")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            InstallScene();
        }

        public static void BatchInstallAndValidate()
        { readyAt = EditorApplication.timeSinceStartup + 2; EditorApplication.update += Batch; }

        static void Batch()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < readyAt) return;
            EditorApplication.update -= Batch;
            try
            {
                InstallScene();
                string saved = File.ReadAllText(TutorialGreyboxSetup.ScenePath);
                string savedPrefab = File.ReadAllText(GreyboxPrefabBuilder.PrefabPath("FireSystem"));
                InstallScene(); // Re-open from disk; the marker makes this non-destructive.
                if (File.ReadAllText(TutorialGreyboxSetup.ScenePath) != saved) throw new Exception("Repeated install changed the scene.");
                if (File.ReadAllText(GreyboxPrefabBuilder.PrefabPath("FireSystem")) != savedPrefab)
                    throw new Exception("Repeated install changed the fire prefab.");
                var layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
                var field = layout.GetComponentInChildren<FireField>();
                if (field.BurningCount != Fires.Length) throw new Exception("Initial fire seeds did not survive scene save/reload.");
                foreach (var point in Fires)
                {
                    if (!field.TryGetCell(point, out var cell) || !field.GetCell(cell.x, cell.y).IsBurning)
                        throw new Exception("Lesson fire cell is missing or nonflammable: " + point);
                }
                var table = layout.transform.Find("Household Props/Coffee table");
                if (!table || Vector3.Distance(table.position, PushablePosition) > .001f || table.GetComponent<Rigidbody>().isKinematic)
                    throw new Exception("Pushable lesson furniture is not configured.");
                var parameters = new SerializedObject(field);
                if (Mathf.Abs(parameters.FindProperty("fireSpreadRate").floatValue - .01f) > .00001f)
                    throw new Exception("Slow-spread setting was not saved.");
                if (new SerializedObject(field.GetComponent<ElementReaction>()).FindProperty("waterPushImpulsePerUnit").floatValue != 48f)
                    throw new Exception("Authored water push tuning did not survive prefab reload.");
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence + "/install.txt", "WATER_GAMEPLAY_INSTALL_PASS\nUnity " + Application.unityVersion +
                    "\nThree flammable ignition seeds; movable furniture; slow-spread parameters; water push 48; persisted reload; scene/prefab idempotent install PASS\n");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        public static void InstallScene()
        {
            InitializeWaterPushTuning();
            var scene = EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            var layout = scene.GetRootGameObjects().Single(g => g.name == "TutorialGreybox").transform.Find(UserLevelOneBuilder.LayoutName);
            if (!layout || !layout.gameObject.activeSelf) throw new Exception("Install the user's Step4 layout first.");
            if (layout.Find(LessonName)) return;
            var field = layout.GetComponentInChildren<FireField>();
            var table = layout.Find("Household Props/Coffee table");
            if (!field || !table || !table.GetComponent<Rigidbody>()) throw new Exception("Existing fire/pushable wiring is missing.");
            if (!Application.isBatchMode)
            {
                Undo.RecordObject(field, "Configure water lesson fire");
                Undo.RecordObject(table, "Position water lesson furniture");
            }
            var seeds = new Vector2Int[Fires.Length];
            for (int i = 0; i < Fires.Length; i++)
            {
                if (!field.TryGetCell(Fires[i], out seeds[i]) || field.GetCell(seeds[i].x, seeds[i].y).Fuel <= 0)
                    throw new Exception("Do not place the lesson fire inside a fixed obstacle.");
            }
            field.SetIgnitionSeeds(seeds);
            var settings = new SerializedObject(field);
            // Allow time to explore/connect while retaining gradual spread. No tutorial state.
            settings.FindProperty("fireSpreadRate").floatValue = .01f;
            settings.FindProperty("fuelBurnRate").floatValue = .01f;
            settings.FindProperty("naturalIntensityLoss").floatValue = .01f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            field.ResetSimulation();
            table.localPosition = PushablePosition;
            PrefabUtility.RecordPrefabInstancePropertyModifications(field);
            PrefabUtility.RecordPrefabInstancePropertyModifications(table);
            var lesson = new GameObject(LessonName);
            lesson.transform.SetParent(layout, false);
            for (int i = 0; i < Fires.Length; i++)
            {
                var marker = new GameObject("Fire seed " + (i + 1));
                marker.transform.SetParent(lesson.transform, false);
                marker.transform.localPosition = Fires[i]; // Authoring only, no runtime/world text.
            }
            if (!Application.isBatchMode) Undo.RegisterCreatedObjectUndo(lesson, "Create water lesson markers");
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Water lesson scene could not be saved.");
        }

        static void InitializeWaterPushTuning()
        {
            string path = GreyboxPrefabBuilder.PrefabPath("FireSystem");
            // New serialized fields can retain their first imported default in Unity's cache.
            // Author this new setting once, but never overwrite a value the user has saved.
            if (File.ReadAllText(path).Contains("waterPushImpulsePerUnit:")) return;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var reaction = root.GetComponent<ElementReaction>();
                if (!reaction) throw new Exception("FireSystem prefab is missing ElementReaction.");
                var settings = new SerializedObject(reaction);
                settings.FindProperty("waterPushImpulsePerUnit").floatValue = 48f;
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
