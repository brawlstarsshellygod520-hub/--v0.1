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
    /// Verifies the window routing rule through isolated fixtures and the saved residential
    /// command path.  It creates only Play Mode objects and never authors the scene.
    /// </summary>
    [InitializeOnLoad]
    public static class WindowHoseValidation
    {
        const string SessionKey = "FireRescue.WindowHoseValidation";
        const string ReportPath = "Docs/Validation/2026-10-22-window-hose/playmode.txt";
        static bool finished;
        static double readyAt;

        static WindowHoseValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false)) return;
                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary Window Hose Validation").AddComponent<WindowHoseValidationDriver>();
            };
        }

        [MenuItem("FireRescue/Validate Window Hose In Residential Scene")]
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
            string report = (success ? "WINDOW_HOSE_PLAYMODE_PASS" : "WINDOW_HOSE_PLAYMODE_FAIL") + "\n" + details;
            File.WriteAllText(ReportPath, report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
                else EditorApplication.ExitPlaymode();
            };
        }
    }

    public sealed class WindowHoseValidationDriver : MonoBehaviour
    {
        const float Dt = 1f / 60f;
        const float GoalTolerance = .12f;
        const float MaximumLength = 13f;
        const string EvidenceDirectory = "Docs/Validation/2026-10-22-window-hose";
        readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        readonly List<string> results = new List<string>();
        readonly List<GameObject> fixtures = new List<GameObject>();

        FirefighterMotor motor;
        LocalPlayerInput input;
        HoseConnection hose;
        SprayEmitter emitter;
        Camera view;
        FluidSource water;
        HoseWindow window;
        ObjectFire targetFire;
        BoxCollider targetCollider;
        Transform windowVisual;
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
            foreach (GameObject fixture in fixtures)
                if (fixture) Destroy(fixture);
        }

        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 180f)
                    throw new TimeoutException("Window hose validation timed out.");
                while (routines.Count > 0)
                {
                    IEnumerator routine = routines.Peek();
                    if (!routine.MoveNext()) { routines.Pop(); continue; }
                    if (routine.Current is IEnumerator nested) { routines.Push(nested); continue; }
                    return;
                }
                enabled = false;
                WindowHoseValidation.Finish(true, "Actual command-path checks=" + checks + "\n" + string.Join("\n", results));
            }
            catch (Exception error)
            {
                enabled = false;
                Capture("failure.png");
                WindowHoseValidation.Finish(false, "Checks passed before failure=" + checks + "\n" + string.Join("\n", results) + "\n" + error);
            }
        }

        IEnumerator Scenario()
        {
            yield return null;
            yield return VerifyRouteApiFixtures();
            SetupSceneReferences();
            yield return VerifyRealWindowRoute();
        }

        IEnumerator VerifyRouteApiFixtures()
        {
            GameObject root = CreateFixture("Window hose API");
            Vector3 origin = new Vector3(40f, 0f, 40f); // Keep isolated reach rays clear of the authored residence.
            HoseConnection first = CreateConnection(root.transform, "First player", origin);
            HoseConnection second = CreateConnection(root.transform, "Second player", origin + new Vector3(30f, 0f, 0f));
            FluidSource firstSource = CreateSource(root.transform, "First source", origin + new Vector3(.5f, 0f, 0f));
            FluidSource secondSource = CreateSource(root.transform, "Second source", origin + new Vector3(30.5f, 0f, 0f));
            HoseWindow nearby = CreateWindow(root.transform, "Nearby window", origin + new Vector3(0f, 0f, 1f));
            HoseWindow far = CreateWindow(root.transform, "Far window", origin + new Vector3(6f, 0f, 0f));
            HoseWindow tooLong = CreateWindow(root.transform, "Long route window", origin + new Vector3(14f, 0f, 0f));
            HoseWindow otherPlayerWindow = CreateWindow(root.transform, "Second player window", origin + new Vector3(30f, 0f, 1f));
            Physics.SyncTransforms();

            Check(!first.TryRouteThrough(nearby) && first.ActiveWindow == null,
                "A window cannot route an unconnected hose.");
            Check(first.TryConnect(firstSource), "Fixture player connects before testing route limits.");
            Check(!first.TryRouteThrough(far) && first.ActiveWindow == null,
                "A window beyond the 1.8m interaction range is rejected without routing.");
            Check(first.TryRouteThrough(nearby) && first.ActiveWindow == nearby,
                "A reachable connected hose routes through its selected window.");
            first.transform.position = tooLong.transform.position;
            Check(!first.TryRouteThrough(tooLong) && first.ActiveWindow == nearby,
                "A reachable source-window-player polyline longer than 13m is rejected while preserving its prior route.");
            first.transform.position = origin;
            float routedLength = first.UsedLength;
            Vector3 clamped = first.ConstrainPosition(origin + new Vector3(20f, 4f, 1f));
            Check(routedLength > PlanarDistance(first.Source.transform.position, first.transform.position) + .5f &&
                Mathf.Abs(clamped.y - 4f) < .001f &&
                PlanarDistance(nearby.RoutePosition, clamped) <= MaximumLength - PlanarDistance(first.Source.transform.position, nearby.RoutePosition) + .001f,
                "Routed UsedLength uses the XZ polyline and constrains around the final window while preserving requested Y.");

            Check(second.TryConnect(secondSource) && second.TryRouteThrough(otherPlayerWindow) &&
                first.ActiveWindow == nearby && second.ActiveWindow == otherPlayerWindow,
                "Each player owns an independent active window route.");
            FluidSource replacement = CreateSource(root.transform, "Replacement source", origin + new Vector3(-.5f, 0f, 0f));
            Check(first.TryConnect(replacement) && first.ActiveWindow == null && second.ActiveWindow == otherPlayerWindow,
                "Changing source clears only that player's route.");
            first.TryRouteThrough(nearby);
            first.Disconnect();
            Check(!first.IsConnected && first.ActiveWindow == null && second.ActiveWindow == otherPlayerWindow,
                "Disconnect clears only that player's route.");

            Check(first.TryConnect(firstSource) && first.TryRouteThrough(nearby), "Fixture route is restored before disabled-window fallback.");
            nearby.gameObject.SetActive(false);
            Check(first.ActiveWindow == null && Mathf.Abs(first.UsedLength - Vector3.Distance(first.transform.position, firstSource.transform.position)) < .001f,
                "A disabled window falls back to the established direct hose length.");
            nearby.gameObject.SetActive(true);
            yield return null;
            Check(first.TryRouteThrough(nearby) && first.ActiveWindow == nearby,
                "A re-enabled window can be selected again before destruction fallback is tested.");
            Destroy(nearby.gameObject);
            yield return null;
            Check(first.ActiveWindow == null && Mathf.Abs(first.UsedLength - Vector3.Distance(first.transform.position, firstSource.transform.position)) < .001f,
                "A deleted window also falls back to the established direct hose length.");
            root.SetActive(false);
        }

        void SetupSceneReferences()
        {
            GameObject layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
            Transform fixture = layout ? layout.transform.Find("Window Hose Test") : null;
            Transform player = layout ? layout.transform.Find("Gameplay/Player") : null;
            Transform windowRoot = fixture ? fixture.Find("Bathroom Hose Window") : null;
            Transform fireRoot = fixture ? fixture.Find("Outdoor water target") : null;
            motor = player ? player.GetComponent<FirefighterMotor>() : null;
            input = player ? player.GetComponent<LocalPlayerInput>() : null;
            hose = player ? player.GetComponent<HoseConnection>() : null;
            emitter = player ? player.GetComponent<SprayEmitter>() : null;
            view = Camera.main;
            water = layout ? layout.GetComponentsInChildren<FluidSource>().FirstOrDefault(source => source.Element == ElementKind.Water) : null;
            window = windowRoot ? windowRoot.GetComponent<HoseWindow>() : null;
            targetFire = fireRoot ? fireRoot.GetComponent<ObjectFire>() : null;
            targetCollider = fireRoot ? fireRoot.GetComponent<BoxCollider>() : null;
            windowVisual = windowRoot ? windowRoot.Find("Visual") : null;
            Check(layout && layout.activeInHierarchy && fixture && motor && input && hose && emitter && view && water && window && targetFire && targetCollider && windowVisual,
                "Saved residential layout contains the window fixture, real player command stack, water source, exterior fire, and Game camera.");
            Check(Approximately(window.transform.position, new Vector3(-8.07f, 0f, 2.45f)) &&
                Approximately(window.RoutePosition, new Vector3(-8.07f, 1.15f, 2.45f)) &&
                Approximately(targetFire.transform.position, new Vector3(-10.2f, 0f, 5.5f)) && targetFire.Kind == FireKind.Ordinary &&
                targetCollider.center == new Vector3(0f, .45f, 0f) && targetCollider.size == new Vector3(.7f, .9f, .7f),
                "Saved fixture positions, window anchors, ordinary target fire, and stable target collider match the window-hose contract.");
            input.ReadDevices = false;
            input.SetInputFocus(true);
        }

        IEnumerator VerifyRealWindowRoute()
        {
            yield return Walk(new Vector3(5.74f, 0f, -4.8f), "Enter the front door from the outdoor spawn");
            yield return Walk(new Vector3(.2f, 0f, -4.6f), "Cross the main room toward the bathroom");
            yield return Walk(new Vector3(.2f, 0f, 2.4f), "Reach the bathroom doorway");
            yield return Walk(new Vector3(-2.3f, 0f, 2.4f), "Reach the real toilet water source");
            Command(Vector2.zero, water.transform.position, true, false);
            yield return null;
            Check(hose.Source == water && hose.IsConnected && hose.ActiveWindow == null,
                "F connects water through the real LocalPlayerInput command path without an initial route.");

            yield return Walk(new Vector3(.2f, 0f, 2.4f), "Return through the bathroom doorway with water connected");
            yield return Walk(new Vector3(2.3f, 0f, 2.65f), "Approach the south side of the kitchen counter");
            yield return Walk(new Vector3(5f, 0f, 2.65f), "Move east of the counter and dry-powder source");
            yield return Walk(new Vector3(4.99f, 0f, 6.94f), "Enter the rear room through its existing opening");
            yield return Walk(new Vector3(2.53f, 0f, 7.2f), "Reach the rear exterior door approach");
            yield return Walk(new Vector3(.13f, 0f, 7.2f), "Pass through the rear door into the courtyard");
            yield return Walk(new Vector3(-8.9f, 0f, 7.2f), "Cross the open courtyard south of the annex to the west exterior");
            yield return Walk(new Vector3(-8.9f, 0f, 2.45f), "Reach the exterior side of the bathroom window");
            Check(hose.IsConnected && hose.UsedLength <= MaximumLength + .02f,
                "The direct hose remains connected while following the real exterior route to the window.");

            Command(Vector2.zero, window.transform.position, true, false);
            yield return null;
            Check(hose.ActiveWindow == window, "F at the window fixes the connected hose through that window.");
            float expectedAtWindow = PlanarDistance(water.transform.position, window.RoutePosition) +
                PlanarDistance(window.RoutePosition, motor.transform.position);
            Check(Mathf.Abs(hose.UsedLength - expectedAtWindow) < .001f,
                "At the nearly collinear window position, UsedLength exactly equals the routed XZ source-window-player polyline.");
            yield return null;
            VerifyRoutedLineRenderer();
            Capture("01-window-fixed.png");

            Command(Vector2.zero, window.transform.position, true, false);
            yield return null;
            Check(hose.IsConnected && hose.ActiveWindow == null, "A second F at the active window releases only the route.");
            Command(Vector2.zero, window.transform.position, true, false);
            yield return null;
            Check(hose.ActiveWindow == window, "F fixes the same window again after release.");

            windowVisual.gameObject.SetActive(false);
            Command(Vector2.zero, window.transform.position, true, false);
            yield return null;
            Command(Vector2.zero, window.transform.position, true, false);
            yield return null;
            Check(hose.ActiveWindow == window, "Disabling the replaceable window Visual does not prevent clearing and fixing the route.");
            Vector3 visualClamp = hose.ConstrainPosition(window.RoutePosition + new Vector3(100f, 2f, 0f));
            Check(PlanarDistance(window.RoutePosition, visualClamp) <= MaximumLength - PlanarDistance(water.transform.position, window.RoutePosition) + .001f,
                "The hidden-window route still applies its remaining hose-length limit.");
            windowVisual.gameObject.SetActive(true);

            Vector3 wallStart = motor.transform.position;
            for (int i = 0; i < 90; i++)
            {
                Command(ViewInput(Vector3.right), motor.transform.position + Vector3.right * 4f, false, false);
                yield return null;
            }
            Check(motor.transform.position.x < wallStart.x + .45f,
                "Existing bathroom wall collision prevents the player from walking directly through the window toward +X.");
            yield return Walk(new Vector3(-8.9f, 0f, 2.45f), "Step back from the solid window wall without teleporting");

            yield return Walk(new Vector3(-8.9f, 0f, 5.5f), "Move outside with the route fixed through the window");
            float directOutside = Vector3.Distance(water.transform.position, motor.transform.position);
            Check(hose.UsedLength > directOutside + 1f && hose.UsedLength <= MaximumLength + .02f,
                "Outside movement keeps the routed polyline materially longer than the direct line while respecting 13m.");
            yield return null;
            VerifyRoutedLineRenderer();
            Capture("02-outside-routed.png");

            Vector2 fireScreen = view.WorldToScreenPoint(targetCollider.bounds.center);
            Vector3 fireAim = input.ResolveAimPoint(fireScreen);
            Check(targetCollider.bounds.SqrDistance(fireAim) < .001f,
                "The Game camera screen-point aim resolves to the exterior fire's stable receiver collider.");
            for (int i = 0; i < 150 && targetFire.IsBurning; i++)
            {
                Command(Vector2.zero, fireAim, false, true);
                yield return null;
            }
            Command(Vector2.zero, fireAim, false, false);
            Check(!targetFire.IsBurning && !emitter.IsSpraying,
                "RMB through the real camera screen-aim and input command path carries routed water outside and extinguishes the ordinary target; release stops it.");
            Capture("03-outdoor-fire-extinguished.png");

            yield return WalkUntilLimited(new Vector3(-8.9f, 0f, 12f), "Walk the open exterior toward z=12");
            float remainingRadius = MaximumLength - PlanarDistance(water.transform.position, window.RoutePosition);
            Check(motor.transform.position.z > 9f && motor.transform.position.z < 11f &&
                Mathf.Abs(PlanarDistance(window.RoutePosition, motor.transform.position) - remainingRadius) < .05f &&
                Mathf.Abs(hose.UsedLength - MaximumLength) < .05f,
                "Open exterior travel reaches the routed 13m hose circle, rather than an exterior boundary collider.");
            Command(Vector2.zero, motor.transform.position + Vector3.forward, true, false);
            yield return null;
            Check(!hose.IsConnected && hose.ActiveWindow == null,
                "F away from sources and windows disconnects the hose and clears its route.");
        }

        IEnumerator Walk(Vector3 goal, string label)
        {
            int frames = 0;
            while (PlanarDistance(motor.transform.position, goal) > GoalTolerance && frames++ < 700)
            {
                Vector3 direction = goal - motor.transform.position;
                direction.y = 0f;
                Command(ViewInput(direction), goal, false, false);
                yield return null;
            }
            Check(PlanarDistance(motor.transform.position, goal) <= GoalTolerance,
                label + ": real LocalPlayerInput reached " + motor.transform.position.ToString("F2") + ".");
        }

        IEnumerator WalkUntilLimited(Vector3 goal, string label)
        {
            float previous = float.MinValue;
            int stalledFrames = 0;
            for (int frames = 0; frames < 700; frames++)
            {
                Vector3 direction = goal - motor.transform.position;
                direction.y = 0f;
                Command(ViewInput(direction), goal, false, false);
                yield return null;
                float progress = Vector3.Dot(motor.transform.position, direction.normalized);
                stalledFrames = progress <= previous + .0005f ? stalledFrames + 1 : 0;
                previous = progress;
                if (stalledFrames > 25) break;
            }
            Check(PlanarDistance(motor.transform.position, goal) > 1f,
                label + ": movement was constrained before the requested open-exterior goal at " + motor.transform.position.ToString("F2") + ".");
        }

        void VerifyRoutedLineRenderer()
        {
            LineRenderer cable = hose.GetComponentsInChildren<LineRenderer>(true).FirstOrDefault(line => line.name == "Hose tube");
            Check(cable && cable.positionCount >= 35, "The routed HoseView uses the expanded two-segment line resolution.");
            Vector3[] points = new Vector3[cable.positionCount];
            cable.GetPositions(points);
            Check(points.Any(point => (point - window.RoutePosition).sqrMagnitude < .0001f),
                "The rendered hose line contains the exact HoseWindow.RoutePosition vertex.");
            bool clear = true;
            for (int i = 1; i < points.Length; i++)
            {
                if (Physics.Linecast(points[i - 1], points[i], out RaycastHit hit, 1 << 8, QueryTriggerInteraction.Ignore) &&
                    PlanarDistance(hit.point, window.RoutePosition) < 1.4f) clear = false;
                // This fixture has an X-normal frame with a .9m sill top and .095m cable width.
                if (Mathf.Abs(points[i].x - window.RoutePosition.x) < .2f &&
                    Mathf.Abs(points[i].z - window.RoutePosition.z) < .6f && points[i].y < .95f) clear = false;
            }
            Check(clear, "The rendered cable clears the solid wall and window sill near the fixed route point.");
        }

        Vector2 ViewInput(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            worldDirection.Normalize();
            Vector3 forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return new Vector2(Vector3.Dot(worldDirection, right), Vector3.Dot(worldDirection, forward));
        }

        void Command(Vector2 movement, Vector3 aim, bool interact, bool spray)
        {
            input.TickCommand(movement, aim, interact, false, false, spray, Dt);
        }

        GameObject CreateFixture(string name)
        {
            GameObject root = new GameObject("Temporary " + name);
            fixtures.Add(root);
            return root;
        }

        static HoseConnection CreateConnection(Transform parent, string name, Vector3 position)
        {
            GameObject player = new GameObject(name);
            player.transform.SetParent(parent, false);
            player.transform.position = position;
            return player.AddComponent<HoseConnection>();
        }

        static FluidSource CreateSource(Transform parent, string name, Vector3 position)
        {
            GameObject sourceObject = new GameObject(name);
            sourceObject.transform.SetParent(parent, false);
            sourceObject.transform.position = position;
            FluidSource source = sourceObject.AddComponent<FluidSource>();
            source.Configure(ElementKind.Water);
            return source;
        }

        static HoseWindow CreateWindow(Transform parent, string name, Vector3 position)
        {
            GameObject windowObject = new GameObject(name);
            windowObject.transform.SetParent(parent, false);
            windowObject.transform.position = position;
            Transform route = new GameObject("Route").transform;
            route.SetParent(windowObject.transform, false);
            Transform interaction = new GameObject("Interaction").transform;
            interaction.SetParent(windowObject.transform, false);
            HoseWindow window = windowObject.AddComponent<HoseWindow>();
            window.Initialize(route, interaction);
            return window;
        }

        void Capture(string name)
        {
            if (!view) return;
            Directory.CreateDirectory(EvidenceDirectory);
            RenderTexture target = new RenderTexture(1600, 900, 24);
            Texture2D image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            RenderTexture previousTarget = view.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                view.targetTexture = target;
                view.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(EvidenceDirectory, name), image.EncodeToPNG());
            }
            finally
            {
                view.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
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

        static bool Approximately(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < .000001f;

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
