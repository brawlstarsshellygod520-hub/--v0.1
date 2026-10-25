using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace FireRescue.Editor
{
    public static class ResidentialPrototypeBuilder
    {
        public const string ScenePath = "Assets/FireRescue/Scenes/ResidentialFire.unity";
        public static readonly Vector3 WaterApproach = new Vector3(5.5f, 0, -3.5f);
        public static readonly Vector3 OilApproach = new Vector3(-4.8f, 0, -.9f);
        public static readonly Vector3 NpcApproach = new Vector3(-.5f, 0, 4.7f);
        public static readonly Vector3 DoorFire = new Vector3(-.5f, 0, 3.5f);
        public static readonly Vector3 SecondFire = new Vector3(-.5f, 0, 4.5f);
        static Material floor, wall, wood;

        [MenuItem("消防 Prototype/開啟住宅場景")]
        public static void Open()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("消防 Prototype/建置住宅與模型（覆寫住宅場景）")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("建置住宅", "會覆寫住宅場景，舊 FireRoom 保留。", "建置", "取消")) return;
            }
            ResidentialArtCatalog.Prepare();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var house = new GameObject("Three-bedroom House").transform;
            foreach (string name in new[] { "Bedroom 1", "Bedroom 2", "Bedroom 3", "Living Room", "Bathroom", "Kitchen" })
                new GameObject(name).transform.SetParent(house, false);
            floor = ResidentialArtCatalog.Material("HouseFloor", new Color(.72f,.66f,.54f));
            wall = ResidentialArtCatalog.Material("HouseWall", new Color(.66f,.69f,.70f));
            wood = ResidentialArtCatalog.Material("DoorWood", new Color(.71f,.45f,.23f));
            Block("Ground", new Vector3(0, -.15f, 1), new Vector3(18, .3f, 20), floor, house, 0);
            var bathroomFloor = Block("Bathroom floor", new Vector3(5.5f,.012f,-2), new Vector3(4.8f,.025f,5.8f),
                ResidentialArtCatalog.Material("BathroomFloor", new Color(.43f,.57f,.61f)), house, 0);
            UnityEngine.Object.DestroyImmediate(bathroomFloor.GetComponent<Collider>());
            // Thickness overlaps the existing NPC grid-clearance probe even between cell centres.
            Wall("West wall", new Vector3(-8,.36f,2), new Vector3(.32f,.72f,14), house);
            Wall("East wall", new Vector3(8,.36f,2), new Vector3(.32f,.72f,14), house);
            Wall("Back wall", new Vector3(0,.36f,9), new Vector3(16,.72f,.32f), house);
            Gap("Entrance", -8, 8, -5, .5f, 2.4f, house);
            Wall("Bedroom partition 1", new Vector3(-2.65f,.36f,6), new Vector3(.32f,.72f,6), house);
            Wall("Bedroom partition 2", new Vector3(2.65f,.36f,6), new Vector3(.32f,.72f,6), house);
            Gap("Bedroom 1 door", -8, -2.65f, 3, -4.5f, 1.8f, house);
            Gap("Bedroom 2 door", -2.65f, 2.65f, 3, -.5f, 1.8f, house);
            Gap("Bedroom 3 door", 2.65f, 8, 3, 5.5f, 1.8f, house);
            Gap("Living room opening", -8, 3, 1, .5f, 3, house, false);
            Gap("Bathroom door", 3, 8, 1, 5.5f, 1.8f, house);
            Wall("Bathroom partition", new Vector3(3,.36f,-2), new Vector3(.32f,.72f,6), house);

            Furniture("Bed 1", "Bed", new Vector3(-5.7f,0,7.2f), house);
            Furniture("Bed 2", "Bed", new Vector3(.5f,0,7.2f), house);
            Furniture("Bed 3", "Bed", new Vector3(5.5f,0,7.2f), house);
            Furniture("Sofa", "Sofa", new Vector3(-3.8f,0,-1.8f), house);
            var table = Furniture("Coffee Table", "CoffeeTable", new Vector3(-3.8f,0,-3.2f), house, 10);
            var rigid = table.AddComponent<Rigidbody>();
            rigid.mass = 2; rigid.linearDamping = 1.5f; rigid.constraints = RigidbodyConstraints.FreezeRotation;
            Furniture("Kitchen counter", "KitchenCounter", new Vector3(-6,0,.15f), house);
            Furniture("Bathroom sink", "Sink", new Vector3(3.85f,0,-2.9f), house);
            Furniture("Shower", "Shower", new Vector3(6.8f,0,-.1f), house);
            var toilet = Furniture("Toilet cistern water source", "Toilet", new Vector3(6.6f,0,-3.7f), house);
            var water = toilet.AddComponent<FluidSource>(); water.Configure(ElementKind.Water);
            var port = new GameObject("Cistern outlet").transform; port.SetParent(toilet.transform, false);
            bool hasTankWater = false;
            foreach (Renderer renderer in toilet.GetComponentsInChildren<Renderer>())
                if (renderer.name == "Tank_Water")
                {
                    port.position = renderer.bounds.center + Vector3.up * .04f;
                    hasTankWater = true;
                }
            if (!hasTankWater) throw new InvalidOperationException("Toilet model has no Tank_Water outlet mesh.");
            water.SetOutlet(port);
            var bottle = Furniture("Salad oil source", "SaladOil", new Vector3(-5.02f,0,.15f), house, 8, .94f);
            var oil = bottle.AddComponent<FluidSource>(); oil.Configure(ElementKind.Oil);
            port = new GameObject("Bottle mouth").transform; port.SetParent(bottle.transform, false);
            Bounds oilBounds = ResidentialArtCatalog.BoundsOf(bottle);
            port.position = new Vector3(oilBounds.center.x, oilBounds.max.y, oilBounds.center.z);
            oil.SetOutlet(port);

            var field = new GameObject("Fire Simulation").AddComponent<FireField>();
            field.Configure(new Vector3(-8,0,-8), 16, 18, 1);
            Physics.SyncTransforms();
            var noFuel = new List<Vector2Int>();
            for (int z = 0; z < field.Height; z++) for (int x = 0; x < field.Width; x++)
            {
                Vector3 p = field.CellCenter(x,z);
                if (p.z < -5 || p.z >= 9 || (p.x > 3 && p.z < 1) ||
                    Physics.CheckBox(p + Vector3.up * .5f, new Vector3(.4f,.4f,.4f), Quaternion.identity, 1 << 8))
                    noFuel.Add(new Vector2Int(x,z));
            }
            field.SetNonFlammableCells(noFuel.ToArray());
            field.TryGetCell(DoorFire, out Vector2Int doorSeed);
            field.TryGetCell(SecondFire, out Vector2Int secondSeed);
            field.SetIgnitionSeeds(new[] { doorSeed, secondSeed }); field.ResetSimulation();
            field.gameObject.AddComponent<FireFieldView>();
            var reactions = new GameObject("Element Reactions").AddComponent<ElementReaction>(); reactions.Initialize(field);
            var exit = new GameObject("Outdoor exit").transform; exit.position = new Vector3(.5f,0,-6.5f);
            var npcObject = Actor("Resident NPC", "Resident", new Vector3(.5f,0,5.5f), 11);
            var npc = npcObject.AddComponent<RescueNpc>(); npc.Initialize(field, exit);
            var npcSettings = new SerializedObject(npc);
            npcSettings.FindProperty("tintBodyByState").boolValue = false;
            npcSettings.ApplyModifiedPropertiesWithoutUndo();
            var player = Actor("Player 1", "Firefighter", exit.position, 9);
            var hose = player.AddComponent<HoseConnection>();
            var motor = player.AddComponent<FirefighterMotor>(); motor.Initialize(field, hose);
            var emitter = player.AddComponent<SprayEmitter>(); emitter.Initialize(hose, reactions);
            player.AddComponent<HoseView>().Initialize(emitter);
            var nozzle = ResidentialArtCatalog.Visual("Nozzle", player.transform);
            nozzle.transform.localPosition = new Vector3(0,.8f,.1f);
            CharacterArtSetup.MountNozzlePresentation(player, player.transform.Find("Firefighter").gameObject);
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 5.7f; camera.farClipPlane = 80;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.78f,.77f,.71f);
            camera.gameObject.AddComponent<AudioListener>();
            camera.gameObject.AddComponent<RescueCamera>().Initialize(player.transform);
            player.AddComponent<LocalPlayerInput>().Initialize(motor, hose, emitter, camera, new[] { water, oil }, new[] { npc });
            new GameObject("Game Manager").AddComponent<RescueGameManager>().Initialize(field, new[] { npc });
            var light = new GameObject("Daylight").AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.1f; light.transform.rotation = Quaternion.Euler(55,-25,0); light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.65f,.65f,.62f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            ResidentialArtPreview.CaptureHouse();
            Debug.Log("RESIDENTIAL_SETUP_PASS: 12 FBX/prefabs, 3 bedrooms, living room, bathroom, open kitchen; household water/oil sources; gameplay retained.");
        }

        static void Gap(string name, float min, float max, float z, float center, float width, Transform parent, bool door = true)
        {
            float left = center - width*.5f, right = center + width*.5f;
            Wall(name+" left", new Vector3((min+left)*.5f,.36f,z), new Vector3(left-min,.72f,.32f), parent);
            Wall(name+" right", new Vector3((max+right)*.5f,.36f,z), new Vector3(max-right,.72f,.32f), parent);
            if (door) Block(name+" fixed-open leaf", new Vector3(right+.1f,.33f,z+.65f), new Vector3(.1f,.66f,1.2f), wood,parent,8);
        }
        static void Wall(string name, Vector3 p, Vector3 size, Transform parent) => Block(name,p,size,wall,parent,8);
        static GameObject Block(string name, Vector3 p, Vector3 size, Material mat, Transform parent, int layer)
        {
            var root = new GameObject(name); root.transform.SetParent(parent,false); root.transform.position=p; root.layer=layer;
            // Scale in Unity's world-aligned axes, outside the FBX coordinate-conversion rotation.
            var scale = new GameObject("Visual Scale").transform; scale.SetParent(root.transform, false);
            scale.localScale = size; scale.localPosition = Vector3.down * size.y * .5f;
            var visual = ResidentialArtCatalog.Visual("Block",scale);
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial=mat;
            root.AddComponent<BoxCollider>().size=size;
            return root;
        }
        static GameObject Furniture(string name,string asset,Vector3 p,Transform parent,int layer=8,float visualLift=0)
        {
            var root = new GameObject(name); root.layer=layer; root.transform.SetParent(parent,false); root.transform.position=p;
            var visual = ResidentialArtCatalog.Visual(asset,root.transform);
            visual.transform.localPosition = Vector3.up * visualLift;
            Bounds bounds=ResidentialArtCatalog.BoundsOf(root);
            var box=root.AddComponent<BoxCollider>(); box.center=bounds.center-p; box.size=bounds.size;
            return root;
        }
        static GameObject Actor(string name,string asset,Vector3 p,int layer)
        {
            var root=new GameObject(name); root.layer=layer; root.transform.position=p;
            var controller=root.AddComponent<CharacterController>();
            controller.height=1.6f; controller.radius=.28f; controller.center=Vector3.up*.8f;
            controller.stepOffset=.2f; controller.skinWidth=.025f; controller.minMoveDistance=0;
            var visual = ResidentialArtCatalog.Visual(asset,root.transform);
            CharacterArtSetup.AttachPresentation(root, visual, asset == "Firefighter" ? 4 : 2.15f);
            return root;
        }
    }
}
