using UnityEngine;
namespace FireRescue
{
    public sealed class NpcRequestNameTag:MonoBehaviour
    {
        [SerializeField] NpcItemRequest request;
        [SerializeField] FirstReviewMission mission;
        [SerializeField] Camera view;
        [SerializeField] float headHeight=1.65f;
        Font font;GUIStyle style;
        public string DisplayText=>request&&request.IsPoisoned?"好難吃...":request&&request.IsFulfilled?"謝謝！吃到布丁了。":"好想吃布丁...";
        public void Initialize(NpcItemRequest target,FirstReviewMission state,Camera camera){request=target;mission=state;view=camera;}
        void Awake(){font=Font.CreateDynamicFontFromOSFont("Microsoft JhengHei",18);}
        void OnGUI()
        {
            if(!request||!view)return;Vector3 p=view.WorldToScreenPoint(transform.position+Vector3.up*headHeight);
            if(p.z<=0||p.x<-110||p.x>Screen.width+110||p.y<0||p.y>Screen.height+80)return;
            if(style==null)style=new GUIStyle(GUI.skin.label){font=font,fontSize=18,alignment=TextAnchor.MiddleCenter,richText=true,normal={textColor=Color.white}};
            float scale=Screen.height/720f;style.fontSize=Mathf.Max(12,Mathf.RoundToInt(18*scale));
            string plain=DisplayText;Vector2 size=style.CalcSize(new GUIContent(plain));
            float width=Mathf.Clamp(size.x+26*scale,142*scale,290*scale),height=38*scale;
            var rect=new Rect(p.x-width*.5f,Screen.height-p.y-height,width,height);
            // How-to-Fish-style in-world thought: thin white frame, translucent light
            // fill, white text and a warm highlight on the requested item, no HUD header.
            var old=GUI.color;GUI.color=new Color(.93f,.94f,.93f,.12f);GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=new Color(1,1,1,.9f);float line=1.15f*scale;
            GUI.DrawTexture(new Rect(rect.x,rect.y,rect.width,line),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x,rect.yMax-line,rect.width,line),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x,rect.y,line,rect.height),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax-line,rect.y,line,rect.height),Texture2D.whiteTexture);
            GUI.color=new Color(0,0,0,.45f);GUI.Label(new Rect(rect.x+scale,rect.y+scale,rect.width,rect.height),plain,style);
            GUI.color=old;GUI.Label(rect,plain.Replace("布丁","<color=#FFE36B>布丁</color>"),style);
        }
        void OnDestroy(){if(font)Destroy(font);}
    }
}
