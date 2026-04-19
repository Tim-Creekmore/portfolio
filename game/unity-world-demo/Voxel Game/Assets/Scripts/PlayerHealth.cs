using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] float maxHP = 100f;

    float _currentHP;
    bool _dead;

    public float CurrentHP => _currentHP;
    public float MaxHP => maxHP;
    public float HPRatio => _currentHP / maxHP;
    public bool IsDead => _dead;

    public event Action OnDeath;
    public event Action<float, float> OnDamaged; // (damage, remainingHP)
    public event Action<float, float> OnHealed;  // (amount, remainingHP)

    void Awake()
    {
        _currentHP = maxHP;
    }

    public void TakeDamage(float damage)
    {
        if (_dead || damage <= 0f) return;

        _currentHP = Mathf.Max(0f, _currentHP - damage);
        OnDamaged?.Invoke(damage, _currentHP);

        if (_currentHP <= 0f)
        {
            _dead = true;
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (_dead || amount <= 0f) return;

        float before = _currentHP;
        _currentHP = Mathf.Min(maxHP, _currentHP + amount);
        if (_currentHP > before)
            OnHealed?.Invoke(_currentHP - before, _currentHP);
    }

    public void ResetHealth()
    {
        _currentHP = maxHP;
        _dead = false;
    }
}
