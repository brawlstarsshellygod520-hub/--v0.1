using UnityEngine;

namespace FireRescue
{
    public sealed class PlayerCarryInteraction : MonoBehaviour
    {
        [SerializeField] Transform carryAnchor;
        [SerializeField] CarryItem[] items;
        [SerializeField] NpcItemRequest recipient;
        [SerializeField] FirefighterTools tools;
        [SerializeField] FirstReviewMission mission;
        [SerializeField] float reach = 1.55f;
        public CarryItem HeldItem { get; private set; }
        public bool IsCarrying => HeldItem;
        public CarryItem NearbyItem { get; private set; }
        public CarryItem NearbyUnsafeItem {get;private set;}
        public bool CanDeliver => HeldItem && recipient && !recipient.IsFulfilled && Reachable(recipient.transform.position + Vector3.up);
        public void Initialize(Transform anchor, CarryItem[] available, NpcItemRequest target, FirefighterTools toolOwner, FirstReviewMission owner)
        { carryAnchor=anchor; items=available; recipient=target; tools=toolOwner; mission=owner; }
        public void RefreshNearby()
        {
            NearbyItem=null;NearbyUnsafeItem=null; if (HeldItem || items==null) return;
            float best=reach*reach;
            foreach(var item in items)
            {
                if (!item || !item.isActiveAndEnabled || item.IsHeld || item.IsDelivered) continue;
                Vector3 d=item.transform.position-transform.position; d.y=0;
                if(d.sqrMagnitude>=best || !Reachable(item.transform.position)) continue;
                best=d.sqrMagnitude;
                if(item.IsSafeToTake){NearbyItem=item;NearbyUnsafeItem=null;}
                else{NearbyUnsafeItem=item;NearbyItem=null;}
            }
        }
        public bool Interact()
        {
            if (!isActiveAndEnabled || (mission && !mission.AllowsInput)) return false;
            if (HeldItem)
            {
                if(CanDeliver && recipient.TryAccept(HeldItem)){HeldItem=null; return true;}
                Vector3 p=transform.position+transform.forward*.6f;
                // A wall in front must not let E deposit an item in the next room.
                if(Physics.Linecast(transform.position+Vector3.up*.5f,p+Vector3.up*.5f,1<<8,QueryTriggerInteraction.Ignore)) return false;
                if(!Physics.Raycast(p+Vector3.up*2,Vector3.down,out var hit,4,1<<0,QueryTriggerInteraction.Ignore)) return false;
                HeldItem.Drop(hit.point+Vector3.up*.14f);HeldItem=null;return true;
            }
            RefreshNearby();
            if(!NearbyItem || !NearbyItem.Take(carryAnchor)) return false;
            HeldItem=NearbyItem;NearbyItem=null;if(tools)tools.Stow();return true;
        }
        public void DropOnDeath()
        {
            if(!HeldItem)return;
            HeldItem.Drop(transform.position+Vector3.up*.14f);HeldItem=null;
        }
        bool Reachable(Vector3 target)
        {
            Vector3 delta=target-transform.position;delta.y=0;
            return delta.sqrMagnitude<=reach*reach && !Physics.Linecast(transform.position+Vector3.up*1.15f,target,1<<8,QueryTriggerInteraction.Ignore);
        }
    }
}
