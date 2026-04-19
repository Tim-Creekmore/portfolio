using UnityEngine;

public class AttackDummy : MonoBehaviour
{
    [SerializeField] float attackInterval = 2.5f;
    [SerializeField] float attackDamage = 15f;
    [SerializeField] float attackRange = 2.5f;
    [SerializeField] float windUpTime = 0.6f;

    Transform _player;
    CombatSystem _playerCombat;
    float _timer;
    bool _winding;
    float _windTimer;

    // Visual
    Transform _swordArm;
    Quaternion _armIdle;
    Quaternion _armWindUp;
    Quaternion _armSwing;
    enum ArmState { Idle, WindUp, Swing, Recovery }
    ArmState _armState = ArmState.Idle;
    float _armTimer;

    void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null)
        {
            var pc = Object.FindObjectOfType<PlayerController>();
            if (pc != null) playerGO = pc.gameObject;
        }

        if (playerGO != null)
        {
            _player = playerGO.transform;
            _playerCombat = playerGO.GetComponent<CombatSystem>();
        }

        // Find the sword arm child at runtime (editor-set references don't survive Play mode)
        if (_swordArm == null)
            _swordArm = transform.Find("SwordArm");

        _timer = attackInterval;
        _armIdle = Quaternion.Euler(0f, 0f, -30f);
        _armWindUp = Quaternion.Euler(-120f, 0f, 0f);
        _armSwing = Quaternion.Euler(30f, 0f, 0f);
    }

    public void SetSwordArm(Transform arm) { _swordArm = arm; }

    void Update()
    {
        if (_player == null) return;

        // Face the player
        Vector3 lookDir = _player.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(lookDir), Time.deltaTime * 3f);

        float dist = Vector3.Distance(transform.position, _player.position);

        if (_winding)
        {
            _windTimer -= Time.deltaTime;
            if (_windTimer <= 0f)
            {
                _winding = false;
                ExecuteAttack();
            }
        }
        else
        {
            if (dist > attackRange)
            {
                _timer = attackInterval * 0.5f;
            }
            else
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    _timer = attackInterval;
                    StartWindUp();
                }
            }
        }

        // Always update arm visual
        UpdateArmVisual();
    }

    void StartWindUp()
    {
        _winding = true;
        _windTimer = windUpTime;
        _armState = ArmState.WindUp;
        _armTimer = windUpTime;
    }

    void ExecuteAttack()
    {
        _armState = ArmState.Swing;
        _armTimer = 0.3f;

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > attackRange) return;

        if (_playerCombat != null)
            _playerCombat.ReceiveAttack(attackDamage);
    }

    void UpdateArmVisual()
    {
        if (_swordArm == null) return;

        Quaternion target;
        float speed;

        switch (_armState)
        {
            case ArmState.WindUp:
                target = _armWindUp;
                // Slow, deliberate wind-up so player can read it
                float windProgress = 1f - Mathf.Clamp01(_windTimer / windUpTime);
                float windEased = windProgress * windProgress;
                _swordArm.localRotation = Quaternion.Slerp(_armIdle, _armWindUp, windEased);
                return;

            case ArmState.Swing:
                target = _armSwing;
                // Fast follow-through
                speed = 20f;
                _armTimer -= Time.deltaTime;
                _swordArm.localRotation = Quaternion.Slerp(
                    _swordArm.localRotation, target, Time.deltaTime * speed);
                if (_armTimer <= 0f)
                {
                    _armState = ArmState.Recovery;
                    _armTimer = 0.6f;
                }
                return;

            case ArmState.Recovery:
                target = _armIdle;
                // Slow return to rest
                _armTimer -= Time.deltaTime;
                float recProgress = 1f - Mathf.Clamp01(_armTimer / 0.6f);
                float recEased = recProgress * recProgress * (3f - 2f * recProgress);
                _swordArm.localRotation = Quaternion.Slerp(_armSwing, _armIdle, recEased);
                if (_armTimer <= 0f) _armState = ArmState.Idle;
                return;

            default:
                target = _armIdle;
                speed = 4f;
                _swordArm.localRotation = Quaternion.Slerp(
                    _swordArm.localRotation, target, Time.deltaTime * speed);
                return;
        }
    }
}
