using UnityEngine;

namespace FireRescue
{
    public sealed class HoseConnection : MonoBehaviour
    {
        [SerializeField, Min(1)] float maximumLength = 3;
        [SerializeField] float connectDistance = 2;
        [SerializeField] Transform interactionOrigin;
        readonly RaycastHit[] reachHits = new RaycastHit[16];
        HoseWindow activeWindow;
        public FluidSource Source { get; private set; }
        public bool IsConnected => Source;
        public float MaximumLength => maximumLength;
        public HoseWindow ActiveWindow
        {
            get
            {
                // Invalid routes fall back to the established direct connection.
                if (!activeWindow || !activeWindow.isActiveAndEnabled) activeWindow = null;
                return activeWindow;
            }
        }
        public float UsedLength
        {
            get
            {
                if (!Source) return 0f;
                HoseWindow window = ActiveWindow;
                return window
                    ? PlanarDistance(Source.transform.position, window.RoutePosition) + PlanarDistance(window.RoutePosition, transform.position)
                    : Vector3.Distance(transform.position, Source.transform.position);
            }
        }
        public void SetInteractionOrigin(Transform origin) => interactionOrigin = origin;

        public bool TryConnect(FluidSource source)
        {
            if (!CanConnect(source)) return false;
            Source = source;
            ClearRoute();
            return true;
        }

        public bool TryRouteThrough(HoseWindow window)
        {
            if (!Source || !Source.isActiveAndEnabled || !window || !window.CanReach(transform.position)) return false;
            float used = PlanarDistance(Source.transform.position, window.RoutePosition) + PlanarDistance(window.RoutePosition, transform.position);
            if (used > maximumLength) return false;
            activeWindow = window;
            return true;
        }

        public void ClearRoute() => activeWindow = null;

        public bool CanConnect(FluidSource source)
        {
            if (!source || !source.isActiveAndEnabled ||
                Vector3.Distance(transform.position,source.transform.position) > connectDistance) return false;
            // The low wall mesh is a cutaway: check reach below its top, not at the raised bottle mouth.
            Vector3 start = interactionOrigin ? interactionOrigin.position : transform.position + Vector3.up * .35f;
            Vector3 end = source.InteractionPosition;
            Vector3 delta = end-start;
            if (delta.sqrMagnitude < .0001f) return true;
            int count = Physics.RaycastNonAlloc(start,delta.normalized,reachHits,delta.magnitude,1 << 8,QueryTriggerInteraction.Ignore);
            if (count == reachHits.Length) return false;
            for (int i=0;i<count;i++)
            {
                Collider obstacle = reachHits[i].collider;
                // A bottle standing on a counter can be reached; its support and own body are not walls.
                if (obstacle.transform.IsChildOf(source.transform) || obstacle.bounds.Contains(end)) continue;
                return false;
            }
            return true;
        }

        public void Disconnect()
        {
            Source = null;
            ClearRoute();
        }

        public void Interact(FluidSource[] sources) => Interact(sources, null);

        public void Interact(FluidSource[] sources, HoseWindow[] windows)
        {
            FluidSource source = FindNearestSource(sources);
            HoseWindow window = FindReachableWindow(windows);
            // A short hose necessarily puts its source near the window. Prefer the closer
            // window over disconnecting the SAME source; switching sources remains explicit.
            if (Source && window && (!source || (source == Source &&
                PlanarDistance(window.RoutePosition, transform.position) <
                PlanarDistance(source.transform.position, transform.position))))
            {
                if (ActiveWindow == window) { ClearRoute(); return; }
                if (TryRouteThrough(window)) return;
            }
            if (source)
            {
                if (source != Source) TryConnect(source);
                else Disconnect();
                return;
            }

            if (Source && window)
            {
                if (ActiveWindow == window) { ClearRoute(); return; }
                if (TryRouteThrough(window)) return;
            }

            Disconnect();
        }

        public Vector3 ConstrainPosition(Vector3 desired)
        {
            if (!Source) return desired;
            HoseWindow window = ActiveWindow;
            if (window)
            {
                Vector3 pivot = window.RoutePosition;
                float prefixLength = PlanarDistance(Source.transform.position, pivot);
                float remainingLength = Mathf.Max(0f, maximumLength - prefixLength);
                Vector3 routedDelta = desired - pivot;
                routedDelta.y = 0;
                Vector3 routedLimited = pivot + Vector3.ClampMagnitude(routedDelta, remainingLength);
                return new Vector3(routedLimited.x, desired.y, routedLimited.z);
            }
            Vector3 anchor = Source.transform.position;
            Vector3 delta = desired - anchor;
            delta.y = 0;
            Vector3 limited = anchor + Vector3.ClampMagnitude(delta, maximumLength);
            return new Vector3(limited.x, desired.y, limited.z);
        }

        public FluidSource FindNearestSource(FluidSource[] sources)
        {
            FluidSource nearest = null;
            float distance = connectDistance;
            if (sources == null) return null;
            foreach (FluidSource candidate in sources)
            {
                if (!candidate) continue;
                float next = Vector3.Distance(candidate.transform.position, transform.position);
                if (next <= distance && CanConnect(candidate)) { nearest = candidate; distance = next; }
            }
            return nearest;
        }

        HoseWindow FindReachableWindow(HoseWindow[] windows)
        {
            HoseWindow nearest = null;
            float distance = float.MaxValue;
            if (windows == null) return null;
            foreach (HoseWindow candidate in windows)
            {
                if (!candidate || !candidate.CanReach(transform.position)) continue;
                float next = PlanarDistance(candidate.RoutePosition, transform.position);
                if (next < distance) { nearest = candidate; distance = next; }
            }
            return nearest;
        }

        static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
