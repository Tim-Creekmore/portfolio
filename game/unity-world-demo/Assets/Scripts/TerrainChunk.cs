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

    void Start()
    {
        BuildTerrain();
        BuildWater();
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

        // Pre-allocate with extra room for skirt verts (4 edges × cols each)
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
                Color grass = new Color(0.28f, 0.42f, 0.18f);
                Color dirt  = new Color(0.42f, 0.30f, 0.18f);
                Color rock  = new Color(0.38f, 0.34f, 0.30f);

                if (slope > 0.85f)
                    colors[vi] = grass;
                else if (slope > 0.6f)
                    colors[vi] = Color.Lerp(dirt, grass, (slope - 0.6f) / 0.25f);
                else
                    colors[vi] = Color.Lerp(rock, dirt, Mathf.Clamp01((slope - 0.3f) / 0.3f));
            }
        }

        // Triangle indices — Unity CW winding
        int quadCount = GRID * GRID;
        // 6 indices per quad for terrain + estimate skirt indices
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
                // CW winding (Godot was CCW: tl,tr,bl / tr,br,bl → flip to tl,bl,tr / tr,bl,br)
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

        // Trim arrays to actual used size
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
                // CW winding (flipped from Godot CCW)
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

    void BuildWater()
    {
        float waterY = WorldData.WATER_Y;
        int res = 32;
        float step = (float)MAP / res;

        var verts   = new System.Collections.Generic.List<Vector3>();
        var norms   = new System.Collections.Generic.List<Vector3>();
        var cols    = new System.Collections.Generic.List<Color>();
        var tris    = new System.Collections.Generic.List<int>();

        for (int iz = 0; iz < res; iz++)
        {
            for (int ix = 0; ix < res; ix++)
            {
                float fx  = ix * step;
                float fz  = iz * step;
                float fx1 = fx + step;
                float fz1 = fz + step;
                float cx  = (fx + fx1) * 0.5f;
                float cz  = (fz + fz1) * 0.5f;

                if (!WorldData.IsRiver(cx, cz))
                    continue;

                Color c = new Color(0.18f, 0.52f, 0.68f, 0.8f);
                int vi = verts.Count;

                // Quad as 2 tris — CW winding
                verts.Add(new Vector3(fx,  waterY, fz));
                verts.Add(new Vector3(fx1, waterY, fz));
                verts.Add(new Vector3(fx,  waterY, fz1));
                verts.Add(new Vector3(fx1, waterY, fz));
                verts.Add(new Vector3(fx1, waterY, fz1));
                verts.Add(new Vector3(fx,  waterY, fz1));

                for (int v = 0; v < 6; v++)
                {
                    norms.Add(Vector3.up);
                    cols.Add(c);
                }

                // CW: 0,2,1 and 3,5,4
                tris.Add(vi);     tris.Add(vi + 2); tris.Add(vi + 1);
                tris.Add(vi + 3); tris.Add(vi + 5); tris.Add(vi + 4);
            }
        }

        if (verts.Count == 0) return;

        var waterMesh = new Mesh();
        waterMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        waterMesh.SetVertices(verts);
        waterMesh.SetNormals(norms);
        waterMesh.SetColors(cols);
        waterMesh.SetTriangles(tris, 0);
        waterMesh.RecalculateBounds();

        var waterGO = new GameObject("WaterMesh");
        waterGO.transform.SetParent(transform, false);
        var mf = waterGO.AddComponent<MeshFilter>();
        var mr = waterGO.AddComponent<MeshRenderer>();
        mf.sharedMesh = waterMesh;
        mr.sharedMaterial = waterMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static T[] TrimArray<T>(T[] source, int length)
    {
        if (source.Length == length) return source;
        var result = new T[length];
        System.Array.Copy(source, result, length);
        return result;
    }
}
