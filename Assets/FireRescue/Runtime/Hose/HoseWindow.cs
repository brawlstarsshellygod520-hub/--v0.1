using UnityEngine;

namespace FireRescue
{
    /// <summary>
    /// A single explicit hose routing point on a window. It contains no rendering or rope logic.
    /// </summary>
    public sealed class HoseWindow : MonoBehaviour
    {
        [SerializeField] private Transform routePoint;
        [SerializeField] private Transform interactionPoint;
        [SerializeField, Min(.01f)] private float interactionDistance = 1.8f;
        // Gameplay aperture, independent of the replaceable frame mesh.
        [SerializeField] private Vector3 sprayOpeningCenter = new Vector3(0, 1.205f, 0);
        // Depth includes the imported wall thickness: the frame is authored on its outside face.
        [SerializeField] private Vector3 sprayOpeningSize = new Vector3(1f, .61f, 1.1f);

        public Vector3 RoutePosition => routePoint ? routePoint.position : transform.position;

        public void Initialize(Transform routeAnchor, Transform interactionAnchor)
        {
            routePoint = routeAnchor;
            interactionPoint = interactionAnchor;
        }

        public bool CanReach(Vector3 playerPoint)
        {
            if (!isActiveAndEnabled) return false;
            Vector3 point = interactionPoint ? interactionPoint.position : transform.position;
            return Vector3.Distance(playerPoint, point) <= interactionDistance;
        }

        public bool AllowsSpray(Vector3 worldPoint, float radius)
        {
            if (!isActiveAndEnabled) return false;
            Vector3 p = transform.InverseTransformPoint(worldPoint) - sprayOpeningCenter;
            Vector3 half = sprayOpeningSize * .5f;
            Vector3 scale = transform.lossyScale;
            float ry = radius / Mathf.Max(.001f, Mathf.Abs(scale.y));
            float rz = radius / Mathf.Max(.001f, Mathf.Abs(scale.z));
            return Mathf.Abs(p.x) <= half.x && Mathf.Abs(p.y) + ry <= half.y && Mathf.Abs(p.z) + rz <= half.z;
        }
    }
}
