using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FireRescue.Editor
{
    /// <summary>
    /// Presentation-only palette for the original Role1 mesh pieces.  It deliberately
    /// assigns whole renderer slots: the imported mesh, rig, clips and transforms stay untouched.
    /// </summary>
    public static class ResidentPaletteSetup
    {
        public const string Folder = "Assets/FireRescue/Art/UserProvided/Characters/Rigged/ResidentPalette";

        const string Hair = "R1_HairOrange";
        const string Skin = "R1_Skin";
        const string Shirt = "R1_ShirtPaleOrange";
        const string Trim = "R1_TrimLightYellow";
        const string Eye = "R1_EyeBlack";
        const string Brow = "R1_BrowOrange";

        static readonly Dictionary<string, string> MaterialByOriginalMeshName = new Dictionary<string, string>
        {
            // The Role1 source is deliberately kept as separate, named primitive meshes.
            { "圓形", Eye },             // paired eyes
            { "圓形.001", Brow },        // paired brows
            { "球體", Hair },            // rear/tail accent
            { "立方體", Skin },          // inseparable head, hands and legs body mesh
            { "立方體.001", Hair },      // separate long side-hair
            { "立方體.002", Shirt },     // torso tunic
            { "立方體.003", Trim },      // upper chest trim
            { "立方體.004", Trim },      // lower chest trim
            { "立方體.005", Hair },      // head hair/ear accent
            { "立方體.006", Hair },      // head hair/ear accent
        };

        static readonly Dictionary<string, Color> ColorByMaterialName = new Dictionary<string, Color>
        {
            { Hair, Hex("FF9358") },
            { Skin, Hex("FFD0B0") },
            { Shirt, Hex("FFA665") },
            { Trim, Hex("FFE88F") },
            { Eye, Hex("201614") },
            { Brow, Hex("A84C2A") },
        };

        [MenuItem("FireRescue/Art/Resident Role1/Create Palette Materials")]
        public static void EnsureMaterials()
        {
            EnsureFolder();
            foreach (var entry in ColorByMaterialName)
            {
                var path = MaterialPath(entry.Key);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null) continue; // Preserve later artist edits; only seed missing materials.
                if (material == null)
                {
                    var shader = Shader.Find("Standard");
                    if (shader == null) throw new InvalidOperationException("The built-in Standard shader is unavailable.");
                    material = new Material(shader) { name = entry.Key };
                    AssetDatabase.CreateAsset(material, path);
                }

                // Soft, non-metal presentation defaults.  Do not create a material instance.
                material.color = entry.Value;
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_Glossiness", 0.16f);
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }

        // Explicit reference-style pass, not part of EnsureMaterials: preserve future artist edits
        // unless this prototype's delivery repair is deliberately invoked again.
        public static void TuneReferenceReadability()
        {
            EnsureMaterials();
            foreach(var entry in ColorByMaterialName)
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(entry.Key));
                Require(material && material.shader.name=="Standard","Expected prototype Standard palette material: "+entry.Key);
                float lift=entry.Key==Eye?0f:entry.Key==Skin?.32f:entry.Key==Hair?.28f:.26f;
                if(lift>0)material.EnableKeyword("_EMISSION");else material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor",entry.Value*lift);
                material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Assigns external shared materials by the original Role1 renderer names only.</summary>
        public static void Apply(GameObject visual)
        {
            Require(visual != null, "Role1 visual is required.");
            EnsureMaterials();

            var renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Require(renderers.Length == MaterialByOriginalMeshName.Count,
                "Expected the ten original Role1 skinned mesh renderers, found " + renderers.Length + ".");

            var seen = new HashSet<string>();
            foreach (var renderer in renderers)
            {
                string meshName = renderer.name;
                Require(MaterialByOriginalMeshName.TryGetValue(meshName, out var materialName),
                    "Unrecognised Role1 mesh renderer: " + meshName);
                Require(seen.Add(meshName), "Duplicate Role1 mesh renderer: " + meshName);
                Require(renderer.sharedMesh != null, "Role1 renderer has no shared mesh: " + meshName);

                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(materialName));
                Require(material != null, "Missing palette material: " + materialName);
                int slotCount = Mathf.Max(1, renderer.sharedMesh.subMeshCount);
                var slots = new Material[slotCount];
                for (int i = 0; i < slots.Length; i++) slots[i] = material;
                renderer.sharedMaterials = slots;
                EditorUtility.SetDirty(renderer);
            }

            Require(seen.Count == MaterialByOriginalMeshName.Count, "A required Role1 mesh was not found.");
        }

        /// <summary>Read-only postcondition check for an already-applied Role1 visual.</summary>
        public static void Validate(GameObject visual)
        {
            Require(visual != null, "Role1 visual is required.");
            var renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Require(renderers.Length == MaterialByOriginalMeshName.Count,
                "Expected the ten original Role1 skinned mesh renderers, found " + renderers.Length + ".");

            var seen = new HashSet<string>();
            foreach (var renderer in renderers)
            {
                string meshName = renderer.name;
                Require(MaterialByOriginalMeshName.TryGetValue(meshName, out var materialName),
                    "Unrecognised Role1 mesh renderer: " + meshName);
                Require(seen.Add(meshName), "Duplicate Role1 mesh renderer: " + meshName);

                var expected = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(materialName));
                Require(expected != null, "Missing palette material: " + materialName);
                Require(renderer.sharedMesh != null, "Role1 renderer has no shared mesh: " + meshName);
                Require(renderer.sharedMaterials.Length == Mathf.Max(1, renderer.sharedMesh.subMeshCount),
                    "Unexpected material slot count on " + meshName + ".");
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    Require(renderer.sharedMaterials[i] == expected,
                        "Incorrect palette material on " + meshName + " slot " + i + ".");
            }

            Require(seen.Count == MaterialByOriginalMeshName.Count, "A required Role1 mesh was not found.");
        }

        static void EnsureFolder()
        {
            const string artFolder = "Assets/FireRescue/Art";
            const string rigFolder = CharacterSwapSetup.Folder;
            if (!AssetDatabase.IsValidFolder(artFolder)) throw new InvalidOperationException("Missing art folder: " + artFolder);
            if (!AssetDatabase.IsValidFolder(rigFolder)) throw new InvalidOperationException("Missing role rig folder: " + rigFolder);
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(rigFolder, "ResidentPalette");
        }

        static string MaterialPath(string materialName) => Folder + "/" + materialName + ".mat";

        static Color Hex(string html)
        {
            if (!ColorUtility.TryParseHtmlString("#" + html, out var color))
                throw new InvalidOperationException("Invalid palette colour: " + html);
            return color;
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
