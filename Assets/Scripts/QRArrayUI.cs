using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QRArrayModeUI : MonoBehaviour
{
    [Header("References")]
    public QRArrayModeManager arrayManager;
    
    [Header("Mode Selection")]
    public GameObject modeSelectionPanel;
    public Button physicalModeButton;
    public Button virtualModeButton;
    
    [Header("Main Buttons")]
    public Button insertButton;
    public Button confirmShiftButton;
    public Button deleteButton;
    public Button accessButton;
    public Button searchButton;
    public Button resetButton;
    
    [Header("Input Field")]
    public TMP_InputField indexInputField;
    
    [Header("UI Panels")]
    public GameObject headerPanel;
    public GameObject instructionCard;
    public GameObject buttonPanel;
    public GameObject explanationPanel;
    public GameObject inputPanel; // 🔧 ADDED - Reference to the input panel
    
    [Header("Info Display")]
    public TextMeshProUGUI explanationText;
    public TextMeshProUGUI tutorialHintText;
    public TextMeshProUGUI statusText; // 🔧 ADDED - Reference to status text
    
    [Header("Analysis Toggle")]
    public AnalysisToggleButton analysisToggle; // 🔧 ADDED THIS LINE
    
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
        
        if (insertButton != null)
        {
            insertButton.onClick.AddListener(OnInsertClicked);
            insertButton.interactable = false;
        }
        
        if (confirmShiftButton != null)
        {
            confirmShiftButton.onClick.AddListener(OnConfirmShiftClicked);
            confirmShiftButton.gameObject.SetActive(false);
        }
        
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteClicked);
            deleteButton.interactable = false;
        }
        
        if (accessButton != null)
        {
            accessButton.onClick.AddListener(OnAccessClicked);
            accessButton.interactable = false;
        }
        
        if (searchButton != null)
        {
            searchButton.onClick.AddListener(OnSearchClicked);
            searchButton.interactable = false;
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
            resetButton.interactable = true;
            SetButtonColor(resetButton, true);
        }
        
        // 🔧 Set initial tutorial hint text
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "Choose a mode to start building your array!";
        }
        
        // 🔧 Set initial status text
        if (statusText != null)
        {
            statusText.text = "Elements: 0";
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
        
        if (inputPanel != null)
            inputPanel.SetActive(false); // 🔧 Hide input panel
        
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        // 🔧 Force hide analysis panel when returning to mode selection
        if (analysisToggle != null)
        {
            analysisToggle.ForceHidePanel();
        }
        
        operationsVisible = false;
    }
    
    void OnPhysicalModeSelected()
    {
        if (arrayManager == null) return;
        
        arrayManager.SetPhysicalMode();
        HideModeSelection();
        ShowSetupInstructions();
        
        if (explanationText != null)
        {
            explanationText.text = "PHYSICAL MODE Selected!\n\n" +
                "In this mode, you will: Scan QR codes and MOVE them physically in real world, Place objects at specific positionsPhysically shift objects for insert/delete operations\n\n" +
                "This mode teaches array concepts with hands-on interaction!";
        }
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "TIP: You'll need to physically move the QR codes for operations";
        }
        
        Debug.Log("User selected PHYSICAL mode");
    }
    
    void OnVirtualModeSelected()
    {
        if (arrayManager == null) return;
        
        arrayManager.SetVirtualMode();
        HideModeSelection();
        ShowSetupInstructions();
        
        if (explanationText != null)
        {
            explanationText.text = "VIRTUAL MODE Selected!\n\n" +
                "In this mode, you will:\n" +
                "- Scan ONE QR code to define the object type\n" +
                "- All array nodes will use that same object\n" +
                "- Insert/delete operations are automatic - NO rescanning needed!\n" +
                "- Objects are manipulated virtually in AR space\n\n" +
                "Perfect for quick demonstrations and learning!";
        }
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "TIP: Just scan ONE QR - all nodes will be that object!";
        }
        
        Debug.Log("User selected VIRTUAL mode");
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
        
        if (inputPanel != null)
            inputPanel.SetActive(false); // 🔧 Keep input panel hidden during setup
        
        if (explanationPanel != null)
            explanationPanel.SetActive(true);
    }
    
    void Update()
    {
        if (arrayManager != null && arrayManager.IsArrayReady() && !operationsVisible)
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
        
        if (inputPanel != null)
            inputPanel.SetActive(true); // 🔧 Show input panel when operations are ready
        
        if (explanationPanel != null)
            explanationPanel.SetActive(true);
        
        string modeInfo = "";
        if (arrayManager != null)
        {
            if (arrayManager.GetCurrentMode() == QRArrayModeManager.InteractionMode.Physical)
            {
                modeInfo = "Physical Mode: Move objects physically for operations\n\n";
            }
            else
            {
                string objType = arrayManager.GetVirtualObjectType().ToUpper();
                modeInfo = $"Virtual Mode: Using {objType} nodes\n" +
                          "All operations are automatic - no rescanning!\n\n";
            }
        }
        
        UpdateExplanation(modeInfo +
            "Array initialized! Operations available:\n\n" +
            "INSERT: Add new nodes (just tap!)\n" +
            "DELETE: Remove nodes (type index)\n" +
            "ACCESS: View details (type index)\n" +
            "SEARCH: Find objects");
            
        if (tutorialHintText != null)
        {
            if (arrayManager.GetCurrentMode() == QRArrayModeManager.InteractionMode.Virtual)
            {
                tutorialHintText.text = "Virtual Mode: All insertions use the same object automatically!";
            }
            else
            {
                tutorialHintText.text = "For DELETE/ACCESS: Type index number first, then press button";
            }
        }
    }
    
    void OnInsertClicked()
    {
        if (arrayManager == null) return;
        
        int targetIndex = -1;
        
        if (indexInputField != null && !string.IsNullOrEmpty(indexInputField.text))
        {
            if (!int.TryParse(indexInputField.text, out targetIndex))
            {
                UpdateExplanation("Invalid index! Enter a number (0, 1, 2...)");
                return;
            }
        }
        
        arrayManager.SimulateInsertAtIndex(targetIndex);
        
        bool isVirtualMode = arrayManager.GetCurrentMode() == QRArrayModeManager.InteractionMode.Virtual;
        
        if (targetIndex == -1 || targetIndex == arrayManager.GetArraySize())
        {
            if (isVirtualMode)
            {
                UpdateExplanation($"INSERT at end [{arrayManager.GetArraySize()}]:\n" +
                    $"Tap anywhere to add new {arrayManager.GetVirtualObjectType().ToUpper()} node!\n" +
                    "No scanning needed!");
            }
            else
            {
                UpdateExplanation($"INSERT at end:\n" +
                    "1. Scan QR code at GREEN spot\n" +
                    "2. Tap QR to confirm");
            }
            
            if (confirmShiftButton != null)
                confirmShiftButton.gameObject.SetActive(false);
        }
        else
        {
            if (isVirtualMode)
            {
                UpdateExplanation($"INSERT at [{targetIndex}]:\n" +
                    $"Tap anywhere to add {arrayManager.GetVirtualObjectType().ToUpper()}!\n" +
                    "Objects will shift automatically!");
            }
            else
            {
                int numToMove = arrayManager.GetArraySize() - targetIndex;
                UpdateExplanation($"INSERT at [{targetIndex}]:\n" +
                    $"1. Move {numToMove} objects RIGHT (yellow spots)\n" +
                    $"2. Press CONFIRM\n" +
                    $"3. Scan new QR at GREEN spot");
                
                if (confirmShiftButton != null)
                {
                    confirmShiftButton.gameObject.SetActive(true);
                    confirmShiftButton.interactable = true;
                    SetButtonColor(confirmShiftButton, true);
                }
            }
        }
        
        if (indexInputField != null)
            indexInputField.text = "";
    }
    
    void OnConfirmShiftClicked()
    {
        if (arrayManager == null) return;
        
        if (!arrayManager.IsWaitingForShiftConfirmation())
        {
            UpdateExplanation("No shift to confirm");
            return;
        }
        
        arrayManager.ConfirmShift();
        
        if (confirmShiftButton != null)
        {
            confirmShiftButton.gameObject.SetActive(false);
        }
        
        UpdateExplanation("Shift confirmed! Now scan the new object's QR code");
    }
    
    void OnDeleteClicked()
    {
        if (arrayManager == null) return;
        
        if (indexInputField == null || string.IsNullOrEmpty(indexInputField.text))
        {
            UpdateExplanation("Please type an index number first!\n\n" +
                "Example: Type '0' to delete first object\n" +
                "Example: Type '1' to delete second object");
            
            if (tutorialHintText != null)
                tutorialHintText.text = $"Valid indices: 0 to {arrayManager.GetArraySize() - 1}";
            
            if (indexInputField != null)
                indexInputField.Select();
            
            return;
        }
        
        if (!int.TryParse(indexInputField.text, out int targetIndex))
        {
            UpdateExplanation("Invalid index! Enter a number");
            return;
        }
        
        if (targetIndex < 0 || targetIndex >= arrayManager.GetArraySize())
        {
            UpdateExplanation($"Invalid index!\n\n" +
                $"Use 0 to {arrayManager.GetArraySize() - 1}\n" +
                $"Array has {arrayManager.GetArraySize()} elements");
            return;
        }
        
        arrayManager.SimulateDeleteAtIndex(targetIndex);
        
        bool isVirtualMode = arrayManager.GetCurrentMode() == QRArrayModeManager.InteractionMode.Virtual;
        
        if (isVirtualMode)
        {
            UpdateExplanation($"DELETE at [{targetIndex}]:\n" +
                "Element removed!\n" +
                "Objects shifted automatically!");
        }
        else
        {
            if (targetIndex == arrayManager.GetArraySize() - 1)
            {
                UpdateExplanation($"DELETE at [{targetIndex}]:\n" +
                    "Last element removed!");
                
                if (confirmShiftButton != null)
                    confirmShiftButton.gameObject.SetActive(false);
            }
            else
            {
                int numToMove = arrayManager.GetArraySize() - targetIndex - 1;
                UpdateExplanation($"DELETE at [{targetIndex}]:\n" +
                    $"1. Remove object\n" +
                    $"2. Move {numToMove} objects LEFT (yellow spots)\n" +
                    $"3. Press CONFIRM");
                
                if (confirmShiftButton != null)
                {
                    confirmShiftButton.gameObject.SetActive(true);
                    confirmShiftButton.interactable = true;
                    SetButtonColor(confirmShiftButton, true);
                }
            }
        }
        
        if (indexInputField != null)
            indexInputField.text = "";
    }
    
    void OnAccessClicked()
    {
        if (arrayManager == null) return;
        
        if (indexInputField == null || string.IsNullOrEmpty(indexInputField.text))
        {
            UpdateExplanation("Please type an index number first!\n\n" +
                "Example: Type '0' to access first object\n" +
                "Example: Type '2' to access third object");
            
            if (tutorialHintText != null)
                tutorialHintText.text = $"Valid indices: 0 to {arrayManager.GetArraySize() - 1}";
            
            if (indexInputField != null)
                indexInputField.Select();
            
            return;
        }
        
        if (!int.TryParse(indexInputField.text, out int targetIndex))
        {
            UpdateExplanation("Invalid index! Enter a number");
            return;
        }
        
        if (targetIndex < 0 || targetIndex >= arrayManager.GetArraySize())
        {
            UpdateExplanation($"Invalid index!\n\n" +
                $"Use 0 to {arrayManager.GetArraySize() - 1}\n" +
                $"Array has {arrayManager.GetArraySize()} elements");
            return;
        }
        
        arrayManager.SimulateAccessAtIndex(targetIndex);
        UpdateExplanation($"ACCESS at [{targetIndex}]:\n" +
            "Watch for the cyan highlight!");
        
        if (confirmShiftButton != null)
            confirmShiftButton.gameObject.SetActive(false);
        
        if (indexInputField != null)
            indexInputField.text = "";
    }
    
    void OnSearchClicked()
    {
        if (arrayManager == null) return;
        
        arrayManager.SimulateSearch();
        UpdateExplanation("SEARCH operation:\n" +
            "Scanning through each element...\n" +
            "This is LINEAR SEARCH algorithm");
        
        if (confirmShiftButton != null)
            confirmShiftButton.gameObject.SetActive(false);
    }
    
    void OnResetClicked()
    {
        if (arrayManager == null) return;
        
        arrayManager.ResetArray();
        operationsVisible = false;

        // 🔧 Hide analysis panel when resetting
        if (analysisToggle != null)
        {
            analysisToggle.ForceHidePanel();
        }
        
        if (confirmShiftButton != null)
            confirmShiftButton.gameObject.SetActive(false);
        
        if (indexInputField != null)
            indexInputField.text = "";
        
        ShowModeSelection();
        
        UpdateExplanation("");
        
        if (tutorialHintText != null)
            tutorialHintText.text = "Choose a mode to start building your array!";
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
        if (arrayManager == null) return;
        
        int size = arrayManager.GetArraySize();
        bool hasElements = size > 0;
        bool canAdd = size < 8;
        bool isWaitingForShift = arrayManager.IsWaitingForShiftConfirmation();
        bool hasIndexInput = indexInputField != null && !string.IsNullOrEmpty(indexInputField.text);
        
        // INSERT: Enabled if not full and not waiting
        if (insertButton != null)
        {
            bool insertEnabled = canAdd && !isWaitingForShift;
            insertButton.interactable = insertEnabled;
            SetButtonColor(insertButton, insertEnabled);
        }
        
        // DELETE: Enabled if has elements, not waiting, AND has index
        if (deleteButton != null)
        {
            bool deleteEnabled = hasElements && !isWaitingForShift && hasIndexInput;
            deleteButton.interactable = deleteEnabled;
            SetButtonColor(deleteButton, deleteEnabled);
        }
        
        // ACCESS: Enabled if has elements, not waiting, AND has index
        if (accessButton != null)
        {
            bool accessEnabled = hasElements && !isWaitingForShift && hasIndexInput;
            accessButton.interactable = accessEnabled;
            SetButtonColor(accessButton, accessEnabled);
        }
        
        // SEARCH: Enabled if has elements and not waiting
        if (searchButton != null)
        {
            bool searchEnabled = hasElements && !isWaitingForShift;
            searchButton.interactable = searchEnabled;
            SetButtonColor(searchButton, searchEnabled);
        }
        
        // CONFIRM: Enabled when waiting for shift (physical mode only)
        if (confirmShiftButton != null && isWaitingForShift)
        {
            confirmShiftButton.interactable = true;
            SetButtonColor(confirmShiftButton, true);
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