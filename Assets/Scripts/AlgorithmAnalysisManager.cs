using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

public class AlgorithmAnalysisManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI complexityDisplayText;
    public TextMeshProUGUI operationCounterText;
    public TextMeshProUGUI performanceMetricsText;
    public TextMeshProUGUI educationalExplanationText;
    
    [Header("Analysis Panels")]
    public GameObject analysisPanel;
    public GameObject detailedMetricsPanel;
    
    // Operation tracking
    private int totalComparisons = 0;
    private int totalShifts = 0;
    private int totalAccesses = 0;
    private Dictionary<string, int> operationHistory = new Dictionary<string, int>();
    
    // Session statistics
    private int sessionInserts = 0;
    private int sessionDeletes = 0;
    private int sessionAccesses = 0;
    private int sessionSearches = 0;
    
    private QRArrayModeManager arrayManager;
    
    void Start()
    {
        arrayManager = FindObjectOfType<QRArrayModeManager>();
        ResetCounters();
    }
    
    // ═══════════════════════════════════════════════════════════
    // INSERT OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeInsertOperation(int insertIndex, int arraySize)
    {
        sessionInserts++;
        
        // Calculate complexity metrics
        int shiftsRequired = arraySize - insertIndex;
        totalShifts += shiftsRequired;
        totalAccesses += 1; // One access to insert
        
        // Time Complexity Analysis
        string timeComplexity;
        string explanation;
        
        if (insertIndex == arraySize)
        {
            // Insert at end
            timeComplexity = "O(1) - Constant Time";
            explanation = "Inserting at the END requires NO shifting.\n" +
                         "We directly place the element at the next available position.\n" +
                         $"Steps: 1 operation (just insertion)";
        }
        else if (insertIndex == 0)
        {
            // Insert at beginning (worst case)
            timeComplexity = "O(n) - Linear Time (WORST CASE)";
            explanation = $"Inserting at index [0] requires shifting ALL {arraySize} elements.\n" +
                         "Every element must move one position to the right.\n" +
                         $"Steps: {shiftsRequired} shifts + 1 insertion = {shiftsRequired + 1} operations";
        }
        else
        {
            // Insert in middle
            timeComplexity = "O(n) - Linear Time";
            explanation = $"Inserting at index [{insertIndex}] requires shifting {shiftsRequired} elements.\n" +
                         "All elements from position [{insertIndex}] to [{arraySize-1}] must move right.\n" +
                         $"Steps: {shiftsRequired} shifts + 1 insertion = {shiftsRequired + 1} operations";
        }
        
        // Update displays
        UpdateComplexityDisplay("INSERT", timeComplexity, "O(1)", shiftsRequired);
        UpdateOperationCounter(shiftsRequired, 0, 1);
        UpdateEducationalExplanation(explanation, GetInsertCaseAnalysis(insertIndex, arraySize));
        
        // Log to history
        RecordOperation($"Insert at [{insertIndex}]", shiftsRequired + 1);
    }
    
    private string GetInsertCaseAnalysis(int index, int size)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ COMPLEXITY BREAKDOWN ━━━");
        analysis.AppendLine($"• Best Case: O(1) - Insert at end (index = {size})");
        analysis.AppendLine($"• Worst Case: O(n) - Insert at start (index = 0)");
        analysis.AppendLine($"• Average Case: O(n/2) ≈ O(n) - Insert in middle");
        analysis.AppendLine($"• Your Case: {(index == size ? "BEST" : index == 0 ? "WORST" : "AVERAGE")}");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // DELETE OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeDeleteOperation(int deleteIndex, int arraySize)
    {
        sessionDeletes++;
        
        // Calculate complexity metrics
        int shiftsRequired = arraySize - deleteIndex - 1;
        totalShifts += shiftsRequired;
        totalAccesses += 1; // One access to delete
        
        // Time Complexity Analysis
        string timeComplexity;
        string explanation;
        
        if (deleteIndex == arraySize - 1)
        {
            // Delete last element
            timeComplexity = "O(1) - Constant Time";
            explanation = "Deleting the LAST element requires NO shifting.\n" +
                         "We simply remove the element at the end.\n" +
                         "Steps: 1 operation (just deletion)";
        }
        else if (deleteIndex == 0)
        {
            // Delete first element (worst case)
            timeComplexity = "O(n) - Linear Time (WORST CASE)";
            explanation = $"Deleting index [0] requires shifting {shiftsRequired} elements LEFT.\n" +
                         "Every element after position [0] must move one position left.\n" +
                         $"Steps: 1 deletion + {shiftsRequired} shifts = {shiftsRequired + 1} operations";
        }
        else
        {
            // Delete from middle
            timeComplexity = "O(n) - Linear Time";
            explanation = $"Deleting index [{deleteIndex}] requires shifting {shiftsRequired} elements LEFT.\n" +
                         $"Elements from [{deleteIndex + 1}] to [{arraySize - 1}] must move left.\n" +
                         $"Steps: 1 deletion + {shiftsRequired} shifts = {shiftsRequired + 1} operations";
        }
        
        // Update displays
        UpdateComplexityDisplay("DELETE", timeComplexity, "O(1)", shiftsRequired);
        UpdateOperationCounter(shiftsRequired, 0, 1);
        UpdateEducationalExplanation(explanation, GetDeleteCaseAnalysis(deleteIndex, arraySize));
        
        // Log to history
        RecordOperation($"Delete at [{deleteIndex}]", shiftsRequired + 1);
    }
    
    private string GetDeleteCaseAnalysis(int index, int size)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ COMPLEXITY BREAKDOWN ━━━");
        analysis.AppendLine($"• Best Case: O(1) - Delete last element (index = {size - 1})");
        analysis.AppendLine($"• Worst Case: O(n) - Delete first element (index = 0)");
        analysis.AppendLine($"• Average Case: O(n/2) ≈ O(n) - Delete from middle");
        analysis.AppendLine($"• Your Case: {(index == size - 1 ? "BEST" : index == 0 ? "WORST" : "AVERAGE")}");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // ACCESS OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeAccessOperation(int accessIndex, int arraySize)
    {
        sessionAccesses++;
        totalAccesses++;
        
        // Time Complexity Analysis
        string timeComplexity = "O(1) - Constant Time";
        string explanation = $"Accessing element at index [{accessIndex}] is INSTANT!\n" +
                            "Arrays use direct memory addressing.\n" +
                            "Formula: base_address + (index × element_size)\n" +
                            "Steps: 1 operation (direct access)\n\n" +
                            "This is the MAIN ADVANTAGE of arrays!";
        
        // Update displays
        UpdateComplexityDisplay("ACCESS", timeComplexity, "O(1)", 0);
        UpdateOperationCounter(0, 0, 1);
        UpdateEducationalExplanation(explanation, GetAccessAnalysis());
        
        // Log to history
        RecordOperation($"Access at [{accessIndex}]", 1);
    }
    
    private string GetAccessAnalysis()
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ WHY O(1) ACCESS? ━━━");
        analysis.AppendLine("Arrays store elements in CONTIGUOUS memory.");
        analysis.AppendLine("The computer calculates exact position instantly:");
        analysis.AppendLine("  Position = Base + (Index × Size)");
        analysis.AppendLine("\nThis is true for ALL indices:");
        analysis.AppendLine("• First element [0]: O(1)");
        analysis.AppendLine("• Last element [n-1]: O(1)");
        analysis.AppendLine("• Any middle element: O(1)");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // SEARCH OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeSearchOperation(int arraySize, bool found, int foundAtIndex = -1)
    {
        sessionSearches++;
        
        int comparisons = found ? foundAtIndex + 1 : arraySize;
        totalComparisons += comparisons;
        
        // Time Complexity Analysis
        string timeComplexity = "O(n) - Linear Time";
        string explanation;
        
        if (found)
        {
            if (foundAtIndex == 0)
            {
                explanation = "LINEAR SEARCH - BEST CASE!\n" +
                             "Found element at first position [0].\n" +
                             $"Comparisons: 1\n" +
                             "This is the luckiest scenario - O(1)";
            }
            else if (foundAtIndex == arraySize - 1)
            {
                explanation = "LINEAR SEARCH - WORST CASE!\n" +
                             $"Found element at last position [{arraySize - 1}].\n" +
                             $"Comparisons: {comparisons}\n" +
                             "Had to check every element - O(n)";
            }
            else
            {
                explanation = $"LINEAR SEARCH - AVERAGE CASE\n" +
                             $"Found element at position [{foundAtIndex}].\n" +
                             $"Comparisons: {comparisons}\n" +
                             $"Average case: O(n/2) ≈ O(n)";
            }
        }
        else
        {
            explanation = "LINEAR SEARCH - ELEMENT NOT FOUND\n" +
                         $"Checked ALL {arraySize} elements.\n" +
                         $"Comparisons: {comparisons}\n" +
                         "This is also WORST CASE - O(n)";
        }
        
        // Update displays
        UpdateComplexityDisplay("SEARCH (Linear)", timeComplexity, "O(1)", 0);
        UpdateOperationCounter(0, comparisons, arraySize);
        UpdateEducationalExplanation(explanation, GetSearchAnalysis(arraySize));
        
        // Log to history
        RecordOperation($"Linear Search ({(found ? "found" : "not found")})", comparisons);
    }
    
    private string GetSearchAnalysis(int size)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ LINEAR SEARCH COMPLEXITY ━━━");
        analysis.AppendLine($"Array Size: {size} elements");
        analysis.AppendLine($"• Best Case: O(1) - Found at index [0]");
        analysis.AppendLine($"• Worst Case: O(n) - Found at [{size-1}] or not found");
        analysis.AppendLine($"• Average Case: O(n/2) ≈ O(n)");
        analysis.AppendLine("\n━━━ BETTER ALTERNATIVE ━━━");
        analysis.AppendLine("Binary Search: O(log n) - but requires SORTED array!");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // UI UPDATE FUNCTIONS
    // ═══════════════════════════════════════════════════════════
    
    void UpdateComplexityDisplay(string operation, string timeComplexity, string spaceComplexity, int actualSteps)
    {
        if (complexityDisplayText == null) return;
        
        StringBuilder display = new StringBuilder();
        display.AppendLine($"<size=24><b>{operation} OPERATION</b></size>");
        display.AppendLine($"<color=#FFD700>━━━━━━━━━━━━━━━━━━━━━━</color>");
        display.AppendLine($"\n<b>Time Complexity:</b> <color=#00FF00>{timeComplexity}</color>");
        display.AppendLine($"<b>Space Complexity:</b> <color=#00FFFF>{spaceComplexity}</color>");
        
        if (actualSteps > 0)
        {
            display.AppendLine($"\n<b>Actual Steps Taken:</b> <color=#FF8C00>{actualSteps}</color>");
        }
        
        complexityDisplayText.text = display.ToString();
    }
    
    void UpdateOperationCounter(int shifts, int comparisons, int accesses)
    {
        if (operationCounterText == null) return;
        
        StringBuilder counter = new StringBuilder();
        counter.AppendLine("<size=20><b>OPERATION METRICS</b></size>");
        counter.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━</color>");
        
        if (shifts > 0)
            counter.AppendLine($"\n<b>Shifts:</b> <color=#FF6B6B>{shifts}</color>");
        
        if (comparisons > 0)
            counter.AppendLine($"<b>Comparisons:</b> <color=#4ECDC4>{comparisons}</color>");
        
        if (accesses > 0)
            counter.AppendLine($"<b>Accesses:</b> <color=#95E1D3>{accesses}</color>");
        
        // Session totals
        counter.AppendLine($"\n<size=16><b>SESSION TOTALS:</b></size>");
        counter.AppendLine($"Total Shifts: {totalShifts}");
        counter.AppendLine($"Total Comparisons: {totalComparisons}");
        counter.AppendLine($"Total Accesses: {totalAccesses}");
        
        operationCounterText.text = counter.ToString();
    }
    
    void UpdateEducationalExplanation(string mainExplanation, string detailedAnalysis)
    {
        if (educationalExplanationText == null) return;
        
        StringBuilder explanation = new StringBuilder();
        explanation.AppendLine("<size=18><b>UNDERSTANDING THE ALGORITHM</b></size>");
        explanation.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>\n");
        explanation.AppendLine(mainExplanation);
        explanation.AppendLine(detailedAnalysis);
        
        educationalExplanationText.text = explanation.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // PERFORMANCE METRICS & STATISTICS
    // ═══════════════════════════════════════════════════════════
    
    public void UpdatePerformanceMetrics()
    {
        if (performanceMetricsText == null) return;
        
        StringBuilder metrics = new StringBuilder();
        metrics.AppendLine("<size=20><b>SESSION STATISTICS</b></size>");
        metrics.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━━━━</color>\n");
        
        metrics.AppendLine($"<b>Operations Performed:</b>");
        metrics.AppendLine($"  • Inserts: {sessionInserts}");
        metrics.AppendLine($"  • Deletes: {sessionDeletes}");
        metrics.AppendLine($"  • Accesses: {sessionAccesses}");
        metrics.AppendLine($"  • Searches: {sessionSearches}");
        
        int totalOps = sessionInserts + sessionDeletes + sessionAccesses + sessionSearches;
        metrics.AppendLine($"\n<b>Total Operations:</b> {totalOps}");
        
        if (totalOps > 0)
        {
            float avgComplexity = (float)(totalShifts + totalComparisons) / totalOps;
            metrics.AppendLine($"<b>Avg Steps/Operation:</b> {avgComplexity:F2}");
        }
        
        // Most/Least efficient operations
        if (operationHistory.Count > 0)
        {
            metrics.AppendLine("\n<b>Most Efficient:</b> O(1) direct operations");
            metrics.AppendLine("<b>Least Efficient:</b> O(n) operations requiring shifts");
        }
        
        performanceMetricsText.text = metrics.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // COMPARATIVE ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public string GetComparativeAnalysis(int arraySize)
    {
        StringBuilder comparison = new StringBuilder();
        comparison.AppendLine("<size=22><b>ARRAY OPERATION COMPARISON</b></size>");
        comparison.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>\n");
        
        comparison.AppendLine($"<b>For array of size {arraySize}:</b>\n");
        
        comparison.AppendLine("<color=#00FF00><b>FAST Operations (O(1)):</b></color>");
        comparison.AppendLine("  • Access any element: 1 step");
        comparison.AppendLine("  • Insert at end: 1 step");
        comparison.AppendLine("  • Delete from end: 1 step");
        
        comparison.AppendLine($"\n<color=#FF8C00><b>SLOW Operations (O(n)):</b></color>");
        comparison.AppendLine($"  • Insert at start: up to {arraySize} shifts");
        comparison.AppendLine($"  • Delete from start: up to {arraySize} shifts");
        comparison.AppendLine($"  • Linear search: up to {arraySize} comparisons");
        
        comparison.AppendLine("\n<b>KEY INSIGHT:</b>");
        comparison.AppendLine("Position matters! Operations at the END are fast.");
        comparison.AppendLine("Operations at the START require moving everything.");
        
        return comparison.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // HELPER FUNCTIONS
    // ═══════════════════════════════════════════════════════════
    
    void RecordOperation(string operationName, int steps)
    {
        if (!operationHistory.ContainsKey(operationName))
        {
            operationHistory[operationName] = 0;
        }
        operationHistory[operationName] += steps;
        
        UpdatePerformanceMetrics();
    }
    
    public void ResetCounters()
    {
        totalComparisons = 0;
        totalShifts = 0;
        totalAccesses = 0;
        sessionInserts = 0;
        sessionDeletes = 0;
        sessionAccesses = 0;
        sessionSearches = 0;
        operationHistory.Clear();
        
        UpdatePerformanceMetrics();
    }
    
    public void ShowAnalysisPanel(bool show)
    {
        if (analysisPanel != null)
            analysisPanel.SetActive(show);
    }
    
    public void ShowDetailedMetrics(bool show)
    {
        if (detailedMetricsPanel != null)
            detailedMetricsPanel.SetActive(show);
    }
    
    // ═══════════════════════════════════════════════════════════
    // PUBLIC GETTERS FOR EXTERNAL USE
    // ═══════════════════════════════════════════════════════════
    
    public int GetTotalShifts() => totalShifts;
    public int GetTotalComparisons() => totalComparisons;
    public int GetTotalAccesses() => totalAccesses;
    public int GetSessionInserts() => sessionInserts;
    public int GetSessionDeletes() => sessionDeletes;
    public int GetSessionAccesses() => sessionAccesses;
    public int GetSessionSearches() => sessionSearches;
    
    public Dictionary<string, int> GetOperationHistory() => new Dictionary<string, int>(operationHistory);
}