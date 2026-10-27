using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace FireRescue.Editor
{
    // Offscreen asset renders only. Does not open, navigate or save the user's editor view.
    public static class UserCharacterPreview
    {
        public static void BatchRender()
        {
            try
            {
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new Exception("Preview requires a graphics device, not -nographics.");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UserCharacterSetup.PrefabPath));
                var animator = model.GetComponentInChildren<Animator>();
                animator.Rebind(); animator.Update(0);
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.transform.position = new Vector3(0,-.06f,0); floor.transform.localScale = new Vector3(20,.1f,20);
                floor.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(.64f,.61f,.55f) };
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.65f,.65f,.65f);
                var light = new GameObject("Preview light").AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.05f; light.shadows = LightShadows.Soft;
                light.transform.rotation = Quaternion.Euler(45,-35,0);
                var camera = new GameObject("Offscreen Camera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.64f,.61f,.55f);
                camera.orthographic = true; camera.orthographicSize = .98f; camera.nearClipPlane = .05f; camera.farClipPlane = 30;
                Directory.CreateDirectory(UserCharacterSetup.Evidence);
                // The imported character faces +Z; a front camera must look along -Z.
                Render(camera,animator,new Vector3(0,180,0),"Idle",.2f,"unity-front-idle.png");
                model.transform.rotation = Quaternion.Euler(0,180,0);
                Render(camera,animator,new Vector3(50,40,0),"Run",.2f,"unity-game-angle-run.png");
                File.WriteAllText(UserCharacterSetup.Evidence + "/preview.txt", "OFFSCREEN_UNITY_PREVIEW_PASS\n" + SystemInfo.graphicsDeviceType +
                    "\nActual imported prefab, external Standard materials and Generic clips. No UI control or scene save.\n");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        static void Render(Camera camera, Animator animator, Vector3 angle, string state, float time, string name)
        {
            animator.SetFloat("Speed",state == "Run" ? 1 : 0); animator.SetFloat("CycleSpeed",1);
            animator.Play(state,0,time); animator.Update(0);
            camera.transform.rotation = Quaternion.Euler(angle);
            camera.transform.position = new Vector3(0,.74f,0) - camera.transform.forward * 5;
            var target = new RenderTexture(700,700,24,RenderTextureFormat.ARGB32);
            var texture = new Texture2D(700,700,TextureFormat.RGB24,false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0,0,700,700),0,0); texture.Apply();
                File.WriteAllBytes(UserCharacterSetup.Evidence + "/" + name,texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(texture); target.Release(); UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
