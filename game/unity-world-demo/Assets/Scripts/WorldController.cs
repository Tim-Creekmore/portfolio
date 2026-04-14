using UnityEngine;

public class WorldController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerController player;
    [SerializeField] TerrainChunk terrainChunk;
    [SerializeField] FoliagePlacer foliage;

    void Start()
    {
        // Position player at spawn
        if (player != null)
        {
            Vector3 spawn = WorldData.GetSpawnPosition();
            player.transform.position = spawn;
            player.transform.rotation = Quaternion.Euler(0, 90f, 0);
        }

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Reduce glow on WebGL builds (if applicable)
        #if UNITY_WEBGL
        // Glow/bloom reduction can be handled via Volume Profile at build time
        #endif
    }
}
