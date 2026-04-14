#ifndef NOISE_INCLUDE_HLSL
#define NOISE_INCLUDE_HLSL

// ── 2D hash ──────────────────────────────────────────────────────────────────
float hash21(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

// ── 3D hash ──────────────────────────────────────────────────────────────────
float hash31(float3 p)
{
    return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453);
}

// ── 2D value noise ───────────────────────────────────────────────────────────
float value_noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash21(i);
    float b = hash21(i + float2(1.0, 0.0));
    float c = hash21(i + float2(0.0, 1.0));
    float d = hash21(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// ── 3D value noise ───────────────────────────────────────────────────────────
float noise3(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float n000 = hash31(i);
    float n100 = hash31(i + float3(1.0, 0.0, 0.0));
    float n010 = hash31(i + float3(0.0, 1.0, 0.0));
    float n110 = hash31(i + float3(1.0, 1.0, 0.0));
    float n001 = hash31(i + float3(0.0, 0.0, 1.0));
    float n101 = hash31(i + float3(1.0, 0.0, 1.0));
    float n011 = hash31(i + float3(0.0, 1.0, 1.0));
    float n111 = hash31(i + float3(1.0, 1.0, 1.0));
    float nx00 = lerp(n000, n100, f.x);
    float nx10 = lerp(n010, n110, f.x);
    float nx01 = lerp(n001, n101, f.x);
    float nx11 = lerp(n011, n111, f.x);
    float nxy0 = lerp(nx00, nx10, f.y);
    float nxy1 = lerp(nx01, nx11, f.y);
    return lerp(nxy0, nxy1, f.z);
}

// ── 3D FBM (4 octaves) ──────────────────────────────────────────────────────
float fbm(float3 p)
{
    float a = 0.5;
    float s = 0.0;
    float3 q = p;
    for (int i = 0; i < 4; i++)
    {
        s += a * noise3(q);
        q *= 2.07;
        a *= 0.48;
    }
    return s;
}

// ── Ridged noise (sharp ridge patterns) ──────────────────────────────────────
float ridged_noise(float2 p)
{
    float v = value_noise(p);
    return 1.0 - abs(v * 2.0 - 1.0);
}

// ── Wind FBM (ridged, 4 octaves) ────────────────────────────────────────────
float wind_fbm(float2 p)
{
    float amp = 0.5;
    float sum = 0.0;
    float gain = 0.45;
    for (int i = 0; i < 4; i++)
    {
        sum += amp * ridged_noise(p);
        p *= 2.1;
        amp *= gain;
    }
    return sum;
}

#endif // NOISE_INCLUDE_HLSL
