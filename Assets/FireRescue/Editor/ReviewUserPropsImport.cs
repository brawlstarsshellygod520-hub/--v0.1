using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FireRescue.Editor
{
    // One-off authored-model integration. No runtime rules depend on these mesh names.
    public static class ReviewUserPropsImport
    {
        public const string Folder = FirstReview1027Setup.Folder + "/Props20261028";
        public const string Evidence = "Docs/Validation/2026-10-28-user-props";
        const string SourceFolder = "ArtSource/UserProvided/Level1027/PropsUpdate20261028";
        [Serializable] sealed class Export { public string sourceSha256; public Vector3 translation; public Part[] meshes; }
        [Serializable] sealed class Part { public string name; public Vector3[] vertices, renderVertices, renderNormals; public int[] triangles; }

        public static void BatchApply()
        {
            try { Apply(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        public static void BatchReviewAndBuild()
        {
            try
            {
                EditorSceneManager.OpenScene(FirstReview1027Setup.ScenePath);
                foreach(var key in new[]{"Toilet Water Source","Soda Powder Source","Oil Pan Fire","Pudding Item"})
                    Capture(GameObject.Find("First Review 1027/"+key).transform.Find("Visual"),key);
                FirstReview1027Setup.BatchBuild();
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static Bounds BoundsFor(IEnumerable<Part> parts)
        {
            var vertices=parts.SelectMany(p=>p.vertices).ToArray();
            if(vertices.Length==0)throw new InvalidDataException("Empty authored model");
            var b=new Bounds(vertices[0],Vector3.zero);foreach(var v in vertices)b.Encapsulate(v);return b;
        }
        static void Apply()
        {
            var data=JsonUtility.FromJson<Export>(File.ReadAllText(SourceFolder+"/props.geometry.json"));
            // The house must be identical: this migration only replaces the four authored props.
            var original=JsonUtility.FromJson<Export>(File.ReadAllText("ArtSource/UserProvided/Level1027/level1027.geometry.json"));
            foreach(var before in original.meshes)
            {
                var after=data.meshes.Single(p=>p.name==before.name);
                if(!before.vertices.SequenceEqual(after.vertices)||!before.triangles.SequenceEqual(after.triangles))
                    throw new InvalidDataException("House changed; a props-only migration is unsafe: "+before.name);
            }
            EnsureFolder(Folder);Directory.CreateDirectory(Evidence);
            var scene=EditorSceneManager.OpenScene(FirstReview1027Setup.ScenePath);
            var root=GameObject.Find("First Review 1027");if(!root)throw new InvalidOperationException("Wrong scene");
            var sources=root.GetComponentsInChildren<FluidSource>();
            var water=sources.Single(s=>s.Element==ElementKind.Water);
            var powder=sources.Single(s=>s.Element==ElementKind.DryPowder);
            var fire=root.GetComponentInChildren<ObjectFire>();var food=root.GetComponentInChildren<CarryItem>();
            var toiletParts=Select(data,"馬桶本體","馬桶座","水箱","水");
            var powderParts=Select(data,"立方體.002");
            var panParts=Select(data,"油鍋","握把","油");
            var foodParts=Select(data,"盤子","布丁","黑糖");
            var toilet=BoundsFor(toiletParts);var soda=BoundsFor(powderParts);var pan=BoundsFor(panParts);var pudding=BoundsFor(foodParts);
            Vector3 toiletOrigin=new Vector3(toilet.center.x,toilet.min.y,toilet.center.z);
            Vector3 sodaOrigin=new Vector3(soda.center.x,soda.min.y,soda.center.z);
            var oil=BoundsFor(Select(data,"油"));
            Vector3 fireOrigin=oil.center;
            Replace(water.gameObject,"Toilet",toiletParts,toiletOrigin,toilet);
            Replace(powder.gameObject,"Soda",powderParts,sodaOrigin,soda);
            Replace(fire.gameObject,"OilPan",panParts,fireOrigin,pan);
            Replace(food.gameObject,"Pudding",foodParts,pudding.center,pudding);
            // Keep source identity/profile and independent gameplay anchors; only align to authored art.
            var tank=BoundsFor(Select(data,"水箱"));
            SetOutlet(water,new Vector3(tank.max.x+.04f,tank.max.y-.035f,tank.center.z));
            SetOutlet(powder,new Vector3(soda.center.x,soda.max.y-.04f,soda.min.z-.04f));
            var interaction=new SerializedObject(water).FindProperty("interactionPoint").objectReferenceValue as Transform;
            if(interaction)interaction.position=new Vector3(toilet.max.x+.08f,.65f,toilet.center.z);
            // Incoming spray can hit the pot surface, not the old placeholder's empty space.
            var hit=fire.GetComponent<BoxCollider>();hit.center=pan.center-fireOrigin;hit.size=pan.size+Vector3.one*.06f;
            Physics.SyncTransforms();
            Check(root.GetComponentsInChildren<FluidSource>().Length==2,"Only two existing source identities");
            Check(root.GetComponentsInChildren<CarryItem>().Length==1,"Only one existing pudding identity");
            Check(root.GetComponentsInChildren<Component>(true).All(c=>c),"No missing components");
            AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
            // Save/reopen proves references survive serialization rather than just live Editor wiring.
            EditorSceneManager.OpenScene(FirstReview1027Setup.ScenePath);
            var lines=new List<string>{"USER_PROPS_IMPORT_PASS","Source SHA256="+data.sourceSha256,"House geometry unchanged; source blend never saved by exporter.",
                "Toilet="+toiletOrigin.ToString("F5"),"Soda="+sodaOrigin.ToString("F5"),"Oil surface="+fireOrigin.ToString("F5"),"Pudding="+pudding.center.ToString("F5"),
                "All 4 visuals use only authored geometry at original world positions; basic colors and Blender corner normals preserved. Water/oil downward faces corrected in derived mesh only.",
                "Excluded detached test objects: 立方體, 立方體.001. Existing old generated asset files retained; no longer used by active prop visuals."};
            foreach(var key in new[]{"Toilet Water Source","Soda Powder Source","Oil Pan Fire","Pudding Item"})
            {
                var go=GameObject.Find("First Review 1027/"+key);Check(go,"Missing root "+key);
                var models=go.transform.Find("Visual").GetComponentsInChildren<MeshFilter>();
                Check(models.Length>0&&models.All(m=>AssetDatabase.GetAssetPath(m.sharedMesh).StartsWith(Folder+"/")),"Authored visuals survived reload: "+key);
                lines.Add("PASS "+key+" meshes="+models.Length);
                Capture(go.transform.Find("Visual"),key);
            }
            File.WriteAllLines(Evidence+"/import.txt",lines);
        }
        static Part[] Select(Export data,params string[] names)=>names.Select(n=>data.meshes.Single(p=>p.name==n)).ToArray();
        static void SetOutlet(FluidSource source,Vector3 world)
        {
            var data=new SerializedObject(source);var port=data.FindProperty("outlet").objectReferenceValue as Transform;
            if(!port){port=new GameObject("Outlet").transform;port.SetParent(source.transform,false);source.SetOutlet(port);}
            port.position=world;
        }
        static void Replace(GameObject owner,string model,Part[] parts,Vector3 origin,Bounds bounds)
        {
            var old=owner.transform.Find("Visual");if(old)Object.DestroyImmediate(old.gameObject);
            // Legacy toilet has inactive prototype children; preserve them but never render them.
            foreach(var renderer in owner.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            owner.transform.SetPositionAndRotation(origin,Quaternion.identity);owner.transform.localScale=Vector3.one;
            var visual=new GameObject("Visual").transform;visual.SetParent(owner.transform,false);
            foreach(var part in parts)
            {
                string path=Folder+"/"+part.name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                var normals=(Vector3[])part.renderNormals.Clone();var triangles=Enumerable.Range(0,part.renderVertices.Length).ToArray();
                if((part.name=="水"||part.name=="油")&&normals.Average(n=>n.y)<0)
                {
                    for(int i=0;i<triangles.Length;i+=3){int oldIndex=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=oldIndex;}
                    for(int i=0;i<normals.Length;i++)normals[i]=-normals[i];
                }
                var generated=new Mesh{name=part.name,vertices=part.renderVertices.Select(v=>v-origin).ToArray(),triangles=triangles,normals=normals};
                generated.RecalculateBounds();
                if(mesh){EditorUtility.CopySerialized(generated,mesh);Object.DestroyImmediate(generated);EditorUtility.SetDirty(mesh);}
                else{mesh=generated;AssetDatabase.CreateAsset(mesh,path);}
                var child=new GameObject(part.name);child.transform.SetParent(visual,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;
                child.AddComponent<MeshRenderer>().sharedMaterial=MaterialFor(part.name);
            }
            var box=owner.GetComponent<BoxCollider>();if(!box)box=owner.AddComponent<BoxCollider>();
            box.center=bounds.center-origin;box.size=bounds.size+Vector3.one*.015f;box.enabled=true;box.isTrigger=false;
            var outline=owner.GetComponent<InteractionOutlineTarget>();if(outline)outline.SetVisualRoot(visual);
            PrefabUtility.SaveAsPrefabAsset(visual.gameObject,Folder+"/"+model+"Visual.prefab");
        }
        static Material MaterialFor(string name)
        {
            Color c=name=="水"?new Color(.22f,.65f,.83f):name=="油"?new Color(.80f,.46f,.055f):
                name=="油鍋"?new Color(.19f,.22f,.23f):name=="握把"?new Color(.13f,.12f,.11f):
                name=="布丁"?new Color(1f,.77f,.24f):name=="黑糖"?new Color(.36f,.14f,.045f):
                name=="立方體.002"?new Color(.3f,.64f,.82f):new Color(.91f,.89f,.80f);
            string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("Standard")){name=name};AssetDatabase.CreateAsset(material,path);}
            material.color=c;material.SetFloat("_Metallic",0);material.SetFloat("_Glossiness",name=="水"||name=="油"?.25f:.12f);EditorUtility.SetDirty(material);return material;
        }
        static void Capture(Transform visual,string name)
        {
            // Isolate a visual copy on its own layer; no gameplay or screen capture involved.
            var copy=Object.Instantiate(visual.gameObject);copy.transform.position=Vector3.zero;
            foreach(var t in copy.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
            var renderers=copy.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            var cam=new GameObject("Temporary prop review camera").AddComponent<Camera>();cam.cullingMask=1<<30;cam.orthographic=true;
            cam.orthographicSize=Mathf.Max(.15f,b.size.magnitude*.6f);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.31f,.33f,.32f);
            cam.transform.position=b.center+new Vector3(2,2,-3);cam.transform.LookAt(b.center);
            var rt=new RenderTexture(720,720,24);var previous=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            var tex=new Texture2D(720,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,720,720),0,0);tex.Apply();File.WriteAllBytes(Evidence+"/"+name+".png",tex.EncodeToPNG());
            RenderTexture.active=previous;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(copy);
        }
        static void Check(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
        static void EnsureFolder(string path){if(AssetDatabase.IsValidFolder(path))return;EnsureFolder(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}
    }
}
