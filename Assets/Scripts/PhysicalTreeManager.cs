using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class PhysicalTreeManager : MonoBehaviour
{
    [Header("Tree Settings")]
    public int maxNodes = 15;
    public float horizontalSpacing = 0.12f;
    public float verticalSpacing = 0.15f;
    
    [Header("Virtual Label Prefabs")]
    public GameObject nodeLabelPrefab;
    public GameObject rootMarkerPrefab;
    
    [Header("AR References")]
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;
    
    [Header("UI References")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI detectionFeedbackText;
    
    [Header("Colors")]
    public Color detectingColor = Color.green;
    public Color notDetectingColor = Color.red;
    public Color edgeColor = Color.yellow;
    
    private enum TreeState
    {
        WaitingForSurface,
        SurfaceDetected,
        PlacingRoot,
        TreeReady,
        SelectingParent,
        PlacingChild
    }
    
    private TreeState currentState = TreeState.WaitingForSurface;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private Vector3 surfacePosition;
    private bool surfacePlaced = false;
    private bool isSurfaceDetected = false;
    
    private class TreeNode
    {
        public int id;
        public string value;
        public Vector3 position;
        public GameObject nodeLabel;
        public GameObject valueLabel;
        public TreeNode parent;
        public List<TreeNode> children = new List<TreeNode>();
        public int level;
        public int childIndex;
    }
    
    private TreeNode root;
    private List<TreeNode> allNodes = new List<TreeNode>();
    private GameObject rootMarker;
    private List<GameObject> edges = new List<GameObject>();
    private int nodeIdCounter = 0;
    
    private TreeNode selectedParent;
    private Vector3 expectedChildPosition;
    private GameObject childIndicator;
    
    void Start()
    {
        if (planeManager == null)
            planeManager = FindObjectOfType<ARPlaneManager>();
        
        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>();
        
        UpdateInstructions();
        
        if (detectionFeedbackText != null)
            detectionFeedbackText.gameObject.SetActive(true);
        
        Debug.Log("🌳 Physical Tree Manager Initialized");
    }
    
    void Update()
    {
        switch (currentState)
        {
            case TreeState.WaitingForSurface:
                CheckForSurfaceDetection();
                break;
                
            case TreeState.SurfaceDetected:
                CheckForSurfacePlacement();
                break;
                
            case TreeState.PlacingRoot:
                DetectRootNode();
                break;
                
            case TreeState.PlacingChild:
                DetectChildNode();
                break;
                
            case TreeState.SelectingParent:
            case TreeState.TreeReady:
                break;
        }
    }
    
    void CheckForSurfaceDetection()
    {
        if (raycastManager == null) return;
        
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        bool hasPlanes = planeManager != null && planeManager.trackables.count > 0;
        bool detecting = raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);
        
        if (detecting != isSurfaceDetected)
        {
            isSurfaceDetected = detecting;
            UpdateDetectionFeedback(detecting, hasPlanes);
        }
        
        if (detecting)
            currentState = TreeState.SurfaceDetected;
    }
    
    void UpdateDetectionFeedback(bool detecting, bool hasPlanes)
    {
        if (detectionFeedbackText == null) return;
        
        if (detecting)
        {
            detectionFeedbackText.text = "✅ Surface Detected - Tap to place";
            detectionFeedbackText.color = detectingColor;
        }
        else if (!hasPlanes)
        {
            detectionFeedbackText.text = "🔍 Scan environment slowly";
            detectionFeedbackText.color = notDetectingColor;
        }
        else
        {
            detectionFeedbackText.text = "📱 Point at detected surface";
            detectionFeedbackText.color = notDetectingColor;
        }
    }
    
    void CheckForSurfacePlacement()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            PlaceSurface(Input.GetTouch(0).position);
        else if (Input.GetMouseButtonDown(0))
            PlaceSurface(Input.mousePosition);
    }
    
    void PlaceSurface(Vector2 touchPosition)
    {
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            surfacePosition = hits[0].pose.position;
            surfacePlaced = true;
            
            HidePlanes();
            
            if (detectionFeedbackText != null)
                detectionFeedbackText.gameObject.SetActive(false);
            
            currentState = TreeState.PlacingRoot;
            UpdateInstructions();
            
            Debug.Log("✅ Surface placed");
        }
    }
    
    void DetectRootNode()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            TapRootNode(Input.GetTouch(0).position);
        else if (Input.GetMouseButtonDown(0))
            TapRootNode(Input.mousePosition);
    }
    
    void TapRootNode(Vector2 touchPosition)
    {
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Vector3 rootPosition = hits[0].pose.position;
            
            root = new TreeNode
            {
                id = nodeIdCounter++,
                value = "Root",
                position = rootPosition,
                level = 0,
                childIndex = 0
            };
            
            CreateNodeVisualization(root, true);
            allNodes.Add(root);
            
            if (rootMarkerPrefab != null && rootMarker == null)
            {
                rootMarker = Instantiate(rootMarkerPrefab);
                rootMarker.transform.position = rootPosition + Vector3.down * 0.08f;
            }
            
            UpdateStatusText();
            
            currentState = TreeState.TreeReady;
            UpdateInstructions();
            
            Debug.Log("🌳 Root node placed");
        }
    }
    
    void DetectChildNode()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            TapChildNode(Input.GetTouch(0).position);
        else if (Input.GetMouseButtonDown(0))
            TapChildNode(Input.mousePosition);
    }
    
    void TapChildNode(Vector2 touchPosition)
    {
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Vector3 tappedPosition = hits[0].pose.position;
            float distance = Vector3.Distance(tappedPosition, expectedChildPosition);
            
            if (distance < horizontalSpacing * 0.8f)
            {
                TreeNode child = new TreeNode
                {
                    id = nodeIdCounter++,
                    value = $"N{nodeIdCounter - 1}",
                    position = tappedPosition,
                    parent = selectedParent,
                    level = selectedParent.level + 1,
                    childIndex = selectedParent.children.Count
                };
                
                selectedParent.children.Add(child);
                allNodes.Add(child);
                
                CreateNodeVisualization(child, false);
                CreateEdge(selectedParent.position, child.position);
                
                if (childIndicator != null)
                {
                    Destroy(childIndicator);
                    childIndicator = null;
                }
                
                UpdateStatusText();
                
                currentState = TreeState.TreeReady;
                if (instructionText != null)
                    instructionText.text = "✅ Child added! Select parent to add more";
                
                Debug.Log($"✅ Child node added to parent {selectedParent.id}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Tap too far. Distance: {distance:F3}m");
                if (instructionText != null)
                    instructionText.text = "⚠️ Too far! Tap near the GREEN spot";
            }
        }
    }
    
    void CreateNodeVisualization(TreeNode node, bool isRoot)
    {
        if (nodeLabelPrefab != null)
        {
            node.nodeLabel = Instantiate(nodeLabelPrefab);
            node.nodeLabel.transform.position = node.position + Vector3.up * 0.1f;
            
            TextMeshPro labelText = node.nodeLabel.GetComponentInChildren<TextMeshPro>();
            if (labelText != null)
            {
                if (isRoot)
                    labelText.text = "🌳 ROOT";
                else
                    labelText.text = $"🟢 {node.value}";
            }
        }
        
        if (nodeLabelPrefab != null)
        {
            node.valueLabel = Instantiate(nodeLabelPrefab);
            node.valueLabel.transform.position = node.position + Vector3.down * 0.05f;
            
            TextMeshPro valueText = node.valueLabel.GetComponentInChildren<TextMeshPro>();
            if (valueText != null)
                valueText.text = $"ID:{node.id}";
        }
    }
    
    void CreateEdge(Vector3 parentPos, Vector3 childPos)
    {
        GameObject edge = new GameObject("TreeEdge");
        LineRenderer lr = edge.AddComponent<LineRenderer>();
        
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = edgeColor;
        lr.endColor = edgeColor;
        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;
        lr.positionCount = 2;
        
        Vector3 start = parentPos + Vector3.up * 0.02f;
        Vector3 end = childPos + Vector3.up * 0.02f;
        
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.useWorldSpace = true;
        
        edges.Add(edge);
    }
    
    public void SelectNodeForChild(int nodeId)
    {
        if (currentState != TreeState.TreeReady) return;
        
        TreeNode parent = FindNodeById(nodeId);
        
        if (parent == null)
        {
            Debug.LogWarning("⚠️ Node not found!");
            if (instructionText != null)
                instructionText.text = "⚠️ Node not found!";
            return;
        }
        
        if (allNodes.Count >= maxNodes)
        {
            Debug.LogWarning("⚠️ Tree is full!");
            if (instructionText != null)
                instructionText.text = "⚠️ Tree is full!";
            return;
        }
        
        selectedParent = parent;
        
        int childCount = parent.children.Count;
        float offset = (childCount - parent.children.Count / 2f) * horizontalSpacing;
        
        Vector3 downDirection = Vector3.forward;
        expectedChildPosition = parent.position + downDirection * verticalSpacing + Vector3.right * offset;
        
        childIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        childIndicator.name = "ChildIndicator";
        childIndicator.transform.position = expectedChildPosition + Vector3.up * 0.05f;
        childIndicator.transform.localScale = Vector3.one * 0.03f;
        
        Renderer rend = childIndicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0, 1, 0, 0.7f);
        rend.material = mat;
        
        Collider col = childIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        currentState = TreeState.PlacingChild;
        
        if (instructionText != null)
            instructionText.text = $"➕ Place child coin at GREEN spot, then TAP it";
        
        Debug.Log($"🟢 Child indicator placed for parent {nodeId}");
    }
    
    public void AddChildToRoot()
    {
        if (root == null) return;
        SelectNodeForChild(root.id);
    }
    
    public void RemoveNode(int nodeId)
    {
        if (currentState != TreeState.TreeReady) return;
        
        TreeNode node = FindNodeById(nodeId);
        
        if (node == null || node == root)
        {
            Debug.LogWarning("⚠️ Cannot remove root or node not found!");
            if (instructionText != null)
                instructionText.text = "⚠️ Cannot remove root!";
            return;
        }
        
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.transform.position = node.position + Vector3.up * 0.05f;
        indicator.transform.localScale = Vector3.one * 0.08f;
        
        Renderer rend = indicator.GetComponent<Renderer>();
        rend.material.color = new Color(1, 0, 0, 0.7f);
        
        if (instructionText != null)
            instructionText.text = $"➖ Remove node {nodeId} and all its children";
        
        Destroy(indicator, 3f);
        StartCoroutine(RemoveNodeAfterDelay(node, 2f));
    }
    
    IEnumerator RemoveNodeAfterDelay(TreeNode node, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        RemoveNodeAndChildren(node);
        
        UpdateStatusText();
        
        if (instructionText != null)
            instructionText.text = "✅ Node removed! Ready for next operation";
        
        Debug.Log($"✅ Node {node.id} and children removed");
    }
    
    void RemoveNodeAndChildren(TreeNode node)
    {
        for (int i = node.children.Count - 1; i >= 0; i--)
        {
            RemoveNodeAndChildren(node.children[i]);
        }
        
        if (node.parent != null)
        {
            node.parent.children.Remove(node);
        }
        
        if (node.nodeLabel != null)
            Destroy(node.nodeLabel);
        if (node.valueLabel != null)
            Destroy(node.valueLabel);
        
        allNodes.Remove(node);
        
        RemoveEdgesConnectedToNode(node);
    }
    
    void RemoveEdgesConnectedToNode(TreeNode node)
    {
        for (int i = edges.Count - 1; i >= 0; i--)
        {
            if (edges[i] != null)
                Destroy(edges[i]);
        }
        edges.Clear();
        
        RebuildEdges();
    }
    
    void RebuildEdges()
    {
        foreach (var node in allNodes)
        {
            if (node.parent != null)
            {
                CreateEdge(node.parent.position, node.position);
            }
        }
    }
    
    TreeNode FindNodeById(int id)
    {
        foreach (var node in allNodes)
        {
            if (node.id == id)
                return node;
        }
        return null;
    }
    
    public void TraverseInOrder()
    {
        if (root == null) return;
        
        List<int> order = new List<int>();
        InOrderTraversal(root, order);
        
        string result = string.Join(" → ", order);
        if (instructionText != null)
            instructionText.text = $"📖 In-Order: {result}";
        
        Debug.Log($"In-Order Traversal: {result}");
    }
    
    void InOrderTraversal(TreeNode node, List<int> order)
    {
        if (node == null) return;
        
        for (int i = 0; i < node.children.Count / 2; i++)
        {
            InOrderTraversal(node.children[i], order);
        }
        
        order.Add(node.id);
        
        for (int i = node.children.Count / 2; i < node.children.Count; i++)
        {
            InOrderTraversal(node.children[i], order);
        }
    }
    
    public void TraversePreOrder()
    {
        if (root == null) return;
        
        List<int> order = new List<int>();
        PreOrderTraversal(root, order);
        
        string result = string.Join(" → ", order);
        if (instructionText != null)
            instructionText.text = $"📖 Pre-Order: {result}";
        
        Debug.Log($"Pre-Order Traversal: {result}");
    }
    
    void PreOrderTraversal(TreeNode node, List<int> order)
    {
        if (node == null) return;
        
        order.Add(node.id);
        
        foreach (var child in node.children)
        {
            PreOrderTraversal(child, order);
        }
    }
    
    public void TraversePostOrder()
    {
        if (root == null) return;
        
        List<int> order = new List<int>();
        PostOrderTraversal(root, order);
        
        string result = string.Join(" → ", order);
        if (instructionText != null)
            instructionText.text = $"📖 Post-Order: {result}";
        
        Debug.Log($"Post-Order Traversal: {result}");
    }
    
    void PostOrderTraversal(TreeNode node, List<int> order)
    {
        if (node == null) return;
        
        foreach (var child in node.children)
        {
            PostOrderTraversal(child, order);
        }
        
        order.Add(node.id);
    }
    
    void UpdateStatusText()
    {
        if (statusText == null) return;
        
        int nodeCount = allNodes.Count;
        int height = CalculateHeight(root);
        
        statusText.text = $"Nodes: {nodeCount} | Height: {height}";
    }
    
    int CalculateHeight(TreeNode node)
    {
        if (node == null || node.children.Count == 0)
            return 0;
        
        int maxHeight = 0;
        foreach (var child in node.children)
        {
            int childHeight = CalculateHeight(child);
            if (childHeight > maxHeight)
                maxHeight = childHeight;
        }
        
        return maxHeight + 1;
    }
    
    void UpdateInstructions()
    {
        if (instructionText == null) return;
        
        switch (currentState)
        {
            case TreeState.WaitingForSurface:
                instructionText.text = "🔍 Move phone to scan surfaces";
                break;
                
            case TreeState.SurfaceDetected:
                instructionText.text = "✅ Tap to place tree area";
                break;
                
            case TreeState.PlacingRoot:
                instructionText.text = "🌳 Place ROOT coin, then TAP it";
                break;
                
            case TreeState.TreeReady:
                instructionText.text = "✅ Tree ready! Use Add Child/Remove buttons";
                break;
        }
    }
    
    public void ResetTree()
    {
        foreach (var node in allNodes)
        {
            if (node.nodeLabel != null)
                Destroy(node.nodeLabel);
            if (node.valueLabel != null)
                Destroy(node.valueLabel);
        }
        allNodes.Clear();
        
        foreach (var edge in edges)
        {
            if (edge != null)
                Destroy(edge);
        }
        edges.Clear();
        
        if (rootMarker != null)
        {
            Destroy(rootMarker);
            rootMarker = null;
        }
        
        if (childIndicator != null)
        {
            Destroy(childIndicator);
            childIndicator = null;
        }
        
        root = null;
        selectedParent = null;
        nodeIdCounter = 0;
        surfacePlaced = false;
        isSurfaceDetected = false;
        
        ShowPlanes();
        
        currentState = TreeState.WaitingForSurface;
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.gameObject.SetActive(true);
            detectionFeedbackText.text = "🔍 Scan environment slowly";
            detectionFeedbackText.color = notDetectingColor;
        }
        
        if (statusText != null)
            statusText.text = "Nodes: 0 | Height: 0";
        
        UpdateInstructions();
        
        Debug.Log("🔄 Tree completely reset");
    }
    
    void HidePlanes()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);
        }
    }
    
    void ShowPlanes()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(true);
        }
    }
    
    public bool IsTreeReady()
    {
        return currentState == TreeState.TreeReady && root != null;
    }
    
    public int GetNodeCount()
    {
        return allNodes.Count;
    }
}