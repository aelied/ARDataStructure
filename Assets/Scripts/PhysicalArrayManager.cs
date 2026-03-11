using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class PhysicalArrayManager : MonoBehaviour
{
    [Header("Array Settings")]
    public int maxCapacity = 8;
    public float detectionRadius = 0.05f;
    
    [Header("Virtual Label Prefabs")]
    public GameObject indexLabelPrefab;
    
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
    
    private enum ArrayState
    {
        WaitingForSurface,
        SurfaceDetected,
        PlacingFirstObject,
        PlacingSecondObject,
        ShowingGuideZone,
        ArrayReady,
        WaitingForShiftConfirmation,
        WaitingForInsertPlacement,
        WaitingForDeleteSelection,
        WaitingForDeleteShiftConfirmation,
        WaitingForAccessSelection
    }
    
    private ArrayState currentState = ArrayState.WaitingForSurface;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private Vector3 surfacePosition;
    private Quaternion surfaceRotation;
    private bool surfacePlaced = false;
    private bool isSurfaceDetected = false;
    
    private class ArrayElement
    {
        public Vector3 position;
        public GameObject indexLabel;
        public int index;
        public string value;
    }
    
    private List<ArrayElement> arrayElements = new List<ArrayElement>();
    private GameObject leftBoundary;
    private GameObject rightBoundary;
    
    private Vector3 firstObjectPosition;
    private Vector3 secondObjectPosition;
    private float elementSpacing;
    private Vector3 arrayDirection;
    
    private int insertAtIndex = -1;
    private int deleteAtIndex = -1;
    private GameObject insertIndicator;
    private List<GameObject> shiftIndicators = new List<GameObject>();
    private Vector3 expectedInsertPosition;
    
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
        
        Debug.Log("📦 Physical Array Manager Initialized");
    }
    
    void Update()
    {
        switch (currentState)
        {
            case ArrayState.WaitingForSurface:
                CheckForSurfaceDetection();
                break;
                
            case ArrayState.SurfaceDetected:
                CheckForSurfacePlacement();
                break;
                
            case ArrayState.PlacingFirstObject:
                DetectFirstObject();
                break;
                
            case ArrayState.PlacingSecondObject:
                DetectSecondObject();
                break;
                
            case ArrayState.WaitingForInsertPlacement:
                DetectInsertObject();
                break;
                
            case ArrayState.WaitingForDeleteSelection:
                DetectElementSelection();
                break;
                
            case ArrayState.WaitingForAccessSelection:
                DetectElementSelection();
                break;
                
            case ArrayState.WaitingForShiftConfirmation:
            case ArrayState.WaitingForDeleteShiftConfirmation:
            case ArrayState.ShowingGuideZone:
            case ArrayState.ArrayReady:
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
            currentState = ArrayState.SurfaceDetected;
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
            
            currentState = ArrayState.PlacingFirstObject;
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
            CreateArrayElement(firstObjectPosition, 0, "Item0");
            
            currentState = ArrayState.PlacingSecondObject;
            UpdateInstructions();
            
            Debug.Log("📦 First array element at: " + firstObjectPosition);
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
            
            arrayDirection = (secondObjectPosition - firstObjectPosition).normalized;
            elementSpacing = Vector3.Distance(firstObjectPosition, secondObjectPosition);
            
            CreateArrayElement(secondObjectPosition, 1, "Item1");
            CreateArrayBoundaries();
            
            currentState = ArrayState.ShowingGuideZone;
            UpdateInstructions();
            
            Debug.Log($"📦 Array initialized. Spacing: {elementSpacing:F3}m");
        }
    }
    
    void DetectInsertObject()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TapInsertObject(Input.GetTouch(0).position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            TapInsertObject(Input.mousePosition);
        }
    }
    
    void TapInsertObject(Vector2 touchPosition)
    {
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Vector3 tappedPosition = hits[0].pose.position;
            float distance = Vector3.Distance(tappedPosition, expectedInsertPosition);
            
            if (distance < elementSpacing * 0.7f)
            {
                // Create new element with the correct value
                string newValue = $"Item{arrayElements.Count}";
                
                // Create a temporary element
                ArrayElement newElement = new ArrayElement
                {
                    position = tappedPosition,
                    index = insertAtIndex,
                    value = newValue
                };
                
                // Create the label for the new element
                if (indexLabelPrefab != null)
                {
                    newElement.indexLabel = Instantiate(indexLabelPrefab);
                    newElement.indexLabel.transform.position = tappedPosition + Vector3.up * 0.15f;
                    
                    // Make label face forward based on array direction
                    Vector3 labelForward = Vector3.Cross(arrayDirection, Vector3.up);
                    if (labelForward != Vector3.zero)
                    {
                        newElement.indexLabel.transform.rotation = Quaternion.LookRotation(labelForward);
                    }
                    
                    TextMeshPro labelText = newElement.indexLabel.GetComponentInChildren<TextMeshPro>();
                    if (labelText != null)
                    {
                        labelText.text = $"[{insertAtIndex}]";
                    }
                }
                
                // Insert the new element at the correct position
                arrayElements.Insert(insertAtIndex, newElement);
                
                if (insertIndicator != null)
                {
                    Destroy(insertIndicator);
                    insertIndicator = null;
                }
                
                UpdateAllIndices();
                ExpandBoundaries();
                
                if (statusText != null)
                    statusText.text = $"Elements: {arrayElements.Count}";
                
                currentState = ArrayState.ArrayReady;
                if (instructionText != null)
                    instructionText.text = $"✅ Inserted at [{insertAtIndex}]! Array size: {arrayElements.Count}";
                
                Debug.Log($"✅ Element inserted at index {insertAtIndex}, total elements: {arrayElements.Count}");
                
                insertAtIndex = -1;
            }
            else
            {
                Debug.LogWarning($"⚠️ Tap too far from expected position. Distance: {distance:F3}m");
                if (instructionText != null)
                    instructionText.text = "⚠️ Too far! Tap near the GREEN spot";
            }
        }
    }
    
    void DetectElementSelection()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            SelectElement(Input.GetTouch(0).position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            SelectElement(Input.mousePosition);
        }
    }
    
    void SelectElement(Vector2 touchPosition)
    {
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Vector3 tappedPosition = hits[0].pose.position;
            int closestIndex = FindClosestElementIndex(tappedPosition);
            
            Debug.Log($"🎯 Tap at {tappedPosition}, closest index: {closestIndex}");
            
            if (closestIndex >= 0)
            {
                if (currentState == ArrayState.WaitingForDeleteSelection)
                {
                    Debug.Log($"🗑️ Deleting element at [{closestIndex}]");
                    DeleteElementAt(closestIndex);
                }
                else if (currentState == ArrayState.WaitingForAccessSelection)
                {
                    Debug.Log($"👁️ Accessing element at [{closestIndex}]");
                    AccessElementAt(closestIndex);
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No element close enough to tap position");
                if (instructionText != null)
                {
                    if (currentState == ArrayState.WaitingForDeleteSelection)
                        instructionText.text = "⚠️ Tap closer to a coin! Try again";
                    else if (currentState == ArrayState.WaitingForAccessSelection)
                        instructionText.text = "⚠️ Tap closer to a coin! Try again";
                }
            }
        }
    }
    
    int FindClosestElementIndex(Vector3 position)
    {
        if (arrayElements.Count == 0) return -1;
        
        float minDist = float.MaxValue;
        int closestIndex = -1;
        
        for (int i = 0; i < arrayElements.Count; i++)
        {
            float dist = Vector3.Distance(position, arrayElements[i].position);
            if (dist < minDist)
            {
                minDist = dist;
                closestIndex = i;
            }
        }
        
        // Increased detection radius for easier selection
        float detectionThreshold = elementSpacing * 0.8f;
        bool isCloseEnough = minDist < detectionThreshold;
        
        Debug.Log($"📍 Closest element: [{closestIndex}], distance: {minDist:F3}m, threshold: {detectionThreshold:F3}m, valid: {isCloseEnough}");
        
        return isCloseEnough ? closestIndex : -1;
    }
    
    Vector3 CalculatePositionAtIndex(int index)
    {
        return firstObjectPosition + arrayDirection * (elementSpacing * index);
    }
    
    void DeleteElementAt(int index)
    {
        if (index < 0 || index >= arrayElements.Count) return;
        
        deleteAtIndex = index;
        ArrayElement elementToDelete = arrayElements[index];
        
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        highlight.name = "DeleteHighlight";
        highlight.transform.position = elementToDelete.position + Vector3.up * 0.05f;
        highlight.transform.localScale = Vector3.one * 0.06f;
        
        Renderer rend = highlight.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(1, 0, 0, 0.8f);
        rend.material = mat;
        
        // Disable shadows for performance
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        
        Collider col = highlight.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        shiftIndicators.Add(highlight);
        
        if (index < arrayElements.Count - 1)
        {
            ShowDeleteShiftIndicators(index);
            
            currentState = ArrayState.WaitingForDeleteShiftConfirmation;
            
            if (instructionText != null)
            {
                int numToMove = arrayElements.Count - index - 1;
                instructionText.text = $"1️⃣ Remove RED coin [{index}]\n2️⃣ Move {numToMove} coin(s) LEFT to YELLOW spots\n3️⃣ Press CONFIRM";
            }
            
            Debug.Log($"⚠️ Delete at [{index}], {arrayElements.Count - index - 1} coins need shifting left");
        }
        else
        {
            if (instructionText != null)
                instructionText.text = $"➖ Remove RED coin at [{index}] (last element)";
            
            StartCoroutine(DirectDeleteAfterDelay(1.5f));
        }
    }
    
    void ShowDeleteShiftIndicators(int deletedIndex)
    {
        for (int i = deletedIndex + 1; i < arrayElements.Count; i++)
        {
            Vector3 currentPos = arrayElements[i].position;
            Vector3 newPos = CalculatePositionAtIndex(i - 1);
            
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = $"ShiftTarget_{i}";
            indicator.transform.position = newPos + Vector3.up * 0.02f;
            indicator.transform.localScale = Vector3.one * 0.03f;
            
            Renderer rend = indicator.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(1, 1, 0, 0.9f);
            rend.material = mat;
            
            // Disable shadows for better performance
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            
            Collider col = indicator.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            shiftIndicators.Add(indicator);
            
            CreateImprovedArrow(currentPos, newPos, Color.yellow, i);
        }
    }
    
    System.Collections.IEnumerator DirectDeleteAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (deleteAtIndex >= 0 && deleteAtIndex < arrayElements.Count)
        {
            ArrayElement element = arrayElements[deleteAtIndex];
            
            if (element.indexLabel != null)
                Destroy(element.indexLabel);
            
            arrayElements.RemoveAt(deleteAtIndex);
            
            ClearShiftIndicators();
            UpdateAllIndices();
            UpdateBoundaries();
            
            currentState = ArrayState.ArrayReady;
            if (instructionText != null)
                instructionText.text = "✅ Deleted! Array size: " + arrayElements.Count;
            
            if (statusText != null)
                statusText.text = $"Elements: {arrayElements.Count}";
            
            deleteAtIndex = -1;
        }
    }
    
    void AccessElementAt(int index)
    {
        if (index < 0 || index >= arrayElements.Count) return;
        
        ArrayElement element = arrayElements[index];
        StartCoroutine(HighlightElement(element, index));
    }
    
    System.Collections.IEnumerator HighlightElement(ArrayElement element, int index)
    {
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        highlight.transform.position = element.position + Vector3.up * 0.05f;
        highlight.transform.localScale = Vector3.one * 0.08f;
        
        Renderer rend = highlight.GetComponent<Renderer>();
        rend.material.color = new Color(0, 1, 0, 0.7f);
        
        if (instructionText != null)
            instructionText.text = $"✅ Accessed: [{index}] = {element.value}";
        
        yield return new WaitForSeconds(2f);
        
        Destroy(highlight);
        
        currentState = ArrayState.ArrayReady;
        if (instructionText != null)
            instructionText.text = "✅ Use buttons for operations";
    }
    
    void CreateArrayElement(Vector3 position, int index, string value)
    {
        ArrayElement element = new ArrayElement
        {
            position = position,
            index = index,
            value = value
        };
        
        if (indexLabelPrefab != null)
        {
            element.indexLabel = Instantiate(indexLabelPrefab);
            element.indexLabel.transform.position = position + Vector3.up * 0.15f;
            
            // Make label face forward based on array direction
            Vector3 labelForward = Vector3.Cross(arrayDirection, Vector3.up);
            if (labelForward != Vector3.zero)
            {
                element.indexLabel.transform.rotation = Quaternion.LookRotation(labelForward);
            }
            
            TextMeshPro labelText = element.indexLabel.GetComponentInChildren<TextMeshPro>();
            if (labelText != null)
            {
                labelText.text = $"[{index}]";
            }
        }
        
        arrayElements.Add(element);
        
        if (statusText != null)
            statusText.text = $"Elements: {arrayElements.Count}";
    }
    
    void CreateArrayBoundaries()
    {
        Vector3 leftPos = firstObjectPosition - arrayDirection * (elementSpacing * 0.5f);
        Vector3 rightPos = secondObjectPosition + arrayDirection * (elementSpacing * 4.5f);
        
        leftBoundary = CreateLine(leftPos + Vector3.up * 0.02f, leftPos + Vector3.up * 0.3f, Color.yellow);
        rightBoundary = CreateLine(rightPos + Vector3.up * 0.02f, rightPos + Vector3.up * 0.3f, Color.yellow);
        
        Debug.Log("📏 Array boundaries created");
    }
    
    void ExpandBoundaries()
    {
        if (arrayElements.Count < 2) return;
        
        Vector3 rightPos = CalculatePositionAtIndex(arrayElements.Count - 1) + arrayDirection * (elementSpacing * 1.5f);
        
        if (rightBoundary != null)
        {
            LineRenderer lr = rightBoundary.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.SetPosition(0, rightPos + Vector3.up * 0.02f);
                lr.SetPosition(1, rightPos + Vector3.up * 0.3f);
            }
        }
    }
    
    void UpdateBoundaries()
    {
        if (arrayElements.Count == 0) return;
        
        Vector3 rightPos = CalculatePositionAtIndex(arrayElements.Count - 1) + arrayDirection * (elementSpacing * 1.5f);
        
        if (rightBoundary != null)
        {
            LineRenderer lr = rightBoundary.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.SetPosition(0, rightPos + Vector3.up * 0.02f);
                lr.SetPosition(1, rightPos + Vector3.up * 0.3f);
            }
        }
    }
    
    GameObject CreateLine(Vector3 start, Vector3 end, Color color)
    {
        GameObject lineObj = new GameObject("BoundaryLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        
        // Use simpler shader for performance
        lr.material = new Material(Shader.Find("Unlit/Color"));
        lr.material.color = color;
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.015f;
        lr.endWidth = 0.015f;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.useWorldSpace = true;
        
        // Disable shadows for performance
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        
        return lineObj;
    }
    
    void UpdateAllIndices()
    {
        for (int i = 0; i < arrayElements.Count; i++)
        {
            arrayElements[i].index = i;
            
            // Only update rotation if it hasn't been set yet or if arrayDirection exists
            if (arrayElements[i].indexLabel != null && arrayDirection != Vector3.zero)
            {
                Vector3 labelForward = Vector3.Cross(arrayDirection, Vector3.up);
                if (labelForward != Vector3.zero)
                {
                    arrayElements[i].indexLabel.transform.rotation = Quaternion.LookRotation(labelForward);
                }
                
                TextMeshPro labelText = arrayElements[i].indexLabel.GetComponentInChildren<TextMeshPro>();
                if (labelText != null)
                {
                    labelText.text = $"[{i}]";
                }
            }
        }
    }
    
    void UpdateInstructions()
    {
        if (instructionText == null) return;
        
        switch (currentState)
        {
            case ArrayState.WaitingForSurface:
                instructionText.text = "🔍 Move phone to scan surfaces";
                break;
                
            case ArrayState.SurfaceDetected:
                instructionText.text = "✅ Tap to place array area";
                break;
                
            case ArrayState.PlacingFirstObject:
                instructionText.text = "📦 Place first item, then TAP it";
                break;
                
            case ArrayState.PlacingSecondObject:
                instructionText.text = "📦 Place second item to the RIGHT, then TAP it";
                break;
                
            case ArrayState.ShowingGuideZone:
                instructionText.text = "✅ Array initialized!";
                currentState = ArrayState.ArrayReady;
                break;
                
            case ArrayState.ArrayReady:
                instructionText.text = "✅ Use buttons for operations";
                break;
        }
    }
    
    public void SimulateInsert()
    {
        SimulateInsertAtIndex(-1);
    }
    
    public void SimulateInsertAtIndex(int targetIndex)
    {
        if (currentState != ArrayState.ArrayReady) return;
        
        if (arrayElements.Count >= maxCapacity)
        {
            Debug.LogWarning("⚠️ Array full!");
            if (instructionText != null)
                instructionText.text = "⚠️ Array is full!";
            return;
        }
        
        if (targetIndex == -1)
        {
            targetIndex = arrayElements.Count;
            Debug.Log($"📦 No index specified, inserting at end: {targetIndex}");
        }
        
        if (targetIndex < 0 || targetIndex > arrayElements.Count)
        {
            Debug.LogWarning($"⚠️ Invalid index: {targetIndex}");
            if (instructionText != null)
                instructionText.text = $"⚠️ Invalid index! Use 0-{arrayElements.Count}";
            return;
        }
        
        insertAtIndex = targetIndex;
        
        if (targetIndex == arrayElements.Count)
        {
            expectedInsertPosition = CalculatePositionAtIndex(targetIndex);
            CreateInsertIndicator(expectedInsertPosition);
            
            currentState = ArrayState.WaitingForInsertPlacement;
            
            if (instructionText != null)
                instructionText.text = $"➕ Place coin at GREEN spot [{targetIndex}], then TAP";
            
            Debug.Log($"🟢 Insert at end [{targetIndex}], no shift needed");
        }
        else
        {
            ShowInsertShiftIndicators(targetIndex);
            
            currentState = ArrayState.WaitingForShiftConfirmation;
            
            if (instructionText != null)
            {
                int numToMove = arrayElements.Count - targetIndex;
                instructionText.text = $"1️⃣ Move {numToMove} coin(s) RIGHT to YELLOW spots\n2️⃣ Press CONFIRM button";
            }
            
            Debug.Log($"⚠️ Middle insertion at [{targetIndex}], {arrayElements.Count - targetIndex} coins need shifting right");
        }
    }
    
    void ShowInsertShiftIndicators(int insertIndex)
    {
        ClearShiftIndicators();
        
        for (int i = insertIndex; i < arrayElements.Count; i++)
        {
            Vector3 currentPos = arrayElements[i].position;
            Vector3 newPos = CalculatePositionAtIndex(i + 1);
            
            // Create smaller, simpler indicator
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = $"ShiftTarget_{i}";
            indicator.transform.position = newPos + Vector3.up * 0.02f;
            indicator.transform.localScale = Vector3.one * 0.03f;
            
            Renderer rend = indicator.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(1, 1, 0, 0.9f);
            rend.material = mat;
            
            // Disable shadows for better performance
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            
            Collider col = indicator.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            shiftIndicators.Add(indicator);
            
            CreateImprovedArrow(currentPos, newPos, Color.yellow, i);
        }
    }
    
    void CreateImprovedArrow(Vector3 from, Vector3 to, Color color, int index)
    {
        GameObject arrow = new GameObject($"ShiftArrow_{index}");
        LineRenderer lr = arrow.AddComponent<LineRenderer>();
        
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.008f;
        lr.endWidth = 0.008f;
        
        Vector3 start = from + Vector3.up * 0.08f;
        Vector3 end = to + Vector3.up * 0.08f;
        
        Vector3 direction = (end - start).normalized;
        
        Vector3 arrowBase = end - direction * 0.015f;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        Vector3 left = arrowBase - perpendicular * 0.012f;
        Vector3 right = arrowBase + perpendicular * 0.012f;
        
        lr.positionCount = 5;
        lr.SetPosition(0, start);
        lr.SetPosition(1, arrowBase);
        lr.SetPosition(2, left);
        lr.SetPosition(3, end);
        lr.SetPosition(4, right);
        lr.useWorldSpace = true;
        
        shiftIndicators.Add(arrow);
        
        Debug.Log($"➡️ Compact arrow created for element [{index}]");
    }
    
    public void ConfirmShift()
    {
        Debug.Log($"🔵 ConfirmShift called. Current state: {currentState}");
        
        if (currentState == ArrayState.WaitingForShiftConfirmation)
        {
            Debug.Log("✅ Processing INSERT shift confirmation");
            
            ClearShiftIndicators();
            
            for (int i = arrayElements.Count - 1; i >= insertAtIndex; i--)
            {
                Vector3 newPos = CalculatePositionAtIndex(i + 1);
                arrayElements[i].position = newPos;
                
                if (arrayElements[i].indexLabel != null)
                {
                    arrayElements[i].indexLabel.transform.position = newPos + Vector3.up * 0.15f;
                }
                
                Debug.Log($"  Shifted [{i}] to position of [{i + 1}]");
            }
            
            expectedInsertPosition = CalculatePositionAtIndex(insertAtIndex);
            CreateInsertIndicator(expectedInsertPosition);
            
            currentState = ArrayState.WaitingForInsertPlacement;
            
            if (instructionText != null)
                instructionText.text = $"➕ Now place NEW coin at GREEN spot [{insertAtIndex}], then TAP it";
            
            Debug.Log($"✅ Ready for insert at [{insertAtIndex}]. State changed to WaitingForInsertPlacement");
        }
        else if (currentState == ArrayState.WaitingForDeleteShiftConfirmation)
        {
            Debug.Log("✅ Processing DELETE shift confirmation");
            
            if (deleteAtIndex >= 0 && deleteAtIndex < arrayElements.Count)
            {
                ArrayElement element = arrayElements[deleteAtIndex];
                
                if (element.indexLabel != null)
                    Destroy(element.indexLabel);
                
                arrayElements.RemoveAt(deleteAtIndex);
                Debug.Log($"  Removed element at [{deleteAtIndex}]");
                
                for (int i = deleteAtIndex; i < arrayElements.Count; i++)
                {
                    Vector3 newPos = CalculatePositionAtIndex(i);
                    arrayElements[i].position = newPos;
                    
                    if (arrayElements[i].indexLabel != null)
                    {
                        arrayElements[i].indexLabel.transform.position = newPos + Vector3.up * 0.15f;
                    }
                    
                    Debug.Log($"  Shifted [{i + 1}] to position of [{i}]");
                }
                
                ClearShiftIndicators();
                UpdateAllIndices();
                UpdateBoundaries();
                
                currentState = ArrayState.ArrayReady;
                if (instructionText != null)
                    instructionText.text = "✅ Deleted! Array size: " + arrayElements.Count;
                
                if (statusText != null)
                    statusText.text = $"Elements: {arrayElements.Count}";
                
                deleteAtIndex = -1;
                Debug.Log("✅ Delete completed. State changed to ArrayReady");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ ConfirmShift called but state is {currentState}");
        }
    }
    
    void CreateInsertIndicator(Vector3 position)
    {
        if (insertIndicator != null)
        {
            Destroy(insertIndicator);
        }
        
        insertIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        insertIndicator.name = "InsertIndicator";
        insertIndicator.transform.position = position + Vector3.up * 0.03f;
        insertIndicator.transform.localScale = Vector3.one * 0.04f;
        
        Renderer rend = insertIndicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0, 1, 0, 0.95f);
        rend.material = mat;
        
        // Disable shadows for better performance
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        
        Collider col = insertIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        Debug.Log($"🟢 Insert indicator created at [{insertAtIndex}]");
    }
    
    void ClearShiftIndicators()
    {
        foreach (GameObject indicator in shiftIndicators)
        {
            if (indicator != null)
                Destroy(indicator);
        }
        shiftIndicators.Clear();
        Debug.Log("🧹 Shift indicators cleared");
    }
    
    public void SimulateDelete()
    {
        if (currentState != ArrayState.ArrayReady || arrayElements.Count == 0) return;
        
        currentState = ArrayState.WaitingForDeleteSelection;
        
        if (instructionText != null)
            instructionText.text = "➖ TAP the coin you want to delete";
        
        Debug.Log("🔴 Waiting for delete selection...");
    }
    
    public void SimulateAccess()
    {
        if (currentState != ArrayState.ArrayReady || arrayElements.Count == 0) return;
        
        currentState = ArrayState.WaitingForAccessSelection;
        
        if (instructionText != null)
            instructionText.text = "👁️ TAP any coin to access it";
        
        Debug.Log("🟢 Waiting for access selection...");
    }
    
    public void SimulateSearch()
    {
        if (arrayElements.Count == 0) return;
        
        StartCoroutine(LinearSearchAnimation());
    }
    
    System.Collections.IEnumerator LinearSearchAnimation()
    {
        if (instructionText != null)
            instructionText.text = "🔍 Searching array...";
        
        for (int i = 0; i < arrayElements.Count; i++)
        {
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            highlight.transform.position = arrayElements[i].position + Vector3.up * 0.05f;
            highlight.transform.localScale = Vector3.one * 0.06f;
            
            Renderer rend = highlight.GetComponent<Renderer>();
            rend.material.color = new Color(1, 1, 0, 0.7f);
            
            yield return new WaitForSeconds(0.5f);
            
            Destroy(highlight);
        }
        
        if (instructionText != null)
            instructionText.text = "✅ Search complete!";
        
        yield return new WaitForSeconds(1f);
        
        if (instructionText != null)
            instructionText.text = "✅ Use buttons for operations";
    }
    
    public void ResetArray()
    {
        foreach (var element in arrayElements)
        {
            if (element.indexLabel != null)
                Destroy(element.indexLabel);
        }
        arrayElements.Clear();
        
        if (leftBoundary != null)
        {
            Destroy(leftBoundary);
            leftBoundary = null;
        }
        
        if (rightBoundary != null)
        {
            Destroy(rightBoundary);
            rightBoundary = null;
        }
        
        if (insertIndicator != null)
        {
            Destroy(insertIndicator);
            insertIndicator = null;
        }
        
        ClearShiftIndicators();
        
        firstObjectPosition = Vector3.zero;
        secondObjectPosition = Vector3.zero;
        elementSpacing = 0f;
        arrayDirection = Vector3.zero;
        insertAtIndex = -1;
        deleteAtIndex = -1;
        expectedInsertPosition = Vector3.zero;
        
        surfacePosition = Vector3.zero;
        surfaceRotation = Quaternion.identity;
        surfacePlaced = false;
        isSurfaceDetected = false;
        
        ShowPlanes();
        
        currentState = ArrayState.WaitingForSurface;
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.gameObject.SetActive(true);
            detectionFeedbackText.text = "🔍 Scan environment slowly";
            detectionFeedbackText.color = notDetectingColor;
        }
        
        if (statusText != null)
        {
            statusText.text = "Elements: 0";
        }
        
        UpdateInstructions();
        
        Debug.Log("🔄 Array completely reset");
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
    
    public int GetArraySize()
    {
        return arrayElements.Count;
    }
    
    public bool IsArrayReady()
    {
        return currentState == ArrayState.ArrayReady;
    }
    
    public bool IsWaitingForShiftConfirmation()
    {
        bool isWaiting = currentState == ArrayState.WaitingForShiftConfirmation || 
                        currentState == ArrayState.WaitingForDeleteShiftConfirmation;
        Debug.Log($"IsWaitingForShiftConfirmation: {isWaiting}, current state: {currentState}");
        return isWaiting;
    }
}