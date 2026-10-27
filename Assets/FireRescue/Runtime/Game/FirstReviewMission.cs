using UnityEngine;

namespace FireRescue
{
    // Authoritative demonstration state; no UI, particles or animation write these rules.
    public sealed class FirstReviewMission : MonoBehaviour
    {
        public enum Status { Active, Evacuating, Won, Failed, Poisoning }
        public enum Failure { None, TimeExpired, PlayerBurned, ResidentPoisoned }
        [SerializeField] float deadlineSeconds=60;
        [SerializeField] NpcItemRequest request;
        [SerializeField] RescueGameManager manager;
        [SerializeField] ObjectFire oilFire;
        [SerializeField] FirefighterTools tools;
        [SerializeField] RescueNpc npc;
        [SerializeField] bool requireAllFiresOut=true;
        public bool AutomaticTick { get; set; }=true;
        public float Remaining { get; private set; }=60;
        public Status CurrentStatus { get; private set; }
        public bool CanDeliver => CurrentStatus==Status.Active && Remaining>0;
        public bool AllowsInput => CurrentStatus==Status.Active || CurrentStatus==Status.Evacuating;
        public int CompletionCount { get; private set; }
        public Failure FailureCause {get;private set;}
        public ObjectFire OilFire => oilFire;
        public NpcItemRequest Request => request;
        public void Initialize(NpcItemRequest target, RescueGameManager game, ObjectFire fire, FirefighterTools toolOwner, RescueNpc resident)
        { request=target;manager=game;oilFire=fire;tools=toolOwner;npc=resident; }
        void Awake(){Remaining=Mathf.Max(1,deadlineSeconds);CurrentStatus=Status.Active;}
        void Update(){if(AutomaticTick)Tick(Time.deltaTime);}
        public void Tick(float dt)
        {
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0) return;
            if(CurrentStatus==Status.Active)
            {
                Remaining=Mathf.Max(0,Remaining-dt);
                if(Remaining<=0)Fail(Failure.TimeExpired);
            }
            if(CurrentStatus==Status.Evacuating && request && request.CanLeave && manager &&
                (requireAllFiresOut?manager.LevelComplete:manager.RescueComplete))
            { CurrentStatus=Status.Won;CompletionCount++;if(tools)tools.Stow(); }
        }
        public void NotifyDelivered(){if(CanDeliver)CurrentStatus=Status.Evacuating;}
        public void RestartRound()=>UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        public void NotifyPoisoned(){if(CanDeliver){CurrentStatus=Status.Poisoning;if(tools)tools.Stow();if(npc)npc.Autonomous=false;}}
        public void Fail(Failure reason)
        {
            if(CurrentStatus==Status.Failed||CurrentStatus==Status.Won)return;
            CurrentStatus=Status.Failed;FailureCause=reason;
            if(tools)tools.Stow();if(npc)npc.Autonomous=false;
        }
    }
}
