using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    public static class UserNozzleSetup
    {
        public const string Folder = "Assets/FireRescue/Art/UserProvided/Tools/Nozzle";
        public const string ModelPath = Folder + "/nozzle.fbx";
        public const string PrefabPath = Folder + "/Nozzle.prefab";
        public const string Evidence = "Docs/Validation/2026-10-23-nozzle-controls";
        const string MetadataPath = "ArtSource/UserProvided/Nozzle/nozzle-metadata.json";
        static double readyAt;

        [Serializable]
        sealed class Metadata
        {
            public float[] muzzle;
            public float[] hoseInlet;
            public float lengthMeters;
            public int triangleCount;
            public string sourceSha256;
        }

        [MenuItem("FireRescue/Greybox/接入噴槍與3m管線（僅首次）")]
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
                string sceneText = File.ReadAllText(TutorialGreyboxSetup.ScenePath);
                string prefabText = File.ReadAllText(PrefabPath);
                InstallScene(); // Reload the saved scene and preserve any existing setup.
                if (sceneText != File.ReadAllText(TutorialGreyboxSetup.ScenePath) || prefabText != File.ReadAllText(PrefabPath))
                    throw new Exception("Repeated nozzle installation changed saved assets.");
                var layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
                var player = layout.GetComponentInChildren<FirefighterTools>();
                var mount = player.transform.Find("Visual/Nozzle Mount");
                var model = mount.Find("User Nozzle Model");
                ValidateModel(model.gameObject);
                var hose = player.GetComponent<HoseConnection>();
                var emitter = player.GetComponent<SprayEmitter>();
                var data = ReadMetadata();
                if (Mathf.Abs(hose.MaximumLength - 3) > .001f ||
                    Vector3.Distance(emitter.NozzlePosition, mount.TransformPoint(Point(data.muzzle))) > .001f ||
                    Vector3.Distance(emitter.HoseInletPosition, mount.TransformPoint(Point(data.hoseInlet))) > .001f ||
                    Vector3.Dot(emitter.NozzlePosition - emitter.HoseInletPosition, player.transform.forward) < .5f)
                    throw new Exception("Persisted 3m length or independent inlet/muzzle anchors are incorrect.");
                if (player.transform.Find("Visual/Nozzle Model").gameObject.activeSelf ||
                    !player.GetComponentInChildren<SourceHighlightView>(true))
                    throw new Exception("Old nozzle backup or source indicator setup is incorrect.");
                var view = player.GetComponentInChildren<FirefighterToolsView>(true);
                if (new SerializedObject(view).FindProperty("nozzleVisual").objectReferenceValue != mount.gameObject)
                    throw new Exception("Tool view does not own the new nozzle mount.");
                foreach (Transform anchor in new[] { player.transform.Find("Anchors/Nozzle"), player.transform.Find("Anchors/Hose Inlet") })
                    if (!anchor || anchor.IsChildOf(player.transform.Find("Visual"))) throw new Exception("Gameplay anchor depends on Visual.");
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence + "/install.txt", "NOZZLE_INSTALL_PASS\nUnity " + Application.unityVersion +
                    "\nModel import + saved reload + 3m override + muzzle/inlet anchors + source ring + idempotence PASS\n" +
                    "Model local length=.55m; exported FBX triangles=" + data.triangleCount + "\nSource SHA256=" + data.sourceSha256 + "\n");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        public static void InstallScene()
        {
            EnsurePrefab();
            var scene = EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            var layout = scene.GetRootGameObjects().Single(g => g.name == "TutorialGreybox").transform.Find(UserLevelOneBuilder.LayoutName);
            if (!layout || !layout.gameObject.activeSelf) throw new Exception("The current residential layout is not active.");
            var player = layout.GetComponentInChildren<FirefighterTools>();
            var visual = player.transform.Find("Visual");
            if (visual.Find("Nozzle Mount")) return;
            var oldNozzle = visual.Find("Nozzle Model");
            var muzzle = player.transform.Find("Anchors/Nozzle");
            if (!oldNozzle || !muzzle) throw new Exception("Existing nozzle/anchor missing; do not guess a replacement.");
            var data = ReadMetadata();
            var mount = Child(visual, "Nozzle Mount", new Vector3(.34f, .95f, .28f));
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), mount);
            model.name = "User Nozzle Model";
            PrefabUtility.RecordPrefabInstancePropertyModifications(model);
            ValidateModel(model);
            oldNozzle.gameObject.SetActive(false);
            var emitter = player.GetComponent<SprayEmitter>();
            muzzle.localPosition = mount.localPosition + Point(data.muzzle);
            var inlet = Child(player.transform.Find("Anchors"), "Hose Inlet", mount.localPosition + Point(data.hoseInlet));
            emitter.SetNozzleAnchor(muzzle);
            emitter.SetHoseInletAnchor(inlet);
            var hose = player.GetComponent<HoseConnection>();
            var hoseSettings = new SerializedObject(hose);
            hoseSettings.FindProperty("maximumLength").floatValue = 3;
            hoseSettings.ApplyModifiedPropertiesWithoutUndo();
            var view = visual.GetComponent<FirefighterToolsView>();
            var viewSettings = new SerializedObject(view);
            viewSettings.FindProperty("nozzleVisual").objectReferenceValue = mount.gameObject;
            viewSettings.ApplyModifiedPropertiesWithoutUndo();
            visual.gameObject.AddComponent<SourceHighlightView>().Initialize(player.GetComponent<LocalPlayerInput>(), hose);
            mount.gameObject.SetActive(false);
            foreach (UnityEngine.Object obj in new UnityEngine.Object[] { oldNozzle.gameObject, muzzle, emitter, hose, view })
            { EditorUtility.SetDirty(obj); PrefabUtility.RecordPrefabInstancePropertyModifications(obj); }
            if (!Application.isBatchMode) Undo.RegisterCreatedObjectUndo(mount.gameObject, "Insert user nozzle presentation");
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save nozzle setup.");
        }

        static Metadata ReadMetadata()
        {
            var data = JsonUtility.FromJson<Metadata>(File.ReadAllText(MetadataPath));
            if (data.muzzle == null || data.muzzle.Length != 3 || data.hoseInlet == null || data.hoseInlet.Length != 3 ||
                Mathf.Abs(data.lengthMeters - .55f) > .01f || data.muzzle[2] <= data.hoseInlet[2])
                throw new Exception("Invalid nozzle export metadata.");
            return data;
        }
        static Vector3 Point(float[] value) => new Vector3(value[0], value[1], value[2]);
        static Transform Child(Transform parent, string name, Vector3 position)
        { var child = new GameObject(name).transform; child.SetParent(parent, false); child.localPosition = position; return child; }

        static void EnsurePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) return;
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (!importer) throw new FileNotFoundException("Export the user nozzle first.", ModelPath);
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.bakeAxisConversion = true;
            importer.globalScale = 1;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            var root = new GameObject("UserNozzle");
            try
            {
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), root.transform);
                ValidateModel(root);
                if (!PrefabUtility.SaveAsPrefabAsset(root, PrefabPath)) throw new IOException("Could not save nozzle prefab.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void ValidateModel(GameObject model)
        {
            if (model.GetComponentsInChildren<Collider>(true).Length > 0 || model.GetComponentsInChildren<MonoBehaviour>(true).Length > 0 ||
                model.GetComponentsInChildren<Rigidbody>(true).Length > 0) throw new Exception("Nozzle model must be presentation only.");
            var meshes = model.GetComponentsInChildren<MeshFilter>(true);
            if (meshes.Length == 0) throw new Exception("Nozzle has no imported geometry.");
            Bounds bounds = new Bounds(); bool first = true;
            foreach (var filter in meshes)
            {
                if (!filter.sharedMesh) throw new Exception("Nozzle mesh reference is missing.");
                var b = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var point = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    point = model.transform.InverseTransformPoint(filter.transform.TransformPoint(point));
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
                }
            }
            if (Mathf.Abs(bounds.size.z - .55f) > .02f || bounds.size.y < .2f || bounds.size.y > .3f || bounds.size.x > .15f)
                throw new Exception("Nozzle axes/scale are not Unity +Z-forward +Y-up: " + bounds);
        }
    }
}
