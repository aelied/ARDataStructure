using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PanelAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float fadeDuration = 0.3f;
    public float slideDuration = 0.3f;
    public AnimationType animationType = AnimationType.Fade;
    
    [Header("Slide Settings")]
    public SlideDirection slideDirection = SlideDirection.FromRight;
    public float slideDistance = 500f;
    
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    
    public enum AnimationType
    {
        Fade,
        Slide,
        FadeAndSlide,
        Scale
    }
    
    public enum SlideDirection
    {
        FromRight,
        FromLeft,
        FromTop,
        FromBottom
    }
    
    void Awake()
    {
        // Add CanvasGroup if doesn't exist
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }
    
    void OnEnable()
    {
        // Animate when panel is enabled
        StopAllCoroutines();
        StartCoroutine(AnimateIn());
    }
    
    public void AnimateOut(System.Action onComplete = null)
    {
        StartCoroutine(AnimateOutCoroutine(onComplete));
    }
    
    IEnumerator AnimateIn()
    {
        switch (animationType)
        {
            case AnimationType.Fade:
                yield return StartCoroutine(FadeIn());
                break;
            case AnimationType.Slide:
                yield return StartCoroutine(SlideIn());
                break;
            case AnimationType.FadeAndSlide:
                StartCoroutine(FadeIn());
                yield return StartCoroutine(SlideIn());
                break;
            case AnimationType.Scale:
                yield return StartCoroutine(ScaleIn());
                break;
        }
    }
    
    IEnumerator AnimateOutCoroutine(System.Action onComplete)
    {
        switch (animationType)
        {
            case AnimationType.Fade:
                yield return StartCoroutine(FadeOut());
                break;
            case AnimationType.Slide:
                yield return StartCoroutine(SlideOut());
                break;
            case AnimationType.FadeAndSlide:
                StartCoroutine(FadeOut());
                yield return StartCoroutine(SlideOut());
                break;
            case AnimationType.Scale:
                yield return StartCoroutine(ScaleOut());
                break;
        }
        
        onComplete?.Invoke();
    }
    
    IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    IEnumerator FadeOut()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
    
    IEnumerator SlideIn()
    {
        Vector2 startPos = GetSlideStartPosition();
        rectTransform.anchoredPosition = startPos;
        
        float elapsed = 0f;
        
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            // Ease out curve
            t = 1f - Mathf.Pow(1f - t, 3f);
            
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, originalPosition, t);
            yield return null;
        }
        
        rectTransform.anchoredPosition = originalPosition;
    }
    
    IEnumerator SlideOut()
    {
        Vector2 endPos = GetSlideStartPosition();
        Vector2 startPos = rectTransform.anchoredPosition;
        
        float elapsed = 0f;
        
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        rectTransform.anchoredPosition = endPos;
    }
    
    IEnumerator ScaleIn()
    {
        transform.localScale = Vector3.zero;
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            // Ease out back
            t = 1f + (--t) * t * t * (1f + 1.70158f);
            
            transform.localScale = Vector3.one * t;
            yield return null;
        }
        
        transform.localScale = Vector3.one;
    }
    
    IEnumerator ScaleOut()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / fadeDuration);
            
            transform.localScale = Vector3.one * t;
            yield return null;
        }
        
        transform.localScale = Vector3.zero;
    }
    
    Vector2 GetSlideStartPosition()
    {
        switch (slideDirection)
        {
            case SlideDirection.FromRight:
                return originalPosition + Vector2.right * slideDistance;
            case SlideDirection.FromLeft:
                return originalPosition + Vector2.left * slideDistance;
            case SlideDirection.FromTop:
                return originalPosition + Vector2.up * slideDistance;
            case SlideDirection.FromBottom:
                return originalPosition + Vector2.down * slideDistance;
            default:
                return originalPosition;
        }
    }
}