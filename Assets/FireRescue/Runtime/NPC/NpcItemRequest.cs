using UnityEngine;

namespace FireRescue
{
    // Scene-specific requirement, not a new calming mechanic. Other NPCs need no component.
    public sealed class NpcItemRequest : MonoBehaviour
    {
        [SerializeField] string wantedItemId = "pudding";
        [SerializeField] Transform receivingHand;
        [SerializeField] BreakableDoor bedroomDoor;
        [SerializeField] ObjectFire cookingFire;
        [SerializeField] FirstReviewMission mission;
        public bool IsFulfilled { get; private set; }
        public bool IsPoisoned {get;private set;}
        public bool IsDead {get;private set;}
        public bool AutomaticTick {get;set;}=true;
        float poisonAge;
        public bool CanLeave => IsFulfilled && !IsPoisoned && !IsDead && bedroomDoor && bedroomDoor.IsBroken && cookingFire && !cookingFire.IsBurning;
        public Transform ReceivingHand => receivingHand;
        public void Initialize(Transform hand, BreakableDoor door, ObjectFire fire, FirstReviewMission owner)
        { receivingHand = hand; bedroomDoor = door; cookingFire = fire; mission = owner; }
        public bool TryAccept(CarryItem item)
        {
            if (!isActiveAndEnabled || IsFulfilled || !item || item.ItemId != wantedItemId ||
                !bedroomDoor || !bedroomDoor.IsBroken || !mission || !mission.CanDeliver) return false;
            if (!item.Deliver(receivingHand)) return false;
            IsFulfilled = true;
            if(item.IsBurnt){IsPoisoned=true;poisonAge=0;mission.NotifyPoisoned();}
            else mission.NotifyDelivered();
            return true;
        }
        void Update(){if(AutomaticTick)Tick(Time.deltaTime);}
        public void Tick(float dt)
        {
            if(!IsPoisoned||IsDead||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            poisonAge+=dt;if(poisonAge<1.4f)return;
            IsDead=true;if(mission)mission.Fail(FirstReviewMission.Failure.ResidentPoisoned);
        }
    }
}
