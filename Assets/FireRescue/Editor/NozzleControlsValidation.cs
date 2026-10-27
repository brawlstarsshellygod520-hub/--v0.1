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
    /// <summary>Play Mode proof for the explicit press/hold nozzle contract. It never saves scene data.</summary>
    [InitializeOnLoad]
    public static class NozzleControlsValidation
    {
        const string SessionKey = "FireRescue.NozzleControlsValidation";
        const string ReportPath = "Docs/Validation/2026-10-23-nozzle-controls/playmode.txt";
        static bool finished;
        static double readyAt;

        static NozzleControlsValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false)) return;
                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary Nozzle Controls Validation").AddComponent<NozzleControlsValidationDriver>();
            };
        }

        [MenuItem("FireRescue/Validate Nozzle Controls In Residential Scene")]
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
            string report = (success ? "NOZZLE_CONTROLS_PLAYMODE_PASS" : "NOZZLE_CONTROLS_PLAYMODE_FAIL") + "\n" + details;
            File.WriteAllText(ReportPath, report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
                else EditorApplication.ExitPlaymode();
            };
        }
    }

    public sealed class NozzleControlsValidationDriver : MonoBehaviour
    {
        const float Dt = 1f / 60f;
        const float GoalTolerance = .12f;
        const string EvidenceDirectory = "Docs/Validation/2026-10-23-nozzle-controls";
        readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        readonly List<string> results = new List<string>();
        readonly List<GameObject> fixtures = new List<GameObject>();
        float started;
        int checks;

        void Start()
        {
            Time.captureDeltaTime = Dt;
            started = Time.realtimeSinceStartup;
            routines.Push(Scenario());
        }

        void OnDestroy()
        {
            Time.captureDeltaTime = 0f;
            foreach (GameObject fixture in fixtures) if (fixture) Destroy(fixture);
        }

        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 180f) throw new TimeoutException("Nozzle controls validation timed out.");
                while (routines.Count > 0)
                {
                    IEnumerator routine = routines.Peek();
                    if (!routine.MoveNext()) { routines.Pop(); continue; }
                    if (routine.Current is IEnumerator nested) { routines.Push(nested); continue; }
                    return;
                }
                enabled = false;
                NozzleControlsValidation.Finish(true, "Pointer-command checks=" + checks + "\n" + string.Join("\n", results));
            }
            catch (Exception error)
            {
                enabled = false;
                Capture(Camera.main, "failure.png");
                NozzleControlsValidation.Finish(false, "Checks passed before failure=" + checks + "\n" + string.Join("\n", results) + "\n" + error);
            }
        }

        IEnumerator Scenario()
        {
            yield return null;
            yield return VerifyIsolatedPointerContract();
            yield return VerifyResidentialPointerRoute();
        }

        IEnumerator VerifyIsolatedPointerContract()
        {
            Fixture fixture = CreateFixture(new Vector3(40f, 0f, 40f));
            Physics.SyncTransforms();
            Vector3 aim = fixture.player.transform.position + Vector3.forward * 8f;

            Pointer(fixture, aim, hosePressed: true, hoseHeld: true);
            Check(fixture.tools.SelectedTool == FirefighterTools.Tool.Hose && !fixture.hose.IsConnected && !fixture.emitter.IsSpraying,
                "First RMB equips the hose and consumes the entire held click without connecting or spraying.");
            Pointer(fixture, aim, hoseHeld: true);
            Check(!fixture.hose.IsConnected && !fixture.emitter.IsSpraying,
                "Holding the equip click cannot advance into connection or spray.");
            Pointer(fixture, aim);
            Pointer(fixture, aim, hosePressed: true, hoseHeld: true);
            Check(fixture.hose.Source == fixture.near && !fixture.emitter.IsSpraying,
                "Second RMB connects the reachable NearbySource and still does not spray.");
            Pointer(fixture, aim);
            Pointer(fixture, aim, hosePressed: true, hoseHeld: true);
            Check(fixture.emitter.IsSpraying, "Third RMB starts hose spray only after equip and connection are complete.");
            Pointer(fixture, aim);
            Check(!fixture.emitter.IsSpraying, "Releasing RMB stops spray immediately.");

            Pointer(fixture, aim, axePressed: true, axeHeld: true);
            Check(fixture.tools.SelectedTool == FirefighterTools.Tool.Axe && !fixture.tools.IsSwinging,
                "First LMB equips the axe and holding it does not begin a swing.");
            Pointer(fixture, aim, axeHeld: true);
            Check(!fixture.tools.IsSwinging, "A held axe equip click remains inert until a new press.");
            Pointer(fixture, aim);
            GameObject axeTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
            axeTarget.name = "Temporary axe hit target";
            axeTarget.transform.SetParent(fixture.player.transform.parent);
            axeTarget.transform.position = fixture.player.transform.position + fixture.player.transform.forward * .7f + Vector3.up * .8f;
            axeTarget.transform.localScale = Vector3.one * .25f;
            Physics.SyncTransforms();
            int shortSwing = fixture.tools.SwingCount;
            int shortHits = fixture.tools.HitCount;
            Pointer(fixture, aim, axePressed: true, axeHeld: true);
            Check(fixture.tools.IsSwinging && fixture.tools.SwingCount == shortSwing + 1,
                "Second LMB starts one complete axe swing.");
            Pointer(fixture, aim);
            for (int i = 0; i < 40; i++) Pointer(fixture, aim);
            Check(!fixture.tools.IsSwinging && fixture.tools.SwingCount == shortSwing + 1 && fixture.tools.HitCount == shortHits + 1,
                "Releasing a short axe click finishes one complete stroke and its one hit without another queued attack.");

            Pointer(fixture, aim, axePressed: true, axeHeld: true);
            for (int i = 0; i < 80; i++) Pointer(fixture, aim, axeHeld: true);
            Check(fixture.tools.SwingCount >= shortSwing + 4, "Holding the armed axe repeats complete swings.");
            Destroy(axeTarget);
            yield return null;
            Pointer(fixture, aim, axePressed: true, axeHeld: true, hosePressed: true, hoseHeld: true);
            Check(fixture.tools.SelectedTool == FirefighterTools.Tool.Hose && !fixture.tools.IsSwinging && !fixture.emitter.IsSpraying,
                "Simultaneous LMB/RMB gives RMB priority, cancels the axe swing, and its first hose equip does not spray.");
            Pointer(fixture, aim);
            Pointer(fixture, aim, hosePressed: true, hoseHeld: true);
            Check(fixture.emitter.IsSpraying, "After switching back to hose, the following RMB press (not the equip press) sprays.");

            fixture.tools.Stop();
            fixture.hose.Disconnect();
            fixture.near.gameObject.SetActive(false);
            fixture.far.gameObject.SetActive(false);
            Pointer(fixture, aim, hosePressed: true, hoseHeld: true);
            Pointer(fixture, aim);
            Pointer(fixture, aim, hosePressed: true, hoseHeld: true);
            Check(!fixture.emitter.IsSpraying && !fixture.hose.IsConnected,
                "A hose with no candidate source cannot spray.");
            fixture.near.gameObject.SetActive(true);
            fixture.far.gameObject.SetActive(true);
            Physics.SyncTransforms();
            fixture.hose.TryConnect(fixture.near);

            Pointer(fixture, aim);
            Pointer(fixture, aim, hosePressed: true, hoseHeld: true);
            Check(fixture.emitter.IsSpraying, "Connected hose can be re-armed before interruption checks.");
            fixture.input.SetInputFocus(false);
            Pointer(fixture, aim, hoseHeld: true);
            fixture.input.SetInputFocus(true);
            Pointer(fixture, aim, hoseHeld: true);
            Check(!fixture.emitter.IsSpraying, "Focus loss stops and disarms hose; recovered held input needs a new press.");
            Pointer(fixture, aim, hosePressed: true, hoseHeld: true);
            fixture.input.SendMessage("OnApplicationPause", true);
            fixture.input.SendMessage("OnApplicationPause", false);
            Pointer(fixture, aim, hoseHeld: true);
            Check(!fixture.emitter.IsSpraying, "Pause stops and disarms hose; resumed held input needs a new press.");
            Pointer(fixture, aim, hosePressed: true, hoseHeld: true);
            fixture.input.enabled = false;
            fixture.input.enabled = true;
            Pointer(fixture, aim, hoseHeld: true);
            Check(!fixture.emitter.IsSpraying, "Disabling input stops and disarms hose; re-enabled held input needs a new press.");
            Pointer(fixture, aim, hosePressed: true, hoseHeld: true, dt: 0f);
            Pointer(fixture, aim, hoseHeld: true);
            Check(!fixture.emitter.IsSpraying, "Zero-delta command stops and disarms hose; a held frame cannot restart it.");

            fixture.hose.Disconnect();
            fixture.wall.SetActive(true);
            Pointer(fixture, aim);
            yield return null;
            Check(fixture.input.NearbySource == null && !fixture.highlight.HasVisibleOutline && !fixture.hose.TryConnect(fixture.near),
                "A layer-8 wall blocks the candidate, hides its object outline, and prevents connection.");
            fixture.wall.SetActive(false);
            fixture.near.transform.position = fixture.player.transform.position + Vector3.forward * 2.1f;
            Physics.SyncTransforms();
            Pointer(fixture, aim);
            yield return null;
            Check(fixture.input.NearbySource == null && !fixture.highlight.HasVisibleOutline,
                "An out-of-range source has no object outline and cannot become NearbySource.");
            fixture.near.transform.position = fixture.player.transform.position + Vector3.forward * .9f;
            fixture.far.transform.position = fixture.player.transform.position + Vector3.forward * 1.5f;
            Physics.SyncTransforms();
            Pointer(fixture, aim);
            yield return null;
            Check(fixture.input.NearbySource == fixture.near && fixture.highlight.CurrentSource == fixture.near &&
                fixture.highlight.HasVisibleOutline && OutlineColor(fixture.near, new Color(1f, .72f, .12f)),
                "The closest reachable source alone owns the Gold object outline.");
            Check(fixture.hose.TryConnect(fixture.near), "Direct fixture connection keeps the source API independent of its outline.");
            Pointer(fixture, aim);
            yield return null;
            Check(fixture.highlight.HasVisibleOutline && OutlineColor(fixture.near, new Color(.15f, .65f, 1f)),
                "A connected candidate object outline is Blue while its Visual is present.");
            fixture.near.transform.Find("Visual").gameObject.SetActive(false);
            Pointer(fixture, aim, hosePressed: true, hoseHeld: true);
            yield return null;
            Check(fixture.hose.Source == fixture.near && fixture.emitter.IsSpraying && !fixture.highlight.HasVisibleOutline,
                "Disabled source Visual does not block connection or spray, while its presentation outline is hidden.");
            yield return null;
            Check(Vector3.Distance(fixture.hoseLine.GetPosition(0), fixture.near.Port) < .001f &&
                Vector3.Distance(fixture.hoseLine.GetPosition(fixture.hoseLine.positionCount - 1), fixture.emitter.HoseInletPosition) < .001f,
                "White hose line uses source port to HoseInletPosition, not the separate nozzle tip.");

            fixture.hose.Disconnect();
            fixture.near.transform.position = fixture.player.transform.position + Vector3.forward;
            fixture.window.transform.position = fixture.player.transform.position - Vector3.forward;
            Physics.SyncTransforms();
            Check(fixture.hose.TryConnect(fixture.near) && fixture.hose.TryRouteThrough(fixture.window) &&
                fixture.hose.UsedLength <= fixture.hose.MaximumLength + .001f,
                "A fixture source-window fold with prefix below 3m routes successfully under the new default limit.");
            Vector3 limited = fixture.hose.ConstrainPosition(fixture.window.transform.position - Vector3.forward * 8f);
            Check(Mathf.Abs(Vector3.Distance(new Vector3(limited.x, 0f, limited.z), new Vector3(fixture.window.transform.position.x, 0f, fixture.window.transform.position.z)) - 1f) < .02f &&
                fixture.hose.MaximumLength == 3f,
                "Routed movement respects the remaining 3m hose length after the source-window prefix.");
            fixture.player.SetActive(false);
        }

        IEnumerator VerifyResidentialPointerRoute()
        {
            GameObject layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
            Transform player = layout ? layout.transform.Find("Gameplay/Player") : null;
            LocalPlayerInput input = player ? player.GetComponent<LocalPlayerInput>() : null;
            FirefighterMotor motor = player ? player.GetComponent<FirefighterMotor>() : null;
            FirefighterTools tools = player ? player.GetComponent<FirefighterTools>() : null;
            HoseConnection hose = player ? player.GetComponent<HoseConnection>() : null;
            SprayEmitter emitter = player ? player.GetComponent<SprayEmitter>() : null;
            Camera camera = Camera.main;
            FluidSource water = layout ? layout.GetComponentsInChildren<FluidSource>(true).FirstOrDefault(source => source.Element == ElementKind.Water) : null;
            Transform visual = player ? player.Find("Visual") : null;
            Transform nozzleMount = visual ? visual.Find("Nozzle Mount") : null;
            Transform userNozzle = nozzleMount ? nozzleMount.Find("User Nozzle Model") : null;
            Transform oldNozzle = visual ? visual.Find("Nozzle Model") : null;
            Transform axePivot = visual ? visual.Find("Axe Pivot") : null;
            SourceHighlightView sourceOutline = visual ? visual.GetComponent<SourceHighlightView>() : null;
            HoseWindow westWindow = layout ? layout.GetComponentsInChildren<HoseWindow>(true).FirstOrDefault() : null;
            Check(layout && player && input && motor && tools && hose && emitter && camera && water && visual && sourceOutline && nozzleMount && userNozzle && oldNozzle && axePivot && westWindow,
                "Residential scene contains the real pointer stack, water source, nozzle mount/model, and window route fixture.");
            Check(userNozzle.GetComponentsInChildren<Collider>(true).Length == 0 && !oldNozzle.gameObject.activeSelf,
                "User nozzle model is presentation-only and the original nozzle model remains disabled as backup.");
            input.ReadDevices = false;
            input.SetInputFocus(true);
            yield return Walk(input, motor, camera, new Vector3(5.74f, 0f, -4.8f), "Enter the front door from spawn");
            yield return Walk(input, motor, camera, new Vector3(.2f, 0f, -4.6f), "Cross the main room");
            yield return Walk(input, motor, camera, new Vector3(.2f, 0f, 2.4f), "Reach bathroom doorway");
            yield return Walk(input, motor, camera, new Vector3(-2.3f, 0f, 2.4f), "Reach toilet source without F");
            // Keep the ray beyond MinimumAimDistance and clear of the character/source body.
            Vector3 aim = new Vector3(-4.8f, .45f, 2f);
            input.TickPointerCommand(Vector2.zero, aim, false, false, false, false, true, true, Dt);
            yield return null;
            Check(tools.SelectedTool == FirefighterTools.Tool.Hose && !hose.IsConnected && userNozzle.gameObject.activeInHierarchy,
                "Real first RMB equips the nozzle at the bathroom source without using F.");
            Capture(camera, "01-near-source-gold.png");
            input.TickPointerCommand(Vector2.zero, aim, false, false, false, false, false, false, Dt);
            input.TickPointerCommand(Vector2.zero, aim, false, false, false, false, true, true, Dt);
            yield return null;
            Check(hose.Source == water && !emitter.IsSpraying, "Real second RMB connects water but does not spray.");
            Capture(camera, "02-equipped-connected-blue.png");

            yield return Walk(input, motor, camera, new Vector3(-2.3f, 0f, 2f), "Step clear of the source body for the real nozzle ray");

            GameObject target = new GameObject("Temporary real nozzle water target");
            target.transform.position = new Vector3(-4.8f, 0f, 2f);
            ObjectFire fire = target.AddComponent<ObjectFire>();
            fire.Configure(FireKind.Ordinary, 1f);
            BoxCollider targetCollider = target.AddComponent<BoxCollider>();
            targetCollider.center = Vector3.up * .45f;
            targetCollider.size = new Vector3(.45f, .9f, .45f);
            Physics.SyncTransforms();
            aim = input.ResolveAimPoint(camera.WorldToScreenPoint(targetCollider.bounds.center));
            Check(targetCollider.bounds.SqrDistance(aim) < .001f, "Game camera screen-point aim resolves to the temporary ordinary water target.");
            float before = fire.Intensity;
            water.transform.Find("Visual").gameObject.SetActive(false);
            input.TickPointerCommand(Vector2.zero, aim, false, false, false, false, false, false, Dt);
            for (int i = 0; i < 36; i++)
            {
                input.TickPointerCommand(Vector2.zero, aim, false, false, false, false, i == 0, true, Dt);
                yield return null;
            }
            Check(emitter.IsSpraying && fire.Intensity < before && !water.transform.Find("Visual").gameObject.activeSelf,
                "Real third RMB sprays water through the scene command path even when source Visual is disabled.");
            Capture(camera, "03-spraying.png");
            input.TickPointerCommand(Vector2.zero, aim, false, false, false, false, false, false, Dt);
            Check(!emitter.IsSpraying, "Real RMB release stops the stream.");
            water.transform.Find("Visual").gameObject.SetActive(true);
            Destroy(target);

            input.TickPointerCommand(Vector2.zero, aim, false, false, true, true, false, false, Dt);
            yield return null;
            Check(tools.SelectedTool == FirefighterTools.Tool.Axe && axePivot.gameObject.activeInHierarchy && !nozzleMount.gameObject.activeInHierarchy,
                "Real FirefighterToolsView shows the axe and hides the nozzle mount after axe equip.");
            input.TickPointerCommand(Vector2.zero, aim, false, false, false, false, false, false, Dt);
            input.TickPointerCommand(Vector2.zero, aim, false, false, false, false, true, true, Dt);
            yield return null;
            Check(tools.SelectedTool == FirefighterTools.Tool.Hose && !axePivot.gameObject.activeInHierarchy && nozzleMount.gameObject.activeInHierarchy,
                "Real FirefighterToolsView restores nozzle mount visibility after hose re-equip.");

            yield return WalkAgainstHoseLimit(input, motor, camera, new Vector3(-7f, 0f, 2f), "Walk into the 3m hose limit in clear bathroom space");
            yield return null;
            Check(Mathf.Abs(hose.UsedLength - 3f) < .08f && motor.transform.position.x < -6.1f && motor.transform.position.x > -6.35f &&
                input.NearbySource == null && !sourceOutline.HasVisibleOutline,
                "The 3m hose limit stops before the west wall and leaving 2m hides the source outline.");

            LineRenderer hoseLine = visual.GetComponentsInChildren<LineRenderer>(true).FirstOrDefault(line => line.name == "Hose tube");
            Check(hoseLine && hoseLine.enabled && hoseLine.startColor == Color.white &&
                Vector3.Distance(hoseLine.GetPosition(0), water.Port) < .001f &&
                Vector3.Distance(hoseLine.GetPosition(hoseLine.positionCount - 1), emitter.HoseInletPosition) < .001f &&
                Vector3.Distance(emitter.HoseInletPosition, emitter.NozzlePosition) > .01f,
                "Real white hose line terminates at HoseInlet while the nozzle tip remains a distinct spray origin.");
            float prefix = PlanarDistance(water.transform.position, westWindow.RoutePosition);
            Check(prefix > hose.MaximumLength && !hose.TryRouteThrough(westWindow),
                "The authored west window is rejected because its source-window prefix alone exceeds the new 3m hose limit.");
        }

        IEnumerator Walk(LocalPlayerInput input, FirefighterMotor motor, Camera camera, Vector3 goal, string label)
        {
            int frames = 0;
            while (PlanarDistance(motor.transform.position, goal) > GoalTolerance && frames++ < 700)
            {
                Vector3 direction = goal - motor.transform.position;
                direction.y = 0f;
                direction.Normalize();
                Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                input.TickPointerCommand(new Vector2(Vector3.Dot(direction, right), Vector3.Dot(direction, forward)), goal,
                    false, false, false, false, false, false, Dt);
                yield return null;
            }
            Check(PlanarDistance(motor.transform.position, goal) <= GoalTolerance, label + ": pointer movement reached " + motor.transform.position.ToString("F2") + ".");
        }

        IEnumerator WalkAgainstHoseLimit(LocalPlayerInput input, FirefighterMotor motor, Camera camera, Vector3 goal, string label)
        {
            // This goal is intentionally outside the 3m supply circle.  Unlike Walk(), reaching
            // it would prove a failure, so drive against the constraint and then verify it settled.
            for (int frame = 0; frame < 240; frame++)
            {
                yield return MoveToward(input, motor, camera, goal);
            }

            Vector3 constrainedPosition = motor.transform.position;
            for (int frame = 0; frame < 45; frame++)
            {
                yield return MoveToward(input, motor, camera, goal);
            }

            Check(PlanarDistance(constrainedPosition, motor.transform.position) <= .015f,
                label + ": sustained input settles at " + motor.transform.position.ToString("F2") + " instead of reaching the out-of-range goal.");
        }

        IEnumerator MoveToward(LocalPlayerInput input, FirefighterMotor motor, Camera camera, Vector3 goal)
        {
            Vector3 direction = goal - motor.transform.position;
            direction.y = 0f;
            direction.Normalize();
            Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            input.TickPointerCommand(new Vector2(Vector3.Dot(direction, right), Vector3.Dot(direction, forward)), goal,
                false, false, false, false, false, false, Dt);
            yield return null;
        }

        Fixture CreateFixture(Vector3 origin)
        {
            GameObject root = new GameObject("Temporary nozzle API fixture");
            fixtures.Add(root);
            GameObject player = new GameObject("Fixture player");
            player.transform.SetParent(root.transform); player.transform.position = origin;
            Transform anchors = Child(player.transform, "Anchors");
            Transform interaction = Child(anchors, "Interaction", Vector3.up * .35f);
            Transform nozzle = Child(anchors, "NozzleAnchor", new Vector3(0f, .9f, .45f));
            Transform inlet = Child(anchors, "HoseInlet", new Vector3(0f, .9f, -.15f));
            Transform visual = Child(player.transform, "Visual");
            GameObject nozzleModel = new GameObject("Nozzle Model");
            nozzleModel.transform.SetParent(visual, false);
            HoseConnection hose = player.AddComponent<HoseConnection>();
            hose.SetInteractionOrigin(interaction);
            SprayEmitter emitter = player.AddComponent<SprayEmitter>();
            emitter.Initialize(hose, null); emitter.SetNozzleAnchor(nozzle); emitter.SetHoseInletAnchor(inlet);
            FirefighterTools tools = player.AddComponent<FirefighterTools>(); tools.Initialize(emitter);
            LocalPlayerInput input = player.AddComponent<LocalPlayerInput>();
            FluidSource near = CreateSource(root.transform, "Nearest source", origin + Vector3.forward * .9f);
            FluidSource far = CreateSource(root.transform, "Far source", origin + Vector3.forward * 3f);
            // Fixture tests exercise command/tool state only; the scene route below owns real motor validation.
            input.Initialize(null, hose, emitter, null, new[] { near, far }, Array.Empty<RescueNpc>());
            input.SetTools(tools); input.ReadDevices = false;
            SourceHighlightView highlight = visual.gameObject.AddComponent<SourceHighlightView>(); highlight.Initialize(input, hose);
            HoseView hoseView = visual.gameObject.AddComponent<HoseView>(); hoseView.Initialize(emitter);
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Temporary source reach wall"; wall.layer = 8; wall.transform.SetParent(root.transform);
            wall.transform.position = origin + Vector3.forward * .45f + Vector3.up * .2f;
            wall.transform.localScale = new Vector3(1f, .8f, .08f); wall.SetActive(false);
            GameObject windowRoot = new GameObject("Temporary fold window"); windowRoot.transform.SetParent(root.transform);
            HoseWindow window = windowRoot.AddComponent<HoseWindow>();
            Transform route = Child(windowRoot.transform, "Route");
            Transform interactionPoint = Child(windowRoot.transform, "Interaction");
            window.Initialize(route, interactionPoint);
            return new Fixture
            {
                player = player, input = input, hose = hose, emitter = emitter, tools = tools, near = near, far = far, wall = wall,
                window = window, highlight = highlight,
                hoseLine = visual.Find("Hose tube").GetComponent<LineRenderer>()
            };
        }

        static FluidSource CreateSource(Transform parent, string name, Vector3 position)
        {
            GameObject root = new GameObject(name); root.transform.SetParent(parent); root.transform.position = position;
            FluidSource source = root.AddComponent<FluidSource>(); source.Configure(ElementKind.Water);
            Transform outlet = Child(root.transform, "Outlet", Vector3.up * .55f);
            Transform interaction = Child(root.transform, "Interaction");
            source.SetOutlet(outlet); source.SetInteractionPoint(interaction);
            Transform visual = Child(root.transform, "Visual");
            Shape(visual, "Source body", PrimitiveType.Cube, new Vector3(0, .22f, 0), new Vector3(.28f, .42f, .22f));
            Shape(visual, "Source cap", PrimitiveType.Cylinder, new Vector3(0, .49f, 0), new Vector3(.12f, .1f, .12f));
            return source;
        }

        static void Shape(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale)
        {
            GameObject shape = GameObject.CreatePrimitive(type); shape.name = name;
            shape.transform.SetParent(parent, false); shape.transform.localPosition = position; shape.transform.localScale = scale;
            Destroy(shape.GetComponent<Collider>());
        }

        static bool OutlineColor(FluidSource source, Color expected)
        {
            Transform visual = source ? source.transform.Find("Visual") : null;
            if (!visual) return false;
            Renderer[] outlines = visual.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.name.EndsWith(" outline", StringComparison.Ordinal)).ToArray();
            return outlines.Length > 0 && outlines.All(renderer => renderer.enabled && renderer.sharedMaterial && Similar(renderer.sharedMaterial.color, expected));
        }

        static Transform Child(Transform parent, string name, Vector3 position = default)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false); child.localPosition = position; return child;
        }

        static void Pointer(Fixture fixture, Vector3 aim, bool axePressed = false, bool axeHeld = false,
            bool hosePressed = false, bool hoseHeld = false, float dt = Dt)
        { fixture.input.TickPointerCommand(Vector2.zero, aim, false, false, axePressed, axeHeld, hosePressed, hoseHeld, dt); }

        void Capture(Camera camera, string name)
        {
            if (!camera) return;
            const int width = 1600, height = 900;
            Directory.CreateDirectory(EvidenceDirectory);
            RenderTexture target = new RenderTexture(width, height, 24);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(EvidenceDirectory, name), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                Destroy(image); target.Release(); Destroy(target);
            }
        }

        static bool Similar(Color a, Color b) => Mathf.Abs(a.r - b.r) < .01f && Mathf.Abs(a.g - b.g) < .01f && Mathf.Abs(a.b - b.b) < .01f;
        static float PlanarDistance(Vector3 a, Vector3 b) { a.y = b.y = 0f; return Vector3.Distance(a, b); }

        void Check(bool condition, string message)
        {
            if (!condition) { results.Add("FAIL " + message); throw new InvalidOperationException(message); }
            checks++; results.Add("PASS " + message);
        }

        sealed class Fixture
        {
            public GameObject player;
            public LocalPlayerInput input;
            public HoseConnection hose;
            public SprayEmitter emitter;
            public FirefighterTools tools;
            public FluidSource near;
            public FluidSource far;
            public GameObject wall;
            public HoseWindow window;
            public SourceHighlightView highlight;
            public LineRenderer hoseLine;
        }
    }
}
