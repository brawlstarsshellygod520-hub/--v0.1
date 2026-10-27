using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FireRescue.Editor
{
    /// <summary>
    /// Focused fixture checks for the controller-sized clearance used by the first-rescue
    /// resident.  A scene-level validator can yield this routine without opening a scene.
    /// </summary>
    public static class FirstRescueNpcValidation
    {
        private const int FixedObstacleLayer = 8;
        private const float Dt = 1f / 60f;

        public static IEnumerator Run(List<string> checks)
        {
            if (checks == null)
            {
                throw new ArgumentNullException(nameof(checks));
            }

            yield return VerifyCornerDetour(checks);
            yield return VerifyCorridorClearance(checks, 1f, true, "wide doorway admits the controller-sized resident");
            yield return VerifyCorridorClearance(checks, .55f, false, "narrow doorway rejects the controller-sized resident");
            yield return VerifyFireRepath(checks);
        }

        private static IEnumerator VerifyCornerDetour(List<string> checks)
        {
            Fixture fixture = CreateFixture("corner detour", 20f, 7, 3);
            GameObject corner = CreateObstacle(fixture.root, fixture.field.CellCenter(3, 1), new Vector3(.8f, 1.5f, .8f));
            Physics.SyncTransforms();
            fixture.npc.Tick(.3f);
            Check(fixture.npc.HasSafePath, checks,
                "A route around a blocked corner stays valid instead of selecting the blocked centre cell.");

            yield return Drive(fixture.npc, 900);
            Check(fixture.npc.IsRescued, checks,
                "The detour reaches the exit without a controller collision stall.");
            yield return DestroyFixture(fixture.root);
        }

        private static IEnumerator VerifyCorridorClearance(List<string> checks, float openingWidth, bool expectedPath, string description)
        {
            Fixture fixture = CreateFixture("doorway " + openingWidth, 40f + openingWidth, 7, 1);
            float centreZ = fixture.field.CellCenter(0, 0).z;
            // Two side walls make an actual gap along the whole route.  This intentionally
            // tests controller radius rather than a point-sized grid node.
            float wallThickness = .10f;
            float wallOffset = openingWidth * .5f + wallThickness * .5f;
            CreateObstacle(fixture.root, new Vector3(fixture.field.Origin.x + 3.5f, .75f, centreZ - wallOffset),
                new Vector3(7f, 1.5f, wallThickness));
            CreateObstacle(fixture.root, new Vector3(fixture.field.Origin.x + 3.5f, .75f, centreZ + wallOffset),
                new Vector3(7f, 1.5f, wallThickness));
            Physics.SyncTransforms();
            fixture.npc.Tick(.3f);
            Check(fixture.npc.HasSafePath == expectedPath, checks, description + ".");

            if (expectedPath)
            {
                yield return Drive(fixture.npc, 900);
                Check(fixture.npc.IsRescued, checks, "The admitted doorway remains traversable while moving.");
            }

            yield return DestroyFixture(fixture.root);
        }

        private static IEnumerator VerifyFireRepath(List<string> checks)
        {
            Fixture fixture = CreateFixture("fire re-path", 60f, 7, 1);
            fixture.npc.Tick(.3f);
            Check(fixture.npc.HasSafePath, checks, "A clear straight route starts in Escaping.");

            Vector3 fire = fixture.field.CellCenter(3, 0);
            fixture.field.Ignite(fire, 1f);
            fixture.npc.Tick(.3f);
            Check(!fixture.npc.HasSafePath && fixture.npc.CurrentState == RescueNpc.State.WaitingForSafeRoute, checks,
                "A newly burning route cell cancels the current route before movement through it.");

            fixture.field.ApplyWater(fire, 100f, .25f);
            fixture.npc.Tick(.3f);
            Check(fixture.npc.HasSafePath && fixture.npc.CurrentState == RescueNpc.State.Escaping, checks,
                "Clearing the fire rebuilds the route without a player command.");
            yield return Drive(fixture.npc, 900);
            Check(fixture.npc.IsRescued, checks, "The resident finishes the rebuilt route exactly by walking it.");
            yield return DestroyFixture(fixture.root);
        }

        private static IEnumerator Drive(RescueNpc npc, int frames)
        {
            for (int frame = 0; frame < frames && !npc.IsRescued; frame++)
            {
                npc.Tick(Dt);
                yield return null;
            }
        }

        private static Fixture CreateFixture(string label, float x, int width, int height)
        {
            GameObject root = new GameObject("First rescue NPC validation - " + label);
            FireField field = new GameObject("Fire field").AddComponent<FireField>();
            field.transform.SetParent(root.transform);
            field.Configure(new Vector3(x, 0f, 40f), width, height, 1f);
            field.SetIgnitionSeeds(Array.Empty<Vector2Int>());
            field.ResetSimulation();
            field.Simulate = false;

            Transform exit = new GameObject("Exit").transform;
            exit.SetParent(root.transform);
            exit.position = field.CellCenter(width - 1, height / 2) + Vector3.up * .05f;
            GameObject resident = new GameObject("Resident");
            resident.transform.SetParent(root.transform);
            resident.transform.position = field.CellCenter(0, height / 2) + Vector3.up * .05f;
            RescueNpc npc = resident.AddComponent<RescueNpc>();
            npc.Autonomous = false;
            npc.Initialize(field, exit);
            Physics.SyncTransforms();
            return new Fixture { root = root, field = field, npc = npc };
        }

        private static GameObject CreateObstacle(GameObject root, Vector3 centre, Vector3 size)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Temporary fixed obstacle";
            obstacle.layer = FixedObstacleLayer;
            obstacle.transform.SetParent(root.transform);
            obstacle.transform.position = centre;
            obstacle.transform.localScale = size;
            return obstacle;
        }

        private static IEnumerator DestroyFixture(GameObject root)
        {
            if (root) UnityEngine.Object.Destroy(root);
            yield return null;
        }

        private static void Check(bool condition, List<string> checks, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }

            checks.Add("PASS " + message);
        }

        private sealed class Fixture
        {
            public GameObject root;
            public FireField field;
            public RescueNpc npc;
        }
    }
}
