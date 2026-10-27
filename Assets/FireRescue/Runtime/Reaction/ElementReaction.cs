using System;
using UnityEngine;

namespace FireRescue
{
    // The sole element-application boundary. A future server can invoke this after validating a spray command.
    public sealed class ElementReaction : MonoBehaviour
    {
        [SerializeField] FireField field;
        [SerializeField, Min(0f)] float waterPushImpulsePerUnit = 48f;
        [SerializeField] bool powderSurfaceSplash;
        const int FixedObstacles = 1 << 8;
        const float GroundClearance = .1f;
        Vector3 waterImpact;
        Predicate<Vector3> waterCanReach;
        Vector3 powderImpact;
        Predicate<Vector3> powderCanReach;
        public void Initialize(FireField fireField) => field = fireField;
        public void EnablePowderSurfaceSplash(bool enabled)=>powderSurfaceSplash=enabled;

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

        /// <summary>
        /// Applies a directional spray impact using the caller's authored footprint and residue lifetime.
        /// Timed floor residues are blocked by layer-8 fixed solids and filtered by the existing low LOS test.
        /// </summary>
        public void ApplySpray(ElementKind element, Vector3 point, Vector3 direction, float amount, Collider hit,
            float radius, float residueSeconds)
            => ApplySpray(element, point, direction, amount, hit, radius, residueSeconds, SprayEffectSettings.Neutral);

        public void ApplySpray(ElementKind element, Vector3 point, Vector3 direction, float amount, Collider hit,
            float radius, float residueSeconds, SprayEffectSettings effects)
        {
            if (!IsFinite(point) || !IsFinite(direction) || !IsFinite(amount) || !IsFinite(radius) || !IsFinite(residueSeconds) ||
                amount <= 0f || radius < 0f || residueSeconds < 0f)
                return;

            effects = effects.Validated();
            bool water = element == ElementKind.Water;
            bool oil = element == ElementKind.Oil;
            bool powder = element == ElementKind.DryPowder;
            if (!water && !oil && !powder) return;
            ElementReceiver receiver = hit ? hit.GetComponentInParent<ElementReceiver>() : null;
            if (receiver && receiver.isActiveAndEnabled)
            {
                receiver.Receive(element, amount * (oil ? 1f : effects.ExtinguishMultiplier));
                if(powder&&powderSurfaceSplash)ApplyPowderSplash(point,direction,amount,radius,residueSeconds,effects);
                return;
            }

            bool fixedSolid = hit && hit.gameObject.layer == 8 &&
                (!hit.attachedRigidbody || hit.attachedRigidbody.isKinematic);
            if(powder&&fixedSolid&&powderSurfaceSplash)
                ApplyPowderSplash(point,direction,amount,radius,residueSeconds,effects);
            if (!fixedSolid && field)
            {
                waterImpact = new Vector3(point.x, field.Origin.y + GroundClearance, point.z);
                if (waterCanReach == null)
                    waterCanReach = WaterReachesCell;

                if (water)
                    field.ApplyTimedWater(point, amount, radius, residueSeconds,
                        effects.ExtinguishMultiplier, effects.ResidueMultiplier, waterCanReach);
                else if (oil)
                    field.ApplyTimedOil(point, amount * effects.ResidueMultiplier, radius, residueSeconds, waterCanReach);
                else
                    field.ApplyTimedDryPowder(point, amount, radius, residueSeconds,
                        effects.ExtinguishMultiplier, effects.ResidueMultiplier, waterCanReach);
            }

            if (!water || !hit)
                return;

            FirefighterMotor player = hit.GetComponentInParent<FirefighterMotor>();
            if (player)
                player.AddImpulse(direction * amount * 5f * effects.PushMultiplier);
            else if (hit.attachedRigidbody && !hit.attachedRigidbody.isKinematic)
                hit.attachedRigidbody.AddForce(direction * amount * waterPushImpulsePerUnit * effects.PushMultiplier, ForceMode.Impulse);
        }

        bool WaterReachesCell(Vector3 center)
        {
            center.y = waterImpact.y;
            return !Physics.Linecast(waterImpact, center, FixedObstacles, QueryTriggerInteraction.Ignore);
        }

        void ApplyPowderSplash(Vector3 point,Vector3 direction,float amount,float radius,float duration,SprayEffectSettings effects)
        {
            if(!field)return;
            // A powder cloud can clear the counter edge then settle, unlike a water
            // splash projected through a solid. Both legs still respect authored walls.
            powderImpact=point-direction.normalized*.08f+Vector3.up*.08f;
            powderImpact.y=Mathf.Max(powderImpact.y,field.Origin.y+.15f);
            if(powderCanReach==null)powderCanReach=PowderReachesCell;
            field.ApplyTimedDryPowder(point,amount,radius,duration,effects.ExtinguishMultiplier,effects.ResidueMultiplier,powderCanReach);
        }
        bool PowderReachesCell(Vector3 center)
        {
            center.y=field.Origin.y+.12f;
            var above=new Vector3(center.x,powderImpact.y,center.z);
            return !Physics.Linecast(powderImpact,above,FixedObstacles,QueryTriggerInteraction.Ignore)&&
                !Physics.Linecast(above,center,FixedObstacles,QueryTriggerInteraction.Ignore);
        }

        static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
