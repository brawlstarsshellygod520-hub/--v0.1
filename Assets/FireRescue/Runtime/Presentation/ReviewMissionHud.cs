using UnityEngine;

namespace FireRescue
{
    public sealed class ReviewMissionHud : MonoBehaviour
    {
        [SerializeField] FirstReviewMission mission;
        [SerializeField] LocalPlayerInput input;
        [SerializeField] PlayerCarryInteraction carry;
        [SerializeField] HoseConnection hose;
        Font font;GUIStyle timer,text,restart;
        public bool CanRestart=>mission&&mission.CurrentStatus==FirstReviewMission.Status.Failed;
        public bool RestartAfterFailure()
        {
            if(!CanRestart)return false;
            mission.RestartRound();return true;
        }
        public void Initialize(FirstReviewMission state,LocalPlayerInput controls,PlayerCarryInteraction items,HoseConnection connection)
        {mission=state;input=controls;carry=items;hose=connection;}
        void Awake(){font=Font.CreateDynamicFontFromOSFont("Microsoft JhengHei",22);}
        void OnDestroy(){if(font)Destroy(font);}
        void OnGUI()
        {
            if(!mission)return;
            if(timer==null)
            {
                text=new GUIStyle(GUI.skin.label){font=font,fontSize=20,normal={textColor=Color.white}};
                timer=new GUIStyle(text){fontSize=36,alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
                restart=new GUIStyle(GUI.skin.button){font=font,fontSize=24};
            }
            var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            int seconds=Mathf.CeilToInt(mission.Remaining);
            Box(new Rect(550,12,180,64));GUI.Label(new Rect(550,12,180,60),$"{seconds/60:00}:{seconds%60:00}",timer);
            if(mission.CurrentStatus==FirstReviewMission.Status.Won || mission.CurrentStatus==FirstReviewMission.Status.Failed)
            {
                Box(new Rect(360,260,560,CanRestart?180:100));GUI.Label(new Rect(380,275,520,65),mission.CurrentStatus==FirstReviewMission.Status.Won?"第一關完成！":"失敗",timer);
                if(CanRestart&&GUI.Button(new Rect(500,360,280,52),"重新開始",restart))RestartAfterFailure();
            }
            GUI.matrix=old;
        }
        static void Box(Rect r){Color old=GUI.color;GUI.color=new Color(.07f,.10f,.14f,.88f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    }
}
