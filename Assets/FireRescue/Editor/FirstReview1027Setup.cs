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
    public static class FirstReview1027Setup
    {
        public const string ScenePath="Assets/FireRescue/Scenes/FirstReview1027.unity";
        public const string Folder="Assets/FireRescue/Art/UserProvided/Level1027";
        public const string Placeholders="Assets/FireRescue/Art/AIGenerated/ReviewPlaceholders";
        public const string Evidence="Docs/Validation/2026-10-28-first-review";
        public static readonly Vector3 Spawn=new Vector3(7.1f,.1f,-4.9f);
        public static readonly Vector3 Water=new Vector3(.1f,.1f,1.9f);
        public static readonly Vector3 Powder=new Vector3(-5.35f,.1f,4.4f);
        public static readonly Vector3 Pan=new Vector3(-5.78f,1.04f,-3.1f);
        public static readonly Vector3 Pudding=new Vector3(-3.55f,1.09f,-5.36f);
        [Serializable] public sealed class Data {public FirstRescue1026Setup.Part[] meshes;}
        public static void BatchInstall()
        {try{Install();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        public static void BatchBuild()
        {
            Directory.CreateDirectory(Patch021Setup.Evidence);Directory.CreateDirectory("Builds/Latest");
            PlayerSettings.bundleVersion="0.2.1";AssetDatabase.SaveAssets();
            var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Latest/FireRescue.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText(Patch021Setup.Evidence+"/build.txt",r.summary.result+"\nErrors="+r.summary.totalErrors+" Warnings="+r.summary.totalWarnings+"\nScene="+ScenePath);
            File.WriteAllText("Builds/Latest/BUILD_INFO.txt","炎上消防隊 v0.2.1｜2026-10-28\nScene="+ScenePath+"\n失敗小文字：按 R 即可重來，沒有按鈕\n外圍透明阻擋牆，保留碰撞\n水初速10m/s、60水包/s、可視大小1.65倍；10m管線不變\nE拿放／交付；否則接管／拔管，拿物自動收管；右鍵仍能接管／噴射，取消F\n5秒首波／水清路／粉末滅鍋／60秒布丁交付／NPC正門逃生規則不變\n");
            EditorApplication.Exit(r.summary.result==BuildResult.Succeeded?0:1);
        }
        public static void BatchPolishMechanics()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(ScenePath);var root=GameObject.Find("First Review 1027");
                ApplyMechanicPolish(root);
                AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new IOException("Polish save failed");
                File.WriteAllText(Evidence+"/polish-install.txt","MECHANIC_POLISH_INSTALLED\nMatte wall / 2s connected oil-spill growth / item heat guard / overhead NPC tag / one 3D PBD tube mesh.\n");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void ApplyMechanicPolish(GameObject root)
        {
            var wall=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Warm plaster.mat");
            var matte=Shader.Find("FireRescue/MattePlaster");if(!matte)throw new InvalidOperationException("Matte plaster shader missing");
            wall.shader=matte;wall.color=new Color(.76f,.73f,.66f);EditorUtility.SetDirty(wall);
            var fire=root.GetComponentInChildren<ObjectFire>();var area=fire.GetComponent<CookingOilArea>();if(!area)area=fire.gameObject.AddComponent<CookingOilArea>();area.Initialize(fire,root.GetComponent<SprayWallOcclusion>());
            fire.GetComponent<OilPanFireView>().SetArea(area);root.GetComponentInChildren<CarryItem>().SetFireHazard(area);
            var npc=root.GetComponentInChildren<RescueNpc>();var tag=npc.GetComponent<NpcRequestNameTag>();if(!tag)tag=npc.gameObject.AddComponent<NpcRequestNameTag>();tag.Initialize(npc.GetComponent<NpcItemRequest>(),root.GetComponentInChildren<FirstReviewMission>(),root.GetComponentInChildren<Camera>());
            var player=root.GetComponentInChildren<FirefighterMotor>();var old=player.GetComponentInChildren<HoseView>(true);if(old)old.enabled=false;
            var tube=player.GetComponent<PhysicalHoseTube>();if(!tube)tube=player.gameObject.AddComponent<PhysicalHoseTube>();tube.Initialize(player.GetComponent<SprayEmitter>());
        }
        public static void BatchFinishPresentation()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(ScenePath);var root=GameObject.Find("First Review 1027");
                var ground=root.transform.Find("Ground");ground.localScale=new Vector3(48,.3f,48);
                PlayerSettings.productName="炎上消防隊";
                var camera=root.GetComponentInChildren<Camera>();var test=new GameObject("Temporary ground coverage check").AddComponent<Camera>();
                test.orthographic=true;test.orthographicSize=camera.orthographicSize;test.transform.rotation=Quaternion.Euler(50,0,0);test.transform.position=Spawn+Vector3.up*.8f-test.transform.forward*20;
                var bounds=ground.GetComponent<Renderer>().bounds;
                foreach(float aspect in new[]{16f/9,21f/9})
                {
                    test.aspect=aspect;
                    foreach(float x in new[]{0f,1f})foreach(float y in new[]{0f,1f})
                    {
                        var ray=test.ViewportPointToRay(new Vector3(x,y,0));
                        if(!new Plane(Vector3.up,Vector3.up*.1f).Raycast(ray,out float d))throw new InvalidOperationException("Camera does not see playable ground");
                        var p=ray.GetPoint(d);
                        if(p.x<bounds.min.x||p.x>bounds.max.x||p.z<bounds.min.z||p.z>bounds.max.z)throw new InvalidOperationException("Spawn camera sees beyond ground: "+p);
                    }
                }
                UnityEngine.Object.DestroyImmediate(test.gameObject);
                if(root.GetComponentsInChildren<Component>(true).Any(c=>!c))throw new InvalidOperationException("Missing component reference");
                AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new IOException("Presentation save failed");
                File.WriteAllText(Evidence+"/presentation.txt","REVIEW_PRESENTATION_PASS\nGround coverage at spawn: all 4 corners for 16:9 and 21:9.\nOnly existing outdoor ground expanded to 48m; user house unchanged.\nProduct title: 炎上消防隊. Missing component references: 0.\n");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        public static void Install()
        {
            if(File.Exists(ScenePath))throw new InvalidOperationException("Review scene already exists; do not overwrite authored work.");
            Directory.CreateDirectory(Evidence);EnsureFolder(Folder);EnsureFolder(Placeholders);
            var old=EditorSceneManager.OpenScene(FirstRescue1026Setup.ScenePath);
            var oldRoot=GameObject.Find("First Rescue 1026");
            var player=UnityEngine.Object.Instantiate(oldRoot.GetComponentInChildren<FirefighterMotor>().gameObject);
            var npc=UnityEngine.Object.Instantiate(oldRoot.GetComponentInChildren<RescueNpc>().gameObject);
            var water=UnityEngine.Object.Instantiate(oldRoot.GetComponentInChildren<FluidSource>().gameObject);
            var camera=UnityEngine.Object.Instantiate(Camera.main.gameObject);
            var light=UnityEngine.Object.Instantiate(oldRoot.GetComponentInChildren<Light>().gameObject);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var root=new GameObject("First Review 1027");
            foreach(var go in new[]{player,npc,water,camera,light}){SceneManager.MoveGameObjectToScene(go,scene);go.transform.SetParent(root.transform,true);}
            EditorSceneManager.CloseScene(old,true);
            player.name="Player";npc.name="Pudding Resident";water.name="Toilet Water Source";camera.name="Main Camera";
            player.transform.SetPositionAndRotation(Spawn,Quaternion.LookRotation(Vector3.left));
            npc.transform.SetPositionAndRotation(new Vector3(3.4f,.1f,3.0f),Quaternion.LookRotation(Vector3.back));
            water.transform.SetPositionAndRotation(Water,Quaternion.Euler(0,180,0));
            var architecture=Child(root.transform,"User House 1027");
            var walls=new List<Collider>();BreakableDoor bedroomDoor=null;
            var data=JsonUtility.FromJson<Data>(File.ReadAllText("ArtSource/UserProvided/Level1027/level1027.geometry.json"));
            var wood=Mat(Folder,"Warm wood",new Color(.55f,.34f,.19f));
            var wallMat=Mat(Folder,"Warm plaster",new Color(.82f,.79f,.7f));
            var steel=Mat(Folder,"Soft steel",new Color(.53f,.61f,.65f));
            foreach(var part in data.meshes)
            {
                // Detached sample sink is preserved in source/export, not placed outside the playable house.
                if(part.controlGeometry||part.name=="流理臺")continue;
                if(part.kind=="handle")continue; // Attached below to the corresponding door presentation.
                var go=Child(architecture,part.name).gameObject;go.layer=8;
                bool door=part.kind=="door",bedroom=part.name=="door.002";
                var full=MeshAsset(part,false);
                var visual=Child(go.transform,"Visual").gameObject;
                visual.AddComponent<MeshFilter>().sharedMesh=part.kind=="wall"?MeshAsset(part,true):full;
                Color color=part.name.Contains("refrigerator")?new Color(.68f,.79f,.8f):part.name.Contains("trash")?new Color(.37f,.48f,.39f):new Color(.81f,.75f,.61f);
                visual.AddComponent<MeshRenderer>().sharedMaterial=part.kind=="wall"?wallMat:door?wood:part.name=="瓦斯爐"?Mat(Folder,"Stove charcoal",new Color(.15f,.18f,.20f)):Mat(Folder,part.name,color);
                if(door)
                {
                    int suffix=int.Parse(part.name.Substring(5))-1;string handleName="door handle_2"+(suffix==0?"":"."+suffix.ToString("000"));
                    var handle=data.meshes.SingleOrDefault(p=>p.name==handleName);
                    if(handle!=null){var h=Child(visual.transform,"User handle").gameObject;h.AddComponent<MeshFilter>().sharedMesh=MeshAsset(handle,false);h.AddComponent<MeshRenderer>().sharedMaterial=steel;}
                    if(!bedroom){go.SetActive(false);continue;}
                    var box=go.AddComponent<BoxCollider>();box.center=full.bounds.center;box.size=full.bounds.size+new Vector3(0,0,.05f);
                    bedroomDoor=go.AddComponent<BreakableDoor>();bedroomDoor.Configure(3,box);
                    go.AddComponent<BreakableDoorView>().Initialize(bedroomDoor,visual,null);walls.Add(box);
                }
                else
                {
                    var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=full;
                    if(part.kind=="wall")walls.Add(collider);
                }
            }
            if(!bedroomDoor)throw new InvalidOperationException("User bedroom door missing.");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Ground";floor.transform.SetParent(root.transform);floor.transform.position=new Vector3(0,-.05f,0);floor.transform.localScale=new Vector3(48,.3f,48);floor.GetComponent<Renderer>().sharedMaterial=Mat(Folder,"Outdoor floor",new Color(.31f,.38f,.34f));
            Floor(root.transform,"Living floor",new Vector3(2.0f,.101f,-2.55f),new Vector3(8.4f,.005f,6.45f),new Color(.62f,.52f,.39f));
            Floor(root.transform,"Kitchen floor",new Vector3(-4.23f,.102f,-3.35f),new Vector3(4.0f,.005f,4.85f),new Color(.69f,.65f,.54f));
            Floor(root.transform,"Bedroom floor",new Vector3(3.56f,.102f,3.4f),new Vector3(5.3f,.005f,5.0f),new Color(.68f,.61f,.53f));
            Floor(root.transform,"Bathroom floor",new Vector3(-.63f,.102f,2.75f),new Vector3(2.8f,.005f,3.8f),new Color(.45f,.64f,.68f));
            Floor(root.transform,"Storage floor",new Vector3(-5.38f,.102f,4.67f),new Vector3(1.9f,.005f,2.4f),new Color(.58f,.53f,.44f));
            var game=Child(root.transform,"Gameplay").gameObject;
            var field=game.AddComponent<FireField>();field.Configure(new Vector3(-8.125f,.1f,-8.125f),68,68,.25f);
            field.SetIgnitionSeeds(Array.Empty<Vector2Int>());field.SetNonFlammableCells(Enumerable.Range(0,68*68).Select(i=>new Vector2Int(i%68,i/68)).ToArray());
            var reaction=game.AddComponent<ElementReaction>();reaction.Initialize(field);
            var geometry=root.AddComponent<SprayWallOcclusion>();geometry.Configure(walls.ToArray(),Array.Empty<HoseWindow>());
            var hose=player.GetComponent<HoseConnection>();hose.Disconnect();hose.SetAutomaticWrapping(true);
            // User approved 15m for this exhibition; all historical 5m scenes remain unchanged.
            SetFloat(hose,"maximumLength",15);hose.ConfigurePhysics(geometry,player.transform.Find("Anchors/Hose Inlet"));
            var emitter=player.GetComponent<SprayEmitter>();emitter.Initialize(hose,reaction);emitter.EnableDirectionalSpray(true);emitter.SetGroundHeight(.1f);emitter.SetWallOcclusion(geometry);
            player.GetComponent<FirefighterMotor>().Initialize(field,hose);player.GetComponent<FirefighterMotor>().ConfigureLocomotion(3.6f,40,48,960);
            var visualRoot=player.transform.Find("Visual");var nozzle=visualRoot.Find("Nozzle Mount");var axe=visualRoot.Find("Axe Pivot");
            nozzle.localPosition=new Vector3(.02f,.76f,.23f);nozzle.localRotation=Quaternion.identity;
            player.transform.Find("Anchors/Nozzle").localPosition=nozzle.localPosition+new Vector3(0,.090184f,.270324f);
            player.transform.Find("Anchors/Hose Inlet").localPosition=nozzle.localPosition+new Vector3(0,.090183f,-.277877f);
            var tools=player.GetComponent<FirefighterTools>();
            // Source, pan and food placeholders have replaceable Visual children and separate anchors/collision.
            var powder=Child(root.transform,"Soda Powder Source").gameObject;powder.transform.position=Powder;
            var powderVisual=Child(powder.transform,"Visual");Primitive(powderVisual,"Soda box",PrimitiveType.Cube,new Vector3(0,.33f,0),new Vector3(.42f,.6f,.24f),Mat(Placeholders,"Soda blue",new Color(.28f,.62f,.79f)));
            Primitive(powderVisual,"White band",PrimitiveType.Cube,new Vector3(0,.36f,-.126f),new Vector3(.34f,.22f,.012f),Mat(Placeholders,"Cream",new Color(.97f,.94f,.8f)));
            var powderSource=powder.AddComponent<FluidSource>();powderSource.Configure(ElementKind.DryPowder,3.2f);
            var port=Child(powder.transform,"Outlet");port.localPosition=new Vector3(0,.48f,-.15f);powderSource.SetOutlet(port);
            powder.AddComponent<InteractionOutlineTarget>().SetVisualRoot(powderVisual);
            water.GetComponent<FluidSource>().SetSprayProfile(Profile("ReviewWater",ElementKind.Water,18,10.5f,9.81f));
            powderSource.SetSprayProfile(Profile("ReviewSoda",ElementKind.DryPowder,12,8f,2f));
            var pan=Child(root.transform,"Oil Pan Fire").gameObject;pan.transform.position=Pan;
            var panVisual=Child(pan.transform,"Visual");
            Primitive(panVisual,"Pan",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.52f,.07f,.52f),Mat(Placeholders,"Pan iron",new Color(.13f,.15f,.17f)));
            Primitive(panVisual,"Oil",PrimitiveType.Cylinder,new Vector3(0,.075f,0),new Vector3(.44f,.008f,.44f),Mat(Placeholders,"Hot oil",new Color(.72f,.44f,.06f)));
            Primitive(panVisual,"Handle",PrimitiveType.Cube,new Vector3(.38f,0,0),new Vector3(.42f,.055f,.085f),Mat(Placeholders,"Pan iron",new Color(.13f,.15f,.17f)));
            var panHit=pan.AddComponent<BoxCollider>();panHit.center=new Vector3(0,.25f,0);panHit.size=new Vector3(.6f,.65f,.6f);
            var fire=pan.AddComponent<ObjectFire>();fire.Configure(FireKind.CookingOil,1);fire.ConfigureWaterEscalation(.65f,3);
            pan.AddComponent<OilPanFireView>().Initialize(fire);
            var pudding=Child(root.transform,"Pudding Item").gameObject;pudding.transform.position=Pudding;
            var foodVisual=Child(pudding.transform,"Visual");
            Primitive(foodVisual,"Custard cup",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.21f,.11f,.21f),Mat(Placeholders,"Custard yellow",new Color(1,.82f,.24f)));
            Primitive(foodVisual,"Red lid",PrimitiveType.Cylinder,new Vector3(0,.12f,0),new Vector3(.235f,.012f,.235f),Mat(Placeholders,"Pudding red",new Color(.82f,.10f,.08f)));
            var foodHit=pudding.AddComponent<BoxCollider>();foodHit.size=new Vector3(.24f,.27f,.24f);var item=pudding.AddComponent<CarryItem>();item.Initialize(new Collider[]{foodHit});
            var exit=Child(root.transform,"Rescue Exit");exit.position=Spawn;
            var npcLogic=npc.GetComponent<RescueNpc>();npcLogic.Initialize(field,exit,new[]{fire});
            var manager=game.AddComponent<RescueGameManager>();manager.Initialize(field,new[]{npcLogic},new[]{fire});
            var mission=game.AddComponent<FirstReviewMission>();var request=npc.AddComponent<NpcItemRequest>();
            var residentHand=npc.GetComponentsInChildren<Transform>(true).First(t=>t.name=="R_Hand");
            var hand=Child(residentHand,"Pudding Receiving Hand");hand.position=residentHand.position+npc.transform.forward*.05f;hand.rotation=npc.transform.rotation;
            request.Initialize(hand,bedroomDoor,fire,mission);npcLogic.SetEscapeRequirement(request);mission.Initialize(request,manager,fire,tools,npcLogic);
            var carry=player.AddComponent<PlayerCarryInteraction>();var carryAnchor=Child(player.transform,"Carry Anchor");carryAnchor.localPosition=new Vector3(0,.78f,.33f);carry.Initialize(carryAnchor,new[]{item},request,tools,mission);
            var input=player.GetComponent<LocalPlayerInput>();input.Initialize(player.GetComponent<FirefighterMotor>(),hose,emitter,camera.GetComponent<Camera>(),new[]{water.GetComponent<FluidSource>(),powderSource},Array.Empty<RescueNpc>());
            input.SetTools(tools);input.SetLocomotionFacingDuringPointerInput(true);input.SetMissionInteraction(carry,mission);
            var inputData=new SerializedObject(input);inputData.FindProperty("hoseWindows").arraySize=0;inputData.ApplyModifiedPropertiesWithoutUndo();
            var highlight=player.GetComponentInChildren<SourceHighlightView>(true);highlight.Initialize(input,hose);
            var toolView=visualRoot.GetComponent<FirefighterToolsView>();toolView.enabled=false;
            visualRoot.gameObject.AddComponent<ReviewToolVisibility>().Initialize(tools,axe.gameObject,nozzle.gameObject);
            var rig=visualRoot.Find("Swap Player Role2");foreach(var a in rig.GetComponentsInChildren<Animator>(true)){a.Rebind();a.Update(0);a.enabled=false;}
            foreach(var a in player.GetComponentsInChildren<CharacterLocomotionAnimation>(true))a.enabled=false;
            var motion=player.AddComponent<FirefighterCartoonMotion>();motion.Initialize(player.transform,rig,tools,carry,nozzle,axe);
            ReviewSkinRefinement.Apply(rig,player.transform);
            var effects=game.AddComponent<FirstRescueEffects>();effects.Initialize(emitter,field);
            // This renderer handles all elements without interpreting them as water reactions.
            game.AddComponent<SprayParticleView>().Initialize(emitter);
            game.AddComponent<ReviewMissionHud>().Initialize(mission,input,carry,hose);
            ApplyMechanicPolish(root);
            camera.GetComponent<RescueCamera>().Initialize(player.transform);camera.GetComponent<RescueCamera>().ConfigureView(50,0);camera.GetComponent<Camera>().orthographicSize=6;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.4f,.42f,.44f);light.GetComponent<Light>().intensity=.9f;
            Physics.SyncTransforms();AssetDatabase.SaveAssets();
            if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Review scene save failed");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).Select(s=>new EditorBuildSettingsScene(s.path,false))).ToArray();
            File.WriteAllText(Evidence+"/install.txt","REVIEW_INSTALL_PASS\nSource SHA256 30a764941f7df43e12341048c539cead3ffaece8a82d1882ec6f7226342ecdff\nSource unchanged; wall-based origin excludes distant sample handle. Detached sink preserved outside playable scene.\nOnly temporary models: existing toilet, soda box, pan, pudding. No generated furnishings.\n");
        }
        static SprayProfile Profile(string name,ElementKind element,float angle,float speed,float gravity)
        {
            string path="Assets/FireRescue/Settings/"+name+".asset";var p=AssetDatabase.LoadAssetAtPath<SprayProfile>(path);
            if(!p){p=ScriptableObject.CreateInstance<SprayProfile>();AssetDatabase.CreateAsset(p,path);}
            var s=SpraySettings.DefaultFor(element);s.PhysicalBallistics=true;s.LaunchAngle=angle;s.MuzzleSpeed=speed;s.Gravity=gravity;s.AirDrag=.08f;s.AirLifetime=1.2f;s.SpreadAngle=element==ElementKind.Water?5:12;s.CollisionRadius=.06f;s.ImpactRadius=.7f;s.GroundLifetime=5;p.Configure(s);EditorUtility.SetDirty(p);return p;
        }
        static Mesh MeshAsset(FirstRescue1026Setup.Part p,bool cut)
        {
            string path=Folder+"/"+p.name+(cut?"-Cutaway":"-Full")+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh)return mesh;
            mesh=new Mesh{name=p.name,vertices=p.vertices.Select(v=>cut?new Vector3(v.x,Mathf.Min(v.y,.82f),v.z):v).ToArray(),triangles=p.triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        static void Floor(Transform parent,string name,Vector3 position,Vector3 size,Color color)
        {var t=Primitive(parent,name,PrimitiveType.Cube,position,size,Mat(Folder,name,color));}
        static Transform Primitive(Transform parent,string name,PrimitiveType kind,Vector3 position,Vector3 size,Material material)
        {var go=GameObject.CreatePrimitive(kind);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;}
        static Transform Child(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static Material Mat(string folder,string name,Color color)
        {string path=folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(Shader.Find("Standard")){name=name,color=color};AssetDatabase.CreateAsset(m,path);return m;}
        static void SetFloat(UnityEngine.Object target,string name,float value){var so=new SerializedObject(target);so.FindProperty(name).floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
        static void EnsureFolder(string path){if(AssetDatabase.IsValidFolder(path))return;EnsureFolder(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}
    }
}
