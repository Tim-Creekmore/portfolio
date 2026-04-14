using System.Collections.Generic;
using UnityEngine;

public static class TreeGenerator
{
    public enum Style { Pine = 0, Round = 1, Oak = 2 }

    public struct TreeMeshes
    {
        public Mesh wood;
        public Mesh leaves;
    }

    public static TreeMeshes Generate(Style style, System.Random rng)
    {
        var woodVerts  = new List<Vector3>();
        var woodNorms  = new List<Vector3>();
        var woodTris   = new List<int>();
        var leafVerts  = new List<Vector3>();
        var leafNorms  = new List<Vector3>();
        var leafTris   = new List<int>();

        switch (style)
        {
            case Style.Pine:
                GeneratePine(rng, woodVerts, woodNorms, woodTris, leafVerts, leafNorms, leafTris);
                break;
            case Style.Round:
                GenerateRound(rng, woodVerts, woodNorms, woodTris, leafVerts, leafNorms, leafTris);
                break;
            case Style.Oak:
                GenerateOak(rng, woodVerts, woodNorms, woodTris, leafVerts, leafNorms, leafTris);
                break;
        }

        var result = new TreeMeshes();
        result.wood = BuildMesh(woodVerts, woodNorms, woodTris);
        result.leaves = BuildMesh(leafVerts, leafNorms, leafTris);
        return result;
    }

    static Mesh BuildMesh(List<Vector3> verts, List<Vector3> normals, List<int> tris)
    {
        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetTriangles(tris, 0);
        if (normals.Count == 0 && verts.Count > 0)
            mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ── Pine ────────────────────────────────────────────────────────────────
    static void GeneratePine(System.Random rng,
        List<Vector3> wV, List<Vector3> wN, List<int> wT,
        List<Vector3> lV, List<Vector3> lN, List<int> lT)
    {
        float trunkH = RngRange(rng, 3.5f, 5.0f);
        float trunkR = RngRange(rng, 0.10f, 0.15f);
        AddCylinder(wV, wN, wT, Vector3.zero, trunkH, trunkR, trunkR * 0.6f, 8, Vector3.up);

        int layers = rng.Next(4, 7);
        float startY = trunkH * 0.25f;
        float layerSpan = trunkH - startY;

        for (int i = 0; i < layers; i++)
        {
            float t = (float)i / (layers - 1);
            float y = startY + t * layerSpan;
            float radius = Mathf.Lerp(RngRange(rng, 1.4f, 2.0f), 0.3f, t * t * 0.55f);
            float coneH = Mathf.Lerp(1.2f, 0.5f, t);
            AddCone(lV, lN, lT, new Vector3(0, y, 0), coneH, radius, 10);
        }
    }

    // ── Round ───────────────────────────────────────────────────────────────
    static void GenerateRound(System.Random rng,
        List<Vector3> wV, List<Vector3> wN, List<int> wT,
        List<Vector3> lV, List<Vector3> lN, List<int> lT)
    {
        float trunkH = RngRange(rng, 3.0f, 4.5f);
        float trunkR = RngRange(rng, 0.08f, 0.12f);
        AddCylinder(wV, wN, wT, Vector3.zero, trunkH, trunkR, trunkR * 0.7f, 8, Vector3.up);

        float canopyR = RngRange(rng, 1.5f, 2.2f);
        AddIcosphere(lV, lN, lT, new Vector3(0, trunkH, 0), canopyR, 2, rng);

        if (rng.NextDouble() < 0.7)
        {
            float r2 = canopyR * RngRange(rng, 0.5f, 0.75f);
            float offX = RngRange(rng, -1.0f, 1.0f);
            float offZ = RngRange(rng, -1.0f, 1.0f);
            AddIcosphere(lV, lN, lT, new Vector3(offX, trunkH + canopyR * 0.3f, offZ), r2, 2, rng);
        }
    }

    // ── Oak ─────────────────────────────────────────────────────────────────
    static void GenerateOak(System.Random rng,
        List<Vector3> wV, List<Vector3> wN, List<int> wT,
        List<Vector3> lV, List<Vector3> lN, List<int> lT)
    {
        float trunkH = RngRange(rng, 2.8f, 3.8f);
        float trunkR = RngRange(rng, 0.18f, 0.28f);
        float forkY  = trunkH * 0.55f;
        AddCylinder(wV, wN, wT, Vector3.zero, forkY, trunkR, trunkR * 0.8f, 8, Vector3.up);

        int branches = rng.Next(2, 5);
        for (int i = 0; i < branches; i++)
        {
            float angle = ((float)i / branches) * Mathf.PI * 2.0f + RngRange(rng, -0.3f, 0.3f);
            float lean  = RngRange(rng, 0.3f, 0.55f);
            Vector3 dir = new Vector3(Mathf.Sin(angle) * lean, 1.0f, Mathf.Cos(angle) * lean).normalized;
            float branchH = RngRange(rng, 1.5f, 2.5f);
            float branchR = trunkR * RngRange(rng, 0.35f, 0.55f);
            Vector3 branchBase = new Vector3(0, forkY, 0);
            AddCylinder(wV, wN, wT, branchBase, branchH, branchR, branchR * 0.5f, 6, dir);

            Vector3 tipPos = branchBase + dir * branchH;
            float blobR = RngRange(rng, 1.2f, 2.0f);
            AddIcosphere(lV, lN, lT, tipPos, blobR, 2, rng);
        }

        // Crown blob on top
        float crownR = RngRange(rng, 1.4f, 2.2f);
        AddIcosphere(lV, lN, lT, new Vector3(0, trunkH + crownR * 0.4f, 0), crownR, 2, rng);
    }

    // ── Cylinder ────────────────────────────────────────────────────────────
    static void AddCylinder(List<Vector3> verts, List<Vector3> normals, List<int> tris,
                            Vector3 basePos, float height, float botRadius, float topRadius,
                            int sides, Vector3 direction)
    {
        direction = direction.normalized;
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, direction);
        int rings = 4;
        int baseIdx = verts.Count;

        for (int r = 0; r <= rings; r++)
        {
            float t = (float)r / rings;
            float rad = Mathf.Lerp(botRadius, topRadius, t);
            Vector3 center = basePos + direction * (height * t);

            for (int s = 0; s < sides; s++)
            {
                float a = (float)s / sides * Mathf.PI * 2.0f;
                Vector3 localOffset = new Vector3(Mathf.Cos(a) * rad, 0, Mathf.Sin(a) * rad);
                Vector3 worldOffset = rot * localOffset;
                verts.Add(center + worldOffset);

                Vector3 localNorm = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                normals.Add((rot * localNorm).normalized);
            }
        }

        // Quad-strip tessellation — CW winding
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < sides; s++)
            {
                int cur  = baseIdx + r * sides + s;
                int next = baseIdx + r * sides + (s + 1) % sides;
                int curUp  = cur + sides;
                int nextUp = next + sides;

                tris.Add(cur);    tris.Add(curUp);  tris.Add(next);
                tris.Add(next);   tris.Add(curUp);  tris.Add(nextUp);
            }
        }
    }

    // ── Cone ────────────────────────────────────────────────────────────────
    static void AddCone(List<Vector3> verts, List<Vector3> normals, List<int> tris,
                        Vector3 basePos, float height, float radius, int sides)
    {
        int baseIdx = verts.Count;
        Vector3 tip = basePos + Vector3.up * height;

        // Base ring
        for (int s = 0; s < sides; s++)
        {
            float a = (float)s / sides * Mathf.PI * 2.0f;
            verts.Add(basePos + new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius));
            Vector3 n = new Vector3(Mathf.Cos(a), radius / height, Mathf.Sin(a)).normalized;
            normals.Add(n);
        }

        // Tip vertex (per-face would be nicer, but one shared tip is fine for low-poly)
        int tipIdx = verts.Count;
        verts.Add(tip);
        normals.Add(Vector3.up);

        // Side tris — CW winding
        for (int s = 0; s < sides; s++)
        {
            int cur  = baseIdx + s;
            int next = baseIdx + (s + 1) % sides;
            tris.Add(cur); tris.Add(tipIdx); tris.Add(next);
        }

        // Base cap — CW winding
        int centerIdx = verts.Count;
        verts.Add(basePos);
        normals.Add(Vector3.down);
        for (int s = 0; s < sides; s++)
        {
            int cur  = baseIdx + s;
            int next = baseIdx + (s + 1) % sides;
            tris.Add(next); tris.Add(centerIdx); tris.Add(cur);
        }
    }

    // ── Icosphere ───────────────────────────────────────────────────────────
    static void AddIcosphere(List<Vector3> verts, List<Vector3> normals, List<int> tris,
                             Vector3 center, float radius, int subdivisions, System.Random rng)
    {
        // Build the icosahedron
        float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;
        var icoVerts = new List<Vector3>
        {
            new Vector3(-1,  t, 0).normalized, new Vector3( 1,  t, 0).normalized,
            new Vector3(-1, -t, 0).normalized, new Vector3( 1, -t, 0).normalized,
            new Vector3(0, -1,  t).normalized, new Vector3(0,  1,  t).normalized,
            new Vector3(0, -1, -t).normalized, new Vector3(0,  1, -t).normalized,
            new Vector3( t, 0, -1).normalized, new Vector3( t, 0,  1).normalized,
            new Vector3(-t, 0, -1).normalized, new Vector3(-t, 0,  1).normalized,
        };

        var icoTris = new List<int>
        {
            0,11,5,  0,5,1,  0,1,7,  0,7,10, 0,10,11,
            1,5,9,   5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4,   3,4,2,  3,2,6,  3,6,8,  3,8,9,
            4,9,5,   2,4,11, 6,2,10, 8,6,7,  9,8,1,
        };

        // Subdivide
        for (int s = 0; s < subdivisions; s++)
        {
            var midpointCache = new Dictionary<long, int>();
            var newTris = new List<int>();

            for (int i = 0; i < icoTris.Count; i += 3)
            {
                int a = icoTris[i];
                int b = icoTris[i + 1];
                int c = icoTris[i + 2];
                int ab = GetMidpoint(icoVerts, midpointCache, a, b);
                int bc = GetMidpoint(icoVerts, midpointCache, b, c);
                int ca = GetMidpoint(icoVerts, midpointCache, c, a);

                newTris.Add(a);  newTris.Add(ab); newTris.Add(ca);
                newTris.Add(b);  newTris.Add(bc); newTris.Add(ab);
                newTris.Add(c);  newTris.Add(ca); newTris.Add(bc);
                newTris.Add(ab); newTris.Add(bc); newTris.Add(ca);
            }
            icoTris = newTris;
        }

        // Apply jitter, scale, and offset — then append to output lists
        int baseIdx = verts.Count;
        for (int i = 0; i < icoVerts.Count; i++)
        {
            Vector3 v = icoVerts[i];
            // ±6% jitter
            float jitter = 1.0f + RngRange(rng, -0.06f, 0.06f);
            v *= radius * jitter;
            verts.Add(center + v);
            normals.Add(icoVerts[i]); // pre-jitter normalized direction = normal
        }

        // CW winding
        for (int i = 0; i < icoTris.Count; i += 3)
        {
            tris.Add(baseIdx + icoTris[i]);
            tris.Add(baseIdx + icoTris[i + 2]);
            tris.Add(baseIdx + icoTris[i + 1]);
        }
    }

    static int GetMidpoint(List<Vector3> verts, Dictionary<long, int> cache, int a, int b)
    {
        long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        if (cache.TryGetValue(key, out int idx))
            return idx;
        Vector3 mid = ((verts[a] + verts[b]) * 0.5f).normalized;
        idx = verts.Count;
        verts.Add(mid);
        cache[key] = idx;
        return idx;
    }

    static float RngRange(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }
}
