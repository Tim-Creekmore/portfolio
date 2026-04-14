Shader "Custom/DayNightSkybox"
{
    Properties
    {
        _SkyTopColor       ("Sky Top",        Color) = (0.32, 0.48, 0.72, 1)
        _SkyHorizonColor   ("Sky Horizon",    Color) = (0.72, 0.65, 0.55, 1)
        _GroundHorizonColor("Ground Horizon", Color) = (0.48, 0.42, 0.36, 1)
        _GroundBottomColor ("Ground Bottom",  Color) = (0.18, 0.16, 0.12, 1)
        _HorizonSharpness  ("Horizon Sharpness", Range(1,16)) = 4.0
    }

    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SkyTopColor;
                half4 _SkyHorizonColor;
                half4 _GroundHorizonColor;
                half4 _GroundBottomColor;
                float _HorizonSharpness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDir    : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.viewDir = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.viewDir);
                float y = dir.y;

                half3 col;
                if (y >= 0.0)
                {
                    float t = saturate(pow(y, 1.0 / _HorizonSharpness));
                    col = lerp(_SkyHorizonColor.rgb, _SkyTopColor.rgb, t);
                }
                else
                {
                    float t = saturate(pow(-y, 1.0 / _HorizonSharpness));
                    col = lerp(_GroundHorizonColor.rgb, _GroundBottomColor.rgb, t);
                }

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
