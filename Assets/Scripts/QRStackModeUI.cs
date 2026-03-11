using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QRStackModeUI : MonoBehaviour
{
    [Header("References")]
    public QRStackModeManager stackManager;
    
    [Header("Mode Selection")]
    public GameObject modeSelectionPanel;
    public Button physicalModeButton;
    public Button virtualModeButton;
    
    [Header("Main Buttons")]
    public Button pushButton;
    public Button popButton;
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
        
        if (pushButton != null)
        {
            pushButton.onClick.AddListener(OnPushClicked);
            pushButton.interactable = false;
        }
        
        if (popButton != null)
        {
            popButton.onClick.AddListener(OnPopClicked);
            popButton.interactable = false;
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
            tutorialHintText.text = "Choose a mode to start building your stack!";
        }
        
        if (statusText != null)
        {
            statusText.text = "Stack Size: 0";
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
        if (stackManager == null) return;
        
        stackManager.SetPhysicalMode();
        HideModeSelection();
        ShowSetupInstructions();
        
        if (explanationText != null)
        {
            explanationText.text = "PHYSICAL MODE Selected!\n\n" +
                "In this mode, you will:\n" +
                "- Scan QR codes and place them BESIDE each other\n" +
                "- Place first QR on the ground as base\n" +
                "- Add more QRs to the RIGHT of the previous one\n" +
                "- Stack grows HORIZONTALLY like a line\n\n" +
                "This mode teaches LIFO (Last In, First Out) with hands-on interaction!";
        }
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "TIP: Stack grows HORIZONTALLY - always add to the RIGHT!";
        }
        
        Debug.Log("User selected PHYSICAL mode for Stack");
    }
    
    void OnVirtualModeSelected()
    {
        if (stackManager == null) return;
        
        stackManager.SetVirtualMode();
        HideModeSelection();
        ShowSetupInstructions();
        
        if (explanationText != null)
        {
            explanationText.text = "VIRTUAL MODE Selected!\n\n" +
                "In this mode, you will:\n" +
                "- Scan ONE QR code to define the object type\n" +
                "- All stack nodes will use that same object\n" +
                "- PUSH/POP operations are automatic - NO rescanning!\n" +
                "- Stack grows VERTICALLY in AR space\n\n" +
                "Perfect for quickly learning LIFO principles!";
        }
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "TIP: One scan = infinite pushes with that object!";
        }
        
        Debug.Log("User selected VIRTUAL mode for Stack");
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
        if (stackManager != null && stackManager.IsStackReady() && !operationsVisible)
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
        if (stackManager != null)
        {
            if (stackManager.GetCurrentMode() == QRStackModeManager.InteractionMode.Physical)
            {
                modeInfo = "Physical Mode: Stack objects horizontally on ground\n\n";
            }
            else
            {
                string objType = stackManager.GetVirtualObjectType().ToUpper();
                modeInfo = $"Virtual Mode: Using {objType} nodes\n" +
                          "All operations are automatic!\n\n";
            }
        }
        
        UpdateExplanation(modeInfo +
            "Stack initialized! Operations available:\n\n" +
            "PUSH: Add to top (O(1))\n" +
            "POP: Remove from top (O(1))\n" +
            "PEEK: View top element (O(1))\n\n" +
            "Notice: ALL stack operations are O(1)!");
            
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "Stack = LIFO (Last In, First Out). Only top element is accessible!";
        }
    }
    
    void OnPushClicked()
    {
        if (stackManager == null) return;
        
        stackManager.SimulatePush();
        
        bool isVirtualMode = stackManager.GetCurrentMode() == QRStackModeManager.InteractionMode.Virtual;
        
        if (isVirtualMode)
        {
            UpdateExplanation($"PUSH operation:\n" +
                $"Tap anywhere to add new {stackManager.GetVirtualObjectType().ToUpper()} to top!\n\n" +
                "Stack will grow UPWARD automatically.\n" +
                "Time Complexity: O(1) - Constant time!");
        }
        else
        {
            UpdateExplanation("PUSH operation:\n" +
                "1. Scan QR code for new object\n" +
                "2. Place it to the RIGHT of the last QR\n" +
                "3. Tap QR to confirm\n\n" +
                "Time Complexity: O(1) - No matter stack size!");
        }
        
        if (tutorialHintText != null)
        {
            if (isVirtualMode)
            {
                tutorialHintText.text = "PUSH always adds to TOP - this is why it's O(1)!";
            }
            else
            {
                tutorialHintText.text = "PUSH always adds to RIGHT - this is why it's O(1)!";
            }
        }
    }
    
    void OnPopClicked()
    {
        if (stackManager == null) return;
        
        if (stackManager.GetStackSize() == 0)
        {
            UpdateExplanation("❌ STACK UNDERFLOW!\n\n" +
                "Cannot POP from an empty stack.\n" +
                "This is an error condition in real programs.\n\n" +
                "Always check: if (!stack.isEmpty()) before popping!");
            
            if (tutorialHintText != null)
                tutorialHintText.text = "Stack Underflow = trying to pop from empty stack";
            
            return;
        }
        
        stackManager.SimulatePop();
        
        bool isVirtualMode = stackManager.GetCurrentMode() == QRStackModeManager.InteractionMode.Virtual;
        
        string removeLocation = isVirtualMode ? "TOP" : "RIGHTMOST";
        
        UpdateExplanation("POP operation complete!\n\n" +
            $"Removed {removeLocation} element from stack.\n" +
            "Time Complexity: O(1) - Constant time!\n\n" +
            $"Why O(1)? We always remove from {removeLocation}.\n" +
            "No shifting or searching needed!");
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = $"POP removes and returns the {removeLocation} element";
        }
    }
    
    void OnPeekClicked()
    {
        if (stackManager == null) return;
        
        if (stackManager.GetStackSize() == 0)
        {
            UpdateExplanation("❌ Cannot PEEK!\n\n" +
                "Stack is empty - nothing to view.\n\n" +
                "PEEK lets you VIEW the top element\n" +
                "WITHOUT removing it from the stack.");
            
            if (tutorialHintText != null)
                tutorialHintText.text = "PEEK = look at top without removing";
            
            return;
        }
        
        stackManager.SimulatePeek();
        
        bool isVirtualMode = stackManager.GetCurrentMode() == QRStackModeManager.InteractionMode.Virtual;
        string topLocation = isVirtualMode ? "top" : "rightmost";
        
        UpdateExplanation("PEEK operation:\n\n" +
            $"Viewing {topLocation} element WITHOUT removing it.\n" +
            "Time Complexity: O(1) - Fastest operation!\n\n" +
            "Use PEEK to:\n" +
            "• Check what's on top\n" +
            "• Validate before popping\n" +
            "• Preview next element");
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "PEEK is non-destructive - stack stays unchanged";
        }
    }
    
    void OnResetClicked()
    {
        if (stackManager == null) return;
        
        stackManager.ResetStack();
        operationsVisible = false;

        // 🔧 FIXED: Force hide analysis panel when resetting
        if (analysisToggle != null)
        {
            analysisToggle.ForceHidePanel();
        }
        
        // 🔧 FIXED: Return to mode selection like Array does
        ShowModeSelection();
        
        UpdateExplanation("");
        
        if (tutorialHintText != null)
            tutorialHintText.text = "Choose a mode to start building your stack!";
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
        if (stackManager == null) return;
        
        int size = stackManager.GetStackSize();
        bool hasElements = size > 0;
        bool canAdd = size < 8;
        
        // PUSH: Enabled if not full
        if (pushButton != null)
        {
            bool pushEnabled = canAdd;
            pushButton.interactable = pushEnabled;
            SetButtonColor(pushButton, pushEnabled);
        }
        
        // POP: Enabled if has elements
        if (popButton != null)
        {
            bool popEnabled = hasElements;
            popButton.interactable = popEnabled;
            SetButtonColor(popButton, popEnabled);
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
        
        // Update status display
        if (statusText != null)
        {
            string status = $"Stack Size: {size}";
            
            if (size == 0)
                status += " (Empty)";
            else if (size >= 8)
                status += " (Full)";
            
            statusText.text = status;
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