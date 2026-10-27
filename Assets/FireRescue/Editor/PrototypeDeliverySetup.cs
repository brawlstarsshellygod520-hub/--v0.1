using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    public static class PrototypeDeliverySetup
    {
        public const string Evidence=AssetProvenanceOrganizer.Evidence;
        public const string Latest="Builds/Latest/FireRescue.exe";
        public static void Audit(string suffix)
        {
            Directory.CreateDirectory(Evidence);var scene=EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            var player=PlayerWhiteboxSetup.Player();var npc=UserCharacterSetup.CurrentNpc();
            var room=GameObject.Find(UserLevelOneBuilder.LayoutName).transform.Find(HouseholdLayoutSetup.Marker+"/Living Room");
            string report="DELIVERY_AUDIT\nScene="+scene.path+"\nSceneHose="+player.GetComponent<HoseConnection>().MaximumLength+
                "\nPlayerPrefabHose="+AssetDatabase.LoadAssetAtPath<GameObject>(GreyboxPrefabBuilder.PrefabPath("Player")).GetComponent<HoseConnection>().MaximumLength+
                "\nEnabledBuildScenes="+string.Join(",",EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path))+"\n";
            foreach(string name in new[]{"Left armchair","Right armchair"}){var t=room.Find(name);report+=name+" pos="+t.position.ToString("F3")+" yaw="+t.eulerAngles.y+"\n";}
            foreach(var r in npc.transform.Find("Visual/"+CharacterSwapSetup.NpcVisual).GetComponentsInChildren<SkinnedMeshRenderer>())report+="NPC "+r.name+"="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"MISSING"))+"\n";
            // Use explicit old/new paths so the before-audit can run prior to the migration.
            var path=AssetDatabase.GUIDToAssetPath("8dfcab96e9676f04da516855eeff9730");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            report+="ResidentPrefab="+path+" colors="+prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.name).Distinct().Count()+"\n";
            File.WriteAllText(Evidence+"/audit-"+suffix+".txt",report);
        }
        public static void BatchRepairAndValidate()
        {
            try
            {
                ResidentPaletteSetup.EnsureMaterials();ResidentPaletteSetup.TuneReferenceReadability();
                var prefab=PrefabUtility.LoadPrefabContents(CharacterSwapSetup.Prefab(1));
                try{ResidentPaletteSetup.Apply(prefab);ResidentPaletteSetup.Validate(prefab);Require(PrefabUtility.SaveAsPrefabAsset(prefab,CharacterSwapSetup.Prefab(1)),"Could not save reusable colored resident.");}
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
                var playerPrefab=PrefabUtility.LoadPrefabContents(GreyboxPrefabBuilder.PrefabPath("Player"));
                try{var s=new SerializedObject(playerPrefab.GetComponent<HoseConnection>());s.FindProperty("maximumLength").floatValue=5;s.ApplyModifiedPropertiesWithoutUndo();Require(PrefabUtility.SaveAsPrefabAsset(playerPrefab,GreyboxPrefabBuilder.PrefabPath("Player")),"Could not save 5m player prefab.");}
                finally{PrefabUtility.UnloadPrefabContents(playerPrefab);}
                var scene=EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
                var player=PlayerWhiteboxSetup.Player();var hs=new SerializedObject(player.GetComponent<HoseConnection>());hs.FindProperty("maximumLength").floatValue=5;hs.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(hs.targetObject);
                var npc=UserCharacterSetup.CurrentNpc();ResidentPaletteSetup.Apply(npc.transform.Find("Visual/"+CharacterSwapSetup.NpcVisual).gameObject);
                foreach(var r in npc.transform.Find("Visual/"+CharacterSwapSetup.NpcVisual).GetComponentsInChildren<Renderer>())PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                var room=GameObject.Find(UserLevelOneBuilder.LayoutName).transform.Find(HouseholdLayoutSetup.Marker+"/Living Room");
                foreach(var pair in new[]{(name:"Left armchair",yaw:-90f),(name:"Right armchair",yaw:90f)}){var t=room.Find(pair.name);t.localRotation=Quaternion.Euler(0,pair.yaw,0);PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
                ResidentialTuningSetup.ValidateChairs();Require(EditorSceneManager.SaveScene(scene),"Scene save failed.");
                var old=EditorBuildSettings.scenes.Where(s=>s.path!=TutorialGreyboxSetup.ScenePath).Select(s=>new EditorBuildSettingsScene(s.path,false));
                EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(TutorialGreyboxSetup.ScenePath,true)}.Concat(old).ToArray();
                AssetDatabase.SaveAssets();Audit("after");
                var loaded=AssetDatabase.LoadAssetAtPath<GameObject>(CharacterSwapSetup.Prefab(1));ResidentPaletteSetup.Validate(loaded);
                Require(EditorBuildSettings.scenes.First(s=>s.enabled).path==TutorialGreyboxSetup.ScenePath,"Default startup scene still points to old game.");
                File.WriteAllText(Evidence+"/delivery-repair.txt","DELIVERY_REPAIR_PASS\nScene + reusable NPC prefab palette, player prefab 5m, armchairs face inward, default build scene corrected.\n");EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        public static void BatchBuildLatest()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Latest));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=Latest,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText(Evidence+"/build.txt",report.summary.result+"\nErrors="+report.summary.totalErrors+" Warnings="+report.summary.totalWarnings+"\n"+Latest);
            File.WriteAllText("Builds/Latest/BUILD_INFO.txt","炎上消防隊｜本機最新測試版\n2026-10-27 Asset classification / inward armchairs / colored resident / 5m hose\nScene: "+TutorialGreyboxSetup.ScenePath+"\n");
            EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
        }
        public static void BatchAuditFinal()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);Audit("final");
                var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                var components=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).ToArray();
                Require(components.All(c=>c),"Scene contains missing scripts after asset migration.");
                Require(components.OfType<MeshFilter>().All(m=>m.sharedMesh),"Scene contains missing static mesh references.");
                Require(components.OfType<SkinnedMeshRenderer>().All(m=>m.sharedMesh && m.bones.All(b=>b)),"Scene contains missing skin meshes or bones.");
                File.WriteAllText(Evidence+"/references.txt","FINAL_REFERENCE_AUDIT_PASS\nMissing scripts/static meshes/skinned meshes/bones: 0\nREADME metadata imported; current scene and reusable-prefab values re-read.\n");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Require(bool v,string s){if(!v)throw new InvalidOperationException(s);}
    }
}
