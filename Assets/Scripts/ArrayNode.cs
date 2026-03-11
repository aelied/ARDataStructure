using UnityEngine;
using TMPro;

public class ArrayNode : MonoBehaviour
{
    [Header("Node Settings")]
    public string nodeValue = "";
    public int nodeIndex = -1;
    
    [Header("Text References - Will Auto-Create if Missing")]
    public TextMeshPro valueText;
    public TextMeshPro indexText;
    
    [Header("Visual Settings")]
    public Color nodeColor = new Color(0.3f, 0.7f, 1f); // Light blue
    public Color accessColor = Color.yellow;
    public Color updateColor = Color.green;
    
    [Header("Animation Settings")]
    public float appearDuration = 0.5f;
    public float disappearDuration = 0.5f;
    public float moveDuration = 0.3f;
    public float updateDuration = 0.3f;
    
    private Vector3 originalScale;
    private Renderer nodeRenderer;
    private Material nodeMaterial;
    
    void Awake()
    {
        // Store original scale
        originalScale = transform.localScale;
        
        // Get or create renderer
        nodeRenderer = GetComponent<Renderer>();
        if (nodeRenderer == null)
        {
            // Create a cube if no renderer exists
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(transform);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = Vector3.one;
            nodeRenderer = cube.GetComponent<Renderer>();
            Debug.Log("✅ Created cube renderer for node");
        }
        
        // Create material
        nodeMaterial = new Material(Shader.Find("Standard"));
        nodeMaterial.color = nodeColor;
        nodeRenderer.material = nodeMaterial;
        
        // Create or find value text
        if (valueText == null)
        {
            valueText = CreateTextObject("ValueText", Vector3.forward * 0.51f, 4f);
        }
        
        // Create or find index text
        if (indexText == null)
        {
            indexText = CreateTextObject("IndexText", Vector3.forward * 0.51f + Vector3.down * 0.6f, 2f);
        }
        
        Debug.Log($"✅ ArrayNode initialized: ValueText={valueText != null}, IndexText={indexText != null}");
    }
    
    // Create a TextMeshPro object
    private TextMeshPro CreateTextObject(string name, Vector3 localPosition, float fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = localPosition;
        textObj.transform.localRotation = Quaternion.identity;
        textObj.transform.localScale = Vector3.one;
        
        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        
        // Set sorting order to render in front
        tmp.sortingOrder = 10;
        
        Debug.Log($"✅ Created TextMeshPro: {name}");
        return tmp;
    }
    
    // Set the value displayed on this node
    public void SetValue(string value)
    {
        nodeValue = value;
        if (valueText != null)
        {
            valueText.text = value;
            Debug.Log($"✅ Set value text to: {value}");
        }
        else
        {
            Debug.LogError("❌ valueText is null!");
        }
    }
    
    // Set the index displayed on this node
    public void SetIndex(int index)
    {
        nodeIndex = index;
        if (indexText != null)
        {
            indexText.text = $"[{index}]";
            Debug.Log($"✅ Set index text to: [{index}]");
        }
        else
        {
            Debug.LogError("❌ indexText is null!");
        }
    }
    
    // Update both value and index
    public void SetData(string value, int index)
    {
        Debug.Log($"🔹 SetData called: value={value}, index={index}");
        SetValue(value);
        SetIndex(index);
    }
    
    // Animate the node appearing (when inserted)
    public void AnimateAppear()
    {
        StartCoroutine(AnimateAppearCoroutine());
    }
    
    // Animate the node disappearing (when deleted)
    public void AnimateDisappear()
    {
        StartCoroutine(AnimateDisappearCoroutine());
    }
    
    // Move to a new position (used when array elements shift)
    public void MoveTo(Vector3 newPosition)
    {
        StartCoroutine(MoveToCoroutine(newPosition));
    }
    
    // Highlight node when accessed
    public void HighlightAccess()
    {
        StartCoroutine(HighlightCoroutine(accessColor));
    }
    
    // Highlight node when updated
    public void HighlightUpdate()
    {
        StartCoroutine(HighlightCoroutine(updateColor));
    }
    
    // Animate value update
    public void AnimateValueUpdate(string newValue)
    {
        StartCoroutine(AnimateValueUpdateCoroutine(newValue));
    }
    
    // Coroutine for appear animation
    private System.Collections.IEnumerator AnimateAppearCoroutine()
    {
        // Start from small scale
        transform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        
        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / appearDuration;
            
            // Ease out back (slight overshoot)
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float smoothT = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, smoothT);
            
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    // Coroutine for disappear animation
    private System.Collections.IEnumerator AnimateDisappearCoroutine()
    {
        Vector3 startScale = transform.localScale;
        
        float elapsed = 0f;
        
        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / disappearDuration;
            
            // Ease in
            float smoothT = t * t;
            
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, smoothT);
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
    
    // Coroutine for moving to a new position (for shifting elements)
    private System.Collections.IEnumerator MoveToCoroutine(Vector3 newPosition)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            
            // Ease out cubic
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            
            transform.position = Vector3.Lerp(startPos, newPosition, smoothT);
            
            yield return null;
        }
        
        transform.position = newPosition;
    }
    
    // Coroutine for highlighting the node
    private System.Collections.IEnumerator HighlightCoroutine(Color highlightColor)
    {
        if (nodeMaterial == null) yield break;
        
        // Store original color
        Color originalColor = nodeMaterial.color;
        
        // Pulse to highlight color
        float elapsed = 0f;
        float pulseDuration = 0.3f;
        
        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pulseDuration;
            
            nodeMaterial.color = Color.Lerp(originalColor, highlightColor, Mathf.Sin(t * Mathf.PI));
            
            yield return null;
        }
        
        // Return to original color
        nodeMaterial.color = originalColor;
    }
    
    // Coroutine for animating value update
    private System.Collections.IEnumerator AnimateValueUpdateCoroutine(string newValue)
    {
        // Scale down
        Vector3 startScale = transform.localScale;
        Vector3 smallScale = originalScale * 0.8f;
        
        float elapsed = 0f;
        
        while (elapsed < updateDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (updateDuration / 2f);
            
            transform.localScale = Vector3.Lerp(startScale, smallScale, t);
            
            yield return null;
        }
        
        // Update value at smallest point
        SetValue(newValue);
        
        // Highlight
        if (nodeMaterial != null)
        {
            Color originalColor = nodeMaterial.color;
            nodeMaterial.color = updateColor;
            yield return new WaitForSeconds(0.1f);
            nodeMaterial.color = originalColor;
        }
        
        // Scale back up
        elapsed = 0f;
        
        while (elapsed < updateDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (updateDuration / 2f);
            
            transform.localScale = Vector3.Lerp(smallScale, originalScale, t);
            
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
}