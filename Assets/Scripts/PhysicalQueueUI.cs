using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhysicalQueueUI : MonoBehaviour
{
    [Header("References")]
    public PhysicalQueueManager queueManager;
    
    [Header("Buttons")]
    public Button enqueueButton;
    public Button dequeueButton;
    public Button peekButton;
    public Button resetButton;
    
    [Header("UI Panels")]
    public GameObject headerPanel;
    public GameObject instructionCard;
    public GameObject buttonPanel;
    public GameObject explanationPanel;
    public GameObject helpPanel;
    
    [Header("Info Display")]
    public TextMeshProUGUI explanationText;
    public TextMeshProUGUI helpText;
    
    private bool operationsVisible = false;
    
    void Start()
    {
        if (enqueueButton != null)
            enqueueButton.onClick.AddListener(OnEnqueueClicked);
        
        if (dequeueButton != null)
            dequeueButton.onClick.AddListener(OnDequeueClicked);
        
        if (peekButton != null)
            peekButton.onClick.AddListener(OnPeekClicked);
        
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);
        
        if (headerPanel != null)
            headerPanel.SetActive(true);
        
        if (instructionCard != null)
            instructionCard.SetActive(true);
        
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        if (helpPanel != null)
            helpPanel.SetActive(false);
        
        SetupHelpText();
        HideButtons();
    }
    
    void SetupHelpText()
    {
        if (helpText != null)
        {
            helpText.text = "📚 QUEUE RULES:\n\n" +
                          "• Enqueue: Add to BACK (right)\n" +
                          "• Dequeue: Remove from FRONT (left)\n" +
                          "• FIFO: First In, First Out\n\n" +
                          "Place physical coins and tap them!";
        }
    }
    
    void Update()
    {
        if (queueManager != null && queueManager.IsQueueReady() && !operationsVisible)
        {
            ShowOperationsPanel();
        }
        
        if (operationsVisible)
        {
            UpdateButtonStates();
        }
    }
    
    void ShowOperationsPanel()
    {
        operationsVisible = true;
        
        if (headerPanel != null)
            headerPanel.SetActive(false);
        
        if (instructionCard != null)
            instructionCard.SetActive(false);
        
        if (buttonPanel != null)
            buttonPanel.SetActive(true);
        
        if (explanationPanel != null)
            explanationPanel.SetActive(true);
        
        if (helpPanel != null)
            helpPanel.SetActive(true);
        
        UpdateExplanation("✅ Physical Queue Ready! Use the buttons below.");
    }
    
    void HideButtons()
    {
        if (buttonPanel != null)
            buttonPanel.SetActive(false);
        
        operationsVisible = false;
    }
    
    void OnEnqueueClicked()
    {
        if (queueManager == null) return;
        
        queueManager.SimulateEnqueue();
        UpdateExplanation("➕ ENQUEUE: Place coin at GREEN spot (BACK), then tap it");
    }
    
    void OnDequeueClicked()
    {
        if (queueManager == null) return;
        
        queueManager.SimulateDequeue();
        UpdateExplanation("➖ DEQUEUE: Remove coin at RED spot (FRONT)");
    }
    
    void OnPeekClicked()
    {
        if (queueManager == null) return;
        
        UpdateExplanation("👁️ PEEK: The FRONT coin is the leftmost one");
    }
    
    void OnResetClicked()
    {
        if (queueManager == null) return;
        
        queueManager.ResetQueue();
        operationsVisible = false;
        
        if (headerPanel != null)
            headerPanel.SetActive(true);
        
        if (instructionCard != null)
            instructionCard.SetActive(true);
        
        if (buttonPanel != null)
            buttonPanel.SetActive(false);
        
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        if (helpPanel != null)
            helpPanel.SetActive(false);
        
        UpdateExplanation("");
    }
    
    void UpdateExplanation(string message)
    {
        if (explanationText != null)
        {
            explanationText.text = message;
        }
    }
    
    void UpdateButtonStates()
    {
        if (queueManager == null) return;
        
        int size = queueManager.GetQueueSize();
        bool hasElements = size > 0;
        bool canAdd = size < 6; // maxObjects
        
        if (enqueueButton != null)
        {
            enqueueButton.interactable = canAdd;
            SetButtonColor(enqueueButton, canAdd);
        }
        
        if (dequeueButton != null)
        {
            dequeueButton.interactable = hasElements;
            SetButtonColor(dequeueButton, hasElements);
        }
        
        if (peekButton != null)
        {
            peekButton.interactable = hasElements;
            SetButtonColor(peekButton, hasElements);
        }
        
        if (resetButton != null)
        {
            resetButton.interactable = true;
            SetButtonColor(resetButton, true);
        }
    }
    
    void SetButtonColor(Button button, bool isEnabled)
    {
        if (button == null) return;
        
        ColorBlock colors = button.colors;
        
        if (isEnabled)
        {
            colors.normalColor = new Color(0.518f, 0.412f, 1f);
            colors.highlightedColor = new Color(0.618f, 0.512f, 1f);
            colors.pressedColor = new Color(0.318f, 0.212f, 0.7f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
        else
        {
            colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            colors.highlightedColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            colors.pressedColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        }
        
        button.colors = colors;
        
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = isEnabled ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        }
    }
}