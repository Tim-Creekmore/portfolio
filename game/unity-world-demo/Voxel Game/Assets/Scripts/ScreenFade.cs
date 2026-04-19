using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class ScreenFade : MonoBehaviour
{
    [SerializeField] Image fadeImage;

    public void FadeOut(float duration, Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(RunFade(0f, 1f, duration, onComplete));
    }

    public void FadeIn(float duration, Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(RunFade(1f, 0f, duration, onComplete));
    }

    IEnumerator RunFade(float from, float to, float duration, Action onComplete)
    {
        if (fadeImage == null) { onComplete?.Invoke(); yield break; }

        fadeImage.raycastTarget = true;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float alpha = Mathf.Lerp(from, to, t * t);
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0f, 0f, 0f, to);
        fadeImage.raycastTarget = to > 0.5f;
        onComplete?.Invoke();
    }

    public void SetBlack()
    {
        if (fadeImage != null)
            fadeImage.color = new Color(0f, 0f, 0f, 1f);
    }

    public void SetClear()
    {
        if (fadeImage != null)
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
    }
}
