using System.Collections.Generic;
using UnityEngine;

namespace FireRescue
{
    /// <summary>
    /// Presentation-only source feedback. It follows the exact source selected by
    /// LocalPlayerInput and never changes reach, connection, or source ownership.
    /// </summary>
    public sealed class SourceHighlightView : MonoBehaviour
    {
        static readonly Color Gold = new Color(1f, .72f, .12f);
        static readonly Color Blue = new Color(.15f, .65f, 1f);

        [SerializeField] LocalPlayerInput input;
        [SerializeField] HoseConnection hose;
        // Kept serialized so player builds retain the small Built-in shader even if no
        // authoring material has been created yet.
        [SerializeField] Shader outlineShader;
        readonly Dictionary<FluidSource, InteractionOutlineTarget> targets = new Dictionary<FluidSource, InteractionOutlineTarget>();
        Material goldMaterial;
        Material blueMaterial;
        FluidSource current;

        public FluidSource CurrentSource => current;
        public bool HasVisibleOutline => current && targets.TryGetValue(current, out InteractionOutlineTarget target) && target && target.IsVisible;

        public void Initialize(LocalPlayerInput owner, HoseConnection connection)
        { input = owner; hose = connection; }

        /// <summary>Call after replacing a selected source's Visual child at runtime.</summary>
        public void Refresh(FluidSource source = null)
        {
            if (source != null && targets.TryGetValue(source, out InteractionOutlineTarget target) && target)
                target.Rebuild();
            else if (source == null && current && targets.TryGetValue(current, out target) && target)
                target.Rebuild();
        }

        void Awake()
        {
            if (!outlineShader) outlineShader = Shader.Find("FireRescue/InteractionOutline");
            if (!outlineShader) return;
            goldMaterial = NewMaterial("Interactable source outline (gold)", Gold);
            blueMaterial = NewMaterial("Connected source outline (blue)", Blue);
        }

        void LateUpdate()
        {
            FluidSource next = input && input.isActiveAndEnabled ? input.NearbySource : null;
            if (!next || !next.isActiveAndEnabled) next = null;
            if (next != current)
            {
                HideCurrent();
                current = next;
            }
            if (!current || !outlineShader) return;
            InteractionOutlineTarget target = TargetFor(current);
            // SetVisible builds a newly discovered target before evaluating its renderers.
            // Do not query HasRenderableVisual first or the first nearby source would
            // never create its hull.
            if (target) target.SetVisible(hose && hose.Source == current ? blueMaterial : goldMaterial);
        }

        InteractionOutlineTarget TargetFor(FluidSource source)
        {
            if (targets.TryGetValue(source, out InteractionOutlineTarget existing) && existing) return existing;
            Transform visual = source.transform.Find("Visual");
            // Do not fall back to collision/gameplay objects. A source without a Visual
            // simply has no presentation marker until art is supplied.
            if (!visual || !visual.gameObject.activeInHierarchy) return null;
            var target = visual.GetComponent<InteractionOutlineTarget>();
            if (!target) target = visual.gameObject.AddComponent<InteractionOutlineTarget>();
            target.SetVisualRoot(visual);
            targets[source] = target;
            return target;
        }

        void HideCurrent()
        {
            if (current && targets.TryGetValue(current, out InteractionOutlineTarget target) && target)
                target.SetVisible(null);
        }

        Material NewMaterial(string name, Color color)
        {
            var material = new Material(outlineShader) { name = name, hideFlags = HideFlags.DontSave };
            material.color = color;
            return material;
        }

        void OnDisable()
        {
            HideCurrent();
            current = null;
        }
        void OnDestroy()
        {
            HideCurrent();
            if (goldMaterial) Destroy(goldMaterial);
            if (blueMaterial) Destroy(blueMaterial);
        }
    }
}
