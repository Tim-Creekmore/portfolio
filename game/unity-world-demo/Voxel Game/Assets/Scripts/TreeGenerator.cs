using System.Collections.Generic;
using UnityEngine;

public static class TreeGenerator
{
    public enum Style { Pine = 0, Round = 1, Oak = 2, TallPine = 3, Birch = 4, Bush = 5, Weeping = 6, Gnarly = 7, Sapling = 8 }

    public enum Category { Mature, Sapling, Bush }

    public static Category GetCategory(Style s)
    {
        switch (s)
        {
            case Style.Bush:    return Category.Bush;
            case Style.Sapling: return Category.Sapling;
            default:            return Category.Mature;
        }
    }

    public struct TreeMeshes
    {
        public Mesh wood;
        public Mesh leaves;
    }

    const float VS = 0.3f;
    const byte AIR = 0, BARK = 1, LEAF = 2;

    struct Grid
    {
        public byte[] cells;
        public int sx, sy, sz;
        public byte Get(int x, int y, int z)
        {
            if (x < 0 || x >= sx || y < 0 || y >= sy || z < 0 || z >= sz) return AIR;
            return cells[x + y * sx + z * sx * sy];
        }
        public void Set(int x, int y, int z, byte v)
        {
            if (x < 0 || x >= sx || y < 0 || y >= sy || z < 0 || z >= sz) return;
            cells[x + y * sx + z * sx * sy] = v;
        }
        public int Neighbors(int x, int y, int z)
        {
            int c = 0;
            if (Get(x - 1, y, z) != AIR) c++;
            if (Get(x + 1, y, z) != AIR) c++;
            if (Get(x, y - 1, z) != AIR) c++;
            if (Get(x, y + 1, z) != AIR) c++;
            if (Get(x, y, z - 1) != AIR) c++;
            if (Get(x, y, z + 1) != AIR) c++;
            return c;
        }
    }

    public static TreeMeshes Generate(Style style, System.Random rng, Color leafColor)
    {
        Grid g;
        switch (style)
        {
            case Style.Pine:     g = FillPine(rng);     break;
            case Style.Oak:      g = FillOak(rng);      break;
            case Style.TallPine: g = FillTallPine(rng); break;
            case Style.Birch:    g = FillBirch(rng);    break;
            case Style.Bush:     g = FillBush(rng);     break;
            case Style.Weeping:  g = FillWeeping(rng);  break;
            case Style.Gnarly:   g = FillGnarly(rng);   break;
            case Style.Sapling:  g = FillSapling(rng);  break;
            default:             g = FillRound(rng);    break;
        }

        Solidify(ref g, LEAF);

        Color barkBase = (style == Style.Birch)
            ? new Color(0.62f, 0.58f, 0.52f)
            : new Color(0.35f, 0.24f, 0.14f);

        var wood   = BuildVoxelMesh(g, BARK, barkBase, rng);
        var leaves = BuildVoxelMesh(g, LEAF, leafColor, rng);
        return new TreeMeshes { wood = wood, leaves = leaves };
    }

    public static TreeMeshes Generate(Style style, System.Random rng)
    {
        return Generate(style, rng, new Color(0.16f, 0.28f, 0.10f));
    }

    static void Solidify(ref Grid g, byte type)
    {
        for (int z = 1; z < g.sz - 1; z++)
            for (int y = 1; y < g.sy - 1; y++)
                for (int x = 1; x < g.sx - 1; x++)
                {
                    if (g.Get(x, y, z) != AIR) continue;
                    int n = 0;
                    if (g.Get(x - 1, y, z) == type) n++;
                    if (g.Get(x + 1, y, z) == type) n++;
                    if (g.Get(x, y - 1, z) == type) n++;
                    if (g.Get(x, y + 1, z) == type) n++;
                    if (g.Get(x, y, z - 1) == type) n++;
                    if (g.Get(x, y, z + 1) == type) n++;
                    if (n >= 4) g.Set(x, y, z, type);
                }
    }

    // ── Trunk helpers ──────────────────────────────────────────────────────

    static void FillTrunkColumn(Grid g, int cx, int cz, int height, int thickness)
    {
        int half = thickness / 2;
        for (int y = 0; y < height; y++)
            for (int dz = 0; dz < thickness; dz++)
                for (int dx = 0; dx < thickness; dx++)
                    g.Set(cx - half + dx, y, cz - half + dz, BARK);
    }

    static void AddForks(Grid g, int cx, int cz, int forkY, System.Random rng, int count, int length)
    {
        int[][] cardinals = { new[]{1,0}, new[]{-1,0}, new[]{0,1}, new[]{0,-1} };
        var used = new List<int>();

        for (int i = 0; i < count && used.Count < 4; i++)
        {
            int dirIdx;
            do { dirIdx = rng.Next(4); } while (used.Contains(dirIdx));
            used.Add(dirIdx);

            var dir = cardinals[dirIdx];
            int bx = cx, bz = cz;
            for (int step = 0; step < length; step++)
            {
                bx += dir[0];
                bz += dir[1];
                int by = forkY + step;
                g.Set(bx, by, bz, BARK);
                g.Set(bx, by + 1, bz, BARK);
            }
        }
    }

    static void AddRootFlare(Grid g, int cx, int cz, int thickness)
    {
        int half = thickness / 2;
        for (int dx = -half - 1; dx <= half + 1; dx++)
            for (int dz = -half - 1; dz <= half + 1; dz++)
            {
                if (Mathf.Abs(dx) == half + 1 && Mathf.Abs(dz) == half + 1) continue;
                g.Set(cx + dx, 0, cz + dz, BARK);
            }
    }

    // ── Pine ───────────────────────────────────────────────────────────────

    static Grid FillPine(System.Random rng)
    {
        int sx = 14, sz = 14, sy = 26;
        var g = NewGrid(sx, sy, sz);
        int cx = sx / 2, cz = sz / 2;

        int trunkH = Ri(rng, 16, 22);
        FillTrunkColumn(g, cx, cz, trunkH, 1);

        int canopyStart = Ri(rng, 8, 11);
        float maxR = Rf(rng, 4.5f, 6.0f);
        for (int y = canopyStart; y < sy - 1; y++)
        {
            float t = (float)(y - canopyStart) / (sy - 1 - canopyStart);
            float r = (1f - t) * maxR + 0.5f;
            FillDiscXZ(g, cx, y, cz, r, LEAF);
        }
        g.Set(cx, sy - 1, cz, LEAF);

        return g;
    }

    // ── Tall Pine ──────────────────────────────────────────────────────────

    static Grid FillTallPine(System.Random rng)
    {
        int sx = 10, sz = 10, sy = 32;
        var g = NewGrid(sx, sy, sz);
        int cx = sx / 2, cz = sz / 2;

        int trunkH = Ri(rng, 24, 30);
        FillTrunkColumn(g, cx, cz, trunkH, 1);

        int canopyStart = Ri(rng, 12, 18);
        float maxR = Rf(rng, 2.5f, 3.8f);
        for (int y = canopyStart; y < sy - 1; y++)
        {
            float t = (float)(y - canopyStart) / (sy - 1 - canopyStart);
            float r = (1f - t) * maxR + 0.3f;
            FillDiscXZ(g, cx, y, cz, r, LEAF);
        }
        g.Set(cx, sy - 1, cz, LEAF);

        return g;
    }

    // ── Round ──────────────────────────────────────────────────────────────

    static Grid FillRound(System.Random rng)
    {
        int sx = 16, sz = 16, sy = 24;
        var g = NewGrid(sx, sy, sz);
        int cx = sx / 2, cz = sz / 2;

        int trunkH = Ri(rng, 10, 14);
        FillTrunkColumn(g, cx, cz, trunkH, 2);

        float crownCY = trunkH + 3f;
        float crownR = Rf(rng, 4.5f, 6.0f);
        float crownRY = crownR * Rf(rng, 0.75f, 1.0f);

        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                {
                    float dx = x - cx;
                    float dy = (y - crownCY) / crownRY * crownR;
                    float dz = z - cz;
                    float d = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
                    if (d < crownR - 0.3f && g.Get(x, y, z) == AIR)
                        g.Set(x, y, z, LEAF);
                }

        return g;
    }

    // ── Oak ────────────────────────────────────────────────────────────────

    static Grid FillOak(System.Random rng)
    {
        int sx = 20, sz = 20, sy = 22;
        var g = NewGrid(sx, sy, sz);
        int cx = sx / 2, cz = sz / 2;

        int trunkH = Ri(rng, 9, 12);
        FillTrunkColumn(g, cx, cz, trunkH, 2);
        AddRootFlare(g, cx, cz, 2);
        AddForks(g, cx, cz, trunkH - 1, rng, Ri(rng, 2, 3), 3);

        float crownCY = trunkH + 2f;
        float spreadX = Rf(rng, 6.0f, 8.0f);
        float spreadZ = spreadX * Rf(rng, 0.9f, 1.1f);
        float crownRY = Rf(rng, 2.5f, 4.0f);

        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                {
                    float dx = (x - cx) / spreadX;
                    float dy = (y - crownCY) / crownRY;
                    float dz = (z - cz) / spreadZ;
                    float d = dx * dx + dy * dy + dz * dz;
                    if (d < 0.92f && g.Get(x, y, z) == AIR)
                        g.Set(x, y, z, LEAF);
                }

        return g;
    }

    // ── Birch ──────────────────────────────────────────────────────────────

    static Grid FillBirch(System.Random rng)
    {
        int sx = 10, sz = 10, sy = 24;
        var g = NewGrid(sx, sy, sz);
        int cx = sx / 2, cz = sz / 2;

        int trunkH = Ri(rng, 14, 20);
        FillTrunkColumn(g, cx, cz, trunkH, 1);

        float crownCY = trunkH - 1f;
        float crownR = Rf(rng, 3.0f, 4.0f);
        float crownRY = crownR * Rf(rng, 1.2f, 1.8f);

        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                {
                    float dx = x - cx;
                    float dy = (y - crownCY) / crownRY * crownR;
                    float dz = z - cz;
                    float d = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
                    if (d < crownR - 0.2f && g.Get(x, y, z) == AIR)
                        g.Set(x, y, z, LEAF);
                }

        return g;
    }

    // ── Bush (ground-level shrub, no visible trunk) ────────────────────────

    static Grid FillBush(System.Random rng)
    {
        int sx = 10, sz = 10, sy = 6;
        var g = NewGrid(sx, sy, sz);
        int cx = sx / 2, cz = sz / 2;

        float crownCY = 2.0f;
        float spreadXZ = Rf(rng, 3.5f, 4.5f);
        float crownRY = Rf(rng, 1.5f, 2.2f);

        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                {
                    float dx = (x - cx) / spreadXZ;
                    float dy = (y - crownCY) / crownRY;
                    float dz = (z - cz) / spreadXZ;
                    float d = dx * dx + dy * dy + dz * dz;
                    if (d < 0.85f)
                        g.Set(x, y, z, LEAF);
                }

        return g;
    }

    // ── Sapling (skinny young tree) ────────────────────────────────────────

    static Grid FillSapling(System.Random rng)
    {
        int sx = 8, sz = 8, sy = 14;
        var g = NewGrid(sx, sy, sz);
        int cx = sx / 2, cz = sz / 2;

        int trunkH = Ri(rng, 6, 9);
        for (int y = 0; y < trunkH; y++)
            g.Set(cx, y, cz, BARK);

        float crownCY = trunkH;
        float crownR = Rf(rng, 2.0f, 3.0f);
        float crownRY = crownR * Rf(rng, 1.0f, 1.6f);

        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                {
                    float dx = x - cx;
                    float dy = (y - crownCY) / crownRY * crownR;
                    float dz = z - cz;
                    float d = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
                    if (d < crownR - 0.3f && g.Get(x, y, z) == AIR)
                        g.Set(x, y, z, LEAF);
                }

        return g;
    }

    // ── Weeping ────────────────────────────────────────────────────────────

    static Grid FillWeeping(System.Random rng)
    {
        int sx = 16, sz = 16, sy = 26;
        var g = NewGrid(sx, sy, sz);
        int cx = sx / 2, cz = sz / 2;

        int trunkH = Ri(rng, 12, 16);
        FillTrunkColumn(g, cx, cz, trunkH, 2);

        float topY = trunkH + 1f;
        float maxSpread = Rf(rng, 5.5f, 7.0f);

        for (int layer = 0; layer < 5; layer++)
        {
            float ly = topY - layer * 1.5f;
            float r = maxSpread - layer * 0.3f;
            int iy = Mathf.RoundToInt(ly);
            FillDiscXZ(g, cx, iy, cz, r, LEAF);
            if (layer > 0)
                FillDiscXZ(g, cx, iy - 1, cz, r - 0.5f, LEAF);
        }

        int dripBase = Mathf.RoundToInt(topY - 4f * 1.5f);
        for (int i = 0; i < 12; i++)
        {
            float angle = Rf(rng, 0, Mathf.PI * 2f);
            float dist = Rf(rng, 3f, maxSpread);
            int dx = Mathf.RoundToInt(cx + Mathf.Cos(angle) * dist);
            int dz = Mathf.RoundToInt(cz + Mathf.Sin(angle) * dist);
            int dripLen = Ri(rng, 2, 5);
            for (int dy = 0; dy < dripLen; dy++)
                g.Set(dx, dripBase - dy, dz, LEAF);
        }

        return g;
    }

    // ── Gnarly ─────────────────────────────────────────────────────────────

    static Grid FillGnarly(System.Random rng)
    {
        int sx = 18, sz = 18, sy = 22;
        var g = NewGrid(sx, sy, sz);
        int cx = sx / 2, cz = sz / 2;

        int trunkH = Ri(rng, 9, 12);
        FillTrunkColumn(g, cx, cz, trunkH, 3);
        AddRootFlare(g, cx, cz, 3);
        AddForks(g, cx, cz, trunkH - 2, rng, Ri(rng, 2, 4), 3);

        int lobes = Ri(rng, 3, 5);
        for (int i = 0; i < lobes; i++)
        {
            float angle = Rf(rng, 0, Mathf.PI * 2f);
            float dist = Rf(rng, 1.5f, 4f);
            float lobeCX = cx + Mathf.Cos(angle) * dist;
            float lobeCZ = cz + Mathf.Sin(angle) * dist;
            float lobeCY = trunkH + Rf(rng, 0.5f, 3f);
            float lobeR = Rf(rng, 2.5f, 4.0f);

            for (int z = 0; z < sz; z++)
                for (int y = 0; y < sy; y++)
                    for (int x = 0; x < sx; x++)
                    {
                        float ddx = x - lobeCX;
                        float ddy = y - lobeCY;
                        float ddz = z - lobeCZ;
                        float d = Mathf.Sqrt(ddx * ddx + ddy * ddy + ddz * ddz);
                        if (d < lobeR && g.Get(x, y, z) == AIR)
                            g.Set(x, y, z, LEAF);
                    }
        }

        return g;
    }

    // ── Disc fill ──────────────────────────────────────────────────────────

    static void FillDiscXZ(Grid g, int cx, int y, int cz, float radius, byte val)
    {
        int r = Mathf.CeilToInt(radius);
        for (int dz = -r; dz <= r; dz++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (dx * dx + dz * dz <= radius * radius)
                    g.Set(cx + dx, y, cz + dz, val);
            }
    }

    // ── Voxel mesh builder ─────────────────────────────────────────────────

    static readonly Vector3Int[] FaceNormals =
    {
        new Vector3Int( 0,  1,  0),
        new Vector3Int( 0, -1,  0),
        new Vector3Int( 1,  0,  0),
        new Vector3Int(-1,  0,  0),
        new Vector3Int( 0,  0,  1),
        new Vector3Int( 0,  0, -1),
    };

    static readonly Vector3[][] FaceVerts =
    {
        new[] { new Vector3(0,1,0), new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,1,0) },
        new[] { new Vector3(0,0,1), new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,0,1) },
        new[] { new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(1,1,1), new Vector3(1,0,1) },
        new[] { new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(0,1,0), new Vector3(0,0,0) },
        new[] { new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1), new Vector3(0,0,1) },
        new[] { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,0,0) },
    };

    static readonly float[] FaceBright = { 1.0f, 0.65f, 0.85f, 0.82f, 0.88f, 0.78f };

    static Mesh BuildVoxelMesh(Grid g, byte voxelType, Color baseColor, System.Random rng)
    {
        var verts  = new List<Vector3>();
        var norms  = new List<Vector3>();
        var colors = new List<Color>();
        var tris   = new List<int>();

        float ox = -g.sx * 0.5f * VS;
        float oz = -g.sz * 0.5f * VS;

        for (int z = 0; z < g.sz; z++)
            for (int y = 0; y < g.sy; y++)
                for (int x = 0; x < g.sx; x++)
                {
                    if (g.Get(x, y, z) != voxelType) continue;

                    int nCount = g.Neighbors(x, y, z);
                    float ao = Mathf.Lerp(0.70f, 1.0f, 1f - nCount / 6f);
                    float hueShift = Rf(rng, -0.04f, 0.04f);
                    Color voxCol = new Color(
                        Mathf.Clamp01(baseColor.r + hueShift * 0.4f),
                        Mathf.Clamp01(baseColor.g + hueShift),
                        Mathf.Clamp01(baseColor.b - hueShift * 0.3f));

                    for (int f = 0; f < 6; f++)
                    {
                        var fn = FaceNormals[f];
                        int nx = x + fn.x, ny = y + fn.y, nz = z + fn.z;
                        if (g.Get(nx, ny, nz) != AIR) continue;

                        float bright = FaceBright[f] * ao;
                        Color faceCol = new Color(
                            Mathf.Clamp01(voxCol.r * bright),
                            Mathf.Clamp01(voxCol.g * bright),
                            Mathf.Clamp01(voxCol.b * bright));

                        Vector3 n3 = new Vector3(fn.x, fn.y, fn.z);
                        var fv = FaceVerts[f];
                        int vi = verts.Count;

                        for (int v = 0; v < 4; v++)
                        {
                            verts.Add(new Vector3(
                                ox + (x + fv[v].x) * VS,
                                y * VS + fv[v].y * VS,
                                oz + (z + fv[v].z) * VS));
                            norms.Add(n3);
                            colors.Add(faceCol);
                        }

                        tris.Add(vi);     tris.Add(vi + 1); tris.Add(vi + 2);
                        tris.Add(vi);     tris.Add(vi + 2); tris.Add(vi + 3);
                    }
                }

        var mesh = new Mesh();
        if (verts.Count > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetColors(colors);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    static Grid NewGrid(int sx, int sy, int sz)
    {
        return new Grid { cells = new byte[sx * sy * sz], sx = sx, sy = sy, sz = sz };
    }

    static float Rf(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);
    static int Ri(System.Random r, int a, int b) => r.Next(a, b + 1);
}
