Shader "Custom/Trunk"
{
    Properties
    {
        _BarkColor     ("Bark Color", Color) = (0.3, 0.18, 0.1, 1)
        _SwaySpeed     ("Sway Speed", Range(0,4)) = 1.0
        _SwayStrength  ("Sway Strength", Range(0,0.1)) = 0.008
        _SwayPhaseLen  ("Sway Phase Length", Range(1,16)) = 8.0
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
                float4 _BarkColor;
                float  _SwaySpeed;
                float  _SwayStrength;
                float  _SwayPhaseLen;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posOS = IN.positionOS.xyz;
                float3 worldPos = TransformObjectToWorld(posOS);

                float heightFactor = saturate((worldPos.y - 0.5) / 2.0);
                float strength = _SwayStrength * heightFactor;
                float phase = hash31(floor(worldPos * 0.5)) * 6.283;

                posOS.x += sin(worldPos.x * _SwayPhaseLen * 1.123 + _Time.y * _SwaySpeed + phase) * strength;
                posOS.z += sin(worldPos.z * _SwayPhaseLen * 0.9123 + _Time.y * _SwaySpeed * 1.3123 + phase) * strength;

                VertexPositionInputs vpi = GetVertexPositionInputs(posOS);
                VertexNormalInputs   vni = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS  = vpi.positionCS;
                OUT.worldPos    = vpi.positionWS;
                OUT.worldNormal = vni.normalWS;
                OUT.uv          = IN.uv;
                OUT.fogFactor   = ComputeFogFactor(vpi.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                float barkNoise = noise3(float3(uv * 12.0, 0));
                float barkGrain = noise3(float3(uv.x * 4.0, uv.y * 24.0, 3.0));

                float3 col = _BarkColor.rgb;
                col += (barkNoise - 0.5) * 0.1;
                col *= 0.9 + barkGrain * 0.15;

                float moss = smoothstep(0.52, 0.62, noise3(float3(uv * 5.0, 7.0)));
                col = lerp(col, float3(0.18, 0.28, 0.1), moss * 0.3);
                col = saturate(col);

                float3 wn = normalize(IN.worldNormal);

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.worldPos;
                inputData.normalWS = wn;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.worldPos);
                inputData.fogCoord = IN.fogFactor;
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
                inputData.normalizedScreenSpaceUV = IN.positionCS.xy / _ScreenParams.xy;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = col;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(0, 0, 0);
                surfaceData.smoothness = 0.08;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 0.8 + barkNoise * 0.2;
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
