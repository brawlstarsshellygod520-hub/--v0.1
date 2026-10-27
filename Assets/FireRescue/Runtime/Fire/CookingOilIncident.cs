using UnityEngine;

namespace FireRescue
{
    // Oil pot and ordinary floor fire have separate state. No renderer participates in reactions.
    public sealed class CookingOilIncident : MonoBehaviour
    {
        [SerializeField] ObjectFire pot;
        [SerializeField] FireField floor;
        [SerializeField] CarryItem pudding;
        [SerializeField] Vector3 spillStart=new Vector3(-5.05f,.1f,-3.46f);
        [SerializeField] float fastSeconds=5,fastSpread=.4f,slowSpread=.035f;
        float age,waterDose;
        bool subscribed;
        int[] waveCells,waveDepth;
        int waveCount,waveCursor,maxDepth;
        float[] coldSeconds;
        [SerializeField] float potHeatRadius=1.55f,relightDelay=.28f;
        public int FirstWaveTargetCount=>waveCount;
        public int FirstWaveProcessedCount=>waveCursor;
        public bool AutomaticTick {get;set;}=true;
        public int ExplosionCount {get;private set;}
        public float ExplosionAge {get;private set;}=100;
        public void Initialize(ObjectFire source,FireField field,CarryItem food)
        {Unsubscribe();pot=source;floor=field;pudding=food;waveCells=null;Subscribe();}
        void OnEnable()=>Subscribe();
        void OnDisable()=>Unsubscribe();
        void Subscribe(){if(!subscribed&&pot){pot.OilContactedByWater+=OnWater;subscribed=true;}}
        void Unsubscribe(){if(subscribed&&pot)pot.OilContactedByWater-=OnWater;subscribed=false;}
        void Update(){if(AutomaticTick)Tick(Time.deltaTime);}
        public void Tick(float dt)
        {
            if(dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            age+=dt;ExplosionAge+=dt;
            if(!floor)return;
            floor.SetSpreadRate(age<fastSeconds?fastSpread:slowSpread);
            if(waveCells==null)BuildFirstWave();
            if(!pot||!pot.IsBurning)return;
            // A connected flashover front, scheduled by flood distance rather than a
            // huge spread multiplier. It reaches all kitchen fuel by the authored deadline.
            while(waveCursor<waveCount)
            {
                float arrival=.12f+(Mathf.Max(.2f,fastSeconds)-.12f)*waveDepth[waveCursor]/Mathf.Max(1,maxDepth);
                if(age+.0001f<arrival)break;
                int cell=waveCells[waveCursor++];floor.Ignite(floor.CellCenter(cell%floor.Width,cell/floor.Width),.95f);
            }
            if(age>=fastSeconds)for(int i=0;i<waveCount;i++)
            {
                int cell=waveCells[i],x=cell%floor.Width,z=cell/floor.Width;var point=floor.CellCenter(x,z);
                Vector3 delta=point-pot.transform.position;delta.y=0;if(delta.sqrMagnitude>potHeatRadius*potHeatRadius)continue;
                if(floor.GetCell(x,z).IsBurning){coldSeconds[i]=0;continue;}
                coldSeconds[i]+=dt;
                if(coldSeconds[i]>=relightDelay){floor.IgniteFromHeat(point);coldSeconds[i]=0;}
            }
        }
        void BuildFirstWave()
        {
            int size=floor.Width*floor.Height;waveCells=new int[size];waveDepth=new int[size];coldSeconds=new float[size];var visited=new bool[size];
            waveCount=waveCursor=maxDepth=0;
            if(!floor.TryGetCell(spillStart,out var start)||floor.GetCell(start.x,start.y).Fuel<=0)return;
            waveCells[waveCount++]=start.y*floor.Width+start.x;visited[waveCells[0]]=true;
            for(int i=0;i<waveCount;i++)
            {
                int x=waveCells[i]%floor.Width,z=waveCells[i]/floor.Width;
                Add(x-1,z,waveDepth[i]+1);Add(x+1,z,waveDepth[i]+1);Add(x,z-1,waveDepth[i]+1);Add(x,z+1,waveDepth[i]+1);
            }
            void Add(int x,int z,int depth)
            {
                if(x<0||z<0||x>=floor.Width||z>=floor.Height)return;int index=z*floor.Width+x;
                if(visited[index]||floor.GetCell(x,z).Fuel<=0)return;
                visited[index]=true;waveCells[waveCount]=index;waveDepth[waveCount++]=depth;maxDepth=Mathf.Max(maxDepth,depth);
            }
        }
        void OnWater(float amount)
        {
            waterDose+=amount;if(ExplosionCount!=0||waterDose<.08f)return;
            ExplosionCount++;ExplosionAge=0;
            if(pudding)
            {
                Vector3 d=pudding.transform.position-transform.position;d.y=0;
                if(d.sqrMagnitude<1.5f*1.5f)pudding.Scorch();
            }
            // Burst ignites a local footprint, not the whole kitchen. Wet cleared cells resist ignition.
            if(floor)for(int z=-2;z<=2;z++)for(int x=-2;x<=2;x++)
                if(x*x+z*z<=4)floor.Ignite(spillStart+new Vector3(x*floor.CellSize,0,z*floor.CellSize),1);
        }
    }
}
