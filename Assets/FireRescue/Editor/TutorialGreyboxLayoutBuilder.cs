using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace FireRescue.Editor
{
    /// <summary>
    /// Scene-only authoring for the third tutorial step.  Gameplay code remains in the
    /// reusable greybox prefabs; this class owns only their residential placement.
    /// </summary>
    public static class TutorialGreyboxLayoutBuilder
    {
        public const string LayoutName = "Step3 Residential Layout";
        public const string ScenePath = TutorialGreyboxSetup.ScenePath;

        const string ReportPath = "Docs/Validation/2026-10-19-greybox-step3/layout-install.txt";
        const int FixedObstacleLayer = 8;
        static double readyAt;

        [MenuItem("FireRescue/Tutorial/建立第三步住宅 Greybox 配置（僅首次）")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            InstallLayout();
        }

        /// <summary>Opens the independent tutorial scene and installs the layout once.</summary>
        public static void InstallLayout()
        {
            if (!File.Exists(ScenePath))
                throw new InvalidOperationException("TutorialGreybox scene is missing. Run step 1 before installing step 3.");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform tutorialRoot = FindTutorialRoot(scene);
            if (tutorialRoot.Find(LayoutName))
            {
                Debug.Log("Step 3 residential layout already exists; keeping authored scene and prefab changes.");
                return;
            }

            // Step 2 remains available as a reference assembly, but cannot run alongside the map.
            if (!Application.isBatchMode)
                Undo.RegisterFullObjectHierarchyUndo(tutorialRoot.gameObject, "Install Step 3 Greybox");
            Transform testAssembly = tutorialRoot.Find(GreyboxPrefabBuilder.AssemblyName);
            if (testAssembly)
                testAssembly.gameObject.SetActive(false);

            GameObject layout = CreateLayout(tutorialRoot);
            if (!Application.isBatchMode)
                Undo.RegisterCreatedObjectUndo(layout, "Install Step 3 Greybox");
            ConfigureCamera(layout);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.65f, .65f, .62f);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException("Could not save the Step 3 residential layout.");

            Debug.Log("TUTORIAL_STEP3_LAYOUT_INSTALLED: residential greybox, scene wiring and camera target are ready.");
        }

        /// <summary>
        /// Creates the map beneath the supplied TutorialGreybox root.  It is intentionally
        /// inactive until prefab scene dependencies and fire-grid authoring are complete.
        /// </summary>
        public static GameObject CreateLayout(Transform parent)
        {
            if (!parent)
                throw new ArgumentNullException(nameof(parent));
            if (parent.Find(LayoutName))
                throw new InvalidOperationException("The Step 3 layout already exists under this parent.");

            RequirePrefab("Player");
            RequirePrefab("Resident");
            RequirePrefab("WaterSource");
            RequirePrefab("OilSource");
            RequirePrefab("Pushable");
            RequirePrefab("FireSystem");
            RequirePrefab("Exit");

            var layout = new GameObject(LayoutName);
            layout.transform.SetParent(parent, false);
            layout.SetActive(false);
            try
            {
                var environment = Child(layout.transform, "Environment");
                var rooms = Child(layout.transform, "Rooms");
                var props = Child(layout.transform, "Furniture and Sources");
                var gameplay = Child(layout.transform, "Gameplay");
                var wallBounds = new List<Bounds>();

                BuildEnvironment(environment, wallBounds);
                BuildHouseWalls(rooms, wallBounds);
                CreateRoomMarkers(rooms);
                BuildFurniture(props);
                ConfigureGameplay(gameplay, wallBounds);

                layout.SetActive(true);
                return layout;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(layout);
                throw;
            }
        }

        /// <summary>Reuses the Step 1 camera object with RescueCamera's fixed 50/40 degree angle.</summary>
        public static void ConfigureCamera(GameObject layout)
        {
            if (!layout)
                throw new ArgumentNullException(nameof(layout));

            FirefighterMotor motor = layout.GetComponentInChildren<FirefighterMotor>(true);
            Transform tutorialRoot = layout.transform.parent;
            Transform cameraTransform = tutorialRoot ? tutorialRoot.Find("Presentation/Main Camera") : null;
            if (!motor || !cameraTransform)
                throw new InvalidOperationException("Step 3 camera requires the scene Player and TutorialGreybox/Presentation/Main Camera.");

            var camera = cameraTransform.GetComponent<Camera>();
            if (!camera)
                throw new InvalidOperationException("TutorialGreybox Main Camera is missing its Camera component.");

            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 60f;
            var rescueCamera = cameraTransform.GetComponent<RescueCamera>();
            if (!rescueCamera)
                rescueCamera = Application.isBatchMode
                    ? cameraTransform.gameObject.AddComponent<RescueCamera>()
                    : Undo.AddComponent<RescueCamera>(cameraTransform.gameObject);
            rescueCamera.Initialize(motor.transform);
        }

        /// <summary>Batch entry point: waits for compilation/import work, then writes a small report and exits.</summary>
        public static void BatchInstallAndValidate()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Use the menu in an interactive Editor.");
            readyAt = EditorApplication.timeSinceStartup + 2d;
            EditorApplication.update += RunBatch;
        }

        static void RunBatch()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < readyAt)
                return;

            EditorApplication.update -= RunBatch;
            var report = new StringBuilder("Unity " + Application.unityVersion + "\n");
            try
            {
                InstallLayout();
                string savedScene = File.ReadAllText(ScenePath);
                // This opens the saved scene again and must not overwrite an existing layout.
                InstallLayout();
                if (File.ReadAllText(ScenePath) != savedScene)
                    throw new InvalidOperationException("Repeated install changed the saved scene.");
                report.AppendLine("PASS persisted scene reload and idempotent install (no overwrite)");
                var layout = GameObject.Find(LayoutName);
                if (!layout)
                    throw new InvalidOperationException("Step 3 layout was not found after installation.");
                var field = layout.GetComponentInChildren<FireField>(true);
                var camera = Camera.main;
                if (!field || field.Origin != new Vector3(-10f, 0f, -10f) || field.Width != 20 || field.Height != 20 ||
                    Mathf.Abs(field.CellSize - 1f) > .001f || field.BurningCount != 0)
                    throw new InvalidOperationException("Fire grid configuration is not the Step 3 map-only setup.");
                if (!camera || !camera.GetComponent<RescueCamera>() || layout.GetComponentInChildren<LocalPlayerInput>(true))
                    throw new InvalidOperationException("Camera hookup or map-only input boundary is invalid.");
                report.AppendLine("PASS layout, prefab wiring, empty fire seeds, and RescueCamera hookup");
                WriteReport(report);
                Debug.Log(report.ToString());
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                report.AppendLine("FAIL " + exception.Message);
                WriteReport(report);
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        static void BuildEnvironment(Transform parent, List<Bounds> wallBounds)
        {
            // The 20 m square keeps the exit and the complete 20x20 fire grid supported.
            Solid("Exterior slab", parent, new Vector3(0f, -.3f, 0f), new Vector3(20f, .3f, 20f), Material("LayoutFloor", new Color(.49f, .5f, .48f)), 0);
            AddWall("West safety boundary", parent, new Vector3(-9.84f, 0f, 0f), new Vector3(.32f, .72f, 20f), wallBounds);
            AddWall("East safety boundary", parent, new Vector3(9.84f, 0f, 0f), new Vector3(.32f, .72f, 20f), wallBounds);
            AddWall("South safety boundary", parent, new Vector3(0f, 0f, -9.84f), new Vector3(20f, .72f, .32f), wallBounds);
            AddWall("North safety boundary", parent, new Vector3(0f, 0f, 9.84f), new Vector3(20f, .72f, .32f), wallBounds);
        }

        static void BuildHouseWalls(Transform parent, List<Bounds> wallBounds)
        {
            AddWall("West outer wall", parent, new Vector3(-8f, 0f, 2f), new Vector3(.32f, .72f, 14f), wallBounds);
            AddWall("East outer wall", parent, new Vector3(8f, 0f, 2f), new Vector3(.32f, .72f, 14f), wallBounds);
            AddWall("Back outer wall", parent, new Vector3(0f, 0f, 9f), new Vector3(16f, .72f, .32f), wallBounds);
            HorizontalWallWithGap("Front outer wall", parent, -8f, 8f, -5f, .5f, 1.8f, wallBounds);

            // The two horizontal walls give a 2.08 m clear corridor between their 0.32 m faces.
            HorizontalWallWithGap("Living corridor passage", parent, -8f, 2.66f, 1f, .5f, 2.2f, wallBounds);
            HorizontalWallWithGap("Bathroom door", parent, 2.66f, 8f, 1f, 4.5f, 1.8f, wallBounds);
            HorizontalWallWithGap("Bedroom one door", parent, -8f, -2.66f, 3.4f, -5.5f, 1.8f, wallBounds);
            HorizontalWallWithGap("Bedroom two door", parent, -2.66f, 2.66f, 3.4f, .5f, 1.8f, wallBounds);
            HorizontalWallWithGap("Bedroom three door", parent, 2.66f, 8f, 3.4f, 5.5f, 1.8f, wallBounds);
            AddWall("Bedroom divider west", parent, new Vector3(-2.66f, 0f, 6.2f), new Vector3(.32f, .72f, 5.6f), wallBounds);
            AddWall("Bedroom divider east", parent, new Vector3(2.66f, 0f, 6.2f), new Vector3(.32f, .72f, 5.6f), wallBounds);
            AddWall("Bathroom divider", parent, new Vector3(2.66f, 0f, -2f), new Vector3(.32f, .72f, 6f), wallBounds);

            Window(parent, "Back window west", new Vector3(-5.5f, .36f, 8.82f));
            Window(parent, "Back window east", new Vector3(5.5f, .36f, 8.82f));
            Surface("Living room floor tint", parent, new Vector3(-2.67f, .012f, -2f), new Vector3(10.66f, .024f, 6f), Material("LayoutLivingFloor", new Color(.59f, .55f, .46f)));
            Surface("Bathroom floor tint", parent, new Vector3(5.33f, .014f, -2f), new Vector3(5.34f, .028f, 6f), Material("LayoutBathroomFloor", new Color(.44f, .58f, .62f)));
        }

        static void CreateRoomMarkers(Transform rooms)
        {
            Transform markers = Child(rooms, "Markers");
            string[] names = { "Bedroom 1", "Bedroom 2", "Bedroom 3", "Living Room", "Kitchen", "Bathroom", "Corridor", "Exterior" };
            Vector3[] centers =
            {
                new Vector3(-5.33f, 0f, 6.2f), new Vector3(0f, 0f, 6.2f), new Vector3(5.33f, 0f, 6.2f),
                new Vector3(-2.8f, 0f, -2f), new Vector3(-6f, 0f, .6f), new Vector3(5.33f, 0f, -2f),
                new Vector3(.5f, 0f, 2.2f), new Vector3(.5f, 0f, -7.5f)
            };
            for (int i = 0; i < names.Length; i++)
                Child(markers, names[i]).localPosition = centers[i];
        }

        static void BuildFurniture(Transform parent)
        {
            Material furniture = Material("LayoutFurniture", new Color(.55f, .43f, .29f));
            Material ceramic = Material("LayoutCeramic", new Color(.78f, .81f, .8f));
            Bed("Bedroom one bed", parent, new Vector3(-5.5f, 0f, 7f), furniture);
            Bed("Bedroom two bed", parent, new Vector3(0f, 0f, 7f), furniture);
            Bed("Bedroom three bed", parent, new Vector3(5.5f, 0f, 7f), furniture);
            Solid("Bedroom one nightstand", parent, new Vector3(-7.05f, 0f, 7.5f), new Vector3(.5f, .48f, .5f), furniture, FixedObstacleLayer);
            Solid("Bedroom two nightstand", parent, new Vector3(1.55f, 0f, 7.5f), new Vector3(.5f, .48f, .5f), furniture, FixedObstacleLayer);
            Solid("Bedroom three nightstand", parent, new Vector3(7.05f, 0f, 7.5f), new Vector3(.5f, .48f, .5f), furniture, FixedObstacleLayer);

            // Keep x=.5 free from the front door through the corridor and bedroom-two door.
            GameObject sofa = Solid("Living sofa", parent, new Vector3(-5.8f, 0f, -2.1f), new Vector3(2.5f, .8f, .9f), furniture, FixedObstacleLayer);
            Primitive(sofa.transform.Find("Visual"), "Backrest", PrimitiveType.Cube, new Vector3(0f, .72f, .31f), new Vector3(2.5f, .58f, .25f), furniture);
            Spawn("Pushable", parent, "Coffee table", new Vector3(-5.1f, 0f, -3.35f));
            Solid("Kitchen counter", parent, new Vector3(-6f, 0f, .6f), new Vector3(3.2f, .9f, .7f), furniture, FixedObstacleLayer);
            // The oil source remains rooted at floor level; its prefab keeps interaction at y=.35.
            RaiseOilSourceForCounter(Spawn("OilSource", parent, "Oil bottle on counter", new Vector3(-6f, 0f, .55f)));

            Solid("Bathroom sink", parent, new Vector3(3.55f, 0f, -3.55f), new Vector3(.65f, .78f, .5f), ceramic, FixedObstacleLayer);
            Solid("Bathroom shower", parent, new Vector3(6.5f, 0f, -1.2f), new Vector3(1.2f, .72f, 1.1f), ceramic, FixedObstacleLayer);
            Spawn("WaterSource", parent, "Toilet water source", new Vector3(6.5f, 0f, -3.5f));
        }

        static void Bed(string name, Transform parent, Vector3 position, Material frameMaterial)
        {
            GameObject bed = Solid(name, parent, position, new Vector3(2.2f, .62f, 1.55f), frameMaterial, FixedObstacleLayer);
            Transform visual = bed.transform.Find("Visual");
            Primitive(visual, "Mattress", PrimitiveType.Cube, new Vector3(0f, .66f, 0f), new Vector3(2.03f, .22f, 1.42f), Material("LayoutMattress", new Color(.81f, .8f, .74f)));
            Primitive(visual, "Pillow", PrimitiveType.Cube, new Vector3(0f, .82f, .48f), new Vector3(1.15f, .16f, .34f), Material("LayoutPillow", new Color(.9f, .89f, .83f)));
        }

        static void RaiseOilSourceForCounter(GameObject oil)
        {
            Transform visual = oil.transform.Find("Visual");
            Transform outlet = oil.transform.Find("Anchors/Outlet");
            BoxCollider collider = oil.GetComponent<BoxCollider>();
            if (!visual || !outlet || !collider)
                throw new InvalidOperationException("OilSource prefab is missing its Visual, Outlet, or BoxCollider.");
            visual.localPosition = Vector3.up * .9f;
            outlet.localPosition += Vector3.up * .9f;
            collider.center += Vector3.up * .9f;
            RecordPrefabOverrides(oil, visual, outlet, collider);
        }

        static void ConfigureGameplay(Transform parent, List<Bounds> wallBounds)
        {
            GameObject system = Spawn("FireSystem", parent, "FireSystem", Vector3.zero);
            FireField field = system.GetComponent<FireField>();
            ElementReaction reactions = system.GetComponent<ElementReaction>();
            if (!field || !reactions)
                throw new InvalidOperationException("FireSystem prefab is missing FireField or ElementReaction.");
            field.Configure(new Vector3(-10f, 0f, -10f), 20, 20, 1f);
            field.SetIgnitionSeeds(Array.Empty<Vector2Int>());
            field.SetNonFlammableCells(NonFlammableCells(field, wallBounds));
            reactions.Initialize(field);

            GameObject exit = Spawn("Exit", parent, "Exit", new Vector3(.5f, 0f, -7.5f));
            Transform arrival = exit.transform.Find("Anchors/Arrival");
            GameObject resident = Spawn("Resident", parent, "Resident", new Vector3(.5f, 0f, 5.5f));
            RescueNpc npc = resident.GetComponent<RescueNpc>();
            if (!arrival || !npc)
                throw new InvalidOperationException("Resident or Exit prefab anchors are missing.");
            npc.Initialize(field, arrival);

            GameObject player = Spawn("Player", parent, "Player", new Vector3(.5f, 0f, -7f));
            FirefighterMotor motor = player.GetComponent<FirefighterMotor>();
            HoseConnection hose = player.GetComponent<HoseConnection>();
            SprayEmitter emitter = player.GetComponent<SprayEmitter>();
            if (!motor || !hose || !emitter)
                throw new InvalidOperationException("Player prefab is missing motor, hose, or emitter.");
            motor.Initialize(field, hose);
            emitter.Initialize(hose, reactions);

            RescueGameManager manager = system.GetComponent<RescueGameManager>();
            if (!manager)
                throw new InvalidOperationException("FireSystem prefab is missing RescueGameManager.");
            manager.Initialize(field, new[] { npc });
            RecordPrefabOverrides(system, field, reactions, manager);
            RecordPrefabOverrides(exit, arrival);
            RecordPrefabOverrides(resident, npc);
            RecordPrefabOverrides(player, motor, hose, emitter);
        }

        static Vector2Int[] NonFlammableCells(FireField field, List<Bounds> wallBounds)
        {
            var cells = new List<Vector2Int>();
            for (int z = 0; z < field.Height; z++)
            for (int x = 0; x < field.Width; x++)
            {
                Vector3 point = field.CellCenter(x, z);
                bool outsideHouse = point.x < -8f || point.x > 8f || point.z < -5f || point.z > 9f;
                bool wall = false;
                for (int i = 0; i < wallBounds.Count && !wall; i++)
                    wall = wallBounds[i].Intersects(new Bounds(new Vector3(point.x, .36f, point.z), new Vector3(1f, .72f, 1f)));
                if (outsideHouse || wall)
                    cells.Add(new Vector2Int(x, z));
            }
            return cells.ToArray();
        }

        static void HorizontalWallWithGap(string name, Transform parent, float minX, float maxX, float z, float gapCenter, float gapWidth, List<Bounds> wallBounds)
        {
            float gapMin = gapCenter - gapWidth * .5f;
            float gapMax = gapCenter + gapWidth * .5f;
            AddWall(name + " left", parent, new Vector3((minX + gapMin) * .5f, 0f, z), new Vector3(gapMin - minX, .72f, .32f), wallBounds);
            AddWall(name + " right", parent, new Vector3((gapMax + maxX) * .5f, 0f, z), new Vector3(maxX - gapMax, .72f, .32f), wallBounds);
        }

        static void AddWall(string name, Transform parent, Vector3 position, Vector3 size, List<Bounds> wallBounds)
        {
            GameObject wall = Solid(name, parent, position, size, Material("LayoutWall", new Color(.68f, .69f, .67f)), FixedObstacleLayer);
            // Inactive hierarchy colliders can report an empty bounds value; author this from
            // the known root-local geometry instead of querying physics before activation.
            wallBounds.Add(new Bounds(position + Vector3.up * (size.y * .5f), size));
        }

        static void Window(Transform parent, string name, Vector3 position)
        {
            var window = new GameObject(name);
            window.transform.SetParent(parent, false);
            window.transform.localPosition = position;
            Transform visual = Child(window.transform, "Visual");
            Primitive(visual, "Blue glass", PrimitiveType.Cube, Vector3.zero, new Vector3(1.65f, .42f, .05f), Material("LayoutWindow", new Color(.38f, .65f, .76f)));
        }

        static void Surface(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var surface = new GameObject(name);
            surface.transform.SetParent(parent, false);
            surface.transform.localPosition = position;
            Primitive(Child(surface.transform, "Visual"), "Model", PrimitiveType.Cube, Vector3.zero, size, material);
        }

        // Every authored block owns its physical BoxCollider on the root.  The cube renderer
        // is isolated below Visual so art can later be replaced without moving gameplay physics.
        static GameObject Solid(string name, Transform parent, Vector3 position, Vector3 size, Material material, int layer)
        {
            var root = new GameObject(name) { layer = layer };
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            var collider = root.AddComponent<BoxCollider>();
            collider.center = Vector3.up * (size.y * .5f);
            collider.size = size;
            Transform visual = Child(root.transform, "Visual");
            Primitive(visual, "Model", PrimitiveType.Cube, collider.center, size, material);
            return root;
        }

        static GameObject Spawn(string prefabName, Transform parent, string instanceName, Vector3 position)
        {
            GameObject prefab = RequirePrefab(prefabName);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = instanceName;
            instance.transform.localPosition = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            return instance;
        }

        static GameObject RequirePrefab(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GreyboxPrefabBuilder.PrefabPath(name));
            if (!prefab)
                throw new InvalidOperationException("Missing Greybox prefab: " + name + ". Run step 2 before Play.");
            return prefab;
        }

        static void RecordPrefabOverrides(GameObject instance, params UnityEngine.Object[] objects)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            foreach (UnityEngine.Object item in objects)
                if (item)
                    PrefabUtility.RecordPrefabInstancePropertyModifications(item);
        }

        static Transform FindTutorialRoot(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "TutorialGreybox")
                    return root.transform;
            throw new InvalidOperationException("TutorialGreybox scene root is missing.");
        }

        static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        static Transform Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 size, Material material)
        {
            GameObject model = GameObject.CreatePrimitive(type);
            model.name = name;
            UnityEngine.Object.DestroyImmediate(model.GetComponent<Collider>());
            model.transform.SetParent(parent, false);
            model.transform.localPosition = position;
            model.transform.localScale = size;
            model.GetComponent<Renderer>().sharedMaterial = material;
            return model.transform;
        }

        static Material Material(string name, Color color)
        {
            string path = GreyboxPrefabBuilder.Folder + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material)
                return material;
            material = new Material(Shader.Find("Standard")) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void WriteReport(StringBuilder report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, report.ToString());
        }
    }
}
