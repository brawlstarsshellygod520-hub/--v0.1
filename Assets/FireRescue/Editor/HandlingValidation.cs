using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    [InitializeOnLoad]
    public static class HandlingValidation
    {
        const string Key = "FireRescue.HandlingValidation";
        const string ScenePath = "Assets/FireRescue/Scenes/ResidentialFire.unity";
        static bool finished;

        static HandlingValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false))
                    return;

                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary Handling Validation").AddComponent<HandlingValidationDriver>();
            };
        }

        [MenuItem("消防 Prototype/驗證操控與互動處理")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(ScenePath);
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }

        static void OnLog(string text, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Finish(false, text + "\n" + stack);
        }

        public static void Finish(bool success, string details)
        {
            if (finished)
                return;

            finished = true;
            Application.logMessageReceived -= OnLog;
            SessionState.SetBool(Key, false);
            string report = (success ? "HANDLING_PLAYMODE_PASS" : "HANDLING_PLAYMODE_FAIL") + "\n" + details;
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/handling-results.txt", report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
                else EditorApplication.ExitPlaymode();
            };
        }
    }

    public sealed class HandlingValidationDriver : MonoBehaviour
    {
        const int FixedObstacleLayer = 8;
        readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        readonly List<string> results = new List<string>();
        readonly List<GameObject> temporaryRoots = new List<GameObject>();

        FirefighterMotor motor;
        HoseConnection hose;
        SprayEmitter emitter;
        LocalPlayerInput input;
        CharacterController playerController;
        float started;

        void Start()
        {
            Time.captureDeltaTime = 1f / 60f;
            started = Time.realtimeSinceStartup;
            motor = FindFirstObjectByType<FirefighterMotor>();
            hose = motor ? motor.GetComponent<HoseConnection>() : null;
            emitter = motor ? motor.GetComponent<SprayEmitter>() : null;
            input = motor ? motor.GetComponent<LocalPlayerInput>() : null;
            playerController = motor ? motor.GetComponent<CharacterController>() : null;
            routines.Push(Scenario());
        }

        void OnDestroy()
        {
            Time.captureDeltaTime = 0f;
            CleanupAllFixtures();
        }

        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 35f)
                    throw new Exception("Handling validation timeout.");

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
                CleanupAllFixtures();
                HandlingValidation.Finish(false, string.Join("\n", results) + "\n" + error);
            }
        }

        IEnumerator Scenario()
        {
            yield return null;
            Check(motor && hose && emitter && input && playerController,
                "ResidentialFire exposes the real motor, hose, emitter, input and CharacterController.");

            yield return VerifyViewRelativeDirections();
            yield return VerifyInputAndSprayLifecycle();
            yield return VerifyHoseReachability();
            yield return VerifyOilAndWallHandling();
            yield return VerifyNpcRouteRecovery();

            enabled = false;
            HandlingValidation.Finish(true, string.Join("\n", results));
        }

        IEnumerator VerifyViewRelativeDirections()
        {
            GameObject root = CreateRoot("View-relative directions");
            GameObject view = new GameObject("Validation view");
            view.transform.SetParent(root.transform);
            view.transform.rotation = Quaternion.LookRotation(new Vector3(.72f, -.58f, .38f));
            Vector3 forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(view.transform.right, Vector3.up).normalized;

            CheckDirection(LocalPlayerInput.ViewRelativeDirection(Vector2.up, view.transform), forward,
                "W resolves to screen-up ground direction.");
            CheckDirection(LocalPlayerInput.ViewRelativeDirection(Vector2.down, view.transform), -forward,
                "S resolves to screen-down ground direction.");
            CheckDirection(LocalPlayerInput.ViewRelativeDirection(Vector2.left, view.transform), -right,
                "A resolves to screen-left ground direction.");
            CheckDirection(LocalPlayerInput.ViewRelativeDirection(Vector2.right, view.transform), right,
                "D resolves to screen-right ground direction.");
            CheckDirection(LocalPlayerInput.ViewRelativeDirection(new Vector2(1f, 1f), view.transform),
                (right + forward).normalized, "Diagonal commands remain screen-relative and normalized.");

            yield return DestroyFixture(root);
        }

        IEnumerator VerifyInputAndSprayLifecycle()
        {
            GameObject root = CreateRoot("Input and spray lifecycle");
            CreateGround(root, new Vector3(44f, -.1f, 0f), new Vector3(12f, .2f, 8f));
            FireField field = CreateField(root, new Vector3(39f, 0f, -3f), 10, 6);
            ElementReaction reactions = CreateReaction(root, field);
            FluidSource source = CreateSource(root, "Connected water source", new Vector3(41.35f, .2f, 0f), ElementKind.Water);
            Teleport(motor, new Vector3(40f, .05f, 0f));
            motor.transform.forward = Vector3.forward;
            motor.Initialize(field, hose);
            emitter.Initialize(hose, reactions);
            hose.Disconnect();

            input.enabled = false;
            input.Initialize(motor,hose,emitter,Camera.main,new[] { source },Array.Empty<RescueNpc>());
            input.TickCommand(Vector2.zero, motor.transform.position + Vector3.forward * 3f, false, false, true, .02f);
            Check(!emitter.IsSpraying, "An unconnected hose cannot spray through the shared TickCommand entry point.");
            input.TickCommand(Vector2.zero,motor.transform.position + Vector3.forward * 3f,true,false,false,.02f);
            Check(hose.Source == source, "The shared F command connects the real hose to the nearby unobstructed fixture source.");

            Vector3 beforeNearAim = motor.transform.forward;
            input.TickCommand(Vector2.zero, motor.transform.position + Vector3.right * .1f, false, false, true, .02f);
            Check(emitter.IsSpraying && Vector3.Dot(beforeNearAim, motor.transform.forward) > .98f,
                "Nearer-than-minimum aim keeps the current forward direction instead of reversing the spray.");

            Vector3 behind = motor.transform.position - Vector3.forward * 3f;
            input.TickCommand(Vector2.zero, behind, false, false, true, .02f);
            Check(Vector3.Dot(motor.transform.forward, (behind - motor.transform.position).normalized) > .999f,
                "Held spray immediately faces a 180-degree ground aim point.");

            input.TickCommand(Vector2.zero, behind, false, false, false, .02f);
            Check(!emitter.IsSpraying && !emitter.HasImpact, "Releasing shared TickCommand stops spray and clears its impact state.");

            input.enabled = true;
            input.TickCommand(Vector2.zero, behind, false, false, true, .02f);
            input.enabled = false;
            bool inputDisableStops = !emitter.IsSpraying && !emitter.HasImpact;

            input.enabled = true;
            input.TickCommand(Vector2.zero, behind, false, false, true, .02f);
            emitter.enabled = false;
            bool emitterDisableStops = !emitter.IsSpraying && !emitter.HasImpact;
            emitter.enabled = true;
            Check(inputDisableStops && emitterDisableStops,
                "Disabling LocalPlayerInput or SprayEmitter immediately stops spray and clears impact state.");

            input.TickCommand(Vector2.zero, behind, false, false, true, .02f);
            input.SendMessage("OnApplicationFocus", false, SendMessageOptions.RequireReceiver);
            Check(!emitter.IsSpraying && !emitter.HasImpact, "Losing application focus immediately stops spray.");
            emitter.Stop();
            hose.Disconnect();
            input.enabled = false;

            yield return DestroyFixture(root);
        }

        IEnumerator VerifyHoseReachability()
        {
            GameObject root = CreateRoot("Hose reachability");
            CreateGround(root, new Vector3(44f, -.1f, 0f), new Vector3(12f, .2f, 8f));
            Teleport(motor, new Vector3(40f, .05f, 0f));
            hose.Disconnect();

            FluidSource tooFar = CreateSource(root, "Too far source", new Vector3(42.05f, .2f, 0f), ElementKind.Water);
            bool tooFarTry = hose.TryConnect(tooFar);
            hose.Interact(new[] { tooFar });
            Check(!tooFarTry && !hose.IsConnected, "TryConnect and Interact reject a source beyond two horizontal metres.");

            FluidSource blocked = CreateSource(root, "Blocked source", new Vector3(41.5f, .2f, 0f), ElementKind.Water);
            CreateObstacle(root, "0.72m connection wall", new Vector3(40.75f, .36f, 0f), new Vector3(.12f, .72f, 1.2f));
            bool blockedTry = hose.TryConnect(blocked);
            hose.Interact(new[] { blocked });
            Check(!blockedTry && !hose.IsConnected, "TryConnect and Interact reject a source behind a 0.72m low wall.");

            GameObject supportRoot = new GameObject("Source support case");
            supportRoot.transform.SetParent(root.transform);
            FluidSource supported = CreateSource(supportRoot, "Supported source", new Vector3(41.5f, .2f, 2f), ElementKind.Water);
            CreateObstacle(supportRoot, "Source support furniture", new Vector3(41.5f, .3f, 2f), new Vector3(.9f, .6f, .9f));
            Teleport(motor, new Vector3(40f, .05f, 2f));
            bool supportedTry = hose.TryConnect(supported);
            hose.Disconnect();
            hose.Interact(new[] { supported });
            Check(supportedTry && hose.Source == supported,
                "The source collider and the fixture supporting its low endpoint do not falsely block TryConnect or Interact.");

            yield return DestroyFixture(root);
        }

        IEnumerator VerifyOilAndWallHandling()
        {
            GameObject root = CreateRoot("Oil steering and wall collision");
            CreateGround(root, new Vector3(44f, -.1f, 0f), new Vector3(12f, .2f, 8f));
            FireField field = CreateField(root, new Vector3(39f, 0f, -2f), 10, 4);
            Teleport(motor, new Vector3(40f, .05f, 0f));
            motor.Initialize(field, hose);
            hose.Disconnect();
            field.ApplyOil(motor.transform.position, 1f, 3f);
            Check(motor.IsSlipping, "The temporary field applies oil beneath the real motor before steering checks.");

            for (int i = 0; i < 10; i++)
                motor.Move(Vector3.right, .05f);
            Check(Vector3.Dot(motor.Velocity, Vector3.right) >= 2.8f && Vector3.Dot(motor.Velocity, Vector3.right) <= 3.15f,
                "Half a second of held input on oil reaches approximately 3m/s.");

            motor.Move(Vector3.left, .05f);
            Check(Vector3.Dot(motor.Velocity, Vector3.right) > .05f,
                "Oil steering preserves momentum for an immediate opposite-direction command.");

            Teleport(motor, new Vector3(40f, .05f, 0f));
            CreateObstacle(root, "Oil collision wall", new Vector3(41.05f, .55f, 0f), new Vector3(.12f, 1.1f, 1.4f));
            for (int i = 0; i < 12; i++)
                motor.Move(Vector3.right, .05f);
            Check(Vector3.Dot(motor.Velocity, Vector3.right) <= .05f,
                "Controller collision removes velocity and impulse directed into the wall.");

            float beforeRetreat = motor.transform.position.x;
            motor.Move(Vector3.left, .05f);
            Check(motor.transform.position.x < beforeRetreat - .01f,
                "After a wall hit, opposite input immediately moves the firefighter away from the wall.");

            yield return DestroyFixture(root);
        }

        IEnumerator VerifyNpcRouteRecovery()
        {
            Teleport(motor, new Vector3(50f, .05f, 8f));
            GameObject blockedRoot = CreateRoot("NPC route independent of player");
            CreateGround(blockedRoot, new Vector3(43f, -.1f, 0f), new Vector3(8f, .2f, 4f));
            FireField blockedField = CreateField(blockedRoot, new Vector3(39f, 0f, -1f), 8, 2);
            Transform blockedExit = CreateExit(blockedRoot, new Vector3(45.5f, 0f, .5f));
            RescueNpc blockedNpc = CreateNpc(blockedRoot, blockedField, blockedExit, new Vector3(41.5f, .05f, .5f));
            CreateObstacle(blockedRoot, "Unrelated player-side wall", new Vector3(40.75f, .36f, .5f), new Vector3(.12f, .72f, 1.1f));
            blockedNpc.Tick(.01f);
            Check(blockedNpc.HasSafePath && !blockedNpc.IsRescued,
                "NPC discovers a safe route without a nearby player, E input, or crossing the unrelated wall.");
            yield return DestroyFixture(blockedRoot);

            GameObject routeRoot = CreateRoot("NPC route recovery");
            CreateGround(routeRoot, new Vector3(43f, -.1f, 0f), new Vector3(8f, .2f, 2f));
            FireField field = CreateField(routeRoot, new Vector3(40f, 0f, 0f), 6, 1);
            Transform exit = CreateExit(routeRoot, new Vector3(45.5f, 0f, .5f));
            RescueNpc npc = CreateNpc(routeRoot, field, exit, new Vector3(40.5f, .05f, .5f));
            npc.Tick(.01f);
            Check(npc.HasSafePath, "NPC first obtains a safe route along the clear one-cell corridor without player interaction.");

            GameObject temporaryPlayer = new GameObject("Temporary player CharacterController blocker");
            temporaryPlayer.layer = 9;
            temporaryPlayer.transform.SetParent(routeRoot.transform);
            temporaryPlayer.transform.position = new Vector3(42.5f, .05f, .5f);
            CharacterController blocker = temporaryPlayer.AddComponent<CharacterController>();
            blocker.height = 1.6f;
            blocker.center = new Vector3(0f, .8f, 0f);
            blocker.radius = .28f;
            Physics.SyncTransforms();
            for (int i = 0; i < 32; i++)
            {
                npc.Tick(.05f);
                yield return null;
            }
            Check(npc.HasSafePath && !npc.IsRescued && npc.transform.position.x > 41.6f && npc.transform.position.x < 42.25f,
                "A true player CharacterController physically stops the resident on an existing route without invalidating its BFS path.");

            Destroy(temporaryPlayer);
            yield return null;
            for (int i = 0; i < 64 && !npc.IsRescued; i++)
            {
                npc.Tick(.05f);
                yield return null;
            }
            Check(npc.IsRescued,
                "After the player leaves, direct NPC Tick completes the original route to the exit without teleporting the resident.");

            yield return DestroyFixture(routeRoot);
        }

        GameObject CreateRoot(string label)
        {
            GameObject root = new GameObject("Temporary Handling Fixture - " + label);
            temporaryRoots.Add(root);
            return root;
        }

        static FireField CreateField(GameObject root, Vector3 origin, int width, int height)
        {
            FireField field = new GameObject("Temporary flammable field").AddComponent<FireField>();
            field.transform.SetParent(root.transform);
            field.Configure(origin, width, height, 1f);
            field.SetIgnitionSeeds(Array.Empty<Vector2Int>());
            field.ResetSimulation();
            field.Simulate = false;
            return field;
        }

        static ElementReaction CreateReaction(GameObject root, FireField field)
        {
            ElementReaction reaction = new GameObject("Temporary reactions").AddComponent<ElementReaction>();
            reaction.transform.SetParent(root.transform);
            reaction.Initialize(field);
            return reaction;
        }

        static FluidSource CreateSource(GameObject root, string name, Vector3 position, ElementKind element)
        {
            GameObject sourceObject = new GameObject(name);
            sourceObject.transform.SetParent(root.transform);
            sourceObject.transform.position = position;
            sourceObject.layer = FixedObstacleLayer;
            GameObject sourceBody = new GameObject("Source body collider");
            sourceBody.transform.SetParent(sourceObject.transform, false);
            sourceBody.layer = FixedObstacleLayer;
            sourceBody.transform.localPosition = Vector3.up * .15f;
            BoxCollider collider = sourceBody.AddComponent<BoxCollider>();
            collider.size = new Vector3(.35f, .7f, .35f);
            FluidSource source = sourceObject.AddComponent<FluidSource>();
            source.Configure(element);
            Physics.SyncTransforms();
            return source;
        }

        static Transform CreateExit(GameObject root, Vector3 position)
        {
            GameObject exit = new GameObject("Temporary exit");
            exit.transform.SetParent(root.transform);
            exit.transform.position = position;
            return exit.transform;
        }

        static RescueNpc CreateNpc(GameObject root, FireField field, Transform exit, Vector3 position)
        {
            GameObject npcObject = new GameObject("Temporary resident");
            npcObject.transform.SetParent(root.transform);
            npcObject.transform.position = position;
            RescueNpc npc = npcObject.AddComponent<RescueNpc>();
            npc.Autonomous = false;
            npc.Initialize(field, exit);
            Physics.SyncTransforms();
            return npc;
        }

        static void CreateGround(GameObject root, Vector3 position, Vector3 scale)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Temporary ground";
            ground.transform.SetParent(root.transform);
            ground.transform.position = position;
            ground.transform.localScale = scale;
            Physics.SyncTransforms();
        }

        static void CreateObstacle(GameObject root, string name, Vector3 position, Vector3 scale)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = name;
            obstacle.layer = FixedObstacleLayer;
            obstacle.transform.SetParent(root.transform);
            obstacle.transform.position = position;
            obstacle.transform.localScale = scale;
            Physics.SyncTransforms();
        }

        IEnumerator DestroyFixture(GameObject root)
        {
            temporaryRoots.Remove(root);
            if (root) Destroy(root);
            yield return null;
        }

        void CleanupAllFixtures()
        {
            foreach (GameObject root in temporaryRoots)
                if (root) Destroy(root);
            temporaryRoots.Clear();
        }

        static void Teleport(FirefighterMotor player, Vector3 position)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = wasEnabled;
            Physics.SyncTransforms();
        }

        void CheckDirection(Vector3 actual, Vector3 expected, string message)
        {
            Check(actual.sqrMagnitude > .99f && Vector3.Dot(actual.normalized, expected) > .999f, message);
        }

        void Check(bool condition, string message)
        {
            results.Add((condition ? "PASS: " : "FAIL: ") + message);
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
