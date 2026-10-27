using UnityEngine;

namespace FireRescue
{
    /// <summary>
    /// Optional primitive-only presentation for FireField. It never changes simulation state.
    /// </summary>
    public sealed class FireFieldView : MonoBehaviour
    {
        [SerializeField] private FireField field;
        private GameObject[] firePillars;
        private GameObject[] oilPatches;
        private GameObject[] wetPatches;
        private GameObject[] powderPatches;
        private Material fireMaterial;
        private Material oilMaterial;
        private Material wetMaterial;
        private Material powderMaterial;

        public void Initialize(FireField fireField)
        {
            if (field == fireField && firePillars != null)
                return;

            ClearVisuals();
            field = fireField;
            if (field == null)
                return;

            CreateMaterials();
            var count = field.Width * field.Height;
            firePillars = new GameObject[count];
            oilPatches = new GameObject[count];
            wetPatches = new GameObject[count];
            powderPatches = new GameObject[count];
            for (var z = 0; z < field.Height; z++)
            {
                for (var x = 0; x < field.Width; x++)
                {
                    var index = z * field.Width + x;
                    firePillars[index] = CreatePrimitive("Fire", fireMaterial);
                    oilPatches[index] = CreatePrimitive("Oil", oilMaterial);
                    wetPatches[index] = CreatePrimitive("Wet", wetMaterial);
                }
            }
        }

        private void Awake()
        {
            if (field == null)
                field = GetComponent<FireField>();
            if (field != null)
                Initialize(field);
        }

        private void LateUpdate()
        {
            if (field == null || firePillars == null)
                return;

            for (var z = 0; z < field.Height; z++)
            {
                for (var x = 0; x < field.Width; x++)
                {
                    var index = z * field.Width + x;
                    var state = field.GetCell(x, z);
                    var center = field.CellCenter(x, z);
                    UpdatePatch(oilPatches[index], center, state.Oil, .012f);
                    UpdatePatch(wetPatches[index], center, state.Wetness, .02f);
                    // Most cells never receive powder; allocate only first use, then reuse.
                    if (state.Powder > .01f && !powderPatches[index])
                        powderPatches[index] = CreatePrimitive("Powder", powderMaterial);
                    if (powderPatches[index]) UpdatePatch(powderPatches[index], center, state.Powder, .016f);
                    UpdateFire(firePillars[index], center, state.Intensity);
                }
            }
        }

        private void OnDestroy()
        {
            ClearVisuals();
        }

        private GameObject CreatePrimitive(string name, Material material)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = name;
            visual.transform.SetParent(transform, false);
            var collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }
            visual.GetComponent<Renderer>().sharedMaterial = material;
            visual.SetActive(false);
            return visual;
        }

        private void UpdatePatch(GameObject patch, Vector3 center, float amount, float height)
        {
            var visible = amount > .01f;
            if (patch.activeSelf != visible)
                patch.SetActive(visible);
            if (!visible)
                return;

            patch.transform.position = center + Vector3.up * height;
            patch.transform.localScale = new Vector3(field.CellSize * .86f, height, field.CellSize * .86f);
        }

        private void UpdateFire(GameObject pillar, Vector3 center, float intensity)
        {
            var visible = intensity > .05f;
            if (pillar.activeSelf != visible)
                pillar.SetActive(visible);
            if (!visible)
                return;

            var height = .2f + intensity * .9f;
            pillar.transform.position = center + Vector3.up * (height * .5f);
            pillar.transform.localScale = new Vector3(field.CellSize * .42f, height, field.CellSize * .42f);
        }

        private void CreateMaterials()
        {
            fireMaterial = CreateMaterial(new Color(1f, .24f, .02f), true);
            oilMaterial = CreateMaterial(new Color(.08f, .045f, .015f), false);
            wetMaterial = CreateMaterial(new Color(.05f, .38f, .9f), false);
            powderMaterial = CreateMaterial(new Color(.82f, .78f, .64f), false);
        }

        private static Material CreateMaterial(Color color, bool emission)
        {
            var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            var material = new Material(shader) { color = color };
            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.5f);
            }

            return material;
        }

        private void ClearVisuals()
        {
            DestroyVisuals(firePillars);
            DestroyVisuals(oilPatches);
            DestroyVisuals(wetPatches);
            DestroyVisuals(powderPatches);
            firePillars = null;
            oilPatches = null;
            wetPatches = null;
            powderPatches = null;
            DestroyMaterial(ref fireMaterial);
            DestroyMaterial(ref oilMaterial);
            DestroyMaterial(ref wetMaterial);
            DestroyMaterial(ref powderMaterial);
        }

        private static void DestroyVisuals(GameObject[] visuals)
        {
            if (visuals == null)
                return;
            for (var i = 0; i < visuals.Length; i++)
            {
                if (visuals[i] != null)
                    Destroy(visuals[i]);
            }
        }

        private static void DestroyMaterial(ref Material material)
        {
            if (material == null)
                return;
            Destroy(material);
            material = null;
        }
    }
}
