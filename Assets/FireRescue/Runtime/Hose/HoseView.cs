using UnityEngine;

namespace FireRescue
{
    // A light visual tether, not a soft-body simulation. Length restriction belongs to HoseConnection.
    public sealed class HoseView : MonoBehaviour
    {
        [SerializeField] SprayEmitter emitter;
        LineRenderer cable;
        LineRenderer stream;
        Material material;
        readonly Vector3[] points = new Vector3[18];
        readonly Vector3[] routedPoints = new Vector3[36];
        bool straightInitialized;
        bool routedInitialized;
        HoseWindow routedWindow;
        public void Initialize(SprayEmitter spray) => emitter = spray;

        void Awake()
        {
            material = new Material(Shader.Find("Sprites/Default"));
            cable = CreateLine("Hose tube", .095f, Color.white, routedPoints.Length);
            stream = CreateLine("Fluid stream", .15f, Color.cyan, 2);
        }

        LineRenderer CreateLine(string label, float width, Color color, int count)
        {
            var line = new GameObject(label).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = material;
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color;
            line.numCapVertices = 3;
            line.positionCount = count;
            line.enabled = false;
            return line;
        }

        void LateUpdate()
        {
            bool connected = emitter && emitter.Connection && emitter.Connection.IsConnected;
            cable.enabled = connected;
            stream.enabled = connected && emitter.IsSpraying;
            if (!connected)
            {
                straightInitialized = false;
                routedInitialized = false;
                routedWindow = null;
                return;
            }
            Vector3 start = emitter.Connection.Source.Port;
            Vector3 end = emitter.HoseInletPosition;
            float slack = Mathf.Clamp01(1 - emitter.Connection.UsedLength / emitter.Connection.MaximumLength);
            HoseWindow window = emitter.Connection.ActiveWindow;
            if (window)
            {
                if (routedWindow != window)
                {
                    routedWindow = window;
                    routedInitialized = false;
                }
                straightInitialized = false;
                Vector3 route = window.RoutePosition;
                FillSegment(routedPoints, start, route, 0, slack, routedInitialized, true);
                FillSegment(routedPoints, route, end, points.Length, slack, routedInitialized, true);
                // Route vertices are never interpolated: visual routing always passes the window.
                routedPoints[0] = start;
                routedPoints[points.Length - 1] = route;
                routedPoints[points.Length] = route;
                routedPoints[routedPoints.Length - 1] = end;
                cable.positionCount = routedPoints.Length;
                cable.SetPositions(routedPoints);
                routedInitialized = true;
            }
            else
            {
                routedInitialized = false;
                routedWindow = null;
                FillSegment(points, start, end, 0, slack, straightInitialized);
                points[0] = start;
                points[points.Length - 1] = end;
                cable.positionCount = points.Length;
                cable.SetPositions(points);
                straightInitialized = true;
            }
            Color fluid = emitter.Connection.Source.Element == ElementKind.Water ? new Color(.15f, .8f, 1) :
                emitter.Connection.Source.Element == ElementKind.DryPowder ? new Color(.96f, .94f, .8f) : new Color(.3f, .2f, .08f);
            stream.startColor = stream.endColor = fluid;
            stream.SetPosition(0, emitter.NozzlePosition); stream.SetPosition(1, emitter.ImpactPoint);
        }

        void FillSegment(Vector3[] destination, Vector3 start, Vector3 end, int offset, float slack, bool smooth, bool routed = false)
        {
            for (int i = 0; i < points.Length; i++)
            {
                float t = i / (float)(points.Length - 1);
                Vector3 next = CablePoint(start, end, t, slack, routed);
                int index = offset + i;
                destination[index] = smooth ? Vector3.Lerp(destination[index], next, 1 - Mathf.Exp(-14 * Time.deltaTime)) : next;
            }
        }

        static Vector3 CablePoint(Vector3 start, Vector3 end, float t, float slack, bool routed)
        {
            Vector3 next = Vector3.Lerp(start, end, t);
            // A short window-to-nozzle segment must not inherit the full long-hose sag:
            // it would dive through the sill despite passing the exact route vertex.
            float span = Vector3.ProjectOnPlane(end - start, Vector3.up).magnitude;
            float sag = routed ? Mathf.Min(1.8f, span * .18f) : 1.8f;
            float wave = routed ? Mathf.Min(.25f, span * .06f) : .25f;
            next.y = Mathf.Max(.09f, next.y - Mathf.Sin(t * Mathf.PI) * sag * slack);
            next += Vector3.Cross((end - start).normalized, Vector3.up) * Mathf.Sin(t * Mathf.PI * 2) * slack * wave;
            return next;
        }

        void OnDisable()
        {
            if (cable) cable.enabled = false;
            if (stream) stream.enabled = false;
            straightInitialized = false;
            routedInitialized = false;
            routedWindow = null;
        }

        void OnDestroy() { if (material) Destroy(material); }
    }
}
