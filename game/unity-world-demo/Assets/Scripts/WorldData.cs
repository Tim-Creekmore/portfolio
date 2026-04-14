using UnityEngine;

public static class WorldData
{
    public const int   SIZE         = 16;
    public const int   GRID         = 64;
    public const float STEP         = (float)SIZE / GRID;
    public const float WATER_Y      = 3.2f;
    public const float RIVER_HALF   = 1.6f;
    public const float RIVER_BED_Y  = 2.2f;

    public static float HeightSmooth(float fx, float fz)
    {
        float meadow = Mathf.Sin(fz * 0.22f) * 0.35f
                      + Mathf.Cos(fx * 0.45f + fz * 0.15f) * 0.25f
                      + Mathf.Sin(fx * 0.8f + fz * 0.6f) * 0.12f;
        float baseH = 4.5f + meadow;

        float ridgeMask = Smoothstep(6.0f, 11.5f, fx) * Smoothstep(3.5f, 12.0f, fz);
        float ridgeShape = 4.2f * ridgeMask
                         + 0.5f * ridgeMask * Mathf.Sin(fz * 0.38f)
                         + 0.4f * ridgeMask * Mathf.Sin(fx * 0.35f)
                         + 0.2f * ridgeMask * Mathf.Sin(fx * 1.2f + fz * 0.9f);
        float h = baseH + ridgeShape;

        float rd = RiverSDF(fx, fz);
        if (rd < RIVER_HALF)
        {
            float bank = Smoothstep(0.0f, RIVER_HALF, rd);
            h = Mathf.Lerp(RIVER_BED_Y, h, bank);
        }

        return Mathf.Clamp(h, 1.0f, 14.0f);
    }

    public static float RiverSDF(float fx, float fz)
    {
        float centerX = 5.0f + Mathf.Sin(fz * 0.35f) * 0.6f;
        return Mathf.Abs(fx - centerX);
    }

    public static bool IsRiver(float fx, float fz)
    {
        return RiverSDF(fx, fz) < RIVER_HALF * 0.7f && fz > 1.5f && fz < 14.5f;
    }

    public static Vector3 GetSpawnPosition()
    {
        float sx = 3.0f;
        float sz = 8.0f;
        float sy = HeightSmooth(sx, sz);
        return new Vector3(sx, sy + 0.85f, sz);
    }

    static float Smoothstep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3.0f - 2.0f * t);
    }
}
