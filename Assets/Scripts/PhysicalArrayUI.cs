using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhysicalArrayUI : MonoBehaviour
{
    [Header("References")]
    public PhysicalArrayManager arrayManager;
    
    [Header("Buttons")]
    public Button insertButton;
    public Button confirmShiftButton;
    public Button deleteButton;
    public Button accessButton;
    public Button searchButton;
    public Button resetButton;
    
    [Header("Input Fields")]
    public TMP_InputField indexInputField;
    
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
        if (insertButton != null)
            insertButton.onClick.AddListener(OnInsertClicked);
        
        if (confirmShiftButton != null)
        {
            confirmShiftButton.onClick.AddListener(OnConfirmShiftClicked);
            confirmShiftButton.gameObject.SetActive(false);
            Debug.Log("✅ Confirm button listener added");
        }
        
        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDeleteClicked);
        
        if (accessButton != null)
            accessButton.onClick.AddListener(OnAccessClicked);
        
        if (searchButton != null)
            searchButton.onClick.AddListener(OnSearchClicked);
        
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
            helpText.text = "📚 ARRAY RULES:\n\n" +
                          "• INSERT: Add at any index\n" +
                          "• DELETE: Remove from any index\n" +
                          "• ACCESS: Read value at index\n" +
                          "• SEARCH: Find element\n\n" +
                          "Fixed-size, indexed storage!";
        }
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
        
        if (explanationPanel != null)
            explanationPanel.SetActive(true);
        
        if (helpPanel != null)
            helpPanel.SetActive(true);
        
        UpdateExplanation("✅ Physical Array Ready! Use buttons below.");
    }
    
    void HideButtons()
    {
        if (buttonPanel != null)
            buttonPanel.SetActive(false);
        
        operationsVisible = false;
    }
    
    void OnInsertClicked()
    {
        if (arrayManager == null) return;
        
        int targetIndex = -1;
        
        if (indexInputField != null && !string.IsNullOrEmpty(indexInputField.text))
        {
            if (int.TryParse(indexInputField.text, out targetIndex))
            {
                Debug.Log($"📥 User specified index: {targetIndex}");
            }
            else
            {
                UpdateExplanation("⚠️ Invalid index! Enter a number");
                return;
            }
        }
        else
        {
            Debug.Log("📥 No index specified, will insert at end");
        }
        
        arrayManager.SimulateInsertAtIndex(targetIndex);
        
        if (targetIndex == -1 || targetIndex == arrayManager.GetArraySize())
        {
            UpdateExplanation($"➕ INSERT: Place coin at END position");
            if (confirmShiftButton != null)
                confirmShiftButton.gameObject.SetActive(false);
        }
        else
        {
            UpdateExplanation($"➕ INSERT at [{targetIndex}]: Move coins with YELLOW arrows RIGHT, then press CONFIRM!");
            if (confirmShiftButton != null)
            {
                confirmShiftButton.gameObject.SetActive(true);
                confirmShiftButton.interactable = true;
                Debug.Log("🟡 Confirm button shown and enabled");
            }
        }
        
        if (indexInputField != null)
            indexInputField.text = "";
    }
    
    void OnConfirmShiftClicked()
    {
        Debug.Log("🔵 Confirm Shift button CLICKED!");
        
        if (arrayManager == null)
        {
            Debug.LogError("❌ Array manager is NULL!");
            return;
        }
        
        if (!arrayManager.IsWaitingForShiftConfirmation())
        {
            Debug.LogWarning("⚠️ Not waiting for shift confirmation. Current state unknown.");
            UpdateExplanation("⚠️ No shift to confirm right now");
            return;
        }
        
        Debug.Log("✅ Calling ConfirmShift on arrayManager...");
        arrayManager.ConfirmShift();
        
        if (confirmShiftButton != null)
        {
            confirmShiftButton.gameObject.SetActive(false);
            Debug.Log("🔵 Confirm button hidden");
        }
        
        UpdateExplanation("✅ Shift confirmed! Now place new coin at GREEN spot and TAP it");
    }
    
    void OnDeleteClicked()
    {
        if (arrayManager == null) return;
        
        arrayManager.SimulateDelete();
        UpdateExplanation("➖ DELETE: Tap item to remove");
        
        if (confirmShiftButton != null)
            confirmShiftButton.gameObject.SetActive(false);
    }
    
    void OnAccessClicked()
    {
        if (arrayManager == null) return;
        
        arrayManager.SimulateAccess();
        UpdateExplanation("👁️ ACCESS: Tap item to read value");
        
        if (confirmShiftButton != null)
            confirmShiftButton.gameObject.SetActive(false);
    }
    
    void OnSearchClicked()
    {
        if (arrayManager == null) return;
        
        arrayManager.SimulateSearch();
        UpdateExplanation("🔍 SEARCH: Scanning array...");
        
        if (confirmShiftButton != null)
            confirmShiftButton.gameObject.SetActive(false);
    }
    
    void OnResetClicked()
    {
        if (arrayManager == null) return;
        
        arrayManager.ResetArray();
        operationsVisible = false;
        
        if (confirmShiftButton != null)
            confirmShiftButton.gameObject.SetActive(false);
        
        if (indexInputField != null)
            indexInputField.text = "";
        
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
        Debug.Log($"📋 Explanation: {message}");
    }
    
    void UpdateButtonStates()
    {
        if (arrayManager == null) return;
        
        int size = arrayManager.GetArraySize();
        bool hasElements = size > 0;
        bool canAdd = size < 8;
        bool isWaitingForShift = arrayManager.IsWaitingForShiftConfirmation();
        
        if (insertButton != null)
        {
            insertButton.interactable = canAdd && !isWaitingForShift;
            SetButtonColor(insertButton, canAdd && !isWaitingForShift);
        }
        
        if (deleteButton != null)
        {
            deleteButton.interactable = hasElements && !isWaitingForShift;
            SetButtonColor(deleteButton, hasElements && !isWaitingForShift);
        }
        
        if (accessButton != null)
        {
            accessButton.interactable = hasElements && !isWaitingForShift;
            SetButtonColor(accessButton, hasElements && !isWaitingForShift);
        }
        
        if (searchButton != null)
        {
            searchButton.interactable = hasElements && !isWaitingForShift;
            SetButtonColor(searchButton, hasElements && !isWaitingForShift);
        }
        
        if (confirmShiftButton != null && isWaitingForShift)
        {
            confirmShiftButton.interactable = true;
            SetButtonColor(confirmShiftButton, true);
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