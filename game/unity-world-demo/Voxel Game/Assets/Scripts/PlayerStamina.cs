using UnityEngine;
using System;

public class PlayerStamina : MonoBehaviour
{
    [SerializeField] float maxStamina = 250f;
    [SerializeField] float regenRate = 14f;
    [SerializeField] float regenDelay = 0.6f;

    float _current;
    float _regenCooldown;

    public float Current => _current;
    public float Max => maxStamina;
    public float Ratio => _current / maxStamina;
    public bool IsEmpty => _current <= 0f;

    public event Action<float, float> OnChanged; // (current, max)

    void Awake()
    {
        _current = maxStamina;
    }

    void Update()
    {
        if (_regenCooldown > 0f)
        {
            _regenCooldown -= Time.deltaTime;
            return;
        }

        if (_current < maxStamina)
        {
            _current = Mathf.Min(maxStamina, _current + regenRate * Time.deltaTime);
            OnChanged?.Invoke(_current, maxStamina);
        }
    }

    public bool TryConsume(float amount)
    {
        if (_current < amount) return false;
        _current -= amount;
        _regenCooldown = regenDelay;
        OnChanged?.Invoke(_current, maxStamina);
        return true;
    }

    public void Drain(float amount)
    {
        _current = Mathf.Max(0f, _current - amount);
        _regenCooldown = regenDelay;
        OnChanged?.Invoke(_current, maxStamina);
    }

    public void ResetStamina()
    {
        _current = maxStamina;
        _regenCooldown = 0f;
        OnChanged?.Invoke(_current, maxStamina);
    }

    public void SetStamina(float value)
    {
        _current = Mathf.Clamp(value, 0f, maxStamina);
        _regenCooldown = 0f;
        OnChanged?.Invoke(_current, maxStamina);
    }
}
