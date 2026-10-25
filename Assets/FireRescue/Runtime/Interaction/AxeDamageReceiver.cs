using System;
using UnityEngine;

namespace FireRescue
{
    /// <summary>
    /// Gameplay-side durability for objects that can be struck by the firefighter axe.
    /// Presentation is deliberately not referenced here.
    /// </summary>
    public class AxeDamageReceiver : MonoBehaviour
    {
        [SerializeField, Min(.01f)] private float maximumDurability = 3f;
        private float remainingDurability;
        private bool initialized;
        private bool depleted;

        public float MaximumDurability => maximumDurability;
        public float RemainingDurability
        {
            get
            {
                EnsureInitialized();
                return remainingDurability;
            }
        }
        public bool IsDepleted
        {
            get
            {
                EnsureInitialized();
                return depleted;
            }
        }

        public event Action<AxeDamageReceiver, float> HitReceived;
        public event Action<AxeDamageReceiver> Depleted;

        protected virtual void Awake() => ResetDamageState();

        /// <summary>Configures a newly built gameplay object and resets its damage state.</summary>
        public virtual void Configure(float durability)
        {
            maximumDurability = PositiveFiniteOrDefault(durability, maximumDurability);
            ResetDamageState();
        }

        /// <summary>Applies one logical axe impact. Returns false for invalid or exhausted targets.</summary>
        public bool ReceiveAxeHit(float damage)
        {
            EnsureInitialized();
            if (depleted || !IsPositiveFinite(damage)) return false;

            remainingDurability = Mathf.Max(0f, remainingDurability - damage);
            HitReceived?.Invoke(this, damage);
            if (remainingDurability > 0f) return true;

            depleted = true;
            OnDepleted();
            Depleted?.Invoke(this);
            return true;
        }

        /// <summary>Restores initial durability for a new play session or an explicit builder reset.</summary>
        public virtual void ResetDamageState()
        {
            maximumDurability = PositiveFiniteOrDefault(maximumDurability, 3f);
            remainingDurability = maximumDurability;
            depleted = false;
            initialized = true;
        }

        protected virtual void OnDepleted() { }

        private void EnsureInitialized()
        {
            if (!initialized) ResetDamageState();
        }

        private static bool IsPositiveFinite(float value) =>
            value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);

        private static float PositiveFiniteOrDefault(float value, float fallback) =>
            IsPositiveFinite(value) ? value : fallback;
    }
}
