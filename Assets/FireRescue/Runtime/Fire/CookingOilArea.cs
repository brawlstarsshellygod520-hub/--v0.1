using UnityEngine;

namespace FireRescue
{
    /// <summary>A connected cooking-oil spill: one fire authority, a growing bounded hazard footprint.</summary>
    public sealed class CookingOilArea:MonoBehaviour
    {
        [SerializeField] ObjectFire source;
        [SerializeField] SprayWallOcclusion walls;
        [SerializeField] Vector2 minimum=new Vector2(-6.25f,-5.8f),maximum=new Vector2(-2.18f,-.92f);
        [SerializeField] float fullGrowthSeconds=2,maximumRadius=3.6f;
        float elapsed;
        public bool AutomaticTick {get;set;}=true;
        public float Radius => source&&source.IsBurning?Mathf.Lerp(.55f,maximumRadius,Mathf.Clamp01(elapsed/Mathf.Max(.1f,fullGrowthSeconds)))*Mathf.Sqrt(Mathf.Max(1,source.Intensity)):0;
        public Vector2 Minimum=>minimum;
        public Vector2 Maximum=>maximum;
        public void Initialize(ObjectFire fire,SprayWallOcclusion geometry){source=fire;walls=geometry;elapsed=0;}
        void Update(){if(AutomaticTick)Tick(Time.deltaTime);}
        public void Tick(float dt){if(dt>0&&!float.IsNaN(dt)&&!float.IsInfinity(dt))elapsed+=dt;}
        public bool IsHot(Vector3 point)
        {
            if(!isActiveAndEnabled||!source||!source.IsBurning||point.x<minimum.x||point.x>maximum.x||point.z<minimum.y||point.z>maximum.y)return false;
            Vector3 delta=point-source.transform.position;delta.y=0;
            return delta.sqrMagnitude<=Radius*Radius&&(!walls||!walls.TryBlock(source.transform.position,point,out _,out _));
        }
    }
}
