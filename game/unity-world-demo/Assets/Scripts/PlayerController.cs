using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    const float SPEED          = 4.2f;
    const float SWIM_SPEED     = 2.4f;
    const float JUMP_VELOCITY  = 5.5f;
    const float SWIM_UP_FORCE  = 4.5f;
    const float SINK_SPEED     = 1.8f;
    const float BUOYANCY       = 3.0f;
    const float WATER_DRAG     = 4.0f;
    const float GRAVITY        = 12.0f;

    [Header("Cameras")]
    [SerializeField] Transform neck;
    [SerializeField] Camera camFP;
    [SerializeField] Camera camTP;
    [SerializeField] Transform springArm;

    [Header("Visuals")]
    [SerializeField] MeshRenderer bodyVisual;

    [Header("Mouse")]
    [SerializeField] float mouseSensitivity = 2.0f;

    CharacterController _cc;
    bool  _thirdPerson;
    float _bobTime;
    float _pitch = -7f * Mathf.Deg2Rad;
    bool  _spaceWasDown;
    bool  _inWater;
    Vector3 _velocity;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ApplyCameraMode();
    }

    void Update()
    {
        HandleMouseLook();
        HandleInput();
    }

    void FixedUpdate()
    {
        float waterY = WorldData.WATER_Y;
        float feetY  = transform.position.y - 0.8f;
        _inWater = feetY < waterY && WorldData.IsRiver(transform.position.x, transform.position.z);
        float submergeDepth = waterY - feetY;

        if (_inWater)
            ProcessSwimming(submergeDepth);
        else
            ProcessGround();

        Vector2 g = GetMoveInput();
        Vector3 forward = transform.forward;
        Vector3 right   = transform.right;
        Vector3 dir = (right * g.x + forward * g.y).normalized;
        float moveSpeed = _inWater ? SWIM_SPEED : SPEED;

        if (g.sqrMagnitude > 0.0001f)
        {
            _velocity.x = dir.x * moveSpeed;
            _velocity.z = dir.z * moveSpeed;
        }
        else
        {
            float decel = _inWater ? WATER_DRAG : 12f;
            _velocity.x = Mathf.MoveTowards(_velocity.x, 0, moveSpeed * Time.fixedDeltaTime * decel);
            _velocity.z = Mathf.MoveTowards(_velocity.z, 0, moveSpeed * Time.fixedDeltaTime * decel);
        }

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

        float buoyancyForce = Mathf.Clamp01(submergeDepth / 1.5f) * BUOYANCY;
        _velocity.y -= GRAVITY * Time.fixedDeltaTime;
        _velocity.y += buoyancyForce * Time.fixedDeltaTime * 10f;

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

    void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mx = Input.GetAxisRaw("Mouse X");
        float my = Input.GetAxisRaw("Mouse Y");

        transform.Rotate(Vector3.up, mx * mouseSensitivity, Space.World);
        _pitch -= my * mouseSensitivity * Mathf.Deg2Rad;
        _pitch = Mathf.Clamp(_pitch, -88f * Mathf.Deg2Rad, 88f * Mathf.Deg2Rad);

        if (_thirdPerson && springArm != null)
            springArm.localRotation = Quaternion.Euler(_pitch * Mathf.Rad2Deg, 0, 0);
        else if (camFP != null)
            camFP.transform.localRotation = Quaternion.Euler(_pitch * Mathf.Rad2Deg, 0, 0);
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            _thirdPerson = !_thirdPerson;
            ApplyCameraMode();
        }

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
    }

    void UpdateHeadBob(float delta, Vector2 g)
    {
        if (camFP == null) return;

        if (_thirdPerson)
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
            _bobTime += delta * 9f;
            float bob = Mathf.Sin(_bobTime) * 0.038f;
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

    void ApplyCameraMode()
    {
        if (bodyVisual != null)
            bodyVisual.enabled = _thirdPerson;

        if (camFP != null) camFP.enabled = !_thirdPerson;
        if (camTP != null) camTP.enabled = _thirdPerson;

        if (_thirdPerson)
        {
            if (springArm != null)
                springArm.localRotation = Quaternion.Euler(_pitch * Mathf.Rad2Deg, 0, 0);
            if (camFP != null)
                camFP.transform.localRotation = Quaternion.identity;
        }
        else
        {
            if (camFP != null)
                camFP.transform.localRotation = Quaternion.Euler(_pitch * Mathf.Rad2Deg, 0, 0);
            if (springArm != null)
                springArm.localRotation = Quaternion.identity;
        }
    }
}
