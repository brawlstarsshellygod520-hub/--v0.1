using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FireRescue.Editor
{
    // Invoked by the current-turn Play Mode driver; it creates no project assets or scene state.
    public static class SourceOutlineValidation
    {
        static readonly Color Gold = new Color(1f, .72f, .12f);
        static readonly Color Blue = new Color(.15f, .65f, 1f);

        public static IEnumerator Run(List<string> checks)
        {
            var root = new GameObject("Temporary source outline fixture");
            var player = new GameObject("Outline player"); player.transform.SetParent(root.transform);
            var hose = player.AddComponent<HoseConnection>();
            var input = player.AddComponent<LocalPlayerInput>();
            var near = Source(root.transform, "Near source", new Vector3(0, 0, .75f));
            var far = Source(root.transform, "Far source", new Vector3(0, 0, 1.35f));
            var unreadable = MakeUnreadable(near.transform.Find("Visual/Bottle cap").GetComponent<MeshFilter>());
            input.Initialize(null, hose, null, null, new[] { near, far }, Array.Empty<RescueNpc>());
            input.ReadDevices = false;
            var highlight = player.AddComponent<SourceHighlightView>(); highlight.Initialize(input, hose);
            input.TickCommand(Vector2.zero, Vector3.forward, false, false, false, 1f / 60f);
            yield return null;

            var nearVisual = near.transform.Find("Visual");
            var nearTarget = nearVisual.GetComponent<InteractionOutlineTarget>();
            Check(nearTarget && highlight.CurrentSource == near && highlight.HasVisibleOutline && nearTarget.OutlineRendererCount == 2,
                checks, "Only nearest source builds visible outline copies for its two Visual meshes");
            Check(OutlineRenderers(nearVisual).All(r => Similar(r.sharedMaterial.color, Gold)) &&
                near.GetComponentsInChildren<Collider>(true).Length == 1 &&
                nearTarget.GetComponentsInChildren<Collider>(true).Length == 0,
                checks, "Nearby unconnected source uses gold visual-only outline without copying collision");
            Check(nearVisual.GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == "Bottle cap outline").sharedMesh == unreadable,
                checks, "Read/Write-disabled replacement mesh safely retains a visible outline fallback");

            Check(hose.TryConnect(near), checks, "Fixture connects through unchanged hose API");
            input.TickCommand(Vector2.zero, Vector3.forward, false, false, false, 1f / 60f);
            yield return null;
            Check(OutlineRenderers(nearVisual).All(r => Similar(r.sharedMaterial.color, Blue)), checks,
                "Connected source changes the existing outline to blue without recreating source gameplay");

            near.transform.position = new Vector3(0, 0, 3.2f);
            far.transform.position = new Vector3(0, 0, .72f);
            hose.Disconnect();
            input.TickCommand(Vector2.zero, Vector3.forward, false, false, false, 1f / 60f);
            yield return null;
            var farVisual = far.transform.Find("Visual");
            var farTarget = farVisual.GetComponent<InteractionOutlineTarget>();
            Check(highlight.CurrentSource == far && !nearTarget.IsVisible && farTarget && farTarget.IsVisible &&
                OutlineRenderers(farVisual).All(r => Similar(r.sharedMaterial.color, Gold)), checks,
                "Changing nearest source hides the old object and selects only the new source");

            var extra = Primitive(farVisual, "Replacement mesh", PrimitiveType.Cylinder, new Vector3(.25f, .1f, 0), new Vector3(.16f, .2f, .16f));
            highlight.Refresh(far);
            yield return null;
            Check(farTarget.OutlineRendererCount == 3 && farTarget.IsVisible && extra.GetComponent<Collider>() == null,
                checks, "Explicit refresh rebuilds outline cache after Visual replacement without adding gameplay colliders");

            extra.GetComponent<Renderer>().enabled = false;
            yield return null;
            Check(!farVisual.GetComponentsInChildren<Renderer>(true).Single(r => r.name == "Replacement mesh outline").enabled &&
                farTarget.IsVisible, checks,
                "Disabling one authored renderer hides only its matching outline clone");

            far.gameObject.SetActive(false);
            input.TickCommand(Vector2.zero, Vector3.forward, false, false, false, 1f / 60f);
            yield return null;
            Check(!highlight.CurrentSource && !farTarget.IsVisible && !highlight.HasVisibleOutline,
                checks, "Inactive source immediately clears outline and selection");
            far.gameObject.SetActive(true);
            highlight.enabled = false;
            yield return null;
            Check(!nearTarget.IsVisible && !farTarget.IsVisible, checks, "Disabling presentation leaves no ghost outline");
            UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(unreadable);
            yield return null;
        }

        static FluidSource Source(Transform parent, string name, Vector3 position)
        {
            var root = new GameObject(name); root.transform.SetParent(parent); root.transform.position = position;
            var source = root.AddComponent<FluidSource>(); source.Configure(ElementKind.Water);
            // Fixture gameplay collision stays on the source root, never its Visual.
            var collision = root.AddComponent<BoxCollider>(); collision.center = new Vector3(0, .25f, 0); collision.size = new Vector3(.3f, .5f, .25f);
            var interaction = new GameObject("Interaction").transform; interaction.SetParent(root.transform, false); source.SetInteractionPoint(interaction);
            var visual = new GameObject("Visual").transform; visual.SetParent(root.transform, false);
            Primitive(visual, "Bottle body", PrimitiveType.Cube, new Vector3(0, .22f, 0), new Vector3(.28f, .42f, .22f));
            Primitive(visual, "Bottle cap", PrimitiveType.Cylinder, new Vector3(0, .49f, 0), new Vector3(.12f, .1f, .12f));
            return source;
        }

        static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale)
        {
            var child = GameObject.CreatePrimitive(type); child.name = name; child.transform.SetParent(parent, false);
            child.transform.localPosition = position; child.transform.localScale = scale;
            UnityEngine.Object.Destroy(child.GetComponent<Collider>());
            return child;
        }

        static Mesh MakeUnreadable(MeshFilter filter)
        {
            Mesh mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
            filter.sharedMesh = mesh;
            mesh.UploadMeshData(true);
            return mesh;
        }

        static IEnumerable<Renderer> OutlineRenderers(Transform visual) => visual.GetComponentsInChildren<Renderer>(true)
            .Where(r => r.name.EndsWith(" outline", StringComparison.Ordinal));
        static bool Similar(Color a, Color b) => Vector4.Distance(a, b) < .001f;
        static void Check(bool condition, List<string> checks, string label)
        {
            if (!condition) throw new InvalidOperationException("Checks before failure:\n" + string.Join("\n", checks) + "\nFAIL " + label);
            checks.Add("PASS " + label);
        }
    }
}
