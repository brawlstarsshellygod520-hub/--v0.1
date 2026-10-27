using UnityEngine;
namespace FireRescue
{
    public sealed class BurntFoodView:MonoBehaviour
    {
        [SerializeField] CarryItem item;
        [SerializeField] Renderer[] foodRenderers;
        MaterialPropertyBlock block;bool applied;
        public void Initialize(CarryItem food,Renderer[] targets){item=food;foodRenderers=targets;}
        void Awake()=>block=new MaterialPropertyBlock();
        void LateUpdate()
        {
            if(applied||!item||!item.IsBurnt)return;applied=true;
            foreach(var r in foodRenderers)if(r){r.GetPropertyBlock(block);block.SetColor("_Color",new Color(.055f,.035f,.025f));r.SetPropertyBlock(block);}
        }
    }
}
