using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    // One explicit, GUID-preserving migration. No model is regenerated or relabelled as original AI art.
    public static class AssetProvenanceOrganizer
    {
        public const string Evidence="Docs/Validation/2026-10-27-asset-classification";
        static readonly (string from,string to)[] Moves={
            ("Assets/FireRescue/Art/PlayerWhitebox","Assets/FireRescue/Art/UserProvided/Characters/Character1"),
            ("Assets/FireRescue/Art/UserCharacters2","Assets/FireRescue/Art/UserProvided/Characters/Character2"),
            ("Assets/FireRescue/Art/RoleSwapRig","Assets/FireRescue/Art/UserProvided/Characters/Rigged"),
            ("Assets/FireRescue/Art/UserFireAxe","Assets/FireRescue/Art/UserProvided/Tools/FireAxe"),
            ("Assets/FireRescue/Art/UserNozzle","Assets/FireRescue/Art/UserProvided/Tools/Nozzle"),
            ("Assets/FireRescue/Greybox/LevelOne","Assets/FireRescue/Art/UserProvided/LevelOne"),
            ("Assets/FireRescue/Art/Animations/UserResident2.controller","Assets/FireRescue/Art/UserProvided/Characters/Character2/Animations/UserResident2.controller"),
            ("Assets/FireRescue/Art/Models","Assets/FireRescue/Art/AIGenerated/Models"),
            ("Assets/FireRescue/Art/Prefabs","Assets/FireRescue/Art/AIGenerated/Prefabs"),
            ("Assets/FireRescue/Art/Materials","Assets/FireRescue/Art/AIGenerated/Materials"),
            ("Assets/FireRescue/Art/Animations","Assets/FireRescue/Art/AIGenerated/Animations"),
            ("Assets/FireRescue/Greybox/Household","Assets/FireRescue/Art/AIGenerated/Household")
        };
        [Serializable] public sealed class GuidRecord{public string guid;public string before;public string after;}
        [Serializable] sealed class GuidReport{public GuidRecord[] assets;}
        public static void BatchOrganize()
        {
            try
            {
                Directory.CreateDirectory(Evidence);
                // Capture the state that explains the reported delivery gap before making changes.
                if(!File.Exists(Evidence+"/audit-before.txt"))PrototypeDeliverySetup.Audit("before");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                string reportPath=Evidence+"/guid-migration.json";
                var records=File.Exists(reportPath)?JsonUtility.FromJson<GuidReport>(File.ReadAllText(reportPath)).assets:Inventory();
                if(!File.Exists(reportPath))File.WriteAllText(reportPath,JsonUtility.ToJson(new GuidReport{assets=records},true));
                foreach(var pair in Moves)
                {
                    bool source=AssetDatabase.IsValidFolder(pair.from)||File.Exists(pair.from);
                    bool destination=AssetDatabase.IsValidFolder(pair.to)||File.Exists(pair.to);
                    if(!source){Require(destination,"Neither source nor destination exists: "+pair.from);continue;}
                    Require(!destination,"Refuse to merge or overwrite existing destination: "+pair.to);
                    EnsureFolder(Path.GetDirectoryName(pair.to).Replace('\\','/'));
                    string guid=AssetDatabase.AssetPathToGUID(pair.from);
                    string error=AssetDatabase.MoveAsset(pair.from,pair.to);
                    Require(string.IsNullOrEmpty(error),"Move failed: "+error);
                    Require(AssetDatabase.AssetPathToGUID(pair.to)==guid,"Moved folder/model GUID changed: "+pair.to);
                }
                AssetDatabase.SaveAssets();AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach(var item in records){item.after=AssetDatabase.GUIDToAssetPath(item.guid);Require(!string.IsNullOrEmpty(item.after),"Lost original asset GUID "+item.guid);}
                File.WriteAllText(reportPath,JsonUtility.ToJson(new GuidReport{assets=records},true));
                File.WriteAllText(Evidence+"/classification.txt","ASSET_CLASSIFICATION_PASS\nPreserved "+records.Length+" existing GUIDs.\nUser source and derived rigs grouped under UserProvided; procedural art under AIGenerated.\nShared shader and gameplay prefabs stay separate from model provenance. No models regenerated.\n");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static GuidRecord[] Inventory()
        {
            var files=new[]{"Assets/FireRescue/Art","Assets/FireRescue/Greybox/LevelOne","Assets/FireRescue/Greybox/Household"}
                .Where(Directory.Exists).SelectMany(p=>Directory.GetFiles(p,"*.meta",SearchOption.AllDirectories)).ToList();
            foreach(string p in new[]{"Assets/FireRescue/Greybox/LevelOne.meta","Assets/FireRescue/Greybox/Household.meta"})if(File.Exists(p))files.Add(p);
            return files.Select(p=>{string guid=Regex.Match(File.ReadAllText(p),@"(?m)^guid: (\w+)").Groups[1].Value;return new GuidRecord{guid=guid,before=p.Substring(0,p.Length-5).Replace('\\','/')};})
                .Where(r=>!string.IsNullOrEmpty(r.guid)).GroupBy(r=>r.guid).Select(g=>g.First()).ToArray();
        }
        public static void EnsureFolder(string path)
        {
            Require(path.StartsWith("Assets/FireRescue/",StringComparison.Ordinal),"Destination outside approved art workspace.");
            if(AssetDatabase.IsValidFolder(path))return;
            var parent=Path.GetDirectoryName(path).Replace('\\','/');if(!AssetDatabase.IsValidFolder(parent))EnsureFolder(parent);
            Require(!string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent,Path.GetFileName(path))),"Cannot create "+path);
        }
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}
