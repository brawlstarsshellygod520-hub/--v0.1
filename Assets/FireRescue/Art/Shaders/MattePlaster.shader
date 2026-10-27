Shader "FireRescue/MattePlaster"
{
    Properties { _Color ("Plaster color", Color) = (0.76,0.73,0.66,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows
        fixed4 _Color;
        struct Input { float3 worldPos; };
        void surf(Input IN, inout SurfaceOutput o) { o.Albedo=_Color.rgb; o.Alpha=1; }
        ENDCG
    }
    Fallback "Diffuse"
}
