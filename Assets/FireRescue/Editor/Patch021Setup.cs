using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    public static class Patch021Setup
    {
        public const string Evidence="Docs/Validation/2026-10-28-v021";
        public static void BatchApply()
        {
            try
            {
                Directory.CreateDirectory(Evidence);
                var scene=EditorSceneManager.OpenScene(FirstReview1027Setup.ScenePath);
                var boundary=GameObject.Find("First Review 1027/Prototype Courtyard Boundary");
                if(!boundary||boundary.GetComponentsInChildren<Collider>().Length!=3)throw new InvalidOperationException("Courtyard collision baseline mismatch");
                foreach(var renderer in boundary.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
                var source=UnityEngine.Object.FindObjectsByType<FluidSource>().Single(s=>s.Element==ElementKind.Water);
                var profile=source.Profile;var settings=profile.Settings;
                settings.MuzzleSpeed=10;settings.MinimumSpeedFraction=.14f;settings.MainStreamSpeedFraction=.7f;settings.PacketsPerSecond=60;profile.Configure(settings);EditorUtility.SetDirty(profile);
                var effects=new SerializedObject(UnityEngine.Object.FindAnyObjectByType<FirstRescueEffects>());
                effects.FindProperty("waterVisualScale").floatValue=1.65f;effects.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Patch scene save failed");
                AssetDatabase.SaveAssets();
                File.WriteAllText(Evidence+"/install.txt","PATCH_021_INSTALLED\nBoundary renderers off; all three colliders retained.\nWater far tier 7 -> 10m/s; main speed fraction .7 / minimum .14 preserves 1.4,2.8,4.2,5.6m/s near/mid tiers. 40 -> 60 packets/s, visual scale 1.65; gravity, flow rate, reaction authority, powder and 10m hose unchanged.\nE=context pickup/drop/deliver, otherwise connect/disconnect. Picking up stows and disconnects the hose. Right-click staged connection retained; F no longer read.\nFailure presents small R hint, not a button.\n");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
