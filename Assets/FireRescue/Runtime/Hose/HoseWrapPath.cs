using UnityEngine;

namespace FireRescue
{
    // Contact polyline, not a global shortest-path search: new bends come only from the
    // player's previous clear endpoint. Thus the hose follows the door actually used.
    internal sealed class HoseWrapPath
    {
        public const int Capacity=24;
        public const float Radius=.06f;
        readonly Vector3[] bends=new Vector3[Capacity];
        readonly Vector3[] trial=new Vector3[Capacity];
        int count,trialCount;
        public int Count=>count;
        public Vector3 this[int i]=>bends[i];
        public void Clear(){count=trialCount=0;}
        public void Commit(){count=trialCount;for(int i=0;i<count;i++)bends[i]=trial[i];}
        public float TrialLength(Vector3 start,Vector3 end)
        {float length=0;for(int i=0;i<trialCount;i++){length+=Planar(start,trial[i]);start=trial[i];}return length+Planar(start,end);}
        public bool Build(SprayWallOcclusion geometry,Vector3 start,Vector3 previousEnd,Vector3 end)
        {
            trialCount=count;for(int i=0;i<count;i++)trial[i]=bends[i];
            Simplify(geometry,start,end);
            Vector3 anchor=trialCount>0?trial[trialCount-1]:start;
            if(geometry.CableBlocked(anchor,end,Radius))
            {
                if(trialCount==Capacity || Planar(anchor,previousEnd)<.015f ||
                    geometry.CableBlocked(anchor,previousEnd,Radius)||geometry.CableBlocked(previousEnd,end,Radius))return false;
                // Only record swept endpoint history, never jump to a different doorway.
                trial[trialCount++]=previousEnd;
            }
            for(int i=0;i<trialCount;i++)
            {
                Vector3 a=i==0?start:trial[i-1], b=trial[i], c=i+1==trialCount?end:trial[i+1];
                Vector3 ac=c-a;ac.y=0;float t=ac.sqrMagnitude<.00001f?0:Mathf.Clamp01(Vector3.Dot(b-a,ac)/ac.sqrMagnitude);
                Vector3 target=Vector3.Lerp(a,c,t);target.y=geometry.CableBendHeight;
                float lo=0,hi=1;
                for(int j=0;j<7;j++)
                {
                    float middle=(lo+hi)*.5f;Vector3 p=Vector3.Lerp(b,target,middle);
                    if(!geometry.CableBlocked(a,p,Radius)&&!geometry.CableBlocked(p,c,Radius)&&
                        !geometry.CableBlocked(b,p,Radius)&&geometry.CableSweepClear(a,b,p)&&geometry.CableSweepClear(b,c,p))lo=middle;else hi=middle;
                }
                trial[i]=Vector3.Lerp(b,target,lo);
            }
            Simplify(geometry,start,end);
            anchor=start;
            for(int i=0;i<trialCount;i++){if(geometry.CableBlocked(anchor,trial[i],Radius))return false;anchor=trial[i];}
            return !geometry.CableBlocked(anchor,end,Radius);
        }
        void Simplify(SprayWallOcclusion geometry,Vector3 start,Vector3 end)
        {
            for(int i=trialCount-1;i>=0;i--)
            {
                Vector3 a=i==0?start:trial[i-1],b=trial[i],c=i+1==trialCount?end:trial[i+1];
                if(geometry.CableBlocked(a,c,Radius)||!geometry.CableSweepClear(a,b,c))continue;
                for(int j=i;j<trialCount-1;j++)trial[j]=trial[j+1];trialCount--;
            }
        }
        public static float Planar(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
    }
}
