using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QRQueueModeUI : MonoBehaviour
{
    [Header("References")]
    public QRQueueModeManager queueManager;
    
    [Header("Mode Selection")]
    public GameObject modeSelectionPanel;
    public Button physicalModeButton;
    public Button virtualModeButton;
    
    [Header("Main Buttons")]
    public Button enqueueButton;
    public Button dequeueButton;
    public Button peekButton;
    public Button resetButton;
    
    [Header("UI Panels")]
    public GameObject headerPanel;
    public GameObject instructionCard;
    public GameObject buttonPanel;
    public GameObject explanationPanel;
    
    [Header("Info Display")]
    public TextMeshProUGUI explanationText;
    public TextMeshProUGUI tutorialHintText;
    public TextMeshProUGUI statusText;
    
    [Header("Analysis Toggle")]
    public AnalysisToggleButton analysisToggle;
    
    private bool operationsVisible = false;
    
    void Start()
    {
        if (physicalModeButton != null)
        {
            physicalModeButton.onClick.AddListener(OnPhysicalModeSelected);
        }
        
        if (virtualModeButton != null)
        {
            virtualModeButton.onClick.AddListener(OnVirtualModeSelected);
        }
        
        if (enqueueButton != null)
        {
            enqueueButton.onClick.AddListener(OnEnqueueClicked);
            enqueueButton.interactable = false;
        }
        
        if (dequeueButton != null)
        {
            dequeueButton.onClick.AddListener(OnDequeueClicked);
            dequeueButton.interactable = false;
        }
        
        if (peekButton != null)
        {
            peekButton.onClick.AddListener(OnPeekClicked);
            peekButton.interactable = false;
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
            resetButton.interactable = true;
            SetButtonColor(resetButton, true);
        }
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "Choose a mode to start building your queue!";
        }
        
        if (statusText != null)
        {
            statusText.text = "Queue Size: 0";
        }
        
        ShowModeSelection();
    }
    
    void ShowModeSelection()
    {
        if (modeSelectionPanel != null)
            modeSelectionPanel.SetActive(true);
        
        if (headerPanel != null)
            headerPanel.SetActive(false);
        
        if (instructionCard != null)
            instructionCard.SetActive(false);
        
        if (buttonPanel != null)
            buttonPanel.SetActive(false);
        
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        if (analysisToggle != null)
        {
            analysisToggle.ForceHidePanel();
        }
        
        operationsVisible = false;
    }
    
    void OnPhysicalModeSelected()
    {
        if (queueManager == null) return;
        
        queueManager.SetPhysicalMode();
        HideModeSelection();
        ShowSetupInstructions();
        
        if (explanationText != null)
        {
            explanationText.text = "PHYSICAL MODE - QUEUE Selected!\n\n" +
                "Queue follows FIFO (First In, First Out) principle:\n" +
                "- ENQUEUE: Add to REAR\n" +
                "- DEQUEUE: Remove from FRONT\n" +
                "- PEEK: View FRONT without removing\n\n" +
                "In Physical Mode:\n" +
                "- Scan QR codes and place them physically\n" +
                "- FRONT (green marker) = First to leave\n" +
                "- REAR (orange marker) = Last added";
        }
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "TIP: Queue = Line at a store! First in line = First served!";
        }
        
        Debug.Log("User selected PHYSICAL mode for Queue");
    }
    
    void OnVirtualModeSelected()
    {
        if (queueManager == null) return;
        
        queueManager.SetVirtualMode();
        HideModeSelection();
        ShowSetupInstructions();
        
        if (explanationText != null)
        {
            explanationText.text = "VIRTUAL MODE - QUEUE Selected!\n\n" +
                "Queue follows FIFO (First In, First Out) principle:\n" +
                "- ENQUEUE: Add to REAR\n" +
                "- DEQUEUE: Remove from FRONT\n" +
                "- PEEK: View FRONT without removing\n\n" +
                "In Virtual Mode:\n" +
                "- Scan ONE QR to define all nodes\n" +
                "- All operations are automatic!\n" +
                "- FRONT (green) and REAR (orange) markers guide you";
        }
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "TIP: Just scan ONE QR - all queue nodes will use that object!";
        }
        
        Debug.Log("User selected VIRTUAL mode for Queue");
    }
    
    void HideModeSelection()
    {
        if (modeSelectionPanel != null)
            modeSelectionPanel.SetActive(false);
    }
    
    void ShowSetupInstructions()
    {
        if (headerPanel != null)
            headerPanel.SetActive(true);
        
        if (instructionCard != null)
            instructionCard.SetActive(true);
        
        if (explanationPanel != null)
            explanationPanel.SetActive(true);
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
        
        string modeInfo = "";
        if (queueManager != null)
        {
            if (queueManager.GetCurrentMode() == QRQueueModeManager.InteractionMode.Physical)
            {
                modeInfo = "Physical Mode: Place objects physically\n\n";
            }
            else
            {
                string objType = queueManager.GetVirtualObjectType().ToUpper();
                modeInfo = $"Virtual Mode: Using {objType} nodes\n" +
                          "All operations are automatic!\n\n";
            }
        }
        
        UpdateExplanation(modeInfo +
            "Queue initialized! FIFO Operations:\n\n" +
            "ENQUEUE: Add to REAR (back of line)\n" +
            "DEQUEUE: Remove from FRONT (front of line)\n" +
            "PEEK: View FRONT element\n\n" +
            "Green marker = FRONT | Orange marker = REAR");
            
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "Remember: FIFO = First In, First Out!";
        }
    }
    
    void OnEnqueueClicked()
    {
        if (queueManager == null) return;
        
        queueManager.SimulateEnqueue();
        
        bool isVirtualMode = queueManager.GetCurrentMode() == QRQueueModeManager.InteractionMode.Virtual;
        
        if (isVirtualMode)
        {
            UpdateExplanation($"ENQUEUE Operation:\n" +
                $"Tap anywhere to add {queueManager.GetVirtualObjectType().ToUpper()} to REAR!\n\n" +
                "O(1) Time Complexity - Instant!");
        }
        else
        {
            UpdateExplanation("ENQUEUE Operation:\n" +
                "1. Scan QR code near REAR (orange marker)\n" +
                "2. Tap QR to confirm\n\n" +
                "Element will be added to REAR of queue");
        }
    }
    
    void OnDequeueClicked()
    {
        if (queueManager == null) return;
        
        queueManager.SimulateDequeue();
        
        UpdateExplanation("DEQUEUE Operation:\n" +
            "Removed element from FRONT!\n\n" +
            "FIFO: First In, First Out\n" +
            "O(1) Time Complexity - Instant!");
    }
    
    void OnPeekClicked()
    {
        if (queueManager == null) return;
        
        queueManager.SimulatePeek();
        
        UpdateExplanation("PEEK Operation:\n" +
            "Viewing FRONT element (cyan highlight)\n\n" +
            "Non-destructive: Queue unchanged\n" +
            "O(1) Time Complexity - Instant!");
    }
    
    void OnResetClicked()
    {
        if (queueManager == null) return;
        
        queueManager.ResetQueue();
        operationsVisible = false;

        if (analysisToggle != null)
        {
            analysisToggle.ForceHidePanel();
        }
        
        ShowModeSelection();
        
        UpdateExplanation("");
        
        if (tutorialHintText != null)
            tutorialHintText.text = "Choose a mode to start building your queue!";
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
        bool canAdd = size < 8;
        
        // ENQUEUE: Enabled if not full
        if (enqueueButton != null)
        {
            bool enqueueEnabled = canAdd;
            enqueueButton.interactable = enqueueEnabled;
            SetButtonColor(enqueueButton, enqueueEnabled);
        }
        
        // DEQUEUE: Enabled if has elements
        if (dequeueButton != null)
        {
            bool dequeueEnabled = hasElements;
            dequeueButton.interactable = dequeueEnabled;
            SetButtonColor(dequeueButton, dequeueEnabled);
        }
        
        // PEEK: Enabled if has elements
        if (peekButton != null)
        {
            bool peekEnabled = hasElements;
            peekButton.interactable = peekEnabled;
            SetButtonColor(peekButton, peekEnabled);
        }
        
        // RESET: Always enabled
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
            colors.normalColor = new Color(1f, 0.549f, 0f);
            colors.highlightedColor = new Color(1f, 0.65f, 0.1f);
            colors.pressedColor = new Color(0.8f, 0.44f, 0f);
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