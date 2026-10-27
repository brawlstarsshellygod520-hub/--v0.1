using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    // One-shot before/after diagnostics, on disposable objects only.
    public static class HosePhysicsBaseline
    {
        public static void BatchRun()
        {
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var source = new GameObject("Probe source").AddComponent<FluidSource>();
                source.Configure(ElementKind.Water);
                var player = new GameObject("Probe player"); player.transform.position=new Vector3(0,0,2);
                var hose=player.AddComponent<HoseConnection>();
                var inlet=new GameObject("Inlet").transform; inlet.SetParent(player.transform,false); inlet.localPosition=Vector3.up*1.04f;
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position=new Vector3(.5f,.35f,.7f);
                wall.transform.localScale=new Vector3(.1f,.7f,1.6f); wall.layer=8;
                var occlusion=new GameObject("Wall geometry").AddComponent<SprayWallOcclusion>();
                occlusion.Configure(new[]{wall.GetComponent<Collider>()},Array.Empty<HoseWindow>());
                // Reflection keeps this diagnostic compilable before the new optional API exists.
                MethodInfo configure=typeof(HoseConnection).GetMethod("ConfigurePhysics");
                configure?.Invoke(hose,new object[]{occlusion,inlet});
                Physics.SyncTransforms();
                bool connected=hose.TryConnect(source);
                Vector3 desired=new Vector3(1.6f,0,2);
                Vector3 limited=hose.ConstrainPosition(desired);
                bool crossed=limited.x>1.5f;
                player.transform.position=new Vector3(3,2,0);
                float usedAtHeight=hose.UsedLength;
                player.transform.position=new Vector3(0,0,2);
                source.enabled=false;
                bool disabledStillConnected=hose.IsConnected;
                string report="HOSE_BASELINE\nOptional collision API="+(configure!=null)+"\nInitial connected="+connected+
                    "\nDesired="+desired+" constrained="+limited+" crosses wall footprint="+crossed+
                    "\nUsedLength at planar radius3 and y2="+usedAtHeight+
                    "\nDisabled source still connected="+disabledStillConnected+"\n";
                Directory.CreateDirectory("Docs/Validation/2026-10-27-hose-fluid-physics");
                string suffix=configure==null?"hose-baseline.txt":"hose-probe-after.txt";
                File.WriteAllText("Docs/Validation/2026-10-27-hose-fluid-physics/"+suffix,report);
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
