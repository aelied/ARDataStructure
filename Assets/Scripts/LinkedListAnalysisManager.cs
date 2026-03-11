using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

public class LinkedListAnalysisManager : MonoBehaviour
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
    private int totalTraversals = 0;
    private int totalComparisons = 0;
    private int totalPointerChanges = 0;
    private Dictionary<string, int> operationHistory = new Dictionary<string, int>();
    
    // Session statistics
    private int sessionInserts = 0;
    private int sessionDeletes = 0;
    private int sessionAccesses = 0;
    private int sessionSearches = 0;
    private int sessionTraversals = 0;
    
    private QRLinkedListManager listManager;
    
    void Start()
    {
        listManager = FindObjectOfType<QRLinkedListManager>();
        ResetCounters();
    }
    
    // ═══════════════════════════════════════════════════════════
    // INSERT OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeInsertOperation(int insertPosition, int listSize)
    {
        sessionInserts++;
        
        // Calculate complexity metrics
        int traversalsRequired = insertPosition;
        int pointerChanges = (insertPosition == 0) ? 1 : 2; // Head: 1 change, Others: 2 changes
        
        totalTraversals += traversalsRequired;
        totalPointerChanges += pointerChanges;
        
        // Time Complexity Analysis
        string timeComplexity;
        string explanation;
        
        if (insertPosition == 0)
        {
            // Insert at head (best case)
            timeComplexity = "O(1) - Constant Time (BEST CASE)";
            explanation = "Inserting at HEAD requires NO traversal!\n" +
                         "Steps:\n" +
                         "1. Create new node\n" +
                         "2. Point new node's next to current head\n" +
                         "3. Update head pointer to new node\n\n" +
                         "Total operations: 3 (constant, regardless of list size)\n" +
                         "Pointer changes: 1";
        }
        else if (insertPosition == listSize)
        {
            // Insert at tail (worst case)
            timeComplexity = "O(n) - Linear Time (WORST CASE)";
            explanation = $"Inserting at TAIL requires traversing ALL {listSize} nodes!\n" +
                         "Steps:\n" +
                         $"1. Start at head\n" +
                         $"2. Traverse {listSize} nodes to find tail\n" +
                         "3. Update tail's next pointer\n" +
                         "4. Update new node's next to NULL\n\n" +
                         $"Total operations: {listSize + 2}\n" +
                         "Pointer changes: 2";
        }
        else
        {
            // Insert at middle position
            timeComplexity = "O(n) - Linear Time";
            explanation = $"Inserting at position [{insertPosition}] requires traversing {insertPosition} nodes.\n" +
                         "Steps:\n" +
                         $"1. Start at head\n" +
                         $"2. Traverse {insertPosition} nodes\n" +
                         "3. Update previous node's next pointer\n" +
                         "4. Point new node's next to next node\n\n" +
                         $"Total operations: {insertPosition + 2}\n" +
                         "Pointer changes: 2";
        }
        
        // Update displays
        UpdateComplexityDisplay("INSERT", timeComplexity, "O(1)", traversalsRequired + pointerChanges);
        UpdateOperationCounter(traversalsRequired, 0, pointerChanges);
        UpdateEducationalExplanation(explanation, GetInsertCaseAnalysis(insertPosition, listSize));
        
        // Log to history
        RecordOperation($"Insert at position {insertPosition}", traversalsRequired + pointerChanges);
    }
    
    private string GetInsertCaseAnalysis(int position, int size)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ COMPLEXITY BREAKDOWN ━━━");
        analysis.AppendLine("• Best Case: O(1) - Insert at head (position = 0)");
        analysis.AppendLine($"• Worst Case: O(n) - Insert at tail (position = {size})");
        analysis.AppendLine("• Average Case: O(n/2) ≈ O(n) - Insert in middle");
        analysis.AppendLine($"• Your Case: {(position == 0 ? "BEST" : position == size ? "WORST" : "AVERAGE")}");
        
        analysis.AppendLine("\n━━━ KEY INSIGHT ━━━");
        analysis.AppendLine("Unlike arrays, inserting at HEAD is O(1)!");
        analysis.AppendLine("But we can't directly access positions - must traverse.");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // DELETE OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeDeleteOperation(int deletePosition, int listSize)
    {
        sessionDeletes++;
        
        // Calculate complexity metrics
        int traversalsRequired = (deletePosition == 0) ? 0 : deletePosition;
        int pointerChanges = 1; // Always change one pointer (or head)
        
        totalTraversals += traversalsRequired;
        totalPointerChanges += pointerChanges;
        
        // Time Complexity Analysis
        string timeComplexity;
        string explanation;
        
        if (deletePosition == 0)
        {
            // Delete head (best case)
            timeComplexity = "O(1) - Constant Time (BEST CASE)";
            explanation = "Deleting HEAD requires NO traversal!\n" +
                         "Steps:\n" +
                         "1. Store head's next pointer\n" +
                         "2. Update head to point to next node\n" +
                         "3. Delete old head\n\n" +
                         "Total operations: 3 (constant)\n" +
                         "Pointer changes: 1";
        }
        else if (deletePosition == listSize - 1)
        {
            // Delete tail (worst case)
            timeComplexity = "O(n) - Linear Time (WORST CASE)";
            explanation = $"Deleting TAIL requires traversing {listSize - 1} nodes!\n" +
                         "Steps:\n" +
                         "1. Start at head\n" +
                         $"2. Traverse {listSize - 1} nodes to find node before tail\n" +
                         "3. Update that node's next to NULL\n" +
                         "4. Delete tail node\n\n" +
                         $"Total operations: {listSize + 1}\n" +
                         "Pointer changes: 1";
        }
        else
        {
            // Delete from middle
            timeComplexity = "O(n) - Linear Time";
            explanation = $"Deleting at position [{deletePosition}] requires traversing {deletePosition} nodes.\n" +
                         "Steps:\n" +
                         "1. Start at head\n" +
                         $"2. Traverse {deletePosition} nodes\n" +
                         "3. Update previous node's next pointer to skip deleted node\n" +
                         "4. Delete node\n\n" +
                         $"Total operations: {deletePosition + 2}\n" +
                         "Pointer changes: 1";
        }
        
        // Update displays
        UpdateComplexityDisplay("DELETE", timeComplexity, "O(1)", traversalsRequired + pointerChanges);
        UpdateOperationCounter(traversalsRequired, 0, pointerChanges);
        UpdateEducationalExplanation(explanation, GetDeleteCaseAnalysis(deletePosition, listSize));
        
        // Log to history
        RecordOperation($"Delete at position {deletePosition}", traversalsRequired + pointerChanges);
    }
    
    private string GetDeleteCaseAnalysis(int position, int size)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ COMPLEXITY BREAKDOWN ━━━");
        analysis.AppendLine("• Best Case: O(1) - Delete head (position = 0)");
        analysis.AppendLine($"• Worst Case: O(n) - Delete tail (position = {size - 1})");
        analysis.AppendLine("• Average Case: O(n/2) ≈ O(n) - Delete from middle");
        analysis.AppendLine($"• Your Case: {(position == 0 ? "BEST" : position == size - 1 ? "WORST" : "AVERAGE")}");
        
        analysis.AppendLine("\n━━━ ADVANTAGE OVER ARRAYS ━━━");
        analysis.AppendLine("NO shifting required! Just change pointers.");
        analysis.AppendLine("But we must traverse to find the position.");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // ACCESS OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeAccessOperation(int accessPosition, int listSize)
    {
        sessionAccesses++;
        totalTraversals += accessPosition;
        
        // Time Complexity Analysis
        string timeComplexity;
        string explanation;
        
        if (accessPosition == 0)
        {
            timeComplexity = "O(1) - Constant Time (BEST CASE)";
            explanation = "Accessing HEAD is instant!\n" +
                         "We always have a direct pointer to the head.\n\n" +
                         "Steps: 1 operation (direct access)\n" +
                         "No traversal needed!";
        }
        else if (accessPosition == listSize - 1)
        {
            timeComplexity = "O(n) - Linear Time (WORST CASE)";
            explanation = $"Accessing TAIL requires traversing ALL {listSize} nodes!\n" +
                         "We must follow pointers from head to tail.\n\n" +
                         $"Steps: {listSize} traversals\n" +
                         "This is the DISADVANTAGE of linked lists!";
        }
        else
        {
            timeComplexity = "O(n) - Linear Time";
            explanation = $"Accessing position [{accessPosition}] requires traversing {accessPosition} nodes.\n" +
                         "We must follow the 'next' pointers sequentially.\n\n" +
                         $"Steps: {accessPosition} traversals\n" +
                         "Unlike arrays, NO direct access!";
        }
        
        // Update displays
        UpdateComplexityDisplay("ACCESS", timeComplexity, "O(1)", accessPosition);
        UpdateOperationCounter(accessPosition, 0, 0);
        UpdateEducationalExplanation(explanation, GetAccessAnalysis(accessPosition, listSize));
        
        // Log to history
        RecordOperation($"Access at position {accessPosition}", accessPosition + 1);
    }
    
    private string GetAccessAnalysis(int position, int size)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ WHY O(n) ACCESS? ━━━");
        analysis.AppendLine("Linked lists store nodes in NON-CONTIGUOUS memory.");
        analysis.AppendLine("We can only reach nodes by following pointers:");
        analysis.AppendLine("  HEAD → Node1 → Node2 → ... → Target");
        
        analysis.AppendLine("\n━━━ COMPARISON WITH ARRAYS ━━━");
        analysis.AppendLine("• Arrays: O(1) access - direct memory calculation");
        analysis.AppendLine($"• Linked Lists: O(n) access - must traverse {position} nodes");
        analysis.AppendLine("\nThis is the main TRADEOFF of linked lists!");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // SEARCH OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeSearchOperation(int listSize, bool found, int foundAtPosition = -1)
    {
        sessionSearches++;
        
        int comparisons = found ? foundAtPosition + 1 : listSize;
        totalComparisons += comparisons;
        totalTraversals += comparisons;
        
        // Time Complexity Analysis
        string timeComplexity = "O(n) - Linear Time";
        string explanation;
        
        if (found)
        {
            if (foundAtPosition == 0)
            {
                explanation = "LINEAR SEARCH - BEST CASE!\n" +
                             "Found element at HEAD (position 0).\n" +
                             "Comparisons: 1\n" +
                             "This is lucky - O(1) in best case!";
            }
            else if (foundAtPosition == listSize - 1)
            {
                explanation = "LINEAR SEARCH - WORST CASE!\n" +
                             $"Found element at TAIL (position {listSize - 1}).\n" +
                             $"Comparisons: {comparisons}\n" +
                             "Had to traverse entire list - O(n)";
            }
            else
            {
                explanation = $"LINEAR SEARCH - AVERAGE CASE\n" +
                             $"Found element at position {foundAtPosition}.\n" +
                             $"Comparisons: {comparisons}\n" +
                             $"Average: O(n/2) ≈ O(n)";
            }
        }
        else
        {
            explanation = "LINEAR SEARCH - NOT FOUND\n" +
                         $"Checked ALL {listSize} nodes.\n" +
                         $"Comparisons: {comparisons}\n" +
                         "This is WORST CASE - O(n)";
        }
        
        // Update displays
        UpdateComplexityDisplay("SEARCH (Linear)", timeComplexity, "O(1)", comparisons);
        UpdateOperationCounter(comparisons, comparisons, 0);
        UpdateEducationalExplanation(explanation, GetSearchAnalysis(listSize));
        
        // Log to history
        RecordOperation($"Linear Search ({(found ? "found" : "not found")})", comparisons);
    }
    
    private string GetSearchAnalysis(int size)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ LINKED LIST SEARCH ━━━");
        analysis.AppendLine($"List Size: {size} nodes");
        analysis.AppendLine("• Best Case: O(1) - Found at head");
        analysis.AppendLine($"• Worst Case: O(n) - Found at tail or not found");
        analysis.AppendLine("• Average Case: O(n/2) ≈ O(n)");
        
        analysis.AppendLine("\n━━━ NO SHORTCUTS! ━━━");
        analysis.AppendLine("Unlike arrays, we CANNOT use:");
        analysis.AppendLine("• Binary Search (no random access)");
        analysis.AppendLine("• Jump Search (no index calculation)");
        analysis.AppendLine("We MUST traverse node by node!");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // TRAVERSAL OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeTraversalOperation(int listSize)
    {
        sessionTraversals++;
        totalTraversals += listSize;
        
        string timeComplexity = "O(n) - Linear Time";
        string explanation = $"TRAVERSAL requires visiting ALL {listSize} nodes.\n\n" +
                            "Steps:\n" +
                            "1. Start at head\n" +
                            "2. Follow next pointer to each node\n" +
                            "3. Continue until next is NULL\n\n" +
                            $"Total operations: {listSize} node visits\n\n" +
                            "This is fundamental to linked lists - we MUST\n" +
                            "traverse to perform most operations!";
        
        // Update displays
        UpdateComplexityDisplay("TRAVERSAL", timeComplexity, "O(1)", listSize);
        UpdateOperationCounter(listSize, 0, 0);
        UpdateEducationalExplanation(explanation, GetTraversalAnalysis(listSize));
        
        // Log to history
        RecordOperation("Traversal", listSize);
    }
    
    private string GetTraversalAnalysis(int size)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ TRAVERSAL COMPLEXITY ━━━");
        analysis.AppendLine("Always O(n) - must visit every node");
        analysis.AppendLine($"For your list: {size} node visits required");
        
        analysis.AppendLine("\n━━━ WHY IS TRAVERSAL IMPORTANT? ━━━");
        analysis.AppendLine("Many operations require traversal:");
        analysis.AppendLine("• Inserting at tail: O(n) traversal");
        analysis.AppendLine("• Deleting at tail: O(n) traversal");
        analysis.AppendLine("• Accessing any position: O(n) traversal");
        analysis.AppendLine("• Searching: O(n) traversal");
        
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
            display.AppendLine($"\n<b>Steps Taken:</b> <color=#FF8C00>{actualSteps}</color>");
        }
        
        complexityDisplayText.text = display.ToString();
    }
    
    void UpdateOperationCounter(int traversals, int comparisons, int pointerChanges)
    {
        if (operationCounterText == null) return;
        
        StringBuilder counter = new StringBuilder();
        counter.AppendLine("<size=20><b>OPERATION METRICS</b></size>");
        counter.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━</color>");
        
        if (traversals > 0)
            counter.AppendLine($"\n<b>Traversals:</b> <color=#4ECDC4>{traversals}</color>");
        
        if (comparisons > 0)
            counter.AppendLine($"<b>Comparisons:</b> <color=#95E1D3>{comparisons}</color>");
        
        if (pointerChanges > 0)
            counter.AppendLine($"<b>Pointer Changes:</b> <color=#FF6B6B>{pointerChanges}</color>");
        
        // Session totals
        counter.AppendLine($"\n<size=16><b>SESSION TOTALS:</b></size>");
        counter.AppendLine($"Total Traversals: {totalTraversals}");
        counter.AppendLine($"Total Comparisons: {totalComparisons}");
        counter.AppendLine($"Total Pointer Changes: {totalPointerChanges}");
        
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
        metrics.AppendLine($"  • Traversals: {sessionTraversals}");
        
        int totalOps = sessionInserts + sessionDeletes + sessionAccesses + sessionSearches + sessionTraversals;
        metrics.AppendLine($"\n<b>Total Operations:</b> {totalOps}");
        
        if (totalOps > 0)
        {
            float avgComplexity = (float)(totalTraversals + totalComparisons) / totalOps;
            metrics.AppendLine($"<b>Avg Steps/Operation:</b> {avgComplexity:F2}");
        }
        
        metrics.AppendLine("\n<b>LINKED LIST CHARACTERISTICS:</b>");
        metrics.AppendLine("• Head operations: O(1) - FAST");
        metrics.AppendLine("• Tail operations: O(n) - SLOW");
        metrics.AppendLine("• No shifting needed (unlike arrays)");
        metrics.AppendLine("• No direct access (unlike arrays)");
        
        performanceMetricsText.text = metrics.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // COMPARATIVE ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public string GetComparativeAnalysis(int listSize)
    {
        StringBuilder comparison = new StringBuilder();
        comparison.AppendLine("<size=22><b>LINKED LIST vs ARRAY</b></size>");
        comparison.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>\n");
        
        comparison.AppendLine($"<b>For size {listSize}:</b>\n");
        
        comparison.AppendLine("<color=#00FF00><b>LINKED LIST ADVANTAGES:</b></color>");
        comparison.AppendLine("  • Insert at head: O(1) vs Array O(n)");
        comparison.AppendLine("  • Delete from head: O(1) vs Array O(n)");
        comparison.AppendLine("  • Dynamic size - no reallocation");
        comparison.AppendLine("  • No shifting required");
        
        comparison.AppendLine($"\n<color=#FF8C00><b>LINKED LIST DISADVANTAGES:</b></color>");
        comparison.AppendLine($"  • Access element: O(n) vs Array O(1)");
        comparison.AppendLine("  • Extra memory for pointers");
        comparison.AppendLine("  • No random access");
        comparison.AppendLine("  • Cache unfriendly (non-contiguous)");
        
        comparison.AppendLine("\n<b>WHEN TO USE LINKED LISTS:</b>");
        comparison.AppendLine("✓ Frequent insertions/deletions at head");
        comparison.AppendLine("✓ Unknown or changing size");
        comparison.AppendLine("✓ Don't need random access");
        comparison.AppendLine("✗ Need fast random access");
        comparison.AppendLine("✗ Memory is critical");
        
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
        totalTraversals = 0;
        totalComparisons = 0;
        totalPointerChanges = 0;
        sessionInserts = 0;
        sessionDeletes = 0;
        sessionAccesses = 0;
        sessionSearches = 0;
        sessionTraversals = 0;
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
    
    public int GetTotalTraversals() => totalTraversals;
    public int GetTotalComparisons() => totalComparisons;
    public int GetTotalPointerChanges() => totalPointerChanges;
    public int GetSessionInserts() => sessionInserts;
    public int GetSessionDeletes() => sessionDeletes;
    public int GetSessionAccesses() => sessionAccesses;
    public int GetSessionSearches() => sessionSearches;
    public int GetSessionTraversals() => sessionTraversals;
    
    public Dictionary<string, int> GetOperationHistory() => new Dictionary<string, int>(operationHistory);
}