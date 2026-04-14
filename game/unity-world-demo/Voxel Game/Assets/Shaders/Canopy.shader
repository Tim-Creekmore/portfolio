Shader "Custom/Canopy"
{
    Properties
    {
        _LeafColor    ("Leaf Color", Color) = (0.353, 0.478, 0.227, 1)
        _WindStrength ("Wind Strength", Range(0,1)) = 0.3
        _WindSpeed    ("Wind Speed", Range(0,4)) = 1.2
        _LeafDetail   ("Leaf Detail", Range(0,1)) = 0.6
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
            #include "NoiseInclude.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _LeafColor;
                float  _WindStrength;
                float  _WindSpeed;
                float  _LeafDetail;
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
                float  fogFactor   : TEXCOORD2;
                float3 vertColor   : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posOS = IN.positionOS.xyz;
                float3 worldPos = TransformObjectToWorld(posOS);
                float3 worldNorm = TransformObjectToWorldNormal(IN.normalOS);

                float bump = noise3(posOS * 3.5) * 0.15 + noise3(posOS * 7.0) * 0.08;
                posOS += IN.normalOS * bump;

                float heightFactor = saturate((worldPos.y - 1.0) / 4.0);
                float phase = hash31(floor(worldPos * 0.5)) * 6.283;
                float sway  = sin(_Time.y * _WindSpeed + worldPos.x * 0.4 + phase) * _WindStrength * heightFactor;
                float sway2 = sin(_Time.y * _WindSpeed * 0.7 + worldPos.z * 0.5 + phase * 1.3) * _WindStrength * 0.5 * heightFactor;
                posOS.x += sway;
                posOS.z += sway2;

                float leafFlutter = sin(_Time.y * 4.0 + posOS.x * 8.0 + posOS.z * 6.0) * 0.012 * heightFactor;
                posOS += IN.normalOS * leafFlutter;

                VertexPositionInputs vpi = GetVertexPositionInputs(posOS);
                OUT.positionCS  = vpi.positionCS;
                OUT.worldPos    = vpi.positionWS;
                OUT.worldNormal = worldNorm;
                OUT.fogFactor   = ComputeFogFactor(vpi.positionCS.z);
                OUT.vertColor   = IN.color.rgb;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 wp = IN.worldPos;
                float3 wn = normalize(IN.worldNormal);

                float3 leafCol = lerp(_LeafColor.rgb, IN.vertColor, 0.7);

                float n1 = noise3(wp * 8.0);
                float n2 = noise3(wp * 16.0 + float3(5, 3, 7));
                float n3 = noise3(wp * 32.0 + float3(13, 17, 11));

                float leafMask = smoothstep(0.35, 0.55, n1);
                float leafEdge = smoothstep(0.3, 0.5, n2);

                float3 dark  = leafCol * 0.6;
                float3 light = leafCol * 1.3 + float3(0.04, 0.07, 0.0);
                float3 col = lerp(dark, leafCol, leafMask);
                col = lerp(col, light, leafEdge * 0.4);
                col += (n3 - 0.5) * 0.06;

                float upDot = dot(wn, float3(0, 1, 0)) * 0.5 + 0.5;
                float ssao = lerp(0.6, 1.0, upDot);
                col *= ssao;
                col = saturate(col);

                Light mainLight = GetMainLight();
                float3 viewDir = GetWorldSpaceNormalizeViewDir(wp);
                float backlightAmt = saturate(dot(-viewDir, mainLight.direction)) * 0.5;
                float3 subsurface = leafCol * 0.35 * backlightAmt * mainLight.color;

                float3 pertNorm = normalize(wn + float3((n2 - 0.5) * _LeafDetail * 0.6, 0, (n3 - 0.5) * _LeafDetail * 0.6));

                InputData inputData = (InputData)0;
                inputData.positionWS = wp;
                inputData.normalWS = pertNorm;
                inputData.viewDirectionWS = viewDir;
                inputData.fogCoord = IN.fogFactor;
                inputData.shadowCoord = TransformWorldToShadowCoord(wp);
                inputData.normalizedScreenSpaceUV = IN.positionCS.xy / _ScreenParams.xy;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = col;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(0.18, 0.18, 0.18);
                surfaceData.smoothness = 1.0 - (0.7 + n1 * 0.15);
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1;
                surfaceData.alpha = 1;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb += subsurface;
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
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
