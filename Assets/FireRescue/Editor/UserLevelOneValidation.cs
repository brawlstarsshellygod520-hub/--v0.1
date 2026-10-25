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
    [InitializeOnLoad]
    public static class UserLevelOneValidation
    {
        const string Key = "FireRescue.UserLevelOneValidation";
        static double readyAt;
        static bool finished;
        static UserLevelOneValidation()
        {
            EditorApplication.playModeStateChanged += s =>
            {
                if (s != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary User Level Validation").AddComponent<UserLevelOneValidationDriver>();
            };
        }
        public static void BatchRun() { readyAt = EditorApplication.timeSinceStartup + 2; EditorApplication.update += Begin; }
        static void Begin()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < readyAt) return;
            EditorApplication.update -= Begin;
            EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            SessionState.SetBool(Key, true); EditorApplication.EnterPlaymode();
        }
        static void OnLog(string s, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false, s + "\n" + stack); }
        public static void Finish(bool success, string details)
        {
            if (finished) return;
            finished = true; SessionState.SetBool(Key, false); Application.logMessageReceived -= OnLog;
            string report = (success ? "USER_LEVEL_ONE_PLAYMODE_PASS" : "USER_LEVEL_ONE_PLAYMODE_FAIL") + "\n" + details;
            Directory.CreateDirectory(UserLevelOneBuilder.Evidence);
            File.WriteAllText(UserLevelOneBuilder.Evidence + "/scene-playmode.txt", report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
        }
    }

    public sealed class UserLevelOneValidationDriver : MonoBehaviour
    {
        const float Dt = 1f / 60;
        readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        readonly List<string> results = new List<string>();
        GameObject layout;
        FirefighterMotor motor;
        LocalPlayerInput input;
        FirefighterTools tools;
        HoseConnection hose;
        SprayEmitter emitter;
        Camera view;
        float started;
        int visibilitySamples;
        void Start() { Time.captureDeltaTime = Dt; started = Time.realtimeSinceStartup; routines.Push(Scenario()); }
        void OnDestroy() => Time.captureDeltaTime = 0;
        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 180) throw new TimeoutException("User map test timeout.");
                while (routines.Count > 0)
                {
                    var task = routines.Peek();
                    if (!task.MoveNext()) { routines.Pop(); continue; }
                    if (task.Current is IEnumerator nested) { routines.Push(nested); continue; }
                    return;
                }
            }
            catch (Exception e)
            {
                enabled = false;
                Capture("failure.png", view);
                UserLevelOneValidation.Finish(false, string.Join("\n", results) + "\n" + e);
            }
        }
        IEnumerator Scenario()
        {
            yield return null;
            layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
            Check(layout, "Saved user layout is active.");
            Check(!layout.transform.parent.Find(TutorialGreyboxLayoutBuilder.LayoutName).gameObject.activeSelf, "Old map preserved but inactive.");
            motor = layout.GetComponentInChildren<FirefighterMotor>();
            input = motor.GetComponent<LocalPlayerInput>();
            input.ReadDevices = false; input.SetInputFocus(true);
            tools = motor.GetComponent<FirefighterTools>();
            hose = motor.GetComponent<HoseConnection>(); emitter = motor.GetComponent<SprayEmitter>();
            view = Camera.main;
            var field = layout.GetComponentInChildren<FireField>(); field.Simulate = false;
            Check(FindObjectsByType<FirefighterMotor>().Length == 1, "One active player, no overlapping old scene actors.");
            Check(layout.GetComponentsInChildren<MeshCollider>().Length == 11, "All 11 source architecture meshes have independent physics.");
            Check(layout.GetComponentsInChildren<TextMesh>(true).Length == 0 && layout.GetComponentsInChildren<Canvas>(true).Length == 0, "No permanent text or UI added.");
            foreach (var t in layout.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) throw new Exception("Missing script " + t.name);
                if (t.name == "Visual" && t.GetComponentsInChildren<Collider>(true).Length > 0) throw new Exception("Collider under Visual " + t.parent.name);
            }
            Check(true, "No missing scripts; Visual owns no colliders.");
            Check(field.CellSize == .5f && field.BurningCount == 0, "Half-metre navigation/fire grid, no future lesson seeds.");
            yield return Settle();
            Overview(); Capture("01-entry.png", view);
            yield return Walk(5.74f, -4.8f, "Front door from outdoor spawn");
            yield return Walk(.2f, -4.64f, "Main room to left doorway");
            yield return Walk(-2.7f, -4.64f, "Left room door");
            yield return Settle(); Capture("02-left-room.png", view);
            var npc = layout.GetComponentInChildren<RescueNpc>(true);
            Check(npc, "Existing resident is present; an already-rescued inactive resident is valid on this fire-free map.");
            if (!npc.IsRescued) npc.Autonomous = false;
            yield return Walk(.2f, -4.64f, "Left door return");
            yield return Walk(.2f, 2.4f, "Main room to bathroom door");
            yield return Walk(-2.3f, 2.4f, "Bathroom door");
            var water = layout.GetComponentsInChildren<FluidSource>().Single(s => s.Element == ElementKind.Water);
            Command(Vector2.zero, water.transform.position, true);
            Check(hose.Source == water, "F attaches to actual toilet source through reachable room.");
            for (int i = 0; i < 30; i++) { Command(Vector2.zero, new Vector3(-6, .04f, 2), false, false, true); yield return null; }
            Check(tools.SelectedTool == FirefighterTools.Tool.Hose && emitter.IsSpraying, "Right hold equips hose and sprays connected water in saved scene.");
            Capture("03-water-spray.png", view);
            Command(Vector2.zero, Vector3.zero);
            Check(!emitter.IsSpraying, "Right release stops stream.");
            yield return Walk(.2f, 2.4f, "Pull connected hose back through bathroom doorway");
            Check(hose.Source == water && hose.UsedLength > 3, "Hose stays connected while moving away.");
            yield return Walk(.7f, 2.6f, "Counter approach");
            Command(Vector2.zero, new Vector3(.7f, 0, 4), true);
            var oil = layout.GetComponentsInChildren<FluidSource>().Single(s => s.Element == ElementKind.Oil);
            Check(hose.Source == oil, "F swaps water for countertop salad oil.");
            for (int i = 0; i < 20; i++) { Command(Vector2.zero, new Vector3(3.5f, .04f, 3), false, false, true); yield return null; }
            Capture("04-oil-spray.png", view);
            Command(Vector2.zero, new Vector3(3.5f, .04f, 3));
            Command(Vector2.zero, new Vector3(3.5f, .04f, 3), true);
            Check(!hose.IsConnected, "F on current source disconnects.");
            yield return Walk(4.5f, 4.5f, "Cross main room");
            yield return Walk(5, 7, "Wide source-model passage to rear room");
            yield return Walk(2.5f, 7.2f, "Rear exterior door approach");
            yield return Walk(.1f, 7.2f, "Rear door to courtyard");
            yield return Walk(-3.4f, 10.4f, "Courtyard to detached annex");
            yield return Walk(-5.8f, 10.4f, "Annex doorway");
            yield return Settle(); Capture("05-annex.png", view);
            yield return Walk(-3.4f, 10.4f, "Annex return");
            yield return Walk(.1f, 7.2f, "Courtyard return");
            yield return Walk(2.5f, 7.2f, "Rear door return");
            yield return Walk(5, 7, "Rear room across");
            yield return Walk(5, 4.5f, "Main room return");
            yield return Walk(3, 1, "Open axe test position");
            int count = tools.SwingCount;
            for (int i = 0; i < 80; i++) { Command(Vector2.zero, new Vector3(3, 0, -3), false, true); yield return null; }
            Check(tools.SwingCount >= count + 2 && !emitter.IsSpraying, "Held left repeats axe cycles, never sprays.");
            Capture("06-axe.png", view);
            Command(Vector2.zero, new Vector3(3, 0, -3));
            Check(!tools.IsSwinging && tools.SelectedTool == FirefighterTools.Tool.Axe, "Release stops action and retains equipped axe.");
            if (!npc.IsRescued) npc.Autonomous = true;
            for (int i = 0; i < 1200 && !npc.IsRescued; i++) yield return null;
            if (!npc.IsRescued)
            {
                var path = (List<Vector2Int>)typeof(RescueNpc).GetField("path", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(npc);
                var index = typeof(RescueNpc).GetField("pathIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(npc);
                results.Add("NPC diagnostic: position=" + npc.transform.position.ToString("F3") + " state=" + npc.CurrentState + " safe=" + npc.HasSafePath + " index=" + index + " path=" + string.Join(",", path));
            }
            Check(npc.IsRescued, "Existing NPC autonomously traverses actual imported doorway and exits outside.");
            yield return CollisionBoundary();
            Check(visibilitySamples > 100, "Chest visibility and fixed Camera verified on " + visibilitySamples + " moving frames.");
            enabled = false;
            UserLevelOneValidation.Finish(true, "Checks=" + results.Count + "\n" + string.Join("\n", results));
        }

        IEnumerator Walk(float x, float z, string label)
        {
            Vector3 goal = new Vector3(x, 0, z);
            int frames = 0;
            while (Distance(motor.transform.position, goal) > .09f && frames++ < 600)
            {
                Vector3 d = goal - motor.transform.position; d.y = 0; d.Normalize();
                Vector3 forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                Command(new Vector2(Vector3.Dot(d, right), Vector3.Dot(d, forward)), goal);
                yield return null;
                if (motor.transform.position.y < -.1f) throw new Exception("Floor fall on " + label);
                if (Quaternion.Angle(view.transform.rotation, Quaternion.Euler(50, 40, 0)) > .01f) throw new Exception("Camera angle changed.");
                if (Physics.Raycast(motor.transform.position + Vector3.up * 1.2f, -view.transform.forward, 30, 1 << 8)) throw new Exception("Chest occluded on " + label);
                visibilitySamples++;
            }
            Check(Distance(motor.transform.position, goal) <= .09f, label + ": actual command/controller at " + motor.transform.position.ToString("F2"));
        }
        IEnumerator Settle() { for (int i = 0; i < 50; i++) { Command(Vector2.zero, motor.transform.position + motor.transform.forward * 4); yield return null; } }
        IEnumerator CollisionBoundary()
        {
            Teleport(new Vector3(7, 0, -3));
            for (int i = 0; i < 90; i++) { motor.Move(Vector3.right, Dt); yield return null; }
            Check(motor.transform.position.x > 7.5f && motor.transform.position.x < 8.4f, "Imported exterior mesh blocks real player.");
            var visuals = layout.transform.Find("Imported Architecture").GetComponentsInChildren<MeshRenderer>();
            foreach (var r in visuals) r.gameObject.SetActive(false);
            Teleport(new Vector3(7, 0, -3));
            for (int i = 0; i < 90; i++) { motor.Move(Vector3.right, Dt); yield return null; }
            Check(motor.transform.position.x < 8.4f, "Source-model collision survives all architecture Visuals being disabled.");
            foreach (var r in visuals) r.gameObject.SetActive(true);
        }
        void Command(Vector2 move, Vector3 aim, bool interact = false, bool axe = false, bool spray = false)
        { input.TickCommand(move, aim, interact, false, axe, spray, Dt); }
        void Teleport(Vector3 p)
        {
            var c = motor.GetComponent<CharacterController>(); c.enabled = false; motor.transform.position = p; c.enabled = true; Physics.SyncTransforms();
        }
        void Overview()
        {
            var go = new GameObject("Temporary overview"); var camera = go.AddComponent<Camera>(); camera.CopyFrom(view);
            camera.orthographicSize = 15; Quaternion rotation = Quaternion.Euler(70, 0, 0);
            camera.transform.SetPositionAndRotation(new Vector3(0, 0, 2) - rotation * Vector3.forward * 34, rotation);
            Capture("00-user-layout-overview.png", camera); Destroy(go);
        }
        static void Capture(string name, Camera camera)
        {
            if (!camera) return;
            var target = new RenderTexture(1600, 900, 24); var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            var previous = camera.targetTexture; var active = RenderTexture.active;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                File.WriteAllBytes(UserLevelOneBuilder.Evidence + "/" + name, image.EncodeToPNG());
            }
            finally { camera.targetTexture = previous; RenderTexture.active = active; Destroy(image); target.Release(); Destroy(target); }
        }
        static float Distance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); results.Add("PASS " + label); }
    }
}
