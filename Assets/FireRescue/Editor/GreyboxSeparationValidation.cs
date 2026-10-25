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
    /// Play Mode coverage for the boundary between the generated greybox's presentation
    /// hierarchy and its colliders, anchors, and gameplay components.  The test scene is
    /// intentionally transient: it never opens or writes a product scene.
    /// </summary>
    [InitializeOnLoad]
    public static class GreyboxSeparationValidation
    {
        const string Key = "FireRescue.GreyboxSeparationValidation";
        const string ReportPath = "Docs/Validation/2026-10-12-greybox-step2/separation-playmode.txt";
        static bool finished;
        static double batchReadyAt;

        static GreyboxSeparationValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false))
                    return;

                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary Greybox Separation Validation")
                    .AddComponent<GreyboxSeparationValidationDriver>();
            };
        }

        [MenuItem("消防 Prototype/驗證灰盒模型與 Gameplay 分離")]
        public static void Run()
        {
            Begin(false);
        }

        // Kept public for -executeMethod.  It creates missing prefabs before entering a
        // disposable empty scene, so a batch run cannot modify a hand-authored scene.
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
                EditorApplication.Exit(1);
            }
        }

        static void Begin(bool batch)
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit Play Mode before running greybox separation validation.");
            if (!batch && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            GreyboxPrefabBuilder.CreateAssets();
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
            string report = (success ? "GREYBOX_SEPARATION_PLAYMODE_PASS" : "GREYBOX_SEPARATION_PLAYMODE_FAIL") +
                "\n" + details;
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

    public sealed class GreyboxSeparationValidationDriver : MonoBehaviour
    {
        const int FixedObstacleLayer = 8;
        const float PositionTolerance = .015f;

        enum VisualVariant
        {
            Primitive,
            Disabled,
            Replaced
        }

        sealed class ColliderSnapshot
        {
            public Collider Collider;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        sealed class AnchorSnapshot
        {
            public Transform Anchor;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        readonly List<string> results = new List<string>();
        readonly List<GameObject> replacementVisuals = new List<GameObject>();

        GameObject assembly;
        FirefighterMotor motor;
        HoseConnection hose;
        SprayEmitter emitter;
        FireField field;
        ElementReaction reactions;
        RescueNpc resident;
        RescueGameManager manager;
        FluidSource water;
        FluidSource oil;
        CharacterController playerController;
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
            CleanupAssembly();
        }

        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 80f)
                    throw new TimeoutException("Greybox separation validation timed out after 80 seconds.");

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
                CleanupAssembly();
                GreyboxSeparationValidation.Finish(false,
                    "Actual checks passed before failure: " + checks + "\n" + string.Join("\n", results) + "\n" + error);
            }
        }

        IEnumerator Scenario()
        {
            yield return null;
            yield return VerifyVariant(VisualVariant.Primitive);
            yield return VerifyVariant(VisualVariant.Disabled);
            yield return VerifyVariant(VisualVariant.Replaced);

            enabled = false;
            GreyboxSeparationValidation.Finish(true,
                "Actual Play Mode checks: " + checks + "\n" + string.Join("\n", results));
        }

        IEnumerator VerifyVariant(VisualVariant variant)
        {
            SetupAssembly();
            yield return null;

            List<ColliderSnapshot> colliders = CaptureGameplayColliders();
            List<AnchorSnapshot> anchors = CaptureAnchors();
            int visualRootCount = DirectVisualRoots().Count;
            Check(visualRootCount >= 9, VariantName(variant) + " assembly exposes direct Visual roots for all greybox prefabs.");

            if (variant == VisualVariant.Disabled)
            {
                foreach (Transform visual in DirectVisualRoots())
                    visual.gameObject.SetActive(false);
            }
            else if (variant == VisualVariant.Replaced)
            {
                ReplaceVisuals();
                yield return null; // Destroy(collider) on the temporary primitive completes at frame end.
                foreach (GameObject visual in replacementVisuals)
                    Check(visual.GetComponentsInChildren<Collider>(true).Length == 0,
                        "Replacement renderer '" + visual.name + "' has no Collider.");
            }

            VerifyGameplayStructureWasNotMoved(colliders, anchors, variant);
            yield return VerifyFireFieldPresentation(variant);
            yield return VerifyMotorAndHose(variant);
            yield return VerifyWaterAndOilGameplay(variant);
            yield return VerifyResidentEscape(variant);

            if (variant == VisualVariant.Disabled)
                yield return VerifyHiddenVisualNegativeCases();

            CleanupAssembly();
            yield return null;
        }

        void SetupAssembly()
        {
            CleanupAssembly();
            assembly = GreyboxPrefabBuilder.CreateTestAssembly();
            Check(assembly && assembly.activeInHierarchy, "CreateTestAssembly returns an active fresh assembly.");

            motor = RequireComponent<FirefighterMotor>("Player motor");
            hose = RequireComponent<HoseConnection>("Player hose");
            emitter = RequireComponent<SprayEmitter>("Player spray emitter");
            playerController = motor.GetComponent<CharacterController>();
            Check(playerController != null, "Player controller exists on the Player motor root.");
            field = RequireComponent<FireField>("FireField");
            reactions = RequireComponent<ElementReaction>("ElementReaction");
            resident = RequireComponent<RescueNpc>("Resident");
            manager = RequireComponent<RescueGameManager>("RescueGameManager");
            water = FindSource(ElementKind.Water);
            oil = FindSource(ElementKind.Oil);
            Check(water && oil, "Assembly contains both real water and oil FluidSource components.");
            Check(assembly.GetComponentInChildren<LocalPlayerInput>(true) == null,
                "Validation assembly has no LocalPlayerInput; commands are supplied directly.");

            field.Simulate = false;
            resident.Autonomous = false;
            Physics.SyncTransforms();
        }

        IEnumerator VerifyMotorAndHose(VisualVariant variant)
        {
            Teleport(motor.transform, new Vector3(0f, .05f, -2.5f), Vector3.forward);
            Vector3 startedAt = motor.transform.position;
            for (int i = 0; i < 30; i++)
                motor.Move(Vector3.forward, .02f);
            Check((motor.transform.position - startedAt).sqrMagnitude > .08f,
                VariantName(variant) + " visual leaves the real FirefighterMotor movable.");

            Teleport(motor.transform, new Vector3(0f, .05f, -2.5f), Vector3.forward);
            hose.Disconnect();
            Check(hose.TryConnect(water) && hose.Source == water,
                VariantName(variant) + " player can connect the actual hose to water.");

            for (int i = 0; i < 240; i++)
                motor.Move(Vector3.back, .05f);
            Vector3 flatPlayer = motor.transform.position;
            flatPlayer.y = water.transform.position.y;
            Check(Vector3.Distance(flatPlayer, water.transform.position) <= hose.MaximumLength + .08f,
                VariantName(variant) + " motor movement obeys the real " + hose.MaximumLength + "m hose limit.");
            Check(motor.transform.position.z < water.transform.position.z - hose.MaximumLength + .5f,
                VariantName(variant) + " motor actually reached the hose-limit boundary rather than only evaluating it.");

            Teleport(motor.transform, new Vector3(0f, .05f, -2.5f), Vector3.forward);
            yield return null;
        }

        IEnumerator VerifyFireFieldPresentation(VisualVariant variant)
        {
            FireFieldView view = RequireComponent<FireFieldView>("FireFieldView");
            yield return null; // Lets FireFieldView.LateUpdate materialize the currently burning cell.

            if (variant == VisualVariant.Disabled)
            {
                float before = field.GetCell(5, 5).Intensity;
                field.Step(.2f);
                Check(field.GetCell(5, 5).Intensity < before,
                    "Disabled Visual does not stop the FireField simulation from stepping.");
                yield break;
            }

            bool visibleFire = false;
            foreach (Renderer renderer in view.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.gameObject.name == "Fire" && renderer.gameObject.activeInHierarchy)
                {
                    visibleFire = true;
                    break;
                }
            }
            Check(visibleFire, VariantName(variant) + " FireFieldView generates a visible fire Renderer from the bound field.");
            if (variant == VisualVariant.Replaced)
            {
                bool rehomed = false;
                foreach (GameObject replacement in replacementVisuals)
                {
                    if (view.transform.IsChildOf(replacement.transform))
                    {
                        rehomed = true;
                        break;
                    }
                }
                Check(rehomed, "Replacement Visual keeps FireFieldView under a replacement renderer hierarchy after Initialize(field).");
            }
        }

        IEnumerator VerifyWaterAndOilGameplay(VisualVariant variant)
        {
            hose.Disconnect();
            Check(hose.TryConnect(water), VariantName(variant) + " reconnects water before live spraying.");
            Vector3 firePoint = field.CellCenter(5, 5);
            float beforeFire = field.GetCell(5, 5).Intensity;
            Check(beforeFire > .05f, VariantName(variant) + " begins with the authored floor fire burning.");
            for (int i = 0; i < 20; i++)
                emitter.Tick(firePoint, true, .1f);
            float afterFire = field.GetCell(5, 5).Intensity;
            Check(emitter.HasImpact && Mathf.Abs(emitter.ImpactPoint.y) < .18f,
                VariantName(variant) + " SprayEmitter raycasts to the actual ground collider.");
            Check(afterFire <= .05f && field.IsSafe(new Vector2Int(5, 5)),
                VariantName(variant) + " water spray actually extinguishes the floor fire to a safe state.");

            Teleport(motor.transform, new Vector3(.5f, .05f, -1.5f), Vector3.forward);
            hose.Disconnect();
            Check(hose.TryConnect(oil) && hose.Source == oil,
                VariantName(variant) + " player can switch the live hose to oil.");
            Vector3 oilAim = new Vector3(.5f, 0f, -1.5f);
            for (int i = 0; i < 6; i++)
                emitter.Tick(oilAim, true, .1f);
            Check(emitter.HasImpact && field.GetOilAt(motor.transform.position) > .08f && motor.IsSlipping,
                VariantName(variant) + " oil spray hits ground and puts the real motor into slipping state.");
            emitter.Stop();
            yield return null;
        }

        IEnumerator VerifyResidentEscape(VisualVariant variant)
        {
            field.SetIgnitionSeeds(Array.Empty<Vector2Int>());
            field.ResetSimulation();
            field.Simulate = false;
            hose.Disconnect();
            Teleport(motor.transform, new Vector3(-7f, .05f, 7f), Vector3.forward);
            resident.Autonomous = true;

            float waitStarted = Time.realtimeSinceStartup;
            while (!resident.IsRescued && Time.realtimeSinceStartup - waitStarted < 12f)
                yield return null;
            Check(resident.IsRescued,
                VariantName(variant) + " resident autonomously reaches the real exit after its route is safe.");
            Check(manager.RescuedCount == 1,
                VariantName(variant) + " RescueGameManager records exactly one actual rescue.");
        }

        IEnumerator VerifyHiddenVisualNegativeCases()
        {
            // Build a fresh disabled-Visual fixture because the positive cases intentionally
            // clear the field and rescue the resident.
            CleanupAssembly();
            yield return null;
            SetupAssembly();
            foreach (Transform visual in DirectVisualRoots())
                visual.gameObject.SetActive(false);
            Physics.SyncTransforms();

            Collider wall = FindFixedObstacle();
            Teleport(motor.transform, new Vector3(wall.bounds.center.x, .05f, wall.bounds.min.z - 1.4f), Vector3.forward);
            for (int i = 0; i < 100; i++)
                motor.Move(Vector3.forward, .02f);
            Check(motor.transform.position.z < wall.bounds.min.z + .12f,
                "Disabled Visual still leaves the FixedObstacle collider blocking real motor movement.");

            Teleport(motor.transform, water.transform.position + Vector3.forward * 1.1f, Vector3.forward);
            hose.Disconnect();
            Check(hose.TryConnect(water), "Hidden-Visual water setup connects before obstacle tests.");
            Teleport(motor.transform, new Vector3(wall.bounds.center.x, .05f, wall.bounds.min.z - 1.6f), Vector3.forward);
            field.SetIgnitionSeeds(Array.Empty<Vector2Int>());
            field.ResetSimulation();
            Vector3 behindWall = field.CellCenter(
                Mathf.Clamp(Mathf.FloorToInt((wall.bounds.center.x - field.Origin.x) / field.CellSize), 0, field.Width - 1),
                Mathf.Clamp(Mathf.FloorToInt((wall.bounds.max.z + .1f - field.Origin.z) / field.CellSize), 0, field.Height - 1));
            field.Ignite(behindWall, .8f);
            field.Simulate = false;
            Check(field.TryGetCell(behindWall, out Vector2Int fireCell), "Blocked-spray target resolves to a real FireField cell.");
            float blockedBefore = field.GetCell(fireCell.x, fireCell.y).Intensity;
            for (int i = 0; i < 8; i++)
                emitter.Tick(behindWall, true, .1f);
            float blockedAfter = field.GetCell(fireCell.x, fireCell.y).Intensity;
            Check(emitter.HasImpact && emitter.ImpactPoint.z < wall.bounds.max.z + .05f &&
                Mathf.Abs(blockedAfter - blockedBefore) < .01f,
                "Disabled Visual wall blocks spray, so fire behind its collider is not hit.");

            Rigidbody pushable = FindPushable();
            Teleport(motor.transform, water.transform.position + Vector3.forward * 1.1f, Vector3.forward);
            hose.Disconnect();
            Check(hose.TryConnect(water), "Hidden-Visual pushable setup connects water.");
            Vector3 start = pushable.position;
            Teleport(motor.transform, pushable.position + Vector3.back * 2f, (pushable.position - motor.transform.position).normalized);
            for (int i = 0; i < 10; i++)
                emitter.Tick(pushable.worldCenterOfMass, true, .1f);
            Check(emitter.HasImpact && pushable.GetComponent<Collider>().bounds.SqrDistance(emitter.ImpactPoint) < .001f,
                "Hidden-Visual spray hits the actual Pushable collider before measuring its motion.");
            float fixedStarted = Time.fixedTime;
            float physicsDeadline = Time.realtimeSinceStartup + 2f;
            // One .02s tick moves a 3kg body by less than .1m even after a valid impulse.
            // Observe several actual physics ticks, keeping the displacement criterion intact.
            while (Time.fixedTime - fixedStarted < .12f && Time.realtimeSinceStartup < physicsDeadline)
                yield return null;
            results.Add("MEASURE: hidden-Visual push displacement=" + Vector3.Distance(pushable.position, start).ToString("F4") +
                "m; physics duration=" + (Time.fixedTime - fixedStarted).ToString("F4") + "s.");
            Check(Time.fixedTime - fixedStarted >= .1f, "Physics advanced before Pushable displacement was evaluated.");
            Check((pushable.position - start).sqrMagnitude > .01f,
                "Water spray still imparts physical movement to Pushable when Visual is disabled.");
        }

        void ReplaceVisuals()
        {
            replacementVisuals.Clear();
            List<Transform> visuals = DirectVisualRoots();
            GameObject fireReplacement = null;
            foreach (Transform visual in visuals)
            {
                Transform parent = visual.parent;
                GameObject replacement = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                replacement.name = "RendererOnly_" + parent.name;
                replacement.transform.SetParent(parent, false);
                replacement.transform.localPosition = new Vector3(.17f, .31f, -.23f);
                replacement.transform.localRotation = Quaternion.Euler(0f, 31f, 0f);
                replacement.transform.localScale = new Vector3(.37f, .64f, .51f);
                Collider temporaryCollider = replacement.GetComponent<Collider>();
                if (temporaryCollider != null)
                    Destroy(temporaryCollider);
                replacementVisuals.Add(replacement);
                if (parent == field.transform)
                    fireReplacement = replacement;
                Destroy(visual.gameObject);
            }

            if (fireReplacement == null)
                throw new InvalidOperationException("FireSystem replacement Visual was not created.");
            GameObject rebuiltViewRoot = new GameObject("FireFieldView");
            rebuiltViewRoot.transform.SetParent(fireReplacement.transform, false);
            rebuiltViewRoot.AddComponent<FireFieldView>().Initialize(field);
        }

        List<ColliderSnapshot> CaptureGameplayColliders()
        {
            List<ColliderSnapshot> snapshots = new List<ColliderSnapshot>();
            foreach (Collider collider in assembly.GetComponentsInChildren<Collider>(true))
            {
                if (IsInsideDirectVisual(collider.transform))
                    continue;
                snapshots.Add(new ColliderSnapshot
                {
                    Collider = collider,
                    Position = collider.transform.position,
                    Rotation = collider.transform.rotation,
                    Scale = collider.transform.lossyScale
                });
            }
            Check(snapshots.Count >= 7, "Assembly exposes gameplay colliders outside all Visual roots.");
            return snapshots;
        }

        List<AnchorSnapshot> CaptureAnchors()
        {
            List<AnchorSnapshot> snapshots = new List<AnchorSnapshot>();
            foreach (Transform candidate in assembly.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != "Nozzle" && candidate.name != "Interaction" &&
                    candidate.name != "Outlet" && candidate.name != "Arrival")
                    continue;
                snapshots.Add(new AnchorSnapshot
                {
                    Anchor = candidate,
                    Position = candidate.position,
                    Rotation = candidate.rotation
                });
            }
            Check(snapshots.Count >= 6, "Assembly exposes Nozzle, interaction, outlet, and exit anchors outside models.");
            return snapshots;
        }

        void VerifyGameplayStructureWasNotMoved(List<ColliderSnapshot> colliders, List<AnchorSnapshot> anchors,
            VisualVariant variant)
        {
            foreach (ColliderSnapshot snapshot in colliders)
            {
                Check(snapshot.Collider && snapshot.Collider.enabled &&
                    Vector3.Distance(snapshot.Collider.transform.position, snapshot.Position) < PositionTolerance &&
                    Quaternion.Angle(snapshot.Collider.transform.rotation, snapshot.Rotation) < .1f &&
                    Vector3.Distance(snapshot.Collider.transform.lossyScale, snapshot.Scale) < PositionTolerance,
                    VariantName(variant) + " preserves gameplay collider " + snapshot.Collider.name + ".");
            }
            foreach (AnchorSnapshot snapshot in anchors)
            {
                Check(snapshot.Anchor && Vector3.Distance(snapshot.Anchor.position, snapshot.Position) < PositionTolerance &&
                    Quaternion.Angle(snapshot.Anchor.rotation, snapshot.Rotation) < .1f,
                    VariantName(variant) + " preserves gameplay anchor " + snapshot.Anchor.name + ".");
            }
            AnchorSnapshot waterOutlet = FindAnchor(anchors, water.transform, "Outlet");
            AnchorSnapshot oilOutlet = FindAnchor(anchors, oil.transform, "Outlet");
            AnchorSnapshot nozzle = FindAnchor(anchors, motor.transform, "Nozzle");
            Check(Vector3.Distance(water.Port, waterOutlet.Position) < PositionTolerance &&
                Vector3.Distance(oil.Port, oilOutlet.Position) < PositionTolerance &&
                Vector3.Distance(emitter.NozzlePosition, nozzle.Position) < PositionTolerance,
                VariantName(variant) + " preserves water, oil, and nozzle exposed connection endpoints.");
        }

        static AnchorSnapshot FindAnchor(List<AnchorSnapshot> snapshots, Transform root, string name)
        {
            foreach (AnchorSnapshot snapshot in snapshots)
                if (snapshot.Anchor.name == name && snapshot.Anchor.IsChildOf(root))
                    return snapshot;
            throw new InvalidOperationException("Missing " + name + " anchor beneath " + root.name + ".");
        }

        List<Transform> DirectVisualRoots()
        {
            List<Transform> roots = new List<Transform>();
            foreach (Transform candidate in assembly.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == "Visual" && candidate.parent != null)
                    roots.Add(candidate);
            }
            return roots;
        }

        bool IsInsideDirectVisual(Transform candidate)
        {
            for (Transform current = candidate; current != null && current != assembly.transform; current = current.parent)
            {
                if (current.name == "Visual")
                    return true;
            }
            return false;
        }

        T RequireComponent<T>(string label) where T : Component
        {
            T component = assembly.GetComponentInChildren<T>(true);
            Check(component != null, label + " exists on the created test assembly.");
            return component;
        }

        FluidSource FindSource(ElementKind kind)
        {
            foreach (FluidSource source in assembly.GetComponentsInChildren<FluidSource>(true))
            {
                if (source.Element == kind)
                    return source;
            }
            throw new InvalidOperationException("No " + kind + " FluidSource exists in the test assembly.");
        }

        Collider FindFixedObstacle()
        {
            foreach (Collider collider in assembly.GetComponentsInChildren<Collider>(true))
            {
                if (collider.gameObject.layer == FixedObstacleLayer && collider.enabled &&
                    collider.GetComponentInParent<FluidSource>() == null)
                    return collider;
            }
            throw new InvalidOperationException("No enabled FixedObstacle collider exists in the test assembly.");
        }

        Rigidbody FindPushable()
        {
            foreach (Rigidbody body in assembly.GetComponentsInChildren<Rigidbody>(true))
            {
                if (!body.isKinematic)
                    return body;
            }
            throw new InvalidOperationException("No non-kinematic Rigidbody exists for Pushable validation.");
        }

        void Teleport(Transform target, Vector3 position, Vector3 forward)
        {
            bool wasController = target == motor.transform && playerController.enabled;
            if (wasController)
                playerController.enabled = false;
            target.position = position;
            if (forward.sqrMagnitude > .001f)
                target.forward = new Vector3(forward.x, 0f, forward.z).normalized;
            if (wasController)
                playerController.enabled = true;
            Physics.SyncTransforms();
        }

        void CleanupAssembly()
        {
            replacementVisuals.Clear();
            if (assembly != null)
            {
                assembly.SetActive(false);
                Destroy(assembly);
            }
            assembly = null;
        }

        void Check(bool condition, string description)
        {
            if (!condition)
            {
                results.Add("FAIL: " + description);
                throw new InvalidOperationException(description);
            }
            checks++;
            results.Add("PASS: " + description);
        }

        static string VariantName(VisualVariant variant)
        {
            switch (variant)
            {
                case VisualVariant.Primitive: return "Original primitive Visual";
                case VisualVariant.Disabled: return "Disabled Visual";
                default: return "Renamed replacement renderer Visual";
            }
        }
    }
}
