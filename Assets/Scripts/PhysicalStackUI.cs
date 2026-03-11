using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhysicalStackUI : MonoBehaviour
{
    [Header("References")]
    public PhysicalStackManager stackManager;
    
    [Header("Buttons")]
    public Button pushButton;
    public Button popButton;
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
        if (pushButton != null)
            pushButton.onClick.AddListener(OnPushClicked);
        
        if (popButton != null)
            popButton.onClick.AddListener(OnPopClicked);
        
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
            helpText.text = "📚 STACK RULES:\n\n" +
                          "• PUSH: Add to TOP\n" +
                          "• POP: Remove from TOP\n" +
                          "• PEEK: View TOP element\n" +
                          "• LIFO: Last In, First Out\n\n" +
                          "Stack books vertically!";
        }
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
        
        if (helpPanel != null)
            helpPanel.SetActive(true);
        
        UpdateExplanation("✅ Physical Stack Ready! Use buttons below.");
    }
    
    void HideButtons()
    {
        if (buttonPanel != null)
            buttonPanel.SetActive(false);
        
        operationsVisible = false;
    }
    
    void OnPushClicked()
    {
        if (stackManager == null) return;
        
        stackManager.SimulatePush();
        UpdateExplanation("➕ PUSH: Place book at GREEN spot (TOP)");
    }
    
    void OnPopClicked()
    {
        if (stackManager == null) return;
        
        stackManager.SimulatePop();
        UpdateExplanation("➖ POP: Remove book at RED spot (TOP)");
    }
    
    void OnPeekClicked()
    {
        if (stackManager == null) return;
        
        stackManager.SimulatePeek();
        UpdateExplanation("👁️ PEEK: Viewing top element...");
    }
    
    void OnResetClicked()
    {
        if (stackManager == null) return;
        
        stackManager.ResetStack();
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
        if (stackManager == null) return;
        
        int size = stackManager.GetStackSize();
        bool hasElements = size > 0;
        bool canAdd = size < 8;
        
        if (pushButton != null)
        {
            pushButton.interactable = canAdd;
            SetButtonColor(pushButton, canAdd);
        }
        
        if (popButton != null)
        {
            popButton.interactable = hasElements;
            SetButtonColor(popButton, hasElements);
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
            colors.normalColor = new Color(0.608f, 0.349f, 0.714f);
            colors.highlightedColor = new Color(0.708f, 0.449f, 0.814f);
            colors.pressedColor = new Color(0.408f, 0.149f, 0.514f);
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