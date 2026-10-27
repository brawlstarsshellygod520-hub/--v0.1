using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    // Updates only the two character visuals; never rebuilds the house or gameplay modules.
    public static class CharacterArtSetup
    {
        const string GeneratedAnimationPath = "Assets/FireRescue/Art/AIGenerated/Animations/";
        const string UserResident2AnimationPath = "Assets/FireRescue/Art/UserProvided/Characters/Character2/Animations/";

        public static void ConfigureImport(ModelImporter importer)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.motionNodeName = "Root";
            importer.optimizeGameObjects = false; // Keep the editable toy skeleton and sockets.
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            // Read source takes rather than retaining an obsolete user-defined clip list.
            importer.clipAnimations = Array.Empty<ModelImporterClipAnimation>();
            importer.SaveAndReimport();
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                if (clip.name.EndsWith("Idle", StringComparison.Ordinal)) clip.name = "Idle";
                else if (clip.name.EndsWith("Run", StringComparison.Ordinal)) clip.name = "Run";
                else throw new InvalidOperationException("Unexpected character animation take: " + clip.name);
                clip.loopTime = true;
                clip.loopPose = true;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
            if (clips.Length != 2) throw new InvalidOperationException("Expected Idle + Run: " + importer.assetPath);
            importer.clipAnimations = clips;
        }

        public static void ConfigureAnimator(GameObject visual, string modelPath, string name)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            var idle = clips.Single(clip => clip.name == "Idle");
            var run = clips.Single(clip => clip.name == "Run");
            string path = (name == "UserResident2" ? UserResident2AnimationPath : GeneratedAnimationPath) + name + ".controller";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.Refresh();
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            if (!controller.parameters.Any(p => p.name == "Speed")) controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            if (!controller.parameters.Any(p => p.name == "CycleSpeed")) controller.AddParameter("CycleSpeed", AnimatorControllerParameterType.Float);
            var parameters = controller.parameters;
            foreach (var parameter in parameters)
                if (parameter.name == "CycleSpeed") parameter.defaultFloat = 1;
            controller.parameters = parameters;
            var machine = controller.layers[0].stateMachine;
            var idleState = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Idle") ?? machine.AddState("Idle", new Vector3(250, 30));
            var runState = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Run") ?? machine.AddState("Run", new Vector3(500, 30));
            idleState.motion = idle;
            runState.motion = run;
            runState.speedParameter = "CycleSpeed";
            runState.speedParameterActive = true;
            foreach (var transition in idleState.transitions) idleState.RemoveTransition(transition);
            foreach (var transition in runState.transitions) runState.RemoveTransition(transition);
            ConfigureTransition(idleState.AddTransition(runState), AnimatorConditionMode.Greater, .06f);
            ConfigureTransition(runState.AddTransition(idleState), AnimatorConditionMode.Less, .04f);
            machine.defaultState = idleState;
            var animator = visual.GetComponentInChildren<Animator>(true);
            if (!animator) throw new InvalidOperationException("Imported character has no Animator: " + name);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                skin.updateWhenOffscreen = true; // Only two small prototypes; avoid stale animated bounds.
            EditorUtility.SetDirty(controller);
        }

        static void ConfigureTransition(AnimatorStateTransition transition, AnimatorConditionMode mode, float threshold)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = .12f;
            transition.AddCondition(mode, threshold, "Speed");
        }

        public static void AttachPresentation(GameObject actor, GameObject visual, float referenceSpeed)
        {
            var animator = visual.GetComponentInChildren<Animator>(true);
            if (!animator || !animator.runtimeAnimatorController)
                throw new InvalidOperationException("Character animation not configured: " + actor.name);
            var presentation = actor.GetComponent<CharacterLocomotionAnimation>();
            if (!presentation) presentation = actor.AddComponent<CharacterLocomotionAnimation>();
            presentation.Initialize(animator, actor.transform, referenceSpeed);
        }

        [MenuItem("消防 Prototype/更新圓潤角色與動畫（保留住宅）")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before updating character assets.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ResidentialArtCatalog.Prepare(new[] { "Firefighter", "Resident" });
            var scene = EditorSceneManager.OpenScene(ResidentialPrototypeBuilder.ScenePath);
            string gameplayBefore = GameplaySnapshot(scene);
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<FirefighterMotor>()) ReplaceVisual(root, "Firefighter", 4);
                else if (root.GetComponent<RescueNpc>()) ReplaceVisual(root, "Resident", 2.15f);
            }
            string gameplayAfter = GameplaySnapshot(scene);
            if (gameplayBefore != gameplayAfter)
                throw new InvalidOperationException("Character replacement changed serialized gameplay data; scene has not been saved.");
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/character-gameplay-preservation.txt", "CHARACTER_GAMEPLAY_PRESERVED\n" + gameplayAfter);
            Debug.Log("CHARACTER_SETUP_PASS: updated two visuals and Idle/Run animation; house/gameplay preserved.");
        }

        static string GameplaySnapshot(UnityEngine.SceneManagement.Scene scene)
        {
            return string.Join("\n", scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Component>(true))
                .Where(component => component && (component is MonoBehaviour || component is Collider) && !(component is CharacterLocomotionAnimation))
                .Select(component => component.gameObject.name + "/" + component.GetType().FullName + ":" + EditorJsonUtility.ToJson(component))
                .OrderBy(line => line, StringComparer.Ordinal));
        }

        static void ReplaceVisual(GameObject actor, string asset, float referenceSpeed)
        {
            var old = actor.transform.Find(asset);
            if (!old) throw new InvalidOperationException("Expected character visual missing: " + actor.name + "/" + asset);
            // Preserve the existing nozzle instance when replacing its animated parent.
            if (asset == "Firefighter")
            {
                var nozzle = FindNozzleVisual(actor);
                if (nozzle) nozzle.SetParent(actor.transform, true);
            }
            UnityEngine.Object.DestroyImmediate(old.gameObject);
            var visual = ResidentialArtCatalog.Visual(asset, actor.transform);
            AttachPresentation(actor, visual, referenceSpeed);
            if (asset == "Firefighter") MountNozzlePresentation(actor, visual);
        }

        public static void MountNozzlePresentation(GameObject actor, GameObject visual)
        {
            var nozzle = FindNozzleVisual(actor);
            if (!nozzle) return;
            var animator = visual.GetComponentInChildren<Animator>();
            animator.Rebind();
            animator.Update(0);
            var bones = visual.GetComponentsInChildren<Transform>(true);
            var chest = bones.Single(t => t.name == "Chest");
            var left = bones.Single(t => t.name == "Hand.L");
            var right = bones.Single(t => t.name == "Hand.R");
            var grip = nozzle.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Nozzle_GripSocket");
            // Only the mesh follows the upper body. SprayEmitter remains authoritative
            // for the unchanged gameplay ray, range and fluid reactions.
            nozzle.position += (left.position + right.position) * .5f - grip.position;
            nozzle.SetParent(chest, true);
        }

        static Transform FindNozzleVisual(GameObject actor)
        {
            // FBX also has an inner node named Nozzle. Select the prefab instance,
            // either at the original actor root or already mounted to Chest.
            var original = actor.transform.Find("Nozzle");
            return original ? original : actor.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(t => t.name == "Nozzle" && t.parent && t.parent.name == "Chest");
        }
    }
}
