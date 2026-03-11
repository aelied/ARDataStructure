using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class StackManager : MonoBehaviour
{
    [Header("Stack Settings")]
    public GameObject nodePrefab;
    public float nodeHeight = 0.3f;
    public int maxStackSize = 6;
    
    [Header("Stack Position")]
    public Transform stackStartPosition;
    
    
    [Header("AR References")]
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;
    
    private List<StackNode> stackNodes = new List<StackNode>();
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private bool stackPlaced = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    
    void Start()
    {
        // Set initial position
        if (stackStartPosition != null)
        {
            basePosition = stackStartPosition.position;
        }
        else
        {
            basePosition = transform.position;
        }
        
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
        
        // Verify nodePrefab
        if (nodePrefab == null)
        {
            Debug.LogError(" NODE PREFAB IS NOT ASSIGNED! Please assign it in the Inspector.");
        }
        else
        {
            Debug.Log(" Node Prefab assigned: " + nodePrefab.name);
        }
    }
    
    void Update()
    {
        // Only check for touches if stack hasn't been placed yet
        if (!stackPlaced)
        {
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
                if (raycastManager != null && raycastManager.Raycast(touchPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
                {
                    Debug.Log($" Hit {hits.Count} planes!");
                    
                    // Get the hit pose
                    Pose hitPose = hits[0].pose;
                    
                    // Set base position and rotation
                    basePosition = hitPose.position;
                    baseRotation = hitPose.rotation;
                    
                    // Move the entire stack manager to this position
                    transform.position = basePosition;
                    transform.rotation = baseRotation;
                    
                    stackPlaced = true;
                    
                    Debug.Log("🎯 Stack placed at: " + basePosition);
                    
                    // Disable plane visualization after placement
                    HidePlanes();
                }
                else
                {
                    Debug.LogWarning("⚠️ Raycast did NOT hit any planes. Make sure planes are detected.");
                }
            }
        }
    }
    
    // Add a node to the top of the stack (Push)
    public void Push(string value)
    {
        Debug.Log($"🔹 Push called with value: {value}");
        
        // Check if stack is full
        if (stackNodes.Count >= maxStackSize)
        {
            Debug.LogWarning("⚠️ Stack is full! Cannot push.");
            return;
        }
        
        // Check if nodePrefab is assigned
        if (nodePrefab == null)
        {
            Debug.LogError(" Cannot push: nodePrefab is not assigned!");
            return;
        }
        
        // Calculate position for new node (on top)
        Vector3 newPosition = CalculateNodePosition(stackNodes.Count);
        Debug.Log($"📍 Creating node at position: {newPosition}");
        
        // Create the new node
        GameObject nodeObj = Instantiate(nodePrefab, newPosition, baseRotation);
        nodeObj.transform.SetParent(transform);
        
        Debug.Log($" Node GameObject created: {nodeObj.name}");
        
        // Get the StackNode component and set its value
        StackNode node = nodeObj.GetComponent<StackNode>();
        if (node != null)
        {
            node.SetValue(value);
            node.AnimateAppear();
            Debug.Log($" StackNode component found and value set to: {value}");
        }
        else
        {
            Debug.LogError(" StackNode component NOT FOUND on prefab! Make sure the prefab has StackNode script attached.");
        }
        
        // Add to our list
        stackNodes.Add(node);
        
        Debug.Log($" Pushed: {value}. Stack size: {stackNodes.Count}");
    }
    
    // Remove a node from the top of the stack (Pop)
    public void Pop()
    {
        // Check if stack is empty
        if (stackNodes.Count == 0)
        {
            Debug.LogWarning("⚠️ Stack is empty! Cannot pop.");
            return;
        }
        
        // Get the top node (last in list)
        int topIndex = stackNodes.Count - 1;
        StackNode topNode = stackNodes[topIndex];
        string value = topNode.nodeValue;
        
        // Remove from list
        stackNodes.RemoveAt(topIndex);
        
        // Animate and destroy
        topNode.AnimateDisappear();
        
        Debug.Log($" Popped: {value}. Stack size: {stackNodes.Count}");
    }
    
    // Peek at the top node without removing it
    public string Peek()
    {
        if (stackNodes.Count == 0)
        {
            return "Empty";
        }
        return stackNodes[stackNodes.Count - 1].nodeValue;
    }
    
    // Get current stack size
    public int Size()
    {
        return stackNodes.Count;
    }
    
    // Check if stack is empty
    public bool IsEmpty()
    {
        return stackNodes.Count == 0;
    }
    
    // Check if stack has been placed
    public bool IsStackPlaced()
    {
        return stackPlaced;
    }
    
    // Get list of nodes (needed for StackLabels if you have one)
    public List<StackNode> GetNodes()
    {
        return stackNodes;
    }
    
    // Clear all nodes (but keep placement)
    public void Clear()
    {
        foreach (StackNode node in stackNodes)
        {
            if (node != null)
            {
                Destroy(node.gameObject);
            }
        }
        stackNodes.Clear();
        Debug.Log("🗑️ Stack cleared!");
    }
    
    // Reset everything - clear nodes AND reset placement
    public void ResetStack()
    {
        // Clear all nodes
        Clear();
        
        // Reset placement flag
        stackPlaced = false;
        
        // Show planes again
        ShowPlanes();
        
        Debug.Log("🔄 Stack RESET! You can now place the stack again.");
    }
    
    // Hide AR planes
    void HidePlanes()
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
    
    // Show AR planes
    void ShowPlanes()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
            Debug.Log("👁️ AR Planes visible again");
        }
    }
    
    // Calculate position for a node at given index
    Vector3 CalculateNodePosition(int index)
    {
        // Arrange nodes vertically (stacked upward)
        Vector3 offset = Vector3.up * (index * nodeHeight);
        return basePosition + offset;
    }
    
    // Add some test data (for testing)
    public void AddTestData()
    {
        string[] testValues = { "1", "2", "3", "4" };
        foreach (string value in testValues)
        {
            if (stackNodes.Count < maxStackSize)
            {
                Push(value);
            }
        }
    }
}