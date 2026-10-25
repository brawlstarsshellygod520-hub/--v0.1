using UnityEngine;

namespace FireRescue
{
    // Presentation-only companion for FirefighterTools. Disabling this view never changes tool gameplay state.
    public sealed class FirefighterToolsView : MonoBehaviour
    {
        [SerializeField] FirefighterTools tools;
        [SerializeField] SprayEmitter emitter;
        [SerializeField] Transform axePivot;
        [SerializeField] GameObject hoseVisual;
        [SerializeField] GameObject nozzleVisual;
        Quaternion axeBaseRotation;
        bool baseRotationCaptured;
        bool axeVisible;
        bool hoseVisible;

        public void Initialize(FirefighterTools toolComponent, SprayEmitter sprayEmitter, Transform axe,
            GameObject hose, GameObject nozzle)
        {
            tools = toolComponent;
            emitter = sprayEmitter;
            axePivot = axe;
            hoseVisual = hose;
            nozzleVisual = nozzle;
            CaptureAxeBaseRotation();
            RefreshVisibility(FirefighterTools.Tool.None);
        }

        void Awake()
        {
            CaptureAxeBaseRotation();
            RefreshVisibility(FirefighterTools.Tool.None);
        }

        void LateUpdate()
        {
            FirefighterTools.Tool selected = tools && tools.isActiveAndEnabled
                ? tools.SelectedTool : FirefighterTools.Tool.None;
            RefreshVisibility(selected);
            if (!axePivot || !baseRotationCaptured) return;

            float swing = tools && tools.IsSwinging ? Mathf.Sin(tools.SwingProgress * Mathf.PI) : 0;
            // Positive local X moves an upright handle forward (+Z), matching the gameplay strike direction.
            axePivot.localRotation = axeBaseRotation * Quaternion.AngleAxis(105 * swing,Vector3.right);
        }

        void OnDisable()
        {
            RefreshVisibility(FirefighterTools.Tool.None);
            if (axePivot && baseRotationCaptured) axePivot.localRotation = axeBaseRotation;
        }

        void CaptureAxeBaseRotation()
        {
            if (!axePivot) return;
            axeBaseRotation = axePivot.localRotation;
            baseRotationCaptured = true;
        }

        void RefreshVisibility(FirefighterTools.Tool selected)
        {
            SetVisible(axePivot ? axePivot.gameObject : null,selected == FirefighterTools.Tool.Axe,ref axeVisible);
            bool showHose = selected == FirefighterTools.Tool.Hose;
            SetVisible(hoseVisual,showHose,ref hoseVisible);
            // The nozzle is part of the hose presentation even when the stream itself is blocked or disconnected.
            if (nozzleVisual && nozzleVisual != hoseVisual) nozzleVisual.SetActive(showHose);
        }

        static void SetVisible(GameObject visual, bool visible, ref bool current)
        {
            if (!visual || (current == visible && visual.activeSelf == visible)) return;
            visual.SetActive(visible);
            current = visible;
        }
    }
}
