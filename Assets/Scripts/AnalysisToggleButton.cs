using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnalysisToggleButton : MonoBehaviour
{
    [Header("References")]
    public GameObject analysisPanel;
    public Button toggleButton;
    public TextMeshProUGUI buttonText;
    
    [Header("Button States")]
    public string showText = "📊 Show Analysis";
    public string hideText = "✕ Hide Analysis";
    
    [Header("Colors")]
    public Color showColor = new Color(0f, 0.8f, 1f); // Cyan
    public Color hideColor = new Color(1f, 0.5f, 0f); // Orange
    
    private bool isAnalysisVisible = false;
    
    void Start()
    {
        if (toggleButton == null)
            toggleButton = GetComponent<Button>();
        
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleAnalysisPanel);
        }
        
        // IMPORTANT: Start with panel hidden
        if (analysisPanel != null)
        {
            analysisPanel.SetActive(false);
            isAnalysisVisible = false;
        }
        
        UpdateButtonAppearance();
        
        Debug.Log("Analysis panel initialized as HIDDEN");
    }
    
    public void ToggleAnalysisPanel()
    {
        if (analysisPanel == null)
        {
            Debug.LogWarning("Analysis panel reference is missing!");
            return;
        }
        
        isAnalysisVisible = !isAnalysisVisible;
        analysisPanel.SetActive(isAnalysisVisible);
        
        UpdateButtonAppearance();
        
        Debug.Log($"Analysis panel: {(isAnalysisVisible ? "SHOWN" : "HIDDEN")}");
    }
    
    void UpdateButtonAppearance()
    {
        // Update button text
        if (buttonText != null)
        {
            buttonText.text = isAnalysisVisible ? hideText : showText;
        }
        
        // Update button colors
        if (toggleButton != null)
        {
            ColorBlock colors = toggleButton.colors;
            Color targetColor = isAnalysisVisible ? hideColor : showColor;
            
            colors.normalColor = targetColor;
            colors.highlightedColor = targetColor * 1.2f;
            colors.pressedColor = targetColor * 0.8f;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            
            toggleButton.colors = colors;
        }
        
        // Update text color
        if (buttonText != null)
        {
            buttonText.color = Color.white;
        }
    }
    
    // Public method to force hide the panel (useful for reset operations)
    public void ForceHidePanel()
    {
        if (analysisPanel != null)
        {
            isAnalysisVisible = false;
            analysisPanel.SetActive(false);
            UpdateButtonAppearance();
        }
    }
    
    // Public method to check if panel is visible
    public bool IsAnalysisVisible()
    {
        return isAnalysisVisible;
    }
}