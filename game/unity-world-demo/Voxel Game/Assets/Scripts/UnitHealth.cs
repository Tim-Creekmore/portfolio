using UnityEngine;
using System;

public class UnitHealth : MonoBehaviour
{
    float _maxHP;
    float _currentHP;
    bool _dead;

    public float CurrentHP => _currentHP;
    public float MaxHP => _maxHP;
    public float HPRatio => _maxHP > 0f ? _currentHP / _maxHP : 0f;
    public bool IsDead => _dead;

    public event Action<UnitHealth> OnDeath;
    public event Action<float> OnDamaged;

    static Material _deathMaterial;

    public void Init(float maxHP)
    {
        _maxHP = maxHP;
        _currentHP = maxHP;
        _dead = false;
    }

    public void TakeDamage(float damage)
    {
        if (_dead || damage <= 0f) return;

        _currentHP = Mathf.Max(0f, _currentHP - damage);
        OnDamaged?.Invoke(damage);

        if (_currentHP <= 0f)
        {
            _dead = true;
            ApplyDeathVisual();
            OnDeath?.Invoke(this);
        }
    }

    public void Heal(float amount)
    {
        if (_dead || amount <= 0f) return;
        _currentHP = Mathf.Min(_maxHP, _currentHP + amount);
    }

    void ApplyDeathVisual()
    {
        if (_deathMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return;
            _deathMaterial = new Material(shader);
            _deathMaterial.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.35f));
            _deathMaterial.SetFloat("_Surface", 1f);
            _deathMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _deathMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _deathMaterial.SetInt("_ZWrite", 0);
            _deathMaterial.renderQueue = 3000;
            _deathMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        var renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
            r.sharedMaterial = _deathMaterial;

        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Self-cleanup after 3 seconds (applies to all units — friendly and enemy)
        Destroy(gameObject, 3f);
    }
}
