using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TreeUI : MonoBehaviour
{
    [Header("References")]
    public TreeVisualizer treeVisualizer;
    
    [Header("Buttons")]
    public Button addRootButton;
    public Button addLeftChildButton;
    public Button addRightChildButton;
    public Button removeNodeButton;
    public Button showTraversalButton;
    public Button clearButton;
    
    [Header("UI Panels")]
    public GameObject headerPanel;
    public GameObject instructionCard;
    public GameObject buttonPanel;
    public GameObject explanationPanel;
    public GameObject traversalPanel;
    public GameObject helpPanel;  //  NEW: Add this for help instructions
    
    [Header("Info Display")]
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI explanationText;
    public TextMeshProUGUI traversalText;
    public TextMeshProUGUI helpText;  //  NEW: Text inside help panel
    
    private bool buttonsVisible = false;
    private bool traversalVisible = false;
    
    void Start()
    {
        if (addRootButton != null)
            addRootButton.onClick.AddListener(OnAddRootClicked);
        
        if (addLeftChildButton != null)
            addLeftChildButton.onClick.AddListener(OnAddLeftChildClicked);
        
        if (addRightChildButton != null)
            addRightChildButton.onClick.AddListener(OnAddRightChildClicked);
        
        if (removeNodeButton != null)
            removeNodeButton.onClick.AddListener(OnRemoveNodeClicked);
        
        if (showTraversalButton != null)
            showTraversalButton.onClick.AddListener(OnShowTraversalClicked);
        
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
        
        if (traversalPanel != null)
            traversalPanel.SetActive(false);
        
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
            helpText.text = "Start with ROOT (red). Add LEFT (cyan) and RIGHT (magenta) children to build your tree!";
        }
    }
    
    void Update()
    {
        if (treeVisualizer != null && treeVisualizer.IsTreePlaced() && !buttonsVisible)
        {
            ShowButtons();
            UpdateExplanation(" Tree placed! Start by adding the ROOT node:");
        }
        
        if (buttonsVisible)
        {
            UpdateInfoText();
            UpdateButtonStates();
        }
        
        if (traversalVisible && GetTreeSize() > 0)
        {
            UpdateTraversalDisplay();
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
            if (addRootButton != null) addRootButton.gameObject.SetActive(false);
            if (addLeftChildButton != null) addLeftChildButton.gameObject.SetActive(false);
            if (addRightChildButton != null) addRightChildButton.gameObject.SetActive(false);
            if (removeNodeButton != null) removeNodeButton.gameObject.SetActive(false);
            if (showTraversalButton != null) showTraversalButton.gameObject.SetActive(false);
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
            if (addRootButton != null) addRootButton.gameObject.SetActive(true);
            if (addLeftChildButton != null) addLeftChildButton.gameObject.SetActive(true);
            if (addRightChildButton != null) addRightChildButton.gameObject.SetActive(true);
            if (removeNodeButton != null) removeNodeButton.gameObject.SetActive(true);
            if (showTraversalButton != null) showTraversalButton.gameObject.SetActive(true);
            if (clearButton != null) clearButton.gameObject.SetActive(true);
        }
        
        buttonsVisible = true;
        UpdateButtonStates();
    }
    
    void UpdateButtonStates()
    {
        if (treeVisualizer == null) return;
        
        int size = GetTreeSize();
        bool hasRoot = size > 0;
        bool canAdd = true;
        
        if (addRootButton != null)
        {
            bool canAddRoot = !hasRoot && canAdd;
            addRootButton.interactable = canAddRoot;
            SetButtonColors(addRootButton, canAddRoot);
        }
        
        if (addLeftChildButton != null)
        {
            bool canAddChild = hasRoot && canAdd;
            addLeftChildButton.interactable = canAddChild;
            SetButtonColors(addLeftChildButton, canAddChild);
        }
        
        if (addRightChildButton != null)
        {
            bool canAddChild = hasRoot && canAdd;
            addRightChildButton.interactable = canAddChild;
            SetButtonColors(addRightChildButton, canAddChild);
        }
        
        if (removeNodeButton != null)
        {
            removeNodeButton.interactable = hasRoot;
            SetButtonColors(removeNodeButton, hasRoot);
        }
        
        if (showTraversalButton != null)
        {
            showTraversalButton.interactable = hasRoot;
            SetButtonColors(showTraversalButton, hasRoot);
        }
        
        if (clearButton != null)
        {
            clearButton.interactable = true;
            SetButtonColors(clearButton, true);
        }
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
    
    void OnAddRootClicked()
    {
        if (treeVisualizer == null) return;
        
        int sizeBefore = GetTreeSize();
        
        if (sizeBefore > 0)
        {
            UpdateExplanation(" Root already exists! Use Left/Right buttons to add children.");
            return;
        }
        
        treeVisualizer.AddRoot();
        StartCoroutine(UpdateAfterAdd("ROOT (red)", "The root is the starting point of the tree!"));
    }
    
    void OnAddLeftChildClicked()
    {
        if (treeVisualizer == null) return;
        
        int sizeBefore = GetTreeSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation(" Add the ROOT node first!");
            return;
        }
        
        treeVisualizer.AddLeftChild();
        StartCoroutine(UpdateAfterAdd("LEFT child (cyan)", "Left children go to the left of their parent!"));
    }
    
    void OnAddRightChildClicked()
    {
        if (treeVisualizer == null) return;
        
        int sizeBefore = GetTreeSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation(" Add the ROOT node first!");
            return;
        }
        
        treeVisualizer.AddRightChild();
        StartCoroutine(UpdateAfterAdd("RIGHT child (magenta)", "Right children go to the right of their parent!"));
    }
    
    System.Collections.IEnumerator UpdateAfterAdd(string nodeType, string explanation)
    {
        yield return new WaitForSeconds(0.1f);
        
        UpdateInfoText();
        UpdateExplanation($" Added {nodeType}\n {explanation}");
    }
    
    void OnRemoveNodeClicked()
    {
        if (treeVisualizer == null) return;
        
        int sizeBefore = GetTreeSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation(" Tree is empty! Nothing to remove.");
            return;
        }
        
        treeVisualizer.RemoveNode();
        StartCoroutine(UpdateAfterRemove());
    }
    
    System.Collections.IEnumerator UpdateAfterRemove()
    {
        yield return new WaitForSeconds(0.1f);
        
        UpdateInfoText();
        
        int sizeAfter = GetTreeSize();
        if (sizeAfter == 0)
        {
            UpdateExplanation(" Removed last node! Tree is now empty.");
            if (traversalPanel != null)
                traversalPanel.SetActive(false);
            traversalVisible = false;
        }
        else
        {
            UpdateExplanation(" Removed last added node\n Nodes are removed in reverse order!");
        }
    }
    
    void OnShowTraversalClicked()
    {
        if (treeVisualizer == null) return;
        
        if (GetTreeSize() == 0)
        {
            UpdateExplanation(" Tree is empty! Add nodes first to see traversals.");
            return;
        }
        
        traversalVisible = !traversalVisible;
        
        if (traversalPanel != null)
            traversalPanel.SetActive(traversalVisible);
        
        if (traversalVisible)
        {
            UpdateTraversalDisplay();
            UpdateExplanation(" Showing tree traversal orders!\n These show different ways to visit nodes.");
        }
        else
        {
            UpdateExplanation(" Traversal panel hidden.");
        }
    }
    
    void UpdateTraversalDisplay()
    {
        if (treeVisualizer == null || traversalText == null) return;
        
        string traversalInfo = treeVisualizer.GetTraversalInfo();
        traversalText.text = traversalInfo;
    }
    
    void OnClearClicked()
    {
        if (treeVisualizer == null) return;
        
        treeVisualizer.Clear();
        buttonsVisible = false;
        traversalVisible = false;
        
        //  FIXED: Hide help panel when clearing
        if (helpPanel != null)
            helpPanel.SetActive(false);
        
        HideButtons();
        
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        if (infoText != null)
            infoText.gameObject.SetActive(false);
        
        if (traversalPanel != null)
            traversalPanel.SetActive(false);
        
        if (headerPanel != null)
            headerPanel.SetActive(true);
        
        if (instructionCard != null)
            instructionCard.SetActive(true);
    }
    
    void UpdateInfoText()
    {
        if (infoText == null) return;
        
        int size = GetTreeSize();
        int height = GetTreeHeight();
        infoText.text = $"Nodes: {size} | Height: {height}";
    }
    
    void UpdateExplanation(string message)
    {
        if (explanationText != null)
        {
            explanationText.text = message;
        }
    }
    
    int GetTreeSize()
    {
        if (treeVisualizer == null) return 0;
        return treeVisualizer.GetNodeCount();
    }
    
    int GetTreeHeight()
    {
        if (treeVisualizer == null) return 0;
        return treeVisualizer.GetHeight();
    }
}