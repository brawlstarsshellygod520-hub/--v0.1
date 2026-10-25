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
    // Loads the persisted map. Tests only change Play Mode objects, never save them.
    [InitializeOnLoad]
    public static class TutorialGreyboxLayoutValidation
    {
        public const string ReportFolder = "Docs/Validation/2026-10-19-greybox-step3";
        const string Key = "FireRescue.Step3Validation";
        static bool finished;
        static double readyAt;

        static TutorialGreyboxLayoutValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary Step3 Validation").AddComponent<TutorialGreyboxLayoutValidationDriver>();
            };
        }

        public static void BatchRun()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Batch entry only.");
            readyAt = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update += Begin;
        }

        static void Begin()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < readyAt) return;
            EditorApplication.update -= Begin;
            EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }

        static void OnLog(string text, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Finish(false, "Console: " + text + "\n" + stack);
        }

        public static void Finish(bool success, string details)
        {
            if (finished) return;
            finished = true;
            SessionState.SetBool(Key, false);
            Application.logMessageReceived -= OnLog;
            Directory.CreateDirectory(ReportFolder);
            string report = (success ? "GREYBOX_LAYOUT_PLAYMODE_PASS" : "GREYBOX_LAYOUT_PLAYMODE_FAIL") + "\n" + details;
            File.WriteAllText(ReportFolder + "/layout-playmode.txt", report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
        }
    }

    public sealed class TutorialGreyboxLayoutValidationDriver : MonoBehaviour
    {
        readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        readonly List<string> results = new List<string>();
        GameObject layout;
        FirefighterMotor motor;
        CharacterController controller;
        FireField field;
        RescueNpc npc;
        Camera cameraView;
        Quaternion cameraRotation;
        float started;
        float maximumCameraOffset;
        int occlusionSamples;

        void Start()
        {
            Time.captureDeltaTime = 1f / 60;
            started = Time.realtimeSinceStartup;
            routines.Push(Scenario());
        }

        void OnDestroy() => Time.captureDeltaTime = 0;

        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 180) throw new TimeoutException("Step3 route validation exceeded 180 seconds.");
                while (routines.Count > 0)
                {
                    var routine = routines.Peek();
                    if (!routine.MoveNext()) { routines.Pop(); continue; }
                    if (routine.Current is IEnumerator nested) { routines.Push(nested); continue; }
                    return;
                }
            }
            catch (Exception exception)
            {
                enabled = false;
                TutorialGreyboxLayoutValidation.Finish(false, string.Join("\n", results) + "\n" + exception);
            }
        }

        IEnumerator Scenario()
        {
            yield return null;
            layout = GameObject.Find(TutorialGreyboxLayoutBuilder.LayoutName);
            Check(layout, "Persisted Step3 layout is active.");
            var tutorialRoot = layout.transform.parent;
            var step2 = tutorialRoot.Find(GreyboxPrefabBuilder.AssemblyName);
            Check(step2 && !step2.gameObject.activeSelf, "Original Step2 assembly remains present and inactive.");
            Check(layout.GetComponentsInChildren<FirefighterMotor>().Length == 1, "Exactly one active firefighter.");
            Check(layout.GetComponentsInChildren<RescueNpc>().Length == 1, "Exactly one resident.");
            Check(layout.GetComponentsInChildren<FluidSource>().Length == 2, "Only household water and oil sources.");
            Check(layout.GetComponentsInChildren<LocalPlayerInput>(true).Length == 0, "Step4 device input was not added early.");
            Check(layout.GetComponentsInChildren<TextMesh>(true).Length == 0 && layout.GetComponentsInChildren<Canvas>(true).Length == 0,
                "No permanent world text or UI.");
            var markers = layout.transform.Find("Rooms/Markers");
            Check(markers && markers.childCount == 8, "Eight named authoring areas exist without world labels.");
            foreach (string room in new[] { "Bedroom 1", "Bedroom 2", "Bedroom 3", "Living Room", "Kitchen", "Bathroom", "Corridor", "Exterior" })
                Check(markers.Find(room), "Area exists: " + room);
            foreach (var t in layout.GetComponentsInChildren<Transform>(true))
            {
                CheckNoMissingScripts(t.gameObject);
                if (t.name != "Visual") continue;
                if (t.GetComponentsInChildren<Collider>(true).Length != 0 || t.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                    throw new InvalidOperationException("Physics entered replaceable Visual: " + t.parent.name);
            }
            Check(true, "Every map Visual is replaceable without removing physics; no missing scripts.");
            Check(!AssetDatabase.GetDependencies(TutorialGreyboxSetup.ScenePath).Any(p => p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)),
                "Saved tutorial scene has no formal FBX dependency.");
            motor = layout.GetComponentInChildren<FirefighterMotor>();
            controller = motor.GetComponent<CharacterController>();
            field = layout.GetComponentInChildren<FireField>();
            npc = layout.GetComponentInChildren<RescueNpc>();
            field.Simulate = false;
            npc.Autonomous = false;
            Check(field.BurningCount == 0, "Map-only step begins without future lesson fires.");
            field.TryGetCell(new Vector3(-3.5f, 0, 1.5f), out var wallCell);
            Check(field.GetCell(wallCell.x, wallCell.y).Fuel == 0, "Wall-overlapping cells are authored nonflammable even when the wall is between cell centres.");
            var roomWalls = layout.transform.Find("Rooms").GetComponentsInChildren<BoxCollider>();
            Check(roomWalls.Length >= 16 && roomWalls.All(c => Mathf.Abs(c.bounds.max.y - .72f) < .001f),
                "Every house wall is a permanently visible .72m cutaway with full authored collision.");
            cameraView = Camera.main;
            Check(cameraView && cameraView.GetComponent<RescueCamera>(), "Existing Main Camera has the existing smooth follow component.");
            var target = new SerializedObject(cameraView.GetComponent<RescueCamera>()).FindProperty("target").objectReferenceValue;
            Check(target == motor.transform, "Persisted camera target is the gameplay actor, not its Visual.");
            Check(cameraView.orthographic && cameraView.orthographicSize >= 3.8f && cameraView.orthographicSize <= 4.8f,
                "Local room-sized orthographic framing.");
            cameraRotation = Quaternion.Euler(50, 40, 0);
            Check(Quaternion.Angle(cameraView.transform.rotation, cameraRotation) < .01f, "Fixed 50 degree pitch / 40 degree yaw.");
            Check(Mathf.Abs(controller.radius - .28f) < .001f, "Routes are exercised with the real .56m-wide controller.");
            CheckGap(new Vector3(.5f, .35f, -5), Vector3.right, 1.8f, "Front door");
            CheckGap(new Vector3(-5.5f, .35f, 3.4f), Vector3.right, 1.8f, "Bedroom 1 door");
            CheckGap(new Vector3(.5f, .35f, 3.4f), Vector3.right, 1.8f, "Bedroom 2 door");
            CheckGap(new Vector3(5.5f, .35f, 3.4f), Vector3.right, 1.8f, "Bedroom 3 door");
            CheckGap(new Vector3(4.5f, .35f, 1), Vector3.right, 1.8f, "Bathroom door");
            CheckGap(new Vector3(-3.5f, .35f, 2.2f), Vector3.forward, 2.08f, "Clear corridor");

            yield return Settle();
            Capture("01-outside.png", cameraView);
            CaptureOverview();
            yield return Walk(.5f, -4, "Outside through front doorway");
            yield return Walk(.5f, -.5f, "Living room clear approach");
            yield return Settle();
            Capture("02-living.png", cameraView);
            Check(VisibleHouseCorners() < 4, "Game Camera does not reveal the whole house from the living room.");
            yield return Walk(.5f, 2.2f, "Living opening to corridor");
            yield return Walk(-5.5f, 2.2f, "Corridor to bedroom 1");
            yield return Walk(-5.5f, 4.5f, "Bedroom 1 doorway");
            yield return Settle();
            Capture("03-bedroom.png", cameraView);
            Check(VisibleHouseCorners() < 4, "Game Camera shows a bedroom and nearby area, not the full floor plan.");
            yield return Walk(-5.5f, 2.2f, "Bedroom 1 back to corridor");
            yield return Walk(.5f, 2.2f, "Corridor to bedroom 2");
            yield return Walk(.5f, 4.5f, "Bedroom 2 doorway");
            yield return Walk(.5f, 2.2f, "Bedroom 2 back to corridor");
            yield return Walk(5.5f, 2.2f, "Corridor to bedroom 3");
            yield return Walk(5.5f, 4.5f, "Bedroom 3 doorway");
            yield return Walk(5.5f, 2.2f, "Bedroom 3 back to corridor");
            yield return Walk(4.5f, 2.2f, "Bathroom approach");
            yield return Walk(4.5f, 0, "Bathroom doorway");
            yield return Walk(4.5f, -2.3f, "Bathroom interior");
            yield return Walk(5.3f, -3.5f, "Toilet cistern approach");
            var water = layout.GetComponentsInChildren<FluidSource>().Single(s => s.Element == ElementKind.Water);
            Check(motor.GetComponent<HoseConnection>().CanConnect(water), "Toilet source is reachable with existing distance and obstruction rules.");
            yield return Settle();
            Capture("04-bathroom.png", cameraView);
            yield return Walk(4.5f, -2.3f, "Leave toilet");
            yield return Walk(4.5f, 2.2f, "Bathroom to corridor");
            yield return Walk(.5f, 2.2f, "Return through corridor");
            yield return Walk(.5f, -.5f, "Return to living room");
            yield return Walk(-6, -.8f, "Living room to open kitchen");
            var oil = layout.GetComponentsInChildren<FluidSource>().Single(s => s.Element == ElementKind.Oil);
            Check(motor.GetComponent<HoseConnection>().CanConnect(oil), "Countertop oil bottle is reachable under the existing source rules.");
            yield return Settle();
            Capture("05-kitchen.png", cameraView);

            Check(maximumCameraOffset < .17f, "Moving actor stays near screen centre; maximum viewport offset=" + maximumCameraOffset.ToString("F3"));
            Check(occlusionSamples > 100, "Head/chest visibility sampled along all room routes: " + occlusionSamples);
            yield return VerifyWall();
            yield return VerifyCameraSmoothing();
            yield return VerifyResidentRoute();
            Check(layout.GetComponentsInChildren<MeshRenderer>().All(r => r.enabled && !r.forceRenderingOff),
                "All authored map renderers remain visible; no foreground wall is hidden.");
            enabled = false;
            TutorialGreyboxLayoutValidation.Finish(true, "Actual checks: " + results.Count + "\n" + string.Join("\n", results));
        }

        IEnumerator Walk(float x, float z, string description)
        {
            var goal = new Vector3(x, 0, z);
            int frames = 0;
            while (PlanarDistance(motor.transform.position, goal) > .09f && frames++ < 500)
            {
                Vector3 direction = goal - motor.transform.position;
                direction.y = 0;
                motor.Move(direction.normalized, 1f / 60);
                motor.Face(goal, 1f / 60);
                yield return null;
                if (motor.transform.position.y < -.1f) throw new InvalidOperationException("Fell through floor on " + description);
                if (Quaternion.Angle(cameraView.transform.rotation, cameraRotation) > .01f)
                    throw new InvalidOperationException("Camera rotated while following.");
                Vector3 viewport = cameraView.WorldToViewportPoint(motor.transform.position + Vector3.up * .7f);
                maximumCameraOffset = Mathf.Max(maximumCameraOffset, Vector2.Distance(new Vector2(viewport.x, viewport.y), Vector2.one * .5f));
                Vector3 chest = motor.transform.position + Vector3.up * 1.2f;
                // Orthographic sight rays are parallel, not directed toward the camera position.
                if (Physics.Raycast(chest, -cameraView.transform.forward, out var hit, 30, 1 << 8, QueryTriggerInteraction.Ignore))
                    throw new InvalidOperationException("Actor occluded along " + description + " by " + hit.collider.name);
                occlusionSamples++;
            }
            Check(PlanarDistance(motor.transform.position, goal) <= .09f, description + " reached by actual Motor.Move: " + motor.transform.position.ToString("F2"));
            for (int i = 0; i < 10; i++) { motor.Move(Vector3.zero, 1f / 60); yield return null; }
        }

        IEnumerator Settle()
        {
            for (int i = 0; i < 60; i++) { motor.Move(Vector3.zero, 1f / 60); yield return null; }
        }

        IEnumerator VerifyWall()
        {
            Teleport(new Vector3(1.6f, 0, -2));
            cameraView.GetComponent<RescueCamera>().Initialize(motor.transform);
            for (int i = 0; i < 90; i++) { motor.Move(Vector3.right, 1f / 60); yield return null; }
            Check(motor.transform.position.x < 2.5f && motor.transform.position.x > 2, "Living/bathroom wall blocks the real controller.");
            var visuals = layout.GetComponentsInChildren<Transform>().Where(t => t.name == "Visual").ToArray();
            try
            {
                foreach (var visual in visuals) visual.gameObject.SetActive(false);
                Teleport(new Vector3(1.6f, 0, -2));
                for (int i = 0; i < 90; i++) { motor.Move(Vector3.right, 1f / 60); yield return null; }
                Check(motor.transform.position.x < 2.5f, "Map collision still works with every Visual disabled.");
            }
            finally
            {
                foreach (var visual in visuals) if (visual) visual.gameObject.SetActive(true);
            }
            yield return Settle();
        }

        IEnumerator VerifyCameraSmoothing()
        {
            Teleport(new Vector3(.5f, 0, -.5f));
            cameraView.GetComponent<RescueCamera>().Initialize(motor.transform);
            yield return Settle();
            Vector3 start = cameraView.transform.position;
            Teleport(new Vector3(1.5f, 0, -.5f));
            yield return null;
            float firstFrameDistance = Vector3.Distance(start, cameraView.transform.position);
            Check(firstFrameDistance > 0 && firstFrameDistance < .5f, "Camera smooths a target step rather than snapping: " + firstFrameDistance.ToString("F4") + "m.");
            yield return Settle();
            Vector3 viewport = cameraView.WorldToViewportPoint(motor.transform.position + Vector3.up * .7f);
            Check(Vector2.Distance(new Vector2(viewport.x, viewport.y), Vector2.one * .5f) < .005f, "Camera settles on the player.");
            Vector3 rested = cameraView.transform.position;
            for (int i = 0; i < 30; i++) yield return null;
            Check(Vector3.Distance(rested, cameraView.transform.position) < .005f, "Stopped camera has no measurable >5mm drift.");
            motor.transform.rotation = Quaternion.Euler(0, 195, 0);
            yield return null;
            Check(Quaternion.Angle(cameraView.transform.rotation, cameraRotation) < .01f, "Actor turning does not rotate Camera.");
        }

        IEnumerator VerifyResidentRoute()
        {
            npc.Autonomous = true;
            Teleport(new Vector3(-5.5f, 0, 2.2f));
            int frames = 0;
            while (!npc.IsRescued && frames++ < 1500) yield return null;
            Check(npc.IsRescued, "Existing NPC autonomously traverses bedroom, corridor, living room and front door to the outdoor exit.");
        }

        int VisibleHouseCorners()
        {
            int visible = 0;
            foreach (float x in new[] { -8f, 8f })
                foreach (float z in new[] { -5f, 9f })
                {
                    Vector3 p = cameraView.WorldToViewportPoint(new Vector3(x, 0, z));
                    if (p.z > 0 && p.x >= 0 && p.x <= 1 && p.y >= 0 && p.y <= 1) visible++;
                }
            return visible;
        }

        void CheckGap(Vector3 center, Vector3 axis, float expected, string name)
        {
            bool left = Physics.Raycast(center, -axis, out var a, 4, 1 << 8, QueryTriggerInteraction.Ignore);
            bool right = Physics.Raycast(center, axis, out var b, 4, 1 << 8, QueryTriggerInteraction.Ignore);
            Check(left && right && Mathf.Abs(a.distance + b.distance - expected) < .02f,
                name + " actual collider clearance=" + (a.distance + b.distance).ToString("F2") + "m.");
        }

        void Teleport(Vector3 position)
        {
            controller.enabled = false;
            motor.transform.position = position;
            controller.enabled = true;
            Physics.SyncTransforms();
        }

        void CaptureOverview()
        {
            var go = new GameObject("Temporary map overview");
            var overview = go.AddComponent<Camera>();
            overview.CopyFrom(cameraView);
            overview.orthographicSize = 12;
            Quaternion angle = Quaternion.Euler(60, 0, 0);
            overview.transform.SetPositionAndRotation(new Vector3(0, .3f, 0) - angle * Vector3.forward * 26, angle);
            Capture("00-layout-overview.png", overview);
            Destroy(go);
        }

        static void Capture(string name, Camera camera)
        {
            Directory.CreateDirectory(TutorialGreyboxLayoutValidation.ReportFolder);
            var target = new RenderTexture(1600, 900, 24);
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                image.Apply();
                File.WriteAllBytes(TutorialGreyboxLayoutValidation.ReportFolder + "/" + name, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Destroy(image);
                target.Release();
                Destroy(target);
            }
        }

        static float PlanarDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        static void CheckNoMissingScripts(GameObject go)
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0) throw new InvalidOperationException("Missing script: " + go.name);
        }
        void Check(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException(message);
            results.Add("PASS: " + message);
        }
    }
}
