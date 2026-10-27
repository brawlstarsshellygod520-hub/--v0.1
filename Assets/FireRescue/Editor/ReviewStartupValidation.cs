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
    public static class ReviewStartupValidation
    {
        public const string Evidence="Docs/Validation/2026-10-28-grounding-tag";
        const string Key="FireRescue.StartupGround.Check";
        static ReviewStartupValidation(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false))new GameObject("Temporary startup grounding probe").AddComponent<ReviewStartupDriver>();};}
        public static void BatchRun(){Directory.CreateDirectory(Evidence);EditorSceneManager.OpenScene(FirstReview1027Setup.ScenePath);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();}
        public static void Finish(bool ok,string result){SessionState.SetBool(Key,false);File.WriteAllText(Evidence+"/startup.txt",(ok?"STARTUP_PASS\n":"STARTUP_FAIL\n")+result);EditorApplication.delayCall+=()=>EditorApplication.Exit(ok?0:1);}
    }
    public sealed class ReviewStartupDriver:MonoBehaviour
    {
        IEnumerator Start()
        {
            var root=GameObject.Find("First Review 1027");var player=root.GetComponentInChildren<FirefighterMotor>();var input=player.GetComponent<LocalPlayerInput>();var controller=player.GetComponent<CharacterController>();
            var ground=root.transform.Find("Ground").GetComponent<BoxCollider>();var notes=new List<string>();
            Application.runInBackground=true;
            float initial=player.transform.position.y,low=initial;int frames=0;float next=0,end=Time.realtimeSinceStartup+6;
            notes.Add("ReadDevices="+input.ReadDevices+" focused="+Application.isFocused+" ground="+ground.enabled+" trigger="+ground.isTrigger+" bounds="+ground.bounds);
            while(Time.realtimeSinceStartup<end)
            {
                yield return null;frames++;low=Mathf.Min(low,player.transform.position.y);
                if(Time.realtimeSinceStartup<next)continue;next=Time.realtimeSinceStartup+.5f;
                var hips=player.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Hips");
                var skin=player.GetComponentsInChildren<SkinnedMeshRenderer>().First();
                bool hit=Physics.Raycast(player.transform.position+Vector3.up*3,Vector3.down,out var h,10,(1<<0)|(1<<8),QueryTriggerInteraction.Ignore);
                notes.Add($"frame={frames} dt={Time.deltaTime:F4} rootY={player.transform.position.y:F4} grounded={controller.isGrounded} hipsY={hips.position.y:F4} visibleMinY={skin.bounds.min.y:F4} floorHit={(hit?h.collider.name:"none")} hitY={(hit?h.point.y:0):F4}");
            }
            bool stable=low>-.1f&&player.transform.position.y<.3f;
            input.ReadDevices=false;
            foreach(float dt in new[]{1f/30,1f/60,1f/144,.1f,.333f,1f})
            {
                for(int i=0;i<15;i++){player.Move(Vector3.zero,dt);yield return null;stable&=player.transform.position.y>-.1f;}
                notes.Add("Idle dt="+dt+" y="+player.transform.position.y+" grounded="+controller.isGrounded);
            }
            // Cross the original entrance in both directions, including a long-frame spike.
            foreach(float dt in new[]{1f/30,1f/144,.1f,.333f})
            {
                foreach(float target in new[]{5.5f,7.1f})
                {
                    for(int i=0;i<120&&Mathf.Abs(target-player.transform.position.x)>.09f;i++)
                    {player.Move(new Vector3(Mathf.Clamp((target-player.transform.position.x)*3,-1,1),0,0),dt);yield return null;stable&=player.transform.position.y>-.1f;}
                    notes.Add("Door crossing dt="+dt+" pos="+player.transform.position.ToString("F4"));
                }
            }
            var tag=root.GetComponentInChildren<NpcRequestNameTag>();bool correctText=tag&&tag.DisplayText=="好想吃布丁...";notes.Add("Requested tag text="+(tag?tag.DisplayText:"missing"));
            ReviewStartupValidation.Finish(stable&&correctText,"Minimum actor y="+low+" real-input frames="+frames+"\n"+string.Join("\n",notes));
        }
    }
}
