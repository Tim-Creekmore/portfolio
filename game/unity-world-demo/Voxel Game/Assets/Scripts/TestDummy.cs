using UnityEngine;

public class TestDummy : MonoBehaviour
{
    [SerializeField] float respawnDelay = 5f;
    [SerializeField] bool autoRespawn = true;

    TargetHealth _health;
    MeshRenderer _headMR, _leftMR, _rightMR, _legsMR;
    Color _headCol, _leftCol, _rightCol, _legsCol;

    GameObject _damagePopup;
    float _popupTimer;
    float _respawnTimer;
    bool _waitingRespawn;

    // Zone flash tracking
    MeshRenderer _lastFlashed;
    float _flashTimer;

    void Awake()
    {
        _health = GetComponent<TargetHealth>();
        if (_health != null)
        {
            _health.OnHit += HandleHit;
            _health.OnDeath += HandleDeath;
        }
    }

    public void AssignZoneRenderers(MeshRenderer head, MeshRenderer left, MeshRenderer right, MeshRenderer legs)
    {
        _headMR = head;
        _leftMR = left;
        _rightMR = right;
        _legsMR = legs;

        _headCol = head.sharedMaterial.color;
        _leftCol = left.sharedMaterial.color;
        _rightCol = right.sharedMaterial.color;
        _legsCol = legs.sharedMaterial.color;
    }

    void Update()
    {
        if (_popupTimer > 0f)
        {
            _popupTimer -= Time.deltaTime;
            if (_damagePopup != null)
            {
                _damagePopup.transform.position += Vector3.up * Time.deltaTime * 0.8f;
                if (_popupTimer <= 0f)
                    Destroy(_damagePopup);
            }
        }

        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f && _lastFlashed != null)
                ResetZoneColor(_lastFlashed);
        }

        if (_waitingRespawn)
        {
            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f)
                Respawn();
        }
    }

    void HandleHit(float damage, WeaponData.AttackDirection dir)
    {
        SpawnDamageNumber(damage, dir);
        FlashZone(dir);
    }

    void HandleDeath()
    {
        SetAllZoneColors(new Color(0.2f, 0.2f, 0.2f));
        if (autoRespawn)
        {
            _waitingRespawn = true;
            _respawnTimer = respawnDelay;
        }
    }

    void Respawn()
    {
        _waitingRespawn = false;
        _health.ResetHealth();
        ResetAllZoneColors();
    }

    void FlashZone(WeaponData.AttackDirection dir)
    {
        MeshRenderer zone = GetZoneRenderer(dir);
        if (zone == null) return;

        _lastFlashed = zone;
        zone.material.color = new Color(1f, 0.25f, 0.15f);
        _flashTimer = 0.15f;
    }

    void ResetZoneColor(MeshRenderer mr)
    {
        if (mr == _headMR)  mr.material.color = _headCol;
        if (mr == _leftMR)  mr.material.color = _leftCol;
        if (mr == _rightMR) mr.material.color = _rightCol;
        if (mr == _legsMR)  mr.material.color = _legsCol;
    }

    void SetAllZoneColors(Color c)
    {
        if (_headMR != null) _headMR.material.color = c;
        if (_leftMR != null) _leftMR.material.color = c;
        if (_rightMR != null) _rightMR.material.color = c;
        if (_legsMR != null) _legsMR.material.color = c;
    }

    void ResetAllZoneColors()
    {
        if (_headMR != null) _headMR.material.color = _headCol;
        if (_leftMR != null) _leftMR.material.color = _leftCol;
        if (_rightMR != null) _rightMR.material.color = _rightCol;
        if (_legsMR != null) _legsMR.material.color = _legsCol;
    }

    MeshRenderer GetZoneRenderer(WeaponData.AttackDirection dir)
    {
        switch (dir)
        {
            case WeaponData.AttackDirection.Overhead: return _headMR;
            case WeaponData.AttackDirection.Left:     return _leftMR;
            case WeaponData.AttackDirection.Right:    return _rightMR;
            case WeaponData.AttackDirection.Thrust:   return _legsMR;
            default: return null;
        }
    }

    void SpawnDamageNumber(float damage, WeaponData.AttackDirection dir)
    {
        if (_damagePopup != null)
            Destroy(_damagePopup);

        _damagePopup = new GameObject("DmgPopup");
        _damagePopup.transform.position = transform.position + Vector3.up * 2.4f;

        var tm = _damagePopup.AddComponent<TextMesh>();
        tm.text = $"-{damage:0} ({dir})";
        tm.characterSize = 0.15f;
        tm.fontSize = 48;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = new Color(1f, 0.3f, 0.2f);

        _damagePopup.AddComponent<Billboard>();
        _popupTimer = 1.5f;
    }
}
