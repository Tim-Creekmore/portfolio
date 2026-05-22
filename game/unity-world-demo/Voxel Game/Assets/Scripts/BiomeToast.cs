using UnityEngine;
using UnityEngine.UI;

public class BiomeToast : MonoBehaviour
{
    [SerializeField] CanvasGroup toastGroup;
    [SerializeField] Text toastLabel;

    const float FADE_IN   = 0.3f;
    const float HOLD      = 1.5f;
    const float FADE_OUT  = 0.5f;
    const float DEBOUNCE  = 0.5f;

    string _currentKey;
    string _displayedKey;
    float _timer;
    float _debounceTimer;
    enum State { Idle, FadeIn, Hold, FadeOut }
    State _state = State.Idle;

    void Start()
    {
        if (toastGroup != null) toastGroup.alpha = 0f;
        _currentKey = SampleBiomeKey(out _);
        _displayedKey = _currentKey;
    }

    void Update()
    {
        string key = SampleBiomeKey(out string displayName);

        if (key != _currentKey)
        {
            _currentKey = key;
            _debounceTimer = DEBOUNCE;
        }

        if (_debounceTimer > 0f)
        {
            _debounceTimer -= Time.deltaTime;
            if (_debounceTimer <= 0f && _currentKey != _displayedKey)
            {
                ShowToast(_currentKey, displayName);
            }
        }

        UpdateFade();
    }

    /// <summary>
    /// Returns a stable key for the current biome (for change detection) and a display name.
    /// Prefers BiomeRegistry when in Phase B biome world; falls back to legacy enum.
    /// </summary>
    string SampleBiomeKey(out string displayName)
    {
        Vector3 pos = transform.position;

        if (!WorldData.ARENA_MODE && BiomeRegistry.IsReady)
        {
            var data = BiomeRegistry.Instance.GetBiomeAt(pos.x, pos.z);
            if (data != null)
            {
                displayName = string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName;
                return "biome:" + data.name;
            }
        }

        var b = WorldData.GetBiome(pos.x, pos.z);
        displayName = WorldData.BiomeDisplayName(b);
        return "enum:" + b.ToString();
    }

    void ShowToast(string key, string displayName)
    {
        _displayedKey = key;
        if (toastLabel != null)
            toastLabel.text = "~ " + displayName + " ~";

        _state = State.FadeIn;
        _timer = 0f;
    }

    void UpdateFade()
    {
        if (toastGroup == null) return;

        switch (_state)
        {
            case State.Idle:
                break;

            case State.FadeIn:
                _timer += Time.deltaTime;
                toastGroup.alpha = Mathf.Clamp01(_timer / FADE_IN);
                if (_timer >= FADE_IN)
                {
                    _state = State.Hold;
                    _timer = 0f;
                }
                break;

            case State.Hold:
                toastGroup.alpha = 1f;
                _timer += Time.deltaTime;
                if (_timer >= HOLD)
                {
                    _state = State.FadeOut;
                    _timer = 0f;
                }
                break;

            case State.FadeOut:
                _timer += Time.deltaTime;
                toastGroup.alpha = 1f - Mathf.Clamp01(_timer / FADE_OUT);
                if (_timer >= FADE_OUT)
                {
                    toastGroup.alpha = 0f;
                    _state = State.Idle;
                }
                break;
        }
    }
}
