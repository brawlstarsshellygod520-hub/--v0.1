using System;
using System.IO;
using UnityEngine;

namespace FireRescue.Editor
{
    // Render actual imported FBX/prefabs, not concept images. Temporary objects are never saved.
    public static class ResidentialArtPreview
    {
        public static void CaptureHouse()
        {
            var go = new GameObject("Temporary overview camera");
            var camera = go.AddComponent<Camera>();
            try
            {
                camera.orthographic = true; camera.orthographicSize = 10;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.81f,.79f,.73f);
                Quaternion rotation = Quaternion.Euler(60,0,0);
                camera.transform.SetPositionAndRotation(new Vector3(0,.4f,1.5f)-rotation*Vector3.forward*30,rotation);
                Capture(camera,"Logs/residential-overview.png",1600,1100);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            CaptureGallery();
        }

        static void CaptureGallery()
        {
            var root = new GameObject("Temporary art gallery");
            var go = new GameObject("Temporary gallery camera");
            var camera = go.AddComponent<Camera>();
            try
            {
                string[] names = { "Firefighter", "Resident", "Toilet", "SaladOil", "Nozzle", "Sofa", "CoffeeTable", "Bed", "KitchenCounter", "Sink", "Shower" };
                for (int i=0;i<names.Length;i++)
                {
                    var model = ResidentialArtCatalog.Visual(names[i],root.transform);
                    model.transform.localPosition = new Vector3(44+(i%4)*2.6f,0,4-(i/4)*2.8f);
                    if (names[i]=="Firefighter" || names[i]=="Resident") model.transform.localRotation=Quaternion.Euler(0,150,0)*model.transform.localRotation;
                }
                camera.orthographic=true; camera.orthographicSize=4.8f;
                camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.81f,.79f,.73f);
                Quaternion rotation=Quaternion.Euler(35,15,0);
                camera.transform.SetPositionAndRotation(new Vector3(48,.7f,1)-rotation*Vector3.forward*25,rotation);
                Capture(camera,"Logs/residential-model-gallery.png",1800,1000);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(go); }
        }

        public static void Capture(Camera camera,string path,int width=1600,int height=900)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var target = new RenderTexture(width,height,24);
            var image = new Texture2D(width,height,TextureFormat.RGB24,false);
            var previousTarget = camera.targetTexture; var previousActive=RenderTexture.active;
            try
            {
                camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply();
                File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=previousTarget; RenderTexture.active=previousActive;
                UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
