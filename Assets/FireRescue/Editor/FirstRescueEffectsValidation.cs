using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FireRescue.Editor
{
    /// <summary>Small lifecycle probe for FirstRescueEffects; safe to call from existing Play Mode validation drivers.</summary>
    public static class FirstRescueEffectsValidation
    {
        public static IEnumerator Run(List<string> checks)
        {
            GameObject root = new GameObject("First Rescue Effects Validation");
            FirstRescueEffects effects = root.AddComponent<FirstRescueEffects>();
            effects.Initialize(null, null);

            effects.Mode = FirstRescueEffectMode.BillboardParticles;
            yield return null;
            Add(checks, effects.Mode == FirstRescueEffectMode.BillboardParticles,
                "billboard mode can be selected without a gameplay source");

            effects.Mode = FirstRescueEffectMode.DropletMeshes;
            yield return null;
            Add(checks, effects.Mode == FirstRescueEffectMode.DropletMeshes,
                "droplet-mesh mode can be selected without a gameplay source");

            GameObject gameplay = new GameObject("First Rescue Effects Gameplay Source");
            SprayEmitter emitter = gameplay.AddComponent<SprayEmitter>();
            FireField field = gameplay.AddComponent<FireField>();
            field.Configure(Vector3.zero, 2, 2, 1f);
            field.SetIgnitionSeeds(new[] { Vector2Int.zero });
            field.ResetSimulation();
            field.Simulate = false;
            effects.Initialize(emitter, field);
            int emittedBeforeDisable = emitter.TotalEmitted;
            int particlesBeforeDisable = emitter.ActiveParticleCount;
            int burningBeforeDisable = field.BurningCount;

            effects.Mode = FirstRescueEffectMode.Hybrid;
            root.SetActive(false);
            yield return null;
            Add(checks, !effects.IsSubscribed && effects.ActiveBillboardCount == 0 && effects.ActiveDropletCount == 0 &&
                emitter.TotalEmitted == emittedBeforeDisable && emitter.ActiveParticleCount == particlesBeforeDisable &&
                field.BurningCount == burningBeforeDisable,
                "disabling presentation clears visuals without changing emitter or FireField state");

            Object.Destroy(root);
            Object.Destroy(gameplay);
        }

        static void Add(List<string> checks, bool passed, string name)
        {
            if (checks != null) checks.Add((passed ? "PASS: " : "FAIL: ") + name);
        }
    }
}
