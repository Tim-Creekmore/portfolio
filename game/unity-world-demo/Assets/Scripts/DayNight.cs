using UnityEngine;

[RequireComponent(typeof(Light))]
public class DayNight : MonoBehaviour
{
    [SerializeField] float dayLengthSec = 720f;
    [SerializeField] Material skyboxMaterial;

    float _phase = 0.22f;
    Light _light;

    void Awake()
    {
        _light = GetComponent<Light>();
    }

    void Update()
    {
        _phase = (_phase + Time.deltaTime / dayLengthSec) % 1.0f;
        float a = _phase * Mathf.PI * 2f;
        float u = 0.5f + 0.5f * Mathf.Sin(a);

        // Sun rotation
        float rotX = Mathf.Lerp(-32f, -172f, u);
        float rotY = Mathf.Cos(a * 0.85f) * 22f;
        transform.rotation = Quaternion.Euler(rotX, rotY, 0f);

        float dayAmt = Mathf.Clamp01((-rotX - 32f) / 105f);

        // Light
        _light.intensity = Mathf.Lerp(0.1f, 1.32f, dayAmt);
        _light.color = new Color(
            1f,
            Mathf.Lerp(0.62f, 0.93f, dayAmt),
            Mathf.Lerp(0.38f, 0.76f, dayAmt));

        float sunsetAmt = 1f - Mathf.Abs(dayAmt - 0.35f) / 0.35f;
        sunsetAmt = Mathf.Clamp01(sunsetAmt) * Mathf.Clamp01(dayAmt * 4f);

        // Ambient
        RenderSettings.ambientIntensity = Mathf.Lerp(0.11f, 0.4f, dayAmt);
        RenderSettings.ambientLight = new Color(
            Mathf.Lerp(0.38f, 0.56f, dayAmt) + sunsetAmt * 0.08f,
            Mathf.Lerp(0.42f, 0.48f, dayAmt),
            Mathf.Lerp(0.52f, 0.42f, dayAmt));

        // Fog
        RenderSettings.fogColor = new Color(
            Mathf.Lerp(0.22f, 0.68f, dayAmt) + sunsetAmt * 0.15f,
            Mathf.Lerp(0.18f, 0.62f, dayAmt) + sunsetAmt * 0.05f,
            Mathf.Lerp(0.25f, 0.52f, dayAmt));

        // Skybox
        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetColor("_SkyTopColor", new Color(
                Mathf.Lerp(0.06f, 0.32f, dayAmt),
                Mathf.Lerp(0.08f, 0.48f, dayAmt),
                Mathf.Lerp(0.18f, 0.72f, dayAmt)));

            skyboxMaterial.SetColor("_SkyHorizonColor", new Color(
                Mathf.Lerp(0.15f, 0.72f, dayAmt) + sunsetAmt * 0.2f,
                Mathf.Lerp(0.10f, 0.65f, dayAmt) + sunsetAmt * 0.08f,
                Mathf.Lerp(0.12f, 0.55f, dayAmt) - sunsetAmt * 0.1f));

            skyboxMaterial.SetColor("_GroundHorizonColor", new Color(
                Mathf.Lerp(0.10f, 0.48f, dayAmt) + sunsetAmt * 0.12f,
                Mathf.Lerp(0.08f, 0.42f, dayAmt),
                Mathf.Lerp(0.10f, 0.36f, dayAmt)));

            skyboxMaterial.SetColor("_GroundBottomColor", new Color(
                Mathf.Lerp(0.04f, 0.18f, dayAmt),
                Mathf.Lerp(0.04f, 0.16f, dayAmt),
                Mathf.Lerp(0.06f, 0.12f, dayAmt)));
        }
    }
}
