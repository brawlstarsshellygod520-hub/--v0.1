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
    }
}
