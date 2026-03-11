using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HintButtonVisuals : MonoBehaviour
{
    [Header("Animation Settings")]
    public float pulseSpeed = 1f;
    public float pulseAmount = 0.2f;
    public bool autoStart = true;

    [Header("Colors")]
    public Color normalColor = new Color(1f, 0.8f, 0f, 0.8f); // Yellow
    public Color highlightColor = new Color(1f, 0.9f, 0.2f, 1f); // Bright yellow

    private Image image;
    private Vector3 originalScale;
    private bool isPulsing = false;

    void Start()
    {
        image = GetComponent<Image>();
        originalScale = transform.localScale;

        if (image != null)
            image.color = normalColor;

        if (autoStart)
            StartPulsing();
    }

    public void StartPulsing()
    {
        if (!isPulsing)
        {
            isPulsing = true;
            StartCoroutine(PulseAnimation());
        }
    }

    public void StopPulsing()
    {
        isPulsing = false;
        StopAllCoroutines();
        transform.localScale = originalScale;
        if (image != null)
            image.color = normalColor;
    }

    IEnumerator PulseAnimation()
    {
        while (isPulsing)
        {
            // Scale up
            float elapsed = 0f;
            float duration = 0.5f / pulseSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Scale
                float scale = Mathf.Lerp(1f, 1f + pulseAmount, Mathf.Sin(t * Mathf.PI));
                transform.localScale = originalScale * scale;

                // Color
                if (image != null)
                {
                    image.color = Color.Lerp(normalColor, highlightColor, Mathf.Sin(t * Mathf.PI));
                }

                yield return null;
            }

            // Small pause at peak
            yield return new WaitForSeconds(0.1f);

            // Scale down
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                float scale = Mathf.Lerp(1f + pulseAmount, 1f, t);
                transform.localScale = originalScale * scale;

                if (image != null)
                {
                    image.color = Color.Lerp(highlightColor, normalColor, t);
                }

                yield return null;
            }

            // Reset
            transform.localScale = originalScale;
            if (image != null)
                image.color = normalColor;

            // Pause before next pulse
            yield return new WaitForSeconds(0.5f / pulseSpeed);
        }
    }

    void OnDisable()
    {
        StopPulsing();
    }
}