using UnityEngine;
namespace FireRescue
{
    public static class RoundedContextFrame
    {
        const float UvBorder=12f/64;
        public static Texture2D CreateTexture(bool readable=false)
        {
            var frame=new Texture2D(64,64,TextureFormat.RGBA32,false){name="Rounded context frame",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[64*64];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                var q=new Vector2(Mathf.Abs(x-31.5f)-21.5f,Mathf.Abs(y-31.5f)-21.5f);
                float d=new Vector2(Mathf.Max(0,q.x),Mathf.Max(0,q.y)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-9;
                float a=Mathf.Clamp01(-d);pixels[y*64+x]=d> -2.4f?new Color(1,1,1,a*.95f):new Color(.08f,.11f,.14f,a*.82f);
            }
            frame.SetPixels(pixels);frame.Apply(false,!readable);return frame;
        }
        public static Rect SourceUv(int column,int row)
        {
            float u=column==0?0:column==1?UvBorder:1-UvBorder;
            // IMGUI rows are top-down; texture V is bottom-up. Top row must sample TOP.
            float v=row==0?1-UvBorder:row==1?UvBorder:0;
            return new Rect(u,v,column==1?1-2*UvBorder:UvBorder,row==1?1-2*UvBorder:UvBorder);
        }
        public static void Draw(Rect rect,float border,Texture2D frame)
        {
            for(int y=0;y<3;y++)for(int x=0;x<3;x++)
            {
                float px=x==0?0:x==1?border:rect.width-border,py=y==0?0:y==1?border:rect.height-border;
                float w=x==1?rect.width-2*border:border,h=y==1?rect.height-2*border:border;
                GUI.DrawTextureWithTexCoords(new Rect(rect.x+px,rect.y+py,w,h),frame,SourceUv(x,y));
            }
        }
    }
}
