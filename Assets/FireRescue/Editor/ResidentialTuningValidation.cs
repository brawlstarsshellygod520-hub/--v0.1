using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    [InitializeOnLoad]
    public static class ResidentialTuningValidation
    {
        const string Key="FireRescue.Resident5mTest";static bool done;
        static ResidentialTuningValidation(){EditorApplication.playModeStateChanged+=s=>{if(s!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;done=false;Application.logMessageReceived+=OnLog;new GameObject("Temporary resident and hose tuning checks").AddComponent<ResidentialTuningDriver>();};}
        public static string Evidence=>SessionState.GetString(Key+"Evidence",ResidentialTuningSetup.Evidence);
        public static void BatchRun(){SessionState.SetString(Key+"Evidence",ResidentialTuningSetup.Evidence);Begin();}
        public static void BatchRunDelivery(){SessionState.SetString(Key+"Evidence",PrototypeDeliverySetup.Evidence);Begin();}
        static void Begin(){EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();}
        static void OnLog(string t,string s,LogType ty){if(ty==LogType.Error || ty==LogType.Assert || ty==LogType.Exception)Finish(false,t+"\n"+s);}
        public static void Finish(bool pass,string details){if(done)return;done=true;SessionState.SetBool(Key,false);Application.logMessageReceived-=OnLog;File.WriteAllText(Evidence+"/tuning-playmode.txt",(pass?"RESIDENT_5M_PASS\n":"FAIL\n")+details);EditorApplication.delayCall+=()=>EditorApplication.Exit(pass?0:1);}
    }
    public sealed class ResidentialTuningDriver:MonoBehaviour
    {
        readonly List<string> checks=new List<string>();float started;
        void Start(){started=Time.realtimeSinceStartup;Time.captureDeltaTime=1f/60;StartCoroutine(Run());}
        void OnDestroy(){Time.captureDeltaTime=0;}
        void Update(){if(Time.realtimeSinceStartup-started>60)ResidentialTuningValidation.Finish(false,string.Join("\n",checks)+"\nTimeout");}
        void Check(bool pass,string s){if(!pass)throw new InvalidOperationException(string.Join("\n",checks)+"\nFAIL "+s);checks.Add("PASS "+s);}
        IEnumerator Run()
        {
            yield return null;
            var player=PlayerWhiteboxSetup.Player();player.GetComponent<LocalPlayerInput>().ReadDevices=false;
            var npc=UserCharacterSetup.CurrentNpc();npc.Autonomous=false;
            var field=GameObject.Find(UserLevelOneBuilder.LayoutName).GetComponentInChildren<FireField>();field.Simulate=false;
            ResidentialTuningSetup.ValidateChairs();Check(true,"Two armchairs exchanged and now face inward without narrowing their footprints");
            var visual=npc.transform.Find("Visual/"+CharacterSwapSetup.NpcVisual).gameObject;
            ResidentPaletteSetup.Validate(visual);Check(true,"Current Role1 resident uses the orange/yellow/skin/eye reference palette");
            var animator=visual.GetComponentInChildren<Animator>();
            Check(animator.avatar && animator.avatar.isValid && !animator.applyRootMotion && animator.runtimeAnimatorController.animationClips.Any(c=>c.name=="Idle") && animator.runtimeAnimatorController.animationClips.Any(c=>c.name=="Walk"),"Existing rig and Idle/Walk remain attached with no root motion");
            Check(visual.GetComponentsInChildren<SkinnedMeshRenderer>().Length==10 && !visual.GetComponentsInChildren<Collider>(true).Any(),"All ten original resident mesh parts remain visual-only");
            Check(player.GetComponent<HoseConnection>().MaximumLength==5,"Actual tutorial player uses the saved 5m hose override");
            Check(AssetDatabase.LoadAssetAtPath<GameObject>(GreyboxPrefabBuilder.PrefabPath("Player")).GetComponent<HoseConnection>().MaximumLength==5,"Reusable Player prefab also stores the 5m length");
            ResidentPaletteSetup.Validate(AssetDatabase.LoadAssetAtPath<GameObject>(CharacterSwapSetup.Prefab(1)));Check(true,"Reusable resident prefab, not only scene overrides, has the reference palette");
            Check(EditorBuildSettings.scenes.First(s=>s.enabled).path==TutorialGreyboxSetup.ScenePath,"Default Windows build startup points to the updated prototype");
            Check(Camera.main.orthographic && Mathf.Abs(Camera.main.orthographicSize-6)<.001f && Quaternion.Angle(Camera.main.transform.rotation,Quaternion.Euler(50,0,0))<.01f,"Existing camera presentation remains unchanged");
            CaptureResident(visual);CaptureRoom();
            yield return VerifyFiveMetres();
            ResidentialTuningValidation.Finish(true,"Checks="+checks.Count+"\n"+string.Join("\n",checks));
        }
        IEnumerator VerifyFiveMetres()
        {
            var root=new GameObject("5m isolated tether fixture");var origin=new Vector3(1000,0,1000);
            var sourceObject=new GameObject("Source");sourceObject.transform.SetParent(root.transform);sourceObject.transform.position=origin;
            var source=sourceObject.AddComponent<FluidSource>();source.Configure(ElementKind.Water);
            var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.SetParent(root.transform);ground.transform.position=origin+new Vector3(0,-.1f,0);ground.transform.localScale=new Vector3(30,.2f,30);
            var actor=new GameObject("Motor");actor.transform.SetParent(root.transform);actor.transform.position=origin+Vector3.right;
            var controller=actor.AddComponent<CharacterController>();controller.height=1.6f;controller.center=Vector3.up*.8f;controller.radius=.28f;
            var hose=actor.AddComponent<HoseConnection>();
            Check(hose.MaximumLength==3,"Historical raw component default stays compatible; current scene and Player prefab use 5m");
            var serialized=new SerializedObject(hose);serialized.FindProperty("maximumLength").floatValue=5;serialized.ApplyModifiedPropertiesWithoutUndo();
            var motor=actor.AddComponent<FirefighterMotor>();motor.Initialize(null,hose);Physics.SyncTransforms();yield return null;
            Check(hose.TryConnect(source),"5m fixture connects using the existing source API");
            for(int i=0;i<150;i++){motor.Move(Vector3.right,1f/60);yield return null;}
            Check(hose.UsedLength>4.8f && hose.UsedLength<=5.02f,"Real CharacterController movement passes old 3m boundary and settles at 5m");
            float edge=actor.transform.position.x;
            for(int i=0;i<40;i++){motor.Move(Vector3.right,1f/60);yield return null;}
            Check(Mathf.Abs(actor.transform.position.x-edge)<.015f && motor.Velocity.x<.15f,"Holding outward input cannot stretch the 5m cable or retain outward momentum");
            for(int i=0;i<25;i++){motor.Move(Vector3.left,1f/60);yield return null;}
            Check(hose.UsedLength<4.5f,"Walking back immediately releases the 5m boundary");
            Check(Mathf.Abs(Vector3.Distance(origin,hose.ConstrainPosition(origin+Vector3.right*10))-5)<.002f,"Direct free-space cable constraint clamps to exactly 5m");
            controller.enabled=false;actor.transform.position=origin+new Vector3(1.25f,0,.75f);controller.enabled=true;Physics.SyncTransforms();
            var windowObject=new GameObject("Window");windowObject.transform.SetParent(root.transform);windowObject.transform.position=origin+Vector3.right*1.25f;
            var route=new GameObject("Route").transform;route.SetParent(windowObject.transform,false);route.localPosition=Vector3.up*1.15f;
            var interaction=new GameObject("Interaction").transform;interaction.SetParent(windowObject.transform,false);interaction.localPosition=Vector3.up*.35f;
            var window=windowObject.AddComponent<HoseWindow>();window.Initialize(route,interaction);
            Check(hose.TryRouteThrough(window),"5m cable can still be explicitly routed through a nearby window");
            Vector3 limit=hose.ConstrainPosition(origin+new Vector3(1.25f,0,10));
            float folded=1.25f+Vector3.Distance(new Vector3(route.position.x,0,route.position.z),new Vector3(limit.x,0,limit.z));
            Check(Mathf.Abs(folded-5)<.002f && Mathf.Abs(limit.z-origin.z-3.75f)<.002f,"Window prefix and remaining cable share one 5m budget, not two 5m segments");
            Destroy(root);yield return null;
        }
        void CaptureResident(GameObject source)
        {
            var model=Instantiate(source);model.name="Temporary colored resident preview";model.transform.position=new Vector3(100,0,0);model.transform.rotation=Quaternion.identity;
            model.GetComponent<CharacterLocomotionAnimation>().enabled=false;var animator=model.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);animator.Play("Idle",0,.2f);animator.Update(0);
            var camera=new GameObject("Resident preview camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=.95f;camera.transform.rotation=Quaternion.Euler(5,180,0);camera.transform.position=new Vector3(100,.76f,0)-camera.transform.forward*5;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.3f,.3f,.3f);
            Render(camera,"resident-front.png",700,800);animator.SetFloat("Speed",1);animator.SetFloat("CycleSpeed",1);animator.Play("Walk",0,.2f);animator.Update(0);camera.transform.rotation=Quaternion.Euler(25,145,0);camera.transform.position=new Vector3(100,.76f,0)-camera.transform.forward*5;Render(camera,"resident-walk.png",700,800);Destroy(camera.gameObject);Destroy(model);
        }
        void CaptureRoom()
        {
            var camera=new GameObject("Armchair review camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=3.2f;camera.transform.rotation=Quaternion.Euler(50,0,0);camera.transform.position=new Vector3(3.5f,.6f,-1.9f)-camera.transform.forward*20;Render(camera,"armchairs-after.png",1000,650);Destroy(camera.gameObject);
        }
        static void Render(Camera camera,string name,int width,int height)
        {
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(width,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(ResidentialTuningValidation.Evidence+"/"+name,texture.EncodeToPNG());}
            finally{RenderTexture.active=previous;camera.targetTexture=null;Destroy(texture);target.Release();Destroy(target);}
        }
    }
}
