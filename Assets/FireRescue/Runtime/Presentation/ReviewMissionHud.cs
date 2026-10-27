using UnityEngine;

namespace FireRescue
{
    public sealed class ReviewMissionHud : MonoBehaviour
    {
        [SerializeField] FirstReviewMission mission;
        [SerializeField] LocalPlayerInput input;
        [SerializeField] PlayerCarryInteraction carry;
        [SerializeField] HoseConnection hose;
        Font font;GUIStyle timer,text,hint,result;
        public const string RestartHint="按 R 即可重來";
        public bool IsRestartHintVisible=>mission&&mission.CurrentStatus==FirstReviewMission.Status.Failed;
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
                result=new GUIStyle(timer){fontSize=24};
                hint=new GUIStyle(timer){fontSize=16,fontStyle=FontStyle.Normal,normal={textColor=new Color(.85f,.88f,.91f)}};
            }
            var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            int seconds=Mathf.CeilToInt(mission.Remaining);
            Box(new Rect(550,12,180,64));GUI.Label(new Rect(550,12,180,60),$"{seconds/60:00}:{seconds%60:00}",timer);
            if(mission.CurrentStatus==FirstReviewMission.Status.Won)
            {
                Box(new Rect(360,260,560,100));GUI.Label(new Rect(380,275,520,65),"第一關完成！",timer);
            }
            else if(IsRestartHintVisible)
            {
                // Small text only: no clickable button or large restart dialog.
                ShadowLabel(new Rect(500,305,280,36),"失敗",result);
                ShadowLabel(new Rect(500,342,280,26),RestartHint,hint);
            }
            GUI.matrix=old;
        }
        static void ShadowLabel(Rect rect,string value,GUIStyle style)
        {
            var color=GUI.color;GUI.color=new Color(0,0,0,.85f);
            GUI.Label(new Rect(rect.x+1,rect.y+1,rect.width,rect.height),value,style);
            GUI.color=color;GUI.Label(rect,value,style);
        }
        static void Box(Rect r){Color old=GUI.color;GUI.color=new Color(.07f,.10f,.14f,.88f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    }
}
