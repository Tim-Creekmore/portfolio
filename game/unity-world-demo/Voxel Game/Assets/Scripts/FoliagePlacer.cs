using System.Collections.Generic;
using UnityEngine;

public class FoliagePlacer : MonoBehaviour
{
    [Header("Grass")]
    public Material grassMaterial;

    [Header("Trees")]
    public Material canopyMaterial;
    public Material trunkMaterial;

    [Header("Rocks")]
    public Material rockMaterial;

    [Header("Wildflowers")]
    public Material wildflowerMaterial;

    [Header("Props")]
    public Material propMaterial;

    static readonly Color[] FLOWER_COLORS =
    {
        new Color(0.68f, 0.62f, 0.28f),
        new Color(0.75f, 0.72f, 0.60f),
        new Color(0.50f, 0.35f, 0.55f),
        new Color(0.62f, 0.30f, 0.32f),
        new Color(0.65f, 0.50f, 0.22f),
        new Color(0.42f, 0.48f, 0.60f),
    };

    static readonly Color[] TREE_COLORS =
    {
        new Color(0.14f, 0.26f, 0.08f),
        new Color(0.18f, 0.30f, 0.10f),
        new Color(0.16f, 0.28f, 0.06f),
        new Color(0.22f, 0.34f, 0.12f),
        new Color(0.20f, 0.32f, 0.14f),
        new Color(0.12f, 0.24f, 0.08f),
        new Color(0.24f, 0.36f, 0.10f),
        new Color(0.16f, 0.30f, 0.12f),
        new Color(0.26f, 0.38f, 0.16f),
        new Color(0.10f, 0.22f, 0.06f),
    };

    void Start()
    {
        Invoke(nameof(PlaceAll), 0f);
    }

    void PlaceAll()
    {
        PlaceGrass();
        PlaceTrees();
        PlaceRocks();
        PlaceWildflowers();
        PlaceFarmProps();
        PlaceVillageProps();
        PlaceRuinsProps();
        PlaceRiverReeds();
    }

    // ── Grass ────────────────────────────────────────────────────────────
    void PlaceGrass()
    {
        int res = 192;
        float cell = (float)WorldData.SIZE / res;
        var verts = new List<Vector3>();
        var uvs   = new List<Vector2>();
        var tris  = new List<int>();

        for (int gz = 0; gz < res; gz++)
        {
            for (int gx = 0; gx < res; gx++)
            {
                float x0 = gx * cell;
                float z0 = gz * cell;
                float x1 = x0 + cell;
                float z1 = z0 + cell;
                float mx = (x0 + x1) * 0.5f;
                float mz = (z0 + z1) * 0.5f;

                if (WorldData.IsWater(mx, mz)) continue;
                if (WorldData.IsRoad(mx, mz)) continue;

                var biome = WorldData.GetBaseBiome(mx, mz);
                if (!WorldData.HasGrass(biome)) continue;

                float y00 = WorldData.HeightSmooth(x0, z0) + 0.01f;
                float y10 = WorldData.HeightSmooth(x1, z0) + 0.01f;
                float y01 = WorldData.HeightSmooth(x0, z1) + 0.01f;
                float y11 = WorldData.HeightSmooth(x1, z1) + 0.01f;

                float minY = Mathf.Min(y00, Mathf.Min(y10, Mathf.Min(y01, y11)));
                if (minY < WorldData.WATER_Y + 0.15f) continue;

                int vi = verts.Count;
                verts.Add(new Vector3(x0, y00, z0));
                verts.Add(new Vector3(x1, y10, z0));
                verts.Add(new Vector3(x0, y01, z1));
                verts.Add(new Vector3(x1, y11, z1));

                float invS = 1f / WorldData.SIZE;
                uvs.Add(new Vector2(x0 * invS, z0 * invS));
                uvs.Add(new Vector2(x1 * invS, z0 * invS));
                uvs.Add(new Vector2(x0 * invS, z1 * invS));
                uvs.Add(new Vector2(x1 * invS, z1 * invS));

                tris.Add(vi);     tris.Add(vi + 2); tris.Add(vi + 1);
                tris.Add(vi + 1); tris.Add(vi + 2); tris.Add(vi + 3);
            }
        }

        if (verts.Count == 0) return;

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        var go = new GameObject("GrassMesh");
        go.transform.SetParent(transform, false);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;
        mr.sharedMaterial = grassMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    // ── Trees ────────────────────────────────────────────────────────────
    void PlaceTrees()
    {
        var rng = new System.Random(0x54524545);
        int treeIdx = 0;

        PlaceBiomeTrees(rng, ref treeIdx, WorldData.Biome.Forest,  80f, 115f, 40f, 80f, 3.5f, 0.10f);
        PlaceBiomeTrees(rng, ref treeIdx, WorldData.Biome.Thicket, 80f, 115f, 5f,  35f, 2.5f, 0.05f);
        PlaceBiomeTrees(rng, ref treeIdx, WorldData.Biome.Meadow,  40f, 75f,  5f,  35f, 8f,   0.50f);
        PlaceBiomeTrees(rng, ref treeIdx, WorldData.Biome.Moor,    5f,  35f,  80f, 115f, 10f, 0.60f);
        PlaceBiomeTrees(rng, ref treeIdx, WorldData.Biome.Farm,    5f,  35f,  42f, 78f,  12f, 0.70f);

        // Scattered saplings/bushes near lake
        for (int i = 0; i < 6; i++)
        {
            float angle = RngRange(rng, 0f, Mathf.PI * 2f);
            float dist = WorldData.LAKE_RADIUS + RngRange(rng, 2f, 6f);
            float fx = WorldData.LakeCenter.x + Mathf.Cos(angle) * dist;
            float fz = WorldData.LakeCenter.y + Mathf.Sin(angle) * dist;
            fx = Mathf.Clamp(fx, 2f, WorldData.SIZE - 2f);
            fz = Mathf.Clamp(fz, 2f, WorldData.SIZE - 2f);
            if (WorldData.IsWater(fx, fz)) continue;
            PlaceOneTree(rng, fx, fz, ref treeIdx,
                rng.NextDouble() < 0.5 ? TreeGenerator.Category.Sapling : TreeGenerator.Category.Bush);
        }
    }

    void PlaceBiomeTrees(System.Random rng, ref int treeIdx,
        WorldData.Biome targetBiome,
        float minX, float maxX, float minZ, float maxZ,
        float spacing, float skipChance)
    {
        for (float fz = minZ + 1f; fz < maxZ; fz += spacing)
        {
            for (float fx = minX + 1f; fx < maxX; fx += spacing)
            {
                float jx = fx + RngRange(rng, -spacing * 0.3f, spacing * 0.3f);
                float jz = fz + RngRange(rng, -spacing * 0.3f, spacing * 0.3f);
                jx = Mathf.Clamp(jx, 2f, WorldData.SIZE - 2f);
                jz = Mathf.Clamp(jz, 2f, WorldData.SIZE - 2f);

                if (rng.NextDouble() < skipChance) continue;
                if (WorldData.IsWater(jx, jz)) continue;
                if (WorldData.IsRoad(jx, jz)) continue;

                var biome = WorldData.GetBiome(jx, jz);
                if (biome != targetBiome) continue;

                var cat = RollCategory(rng, biome);
                PlaceOneTree(rng, jx, jz, ref treeIdx, cat);
            }
        }
    }

    TreeGenerator.Category RollCategory(System.Random rng, WorldData.Biome biome)
    {
        float roll = (float)rng.NextDouble();
        switch (biome)
        {
            case WorldData.Biome.Thicket:
                return roll < 0.3f ? TreeGenerator.Category.Mature :
                       roll < 0.7f ? TreeGenerator.Category.Sapling :
                       TreeGenerator.Category.Bush;
            case WorldData.Biome.Moor:
                return roll < 0.2f ? TreeGenerator.Category.Sapling : TreeGenerator.Category.Bush;
            case WorldData.Biome.Farm:
                return TreeGenerator.Category.Sapling;
            default:
                return roll < 0.65f ? TreeGenerator.Category.Mature :
                       roll < 0.85f ? TreeGenerator.Category.Sapling :
                       TreeGenerator.Category.Bush;
        }
    }

    static readonly TreeGenerator.Style[] MATURE_STYLES =
    {
        TreeGenerator.Style.Pine, TreeGenerator.Style.Round, TreeGenerator.Style.Oak,
        TreeGenerator.Style.TallPine, TreeGenerator.Style.Birch,
        TreeGenerator.Style.Weeping, TreeGenerator.Style.Gnarly
    };

    TreeGenerator.Style PickStyle(System.Random rng, TreeGenerator.Category cat)
    {
        switch (cat)
        {
            case TreeGenerator.Category.Bush:    return TreeGenerator.Style.Bush;
            case TreeGenerator.Category.Sapling: return TreeGenerator.Style.Sapling;
            default: return MATURE_STYLES[rng.Next(MATURE_STYLES.Length)];
        }
    }

    void PlaceOneTree(System.Random rng, float fx, float fz, ref int treeIdx)
    {
        PlaceOneTree(rng, fx, fz, ref treeIdx, TreeGenerator.Category.Mature);
    }

    void PlaceOneTree(System.Random rng, float fx, float fz, ref int treeIdx, TreeGenerator.Category cat)
    {
        float y = WorldData.HeightSmooth(fx, fz);

        var style = PickStyle(rng, cat);
        Color leafColor = TREE_COLORS[rng.Next(TREE_COLORS.Length)];
        float lighten = RngRange(rng, -0.03f, 0.03f);
        leafColor = new Color(
            Mathf.Clamp01(leafColor.r + lighten),
            Mathf.Clamp01(leafColor.g + lighten),
            Mathf.Clamp01(leafColor.b + lighten));

        var meshes = TreeGenerator.Generate(style, new System.Random(rng.Next()), leafColor);

        float s, sy;
        switch (cat)
        {
            case TreeGenerator.Category.Bush:
                s = RngRange(rng, 0.5f, 0.9f);
                sy = s * RngRange(rng, 0.7f, 1.0f);
                break;
            case TreeGenerator.Category.Sapling:
                s = RngRange(rng, 0.7f, 1.1f);
                sy = s * RngRange(rng, 0.9f, 1.1f);
                break;
            default:
                s = RngRange(rng, 1.0f, 1.5f);
                sy = s * RngRange(rng, 0.9f, 1.1f);
                break;
        }

        float rotY = (float)(rng.NextDouble() * Mathf.PI * 2.0f);

        var treeGO = new GameObject($"Tree_{treeIdx}");
        treeGO.transform.SetParent(transform, false);
        treeGO.transform.position = new Vector3(fx, y, fz);
        treeGO.transform.localScale = new Vector3(s, sy, s);
        treeGO.transform.rotation = Quaternion.Euler(0, rotY * Mathf.Rad2Deg, 0);

        var trunkGO = new GameObject("Trunk");
        trunkGO.transform.SetParent(treeGO.transform, false);
        var tmf = trunkGO.AddComponent<MeshFilter>();
        var tmr = trunkGO.AddComponent<MeshRenderer>();
        tmf.sharedMesh = meshes.wood;
        tmr.sharedMaterial = trunkMaterial;

        if (meshes.wood.vertexCount > 0)
        {
            var col = trunkGO.AddComponent<BoxCollider>();
            col.center = meshes.wood.bounds.center;
            col.size = meshes.wood.bounds.size;
        }

        var canopyGO = new GameObject("Canopy");
        canopyGO.transform.SetParent(treeGO.transform, false);
        var cmf = canopyGO.AddComponent<MeshFilter>();
        var cmr = canopyGO.AddComponent<MeshRenderer>();
        cmf.sharedMesh = meshes.leaves;
        cmr.sharedMaterial = canopyMaterial;

        treeIdx++;
    }

    // ── Rocks ────────────────────────────────────────────────────────────
    void PlaceRocks()
    {
        var rng = new System.Random(0x524f434b);
        var rockMesh = CreateRockMesh();
        var matrices = new List<Matrix4x4>();

        for (int i = 0; i < 120; i++)
        {
            float fx = RngRange(rng, 2f, WorldData.SIZE - 2f);
            float fz = RngRange(rng, 2f, WorldData.SIZE - 2f);

            if (WorldData.IsWater(fx, fz)) continue;
            if (WorldData.IsRoad(fx, fz)) continue;

            var biome = WorldData.GetBiome(fx, fz);

            // Skip biomes where rocks don't belong
            if (biome == WorldData.Biome.Farm || biome == WorldData.Biome.Village)
                continue;

            float y = WorldData.HeightSmooth(fx, fz);
            Vector3 pos = new Vector3(fx, y + 0.05f, fz);
            Quaternion rot = Quaternion.Euler(
                RngRange(rng, -17f, 17f),
                RngRange(rng, 0, 360f),
                RngRange(rng, -17f, 17f));

            float scaleBase;
            switch (biome)
            {
                case WorldData.Biome.Rocky:
                case WorldData.Biome.Cliff:
                    scaleBase = RngRange(rng, 0.8f, 2.5f); break;
                case WorldData.Biome.Beach:
                    scaleBase = RngRange(rng, 0.2f, 0.6f); break;
                case WorldData.Biome.Ruins:
                    scaleBase = RngRange(rng, 0.5f, 1.5f); break;
                default:
                    scaleBase = RngRange(rng, 0.3f, 1.0f); break;
            }

            Vector3 scale = new Vector3(
                scaleBase * RngRange(rng, 0.7f, 1.3f),
                scaleBase * RngRange(rng, 0.5f, 0.9f),
                scaleBase * RngRange(rng, 0.7f, 1.2f));

            matrices.Add(Matrix4x4.TRS(pos, rot, scale));
        }

        if (matrices.Count == 0) return;

        var go = new GameObject("Rocks");
        go.transform.SetParent(transform, false);
        var drawer = go.AddComponent<InstancedDrawer>();
        drawer.mesh = rockMesh;
        drawer.material = rockMaterial;
        drawer.matrices = matrices.ToArray();
    }

    Mesh CreateRockMesh()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var mesh = Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);
        Object.Destroy(go);
        var verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i].x *= 0.6f;
            verts[i].y *= 0.45f;
            verts[i].z *= 0.6f;
        }
        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ── Wildflowers ─────────────────────────────────────────────────────
    void PlaceWildflowers()
    {
        var rng = new System.Random(0x464C5752);
        var flowerMesh = BuildFlowerMesh();
        var matrices = new List<Matrix4x4>();
        var colorList = new List<Vector4>();

        for (int i = 0; i < 12000; i++)
        {
            float fx = RngRange(rng, 2f, WorldData.SIZE - 2f);
            float fz = RngRange(rng, 2f, WorldData.SIZE - 2f);
            if (WorldData.IsWater(fx, fz)) continue;

            var biome = WorldData.GetBiome(fx, fz);
            float density;
            switch (biome)
            {
                case WorldData.Biome.Meadow:  density = 1.0f; break;
                case WorldData.Biome.Forest:  density = 0.15f; break;
                case WorldData.Biome.Farm:    density = 0.3f; break;
                case WorldData.Biome.Moor:    density = 0.4f; break;
                case WorldData.Biome.Ruins:   density = 0.5f; break;
                case WorldData.Biome.Thicket: density = 0.08f; break;
                case WorldData.Biome.Rocky:   density = 0.1f; break;
                default:                      density = 0.05f; break;
            }

            if (rng.NextDouble() > density) continue;

            float y = WorldData.HeightSmooth(fx, fz);
            float scale = RngRange(rng, 0.7f, 1.3f);
            Quaternion rot = Quaternion.Euler(
                RngRange(rng, -8.6f, 8.6f),
                RngRange(rng, 0, 360f),
                RngRange(rng, -8.6f, 8.6f));
            Vector3 pos = new Vector3(fx, y + 0.15f + RngRange(rng, 0f, 0.1f), fz);

            matrices.Add(Matrix4x4.TRS(pos, rot, Vector3.one * scale));

            Color col = FLOWER_COLORS[rng.Next(FLOWER_COLORS.Length)];
            float lighten = RngRange(rng, -0.08f, 0.08f);
            col = new Color(
                Mathf.Clamp01(col.r + lighten),
                Mathf.Clamp01(col.g + lighten),
                Mathf.Clamp01(col.b + lighten));
            colorList.Add(new Vector4(col.r, col.g, col.b, 1));
        }

        if (matrices.Count == 0) return;

        var go = new GameObject("Wildflowers");
        go.transform.SetParent(transform, false);
        var drawer = go.AddComponent<InstancedDrawer>();
        drawer.mesh = flowerMesh;
        drawer.material = wildflowerMaterial;
        drawer.matrices = matrices.ToArray();
        drawer.instanceColors = colorList.ToArray();
        drawer.castShadows = false;
    }

    Mesh BuildFlowerMesh()
    {
        int petals = 5;
        float petalW = 0.035f;
        float petalH = 0.06f;
        float centerR = 0.015f;

        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs   = new List<Vector2>();
        var tris  = new List<int>();

        for (int p = 0; p < petals; p++)
        {
            float angle = (float)p / petals * Mathf.PI * 2f;
            float ca = Mathf.Cos(angle);
            float sa = Mathf.Sin(angle);
            Vector3 bse = new Vector3(ca * centerR, 0, sa * centerR);
            Vector3 tip = new Vector3(ca * (centerR + petalH), 0, sa * (centerR + petalH));
            Vector3 perp = new Vector3(-sa, 0, ca) * petalW * 0.5f;
            Vector3 left  = bse + perp;
            Vector3 right = bse - perp;

            int vi = verts.Count;
            verts.Add(left);  norms.Add(Vector3.up); uvs.Add(new Vector2(0, 1));
            verts.Add(right); norms.Add(Vector3.up); uvs.Add(new Vector2(1, 1));
            verts.Add(tip);   norms.Add(Vector3.up); uvs.Add(new Vector2(0.5f, 0));

            tris.Add(vi); tris.Add(vi + 2); tris.Add(vi + 1);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // ── Farm props (wheat rows, fences) ─────────────────────────────────
    void PlaceFarmProps()
    {
        if (propMaterial == null) return;
        var rng = new System.Random(0x4641524D);
        var wheatMesh = BuildWheatMesh();
        var matrices = new List<Matrix4x4>();
        var colors = new List<Vector4>();
        Color wheatColor = new Color(0.72f, 0.65f, 0.30f);

        for (float fz = 45f; fz < 75f; fz += 1.2f)
        {
            for (float fx = 8f; fx < 32f; fx += 0.8f)
            {
                float jx = fx + RngRange(rng, -0.15f, 0.15f);
                float jz = fz + RngRange(rng, -0.15f, 0.15f);

                if (WorldData.GetBiome(jx, jz) != WorldData.Biome.Farm) continue;
                if (WorldData.IsRoad(jx, jz)) continue;

                float y = WorldData.HeightSmooth(jx, jz);
                float s = RngRange(rng, 0.8f, 1.2f);
                float rotY = RngRange(rng, -15f, 15f);
                matrices.Add(Matrix4x4.TRS(
                    new Vector3(jx, y, jz),
                    Quaternion.Euler(0, rotY, 0),
                    Vector3.one * s));

                float v = RngRange(rng, -0.06f, 0.06f);
                colors.Add(new Vector4(wheatColor.r + v, wheatColor.g + v, wheatColor.b, 1));
            }
        }

        if (matrices.Count == 0) return;

        var go = new GameObject("WheatCrops");
        go.transform.SetParent(transform, false);
        var drawer = go.AddComponent<InstancedDrawer>();
        drawer.mesh = wheatMesh;
        drawer.material = propMaterial;
        drawer.matrices = matrices.ToArray();
        drawer.instanceColors = colors.ToArray();
        drawer.castShadows = false;
    }

    Mesh BuildWheatMesh()
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var tris  = new List<int>();

        float w = 0.02f, h = 0.6f;
        // Two crossed quads
        for (int i = 0; i < 2; i++)
        {
            float angle = i * Mathf.PI * 0.5f;
            float ca = Mathf.Cos(angle);
            float sa = Mathf.Sin(angle);
            Vector3 right = new Vector3(ca, 0, sa) * w;

            int vi = verts.Count;
            verts.Add(-right); norms.Add(Vector3.forward);
            verts.Add( right); norms.Add(Vector3.forward);
            verts.Add( right + Vector3.up * h); norms.Add(Vector3.forward);
            verts.Add(-right + Vector3.up * h); norms.Add(Vector3.forward);
            tris.Add(vi); tris.Add(vi+2); tris.Add(vi+1);
            tris.Add(vi); tris.Add(vi+3); tris.Add(vi+2);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // Village props now handled by VillagePlacer with real FBX models
    void PlaceVillageProps() { }

    // ── Ruins props (crumbled walls, broken pillars) ────────────────────
    void PlaceRuinsProps()
    {
        if (propMaterial == null) return;
        var rng = new System.Random(0x5255494E);
        var cubeMesh = BuildCubeMesh();
        var matrices = new List<Matrix4x4>();
        var colors = new List<Vector4>();

        Color ruinStone = new Color(0.48f, 0.45f, 0.40f);

        // Wall segments
        for (int i = 0; i < 20; i++)
        {
            float fx = RngRange(rng, 6f, 22f);
            float fz = RngRange(rng, 100f, 116f);
            if (WorldData.GetBiome(fx, fz) != WorldData.Biome.Ruins) continue;

            float y = WorldData.HeightSmooth(fx, fz);
            float wallW = RngRange(rng, 2f, 4f);
            float wallH = RngRange(rng, 1f, 3f);
            float wallD = RngRange(rng, 0.3f, 0.6f);
            float rotY = RngRange(rng, 0, 360);

            matrices.Add(Matrix4x4.TRS(
                new Vector3(fx, y + wallH * 0.5f, fz),
                Quaternion.Euler(RngRange(rng, -5f, 5f), rotY, RngRange(rng, -3f, 3f)),
                new Vector3(wallW, wallH, wallD)));

            float v = RngRange(rng, -0.04f, 0.04f);
            colors.Add(new Vector4(ruinStone.r + v, ruinStone.g + v, ruinStone.b + v, 1));
        }

        if (matrices.Count == 0) return;

        var go = new GameObject("RuinsProps");
        go.transform.SetParent(transform, false);
        var drawer = go.AddComponent<InstancedDrawer>();
        drawer.mesh = cubeMesh;
        drawer.material = propMaterial;
        drawer.matrices = matrices.ToArray();
        drawer.instanceColors = colors.ToArray();
    }

    // ── River reeds ─────────────────────────────────────────────────────
    void PlaceRiverReeds()
    {
        var rng = new System.Random(0x52454544);
        var reedMesh = BuildWheatMesh();
        var matrices = new List<Matrix4x4>();
        var colors = new List<Vector4>();
        Color reedColor = new Color(0.35f, 0.42f, 0.18f);

        for (int i = 0; i < 800; i++)
        {
            float fx = RngRange(rng, 2f, WorldData.SIZE - 2f);
            float fz = RngRange(rng, 2f, WorldData.SIZE - 2f);
            float rd = WorldData.RiverSDF(fx, fz);
            float ld = WorldData.LakeSDF(fx, fz);

            bool nearRiver = rd > 2f && rd < 5f;
            bool nearLake = ld > WorldData.LAKE_RADIUS * 0.8f && ld < WorldData.LAKE_RADIUS + 3f;
            if (!nearRiver && !nearLake) continue;

            float y = WorldData.HeightSmooth(fx, fz);
            float s = RngRange(rng, 0.6f, 1.0f);
            matrices.Add(Matrix4x4.TRS(
                new Vector3(fx, y, fz),
                Quaternion.Euler(RngRange(rng, -5f, 5f), RngRange(rng, 0, 360), 0),
                new Vector3(s, s * RngRange(rng, 0.8f, 1.5f), s)));

            float v = RngRange(rng, -0.04f, 0.04f);
            colors.Add(new Vector4(reedColor.r + v, reedColor.g + v, reedColor.b + v, 1));
        }

        if (matrices.Count == 0) return;

        var go = new GameObject("RiverReeds");
        go.transform.SetParent(transform, false);
        var drawer = go.AddComponent<InstancedDrawer>();
        drawer.mesh = reedMesh;
        drawer.material = propMaterial;
        drawer.matrices = matrices.ToArray();
        drawer.instanceColors = colors.ToArray();
        drawer.castShadows = false;
    }

    Mesh BuildCubeMesh()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var mesh = Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);
        Object.Destroy(go);
        return mesh;
    }

    static float RngRange(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }
}

public class InstancedDrawer : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public Matrix4x4[] matrices;
    public Vector4[] instanceColors;
    public bool castShadows = true;

    MaterialPropertyBlock[] _blocks;
    Matrix4x4[][] _batches;
    int _batchCount;

    void Start()
    {
        if (matrices == null || matrices.Length == 0) return;
        _batchCount = Mathf.CeilToInt(matrices.Length / 1023f);

        _batches = new Matrix4x4[_batchCount][];
        for (int b = 0; b < _batchCount; b++)
        {
            int start = b * 1023;
            int count = Mathf.Min(1023, matrices.Length - start);
            _batches[b] = new Matrix4x4[count];
            System.Array.Copy(matrices, start, _batches[b], 0, count);
        }

        if (instanceColors != null && instanceColors.Length > 0)
        {
            _blocks = new MaterialPropertyBlock[_batchCount];
            for (int b = 0; b < _batchCount; b++)
            {
                int start = b * 1023;
                int count = Mathf.Min(1023, matrices.Length - start);
                var cArr = new Vector4[count];
                System.Array.Copy(instanceColors, start, cArr, 0, count);
                _blocks[b] = new MaterialPropertyBlock();
                _blocks[b].SetVectorArray("_InstanceColor", cArr);
            }
        }
    }

    void Update()
    {
        if (mesh == null || material == null || _batches == null) return;

        var shadow = castShadows
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.Off;

        for (int b = 0; b < _batchCount; b++)
        {
            MaterialPropertyBlock block = (_blocks != null) ? _blocks[b] : null;
            Graphics.DrawMeshInstanced(mesh, 0, material, _batches[b], _batches[b].Length, block,
                shadow, receiveShadows: true, layer: gameObject.layer);
        }
    }
}
