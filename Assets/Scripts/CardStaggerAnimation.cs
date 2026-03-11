using UnityEngine;
using System.Collections;

public class CardStaggerAnimation : MonoBehaviour
{
    [Header("Cards to Animate")]
    public RectTransform[] cards;
    
    [Header("Animation Settings")]
    public float delayBetweenCards = 0.1f;
    public float slideDuration = 0.4f;
    public float slideDistance = 300f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    void OnEnable()
    {
        StartCoroutine(AnimateCards());
    }
    
    IEnumerator AnimateCards()
    {
        // Setup: move all cards to starting position
        foreach (RectTransform card in cards)
        {
            if (card != null)
            {
                CanvasGroup cg = card.GetComponent<CanvasGroup>();
                if (cg == null) cg = card.gameObject.AddComponent<CanvasGroup>();
                
                cg.alpha = 0f;
                Vector2 originalPos = card.anchoredPosition;
                card.anchoredPosition = new Vector2(originalPos.x + slideDistance, originalPos.y);
            }
        }
        
        // Animate each card with delay
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] != null)
            {
                StartCoroutine(AnimateCard(cards[i]));
                yield return new WaitForSeconds(delayBetweenCards);
            }
        }
    }
    
    IEnumerator AnimateCard(RectTransform card)
    {
        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        Vector2 endPos = card.anchoredPosition - new Vector2(slideDistance, 0);
        Vector2 startPos = card.anchoredPosition;
        
        float elapsed = 0f;
        
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            float curveT = easeCurve.Evaluate(t);
            
            card.anchoredPosition = Vector2.Lerp(startPos, endPos, curveT);
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            
            yield return null;
        }
        
        card.anchoredPosition = endPos;
        cg.alpha = 1f;
    }
}