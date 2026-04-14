Shader "Custom/Water"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.30, 0.50, 0.42, 0.60)
        _DeepColor    ("Deep Color",    Color) = (0.08, 0.18, 0.22, 0.88)
        _DepthFade    ("Depth Fade Distance", Float) = 3.0

        _EdgeColor    ("Shore Edge Color", Color) = (0.55, 0.65, 0.55, 0.70)
        _EdgeWidth    ("Shore Edge Width", Float) = 0.5

        _WaveHeight   ("Wave Height", Float) = 0.08
        _WaveSpeed    ("Wave Speed",  Float) = 0.8
        _WaveScale    ("Wave Scale",  Float) = 0.6

        _ShimmerStr   ("Shimmer Strength", Range(0, 1)) = 0.12
        _ShimmerScale ("Shimmer Scale",    Float) = 3.0
        _ShimmerSpeed ("Shimmer Speed",    Float) = 0.4

        _Smoothness   ("Smoothness", Range(0, 1)) = 0.55
        _Specular     ("Specular",   Range(0, 1)) = 0.10
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "WaterForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "NoiseInclude.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float  _DepthFade;

                float4 _EdgeColor;
                float  _EdgeWidth;

                float  _WaveHeight;
                float  _WaveSpeed;
                float  _WaveScale;

                float  _ShimmerStr;
                float  _ShimmerScale;
                float  _ShimmerSpeed;

                float  _Smoothness;
                float  _Specular;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float4 screenPos   : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            float wave(float2 p, float2 dir, float freq, float phase)
            {
                return sin(dot(p, dir) * freq + phase);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 posOS = IN.positionOS.xyz;
                float3 posWS = TransformObjectToWorld(posOS);

                float2 xz = posWS.xz;
                float t = _Time.y * _WaveSpeed;
                float s = _WaveScale;

                float w1 = wave(xz, float2(1.0, 0.3), s,       t);
                float w2 = wave(xz, float2(0.4, 1.0), s * 1.4, t * 0.7);
                float w3 = wave(xz, float2(0.7, 0.7), s * 2.1, t * 1.3) * 0.4;
                float displacement = (w1 + w2 + w3) * _WaveHeight;

                posWS.y += displacement;

                float eps = 0.3;
                float hR = (wave(xz + float2(eps, 0), float2(1,0.3), s, t)
                          + wave(xz + float2(eps, 0), float2(0.4,1), s*1.4, t*0.7)
                          + wave(xz + float2(eps, 0), float2(0.7,0.7), s*2.1, t*1.3)*0.4) * _WaveHeight;
                float hF = (wave(xz + float2(0, eps), float2(1,0.3), s, t)
                          + wave(xz + float2(0, eps), float2(0.4,1), s*1.4, t*0.7)
                          + wave(xz + float2(0, eps), float2(0.7,0.7), s*2.1, t*1.3)*0.4) * _WaveHeight;

                float3 tangentX = normalize(float3(eps, hR - displacement, 0));
                float3 tangentZ = normalize(float3(0, hF - displacement, eps));
                float3 waveNormal = normalize(cross(tangentZ, tangentX));

                OUT.positionCS  = TransformWorldToHClip(posWS);
                OUT.worldPos    = posWS;
                OUT.worldNormal = waveNormal;
                OUT.screenPos   = ComputeScreenPos(OUT.positionCS);
                OUT.fogFactor   = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 wp = IN.worldPos;
                float3 wn = normalize(IN.worldNormal);

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float sceneDepth01 = SampleSceneDepth(screenUV);
                float sceneDepthLinear = LinearEyeDepth(sceneDepth01, _ZBufferParams);
                float waterSurfaceDepth = IN.screenPos.w;
                float depthDiff = sceneDepthLinear - waterSurfaceDepth;

                float depthFactor = saturate(depthDiff / _DepthFade);
                float4 waterColor = lerp(_ShallowColor, _DeepColor, depthFactor);

                float edgeFactor = 1.0 - saturate(depthDiff / _EdgeWidth);
                edgeFactor = edgeFactor * edgeFactor;
                waterColor = lerp(waterColor, _EdgeColor, edgeFactor * 0.6);

                float2 shimUV = wp.xz * _ShimmerScale * 0.1;
                float shimT = fmod(_Time.y * _ShimmerSpeed, 100.0);
                float shim1 = value_noise(shimUV + float2(shimT, shimT * 0.7));
                float shim2 = value_noise(shimUV * 1.7 + float2(-shimT * 0.5, shimT * 0.3));
                float shimmer = (shim1 + shim2) * 0.5;
                shimmer = smoothstep(0.45, 0.65, shimmer);

                float3 viewDir = GetWorldSpaceNormalizeViewDir(wp);
                float fresnel = 1.0 - saturate(dot(viewDir, wn));
                fresnel = fresnel * fresnel * 0.4;

                float3 albedo = waterColor.rgb;
                albedo += shimmer * _ShimmerStr * (0.6 + fresnel * 0.4) * float3(0.7, 0.8, 0.75);

                float alpha = waterColor.a;
                alpha = lerp(alpha, min(alpha + 0.08, 1.0), fresnel);
                alpha = saturate(alpha * saturate(depthDiff / 0.15));

                InputData inputData = (InputData)0;
                inputData.positionWS = wp;
                inputData.normalWS = wn;
                inputData.viewDirectionWS = viewDir;
                inputData.fogCoord = IN.fogFactor;
                inputData.shadowCoord = TransformWorldToShadowCoord(wp);
                inputData.normalizedScreenSpaceUV = screenUV;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(_Specular, _Specular, _Specular);
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1;
                surfaceData.alpha = alpha;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                color.a = alpha;
                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
