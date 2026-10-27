using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    // Runtime-only verification for the scene installation.  It deliberately never saves the scene or prefabs.
    [InitializeOnLoad]
    public static class PlayerWhiteboxValidation
    {
        const string Key = "FireRescue.PlayerWhiteboxCameraTest";
        static bool finished;

        static PlayerWhiteboxValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary player whitebox camera tests").AddComponent<PlayerWhiteboxCameraDriver>();
            };
        }

        [MenuItem("FireRescue/驗證/主角白模與相機 Play Mode 驗證")]
        public static void BatchRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }

        static void OnLog(string text, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
                Finish(false, text + "\n" + trace);
        }

        public static void Finish(bool passed, string details)
        {
            if (finished) return;
            finished = true;
            Application.logMessageReceived -= OnLog;
            SessionState.SetBool(Key, false);
            Directory.CreateDirectory(PlayerWhiteboxSetup.Evidence);
            File.WriteAllText(PlayerWhiteboxSetup.Evidence + "/player-camera-playmode.txt",
                (passed ? "PLAYER_WHITEBOX_CAMERA_PLAYMODE_PASS\n" : "FAIL\n") + details);
            EditorApplication.delayCall += () => EditorApplication.Exit(passed ? 0 : 1);
        }
    }

    public sealed class PlayerWhiteboxCameraDriver : MonoBehaviour
    {
        readonly List<string> checks = new List<string>();
        float started;

        void Start()
        {
            started = Time.realtimeSinceStartup;
            Time.captureDeltaTime = 1f / 60f;
            StartCoroutine(Scenario());
        }

        void Update()
        {
            if (Time.realtimeSinceStartup - started > 60f)
                PlayerWhiteboxValidation.Finish(false, string.Join("\n", checks) + "\nTimeout");
        }

        void OnDestroy() { Time.captureDeltaTime = 0; }

        void Check(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(string.Join("\n", checks) + "\nFAIL " + label);
            checks.Add("PASS " + label);
        }

        IEnumerator Scenario()
        {
            yield return null; // Let scene Awake/Start and the first camera LateUpdate settle.

                var player = PlayerWhiteboxSetup.Player();
                Check(player && player.gameObject.scene.name == "TutorialGreybox", "TutorialGreybox contains the actual player motor");
                var visual = player.transform.Find("Visual");
                var model = visual ? visual.Find(PlayerWhiteboxSetup.VisualName) : null;
                Check(visual && model, "Player has the installed User Player Model presentation");
                PlayerWhiteboxSetup.ValidateVisual(model.gameObject);
                Check(model.GetComponentsInChildren<MeshFilter>(true).Length == 10, "New static FBX retains all ten mesh pieces");
                Check(model.GetComponentsInChildren<Collider>(true).Length == 0 &&
                    model.GetComponentsInChildren<Rigidbody>(true).Length == 0 &&
                    model.GetComponentsInChildren<MonoBehaviour>(true).Length == 0, "New visual has no collision, rigidbody, or behaviour authority");
                Check(!visual.Find("Body").gameObject.activeSelf && !visual.Find("Helmet").gameObject.activeSelf,
                    "Old capsule body and helmet remain inactive backups");
                var resident = GameObject.Find(UserLevelOneBuilder.LayoutName).GetComponentInChildren<RescueNpc>(true);
                Check(resident && resident.transform.Find("Visual/" + UserCharacterSetup.VisualName),
                    "Existing User Resident 2 NPC presentation remains in the tutorial");

                var camera = Camera.main;
                var follower = camera ? camera.GetComponent<RescueCamera>() : null;
                Check(camera && follower && camera.orthographic && Mathf.Abs(camera.orthographicSize - PlayerWhiteboxSetup.ViewSize) < .001f,
                    "Current camera is orthographic at size 6");
                Check(Quaternion.Angle(camera.transform.rotation, Quaternion.Euler(PlayerWhiteboxSetup.ViewPitch, 0, 0)) < .01f,
                    "Current camera uses pitch 50 and yaw 0");

                var input = player.GetComponentInChildren<LocalPlayerInput>(true);
                Check(input && player.GetComponent<CharacterController>(), "Actual LocalPlayerInput and CharacterController remain on the gameplay player");
                input.ReadDevices = false;
                input.SetInputFocus(true);
                Check(!input.ReadDevices, "Physical device reads are disabled for deterministic command testing");

                Vector3 beforeForward = player.transform.position;
                yield return Tick(input, Vector2.up, beforeForward + Vector3.forward * 5, .3f);
                Vector3 forwardDelta = player.transform.position - beforeForward;
                Check(forwardDelta.z > .35f && Mathf.Abs(forwardDelta.z) > Mathf.Abs(forwardDelta.x) * 3f,
                    "TickPointerCommand W moves camera-relative forward along world +Z near the spawn");

                Vector3 beforeRight = player.transform.position;
                yield return Tick(input, Vector2.right, beforeRight + Vector3.right * 5, .3f);
                Vector3 rightDelta = player.transform.position - beforeRight;
                Check(rightDelta.x > .35f && Mathf.Abs(rightDelta.x) > Mathf.Abs(rightDelta.z) * 3f,
                    "TickPointerCommand D moves camera-relative right along world +X");

                Quaternion cameraBeforeFace = camera.transform.rotation;
                yield return Tick(input, Vector2.zero, player.transform.position + Vector3.right * 5, .12f);
                Check(Quaternion.Angle(cameraBeforeFace, camera.transform.rotation) < .01f,
                    "Turning the firefighter through the real input path does not rotate the camera");

                visual.gameObject.SetActive(false);
                Vector3 hiddenStart = player.transform.position;
                // After W then D, +Z intersects the real front wall outside the doorway.
                // Test visual independence toward the clear exterior, not against that wall.
                yield return Tick(input, Vector2.down, hiddenStart - Vector3.forward * 5, .3f);
                Check((player.transform.position - hiddenStart).sqrMagnitude > .12f,
                    "Gameplay player still moves into the clear exterior while the presentation visual is disabled");
                visual.gameObject.SetActive(true);
                Check(model.gameObject.activeSelf && player.GetComponent<CharacterController>() && input.isActiveAndEnabled,
                    "Re-enabling visual leaves the same gameplay authority intact");

                yield return Tick(input, Vector2.zero, player.transform.position, .3f);
                yield return new WaitForSeconds(.8f);
                Vector3 viewport = camera.WorldToViewportPoint(player.transform.position + Vector3.up * .7f);
                Check(Vector2.Distance(new Vector2(viewport.x, viewport.y), new Vector2(.5f, .5f)) < .025f && viewport.z > 0,
                    "Fixed-angle smooth follow settles root plus 0.7m near viewport centre");

                var legacyObject = new GameObject("Temporary legacy RescueCamera");
                try
                {
                    var legacyCamera = legacyObject.AddComponent<Camera>();
                    var legacy = legacyObject.AddComponent<RescueCamera>();
                    yield return null;
                    Check(Quaternion.Angle(legacyCamera.transform.rotation, Quaternion.Euler(50, 40, 0)) < .01f && legacy,
                        "A new RescueCamera without ConfigureView preserves legacy 50/40 view angles");
                }
                finally { Destroy(legacyObject); }

                PlayerWhiteboxValidation.Finish(true, "Checks=" + checks.Count + "\n" + string.Join("\n", checks));
        }

        static IEnumerator Tick(LocalPlayerInput input, Vector2 movement, Vector3 aimPoint, float seconds)
        {
            int frames = Mathf.CeilToInt(seconds * 60f);
            for (int i = 0; i < frames; i++)
            {
                input.TickPointerCommand(movement, aimPoint, false, false, false, false, false, false, 1f / 60f);
                yield return null;
            }
        }
    }
}
