using UnityEngine;

namespace FireRescue
{
    // Reads the exact same reachable candidate as the connect command; no gameplay or model dependency.
    public sealed class SourceHighlightView : MonoBehaviour
    {
        [SerializeField] LocalPlayerInput input;
        [SerializeField] HoseConnection hose;
        [SerializeField, Min(.1f)] float radius = .65f;
        readonly Vector3[] points = new Vector3[64];
        LineRenderer ring;
        Material material;

        public void Initialize(LocalPlayerInput owner, HoseConnection connection)
        { input = owner; hose = connection; }

        void Awake()
        {
            material = new Material(Shader.Find("Sprites/Default"));
            ring = new GameObject("Source proximity ring").AddComponent<LineRenderer>();
            ring.transform.SetParent(transform, false);
            ring.sharedMaterial = material;
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = points.Length;
            ring.startWidth = ring.endWidth = .045f;
            ring.enabled = false;
        }

        void LateUpdate()
        {
            FluidSource source = input && input.isActiveAndEnabled ? input.NearbySource : null;
            ring.enabled = source && source.isActiveAndEnabled;
            if (!ring.enabled) return;
            Vector3 center = source.transform.position + Vector3.up * .045f;
            Color color = hose && hose.Source == source ? new Color(.15f, .65f, 1f) : new Color(1f, .72f, .12f);
            ring.startColor = ring.endColor = color;
            for (int i = 0; i < points.Length; i++)
            {
                float angle = 2 * Mathf.PI * i / points.Length;
                points[i] = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            }
            ring.SetPositions(points);
        }

        void OnDisable() { if (ring) ring.enabled = false; }
        void OnDestroy()
        {
            if (ring) Destroy(ring.gameObject);
            if (material) Destroy(material);
        }
    }
}
