using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Button))]
public class ButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Animation")]
    public bool enableScaleAnimation = true;
    public float pressedScale = 0.95f;
    public float animationSpeed = 10f;
    
    [Header("Color Animation")]
    public bool enableColorAnimation = false;
    public Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    
    private Vector3 originalScale;
    private Vector3 targetScale;
    private Image buttonImage;
    private Color originalColor;
    private bool isPressed = false;
    
    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        
        buttonImage = GetComponent<Image>();
        if (buttonImage != null)
        {
            originalColor = buttonImage.color;
        }
    }
    
    void Update()
    {
        // Smooth scale animation
        if (enableScaleAnimation)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        
        if (enableScaleAnimation)
        {
            targetScale = originalScale * pressedScale;
        }
        
        if (enableColorAnimation && buttonImage != null)
        {
            buttonImage.color = pressedColor;
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        
        if (enableScaleAnimation)
        {
            targetScale = originalScale;
        }
        
        if (enableColorAnimation && buttonImage != null)
        {
            buttonImage.color = originalColor;
        }
    }
}