using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FireRescue.Editor
{
    [InitializeOnLoad]
    public static class FirstReview1027Validation
    {
        const string Key="FireRescue.Review1027.Test";static bool done;
        static FirstReview1027Validation(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){done=false;Application.logMessageReceived+=OnLog;new GameObject("Temporary review checks").AddComponent<FirstReview1027Driver>();}};}
        public static void BatchRun(){Directory.CreateDirectory(ReviewUserPropsImport.Evidence);SyncHand();SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();}
        public static void BatchPoses(){SessionState.SetBool("Review.PosesOnly",true);BatchRun();}
        public static void BatchSyncHand()
        { SyncHand();EditorApplication.Exit(0); }
        static void SyncHand()
        {
            var s=EditorSceneManager.OpenScene(FirstReview1027Setup.ScenePath);var r=UnityEngine.Object.FindAnyObjectByType<NpcItemRequest>();
            var hand=r.ReceivingHand;var bone=r.GetComponentsInChildren<Transform>(true).First(t=>t.name=="R_Hand");
            hand.SetParent(bone,true);hand.position=bone.position+r.transform.forward*.05f;hand.rotation=r.transform.rotation;
            var motion=UnityEngine.Object.FindAnyObjectByType<FirefighterCartoonMotion>();var data=new SerializedObject(motion);data.FindProperty("stride").floatValue=.92f;data.ApplyModifiedPropertiesWithoutUndo();
            ReviewSkinRefinement.Apply(motion.transform.Find("Visual/Swap Player Role2"),motion.transform);AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(s);
        }
        static void OnLog(string t,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,t+"\n"+stack);}
        public static void Finish(bool pass,string result)
        {
            if(done)return;done=true;Application.logMessageReceived-=OnLog;SessionState.SetBool(Key,false);
            File.WriteAllText(ReviewUserPropsImport.Evidence+"/playmode.txt",(pass?"REVIEW_PLAYMODE_PASS\n":"REVIEW_FAIL\n")+result);
            EditorApplication.delayCall+=()=>EditorApplication.Exit(pass?0:1);
        }
    }
    public sealed class FirstReview1027Driver:MonoBehaviour
    {
        const float Dt=1f/60;readonly List<string> checks=new List<string>();readonly List<string> trace=new List<string>();
        LocalPlayerInput input;FireField field;RescueNpc npc;SprayEmitter emitter;HoseConnection hose;RescueGameManager manager;FirstReviewMission mission;
        ObjectFire fire;BreakableDoor door;PlayerCarryInteraction carry;NpcItemRequest request;FirefighterTools tools;GameObject root;
        CookingOilArea area;PhysicalHoseTube tube;
        float started;int completions;
        void Start(){DontDestroyOnLoad(gameObject);started=Time.realtimeSinceStartup;Time.captureDeltaTime=Dt;StartCoroutine(Run());}
        void OnDestroy(){Time.captureDeltaTime=0;}
        void Update(){if(Time.realtimeSinceStartup-started>240)FirstReview1027Validation.Finish(false,"Timeout\n"+string.Join("\n",checks)+"\n"+string.Join("\n",trace));}
        void Check(bool condition,string message)
        {File.WriteAllLines(ReviewUserPropsImport.Evidence+"/trace.txt",trace);if(!condition)throw new InvalidOperationException(string.Join("\n",checks)+"\nFAIL "+message+"\n"+string.Join("\n",trace.TakeLast(16)));checks.Add("PASS "+message);}
        void Bind()
        {
            root=GameObject.Find("First Review 1027");input=root.GetComponentInChildren<LocalPlayerInput>();input.ReadDevices=false;input.SetInputFocus(true);
            field=root.GetComponentInChildren<FireField>();field.Simulate=false;npc=root.GetComponentInChildren<RescueNpc>();npc.Autonomous=false;
            emitter=input.GetComponent<SprayEmitter>();emitter.AutomaticSimulation=false;hose=input.GetComponent<HoseConnection>();manager=root.GetComponentInChildren<RescueGameManager>();
            mission=root.GetComponentInChildren<FirstReviewMission>();mission.AutomaticTick=false;fire=root.GetComponentInChildren<ObjectFire>();door=root.GetComponentInChildren<BreakableDoor>();
            carry=input.GetComponent<PlayerCarryInteraction>();request=npc.GetComponent<NpcItemRequest>();tools=input.GetComponent<FirefighterTools>();
            area=fire.GetComponent<CookingOilArea>();if(area)area.AutomaticTick=false;tube=input.GetComponent<PhysicalHoseTube>();
            manager.RescueCompleted+=()=>completions++;
        }
        IEnumerator Run()
        {
            Bind();yield return null;
            if(SessionState.GetBool("Review.PosesOnly",false))
            {SessionState.SetBool("Review.PosesOnly",false);yield return PoseChecks();FirstReview1027Validation.Finish(true,string.Join("\n",checks)+"\n"+string.Join("\n",trace));yield break;}
            Check(hose.MaximumLength==15&&hose.AutomaticWallWrapping,"Approved 15m routed hose enabled only in new scene");
            Check(root.GetComponentsInChildren<FluidSource>().Length==2&&field.BurningCount==0&&fire.Kind==FireKind.CookingOil,"One toilet + one soda source; only oil pan is burning");
            foreach(var key in new[]{"Toilet Water Source","Soda Powder Source","Oil Pan Fire","Pudding Item"})
            {
                var model=root.transform.Find(key+"/Visual").GetComponentsInChildren<MeshFilter>();
                Check(model.Length>0&&model.All(m=>AssetDatabase.GetAssetPath(m.sharedMesh).StartsWith(ReviewUserPropsImport.Folder+"/")),key+" uses new user meshes, not generated primitives");
            }
            Check(!root.GetComponentsInChildren<TextMesh>().Any(),"No permanent world debug labels");
            Check(npc.GetComponent<NpcRequestNameTag>(),"Request is a screen-projected name tag attached above resident");
            var plaster=AssetDatabase.LoadAssetAtPath<Material>(FirstReview1027Setup.Folder+"/Warm plaster.mat");
            Check(plaster.shader.name=="FireRescue/MattePlaster"&&!plaster.HasProperty("_Glossiness"),"Wall uses diffuse-only plaster shader, with no metal/specular lobe");
            Check(root.GetComponentsInChildren<SkinnedMeshRenderer>().All(s=>s.sharedMesh&&s.bones.All(b=>b)),"User skins and bones intact");
            npc.Tick(.3f);Check(!npc.HasSafePath&&!request.CanLeave,"Resident waits for actual item and door/fire conditions");
            Capture("01-layout.png",new Vector3(0,.4f,0),9,true);
            var food=root.GetComponentInChildren<CarryItem>();Check(area&&food.IsSafeToTake,"Pudding starts outside initial small fire footprint");
            for(int i=0;i<120;i++){Command(Vector2.zero,input.transform.position+Vector3.forward);yield return null;}
            Check(area.IsHot(food.transform.position)&&!food.IsSafeToTake&&!food.Take(input.transform.Find("Carry Anchor")),"At 2s fire reaches pudding and authoritative pickup refuses hot item");
            Capture("oil-spread-2s.png",new Vector3(-4.5f,.3f,-3.5f),3.4f,false);
            yield return Walk(new Vector3(5.5f,.1f,-4.9f));yield return Walk(new Vector3(3,.1f,-2));yield return Walk(new Vector3(.45f,.1f,.2f));yield return Walk(new Vector3(.45f,.1f,2.65f));
            yield return RightPress();Check(tools.SelectedTool==FirefighterTools.Tool.Hose&&!hose.IsConnected,"Right press 1 only equips nozzle");
            yield return RightPress();Check(hose.IsConnected&&hose.Source.Element==ElementKind.Water&&!emitter.IsSpraying,"Right press 2 connects toilet without firing");
            Check(tube&&tube.IsVisible&&tube.NodeCount>=128&&tube.DisplayMesh.vertexCount==tube.NodeCount*8,"Connected hose is one continuous 8-sided 3D mesh, not billboard line");
            yield return Walk(new Vector3(.45f,.1f,.2f));yield return Walk(new Vector3(-1.4f,.1f,-3.4f));yield return Walk(new Vector3(-3.9f,.1f,-3.4f));yield return Walk(new Vector3(-3.9f,.1f,-3.1f));
            Check(hose.IsConnected&&hose.UsedLength<=15.01f&&hose.AutomaticBendCount>0,"Real player pulls water hose around bathroom/kitchen walls within budget");
            float initial=fire.Intensity;yield return Spray(1.4f);
            trace.Add("WATER intensity="+fire.Intensity+" amount="+fire.WaterApplied+" nozzle="+emitter.NozzlePosition);
            Check(fire.WaterApplied>.1f&&fire.Intensity>initial&&fire.IsBurning,"Actual swept water packets strike pan and increase oil fire");
            Capture("02-water-mistake.png",input.transform.position+Vector3.left*.8f+Vector3.up*.5f,3.3f,false);
            yield return PressF();Check(!hose.IsConnected,"F disconnects old source and clears route");
            yield return Walk(new Vector3(-2.84f,.1f,-1.55f));yield return Walk(new Vector3(-2.84f,.1f,-.25f));yield return Walk(new Vector3(-4f,.1f,1.1f));yield return Walk(new Vector3(-5.4f,.1f,2.8f));yield return Walk(new Vector3(-5.4f,.1f,3.95f));
            yield return RightPress();Check(hose.IsConnected&&hose.Source.Element==ElementKind.DryPowder&&!emitter.IsSpraying,"Existing nozzle connects storage soda on a separate press");
            yield return Walk(new Vector3(-5.4f,.1f,2.8f));yield return Walk(new Vector3(-4f,.1f,1.1f));yield return Walk(new Vector3(-2.84f,.1f,-.25f));yield return Walk(new Vector3(-2.84f,.1f,-1.55f));yield return Walk(new Vector3(-3.9f,.1f,-3.1f));
            Check(hose.IsConnected&&hose.UsedLength<=15.01f,"Storage-to-kitchen physical route fits approved length");
            yield return Spray(4.0f);
            Check(!fire.IsBurning&&manager.AllFiresOut,"Real powder packets extinguish the water-enlarged oil fire");
            Check(!area.IsHot(food.transform.position)&&food.IsSafeToTake,"Extinguishing connected oil spill clears heat gate for pudding");
            Check(tube.ConnectedVisibleFrames>200&&tube.UnexpectedHiddenFrames==0&&tube.IsVisible,"Wall bends and changing route topology never blank the complete 3D hose");
            Check(tube.LongestSegment<.5f&&tube.DisplayMesh.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsInfinity(v.x)),"Physical tube node spacing stays bounded and mesh vertices finite");
            Capture("continuous-3d-hose.png",new Vector3(-3.5f,.5f,-1.3f),5,false);
            Check(!npc.HasSafePath&&!request.IsFulfilled&&!manager.LevelComplete,"Fire out alone never bypasses pudding and closed door");
            Capture("03-soda-out.png",input.transform.position+Vector3.left,3.3f,false);
            yield return PressF();
            yield return Walk(new Vector3(-3.3f,.1f,-3.4f));yield return Walk(new Vector3(-1.5f,.1f,-3.4f));yield return Walk(new Vector3(5.28f,.1f,-.05f));
            Vector3 doorAim=new Vector3(5.28f,.8f,1.5f);
            Command(Vector2.zero,doorAim,axePressed:true,axeHeld:true);yield return null;Command(Vector2.zero,doorAim);yield return null;
            Check(tools.SelectedTool==FirefighterTools.Tool.Axe&&!door.IsBroken,"First left click equips without damaging door");
            Command(Vector2.zero,doorAim,axePressed:true,axeHeld:true);yield return null;
            for(int i=0;i<120&&!door.IsBroken;i++){Command(Vector2.zero,doorAim,axeHeld:true);if(i==18)Capture("04-axe-pose.png",input.transform.position+Vector3.up*.7f,1.7f,false);yield return null;}
            Command(Vector2.zero,doorAim);yield return null;
            Check(door.IsBroken&&!door.BlockingCollider.enabled&&tools.SwingCount==3,"Three strokes break user door once per swing; route collider removed");
            npc.Tick(.3f);Check(!npc.HasSafePath,"Door/fire clear but missing pudding still blocks escape");
            yield return Walk(new Vector3(2.5f,.1f,-2.5f));yield return Walk(new Vector3(-1.4f,.1f,-3));yield return Walk(new Vector3(-4.85f,.1f,-4.09f));
            carry.RefreshNearby();Check(carry.NearbyItem,"Pudding on existing user counter is reachable with E");
            Check(carry.Interact()&&carry.IsCarrying&&tools.SelectedTool==FirefighterTools.Tool.None,"E picks one item, disables its collision and stows tools");
            Command(Vector2.zero,doorAim,axePressed:true,axeHeld:true);yield return null;
            Check(!tools.IsSwinging&&tools.SelectedTool==FirefighterTools.Tool.None,"Cannot swing a tool through carried pudding");
            yield return Walk(new Vector3(-1.4f,.1f,-3));yield return Walk(new Vector3(3.5f,.1f,-1.4f));yield return Walk(new Vector3(5.28f,.1f,.05f));
            Check(!carry.CanDeliver&&!request.IsFulfilled,"Outside bedroom cannot deliver through distant wall");
            yield return Walk(new Vector3(5.28f,.1f,1.65f));yield return Walk(new Vector3(4.15f,.1f,2.3f));
            Check(mission.Remaining>0&&carry.CanDeliver,"Player reaches resident before 60s deadline");
            Check(carry.Interact()&&request.IsFulfilled&&!carry.IsCarrying&&mission.CurrentStatus==FirstReviewMission.Status.Evacuating,"E transfers pudding to resident and stops request deadline");
            Check(request.ReceivingHand.GetComponentInParent<RescueNpc>()==npc&&request.ReceivingHand.parent.name=="R_Hand","Delivered pudding is attached to actual hand bone");
            var delivered=request.ReceivingHand.GetComponentInChildren<CarryItem>();
            Check(delivered&&Mathf.Abs(delivered.transform.lossyScale.x-1)<.01f,"FBX 100x hand-bone units never resize the delivered pudding");
            Capture("pudding-delivered.png",npc.transform.position+Vector3.up*.5f,1.8f,false);
            float remaining=mission.Remaining;yield return Walk(new Vector3(2.7f,.1f,4.4f));
            for(int i=0;i<1500&&!npc.IsRescued;i++){Command(Vector2.zero,doorAim);yield return null;}
            Check(npc.IsRescued&&manager.LevelComplete&&completions==1&&mission.CurrentStatus==FirstReviewMission.Status.Won&&mission.CompletionCount==1,"NPC walks through broken door and outside exit; first level completes exactly once");
            Check(Mathf.Abs(remaining-mission.Remaining)<.001f,"One-minute requirement timer remains frozen after valid delivery");
            var motion=input.GetComponent<FirefighterCartoonMotion>();Check(motion.TravelledDistance>30&&motion.GaitPhase>=0&&motion.GaitPhase<1,"Distance-driven gait follows actual resolved movement");
            float phase=motion.GaitPhase;for(int i=0;i<30;i++)yield return null;
            Check(Mathf.Abs(phase-motion.GaitPhase)<.001f&&motion.MovementBlend<.05f,"Idle stops gait phase and smoothly settles body");
            Capture("05-complete.png",new Vector3(4,.4f,-3.2f),5,false);
            // Isolated boundary fixture does not change production scene or skip any mission step.
            var fixture=new GameObject("Temporary deadline fixture").AddComponent<FirstReviewMission>();fixture.AutomaticTick=false;fixture.Tick(float.NaN);fixture.Tick(-1);
            Check(fixture.Remaining==60,"Invalid/nonpositive timer deltas are ignored");fixture.Tick(60);
            Check(fixture.CurrentStatus==FirstReviewMission.Status.Failed&&!fixture.CanDeliver&&!fixture.AllowsInput,"Exact 60s timeout rejects late delivery and tools");fixture.NotifyDelivered();fixture.Tick(1);
            Check(fixture.CurrentStatus==FirstReviewMission.Status.Failed&&fixture.CompletionCount==0,"Late notification cannot turn a failure into success");Destroy(fixture.gameObject);
            SceneManager.LoadScene(FirstReview1027Setup.ScenePath);yield return null;yield return null;Bind();yield return null;
            Check(mission.Remaining>59.9f&&mission.CurrentStatus==FirstReviewMission.Status.Active&&!request.IsFulfilled,"Fresh scene has clean request/deadline state");
            Check(!door.IsBroken&&fire.Intensity==1&&fire.WaterApplied==0&&!hose.IsConnected&&!carry.IsCarrying,"Fresh scene resets door, fire, hose and pudding ownership");
            yield return PoseChecks();
            var carryFixture=new GameObject("Temporary carried-item disconnect fixture");carryFixture.transform.position=new Vector3(20,.1f,0);
            var fixtureHose=carryFixture.AddComponent<HoseConnection>();var fixtureInput=carryFixture.AddComponent<LocalPlayerInput>();fixtureInput.ReadDevices=false;
            var fixtureCarry=carryFixture.AddComponent<PlayerCarryInteraction>();var fixtureItem=new GameObject("Temporary carry item").AddComponent<CarryItem>();fixtureItem.transform.position=carryFixture.transform.position;
            fixtureItem.Initialize(Array.Empty<Collider>());fixtureCarry.Initialize(carryFixture.transform,new[]{fixtureItem},null,null,null);
            var fixtureSource=new GameObject("Temporary source").AddComponent<FluidSource>();fixtureSource.transform.position=carryFixture.transform.position;
            fixtureInput.Initialize(null,fixtureHose,null,null,new[]{fixtureSource},Array.Empty<RescueNpc>());fixtureInput.SetMissionInteraction(fixtureCarry,null);
            Check(fixtureCarry.Interact()&&fixtureHose.TryConnect(fixtureSource),"Disposable carry fixture can hold item while still connected");
            fixtureInput.TickPointerCommand(Vector2.zero,carryFixture.transform.position+Vector3.forward,true,false,false,false,false,false,Dt);
            Check(fixtureCarry.IsCarrying&&!fixtureHose.IsConnected,"F can disconnect while carrying, without dropping pudding or enabling tools");Destroy(carryFixture);Destroy(fixtureSource.gameObject);
            var fireFixture=new GameObject("Temporary oil reaction fixture").AddComponent<ObjectFire>();fireFixture.Configure(FireKind.CookingOil,4);
            fireFixture.Receive(ElementKind.Water,2);Check(fireFixture.Intensity==4,"Historical oil fire with growth disabled stays unchanged under water");
            fireFixture.Configure(FireKind.CookingOil,1);fireFixture.ConfigureWaterEscalation(.65f,3);fireFixture.Receive(ElementKind.Water,float.PositiveInfinity);
            Check(fireFixture.Intensity==1&&fireFixture.WaterApplied==0,"Nonfinite spray amount never poisons oil fire state");
            fireFixture.Receive(ElementKind.Water,100);Check(fireFixture.Intensity==3,"Water escalation saturates at authored cap");
            fireFixture.Receive(ElementKind.DryPowder,6);fireFixture.Receive(ElementKind.Water,1);Check(!fireFixture.IsBurning,"Powder extinguishes cap fire; later water never resurrects it");Destroy(fireFixture.gameObject);
            FirstReview1027Validation.Finish(true,"Checks="+checks.Count+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",trace)+"\nScripted commands + real CharacterController/swept spray; not manual keyboard acceptance.");
        }
        IEnumerator PoseChecks()
        {
            var motion=input.GetComponent<FirefighterCartoonMotion>();
            foreach(string name in new[]{"Hips","Chest","L_UpperArm","L_LowerArm","L_Hand","R_UpperArm","R_LowerArm","R_Hand","L_Thigh","L_Shin","L_Foot"})
            {var t=input.GetComponentsInChildren<Transform>(true).First(x=>x.name==name);trace.Add(name+" rootLocal="+input.transform.InverseTransformPoint(t.position).ToString("F4")+" scale="+t.lossyScale.ToString("F4"));}
            tools.Stow();for(int i=0;i<10;i++)yield return null;CapturePose("pose-idle.png");
            tools.TickPointer(input.transform.position+input.transform.forward,false,false,true,true,null,Dt);
            for(int i=0;i<10;i++)yield return null;
            trace.Add("HOSE grips left="+motion.LeftGripError+" right="+motion.RightGripError);
            CapturePose("pose-hose.png");
            Check(motion.LeftGripError<.03f&&motion.RightGripError<.03f,"Idle hose wrists reach authored grip targets within 3cm");
            tools.TickPointer(input.transform.position+input.transform.forward,true,true,false,false,null,Dt);for(int i=0;i<10;i++)yield return null;CapturePose("pose-axe.png");
            tools.Stow();
            var left=input.GetComponentsInChildren<Transform>(true).First(t=>t.name=="L_Foot");
            float footSpeed=0;int samples=0;Vector3 before=left.position;float beforePhase=motion.GaitPhase;
            for(int i=0;i<70;i++)
            {
                Command(Vector2.up,input.transform.position+Vector3.forward);
                yield return null;
                if(i>20&&beforePhase>.08f&&beforePhase<.42f){footSpeed+=Vector3.ProjectOnPlane(left.position-before,Vector3.up).magnitude/Dt;samples++;}
                before=left.position;beforePhase=motion.GaitPhase;
                if(i==42)CapturePose("pose-walk.png");
            }
            float stanceSpeed=samples>0?footSpeed/samples:float.PositiveInfinity;
            trace.Add("GAIT mean stance planar speed="+stanceSpeed+" samples="+samples);
            Check(samples>5&&stanceSpeed<.15f,"Straight-walk stance foot remains planted (mean slide below 0.15m/s)");
            Command(Vector2.zero,input.transform.position+Vector3.forward);yield return null;
            // Holding the nozzle while walking and aiming uses the same gait, not the Idle clip.
            tools.TickPointer(input.transform.position+input.transform.forward,false,false,true,true,null,Dt);float gripError=0;
            for(int i=0;i<30;i++){Command(Vector2.up,input.transform.position+Vector3.forward);yield return null;gripError=Mathf.Max(gripError,motion.LeftGripError,motion.RightGripError);}
            Check(gripError<.06f,"Moving hose grip stays within 6cm without changing gameplay anchors");
            CapturePose("pose-hose-walk.png");Command(Vector2.zero,input.transform.position+Vector3.forward);yield return null;
        }
        void CapturePose(string name)
        {
            var cam=new GameObject("Temporary front pose review").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=1.1f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.27f,.30f,.31f);
            var focus=input.transform.position+Vector3.up*.8f;cam.transform.position=focus+input.transform.forward*4+input.transform.right*1.2f+Vector3.up*.65f;cam.transform.LookAt(focus);
            SaveCamera(cam,name);Destroy(cam.gameObject);
        }
        static void SaveCamera(Camera cam,string name)
        {
            var rt=new RenderTexture(900,900,24);cam.targetTexture=rt;var old=RenderTexture.active;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(900,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,900,900),0,0);tex.Apply();File.WriteAllBytes(ReviewUserPropsImport.Evidence+"/"+name,tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;Destroy(tex);rt.Release();Destroy(rt);
        }
        void Command(Vector2 movement,Vector3 aim,bool interact=false,bool axePressed=false,bool axeHeld=false,bool hosePressed=false,bool hoseHeld=false)
        {input.TickPointerCommand(movement,aim,interact,false,axePressed,axeHeld,hosePressed,hoseHeld,Dt);emitter.Advance(Dt);field.Step(Dt);if(area)area.Tick(Dt);npc.Tick(Dt);mission.Tick(Dt);}
        IEnumerator RightPress(){Command(Vector2.zero,input.transform.position+input.transform.forward,hosePressed:true,hoseHeld:true);yield return null;Command(Vector2.zero,input.transform.position+input.transform.forward);yield return null;}
        IEnumerator PressF(){Command(Vector2.zero,input.transform.position+input.transform.forward,interact:true);yield return null;Command(Vector2.zero,input.transform.position+input.transform.forward);yield return null;}
        IEnumerator Spray(float seconds)
        {Command(Vector2.zero,fire.transform.position,hosePressed:true,hoseHeld:true);yield return null;for(int i=0;i<seconds/Dt;i++){Command(Vector2.zero,fire.transform.position,hoseHeld:true);yield return null;}Command(Vector2.zero,fire.transform.position);yield return null;for(int i=0;i<80;i++){Command(Vector2.zero,fire.transform.position);yield return null;}}
        IEnumerator Walk(Vector3 target)
        {
            int i;for(i=0;i<450;i++){Vector3 d=target-input.transform.position;d.y=0;if(d.magnitude<.085f)break;Command(Vector2.ClampMagnitude(new Vector2(d.x,d.z)*4,1),target);yield return null;}
            trace.Add("WALK "+target.ToString("F2")+" actual="+input.transform.position.ToString("F3")+" used="+hose.UsedLength+" bends="+hose.AutomaticBendCount+" frames="+i+" time="+mission.Remaining);
            Check(i<450,"Walk reached "+target.ToString("F2"));Command(Vector2.zero,target);yield return null;
        }
        public static void Capture(string name,Vector3 focus,float size,bool top)
        {
            var cam=new GameObject("Temporary review render").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=size;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.24f,.28f,.29f);cam.transform.rotation=Quaternion.Euler(top?90:50,0,0);cam.transform.position=focus-cam.transform.forward*24;
            var rt=new RenderTexture(1280,900,24);cam.targetTexture=rt;var old=RenderTexture.active;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,900),0,0);tex.Apply();File.WriteAllBytes(ReviewUserPropsImport.Evidence+"/"+name,tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;Destroy(tex);rt.Release();Destroy(rt);Destroy(cam.gameObject);
        }
    }
}
