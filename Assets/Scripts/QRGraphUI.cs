using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QRGraphUI : MonoBehaviour
{
    [Header("References")]
    public QRGraphManager graphManager;
    
    [Header("Mode Selection")]
    public GameObject modeSelectionPanel;
    public Button physicalModeButton;
    public Button virtualModeButton;
    public Button undirectedGraphButton;
    public Button directedGraphButton;
    
    [Header("Main Buttons")]
    public Button addNodeButton;
    public Button addEdgeButton;
    public Button removeNodeButton;
    public Button bfsButton;
    public Button dfsButton;
    public Button dijkstraButton;
    public Button resetButton;
    
    [Header("UI Panels")]
    public GameObject headerPanel;
    public GameObject instructionCard;
    public GameObject buttonPanel;
    public GameObject explanationPanel;
    
    [Header("Info Display")]
    public TextMeshProUGUI explanationText;
    public TextMeshProUGUI tutorialHintText;
    public TextMeshProUGUI statusText;
    
    [Header("Analysis Toggle")]
    public AnalysisToggleButton analysisToggle;
    
    private bool operationsVisible = false;
    
    void Start()
    {
        // Mode Selection
        if (physicalModeButton != null)
        {
            physicalModeButton.onClick.AddListener(OnPhysicalModeSelected);
        }
        
        if (virtualModeButton != null)
        {
            virtualModeButton.onClick.AddListener(OnVirtualModeSelected);
        }
        
        if (undirectedGraphButton != null)
        {
            undirectedGraphButton.onClick.AddListener(OnUndirectedSelected);
        }
        
        if (directedGraphButton != null)
        {
            directedGraphButton.onClick.AddListener(OnDirectedSelected);
        }
        
        // Operation Buttons - FIXED: Now calls TriggerAddNode()
        if (addNodeButton != null)
        {
            addNodeButton.onClick.AddListener(OnAddNodeClicked);
            addNodeButton.interactable = false;
        }
        
        if (addEdgeButton != null)
        {
            addEdgeButton.onClick.AddListener(OnAddEdgeClicked);
            addEdgeButton.interactable = false;
        }
        
        if (removeNodeButton != null)
        {
            removeNodeButton.onClick.AddListener(OnRemoveNodeClicked);
            removeNodeButton.interactable = false;
        }
        
        if (bfsButton != null)
        {
            bfsButton.onClick.AddListener(OnBFSClicked);
            bfsButton.interactable = false;
        }
        
        if (dfsButton != null)
        {
            dfsButton.onClick.AddListener(OnDFSClicked);
            dfsButton.interactable = false;
        }
        
        if (dijkstraButton != null)
        {
            dijkstraButton.onClick.AddListener(OnDijkstraClicked);
            dijkstraButton.interactable = false;
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
            resetButton.interactable = true;
            SetButtonColor(resetButton, true);
        }
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "Choose mode and graph type to begin!";
        }
        
        if (statusText != null)
        {
            statusText.text = "Nodes: 0 | Edges: 0";
        }
        
        ShowModeSelection();
    }
    
    void ShowModeSelection()
    {
        if (modeSelectionPanel != null)
            modeSelectionPanel.SetActive(true);
        
        if (headerPanel != null)
            headerPanel.SetActive(false);
        
        if (instructionCard != null)
            instructionCard.SetActive(false);
        
        if (buttonPanel != null)
            buttonPanel.SetActive(false);
        
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        if (analysisToggle != null)
        {
            analysisToggle.ForceHidePanel();
        }
        
        operationsVisible = false;
    }
    
    void OnPhysicalModeSelected()
    {
        if (graphManager == null) return;
        
        graphManager.SetPhysicalMode();
        HideModeSelection();
        ShowSetupInstructions();
        
        if (explanationText != null)
        {
            explanationText.text = "PHYSICAL MODE Selected!, Scan QR codes and MOVE them in real space, Physical positioning creates graph structure, Edges connect objects where you place them, Hands-on learning of graph topology!";
        }
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "TIP: Move QR codes to create your graph structure";
        }
        
        Debug.Log("User selected PHYSICAL mode");
    }
    
    void OnVirtualModeSelected()
    {
        if (graphManager == null) return;
        
        graphManager.SetVirtualMode();
        HideModeSelection();
        ShowSetupInstructions();
        
        if (explanationText != null)
        {
            explanationText.text = "VIRTUAL MODE Selected! Scan ONE QR to define node type, All nodes use same object type, Add/remove nodes with button taps, Automatic positioning in AR space, Perfect for quick algorithm demos";
        }
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "TIP: Scan ONE QR - all nodes will use that object!";
        }
        
        Debug.Log("User selected VIRTUAL mode");
    }
    
    void OnUndirectedSelected()
    {
        if (graphManager == null) return;
        
        graphManager.SetUndirectedGraph();
        
        if (explanationText != null)
        {
            explanationText.text = "UNDIRECTED GRAPH Selected!\n\n" +
                "• Edges work in BOTH directions\n" +
                "• A→B means B→A automatically\n" +
                "• Like friendship: mutual connection\n" +
                "• Social networks, road maps";
        }
        
        Debug.Log("Graph type: UNDIRECTED");
    }
    
    void OnDirectedSelected()
    {
        if (graphManager == null) return;
        
        graphManager.SetDirectedGraph();
        
        if (explanationText != null)
        {
            explanationText.text = "DIRECTED GRAPH Selected!\n\n" +
                "• Edges have specific direction\n" +
                "• A→B does NOT mean B→A\n" +
                "• Like following: one-way relationship\n" +
                "• Web pages, Twitter, task dependencies";
        }
        
        Debug.Log("Graph type: DIRECTED");
    }
    
    void HideModeSelection()
    {
        if (modeSelectionPanel != null)
            modeSelectionPanel.SetActive(false);
    }
    
    void ShowSetupInstructions()
    {
        if (headerPanel != null)
            headerPanel.SetActive(true);
        
        if (instructionCard != null)
            instructionCard.SetActive(true);
        
        if (explanationPanel != null)
            explanationPanel.SetActive(true);
    }
    
    void Update()
    {
        if (graphManager != null && graphManager.IsGraphReady() && !operationsVisible)
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
        
        string modeInfo = "";
        if (graphManager != null)
        {
            string mode = graphManager.GetCurrentMode() == QRGraphManager.InteractionMode.Physical ? "Physical" : "Virtual";
            string graphType = graphManager.GetGraphType() == QRGraphManager.GraphType.Undirected ? "Undirected" : "Directed";
            
            modeInfo = $"{mode} Mode | {graphType} Graph\n\n";
        }
        
        UpdateExplanation(modeInfo +
            "Graph initialized! Operations:\n\n" +
            "ADD NODE: Add vertices to graph\n" +
            "ADD EDGE: Connect two nodes\n" +
            "REMOVE NODE: Delete vertex & edges\n" +
            "BFS: Breadth-First Search\n" +
            "DFS: Depth-First Search\n" +
            "DIJKSTRA: Shortest path algorithm");
            
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "Build your graph by adding nodes and edges!";
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // BUTTON HANDLERS - FIXED
    // ═══════════════════════════════════════════════════════════
    
    void OnAddNodeClicked()
    {
        if (graphManager == null) return;
        
        // CRITICAL FIX: Actually call the manager's TriggerAddNode method
        graphManager.TriggerAddNode();
        
        if (graphManager.GetCurrentMode() == QRGraphManager.InteractionMode.Physical)
        {
            UpdateExplanation("ADD NODE (Physical):\n\n" +
                "1. Scan a NEW QR code\n" +
                "2. Different from existing nodes\n" +
                "3. Tap QR to confirm node\n\n" +
                "Each node must be a unique object!");
        }
        else
        {
            UpdateExplanation("ADD NODE (Virtual):\n\n" +
                "Node added automatically!\n" +
                "• Uses same object type as first node\n" +
                "• Positioned in AR space\n" +
                "• Click button again for more nodes\n\n" +
                "Quick and easy!");
        }
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = graphManager.GetCurrentMode() == QRGraphManager.InteractionMode.Physical 
                ? "Scan a NEW QR code to add node" 
                : "Node added! Click again for more";
        }
    }
    
    void OnAddEdgeClicked()
    {
        if (graphManager == null) return;
        
        graphManager.StartAddEdge();
        
        string graphType = graphManager.GetGraphType() == QRGraphManager.GraphType.Undirected ? "Undirected" : "Directed";
        
        UpdateExplanation($"ADD EDGE ({graphType}):\n\n" +
            "1. TAP source node\n" +
            "2. TAP destination node\n" +
            "3. Edge will connect them\n\n" +
            (graphType == "Undirected" 
                ? "Connection works BOTH ways!" 
                : "Connection has DIRECTION (arrow)!"));
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "Tap two nodes to connect them";
        }
    }
    
    void OnRemoveNodeClicked()
    {
        if (graphManager == null) return;
        
        graphManager.StartRemoveNode();
        
        UpdateExplanation("REMOVE NODE:\n\n" +
            "• Tap any node to remove it\n" +
            "• ALL connected edges will be deleted\n" +
            "• Other nodes remain unchanged\n\n" +
            "⚠️ This affects graph connectivity!");
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "Tap node to remove it and all its edges";
        }
    }
    
    void OnBFSClicked()
    {
        if (graphManager == null) return;
        
        graphManager.StartBFS();
        
        UpdateExplanation("BREADTH-FIRST SEARCH (BFS):\n\n" +
            "Algorithm:\n" +
            "• Start at selected node\n" +
            "• Visit all neighbors first\n" +
            "• Then visit their neighbors\n" +
            "• Uses QUEUE (FIFO)\n\n" +
            "Time: O(V + E)\n" +
            "Space: O(V)\n\n" +
            "TAP starting node!");
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "BFS explores layer by layer (like ripples in water)";
        }
    }
    
    void OnDFSClicked()
    {
        if (graphManager == null) return;
        
        graphManager.StartDFS();
        
        UpdateExplanation("DEPTH-FIRST SEARCH (DFS):\n\n" +
            "Algorithm:\n" +
            "• Start at selected node\n" +
            "• Go as DEEP as possible\n" +
            "• Backtrack when stuck\n" +
            "• Uses STACK (LIFO)\n\n" +
            "Time: O(V + E)\n" +
            "Space: O(V)\n\n" +
            "TAP starting node!");
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "DFS explores one path fully before trying others";
        }
    }
    
    void OnDijkstraClicked()
    {
        if (graphManager == null) return;
        
        graphManager.StartDijkstra();
        
        UpdateExplanation("DIJKSTRA'S SHORTEST PATH:\n\n" +
            "Algorithm:\n" +
            "• Finds SHORTEST path between nodes\n" +
            "• Uses edge weights (distances)\n" +
            "• Greedy approach\n" +
            "• Priority queue optimization\n\n" +
            "Time: O((V+E) log V)\n" +
            "Space: O(V)\n\n" +
            "TAP source, then destination!");
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "Find the shortest path between two nodes!";
        }
    }
    
    void OnResetClicked()
    {
        if (graphManager == null) return;
        
        graphManager.ResetGraph();
        operationsVisible = false;

        if (analysisToggle != null)
        {
            analysisToggle.ForceHidePanel();
        }
        
        ShowModeSelection();
        
        UpdateExplanation("");
        
        if (tutorialHintText != null)
            tutorialHintText.text = "Choose mode and graph type to begin!";
    }
    
    // ═══════════════════════════════════════════════════════════
    // UI HELPERS
    // ═══════════════════════════════════════════════════════════
    
    void UpdateExplanation(string message)
    {
        if (explanationText != null)
        {
            explanationText.text = message;
        }
    }
    
    void UpdateButtonStates()
    {
        if (graphManager == null) return;
        
        int nodeCount = graphManager.GetNodeCount();
        int edgeCount = graphManager.GetEdgeCount();
        bool hasNodes = nodeCount > 0;
        bool canAddEdge = nodeCount >= 2;
        bool isReady = graphManager.IsGraphReady();
        
        // ADD NODE: Always enabled when ready (except at max)
        if (addNodeButton != null)
        {
            bool addNodeEnabled = isReady && nodeCount < 10;
            addNodeButton.interactable = addNodeEnabled;
            SetButtonColor(addNodeButton, addNodeEnabled);
        }
        
        // ADD EDGE: Need at least 2 nodes
        if (addEdgeButton != null)
        {
            bool addEdgeEnabled = isReady && canAddEdge;
            addEdgeButton.interactable = addEdgeEnabled;
            SetButtonColor(addEdgeButton, addEdgeEnabled);
        }
        
        // REMOVE NODE: Need at least 1 node
        if (removeNodeButton != null)
        {
            bool removeEnabled = isReady && hasNodes;
            removeNodeButton.interactable = removeEnabled;
            SetButtonColor(removeNodeButton, removeEnabled);
        }
        
        // BFS: Need at least 1 node
        if (bfsButton != null)
        {
            bool bfsEnabled = isReady && hasNodes;
            bfsButton.interactable = bfsEnabled;
            SetButtonColor(bfsButton, bfsEnabled);
        }
        
        // DFS: Need at least 1 node
        if (dfsButton != null)
        {
            bool dfsEnabled = isReady && hasNodes;
            dfsButton.interactable = dfsEnabled;
            SetButtonColor(dfsButton, dfsEnabled);
        }
        
        // DIJKSTRA: Need at least 2 nodes
        if (dijkstraButton != null)
        {
            bool dijkstraEnabled = isReady && canAddEdge;
            dijkstraButton.interactable = dijkstraEnabled;
            SetButtonColor(dijkstraButton, dijkstraEnabled);
        }
        
        // RESET: Always enabled
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