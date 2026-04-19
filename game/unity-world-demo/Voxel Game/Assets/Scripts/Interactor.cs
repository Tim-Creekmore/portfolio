using UnityEngine;
using UnityEngine.UI;

public class Interactor : MonoBehaviour
{
    [SerializeField] float pickupRange = 2.5f;
    [SerializeField] DeathSystem deathSystem;
    [SerializeField] CameraStateMachine cameraStateMachine;
    [SerializeField] Text pickupText;

    float _pickupTextTimer;

    void Update()
    {
        if (cameraStateMachine != null && cameraStateMachine.IsCommanderMode) return;

        if (Input.GetKeyDown(KeyCode.R))
            TryPickup();

        if (_pickupTextTimer > 0f)
        {
            _pickupTextTimer -= Time.deltaTime;
            if (_pickupTextTimer <= 0f && pickupText != null)
                pickupText.text = "";
        }
    }

    void TryPickup()
    {
        if (deathSystem != null && deathSystem.ActiveMarker != null)
        {
            var marker = deathSystem.ActiveMarker;
            float dist = Vector3.Distance(transform.position, marker.DeathPosition);

            if (dist <= pickupRange)
            {
                RetrieveDeathMarker(marker);
                return;
            }
        }
    }

    void RetrieveDeathMarker(DeathMarker marker)
    {
        Destroy(marker.gameObject);
        ShowPickupText("Items retrieved");
    }

    void ShowPickupText(string msg)
    {
        if (pickupText != null)
        {
            pickupText.text = msg;
            _pickupTextTimer = 2.5f;
        }
    }
}
