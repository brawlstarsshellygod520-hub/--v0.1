using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    // Disposable Play Mode fixtures for the hose constraint contract.  This intentionally
    // does not author any scene assets, so it can also be used by batch validation.
    [InitializeOnLoad]
    public static class HosePhysicsValidation
    {
        const string Key = "FireRescue.HosePhysicsValidation";
        const string Report = "Docs/Validation/2026-10-27-hose-fluid-physics/hose-playmode.txt";
        static bool finished;

        static HosePhysicsValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary hose physics validation").AddComponent<HosePhysicsValidationDriver>();
            };
        }

        public static void BatchRun()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }

        static void OnLog(string text, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
                Finish(false, "Console " + type + ": " + text + "\n" + stack);
        }

        public static void Finish(bool passed, string details)
        {
            if (finished) return;
            finished = true;
            SessionState.SetBool(Key, false);
            Application.logMessageReceived -= OnLog;
            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllText(Report, (passed ? "HOSE_PHYSICS_PLAYMODE_PASS" : "HOSE_PHYSICS_PLAYMODE_FAIL") + "\n" + details);
            EditorApplication.delayCall += () => EditorApplication.Exit(passed ? 0 : 1);
        }
    }

    public sealed class HosePhysicsValidationDriver : MonoBehaviour
    {
        const float Dt = 1f / 60f;
        readonly List<string> checks = new List<string>();
        float started;

        void Start()
        {
            started = Time.realtimeSinceStartup;
            Time.captureDeltaTime = Dt;
            StartCoroutine(Scenario());
        }

        void Update()
        {
            if (Time.realtimeSinceStartup - started > 60f)
                HosePhysicsValidation.Finish(false, string.Join("\n", checks) + "\nTimeout");
        }

        void OnDestroy() { Time.captureDeltaTime = 0f; }

        IEnumerator Scenario()
        {
            MethodInfo configure = typeof(HoseConnection).GetMethod("ConfigurePhysics");
            MethodInfo cableBlocked = typeof(HoseConnection).GetMethod("IsCableBlocked");
            Check(configure != null, "ConfigurePhysics is available for physical hose fixtures");
            Check(cableBlocked != null, "IsCableBlocked remains an inspect-only cable query");

            yield return VerifyChordWall(configure, cableBlocked);
            yield return VerifyLengthAndImpulse(configure);
            yield return VerifyInvalidSources(configure);
            yield return VerifyWindowRoute(configure);
            HosePhysicsValidation.Finish(true, "Checks=" + checks.Count + "\n" + string.Join("\n", checks));
        }

        IEnumerator VerifyChordWall(MethodInfo configure, MethodInfo cableBlocked)
        {
            GameObject root = new GameObject("Chord wall fixture");
            FluidSource source = Source(root.transform, "Source", Vector3.zero);
            GameObject player = Player(root.transform, "Player", new Vector3(0, 0, 2), out HoseConnection hose, out FirefighterMotor motor, out CharacterController controller, out Transform inlet);
            GameObject wall = Wall(root.transform, "Cable-only wall", new Vector3(.5f, .35f, .7f), new Vector3(.1f, .7f, 1.6f));
            Floor(root.transform, Vector3.zero);
            wall.layer = 8;
            SprayWallOcclusion geometry = root.AddComponent<SprayWallOcclusion>();
            geometry.Configure(new[] { wall.GetComponent<Collider>() }, Array.Empty<HoseWindow>());
            Configure(configure, hose, geometry, inlet); Physics.SyncTransforms();
            Check(hose.TryConnect(source), "Actor connects source zero before the chord-wall test");
            Check(!CableBlocked(cableBlocked, hose, source.Port, inlet.position) &&
                CableBlocked(cableBlocked, hose, source.Port, new Vector3(1.6f, 1.04f, 2)), "Start chord is clear but the rightward target chord crosses the low wall");
            SprayEmitter emitter = player.AddComponent<SprayEmitter>(); emitter.Initialize(hose, null); emitter.SetHoseInletAnchor(inlet);
            HoseView view = new GameObject("Hose view").AddComponent<HoseView>(); view.transform.SetParent(player.transform); view.Initialize(emitter);
            yield return null; yield return null;
            Check(HoseTube(view).enabled && TubeIsClear(HoseTube(view), cableBlocked, hose), "Enabled HoseView emits only cable segments clear of the wall");
            for (int i = 0; i < 60; i++) { motor.Move(Vector3.right, Dt); yield return null; }
            Check(player.transform.position.x < 1f, "Motor cannot pull the hose through a wall even where the body path is clear");
            Check(TubeIsClear(HoseTube(view), cableBlocked, hose), "HoseView remains wall-clear while the physical tether is blocked");
            view.enabled = false;
            for (int i = 0; i < 12; i++) { motor.Move(Vector3.right, Dt); yield return null; }
            Check(player.transform.position.x < 1f, "Disabling HoseView cannot disable the physical tether");
            view.enabled = true; yield return null; yield return null;
            Check(HoseTube(view).enabled && TubeIsClear(HoseTube(view), cableBlocked, hose), "Re-enabled HoseView resumes a clear cable without changing the tether");
            wall.GetComponent<Collider>().enabled = false; Physics.SyncTransforms();
            for (int i = 0; i < 30; i++) { motor.Move(Vector3.right, Dt); yield return null; }
            Check(player.transform.position.x > 1f && Planar(source.transform.position, player.transform.position) <= 3.02f, "Disabling that wall releases movement but retains the 3m tether");
            Destroy(root); yield return null; Physics.SyncTransforms();
        }

        IEnumerator VerifyLengthAndImpulse(MethodInfo configure)
        {
            GameObject root = new GameObject("Length fixture");
            FluidSource source = Source(root.transform, "Source", new Vector3(20, 0, 0));
            GameObject player = Player(root.transform, "Player", new Vector3(21, 0, 0), out HoseConnection hose, out FirefighterMotor motor, out CharacterController controller, out Transform inlet);
            Floor(root.transform, new Vector3(22, 0, 0));
            Configure(configure, hose, null, inlet); Physics.SyncTransforms();
            Check(hose.TryConnect(source), "Separate fixture connects before force and planar-length checks");
            controller.enabled = false; player.transform.position = new Vector3(22.95f, 3f, 0); controller.enabled = true; Physics.SyncTransforms();
            Check(Mathf.Abs(hose.UsedLength - 2.95f) < .02f && Planar(source.transform.position, hose.ConstrainPosition(new Vector3(30, -4, 0))) <= 3.001f, "UsedLength and constraint both use the root XZ measure regardless of height");
            controller.enabled = false; player.transform.position = new Vector3(22.95f, 0, 0); controller.enabled = true; Physics.SyncTransforms();
            motor.AddImpulse(Vector3.right * 8f);
            for (int i = 0; i < 30; i++) { motor.Move(Vector3.right, Dt); yield return null; }
            Check(Planar(source.transform.position, player.transform.position) <= 3.02f && motor.Velocity.x < .15f, "Outward impulse is not stored at the tether boundary");
            float edge = player.transform.position.x;
            for (int i = 0; i < 12; i++) { motor.Move(Vector3.left, Dt); yield return null; }
            Check(player.transform.position.x < edge - .04f, "Reversing immediately leaves the tether edge");
            float z = player.transform.position.z;
            for (int i = 0; i < 12; i++) { motor.Move(Vector3.forward, Dt); yield return null; }
            Check(Mathf.Abs(player.transform.position.z - z) > .03f, "Tangential movement remains available at the tether edge");
            Destroy(root); yield return null; Physics.SyncTransforms();
        }

        IEnumerator VerifyInvalidSources(MethodInfo configure)
        {
            GameObject root = new GameObject("Invalid source fixture");
            FluidSource first = Source(root.transform, "Disabled source", new Vector3(40, 0, 0));
            GameObject player = Player(root.transform, "Player", new Vector3(41, 0, 0), out HoseConnection hose, out FirefighterMotor motor, out CharacterController controller, out Transform inlet);
            Floor(root.transform, new Vector3(40, 0, 0));
            Configure(configure, hose, null, inlet); Physics.SyncTransforms();
            Check(hose.TryConnect(first), "Fixture connects a source before invalidation");
            first.enabled = false;
            Check(!hose.IsConnected && hose.Source == null && hose.ConstrainPosition(new Vector3(50, 0, 0)).x == 50, "Disabled source clears itself with no ghost constraint");
            first.enabled = true; Check(hose.TryConnect(first), "A re-enabled source can reconnect");
            Destroy(first.gameObject); yield return null;
            Check(!hose.IsConnected && hose.Source == null, "Destroyed source also clears the connection");
            Destroy(root); yield return null; Physics.SyncTransforms();
        }

        IEnumerator VerifyWindowRoute(MethodInfo configure)
        {
            GameObject root = new GameObject("Window route fixture");
            FluidSource source = Source(root.transform, "Source", new Vector3(60, 0, .1f));
            GameObject player = Player(root.transform, "Player", new Vector3(60, 0, .1f), out HoseConnection hose, out FirefighterMotor motor, out CharacterController controller, out Transform inlet);
            GameObject wall = Wall(root.transform, "Full wall", new Vector3(60, .35f, 1), new Vector3(3, .7f, .1f));
            Floor(root.transform, new Vector3(60, 0, 1));
            wall.layer = 8;
            GameObject windowObject = new GameObject("Hose window"); windowObject.transform.SetParent(root.transform); windowObject.transform.position = new Vector3(60, 0, 1);
            Transform route = new GameObject("Route anchor").transform; route.SetParent(windowObject.transform); route.localPosition = new Vector3(0, 1.205f, 0);
            HoseWindow window = windowObject.AddComponent<HoseWindow>(); window.Initialize(route, route);
            SprayWallOcclusion geometry = root.AddComponent<SprayWallOcclusion>(); geometry.Configure(new[] { wall.GetComponent<Collider>() }, new[] { window });
            Configure(configure, hose, geometry, inlet); Physics.SyncTransforms();
            Check(hose.TryConnect(source) && hose.TryRouteThrough(window), "Manual window routing selects the permitted aperture");
            Vector3 through = hose.ConstrainPosition(new Vector3(60, 0, 2.5f));
            Check(through.z > 2.45f, "The routed hose constraint permits its path through the aperture");
            for (int i = 0; i < 60; i++) { motor.Move(Vector3.forward, Dt); yield return null; }
            Check(player.transform.position.z < .8f, "The character controller still cannot pass through the presentation wall");
            Destroy(root); yield return null; Physics.SyncTransforms();
        }

        static GameObject Player(Transform parent, string name, Vector3 position, out HoseConnection hose, out FirefighterMotor motor, out CharacterController controller, out Transform inlet)
        {
            GameObject player = new GameObject(name); player.transform.SetParent(parent); player.transform.position = position;
            controller = player.AddComponent<CharacterController>(); controller.height = 1.8f; controller.radius = .3f; controller.center = new Vector3(0, .9f, 0); controller.minMoveDistance = 0;
            hose = player.AddComponent<HoseConnection>(); motor = player.AddComponent<FirefighterMotor>(); motor.Initialize(null, hose);
            inlet = new GameObject("Actual inlet").transform; inlet.SetParent(player.transform); inlet.localPosition = new Vector3(0, 1.04f, 0);
            return player;
        }

        static FluidSource Source(Transform parent, string name, Vector3 position)
        {
            GameObject source = new GameObject(name); source.transform.SetParent(parent); source.transform.position = position;
            FluidSource fluid = source.AddComponent<FluidSource>(); fluid.Configure(ElementKind.Water); return fluid;
        }

        static GameObject Wall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = name; wall.transform.SetParent(parent); wall.transform.position = position; wall.transform.localScale = scale; return wall;
        }

        static void Floor(Transform parent, Vector3 position)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Ground"; floor.transform.SetParent(parent);
            floor.transform.position = position + Vector3.down * .1f; floor.transform.localScale = new Vector3(20, .2f, 20);
        }

        static void Configure(MethodInfo method, HoseConnection hose, SprayWallOcclusion geometry, Transform inlet)
        { method.Invoke(hose, new object[] { geometry, inlet }); }
        static bool CableBlocked(MethodInfo method, HoseConnection hose, Vector3 from, Vector3 to)
        { return (bool)method.Invoke(hose, new object[] { from, to, .0475f }); }
        static LineRenderer HoseTube(HoseView view)
        {
            foreach (LineRenderer line in view.GetComponentsInChildren<LineRenderer>())
                if (line.gameObject.name == "Hose tube") return line;
            throw new InvalidOperationException("HoseView did not create its hose tube.");
        }
        static bool TubeIsClear(LineRenderer tube, MethodInfo cableBlocked, HoseConnection hose)
        {
            for (int i = 1; i < tube.positionCount; i++)
                if (CableBlocked(cableBlocked, hose, tube.GetPosition(i - 1), tube.GetPosition(i))) return false;
            return true;
        }
        static float Planar(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Checks before failure:\n" + string.Join("\n", checks) + "\nFAIL " + label);
            checks.Add("PASS " + label);
        }
    }
}
