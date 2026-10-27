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
    public static class CharacterSwapValidation
    {
        const string Key="FireRescue.CharacterSwapTest";
        static bool finished;
        static CharacterSwapValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false)) return;
                finished=false; Application.logMessageReceived+=OnLog;
                new GameObject("Temporary character swap checks").AddComponent<CharacterSwapDriver>();
            };
        }
        public static void BatchRun()
        {
            EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            SessionState.SetBool(Key,true); EditorApplication.EnterPlaymode();
        }
        static void OnLog(string t,string s,LogType type)
        {if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) Finish(false,t+"\n"+s);}
        public static void Finish(bool pass,string details)
        {
            if(finished)return; finished=true; SessionState.SetBool(Key,false); Application.logMessageReceived-=OnLog;
            Directory.CreateDirectory(CharacterSwapSetup.Evidence);
            File.WriteAllText(CharacterSwapSetup.Evidence+"/characters-facing-playmode.txt",(pass?"CHARACTERS_FACING_PASS\n":"FAIL\n")+details);
            EditorApplication.delayCall+=()=>EditorApplication.Exit(pass?0:1);
        }
    }

    public sealed class CharacterSwapDriver:MonoBehaviour
    {
        readonly List<string> checks=new List<string>(); float started;
        void Start(){started=Time.realtimeSinceStartup; Time.captureDeltaTime=1f/60; StartCoroutine(Scenario());}
        void OnDestroy(){Time.captureDeltaTime=0;}
        void Update(){if(Time.realtimeSinceStartup-started>100) CharacterSwapValidation.Finish(false,string.Join("\n",checks)+"\nTimeout");}
        void Check(bool pass,string label)
        {if(!pass)throw new InvalidOperationException(string.Join("\n",checks)+"\nFAIL "+label); checks.Add("PASS "+label);}
        IEnumerator Scenario()
        {
            yield return null;
            var player=PlayerWhiteboxSetup.Player(); var npc=UserCharacterSetup.CurrentNpc();
            player.GetComponent<LocalPlayerInput>().ReadDevices=false; npc.Autonomous=false;
            var playerModel=player.transform.Find("Visual/"+CharacterSwapSetup.PlayerVisual);
            var npcModel=npc.transform.Find("Visual/"+CharacterSwapSetup.NpcVisual);
            Check(playerModel && npcModel && playerModel.gameObject.activeInHierarchy && npcModel.gameObject.activeInHierarchy,"Current scene roles are Player=Role2 and Resident=Role1");
            Check(!player.transform.Find("Visual/"+PlayerWhiteboxSetup.VisualName).gameObject.activeSelf && !npc.transform.Find("Visual/"+UserCharacterSetup.VisualName).gameObject.activeSelf,"Previous visuals are hidden intact backups");
            foreach(var pair in new[]{(actor:player.transform,model:playerModel),(actor:npc.transform,model:npcModel)})
            {
                var presenter=pair.model.GetComponent<CharacterLocomotionAnimation>();
                Check(presenter && new SerializedObject(presenter).FindProperty("actorRoot").objectReferenceValue==pair.actor,"Presentation samples the unchanged gameplay actor: "+pair.actor.name);
            }
            Check(player.GetComponent<HoseConnection>().MaximumLength==3 && player.GetComponent<CharacterController>() && npc.GetComponent<CharacterController>(),"3m hose and both gameplay colliders preserved");
            Check(Camera.main.orthographic && Mathf.Abs(Camera.main.orthographicSize-6)<.001f && Quaternion.Angle(Camera.main.transform.rotation,Quaternion.Euler(50,0,0))<.01f,"Aligned wider fixed camera preserved");
            yield return TestRig(1,1.5f); yield return TestRig(2,1.6f);
            yield return CharacterFacingValidation.Run(checks);
            CharacterSwapValidation.Finish(true,"Checks="+checks.Count+"\n"+string.Join("\n",checks));
        }

        IEnumerator TestRig(int role,float height)
        {
            var actor=new GameObject("Animation fixture Role"+role); actor.transform.position=new Vector3(100+role*10,0,0);
            var visual=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CharacterSwapSetup.Prefab(role)),actor.transform);
            var animator=visual.GetComponentInChildren<Animator>();
            var presenter=visual.AddComponent<CharacterLocomotionAnimation>(); presenter.Initialize(animator,actor.transform,4);
            yield return null;
            CharacterSwapSetup.Validate(visual,role,height);
            string prefix="Role"+role+": ";
            Check(animator.avatar.isValid && !animator.avatar.isHuman && !animator.applyRootMotion,prefix+"valid Generic bones, root motion disabled");
            Check(visual.GetComponentsInChildren<SkinnedMeshRenderer>().All(s=>s.bones.All(b=>b)),prefix+"all original mesh parts have valid skin bones");
            var clips=animator.runtimeAnimatorController.animationClips;
            Check(clips.Length==2 && clips.All(c=>c.isLooping) && clips.Any(c=>c.name=="Idle") && clips.Any(c=>c.name=="Walk"),prefix+"Idle and Walk loop clips imported");
            yield return new WaitForSeconds(.3f);
            Check(!presenter.IsMoving && animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),prefix+"stationary actor starts Idle");
            presenter.enabled=false;
            var skins=visual.GetComponentsInChildren<SkinnedMeshRenderer>();
            Vector3 rootBefore=actor.transform.position;
            var a=new Mesh();var b=new Mesh();
            try
            {
                float walkDifference=0,idleDifference=0,seam=0;
                foreach(var skin in skins)
                {
                    animator.SetFloat("Speed",1);animator.SetFloat("CycleSpeed",1);
                    animator.Play("Walk",0,.15f);animator.Update(0);skin.BakeMesh(a,true);
                    animator.Play("Walk",0,.65f);animator.Update(0);skin.BakeMesh(b,true);
                    walkDifference=Mathf.Max(walkDifference,Difference(a,b));
                    animator.Play("Walk",0,0);animator.Update(0);skin.BakeMesh(a,true);
                    animator.Update(clips.Single(c=>c.name=="Walk").length);skin.BakeMesh(b,true);
                    seam=Mathf.Max(seam,Difference(a,b));
                    animator.SetFloat("Speed",0);
                    animator.Play("Idle",0,.15f);animator.Update(0);skin.BakeMesh(a,true);
                    animator.Play("Idle",0,.65f);animator.Update(0);skin.BakeMesh(b,true);
                    idleDifference=Mathf.Max(idleDifference,Difference(a,b));
                }
                Check(walkDifference>.01f,prefix+"Walk deforms supplied skin vertices");
                Check(idleDifference>.0005f,prefix+"Idle contains subtle skin motion");
                Check(seam<.025f,prefix+"Walk skin seam under 2.5cm");
                Check(Vector3.Distance(rootBefore,actor.transform.position)<.0001f,prefix+"animation never moves actor root");
            }
            finally{Destroy(a);Destroy(b);}
            animator.Rebind();animator.Update(0);presenter.enabled=true;
            for(int i=0;i<40;i++){actor.transform.position+=Vector3.forward*(2f/60);yield return null;}
            Check(presenter.IsMoving && animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"),prefix+"resolved actor displacement enters Walk");
            yield return new WaitForSeconds(.4f);
            Check(!presenter.IsMoving && animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),prefix+"stopping returns to Idle");
            presenter.enabled=false;
            Check(animator.GetFloat("Speed")==0,prefix+"disable resets animation speed");
            presenter.enabled=true;yield return new WaitForSeconds(.2f);
            Check(!presenter.IsMoving && animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),prefix+"re-enable has no displacement spike");
            Destroy(actor);yield return null;
        }
        static float Difference(Mesh a,Mesh b)
        {
            if(a.vertexCount!=b.vertexCount || a.vertexCount==0)return float.PositiveInfinity;
            var av=a.vertices;var bv=b.vertices;float max=0;
            for(int i=0;i<av.Length;i++)max=Mathf.Max(max,Vector3.Distance(av[i],bv[i]));
            return max;
        }
    }
}
