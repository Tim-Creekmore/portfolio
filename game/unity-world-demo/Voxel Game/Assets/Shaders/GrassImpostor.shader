Shader "Custom/GrassImpostor"
{
    Properties
    {
        _ColorYoung ("Young Color", Color) = (0.353, 0.478, 0.227, 1)
        _ColorOld   ("Old Color",   Color) = (0.290, 0.408, 0.188, 1)
        _GroundColor("Ground Color",Color) = (0.18, 0.14, 0.08, 1)
        _PatchScale ("Patch Scale", Float) = 6.0
        _DetailScale("Detail Scale",Float) = 2.0
        _WindDir    ("Wind Direction", Vector) = (1.0, 0.3, 0, 0)
        _WindStrength("Wind Strength", Range(0,0.5)) = 0.06
        _WindSpeed  ("Wind Speed", Range(0,3)) = 0.8
        _WindAO     ("Wind AO Affect", Range(0,1)) = 0.35
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
                float3 _ColorYoung;
                float3 _ColorOld;
                float3 _GroundColor;
                float  _PatchScale;
                float  _DetailScale;
                float2 _WindDir;
                float  _WindStrength;
                float  _WindSpeed;
                float  _WindAO;
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
                float  fogFactor   : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vni = GetVertexNormalInputs(IN.normalOS);
                OUT.positionCS  = vpi.positionCS;
                OUT.worldPos    = vpi.positionWS;
                OUT.worldNormal = vni.normalWS;
                OUT.fogFactor   = ComputeFogFactor(vpi.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 wp = IN.worldPos;
                float3 wn = normalize(IN.worldNormal);
                float3 viewDir = GetWorldSpaceNormalizeViewDir(wp);

                float normalToView = 1.0 - dot(viewDir, wn);

                float patch = value_noise(wp.xz / _PatchScale);
                patch = patch * 0.65 + value_noise(wp.xz / (_PatchScale * 0.35) + float2(7.3, 3.1)) * 0.35;
                patch = lerp(patch, 1.0, normalToView * 0.4);

                float3 grassCol = lerp(_ColorYoung, _ColorOld, patch);
                grassCol = lerp(grassCol * 0.7, grassCol * 1.15, patch);

                float highFreq  = value_noise(wp.xz / _DetailScale);
                float highFreq2 = value_noise(wp.xz / (_DetailScale * 0.4) + float2(3.7, 9.1));

                float spottiness = (1.0 - normalToView) * 0.5 - patch * 0.2;
                float groundFactor = smoothstep(spottiness + 0.1, spottiness - 0.1, highFreq);

                float3 albedo = lerp(grassCol, _GroundColor, groundFactor);
                albedo = saturate(albedo);

                float2 windPos = wp.xz / 10.0 - _Time.y * normalize(_WindDir) * _WindSpeed;
                float currentWind = wind_fbm(windPos) * _WindStrength;
                float bttSim = highFreq + smoothstep(0.6, 1.0, normalToView) * 0.4;
                float ao = lerp(0.5, 1.0, bttSim) - currentWind * _WindAO;

                float3 pertNorm = normalize(wn + float3((highFreq2 - 0.5) * 0.6, 0, (highFreq - 0.5) * 0.6));

                InputData inputData = (InputData)0;
                inputData.positionWS = wp;
                inputData.normalWS = pertNorm;
                inputData.viewDirectionWS = viewDir;
                inputData.fogCoord = IN.fogFactor;
                inputData.shadowCoord = TransformWorldToShadowCoord(wp);
                inputData.normalizedScreenSpaceUV = IN.positionCS.xy / _ScreenParams.xy;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(0.12, 0.12, 0.12);
                surfaceData.smoothness = 0.6;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = saturate(ao);
                surfaceData.alpha = 1;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
