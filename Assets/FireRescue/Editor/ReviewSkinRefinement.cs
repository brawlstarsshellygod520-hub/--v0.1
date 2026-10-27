using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FireRescue.Editor
{
    // A derived binding of the supplied Role2 geometry, not a generated replacement model.
    public static class ReviewSkinRefinement
    {
        public const string Folder="Assets/FireRescue/Art/UserProvided/Characters/Rigged/Review1027";
        public static void Apply(Transform rig,Transform actor)
        {
            EnsureFolder(Folder);
            foreach(var skin in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                string name=skin.name;
                if(name=="立方體"||name=="圓形")continue; // Entire face/hair stays rigidly on Head.
                string path=Folder+"/"+name+"-Skin.asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                var source=skin.sharedMesh;var copy=UnityEngine.Object.Instantiate(source);copy.name=name+" Review binding";
                var bones=skin.bones;
                int Index(string n)=>Array.FindIndex(bones,b=>b&&b.name==n);
                // Bind-pose heads are stable even when the imported preview is playing Idle.
                Vector3[] heads=source.bindposes.Select(m=>actor.InverseTransformPoint(skin.transform.TransformPoint(m.inverse.MultiplyPoint3x4(Vector3.zero)))).ToArray();
                int hips=Index("Hips"),spine=Index("Spine"),chest=Index("Chest"),head=Index("Head");
                BoneWeight Pair(int a,int b,float t)=>new BoneWeight{boneIndex0=a,weight0=1-Mathf.Clamp01(t),boneIndex1=b,weight1=Mathf.Clamp01(t)};
                BoneWeight Torso(float y)=>y<.55f?Pair(hips,spine,Mathf.InverseLerp(.34f,.55f,y)):Pair(spine,chest,Mathf.InverseLerp(.55f,.72f,y));
                BoneWeight Chain(Vector3 p,int a,int b,int c)
                {
                    Vector3 ab=heads[b]-heads[a],bc=heads[c]-heads[b];
                    float t=Mathf.Clamp01(Vector3.Dot(p-heads[a],ab)/ab.sqrMagnitude),u=Mathf.Clamp01(Vector3.Dot(p-heads[b],bc)/bc.sqrMagnitude);
                    return (p-Vector3.Lerp(heads[a],heads[b],t)).sqrMagnitude < (p-Vector3.Lerp(heads[b],heads[c],u)).sqrMagnitude?Pair(a,b,t):Pair(b,c,u);
                }
                var weights=new BoneWeight[source.vertexCount];var vertices=source.vertices;
                for(int i=0;i<weights.Length;i++)
                {
                    Vector3 p=actor.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]));
                    bool coat=name=="立方體.002",trim=name=="立方體.003"||name=="立方體.004";
                    if(trim){weights[i]=Torso(p.y);continue;}
                    if(!coat&&p.y>.84f){weights[i]=Pair(head,head,0);continue;}
                    // The original exporter never assigned LowerArm, and assigned coat hems
                    // and the lower trim to thigh bones. Keep clothes attached to the torso.
                    if(Mathf.Abs(p.x)>.30f&&p.y>(coat?.48f:.36f)&&p.y<.81f)
                    {
                        string side=heads[Index("L_UpperArm")].x*p.x>0?"L_":"R_";
                        weights[i]=Chain(p,Index(side+"UpperArm"),Index(side+"LowerArm"),Index(side+"Hand"));
                    }
                    else if(!coat&&p.y<.34f)
                    {
                        string side=heads[Index("L_Thigh")].x*p.x>0?"L_":"R_";
                        weights[i]=Chain(p,Index(side+"Thigh"),Index(side+"Shin"),Index(side+"Foot"));
                    }
                    else weights[i]=Torso(p.y);
                }
                copy.boneWeights=weights;
                if(copy.vertexCount!=source.vertexCount||!copy.triangles.SequenceEqual(source.triangles))throw new InvalidOperationException("Review skin refinement must not replace user topology");
                if(existing){EditorUtility.CopySerialized(copy,existing);UnityEngine.Object.DestroyImmediate(copy);EditorUtility.SetDirty(existing);skin.sharedMesh=existing;}
                else{AssetDatabase.CreateAsset(copy,path);skin.sharedMesh=copy;}
                var bounds=skin.sharedMesh.bounds;bounds.Expand(.4f);skin.localBounds=bounds;
            }
        }
        static void EnsureFolder(string path){if(AssetDatabase.IsValidFolder(path))return;EnsureFolder(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}
    }
}
