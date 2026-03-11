using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

public class TreeAlgorithmAnalysisManager : MonoBehaviour
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
    private int totalInserts = 0;
    private int totalDeletes = 0;
    private int totalSearches = 0;
    private int totalTraversals = 0;
    private Dictionary<string, int> operationHistory = new Dictionary<string, int>();
    
    // Session statistics
    private int sessionInserts = 0;
    private int sessionDeletes = 0;
    private int sessionSearches = 0;
    private int sessionTraversals = 0;
    
    private QRTreeModeManager treeManager;
    
    void Start()
    {
        treeManager = FindObjectOfType<QRTreeModeManager>();
        ResetCounters();
    }
    
    // ═══════════════════════════════════════════════════════════
    // INSERT OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeInsertOperation(int value, int comparisons, int treeSize)
    {
        sessionInserts++;
        totalComparisons += comparisons;
        totalInserts++;
        
        // Time Complexity Analysis
        string timeComplexity;
        string explanation;
        
        int height = treeManager != null ? treeManager.GetTreeHeight() : comparisons;
        
        if (comparisons == 1)
        {
            timeComplexity = "O(1) - Constant Time (BEST CASE)";
            explanation = "Inserting as ROOT node or immediate child.\n" +
                         "Only 1 comparison needed!\n" +
                         $"Steps: {comparisons} comparison";
        }
        else if (IsBalanced(treeSize, height))
        {
            timeComplexity = "O(log n) - Logarithmic Time";
            explanation = $"BALANCED tree insertion for value {value}.\n" +
                         $"Tree height: {height}, Tree size: {treeSize}\n" +
                         $"Path traversed: {comparisons} nodes\n" +
                         $"This is EFFICIENT - height grows slowly with size!";
        }
        else
        {
            timeComplexity = "O(n) - Linear Time (WORST CASE)";
            explanation = $"UNBALANCED tree - inserting value {value}.\n" +
                         $"Height: {height}, Size: {treeSize}\n" +
                         $"Comparisons: {comparisons}\n" +
                         "Tree is degenerating into a linked list!\n" +
                         "Consider rebalancing (AVL or Red-Black tree)";
        }
        
        // Update displays
        UpdateComplexityDisplay("INSERT", timeComplexity, "O(1)", comparisons);
        UpdateOperationCounter(0, comparisons, 0);
        UpdateEducationalExplanation(explanation, GetInsertCaseAnalysis(comparisons, height, treeSize));
        
        // Log to history
        RecordOperation($"Insert {value}", comparisons);
    }
    
    private string GetInsertCaseAnalysis(int comparisons, int height, int size)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ BST INSERT COMPLEXITY ━━━");
        analysis.AppendLine($"• Best Case: O(1) - Insert at root or immediate child");
        analysis.AppendLine($"• Average Case: O(log n) - Balanced tree");
        analysis.AppendLine($"• Worst Case: O(n) - Skewed tree (like linked list)");
        analysis.AppendLine($"\n━━━ YOUR TREE STATUS ━━━");
        analysis.AppendLine($"• Tree Size: {size} nodes");
        analysis.AppendLine($"• Tree Height: {height}");
        analysis.AppendLine($"• Comparisons: {comparisons}");
        
        float balanceFactor = size > 0 ? (float)height / Mathf.Log(size + 1, 2) : 1;
        
        if (balanceFactor <= 1.5f)
        {
            analysis.AppendLine($"• Status: ✓ WELL BALANCED");
        }
        else if (balanceFactor <= 2.5f)
        {
            analysis.AppendLine($"• Status: ⚠ MODERATELY BALANCED");
        }
        else
        {
            analysis.AppendLine($"• Status: ✗ POORLY BALANCED");
        }
        
        return analysis.ToString();
    }
    
    bool IsBalanced(int size, int height)
    {
        if (size <= 1) return true;
        
        // A tree is reasonably balanced if height ≈ log₂(n)
        float idealHeight = Mathf.Log(size + 1, 2);
        float balanceFactor = height / idealHeight;
        
        return balanceFactor <= 2.0f; // Allow some imbalance
    }
    
    // ═══════════════════════════════════════════════════════════
    // DELETE OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeDeleteOperation(int value, int comparisons, int treeSize)
    {
        sessionDeletes++;
        totalComparisons += comparisons;
        totalDeletes++;
        
        // Time Complexity Analysis
        string timeComplexity;
        string explanation;
        
        int height = treeManager != null ? treeManager.GetTreeHeight() : comparisons;
        
        if (comparisons == 1)
        {
            timeComplexity = "O(1) - Constant Time (BEST CASE)";
            explanation = $"Deleting root node (value {value}).\n" +
                         "Only 1 comparison needed to find it!\n" +
                         "Note: Deletion may still require finding successor.";
        }
        else if (IsBalanced(treeSize, height))
        {
            timeComplexity = "O(log n) - Logarithmic Time";
            explanation = $"DELETE in balanced tree for value {value}.\n" +
                         $"Search path: {comparisons} comparisons\n" +
                         $"Tree Height: {height}, Size: {treeSize}\n" +
                         "Efficient deletion!";
        }
        else
        {
            timeComplexity = "O(n) - Linear Time (WORST CASE)";
            explanation = $"DELETE in unbalanced tree - value {value}.\n" +
                         $"Had to traverse {comparisons} nodes\n" +
                         $"Height: {height}, Size: {treeSize}\n" +
                         "Tree is too deep!";
        }
        
        // Update displays
        UpdateComplexityDisplay("DELETE", timeComplexity, "O(1)", comparisons);
        UpdateOperationCounter(0, comparisons, 0);
        UpdateEducationalExplanation(explanation, GetDeleteCaseAnalysis(comparisons, height, treeSize));
        
        // Log to history
        RecordOperation($"Delete {value}", comparisons);
    }
    
    private string GetDeleteCaseAnalysis(int comparisons, int height, int size)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ BST DELETE COMPLEXITY ━━━");
        analysis.AppendLine("Delete has 3 cases:");
        analysis.AppendLine("1. Leaf node - Simple removal");
        analysis.AppendLine("2. One child - Replace with child");
        analysis.AppendLine("3. Two children - Find successor, swap, delete");
        analysis.AppendLine($"\n━━━ CURRENT OPERATION ━━━");
        analysis.AppendLine($"• Comparisons to find: {comparisons}");
        analysis.AppendLine($"• Tree Height: {height}");
        analysis.AppendLine($"• Tree Size: {size}");
        analysis.AppendLine($"\n• Time Complexity: O({(IsBalanced(size, height) ? "log n" : "n")})");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // SEARCH OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeSearchOperation(int value, int comparisons, bool found, int treeSize)
    {
        sessionSearches++;
        totalComparisons += comparisons;
        totalSearches++;
        
        // Time Complexity Analysis
        string timeComplexity;
        string explanation;
        
        int height = treeManager != null ? treeManager.GetTreeHeight() : comparisons;
        
        if (found && comparisons == 1)
        {
            timeComplexity = "O(1) - Constant Time (BEST CASE)";
            explanation = $"BINARY SEARCH TREE - BEST CASE!\n" +
                         $"Found {value} at root in just 1 comparison.\n" +
                         "This is as fast as it gets!";
        }
        else if (found && IsBalanced(treeSize, height))
        {
            timeComplexity = "O(log n) - Logarithmic Time";
            explanation = $"BINARY SEARCH - EFFICIENT!\n" +
                         $"Found {value} in {comparisons} comparisons.\n" +
                         $"Tree Height: {height}, Size: {treeSize}\n" +
                         "Eliminated half the remaining nodes at each step!";
        }
        else if (found)
        {
            timeComplexity = "O(n) - Linear Time (WORST CASE)";
            explanation = $"BINARY SEARCH - UNBALANCED TREE\n" +
                         $"Found {value} after {comparisons} comparisons.\n" +
                         $"Height: {height}, Size: {treeSize}\n" +
                         "Tree is too deep - approaching linked list performance!";
        }
        else
        {
            timeComplexity = "O(log n) or O(n) - Not Found";
            explanation = $"SEARCH UNSUCCESSFUL\n" +
                         $"Value {value} not in tree.\n" +
                         $"Checked {comparisons} nodes.\n" +
                         $"Searched to leaf level.";
        }
        
        // Update displays
        UpdateComplexityDisplay("SEARCH (Binary)", timeComplexity, "O(1)", comparisons);
        UpdateOperationCounter(0, comparisons, 0);
        UpdateEducationalExplanation(explanation, GetSearchAnalysis(comparisons, height, treeSize, found));
        
        // Log to history
        RecordOperation($"Search {value} ({(found ? "found" : "not found")})", comparisons);
    }
    
    private string GetSearchAnalysis(int comparisons, int height, int size, bool found)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ BINARY SEARCH TREE SEARCH ━━━");
        analysis.AppendLine("At each node, compare target with current:");
        analysis.AppendLine("  • If equal → FOUND!");
        analysis.AppendLine("  • If less → Go LEFT");
        analysis.AppendLine("  • If greater → Go RIGHT");
        analysis.AppendLine($"\n━━━ YOUR SEARCH ━━━");
        analysis.AppendLine($"• Comparisons: {comparisons}");
        analysis.AppendLine($"• Tree Height: {height}");
        analysis.AppendLine($"• Tree Size: {size}");
        analysis.AppendLine($"• Result: {(found ? "FOUND ✓" : "NOT FOUND ✗")}");
        analysis.AppendLine($"\n• Best Case: O(1) - Value at root");
        analysis.AppendLine($"• Average (balanced): O(log n)");
        analysis.AppendLine($"• Worst (skewed): O(n)");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // TRAVERSAL OPERATION ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeTraversalOperation(string traversalType, int treeSize)
    {
        sessionTraversals++;
        totalTraversals++;
        
        // Time Complexity Analysis
        string timeComplexity = "O(n) - Linear Time";
        string explanation;
        
        switch (traversalType.ToLower())
        {
            case "inorder":
                explanation = "IN-ORDER TRAVERSAL (Left → Root → Right)\n" +
                             $"Visited all {treeSize} nodes.\n" +
                             "Produces SORTED output for BST!\n" +
                             "Use case: Get sorted list from tree";
                break;
                
            case "preorder":
                explanation = "PRE-ORDER TRAVERSAL (Root → Left → Right)\n" +
                             $"Visited all {treeSize} nodes.\n" +
                             "Root processed FIRST.\n" +
                             "Use case: Create copy of tree, prefix expressions";
                break;
                
            case "postorder":
                explanation = "POST-ORDER TRAVERSAL (Left → Right → Root)\n" +
                             $"Visited all {treeSize} nodes.\n" +
                             "Root processed LAST.\n" +
                             "Use case: Delete tree, postfix expressions";
                break;
                
            case "levelorder":
                explanation = "LEVEL-ORDER TRAVERSAL (Breadth-First)\n" +
                             $"Visited all {treeSize} nodes level by level.\n" +
                             "Uses QUEUE data structure.\n" +
                             "Use case: Level-wise processing, shortest path";
                break;
                
            default:
                explanation = $"{traversalType} traversal completed.\n" +
                             $"Visited all {treeSize} nodes.";
                break;
        }
        
        // Update displays
        UpdateComplexityDisplay($"TRAVERSAL ({traversalType})", timeComplexity, "O(n)", treeSize);
        UpdateOperationCounter(0, 0, treeSize);
        UpdateEducationalExplanation(explanation, GetTraversalAnalysis(traversalType, treeSize));
        
        // Log to history
        RecordOperation($"{traversalType} Traversal", treeSize);
    }
    
    private string GetTraversalAnalysis(string traversalType, int size)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ TREE TRAVERSAL COMPARISON ━━━");
        analysis.AppendLine($"All traversals visit EVERY node: O(n)");
        analysis.AppendLine($"\nFor your tree with {size} nodes:");
        analysis.AppendLine("• In-Order: Sorted output (Left-Root-Right)");
        analysis.AppendLine("• Pre-Order: Root first (Root-Left-Right)");
        analysis.AppendLine("• Post-Order: Root last (Left-Right-Root)");
        analysis.AppendLine("• Level-Order: By levels (BFS)");
        analysis.AppendLine($"\n━━━ SPACE COMPLEXITY ━━━");
        analysis.AppendLine("• Recursive: O(h) - Stack space for height h");
        analysis.AppendLine("• Iterative: O(h) - Explicit stack/queue");
        analysis.AppendLine($"• Your tree height: {(treeManager != null ? treeManager.GetTreeHeight() : 0)}");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // UI UPDATE FUNCTIONS
    // ═══════════════════════════════════════════════════════════
    
    void UpdateComplexityDisplay(string operation, string timeComplexity, string spaceComplexity, int actualSteps)
    {
        if (complexityDisplayText == null) return;
        
        StringBuilder display = new StringBuilder();
        display.AppendLine($"<size=24><b>{operation}</b></size>");
        display.AppendLine($"<color=#FFD700>━━━━━━━━━━━━━━━━━━━━━━</color>");
        display.AppendLine($"\n<b>Time Complexity:</b> <color=#00FF00>{timeComplexity}</color>");
        display.AppendLine($"<b>Space Complexity:</b> <color=#00FFFF>{spaceComplexity}</color>");
        
        if (actualSteps > 0)
        {
            display.AppendLine($"\n<b>Operations:</b> <color=#FF8C00>{actualSteps}</color>");
        }
        
        complexityDisplayText.text = display.ToString();
    }
    
    void UpdateOperationCounter(int shifts, int comparisons, int visits)
    {
        if (operationCounterText == null) return;
        
        StringBuilder counter = new StringBuilder();
        counter.AppendLine("<size=20><b>OPERATION METRICS</b></size>");
        counter.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━</color>");
        
        if (comparisons > 0)
            counter.AppendLine($"\n<b>Comparisons:</b> <color=#4ECDC4>{comparisons}</color>");
        
        if (visits > 0)
            counter.AppendLine($"<b>Nodes Visited:</b> <color=#95E1D3>{visits}</color>");
        
        // Session totals
        counter.AppendLine($"\n<size=16><b>SESSION TOTALS:</b></size>");
        counter.AppendLine($"Total Comparisons: {totalComparisons}");
        counter.AppendLine($"Total Inserts: {totalInserts}");
        counter.AppendLine($"Total Deletes: {totalDeletes}");
        counter.AppendLine($"Total Searches: {totalSearches}");
        
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
        metrics.AppendLine($"  • Searches: {sessionSearches}");
        metrics.AppendLine($"  • Traversals: {sessionTraversals}");
        
        int totalOps = sessionInserts + sessionDeletes + sessionSearches + sessionTraversals;
        metrics.AppendLine($"\n<b>Total Operations:</b> {totalOps}");
        
        if (totalOps > 0)
        {
            float avgComparisons = (float)totalComparisons / totalOps;
            metrics.AppendLine($"<b>Avg Comparisons/Op:</b> {avgComparisons:F2}");
        }
        
        if (treeManager != null)
        {
            int treeSize = treeManager.GetNodeCount();
            int treeHeight = treeManager.GetTreeHeight();
            
            metrics.AppendLine($"\n<b>Tree Statistics:</b>");
            metrics.AppendLine($"  • Size: {treeSize} nodes");
            metrics.AppendLine($"  • Height: {treeHeight}");
            
            if (treeSize > 0)
            {
                float idealHeight = Mathf.Log(treeSize + 1, 2);
                float balanceFactor = treeHeight / idealHeight;
                
                metrics.AppendLine($"  • Balance: {(balanceFactor <= 1.5f ? "Good" : balanceFactor <= 2.5f ? "Fair" : "Poor")}");
            }
        }
        
        performanceMetricsText.text = metrics.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // COMPARATIVE ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public string GetComparativeAnalysis()
    {
        StringBuilder comparison = new StringBuilder();
        comparison.AppendLine("<size=22><b>BST vs ARRAY COMPARISON</b></size>");
        comparison.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>\n");
        
        comparison.AppendLine("<b>Operation Time Complexities:</b>\n");
        
        comparison.AppendLine("<color=#FFD700>SEARCH:</color>");
        comparison.AppendLine("  • Array (unsorted): O(n)");
        comparison.AppendLine("  • Array (sorted): O(log n) - binary search");
        comparison.AppendLine("  • BST (balanced): O(log n) ✓");
        comparison.AppendLine("  • BST (worst): O(n)");
        
        comparison.AppendLine("\n<color=#FFD700>INSERT:</color>");
        comparison.AppendLine("  • Array (end): O(1)");
        comparison.AppendLine("  • Array (middle): O(n) - shifting");
        comparison.AppendLine("  • BST (balanced): O(log n) ✓");
        comparison.AppendLine("  • BST (worst): O(n)");
        
        comparison.AppendLine("\n<color=#FFD700>DELETE:</color>");
        comparison.AppendLine("  • Array: O(n) - shifting");
        comparison.AppendLine("  • BST (balanced): O(log n) ✓");
        comparison.AppendLine("  • BST (worst): O(n)");
        
        comparison.AppendLine("\n<b>KEY INSIGHTS:</b>");
        comparison.AppendLine("• BST excels at dynamic insertion/deletion");
        comparison.AppendLine("• BST maintains sorted order naturally");
        comparison.AppendLine("• Balance is CRITICAL for BST performance");
        comparison.AppendLine("• Arrays better for direct index access");
        
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
        totalInserts = 0;
        totalDeletes = 0;
        totalSearches = 0;
        totalTraversals = 0;
        sessionInserts = 0;
        sessionDeletes = 0;
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
    // PUBLIC GETTERS FOR EXTERNAL USE
    // ═══════════════════════════════════════════════════════════
    
    public int GetTotalComparisons() => totalComparisons;
    public int GetTotalInserts() => totalInserts;
    public int GetTotalDeletes() => totalDeletes;
    public int GetTotalSearches() => totalSearches;
    public int GetTotalTraversals() => totalTraversals;
    public int GetSessionInserts() => sessionInserts;
    public int GetSessionDeletes() => sessionDeletes;
    public int GetSessionSearches() => sessionSearches;
    public int GetSessionTraversals() => sessionTraversals;
    
    public Dictionary<string, int> GetOperationHistory() => new Dictionary<string, int>(operationHistory);
}