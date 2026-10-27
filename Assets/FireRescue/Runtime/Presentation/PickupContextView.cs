using UnityEngine;
namespace FireRescue
{
    // Reads the very same pickup candidate as E. No duplicate range/LOS rules.
    [DefaultExecutionOrder(100)]
    public sealed class PickupContextView:MonoBehaviour
    {
        public const string LimitMessage="管線不夠長了！按 F 解除綁定！";
        [SerializeField] PlayerCarryInteraction carry;
        [SerializeField] HoseConnection hose;
        [SerializeField] FirstReviewMission mission;
        [SerializeField] Camera view;
        [SerializeField] Shader outlineShader;
        CarryItem current;InteractionOutlineTarget target;
        Material gold;Font font;Texture2D frame;GUIStyle keyStyle,warningStyle;
        public CarryItem CurrentItem=>current;
        public bool IsPickupPromptVisible=>current&&target&&target.IsVisible;
        public bool IsLengthWarningVisible {get;private set;}
        public void Initialize(PlayerCarryInteraction items,HoseConnection connection,FirstReviewMission state,Camera camera,Shader shader)
        {carry=items;hose=connection;mission=state;view=camera;outlineShader=shader;}
        void Awake()
        {
            if(!outlineShader)outlineShader=Shader.Find("FireRescue/InteractionOutline");
            if(outlineShader)gold=new Material(outlineShader){name="Pickup outline",color=new Color(1,.72f,.12f)};
            font=Font.CreateDynamicFontFromOSFont("Microsoft JhengHei",20);
            frame=RoundedContextFrame.CreateTexture();
        }
        void LateUpdate()=>Refresh();
        public void Refresh()
        {
            bool active=!mission||mission.AllowsInput;
            var next=active&&carry&&!carry.IsCarrying?carry.NearbyItem:null;
            if(next&&(!next.isActiveAndEnabled||next.IsHeld||next.IsDelivered||!next.IsSafeToTake))next=null;
            if(next!=current)
            {
                if(target)target.SetVisible(null);current=next;target=null;
                if(current)
                {
                    var visual=current.transform.Find("Visual");
                    if(visual){target=visual.GetComponent<InteractionOutlineTarget>();if(!target)target=visual.gameObject.AddComponent<InteractionOutlineTarget>();target.SetVisualRoot(visual);}
                }
            }
            if(target)target.SetVisible(gold);
            if(!active||!hose||!hose.IsConnected)IsLengthWarningVisible=false;
            else IsLengthWarningVisible=IsLengthWarningVisible?hose.UsedLength>=hose.MaximumLength-.2f:hose.IsAtLengthLimit;
        }
        void OnGUI()
        {
            if(!frame||!view)return;
            float scale=Screen.height/720f;
            if(keyStyle==null){keyStyle=new GUIStyle(GUI.skin.label){font=font,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,normal={textColor=Color.white}};warningStyle=new GUIStyle(keyStyle){wordWrap=true,fontStyle=FontStyle.Normal};}
            keyStyle.fontSize=Mathf.Max(14,Mathf.RoundToInt(21*scale));warningStyle.fontSize=Mathf.Max(13,Mathf.RoundToInt(18*scale));
            if(IsPickupPromptVisible)
            {
                var p=view.WorldToScreenPoint(current.transform.position+Vector3.up*.18f);
                if(p.z>0&&p.x>=0&&p.x<Screen.width&&p.y>=0&&p.y<Screen.height)
                {
                    var rect=new Rect(p.x+12*scale,Screen.height-p.y-26*scale,46*scale,46*scale);
                    GUI.DrawTexture(rect,frame);GUI.Label(rect,"E",keyStyle);
                }
            }
            if(IsLengthWarningVisible)
            {
                float width=Mathf.Min(Screen.width-24,480*scale);var rect=new Rect((Screen.width-width)*.5f,Screen.height-100*scale,width,48*scale);
                RoundedContextFrame.Draw(rect,10*scale,frame);
                GUI.Label(rect,LimitMessage,warningStyle);
            }
        }
        void OnDisable(){if(target)target.SetVisible(null);current=null;target=null;IsLengthWarningVisible=false;}
        void OnDestroy(){if(gold)Destroy(gold);if(font)Destroy(font);if(frame)Destroy(frame);}
    }
}
