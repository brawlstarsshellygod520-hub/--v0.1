using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    public static class ReviewFireLoopV2Setup
    {
        public const string Evidence="Docs/Validation/2026-10-28-fire-loop-v2";
        public static void BatchApply()
        {
            try
            {
                Directory.CreateDirectory(Evidence);var scene=EditorSceneManager.OpenScene(FirstReview1027Setup.ScenePath);
                var root=GameObject.Find("First Review 1027");var player=root.GetComponentInChildren<LocalPlayerInput>();
                player.transform.SetPositionAndRotation(new Vector3(2.6f,.1f,-3.7f),Quaternion.LookRotation(Vector3.forward));
                var field=root.GetComponentInChildren<FireField>();field.SetIgnitionSeeds(Array.Empty<Vector2Int>());
                var nonflammable=new List<Vector2Int>();
                Physics.SyncTransforms();
                for(int z=0;z<field.Height;z++)for(int x=0;x<field.Width;x++)
                {
                    var p=field.CellCenter(x,z);
                    bool kitchen=p.x>-5.23f&&p.x<-2.3f&&p.z>-5.2f&&p.z<-.97f;
                    bool blocked=Physics.CheckSphere(p+Vector3.up*.16f,.12f,1<<8,QueryTriggerInteraction.Ignore);
                    if(!kitchen||blocked)nonflammable.Add(new Vector2Int(x,z));
                }
                field.SetNonFlammableCells(nonflammable.ToArray());
                Set(field,"fuelBurnRate",.001f);Set(field,"naturalIntensityLoss",.002f);Set(field,"fireSpreadRate",.4f);
                var fire=root.GetComponentInChildren<ObjectFire>();var area=fire.GetComponent<CookingOilArea>();if(area)area.enabled=false;
                var food=root.GetComponentInChildren<CarryItem>();food.SetFireHazard(null);food.SetFloorHazard(field);
                var incident=Get<CookingOilIncident>(fire.gameObject);incident.Initialize(fire,field,food);
                Set(incident,"fastSpread",.4f);Set(incident,"slowSpread",.035f);
                var fireView=fire.GetComponent<OilPanFireView>();fireView.SetArea(null);fireView.SetIncident(incident);
                var foodView=Get<BurntFoodView>(food.gameObject);foodView.Initialize(food,food.GetComponentsInChildren<Renderer>().Where(r=>r.name=="布丁"||r.name=="黑糖").ToArray());
                var mission=root.GetComponentInChildren<FirstReviewMission>();var m=new SerializedObject(mission);m.FindProperty("requireAllFiresOut").boolValue=false;m.ApplyModifiedPropertiesWithoutUndo();
                var contact=Get<PlayerFireContact>(player.gameObject);contact.Initialize(field,mission,player.GetComponent<PlayerCarryInteraction>());
                Get<ActorDefeatView>(player.gameObject).Initialize(player.transform.Find("Visual"),contact,null);
                var request=root.GetComponentInChildren<NpcItemRequest>();Get<ActorDefeatView>(request.gameObject).Initialize(request.transform.Find("Visual"),null,request);
                var effects=root.GetComponentInChildren<FirstRescueEffects>();Set(effects,"fireCapacity",512);
                foreach(var source in root.GetComponentsInChildren<FluidSource>())
                {
                    var profile=source.Profile;var settings=profile.Settings;
                    settings.LaunchAngle=18;settings.MuzzleSpeed=source.Element==ElementKind.Water?7:6;
                    settings.Gravity=source.Element==ElementKind.Water?16:8;settings.AirLifetime=1.5f;
                    profile.Configure(settings);EditorUtility.SetDirty(profile);
                }
                AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
                File.WriteAllText(Evidence+"/install.txt","FIRE_LOOP_V2_INSTALLED\nIndoor spawn; independent ordinary floor fire; fast then slow propagation; water/oil burst and scorched food; actor defeat; HUD hints removed; stable hose particles.\nHouse/models/camera/15m unchanged.\n");
                EditorApplication.Exit(0);
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static T Get<T>(GameObject go)where T:Component=>go.GetComponent<T>()??go.AddComponent<T>();
        static void Set(UnityEngine.Object target,string property,float value){var so=new SerializedObject(target);var p=so.FindProperty(property);if(p.propertyType==SerializedPropertyType.Integer)p.intValue=(int)value;else p.floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    }
}
