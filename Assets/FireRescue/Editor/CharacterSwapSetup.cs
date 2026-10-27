using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    // Current tutorial only. Imported models/animation are presentation, never gameplay authority.
    public static class CharacterSwapSetup
    {
        public const string Folder = "Assets/FireRescue/Art/UserProvided/Characters/Rigged";
        public const string Evidence = "Docs/Validation/2026-10-27-character-swap-animation";
        public const string PlayerVisual = "Swap Player Role2";
        public const string NpcVisual = "Swap Resident Role1";
        public static string Model(int role) => Folder + "/Models/Role" + role + ".fbx";
        public static string Prefab(int role) => Folder + "/Role" + role + ".prefab";

        [MenuItem("FireRescue/Art/交換雙角色並接入基本動畫")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            InstallScene();
        }

        public static void BatchInstallAndValidate()
        {
            try
            {
                InstallScene();
                string once = File.ReadAllText(TutorialGreyboxSetup.ScenePath);
                InstallScene();
                Require(once == File.ReadAllText(TutorialGreyboxSetup.ScenePath), "Repeat installation changed saved scene.");
                EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
                var player = PlayerWhiteboxSetup.Player(); var npc = UserCharacterSetup.CurrentNpc();
                Validate(player.transform.Find("Visual/" + PlayerVisual).gameObject, 2, 1.6f);
                Validate(npc.transform.Find("Visual/" + NpcVisual).gameObject, 1, 1.5f);
                Require(!player.transform.Find("Visual/" + PlayerWhiteboxSetup.VisualName).gameObject.activeSelf &&
                    !npc.transform.Find("Visual/" + UserCharacterSetup.VisualName).gameObject.activeSelf, "Prior models must remain hidden backups.");
                Require(player.GetComponent<HoseConnection>().MaximumLength == 3, "3m hose changed.");
                Require(new SerializedObject(player.GetComponent<LocalPlayerInput>()).FindProperty("faceMovementWhenIdle").boolValue, "New facing policy not persisted.");
                Directory.CreateDirectory(Evidence);
                File.WriteAllText(Evidence + "/install.txt", "CHARACTER_SWAP_INSTALL_PASS\n" +
                    "Unity " + Application.unityVersion + "\nPlayer=Role2/1.6m; NPC=Role1/1.5m; Generic Idle/Walk; +Z front; feet Y0.\n" +
                    "Saved reload and idempotence PASS; gameplay/anchors/camera snapshot unchanged except explicit input policy.\n");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void InstallScene()
        {
            EnsurePrefab(1,1.5f); EnsurePrefab(2,1.6f);
            var scene = EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            var player = PlayerWhiteboxSetup.Player(); var npc = UserCharacterSetup.CurrentNpc();
            var pSlot = player.transform.Find("Visual"); var nSlot = npc.transform.Find("Visual");
            Require(pSlot && nSlot, "Expected Visual slots missing.");
            var input = new SerializedObject(player.GetComponent<LocalPlayerInput>());
            var facing = input.FindProperty("faceMovementWhenIdle");
            Require(facing != null, "Facing policy script not installed.");
            bool changed = !facing.boolValue;
            facing.boolValue = true; input.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(input.targetObject);
            // Snapshot after the one intentional gameplay setting; all other data must stay byte-equivalent.
            string before = Snapshot(scene, pSlot, nSlot);
            changed |= Attach(player.transform,pSlot,2,PlayerVisual,PlayerWhiteboxSetup.VisualName,4);
            changed |= Attach(npc.transform,nSlot,1,NpcVisual,UserCharacterSetup.VisualName,2.15f);
            Require(before == Snapshot(scene,pSlot,nSlot), "Non-presentation serialized data changed; refuse save.");
            if (changed) Require(EditorSceneManager.SaveScene(scene), "Scene save failed.");
        }

        static bool Attach(Transform actor, Transform slot, int role, string name, string previous, float speed)
        {
            if (slot.Find(name)) return false;
            Require(slot.Find(previous), "Previous visual missing: " + previous);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(role)),slot);
            instance.name = name;
            instance.AddComponent<CharacterLocomotionAnimation>().Initialize(instance.GetComponentInChildren<Animator>(),actor,speed);
            if (!Application.isBatchMode) { Undo.RegisterCreatedObjectUndo(instance,"Swap character presentation"); Undo.RecordObject(slot.Find(previous).gameObject,"Hide prior character"); }
            slot.Find(previous).gameObject.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(slot.Find(previous).gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            return true;
        }

        static string Snapshot(UnityEngine.SceneManagement.Scene scene, Transform a, Transform b)
        {
            return string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true))
                .Where(c=>c && !c.transform.IsChildOf(a) && !c.transform.IsChildOf(b) && (c is MonoBehaviour || c is Collider || c is Rigidbody || c is Transform || c is Camera))
                .Select(c=>c.name+":"+c.GetType().FullName+":"+EditorJsonUtility.ToJson(c)).OrderBy(s=>s,StringComparer.Ordinal));
        }

        static void EnsurePrefab(int role, float height)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Prefab(role))) return;
            var importer = AssetImporter.GetAtPath(Model(role)) as ModelImporter;
            Require(importer,"Missing derived rig " + Model(role));
            importer.globalScale=1; importer.bakeAxisConversion=true;
            importer.importAnimation=true; importer.importCameras=false; importer.importLights=false; importer.addCollider=false;
            importer.importNormals=ModelImporterNormals.Import; importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.animationType=ModelImporterAnimationType.Generic; importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.motionNodeName="Root"; importer.optimizeGameObjects=false; importer.animationCompression=ModelImporterAnimationCompression.Off;
            importer.clipAnimations=Array.Empty<ModelImporterClipAnimation>(); importer.SaveAndReimport();
            var takes=importer.defaultClipAnimations;
            Require(takes.Length==2,"Expected Idle and Walk takes.");
            foreach(var clip in takes)
            {
                clip.name=clip.name.EndsWith("Idle",StringComparison.Ordinal)?"Idle":clip.name.EndsWith("Walk",StringComparison.Ordinal)?"Walk":throw new InvalidOperationException("Unexpected take "+clip.name);
                clip.loopTime=true; clip.loopPose=true; clip.lockRootRotation=true; clip.lockRootHeightY=true; clip.lockRootPositionXZ=true;
                clip.keepOriginalOrientation=true; clip.keepOriginalPositionY=true; clip.keepOriginalPositionXZ=true;
            }
            importer.clipAnimations=takes; importer.SaveAndReimport();
            var root=new GameObject("Role"+role);
            try
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model(role)),root.transform);
                model.name="Rig";
                foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    var materials=renderer.sharedMaterials;
                    for(int i=0;i<materials.Length;i++)
                    {
                        string path=role==1?PlayerWhiteboxSetup.Folder+"/Whitebox.mat":UserCharacterSetup.Folder+"/Materials/"+materials[i].name.Split('.')[0]+".mat";
                        materials[i]=AssetDatabase.LoadAssetAtPath<Material>(path);
                        Require(materials[i],"Missing external material "+path);
                    }
                    renderer.sharedMaterials=materials;
                }
                var all=model.GetComponentsInChildren<Transform>(true);
                Vector3 front=all.Single(t=>t.name=="FaceForward").position-all.Single(t=>t.name=="HeadCenter").position; front.y=0;
                model.transform.localRotation=Quaternion.FromToRotation(front.normalized,Vector3.forward)*model.transform.localRotation;
                ConfigureAnimator(root,role);
                Validate(root,role,height);
                Require(PrefabUtility.SaveAsPrefabAsset(root,Prefab(role)),"Could not save presentation prefab.");
                AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void ConfigureAnimator(GameObject root,int role)
        {
            var clips=AssetDatabase.LoadAllAssetsAtPath(Model(role)).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__",StringComparison.Ordinal)).ToArray();
            var controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/Role"+role+".controller");
            controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            controller.AddParameter("CycleSpeed",AnimatorControllerParameterType.Float);
            var parameters=controller.parameters; parameters.Single(p=>p.name=="CycleSpeed").defaultFloat=1; controller.parameters=parameters;
            var machine=controller.layers[0].stateMachine;
            var idle=machine.AddState("Idle",new Vector3(230,30)); idle.motion=clips.Single(c=>c.name=="Idle");
            var walk=machine.AddState("Walk",new Vector3(480,30)); walk.motion=clips.Single(c=>c.name=="Walk"); walk.speedParameter="CycleSpeed"; walk.speedParameterActive=true;
            var enter=idle.AddTransition(walk); enter.hasExitTime=false; enter.hasFixedDuration=true; enter.duration=.12f; enter.AddCondition(AnimatorConditionMode.Greater,.06f,"Speed");
            var exit=walk.AddTransition(idle); exit.hasExitTime=false; exit.hasFixedDuration=true; exit.duration=.12f; exit.AddCondition(AnimatorConditionMode.Less,.04f,"Speed");
            machine.defaultState=idle;
            var animator=root.GetComponentInChildren<Animator>(); animator.runtimeAnimatorController=controller; animator.applyRootMotion=false; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.updateWhenOffscreen=true;
            EditorUtility.SetDirty(controller);
        }

        public static void Validate(GameObject root,int role,float height)
        {
            Require(!root.GetComponentsInChildren<Collider>(true).Any() && !root.GetComponentsInChildren<Rigidbody>(true).Any(),"Visual must not own collision.");
            Require(root.GetComponentsInChildren<MonoBehaviour>(true).All(m=>m is CharacterLocomotionAnimation),"Visual has missing/unauthorized scripts.");
            var animator=root.GetComponentInChildren<Animator>(true);
            Require(animator && animator.avatar && animator.avatar.isValid && !animator.avatar.isHuman && animator.runtimeAnimatorController && !animator.applyRootMotion,"Invalid Generic rig/controller.");
            var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Require(skins.Length==(role==1?10:6) && skins.All(s=>s.sharedMesh && s.bones.Length>0 && s.bones.All(b=>b)),"Missing source pieces or skin bones.");
            Require(skins.SelectMany(s=>s.sharedMaterials).All(m=>m && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m))),"Missing material.");
            animator.Rebind(); animator.Update(0);
            var bounds=UserCharacterSetup.LocalBounds(root);
            Require(Mathf.Abs(bounds.size.y-height)<.04f && Mathf.Abs(bounds.min.y)<.04f,"Height/ground alignment: "+bounds);
            var all=root.GetComponentsInChildren<Transform>(true);
            Vector3 facing=root.transform.InverseTransformDirection(all.Single(t=>t.name=="FaceForward").position-all.Single(t=>t.name=="HeadCenter").position);
            Require(facing.z>.05f && Mathf.Abs(facing.x)<.02f,"Character must face +Z.");
        }

        public static void BatchBuildWindows()
        {
            const string output="Builds/CharacterSwap/FireRescue.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{TutorialGreyboxSetup.ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText(Evidence+"/build.txt",report.summary.result+"\nErrors="+report.summary.totalErrors+" Warnings="+report.summary.totalWarnings+"\n"+output);
            EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
        }

        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}
