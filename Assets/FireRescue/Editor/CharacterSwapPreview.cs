using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace FireRescue.Editor
{
    // Real Unity import/skin/controller render, independent of Blender's source preview.
    public static class CharacterSwapPreview
    {
        public static void BatchRender()
        {
            try
            {
                if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("Graphics device required.");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.65f,.65f,.65f);
                var light=new GameObject("Preview light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.05f;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(45,-35,0);
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.06f,0);floor.transform.localScale=new Vector3(20,.1f,20);
                floor.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Standard")){color=new Color(.64f,.61f,.55f)};
                var camera=new GameObject("Offscreen camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.64f,.61f,.55f);camera.orthographic=true;camera.orthographicSize=1.06f;camera.nearClipPlane=.05f;camera.farClipPlane=30;
                for(int role=1;role<=2;role++)
                {
                    var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CharacterSwapSetup.Prefab(role)));
                    var animator=model.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);
                    Render(camera,animator,new Vector3(10,180,0),"Idle",.2f,"role"+role+"-front-idle.png");
                    Render(camera,animator,new Vector3(25,145,0),"Walk",.2f,"role"+role+"-walk.png");
                    UnityEngine.Object.DestroyImmediate(model);
                }
                File.WriteAllText(CharacterSwapSetup.Evidence+"/preview.txt","UNITY_RENDER_PASS\n"+SystemInfo.graphicsDeviceType+"\nActual Generic skins, Idle/Walk controllers, external Standard materials.\n");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Render(Camera camera,Animator animator,Vector3 angle,string state,float time,string name)
        {
            animator.SetFloat("Speed",state=="Walk"?1:0);animator.SetFloat("CycleSpeed",1);animator.Play(state,0,time);animator.Update(0);
            camera.transform.rotation=Quaternion.Euler(angle);camera.transform.position=new Vector3(0,.78f,0)-camera.transform.forward*5;
            var target=new RenderTexture(700,700,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(700,700,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,700,700),0,0);texture.Apply();File.WriteAllBytes(CharacterSwapSetup.Evidence+"/"+name,texture.EncodeToPNG());}
            finally{RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(texture);target.Release();UnityEngine.Object.DestroyImmediate(target);}
        }
    }
}
