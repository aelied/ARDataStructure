using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QueueUI : MonoBehaviour
{
    [Header("References")]
    public QueueManager queueManager;
    
    [Header("Buttons")]
    public Button enqueueButton;
    public Button dequeueButton;
    public Button peekButton;
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
    public int initialNodeCount = 0;
    
    private int valueCounter = 0;
    private string[] testValues = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
    private bool buttonsVisible = false;
    private bool hasAutoPopulated = false;
    
    void Start()
    {
        if (enqueueButton != null)
            enqueueButton.onClick.AddListener(OnEnqueueClicked);
        
        if (dequeueButton != null)
            dequeueButton.onClick.AddListener(OnDequeueClicked);
        
        if (peekButton != null)
            peekButton.onClick.AddListener(OnPeekClicked);
        
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
            helpText.text = "Enqueue adds to the BACK. Dequeue removes from the FRONT. FIFO = First In, First Out!";
        }
    }
    
    void Update()
    {
        if (queueManager != null && queueManager.IsQueuePlaced() && !buttonsVisible)
        {
            buttonsVisible = true;
            
            if (autoPopulateOnPlacement && !hasAutoPopulated && initialNodeCount > 0)
            {
                hasAutoPopulated = true;
                StartCoroutine(AutoPopulateQueue());
            }
            else
            {
                ShowButtons();
                UpdateExplanation(" Queue placed! Tap Enqueue to add nodes.");
            }
        }
        
        if (buttonsVisible)
        {
            UpdateInfoText();
            UpdateButtonStates();
        }
    }
    
    System.Collections.IEnumerator AutoPopulateQueue()
    {
        yield return new WaitForSeconds(0.3f);
        
        for (int i = 0; i < initialNodeCount && i < queueManager.maxQueueSize; i++)
        {
            string value = testValues[valueCounter % testValues.Length];
            valueCounter++;
            queueManager.Enqueue(value);
            yield return new WaitForSeconds(0.2f);
        }
        
        ShowButtons();
        UpdateInfoText();
        UpdateExplanation($" Queue initialized!");
    }
    
    void HideButtons()
    {
        if (buttonPanel != null)
            buttonPanel.SetActive(false);
        else
        {
            if (enqueueButton != null) enqueueButton.gameObject.SetActive(false);
            if (dequeueButton != null) dequeueButton.gameObject.SetActive(false);
            if (peekButton != null) peekButton.gameObject.SetActive(false);
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
            if (enqueueButton != null) enqueueButton.gameObject.SetActive(true);
            if (dequeueButton != null) dequeueButton.gameObject.SetActive(true);
            if (peekButton != null) peekButton.gameObject.SetActive(true);
            if (clearButton != null) clearButton.gameObject.SetActive(true);
        }
        
        buttonsVisible = true;
    }
    
    void OnEnqueueClicked()
    {
        if (queueManager == null) return;
        
        string value = testValues[valueCounter % testValues.Length];
        valueCounter++;
        
        queueManager.Enqueue(value);
        UpdateInfoText();
        UpdateExplanation($" Added '{value}' to the BACK of the queue");
    }
    
    void OnDequeueClicked()
    {
        if (queueManager == null) return;
        
        string value = queueManager.Peek();
        queueManager.Dequeue();
        UpdateInfoText();
        
        if (value != "Empty")
            UpdateExplanation($" Dequeued '{value}' from the FRONT");
        else
            UpdateExplanation(" Queue is empty!");
    }
    
    void OnPeekClicked()
    {
        if (queueManager == null) return;
        
        string frontValue = queueManager.Peek();
        UpdateInfoText();
        UpdateExplanation($"👁️ Front element: '{frontValue}'");
    }
    
    void OnClearClicked()
    {
        if (queueManager == null) return;
        
        queueManager.ResetQueue();
        valueCounter = 0;
        hasAutoPopulated = false;
        buttonsVisible = false;
        
        //  FIXED: Hide help panel when clearing
        if (helpPanel != null)
            helpPanel.SetActive(false);
        
        HideButtons();
        
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        if (infoText != null)
        {
            infoText.text = "Queue Size: 0";
            infoText.gameObject.SetActive(false);
        }
        
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
            int size = queueManager != null ? queueManager.Size() : 0;
            infoText.text = $"Queue Size: {size}";
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
    
    void UpdateButtonStates()
    {
        if (queueManager == null) return;
        
        int size = queueManager.Size();
        bool hasElements = size > 0;
        bool canAdd = size < queueManager.maxQueueSize;
        
        if (enqueueButton != null)
        {
            enqueueButton.interactable = canAdd;
            SetButtonColors(enqueueButton, canAdd);
        }
        
        if (dequeueButton != null)
        {
            dequeueButton.interactable = hasElements;
            SetButtonColors(dequeueButton, hasElements);
        }
        
        if (peekButton != null)
        {
            peekButton.interactable = hasElements;
            SetButtonColors(peekButton, hasElements);
        }
        
        if (clearButton != null)
        {
            clearButton.interactable = true;
            SetButtonColors(clearButton, true);
        }
    }
}