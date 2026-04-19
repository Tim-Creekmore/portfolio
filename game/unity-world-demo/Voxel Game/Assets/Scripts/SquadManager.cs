using UnityEngine;
using System.Collections.Generic;

public class SquadManager : MonoBehaviour
{
    [SerializeField] int maxSquadSize = 6;
    [SerializeField] Transform followTarget;

    readonly List<UnitAI> _units = new List<UnitAI>();
    int _maxRecruited;

    public IReadOnlyList<UnitAI> Units => _units;
    public int AliveCount { get { int c = 0; foreach (var u in _units) if (u != null && u.IsAlive) c++; return c; } }
    public int TotalCount => _maxRecruited;

    void Start()
    {
        var allUnits = Object.FindObjectsOfType<UnitAI>();
        foreach (var u in allUnits)
        {
            if (u.GetComponent<EnemyTag>() != null) continue;
            if (_units.Count >= maxSquadSize) break;

            _units.Add(u);
            u.Health.OnDeath += HandleUnitDeath;

            if (followTarget != null)
                u.SetFollowTarget(followTarget);
        }
        _maxRecruited = _units.Count;
    }

    public void SetFollowTarget(Transform target) { followTarget = target; }

    public bool AddUnit(UnitAI unit)
    {
        if (_units.Count >= maxSquadSize) return false;
        _units.Add(unit);
        _maxRecruited = Mathf.Max(_maxRecruited, _units.Count);
        unit.Health.OnDeath += HandleUnitDeath;
        return true;
    }

    public void RemoveUnit(UnitAI unit)
    {
        if (unit != null && unit.Health != null)
            unit.Health.OnDeath -= HandleUnitDeath;
        _units.Remove(unit);
    }

    public void ResetSquad()
    {
        _units.Clear();
        _maxRecruited = 0;
    }

    public void SetAllState(UnitAI.UnitState state)
    {
        foreach (var u in _units)
        {
            if (u != null && u.IsAlive)
            {
                if (state == UnitAI.UnitState.Following && followTarget != null)
                    u.SetFollowTarget(followTarget);
                else
                    u.SetState(state);
            }
        }
    }

    public void SetRallyPoint(Vector3 position)
    {
        foreach (var u in _units)
        {
            if (u != null && u.IsAlive)
                u.SetRallyPoint(position);
        }
    }

    public void SetAttackTarget(Transform target)
    {
        foreach (var u in _units)
        {
            if (u != null && u.IsAlive)
                u.SetAttackTarget(target);
        }
    }

    void HandleUnitDeath(UnitHealth deadUnit)
    {
        deadUnit.OnDeath -= HandleUnitDeath;

        for (int i = _units.Count - 1; i >= 0; i--)
        {
            if (_units[i] != null && _units[i].Health == deadUnit)
            {
                _units.RemoveAt(i);
                break;
            }
        }
    }
}
