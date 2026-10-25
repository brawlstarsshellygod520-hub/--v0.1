using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    // One-time additive authoring. Neither model names nor this setup are runtime requirements.
    public static class PowderGameplayBuilder
    {
        public const string Folder = "Assets/FireRescue/Greybox/Powder";
        public const string Marker = "Powder Gameplay Test";
        public const string Evidence = "Docs/Validation/2026-10-21-axe-powder";
        public static readonly Vector3 PanPosition = new Vector3(1.5f, 0, 3.8f);
        public static readonly Vector3 SourcePosition = new Vector3(4.4f, 0, 4.6f);
        static double readyAt;

        [MenuItem("FireRescue/Greybox/配置乾粉與油鍋（僅首次）")]
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
                InstallScene();
                if (saved != File.ReadAllText(TutorialGreyboxSetup.ScenePath)) throw new Exception("Repeated powder install changed the scene.");
                Transform setup = GameObject.Find(UserLevelOneBuilder.LayoutName).transform.Find(Marker);
                var source = setup.GetComponentInChildren<FluidSource>();
                var fire = setup.GetComponentInChildren<ObjectFire>();
                if (!source || source.Element != ElementKind.DryPowder || !fire || fire.Kind != FireKind.CookingOil)
                    throw new Exception("Saved source/fire class is incorrect.");
                var input = GameObject.Find(UserLevelOneBuilder.LayoutName).GetComponentInChildren<LocalPlayerInput>();
                var sources = new SerializedObject(input).FindProperty("sources");
                if (sources.arraySize != 3 || sources.GetArrayElementAtIndex(2).objectReferenceValue != source)
                    throw new Exception("Saved local source binding is missing.");
                foreach (Transform visual in new[] { source.transform.Find("Visual"), fire.transform.Find("Visual") })
                    if (!visual || visual.GetComponentsInChildren<Collider>(true).Length != 0)
                        throw new Exception("Physics leaked into replaceable Visual.");
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence + "/powder-install.txt", "POWDER_INSTALL_PASS\nUnity " + Application.unityVersion +
                    "\nSaved source/fire prefabs, enum values, command binding, Visual separation, scene reload and idempotence PASS\n");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        public static void InstallScene()
        {
            EnsurePrefabs();
            var scene = EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            var layout = scene.GetRootGameObjects().Single(g => g.name == "TutorialGreybox").transform.Find(UserLevelOneBuilder.LayoutName);
            if (!layout || !layout.gameObject.activeSelf) throw new Exception("The user's residential layout is not active.");
            if (layout.Find(Marker)) return;
            var input = layout.GetComponentInChildren<LocalPlayerInput>();
            if (!input) throw new Exception("Existing player input is missing.");
            var setup = new GameObject(Marker);
            setup.transform.SetParent(layout, false);
            var pan = Spawn("OilPan", setup.transform, PanPosition);
            var source = Spawn("DryPowderSource", setup.transform, SourcePosition).GetComponent<FluidSource>();
            var settings = new SerializedObject(input);
            var sources = settings.FindProperty("sources");
            sources.InsertArrayElementAtIndex(sources.arraySize);
            sources.GetArrayElementAtIndex(sources.arraySize - 1).objectReferenceValue = source;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(input);
            if (!Application.isBatchMode) Undo.RegisterCreatedObjectUndo(setup, "Add powder gameplay fixtures");
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save powder gameplay fixtures.");
        }

        static GameObject Spawn(string name, Transform parent, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/" + name + ".prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.localPosition = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            return instance;
        }

        static void EnsurePrefabs()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/FireRescue/Greybox", "Powder");
            Material red = Mat("ExtinguisherRed", new Color(.75f, .09f, .05f));
            Material steel = Mat("PanSteel", new Color(.16f, .18f, .2f));
            Material white = Mat("PowderBand", new Color(.94f, .92f, .78f));
            Material fireMat = Mat("PanFlame", new Color(1, .26f, .02f));
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/DryPowderSource.prefab"))
            {
                var root = new GameObject("DryPowderSource") { layer = 8 };
                try
                {
                    var source = root.AddComponent<FluidSource>();
                    source.Configure(ElementKind.DryPowder, 2);
                    var anchors = Child(root.transform, "Anchors");
                    source.SetOutlet(Child(anchors, "Outlet", new Vector3(.12f, .95f, 0)));
                    source.SetInteractionPoint(Child(anchors, "Interaction", new Vector3(0, .35f, 0)));
                    var box = root.AddComponent<BoxCollider>();
                    box.center = new Vector3(0, .5f, 0); box.size = new Vector3(.4f, 1, .4f);
                    var visual = Child(root.transform, "Visual");
                    Shape(visual, "Cylinder", PrimitiveType.Cylinder, new Vector3(0, .45f, 0), new Vector3(.32f, .4f, .32f), red);
                    Shape(visual, "Plain white band", PrimitiveType.Cylinder, new Vector3(0, .5f, 0), new Vector3(.325f, .1f, .325f), white);
                    Shape(visual, "Handle", PrimitiveType.Cube, new Vector3(0, .94f, 0), new Vector3(.35f, .08f, .12f), steel);
                    Shape(visual, "Outlet", PrimitiveType.Cube, new Vector3(.15f, .88f, 0), new Vector3(.25f, .08f, .08f), steel);
                    PrefabUtility.SaveAsPrefabAsset(root, Folder + "/DryPowderSource.prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/OilPan.prefab"))
            {
                var root = new GameObject("OilPan") { layer = 8 };
                try
                {
                    var fire = root.AddComponent<ObjectFire>(); fire.Configure(FireKind.CookingOil, 1);
                    // Stable gameplay hit volume, independent of the visible bowl/flames.
                    var box = root.AddComponent<BoxCollider>();
                    box.center = new Vector3(0, 1.06f, 0); box.size = new Vector3(.64f, .42f, .64f);
                    var visual = Child(root.transform, "Visual");
                    Shape(visual, "Bowl", PrimitiveType.Cylinder, new Vector3(0, .91f, 0), new Vector3(.62f, .06f, .62f), steel);
                    Shape(visual, "Handle", PrimitiveType.Cube, new Vector3(.37f, .92f, 0), new Vector3(.35f, .08f, .12f), steel);
                    var flame = Child(visual, "Flames", new Vector3(0, .95f, 0));
                    Shape(flame, "Center", PrimitiveType.Capsule, new Vector3(0, .24f, 0), new Vector3(.22f, .3f, .22f), fireMat);
                    Shape(flame, "Left", PrimitiveType.Capsule, new Vector3(-.18f, .17f, 0), new Vector3(.15f, .21f, .15f), fireMat);
                    Shape(flame, "Right", PrimitiveType.Capsule, new Vector3(.18f, .16f, 0), new Vector3(.14f, .2f, .14f), fireMat);
                    visual.gameObject.AddComponent<ObjectFireView>().Initialize(fire, flame);
                    PrefabUtility.SaveAsPrefabAsset(root, Folder + "/OilPan.prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
        }

        static Transform Child(Transform parent, string name, Vector3 position = default)
        { var child = new GameObject(name).transform; child.SetParent(parent, false); child.localPosition = position; return child; }
        static void Shape(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var child = GameObject.CreatePrimitive(type); child.name = name; child.transform.SetParent(parent, false);
            child.transform.localPosition = position; child.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(child.GetComponent<Collider>());
            child.GetComponent<Renderer>().sharedMaterial = material;
        }
        static Material Mat(string name, Color color)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Standard")) { name = name, color = color }; AssetDatabase.CreateAsset(material, path); }
            return material;
        }
    }
}
