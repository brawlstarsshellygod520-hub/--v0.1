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
    public static class FirstRescue1026Validation
    {
        const string Key="FireRescue.FirstRescue1026.Validation";static bool done;
        static FirstRescue1026Validation(){EditorApplication.playModeStateChanged+=s=>{if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Key,false))return;done=false;Application.logMessageReceived+=OnLog;new GameObject("Temporary first rescue checks").AddComponent<FirstRescue1026Driver>();};}
        public static void BatchRun(){Directory.CreateDirectory(FirstRescue1026Setup.Evidence);EditorSceneManager.OpenScene(FirstRescue1026Setup.ScenePath);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();}
        public static void BatchFixtures(){SessionState.SetBool("FireRescue.FirstRescue1026.Fixtures",true);BatchRun();}
        static void OnLog(string t,string s,LogType ty){if(ty==LogType.Error||ty==LogType.Exception||ty==LogType.Assert)Finish(false,t+"\n"+s);}
        public static void Finish(bool pass,string details){if(done)return;done=true;SessionState.SetBool(Key,false);Application.logMessageReceived-=OnLog;File.WriteAllText(FirstRescue1026Setup.Evidence+"/playmode.txt",(pass?"FIRST_RESCUE_PLAYMODE_PASS\n":"FIRST_RESCUE_FAIL\n")+details);EditorApplication.delayCall+=()=>EditorApplication.Exit(pass?0:1);}
    }
    public sealed class FirstRescue1026Driver:MonoBehaviour
    {
        readonly List<string> checks=new List<string>();readonly List<string> trace=new List<string>();
        LocalPlayerInput input;FireField field;RescueNpc npc;SprayEmitter emitter;HoseConnection hose;RescueGameManager manager;
        float started;const float Dt=1f/60;int completions;
        void Start(){started=Time.realtimeSinceStartup;Time.captureDeltaTime=Dt;Application.logMessageReceived+=TraceError;StartCoroutine(Run());}
        void TraceError(string t,string stack,LogType ty){if(ty==LogType.Exception||ty==LogType.Error)File.WriteAllLines(FirstRescue1026Setup.Evidence+"/trace.txt",trace);}
        void OnDestroy(){Time.captureDeltaTime=0;Application.logMessageReceived-=TraceError;}
        void Update(){if(Time.realtimeSinceStartup-started>180)FirstRescue1026Validation.Finish(false,string.Join("\n",checks)+"\nTimeout\n"+string.Join("\n",trace));}
        void Check(bool v,string s){if(!v)throw new InvalidOperationException(string.Join("\n",checks)+"\nFAIL "+s+"\n"+string.Join("\n",trace.TakeLast(20)));checks.Add("PASS "+s);}
        IEnumerator Run()
        {
            var root=GameObject.Find("First Rescue 1026");input=root.GetComponentInChildren<LocalPlayerInput>();input.ReadDevices=false;input.SetInputFocus(true);
            field=root.GetComponentInChildren<FireField>();field.Simulate=false;npc=root.GetComponentInChildren<RescueNpc>();npc.Autonomous=false;
            emitter=input.GetComponent<SprayEmitter>();emitter.AutomaticSimulation=false;hose=input.GetComponent<HoseConnection>();manager=root.GetComponentInChildren<RescueGameManager>();
            manager.RescueCompleted+=()=>completions++;
            yield return null;
            if(SessionState.GetBool("FireRescue.FirstRescue1026.Fixtures",false))
            {
                SessionState.SetBool("FireRescue.FirstRescue1026.Fixtures",false);
                yield return FirstRescueNpcValidation.Run(checks);
                yield return FirstRescueBallisticValidation.Run(checks);
                yield return FirstRescueEffectsValidation.Run(checks);
                Check(!checks.Any(s=>s.StartsWith("FAIL")),"Targeted isolated fixtures passed (not the scene loop)");
                FirstRescue1026Validation.Finish(true,string.Join("\n",checks));yield break;
            }
            Check(hose.MaximumLength==5 && root.GetComponentsInChildren<FluidSource>().Length==1,"One toilet source and unchanged 5m tether");
            Check(!root.GetComponentsInChildren<FireFieldView>().Any() && !root.GetComponentsInChildren<ObjectFire>().Any(),"No old grid pillars, furniture fire or duplicate fire authority");
            Check(input.transform.Find("Visual/Swap Player Role2") && npc.transform.Find("Visual/Swap Resident Role1") &&
                root.GetComponentsInChildren<Animator>().Count(a=>a.avatar&&a.avatar.isValid&&!a.applyRootMotion)>=2,"User character meshes/rigs retained");
            Check(!root.GetComponentsInChildren<MonoBehaviour>().Any(c=>!c),"No missing active scripts");
            npc.Tick(.3f);Check(field.BurningCount>0&&!npc.HasSafePath,"Authored bedroom door fire blocks the first resident before water");
            Capture("layout-overview.png",new Vector3(0,.5f,0),9,true);
            yield return Walk(new Vector3(5.5f,.1f,-4));
            yield return Walk(new Vector3(3,.1f,-2));
            yield return Walk(new Vector3(.24f,.1f,.25f));
            yield return Walk(new Vector3(.24f,.1f,1.05f));
            Command(Vector2.zero,Vector3.forward,false,true,true);yield return null;
            Check(input.GetComponent<FirefighterTools>().SelectedTool==FirefighterTools.Tool.Hose&&!hose.IsConnected,"First right press equips only");
            Command(Vector2.zero,Vector3.forward);yield return null;
            Command(Vector2.zero,Vector3.forward,false,true,true);yield return null;
            Check(hose.IsConnected&&!emitter.IsSpraying,"Second right press connects real toilet and does not spray");
            Command(Vector2.zero,Vector3.forward);yield return null;
            yield return Walk(new Vector3(.24f,.1f,.15f));
            Command(Vector2.zero,Vector3.forward,true);yield return null;
            Check(hose.ActiveWindow,"F at bathroom doorway sets explicit bend without a wall bypass");
            yield return Walk(FirstRescue1026Setup.SprayPosition);
            Check(hose.UsedLength<=5.01f && hose.UsedLength>3,"Player pulled the actual hose beyond 3m and below 5m");
            int burning=field.BurningCount;Vector3 target=FirstRescue1026Setup.FireTarget;
            int observed=0;emitter.ParticleImpacted+=hit=>{observed++;if(observed<8||observed%100==0)trace.Add("HIT "+observed+" "+hit.Position.ToString("F3")+" n="+hit.Normal.ToString("F2"));};
            trace.Add("Nozzle="+emitter.NozzlePosition+" settings="+JsonUtility.ToJson(hose.Source.SpraySettings));
            Command(Vector2.zero,target,false,true,true);yield return null;
            for(int i=0;i<900 && !npc.HasSafePath;i++)
            {
                Command(Vector2.zero,target,false,false,true);
                if(i==14)
                {
                    var fx=root.GetComponentInChildren<FirstRescueEffects>();
                    foreach(FirstRescueEffectMode mode in Enum.GetValues(typeof(FirstRescueEffectMode)))
                    {fx.Mode=mode;yield return null;Capture("water-"+mode+".png",input.transform.position+new Vector3(1.8f,.5f,1.7f),4,false);}
                    fx.Mode=FirstRescueEffectMode.Hybrid;
                }
                yield return null;
            }
            Command(Vector2.zero,target);yield return null;
            trace.Add("AFTER shots="+emitter.TotalEmitted+" impacts="+emitter.TotalImpacts+" burning "+burning+" -> "+field.BurningCount+" npc="+npc.CurrentState+" pos="+npc.transform.position);
            Check(emitter.TotalImpacts>0&&field.BurningCount<burning,"Physical flights hit surfaces and reduce real burning cells");
            Check(npc.HasSafePath,"Water cleared a complete bedroom-to-exit route; no teleport or direct ApplyWater used");
            for(int i=0;i<1400&&!npc.IsRescued;i++){Command(Vector2.zero,target);yield return null;}
            Check(npc.IsRescued && manager.RescuedCount==1 && manager.RescueComplete && completions==1,"Resident walked from bedroom through both real doorways to outside; completion exactly once");
            Capture("rescue-complete.png",new Vector3(4,.5f,-3),5,false);
            yield return FirstRescueNpcValidation.Run(checks);
            yield return FirstRescueBallisticValidation.Run(checks);
            yield return FirstRescueEffectsValidation.Run(checks);
            Check(!checks.Any(s=>s.StartsWith("FAIL")),"All bounded presentation probes passed");
            File.WriteAllLines(FirstRescue1026Setup.Evidence+"/trace.txt",trace);
            FirstRescue1026Validation.Finish(true,"Checks="+checks.Count+"\n"+string.Join("\n",checks)+"\nWater shots="+emitter.TotalEmitted+" impacts="+emitter.TotalImpacts+"\nNo GUI/device automation. Scripted input uses the same pointer command path as gameplay.");
        }
        void Command(Vector2 move,Vector3 aim,bool interact=false,bool pressed=false,bool held=false)
        {input.TickPointerCommand(move,aim,interact,false,false,false,pressed,held,Dt);emitter.Advance(Dt);field.Step(Dt);npc.Tick(Dt);}
        IEnumerator Walk(Vector3 target)
        {
            Vector3 start=input.transform.position;int i;
            for(i=0;i<600;i++)
            {Vector3 d=target-input.transform.position;d.y=0;if(d.magnitude<.09f)break;Command(Vector2.ClampMagnitude(new Vector2(d.x,d.z)*3,1),target);yield return null;}
            trace.Add("WALK "+start.ToString("F3")+" -> "+target.ToString("F3")+" actual="+input.transform.position.ToString("F3")+" frames="+i+" used="+hose.UsedLength+" blocked="+hose.IsBlocked);
            Check(i<600,"Walk reached "+target.ToString("F2"));
            Command(Vector2.zero,target);yield return null;
        }
        public static void Capture(string filename,Vector3 focus,float size,bool top)
        {
            var cam=new GameObject("Temporary offline render").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=size;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.25f,.28f,.27f);cam.transform.rotation=Quaternion.Euler(top?90:50,0,0);cam.transform.position=focus-cam.transform.forward*24;
            var rt=new RenderTexture(1280,900,24);cam.targetTexture=rt;var previous=RenderTexture.active;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,900),0,0);tex.Apply();File.WriteAllBytes(FirstRescue1026Setup.Evidence+"/"+filename,tex.EncodeToPNG());RenderTexture.active=previous;cam.targetTexture=null;Destroy(tex);rt.Release();Destroy(rt);Destroy(cam.gameObject);
        }
    }
}
