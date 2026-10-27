using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    /// <summary>Batch probes for the directional spray's delivered fluid, rather than its visual packet count.</summary>
    [InitializeOnLoad]
    public static class FluidPhysicsValidation
    {
        const string SessionKey = "FireRescue.FluidPhysicsValidation.Mode";
        const string BaselinePath = "Docs/Validation/2026-10-27-hose-fluid-physics/fluid-baseline.txt";
        const string ResultPath = "Docs/Validation/2026-10-27-hose-fluid-physics/fluid-playmode.txt";
        static bool finished;
        static double readyAt;

        static FluidPhysicsValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                string mode = SessionState.GetString(SessionKey, string.Empty);
                if (state != PlayModeStateChange.EnteredPlayMode || string.IsNullOrEmpty(mode)) return;
                finished = false; Application.logMessageReceived += OnLog;
                new GameObject("Temporary Fluid Physics Validation").AddComponent<FluidPhysicsValidationDriver>().Configure(mode == "baseline");
            };
        }

        [MenuItem("FireRescue/Probe Fluid Physics Baseline")]
        public static void ProbeBaseline() => Begin("baseline");
        [MenuItem("FireRescue/Validate Fluid Physics")]
        public static void Run() => Begin("validate");
        public static void BatchProbeBaseline() { if (!Application.isBatchMode) throw new InvalidOperationException("Batch-only entry point."); Begin("baseline"); }
        public static void BatchRun() { if (!Application.isBatchMode) throw new InvalidOperationException("Batch-only entry point."); Begin("validate"); }

        static void Begin(string mode)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetString(SessionKey, mode); readyAt = EditorApplication.timeSinceStartup + 2d;
            EditorApplication.update -= Enter; EditorApplication.update += Enter;
        }

        static void Enter()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < readyAt) return;
            EditorApplication.update -= Enter;
            try { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); EditorApplication.EnterPlaymode(); }
            catch (Exception error) { Finish(false, "Setup exception:\n" + error); }
        }

        static void OnLog(string text, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Finish(false, "Console " + type + ": " + text + "\n" + stack);
        }

        public static void Finish(bool success, string details)
        {
            if (finished) return;
            finished = true; string mode = SessionState.GetString(SessionKey, string.Empty);
            SessionState.EraseString(SessionKey); Application.logMessageReceived -= OnLog;
            string path = mode == "baseline" ? BaselinePath : ResultPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string report = (success ? "FLUID_PHYSICS_" + mode.ToUpperInvariant() + "_PASS" : "FLUID_PHYSICS_" + mode.ToUpperInvariant() + "_FAIL") + "\n" + details;
            File.WriteAllText(path, report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () => { if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1); else EditorApplication.ExitPlaymode(); };
        }
    }

    public sealed class FluidPhysicsValidationDriver : MonoBehaviour
    {
        readonly List<GameObject> fixtures = new List<GameObject>();
        readonly List<ScriptableObject> profiles = new List<ScriptableObject>();
        readonly List<string> results = new List<string>();
        bool baseline;
        int checks;
        int fixtureIndex;

        public void Configure(bool baselineMode) => baseline = baselineMode;
        void Start()
        {
            try
            {
                if (baseline) ProbeCurrentDelivery();
                else ValidateFixedContract();
                FluidPhysicsValidation.Finish(true, "Checks=" + checks + "\n" + string.Join("\n", results));
            }
            catch (Exception error) { FluidPhysicsValidation.Finish(false, "Checks before failure=" + checks + "\n" + string.Join("\n", results) + "\n" + error); }
            finally { Cleanup(); }
        }

        void ProbeCurrentDelivery()
        {
            Delivery tap = Deliver("baseline tap", new[] { .01f });
            Delivery held30 = Deliver("baseline 50ms 30Hz", new[] { 1f / 30f, 1f / 60f });
            Delivery held60 = Deliver("baseline 50ms 60Hz", new[] { 1f / 60f, 1f / 60f, 1f / 60f });
            Delivery taps = DeliverTaps("baseline five 10ms taps", 5, .01f);
            Delivery hitch = Deliver("baseline hitch", new[] { .5f });
            Note("Tap .01s delivered=" + tap.Amount + " expectedFlow=" + (.01f * 3f) + " impacts=" + tap.Hits);
            Note("50ms delivered: 30Hz=" + held30.Amount + ", 60Hz=" + held60.Amount + ", expectedFlow=.15");
            Note("Five separated 10ms taps delivered=" + taps.Amount + " expectedFlow=.15");
            Note("0.5s one Tick delivered=" + hitch.Amount + " expectedFlow=1.5 impacts=" + hitch.Hits);
            ProbeSourceSwap(); ProbeCapacity(); ProbeNearestCollider();
        }

        void ValidateFixedContract()
        {
            Delivery tap = Deliver("tap", new[] { .01f });
            Check(Approximately(tap.Amount, .03f), "A .01s tap delivers actual FlowRate*dt, not a whole free packet.");
            Delivery held30 = Deliver("50ms 30Hz", new[] { 1f / 30f, 1f / 60f });
            Delivery held60 = Deliver("50ms 60Hz", new[] { 1f / 60f, 1f / 60f, 1f / 60f });
            Delivery held120 = Deliver("50ms 120Hz", new[] { 1f / 120f, 1f / 120f, 1f / 120f, 1f / 120f, 1f / 120f, 1f / 120f });
            Check(Approximately(held30.Amount, .15f) && Approximately(held60.Amount, .15f) && Approximately(held120.Amount, .15f), "Equal held time delivers equal receiver amount at 30/60/120Hz.");
            Delivery taps = DeliverTaps("five taps", 5, .01f);
            Check(Approximately(taps.Amount, .15f), "Separated short taps deliver the same total fluid as their combined held duration.");
            Delivery hitch = Deliver("hitch", new[] { .5f });
            Delivery regular = Deliver("regular", Repeat(1f / 30f, 15));
            Check(Approximately(hitch.Amount, 1.5f) && Approximately(regular.Amount, 1.5f), "A .5s hitch preserves the same delivered flow as 30Hz ticks.");
            ProbeSourceSwap(true); ProbeCapacity(true); ProbeNearestCollider(true);
            VerifyStopDoesNotDuplicateFlush(); VerifyHugeFiniteDt();
            VerifyEffectSnapshot(); VerifyInvalidFlow();
        }

        Delivery Deliver(string label, float[] ticks)
        {
            Fixture f = CreateFixture(label, ElementKind.Water, 3f); Vector3 aim = f.Player.transform.position + Vector3.forward * 8f;
            Physics.SyncTransforms();
            foreach (float dt in ticks) f.Emitter.Tick(aim, true, dt);
            f.Emitter.Stop(); Physics.SyncTransforms(); f.Emitter.Advance(2f);
            Delivery value = new Delivery { Amount = f.Receiver.Amount, Hits = f.Receiver.Hits };
            DestroyFixture(f); return value;
        }

        Delivery DeliverTaps(string label, int count, float dt)
        {
            Fixture f = CreateFixture(label, ElementKind.Water, 3f); Vector3 aim = f.Player.transform.position + Vector3.forward * 8f;
            Physics.SyncTransforms();
            for (int i = 0; i < count; i++) { f.Emitter.Tick(aim, true, dt); f.Emitter.Stop(); }
            Physics.SyncTransforms(); f.Emitter.Advance(2f);
            Delivery value = new Delivery { Amount = f.Receiver.Amount, Hits = f.Receiver.Hits };
            DestroyFixture(f); return value;
        }

        void VerifyStopDoesNotDuplicateFlush()
        {
            Fixture f = CreateFixture("stop once", ElementKind.Water, 3f); Vector3 aim = f.Player.transform.position + Vector3.forward * 8f;
            Physics.SyncTransforms(); f.Emitter.Tick(aim, true, .01f); f.Emitter.Stop(); f.Emitter.Stop(); Physics.SyncTransforms(); f.Emitter.Advance(2f);
            Check(Approximately(f.Receiver.Amount, .03f), "Repeated Stop cannot duplicate a final fractional packet.");
            DestroyFixture(f);
        }

        void ProbeSourceSwap(bool assertions = false)
        {
            Fixture f = CreateFixture("swap", ElementKind.Water, 3f); Vector3 aim = f.Player.transform.position + Vector3.forward * 8f;
            Physics.SyncTransforms(); f.Emitter.Tick(aim, true, .01f); f.Emitter.Tick(aim, true, .01f); // Leaves a sub-packet water remainder at 40 PPS.
            FluidSource oil = new GameObject("Oil source").AddComponent<FluidSource>(); oil.transform.SetParent(f.Root.transform, false); oil.transform.localPosition = Vector3.zero; oil.Configure(ElementKind.Oil, 3f); oil.SetSprayProfile(CreateProfile());
            DestroyImmediate(f.Source.gameObject); f.Hose.Disconnect(); f.Hose.TryConnect(oil); Physics.SyncTransforms(); f.Emitter.Tick(aim, true, .01f); f.Emitter.Stop(); Physics.SyncTransforms(); f.Emitter.Advance(2f);
            if (assertions) Check(Approximately(f.Receiver.Water, .06f) && Approximately(f.Receiver.Oil, .03f), "Packets and stop-flush keep their captured source element after its old source is destroyed.");
            else Note("Source swap received water=" + f.Receiver.Water + " oil=" + f.Receiver.Oil);
            DestroyFixture(f);
        }

        void ProbeCapacity(bool assertions = false)
        {
            Fixture f = CreateFixture("capacity", ElementKind.Water, 3f); Vector3 aim = f.Player.transform.position + Vector3.forward * 8f;
            Physics.SyncTransforms(); f.Emitter.Tick(aim, true, 10f); f.Emitter.Tick(aim, true, .025f);
            if (assertions) Check(f.Emitter.ActiveParticleCount <= SprayEmitter.ParticleCapacity && f.Emitter.DroppedAmount > 0f,
                "A saturated fixed capacity stays bounded and reports the newly dropped fluid instead of replaying it later.");
            else Note("10s Tick active=" + f.Emitter.ActiveParticleCount + " capacity=" + SprayEmitter.ParticleCapacity + " dropped=" + f.Emitter.DroppedAmount);
            DestroyFixture(f);
        }

        void VerifyEffectSnapshot()
        {
            Fixture f = CreateFixture("effect snapshot", ElementKind.Water, 3f); Vector3 aim = f.Player.transform.position + Vector3.forward * 8f;
            f.Profile.ConfigureEffects(new SprayEffectSettings { ExtinguishMultiplier = 2f, PushMultiplier = 1f, ResidueMultiplier = 1f });
            Physics.SyncTransforms(); f.Emitter.Tick(aim, true, .01f);
            f.Profile.ConfigureEffects(new SprayEffectSettings { ExtinguishMultiplier = .5f, PushMultiplier = 1f, ResidueMultiplier = 1f });
            f.Emitter.Stop(); Physics.SyncTransforms(); f.Emitter.Advance(2f);
            Check(Approximately(f.Receiver.Water, .06f), "An emitted packet retains its captured effects after its profile changes.");
            DestroyFixture(f);
        }

        void VerifyHugeFiniteDt()
        {
            Fixture f = CreateFixture("huge finite dt", ElementKind.Water, 3f); Physics.SyncTransforms();
            f.Emitter.Tick(f.Player.transform.position + Vector3.forward * 8f, true, 1e8f);
            Check(f.Emitter.ActiveParticleCount <= SprayEmitter.ParticleCapacity &&
                !float.IsNaN(f.Emitter.TotalEmittedAmount) && !float.IsInfinity(f.Emitter.TotalEmittedAmount),
                "A huge finite Tick remains bounded without float-to-int overflow or non-finite diagnostics.");
            DestroyFixture(f);
        }

        void VerifyInvalidFlow()
        {
            Fixture f = CreateFixture("invalid flow", ElementKind.Water, 3f); f.Source.Configure(ElementKind.Water, float.NaN);
            Physics.SyncTransforms(); f.Emitter.Tick(f.Player.transform.position + Vector3.forward * 8f, true, .01f); f.Emitter.Advance(2f);
            Check(f.Emitter.TotalEmittedAmount == 0f && f.Receiver.Amount == 0f, "NaN flow cannot create a packet or receiver effect.");
            DestroyFixture(f);
        }

        void ProbeNearestCollider(bool assertions = false)
        {
            Fixture f = CreateFixture("nearest", ElementKind.Water, 3f);
            ProbeReceiver near = Receiver(f.Root.transform, "Near", new Vector3(0f, .75f, 1.2f));
            ProbeReceiver far = Receiver(f.Root.transform, "Far", new Vector3(0f, .75f, 2.2f));
            Physics.SyncTransforms(); f.Emitter.Tick(f.Player.transform.position + Vector3.forward * 8f, true, .05f); f.Emitter.Stop(); Physics.SyncTransforms(); f.Emitter.Advance(2f);
            if (assertions) Check(near.Amount > 0f && far.Amount == 0f, "Nearest collider alone receives each flight impact.");
            else Note("Nearest probe: near=" + near.Amount + " far=" + far.Amount);
            DestroyFixture(f);
        }

        Fixture CreateFixture(string label, ElementKind element, float flow)
        {
            GameObject root = new GameObject("Temporary fluid physics - " + label); root.transform.position = new Vector3(1000f + fixtureIndex++ * 50f, 0f, 0f); fixtures.Add(root);
            GameObject player = new GameObject("Player"); player.transform.SetParent(root.transform, false); player.transform.localPosition = Vector3.zero; player.transform.forward = Vector3.forward;
            GameObject reactionObject = new GameObject("Reaction"); reactionObject.transform.SetParent(root.transform, false); ElementReaction reaction = reactionObject.AddComponent<ElementReaction>();
            HoseConnection hose = player.AddComponent<HoseConnection>(); SprayEmitter emitter = player.AddComponent<SprayEmitter>(); emitter.Initialize(hose, reaction); emitter.SetGroundHeight(0f); emitter.EnableDirectionalSpray(true); emitter.AutomaticSimulation = false;
            FluidSource source = new GameObject("Source").AddComponent<FluidSource>(); source.transform.SetParent(root.transform, false); source.transform.localPosition = Vector3.zero; source.Configure(element, flow); SprayProfile profile = CreateProfile(); source.SetSprayProfile(profile); hose.TryConnect(source);
            ProbeReceiver receiver = Receiver(root.transform, "Receiver", new Vector3(0f, .45f, 1.8f));
            return new Fixture { Root = root, Player = player, Hose = hose, Emitter = emitter, Source = source, Profile = profile, Receiver = receiver };
        }

        SprayProfile CreateProfile()
        {
            SprayProfile p = ScriptableObject.CreateInstance<SprayProfile>(); profiles.Add(p);
            SpraySettings s = SpraySettings.DefaultFor(ElementKind.Water); s.MinimumRange = s.MaximumRange = 3f; s.AirLifetime = .35f; s.SpreadAngle = 0f; s.CollisionRadius = .05f; s.ImpactRadius = .1f; s.PacketsPerSecond = 40f; p.Configure(s); return p;
        }
        static ProbeReceiver Receiver(Transform parent, string name, Vector3 position)
        {
            GameObject o = new GameObject(name); o.transform.SetParent(parent, false); o.transform.localPosition = position; o.AddComponent<SphereCollider>().radius = .35f; return o.AddComponent<ProbeReceiver>();
        }
        static float[] Repeat(float dt, int count) { float[] values = new float[count]; for (int i = 0; i < count; i++) values[i] = dt; return values; }
        static bool Approximately(float a, float b) => Mathf.Abs(a - b) < .0005f;
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; results.Add("PASS " + message); }
        void Note(string message) { checks++; results.Add("PROBE " + message); }
        void DestroyFixture(Fixture f) { fixtures.Remove(f.Root); if (f.Root) Destroy(f.Root); }
        void Cleanup() { foreach (GameObject f in fixtures) if (f) Destroy(f); foreach (ScriptableObject p in profiles) if (p) Destroy(p); fixtures.Clear(); profiles.Clear(); }
        struct Delivery { public float Amount; public int Hits; }
        struct Fixture { public GameObject Root, Player; public HoseConnection Hose; public SprayEmitter Emitter; public FluidSource Source; public SprayProfile Profile; public ProbeReceiver Receiver; }
    }

    public sealed class ProbeReceiver : ElementReceiver
    {
        public float Amount { get; private set; }
        public float Water { get; private set; }
        public float Oil { get; private set; }
        public int Hits { get; private set; }
        public override void Receive(ElementKind element, float amount)
        {
            Amount += amount; Hits++;
            if (element == ElementKind.Water) Water += amount;
            if (element == ElementKind.Oil) Oil += amount;
        }
    }
}
