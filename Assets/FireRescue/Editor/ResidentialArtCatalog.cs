using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FireRescue.Editor
{
    // Authoring-only import/prefab setup. Gameplay components never load model assets.
    public static class ResidentialArtCatalog
    {
        public const string ModelPath = "Assets/FireRescue/Art/Models/";
        public const string PrefabPath = "Assets/FireRescue/Art/Prefabs/";
        public static readonly string[] Models = { "Firefighter", "Resident", "Toilet", "SaladOil",
            "Nozzle", "Sofa", "CoffeeTable", "Bed", "KitchenCounter", "Sink", "Shower", "Block" };
        static readonly Dictionary<string, Color> Palette = new Dictionary<string, Color>
        {
            {"Skin", new Color(.91f,.65f,.42f)}, {"Uniform", new Color(.9f,.19f,.08f)},
            {"Reflector", new Color(1,.78f,.22f)}, {"Helmet", new Color(1,.68f,.08f)},
            {"Charcoal", new Color(.09f,.105f,.12f)}, {"Hair", new Color(.17f,.075f,.035f)},
            {"Eye", new Color(.045f,.025f,.02f)}, {"CivilianShirt", new Color(.11f,.49f,.46f)},
            {"CivilianPants", new Color(.7f,.59f,.41f)}, {"White", new Color(.91f,.91f,.86f)},
            {"Water", new Color(.10f,.59f,.88f)}, {"Oil", new Color(1,.64f,.08f)},
            {"Label", new Color(.2f,.45f,.12f)}, {"Plastic", new Color(1,.88f,.5f)},
            {"Metal", new Color(.43f,.48f,.51f)}, {"Wood", new Color(.67f,.40f,.19f)},
            {"Sofa", new Color(.86f,.32f,.2f)}, {"Blanket", new Color(.36f,.64f,.57f)},
            {"Pillow", new Color(.95f,.90f,.77f)}, {"Cream", new Color(.81f,.77f,.65f)}
        };

        public static void Prepare(string[] selectedModels = null)
        {
            Directory.CreateDirectory(PrefabPath);
            Directory.CreateDirectory("Assets/FireRescue/Art/Materials");
            AssetDatabase.Refresh();
            var report = new List<string>();
            foreach (string name in selectedModels ?? Models)
            {
                string path = ModelPath + name + ".fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (!importer) throw new InvalidOperationException("Missing model: " + path);
                importer.globalScale = 1;
                bool character = name == "Firefighter" || name == "Resident";
                importer.importAnimation = character;
                if (character) CharacterArtSetup.ConfigureImport(importer);
                importer.addCollider = false;
                importer.importNormals = ModelImporterNormals.Import;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.SaveAndReimport();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var instance = UnityEngine.Object.Instantiate(model);
                instance.name = name;
                instance.transform.position = Vector3.zero;
                try
                {
                    foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                    {
                        var materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            string key = materials[i] ? materials[i].name : "";
                            int prefix = key.IndexOf("FR_", StringComparison.Ordinal);
                            if (prefix >= 0) key = key.Substring(prefix + 3);
                            int suffix = key.IndexOf('.');
                            if (suffix >= 0) key = key.Substring(0, suffix);
                            if (!Palette.TryGetValue(key, out Color color))
                                throw new InvalidOperationException("Unknown imported material " + key + " in " + name);
                            materials[i] = Material(key, color);
                        }
                        renderer.sharedMaterials = materials;
                    }
                    // Imported Blender forward can be -Z. Gameplay Face/Nozzle use +Z.
                    string frontName = name == "Firefighter" ? "Firefighter_Eye_L" :
                        name == "Resident" ? "Resident_Eye_L" : name == "Nozzle" ? "Nozzle_Tip" : null;
                    string bodyName = name == "Firefighter" ? "Firefighter_Head" :
                        name == "Resident" ? "Resident_Head" : "Nozzle_GripSocket";
                    if (frontName != null)
                    {
                        Transform front = null, body = null;
                        foreach (Transform marker in instance.GetComponentsInChildren<Transform>(true))
                        {
                            if (marker.name == frontName) front = marker;
                            if (marker.name == bodyName) body = marker;
                        }
                        if (!front || !body) throw new InvalidOperationException("Missing facing markers in " + name);
                        if (front.position.z < body.position.z)
                            instance.transform.rotation = Quaternion.Euler(0,180,0) * instance.transform.rotation;
                    }
                    if (character) CharacterArtSetup.ConfigureAnimator(instance, path, name);
                    Bounds bounds = BoundsOf(instance);
                    if (bounds.size.y < .01f || bounds.size.y > 3)
                        throw new InvalidOperationException("Unexpected imported orientation/scale: " + name + " " + bounds);
                    PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath + name + ".prefab");
                    int triangles = 0;
                    foreach (MeshFilter mesh in instance.GetComponentsInChildren<MeshFilter>())
                        for (int i = 0; i < mesh.sharedMesh.subMeshCount; i++)
                            triangles += (int)mesh.sharedMesh.GetIndexCount(i) / 3;
                    foreach (SkinnedMeshRenderer mesh in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                        for (int i = 0; i < mesh.sharedMesh.subMeshCount; i++)
                            triangles += (int)mesh.sharedMesh.GetIndexCount(i) / 3;
                    report.Add(name + " | triangles=" + triangles + " | bounds=" + bounds);
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllLines(selectedModels == null ? "Logs/residential-art-import.txt" : "Logs/character-art-import.txt", report);
        }

        public static Material Material(string name, Color color)
        {
            string path = "Assets/FireRescue/Art/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat, path); }
            mat.color = color;
            mat.SetFloat("_Glossiness", .16f);
            return mat;
        }

        public static GameObject Visual(string name, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath + name + ".prefab");
            if (!prefab) throw new InvalidOperationException("Missing art prefab " + name);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            visual.transform.SetParent(parent, false);
            return visual;
        }

        public static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("No mesh renderers in " + root.name);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
