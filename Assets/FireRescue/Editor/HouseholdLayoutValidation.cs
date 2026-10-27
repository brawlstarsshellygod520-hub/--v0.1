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
    public static class HouseholdLayoutValidation
    {
        const string Key="FireRescue.HouseholdOutlineTest";static bool done;
        static HouseholdLayoutValidation(){EditorApplication.playModeStateChanged+=s=>{if(s!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;done=false;Application.logMessageReceived+=OnLog;new GameObject("Temporary household checks").AddComponent<HouseholdLayoutDriver>();};}
        public static bool Probe => SessionState.GetBool(Key+"Probe",false);
        public static void BatchRun(){SessionState.SetBool(Key+"Probe",false);Begin();}
        public static void BatchProbeNpc(){SessionState.SetBool(Key+"Probe",true);Begin();}
        static void Begin(){EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();}
        static void OnLog(string t,string s,LogType ty){if(ty==LogType.Error || ty==LogType.Assert || ty==LogType.Exception)Finish(false,t+"\n"+s);}
        public static void Finish(bool pass,string details){if(done)return;done=true;SessionState.SetBool(Key,false);Application.logMessageReceived-=OnLog;File.WriteAllText(HouseholdLayoutSetup.Evidence+(Probe?"/npc-layout-probe.txt":"/household-outline-playmode.txt"),(Probe?"NPC_LAYOUT_PROBE\n":pass?"HOUSEHOLD_OUTLINE_PASS\n":"FAIL\n")+details);EditorApplication.delayCall+=()=>EditorApplication.Exit(pass?0:1);}
    }
    public sealed class HouseholdLayoutDriver:MonoBehaviour
    {
        readonly List<string> checks=new List<string>();float started;
        void Start(){started=Time.realtimeSinceStartup;Time.captureDeltaTime=1f/60;StartCoroutine(HouseholdLayoutValidation.Probe?ProbeNpc():Run());}
        void OnDestroy(){Time.captureDeltaTime=0;}
        void Update(){if(Time.realtimeSinceStartup-started>90)HouseholdLayoutValidation.Finish(false,string.Join("\n",checks)+"\nTimeout");}
        void Check(bool pass,string s){if(!pass)throw new InvalidOperationException(string.Join("\n",checks)+"\nFAIL "+s);checks.Add("PASS "+s);}
        IEnumerator ProbeNpc()
        {
            yield return null;
            var layout=GameObject.Find(UserLevelOneBuilder.LayoutName);var player=layout.GetComponentInChildren<LocalPlayerInput>();player.ReadDevices=false;
            // Isolate furniture clearance, not the known player-body blocking case at the exit.
            var body=player.GetComponent<CharacterController>();body.enabled=false;player.transform.position=new Vector3(10,0,-9);body.enabled=true;Physics.SyncTransforms();
            var field=layout.GetComponentInChildren<FireField>();field.Simulate=false;field.ApplyWater(Vector3.zero,100,100);
            foreach(var fire in layout.GetComponentsInChildren<ObjectFire>())fire.gameObject.SetActive(false);
            layout.GetComponentInChildren<BreakableDoor>().ReceiveAxeHit(100);
            var npc=layout.GetComponentInChildren<RescueNpc>();
            for(int i=0;i<1800 && !npc.IsRescued;i++)yield return null;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var path=(List<Vector2Int>)typeof(RescueNpc).GetField("path",flags).GetValue(npc);
            int index=(int)typeof(RescueNpc).GetField("pathIndex",flags).GetValue(npc);
            string report="Rescued="+npc.IsRescued+" state="+npc.CurrentState+" safe="+npc.HasSafePath+" pos="+npc.transform.position.ToString("F4")+" index="+index+"\n";
            for(int i=Mathf.Max(0,index-2);i<Mathf.Min(path.Count,index+8);i++)report+="Route "+i+"="+field.CellCenter(path[i].x,path[i].y).ToString("F3")+"\n";
            foreach(var c in Physics.OverlapBox(npc.transform.position+Vector3.up*.7f,new Vector3(.7f,.7f,.7f)))report+="Nearby "+c.name+" "+c.bounds.ToString("F3")+" layer="+c.gameObject.layer+"\n";
            HouseholdLayoutSetup.RenderOverview("npc-layout-probe.png");HouseholdLayoutValidation.Finish(true,report);
        }
        IEnumerator Run()
        {
            yield return null;
            var layout=GameObject.Find(UserLevelOneBuilder.LayoutName);var player=layout.GetComponentInChildren<LocalPlayerInput>();player.ReadDevices=false;player.SetInputFocus(true);
            var field=layout.GetComponentInChildren<FireField>();field.Simulate=false;layout.GetComponentInChildren<RescueNpc>().Autonomous=false;
            var set=layout.transform.Find(HouseholdLayoutSetup.Marker);
            Check(set && set.childCount==6,"Six household area groups persist in the active tutorial");
            foreach(string kind in HouseholdPropCatalog.Kinds)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(HouseholdPropCatalog.PrefabPath(kind));
                Check(prefab && prefab.GetComponent<BoxCollider>() && prefab.transform.Find("Visual") &&
                    prefab.transform.Find("Visual").GetComponentsInChildren<Collider>(true).Length==0 &&
                    prefab.transform.Find("Visual").GetComponentsInChildren<MonoBehaviour>(true).Length==0,kind+" has replaceable Visual and independent root collision");
            }
            Check(!layout.transform.Find("Household Props/Bed").gameObject.activeSelf && !layout.transform.Find("Household Props/Sofa").gameObject.activeSelf,"Old primitive blocks remain hidden backups");
            var table=layout.transform.Find("Household Props/Coffee table");
            Check(table.GetComponent<Rigidbody>() && !table.GetComponent<Rigidbody>().isKinematic && table.GetComponent<Collider>().enabled,"Original pushable coffee-table mechanics stay on the unchanged root");
            var water=layout.GetComponentsInChildren<FluidSource>().Single(s=>s.Element==ElementKind.Water);
            Check(Vector3.Distance(water.transform.position,new Vector3(-3.3f,0,2.85f))<.001f && player.GetComponent<HoseConnection>().MaximumLength==3,"Water source position and 3m hose preserved");
            Check(!set.GetComponentsInChildren<TextMesh>(true).Any(),"No persistent room or item debug text added");
            var shader=Shader.Find("FireRescue/InteractionOutline");
            Check(shader && !ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Built-in outline shader compiles without errors");

            // Separate rendering observation from full traversal regression: this is only a visual probe.
            var controller=player.GetComponent<CharacterController>();controller.enabled=false;player.transform.position=new Vector3(-2.3f,.02f,2);controller.enabled=true;Physics.SyncTransforms();
            player.TickPointerCommand(Vector2.zero,water.transform.position,false,false,false,false,false,false,1f/60);
            yield return new WaitForSeconds(.8f);
            var view=player.GetComponentInChildren<SourceHighlightView>();
            Check(view && view.CurrentSource==water && view.HasVisibleOutline,"Real scene selects only nearby toilet source outline");
            HouseholdLayoutSetup.Render(Camera.main,"source-gold-game.png");
            Check(player.GetComponent<HoseConnection>().TryConnect(water),"Real source connects with unchanged hose API");
            yield return null;HouseholdLayoutSetup.Render(Camera.main,"source-blue-game.png");
            Check(!player.GetComponentsInChildren<LineRenderer>(true).Any(r=>r.name=="Source proximity ring"),"Old ground proximity circle is not created");
            player.GetComponent<HoseConnection>().Disconnect();
            yield return SourceOutlineValidation.Run(checks);
            // Source rendering should never remove source collision or actors' gameplay anchors.
            Check(water.GetComponent<Collider>().enabled && player.transform.Find("Anchors"),"Outline preserves source collider and actor anchors");
            Rooms();
            HouseholdLayoutValidation.Finish(true,"Checks="+checks.Count+"\n"+string.Join("\n",checks));
        }
        void Rooms()
        {
            var camera=new GameObject("Temporary room review camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=4.5f;camera.transform.rotation=Quaternion.Euler(50,0,0);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.25f,.27f,.29f);
            var views=new[]{(name:"living-room.png",focus:new Vector3(3.5f,.6f,-2.1f)),(name:"kitchen.png",focus:new Vector3(-4.9f,.6f,-2.2f)),(name:"bedroom.png",focus:new Vector3(4.8f,.6f,9.0f)),(name:"bathroom.png",focus:new Vector3(-4.8f,.6f,2.8f))};
            foreach(var v in views){camera.transform.position=v.focus-camera.transform.forward*20;HouseholdLayoutSetup.Render(camera,v.name);}
            Destroy(camera.gameObject);
        }
    }
}
