using UnityEngine;
using UnityEngine.UI;

public class CombatHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] PlayerStamina playerStamina;
    [SerializeField] CombatSystem combatSystem;
    [SerializeField] SquadManager squadManager;

    [Header("Health Bar")]
    [SerializeField] RectTransform healthFillRT;
    [SerializeField] Image healthFillImage;

    [Header("Stamina Bar")]
    [SerializeField] RectTransform staminaFillRT;
    [SerializeField] Image staminaFillImage;

    [Header("Squad")]
    [SerializeField] Text squadCountText;

    [Header("Feedback")]
    [SerializeField] Text damageText;
    [SerializeField] Text outgoingToast;

    [Header("Debug")]
    [SerializeField] bool enableDebugDamage = true;

    float _damageTextTimer;
    float _toastTimer;
    float _toastFadeDuration = 1.2f;
    float _toastShowDuration = 1.5f;
    CanvasGroup _toastCanvasGroup;

    void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnDamaged += HandleDamaged;
        if (combatSystem != null)
        {
            combatSystem.OnBlockedHit += HandleFullBlock;
            combatSystem.OnDamageDealt += HandleOutgoingDamage;
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDamaged -= HandleDamaged;
        if (combatSystem != null)
        {
            combatSystem.OnBlockedHit -= HandleFullBlock;
            combatSystem.OnDamageDealt -= HandleOutgoingDamage;
        }
    }

    void HandleDamaged(float damage, float remainingHP)
    {
        string blockInfo = "";
        if (combatSystem != null && combatSystem.IsBlocking)
            blockInfo = combatSystem.ShieldEquipped ? " [SHIELD]" : " [BLOCKED]";

        ShowDamageText($"-{damage:0}{blockInfo}  HP: {remainingHP:0}/{playerHealth.MaxHP:0}");
    }

    void HandleFullBlock(float baseDmg, float finalDmg, bool shieldUsed)
    {
        float hp = playerHealth != null ? playerHealth.CurrentHP : 0f;
        float max = playerHealth != null ? playerHealth.MaxHP : 0f;
        ShowDamageText($"BLOCKED {baseDmg:0} [SHIELD]  HP: {hp:0}/{max:0}");
    }

    void HandleOutgoingDamage(float damage, string targetName)
    {
        ShowOutgoingToast($"{damage:0} dmg");
    }

    void Update()
    {
        if (playerHealth != null && healthFillRT != null)
        {
            float ratio = playerHealth.HPRatio;
            healthFillRT.anchorMax = new Vector2(ratio, 1f);

            if (healthFillImage != null)
            {
                healthFillImage.color = Color.Lerp(
                    new Color(0.8f, 0.1f, 0.08f),
                    new Color(0.7f, 0.15f, 0.12f),
                    ratio);
            }
        }

        if (playerStamina != null && staminaFillRT != null)
        {
            float stRatio = playerStamina.Ratio;
            staminaFillRT.anchorMax = new Vector2(stRatio, 1f);

            if (staminaFillImage != null)
            {
                staminaFillImage.color = Color.Lerp(
                    new Color(0.6f, 0.4f, 0.1f),
                    new Color(0.85f, 0.7f, 0.15f),
                    stRatio);
            }
        }

        if (_damageTextTimer > 0f)
        {
            _damageTextTimer -= Time.deltaTime;
            if (_damageTextTimer <= 0f && damageText != null)
                damageText.text = "";
        }

        if (squadManager != null && squadCountText != null)
            squadCountText.text = $"Squad: {squadManager.AliveCount}/{squadManager.TotalCount}";

        UpdateToastFade();

        if (enableDebugDamage && Input.GetKeyDown(KeyCode.P) && playerHealth != null && !playerHealth.IsDead)
            playerHealth.TakeDamage(25f);
    }

    void ShowDamageText(string msg)
    {
        if (damageText != null)
        {
            damageText.text = msg;
            _damageTextTimer = 2f;
        }
    }

    void ShowOutgoingToast(string msg)
    {
        if (outgoingToast == null) return;

        outgoingToast.text = msg;
        _toastTimer = _toastShowDuration + _toastFadeDuration;

        if (_toastCanvasGroup == null)
            _toastCanvasGroup = outgoingToast.GetComponent<CanvasGroup>();
        if (_toastCanvasGroup == null)
            _toastCanvasGroup = outgoingToast.gameObject.AddComponent<CanvasGroup>();

        _toastCanvasGroup.alpha = 1f;
    }

    void UpdateToastFade()
    {
        if (_toastTimer <= 0f) return;

        _toastTimer -= Time.deltaTime;

        if (_toastCanvasGroup != null)
        {
            if (_toastTimer <= _toastFadeDuration)
            {
                float t = Mathf.Clamp01(_toastTimer / _toastFadeDuration);
                _toastCanvasGroup.alpha = t;
            }
            else
            {
                _toastCanvasGroup.alpha = 1f;
            }
        }

        if (_toastTimer <= 0f && outgoingToast != null)
            outgoingToast.text = "";
    }
}
