using System;
using UnityEngine;

namespace FireRescue
{
    // The sole element-application boundary. A future server can invoke this after validating a spray command.
    public sealed class ElementReaction : MonoBehaviour
    {
        [SerializeField] FireField field;
        [SerializeField, Min(0f)] float waterPushImpulsePerUnit = 48f;
        const int FixedObstacles = 1 << 8;
        const float GroundClearance = .1f;
        Vector3 waterImpact;
        Predicate<Vector3> waterCanReach;
        public void Initialize(FireField fireField) => field = fireField;

        public void Apply(ElementKind element, Vector3 point, Vector3 direction, float amount, Collider hit = null)
        {
            if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            // Physical targets own their fire class. Do not project their response onto
            // the floor beneath a counter, or require a grid/visual for a discrete fire.
            ElementReceiver receiver = hit ? hit.GetComponentInParent<ElementReceiver>() : null;
            if (receiver && receiver.isActiveAndEnabled) { receiver.Receive(element, amount); return; }
            if (!field) return;
            if (element == ElementKind.Oil) { field.ApplyOil(point, amount, .8f); return; }
            bool water = element == ElementKind.Water;
            if (!water && element != ElementKind.DryPowder) return;

            // Static solids stop the jet, rather than projecting a floor splash from inside
            // their footprint. Dynamic targets still receive their existing water impulse.
            bool fixedSolid = hit && hit.gameObject.layer == 8 &&
                (!hit.attachedRigidbody || hit.attachedRigidbody.isKinematic);
            if (!fixedSolid)
            {
                waterImpact = new Vector3(point.x, field.Origin.y + GroundClearance, point.z);
                if (waterCanReach == null) waterCanReach = WaterReachesCell;
                if (water) field.ApplyWater(point, amount, .95f, waterCanReach);
                else field.ApplyDryPowder(point, amount, .7f, waterCanReach);
            }
            if (!water || !hit) return;
            FirefighterMotor player = hit.GetComponentInParent<FirefighterMotor>();
            if (player) player.AddImpulse(direction * amount * 5);
            else if (hit.attachedRigidbody && !hit.attachedRigidbody.isKinematic)
                hit.attachedRigidbody.AddForce(direction * amount * waterPushImpulsePerUnit, ForceMode.Impulse);
        }

        bool WaterReachesCell(Vector3 center)
        {
            center.y = waterImpact.y;
            return !Physics.Linecast(waterImpact, center, FixedObstacles, QueryTriggerInteraction.Ignore);
        }
    }
}
