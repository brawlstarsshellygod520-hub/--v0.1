using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    public static class HoseFluidPhysicsBuilder
    {
        public const string Evidence = "Docs/Validation/2026-10-27-hose-fluid-physics";

        [MenuItem("FireRescue/Physics/啟用住宅管線牆阻（保留3m）")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            InstallScene();
        }

        public static void BatchInstallAndValidate()
        {
            try
            {
                InstallScene(); string first=File.ReadAllText(TutorialGreyboxSetup.ScenePath);
                InstallScene();
                Require(first==File.ReadAllText(TutorialGreyboxSetup.ScenePath),"Repeated physics wiring changed the scene.");
                var layout=GameObject.Find(UserLevelOneBuilder.LayoutName);
                var hose=layout.GetComponentInChildren<HoseConnection>();
                var data=new SerializedObject(hose);
                Require(hose.HasCollisionGeometry && data.FindProperty("wallOcclusion").objectReferenceValue==layout.GetComponent<SprayWallOcclusion>() &&
                    data.FindProperty("hoseInletAnchor").objectReferenceValue==hose.transform.Find("Anchors/Hose Inlet"),"Saved physical hose wiring is missing.");
                Require(hose.MaximumLength==3,"Do not relax the 3m hose length.");
                PlayerWhiteboxSetup.ValidateVisual(hose.transform.Find("Visual/"+PlayerWhiteboxSetup.VisualName).gameObject);
                Require(Mathf.Abs(Camera.main.orthographicSize-PlayerWhiteboxSetup.ViewSize)<.001f &&
                    Quaternion.Angle(Camera.main.transform.rotation,Quaternion.Euler(PlayerWhiteboxSetup.ViewPitch,0,0))<.01f,"Do not change the requested camera.");
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence+"/install.txt","HOSE_FLUID_PHYSICS_INSTALL_PASS\nUnity "+Application.unityVersion+
                    "\nCurrent hose wall/inlet references and known interior window hook migrated; 3m, whitebox, camera, sources and other gameplay wiring preserved.\nSaved reload and repeated installation PASS.\n");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }

        static void InstallScene()
        {
            var scene=EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            var layout=GameObject.Find(UserLevelOneBuilder.LayoutName);
            Require(layout,"Current residential layout is missing.");
            var hose=layout.GetComponentInChildren<HoseConnection>();
            var geometry=layout.GetComponent<SprayWallOcclusion>();
            var inlet=hose.transform.Find("Anchors/Hose Inlet");
            Require(geometry && inlet && !inlet.IsChildOf(hose.transform.Find("Visual")),"Use authored gameplay walls and independent inlet anchor.");
            var window=layout.GetComponentInChildren<HoseWindow>();
            var route=window ? new SerializedObject(window).FindProperty("routePoint").objectReferenceValue as Transform : null;
            // The original hook was on the OUTSIDE frame. The source-to-hook chord crossed
            // the inner wall 1.34m sideways from the opening. Hook at the measured interior rim.
            // Migrate only the known untouched authored position; preserve custom window setups.
            bool moveRoute=route && Vector3.Distance(window.transform.position,new Vector3(-5.2f,0,3.6f))<.01f &&
                Vector3.Distance(route.localPosition,new Vector3(0,1.15f,0))<.001f;
            var data=new SerializedObject(hose);
            if(!moveRoute && data.FindProperty("wallOcclusion").objectReferenceValue==geometry && data.FindProperty("hoseInletAnchor").objectReferenceValue==inlet)return;
            string before=Snapshot(scene,hose,moveRoute?route:null);
            float length=data.FindProperty("maximumLength").floatValue;
            float reach=data.FindProperty("connectDistance").floatValue;
            var interaction=data.FindProperty("interactionOrigin").objectReferenceValue;
            if(!Application.isBatchMode)Undo.RecordObject(hose,"Bind physical hose geometry");
            hose.ConfigurePhysics(geometry,inlet);
            EditorUtility.SetDirty(hose); PrefabUtility.RecordPrefabInstancePropertyModifications(hose);
            if(moveRoute)
            {
                if(!Application.isBatchMode)Undo.RecordObject(route,"Align hose hook to interior window rim");
                route.localPosition=new Vector3(.4f,1.15f,0);
                EditorUtility.SetDirty(route); PrefabUtility.RecordPrefabInstancePropertyModifications(route);
            }
            data.Update();
            Require(before==Snapshot(scene,hose,moveRoute?route:null) && data.FindProperty("maximumLength").floatValue==length &&
                data.FindProperty("connectDistance").floatValue==reach && data.FindProperty("interactionOrigin").objectReferenceValue==interaction,
                "Physics installation changed unrelated authored gameplay; scene not saved.");
            Require(EditorSceneManager.SaveScene(scene),"Could not save physical hose wiring.");
        }

        static string Snapshot(UnityEngine.SceneManagement.Scene scene,HoseConnection hose,Transform migratedRoute=null)
        {
            return string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true))
                .Where(c=>c && c!=hose && c!=migratedRoute && (c is MonoBehaviour || c is Collider || c is Rigidbody || c is Transform))
                .Select(c=>c.name+":"+c.GetType().FullName+":"+EditorJsonUtility.ToJson(c)).OrderBy(s=>s,StringComparer.Ordinal));
        }
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}

        public static void BatchProbeWindow()
        {
            try
            {
                EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
                var layout=GameObject.Find(UserLevelOneBuilder.LayoutName);
                var player=layout.GetComponentInChildren<SprayEmitter>();
                var source=layout.GetComponentsInChildren<FluidSource>().Single(s=>s.Element==ElementKind.Water);
                var window=layout.GetComponentInChildren<HoseWindow>();
                var geometry=layout.GetComponent<SprayWallOcclusion>();
                player.transform.position=new Vector3(-5.04f,.02f,2.73f);
                player.transform.rotation=Quaternion.LookRotation(new Vector3(-5.2f,0,4.8f)-new Vector3(-5.04f,0,2.73f));
                Physics.SyncTransforms();
                string report="WINDOW_PIPE_PROBE\nPort="+source.Port.ToString("F4")+" inlet="+player.HoseInletPosition.ToString("F4")+
                    " root="+window.transform.position.ToString("F4")+" route="+window.RoutePosition.ToString("F4")+"\n";
                foreach(float offset in new[]{0f,.2f,.35f,.4f,.5f})
                {
                    Vector3 route=window.RoutePosition+Vector3.back*offset;
                    bool a=geometry.TryBlock(source.Port,route,out var first,out var firstPoint,.0475f);
                    bool b=geometry.TryBlock(route,player.HoseInletPosition,out var second,out var secondPoint,.0475f);
                    report+="Interior offset="+offset+" prefixBlocked="+a+" wall="+(first?first.name:"none")+
                        " point="+firstPoint.ToString("F4")+" local="+window.transform.InverseTransformPoint(firstPoint).ToString("F4")+
                        " suffixBlocked="+b+" wall="+(second?second.name:"none")+" point="+secondPoint.ToString("F4")+"\n";
                }
                Directory.CreateDirectory(Evidence); File.WriteAllText(Evidence+"/window-pipe-probe.txt",report);
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }

        public static void BatchBuildWindows()
        {
            const string output="Builds/HoseFluidPhysics/FireRescue.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{TutorialGreyboxSetup.ScenePath},
                target=BuildTarget.StandaloneWindows64,locationPathName=output,options=BuildOptions.Development});
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence+"/build.txt",report.summary.result+"\nErrors="+report.summary.totalErrors+" Warnings="+report.summary.totalWarnings+"\n"+output);
            EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
        }
    }
}
