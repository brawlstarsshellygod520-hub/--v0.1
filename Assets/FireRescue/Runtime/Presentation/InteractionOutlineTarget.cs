using System.Collections.Generic;
using UnityEngine;

namespace FireRescue
{
    /// <summary>
    /// Clones only renderers under a supplied Visual root into lightweight inverted
    /// hull renderers. Gameplay colliders, anchors and source data are deliberately
    /// never inspected or copied.
    /// </summary>
    public sealed class InteractionOutlineTarget : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        readonly List<Renderer> originals = new List<Renderer>();
        readonly List<Renderer> outlines = new List<Renderer>();
        readonly List<Material[]> materialSlots = new List<Material[]>();
        readonly List<Mesh> outlineMeshes = new List<Mesh>();
        bool built;
        Material activeMaterial;

        public bool IsVisible { get; private set; }
        public int OutlineRendererCount => outlines.Count;
        public bool HasRenderableVisual
        {
            get
            {
                if (!built || !visualRoot || !visualRoot.gameObject.activeInHierarchy) return false;
                for (int i = 0; i < originals.Count; i++)
                    if (CanOutline(originals[i])) return true;
                return false;
            }
        }

        public void SetVisualRoot(Transform root)
        {
            if (visualRoot == root) return;
            Clear();
            visualRoot = root;
        }

        /// <summary>Explicitly rebuild after artists replace objects below Visual.</summary>
        public void Rebuild()
        {
            bool visible = IsVisible;
            Material material = outlines.Count > 0 && outlines[0] ? outlines[0].sharedMaterial : null;
            Clear();
            Build();
            if (visible) SetVisible(material);
        }

        public void SetVisible(Material material)
        {
            if (material && !built) Build();
            bool requested = material && isActiveAndEnabled && HasRenderableVisual;
            bool materialChanged = activeMaterial != material;
            activeMaterial = material;
            bool anyVisible = false;
            for (int i = 0; i < outlines.Count; i++)
            {
                Renderer outline = outlines[i];
                if (!outline) continue;
                bool visible = requested && CanOutline(originals[i]);
                if (originals[i] is SkinnedMeshRenderer sourceSkin && sourceSkin.sharedMesh && outline is SkinnedMeshRenderer outlineSkin)
                {
                    for (int shape = 0; shape < sourceSkin.sharedMesh.blendShapeCount; shape++)
                        outlineSkin.SetBlendShapeWeight(shape, sourceSkin.GetBlendShapeWeight(shape));
                }
                if (materialChanged && material)
                {
                    Material[] slots = materialSlots[i];
                    for (int slot = 0; slot < slots.Length; slot++) slots[slot] = material;
                    outline.sharedMaterials = slots;
                }
                outline.enabled = visible;
                anyVisible |= visible;
            }
            IsVisible = anyVisible;
        }

        void Build()
        {
            built = true;
            if (!visualRoot) return;
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer source = renderers[i];
                if (!CanOutline(source) || source is ParticleSystemRenderer || source.GetComponent<OutlineCloneMarker>()) continue;
                if (source is MeshRenderer meshRenderer)
                {
                    MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
                    if (!filter || !filter.sharedMesh) continue;
                    var clone = NewClone(source.transform, source.name + " outline");
                    Mesh outlineMesh = CreateOutlineMesh(filter.sharedMesh);
                    clone.AddComponent<MeshFilter>().sharedMesh = outlineMesh;
                    MeshRenderer renderer = clone.AddComponent<MeshRenderer>();
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.enabled = false;
                    AddPair(source, renderer, outlineMesh, outlineMesh != filter.sharedMesh);
                }
                else if (source is SkinnedMeshRenderer skin && skin.sharedMesh)
                {
                    // A separate SkinnedMeshRenderer shares the same mesh/bones; this keeps
                    // future animated replacement models outlined without BakeMesh churn.
                    var clone = NewClone(source.transform.parent, source.name + " outline");
                    clone.transform.localPosition = source.transform.localPosition;
                    clone.transform.localRotation = source.transform.localRotation;
                    clone.transform.localScale = source.transform.localScale;
                    var renderer = clone.AddComponent<SkinnedMeshRenderer>();
                    Mesh outlineMesh = CreateOutlineMesh(skin.sharedMesh);
                    renderer.sharedMesh = outlineMesh;
                    renderer.bones = skin.bones;
                    renderer.rootBone = skin.rootBone;
                    renderer.localBounds = skin.localBounds;
                    for (int shape = 0; shape < skin.sharedMesh.blendShapeCount; shape++)
                        renderer.SetBlendShapeWeight(shape, skin.GetBlendShapeWeight(shape));
                    renderer.updateWhenOffscreen = skin.updateWhenOffscreen;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.enabled = false;
                    AddPair(source, renderer, outlineMesh, outlineMesh != skin.sharedMesh);
                }
            }
        }

        void AddPair(Renderer original, Renderer outline, Mesh mesh, bool ownsMesh)
        {
            originals.Add(original);
            outlines.Add(outline);
            materialSlots.Add(new Material[Mathf.Max(1, mesh.subMeshCount)]);
            if (ownsMesh) outlineMeshes.Add(mesh);
        }

        // The supplied mesh is never mutated. Averaging duplicate-position normals makes
        // primitive cube edges a continuous hull instead of six visibly separated faces.
        // Imported production meshes may have Read/Write disabled; use their original
        // GPU mesh as a safe fallback so interaction feedback still works in a player.
        static Mesh CreateOutlineMesh(Mesh source)
        {
            if (!source.isReadable) return source;
            var copy = Instantiate(source);
            copy.name = source.name + " interaction outline";
            copy.hideFlags = HideFlags.DontSave;
            Vector3[] vertices = copy.vertices;
            Vector3[] normals = copy.normals;
            if (normals == null || normals.Length != vertices.Length)
            {
                copy.RecalculateNormals();
                normals = copy.normals;
            }
            var sums = new Dictionary<Vector3, Vector3>();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 normal = normals[i];
                if (sums.TryGetValue(vertices[i], out Vector3 sum)) sums[vertices[i]] = sum + normal;
                else sums.Add(vertices[i], normal);
            }
            for (int i = 0; i < normals.Length; i++) normals[i] = sums[vertices[i]].normalized;
            copy.normals = normals;
            return copy;
        }

        static bool CanOutline(Renderer renderer)
            => renderer && renderer.enabled && renderer.gameObject.activeInHierarchy;

        GameObject NewClone(Transform parent, string name)
        {
            var clone = new GameObject(name) { hideFlags = HideFlags.DontSave };
            clone.transform.SetParent(parent, false);
            clone.AddComponent<OutlineCloneMarker>();
            return clone;
        }

        void OnDisable() => SetVisible(null);
        void OnDestroy() => Clear();

        void Clear()
        {
            IsVisible = false;
            activeMaterial = null;
            for (int i = 0; i < outlines.Count; i++)
                if (outlines[i]) { outlines[i].enabled = false; Destroy(outlines[i].gameObject); }
            for (int i = 0; i < outlineMeshes.Count; i++)
                if (outlineMeshes[i]) Destroy(outlineMeshes[i]);
            originals.Clear(); outlines.Clear(); materialSlots.Clear(); outlineMeshes.Clear(); built = false;
        }

        sealed class OutlineCloneMarker : MonoBehaviour { }
    }
}
