using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    public static class ResidentialTuningSetup
    {
        public const string Evidence="Docs/Validation/2026-10-27-resident-color-5m";
        public const string Marker="Resident palette and 5m hose v1";
        public const float HoseLength=5f;
        [MenuItem("FireRescue/Greybox/對調單椅、居民配色與5m管長")]
        public static void InstallMenu(){if(EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;Install();}
        public static void BatchInstallAndValidate()
        {
            try
            {
                Install();string once=File.ReadAllText(TutorialGreyboxSetup.ScenePath);Install();
                Require(once==File.ReadAllText(TutorialGreyboxSetup.ScenePath),"Repeat migration changed scene.");
                EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
                var player=PlayerWhiteboxSetup.Player();var npc=UserCharacterSetup.CurrentNpc();
                Require(player.GetComponent<HoseConnection>().MaximumLength==HoseLength,"5m hose did not persist.");
                ResidentPaletteSetup.Validate(npc.transform.Find("Visual/"+CharacterSwapSetup.NpcVisual).gameObject);
                ValidateChairs();
                Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/install.txt","RESIDENT_TUNING_INSTALL_PASS\nUnity "+Application.unityVersion+"\nArmchairs exchanged with rotations preserved, spatial names corrected; Role1 palette only; 5m scene override.\nSave/reload/idempotence PASS. Core snapshot preserved except authored hose length.\n");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Install()
        {
            ResidentPaletteSetup.EnsureMaterials();
            var scene=EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            var layout=GameObject.Find(UserLevelOneBuilder.LayoutName).transform;
            if(layout.Find("Authoring Markers/"+Marker))return;
            var player=layout.GetComponentInChildren<FirefighterMotor>();var hose=player.GetComponent<HoseConnection>();
            var length=new SerializedObject(hose);length.FindProperty("maximumLength").floatValue=HoseLength;length.ApplyModifiedProperties();Record(hose);
            string before=CoreSnapshot(layout);
            var room=layout.Find(HouseholdLayoutSetup.Marker+"/Living Room");
            var left=room.Find("Left armchair");var right=room.Find("Right armchair");Require(left && right,"Expected armchairs missing.");
            if(!Application.isBatchMode)Undo.RecordObjects(new UnityEngine.Object[]{left,right,left.gameObject,right.gameObject},"Exchange armchairs");
            Vector3 a=left.localPosition;left.localPosition=right.localPosition;right.localPosition=a;
            left.name="Right armchair";right.name="Left armchair";
            Record(left);Record(right);Record(left.gameObject);Record(right.gameObject);
            var npc=layout.GetComponentInChildren<RescueNpc>();var visual=npc.transform.Find("Visual/"+CharacterSwapSetup.NpcVisual);
            Require(visual,"Current resident Role1 presentation missing.");
            ResidentPaletteSetup.Apply(visual.gameObject);
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))Record(renderer);
            Require(before==CoreSnapshot(layout),"Unrelated gameplay, anchors, rig transform or camera changed; refusing save.");
            var marker=new GameObject(Marker);marker.transform.SetParent(layout.Find("Authoring Markers"),false);
            if(!Application.isBatchMode)Undo.RegisterCreatedObjectUndo(marker,"Record residential tuning");
            ValidateChairs();Require(EditorSceneManager.SaveScene(scene),"Scene save failed.");
        }
        public static void ValidateChairs()
        {
            var layout=GameObject.Find(UserLevelOneBuilder.LayoutName).transform;var room=layout.Find(HouseholdLayoutSetup.Marker+"/Living Room");
            var left=room.Find("Left armchair");var right=room.Find("Right armchair");
            Require(Vector3.Distance(left.localPosition,new Vector3(1.65f,0,-2.5f))<.001f && Vector3.Distance(right.localPosition,new Vector3(5.65f,0,-2.5f))<.001f,"Armchair anchors changed unexpectedly.");
            Require(Quaternion.Angle(left.localRotation,Quaternion.Euler(0,-90,0))<.01f && Quaternion.Angle(right.localRotation,Quaternion.Euler(0,90,0))<.01f,"Armchairs must face inward to the coffee table.");
        }
        static string CoreSnapshot(Transform layout)
        {
            // No geometry, skeleton, actor transform or gameplay field is changed by a palette assignment.
            return string.Join("\n",layout.GetComponentsInChildren<Component>(true)
                .Where(c=>c && ((c is MonoBehaviour && !(c is SourceHighlightView)) || c is Collider || c is Rigidbody || c is Animator || (c is Transform t && (c.GetComponent<FirefighterMotor>() || c.GetComponent<RescueNpc>() || c.name=="Anchors" || t.parent && t.parent.name=="Anchors"))))
                .Select(c=>c.GetType().FullName+":"+EditorJsonUtility.ToJson(c)).OrderBy(s=>s,StringComparer.Ordinal))+
                "\nCamera:"+EditorJsonUtility.ToJson(Camera.main)+EditorJsonUtility.ToJson(Camera.main.transform)+EditorJsonUtility.ToJson(Camera.main.GetComponent<RescueCamera>());
        }
        public static void BatchBuildWindows()
        {
            const string path="Builds/ResidentColor5m/FireRescue.exe";Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{TutorialGreyboxSetup.ScenePath},target=BuildTarget.StandaloneWindows64,locationPathName=path,options=BuildOptions.Development});
            File.WriteAllText(Evidence+"/build.txt",report.summary.result+"\nErrors="+report.summary.totalErrors+" Warnings="+report.summary.totalWarnings+"\n"+path);EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
        }
        static void Record(UnityEngine.Object obj){EditorUtility.SetDirty(obj);if(PrefabUtility.IsPartOfPrefabInstance(obj))PrefabUtility.RecordPrefabInstancePropertyModifications(obj);}
        static void Require(bool v,string s){if(!v)throw new InvalidOperationException(s);}
    }
}
