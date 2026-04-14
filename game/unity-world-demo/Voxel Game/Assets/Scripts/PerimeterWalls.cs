using UnityEngine;

public class PerimeterWalls : MonoBehaviour
{
    const float EDGE   = WorldData.SIZE;
    const float HEIGHT = 20f;
    const float THICK  = 0.75f;

    void Start()
    {
        AddWall("West",  new Vector3(THICK, HEIGHT, EDGE + THICK * 2f),
                         new Vector3(-THICK * 0.5f, HEIGHT * 0.5f, EDGE * 0.5f));
        AddWall("East",  new Vector3(THICK, HEIGHT, EDGE + THICK * 2f),
                         new Vector3(EDGE + THICK * 0.5f, HEIGHT * 0.5f, EDGE * 0.5f));
        AddWall("North", new Vector3(EDGE + THICK * 4f, HEIGHT, THICK),
                         new Vector3(EDGE * 0.5f, HEIGHT * 0.5f, -THICK * 0.5f));
        AddWall("South", new Vector3(EDGE + THICK * 4f, HEIGHT, THICK),
                         new Vector3(EDGE * 0.5f, HEIGHT * 0.5f, EDGE + THICK * 0.5f));
    }

    void AddWall(string wallName, Vector3 size, Vector3 center)
    {
        var go = new GameObject(wallName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = center;
        go.layer = gameObject.layer;

        var box = go.AddComponent<BoxCollider>();
        box.size = size;
    }
}
