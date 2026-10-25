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
    public static class FirePrototypeValidation
    {
        const string Key = "FireRescue.Validation";
        static bool finished;
        static FirePrototypeValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
                {
                    finished = false;
                    Application.logMessageReceived += OnLog;
                    new GameObject("Temporary Smoke Test").AddComponent<FireSmokeDriver>();
                }
            };
        }
        [MenuItem("消防 Prototype/驗證核心循環")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(FirePrototypeBuilder.ScenePath);
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }
        static void OnLog(string text, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false, text + "\n" + stack);
        }
        public static void Finish(bool success, string details)
        {
            if (finished) return;
            finished = true;
            Application.logMessageReceived -= OnLog;
            SessionState.SetBool(Key, false);
            string report = (success ? "FIRE_PLAYMODE_PASS" : "FIRE_PLAYMODE_FAIL") + "\n" + details;
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/fire-playmode-results.txt", report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
                else EditorApplication.ExitPlaymode();
            };
        }
    }

    public sealed class FireSmokeDriver : MonoBehaviour
    {
        readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        readonly List<string> results = new List<string>();
        FireField field;
        FirefighterMotor player;
        HoseConnection hose;
        SprayEmitter emitter;
        RescueNpc npc;
        FluidSource water, oil;
        float started;
        void Start()
        {
            // Batch mode can otherwise run tens of thousands of tiny frames per second.
            Time.captureDeltaTime = 1f / 60;
            started = Time.realtimeSinceStartup;
            field = FindFirstObjectByType<FireField>(); field.Simulate = false;
            player = FindFirstObjectByType<FirefighterMotor>();
            player.GetComponent<LocalPlayerInput>().enabled = false;
            hose = player.GetComponent<HoseConnection>(); emitter = player.GetComponent<SprayEmitter>();
            npc = FindFirstObjectByType<RescueNpc>(); npc.Autonomous = false;
            foreach (var source in FindObjectsByType<FluidSource>(FindObjectsSortMode.None))
                if (source.Element == ElementKind.Water) water = source; else oil = source;
            routines.Push(Scenario());
        }
        void OnDestroy() => Time.captureDeltaTime = 0;
        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 100) throw new Exception("Smoke timeout.");
                while (routines.Count > 0)
                {
                    var routine = routines.Peek();
                    if (!routine.MoveNext()) { routines.Pop(); continue; }
                    if (routine.Current is IEnumerator nested) { routines.Push(nested); continue; }
                    return;
                }
            }
            catch (Exception e) { enabled = false; FirePrototypeValidation.Finish(false, string.Join("\n", results) + "\n" + e); }
        }
        IEnumerator Scenario()
        {
            yield return null;
            Check(water && oil && field.BurningCount == 10, "Serialized scene contains two sources and the fire barrier.");
            Check(FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Length == 0, "No runtime world text or UI was added.");
            Check(!hose.IsConnected, "Player starts disconnected.");
            emitter.Tick(Vector3.zero, true, .1f);
            Check(!emitter.IsSpraying, "Disconnected hose cannot spray.");
            Check(hose.TryConnect(water), "Player can connect to nearby water source.");
            Vector3 limited = hose.ConstrainPosition(new Vector3(100, 0, 100));
            Check(Mathf.Abs(Vector3.Distance(limited, water.transform.position) - hose.MaximumLength) < .001f,
                "Hose limits travel to its maximum length.");
            npc.Tick(.1f);
            Check(!npc.HasSafePath && !npc.IsRescued, "NPC waits when the fire barrier leaves no safe route, without requiring player assistance.");

            CheckReactions();
            // Oil slip uses the same motor as keyboard movement; release retains momentum only on oil.
            Vector3 start = player.transform.position;
            for (int i = 0; i < 20; i++) player.Move(Vector3.right, .02f);
            player.Move(Vector3.zero, .1f);
            float dry = player.Velocity.magnitude;
            Place(player.gameObject, start);
            field.ApplyOil(start, 1, 3);
            for (int i = 0; i < 20; i++) player.Move(Vector3.right, .02f);
            float beforeRelease = player.Velocity.magnitude;
            player.Move(Vector3.zero, .1f);
            Check(player.IsSlipping && player.Velocity.magnitude > dry && player.Velocity.magnitude > beforeRelease * .5f,
                "Oil makes the player keep sliding after releasing movement.");
            field.ResetSimulation();
            Place(player.gameObject, start);
            for (int i = 0; i < 10; i++) player.Move(Vector3.zero, .05f);

            yield return MoveTo(new Vector3(.5f, 0, 3.2f));
            Check(hose.UsedLength > 7 && hose.IsConnected, "Player can enter the room while pulling a connected hose.");
            Check(!hose.TryConnect(oil) && hose.Source == water, "Distant sources cannot replace the existing connection.");
            npc.Tick(.3f);
            Check(!npc.HasSafePath, "NPC stays put while all exit routes contain fire without any player interaction.");
            Vector3 gap = field.CellCenter(6, 7);
            field.ApplyWater(gap, 5, .4f);
            npc.Tick(.3f);
            Check(npc.HasSafePath, "Clearing a single continuous route enables evacuation.");
            field.ResetSimulation();
            npc.Tick(.3f);
            Check(!npc.HasSafePath, "NPC stops when fire blocks the route again.");

            yield return MoveTo(new Vector3(-1, 0, -1.7f));
            field.Simulate = true; npc.Autonomous = true;
            float until = Time.time + 14;
            bool impact = false;
            while (!npc.IsRescued && Time.time < until)
            {
                player.Move(Vector3.zero, Time.deltaTime);
                player.Face(gap, Time.deltaTime);
                emitter.Tick(gap, true, Time.deltaTime);
                impact |= emitter.HasImpact;
                yield return null;
            }
            Check(impact && field.IsSafe(new Vector2Int(6, 7)), "Actual water spray contacts the floor and extinguishes the escape gap.");
            Check(npc.IsRescued, "With live fire simulation, NPC independently walks through the watered route to the exit.");
            var game = FindFirstObjectByType<RescueGameManager>();
            Check(game.RescueComplete && game.RescuedCount == 1, "Game manager receives exactly one rescued NPC.");
            npc.Tick(.5f);
            Check(npc.IsRescued && game.RescuedCount == 1, "A repeated NPC Tick after rescue does not count the NPC again.");
            emitter.Tick(gap, false, Time.deltaTime);
            Check(!emitter.IsSpraying, "Releasing spray stops emission.");
            field.Simulate = false;
            yield return null;
            Capture("Logs/fire-rescue-loop.png");

            // Exercise physical water impulses, independently of floor fire logic.
            Rigidbody crate = GameObject.Find("Pushable crate").GetComponent<Rigidbody>();
            var reactions = FindFirstObjectByType<ElementReaction>();
            Vector3 crateStart = crate.position;
            reactions.Apply(ElementKind.Water, crate.position, Vector3.right, 1, crate.GetComponent<Collider>());
            float wait = Time.time + .3f;
            while (Time.time < wait) yield return null;
            Check(crate.position.x > crateStart.x + .05f, "Water physically pushes a Rigidbody object.");
            reactions.Apply(ElementKind.Water, player.transform.position, Vector3.right, .5f, player.GetComponent<Collider>());
            Check(player.Velocity.x > 1, "Water impulse is also supported by the player motor.");
            hose.Disconnect();
            Place(player.gameObject, new Vector3(1, 0, -5.3f));
            Check(hose.TryConnect(oil), "Player can disconnect and connect to the oil source.");
            Vector3 oilTarget = field.CellCenter(8, 3);
            for (int i = 0; i < 30; i++)
            {
                player.Face(oilTarget, .03f);
                emitter.Tick(oilTarget, true, .03f);
                yield return null;
            }
            Check(emitter.HasImpact && field.GetOilAt(oilTarget) < .01f, "The front wall blocks a spray aimed through it.");
            oilTarget = field.CellCenter(6, 3);
            for (int i = 0; i < 30; i++)
            {
                player.Face(oilTarget, .03f);
                emitter.Tick(oilTarget, true, .03f);
                yield return null;
            }
            Check(field.GetOilAt(oilTarget) > .1f, "Actual oil spray deposits oil on the ground.");
            yield return null;
            Capture("Logs/fire-oil-hose.png");
            enabled = false;
            FirePrototypeValidation.Finish(true, string.Join("\n", results));
        }

        void CheckReactions()
        {
            var test = new GameObject("Temporary reaction field").AddComponent<FireField>();
            test.Simulate = false;
            test.Configure(new Vector3(40, 0, 40), 3, 3, 1);
            test.SetIgnitionSeeds(Array.Empty<Vector2Int>()); test.ResetSimulation();
            Vector3 center = test.CellCenter(1, 1), neighbour = test.CellCenter(2, 1);
            test.Ignite(center); test.Step(1);
            float normal = test.GetCell(2, 1).Intensity;
            Check(normal > 0 && Mathf.Abs(normal - test.GetCell(0, 1).Intensity) < .0001f, "Fire spreads to neighbouring fuel without directional bias.");
            test.ResetSimulation(); test.ApplyOil(neighbour, 1, .3f); test.Ignite(center); test.Step(1);
            Check(test.GetCell(2, 1).Intensity > normal * 2, "Oil accelerates fire spread.");
            test.ApplyWater(center, 10, .3f);
            Check(!test.GetCell(1, 1).IsBurning && test.GetCell(1, 1).Wetness > 0, "Sustained water extinguishes fire and briefly wets the floor.");
            test.SetNonFlammableCells(new[] { new Vector2Int(1, 1), new Vector2Int(2, 1) });
            test.Ignite(center);
            Check(!test.GetCell(1, 1).IsBurning, "Dry non-flammable ground cannot ignite.");
            test.ApplyOil(center, 1, .3f); test.Ignite(center); test.Step(.2f);
            Check(test.GetCell(1, 1).IsBurning && !test.GetCell(2, 1).IsBurning, "Oil itself burns on non-flammable paving without igniting dry paving.");
            Destroy(test.gameObject);
        }

        IEnumerator MoveTo(Vector3 target)
        {
            float deadline = Time.time + 12;
            int steps = 0;
            while (true)
            {
                Vector3 delta = target - player.transform.position; delta.y = 0;
                if (delta.magnitude < .13f) break;
                if (Time.time > deadline)
                {
                    string nearby = "";
                    foreach (var collider in Physics.OverlapSphere(player.transform.position + Vector3.up * .8f, 1.5f))
                        nearby += collider.name + " " + collider.bounds + "; ";
                    throw new Exception("Movement blocked at " + player.transform.position + " toward " + target +
                        "; steps=" + steps + "; dt=" + Time.deltaTime + "; velocity=" + player.Velocity + "; " + nearby);
                }
                steps++;
                player.Move(delta.normalized * Mathf.Min(1, delta.magnitude * 2), Mathf.Min(.04f, Time.deltaTime));
                yield return null;
            }
            for (int i = 0; i < 8; i++) { player.Move(Vector3.zero, .02f); yield return null; }
        }
        static void Place(GameObject go, Vector3 position)
        {
            var controller = go.GetComponent<CharacterController>();
            controller.enabled = false; go.transform.position = position; controller.enabled = true;
            Physics.SyncTransforms();
        }
        static void Capture(string path)
        {
            var camera = Camera.main;
            var rt = new RenderTexture(1600, 900, 24);
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
                Destroy(image); Destroy(rt);
            }
        }
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            results.Add("PASS: " + message);
        }
    }
}
