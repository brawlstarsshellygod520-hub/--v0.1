using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace FireRescue.Editor
{
    // All images are rendered from the actual imported, weighted FBX models.
    public static class CharacterRigPreview
    {
        const string Output = "Docs/Art/Models/v0.3/";
        const string Frames = "Logs/CharacterFrames/v0.3/";

        [MenuItem("消防 Prototype/擷取新版角色與動作預覽")]
        public static void Capture() => CaptureTo(Output, Frames);

        public static void CaptureBaseline() => CaptureTo(Output + "before/", null);

        static void CaptureTo(string output, string frames)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var oldScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var oldRoots = oldScene.GetRootGameObjects();
            var oldActive = oldRoots.Select(root => root.activeSelf).ToArray();
            foreach (var root in oldRoots) root.SetActive(false);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            try
            {
                Directory.CreateDirectory(output);
                if (frames != null) Directory.CreateDirectory(frames);
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Temporary studio floor";
                floor.transform.localScale = new Vector3(200, .08f, 200);
                floor.transform.position = new Vector3(0, -.04f, 0); // Top surface matches the actor's y=0 ground.
                var floorMat = new Material(Shader.Find("Standard"));
                floorMat.color = new Color(.48f, .43f, .37f);
                floorMat.SetFloat("_Glossiness", .05f);
                floor.GetComponent<Renderer>().sharedMaterial = floorMat;
                floor.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.42f, .40f, .37f);
                var light = new GameObject("Temporary soft key").AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = .72f;
                light.shadows = LightShadows.Soft;
                light.shadowResolution = LightShadowResolution.VeryHigh;
                // A large normal bias detached toy-scale foot shadows by many centimetres.
                // This temporary studio setting does not change gameplay lighting.
                light.shadowBias = .01f; light.shadowNormalBias = .015f;
                light.transform.rotation = Quaternion.Euler(70, -28, 0);
                var fill = new GameObject("Temporary fill").AddComponent<Light>();
                fill.type = LightType.Directional; fill.intensity = .18f;
                fill.transform.rotation = Quaternion.Euler(20, 145, 0);
                var firefighter = ResidentialArtCatalog.Visual("Firefighter", null);
                var resident = ResidentialArtCatalog.Visual("Resident", null);
                firefighter.transform.position = new Vector3(-.85f, 0, 0);
                resident.transform.position = new Vector3(.85f, 0, 0);
                var nozzle = ResidentialArtCatalog.Visual("Nozzle", firefighter.transform);
                nozzle.transform.localPosition = new Vector3(0, .8f, .1f);
                CharacterArtSetup.MountNozzlePresentation(firefighter, firefighter);
                foreach (var skin in firefighter.GetComponentsInChildren<SkinnedMeshRenderer>().Concat(resident.GetComponentsInChildren<SkinnedMeshRenderer>()))
                {
                    skin.updateWhenOffscreen = true;
                    // Camera.Render captures many poses in one Editor update; refresh cached skin matrices each render.
                    skin.forceMatrixRecalculationPerRender = true;
                }
                foreach (var animator in firefighter.GetComponentsInChildren<Animator>().Concat(resident.GetComponentsInChildren<Animator>()))
                {
                    animator.Rebind();
                    animator.Update(0);
                }
                var camera = new GameObject("Temporary studio camera").AddComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 1.55f;
                camera.nearClipPlane = .1f; camera.farClipPlane = 25f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.66f, .61f, .54f);
                var center = new Vector3(0, .75f, 0);
                camera.transform.position = center + new Vector3(2.2f, 2.1f, 7);
                camera.transform.LookAt(center);
                Sample(firefighter, "Firefighter", "Idle", .5f);
                Sample(resident, "Resident", "Idle", .5f);
                ResidentialArtPreview.Capture(camera, output + "unity-characters.png", 1400, 900);
                camera.transform.position = center + new Vector3(0, 1.2f, 7);
                camera.transform.LookAt(center);
                ResidentialArtPreview.Capture(camera, output + "unity-front.png", 1400, 900);
                // Spread along screen-horizontal for this view; don't let one actor hide the other.
                firefighter.transform.position = new Vector3(0, 0, -.85f);
                resident.transform.position = new Vector3(0, 0, .85f);
                camera.transform.position = center + new Vector3(7, 1.2f, 0);
                camera.transform.LookAt(center);
                ResidentialArtPreview.Capture(camera, output + "unity-side.png", 1400, 900);
                firefighter.transform.position = new Vector3(-.85f, 0, 0);
                resident.transform.position = new Vector3(.85f, 0, 0);
                camera.transform.position = center + new Vector3(-2.2f, 2.1f, -7);
                camera.transform.LookAt(center);
                ResidentialArtPreview.Capture(camera, output + "unity-back.png", 1400, 900);
                camera.transform.position = center + Quaternion.Euler(48, 40, 0) * Vector3.back * 7;
                camera.transform.LookAt(center);
                ResidentialArtPreview.Capture(camera, output + "unity-game-angle.png", 1400, 900);
                camera.transform.position = center + new Vector3(2.2f, 2.1f, 7);
                camera.transform.LookAt(center);
                for (int frame = 0; frames != null && frame < 24; frame++)
                {
                    float time = frame / 30f;
                    Sample(firefighter, "Firefighter", "Run", time);
                    Sample(resident, "Resident", "Run", time);
                    ResidentialArtPreview.Capture(camera, frames + "run-" + frame.ToString("D2") + ".png", 900, 600);
                }
                if (frames != null)
                {
                    firefighter.transform.position = new Vector3(0, 0, -.85f);
                    resident.transform.position = new Vector3(0, 0, .85f);
                    camera.transform.position = center + new Vector3(7, 1.2f, 0);
                    camera.transform.LookAt(center);
                    for (int frame = 0; frame < 24; frame++)
                    {
                        Sample(firefighter, "Firefighter", "Run", frame / 30f);
                        Sample(resident, "Resident", "Run", frame / 30f);
                        ResidentialArtPreview.Capture(camera, frames + "run-side-" + frame.ToString("D2") + ".png", 900, 600);
                    }
                    firefighter.transform.position = new Vector3(-.85f, 0, 0);
                    resident.transform.position = new Vector3(.85f, 0, 0);
                    camera.transform.position = center + new Vector3(2.2f, 2.1f, 7);
                    camera.transform.LookAt(center);
                }
                for (int frame = 0; frames != null && frame < 30; frame++)
                {
                    float time = frame / 15f;
                    Sample(firefighter, "Firefighter", "Idle", time);
                    Sample(resident, "Resident", "Idle", time);
                    ResidentialArtPreview.Capture(camera, frames + "idle-" + frame.ToString("D2") + ".png", 900, 600);
                }
                UnityEngine.Object.DestroyImmediate(floorMat);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(oldScene);
                for (int i = 0; i < oldRoots.Length; i++) oldRoots[i].SetActive(oldActive[i]);
            }
            Debug.Log("CHARACTER_PREVIEW_PASS: actual Unity front/angled/side/back/game-angle and 24 Run + 24 side Run + 30 Idle frames.");
        }

        static void Sample(GameObject visual, string name, string action, float time)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(ResidentialArtCatalog.ModelPath + name + ".fbx")
                .OfType<AnimationClip>().Single(c => c.name == action);
            var animator = visual.GetComponentInChildren<Animator>();
            Vector3 position = visual.transform.position;
            Quaternion rotation = visual.transform.rotation;
            // Imported Generic clips are evaluated through their actual Animator, not legacy SampleAnimation.
            animator.SetFloat("Speed", action == "Run" ? 1 : 0);
            animator.SetFloat("CycleSpeed", 1);
            animator.Play(action, 0, time / clip.length);
            animator.Update(0);
            visual.transform.SetPositionAndRotation(position, rotation);
        }
    }
}
