using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using System.Linq;

public class QRGraphManager : MonoBehaviour
{
    public enum GraphType
    {
        Undirected,
        Directed
    }
    
    public enum InteractionMode
    {
        Physical,
        Virtual
    }
    
    [Header("Graph Settings")]
    public GraphType currentGraphType = GraphType.Undirected;
    public InteractionMode currentMode = InteractionMode.Physical;
    public int maxNodes = 10;
    
    [Header("AR References")]
    public ARTrackedImageManager trackedImageManager;
    
    [Header("3D Object Prefabs")]
    public GameObject cubePrefab;
    public GameObject chairPrefab;
    public GameObject coinPrefab;
    public GameObject penPrefab;
    public GameObject bookPrefab;
    
    [Header("Graph Visualization")]
    public GameObject nodeLabelPrefab;
    public Material edgeLineMaterial;
    public Material directedEdgeMaterial;
    
    [Header("Click Indicators")]
    public bool showClickIndicators = true;
    public Color clickIndicatorColor = new Color(1f, 1f, 0f, 0.6f); // Yellow
    public float clickIndicatorSize = 0.15f;
    
    [Header("Algorithm Analysis")]
    public GraphAlgorithmAnalysisManager analysisManager;
    
    [Header("UI References")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI detectionFeedbackText;
    public TextMeshProUGUI modeIndicatorText;
    public TextMeshProUGUI graphTypeIndicatorText;
    
    [Header("Colors")]
    public Color detectingColor = Color.green;
    public Color notDetectingColor = Color.red;
    public Color nodeColor = new Color(0.2f, 0.8f, 1f, 0.9f);
    public Color edgeColor = new Color(0, 1, 1, 0.8f);
    public Color visitedNodeColor = new Color(1f, 0.7f, 0.2f);
    public Color pathHighlightColor = new Color(0f, 1f, 0f);
    
    [Header("Audio")]
    public AudioClip scanSound;
    public AudioClip confirmSound;
    public AudioClip addNodeSound;
    public AudioClip addEdgeSound;
    public AudioClip removeSound;
    public AudioClip errorSound;
    private AudioSource audioSource;
    
    private enum GraphState
    {
        ModeSelection,
        WaitingForFirstNode,
        WaitingToConfirmFirstNode,
        GraphReady,
        WaitingForNewNode,
        WaitingToConfirmNewNode,
        WaitingForEdgeSourceSelection,
        WaitingForEdgeDestSelection,
        WaitingForNodeRemoval,
        WaitingForTraversalStart,
        WaitingForDijkstraDestination
    }
    
    private GraphState currentState = GraphState.ModeSelection;
    
    private class GraphNode
    {
        public int id;
        public Vector3 position;
        public GameObject nodeObject;
        public GameObject nodeLabel;
        public GameObject clickIndicator;
        public string objectName;
        public ARTrackedImage trackedImage;
        public bool isConfirmed;
        public ObjectAnimator animator;
        public bool isVirtual;
        public List<GraphEdge> outgoingEdges = new List<GraphEdge>();
        public List<GraphEdge> incomingEdges = new List<GraphEdge>();
    }
    
    private class GraphEdge
    {
        public GraphNode source;
        public GraphNode destination;
        public GameObject edgeLine;
        public float weight;
        public bool isDirected;
    }
    
    private List<GraphNode> nodes = new List<GraphNode>();
    private List<GraphEdge> edges = new List<GraphEdge>();
    private Dictionary<string, ARTrackedImage> trackedQRCodes = new Dictionary<string, ARTrackedImage>();
    private Dictionary<string, GameObject> objectPrefabs = new Dictionary<string, GameObject>();
    private HashSet<string> processedImages = new HashSet<string>();
    
    // Virtual mode specific
    private string virtualObjectType = "";
    private Vector3 virtualGraphCenter;
    private bool virtualGraphInitialized = false;
    private float virtualNodeSpacing = 0.3f;
    
    // Interaction tracking
    private GraphNode pendingNode;
    private ARTrackedImage pendingNodeImage;
    private GraphNode selectedSourceNode;
    private GraphNode selectedDestNode;
    
    // Dijkstra specific
    private bool waitingForDijkstraDestination = false;
    
    // Visualization
    private Dictionary<GraphNode, GameObject> nodeHighlights = new Dictionary<GraphNode, GameObject>();
    private List<GameObject> pathVisualization = new List<GameObject>();
    
    void Start()
    {
        if (trackedImageManager == null)
            trackedImageManager = FindObjectOfType<ARTrackedImageManager>();
        
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        
        SetupObjectPrefabs();
        UpdateModeIndicators();
        UpdateInstructions();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.gameObject.SetActive(true);
            detectionFeedbackText.text = "Choose mode to begin";
            detectionFeedbackText.color = Color.yellow;
        }
        
        Debug.Log($"Graph Manager Initialized - Mode: {currentMode}, Type: {currentGraphType}");
    }
    
    void SetupObjectPrefabs()
    {
        if (cubePrefab != null) objectPrefabs["cube"] = cubePrefab;
        if (chairPrefab != null) objectPrefabs["chair"] = chairPrefab;
        if (coinPrefab != null) objectPrefabs["coin"] = coinPrefab;
        if (penPrefab != null) objectPrefabs["pen"] = penPrefab;
        if (bookPrefab != null) objectPrefabs["book"] = bookPrefab;
    }
    
    // ═══════════════════════════════════════════════════════════
    // MODE SELECTION
    // ═══════════════════════════════════════════════════════════
    
    public void SetPhysicalMode()
    {
        currentMode = InteractionMode.Physical;
        currentState = GraphState.WaitingForFirstNode;
        UpdateModeIndicators();
        UpdateInstructions();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Physical Mode: Scan first node";
            detectionFeedbackText.color = notDetectingColor;
        }
        
        Debug.Log("Switched to PHYSICAL mode");
    }
    
    public void SetVirtualMode()
    {
        currentMode = InteractionMode.Virtual;
        currentState = GraphState.WaitingForFirstNode;
        UpdateModeIndicators();
        UpdateInstructions();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Virtual Mode: Scan ONE QR for all nodes";
            detectionFeedbackText.color = notDetectingColor;
        }
        
        Debug.Log("Switched to VIRTUAL mode");
    }
    
    public void SetUndirectedGraph()
    {
        currentGraphType = GraphType.Undirected;
        UpdateModeIndicators();
        
        foreach (var edge in edges)
        {
            UpdateEdgeVisualization(edge);
        }
        
        Debug.Log("Graph type set to UNDIRECTED");
    }
    
    public void SetDirectedGraph()
    {
        currentGraphType = GraphType.Directed;
        UpdateModeIndicators();
        
        foreach (var edge in edges)
        {
            UpdateEdgeVisualization(edge);
        }
        
        Debug.Log("Graph type set to DIRECTED");
    }
    
    void UpdateModeIndicators()
    {
        if (modeIndicatorText != null)
        {
            if (currentMode == InteractionMode.Physical)
            {
                modeIndicatorText.text = "PHYSICAL MODE";
                modeIndicatorText.color = new Color(0.2f, 0.8f, 1f);
            }
            else
            {
                modeIndicatorText.text = "VIRTUAL MODE";
                modeIndicatorText.color = new Color(1f, 0.7f, 0.2f);
            }
        }
        
        if (graphTypeIndicatorText != null)
        {
            if (currentGraphType == GraphType.Undirected)
            {
                graphTypeIndicatorText.text = "UNDIRECTED GRAPH";
                graphTypeIndicatorText.color = new Color(0.2f, 1f, 0.8f);
            }
            else
            {
                graphTypeIndicatorText.text = "DIRECTED GRAPH";
                graphTypeIndicatorText.color = new Color(1f, 0.2f, 0.8f);
            }
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // CLICK INDICATORS
    // ═══════════════════════════════════════════════════════════
    
    void ShowClickIndicators()
    {
        if (!showClickIndicators) return;
        
        foreach (var node in nodes)
        {
            if (node.clickIndicator == null)
            {
                CreateClickIndicator(node);
            }
            node.clickIndicator.SetActive(true);
        }
    }
    
    void HideClickIndicators()
    {
        foreach (var node in nodes)
        {
            if (node.clickIndicator != null)
            {
                node.clickIndicator.SetActive(false);
            }
        }
    }
    
    void CreateClickIndicator(GraphNode node)
    {
        // Create pulsing circle under node
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicator.name = $"ClickIndicator_N{node.id}";
        
        // Position below the node
        indicator.transform.position = node.position + Vector3.down * 0.05f;
        indicator.transform.localScale = new Vector3(clickIndicatorSize, 0.01f, clickIndicatorSize);
        
        // Make it transparent and yellow
        Renderer rend = indicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = clickIndicatorColor;
        rend.material = mat;
        
        // Remove collider
        Collider col = indicator.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        // Add pulsing animation
        ClickIndicatorPulse pulse = indicator.AddComponent<ClickIndicatorPulse>();
        pulse.minScale = clickIndicatorSize * 0.8f;
        pulse.maxScale = clickIndicatorSize * 1.2f;
        
        node.clickIndicator = indicator;
        indicator.SetActive(false);
    }
    
    void UpdateClickIndicatorPositions()
    {
        foreach (var node in nodes)
        {
            if (node.clickIndicator != null)
            {
                node.clickIndicator.transform.position = node.position + Vector3.down * 0.05f;
            }
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // AR TRACKING
    // ═══════════════════════════════════════════════════════════
    
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
        
        if (currentMode == InteractionMode.Physical)
        {
            UpdatePhysicalNodePositions();
            UpdateClickIndicatorPositions();
        }
    }
    
    void HandleTapConfirmation(Vector2 screenPosition)
    {
        switch (currentState)
        {
            case GraphState.WaitingToConfirmFirstNode:
                ConfirmFirstNode();
                break;
                
            case GraphState.WaitingToConfirmNewNode:
                ConfirmNewNode();
                break;
                
            case GraphState.WaitingForEdgeSourceSelection:
            case GraphState.WaitingForEdgeDestSelection:
            case GraphState.WaitingForDijkstraDestination:
            case GraphState.WaitingForNodeRemoval:
            case GraphState.WaitingForTraversalStart:
                HandleNodeSelection(screenPosition);
                break;
        }
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
        
        if (currentMode == InteractionMode.Virtual)
        {
            if (currentState == GraphState.WaitingForFirstNode && string.IsNullOrEmpty(virtualObjectType))
            {
                // Accept first scan
            }
            else if (currentState == GraphState.WaitingForFirstNode)
            {
                Debug.Log($"Virtual mode: Already using {virtualObjectType}");
                return;
            }
            else
            {
                return;
            }
        }
        
        bool shouldProcess = false;
        
        switch (currentState)
        {
            case GraphState.WaitingForFirstNode:
            case GraphState.WaitingForNewNode:
                shouldProcess = true;
                break;
                
            default:
                shouldProcess = false;
                break;
        }
        
        if (!shouldProcess)
        {
            Debug.Log($"Skipping {imageName} - state: {currentState}");
            return;
        }
        
        if (currentMode == InteractionMode.Physical && currentState == GraphState.WaitingForNewNode)
        {
            foreach (var node in nodes)
            {
                if (node.objectName.ToLower() == imageName)
                {
                    Debug.Log($"{imageName} already in graph");
                    PlaySound(errorSound);
                    return;
                }
            }
        }
        
        if (processedImages.Contains(imageName) && currentMode == InteractionMode.Physical)
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
        
        if (!trackedQRCodes.ContainsKey(imageName))
        {
            trackedQRCodes[imageName] = trackedImage;
        }
        
        processedImages.Add(imageName);
        
        switch (currentState)
        {
            case GraphState.WaitingForFirstNode:
                ShowFirstNodePreview(imageName, trackedImage);
                break;
                
            case GraphState.WaitingForNewNode:
                ShowNewNodePreview(imageName, trackedImage);
                break;
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // NODE CREATION
    // ═══════════════════════════════════════════════════════════
    
    public void TriggerAddNode()
    {
        if (currentState != GraphState.GraphReady)
        {
            PlaySound(errorSound);
            return;
        }
        
        if (nodes.Count >= maxNodes)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "Maximum nodes reached!";
            return;
        }
        
        if (currentMode == InteractionMode.Virtual)
        {
            if (string.IsNullOrEmpty(virtualObjectType))
            {
                PlaySound(errorSound);
                if (instructionText != null)
                    instructionText.text = "ERROR: No object type set!";
                return;
            }
            
            Vector3 position = CalculateVirtualNodePosition();
            GameObject nodeObj = InstantiateObjectVirtual(virtualObjectType, position);
            
            GraphNode newNode = new GraphNode
            {
                id = nodes.Count,
                position = position,
                nodeObject = nodeObj,
                objectName = virtualObjectType,
                trackedImage = null,
                isConfirmed = true,
                isVirtual = true
            };
            
            ObjectAnimator animator = nodeObj.GetComponent<ObjectAnimator>();
            if (animator == null)
            {
                animator = nodeObj.AddComponent<ObjectAnimator>();
            }
            newNode.animator = animator;
            
            CreateNodeLabel(newNode);
            CreateClickIndicator(newNode);
            nodes.Add(newNode);
            
            PlaySound(addNodeSound);
            
            if (detectionFeedbackText != null)
            {
                detectionFeedbackText.text = $"Node {newNode.id} added!";
                detectionFeedbackText.color = detectingColor;
            }
            
            if (analysisManager != null)
            {
                analysisManager.AnalyzeAddNode(nodes.Count);
            }
            
            UpdateStatusDisplay();
            
            Debug.Log($"Virtual node added. Total nodes: {nodes.Count}");
        }
        else
        {
            currentState = GraphState.WaitingForNewNode;
            
            if (instructionText != null)
                instructionText.text = "Scan NEW QR code to add node";
            
            if (detectionFeedbackText != null)
            {
                detectionFeedbackText.text = "Waiting for QR scan...";
                detectionFeedbackText.color = Color.yellow;
            }
            
            Debug.Log("Waiting for physical QR scan");
        }
    }
    
    void ShowFirstNodePreview(string objectName, ARTrackedImage trackedImage)
    {
        pendingNodeImage = trackedImage;
        
        GameObject previewObj;
        
        if (currentMode == InteractionMode.Physical)
        {
            previewObj = InstantiateObject(objectName, trackedImage);
        }
        else
        {
            virtualObjectType = objectName;
            virtualGraphCenter = trackedImage.transform.position;
            previewObj = InstantiateObjectVirtual(objectName, virtualGraphCenter);
            
            Debug.Log($"Virtual mode: Set object type to {virtualObjectType}");
        }
        
        MakeTransparent(previewObj, 0.7f);
        pendingNode = new GraphNode
        {
            id = nodes.Count,
            position = trackedImage.transform.position,
            nodeObject = previewObj,
            objectName = objectName,
            trackedImage = trackedImage,
            isConfirmed = false,
            isVirtual = (currentMode == InteractionMode.Virtual)
        };
        
        currentState = GraphState.WaitingToConfirmFirstNode;
        
        if (instructionText != null)
        {
            instructionText.text = currentMode == InteractionMode.Physical 
                ? $"Tap the {objectName.ToUpper()} QR to confirm" 
                : $"Tap ANYWHERE to confirm {objectName.ToUpper()} nodes";
        }
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Tap to confirm";
            detectionFeedbackText.color = Color.yellow;
        }
    }
    
    void ConfirmFirstNode()
    {
        if (pendingNode == null) return;
        
        PlaySound(confirmSound);
        MakeOpaque(pendingNode.nodeObject);
        pendingNode.isConfirmed = true;
        
        ObjectAnimator animator = pendingNode.nodeObject.GetComponent<ObjectAnimator>();
        if (animator == null)
        {
            animator = pendingNode.nodeObject.AddComponent<ObjectAnimator>();
        }
        pendingNode.animator = animator;
        
        CreateNodeLabel(pendingNode);
        CreateClickIndicator(pendingNode);
        nodes.Add(pendingNode);
        
        if (currentMode == InteractionMode.Virtual)
        {
            virtualGraphInitialized = true;
        }
        
        currentState = GraphState.GraphReady;
        UpdateInstructions();
        UpdateStatusDisplay();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Graph Ready! Add more nodes";
            detectionFeedbackText.color = detectingColor;
        }
        
        pendingNode = null;
        pendingNodeImage = null;
        
        Debug.Log($"First node confirmed. Total nodes: {nodes.Count}");
    }
    
    void ShowNewNodePreview(string objectName, ARTrackedImage trackedImage)
    {
        GameObject previewObj;
        Vector3 position = trackedImage.transform.position;
        
        previewObj = InstantiateObject(objectName, trackedImage);
        
        MakeTransparent(previewObj, 0.7f);
        pendingNode = new GraphNode
        {
            id = nodes.Count,
            position = position,
            nodeObject = previewObj,
            objectName = objectName,
            trackedImage = trackedImage,
            isConfirmed = false,
            isVirtual = false
        };
        
        pendingNodeImage = trackedImage;
        currentState = GraphState.WaitingToConfirmNewNode;
        
        if (instructionText != null)
        {
            instructionText.text = $"Tap the {objectName.ToUpper()} QR to confirm";
        }
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Tap to confirm";
            detectionFeedbackText.color = Color.yellow;
        }
    }
    
    void ConfirmNewNode()
    {
        if (pendingNode == null) return;
        
        PlaySound(addNodeSound);
        MakeOpaque(pendingNode.nodeObject);
        pendingNode.isConfirmed = true;
        
        ObjectAnimator animator = pendingNode.nodeObject.GetComponent<ObjectAnimator>();
        if (animator == null)
        {
            animator = pendingNode.nodeObject.AddComponent<ObjectAnimator>();
        }
        pendingNode.animator = animator;
        
        CreateNodeLabel(pendingNode);
        CreateClickIndicator(pendingNode);
        nodes.Add(pendingNode);
        
        currentState = GraphState.GraphReady;
        UpdateInstructions();
        UpdateStatusDisplay();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = $"Node {pendingNode.id} added!";
            detectionFeedbackText.color = detectingColor;
        }
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzeAddNode(nodes.Count);
        }
        
        pendingNode = null;
        pendingNodeImage = null;
        
        Debug.Log($"New node confirmed. Total nodes: {nodes.Count}");
    }
    
    Vector3 CalculateVirtualNodePosition()
    {
        if (nodes.Count == 0)
        {
            return virtualGraphCenter;
        }
        
        float angleStep = 360f / (nodes.Count + 1);
        float angle = angleStep * nodes.Count;
        float radius = virtualNodeSpacing;
        
        float x = virtualGraphCenter.x + radius * Mathf.Cos(angle * Mathf.Deg2Rad);
        float z = virtualGraphCenter.z + radius * Mathf.Sin(angle * Mathf.Deg2Rad);
        
        return new Vector3(x, virtualGraphCenter.y, z);
    }
    
    void CreateNodeLabel(GraphNode node)
    {
        if (nodeLabelPrefab == null) return;
        
        node.nodeLabel = Instantiate(nodeLabelPrefab);
        node.nodeLabel.transform.position = node.position + Vector3.up * 0.2f;
        
        TextMeshPro labelText = node.nodeLabel.GetComponentInChildren<TextMeshPro>();
        if (labelText != null)
        {
            labelText.text = $"N{node.id}";
            labelText.color = nodeColor;
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // EDGE OPERATIONS
    // ═══════════════════════════════════════════════════════════
    
    public void StartAddEdge()
    {
        if (currentState != GraphState.GraphReady || nodes.Count < 2)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "Need at least 2 nodes to add edge!";
            return;
        }
        
        currentState = GraphState.WaitingForEdgeSourceSelection;
        ShowClickIndicators();
        
        if (instructionText != null)
            instructionText.text = "TAP the SOURCE node (Yellow circle)";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Select source node";
            detectionFeedbackText.color = Color.yellow;
        }
    }
    
    void HandleNodeSelection(Vector2 screenPosition)
    {
        Camera arCamera = Camera.main;
        if (arCamera == null) return;
        
        GraphNode closestNode = null;
        float minDist = float.MaxValue;
        
        foreach (var node in nodes)
        {
            Vector3 screenPoint = arCamera.WorldToScreenPoint(node.position);
            float distance = Vector2.Distance(screenPosition, new Vector2(screenPoint.x, screenPoint.y));
            
            if (distance < minDist)
            {
                minDist = distance;
                closestNode = node;
            }
        }
        
        float threshold = Screen.width * 0.15f;
        
        if (closestNode != null && minDist < threshold)
        {
            switch (currentState)
            {
                case GraphState.WaitingForEdgeSourceSelection:
                    SelectSourceNode(closestNode);
                    break;
                    
                case GraphState.WaitingForEdgeDestSelection:
                    SelectDestinationNode(closestNode);
                    break;
                    
                case GraphState.WaitingForDijkstraDestination:
                    SelectDijkstraDestination(closestNode);
                    break;
                    
                case GraphState.WaitingForNodeRemoval:
                    RemoveNode(closestNode);
                    break;
                    
                case GraphState.WaitingForTraversalStart:
                    StartTraversalFrom(closestNode);
                    break;
            }
        }
    }
    
    void SelectSourceNode(GraphNode node)
    {
        selectedSourceNode = node;
        HighlightNode(node, Color.cyan);
        PlaySound(confirmSound);
        
        currentState = GraphState.WaitingForEdgeDestSelection;
        
        if (instructionText != null)
            instructionText.text = $"Selected N{node.id}. TAP DESTINATION (Yellow circle)";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Select destination";
            detectionFeedbackText.color = Color.yellow;
        }
    }
    
    void SelectDestinationNode(GraphNode node)
    {
        if (node == selectedSourceNode)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "Cannot connect node to itself!";
            return;
        }
        
        bool edgeExists = edges.Any(e => 
            (e.source == selectedSourceNode && e.destination == node) ||
            (currentGraphType == GraphType.Undirected && e.source == node && e.destination == selectedSourceNode)
        );
        
        if (edgeExists)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "Edge already exists!";
            
            HideClickIndicators();
            ClearNodeHighlights();
            selectedSourceNode = null;
            currentState = GraphState.GraphReady;
            return;
        }
        
        selectedDestNode = node;
        CreateEdge(selectedSourceNode, selectedDestNode);
        
        PlaySound(addEdgeSound);
        HideClickIndicators();
        ClearNodeHighlights();
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzeAddEdge(nodes.Count, edges.Count);
        }
        
        if (instructionText != null)
            instructionText.text = $"Edge added: N{selectedSourceNode.id} → N{selectedDestNode.id}";
        
        selectedSourceNode = null;
        selectedDestNode = null;
        currentState = GraphState.GraphReady;
        UpdateStatusDisplay();
    }
    
    void CreateEdge(GraphNode source, GraphNode dest)
    {
        GraphEdge edge = new GraphEdge
        {
            source = source,
            destination = dest,
            weight = Vector3.Distance(source.position, dest.position),
            isDirected = (currentGraphType == GraphType.Directed)
        };
        
        edge.edgeLine = CreateEdgeLine(source.position, dest.position, edge.isDirected);
        
        source.outgoingEdges.Add(edge);
        dest.incomingEdges.Add(edge);
        
        if (currentGraphType == GraphType.Undirected)
        {
            dest.outgoingEdges.Add(edge);
            source.incomingEdges.Add(edge);
        }
        
        edges.Add(edge);
        
        Debug.Log($"Edge created: N{source.id} → N{dest.id} (Weight: {edge.weight:F2})");
    }
    
    GameObject CreateEdgeLine(Vector3 start, Vector3 end, bool directed)
    {
        GameObject lineObj = new GameObject($"Edge_{edges.Count}");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        
        Material mat = directed && directedEdgeMaterial != null ? directedEdgeMaterial : edgeLineMaterial;
        if (mat != null)
        {
            lr.material = mat;
        }
        else
        {
            lr.material = new Material(Shader.Find("Unlit/Color"));
            lr.material.color = edgeColor;
        }
        
        lr.startColor = edgeColor;
        lr.endColor = edgeColor;
        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;
        lr.useWorldSpace = true;
        
        Vector3 offset = Vector3.up * 0.05f;
        
        if (directed)
        {
            Vector3 direction = (end - start).normalized;
            Vector3 arrowBase = end - direction * 0.05f;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
            
            lr.positionCount = 5;
            lr.SetPosition(0, start + offset);
            lr.SetPosition(1, arrowBase + offset);
            lr.SetPosition(2, arrowBase - perpendicular * 0.02f + offset);
            lr.SetPosition(3, end + offset);
            lr.SetPosition(4, arrowBase + perpendicular * 0.02f + offset);
        }
        else
        {
            lr.positionCount = 2;
            lr.SetPosition(0, start + offset);
            lr.SetPosition(1, end + offset);
        }
        
        return lineObj;
    }
    
    void UpdateEdgeVisualization(GraphEdge edge)
    {
        if (edge.edgeLine != null)
        {
            Destroy(edge.edgeLine);
        }
        
        bool directed = (currentGraphType == GraphType.Directed);
        edge.isDirected = directed;
        edge.edgeLine = CreateEdgeLine(edge.source.position, edge.destination.position, directed);
    }
    
    void UpdateAllEdgePositions()
    {
        foreach (var edge in edges)
        {
            if (edge.edgeLine != null)
            {
                LineRenderer lr = edge.edgeLine.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    Vector3 offset = Vector3.up * 0.05f;
                    Vector3 start = edge.source.position;
                    Vector3 end = edge.destination.position;
                    
                    if (edge.isDirected)
                    {
                        Vector3 direction = (end - start).normalized;
                        Vector3 arrowBase = end - direction * 0.05f;
                        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
                        
                        lr.SetPosition(0, start + offset);
                        lr.SetPosition(1, arrowBase + offset);
                        lr.SetPosition(2, arrowBase - perpendicular * 0.02f + offset);
                        lr.SetPosition(3, end + offset);
                        lr.SetPosition(4, arrowBase + perpendicular * 0.02f + offset);
                    }
                    else
                    {
                        lr.SetPosition(0, start + offset);
                        lr.SetPosition(1, end + offset);
                    }
                }
            }
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // NODE REMOVAL
    // ═══════════════════════════════════════════════════════════
    
    public void StartRemoveNode()
    {
        if (currentState != GraphState.GraphReady || nodes.Count == 0)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "No nodes to remove!";
            return;
        }
        
        currentState = GraphState.WaitingForNodeRemoval;
        ShowClickIndicators();
        
        if (instructionText != null)
            instructionText.text = "TAP node to remove (Yellow circle)";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Select node to remove";
            detectionFeedbackText.color = Color.red;
        }
    }
    
    void RemoveNode(GraphNode node)
    {
        PlaySound(removeSound);
        
        List<GraphEdge> edgesToRemove = new List<GraphEdge>();
        
        foreach (var edge in edges)
        {
            if (edge.source == node || edge.destination == node)
            {
                edgesToRemove.Add(edge);
            }
        }
        
        foreach (var edge in edgesToRemove)
        {
            if (edge.edgeLine != null) Destroy(edge.edgeLine);
            edges.Remove(edge);
        }
        
        if (node.nodeLabel != null) Destroy(node.nodeLabel);
        if (node.nodeObject != null) Destroy(node.nodeObject);
        if (node.clickIndicator != null) Destroy(node.clickIndicator);
        
        nodes.Remove(node);
        
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].id = i;
            UpdateNodeLabel(nodes[i]);
        }
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzeRemoveNode(nodes.Count, edgesToRemove.Count);
        }
        
        HideClickIndicators();
        currentState = GraphState.GraphReady;
        UpdateInstructions();
        UpdateStatusDisplay();
        
        if (instructionText != null)
            instructionText.text = $"Node removed! {edgesToRemove.Count} edges deleted";
        
        Debug.Log($"Node removed. Remaining nodes: {nodes.Count}");
    }
    
    void UpdateNodeLabel(GraphNode node)
    {
        if (node.nodeLabel == null) return;
        
        TextMeshPro labelText = node.nodeLabel.GetComponentInChildren<TextMeshPro>();
        if (labelText != null)
        {
            labelText.text = $"N{node.id}";
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // GRAPH TRAVERSAL ALGORITHMS
    // ═══════════════════════════════════════════════════════════
    
    public void StartBFS()
    {
        if (nodes.Count == 0)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "Graph is empty!";
            return;
        }
        
        currentState = GraphState.WaitingForTraversalStart;
        ShowClickIndicators();
        
        if (instructionText != null)
            instructionText.text = "TAP starting node for BFS (Yellow circle)";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Select BFS start";
            detectionFeedbackText.color = Color.yellow;
        }
    }
    
    public void StartDFS()
    {
        if (nodes.Count == 0)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "Graph is empty!";
            return;
        }
        
        currentState = GraphState.WaitingForTraversalStart;
        ShowClickIndicators();
        
        if (instructionText != null)
            instructionText.text = "TAP starting node for DFS (Yellow circle)";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Select DFS start";
            detectionFeedbackText.color = Color.yellow;
        }
    }
    
    void StartTraversalFrom(GraphNode startNode)
    {
        HideClickIndicators();
        string traversalType = instructionText.text.Contains("BFS") ? "BFS" : "DFS";
        
        if (traversalType == "BFS")
        {
            StartCoroutine(BFSAnimation(startNode));
        }
        else
        {
            StartCoroutine(DFSAnimation(startNode));
        }
        
        currentState = GraphState.GraphReady;
    }
    
    System.Collections.IEnumerator BFSAnimation(GraphNode start)
    {
        if (instructionText != null)
            instructionText.text = "BFS: Exploring level by level...";
        
        Queue<GraphNode> queue = new Queue<GraphNode>();
        HashSet<GraphNode> visited = new HashSet<GraphNode>();
        List<GraphNode> traversalOrder = new List<GraphNode>();
        
        queue.Enqueue(start);
        visited.Add(start);
        
        while (queue.Count > 0)
        {
            GraphNode current = queue.Dequeue();
            traversalOrder.Add(current);
            
            HighlightNode(current, visitedNodeColor);
            if (instructionText != null)
                instructionText.text = $"BFS: Visiting N{current.id}...";
            
            yield return new WaitForSeconds(0.8f);
            
            foreach (var edge in current.outgoingEdges)
            {
                GraphNode neighbor = (edge.source == current) ? edge.destination : edge.source;
                
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    
                    HighlightEdge(edge, pathHighlightColor);
                    yield return new WaitForSeconds(0.4f);
                }
            }
        }
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzeBFS(nodes.Count, edges.Count, traversalOrder.Count);
        }
        
        if (instructionText != null)
            instructionText.text = $"BFS Complete! Visited {traversalOrder.Count} nodes";
        
        yield return new WaitForSeconds(2f);
        ClearAllHighlights();
    }
    
    System.Collections.IEnumerator DFSAnimation(GraphNode start)
    {
        if (instructionText != null)
            instructionText.text = "DFS: Exploring depth first...";
        
        HashSet<GraphNode> visited = new HashSet<GraphNode>();
        List<GraphNode> traversalOrder = new List<GraphNode>();
        
        yield return DFSRecursive(start, visited, traversalOrder);
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzeDFS(nodes.Count, edges.Count, traversalOrder.Count);
        }
        
        if (instructionText != null)
            instructionText.text = $"DFS Complete! Visited {traversalOrder.Count} nodes";
        
        yield return new WaitForSeconds(2f);
        ClearAllHighlights();
    }
    
    System.Collections.IEnumerator DFSRecursive(GraphNode node, HashSet<GraphNode> visited, List<GraphNode> order)
    {
        visited.Add(node);
        order.Add(node);
        
        HighlightNode(node, visitedNodeColor);
        if (instructionText != null)
            instructionText.text = $"DFS: Visiting N{node.id}...";
        
        yield return new WaitForSeconds(0.8f);
        
        foreach (var edge in node.outgoingEdges)
        {
            GraphNode neighbor = (edge.source == node) ? edge.destination : edge.source;
            
            if (!visited.Contains(neighbor))
            {
                HighlightEdge(edge, pathHighlightColor);
                yield return new WaitForSeconds(0.4f);
                
                yield return DFSRecursive(neighbor, visited, order);
            }
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // DIJKSTRA'S SHORTEST PATH
    // ═══════════════════════════════════════════════════════════
    
    public void StartDijkstra()
    {
        if (nodes.Count < 2)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "Need at least 2 nodes!";
            return;
        }
        
        waitingForDijkstraDestination = false;
        currentState = GraphState.WaitingForEdgeSourceSelection;
        ShowClickIndicators();
        
        if (instructionText != null)
            instructionText.text = "TAP SOURCE for shortest path (Yellow circle)";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Select source";
            detectionFeedbackText.color = Color.yellow;
        }
    }
    
    void SelectDijkstraDestination(GraphNode destination)
    {
        if (destination == selectedSourceNode)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "Source and destination cannot be same!";
            return;
        }
        
        HighlightNode(destination, Color.magenta);
        PlaySound(confirmSound);
        HideClickIndicators();
        
        StartCoroutine(DijkstraAnimation(selectedSourceNode, destination));
        
        ClearNodeHighlights();
        selectedSourceNode = null;
        waitingForDijkstraDestination = false;
        currentState = GraphState.GraphReady;
    }
    
    System.Collections.IEnumerator DijkstraAnimation(GraphNode source, GraphNode destination)
    {
        if (instructionText != null)
            instructionText.text = "DIJKSTRA: Finding shortest path...";
        
        Dictionary<GraphNode, float> distances = new Dictionary<GraphNode, float>();
        Dictionary<GraphNode, GraphNode> previous = new Dictionary<GraphNode, GraphNode>();
        HashSet<GraphNode> visited = new HashSet<GraphNode>();
        
        foreach (var node in nodes)
        {
            distances[node] = float.MaxValue;
        }
        distances[source] = 0;
        
        while (visited.Count < nodes.Count)
        {
            GraphNode current = null;
            float minDist = float.MaxValue;
            
            foreach (var node in nodes)
            {
                if (!visited.Contains(node) && distances[node] < minDist)
                {
                    minDist = distances[node];
                    current = node;
                }
            }
            
            if (current == null || minDist == float.MaxValue) break;
            
            visited.Add(current);
            HighlightNode(current, visitedNodeColor);
            
            if (instructionText != null)
                instructionText.text = $"DIJKSTRA: Checking N{current.id} (dist: {distances[current]:F2})";
            
            yield return new WaitForSeconds(0.6f);
            
            if (current == destination) break;
            
            foreach (var edge in current.outgoingEdges)
            {
                GraphNode neighbor = (edge.source == current) ? edge.destination : edge.source;
                
                if (!visited.Contains(neighbor))
                {
                    float newDist = distances[current] + edge.weight;
                    
                    if (newDist < distances[neighbor])
                    {
                        distances[neighbor] = newDist;
                        previous[neighbor] = current;
                        
                        HighlightEdge(edge, Color.yellow);
                        yield return new WaitForSeconds(0.3f);
                    }
                }
            }
        }
        
        List<GraphNode> path = new List<GraphNode>();
        GraphNode pathNode = destination;
        
        while (pathNode != null)
        {
            path.Insert(0, pathNode);
            previous.TryGetValue(pathNode, out pathNode);
        }
        
        ClearAllHighlights();
        
        for (int i = 0; i < path.Count; i++)
        {
            HighlightNode(path[i], pathHighlightColor);
            
            if (i < path.Count - 1)
            {
                var edge = edges.FirstOrDefault(e => 
                    (e.source == path[i] && e.destination == path[i + 1]) ||
                    (e.source == path[i + 1] && e.destination == path[i]));
                
                if (edge != null)
                {
                    HighlightEdge(edge, pathHighlightColor);
                }
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzeDijkstra(nodes.Count, edges.Count, path.Count, distances[destination]);
        }
        
        if (instructionText != null)
            instructionText.text = $"Shortest path: {path.Count} nodes, Distance: {distances[destination]:F2}";
        
        yield return new WaitForSeconds(3f);
        ClearAllHighlights();
    }
    
    // ═══════════════════════════════════════════════════════════
    // VISUALIZATION HELPERS
    // ═══════════════════════════════════════════════════════════
    
    void HighlightNode(GraphNode node, Color color)
    {
        if (nodeHighlights.ContainsKey(node))
        {
            Destroy(nodeHighlights[node]);
        }
        
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        highlight.transform.position = node.position + Vector3.up * 0.12f;
        highlight.transform.localScale = Vector3.one * 0.08f;
        
        Renderer rend = highlight.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Unlit/Color"));
        color.a = 0.7f;
        mat.color = color;
        rend.material = mat;
        
        Collider col = highlight.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        nodeHighlights[node] = highlight;
    }
    
    void HighlightEdge(GraphEdge edge, Color color)
    {
        if (edge.edgeLine != null)
        {
            LineRenderer lr = edge.edgeLine.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.startColor = color;
                lr.endColor = color;
                lr.startWidth = 0.02f;
                lr.endWidth = 0.02f;
            }
        }
    }
    
    void ClearNodeHighlights()
    {
        foreach (var highlight in nodeHighlights.Values)
        {
            if (highlight != null) Destroy(highlight);
        }
        nodeHighlights.Clear();
    }
    
    void ClearAllHighlights()
    {
        ClearNodeHighlights();
        
        foreach (var edge in edges)
        {
            if (edge.edgeLine != null)
            {
                LineRenderer lr = edge.edgeLine.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    lr.startColor = edgeColor;
                    lr.endColor = edgeColor;
                    lr.startWidth = 0.01f;
                    lr.endWidth = 0.01f;
                }
            }
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // OBJECT INSTANTIATION
    // ═══════════════════════════════════════════════════════════
    
    GameObject InstantiateObject(string objectName, ARTrackedImage trackedImage)
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
            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            customScale = Vector3.one * 0.05f;
        }
        
        obj.transform.SetParent(trackedImage.transform);
        obj.transform.localPosition = Vector3.up * 0.08f;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = customScale;
        
        return obj;
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
            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            customScale = Vector3.one * 0.05f;
        }
        
        obj.name = $"VirtualNode_{nodes.Count}";
        obj.transform.position = position + Vector3.up * 0.08f;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = customScale;
        
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
    
    void MakeTransparent(GameObject obj, float alpha)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Color color = rend.material.color;
            color.a = alpha;
            rend.material.color = color;
        }
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
    
    // ═══════════════════════════════════════════════════════════
    // POSITION UPDATES
    // ═══════════════════════════════════════════════════════════
    
    void UpdatePhysicalNodePositions()
    {
        foreach (var node in nodes)
        {
            if (node.trackedImage != null && node.isConfirmed && !node.isVirtual)
            {
                node.position = node.trackedImage.transform.position;
                
                if (node.nodeLabel != null)
                {
                    node.nodeLabel.transform.position = node.position + Vector3.up * 0.2f;
                }
            }
        }
        
        UpdateAllEdgePositions();
    }
    
    // ═══════════════════════════════════════════════════════════
    // UI UPDATES
    // ═══════════════════════════════════════════════════════════
    
    void UpdateInstructions()
    {
        if (instructionText == null) return;
        
        switch (currentState)
        {
            case GraphState.ModeSelection:
                instructionText.text = "Select mode to begin";
                break;
                
            case GraphState.WaitingForFirstNode:
                instructionText.text = currentMode == InteractionMode.Physical 
                    ? "Scan FIRST node's QR code" 
                    : "Scan ONE QR to define all nodes";
                break;
                
            case GraphState.WaitingToConfirmFirstNode:
                instructionText.text = "TAP to confirm first node";
                break;
                
            case GraphState.GraphReady:
                instructionText.text = "Graph ready! Use buttons for operations";
                break;
        }
    }
    
    void UpdateStatusDisplay()
    {
        if (statusText != null)
        {
            statusText.text = $"Nodes: {nodes.Count} | Edges: {edges.Count}";
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // RESET & CLEANUP
    // ═══════════════════════════════════════════════════════════
    
    public void ResetGraph()
    {
        foreach (var node in nodes)
        {
            if (node.nodeLabel != null) Destroy(node.nodeLabel);
            if (node.nodeObject != null) Destroy(node.nodeObject);
            if (node.clickIndicator != null) Destroy(node.clickIndicator);
        }
        
        foreach (var edge in edges)
        {
            if (edge.edgeLine != null) Destroy(edge.edgeLine);
        }
        
        ClearAllHighlights();
        
        nodes.Clear();
        edges.Clear();
        trackedQRCodes.Clear();
        processedImages.Clear();
        
        pendingNode = null;
        pendingNodeImage = null;
        selectedSourceNode = null;
        selectedDestNode = null;
        waitingForDijkstraDestination = false;
        
        virtualObjectType = "";
        virtualGraphInitialized = false;
        currentState = GraphState.ModeSelection;
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Choose mode";
            detectionFeedbackText.color = Color.yellow;
        }
        
        if (statusText != null)
            statusText.text = "Nodes: 0 | Edges: 0";
        
        UpdateInstructions();
        
        if (analysisManager != null)
        {
            analysisManager.ResetCounters();
        }
        
        Debug.Log("Graph reset");
    }
    
    // ═══════════════════════════════════════════════════════════
    // PUBLIC GETTERS
    // ═══════════════════════════════════════════════════════════
    
    public bool IsGraphReady()
    {
        return currentState == GraphState.GraphReady;
    }
    
    public bool IsModeSelected()
    {
        return currentState != GraphState.ModeSelection;
    }
    
    public int GetNodeCount()
    {
        return nodes.Count;
    }
    
    public int GetEdgeCount()
    {
        return edges.Count;
    }
    
    public InteractionMode GetCurrentMode()
    {
        return currentMode;
    }
    
    public GraphType GetGraphType()
    {
        return currentGraphType;
    }
}

// ═══════════════════════════════════════════════════════════
// CLICK INDICATOR PULSE COMPONENT
// ═══════════════════════════════════════════════════════════

public class ClickIndicatorPulse : MonoBehaviour
{
    public float minScale = 0.12f;
    public float maxScale = 0.18f;
    public float pulseSpeed = 2f;
    
    private float timer = 0f;
    
    void Update()
    {
        timer += Time.deltaTime * pulseSpeed;
        float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(timer) + 1f) / 2f);
        transform.localScale = new Vector3(scale, 0.01f, scale);
        
        // Rotate slowly
        transform.Rotate(Vector3.up, 30f * Time.deltaTime);
    }
}