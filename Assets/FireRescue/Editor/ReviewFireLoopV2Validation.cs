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
    public static class ReviewFireLoopV2Validation
    {
        const string Key="Review.FireV2.Validation";static bool done;
        static ReviewFireLoopV2Validation(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){done=false;Application.logMessageReceived+=OnError;new GameObject("Temporary fire v2 validation").AddComponent<ReviewFireLoopV2Driver>();}};}
        public static void BatchRun(){Directory.CreateDirectory(ReleasePolishSetup.Evidence);EditorSceneManager.OpenScene(FirstReview1027Setup.ScenePath);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();}
        static void OnError(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,text+"\n"+stack);}
        public static void Finish(bool success,string text)
        {if(done)return;done=true;SessionState.SetBool(Key,false);Application.logMessageReceived-=OnError;File.WriteAllText(ReleasePolishSetup.Evidence+"/playmode.txt",(success?"RELEASE_POLISH_PASS\n":"RELEASE_POLISH_FAIL\n")+text);EditorApplication.delayCall+=()=>EditorApplication.Exit(success?0:1);}
    }
    public sealed class ReviewFireLoopV2Driver:MonoBehaviour
    {
        const float Dt=1f/60;readonly List<string> checks=new List<string>(),trace=new List<string>();
        LocalPlayerInput input;FireField field;CookingOilIncident incident;ObjectFire pot;CarryItem food;FirstReviewMission mission;
        NpcItemRequest request;RescueNpc npc;FirefighterTools tools;HoseConnection hose;SprayEmitter emitter;PlayerCarryInteraction carry;
        PlayerFireContact contact;BreakableDoor door;PhysicalHoseTube tube;float started;
        void Start(){DontDestroyOnLoad(gameObject);started=Time.realtimeSinceStartup;Time.captureDeltaTime=Dt;StartCoroutine(Run());}
        void OnDestroy(){Time.captureDeltaTime=0;}
        void Update(){if(Time.realtimeSinceStartup-started>220)ReviewFireLoopV2Validation.Finish(false,"Timeout\n"+string.Join("\n",checks)+"\n"+string.Join("\n",trace));}
        void Check(bool value,string label){if(!value)throw new InvalidOperationException(string.Join("\n",checks)+"\nFAIL "+label+"\n"+string.Join("\n",trace));checks.Add("PASS "+label);}
        void Bind()
        {
            input=FindAnyObjectByType<LocalPlayerInput>();input.ReadDevices=false;input.SetInputFocus(true);
            field=FindAnyObjectByType<FireField>();field.Simulate=false;incident=FindAnyObjectByType<CookingOilIncident>();incident.AutomaticTick=false;
            pot=FindAnyObjectByType<ObjectFire>();food=FindAnyObjectByType<CarryItem>();mission=FindAnyObjectByType<FirstReviewMission>();mission.AutomaticTick=false;
            npc=FindAnyObjectByType<RescueNpc>();npc.Autonomous=false;request=npc.GetComponent<NpcItemRequest>();request.AutomaticTick=false;
            tools=input.GetComponent<FirefighterTools>();hose=input.GetComponent<HoseConnection>();emitter=input.GetComponent<SprayEmitter>();emitter.AutomaticSimulation=false;
            carry=input.GetComponent<PlayerCarryInteraction>();contact=input.GetComponent<PlayerFireContact>();contact.AutomaticTick=false;door=FindAnyObjectByType<BreakableDoor>();tube=input.GetComponent<PhysicalHoseTube>();
        }
        IEnumerator Reload(){SceneManager.LoadScene(FirstReview1027Setup.ScenePath);yield return null;Bind();yield return null;}
        void Command(Vector2 movement,Vector3 aim,bool right=false,bool hold=false,bool left=false,bool axe=false,bool disconnect=false)
        {
            input.TickPointerCommand(movement,aim,disconnect,false,left,axe,right,hold,Dt);emitter.Advance(Dt);
            incident.Tick(Dt);field.Step(Dt);contact.Evaluate();request.Tick(Dt);npc.Tick(Dt);mission.Tick(Dt);
        }
        IEnumerator Wait(float seconds){for(int i=0;i<seconds/Dt;i++){Command(Vector2.zero,input.transform.position+Vector3.forward);yield return null;}}
        IEnumerator Right(){Command(Vector2.zero,input.transform.position+input.transform.forward,right:true,hold:true);yield return null;Command(Vector2.zero,input.transform.position+input.transform.forward);yield return null;}
        IEnumerator Disconnect(){Command(Vector2.zero,input.transform.position+input.transform.forward,disconnect:true);yield return null;}
        IEnumerator Walk(Vector3 p)
        {
            int i;for(i=0;i<450;i++){var d=p-input.transform.position;d.y=0;if(d.magnitude<.08f)break;Command(Vector2.ClampMagnitude(new Vector2(d.x,d.z)*4,1),p);yield return null;}
            trace.Add("WALK "+p.ToString("F2")+" actual="+input.transform.position.ToString("F3")+" remaining="+mission.Remaining+" cells="+field.BurningCount+" dead="+contact.IsDead+" hose="+hose.UsedLength);
            Check(i<450,"Real player reached "+p.ToString("F2"));Command(Vector2.zero,p);yield return null;
        }
        IEnumerator Spray(Vector3 target,float seconds)
        {
            Command(Vector2.zero,target,right:true,hold:true);yield return null;
            for(int i=0;i<seconds/Dt;i++){Command(Vector2.zero,target,hold:true);yield return null;}
            Command(Vector2.zero,target);yield return null;
            yield return Wait(1.1f);
        }
        IEnumerator Run()
        {
            Bind();yield return null;
            var hud=FindAnyObjectByType<ReviewMissionHud>();Check(!hud.CanRestart&&!hud.RestartAfterFailure(),"Failure button is hidden and guarded during active play");
            Check(input.transform.position.x<5&&input.transform.position.z>-5.5f,"Spawn is inside living room, not ambiguous exterior entrance");
            Check(field.BurningCount==0&&pot.IsBurning,"Only pot is initially on fire; no full-kitchen flame carpet");
            yield return Wait(.35f);int start=field.BurningCount;float fastRate=field.SpreadRate;Capture("spread-start.png");
            yield return Wait(1.65f);int atTwo=field.BurningCount;
            Check(atTwo>start&&atTwo<incident.FirstWaveTargetCount&&incident.FirstWaveProcessedCount<incident.FirstWaveTargetCount,"At two seconds fire is spreading, not already filling the entire kitchen");
            yield return Wait(3f);int fast=field.BurningCount;Capture("spread-fast.png");
            Check(incident.FirstWaveTargetCount>100&&incident.FirstWaveProcessedCount==incident.FirstWaveTargetCount&&fast==incident.FirstWaveTargetCount,"By five seconds the connected kitchen floor is fully ignited");
            var effects=FindAnyObjectByType<FirstRescueEffects>();yield return null;
            Check(effects.ActiveFlameCount>0&&effects.ActiveFlameCount<fast*.75f&&effects.ActiveHeatMarkCount==fast,"Spacious flame presentation reduces clusters while every hazardous cell keeps a floor footprint");
            var density=new SerializedObject(effects);density.FindProperty("spaciousFire").boolValue=false;density.ApplyModifiedPropertiesWithoutUndo();yield return null;Capture("fire-density-before.png",new Vector3(-3.9f,.4f,-3.0f),3.3f);
            density.FindProperty("spaciousFire").boolValue=true;density.ApplyModifiedPropertiesWithoutUndo();yield return null;Capture("fire-density-after.png",new Vector3(-3.9f,.4f,-3.0f),3.3f);
            Check(field.BurningCount==fast,"Changing flame density does not change fire or contact gameplay");
            Check(!CanReachStorage(true)&&CanReachStorage(false),"Burning kitchen plus courtyard fences block the outdoor shortcut, not an invisible objective gate");
            yield return Wait(4f);int slow=field.BurningCount;Capture("spread-slow.png");
            trace.Add("SPREAD counts start="+start+" fast="+fast+" later="+slow+" rates="+fastRate+" -> "+field.SpreadRate);
            Check(start>0&&fast>start&&slow>=fast,"Actual ignition front begins at pot and fills the kitchen, not a frame-zero flame carpet");
            Check(fastRate>field.SpreadRate*5,"Early rapid propagation transitions to slower spread");
            // Reaction branch on actual scene objects, deliberately separate from the command-driven success run.
            float intensity=pot.Intensity;pot.Receive(ElementKind.Water,.1f);yield return null;
            Check(pot.Intensity>=intensity&&pot.IsBurning&&incident.ExplosionCount==1,"Water on burning oil never extinguishes it and triggers a burst");
            Check(food.IsBurnt,"Nearby original pudding becomes scorched");Capture("oil-burst-burnt-food.png",new Vector3(-4.9f,.6f,-3.7f),2.1f);
            pot.Receive(ElementKind.Water,1);Check(incident.ExplosionCount==1,"Continuous spray cannot emit an explosion every particle/frame");
            var fpos=new Vector3(-5.0f,.1f,-3.5f);field.Ignite(fpos,1);field.ApplyTimedWater(fpos,10,.9f,5);
            Check(!field.IsBurningAt(fpos)&&pot.IsBurning,"Water extinguishes ordinary neighboring floor cells without deleting oil fire");
            for(int i=0;i<30;i++){incident.Tick(Dt);field.Step(Dt);}
            Check(field.IsBurningAt(fpos),"Water can clear stove-side fire, but ongoing pot heat quickly relights it");
            var far=new Vector3(-2.75f,.1f,-1.5f);field.ApplyTimedWater(far,10,.7f,5);
            for(int i=0;i<30;i++){incident.Tick(Dt);field.Step(Dt);}
            Check(!field.IsBurningAt(far),"Water leaves a traversable wet route outside the pot heat radius");
            pot.Receive(ElementKind.DryPowder,20);field.ApplyTimedDryPowder(fpos,20,1.1f,8);
            for(int i=0;i<60;i++){incident.Tick(Dt);field.Step(Dt);}
            Check(!field.IsBurningAt(fpos),"After powder puts out the pot, neighboring fire stops being relit");
            // Poison branch exercises real ownership transfer, door requirement and delayed failure.
            // Inject door/heat preconditions only in this reaction branch, not in the success walkthrough.
            door.ReceiveAxeHit(3);field.ApplyTimedWater(food.transform.position,10,12,5);
            Check(door.IsBroken&&food.IsSafeToTake,"Poison branch prerequisites are isolated from success walkthrough");
            Check(food.Take(input.transform.Find("Carry Anchor"))&&request.TryAccept(food),"Real burnt pudding transfers to original resident's hand");
            Check(request.IsPoisoned&&!request.CanLeave&&mission.CurrentStatus==FirstReviewMission.Status.Poisoning,"Bad meal blocks rescue and enters poisoning state");
            Check(npc.GetComponent<NpcRequestNameTag>().DisplayText=="好難吃...","Resident's actual overhead reaction says 好難吃...");
            request.Tick(1);Check(!request.IsDead,"Complaint has a visible delay before death");request.Tick(.5f);
            Check(request.IsDead&&mission.FailureCause==FirstReviewMission.Failure.ResidentPoisoned&&mission.CompletionCount==0,"Poisoned resident dies; failure never counts as a rescue");
            for(int i=0;i<30;i++)yield return null;
            Check(Quaternion.Angle(npc.transform.Find("Visual").localRotation,Quaternion.identity)>70,"Original resident visual falls over without moving collider root");
            Capture("npc-poison-death.png",npc.transform.position+Vector3.up*.4f,1.8f);
            Check(hud.CanRestart&&hud.RestartAfterFailure(),"Poison failure exposes restart button and its handler reloads the real scene");yield return null;Bind();yield return null;
            Check(mission.CurrentStatus==FirstReviewMission.Status.Active&&!request.IsPoisoned&&!food.IsBurnt&&mission.Remaining>59.9f,"Button restart resets poison, food, mission and timer");
            // Contact branch: no direct damage call, walk into an actual live floor cell.
            yield return Wait(5);
            yield return Walk(new Vector3(-1.45f,.1f,-3.45f));
            for(int i=0;i<100&&!contact.IsDead;i++){Command(Vector2.left,new Vector3(-6,.1f,-3.45f));yield return null;}
            Check(contact.IsDead&&mission.FailureCause==FirstReviewMission.Failure.PlayerBurned&&!mission.AllowsInput,"Walking onto live floor fire kills player and closes tool/input gates");Capture("player-fire-death.png");
            hud=FindAnyObjectByType<ReviewMissionHud>();Check(hud.CanRestart&&hud.RestartAfterFailure(),"Player fire death can also restart through the failure button handler");yield return null;Bind();yield return null;
            Check(!contact.IsDead&&!request.IsPoisoned&&!food.IsBurnt&&incident.ExplosionCount==0&&mission.Remaining>59.9f&&!hose.IsConnected,"Button restart resets death, food, burst, hose and timer");
            yield return Walk(new Vector3(.45f,.1f,.2f));yield return Walk(new Vector3(.45f,.1f,2.65f));
            yield return Right();yield return Right();Check(hose.IsConnected&&hose.Source.Element==ElementKind.Water,"Separate right presses equip and connect authored toilet");
            yield return Walk(new Vector3(.45f,.1f,.2f));yield return Walk(new Vector3(.6f,.1f,-3.45f));
            int before=field.BurningCount;float waterFloor=0;
            emitter.ParticleImpacted+=p=>{if(p.Element==ElementKind.Water&&p.Position.y<.3f)waterFloor+=p.Amount;};
            yield return Spray(new Vector3(-2.8f,.1f,-3.45f),2.4f);
            yield return Walk(new Vector3(-1.45f,.1f,-3.45f));yield return Spray(new Vector3(-5.0f,.1f,-3.45f),2.4f);
            yield return Walk(new Vector3(-2.8f,.1f,-3.45f));
            yield return Spray(new Vector3(-2.84f,.1f,-.8f),2.4f);
            trace.Add("WATER floorAmount="+waterFloor+" cells="+before+" -> "+field.BurningCount+" pot="+pot.Intensity);
            Check(waterFloor>.2f&&pot.Intensity==1&&incident.ExplosionCount==0,"Real ballistic water hits floor while deliberate aiming avoids pot explosion");Capture("water-cleared-route.png");
            Check(hose.MaximumLength==10,"Hose is shortened to 10m, not silently expanded to make the test pass");
            Check(!food.IsSafeToTake&&!food.Take(input.transform.Find("Carry Anchor")),"Water route alone cannot safely collect pudding while the pot keeps relighting nearby fire");
            Check(tube.NodeCount==128&&tube.UnexpectedHiddenFrames==0,"Hose wall contacts retain fixed particle count and continuous visibility");
            trace.Add("HOSE max material node displacement="+tube.MaximumNodeStep+" longest="+tube.LongestSegment);
            Check(tube.MaximumNodeStep<.25f&&tube.LongestSegment<.3f,"Wall bends do not pop a new hard node or stretch the tube into long spans");
            yield return Disconnect();
            yield return Walk(new Vector3(-2.84f,.1f,-1.55f));yield return Walk(new Vector3(-2.84f,.1f,-.25f));yield return Walk(new Vector3(-5.4f,.1f,2.8f));yield return Walk(new Vector3(-5.4f,.1f,3.95f));
            yield return Right();Check(hose.IsConnected&&hose.Source.Element==ElementKind.DryPowder,"Cleared route leads to storage powder connection");
            yield return Walk(new Vector3(-5.4f,.1f,2.8f));yield return Walk(new Vector3(-2.84f,.1f,-.25f));
            yield return Walk(new Vector3(-2.84f,.1f,-1.55f));
            yield return Spray(new Vector3(-4.1f,.1f,-2.2f),3.3f);
            yield return Walk(new Vector3(-3.65f,.1f,-2.0f));
            yield return Spray(pot.transform.position,5.0f);
            yield return Spray(new Vector3(-4.65f,.1f,-4.1f),3f);
            trace.Add("POWDER pot="+pot.Intensity+" cells="+field.BurningCount+" player="+input.transform.position);
            Check(!pot.IsBurning&&!food.IsBurnt&&!contact.IsDead,"Actual powder extinguishes oil while pudding remains edible");
            yield return Disconnect();yield return Walk(new Vector3(-4.78f,.1f,-4.09f));
            carry.RefreshNearby();var context=input.GetComponent<PickupContextView>();context.Refresh();
            Check(context.CurrentItem==food&&context.IsPickupPromptVisible,"Reachable pudding has the same gold outline and contextual E badge");Capture("pudding-outline.png",food.transform.position,1.6f);
            Check(carry.NearbyItem&&carry.Interact(),"E picks safe original pudding after clearing fire");context.Refresh();
            Check(!context.IsPickupPromptVisible,"Pickup outline and E disappear immediately when held");
            yield return Walk(new Vector3(-1.45f,.1f,-3));yield return Walk(new Vector3(5.28f,.1f,-.05f));
            Check(carry.Interact()&&!carry.IsCarrying,"Place pudding safely before using axe; no tool action while carrying");
            var doorAim=new Vector3(5.28f,.8f,1.5f);Command(Vector2.zero,doorAim,left:true,axe:true);yield return null;Command(Vector2.zero,doorAim);yield return null;
            Command(Vector2.zero,doorAim,left:true,axe:true);yield return null;
            for(int i=0;i<120&&!door.IsBroken;i++){Command(Vector2.zero,doorAim,axe:true);yield return null;}
            Check(door.IsBroken&&tools.SwingCount==3,"Existing axe still breaks door in exactly three hits");
            carry.RefreshNearby();Check(carry.Interact()&&carry.IsCarrying,"Original pudding can be picked up again after breaking door");
            yield return Walk(new Vector3(5.28f,.1f,1.65f));yield return Walk(new Vector3(4.15f,.1f,2.3f));
            Check(carry.CanDeliver&&mission.Remaining>0&&carry.Interact()&&!request.IsPoisoned,"Edible pudding reaches actual NPC hand before deadline");
            yield return Walk(new Vector3(2.7f,.1f,4.4f));
            float walkDistance=0,maxNpcZ=npc.transform.position.z;var lastNpc=npc.transform.position;
            for(int i=0;i<1500&&!npc.IsRescued;i++){Command(Vector2.zero,doorAim);walkDistance+=Vector3.Distance(npc.transform.position,lastNpc);lastNpc=npc.transform.position;maxNpcZ=Mathf.Max(maxNpcZ,lastNpc.z);yield return null;}
            trace.Add("NPC distance="+walkDistance+" maxZ="+maxNpcZ);
            Check(walkDistance<15&&maxNpcZ<3.4f,"Resident actually walks directly via the front door, never loops through the back yard");
            Check(npc.IsRescued&&mission.CurrentStatus==FirstReviewMission.Status.Won&&mission.CompletionCount==1,"Safe route and real evacuation complete first level exactly once");Capture("v2-rescue-complete.png");
            mission.Tick(1);mission.Tick(1);Check(mission.CompletionCount==1,"Repeated completion observation never awards a second rescue");
            var deadline=new GameObject("Temporary timeout fixture").AddComponent<FirstReviewMission>();deadline.AutomaticTick=false;deadline.Tick(float.NaN);deadline.Tick(-1);
            Check(deadline.Remaining==60,"Invalid deadline deltas leave the timer unchanged");deadline.Tick(60);deadline.NotifyDelivered();
            Check(deadline.CurrentStatus==FirstReviewMission.Status.Failed&&deadline.FailureCause==FirstReviewMission.Failure.TimeExpired&&!deadline.CanDeliver,"Exact timeout rejects late pudding delivery");Destroy(deadline.gameObject);
            var warningFixture=new GameObject("Temporary hose-limit UI fixture");var warningHose=warningFixture.AddComponent<HoseConnection>();
            var length=new SerializedObject(warningHose);length.FindProperty("maximumLength").floatValue=10;length.ApplyModifiedPropertiesWithoutUndo();
            var fixtureSource=new GameObject("Temporary UI fixture source").AddComponent<FluidSource>();fixtureSource.transform.position=warningFixture.transform.position;
            warningHose.TryConnect(fixtureSource);warningFixture.transform.position=Vector3.right*10;
            var feedback=warningFixture.AddComponent<PickupContextView>();feedback.Initialize(null,warningHose,null,Camera.main,Shader.Find("FireRescue/InteractionOutline"));feedback.Refresh();
            Check(warningHose.IsAtLengthLimit&&feedback.IsLengthWarningVisible,"10m tension displays the contextual F disconnect warning");
            warningFixture.transform.position=Vector3.right*9.5f;feedback.Refresh();Check(!feedback.IsLengthWarningVisible,"Backing away from tension removes warning");
            warningFixture.transform.position=Vector3.right*10;feedback.Refresh();
            var otherSource=new GameObject("Temporary nearby alternate source").AddComponent<FluidSource>();otherSource.transform.position=warningFixture.transform.position;
            var fInput=warningFixture.AddComponent<LocalPlayerInput>();fInput.ReadDevices=false;fInput.Initialize(null,warningHose,null,Camera.main,new[]{fixtureSource,otherSource},Array.Empty<RescueNpc>());
            fInput.TickPointerCommand(Vector2.zero,Vector3.forward,true,false,false,false,false,false,Dt);feedback.Refresh();
            Check(!warningHose.IsConnected&&!feedback.IsLengthWarningVisible,"Actual pointer F disconnects even beside a different source; warning disappears");Destroy(warningFixture);Destroy(fixtureSource.gameObject);Destroy(otherSource.gameObject);
            // Separate physical-impact branch. Only floor suppression is injected so the
            // oil collider/packet/receiver reaction can be isolated without dying en route.
            yield return Reload();yield return Walk(new Vector3(.45f,.1f,.2f));yield return Walk(new Vector3(.45f,.1f,2.65f));yield return Right();yield return Right();
            yield return Walk(new Vector3(.45f,.1f,.2f));yield return Walk(new Vector3(-1.45f,.1f,-3.45f));
            field.ApplyTimedWater(new Vector3(-4,.1f,-3.4f),100,12,60);
            yield return Walk(new Vector3(-3.9f,.1f,-3.46f));yield return Spray(pot.transform.position,.35f);
            Check(pot.WaterApplied>.08f&&pot.IsBurning&&incident.ExplosionCount==1&&food.IsBurnt,"Actual swept water packets hit authored pot and scorch pudding, not just direct Receive tests");
            Capture("physical-water-oil-burst.png",new Vector3(-4.9f,.6f,-3.7f),2.1f);
            ReviewFireLoopV2Validation.Finish(true,"Checks="+checks.Count+"\n"+string.Join("\n",checks)+"\n"+string.Join("\n",trace)+"\nCommands + real controllers and swept fluid. No manual input acceptance.");
        }
        void Capture(string filename,Vector3? focus=null,float size=8.2f)
        {
            var cam=new GameObject("Temporary v2 review camera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=size;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.3f,.34f,.32f);
            cam.transform.rotation=Quaternion.Euler(50,0,0);cam.transform.position=(focus??new Vector3(0,.4f,0))-cam.transform.forward*24;
            var rt=new RenderTexture(1280,900,24);var old=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1280,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,900),0,0);tex.Apply();File.WriteAllBytes(ReleasePolishSetup.Evidence+"/"+filename,tex.EncodeToPNG());
            RenderTexture.active=old;cam.targetTexture=null;rt.Release();Destroy(rt);Destroy(tex);Destroy(cam.gameObject);
        }
        bool CanReachStorage(bool fences)
        {
            var boundary=GameObject.Find("First Review 1027/Prototype Courtyard Boundary");boundary.SetActive(fences);Physics.SyncTransforms();
            var probe=new GameObject("Temporary route reachability probe");probe.transform.position=input.transform.position;
            var agent=probe.AddComponent<RescueNpc>();agent.Autonomous=false;
            var exit=new GameObject("Temporary storage goal");exit.transform.position=new Vector3(-5.4f,.1f,3.95f);agent.Initialize(field,exit.transform,Array.Empty<ObjectFire>());agent.Tick(0);
            bool reached=agent.HasSafePath;Destroy(probe);Destroy(exit);boundary.SetActive(true);Physics.SyncTransforms();return reached;
        }
    }
}
