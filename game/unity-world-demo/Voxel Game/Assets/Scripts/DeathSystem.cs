using UnityEngine;
using System.Collections;

public class DeathSystem : MonoBehaviour
{
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] PlayerStamina playerStamina;
    [SerializeField] Transform playerTransform;
    [SerializeField] ScreenFade screenFade;
    [SerializeField] CharacterController characterController;
    [SerializeField] CameraStateMachine cameraStateMachine;
    [SerializeField] UnitSpawner unitSpawner;

    [Header("Respawn Timing")]
    [SerializeField] float fadeOutDuration = 1.2f;
    [SerializeField] float blackScreenHold = 0.8f;
    [SerializeField] float fadeInDuration = 1.5f;

    DeathMarker _activeMarker;
    bool _respawning;

    void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnDeath += HandleDeath;
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDeath -= HandleDeath;
    }

    void HandleDeath()
    {
        if (_respawning) return;
        StartCoroutine(RespawnFlow());
    }

    void SpawnDeathMarker()
    {
        if (_activeMarker != null)
            Destroy(_activeMarker.gameObject);

        var go = new GameObject("DeathMarker");
        _activeMarker = go.AddComponent<DeathMarker>();
        _activeMarker.Init(playerTransform.position);
    }

    IEnumerator RespawnFlow()
    {
        _respawning = true;

        // Force hero camera mode so fade looks right
        if (cameraStateMachine != null && cameraStateMachine.IsCommanderMode)
        {
            // Switch back to hero before fading
        }

        // Freeze player
        if (characterController != null)
            characterController.enabled = false;

        // Fade to black
        if (screenFade != null)
        {
            bool fadeDone = false;
            screenFade.FadeOut(fadeOutDuration, () => fadeDone = true);
            while (!fadeDone) yield return null;
        }

        // Spawn death marker while screen is black (player can't see it pop in)
        SpawnDeathMarker();

        // Hold on black
        yield return new WaitForSecondsRealtime(blackScreenHold);

        // Teleport to spawn
        Vector3 spawnPos = WorldData.GetSpawnPosition();
        playerTransform.position = spawnPos;
        playerTransform.rotation = Quaternion.Euler(0f, 90f, 0f);

        playerHealth.ResetHealth();
        if (playerStamina != null) playerStamina.ResetStamina();

        // Respawn all units (friendly + enemy)
        if (unitSpawner != null)
            unitSpawner.RespawnAll();

        // Brief pause before re-enabling so physics catches up
        yield return new WaitForFixedUpdate();

        // Re-enable movement
        if (characterController != null)
            characterController.enabled = true;

        // Fade back in
        if (screenFade != null)
        {
            bool fadeDone = false;
            screenFade.FadeIn(fadeInDuration, () => fadeDone = true);
            while (!fadeDone) yield return null;
        }

        _respawning = false;
    }

    public DeathMarker ActiveMarker => _activeMarker;
    public bool IsRespawning => _respawning;
}
