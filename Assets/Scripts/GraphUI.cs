using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GraphUI : MonoBehaviour
{
    [Header("References")]
    public GraphVisualizer graphVisualizer;
    
    [Header("Vertex Buttons")]
    public Button addVertexButton;
    public Button removeVertexButton;
    
    [Header("Edge Buttons")]
    public Button addEdgeButton;
    public Button addRandomEdgeButton;
    public Button removeEdgeButton;
    
    [Header("Other Buttons")]
    public Button toggleDirectedButton;
    public Button clearButton;
    
    [Header("UI Panels")]
    public GameObject headerPanel;
    public GameObject instructionCard;
    public GameObject buttonPanel;
    public GameObject explanationPanel;
    public GameObject helpPanel;  //  NEW: Add this for help instructions
    
    [Header("Info Display")]
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI explanationText;
    public TextMeshProUGUI directedStatusText;
    public TextMeshProUGUI helpText;  //  NEW: Text inside help panel
    
    [Header("Edge Input (Optional)")]
    public TMP_InputField fromVertexInput;
    public TMP_InputField toVertexInput;
    
    private bool buttonsVisible = false;
    
    void Start()
    {
        if (addVertexButton != null)
            addVertexButton.onClick.AddListener(OnAddVertexClicked);
        
        if (removeVertexButton != null)
            removeVertexButton.onClick.AddListener(OnRemoveVertexClicked);
        
        if (addEdgeButton != null)
            addEdgeButton.onClick.AddListener(OnAddEdgeClicked);
        
        if (addRandomEdgeButton != null)
            addRandomEdgeButton.onClick.AddListener(OnAddRandomEdgeClicked);
        
        if (removeEdgeButton != null)
            removeEdgeButton.onClick.AddListener(OnRemoveEdgeClicked);
        
        if (toggleDirectedButton != null)
            toggleDirectedButton.onClick.AddListener(OnToggleDirectedClicked);
        
        if (clearButton != null)
            clearButton.onClick.AddListener(OnClearClicked);
        
        if (headerPanel != null)
            headerPanel.SetActive(true);
        
        if (instructionCard != null)
            instructionCard.SetActive(true);
        
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        if (infoText != null)
            infoText.gameObject.SetActive(false);
        
        //  FIXED: Hide help panel initially
        if (helpPanel != null)
            helpPanel.SetActive(false);
        
        SetupHelpText();
        HideButtons();
    }
    
    void SetupHelpText()
    {
        if (helpText != null)
        {
            helpText.text = "Add vertices first, then connect them with edges. Toggle between directed/undirected modes!";
        }
    }
    
    void Update()
    {
        if (graphVisualizer != null && graphVisualizer.IsGraphPlaced() && !buttonsVisible)
        {
            ShowButtons();
            UpdateExplanation(" Graph placed! Add vertices to start building.");
        }
        
        if (buttonsVisible)
        {
            UpdateInfoText();
            UpdateDirectedStatus();
            UpdateButtonStates();
        }
    }
    
    void HideButtons()
    {
        if (buttonPanel != null)
        {
            buttonPanel.SetActive(false);
        }
        else
        {
            if (addVertexButton != null) addVertexButton.gameObject.SetActive(false);
            if (removeVertexButton != null) removeVertexButton.gameObject.SetActive(false);
            if (addEdgeButton != null) addEdgeButton.gameObject.SetActive(false);
            if (addRandomEdgeButton != null) addRandomEdgeButton.gameObject.SetActive(false);
            if (removeEdgeButton != null) removeEdgeButton.gameObject.SetActive(false);
            if (toggleDirectedButton != null) toggleDirectedButton.gameObject.SetActive(false);
            if (clearButton != null) clearButton.gameObject.SetActive(false);
        }
        
        buttonsVisible = false;
    }
    
    void ShowButtons()
    {
        if (headerPanel != null)
            headerPanel.SetActive(false);
        
        if (instructionCard != null)
            instructionCard.SetActive(false);
        
        if (explanationPanel != null)
            explanationPanel.SetActive(true);
        
        if (infoText != null)
            infoText.gameObject.SetActive(true);
        
        //  FIXED: Show help panel after placement
        if (helpPanel != null)
            helpPanel.SetActive(true);
        
        if (buttonPanel != null)
        {
            buttonPanel.SetActive(true);
        }
        else
        {
            if (addVertexButton != null) addVertexButton.gameObject.SetActive(true);
            if (removeVertexButton != null) removeVertexButton.gameObject.SetActive(true);
            if (addEdgeButton != null) addEdgeButton.gameObject.SetActive(true);
            if (addRandomEdgeButton != null) addRandomEdgeButton.gameObject.SetActive(true);
            if (removeEdgeButton != null) removeEdgeButton.gameObject.SetActive(true);
            if (toggleDirectedButton != null) toggleDirectedButton.gameObject.SetActive(true);
            if (clearButton != null) clearButton.gameObject.SetActive(true);
        }
        
        buttonsVisible = true;
    }
    
    void OnAddVertexClicked()
    {
        if (graphVisualizer == null) return;
        
        int countBefore = GetVertexCount();
        graphVisualizer.AddVertex();
        
        StartCoroutine(UpdateAfterAddVertex(countBefore));
    }
    
    System.Collections.IEnumerator UpdateAfterAddVertex(int countBefore)
    {
        yield return new WaitForEndOfFrame();
        
        int countAfter = GetVertexCount();
        UpdateInfoText();
        
        if (countAfter > countBefore)
            UpdateExplanation($" Vertex {countAfter} added\n Vertices are the nodes in a graph!");
        else
            UpdateExplanation(" Graph is full! Maximum vertices reached.");
    }
    
    void OnRemoveVertexClicked()
    {
        if (graphVisualizer == null) return;
        
        int countBefore = GetVertexCount();
        
        if (countBefore == 0)
        {
            UpdateExplanation(" Graph is empty! Add vertices first.");
            return;
        }
        
        graphVisualizer.RemoveVertex();
        
        StartCoroutine(UpdateAfterRemoveVertex(countBefore));
    }
    
    System.Collections.IEnumerator UpdateAfterRemoveVertex(int countBefore)
    {
        yield return new WaitForEndOfFrame();
        
        int countAfter = GetVertexCount();
        UpdateInfoText();
        
        if (countAfter < countBefore)
            UpdateExplanation($" Removed vertex and its connections\n Connected edges were also deleted!");
        else
            UpdateExplanation(" Could not remove vertex!");
    }
    
    void OnAddEdgeClicked()
    {
        if (graphVisualizer == null) return;
        
        if (GetVertexCount() < 2)
        {
            UpdateExplanation(" Need at least 2 vertices to create an edge!");
            return;
        }
        
        if (fromVertexInput != null && toVertexInput != null &&
            !string.IsNullOrEmpty(fromVertexInput.text) && !string.IsNullOrEmpty(toVertexInput.text))
        {
            if (int.TryParse(fromVertexInput.text, out int from) && 
                int.TryParse(toVertexInput.text, out int to))
            {
                from -= 1;
                to -= 1;
                
                int edgesBefore = GetEdgeCount();
                graphVisualizer.AddEdgeBetweenVertices(from, to);
                
                StartCoroutine(UpdateAfterAddEdge(edgesBefore, from + 1, to + 1));
                return;
            }
        }
        
        OnAddRandomEdgeClicked();
    }
    
    void OnAddRandomEdgeClicked()
    {
        if (graphVisualizer == null) return;
        
        if (GetVertexCount() < 2)
        {
            UpdateExplanation(" Need at least 2 vertices to create an edge!");
            return;
        }
        
        int edgesBefore = GetEdgeCount();
        graphVisualizer.AddRandomEdge();
        
        StartCoroutine(UpdateAfterAddRandomEdge(edgesBefore));
    }
    
    System.Collections.IEnumerator UpdateAfterAddEdge(int edgesBefore, int from, int to)
    {
        yield return new WaitForEndOfFrame();
        
        int edgesAfter = GetEdgeCount();
        UpdateInfoText();
        
        string graphType = graphVisualizer.GetGraphType();
        
        if (edgesAfter > edgesBefore)
            UpdateExplanation($" Edge created: {from} → {to}\n Edges connect vertices in a {graphType} graph!");
        else
            UpdateExplanation(" Edge already exists or could not be created!");
    }
    
    System.Collections.IEnumerator UpdateAfterAddRandomEdge(int edgesBefore)
    {
        yield return new WaitForEndOfFrame();
        
        int edgesAfter = GetEdgeCount();
        UpdateInfoText();
        
        string graphType = graphVisualizer.GetGraphType();
        
        if (edgesAfter > edgesBefore)
            UpdateExplanation($" Random edge created!\n This is a {graphType} graph.");
        else
            UpdateExplanation(" Could not create edge! All possible edges may exist.");
    }
    
    void OnRemoveEdgeClicked()
    {
        if (graphVisualizer == null) return;
        
        int edgesBefore = GetEdgeCount();
        
        if (edgesBefore == 0)
        {
            UpdateExplanation(" No edges to remove!");
            return;
        }
        
        graphVisualizer.RemoveLastEdge();
        
        StartCoroutine(UpdateAfterRemoveEdge(edgesBefore));
    }
    
    System.Collections.IEnumerator UpdateAfterRemoveEdge(int edgesBefore)
    {
        yield return new WaitForEndOfFrame();
        
        int edgesAfter = GetEdgeCount();
        UpdateInfoText();
        
        if (edgesAfter < edgesBefore)
            UpdateExplanation(" Last edge removed\n Vertices remain, only connection is gone!");
        else
            UpdateExplanation(" Could not remove edge!");
    }
    
    void OnToggleDirectedClicked()
    {
        if (graphVisualizer == null) return;
        
        graphVisualizer.ToggleDirected();
        
        string graphType = graphVisualizer.GetGraphType();
        UpdateDirectedStatus();
        
        if (graphType == "Directed")
            UpdateExplanation(" Changed to DIRECTED graph\n Edges now have direction (one-way)!");
        else
            UpdateExplanation(" Changed to UNDIRECTED graph\n Edges are now bidirectional (two-way)!");
    }
    
    void OnClearClicked()
    {
        if (graphVisualizer == null) return;
        
        graphVisualizer.Clear();
        buttonsVisible = false;
        
        //  FIXED: Hide help panel when clearing
        if (helpPanel != null)
            helpPanel.SetActive(false);
        
        HideButtons();
        
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        if (infoText != null)
            infoText.gameObject.SetActive(false);
        
        if (headerPanel != null)
            headerPanel.SetActive(true);
        
        if (instructionCard != null)
            instructionCard.SetActive(true);
    }
    
    void UpdateInfoText()
    {
        if (infoText == null || graphVisualizer == null) return;
        
        int vertices = GetVertexCount();
        int edges = GetEdgeCount();
        infoText.text = $"Vertices: {vertices} | Edges: {edges}";
    }
    
    void UpdateDirectedStatus()
    {
        if (directedStatusText != null && graphVisualizer != null)
        {
            string type = graphVisualizer.GetGraphType();
            directedStatusText.text = $"Type: {type}";
            
            if (type == "Directed")
                directedStatusText.color = Color.yellow;
            else
                directedStatusText.color = Color.cyan;
        }
    }
    
    void UpdateExplanation(string message)
    {
        if (explanationText != null)
        {
            explanationText.text = message;
        }
    }
    
    int GetVertexCount()
    {
        if (graphVisualizer == null) return 0;
        return graphVisualizer.GetVertexCount();
    }
    
    int GetEdgeCount()
    {
        if (graphVisualizer == null) return 0;
        return graphVisualizer.GetEdgeCount();
    }

    void SetButtonColors(Button button, bool isEnabled)
    {
        if (button == null) return;
        
        ColorBlock colors = button.colors;
        
        if (isEnabled)
        {
            colors.normalColor = new Color(0.518f, 0.412f, 1f);
            colors.highlightedColor = new Color(0.618f, 0.512f, 1f);
            colors.pressedColor = new Color(0.318f, 0.212f, 0.7f);
            colors.selectedColor = new Color(0.418f, 0.312f, 0.9f);
            colors.disabledColor = new Color(0.518f, 0.412f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
        }
        else
        {
            colors.normalColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.selectedColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.disabledColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
        }
        
        button.colors = colors;
        
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = isEnabled ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.6f);
        }
        
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color imgColor = buttonImage.color;
            imgColor.a = isEnabled ? 1f : 0.5f;
            buttonImage.color = imgColor;
        }
    }

    void UpdateButtonStates()
    {
        if (graphVisualizer == null) return;
        
        int vertices = GetVertexCount();
        int edges = GetEdgeCount();
        bool hasVertices = vertices > 0;
        bool canAddVertex = true;
        bool canAddEdge = vertices >= 2;
        bool hasEdges = edges > 0;
        
        if (addVertexButton != null)
        {
            addVertexButton.interactable = canAddVertex;
            SetButtonColors(addVertexButton, canAddVertex);
        }
        
        if (removeVertexButton != null)
        {
            removeVertexButton.interactable = hasVertices;
            SetButtonColors(removeVertexButton, hasVertices);
        }
        
        if (addEdgeButton != null)
        {
            addEdgeButton.interactable = canAddEdge;
            SetButtonColors(addEdgeButton, canAddEdge);
        }
        
        if (addRandomEdgeButton != null)
        {
            addRandomEdgeButton.interactable = canAddEdge;
            SetButtonColors(addRandomEdgeButton, canAddEdge);
        }
        
        if (removeEdgeButton != null)
        {
            removeEdgeButton.interactable = hasEdges;
            SetButtonColors(removeEdgeButton, hasEdges);
        }
        
        if (toggleDirectedButton != null)
        {
            toggleDirectedButton.interactable = true;
            SetButtonColors(toggleDirectedButton, true);
        }
        
        if (clearButton != null)
        {
            clearButton.interactable = true;
            SetButtonColors(clearButton, true);
        }
    }
}