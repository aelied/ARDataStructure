using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class PhysicalQueueManager : MonoBehaviour
{
    [Header("Queue Settings")]
    public int minObjects = 2;
    public int maxObjects = 6;
    public float detectionRadius = 0.05f;
    
    [Header("Virtual Label Prefabs")]
    public GameObject indexLabelPrefab;
    public GameObject frontMarkerPrefab;
    public GameObject backMarkerPrefab;
    
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
    
    // State management
    private enum QueueState
    {
        WaitingForSurface,
        SurfaceDetected,
        PlacingFirstObject,
        PlacingSecondObject,
        ShowingGuideZone,
        QueueReady,
        WaitingForEnqueuePlacement
    }
    
    private QueueState currentState = QueueState.WaitingForSurface;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private Vector3 surfacePosition;
    private Quaternion surfaceRotation;
    private bool surfacePlaced = false;
    private bool isSurfaceDetected = false;
    
    // Physical object tracking
    private class PhysicalQueueNode
    {
        public Vector3 position;
        public GameObject indexLabel;
        public int index;
        public string value;
    }
    
    private List<PhysicalQueueNode> physicalNodes = new List<PhysicalQueueNode>();
    private GameObject frontMarker;
    private GameObject backMarker;
    private GameObject leftGuideLine;
    private GameObject rightGuideLine;
    
    private Vector3 firstObjectPosition;
    private Vector3 secondObjectPosition;
    private float objectSpacing;
    private Vector3 queueDirection;
    
    private Vector3 expectedEnqueuePosition;
    private GameObject enqueueIndicator;
    
    void Start()
    {
        if (planeManager == null)
            planeManager = FindObjectOfType<ARPlaneManager>();
        
        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>();
        
        UpdateInstructions();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.gameObject.SetActive(true);
        }
        
        Debug.Log("🎯 Physical Queue Manager Initialized");
    }
    
    void Update()
    {
        switch (currentState)
        {
            case QueueState.WaitingForSurface:
                CheckForSurfaceDetection();
                break;
                
            case QueueState.SurfaceDetected:
                CheckForSurfacePlacement();
                break;
                
            case QueueState.PlacingFirstObject:
                DetectFirstObject();
                break;
                
            case QueueState.PlacingSecondObject:
                DetectSecondObject();
                break;
                
            case QueueState.WaitingForEnqueuePlacement:
                DetectEnqueueObject();
                break;
                
            case QueueState.ShowingGuideZone:
            case QueueState.QueueReady:
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
        {
            currentState = QueueState.SurfaceDetected;
        }
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
        {
            PlaceSurface(Input.GetTouch(0).position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            PlaceSurface(Input.mousePosition);
        }
    }
    
    void PlaceSurface(Vector2 touchPosition)
    {
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            surfacePosition = hitPose.position;
            surfaceRotation = hitPose.rotation;
            surfacePlaced = true;
            
            HidePlanes();
            
            if (detectionFeedbackText != null)
            {
                detectionFeedbackText.gameObject.SetActive(false);
            }
            
            currentState = QueueState.PlacingFirstObject;
            UpdateInstructions();
            
            Debug.Log("✅ Surface placed at: " + surfacePosition);
        }
    }
    
    void DetectFirstObject()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TapFirstObject(Input.GetTouch(0).position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            TapFirstObject(Input.mousePosition);
        }
    }
    
    void TapFirstObject(Vector2 touchPosition)
    {
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            firstObjectPosition = hits[0].pose.position;
            CreateVirtualLabel(firstObjectPosition, 0, "Coin1");
            
            currentState = QueueState.PlacingSecondObject;
            UpdateInstructions();
            
            Debug.Log("🪙 First object marked at: " + firstObjectPosition);
        }
    }
    
    void DetectSecondObject()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TapSecondObject(Input.GetTouch(0).position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            TapSecondObject(Input.mousePosition);
        }
    }
    
    void TapSecondObject(Vector2 touchPosition)
    {
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            secondObjectPosition = hits[0].pose.position;
            
            queueDirection = (secondObjectPosition - firstObjectPosition).normalized;
            objectSpacing = Vector3.Distance(firstObjectPosition, secondObjectPosition);
            
            CreateVirtualLabel(secondObjectPosition, 1, "Coin2");
            CreateArrowBetween(firstObjectPosition, secondObjectPosition);
            CreateAdaptiveGuideZone();
            ShowFrontBackMarkers();
            
            currentState = QueueState.ShowingGuideZone;
            UpdateInstructions();
            
            Debug.Log($"🪙 Second object marked. Spacing: {objectSpacing:F3}m");
        }
    }
    
    void DetectEnqueueObject()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TapEnqueueObject(Input.GetTouch(0).position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            TapEnqueueObject(Input.mousePosition);
        }
    }
    
    void TapEnqueueObject(Vector2 touchPosition)
    {
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Vector3 tappedPosition = hits[0].pose.position;
            float distance = Vector3.Distance(tappedPosition, expectedEnqueuePosition);
            
            if (distance < objectSpacing * 0.8f)
            {
                int newIndex = physicalNodes.Count;
                CreateVirtualLabel(tappedPosition, newIndex, $"Coin{newIndex + 1}");
                
                if (enqueueIndicator != null)
                {
                    Destroy(enqueueIndicator);
                    enqueueIndicator = null;
                }
                
                UpdateFrontBackMarkers();
                ExpandParallelLines();
                
                currentState = QueueState.QueueReady;
                if (instructionText != null)
                    instructionText.text = "✅ Enqueued! Ready for next operation";
                
                Debug.Log($"✅ New object enqueued at index {newIndex}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Tap too far from expected position. Distance: {distance:F3}m (expected < {objectSpacing * 0.8f:F3}m)");
                if (instructionText != null)
                    instructionText.text = "⚠️ Too far! Tap near the GREEN spot";
            }
        }
    }
    
    void ExpandParallelLines()
    {
        if (physicalNodes.Count < 2) return;
        
        Vector3 perpendicular = Vector3.Cross(queueDirection, Vector3.up).normalized;
        float lineWidth = objectSpacing * 0.6f;
        
        Vector3 lastCoinPosition = physicalNodes[physicalNodes.Count - 1].position;
        Vector3 newEndPosition = lastCoinPosition + queueDirection * (objectSpacing * 2);
        
        if (leftGuideLine != null)
        {
            LineRenderer leftLR = leftGuideLine.GetComponent<LineRenderer>();
            if (leftLR != null)
            {
                Vector3 leftLineEnd = newEndPosition - perpendicular * lineWidth;
                leftLR.SetPosition(1, leftLineEnd + Vector3.up * 0.002f);
            }
        }
        
        if (rightGuideLine != null)
        {
            LineRenderer rightLR = rightGuideLine.GetComponent<LineRenderer>();
            if (rightLR != null)
            {
                Vector3 rightLineEnd = newEndPosition + perpendicular * lineWidth;
                rightLR.SetPosition(1, rightLineEnd + Vector3.up * 0.002f);
            }
        }
        
        Debug.Log("📏 Parallel lines expanded");
    }
    
    void CreateAdaptiveGuideZone()
    {
        Vector3 perpendicular = Vector3.Cross(queueDirection, Vector3.up).normalized;
        float lineWidth = objectSpacing * 0.6f;
        
        Vector3 leftLineStart = firstObjectPosition - perpendicular * lineWidth;
        Vector3 leftLineEnd = secondObjectPosition + queueDirection * (objectSpacing * 3) - perpendicular * lineWidth;
        
        Vector3 rightLineStart = firstObjectPosition + perpendicular * lineWidth;
        Vector3 rightLineEnd = secondObjectPosition + queueDirection * (objectSpacing * 3) + perpendicular * lineWidth;
        
        leftGuideLine = CreateLine(leftLineStart, leftLineEnd, Color.cyan);
        rightGuideLine = CreateLine(rightLineStart, rightLineEnd, Color.cyan);
        
        Debug.Log("📏 Adaptive guide zone created");
    }
    
    GameObject CreateLine(Vector3 start, Vector3 end, Color color)
    {
        GameObject lineObj = new GameObject("GuideLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        
        // Use Unlit shader for better visibility
        lr.material = new Material(Shader.Find("Unlit/Color"));
        lr.material.color = color;
        
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.012f;
        lr.endWidth = 0.012f;
        lr.positionCount = 2;
        
        // Lift lines slightly above surface to avoid z-fighting
        lr.SetPosition(0, start + Vector3.up * 0.002f);
        lr.SetPosition(1, end + Vector3.up * 0.002f);
        lr.useWorldSpace = true;
        
        // Ensure it renders on top
        lr.material.renderQueue = 3000;
        
        return lineObj;
    }
    
    void CreateArrowBetween(Vector3 start, Vector3 end)
    {
        GameObject arrow = new GameObject("Arrow");
        LineRenderer lr = arrow.AddComponent<LineRenderer>();
        
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.yellow;
        lr.endColor = Color.yellow;
        lr.startWidth = 0.015f;
        lr.endWidth = 0.015f;
        lr.positionCount = 2;
        
        Vector3 arrowStart = start + Vector3.up * 0.05f;
        Vector3 arrowEnd = end + Vector3.up * 0.05f;
        
        lr.SetPosition(0, arrowStart);
        lr.SetPosition(1, arrowEnd);
        lr.useWorldSpace = true;
    }
    
    void CreateVirtualLabel(Vector3 position, int index, string value)
    {
        PhysicalQueueNode node = new PhysicalQueueNode
        {
            position = position,
            index = index,
            value = value
        };
        
        if (indexLabelPrefab != null)
        {
            node.indexLabel = Instantiate(indexLabelPrefab);
            node.indexLabel.transform.position = position + Vector3.up * 0.15f;
            
            TextMeshPro labelText = node.indexLabel.GetComponentInChildren<TextMeshPro>();
            if (labelText != null)
            {
                labelText.text = $"[{index}]";
            }
        }
        
        physicalNodes.Add(node);
        
        if (statusText != null)
            statusText.text = $"Coins: {physicalNodes.Count}";
    }
    
    void ShowFrontBackMarkers()
    {
        if (physicalNodes.Count < 2) return;
        
        Vector3 frontPos = physicalNodes[0].position;
        Vector3 backPos = physicalNodes[physicalNodes.Count - 1].position;
        
        if (frontMarkerPrefab != null && frontMarker == null)
        {
            frontMarker = Instantiate(frontMarkerPrefab);
        }
        
        if (frontMarker != null)
        {
            frontMarker.transform.position = frontPos + Vector3.up * 0.25f;
        }
        
        if (backMarkerPrefab != null && backMarker == null)
        {
            backMarker = Instantiate(backMarkerPrefab);
        }
        
        if (backMarker != null)
        {
            backMarker.transform.position = backPos + Vector3.up * 0.25f;
        }
    }
    
    void UpdateFrontBackMarkers()
    {
        if (physicalNodes.Count == 0) return;
        
        Vector3 frontPos = physicalNodes[0].position;
        Vector3 backPos = physicalNodes[physicalNodes.Count - 1].position;
        
        if (frontMarker != null)
        {
            frontMarker.transform.position = frontPos + Vector3.up * 0.25f;
        }
        
        if (backMarker != null)
        {
            backMarker.transform.position = backPos + Vector3.up * 0.25f;
        }
    }
    
    void UpdateInstructions()
    {
        if (instructionText == null) return;
        
        switch (currentState)
        {
            case QueueState.WaitingForSurface:
                instructionText.text = "🔍 Move phone to scan surfaces";
                break;
                
            case QueueState.SurfaceDetected:
                instructionText.text = "✅ Tap to place queue area";
                break;
                
            case QueueState.PlacingFirstObject:
                instructionText.text = "🪙 Place first coin, then TAP it";
                break;
                
            case QueueState.PlacingSecondObject:
                instructionText.text = "🪙 Place second coin to the RIGHT, then TAP it";
                break;
                
            case QueueState.ShowingGuideZone:
                instructionText.text = "✅ Parallel lines created! Ready for operations";
                currentState = QueueState.QueueReady;
                break;
                
            case QueueState.QueueReady:
                instructionText.text = "✅ Use Enqueue/Dequeue buttons";
                break;
        }
    }
    
    public void FinalizeQueue()
    {
        if (physicalNodes.Count >= minObjects)
        {
            currentState = QueueState.QueueReady;
            UpdateInstructions();
            
            if (leftGuideLine != null) leftGuideLine.SetActive(false);
            if (rightGuideLine != null) rightGuideLine.SetActive(false);
            
            Debug.Log($"✅ Queue ready with {physicalNodes.Count} objects");
        }
    }
    
    public void SimulateEnqueue()
    {
        if (currentState != QueueState.QueueReady) return;
        
        if (physicalNodes.Count >= maxObjects)
        {
            Debug.LogWarning("⚠️ Queue full!");
            if (instructionText != null)
                instructionText.text = "⚠️ Queue is full!";
            return;
        }
        
        expectedEnqueuePosition = physicalNodes[physicalNodes.Count - 1].position + queueDirection * objectSpacing;
        
        enqueueIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        enqueueIndicator.name = "EnqueueIndicator";
        enqueueIndicator.transform.position = expectedEnqueuePosition + Vector3.up * 0.05f;
        enqueueIndicator.transform.localScale = Vector3.one * 0.03f;
        
        Renderer rend = enqueueIndicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0, 1, 0, 0.7f);
        rend.material = mat;
        
        Collider col = enqueueIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        currentState = QueueState.WaitingForEnqueuePlacement;
        
        if (instructionText != null)
            instructionText.text = "➕ Place coin at GREEN spot (BACK), then TAP it";
        
        Debug.Log("🟢 Enqueue indicator placed, waiting for tap...");
    }
    
    public void SimulateDequeue()
    {
        if (currentState != QueueState.QueueReady || physicalNodes.Count == 0) return;
        
        PhysicalQueueNode frontNode = physicalNodes[0];
        
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.transform.position = frontNode.position + Vector3.up * 0.05f;
        indicator.transform.localScale = Vector3.one * 0.08f;
        
        Renderer rend = indicator.GetComponent<Renderer>();
        rend.material.color = new Color(1, 0, 0, 0.7f);
        
        if (instructionText != null)
            instructionText.text = "➖ Remove coin at RED spot (FRONT)";
        
        Destroy(indicator, 3f);
        
        StartCoroutine(RemoveFrontNodeAfterDelay(2f));
    }
    
    System.Collections.IEnumerator RemoveFrontNodeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (physicalNodes.Count > 0)
        {
            PhysicalQueueNode removedNode = physicalNodes[0];
            
            if (removedNode.indexLabel != null)
                Destroy(removedNode.indexLabel);
            
            physicalNodes.RemoveAt(0);
            
            for (int i = 0; i < physicalNodes.Count; i++)
            {
                TextMeshPro labelText = physicalNodes[i].indexLabel.GetComponentInChildren<TextMeshPro>();
                if (labelText != null)
                {
                    labelText.text = $"[{i}]";
                }
                physicalNodes[i].index = i;
            }
            
            UpdateFrontBackMarkers();
            
            if (statusText != null)
                statusText.text = $"Coins: {physicalNodes.Count}";
            
            if (instructionText != null)
                instructionText.text = "✅ Dequeued! Ready for next operation";
            
            Debug.Log("✅ Front object removed");
        }
    }
    
 public void ResetQueue()
    {
        foreach (var node in physicalNodes)
        {
            if (node.indexLabel != null)
                Destroy(node.indexLabel);
        }
        physicalNodes.Clear();
        
        if (frontMarker != null)
        {
            Destroy(frontMarker);
            frontMarker = null;
        }
        
        if (backMarker != null)
        {
            Destroy(backMarker);
            backMarker = null;
        }
        
        if (leftGuideLine != null)
        {
            Destroy(leftGuideLine);
            leftGuideLine = null;
        }
        
        if (rightGuideLine != null)
        {
            Destroy(rightGuideLine);
            rightGuideLine = null;
        }
        
        if (enqueueIndicator != null)
        {
            Destroy(enqueueIndicator);
            enqueueIndicator = null;
        }
        
        GameObject[] arrows = GameObject.FindGameObjectsWithTag("Untagged");
        foreach (GameObject obj in arrows)
        {
            if (obj.name == "Arrow")
            {
                Destroy(obj);
            }
        }
        
        firstObjectPosition = Vector3.zero;
        secondObjectPosition = Vector3.zero;
        objectSpacing = 0f;
        queueDirection = Vector3.zero;
        expectedEnqueuePosition = Vector3.zero;
        
        surfacePosition = Vector3.zero;
        surfaceRotation = Quaternion.identity;
        surfacePlaced = false;
        isSurfaceDetected = false;
        
        ShowPlanes();
        
        currentState = QueueState.WaitingForSurface;
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.gameObject.SetActive(true);
            detectionFeedbackText.text = "🔍 Scan environment slowly";
            detectionFeedbackText.color = notDetectingColor;
        }
        
        if (statusText != null)
        {
            statusText.text = "Coins: 0";
        }
        
        UpdateInstructions();
        
        Debug.Log("🔄 Queue completely reset - all objects cleared");
    }
    
    void HidePlanes()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
        }
    }
    
    void ShowPlanes()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
        }
    }
    
    public int GetQueueSize()
    {
        return physicalNodes.Count;
    }
    
    public bool IsQueueReady()
    {
        return currentState == QueueState.QueueReady;
    }
}