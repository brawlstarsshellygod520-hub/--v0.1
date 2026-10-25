using UnityEngine;

namespace FireRescue
{
    /// <summary>
    /// Presentation-only door swap. The door's gameplay root, state and collider remain untouched.
    /// </summary>
    public sealed class BreakableDoorView : MonoBehaviour
    {
        [SerializeField] private BreakableDoor door;
        [SerializeField] private GameObject intactVisual;
        [SerializeField] private GameObject brokenVisual;
        private bool subscribed;

        /// <summary>Builder-friendly setup for replacing or omitting either visual independently.</summary>
        public void Initialize(BreakableDoor doorComponent, GameObject intactModel, GameObject brokenModel)
        {
            Unsubscribe();
            door = doorComponent;
            intactVisual = intactModel;
            brokenVisual = brokenModel;
            Subscribe();
            Refresh();
        }

        public void Configure(BreakableDoor doorComponent, GameObject intactModel, GameObject brokenModel) =>
            Initialize(doorComponent, intactModel, brokenModel);

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable() => Unsubscribe();

        private void OnDoorBroken(BreakableDoor _) => Refresh();

        private void Subscribe()
        {
            if (subscribed || !door) return;
            door.Broken += OnDoorBroken;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || !door) return;
            door.Broken -= OnDoorBroken;
            subscribed = false;
        }

        private void Refresh()
        {
            bool broken = door && door.IsBroken;
            SetVisualActive(intactVisual, !broken);
            SetVisualActive(brokenVisual, broken);
        }

        private void SetVisualActive(GameObject visual, bool visible)
        {
            if (!visual || visual == gameObject || transform.IsChildOf(visual.transform)) return;
            if (visual.activeSelf != visible) visual.SetActive(visible);
        }
    }
}
