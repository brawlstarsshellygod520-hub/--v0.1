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
        readonly Vector3[] routedPoints = new Vector3[18*(HoseConnection.MaximumPathPoints-1)];
        bool routedInitialized;
        HoseWindow routedWindow;
        FluidSource drawnSource;
        int drawnPathCount;
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
            // Directional particles have their own read-only view; never draw the old instant laser.
            stream.enabled = connected && emitter.IsSpraying && !emitter.UsesDirectionalSpray;
            if (!connected)
            {
                routedInitialized = false;
                routedWindow = null;
                drawnSource = null;
                return;
            }
            if (drawnSource != emitter.Connection.Source)
            {
                // A new source must not interpolate a cable across the old source's room.
                drawnSource = emitter.Connection.Source;
                routedInitialized = false;
                routedWindow = null;
            }
            float slack = Mathf.Clamp01(1 - emitter.Connection.UsedLength / emitter.Connection.MaximumLength);
            HoseWindow window = emitter.Connection.ActiveWindow;
            int count=emitter.Connection.PathPointCount;
            bool smooth=routedInitialized && drawnPathCount==count && routedWindow==window;
            bool clear=true;
            for(int segment=0;segment<count-1;segment++)
            {
                Vector3 start=emitter.Connection.GetPathPoint(segment);
                Vector3 end=segment==count-2?emitter.HoseInletPosition:emitter.Connection.GetPathPoint(segment+1);
                int offset=segment*points.Length;
                FillSegment(routedPoints,start,end,offset,slack,smooth,count>2);
                // Contact nodes are authoritative and never interpolated through a wall.
                routedPoints[offset]=start;routedPoints[offset+points.Length-1]=end;
                clear &= KeepClear(routedPoints,offset,start,end);
            }
            cable.enabled=clear;cable.positionCount=(count-1)*points.Length;
            for(int i=0;i<cable.positionCount;i++)cable.SetPosition(i,routedPoints[i]);
            routedInitialized=true;drawnPathCount=count;routedWindow=window;
            Color fluid = emitter.Connection.Source.Element == ElementKind.Water ? new Color(.15f, .8f, 1) :
                emitter.Connection.Source.Element == ElementKind.DryPowder ? new Color(.96f, .94f, .8f) : new Color(.3f, .2f, .08f);
            stream.startColor = stream.endColor = fluid;
            stream.SetPosition(0, emitter.NozzlePosition); stream.SetPosition(1, emitter.ImpactPoint);
        }

        bool KeepClear(Vector3[] destination, int offset, Vector3 start, Vector3 end)
        {
            var connection = emitter.Connection;
            if (!connection.HasCollisionGeometry) return true;
            for (int i = 1; i < points.Length; i++)
            {
                if (!connection.IsCableBlocked(destination[offset + i - 1], destination[offset + i])) continue;
                // Slack/smoothing may clip a window sill or corner even when the route is valid.
                // Pull this section straight rather than drawing through authored solid geometry.
                for (int j = 0; j < points.Length; j++)
                    destination[offset + j] = Vector3.Lerp(start, end, j / (float)(points.Length - 1));
                return !connection.IsCableBlocked(start, end);
            }
            return true;
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
            routedInitialized = false;
            routedWindow = null;
            drawnSource = null;
        }

        void OnDestroy() { if (material) Destroy(material); }
    }
}
