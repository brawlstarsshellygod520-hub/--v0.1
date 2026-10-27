using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace FireRescue.Editor
{
    public static class KitchenIterationSetup
    {
        public static void BatchApply()
        {
            try
            {
                var scene=EditorSceneManager.OpenScene(FirstReview1027Setup.ScenePath);var root=GameObject.Find("First Review 1027");
                var input=root.GetComponentInChildren<LocalPlayerInput>();var hose=input.GetComponent<HoseConnection>();
                Set(hose,"maximumLength",10);
                root.GetComponentInChildren<ElementReaction>().EnablePowderSurfaceSplash(true);
                root.GetComponentInChildren<FireField>().EnablePowderFirebreak(true);
                Set(root.GetComponentInChildren<CookingOilIncident>(),"fastSeconds",5);
                var soda=root.GetComponentsInChildren<FluidSource>().Single(s=>s.Element==ElementKind.DryPowder).Profile;
                var settings=soda.Settings;settings.ImpactRadius=1.35f;settings.MinimumSpeedFraction=.3f;settings.GroundLifetime=20;soda.Configure(settings);
                var effects=soda.Effects;effects.ExtinguishMultiplier=.65f;soda.ConfigureEffects(effects);EditorUtility.SetDirty(soda);
                var water=root.GetComponentsInChildren<FluidSource>().Single(s=>s.Element==ElementKind.Water).Profile;
                var waterSettings=water.Settings;waterSettings.MinimumSpeedFraction=.2f;waterSettings.ImpactRadius=.9f;water.Configure(waterSettings);EditorUtility.SetDirty(water);
                var feedback=input.GetComponent<PickupContextView>();if(!feedback)feedback=input.gameObject.AddComponent<PickupContextView>();
                feedback.Initialize(input.GetComponent<PlayerCarryInteraction>(),hose,root.GetComponentInChildren<FirstReviewMission>(),root.GetComponentInChildren<Camera>(),Shader.Find("FireRescue/InteractionOutline"));
                // Authorized greybox courtyard boundary. User geometry stays untouched.
                var boundary=root.transform.Find("Prototype Courtyard Boundary");
                if(!boundary){boundary=new GameObject("Prototype Courtyard Boundary").transform;boundary.SetParent(root.transform,false);}
                string folder="Assets/FireRescue/Art/AIGenerated/ReviewBoundaries";
                if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/FireRescue/Art/AIGenerated","ReviewBoundaries");
                var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"/Courtyard plaster.mat");
                if(!material){material=new Material(Shader.Find("FireRescue/MattePlaster")){name="Courtyard plaster",color=new Color(.58f,.60f,.55f)};AssetDatabase.CreateAsset(material,folder+"/Courtyard plaster.mat");}
                Wall(boundary,"West boundary",new Vector3(-6.52f,.7f,2.72f),new Vector3(.24f,1.2f,7.38f),material);
                Wall(boundary,"North boundary",new Vector3(-4.36f,.7f,6.29f),new Vector3(4.56f,1.2f,.24f),material);
                Wall(boundary,"East boundary",new Vector3(-2.2f,.7f,2.72f),new Vector3(.24f,1.2f,7.38f),material);
                var geometry=root.GetComponent<SprayWallOcclusion>();var serialized=new SerializedObject(geometry);var list=serialized.FindProperty("walls");
                var walls=Enumerable.Range(0,list.arraySize).Select(i=>list.GetArrayElementAtIndex(i).objectReferenceValue as Collider).Where(c=>c).Concat(boundary.GetComponentsInChildren<Collider>()).Distinct().ToArray();
                geometry.Configure(walls,Array.Empty<HoseWindow>());
                Physics.SyncTransforms();AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");
                File.WriteAllText(KitchenIterationValidation.Evidence+"/install.txt","KITCHEN_ITERATION_INSTALLED\n5s connected flashover; pot heat relights neighboring floor; all-fire powder surface splash at .65 strength; front sill navigation tolerance; pickup outline/E; length warning; 10m hose; authorized courtyard fences.\n");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Set(UnityEngine.Object o,string field,float value){var s=new SerializedObject(o);s.FindProperty(field).floatValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        static void Wall(Transform parent,string name,Vector3 position,Vector3 size,Material material)
        {
            var t=parent.Find(name);if(!t){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;t=go.transform;t.SetParent(parent,false);}
            t.gameObject.layer=8;t.position=position;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=material;
        }
    }
}
