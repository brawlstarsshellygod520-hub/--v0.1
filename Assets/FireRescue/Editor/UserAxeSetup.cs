using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    // Only presentation is replaced. The existing pivot, input and strike query are unchanged.
    public static class UserAxeSetup
    {
        public const string Folder = "Assets/FireRescue/Art/UserProvided/Tools/FireAxe";
        public const string ModelPath = Folder + "/fire_axe.fbx";
        public const string PrefabPath = Folder + "/FireAxe.prefab";
        public const string ModelName = "Fire Axe Model";
        public const string Evidence = "Docs/Validation/2026-10-21-axe-powder";
        static double readyAt;

        [MenuItem("FireRescue/Greybox/接入使用者消防斧（保留既有配置）")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            InstallScene();
        }

        public static void BatchInstallAndValidate()
        {
            readyAt = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update += Batch;
        }

        static void Batch()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < readyAt) return;
            EditorApplication.update -= Batch;
            try
            {
                InstallScene();
                string sceneText = File.ReadAllText(TutorialGreyboxSetup.ScenePath);
                string prefabText = File.ReadAllText(PrefabPath);
                InstallScene();
                if (sceneText != File.ReadAllText(TutorialGreyboxSetup.ScenePath) || prefabText != File.ReadAllText(PrefabPath))
                    throw new Exception("Repeated install overwrote the scene or prefab.");
                var layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
                var player = layout.GetComponentInChildren<FirefighterTools>();
                var pivot = player.transform.Find("Visual/Axe Pivot");
                var model = pivot.Find(ModelName);
                if (!model || !model.gameObject.activeSelf || pivot.Find("Handle").gameObject.activeSelf || pivot.Find("Head").gameObject.activeSelf)
                    throw new Exception("Persisted imported axe/greybox visibility is incorrect.");
                ValidatePresentation(model.gameObject);
                ValidateBladeDirection(model.gameObject);
                var view = player.GetComponentInChildren<FirefighterToolsView>(true);
                if (new SerializedObject(view).FindProperty("axePivot").objectReferenceValue != pivot)
                    throw new Exception("The existing swing pivot reference was lost.");
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence + "/axe-install.txt", "USER_AXE_INSTALL_PASS\nUnity " + Application.unityVersion +
                    "\nExisting gameplay serialization unchanged; imported model only; no model physics; saved scene reload; idempotent install PASS\n" +
                    "Model world bounds " + ModelBounds(model.gameObject) + "\n");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        public static void InstallScene()
        {
            EnsurePrefab();
            var scene = EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            var layout = scene.GetRootGameObjects().Single(g => g.name == "TutorialGreybox").transform.Find(UserLevelOneBuilder.LayoutName);
            if (!layout || !layout.gameObject.activeSelf) throw new Exception("User residential layout is not active.");
            var player = layout.GetComponentInChildren<FirefighterTools>();
            var pivot = player ? player.transform.Find("Visual/Axe Pivot") : null;
            if (!pivot) throw new Exception("Existing axe presentation pivot is missing.");
            if (pivot.Find(ModelName)) return;
            var oldHandle = pivot.Find("Handle");
            var oldHead = pivot.Find("Head");
            if (!oldHandle || !oldHead) throw new Exception("Axe presentation has changed; preserve it instead of guessing a replacement.");
            string before = GameplaySnapshot(layout.gameObject);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pivot);
            model.name = ModelName;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            if (!Application.isBatchMode)
            {
                Undo.RegisterCreatedObjectUndo(model, "Insert user axe model");
                Undo.RecordObjects(new UnityEngine.Object[] { oldHandle.gameObject, oldHead.gameObject }, "Keep primitive axe as disabled backup");
            }
            oldHandle.gameObject.SetActive(false);
            oldHead.gameObject.SetActive(false);
            ValidatePresentation(model);
            if (GameplaySnapshot(layout.gameObject) != before)
                throw new Exception("Model replacement unexpectedly changed gameplay or physics serialization; scene was not saved.");
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save axe presentation.");
        }

        static void EnsurePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) return;
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (!importer) throw new FileNotFoundException("Export and copy the user axe FBX first.", ModelPath);
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.bakeAxisConversion = true;
            importer.globalScale = 1;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            var root = new GameObject("FireAxe");
            try
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
                model.name = "User Model";
                ValidatePresentation(root);
                if (!PrefabUtility.SaveAsPrefabAsset(root, PrefabPath)) throw new IOException("Could not save fire axe prefab.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        public static Bounds ModelBounds(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new Exception("Axe contains no renderers.");
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        static void ValidatePresentation(GameObject model)
        {
            if (model.GetComponentsInChildren<Collider>(true).Length != 0 || model.GetComponentsInChildren<Rigidbody>(true).Length != 0 ||
                model.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                throw new Exception("Axe model must be presentation only.");
            Bounds bounds = ModelBounds(model);
            if (bounds.size.y < .65f || bounds.size.y > .8f) throw new Exception("Unexpected imported axe size: " + bounds.size);
        }

        static void ValidateBladeDirection(GameObject model)
        {
            // Source-specific import QA only, never gameplay coupling: material .005 is
            // the broad cutting edge in this user's six-part model, .006 includes the spike.
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var materials = filter.GetComponent<Renderer>().sharedMaterials;
                var mesh = filter.sharedMesh;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i].name != "材質.005") continue;
                    var vertices = mesh.vertices;
                    var indices = mesh.GetTriangles(i);
                    float front = indices.Average(index => model.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[index])).z);
                    if (front < .05f) throw new Exception("The cutting edge points backwards after FBX conversion: " + front);
                    return;
                }
            }
            throw new Exception("Cannot identify source cutting-edge material for orientation QA.");
        }

        static string GameplaySnapshot(GameObject layout)
        {
            var output = new StringBuilder();
            foreach (var component in layout.GetComponentsInChildren<Component>(true))
            {
                if (component is MonoBehaviour || component is Collider || component is Rigidbody)
                    output.Append(component.GetEntityId().ToString()).Append(':').Append(EditorJsonUtility.ToJson(component)).AppendLine();
            }
            return output.ToString();
        }
    }
}
