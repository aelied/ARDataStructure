using UnityEngine;
using TMPro;

public class QueueNode : MonoBehaviour
{
    [Header("Node Settings")]
    public string nodeValue;
    
    [Header("References")]
    public TextMeshPro valueText;
    public GameObject labelBackground;
    
    [Header("Animation Settings")]
    public float moveSpeed = 2f;
    public float scaleSpeed = 3f;
    
    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        targetPosition = transform.position;
        
        // If valueText is assigned, update it
        if (valueText != null)
        {
            UpdateDisplay();
        }
    }

    void Update()
    {
        // Smooth movement animation
        if (isMoving)
        {
            transform.position = Vector3.Lerp(
                transform.position, 
                targetPosition, 
                Time.deltaTime * moveSpeed
            );
            
            // Stop moving when close enough
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
        }
    }

    // Set the value displayed on this node
    public void SetValue(string value)
    {
        nodeValue = value;
        UpdateDisplay();
    }

    // Update the text display
    void UpdateDisplay()
    {
        if (valueText != null)
        {
            valueText.text = nodeValue;
        }
    }

    // Move node to a new position with animation
    public void MoveTo(Vector3 newPosition)
    {
        targetPosition = newPosition;
        isMoving = true;
    }

    // Instantly set position without animation
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
        targetPosition = position;
        isMoving = false;
    }

    // Animation for when node is added (scale up)
    public void AnimateAppear()
    {
        StartCoroutine(ScaleAnimation(Vector3.zero, Vector3.one));
    }

    // Animation for when node is removed (scale down)
    public void AnimateDisappear()
    {
        StartCoroutine(ScaleAnimation(Vector3.one, Vector3.zero));
    }

    // Scale animation coroutine
    System.Collections.IEnumerator ScaleAnimation(Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        float duration = 1f / scaleSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        transform.localScale = to;

        // If scaling to zero, destroy the node
        if (to == Vector3.zero)
        {
            Destroy(gameObject);
        }
    }
}