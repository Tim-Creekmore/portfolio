using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    public enum State { Idle, WindUp, Swing, Recovery, Blocking }

    [SerializeField] WeaponData equippedWeapon;
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] PlayerStamina playerStamina;
    [SerializeField] CameraStateMachine cameraStateMachine;
    [SerializeField] CharacterController characterController;
    [SerializeField] Camera heroCam;
    [SerializeField] Transform attackOrigin;

    [Header("Jump Attack")]
    [SerializeField] float jumpDamageMultiplier = 1.5f;

    [Header("Blocking")]
    [SerializeField] float swordBlockMultiplier = 0.5f;
    [SerializeField] float shieldBlockMultiplier = 0f;

    [Header("Visual Feedback")]
    [SerializeField] Transform weaponVisual;
    [SerializeField] Transform shieldVisual;

    State _state = State.Idle;
    float _stateTimer;
    float _stateDuration;
    WeaponData.AttackDirection _currentDirection;
    bool _hitLanded;
    bool _shieldEquipped;

    // Smooth animation tracking
    Quaternion _animFrom;
    Quaternion _animTo;
    Vector3 _posFrom;
    Vector3 _posTo;

    public State CurrentState => _state;
    public WeaponData.AttackDirection CurrentDirection => _currentDirection;
    public WeaponData EquippedWeapon => equippedWeapon;

    void Update()
    {
        // Hide weapon when not in hero mode
        if (weaponVisual != null)
        {
            bool show = cameraStateMachine == null ||
                cameraStateMachine.CurrentMode == CameraStateMachine.Mode.Hero;
            weaponVisual.gameObject.SetActive(show);
        }

        if (cameraStateMachine != null && !cameraStateMachine.PlayerCanMove) return;
        if (playerHealth != null && playerHealth.IsDead) return;

        // Shield toggle
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _shieldEquipped = !_shieldEquipped;
            if (shieldVisual != null)
                shieldVisual.gameObject.SetActive(_shieldEquipped);
        }

        _stateTimer -= Time.deltaTime;

        switch (_state)
        {
            case State.Idle:
                HandleIdleInput();
                break;
            case State.WindUp:
                UpdateWindUp();
                break;
            case State.Swing:
                UpdateSwing();
                break;
            case State.Recovery:
                if (_stateTimer <= 0f) TransitionTo(State.Idle);
                break;
            case State.Blocking:
                UpdateBlocking();
                break;
        }

        UpdateWeaponVisual();
    }

    bool _isJumpAttack;

    void HandleIdleInput()
    {
        if (equippedWeapon == null) return;

        if (Input.GetMouseButton(1))
        {
            if (playerStamina != null && !playerStamina.TryConsume(equippedWeapon.staminaCostBlock * Time.deltaTime))
                return;
            TransitionTo(State.Blocking);
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (playerStamina != null && !playerStamina.TryConsume(equippedWeapon.staminaCostAttack))
                return;

            bool airborne = characterController != null && !characterController.isGrounded;
            _isJumpAttack = airborne;
            _currentDirection = airborne
                ? WeaponData.AttackDirection.Overhead
                : DetermineDirection();
            TransitionTo(State.WindUp);
        }
    }

    void UpdateWindUp()
    {
        if (_stateTimer <= 0f)
            TransitionTo(State.Swing);
    }

    void UpdateSwing()
    {
        if (!_hitLanded)
            TryHit();

        if (_stateTimer <= 0f)
            TransitionTo(State.Recovery);
    }

    static readonly Vector3 SHIELD_BLOCK_POS = new Vector3(-0.25f, -0.05f, 0.5f);
    static readonly Quaternion SHIELD_BLOCK_ROT = Quaternion.Euler(5f, 25f, 0f);
    static readonly Vector3 SHIELD_IDLE_POS = new Vector3(-0.35f, -0.2f, 0.35f);
    static readonly Quaternion SHIELD_IDLE_ROT = Quaternion.Euler(5f, 10f, 0f);

    void UpdateBlocking()
    {
        if (playerStamina != null)
        {
            playerStamina.Drain(equippedWeapon.staminaCostBlock * Time.deltaTime);
            if (playerStamina.IsEmpty)
            {
                if (_shieldEquipped && shieldVisual != null)
                {
                    shieldVisual.localPosition = SHIELD_IDLE_POS;
                    shieldVisual.localRotation = SHIELD_IDLE_ROT;
                }
                TransitionTo(State.Idle);
                return;
            }
        }

        _currentDirection = DetermineDirection();
        float lerpSpeed = Time.deltaTime * 12f;

        if (_shieldEquipped && shieldVisual != null)
        {
            // Shield comes forward and angles to face the attacker
            shieldVisual.localPosition = Vector3.Lerp(
                shieldVisual.localPosition, SHIELD_BLOCK_POS, lerpSpeed);
            shieldVisual.localRotation = Quaternion.Slerp(
                shieldVisual.localRotation, SHIELD_BLOCK_ROT, lerpSpeed);

            // Sword pulls back behind shield
            if (weaponVisual != null)
            {
                weaponVisual.localRotation = Quaternion.Slerp(
                    weaponVisual.localRotation, Quaternion.Euler(-20f, -15f, 10f), lerpSpeed);
                weaponVisual.localPosition = Vector3.Lerp(
                    weaponVisual.localPosition, new Vector3(0.35f, -0.15f, 0.2f), lerpSpeed);
            }
        }
        else
        {
            // Sword-only block — horizontal guard
            if (weaponVisual != null)
            {
                weaponVisual.localRotation = Quaternion.Slerp(
                    weaponVisual.localRotation, BlockRot(), lerpSpeed);
                weaponVisual.localPosition = Vector3.Lerp(
                    weaponVisual.localPosition, BLOCK_POS, lerpSpeed);
            }
        }

        if (!Input.GetMouseButton(1))
        {
            // Return shield to idle pose
            if (_shieldEquipped && shieldVisual != null)
            {
                shieldVisual.localPosition = SHIELD_IDLE_POS;
                shieldVisual.localRotation = SHIELD_IDLE_ROT;
            }
            TransitionTo(State.Idle);
        }
    }

    // ── Option E: camera-relative direction ──────────────────────────

    const float AIM_RANGE = 2.5f;

    WeaponData.AttackDirection DetermineDirection()
    {
        if (heroCam == null) return RandomDirection();

        Ray ray = heroCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, AIM_RANGE))
            return RandomDirection();

        var targetHealth = hit.collider.GetComponentInParent<TargetHealth>();
        if (targetHealth == null)
            return RandomDirection();

        float dist = Vector3.Distance(transform.position, targetHealth.transform.position);
        if (dist > AIM_RANGE)
            return RandomDirection();

        // Directly check which zone box the crosshair hit
        string zoneName = hit.collider.gameObject.name;
        if (zoneName.Contains("Head"))  return WeaponData.AttackDirection.Overhead;
        if (zoneName.Contains("Left"))  return WeaponData.AttackDirection.Left;
        if (zoneName.Contains("Right")) return WeaponData.AttackDirection.Right;
        if (zoneName.Contains("Legs"))  return WeaponData.AttackDirection.Thrust;

        return RandomDirection();
    }

    // ── Hit detection ────────────────────────────────────────────────

    public event System.Action<float, string> OnDamageDealt; // (damage, targetName)

    void TryHit()
    {
        if (equippedWeapon == null || attackOrigin == null) return;

        Vector3 origin = attackOrigin.position;
        Vector3 dir = attackOrigin.forward;

        RaycastHit hit;
        if (!Physics.SphereCast(origin, equippedWeapon.hitRadius, dir,
            out hit, equippedWeapon.range))
            return;

        float dmg = equippedWeapon.baseDamage
            * equippedWeapon.GetDirectionMultiplier(_currentDirection);
        if (_isJumpAttack)
            dmg *= jumpDamageMultiplier;

        // Try TargetHealth (test dummies)
        var targetHP = hit.collider.GetComponentInParent<TargetHealth>();
        if (targetHP != null)
        {
            targetHP.TakeDamage(dmg, _currentDirection);
            _hitLanded = true;
            OnDamageDealt?.Invoke(dmg, targetHP.gameObject.name);
            return;
        }

        // Try UnitHealth (enemy units)
        var unitHP = hit.collider.GetComponentInParent<UnitHealth>();
        if (unitHP != null && !unitHP.IsDead)
        {
            unitHP.TakeDamage(dmg);
            _hitLanded = true;
            OnDamageDealt?.Invoke(dmg, unitHP.gameObject.name);
        }
    }

    // ── State transitions ────────────────────────────────────────────

    void TransitionTo(State newState)
    {
        _state = newState;
        _hitLanded = false;

        switch (newState)
        {
            case State.WindUp:
                _stateTimer = equippedWeapon.windUpTime;
                _stateDuration = _stateTimer;
                SetAnimTarget(WindUpRot(_currentDirection), WindUpPos(_currentDirection));
                break;
            case State.Swing:
                _stateTimer = equippedWeapon.swingTime;
                _stateDuration = _stateTimer;
                SetAnimTarget(SwingRot(_currentDirection), SwingPos(_currentDirection));
                break;
            case State.Recovery:
                _stateTimer = equippedWeapon.recoveryTime;
                _stateDuration = _stateTimer;
                SetAnimTarget(IdleRot(), IDLE_POS);
                break;
            case State.Blocking:
                _stateTimer = 0f;
                _stateDuration = 0.2f;
                SetAnimTarget(BlockRot(), BLOCK_POS);
                break;
            case State.Idle:
                _stateTimer = 0f;
                _stateDuration = 0.15f;
                SetAnimTarget(IdleRot(), IDLE_POS);
                break;
        }
    }

    // ── Weapon visual ────────────────────────────────────────────────

    static readonly Vector3 IDLE_POS       = new Vector3(0.3f, -0.25f, 0.4f);
    static readonly Vector3 BLOCK_POS      = new Vector3(0.1f, -0.05f, 0.35f);

    void UpdateWeaponVisual()
    {
        if (weaponVisual == null) return;

        float progress = (_stateDuration > 0f)
            ? 1f - Mathf.Clamp01(_stateTimer / _stateDuration)
            : 1f;

        // Ease curve: fast start, smooth end for swings; smooth both ends otherwise
        float eased;
        if (_state == State.Swing)
            eased = 1f - (1f - progress) * (1f - progress); // ease-out quad
        else
            eased = progress * progress * (3f - 2f * progress); // smoothstep

        weaponVisual.localRotation = Quaternion.Slerp(_animFrom, _animTo, eased);
        weaponVisual.localPosition = Vector3.Lerp(_posFrom, _posTo, eased);
    }

    void SetAnimTarget(Quaternion toRot, Vector3 toPos)
    {
        _animFrom = weaponVisual != null ? weaponVisual.localRotation : Quaternion.identity;
        _animTo = toRot;
        _posFrom = weaponVisual != null ? weaponVisual.localPosition : IDLE_POS;
        _posTo = toPos;
    }

    static Quaternion IdleRot()    => Quaternion.Euler(0f, 0f, -30f);
    static Quaternion BlockRot()   => Quaternion.Euler(-10f, 15f, 0f);

    static Quaternion WindUpRot(WeaponData.AttackDirection dir)
    {
        switch (dir)
        {
            case WeaponData.AttackDirection.Overhead: return Quaternion.Euler(-130f, 0f, 0f);
            case WeaponData.AttackDirection.Left:     return Quaternion.Euler(-20f, -70f, 50f);
            case WeaponData.AttackDirection.Right:    return Quaternion.Euler(-20f, 70f, -50f);
            case WeaponData.AttackDirection.Thrust:   return Quaternion.Euler(-70f, 0f, 0f);
            default: return Quaternion.identity;
        }
    }

    static Vector3 WindUpPos(WeaponData.AttackDirection dir)
    {
        switch (dir)
        {
            case WeaponData.AttackDirection.Overhead: return new Vector3(0.25f, 0.1f, 0.2f);
            case WeaponData.AttackDirection.Left:     return new Vector3(-0.1f, -0.15f, 0.3f);
            case WeaponData.AttackDirection.Right:    return new Vector3(0.5f, -0.15f, 0.3f);
            case WeaponData.AttackDirection.Thrust:   return new Vector3(0.2f, -0.2f, 0.15f);
            default: return IDLE_POS;
        }
    }

    static Quaternion SwingRot(WeaponData.AttackDirection dir)
    {
        switch (dir)
        {
            case WeaponData.AttackDirection.Overhead: return Quaternion.Euler(30f, 0f, 0f);
            case WeaponData.AttackDirection.Left:     return Quaternion.Euler(-10f, 50f, -40f);
            case WeaponData.AttackDirection.Right:    return Quaternion.Euler(-10f, -50f, 40f);
            case WeaponData.AttackDirection.Thrust:   return Quaternion.Euler(5f, 0f, 0f);
            default: return Quaternion.identity;
        }
    }

    static Vector3 SwingPos(WeaponData.AttackDirection dir)
    {
        switch (dir)
        {
            case WeaponData.AttackDirection.Overhead: return new Vector3(0.25f, -0.4f, 0.55f);
            case WeaponData.AttackDirection.Left:     return new Vector3(0.5f, -0.2f, 0.5f);
            case WeaponData.AttackDirection.Right:    return new Vector3(-0.1f, -0.2f, 0.5f);
            case WeaponData.AttackDirection.Thrust:   return new Vector3(0.2f, -0.15f, 0.7f);
            default: return IDLE_POS;
        }
    }

    // ── Utility ──────────────────────────────────────────────────────

    static WeaponData.AttackDirection RandomDirection()
    {
        int r = Random.Range(0, 4);
        return (WeaponData.AttackDirection)r;
    }

    // ── Incoming damage (called by enemies) ────────────────────────

    public event System.Action<float, float, bool> OnBlockedHit; // (baseDmg, finalDmg, shieldUsed)

    public void ReceiveAttack(float baseDamage)
    {
        if (playerHealth == null || playerHealth.IsDead) return;

        float finalDamage = baseDamage;
        bool blocked = false;

        if (_state == State.Blocking)
        {
            blocked = true;
            float mult = _shieldEquipped ? shieldBlockMultiplier : swordBlockMultiplier;
            finalDamage = baseDamage * mult;
        }

        if (finalDamage > 0f)
            playerHealth.TakeDamage(finalDamage);

        if (blocked && finalDamage <= 0f)
            OnBlockedHit?.Invoke(baseDamage, 0f, _shieldEquipped);
    }

    // ── Public API ───────────────────────────────────────────────────

    public bool CanAttack => _state == State.Idle && equippedWeapon != null;
    public bool IsBlocking => _state == State.Blocking;
    public bool IsAttacking => _state == State.WindUp || _state == State.Swing;
    public bool ShieldEquipped => _shieldEquipped;
}
