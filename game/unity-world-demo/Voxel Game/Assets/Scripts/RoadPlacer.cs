using UnityEngine;

public class RoadPlacer : MonoBehaviour
{
    [Header("Road Mesh")]
    public Mesh roadTileMesh;
    public Material roadMaterial;

    [Header("Tile Settings")]
    public float tileLength = 2.0f;
    public float tileScale  = 1.0f;
    public float yOffset    = 0.02f;

    void Start()
    {
        if (roadTileMesh == null || roadMaterial == null) return;
        PlaceAllRoads();
    }

    void PlaceAllRoads()
    {
        var paths = WorldData.RoadPaths;
        for (int p = 0; p < paths.Length; p++)
        {
            PlaceRoadPath(paths[p], p);
        }
    }

    void PlaceRoadPath(Vector2[] waypoints, int pathIndex)
    {
        var parent = new GameObject($"Road_{pathIndex}");
        parent.transform.SetParent(transform, false);

        // Build a dense list of points along the spline
        var points = new System.Collections.Generic.List<Vector2>();
        float step = tileLength * tileScale * 0.5f;

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector2 a = waypoints[i];
            Vector2 b = waypoints[i + 1];
            float segLen = Vector2.Distance(a, b);
            int samples = Mathf.Max(1, Mathf.CeilToInt(segLen / step));
            for (int s = 0; s < samples; s++)
            {
                float t = (float)s / samples;
                points.Add(Vector2.Lerp(a, b, t));
            }
        }
        points.Add(waypoints[waypoints.Length - 1]);

        // Place tiles at intervals along the path
        float accumulated = 0f;
        float spacing = tileLength * tileScale;
        int tileIdx = 0;

        for (int i = 1; i < points.Count; i++)
        {
            float segDist = Vector2.Distance(points[i - 1], points[i]);
            accumulated += segDist;

            if (accumulated >= spacing)
            {
                accumulated -= spacing;

                float fx = points[i].x;
                float fz = points[i].y;
                float fy = WorldData.HeightSmooth(fx, fz) + yOffset;

                // Direction for rotation
                Vector2 dir;
                if (i < points.Count - 1)
                    dir = (points[i + 1] - points[i - 1]).normalized;
                else
                    dir = (points[i] - points[i - 1]).normalized;

                float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;

                var tileGO = new GameObject($"Tile_{tileIdx}");
                tileGO.transform.SetParent(parent.transform, false);
                tileGO.transform.position = new Vector3(fx, fy, fz);
                tileGO.transform.rotation = Quaternion.Euler(0, angle, 0);
                tileGO.transform.localScale = Vector3.one * tileScale;

                var mf = tileGO.AddComponent<MeshFilter>();
                var mr = tileGO.AddComponent<MeshRenderer>();
                mf.sharedMesh = roadTileMesh;
                mr.sharedMaterial = roadMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                tileIdx++;
            }
        }
    }
}
