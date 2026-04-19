using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    const float SPEED          = 4.2f;
    const float SPRINT_SPEED   = 7.5f;
    const float SPRINT_DRAIN   = 12f;
    const float SWIM_SPEED     = 2.4f;
    const float JUMP_VELOCITY  = 5.5f;
    const float SWIM_UP_FORCE  = 4.5f;
    const float SINK_SPEED     = 1.8f;
    const float BUOYANCY       = 3.0f;
    const float WATER_DRAG     = 4.0f;
    const float GRAVITY        = 12.0f;

    [Header("Camera")]
    [SerializeField] CameraStateMachine cameraStateMachine;
    [SerializeField] Camera camFP;

    [Header("Stamina")]
    [SerializeField] PlayerStamina playerStamina;

    CharacterController _cc;
    float _bobTime;
    bool  _spaceWasDown;
    bool  _inWater;
    bool  _sprinting;
    Vector3 _velocity;

    public bool IsSprinting => _sprinting;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleCursorToggle();
    }

    void FixedUpdate()
    {
        if (cameraStateMachine != null && !cameraStateMachine.PlayerCanMove)
            return;

        float waterY = WorldData.WATER_Y;
        float feetY  = transform.position.y - 0.8f;
        _inWater = feetY < waterY && WorldData.IsPond(transform.position.x, transform.position.z);
        float submergeDepth = waterY - feetY;

        if (_inWater)
            ProcessSwimming(submergeDepth);
        else
            ProcessGround();

        Vector2 g = GetMoveInput();
        Vector3 forward = transform.forward;
        Vector3 right   = transform.right;
        Vector3 dir = (right * g.x + forward * g.y).normalized;

        bool wantSprint = !_inWater && g.sqrMagnitude > 0.1f
            && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        _sprinting = wantSprint && playerStamina != null && !playerStamina.IsEmpty;

        if (_sprinting)
            playerStamina.Drain(SPRINT_DRAIN * Time.fixedDeltaTime);

        float moveSpeed = _inWater ? SWIM_SPEED : (_sprinting ? SPRINT_SPEED : SPEED);

        if (g.sqrMagnitude > 0.0001f)
        {
            _velocity.x = dir.x * moveSpeed;
            _velocity.z = dir.z * moveSpeed;
        }
        else
        {
            _sprinting = false;
            float decel = _inWater ? WATER_DRAG : 12f;
            _velocity.x = Mathf.MoveTowards(_velocity.x, 0, moveSpeed * Time.fixedDeltaTime * decel);
            _velocity.z = Mathf.MoveTowards(_velocity.z, 0, moveSpeed * Time.fixedDeltaTime * decel);
        }

        if (_cc.enabled)
            _cc.Move(_velocity * Time.fixedDeltaTime);
        UpdateHeadBob(Time.fixedDeltaTime, g);
    }

    void ProcessGround()
    {
        if (!_cc.isGrounded)
            _velocity.y -= GRAVITY * Time.fixedDeltaTime;

        bool spaceDown = Input.GetKey(KeyCode.Space);
        if (_cc.isGrounded && spaceDown && !_spaceWasDown)
            _velocity.y = JUMP_VELOCITY;
        _spaceWasDown = spaceDown;
    }

    void ProcessSwimming(float submergeDepth)
    {
        bool spaceDown = Input.GetKey(KeyCode.Space);
        bool shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        _spaceWasDown = spaceDown;

        float targetY = WorldData.WATER_Y - 0.6f;
        float currentY = transform.position.y;
        float diff = targetY - currentY;

        float swimForce = diff * 8f;
        _velocity.y += swimForce * Time.fixedDeltaTime;

        if (spaceDown)
            _velocity.y = Mathf.MoveTowards(_velocity.y, SWIM_UP_FORCE, SWIM_UP_FORCE * Time.fixedDeltaTime * 6f);
        else if (shiftDown)
            _velocity.y = Mathf.MoveTowards(_velocity.y, -SINK_SPEED, SINK_SPEED * Time.fixedDeltaTime * 6f);

        _velocity.y *= (1f - WATER_DRAG * Time.fixedDeltaTime);
    }

    Vector2 GetMoveInput()
    {
        Vector2 g = Vector2.zero;
        if (Input.GetKey(KeyCode.A)) g.x -= 1f;
        if (Input.GetKey(KeyCode.D)) g.x += 1f;
        if (Input.GetKey(KeyCode.W)) g.y += 1f;
        if (Input.GetKey(KeyCode.S)) g.y -= 1f;
        return Vector2.ClampMagnitude(g, 1f);
    }

    void HandleCursorToggle()
    {
        if (cameraStateMachine != null && cameraStateMachine.IsCommanderMode)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Re-lock on focus return (prevents cursor escaping to other monitors)
        if (Cursor.lockState == CursorLockMode.Locked && !Cursor.visible)
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && cameraStateMachine != null && !cameraStateMachine.IsCommanderMode)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void UpdateHeadBob(float delta, Vector2 g)
    {
        if (camFP == null) return;
        if (cameraStateMachine != null && cameraStateMachine.CurrentMode != CameraStateMachine.Mode.Hero)
        {
            camFP.transform.localPosition = Vector3.zero;
            return;
        }

        if (_inWater)
        {
            _bobTime += delta * 2.5f;
            float bob = Mathf.Sin(_bobTime) * 0.06f;
            camFP.transform.localPosition = new Vector3(
                Mathf.Cos(_bobTime * 0.7f) * 0.03f, bob, 0);
            return;
        }

        float spd = new Vector2(_velocity.x, _velocity.z).magnitude;
        if (spd > 0.35f && _cc.isGrounded)
        {
            float bobFreq = _sprinting ? 13f : 9f;
            float bobAmp  = _sprinting ? 0.055f : 0.038f;
            _bobTime += delta * bobFreq;
            float bob = Mathf.Sin(_bobTime) * bobAmp;
            camFP.transform.localPosition = new Vector3(
                Mathf.Cos(_bobTime * 0.5f) * 0.018f, bob, 0);
        }
        else if (g.sqrMagnitude < 0.01f)
        {
            _bobTime = 0;
            camFP.transform.localPosition = Vector3.Lerp(
                camFP.transform.localPosition, Vector3.zero, delta * 10f);
        }
    }
}
