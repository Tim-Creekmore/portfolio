Shader "Custom/VoxelBlock"
{
    Properties
    {
        _WindStrength ("Wind Strength", Range(0,0.5)) = 0.05
        _WindSpeed    ("Wind Speed", Range(0,4)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _WindStrength;
                float _WindSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 vertColor   : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posOS = IN.positionOS.xyz;
                float3 worldPos = TransformObjectToWorld(posOS);

                float heightFactor = saturate((worldPos.y - 1.0) / 5.0);
                float phase = frac(dot(floor(worldPos * 0.25), float3(12.9898, 78.233, 45.164))) * 6.283;
                float sway = sin(_Time.y * _WindSpeed + phase) * _WindStrength * heightFactor;
                float sway2 = sin(_Time.y * _WindSpeed * 0.7 + phase * 1.3) * _WindStrength * 0.6 * heightFactor;
                posOS.x += sway;
                posOS.z += sway2;

                VertexPositionInputs vpi = GetVertexPositionInputs(posOS);
                OUT.positionCS  = vpi.positionCS;
                OUT.worldPos    = vpi.positionWS;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                OUT.vertColor   = IN.color.rgb;
                OUT.fogFactor   = ComputeFogFactor(vpi.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 wn = normalize(IN.worldNormal);
                float3 col = IN.vertColor;

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.worldPos));

                float NdotL = dot(wn, mainLight.direction);
                float wrap = NdotL * 0.35 + 0.5;

                float shadow = lerp(0.45, 1.0, mainLight.shadowAttenuation);

                half3 direct = col * wrap * shadow * mainLight.color;
                half3 ambient = col * half3(0.20, 0.25, 0.30) * 0.35;
                half3 lit = direct + ambient;

                lit = MixFog(lit, IN.fogFactor);
                return half4(lit, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _WindStrength;
                float _WindSpeed;
            CBUFFER_END

            float3 _LightDirection;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings  { float4 positionCS : SV_POSITION; };

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;
                float3 posOS = IN.positionOS.xyz;
                float3 worldPos = TransformObjectToWorld(posOS);

                float heightFactor = saturate((worldPos.y - 1.0) / 5.0);
                float phase = frac(dot(floor(worldPos * 0.25), float3(12.9898, 78.233, 45.164))) * 6.283;
                float sway = sin(_Time.y * _WindSpeed + phase) * _WindStrength * heightFactor;
                float sway2 = sin(_Time.y * _WindSpeed * 0.7 + phase * 1.3) * _WindStrength * 0.6 * heightFactor;
                posOS.x += sway;
                posOS.z += sway2;

                float3 posWS = TransformObjectToWorld(posOS);
                float3 normWS = TransformObjectToWorldNormal(IN.normalOS);
                posWS = ApplyShadowBias(posWS, normWS, _LightDirection);
                OUT.positionCS = TransformWorldToHClip(posWS);
                #if UNITY_REVERSED_Z
                    OUT.positionCS.z = min(OUT.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    OUT.positionCS.z = max(OUT.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return OUT;
            }
            half4 ShadowFrag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
