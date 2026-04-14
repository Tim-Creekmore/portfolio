Shader "Custom/Water"
{
    Properties
    {
        _ShallowColor   ("Shallow Color", Color) = (0.18, 0.45, 0.52, 1)
        _DeepColor      ("Deep Color",    Color) = (0.06, 0.18, 0.28, 1)
        _AlphaBase      ("Alpha Base", Range(0,1)) = 0.72
        _WaveSpeed      ("Wave Speed", Range(0,4)) = 0.6
        _WaveScale      ("Wave Scale", Range(0.5,20)) = 4.0
        _WaveHeight     ("Wave Height", Range(0,0.3)) = 0.04
        _RippleStrength ("Ripple Strength", Range(0,1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "NoiseInclude.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float  _AlphaBase;
                float  _WaveSpeed;
                float  _WaveScale;
                float  _WaveHeight;
                float  _RippleStrength;
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
                float  fogFactor   : TEXCOORD2;
            };

            float water_waves(float2 p, float t)
            {
                float w = 0.0;
                w += sin(p.x * 1.2 + p.y * 0.8 + t * 1.1) * 0.4;
                w += sin(p.x * 0.7 - p.y * 1.5 + t * 0.9) * 0.3;
                w += sin(p.x * 2.1 + p.y * 1.7 + t * 1.6) * 0.15;
                w += value_noise(p * 2.0 + float2(t * 0.3, t * 0.2)) * 0.15;
                return w;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posOS = IN.positionOS.xyz;
                float3 worldPos = TransformObjectToWorld(posOS);
                OUT.worldPos = worldPos;

                float2 wp = worldPos.xz * _WaveScale;
                float t = _Time.y * _WaveSpeed;
                float h = water_waves(wp, t);
                posOS.y += h * _WaveHeight;

                VertexPositionInputs vpi = GetVertexPositionInputs(posOS);
                OUT.positionCS  = vpi.positionCS;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                OUT.fogFactor   = ComputeFogFactor(vpi.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 wp = IN.worldPos;
                float2 wpScaled = wp.xz * _WaveScale;
                float t = _Time.y * _WaveSpeed;

                float eps = 0.1;
                float hC = water_waves(wpScaled, t);
                float hR = water_waves(wpScaled + float2(eps, 0), t);
                float hU = water_waves(wpScaled + float2(0, eps), t);
                float3 rippleNormal = normalize(float3(
                    (hC - hR) * _RippleStrength,
                    1.0,
                    (hC - hU) * _RippleStrength));

                float3 wn = normalize(IN.worldNormal);
                wn = normalize(lerp(wn, rippleNormal, 0.7));

                float3 viewDir = GetWorldSpaceNormalizeViewDir(wp);
                float fresnel = pow(1.0 - abs(dot(viewDir, wn)), 3.0);

                float3 col = lerp(_DeepColor.rgb, _ShallowColor.rgb, fresnel * 0.6 + 0.2);

                float foamNoise = value_noise(wp.xz * 8.0 + float2(_Time.y * 0.15, 0));
                float foam = smoothstep(0.72, 0.82, foamNoise) * 0.15;
                col += foam;
                col = saturate(col);

                float alpha = saturate(_AlphaBase + fresnel * 0.2);
                alpha = min(alpha, 0.92);

                InputData inputData = (InputData)0;
                inputData.positionWS = wp;
                inputData.normalWS = wn;
                inputData.viewDirectionWS = viewDir;
                inputData.fogCoord = IN.fogFactor;
                inputData.shadowCoord = TransformWorldToShadowCoord(wp);
                inputData.normalizedScreenSpaceUV = IN.positionCS.xy / _ScreenParams.xy;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = col;
                surfaceData.metallic = 0.15;
                surfaceData.specular = half3(0.7, 0.7, 0.7);
                surfaceData.smoothness = 0.95;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1;
                surfaceData.emission = _DeepColor.rgb * 0.08;
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
