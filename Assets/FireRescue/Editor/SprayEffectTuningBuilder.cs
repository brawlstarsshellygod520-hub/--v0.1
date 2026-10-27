using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FireRescue.Editor
{
    public static class SprayEffectTuningBuilder
    {
        public const string Evidence = "Docs/Validation/2026-10-26-spray-effect-tuning";
        public static void BatchPrepareAndValidate()
        {
            try
            {
                foreach (var element in new[] { ElementKind.Water, ElementKind.Oil, ElementKind.DryPowder })
                {
                    string path = DirectionalSprayBuilder.Folder + "/" + element + ".asset";
                    var profile = AssetDatabase.LoadAssetAtPath<SprayProfile>(path);
                    if (!profile) throw new Exception("Missing source profile: " + path);
                    string beforeSettings = JsonUtility.ToJson(profile.Settings);
                    string beforeEffects = JsonUtility.ToJson(profile.Effects);
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    // Serialize new fields explicitly, without resetting existing tuning.
                    EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);
                    string once = File.ReadAllText(path);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    profile = AssetDatabase.LoadAssetAtPath<SprayProfile>(path);
                    if (beforeSettings != JsonUtility.ToJson(profile.Settings) || beforeEffects != JsonUtility.ToJson(profile.Effects) ||
                        guid != AssetDatabase.AssetPathToGUID(path)) throw new Exception("Profile changed during save/reload: " + path);
                    EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);
                    if (once != File.ReadAllText(path)) throw new Exception("Profile re-save was not idempotent: " + path);
                }
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence + "/prepare.txt", "SPRAY_EFFECT_PREPARE_PASS\nUnity " + Application.unityVersion +
                    "\nThree profiles: preserve trajectory, effect tuning and GUID; save/reload and idempotence PASS. No scene changes.\n");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void BatchBuildWindows()
        {
            const string output = "Builds/SprayEffectTuning/FireRescue.exe";
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
