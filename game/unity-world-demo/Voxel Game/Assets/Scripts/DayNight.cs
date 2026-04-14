using UnityEngine;

[RequireComponent(typeof(Light))]
public class DayNight : MonoBehaviour
{
    [SerializeField] float dayLengthSec = 720f;
    [SerializeField] Material skyboxMaterial;

    float _phase = 0.22f;
    Light _light;

    static readonly Color SunDay     = new Color(1.00f, 0.80f, 0.53f);   // #ffcc88 warm amber
    static readonly Color SunSunset  = new Color(1.00f, 0.53f, 0.27f);   // #ff8844
    static readonly Color SunNight   = new Color(0.13f, 0.20f, 0.67f);   // #2233aa cool blue moonlight

    static readonly Color AmbientDay   = new Color(0.376f, 0.282f, 0.188f); // #604830 warm shadow fill
    static readonly Color AmbientNight = new Color(0.08f, 0.10f, 0.18f);

    static readonly Color FogDay   = new Color(0.784f, 0.722f, 0.565f); // #c8b890 warm haze
    static readonly Color FogNight = new Color(0.10f, 0.10f, 0.14f);

    void Awake()
    {
        _light = GetComponent<Light>();

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 20f;
        RenderSettings.fogEndDistance = 55f;
    }

    void Update()
    {
        _phase = (_phase + Time.deltaTime / dayLengthSec) % 1.0f;
        float a = _phase * Mathf.PI * 2f;
        float u = 0.5f + 0.5f * Mathf.Sin(a);

        float rotX = Mathf.Lerp(-25f, -175f, u);
        float rotY = Mathf.Cos(a * 0.85f) * 22f;
        transform.rotation = Quaternion.Euler(rotX, rotY, 0f);

        float dayAmt = Mathf.Clamp01((-rotX - 25f) / 110f);

        float sunsetAmt = 1f - Mathf.Abs(dayAmt - 0.35f) / 0.35f;
        sunsetAmt = Mathf.Clamp01(sunsetAmt) * Mathf.Clamp01(dayAmt * 4f);

        _light.intensity = Mathf.Lerp(0.08f, 1.3f, dayAmt);
        Color sunBase = Color.Lerp(SunNight, SunDay, dayAmt);
        _light.color = Color.Lerp(sunBase, SunSunset, sunsetAmt * 0.5f);

        RenderSettings.ambientIntensity = Mathf.Lerp(0.12f, 0.45f, dayAmt);
        RenderSettings.ambientLight = Color.Lerp(AmbientNight, AmbientDay, dayAmt);

        RenderSettings.fogColor = Color.Lerp(FogNight, FogDay, dayAmt);

        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetColor("_SkyTopColor", new Color(
                Mathf.Lerp(0.04f, 0.30f, dayAmt),
                Mathf.Lerp(0.05f, 0.45f, dayAmt),
                Mathf.Lerp(0.14f, 0.68f, dayAmt)));

            Color horizonDay = new Color(0.78f, 0.72f, 0.58f);
            Color horizonNight = new Color(0.12f, 0.10f, 0.14f);
            Color horizon = Color.Lerp(horizonNight, horizonDay, dayAmt);
            horizon.r += sunsetAmt * 0.18f;
            horizon.g += sunsetAmt * 0.06f;
            horizon.b -= sunsetAmt * 0.08f;
            skyboxMaterial.SetColor("_SkyHorizonColor", horizon);

            skyboxMaterial.SetColor("_GroundHorizonColor", new Color(
                Mathf.Lerp(0.08f, 0.45f, dayAmt) + sunsetAmt * 0.10f,
                Mathf.Lerp(0.06f, 0.38f, dayAmt),
                Mathf.Lerp(0.08f, 0.30f, dayAmt)));

            skyboxMaterial.SetColor("_GroundBottomColor", new Color(
                Mathf.Lerp(0.03f, 0.16f, dayAmt),
                Mathf.Lerp(0.03f, 0.14f, dayAmt),
                Mathf.Lerp(0.05f, 0.10f, dayAmt)));
        }
    }
}
