#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FireRescue
{
    // Explicit opt-in diagnostic for a headless Windows player. Never runs in normal play.
    public sealed class PrototypeBuildProbe:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(!Environment.GetCommandLineArgs().Contains("--verify-prototype"))return;
            new GameObject("Temporary packaged-build verification").AddComponent<PrototypeBuildProbe>();
        }
        IEnumerator Start()
        {
            yield return null;yield return null;
            int code=0;string report;
            try
            {
                Need(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null,"Palette verification requires a graphics device. Use -batchmode without -nographics; Null-device material colors are not reliable evidence.");
                var scene=SceneManager.GetActiveScene();
                bool firstRescue=scene.path=="Assets/FireRescue/Scenes/FirstRescue1026.unity";
                var layout=GameObject.Find(firstRescue?"First Rescue 1026":"Step4 User Layout");
                Need((firstRescue||scene.path=="Assets/FireRescue/Scenes/TutorialGreybox.unity") && layout,"Wrong packaged startup scene: "+scene.path);
                var input=layout.GetComponentInChildren<LocalPlayerInput>();input.ReadDevices=false;
                Need(input.GetComponent<HoseConnection>().MaximumLength==5,"Packaged hose is not 5m.");
                var npc=layout.GetComponentInChildren<RescueNpc>();
                var model=npc.transform.Find("Visual/Swap Resident Role1");Need(model,"Missing packaged resident model.");
                var colors=model.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.color).Distinct().Count();
                Need(colors>=5,"Packaged resident still has whitebox-only materials.");
                var animator=model.GetComponentInChildren<Animator>();Need(animator && animator.avatar && animator.avatar.isValid && !animator.applyRootMotion,"Packaged resident rig is invalid.");
                if(firstRescue)
                {
                    var sources=layout.GetComponentsInChildren<FluidSource>();
                    Need(sources.Length==1&&sources[0].Element==ElementKind.Water&&sources[0].SpraySettings.PhysicalBallistics,"First-rescue toilet water profile missing.");
                    Need(layout.GetComponentInChildren<FirstRescueEffects>()&&layout.GetComponentInChildren<FireField>().BurningCount>0,"First-rescue effects or fire state missing.");
                    Need(layout.transform.Find("User House 1026")&&!layout.GetComponentInChildren<FireFieldView>(),"Wrong house / legacy grid view in player.");
                    Need(!input.GetComponent<HoseConnection>().IsConnected&&!npc.IsRescued&&layout.GetComponentInChildren<RescueGameManager>().RescuedCount==0,"Packaged rescue does not start clean.");
                }
                else
                {
                    var room=layout.transform.Find("Residential Household Greybox/Living Room");
                    var left=room.Find("Left armchair");var right=room.Find("Right armchair");
                    Need(left && right && Quaternion.Angle(left.localRotation,Quaternion.Euler(0,-90,0))<.01f && Quaternion.Angle(right.localRotation,Quaternion.Euler(0,90,0))<.01f,"Packaged armchairs do not face inward.");
                }
                report="PACKAGED_BUILD_PASS\nScene="+scene.path+"\nHose=5m\nResidentColors="+colors+"\nGraphics="+SystemInfo.graphicsDeviceType+"\nGeneric rig / "+(firstRescue?"new user house + toilet ballistic water":"inward armchairs")+" PASS\nDataPath="+Application.dataPath+"\nNo UI or device input exercised.\n";
            }
            catch(Exception e){code=1;report="PACKAGED_BUILD_FAIL\n"+e;}
            Debug.Log(report);
            // Fixed diagnostic output lives inside this local build, never in arbitrary user paths.
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"packaged-verification.txt"),report);
            Application.Quit(code);
        }
        static void Need(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}
#endif
