using UnityEngine;

namespace FireRescue
{
    public sealed class FluidSource : MonoBehaviour
    {
        [SerializeField] ElementKind element;
        [SerializeField, Min(.1f)] float flowRate = 2;
        [SerializeField] Transform outlet;
        [SerializeField] Transform interactionPoint;
        public ElementKind Element => element;
        public float FlowRate => flowRate;
        public Vector3 Port => outlet ? outlet.position : transform.position + Vector3.up * .6f;
        public Vector3 InteractionPosition => interactionPoint ? interactionPoint.position : transform.position + Vector3.up * .35f;
        public void Configure(ElementKind kind, float rate = 2) { element = kind; flowRate = rate; }
        public void SetOutlet(Transform port) => outlet = port;
        public void SetInteractionPoint(Transform point) => interactionPoint = point;
        // Sources are unlimited and non-exclusive in P1: each player owns their own connection.
    }
}
