using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FireRescue.Editor
{
    [InitializeOnLoad]
    public static class KitchenIterationValidation
    {
        public const string Evidence="Docs/Validation/2026-10-28-kitchen-hints";
        const string Key="Kitchen.Iteration.Before";
        static KitchenIterationValidation(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);new GameObject("Temporary kitchen baseline").AddComponent<KitchenBaselineDriver>();}};}
        public static void BatchBefore(){Directory.CreateDirectory(Evidence);EditorSceneManager.OpenScene(FirstReview1027Setup.ScenePath);SessionState.SetBool(Key,true);EditorApplication.EnterPlaymode();}
    }
    public sealed class KitchenBaselineDriver:MonoBehaviour
    {
        IEnumerator Start()
        {
            var lines=new List<string>();
            var input=FindAnyObjectByType<LocalPlayerInput>();input.ReadDevices=false;
            var floor=FindAnyObjectByType<FireField>();floor.Simulate=false;
            var incident=FindAnyObjectByType<CookingOilIncident>();incident.AutomaticTick=false;
            var mission=FindAnyObjectByType<FirstReviewMission>();mission.AutomaticTick=false;
            var npc=FindAnyObjectByType<RescueNpc>();npc.Autonomous=false;
            var pot=FindAnyObjectByType<ObjectFire>();var reaction=FindAnyObjectByType<ElementReaction>();
            var near=new Vector3(-5,.1f,-3.5f);floor.Ignite(near,1);
            reaction.ApplySpray(ElementKind.DryPowder,pot.transform.position,Vector3.left,4,pot.GetComponent<Collider>(),1.1f,5);
            lines.Add("Powder dose4 on pot: pot="+pot.Intensity+" neighboring floor="+(floor.TryGetCell(near,out var cell)?floor.GetCell(cell.x,cell.y).Intensity:-1));
            reaction.EnablePowderSurfaceSplash(false);pot.Configure(FireKind.CookingOil,1);floor.IgniteFromHeat(near,1);
            reaction.ApplySpray(ElementKind.DryPowder,pot.transform.position,Vector3.left,4,pot.GetComponent<Collider>(),1.1f,5);
            lines.Add("ISOLATED OLD reaction early-return: pot="+pot.Intensity+" neighboring floor="+floor.GetCell(cell.x,cell.y).Intensity);
            floor.ApplyTimedWater(Vector3.zero,100,30,60);pot.Receive(ElementKind.DryPowder,10);
            FindAnyObjectByType<BreakableDoor>().ReceiveAxeHit(3);
            var food=FindAnyObjectByType<CarryItem>();food.SetFloorHazard(null);food.Take(input.transform.Find("Carry Anchor"));npc.GetComponent<NpcItemRequest>().TryAccept(food);
            Physics.SyncTransforms();npc.Tick(0);
            var path=(List<Vector2Int>)typeof(RescueNpc).GetField("path",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(npc);
            lines.Add("HasPath="+npc.HasSafePath+" points="+path.Count);
            lines.AddRange(path.Select(p=>"ROUTE "+floor.CellCenter(p.x,p.y).ToString("F3")));
            npc.GetComponent<CharacterController>().stepOffset=0;
            GameObject.Find("First Review 1027/Prototype Courtyard Boundary").SetActive(false);
            npc.Initialize(floor,GameObject.Find("First Review 1027/Rescue Exit").transform,new[]{pot});npc.Tick(0);
            lines.Add("ISOLATED OLD floor-grazing query (fences off): route points="+path.Count);
            for(float z=-5.25f;z< -4.65f;z+=.125f)
            {
                var feet=new Vector3(5.5f,.1f,z);var a=feet+Vector3.up*.315f;var b=feet+Vector3.up*(1.5f-.315f);
                bool blocked=Physics.CapsuleCast(a,b,.315f,Vector3.right,out var hit,1.6f,1<<8,QueryTriggerInteraction.Ignore);
                bool raised=Physics.CapsuleCast(a+Vector3.up*.025f,b+Vector3.up*.025f,.315f,Vector3.right,out _,1.6f,1<<8,QueryTriggerInteraction.Ignore);
                lines.Add("FRONT z="+z+" blocked="+blocked+" collider="+(blocked?hit.collider.name:"none")+" raised25mmBlocked="+raised);
            }
            yield return null;
            File.WriteAllLines(KitchenIterationValidation.Evidence+"/reaction-route-probe.txt",lines);EditorApplication.Exit(0);
        }
    }
}
