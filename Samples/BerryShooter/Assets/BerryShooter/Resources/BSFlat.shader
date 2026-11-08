Shader "BerryShooter/Flat"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Emission ("Emission", Range(0,1)) = 0
        _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull [_Cull]
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            half _Emission;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; half light : TEXCOORD0; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 n = UnityObjectToWorldNormal(v.normal);
                o.light = saturate(dot(n, normalize(float3(0.4, 0.8, 0.3)))) * 0.6 + 0.4;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                half l = lerp(i.light, 1.0, _Emission);
                return fixed4(_Color.rgb * l, 1);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Color"
}
