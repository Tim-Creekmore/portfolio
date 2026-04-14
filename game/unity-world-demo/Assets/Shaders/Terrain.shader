Shader "Custom/Terrain"
{
    Properties
    {
        _RoughnessBase ("Roughness Base", Range(0,1)) = 0.88
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

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
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "NoiseInclude.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _RoughnessBase;
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
                float4 vertColor   : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vni = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS  = vpi.positionCS;
                OUT.worldPos    = vpi.positionWS;
                OUT.worldNormal = vni.normalWS;
                OUT.vertColor   = IN.color;
                OUT.fogFactor   = ComputeFogFactor(vpi.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 wp = IN.worldPos;
                float3 wn = normalize(IN.worldNormal);
                float3 tint = IN.vertColor.rgb;

                float n1 = fbm(wp * 2.5);
                float n2 = fbm(wp * 6.0 + float3(13, 5, 21));
                float n3 = fbm(wp * 12.0 + float3(7, 19, 3));

                float3 albedo = tint;
                albedo *= 0.88 + n1 * 0.18;
                albedo *= 0.95 + n2 * 0.08;
                albedo += float3(0.01, 0.015, 0.0) * n3;

                float slope = wn.y;
                float pathNoise = fbm(wp * float3(3, 0.5, 3) + float3(31, 0, 17));
                float path = smoothstep(0.55, 0.65, pathNoise) * smoothstep(0.82, 0.92, slope) * 0.08;
                albedo = lerp(albedo, float3(0.35, 0.28, 0.18), path);

                albedo = saturate(albedo);

                InputData inputData = (InputData)0;
                inputData.positionWS = wp;
                inputData.normalWS = wn;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(wp);
                inputData.fogCoord = IN.fogFactor;
                inputData.shadowCoord = TransformWorldToShadowCoord(wp);
                inputData.normalizedScreenSpaceUV = IN.positionCS.xy / _ScreenParams.xy;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(0.18, 0.18, 0.18);
                surfaceData.smoothness = 1.0 - saturate(_RoughnessBase + n1 * 0.06 - n2 * 0.03);
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = saturate(0.82 + n1 * 0.18);
                surfaceData.alpha = 1;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float3 _LightDirection;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings  { float4 positionCS : SV_POSITION; };

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
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
