using System;
using UnityEngine;

namespace FireRescue
{
    public enum FireKind
    {
        Ordinary = 0,
        CookingOil = 1
    }

    /// <summary>
    /// Self-contained fire state for a single scene object. Spread and visual presentation
    /// deliberately belong to other components.
    /// </summary>
    public sealed class ObjectFire : ElementReceiver
    {
        [SerializeField] private FireKind kind = FireKind.Ordinary;
        [SerializeField, Min(0f)] private float initialIntensity = 1f;
        [SerializeField, Min(0f)] private float escapeHazardRadius = .7f;
        // Optional exhibition rule. Existing ordinary/oil fires keep their previous behaviour.
        [SerializeField, Min(0f)] private float waterGrowth;
        [SerializeField, Min(1f)] private float maximumIntensity = 3f;
        private float intensity = 1f;
        public float WaterApplied { get; private set; }
        public event Action<float> OilContactedByWater;

        public FireKind Kind => kind;
        public float InitialIntensity => initialIntensity;
        public float Intensity => intensity;
        public bool IsBurning => intensity > 0f;

        // Navigation reads gameplay state, never flame renderers or model bounds.
        public bool BlocksEscapeAt(Vector3 position, float clearance)
        {
            if (!isActiveAndEnabled || !IsBurning) return false;
            Vector3 delta = position - transform.position;
            delta.y = 0f;
            float radius = Mathf.Max(0f, escapeHazardRadius) + Mathf.Max(0f, clearance);
            return delta.sqrMagnitude <= radius * radius;
        }

        private void Awake() => intensity = PositiveFiniteOrZero(initialIntensity);

        public void Configure(FireKind fireKind, float value)
        {
            kind = fireKind;
            initialIntensity = PositiveFiniteOrZero(value);
            intensity = initialIntensity;
            WaterApplied = 0f;
        }

        public void ConfigureWaterEscalation(float growth, float cap)
        { waterGrowth = PositiveFiniteOrZero(growth); maximumIntensity = Mathf.Max(initialIntensity, PositiveFiniteOrZero(cap)); }

        public void Ignite(float value = 1f)
        {
            if (!IsPositiveFinite(value)) return;
            intensity = Mathf.Max(intensity, value);
        }

        public override void Receive(ElementKind element, float amount)
        {
            if (!IsPositiveFinite(amount) || !IsBurning) return;

            switch (element)
            {
                case ElementKind.Water when kind == FireKind.CookingOil:
                    WaterApplied += amount;
                    if (waterGrowth > 0) intensity = Mathf.Min(maximumIntensity, intensity + amount * waterGrowth);
                    OilContactedByWater?.Invoke(amount);
                    break;
                case ElementKind.DryPowder:
                    Reduce(amount);
                    break;
                case ElementKind.Water when kind == FireKind.Ordinary:
                    Reduce(amount);
                    break;
            }
        }

        private void Reduce(float amount)
        {
            intensity = Mathf.Max(0f, intensity - amount * .5f);
        }

        private static bool IsPositiveFinite(float value) =>
            value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);

        private static float PositiveFiniteOrZero(float value) =>
            IsPositiveFinite(value) ? value : 0f;
    }
}
