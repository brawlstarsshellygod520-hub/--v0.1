using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FireRescue.Editor
{
    /// <summary>
    /// Focused physics checks for the opt-in ballistic water profile.  This owns only
    /// temporary objects, so a scene-level validator can yield it without changing a map.
    /// </summary>
    public static class FirstRescueBallisticValidation
    {
        private const float Dt = 1f / 60f;

        public static IEnumerator Run(List<string> checks)
        {
            if (checks == null) throw new ArgumentNullException(nameof(checks));
            yield return VerifyGravityAndDistance(checks);
            yield return VerifyStepSizeIndependence(checks);
            yield return VerifyStopAndGroundImpact(checks);
            yield return VerifyWallInterceptAndAmount(checks);
        }

        private static IEnumerator VerifyGravityAndDistance(List<string> checks)
        {
            Fixture fixture = CreateFixture("gravity", 100f, 10f, 2f);
            EmitOne(fixture.emitter);
            SprayEmitter.ParticleSample initial = fixture.emitter.GetParticle(0);
            fixture.emitter.Advance(.2f);
            SprayEmitter.ParticleSample later = fixture.emitter.GetParticle(0);
            Check(later.Position.z > initial.Position.z + .5f && later.Velocity.y < initial.Velocity.y - .5f, checks,
                "Ballistic water advances forward while gravity lowers its vertical velocity.");
            yield return DestroyFixture(fixture);
        }

        private static IEnumerator VerifyStepSizeIndependence(List<string> checks)
        {
            Fixture sixty = CreateFixture("60 step", 130f, 10f, 2f);
            Fixture one = CreateFixture("one step", 160f, 10f, 2f);
            EmitOne(sixty.emitter);
            EmitOne(one.emitter);
            for (int i = 0; i < 60; i++) sixty.emitter.Advance(Dt);
            one.emitter.Advance(1f);
            SprayEmitter.ParticleSample a = sixty.emitter.GetParticle(0);
            SprayEmitter.ParticleSample b = one.emitter.GetParticle(0);
            Check(sixty.emitter.ActiveParticleCount == 1 && one.emitter.ActiveParticleCount == 1 &&
                Vector3.Distance(a.Position-sixty.emitter.transform.position, b.Position-one.emitter.transform.position) < .002f && Vector3.Distance(a.Velocity, b.Velocity) < .002f, checks,
                "Sixty fixed advances and one large advance produce the same analytic ballistic sample.");
            yield return DestroyFixture(sixty);
            yield return DestroyFixture(one);
        }

        private static IEnumerator VerifyStopAndGroundImpact(List<string> checks)
        {
            Fixture fixture = CreateFixture("ground", 190f, 1f, 1.5f);
            CreateCollider(fixture.root, "ground", new Vector3(190f, -.1f, 4f), new Vector3(30f, .2f, 30f));
            int impacts = 0;
            float impactedAmount = 0f;
            fixture.emitter.ParticleImpacted += impact => { impacts++; impactedAmount += impact.Amount; };
            EmitOne(fixture.emitter);
            fixture.emitter.Stop();
            Check(fixture.emitter.ActiveParticleCount == 1, checks,
                "Stopping the hose leaves an already emitted water packet in flight.");
            fixture.emitter.Advance(1f);
            Check(impacts == 1 && fixture.emitter.TotalImpacts == 1 && fixture.emitter.ActiveParticleCount == 0, checks,
                "A ground strike raises exactly one impact event for its packet.");
            Check(Mathf.Abs(impactedAmount - fixture.emitter.TotalEmittedAmount) < .0001f, checks,
                "Impact notification amount equals the funded emitted water amount.");
            yield return DestroyFixture(fixture);
        }

        private static IEnumerator VerifyWallInterceptAndAmount(List<string> checks)
        {
            Fixture fixture = CreateFixture("wall", 230f, 1f, 1.5f);
            CreateCollider(fixture.root, "wall", new Vector3(230f, 1f, 2.5f), new Vector3(4f, 3f, .2f));
            int impacts = 0;
            Vector3 hitPoint = default;
            float notifiedAmount = 0f;
            fixture.emitter.ParticleImpacted += impact => { impacts++; hitPoint = impact.Position; notifiedAmount += impact.Amount; };
            EmitOne(fixture.emitter);
            fixture.emitter.Stop();
            fixture.emitter.Advance(1f);
            Check(impacts == 1 && hitPoint.z < 2.6f, checks,
                "A wall intercepts the water before it can travel through the far side.");
            Check(notifiedAmount > 0f && Mathf.Abs(notifiedAmount - fixture.emitter.TotalEmittedAmount) < .0001f &&
                fixture.emitter.DroppedAmount == 0f, checks,
                "Impact events do not duplicate or create water beyond the positive source flow budget.");
            yield return DestroyFixture(fixture);
        }

        private static void EmitOne(SprayEmitter emitter)
        {
            emitter.Tick(emitter.transform.position + Vector3.forward * 30f, true, .1f);
            if (emitter.ActiveParticleCount != 1)
                throw new InvalidOperationException("Ballistic fixture must emit exactly one funded packet.");
        }

        private static Fixture CreateFixture(string label, float x, float nozzleHeight, float lifetime)
        {
            GameObject root = new GameObject("First rescue ballistic validation - " + label);
            GameObject player = new GameObject("Emitter");
            player.transform.SetParent(root.transform);
            player.transform.position = new Vector3(x, 0f, 0f);
            HoseConnection hose = player.AddComponent<HoseConnection>();
            SprayEmitter emitter = player.AddComponent<SprayEmitter>();
            ElementReaction reactions = new GameObject("Reactions").AddComponent<ElementReaction>();
            reactions.transform.SetParent(root.transform);
            GameObject nozzle = new GameObject("Nozzle");
            nozzle.transform.SetParent(root.transform);
            nozzle.transform.position = new Vector3(x, nozzleHeight, 0f);
            emitter.SetNozzleAnchor(nozzle.transform);
            emitter.Initialize(hose, reactions);
            emitter.EnableDirectionalSpray(true);
            emitter.AutomaticSimulation = false;
            emitter.SetGroundHeight(0f);

            GameObject sourceObject = new GameObject("Water source");
            sourceObject.transform.SetParent(root.transform);
            sourceObject.transform.position = player.transform.position;
            FluidSource source = sourceObject.AddComponent<FluidSource>();
            source.Configure(ElementKind.Water, 3f);
            SprayProfile profile = ScriptableObject.CreateInstance<SprayProfile>();
            profile.Configure(new SpraySettings
            {
                MinimumRange = 1f, MaximumRange = 20f, AirLifetime = lifetime, GroundLifetime = 1f,
                SpreadAngle = 0f, CollisionRadius = .04f, ImpactRadius = .5f, ArcHeight = 0f,
                PacketsPerSecond = 1f, PhysicalBallistics = true, MuzzleSpeed = 10f,
                LaunchAngle = 0f, Gravity = 9.81f, AirDrag = .1f
            });
            source.SetSprayProfile(profile);
            if (!hose.TryConnect(source)) throw new InvalidOperationException("Fixture water source should connect.");
            return new Fixture { root = root, emitter = emitter, profile = profile };
        }

        private static void CreateCollider(GameObject root, string name, Vector3 position, Vector3 scale)
        {
            GameObject collider = GameObject.CreatePrimitive(PrimitiveType.Cube);
            collider.name = name;
            collider.transform.SetParent(root.transform);
            collider.transform.position = position;
            collider.transform.localScale = scale;
            Physics.SyncTransforms();
        }

        private static IEnumerator DestroyFixture(Fixture fixture)
        {
            if (fixture.root) UnityEngine.Object.Destroy(fixture.root);
            if (fixture.profile) UnityEngine.Object.Destroy(fixture.profile);
            yield return null;
        }

        private static void Check(bool condition, List<string> checks, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            checks.Add("PASS " + message);
        }

        private sealed class Fixture
        {
            public GameObject root;
            public SprayEmitter emitter;
            public SprayProfile profile;
        }
    }
}
