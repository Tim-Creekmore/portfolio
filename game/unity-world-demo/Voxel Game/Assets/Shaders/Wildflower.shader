Shader "Custom/Wildflower"
{
    Properties
    {
        _WindDir      ("Wind Direction", Vector) = (1.0, 0.3, 0, 0)
        _WindStrength ("Wind Strength", Range(0,0.3)) = 0.04
        _WindSpeed    ("Wind Speed", Range(0,3)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "NoiseInclude.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float2 _WindDir;
                float  _WindStrength;
                float  _WindSpeed;
            CBUFFER_END

            #ifdef UNITY_INSTANCING_ENABLED
                UNITY_INSTANCING_BUFFER_START(Props)
                    UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
                UNITY_INSTANCING_BUFFER_END(Props)
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float  vHeight     : TEXCOORD3;
                float  fogFactor   : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 posOS = IN.positionOS.xyz;
                float vHeight = 1.0 - IN.uv.y;
                OUT.vHeight = vHeight;

                float3 worldOrigin = TransformObjectToWorld(float3(0,0,0));
                float2 windPos = worldOrigin.xz / 10.0 - _Time.y * normalize(_WindDir) * _WindSpeed;
                float windNoise = value_noise(windPos);
                float sway = windNoise * _WindStrength * vHeight * 2.0;

                float4x4 invModel = unity_WorldToObject;
                float2 localDir = mul((float3x3)invModel, float3(_WindDir.x, 0, _WindDir.y)).xz;
                posOS.xz += sway * localDir;
                posOS.y  -= sway * 0.2;

                VertexPositionInputs vpi = GetVertexPositionInputs(posOS);
                VertexNormalInputs   vni = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS  = vpi.positionCS;
                OUT.worldPos    = vpi.positionWS;
                OUT.worldNormal = vni.normalWS;
                OUT.uv          = IN.uv;
                OUT.fogFactor   = ComputeFogFactor(vpi.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 wn = normalize(IN.worldNormal);
                if (!isFrontFace) wn = -wn;

                float3 flowerCol = float3(0.65, 0.55, 0.25);
                #ifdef UNITY_INSTANCING_ENABLED
                    flowerCol = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor).rgb;
                #endif

                Light mainLight = GetMainLight();
                float3 viewDir = GetWorldSpaceNormalizeViewDir(IN.worldPos);
                float backlightAmt = saturate(dot(-viewDir, mainLight.direction)) * 0.5;
                float3 subsurface = flowerCol * 0.3 * backlightAmt * mainLight.color;

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.worldPos;
                inputData.normalWS = wn;
                inputData.viewDirectionWS = viewDir;
                inputData.fogCoord = IN.fogFactor;
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
                inputData.normalizedScreenSpaceUV = IN.positionCS.xy / _ScreenParams.xy;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = flowerCol;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(0.15, 0.15, 0.15);
                surfaceData.smoothness = 0.4;
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
    }
    FallBack Off
}
