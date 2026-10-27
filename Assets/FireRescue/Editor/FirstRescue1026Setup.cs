using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FireRescue.Editor
{
    // A separate focused scene. Historical tutorial maps and source assets are never overwritten.
    public static class FirstRescue1026Setup
    {
        public const string ScenePath = "Assets/FireRescue/Scenes/FirstRescue1026.unity";
        public const string Folder = "Assets/FireRescue/Art/UserProvided/Level1026";
        public const string Evidence = "Docs/Validation/2026-10-27-first-rescue-1026";
        public static readonly Vector3 Spawn = new Vector3(7.1f, .1f, -4f);
        public static readonly Vector3 Toilet = new Vector3(.24f, .1f, 1.8f);
        public static readonly Vector3 Guide = new Vector3(.24f, .1f, .82f);
        public static readonly Vector3 SprayPosition = new Vector3(3.15f, .1f, -.6f);
        public static readonly Vector3 FireTarget = new Vector3(5.27f, .1f, 1.12f);
        [Serializable] public sealed class Data { public Part[] meshes; }
        [Serializable] public sealed class Part { public string name, kind; public bool controlGeometry; public Vector3[] vertices; public int[] triangles; }

        public static void BatchInstall()
        {
            try { Install(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        public static void BatchBuildLatest()
        {
            Directory.CreateDirectory("Builds/Latest");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Latest/FireRescue.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText(Evidence+"/build.txt",report.summary.result+"\nErrors="+report.summary.totalErrors+" Warnings="+report.summary.totalWarnings+"\nScene="+ScenePath);
            File.WriteAllText("Builds/Latest/BUILD_INFO.txt","炎上消防隊｜2026-10-27 第一人馬桶水救援\nScene: "+ScenePath+"\n5m hose / physical water / user-provided house, characters and tools\n");
            EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
        }
        public static void BatchPresentationAndAudit()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(ScenePath);
                var root=GameObject.Find("First Rescue 1026");
                root.GetComponentInChildren<Light>().intensity=.85f;
                RenderSettings.ambientLight=new Color(.34f,.36f,.38f);
                var components=root.GetComponentsInChildren<Component>(true);
                Need(components.All(c=>c),"Missing scene scripts.");
                Need(components.OfType<MeshFilter>().All(f=>f.sharedMesh),"Missing mesh.");
                Need(components.OfType<SkinnedMeshRenderer>().All(s=>s.sharedMesh&&s.bones.All(b=>b)),"Missing skin/bone.");
                var toilet=root.GetComponentInChildren<FluidSource>().transform;
                foreach(var mesh in root.GetComponentsInChildren<MeshFilter>())
                {
                    string path=AssetDatabase.GetAssetPath(mesh.sharedMesh);
                    Need(!path.Contains("/AIGenerated/")||mesh.transform.IsChildOf(toilet),"Unrequested generated furniture: "+path);
                }
                Need(!root.GetComponentsInChildren<TextMesh>().Any(),"Persistent world text included.");
                Need(EditorSceneManager.SaveScene(scene),"Scene presentation save failed.");
                File.WriteAllText(Evidence+"/references.txt","FIRST_RESCUE_REFERENCE_PASS\nMissing scripts, mesh and bone references: 0\nActive generated model geometry: toilet only.\nSource BLEND unmodified; complete original collision + cutaway wall view.\nNo persistent world text / old fire-grid view.\n");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        public static void BatchProbeDoors()
        {
            EditorSceneManager.OpenScene(ScenePath);Physics.SyncTransforms();
            var report=new List<string>();
            foreach(var p in new[]{new Vector3(.24f,.6f,.82f),new Vector3(5.27f,.6f,.82f),new Vector3(6.26f,.6f,-4)})
            {
                report.Add("OPENING "+p);
                foreach(var c in Physics.OverlapSphere(p,.1f,1<<8))report.Add("Overlap "+c.name+" active="+c.gameObject.activeInHierarchy);
                foreach(var h in Physics.RaycastAll(p+Vector3.back,Vector3.forward,2,1<<8))report.Add("Ray "+h.collider.name+" at "+h.point);
            }
            File.WriteAllLines(Evidence+"/door-probe.txt",report);EditorApplication.Exit(0);
        }
        public static void BatchRefreshWall()
        {
            try
            {
                var data=JsonUtility.FromJson<Data>(File.ReadAllText("ArtSource/UserProvided/Level1026/level1026.geometry.json"));
                var part=data.meshes.Single(p=>p.kind=="wall");
                foreach(bool cut in new[]{false,true})
                {
                    var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/wall"+(cut?"-Cutaway":"-Full")+".asset");
                    Need(mesh,"Missing derived wall asset.");mesh.Clear();mesh.vertices=part.vertices.Select(v=>cut?new Vector3(v.x,Mathf.Min(v.y,.72f),v.z):v).ToArray();mesh.triangles=part.triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                }
                AssetDatabase.SaveAssets();
                var scene=EditorSceneManager.OpenScene(ScenePath);Physics.SyncTransforms();
                AuthorFireGrid(GameObject.Find("First Rescue 1026").GetComponentInChildren<FireField>());
                Need(EditorSceneManager.SaveScene(scene),"Could not save aligned first-rescue fire grid.");
                string oldProfile=Folder+"/ToiletWater.asset";
                if(AssetDatabase.LoadAssetAtPath<SprayProfile>(oldProfile))Need(string.IsNullOrEmpty(AssetDatabase.MoveAsset(oldProfile,"Assets/FireRescue/Settings/FirstRescueToiletWater.asset")),"Could not categorize water gameplay settings.");
                AssetDatabase.SaveAssets();BatchProbeDoors();
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        public static void Install()
        {
            Directory.CreateDirectory(Evidence);
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Focused scene already exists; refusing to overwrite authored changes.");
            var old = EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            var layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
            var player = UnityEngine.Object.Instantiate(layout.GetComponentInChildren<FirefighterMotor>().gameObject);
            var npc = UnityEngine.Object.Instantiate(layout.GetComponentInChildren<RescueNpc>().gameObject);
            var source = UnityEngine.Object.Instantiate(layout.GetComponentsInChildren<FluidSource>().Single(s => s.Element == ElementKind.Water).gameObject);
            var camera = UnityEngine.Object.Instantiate(Camera.main.gameObject);
            var light = UnityEngine.Object.Instantiate(UnityEngine.Object.FindAnyObjectByType<Light>().gameObject);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root = new GameObject("First Rescue 1026");
            foreach (var go in new[] { player, npc, source, camera, light })
            { SceneManager.MoveGameObjectToScene(go, scene); go.transform.SetParent(root.transform, true); }
            EditorSceneManager.CloseScene(old, true);
            player.name = "Player"; npc.name = "First Resident"; source.name = "Toilet Water Source";
            camera.name = "Main Camera"; light.name = "Sun";
            player.transform.SetPositionAndRotation(Spawn, Quaternion.LookRotation(Vector3.left));
            npc.transform.SetPositionAndRotation(new Vector3(3.25f, .1f, 3.2f), Quaternion.identity);
            source.transform.SetPositionAndRotation(Toilet, Quaternion.Euler(0,180,0));
            EnsureFolder(Folder);
            var data = JsonUtility.FromJson<Data>(File.ReadAllText("ArtSource/UserProvided/Level1026/level1026.geometry.json"));
            Need(data.meshes.Length == 40, "Expected the actual 40-part source model.");
            var architecture = Child(root.transform, "User House 1026");
            var wallMaterial = Mat("Wall", new Color(.72f,.7f,.64f));
            var wood = Mat("Door", new Color(.48f,.31f,.17f));
            var metal = Mat("Equipment", new Color(.65f,.69f,.7f));
            var walls = new List<Collider>();
            foreach (var part in data.meshes)
            {
                if (part.controlGeometry || part.name.StartsWith("CTRL_")) continue; // Blender Boolean helpers are not furniture.
                var owner = Child(architecture, part.name).gameObject; owner.layer = 8;
                bool openDoor = part.kind == "door" || part.kind == "handle";
                // This rescue slice starts with open doors; no substitute AI door mesh is introduced.
                owner.SetActive(!openDoor);
                var collider = owner.AddComponent<MeshCollider>(); collider.sharedMesh = MeshAsset(part, false);
                var visual = Child(owner.transform,"Visual").gameObject;
                visual.AddComponent<MeshFilter>().sharedMesh = MeshAsset(part, part.kind == "wall");
                visual.AddComponent<MeshRenderer>().sharedMaterial = part.kind == "door" ? wood : part.kind == "wall" ? wallMaterial : metal;
                if (part.kind == "wall") walls.Add(collider);
            }
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Ground"; floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3(0,-.06f,0); floor.transform.localScale = new Vector3(20,.3f,20);
            floor.GetComponent<Renderer>().sharedMaterial = Mat("Ground",new Color(.34f,.39f,.36f));
            // Floor tints are not gameplay collision. They make the source floor plan readable.
            Floor(root.transform,"Living floor",new Vector3(2.8f,.095f,-2.5f),new Vector3(7,.01f,6.3f),new Color(.64f,.58f,.48f));
            Floor(root.transform,"Bedroom floor",new Vector3(3.55f,.096f,3.35f),new Vector3(5.3f,.01f,5.1f),new Color(.61f,.57f,.51f));
            Floor(root.transform,"Bathroom floor",new Vector3(-.7f,.097f,2.8f),new Vector3(2.8f,.01f,3.8f),new Color(.49f,.66f,.68f));
            Floor(root.transform,"Kitchen floor",new Vector3(-3.8f,.096f,-2.5f),new Vector3(4.1f,.01f,6.4f),new Color(.62f,.6f,.54f));

            var system = Child(root.transform,"Gameplay").gameObject;
            var field = system.AddComponent<FireField>();
            Physics.SyncTransforms();
            AuthorFireGrid(field);
            var reaction=system.AddComponent<ElementReaction>(); reaction.Initialize(field);
            var exit=Child(root.transform,"Rescue Exit"); exit.position=new Vector3(7.1f,.1f,-4f);
            var guideObject=Child(root.transform,"Bathroom doorway hose guide");guideObject.position=Guide;
            var route=Child(guideObject,"Route");route.localPosition=Vector3.up*.15f;
            var interact=Child(guideObject,"Interaction");interact.localPosition=Vector3.back*.25f;
            var guide=guideObject.gameObject.AddComponent<HoseWindow>();guide.Initialize(route,interact);
            // A real door opening already exists. Do NOT grant a virtual spray hole in adjacent walls.
            var geometry=root.AddComponent<SprayWallOcclusion>();geometry.Configure(walls.ToArray(),Array.Empty<HoseWindow>());
            var hose=player.GetComponent<HoseConnection>();hose.Disconnect();hose.ConfigurePhysics(geometry,player.transform.Find("Anchors/Hose Inlet"));
            var emitter=player.GetComponent<SprayEmitter>();emitter.Initialize(hose,reaction);emitter.EnableDirectionalSpray(true);emitter.SetWallOcclusion(geometry);emitter.SetGroundHeight(.1f);
            player.GetComponent<FirefighterMotor>().Initialize(field,hose);
            var input=player.GetComponent<LocalPlayerInput>();
            input.Initialize(player.GetComponent<FirefighterMotor>(),hose,emitter,camera.GetComponent<Camera>(),new[]{source.GetComponent<FluidSource>()},Array.Empty<RescueNpc>());
            input.SetLocomotionFacingDuringPointerInput(true);
            var inputData=new SerializedObject(input);var windows=inputData.FindProperty("hoseWindows");windows.arraySize=1;windows.GetArrayElementAtIndex(0).objectReferenceValue=guide;inputData.ApplyModifiedPropertiesWithoutUndo();
            var highlight=player.GetComponentInChildren<SourceHighlightView>();if(highlight)highlight.Initialize(input,hose);
            var oldParticles=player.GetComponentInChildren<SprayParticleView>();if(oldParticles)UnityEngine.Object.DestroyImmediate(oldParticles);
            var effects=system.AddComponent<FirstRescueEffects>();effects.Initialize(emitter,field);
            npc.GetComponent<RescueNpc>().Initialize(field,exit,Array.Empty<ObjectFire>());
            var manager=system.AddComponent<RescueGameManager>();manager.Initialize(field,new[]{npc.GetComponent<RescueNpc>()},Array.Empty<ObjectFire>());
            const string profilePath="Assets/FireRescue/Settings/FirstRescueToiletWater.asset";
            var profile=AssetDatabase.LoadAssetAtPath<SprayProfile>(profilePath);
            if(!profile){profile=ScriptableObject.CreateInstance<SprayProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
            var settings=SpraySettings.DefaultFor(ElementKind.Water);settings.PhysicalBallistics=true;settings.MuzzleSpeed=10.5f;settings.LaunchAngle=-10;settings.Gravity=9.81f;settings.AirDrag=.08f;
            settings.AirLifetime=1.3f;settings.SpreadAngle=6;settings.CollisionRadius=.055f;settings.ImpactRadius=.8f;settings.GroundLifetime=8;
            profile.Configure(settings);profile.ConfigureEffects(SprayEffectSettings.Neutral);EditorUtility.SetDirty(profile);
            source.GetComponent<FluidSource>().SetSprayProfile(profile);source.GetComponent<FluidSource>().Configure(ElementKind.Water,3);
            camera.GetComponent<RescueCamera>().Initialize(player.transform);camera.GetComponent<RescueCamera>().ConfigureView(50,0);
            camera.GetComponent<Camera>().orthographicSize=6;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.62f,.65f,.68f);
            Physics.SyncTransforms();
            Need(!root.GetComponentsInChildren<FireFieldView>().Any(),"Old grid cube view must not be included.");
            Need(root.GetComponentsInChildren<FluidSource>().Length==1 && hose.MaximumLength==5,"One toilet water source / 5m hose.");
            AssetDatabase.SaveAssets();Need(EditorSceneManager.SaveScene(scene,ScenePath),"Scene save failed.");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).Select(s=>new EditorBuildSettingsScene(s.path,false))).ToArray();
            File.WriteAllText(Evidence+"/install.txt","FIRST_RESCUE_INSTALL_PASS\n40 source meshes; Boolean helpers excluded; original doors authored open; independent full collision + cutaway visual.\nOne toilet source, user characters/tools, no AI furnishings. Hose 5m, explicit bathroom door guide.\nFire seeds="+field.BurningCount+"\nSource SHA256 d68d113f535f89d4dc28246005413e4f14746c537dfea0921dd3321d44cf142c\n");
        }
        static void AuthorFireGrid(FireField field)
        {
            // Centres on .25m multiples align with measured .24m bathroom / 5.27m bedroom doors.
            // This avoids a grid node sitting too close to a jamb despite the physical door being wide enough.
            field.Configure(new Vector3(-8.125f,.1f,-8.125f),64,64,.25f);
            var nonflammable=new List<Vector2Int>();var seeds=new List<Vector2Int>();
            for(int z=0;z<field.Height;z++)for(int x=0;x<field.Width;x++)
            {
                var p=field.CellCenter(x,z);
                bool doorway=p.x>4.7f&&p.x<5.9f&&p.z>.5f&&p.z<1.4f;
                bool desk=p.x>5.2f&&p.x<5.8f&&p.z>5f&&p.z<5.6f;
                if((!doorway&&!desk)||Physics.CheckBox(p+Vector3.up*.35f,new Vector3(.1f,.3f,.1f),Quaternion.identity,1<<8))nonflammable.Add(new Vector2Int(x,z));
                else seeds.Add(new Vector2Int(x,z));
            }
            field.SetIgnitionSeeds(seeds.ToArray());field.SetNonFlammableCells(nonflammable.ToArray());
        }
        static Mesh MeshAsset(Part part,bool cut)
        {
            string path=Folder+"/"+part.name+(cut?"-Cutaway":"-Full")+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing)return existing;
            var vertices=part.vertices.Select(v=>cut?new Vector3(v.x,Mathf.Min(v.y,.72f),v.z):v).ToArray();
            var mesh=new Mesh{name=part.name,vertices=vertices,triangles=part.triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static void Floor(Transform root,string name,Vector3 p,Vector3 size,Color color)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root);go.transform.position=p;go.transform.localScale=size;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=Mat(name,color);}
        static Material Mat(string name,Color color)
        {string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(Shader.Find("Standard")){name=name,color=color};AssetDatabase.CreateAsset(m,path);return m;}
        static Transform Child(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static void EnsureFolder(string path){if(AssetDatabase.IsValidFolder(path))return;EnsureFolder(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}
        static void Need(bool v,string message){if(!v)throw new InvalidOperationException(message);}
    }
}
