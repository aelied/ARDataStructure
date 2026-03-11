using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

public class StackAnalysisManager : MonoBehaviour
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
    private int totalOperations = 0;
    private Dictionary<string, int> operationHistory = new Dictionary<string, int>();
    
    // Session statistics
    private int sessionPushes = 0;
    private int sessionPops = 0;
    private int sessionPeeks = 0;
    
    private QRStackModeManager stackManager;
    
    void Start()
    {
        stackManager = FindObjectOfType<QRStackModeManager>();
        ResetCounters();
    }
    
    // ═══════════════════════════════════════════════════════════
    // PUSH OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzePushOperation(int stackSize)
    {
        sessionPushes++;
        totalOperations++;
        
        // Time Complexity Analysis
        string timeComplexity = "O(1) - Constant Time";
        string explanation = "PUSH operation on a stack is ALWAYS O(1)!\n\n" +
                            "Why? Because we ALWAYS add to the TOP.\n" +
                            "No shifting or searching required.\n\n" +
                            "Steps performed:\n" +
                            "1. Check if stack is full (1 operation)\n" +
                            "2. Place element at top position (1 operation)\n" +
                            "3. Update top pointer (1 operation)\n\n" +
                            $"Total: 3 operations regardless of stack size ({stackSize} elements)";
        
        // Update displays
        UpdateComplexityDisplay("PUSH", timeComplexity, "O(1)", 3);
        UpdateOperationCounter(1, 0);
        UpdateEducationalExplanation(explanation, GetPushAnalysis(stackSize));
        
        // Log to history
        RecordOperation("Push", 3);
    }
    
    private string GetPushAnalysis(int size)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ STACK PUSH COMPLEXITY ━━━");
        analysis.AppendLine($"Current Stack Size: {size} elements");
        analysis.AppendLine("\n<b>Time Complexity: O(1)</b>");
        analysis.AppendLine("• Best Case: O(1)");
        analysis.AppendLine("• Worst Case: O(1)");
        analysis.AppendLine("• Average Case: O(1)");
        analysis.AppendLine("\n<b>Why Always O(1)?</b>");
        analysis.AppendLine("Stacks use LIFO (Last In, First Out).");
        analysis.AppendLine("We ALWAYS add to the top - no matter the size!");
        analysis.AppendLine("\n<b>Comparison to Arrays:</b>");
        analysis.AppendLine("• Array insert at start: O(n) - shifts needed");
        analysis.AppendLine("• Stack push: O(1) - no shifts ever!");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // POP OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzePopOperation(int stackSizeBefore)
    {
        sessionPops++;
        totalOperations++;
        
        // Time Complexity Analysis
        string timeComplexity = "O(1) - Constant Time";
        string explanation = "POP operation on a stack is ALWAYS O(1)!\n\n" +
                            "Why? Because we ALWAYS remove from the TOP.\n" +
                            "No shifting or searching required.\n\n" +
                            "Steps performed:\n" +
                            "1. Check if stack is empty (1 operation)\n" +
                            "2. Retrieve element at top (1 operation)\n" +
                            "3. Remove top element (1 operation)\n" +
                            "4. Update top pointer (1 operation)\n\n" +
                            $"Total: 4 operations regardless of stack size ({stackSizeBefore} → {stackSizeBefore - 1} elements)";
        
        // Update displays
        UpdateComplexityDisplay("POP", timeComplexity, "O(1)", 4);
        UpdateOperationCounter(1, 0);
        UpdateEducationalExplanation(explanation, GetPopAnalysis(stackSizeBefore));
        
        // Log to history
        RecordOperation("Pop", 4);
    }
    
    private string GetPopAnalysis(int sizeBefore)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ STACK POP COMPLEXITY ━━━");
        analysis.AppendLine($"Stack Size Before Pop: {sizeBefore} elements");
        analysis.AppendLine($"Stack Size After Pop: {sizeBefore - 1} elements");
        analysis.AppendLine("\n<b>Time Complexity: O(1)</b>");
        analysis.AppendLine("• Best Case: O(1)");
        analysis.AppendLine("• Worst Case: O(1)");
        analysis.AppendLine("• Average Case: O(1)");
        analysis.AppendLine("\n<b>Why Always O(1)?</b>");
        analysis.AppendLine("We ALWAYS remove from the top.");
        analysis.AppendLine("No matter if stack has 1 or 1000 elements!");
        analysis.AppendLine("\n<b>Comparison to Arrays:</b>");
        analysis.AppendLine("• Array delete at start: O(n) - shifts needed");
        analysis.AppendLine("• Stack pop: O(1) - just remove top!");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // PEEK OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzePeekOperation(int stackSize)
    {
        sessionPeeks++;
        totalOperations++;
        
        // Time Complexity Analysis
        string timeComplexity = "O(1) - Constant Time";
        string explanation = "PEEK operation on a stack is ALWAYS O(1)!\n\n" +
                            "Why? We just VIEW the top element without removing it.\n" +
                            "No modification needed.\n\n" +
                            "Steps performed:\n" +
                            "1. Check if stack is empty (1 operation)\n" +
                            "2. Return element at top (1 operation)\n\n" +
                            $"Total: 2 operations regardless of stack size ({stackSize} elements)\n\n" +
                            "PEEK is the fastest stack operation!";
        
        // Update displays
        UpdateComplexityDisplay("PEEK", timeComplexity, "O(1)", 2);
        UpdateOperationCounter(1, 0);
        UpdateEducationalExplanation(explanation, GetPeekAnalysis(stackSize));
        
        // Log to history
        RecordOperation("Peek", 2);
    }
    
    private string GetPeekAnalysis(int size)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ STACK PEEK COMPLEXITY ━━━");
        analysis.AppendLine($"Current Stack Size: {size} elements");
        analysis.AppendLine("\n<b>Time Complexity: O(1)</b>");
        analysis.AppendLine("• Best Case: O(1)");
        analysis.AppendLine("• Worst Case: O(1)");
        analysis.AppendLine("• Average Case: O(1)");
        analysis.AppendLine("\n<b>Why Always O(1)?</b>");
        analysis.AppendLine("PEEK just reads the top element.");
        analysis.AppendLine("No removal, no modification - just look!");
        analysis.AppendLine("\n<b>Common Use Cases:</b>");
        analysis.AppendLine("• Check next function to execute");
        analysis.AppendLine("• Preview undo operation");
        analysis.AppendLine("• Validate before popping");
        
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
        
        display.AppendLine("\n<color=#FFD700>⚡ STACK ADVANTAGE ⚡</color>");
        display.AppendLine("<color=#00FF00>ALL operations are O(1)!</color>");
        
        complexityDisplayText.text = display.ToString();
    }
    
    void UpdateOperationCounter(int operations, int comparisons)
    {
        if (operationCounterText == null) return;
        
        StringBuilder counter = new StringBuilder();
        counter.AppendLine("<size=20><b>OPERATION METRICS</b></size>");
        counter.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━</color>");
        
        if (operations > 0)
            counter.AppendLine($"\n<b>Operations:</b> <color=#95E1D3>{operations}</color>");
        
        if (comparisons > 0)
            counter.AppendLine($"<b>Comparisons:</b> <color=#4ECDC4>{comparisons}</color>");
        
        // Session totals
        counter.AppendLine($"\n<size=16><b>SESSION TOTALS:</b></size>");
        counter.AppendLine($"Total Operations: {totalOperations}");
        counter.AppendLine($"Total Pushes: {sessionPushes}");
        counter.AppendLine($"Total Pops: {sessionPops}");
        counter.AppendLine($"Total Peeks: {sessionPeeks}");
        
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
        metrics.AppendLine($"  • Pushes: {sessionPushes}");
        metrics.AppendLine($"  • Pops: {sessionPops}");
        metrics.AppendLine($"  • Peeks: {sessionPeeks}");
        
        int totalOps = sessionPushes + sessionPops + sessionPeeks;
        metrics.AppendLine($"\n<b>Total Operations:</b> {totalOps}");
        
        if (totalOps > 0)
        {
            float avgSteps = (float)totalOperations / totalOps;
            metrics.AppendLine($"<b>Avg Steps/Operation:</b> {avgSteps:F2}");
        }
        
        metrics.AppendLine("\n<b>Efficiency Analysis:</b>");
        metrics.AppendLine("<color=#00FF00>All operations: O(1) ✓</color>");
        metrics.AppendLine("Stack is HIGHLY efficient!");
        
        performanceMetricsText.text = metrics.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // COMPARATIVE ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public string GetComparativeAnalysis(int stackSize)
    {
        StringBuilder comparison = new StringBuilder();
        comparison.AppendLine("<size=22><b>STACK vs ARRAY COMPARISON</b></size>");
        comparison.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>\n");
        
        comparison.AppendLine($"<b>Current Stack Size: {stackSize} elements</b>\n");
        
        comparison.AppendLine("<color=#00FF00><b>STACK Operations (ALL O(1)):</b></color>");
        comparison.AppendLine("  • PUSH: Add to top - 1 step");
        comparison.AppendLine("  • POP: Remove from top - 1 step");
        comparison.AppendLine("  • PEEK: View top - 1 step");
        
        comparison.AppendLine($"\n<color=#FF8C00><b>ARRAY Operations (Variable):</b></color>");
        comparison.AppendLine($"  • Insert at start: O(n) - {stackSize} shifts");
        comparison.AppendLine($"  • Delete from start: O(n) - {stackSize} shifts");
        comparison.AppendLine("  • Access by index: O(1) - direct");
        
        comparison.AppendLine("\n<b>KEY INSIGHTS:</b>");
        comparison.AppendLine("✓ Stack: Perfect for LIFO operations");
        comparison.AppendLine("✓ Stack: No shifting ever needed");
        comparison.AppendLine("✓ Stack: Predictable O(1) performance");
        comparison.AppendLine("✗ Stack: Can only access top element");
        comparison.AppendLine("✗ Array: Flexible but slower for start operations");
        
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
        totalOperations = 0;
        sessionPushes = 0;
        sessionPops = 0;
        sessionPeeks = 0;
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
    
    public int GetTotalOperations() => totalOperations;
    public int GetSessionPushes() => sessionPushes;
    public int GetSessionPops() => sessionPops;
    public int GetSessionPeeks() => sessionPeeks;
    
    public Dictionary<string, int> GetOperationHistory() => new Dictionary<string, int>(operationHistory);
}