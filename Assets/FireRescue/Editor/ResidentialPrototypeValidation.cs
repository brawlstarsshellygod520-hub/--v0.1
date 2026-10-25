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
    public static class ResidentialPrototypeValidation
    {
        const string Key = "FireRescue.ResidentialValidation";
        static bool finished;

        static ResidentialPrototypeValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
                {
                    finished = false;
                    Application.logMessageReceived += OnLog;
                    new GameObject("Temporary Residential Validation").AddComponent<ResidentialSmokeDriver>();
                }
            };
        }

        [MenuItem("消防 Prototype/驗證住宅救援流程")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(ResidentialPrototypeBuilder.ScenePath);
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
            string report = (success ? "RESIDENTIAL_PLAYMODE_PASS" : "RESIDENTIAL_PLAYMODE_FAIL") + "\n" + details;
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/residential-playmode-results.txt", report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
                else EditorApplication.ExitPlaymode();
            };
        }
    }

    public sealed class ResidentialSmokeDriver : MonoBehaviour
    {
        readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
        readonly List<string> results = new List<string>();
        FireField field;
        FirefighterMotor player;
        HoseConnection hose;
        SprayEmitter emitter;
        RescueNpc npc;
        FluidSource water;
        FluidSource oil;
        ElementReaction reactions;
        float started;
        bool crossedDoorway;
        bool crossedCorridor;
        bool crossedLivingRoom;

        void Start()
        {
            Time.captureDeltaTime = 1f / 60f;
            started = Time.realtimeSinceStartup;
            field = FindFirstObjectByType<FireField>();
            player = FindFirstObjectByType<FirefighterMotor>();
            npc = FindFirstObjectByType<RescueNpc>();
            reactions = FindFirstObjectByType<ElementReaction>();
            hose = player ? player.GetComponent<HoseConnection>() : null;
            emitter = player ? player.GetComponent<SprayEmitter>() : null;
            if (player)
            {
                LocalPlayerInput input = player.GetComponent<LocalPlayerInput>();
                if (input) input.enabled = false;
            }
            if (field) field.Simulate = false;
            if (npc) npc.Autonomous = false;
            foreach (FluidSource source in FindObjectsByType<FluidSource>(FindObjectsSortMode.None))
            {
                if (source.Element == ElementKind.Water) water = source;
                else if (source.Element == ElementKind.Oil) oil = source;
            }
            routines.Push(Scenario());
        }

        void OnDestroy() => Time.captureDeltaTime = 0f;

        void Update()
        {
            try
            {
                if (Time.realtimeSinceStartup - started > 130f)
                    throw new Exception("Residential validation timeout.");

                while (routines.Count > 0)
                {
                    IEnumerator routine = routines.Peek();
                    if (!routine.MoveNext()) { routines.Pop(); continue; }
                    if (routine.Current is IEnumerator nested) { routines.Push(nested); continue; }
                    return;
                }
            }
            catch (Exception error)
            {
                enabled = false;
                ResidentialPrototypeValidation.Finish(false, string.Join("\n", results) + "\n" + error);
            }
        }

        IEnumerator Scenario()
        {
            yield return null;
            Check(field && player && hose && emitter && npc && water && oil && reactions,
                "Residential scene exposes the real fire, motor, hose, emitter, NPC, sources and reactions.");
            CheckMarkers();
            Check(FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Length == 0,
                "Room markers are scene names, not runtime TextMesh UI.");
            CheckSources();
            CheckBlockBounds();
            CheckGridObstacleEdges();
            CheckCharacterVisual(player.gameObject, "firefighter");
            CheckCharacterVisual(npc.gameObject, "resident");
            Vector2Int npcStartCell = CellAt(npc.transform.position);
            Check(!Physics.CheckBox(field.CellCenter(npcStartCell.x,npcStartCell.y) + Vector3.up * .7f,
                new Vector3(.38f,.7f,.38f), Quaternion.identity, 1 << 8, QueryTriggerInteraction.Ignore),
                "Resident's initial route cell has enough clearance from beds and fixed obstacles.");

            Vector2Int doorFire = CellAt(ResidentialPrototypeBuilder.DoorFire);
            Vector2Int secondFire = CellAt(ResidentialPrototypeBuilder.SecondFire);
            Check(field.Width == 16 && field.Height == 18 && Mathf.Approximately(field.CellSize, 1f) &&
                field.Origin == new Vector3(-8, 0, -8), "Residential field matches the 16 by 18 metre authoring grid.");
            Check(field.BurningCount == 2 && field.GetCell(doorFire.x, doorFire.y).Fuel > 0 &&
                field.GetCell(secondFire.x, secondFire.y).Fuel > 0,
                "Only the two authored doorway fire cells start burning and neither is a non-flammable wall cell.");
            Check(!hose.IsConnected, "Firefighter starts with an unconnected hose.");
            emitter.Tick(ResidentialPrototypeBuilder.DoorFire, true, .1f);
            Check(!emitter.IsSpraying, "An unconnected hose cannot spray.");

            yield return MoveTo(new Vector3(.5f, 0, -4.2f), "entering through the front door");
            yield return MoveTo(new Vector3(.5f, 0, 2f), "crossing the living room to the corridor");
            yield return MoveTo(new Vector3(5.5f, 0, 2f), "crossing the corridor to the bathroom");
            yield return MoveTo(ResidentialPrototypeBuilder.WaterApproach, "approaching the toilet water outlet");
            Check(hose.TryConnect(water) && hose.Source == water, "Real motor reaches and connects to the toilet water source.");
            Check(Mathf.Approximately(hose.MaximumLength, 13f) &&
                Mathf.Abs(Vector3.Distance(hose.ConstrainPosition(water.transform.position + Vector3.right * 40f),
                    water.transform.position) - 13f) < .01f,
                "Connected hose enforces its 13 metre movement constraint.");

            yield return MoveTo(new Vector3(5.5f, 0, 2f), "returning from the bathroom");
            yield return MoveTo(new Vector3(-.5f, 0, 2f), "returning through the corridor");
            npc.Tick(.3f);
            Check(!npc.HasSafePath && !npc.IsRescued,
                "Both fires block the resident's route before the player clears it, without any interaction.");

            yield return MoveTo(new Vector3(-.5f, 0, 2.2f), "taking the safe side of the corridor fires");
            crossedDoorway = false;
            crossedCorridor = false;
            crossedLivingRoom = false;
            field.Simulate = true;
            npc.Autonomous = true;
            yield return AlternateWaterAtFires(doorFire, secondFire);
            Check(field.IsSafe(doorFire) && field.IsSafe(secondFire),
                "Alternating actual water spray extinguishes both doorway fire cells.");
            yield return MoveTo(new Vector3(-1.4f,0,2.2f), "stepping aside to leave the resident's corridor route open");
            float rescueDeadline = Time.time + 18f;
            while (!npc.IsRescued && Time.time < rescueDeadline)
            {
                player.Move(Vector3.zero, Time.deltaTime);
                ObserveNpcRoute();
                yield return null;
            }
            Check(npc.IsRescued && crossedDoorway && crossedCorridor && crossedLivingRoom,
                "Autonomous resident physically routes through the opened doorway, corridor and living room to the front exit." +
                " State: rescued=" + npc.IsRescued + ", safePath=" + npc.HasSafePath + ", position=" + npc.transform.position +
                ", observed=" + crossedDoorway + "/" + crossedCorridor + "/" + crossedLivingRoom + ", burning=" + field.BurningCount +
                "; " + FireDiagnostics());
            RescueGameManager game = FindFirstObjectByType<RescueGameManager>();
            Check(game && game.RescueComplete && game.RescuedCount == 1 && npc.IsRescued,
                "The game manager records the resident exactly once.");
            emitter.Tick(ResidentialPrototypeBuilder.DoorFire, false, Time.deltaTime);
            Check(!emitter.IsSpraying, "Releasing the water control stops emission.");
            Capture("Logs/residential-water-gameplay.png");

            field.Simulate = false;
            yield return VerifyWallBlocksSpray();
            yield return VerifyPhysicalWaterImpact();
            CheckReactionPrinciples();

            hose.Disconnect();
            yield return MoveTo(new Vector3(.5f, 0, 2f), "returning to the living-room door from the corridor");
            yield return MoveTo(new Vector3(.5f, 0, -.9f), "passing through the living-room opening");
            yield return MoveTo(ResidentialPrototypeBuilder.OilApproach, "walking to the salad oil bottle");
            Check(hose.TryConnect(oil) && hose.Source == oil, "Firefighter can disconnect and connect to the salad oil source.");
            Vector3 oilGround = new Vector3(-4.8f, 0, -.9f);
            for (int i = 0; i < 36; i++)
            {
                player.Face(oilGround, .03f);
                emitter.Tick(oilGround, true, .03f);
                yield return null;
            }
            Check(emitter.HasImpact && field.GetOilAt(oilGround) > .1f,
                "Actual oil spray deposits oil onto nearby open floor.");
            yield return MoveTo(oilGround, "walking onto the sprayed oil");
            player.Move(Vector3.right, .05f);
            Check(player.IsSlipping, "Oil on the open floor produces the motor's real slippery response.");
            emitter.Tick(oilGround, false, .03f);
            Check(!emitter.IsSpraying, "Releasing the oil control stops emission.");
            Capture("Logs/residential-oil-gameplay.png");

            enabled = false;
            ResidentialPrototypeValidation.Finish(true, string.Join("\n", results));
        }

        void CheckMarkers()
        {
            string[] markers = { "Bedroom 1", "Bedroom 2", "Bedroom 3", "Living Room", "Bathroom", "Kitchen" };
            foreach (string marker in markers)
                Check(GameObject.Find(marker), "Scene contains the " + marker + " development marker.");
        }

        string FireDiagnostics()
        {
            string result = "fire=";
            for (int z = 0; z < field.Height; z++) for (int x = 0; x < field.Width; x++)
                if (field.GetCell(x,z).IsBurning) result += field.CellCenter(x,z) + ":" + field.GetCell(x,z).Intensity.ToString("F2") + "; ";
            return result;
        }

        void CheckSources()
        {
            FluidSource[] sources = FindObjectsByType<FluidSource>(FindObjectsSortMode.None);
            Check(sources.Length == 2 && water.Element == ElementKind.Water && oil.Element == ElementKind.Oil,
                "Scene contains exactly the water and oil FluidSource components.");
            Check(water.name == "Toilet cistern water source" && oil.name == "Salad oil source" &&
                water.transform.Find("Toilet") && oil.transform.Find("SaladOil") &&
                Vector3.Distance(water.transform.position, new Vector3(6.6f, 0, -3.7f)) < .02f &&
                Vector3.Distance(oil.transform.position, new Vector3(-5.02f, 0, .15f)) < .02f,
                "Sources are attached to the toilet and salad-oil prefabs instead of industrial source props.");
            Vector3 waterFallback = water.transform.position + Vector3.up * .6f;
            Vector3 oilFallback = oil.transform.position + Vector3.up * .6f;
            Transform waterOutlet = water.transform.Find("Cistern outlet");
            Transform oilOutlet = oil.transform.Find("Bottle mouth");
            Check(waterOutlet && oilOutlet && Vector3.Distance(water.Port, waterOutlet.position) < .001f &&
                Vector3.Distance(oil.Port, oilOutlet.position) < .001f &&
                (water.Port - waterFallback).sqrMagnitude > .0004f && (oil.Port - oilFallback).sqrMagnitude > .0004f &&
                oil.Port.y > oil.transform.position.y + .9f,
                "Both sources use authored outlet transforms; the oil outlet remains at the raised bottle mouth, not fallback height.");
        }

        void CheckBlockBounds()
        {
            int checkedBlocks = 0;
            string mismatch = null;
            foreach (BoxCollider collider in FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
            {
                string name = collider.name;
                if (name.IndexOf("wall", StringComparison.OrdinalIgnoreCase) < 0 &&
                    name.IndexOf("partition", StringComparison.OrdinalIgnoreCase) < 0 &&
                    name.IndexOf("ground", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                Renderer[] renderers = collider.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    mismatch = name + " has no Block renderer";
                    break;
                }
                Bounds visual = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) visual.Encapsulate(renderers[i].bounds);
                checkedBlocks++;
                if (!BoundsClose(visual, collider.bounds, .06f))
                {
                    mismatch = name + " visual=" + visual + " collider=" + collider.bounds;
                    break;
                }
            }
            Check(checkedBlocks > 0 && mismatch == null,
                "Every wall, partition and Ground Block visual matches its BoxCollider bounds within 0.06m" +
                (mismatch == null ? "." : ": " + mismatch));
        }

        void CheckCharacterVisual(GameObject actor, string label)
        {
            Renderer renderer = actor.GetComponentInChildren<Renderer>();
            MeshFilter filter = actor.GetComponentInChildren<MeshFilter>();
            bool nonCapsuleMesh = renderer is SkinnedMeshRenderer ||
                (filter && filter.sharedMesh && !filter.sharedMesh.name.Contains("Capsule"));
            Check(renderer && nonCapsuleMesh && renderer.sharedMaterial && renderer.sharedMaterial.shader &&
                renderer.sharedMaterial.shader.name != "Hidden/InternalErrorShader",
                "The " + label + " uses an imported character mesh with a valid shader, not a Capsule visual.");
        }

        void CheckGridObstacleEdges()
        {
            string blockedEdge = null;
            for (int z = 0; z < field.Height && blockedEdge == null; z++)
            for (int x = 0; x < field.Width && blockedEdge == null; x++)
            {
                Vector3 start = field.CellCenter(x,z);
                if (Physics.CheckBox(start + Vector3.up*.7f, new Vector3(.38f,.7f,.38f), Quaternion.identity, 1 << 8)) continue;
                foreach (Vector2Int offset in new[] { Vector2Int.right, Vector2Int.up })
                {
                    int nx=x+offset.x, nz=z+offset.y;
                    if (nx >= field.Width || nz >= field.Height) continue;
                    Vector3 end = field.CellCenter(nx,nz);
                    if (Physics.CheckBox(end + Vector3.up*.7f, new Vector3(.38f,.7f,.38f), Quaternion.identity, 1 << 8)) continue;
                    if (Physics.Linecast(start + Vector3.up*.5f, end + Vector3.up*.5f, 1 << 8))
                    { blockedEdge=start + " -> " + end; break; }
                }
            }
            Check(blockedEdge == null, "NPC-clear neighbouring grid cells never connect through a fixed wall" +
                (blockedEdge == null ? "." : ": " + blockedEdge));
        }

        Vector2Int CellAt(Vector3 world)
        {
            if (!field.TryGetCell(world, out Vector2Int cell))
                throw new Exception("Required fire point is outside the residential FireField: " + world);
            return cell;
        }

        IEnumerator AlternateWaterAtFires(Vector2Int doorFire, Vector2Int secondFire)
        {
            bool impact = false;
            for (int i = 0; i < 96; i++)
            {
                Vector3 target = field.CellCenter((i & 1) == 0 ? doorFire.x : secondFire.x,
                    (i & 1) == 0 ? doorFire.y : secondFire.y);
                player.Face(target, .04f);
                emitter.Tick(target, true, .04f);
                impact |= emitter.HasImpact;
                ObserveNpcRoute();
                yield return null;
            }
            Check(impact, "Water emitter obtains physical ground impacts while treating the fires.");
        }

        IEnumerator VerifyWallBlocksSpray()
        {
            bool checkedWall = false;
            foreach (Collider wall in FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (wall.gameObject.layer != 8 ||
                    (wall.name.IndexOf("wall", StringComparison.OrdinalIgnoreCase) < 0 &&
                     wall.name.IndexOf("partition", StringComparison.OrdinalIgnoreCase) < 0)) continue;
                Vector3 aim = wall.bounds.center;
                Vector3 direction = aim - emitter.NozzlePosition;
                if (direction.sqrMagnitude < .1f || direction.magnitude > 6f) continue;
                direction.Normalize();
                emitter.Tick(aim, true, .04f);
                if (!emitter.HasImpact) continue;
                Vector3 behind = emitter.ImpactPoint + direction * 1.6f;
                if (!field.TryGetCell(behind, out Vector2Int behindCell) ||
                    Vector3.Distance(field.CellCenter(behindCell.x, behindCell.y), emitter.ImpactPoint) <= 1.1f)
                    continue;

                float before = field.GetCell(behindCell.x, behindCell.y).Wetness;
                for (int i = 0; i < 10; i++)
                {
                    emitter.Tick(aim, true, .04f);
                    yield return null;
                }
                Check(emitter.HasImpact && field.GetCell(behindCell.x, behindCell.y).Wetness <= before + .001f,
                    "Spray impact on a wall cannot directly apply water to a cell behind the wall.");
                checkedWall = true;
                break;
            }
            Check(checkedWall, "Residential collision geometry provides a wall-blocking spray negative case.");
            emitter.Tick(player.transform.position + player.transform.forward, false, .02f);
        }

        IEnumerator VerifyPhysicalWaterImpact()
        {
            GameObject tableObject = GameObject.Find("Coffee Table");
            Rigidbody table = tableObject ? tableObject.GetComponent<Rigidbody>() : null;
            Check(table, "Coffee Table is a pushable Rigidbody furniture object.");
            Vector3 tableStart = table.position;
            reactions.Apply(ElementKind.Water, table.position, Vector3.right, 1f, table.GetComponent<Collider>());
            float deadline = Time.time + .5f;
            while (Time.time < deadline) yield return null;
            Check(table.position.x > tableStart.x + .05f, "Water reaction physically pushes the Coffee Table Rigidbody.");
            reactions.Apply(ElementKind.Water, player.transform.position, Vector3.right, .5f, player.GetComponent<Collider>());
            Check(player.Velocity.x > 1f, "Water reaction applies an external impulse to the real firefighter motor.");
        }

        void CheckReactionPrinciples()
        {
            FireField test = new GameObject("Temporary residential reaction field").AddComponent<FireField>();
            test.Simulate = false;
            test.Configure(new Vector3(40, 0, 40), 3, 3, 1f);
            test.SetIgnitionSeeds(Array.Empty<Vector2Int>());
            test.ResetSimulation();
            Vector3 center = test.CellCenter(1, 1);
            Vector3 neighbour = test.CellCenter(2, 1);
            test.Ignite(center);
            test.Step(1f);
            float normal = test.GetCell(2, 1).Intensity;
            test.ResetSimulation();
            test.ApplyOil(neighbour, 1f, .3f);
            test.Ignite(center);
            test.Step(1f);
            Check(test.GetCell(2, 1).Intensity > normal * 2f, "Oil remains an accelerant in an isolated reaction field.");
            test.ApplyWater(center, 10f, .3f);
            Check(!test.GetCell(1, 1).IsBurning && test.GetCell(1, 1).Wetness > 0f,
                "Water remains capable of extinguishing and wetting an isolated fire cell.");
            Destroy(test.gameObject);
        }

        IEnumerator MoveTo(Vector3 target, string stage)
        {
            float deadline = Time.time + 12f;
            int steps = 0;
            while (true)
            {
                Vector3 delta = target - player.transform.position;
                delta.y = 0f;
                if (delta.magnitude < .13f) break;
                if (Time.time > deadline)
                    throw new Exception("Movement deadline during " + stage + "; position=" + player.transform.position +
                        "; target=" + target + "; steps=" + steps + "; velocity=" + player.Velocity + "; " + NearbyColliders());
                steps++;
                player.Move(delta.normalized * Mathf.Min(1f, delta.magnitude * 2f), Mathf.Min(.04f, Time.deltaTime));
                if (npc.Autonomous) ObserveNpcRoute();
                yield return null;
            }
            for (int i = 0; i < 8; i++) { player.Move(Vector3.zero, .02f); yield return null; }
        }

        string NearbyColliders()
        {
            string result = "colliders=";
            foreach (Collider collider in Physics.OverlapSphere(player.transform.position + Vector3.up * .8f, 1.5f))
                result += collider.name + " " + collider.bounds + "; ";
            return result;
        }

        void ObserveNpcRoute()
        {
            Vector3 npcPosition = npc.transform.position;
            crossedDoorway |= PlanarDistance(npcPosition, ResidentialPrototypeBuilder.DoorFire) < .7f;
            crossedCorridor |= npcPosition.z >= 1f && npcPosition.z <= 3f;
            crossedLivingRoom |= npcPosition.z < 1f && npcPosition.z > -5f;
        }

        static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        static bool BoundsClose(Bounds a, Bounds b, float tolerance)
        {
            return Mathf.Abs(a.min.x - b.min.x) <= tolerance && Mathf.Abs(a.min.y - b.min.y) <= tolerance &&
                Mathf.Abs(a.min.z - b.min.z) <= tolerance && Mathf.Abs(a.max.x - b.max.x) <= tolerance &&
                Mathf.Abs(a.max.y - b.max.y) <= tolerance && Mathf.Abs(a.max.z - b.max.z) <= tolerance;
        }

        static void Capture(string path)
        {
            Camera camera = Camera.main;
            if (!camera) throw new Exception("No main camera available for residential validation capture.");
            RenderTexture target = new RenderTexture(1600, 900, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Texture2D image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Destroy(image);
                Destroy(target);
            }
        }

        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            results.Add("PASS: " + message);
        }
    }
}
