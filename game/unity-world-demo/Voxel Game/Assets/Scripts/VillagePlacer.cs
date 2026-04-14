using UnityEngine;
using System.Collections.Generic;

public class VillagePlacer : MonoBehaviour
{
    [Header("Building Prefabs")]
    public GameObject house1Prefab;
    public GameObject house2Prefab;
    public GameObject house3Prefab;
    public GameObject house4Prefab;
    public GameObject innPrefab;
    public GameObject blacksmithPrefab;
    public GameObject stablePrefab;
    public GameObject bellTowerPrefab;

    [Header("Prop Prefabs")]
    public GameObject wellPrefab;
    public GameObject gazeboPrefab;
    public GameObject bonfirePrefab;
    public GameObject bench1Prefab;
    public GameObject marketStand1Prefab;
    public GameObject marketStand2Prefab;
    public GameObject cartPrefab;
    public GameObject barrelPrefab;
    public GameObject cratePrefab;
    public GameObject fencePrefab;
    public GameObject hayPrefab;

    [Header("Materials (URP)")]
    public Material woodMaterial;
    public Material stoneMaterial;
    public Material roofMaterial;

    [Header("Scale & Offset")]
    public float buildingScale = 430f;
    public float propScale = 300f;

    static readonly Dictionary<string, int> matCategory = new Dictionary<string, int>
    {
        {"Wood", 0}, {"DarkWood", 0}, {"Hay", 0}, {"Rope", 0},
        {"Stone_Dark", 1}, {"Stone_Light", 1}, {"Plaster", 1}, {"Metal", 1},
        {"RoofTiles_Red", 2}, {"RoofTiles_Grey", 2}, {"Fabric", 2},
    };

    struct Placement
    {
        public GameObject prefab;
        public float x, z, yaw, scale;
        public Placement(GameObject p, float x, float z, float yaw, float s)
        {
            prefab = p; this.x = x; this.z = z; this.yaw = yaw; scale = s;
        }
    }

    void Awake()
    {
        PlaceVillage();
    }

    void PlaceVillage()
    {
        var placements = new List<Placement>();

        float bs = buildingScale;
        float ps = propScale;

        // Roads form a cross at (60,60). River passes ~X=42-50 on the west side.
        // Buildings go in four quadrants BETWEEN roads, all east of X=50.

        // ── NW quadrant (X=51-57, Z=63+) ────────────────────────
        Add(placements, innPrefab,    52f, 72f, 135f,  bs);   // faces SE toward center
        Add(placements, house1Prefab, 52f, 65f,  90f,  bs);   // faces east toward road

        // ── NE quadrant (X=63+, Z=63+) ──────────────────────────
        Add(placements, house2Prefab,    72f, 72f, 225f, bs);  // faces SW toward center
        Add(placements, bellTowerPrefab, 70f, 65f, -90f, bs);  // faces west, visible landmark
        Add(placements, house3Prefab,    78f, 68f, -90f, bs);  // extra house on far east

        // ── SW quadrant (X=51-57, Z < 57) ───────────────────────
        Add(placements, blacksmithPrefab, 52f, 53f,  90f, bs); // faces east toward road
        Add(placements, house4Prefab,     52f, 46f,  45f, bs); // faces NE toward center

        // ── SE quadrant (X=63+, Z < 57) ─────────────────────────
        Add(placements, stablePrefab, 72f, 48f, -90f, bs);     // faces west toward road
        Add(placements, innPrefab,    78f, 53f, -90f, bs * 0.8f); // smaller second inn/tavern

        // ── Town square props (in quadrant corners near the crossroads) ──
        Add(placements, wellPrefab,    57f, 63.5f, 0f,   ps);  // NW of intersection
        Add(placements, gazeboPrefab,  64f, 64f,   0f,   ps);  // NE of intersection
        Add(placements, bonfirePrefab, 64f, 56f,   0f,   ps);  // SE of intersection

        // Market stands on south side of E-W road, not blocking it
        Add(placements, marketStand1Prefab, 56f, 57f,   0f, ps);
        Add(placements, marketStand2Prefab, 65f, 57f,   0f, ps);

        // Cart and benches around the square
        Add(placements, cartPrefab,   66f, 64.5f, -30f, ps);
        Add(placements, bench1Prefab, 56f, 64f,    90f, ps);  // near well
        Add(placements, bench1Prefab, 65f, 55f,     0f, ps);  // near bonfire

        // Hay near stable
        Add(placements, hayPrefab, 70f, 45f,  15f, ps);
        Add(placements, hayPrefab, 74f, 46f, -10f, ps);

        // Barrels and crates scattered
        var rng = new System.Random(0xB33F);
        PlaceScattered(placements, barrelPrefab, rng, 10, ps);
        PlaceScattered(placements, cratePrefab,  rng, 8,  ps);

        // Fences — gaps where roads pass through, west side pulled to X=50
        PlaceFenceRow(placements, 50f, 44f, 57f, 44f, 4,   0f);   // south, west half
        PlaceFenceRow(placements, 63f, 44f, 80f, 44f, 5,   0f);   // south, east half
        PlaceFenceRow(placements, 50f, 76f, 57f, 76f, 4, 180f);   // north, west half
        PlaceFenceRow(placements, 63f, 76f, 80f, 76f, 5, 180f);   // north, east half
        PlaceFenceRow(placements, 50f, 46f, 50f, 57f, 4,  90f);   // west, south segment
        PlaceFenceRow(placements, 50f, 63f, 50f, 74f, 4,  90f);   // west, north segment
        PlaceFenceRow(placements, 80f, 46f, 80f, 57f, 4, -90f);   // east, south segment
        PlaceFenceRow(placements, 80f, 63f, 80f, 74f, 4, -90f);   // east, north segment

        int placed = 0;
        foreach (var p in placements)
        {
            if (p.prefab == null) continue;
            float terrainY = WorldData.HeightSmooth(p.x, p.z);

            var go = Instantiate(p.prefab, transform);
            go.transform.localScale = Vector3.one * p.scale;

            // Compose yaw on top of the prefab's native rotation (preserves FBX axis correction)
            Quaternion nativeRot = p.prefab.transform.rotation;
            go.transform.rotation = Quaternion.Euler(0f, p.yaw, 0f) * nativeRot;

            // Place temporarily to compute world bounds
            go.transform.position = new Vector3(p.x, 0f, p.z);

            // Calculate combined bounds of all renderers to find the bottom
            Bounds combined = new Bounds(go.transform.position, Vector3.zero);
            bool first = true;
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
            {
                if (first) { combined = mr.bounds; first = false; }
                else combined.Encapsulate(mr.bounds);
            }

            // Shift so the bottom of the model sits on the terrain
            float bottomY = combined.min.y;
            float correction = terrainY - bottomY;
            go.transform.position = new Vector3(p.x, correction, p.z);

            go.isStatic = true;
            UpgradeMaterials(go);

            if (placed < 3)
            {
                Debug.Log($"[VillagePlacer] {go.name} pos=({p.x},{go.transform.position.y:F2},{p.z}) " +
                          $"scale={p.scale} boundsSize={combined.size} boundsMinY={bottomY:F3} " +
                          $"nativeRot={nativeRot.eulerAngles} terrainY={terrainY:F2}");
            }
            placed++;
        }
        Debug.Log($"[VillagePlacer] Placed {placed} objects total.");
    }

    Material _fallback;
    Material GetFallback()
    {
        if (_fallback != null) return _fallback;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) return null;
        _fallback = new Material(shader);
        _fallback.SetColor("_BaseColor", new Color(0.50f, 0.35f, 0.20f));
        _fallback.SetFloat("_Smoothness", 0.08f);
        return _fallback;
    }

    void UpgradeMaterials(GameObject go)
    {
        Material wood  = woodMaterial  != null ? woodMaterial  : GetFallback();
        Material stone = stoneMaterial != null ? stoneMaterial : GetFallback();
        Material roof  = roofMaterial  != null ? roofMaterial  : GetFallback();
        if (wood == null) return;

        Material[] urpMats = { wood, stone, roof };
        foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = mr.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) { mats[i] = wood; continue; }
                string matName = mats[i].name;
                int cat;
                if (matCategory.TryGetValue(matName, out cat))
                    mats[i] = urpMats[cat];
                else
                    mats[i] = wood;
            }
            mr.sharedMaterials = mats;
        }
    }

    void Add(List<Placement> list,
             GameObject prefab, float x, float z, float yaw, float scale)
    {
        list.Add(new Placement(prefab, x, z, yaw, scale));
    }

    void PlaceScattered(List<Placement> list,
                        GameObject prefab, System.Random rng, int count, float scale)
    {
        if (prefab == null) return;
        for (int i = 0; i < count; i++)
        {
            float fx = 50f + (float)(rng.NextDouble() * 20f);
            float fz = 50f + (float)(rng.NextDouble() * 20f);
            if (WorldData.GetBiome(fx, fz) != WorldData.Biome.Village) continue;
            if (WorldData.IsRoad(fx, fz)) continue;
            float yaw = (float)(rng.NextDouble() * 360.0);
            list.Add(new Placement(prefab, fx, fz, yaw, scale * (0.8f + (float)rng.NextDouble() * 0.4f)));
        }
    }

    void PlaceFenceRow(List<Placement> list,
                       float x1, float z1, float x2, float z2, int count, float yaw)
    {
        if (fencePrefab == null) return;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            float fx = Mathf.Lerp(x1, x2, t);
            float fz = Mathf.Lerp(z1, z2, t);
            list.Add(new Placement(fencePrefab, fx, fz, yaw, propScale));
        }
    }
}
