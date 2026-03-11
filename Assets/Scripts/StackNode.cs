using UnityEngine;
using TMPro;

public class StackNode : MonoBehaviour
{
    [Header("Node Settings")]
    public string nodeValue = "";
    public TextMeshPro valueText;
    
    [Header("Animation Settings")]
    public float appearDuration = 0.5f;
    public float disappearDuration = 0.5f;
    public float moveDuration = 0.3f;
    
    private Vector3 originalScale;
    
    void Start()
    {
        // Auto-find TextMeshPro if not assigned
        if (valueText == null)
        {
            valueText = GetComponentInChildren<TextMeshPro>();
            if (valueText == null)
            {
                Debug.LogWarning("⚠️ No TextMeshPro found in StackNode!");
            }
        }
        
        originalScale = transform.localScale;
    }
    
    // Set the value displayed on this node
    public void SetValue(string value)
    {
        nodeValue = value;
        if (valueText != null)
        {
            valueText.text = value;
        }
    }
    
    // Animate the node appearing (when pushed)
    public void AnimateAppear()
    {
        StartCoroutine(AnimateAppearCoroutine());
    }
    
    // Animate the node disappearing (when popped)
    public void AnimateDisappear()
    {
        StartCoroutine(AnimateDisappearCoroutine());
    }
    
    // Move to a new position (used when nodes shift)
    public void MoveTo(Vector3 newPosition)
    {
        StartCoroutine(MoveToCoroutine(newPosition));
    }
    
    // Coroutine for appear animation
    private System.Collections.IEnumerator AnimateAppearCoroutine()
    {
        // Start from above and small
        Vector3 startPos = transform.position + Vector3.up * 0.3f;
        Vector3 endPos = transform.position;
        
        transform.position = startPos;
        transform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        
        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / appearDuration;
            
            // Ease out cubic
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            
            transform.position = Vector3.Lerp(startPos, endPos, smoothT);
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, smoothT);
            
            yield return null;
        }
        
        transform.position = endPos;
        transform.localScale = originalScale;
    }
    
    // Coroutine for disappear animation
    private System.Collections.IEnumerator AnimateDisappearCoroutine()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * 0.3f;
        Vector3 startScale = transform.localScale;
        
        float elapsed = 0f;
        
        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / disappearDuration;
            
            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
    
    // Coroutine for moving to a new position
    private System.Collections.IEnumerator MoveToCoroutine(Vector3 newPosition)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            
            // Ease out
            float smoothT = 1f - Mathf.Pow(1f - t, 2f);
            
            transform.position = Vector3.Lerp(startPos, newPosition, smoothT);
            
            yield return null;
        }
        
        transform.position = newPosition;
    }
}