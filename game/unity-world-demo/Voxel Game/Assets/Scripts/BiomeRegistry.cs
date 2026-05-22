using UnityEngine;

/// <summary>
/// Runtime biome region lookup. Uses 3-seed Voronoi with noise-warped borders
/// to decide which BiomeData applies at any (x, z) on the world map.
/// Place one BiomeRegistry component in the scene — WorldSceneSetup wires this up.
/// </summary>
public class BiomeRegistry : MonoBehaviour
{
    [Header("Biome Assets")]
    [SerializeField] BiomeData temperate;
    [SerializeField] BiomeData mountain;
    [SerializeField] BiomeData frontier;

    [Header("Region Seeds (world-space XZ)")]
    [SerializeField] Vector2 temperateSeed = new Vector2(40f, 40f);   // SW starter
    [SerializeField] Vector2 frontierSeed  = new Vector2(95f, 45f);   // East wilds
    [SerializeField] Vector2 mountainSeed  = new Vector2(60f, 95f);   // North heights

    [Header("Border softening")]
    [Tooltip("How wavy the biome borders are. 0 = straight Voronoi, higher = more organic.")]
    [SerializeField] float borderNoiseAmplitude = 6f;
    [SerializeField] float borderNoiseScale     = 0.06f;

    static BiomeRegistry _instance;
    public static BiomeRegistry Instance => _instance;
    public static bool IsReady => _instance != null
        && _instance.temperate != null && _instance.mountain != null && _instance.frontier != null;

    void Awake()
    {
        _instance = this;
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    /// <summary>Returns the closest biome's data at (x, z).</summary>
    public BiomeData GetBiomeAt(float x, float z)
    {
        if (temperate == null) return null;

        // Warp the sample point with noise so borders feel organic instead of straight Voronoi edges
        float warpX = (Mathf.PerlinNoise(x * borderNoiseScale, z * borderNoiseScale) - 0.5f) * 2f * borderNoiseAmplitude;
        float warpZ = (Mathf.PerlinNoise((x + 123f) * borderNoiseScale, (z + 456f) * borderNoiseScale) - 0.5f) * 2f * borderNoiseAmplitude;

        float sx = x + warpX;
        float sz = z + warpZ;

        float dT = SqrDist(sx, sz, temperateSeed);
        float dF = SqrDist(sx, sz, frontierSeed);
        float dM = SqrDist(sx, sz, mountainSeed);

        if (dT <= dF && dT <= dM) return temperate;
        if (dF <= dM) return frontier;
        return mountain;
    }

    /// <summary>
    /// Returns the primary biome and a second-closest biome with a blend weight in [0,1].
    /// Used by terrain shading to soften biome borders.
    /// </summary>
    public void GetBlendedBiomes(float x, float z, out BiomeData primary, out BiomeData secondary, out float blend)
    {
        primary   = temperate;
        secondary = temperate;
        blend = 0f;

        if (temperate == null) return;

        float warpX = (Mathf.PerlinNoise(x * borderNoiseScale, z * borderNoiseScale) - 0.5f) * 2f * borderNoiseAmplitude;
        float warpZ = (Mathf.PerlinNoise((x + 123f) * borderNoiseScale, (z + 456f) * borderNoiseScale) - 0.5f) * 2f * borderNoiseAmplitude;
        float sx = x + warpX;
        float sz = z + warpZ;

        // Sort 3 distances. Array approach keeps code simple for 3 entries.
        BiomeData[] biomes = { temperate, frontier, mountain };
        float[] dists = {
            Mathf.Sqrt(SqrDist(sx, sz, temperateSeed)),
            Mathf.Sqrt(SqrDist(sx, sz, frontierSeed)),
            Mathf.Sqrt(SqrDist(sx, sz, mountainSeed)),
        };

        int iBest = 0, iSecond = 1;
        if (dists[1] < dists[iBest]) { iSecond = iBest; iBest = 1; }
        else                          { iSecond = 1; }
        if (dists[2] < dists[iBest]) { iSecond = iBest; iBest = 2; }
        else if (dists[2] < dists[iSecond]) { iSecond = 2; }

        primary   = biomes[iBest];
        secondary = biomes[iSecond];

        // Blend only in a narrow transition band based on relative distance
        float diff = dists[iSecond] - dists[iBest];
        const float TRANSITION_BAND = 6f;
        blend = 1f - Mathf.Clamp01(diff / TRANSITION_BAND);
        // Ease for softer falloff at the center of a region
        blend *= blend;
    }

    static float SqrDist(float x, float z, Vector2 seed)
    {
        float dx = x - seed.x;
        float dz = z - seed.y;
        return dx * dx + dz * dz;
    }

    // ── Editor gizmos for region visualization ─────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 0.4f, 0.6f);
        Gizmos.DrawSphere(new Vector3(temperateSeed.x, 10f, temperateSeed.y), 2f);
        Gizmos.color = new Color(0.9f, 0.5f, 0.2f, 0.6f);
        Gizmos.DrawSphere(new Vector3(frontierSeed.x, 10f, frontierSeed.y), 2f);
        Gizmos.color = new Color(0.8f, 0.85f, 0.95f, 0.6f);
        Gizmos.DrawSphere(new Vector3(mountainSeed.x, 10f, mountainSeed.y), 2f);
    }
}
