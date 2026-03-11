using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PuzzleItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Transform container;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        canvas = GetComponentInParent<Canvas>();
    }

    public void SetContainer(Transform containerTransform)
    {
        container = containerTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        
        // Make semi-transparent while dragging
        canvasGroup.alpha = 0.6f;
        
        // Don't block raycasts so we can detect other items
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        // Check if we dropped on another puzzle item
        GameObject droppedOn = eventData.pointerCurrentRaycast.gameObject;
        
        if (droppedOn != null && droppedOn != gameObject)
        {
            PuzzleItemDragHandler otherItem = droppedOn.GetComponent<PuzzleItemDragHandler>();
            
            // If not directly on another item, check parent
            if (otherItem == null && droppedOn.transform.parent != null)
            {
                otherItem = droppedOn.transform.parent.GetComponent<PuzzleItemDragHandler>();
            }
            
            if (otherItem != null && otherItem.transform.parent == container)
            {
                // Swap positions in hierarchy
                int myIndex = transform.GetSiblingIndex();
                int otherIndex = otherItem.transform.GetSiblingIndex();
                
                transform.SetSiblingIndex(otherIndex);
                otherItem.transform.SetSiblingIndex(myIndex);
            }
        }
        
        // IMPORTANT: Force return to parent and let layout group reposition
        transform.SetParent(container, false);
        rectTransform.anchoredPosition = Vector2.zero;
        
        // Force layout rebuild
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(container as RectTransform);
    }
}