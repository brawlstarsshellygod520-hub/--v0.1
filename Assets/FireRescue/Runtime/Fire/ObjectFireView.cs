using UnityEngine;

namespace FireRescue
{
    /// <summary>
    /// Presentation-only companion for ObjectFire. It observes fire state but never changes it.
    /// </summary>
    public sealed class ObjectFireView : MonoBehaviour
    {
        [SerializeField] private ObjectFire fire;
        [SerializeField] private Transform flameRoot;
        private Vector3 baseScale;
        private bool hasBaseScale;

        public void Initialize(ObjectFire objectFire, Transform root)
        {
            fire = objectFire;
            if (flameRoot != root)
            {
                flameRoot = root;
                hasBaseScale = false;
            }

            CaptureBaseScale();
            Refresh();
        }

        private void OnEnable()
        {
            CaptureBaseScale();
            Refresh();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        private void OnDisable()
        {
            if (flameRoot) flameRoot.gameObject.SetActive(false);
        }

        private void CaptureBaseScale()
        {
            if (hasBaseScale || flameRoot == null) return;
            baseScale = flameRoot.localScale;
            hasBaseScale = true;
        }

        private void Refresh()
        {
            if (fire == null || flameRoot == null) return;
            bool visible = fire.IsBurning;
            if (flameRoot.gameObject.activeSelf != visible)
                flameRoot.gameObject.SetActive(visible);
            if (visible && hasBaseScale)
                flameRoot.localScale = baseScale * fire.Intensity;
        }
    }
}
