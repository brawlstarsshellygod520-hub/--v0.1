using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    public static class WindowHoseBuilder
    {
        public const string Marker = "Window Hose Test";
        public const string Folder = "Assets/FireRescue/Greybox/Window";
        public const string Evidence = "Docs/Validation/2026-10-22-window-hose";
        static double readyAt;

        [MenuItem("FireRescue/Greybox/配置窗口導管（僅首次）")]
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
                if (File.ReadAllText(TutorialGreyboxSetup.ScenePath) != saved) throw new Exception("Repeated window install changed the scene.");
                var layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
                var fixture = layout.transform.Find(Marker);
                var window = fixture.GetComponentInChildren<HoseWindow>();
                var input = layout.GetComponentInChildren<LocalPlayerInput>();
                var windows = new SerializedObject(input).FindProperty("hoseWindows");
                if (!window || Vector3.Distance(window.RoutePosition, new Vector3(-8.07f, 1.15f, 2.45f)) > .001f ||
                    windows.arraySize != 1 || windows.GetArrayElementAtIndex(0).objectReferenceValue != window)
                    throw new Exception("Saved window/route/input binding is incorrect.");
                if (window.transform.Find("Visual").GetComponentsInChildren<Collider>(true).Length != 0 ||
                    window.transform.Find("Anchors/Route").IsChildOf(window.transform.Find("Visual")))
                    throw new Exception("Window gameplay anchor or physics depends on Visual.");
                var fire = fixture.GetComponentInChildren<ObjectFire>();
                if (!fire || fire.Kind != FireKind.Ordinary) throw new Exception("Outdoor water target missing.");
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence + "/install.txt", "WINDOW_HOSE_INSTALL_PASS\nUnity " + Application.unityVersion +
                    "\nScene/prefab save-reload; source-independent window anchor; input reference; Visual separation; idempotence PASS\n");
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
            var setup = Child(layout, Marker);
            var window = Spawn("HoseWindow", setup, "Bathroom Hose Window", new Vector3(-8.07f, 0, 2.45f)).GetComponent<HoseWindow>();
            Spawn("WindowTarget", setup, "Outdoor water target", new Vector3(-10.2f, 0, 5.5f));
            var settings = new SerializedObject(input);
            var windows = settings.FindProperty("hoseWindows");
            windows.InsertArrayElementAtIndex(windows.arraySize);
            windows.GetArrayElementAtIndex(windows.arraySize - 1).objectReferenceValue = window;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(input);
            if (!Application.isBatchMode) Undo.RegisterCreatedObjectUndo(setup.gameObject, "Add window hose fixture");
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save window hose fixture.");
        }

        static void EnsurePrefabs()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/FireRescue/Greybox", "Window");
            var frameMat = Mat("WindowFrame", new Color(.16f, .5f, .56f));
            var wood = Mat("TestWood", new Color(.5f, .32f, .17f));
            var flameMat = Mat("TestFlame", new Color(1, .26f, .02f));
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/HoseWindow.prefab"))
            {
                var root = new GameObject("HoseWindow");
                try
                {
                    var anchors = Child(root.transform, "Anchors");
                    root.AddComponent<HoseWindow>().Initialize(Child(anchors, "Route", new Vector3(0, 1.15f, 0)),
                        Child(anchors, "Interaction", new Vector3(0, .35f, 0)));
                    var visual = Child(root.transform, "Visual");
                    // Existing .8m cutaway wall remains solid for players. The route passes
                    // through this open frame above its sill; no source mesh is edited.
                    Shape(visual, "Sill", PrimitiveType.Cube, new Vector3(0, .84f, 0), new Vector3(.38f, .12f, 1.3f), frameMat);
                    Shape(visual, "Top", PrimitiveType.Cube, new Vector3(0, 1.56f, 0), new Vector3(.14f, .1f, 1.3f), frameMat);
                    Shape(visual, "North jamb", PrimitiveType.Cube, new Vector3(0, 1.2f, .6f), new Vector3(.14f, .72f, .1f), frameMat);
                    Shape(visual, "South jamb", PrimitiveType.Cube, new Vector3(0, 1.2f, -.6f), new Vector3(.14f, .72f, .1f), frameMat);
                    PrefabUtility.SaveAsPrefabAsset(root, Folder + "/HoseWindow.prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/WindowTarget.prefab"))
            {
                var root = new GameObject("WindowTarget") { layer = 8 };
                try
                {
                    var fire = root.AddComponent<ObjectFire>(); fire.Configure(FireKind.Ordinary, 1);
                    var collider = root.AddComponent<BoxCollider>(); collider.center = new Vector3(0, .45f, 0); collider.size = new Vector3(.7f, .9f, .7f);
                    var visual = Child(root.transform, "Visual");
                    Shape(visual, "Wood block", PrimitiveType.Cube, new Vector3(0, .15f, 0), new Vector3(.65f, .3f, .65f), wood);
                    var flames = Child(visual, "Flames", new Vector3(0, .3f, 0));
                    Shape(flames, "Fire", PrimitiveType.Capsule, new Vector3(0, .25f, 0), new Vector3(.32f, .3f, .32f), flameMat);
                    visual.gameObject.AddComponent<ObjectFireView>().Initialize(fire, flames);
                    PrefabUtility.SaveAsPrefabAsset(root, Folder + "/WindowTarget.prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
        }
        static GameObject Spawn(string prefabName, Transform parent, string name, Vector3 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/" + prefabName + ".prefab"), parent);
            instance.name = name; instance.transform.localPosition = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform); return instance;
        }
        static Transform Child(Transform parent, string name, Vector3 position = default)
        { var child = new GameObject(name).transform; child.SetParent(parent, false); child.localPosition = position; return child; }
        static void Shape(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var child = GameObject.CreatePrimitive(type); child.name = name; child.transform.SetParent(parent, false);
            child.transform.localPosition = position; child.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(child.GetComponent<Collider>()); child.GetComponent<Renderer>().sharedMaterial = material;
        }
        static Material Mat(string name, Color color)
        {
            string path = Folder + "/" + name + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Standard")) { name = name, color = color }; AssetDatabase.CreateAsset(material, path); }
            return material;
        }
    }
}
