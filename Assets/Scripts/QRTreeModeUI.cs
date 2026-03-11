using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QRTreeModeUI : MonoBehaviour
{
    [Header("References")]
    public QRTreeModeManager treeManager;
    
    [Header("Main Buttons")]
    public Button insertButton;
    public Button deleteButton;
    public Button searchButton;
    public Button traversalButton;
    public Button resetButton;
    
    [Header("Traversal Panel")]
    public GameObject traversalPanel;
    public Button backButton;
    public Button inOrderButton;
    public Button preOrderButton;
    public Button postOrderButton;
    public Button levelOrderButton;
    
    [Header("Input Field")]
    public TMP_InputField valueInputField;
    
    [Header("UI Panels")]
    public GameObject headerPanel;
    public GameObject instructionCard;
    public GameObject buttonPanel;
    public GameObject explanationPanel;
    public GameObject inputPanel;
    
    [Header("Info Display")]
    public TextMeshProUGUI explanationText;
    public TextMeshProUGUI tutorialHintText;
    public TextMeshProUGUI statusText;
    
    [Header("Analysis Toggle")]
    public AnalysisToggleButton analysisToggle;
    
    private bool operationsVisible = false;
    
    void Start()
    {
        if (insertButton != null)
        {
            insertButton.onClick.AddListener(OnInsertClicked);
            insertButton.interactable = false;
        }
        
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteClicked);
            deleteButton.interactable = false;
        }
        
        if (searchButton != null)
        {
            searchButton.onClick.AddListener(OnSearchClicked);
            searchButton.interactable = false;
        }
        
        if (traversalButton != null)
        {
            traversalButton.onClick.AddListener(OnTraversalButtonClicked);
            traversalButton.interactable = false;
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
            resetButton.interactable = true;
            SetButtonColor(resetButton, true);
        }
        
        // Traversal panel buttons
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
        
        if (inOrderButton != null)
        {
            inOrderButton.onClick.AddListener(() => OnTraversalClicked("InOrder"));
        }
        
        if (preOrderButton != null)
        {
            preOrderButton.onClick.AddListener(() => OnTraversalClicked("PreOrder"));
        }
        
        if (postOrderButton != null)
        {
            postOrderButton.onClick.AddListener(() => OnTraversalClicked("PostOrder"));
        }
        
        if (levelOrderButton != null)
        {
            levelOrderButton.onClick.AddListener(() => OnTraversalClicked("LevelOrder"));
        }
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "Scan ONE QR code to start building your tree!";
        }
        
        if (statusText != null)
        {
            statusText.text = "Nodes: 0";
        }
        
        ShowSetupInstructions();
        
        // Hide traversal panel initially
        if (traversalPanel != null)
        {
            traversalPanel.SetActive(false);
        }
    }
    
    void ShowSetupInstructions()
    {
        if (headerPanel != null)
            headerPanel.SetActive(true);
        
        if (instructionCard != null)
            instructionCard.SetActive(true);
        
        if (inputPanel != null)
            inputPanel.SetActive(false);
        
        if (explanationPanel != null)
        {
            explanationPanel.SetActive(true);
        }
        
        if (explanationText != null)
        {
            explanationText.text = "VIRTUAL MODE\n\n" +
                "In this mode, you will:\n" +
                "- Scan ONE QR code to define the node type\n" +
                "- All tree nodes will use that same object\n" +
                "- Insert/delete/search operations are AUTOMATIC!\n" +
                "- Tree is manipulated virtually in AR space\n\n" +
                "Perfect for quick demonstrations and learning!";
        }
        
        if (analysisToggle != null)
        {
            analysisToggle.ForceHidePanel();
        }
        
        operationsVisible = false;
    }
    
    void Update()
    {
        if (treeManager != null && treeManager.IsTreeReady() && !operationsVisible)
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
        
        if (inputPanel != null)
            inputPanel.SetActive(true);
        
        if (explanationPanel != null)
            explanationPanel.SetActive(true);
        
        string modeInfo = "";
        if (treeManager != null)
        {
            string objType = treeManager.GetVirtualObjectType().ToUpper();
            modeInfo = $"Virtual Mode: Using {objType} nodes\n" +
                      "All operations are automatic!\n\n";
        }
        
        UpdateExplanation(modeInfo +
            "Binary Search Tree initialized!\n\n" +
            "Operations available:\n" +
            "INSERT: Add nodes (enter value)\n" +
            "DELETE: Remove nodes (tap node)\n" +
            "SEARCH: Find values (tap node)\n" +
            "TRAVERSAL: Visit all nodes in order");
            
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "Enter values to insert - tree builds automatically!";
        }
    }
    
    void OnInsertClicked()
    {
        if (treeManager == null) return;
        
        if (valueInputField == null || string.IsNullOrEmpty(valueInputField.text))
        {
            UpdateExplanation("Please enter a value first!\n\n" +
                "Example: Type '50' to insert value 50\n" +
                "Example: Type '25' to insert value 25");
            
            if (tutorialHintText != null)
                tutorialHintText.text = "BST: Smaller values go LEFT, larger go RIGHT";
            
            if (valueInputField != null)
                valueInputField.Select();
            
            return;
        }
        
        if (!int.TryParse(valueInputField.text, out int value))
        {
            UpdateExplanation("Invalid value! Enter a number (1-99)");
            return;
        }
        
        if (value < 1 || value > 99)
        {
            UpdateExplanation("Value must be between 1 and 99");
            return;
        }
        
        treeManager.SimulateInsertValue(value);
        
        UpdateExplanation($"INSERT value {value}:\n" +
            "Tap anywhere to add node!\n" +
            "Tree will auto-position the node.");
        
        if (valueInputField != null)
            valueInputField.text = "";
    }
    
    void OnDeleteClicked()
    {
        if (treeManager == null) return;
        
        treeManager.SimulateDelete();
        
        UpdateExplanation("DELETE operation:\n" +
            "TAP the node you want to delete.\n\n" +
            "3 Delete Cases:\n" +
            "1. Leaf node - Simply remove\n" +
            "2. One child - Replace with child\n" +
            "3. Two children - Replace with successor");
            
        if (tutorialHintText != null)
            tutorialHintText.text = "Tap a node to delete it";
    }
    
    void OnSearchClicked()
    {
        if (treeManager == null) return;
        
        treeManager.SimulateSearch();
        
        UpdateExplanation("SEARCH operation:\n" +
            "TAP the node you want to search for.\n\n" +
            "Binary Search:\n" +
            "- Compare with current node\n" +
            "- Go LEFT if smaller\n" +
            "- Go RIGHT if larger\n" +
            "- Much faster than linear search!");
            
        if (tutorialHintText != null)
            tutorialHintText.text = "Tap a node to search for its value";
    }
    
    void OnTraversalButtonClicked()
    {
        if (traversalPanel != null)
        {
            traversalPanel.SetActive(true);
        }
        
        UpdateExplanation("TREE TRAVERSAL\n\n" +
            "Choose a traversal method:\n\n" +
            "IN-ORDER: Left → Root → Right (Sorted)\n" +
            "PRE-ORDER: Root → Left → Right\n" +
            "POST-ORDER: Left → Right → Root\n" +
            "LEVEL-ORDER: Level by level (BFS)");
        
        if (tutorialHintText != null)
            tutorialHintText.text = "Select a traversal method to visualize";
    }
    
    void OnBackButtonClicked()
    {
        if (traversalPanel != null)
        {
            traversalPanel.SetActive(false);
        }
        
        string modeInfo = "";
        if (treeManager != null)
        {
            string objType = treeManager.GetVirtualObjectType().ToUpper();
            modeInfo = $"Virtual Mode: Using {objType} nodes\n" +
                      "All operations are automatic!\n\n";
        }
        
        UpdateExplanation(modeInfo +
            "Binary Search Tree initialized!\n\n" +
            "Operations available:\n" +
            "INSERT: Add nodes (enter value)\n" +
            "DELETE: Remove nodes (tap node)\n" +
            "SEARCH: Find values (tap node)\n" +
            "TRAVERSAL: Visit all nodes in order");
            
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "Enter values to insert - tree builds automatically!";
        }
    }
    
    void OnTraversalClicked(string traversalType)
    {
        if (treeManager == null) return;
        
        treeManager.SimulateTraversal(traversalType);
        
        // Hide traversal panel during animation
        if (traversalPanel != null)
        {
            traversalPanel.SetActive(false);
        }
        
        string explanation = "";
        
        switch (traversalType)
        {
            case "InOrder":
                explanation = "IN-ORDER TRAVERSAL:\n" +
                    "Order: Left → Root → Right\n\n" +
                    "Produces SORTED output!\n" +
                    "Visit left subtree first,\n" +
                    "then root, then right subtree.";
                break;
                
            case "PreOrder":
                explanation = "PRE-ORDER TRAVERSAL:\n" +
                    "Order: Root → Left → Right\n\n" +
                    "Process root FIRST.\n" +
                    "Useful for copying tree structure.";
                break;
                
            case "PostOrder":
                explanation = "POST-ORDER TRAVERSAL:\n" +
                    "Order: Left → Right → Root\n\n" +
                    "Process root LAST.\n" +
                    "Useful for deleting tree.";
                break;
                
            case "LevelOrder":
                explanation = "LEVEL-ORDER TRAVERSAL:\n" +
                    "Visit nodes level by level.\n\n" +
                    "Also called Breadth-First Search.\n" +
                    "Uses QUEUE data structure.";
                break;
        }
        
        UpdateExplanation(explanation);
        
        if (tutorialHintText != null)
            tutorialHintText.text = "Watch the cyan highlight visit each node!";
    }
    
    void OnResetClicked()
    {
        if (treeManager == null) return;
        
        treeManager.ResetTree();
        operationsVisible = false;

        if (analysisToggle != null)
        {
            analysisToggle.ForceHidePanel();
        }
        
        if (valueInputField != null)
            valueInputField.text = "";
        
        // Hide traversal panel
        if (traversalPanel != null)
        {
            traversalPanel.SetActive(false);
        }
        
        ShowSetupInstructions();
        
        if (tutorialHintText != null)
            tutorialHintText.text = "Scan ONE QR code to start building your tree!";
    }
    
    void UpdateExplanation(string message)
    {
        if (explanationText != null)
        {
            explanationText.text = message;
        }
    }
    
    void UpdateButtonStates()
    {
        if (treeManager == null) return;
        
        int nodeCount = treeManager.GetNodeCount();
        bool hasNodes = nodeCount > 0;
        bool hasInputValue = valueInputField != null && !string.IsNullOrEmpty(valueInputField.text);
        
        // INSERT: Enabled if has input value
        if (insertButton != null)
        {
            bool insertEnabled = (nodeCount == 0) || hasInputValue;
            insertButton.interactable = insertEnabled;
            SetButtonColor(insertButton, insertEnabled);
        }
        
        // DELETE: Enabled if tree has nodes
        if (deleteButton != null)
        {
            bool deleteEnabled = hasNodes;
            deleteButton.interactable = deleteEnabled;
            SetButtonColor(deleteButton, deleteEnabled);
        }
        
        // SEARCH: Enabled if tree has nodes
        if (searchButton != null)
        {
            bool searchEnabled = hasNodes;
            searchButton.interactable = searchEnabled;
            SetButtonColor(searchButton, searchEnabled);
        }
        
        // TRAVERSAL: Enabled if tree has nodes
        if (traversalButton != null)
        {
            bool traversalEnabled = hasNodes;
            traversalButton.interactable = traversalEnabled;
            SetButtonColor(traversalButton, traversalEnabled);
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
            colors.normalColor = new Color(0.2f, 0.6f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.7f, 1f);
            colors.pressedColor = new Color(0.1f, 0.5f, 0.9f);
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