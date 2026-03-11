using UnityEngine;
using TMPro;
using System.Collections;

public class NodeVisualizer : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TextMeshPro dataText;
    [SerializeField] private TextMeshPro indexText;
    [SerializeField] private Renderer nodeRenderer;
    [SerializeField] private GameObject glowRing;
    [SerializeField] private GameObject frontBadge;
    
    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.6f, 1f, 0.8f);
    [SerializeField] private Color frontColor = new Color(0.2f, 1f, 0.2f, 0.8f);
    [SerializeField] private Color removeColor = new Color(1f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0.2f, 0.9f);
    
    private Material nodeMaterial;
    private bool isFront = false;
    
    void Awake()
    {
        if (nodeRenderer != null)
        {
            nodeMaterial = nodeRenderer.material;
        }
        
        if (glowRing != null)
        {
            glowRing.SetActive(true);
        }
    }
    
    void Update()
    {
        // Always face camera
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0); // Flip to face forward
        }
        
        // Gentle floating animation
        float offset = Mathf.Sin(Time.time * 2f) * 0.01f;
        transform.position = new Vector3(
            transform.position.x,
            transform.position.y + offset * Time.deltaTime,
            transform.position.z
        );
    }
    
    public void SetData(string data, int index, bool isFrontNode)
    {
        isFront = isFrontNode;
        
        if (dataText != null)
        {
            dataText.text = data;
            dataText.fontSize = 0.3f;
            dataText.alignment = TextAlignmentOptions.Center;
        }
        
        if (indexText != null)
        {
            indexText.text = $"[{index}]";
            indexText.fontSize = 0.2f;
            indexText.alignment = TextAlignmentOptions.Center;
        }
        
        if (frontBadge != null)
        {
            frontBadge.SetActive(isFrontNode);
        }
        
        UpdateColor();
    }
    
    void UpdateColor()
    {
        Color targetColor = isFront ? frontColor : normalColor;
        
        if (nodeMaterial != null)
        {
            nodeMaterial.color = targetColor;
        }
        
        if (glowRing != null)
        {
            Renderer glowRenderer = glowRing.GetComponent<Renderer>();
            if (glowRenderer != null)
            {
                glowRenderer.material.color = targetColor;
            }
        }
    }
    
    public void MarkForRemoval()
    {
        StartCoroutine(RemovalAnimation());
    }
    
    IEnumerator RemovalAnimation()
    {
        // Change color to red
        if (nodeMaterial != null)
        {
            nodeMaterial.color = removeColor;
        }
        
        // Pulsate
        float duration = 1.5f;
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = 1f + Mathf.Sin(elapsed * 10f) * 0.2f;
            transform.localScale = originalScale * scale;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    public void Highlight()
    {
        StartCoroutine(HighlightAnimation());
    }
    
    IEnumerator HighlightAnimation()
    {
        Color originalColor = nodeMaterial.color;
        
        // Flash highlight color
        for (int i = 0; i < 3; i++)
        {
            nodeMaterial.color = highlightColor;
            yield return new WaitForSeconds(0.3f);
            nodeMaterial.color = originalColor;
            yield return new WaitForSeconds(0.3f);
        }
    }
    
    public void ShowEnqueueIndicator()
    {
        StartCoroutine(EnqueuePulse());
    }
    
    IEnumerator EnqueuePulse()
    {
        if (glowRing != null)
        {
            float duration = 2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 0.5f + Mathf.Sin(elapsed * 5f) * 0.3f;
                
                Renderer glowRenderer = glowRing.GetComponent<Renderer>();
                if (glowRenderer != null)
                {
                    Color glowColor = glowRenderer.material.color;
                    glowColor.a = alpha;
                    glowRenderer.material.color = glowColor;
                }
                
                yield return null;
            }
        }
    }
}