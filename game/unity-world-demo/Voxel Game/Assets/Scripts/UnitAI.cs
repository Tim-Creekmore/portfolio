using UnityEngine;

[RequireComponent(typeof(UnitHealth))]
public class UnitAI : MonoBehaviour
{
    public enum UnitState { Following, HoldPosition, Attacking, Retreating }

    [SerializeField] UnitData unitData;
    [SerializeField] Transform followTarget;
    [SerializeField] int formationIndex;
    [SerializeField] UnitState initialState = UnitState.Following;

    UnitHealth _health;
    UnitState _state = UnitState.Following;
    Vector3 _rallyPoint;
    Transform _attackTarget;
    float _attackTimer;
    float _retargetTimer;
    bool _commanderOrdered; // true when commander explicitly set state (prevents auto-override)
    CharacterController _cc;
    Vector3 _velocity;

    const float RETARGET_INTERVAL = 1.5f;

    public UnitState State => _state;
    public UnitData Data => unitData;
    public UnitHealth Health => _health;
    public bool IsAlive => _health != null && !_health.IsDead;

    const float GRAVITY = 12f;
    const float STOP_DISTANCE = 0.5f;
    const float ATTACK_MOVE_DISTANCE = 0.3f;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _health = GetComponent<UnitHealth>();
    }

    void Start()
    {
        if (unitData != null && _health != null)
            _health.Init(unitData.maxHP);

        _state = initialState;
        _rallyPoint = transform.position;
    }

    public void SetState(UnitState newState)
    {
        _state = newState;
        _attackTarget = null;
        _commanderOrdered = true;
    }

    public void SetRallyPoint(Vector3 point)
    {
        _rallyPoint = point;
        followTarget = null; // clear follow so GetFormationPosition uses rally point
        _state = UnitState.Following;
        _attackTarget = null;
        _commanderOrdered = true;
    }

    public void SetAttackTarget(Transform target)
    {
        _attackTarget = target;
        _state = UnitState.Attacking;
        _commanderOrdered = true;
        followTarget = null; // clear follow so units move to the target
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        _state = UnitState.Following;
        _attackTarget = null;
        _commanderOrdered = false; // back to autonomous behavior
    }

    void Update()
    {
        if (!IsAlive || unitData == null) return;

        switch (_state)
        {
            case UnitState.Following: UpdateFollowing(); break;
            case UnitState.HoldPosition: UpdateHoldPosition(); break;
            case UnitState.Attacking: UpdateAttacking(); break;
            case UnitState.Retreating: UpdateRetreating(); break;
        }

        ApplyGravity();
    }

    void UpdateFollowing()
    {
        // Only auto-engage if not explicitly commanded to rally/follow
        if (!_commanderOrdered && _attackTarget == null)
            ScanForThreats();

        if (_attackTarget != null && _state == UnitState.Attacking)
        {
            UpdateAttacking();
            return;
        }

        Vector3 destination = GetFormationPosition();
        float stopDist = followTarget != null ? unitData.followDistance * 0.5f : STOP_DISTANCE;
        MoveToward(destination, stopDist);
        FaceTarget(destination);

        // Once we arrive at a rally point, clear commander flag so we can defend ourselves
        if (_commanderOrdered && followTarget == null)
        {
            float dist = HorizontalDistance(transform.position, _rallyPoint);
            if (dist < 1.5f)
                _commanderOrdered = false;
        }
    }

    void UpdateHoldPosition()
    {
        if (_attackTarget == null || !IsTargetAlive(_attackTarget))
        {
            _attackTarget = null;

            // Commander-ordered hold: only react to melee-range threats
            // Non-commanded hold (e.g. enemies): scan full detection range
            if (_commanderOrdered)
                ScanForCloseThreats();
            else
                ScanForThreats();
        }

        if (_attackTarget != null)
        {
            FaceTarget(_attackTarget.position);
            float dist = HorizontalDistance(transform.position, _attackTarget.position);
            if (dist <= unitData.attackRange)
                TryAttack();
            else if (!_commanderOrdered)
            {
                // Non-commanded: move to engage
                _state = UnitState.Attacking;
            }
        }
    }

    void ScanForCloseThreats()
    {
        float closeRange = unitData.attackRange * 1.5f;
        float closest = closeRange;
        Transform best = null;
        bool iAmEnemy = GetComponent<EnemyTag>() != null;

        if (iAmEnemy)
        {
            var players = Object.FindObjectsOfType<UnitAI>();
            foreach (var p in players)
            {
                if (p == this || p == null || p.GetComponent<EnemyTag>() != null || !p.IsAlive) continue;
                float d = HorizontalDistance(transform.position, p.transform.position);
                if (d < closest) { closest = d; best = p.transform; }
            }
        }
        else
        {
            var enemies = Object.FindObjectsOfType<EnemyTag>();
            foreach (var e in enemies)
            {
                if (e == null) continue;
                var eh = e.GetComponent<UnitHealth>();
                if (eh != null && eh.IsDead) continue;
                float d = HorizontalDistance(transform.position, e.transform.position);
                if (d < closest) { closest = d; best = e.transform; }
            }
        }

        if (best != null)
            _attackTarget = best;
    }

    void UpdateAttacking()
    {
        if (_attackTarget == null || !IsTargetAlive(_attackTarget))
        {
            _attackTarget = null;
            _commanderOrdered = false;
            ScanForThreats();
            if (_attackTarget == null)
            {
                _state = UnitState.Following;
                return;
            }
        }

        // Only re-evaluate targets when not explicitly commanded
        if (!_commanderOrdered)
        {
            _retargetTimer -= Time.deltaTime;
            if (_retargetTimer <= 0f)
            {
                _retargetTimer = RETARGET_INTERVAL;
                ScanForThreats();
            }
        }

        float dist = HorizontalDistance(transform.position, _attackTarget.position);

        if (dist > unitData.attackRange)
        {
            MoveToward(_attackTarget.position, unitData.attackRange - ATTACK_MOVE_DISTANCE);
        }
        else
        {
            TryAttack();
        }

        FaceTarget(_attackTarget.position);
    }

    void UpdateRetreating()
    {
        if (followTarget == null) return;
        MoveToward(followTarget.position, STOP_DISTANCE);
        FaceTarget(followTarget.position);
    }

    void ScanForThreats()
    {
        if (unitData == null) return;

        float detectRange = unitData.threatDetectionRange;
        float closest = detectRange;
        Transform best = null;

        // Friendly units scan for EnemyTag, enemies scan for non-EnemyTag UnitAI
        bool iAmEnemy = GetComponent<EnemyTag>() != null;

        if (iAmEnemy)
        {
            var players = Object.FindObjectsOfType<UnitAI>();
            foreach (var p in players)
            {
                if (p == this || p == null) continue;
                if (p.GetComponent<EnemyTag>() != null) continue;
                if (!p.IsAlive) continue;

                float d = HorizontalDistance(transform.position, p.transform.position);
                if (d < closest)
                {
                    closest = d;
                    best = p.transform;
                }
            }

            // Also detect the player
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO == null)
            {
                var pc = Object.FindObjectOfType<PlayerController>();
                if (pc != null) playerGO = pc.gameObject;
            }
            if (playerGO != null)
            {
                var ph = playerGO.GetComponent<PlayerHealth>();
                if (ph != null && !ph.IsDead)
                {
                    float d = HorizontalDistance(transform.position, playerGO.transform.position);
                    if (d < closest)
                    {
                        closest = d;
                        best = playerGO.transform;
                    }
                }
            }
        }
        else
        {
            var enemies = Object.FindObjectsOfType<EnemyTag>();
            foreach (var e in enemies)
            {
                if (e == null) continue;
                var eh = e.GetComponent<UnitHealth>();
                if (eh != null && eh.IsDead) continue;

                float d = HorizontalDistance(transform.position, e.transform.position);
                if (d < closest)
                {
                    closest = d;
                    best = e.transform;
                }
            }
        }

        if (best != null)
        {
            _attackTarget = best;
            if (_state == UnitState.Following || _state == UnitState.HoldPosition)
                _state = UnitState.Attacking;
        }
    }

    void TryAttack()
    {
        _attackTimer -= Time.deltaTime;
        if (_attackTimer > 0f) return;

        _attackTimer = unitData.attackInterval;

        // Damage UnitHealth targets
        var targetHealth = _attackTarget.GetComponent<UnitHealth>();
        if (targetHealth != null && !targetHealth.IsDead)
        {
            targetHealth.TakeDamage(unitData.damage);
            return;
        }

        // Damage player via CombatSystem
        var combatSys = _attackTarget.GetComponent<CombatSystem>();
        if (combatSys != null)
        {
            combatSys.ReceiveAttack(unitData.damage);
            return;
        }

        // Fallback — damage PlayerHealth directly
        var playerHP = _attackTarget.GetComponent<PlayerHealth>();
        if (playerHP != null && !playerHP.IsDead)
            playerHP.TakeDamage(unitData.damage);
    }

    void MoveToward(Vector3 target, float stopDist)
    {
        Vector3 diff = target - transform.position;
        diff.y = 0f;
        float dist = diff.magnitude;

        if (dist < stopDist) return;

        Vector3 dir = diff / dist;
        float speed = unitData != null ? unitData.moveSpeed : 3.5f;
        Vector3 move = dir * speed * Time.deltaTime;
        move.y = _velocity.y * Time.deltaTime;

        if (_cc != null && _cc.enabled)
            _cc.Move(move);
    }

    void ApplyGravity()
    {
        if (_cc == null) return;

        if (_cc.isGrounded)
            _velocity.y = -0.5f;
        else
            _velocity.y -= GRAVITY * Time.deltaTime;
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
    }

    Vector3 GetFormationPosition()
    {
        if (followTarget != null)
        {
            float angle = formationIndex * 60f * Mathf.Deg2Rad;
            float radius = unitData != null ? unitData.followDistance : 2.5f;
            Vector3 offset = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
            return followTarget.position + offset;
        }

        return _rallyPoint;
    }

    bool IsTargetAlive(Transform target)
    {
        if (target == null) return false;
        var h = target.GetComponent<UnitHealth>();
        if (h != null) return !h.IsDead;
        var ph = target.GetComponent<PlayerHealth>();
        if (ph != null) return !ph.IsDead;
        return true;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
