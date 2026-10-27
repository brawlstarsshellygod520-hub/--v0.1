using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace FireRescue.Editor
{
    public static class ReleasePolishSetup
    {
        public const string Evidence="Docs/Validation/2026-10-28-restart-release";
        public static void BatchAudit()
        {
            EditorSceneManager.OpenScene(FirstReview1027Setup.ScenePath);
            var root=GameObject.Find("First Review 1027");
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var f=r.GetComponent<MeshFilter>();var mesh=r is SkinnedMeshRenderer s?s.sharedMesh:f?f.sharedMesh:null;
                string p=AssetDatabase.GetAssetPath(mesh);
                if(p.Contains("/AIGenerated/")||!r.gameObject.activeInHierarchy||!r.enabled)
                    Debug.Log("ART_AUDIT "+PathOf(r.transform)+" active="+r.gameObject.activeInHierarchy+" renderer="+r.enabled+" mesh="+p+" materials="+string.Join(";",r.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
            }
            foreach(var p in AssetDatabase.GetDependencies(FirstReview1027Setup.ScenePath,true).Where(p=>p.Contains("/AIGenerated/")))Debug.Log("ART_DEP "+p);
            EditorApplication.Exit(0);
        }
        public static void BatchApply()
        {
            try
            {
                Directory.CreateDirectory(Evidence);var scene=EditorSceneManager.OpenScene(FirstReview1027Setup.ScenePath);
                var incident=UnityEngine.Object.FindAnyObjectByType<CookingOilIncident>();var data=new SerializedObject(incident);data.FindProperty("fastSeconds").floatValue=5;data.ApplyModifiedPropertiesWithoutUndo();
                var effects=new SerializedObject(UnityEngine.Object.FindAnyObjectByType<FirstRescueEffects>());effects.FindProperty("spaciousFire").boolValue=true;effects.ApplyModifiedPropertiesWithoutUndo();
                var oldArt=AssetDatabase.GetDependencies(FirstReview1027Setup.ScenePath,true).Where(p=>p.Contains("/AIGenerated/")&&!p.Contains("/ReviewBoundaries/")).ToArray();
                if(oldArt.Length>0)throw new InvalidOperationException("Unexpected legacy AI art dependency: "+string.Join(";",oldArt));
                WriteFramePreview(false);WriteFramePreview(true);
                if(RoundedContextFrame.SourceUv(0,0).y<=RoundedContextFrame.SourceUv(0,2).y)throw new InvalidOperationException("Context frame vertical UV regression");
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                File.WriteAllText(Evidence+"/install.txt","POLISH_INSTALLED\nFirst kitchen wave=5 seconds. Restart button and shared scene restart handler. Rounded context frame top-down GUI / bottom-up texture mapping corrected.\nLegacy AI art dependencies=0; authorized procedural courtyard material retained. Historical source assets preserved, not deleted.\n");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void WriteFramePreview(bool corrected)
        {
            const int w=1000,h=150,b=14,left=40,top=30,pw=920,ph=90;
            var src=RoundedContextFrame.CreateTexture(true);var dst=new Texture2D(w,h,TextureFormat.RGB24,false);var bg=new Color(.39f,.48f,.48f);
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                Color c=bg;int px=x-left,py=y-top;
                if(px>=0&&py>=0&&px<pw&&py<ph)
                {
                    int col=px<b?0:px>=pw-b?2:1,row=py<b?0:py>=ph-b?2:1;
                    float ox=col==0?0:col==1?b:pw-b,oy=row==0?0:row==1?b:ph-b;
                    float sw=col==1?pw-2*b:b,sh=row==1?ph-2*b:b;
                    var uv=RoundedContextFrame.SourceUv(col,row);if(!corrected&&row!=1)uv.y=row==0?0:1-12f/64;
                    var pixel=src.GetPixelBilinear(uv.x+(px-ox+.5f)/sw*uv.width,uv.y+(1-(py-oy+.5f)/sh)*uv.height);
                    c=Color.Lerp(bg,pixel,pixel.a);
                }
                dst.SetPixel(x,h-1-y,c);
            }
            dst.Apply();File.WriteAllBytes(Evidence+(corrected?"/frame-after.png":"/frame-before.png"),dst.EncodeToPNG());UnityEngine.Object.DestroyImmediate(src);UnityEngine.Object.DestroyImmediate(dst);
        }
        static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
    }
}
