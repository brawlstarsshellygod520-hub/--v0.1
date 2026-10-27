using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    /// <summary>Opt-in batch Play Mode proof for independent spray-effect multipliers.</summary>
    [InitializeOnLoad]
    public static class SprayEffectTuningValidation
    {
        const string SessionKey = "FireRescue.SprayEffectTuningValidation";
        static string ReportPath => SessionState.GetBool(SessionKey + ".Physics", false) ? HoseFluidPhysicsBuilder.Evidence + "/effects-regression.txt" : "Docs/Validation/2026-10-26-spray-effect-tuning/playmode.txt";
        static bool finished;

        static SprayEffectTuningValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false)) return;
                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary Spray Effect Tuning Validation").AddComponent<SprayEffectTuningValidationDriver>();
            };
        }

        [MenuItem("FireRescue/Validate Spray Effect Tuning")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool(SessionKey + ".Physics", false);
            Begin();
        }

        /// <summary>Unity batch entry point: FireRescue.Editor.SprayEffectTuningValidation.BatchRun.</summary>
        public static void BatchRun()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("BatchRun is intended for Unity batch mode.");
            SessionState.SetBool(SessionKey + ".Physics", false);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Begin();
        }
        public static void BatchRunPhysics()
        { SessionState.SetBool(SessionKey + ".Physics", true); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); Begin(); }

        static void Begin()
        {
            SessionState.SetBool(SessionKey, true);
            EditorApplication.EnterPlaymode();
        }

        static void OnLog(string text, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Finish(false, "Console " + type + ": " + text + "\n" + stack);
        }

        public static void Finish(bool success, string details)
        {
            if (finished) return;
            finished = true;
            SessionState.SetBool(SessionKey, false);
            Application.logMessageReceived -= OnLog;
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            string report = (success ? "SPRAY_EFFECT_TUNING_PLAYMODE_PASS" : "SPRAY_EFFECT_TUNING_PLAYMODE_FAIL") + "\n" + details;
            File.WriteAllText(ReportPath, report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
                else EditorApplication.ExitPlaymode();
            };
        }
    }

    public sealed class SprayEffectTuningValidationDriver : MonoBehaviour
    {
        readonly List<GameObject> fixtures = new List<GameObject>();
        readonly List<ScriptableObject> profiles = new List<ScriptableObject>();
        readonly List<string> results = new List<string>();
        readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        float started;
        float fixedTimeAtWait;
        bool waitingForFixedStep;
        int checks;

        void Start() { started = Time.realtimeSinceStartup; routines.Push(Scenario()); }
        void OnDestroy() { Cleanup(); }

        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 25f) throw new TimeoutException("Spray effect tuning validation timed out.");
                if (waitingForFixedStep)
                {
                    if (Time.fixedTime <= fixedTimeAtWait) return;
                    waitingForFixedStep = false;
                }
                while (routines.Count > 0)
                {
                    IEnumerator routine = routines.Peek();
                    if (!routine.MoveNext()) { routines.Pop(); continue; }
                    if (routine.Current is IEnumerator nested) { routines.Push(nested); continue; }
                    if (routine.Current is WaitForFixedUpdate)
                    {
                        fixedTimeAtWait = Time.fixedTime;
                        waitingForFixedStep = true;
                    }
                    return;
                }
                enabled = false;
                SprayEffectTuningValidation.Finish(true, "Checks=" + checks + "\n" + string.Join("\n", results));
            }
            catch (Exception error)
            {
                enabled = false;
                SprayEffectTuningValidation.Finish(false, "Checks passed before failure=" + checks + "\n" + string.Join("\n", results) + "\n" + error);
            }
        }

        IEnumerator Scenario()
        {
            VerifyNeutralAndInvalidSettings();
            VerifyFloorEffects();
            VerifyObjectFireAndOilRules();
            yield return VerifyPushEffects();
            yield return VerifyFlightEffectOwnership();
            VerifyPersistedProfilesAreReadable();
            Cleanup();
        }

        void VerifyNeutralAndInvalidSettings()
        {
            Fixture f = CreateFixture("neutral", 1300f);
            Vector3 cell = f.Field.CellCenter(0, 0);
            f.Field.Ignite(cell, 1f);
            f.Reaction.ApplySpray(ElementKind.Water, cell, Vector3.forward, 1f, null, .2f, 2f);
            FireCellState legacy = f.Field.GetCell(0, 0);
            f.Field.ResetSimulation(); f.Field.Ignite(cell, 1f);
            f.Reaction.ApplySpray(ElementKind.Water, cell, Vector3.forward, 1f, null, .2f, 2f, SprayEffectSettings.Neutral);
            FireCellState neutral = f.Field.GetCell(0, 0);
            Check(Approximately(legacy.Intensity, neutral.Intensity) && Approximately(legacy.Wetness, neutral.Wetness) && Approximately(legacy.WaterRemaining, neutral.WaterRemaining),
                "Neutral effects preserve legacy ApplySpray floor behavior.");

            SprayEffectSettings invalid = new SprayEffectSettings { ExtinguishMultiplier = float.NaN, PushMultiplier = float.PositiveInfinity, ResidueMultiplier = float.NegativeInfinity }.Validated();
            Check(Approximately(invalid.ExtinguishMultiplier, 1f) && Approximately(invalid.PushMultiplier, 1f) && Approximately(invalid.ResidueMultiplier, 1f),
                "NaN and infinity effect settings fall back safely to neutral.");
            SprayEffectSettings clamped = new SprayEffectSettings { ExtinguishMultiplier = -2f, PushMultiplier = 9f, ResidueMultiplier = 2f }.Validated();
            Check(clamped.ExtinguishMultiplier == 0f && clamped.PushMultiplier == 4f && clamped.ResidueMultiplier == 2f,
                "Effect settings clamp independently to the 0..4 authoring range.");
        }

        void VerifyFloorEffects()
        {
            Fixture f = CreateFixture("floor", 1320f);
            Vector3 cell = f.Field.CellCenter(0, 0);
            SprayEffectSettings noExtinguish = Effects(0f, 1f, 1f);
            SprayEffectSettings noResidue = Effects(1f, 1f, 0f);

            f.Field.Ignite(cell, 1f);
            f.Reaction.ApplySpray(ElementKind.Water, cell, Vector3.forward, 1f, null, .2f, 2f, noExtinguish);
            FireCellState waterNoExtinguish = f.Field.GetCell(0, 0);
            Check(waterNoExtinguish.Intensity > .99f && waterNoExtinguish.Wetness > 0f && waterNoExtinguish.WaterRemaining > 0f,
                "Water extinguish zero still leaves timed water residue.");
            f.Field.ResetSimulation(); f.Field.Ignite(cell, 1f);
            f.Reaction.ApplySpray(ElementKind.Water, cell, Vector3.forward, 1f, null, .2f, 2f, noResidue);
            FireCellState waterNoResidue = f.Field.GetCell(0, 0);
            Check(waterNoResidue.Intensity < .99f && waterNoResidue.Wetness == 0f && waterNoResidue.WaterRemaining == 0f,
                "Water residue zero still extinguishes without creating residue.");

            f.Field.ResetSimulation(); f.Field.Ignite(cell, 1f);
            f.Reaction.ApplySpray(ElementKind.DryPowder, cell, Vector3.forward, 1f, null, .2f, 2f, noExtinguish);
            FireCellState powderNoExtinguish = f.Field.GetCell(0, 0);
            Check(powderNoExtinguish.Intensity > .99f && powderNoExtinguish.Powder > 0f && powderNoExtinguish.PowderRemaining > 0f,
                "Powder extinguish zero still leaves powder residue.");
            f.Field.ResetSimulation(); f.Field.Ignite(cell, 1f);
            f.Reaction.ApplySpray(ElementKind.DryPowder, cell, Vector3.forward, 1f, null, .2f, 2f, noResidue);
            FireCellState powderNoResidue = f.Field.GetCell(0, 0);
            Check(powderNoResidue.Intensity < .99f && powderNoResidue.Powder == 0f && powderNoResidue.PowderRemaining == 0f,
                "Powder residue zero still extinguishes without wetting the floor.");

            f.Field.ApplyTimedWater(cell, 1f, .2f, 3f);
            float before = f.Field.GetCell(0, 0).WaterRemaining;
            f.Field.Step(.5f);
            float aging = f.Field.GetCell(0, 0).WaterRemaining;
            f.Reaction.ApplySpray(ElementKind.Water, cell, Vector3.forward, 1f, null, .2f, 3f, noResidue);
            Check(f.Field.GetCell(0, 0).WaterRemaining <= aging + .001f && aging < before,
                "Zero residue does not refresh an existing water timer.");
        }

        void VerifyObjectFireAndOilRules()
        {
            Fixture f = CreateFixture("object fires", 1340f);
            ObjectFire ordinary = CreateObjectFire(f.Root.transform, FireKind.Ordinary, new Vector3(1340f, 0f, 2f));
            ObjectFire oilFire = CreateObjectFire(f.Root.transform, FireKind.CookingOil, new Vector3(1342f, 0f, 2f));
            SprayEffectSettings zero = Effects(0f, 1f, 1f);
            SprayEffectSettings doubleExtinguish = Effects(2f, 1f, 1f);
            f.Reaction.ApplySpray(ElementKind.Water, ordinary.transform.position, Vector3.forward, 1f, ordinary.GetComponent<Collider>(), .2f, 1f, zero);
            f.Reaction.ApplySpray(ElementKind.Water, oilFire.transform.position, Vector3.forward, 2f, oilFire.GetComponent<Collider>(), .2f, 1f, doubleExtinguish);
            Check(ordinary.IsBurning, "ObjectFire respects zero extinguish multiplier.");
            Check(oilFire.IsBurning && oilFire.Kind == FireKind.CookingOil, "Water does not extinguish CookingOil ObjectFire regardless of multiplier.");
            f.Reaction.ApplySpray(ElementKind.DryPowder, oilFire.transform.position, Vector3.forward, 1f, oilFire.GetComponent<Collider>(), .2f, 1f, doubleExtinguish);
            Check(!oilFire.IsBurning, "Dry powder retains CookingOil ObjectFire eligibility and uses extinguish multiplier.");

            Vector3 cell = f.Field.CellCenter(0, 0);
            f.Reaction.ApplySpray(ElementKind.Oil, cell, Vector3.forward, .3f, null, .2f, 2f, Effects(0f, 0f, 1f));
            float oilWithNeutralResidue = f.Field.GetCell(0, 0).Oil;
            f.Field.ResetSimulation();
            f.Reaction.ApplySpray(ElementKind.Oil, cell, Vector3.forward, .3f, null, .2f, 2f, Effects(2f, 2f, 1f));
            Check(Approximately(oilWithNeutralResidue, f.Field.GetCell(0, 0).Oil), "Oil ignores extinguish and push multipliers.");
            f.Field.ResetSimulation();
            f.Reaction.ApplySpray(ElementKind.Oil, cell, Vector3.forward, .3f, null, .2f, 2f, Effects(1f, 1f, 2f));
            Check(Approximately(f.Field.GetCell(0, 0).Oil, oilWithNeutralResidue * 2f), "Oil residue multiplier changes only deposited oil amount.");
        }

        IEnumerator VerifyPushEffects()
        {
            Fixture f = CreateFixture("push", 1360f);
            Rigidbody body = DynamicBody(f.Root.transform, new Vector3(1360f, .5f, 2f));
            Collider hit = body.GetComponent<Collider>();
            f.Reaction.ApplySpray(ElementKind.Water, body.position, Vector3.right, 1f, hit, .2f, 1f, Effects(1f, 0f, 1f));
            yield return new WaitForFixedUpdate();
            Check(body.linearVelocity.sqrMagnitude < .0001f, "Water push zero applies no Rigidbody impulse.");
            body.linearVelocity = Vector3.zero;
            f.Reaction.ApplySpray(ElementKind.Water, body.position, Vector3.right, 1f, hit, .2f, 1f, SprayEffectSettings.Neutral);
            yield return new WaitForFixedUpdate();
            float neutralSpeed = body.linearVelocity.x;
            body.linearVelocity = Vector3.zero;
            f.Reaction.ApplySpray(ElementKind.Water, body.position, Vector3.right, 1f, hit, .2f, 1f, Effects(1f, 2f, 1f));
            yield return new WaitForFixedUpdate();
            Check(neutralSpeed > .01f && body.linearVelocity.x > neutralSpeed * 1.8f && body.linearVelocity.x < neutralSpeed * 2.2f,
                "Water push multiplier scales Rigidbody impulse independently.");
            body.linearVelocity = Vector3.zero;
            f.Reaction.ApplySpray(ElementKind.Oil, body.position, Vector3.right, 1f, hit, .2f, 1f, Effects(1f, 4f, 1f));
            f.Reaction.ApplySpray(ElementKind.DryPowder, body.position, Vector3.right, 1f, hit, .2f, 1f, Effects(1f, 4f, 1f));
            yield return new WaitForFixedUpdate();
            Check(body.linearVelocity.sqrMagnitude < .0001f, "Oil and powder never push Rigidbody targets.");
            Rigidbody fixedBody = DynamicBody(f.Root.transform, new Vector3(1362f, .5f, 2f)); fixedBody.isKinematic = true;
            f.Reaction.ApplySpray(ElementKind.Water, fixedBody.position, Vector3.right, 1f, fixedBody.GetComponent<Collider>(), .2f, 1f, Effects(1f, 4f, 1f));
            Check(fixedBody.linearVelocity.sqrMagnitude == 0f, "Fixed Rigidbody targets remain immovable.");
        }

        IEnumerator VerifyFlightEffectOwnership()
        {
            Fixture f = CreateFixture("flight ownership", 1380f);
            f.Emitter.EnableDirectionalSpray(true); f.Emitter.AutomaticSimulation = false;
            SprayProfile shared = f.Profile;
            shared.ConfigureEffects(Effects(1f, 1f, 1f));
            f.Emitter.Tick(f.Root.transform.position + Vector3.forward * 4f, true, .02f);
            Check(f.Emitter.ActiveParticleCount > 0 && f.Source.SprayEffects.ResidueMultiplier == 1f,
                "First emitter captures the shared profile's initial effects.");
            shared.ConfigureEffects(Effects(1f, 1f, 0f));
            FluidSource secondSource = new GameObject("Second shared-profile source").AddComponent<FluidSource>();
            secondSource.transform.SetParent(f.Root.transform); secondSource.transform.position = f.Root.transform.position;
            secondSource.Configure(ElementKind.Water, 3f); secondSource.SetSprayProfile(shared);
            GameObject secondPlayer = new GameObject("Second emitter"); secondPlayer.transform.SetParent(f.Root.transform); secondPlayer.transform.position = secondSource.transform.position;
            HoseConnection secondHose = secondPlayer.AddComponent<HoseConnection>(); SprayEmitter secondEmitter = secondPlayer.AddComponent<SprayEmitter>();
            secondEmitter.Initialize(secondHose, f.Reaction); secondEmitter.SetGroundHeight(0f); secondEmitter.EnableDirectionalSpray(true); secondEmitter.AutomaticSimulation = false; secondHose.TryConnect(secondSource);
            secondEmitter.Tick(secondPlayer.transform.position + Vector3.forward * 4f, true, .02f);
            Physics.SyncTransforms();
            f.Emitter.Advance(2f);
            Check(f.Field.GetCell(0, 0).WaterRemaining > 0f, "Packets already flying retain their captured residue multiplier after profile edits.");
            f.Field.ResetSimulation(); secondEmitter.Advance(2f);
            Check(f.Field.GetCell(0, 0).WaterRemaining == 0f && secondSource.SprayEffects.ResidueMultiplier == 0f,
                "New packets from a second emitter use the changed shared profile effects.");
            yield return null;
        }

        void VerifyPersistedProfilesAreReadable()
        {
            string[] paths = { "Assets/FireRescue/Settings/Sprays/Water.asset", "Assets/FireRescue/Settings/Sprays/Oil.asset", "Assets/FireRescue/Settings/Sprays/DryPowder.asset" };
            for (int i = 0; i < paths.Length; i++)
            {
                SprayProfile profile = AssetDatabase.LoadAssetAtPath<SprayProfile>(paths[i]);
                Check(profile != null && Neutral(profile.Effects), "Saved spray profile reloads read-only with neutral default effects: " + paths[i]);
            }
            SprayProfile unconfigured = ScriptableObject.CreateInstance<SprayProfile>(); profiles.Add(unconfigured);
            Check(Neutral(unconfigured.Effects), "A new unconfigured SprayProfile defaults effects to neutral.");
        }

        Fixture CreateFixture(string label, float x)
        {
            GameObject root = new GameObject("Temporary spray effect validation - " + label); root.transform.position = new Vector3(x, 0f, 0f); fixtures.Add(root);
            FireField field = new GameObject("Field").AddComponent<FireField>(); field.transform.SetParent(root.transform); field.Configure(new Vector3(x - .5f, 0f, 2.5f), 1, 1, 1f); field.SetIgnitionSeeds(Array.Empty<Vector2Int>()); field.ResetSimulation(); field.Simulate = false;
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "Ground"; ground.transform.SetParent(root.transform); ground.transform.position = new Vector3(x, -.05f, 3f); ground.transform.localScale = new Vector3(3f, .1f, 3f);
            ElementReaction reaction = new GameObject("Reaction").AddComponent<ElementReaction>(); reaction.transform.SetParent(root.transform); reaction.Initialize(field);
            GameObject player = new GameObject("Player"); player.transform.SetParent(root.transform); player.transform.position = root.transform.position; player.transform.forward = Vector3.forward;
            HoseConnection hose = player.AddComponent<HoseConnection>(); SprayEmitter emitter = player.AddComponent<SprayEmitter>(); emitter.Initialize(hose, reaction); emitter.SetGroundHeight(0f);
            FluidSource source = new GameObject("Source").AddComponent<FluidSource>(); source.transform.SetParent(root.transform); source.transform.position = player.transform.position; source.Configure(ElementKind.Water, 3f);
            SprayProfile profile = ScriptableObject.CreateInstance<SprayProfile>(); profiles.Add(profile); profile.Configure(DirectionSettings()); profile.ConfigureEffects(SprayEffectSettings.Neutral); source.SetSprayProfile(profile); hose.TryConnect(source);
            return new Fixture { Root = root, Field = field, Reaction = reaction, Emitter = emitter, Source = source, Profile = profile };
        }

        static SpraySettings DirectionSettings()
        {
            SpraySettings settings = SpraySettings.DefaultFor(ElementKind.Water);
            settings.MinimumRange = settings.MaximumRange = 3f; settings.AirLifetime = .35f; settings.GroundLifetime = 2f; settings.SpreadAngle = 0f; settings.CollisionRadius = .05f; settings.ImpactRadius = .3f; settings.ArcHeight = 0f; settings.PacketsPerSecond = 20f;
            return settings;
        }

        static ObjectFire CreateObjectFire(Transform parent, FireKind kind, Vector3 position)
        { GameObject o = new GameObject(kind + " ObjectFire"); o.transform.SetParent(parent); o.transform.position = position; o.AddComponent<SphereCollider>().radius = .4f; ObjectFire fire = o.AddComponent<ObjectFire>(); fire.Configure(kind, 1f); return fire; }
        static Rigidbody DynamicBody(Transform parent, Vector3 position)
        { GameObject o = new GameObject("Rigidbody target"); o.transform.SetParent(parent); o.transform.position = position; o.AddComponent<SphereCollider>(); Rigidbody body = o.AddComponent<Rigidbody>(); body.useGravity = false; return body; }
        static SprayEffectSettings Effects(float extinguish, float push, float residue) => new SprayEffectSettings { ExtinguishMultiplier = extinguish, PushMultiplier = push, ResidueMultiplier = residue };
        static bool Approximately(float a, float b) => Mathf.Abs(a - b) < .0001f;
        static bool Neutral(SprayEffectSettings value) => Approximately(value.ExtinguishMultiplier, 1f) && Approximately(value.PushMultiplier, 1f) && Approximately(value.ResidueMultiplier, 1f);
        void Check(bool condition, string message) { if (!condition) { results.Add("FAIL " + message); throw new InvalidOperationException(message); } checks++; results.Add("PASS " + message); }
        void Cleanup() { foreach (GameObject fixture in fixtures) if (fixture) Destroy(fixture); fixtures.Clear(); foreach (ScriptableObject profile in profiles) if (profile) Destroy(profile); profiles.Clear(); }

        struct Fixture { public GameObject Root; public FireField Field; public ElementReaction Reaction; public SprayEmitter Emitter; public FluidSource Source; public SprayProfile Profile; }
    }
}
