// ========================================
// STACK UI - Updated
// ========================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StackUI : MonoBehaviour
{
    [Header("References")]
    public StackVisualizer stackVisualizer;
    
    [Header("Buttons")]
    public Button pushButton;
    public Button popButton;
    public Button clearButton;
    
    [Header("UI Panels")]
    public GameObject headerPanel;
    public GameObject instructionCard;
    public GameObject dottedScanBox;
    public GameObject buttonPanel;
    public GameObject explanationPanel;
    public GameObject helpPanel;  //  NEW: Add this for help instructions
    
    [Header("Info Display")]
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI explanationText;
    public TextMeshProUGUI helpText;  //  NEW: Text inside help panel
    
    [Header("Auto-Populate Settings")]
    public bool autoPopulateOnPlacement = false;
    public int initialItemCount = 0;
    
    private bool buttonsVisible = false;
    private bool hasAutoPopulated = false;
    
    void Start()
    {
        if (pushButton != null)
            pushButton.onClick.AddListener(OnPushClicked);
        
        if (popButton != null)
            popButton.onClick.AddListener(OnPopClicked);
        
        if (clearButton != null)
            clearButton.onClick.AddListener(OnClearClicked);
        
        if (headerPanel != null)
            headerPanel.SetActive(true);
        
        if (instructionCard != null)
            instructionCard.SetActive(true);
        
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        if (infoText != null)
            infoText.gameObject.SetActive(false);
        
        //  FIXED: Hide help panel initially
        if (helpPanel != null)
            helpPanel.SetActive(false);
        
        SetupHelpText();
        HideButtons();
    }
    
    void SetupHelpText()
    {
        if (helpText != null)
        {
            helpText.text = "Push adds items to the TOP of the stack. Pop removes from the TOP. LIFO = Last In, First Out!";
        }
    }
    
    void Update()
    {
        if (stackVisualizer != null && stackVisualizer.IsStackPlaced() && !buttonsVisible)
        {
            buttonsVisible = true;
            
            if (autoPopulateOnPlacement && !hasAutoPopulated && initialItemCount > 0)
            {
                hasAutoPopulated = true;
                StartCoroutine(AutoPopulateStack());
            }
            else
            {
                ShowButtons();
                UpdateExplanation(" Stack placed! Tap Push to add items.");
            }
        }
        
        if (buttonsVisible)
        {
            UpdateInfoText();
            UpdateButtonStates();
        }
    }
    
    System.Collections.IEnumerator AutoPopulateStack()
    {
        yield return new WaitForSeconds(0.3f);
        
        for (int i = 0; i < initialItemCount && i < stackVisualizer.maxStackSize; i++)
        {
            stackVisualizer.Push();
            yield return new WaitForSeconds(0.3f);
        }
        
        ShowButtons();
        UpdateInfoText();
        UpdateExplanation($" Stack initialized!");
    }
    
    void HideButtons()
    {
        if (buttonPanel != null)
            buttonPanel.SetActive(false);
        else
        {
            if (pushButton != null) pushButton.gameObject.SetActive(false);
            if (popButton != null) popButton.gameObject.SetActive(false);
            if (clearButton != null) clearButton.gameObject.SetActive(false);
        }
        
        buttonsVisible = false;
    }
    
    void ShowButtons()
    {
        if (headerPanel != null)
            headerPanel.SetActive(false);
        
        if (instructionCard != null)
            instructionCard.SetActive(false);
        
        if (explanationPanel != null)
            explanationPanel.SetActive(true);
        
        if (infoText != null)
            infoText.gameObject.SetActive(true);
        
        //  FIXED: Show help panel after placement
        if (helpPanel != null)
            helpPanel.SetActive(true);
        
        if (buttonPanel != null)
            buttonPanel.SetActive(true);
        else
        {
            if (pushButton != null) pushButton.gameObject.SetActive(true);
            if (popButton != null) popButton.gameObject.SetActive(true);
            if (clearButton != null) clearButton.gameObject.SetActive(true);
        }
        
        buttonsVisible = true;
        UpdateButtonStates();
    }
    
    void UpdateButtonStates()
    {
        if (stackVisualizer == null) return;
        
        int size = GetStackSize();
        int maxSize = stackVisualizer.maxStackSize;
        bool hasElements = size > 0;
        bool canAdd = size < maxSize;
        
        if (pushButton != null)
        {
            pushButton.interactable = canAdd;
            SetButtonColors(pushButton, canAdd);
        }
        
        if (popButton != null)
        {
            popButton.interactable = hasElements;
            SetButtonColors(popButton, hasElements);
        }
        
        if (clearButton != null)
        {
            clearButton.interactable = true;
            SetButtonColors(clearButton, true);
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
            colors.selectedColor = new Color(0.418f, 0.312f, 0.9f);
            colors.disabledColor = new Color(0.518f, 0.412f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
        }
        else
        {
            colors.normalColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.selectedColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.disabledColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
        }
        
        button.colors = colors;
        
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = isEnabled ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.6f);
        }
        
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color imgColor = buttonImage.color;
            imgColor.a = isEnabled ? 1f : 0.5f;
            buttonImage.color = imgColor;
        }
    }
    
    void OnPushClicked()
    {
        if (stackVisualizer == null) return;
        
        int sizeBefore = GetStackSize();
        int maxSize = stackVisualizer.maxStackSize;
        
        if (sizeBefore >= maxSize)
        {
            UpdateExplanation($" Stack is full! Maximum size is {maxSize}.");
            return;
        }
        
        stackVisualizer.Push();
        StartCoroutine(UpdateAfterPush(sizeBefore));
    }
    
    System.Collections.IEnumerator UpdateAfterPush(int sizeBefore)
    {
        yield return new WaitForEndOfFrame();
        
        int sizeAfter = GetStackSize();
        UpdateInfoText();
        
        if (sizeAfter > sizeBefore)
            UpdateExplanation($" Pushed item #{sizeAfter} onto the TOP of the stack\n LIFO: Last In, First Out!");
        else
            UpdateExplanation(" Could not push item!");
    }
    
    void OnPopClicked()
    {
        if (stackVisualizer == null) return;
        
        int sizeBefore = GetStackSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation(" Stack is empty! Nothing to pop.");
            return;
        }
        
        stackVisualizer.Pop();
        StartCoroutine(UpdateAfterPop(sizeBefore));
    }
    
    System.Collections.IEnumerator UpdateAfterPop(int sizeBefore)
    {
        yield return new WaitForEndOfFrame();
        
        int sizeAfter = GetStackSize();
        UpdateInfoText();
        
        if (sizeAfter < sizeBefore)
            UpdateExplanation($" Popped item from the TOP of the stack\n Most recent item removed!");
        else
            UpdateExplanation(" Could not pop item!");
    }
    
    void OnClearClicked()
    {
        if (stackVisualizer == null) return;
        
        stackVisualizer.Clear();
        hasAutoPopulated = false;
        buttonsVisible = false;
        
        //  FIXED: Hide help panel when clearing
        if (helpPanel != null)
            helpPanel.SetActive(false);
        
        HideButtons();
        
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        if (infoText != null)
            infoText.gameObject.SetActive(false);
        
        if (headerPanel != null)
            headerPanel.SetActive(true);
        
        if (instructionCard != null)
            instructionCard.SetActive(true);
    }
    
    void UpdateInfoText(string message = "")
    {
        if (infoText == null) return;
        
        if (string.IsNullOrEmpty(message))
        {
            int size = GetStackSize();
            int maxSize = stackVisualizer != null ? stackVisualizer.maxStackSize : 10;
            infoText.text = $"Stack Size: {size}/{maxSize}";
        }
        else
        {
            infoText.text = message;
        }
    }
    
    void UpdateExplanation(string message)
    {
        if (explanationText != null)
        {
            explanationText.text = message;
        }
    }
    
    int GetStackSize()
    {
        if (stackVisualizer == null) return 0;
        return stackVisualizer.Size();
    }
}
