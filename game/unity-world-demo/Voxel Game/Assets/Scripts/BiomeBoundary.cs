using System.Collections.Generic;
using UnityEngine;

public class BiomeBoundary : MonoBehaviour
{
    public Material lineMaterial;

    const float SAMPLE_STEP = 3.0f;
    const float LINE_WIDTH  = 0.15f;
    const float Y_OFFSET    = 0.06f;

    void Start()
    {
        if (WorldData.ARENA_MODE) return;
        BuildBoundaryMesh();
    }

    static bool IsOverlay(WorldData.Biome b)
    {
        return b == WorldData.Biome.River || b == WorldData.Biome.Road || b == WorldData.Biome.Pond;
    }

    static WorldData.Biome GetRegionBiome(float fx, float fz)
    {
        var b = WorldData.GetBiome(fx, fz);
        if (!IsOverlay(b)) return b;

        // Sample in a spiral to find the true underlying region
        float[] offsets = { 2f, 4f, 6f, 8f };
        float[] angles = { 0, 1.57f, 3.14f, 4.71f, 0.78f, 2.36f, 3.93f, 5.50f };
        for (int d = 0; d < offsets.Length; d++)
        {
            for (int a = 0; a < angles.Length; a++)
            {
                float tx = fx + Mathf.Cos(angles[a]) * offsets[d];
                float tz = fz + Mathf.Sin(angles[a]) * offsets[d];
                var test = WorldData.GetBiome(tx, tz);
                if (!IsOverlay(test)) return test;
            }
        }
        return b;
    }

    void BuildBoundaryMesh()
    {
        var verts = new List<Vector3>();
        var tris  = new List<int>();

        int stepsX = Mathf.CeilToInt(WorldData.SIZE / SAMPLE_STEP);
        int stepsZ = Mathf.CeilToInt(WorldData.SIZE / SAMPLE_STEP);

        for (int iz = 0; iz < stepsZ; iz++)
        {
            for (int ix = 0; ix < stepsX; ix++)
            {
                float fx = ix * SAMPLE_STEP;
                float fz = iz * SAMPLE_STEP;
                var biome = GetRegionBiome(fx, fz);

                if (ix < stepsX - 1)
                {
                    var eastBiome = GetRegionBiome(fx + SAMPLE_STEP, fz);
                    if (biome != eastBiome)
                        AddLineSegment(verts, tris, fx + SAMPLE_STEP, fz, fx + SAMPLE_STEP, fz + SAMPLE_STEP);
                }

                if (iz < stepsZ - 1)
                {
                    var northBiome = GetRegionBiome(fx, fz + SAMPLE_STEP);
                    if (biome != northBiome)
                        AddLineSegment(verts, tris, fx, fz + SAMPLE_STEP, fx + SAMPLE_STEP, fz + SAMPLE_STEP);
                }
            }
        }

        if (verts.Count == 0) return;

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var go = new GameObject("BoundaryLines");
        go.transform.SetParent(transform, false);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;
        mr.sharedMaterial = lineMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void AddLineSegment(List<Vector3> verts, List<int> tris,
        float ax, float az, float bx, float bz)
    {
        float hw = LINE_WIDTH * 0.5f;

        // Perpendicular offset direction
        float dx = bx - ax, dz = bz - az;
        float len = Mathf.Sqrt(dx * dx + dz * dz);
        if (len < 0.001f) return;
        float px = -dz / len * hw;
        float pz =  dx / len * hw;

        float ya = WorldData.HeightSmooth(ax, az) + Y_OFFSET;
        float yb = WorldData.HeightSmooth(bx, bz) + Y_OFFSET;

        Vector3 off = new Vector3(px, 0, pz);
        Vector3 pa = new Vector3(ax, ya, az);
        Vector3 pb = new Vector3(bx, yb, bz);

        int vi = verts.Count;
        verts.Add(pa - off);
        verts.Add(pa + off);
        verts.Add(pb - off);
        verts.Add(pb + off);

        tris.Add(vi);     tris.Add(vi + 2); tris.Add(vi + 1);
        tris.Add(vi + 1); tris.Add(vi + 2); tris.Add(vi + 3);
    }
}
