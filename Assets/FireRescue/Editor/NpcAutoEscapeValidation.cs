using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    /// <summary>Play Mode proof that residents own evacuation; no player command is needed.</summary>
    [InitializeOnLoad]
    public static class NpcAutoEscapeValidation
    {
        const string SessionKey = "FireRescue.NpcAutoEscapeValidation";
        const string ReportPath = "Docs/Validation/2026-10-24-npc-autoescape/playmode.txt";
        static bool finished;
        static double readyAt;

        static NpcAutoEscapeValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false)) return;
                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary NPC Auto Escape Validation").AddComponent<NpcAutoEscapeValidationDriver>();
            };
        }

        [MenuItem("FireRescue/Validate NPC Auto Escape")]
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
            string report = (success ? "NPC_AUTOESCAPE_PLAYMODE_PASS" : "NPC_AUTOESCAPE_PLAYMODE_FAIL") + "\n" + details;
            File.WriteAllText(ReportPath, report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
                else EditorApplication.ExitPlaymode();
            };
        }
    }

    public sealed class NpcAutoEscapeValidationDriver : MonoBehaviour
    {
        const float Dt = 1f / 60f;
        const int FixedObstacleLayer = 8;
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
                if (Time.realtimeSinceStartup - started > 90f) throw new TimeoutException("NPC auto escape validation timed out.");
                while (routines.Count > 0)
                {
                    IEnumerator routine = routines.Peek();
                    if (!routine.MoveNext()) { routines.Pop(); continue; }
                    if (routine.Current is IEnumerator nested) { routines.Push(nested); continue; }
                    return;
                }

                enabled = false;
                NpcAutoEscapeValidation.Finish(true, "Checks=" + checks + "\n" + string.Join("\n", results));
            }
            catch (Exception error)
            {
                enabled = false;
                NpcAutoEscapeValidation.Finish(false, "Checks passed before failure=" + checks + "\n" + string.Join("\n", results) + "\n" + error);
            }
        }

        IEnumerator Scenario()
        {
            // This case is intentionally first: it observes the saved Tutorial scene before any
            // fixture work.  Input is disabled and no interaction command is ever issued.
            yield return VerifyTutorialSceneWithoutInteraction();
            yield return VerifySafeRouteWithoutPlayer();
            yield return VerifyFireAndWallRecoveryWithoutPlayer();
            yield return VerifyNewFireStopsAndClearFireResumes();
        }

        IEnumerator VerifyTutorialSceneWithoutInteraction()
        {
            yield return null;
            GameObject layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
            Check(layout && layout.activeInHierarchy, "The current user layout is active, not an archived Step2/Step3 assembly.");
            LocalPlayerInput input = layout.GetComponentInChildren<LocalPlayerInput>();
            if (input) input.ReadDevices = false;

            RescueNpc resident = layout.GetComponentInChildren<RescueNpc>(true);
            FireField field = layout.GetComponentInChildren<FireField>();
            Check(resident && field, "TutorialGreybox contains its authored resident and fire field.");
            Check(resident.Autonomous, "The saved resident enables autonomous Update without test activation.");
            // The spawn is beside the exit: move the player sideways, never toward the NPC,
            // so the player body does not physically block an otherwise safe escape route.
            FirefighterMotor motor = input ? input.GetComponent<FirefighterMotor>() : null;
            for (int frame = 0; frame < 30; frame++)
            {
                if (motor) motor.Move(Vector3.right, Dt);
                yield return null;
            }
            if (resident.IsRescued)
            {
                Check(!resident.gameObject.activeInHierarchy,
                    "Tutorial resident had already escaped autonomously before the validator drove any player input.");
                yield break;
            }

            if (!resident.HasSafePath)
            {
                Check(resident.CurrentState == RescueNpc.State.WaitingForSafeRoute,
                    "Tutorial resident waits automatically when its authored opening state has no safe route; no E command was issued.");
                yield break;
            }

            Check(resident.CurrentState == RescueNpc.State.Escaping,
                "Tutorial resident enters Escaping from a safe authored route without player proximity or E input.");
            for (int frame = 0; frame < 1200 && !resident.IsRescued; frame++)
            {
                yield return null;
            }
            Check(resident.IsRescued,
                "Tutorial resident reaches the authored exit with input disabled and no interaction command.");
        }

        IEnumerator VerifySafeRouteWithoutPlayer()
        {
            Fixture fixture = CreateFixture("safe route", 30f);
            yield return null;
            fixture.npc.Tick(.01f);
            Check(fixture.npc.HasSafePath && fixture.npc.CurrentState == RescueNpc.State.Escaping,
                "A resident with a clear route enters Escaping without any player object or interaction.");
            yield return DriveToRescue(fixture.npc, 900);
            Check(fixture.npc.IsRescued && fixture.manager.RescuedCount == 1 && fixture.completedEvents == 1,
                "Clear-route resident self-evacuates and RescueGameManager completion fires exactly once.");
            fixture.npc.Tick(1f);
            yield return null;
            Check(fixture.manager.RescuedCount == 1 && fixture.completedEvents == 1,
                "Repeated Tick after rescue cannot increment rescue count or completion event.");
            yield return DestroyFixture(fixture.root);
        }

        IEnumerator VerifyFireAndWallRecoveryWithoutPlayer()
        {
            Fixture fireFixture = CreateFixture("initial fire", 40f);
            Vector3 fireCell = fireFixture.field.CellCenter(2, 0);
            fireFixture.field.Ignite(fireCell, 1f);
            fireFixture.npc.Tick(.3f);
            Check(!fireFixture.npc.HasSafePath && fireFixture.npc.CurrentState == RescueNpc.State.WaitingForSafeRoute,
                "An initial fire blocks the route and leaves the resident waiting without a player command.");
            fireFixture.field.ApplyWater(fireCell, 100f, .25f);
            fireFixture.npc.Tick(.3f);
            Check(fireFixture.npc.HasSafePath && fireFixture.npc.CurrentState == RescueNpc.State.Escaping,
                "Extinguishing the blocking fire lets the resident discover the route on its own.");
            yield return DriveToRescue(fireFixture.npc, 900);
            Check(fireFixture.npc.IsRescued && fireFixture.manager.RescuedCount == 1,
                "After water clears the initial fire, the resident actually reaches the exit without interaction.");
            yield return DestroyFixture(fireFixture.root);

            Fixture wallFixture = CreateFixture("fixed wall", 50f);
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Temporary NPC route wall";
            wall.layer = FixedObstacleLayer;
            wall.transform.SetParent(wallFixture.root.transform);
            wall.transform.position = wallFixture.field.CellCenter(3, 0) + Vector3.up * .7f;
            wall.transform.localScale = new Vector3(.82f, 1.4f, .82f);
            Physics.SyncTransforms();
            wallFixture.npc.Tick(.3f);
            Check(!wallFixture.npc.HasSafePath && wallFixture.npc.CurrentState == RescueNpc.State.WaitingForSafeRoute,
                "A real layer-8 wall blocks BFS and leaves the resident waiting without player interaction.");
            Destroy(wall);
            yield return null;
            Physics.SyncTransforms();
            wallFixture.npc.Tick(.3f);
            Check(wallFixture.npc.HasSafePath && wallFixture.npc.CurrentState == RescueNpc.State.Escaping,
                "Removing the physical wall lets the resident resume route discovery on its own.");
            yield return DriveToRescue(wallFixture.npc, 900);
            Check(wallFixture.npc.IsRescued && wallFixture.manager.RescuedCount == 1,
                "After the wall is removed, the resident actually walks to the exit without interaction.");
            yield return DestroyFixture(wallFixture.root);
        }

        IEnumerator VerifyNewFireStopsAndClearFireResumes()
        {
            Fixture fixture = CreateFixture("new fire recovery", 60f);
            fixture.npc.Tick(.01f);
            fixture.npc.Tick(.3f);
            Check(fixture.npc.HasSafePath && fixture.npc.CurrentState == RescueNpc.State.Escaping,
                "Resident begins moving along a clear route before the dynamic-fire check.");

            Vector3 futureCell = fixture.field.CellCenter(3, 0);
            fixture.field.Ignite(futureCell, 1f);
            fixture.npc.Tick(.3f);
            Check(!fixture.npc.HasSafePath && fixture.npc.CurrentState == RescueNpc.State.WaitingForSafeRoute,
                "A new fire on the remaining route stops the escaping resident without player input.");

            fixture.field.ApplyWater(futureCell, 100f, .25f);
            fixture.npc.Tick(.3f);
            Check(fixture.npc.HasSafePath && fixture.npc.CurrentState == RescueNpc.State.Escaping,
                "Clearing the new fire makes the resident resume the route automatically.");
            yield return DriveToRescue(fixture.npc, 900);
            Check(fixture.npc.IsRescued && fixture.manager.RescuedCount == 1 && fixture.completedEvents == 1,
                "Recovered resident completes one autonomous rescue even with its Visual disabled.");
            yield return DestroyFixture(fixture.root);
        }

        IEnumerator DriveToRescue(RescueNpc npc, int maximumFrames)
        {
            for (int frame = 0; frame < maximumFrames && !npc.IsRescued; frame++)
            {
                npc.Tick(Dt);
                yield return null;
            }
        }

        Fixture CreateFixture(string label, float x)
        {
            GameObject root = new GameObject("Temporary NPC auto escape - " + label);
            fixtures.Add(root);
            FireField field = new GameObject("Fire field").AddComponent<FireField>();
            field.transform.SetParent(root.transform);
            field.Configure(new Vector3(x, 0f, 30f), 6, 1, 1f);
            field.SetIgnitionSeeds(Array.Empty<Vector2Int>());
            field.ResetSimulation();
            field.Simulate = false;

            Transform exit = new GameObject("Exit").transform;
            exit.SetParent(root.transform);
            exit.position = field.CellCenter(5, 0) + Vector3.up * .05f;
            GameObject resident = new GameObject("Resident without a player");
            resident.transform.SetParent(root.transform);
            resident.transform.position = field.CellCenter(0, 0) + Vector3.up * .05f;
            RescueNpc npc = resident.AddComponent<RescueNpc>();
            npc.Autonomous = false;
            npc.Initialize(field, exit);

            GameObject hiddenVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hiddenVisual.name = "Visual";
            hiddenVisual.transform.SetParent(resident.transform, false);
            hiddenVisual.SetActive(false);

            GameObject managerObject = new GameObject("Rescue Game Manager");
            managerObject.SetActive(false);
            managerObject.transform.SetParent(root.transform);
            RescueGameManager manager = managerObject.AddComponent<RescueGameManager>();
            manager.Initialize(field, new[] { npc });
            Fixture fixture = new Fixture { root = root, field = field, npc = npc, manager = manager };
            manager.RescueCompleted += () => fixture.completedEvents++;
            managerObject.SetActive(true);
            Physics.SyncTransforms();
            return fixture;
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

        sealed class Fixture
        {
            public GameObject root;
            public FireField field;
            public RescueNpc npc;
            public RescueGameManager manager;
            public int completedEvents;
        }
    }
}
