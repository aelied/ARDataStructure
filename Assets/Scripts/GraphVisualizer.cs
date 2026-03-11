using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class GraphVisualizer : MonoBehaviour
{
    [Header("Graph Settings")]
    public GameObject vertexPrefab;
    public GameObject edgePrefab;
    public float radius = 0.3f;
    public int maxVertices = 8;
    public float animationDuration = 0.5f;
    public bool isDirected = false; // Toggle for directed/undirected graph

     [Header("Surface Detection Feedback")]
    public TextMeshProUGUI detectionFeedbackText;
    public Color detectingColor = Color.green;
    public Color notDetectingColor = Color.red;
    
    [Header("AR References")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    private UniversalARPlacementOptimizer arOptimizer;
    private ARExplorationTracker explorationTracker;
    
    private List<GameObject> vertices = new List<GameObject>();
    private List<Edge> edges = new List<Edge>();
    private Dictionary<GameObject, List<GameObject>> adjacencyList = new Dictionary<GameObject, List<GameObject>>();
    private Vector3 centerPosition;
    private Quaternion baseRotation;
    private bool isPlaced = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private int vertexIdCounter = 1;
    private bool isSurfaceDetected = false;
    private bool hadPlanesLastFrame = false;
    
    class Edge
    {
        public GameObject gameObject;
        public int fromIndex;
        public int toIndex;
        public GameObject arrow; // For directed graphs
    }
    
    void Start()
    {
        // Auto-find ARPlaneManager if not assigned
        if (planeManager == null)
        {
            planeManager = FindObjectOfType<ARPlaneManager>();
            Debug.Log("ARPlaneManager: " + (planeManager != null ? "Found" : "NOT FOUND"));
        }
        
        // Auto-find ARRaycastManager if not assigned
        if (raycastManager == null)
        {
            raycastManager = FindObjectOfType<ARRaycastManager>();
            Debug.Log("ARRaycastManager: " + (raycastManager != null ? "Found" : "NOT FOUND"));
        }
        
        // Verify prefabs
        if (vertexPrefab == null)
        {
            Debug.LogError(" VERTEX PREFAB IS NOT ASSIGNED! Please assign it in the Inspector.");
        }
        else
        {
            Debug.Log(" Vertex Prefab assigned: " + vertexPrefab.name);
        }
        
        if (edgePrefab == null)
        {
            Debug.LogWarning(" Edge Prefab is not assigned. Connections will not be shown.");
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
    // Only check for touches if graph hasn't been placed yet
    if (!isPlaced)
    {
        CheckSurfaceDetection();
        // Check for touch input (also support mouse for editor testing)
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
            
            // Raycast against AR planes
            if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Debug.Log($" Hit {hits.Count} planes!");
                
                // Get the hit pose
                Pose hitPose = hits[0].pose;
                
                // Set base position and rotation
                centerPosition = hitPose.position;
                centerPosition.y += 0.01f; // Just slightly above the plane
                baseRotation = hitPose.rotation;
                
                // Move the entire graph manager to this position
                transform.position = centerPosition;
                transform.rotation = baseRotation;
                
                isPlaced = true;

                if (detectionFeedbackText != null)
                {
                    detectionFeedbackText.gameObject.SetActive(false);
                }
                
                Debug.Log("🎯 Graph placed at: " + centerPosition);
                
                // ⭐ NOTIFY OPTIMIZER
                if (arOptimizer != null)
                {
                    arOptimizer.OnStructurePlaced();
                }
                
                // ⭐ RECORD INTERACTION
                if (explorationTracker != null)
                {
                    explorationTracker.RecordInteraction();
                }
                
                // Disable plane visualization after placement
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
        // 🌟 KEY FIX: Continuously enforce plane hiding while graph is placed
        EnsurePlanesHidden();
    }
}

void EnsurePlanesHidden()
{
    if (isPlaced && planeManager != null)
    {
        // Keep plane manager disabled
        if (planeManager.enabled)
        {
            planeManager.enabled = false;
            Debug.Log("⚠️ Plane manager was re-enabled, disabling again");
        }
        
        // Keep all planes hidden
        foreach (var plane in planeManager.trackables)
        {
            if (plane.gameObject.activeSelf)
            {
                plane.gameObject.SetActive(false);
                Debug.Log("⚠️ Plane became visible, hiding again");
            }
        }
    }
}
    
    public bool IsGraphPlaced()
    {
        return isPlaced;
    }
    
    public void AddVertex()
    {
        if (!isPlaced)
        {
            Debug.Log("Graph not placed yet! Tap on a plane first.");
            return;
        }
        
        if (vertices.Count >= maxVertices)
        {
            Debug.Log("Graph is full!");
            return;
        }
        
        if (vertexPrefab == null)
        {
            Debug.LogError(" Cannot add vertex: vertexPrefab is not assigned!");
            return;
        }
        
        // Position vertices in a circle
        float angle = (vertices.Count * 360f / maxVertices) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * radius,
            0,
            Mathf.Sin(angle) * radius
        );
        Vector3 position = centerPosition + transform.TransformDirection(offset);
        
        GameObject newVertex = Instantiate(vertexPrefab, position, baseRotation);
        newVertex.transform.SetParent(transform);
        newVertex.name = $"Vertex_{vertexIdCounter}";
        
        // Set color
        Renderer renderer = newVertex.GetComponent<Renderer>();
        if (renderer != null)
        {
            float hue = (vertices.Count * 0.125f) % 1f;
            renderer.material.color = Color.HSVToRGB(hue, 0.8f, 0.9f);
        }
        
        // Add label
        TextMeshPro textMesh = newVertex.GetComponentInChildren<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.text = vertexIdCounter.ToString();
        }
        
        vertices.Add(newVertex);
        adjacencyList[newVertex] = new List<GameObject>();
        vertexIdCounter++;
        
        StartCoroutine(AnimateAddVertex(newVertex, position));
        Debug.Log($" Added vertex {vertices.Count}");
          // ⭐ NOTIFY OPTIMIZER
    if (arOptimizer != null)
    {
        arOptimizer.OnStructureReset();
    }
    }
    
    public void RemoveVertex()
    {
        if (vertices.Count == 0)
        {
            Debug.Log("Graph is empty!");
            return;
        }
        
        // Remove last vertex
        GameObject lastVertex = vertices[vertices.Count - 1];
        vertices.RemoveAt(vertices.Count - 1);
        
        // Remove all edges connected to this vertex
        for (int i = edges.Count - 1; i >= 0; i--)
        {
            if (edges[i].fromIndex == vertices.Count || edges[i].toIndex == vertices.Count)
            {
                if (edges[i].gameObject != null)
                    Destroy(edges[i].gameObject);
                if (edges[i].arrow != null)
                    Destroy(edges[i].arrow);
                edges.RemoveAt(i);
            }
        }
        
        // Remove from adjacency list
        if (adjacencyList.ContainsKey(lastVertex))
        {
            adjacencyList.Remove(lastVertex);
        }
        
        Destroy(lastVertex);
        Debug.Log($" Removed vertex. Count: {vertices.Count}");
          // ⭐ NOTIFY OPTIMIZER
    if (arOptimizer != null)
    {
        arOptimizer.OnStructureReset();
    }
    }
    
    public void AddEdgeBetweenVertices(int fromIndex, int toIndex)
    {
        if (vertices.Count < 2)
        {
            Debug.Log("Need at least 2 vertices!");
            return;
        }
        
        fromIndex = Mathf.Clamp(fromIndex, 0, vertices.Count - 1);
        toIndex = Mathf.Clamp(toIndex, 0, vertices.Count - 1);
        
        if (fromIndex == toIndex)
        {
            Debug.Log("Cannot create self-loop!");
            return;
        }
        
        // Check if edge already exists
        foreach (Edge e in edges)
        {
            if (e.fromIndex == fromIndex && e.toIndex == toIndex)
            {
                Debug.Log("Edge already exists!");
                return;
            }
            // For undirected graphs, check reverse too
            if (!isDirected && e.fromIndex == toIndex && e.toIndex == fromIndex)
            {
                Debug.Log("Edge already exists (undirected)!");
                return;
            }
        }
        
        CreateEdge(fromIndex, toIndex);
        
        // Update adjacency list
        adjacencyList[vertices[fromIndex]].Add(vertices[toIndex]);
        if (!isDirected)
        {
            adjacencyList[vertices[toIndex]].Add(vertices[fromIndex]);
        }
        
        Debug.Log($" Added edge: {fromIndex + 1} → {toIndex + 1}");
          // ⭐ NOTIFY OPTIMIZER
    if (arOptimizer != null)
    {
        arOptimizer.OnStructureReset();
    }
    }
    
    public void AddRandomEdge()
    {
        if (vertices.Count < 2)
        {
            Debug.Log("Need at least 2 vertices!");
            return;
        }
        
        int attempts = 0;
        while (attempts < 20) // Try up to 20 times
        {
            int from = Random.Range(0, vertices.Count);
            int to = Random.Range(0, vertices.Count);
            
            if (from != to)
            {
                // Check if edge already exists
                bool exists = false;
                foreach (Edge e in edges)
                {
                    if ((e.fromIndex == from && e.toIndex == to) ||
                        (!isDirected && e.fromIndex == to && e.toIndex == from))
                    {
                        exists = true;
                        break;
                    }
                }
                
                if (!exists)
                {
                    AddEdgeBetweenVertices(from, to);
                    return;
                }
            }
            attempts++;
        }
        
        Debug.Log("Could not find valid edge to add!");
          // ⭐ NOTIFY OPTIMIZER
    if (arOptimizer != null)
    {
        arOptimizer.OnStructureReset();
    }
    }
    
    public void RemoveLastEdge()
    {
        if (edges.Count == 0)
        {
            Debug.Log("No edges to remove!");
            return;
        }
        
        Edge lastEdge = edges[edges.Count - 1];
        
        // Update adjacency list
        if (lastEdge.fromIndex < vertices.Count && lastEdge.toIndex < vertices.Count)
        {
            adjacencyList[vertices[lastEdge.fromIndex]].Remove(vertices[lastEdge.toIndex]);
            if (!isDirected)
            {
                adjacencyList[vertices[lastEdge.toIndex]].Remove(vertices[lastEdge.fromIndex]);
            }
        }
        
        if (lastEdge.gameObject != null)
            Destroy(lastEdge.gameObject);
        if (lastEdge.arrow != null)
            Destroy(lastEdge.arrow);
        
        edges.RemoveAt(edges.Count - 1);
        Debug.Log($" Removed edge. Count: {edges.Count}");
          // ⭐ NOTIFY OPTIMIZER
    if (arOptimizer != null)
    {
        arOptimizer.OnStructureReset();
    }
    }
    
    public void ToggleDirected()
    {
        isDirected = !isDirected;
        
        // Recreate all edges with new direction setting
        List<(int, int)> edgeList = new List<(int, int)>();
        foreach (Edge e in edges)
        {
            edgeList.Add((e.fromIndex, e.toIndex));
        }
        
        // Clear and recreate
        foreach (Edge e in edges)
        {
            if (e.gameObject != null) Destroy(e.gameObject);
            if (e.arrow != null) Destroy(e.arrow);
        }
        edges.Clear();
        
        // Rebuild adjacency list
        foreach (var vertex in adjacencyList.Keys)
        {
            adjacencyList[vertex].Clear();
        }
        
        foreach (var (from, to) in edgeList)
        {
            if (from < vertices.Count && to < vertices.Count)
            {
                CreateEdge(from, to);
                adjacencyList[vertices[from]].Add(vertices[to]);
                if (!isDirected)
                {
                    adjacencyList[vertices[to]].Add(vertices[from]);
                }
            }
        }
        
        Debug.Log($"Graph is now {(isDirected ? "DIRECTED" : "UNDIRECTED")}");
          // ⭐ NOTIFY OPTIMIZER
    if (arOptimizer != null)
    {
        arOptimizer.OnStructureReset();
    }

    }

        private void CheckSurfaceDetection()
    {
        if (raycastManager == null || isPlaced)
        {
            return;
        }
        
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
    
    public void Clear()
    {
        foreach (GameObject vertex in vertices)
        {
            if (vertex != null) Destroy(vertex);
        }
        foreach (Edge edge in edges)
        {
            if (edge.gameObject != null) Destroy(edge.gameObject);
            if (edge.arrow != null) Destroy(edge.arrow);
        }
        
        vertices.Clear();
        edges.Clear();
        adjacencyList.Clear();
        vertexIdCounter = 1;
        isPlaced = false;
        isSurfaceDetected = false;
        hadPlanesLastFrame = false;
          // ⭐ NOTIFY OPTIMIZER
    if (arOptimizer != null)
    {
        arOptimizer.OnStructureReset();
    }
       if (detectionFeedbackText != null)
        {
            detectionFeedbackText.gameObject.SetActive(true);
            bool hasPlanes = planeManager != null && planeManager.trackables.count > 0;
            UpdateDetectionFeedback(false, hasPlanes);
        }
        // Show planes again
        ShowPlanes();
        
        Debug.Log("🗑️ Graph cleared and reset!");
    }
    
    public int GetVertexCount()
    {
        return vertices.Count;
    }
    
    public int GetEdgeCount()
    {
        return edges.Count;
    }
    
    public string GetGraphType()
    {
        return isDirected ? "Directed" : "Undirected";
    }
    
    public int GetVertexDegree(int vertexIndex)
    {
        if (vertexIndex < 0 || vertexIndex >= vertices.Count)
            return 0;
        
        return adjacencyList[vertices[vertexIndex]].Count;
    }
    
    // Hide AR planes
 void HidePlanes()
{
    if (planeManager != null)
    {
        // Disable plane detection
        planeManager.enabled = false;
        
        // Hide all existing planes completely
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(false);
            
            // Also disable the mesh renderer and line renderer
            var meshRenderer = plane.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
            
            var lineRenderer = plane.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
            
            // Disable ARPlaneMeshVisualizer if present
            var meshVisualizer = plane.GetComponent<ARPlaneMeshVisualizer>();
            if (meshVisualizer != null)
            {
                meshVisualizer.enabled = false;
            }
        }
        
        Debug.Log("👻 AR Planes completely hidden");
    }
}
    
    // Show AR planes
void ShowPlanes()
{
    if (planeManager != null)
    {
        // Re-enable plane detection
        planeManager.enabled = true;
        
        // Show all existing planes
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(true);
            
            // Re-enable the mesh renderer and line renderer
            var meshRenderer = plane.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = true;
            }
            
            var lineRenderer = plane.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
            }
            
            // Re-enable ARPlaneMeshVisualizer if present
            var meshVisualizer = plane.GetComponent<ARPlaneMeshVisualizer>();
            if (meshVisualizer != null)
            {
                meshVisualizer.enabled = true;
            }
        }
        
        Debug.Log("👁️ AR Planes visible again");
    }
}

    
    void CreateEdge(int fromIndex, int toIndex)
    {
        if (edgePrefab == null || fromIndex >= vertices.Count || toIndex >= vertices.Count) return;
        
        Vector3 start = vertices[fromIndex].transform.position;
        Vector3 end = vertices[toIndex].transform.position;
        Vector3 midPoint = (start + end) / 2f;
        
        GameObject edge = Instantiate(edgePrefab, midPoint, Quaternion.identity);
        edge.transform.SetParent(transform);
        edge.transform.LookAt(end);
        edge.transform.localScale = new Vector3(0.03f, 0.03f, Vector3.Distance(start, end));
        
        Renderer renderer = edge.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
        }
        
        Edge newEdge = new Edge
        {
            gameObject = edge,
            fromIndex = fromIndex,
            toIndex = toIndex
        };
        
        // Add arrow for directed graphs
        if (isDirected)
        {
            Vector3 arrowPos = Vector3.Lerp(start, end, 0.75f); // 75% along the edge
            GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.transform.position = arrowPos;
            arrow.transform.SetParent(transform);
            arrow.transform.LookAt(end);
            arrow.transform.localScale = new Vector3(0.08f, 0.08f, 0.02f); // Triangle-like shape
            
            Renderer arrowRenderer = arrow.GetComponent<Renderer>();
            if (arrowRenderer != null)
            {
                arrowRenderer.material.color = Color.yellow;
            }
            
            // Remove the collider to avoid interference
            Collider arrowCollider = arrow.GetComponent<Collider>();
            if (arrowCollider != null)
            {
                Destroy(arrowCollider);
            }
            
            newEdge.arrow = arrow;
        }
        
        edges.Add(newEdge);
    }
    
    System.Collections.IEnumerator AnimateAddVertex(GameObject vertex, Vector3 targetPos)
    {
        Vector3 startPos = centerPosition + Vector3.up * 0.3f;
        vertex.transform.position = startPos;
        vertex.transform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - elapsed / animationDuration, 3f);
            
            vertex.transform.position = Vector3.Lerp(startPos, targetPos, t);
            vertex.transform.localScale = Vector3.one * 0.12f * t;
            yield return null;
        }
        
        vertex.transform.position = targetPos;
        vertex.transform.localScale = Vector3.one * 0.12f;
    }
}