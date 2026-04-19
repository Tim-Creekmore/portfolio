using UnityEngine;

public class CameraStateMachine : MonoBehaviour
{
    public enum Mode { Hero, ThirdPerson, Commander }

    [Header("References")]
    [SerializeField] Camera heroCam;
    [SerializeField] Camera thirdPersonCam;
    [SerializeField] Transform springArm;
    [SerializeField] Transform playerBody;
    [SerializeField] MeshRenderer bodyVisual;

    [Header("Commander Settings")]
    [SerializeField] float cmdHeight = 18f;
    [SerializeField] float cmdPanSpeed = 20f;
    [SerializeField] float cmdZoomSpeed = 8f;
    [SerializeField] float cmdMinHeight = 10f;
    [SerializeField] float cmdMaxHeight = 40f;

    [Header("Third Person")]
    [SerializeField] float tpDistDefault = 8f;
    [SerializeField] float tpDistMin = 3f;
    [SerializeField] float tpDistMax = 16f;
    [SerializeField] float tpHeight = 2.5f;
    [SerializeField] float tpZoomSpeed = 2f;

    [Header("Mouse")]
    [SerializeField] float mouseSensitivity = 2f;

    Mode _mode = Mode.Hero;
    public Mode CurrentMode => _mode;

    float _pitch = -7f * Mathf.Deg2Rad;
    float _tpDist;
    Vector3 _cmdPosition;

    // Transition state
    bool _transitioning;
    float _transitionTimer;
    const float TRANSITION_DURATION = 0.4f;
    Vector3 _transFromPos;
    Quaternion _transFromRot;
    Vector3 _transToPos;
    Quaternion _transToRot;
    Camera _transCamera;

    void Awake()
    {
        _tpDist = tpDistDefault;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ApplyMode();
    }

    void Update()
    {
        HandleModeSwitch();
    }

    void LateUpdate()
    {
        if (_transitioning)
        {
            RunTransition();
            return;
        }

        switch (_mode)
        {
            case Mode.Hero:        UpdateHero(); break;
            case Mode.ThirdPerson: UpdateThirdPerson(); break;
            case Mode.Commander:   UpdateCommander(); break;
        }
    }

    // ── Mode switching ───────────────────────────────────────────────

    void HandleModeSwitch()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            _mode = (Mode)(((int)_mode + 1) % 3);
            ApplyMode();
        }
    }

    void ApplyMode()
    {
        bool isHero = _mode == Mode.Hero;
        bool isTP   = _mode == Mode.ThirdPerson;
        bool isCmd  = _mode == Mode.Commander;

        if (heroCam != null) heroCam.enabled = isHero;
        if (thirdPersonCam != null) thirdPersonCam.enabled = isTP;
        if (bodyVisual != null) bodyVisual.enabled = !isHero;

        if (isCmd)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (heroCam != null) heroCam.enabled = true;
            if (thirdPersonCam != null) thirdPersonCam.enabled = false;

            _cmdPosition = playerBody != null
                ? new Vector3(playerBody.position.x, cmdHeight, playerBody.position.z)
                : new Vector3(60f, cmdHeight, 60f);

            Vector3 targetPos = _cmdPosition;
            Quaternion targetRot = Quaternion.Euler(90f, 0f, 0f);

            StartTransition(heroCam, heroCam.transform.position, heroCam.transform.rotation,
                targetPos, targetRot);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (isTP)
            {
                if (springArm != null)
                    springArm.localRotation = Quaternion.Euler(_pitch * Mathf.Rad2Deg, 0, 0);
                UpdateTPPosition();
            }
            else
            {
                if (heroCam != null)
                    heroCam.transform.localRotation = Quaternion.Euler(_pitch * Mathf.Rad2Deg, 0, 0);
                if (springArm != null)
                    springArm.localRotation = Quaternion.identity;
            }
        }
    }

    // ── Transitions ──────────────────────────────────────────────────

    void StartTransition(Camera cam, Vector3 fromPos, Quaternion fromRot,
        Vector3 toPos, Quaternion toRot)
    {
        _transCamera = cam;
        _transFromPos = fromPos;
        _transFromRot = fromRot;
        _transToPos = toPos;
        _transToRot = toRot;
        _transitionTimer = 0f;
        _transitioning = true;
    }

    void RunTransition()
    {
        _transitionTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_transitionTimer / TRANSITION_DURATION);
        float smooth = t * t * (3f - 2f * t); // smoothstep

        if (_transCamera != null)
        {
            _transCamera.transform.position = Vector3.Lerp(_transFromPos, _transToPos, smooth);
            _transCamera.transform.rotation = Quaternion.Slerp(_transFromRot, _transToRot, smooth);
        }

        if (t >= 1f)
            _transitioning = false;
    }

    // ── Hero mode ────────────────────────────────────────────────────

    void UpdateHero()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mx = Input.GetAxisRaw("Mouse X");
        float my = Input.GetAxisRaw("Mouse Y");

        if (playerBody != null)
            playerBody.Rotate(Vector3.up, mx * mouseSensitivity, Space.World);

        _pitch -= my * mouseSensitivity * Mathf.Deg2Rad;
        _pitch = Mathf.Clamp(_pitch, -88f * Mathf.Deg2Rad, 88f * Mathf.Deg2Rad);

        if (heroCam != null)
            heroCam.transform.localRotation = Quaternion.Euler(_pitch * Mathf.Rad2Deg, 0, 0);
    }

    // ── Third Person mode ────────────────────────────────────────────

    void UpdateThirdPerson()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mx = Input.GetAxisRaw("Mouse X");
        float my = Input.GetAxisRaw("Mouse Y");

        if (playerBody != null)
            playerBody.Rotate(Vector3.up, mx * mouseSensitivity, Space.World);

        _pitch -= my * mouseSensitivity * Mathf.Deg2Rad;
        _pitch = Mathf.Clamp(_pitch, -88f * Mathf.Deg2Rad, 88f * Mathf.Deg2Rad);

        if (springArm != null)
            springArm.localRotation = Quaternion.Euler(_pitch * Mathf.Rad2Deg, 0, 0);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            _tpDist = Mathf.Clamp(_tpDist - scroll * tpZoomSpeed * 10f, tpDistMin, tpDistMax);
            UpdateTPPosition();
        }
    }

    void UpdateTPPosition()
    {
        if (thirdPersonCam != null)
            thirdPersonCam.transform.localPosition = new Vector3(0, tpHeight, -_tpDist);
    }

    // ── Commander mode ───────────────────────────────────────────────

    void UpdateCommander()
    {
        if (heroCam == null) return;

        // WASD pan
        Vector3 pan = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) pan.z += 1f;
        if (Input.GetKey(KeyCode.S)) pan.z -= 1f;
        if (Input.GetKey(KeyCode.A)) pan.x -= 1f;
        if (Input.GetKey(KeyCode.D)) pan.x += 1f;

        _cmdPosition += pan.normalized * cmdPanSpeed * Time.deltaTime;

        // Scroll zoom (height)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
            _cmdPosition.y = Mathf.Clamp(_cmdPosition.y - scroll * cmdZoomSpeed * 10f, cmdMinHeight, cmdMaxHeight);

        // Clamp to world bounds
        float margin = 5f;
        _cmdPosition.x = Mathf.Clamp(_cmdPosition.x, margin, WorldData.SIZE - margin);
        _cmdPosition.z = Mathf.Clamp(_cmdPosition.z, margin, WorldData.SIZE - margin);

        heroCam.transform.position = _cmdPosition;
        heroCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // ── Public API for PlayerController ──────────────────────────────

    public bool IsCommanderMode => _mode == Mode.Commander;
    public bool IsTransitioning => _transitioning;

    public bool PlayerCanMove => !IsCommanderMode && !IsTransitioning;
    public bool PlayerCanLook => false; // Camera machine handles all look now
}
