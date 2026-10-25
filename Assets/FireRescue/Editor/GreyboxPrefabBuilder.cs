using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    // Authoring only. Runtime rules use components and anchors, not these object names.
    public static class GreyboxPrefabBuilder
    {
        public const string Folder = "Assets/FireRescue/Greybox";
        public const string AssemblyName = "Step2 Test Assembly";
        public static readonly string[] PartNames =
        {
            "Player", "Resident", "WaterSource", "OilSource", "Pushable",
            "FixedObstacle", "Ground", "FireSystem", "Exit"
        };
        static double readyAt;

        public static void BatchInstallAndValidate()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use the menu in an interactive Editor.");
            readyAt = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update += RunBatch;
        }

        static void RunBatch()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < readyAt) return;
            EditorApplication.update -= RunBatch;
            try
            {
                CreateAssets();
                var report = new StringBuilder("Unity " + Application.unityVersion + "\n");
                foreach (string name in PartNames)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
                    ValidateVisualBoundary(prefab);
                    if (AssetDatabase.GetDependencies(PrefabPath(name)).Any(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException(name + " depends on a formal model.");
                    report.AppendLine("PASS " + name + ": gameplay/physics outside Visual; primitive assets; GUID=" + AssetDatabase.AssetPathToGUID(PrefabPath(name)));
                }
                InstallAssembly();
                // Check persisted prefab overrides, not only the just-created in-memory objects.
                EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
                var assembly = GameObject.Find(AssemblyName);
                foreach (Transform part in assembly.transform) ValidateVisualBoundary(part.gameObject);
                report.AppendLine("PASS tutorial assembly: scene-owned references stay outside Visual");
                ValidateSavedWiring(assembly);
                report.AppendLine("PASS saved scene reload: motor, emitter, source, views, NPC exit and rescue manager references resolve");
                report.AppendLine("GREYBOX_PREFAB_CONTRACT_PASS prefabs=" + PartNames.Length);
                Directory.CreateDirectory("Docs/Validation/2026-10-12-greybox-step2");
                File.WriteAllText("Docs/Validation/2026-10-12-greybox-step2/prefab-contract.txt", report.ToString());
                Debug.Log(report.ToString());
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        static void ValidateVisualBoundary(GameObject part)
        {
            var visual = part.transform.Find("Visual");
            if (!visual) throw new InvalidOperationException(part.name + " has no Visual slot.");
            if (visual.GetComponentsInChildren<Collider>(true).Length > 0 || visual.GetComponentsInChildren<Rigidbody>(true).Length > 0)
                throw new InvalidOperationException(part.name + " has physics inside Visual.");
            foreach (var behaviour in visual.GetComponentsInChildren<MonoBehaviour>(true))
                if (!(behaviour is HoseView) && !(behaviour is FireFieldView) && !(behaviour is CharacterLocomotionAnimation))
                    throw new InvalidOperationException(part.name + " has gameplay or missing script inside Visual.");
            foreach (var behaviour in part.GetComponents<MonoBehaviour>())
            {
                if (!behaviour) throw new InvalidOperationException(part.name + " has a missing script.");
                var properties = new SerializedObject(behaviour).GetIterator();
                while (properties.Next(true))
                {
                    if (properties.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var value = properties.objectReferenceValue;
                    Transform referenced = value is Component component ? component.transform : value is GameObject go ? go.transform : null;
                    if (referenced && referenced.IsChildOf(visual))
                        throw new InvalidOperationException(part.name + "." + properties.propertyPath + " points into replaceable Visual.");
                }
            }
        }

        static void ValidateSavedWiring(GameObject assembly)
        {
            var field = assembly.GetComponentInChildren<FireField>(true);
            var reactions = assembly.GetComponentInChildren<ElementReaction>(true);
            var motor = assembly.GetComponentInChildren<FirefighterMotor>(true);
            var hose = motor.GetComponent<HoseConnection>();
            var emitter = motor.GetComponent<SprayEmitter>();
            var npc = assembly.GetComponentInChildren<RescueNpc>(true);
            var manager = assembly.GetComponentInChildren<RescueGameManager>(true);
            RequireReference(motor, "field", field);
            RequireReference(motor, "hose", hose);
            RequireReference(hose, "interactionOrigin", motor.transform.Find("Anchors/Interaction"));
            RequireReference(emitter, "hose", hose);
            RequireReference(emitter, "reactions", reactions);
            RequireReference(emitter, "nozzleAnchor", motor.transform.Find("Anchors/Nozzle"));
            RequireReference(reactions, "field", field);
            RequireReference(npc, "field", field);
            RequireReference(npc, "exit", assembly.transform.Find("Exit/Anchors/Arrival"));
            RequireReference(manager, "field", field);
            RequireReference(manager, "survivors.Array.data[0]", npc);
            RequireReference(assembly.GetComponentInChildren<FireFieldView>(true), "field", field);
            RequireReference(assembly.GetComponentInChildren<HoseView>(true), "emitter", emitter);
            foreach (var source in assembly.GetComponentsInChildren<FluidSource>(true))
            {
                RequireReference(source, "outlet", source.transform.Find("Anchors/Outlet"));
                RequireReference(source, "interactionPoint", source.transform.Find("Anchors/Interaction"));
            }
        }

        static void RequireReference(MonoBehaviour owner, string property, UnityEngine.Object expected)
        {
            if (!owner || !expected || new SerializedObject(owner).FindProperty(property).objectReferenceValue != expected)
                throw new InvalidOperationException("Missing persisted wiring: " + property);
        }

        [MenuItem("FireRescue/Tutorial/建立第二步 Greybox 元件與測試組裝（僅首次）")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateAssets();
            InstallAssembly();
        }

        public static void CreateAssets()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Create Greybox assets before Play Mode.");
            EnsureFolder(Folder);
            EnsureFolder(Folder + "/Prefabs");
            EnsureFolder(Folder + "/Materials");
            SaveIfMissing("Player", Player);
            SaveIfMissing("Resident", Resident);
            SaveIfMissing("WaterSource", () => Source(ElementKind.Water));
            SaveIfMissing("OilSource", () => Source(ElementKind.Oil));
            SaveIfMissing("Pushable", () => Solid("Pushable", new Vector3(0, .35f, 0), new Vector3(1, .7f, .8f), 10, true));
            SaveIfMissing("FixedObstacle", () => Solid("FixedObstacle", new Vector3(0, .4f, 0), new Vector3(1, .8f, 1), 8));
            SaveIfMissing("Ground", () => Solid("Ground", new Vector3(0, -.15f, 0), new Vector3(12, .3f, 12), 0));
            SaveIfMissing("FireSystem", FireSystem);
            SaveIfMissing("Exit", Exit);
        }

        public static void InstallAssembly()
        {
            var scene = EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            Transform tutorialRoot = null;
            foreach (var candidate in scene.GetRootGameObjects())
                if (candidate.name == "TutorialGreybox") tutorialRoot = candidate.transform;
            if (!tutorialRoot) throw new InvalidOperationException("TutorialGreybox scene root is missing.");
            if (tutorialRoot.Find(AssemblyName))
            {
                Debug.Log("Greybox test assembly already exists; keeping authored scene and prefab changes.");
                return;
            }
            var assembly = CreateTestAssembly();
            assembly.transform.SetParent(tutorialRoot, false);
            Undo.RegisterCreatedObjectUndo(assembly, "Create Greybox test assembly");
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save tutorial assembly.");
            Debug.Log("GREYBOX_PARTS_INSTALLED: nine reusable prefabs and one wired test assembly.");
        }

        // Shared composition for the authoring scene and real Prefab Play Mode checks.
        // Parent is inactive until every scene-owned reference has been supplied.
        public static GameObject CreateTestAssembly()
        {
            var root = new GameObject(AssemblyName);
            root.SetActive(false);
            try
            {
                Spawn("Ground", root.transform, Vector3.zero);
                var system = Spawn("FireSystem", root.transform, Vector3.zero);
                var field = system.GetComponent<FireField>();
                var reactions = system.GetComponent<ElementReaction>();
                reactions.Initialize(field);
                var exit = Spawn("Exit", root.transform, new Vector3(2.5f, 0, -3.5f));
                var survivor = Spawn("Resident", root.transform, new Vector3(2.5f, 0, 3.5f));
                var npc = survivor.GetComponent<RescueNpc>();
                npc.Initialize(field, exit.transform.Find("Anchors/Arrival"));
                var player = Spawn("Player", root.transform, new Vector3(0, 0, -2.5f));
                var hose = player.GetComponent<HoseConnection>();
                player.GetComponent<FirefighterMotor>().Initialize(field, hose);
                player.GetComponent<SprayEmitter>().Initialize(hose, reactions);
                Spawn("WaterSource", root.transform, new Vector3(-1.5f, 0, -2.5f));
                Spawn("OilSource", root.transform, new Vector3(1.5f, 0, -2.5f));
                Spawn("Pushable", root.transform, new Vector3(-2, 0, .5f));
                Spawn("FixedObstacle", root.transform, new Vector3(2, 0, .5f));
                system.GetComponent<RescueGameManager>().Initialize(field, new[] { npc });
                root.SetActive(true);
                return root;
            }
            catch
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(root);
                else UnityEngine.Object.DestroyImmediate(root);
                throw;
            }
        }

        static GameObject Spawn(string name, Transform parent, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name));
            if (!prefab) throw new InvalidOperationException("Missing Greybox prefab: " + name + ". Run CreateAssets before Play.");
            var instance = Application.isPlaying ? UnityEngine.Object.Instantiate(prefab, parent) :
                (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.localPosition = position;
            return instance;
        }

        static GameObject Player()
        {
            var root = Actor("Player", 9, 1.6f);
            var anchors = Child(root.transform, "Anchors");
            var nozzle = Child(anchors, "Nozzle", new Vector3(0, .9f, .45f));
            var interaction = Child(anchors, "Interaction", Vector3.up * .35f);
            var hose = root.AddComponent<HoseConnection>();
            hose.SetInteractionOrigin(interaction);
            root.AddComponent<FirefighterMotor>().Initialize(null, hose);
            var emitter = root.AddComponent<SprayEmitter>();
            emitter.Initialize(hose, null);
            emitter.SetNozzleAnchor(nozzle);
            var visual = root.transform.Find("Visual");
            Primitive(visual, "Body", PrimitiveType.Capsule, new Vector3(0, .75f, 0), new Vector3(.6f, .65f, .6f), Mat("Firefighter", new Color(.85f, .35f, .16f)));
            Primitive(visual, "Helmet", PrimitiveType.Sphere, new Vector3(0, 1.45f, 0), new Vector3(.75f, .3f, .75f), Mat("SafetyYellow", new Color(.95f, .73f, .12f)));
            var nozzleVisual = Primitive(visual, "Nozzle Model", PrimitiveType.Cylinder, new Vector3(0, .9f, .3f), new Vector3(.1f, .15f, .1f), Mat("Dark", new Color(.18f, .2f, .22f)));
            nozzleVisual.localRotation = Quaternion.Euler(90, 0, 0);
            Child(visual, "Hose Presentation").gameObject.AddComponent<HoseView>().Initialize(emitter);
            return root;
        }

        static GameObject Resident()
        {
            var root = Actor("Resident", 11, 1.5f);
            var npc = root.AddComponent<RescueNpc>();
            var settings = new SerializedObject(npc);
            settings.FindProperty("tintBodyByState").boolValue = false;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Primitive(root.transform.Find("Visual"), "Model", PrimitiveType.Capsule, Vector3.up * .75f, new Vector3(.6f, .75f, .6f), Mat("Resident", new Color(.35f, .6f, .42f)));
            return root;
        }

        static GameObject Source(ElementKind kind)
        {
            bool water = kind == ElementKind.Water;
            var root = Root(water ? "WaterSource" : "OilSource", 8);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = water ? new Vector3(.8f, 1f, .85f) : new Vector3(.25f, .55f, .25f);
            collider.center = Vector3.up * collider.size.y * .5f;
            var anchors = Child(root.transform, "Anchors");
            var outlet = Child(anchors, "Outlet", water ? new Vector3(0, .95f, .25f) : Vector3.up * .55f);
            var source = root.AddComponent<FluidSource>();
            source.Configure(kind);
            source.SetOutlet(outlet);
            source.SetInteractionPoint(Child(anchors, "Interaction", Vector3.up * .35f));
            var visual = root.transform.Find("Visual");
            if (water)
            {
                var ceramic = Mat("Ceramic", new Color(.83f, .84f, .82f));
                Primitive(visual, "Toilet base", PrimitiveType.Cube, new Vector3(0, .25f, -.1f), new Vector3(.45f, .5f, .55f), ceramic);
                Primitive(visual, "Bowl", PrimitiveType.Sphere, new Vector3(0, .48f, -.1f), new Vector3(.75f, .3f, .65f), ceramic);
                Primitive(visual, "Cistern", PrimitiveType.Cube, new Vector3(0, .73f, .25f), new Vector3(.68f, .5f, .28f), ceramic);
                Primitive(visual, "Water", PrimitiveType.Cube, new Vector3(0, .99f, .25f), new Vector3(.55f, .025f, .2f), Mat("Water", new Color(.2f, .65f, .82f)));
            }
            else
            {
                Primitive(visual, "Salad oil bottle", PrimitiveType.Cylinder, Vector3.up * .25f, new Vector3(.24f, .25f, .24f), Mat("Oil", new Color(.82f, .66f, .2f)));
                Primitive(visual, "Cap", PrimitiveType.Cylinder, Vector3.up * .525f, new Vector3(.13f, .025f, .13f), Mat("Firefighter", new Color(.85f, .35f, .16f)));
            }
            return root;
        }

        static GameObject FireSystem()
        {
            var root = Root("FireSystem", 0);
            var field = root.AddComponent<FireField>();
            field.Configure(new Vector3(-5, 0, -5), 10, 10, 1);
            field.SetIgnitionSeeds(new[] { new Vector2Int(5, 5) });
            field.SetNonFlammableCells(Array.Empty<Vector2Int>());
            root.AddComponent<ElementReaction>().Initialize(field);
            root.AddComponent<RescueGameManager>();
            var view = root.transform.Find("Visual").gameObject.AddComponent<FireFieldView>();
            var settings = new SerializedObject(view);
            settings.FindProperty("field").objectReferenceValue = field;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static GameObject Exit()
        {
            var root = Root("Exit", 0);
            Child(Child(root.transform, "Anchors"), "Arrival");
            Primitive(root.transform.Find("Visual"), "Marker", PrimitiveType.Cube, Vector3.up * .02f, new Vector3(1, .04f, 1), Mat("Resident", new Color(.35f, .6f, .42f)));
            return root;
        }

        static GameObject Solid(string name, Vector3 center, Vector3 size, int layer, bool pushable = false)
        {
            var root = Root(name, layer);
            var box = root.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
            if (pushable)
            {
                var body = root.AddComponent<Rigidbody>();
                body.mass = 3;
                body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            }
            Primitive(root.transform.Find("Visual"), "Model", PrimitiveType.Cube, center, size,
                Mat(name == "Ground" ? "Ground" : "Block", name == "Ground" ? new Color(.52f, .53f, .52f) : new Color(.65f, .66f, .64f)));
            return root;
        }

        static GameObject Actor(string name, int layer, float height)
        {
            var root = Root(name, layer);
            var controller = root.AddComponent<CharacterController>();
            controller.height = height;
            controller.center = Vector3.up * height * .5f;
            controller.radius = .28f;
            controller.stepOffset = .2f;
            controller.skinWidth = .025f;
            controller.minMoveDistance = 0;
            return root;
        }

        static GameObject Root(string name, int layer)
        {
            var root = new GameObject(name) { layer = layer };
            Child(root.transform, "Visual");
            return root;
        }

        static Transform Child(Transform parent, string name, Vector3 position = default)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = position;
            return child;
        }

        static Transform Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 size, Material material)
        {
            var model = GameObject.CreatePrimitive(type);
            model.name = name;
            UnityEngine.Object.DestroyImmediate(model.GetComponent<Collider>());
            model.transform.SetParent(parent, false);
            model.transform.localPosition = position;
            model.transform.localScale = size;
            model.GetComponent<Renderer>().sharedMaterial = material;
            return model.transform;
        }

        static Material Mat(string name, Color color)
        {
            string path = Folder + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material) return material;
            material = new Material(Shader.Find("Standard")) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static string PrefabPath(string name) => Folder + "/Prefabs/" + name + ".prefab";
        static void SaveIfMissing(string name, Func<GameObject> make)
        {
            string path = PrefabPath(name);
            if (File.Exists(path)) return;
            var root = make();
            try
            {
                if (!PrefabUtility.SaveAsPrefabAsset(root, path)) throw new IOException("Could not save " + path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
    }
}
