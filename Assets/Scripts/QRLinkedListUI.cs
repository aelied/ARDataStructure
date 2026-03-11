using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QRLinkedListUI : MonoBehaviour
{
    [Header("References")]
    public QRLinkedListManager listManager;
    
    [Header("Mode Selection")]
    public GameObject modeSelectionPanel;
    public Button physicalModeButton;
    public Button virtualModeButton;
    
    [Header("Insert Buttons")]
    public Button insertHeadButton;
    public Button insertTailButton;
    public Button insertMiddleButton;
    
    [Header("Delete Buttons")]
    public Button deleteHeadButton;
    public Button deleteTailButton;
    public Button deleteMiddleButton;
    
    [Header("Other Buttons")]
    public Button traverseButton;
    public Button searchButton;
    public Button confirmShiftButton; // 🔧 ADDED - For physical mode shift confirmation
    public Button resetButton;
    
    [Header("UI Panels")]
    public GameObject headerPanel;
    public GameObject instructionCard;
    public GameObject buttonPanel;
    public GameObject explanationPanel;
    public GameObject inputPanel;
    public GameObject helpPanel;
    
    [Header("Info Display")]
    public TextMeshProUGUI explanationText;
    public TextMeshProUGUI tutorialHintText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI helpText;
    
    [Header("Analysis Toggle")]
    public AnalysisToggleButton analysisToggle;
    
    private bool operationsVisible = false;
    
    void Start()
    {
        if (physicalModeButton != null)
        {
            physicalModeButton.onClick.AddListener(OnPhysicalModeSelected);
        }
        
        if (virtualModeButton != null)
        {
            virtualModeButton.onClick.AddListener(OnVirtualModeSelected);
        }
        
        if (insertHeadButton != null)
        {
            insertHeadButton.onClick.AddListener(OnInsertHeadClicked);
            insertHeadButton.interactable = false;
        }
        
        if (insertTailButton != null)
        {
            insertTailButton.onClick.AddListener(OnInsertTailClicked);
            insertTailButton.interactable = false;
        }
        
        if (insertMiddleButton != null)
        {
            insertMiddleButton.onClick.AddListener(OnInsertMiddleClicked);
            insertMiddleButton.interactable = false;
        }
        
        if (deleteHeadButton != null)
        {
            deleteHeadButton.onClick.AddListener(OnDeleteHeadClicked);
            deleteHeadButton.interactable = false;
        }
        
        if (deleteTailButton != null)
        {
            deleteTailButton.onClick.AddListener(OnDeleteTailClicked);
            deleteTailButton.interactable = false;
        }
        
        if (deleteMiddleButton != null)
        {
            deleteMiddleButton.onClick.AddListener(OnDeleteMiddleClicked);
            deleteMiddleButton.interactable = false;
        }
        
        if (traverseButton != null)
        {
            traverseButton.onClick.AddListener(OnTraverseClicked);
            traverseButton.interactable = false;
        }
        
        if (searchButton != null)
        {
            searchButton.onClick.AddListener(OnSearchClicked);
            searchButton.interactable = false;
        }
        
        // 🔧 NEW - Confirm Shift Button
        if (confirmShiftButton != null)
        {
            confirmShiftButton.onClick.AddListener(OnConfirmShiftClicked);
            confirmShiftButton.gameObject.SetActive(false); // Hidden by default
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetClicked);
            resetButton.interactable = true;
            SetButtonColor(resetButton, true);
        }
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "Choose a mode to start building your linked list!";
        }
        
        if (statusText != null)
        {
            statusText.text = "Nodes: 0";
        }
        
        if (helpPanel != null)
            helpPanel.SetActive(false);
        
        SetupHelpText();
        ShowModeSelection();
    }
    
    void SetupHelpText()
    {
        if (helpText != null)
        {
            helpText.text = "Insert adds nodes at head, tail, or middle. Delete removes from head, tail, or middle position.";
        }
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
        
        if (inputPanel != null)
            inputPanel.SetActive(false);
        
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
        
        if (helpPanel != null)
            helpPanel.SetActive(false);
        
        if (confirmShiftButton != null)
            confirmShiftButton.gameObject.SetActive(false);
        
        if (analysisToggle != null)
        {
            analysisToggle.ForceHidePanel();
        }
        
        operationsVisible = false;
    }
    
    void OnPhysicalModeSelected()
    {
        if (listManager == null) return;
        
        listManager.SetPhysicalMode();
        HideModeSelection();
        ShowSetupInstructions();
        
        if (explanationText != null)
        {
            explanationText.text = "PHYSICAL MODE Selected!\n\n" +
                "In this mode, you will:\n" +
                "• Scan QR codes (5x5cm) and MOVE them physically in real world\n" +
                "• See pointer arrows connecting nodes\n" +
                "• See YELLOW ARROWS showing where to move nodes\n" +
                "• Press CONFIRM after moving nodes physically\n\n" +
                "This mode teaches linked list traversal with hands-on interaction!";
        }
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "TIP: Follow the yellow arrows to move QR codes physically";
        }
        
        Debug.Log("User selected PHYSICAL mode");
    }
    
    void OnVirtualModeSelected()
    {
        if (listManager == null) return;
        
        listManager.SetVirtualMode();
        HideModeSelection();
        ShowSetupInstructions();
        
        if (explanationText != null)
        {
            explanationText.text = "VIRTUAL MODE Selected!\n\n" +
                "In this mode, you will:\n" +
                "• Scan ONE QR code (5x5cm) to define the node type\n" +
                "• All list nodes will use that same object\n" +
                "• Insert/delete operations are automatic - NO rescanning needed!\n" +
                "• Nodes are manipulated virtually in AR space\n\n" +
                "Perfect for quick demonstrations and learning!";
        }
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "TIP: Just scan ONE QR - all nodes will be that object!";
        }
        
        Debug.Log("User selected VIRTUAL mode");
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
        
        if (inputPanel != null)
            inputPanel.SetActive(false);
        
        if (explanationPanel != null)
            explanationPanel.SetActive(true);
        
        if (helpPanel != null)
            helpPanel.SetActive(false);
    }
    
    void Update()
    {
        if (listManager != null && listManager.IsListReady() && !operationsVisible)
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
            inputPanel.SetActive(false);
        
        if (explanationPanel != null)
            explanationPanel.SetActive(true);
        
        if (helpPanel != null)
            helpPanel.SetActive(true);
        
        string modeInfo = "";
        if (listManager != null)
        {
            if (listManager.GetCurrentMode() == QRLinkedListManager.InteractionMode.Physical)
            {
                modeInfo = "Physical Mode: Move QR codes physically (follow yellow arrows)\n\n";
            }
            else
            {
                string objType = listManager.GetVirtualObjectType().ToUpper();
                modeInfo = $"Virtual Mode: Using {objType} nodes\n" +
                          "All operations are automatic - no rescanning!\n\n";
            }
        }
        
        UpdateExplanation(modeInfo +
            "✅ Linked List initialized! Operations available:\n\n" +
            "INSERT HEAD: Add at beginning - O(1)\n" +
            "INSERT TAIL: Add at end - O(n)\n" +
            "INSERT MIDDLE: Add at center\n" +
            "DELETE HEAD/TAIL/MIDDLE: Remove nodes\n" +
            "TRAVERSE: Walk through list\n" +
            "SEARCH: Find nodes");
            
        if (tutorialHintText != null)
        {
            if (listManager.GetCurrentMode() == QRLinkedListManager.InteractionMode.Physical)
            {
                tutorialHintText.text = "Physical Mode: You'll move QR codes and press CONFIRM";
            }
            else
            {
                tutorialHintText.text = "Virtual Mode: All operations happen automatically!";
            }
        }
    }
    
    void OnInsertHeadClicked()
    {
        if (listManager == null) return;
        
        int sizeBefore = listManager.GetListSize();
        
        listManager.InsertAtHead();
        
        bool isPhysical = listManager.GetCurrentMode() == QRLinkedListManager.InteractionMode.Physical;
        
        if (isPhysical)
        {
            UpdateExplanation($"INSERT HEAD:\n\n" +
                $"1. Move {sizeBefore} existing node(s) RIGHT (follow yellow arrows)\n" +
                $"2. Press CONFIRM button below\n" +
                $"3. Scan QR code for new HEAD node\n\n" +
                "Follow the yellow arrows and spheres!");
            
            if (tutorialHintText != null)
                tutorialHintText.text = "Physical mode: Move nodes, then press CONFIRM";
        }
        else
        {
            StartCoroutine(UpdateAfterInsert(sizeBefore, "HEAD (beginning)", true));
        }
    }
    
    void OnInsertTailClicked()
    {
        if (listManager == null) return;
        
        int sizeBefore = listManager.GetListSize();
        
        listManager.InsertAtTail();
        
        bool isPhysical = listManager.GetCurrentMode() == QRLinkedListManager.InteractionMode.Physical;
        
        if (isPhysical)
        {
            UpdateExplanation($"INSERT TAIL:\n\n" +
                "No shifting needed - tail insertion!\n" +
                "Simply scan QR code for new TAIL node\n\n" +
                "Complexity: O(n) - Must traverse to end");
            
            if (tutorialHintText != null)
                tutorialHintText.text = "Scan QR code to add to end of list";
        }
        else
        {
            StartCoroutine(UpdateAfterInsert(sizeBefore, "TAIL (end)", false));
        }
    }
    
    void OnInsertMiddleClicked()
    {
        if (listManager == null) return;
        
        int sizeBefore = listManager.GetListSize();
        int position = sizeBefore / 2;
        
        listManager.InsertAtMiddle();
        
        bool isPhysical = listManager.GetCurrentMode() == QRLinkedListManager.InteractionMode.Physical;
        
        if (isPhysical)
        {
            int numToMove = sizeBefore - position;
            UpdateExplanation($"INSERT MIDDLE (position {position}):\n\n" +
                $"1. Move {numToMove} node(s) RIGHT (follow yellow arrows)\n" +
                $"2. Press CONFIRM button below\n" +
                $"3. Scan QR code for new node\n\n" +
                "Follow the yellow arrows!");
            
            if (tutorialHintText != null)
                tutorialHintText.text = "Physical mode: Move nodes, then press CONFIRM";
        }
        else
        {
            StartCoroutine(UpdateAfterInsert(sizeBefore, $"MIDDLE (position {position})", false));
        }
    }
    
    void OnConfirmShiftClicked()
    {
        if (listManager == null) return;
        
        listManager.ConfirmShift();
        
        UpdateExplanation("✅ Shift confirmed!\n\n" +
            "Now scan the QR code for the new node\n" +
            "Place it in the position shown");
        
        if (tutorialHintText != null)
            tutorialHintText.text = "Scan QR code for the new node";
        
        // Hide the confirm button after clicking
        if (confirmShiftButton != null)
            confirmShiftButton.gameObject.SetActive(false);
    }
    
    void OnDeleteHeadClicked()
    {
        if (listManager == null) return;
        
        int sizeBefore = listManager.GetListSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation("❌ List is empty! Nothing to delete.");
            return;
        }
        
        string nodeValue = listManager.GetNodeValue(0);
        listManager.DeleteFromHead();
        StartCoroutine(UpdateAfterDelete(sizeBefore, $"HEAD (Node {nodeValue})", true));
    }
    
    void OnDeleteTailClicked()
    {
        if (listManager == null) return;
        
        int sizeBefore = listManager.GetListSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation("❌ List is empty! Nothing to delete.");
            return;
        }
        
        string nodeValue = listManager.GetNodeValue(sizeBefore - 1);
        listManager.DeleteFromTail();
        StartCoroutine(UpdateAfterDelete(sizeBefore, $"TAIL (Node {nodeValue})", false));
    }
    
    void OnDeleteMiddleClicked()
    {
        if (listManager == null) return;
        
        int sizeBefore = listManager.GetListSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation("❌ List is empty! Nothing to delete.");
            return;
        }
        
        int position = sizeBefore / 2;
        string nodeValue = listManager.GetNodeValue(position);
        listManager.DeleteFromMiddle();
        StartCoroutine(UpdateAfterDelete(sizeBefore, $"MIDDLE position {position} (Node {nodeValue})", false));
    }
    
    System.Collections.IEnumerator UpdateAfterInsert(int sizeBefore, string location, bool isO1)
    {
        yield return new WaitForSeconds(0.6f);
        
        int sizeAfter = listManager.GetListSize();
        
        if (sizeAfter > sizeBefore)
        {
            bool isVirtualMode = listManager.GetCurrentMode() == QRLinkedListManager.InteractionMode.Virtual;
            string modeText = isVirtualMode ? "Virtual mode - auto-created!" : "Physical mode - manually placed!";
            
            UpdateExplanation($"✅ Inserted node at {location}\n{modeText}\nAll nodes shifted to make space!\n\n" +
                $"Complexity: {(isO1 ? "O(1) - Constant time!" : "O(n) - Must traverse list")}\n" +
                $"List size: {sizeAfter} nodes");
            
            if (tutorialHintText != null)
            {
                tutorialHintText.text = isO1 ? "HEAD insertion is the FASTEST - just change head pointer!" : 
                                                "Required traversal to find insertion point";
            }
        }
        else
        {
            UpdateExplanation("❌ Could not insert node!");
        }
    }
    
    System.Collections.IEnumerator UpdateAfterDelete(int sizeBefore, string location, bool isO1)
    {
        yield return new WaitForSeconds(0.6f);
        
        int sizeAfter = listManager.GetListSize();
        
        if (sizeAfter < sizeBefore)
        {
            UpdateExplanation($"✅ Deleted node from {location}\nRemaining nodes shifted left!\n\n" +
                $"Complexity: {(isO1 ? "O(1) - Constant time!" : "O(n) - Traversed to position")}\n" +
                $"List size: {sizeAfter} nodes");
            
            if (tutorialHintText != null)
            {
                tutorialHintText.text = isO1 ? "HEAD deletion is FASTEST!" : 
                                                "Required traversal to reach node";
            }
        }
        else
        {
            UpdateExplanation("❌ Could not delete node!");
        }
    }
    
    void OnTraverseClicked()
    {
        if (listManager == null) return;
        
        listManager.SimulateTraversal();
        UpdateExplanation("🚶 TRAVERSAL operation:\n" +
            "Walking through each node from HEAD to TAIL...\n\n" +
            "This is O(n) - must visit every node\n" +
            "Follow the pointer arrows!");
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "Traversal shows how we follow pointers from node to node";
        }
    }
    
    void OnSearchClicked()
    {
        if (listManager == null) return;
        
        listManager.SimulateSearch();
        UpdateExplanation("🔍 SEARCH operation:\n" +
            "Scanning through each node...\n\n" +
            "This is LINEAR SEARCH - O(n)\n" +
            "Must check nodes one by one!");
        
        if (tutorialHintText != null)
        {
            tutorialHintText.text = "Search in linked lists requires traversal - no shortcuts!";
        }
    }
    
    void OnResetClicked()
    {
        if (listManager == null) return;
        
        listManager.ResetList();
        operationsVisible = false;
        
        if (analysisToggle != null)
        {
            analysisToggle.ForceHidePanel();
        }
        
        if (confirmShiftButton != null)
            confirmShiftButton.gameObject.SetActive(false);
        
        ShowModeSelection();
        
        UpdateExplanation("");
        
        if (tutorialHintText != null)
            tutorialHintText.text = "Choose a mode to start building your linked list!";
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
        if (listManager == null) return;
        
        int size = listManager.GetListSize();
        bool hasNodes = size > 0;
        bool isReady = listManager.IsListReady();
        bool isWaitingForShift = listManager.IsWaitingForShiftConfirmation();
        
        // Show/hide confirm shift button (only in physical mode)
        if (confirmShiftButton != null)
        {
            bool isPhysical = listManager.GetCurrentMode() == QRLinkedListManager.InteractionMode.Physical;
            confirmShiftButton.gameObject.SetActive(isPhysical && isWaitingForShift);
            
            if (isWaitingForShift)
            {
                confirmShiftButton.interactable = true;
                SetButtonColor(confirmShiftButton, true);
            }
        }
        
        // INSERT buttons: Enabled when ready and NOT waiting for shift
        if (insertHeadButton != null)
        {
            insertHeadButton.interactable = isReady && !isWaitingForShift;
            SetButtonColor(insertHeadButton, isReady && !isWaitingForShift);
        }
        
        if (insertTailButton != null)
        {
            insertTailButton.interactable = isReady && !isWaitingForShift;
            SetButtonColor(insertTailButton, isReady && !isWaitingForShift);
        }
        
        if (insertMiddleButton != null)
        {
            insertMiddleButton.interactable = isReady && !isWaitingForShift;
            SetButtonColor(insertMiddleButton, isReady && !isWaitingForShift);
        }
        
        // DELETE buttons: Enabled if has nodes and not waiting
        if (deleteHeadButton != null)
        {
            bool enabled = hasNodes && isReady && !isWaitingForShift;
            deleteHeadButton.interactable = enabled;
            SetButtonColor(deleteHeadButton, enabled);
        }
        
        if (deleteTailButton != null)
        {
            bool enabled = hasNodes && isReady && !isWaitingForShift;
            deleteTailButton.interactable = enabled;
            SetButtonColor(deleteTailButton, enabled);
        }
        
        if (deleteMiddleButton != null)
        {
            bool enabled = hasNodes && isReady && !isWaitingForShift;
            deleteMiddleButton.interactable = enabled;
            SetButtonColor(deleteMiddleButton, enabled);
        }
        
        // TRAVERSE: Enabled if has nodes and not waiting
        if (traverseButton != null)
        {
            bool enabled = hasNodes && isReady && !isWaitingForShift;
            traverseButton.interactable = enabled;
            SetButtonColor(traverseButton, enabled);
        }
        
        // SEARCH: Enabled if has nodes and not waiting
        if (searchButton != null)
        {
            bool enabled = hasNodes && isReady && !isWaitingForShift;
            searchButton.interactable = enabled;
            SetButtonColor(searchButton, enabled);
        }
        
        // RESET: Always enabled
        if (resetButton != null)
        {
            resetButton.interactable = true;
            SetButtonColor(resetButton, true);
        }
        
        // Update status display
        if (statusText != null)
            statusText.text = $"List Size: {size} nodes";
    }
    
    void SetButtonColor(Button button, bool isEnabled)
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
}