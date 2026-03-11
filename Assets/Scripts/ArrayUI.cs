    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    public class ArrayUI : MonoBehaviour
    {
        [Header("References")]
        public ArrayVisualizer arrayVisualizer;
        
        [Header("Buttons")]
        public Button addButton;
        public Button insertAtButton;
        public Button removeButton;
        public Button updateButton;
        public Button accessButton;
        public Button clearButton;
        
        [Header("Input Fields")]
        public TMP_InputField valueInput;
        public TMP_InputField indexInput;
        
        [Header("UI Panels")]
        public GameObject headerPanel;
        public GameObject instructionCard;
        public GameObject buttonPanel;
        public GameObject inputFieldsPanel;
        public GameObject explanationPanel;
        public GameObject helpPanel;  // This is your "ExplanationPanel (1)"
        
        [Header("Info Display")]
        public TextMeshProUGUI infoText;
        public TextMeshProUGUI explanationText;
        public TextMeshProUGUI helpText;  // Text inside the help panel
        
        private bool buttonsVisible = false;
        private int valueCounter = 1;
        private int lastAccessedIndex = -1;
        
        void Start()
        {
            // Connect button events
            if (addButton != null)
                addButton.onClick.AddListener(OnAddClicked);
            
            if (insertAtButton != null)
                insertAtButton.onClick.AddListener(OnInsertAtClicked);
            
            if (removeButton != null)
                removeButton.onClick.AddListener(OnRemoveClicked);
            
            if (updateButton != null)
                updateButton.onClick.AddListener(OnUpdateClicked);
            
            if (accessButton != null)
                accessButton.onClick.AddListener(OnAccessClicked);
            
            if (clearButton != null)
                clearButton.onClick.AddListener(OnClearClicked);
            
            // Show header and instruction initially
            if (headerPanel != null)
                headerPanel.SetActive(true);
            
            if (instructionCard != null)
                instructionCard.SetActive(true);
            
            // Hide ALL interactive elements initially
            if (buttonPanel != null)
                buttonPanel.SetActive(false);
            
            if (inputFieldsPanel != null)
                inputFieldsPanel.SetActive(false);
            
            if (explanationPanel != null)
                explanationPanel.SetActive(false);
            
            if (infoText != null)
                infoText.gameObject.SetActive(false);
            
            // Hide help panel initially - will show after array placement
            if (helpPanel != null)
                helpPanel.SetActive(false);
            
            // Hide individual input fields if not in a panel
            if (valueInput != null)
                valueInput.gameObject.SetActive(false);
            
            if (indexInput != null)
                indexInput.gameObject.SetActive(false);
            
            // Setup help text content
            SetupHelpText();
            
            Debug.Log(" ArrayUI initialized - all interactive elements hidden");
        }
        
        void Update()
        {
            if (arrayVisualizer != null && arrayVisualizer.IsArrayPlaced() && !buttonsVisible)
            {
                ShowButtons();
                UpdateExplanation(" Array placed! Add elements to start building.");
            }
            
            if (buttonsVisible)
            {
                UpdateInfoText();
                UpdateButtonStates();
            }
        }
        
        void SetupHelpText()
        {
            if (helpText != null)
            {
                helpText.text = @"Add a Value to the Node. Input an Index to Insert at. Access by inputing value to update the value";
            }
        }
        
    void UpdateButtonStates()
    {
        int size = GetSize();
        bool hasElements = size > 0;
        bool canAdd = size < arrayVisualizer.GetMaxSize();
        bool hasValidIndex = HasValidIndexInput();
        bool hasValue = HasValue();
        
        // Add button
        if (addButton != null)
        {
            addButton.interactable = canAdd && hasValue;
            SetButtonColors(addButton, canAdd && hasValue);
        }
        
        // Insert At button
        if (insertAtButton != null)
        {
            insertAtButton.interactable = canAdd && hasValidIndex && hasValue;
            SetButtonColors(insertAtButton, canAdd && hasValidIndex && hasValue);
        }
        
        // Remove button
        if (removeButton != null)
        {
            removeButton.interactable = hasElements;
            SetButtonColors(removeButton, hasElements);
        }
        
        // Access button
        if (accessButton != null)
        {
            accessButton.interactable = hasElements;
            SetButtonColors(accessButton, hasElements);
        }
        
        // Update button
        if (updateButton != null)
        {
            updateButton.interactable = hasElements && hasValue;
            SetButtonColors(updateButton, hasElements && hasValue);
        }
        
        // Clear button - always enabled
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
            // Active state: Purple background (#8469FF) with white text
            colors.normalColor = new Color(0.518f, 0.412f, 1f); // #8469FF
            colors.highlightedColor = new Color(0.618f, 0.512f, 1f); // Lighter purple on hover
            colors.pressedColor = new Color(0.318f, 0.212f, 0.7f); // Much darker purple when clicked
            colors.selectedColor = new Color(0.418f, 0.312f, 0.9f); // Selected state
            colors.disabledColor = new Color(0.518f, 0.412f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f; // Faster transition for better feedback
        }
        else
        {
            // Disabled state: Semi-transparent gray
            colors.normalColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.selectedColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.disabledColor = new Color(0.85f, 0.85f, 0.85f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
        }
        
        button.colors = colors;
        
        // Update text color
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = isEnabled ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.6f);
        }
        
        // Update image alpha
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color imgColor = buttonImage.color;
            imgColor.a = isEnabled ? 1f : 0.5f;
            buttonImage.color = imgColor;
        }
    }
        
        bool HasValue()
        {
            if (valueInput == null || string.IsNullOrWhiteSpace(valueInput.text))
                return false;
            
            return true;
        }
        
        bool HasValidIndexInput()
        {
            if (indexInput == null || string.IsNullOrEmpty(indexInput.text))
                return false;
            
            if (int.TryParse(indexInput.text, out int index))
            {
                int size = GetSize();
                return index >= 0 && index <= size;
            }
            
            return false;
        }
        
        void HideButtons()
        {
            if (buttonPanel != null)
            {
                buttonPanel.SetActive(false);
            }
            
            buttonsVisible = false;
        }
        
        void ShowButtons()
        {
            // Hide header and instruction
            if (headerPanel != null)
                headerPanel.SetActive(false);
            
            if (instructionCard != null)
                instructionCard.SetActive(false);
            
            // Show explanation and info
            if (explanationPanel != null)
                explanationPanel.SetActive(true);
            
            if (infoText != null)
                infoText.gameObject.SetActive(true);
            
            // Show input fields
            if (inputFieldsPanel != null)
                inputFieldsPanel.SetActive(true);
            
            if (valueInput != null)
                valueInput.gameObject.SetActive(true);
            
            if (indexInput != null)
                indexInput.gameObject.SetActive(true);
            
            // Show buttons
            if (buttonPanel != null)
            {
                buttonPanel.SetActive(true);
            }
            else
            {
                if (addButton != null) addButton.gameObject.SetActive(true);
                if (insertAtButton != null) insertAtButton.gameObject.SetActive(true);
                if (removeButton != null) removeButton.gameObject.SetActive(true);
                if (updateButton != null) updateButton.gameObject.SetActive(true);
                if (accessButton != null) accessButton.gameObject.SetActive(true);
                if (clearButton != null) clearButton.gameObject.SetActive(true);
            }
            
            // Show help panel after array placement
            if (helpPanel != null)
                helpPanel.SetActive(true);
            
            buttonsVisible = true;
            
            Debug.Log(" Buttons and inputs shown after array placement");
        }
        
        void OnAddClicked()
        {
            if (arrayVisualizer == null) return;
            
            if (valueInput == null || string.IsNullOrWhiteSpace(valueInput.text))
            {
                UpdateExplanation(" Enter a value first!");
                return;
            }
            
            string value = valueInput.text.Trim();
            int sizeBefore = GetSize();
            
            if (sizeBefore >= arrayVisualizer.GetMaxSize())
            {
                UpdateExplanation($" Array is full! Maximum size is {arrayVisualizer.GetMaxSize()}.");
                return;
            }
            
            arrayVisualizer.Add(value);
            valueCounter++;
            
            StartCoroutine(UpdateAfterAdd(sizeBefore, value));
        }
        
        System.Collections.IEnumerator UpdateAfterAdd(int sizeBefore, string value)
        {
            yield return new WaitForEndOfFrame();
            
            int sizeAfter = GetSize();
            UpdateInfoText();
            
            if (sizeAfter > sizeBefore)
                UpdateExplanation($" Added '{value}' at end (index [{sizeAfter - 1}])\n Adding to end is O(1) - very fast!");
            else
                UpdateExplanation(" Failed to add element.");
            
            if (valueInput != null)
                valueInput.text = "";
        }
        
        void OnInsertAtClicked()
        {
            if (arrayVisualizer == null) return;
            
            if (valueInput == null || string.IsNullOrWhiteSpace(valueInput.text))
            {
                UpdateExplanation(" Enter a value first!");
                return;
            }
            
            int sizeBefore = GetSize();
            
            if (sizeBefore >= arrayVisualizer.GetMaxSize())
            {
                UpdateExplanation($" Array is full! Maximum size is {arrayVisualizer.GetMaxSize()}.");
                return;
            }
            
            if (!HasValidIndexInput())
            {
                UpdateExplanation($" Enter a valid index (0 to {sizeBefore}).");
                return;
            }
            
            int index = int.Parse(indexInput.text);
            string value = valueInput.text.Trim();
            
            arrayVisualizer.InsertAt(index, value);
            valueCounter++;
            
            StartCoroutine(UpdateAfterInsertAt(sizeBefore, index, value));
        }
        
        System.Collections.IEnumerator UpdateAfterInsertAt(int sizeBefore, int index, string value)
        {
            yield return new WaitForEndOfFrame();
            
            int sizeAfter = GetSize();
            UpdateInfoText();
            
            if (sizeAfter > sizeBefore)
                UpdateExplanation($" Inserted '{value}' at index [{index}]\n Elements after [{index}] shifted right!");
            else
                UpdateExplanation(" Failed to insert element.");
            
            if (valueInput != null)
                valueInput.text = "";
            if (indexInput != null)
                indexInput.text = "";
        }
        
        void OnRemoveClicked()
        {
            if (arrayVisualizer == null) return;
            
            int sizeBefore = GetSize();
            
            if (sizeBefore == 0)
            {
                UpdateExplanation(" Array is empty! Add elements first.");
                return;
            }
            
            int index = sizeBefore - 1;
            
            arrayVisualizer.RemoveAt(index);
            
            StartCoroutine(UpdateAfterRemove(sizeBefore, index));
        }
        
        System.Collections.IEnumerator UpdateAfterRemove(int sizeBefore, int index)
        {
            yield return new WaitForEndOfFrame();
            
            int sizeAfter = GetSize();
            UpdateInfoText();
            
            if (sizeAfter < sizeBefore)
                UpdateExplanation($" Removed last element (was at [{index}])\n Removing from end is O(1) - efficient!");
            
            if (valueInput != null)
                valueInput.text = "";
        }
        
        void OnUpdateClicked()
        {
            if (arrayVisualizer == null) return;
            
            int size = GetSize();
            
            if (size == 0)
            {
                UpdateExplanation(" Array is empty! Add elements first.");
                return;
            }
            
            if (valueInput == null || string.IsNullOrWhiteSpace(valueInput.text))
            {
                UpdateExplanation(" Enter a new value first!");
                return;
            }
            
            string newValue = valueInput.text.Trim();
            
            if (lastAccessedIndex >= 0 && lastAccessedIndex < size)
            {
                string oldValue = arrayVisualizer.GetAt(lastAccessedIndex);
                arrayVisualizer.UpdateAt(lastAccessedIndex, newValue);
                UpdateExplanation($" Updated index [{lastAccessedIndex}]: '{oldValue}' → '{newValue}'\n Direct access via index - O(1)!");
                lastAccessedIndex = -1;
            }
            else
            {
                int index = size - 1;
                arrayVisualizer.UpdateAt(index, newValue);
                UpdateExplanation($" Updated last element ([{index}]) to '{newValue}'\n Direct access via index - O(1)!");
            }
            
            if (valueInput != null)
                valueInput.text = "";
        }
        
        void OnAccessClicked()
        {
            if (arrayVisualizer == null) return;
            
            int size = GetSize();
            
            if (size == 0)
            {
                UpdateExplanation(" Array is empty! Add elements first.");
                return;
            }
            
            if (valueInput != null && !string.IsNullOrWhiteSpace(valueInput.text))
            {
                string searchValue = valueInput.text.Trim();
                
                int foundIndex = arrayVisualizer.FindValue(searchValue);
                
                if (foundIndex >= 0)
                {
                    lastAccessedIndex = foundIndex;
                    string value = arrayVisualizer.GetAt(foundIndex);
                    UpdateExplanation($" Found '{searchValue}' at index [{foundIndex}]\n Now you can UPDATE it to a new value!");
                    
                    if (valueInput != null)
                        valueInput.text = "";
                }
                else
                {
                    lastAccessedIndex = -1;
                    UpdateExplanation($" Value '{searchValue}' not found in array\n Searched all {size} elements");
                }
            }
            else
            {
                int index = size - 1;
                lastAccessedIndex = index;
                string value = arrayVisualizer.GetAt(index);
                UpdateExplanation($" Accessed last: array[{index}] = '{value}'\n O(1) access - instant retrieval!");
            }
        }
        
        void OnClearClicked()
        {
            if (arrayVisualizer == null) return;
            
            arrayVisualizer.Clear();
            buttonsVisible = false;
            valueCounter = 1;
            lastAccessedIndex = -1;
            
            // Hide help panel FIRST before calling HideButtons
            if (helpPanel != null)
                helpPanel.SetActive(false);
            
            HideButtons();
            
            // Hide all interactive elements
            if (buttonPanel != null)
                buttonPanel.SetActive(false);
            
            if (inputFieldsPanel != null)
                inputFieldsPanel.SetActive(false);
            
            if (valueInput != null)
                valueInput.gameObject.SetActive(false);
            
            if (indexInput != null)
                indexInput.gameObject.SetActive(false);
            
            if (explanationPanel != null)
                explanationPanel.SetActive(false);
            
            if (infoText != null)
                infoText.gameObject.SetActive(false);
            
            // Show header and instruction again
            if (headerPanel != null)
                headerPanel.SetActive(true);
            
            if (instructionCard != null)
                instructionCard.SetActive(true);
            
            Debug.Log("🗑️ UI reset to initial state - help panel hidden");
        }
        
        void UpdateInfoText()
        {
            if (infoText == null || arrayVisualizer == null) return;
            
            int size = GetSize();
            int maxSize = arrayVisualizer.GetMaxSize();
            infoText.text = $"Array Size: {size}/{maxSize}";
        }
        
        void UpdateExplanation(string message)
        {
            if (explanationText != null)
            {
                explanationText.text = message;
            }
        }
        
        int GetSize()
        {
            if (arrayVisualizer == null) return 0;
            return arrayVisualizer.GetSize();
        }
    }