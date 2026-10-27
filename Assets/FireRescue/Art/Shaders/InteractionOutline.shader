Shader "FireRescue/InteractionOutline"
{
    Properties
    {
        _Color ("Outline Color", Color) = (1, .72, .12, 1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.08)) = 0.035
    }
    SubShader
    {
        // Render just before ordinary opaque source materials; their original mesh
        // covers this expanded back-face hull, leaving only the outer silhouette.
        Tags { "Queue"="Geometry-1" "RenderType"="Opaque" }
        Pass
        {
            Cull Front
            ZWrite On
            ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _OutlineWidth;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert(appdata v)
            {
                v2f o;
                // Width is in world metres, independent of artist import scale.
                float3 worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = normalize(UnityObjectToWorldNormal(v.normal));
                worldPosition += worldNormal * _OutlineWidth;
                o.pos = UnityWorldToClipPos(worldPosition);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target { return _Color; }
            ENDCG
        }
    }
}
