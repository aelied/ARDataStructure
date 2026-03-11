using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class QRTreeModeManager : MonoBehaviour
{
    [Header("AR References")]
    public ARTrackedImageManager trackedImageManager;
    
    [Header("3D Object Prefabs")]
    public GameObject cubePrefab;
    public GameObject chairPrefab;
    public GameObject coinPrefab;
    public GameObject penPrefab;
    public GameObject bookPrefab;
    
    [Header("Virtual Label Prefabs")]
    public GameObject valueLabelPrefab;
    public GameObject connectionLinePrefab;

    [Header("Algorithm Analysis")]
    public TreeAlgorithmAnalysisManager analysisManager;
    
    [Header("UI References")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI detectionFeedbackText;
    public TextMeshProUGUI modeIndicatorText;
    
    [Header("Colors")]
    public Color detectingColor = Color.green;
    public Color notDetectingColor = Color.red;
    public Color parentLineColor = new Color(0.2f, 0.8f, 1f, 0.8f);
    public Color leftLineColor = new Color(0f, 1f, 0.5f, 0.8f);
    public Color rightLineColor = new Color(1f, 0.5f, 0f, 0.8f);
    
    [Header("Audio")]
    public AudioClip scanSound;
    public AudioClip confirmSound;
    public AudioClip insertSound;
    public AudioClip deleteSound;
    public AudioClip searchSound;
    public AudioClip errorSound;
    private AudioSource audioSource;
    
    [Header("Tree Settings")]
    public float horizontalSpacing = 0.25f;
    public float verticalSpacing = 0.20f;
    
    private enum TreeState
    {
        WaitingForRoot,
        WaitingToConfirmRoot,
        TreeReady,
        WaitingToConfirmInsert,
        WaitingForDeleteSelection,
        WaitingForSearchSelection
    }
    
    private TreeState currentState = TreeState.WaitingForRoot;
    
    private class TreeNode
    {
        public int value;
        public Vector3 position;
        public GameObject valueLabel;
        public GameObject objectInstance;
        public string objectName;
        public bool isConfirmed;
        public ObjectAnimator animator;
        
        public TreeNode left;
        public TreeNode right;
        public TreeNode parent;
        
        public GameObject leftLine;
        public GameObject rightLine;
        public int depth;
        public float horizontalOffset;
    }
    
    private TreeNode root = null;
    private Dictionary<string, GameObject> objectPrefabs = new Dictionary<string, GameObject>();
    private HashSet<string> processedImages = new HashSet<string>();
    
    // Virtual mode specific
    private string virtualObjectType = "";
    private Vector3 virtualTreeStartPosition;
    private bool virtualTreeInitialized = false;
    
    private GameObject insertIndicator;
    private List<GameObject> searchIndicators = new List<GameObject>();
    
    private int pendingInsertValue = -1;
    private Vector3 expectedInsertPosition;
    
    private int nodeCount = 0;
    
    void Start()
    {
        if (trackedImageManager == null)
            trackedImageManager = FindObjectOfType<ARTrackedImageManager>();
        
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        
        SetupObjectPrefabs();
        UpdateModeIndicator();
        UpdateInstructions();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.gameObject.SetActive(true);
            detectionFeedbackText.text = "Scan ONE QR to define all tree nodes";
            detectionFeedbackText.color = notDetectingColor;
        }
        
        Debug.Log("QR Tree Manager Initialized - Virtual Mode Only");
    }
    
    void UpdateModeIndicator()
    {
        if (modeIndicatorText != null)
        {
            modeIndicatorText.text = "VIRTUAL MODE";
            modeIndicatorText.color = new Color(1f, 0.7f, 0.2f);
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    void SetupObjectPrefabs()
    {
        if (cubePrefab != null) objectPrefabs["cube"] = cubePrefab;
        if (chairPrefab != null) objectPrefabs["chair"] = chairPrefab;
        if (coinPrefab != null) objectPrefabs["coin"] = coinPrefab;
        if (penPrefab != null) objectPrefabs["pen"] = penPrefab;
        if (bookPrefab != null) objectPrefabs["book"] = bookPrefab;
    }
    
    void OnEnable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }
    
    void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }
    
    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleTapConfirmation(Input.GetTouch(0).position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            HandleTapConfirmation(Input.mousePosition);
        }
    }
    
    void HandleTapConfirmation(Vector2 screenPosition)
    {
        switch (currentState)
        {
            case TreeState.WaitingToConfirmRoot:
                ConfirmRootNode();
                break;
                
            case TreeState.WaitingToConfirmInsert:
                ConfirmInsertNode();
                break;
                
            case TreeState.WaitingForDeleteSelection:
            case TreeState.WaitingForSearchSelection:
                HandleNodeTapSelection(screenPosition);
                break;
        }
    }
    
    void HandleNodeTapSelection(Vector2 screenPosition)
    {
        Camera arCamera = Camera.main;
        if (arCamera == null) return;
        
        TreeNode closestNode = null;
        float minDist = float.MaxValue;
        
        FindClosestNode(root, screenPosition, arCamera, ref closestNode, ref minDist);
        
        float threshold = Screen.width * 0.15f;
        
        if (closestNode != null && minDist < threshold)
        {
            if (currentState == TreeState.WaitingForDeleteSelection)
            {
                HandleDeleteSelection(closestNode);
            }
            else if (currentState == TreeState.WaitingForSearchSelection)
            {
                HandleSearchSelection(closestNode);
            }
        }
    }
    
    void FindClosestNode(TreeNode node, Vector2 screenPosition, Camera arCamera, ref TreeNode closestNode, ref float minDist)
    {
        if (node == null) return;
        
        Vector3 screenPoint = arCamera.WorldToScreenPoint(node.position);
        float distance = Vector2.Distance(screenPosition, new Vector2(screenPoint.x, screenPoint.y));
        
        if (distance < minDist)
        {
            minDist = distance;
            closestNode = node;
        }
        
        FindClosestNode(node.left, screenPosition, arCamera, ref closestNode, ref minDist);
        FindClosestNode(node.right, screenPosition, arCamera, ref closestNode, ref minDist);
    }
    
    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var trackedImage in args.added)
        {
            OnImageDetected(trackedImage);
        }
        
        foreach (var trackedImage in args.updated)
        {
            string imageName = trackedImage.referenceImage.name.ToLower();
            
            if (!processedImages.Contains(imageName) && 
                trackedImage.trackingState == TrackingState.Tracking)
            {
                OnImageDetected(trackedImage);
            }
        }
    }
    
    void OnImageDetected(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name.ToLower();
        Vector3 imagePosition = trackedImage.transform.position;
        
        // Only process first scan for object type
        if (currentState == TreeState.WaitingForRoot && string.IsNullOrEmpty(virtualObjectType))
        {
            // Accept first scan
        }
        else if (currentState == TreeState.WaitingForRoot)
        {
            Debug.Log($"Already using {virtualObjectType}. Ignoring {imageName}");
            return;
        }
        else
        {
            // After setup, no scans needed
            return;
        }
        
        if (processedImages.Contains(imageName))
        {
            Debug.Log($"{imageName} already processed");
            return;
        }
        
        Debug.Log($"Detected: {imageName} at {imagePosition}");
        PlaySound(scanSound);
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = $"Detected: {imageName.ToUpper()}";
            detectionFeedbackText.color = detectingColor;
        }
        
        processedImages.Add(imageName);
        
        if (currentState == TreeState.WaitingForRoot)
        {
            ShowRootNodePreview(imageName, trackedImage);
        }
    }
    
    void ShowRootNodePreview(string objectName, ARTrackedImage trackedImage)
    {
        virtualObjectType = objectName;
        virtualTreeStartPosition = trackedImage.transform.position;
        GameObject previewObj = InstantiateObjectVirtual(objectName, virtualTreeStartPosition);
        
        Debug.Log($"Set object type to {virtualObjectType}");
        
        Renderer[] renderers = previewObj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Color color = rend.material.color;
            color.a = 0.7f;
            rend.material.color = color;
        }
        
        currentState = TreeState.WaitingToConfirmRoot;
        
        if (instructionText != null)
        {
            instructionText.text = $"Tap ANYWHERE to confirm {objectName.ToUpper()} as tree nodes";
        }
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Tap to confirm";
            detectionFeedbackText.color = Color.yellow;
        }
    }
    
    void ConfirmRootNode()
    {
        string objectName = virtualObjectType;
        int value = 50; // Default root value
        
        PlaySound(confirmSound);
        
        GameObject existingObject = GameObject.Find($"VirtualPreview_{objectName}");
        if (existingObject != null)
        {
            MakeOpaque(existingObject);
        }
        
        Vector3 position = virtualTreeStartPosition;
        virtualTreeInitialized = true;
        
        root = CreateTreeNode(value, objectName, position, true, existingObject);
        root.depth = 0;
        root.horizontalOffset = 0;
        nodeCount = 1;
        
        currentState = TreeState.TreeReady;
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Tree Ready!";
            detectionFeedbackText.color = detectingColor;
        }
        
        if (statusText != null)
            statusText.text = $"Nodes: {nodeCount}";
        
        UpdateInstructions();
    }
    
    void ConfirmInsertNode()
    {
        PlaySound(insertSound);
        
        string objectName = virtualObjectType;
        int value = pendingInsertValue;
        Vector3 position = expectedInsertPosition;
        GameObject existingObject = InstantiateObjectVirtual(virtualObjectType, position);
        
        TreeNode newNode = CreateTreeNode(value, objectName, position, true, existingObject);
        
        InsertNode(root, newNode);
        nodeCount++;
        
        if (analysisManager != null)
        {
            int comparisons = CalculateInsertComparisons(root, value);
            analysisManager.AnalyzeInsertOperation(value, comparisons, nodeCount);
        }
        
        if (insertIndicator != null)
        {
            Destroy(insertIndicator);
            insertIndicator = null;
        }
        
        RecalculateTreePositions();
        
        if (statusText != null)
            statusText.text = $"Nodes: {nodeCount}";
        
        currentState = TreeState.TreeReady;
        if (instructionText != null)
            instructionText.text = $"Inserted {value} into tree!";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Insert confirmed!";
            detectionFeedbackText.color = detectingColor;
        }
        
        pendingInsertValue = -1;
    }
    
    void InsertNode(TreeNode current, TreeNode newNode)
    {
        if (newNode.value < current.value)
        {
            if (current.left == null)
            {
                current.left = newNode;
                newNode.parent = current;
                newNode.depth = current.depth + 1;
                CreateConnectionLine(current, newNode, true);
            }
            else
            {
                InsertNode(current.left, newNode);
            }
        }
        else
        {
            if (current.right == null)
            {
                current.right = newNode;
                newNode.parent = current;
                newNode.depth = current.depth + 1;
                CreateConnectionLine(current, newNode, false);
            }
            else
            {
                InsertNode(current.right, newNode);
            }
        }
    }
    
    int CalculateInsertComparisons(TreeNode node, int value)
    {
        if (node == null) return 0;
        
        int comparisons = 1;
        
        if (value < node.value && node.left != null)
        {
            comparisons += CalculateInsertComparisons(node.left, value);
        }
        else if (value >= node.value && node.right != null)
        {
            comparisons += CalculateInsertComparisons(node.right, value);
        }
        
        return comparisons;
    }
    
    void CreateConnectionLine(TreeNode parent, TreeNode child, bool isLeft)
    {
        GameObject line = new GameObject($"Line_{parent.value}_to_{child.value}");
        LineRenderer lr = line.AddComponent<LineRenderer>();
        
        lr.material = new Material(Shader.Find("Unlit/Color"));
        Color lineColor = isLeft ? leftLineColor : rightLineColor;
        lr.material.color = lineColor;
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        
        UpdateConnectionLine(lr, parent, child);
        
        if (isLeft)
            parent.leftLine = line;
        else
            parent.rightLine = line;
    }
    
    void UpdateConnectionLine(LineRenderer lr, TreeNode parent, TreeNode child)
    {
        Vector3 start = parent.position + Vector3.down * 0.05f;
        Vector3 end = child.position + Vector3.up * 0.05f;
        
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
    
    void HandleDeleteSelection(TreeNode node)
    {
        PlaySound(deleteSound);
        
        if (analysisManager != null)
        {
            int comparisons = CalculateSearchComparisons(root, node.value);
            analysisManager.AnalyzeDeleteOperation(node.value, comparisons, nodeCount);
        }
        
        DeleteNode(node);
        nodeCount--;
        
        RecalculateTreePositions();
        
        if (statusText != null)
            statusText.text = $"Nodes: {nodeCount}";
        
        currentState = TreeState.TreeReady;
        if (instructionText != null)
            instructionText.text = $"Deleted node {node.value}!";
    }
    
    void DeleteNode(TreeNode node)
    {
        if (node == null) return;
        
        // Case 1: Leaf node
        if (node.left == null && node.right == null)
        {
            if (node.parent != null)
            {
                if (node.parent.left == node)
                {
                    if (node.parent.leftLine != null) Destroy(node.parent.leftLine);
                    node.parent.left = null;
                }
                else
                {
                    if (node.parent.rightLine != null) Destroy(node.parent.rightLine);
                    node.parent.right = null;
                }
            }
            else
            {
                root = null;
            }
            
            DestroyNodeObjects(node);
        }
        // Case 2: Node with one child
        else if (node.left == null || node.right == null)
        {
            TreeNode child = (node.left != null) ? node.left : node.right;
            
            if (node.parent != null)
            {
                if (node.parent.left == node)
                {
                    if (node.parent.leftLine != null) Destroy(node.parent.leftLine);
                    node.parent.left = child;
                    CreateConnectionLine(node.parent, child, true);
                }
                else
                {
                    if (node.parent.rightLine != null) Destroy(node.parent.rightLine);
                    node.parent.right = child;
                    CreateConnectionLine(node.parent, child, false);
                }
                child.parent = node.parent;
            }
            else
            {
                root = child;
                child.parent = null;
            }
            
            DestroyNodeObjects(node);
        }
        // Case 3: Node with two children
        else
        {
            TreeNode successor = FindMin(node.right);
            
            // Copy successor's value
            node.value = successor.value;
            
            // Update label
            if (node.valueLabel != null)
            {
                TextMeshPro labelText = node.valueLabel.GetComponentInChildren<TextMeshPro>();
                if (labelText != null)
                {
                    labelText.text = successor.value.ToString();
                }
            }
            
            // Delete successor
            DeleteNode(successor);
        }
    }
    
    TreeNode FindMin(TreeNode node)
    {
        while (node.left != null)
        {
            node = node.left;
        }
        return node;
    }
    
    void DestroyNodeObjects(TreeNode node)
    {
        if (node.valueLabel != null) Destroy(node.valueLabel);
        if (node.objectInstance != null) Destroy(node.objectInstance);
        if (node.leftLine != null) Destroy(node.leftLine);
        if (node.rightLine != null) Destroy(node.rightLine);
    }
    
    void HandleSearchSelection(TreeNode targetNode)
    {
        PlaySound(searchSound);
        StartCoroutine(BinarySearchAnimation(targetNode.value));
    }
    
    System.Collections.IEnumerator BinarySearchAnimation(int targetValue)
    {
        if (instructionText != null)
            instructionText.text = $"Searching for {targetValue}...";
        
        TreeNode current = root;
        int comparisons = 0;
        bool found = false;
        
        while (current != null)
        {
            comparisons++;
            
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            highlight.transform.position = current.position + Vector3.up * 0.12f;
            highlight.transform.localScale = Vector3.one * 0.08f;
            
            Renderer rend = highlight.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Unlit/Color"));
            
            if (current.value == targetValue)
            {
                found = true;
                mat.color = new Color(0, 1, 0, 0.9f);
                
                if (instructionText != null)
                    instructionText.text = $"Found {targetValue}!";
            }
            else
            {
                mat.color = new Color(1, 1, 0, 0.7f);
            }
            
            rend.material = mat;
            
            Collider col = highlight.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            searchIndicators.Add(highlight);
            
            yield return new WaitForSeconds(0.8f);
            
            if (found)
            {
                yield return new WaitForSeconds(1f);
                break;
            }
            else
            {
                Destroy(highlight);
                searchIndicators.Remove(highlight);
            }
            
            if (targetValue < current.value)
            {
                current = current.left;
            }
            else if (targetValue > current.value)
            {
                current = current.right;
            }
        }
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzeSearchOperation(targetValue, comparisons, found, nodeCount);
        }
        
        yield return new WaitForSeconds(2f);
        
        foreach (GameObject indicator in searchIndicators)
        {
            if (indicator != null) Destroy(indicator);
        }
        searchIndicators.Clear();
        
        currentState = TreeState.TreeReady;
        if (instructionText != null)
            instructionText.text = found ? $"Search complete! Found {targetValue}" : "Search complete! Not found";
    }
    
    int CalculateSearchComparisons(TreeNode node, int value)
    {
        if (node == null) return 0;
        
        int comparisons = 1;
        
        if (value < node.value)
        {
            comparisons += CalculateSearchComparisons(node.left, value);
        }
        else if (value > node.value)
        {
            comparisons += CalculateSearchComparisons(node.right, value);
        }
        
        return comparisons;
    }
    
    void RecalculateTreePositions()
    {
        if (root == null) return;
        
        CalculateHorizontalOffsets(root, 0);
        UpdateVirtualNodePositions(root, virtualTreeStartPosition);
    }
    
    void CalculateHorizontalOffsets(TreeNode node, float offset)
    {
        if (node == null) return;
        
        node.horizontalOffset = offset;
        
        float spacing = horizontalSpacing / Mathf.Pow(2, node.depth);
        
        if (node.left != null)
        {
            CalculateHorizontalOffsets(node.left, offset - spacing);
        }
        
        if (node.right != null)
        {
            CalculateHorizontalOffsets(node.right, offset + spacing);
        }
    }
    
    void UpdateVirtualNodePositions(TreeNode node, Vector3 rootPosition)
    {
        if (node == null) return;
        
        node.position = rootPosition + 
                       new Vector3(node.horizontalOffset, -node.depth * verticalSpacing, 0);
        
        if (node.objectInstance != null)
        {
            node.objectInstance.transform.position = node.position + Vector3.up * 0.08f;
            
            if (node.animator != null)
            {
                node.animator.UpdateStartPosition();
            }
        }
        
        if (node.valueLabel != null)
        {
            node.valueLabel.transform.position = node.position + Vector3.up * 0.25f;
        }
        
        if (node.parent != null)
        {
            if (node.parent.left == node && node.parent.leftLine != null)
            {
                UpdateConnectionLine(node.parent.leftLine.GetComponent<LineRenderer>(), node.parent, node);
            }
            else if (node.parent.right == node && node.parent.rightLine != null)
            {
                UpdateConnectionLine(node.parent.rightLine.GetComponent<LineRenderer>(), node.parent, node);
            }
        }
        
        UpdateVirtualNodePositions(node.left, rootPosition);
        UpdateVirtualNodePositions(node.right, rootPosition);
    }
    
    void MakeOpaque(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Color color = rend.material.color;
            color.a = 1f;
            rend.material.color = color;
        }
    }
    
    GameObject InstantiateObjectVirtual(string objectName, Vector3 position)
    {
        GameObject obj = null;
        Vector3 customScale = Vector3.one * 0.05f;
        
        if (objectPrefabs.ContainsKey(objectName))
        {
            obj = Instantiate(objectPrefabs[objectName]);
            customScale = GetScaleForObject(objectName);
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            customScale = Vector3.one * 0.05f;
        }
        
        obj.name = $"VirtualPreview_{objectName}";
        obj.transform.position = position + Vector3.up * 0.08f;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = customScale;
        obj.AddComponent<ObjectAnimator>();
        
        return obj;
    }
    
    Vector3 GetScaleForObject(string objectName)
    {
        switch (objectName.ToLower())
        {
            case "cube": return Vector3.one * 0.03f;
            case "coin": return Vector3.one * 0.08f;
            case "chair": return Vector3.one * 0.06f;
            case "pen": return Vector3.one * 0.08f;
            case "book": return Vector3.one * 0.08f;
            default: return Vector3.one * 0.05f;
        }
    }
    
    TreeNode CreateTreeNode(int value, string objectName, Vector3 position, bool confirmed, GameObject existingObject = null)
    {
        GameObject objectInstance = existingObject;
        
        if (objectInstance == null)
        {
            objectInstance = InstantiateObjectVirtual(objectName, position);
        }
        
        ObjectAnimator animator = objectInstance.GetComponent<ObjectAnimator>();
        if (animator == null)
        {
            animator = objectInstance.AddComponent<ObjectAnimator>();
        }
        
        TreeNode node = new TreeNode
        {
            value = value,
            position = position,
            objectName = objectName,
            objectInstance = objectInstance,
            isConfirmed = confirmed,
            animator = animator
        };
        
        if (valueLabelPrefab != null)
        {
            node.valueLabel = Instantiate(valueLabelPrefab);
            node.valueLabel.transform.position = position + Vector3.up * 0.25f;
            
            TextMeshPro labelText = node.valueLabel.GetComponentInChildren<TextMeshPro>();
            if (labelText != null)
            {
                labelText.text = value.ToString();
            }
        }
        
        return node;
    }
    
    public void SimulateInsertValue(int value)
    {
        if (currentState != TreeState.TreeReady) return;
        
        if (root == null)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "Tree is empty! Initialize first";
            return;
        }
        
        pendingInsertValue = value;
        expectedInsertPosition = CalculateInsertPosition(root, value);
        CreateInsertIndicator(expectedInsertPosition);
        
        currentState = TreeState.WaitingToConfirmInsert;
        
        if (instructionText != null)
            instructionText.text = $"Tap to insert value {value}";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Tap to confirm insert";
            detectionFeedbackText.color = Color.yellow;
        }
    }
    
    Vector3 CalculateInsertPosition(TreeNode node, int value)
    {
        if (value < node.value)
        {
            if (node.left == null)
            {
                float offset = horizontalSpacing / Mathf.Pow(2, node.depth + 1);
                return node.position + new Vector3(-offset, -verticalSpacing, 0);
            }
            else
            {
                return CalculateInsertPosition(node.left, value);
            }
        }
        else
        {
            if (node.right == null)
            {
                float offset = horizontalSpacing / Mathf.Pow(2, node.depth + 1);
                return node.position + new Vector3(offset, -verticalSpacing, 0);
            }
            else
            {
                return CalculateInsertPosition(node.right, value);
            }
        }
    }
    
    void CreateInsertIndicator(Vector3 position)
    {
        if (insertIndicator != null) Destroy(insertIndicator);
        
        insertIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        insertIndicator.transform.position = position + Vector3.up * 0.03f;
        insertIndicator.transform.localScale = Vector3.one * 0.05f;
        
        Renderer rend = insertIndicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0, 1, 0, 0.95f);
        rend.material = mat;
        
        Collider col = insertIndicator.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);
    }
    
    public void SimulateDelete()
    {
        if (currentState != TreeState.TreeReady || root == null) return;
        
        currentState = TreeState.WaitingForDeleteSelection;
        
        if (instructionText != null)
            instructionText.text = "TAP the node to delete";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Tap node";
            detectionFeedbackText.color = notDetectingColor;
        }
    }
    
    public void SimulateSearch()
    {
        if (root == null) return;
        
        currentState = TreeState.WaitingForSearchSelection;
        
        if (instructionText != null)
            instructionText.text = "TAP node to search for";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Tap node";
            detectionFeedbackText.color = notDetectingColor;
        }
    }
    
    public void SimulateTraversal(string traversalType)
    {
        if (root == null) return;
        StartCoroutine(TraversalAnimation(traversalType));
    }
    
    System.Collections.IEnumerator TraversalAnimation(string traversalType)
    {
        if (instructionText != null)
            instructionText.text = $"{traversalType} Traversal in progress...";
        
        List<TreeNode> visitOrder = new List<TreeNode>();
        
        switch (traversalType.ToLower())
        {
            case "inorder":
                InOrderTraversal(root, visitOrder);
                break;
            case "preorder":
                PreOrderTraversal(root, visitOrder);
                break;
            case "postorder":
                PostOrderTraversal(root, visitOrder);
                break;
            case "levelorder":
                LevelOrderTraversal(root, visitOrder);
                break;
        }
        
        foreach (TreeNode node in visitOrder)
        {
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            highlight.transform.position = node.position + Vector3.up * 0.12f;
            highlight.transform.localScale = Vector3.one * 0.08f;
            
            Renderer rend = highlight.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0, 1, 1, 0.9f);
            rend.material = mat;
            
            Collider col = highlight.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            searchIndicators.Add(highlight);
            
            yield return new WaitForSeconds(0.6f);
            
            Destroy(highlight);
            searchIndicators.Remove(highlight);
        }
        
        if (instructionText != null)
            instructionText.text = $"{traversalType} Traversal complete!";
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzeTraversalOperation(traversalType, nodeCount);
        }
    }
    
    void InOrderTraversal(TreeNode node, List<TreeNode> result)
    {
        if (node == null) return;
        InOrderTraversal(node.left, result);
        result.Add(node);
        InOrderTraversal(node.right, result);
    }
    
    void PreOrderTraversal(TreeNode node, List<TreeNode> result)
    {
        if (node == null) return;
        result.Add(node);
        PreOrderTraversal(node.left, result);
        PreOrderTraversal(node.right, result);
    }
    
    void PostOrderTraversal(TreeNode node, List<TreeNode> result)
    {
        if (node == null) return;
        PostOrderTraversal(node.left, result);
        PostOrderTraversal(node.right, result);
        result.Add(node);
    }
    
    void LevelOrderTraversal(TreeNode node, List<TreeNode> result)
    {
        if (node == null) return;
        
        Queue<TreeNode> queue = new Queue<TreeNode>();
        queue.Enqueue(node);
        
        while (queue.Count > 0)
        {
            TreeNode current = queue.Dequeue();
            result.Add(current);
            
            if (current.left != null) queue.Enqueue(current.left);
            if (current.right != null) queue.Enqueue(current.right);
        }
    }
    
    void UpdateInstructions()
    {
        if (instructionText == null) return;
        
        switch (currentState)
        {
            case TreeState.WaitingForRoot:
                instructionText.text = "Scan ONE QR to define all tree nodes";
                break;
                
            case TreeState.WaitingToConfirmRoot:
                instructionText.text = "TAP to confirm root node";
                break;
                
            case TreeState.TreeReady:
                instructionText.text = "Tree ready! Use buttons to perform operations";
                break;
        }
    }
    
    public void ResetTree()
    {
        DestroyTree(root);
        root = null;
        
        if (insertIndicator != null) Destroy(insertIndicator);
        
        foreach (GameObject indicator in searchIndicators)
        {
            if (indicator != null) Destroy(indicator);
        }
        searchIndicators.Clear();
        
        processedImages.Clear();
        
        virtualObjectType = "";
        virtualTreeInitialized = false;
        nodeCount = 0;
        currentState = TreeState.WaitingForRoot;
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Scan ONE QR to start";
            detectionFeedbackText.color = notDetectingColor;
        }
        
        if (statusText != null)
            statusText.text = "Nodes: 0";
        
        UpdateInstructions();
        
        if (analysisManager != null)
        {
            analysisManager.ResetCounters();
        }
        
        Debug.Log("Tree reset");
    }
    
    void DestroyTree(TreeNode node)
    {
        if (node == null) return;
        
        DestroyTree(node.left);
        DestroyTree(node.right);
        DestroyNodeObjects(node);
    }
    
    public int GetNodeCount()
    {
        return nodeCount;
    }
    
    public bool IsTreeReady()
    {
        return currentState == TreeState.TreeReady;
    }
    
    public string GetVirtualObjectType()
    {
        return virtualObjectType;
    }
    
    public int GetTreeHeight()
    {
        return CalculateHeight(root);
    }
    
    int CalculateHeight(TreeNode node)
    {
        if (node == null) return 0;
        
        int leftHeight = CalculateHeight(node.left);
        int rightHeight = CalculateHeight(node.right);
        
        return Mathf.Max(leftHeight, rightHeight) + 1;
    }
}