using UnityEngine;
namespace FireRescue
{
    // Only the replaceable visual falls over. CharacterController and actor transform stay valid.
    [DefaultExecutionOrder(120)]
    public sealed class ActorDefeatView:MonoBehaviour
    {
        [SerializeField] Transform visual;
        [SerializeField] PlayerFireContact player;
        [SerializeField] NpcItemRequest resident;
        Quaternion rest;Vector3 restPosition;float amount;
        public void Initialize(Transform target,PlayerFireContact p,NpcItemRequest n){visual=target;player=p;resident=n;}
        void Awake(){if(visual){rest=visual.localRotation;restPosition=visual.localPosition;}}
        void LateUpdate()
        {
            bool dead=(player&&player.IsDead)||(resident&&resident.IsDead);
            amount=Mathf.MoveTowards(amount,dead?1:0,Time.deltaTime*3);
            if(visual&&amount>0){visual.localRotation=rest*Quaternion.Euler(0,0,82*amount);visual.localPosition=restPosition+Vector3.up*(.35f*amount);}
        }
    }
}
