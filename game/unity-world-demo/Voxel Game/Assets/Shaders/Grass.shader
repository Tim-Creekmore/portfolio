Shader "Custom/Grass"
{
    Properties
    {
        [Header(Shading)]
        _TopColor("Top Color", Color) = (0.32, 0.48, 0.18, 1)
        _BottomColor("Bottom Color", Color) = (0.08, 0.15, 0.04, 1)
        _TranslucentGain("Translucent Gain", Range(0, 1)) = 0.5

        [Header(Blade Shape)]
        _BladeWidth("Blade Width", Float) = 0.04
        _BladeWidthRandom("Blade Width Random", Float) = 0.02
        _BladeHeight("Blade Height", Float) = 0.5
        _BladeHeightRandom("Blade Height Random", Float) = 0.3
        _BladeForward("Blade Forward Amount", Float) = 0.38
        _BladeCurve("Blade Curvature Amount", Range(1, 4)) = 2
        _BendRotationRandom("Bend Rotation Random", Range(0, 1)) = 0.2

        [Header(Tessellation)]
        _TessellationUniform("Tessellation Uniform", Range(1, 64)) = 3

        [Header(Wind)]
        _WindFrequency("Wind Frequency", Vector) = (0.05, 0.05, 0, 0)
        _WindStrength("Wind Strength", Float) = 0.3
    }

    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
    #include "NoiseInclude.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _TopColor;
        float4 _BottomColor;
        float  _TranslucentGain;
        float  _BladeWidth;
        float  _BladeWidthRandom;
        float  _BladeHeight;
        float  _BladeHeightRandom;
        float  _BladeForward;
        float  _BladeCurve;
        float  _BendRotationRandom;
        float  _TessellationUniform;
        float2 _WindFrequency;
        float  _WindStrength;
    CBUFFER_END

    #include "CustomTessellation.hlsl"

    #define BLADE_SEGMENTS 3

    struct geometryOutput
    {
        float4 pos      : SV_POSITION;
        float2 uv       : TEXCOORD0;
        float3 worldPos : TEXCOORD1;
        float3 normal   : TEXCOORD2;
        float  fogCoord : TEXCOORD3;
    };

    float rand(float3 co)
    {
        return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
    }

    float3x3 AngleAxis3x3(float angle, float3 axis)
    {
        float c, s;
        sincos(angle, s, c);
        float t = 1.0 - c;
        float x = axis.x;
        float y = axis.y;
        float z = axis.z;
        return float3x3(
            t*x*x + c,   t*x*y - s*z, t*x*z + s*y,
            t*x*y + s*z, t*y*y + c,   t*y*z - s*x,
            t*x*z - s*y, t*y*z + s*x, t*z*z + c
        );
    }

    geometryOutput VertexOutput(float3 pos, float2 uv, float3 normal)
    {
        geometryOutput o;
        o.worldPos = TransformObjectToWorld(pos);
        o.normal   = TransformObjectToWorldNormal(normal);
        o.pos      = TransformWorldToHClip(o.worldPos);
        o.uv       = uv;
        o.fogCoord = ComputeFogFactor(o.pos.z);
        return o;
    }

    geometryOutput GenerateGrassVertex(float3 vertexPosition, float width, float height,
                                       float forward, float2 uv, float3x3 transformMatrix)
    {
        float3 tangentPoint  = float3(width, forward, height);
        float3 tangentNormal = normalize(float3(0, -1, forward));

        float3 localPosition = vertexPosition + mul(transformMatrix, tangentPoint);
        float3 localNormal   = mul(transformMatrix, tangentNormal);
        return VertexOutput(localPosition, uv, localNormal);
    }

    // 3 verts for ground fill + BLADE_SEGMENTS*2+1 for the blade
    [maxvertexcount(BLADE_SEGMENTS * 2 + 1 + 3)]
    void geo(triangle vertexOutput IN[3], inout TriangleStream<geometryOutput> triStream)
    {
        // Ground fill: emit the input triangle as a visible surface between blades.
        // UV.y = 0 maps to _BottomColor in the fragment shader.
        for (int j = 0; j < 3; j++)
        {
            triStream.Append(VertexOutput(IN[j].vertex.xyz, float2(0, 0), IN[j].normal));
        }
        triStream.RestartStrip();

        // Grass blade from vertex[0]
        float3 pos      = IN[0].vertex.xyz;
        float3 vNormal  = IN[0].normal;
        float4 vTangent = IN[0].tangent;
        float3 vBinormal = cross(vNormal, vTangent.xyz) * vTangent.w;

        float3x3 tangentToLocal = float3x3(
            vTangent.x, vBinormal.x, vNormal.x,
            vTangent.y, vBinormal.y, vNormal.y,
            vTangent.z, vBinormal.z, vNormal.z
        );

        float3x3 facingRotationMatrix = AngleAxis3x3(rand(pos) * TWO_PI, float3(0, 0, 1));

        float3x3 bendRotationMatrix = AngleAxis3x3(
            rand(pos.zzx) * _BendRotationRandom * PI * 0.5,
            float3(-1, 0, 0));

        float2 windUV = pos.xz * 0.1 + _WindFrequency * _Time.y;
        float2 windSample = (float2(
            value_noise(windUV),
            value_noise(windUV + float2(43.7, 17.3))
        ) * 2 - 1) * _WindStrength;
        float3 wind = normalize(float3(windSample.x, windSample.y, 0));
        float3x3 windRotation = AngleAxis3x3(length(windSample), wind);

        float3x3 transformationMatrix = mul(mul(mul(tangentToLocal, windRotation), facingRotationMatrix), bendRotationMatrix);
        float3x3 transformationMatrixFacing = mul(tangentToLocal, facingRotationMatrix);

        float height = (rand(pos.zyx) * 2 - 1) * _BladeHeightRandom + _BladeHeight;
        float width  = (rand(pos.xzy) * 2 - 1) * _BladeWidthRandom + _BladeWidth;
        float forward = rand(pos.yyz) * _BladeForward;

        for (int i = 0; i < BLADE_SEGMENTS; i++)
        {
            float t = i / (float)BLADE_SEGMENTS;
            float segmentHeight  = height * t;
            float segmentWidth   = width * (1 - t);
            float segmentForward = pow(t, _BladeCurve) * forward;

            float3x3 transformMatrix = i == 0 ? transformationMatrixFacing : transformationMatrix;

            triStream.Append(GenerateGrassVertex(pos,  segmentWidth, segmentHeight, segmentForward, float2(0, t), transformMatrix));
            triStream.Append(GenerateGrassVertex(pos, -segmentWidth, segmentHeight, segmentForward, float2(1, t), transformMatrix));
        }

        triStream.Append(GenerateGrassVertex(pos, 0, height, forward, float2(0.5, 1), transformationMatrix));
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" }

        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            ZWrite On

            HLSLPROGRAM
            #pragma target 4.6
            #pragma vertex vert
            #pragma hull hull
            #pragma domain domain
            #pragma geometry geo
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            half4 frag(geometryOutput i, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float3 normal = isFrontFace ? i.normal : -i.normal;

                float4 shadowCoord = TransformWorldToShadowCoord(i.worldPos);
                Light mainLight = GetMainLight(shadowCoord);

                float shadow = mainLight.shadowAttenuation;
                float NdotL = saturate(saturate(dot(normal, mainLight.direction)) + _TranslucentGain) * shadow;

                float3 ambient = SampleSH(normal);
                float3 lightIntensity = NdotL * mainLight.color + ambient;

                float4 col = lerp(_BottomColor, _TopColor * float4(lightIntensity, 1), i.uv.y);

                col.rgb = MixFog(col.rgb, i.fogCoord);

                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma target 4.6
            #pragma vertex vert
            #pragma hull hull
            #pragma domain domain
            #pragma geometry geo
            #pragma fragment fragShadow

            half4 fragShadow(geometryOutput i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma target 4.6
            #pragma vertex vert
            #pragma hull hull
            #pragma domain domain
            #pragma geometry geo
            #pragma fragment fragDepth

            half4 fragDepth(geometryOutput i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 4.6
            #pragma vertex vert
            #pragma hull hull
            #pragma domain domain
            #pragma geometry geo
            #pragma fragment fragDepthNormals

            half4 fragDepthNormals(geometryOutput i) : SV_Target
            {
                float3 n = normalize(i.normal) * 0.5 + 0.5;
                return half4(n, 0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
