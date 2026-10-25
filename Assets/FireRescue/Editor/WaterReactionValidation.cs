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
    /// <summary>
    /// Disposable Play Mode coverage for the real hose-to-water reaction path.  It uses the
    /// greybox prefabs, but moves their instances into an isolated part of an empty scene.
    /// </summary>
    [InitializeOnLoad]
    public static class WaterReactionValidation
    {
        const string Key = "FireRescue.WaterReactionValidation";
        const string ReportPath = "Docs/Validation/2026-10-21-water-gameplay/water-reactions.txt";
        static bool finished;
        static double batchReadyAt;

        static WaterReactionValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false))
                    return;

                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary Water Reaction Validation").AddComponent<WaterReactionValidationDriver>();
            };
        }

        [MenuItem("消防 Prototype/驗證水流反應與牆面阻擋")]
        public static void Run() => Begin(false);

        // Public for -executeMethod.  Delay until imports and compilation settle so this
        // remains reliable immediately after the validation script itself is imported.
        public static void BatchRun()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("BatchRun is only for Unity batchmode.");
            batchReadyAt = EditorApplication.timeSinceStartup + 2d;
            EditorApplication.update += BeginBatch;
        }

        static void BeginBatch()
        {
            if (EditorApplication.timeSinceStartup < batchReadyAt || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            EditorApplication.update -= BeginBatch;
            try
            {
                Begin(true);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Finish(false, "Batch setup exception:\n" + error);
            }
        }

        static void Begin(bool batch)
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit Play Mode before running water reaction validation.");
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

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
            if (finished)
                return;

            finished = true;
            Application.logMessageReceived -= OnLog;
            SessionState.SetBool(Key, false);
            string report = (success ? "WATER_REACTIONS_PLAYMODE_PASS" : "WATER_REACTIONS_PLAYMODE_FAIL") + "\n" + details;
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, report);
            if (success)
                Debug.Log(report);
            else
                Debug.LogError(report);
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode)
                    EditorApplication.Exit(success ? 0 : 1);
                else
                    EditorApplication.ExitPlaymode();
            };
        }
    }

    public sealed class WaterReactionValidationDriver : MonoBehaviour
    {
        const int FixedObstacleLayer = 8;
        const float Dt = .1f;
        static readonly Vector3 FixtureOrigin = new Vector3(100f, 0f, 100f);
        static readonly Vector3 FieldOrigin = new Vector3(100f, 0f, 99f);

        readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        readonly List<string> results = new List<string>();
        GameObject assembly;
        FirefighterMotor motor;
        HoseConnection hose;
        SprayEmitter emitter;
        FireField field;
        ElementReaction reactions;
        FluidSource water;
        Rigidbody pushable;
        BoxCollider fixedObstacle;
        FireFieldView fireView;
        float started;
        int checks;

        void Start()
        {
            Time.captureDeltaTime = 1f / 60f;
            started = Time.realtimeSinceStartup;
            routines.Push(Scenario());
        }

        void OnDestroy()
        {
            Time.captureDeltaTime = 0f;
            if (assembly)
                Destroy(assembly);
        }

        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 45f)
                    throw new TimeoutException("Water reaction validation timed out.");

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
                WaterReactionValidation.Finish(false, "Actual checks passed before failure: " + checks + "\n" + string.Join("\n", results) + "\n" + error);
            }
        }

        IEnumerator Scenario()
        {
            assembly = GreyboxPrefabBuilder.CreateTestAssembly();
            yield return null;
            SetupRealPrefabFixture();
            yield return VerifyDisconnectedSprayDoesNothing();
            yield return VerifyConnectedFloorWaterExtinguishes();
            yield return VerifyWaterImpulseUsesFixedPhysics();
            yield return VerifyWallBlocksStreamSplashAndPush();
            yield return VerifyFloorWaterCannotRadiusLeakThroughWall();
            yield return VerifyWaterDoesNotNeedFirePresentation();
            enabled = false;
            WaterReactionValidation.Finish(true, "Actual Play Mode checks: " + checks + "\n" + string.Join("\n", results));
        }

        void SetupRealPrefabFixture()
        {
            assembly.transform.position = FixtureOrigin;
            motor = assembly.GetComponentInChildren<FirefighterMotor>();
            hose = motor ? motor.GetComponent<HoseConnection>() : null;
            emitter = motor ? motor.GetComponent<SprayEmitter>() : null;
            field = assembly.GetComponentInChildren<FireField>();
            reactions = assembly.GetComponentInChildren<ElementReaction>();
            fireView = assembly.GetComponentInChildren<FireFieldView>();
            water = FindSource(ElementKind.Water);
            pushable = assembly.GetComponentInChildren<Rigidbody>();
            fixedObstacle = FindNamedCollider("FixedObstacle");
            Check(motor && hose && emitter && field && reactions && water && pushable && fixedObstacle,
                "Real prefab player, emitter, reaction, water source, rigidbody, floor, and fixed obstacle are available.");
            Check(assembly.GetComponentInChildren<Collider>() != null, "Real prefab fixture includes physical colliders.");

            // Keep the actual water source nearby but wholly outside every +X spray beam.
            TeleportPlayer(new Vector3(101f, .05f, 100.5f), Vector3.right);
            water.transform.position = new Vector3(101f, 0f, 102f);
            fixedObstacle.enabled = false;
            pushable.gameObject.SetActive(false);
            field.Simulate = false;
            ResetField();
            Physics.SyncTransforms();
            Check(hose.TryConnect(water) && hose.Source == water,
                "Real player can connect its real hose to the moved real water source.");
            hose.Disconnect();
        }

        IEnumerator VerifyDisconnectedSprayDoesNothing()
        {
            ResetField();
            Vector3 fire = Cell(2);
            field.Ignite(fire, .7f);
            float intensity = field.GetCell(2, 1).Intensity;
            float wetness = field.GetCell(2, 1).Wetness;
            PreparePushable(new Vector3(103f, 0f, 100.5f));
            Vector3 beforePush = pushable.position;
            emitter.Tick(pushable.worldCenterOfMass, true, Dt);
            yield return WaitForPhysics();
            Check(!emitter.IsSpraying && !emitter.HasImpact,
                "Disconnected right spray has no active stream or impact.");
            Check(Mathf.Approximately(field.GetCell(2, 1).Intensity, intensity) && Mathf.Approximately(field.GetCell(2, 1).Wetness, wetness),
                "Disconnected spray cannot wet or extinguish the fire field while simulation is disabled.");
            Check((pushable.position - beforePush).sqrMagnitude < .000001f,
                "Disconnected spray cannot push a real Rigidbody.");
            pushable.gameObject.SetActive(false);
        }

        IEnumerator VerifyConnectedFloorWaterExtinguishes()
        {
            ResetField();
            Vector3 fire = Cell(2);
            field.Ignite(fire, .7f);
            float before = field.GetCell(2, 1).Intensity;
            ConnectWater();
            for (int i = 0; i < 20; i++)
                emitter.Tick(new Vector3(102.75f, 0f, 100.5f), true, Dt);
            FireCellState after = field.GetCell(2, 1);
            Check(emitter.HasImpact && Mathf.Abs(emitter.ImpactPoint.y) < .12f,
                "Connected stream reaches the real prefab floor directly, not a source or support in its beam.");
            Check(after.Intensity < before && after.Wetness > 0f,
                "Continuous direct floor water decreases fire intensity and increases wetness.");
            Check(after.Intensity <= .0001f && field.IsSafe(new Vector2Int(2, 1)),
                "Continuous direct floor water extinguishes the targeted fire.");
            emitter.Stop();
            hose.Disconnect();
            yield return null;
        }

        IEnumerator VerifyWaterImpulseUsesFixedPhysics()
        {
            ResetField();
            PreparePushable(new Vector3(103f, 0f, 100.5f));
            ConnectWater();
            Vector3 before = pushable.position;
            for (int i = 0; i < 24; i++)
            {
                emitter.Tick(pushable.worldCenterOfMass, true, 1f / 60f);
                yield return null;
            }
            Check(emitter.HasImpact, "Connected stream directly impacts the actual dynamic Rigidbody fixture.");
            yield return WaitForPhysics();
            yield return WaitForPhysics();
            Check(pushable.position.x > before.x + .1f,
                "60 Hz grounded push >.1m: " + before.ToString("F4") + " -> " + pushable.position.ToString("F4") +
                ", velocity=" + pushable.linearVelocity.ToString("F4") + ", impact=" + emitter.ImpactPoint.ToString("F4") +
                ", push=" + new SerializedObject(reactions).FindProperty("waterPushImpulsePerUnit").floatValue +
                ", physicsTime=" + Time.fixedTime.ToString("F3") + ".");
            emitter.Stop();
            hose.Disconnect();
            pushable.gameObject.SetActive(false);
        }

        IEnumerator VerifyWallBlocksStreamSplashAndPush()
        {
            ResetField();
            BoxCollider wall = CreateThinWall();
            Vector3 hiddenFire = Cell(3);
            field.Ignite(hiddenFire, .7f);
            float intensity = field.GetCell(3, 1).Intensity;
            PreparePushable(new Vector3(104f, 0f, 100.5f));
            Vector3 beforePush = pushable.position;
            ConnectWater();
            for (int i = 0; i < 8; i++)
                emitter.Tick(new Vector3(104f, .4f, 100.5f), true, Dt);
            yield return WaitForPhysics();
            Check(emitter.HasImpact && emitter.ImpactPoint.x < wall.bounds.center.x,
                "The thin layer-8 wall receives the actual stream before objects behind it.");
            Check(Mathf.Approximately(field.GetCell(3, 1).Intensity, intensity) && Mathf.Approximately(field.GetCell(3, 1).Wetness, 0f),
                "A wall blocks water splash and extinguishing for fire within the .95m radius behind it.");
            Check((pushable.position - beforePush).sqrMagnitude < .000001f,
                "A wall blocks water push on the real Rigidbody behind it.");
            emitter.Stop();
            hose.Disconnect();
            pushable.gameObject.SetActive(false);
            Destroy(wall.gameObject);
            yield return null;
        }

        IEnumerator VerifyFloorWaterCannotRadiusLeakThroughWall()
        {
            ResetField();
            BoxCollider wall = CreateThinWall();
            Vector3 left = Cell(1);
            Vector3 front = Cell(2);
            Vector3 hidden = Cell(3);
            field.Ignite(left, .7f);
            field.Ignite(front, .7f);
            field.Ignite(hidden, .7f);
            float hiddenIntensity = field.GetCell(3, 1).Intensity;
            ConnectWater();

            // This first direct floor impact should wet two adjacent, same-side cells.
            for (int i = 0; i < 3; i++)
                emitter.Tick(new Vector3(102.2f, 0f, 100.5f), true, Dt);
            Check(field.GetCell(1, 1).Wetness > 0f && field.GetCell(2, 1).Wetness > 0f,
                "Direct floor water still wets adjacent cells on the same side of the wall.");

            // The closer impact is inside the old .95m radius of the hidden cell; it must
            // nevertheless remain dry because the layer-8 wall crosses the floor path.
            for (int i = 0; i < 10; i++)
                emitter.Tick(new Vector3(102.75f, 0f, 100.5f), true, Dt);
            Check(emitter.HasImpact && Mathf.Abs(emitter.ImpactPoint.y) < .12f && emitter.ImpactPoint.x < wall.bounds.center.x,
                "The radius-leak check is a direct floor impact in front of the thin wall.");
            Check(field.GetCell(2, 1).Intensity < .7f && field.GetCell(2, 1).Wetness > 0f,
                "Floor water continues to affect the reachable front cell.");
            Check(Mathf.Approximately(field.GetCell(3, 1).Intensity, hiddenIntensity) && Mathf.Approximately(field.GetCell(3, 1).Wetness, 0f),
                "Direct floor water cannot radius-leak through the wall to the fire behind it.");
            emitter.Stop();
            hose.Disconnect();
            Destroy(wall.gameObject);
            yield return null;
        }

        IEnumerator VerifyWaterDoesNotNeedFirePresentation()
        {
            ResetField();
            Vector3 fire = Cell(2);
            field.Ignite(fire, .7f);
            float before = field.GetCell(2, 1).Intensity;
            Check(fireView != null, "Real fire prefab supplies an optional FireFieldView presentation.");
            fireView.enabled = false;
            fireView.gameObject.SetActive(false);
            ConnectWater();
            for (int i = 0; i < 4; i++)
                emitter.Tick(new Vector3(102.75f, 0f, 100.5f), true, Dt);
            Check(field.GetCell(2, 1).Intensity < before && field.GetCell(2, 1).Wetness > 0f,
                "Disabled fire presentation does not prevent real water reactions.");
            emitter.Stop();
            hose.Disconnect();
            yield return null;
        }

        void ResetField()
        {
            field.SetIgnitionSeeds(Array.Empty<Vector2Int>());
            field.Configure(FieldOrigin, 5, 3, 1f);
            field.Simulate = false;
        }

        void ConnectWater()
        {
            hose.Disconnect();
            Physics.SyncTransforms();
            Check(hose.TryConnect(water) && hose.Source == water, "Test reconnects the real water source.");
        }

        void PreparePushable(Vector3 position)
        {
            pushable.gameObject.SetActive(true);
            pushable.linearVelocity = Vector3.zero;
            pushable.angularVelocity = Vector3.zero;
            pushable.transform.position = position;
            Physics.SyncTransforms();
        }

        BoxCollider CreateThinWall()
        {
            GameObject wall = new GameObject("Temporary .8m layer-8 water wall") { layer = FixedObstacleLayer };
            wall.transform.position = new Vector3(103f, .4f, 100.5f);
            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.size = new Vector3(.08f, .8f, 2f);
            Physics.SyncTransforms();
            return collider;
        }

        FluidSource FindSource(ElementKind kind)
        {
            foreach (FluidSource source in assembly.GetComponentsInChildren<FluidSource>(true))
                if (source.Element == kind)
                    return source;
            return null;
        }

        BoxCollider FindNamedCollider(string name)
        {
            foreach (BoxCollider collider in assembly.GetComponentsInChildren<BoxCollider>(true))
                if (collider.gameObject.name == name || collider.gameObject.name == name + "(Clone)")
                    return collider;
            return null;
        }

        Vector3 Cell(int x) => field.CellCenter(x, 1);

        // This driver pumps nested enumerators itself, so wait on actual physics time.
        IEnumerator WaitForPhysics()
        {
            float before = Time.fixedTime;
            while (Time.fixedTime <= before) yield return null;
        }

        void TeleportPlayer(Vector3 position, Vector3 forward)
        {
            CharacterController controller = motor.GetComponent<CharacterController>();
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            motor.transform.position = position;
            motor.transform.forward = forward;
            controller.enabled = wasEnabled;
            Physics.SyncTransforms();
        }

        void Check(bool condition, string text)
        {
            if (!condition)
            {
                results.Add("FAIL: " + text);
                throw new InvalidOperationException(text);
            }
            checks++;
            results.Add("PASS: " + text);
        }
    }
}
