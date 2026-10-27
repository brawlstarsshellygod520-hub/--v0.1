using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace FireRescue
{
    // Opt-in build diagnostic: retains the real LocalPlayerInput.Update path, no
    // fabricated movement commands or Physics.SyncTransforms to mask startup faults.
    public sealed class ReviewStartupProbe:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("--verify-startup"))new GameObject("Temporary startup probe").AddComponent<ReviewStartupProbe>();}
        IEnumerator Start()
        {
            Application.runInBackground=true;
            var player=FindAnyObjectByType<FirefighterMotor>();var input=player.GetComponent<LocalPlayerInput>();var cc=player.GetComponent<CharacterController>();
            var floor=GameObject.Find("First Review 1027/Ground").GetComponent<BoxCollider>();var notes=new List<string>();
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","StartupProbe"));Directory.CreateDirectory(folder);
            notes.Add("ReadDevices="+input.ReadDevices+" focused="+Application.isFocused+" collider="+floor.enabled+" trigger="+floor.isTrigger+" bounds="+floor.bounds+" playerLayer="+player.gameObject.layer+" groundLayer="+floor.gameObject.layer+" ignored="+Physics.GetIgnoreLayerCollision(player.gameObject.layer,floor.gameObject.layer));
            float low=player.transform.position.y,next=0,end=Time.realtimeSinceStartup+6;int frames=0,grounded=0;
            while(Time.realtimeSinceStartup<end)
            {
                yield return null;frames++;low=Mathf.Min(low,player.transform.position.y);if(cc.isGrounded)grounded++;
                if(Time.realtimeSinceStartup<next)continue;next=Time.realtimeSinceStartup+.5f;
                bool hit=Physics.Raycast(player.transform.position+Vector3.up*4,Vector3.down,out var h,15,(1<<0)|(1<<8),QueryTriggerInteraction.Ignore);
                notes.Add($"frame={frames} dt={Time.deltaTime:F5} position={player.transform.position:F4} grounded={cc.isGrounded} detect={cc.detectCollisions} floor={(hit?h.collider.name:"none")} floorY={(hit?h.point.y:0):F4}");
            }
            bool pass=low>-.1f&&cc.isGrounded&&input.ReadDevices&&grounded>10;
            File.WriteAllText(folder+"/startup.txt",(pass?"PLAYER_STARTUP_PASS\n":"PLAYER_STARTUP_FAIL\n")+"minimumY="+low+" frames="+frames+" groundedFrames="+grounded+"\n"+string.Join("\n",notes));
            Application.Quit(pass?0:1);
        }
    }
}
