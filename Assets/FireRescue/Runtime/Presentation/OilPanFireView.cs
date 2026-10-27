using UnityEngine;
namespace FireRescue
{
    // Read-only fire scale/flicker. Removing all visual children leaves gameplay unchanged.
    public sealed class OilPanFireView:MonoBehaviour
    {
        [SerializeField] ObjectFire fire;
        [SerializeField] CookingOilArea area;
        [SerializeField] CookingOilIncident incident;
        Transform[] flames;
        Transform[] patches;
        Vector3[] patchPositions;
        Material orange,yellow;
        Mesh mesh;
        public void Initialize(ObjectFire state)=>fire=state;
        public void SetArea(CookingOilArea value)=>area=value;
        public void SetIncident(CookingOilIncident value)=>incident=value;
        void Awake()
        {
            orange=new Material(Shader.Find("Standard")){color=new Color(1,.23f,.015f)};orange.EnableKeyword("_EMISSION");orange.SetColor("_EmissionColor",new Color(1,.15f,0)*.8f);
            yellow=new Material(Shader.Find("Standard")){color=new Color(1,.8f,.09f)};yellow.EnableKeyword("_EMISSION");yellow.SetColor("_EmissionColor",new Color(1,.6f,.02f));
            mesh=new Mesh{name="Temporary flame"};var vertices=new Vector3[13];var indices=new int[54];
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;vertices[i]=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));vertices[i+6]=vertices[i]*.6f+Vector3.up*.4f;}
            vertices[12]=new Vector3(.2f,1,-.08f);int w=0;
            for(int i=0;i<6;i++){int n=(i+1)%6;foreach(var index in new[]{i,6+n,n,i,6+i,6+n,6+i,12,6+n})indices[w++]=index;}
            mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateNormals();
            flames=new Transform[8];for(int i=0;i<8;i++){var go=new GameObject("Flame view "+i);go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=i%2==0?orange:yellow;flames[i]=go.transform;}
            if(area)
            {
                patches=new Transform[72];patchPositions=new Vector3[72];
                for(int i=0;i<patches.Length;i++)
                {
                    int x=i%8,z=i/8;patchPositions[i]=new Vector3(Mathf.Lerp(area.Minimum.x+.12f,area.Maximum.x-.12f,x/7f),.12f,Mathf.Lerp(area.Minimum.y+.12f,area.Maximum.y-.12f,z/8f));
                    patchPositions[i]+=new Vector3(Mathf.Sin(i*17.13f)*.13f,0,Mathf.Cos(i*23.71f)*.13f);
                    if(Physics.Raycast(patchPositions[i]+Vector3.up*2,Vector3.down,out var surface,3,(1<<8)|(1<<0),QueryTriggerInteraction.Ignore))patchPositions[i].y=surface.point.y+.02f;
                    var go=new GameObject("Oil-spill flame "+i);go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=i%3==0?yellow:orange;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;patches[i]=go.transform;
                }
            }
        }
        void LateUpdate()
        {
            if(flames==null)return;float intensity=fire?fire.Intensity:0;
            for(int i=0;i<flames.Length;i++)
            {
                var f=flames[i];f.gameObject.SetActive(intensity>0);if(intensity<=0)continue;
                float burst=incident?Mathf.Clamp01(1-incident.ExplosionAge/.6f)*2:0;
                float t=Time.time*8+i*1.7f,s=Mathf.Sqrt(intensity)+burst;bool core=i%2==1;
                float a=i*Mathf.PI*.5f;f.localPosition=new Vector3(Mathf.Cos(a)*.12f*s,.07f,Mathf.Sin(a)*.12f*s-(core?.04f:0));
                float width=(core?.09f:.16f)*s;f.localScale=new Vector3(width,(core?.33f:.55f)*s*(1+Mathf.Sin(t)*.13f),width);
                f.localRotation=Quaternion.Euler(Mathf.Sin(t)*8,0,Mathf.Cos(t)*8);
            }
            if(patches!=null)for(int i=0;i<patches.Length;i++)
            {
                var f=patches[i];bool hot=area&&area.IsHot(patchPositions[i]);f.gameObject.SetActive(hot);if(!hot)continue;
                float t=Time.time*7+i*1.9f;f.position=patchPositions[i];float s=.18f+.035f*Mathf.Sin(t);
                f.localScale=new Vector3(s,(.32f+.12f*Mathf.Sin(t*.7f))*Mathf.Sqrt(Mathf.Max(1,intensity)),s);f.rotation=Quaternion.Euler(Mathf.Sin(t)*9,0,Mathf.Cos(t)*8);
            }
        }
        void OnDisable(){if(flames!=null)foreach(var f in flames)if(f)f.gameObject.SetActive(false);if(patches!=null)foreach(var f in patches)if(f)f.gameObject.SetActive(false);}
        void OnDestroy(){if(mesh)Destroy(mesh);if(orange)Destroy(orange);if(yellow)Destroy(yellow);}
    }
}
