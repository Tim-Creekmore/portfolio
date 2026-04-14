using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class TerrainChunk : MonoBehaviour
{
    const int   GRID    = WorldData.GRID;
    const float STEP    = WorldData.STEP;
    const int   MAP     = WorldData.SIZE;
    const float SKIRT_Y = 0.0f;

    [Header("Materials")]
    public Material terrainMaterial;
    public Material waterMaterial;

    void Awake()
    {
        BuildTerrain();
        BuildWater();
    }

    // ── Per-biome ground colors ─────────────────────────────────────────
    static Color BiomeColor(WorldData.Biome biome, float slope)
    {
        Color grass  = new Color(0.40f, 0.52f, 0.26f);
        Color dirt   = new Color(0.55f, 0.44f, 0.30f);
        Color rock   = new Color(0.52f, 0.50f, 0.46f);

        switch (biome)
        {
            case WorldData.Biome.Beach:
                return Color.Lerp(
                    new Color(0.80f, 0.74f, 0.55f),
                    new Color(0.90f, 0.85f, 0.68f),
                    Mathf.Clamp01(slope));

            case WorldData.Biome.Farm:
                return Color.Lerp(
                    new Color(0.50f, 0.38f, 0.22f),
                    new Color(0.58f, 0.48f, 0.30f),
                    slope);

            case WorldData.Biome.Village:
                return Color.Lerp(
                    new Color(0.56f, 0.48f, 0.38f),
                    new Color(0.62f, 0.54f, 0.42f),
                    slope);

            case WorldData.Biome.Road:
                return Color.Lerp(
                    new Color(0.58f, 0.56f, 0.52f),
                    new Color(0.64f, 0.62f, 0.58f),
                    slope);

            case WorldData.Biome.Moor:
                return Color.Lerp(
                    new Color(0.44f, 0.38f, 0.30f),
                    new Color(0.50f, 0.46f, 0.34f),
                    slope);

            case WorldData.Biome.Cliff:
                return Color.Lerp(
                    new Color(0.52f, 0.48f, 0.42f),
                    new Color(0.60f, 0.56f, 0.48f),
                    Mathf.Clamp01((slope - 0.3f) / 0.5f));

            case WorldData.Biome.Rocky:
                return Color.Lerp(rock, dirt, Mathf.Clamp01((slope - 0.3f) / 0.3f));

            case WorldData.Biome.Thicket:
                return Color.Lerp(
                    new Color(0.28f, 0.32f, 0.18f),
                    new Color(0.38f, 0.40f, 0.24f),
                    slope);

            case WorldData.Biome.Ruins:
                return Color.Lerp(
                    new Color(0.50f, 0.48f, 0.42f),
                    new Color(0.44f, 0.46f, 0.34f),
                    slope);

            case WorldData.Biome.River:
            case WorldData.Biome.Pond:
                return dirt;

            default: // Meadow, Forest
                if (slope > 0.85f)
                    return grass;
                else if (slope > 0.6f)
                    return Color.Lerp(dirt, grass, (slope - 0.6f) / 0.25f);
                else
                    return Color.Lerp(rock, dirt, Mathf.Clamp01((slope - 0.3f) / 0.3f));
        }
    }

    void BuildTerrain()
    {
        int cols = GRID + 1;
        int rows = GRID + 1;
        int vertCount = cols * rows;

        float[] heights = new float[vertCount];
        for (int iz = 0; iz < rows; iz++)
            for (int ix = 0; ix < cols; ix++)
                heights[iz * cols + ix] = WorldData.HeightSmooth(ix * STEP, iz * STEP);

        int skirtExtra = cols * 4;
        var verts   = new Vector3[vertCount + skirtExtra];
        var normals = new Vector3[vertCount + skirtExtra];
        var colors  = new Color[vertCount + skirtExtra];

        for (int iz = 0; iz < rows; iz++)
        {
            for (int ix = 0; ix < cols; ix++)
            {
                float fx = ix * STEP;
                float fz = iz * STEP;
                float y  = heights[iz * cols + ix];
                int   vi = iz * cols + ix;

                verts[vi] = new Vector3(fx, y, fz);

                float eps = STEP;
                float hL = WorldData.HeightSmooth(fx - eps, fz);
                float hR = WorldData.HeightSmooth(fx + eps, fz);
                float hD = WorldData.HeightSmooth(fx, fz - eps);
                float hU = WorldData.HeightSmooth(fx, fz + eps);
                normals[vi] = new Vector3(hL - hR, 2.0f * eps, hD - hU).normalized;

                float slope = normals[vi].y;
                var biome = WorldData.GetBiome(fx, fz);

                float roadD = WorldData.RoadSDF(fx, fz);
                float roadBlendRadius = WorldData.ROAD_HALF_WIDTH_PUBLIC + 1.5f;

                if (roadD < roadBlendRadius)
                {
                    var baseBiome = (biome == WorldData.Biome.Road)
                        ? WorldData.GetBaseBiome(fx, fz) : biome;
                    Color baseCol = BiomeColor(baseBiome, slope);
                    Color roadCol = BiomeColor(WorldData.Biome.Road, slope);
                    float t = Mathf.Clamp01((roadD - WorldData.ROAD_HALF_WIDTH_PUBLIC * 0.5f)
                              / (roadBlendRadius - WorldData.ROAD_HALF_WIDTH_PUBLIC * 0.5f));
                    colors[vi] = Color.Lerp(roadCol, baseCol, t * t);
                }
                else
                {
                    colors[vi] = BiomeColor(biome, slope);
                }
            }
        }

        int quadCount = GRID * GRID;
        var indices = new int[quadCount * 6 + skirtExtra * 6];
        int idx = 0;

        for (int iz = 0; iz < GRID; iz++)
        {
            for (int ix = 0; ix < GRID; ix++)
            {
                int tl = iz * cols + ix;
                int tr = tl + 1;
                int bl = (iz + 1) * cols + ix;
                int br = bl + 1;
                indices[idx++] = tl;
                indices[idx++] = bl;
                indices[idx++] = tr;
                indices[idx++] = tr;
                indices[idx++] = bl;
                indices[idx++] = br;
            }
        }

        int vertUsed = vertCount;
        idx = AddSkirtEdge(verts, normals, colors, indices, heights, cols, rows, vertUsed, idx);
        vertUsed = vertCount + skirtExtra;

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = TrimArray(verts, vertUsed);
        mesh.normals  = TrimArray(normals, vertUsed);
        mesh.colors   = TrimArray(colors, vertUsed);
        mesh.triangles = TrimArray(indices, idx);
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshRenderer>().sharedMaterial = terrainMaterial;

        var collider = GetComponent<MeshCollider>();
        collider.sharedMesh = mesh;
    }

    int AddSkirtEdge(Vector3[] verts, Vector3[] normals, Color[] colors, int[] indices,
                     float[] heights, int cols, int rows, int vOff, int iOff)
    {
        Color cliffColor = new Color(0.36f, 0.30f, 0.22f);
        int si = vOff;
        int ii = iOff;

        // South edge (z = 0)
        for (int ix = 0; ix < cols; ix++)
        {
            int topIdx = ix;
            verts[si]   = new Vector3(ix * STEP, SKIRT_Y, 0.0f);
            normals[si] = Vector3.back;
            colors[si]  = cliffColor;
            if (ix > 0)
            {
                int prevTop = topIdx - 1;
                int prevBot = si - 1;
                indices[ii++] = prevTop; indices[ii++] = topIdx;  indices[ii++] = prevBot;
                indices[ii++] = topIdx;  indices[ii++] = si;      indices[ii++] = prevBot;
            }
            si++;
        }

        // North edge (z = MAP)
        for (int ix = 0; ix < cols; ix++)
        {
            int topIdx = GRID * cols + ix;
            verts[si]   = new Vector3(ix * STEP, SKIRT_Y, MAP);
            normals[si] = Vector3.forward;
            colors[si]  = cliffColor;
            if (ix > 0)
            {
                int prevTop = topIdx - 1;
                int prevBot = si - 1;
                indices[ii++] = topIdx;  indices[ii++] = prevTop; indices[ii++] = si;
                indices[ii++] = prevTop; indices[ii++] = prevBot; indices[ii++] = si;
            }
            si++;
        }

        // West edge (x = 0)
        for (int iz = 0; iz < cols; iz++)
        {
            int topIdx = iz * cols;
            verts[si]   = new Vector3(0.0f, SKIRT_Y, iz * STEP);
            normals[si] = Vector3.left;
            colors[si]  = cliffColor;
            if (iz > 0)
            {
                int prevTop = (iz - 1) * cols;
                int prevBot = si - 1;
                indices[ii++] = topIdx;  indices[ii++] = prevTop; indices[ii++] = si;
                indices[ii++] = prevTop; indices[ii++] = prevBot; indices[ii++] = si;
            }
            si++;
        }

        // East edge (x = MAP)
        for (int iz = 0; iz < cols; iz++)
        {
            int topIdx = iz * cols + GRID;
            verts[si]   = new Vector3(MAP, SKIRT_Y, iz * STEP);
            normals[si] = Vector3.right;
            colors[si]  = cliffColor;
            if (iz > 0)
            {
                int prevTop = (iz - 1) * cols + GRID;
                int prevBot = si - 1;
                indices[ii++] = prevTop; indices[ii++] = topIdx;  indices[ii++] = prevBot;
                indices[ii++] = topIdx;  indices[ii++] = si;      indices[ii++] = prevBot;
            }
            si++;
        }

        return ii;
    }

    // ── Unified water plane covering all water areas ─────────────────────
    void BuildWater()
    {
        float waterY = WorldData.WATER_Y;
        float cellSize = 2f;
        int gridW = Mathf.CeilToInt(MAP / cellSize);
        int gridH = gridW;

        var verts = new System.Collections.Generic.List<Vector3>();
        var norms = new System.Collections.Generic.List<Vector3>();
        var tris  = new System.Collections.Generic.List<int>();

        bool[,] hasWater = new bool[gridW + 1, gridH + 1];

        for (int gz = 0; gz <= gridH; gz++)
        for (int gx = 0; gx <= gridW; gx++)
        {
            float fx = gx * cellSize;
            float fz = gz * cellSize;
            float h = WorldData.HeightSmooth(fx, fz);
            hasWater[gx, gz] = h < waterY + 0.1f;
        }

        int[,] vertIdx = new int[gridW + 1, gridH + 1];
        for (int gz = 0; gz <= gridH; gz++)
        for (int gx = 0; gx <= gridW; gx++)
            vertIdx[gx, gz] = -1;

        for (int gz = 0; gz < gridH; gz++)
        for (int gx = 0; gx < gridW; gx++)
        {
            bool a = hasWater[gx, gz];
            bool b = hasWater[gx + 1, gz];
            bool c = hasWater[gx, gz + 1];
            bool d = hasWater[gx + 1, gz + 1];
            if (!(a || b || c || d)) continue;

            int[] corners = { Pack(gx, gz), Pack(gx + 1, gz), Pack(gx, gz + 1), Pack(gx + 1, gz + 1) };
            int[,] coords = { { gx, gz }, { gx + 1, gz }, { gx, gz + 1 }, { gx + 1, gz + 1 } };

            for (int k = 0; k < 4; k++)
            {
                int cx = coords[k, 0], cz = coords[k, 1];
                if (vertIdx[cx, cz] < 0)
                {
                    vertIdx[cx, cz] = verts.Count;
                    verts.Add(new Vector3(cx * cellSize, waterY, cz * cellSize));
                    norms.Add(Vector3.up);
                }
            }

            int v00 = vertIdx[gx, gz];
            int v10 = vertIdx[gx + 1, gz];
            int v01 = vertIdx[gx, gz + 1];
            int v11 = vertIdx[gx + 1, gz + 1];

            tris.Add(v00); tris.Add(v01); tris.Add(v10);
            tris.Add(v10); tris.Add(v01); tris.Add(v11);
        }

        if (verts.Count == 0) return;

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();

        var go = new GameObject("WaterMesh");
        go.transform.SetParent(transform, false);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;
        mr.sharedMaterial = waterMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static int Pack(int x, int z) { return x * 10000 + z; }

    static T[] TrimArray<T>(T[] source, int length)
    {
        if (source.Length == length) return source;
        var result = new T[length];
        System.Array.Copy(source, result, length);
        return result;
    }
}
