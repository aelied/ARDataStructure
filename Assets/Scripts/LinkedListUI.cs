using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LinkedListUI : MonoBehaviour
{
    [Header("References")]
    public LinkedListVisualizer linkedListVisualizer;
    
    [Header("Insert Buttons")]
    public Button insertHeadButton;
    public Button insertTailButton;
    public Button insertMiddleButton;
    
    [Header("Delete Buttons")]
    public Button deleteHeadButton;
    public Button deleteTailButton;
    public Button deleteMiddleButton;
    
    [Header("Other Buttons")]
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
    public TextMeshProUGUI helpText;  //  NEW: Text inside help panel
    
    [Header("Position Input (Optional)")]
    public TMP_InputField positionInputField;
    
    private bool buttonsVisible = false;
    
    void Start()
    {
        if (insertHeadButton != null)
            insertHeadButton.onClick.AddListener(OnInsertHeadClicked);
        
        if (insertTailButton != null)
            insertTailButton.onClick.AddListener(OnInsertTailClicked);
        
        if (insertMiddleButton != null)
            insertMiddleButton.onClick.AddListener(OnInsertMiddleClicked);
        
        if (deleteHeadButton != null)
            deleteHeadButton.onClick.AddListener(OnDeleteHeadClicked);
        
        if (deleteTailButton != null)
            deleteTailButton.onClick.AddListener(OnDeleteTailClicked);
        
        if (deleteMiddleButton != null)
            deleteMiddleButton.onClick.AddListener(OnDeleteMiddleClicked);
        
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
            helpText.text = "Insert adds nodes at head, tail, or middle. Delete removes from head, tail, or middle position.";
        }
    }
    
    void Update()
    {
        if (linkedListVisualizer != null && linkedListVisualizer.IsListPlaced() && !buttonsVisible)
        {
            ShowButtons();
            UpdateExplanation(" Linked List placed! Choose an operation:");
        }
        
        if (buttonsVisible)
        {
            UpdateInfoText();
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
            if (insertHeadButton != null) insertHeadButton.gameObject.SetActive(false);
            if (insertTailButton != null) insertTailButton.gameObject.SetActive(false);
            if (insertMiddleButton != null) insertMiddleButton.gameObject.SetActive(false);
            if (deleteHeadButton != null) deleteHeadButton.gameObject.SetActive(false);
            if (deleteTailButton != null) deleteTailButton.gameObject.SetActive(false);
            if (deleteMiddleButton != null) deleteMiddleButton.gameObject.SetActive(false);
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
            if (insertHeadButton != null) insertHeadButton.gameObject.SetActive(true);
            if (insertTailButton != null) insertTailButton.gameObject.SetActive(true);
            if (insertMiddleButton != null) insertMiddleButton.gameObject.SetActive(true);
            if (deleteHeadButton != null) deleteHeadButton.gameObject.SetActive(true);
            if (deleteTailButton != null) deleteTailButton.gameObject.SetActive(true);
            if (deleteMiddleButton != null) deleteMiddleButton.gameObject.SetActive(true);
            if (clearButton != null) clearButton.gameObject.SetActive(true);
        }
        
        buttonsVisible = true;
        UpdateButtonStates();
    }
    
    void UpdateButtonStates()
    {
        if (linkedListVisualizer == null) return;
        
        int size = GetListSize();
        int maxSize = linkedListVisualizer.maxNodes;
        bool hasElements = size > 0;
        bool canAdd = size < maxSize;
        
        if (insertHeadButton != null)
        {
            insertHeadButton.interactable = canAdd;
            SetButtonColors(insertHeadButton, canAdd);
        }
        
        if (insertTailButton != null)
        {
            insertTailButton.interactable = canAdd;
            SetButtonColors(insertTailButton, canAdd);
        }
        
        if (insertMiddleButton != null)
        {
            insertMiddleButton.interactable = canAdd;
            SetButtonColors(insertMiddleButton, canAdd);
        }
        
        if (deleteHeadButton != null)
        {
            deleteHeadButton.interactable = hasElements;
            SetButtonColors(deleteHeadButton, hasElements);
        }
        
        if (deleteTailButton != null)
        {
            deleteTailButton.interactable = hasElements;
            SetButtonColors(deleteTailButton, hasElements);
        }
        
        if (deleteMiddleButton != null)
        {
            deleteMiddleButton.interactable = hasElements;
            SetButtonColors(deleteMiddleButton, hasElements);
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
    
    void OnInsertHeadClicked()
    {
        if (linkedListVisualizer == null) return;
        
        int sizeBefore = GetListSize();
        int maxSize = linkedListVisualizer.maxNodes;
        
        if (sizeBefore >= maxSize)
        {
            UpdateExplanation($" List is full! Maximum size is {maxSize} nodes.");
            return;
        }
        
        linkedListVisualizer.InsertAtHead();
        StartCoroutine(UpdateAfterInsert(sizeBefore, "HEAD (beginning)"));
    }
    
    void OnInsertTailClicked()
    {
        if (linkedListVisualizer == null) return;
        
        int sizeBefore = GetListSize();
        int maxSize = linkedListVisualizer.maxNodes;
        
        if (sizeBefore >= maxSize)
        {
            UpdateExplanation($" List is full! Maximum size is {maxSize} nodes.");
            return;
        }
        
        linkedListVisualizer.InsertAtTail();
        StartCoroutine(UpdateAfterInsert(sizeBefore, "TAIL (end)"));
    }
    
    void OnInsertMiddleClicked()
    {
        if (linkedListVisualizer == null) return;
        
        int sizeBefore = GetListSize();
        int maxSize = linkedListVisualizer.maxNodes;
        
        if (sizeBefore >= maxSize)
        {
            UpdateExplanation($" List is full! Maximum size is {maxSize} nodes.");
            return;
        }
        
        int position = sizeBefore / 2;
        
        if (positionInputField != null && !string.IsNullOrEmpty(positionInputField.text))
        {
            if (int.TryParse(positionInputField.text, out int inputPos))
            {
                position = Mathf.Clamp(inputPos, 0, sizeBefore);
            }
        }
        
        linkedListVisualizer.InsertAtPosition(position);
        StartCoroutine(UpdateAfterInsert(sizeBefore, $"position {position}"));
    }
    
    System.Collections.IEnumerator UpdateAfterInsert(int sizeBefore, string location)
    {
        yield return new WaitForSeconds(0.1f);
        
        int sizeAfter = GetListSize();
        UpdateInfoText();
        
        if (sizeAfter > sizeBefore)
            UpdateExplanation($" Inserted node at {location}\n All nodes shifted to make space!");
        else
            UpdateExplanation(" Could not insert node!");
    }
    
    void OnDeleteHeadClicked()
    {
        if (linkedListVisualizer == null) return;
        
        int sizeBefore = GetListSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation(" List is empty! Nothing to delete.");
            return;
        }
        
        string nodeValue = linkedListVisualizer.GetNodeValue(0);
        linkedListVisualizer.DeleteFromHead();
        StartCoroutine(UpdateAfterDelete(sizeBefore, $"HEAD (Node {nodeValue})"));
    }
    
    void OnDeleteTailClicked()
    {
        if (linkedListVisualizer == null) return;
        
        int sizeBefore = GetListSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation(" List is empty! Nothing to delete.");
            return;
        }
        
        string nodeValue = linkedListVisualizer.GetNodeValue(sizeBefore - 1);
        linkedListVisualizer.DeleteFromTail();
        StartCoroutine(UpdateAfterDelete(sizeBefore, $"TAIL (Node {nodeValue})"));
    }
    
    void OnDeleteMiddleClicked()
    {
        if (linkedListVisualizer == null) return;
        
        int sizeBefore = GetListSize();
        
        if (sizeBefore == 0)
        {
            UpdateExplanation(" List is empty! Nothing to delete.");
            return;
        }
        
        int position = sizeBefore / 2;
        
        if (positionInputField != null && !string.IsNullOrEmpty(positionInputField.text))
        {
            if (int.TryParse(positionInputField.text, out int inputPos))
            {
                position = Mathf.Clamp(inputPos, 0, sizeBefore - 1);
            }
        }
        
        string nodeValue = linkedListVisualizer.GetNodeValue(position);
        linkedListVisualizer.DeleteAtPosition(position);
        StartCoroutine(UpdateAfterDelete(sizeBefore, $"position {position} (Node {nodeValue})"));
    }
    
    System.Collections.IEnumerator UpdateAfterDelete(int sizeBefore, string location)
    {
        yield return new WaitForSeconds(0.1f);
        
        int sizeAfter = GetListSize();
        UpdateInfoText();
        
        if (sizeAfter < sizeBefore)
            UpdateExplanation($" Deleted node from {location}\n Remaining nodes shifted left!");
        else
            UpdateExplanation(" Could not delete node!");
    }
    
    void OnClearClicked()
    {
        if (linkedListVisualizer == null) return;
        
        linkedListVisualizer.Clear();
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
        if (infoText == null) return;
        
        int size = GetListSize();
        int maxSize = linkedListVisualizer != null ? linkedListVisualizer.maxNodes : 10;
        infoText.text = $"List Size: {size}/{maxSize} nodes";
    }
    
    void UpdateExplanation(string message)
    {
        if (explanationText != null)
        {
            explanationText.text = message;
        }
    }
    
    int GetListSize()
    {
        if (linkedListVisualizer == null) return 0;
        return linkedListVisualizer.Size();
    }
}