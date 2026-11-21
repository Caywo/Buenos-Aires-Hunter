
Shader "Custom/SkyFog"
{
    Properties
    {
        _Tex ("Cubemap", Cube) = "white" {}
        _Tint ("Tint Color", Color) = (1,1,1,1)
        _Exposure ("Exposure", Float) = 1

        _FogColor ("Fog Color", Color) = (0.5,0.3,0.7,1)
        _FogStartHeight ("Fog Start Height", Range(-1,1)) = -0.1
        _FogEndHeight ("Fog End Height", Range(-1,1)) = 0.1
        _FogIntensity ("Fog Intensity", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            samplerCUBE _Tex;
            float4 _Tint;
            float _Exposure;

            float4 _FogColor;
            float _FogStartHeight;
            float _FogEndHeight;
            float _FogIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.direction = v.vertex.xyz;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.direction);

                half4 sky = texCUBE(_Tex, dir);
                sky.rgb *= _Tint.rgb * _Exposure;

                float height = dir.y;

                float fog = 1.0 - smoothstep(
                    _FogStartHeight,
                    _FogEndHeight,
                    height
                );

                sky.rgb = lerp(
                    sky.rgb,
                    _FogColor.rgb,
                    fog * _FogIntensity
                );

                return sky;
            }
            ENDCG
        }
    }
    Fallback Off
}
