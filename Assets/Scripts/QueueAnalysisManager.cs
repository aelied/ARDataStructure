using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

public class QueueAnalysisManager : MonoBehaviour
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
    private int totalEnqueues = 0;
    private int totalDequeues = 0;
    private int totalPeeks = 0;
    private Dictionary<string, int> operationHistory = new Dictionary<string, int>();
    
    private QRQueueModeManager queueManager;
    
    void Start()
    {
        queueManager = FindObjectOfType<QRQueueModeManager>();
        ResetCounters();
    }
    
    // ═══════════════════════════════════════════════════════════
    // ENQUEUE OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeEnqueueOperation(int queueSize)
    {
        totalEnqueues++;
        
        string timeComplexity = "O(1) - Constant Time";
        string explanation = "ENQUEUE adds element at the REAR of the queue.\n\n" +
                           "Steps:\n" +
                           "1. Access REAR pointer (O(1))\n" +
                           "2. Insert new element (O(1))\n" +
                           "3. Update REAR pointer (O(1))\n\n" +
                           $"Current queue size: {queueSize}\n" +
                           "Time complexity remains O(1) regardless of queue size!";
        
        UpdateComplexityDisplay("ENQUEUE", timeComplexity, "O(1)", 3);
        UpdateOperationCounter(1, 0, 0);
        UpdateEducationalExplanation(explanation, GetEnqueueAnalysis());
        
        RecordOperation("Enqueue", 1);
    }
    
    private string GetEnqueueAnalysis()
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ ENQUEUE COMPLEXITY ━━━");
        analysis.AppendLine("• Best Case: O(1)");
        analysis.AppendLine("• Worst Case: O(1)");
        analysis.AppendLine("• Average Case: O(1)");
        analysis.AppendLine("\n━━━ WHY ALWAYS O(1)? ━━━");
        analysis.AppendLine("Queue maintains REAR pointer.");
        analysis.AppendLine("Always insert at REAR in constant time.");
        analysis.AppendLine("No shifting or searching required!");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // DEQUEUE OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeDequeueOperation(int queueSizeBeforeDequeue)
    {
        totalDequeues++;
        
        string timeComplexity = "O(1) - Constant Time";
        string explanation = "DEQUEUE removes element from the FRONT of the queue.\n\n" +
                           "Steps:\n" +
                           "1. Access FRONT pointer (O(1))\n" +
                           "2. Remove element at FRONT (O(1))\n" +
                           "3. Update FRONT pointer (O(1))\n\n" +
                           $"Queue size before dequeue: {queueSizeBeforeDequeue}\n" +
                           "Time complexity is O(1) - FIFO principle!";
        
        UpdateComplexityDisplay("DEQUEUE", timeComplexity, "O(1)", 3);
        UpdateOperationCounter(1, 0, 0);
        UpdateEducationalExplanation(explanation, GetDequeueAnalysis());
        
        RecordOperation("Dequeue", 1);
    }
    
    private string GetDequeueAnalysis()
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ DEQUEUE COMPLEXITY ━━━");
        analysis.AppendLine("• Best Case: O(1)");
        analysis.AppendLine("• Worst Case: O(1)");
        analysis.AppendLine("• Average Case: O(1)");
        analysis.AppendLine("\n━━━ FIFO PRINCIPLE ━━━");
        analysis.AppendLine("First In, First Out!");
        analysis.AppendLine("FRONT pointer always points to next element.");
        analysis.AppendLine("No need to shift remaining elements.");
        analysis.AppendLine("This is the KEY ADVANTAGE of queues!");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // PEEK OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzePeekOperation()
    {
        totalPeeks++;
        
        string timeComplexity = "O(1) - Constant Time";
        string explanation = "PEEK views the FRONT element without removing it.\n\n" +
                           "Steps:\n" +
                           "1. Access FRONT pointer (O(1))\n" +
                           "2. Return element value (O(1))\n\n" +
                           "No modifications made to queue.\n" +
                           "Instant access via FRONT pointer!";
        
        UpdateComplexityDisplay("PEEK", timeComplexity, "O(1)", 2);
        UpdateOperationCounter(0, 0, 1);
        UpdateEducationalExplanation(explanation, GetPeekAnalysis());
        
        RecordOperation("Peek", 1);
    }
    
    private string GetPeekAnalysis()
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ PEEK COMPLEXITY ━━━");
        analysis.AppendLine("• Always O(1) - Constant Time");
        analysis.AppendLine("\n━━━ NON-DESTRUCTIVE ━━━");
        analysis.AppendLine("Peek only READS the FRONT element.");
        analysis.AppendLine("Queue structure remains unchanged.");
        analysis.AppendLine("Useful for checking next item to process!");
        
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
            display.AppendLine($"\n<b>Actual Steps:</b> <color=#FF8C00>{actualSteps}</color>");
        }
        
        complexityDisplayText.text = display.ToString();
    }
    
    void UpdateOperationCounter(int modifications, int comparisons, int accesses)
    {
        if (operationCounterText == null) return;
        
        StringBuilder counter = new StringBuilder();
        counter.AppendLine("<size=20><b>OPERATION METRICS</b></size>");
        counter.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━</color>");
        
        if (modifications > 0)
            counter.AppendLine($"\n<b>Modifications:</b> <color=#FF6B6B>{modifications}</color>");
        
        if (comparisons > 0)
            counter.AppendLine($"<b>Comparisons:</b> <color=#4ECDC4>{comparisons}</color>");
        
        if (accesses > 0)
            counter.AppendLine($"<b>Accesses:</b> <color=#95E1D3>{accesses}</color>");
        
        counter.AppendLine($"\n<size=16><b>SESSION TOTALS:</b></size>");
        counter.AppendLine($"Total Enqueues: {totalEnqueues}");
        counter.AppendLine($"Total Dequeues: {totalDequeues}");
        counter.AppendLine($"Total Peeks: {totalPeeks}");
        
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
        metrics.AppendLine($"  • Enqueues: {totalEnqueues}");
        metrics.AppendLine($"  • Dequeues: {totalDequeues}");
        metrics.AppendLine($"  • Peeks: {totalPeeks}");
        
        int totalOps = totalEnqueues + totalDequeues + totalPeeks;
        metrics.AppendLine($"\n<b>Total Operations:</b> {totalOps}");
        
        if (totalOps > 0)
        {
            metrics.AppendLine($"<b>Avg Time Complexity:</b> O(1)");
            metrics.AppendLine("\n<color=#00FF00>All queue operations are O(1)!</color>");
        }
        
        performanceMetricsText.text = metrics.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // COMPARATIVE ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public string GetComparativeAnalysis()
    {
        StringBuilder comparison = new StringBuilder();
        comparison.AppendLine("<size=22><b>QUEUE VS ARRAY COMPARISON</b></size>");
        comparison.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>\n");
        
        comparison.AppendLine("<color=#00FF00><b>QUEUE ADVANTAGES:</b></color>");
        comparison.AppendLine("  • ENQUEUE: Always O(1)");
        comparison.AppendLine("  • DEQUEUE: Always O(1)");
        comparison.AppendLine("  • PEEK: Always O(1)");
        comparison.AppendLine("  • No shifting needed!");
        
        comparison.AppendLine($"\n<color=#FF8C00><b>ARRAY DISADVANTAGES:</b></color>");
        comparison.AppendLine($"  • Insert at start: O(n)");
        comparison.AppendLine($"  • Delete from start: O(n)");
        comparison.AppendLine($"  • Requires shifting elements");
        
        comparison.AppendLine("\n<b>KEY INSIGHT:</b>");
        comparison.AppendLine("Queues use FRONT and REAR pointers.");
        comparison.AppendLine("This eliminates the need for shifting!");
        comparison.AppendLine("Perfect for FIFO scenarios!");
        
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
        totalEnqueues = 0;
        totalDequeues = 0;
        totalPeeks = 0;
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
    // PUBLIC GETTERS
    // ═══════════════════════════════════════════════════════════
    
    public int GetTotalEnqueues() => totalEnqueues;
    public int GetTotalDequeues() => totalDequeues;
    public int GetTotalPeeks() => totalPeeks;
    public Dictionary<string, int> GetOperationHistory() => new Dictionary<string, int>(operationHistory);
}