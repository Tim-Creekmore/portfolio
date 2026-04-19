using UnityEngine;

public class AmbientAudio : MonoBehaviour
{
    AudioSource _windSource;
    AudioSource _birdsSource;

    void Awake()
    {
        var windGO = transform.Find("WindLoop");
        var birdsGO = transform.Find("BirdsLoop");

        if (windGO != null)
            _windSource = windGO.GetComponent<AudioSource>();
        if (birdsGO != null)
            _birdsSource = birdsGO.GetComponent<AudioSource>();
    }

    void Start()
    {
        if (_windSource != null)
        {
            _windSource.clip = GenerateWind(8f, 44100);
            _windSource.Play();
        }
        if (_birdsSource != null)
        {
            _birdsSource.clip = GenerateBirds(12f, 44100);
            _birdsSource.Play();
        }
    }

    static AudioClip GenerateWind(float duration, int sampleRate)
    {
        int samples = (int)(duration * sampleRate);
        float[] data = new float[samples];

        float lp = 0f;
        float lp2 = 0f;
        float swell = 0f;
        float swellFreq = 0.08f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float noise = Random.Range(-1f, 1f);

            // Two-pole low-pass for deep rumble
            lp += (noise - lp) * 0.003f;
            lp2 += (lp - lp2) * 0.003f;

            // Slow volume swell for natural gusting
            swell = Mathf.Sin(t * swellFreq * Mathf.PI * 2f) * 0.3f + 0.7f;
            swell *= Mathf.Sin(t * swellFreq * 0.37f * Mathf.PI * 2f) * 0.2f + 0.8f;

            data[i] = lp2 * swell * 0.7f;
        }

        var clip = AudioClip.Create("Wind", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip GenerateBirds(float duration, int sampleRate)
    {
        int samples = (int)(duration * sampleRate);
        float[] data = new float[samples];

        // Pre-generate bird chirp events (random timing)
        int chirpCount = (int)(duration * 1.5f);
        float[] chirpTimes = new float[chirpCount];
        float[] chirpFreqs = new float[chirpCount];
        float[] chirpDurations = new float[chirpCount];

        var rng = new System.Random(42);
        for (int c = 0; c < chirpCount; c++)
        {
            chirpTimes[c] = (float)(rng.NextDouble() * duration);
            chirpFreqs[c] = 2200f + (float)(rng.NextDouble() * 2800f);
            chirpDurations[c] = 0.04f + (float)(rng.NextDouble() * 0.12f);
        }

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float val = 0f;

            for (int c = 0; c < chirpCount; c++)
            {
                float dt = t - chirpTimes[c];
                if (dt < 0f || dt > chirpDurations[c]) continue;

                float env = Mathf.Sin(dt / chirpDurations[c] * Mathf.PI);
                float freq = chirpFreqs[c] + dt * 1500f;
                val += Mathf.Sin(dt * freq * Mathf.PI * 2f) * env * 0.3f;
            }

            data[i] = Mathf.Clamp(val, -1f, 1f);
        }

        var clip = AudioClip.Create("Birds", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
