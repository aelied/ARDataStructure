using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

public class GraphAlgorithmAnalysisManager : MonoBehaviour
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
    private int totalVertexOperations = 0;
    private int totalEdgeOperations = 0;
    private int totalTraversalSteps = 0;
    private int totalComparisons = 0;
    
    // Session statistics
    private int sessionAddNode = 0;
    private int sessionRemoveNode = 0;
    private int sessionAddEdge = 0;
    private int sessionBFS = 0;
    private int sessionDFS = 0;
    private int sessionDijkstra = 0;
    
    private Dictionary<string, int> operationHistory = new Dictionary<string, int>();
    
    void Start()
    {
        ResetCounters();
    }
    
    // ═══════════════════════════════════════════════════════════
    // ADD NODE ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeAddNode(int currentNodeCount)
    {
        sessionAddNode++;
        totalVertexOperations++;
        
        string timeComplexity = "O(1) - Constant Time";
        string explanation = "Adding a node to a graph is CONSTANT time!\n\n" +
                            "Operations:\n" +
                            "• Create new vertex object\n" +
                            "• Add to node list\n" +
                            "• Initialize empty edge lists\n\n" +
                            $"Total nodes now: {currentNodeCount}\n" +
                            "Steps: 1 operation";
        
        UpdateComplexityDisplay("ADD NODE", timeComplexity, "O(1)", 1);
        UpdateOperationCounter(1, 0, 0, 0);
        UpdateEducationalExplanation(explanation, GetAddNodeAnalysis());
        
        RecordOperation("Add Node", 1);
    }
    
    private string GetAddNodeAnalysis()
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ WHY O(1)? ━━━");
        analysis.AppendLine("Adding a vertex is simple:");
        analysis.AppendLine("• No searching required");
        analysis.AppendLine("• No shifting of existing data");
        analysis.AppendLine("• Just append to vertex list");
        analysis.AppendLine("\nThis is true regardless of graph size!");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // REMOVE NODE ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeRemoveNode(int remainingNodes, int edgesRemoved)
    {
        sessionRemoveNode++;
        totalVertexOperations++;
        totalEdgeOperations += edgesRemoved;
        
        int totalOps = 1 + edgesRemoved;
        
        string timeComplexity = "O(V + E) - Linear Time";
        string explanation = $"Removing a node requires checking connections!\n\n" +
                            "Operations:\n" +
                            $"• Remove vertex: 1 operation\n" +
                            $"• Delete {edgesRemoved} connected edges\n" +
                            "• Update adjacency information\n\n" +
                            $"Remaining nodes: {remainingNodes}\n" +
                            $"Total steps: {totalOps}";
        
        UpdateComplexityDisplay("REMOVE NODE", timeComplexity, "O(1)", totalOps);
        UpdateOperationCounter(totalOps, 0, 0, edgesRemoved);
        UpdateEducationalExplanation(explanation, GetRemoveNodeAnalysis(edgesRemoved));
        
        RecordOperation($"Remove Node ({edgesRemoved} edges)", totalOps);
    }
    
    private string GetRemoveNodeAnalysis(int edgesRemoved)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ COMPLEXITY BREAKDOWN ━━━");
        analysis.AppendLine("Removing a vertex is more complex:");
        analysis.AppendLine($"• Must delete ALL {edgesRemoved} incident edges");
        analysis.AppendLine("• Update adjacency lists of neighbors");
        analysis.AppendLine("• Remove from vertex set");
        analysis.AppendLine("\nWorst case: O(V + E)");
        analysis.AppendLine("When node has many connections!");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // ADD EDGE ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeAddEdge(int nodeCount, int edgeCount)
    {
        sessionAddEdge++;
        totalEdgeOperations++;
        
        string timeComplexity = "O(1) - Constant Time";
        string explanation = "Adding an edge is CONSTANT time!\n\n" +
                            "Operations:\n" +
                            "• Create edge object\n" +
                            "• Add to source's adjacency list\n" +
                            "• Add to destination's adjacency list\n" +
                            "• (For undirected: add reverse too)\n\n" +
                            $"Total edges now: {edgeCount}\n" +
                            "Steps: 1-2 operations";
        
        UpdateComplexityDisplay("ADD EDGE", timeComplexity, "O(1)", 1);
        UpdateOperationCounter(1, 0, 0, 1);
        UpdateEducationalExplanation(explanation, GetAddEdgeAnalysis());
        
        RecordOperation("Add Edge", 1);
    }
    
    private string GetAddEdgeAnalysis()
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ ADJACENCY LIST ADVANTAGE ━━━");
        analysis.AppendLine("Using adjacency lists makes this fast:");
        analysis.AppendLine("• No matrix to resize");
        analysis.AppendLine("• Just append to list");
        analysis.AppendLine("• O(1) average case");
        analysis.AppendLine("\nAdjacency Matrix would be O(1) too,");
        analysis.AppendLine("but uses O(V²) space!");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // BFS ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeBFS(int nodeCount, int edgeCount, int visitedCount)
    {
        sessionBFS++;
        int steps = visitedCount + edgeCount;
        totalTraversalSteps += steps;
        
        string timeComplexity = "O(V + E) - Linear Time";
        string explanation = "BREADTH-FIRST SEARCH Algorithm:\n\n" +
                            "Process:\n" +
                            "• Use QUEUE (FIFO) for traversal\n" +
                            "• Visit all neighbors before going deeper\n" +
                            $"• Visited {visitedCount} nodes\n" +
                            $"• Explored {edgeCount} edges\n\n" +
                            "Time: O(V + E)\n" +
                            "Space: O(V) for queue\n" +
                            $"Total steps: {steps}";
        
        UpdateComplexityDisplay("BFS TRAVERSAL", timeComplexity, "O(V)", steps);
        UpdateOperationCounter(0, visitedCount, steps, 0);
        UpdateEducationalExplanation(explanation, GetBFSAnalysis(nodeCount, edgeCount));
        
        RecordOperation("BFS Traversal", steps);
    }
    
    private string GetBFSAnalysis(int V, int E)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ BFS CHARACTERISTICS ━━━");
        analysis.AppendLine("Uses: QUEUE (FIFO)");
        analysis.AppendLine($"• Visit ALL {V} vertices: O(V)");
        analysis.AppendLine($"• Check ALL {E} edges: O(E)");
        analysis.AppendLine("• Total: O(V + E)");
        analysis.AppendLine("\nBest for:");
        analysis.AppendLine("• Shortest path (unweighted)");
        analysis.AppendLine("• Level-order traversal");
        analysis.AppendLine("• Finding connected components");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // DFS ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeDFS(int nodeCount, int edgeCount, int visitedCount)
    {
        sessionDFS++;
        int steps = visitedCount + edgeCount;
        totalTraversalSteps += steps;
        
        string timeComplexity = "O(V + E) - Linear Time";
        string explanation = "DEPTH-FIRST SEARCH Algorithm:\n\n" +
                            "Process:\n" +
                            "• Use STACK (LIFO) / Recursion\n" +
                            "• Go as deep as possible first\n" +
                            $"• Visited {visitedCount} nodes\n" +
                            $"• Explored {edgeCount} edges\n\n" +
                            "Time: O(V + E)\n" +
                            "Space: O(V) for stack\n" +
                            $"Total steps: {steps}";
        
        UpdateComplexityDisplay("DFS TRAVERSAL", timeComplexity, "O(V)", steps);
        UpdateOperationCounter(0, visitedCount, steps, 0);
        UpdateEducationalExplanation(explanation, GetDFSAnalysis(nodeCount, edgeCount));
        
        RecordOperation("DFS Traversal", steps);
    }
    
    private string GetDFSAnalysis(int V, int E)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ DFS CHARACTERISTICS ━━━");
        analysis.AppendLine("Uses: STACK (LIFO) / Recursion");
        analysis.AppendLine($"• Visit ALL {V} vertices: O(V)");
        analysis.AppendLine($"• Check ALL {E} edges: O(E)");
        analysis.AppendLine("• Total: O(V + E)");
        analysis.AppendLine("\nBest for:");
        analysis.AppendLine("• Detecting cycles");
        analysis.AppendLine("• Topological sorting");
        analysis.AppendLine("• Finding strongly connected components");
        
        return analysis.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // DIJKSTRA'S ALGORITHM ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public void AnalyzeDijkstra(int nodeCount, int edgeCount, int pathLength, float pathDistance)
    {
        sessionDijkstra++;
        
        // With binary heap: O((V + E) log V)
        int theoreticalOps = (int)((nodeCount + edgeCount) * Mathf.Log(nodeCount, 2));
        totalComparisons += theoreticalOps;
        
        string timeComplexity = "O((V + E) log V) - Logarithmic";
        string explanation = "DIJKSTRA'S SHORTEST PATH Algorithm:\n\n" +
                            "Process:\n" +
                            "• Use PRIORITY QUEUE (min-heap)\n" +
                            "• Greedy: always pick closest vertex\n" +
                            $"• Path length: {pathLength} nodes\n" +
                            $"• Total distance: {pathDistance:F2}\n\n" +
                            "Time: O((V + E) log V) with heap\n" +
                            "Space: O(V)\n" +
                            $"Theoretical operations: ~{theoreticalOps}";
        
        UpdateComplexityDisplay("DIJKSTRA", timeComplexity, "O(V)", theoreticalOps);
        UpdateOperationCounter(0, pathLength, theoreticalOps, 0);
        UpdateEducationalExplanation(explanation, GetDijkstraAnalysis(nodeCount, edgeCount));
        
        RecordOperation("Dijkstra", theoreticalOps);
    }
    
    private string GetDijkstraAnalysis(int V, int E)
    {
        StringBuilder analysis = new StringBuilder();
        analysis.AppendLine("\n━━━ DIJKSTRA COMPLEXITY ━━━");
        analysis.AppendLine("Implementation affects complexity:");
        analysis.AppendLine($"• Simple array: O(V²)");
        analysis.AppendLine($"• Binary heap: O((V + E) log V)");
        analysis.AppendLine($"• Fibonacci heap: O(E + V log V)");
        analysis.AppendLine($"\nYour graph: V={V}, E={E}");
        analysis.AppendLine($"Binary heap: ~{(int)((V + E) * Mathf.Log(V, 2))} ops");
        analysis.AppendLine("\nRequirements:");
        analysis.AppendLine("• Non-negative edge weights");
        analysis.AppendLine("• Works on directed/undirected");
        
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
            display.AppendLine($"\n<b>Steps Taken:</b> <color=#FF8C00>{actualSteps}</color>");
        }
        
        complexityDisplayText.text = display.ToString();
    }
    
    void UpdateOperationCounter(int nodeOps, int visited, int steps, int edgeOps)
    {
        if (operationCounterText == null) return;
        
        StringBuilder counter = new StringBuilder();
        counter.AppendLine("<size=20><b>OPERATION METRICS</b></size>");
        counter.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━</color>");
        
        if (nodeOps > 0)
            counter.AppendLine($"\n<b>Node Operations:</b> <color=#4ECDC4>{nodeOps}</color>");
        
        if (edgeOps > 0)
            counter.AppendLine($"<b>Edge Operations:</b> <color=#95E1D3>{edgeOps}</color>");
        
        if (visited > 0)
            counter.AppendLine($"<b>Nodes Visited:</b> <color=#F38181>{visited}</color>");
        
        if (steps > 0)
            counter.AppendLine($"<b>Total Steps:</b> <color=#FFB6C1>{steps}</color>");
        
        // Session totals
        counter.AppendLine($"\n<size=16><b>SESSION TOTALS:</b></size>");
        counter.AppendLine($"Vertex Ops: {totalVertexOperations}");
        counter.AppendLine($"Edge Ops: {totalEdgeOperations}");
        counter.AppendLine($"Traversal Steps: {totalTraversalSteps}");
        
        operationCounterText.text = counter.ToString();
    }
    
    void UpdateEducationalExplanation(string mainExplanation, string detailedAnalysis)
    {
        if (educationalExplanationText == null) return;
        
        StringBuilder explanation = new StringBuilder();
        explanation.AppendLine("<size=18><b>ALGORITHM EXPLANATION</b></size>");
        explanation.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>\n");
        explanation.AppendLine(mainExplanation);
        explanation.AppendLine(detailedAnalysis);
        
        educationalExplanationText.text = explanation.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // PERFORMANCE METRICS
    // ═══════════════════════════════════════════════════════════
    
    public void UpdatePerformanceMetrics()
    {
        if (performanceMetricsText == null) return;
        
        StringBuilder metrics = new StringBuilder();
        metrics.AppendLine("<size=20><b>SESSION STATISTICS</b></size>");
        metrics.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━━━━</color>\n");
        
        metrics.AppendLine($"<b>Graph Operations:</b>");
        metrics.AppendLine($"  • Nodes Added: {sessionAddNode}");
        metrics.AppendLine($"  • Nodes Removed: {sessionRemoveNode}");
        metrics.AppendLine($"  • Edges Added: {sessionAddEdge}");
        
        metrics.AppendLine($"\n<b>Algorithm Executions:</b>");
        metrics.AppendLine($"  • BFS: {sessionBFS}");
        metrics.AppendLine($"  • DFS: {sessionDFS}");
        metrics.AppendLine($"  • Dijkstra: {sessionDijkstra}");
        
        int totalOps = sessionAddNode + sessionRemoveNode + sessionAddEdge + 
                       sessionBFS + sessionDFS + sessionDijkstra;
        metrics.AppendLine($"\n<b>Total Operations:</b> {totalOps}");
        
        performanceMetricsText.text = metrics.ToString();
    }
    
    // ═══════════════════════════════════════════════════════════
    // COMPARATIVE ANALYSIS
    // ═══════════════════════════════════════════════════════════
    
    public string GetGraphComparison(int V, int E)
    {
        StringBuilder comparison = new StringBuilder();
        comparison.AppendLine("<size=22><b>GRAPH ALGORITHM COMPARISON</b></size>");
        comparison.AppendLine("<color=#FFD700>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</color>\n");
        
        comparison.AppendLine($"<b>For graph with V={V} vertices, E={E} edges:</b>\n");
        
        comparison.AppendLine("<color=#00FF00><b>O(1) Operations:</b></color>");
        comparison.AppendLine("  • Add vertex");
        comparison.AppendLine("  • Add edge");
        
        comparison.AppendLine($"\n<color=#FFD700><b>O(V + E) Operations:</b></color>");
        comparison.AppendLine("  • Remove vertex");
        comparison.AppendLine("  • BFS traversal");
        comparison.AppendLine("  • DFS traversal");
        
        comparison.AppendLine($"\n<color=#FF8C00><b>O((V+E) log V) Operations:</b></color>");
        comparison.AppendLine("  • Dijkstra's shortest path");
        
        comparison.AppendLine("\n<b>KEY INSIGHTS:</b>");
        comparison.AppendLine("• Adjacency list: Space O(V + E)");
        comparison.AppendLine("• Adjacency matrix: Space O(V²)");
        comparison.AppendLine("• Dense graphs: E ≈ V²");
        comparison.AppendLine("• Sparse graphs: E ≈ V");
        
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
        totalVertexOperations = 0;
        totalEdgeOperations = 0;
        totalTraversalSteps = 0;
        totalComparisons = 0;
        sessionAddNode = 0;
        sessionRemoveNode = 0;
        sessionAddEdge = 0;
        sessionBFS = 0;
        sessionDFS = 0;
        sessionDijkstra = 0;
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
    
    public int GetTotalVertexOps() => totalVertexOperations;
    public int GetTotalEdgeOps() => totalEdgeOperations;
    public int GetTotalTraversalSteps() => totalTraversalSteps;
    public int GetSessionBFS() => sessionBFS;
    public int GetSessionDFS() => sessionDFS;
    public int GetSessionDijkstra() => sessionDijkstra;
    
    public Dictionary<string, int> GetOperationHistory() => new Dictionary<string, int>(operationHistory);
}