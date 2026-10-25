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
        readonly RaycastHit[] hits = new RaycastHit[32];
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

        public void Stop()
        {
            IsSpraying = false; HasImpact = false; ImpactPoint = NozzlePosition;
        }
        void OnDisable() => Stop();

        public void Tick(Vector3 aimPoint, bool held, float dt)
        {
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
    }
}
