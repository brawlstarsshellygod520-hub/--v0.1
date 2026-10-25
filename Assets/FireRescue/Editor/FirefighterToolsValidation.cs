using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FireRescue.Editor
{
    // Disposable command-path coverage for the tutorial tools.  It deliberately creates
    // no scene or prefab assets: GreyboxPrefabBuilder supplies the already-imported parts.
    [InitializeOnLoad]
    public static class FirefighterToolsValidation
    {
        const string Key = "FireRescue.FirefighterToolsValidation";
        const string ReportPath = "Docs/Validation/2026-10-19-greybox-step4/tools-playmode.txt";
        static bool finished;
        static double batchReadyAt;

        static FirefighterToolsValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary Firefighter Tools Validation").AddComponent<FirefighterToolsValidationDriver>();
            };
        }

        [MenuItem("消防 Prototype/驗證消防員工具命令")]
        public static void Run() => Begin(false);

        // Kept public for -executeMethod.  Import/compile must settle before the disposable
        // scene asks GreyboxPrefabBuilder to instantiate its nine pre-existing prefabs.
        public static void BatchRun()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("BatchRun is only for Unity batchmode.");
            batchReadyAt = EditorApplication.timeSinceStartup + 2d;
            EditorApplication.update += BeginBatch;
        }

        static void BeginBatch()
        {
            if (EditorApplication.timeSinceStartup < batchReadyAt || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= BeginBatch;
            try { Begin(true); }
            catch (Exception error)
            {
                Debug.LogException(error);
                Finish(false, "Batch setup exception:\n" + error);
            }
        }

        static void Begin(bool batch)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before running tool validation.");
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Key, true);
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
            Application.logMessageReceived -= OnLog;
            SessionState.SetBool(Key, false);
            string report = (success ? "FIREFIGHTER_TOOLS_PLAYMODE_PASS" : "FIREFIGHTER_TOOLS_PLAYMODE_FAIL") + "\n" + details;
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
                else EditorApplication.ExitPlaymode();
            };
        }
    }

    public sealed class FirefighterToolsValidationDriver : MonoBehaviour
    {
        const float Dt = 1f / 60f;
        readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        readonly List<string> results = new List<string>();
        GameObject assembly;
        FirefighterMotor motor;
        HoseConnection hose;
        SprayEmitter emitter;
        FirefighterTools tools;
        LocalPlayerInput input;
        Camera view;
        FluidSource water;
        FluidSource oil;
        int checks;
        float started;

        void Start()
        {
            Time.captureDeltaTime = Dt;
            started = Time.realtimeSinceStartup;
            routines.Push(Scenario());
        }

        void OnDestroy()
        {
            Time.captureDeltaTime = 0f;
            if (assembly) Destroy(assembly);
            if (view) Destroy(view.gameObject);
        }

        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 45f) throw new TimeoutException("Tool validation timed out.");
                while (routines.Count > 0)
                {
                    IEnumerator routine = routines.Peek();
                    if (!routine.MoveNext()) { routines.Pop(); continue; }
                    if (routine.Current is IEnumerator nested) { routines.Push(nested); continue; }
                    return;
                }
            }
            catch (Exception error)
            {
                enabled = false;
                FirefighterToolsValidation.Finish(false, "Actual checks passed before failure: " + checks + "\n" + string.Join("\n", results) + "\n" + error);
            }
        }

        IEnumerator Scenario()
        {
            assembly = GreyboxPrefabBuilder.CreateTestAssembly();
            yield return null;
            SetupCommands();
            yield return VerifyCameraRelativeCommands();
            yield return VerifyAxeAndHoseCommands();
            yield return VerifyAttachReachAndWallExclusion();
            yield return VerifyStopsAndPresentationIndependence();
            enabled = false;
            FirefighterToolsValidation.Finish(true, "Actual Play Mode checks: " + checks + "\n" + string.Join("\n", results));
        }

        void SetupCommands()
        {
            CreateSupportGround();
            motor = assembly.GetComponentInChildren<FirefighterMotor>();
            hose = motor ? motor.GetComponent<HoseConnection>() : null;
            emitter = motor ? motor.GetComponent<SprayEmitter>() : null;
            Check(motor && hose && emitter, "Test assembly exposes the real player motor, hose, and emitter.");
            tools = motor.gameObject.AddComponent<FirefighterTools>();
            tools.Initialize(emitter);
            input = motor.gameObject.AddComponent<LocalPlayerInput>();
            view = new GameObject("Temporary command camera").AddComponent<Camera>();
            view.transform.rotation = Quaternion.LookRotation(new Vector3(.72f, -.58f, .38f));
            water = FindSource(ElementKind.Water);
            oil = FindSource(ElementKind.Oil);
            input.Initialize(motor, hose, emitter, view, new[] { water, oil }, Array.Empty<RescueNpc>());
            input.SetTools(tools);
            input.ReadDevices = false;
            input.SetInputFocus(true);
            Check(input.enabled && !input.ReadDevices, "Command input remains enabled while physical-device reads are disabled.");
        }

        IEnumerator VerifyCameraRelativeCommands()
        {
            Vector3 forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(view.transform.right, Vector3.up).normalized;
            CheckDirection(LocalPlayerInput.ViewRelativeDirection(Vector2.up, view.transform), forward, "W is screen-up.");
            CheckDirection(LocalPlayerInput.ViewRelativeDirection(Vector2.down, view.transform), -forward, "S is screen-down.");
            CheckDirection(LocalPlayerInput.ViewRelativeDirection(Vector2.left, view.transform), -right, "A is screen-left.");
            CheckDirection(LocalPlayerInput.ViewRelativeDirection(Vector2.right, view.transform), right, "D is screen-right.");
            Vector3 diagonal = LocalPlayerInput.ViewRelativeDirection(Vector2.one, view.transform);
            CheckDirection(diagonal, (forward + right).normalized, "W+D remains camera-relative.");
            Check(diagonal.magnitude <= 1.0001f && Mathf.Abs(diagonal.magnitude - 1f) < .001f,
                "Diagonal command is normalized and no faster than cardinal input.");
            Teleport(new Vector3(12f, .05f, 10f), Vector3.forward);
            Vector3 cardinalStart = motor.transform.position;
            for (int i = 0; i < 90; i++) MoveCommand(Vector2.up);
            Vector3 cardinalTravel = Planar(motor.transform.position - cardinalStart);
            Check(cardinalTravel.magnitude > 5f && Vector3.Dot(cardinalTravel.normalized, forward) > .995f,
                "Actual TickCommand W displacement follows screen-up on the supporting ground.");
            for (int i = 0; i < 30; i++) MoveCommand(Vector2.zero);
            Teleport(new Vector3(22f, .05f, 10f), Vector3.forward);
            Vector3 diagonalStart = motor.transform.position;
            for (int i = 0; i < 90; i++) MoveCommand(Vector2.one);
            Vector3 diagonalTravel = Planar(motor.transform.position - diagonalStart);
            Check(Vector3.Dot(diagonalTravel.normalized, diagonal) > .995f &&
                Mathf.Abs(diagonalTravel.magnitude - cardinalTravel.magnitude) < .08f,
                "Actual TickCommand diagonal movement is camera-relative and no faster than W.");
            for (int i = 0; i < 30; i++) MoveCommand(Vector2.zero);
            yield return null;
        }

        IEnumerator VerifyAxeAndHoseCommands()
        {
            Transform fixture = NewFixture("axe ordering");
            Teleport(new Vector3(10f, .05f, 10f), Vector3.forward);
            BoxCollider owner = Block(fixture, "Owner collider (must be skipped)", motor.transform.position + Vector3.forward * .15f, new Vector3(.25f, .5f, .25f));
            owner.transform.SetParent(motor.transform, true);
            BoxCollider wall = Block(fixture, "Nearest axe wall", motor.transform.position + Vector3.forward * .7f + Vector3.up * .75f, new Vector3(.18f, 1.5f, 1f));
            Block(fixture, "Axe target behind wall", motor.transform.position + Vector3.forward * 1.08f + Vector3.up * .75f, new Vector3(.2f, 1.2f, .4f));
            Physics.SyncTransforms();
            hose.Disconnect();
            int beforeHits = tools.HitCount;
            int beforeSwings = tools.SwingCount;
            for (int i = 0; i < 65; i++) Command(true, false);
            Check(tools.SelectedTool == FirefighterTools.Tool.Axe && tools.IsSwinging && tools.SwingCount == beforeSwings + 2,
                "Left command repeatedly drives exactly two full axe swing cycles.");
            Check(tools.HitCount == beforeHits + 2 && tools.LastHit.collider == wall,
                "Each axe cycle skips its owner and stops at the nearest wall instead of hitting through it.");

            Command(false, true);
            Check(tools.SelectedTool == FirefighterTools.Tool.Hose && !tools.IsSwinging && !emitter.IsSpraying,
                "Right command equips the hose but cannot spray without a connected source.");
            Teleport(water.transform.position + Vector3.right * 1.15f, Vector3.forward);
            Check(hose.TryConnect(water), "Test connects the real water source before hose-command checks.");
            Command(false, true);
            Check(tools.SelectedTool == FirefighterTools.Tool.Hose && emitter.IsSpraying && !tools.IsSwinging,
                "Connected right command sprays without swinging the axe.");
            Command(true, false);
            Check(tools.SelectedTool == FirefighterTools.Tool.Axe && tools.IsSwinging && !emitter.IsSpraying,
                "Switching from hose to axe immediately stops spray.");
            Command(false, true);
            Check(tools.SelectedTool == FirefighterTools.Tool.Hose && emitter.IsSpraying && !tools.IsSwinging,
                "Switching from axe to connected hose is mutually exclusive and sprays.");
            Command(true, true);
            Check(tools.SelectedTool == FirefighterTools.Tool.Hose && emitter.IsSpraying && !tools.IsSwinging,
                "Simultaneous holds deterministically give right-click hose priority.");
            Command(false, false);
            Check(!tools.IsSwinging && !emitter.IsSpraying && tools.SelectedTool == FirefighterTools.Tool.Hose,
                "Release stops both actions while retaining the selected tool.");
            Destroy(owner.gameObject);
            yield return null;
        }

        IEnumerator VerifyAttachReachAndWallExclusion()
        {
            hose.Disconnect();
            Teleport(water.transform.position + Vector3.right * 1.15f, Vector3.forward);
            Command(false, false, true);
            Check(hose.Source == water, "F command attaches at the authored water-source coordinates.");
            Teleport(oil.transform.position + Vector3.left * 1.1f, Vector3.forward);
            Command(false, false, true);
            Check(hose.Source == oil, "F command swaps to the authored oil-source coordinates.");
            Command(false, false, true);
            Check(!hose.IsConnected, "F command detaches when used again at the connected source.");

            Teleport(water.transform.position + Vector3.forward * 1.1f, Vector3.right);
            Check(hose.TryConnect(water), "Reach fixture reconnects water.");
            for (int i = 0; i < 300; i++) motor.Move(Vector3.right, .05f);
            Vector3 flat = motor.transform.position - water.transform.position;
            flat.y = 0;
            Check(flat.magnitude <= hose.MaximumLength + .06f && flat.magnitude >= hose.MaximumLength - .25f,
                "Actual FirefighterMotor movement reaches but never exceeds the 13m hose constraint.");

            Transform fixture = NewFixture("blocked source");
            FluidSource blocked = Source(fixture, "Blocked command source", new Vector3(40f, 0f, 0f));
            Block(fixture, "Command reach wall", new Vector3(39.25f, .36f, 0f), new Vector3(.12f, .72f, 1.2f));
            input.Initialize(motor, hose, emitter, view, new[] { blocked }, Array.Empty<RescueNpc>());
            hose.Disconnect();
            Teleport(new Vector3(38.5f, .05f, 0f), Vector3.right);
            Physics.SyncTransforms();
            Command(false, false, true);
            Check(!hose.IsConnected, "F command excludes a source behind the temporary low wall.");
            input.Initialize(motor, hose, emitter, view, new[] { water, oil }, Array.Empty<RescueNpc>());
            yield return null;
        }

        IEnumerator VerifyStopsAndPresentationIndependence()
        {
            Teleport(water.transform.position + Vector3.right * 1.1f, Vector3.forward);
            hose.Disconnect();
            Check(hose.TryConnect(water), "Stop checks reconnect water.");
            Command(true, false);
            input.SetInputFocus(false);
            Check(!tools.IsSwinging && !emitter.IsSpraying, "Focus loss immediately stops tool actions.");
            input.SetInputFocus(true);
            Command(false, true);
            input.SendMessage("OnApplicationPause", true, SendMessageOptions.RequireReceiver);
            Check(!tools.IsSwinging && !emitter.IsSpraying, "Application pause immediately stops tool actions.");
            Command(true, false);
            Check(!tools.IsSwinging && !emitter.IsSpraying, "Paused command input cannot restart tools.");
            input.SendMessage("OnApplicationPause", false, SendMessageOptions.RequireReceiver);
            input.enabled = false;
            Check(!tools.IsSwinging && !emitter.IsSpraying, "Disabling command input immediately stops tool actions.");
            input.enabled = true;
            input.SetInputFocus(true);
            Command(true, false);
            Command(true, false, false, 0f);
            Check(!tools.IsSwinging && !emitter.IsSpraying, "Zero-delta command stops tool actions.");
            Command(true, false);
            Check(tools.IsSwinging, "Re-enabled focused command input resumes tool actions.");

            input.enabled = false;
            input.TickCommand(Vector2.zero, motor.transform.position + motor.transform.forward * 3f, false, false, true, Dt);
            Check(emitter.IsSpraying, "Legacy six-argument TickCommand remains callable while input is disabled.");
            emitter.Stop();
            input.enabled = true;
            input.SetInputFocus(true);

            GameObject axeVisual = new GameObject("Temporary axe visual");
            GameObject hoseVisual = new GameObject("Temporary hose visual");
            axeVisual.transform.SetParent(motor.transform, false);
            hoseVisual.transform.SetParent(motor.transform, false);
            FirefighterToolsView toolView = motor.gameObject.AddComponent<FirefighterToolsView>();
            toolView.Initialize(tools, emitter, axeVisual.transform, hoseVisual, hoseVisual);
            Command(false, false);
            for (int i = 0; i < 15; i++) Command(true, false);
            yield return null;
            yield return null;
            Check(axeVisual.activeSelf && !hoseVisual.activeSelf && (axeVisual.transform.localRotation * Vector3.up).z > .8f,
                "Visible axe swings forward in the strike direction; hose model is hidden.");
            Command(false, true);
            yield return null;
            yield return null;
            Check(!axeVisual.activeSelf && hoseVisual.activeSelf, "Tool view switches from axe to hose.");
            Command(false, false);
            toolView.enabled = false;
            axeVisual.SetActive(false);
            hoseVisual.SetActive(false);
            BoxCollider visualIndependentTarget = Block(NewFixture("visual independent axe"), "Hidden-view axe target",
                motor.transform.position + motor.transform.forward * .7f + Vector3.up * .75f, new Vector3(.2f, 1.2f, .5f));
            Physics.SyncTransforms();
            int hitsBeforeVisualDestroy = tools.HitCount;
            for (int i = 0; i < 22; i++) Command(true, false);
            Destroy(axeVisual);
            Destroy(hoseVisual);
            yield return null;
            Check(tools.IsSwinging && tools.HitCount == hitsBeforeVisualDestroy + 1 && tools.LastHit.collider == visualIndependentTarget,
                "Hidden or destroyed presentation visuals do not prevent core axe swings or hits.");
            Command(false, true);
            Check(emitter.IsSpraying && !tools.IsSwinging, "Destroyed presentation visuals do not prevent core hose spray.");
        }

        void Command(bool axeHeld, bool sprayHeld, bool interact = false, float dt = Dt)
        {
            input.TickCommand(Vector2.zero, motor.transform.position + motor.transform.forward * 3f,
                interact, false, axeHeld, sprayHeld, dt);
        }

        void MoveCommand(Vector2 movement) => input.TickCommand(movement,
            motor.transform.position + motor.transform.forward * 3f, false, false, false, false, Dt);

        void CreateSupportGround()
        {
            GameObject ground = new GameObject("Temporary validation support ground");
            ground.transform.SetParent(assembly.transform);
            ground.transform.position = new Vector3(20f, -.15f, 5f);
            ground.layer = 0;
            BoxCollider collider = ground.AddComponent<BoxCollider>();
            collider.size = new Vector3(70f, .3f, 35f);
        }

        Transform NewFixture(string name)
        {
            GameObject root = new GameObject("Temporary tool fixture - " + name);
            root.transform.SetParent(assembly.transform);
            return root.transform;
        }

        static BoxCollider Block(Transform parent, string name, Vector3 position, Vector3 size)
        {
            GameObject block = new GameObject(name);
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.layer = 8;
            BoxCollider collider = block.AddComponent<BoxCollider>();
            collider.size = size;
            return collider;
        }

        static FluidSource Source(Transform parent, string name, Vector3 position)
        {
            GameObject source = new GameObject(name);
            source.transform.SetParent(parent);
            source.transform.position = position;
            source.layer = 8;
            BoxCollider collider = source.AddComponent<BoxCollider>();
            collider.center = Vector3.up * .35f;
            collider.size = new Vector3(.35f, .7f, .35f);
            FluidSource result = source.AddComponent<FluidSource>();
            result.Configure(ElementKind.Water);
            return result;
        }

        FluidSource FindSource(ElementKind kind)
        {
            foreach (FluidSource source in assembly.GetComponentsInChildren<FluidSource>())
                if (source.Element == kind) return source;
            throw new InvalidOperationException("Missing " + kind + " source in test assembly.");
        }

        void Teleport(Vector3 position, Vector3 forward)
        {
            CharacterController controller = motor.GetComponent<CharacterController>();
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            motor.transform.position = position;
            motor.transform.forward = forward;
            controller.enabled = wasEnabled;
            Physics.SyncTransforms();
        }

        static Vector3 Planar(Vector3 value)
        {
            value.y = 0;
            return value;
        }

        void CheckDirection(Vector3 actual, Vector3 expected, string text) =>
            Check(Vector3.Dot(actual.normalized, expected.normalized) > .999f, text);

        void Check(bool condition, string text)
        {
            if (!condition) { results.Add("FAIL: " + text); throw new InvalidOperationException(text); }
            checks++;
            results.Add("PASS: " + text);
        }
    }
}
