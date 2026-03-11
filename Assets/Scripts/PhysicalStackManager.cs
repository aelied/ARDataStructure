using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class PhysicalStackManager : MonoBehaviour
{
    [Header("Stack Settings")]
    public int maxObjects = 8;
    public float detectionRadius = 0.05f;
    
    [Header("Virtual Label Prefabs")]
    public GameObject indexLabelPrefab;
    public GameObject topMarkerPrefab;
    
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
    
    private enum StackState
    {
        WaitingForSurface,
        SurfaceDetected,
        PlacingFirstObject,
        PlacingSecondObject,
        ShowingGuideZone,
        StackReady,
        WaitingForPushPlacement
    }
    
    private StackState currentState = StackState.WaitingForSurface;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private Vector3 surfacePosition;
    private Quaternion surfaceRotation;
    private bool surfacePlaced = false;
    private bool isSurfaceDetected = false;
    
    private class StackNode
    {
        public Vector3 position;
        public GameObject indexLabel;
        public int index;
        public string value;
    }
    
    private List<StackNode> stackNodes = new List<StackNode>();
    private GameObject topMarker;
    private GameObject verticalGuideLine;
    
    private Vector3 firstObjectPosition;
    private Vector3 secondObjectPosition;
    private float objectSpacing;
    private Vector3 stackDirection;
    
    private Vector3 expectedPushPosition;
    private GameObject pushIndicator;
    
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
        
        Debug.Log("📚 Physical Stack Manager Initialized");
    }
    
    void Update()
    {
        switch (currentState)
        {
            case StackState.WaitingForSurface:
                CheckForSurfaceDetection();
                break;
                
            case StackState.SurfaceDetected:
                CheckForSurfacePlacement();
                break;
                
            case StackState.PlacingFirstObject:
                DetectFirstObject();
                break;
                
            case StackState.PlacingSecondObject:
                DetectSecondObject();
                break;
                
            case StackState.WaitingForPushPlacement:
                DetectPushObject();
                break;
                
            case StackState.ShowingGuideZone:
            case StackState.StackReady:
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
            currentState = StackState.SurfaceDetected;
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
            
            currentState = StackState.PlacingFirstObject;
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
            CreateStackNode(firstObjectPosition, 0, "Book1");
            
            currentState = StackState.PlacingSecondObject;
            UpdateInstructions();
            
            Debug.Log("📚 First stack item at: " + firstObjectPosition);
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
            
            // Stack direction should be UPWARD (vertical)
            stackDirection = (secondObjectPosition - firstObjectPosition).normalized;
            objectSpacing = Vector3.Distance(firstObjectPosition, secondObjectPosition);
            
            CreateStackNode(secondObjectPosition, 1, "Book2");
            CreateVerticalGuideLine();
            ShowTopMarker();
            
            currentState = StackState.ShowingGuideZone;
            UpdateInstructions();
            
            Debug.Log($"📚 Stack initialized. Spacing: {objectSpacing:F3}m");
        }
    }
    
    void DetectPushObject()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TapPushObject(Input.GetTouch(0).position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            TapPushObject(Input.mousePosition);
        }
    }
    
    void TapPushObject(Vector2 touchPosition)
    {
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Vector3 tappedPosition = hits[0].pose.position;
            float distance = Vector3.Distance(tappedPosition, expectedPushPosition);
            
            if (distance < objectSpacing * 0.8f)
            {
                int newIndex = stackNodes.Count;
                CreateStackNode(tappedPosition, newIndex, $"Book{newIndex + 1}");
                
                if (pushIndicator != null)
                {
                    Destroy(pushIndicator);
                    pushIndicator = null;
                }
                
                UpdateTopMarker();
                ExtendGuideLine();
                
                currentState = StackState.StackReady;
                if (instructionText != null)
                    instructionText.text = "✅ Pushed! Stack size: " + stackNodes.Count;
                
                Debug.Log($"✅ Item pushed at index {newIndex}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Tap too far from expected position. Distance: {distance:F3}m");
                if (instructionText != null)
                    instructionText.text = "⚠️ Too far! Tap near the GREEN spot";
            }
        }
    }
    
    void CreateVerticalGuideLine()
    {
        Vector3 lineStart = firstObjectPosition;
        Vector3 lineEnd = secondObjectPosition + stackDirection * (objectSpacing * 4);
        
        verticalGuideLine = CreateLine(lineStart, lineEnd, Color.cyan);
        
        Debug.Log("📏 Vertical guide line created");
    }
    
    void ExtendGuideLine()
    {
        if (stackNodes.Count < 2 || verticalGuideLine == null) return;
        
        Vector3 topPosition = stackNodes[stackNodes.Count - 1].position;
        Vector3 newEndPosition = topPosition + stackDirection * (objectSpacing * 2);
        
        LineRenderer lr = verticalGuideLine.GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.SetPosition(1, newEndPosition);
        }
        
        Debug.Log("📏 Guide line extended");
    }
    
    GameObject CreateLine(Vector3 start, Vector3 end, Color color)
    {
        GameObject lineObj = new GameObject("StackGuideLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.useWorldSpace = true;
        
        return lineObj;
    }
    
    void CreateStackNode(Vector3 position, int index, string value)
    {
        StackNode node = new StackNode
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
        
        stackNodes.Add(node);
        
        if (statusText != null)
            statusText.text = $"Stack: {stackNodes.Count}";
    }
    
    void ShowTopMarker()
    {
        if (stackNodes.Count == 0) return;
        
        Vector3 topPos = stackNodes[stackNodes.Count - 1].position;
        
        if (topMarkerPrefab != null && topMarker == null)
        {
            topMarker = Instantiate(topMarkerPrefab);
        }
        
        if (topMarker != null)
        {
            topMarker.transform.position = topPos + Vector3.up * 0.25f;
        }
    }
    
    void UpdateTopMarker()
    {
        if (stackNodes.Count == 0) return;
        
        Vector3 topPos = stackNodes[stackNodes.Count - 1].position;
        
        if (topMarker != null)
        {
            topMarker.transform.position = topPos + Vector3.up * 0.25f;
        }
    }
    
    void UpdateInstructions()
    {
        if (instructionText == null) return;
        
        switch (currentState)
        {
            case StackState.WaitingForSurface:
                instructionText.text = "🔍 Move phone to scan surfaces";
                break;
                
            case StackState.SurfaceDetected:
                instructionText.text = "✅ Tap to place stack area";
                break;
                
            case StackState.PlacingFirstObject:
                instructionText.text = "📚 Place first book, then TAP it";
                break;
                
            case StackState.PlacingSecondObject:
                instructionText.text = "📚 Place second book ABOVE first, then TAP it";
                break;
                
            case StackState.ShowingGuideZone:
                instructionText.text = "✅ Vertical guide created!";
                currentState = StackState.StackReady;
                break;
                
            case StackState.StackReady:
                instructionText.text = "✅ Use Push/Pop buttons";
                break;
        }
    }
    
    public void SimulatePush()
    {
        if (currentState != StackState.StackReady) return;
        
        if (stackNodes.Count >= maxObjects)
        {
            Debug.LogWarning("⚠️ Stack full!");
            if (instructionText != null)
                instructionText.text = "⚠️ Stack overflow!";
            return;
        }
        
        expectedPushPosition = stackNodes[stackNodes.Count - 1].position + stackDirection * objectSpacing;
        
        pushIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pushIndicator.name = "PushIndicator";
        pushIndicator.transform.position = expectedPushPosition + Vector3.up * 0.05f;
        pushIndicator.transform.localScale = Vector3.one * 0.03f;
        
        Renderer rend = pushIndicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0, 1, 0, 0.7f);
        rend.material = mat;
        
        Collider col = pushIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        currentState = StackState.WaitingForPushPlacement;
        
        if (instructionText != null)
            instructionText.text = "➕ Place book at GREEN spot (TOP), then TAP";
        
        Debug.Log("🟢 Push indicator placed, waiting for tap...");
    }
    
    public void SimulatePop()
    {
        if (currentState != StackState.StackReady || stackNodes.Count == 0) return;
        
        StackNode topNode = stackNodes[stackNodes.Count - 1];
        
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.transform.position = topNode.position + Vector3.up * 0.05f;
        indicator.transform.localScale = Vector3.one * 0.08f;
        
        Renderer rend = indicator.GetComponent<Renderer>();
        rend.material.color = new Color(1, 0, 0, 0.7f);
        
        if (instructionText != null)
            instructionText.text = "➖ Remove book at RED spot (TOP)";
        
        Destroy(indicator, 3f);
        
        StartCoroutine(RemoveTopNodeAfterDelay(2f));
    }
    
    System.Collections.IEnumerator RemoveTopNodeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (stackNodes.Count > 0)
        {
            StackNode removedNode = stackNodes[stackNodes.Count - 1];
            
            if (removedNode.indexLabel != null)
                Destroy(removedNode.indexLabel);
            
            stackNodes.RemoveAt(stackNodes.Count - 1);
            
            UpdateTopMarker();
            
            if (statusText != null)
                statusText.text = $"Stack: {stackNodes.Count}";
            
            if (instructionText != null)
                instructionText.text = "✅ Popped! Ready for next operation";
            
            Debug.Log("✅ Top item removed");
        }
    }
    
    public void SimulatePeek()
    {
        if (stackNodes.Count == 0) return;
        
        StackNode topNode = stackNodes[stackNodes.Count - 1];
        
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        highlight.transform.position = topNode.position + Vector3.up * 0.05f;
        highlight.transform.localScale = Vector3.one * 0.08f;
        
        Renderer rend = highlight.GetComponent<Renderer>();
        rend.material.color = new Color(1, 1, 0, 0.7f);
        
        if (instructionText != null)
            instructionText.text = $"👁️ PEEK: Top is [{topNode.index}] = {topNode.value}";
        
        Destroy(highlight, 2f);
        
        StartCoroutine(ResetPeekMessage());
    }
    
    System.Collections.IEnumerator ResetPeekMessage()
    {
        yield return new WaitForSeconds(2.5f);
        
        if (instructionText != null)
            instructionText.text = "✅ Use Push/Pop buttons";
    }
    
    public void ResetStack()
    {
        foreach (var node in stackNodes)
        {
            if (node.indexLabel != null)
                Destroy(node.indexLabel);
        }
        stackNodes.Clear();
        
        if (topMarker != null)
        {
            Destroy(topMarker);
            topMarker = null;
        }
        
        if (verticalGuideLine != null)
        {
            Destroy(verticalGuideLine);
            verticalGuideLine = null;
        }
        
        if (pushIndicator != null)
        {
            Destroy(pushIndicator);
            pushIndicator = null;
        }
        
        firstObjectPosition = Vector3.zero;
        secondObjectPosition = Vector3.zero;
        objectSpacing = 0f;
        stackDirection = Vector3.zero;
        expectedPushPosition = Vector3.zero;
        
        surfacePosition = Vector3.zero;
        surfaceRotation = Quaternion.identity;
        surfacePlaced = false;
        isSurfaceDetected = false;
        
        ShowPlanes();
        
        currentState = StackState.WaitingForSurface;
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.gameObject.SetActive(true);
            detectionFeedbackText.text = "🔍 Scan environment slowly";
            detectionFeedbackText.color = notDetectingColor;
        }
        
        if (statusText != null)
        {
            statusText.text = "Stack: 0";
        }
        
        UpdateInstructions();
        
        Debug.Log("🔄 Stack completely reset");
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
    
    public int GetStackSize()
    {
        return stackNodes.Count;
    }
    
    public bool IsStackReady()
    {
        return currentState == StackState.StackReady;
    }
}