using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    [InitializeOnLoad]
    public static class CharacterAnimationValidation
    {
        private const string Key = "FireRescue.CharacterAnimationValidation";
        private const string ScenePath = "Assets/FireRescue/Scenes/ResidentialFire.unity";
        private static bool finished;

        static CharacterAnimationValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary Character Animation Validation").AddComponent<CharacterAnimationDriver>();
            };
        }

        [MenuItem("消防 Prototype/驗證角色骨架與動畫")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }

        private static void OnLog(string text, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Finish(false, text + "\n" + stack);
        }

        public static void Finish(bool success, string details)
        {
            if (finished) return;
            finished = true;
            Application.logMessageReceived -= OnLog;
            SessionState.SetBool(Key, false);
            string report = (success ? "CHARACTER_ANIMATION_PLAYMODE_PASS" : "CHARACTER_ANIMATION_PLAYMODE_FAIL") +
                "\n" + details;
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/character-animation-results.txt", report);
            if (success) Debug.Log(report); else Debug.LogError(report);
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
                else EditorApplication.ExitPlaymode();
            };
        }
    }

    public sealed class CharacterAnimationDriver : MonoBehaviour
    {
        private readonly List<string> results = new List<string>();
        private FireField field;
        private FirefighterMotor player;
        private RescueNpc npc;
        private CharacterLocomotionAnimation playerPresentation;
        private CharacterLocomotionAnimation npcPresentation;
        private Animator playerAnimator;
        private Animator npcAnimator;
        private float started;

        private void Start()
        {
            Time.captureDeltaTime = 1f / 60f;
            started = Time.realtimeSinceStartup;
            field = FindFirstObjectByType<FireField>();
            player = FindFirstObjectByType<FirefighterMotor>();
            npc = FindFirstObjectByType<RescueNpc>();
            if (player)
            {
                LocalPlayerInput input = player.GetComponent<LocalPlayerInput>();
                if (input) input.enabled = false;
            }
            if (field) field.Simulate = false;
            if (npc) npc.Autonomous = false;
            StartCoroutine(Scenario());
        }

        private void OnDestroy() => Time.captureDeltaTime = 0f;

        private void Update()
        {
            if (Time.realtimeSinceStartup - started > 25f)
            {
                StopAllCoroutines();
                enabled = false;
                CharacterAnimationValidation.Finish(false, string.Join("\n", results) + "\nCharacter animation validation timeout.");
            }
        }

        private IEnumerator Scenario()
        {
            yield return null;
            Check(field && player && npc, "Residential scene exposes its real fire field, firefighter and resident.");
            CheckCharacter(player.gameObject, "firefighter", 4f, out playerPresentation, out playerAnimator);
            CheckCharacter(npc.gameObject, "resident", 2.15f, out npcPresentation, out npcAnimator);
            yield return VerifyPlayerLocomotion();
            yield return VerifyNpcEscapeLocomotion();
            CharacterAnimationValidation.Finish(true, string.Join("\n", results));
        }

        private void CheckCharacter(GameObject actor, string label, float referenceSpeed,
            out CharacterLocomotionAnimation presentation, out Animator animator)
        {
            presentation = actor.GetComponent<CharacterLocomotionAnimation>();
            animator = actor.GetComponentInChildren<Animator>(true);
            SkinnedMeshRenderer[] skins = actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            HashSet<Transform> bones = new HashSet<Transform>();
            foreach (SkinnedMeshRenderer skin in skins)
                foreach (Transform bone in skin.bones) if (bone) bones.Add(bone);
            SkinnedMeshRenderer legSkin = skins.FirstOrDefault(skin => skin.name.IndexOf("Boot_L", StringComparison.OrdinalIgnoreCase) >= 0 ||
                skin.name.IndexOf("Shoe_L", StringComparison.OrdinalIgnoreCase) >= 0);
            Check(presentation && animator && legSkin && bones.Count >= 14 && HasRigBones(actor.transform),
                "The " + label + " has a presentation component, skinned leg mesh and the authored Root/Hips/Chest/Head rig bones.");
            SerializedObject presentationData = new SerializedObject(presentation);
            Check(presentationData.FindProperty("actorRoot").objectReferenceValue == actor.transform &&
                Mathf.Abs(presentationData.FindProperty("referenceSpeed").floatValue - referenceSpeed) < .001f,
                "The " + label + " scene persists its actor root and " + referenceSpeed + "m/s animation reference speed.");
            Check(animator.avatar && animator.avatar.isValid && !animator.avatar.isHuman && !animator.applyRootMotion,
                "The " + label + " uses a valid Generic avatar with root motion disabled.");

            string modelPath = AssetDatabase.GetAssetPath(legSkin.sharedMesh);
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            Check(importer && importer.animationType == ModelImporterAnimationType.Generic && importer.importAnimation,
                "The " + label + " model is imported as a Generic animated rig.");

            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
            Check(controller && controller.parameters.Any(parameter => parameter.name == "Speed") &&
                controller.parameters.Any(parameter => parameter.name == "CycleSpeed"),
                "The " + label + " Animator Controller exposes Speed and CycleSpeed parameters.");
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = machine.states.Select(state => state.state).FirstOrDefault(state => state.name == "Idle");
            AnimatorState run = machine.states.Select(state => state.state).FirstOrDefault(state => state.name == "Run");
            AnimationClip idleClip = idle != null ? idle.motion as AnimationClip : null;
            AnimationClip runClip = run != null ? run.motion as AnimationClip : null;
            Check(idleClip && runClip && idleClip.length > 0f && runClip.length > 0f && idleClip.isLooping && runClip.isLooping,
                "The " + label + " has non-empty looping Idle and Run clips.");
            Check(run.speedParameterActive && run.speedParameter == "CycleSpeed",
                "The " + label + " Run state uses CycleSpeed as its playback-speed parameter.");
            Check(idle.transitions.Any(transition => HasSpeedCondition(transition, AnimatorConditionMode.Greater, .06f)) &&
                run.transitions.Any(transition => HasSpeedCondition(transition, AnimatorConditionMode.Less, .04f)),
                "The " + label + " Idle/Run transitions use the 0.06 and 0.04 Speed hysteresis.");

            presentation.Initialize(animator, actor.transform, referenceSpeed);
            animator.Rebind();
            animator.Update(0f);
            VerifyRunClipDeforms(label, animator, legSkin, run, runClip, actor.transform);
        }

        private void VerifyRunClipDeforms(string label, Animator animator, SkinnedMeshRenderer skin,
            AnimatorState run, AnimationClip runClip, Transform actor)
        {
            Mesh loopStart = new Mesh();
            Mesh loopEnd = new Mesh();
            Mesh first = new Mesh();
            Mesh second = new Mesh();
            Vector3 origin = actor.position;
            try
            {
                animator.SetFloat("Speed", 1f); // Keep the automatic Idle transition out of explicit clip-pose sampling.
                animator.SetFloat("CycleSpeed", 1f);
                animator.Play(run.name, 0, 0f);
                animator.Update(0f);
                skin.BakeMesh(loopStart);
                animator.Play(run.name, 0, 0f);
                animator.Update(runClip.length);
                skin.BakeMesh(loopEnd);
                animator.Play(run.name, 0, .15f);
                animator.Update(runClip.length * .1f);
                skin.BakeMesh(first);
                animator.Update(runClip.length * .35f);
                skin.BakeMesh(second);
                Check(first.vertexCount > 0 && second.vertexCount == first.vertexCount && VerticesDiffer(first, second),
                    "The " + label + " Run clip changes the left boot/shoe skinned pose.");
                Check(MaxVertexDistance(loopStart, loopEnd) < .025f && Vector3.Distance(origin, actor.position) < .025f,
                    "The " + label + " Run clip's 0-to-length pose loop and root stay within 0.025m.");
                if (label == "firefighter")
                {
                    var transforms = actor.GetComponentsInChildren<Transform>();
                    var nozzle = transforms.Single(t => t.name == "Nozzle" && t.parent.name == "Chest");
                    var grip = transforms.Single(t => t.name == "Nozzle_GripSocket");
                    var left = transforms.Single(t => t.name == "Hand.L");
                    var right = transforms.Single(t => t.name == "Hand.R");
                    Check(nozzle.parent.name == "Chest", "The real firefighter nozzle mesh follows the animated Chest, not a stationary actor socket.");
                    Check(Vector3.Distance(grip.position, (left.position + right.position) * .5f) < .07f,
                        "The real running firefighter keeps its nozzle grip within 7cm of the two-hand midpoint.");
                }
            }
            finally
            {
                Destroy(loopStart);
                Destroy(loopEnd);
                Destroy(first);
                Destroy(second);
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private IEnumerator VerifyPlayerLocomotion()
        {
            playerPresentation.Initialize(playerAnimator, player.transform, 4f);
            float runDeadline = Time.time + .8f;
            while (Time.time < runDeadline && (!playerPresentation.IsMoving || !IsState(playerAnimator, "Run")))
            {
                player.Move(Vector3.right, Time.deltaTime);
                yield return null;
            }
            Check(playerPresentation.IsMoving && IsState(playerAnimator, "Run"),
                "Actual FirefighterMotor movement drives the firefighter Animator into Run.");

            for (int frame = 0; frame < 16; frame++)
            {
                player.Move(Vector3.zero, Time.deltaTime);
                yield return null;
            }
            yield return new WaitForSeconds(.2f);
            Check(!playerPresentation.IsMoving && IsState(playerAnimator, "Idle"),
                "Stopping actual FirefighterMotor movement returns the firefighter Animator to Idle.");

            Vector3 originalPosition = player.transform.position;
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "Temporary animation blocking wall";
            blocker.layer = 8;
            blocker.transform.position = new Vector3(40.5f, .75f, 0f);
            blocker.transform.localScale = new Vector3(.2f, 1.5f, 3f);
            Teleport(player, new Vector3(40.115f, .05f, 0f));
            playerPresentation.Initialize(playerAnimator, player.transform, 4f);
            Physics.SyncTransforms();
            Vector3 blockedStart = player.transform.position;
            for (int frame = 0; frame < 10; frame++)
            {
                player.Move(Vector3.right, Time.deltaTime);
                yield return null;
            }
            yield return new WaitForSeconds(.15f);
            Check(PlanarDistance(blockedStart, player.transform.position) < .015f && !playerPresentation.IsMoving &&
                IsState(playerAnimator, "Idle"), "A physical blocking wall keeps the actual firefighter stationary and Idle.");
            Destroy(blocker);

            playerPresentation.enabled = false;
            Check(Mathf.Approximately(playerAnimator.GetFloat("Speed"), 0f), "Disabling presentation immediately sets Animator Speed to zero.");
            playerPresentation.enabled = true;
            Teleport(player, originalPosition + Vector3.right * 3f);
            yield return new WaitForSeconds(.2f);
            Check(!playerPresentation.IsMoving && Mathf.Approximately(playerAnimator.GetFloat("Speed"), 0f) && IsState(playerAnimator, "Idle"),
                "Enable and teleport reset sampling to Idle without a spawn-motion spike.");
            Teleport(player, originalPosition);
            playerPresentation.Initialize(playerAnimator, player.transform, 4f);
        }

        private IEnumerator VerifyNpcEscapeLocomotion()
        {
            field.ApplyWater(ResidentialPrototypeBuilder.DoorFire, 100f, .25f);
            field.ApplyWater(ResidentialPrototypeBuilder.SecondFire, 100f, .25f);
            Check(field.BurningCount == 0, "Water clears both authored fire cells before the resident escapes.");
            Check(!npc.IsRescued, "The resident remains available to discover its own route without player interaction.");
            npc.Autonomous = true;

            float runDeadline = Time.time + 1f;
            while (Time.time < runDeadline && (!npcPresentation.IsMoving || !IsState(npcAnimator, "Run")))
                yield return null;
            Check(npcPresentation.IsMoving && IsState(npcAnimator, "Run"),
                "The real autonomous resident route drives its Animator into Run within one second.");

            float escapeDeadline = Time.time + 12f;
            while (Time.time < escapeDeadline && !npc.IsRescued)
                yield return null;
            Check(npc.IsRescued, "The resident reaches the real exit within twelve seconds.");
            npc.gameObject.SetActive(true);
            yield return new WaitForSeconds(.2f);
            Check(Mathf.Approximately(npcAnimator.GetFloat("Speed"), 0f) && IsState(npcAnimator, "Idle"),
                "After rescue disables it, the resident re-enables in a stable Idle animation state.");
        }

        private static bool HasSpeedCondition(AnimatorStateTransition transition, AnimatorConditionMode mode, float threshold)
        {
            return transition.conditions.Any(condition => condition.parameter == "Speed" && condition.mode == mode &&
                Mathf.Abs(condition.threshold - threshold) < .0001f);
        }

        private static bool VerticesDiffer(Mesh first, Mesh second)
        {
            Vector3[] a = first.vertices;
            Vector3[] b = second.vertices;
            int stride = Mathf.Max(1, a.Length / 48);
            for (int index = 0; index < a.Length; index += stride)
                if ((a[index] - b[index]).sqrMagnitude > .00000001f) return true;
            return false;
        }

        private static float MaxVertexDistance(Mesh first, Mesh second)
        {
            Vector3[] a = first.vertices;
            Vector3[] b = second.vertices;
            if (a.Length != b.Length) return float.PositiveInfinity;
            float maximum = 0f;
            for (int index = 0; index < a.Length; index++)
                maximum = Mathf.Max(maximum, Vector3.Distance(a[index], b[index]));
            return maximum;
        }

        private static bool HasRigBones(Transform root)
        {
            bool hasRoot = false, hasHips = false, hasChest = false, hasHead = false;
            foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
            {
                hasRoot |= bone.name == "Root";
                hasHips |= bone.name == "Hips";
                hasChest |= bone.name == "Chest";
                hasHead |= bone.name == "Head";
            }
            return hasRoot && hasHips && hasChest && hasHead;
        }

        private static bool IsState(Animator animator, string stateName)
        {
            return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash(stateName);
        }

        private static void Teleport(FirefighterMotor motor, Vector3 position)
        {
            CharacterController controller = motor.GetComponent<CharacterController>();
            bool enabled = controller.enabled;
            controller.enabled = false;
            motor.transform.position = position;
            controller.enabled = enabled;
            Physics.SyncTransforms();
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            results.Add("PASS: " + message);
        }
    }
}
