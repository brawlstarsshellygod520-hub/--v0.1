using System;
using UnityEngine;

namespace FireRescue
{
    /// <summary>
    /// A gameplay door that opens its route only after its inherited axe durability is depleted.
    /// The root remains active; only the explicitly assigned route-blocking collider changes.
    /// </summary>
    public sealed class BreakableDoor : AxeDamageReceiver
    {
        [SerializeField] private Collider blockingCollider;

        public Collider BlockingCollider => blockingCollider;
        public bool IsBroken => IsDepleted;

        public event Action<BreakableDoor> Broken;

        /// <summary>Builder-friendly setup for a door whose collider already lives on its gameplay root.</summary>
        public void Initialize(Collider routeBlockingCollider)
        {
            blockingCollider = routeBlockingCollider;
            ResetDamageState();
        }

        /// <summary>Builder-friendly complete setup; invalid durability keeps the prior safe value.</summary>
        public void Configure(float durability, Collider routeBlockingCollider)
        {
            blockingCollider = routeBlockingCollider;
            base.Configure(durability);
        }

        public override void ResetDamageState()
        {
            base.ResetDamageState();
            if (blockingCollider) blockingCollider.enabled = true;
        }

        protected override void OnDepleted()
        {
            if (blockingCollider) blockingCollider.enabled = false;
            Broken?.Invoke(this);
        }
    }
}
