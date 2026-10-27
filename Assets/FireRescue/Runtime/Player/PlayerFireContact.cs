using UnityEngine;

namespace FireRescue
{
    [DefaultExecutionOrder(90)]
    public sealed class PlayerFireContact : MonoBehaviour
    {
        [SerializeField] FireField field;
        [SerializeField] FirstReviewMission mission;
        [SerializeField] PlayerCarryInteraction carry;
        Vector3 previous,query;
        System.Predicate<Vector3> canReach;
        public bool IsDead {get;private set;}
        public bool AutomaticTick {get;set;}=true;
        public void Initialize(FireField floor,FirstReviewMission state,PlayerCarryInteraction items){field=floor;mission=state;carry=items;previous=transform.position;}
        void OnEnable()=>previous=transform.position;
        void Awake()=>canReach=ReachesFire;
        bool ReachesFire(Vector3 center)
        {center.y=field.Origin.y+.16f;return !Physics.Linecast(query,center,1<<8,QueryTriggerInteraction.Ignore);}
        void LateUpdate(){if(AutomaticTick)Evaluate();}
        public void Evaluate()
        {
            if(IsDead||!mission||!mission.AllowsInput){previous=transform.position;return;}
            Vector3 now=transform.position;int samples=Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(previous,now)/.1f),1,256);
            for(int i=0;i<=samples;i++)
            {
                query=Vector3.Lerp(previous,now,i/(float)samples);if(field)query.y=field.Origin.y+.16f;
                if(field&&field.IsBurningAt(query,.16f,canReach))
                {IsDead=true;if(carry)carry.DropOnDeath();mission.Fail(FirstReviewMission.Failure.PlayerBurned);break;}
            }
            previous=now;
        }
    }
}
