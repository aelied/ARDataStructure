using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class PhysicalGraphManager : MonoBehaviour
{
    [Header("Graph Settings")]
    public int maxVertices = 10;
    public int maxEdgesPerVertex = 5;
    
    [Header("Virtual Label Prefabs")]
    public GameObject vertexLabelPrefab;
    
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
    public Color vertexColor = Color.cyan;
    public Color edgeColor = Color.yellow;
    public Color directedEdgeColor = Color.magenta;
    
    private enum GraphState
    {
        WaitingForSurface,
        SurfaceDetected,
        GraphReady,
        AddingVertex,
        SelectingSourceVertex,
        SelectingTargetVertex
    }
    
    private GraphState currentState = GraphState.WaitingForSurface;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool isSurfaceDetected = false;
    
    private class Vertex
    {
        public int id;
        public string value;
        public Vector3 position;
        public GameObject vertexLabel;
        public GameObject valueLabel;
        public List<Edge> edges = new List<Edge>();
    }
    
    private class Edge
    {
        public Vertex source;
        public Vertex target;
        public GameObject edgeObject;
        public bool isDirected;
        public float weight;
    }
    
    private List<Vertex> vertices = new List<Vertex>();
    private List<Edge> edges = new List<Edge>();
    private int vertexIdCounter = 0;
    
    // For edge creation
    private Vertex selectedSourceVertex;
    private Vertex selectedTargetVertex;
    private GameObject vertexIndicator;
    private bool isDirectedMode = false;
    
    void Start()
    {
        if (planeManager == null)
            planeManager = FindObjectOfType<ARPlaneManager>();
        
        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>();
        
        UpdateInstructions();
        
        if (detectionFeedbackText != null)
            detectionFeedbackText.gameObject.SetActive(true);
        
        currentState = GraphState.WaitingForSurface;
        
        Debug.Log("🕸️ Physical Graph Manager Initialized");
    }
    
    void Update()
    {
        switch (currentState)
        {
            case GraphState.WaitingForSurface:
                CheckForSurfaceDetection();
                break;
                
            case GraphState.SurfaceDetected:
                CheckForSurfacePlacement();
                break;
                
            case GraphState.AddingVertex:
                DetectNewVertex();
                break;
                
            case GraphState.GraphReady:
            case GraphState.SelectingSourceVertex:
            case GraphState.SelectingTargetVertex:
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
            currentState = GraphState.SurfaceDetected;
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
            HidePlanes();
            
            if (detectionFeedbackText != null)
                detectionFeedbackText.gameObject.SetActive(false);
            
            currentState = GraphState.GraphReady;
            UpdateInstructions();
            
            Debug.Log("✅ Surface placed - Graph ready");
        }
    }
    
    void DetectNewVertex()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            TapNewVertex(Input.GetTouch(0).position);
        else if (Input.GetMouseButtonDown(0))
            TapNewVertex(Input.mousePosition);
    }
    
    void TapNewVertex(Vector2 touchPosition)
    {
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Vector3 vertexPosition = hits[0].pose.position;
            
            Vertex newVertex = new Vertex
            {
                id = vertexIdCounter++,
                value = $"V{vertexIdCounter - 1}",
                position = vertexPosition
            };
            
            CreateVertexVisualization(newVertex);
            vertices.Add(newVertex);
            
            if (vertexIndicator != null)
            {
                Destroy(vertexIndicator);
                vertexIndicator = null;
            }
            
            UpdateStatusText();
            
            currentState = GraphState.GraphReady;
            if (instructionText != null)
                instructionText.text = "✅ Vertex added! Ready for next operation";
            
            Debug.Log($"✅ Vertex {newVertex.id} added");
        }
    }
    
    void CreateVertexVisualization(Vertex vertex)
    {
        // Create vertex sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = $"Vertex_{vertex.id}";
        sphere.transform.position = vertex.position + Vector3.up * 0.03f;
        sphere.transform.localScale = Vector3.one * 0.05f;
        
        Renderer rend = sphere.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = vertexColor;
        rend.material = mat;
        
        // Create label
        if (vertexLabelPrefab != null)
        {
            vertex.vertexLabel = Instantiate(vertexLabelPrefab);
            vertex.vertexLabel.transform.position = vertex.position + Vector3.up * 0.12f;
            
            TextMeshPro labelText = vertex.vertexLabel.GetComponentInChildren<TextMeshPro>();
            if (labelText != null)
                labelText.text = $"🔵 {vertex.value}";
        }
        
        // Create ID label below
        if (vertexLabelPrefab != null)
        {
            vertex.valueLabel = Instantiate(vertexLabelPrefab);
            vertex.valueLabel.transform.position = vertex.position + Vector3.down * 0.05f;
            
            TextMeshPro valueText = vertex.valueLabel.GetComponentInChildren<TextMeshPro>();
            if (valueText != null)
                valueText.text = $"ID:{vertex.id}";
        }
    }
    
    public void StartAddVertex()
    {
        if (currentState != GraphState.GraphReady) return;
        
        if (vertices.Count >= maxVertices)
        {
            Debug.LogWarning("⚠️ Graph is full!");
            if (instructionText != null)
                instructionText.text = "⚠️ Max vertices reached!";
            return;
        }
        
        // Show indicator at a default position
        Vector3 indicatorPos = vertices.Count > 0 ? 
            vertices[vertices.Count - 1].position + Vector3.forward * 0.15f : 
            Vector3.up * 0.1f;
        
        vertexIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vertexIndicator.name = "VertexIndicator";
        vertexIndicator.transform.position = indicatorPos;
        vertexIndicator.transform.localScale = Vector3.one * 0.04f;
        
        Renderer rend = vertexIndicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0, 1, 0, 0.5f);
        rend.material = mat;
        
        Collider col = vertexIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        currentState = GraphState.AddingVertex;
        
        if (instructionText != null)
            instructionText.text = "➕ Place coin for new vertex, then TAP it";
        
        Debug.Log("🟢 Waiting for vertex placement");
    }
    
    public void StartAddEdge(bool directed)
    {
        if (currentState != GraphState.GraphReady) return;
        
        if (vertices.Count < 2)
        {
            Debug.LogWarning("⚠️ Need at least 2 vertices!");
            if (instructionText != null)
                instructionText.text = "⚠️ Need at least 2 vertices!";
            return;
        }
        
        isDirectedMode = directed;
        currentState = GraphState.SelectingSourceVertex;
        
        if (instructionText != null)
            instructionText.text = "🎯 Select SOURCE vertex (use UI button with vertex ID)";
        
        Debug.Log("Waiting for source vertex selection");
    }
    
    public void SelectSourceVertex(int vertexId)
    {
        if (currentState != GraphState.SelectingSourceVertex) return;
        
        selectedSourceVertex = FindVertexById(vertexId);
        
        if (selectedSourceVertex == null)
        {
            Debug.LogWarning("⚠️ Vertex not found!");
            if (instructionText != null)
                instructionText.text = "⚠️ Vertex not found!";
            return;
        }
        
        // Highlight source
        HighlightVertex(selectedSourceVertex, Color.green);
        
        currentState = GraphState.SelectingTargetVertex;
        
        if (instructionText != null)
            instructionText.text = $"🎯 Select TARGET vertex (source: V{selectedSourceVertex.id})";
        
        Debug.Log($"Source selected: V{selectedSourceVertex.id}");
    }
    
    public void SelectTargetVertex(int vertexId)
    {
        if (currentState != GraphState.SelectingTargetVertex) return;
        
        selectedTargetVertex = FindVertexById(vertexId);
        
        if (selectedTargetVertex == null)
        {
            Debug.LogWarning("⚠️ Vertex not found!");
            if (instructionText != null)
                instructionText.text = "⚠️ Vertex not found!";
            return;
        }
        
        if (selectedTargetVertex == selectedSourceVertex)
        {
            Debug.LogWarning("⚠️ Cannot connect vertex to itself!");
            if (instructionText != null)
                instructionText.text = "⚠️ Cannot connect to itself!";
            return;
        }
        
        // Check if edge already exists
        if (EdgeExists(selectedSourceVertex, selectedTargetVertex))
        {
            Debug.LogWarning("⚠️ Edge already exists!");
            if (instructionText != null)
                instructionText.text = "⚠️ Edge already exists!";
            
            UnhighlightVertex(selectedSourceVertex);
            currentState = GraphState.GraphReady;
            return;
        }
        
        // Create edge
        CreateEdge(selectedSourceVertex, selectedTargetVertex, isDirectedMode);
        
        UnhighlightVertex(selectedSourceVertex);
        
        UpdateStatusText();
        
        currentState = GraphState.GraphReady;
        if (instructionText != null)
            instructionText.text = $"✅ Edge created: V{selectedSourceVertex.id} → V{selectedTargetVertex.id}";
        
        Debug.Log($"✅ Edge created: {selectedSourceVertex.id} → {selectedTargetVertex.id}");
    }
    
    void CreateEdge(Vertex source, Vertex target, bool directed)
    {
        Edge edge = new Edge
        {
            source = source,
            target = target,
            isDirected = directed,
            weight = 1.0f
        };
        
        GameObject edgeObj = new GameObject($"Edge_{source.id}_{target.id}");
        LineRenderer lr = edgeObj.AddComponent<LineRenderer>();
        
        lr.material = new Material(Shader.Find("Sprites/Default"));
        Color color = directed ? directedEdgeColor : edgeColor;
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.012f;
        lr.endWidth = 0.012f;
        lr.positionCount = 2;
        
        Vector3 start = source.position + Vector3.up * 0.04f;
        Vector3 end = target.position + Vector3.up * 0.04f;
        
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.useWorldSpace = true;
        
        edge.edgeObject = edgeObj;
        
        source.edges.Add(edge);
        if (!directed)
        {
            // For undirected, add reverse edge
            Edge reverseEdge = new Edge
            {
                source = target,
                target = source,
                isDirected = false,
                weight = 1.0f,
                edgeObject = edgeObj
            };
            target.edges.Add(reverseEdge);
        }
        
        edges.Add(edge);
        
        // Add arrow for directed edges
        if (directed)
        {
            CreateArrowhead(start, end, edgeObj.transform);
        }
    }
    
    void CreateArrowhead(Vector3 start, Vector3 end, Transform parent)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 arrowPos = end - direction * 0.03f;
        
        // Create arrowhead using a cylinder (pointed like an arrow)
        GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arrow.name = "Arrowhead";
        arrow.transform.parent = parent;
        arrow.transform.position = arrowPos;
        arrow.transform.localScale = new Vector3(0.015f, 0.025f, 0.015f); // Thin and tall
        arrow.transform.rotation = Quaternion.LookRotation(direction);
        arrow.transform.Rotate(90, 0, 0); // Point it in the right direction
        
        Renderer rend = arrow.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = directedEdgeColor;
        rend.material = mat;
        
        // Remove collider
        Collider col = arrow.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }
    
    bool EdgeExists(Vertex source, Vertex target)
    {
        foreach (var edge in source.edges)
        {
            if (edge.target == target)
                return true;
        }
        return false;
    }
    
    void HighlightVertex(Vertex vertex, Color color)
    {
        GameObject sphere = GameObject.Find($"Vertex_{vertex.id}");
        if (sphere != null)
        {
            Renderer rend = sphere.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = color;
        }
    }
    
    void UnhighlightVertex(Vertex vertex)
    {
        HighlightVertex(vertex, vertexColor);
    }
    
    Vertex FindVertexById(int id)
    {
        foreach (var vertex in vertices)
        {
            if (vertex.id == id)
                return vertex;
        }
        return null;
    }
    
    public void RemoveVertex(int vertexId)
    {
        if (currentState != GraphState.GraphReady) return;
        
        Vertex vertex = FindVertexById(vertexId);
        
        if (vertex == null)
        {
            Debug.LogWarning("⚠️ Vertex not found!");
            if (instructionText != null)
                instructionText.text = "⚠️ Vertex not found!";
            return;
        }
        
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.transform.position = vertex.position + Vector3.up * 0.05f;
        indicator.transform.localScale = Vector3.one * 0.08f;
        
        Renderer rend = indicator.GetComponent<Renderer>();
        rend.material.color = new Color(1, 0, 0, 0.7f);
        
        if (instructionText != null)
            instructionText.text = $"➖ Removing vertex {vertexId} and its edges...";
        
        Destroy(indicator, 3f);
        StartCoroutine(RemoveVertexAfterDelay(vertex, 2f));
    }
    
    IEnumerator RemoveVertexAfterDelay(Vertex vertex, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Remove all edges connected to this vertex
        for (int i = edges.Count - 1; i >= 0; i--)
        {
            if (edges[i].source == vertex || edges[i].target == vertex)
            {
                if (edges[i].edgeObject != null)
                    Destroy(edges[i].edgeObject);
                edges.RemoveAt(i);
            }
        }
        
        // Remove from other vertices' edge lists
        foreach (var v in vertices)
        {
            v.edges.RemoveAll(e => e.source == vertex || e.target == vertex);
        }
        
        // Destroy visuals
        if (vertex.vertexLabel != null)
            Destroy(vertex.vertexLabel);
        if (vertex.valueLabel != null)
            Destroy(vertex.valueLabel);
        
        GameObject sphere = GameObject.Find($"Vertex_{vertex.id}");
        if (sphere != null)
            Destroy(sphere);
        
        vertices.Remove(vertex);
        
        UpdateStatusText();
        
        if (instructionText != null)
            instructionText.text = "✅ Vertex removed! Ready for next operation";
        
        Debug.Log($"✅ Vertex {vertex.id} removed");
    }
    
    public void PerformBFS(int startVertexId)
    {
        Vertex startVertex = FindVertexById(startVertexId);
        
        if (startVertex == null)
        {
            Debug.LogWarning("⚠️ Start vertex not found!");
            if (instructionText != null)
                instructionText.text = "⚠️ Start vertex not found!";
            return;
        }
        
        List<int> visitOrder = new List<int>();
        HashSet<Vertex> visited = new HashSet<Vertex>();
        Queue<Vertex> queue = new Queue<Vertex>();
        
        queue.Enqueue(startVertex);
        visited.Add(startVertex);
        
        while (queue.Count > 0)
        {
            Vertex current = queue.Dequeue();
            visitOrder.Add(current.id);
            
            foreach (var edge in current.edges)
            {
                if (!visited.Contains(edge.target))
                {
                    visited.Add(edge.target);
                    queue.Enqueue(edge.target);
                }
            }
        }
        
        string result = string.Join(" → ", visitOrder);
        if (instructionText != null)
            instructionText.text = $"📖 BFS from V{startVertexId}: {result}";
        
        Debug.Log($"BFS Traversal: {result}");
    }
    
    public void PerformDFS(int startVertexId)
    {
        Vertex startVertex = FindVertexById(startVertexId);
        
        if (startVertex == null)
        {
            Debug.LogWarning("⚠️ Start vertex not found!");
            if (instructionText != null)
                instructionText.text = "⚠️ Start vertex not found!";
            return;
        }
        
        List<int> visitOrder = new List<int>();
        HashSet<Vertex> visited = new HashSet<Vertex>();
        
        DFSRecursive(startVertex, visited, visitOrder);
        
        string result = string.Join(" → ", visitOrder);
        if (instructionText != null)
            instructionText.text = $"📖 DFS from V{startVertexId}: {result}";
        
        Debug.Log($"DFS Traversal: {result}");
    }
    
    void DFSRecursive(Vertex vertex, HashSet<Vertex> visited, List<int> order)
    {
        visited.Add(vertex);
        order.Add(vertex.id);
        
        foreach (var edge in vertex.edges)
        {
            if (!visited.Contains(edge.target))
            {
                DFSRecursive(edge.target, visited, order);
            }
        }
    }
    
    void UpdateStatusText()
    {
        if (statusText == null) return;
        
        statusText.text = $"Vertices: {vertices.Count} | Edges: {edges.Count}";
    }
    
    void UpdateInstructions()
    {
        if (instructionText == null) return;
        
        switch (currentState)
        {
            case GraphState.WaitingForSurface:
                instructionText.text = "🔍 Move phone to scan surfaces";
                break;
                
            case GraphState.SurfaceDetected:
                instructionText.text = "✅ Tap to place graph area";
                break;
                
            case GraphState.GraphReady:
                instructionText.text = "✅ Graph ready! Use Add Vertex/Edge buttons";
                break;
        }
    }
    
    public void ResetGraph()
    {
        foreach (var vertex in vertices)
        {
            if (vertex.vertexLabel != null)
                Destroy(vertex.vertexLabel);
            if (vertex.valueLabel != null)
                Destroy(vertex.valueLabel);
            
            GameObject sphere = GameObject.Find($"Vertex_{vertex.id}");
            if (sphere != null)
                Destroy(sphere);
        }
        vertices.Clear();
        
        foreach (var edge in edges)
        {
            if (edge.edgeObject != null)
                Destroy(edge.edgeObject);
        }
        edges.Clear();
        
        if (vertexIndicator != null)
        {
            Destroy(vertexIndicator);
            vertexIndicator = null;
        }
        
        selectedSourceVertex = null;
        selectedTargetVertex = null;
        vertexIdCounter = 0;
        isSurfaceDetected = false;
        
        ShowPlanes();
        
        currentState = GraphState.WaitingForSurface;
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.gameObject.SetActive(true);
            detectionFeedbackText.text = "🔍 Scan environment slowly";
            detectionFeedbackText.color = notDetectingColor;
        }
        
        if (statusText != null)
            statusText.text = "Vertices: 0 | Edges: 0";
        
        UpdateInstructions();
        
        Debug.Log("🔄 Graph completely reset");
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
    
    public bool IsGraphReady()
    {
        return currentState == GraphState.GraphReady;
    }
    
    public int GetVertexCount()
    {
        return vertices.Count;
    }
    
    public int GetEdgeCount()
    {
        return edges.Count;
    }
}