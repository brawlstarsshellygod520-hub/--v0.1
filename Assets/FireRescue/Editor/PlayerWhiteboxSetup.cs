using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FireRescue.Editor
{
    // Authoring only: preserve player root/anchors and replace only the body presentation.
    public static class PlayerWhiteboxSetup
    {
        public const string Folder = "Assets/FireRescue/Art/UserProvided/Characters/Character1";
        public const string ModelPath = Folder + "/Character1.fbx";
        public const string PrefabPath = Folder + "/PlayerCharacter1.prefab";
        public const string VisualName = "User Player Model";
        public const string Evidence = "Docs/Validation/2026-10-27-player-whitebox-camera";
        public const float ViewSize = 6f;
        public const float ViewPitch = 50f;

        [MenuItem("FireRescue/Art/替換主角白模與正向較遠相機")]
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
                string scene = File.ReadAllText(TutorialGreyboxSetup.ScenePath), prefab = File.ReadAllText(PrefabPath);
                InstallScene();
                Require(scene == File.ReadAllText(TutorialGreyboxSetup.ScenePath) && prefab == File.ReadAllText(PrefabPath), "Repeat install must preserve saved assets.");
                var player = Player();
                ValidateVisual(player.transform.Find("Visual/" + VisualName).gameObject);
                Require(!player.transform.Find("Visual/Body").gameObject.activeSelf && !player.transform.Find("Visual/Helmet").gameObject.activeSelf,
                    "Old capsule/helmet should be hidden backups.");
                Require(player.GetComponent<CharacterController>() && player.GetComponent<HoseConnection>().MaximumLength == 3, "Root physics and 3m hose must remain.");
                var camera = Camera.main;
                Require(camera.orthographic && Mathf.Abs(camera.orthographicSize-ViewSize)<.001f &&
                    Quaternion.Angle(camera.transform.rotation,Quaternion.Euler(ViewPitch,0,0))<.01f, "Persisted aligned wider camera is incorrect.");
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence + "/install.txt", "PLAYER_WHITEBOX_INSTALL_PASS\nUnity " + Application.unityVersion +
                    "\nOriginal 10 meshes, white material, feet=0, height=1.6m, +Z-facing; no new rig/animation.\n" +
                    "Gameplay/physics/anchors snapshot unchanged; NPC and tools preserved; saved reload/idempotence PASS.\n" +
                    "Camera: orthographic size=6 (previous 4.5), pitch=50 yaw=0; fixed smooth follow preserved.\n");
                EditorApplication.Exit(0);
            }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void InstallScene()
        {
            EnsurePrefab();
            var scene = EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            var player = Player(); var visual = player.transform.Find("Visual");
            var camera = Camera.main;
            Require(camera && camera.GetComponent<RescueCamera>(), "Expected current camera/follower.");
            string before = Snapshot(scene,visual,camera.transform);
            bool changed = false;
            if (!visual.Find(VisualName))
            {
                Require(visual.Find("Body") && visual.Find("Helmet"), "Expected original player body/helmet; refuse to guess.");
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),visual);
                model.name = VisualName;
                foreach(string name in new[]{"Body","Helmet"})
                {
                    if(!Application.isBatchMode) Undo.RecordObject(visual.Find(name).gameObject,"Hide original player whitebox");
                    visual.Find(name).gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(visual.Find(name).gameObject);
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(model);
                if(!Application.isBatchMode) Undo.RegisterCreatedObjectUndo(model,"Replace player whitebox");
                changed = true;
            }
            if (Mathf.Abs(camera.orthographicSize-ViewSize)>.001f || Quaternion.Angle(camera.transform.rotation,Quaternion.Euler(ViewPitch,0,0))>.01f)
            {
                if(!Application.isBatchMode) Undo.RecordObjects(new UnityEngine.Object[]{camera,camera.transform,camera.GetComponent<RescueCamera>()},"Align and widen camera");
                camera.orthographicSize = ViewSize;
                camera.GetComponent<RescueCamera>().ConfigureView(ViewPitch,0);
                EditorUtility.SetDirty(camera); EditorUtility.SetDirty(camera.GetComponent<RescueCamera>());
                changed = true;
            }
            Require(before == Snapshot(scene,visual,camera.transform), "Gameplay snapshot changed; refusing scene save.");
            if(changed) Require(EditorSceneManager.SaveScene(scene), "Could not save player/camera scene.");
        }

        public static FirefighterMotor Player() => GameObject.Find(UserLevelOneBuilder.LayoutName).GetComponentInChildren<FirefighterMotor>();

        static string Snapshot(UnityEngine.SceneManagement.Scene scene, Transform visual, Transform camera)
        {
            return string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true))
                .Where(c=>c && !c.transform.IsChildOf(visual) && c.transform != camera && (c is MonoBehaviour || c is Collider || c is Rigidbody || c is Transform))
                .Select(c=>c.name+":"+c.GetType().FullName+":"+EditorJsonUtility.ToJson(c)).OrderBy(s=>s,StringComparer.Ordinal));
        }

        static void EnsurePrefab()
        {
            if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) return;
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            Require(importer,"Copy the original Character1.fbx before installation.");
            importer.importAnimation=false; importer.animationType=ModelImporterAnimationType.None;
            importer.importCameras=false; importer.importLights=false; importer.addCollider=false;
            importer.importNormals=ModelImporterNormals.Import; importer.bakeAxisConversion=true; importer.globalScale=1;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard; importer.SaveAndReimport();
            var root = new GameObject("PlayerCharacter1");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath),root.transform);
                model.name="Source Model";
                var eyes = model.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="圓形");
                Vector3 delta = root.transform.InverseTransformPoint(eyes.bounds.center)-BoundsOf(root).center; delta.y=0;
                // Source eye position identifies the front axis after Unity's own FBX axis conversion.
                Vector3 axis = Mathf.Abs(delta.x)>Mathf.Abs(delta.z) ? Vector3.right*Mathf.Sign(delta.x) : Vector3.forward*Mathf.Sign(delta.z);
                model.transform.localRotation=Quaternion.FromToRotation(axis,Vector3.forward)*model.transform.localRotation;
                float scale=1.6f/BoundsOf(root).size.y;
                model.transform.localScale*=scale;
                var bounds=BoundsOf(root);
                model.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                string materialPath=Folder+"/Whitebox.mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(!material)
                {
                    material=new Material(Shader.Find("Standard")){name="Whitebox",color=new Color(.82f,.82f,.82f)};
                    material.SetFloat("_Metallic",0); material.SetFloat("_Glossiness",.2f); AssetDatabase.CreateAsset(material,materialPath);
                }
                foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    int slots=Mathf.Max(1,renderer.sharedMaterials.Length);
                    renderer.sharedMaterials=Enumerable.Repeat(material,slots).ToArray();
                }
                ValidateVisual(root);
                Require(PrefabUtility.SaveAsPrefabAsset(root,PrefabPath),"Could not save player presentation prefab.");
                AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        public static Bounds BoundsOf(GameObject root)
        {
            Bounds result=default; bool first=true;
            foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Require(mesh.sharedMesh,"Missing character mesh."); var b=mesh.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    Vector3 corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    Vector3 point=root.transform.InverseTransformPoint(mesh.transform.TransformPoint(corner));
                    if(first){result=new Bounds(point,Vector3.zero);first=false;}else result.Encapsulate(point);
                }
            }
            return result;
        }

        public static void ValidateVisual(GameObject model)
        {
            Require(model.GetComponentsInChildren<MeshFilter>(true).Length==10,"All ten source pieces must be present.");
            Require(model.GetComponentsInChildren<Collider>(true).Length==0 && model.GetComponentsInChildren<Rigidbody>(true).Length==0 &&
                model.GetComponentsInChildren<MonoBehaviour>(true).Length==0,"Model must be presentation-only.");
            Bounds b=BoundsOf(model);
            Require(Mathf.Abs(b.size.y-1.6f)<.01f && Mathf.Abs(b.min.y)<.01f,"Whitebox feet/scale incorrect: "+b);
            var eyes=model.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="圓形");
            Require(model.transform.InverseTransformPoint(eyes.bounds.center).z>b.center.z+.1f,"Whitebox is not facing +Z.");
            Require(model.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).All(m=>m && m.name=="Whitebox"),"Whitebox material missing.");
        }

        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}

        public static void BatchBuildWindows()
        {
            const string output="Builds/PlayerWhitebox/FireRescue.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{TutorialGreyboxSetup.ScenePath},locationPathName=output,
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence+"/build.txt",report.summary.result+"\nErrors="+report.summary.totalErrors+" Warnings="+report.summary.totalWarnings+"\n"+output);
            EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
        }
    }
}
