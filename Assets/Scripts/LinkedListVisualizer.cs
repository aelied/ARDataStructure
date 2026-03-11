using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class LinkedListVisualizer : MonoBehaviour
{
    [Header("Linked List Settings")]
    public GameObject nodePrefab;
    public GameObject arrowPrefab;
    public float nodeSpacing = 0.3f;
    public int maxNodes = 10;
    public float animationDuration = 0.5f;

    [Header("Surface Detection Feedback")]
    public TextMeshProUGUI detectionFeedbackText;
    public Color detectingColor = Color.green;
    public Color notDetectingColor = Color.red;
    
    [Header("AR References")]
    private UniversalARPlacementOptimizer arOptimizer;
    private ARExplorationTracker explorationTracker;

    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    
    private List<GameObject> nodes = new List<GameObject>();
    private List<GameObject> arrows = new List<GameObject>();
    private Vector3 startPosition;
    private Quaternion baseRotation;
    private bool isPlaced = false;
    private bool isSurfaceDetected = false;
    private bool hadPlanesLastFrame = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private int nodeIdCounter = 1;
    
    // ⭐ NEW: Store current scale for NodeSizeController
    private float currentNodeScale = 0.15f;
    
    void Start()
    {
        if (planeManager == null)
        {
            planeManager = FindObjectOfType<ARPlaneManager>();
            Debug.Log("ARPlaneManager: " + (planeManager != null ? "Found" : "NOT FOUND"));
        }
        
        if (raycastManager == null)
        {
            raycastManager = FindObjectOfType<ARRaycastManager>();
            Debug.Log("ARRaycastManager: " + (raycastManager != null ? "Found" : "NOT FOUND"));
        }
        
        if (nodePrefab == null)
        {
            Debug.LogError(" NODE PREFAB IS NOT ASSIGNED! Please assign it in the Inspector.");
        }
        else
        {
            Debug.Log(" Node Prefab assigned: " + nodePrefab.name);
        }
        
        if (arrowPrefab == null)
        {
            Debug.LogWarning(" Arrow Prefab is not assigned. Arrows will not be shown.");
        }

        arOptimizer = FindObjectOfType<UniversalARPlacementOptimizer>();
        explorationTracker = FindObjectOfType<ARExplorationTracker>();
        
        Debug.Log("AROptimizer: " + (arOptimizer != null ? "Found" : "NOT FOUND"));
        Debug.Log("ExplorationTracker: " + (explorationTracker != null ? "Found" : "NOT FOUND"));

        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.gameObject.SetActive(true);
            UpdateDetectionFeedback(false, false);
        }
    }
    
    void Update()
    {
        if (!isPlaced)
        {
            CheckSurfaceDetection();
            
            if (Input.touchCount > 0 || Input.GetMouseButtonDown(0))
            {
                Vector2 touchPosition;
                
                if (Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase != TouchPhase.Began) return;
                    touchPosition = touch.position;
                }
                else
                {
                    touchPosition = Input.mousePosition;
                }
                
                Debug.Log("Touch detected at: " + touchPosition);
                
                if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
                {
                    Debug.Log($" Hit {hits.Count} planes!");
                    
                    Pose hitPose = hits[0].pose;
                    
                    startPosition = hitPose.position;
                    startPosition.y += 0.01f;
                    baseRotation = hitPose.rotation;
                    
                    transform.position = startPosition;
                    transform.rotation = baseRotation;
                    
                    isPlaced = true;

                    if (detectionFeedbackText != null)
                    {
                        detectionFeedbackText.gameObject.SetActive(false);
                    }
                    
                    Debug.Log("🎯 Linked List placed at: " + startPosition);

                    if (arOptimizer != null)
                    {
                        arOptimizer.OnStructurePlaced();
                    }
                    
                    if (explorationTracker != null)
                    {
                        explorationTracker.RecordInteraction();
                    }
                    
                    HidePlanes();
                }
                else
                {
                    Debug.LogWarning(" Raycast did NOT hit any planes. Make sure planes are detected.");
                }
            }
        }
        else
        {
            // ⭐ NEW: Keep planes hidden while linked list is placed
            EnsurePlanesHidden();
        }
    }

    private void CheckSurfaceDetection()
    {
        if (raycastManager == null || isPlaced) return;
        
        bool hasAnyPlanes = planeManager != null && planeManager.trackables.count > 0;
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        bool detected = raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);
        
        if (detected != isSurfaceDetected || hasAnyPlanes != hadPlanesLastFrame)
        {
            isSurfaceDetected = detected;
            hadPlanesLastFrame = hasAnyPlanes;
            UpdateDetectionFeedback(detected, hasAnyPlanes);
        }
    }
    
    private void UpdateDetectionFeedback(bool detecting, bool hasPlanes)
    {
        if (detectionFeedbackText == null) return;
        
        detectionFeedbackText.gameObject.SetActive(true);
        
        if (detecting)
        {
            detectionFeedbackText.text = " Surface Detected - Tap to place";
            detectionFeedbackText.color = detectingColor;
        }
        else if (!hasPlanes)
        {
            detectionFeedbackText.text = "Move to a well-lit area for better detection";
            detectionFeedbackText.color = notDetectingColor;
        }
        else
        {
            detectionFeedbackText.text = "Point camera at detected surface";
            detectionFeedbackText.color = notDetectingColor;
        }
    }
    
    public bool IsListPlaced()
    {
        return isPlaced;
    }
    
    public void InsertAtHead()
    {
        if (!isPlaced)
        {
            Debug.Log("Linked List not placed yet! Tap on a plane first.");
            return;
        }
        
        if (nodes.Count >= maxNodes)
        {
            Debug.Log("List is full!");
            return;
        }
        
        if (nodePrefab == null)
        {
            Debug.LogError(" Cannot add node: nodePrefab is not assigned!");
            return;
        }
        
        // ⭐ Ensure planes stay hidden before operation
        EnsurePlanesHidden();
        
        Vector3 newPosition = startPosition;
        GameObject newNode = CreateNode(newPosition, nodeIdCounter++);
        
        nodes.Insert(0, newNode);
        
        StartCoroutine(ShiftNodesRight(0));
        
        Debug.Log($" Inserted at HEAD. List size: {nodes.Count}");

        if (arOptimizer != null)
        {
            arOptimizer.OnStructureReset();
        }
    }
    
    public void InsertAtTail()
    {
        if (!isPlaced)
        {
            Debug.Log("Linked List not placed yet! Tap on a plane first.");
            return;
        }
        
        if (nodes.Count >= maxNodes)
        {
            Debug.Log("List is full!");
            return;
        }
        
        if (nodePrefab == null)
        {
            Debug.LogError(" Cannot add node: nodePrefab is not assigned!");
            return;
        }
        
        // ⭐ Ensure planes stay hidden before operation
        EnsurePlanesHidden();
        
        Vector3 newPosition = startPosition + transform.right * (nodes.Count * nodeSpacing);
        GameObject newNode = CreateNode(newPosition, nodeIdCounter++);
        
        nodes.Add(newNode);
        
        if (nodes.Count > 1)
        {
            CreateArrow(nodes.Count - 2);
        }
        
        StartCoroutine(AnimateAddNode(newNode, newPosition));
        Debug.Log($" Inserted at TAIL. List size: {nodes.Count}");

        if (arOptimizer != null)
        {
            arOptimizer.OnStructureReset();
        }
    }
    
    public void InsertAtPosition(int position)
    {
        if (!isPlaced) return;
        
        if (nodes.Count >= maxNodes)
        {
            Debug.Log("List is full!");
            return;
        }
        
        position = Mathf.Clamp(position, 0, nodes.Count);
        
        if (position == 0)
        {
            InsertAtHead();
            return;
        }
        
        if (position >= nodes.Count)
        {
            InsertAtTail();
            return;
        }
        
        // ⭐ Ensure planes stay hidden before operation
        EnsurePlanesHidden();
        
        Vector3 newPosition = startPosition + transform.right * (position * nodeSpacing);
        GameObject newNode = CreateNode(newPosition, nodeIdCounter++);
        
        nodes.Insert(position, newNode);
        
        StartCoroutine(ShiftNodesRight(position));
        
        Debug.Log($" Inserted at position {position}. List size: {nodes.Count}");
        
        if (arOptimizer != null)
        {
            arOptimizer.OnStructureReset();
        }
    }
    
    public void DeleteFromHead()
    {
        if (nodes.Count == 0)
        {
            Debug.Log("List is empty!");
            return;
        }
        
        GameObject nodeToRemove = nodes[0];
        nodes.RemoveAt(0);
        
        if (arrows.Count > 0)
        {
            Destroy(arrows[0]);
            arrows.RemoveAt(0);
        }
        
        StartCoroutine(AnimateRemoveNode(nodeToRemove));
        StartCoroutine(ShiftNodesLeft(0));
        
        Debug.Log($" Deleted from HEAD. List size: {nodes.Count}");
        
        if (arOptimizer != null)
        {
            arOptimizer.OnStructureReset();
        }
    }
    
    public void DeleteFromTail()
    {
        if (nodes.Count == 0)
        {
            Debug.Log("List is empty!");
            return;
        }
        
        GameObject nodeToRemove = nodes[nodes.Count - 1];
        nodes.RemoveAt(nodes.Count - 1);
        
        if (arrows.Count > 0)
        {
            Destroy(arrows[arrows.Count - 1]);
            arrows.RemoveAt(arrows.Count - 1);
        }
        
        StartCoroutine(AnimateRemoveNode(nodeToRemove));
        Debug.Log($" Deleted from TAIL. List size: {nodes.Count}");
        
        if (arOptimizer != null)
        {
            arOptimizer.OnStructureReset();
        }
    }
    
    public void DeleteAtPosition(int position)
    {
        if (nodes.Count == 0)
        {
            Debug.Log("List is empty!");
            return;
        }
        
        position = Mathf.Clamp(position, 0, nodes.Count - 1);
        
        if (position == 0)
        {
            DeleteFromHead();
            return;
        }
        
        if (position >= nodes.Count - 1)
        {
            DeleteFromTail();
            return;
        }
        
        GameObject nodeToRemove = nodes[position];
        nodes.RemoveAt(position);
        
        if (position < arrows.Count)
        {
            Destroy(arrows[position]);
            arrows.RemoveAt(position);
        }
        
        StartCoroutine(AnimateRemoveNode(nodeToRemove));
        StartCoroutine(ShiftNodesLeft(position));
        
        Debug.Log($" Deleted from position {position}. List size: {nodes.Count}");
        
        if (arOptimizer != null)
        {
            arOptimizer.OnStructureReset();
        }
    }
    
    public void Clear()
    {
        foreach (GameObject node in nodes) Destroy(node);
        foreach (GameObject arrow in arrows) Destroy(arrow);
        
        nodes.Clear();
        arrows.Clear();
        nodeIdCounter = 1;
        
        isPlaced = false;
        isSurfaceDetected = false;
        hadPlanesLastFrame = false;
        
        if (arOptimizer != null)
        {
            arOptimizer.OnStructureReset();
        }
        
        ShowPlanes();
        
        Debug.Log("🗑️ Linked List cleared and reset!");
    }
    
    public int Size()
    {
        return nodes.Count;
    }
    
    public string GetNodeValue(int position)
    {
        if (position < 0 || position >= nodes.Count)
            return "Invalid";
        
        TextMeshPro textMesh = nodes[position].GetComponentInChildren<TextMeshPro>();
        if (textMesh != null)
            return textMesh.text;
        
        return $"Node {position}";
    }
    
    // ⭐ NEW METHOD: Required by NodeSizeController
    public void SetNodeScale(float scale)
    {
        currentNodeScale = scale;
        
        // Update scale for all existing nodes
        foreach (GameObject node in nodes)
        {
            if (node != null)
            {
                node.transform.localScale = Vector3.one * scale;
            }
        }
        
        // Recreate arrows to match new node sizes
        RecreateAllArrows();
        
        Debug.Log($"🔍 LinkedList node scale set to: {scale}");
    }
    
    // ⭐ NEW METHOD: Get current node scale
    public float GetNodeScale()
    {
        return currentNodeScale;
    }
    
    GameObject CreateNode(Vector3 position, int id)
    {
        GameObject newNode = Instantiate(nodePrefab, position, baseRotation);
        newNode.transform.SetParent(transform);
        newNode.name = $"Node_{id}";
        
        // ⭐ UPDATED: Use currentNodeScale
        newNode.transform.localScale = Vector3.one * currentNodeScale;
        
        Renderer renderer = newNode.GetComponent<Renderer>();
        if (renderer != null)
        {
            float hue = (nodes.Count * 0.15f) % 1f;
            renderer.material.color = Color.HSVToRGB(hue, 0.7f, 0.9f);
        }
        
        TextMeshPro textMesh = newNode.GetComponentInChildren<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.text = id.ToString();
        }
        
        return newNode;
    }
    
    System.Collections.IEnumerator ShiftNodesRight(int startIndex)
    {
        yield return new WaitForSeconds(0.1f);
        
        for (int i = startIndex; i < nodes.Count; i++)
        {
            Vector3 targetPos = startPosition + transform.right * (i * nodeSpacing);
            StartCoroutine(MoveNode(nodes[i], targetPos));
        }
        
        yield return new WaitForSeconds(animationDuration);
        RecreateAllArrows();
    }
    
    System.Collections.IEnumerator ShiftNodesLeft(int startIndex)
    {
        yield return new WaitForSeconds(0.1f);
        
        for (int i = startIndex; i < nodes.Count; i++)
        {
            Vector3 targetPos = startPosition + transform.right * (i * nodeSpacing);
            StartCoroutine(MoveNode(nodes[i], targetPos));
        }
        
        yield return new WaitForSeconds(animationDuration);
        RecreateAllArrows();
    }
    
    System.Collections.IEnumerator MoveNode(GameObject node, Vector3 targetPos)
    {
        Vector3 startPos = node.transform.position;
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            node.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        node.transform.position = targetPos;
    }
    
    void RecreateAllArrows()
    {
        foreach (GameObject arrow in arrows)
        {
            if (arrow != null) Destroy(arrow);
        }
        arrows.Clear();
        
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            CreateArrow(i);
        }
    }
    
    void HidePlanes()
    {
        if (planeManager != null)
        {
            // Disable plane manager to stop detection
            planeManager.enabled = false;
            
            // Hide existing planes
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
            Debug.Log("👻 AR Planes hidden and disabled");
        }
    }
    
    void ShowPlanes()
    {
        if (planeManager != null)
        {
            // Re-enable plane manager
            planeManager.enabled = true;
            
            // Show planes
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
            Debug.Log("👁️ AR Planes visible and enabled again");
        }
    }
    
    // ⭐ NEW: Call this after any operation to ensure planes stay hidden
    void EnsurePlanesHidden()
    {
        if (isPlaced && planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
            {
                if (plane.gameObject.activeSelf)
                {
                    plane.gameObject.SetActive(false);
                }
            }
        }
    }
    
    void CreateArrow(int fromIndex)
    {
        if (arrowPrefab == null || fromIndex >= nodes.Count - 1) return;
        
        Vector3 start = nodes[fromIndex].transform.position;
        Vector3 end = nodes[fromIndex + 1].transform.position;
        Vector3 midPoint = (start + end) / 2f;
        
        GameObject arrow = Instantiate(arrowPrefab, midPoint, Quaternion.identity);
        arrow.transform.SetParent(transform);
        arrow.transform.LookAt(end);
        
        // ⭐ UPDATED: Scale arrow width based on node scale
        float arrowWidth = 0.05f * (currentNodeScale / 0.15f);
        arrow.transform.localScale = new Vector3(arrowWidth, arrowWidth, Vector3.Distance(start, end));
        
        arrows.Add(arrow);
    }
    
    System.Collections.IEnumerator AnimateAddNode(GameObject node, Vector3 targetPos)
    {
        Vector3 startPos = targetPos + Vector3.up * 0.3f;
        node.transform.position = startPos;
        node.transform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - elapsed / animationDuration, 3f);
            
            node.transform.position = Vector3.Lerp(startPos, targetPos, t);
            // ⭐ UPDATED: Use currentNodeScale
            node.transform.localScale = Vector3.one * currentNodeScale * t;
            yield return null;
        }
        
        node.transform.position = targetPos;
        node.transform.localScale = Vector3.one * currentNodeScale;
    }
    
    System.Collections.IEnumerator AnimateRemoveNode(GameObject node)
    {
        float elapsed = 0f;
        Vector3 startPos = node.transform.position;
        Vector3 startScale = node.transform.localScale;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            node.transform.position = startPos + Vector3.up * (0.3f * t);
            node.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
        
        Destroy(node);
    }
}