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
    public static class ResidentialIntegrationValidation
    {
        const string Key = "FireRescue.ResidentialIntegrationTest";
        static bool finished;
        static ResidentialIntegrationValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Key, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    finished = false;
                    Application.logMessageReceived += OnLog;
                    new GameObject("Temporary integration validator").AddComponent<ResidentialIntegrationDriver>();
                }
                else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key + "Replay", false))
                    EditorApplication.delayCall += () => EditorApplication.EnterPlaymode();
            };
        }
        public static bool Replay => SessionState.GetBool(Key + "Replay", false);
        public static float ExpectedHoseLength => SessionState.GetFloat(Key + "ExpectedHoseLength", 3f);
        public static string Evidence => !string.IsNullOrEmpty(SessionState.GetString(Key + "EvidenceOverride", "")) ? SessionState.GetString(Key + "EvidenceOverride", "") :
            SessionState.GetBool(Key + "Characters", false) ? UserCharacterSetup.Evidence :
            SessionState.GetBool(Key + "Effects", false) ? SprayEffectTuningBuilder.Evidence :
            SessionState.GetBool(Key + "Directional", false) ? DirectionalSprayBuilder.Evidence : ResidentialIntegrationBuilder.Evidence;
        static string Report => Evidence + (SessionState.GetBool(Key + "Directional", false) ? "/residential-loop.txt" : "/playmode.txt");
        public static void BatchRun()
        { SessionState.SetBool(Key + "Characters", false); SessionState.SetBool(Key + "Effects", false); SessionState.SetBool(Key + "Directional", false); Begin(); }
        public static void BatchRunDirectional()
        { SessionState.SetBool(Key + "Characters", false); SessionState.SetBool(Key + "Effects", false); SessionState.SetBool(Key + "Directional", true); Begin(); }
        public static void BatchRunEffectTuning()
        { SessionState.SetBool(Key + "Characters", false); SessionState.SetBool(Key + "Effects", true); SessionState.SetBool(Key + "Directional", true); Begin(); }
        public static void BatchRunUserCharacters()
        { SessionState.SetBool(Key + "Characters", true); SessionState.SetBool(Key + "Effects", true); SessionState.SetBool(Key + "Directional", true); Begin(); }
        public static void BatchRunPlayerWhiteboxCamera()
        { SessionState.SetBool(Key + "Characters", false); SessionState.SetBool(Key + "Effects", false); SessionState.SetBool(Key + "Directional", true); Begin(PlayerWhiteboxSetup.Evidence); }
        public static void BatchRunHoseFluidPhysics()
        { SessionState.SetBool(Key + "Characters", false); SessionState.SetBool(Key + "Effects", false); SessionState.SetBool(Key + "Directional", true); Begin(HoseFluidPhysicsBuilder.Evidence); }
        public static void BatchRunCharacterSwap()
        { SessionState.SetBool(Key + "Characters", false); SessionState.SetBool(Key + "Effects", false); SessionState.SetBool(Key + "Directional", true); Begin(CharacterSwapSetup.Evidence); }
        public static void BatchRunHousehold()
        { SessionState.SetBool(Key + "Characters", false); SessionState.SetBool(Key + "Effects", false); SessionState.SetBool(Key + "Directional", true); Begin(HouseholdLayoutSetup.Evidence); }
        public static void BatchRunResident5m()
        { SessionState.SetBool(Key + "Characters", false); SessionState.SetBool(Key + "Effects", false); SessionState.SetBool(Key + "Directional", true); Begin(ResidentialTuningSetup.Evidence,ResidentialTuningSetup.HoseLength); }
        public static void BatchRunCurrentDelivery()
        { SessionState.SetBool(Key + "Characters", false); SessionState.SetBool(Key + "Effects", false); SessionState.SetBool(Key + "Directional", true); Begin(PrototypeDeliverySetup.Evidence,5f); }
        static void Begin(string evidenceOverride = "", float expectedHoseLength = 3f)
        {
            SessionState.SetFloat(Key + "ExpectedHoseLength",expectedHoseLength);
            SessionState.SetString(Key + "EvidenceOverride", evidenceOverride);
            EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            SessionState.SetBool(Key, true); SessionState.SetBool(Key + "Replay", false);
            EditorApplication.EnterPlaymode();
        }
        static void OnLog(string text, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false, text + "\n" + stack); }
        public static void Finish(bool success, string details)
        {
            if (finished) return; finished = true;
            Application.logMessageReceived -= OnLog;
            Directory.CreateDirectory(Evidence);
            if (success && !Replay)
            {
                File.WriteAllText(Report, "RESIDENTIAL_LOOP_PASS\n" + details);
                SessionState.SetBool(Key + "Replay", true);
                EditorApplication.delayCall += () => EditorApplication.ExitPlaymode();
                return;
            }
            File.AppendAllText(Report, "\n" + (success ? "REPLAY_PASS\n" : "FAIL\n") + details);
            SessionState.SetBool(Key, false);
            EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
        }
    }

    public sealed class ResidentialIntegrationDriver : MonoBehaviour
    {
        const float Dt = 1f / 60f;
        readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
        readonly List<string> results = new List<string>();
        LocalPlayerInput input;
        HoseConnection hose;
        SprayEmitter emitter;
        Camera camera;
        float started;
        void Start() { Time.captureDeltaTime = Dt; started = Time.realtimeSinceStartup; stack.Push(Scenario()); }
        void OnDestroy() { Time.captureDeltaTime = 0; }
        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 240) throw new TimeoutException("Integration timeout");
                while (stack.Count > 0)
                {
                    var routine = stack.Peek();
                    if (!routine.MoveNext()) { stack.Pop(); continue; }
                    if (routine.Current is IEnumerator nested) { stack.Push(nested); continue; }
                    return;
                }
                enabled = false; ResidentialIntegrationValidation.Finish(true, string.Join("\n", results));
            }
            catch (Exception error)
            {
                enabled = false; Capture("failure");
                ResidentialIntegrationValidation.Finish(false, string.Join("\n", results) + "\n" + error);
            }
        }
        IEnumerator Scenario()
        {
            yield return null;
            var layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
            input = layout.GetComponentInChildren<LocalPlayerInput>(); input.ReadDevices = false; input.SetInputFocus(true);
            hose = input.GetComponent<HoseConnection>(); emitter = input.GetComponent<SprayEmitter>(); camera = Camera.main;
            var tools = input.GetComponent<FirefighterTools>();
            var door = layout.GetComponentInChildren<BreakableDoor>();
            var npc = layout.GetComponentInChildren<RescueNpc>(true);
            var field = layout.GetComponentInChildren<FireField>();
            var manager = layout.GetComponentInChildren<RescueGameManager>();
            var window = layout.GetComponentInChildren<HoseWindow>();
            var objectFires = layout.GetComponentsInChildren<ObjectFire>();
            var target = objectFires.Single(f => f.Kind == FireKind.Ordinary);
            var pan = objectFires.Single(f => f.Kind == FireKind.CookingOil);
            int completed = 0; manager.RescueCompleted += () => completed++;
            Check(door.RemainingDurability == 3 && !door.IsBroken && door.BlockingCollider.enabled, "Fresh door starts closed with 3 durability");
            Check(!npc.IsRescued && !npc.HasSafePath && manager.RescuedCount == 0 && !manager.RescueComplete, "NPC initially waits; rescue state starts clean");
            Check(field.BurningCount >= 5 && objectFires.All(f => f.IsBurning) && !manager.AllFiresOut, "Both floor and two authored object fires start burning");
            float expectedLength=ResidentialIntegrationValidation.ExpectedHoseLength;
            Check(hose.MaximumLength == expectedLength && !hose.IsConnected && Planar(input.transform.position, new Vector3(5.74f,0,-7.5f)) < .1f, "Spawn and disconnected "+expectedLength+"m hose start clean");
            if (ResidentialIntegrationValidation.Replay) yield break;
            Capture("01-start");
            yield return Walk(new Vector3(5.74f,0,-4.8f));
            yield return Walk(new Vector3(.2f,0,-4.6f));
            yield return Walk(new Vector3(.0f,0,2.4f));
            var aim = door.transform.position + Vector3.up * .8f;
            for (int i=0;i<20;i++) { Pointer(aim); yield return null; }
            Pointer(aim, axePressed:true, axeHeld:true); yield return null;
            Check(door.RemainingDurability == 3 && !tools.IsSwinging, "First LMB only equips axe");
            Pointer(aim); yield return null;
            for (int hit=1;hit<=3;hit++)
            {
                Pointer(aim, axePressed:true, axeHeld:true); yield return null;
                for(int i=0;i<38;i++) { Pointer(aim); yield return null; }
                Check(door.RemainingDurability == 3-hit, "One clicked stroke produces exactly one door hit: " + hit);
            }
            Check(door.IsBroken && !door.BlockingCollider.enabled && door.gameObject.activeInHierarchy, "Broken door opens only gameplay collision, root stays alive");
            Check(!npc.HasSafePath && !npc.IsRescued, "Door open alone is insufficient while escape route still burns");
            yield return Walk(new Vector3(-2.3f,0,2.4f));
            Check(input.transform.position.x < door.transform.position.x, "Player physically crossed the broken doorway");
            aim = new Vector3(-4.25f,.04f,2.25f);
            Pointer(aim, hosePressed:true, hoseHeld:true); yield return null;
            Pointer(aim); yield return null;
            Pointer(aim, hosePressed:true, hoseHeld:true); yield return null;
            Check(hose.Source && hose.Source.Element == ElementKind.Water && !emitter.IsSpraying, "Separate RMB equip then connect water without auto-spray");
            yield return Walk(new Vector3(-2.4f,0,2f));
            // Walk around the solid toilet body before aiming at the northern fire cells.
            yield return Walk(new Vector3(-4.9f,0,2f));
            yield return ClearFloor(field, p=>p.x< -1.5f);
            for(int i=0;i<20;i++) yield return null;
            Check(npc.HasSafePath || npc.IsRescued, "NPC discovers the opened, extinguished route without E or calming");
            yield return Walk(new Vector3(-4.8f,0,2.1f));
            yield return Walk(new Vector3(-5.1f,0,2.8f));
            Pointer(target.transform.position, interact:true); yield return null;
            Check(hose.ActiveWindow == window && hose.UsedLength <= expectedLength+.001f, "F routes through closer window instead of disconnecting nearby water; folded length " + hose.UsedLength.ToString("F2") + "m");
            Vector3 limit = hose.ConstrainPosition(new Vector3(-9,0,3.6f));
            Check(Planar(hose.Source.transform.position,window.RoutePosition)+Planar(window.RoutePosition,limit)<=expectedLength+.001f, "Routed movement retains exact "+expectedLength+"m cap");
            yield return Spray(target.transform.position+Vector3.up*.55f, 95);
            Check(!target.IsBurning && hose.ActiveWindow == window, "Real spray extinguishes outdoor target through north window while routed" +
                " intensity=" + target.Intensity + " lastImpact=" + emitter.ImpactPoint.ToString("F3") +
                " nozzle=" + emitter.NozzlePosition.ToString("F3") + " localImpact=" + window.transform.InverseTransformPoint(emitter.ImpactPoint).ToString("F3"));
            Capture("02-window-water");
            Pointer(target.transform.position,interact:true); yield return null;
            Check(!hose.ActiveWindow && hose.IsConnected, "Second F releases window but keeps water connected");
            yield return Walk(new Vector3(-4.8f,0,2f));
            yield return Walk(new Vector3(-2.3f,0,2f));
            yield return Walk(new Vector3(-.55f,0,2.4f));
            yield return ClearFloor(field,p=>p.x>=-1.5f);
            Check(field.BurningCount == 0 && !manager.AllFiresOut && pan.IsBurning, "Floor clear still does not hide burning oil pan from manager");
            yield return Spray(pan.transform.position+Vector3.up*1.1f, 45);
            Check(pan.Intensity == pan.InitialIntensity, "Water remains ineffective against cooking-oil object fire");
            yield return Walk(new Vector3(-2.3f,0,2f)); // At the current source; F near oil would intentionally switch source.
            Pointer(pan.transform.position,interact:true); yield return null; // F at/away from source disconnects.
            Check(!hose.IsConnected, "F disconnects water for free movement");
            yield return Walk(new Vector3(2.6f,0,2.5f));
            yield return Walk(new Vector3(3.4f,0,3.4f));
            Pointer(pan.transform.position,hosePressed:true,hoseHeld:true); yield return null;
            Check(hose.Source && hose.Source.Element == ElementKind.DryPowder, "RMB connects nearby domestic dry-powder source");
            yield return Walk(new Vector3(2.7f,0,2.5f));
            yield return Spray(pan.transform.position+Vector3.up*1.1f, 95);
            Check(!pan.IsBurning && manager.AllFiresOut, "Actual powder spray extinguishes oil pan; manager reports all fires out");
            for(int i=0;i<1500 && !npc.IsRescued;i++) yield return null;
            Check(npc.IsRescued && manager.RescueComplete && manager.RescuedCount==1 && completed==1, "Resident physically exits via broken door; rescue completion exactly once");
            for(int i=0;i<30;i++) yield return null;
            Check(manager.LevelComplete && completed==1, "Whole-level success stays stable without repeated completion events");
            Capture("03-completed");
        }
        IEnumerator ClearFloor(FireField field, Predicate<Vector3> region)
        {
            for(int pass=0;pass<4;pass++)
            {
                var points = new List<Vector3>();
                for(int z=0;z<field.Height;z++) for(int x=0;x<field.Width;x++)
                    if(field.GetCell(x,z).IsBurning && region(field.CellCenter(x,z))) points.Add(field.CellCenter(x,z));
                if(points.Count==0) yield break;
                foreach(var p in points) yield return Spray(p+Vector3.up*.04f,70);
            }
            throw new Exception("Floor spray could not clear region; player="+input.transform.position+" impact="+emitter.ImpactPoint);
        }
        IEnumerator Spray(Vector3 aim,int frames)
        {
            Pointer(aim); yield return null;
            // Directional spread deposits finite packets at several distances instead of an instant point hit.
            if (emitter.UsesDirectionalSpray) frames *= 3;
            for(int i=0;i<frames;i++)
            {
                Pointer(aim,hosePressed:i==0,hoseHeld:true); yield return null;
                if (emitter.UsesDirectionalSpray && i == frames / 2)
                    Capture("spraying-" + hose.Source.Element);
            }
            Pointer(aim); yield return null;
            if (emitter.UsesDirectionalSpray) for(int i=0;i<70;i++) yield return null; // Already emitted fluid finishes flying.
        }
        IEnumerator Walk(Vector3 goal)
        {
            for(int i=0;i<600 && Planar(input.transform.position,goal)>.12f;i++)
            {
                Vector3 d=(goal-input.transform.position); d.y=0; d.Normalize();
                Vector3 f=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;
                Pointer(goal,new Vector2(Vector3.Dot(d,Vector3.Cross(Vector3.up,f)),Vector3.Dot(d,f)));
                yield return null;
            }
            Check(Planar(input.transform.position,goal)<=.12f,"Walk reached "+goal+" actual="+input.transform.position.ToString("F2"));
        }
        void Pointer(Vector3 aim,Vector2 movement=default,bool interact=false,bool axePressed=false,bool axeHeld=false,bool hosePressed=false,bool hoseHeld=false)
        { input.TickPointerCommand(movement,aim,interact,false,axePressed,axeHeld,hosePressed,hoseHeld,Dt); }
        static float Planar(Vector3 a,Vector3 b) { a.y=b.y=0; return Vector3.Distance(a,b); }
        void Check(bool ok,string text) { if(!ok) throw new Exception(text); results.Add("PASS "+text); }
        void Capture(string name)
        {
            if(!camera || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            Directory.CreateDirectory(ResidentialIntegrationValidation.Evidence);
            var rt=RenderTexture.GetTemporary(1280,720,24); var old=camera.targetTexture; var previous=RenderTexture.active;
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try { camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt; texture.ReadPixels(new Rect(0,0,1280,720),0,0); texture.Apply(); File.WriteAllBytes(ResidentialIntegrationValidation.Evidence+"/"+name+".png",texture.EncodeToPNG()); }
            finally { camera.targetTexture=old; RenderTexture.active=previous; RenderTexture.ReleaseTemporary(rt); Destroy(texture); }
        }
    }
}
