using UnityEngine;
using System;

public class TargetHealth : MonoBehaviour
{
    [SerializeField] float maxHP = 100f;
    [SerializeField] float height = 1.8f;

    float _currentHP;
    bool _dead;

    public float CurrentHP => _currentHP;
    public float MaxHP => maxHP;
    public float Height => height;
    public bool IsDead => _dead;

    public event Action OnDeath;
    public event Action<float, WeaponData.AttackDirection> OnHit;

    void Awake()
    {
        _currentHP = maxHP;
    }

    public void TakeDamage(float damage, WeaponData.AttackDirection direction)
    {
        if (_dead) return;

        _currentHP = Mathf.Max(0f, _currentHP - damage);
        OnHit?.Invoke(damage, direction);

        if (_currentHP <= 0f)
        {
            _dead = true;
            OnDeath?.Invoke();
        }
    }

    public void ResetHealth()
    {
        _currentHP = maxHP;
        _dead = false;
    }
}
