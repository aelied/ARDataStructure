using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ARObjectPlacer : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    
    [Header("Object to Place")]
    [SerializeField] private GameObject objectToPlace;
    
    [Header("Surface Detection Feedback")]
    [SerializeField] private TextMeshProUGUI detectionFeedbackText;
    [SerializeField] private Color detectingColor = Color.green;
    [SerializeField] private Color notDetectingColor = Color.red;
    
    [Header("Optional References")]
    private UniversalARPlacementOptimizer arOptimizer;
    private ARExplorationTracker explorationTracker;
    
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject spawnedObject;
    private bool isPlaced = false;
    private bool isSurfaceDetected = false;

    void Start()
    {
        // Auto-find AR managers if not assigned
        if (raycastManager == null)
        {
            raycastManager = FindObjectOfType<ARRaycastManager>();
            Debug.Log("ARRaycastManager: " + (raycastManager != null ? "Found" : "NOT FOUND"));
        }
        
        if (planeManager == null)
        {
            planeManager = FindObjectOfType<ARPlaneManager>();
            Debug.Log("ARPlaneManager: " + (planeManager != null ? "Found" : "NOT FOUND"));
        }
        
        // Find optional components
        arOptimizer = FindObjectOfType<UniversalARPlacementOptimizer>();
        explorationTracker = FindObjectOfType<ARExplorationTracker>();
        
        Debug.Log("AROptimizer: " + (arOptimizer != null ? "Found" : "NOT FOUND"));
        Debug.Log("ExplorationTracker: " + (explorationTracker != null ? "Found" : "NOT FOUND"));
        
        // Initialize feedback text
        if (detectionFeedbackText != null)
        {
            UpdateDetectionFeedback(false);
        }
    }

    void Update()
    {
        // Continuously check for surface detection at screen center
        CheckSurfaceDetection();
        
        // Check if user touched the screen
        if (Input.touchCount == 0 && !Input.GetMouseButtonDown(0))
            return;

        Vector2 touchPosition;
        
        // Support both touch and mouse input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            // Only place on first touch
            if (touch.phase != TouchPhase.Began)
                return;
                
            touchPosition = touch.position;
        }
        else
        {
            touchPosition = Input.mousePosition;
        }

        // Perform raycast from touch position
        if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            // Get the hit pose (position and rotation)
            Pose hitPose = hits[0].pose;

            // If object hasn't been spawned yet, create it
            if (spawnedObject == null)
            {
                spawnedObject = Instantiate(objectToPlace, hitPose.position, hitPose.rotation);
                isPlaced = true;
                
                // ⭐ Notify optimizer that structure was placed
                if (arOptimizer != null)
                {
                    arOptimizer.OnStructurePlaced();
                }
                
                // ⭐ Record interaction for progress tracking
                if (explorationTracker != null)
                {
                    explorationTracker.RecordInteraction();
                }
                
                HidePlanes();
                Debug.Log("✅ Object placed! Planes hidden.");
            }
            else
            {
                // Move existing object to new position
                spawnedObject.transform.position = hitPose.position;
                spawnedObject.transform.rotation = hitPose.rotation;
                
                // ⭐ Record interaction when moving object
                if (explorationTracker != null)
                {
                    explorationTracker.RecordInteraction();
                }
                
                Debug.Log("Object moved!");
            }
        }
    }

    /// <summary>
    /// Continuously check if surfaces are being detected
    /// </summary>
    private void CheckSurfaceDetection()
    {
        if (raycastManager == null || isPlaced)
        {
            // Hide feedback when object is already placed
            if (detectionFeedbackText != null && isPlaced)
            {
                detectionFeedbackText.gameObject.SetActive(false);
            }
            return;
        }
        
        // Check from screen center
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        
        bool detected = raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);
        
        // Only update UI if detection state changed
        if (detected != isSurfaceDetected)
        {
            isSurfaceDetected = detected;
            UpdateDetectionFeedback(detected);
        }
    }

    /// <summary>
    /// Update the visual feedback for surface detection
    /// </summary>
    private void UpdateDetectionFeedback(bool detecting)
    {
        if (detectionFeedbackText == null) return;
        
        detectionFeedbackText.gameObject.SetActive(true);
        
        if (detecting)
        {
            detectionFeedbackText.text = "Surface Detected\nTap to place";
            detectionFeedbackText.color = detectingColor;
        }
        else
        {
            detectionFeedbackText.text = "Searching for surface...\nMove your device";
            detectionFeedbackText.color = notDetectingColor;
        }
    }

    /// <summary>
    /// Call this method when the Clear button is clicked
    /// </summary>
    public void Clear()
    {
        if (spawnedObject != null)
        {
            Destroy(spawnedObject);
            spawnedObject = null;
            Debug.Log("🗑️ Object destroyed");
        }
        
        isPlaced = false;
        
        // ⭐ Notify optimizer that structure was reset
        if (arOptimizer != null)
        {
            arOptimizer.OnStructureReset();
        }
        
        ShowPlanes();
        
        // Re-enable detection feedback
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.gameObject.SetActive(true);
            UpdateDetectionFeedback(isSurfaceDetected);
        }
        
        Debug.Log("👁️ Planes visible again - ready to place new object");
    }

    /// <summary>
    /// Hide all detected AR planes
    /// </summary>
    private void HidePlanes()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
            Debug.Log("👻 AR Planes hidden");
        }
    }

    /// <summary>
    /// Show all detected AR planes
    /// </summary>
    private void ShowPlanes()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
            Debug.Log("👁️ AR Planes visible");
        }
    }

    /// <summary>
    /// Manual toggle for plane visualization (optional)
    /// </summary>
    public void TogglePlaneVisualization(bool show)
    {
        if (show)
            ShowPlanes();
        else
            HidePlanes();
    }

    /// <summary>
    /// Check if an object has been placed
    /// </summary>
    public bool IsObjectPlaced()
    {
        return isPlaced;
    }
    
    /// <summary>
    /// ⭐ NEW: Clean up when leaving scene
    /// </summary>
    void OnDestroy()
    {
        if (spawnedObject != null)
        {
            Destroy(spawnedObject);
        }
        
        Debug.Log("🗑️ ARObjectPlacer cleanup completed");
    }
}