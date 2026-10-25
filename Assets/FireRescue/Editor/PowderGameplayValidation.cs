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
    /// <summary>
    /// Exercises the persisted water-to-powder lesson fixtures through LocalPlayerInput.
    /// The script is verification-only and creates no saved scene or gameplay assets.
    /// </summary>
    [InitializeOnLoad]
    public static class PowderGameplayValidation
    {
        const string SessionKey = "FireRescue.PowderGameplayValidation";
        const string ReportPath = "Docs/Validation/2026-10-21-axe-powder/powder-playmode.txt";
        static bool finished;
        static double readyAt;

        static PowderGameplayValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false)) return;
                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary Powder Gameplay Validation").AddComponent<PowderGameplayValidationDriver>();
            };
        }

        [MenuItem("FireRescue/Validate Powder Gameplay In Residential Scene")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BeginWhenReady();
        }

        public static void BatchRun()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("BatchRun is intended for Unity batch mode.");
            BeginWhenReady();
        }

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
            catch (Exception error)
            {
                Finish(false, "Play Mode setup exception:\n" + error);
            }
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
            string report = (success ? "POWDER_GAMEPLAY_PLAYMODE_PASS" : "POWDER_GAMEPLAY_PLAYMODE_FAIL") + "\n" + details;
            File.WriteAllText(ReportPath, report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
                else EditorApplication.ExitPlaymode();
            };
        }
    }

    public sealed class PowderGameplayValidationDriver : MonoBehaviour
    {
        const float Dt = 1f / 60f;
        const float GoalTolerance = .12f;
        const string EvidenceDirectory = "Docs/Validation/2026-10-21-axe-powder";
        readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        readonly List<string> results = new List<string>();
        GameObject layout;
        FirefighterMotor motor;
        LocalPlayerInput input;
        HoseConnection hose;
        SprayEmitter emitter;
        FluidSource water;
        FluidSource powder;
        ObjectFire panFire;
        BoxCollider panCollider;
        Transform panVisual;
        Camera gameCamera;
        float started;
        int checks;

        void Start()
        {
            Time.captureDeltaTime = Dt;
            started = Time.realtimeSinceStartup;
            routines.Push(Scenario());
        }

        void OnDestroy() => Time.captureDeltaTime = 0f;

        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 150f)
                    throw new TimeoutException("Powder gameplay validation timed out.");
                while (routines.Count > 0)
                {
                    IEnumerator routine = routines.Peek();
                    if (!routine.MoveNext()) { routines.Pop(); continue; }
                    if (routine.Current is IEnumerator nested) { routines.Push(nested); continue; }
                    return;
                }
                enabled = false;
                PowderGameplayValidation.Finish(true, "Actual command-path checks=" + checks + "\n" + string.Join("\n", results));
            }
            catch (Exception error)
            {
                enabled = false;
                Capture("failure.png");
                PowderGameplayValidation.Finish(false, "Checks passed before failure=" + checks + "\n" + string.Join("\n", results) + "\n" + error);
            }
        }

        IEnumerator Scenario()
        {
            yield return null;
            SetupSceneReferences();
            VerifyObjectFireRules();
            yield return VerifyReactionBoundaries();
            yield return VerifyPlayableWaterThenPowderRoute();
        }

        void SetupSceneReferences()
        {
            layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
            Transform player = layout ? layout.transform.Find("Gameplay/Player") : null;
            Transform setup = layout ? layout.transform.Find(PowderGameplayBuilder.Marker) : null;
            Transform pan = setup ? setup.Find("OilPan") : null;
            motor = player ? player.GetComponent<FirefighterMotor>() : null;
            input = player ? player.GetComponent<LocalPlayerInput>() : null;
            hose = player ? player.GetComponent<HoseConnection>() : null;
            emitter = player ? player.GetComponent<SprayEmitter>() : null;
            panFire = pan ? pan.GetComponent<ObjectFire>() : null;
            panCollider = pan ? pan.GetComponent<BoxCollider>() : null;
            panVisual = pan ? pan.Find("Visual") : null;
            water = layout ? layout.GetComponentsInChildren<FluidSource>().FirstOrDefault(source => source.Element == ElementKind.Water) : null;
            powder = setup ? setup.GetComponentInChildren<FluidSource>() : null;
            gameCamera = Camera.main;
            Check(layout && layout.activeInHierarchy && setup && motor && input && hose && emitter && panFire && panCollider && panVisual && water && powder && gameCamera,
                "Active persisted layout contains the marker, player command path, oil pan, water source, powder source, and Game camera.");
            Check(pan.position == PowderGameplayBuilder.PanPosition && powder.transform.position == PowderGameplayBuilder.SourcePosition &&
                panFire.Kind == FireKind.CookingOil && powder.Element == ElementKind.DryPowder && Mathf.Abs(powder.FlowRate - 2f) < .001f,
                "Persisted pan and dry-powder fixture match their authored gameplay configuration.");
            Check(panCollider.center == new Vector3(0f, 1.06f, 0f) && panCollider.size == new Vector3(.64f, .42f, .64f),
                "Oil pan has the stable authored receiver hit volume, independent of Visual/Flames.");
            input.ReadDevices = false;
            input.SetInputFocus(true);
            Check(input.isActiveAndEnabled && !input.ReadDevices, "Real input adapter is active while physical-device polling is disabled.");
        }

        void VerifyObjectFireRules()
        {
            GameObject fixture = new GameObject("Temporary ObjectFire rule fixture");
            ObjectFire ordinary = fixture.AddComponent<ObjectFire>();
            ordinary.Configure(FireKind.Ordinary, 1f);
            ordinary.Receive(ElementKind.Water, 1f);
            Check(Mathf.Abs(ordinary.Intensity - .5f) < .0001f && ordinary.IsBurning, "Water lowers ordinary object fire by 0.5 per unit without prematurely extinguishing it.");
            float before = ordinary.Intensity;
            ordinary.Receive(ElementKind.Oil, 1f);
            ordinary.Receive((ElementKind)99, 1f);
            ordinary.Receive(ElementKind.Water, float.NaN);
            ordinary.Receive(ElementKind.Water, float.PositiveInfinity);
            ordinary.Receive(ElementKind.Water, 0f);
            Check(Mathf.Abs(ordinary.Intensity - before) < .0001f, "Oil, unknown elements, zero, NaN, and Infinity do not alter object fire.");
            ordinary.Receive(ElementKind.DryPowder, 1f);
            Check(!ordinary.IsBurning && ordinary.Intensity == 0f, "Dry powder reaches exactly zero and extinguishes ordinary object fire.");
            ordinary.Ignite();
            ordinary.Configure(FireKind.CookingOil, 1f);
            ordinary.Receive(ElementKind.Water, 2f);
            Check(ordinary.IsBurning && Mathf.Abs(ordinary.Intensity - 1f) < .0001f, "Water cannot suppress cooking-oil object fire.");
        }

        IEnumerator VerifyReactionBoundaries()
        {
            GameObject receiverRoot = new GameObject("Temporary receiver without FireField");
            ObjectFire receiver = receiverRoot.AddComponent<ObjectFire>();
            receiver.Configure(FireKind.CookingOil, 1f);
            BoxCollider receiverCollider = receiverRoot.AddComponent<BoxCollider>();
            GameObject reactionRoot = new GameObject("Temporary receiver reaction");
            ElementReaction noFieldReaction = reactionRoot.AddComponent<ElementReaction>();
            noFieldReaction.Initialize(null);
            noFieldReaction.Apply(ElementKind.DryPowder, receiverRoot.transform.position, Vector3.forward, 2f, receiverCollider);
            Check(!receiver.IsBurning, "ElementReaction routes dry powder to an ElementReceiver even with no FireField.");
            receiverRoot.SetActive(false);

            GameObject root = new GameObject("Temporary powder floor reaction");
            FireField field = root.AddComponent<FireField>();
            field.Configure(new Vector3(-1f, 0f, -1f), 8, 8, .25f);
            field.Simulate = false;
            ElementReaction reaction = root.AddComponent<ElementReaction>();
            reaction.Initialize(field);
            Vector3 sameSide = new Vector3(-.375f, 0f, -.125f);
            field.Ignite(sameSide, 1f);
            field.TryGetCell(sameSide, out Vector2Int sameSideCell);
            float wetBefore = field.GetCell(sameSideCell.x, sameSideCell.y).Wetness;
            reaction.Apply(ElementKind.DryPowder, sameSide, Vector3.forward, .4f);
            FireCellState sameSideState = field.GetCell(sameSideCell.x, sameSideCell.y);
            Check(sameSideState.Intensity < 1f && Mathf.Abs(sameSideState.Wetness - wetBefore) < .0001f,
                "Powder suppresses reachable floor fire without creating wetness.");

            GameObject pushTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pushTarget.name = "Temporary powder non-push target";
            Rigidbody body = pushTarget.AddComponent<Rigidbody>();
            body.useGravity = false;
            pushTarget.transform.position = new Vector3(30f, 5f, 30f);
            reaction.Apply(ElementKind.DryPowder, pushTarget.transform.position, Vector3.forward, 1f, pushTarget.GetComponent<Collider>());
            float afterPhysics = Time.fixedTime + .04f;
            while (Time.fixedTime < afterPhysics) yield return null;
            Check(body.linearVelocity.sqrMagnitude < .000001f, "Powder reaction does not add a rigidbody push impulse after physics advances.");

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Temporary thin powder wall";
            wall.layer = 8;
            wall.transform.position = new Vector3(0f, .5f, -.125f);
            wall.transform.localScale = new Vector3(.08f, 1f, .6f);
            Vector3 behindWall = new Vector3(.125f, 0f, -.125f);
            field.Ignite(behindWall, 1f);
            field.TryGetCell(behindWall, out Vector2Int behindCell);
            float behindBefore = field.GetCell(behindCell.x, behindCell.y).Intensity;
            Physics.SyncTransforms();
            reaction.Apply(ElementKind.DryPowder, new Vector3(-.125f, 0f, -.125f), Vector3.right, .5f);
            Check(Mathf.Abs(field.GetCell(behindCell.x, behindCell.y).Intensity - behindBefore) < .0001f,
                "Thin layer-8 wall blocks powder suppression from reaching fire behind it.");
            // These isolated fixtures must not block the subsequent real residential walk.
            reactionRoot.SetActive(false); root.SetActive(false); pushTarget.SetActive(false); wall.SetActive(false);
            Physics.SyncTransforms();
        }

        IEnumerator VerifyPlayableWaterThenPowderRoute()
        {
            Capture("01-before-elements.png");
            yield return Walk(new Vector3(5.74f, 0f, -4.8f), "Enter front door from outdoor spawn");
            yield return Walk(new Vector3(.2f, 0f, -4.6f), "Cross main room toward bathroom");
            yield return Walk(new Vector3(.2f, 0f, 2.4f), "Reach bathroom doorway");
            yield return Walk(new Vector3(-2.3f, 0f, 2.4f), "Reach toilet cistern water source");
            Command(water.transform.position, true, false);
            yield return null;
            Check(hose.Source == water && hose.IsConnected, "F connects to the real toilet water source.");
            float lengthAtWater = hose.UsedLength;

            yield return Walk(new Vector3(.2f, 0f, 2.4f), "Pull connected water hose through bathroom doorway");
            yield return Walk(new Vector3(1.5f, 0f, 2.4f), "Reach a clear water position before the oil pan");
            Check(hose.UsedLength > lengthAtWater + 3f, "Connected water hose visibly changes its used length along the playable route.");
            Vector3 panAim = ResolvePanAim();
            float waterBefore = panFire.Intensity;
            for (int i = 0; i < 20; i++)
            {
                Command(panAim, false, true);
                yield return null;
            }
            Check(emitter.IsSpraying && emitter.HasImpact && Mathf.Abs(panFire.Intensity - waterBefore) < .0001f,
                "F-connected water impacts the pan through the real aim path but cannot extinguish cooking oil.");
            Capture("02-water-at-oil-pan.png");

            yield return Walk(new Vector3(3.4f, 0f, 4.3f), "Walk around counter to dry-powder source");
            Command(powder.transform.position, true, false);
            yield return null;
            Check(hose.Source == powder && hose.IsConnected, "F swaps the hose from water to the real dry-powder extinguisher source.");
            float lengthAtPowder = hose.UsedLength;
            yield return Walk(new Vector3(1.5f, 0f, 2.4f), "Carry the connected powder hose back to the pan");
            Check(hose.UsedLength > lengthAtPowder + 1f, "Powder hose remains connected and changes length while returning to the pan.");
            panAim = ResolvePanAim();
            float powderBefore = panFire.Intensity;
            var sprayWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sprayWall.name = "Temporary wall blocking real powder jet"; sprayWall.layer = 8;
            sprayWall.transform.position = new Vector3(1.5f, .8f, 3.1f);
            sprayWall.transform.localScale = new Vector3(1.2f, 1.6f, .1f);
            Physics.SyncTransforms();
            for (int i = 0; i < 12; i++) { Command(panAim, false, true); yield return null; }
            Check(emitter.HasImpact && panFire.Intensity == powderBefore,
                "A solid wall stops the real powder jet before the oil pan; fire behind the wall is unchanged.");
            sprayWall.SetActive(false); Physics.SyncTransforms();
            for (int i = 0; i < 18; i++)
            {
                Command(panAim, false, true);
                yield return null;
            }
            Check(emitter.IsSpraying && emitter.HasImpact && panFire.Intensity < powderBefore - .2f && panFire.IsBurning,
                "Continuous dry-powder spray progressively lowers oil-pan fire before it is extinguished.");
            Capture("03-powder-suppressing.png");
            float releasedIntensity = panFire.Intensity;
            for (int i = 0; i < 6; i++) { Command(panAim, false, false); yield return null; }
            Check(!emitter.IsSpraying && panFire.Intensity == releasedIntensity,
                "Releasing RMB mid-fire stops suppression immediately without altering remaining intensity.");
            for (int i = 0; i < 90 && panFire.IsBurning; i++)
            {
                Command(panAim, false, true);
                yield return null;
            }
            Check(!panFire.IsBurning && panFire.Intensity == 0f, "Sustained real dry-powder spray extinguishes the oil-pan fire at zero intensity.");
            Command(panAim, false, false);
            yield return null;
            Check(!emitter.IsSpraying, "Releasing RMB stops the powder stream.");
            Capture("04-oil-pan-extinguished.png");

            panFire.Ignite(1f);
            yield return null;
            panVisual.gameObject.SetActive(false);
            for (int i = 0; i < 70 && panFire.IsBurning; i++)
            {
                Command(panAim, false, true);
                yield return null;
            }
            Check(!panFire.IsBurning && !panVisual.gameObject.activeSelf,
                "Disabling the complete replaceable pan Visual does not stop powder gameplay from extinguishing the fire.");

            yield return Walk(new Vector3(2.7f, 0f, 1.7f), "Step out of all source interaction ranges");
            Command(panAim, true, false);
            yield return null;
            Check(!hose.IsConnected, "F away from all sources disconnects the hose.");
            panFire.Ignite(1f);
            float disconnectedBefore = panFire.Intensity;
            Command(panAim, false, true);
            yield return null;
            Check(!emitter.IsSpraying && Mathf.Abs(panFire.Intensity - disconnectedBefore) < .0001f,
                "Without any connected source, RMB cannot emit powder or alter object fire.");
        }

        IEnumerator Walk(Vector3 goal, string label)
        {
            int frames = 0;
            while (PlanarDistance(motor.transform.position, goal) > GoalTolerance && frames++ < 600)
            {
                Vector3 direction = goal - motor.transform.position;
                direction.y = 0f;
                direction.Normalize();
                Vector3 forward = Vector3.ProjectOnPlane(gameCamera.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                input.TickCommand(new Vector2(Vector3.Dot(direction, right), Vector3.Dot(direction, forward)), goal,
                    false, false, false, false, Dt);
                yield return null;
            }
            Check(PlanarDistance(motor.transform.position, goal) <= GoalTolerance,
                label + ": real camera-relative input reached " + motor.transform.position.ToString("F2") + ".");
        }

        Vector3 ResolvePanAim()
        {
            Vector2 screen = gameCamera.WorldToScreenPoint(panCollider.bounds.center);
            Vector3 aim = input.ResolveAimPoint(screen);
            Check(panCollider.bounds.SqrDistance(aim) < .0001f, "Camera screen-point aim resolves to the oil pan ElementReceiver hit volume.");
            return aim;
        }

        void Command(Vector3 aim, bool interact, bool spray)
        {
            input.TickCommand(Vector2.zero, aim, interact, false, false, spray, Dt);
        }

        void Capture(string name)
        {
            if (!gameCamera) return;
            const int width = 1600;
            const int height = 900;
            Directory.CreateDirectory(EvidenceDirectory);
            RenderTexture target = new RenderTexture(width, height, 24);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture oldTarget = gameCamera.targetTexture;
            RenderTexture oldActive = RenderTexture.active;
            try
            {
                gameCamera.targetTexture = target;
                gameCamera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(EvidenceDirectory, name), image.EncodeToPNG());
            }
            finally
            {
                gameCamera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                Destroy(image);
                target.Release();
                Destroy(target);
            }
        }

        static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        void Check(bool condition, string message)
        {
            if (!condition)
            {
                results.Add("FAIL " + message);
                throw new InvalidOperationException(message);
            }
            checks++;
            results.Add("PASS " + message);
        }
    }
}
