using System;
using UnityEditor;
using UnityEngine;

namespace FireRescue.Editor
{
    public static class CharacterBatchLaunch
    {
        static double readyAt;
        static Action action;
        public static void ApplyAndCapture() => Queue(() =>
        {
            CharacterArtSetup.Apply();
            CharacterRigPreview.Capture();
            CharacterMeshValidation.Run();
            EditorApplication.Exit(0);
        });
        public static void Animation() => Queue(CharacterAnimationValidation.Run);
        public static void Residential() => Queue(ResidentialPrototypeValidation.Run);
        public static void Mesh() => Queue(() =>
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ResidentialPrototypeBuilder.ScenePath);
            CharacterMeshValidation.Run();
            EditorApplication.Exit(0);
        });
        public static void Baseline() => Queue(() =>
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ResidentialPrototypeBuilder.ScenePath);
            CharacterRigPreview.CaptureBaseline();
            EditorApplication.Exit(0);
        });
        public static void Preview() => Queue(() =>
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ResidentialPrototypeBuilder.ScenePath);
            CharacterRigPreview.Capture();
            EditorApplication.Exit(0);
        });

        static void Queue(Action next)
        {
            action = next;
            readyAt = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update += Begin;
        }
        static void Begin()
        {
            if (EditorApplication.timeSinceStartup < readyAt || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= Begin;
            try { action(); }
            catch (Exception exception) { Debug.LogException(exception); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
