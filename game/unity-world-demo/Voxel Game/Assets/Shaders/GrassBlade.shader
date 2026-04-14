Shader "Custom/GrassBlade"
{
    Properties
    {
        _SizeSmall   ("Size Small", Range(0.01,1)) = 0.4
        _SizeLarge   ("Size Large", Range(0.1,2)) = 0.75
        _BladeBend   ("Blade Bend", Range(0,2)) = 0.2
        _PatchScale  ("Patch Scale", Float) = 6.0
        _WindDir     ("Wind Direction", Vector) = (1.0, 0.3, 0, 0)
        _WindStrength("Wind Strength", Range(0,0.5)) = 0.06
        _WindSpeed   ("Wind Speed", Range(0,3)) = 0.8
        _WindBendStr ("Wind Bend Strength", Range(0,5)) = 1.0
        _WindAO      ("Wind AO Affect", Range(0,1)) = 0.35
        _ColorYoung  ("Young Color", Color) = (0.353, 0.478, 0.227, 1)
        _ColorOld    ("Old Color",   Color) = (0.290, 0.408, 0.188, 1)
        _Backlight   ("Backlight", Color) = (0.20, 0.25, 0.10, 1)
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
                float  _SizeSmall;
                float  _SizeLarge;
                float  _BladeBend;
                float  _PatchScale;
                float2 _WindDir;
                float  _WindStrength;
                float  _WindSpeed;
                float  _WindBendStr;
                float  _WindAO;
                float3 _ColorYoung;
                float3 _ColorOld;
                float3 _Backlight;
            CBUFFER_END

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
                float  btt         : TEXCOORD3;
                float  patch       : TEXCOORD4;
                float  windBend    : TEXCOORD5;
                float  fogFactor   : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 posOS = IN.positionOS.xyz;
                float btt = 1.0 - IN.uv.y;
                OUT.btt = btt;

                float3 worldOrigin = TransformObjectToWorld(float3(0,0,0));
                float2 worldXZ = worldOrigin.xz;

                float patch = value_noise(worldXZ / _PatchScale);
                patch = patch * 0.65 + value_noise(worldXZ / (_PatchScale * 0.35) + float2(7.3, 3.1)) * 0.35;
                OUT.patch = patch;

                float sz = lerp(_SizeSmall, _SizeLarge, patch);
                posOS *= sz;

                float bendAmount = _BladeBend * (0.5 + patch * 0.5);
                posOS.z += bendAmount * btt * btt * sz;

                float2 windPos = worldXZ / 10.0 - _Time.y * normalize(_WindDir) * _WindSpeed;
                float windNoise = wind_fbm(windPos);
                float currentWind = windNoise * _WindStrength * btt * 2.0;
                OUT.windBend = currentWind;

                float4x4 invModel = unity_WorldToObject;
                float2 localDir = mul((float3x3)invModel, float3(_WindDir.x, 0, _WindDir.y)).xz;

                posOS.xz += currentWind * localDir * _WindBendStr;
                posOS.y  -= currentWind * 0.4 * sz;

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

                wn = normalize(lerp(wn, float3(0, 1, 0), IN.btt));

                float3 colBase = lerp(_ColorYoung, _ColorOld, IN.patch);
                float3 col = lerp(colBase * 0.65, colBase * 1.25, IN.btt);
                col = saturate(col);

                float ao = IN.btt - IN.windBend * _WindAO;

                Light mainLight = GetMainLight();
                float3 viewDir = GetWorldSpaceNormalizeViewDir(IN.worldPos);
                float backlight = saturate(dot(-viewDir, mainLight.direction)) * 0.5;
                float3 subsurface = _Backlight * backlight * mainLight.color;

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.worldPos;
                inputData.normalWS = wn;
                inputData.viewDirectionWS = viewDir;
                inputData.fogCoord = IN.fogFactor;
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
                inputData.normalizedScreenSpaceUV = IN.positionCS.xy / _ScreenParams.xy;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = col;
                surfaceData.metallic = 0;
                surfaceData.specular = half3(0.2, 0.2, 0.2);
                surfaceData.smoothness = 0.6;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = saturate(ao);
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
