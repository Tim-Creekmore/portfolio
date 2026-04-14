using UnityEngine;

public class WorldController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerController player;
    [SerializeField] TerrainChunk terrainChunk;
    [SerializeField] FoliagePlacer foliage;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Ensure ambient light is bright enough for all biomes
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.45f, 0.35f);
        RenderSettings.fogStartDistance = 50f;
        RenderSettings.fogEndDistance = 140f;
        RenderSettings.fogColor = new Color(0.78f, 0.74f, 0.62f);

        if (player != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Vector3 spawn = WorldData.GetSpawnPosition();
            player.transform.position = spawn;
            player.transform.rotation = Quaternion.Euler(0, 90f, 0);

            Invoke(nameof(EnablePlayer), 0.1f);
        }

        #if UNITY_WEBGL
        #endif
    }

    void EnablePlayer()
    {
        if (player != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;
        }
    }
}
