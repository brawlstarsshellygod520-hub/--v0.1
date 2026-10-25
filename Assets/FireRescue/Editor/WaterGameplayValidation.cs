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
    /// Drives the persisted residential water gameplay setup through the same LocalPlayerInput
    /// command path as a player.  It is intentionally verification-only: no tutorial state,
    /// scene authoring, or gameplay rules live here.
    /// </summary>
    [InitializeOnLoad]
    public static class WaterGameplayValidation
    {
        const string SessionKey = "FireRescue.WaterGameplayValidation";
        const string ReportPath = "Docs/Validation/2026-10-21-water-gameplay/playmode.txt";
        static bool finished;
        static double readyAt;

        static WaterGameplayValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false))
                    return;

                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary Water Gameplay Validation").AddComponent<WaterGameplayValidationDriver>();
            };
        }

        [MenuItem("FireRescue/Validate Water Gameplay In Residential Scene")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            BeginWhenReady();
        }

        // Entry point for Unity -executeMethod.  The wait makes it safe to invoke immediately
        // after this editor script or its scene authoring counterpart has been imported.
        public static void BatchRun()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("BatchRun is intended for Unity batch mode.");
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
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < readyAt)
                return;

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
            if (finished)
                return;

            finished = true;
            SessionState.SetBool(SessionKey, false);
            Application.logMessageReceived -= OnLog;
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            string report = (success ? "WATER_GAMEPLAY_PLAYMODE_PASS" : "WATER_GAMEPLAY_PLAYMODE_FAIL") + "\n" + details;
            File.WriteAllText(ReportPath, report);
            if (success)
                Debug.Log(report);
            else
                Debug.LogError(report);
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
                else EditorApplication.ExitPlaymode();
            };
        }
    }

    public sealed class WaterGameplayValidationDriver : MonoBehaviour
    {
        const float Dt = 1f / 60f;
        const float GoalTolerance = .12f;
        readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        readonly List<string> results = new List<string>();

        GameObject layout;
        FirefighterMotor motor;
        LocalPlayerInput input;
        HoseConnection hose;
        SprayEmitter emitter;
        FireField field;
        FluidSource water;
        Rigidbody coffeeTable;
        Camera gameCamera;
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
        }

        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 150f)
                    throw new TimeoutException("Residential water gameplay validation timed out.");

                while (routines.Count > 0)
                {
                    IEnumerator routine = routines.Peek();
                    if (!routine.MoveNext())
                    {
                        routines.Pop();
                        continue;
                    }

                    if (routine.Current is IEnumerator nested)
                    {
                        routines.Push(nested);
                        continue;
                    }

                    return;
                }
            }
            catch (Exception error)
            {
                enabled = false;
                Capture("failure.png");
                WaterGameplayValidation.Finish(false, "Checks passed before failure: " + checks + "\n" + string.Join("\n", results) + "\n" + error);
            }
        }

        IEnumerator Scenario()
        {
            yield return null;
            layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
            Check(layout && layout.activeInHierarchy, "Persisted residential gameplay layout is active.");
            motor = layout.GetComponentInChildren<FirefighterMotor>();
            input = motor ? motor.GetComponent<LocalPlayerInput>() : null;
            hose = motor ? motor.GetComponent<HoseConnection>() : null;
            emitter = motor ? motor.GetComponent<SprayEmitter>() : null;
            field = layout.GetComponentInChildren<FireField>();
            water = layout.GetComponentsInChildren<FluidSource>().FirstOrDefault(source => source.Element == ElementKind.Water);
            Transform table = layout.transform.Find("Household Props/Coffee table");
            coffeeTable = table ? table.GetComponent<Rigidbody>() : null;
            gameCamera = Camera.main;
            Check(motor && input && hose && emitter && field && water && coffeeTable && gameCamera,
                "Player input, hose, emitter, fire field, toilet water source, movable coffee table, and Game camera are wired.");

            input.ReadDevices = false;
            input.SetInputFocus(true);
            Check(input.isActiveAndEnabled, "Real command input is enabled, with only hardware polling disabled.");
            field.Simulate = true;
            Check(field.BurningCount == WaterGameplayBuilder.Fires.Length, "Fresh persisted scene starts with exactly three water-gameplay fire seeds.");
            foreach (Vector3 point in WaterGameplayBuilder.Fires)
            {
                Check(field.TryGetCell(point, out Vector2Int cell) && field.GetCell(cell.x, cell.y).IsBurning,
                    "Initial fire seed is burning at " + point.ToString("F2") + ".");
            }
            Check(Vector3.Distance(coffeeTable.position, WaterGameplayBuilder.PushablePosition) < .01f && !coffeeTable.isKinematic,
                "Coffee table is the authored movable water-push target.");

            Capture("01-before-water.png");
            yield return Walk(new Vector3(5.74f, 0f, -4.8f), "Enter the front door from outdoor spawn");
            yield return Walk(new Vector3(.2f, 0f, -4.6f), "Cross the main room toward the bathroom");
            yield return Walk(new Vector3(.2f, 0f, 2.4f), "Reach the bathroom doorway");
            yield return Walk(new Vector3(-2.3f, 0f, 2.4f), "Reach the toilet water source");

            Command(Vector2.zero, water.transform.position, true);
            yield return null;
            Check(hose.Source == water && hose.IsConnected, "F connects the hose to the actual toilet cistern water source.");

            yield return Walk(new Vector3(.2f, 0f, 2.4f), "Pull the connected hose back through the bathroom doorway");
            yield return Walk(new Vector3(.2f, 0f, .75f), "Reach a clear water-spray position near the fire");
            Check(hose.Source == water && hose.UsedLength > 3f, "Hose remains connected while the player walks away from the source.");
            Capture("01b-fire-before-spray.png");

            Vector3 fireAim = WaterGameplayBuilder.Fires[0];
            float beforeIntensity = TotalFireIntensity();
            for (int i = 0; i < 12; i++)
            {
                Command(Vector2.zero, fireAim, false, false, true);
                yield return null;
            }
            Check(emitter.IsSpraying && emitter.HasImpact, "Held RMB uses the connected hose and produces an actual water impact.");
            Check(TotalFireIntensity() < beforeIntensity - .03f, "Sustained real water spray lowers the initial fire intensity.");
            Capture("02-during-water.png");

            for (int i = 0; i < 180 && field.BurningCount > 0; i++)
            {
                Command(Vector2.zero, fireAim, false, false, true);
                yield return null;
            }
            Check(field.BurningCount == 0, "Continuous connected water spray extinguishes the complete initial fire patch.");
            Command(Vector2.zero, fireAim);
            yield return null;
            Check(!emitter.IsSpraying, "Releasing RMB stops the water stream.");
            Capture("03-after-extinguish.png");

            yield return Walk(new Vector3(1.4f, 0f, -2.5f), "Move beside the coffee table with the hose still connected");
            Check(hose.Source == water, "Water source stays connected before the push interaction.");
            Vector3 tableBefore = coffeeTable.position;
            Vector3 tableAim = new Vector3(5.5f, 0f, -2.5f);
            for (int i = 0; i < 24; i++)
            {
                Command(Vector2.zero, tableAim, false, false, true);
                yield return null;
            }
            Bounds tableBounds = coffeeTable.GetComponent<Collider>().bounds;
            tableBounds.Expand(.1f);
            Check(emitter.HasImpact && tableBounds.Contains(emitter.ImpactPoint) && emitter.ImpactPoint.y > .1f,
                "Water actually hits the table above floor level before measuring its motion.");
            yield return WaitForFixedSteps(4);
            Check(Vector3.Distance(coffeeTable.position, tableBefore) > .1f,
                "Water pushes the grounded table >0.1m: " + tableBefore.ToString("F3") + " -> " + coffeeTable.position.ToString("F3") + ".");
            Command(Vector2.zero, tableAim);
            yield return null;
            Capture("04-water-pushed-table.png");

            Command(Vector2.zero, tableAim, true);
            yield return null;
            Check(!hose.IsConnected, "F away from a reachable source disconnects the hose.");
            Command(Vector2.zero, tableAim, false, false, true);
            yield return null;
            Check(!emitter.IsSpraying, "Disconnected RMB cannot start a water stream.");

            enabled = false;
            WaterGameplayValidation.Finish(true, "Actual command-path checks=" + checks + "\n" + string.Join("\n", results));
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
                Command(new Vector2(Vector3.Dot(direction, right), Vector3.Dot(direction, forward)), goal);
                yield return null;
            }

            Check(PlanarDistance(motor.transform.position, goal) <= GoalTolerance,
                label + ": real camera-relative input reached " + motor.transform.position.ToString("F2") + ".");
        }

        IEnumerator WaitForFixedSteps(int count)
        {
            float target = Time.fixedTime + Time.fixedDeltaTime * count;
            int frames = 0;
            while (Time.fixedTime < target && frames++ < 180)
                yield return null;
            Check(Time.fixedTime >= target, "Observed " + count + " fixed physics steps for water impulse.");
        }

        void Command(Vector2 move, Vector3 aim, bool interact = false, bool axe = false, bool spray = false)
        {
            input.TickCommand(move, aim, interact, false, axe, spray, Dt);
        }

        float TotalFireIntensity()
        {
            float total = 0f;
            for (int i = 0; i < WaterGameplayBuilder.Fires.Length; i++)
            {
                if (!field.TryGetCell(WaterGameplayBuilder.Fires[i], out Vector2Int cell))
                    continue;
                total += field.GetCell(cell.x, cell.y).Intensity;
            }
            return total;
        }

        void Capture(string name)
        {
            if (!gameCamera)
                return;
            const int width = 1600;
            const int height = 900;
            Directory.CreateDirectory("Docs/Validation/2026-10-21-water-gameplay");
            RenderTexture target = new RenderTexture(width, height, 24);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = gameCamera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                gameCamera.targetTexture = target;
                gameCamera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes("Docs/Validation/2026-10-21-water-gameplay/" + name, image.EncodeToPNG());
            }
            finally
            {
                gameCamera.targetTexture = previousTarget;
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
