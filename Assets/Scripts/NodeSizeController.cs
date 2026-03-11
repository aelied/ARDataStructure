using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NodeSizeController : MonoBehaviour
{
    [Header("Size Settings")]
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 2.0f;
    [SerializeField] private float scaleStep = 0.25f;
    [SerializeField] private float animationSpeed = 5f;
    
    [Header("UI References")]
    [SerializeField] private Button increaseButton;
    [SerializeField] private Button decreaseButton;
    [SerializeField] private TextMeshProUGUI sizeDisplayText;
    
    [Header("Target Visualizers (Assign the one you're using)")]
    [SerializeField] private ArrayVisualizer arrayVisualizer;
    [SerializeField] private LinkedListVisualizer linkedListVisualizer;
    [SerializeField] private StackVisualizer stackVisualizer;
    [SerializeField] private QueueManager queueManager;
    [SerializeField] private TreeVisualizer treeVisualizer;
    [SerializeField] private GraphVisualizer graphVisualizer;
    
    private float currentScaleMultiplier = 1f;
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
    private bool buttonsVisible = false;
    
    void Start()
    {
        // Setup button listeners
        if (increaseButton != null)
            increaseButton.onClick.AddListener(IncreaseSize);
        
        if (decreaseButton != null)
            decreaseButton.onClick.AddListener(DecreaseSize);
        
        // Hide buttons initially
        HideButtons();
        UpdateSizeDisplay();
    }
    
    void Update()
    {
        // Check if structure has been placed
        if (!buttonsVisible && IsStructurePlaced())
        {
            ShowButtons();
        }
        
        // Check if structure has been cleared
        if (buttonsVisible && !IsStructurePlaced())
        {
            HideButtons();
        }
        
        // Continuously update button states when visible
        if (buttonsVisible)
        {
            UpdateButtonStates();
        }
    }
    
    bool IsStructurePlaced()
    {
        if (arrayVisualizer != null)
            return arrayVisualizer.IsArrayPlaced();
        
        if (linkedListVisualizer != null)
            return linkedListVisualizer.IsListPlaced();
        
        if (stackVisualizer != null)
            return stackVisualizer.IsStackPlaced();
        
        if (queueManager != null)
            return queueManager.IsQueuePlaced();
        
        if (treeVisualizer != null)
            return treeVisualizer.IsTreePlaced();
        
        if (graphVisualizer != null)
            return graphVisualizer.IsGraphPlaced();
        
        return false;
    }
    
    void HideButtons()
    {
        if (increaseButton != null)
            increaseButton.gameObject.SetActive(false);
        
        if (decreaseButton != null)
            decreaseButton.gameObject.SetActive(false);
        
        if (sizeDisplayText != null)
            sizeDisplayText.gameObject.SetActive(false);
        
        buttonsVisible = false;
    }
    
    void ShowButtons()
    {
        if (increaseButton != null)
            increaseButton.gameObject.SetActive(true);
        
        if (decreaseButton != null)
            decreaseButton.gameObject.SetActive(true);
        
        if (sizeDisplayText != null)
            sizeDisplayText.gameObject.SetActive(true);
        
        buttonsVisible = true;
    }
    
    public void IncreaseSize()
    {
        if (currentScaleMultiplier < maxScale)
        {
            currentScaleMultiplier = Mathf.Min(currentScaleMultiplier + scaleStep, maxScale);
            ApplyScaleToAllNodes();
            UpdateSizeDisplay();
        }
    }
    
    public void DecreaseSize()
    {
        if (currentScaleMultiplier > minScale)
        {
            currentScaleMultiplier = Mathf.Max(currentScaleMultiplier - scaleStep, minScale);
            ApplyScaleToAllNodes();
            UpdateSizeDisplay();
        }
    }
    
    void ApplyScaleToAllNodes()
    {
        // Array nodes
        if (arrayVisualizer != null)
        {
            ScaleArrayNodes();
        }
        
        // Linked List nodes
        if (linkedListVisualizer != null)
        {
            ScaleLinkedListNodes();
        }
        
        // Stack nodes
         if (stackVisualizer != null)
        {
            float targetScale = 0.15f * currentScaleMultiplier;
            stackVisualizer.SetNodeScale(targetScale);
        }
        
        // Queue nodes
        if (queueManager != null)
        {
            ScaleQueueNodes();
        }
        
        // Tree nodes
        if (treeVisualizer != null)
        {
            ScaleTreeNodes();
        }
        
        // Graph nodes
        if (graphVisualizer != null)
        {
            ScaleGraphNodes();
        }
    }
    
    void ScaleArrayNodes()
    {
        // Find all array nodes (they're children of ArrayVisualizer)
        foreach (Transform child in arrayVisualizer.transform)
        {
            if (child.name.Contains("Node") || child.GetComponent<Renderer>() != null)
            {
                ScaleNode(child.gameObject, new Vector3(0.12f, 0.12f, 0.12f));
            }
        }
    }
    
    void ScaleLinkedListNodes()
    {
        // Find all linked list nodes
        foreach (Transform child in linkedListVisualizer.transform)
        {
            if (child.name.Contains("Node"))
            {
                ScaleNode(child.gameObject, Vector3.one * 0.15f);
            }
        }
    }
    
    void ScaleStackNodes()
    {
        // Find all stack items
        foreach (Transform child in stackVisualizer.transform)
        {
            if (child.GetComponent<StackNode>() != null)
            {
                ScaleNode(child.gameObject, Vector3.one * 0.15f);
            }
        }
    }
    
    void ScaleQueueNodes()
    {
        var nodes = queueManager.GetNodes();
        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                if (node != null && node.gameObject != null)
                {
                    ScaleNode(node.gameObject, Vector3.one * 0.15f);
                }
            }
        }
    }
    
    void ScaleTreeNodes()
    {
        // Find all tree nodes
        foreach (Transform child in treeVisualizer.transform)
        {
            if (child.GetComponent<Renderer>() != null && !child.name.Contains("Line"))
            {
                ScaleNode(child.gameObject, Vector3.one * 0.1f);
            }
        }
    }
    
    void ScaleGraphNodes()
    {
        // Find all graph vertices
        foreach (Transform child in graphVisualizer.transform)
        {
            if (child.name.Contains("Vertex"))
            {
                ScaleNode(child.gameObject, Vector3.one * 0.12f);
            }
        }
    }
    
    void ScaleNode(GameObject node, Vector3 baseScale)
    {
        // Store original scale if not already stored
        if (!originalScales.ContainsKey(node))
        {
            originalScales[node] = baseScale;
        }
        
        // Apply scaled size smoothly
        Vector3 targetScale = originalScales[node] * currentScaleMultiplier;
        
        // Smooth interpolation
        if (Application.isPlaying)
        {
            node.transform.localScale = Vector3.Lerp(
                node.transform.localScale,
                targetScale,
                Time.deltaTime * animationSpeed
            );
        }
        else
        {
            node.transform.localScale = targetScale;
        }
    }
    
    void UpdateSizeDisplay()
    {
        if (sizeDisplayText != null)
        {
            int percentage = Mathf.RoundToInt(currentScaleMultiplier * 100f);
            sizeDisplayText.text = $"Size: {percentage}%";
        }
    }
    
    void UpdateButtonStates()
    {
        if (increaseButton != null)
        {
            bool canIncrease = currentScaleMultiplier < maxScale;
            increaseButton.interactable = canIncrease;
            SetButtonColors(increaseButton, canIncrease);
        }
        
        if (decreaseButton != null)
        {
            bool canDecrease = currentScaleMultiplier > minScale;
            decreaseButton.interactable = canDecrease;
            SetButtonColors(decreaseButton, canDecrease);
        }
    }
    
    void SetButtonColors(Button button, bool isEnabled)
    {
        if (button == null) return;
        
        ColorBlock colors = button.colors;
        
        if (isEnabled)
        {
            colors.normalColor = new Color(0.518f, 0.412f, 1f);
            colors.highlightedColor = new Color(0.618f, 0.512f, 1f);
            colors.pressedColor = new Color(0.318f, 0.212f, 0.7f);
            colors.disabledColor = new Color(0.518f, 0.412f, 1f);
        }
        else
        {
            colors.normalColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.disabledColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
        }
        
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        
        // Update text color
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = isEnabled ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.6f);
        }
    }
    
    // Public method to reset scale when clearing structures
    public void ResetScale()
    {
        currentScaleMultiplier = 1f;
        originalScales.Clear();
        UpdateSizeDisplay();
        HideButtons();
        buttonsVisible = false;
    }
}