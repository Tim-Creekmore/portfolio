using System.Collections.Generic;
using UnityEngine;

public class FoliagePlacer : MonoBehaviour
{
    [Header("Grass")]
    public Material grassBladeMaterial;
    public Material grassImpostorMaterial;

    [Header("Trees")]
    public Material canopyMaterial;
    public Material trunkMaterial;

    [Header("Rocks")]
    public Material rockMaterial;

    [Header("Wildflowers")]
    public Material wildflowerMaterial;

    static readonly Color[] FLOWER_COLORS =
    {
        new Color(0.95f, 0.90f, 0.30f),
        new Color(1.00f, 1.00f, 0.85f),
        new Color(0.65f, 0.40f, 0.80f),
        new Color(0.90f, 0.35f, 0.40f),
        new Color(0.95f, 0.65f, 0.20f),
        new Color(0.50f, 0.60f, 0.90f),
    };

    static readonly Color[] TREE_COLORS =
    {
        new Color(0.22f, 0.40f, 0.14f),
        new Color(0.26f, 0.44f, 0.18f),
        new Color(0.30f, 0.48f, 0.16f),
        new Color(0.34f, 0.42f, 0.12f),
        new Color(0.50f, 0.52f, 0.10f),
        new Color(0.58f, 0.44f, 0.08f),
        new Color(0.62f, 0.30f, 0.08f),
        new Color(0.55f, 0.22f, 0.06f),
    };

    void Start()
    {
        Invoke(nameof(PlaceAll), 0f);
    }

    void PlaceAll()
    {
        PlaceGrassImpostor();
        PlaceTrees();
        PlaceRocks();
        PlaceGrass();
        PlaceWildflowers();
    }

    // ── Grass Impostor ──────────────────────────────────────────────────────
    void PlaceGrassImpostor()
    {
        int res = 48;
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

                if (WorldData.IsRiver(mx, mz)) continue;
                if (WorldData.RiverSDF(mx, mz) < 0.5f) continue;

                float y00 = WorldData.HeightSmooth(x0, z0) + 0.01f;
                float y10 = WorldData.HeightSmooth(x1, z0) + 0.01f;
                float y01 = WorldData.HeightSmooth(x0, z1) + 0.01f;
                float y11 = WorldData.HeightSmooth(x1, z1) + 0.01f;

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

                // CW winding
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
        mesh.RecalculateBounds();

        var go = new GameObject("GrassImpostor");
        go.transform.SetParent(transform, false);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;
        mr.sharedMaterial = grassImpostorMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ── Trees ───────────────────────────────────────────────────────────────
    void PlaceTrees()
    {
        var rng = new System.Random(0x54524545);

        for (int i = 0; i < 22; i++)
        {
            float fx = RngRange(rng, 0.8f, 15.2f);
            float fz = RngRange(rng, 0.8f, 15.2f);
            if (WorldData.IsRiver(fx, fz)) continue;
            if (WorldData.RiverSDF(fx, fz) < 1.2f) continue;

            float y = WorldData.HeightSmooth(fx, fz);

            var style = (TreeGenerator.Style)(rng.Next(0, 3));
            var meshes = TreeGenerator.Generate(style, new System.Random(rng.Next()));

            float s = RngRange(rng, 0.6f, 1.15f);
            float sy = s * RngRange(rng, 0.85f, 1.15f);
            float rotY = (float)(rng.NextDouble() * Mathf.PI * 2.0f);

            Color leafColor = TREE_COLORS[rng.Next(TREE_COLORS.Length)];
            float lighten = RngRange(rng, -0.06f, 0.06f);
            leafColor = new Color(
                Mathf.Clamp01(leafColor.r + lighten),
                Mathf.Clamp01(leafColor.g + lighten),
                Mathf.Clamp01(leafColor.b + lighten));

            var treeGO = new GameObject($"Tree_{i}");
            treeGO.transform.SetParent(transform, false);
            treeGO.transform.position = new Vector3(fx, y, fz);
            treeGO.transform.localScale = new Vector3(s, sy, s);
            treeGO.transform.rotation = Quaternion.Euler(0, rotY * Mathf.Rad2Deg, 0);

            float windStr = RngRange(rng, 0.2f, 0.4f);
            float windSpd = RngRange(rng, 0.8f, 1.4f);

            // Trunk
            var trunkGO = new GameObject("Trunk");
            trunkGO.transform.SetParent(treeGO.transform, false);
            var tmf = trunkGO.AddComponent<MeshFilter>();
            var tmr = trunkGO.AddComponent<MeshRenderer>();
            tmf.sharedMesh = meshes.wood;
            var tMat = new Material(trunkMaterial);
            tMat.SetFloat("_SwaySpeed", windSpd);
            tMat.SetFloat("_SwayStrength", 0.006f);
            tmr.sharedMaterial = tMat;

            // Canopy
            var canopyGO = new GameObject("Canopy");
            canopyGO.transform.SetParent(treeGO.transform, false);
            var cmf = canopyGO.AddComponent<MeshFilter>();
            var cmr = canopyGO.AddComponent<MeshRenderer>();
            cmf.sharedMesh = meshes.leaves;
            var cMat = new Material(canopyMaterial);
            cMat.SetVector("_LeafColor", new Vector4(leafColor.r, leafColor.g, leafColor.b, 1));
            cMat.SetFloat("_WindStrength", windStr);
            cMat.SetFloat("_WindSpeed", windSpd);
            cmr.sharedMaterial = cMat;
        }
    }

    // ── Rocks ───────────────────────────────────────────────────────────────
    void PlaceRocks()
    {
        var rng = new System.Random(0x524f434b);
        var rockMesh = CreateRockMesh();
        var matrices = new List<Matrix4x4>();

        for (int i = 0; i < 30; i++)
        {
            float fx = RngRange(rng, 0.5f, 15.5f);
            float fz = RngRange(rng, 0.5f, 15.5f);
            if (WorldData.IsRiver(fx, fz)) continue;

            float y = WorldData.HeightSmooth(fx, fz);
            Vector3 pos = new Vector3(fx, y + 0.05f, fz);
            Quaternion rot = Quaternion.Euler(
                RngRange(rng, -17f, 17f),
                RngRange(rng, 0, 360f),
                RngRange(rng, -17f, 17f));
            Vector3 scale = new Vector3(
                RngRange(rng, 0.5f, 1.4f),
                RngRange(rng, 0.4f, 0.9f),
                RngRange(rng, 0.5f, 1.3f));

            matrices.Add(Matrix4x4.TRS(pos, rot, scale));
        }

        if (matrices.Count == 0) return;

        // Use Graphics.DrawMeshInstanced in batches of 1023
        var go = new GameObject("Rocks");
        go.transform.SetParent(transform, false);
        var drawer = go.AddComponent<InstancedDrawer>();
        drawer.mesh = rockMesh;
        drawer.material = rockMaterial;
        drawer.matrices = matrices.ToArray();
    }

    Mesh CreateRockMesh()
    {
        // Low-poly sphere approximation (UV sphere)
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var mesh = Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);
        Object.Destroy(go);
        // Scale vertices to match Godot's SphereMesh (radius 0.3, height 0.45)
        var verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i].x *= 0.6f;  // radius 0.3
            verts[i].y *= 0.45f; // half-height 0.225
            verts[i].z *= 0.6f;
        }
        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ── Grass Blades ────────────────────────────────────────────────────────
    void PlaceGrass()
    {
        var rng = new System.Random(0x47524153);
        var bladeMesh = BuildGrassBlade();
        var matrices = new List<Matrix4x4>();

        for (int i = 0; i < 50000; i++)
        {
            float fx = RngRange(rng, 0.2f, 15.8f);
            float fz = RngRange(rng, 0.2f, 15.8f);
            if (WorldData.IsRiver(fx, fz)) continue;
            if (WorldData.RiverSDF(fx, fz) < 0.4f) continue;

            float y = WorldData.HeightSmooth(fx, fz);
            Quaternion rot = Quaternion.Euler(
                RngRange(rng, -5.7f, 5.7f),
                RngRange(rng, 0, 360f),
                RngRange(rng, -5.7f, 5.7f));

            matrices.Add(Matrix4x4.TRS(new Vector3(fx, y, fz), rot, Vector3.one));
        }

        if (matrices.Count == 0) return;

        var go = new GameObject("GrassBlades");
        go.transform.SetParent(transform, false);
        var drawer = go.AddComponent<InstancedDrawer>();
        drawer.mesh = bladeMesh;
        drawer.material = grassBladeMaterial;
        drawer.matrices = matrices.ToArray();
        drawer.castShadows = false;
    }

    Mesh BuildGrassBlade()
    {
        int segs = 5;
        float bladeW = 0.055f;
        float bladeH = 1.0f;

        var verts   = new List<Vector3>();
        var norms   = new List<Vector3>();
        var uvsList = new List<Vector2>();
        var tris    = new List<int>();

        for (int s = 0; s < segs; s++)
        {
            float t0 = (float)s / segs;
            float t1 = (float)(s + 1) / segs;
            float y0 = t0 * bladeH;
            float y1 = t1 * bladeH;
            float w0 = bladeW * (1.0f - t0 * 0.85f);
            float w1 = bladeW * (1.0f - t1 * 0.85f);
            float uvV0 = 1.0f - t0;
            float uvV1 = 1.0f - t1;

            Vector3 l0 = new Vector3(-w0, y0, 0);
            Vector3 r0 = new Vector3(w0, y0, 0);
            Vector3 l1 = new Vector3(-w1, y1, 0);
            Vector3 r1 = new Vector3(w1, y1, 0);
            Vector3 nl = new Vector3(-0.4f, 0, 1f).normalized;
            Vector3 nr = new Vector3(0.4f, 0, 1f).normalized;

            int vi = verts.Count;

            // CW winding: swap second and third vertex of each tri
            verts.Add(l0); norms.Add(nl); uvsList.Add(new Vector2(0, uvV0));
            verts.Add(r0); norms.Add(nr); uvsList.Add(new Vector2(1, uvV0));
            verts.Add(l1); norms.Add(nl); uvsList.Add(new Vector2(0, uvV1));
            verts.Add(r1); norms.Add(nr); uvsList.Add(new Vector2(1, uvV1));

            tris.Add(vi);     tris.Add(vi + 2); tris.Add(vi + 1);
            tris.Add(vi + 1); tris.Add(vi + 2); tris.Add(vi + 3);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvsList);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // ── Wildflowers ─────────────────────────────────────────────────────────
    void PlaceWildflowers()
    {
        var rng = new System.Random(0x464C5752);
        var flowerMesh = BuildFlowerMesh();
        var matrices = new List<Matrix4x4>();
        var colorList = new List<Vector4>();

        for (int i = 0; i < 3000; i++)
        {
            float fx = RngRange(rng, 0.5f, 15.5f);
            float fz = RngRange(rng, 0.5f, 15.5f);
            if (WorldData.IsRiver(fx, fz)) continue;
            if (WorldData.RiverSDF(fx, fz) < 0.8f) continue;

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

            // CW winding
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

    static float RngRange(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }
}

/// <summary>
/// Helper component that draws instanced meshes each frame via Graphics.DrawMeshInstanced.
/// Batches automatically in groups of 1023 (Unity limit per call).
/// Supports optional per-instance colors via MaterialPropertyBlock.
/// </summary>
public class InstancedDrawer : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public Matrix4x4[] matrices;
    public Vector4[] instanceColors;
    public bool castShadows = true;

    MaterialPropertyBlock[] _blocks;
    int _batchCount;

    void Start()
    {
        if (matrices == null || matrices.Length == 0) return;
        _batchCount = Mathf.CeilToInt(matrices.Length / 1023f);
        if (instanceColors != null && instanceColors.Length > 0)
        {
            _blocks = new MaterialPropertyBlock[_batchCount];
            for (int b = 0; b < _batchCount; b++)
            {
                int start = b * 1023;
                int count = Mathf.Min(1023, matrices.Length - start);
                var colors = new Vector4[count];
                System.Array.Copy(instanceColors, start, colors, 0, count);
                _blocks[b] = new MaterialPropertyBlock();
                _blocks[b].SetVectorArray("_InstanceColor", colors);
            }
        }
    }

    void Update()
    {
        if (mesh == null || material == null || matrices == null) return;

        var shadow = castShadows
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.Off;

        for (int b = 0; b < _batchCount; b++)
        {
            int start = b * 1023;
            int count = Mathf.Min(1023, matrices.Length - start);
            var batch = new Matrix4x4[count];
            System.Array.Copy(matrices, start, batch, 0, count);

            MaterialPropertyBlock block = (_blocks != null) ? _blocks[b] : null;
            Graphics.DrawMeshInstanced(mesh, 0, material, batch, count, block,
                shadow, receiveShadows: true, layer: gameObject.layer);
        }
    }
}
