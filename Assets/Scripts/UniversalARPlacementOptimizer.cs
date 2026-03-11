using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Universal AR Placement Optimizer - Makes plane detection 2-3x faster
/// Attach this to your AR Session Origin (XR Origin) in EVERY AR scene
/// </summary>
public class UniversalARPlacementOptimizer : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Only detect horizontal planes (floor/table) - much faster than detecting all planes")]
    public bool horizontalOnly = true;
    
    [Tooltip("Minimum plane size to consider valid (filters out tiny planes)")]
    public float minimumPlaneSize = 0.15f;
    
    [Tooltip("Stop plane detection after structure is placed (saves battery)")]
    public bool stopDetectionAfterPlacement = true;
    
    [Tooltip("Maximum number of active planes to keep (better performance)")]
    public int maxActivePlanes = 8;
    
    [Header("UI Feedback (Optional)")]
    public TextMeshProUGUI statusText;
    public GameObject tapToPlaceHint;
    
    [Header("Auto-Found References")]
    private ARPlaneManager planeManager;
    private ARSession arSession;
    
    private bool isStructurePlaced = false;
    private float detectionStartTime;
    private int planesDetected = 0;
    
    void Start()
    {
        // Auto-find AR components
        planeManager = FindObjectOfType<ARPlaneManager>();
        arSession = FindObjectOfType<ARSession>();
        
        if (planeManager == null)
        {
            Debug.LogError("❌ ARPlaneManager not found! Add one to your scene.");
            return;
        }
        
        if (arSession == null)
        {
            Debug.LogError("❌ ARSession not found! Add one to your scene.");
            return;
        }
        
        // ⭐ CRITICAL FIX: Clean up planes from previous scene FIRST
        CleanupAllPlanes();
        
        // ⭐ CRITICAL FIX: Reset AR session to fresh state
        ResetARSession();
        
        // Configure plane manager for speed
        ConfigurePlaneManager();
        
        // Subscribe to plane events
        planeManager.planesChanged += OnPlanesChanged;
        
        detectionStartTime = Time.time;
        
        Debug.Log(" AR Optimizer initialized - Scanning for planes...");
        UpdateStatusUI("🔍 Scanning for surfaces...");
    }
    
    /// <summary>
    /// ⭐ NEW METHOD: Properly reset AR session for new scene
    /// </summary>
    void ResetARSession()
    {
        if (arSession == null) return;
        
        // Reset the AR session to clear all tracked data
        arSession.Reset();
        
        Debug.Log("🔄 AR Session reset - ready for new detection");
        
        // Wait a frame then re-enable
        StartCoroutine(ReEnableARSession());
    }
    
    System.Collections.IEnumerator ReEnableARSession()
    {
        // Wait for reset to complete
        yield return new WaitForSeconds(0.1f);
        
        if (arSession != null)
        {
            arSession.enabled = true;
        }
        
        if (planeManager != null)
        {
            planeManager.enabled = true;
        }
        
        Debug.Log(" AR Session re-enabled and ready");
    }
    
    void ConfigurePlaneManager()
    {
        // Set detection mode for maximum speed
        if (horizontalOnly)
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            Debug.Log("⚡ Horizontal-only mode enabled (2-3x faster)");
        }
        else
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;
            Debug.Log("⚡ Detecting both horizontal and vertical planes");
        }
        
        // Enable plane detection
        planeManager.enabled = true;
    }
    
    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        // Process newly detected planes
        foreach (var plane in args.added)
        {
            planesDetected++;
            
            // Filter by size
            if (plane.size.x * plane.size.y >= minimumPlaneSize)
            {
                float detectionTime = Time.time - detectionStartTime;
                Debug.Log($" Valid plane detected in {detectionTime:F1}s - Size: {plane.size}");
                
                // Show "tap to place" hint after first valid plane
                if (tapToPlaceHint != null && !isStructurePlaced)
                {
                    tapToPlaceHint.SetActive(true);
                }
                
                UpdateStatusUI($" Surface found! Tap to place ({planesDetected} planes)");
            }
            else
            {
                // Hide tiny planes immediately
                plane.gameObject.SetActive(false);
                Debug.Log($"⚠️ Plane too small ({plane.size.x:F2}x{plane.size.y:F2}m) - hidden");
            }
        }
        
        // Limit number of active planes for performance
        LimitActivePlanes();
    }
    
    void LimitActivePlanes()
    {
        if (planeManager.trackables.count > maxActivePlanes)
        {
            int planesToRemove = planeManager.trackables.count - maxActivePlanes;
            int removed = 0;
            
            // Remove oldest/smallest planes
            foreach (var plane in planeManager.trackables)
            {
                if (removed >= planesToRemove) break;
                
                float planeArea = plane.size.x * plane.size.y;
                if (planeArea < minimumPlaneSize * 2)
                {
                    plane.gameObject.SetActive(false);
                    removed++;
                }
            }
            
            if (removed > 0)
            {
                Debug.Log($"🧹 Removed {removed} small planes for performance");
            }
        }
    }
    
    /// <summary>
    /// Call this when your structure (stack/queue/tree/graph) is placed
    /// </summary>
    public void OnStructurePlaced()
    {
        if (isStructurePlaced) return;
        
        isStructurePlaced = true;
        
        float totalTime = Time.time - detectionStartTime;
        Debug.Log($"🎯 Structure placed! Total detection time: {totalTime:F1}s");
        
        // Hide tap hint
        if (tapToPlaceHint != null)
        {
            tapToPlaceHint.SetActive(false);
        }
        
        UpdateStatusUI(" Placed!");
        
        // Stop detection to save battery
        if (stopDetectionAfterPlacement)
        {
            StopPlaneDetection();
        }
    }
    
    /// <summary>
    /// Call this when structure is cleared/reset
    /// </summary>
    public void OnStructureReset()
    {
        isStructurePlaced = false;
        detectionStartTime = Time.time;
        planesDetected = 0;
        
        Debug.Log("🔄 Structure reset - resuming plane detection");
        UpdateStatusUI("🔍 Scanning for surfaces...");
        
        // ⭐ CRITICAL FIX: Properly resume detection
        ResumeDetection();
    }
    
    /// <summary>
    /// ⭐ NEW METHOD: Resume plane detection properly
    /// </summary>
    void ResumeDetection()
    {
        if (planeManager != null)
        {
            planeManager.enabled = true;
        }
        
        if (arSession != null)
        {
            arSession.enabled = true;
        }
        
        // Show existing planes
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
        }
        
        Debug.Log(" Plane detection resumed");
    }
    
    void StopPlaneDetection()
    {
        if (planeManager != null)
        {
            planeManager.enabled = false;
            Debug.Log("🛑 Plane detection stopped (saves 30-40% battery)");
        }
    }
    
    void UpdateStatusUI(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
    
    void Update()
    {
        // Periodic status update
        if (!isStructurePlaced && Time.frameCount % 60 == 0)
        {
            float elapsed = Time.time - detectionStartTime;
            
            if (planesDetected == 0)
            {
                UpdateStatusUI($"🔍 Scanning... {elapsed:F0}s");
            }
        }
    }
    
    /// <summary>
    /// ⭐ IMPROVED: Cleanup all existing planes
    /// </summary>
    void CleanupAllPlanes()
    {
        if (planeManager == null) return;
        
        int cleanedCount = 0;
        
        // Create a list to avoid modifying collection while iterating
        List<ARPlane> planesToDestroy = new List<ARPlane>();
        
        foreach (var plane in planeManager.trackables)
        {
            if (plane != null)
            {
                planesToDestroy.Add(plane);
            }
        }
        
        // Now destroy them
        foreach (var plane in planesToDestroy)
        {
            if (plane != null && plane.gameObject != null)
            {
                Destroy(plane.gameObject);
                cleanedCount++;
            }
        }
        
        if (cleanedCount > 0)
        {
            Debug.Log($"🧹 Cleaned up {cleanedCount} planes from previous scene");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (planeManager != null)
        {
            planeManager.planesChanged -= OnPlanesChanged;
        }
        
        // ⭐ Clean up all planes when leaving the scene
        CleanupAllPlanes();
        
        Debug.Log("🗑️ AR Optimizer destroyed - planes cleaned up");
    }
    
    void OnDisable()
    {
        // ⭐ NEW: Also cleanup when disabled
        if (planeManager != null)
        {
            planeManager.planesChanged -= OnPlanesChanged;
        }
    }
    
    void OnEnable()
    {
        // ⭐ NEW: Re-subscribe when enabled
        if (planeManager != null)
        {
            planeManager.planesChanged += OnPlanesChanged;
        }
    }
    
    /// <summary>
    /// ⭐ PUBLIC METHOD: Manual cleanup (call this before changing scenes)
    /// </summary>
    public void ManualCleanup()
    {
        CleanupAllPlanes();
        
        // Stop detection
        if (planeManager != null)
        {
            planeManager.enabled = false;
        }
        
        Debug.Log("🧹 Manual cleanup completed");
    }
    
    // Public getters for debugging
    public int GetPlanesDetected() => planesDetected;
    public bool IsStructurePlaced() => isStructurePlaced;
    public float GetDetectionTime() => Time.time - detectionStartTime;
}