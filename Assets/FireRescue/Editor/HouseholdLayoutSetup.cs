using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FireRescue.Editor
{
    public static class HouseholdLayoutSetup
    {
        public const string Evidence="Docs/Validation/2026-10-27-household-outline";
        public const string Marker="Residential Household Greybox";

        [MenuItem("FireRescue/Greybox/配置住宅家具與來源輪廓")]
        public static void InstallMenu()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Install();
        }

        public static void BatchInstallAndValidate()
        {
            try
            {
                Install();string once=File.ReadAllText(TutorialGreyboxSetup.ScenePath);
                Install();Require(once==File.ReadAllText(TutorialGreyboxSetup.ScenePath),"Repeated install changed saved scene.");
                HouseholdPropCatalog.ValidateAll();
                var layout=GameObject.Find(UserLevelOneBuilder.LayoutName);var set=layout.transform.Find(Marker);
                Require(set && set.childCount==6,"Expected six household area groups.");
                foreach(var group in set.Cast<Transform>())Require(group.childCount>0,"Empty household area "+group.name);
                foreach(var t in set.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Visual"))
                    Require(!t.GetComponentsInChildren<Collider>(true).Any(),"Furniture Visual contains gameplay colliders.");
                var player=layout.GetComponentInChildren<FirefighterMotor>();
                Require(player.GetComponent<HoseConnection>().MaximumLength==3,"Do not relax the 3m hose.");
                var highlight=player.GetComponentInChildren<SourceHighlightView>();
                Require(highlight && new SerializedObject(highlight).FindProperty("outlineShader").objectReferenceValue,"Outline shader must be serialized for player build.");
                Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/install.txt","HOUSEHOLD_INSTALL_PASS\nUnity "+Application.unityVersion+"\n25 catalog prefabs, six room groups, Visual/collider separation; idempotent scene save/reload.\nCore systems, source/fire/window anchors and 3m hose unchanged. Shader explicitly serialized.\n");
                RenderOverview("layout-after.png");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }

        static void Install()
        {
            HouseholdPropCatalog.EnsureAll();
            var scene=EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
            var layout=GameObject.Find(UserLevelOneBuilder.LayoutName).transform;
            if(layout.Find(Marker))
            {
                // Narrow migration for our first placement only; preserve later user-authored positions.
                bool adjusted=false;
                var couch=layout.Find(Marker+"/Living Room/Main sofa");
                var chair=layout.Find(Marker+"/Living Room/Right armchair");
                if(couch && Vector3.Distance(couch.localPosition,new Vector3(3.5f,0,-1.1f))<.001f){couch.localPosition=new Vector3(3.5f,0,-.75f);Record(couch);adjusted=true;}
                if(chair && Vector3.Distance(chair.localPosition,new Vector3(5.45f,0,-2.5f))<.001f){chair.localPosition=new Vector3(5.65f,0,-2.5f);Record(chair);adjusted=true;}
                if(adjusted)Require(EditorSceneManager.SaveScene(scene),"Clearance save failed.");
                return;
            }
            string before=CoreSnapshot(layout);
            var set=Child(layout,Marker);
            var kitchen=Child(set,"Kitchen");var living=Child(set,"Living Room");var bath=Child(set,"Bathroom");
            var bedroom=Child(set,"Bedroom");var storage=Child(set,"Storage");var outside=Child(set,"Exterior");

            // Keep tested gameplay hotspots and inlet/muzzle/window anchors fixed. This pass is household dressing.
            var props=layout.Find("Household Props");
            Hide(props.Find("Bed"));Hide(props.Find("Sofa")); // Old blocks remain recoverable, never deleted.
            ReplaceVisual(props.Find("Coffee table"),"CoffeeTable",new Vector3(.87f,1.5f,1.29f));
            ReplaceVisual(props.Find("Kitchen counter"),"KitchenCounter",new Vector3(2.4f,.9f,1.13f));
            ReplaceVisual(props.Find("Salad oil source"),"SaladOilBottle",Vector3.one);
            ReplaceVisual(props.Find("Toilet cistern source"),"Toilet",Vector3.one);

            Place(storage,"StorageCabinet","Storage cabinet",new Vector3(-6.5f,0,11.1f));
            Place(storage,"BakingSodaJar","Baking soda jar (placeholder)",new Vector3(-6.5f,1.4f,11.1f),0,false);
            Place(bath,"WashingMachine","Washing machine",new Vector3(-7.95f,0,2.9f));
            Place(bath,"Shower","Shower",new Vector3(-7.0f,0,2.95f));
            Place(bath,"BathroomSink","Washbasin",new Vector3(-2.05f,0,3.12f));

            Place(bedroom,"Bed","Bed",new Vector3(2.85f,0,9.45f));
            Place(bedroom,"Wardrobe","Wardrobe",new Vector3(4.1f,0,6.25f),180);
            Place(bedroom,"Desk","Computer desk",new Vector3(6.8f,0,10.9f));
            Place(bedroom,"Chair","Desk chair",new Vector3(6.8f,0,10.0f),180);
            var monitor=Place(bedroom,"Television","Computer monitor",new Vector3(6.8f,.76f,11.0f),0,false);
            monitor.localScale=new Vector3(.55f,.55f,.55f);Record(monitor);
            Frame(bedroom,"North bedroom window",new Vector3(5.2f,1.15f,11.65f),0,1.3f,.8f);
            Frame(bedroom,"East bedroom window",new Vector3(8.42f,1.15f,8.7f),90,1.3f,.8f);

            Place(kitchen,"Fridge","Fridge",new Vector3(-7.95f,0,.55f),90);
            Place(kitchen,"KitchenCounter","West counter A",new Vector3(-8.0f,0,-.55f),90);
            Place(kitchen,"Stove","Stove",new Vector3(-8.0f,0,-1.5f),90);
            Place(kitchen,"KitchenCounter","West counter B",new Vector3(-8.0f,0,-2.45f),90);
            Place(kitchen,"KitchenSink","Kitchen sink",new Vector3(-8.0f,0,-3.45f),90);
            Place(kitchen,"KitchenCounter","West counter C",new Vector3(-8.0f,0,-4.45f),90);
            for(int i=0;i<6;i++)Place(kitchen,"KitchenCounter","South counter "+(i+1),new Vector3(-7.35f+i,0,-5.55f));
            Frame(kitchen,"Kitchen window",new Vector3(-5.2f,1.15f,1.35f),0,1.5f,.8f);

            Place(living,"LowCabinet","Rear side cabinet",new Vector3(4.0f,0,5.08f),180);
            Place(living,"RecyclingBin","Recycling bin",new Vector3(-.35f,0,4.8f));
            Place(living,"TrashBin","Trash bin",new Vector3(-.35f,0,3.95f));
            Place(living,"Sofa","Main sofa",new Vector3(3.5f,0,-.75f));
            Place(living,"Armchair","Left armchair",new Vector3(1.65f,0,-2.5f),-90);
            Place(living,"Armchair","Right armchair",new Vector3(5.65f,0,-2.5f),90);
            Place(living,"TvStand","TV stand",new Vector3(3.5f,0,-5.35f),180);
            Place(living,"Television","Television",new Vector3(3.5f,.54f,-5.35f),180,false);
            Place(living,"LowCabinet","TV left console",new Vector3(1.9f,0,-5.35f),180);
            Place(living,"LowCabinet","TV right console",new Vector3(7.0f,0,-5.35f),180);
            Place(living,"EntryShoeCabinet","Entry shoe cabinet",new Vector3(8.02f,0,-3.9f),-90);
            Frame(living,"Living room tall window",new Vector3(8.42f,1.0f,3.1f),90,1.6f,1.5f);
            Place(outside,"SodaCart","Handmade soda cart (placeholder)",new Vector3(10.65f,0,8.5f),-90);

            var highlight=layout.GetComponentInChildren<SourceHighlightView>();
            var settings=new SerializedObject(highlight);var shader=Shader.Find("FireRescue/InteractionOutline");Require(shader,"Outline shader missing.");
            settings.FindProperty("outlineShader").objectReferenceValue=shader;settings.ApplyModifiedProperties();Record(highlight);
            Require(before==CoreSnapshot(layout),"Gameplay/anchor/camera data changed; refuse scene save.");
            Physics.SyncTransforms();
            if(!Application.isBatchMode)Undo.RegisterCreatedObjectUndo(set.gameObject,"Add household greybox");
            Require(EditorSceneManager.SaveScene(scene),"Scene save failed.");
        }

        static string CoreSnapshot(Transform layout)
        {
            var types=new[]{typeof(FireField),typeof(FluidSource),typeof(HoseConnection),typeof(FirefighterMotor),typeof(LocalPlayerInput),typeof(SprayEmitter),typeof(ElementReaction),typeof(RescueNpc),typeof(RescueGameManager),typeof(BreakableDoor),typeof(HoseWindow),typeof(ObjectFire)};
            var core=layout.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c && types.Contains(c.GetType())).Cast<Component>().ToList();
            foreach(var c in core.ToArray())
            {
                core.Add(c.transform);
                var anchors=c.transform.Find("Anchors");if(anchors)core.AddRange(anchors.GetComponentsInChildren<Transform>(true));
            }
            core.Add(Camera.main);core.Add(Camera.main.transform);core.Add(Camera.main.GetComponent<RescueCamera>());
            return string.Join("\n",core.Distinct().Select(c=>c.GetType().FullName+":"+EditorJsonUtility.ToJson(c)).OrderBy(s=>s,StringComparer.Ordinal));
        }

        static Transform Place(Transform parent,string kind,string name,Vector3 position,float yaw=0,bool blocking=true)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HouseholdPropCatalog.PrefabPath(kind)),parent);
            go.name=name;go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            if(!blocking)go.GetComponent<BoxCollider>().enabled=false;
            Record(go.transform);Record(go);Record(go.GetComponent<BoxCollider>());return go.transform;
        }
        static void ReplaceVisual(Transform owner,string kind,Vector3 scale)
        {
            Require(owner,"Missing household root for "+kind);var slot=owner.Find("Visual");Require(slot,"Missing Visual for "+owner.name);
            foreach(var child in slot.Cast<Transform>().ToArray())Hide(child);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(HouseholdPropCatalog.PrefabPath(kind));
            var visual=UnityEngine.Object.Instantiate(prefab.transform.Find("Visual").gameObject,slot);visual.name="Greybox "+kind;visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;visual.transform.localScale=scale;
        }
        static void Hide(Transform t){Require(t,"Expected backup missing.");if(!Application.isBatchMode)Undo.RecordObject(t.gameObject,"Hide old blockout");t.gameObject.SetActive(false);Record(t.gameObject);}
        static Transform Child(Transform p,string name){var t=new GameObject(name).transform;t.SetParent(p,false);return t;}
        static void Record(UnityEngine.Object obj){EditorUtility.SetDirty(obj);if(PrefabUtility.IsPartOfPrefabInstance(obj))PrefabUtility.RecordPrefabInstancePropertyModifications(obj);}
        static void Require(bool pass,string label){if(!pass)throw new InvalidOperationException(label);}
        static void Frame(Transform p,string name,Vector3 position,float yaw,float width,float height)
        {
            var root=Child(p,name);root.localPosition=position;root.localRotation=Quaternion.Euler(0,yaw,0);var v=Child(root,"Visual");
            var mat=AssetDatabase.LoadAssetAtPath<Material>(HouseholdPropCatalog.Folder+"/Materials/BathroomBlue.mat");
            foreach(var entry in new[]{(p:new Vector3(-width/2,0,0),s:new Vector3(.07f,height,.12f)),(p:new Vector3(width/2,0,0),s:new Vector3(.07f,height,.12f)),(p:new Vector3(0,height/2,0),s:new Vector3(width,.07f,.12f)),(p:new Vector3(0,-height/2,0),s:new Vector3(width,.07f,.12f))})
            {
                var bar=GameObject.CreatePrimitive(PrimitiveType.Cube);bar.name="Frame";UnityEngine.Object.DestroyImmediate(bar.GetComponent<Collider>());bar.transform.SetParent(v,false);bar.transform.localPosition=entry.p;bar.transform.localScale=entry.s;bar.GetComponent<Renderer>().sharedMaterial=mat;
            }
        }

        public static void BatchBuildWindows()
        {
            const string output="Builds/HouseholdGreybox/FireRescue.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{TutorialGreyboxSetup.ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText(Evidence+"/build.txt",report.summary.result+"\nErrors="+report.summary.totalErrors+" Warnings="+report.summary.totalWarnings+"\n"+output);EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
        }

        public static void BatchProbe()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(TutorialGreyboxSetup.ScenePath);
                var layout=GameObject.Find(UserLevelOneBuilder.LayoutName);
                string text="CURRENT_LAYOUT\n";
                foreach(var t in layout.GetComponentsInChildren<Transform>(true).Where(t=>t.parent && (t.parent.name=="Household Props" || t.parent.name=="Imported Architecture" || t.GetComponent<FluidSource>() || t.GetComponent<HoseWindow>() || t.GetComponent<FirefighterMotor>() || t.GetComponent<RescueNpc>() || t.GetComponent<ObjectFire>())))
                {
                    text+=t.name+" active="+t.gameObject.activeInHierarchy+" pos="+t.position.ToString("F3")+" scale="+t.lossyScale.ToString("F3");
                    foreach(var c in t.GetComponents<Collider>())text+=" collider="+c.bounds.ToString("F3");
                    text+="\n";
                }
                Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/layout-before.txt",text);
                RenderOverview("layout-before.png");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        public static void RenderOverview(string filename)
        {
            var camera=new GameObject("Temporary overhead review camera").AddComponent<Camera>();
            camera.transform.position=new Vector3(0,40,2);camera.transform.rotation=Quaternion.Euler(90,0,0);
            camera.orthographic=true;camera.orthographicSize=14;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.25f,.27f,.29f);
            Render(camera,filename,1280,1280);UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }
        public static void Render(Camera camera,string name,int width=1280,int height=720)
        {
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(width,height,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(Evidence+"/"+name,texture.EncodeToPNG());}
            finally{RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(texture);target.Release();UnityEngine.Object.DestroyImmediate(target);}
        }
    }
}
