using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FireRescue
{
    // Explicit command-line validation only; never created in a normal exhibition session.
    public sealed class ReviewPlayerSmoke:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {if(Environment.GetCommandLineArgs().Contains("--verify-review"))new GameObject("Temporary review player verification").AddComponent<ReviewPlayerSmoke>();}
        string folder;bool failed;
        void OnError(string text,string stack,LogType type)
        {if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;failed=true;File.AppendAllText(folder+"/player-smoke.txt","ERROR "+text+"\n"+stack+"\n");}
        void Need(bool condition,string text)
        {if(!condition)failed=true;File.AppendAllText(folder+"/player-smoke.txt",(condition?"PASS ":"FAIL ")+text+"\n");}
        IEnumerator Start()
        {
            folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","ReviewSmoke"));Directory.CreateDirectory(folder);File.WriteAllText(folder+"/player-smoke.txt","Windows D3D11 runtime verification\n");
            Application.logMessageReceived+=OnError;Application.runInBackground=true;
            yield return null;yield return null;
            Need(SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Null,"Real graphics device: "+SystemInfo.graphicsDeviceType);
            Need(SceneManager.GetActiveScene().name=="FirstReview1027","Latest starts the requested review scene");
            var input=FindAnyObjectByType<LocalPlayerInput>();Need(input,"Local input exists");if(!input){Application.Quit(1);yield break;}input.ReadDevices=false;
            var hose=input.GetComponent<HoseConnection>();var motion=input.GetComponent<FirefighterCartoonMotion>();var state=FindAnyObjectByType<FirstReviewMission>();
            Need(hose&&hose.MaximumLength==10&&hose.AutomaticWallWrapping,"10m automatic wrap is in packaged scene");
            Need(input.GetComponent<PhysicalHoseTube>()&&!input.GetComponentInChildren<HoseView>(true).enabled,"Continuous 3D physical tube replaces old line presentation");
            Need(Application.productName=="炎上消防隊","Updated game product title");
            Need(Application.version=="0.2.1","v0.2.1 is the packaged application version");
            Need(!FindAnyObjectByType<ReviewMissionHud>().IsRestartHintVisible&&ReviewMissionHud.RestartHint=="按 R 即可重來","Small R hint is hidden during active gameplay");
            Need(motion&&state&&FindAnyObjectByType<ReviewMissionHud>(),"New gait, request state and HUD are included");
            Need(input.transform.position.x<5&&input.transform.position.z>-5.5f,"Packaged player spawns inside living room");
            Need(FindAnyObjectByType<CookingOilIncident>()&&input.GetComponent<PlayerFireContact>(),"Independent floor/oil reactions and player fire contact included");
            Need(input.GetComponent<PickupContextView>(),"Contextual pickup outline/E and hose-limit warning presenter packaged");
            var boundary=GameObject.Find("First Review 1027/Prototype Courtyard Boundary");
            Need(boundary&&boundary.GetComponentsInChildren<Collider>().Length==3,"Authorized courtyard walls block the outdoor shortcut");
            Need(boundary&&boundary.GetComponentsInChildren<Renderer>(true).All(r=>!r.enabled)&&boundary.GetComponentsInChildren<Collider>().All(c=>c.enabled),"Invisible courtyard barriers retain physical collision");
            Need(FindObjectsByType<FluidSource>().Single(s=>s.Element==ElementKind.Water).Profile.Settings.MinimumSpeedFraction==.14f,"Near-range water droplets included without cursor-distance targeting");
            var water=FindObjectsByType<FluidSource>().Single(s=>s.Element==ElementKind.Water).Profile.Settings;
            Need(water.MuzzleSpeed==10&&water.PacketsPerSecond==60&&water.MainStreamSpeedFraction==.7f&&FindAnyObjectByType<FirstRescueEffects>().WaterVisualScale==1.65f,"Longer, denser water configuration packaged");
            Need(PickupContextView.LimitMessage.Contains("按 E"),"Length warning uses unified E action, not F");
            Need(FindObjectsByType<FluidSource>().Length==2&&FindAnyObjectByType<ObjectFire>().IsBurning,"Two sources and burning oil pan loaded");
            foreach(var pair in new[]{("Toilet Water Source","馬桶本體"),("Soda Powder Source","立方體.002"),("Oil Pan Fire","油鍋"),("Pudding Item","布丁")})
            {
                var visual=GameObject.Find("First Review 1027/"+pair.Item1+"/Visual");
                Need(visual&&visual.GetComponentsInChildren<MeshFilter>().Any(m=>m.sharedMesh&&m.sharedMesh.name==pair.Item2),"User-authored model packaged: "+pair.Item1);
            }
            Need(FindObjectsByType<SkinnedMeshRenderer>().All(s=>s.sharedMesh&&s.bones.All(b=>b)),"All packaged character skins/bones resolve");
            Need(FindAnyObjectByType<NpcRequestNameTag>(),"Resident overhead request tag loaded");
            Need(FindAnyObjectByType<NpcRequestNameTag>().DisplayText=="好想吃布丁...","Requested pudding thought text included in build");
            var plaster=GameObject.Find("First Review 1027/User House 1027/wall/Visual").GetComponent<Renderer>().sharedMaterial;
            Need(plaster.shader.name=="FireRescue/MattePlaster"&&plaster.shader.isSupported,"Diffuse-only plaster shader compiled for Windows");
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(folder+"/player-view.png");
            float end=Time.realtimeSinceStartup+5.2f;while(Time.realtimeSinceStartup<end)yield return null;
            Need(File.Exists(folder+"/player-view.png"),"Screenshot file written (visual content requires separate review)");
            // Hidden/minimized Windows swapchains may output a fully black image. File
            // existence alone is never proof that UI or visual quality was verified.
            var image=new Texture2D(2,2);bool nonblack=false;
            if(File.Exists(folder+"/player-view.png")&&image.LoadImage(File.ReadAllBytes(folder+"/player-view.png")))
                foreach(var pixel in image.GetPixels32())if(pixel.r>12||pixel.g>12||pixel.b>12){nonblack=true;break;}
            Destroy(image);File.AppendAllText(folder+"/player-smoke.txt",nonblack?"NOTE screenshot has image content; manual visual review still required\n":"NOTE black hidden-window capture; this run does NOT validate visuals/UI\n");
            Need(state&&state.Remaining<60&&state.Remaining>50,"One-minute countdown advances in actual player");
            var floor=FindAnyObjectByType<FireField>();var incident=FindAnyObjectByType<CookingOilIncident>();
            Need(floor&&incident&&floor.BurningCount==incident.FirstWaveTargetCount&&floor.BurningCount>100,"Connected first wave fills the kitchen in packaged runtime");
            var effects=FindAnyObjectByType<FirstRescueEffects>();Need(effects&&effects.ActiveFlameCount<floor.BurningCount*.75f&&effects.ActiveHeatMarkCount==floor.BurningCount,"Spacious fire retains readable danger footprints in Windows package");
            var food=FindAnyObjectByType<CarryItem>();Need(food&&!food.IsSafeToTake&&!food.IsBurnt,"Ordinary floor fire gates pudding; no spontaneous oil explosion in packaged player");
            File.AppendAllText(folder+"/player-smoke.txt",failed?"REVIEW_PLAYER_FAIL\n":"REVIEW_PLAYER_PASS\n");
            Application.logMessageReceived-=OnError;Application.Quit(failed?1:0);
        }
    }
}
