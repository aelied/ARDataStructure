using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComplexityComparisonChart : MonoBehaviour
{
    [Header("Chart UI References")]
    public GameObject chartPanel;
    public RectTransform chartContainer;
    public TextMeshProUGUI chartTitleText;
    
    [Header("Bar Chart Elements")]
    public GameObject barPrefab; // Simple Image component
    public Color o1Color = new Color(0, 1, 0, 0.8f);      // Green for O(1)
    public Color onColor = new Color(1, 0.65f, 0, 0.8f);  // Orange for O(n)
    public Color ologColor = new Color(0, 0.5f, 1, 0.8f); // Blue for O(log n)
    
    [Header("Layout Settings")]
    public float barWidth = 80f;
    public float barSpacing = 20f;
    public float maxBarHeight = 200f;
    
    private AlgorithmAnalysisManager analysisManager;
    
    void Start()
    {
        analysisManager = FindObjectOfType<AlgorithmAnalysisManager>();
    }
    
    /// <summary>
    /// Creates a visual comparison of all array operations
    /// </summary>
    public void ShowOperationComparison(int arraySize)
    {
        ClearChart();
        
        if (chartTitleText != null)
        {
            chartTitleText.text = $"Array Operations Complexity (n = {arraySize})";
        }
        
        // Create bars for different operations
        float xPosition = -300f;
        
        // Access - O(1)
        CreateBar("ACCESS\n[i]", 1, arraySize, o1Color, xPosition, "O(1)\n1 step");
        xPosition += barWidth + barSpacing;
        
        // Insert at End - O(1)
        CreateBar("INSERT\n@end", 1, arraySize, o1Color, xPosition, "O(1)\n1 step");
        xPosition += barWidth + barSpacing;
        
        // Delete from End - O(1)
        CreateBar("DELETE\n@end", 1, arraySize, o1Color, xPosition, "O(1)\n1 step");
        xPosition += barWidth + barSpacing;
        
        // Insert at Middle - O(n/2)
        CreateBar("INSERT\n@middle", arraySize / 2, arraySize, onColor, xPosition, $"O(n/2)\n~{arraySize/2} steps");
        xPosition += barWidth + barSpacing;
        
        // Insert at Start - O(n) WORST
        CreateBar("INSERT\n@start", arraySize, arraySize, onColor, xPosition, $"O(n)\n{arraySize} steps");
        xPosition += barWidth + barSpacing;
        
        // Delete from Start - O(n) WORST
        CreateBar("DELETE\n@start", arraySize, arraySize, onColor, xPosition, $"O(n)\n{arraySize} steps");
        xPosition += barWidth + barSpacing;
        
        // Linear Search - O(n)
        CreateBar("SEARCH\n(linear)", arraySize, arraySize, onColor, xPosition, $"O(n)\navg {arraySize/2}");
        xPosition += barWidth + barSpacing;
        
        // Binary Search - O(log n) (for comparison, if array were sorted)
        int logSteps = Mathf.CeilToInt(Mathf.Log(arraySize, 2));
        CreateBar("SEARCH\n(binary)", logSteps, arraySize, ologColor, xPosition, $"O(log n)\n{logSteps} steps");
        
        chartPanel.SetActive(true);
    }
    
    /// <summary>
    /// Creates a single bar in the chart
    /// </summary>
    void CreateBar(string label, int actualSteps, int maxSteps, Color barColor, float xPos, string tooltip)
    {
        // Create bar container
        GameObject barObj = new GameObject($"Bar_{label}");
        barObj.transform.SetParent(chartContainer, false);
        
        RectTransform barRect = barObj.AddComponent<RectTransform>();
        barRect.anchoredPosition = new Vector2(xPos, 0);
        barRect.sizeDelta = new Vector2(barWidth, maxBarHeight);
        
        // Create colored bar (actual height based on steps)
        GameObject coloredBar = new GameObject("ColoredBar");
        coloredBar.transform.SetParent(barObj.transform, false);
        
        Image barImage = coloredBar.AddComponent<Image>();
        barImage.color = barColor;
        
        RectTransform coloredRect = coloredBar.GetComponent<RectTransform>();
        float normalizedHeight = (float)actualSteps / maxSteps;
        float barHeight = normalizedHeight * maxBarHeight;
        
        coloredRect.anchorMin = new Vector2(0, 0);
        coloredRect.anchorMax = new Vector2(1, 0);
        coloredRect.pivot = new Vector2(0.5f, 0);
        coloredRect.anchoredPosition = Vector2.zero;
        coloredRect.sizeDelta = new Vector2(0, barHeight);
        
        // Add label below bar
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(barObj.transform, false);
        
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 12;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 0);
        labelRect.pivot = new Vector2(0.5f, 1);
        labelRect.anchoredPosition = new Vector2(0, -10);
        labelRect.sizeDelta = new Vector2(0, 40);
        
        // Add value label above bar
        GameObject valueObj = new GameObject("Value");
        valueObj.transform.SetParent(barObj.transform, false);
        
        TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.text = tooltip;
        valueText.fontSize = 10;
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.color = barColor;
        valueText.fontStyle = FontStyles.Bold;
        
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0, 0);
        valueRect.anchorMax = new Vector2(1, 0);
        valueRect.pivot = new Vector2(0.5f, 0);
        valueRect.anchoredPosition = new Vector2(0, barHeight + 5);
        valueRect.sizeDelta = new Vector2(0, 40);
    }
    
    /// <summary>
    /// Shows real-time comparison based on actual session data
    /// </summary>
    public void ShowSessionComparison()
    {
        if (analysisManager == null) return;
        
        ClearChart();
        
        if (chartTitleText != null)
        {
            chartTitleText.text = "Your Session - Operations Performed";
        }
        
        int inserts = analysisManager.GetSessionInserts();
        int deletes = analysisManager.GetSessionDeletes();
        int accesses = analysisManager.GetSessionAccesses();
        int searches = analysisManager.GetSessionSearches();
        
        int maxOps = Mathf.Max(inserts, deletes, accesses, searches, 1);
        
        float xPosition = -200f;
        
        CreateBar("INSERTS", inserts, maxOps, new Color(1, 0.5f, 0, 0.8f), xPosition, $"{inserts}");
        xPosition += barWidth + barSpacing;
        
        CreateBar("DELETES", deletes, maxOps, new Color(1, 0.2f, 0.2f, 0.8f), xPosition, $"{deletes}");
        xPosition += barWidth + barSpacing;
        
        CreateBar("ACCESSES", accesses, maxOps, new Color(0.2f, 1, 0.2f, 0.8f), xPosition, $"{accesses}");
        xPosition += barWidth + barSpacing;
        
        CreateBar("SEARCHES", searches, maxOps, new Color(0.2f, 0.6f, 1, 0.8f), xPosition, $"{searches}");
        
        chartPanel.SetActive(true);
    }
    
    /// <summary>
    /// Shows step count comparison
    /// </summary>
    public void ShowStepsComparison()
    {
        if (analysisManager == null) return;
        
        ClearChart();
        
        if (chartTitleText != null)
        {
            chartTitleText.text = "Total Steps Taken This Session";
        }
        
        int shifts = analysisManager.GetTotalShifts();
        int comparisons = analysisManager.GetTotalComparisons();
        int accesses = analysisManager.GetTotalAccesses();
        
        int maxSteps = Mathf.Max(shifts, comparisons, accesses, 1);
        
        float xPosition = -150f;
        
        CreateBar("SHIFTS", shifts, maxSteps, new Color(1, 0.4f, 0.4f, 0.8f), xPosition, $"{shifts}\nsteps");
        xPosition += barWidth + barSpacing;
        
        CreateBar("COMPARES", comparisons, maxSteps, new Color(0.4f, 0.8f, 1, 0.8f), xPosition, $"{comparisons}\nsteps");
        xPosition += barWidth + barSpacing;
        
        CreateBar("ACCESSES", accesses, maxSteps, new Color(0.6f, 1, 0.6f, 0.8f), xPosition, $"{accesses}\nsteps");
        
        chartPanel.SetActive(true);
    }
    
    /// <summary>
    /// Creates a complexity growth chart showing how operations scale
    /// </summary>
    public void ShowComplexityGrowth()
    {
        ClearChart();
        
        if (chartTitleText != null)
        {
            chartTitleText.text = "How Complexity Grows with Array Size";
        }
        
        int[] arraySizes = { 2, 4, 8, 16, 32 };
        float xPosition = -350f;
        
        for (int i = 0; i < arraySizes.Length; i++)
        {
            int n = arraySizes[i];
            
            // O(1) stays constant
            CreateBar($"n={n}\nO(1)", 1, 32, o1Color, xPosition, "1");
            xPosition += 50f;
            
            // O(log n) grows logarithmically
            int logN = Mathf.CeilToInt(Mathf.Log(n, 2));
            CreateBar($"n={n}\nO(log n)", logN, 32, ologColor, xPosition, $"{logN}");
            xPosition += 50f;
            
            // O(n) grows linearly
            CreateBar($"n={n}\nO(n)", n, 32, onColor, xPosition, $"{n}");
            xPosition += 80f;
        }
        
        chartPanel.SetActive(true);
    }
    
    void ClearChart()
    {
        if (chartContainer == null) return;
        
        foreach (Transform child in chartContainer)
        {
            Destroy(child.gameObject);
        }
    }
    
    public void HideChart()
    {
        if (chartPanel != null)
            chartPanel.SetActive(false);
    }
    
    public void ToggleChart()
    {
        if (chartPanel != null)
            chartPanel.SetActive(!chartPanel.activeSelf);
    }
}