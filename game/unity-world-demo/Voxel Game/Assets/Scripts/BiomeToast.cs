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

    WorldData.Biome _currentBiome;
    WorldData.Biome _displayedBiome;
    float _timer;
    float _debounceTimer;
    enum State { Idle, FadeIn, Hold, FadeOut }
    State _state = State.Idle;

    void Start()
    {
        if (toastGroup != null) toastGroup.alpha = 0f;
        _currentBiome = SampleBiome();
        _displayedBiome = _currentBiome;
    }

    void Update()
    {
        var biome = SampleBiome();

        if (biome != _currentBiome)
        {
            _currentBiome = biome;
            _debounceTimer = DEBOUNCE;
        }

        if (_debounceTimer > 0f)
        {
            _debounceTimer -= Time.deltaTime;
            if (_debounceTimer <= 0f && _currentBiome != _displayedBiome)
            {
                ShowToast(_currentBiome);
            }
        }

        UpdateFade();
    }

    WorldData.Biome SampleBiome()
    {
        Vector3 pos = transform.position;
        return WorldData.GetBiome(pos.x, pos.z);
    }

    void ShowToast(WorldData.Biome biome)
    {
        _displayedBiome = biome;
        if (toastLabel != null)
            toastLabel.text = "~ " + WorldData.BiomeDisplayName(biome) + " ~";

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
