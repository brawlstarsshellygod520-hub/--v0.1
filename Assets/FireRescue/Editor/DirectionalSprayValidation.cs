using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    /// <summary>Batch Play Mode contract proof for opt-in, deterministic directional hose spray.</summary>
    [InitializeOnLoad]
    public static class DirectionalSprayValidation
    {
        const string SessionKey = "FireRescue.DirectionalSprayValidation";
        static bool PhysicsRun => SessionState.GetBool(SessionKey + ".Physics", false);
        static string ReportPath => PhysicsRun ? HoseFluidPhysicsBuilder.Evidence + "/directional-regression.txt" : "Docs/Validation/2026-10-26-directional-spray/playmode.txt";
        public static string EvidencePath => PhysicsRun ? HoseFluidPhysicsBuilder.Evidence + "/DirectionalVisuals" : "Docs/Validation/2026-10-26-directional-spray/Evidence";
        static bool finished;
        static double readyAt;

        static DirectionalSprayValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false)) return;
                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary Directional Spray Validation").AddComponent<DirectionalSprayValidationDriver>();
            };
        }

        [MenuItem("FireRescue/Validate Directional Spray")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetBool(SessionKey + ".Physics", false);
            BeginWhenReady();
        }

        /// <summary>Unity batch entry point: FireRescue.Editor.DirectionalSprayValidation.BatchRun.</summary>
        public static void BatchRun()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("BatchRun is intended for Unity batch mode.");
            SessionState.SetBool(SessionKey + ".Physics", false);
            BeginWhenReady();
        }
        public static void BatchRunPhysics()
        { SessionState.SetBool(SessionKey + ".Physics", true); BeginWhenReady(); }

        static void BeginWhenReady()
        {
            readyAt = EditorApplication.timeSinceStartup + 2d;
            EditorApplication.update -= Begin;
            EditorApplication.update += Begin;
        }

        static void Begin()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < readyAt) return;
            EditorApplication.update -= Begin;
            try
            {
                EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
                SessionState.SetBool(SessionKey, true);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception error) { Finish(false, "Play Mode setup exception:\n" + error); }
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
            string report = (success ? "DIRECTIONAL_SPRAY_PLAYMODE_PASS" : "DIRECTIONAL_SPRAY_PLAYMODE_FAIL") + "\n" + details;
            File.WriteAllText(ReportPath, report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () => { if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1); else EditorApplication.ExitPlaymode(); };
        }
    }

    public sealed class DirectionalSprayValidationDriver : MonoBehaviour
    {
        readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        readonly List<GameObject> fixtures = new List<GameObject>();
        readonly List<ScriptableObject> profiles = new List<ScriptableObject>();
        readonly List<string> results = new List<string>();
        float started;
        int checks;

        void Start() { started = Time.realtimeSinceStartup; routines.Push(Scenario()); }
        void OnDestroy()
        {
            foreach (GameObject fixture in fixtures) if (fixture) Destroy(fixture);
            foreach (ScriptableObject profile in profiles) if (profile) Destroy(profile);
        }
        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 50f) throw new TimeoutException("Directional spray validation timed out.");
                while (routines.Count > 0)
                {
                    IEnumerator routine = routines.Peek();
                    if (!routine.MoveNext()) { routines.Pop(); continue; }
                    if (routine.Current is IEnumerator nested) { routines.Push(nested); continue; }
                    return;
                }
                enabled = false;
                DirectionalSprayValidation.Finish(true, "Checks=" + checks + "\n" + string.Join("\n", results));
            }
            catch (Exception error)
            {
                enabled = false;
                DirectionalSprayValidation.Finish(false, "Checks passed before failure=" + checks + "\n" + string.Join("\n", results) + "\n" + error);
            }
        }

        IEnumerator Scenario()
        {
            yield return VerifyOptInAndFlightOwnership();
            yield return VerifyAimAndCollisionContracts();
            yield return VerifyWallOcclusion();
            yield return VerifyResiduesAndTargets();
            yield return VerifyTimedFieldAndRateContracts();
            yield return VerifyParticleVisuals();
        }

        IEnumerator VerifyOptInAndFlightOwnership()
        {
            Fixture f = CreateFixture("opt in", 400f, ElementKind.Water, Settings(ElementKind.Water));
            Check(!f.Emitter.UsesDirectionalSpray, "Directional spray remains opt-in for legacy serialized scenes.");
            f.Emitter.EnableDirectionalSpray(true); f.Emitter.AutomaticSimulation = false;
            Cube(f.Root.transform, "ground", new Vector3(400f, -.05f, 3f), new Vector3(12f, .1f, 12f), 0);
            Physics.SyncTransforms();
            f.Emitter.Tick(new Vector3(420f, 99f, 40f), true, .01f);
            int emitted = f.Emitter.TotalEmitted;
            Check(emitted > 0 && f.Emitter.ActiveParticleCount > 0 && f.Emitter.TotalImpacts == 0,
                "Tick emits manually simulated packets but cannot damage at spawn.");
            for (int i = 0; i < 30; i++) f.Emitter.Tick(new Vector3(420f, 0f, 40f), true, .25f);
            Check(f.Emitter.ActiveParticleCount <= SprayEmitter.ParticleCapacity,
                "Live directional packets never exceed the fixed capacity bound.");
            f.Emitter.Stop(); f.Hose.Disconnect(); f.Emitter.Advance(.5f);
            Check(f.Emitter.ActiveParticleCount == 0 && f.Emitter.TotalImpacts > 0,
                "Already emitted packets age and impact after Stop and disconnect.");
            f.Hose.TryConnect(f.Source); f.Emitter.EnableDirectionalSpray(true);
            f.Emitter.Tick(new Vector3(400f, 0f, 30f), true, .01f);
            ElementKind before = f.Emitter.GetParticle(0).Element;
            FluidSource replacement = new GameObject("Replacement oil source").AddComponent<FluidSource>(); replacement.transform.SetParent(f.Root.transform); replacement.transform.position = f.Emitter.transform.position; replacement.Configure(ElementKind.Oil, 5f);
            SpraySettings swappedSettings = Settings(ElementKind.Oil); swappedSettings.ArcHeight = .7f;
            SprayProfile oilProfile = ScriptableObject.CreateInstance<SprayProfile>(); profiles.Add(oilProfile); f.Profiles.Add(oilProfile); oilProfile.Configure(swappedSettings); replacement.SetSprayProfile(oilProfile);
            f.Hose.Disconnect(); f.Hose.TryConnect(replacement); f.Emitter.Tick(new Vector3(430f, 0f, 0f), true, .01f);
            SprayEmitter.ParticleSample old = f.Emitter.GetParticle(0);
            SprayEmitter.ParticleSample latest = f.Emitter.GetParticle(f.Emitter.ActiveParticleCount - 1);
            Check(old.Element == before && old.Velocity.z > 0f && latest.Element == ElementKind.Oil && latest.Velocity.x > 0f && latest.Velocity.y > 0f,
                "Aim/source changes are captured per packet and affect only newly emitted packets.");
            f.Hose.Source.gameObject.SetActive(false); f.Emitter.Tick(Vector3.forward, true, .1f);
            Check(!f.Emitter.IsSpraying, "A disabled source cannot emit.");
            f.Emitter.gameObject.SetActive(false);
            Check(f.Emitter.ActiveParticleCount == 0, "Disabling the emitter clears all live packets.");
            yield return DestroyFixture(f);
        }

        IEnumerator VerifyAimAndCollisionContracts()
        {
            Fixture f = CreateFixture("aim and collision", 500f, ElementKind.Water, Settings(ElementKind.Water));
            f.Emitter.EnableDirectionalSpray(true); f.Emitter.AutomaticSimulation = false;
            Vector3 near = SamplePosition(f, new Vector3(501f, 0f, 8f), .2f);
            Vector3 far = SamplePosition(f, new Vector3(510f, 30f, 80f), .2f);
            Check(Vector3.Distance(near, far) < .001f, "Near, far, and high-Y cursor aims with one azimuth share a trajectory.");
            var input = f.Emitter.gameObject.AddComponent<LocalPlayerInput>(); input.ReadDevices = false;
            input.Initialize(null, f.Hose, f.Emitter, null, new[] { f.Source }, Array.Empty<RescueNpc>());
            input.SetInputFocus(true); f.Emitter.EnableDirectionalSpray(true);
            input.TickPointerCommand(Vector2.zero, f.Emitter.transform.position + Vector3.right * .1f,
                false, false, false, false, true, true, .01f);
            Check(f.Emitter.ActiveParticleCount > 0 && f.Emitter.GetParticle(0).Velocity.x > 0f &&
                Mathf.Abs(f.Emitter.GetParticle(0).Velocity.z) < .001f,
                "LocalPlayerInput preserves heading for a near cursor instead of forcing the old facing.");
            f.Emitter.EnableDirectionalSpray(true);
            Vector3 origin = f.Emitter.NozzlePosition;
            f.Emitter.Tick(new Vector3(500f, 0f, 30f), true, .01f);
            Check(Vector3.Distance(f.Emitter.GetParticle(0).Position, origin) < .001f && origin.x > 499f,
                "Packet endpoint ownership starts at the nozzle/player, never at the cursor.");
            GameObject wall = Cube(f.Root.transform, "early wall", new Vector3(500f, .45f, 1.2f), new Vector3(4f, .9f, .16f), 8);
            Physics.SyncTransforms(); f.Emitter.Advance(1f);
            Check(f.Emitter.TotalImpacts > 0 && f.Field.GetCell(0, 0).Wetness == 0f,
                "An early fixed wall stops spray and creates no residue behind it.");
            f.Emitter.EnableDirectionalSpray(true); f.Emitter.Tick(new Vector3(500f, 0f, 20f), true, .01f);
            wall.transform.position = f.Emitter.NozzlePosition; Physics.SyncTransforms(); f.Emitter.Advance(.1f);
            Check(f.Emitter.ActiveParticleCount == 0, "A nozzle initially inside a wall cannot launch material through it.");
            yield return DestroyFixture(f);
            Fixture air = CreateFixture("air expiry", 560f, ElementKind.Water, Settings(ElementKind.Water));
            air.Emitter.EnableDirectionalSpray(true); air.Emitter.AutomaticSimulation = false;
            air.Emitter.Tick(air.Root.transform.position + Vector3.forward * 8f, true, .01f); air.Emitter.Advance(2f);
            FireCellState empty = air.Field.GetCell(0, 0);
            Check(air.Emitter.ActiveParticleCount == 0 && air.Emitter.TotalImpacts == 0 && empty.Wetness == 0f && empty.WaterRemaining == 0f,
                "Without a floor, a packet expires at AirLifetime without producing residue.");
            yield return DestroyFixture(air);
        }

        IEnumerator VerifyWallOcclusion()
        {
            Fixture solid = CreateFixture("occlusion solid", 1100f, ElementKind.Water, WallSettings(0f));
            Transform solidNozzle = Nozzle(solid.Root.transform, new Vector3(1100f, 1.04f, .55f)); solid.Emitter.SetNozzleAnchor(solidNozzle);
            GameObject wallObject = Cube(solid.Root.transform, "cutaway wall", new Vector3(1100f, .36f, 1f), new Vector3(4f, .72f, .1f), 0);
            SprayWallOcclusion wall = solid.Root.AddComponent<SprayWallOcclusion>(); wall.Configure(new[] { wallObject.GetComponent<Collider>() }, Array.Empty<HoseWindow>()); solid.Emitter.SetWallOcclusion(wall);
            Physics.SyncTransforms();
            Vector3 from = new Vector3(1100f, 1.04f, 0f), through = new Vector3(1100f, 1.04f, 6f);
            Check(wall.TryBlock(from, through, out Collider blockedWall, out Vector3 blockedPoint) && blockedWall == wallObject.GetComponent<Collider>() && blockedPoint.z < 1.1f,
                "Low cutaway wall blocks the projected high-nozzle path as authored geometry.");
            Physics.SyncTransforms(); solid.Emitter.EnableDirectionalSpray(true); solid.Emitter.AutomaticSimulation = false;
            solid.Emitter.Tick(through, true, .01f); solid.Emitter.Advance(.15f);
            Check(solid.Emitter.TotalImpacts > 0 && solid.Emitter.ActiveParticleCount == 0,
                "A real flight is stopped by the low cutaway wall rather than passing above its visible mesh.");
            yield return DestroyFixture(solid);

            Fixture opening = CreateFixture("occlusion opening", 1140f, ElementKind.Water, WallSettings(.3f));
            Transform openNozzle = Nozzle(opening.Root.transform, new Vector3(1140f, 1.04f, .55f)); opening.Emitter.SetNozzleAnchor(openNozzle);
            GameObject openingWall = Cube(opening.Root.transform, "window wall", new Vector3(1140f, .36f, 1f), new Vector3(4f, .72f, .1f), 0);
            HoseWindow window = new GameObject("rotated hose window").AddComponent<HoseWindow>(); window.transform.SetParent(opening.Root.transform); window.transform.position = new Vector3(1140f, 0f, 1f); window.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            SprayWallOcclusion windowOcclusion = opening.Root.AddComponent<SprayWallOcclusion>(); windowOcclusion.Configure(new[] { openingWall.GetComponent<Collider>() }, new[] { window }); opening.Emitter.SetWallOcclusion(windowOcclusion);
            Physics.SyncTransforms();
            bool centerBlocked = windowOcclusion.TryBlock(new Vector3(1140f, 1.2f, 0f), new Vector3(1140f, 1.2f, 6f), out _, out _);
            bool outsideBlocked = windowOcclusion.TryBlock(new Vector3(1140.8f, 1.2f, 0f), new Vector3(1140.8f, 1.2f, 6f), out _, out _);
            Check(!centerBlocked && outsideBlocked, "A Y-90 hose-window opening permits its aperture but not the wall outside it.");
            Physics.SyncTransforms(); opening.Emitter.EnableDirectionalSpray(true); opening.Emitter.AutomaticSimulation = false;
            opening.Emitter.Tick(new Vector3(1140f, 1.04f, 8f), true, .01f); opening.Emitter.Advance(.15f);
            Check(opening.Emitter.ActiveParticleCount > 0 && opening.Emitter.TotalImpacts == 0,
                "A high-arc flight can cross only the explicit rotated window opening.");
            yield return DestroyFixture(opening);

            Fixture extended = CreateFixture("occlusion extended nozzle", 1180f, ElementKind.Water, WallSettings(0f));
            Transform extendedNozzle = Nozzle(extended.Root.transform, new Vector3(1180f, 1.04f, 1.2f)); extended.Emitter.SetNozzleAnchor(extendedNozzle);
            GameObject extendedWall = Cube(extended.Root.transform, "nozzle crossing wall", new Vector3(1180f, .36f, 1f), new Vector3(4f, .72f, .1f), 0);
            SprayWallOcclusion extendedOcclusion = extended.Root.AddComponent<SprayWallOcclusion>(); extendedOcclusion.Configure(new[] { extendedWall.GetComponent<Collider>() }, Array.Empty<HoseWindow>()); extended.Emitter.SetWallOcclusion(extendedOcclusion);
            Physics.SyncTransforms(); extended.Emitter.EnableDirectionalSpray(true); extended.Emitter.AutomaticSimulation = false;
            extended.Emitter.Tick(new Vector3(1180f, 1.04f, 8f), true, .01f);
            Check(extended.Emitter.GetParticle(0).Position.z < extendedNozzle.position.z,
                "A nozzle extending through a wall is clipped back by player-to-nozzle occlusion before launch.");
            extended.Emitter.Advance(.15f);
            Check(extended.Emitter.TotalImpacts > 0 && extended.Emitter.ActiveParticleCount == 0,
                "The clipped launch cannot escape through the wall on its first flight segment.");
            yield return DestroyFixture(extended);
        }

        IEnumerator VerifyResiduesAndTargets()
        {
            foreach (ElementKind kind in new[] { ElementKind.Water, ElementKind.Oil, ElementKind.DryPowder })
            {
                Fixture f = CreateFixture("ground " + kind, 600f + (int)kind * 30f, kind, Settings(kind));
                f.Emitter.EnableDirectionalSpray(true); f.Emitter.AutomaticSimulation = false;
                Cube(f.Root.transform, "ground", new Vector3(f.Root.transform.position.x, -.05f, 3f), new Vector3(12f, .1f, 12f), 0);
                Physics.SyncTransforms(); f.Emitter.Tick(f.Root.transform.position + Vector3.forward * 8f, true, .02f); f.Emitter.Advance(2f);
                FireCellState state = f.Field.GetCell(0, 0);
                Check(kind == ElementKind.Water ? state.Wetness > 0f && state.WaterRemaining > 0f :
                    kind == ElementKind.Oil ? state.Oil > 0f && state.OilRemaining > 0f : state.Powder > 0f && state.PowderRemaining > 0f,
                    kind + " ground impact uses its configured radius and lifetime.");
                yield return DestroyFixture(f);
            }
            Fixture targets = CreateFixture("object fires", 720f, ElementKind.Water, Settings(ElementKind.Water));
            ObjectFire ordinary = CreateObjectFire(targets.Root.transform, FireKind.Ordinary, new Vector3(720f, 0f, 2f));
            ObjectFire oil = CreateObjectFire(targets.Root.transform, FireKind.CookingOil, new Vector3(722f, 0f, 2f));
            targets.Reaction.ApplySpray(ElementKind.Water, ordinary.transform.position, Vector3.forward, 2f, ordinary.GetComponent<Collider>(), 1f, 1f);
            targets.Reaction.ApplySpray(ElementKind.DryPowder, oil.transform.position, Vector3.forward, 2f, oil.GetComponent<Collider>(), 1f, 1f);
            Check(!ordinary.IsBurning && !oil.IsBurning, "Water hits ordinary ObjectFire and powder hits cooking-oil ObjectFire.");
            yield return DestroyFixture(targets);
        }

        IEnumerator VerifyTimedFieldAndRateContracts()
        {
            Fixture f = CreateFixture("timed field", 800f, ElementKind.Water, Settings(ElementKind.Water));
            Vector3 c = f.Field.CellCenter(0, 0);
            f.Field.ApplyTimedWater(c, 1f, .1f, .3f); f.Field.Step(.2f); f.Field.ApplyTimedWater(c, 1f, .1f, .5f);
            Check(f.Field.GetCell(0, 0).WaterRemaining > .45f, "Timed residue refreshes to the maximum duration.");
            f.Field.Step(.6f); Check(f.Field.GetCell(0, 0).Wetness == 0f, "Timed water expires and clears wetness.");
            f.Field.ApplyTimedOil(c, 1f, .1f, .2f); float oil = f.Field.GetCell(0, 0).Oil;
            f.Field.ApplyTimedOil(c, 1f, .1f, 0f); Check(Mathf.Approximately(oil, f.Field.GetCell(0, 0).Oil), "Zero-duration oil creates no new puddle and preserves old state.");
            f.Field.ApplyTimedDryPowder(c, 1f, .1f, .2f); f.Field.Step(.3f);
            Check(f.Field.GetCell(0, 0).Oil == 0f && f.Field.GetCell(0, 0).Powder == 0f, "Timed oil and powder expire independently.");
            FireFieldView view = f.Field.gameObject.AddComponent<FireFieldView>(); view.Initialize(f.Field); view.enabled = false;
            f.Field.ApplyTimedWater(c, 1f, .1f, 1f); Check(f.Field.GetCell(0, 0).Wetness > 0f, "Disabled FireFieldView cannot disable simulation state.");
            f.Emitter.EnableDirectionalSpray(true); f.Emitter.AutomaticSimulation = false;
            f.Emitter.Tick(f.Root.transform.position + Vector3.forward * 8f, true, .01f); f.Emitter.Advance(.05f);
            Check(f.Emitter.ActiveParticleCount > 0 && f.Emitter.GetParticle(0).Position.z > f.Emitter.NozzlePosition.z,
                "Disabling FireFieldView does not stop an independent emitter flight.");
            f.Field.ResetSimulation(); Check(f.Field.GetCell(0, 0).Wetness == 0f && f.Field.GetCell(0, 0).WaterRemaining == 0f, "Reset clears residue state.");
            FrameRateResult rate30 = SimulateFrameRate(30), rate60 = SimulateFrameRate(60);
            Check(Mathf.Abs(rate30.Emitted - rate60.Emitted) <= 1 && Mathf.Abs(rate30.Impacts - rate60.Impacts) <= 1 &&
                Mathf.Abs(rate30.Wetness - rate60.Wetness) < .01f && rate30.Wetness < .5f && rate60.Wetness < .5f,
                "30Hz versus 60Hz Tick+Advance preserves emitted, impacts, and unclamped residue within one packet.");
            SpraySettings invalid = Settings(ElementKind.Water); invalid.MinimumRange = float.NaN; invalid.MaximumRange = float.PositiveInfinity;
            invalid.AirLifetime = float.NegativeInfinity; invalid.GroundLifetime = float.NaN; invalid.SpreadAngle = float.PositiveInfinity;
            invalid.CollisionRadius = float.NaN; invalid.ImpactRadius = float.PositiveInfinity; invalid.ArcHeight = float.NaN; invalid.PacketsPerSecond = float.PositiveInfinity;
            SprayProfile sanitizedProfile = ScriptableObject.CreateInstance<SprayProfile>(); profiles.Add(sanitizedProfile); f.Profiles.Add(sanitizedProfile); sanitizedProfile.Configure(invalid);
            SpraySettings sanitized = sanitizedProfile.Settings;
            Check(Finite(sanitized.MinimumRange) && Finite(sanitized.MaximumRange) && Finite(sanitized.AirLifetime) && Finite(sanitized.GroundLifetime) &&
                Finite(sanitized.SpreadAngle) && Finite(sanitized.CollisionRadius) && Finite(sanitized.ImpactRadius) && Finite(sanitized.ArcHeight) && Finite(sanitized.PacketsPerSecond),
                "NaN and Infinity spray parameters are clamped to finite validated settings.");
            SpraySettings water = SpraySettings.DefaultFor(ElementKind.Water), oilProfile = SpraySettings.DefaultFor(ElementKind.Oil), powder = SpraySettings.DefaultFor(ElementKind.DryPowder);
            Check(water.AirLifetime != oilProfile.AirLifetime && oilProfile.AirLifetime != powder.AirLifetime &&
                water.GroundLifetime != oilProfile.GroundLifetime && oilProfile.GroundLifetime != powder.GroundLifetime,
                "Default source profiles have distinct airborne and ground lifetimes.");
            f.Field.ResetSimulation();
            f.Field.ApplyTimedWater(f.Field.Origin, .1f, .01f, 1f);
            f.Field.ApplyTimedOil(f.Field.Origin, .1f, .01f, 1f);
            f.Field.ApplyTimedDryPowder(f.Field.Origin, .1f, .01f, 1f);
            var corner = f.Field.GetCell(0,0);
            Check(corner.Wetness > 0f && corner.Oil > 0f && corner.Powder > 0f,
                "Tiny footprints at a cell corner still affect the directly hit cell.");
            f.Field.ResetSimulation(); f.Field.ApplyTimedOil(f.Field.Origin, .1f, .01f, 1f, _ => false);
            Check(f.Field.GetCell(0,0).Oil == 0f, "Hit-cell fallback never bypasses the occlusion predicate.");
            f.Field.ResetSimulation(); f.Field.Ignite(c, 1f);
            f.Field.ApplyTimedWater(f.Field.Origin, .2f, .01f, 0f);
            float afterWater = f.Field.GetCell(0,0).Intensity;
            f.Field.ApplyTimedDryPowder(f.Field.Origin, .2f, .01f, 0f);
            var instant = f.Field.GetCell(0,0);
            Check(afterWater < 1f && instant.Intensity < afterWater && instant.Wetness == 0f && instant.Powder == 0f,
                "Zero-lifetime water and powder suppress a corner hit without creating residue.");
            Check(water.MaximumRange != oilProfile.MaximumRange && oilProfile.SpreadAngle != powder.SpreadAngle && powder.ArcHeight != water.ArcHeight,
                "Water, oil, and powder profiles retain distinct range, spread, and arc contracts.");
            yield return DestroyFixture(f);
        }

        Vector3 SamplePosition(Fixture f, Vector3 aim, float age)
        {
            f.Emitter.EnableDirectionalSpray(true); f.Emitter.Tick(aim, true, .01f); f.Emitter.Advance(age); return f.Emitter.GetParticle(0).Position;
        }

        FrameRateResult SimulateFrameRate(int framesPerSecond)
        {
            SpraySettings s = Settings(ElementKind.Water); s.PacketsPerSecond = 30f;
            Fixture f = CreateFixture("frame rate " + framesPerSecond, 900f + framesPerSecond, ElementKind.Water, s, .1f);
            f.Emitter.EnableDirectionalSpray(true); f.Emitter.AutomaticSimulation = false;
            Cube(f.Root.transform, "ground", new Vector3(f.Root.transform.position.x, -.05f, 3f), new Vector3(12f, .1f, 12f), 0);
            Physics.SyncTransforms();
            float dt = 1f / framesPerSecond;
            for (int i = 0; i < framesPerSecond; i++) { f.Emitter.Tick(f.Root.transform.position + Vector3.forward * 8f, true, dt); f.Emitter.Advance(dt); }
            f.Emitter.Stop(); f.Emitter.Advance(2f);
            var result = new FrameRateResult { Emitted = f.Emitter.TotalEmitted, Impacts = f.Emitter.TotalImpacts, Wetness = f.Field.GetCell(0, 0).Wetness };
            DestroyFixtureImmediately(f);
            return result;
        }

        IEnumerator VerifyParticleVisuals()
        {
            Directory.CreateDirectory(DirectionalSprayValidation.EvidencePath);
            foreach (ElementKind kind in new[] { ElementKind.Water, ElementKind.Oil, ElementKind.DryPowder })
            {
                Fixture f = CreateFixture("particle visual " + kind, 1000f + (int)kind * 30f, kind, SpraySettings.DefaultFor(kind), .15f);
                f.Emitter.EnableDirectionalSpray(true); f.Emitter.AutomaticSimulation = false;
                SprayParticleView view = f.Emitter.gameObject.AddComponent<SprayParticleView>(); view.Initialize(f.Emitter);
                Camera camera = new GameObject("Capture camera").AddComponent<Camera>(); camera.transform.SetParent(f.Root.transform); camera.transform.position = f.Root.transform.position + new Vector3(0f, 2f, -5f); camera.transform.LookAt(f.Root.transform.position + Vector3.forward * 2.5f); camera.orthographic = true; camera.orthographicSize = 3f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.07f, .07f, .07f); camera.enabled = false;
                for (int i = 0; i < 60; i++) { f.Emitter.Tick(f.Root.transform.position + Vector3.forward * 8f, true, 1f / 60f); f.Emitter.Advance(1f / 60f); }
                yield return null; // Let SprayParticleView copy the emitter snapshots in LateUpdate.
                ParticleSystem particles = view.GetComponentInChildren<ParticleSystem>(true);
                Check(particles && particles.particleCount == f.Emitter.ActiveParticleCount,
                    kind + " particle view count matches the active directional flights.");
                Capture(camera, Path.Combine(DirectionalSprayValidation.EvidencePath, "directional-spray-" + kind.ToString().ToLowerInvariant() + ".png"));
                yield return DestroyFixture(f);
            }
        }

        Fixture CreateFixture(string label, float x, ElementKind kind, SpraySettings settings, float flowRate = 3f)
        {
            GameObject root = new GameObject("Temporary directional spray validation - " + label);
            root.transform.position = new Vector3(x, 0f, 0f); fixtures.Add(root);
            FireField field = new GameObject("Field").AddComponent<FireField>(); field.transform.SetParent(root.transform); field.Configure(new Vector3(x - .5f, 0f, 2.5f), 1, 1, 1f); field.SetIgnitionSeeds(Array.Empty<Vector2Int>()); field.ResetSimulation(); field.Simulate = false;
            ElementReaction reaction = new GameObject("Reaction").AddComponent<ElementReaction>(); reaction.transform.SetParent(root.transform); reaction.Initialize(field);
            GameObject player = new GameObject("Player"); player.transform.SetParent(root.transform); player.transform.position = new Vector3(x, 0f, 0f); player.transform.forward = Vector3.forward;
            HoseConnection hose = player.AddComponent<HoseConnection>(); SprayEmitter emitter = player.AddComponent<SprayEmitter>(); emitter.Initialize(hose, reaction); emitter.SetGroundHeight(0f);
            FluidSource source = new GameObject("Source").AddComponent<FluidSource>(); source.transform.SetParent(root.transform); source.transform.position = player.transform.position; source.Configure(kind, flowRate);
            SprayProfile profile = ScriptableObject.CreateInstance<SprayProfile>(); profiles.Add(profile); profile.Configure(settings); source.SetSprayProfile(profile); hose.TryConnect(source);
            return new Fixture { Root = root, Field = field, Reaction = reaction, Hose = hose, Emitter = emitter, Source = source, Profiles = new List<ScriptableObject> { profile } };
        }

        static SpraySettings Settings(ElementKind kind)
        {
            SpraySettings s = SpraySettings.DefaultFor(kind); s.MinimumRange = s.MaximumRange = 3f; s.AirLifetime = .35f; s.GroundLifetime = 1f; s.SpreadAngle = 0f; s.CollisionRadius = .05f; s.ImpactRadius = .45f; s.ArcHeight = 0f; return s;
        }

        static SpraySettings WallSettings(float arcHeight)
        {
            SpraySettings s = SpraySettings.DefaultFor(ElementKind.Water); s.MinimumRange = s.MaximumRange = 5.5f; s.AirLifetime = .9f;
            s.GroundLifetime = 1f; s.SpreadAngle = 0f; s.CollisionRadius = .05f; s.ImpactRadius = .45f; s.ArcHeight = arcHeight; return s;
        }

        static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale, int layer)
        { GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name; cube.transform.SetParent(parent); cube.transform.position = position; cube.transform.localScale = scale; cube.layer = layer; return cube; }
        static Transform Nozzle(Transform parent, Vector3 position)
        { GameObject anchor = new GameObject("Nozzle anchor"); anchor.transform.SetParent(parent); anchor.transform.position = position; return anchor.transform; }
        static ObjectFire CreateObjectFire(Transform parent, FireKind kind, Vector3 position)
        { GameObject o = new GameObject(kind + " ObjectFire"); o.transform.SetParent(parent); o.transform.position = position; o.AddComponent<SphereCollider>().radius = .4f; ObjectFire fire = o.AddComponent<ObjectFire>(); fire.Configure(kind, 1f); return fire; }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static void Capture(Camera camera, string path)
        {
            RenderTexture target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active; camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            Texture2D image = new Texture2D(1280, 720, TextureFormat.RGBA32, false); image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG()); camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Destroy(image);
        }
        IEnumerator DestroyFixture(Fixture fixture) { CleanupProfiles(fixture.Profiles); fixtures.Remove(fixture.Root); if (fixture.Root) Destroy(fixture.Root); yield return null; }
        void DestroyFixtureImmediately(Fixture fixture) { CleanupProfiles(fixture.Profiles); fixtures.Remove(fixture.Root); if (fixture.Root) Destroy(fixture.Root); }
        void CleanupProfiles(List<ScriptableObject> owned) { foreach (ScriptableObject profile in owned) { profiles.Remove(profile); if (profile) Destroy(profile); } }
        void Check(bool condition, string message) { if (!condition) { results.Add("FAIL " + message); throw new InvalidOperationException(message); } checks++; results.Add("PASS " + message); }

        struct FrameRateResult { public int Emitted, Impacts; public float Wetness; }
        struct Fixture { public GameObject Root; public FireField Field; public ElementReaction Reaction; public HoseConnection Hose; public SprayEmitter Emitter; public FluidSource Source; public List<ScriptableObject> Profiles; }
    }
}
