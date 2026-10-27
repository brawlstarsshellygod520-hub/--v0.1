using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FireRescue.Editor
{
    // One supplied resident model, not a replacement for the firefighter or gameplay prefab.
    public static class UserCharacterSetup
    {
        public const string Folder = "Assets/FireRescue/Art/UserProvided/Characters/Character2";
        public const string ModelPath = Folder + "/Resident2.fbx";
        public const string PrefabPath = Folder + "/Resident2.prefab";
        public const string Evidence = "Docs/Validation/2026-10-26-user-characters";
        public const string VisualName = "User Resident 2";
        static readonly Dictionary<string, Color> Palette = new Dictionary<string, Color>
        {
            { "U2_Hair", new Color(.24f,.115f,.065f) },
            { "U2_Shirt", new Color(.16f,.56f,.49f) },
            { "U2_Trim", new Color(.94f,.85f,.61f) },
            { "U2_Skin", new Color(.94f,.70f,.49f) },
            { "U2_Pants", new Color(.14f,.20f,.28f) },
            { "U2_Dark", new Color(.085f,.10f,.12f) },
            { "U2_Eye", new Color(.045f,.028f,.022f) }
        };

        [MenuItem("FireRescue/Art/接入使用者居民白模（僅首次）")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            InstallScene();
        }

        public static void BatchInstallAndValidate()
        {
            try
            {
                InstallScene();
                string sceneOnce = File.ReadAllText(TutorialGreyboxSetup.ScenePath);
                string prefabOnce = File.ReadAllText(PrefabPath);
                InstallScene();
                Require(sceneOnce == File.ReadAllText(TutorialGreyboxSetup.ScenePath) &&
                    prefabOnce == File.ReadAllText(PrefabPath), "Repeated installation overwrote authored assets.");
                var npc = CurrentNpc();
                var model = npc.transform.Find("Visual/" + VisualName);
                ValidateModel(model.gameObject);
                Require(!npc.transform.Find("Visual/Model").gameObject.activeSelf, "Greybox backup must be hidden, not deleted.");
                var presentation = model.GetComponent<CharacterLocomotionAnimation>();
                Require(presentation && new SerializedObject(presentation).FindProperty("actorRoot").objectReferenceValue == npc.transform,
                    "Locomotion presentation must sample the unchanged NPC root.");
                Require(!new SerializedObject(npc).FindProperty("tintBodyByState").boolValue, "NPC state tint would overwrite authored colors.");
                Require(npc.GetComponent<CharacterController>() && !model.GetComponentInChildren<Collider>(true), "Physics must remain on NPC root.");
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence + "/install.txt", "USER_CHARACTER_INSTALL_PASS\nUnity " + Application.unityVersion +
                    "\nGeneric rig / Idle+Run / 7 material palette / 1.5m feet alignment / +Z facing / visual-only boundary PASS\n" +
                    "Gameplay snapshot unchanged; saved scene reload; repeated install does not overwrite assets PASS\n" +
                    "Supplied FBX contains ONE resident. Firefighter and all gameplay prefabs untouched.\n");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void InstallScene()
        {
            EnsurePrefab();
            var scene = EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            var npc = CurrentNpc();
            Transform slot = npc.transform.Find("Visual");
            if (slot.Find(VisualName)) return;
            Require(slot.Find("Model"), "Existing resident greybox not found; refuse to guess.");
            string before = GameplaySnapshot(scene, slot);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), slot);
            model.name = VisualName;
            var presentation = model.AddComponent<CharacterLocomotionAnimation>();
            presentation.Initialize(model.GetComponentInChildren<Animator>(), npc.transform, 2.15f);
            slot.Find("Model").gameObject.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(slot.Find("Model").gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(model);
            ValidateModel(model);
            Require(before == GameplaySnapshot(scene, slot), "Gameplay/physics/anchors changed; refusing scene save.");
            if (!Application.isBatchMode) Undo.RegisterCreatedObjectUndo(model, "Insert user resident visual");
            Require(EditorSceneManager.SaveScene(scene), "Scene save failed.");
        }

        public static RescueNpc CurrentNpc()
        {
            var layout = GameObject.Find(UserLevelOneBuilder.LayoutName);
            Require(layout, "Current residential layout is missing or inactive.");
            return layout.GetComponentsInChildren<RescueNpc>(true).Single();
        }

        static string GameplaySnapshot(UnityEngine.SceneManagement.Scene scene, Transform visual)
        {
            return string.Join("\n", scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true))
                .Where(c => c && !c.transform.IsChildOf(visual) && (c is MonoBehaviour || c is Collider || c is Rigidbody || c is Transform))
                .Select(c => c.name + ":" + c.GetType().FullName + ":" + EditorJsonUtility.ToJson(c)).OrderBy(s => s, StringComparer.Ordinal));
        }

        static void EnsurePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) return;
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            Require(importer, "Export the supplied character first: " + ModelPath);
            importer.globalScale = 1;
            importer.bakeAxisConversion = true;
            importer.importAnimation = true;
            importer.importCameras = false; importer.importLights = false; importer.addCollider = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            CharacterArtSetup.ConfigureImport(importer);
            importer.SaveAndReimport();
            Directory.CreateDirectory(Folder + "/Materials"); AssetDatabase.Refresh();
            var root = new GameObject("Resident2");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), root.transform);
                model.name = "Model";
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        string key = materials[i] ? materials[i].name.Split('.')[0] : "";
                        Require(Palette.ContainsKey(key), "Unknown material: " + key);
                        string path = Folder + "/Materials/" + key + ".mat";
                        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (!material)
                        {
                            material = new Material(Shader.Find("Standard")) { name = key, color = Palette[key] };
                            material.SetFloat("_Metallic", 0); material.SetFloat("_Glossiness", .22f);
                            AssetDatabase.CreateAsset(material, path);
                        }
                        materials[i] = material;
                    }
                    renderer.sharedMaterials = materials;
                }
                var transforms = model.GetComponentsInChildren<Transform>(true);
                Transform front = transforms.Single(t => t.name == "FaceForward"), head = transforms.Single(t => t.name == "HeadCenter");
                if (front.position.z < head.position.z) model.transform.localRotation = Quaternion.Euler(0,180,0) * model.transform.localRotation;
                CharacterArtSetup.ConfigureAnimator(root, ModelPath, "UserResident2");
                ValidateModel(root);
                Require(PrefabUtility.SaveAsPrefabAsset(root, PrefabPath), "Prefab save failed.");
                AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        public static void ValidateModel(GameObject root)
        {
            Require(root.GetComponentsInChildren<Collider>(true).Length == 0 && root.GetComponentsInChildren<Rigidbody>(true).Length == 0,
                "Visual must not contain physics.");
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                Require(component is CharacterLocomotionAnimation, "Missing script or gameplay component in visual.");
            var animator = root.GetComponentInChildren<Animator>(true);
            Require(animator && animator.avatar && animator.avatar.isValid && !animator.applyRootMotion && animator.runtimeAnimatorController,
                "Missing Generic rig/controller or root motion enabled.");
            var skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Require(skins.Length == 6 && skins.All(s => s.sharedMesh && s.bones.Length > 0 && s.bones.All(b => b)), "Expected all six original pieces skinned.");
            Require(skins.SelectMany(s => s.sharedMaterials).All(m => m && AssetDatabase.GetAssetPath(m).StartsWith(Folder + "/Materials/", StringComparison.Ordinal)),
                "Missing external color material.");
            animator.Rebind(); animator.Update(0);
            Bounds bounds = LocalBounds(root);
            Require(Mathf.Abs(bounds.size.y - 1.5f) < .04f && Mathf.Abs(bounds.min.y) < .04f, "Model must be 1.5m, feet aligned: " + bounds);
            var all = root.GetComponentsInChildren<Transform>(true);
            Vector3 forward = root.transform.InverseTransformPoint(all.Single(t => t.name == "FaceForward").position);
            Vector3 center = root.transform.InverseTransformPoint(all.Single(t => t.name == "HeadCenter").position);
            Require(forward.z > center.z + .05f, "Model must face gameplay +Z.");
        }

        public static Bounds LocalBounds(GameObject root)
        {
            var mesh = new Mesh(); Bounds result = default; bool first = true;
            try
            {
                foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    // Unity 6000.5 defaults useScale=false, which already includes the
                    // renderer scale. Compensate before TransformPoint applies it once.
                    skin.BakeMesh(mesh, true);
                    foreach (Vector3 vertex in mesh.vertices)
                    {
                        Vector3 point = root.transform.InverseTransformPoint(skin.transform.TransformPoint(vertex));
                        if (first) { result = new Bounds(point, Vector3.zero); first = false; } else result.Encapsulate(point);
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
            return result;
        }

        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        public static void BatchBuildWindows()
        {
            const string output = "Builds/UserCharacters/FireRescue.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { TutorialGreyboxSetup.ScenePath },
                locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence + "/build.txt", report.summary.result + "\nErrors=" + report.summary.totalErrors +
                " Warnings=" + report.summary.totalWarnings + "\n" + output);
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
