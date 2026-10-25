using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace FireRescue.Editor
{
    public static class FirePrototypeBuilder
    {
        public const string ScenePath = "Assets/FireRescue/Scenes/FireRoom.unity";
        const string MaterialsPath = "Assets/FireRescue/Materials/";

        [MenuItem("消防 Prototype/開啟測試場景")]
        public static void OpenScene()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("消防 Prototype/重建測試房間（覆寫）")]
        public static void Build()
        {
            if (!Application.isBatchMode && File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("重建測試房間", "將覆寫 FireRoom 的手動修改，是否繼續？", "重建", "取消")) return;
            Directory.CreateDirectory("Assets/FireRescue/Scenes");
            Directory.CreateDirectory(MaterialsPath);
            AssetDatabase.Refresh();
            ConfigureLayers();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var map = new GameObject("Test Room").transform;
            Material floor = Material("Floor", new Color(.57f, .5f, .37f));
            Material wall = Material("Wall", new Color(.73f, .76f, .75f));
            Box("Ground", new Vector3(0, -.2f, 0), new Vector3(16, .4f, 17), floor, map);
            Box("Left wall", new Vector3(-6, .55f, 1), new Vector3(.3f, 1.1f, 10), wall, map, 8);
            Box("Right wall", new Vector3(6, .55f, 1), new Vector3(.3f, 1.1f, 10), wall, map, 8);
            Box("Back wall", new Vector3(0, .55f, 6), new Vector3(12, 1.1f, .3f), wall, map, 8);
            Box("Front left", new Vector3(-3.8f, .55f, -4), new Vector3(4.4f, 1.1f, .3f), wall, map, 8);
            Box("Front right", new Vector3(3.8f, .55f, -4), new Vector3(4.4f, 1.1f, .3f), wall, map, 8);
            Box("Yard left boundary", new Vector3(-7.5f, .35f, -4.8f), new Vector3(.3f, .7f, 5), wall, map, 8);
            Box("Yard right boundary", new Vector3(7.5f, .35f, -4.8f), new Vector3(.3f, .7f, 5), wall, map, 8);
            Box("Yard front boundary", new Vector3(0, .35f, -7.25f), new Vector3(15, .7f, .3f), wall, map, 8);
            Material furniture = Material("Obstacle", new Color(.32f, .35f, .36f));
            Box("Work bench", new Vector3(-3, .6f, 3), new Vector3(2, 1.2f, 1.3f), furniture, map, 8);
            Box("Storage", new Vector3(3, .75f, 2.5f), new Vector3(1.3f, 1.5f, 2), furniture, map, 8);
            var crate = Box("Pushable crate", new Vector3(2, .45f, -1.5f), new Vector3(.85f, .9f, .85f),
                Material("Crate", new Color(.7f, .4f, .13f)), map, 10);
            var rigid = crate.AddComponent<Rigidbody>();
            rigid.mass = 2;
            rigid.linearDamping = 1.5f;
            rigid.constraints = RigidbodyConstraints.FreezeRotation;

            var field = new GameObject("Fire Simulation").AddComponent<FireField>();
            field.Configure(new Vector3(-6, 0, -7), 12, 14, 1);
            Physics.SyncTransforms();
            var noFuel = new List<Vector2Int>();
            for (int z = 0; z < field.Height; z++) for (int x = 0; x < field.Width; x++)
                if (z < 3 || z >= 13 || Physics.CheckBox(field.CellCenter(x, z) + Vector3.up * .5f,
                    new Vector3(.4f, .4f, .4f), Quaternion.identity, 1 << 8)) noFuel.Add(new Vector2Int(x, z));
            field.SetNonFlammableCells(noFuel.ToArray());
            var seeds = new List<Vector2Int>();
            for (int x = 1; x <= 10; x++) seeds.Add(new Vector2Int(x, 7));
            field.SetIgnitionSeeds(seeds.ToArray());
            field.ResetSimulation();
            field.gameObject.AddComponent<FireFieldView>();
            var reactions = new GameObject("Element Reactions").AddComponent<ElementReaction>();
            reactions.Initialize(field);

            FluidSource water = Source("Water source", new Vector3(-1.8f, 0, -5.8f), ElementKind.Water, new Color(.05f, .5f, .9f));
            FluidSource oil = Source("Oil source", new Vector3(1.8f, 0, -5.8f), ElementKind.Oil, new Color(.22f, .12f, .035f));
            var exit = Box("Exit", new Vector3(.5f, .015f, -6), new Vector3(2, .03f, 1.5f),
                Material("Exit", new Color(.12f, .7f, .3f)), map);
            Object.DestroyImmediate(exit.GetComponent<Collider>());
            var npcObject = Actor("Survivor", new Vector3(.5f, 0, 4.5f), Material("Survivor", Color.yellow), 11);
            var npc = npcObject.AddComponent<RescueNpc>();
            npc.Initialize(field, exit.transform);

            var player = Actor("Player 1", new Vector3(0, 0, -5.5f), Material("Firefighter", new Color(.92f, .22f, .1f)), 9);
            var hose = player.AddComponent<HoseConnection>();
            var motor = player.AddComponent<FirefighterMotor>();
            motor.Initialize(field, hose);
            var emitter = player.AddComponent<SprayEmitter>();
            emitter.Initialize(hose, reactions);
            player.AddComponent<HoseView>().Initialize(emitter);
            var nozzle = Box("Nozzle", new Vector3(0, .9f, .55f), new Vector3(.16f, .16f, .42f), furniture, player.transform);
            nozzle.transform.localPosition = new Vector3(0, .9f, .55f);
            Object.DestroyImmediate(nozzle.GetComponent<Collider>());

            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 7.5f;
            camera.farClipPlane = 80;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.15f, .2f, .23f);
            camera.gameObject.AddComponent<AudioListener>();
            camera.gameObject.AddComponent<RescueCamera>().Initialize(player.transform);
            player.AddComponent<LocalPlayerInput>().Initialize(motor, hose, emitter, camera,
                new[] { water, oil }, new[] { npc });
            new GameObject("Game Manager").AddComponent<RescueGameManager>().Initialize(field, new[] { npc });
            var light = new GameObject("Daylight").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(55, -25, 0);
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.65f, .67f, .7f);
            PlayerSettings.productName = "Fire Rescue Prototype";
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("FIRE_SETUP_PASS: 1 player, 2 sources, hose, fire field, pushable crate, 1 self-evacuating NPC; no UI or networking.");
        }

        static FluidSource Source(string name, Vector3 position, ElementKind kind, Color color)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            var source = root.AddComponent<FluidSource>();
            source.Configure(kind);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Tank";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.up * .45f;
            visual.transform.localScale = new Vector3(.7f, .45f, .7f);
            visual.GetComponent<Renderer>().sharedMaterial = Material(name, color);
            return source;
        }

        static GameObject Actor(string name, Vector3 position, Material material, int layer)
        {
            var root = new GameObject(name);
            root.layer = layer;
            root.transform.position = position;
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.6f;
            controller.radius = .28f;
            controller.center = Vector3.up * .8f;
            controller.stepOffset = .2f;
            controller.skinWidth = .025f;
            controller.minMoveDistance = 0;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up * .8f;
            body.transform.localScale = new Vector3(.56f, .8f, .56f);
            body.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(body.GetComponent<Collider>());
            return root;
        }

        static GameObject Box(string name, Vector3 position, Vector3 size, Material material, Transform parent, int layer = 0)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.layer = layer;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        static Material Material(string name, Color color)
        {
            string path = MaterialsPath + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
            material.color = color;
            material.SetFloat("_Glossiness", .12f);
            return material;
        }

        static void ConfigureLayers()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = settings.FindProperty("layers");
            string[] names = { "FixedObstacle", "Firefighter", "Pushable", "Survivor" };
            for (int i = 0; i < names.Length; i++) layers.GetArrayElementAtIndex(8 + i).stringValue = names[i];
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
