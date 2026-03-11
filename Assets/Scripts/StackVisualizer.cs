using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class StackVisualizer : MonoBehaviour
{
    [Header("Stack Settings")]
    public GameObject stackItemPrefab;
    public float itemHeight = 0.2f;
    public float spacing = 0.02f;
    public int maxStackSize = 10;
    public float animationDuration = 0.5f;

    [Header("Surface Detection Feedback")]
    public TextMeshProUGUI detectionFeedbackText;
    public Color detectingColor = Color.green;
    public Color notDetectingColor = Color.red;
    
    [Header("AR References")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    private UniversalARPlacementOptimizer arOptimizer;
    private ARExplorationTracker explorationTracker;
    
    [Header("UI References")]
    public TextMeshProUGUI sizeText;
    public TextMeshProUGUI topValueText;

    private Stack<GameObject> stackItems = new Stack<GameObject>();
    private Vector3 spawnPosition;
    private Quaternion baseRotation;
    private bool isPlaced = false;
    private bool isSurfaceDetected = false;
    private bool hadPlanesLastFrame = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    
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
        
        if (stackItemPrefab == null)
        {
            Debug.LogError(" STACK ITEM PREFAB IS NOT ASSIGNED! Please assign it in the Inspector.");
        }
        else
        {
            Debug.Log(" Stack Item Prefab assigned: " + stackItemPrefab.name);
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
        
        UpdateUI();
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
                    
                    spawnPosition = hitPose.position;
                    spawnPosition.y += 0.01f;
                    baseRotation = hitPose.rotation;
                    
                    transform.position = spawnPosition;
                    transform.rotation = baseRotation;
                    
                    isPlaced = true;

                    if (detectionFeedbackText != null)
                    {
                        detectionFeedbackText.gameObject.SetActive(false);
                    }

                    if (arOptimizer != null)
                    {
                        arOptimizer.OnStructurePlaced();
                    }
                    
                    if (explorationTracker != null)
                    {
                        explorationTracker.RecordInteraction();
                    }
                    
                    Debug.Log("🎯 Stack placed at: " + spawnPosition);
                    
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
            // Keep planes hidden while stack is placed
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
            detectionFeedbackText.text = "Surface Detected - Tap to place";
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
    
    public bool IsStackPlaced()
    {
        return isPlaced;
    }
    
    public void Push()
    {
        if (!isPlaced)
        {
            Debug.Log("Stack not placed yet! Tap on a plane first.");
            return;
        }
        
        if (stackItems.Count >= maxStackSize)
        {
            Debug.Log("Stack is full!");
            return;
        }
        
        if (stackItemPrefab == null)
        {
            Debug.LogError(" Cannot push: stackItemPrefab is not assigned!");
            return;
        }
        
        Vector3 newPosition = spawnPosition;
        newPosition.y += stackItems.Count * (itemHeight + spacing);
        
        Debug.Log($"📍 Creating stack item at position: {newPosition}");
        
        GameObject newItem = Instantiate(stackItemPrefab, newPosition, baseRotation);
        newItem.transform.SetParent(transform);
        
        Debug.Log($" Stack item GameObject created: {newItem.name}");
        
        Renderer renderer = newItem.GetComponent<Renderer>();
        if (renderer != null)
        {
            float hue = (stackItems.Count * 0.1f) % 1f;
            renderer.material.color = Color.HSVToRGB(hue, 0.8f, 0.9f);
        }
        
        stackItems.Push(newItem);
        StartCoroutine(AnimatePush(newItem, newPosition));
        
        Debug.Log($" Pushed. Stack size: {stackItems.Count}");
        
        if (explorationTracker != null)
        {
            explorationTracker.RecordInteraction();
        }
        
        UpdateUI();
    }
    
    public void Pop()
    {
        if (stackItems.Count == 0)
        {
            Debug.Log("Stack is empty!");
            return;
        }
        
        GameObject topItem = stackItems.Pop();
        StartCoroutine(AnimatePop(topItem));
        
        Debug.Log($" Popped. Stack size: {stackItems.Count}");
        
        if (explorationTracker != null)
        {
            explorationTracker.RecordInteraction();
        }
        
        UpdateUI();
    }
    
    public void Clear()
    {
        while (stackItems.Count > 0)
        {
            Destroy(stackItems.Pop());
        }
        
        isPlaced = false;
        isSurfaceDetected = false;
        hadPlanesLastFrame = false;
        
        if (arOptimizer != null)
        {
            arOptimizer.OnStructureReset();
        }

        ShowPlanes();
        
        Debug.Log("🗑️ Stack cleared and reset!");
        
        UpdateUI();
    }
    
    public int Size()
    {
        return stackItems.Count;
    }
    
    public void SetNodeScale(float scale)
    {
        currentNodeScale = scale;
        
        // Convert Stack to array to iterate without modifying it
        GameObject[] items = stackItems.ToArray();
        foreach (GameObject item in items)
        {
            if (item != null)
            {
                item.transform.localScale = Vector3.one * scale;
            }
        }
        
        Debug.Log($"🔍 Stack node scale set to: {scale}");
    }
    
    public float GetNodeScale()
    {
        return currentNodeScale;
    }
    
    void UpdateUI()
    {
        if (sizeText != null)
        {
            sizeText.text = $"Size: {stackItems.Count}";
        }
        
        if (topValueText != null)
        {
            if (stackItems.Count > 0)
            {
                topValueText.text = $"Top: Item {stackItems.Count}";
            }
            else
            {
                topValueText.text = "Top: Empty";
            }
        }
    }
    
    void HidePlanes()
    {
        if (planeManager != null)
        {
            planeManager.enabled = false;
            
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
            planeManager.enabled = true;
            
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
            Debug.Log("👁️ AR Planes visible and enabled again");
        }
    }
    
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
    
    System.Collections.IEnumerator AnimatePush(GameObject item, Vector3 targetPos)
    {
        Vector3 startPos = targetPos + Vector3.up * 0.5f;
        item.transform.position = startPos;
        item.transform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - elapsed / animationDuration, 3f);
            
            item.transform.position = Vector3.Lerp(startPos, targetPos, t);
            item.transform.localScale = Vector3.one * currentNodeScale * t;
            yield return null;
        }
        
        item.transform.position = targetPos;
        item.transform.localScale = Vector3.one * currentNodeScale;
    }
    
    System.Collections.IEnumerator AnimatePop(GameObject item)
    {
        Vector3 startPos = item.transform.position;
        Vector3 endPos = startPos + Vector3.up * 0.5f;
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            item.transform.position = Vector3.Lerp(startPos, endPos, t);
            item.transform.localScale = Vector3.Lerp(Vector3.one * currentNodeScale, Vector3.zero, t);
            yield return null;
        }
        
        Destroy(item);
    }
}