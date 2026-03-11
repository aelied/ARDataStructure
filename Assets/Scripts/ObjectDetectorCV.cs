using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Detects physical objects placed on AR plane using computer vision
/// Works by comparing camera feed before/after object placement
/// </summary>
public class ObjectDetectorCV : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionInterval = 0.5f; // Check every 0.5 seconds
    public float changeThreshold = 0.15f; // 15% change to consider new object
    public int minBlobSize = 20; // Minimum pixels to consider as object
    public bool showDebugTexture = false;
    
    [Header("AR References")]
    public ARCameraManager cameraManager;
    public ARPlaneManager planeManager;
    
    // Detection data
    private Texture2D baselineTexture;
    private Texture2D currentTexture;
    private Texture2D differenceTexture;
    private float lastDetectionTime;
    private bool isDetecting = false;
    private ARPlane trackedPlane;
    private Rect planeScreenRect;
    
    // Detected objects
    public class DetectedObject
    {
        public Vector2 screenPosition;
        public Vector3 worldPosition;
        public float size;
        public Color averageColor;
        public int pixelCount;
    }
    
    private List<DetectedObject> detectedObjects = new List<DetectedObject>();
    
    // Events
    public delegate void OnObjectDetected(Vector3 worldPosition, float size);
    public event OnObjectDetected ObjectDetected;
    
    public delegate void OnObjectRemoved(Vector3 worldPosition);
    public event OnObjectRemoved ObjectRemoved;
    
    void Start()
    {
        if (cameraManager == null)
            cameraManager = FindObjectOfType<ARCameraManager>();
        
        if (planeManager == null)
            planeManager = FindObjectOfType<ARPlaneManager>();
        
        if (cameraManager != null)
        {
            cameraManager.frameReceived += OnCameraFrameReceived;
        }
        
        Debug.Log("🎥 Object Detector CV Initialized");
    }
    
    void OnDestroy()
    {
        if (cameraManager != null)
        {
            cameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }
    
    /// <summary>
    /// Start detecting objects on a specific plane
    /// </summary>
    public void StartDetecting(ARPlane plane)
    {
        trackedPlane = plane;
        isDetecting = true;
        detectedObjects.Clear();
        
        // Calculate plane's screen rectangle
        CalculatePlaneScreenRect();
        
        // Capture baseline (empty plane)
        CaptureBaseline();
        
        Debug.Log($"🎯 Started detecting on plane {plane.trackableId}");
    }
    
    /// <summary>
    /// Stop detecting objects
    /// </summary>
    public void StopDetecting()
    {
        isDetecting = false;
        trackedPlane = null;
        detectedObjects.Clear();
        
        Debug.Log("⏹️ Stopped detecting");
    }
    
    /// <summary>
    /// Get list of currently detected objects
    /// </summary>
    public List<DetectedObject> GetDetectedObjects()
    {
        return new List<DetectedObject>(detectedObjects);
    }
    
    void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (!isDetecting || trackedPlane == null) return;
        
        if (Time.time - lastDetectionTime < detectionInterval) return;
        
        lastDetectionTime = Time.time;
        
        // Capture current frame
        CaptureCurrentFrame();
        
        // Detect changes
        DetectChanges();
    }
    
    void CalculatePlaneScreenRect()
    {
        if (trackedPlane == null || Camera.main == null) return;
        
        // Get plane bounds in world space
        MeshCollider meshCollider = trackedPlane.GetComponent<MeshCollider>();
        Bounds planeBounds;
        
        if (meshCollider != null && meshCollider.sharedMesh != null)
        {
            planeBounds = meshCollider.bounds;
        }
        else
        {
            // Fallback: create bounds from plane center and size
            planeBounds = new Bounds(trackedPlane.center, trackedPlane.size);
        }
        
        // Project bounds to screen space
        Vector3[] corners = new Vector3[4];
        corners[0] = planeBounds.min;
        corners[1] = new Vector3(planeBounds.max.x, planeBounds.min.y, planeBounds.min.z);
        corners[2] = planeBounds.max;
        corners[3] = new Vector3(planeBounds.min.x, planeBounds.min.y, planeBounds.max.z);
        
        Vector2 minScreen = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 maxScreen = new Vector2(float.MinValue, float.MinValue);
        
        foreach (Vector3 corner in corners)
        {
            Vector2 screenPoint = Camera.main.WorldToScreenPoint(corner);
            minScreen = Vector2.Min(minScreen, screenPoint);
            maxScreen = Vector2.Max(maxScreen, screenPoint);
        }
        
        planeScreenRect = new Rect(minScreen.x, minScreen.y, maxScreen.x - minScreen.x, maxScreen.y - minScreen.y);
        
        Debug.Log($"📐 Plane screen rect: {planeScreenRect}");
    }
    
    void CaptureBaseline()
    {
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            Debug.LogWarning("⚠️ Could not acquire camera image for baseline");
            return;
        }
        
        baselineTexture = ConvertToTexture(image);
        image.Dispose();
        
        Debug.Log("📸 Baseline captured");
    }
    
    void CaptureCurrentFrame()
    {
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            return;
        }
        
        currentTexture = ConvertToTexture(image);
        image.Dispose();
    }
    
    Texture2D ConvertToTexture(XRCpuImage image)
    {
        // Convert to RGBA format
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width / 4, image.height / 4), // Downscale for performance
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorY
        };
        
        int size = image.GetConvertedDataSize(conversionParams);
        var buffer = new Unity.Collections.NativeArray<byte>(size, Unity.Collections.Allocator.Temp);
        
        image.Convert(conversionParams, buffer);
        
        Texture2D texture = new Texture2D(
            conversionParams.outputDimensions.x,
            conversionParams.outputDimensions.y,
            TextureFormat.RGBA32,
            false
        );
        
        texture.LoadRawTextureData(buffer);
        texture.Apply();
        
        buffer.Dispose();
        
        return texture;
    }
    
    void DetectChanges()
    {
        if (baselineTexture == null || currentTexture == null) return;
        
        int width = Mathf.Min(baselineTexture.width, currentTexture.width);
        int height = Mathf.Min(baselineTexture.height, currentTexture.height);
        
        // Create difference texture
        differenceTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        Color[] baselinePixels = baselineTexture.GetPixels();
        Color[] currentPixels = currentTexture.GetPixels();
        Color[] diffPixels = new Color[width * height];
        
        // Track changed regions (potential objects)
        bool[,] changedPixels = new bool[width, height];
        int totalChanged = 0;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                
                Color baseline = baselinePixels[index];
                Color current = currentPixels[index];
                
                // Calculate difference
                float diff = Mathf.Abs(baseline.r - current.r) +
                            Mathf.Abs(baseline.g - current.g) +
                            Mathf.Abs(baseline.b - current.b);
                
                diff /= 3f; // Normalize
                
                if (diff > changeThreshold)
                {
                    diffPixels[index] = Color.white; // Changed
                    changedPixels[x, y] = true;
                    totalChanged++;
                }
                else
                {
                    diffPixels[index] = Color.black; // No change
                    changedPixels[x, y] = false;
                }
            }
        }
        
        differenceTexture.SetPixels(diffPixels);
        differenceTexture.Apply();
        
        // Find blobs (connected components)
        if (totalChanged > minBlobSize)
        {
            List<DetectedObject> newObjects = FindBlobs(changedPixels, width, height, currentPixels);
            
            // Compare with previous detections
            CompareDetections(newObjects);
        }
    }
    
    List<DetectedObject> FindBlobs(bool[,] pixels, int width, int height, Color[] imagePixels)
    {
        List<DetectedObject> blobs = new List<DetectedObject>();
        bool[,] visited = new bool[width, height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[x, y] && !visited[x, y])
                {
                    // Found a new blob, do flood fill
                    List<Vector2Int> blobPixels = new List<Vector2Int>();
                    FloodFill(x, y, pixels, visited, width, height, blobPixels);
                    
                    if (blobPixels.Count >= minBlobSize)
                    {
                        // Calculate blob center and properties
                        Vector2 center = Vector2.zero;
                        Color averageColor = Color.black;
                        
                        foreach (Vector2Int pixel in blobPixels)
                        {
                            center += new Vector2(pixel.x, pixel.y);
                            int index = pixel.y * width + pixel.x;
                            averageColor += imagePixels[index];
                        }
                        
                        center /= blobPixels.Count;
                        averageColor /= blobPixels.Count;
                        
                        // Convert to screen space
                        Vector2 screenPos = new Vector2(
                            (center.x / width) * Screen.width,
                            (center.y / height) * Screen.height
                        );
                        
                        // Convert to world space using raycast
                        Vector3 worldPos = ScreenToWorld(screenPos);
                        
                        if (worldPos != Vector3.zero)
                        {
                            DetectedObject obj = new DetectedObject
                            {
                                screenPosition = screenPos,
                                worldPosition = worldPos,
                                size = blobPixels.Count,
                                averageColor = averageColor,
                                pixelCount = blobPixels.Count
                            };
                            
                            blobs.Add(obj);
                        }
                    }
                }
            }
        }
        
        return blobs;
    }
    
    void FloodFill(int startX, int startY, bool[,] pixels, bool[,] visited, int width, int height, List<Vector2Int> result)
    {
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(startX, startY));
        
        while (stack.Count > 0)
        {
            Vector2Int current = stack.Pop();
            int x = current.x;
            int y = current.y;
            
            if (x < 0 || x >= width || y < 0 || y >= height) continue;
            if (visited[x, y] || !pixels[x, y]) continue;
            
            visited[x, y] = true;
            result.Add(current);
            
            // Check 4-connected neighbors
            stack.Push(new Vector2Int(x + 1, y));
            stack.Push(new Vector2Int(x - 1, y));
            stack.Push(new Vector2Int(x, y + 1));
            stack.Push(new Vector2Int(x, y - 1));
        }
    }
    
    Vector3 ScreenToWorld(Vector2 screenPos)
    {
        // Raycast from screen position to plane
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        
        ARRaycastManager raycastManager = FindObjectOfType<ARRaycastManager>();
        if (raycastManager == null) return Vector3.zero;
        
        if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            foreach (var hit in hits)
            {
                if (planeManager.GetPlane(hit.trackableId) == trackedPlane)
                {
                    return hit.pose.position;
                }
            }
        }
        
        return Vector3.zero;
    }
    
    void CompareDetections(List<DetectedObject> newObjects)
    {
        // Check for new objects
        foreach (var newObj in newObjects)
        {
            bool isNew = true;
            
            foreach (var existing in detectedObjects)
            {
                float distance = Vector3.Distance(newObj.worldPosition, existing.worldPosition);
                if (distance < 0.1f) // Within 10cm = same object
                {
                    isNew = false;
                    break;
                }
            }
            
            if (isNew)
            {
                detectedObjects.Add(newObj);
                ObjectDetected?.Invoke(newObj.worldPosition, newObj.size);
                Debug.Log($"✨ NEW OBJECT detected at {newObj.worldPosition} with {newObj.pixelCount} pixels");
            }
        }
        
        // Check for removed objects
        for (int i = detectedObjects.Count - 1; i >= 0; i--)
        {
            bool stillExists = false;
            
            foreach (var newObj in newObjects)
            {
                float distance = Vector3.Distance(detectedObjects[i].worldPosition, newObj.worldPosition);
                if (distance < 0.1f)
                {
                    stillExists = true;
                    break;
                }
            }
            
            if (!stillExists)
            {
                Vector3 removedPos = detectedObjects[i].worldPosition;
                detectedObjects.RemoveAt(i);
                ObjectRemoved?.Invoke(removedPos);
                Debug.Log($"🗑️ OBJECT REMOVED from {removedPos}");
            }
        }
    }
    
    void OnGUI()
    {
        if (!showDebugTexture || differenceTexture == null) return;
        
        // Show difference texture for debugging
        float scale = 0.3f;
        GUI.DrawTexture(
            new Rect(10, 10, differenceTexture.width * scale, differenceTexture.height * scale),
            differenceTexture
        );
        
        GUI.Label(new Rect(10, differenceTexture.height * scale + 20, 300, 30),
            $"Detected Objects: {detectedObjects.Count}");
    }
}