using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 2D Interactive Visualizer for Data Structure Topics
/// Complete working version with horizontal scrolling for trees
/// </summary>
public class Interactive2DVisualizer : MonoBehaviour
{
    [Header("Visualization Container")]
    public GameObject visualizationPanel;
    public Transform nodeContainer;
    public ScrollRect scrollRect;
    
    [Header("Header Components")]
    public TextMeshProUGUI headerTitleText;
    public Button backButton;
    public GameObject headerPanel;
    public TextMeshProUGUI instructionText;
    
    [Header("Node Prefabs")]
    public GameObject queueNodePrefab;
    public GameObject stackNodePrefab;
    public GameObject linkedListNodePrefab;
    public GameObject treeNodePrefab;
    public GameObject graphNodePrefab;
    
    [Header("Control Buttons")]
    public Button addButton;
    public Button removeButton;
    public Button clearButton;
    public TMP_InputField valueInputField;
    public TextMeshProUGUI operationFeedbackText;
    public GameObject feedbackPanel;
    
    [Header("Animation Settings")]
    public float animationDuration = 0.5f;
    public Color highlightColor = Color.yellow;
    public Color normalColor = Color.white;
    
    [Header("Layout Settings")]
    public float nodeSpacing = 120f;
    public float verticalSpacing = 100f;
    public Vector2 startPosition = new Vector2(-300, 0);
    
    private string currentTopicType;
    private List<GameObject> activeNodes = new List<GameObject>();
    private Queue<int> queueData = new Queue<int>();
    private Stack<int> stackData = new Stack<int>();
    private LinkedList<int> linkedListData = new LinkedList<int>();
    private List<int> arrayData = new List<int>();
    private bool isAnimating = false;
    
    // Tree structure
    private TreeNode binaryTreeRoot;
    
    private class TreeNode
    {
        public int value;
        public TreeNode left;
        public TreeNode right;
        public GameObject visualObject;
        public GameObject leftLine;
        public GameObject rightLine;
    }
    
    // Graph structure
    private Dictionary<int, GameObject> graphNodes = new Dictionary<int, GameObject>();
    private Dictionary<string, GameObject> graphEdges = new Dictionary<string, GameObject>();
    private Dictionary<int, List<int>> graphAdjacencyList = new Dictionary<int, List<int>>();
    
    // Linked List arrows
    private List<GameObject> linkedListArrows = new List<GameObject>();
    
    void Start()
    {
        // Verify nodeContainer
        if (nodeContainer == null)
        {
            Debug.LogError("NodeContainer is NULL! Please assign it in the inspector.");
        }
        else
        {
            Debug.Log($"NodeContainer assigned: {nodeContainer.name}");
        }
        
         if (feedbackPanel != null)
            {
                feedbackPanel.SetActive(false);
            }
        
        if (addButton != null)
            addButton.onClick.AddListener(OnAddClicked);
        
        if (removeButton != null)
            removeButton.onClick.AddListener(OnRemoveClicked);
        
        if (clearButton != null)
            clearButton.onClick.AddListener(OnClearClicked);
        
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
            Debug.Log("Visualizer back button listener added");
        }
        
        if (visualizationPanel != null)
            visualizationPanel.SetActive(false);
    }
    
    void OnBackButtonClicked()
    {
        Debug.Log("Visualizer back button clicked");
        HideVisualization();
        
        TopicDetailPanel detailPanel = FindObjectOfType<TopicDetailPanel>();
        if (detailPanel != null && detailPanel.toggleVisualizerButtonText != null)
        {
            detailPanel.toggleVisualizerButtonText.text = "Show Interactive";
        }
    }
    
    public void InitializeVisualization(string topicName)
    {
        currentTopicType = TopicNameConstants.Normalize(topicName);
        
        ClearVisualization();
        
        if (visualizationPanel != null)
            visualizationPanel.SetActive(true);
        
        UpdateHeader();
        SetupControlsForTopic();
        ShowInstructions();
        
        // Enable horizontal scrolling for trees
        if (scrollRect != null)
        {
            if (currentTopicType == TopicNameConstants.TREES)
            {
                scrollRect.horizontal = true;
                scrollRect.vertical = false;
                Debug.Log("Enabled horizontal scrolling for tree");
            }
            else
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = false;
            }
        }
        
        Debug.Log($"Initialized visualization for: {currentTopicType}");
    }
    
    void UpdateHeader()
    {
        if (headerTitleText != null)
        {
            string displayName = GetDisplayName(currentTopicType);
            headerTitleText.text = $"Interactive {displayName} Visualizer";
        }
        
        if (headerPanel != null)
        {
            headerPanel.SetActive(true);
        }
    }
    
    string GetDisplayName(string topicType)
    {
        switch (topicType)
        {
            case TopicNameConstants.QUEUE: return "Queue";
            case TopicNameConstants.STACKS: return "Stack";
            case TopicNameConstants.LINKED_LISTS: return "Linked List";
            case TopicNameConstants.TREES: return "Binary Tree";
            case TopicNameConstants.GRAPHS: return "Graph";
            case TopicNameConstants.ARRAYS: return "Array";
            default: return topicType;
        }
    }
    
    void SetupControlsForTopic()
    {
        switch (currentTopicType)
        {
            case TopicNameConstants.QUEUE:
                SetButtonText(addButton, "Enqueue");
                SetButtonText(removeButton, "Dequeue");
                break;
            
            case TopicNameConstants.STACKS:
                SetButtonText(addButton, "Push");
                SetButtonText(removeButton, "Pop");
                break;
            
            case TopicNameConstants.LINKED_LISTS:
                SetButtonText(addButton, "Add Node");
                SetButtonText(removeButton, "Remove Last");
                break;
            
            case TopicNameConstants.TREES:
                SetButtonText(addButton, "Insert");
                SetButtonText(removeButton, "Clear Tree");
                break;
            
            case TopicNameConstants.GRAPHS:
                SetButtonText(addButton, "Add Node");
                SetButtonText(removeButton, "Add Edge");
                break;

            case TopicNameConstants.ARRAYS:
                SetButtonText(addButton, "Append");
                SetButtonText(removeButton, "Remove Last");
                break;
        }
    }
    
    void SetButtonText(Button button, string text)
    {
        if (button == null) return;
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
            buttonText.text = text;
    }
    
    void ShowInstructions()
    {
        string instructions = GetContextualInstructions();
        
        if (instructionText != null)
        {
            instructionText.text = instructions;
        }
        else
        {
            ShowFeedback(instructions);
        }
    }
    
    string GetContextualInstructions()
    {
        switch (currentTopicType)
        {
            case TopicNameConstants.QUEUE:
                if (queueData.Count == 0)
                    return "QUEUE (First-In-First-Out)\nEnter a number and click 'Enqueue' to add to the back!";
                else
                    return $"Queue has {queueData.Count} item(s). Enqueue adds to back, Dequeue removes from front.";
                
            case TopicNameConstants.STACKS:
                if (stackData.Count == 0)
                    return "STACK (Last-In-First-Out)\nEnter a number and click 'Push' to add to the top!";
                else
                    return $"Stack has {stackData.Count} item(s). Push adds to top, Pop removes from top.";
                
            case TopicNameConstants.LINKED_LISTS:
                if (linkedListData.Count == 0)
                    return "LINKED LIST\nEnter a number and click 'Add Node' to start!";
                else
                    return $"List has {linkedListData.Count} node(s). Each node points to the next.";
                
            case TopicNameConstants.TREES:
                if (binaryTreeRoot == null)
                    return "BINARY SEARCH TREE\nEnter a number to create the root!\nTip: Smaller values go LEFT, larger go RIGHT.\nSwipe left/right to see the full tree.";
                else
                    return $"Tree created! Add more numbers.\nRule: values < parent go LEFT, values > parent go RIGHT.\nTry: 50, 30, 70, 20, 40, 60, 80\nSwipe to see all nodes!";
                
            case TopicNameConstants.GRAPHS:
                if (graphNodes.Count == 0)
                    return "GRAPH (Nodes & Edges)\nSTEP 1: Add nodes by entering numbers";
                else if (graphEdges.Count == 0)
                    return $"{graphNodes.Count} node(s) created!\nSTEP 2: Connect them using 'Add Edge'\nFormat: enter '1-2' to connect node 1 to node 2";
                else
                    return $"Graph: {graphNodes.Count} nodes, {graphEdges.Count} edges\nKeep building! Add more nodes or edges (format: 1-2)";
                
            case TopicNameConstants.ARRAYS:
                if (arrayData.Count == 0)
                    return "ARRAY (Contiguous Memory)\nAn array stores elements side-by-side in memory.\nType a number and click 'Append' to start!";
                else
                    return $"Array Size: {arrayData.Count}\nElements are indexed from 0 to {arrayData.Count - 1}.\nAccess is O(1) - instant access by index!";

            default:
                return "Enter a value to begin";
        }
    }
    
    void OnAddClicked()
    {
        if (isAnimating) return;
        
        if (string.IsNullOrEmpty(valueInputField?.text))
        {
            ShowFeedback("Please enter a value!", true); // RED - Error
            return;
        }
        
        if (!int.TryParse(valueInputField.text, out int value))
        {
            ShowFeedback("Please enter a valid number!", true); // RED - Error
            return;
        }
        
        switch (currentTopicType)
        {
            case TopicNameConstants.QUEUE:
                StartCoroutine(EnqueueAnimation(value));
                break;
            case TopicNameConstants.STACKS:
                StartCoroutine(PushAnimation(value));
                break;
            case TopicNameConstants.LINKED_LISTS:
                StartCoroutine(AddLinkedListNodeAnimation(value));
                break;
            case TopicNameConstants.TREES:
                StartCoroutine(InsertTreeNodeAnimation(value));
                break;
            case TopicNameConstants.GRAPHS:
                StartCoroutine(AddGraphNodeAnimation(value));
                break;
            case TopicNameConstants.ARRAYS:
                StartCoroutine(AppendArrayAnimation(value));
                break;
        }
        
        valueInputField.text = "";
    }
    
    void OnRemoveClicked()
    {
        if (isAnimating) return;
        
        switch (currentTopicType)
        {
            case TopicNameConstants.QUEUE:
                StartCoroutine(DequeueAnimation());
                break;
            case TopicNameConstants.STACKS:
                StartCoroutine(PopAnimation());
                break;
            case TopicNameConstants.LINKED_LISTS:
                StartCoroutine(RemoveLinkedListNodeAnimation());
                break;
            case TopicNameConstants.TREES:
                ClearVisualization();
                ShowFeedback("Tree cleared!", false); // GREEN - Success
                ShowInstructions();
                break;
            case TopicNameConstants.GRAPHS:
                if (!string.IsNullOrEmpty(valueInputField?.text))
                    StartCoroutine(AddGraphEdgeAnimation(valueInputField.text));
                else
                    ShowFeedback("Enter edge format (e.g., 1-2)", true); // RED - Error
                break;
            case TopicNameConstants.ARRAYS:
                StartCoroutine(RemoveArrayAnimation());
                break;
        }
    }
    
    void OnClearClicked()
    {
        ClearVisualization();
        ShowFeedback("Visualization cleared!", false); // GREEN - Success
        ShowInstructions();
    }

    // ========== ARRAY ANIMATIONS ==========
    IEnumerator AppendArrayAnimation(int value)
    {
        isAnimating = true;
        
        if (arrayData.Count >= 10)
        {
            ShowFeedback("Array visuals limited to 10 items!", true); // RED - Error
            isAnimating = false;
            yield break;
        }

        arrayData.Add(value);
        GameObject node = CreateNode(queueNodePrefab, value);
        activeNodes.Add(node);

        Vector2 targetPos = startPosition + new Vector2((activeNodes.Count - 1) * nodeSpacing, 0);
        node.transform.localPosition = targetPos;
        node.transform.localScale = Vector3.zero;

        yield return StartCoroutine(ScaleAnimation(node, Vector3.one, animationDuration));
        
        ShowFeedback($"Appended: {value} at index {arrayData.Count - 1}", false); // GREEN - Success
        ShowInstructions();
        
        isAnimating = false;
    }

    IEnumerator RemoveArrayAnimation()
    {
        if (arrayData.Count == 0)
        {
            ShowFeedback("Array is empty!", true); // RED - Error
            yield break;
        }

        isAnimating = true;

        int index = arrayData.Count - 1;
        int value = arrayData[index];
        
        arrayData.RemoveAt(index);
        GameObject node = activeNodes[index];
        activeNodes.RemoveAt(index);

        yield return StartCoroutine(HighlightNode(node));
        yield return StartCoroutine(ScaleAnimation(node, Vector3.zero, animationDuration));

        Destroy(node);
        
        ShowFeedback($"Removed index {index} (Value: {value})", false); // GREEN - Success
        ShowInstructions();

        isAnimating = false;
    }
    
    // ========== QUEUE ANIMATIONS ==========
    IEnumerator EnqueueAnimation(int value)
    {
        isAnimating = true;
        
        queueData.Enqueue(value);
        GameObject node = CreateNode(queueNodePrefab, value);
        activeNodes.Add(node);
        
        node.transform.localScale = Vector3.zero;
        yield return StartCoroutine(ScaleAnimation(node, Vector3.one, animationDuration));
        
        RepositionQueueNodes();
        ShowFeedback($"Enqueued: {value}", false); // GREEN - Success
        ShowInstructions();
        
        isAnimating = false;
    }
    
    IEnumerator DequeueAnimation()
    {
        if (queueData.Count == 0)
        {
            ShowFeedback("Queue is empty! Add items first.", true); // RED - Error
            yield break;
        }
        
        isAnimating = true;
        
        int value = queueData.Dequeue();
        GameObject node = activeNodes[0];
        activeNodes.RemoveAt(0);
        
        yield return StartCoroutine(HighlightNode(node));
        yield return StartCoroutine(ScaleAnimation(node, Vector3.zero, animationDuration));
        
        Destroy(node);
        RepositionQueueNodes();
        
        ShowFeedback($"Dequeued: {value}", false); // GREEN - Success
        ShowInstructions();
        
        isAnimating = false;
    }
    
    void RepositionQueueNodes()
    {
        for (int i = 0; i < activeNodes.Count; i++)
        {
            Vector2 targetPos = startPosition + new Vector2(i * nodeSpacing, 0);
            StartCoroutine(MoveAnimation(activeNodes[i], targetPos, animationDuration));
        }
    }
    
    // ========== STACK ANIMATIONS ==========
    IEnumerator PushAnimation(int value)
    {
        isAnimating = true;
        
        stackData.Push(value);
        GameObject node = CreateNode(stackNodePrefab, value);
        activeNodes.Add(node);
        
        Vector2 targetPos = new Vector2(0, -150 + (activeNodes.Count - 1) * 80);
        node.transform.localPosition = targetPos + Vector2.up * 100;
        
        yield return StartCoroutine(MoveAnimation(node, targetPos, animationDuration));
        ShowFeedback($"Pushed: {value}", false); // GREEN - Success
        ShowInstructions();
        
        isAnimating = false;
    }
    
    IEnumerator PopAnimation()
    {
        if (stackData.Count == 0)
        {
            ShowFeedback("Stack is empty! Push items first.", true); // RED - Error
            yield break;
        }
        
        isAnimating = true;
        
        int value = stackData.Pop();
        GameObject node = activeNodes[activeNodes.Count - 1];
        activeNodes.RemoveAt(activeNodes.Count - 1);
        
        yield return StartCoroutine(HighlightNode(node));
        
        Vector2 currentPos = node.transform.localPosition;
        Vector2 targetPos = currentPos + Vector2.up * 100;
        yield return StartCoroutine(MoveAnimation(node, targetPos, animationDuration));
        
        Destroy(node);
        ShowFeedback($"Popped: {value}", false); // GREEN - Success
        ShowInstructions();
        
        isAnimating = false;
    }
    
    // ========== LINKED LIST ANIMATIONS ==========
    IEnumerator AddLinkedListNodeAnimation(int value)
    {
        isAnimating = true;
        
        linkedListData.AddLast(value);
        GameObject node = CreateNode(linkedListNodePrefab, value);
        activeNodes.Add(node);
        
        Vector2 targetPos = startPosition + new Vector2((activeNodes.Count - 1) * nodeSpacing, 0);
        node.transform.localPosition = targetPos;
        
        node.transform.localScale = Vector3.zero;
        yield return StartCoroutine(ScaleAnimation(node, Vector3.one, animationDuration));
        
        if (activeNodes.Count > 1)
        {
            GameObject arrow = DrawArrow(activeNodes[activeNodes.Count - 2], node);
            linkedListArrows.Add(arrow);
        }
        
        ShowFeedback($"Added node: {value}", false); // GREEN - Success
        ShowInstructions();
        
        isAnimating = false;
    }
    
    IEnumerator RemoveLinkedListNodeAnimation()
    {
        if (linkedListData.Count == 0)
        {
            ShowFeedback("List is empty!", true); // RED - Error
            yield break;
        }
        
        isAnimating = true;
        
        linkedListData.RemoveLast();
        GameObject node = activeNodes[activeNodes.Count - 1];
        activeNodes.RemoveAt(activeNodes.Count - 1);
        
        if (linkedListArrows.Count > 0)
        {
            GameObject lastArrow = linkedListArrows[linkedListArrows.Count - 1];
            linkedListArrows.RemoveAt(linkedListArrows.Count - 1);
            Destroy(lastArrow);
        }
        
        yield return StartCoroutine(HighlightNode(node));
        yield return StartCoroutine(ScaleAnimation(node, Vector3.zero, animationDuration));
        
        Destroy(node);
        ShowFeedback("Removed last node", false); // GREEN - Success
        ShowInstructions();
        
        isAnimating = false;
    }
    
    // ========== TREE ANIMATIONS ==========
    IEnumerator InsertTreeNodeAnimation(int value)
    {
        isAnimating = true;
        
        if (TreeContainsValue(binaryTreeRoot, value))
        {
            ShowFeedback($"Value {value} already exists!", true); // RED - Error
            isAnimating = false;
            yield break;
        }
        
        GameObject nodeObj = CreateNode(treeNodePrefab, value);
        
        if (binaryTreeRoot == null)
        {
            binaryTreeRoot = new TreeNode { value = value, visualObject = nodeObj };
            nodeObj.transform.localPosition = new Vector2(0, 300);
            nodeObj.transform.localScale = Vector3.zero;
            yield return StartCoroutine(ScaleAnimation(nodeObj, Vector3.one, animationDuration));
            ShowFeedback($"Created root: {value}", false); // GREEN - Success
        }
        else
        {
            TreeNode newNode = new TreeNode { value = value, visualObject = nodeObj };
            bool inserted = InsertIntoTree(binaryTreeRoot, newNode);
            
            if (inserted)
            {
                UpdateTreePositions();
                nodeObj.transform.localScale = Vector3.zero;
                yield return StartCoroutine(ScaleAnimation(nodeObj, Vector3.one, animationDuration));
                ShowFeedback($"Inserted: {value}", false); // GREEN - Success
            }
            else
            {
                Destroy(nodeObj);
                ShowFeedback($"Failed to insert {value}", true); // RED - Error
            }
        }
        
        ShowInstructions();
        isAnimating = false;
    }
    
    bool TreeContainsValue(TreeNode node, int value)
    {
        if (node == null) return false;
        if (node.value == value) return true;
        if (value < node.value) return TreeContainsValue(node.left, value);
        return TreeContainsValue(node.right, value);
    }
    
    bool InsertIntoTree(TreeNode current, TreeNode newNode)
    {
        Debug.Log($"Comparing {newNode.value} with {current.value}");
        
        if (newNode.value < current.value)
        {
            Debug.Log($"{newNode.value} < {current.value} -> Going LEFT");
            if (current.left == null)
            {
                current.left = newNode;
                Debug.Log($"Inserted {newNode.value} to LEFT of {current.value}");
                return true;
            }
            return InsertIntoTree(current.left, newNode);
        }
        else if (newNode.value > current.value)
        {
            Debug.Log($"{newNode.value} > {current.value} -> Going RIGHT");
            if (current.right == null)
            {
                current.right = newNode;
                Debug.Log($"Inserted {newNode.value} to RIGHT of {current.value}");
                return true;
            }
            return InsertIntoTree(current.right, newNode);
        }
        
        return false;
    }
    
    void UpdateTreePositions()
    {
        if (binaryTreeRoot == null) return;
        
        Debug.Log("=== REPOSITIONING TREE (In-Order) ===");
        
        List<TreeNode> inOrderNodes = new List<TreeNode>();
        GetInOrderNodes(binaryTreeRoot, inOrderNodes);
        int totalNodes = inOrderNodes.Count;
        float horizontalSpacing = 80f;
        float startX = -(totalNodes - 1) * horizontalSpacing * 0.5f;
        
        Dictionary<TreeNode, float> xPositions = new Dictionary<TreeNode, float>();
        for (int i = 0; i < totalNodes; i++)
        {
            xPositions[inOrderNodes[i]] = startX + (i * horizontalSpacing);
        }
        
        RepositionTreeNodeInOrder(binaryTreeRoot, xPositions, 0, 300, 100f);
        ExpandContentAreaForTree();
        
        Debug.Log("=== TREE REPOSITIONING COMPLETE ===");
    }
    
    void GetInOrderNodes(TreeNode node, List<TreeNode> list)
    {
        if (node == null) return;
        GetInOrderNodes(node.left, list);
        list.Add(node);
        GetInOrderNodes(node.right, list);
    }

    void RepositionTreeNodeInOrder(TreeNode node, Dictionary<TreeNode, float> xPositions, int depth, float startY, float verticalSpacing)
    {
        if (node == null) return;
        
        float x = xPositions[node];
        float y = startY - (depth * verticalSpacing);
        
        Vector3 newPos = new Vector3(x, y, 0);
        
        if (node.visualObject != null)
        {
            node.visualObject.GetComponent<RectTransform>().anchoredPosition = newPos;
        }
        
        if (node.left != null)
        {
            RepositionTreeNodeInOrder(node.left, xPositions, depth + 1, startY, verticalSpacing);
            if (node.leftLine != null) Destroy(node.leftLine);
            node.leftLine = DrawTreeConnection(node.visualObject, node.left.visualObject);
        }
        
        if (node.right != null)
        {
            RepositionTreeNodeInOrder(node.right, xPositions, depth + 1, startY, verticalSpacing);
            if (node.rightLine != null) Destroy(node.rightLine);
            node.rightLine = DrawTreeConnection(node.visualObject, node.right.visualObject);
        }
    }

    int CalculateTreeDepth(TreeNode node)
    {
        if (node == null) return 0;
        return 1 + Mathf.Max(CalculateTreeDepth(node.left), CalculateTreeDepth(node.right));
    }
    
    int CountTreeNodes(TreeNode node)
    {
        if (node == null) return 0;
        return 1 + CountTreeNodes(node.left) + CountTreeNodes(node.right);
    }
    
    void ExpandContentAreaForTree()
    {
        if (binaryTreeRoot == null || nodeContainer == null) return;
        
        Vector2 bounds = FindTreeBounds(binaryTreeRoot);
        float minX = bounds.x;
        float maxX = bounds.y;
        
        float padding = 100f;
        float totalWidth = Mathf.Max(1080f, (maxX - minX) + padding * 2);
        
        RectTransform contentRect = nodeContainer.GetComponent<RectTransform>();
        if (contentRect != null)
        {
            contentRect.sizeDelta = new Vector2(totalWidth, contentRect.sizeDelta.y);
            Debug.Log($"Expanded content width to: {totalWidth}");
        }
    }
    
    Vector2 FindTreeBounds(TreeNode node)
    {
        if (node == null) return new Vector2(0, 0);
        
        float minX = node.visualObject.GetComponent<RectTransform>().anchoredPosition.x;
        float maxX = minX;
        
        if (node.left != null)
        {
            Vector2 leftBounds = FindTreeBounds(node.left);
            minX = Mathf.Min(minX, leftBounds.x);
            maxX = Mathf.Max(maxX, leftBounds.y);
        }
        
        if (node.right != null)
        {
            Vector2 rightBounds = FindTreeBounds(node.right);
            minX = Mathf.Min(minX, rightBounds.x);
            maxX = Mathf.Max(maxX, rightBounds.y);
        }
        
        return new Vector2(minX, maxX);
    }
    
    GameObject DrawTreeConnection(GameObject from, GameObject to)
    {
        if (from == null || to == null || nodeContainer == null) return null;
        
        GameObject line = new GameObject("TreeLine");
        line.transform.SetParent(nodeContainer, false);
        line.transform.SetAsFirstSibling();
        
        RectTransform rt = line.AddComponent<RectTransform>();
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        
        Image img = line.AddComponent<Image>();
        img.color = new Color(0.8f, 0.8f, 0.8f, 0.6f);
        img.raycastTarget = false;
        
        Vector2 fromPos = from.GetComponent<RectTransform>().anchoredPosition;
        Vector2 toPos = to.GetComponent<RectTransform>().anchoredPosition;
        Vector2 dir = toPos - fromPos;
        float distance = dir.magnitude;
        
        rt.sizeDelta = new Vector2(distance, 3);
        rt.localPosition = fromPos;
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        rt.localScale = Vector3.one;
        
        return line;
    }
    
    // ========== GRAPH ANIMATIONS ==========
    IEnumerator AddGraphNodeAnimation(int value)
    {
        isAnimating = true;
        
        if (graphNodes.ContainsKey(value))
        {
            ShowFeedback($"Node {value} already exists!", true); // RED - Error
            isAnimating = false;
            yield break;
        }
        
        GameObject node = CreateNode(graphNodePrefab, value);
        graphNodes[value] = node;
        graphAdjacencyList[value] = new List<int>();
        
        RepositionGraphNodes();
        
        node.transform.localScale = Vector3.zero;
        yield return StartCoroutine(ScaleAnimation(node, Vector3.one, animationDuration));
        
        ShowFeedback($"Added node: {value}", false); // GREEN - Success
        ShowInstructions();
        
        isAnimating = false;
    }
    
    void RepositionGraphNodes()
    {
        int nodeCount = graphNodes.Count;
        if (nodeCount == 0) return;

        List<int> keys = new List<int>(graphNodes.Keys);
        keys.Sort();

        float angleStep = 360f / Mathf.Max(nodeCount, 1);
        float radius = Mathf.Max(180f, nodeCount * 25f);

        for (int i = 0; i < nodeCount; i++)
        {
            int key = keys[i];
            GameObject node = graphNodes[key];
            
            float angle = i * angleStep;
            Vector2 pos = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius
            );
            
            node.transform.localPosition = pos;
        }

        UpdateGraphEdges();
    }

    void UpdateGraphEdges()
    {
        foreach (var edgeKey in new List<string>(graphEdges.Keys))
        {
            GameObject oldEdge = graphEdges[edgeKey];
            Destroy(oldEdge);

            string[] parts = edgeKey.Split('-');
            int from = int.Parse(parts[0]);
            int to = int.Parse(parts[1]);

            if (graphNodes.ContainsKey(from) && graphNodes.ContainsKey(to))
            {
                graphEdges[edgeKey] = DrawArrow(graphNodes[from], graphNodes[to]);
            }
        }
    }

    IEnumerator AddGraphEdgeAnimation(string edgeString)
    {
        isAnimating = true;
        
        char[] separators = new char[] { '-', ' ', ',', '>', ';' };
        string[] parts = edgeString.Split(separators, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0].Trim(), out int from) || !int.TryParse(parts[1].Trim(), out int to))
        {
            ShowFeedback("Invalid format! Use: 1-2 or 1 2", true); // RED - Error
            isAnimating = false;
            yield break;
        }
        
        if (!graphNodes.ContainsKey(from))
        {
            ShowFeedback($"Node {from} doesn't exist! Add it first.", true); // RED - Error
            isAnimating = false;
            yield break;
        }
        
        if (!graphNodes.ContainsKey(to))
        {
            ShowFeedback($"Node {to} doesn't exist! Add it first.", true); // RED - Error
            isAnimating = false;
            yield break;
        }
        
        string edgeKey = $"{from}-{to}";
        if (graphEdges.ContainsKey(edgeKey))
        {
            ShowFeedback($"Edge {from} to {to} already exists!", true); // RED - Error
            isAnimating = false;
            yield break;
        }
        
        GameObject fromNode = graphNodes[from];
        GameObject toNode = graphNodes[to];
        
        Debug.Log($"Creating edge: {from} -> {to}");
        Debug.Log($"From position: {fromNode.transform.localPosition}");
        Debug.Log($"To position: {toNode.transform.localPosition}");
        
        GameObject edge = DrawArrow(fromNode, toNode);
        if (edge != null)
        {
            graphEdges[edgeKey] = edge;
            graphAdjacencyList[from].Add(to);
            ShowFeedback($"Connected: {from} -> {to}", false); // GREEN - Success
        }
        else
        {
            ShowFeedback("Failed to create edge", true); // RED - Error
        }
        
        ShowInstructions();
        yield return new WaitForSeconds(0.3f);
        
        isAnimating = false;
        
        if (valueInputField != null)
            valueInputField.text = "";
    }
    
    // ========== HELPER METHODS ==========
    GameObject CreateNode(GameObject prefab, int value)
    {
        if (nodeContainer == null)
        {
            Debug.LogError("Cannot create node: nodeContainer is NULL!");
            return null;
        }
        
        GameObject node = Instantiate(prefab != null ? prefab : CreateDefaultNodePrefab(), nodeContainer);
        
        TextMeshProUGUI valueText = node.GetComponentInChildren<TextMeshProUGUI>();
        if (valueText != null)
            valueText.text = value.ToString();
        
        Debug.Log($"Created node: {value} at {node.transform.localPosition}");
        
        return node;
    }
    
    GameObject CreateDefaultNodePrefab()
    {
        GameObject node = new GameObject("Node");
        RectTransform nodeRect = node.AddComponent<RectTransform>();
        nodeRect.sizeDelta = new Vector2(80, 80);
        
        Image bg = node.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.4f, 0.8f);
        bg.raycastTarget = false;
        
        Outline outline = node.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2, 2);
        
        GameObject textObj = new GameObject("Value");
        textObj.transform.SetParent(node.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(80, 80);
        textRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 32;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        
        return node;
    }
    
    GameObject DrawArrow(GameObject from, GameObject to)
    {
        if (from == null || to == null || nodeContainer == null)
        {
            Debug.LogError("Cannot draw arrow: missing references");
            return null;
        }
        
        GameObject arrow = new GameObject("Arrow");
        arrow.transform.SetParent(nodeContainer, false);
        arrow.transform.SetAsFirstSibling();
        
        RectTransform rt = arrow.AddComponent<RectTransform>();
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        
        Image line = arrow.AddComponent<Image>();
        line.color = new Color(1f, 0.6f, 0f, 1f);
        line.raycastTarget = false;
        
        Vector2 fromPos = from.transform.localPosition;
        Vector2 toPos = to.transform.localPosition;
        Vector2 dir = toPos - fromPos;
        float distance = dir.magnitude;
        
        Debug.Log($"Drawing arrow from {fromPos} to {toPos}, distance: {distance}");
        
        if (distance < 1f)
        {
            Debug.LogWarning("Distance too small!");
            distance = 1f;
        }
        
        float nodeRadius = 40f;
        float adjustedDistance = Mathf.Max(distance - (nodeRadius * 2), 10f);
        
        rt.sizeDelta = new Vector2(adjustedDistance, 6);
        rt.localPosition = fromPos + dir.normalized * nodeRadius;
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        rt.localScale = Vector3.one;
        
        Debug.Log($"Arrow created at {rt.localPosition}, rotation: {rt.localRotation.eulerAngles.z}");
        
        return arrow;
    }
    
    IEnumerator MoveAnimation(GameObject obj, Vector2 targetPos, float duration)
    {
        if (obj == null) yield break;
        
        Vector2 startPos = obj.transform.localPosition;
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            obj.transform.localPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        if (obj != null)
            obj.transform.localPosition = targetPos;
    }
    
    IEnumerator ScaleAnimation(GameObject obj, Vector3 targetScale, float duration)
    {
        if (obj == null) yield break;
        
        Vector3 startScale = obj.transform.localScale;
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            obj.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        if (obj != null)
            obj.transform.localScale = targetScale;
    }
    
    IEnumerator HighlightNode(GameObject node)
    {
        if (node == null) yield break;
        
        Image img = node.GetComponent<Image>();
        if (img != null)
        {
            Color originalColor = img.color;
            img.color = highlightColor;
            yield return new WaitForSeconds(0.3f);
            img.color = originalColor;
        }
    }
    
void ShowFeedback(string message, bool isError = false)
{
    if (operationFeedbackText != null && feedbackPanel != null)
    {
        // Show the feedback panel
        feedbackPanel.SetActive(true);
        
        operationFeedbackText.text = message;
        operationFeedbackText.color = Color.white; // Always white text
        
        // Get the Image component from the panel (not the text)
        Image bgImage = feedbackPanel.GetComponent<Image>();
        if (bgImage == null)
        {
            bgImage = feedbackPanel.AddComponent<Image>();
        }
        
        // Set background color based on message type
        bgImage.color = isError ? new Color(0.8f, 0.2f, 0.2f, 1f) : new Color(0.2f, 0.7f, 0.3f, 1f); // Red or Green background
        
        StopCoroutine("ClearFeedback");
        StartCoroutine(ClearFeedback());
    }
    Debug.Log($"Feedback: {message}");
}
    
   IEnumerator ClearFeedback()
{
    yield return new WaitForSeconds(3f);
    if (feedbackPanel != null)
    {
        feedbackPanel.SetActive(false); // Hide the panel
    }
    if (operationFeedbackText != null)
    {
        operationFeedbackText.text = "";
    }
}
    
    public void ClearVisualization()
    {
        foreach (GameObject node in activeNodes)
            if (node != null) Destroy(node);
        activeNodes.Clear();
        
        foreach (GameObject arrow in linkedListArrows)
            if (arrow != null) Destroy(arrow);
        linkedListArrows.Clear();
        
        if (binaryTreeRoot != null)
        {
            DestroyTreeNode(binaryTreeRoot);
            binaryTreeRoot = null;
        }
        
        foreach (var kvp in graphNodes)
            if (kvp.Value != null) Destroy(kvp.Value);
        graphNodes.Clear();
        
        foreach (var kvp in graphEdges)
            if (kvp.Value != null) Destroy(kvp.Value);
        graphEdges.Clear();
        
        graphAdjacencyList.Clear();
        queueData.Clear();
        stackData.Clear();
        linkedListData.Clear();
        arrayData.Clear();
        
        Debug.Log("Visualization cleared");
    }
    
    void DestroyTreeNode(TreeNode node)
    {
        if (node == null) return;
        
        DestroyTreeNode(node.left);
        DestroyTreeNode(node.right);
        
        if (node.leftLine != null) Destroy(node.leftLine);
        if (node.rightLine != null) Destroy(node.rightLine);
        if (node.visualObject != null) Destroy(node.visualObject);
    }
    
    public void HideVisualization()
    {
        if (visualizationPanel != null)
            visualizationPanel.SetActive(false);
        
        Debug.Log("Visualizer hidden");
    }
}