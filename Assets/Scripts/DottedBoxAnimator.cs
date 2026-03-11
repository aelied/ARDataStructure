using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DottedBoxAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float pulseSpeed = 2f;
    public float minAlpha = 0.3f;
    public float maxAlpha = 0.8f;

    private Image boxImage;
    private Material runtimeMaterial;
    private float timer = 0f;

    void Start()
    {
        boxImage = GetComponent<Image>();
        if (boxImage == null)
        {
            Debug.LogWarning("DottedBoxAnimator: No Image component found!");
            enabled = false;
            return;
        }

        // Clone the material so we don’t edit the shared asset
        runtimeMaterial = Instantiate(boxImage.material);
        boxImage.material = runtimeMaterial;
    }

    void Update()
    {
        if (runtimeMaterial == null) return;

        // Pulse animation using sine wave
        timer += Time.deltaTime * pulseSpeed;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(timer) + 1f) / 2f);

        // Get current color from material and update alpha
        Color currentColor = runtimeMaterial.GetColor("_Color");
        currentColor.a = alpha;
        runtimeMaterial.SetColor("_Color", currentColor);
    }

    void OnDestroy()
    {
        if (Application.isPlaying && runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}
