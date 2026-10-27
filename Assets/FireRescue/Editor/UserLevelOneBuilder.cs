using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    // Imports evaluated mesh data from the user's .blend. No runtime rule depends on mesh names.
    public static class UserLevelOneBuilder
    {
        public const string LayoutName = "Step4 User Layout";
        public const string Folder = "Assets/FireRescue/Art/UserProvided/LevelOne";
        public const string Evidence = "Docs/Validation/2026-10-19-greybox-step4";
        [Serializable] public sealed class Geometry { public float scale; public float cutHeight; public Part[] meshes; public Door[] doors; }
        [Serializable] public sealed class Part { public string name; public string kind; public Vector3[] vertices; public int[] triangles; }
        [Serializable] public sealed class Door { public string name; public Vector3 center; public bool alongX; public float width; }
        static double readyAt;
        static bool refreshExport;

        [MenuItem("FireRescue/Tutorial/匯入第一關戶型與第四步操控（僅首次）")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            InstallLayout();
        }

        public static void BatchInstallAndValidate()
        {
            readyAt = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update += Batch;
        }

        // Explicit refresh for this generated import only; ordinary Install never rebuilds existing assets.
        public static void BatchRefreshExport()
        { refreshExport = true; BatchInstallAndValidate(); }

        static void Batch()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < readyAt) return;
            EditorApplication.update -= Batch;
            try
            {
                if (refreshExport)
                {
                    var data = JsonUtility.FromJson<Geometry>(File.ReadAllText("ArtSource/UserProvided/LevelOne/level-one.geometry.json"));
                    foreach (Part part in data.meshes)
                    foreach (string suffix in new[] { "Visual", "Collision" })
                    {
                        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/" + part.name + "-" + suffix + ".asset");
                        if (!mesh) throw new Exception("Missing generated mesh " + part.name);
                        mesh.Clear(); mesh.vertices = part.vertices; mesh.triangles = part.triangles;
                        mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
                    }
                    AssetDatabase.SaveAssets();
                    var scene = EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
                    var imported = GameObject.Find(LayoutName);
                    Physics.SyncTransforms();
                    AuthorFireGrid(imported.GetComponentInChildren<FireField>());
                    EditorSceneManager.SaveScene(scene);
                }
                InstallLayout();
                string saved = File.ReadAllText(TutorialGreyboxSetup.ScenePath);
                InstallLayout();
                if (saved != File.ReadAllText(TutorialGreyboxSetup.ScenePath)) throw new Exception("Install overwrote an existing layout.");
                GameObject layout = GameObject.Find(LayoutName);
                var motor = layout.GetComponentInChildren<FirefighterMotor>();
                var input = motor.GetComponent<LocalPlayerInput>();
                var tools = motor.GetComponent<FirefighterTools>();
                if (!input || !tools || !layout.GetComponentInChildren<FirefighterToolsView>()) throw new Exception("Tools wiring missing.");
                if (new SerializedObject(input).FindProperty("tools").objectReferenceValue != tools) throw new Exception("Persisted tools reference missing.");
                foreach (var collider in layout.GetComponentsInChildren<MeshCollider>())
                {
                    var visual = collider.transform.Find("Visual").GetComponent<MeshFilter>();
                    if (!collider.sharedMesh || !visual.sharedMesh || collider.sharedMesh == visual.sharedMesh)
                        throw new Exception("Visual/collision mesh ownership not independent.");
                }
                if (layout.GetComponentsInChildren<MeshCollider>().Length != 11) throw new Exception("Source mesh count mismatch.");
                if (layout.GetComponentInChildren<FireField>().BurningCount != 0) throw new Exception("Step4 must not start future lessons.");
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence + "/install.txt", "USER_LEVEL_ONE_INSTALL_PASS\nUnity " + Application.unityVersion +
                    "\n11 actual source meshes; separate visual/physics assets; serialized tools/input references; no ignition seeds; reload and idempotence PASS\n");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        public static void InstallLayout()
        {
            var scene = EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            var root = scene.GetRootGameObjects().Single(x => x.name == "TutorialGreybox").transform;
            if (root.Find(LayoutName)) return; // Never overwrite the user's later edits.
            var data = JsonUtility.FromJson<Geometry>(File.ReadAllText("ArtSource/UserProvided/LevelOne/level-one.geometry.json"));
            if (data.meshes.Length != 11 || data.doors.Length != 5) throw new Exception("Unexpected source export.");
            EnsureFolder(Folder);
            root.Find(TutorialGreyboxLayoutBuilder.LayoutName).gameObject.SetActive(false);
            root.Find(GreyboxPrefabBuilder.AssemblyName).gameObject.SetActive(false);
            var layout = Child(root, LayoutName).gameObject;
            layout.SetActive(false);
            var geometry = Child(layout.transform, "Imported Architecture");
            var wall = Mat("Wall", new Color(.69f, .7f, .68f));
            var doorMat = Mat("Door", new Color(.56f, .43f, .29f));
            foreach (Part part in data.meshes)
            {
                var owner = Child(geometry, part.name).gameObject;
                owner.layer = 8;
                owner.AddComponent<MeshCollider>().sharedMesh = MeshAsset(part, "Collision");
                var visual = Child(owner.transform, "Visual").gameObject;
                visual.AddComponent<MeshFilter>().sharedMesh = MeshAsset(part, "Visual");
                visual.AddComponent<MeshRenderer>().sharedMaterial = part.kind == "door" ? doorMat : wall;
            }
            var markers = Child(layout.transform, "Authoring Markers");
            foreach (Door door in data.doors) Child(markers, door.name, door.center);
            var environment = Child(layout.transform, "Floor and Boundary");
            Solid(environment, "Ground", new Vector3(0, -.3f, 2), new Vector3(25, .3f, 26), Mat("Paving", new Color(.43f, .47f, .46f)), 0);
            // Non-colliding floor tints expose the imported rooms, without adding text or invented walls.
            Tint(environment, "Main room floor", new Vector3(3.6f, .006f, -.3f), new Vector3(9.45f, .012f, 11.4f), new Color(.59f, .56f, .49f));
            Tint(environment, "Left room floor", new Vector3(-4.9f, .006f, -2.4f), new Vector3(7.1f, .012f, 7.3f), new Color(.61f, .57f, .5f));
            Tint(environment, "Bathroom floor", new Vector3(-4.9f, .007f, 2.45f), new Vector3(7.1f, .014f, 2.15f), new Color(.45f, .59f, .62f));
            Tint(environment, "Rear room floor", new Vector3(4.85f, .006f, 8.65f), new Vector3(6.9f, .012f, 5.95f), new Color(.6f, .56f, .5f));
            Tint(environment, "Annex floor", new Vector3(-6.5f, .006f, 10.4f), new Vector3(3.65f, .012f, 2.6f), new Color(.57f, .54f, .48f));
            Solid(environment, "West boundary", new Vector3(-12.35f, 0, 2), new Vector3(.3f, .8f, 26), wall);
            Solid(environment, "East boundary", new Vector3(12.35f, 0, 2), new Vector3(.3f, .8f, 26), wall);
            Solid(environment, "Front boundary", new Vector3(0, 0, -10.85f), new Vector3(25, .8f, .3f), wall);
            Solid(environment, "Rear boundary", new Vector3(0, 0, 14.85f), new Vector3(25, .8f, .3f), wall);

            var props = Child(layout.transform, "Household Props");
            var furniture = Mat("Furniture", new Color(.54f, .44f, .32f));
            Solid(props, "Bed", new Vector3(-6.2f, 0, -2.5f), new Vector3(2, .6f, 2.5f), furniture);
            Solid(props, "Sofa", new Vector3(6.8f, 0, .5f), new Vector3(1, .8f, 2.5f), furniture);
            Spawn("Pushable", props, "Coffee table", new Vector3(4.7f, 0, .5f));
            Solid(props, "Kitchen counter", new Vector3(.7f, 0, 3.8f), new Vector3(2.4f, .85f, .7f), furniture);
            GameObject oil = Spawn("OilSource", props, "Salad oil source", new Vector3(.7f, 0, 3.8f));
            oil.transform.Find("Visual").localPosition += Vector3.up * .85f;
            oil.transform.Find("Anchors/Outlet").localPosition += Vector3.up * .85f;
            oil.GetComponent<BoxCollider>().center += Vector3.up * .85f;
            Record(oil);
            GameObject water = Spawn("WaterSource", props, "Toilet cistern source", new Vector3(-3.3f, 0, 2.85f));

            var gameplay = Child(layout.transform, "Gameplay");
            GameObject system = Spawn("FireSystem", gameplay, "FireSystem", Vector3.zero);
            var field = system.GetComponent<FireField>();
            field.Configure(new Vector3(-12, 0, -10), 48, 48, .5f);
            field.SetIgnitionSeeds(Array.Empty<Vector2Int>());
            field.SetNonFlammableCells(Array.Empty<Vector2Int>());
            var reactions = system.GetComponent<ElementReaction>();
            reactions.Initialize(field);
            var exit = Spawn("Exit", gameplay, "Exit", new Vector3(5.75f, 0, -8.25f));
            var npc = Spawn("Resident", gameplay, "Resident", new Vector3(-3.75f, 0, -3.75f)).GetComponent<RescueNpc>();
            npc.Initialize(field, exit.transform.Find("Anchors/Arrival"));
            var player = Spawn("Player", gameplay, "Player", new Vector3(5.74f, 0, -7.5f));
            var motor = player.GetComponent<FirefighterMotor>();
            var hose = player.GetComponent<HoseConnection>();
            var emitter = player.GetComponent<SprayEmitter>();
            motor.Initialize(field, hose);
            emitter.Initialize(hose, reactions);
            var tools = player.AddComponent<FirefighterTools>();
            tools.Initialize(emitter);
            var input = player.AddComponent<LocalPlayerInput>();
            input.Initialize(motor, hose, emitter, root.Find("Presentation/Main Camera").GetComponent<Camera>(),
                new[] { water.GetComponent<FluidSource>(), oil.GetComponent<FluidSource>() }, new[] { npc });
            input.SetTools(tools);
            var visualRoot = player.transform.Find("Visual");
            var axe = Child(visualRoot, "Axe Pivot", new Vector3(.3f, .92f, .25f));
            Model(axe, "Handle", PrimitiveType.Cylinder, new Vector3(0, .26f, 0), new Vector3(.055f, .3f, .055f), furniture);
            Model(axe, "Head", PrimitiveType.Cube, new Vector3(0, .55f, 0), new Vector3(.38f, .22f, .12f), Mat("AxeSteel", new Color(.66f, .7f, .72f)));
            // Keep the connected cable visible while using the axe. Only the handheld nozzle changes equipment visibility.
            visualRoot.gameObject.AddComponent<FirefighterToolsView>().Initialize(tools, emitter, axe, null, visualRoot.Find("Nozzle Model").gameObject);
            system.GetComponent<RescueGameManager>().Initialize(field, new[] { npc });
            Record(system); Record(npc.gameObject); Record(player);
            layout.SetActive(true);
            Physics.SyncTransforms();
            AuthorFireGrid(field);
            TutorialGreyboxLayoutBuilder.ConfigureCamera(layout);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed.");
        }

        static void AuthorFireGrid(FireField field)
        {
            var nonflammable = new List<Vector2Int>();
            for (int z = 0; z < field.Height; z++)
            for (int x = 0; x < field.Width; x++)
            {
                var p = field.CellCenter(x, z);
                bool interior = Rect(p, -8.5f, -1.1f, -6, 3.6f) || Rect(p, -1.1f, 8.4f, -6, 5.65f) ||
                    Rect(p, 1.45f, 8.4f, 5.65f, 11.65f) || Rect(p, -8.35f, -4.65f, 9.1f, 11.65f);
                if (!interior || Physics.CheckBox(p + Vector3.up * .4f, new Vector3(.245f, .35f, .245f), Quaternion.identity, 1 << 8))
                    nonflammable.Add(new Vector2Int(x, z));
            }
            field.SetNonFlammableCells(nonflammable.ToArray());
            Record(field.gameObject);
        }

        static bool Rect(Vector3 p, float x0, float x1, float z0, float z1) => p.x >= x0 && p.x <= x1 && p.z >= z0 && p.z <= z1;
        static Mesh MeshAsset(Part part, string suffix)
        {
            string path = Folder + "/" + part.name + "-" + suffix + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) return existing;
            var mesh = new Mesh { name = part.name + " " + suffix, vertices = part.vertices, triangles = part.triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        static GameObject Spawn(string prefabName, Transform parent, string name, Vector3 pos)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GreyboxPrefabBuilder.PrefabPath(prefabName));
            if (!prefab) throw new Exception("Missing prefab " + prefabName);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name; go.transform.localPosition = pos; Record(go); return go;
        }
        static Transform Child(Transform parent, string name, Vector3 pos = default)
        { var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = pos; return t; }
        static void Record(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<Component>(true)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            PrefabUtility.RecordPrefabInstancePropertyModifications(root);
        }
        static void Solid(Transform parent, string name, Vector3 basePos, Vector3 size, Material material, int layer = 8)
        {
            var owner = Child(parent, name, basePos).gameObject; owner.layer = layer;
            var box = owner.AddComponent<BoxCollider>(); box.center = Vector3.up * size.y * .5f; box.size = size;
            Model(Child(owner.transform, "Visual"), "Model", PrimitiveType.Cube, box.center, size, material);
        }
        static void Tint(Transform parent, string name, Vector3 pos, Vector3 size, Color color)
        { Model(Child(Child(parent, name), "Visual"), "Model", PrimitiveType.Cube, pos, size, Mat(name, color)); }
        static void Model(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
        static Material Mat(string name, Color color)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material) return material;
            material = new Material(Shader.Find("Standard")) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
