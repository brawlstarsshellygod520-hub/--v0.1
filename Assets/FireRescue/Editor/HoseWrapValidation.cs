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
    public static class HoseWrapValidation
    {
        public const string Evidence="Docs/Validation/2026-10-27-hose-wrapping";
        const string Key="FireRescue.HoseWrapValidation";static bool done;
        static HoseWrapValidation(){EditorApplication.playModeStateChanged+=s=>{if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Key,false))return;done=false;Application.logMessageReceived+=OnLog;new GameObject("Temporary hose wrap checks").AddComponent<HoseWrapDriver>();};}
        public static void BatchRun(){Directory.CreateDirectory(Evidence);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();}
        public static void BatchEnable()
        {
            try
            {
                Directory.CreateDirectory(Evidence);
                var scene=EditorSceneManager.OpenScene(FirstRescue1026Setup.ScenePath);
                var root=GameObject.Find("First Rescue 1026");var hose=root.GetComponentInChildren<HoseConnection>();
                hose.SetAutomaticWrapping(true);EditorUtility.SetDirty(hose);
                root.transform.Find("Bathroom doorway hose guide").gameObject.SetActive(false);
                var data=new SerializedObject(hose.GetComponent<LocalPlayerInput>());data.FindProperty("hoseWindows").arraySize=0;data.ApplyModifiedPropertiesWithoutUndo();
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save opt-in wrapping.");
                File.WriteAllText(Evidence+"/install.txt","WRAP_INSTALL_PASS\nFirstRescue1026: automatic wrapping enabled; 5m unchanged.\nManual bathroom guide disabled (preserved), input list cleared.\nNo map, source, camera, NPC, spray profile or model replaced.\n");EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void OnLog(string t,string stack,LogType ty){if(ty==LogType.Error||ty==LogType.Exception||ty==LogType.Assert)Finish(false,t+"\n"+stack);}
        public static void Finish(bool pass,string text){if(done)return;done=true;SessionState.SetBool(Key,false);Application.logMessageReceived-=OnLog;Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/playmode.txt",(pass?"HOSE_WRAP_PASS\n":"HOSE_WRAP_FAIL\n")+text);EditorApplication.delayCall+=()=>EditorApplication.Exit(pass?0:1);}
    }
    public sealed class HoseWrapDriver:MonoBehaviour
    {
        const float Dt=1f/60;readonly List<string> checks=new List<string>();float began;
        void Start(){Time.captureDeltaTime=Dt;began=Time.realtimeSinceStartup;StartCoroutine(Run());}
        void OnDestroy(){Time.captureDeltaTime=0;}
        void Update(){if(Time.realtimeSinceStartup-began>150)HoseWrapValidation.Finish(false,string.Join("\n",checks)+"\nTimeout");}
        void Check(bool v,string s){if(!v)throw new InvalidOperationException(string.Join("\n",checks)+"\nFAIL "+s);checks.Add("PASS "+s);}
        IEnumerator Run()
        {
            var f=Fixture(new Vector3(0,0,0),new Vector3(.18f,1,2));
            Check(f.hose.TryConnect(f.source),"Connect on the source side of the wall");
            yield return Walk(f,new Vector3(-1,0,1.6f));
            for(int i=0;i<70;i++){f.motor.Move(Vector3.right,Dt);yield return null;}
            Check(f.actor.position.x<.9f,"Baseline direct-chord mode stops although the actor could walk past the wall tip");
            yield return Walk(f,new Vector3(-1,0,1.6f));f.hose.SetAutomaticWrapping(true);
            yield return Walk(f,new Vector3(1,0,1.6f));
            yield return Walk(f,new Vector3(1,0,.1f));
            Check(f.hose.AutomaticBendCount>0&&f.hose.UsedLength>Vector3.Distance(f.source.transform.position,f.actor.position)+.3f,"Walking around wall retains contact bends and charges their extra length");
            Check(PathClear(f.hose),"Every authoritative segment clears the thick wall");
            yield return null;yield return null;
            var tube=f.actor.GetComponentsInChildren<LineRenderer>().Single(x=>x.name=="Hose tube");
            Check(tube.enabled&&tube.positionCount>18&&TubeClear(f.hose,tube),"White hose renders all wrapped legs without clipping or disappearing");
            Capture(f,"wall-wrap.png");
            float old=f.hose.UsedLength;Vector3 pos=f.actor.position;
            for(int i=0;i<40;i++){f.motor.Move(Vector3.zero,Dt);yield return null;}
            Check(Vector3.Distance(pos,f.actor.position)<.03f&&Mathf.Abs(old-f.hose.UsedLength)<.04f,"Idle contact path remains stable");
            for(int i=0;i<160;i++){f.motor.Move(Vector3.right,Dt);yield return null;}
            Check(f.hose.UsedLength>4.9f&&f.hose.UsedLength<=5.01f,"Wrapped polyline, not source straight distance, stops at 5m");
            pos=f.actor.position;
            for(int i=0;i<30;i++){f.motor.Move(Vector3.right,Dt);yield return null;}
            Check(Vector3.Distance(pos,f.actor.position)<.02f&&f.motor.Velocity.x<.15f,"Holding outward input cannot stretch cable or store outward velocity");
            yield return Walk(f,new Vector3(1,0,1.6f));yield return Walk(f,new Vector3(-1,0,1.6f));yield return Walk(f,new Vector3(-1,0,0));
            Check(f.hose.AutomaticBendCount==0&&PathClear(f.hose),"Retracing around the tip unwraps contacts without disconnecting");
            f.hose.Disconnect();Check(f.hose.AutomaticBendCount==0&&f.hose.PathPointCount==0,"Disconnect clears all transient contact state");
            Check(f.hose.TryConnect(f.source)&&f.hose.AutomaticBendCount==0,"Reconnect begins with a clean direct hose");
            f.source.gameObject.SetActive(false);Check(!f.hose.IsConnected&&f.hose.AutomaticBendCount==0,"Source disable clears stale contacts");
            Destroy(f.root);yield return null;
            yield return Pillar();
            HoseWrapValidation.Finish(true,"Checks="+checks.Count+"\n"+string.Join("\n",checks));
        }
        IEnumerator Pillar()
        {
            var f=Fixture(new Vector3(15,0,0),new Vector3(.4f,1,.4f));
            f.hose.SetAutomaticWrapping(true);Check(f.hose.TryConnect(f.source),"Pillar fixture connects");
            yield return Walk(f,new Vector3(-1,0,-.8f));yield return Walk(f,new Vector3(.65f,0,-.8f));yield return Walk(f,new Vector3(.65f,0,.8f));yield return Walk(f,new Vector3(-1,0,.8f));
            Check(f.hose.AutomaticBendCount>=2&&f.hose.UsedLength>2.5f&&PathClear(f.hose),"Pillar loop cannot shortcut through the solid inside its swept area");
            Capture(f,"pillar-wrap.png");
            yield return Walk(f,new Vector3(.65f,0,.8f));yield return Walk(f,new Vector3(.65f,0,-.8f));yield return Walk(f,new Vector3(-1,0,-.8f));
            Check(f.hose.AutomaticBendCount==0,"Reverse traversal unwinds the pillar loop");
            Destroy(f.root);yield return null;
        }
        IEnumerator Walk(F f,Vector3 local)
        {
            Vector3 target=f.origin+local;int i;
            for(i=0;i<650;i++){Vector3 d=target-f.actor.position;d.y=0;if(d.magnitude<.06f)break;f.motor.Move(Vector3.ClampMagnitude(d*4,1),Dt);yield return null;}
            Check(i<650,"Walk reached "+local.ToString("F2")+" actual="+(f.actor.position-f.origin).ToString("F3")+" bends="+f.hose.AutomaticBendCount+" used="+f.hose.UsedLength.ToString("F3"));
            for(int j=0;j<8;j++){f.motor.Move(Vector3.zero,Dt);yield return null;}
        }
        static bool PathClear(HoseConnection h){for(int i=1;i<h.PathPointCount;i++)if(h.IsCableBlocked(h.GetPathPoint(i-1),h.GetPathPoint(i)))return false;return h.UsedLength<=5.01f;}
        static bool TubeClear(HoseConnection h,LineRenderer tube){for(int i=1;i<tube.positionCount;i++)if(h.IsCableBlocked(tube.GetPosition(i-1),tube.GetPosition(i)))return false;return true;}
        F Fixture(Vector3 origin,Vector3 wallSize)
        {
            var root=new GameObject("Wrapping fixture");var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.transform.SetParent(root.transform);ground.transform.position=origin+new Vector3(0,-.1f,0);ground.transform.localScale=new Vector3(14,.2f,10);
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="Wall";obstacle.transform.SetParent(root.transform);obstacle.transform.position=origin+Vector3.up*.5f;obstacle.transform.localScale=wallSize;obstacle.layer=8;
            var geometry=root.AddComponent<SprayWallOcclusion>();geometry.Configure(new[]{obstacle.GetComponent<Collider>()},Array.Empty<HoseWindow>());
            var sourceGo=new GameObject("Source");sourceGo.transform.SetParent(root.transform);sourceGo.transform.position=origin+new Vector3(-1,0,-.8f);var source=sourceGo.AddComponent<FluidSource>();source.Configure(ElementKind.Water);
            var actor=new GameObject("Actor");actor.transform.SetParent(root.transform);actor.transform.position=origin+new Vector3(-1,0,0);var cc=actor.AddComponent<CharacterController>();cc.radius=.2f;cc.height=1.6f;cc.center=Vector3.up*.8f;
            var inlet=new GameObject("Inlet").transform;inlet.SetParent(actor.transform,false);inlet.localPosition=Vector3.up*.9f;
            var hose=actor.AddComponent<HoseConnection>();var s=new SerializedObject(hose);s.FindProperty("maximumLength").floatValue=5;s.ApplyModifiedPropertiesWithoutUndo();hose.ConfigurePhysics(geometry,inlet);
            var motor=actor.AddComponent<FirefighterMotor>();motor.Initialize(null,hose);var emitter=actor.AddComponent<SprayEmitter>();emitter.Initialize(hose,null);emitter.SetHoseInletAnchor(inlet);emitter.EnableDirectionalSpray(true);
            var view=new GameObject("Hose view");view.transform.SetParent(actor.transform);view.AddComponent<HoseView>().Initialize(emitter);Physics.SyncTransforms();
            return new F{root=root,origin=origin,actor=actor.transform,hose=hose,motor=motor,source=source};
        }
        static void Capture(F f,string name)
        {
            var camera=new GameObject("Wrap evidence camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=3.4f;camera.transform.rotation=Quaternion.Euler(65,0,0);camera.transform.position=f.origin-camera.transform.forward*12;
            var light=new GameObject("Wrap evidence light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(50,25,0);
            var rt=new RenderTexture(1000,800,24);camera.targetTexture=rt;var old=RenderTexture.active;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1000,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1000,800),0,0);tex.Apply();File.WriteAllBytes(HoseWrapValidation.Evidence+"/"+name,tex.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;rt.Release();Destroy(rt);Destroy(tex);Destroy(camera.gameObject);Destroy(light.gameObject);
        }
        sealed class F{public GameObject root;public Vector3 origin;public Transform actor;public HoseConnection hose;public FirefighterMotor motor;public FluidSource source;}
    }
}
