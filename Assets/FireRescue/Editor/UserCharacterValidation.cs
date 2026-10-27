using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    [InitializeOnLoad]
    public static class UserCharacterValidation
    {
        const string Key = "FireRescue.UserCharacter2Test";
        static bool finished;
        static UserCharacterValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                finished = false;
                Application.logMessageReceived += OnLog;
                new GameObject("Temporary user character tests").AddComponent<UserCharacterTestDriver>();
            };
        }
        public static void BatchRun()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }
        static void OnLog(string text, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception) Finish(false, text + "\n" + trace); }
        public static void Finish(bool passed, string details)
        {
            if (finished) return; finished = true;
            Application.logMessageReceived -= OnLog;
            SessionState.SetBool(Key, false);
            Directory.CreateDirectory(UserCharacterSetup.Evidence);
            File.WriteAllText(UserCharacterSetup.Evidence + "/character-playmode.txt", (passed ? "USER_CHARACTER_PLAYMODE_PASS\n" : "FAIL\n") + details);
            EditorApplication.delayCall += () => EditorApplication.Exit(passed ? 0 : 1);
        }
    }

    public sealed class UserCharacterTestDriver : MonoBehaviour
    {
        readonly List<string> checks = new List<string>();
        float started;
        void Start() { started = Time.realtimeSinceStartup; Time.captureDeltaTime = 1f / 60; StartCoroutine(Scenario()); }
        void Update()
        { if (Time.realtimeSinceStartup - started > 60) UserCharacterValidation.Finish(false, string.Join("\n", checks) + "\nTimeout"); }
        void OnDestroy() { Time.captureDeltaTime = 0; }
        void Check(bool value, string label)
        {
            if (!value) throw new Exception(string.Join("\n", checks) + "\nFAIL " + label);
            checks.Add("PASS " + label);
        }
        IEnumerator Scenario()
        {
            var field = new GameObject("Field").AddComponent<FireField>();
            field.Configure(new Vector3(-1.5f,0,-1), 3, 8, 1);
            field.SetIgnitionSeeds(Array.Empty<Vector2Int>()); field.ResetSimulation(); field.Simulate = false;
            for (int x = 0; x < 3; x++) field.Ignite(field.CellCenter(x,3), 1);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0,-.1f,3); ground.transform.localScale = new Vector3(6,.2f,12);
            var exit = new GameObject("Exit").transform; exit.position = new Vector3(0,0,5);
            var root = new GameObject("Test parent"); root.SetActive(false);
            var actor = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(GreyboxPrefabBuilder.PrefabPath("Resident")), root.transform);
            var npc = actor.GetComponent<RescueNpc>(); npc.Initialize(field,exit); npc.Autonomous = true;
            var visual = actor.transform.Find("Visual"); visual.Find("Model").gameObject.SetActive(false);
            var model = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UserCharacterSetup.PrefabPath),visual);
            var animator = model.GetComponentInChildren<Animator>();
            var presentation = model.AddComponent<CharacterLocomotionAnimation>(); presentation.Initialize(animator,actor.transform,2.15f);
            root.SetActive(true); Physics.SyncTransforms();
            yield return null;
            UserCharacterSetup.ValidateModel(model);
            Check(animator.avatar.isValid && !animator.avatar.isHuman && !animator.applyRootMotion, "Real supplied model has a valid Generic rig without root motion");
            Check(model.GetComponentsInChildren<SkinnedMeshRenderer>().Length == 6, "All six supplied mesh parts are present and skinned");
            Check(model.GetComponentsInChildren<Collider>(true).Length == 0 && actor.GetComponent<CharacterController>(), "New visual owns no collision or NPC authority");
            var colors = model.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Select(m => m.color).Distinct().Count();
            Check(colors >= 6, "Hair, skin, clothing, trim, trousers and facial colors remain separate");
            var clips = animator.runtimeAnimatorController.animationClips;
            Check(clips.Any(c => c.name == "Idle" && c.isLooping) && clips.Any(c => c.name == "Run" && c.isLooping), "Idle and Run are imported looping clips");
            yield return new WaitForSeconds(.35f);
            Check(!npc.HasSafePath && !presentation.IsMoving && animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "NPC waits in Idle while the complete route is burning");
            npc.Autonomous = false; presentation.enabled = false;
            var skin = model.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(s => s.sharedMesh.vertexCount).First();
            var a = new Mesh(); var b = new Mesh(); var start = new Mesh(); var end = new Mesh();
            Vector3 actorStart = actor.transform.position;
            try
            {
                animator.SetFloat("Speed",1); animator.SetFloat("CycleSpeed",1);
                animator.Play("Run",0,.15f); animator.Update(0); skin.BakeMesh(a);
                animator.Play("Run",0,.65f); animator.Update(0); skin.BakeMesh(b);
                Check(MaxDifference(a,b) > .01f, "Run deforms the supplied body vertices, not only a floating object root");
                float duration = clips.Single(c => c.name == "Run").length;
                animator.Play("Run",0,0); animator.Update(0); skin.BakeMesh(start);
                animator.Update(duration); skin.BakeMesh(end);
                Check(MaxDifference(start,end) < .025f, "Run start/end skin poses loop within 2.5cm");
                Check(Vector3.Distance(actorStart,actor.transform.position) < .0001f, "Sampling Run never moves the gameplay root");
                animator.SetFloat("Speed",0);
                animator.Play("Idle",0,.15f); animator.Update(0); skin.BakeMesh(a);
                animator.Play("Idle",0,.65f); animator.Update(0); skin.BakeMesh(b);
                Check(MaxDifference(a,b) > .0005f, "Idle contains subtle body motion");
            }
            finally { Destroy(a); Destroy(b); Destroy(start); Destroy(end); }
            animator.Rebind(); animator.Update(0); presentation.enabled = true;
            field.ApplyWater(new Vector3(0,0,2.5f),100,5);
            npc.Autonomous = true;
            float deadline = Time.time + 1.5f;
            while (Time.time < deadline && !(presentation.IsMoving && animator.GetCurrentAnimatorStateInfo(0).IsName("Run"))) yield return null;
            Check(npc.HasSafePath && presentation.IsMoving && animator.GetCurrentAnimatorStateInfo(0).IsName("Run"), "Clearing the route triggers autonomous NPC movement and Run without interaction");
            npc.Autonomous = false;
            yield return new WaitForSeconds(.35f);
            Check(!presentation.IsMoving && animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "Stopping resolved NPC movement returns to Idle");
            presentation.enabled = false;
            Check(Mathf.Approximately(animator.GetFloat("Speed"),0), "Disabling presentation resets animation speed");
            presentation.enabled = true;
            yield return new WaitForSeconds(.2f);
            Check(!presentation.IsMoving && animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "Re-enabling presentation remains Idle without motion spikes");
            int rescued = 0; npc.Rescued += _ => rescued++;
            model.SetActive(false); npc.Autonomous = true;
            deadline = Time.time + 5;
            while (Time.time < deadline && !npc.IsRescued) yield return null;
            Check(npc.IsRescued && rescued == 1, "Disabling the replacement model does not prevent NPC rescue or duplicate the event");
            UserCharacterValidation.Finish(true, "Checks=" + checks.Count + "\n" + string.Join("\n",checks));
        }
        static float MaxDifference(Mesh a, Mesh b)
        {
            if (a.vertexCount != b.vertexCount || a.vertexCount == 0) return float.PositiveInfinity;
            Vector3[] av = a.vertices, bv = b.vertices; float max = 0;
            for (int i = 0; i < av.Length; i++) max = Mathf.Max(max,Vector3.Distance(av[i],bv[i]));
            return max;
        }
    }
}
