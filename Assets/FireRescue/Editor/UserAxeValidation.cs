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
    /// Play-mode acceptance for the user-supplied fire axe presentation.  This is intentionally
    /// verification-only: it never authors the scene, changes gameplay rules, or saves assets.
    /// </summary>
    [InitializeOnLoad]
    public static class UserAxeValidation
    {
        const string SessionKey = "FireRescue.UserAxeValidation";
        const string ReportPath = "Docs/Validation/2026-10-21-axe-powder/axe-playmode.txt";
        static bool finished;
        static double readyAt;

        static UserAxeValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false)) return;
                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary User Axe Validation").AddComponent<UserAxeValidationDriver>();
            };
        }

        [MenuItem("FireRescue/Validate User Fire Axe In Residential Scene")]
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
                ValidatePersistedEditScene();
                SessionState.SetBool(SessionKey, true);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception error)
            {
                Finish(false, "Edit-mode setup exception:\n" + error);
            }
        }

        static void ValidatePersistedEditScene()
        {
            GameObject layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
            if (!layout || !layout.activeInHierarchy) throw new InvalidOperationException("Active persisted user layout is missing.");
            Transform player = layout.transform.Find("Gameplay/Player");
            Transform pivot = player ? player.Find("Visual/Axe Pivot") : null;
            Transform model = pivot ? pivot.Find("Fire Axe Model") : null;
            if (!player || !pivot || !model) throw new InvalidOperationException("Player Visual/Axe Pivot/Fire Axe Model is not persisted.");
            if (!PrefabUtility.IsPartOfPrefabInstance(model.gameObject))
                throw new InvalidOperationException("Fire Axe Model is not a prefab instance.");
            if (!pivot.Find("Handle") || !pivot.Find("Head"))
                throw new InvalidOperationException("Legacy Handle and Head must remain under Axe Pivot for reversible fallback.");
            if (pivot.Find("Handle").gameObject.activeSelf || pivot.Find("Head").gameObject.activeSelf)
                throw new InvalidOperationException("Legacy Handle and Head must be disabled when the user axe prefab is active.");
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
            string report = (success ? "USER_AXE_PLAYMODE_PASS" : "USER_AXE_PLAYMODE_FAIL") + "\n" + details;
            File.WriteAllText(ReportPath, report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
                else EditorApplication.ExitPlaymode();
            };
        }
    }

    public sealed class UserAxeValidationDriver : MonoBehaviour
    {
        const float Dt = 1f / 60f;
        const string EvidenceDirectory = "Docs/Validation/2026-10-21-axe-powder";
        readonly List<string> results = new List<string>();
        IEnumerator scenario;
        GameObject layout;
        FirefighterMotor motor;
        LocalPlayerInput input;
        FirefighterTools tools;
        SprayEmitter emitter;
        Transform axePivot;
        GameObject axeModel;
        GameObject nozzleModel;
        Camera gameCamera;
        float started;
        int checks;

        void Start()
        {
            Time.captureDeltaTime = Dt;
            started = Time.realtimeSinceStartup;
            scenario = Scenario();
        }

        void OnDestroy()
        {
            Time.captureDeltaTime = 0f;
        }

        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 75f)
                    throw new TimeoutException("User axe play-mode validation timed out.");
                if (!scenario.MoveNext())
                {
                    enabled = false;
                    UserAxeValidation.Finish(true, "Actual command-path checks=" + checks + "\n" + string.Join("\n", results));
                }
            }
            catch (Exception error)
            {
                enabled = false;
                Capture("failure.png");
                UserAxeValidation.Finish(false, "Checks passed before failure=" + checks + "\n" + string.Join("\n", results) + "\n" + error);
            }
        }

        IEnumerator Scenario()
        {
            yield return null;
            layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
            Transform player = layout ? layout.transform.Find("Gameplay/Player") : null;
            motor = player ? player.GetComponent<FirefighterMotor>() : null;
            input = player ? player.GetComponent<LocalPlayerInput>() : null;
            tools = player ? player.GetComponent<FirefighterTools>() : null;
            emitter = player ? player.GetComponent<SprayEmitter>() : null;
            axePivot = player ? player.Find("Visual/Axe Pivot") : null;
            Transform modelTransform = axePivot ? axePivot.Find("Fire Axe Model") : null;
            axeModel = modelTransform ? modelTransform.gameObject : null;
            Transform nozzle = player ? player.Find("Visual/Nozzle Model") : null;
            nozzleModel = nozzle ? nozzle.gameObject : null;
            gameCamera = Camera.main;
            Check(layout && layout.activeInHierarchy && motor && input && tools && emitter && axePivot && axeModel && nozzleModel && gameCamera,
                "Persisted active layout exposes the real player, tool core, axe presentation, hose nozzle, and Game camera.");

            input.ReadDevices = false;
            input.SetInputFocus(true);
            Check(input.isActiveAndEnabled && !input.ReadDevices, "Input command adapter is active while hardware reads are disabled.");
            Check(axePivot.localPosition == new Vector3(.3f, .92f, .25f), "Axe Pivot retains the authored gameplay mount position.");
            Check(!axePivot.Find("Handle").gameObject.activeSelf && !axePivot.Find("Head").gameObject.activeSelf,
                "Legacy Handle and Head stay disabled but preserved below the pivot.");
            Check(axeModel.GetComponentsInChildren<Renderer>(true).Length > 0, "User Fire Axe prefab provides a renderable model.");
            Bounds localBounds = BoundsInSpace(axeModel.GetComponentsInChildren<Renderer>(true), axePivot);
            Check(localBounds.size.y > .62f && localBounds.size.y < .84f && Mathf.Abs(localBounds.min.y + .12f) < .08f,
                "Axe visual spans approximately 0.72m with handle bottom 0.12m below the grip origin (actual " + localBounds.size.y.ToString("F3") + ", " + localBounds.min.y.ToString("F3") + ").");
            Check(axeModel.GetComponentsInChildren<Collider>(true).Length == 0 && axeModel.GetComponentsInChildren<Rigidbody>(true).Length == 0,
                "User axe presentation contains no collision or rigidbody gameplay physics.");
            Check(!axeModel.GetComponentsInChildren<MonoBehaviour>(true).Any(component => component is FirefighterMotor || component is LocalPlayerInput || component is FirefighterTools || component is SprayEmitter || component is HoseConnection),
                "User axe presentation contains no gameplay component; mechanics remain on the player root.");

            Teleport(new Vector3(5.74f, .05f, -7.5f), Vector3.back);
            yield return null;
            Command(true, false);
            yield return null;
            yield return null;
            Check(axeModel.activeInHierarchy && tools.SelectedTool == FirefighterTools.Tool.Axe && tools.IsSwinging,
                "LMB command equips the user axe and starts a gameplay swing.");
            BoxCollider target = CreateTarget();
            Physics.SyncTransforms();
            int hitsBefore = tools.HitCount;
            for (int i = 0; i < 18; i++)
            {
                Command(true, false);
                yield return null;
            }
            Check(tools.HitCount == hitsBefore + 1 && tools.LastHit.collider == target,
                "Actual LMB swing hits the temporary layer-8 target through the existing gameplay query.");
            Capture("02-swing-at-hit.png");
            CaptureCloseup("05-swing-closeup.png");

            int swingsBefore = tools.SwingCount;
            for (int i = 0; i < 70; i++)
            {
                Command(true, false);
                yield return null;
            }
            Check(tools.SwingCount >= swingsBefore + 2 && tools.IsSwinging,
                "Holding LMB chains more than one complete axe swing cycle.");
            Command(false, false);
            yield return null;
            yield return null;
            Check(!tools.IsSwinging && tools.SelectedTool == FirefighterTools.Tool.Axe && axeModel.activeInHierarchy,
                "Releasing LMB stops the swing while keeping the axe equipped.");
            Capture("01-idle-equipped.png");
            CaptureCloseup("04-equipped-closeup.png");

            Command(false, true);
            yield return null;
            yield return null;
            Check(tools.SelectedTool == FirefighterTools.Tool.Hose && !tools.IsSwinging && !axeModel.activeInHierarchy && nozzleModel.activeInHierarchy,
                "RMB switches presentation to the hose: axe hidden and nozzle visible.");
            Command(true, true);
            yield return null;
            Check(tools.SelectedTool == FirefighterTools.Tool.Hose && !tools.IsSwinging,
                "Simultaneous clicks keep the specified RMB hose priority.");
            Capture("03-hose-switched.png");

            Command(true, false);
            yield return null;
            yield return null;
            Check(axeModel.activeInHierarchy, "Axe visual returns when switching back from hose.");
            axeModel.SetActive(false);
            Physics.SyncTransforms();
            hitsBefore = tools.HitCount;
            for (int i = 0; i < 20; i++)
            {
                Command(true, false);
                yield return null;
            }
            Check(tools.HitCount == hitsBefore + 1 && tools.LastHit.collider == target,
                "Disabling the new axe visual during Play Mode does not stop core axe hits or alter its target query.");

        }

        BoxCollider CreateTarget()
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "Temporary axe command target";
            target.layer = 8;
            target.transform.position = motor.transform.position + motor.transform.forward * 1f + Vector3.up * .8f;
            target.transform.localScale = new Vector3(.3f, .8f, .3f);
            return target.GetComponent<BoxCollider>();
        }

        void Teleport(Vector3 position, Vector3 forward)
        {
            CharacterController controller = motor.GetComponent<CharacterController>();
            bool wasEnabled = controller && controller.enabled;
            if (controller) controller.enabled = false;
            motor.transform.position = position;
            motor.transform.forward = forward;
            if (controller) controller.enabled = wasEnabled;
            Physics.SyncTransforms();
        }

        void Command(bool axeHeld, bool sprayHeld)
        {
            input.TickCommand(Vector2.zero, motor.transform.position + motor.transform.forward * 3f,
                false, false, axeHeld, sprayHeld, Dt);
        }

        static Bounds BoundsInSpace(IEnumerable<Renderer> renderers, Transform space)
        {
            bool hasBounds = false;
            Bounds result = default;
            foreach (Renderer renderer in renderers)
            {
                Bounds world = renderer.bounds;
                Vector3 min = world.min;
                Vector3 max = world.max;
                for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                {
                    Vector3 point = space.InverseTransformPoint(new Vector3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y, z == 0 ? min.z : max.z));
                    if (!hasBounds) { result = new Bounds(point, Vector3.zero); hasBounds = true; }
                    else result.Encapsulate(point);
                }
            }
            if (!hasBounds) throw new InvalidOperationException("Unable to calculate Fire Axe renderer bounds.");
            return result;
        }

        void CaptureCloseup(string name)
        {
            // Same real scene/materials and camera angle; temporary zoom for asset QA only.
            Vector3 position = gameCamera.transform.position;
            float size = gameCamera.orthographicSize;
            try
            {
                gameCamera.orthographicSize = 1.35f;
                gameCamera.transform.position = motor.transform.position + Vector3.up * .85f - gameCamera.transform.forward * 18;
                Capture(name);
            }
            finally
            {
                gameCamera.transform.position = position;
                gameCamera.orthographicSize = size;
            }
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
