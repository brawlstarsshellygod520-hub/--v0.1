using UnityEngine;

namespace FireRescue
{
    public sealed class SprayEmitter : MonoBehaviour
    {
        [SerializeField] HoseConnection hose;
        [SerializeField] ElementReaction reactions;
        [SerializeField] Transform nozzleAnchor;
        [SerializeField] Transform hoseInletAnchor;
        [SerializeField] float range = 6.5f;
        // Opt-in migration: historical scenes keep their serialized point-aim contract.
        [SerializeField] bool directionalSpray;
        [SerializeField] bool automaticSimulation = true;
        [SerializeField] float groundHeight;
        [SerializeField] SprayWallOcclusion wallOcclusion;
        public const int ParticleCapacity = 256;
        readonly RaycastHit[] hits = new RaycastHit[32];
        readonly Collider[] overlaps = new Collider[32];
        readonly Flight[] flights = new Flight[ParticleCapacity];
        int flightCount;
        uint sequence;
        float emissionCredit;
        float pendingAmount;
        FluidSource emissionSource;
        EmissionSnapshot pendingSnapshot;
        struct Flight
        {
            public Vector3 Origin, Direction, Position, Previous, InitialVelocity;
            public SpraySettings Settings;
            public SprayEffectSettings Effects;
            public ElementKind Element;
            public float Amount, Reach, Lifetime, Age, Floor;
            public Collider SpawnBlock;
        }
        struct EmissionSnapshot
        {
            public Vector3 Origin, Direction;
            public SpraySettings Settings;
            public SprayEffectSettings Effects;
            public ElementKind Element;
            public float Floor;
            public Collider SpawnBlock;
            public bool Valid;
        }
        public readonly struct ParticleSample
        {
            public readonly Vector3 Position, Velocity;
            public readonly float Radius, NormalizedAge;
            public readonly ElementKind Element;
            public ParticleSample(Vector3 position, Vector3 velocity, float radius, float age, ElementKind element)
            { Position = position; Velocity = velocity; Radius = radius; NormalizedAge = age; Element = element; }
        }
        // Notification only. Reactions are applied exactly once by this emitter, never by VFX.
        public readonly struct ParticleImpact
        {
            public readonly Vector3 Position, Normal, Velocity;
            public readonly ElementKind Element;
            public readonly float Amount;
            public ParticleImpact(Vector3 position, Vector3 normal, Vector3 velocity, ElementKind element, float amount)
            { Position = position; Normal = normal; Velocity = velocity; Element = element; Amount = amount; }
        }
        public event System.Action<ParticleImpact> ParticleImpacted;
        public bool UsesDirectionalSpray => directionalSpray;
        public bool AutomaticSimulation { get => automaticSimulation; set => automaticSimulation = value; }
        public int ActiveParticleCount => flightCount;
        public int TotalEmitted { get; private set; }
        public float TotalEmittedAmount { get; private set; }
        public float DroppedAmount { get; private set; }
        public int TotalImpacts { get; private set; }
        public bool IsSpraying { get; private set; }
        public bool HasImpact { get; private set; }
        public Vector3 ImpactPoint { get; private set; }
        // Authored on the gameplay root, never on a replaceable mesh or animation bone.
        public Vector3 NozzlePosition => nozzleAnchor ? nozzleAnchor.position : transform.position + Vector3.up * .9f + transform.forward * .45f;
        public Vector3 HoseInletPosition => hoseInletAnchor ? hoseInletAnchor.position : NozzlePosition;
        public HoseConnection Connection => hose;
        public void Initialize(HoseConnection connection, ElementReaction reaction) { hose = connection; reactions = reaction; }
        public void SetNozzleAnchor(Transform anchor) => nozzleAnchor = anchor;
        public void SetHoseInletAnchor(Transform anchor) => hoseInletAnchor = anchor;
        public void SetGroundHeight(float height) => groundHeight = height;
        public void SetWallOcclusion(SprayWallOcclusion value) => wallOcclusion = value;
        public void EnableDirectionalSpray(bool enabled)
        {
            Stop(); flightCount = 0; sequence = 0;
            directionalSpray = enabled;
        }

        void Update()
        {
            if (directionalSpray && automaticSimulation) Advance(Time.deltaTime);
        }

        public void Stop()
        {
            FlushPending();
            IsSpraying = false; emissionCredit = 0; emissionSource = null;
            pendingSnapshot = default;
            if (!directionalSpray) { HasImpact = false; ImpactPoint = NozzlePosition; }
        }
        void OnDisable()
        {
            // Disable remains a hard lifecycle boundary: do not launch a late packet into a disabled emitter.
            DropPending(); IsSpraying = false; emissionCredit = 0f; emissionSource = null; pendingSnapshot = default;
            flightCount = 0; HasImpact = false;
        }

        public void Tick(Vector3 aimPoint, bool held, float dt)
        {
            if (directionalSpray) { EmitDirectional(aimPoint, held, dt); return; }
            IsSpraying = held && hose && hose.IsConnected && dt > 0;
            HasImpact = false;
            Vector3 direction = (aimPoint - NozzlePosition).normalized;
            ImpactPoint = NozzlePosition + direction * range;
            if (!IsSpraying || direction.sqrMagnitude < .5f) return;
            int count = Physics.SphereCastNonAlloc(NozzlePosition, .08f, direction, hits, range, ~0, QueryTriggerInteraction.Ignore);
            float closest = float.MaxValue;
            RaycastHit hit = default;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform)) continue;
                if (hits[i].distance >= closest) continue;
                hit = hits[i]; closest = hit.distance; HasImpact = true;
            }
            if (!HasImpact) return;
            ImpactPoint = hit.point;
            reactions.Apply(hose.Source.Element, hit.point, direction, hose.Source.FlowRate * dt, hit.collider);
        }

        public static Vector3 PlanarAimDirection(Vector3 playerPosition, Vector3 aimPoint, Vector3 fallback)
        {
            Vector3 direction = aimPoint - playerPosition; direction.y = 0;
            if (!Finite(direction) || direction.sqrMagnitude < .0001f)
            { direction = fallback; direction.y = 0; }
            return direction.sqrMagnitude > .0001f && Finite(direction) ? direction.normalized : Vector3.forward;
        }

        void EmitDirectional(Vector3 aimPoint, bool held, float dt)
        {
            if (!isActiveAndEnabled || !held || !hose || !hose.isActiveAndEnabled || !hose.Source || !hose.Source.isActiveAndEnabled ||
                !reactions || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) { Stop(); return; }
            FluidSource source = hose.Source;
            if (!Finite(source.FlowRate) || source.FlowRate <= 0f) { Stop(); return; }
            SpraySettings settings = source.SpraySettings;
            SprayEffectSettings effects = source.SprayEffects;
            if (!IsSpraying || emissionSource != source)
            {
                // Snapshot ownership is value-based: a destroyed Unity source still has to flush its old material.
                if (pendingAmount > 0f) FlushPending();
                emissionCredit = 1f; // Preserve the immediate visual/physical packet, funded by this tick's actual flow.
            }
            IsSpraying = true; emissionSource = source;
            float addedAmount = source.FlowRate * dt;
            if (!Finite(addedAmount)) { Stop(); return; }
            float priorAmount = pendingAmount;
            pendingAmount += addedAmount;
            if (!Finite(pendingAmount))
            {
                DroppedAmount += priorAmount; DroppedAmount += addedAmount;
                pendingAmount = 0f; pendingSnapshot = default; Stop(); return;
            }
            emissionCredit += dt * settings.PacketsPerSecond;
            if (!Finite(emissionCredit)) { DroppedAmount += pendingAmount; pendingAmount = 0f; pendingSnapshot = default; Stop(); return; }
            Vector3 direction = PlanarAimDirection(transform.position, aimPoint, transform.forward);
            pendingSnapshot = CaptureSnapshot(direction, settings, effects, source.Element);
            float wholeCredit = Mathf.Floor(emissionCredit);
            if (wholeCredit < 1f) return;
            emissionCredit -= wholeCredit;
            int available = ParticleCapacity - flightCount;
            if (available <= 0)
            {
                DropPending(); return;
            }
            // Clamp while still a float; casting a huge finite credit directly to int is undefined for this contract.
            int emitted = (int)Mathf.Min(wholeCredit, (float)available);
            float amountPerFlight = pendingAmount / emitted;
            pendingAmount = 0f;
            for (int i = 0; i < emitted; i++) EmitFlight(pendingSnapshot, amountPerFlight);
            pendingSnapshot = default;
        }

        EmissionSnapshot CaptureSnapshot(Vector3 direction, SpraySettings settings, SprayEffectSettings effects, ElementKind element)
        {
            Vector3 origin = NozzlePosition;
            Collider spawnBlock = null;
            if (wallOcclusion && wallOcclusion.TryBlock(new Vector3(transform.position.x, origin.y, transform.position.z),
                origin, out spawnBlock, out var blockedPoint, settings.CollisionRadius)) origin = blockedPoint;
            return new EmissionSnapshot { Origin = origin, Direction = direction, Settings = settings, Effects = effects,
                Element = element, Floor = groundHeight, SpawnBlock = spawnBlock, Valid = true };
        }

        void EmitFlight(EmissionSnapshot snapshot, float amount)
        {
            if (!snapshot.Valid || amount <= 0f || !Finite(amount)) return;
            float tier = (sequence * 3 % 5) / 4f;
            float angle = ((sequence * 5 % 7) / 3f - 1f) * snapshot.Settings.SpreadAngle * .5f;
            sequence++;
            float reach = Mathf.Lerp(snapshot.Settings.MinimumRange, snapshot.Settings.MaximumRange, tier);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * snapshot.Direction;
            float elevation = snapshot.Settings.LaunchAngle * Mathf.Deg2Rad;
            Vector3 velocity = (direction * Mathf.Cos(elevation) + Vector3.up * Mathf.Sin(elevation)) *
                snapshot.Settings.MuzzleSpeed * Mathf.Lerp(snapshot.Settings.MinimumSpeedFraction, 1f, tier);
            flights[flightCount++] = new Flight
            {
                Origin = snapshot.Origin, Position = snapshot.Origin, Previous = snapshot.Origin,
                Direction = direction, InitialVelocity = velocity,
                Reach = reach, Lifetime = snapshot.Settings.AirLifetime * (snapshot.Settings.PhysicalBallistics ? 1f : Mathf.Lerp(.55f, 1f, reach / snapshot.Settings.MaximumRange)),
                Settings = snapshot.Settings, Effects = snapshot.Effects, Element = snapshot.Element, Amount = amount,
                Floor = snapshot.Floor, SpawnBlock = snapshot.SpawnBlock
            };
            TotalEmitted++; TotalEmittedAmount += amount;
        }

        void FlushPending()
        {
            if (pendingAmount <= 0f) { pendingAmount = 0f; pendingSnapshot = default; return; }
            if (!pendingSnapshot.Valid) { DroppedAmount += pendingAmount; pendingAmount = 0f; return; }
            if (flightCount < ParticleCapacity) EmitFlight(pendingSnapshot, pendingAmount);
            else DroppedAmount += pendingAmount;
            pendingAmount = 0f; pendingSnapshot = default;
        }

        void DropPending()
        {
            if (pendingAmount > 0f) DroppedAmount += pendingAmount;
            pendingAmount = 0f; pendingSnapshot = default;
        }

        // Separate from input: already emitted material keeps moving after release, tool switch or disconnect.
        // A later server can set AutomaticSimulation=false and call Advance with its own simulation clock.
        public void Advance(float dt)
        {
            if (!directionalSpray || !isActiveAndEnabled || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            HasImpact = false;
            for (int i = flightCount - 1; i >= 0; i--)
            {
                Flight flight = flights[i];
                if (flight.SpawnBlock)
                {
                    HasImpact = true; ImpactPoint = flight.Origin; TotalImpacts++;
                    flights[i] = flights[--flightCount];
                    continue; // Nozzle protrudes across a solid wall: no material can be created on its far side.
                }
                float remaining = Mathf.Min(dt, flight.Lifetime - flight.Age);
                bool alive = true;
                while (remaining > .000001f && alive)
                {
                    float step = Mathf.Min(remaining, 1f / 60f);
                    Vector3 previous = flight.Position;
                    flight.Previous = previous;
                    flight.Age = Mathf.Min(flight.Lifetime, flight.Age + step);
                    Vector3 next = PositionAt(flight, flight.Age);
                    if (FindCollision(previous, next, flight.Settings.CollisionRadius, out Collider collider, out Vector3 point, out Vector3 normal))
                    {
                        HasImpact = true; ImpactPoint = point; TotalImpacts++;
                        if (reactions) reactions.ApplySpray(flight.Element, point, (next - previous).normalized,
                            flight.Amount, collider, flight.Settings.ImpactRadius, flight.Settings.GroundLifetime, flight.Effects);
                        ParticleImpacted?.Invoke(new ParticleImpact(point, normal, VelocityAt(flight, flight.Age), flight.Element, flight.Amount));
                        alive = false;
                    }
                    flight.Position = next;
                    if (flight.Age >= flight.Lifetime) alive = false;
                    remaining -= step;
                }
                if (alive) flights[i] = flight;
                else flights[i] = flights[--flightCount];
            }
        }

        static Vector3 PositionAt(Flight flight, float age)
        {
            if (flight.Settings.PhysicalBallistics)
            {
                float drag = flight.Settings.AirDrag;
                Vector3 gravity = Vector3.down * flight.Settings.Gravity;
                if (drag < .001f) return flight.Origin + flight.InitialVelocity * age + gravity * (.5f * age * age);
                float response = (1f - Mathf.Exp(-drag * age)) / drag;
                return flight.Origin + flight.InitialVelocity * response + gravity * ((age - response) / drag);
            }
            float t = Mathf.Clamp01(age / flight.Lifetime);
            // Range and flight shape belong to the source profile, never the cursor's distance/height.
            float vertical = 4f * flight.Settings.ArcHeight * t * (1f - t) -
                Mathf.Max(0f, flight.Origin.y - flight.Floor) * t * t;
            return flight.Origin + flight.Direction * (flight.Reach * t) + Vector3.up * vertical;
        }

        static Vector3 VelocityAt(Flight f, float age)
        {
            if (f.Settings.PhysicalBallistics)
            {
                float drag = f.Settings.AirDrag;
                if (drag < .001f) return f.InitialVelocity + Vector3.down * (f.Settings.Gravity * age);
                float decay = Mathf.Exp(-drag * age);
                return f.InitialVelocity * decay + Vector3.down * (f.Settings.Gravity * (1f - decay) / drag);
            }
            float t = age / f.Lifetime;
            float verticalSpeed = (4f * f.Settings.ArcHeight * (1f - 2f * t) -
                2f * Mathf.Max(0, f.Origin.y - f.Floor) * t) / f.Lifetime;
            return f.Direction * (f.Reach / f.Lifetime) + Vector3.up * verticalSpeed;
        }

        public ParticleSample GetParticle(int index)
        {
            if (index < 0 || index >= flightCount) return default;
            Flight f = flights[index];
            float t = f.Age / f.Lifetime;
            return new ParticleSample(f.Position, VelocityAt(f, f.Age),
                f.Settings.CollisionRadius, t, f.Element);
        }

        bool FindCollision(Vector3 from, Vector3 to, float radius, out Collider collider, out Vector3 point, out Vector3 normal)
        {
            collider = null; point = to; normal = Vector3.up;
            int overlapCount = Physics.OverlapSphereNonAlloc(from, radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            // NonAlloc ordering is unspecified when full. Rare dense clusters use the complete query.
            var overlapBuffer = overlapCount == overlaps.Length
                ? Physics.OverlapSphere(from, radius, ~0, QueryTriggerInteraction.Ignore) : overlaps;
            if (overlapBuffer != overlaps) overlapCount = overlapBuffer.Length;
            float closest = float.MaxValue;
            for (int i = 0; i < overlapCount; i++)
            {
                Collider candidate = overlapBuffer[i];
                if (!candidate || candidate.transform.IsChildOf(transform)) continue;
                Vector3 contact = candidate.ClosestPoint(from);
                float d = (contact - from).sqrMagnitude;
                if (d < closest) { closest = d; collider = candidate; point = contact; normal = (from - contact).normalized; }
            }
            if (collider) return true; // Starting inside a wall cannot launch material through it.
            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < .000001f) return false;
            closest = float.MaxValue;
            if (wallOcclusion && wallOcclusion.TryBlock(from, to, out collider, out point, radius))
            { closest = Vector3.Distance(from, point); normal = -delta.normalized; }
            int count = Physics.SphereCastNonAlloc(from, radius, delta / length, hits, length, ~0, QueryTriggerInteraction.Ignore);
            var hitBuffer = count == hits.Length
                ? Physics.SphereCastAll(from, radius, delta / length, length, ~0, QueryTriggerInteraction.Ignore) : hits;
            if (hitBuffer != hits) count = hitBuffer.Length;
            for (int i = 0; i < count; i++)
            {
                Collider candidate = hitBuffer[i].collider;
                if (!candidate || candidate.transform.IsChildOf(transform) || hitBuffer[i].distance >= closest) continue;
                closest = hitBuffer[i].distance; collider = candidate; point = hitBuffer[i].point; normal = hitBuffer[i].normal;
            }
            return collider;
        }

        static bool Finite(Vector3 v) => !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
            !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
