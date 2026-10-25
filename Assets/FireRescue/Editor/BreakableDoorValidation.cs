using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    /// <summary>Batch Play Mode proof for axe command ownership and presentation-independent doors.</summary>
    [InitializeOnLoad]
    public static class BreakableDoorValidation
    {
        const string SessionKey = "FireRescue.BreakableDoorValidation";
        const string ReportPath = "Docs/Validation/2026-10-25-breakable-door/playmode.txt";
        static bool finished;
        static double readyAt;

        static BreakableDoorValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false)) return;
                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary Breakable Door Validation").AddComponent<BreakableDoorValidationDriver>();
            };
        }

        [MenuItem("FireRescue/Validate Breakable Door")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BeginWhenReady();
        }

        /// <summary>Unity batch entry point: FireRescue.Editor.BreakableDoorValidation.BatchRun.</summary>
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
            string report = (success ? "BREAKABLE_DOOR_PLAYMODE_PASS" : "BREAKABLE_DOOR_PLAYMODE_FAIL") + "\n" + details;
            File.WriteAllText(ReportPath, report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
                else EditorApplication.ExitPlaymode();
            };
        }
    }

    public sealed class BreakableDoorValidationDriver : MonoBehaviour
    {
        const float StrokePressTime = .3f;
        const float StrokeReleaseTime = .3f;
        readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        readonly List<GameObject> fixtures = new List<GameObject>();
        readonly List<string> results = new List<string>();
        float started;
        int checks;

        void Start()
        {
            started = Time.realtimeSinceStartup;
            routines.Push(Scenario());
        }

        void OnDestroy()
        {
            foreach (GameObject fixture in fixtures)
                if (fixture) Destroy(fixture);
        }

        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 45f)
                    throw new TimeoutException("Breakable door validation timed out.");

                while (routines.Count > 0)
                {
                    IEnumerator routine = routines.Peek();
                    if (!routine.MoveNext()) { routines.Pop(); continue; }
                    if (routine.Current is IEnumerator nested) { routines.Push(nested); continue; }
                    return;
                }

                enabled = false;
                BreakableDoorValidation.Finish(true, "Checks=" + checks + "\n" + string.Join("\n", results));
            }
            catch (Exception error)
            {
                enabled = false;
                BreakableDoorValidation.Finish(false,
                    "Checks passed before failure=" + checks + "\n" + string.Join("\n", results) + "\n" + error);
            }
        }

        IEnumerator Scenario()
        {
            yield return VerifyPointerAxeCommand();
            yield return VerifyNearestColliderBlocksDoor();
            yield return VerifyDoorBreakAndPresentationIsolation();
            yield return VerifyLevelFireAndRescueAccounting();
            yield return VerifyShortHoseWindowPriority();
        }

        IEnumerator VerifyShortHoseWindowPriority()
        {
            GameObject root = CreateFixture("short hose window priority", 70f);
            var player = new GameObject("Hose player"); player.transform.SetParent(root.transform, false);
            var hose = player.AddComponent<HoseConnection>();
            var source = new GameObject("Water source").AddComponent<FluidSource>();
            source.transform.SetParent(root.transform, false); source.transform.localPosition = Vector3.right * 1.8f;
            var window = new GameObject("Hose window").AddComponent<HoseWindow>();
            window.transform.SetParent(root.transform, false); window.transform.localPosition = Vector3.right * .5f;
            Check(hose.TryConnect(source), "Short-hose fixture connects a reachable source");
            hose.Interact(new[] {source}, new[] {window});
            Check(hose.ActiveWindow == window && hose.UsedLength <= 3f, "Closer valid window takes priority over disconnecting the same source");
            hose.Interact(new[] {source}, new[] {window});
            Check(!hose.ActiveWindow && hose.IsConnected, "F on the same routed window clears only the route");
            window.transform.localPosition = Vector3.left;
            hose.Interact(new[] {source}, new[] {window});
            Check(!hose.IsConnected && !hose.ActiveWindow, "Reachable but overlength window falls back to source disconnect instead of swallowing F");
            yield return DestroyFixture(root);
        }

        IEnumerator VerifyPointerAxeCommand()
        {
            GameObject root = CreateFixture("pointer command", 10f);
            FirefighterTools tools = CreateTools(root.transform, 10f);
            BreakableDoor door = CreateDoor(root.transform, 10f, 1.1f, 10f, false);
            int hitEvents = 0;
            door.HitReceived += (_, _) => hitEvents++;
            Physics.SyncTransforms();

            tools.TickPointer(Vector3.zero, true, true, false, false, null, .1f);
            Check(tools.SelectedTool == FirefighterTools.Tool.Axe && !tools.IsSwinging && hitEvents == 0,
                "The first axe click equips only; it does not swing or damage a door.");

            RunSingleStroke(tools);
            Check(hitEvents == 1 && tools.HitCount == 1 && Mathf.Approximately(door.RemainingDurability, 9f),
                "A subsequent press/release stroke performs exactly one logical axe hit.");

            tools.TickPointer(Vector3.zero, true, true, false, false, null, 1.75f);
            Check(hitEvents == 4 && tools.HitCount == 4 && Mathf.Approximately(door.RemainingDurability, 6f),
                "Holding the armed axe repeats complete swings and applies one hit per swing.");
            yield return DestroyFixture(root);
        }

        IEnumerator VerifyNearestColliderBlocksDoor()
        {
            GameObject root = CreateFixture("blocking wall", 20f);
            FirefighterTools tools = CreateTools(root.transform, 20f);
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Temporary blocking wall";
            wall.transform.SetParent(root.transform);
            wall.transform.position = new Vector3(20f, .8f, .55f);
            wall.transform.localScale = new Vector3(.9f, 1.6f, .2f);
            BreakableDoor door = CreateDoor(root.transform, 20f, 1.45f, 3f, false);
            int doorHits = 0;
            door.HitReceived += (_, _) => doorHits++;
            Physics.SyncTransforms();

            tools.TickPointer(Vector3.zero, true, true, false, false, null, .1f);
            RunSingleStroke(tools);
            Check(doorHits == 0 && !door.IsBroken && tools.LastHit.collider == wall.GetComponent<Collider>(),
                "The closest solid wall receives the cast, so a door behind it takes no axe damage.");
            yield return DestroyFixture(root);
        }

        IEnumerator VerifyDoorBreakAndPresentationIsolation()
        {
            GameObject root = CreateFixture("door lifecycle", 30f);
            FirefighterTools tools = CreateTools(root.transform, 30f);
            BreakableDoor door = CreateDoor(root.transform, 30f, 1.1f, 3f, true);
            BreakableDoorView view = door.GetComponentInChildren<BreakableDoorView>();
            GameObject replacementIntact = new GameObject("Replacement intact visual");
            replacementIntact.transform.SetParent(view.transform, false);
            GameObject replacementBroken = new GameObject("Replacement broken visual");
            replacementBroken.transform.SetParent(view.transform, false);
            view.Initialize(door, replacementIntact, replacementBroken);
            view.enabled = false;
            int brokenEvents = 0;
            door.Broken += _ => brokenEvents++;
            Physics.SyncTransforms();

            tools.TickPointer(Vector3.zero, true, true, false, false, null, .1f);
            RunSingleStroke(tools);
            RunSingleStroke(tools);
            RunSingleStroke(tools);
            Check(door.IsBroken && brokenEvents == 1 && door.gameObject.activeInHierarchy && !door.BlockingCollider.enabled,
                "Three actual tool strikes break the active gameplay root once and disable only its blocking collider.");

            GameObject walker = new GameObject("Temporary character controller");
            walker.transform.SetParent(root.transform);
            walker.transform.position = new Vector3(30f, 0f, -1f);
            CharacterController controller = walker.AddComponent<CharacterController>();
            controller.radius = .25f;
            controller.height = 1.6f;
            controller.center = new Vector3(0f, .8f, 0f);
            Physics.SyncTransforms();
            controller.Move(Vector3.forward * 3f);
            Check(walker.transform.position.z > 1.5f,
                "The disabled route collider lets a CharacterController pass through the broken doorway.");

            bool accepted = door.ReceiveAxeHit(1f);
            Check(!accepted && brokenEvents == 1 && root.activeInHierarchy,
                "Extra hits after break are ignored and cannot emit another break event or disable the gameplay root.");
            yield return DestroyFixture(root);
        }

        IEnumerator VerifyLevelFireAndRescueAccounting()
        {
            GameObject root = CreateFixture("level accounting", 40f);
            FireField field = new GameObject("Floor fire field").AddComponent<FireField>();
            field.transform.SetParent(root.transform);
            field.Configure(new Vector3(40f, 0f, 10f), 2, 1, 1f);
            field.SetIgnitionSeeds(Array.Empty<Vector2Int>());
            field.ResetSimulation();
            field.Simulate = false;

            ObjectFire objectFire = new GameObject("Object fire").AddComponent<ObjectFire>();
            objectFire.transform.SetParent(root.transform);
            objectFire.transform.position = new Vector3(41.5f, 0f, 10.5f);
            objectFire.Configure(FireKind.Ordinary, 0f);
            GameObject flameVisual = new GameObject("Flame visual");
            flameVisual.transform.SetParent(objectFire.transform, false);
            ObjectFireView view = objectFire.gameObject.AddComponent<ObjectFireView>();
            view.Initialize(objectFire, flameVisual.transform);

            Transform exit = new GameObject("Exit").transform;
            exit.SetParent(root.transform);
            exit.position = field.CellCenter(1, 0);
            GameObject residentObject = new GameObject("Resident");
            residentObject.transform.SetParent(root.transform);
            residentObject.transform.position = field.CellCenter(0, 0);
            RescueNpc resident = residentObject.AddComponent<RescueNpc>();
            resident.Autonomous = false;
            resident.Initialize(field, exit, new[] { objectFire });

            GameObject managerObject = new GameObject("Rescue Game Manager");
            managerObject.transform.SetParent(root.transform);
            managerObject.SetActive(false);
            RescueGameManager manager = managerObject.AddComponent<RescueGameManager>();
            manager.Initialize(field, new[] { resident, null, resident }, new[] { objectFire, null, objectFire });
            int completionEvents = 0;
            manager.RescueCompleted += () => completionEvents++;
            managerObject.SetActive(true);

            field.Ignite(field.CellCenter(0, 0), 1f);
            Check(!manager.AllFiresOut, "A burning floor cell alone prevents all-fires-out completion.");
            field.ApplyWater(field.CellCenter(0, 0), 10f, .75f);
            objectFire.Ignite();
            Check(!manager.AllFiresOut, "An explicit burning ObjectFire is counted after the floor fire is out.");
            view.enabled = false;
            Check(!manager.AllFiresOut,
                "Disabling a separate ObjectFireView hides presentation only; its active gameplay fire still counts.");
            resident.Tick(.3f);
            Check(!resident.HasSafePath && resident.CurrentState == RescueNpc.State.WaitingForSafeRoute,
                "A burning ObjectFire blocks the NPC's one-row route without any player command.");
            objectFire.Receive(ElementKind.DryPowder, 2f);
            Check(manager.AllFiresOut, "Null and duplicate explicit fire entries do not block completion once the unique floor and object fires are out.");
            resident.Tick(.3f);
            Check(resident.HasSafePath && resident.CurrentState == RescueNpc.State.Escaping,
                "Extinguishing the object hazard resumes automatic route discovery even while its view stays disabled.");
            objectFire.Ignite();
            resident.Tick(.3f);
            Check(!resident.HasSafePath && resident.CurrentState == RescueNpc.State.WaitingForSafeRoute,
                "Re-igniting an object hazard during escape rechecks the remaining route and stops the NPC.");
            objectFire.Receive(ElementKind.DryPowder, 2f);
            resident.Tick(.3f);
            Check(resident.HasSafePath && resident.CurrentState == RescueNpc.State.Escaping,
                "A second extinguish lets the stopped NPC resume without an interaction command.");

            for (int frame = 0; frame < 120 && !resident.IsRescued; frame++)
                resident.Tick(1f / 30f);
            Check(resident.IsRescued && manager.RescuedCount == 1 && manager.RescueComplete && completionEvents == 1,
                "Duplicate and null survivor entries still produce one automatic rescue completion event.");
            resident.Tick(1f);
            Check(manager.RescuedCount == 1 && completionEvents == 1,
                "Repeated post-rescue ticks cannot duplicate the rescue completion event.");
            yield return DestroyFixture(root);
        }

        GameObject CreateFixture(string label, float x)
        {
            GameObject root = new GameObject("Temporary breakable door validation - " + label);
            root.transform.position = new Vector3(x, 0f, 0f);
            fixtures.Add(root);
            return root;
        }

        static FirefighterTools CreateTools(Transform parent, float x)
        {
            GameObject player = new GameObject("Temporary firefighter tools");
            player.transform.SetParent(parent);
            player.transform.position = new Vector3(x, 0f, 0f);
            player.transform.forward = Vector3.forward;
            return player.AddComponent<FirefighterTools>();
        }

        static BreakableDoor CreateDoor(Transform parent, float x, float z, float durability, bool withView)
        {
            GameObject doorObject = new GameObject("Temporary breakable door");
            doorObject.transform.SetParent(parent);
            doorObject.transform.position = new Vector3(x, .8f, z);
            BoxCollider collider = doorObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(.8f, 1.6f, .2f);
            BreakableDoor door = doorObject.AddComponent<BreakableDoor>();
            door.Configure(durability, collider);
            if (!withView) return door;

            GameObject viewObject = new GameObject("Door view");
            viewObject.transform.SetParent(doorObject.transform, false);
            BreakableDoorView view = viewObject.AddComponent<BreakableDoorView>();
            GameObject intact = new GameObject("Intact visual");
            intact.transform.SetParent(viewObject.transform, false);
            GameObject broken = new GameObject("Broken visual");
            broken.transform.SetParent(viewObject.transform, false);
            view.Initialize(door, intact, broken);
            return door;
        }

        static void RunSingleStroke(FirefighterTools tools)
        {
            tools.TickPointer(Vector3.zero, true, true, false, false, null, StrokePressTime);
            tools.TickPointer(Vector3.zero, false, false, false, false, null, StrokeReleaseTime);
        }

        IEnumerator DestroyFixture(GameObject root)
        {
            fixtures.Remove(root);
            if (root) Destroy(root);
            yield return null;
        }

        void Check(bool condition, string message)
        {
            if (!condition) { results.Add("FAIL " + message); throw new InvalidOperationException(message); }
            checks++;
            results.Add("PASS " + message);
        }
    }
}
