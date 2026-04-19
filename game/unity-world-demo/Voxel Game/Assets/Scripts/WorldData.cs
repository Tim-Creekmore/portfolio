using UnityEngine;

public static class WorldData
{
    // PHASE A: simplified grass arena. Set false to restore full 13-biome map.
    public const bool ARENA_MODE = true;

    public const int   SIZE = 120;
    public const int   GRID = 192;
    public const float STEP = (float)SIZE / GRID;

    public const float WATER_Y    = 3.5f;
    public const float POND_BED_Y = 0.8f;

    // Lake in the south-center area
    public static readonly Vector2 LakeCenter = new Vector2(50f, 22f);
    public const float LAKE_RADIUS = 14f;

    // Legacy compatibility
    public static readonly Vector2 PondCenter = LakeCenter;
    public const float POND_RADIUS = LAKE_RADIUS;

    public enum Biome
    {
        Meadow, Forest, Pond, Rocky,
        Farm, Beach, Cliff, Thicket, Moor,
        Village, Ruins, Road, River
    }

    // Display names for the toast UI
    public static string BiomeDisplayName(Biome b)
    {
        switch (b)
        {
            case Biome.Meadow:  return "The Meadow";
            case Biome.Forest:  return "The Forest";
            case Biome.Pond:    return "The Lake";
            case Biome.Rocky:   return "The Highlands";
            case Biome.Farm:    return "The Farmlands";
            case Biome.Beach:   return "The Shore";
            case Biome.Cliff:   return "The Cliffs";
            case Biome.Thicket: return "The Thicket";
            case Biome.Moor:    return "The Moor";
            case Biome.Village: return "The Village";
            case Biome.Ruins:   return "The Ruins";
            case Biome.Road:    return "The Road";
            case Biome.River:   return "The River";
            default:            return b.ToString();
        }
    }

    // ── Biome region centers (Voronoi seeds) ─────────────────────────────
    struct BiomeSeed { public float x, z; public Biome biome; }

    static readonly BiomeSeed[] Seeds =
    {
        new BiomeSeed { x = 20f,  z = 100f, biome = Biome.Moor    },
        new BiomeSeed { x = 60f,  z = 108f, biome = Biome.Cliff   },
        new BiomeSeed { x = 100f, z = 100f, biome = Biome.Rocky   },
        new BiomeSeed { x = 20f,  z = 60f,  biome = Biome.Farm    },
        new BiomeSeed { x = 60f,  z = 60f,  biome = Biome.Village },
        new BiomeSeed { x = 100f, z = 60f,  biome = Biome.Forest  },
        new BiomeSeed { x = 20f,  z = 20f,  biome = Biome.Beach   },
        new BiomeSeed { x = 55f,  z = 22f,  biome = Biome.Meadow  },
        new BiomeSeed { x = 100f, z = 20f,  biome = Biome.Thicket },
    };

    // Ruins sub-region inside the Moor
    static readonly Vector2 RuinsCenter = new Vector2(14f, 108f);
    const float RUINS_RADIUS = 10f;

    // ── River spline (NW → SE) ──────────────────────────────────────────
    static readonly Vector2[] RiverPoints =
    {
        new Vector2(8f,   115f),
        new Vector2(18f,  95f),
        new Vector2(30f,  78f),
        new Vector2(42f,  62f),
        new Vector2(50f,  45f),
        new Vector2(48f,  28f),
        new Vector2(55f,  18f),
        new Vector2(65f,  8f),
    };
    const float RIVER_HALF_WIDTH = 2.5f;
    const float RIVER_BANK_WIDTH = 8f;

    // ── Road splines (from village center outward) ──────────────────────
    public static readonly Vector2[][] RoadPaths =
    {
        // Village → Farm (west)
        new[] { new Vector2(60f, 60f), new Vector2(45f, 60f), new Vector2(30f, 60f) },
        // Village → Meadow (south)
        new[] { new Vector2(60f, 60f), new Vector2(60f, 48f), new Vector2(58f, 36f) },
        // Village → Forest (east)
        new[] { new Vector2(60f, 60f), new Vector2(75f, 60f), new Vector2(88f, 60f) },
        // Village → Cliff (north)
        new[] { new Vector2(60f, 60f), new Vector2(60f, 75f), new Vector2(60f, 88f) },
    };
    const float ROAD_HALF_WIDTH = 1.5f;
    public const float ROAD_HALF_WIDTH_PUBLIC = ROAD_HALF_WIDTH;

    // ── Biome lookup ────────────────────────────────────────────────────
    public static Biome GetBiome(float fx, float fz)
    {
        if (ARENA_MODE) return Biome.Meadow;

        // Overlay checks first (river, road, lake)
        if (IsLake(fx, fz))
            return Biome.Pond;

        float rd = RiverSDF(fx, fz);
        if (rd < RIVER_HALF_WIDTH)
            return Biome.River;

        float roadD = RoadSDF(fx, fz);
        if (roadD < ROAD_HALF_WIDTH)
            return Biome.Road;

        return GetBaseBiome(fx, fz);
    }

    /// <summary>Returns the underlying terrain biome, ignoring road/river/lake overlays.</summary>
    public static Biome GetBaseBiome(float fx, float fz)
    {
        // Voronoi region lookup with smooth noise-warped borders
        float warpAmt = 3f;
        float nx = fx + warpAmt * SmoothNoise(fx * 0.025f, fz * 0.025f);
        float nz = fz + warpAmt * SmoothNoise(fx * 0.025f + 31.7f, fz * 0.025f + 17.3f);

        float bestDist = float.MaxValue;
        Biome bestBiome = Biome.Meadow;

        for (int i = 0; i < Seeds.Length; i++)
        {
            float dx = nx - Seeds[i].x;
            float dz = nz - Seeds[i].z;
            float d = dx * dx + dz * dz;
            if (d < bestDist)
            {
                bestDist = d;
                bestBiome = Seeds[i].biome;
            }
        }

        // Ruins sub-region within Moor
        if (bestBiome == Biome.Moor)
        {
            float ruinsDx = fx - RuinsCenter.x;
            float ruinsDz = fz - RuinsCenter.y;
            if (ruinsDx * ruinsDx + ruinsDz * ruinsDz < RUINS_RADIUS * RUINS_RADIUS)
                return Biome.Ruins;
        }

        return bestBiome;
    }

    // ── Height ──────────────────────────────────────────────────────────
    public static float HeightSmooth(float fx, float fz)
    {
        float baseH = 4.5f
            + Mathf.Sin(fz * 0.04f) * 0.4f
            + Mathf.Cos(fx * 0.06f + fz * 0.03f) * 0.3f
            + Mathf.Sin(fx * 0.12f + fz * 0.09f) * 0.1f;

        if (ARENA_MODE)
            return Mathf.Clamp(baseH, 0.5f, 16.0f);

        float h = baseH;

        // Cliff: steep rise in the north-center
        float cliffMask = Smoothstep(80f, 105f, fz) * BellCurve(fx, 60f, 30f);
        h += cliffMask * (6f + 2f * Mathf.Sin(fx * 0.2f) + StepNoise(fz * 0.15f) * 1.5f);

        // Rocky highlands: NE elevated + rugged
        float rockyMask = Smoothstep(75f, 110f, fx) * Smoothstep(75f, 110f, fz);
        h += rockyMask * (3.5f
            + 0.6f * Mathf.Sin(fx * 0.25f + fz * 0.2f)
            + 0.4f * Mathf.Sin(fx * 0.5f));

        // Moor: undulating but low, NW
        float moorMask = Smoothstep(35f, 5f, fx) * Smoothstep(75f, 110f, fz);
        h += moorMask * (1.2f + 0.5f * Mathf.Sin(fx * 0.3f + fz * 0.25f));

        // Farm: flatten toward village level
        float farmMask = Smoothstep(35f, 5f, fx) * BellCurve(fz, 60f, 25f);
        h = Mathf.Lerp(h, 4.3f + 0.15f * Mathf.Sin(fx * 0.15f), farmMask * 0.7f);

        // Village: flat packed earth
        float villageMask = BellCurve(fx, 60f, 18f) * BellCurve(fz, 60f, 18f);
        h = Mathf.Lerp(h, 4.5f, villageMask * 0.85f);

        // Beach: slope down to water on SW
        float beachMask = Smoothstep(35f, 5f, fx) * Smoothstep(35f, 5f, fz);
        float beachH = Mathf.Lerp(WATER_Y - 0.3f, 4.2f, Mathf.Max(fx, fz) / 35f);
        h = Mathf.Lerp(h, beachH, beachMask * 0.9f);

        // Thicket: slightly lower, bumpy floor
        float thicketMask = Smoothstep(75f, 110f, fx) * Smoothstep(35f, 5f, fz);
        h += thicketMask * (-0.5f + 0.3f * Mathf.Sin(fx * 0.4f + fz * 0.35f));

        // Forest: gentle bumps
        float forestMask = Smoothstep(75f, 110f, fx) * BellCurve(fz, 60f, 25f);
        h += forestMask * 0.6f * Mathf.Sin(fx * 0.3f) * Mathf.Cos(fz * 0.25f);

        // Lake depression — gentle bank slope
        float ld = LakeSDF(fx, fz);
        float lakeOuter = LAKE_RADIUS + 4f;
        if (ld < lakeOuter)
        {
            float bank = Smoothstep(0f, lakeOuter, ld);
            float lakeFloor = POND_BED_Y + 0.5f;
            h = Mathf.Lerp(lakeFloor, h, bank);
        }

        // River carving — gentle banks
        float riverD = RiverSDF(fx, fz);
        float riverCarve = RIVER_HALF_WIDTH + RIVER_BANK_WIDTH;
        if (riverD < riverCarve)
        {
            float riverDepth = WATER_Y - 0.6f;
            float bankBlend = Smoothstep(RIVER_HALF_WIDTH, riverCarve, riverD);
            float riverFloor = riverDepth + 0.08f * Mathf.Sin(fx * 0.5f + fz * 0.3f);
            h = Mathf.Lerp(riverFloor, h, bankBlend);
        }

        // Road: slight flattening/lowering
        float roadD = RoadSDF(fx, fz);
        if (roadD < ROAD_HALF_WIDTH + 1f)
        {
            float roadBlend = Smoothstep(ROAD_HALF_WIDTH, ROAD_HALF_WIDTH + 1f, roadD);
            h = Mathf.Lerp(h - 0.08f, h, roadBlend);
        }

        return Mathf.Clamp(h, 0.5f, 16.0f);
    }

    // ── SDF functions ───────────────────────────────────────────────────
    public static float LakeSDF(float fx, float fz)
    {
        float dx = fx - LakeCenter.x;
        float dz = fz - LakeCenter.y;
        float wobble = Mathf.Sin(Mathf.Atan2(dz, dx) * 3f) * 1.2f;
        return Mathf.Sqrt(dx * dx + dz * dz) + wobble;
    }

    public static float PondSDF(float fx, float fz) => LakeSDF(fx, fz);

    public static bool IsPond(float fx, float fz) => IsLake(fx, fz);

    public static bool IsLake(float fx, float fz)
    {
        if (ARENA_MODE) return false;
        return LakeSDF(fx, fz) < LAKE_RADIUS * 0.85f;
    }

    public static bool IsWater(float fx, float fz)
    {
        if (ARENA_MODE) return false;
        return IsLake(fx, fz) || RiverSDF(fx, fz) < RIVER_HALF_WIDTH;
    }

    public static float RiverSDF(float fx, float fz)
    {
        float minDist = float.MaxValue;
        for (int i = 0; i < RiverPoints.Length - 1; i++)
        {
            float d = DistToSegment(fx, fz,
                RiverPoints[i].x, RiverPoints[i].y,
                RiverPoints[i + 1].x, RiverPoints[i + 1].y);
            if (d < minDist) minDist = d;
        }
        return minDist;
    }

    public static bool IsRiver(float fx, float fz)
    {
        return RiverSDF(fx, fz) < RIVER_HALF_WIDTH;
    }

    public static float RoadSDF(float fx, float fz)
    {
        float minDist = float.MaxValue;
        for (int p = 0; p < RoadPaths.Length; p++)
        {
            var path = RoadPaths[p];
            for (int i = 0; i < path.Length - 1; i++)
            {
                float d = DistToSegment(fx, fz,
                    path[i].x, path[i].y,
                    path[i + 1].x, path[i + 1].y);
                if (d < minDist) minDist = d;
            }
        }
        return minDist;
    }

    public static bool IsRoad(float fx, float fz)
    {
        if (ARENA_MODE) return false;
        return RoadSDF(fx, fz) < ROAD_HALF_WIDTH;
    }

    // Returns true for biomes where grass should grow
    public static bool HasGrass(Biome b)
    {
        switch (b)
        {
            case Biome.Meadow:
            case Biome.Forest:
            case Biome.Farm:
            case Biome.Moor:
            case Biome.Thicket:
            case Biome.Ruins:
                return true;
            default:
                return false;
        }
    }

    // ── Spawn ───────────────────────────────────────────────────────────
    public static Vector3 GetSpawnPosition()
    {
        // South end of the 50x50 arena (centered at 60,60)
        float sx = 60f;
        float sz = 38f;
        float sy = HeightSmooth(sx, sz);
        return new Vector3(sx, sy + 0.85f, sz);
    }

    // ── Utility ─────────────────────────────────────────────────────────
    static float Smoothstep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3.0f - 2.0f * t);
    }

    static float BellCurve(float x, float center, float width)
    {
        float d = (x - center) / width;
        return Mathf.Exp(-d * d * 2f);
    }

    static float StepNoise(float x)
    {
        float floor = Mathf.Floor(x);
        float frac = x - floor;
        float a = HashNoise(floor, 0f);
        float b = HashNoise(floor + 1f, 0f);
        float t = frac * frac * (3f - 2f * frac);
        return Mathf.Lerp(a, b, t) * 2f - 1f;
    }

    static float HashNoise(float x, float y)
    {
        float n = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
        return n - Mathf.Floor(n);
    }

    // Bilinear-interpolated value noise for smooth Voronoi warp
    static float SmoothNoise(float x, float y)
    {
        float ix = Mathf.Floor(x), iy = Mathf.Floor(y);
        float fx = x - ix, fy = y - iy;
        float sx = fx * fx * (3f - 2f * fx);
        float sy = fy * fy * (3f - 2f * fy);
        float a = HashNoise(ix, iy);
        float b = HashNoise(ix + 1f, iy);
        float c = HashNoise(ix, iy + 1f);
        float d = HashNoise(ix + 1f, iy + 1f);
        return Mathf.Lerp(Mathf.Lerp(a, b, sx), Mathf.Lerp(c, d, sx), sy) * 2f - 1f;
    }

    static float DistToSegment(float px, float pz, float ax, float az, float bx, float bz)
    {
        float abx = bx - ax, abz = bz - az;
        float apx = px - ax, apz = pz - az;
        float t = Mathf.Clamp01((apx * abx + apz * abz) / (abx * abx + abz * abz + 1e-8f));
        float cx = ax + t * abx - px;
        float cz = az + t * abz - pz;
        return Mathf.Sqrt(cx * cx + cz * cz);
    }
}
