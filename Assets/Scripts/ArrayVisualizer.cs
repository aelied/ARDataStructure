using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ArrayVisualizer : MonoBehaviour
{
    [Header("Array Settings")]
    public GameObject nodePrefab;
    public float nodeWidth = 0.3f;
    public float nodeSpacing = 0.05f;
    public int maxArraySize = 10;
    public float animationDuration = 0.5f;
    
    [Header("Visual Settings")]
    public Color nodeColor = new Color(0.3f, 0.7f, 1f);
    public Color accessColor = Color.yellow;
    public Color updateColor = Color.green;
    
    [Header("Surface Detection Feedback")]
    public TextMeshProUGUI detectionFeedbackText;  // This should be the BOTTOM text in your instruction card
    public Color detectingColor = Color.green;
    public Color notDetectingColor = Color.red;
    
    [Header("AR References")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    
    private UniversalARPlacementOptimizer arOptimizer;
    private ARExplorationTracker explorationTracker;
    
    private List<ArrayNode> nodes = new List<ArrayNode>();
    private Vector3 centerPosition;
    private Quaternion baseRotation;
    private bool isPlaced = false;
    private bool isSurfaceDetected = false;
    private bool hadPlanesLastFrame = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    
    class ArrayNode
    {
        public GameObject gameObject;
        public string value;
        public int index;
        public TextMesh valueText;
        public TextMesh indexText;
        public Renderer renderer;
        public Material material;
        public Vector3 originalScale;
    }
    
    void Start()
    {
        // Auto-find AR components
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
        
        // Verify prefab
        if (nodePrefab == null)
        {
            Debug.LogError("❌ NODE PREFAB IS NOT ASSIGNED! Please assign it in the Inspector.");
        }
        else
        {
            Debug.Log(" Node Prefab assigned: " + nodePrefab.name);
        }
        
        arOptimizer = FindObjectOfType<UniversalARPlacementOptimizer>();
        explorationTracker = FindObjectOfType<ARExplorationTracker>();
        
        Debug.Log("AROptimizer: " + (arOptimizer != null ? "Found" : "NOT FOUND"));
        Debug.Log("ExplorationTracker: " + (explorationTracker != null ? "Found" : "NOT FOUND"));
        
        // Initialize feedback text - start with red "no planes" message
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
            // Check for surface detection continuously
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
                    centerPosition = hitPose.position;
                    centerPosition.y += 0.01f;
                    baseRotation = hitPose.rotation;
                    
                    transform.position = centerPosition;
                    transform.rotation = baseRotation;
                    
                    isPlaced = true;
                    
                    // Hide feedback after placement
                    if (detectionFeedbackText != null)
                    {
                        detectionFeedbackText.gameObject.SetActive(false);
                    }
                    
                    Debug.Log("🎯 Array placed at: " + centerPosition);
                    
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
                    Debug.LogWarning("⚠️ Raycast did NOT hit any planes.");
                }
            }
        }
        else
        {
            // Keep planes hidden while array is placed
            EnsurePlanesHidden();
        }
    }
    
    /// <summary>
    /// Continuously check if surfaces are being detected
    /// </summary>
    private void CheckSurfaceDetection()
    {
        if (raycastManager == null || isPlaced)
        {
            return;
        }
        
        // Check if any planes exist at all
        bool hasAnyPlanes = planeManager != null && planeManager.trackables.count > 0;
        
        // Check from screen center
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        bool detected = raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);
        
        // Update UI if detection state OR plane existence changed
        if (detected != isSurfaceDetected || hasAnyPlanes != hadPlanesLastFrame)
        {
            isSurfaceDetected = detected;
            hadPlanesLastFrame = hasAnyPlanes;
            UpdateDetectionFeedback(detected, hasAnyPlanes);
        }
    }
    
    /// <summary>
    /// Update the visual feedback for surface detection
    /// </summary>
    private void UpdateDetectionFeedback(bool detecting, bool hasPlanes)
    {
        if (detectionFeedbackText == null) return;
        
        // Always keep the text visible until placement
        detectionFeedbackText.gameObject.SetActive(true);
        
        if (detecting)
        {
            // Surface is detected and ready - GREEN text
            detectionFeedbackText.text = "Surface Detected - Tap to place";
            detectionFeedbackText.color = detectingColor;
        }
        else if (!hasPlanes)
        {
            // No planes detected yet - RED text with lighting hint
            detectionFeedbackText.text = "Move to a well-lit area for better detection";
            detectionFeedbackText.color = notDetectingColor;
        }
        else
        {
            // Has planes but not currently detecting center - RED text
            detectionFeedbackText.text = "Point camera at detected surface";
            detectionFeedbackText.color = notDetectingColor;
        }
    }
    
    public bool IsArrayPlaced()
    {
        return isPlaced;
    }
    
    public void Add(string value)
    {
        if (!isPlaced)
        {
            Debug.Log("Array not placed yet! Tap on a plane first.");
            return;
        }
        
        if (nodes.Count >= maxArraySize)
        {
            Debug.Log("Array is full!");
            return;
        }
        
        if (nodePrefab == null)
        {
            Debug.LogError("❌ Cannot add node: nodePrefab is not assigned!");
            return;
        }
        
        int index = nodes.Count;
        
        GameObject nodeObj = Instantiate(nodePrefab, centerPosition, baseRotation);
        nodeObj.transform.SetParent(transform);
        
        ArrayNode node = new ArrayNode
        {
            gameObject = nodeObj,
            value = value,
            index = index,
            originalScale = new Vector3(0.12f, 0.12f, 0.12f)
        };
        
        // Setup renderer
        node.renderer = nodeObj.GetComponent<Renderer>();
        if (node.renderer == null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(nodeObj.transform);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = Vector3.one;
            node.renderer = cube.GetComponent<Renderer>();
        }
        
        node.material = new Material(Shader.Find("Standard"));
        node.material.color = nodeColor;
        node.renderer.material = node.material;
        
        // Create texts on TOP of the spheres (visible from above)
        node.valueText = CreateTextObject(nodeObj.transform, "ValueText", 
            Vector3.up * 0.15f, 40, value);
        node.indexText = CreateTextObject(nodeObj.transform, "IndexText", 
            Vector3.down * 0.15f, 30, $"[{index}]");
        
        nodes.Add(node);
        
        // Recalculate all positions to keep centered
        RecalculateAllPositions();
        
        StartCoroutine(AnimateAppear(node));
        
        Debug.Log($" Added '{value}' at index [{index}]. Array size: {nodes.Count}");
        
        if (arOptimizer != null)
        {
            arOptimizer.OnStructureReset();
        }
    }
    
    public void InsertAt(int index, string value)
    {
        if (!isPlaced || index < 0 || index > nodes.Count || nodes.Count >= maxArraySize)
        {
            Debug.LogWarning("Cannot insert at this position");
            return;
        }
        
        GameObject nodeObj = Instantiate(nodePrefab, centerPosition, baseRotation);
        nodeObj.transform.SetParent(transform);
        
        ArrayNode node = new ArrayNode
        {
            gameObject = nodeObj,
            value = value,
            index = index,
            originalScale = new Vector3(0.12f, 0.12f, 0.12f)
        };
        
        // Setup renderer
        node.renderer = nodeObj.GetComponent<Renderer>();
        if (node.renderer == null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(nodeObj.transform);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = Vector3.one;
            node.renderer = cube.GetComponent<Renderer>();
        }
        
        node.material = new Material(Shader.Find("Standard"));
        node.material.color = nodeColor;
        node.renderer.material = node.material;
        
        // Create texts on TOP of the spheres (visible from above)
        node.valueText = CreateTextObject(nodeObj.transform, "ValueText", 
            Vector3.up * 0.15f, 40, value);
        node.indexText = CreateTextObject(nodeObj.transform, "IndexText", 
            Vector3.down * 0.15f, 30, $"[{index}]");
        
        // Insert into list
        nodes.Insert(index, node);
        
        // Recalculate ALL positions to keep array centered and prevent overlap
        RecalculateAllPositions();
        
        StartCoroutine(AnimateAppear(node));
        
        Debug.Log($" Added '{value}' at index [{index}]. Array size: {nodes.Count}");
        
        if (arOptimizer != null)
        {
            arOptimizer.OnStructureReset();
        }
    }
    
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= nodes.Count)
        {
            Debug.LogWarning($"Invalid index {index}");
            return;
        }
        
        ArrayNode node = nodes[index];
        string value = node.value;
        
        nodes.RemoveAt(index);
        
        StartCoroutine(AnimateDisappear(node));
        
        // Recalculate all positions after removal
        RecalculateAllPositions();
        
        Debug.Log($" Removed '{value}' at index [{index}]. Array size: {nodes.Count}");
        
        if (arOptimizer != null)
        {
            arOptimizer.OnStructureReset();
        }
    }
    
    public void UpdateAt(int index, string newValue)
    {
        if (index < 0 || index >= nodes.Count)
        {
            Debug.LogWarning($"Invalid index {index}");
            return;
        }
        
        ArrayNode node = nodes[index];
        StartCoroutine(AnimateValueUpdate(node, newValue));
        
        Debug.Log($" Updated index {index} to: {newValue}");
    }
    
    public string GetAt(int index)
    {
        if (index < 0 || index >= nodes.Count)
        {
            Debug.LogWarning($"Invalid index {index}");
            return "Invalid";
        }
        
        ArrayNode node = nodes[index];
        StartCoroutine(HighlightNode(node, accessColor));
        
        Debug.Log($" Accessed index {index}: {node.value}");
        return node.value;
    }
    
    public int FindValue(string value)
    {
        // Linear search through the array to find the value
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].value == value)
            {
                // Found it! Highlight the node
                StartCoroutine(HighlightNode(nodes[i], accessColor));
                Debug.Log($" Found value '{value}' at index {i}");
                return i;
            }
        }
        
        // Not found
        Debug.Log($"❌ Value '{value}' not found in array");
        return -1;
    }
    
    public void Clear()
    {
        foreach (ArrayNode node in nodes)
        {
            if (node.gameObject != null)
            {
                Destroy(node.gameObject);
            }
        }
        
        nodes.Clear();
        isPlaced = false;
        isSurfaceDetected = false;
        hadPlanesLastFrame = false;
        
        if (arOptimizer != null)
        {
            arOptimizer.OnStructureReset();
        }
        
        // Re-enable plane detection for next placement
        if (planeManager != null)
        {
            planeManager.enabled = true;
        }
        
        // Show feedback again for re-placement
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.gameObject.SetActive(true);
            bool hasPlanes = planeManager != null && planeManager.trackables.count > 0;
            UpdateDetectionFeedback(false, hasPlanes);
        }
        
        ShowPlanes();
        
        Debug.Log("🗑️ Array cleared and reset!");
    }
    
    public int GetSize()
    {
        return nodes.Count;
    }
    
    public int GetMaxSize()
    {
        return maxArraySize;
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
    
    // Call this after any operation to ensure planes stay hidden
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
    
    void RecalculateAllPositions()
    {
        int totalNodes = nodes.Count;
        if (totalNodes == 0) return;
        
        float totalWidth = totalNodes * (nodeWidth + nodeSpacing) - nodeSpacing;
        float startOffset = -totalWidth / 2f;
        
        for (int i = 0; i < nodes.Count; i++)
        {
            ArrayNode node = nodes[i];
            node.index = i;
            
            // Update index text
            if (node.indexText != null)
            {
                node.indexText.text = $"[{i}]";
            }
            
            // Calculate new centered position
            Vector3 offset = transform.right * (startOffset + i * (nodeWidth + nodeSpacing));
            Vector3 newPosition = centerPosition + offset;
            
            // Move node to new position
            StartCoroutine(MoveNode(node, newPosition));
        }
        
        Debug.Log($"📐 Recalculated positions for {totalNodes} nodes");
    }
    
    TextMesh CreateTextObject(Transform parent, string name, Vector3 localPos, float fontSize, string text)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        textObj.transform.localPosition = localPos;
        textObj.transform.localRotation = Quaternion.identity;
        textObj.transform.localScale = Vector3.one * 0.05f;
        
        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = (int)fontSize;
        tm.color = Color.black;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.characterSize = 1f;
        tm.richText = false;
        
        textObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        
        GameObject bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgQuad.transform.SetParent(textObj.transform, false);
        bgQuad.transform.localPosition = Vector3.back * 0.01f;
        bgQuad.transform.localScale = new Vector3(0.8f, 0.4f, 1f);
        bgQuad.transform.localRotation = Quaternion.identity;
        
        Renderer bgRenderer = bgQuad.GetComponent<Renderer>();
        if (bgRenderer != null)
        {
            bgRenderer.material = new Material(Shader.Find("Unlit/Color"));
            bgRenderer.material.color = new Color(1f, 1f, 1f, 0.9f);
        }
        
        Destroy(bgQuad.GetComponent<Collider>());
        
        Debug.Log($" Created TextMesh: {name} - '{text}' at localPos {localPos}");
        
        return tm;
    }
    
    System.Collections.IEnumerator AnimateAppear(ArrayNode node)
    {
        node.gameObject.transform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float smoothT = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            
            node.gameObject.transform.localScale = Vector3.Lerp(Vector3.zero, node.originalScale, smoothT);
            
            yield return null;
        }
        
        node.gameObject.transform.localScale = node.originalScale;
    }
    
    System.Collections.IEnumerator AnimateDisappear(ArrayNode node)
    {
        Vector3 startScale = node.gameObject.transform.localScale;
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float smoothT = t * t;
            
            node.gameObject.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, smoothT);
            
            yield return null;
        }
        
        Destroy(node.gameObject);
    }
    
    System.Collections.IEnumerator MoveNode(ArrayNode node, Vector3 newPosition)
    {
        Vector3 startPos = node.gameObject.transform.position;
        float elapsed = 0f;
        float moveDuration = 0.3f;
        
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            
            node.gameObject.transform.position = Vector3.Lerp(startPos, newPosition, smoothT);
            
            yield return null;
        }
        
        node.gameObject.transform.position = newPosition;
    }
    
    System.Collections.IEnumerator HighlightNode(ArrayNode node, Color highlightColor)
    {
        if (node.material == null) yield break;
        
        Color originalColor = node.material.color;
        float elapsed = 0f;
        float pulseDuration = 0.3f;
        
        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pulseDuration;
            
            node.material.color = Color.Lerp(originalColor, highlightColor, Mathf.Sin(t * Mathf.PI));
            
            yield return null;
        }
        
        node.material.color = originalColor;
    }
    
    System.Collections.IEnumerator AnimateValueUpdate(ArrayNode node, string newValue)
    {
        Vector3 startScale = node.gameObject.transform.localScale;
        Vector3 smallScale = node.originalScale * 0.8f;
        
        float elapsed = 0f;
        float halfDuration = animationDuration / 2f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            
            node.gameObject.transform.localScale = Vector3.Lerp(startScale, smallScale, t);
            
            yield return null;
        }
        
        node.value = newValue;
        if (node.valueText != null)
        {
            node.valueText.text = newValue;
        }
        
        if (node.material != null)
        {
            Color originalColor = node.material.color;
            node.material.color = updateColor;
            yield return new WaitForSeconds(0.1f);
            node.material.color = originalColor;
        }
        
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            
            node.gameObject.transform.localScale = Vector3.Lerp(smallScale, node.originalScale, t);
            
            yield return null;
        }
        
        node.gameObject.transform.localScale = node.originalScale;
    }
}