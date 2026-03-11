using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class QRLinkedListManager : MonoBehaviour
{
    public enum InteractionMode
    {
        Physical,
        Virtual
    }
    
    [Header("Mode Settings")]
    public InteractionMode currentMode = InteractionMode.Physical;
    
    [Header("AR References")]
    public ARTrackedImageManager trackedImageManager;
    
    [Header("3D Object Prefabs")]
    public GameObject cubePrefab;
    public GameObject chairPrefab;
    public GameObject coinPrefab;
    public GameObject penPrefab;
    public GameObject bookPrefab;
    
    [Header("Visual Elements")]
    public GameObject nodeLabelPrefab;
    public GameObject pointerArrowPrefab;
    
    [Header("Algorithm Analysis")]
    public LinkedListAnalysisManager analysisManager;
    
    [Header("UI References")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI detectionFeedbackText;
    public TextMeshProUGUI modeIndicatorText;
    
    [Header("Colors")]
    public Color detectingColor = Color.green;
    public Color notDetectingColor = Color.red;
    public Color headNodeColor = new Color(1f, 0.84f, 0f); // Gold
    public Color tailNodeColor = new Color(0.5f, 0f, 0.5f); // Purple
    public Color regularNodeColor = new Color(0, 0.8f, 1f, 0.8f); // Cyan
    
    [Header("Audio")]
    public AudioClip scanSound;
    public AudioClip confirmSound;
    public AudioClip insertSound;
    public AudioClip deleteSound;
    public AudioClip accessSound;
    public AudioClip errorSound;
    private AudioSource audioSource;
    
    [Header("Animation Settings")]
    public float animationDuration = 0.5f;
    public float nodeSpacing = 0.2f;
    
    private enum ListState
    {
        ModeSelection,
        WaitingForFirstNode,
        WaitingToConfirmFirst,
        ListReady,
        WaitingForInsertShift,
        WaitingForInsertNode,
        WaitingToConfirmInsert,
        Animating
    }
    
    private ListState currentState = ListState.ModeSelection;
    
    private class LinkedListNode
    {
        public Vector3 position;
        public GameObject nodeLabel;
        public GameObject objectInstance;
        public string objectName;
        public ARTrackedImage trackedImage;
        public bool isConfirmed;
        public ObjectAnimator animator;
        public bool isVirtual;
        public int nodeId;
        
        public LinkedListNode next;
        public GameObject pointerArrow;
    }
    
    private LinkedListNode head = null;
    private Dictionary<string, ARTrackedImage> trackedQRCodes = new Dictionary<string, ARTrackedImage>();
    private Dictionary<string, GameObject> objectPrefabs = new Dictionary<string, GameObject>();
    private HashSet<string> processedImages = new HashSet<string>();
    
    private string virtualObjectType = "";
    private Vector3 virtualListStartPosition;
    private bool virtualListInitialized = false;
    
    private Vector3 listDirection = Vector3.right;
    
    private ARTrackedImage pendingFirstImage;
    private ARTrackedImage pendingInsertImage;
    private string pendingInsertObjectName = "";
    private int insertAtPosition = -1;
    private int nodeIdCounter = 1;
    
    private GameObject currentPreviewObject = null;
    private List<GameObject> shiftIndicators = new List<GameObject>();
    private GameObject insertPositionIndicator;
    
    void Start()
    {
        if (trackedImageManager == null)
            trackedImageManager = FindObjectOfType<ARTrackedImageManager>();
        
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        
        SetupObjectPrefabs();
        UpdateModeIndicator();
        UpdateInstructions();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.gameObject.SetActive(true);
            detectionFeedbackText.text = "Choose a mode to begin";
            detectionFeedbackText.color = Color.yellow;
        }
        
        Debug.Log($"QR Linked List Manager Initialized - Mode: {currentMode}");
    }
    
    public void SetPhysicalMode()
    {
        currentMode = InteractionMode.Physical;
        currentState = ListState.WaitingForFirstNode;
        UpdateModeIndicator();
        UpdateInstructions();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Physical Mode: Scan first QR";
            detectionFeedbackText.color = notDetectingColor;
        }
        
        Debug.Log("Switched to PHYSICAL mode");
    }
    
    public void SetVirtualMode()
    {
        currentMode = InteractionMode.Virtual;
        currentState = ListState.WaitingForFirstNode;
        UpdateModeIndicator();
        UpdateInstructions();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Virtual Mode: Scan ONE QR to define nodes";
            detectionFeedbackText.color = notDetectingColor;
        }
        
        Debug.Log("Switched to VIRTUAL mode");
    }
    
    void UpdateModeIndicator()
    {
        if (modeIndicatorText != null)
        {
            if (currentMode == InteractionMode.Physical)
            {
                modeIndicatorText.text = "PHYSICAL MODE";
                modeIndicatorText.color = new Color(0.2f, 0.8f, 1f);
            }
            else
            {
                modeIndicatorText.text = "VIRTUAL MODE";
                modeIndicatorText.color = new Color(1f, 0.7f, 0.2f);
            }
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    void SetupObjectPrefabs()
    {
        if (cubePrefab != null) objectPrefabs["cube"] = cubePrefab;
        if (chairPrefab != null) objectPrefabs["chair"] = chairPrefab;
        if (coinPrefab != null) objectPrefabs["coin"] = coinPrefab;
        if (penPrefab != null) objectPrefabs["pen"] = penPrefab;
        if (bookPrefab != null) objectPrefabs["book"] = bookPrefab;
    }
    
    void OnEnable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }
    
    void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }
    
    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleTapConfirmation(Input.GetTouch(0).position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            HandleTapConfirmation(Input.mousePosition);
        }
    }
    
    void HandleTapConfirmation(Vector2 screenPosition)
    {
        switch (currentState)
        {
            case ListState.WaitingToConfirmFirst:
                if (currentMode == InteractionMode.Physical)
                {
                    if (pendingFirstImage != null && TapNearTrackedImage(screenPosition, pendingFirstImage))
                    {
                        ConfirmFirstNode();
                    }
                }
                else
                {
                    ConfirmFirstNode();
                }
                break;
                
            case ListState.WaitingToConfirmInsert:
                if (currentMode == InteractionMode.Physical)
                {
                    if (pendingInsertImage != null && TapNearTrackedImage(screenPosition, pendingInsertImage))
                    {
                        ConfirmInsertNode();
                    }
                }
                else
                {
                    ConfirmInsertNode();
                }
                break;
        }
    }
    
    bool TapNearTrackedImage(Vector2 screenPosition, ARTrackedImage trackedImage)
    {
        Camera arCamera = Camera.main;
        if (arCamera == null) return false;
        
        Vector3 screenPoint = arCamera.WorldToScreenPoint(trackedImage.transform.position);
        float distance = Vector2.Distance(screenPosition, new Vector2(screenPoint.x, screenPoint.y));
        
        float threshold = Screen.width * 0.15f;
        return distance < threshold;
    }
    
    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var trackedImage in args.added)
        {
            OnImageDetected(trackedImage);
        }
        
        foreach (var trackedImage in args.updated)
        {
            string imageName = trackedImage.referenceImage.name.ToLower();
            
            if (!processedImages.Contains(imageName) && 
                trackedImage.trackingState == TrackingState.Tracking)
            {
                OnImageDetected(trackedImage);
            }
            else if (currentMode == InteractionMode.Physical && currentState != ListState.Animating)
            {
                UpdateTrackedNode(trackedImage);
            }
        }
    }
    
    void OnImageDetected(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name.ToLower();
        Vector3 imagePosition = trackedImage.transform.position;
        
        if (currentMode == InteractionMode.Virtual)
        {
            if (currentState == ListState.WaitingForFirstNode && string.IsNullOrEmpty(virtualObjectType))
            {
                // First scan in virtual mode
            }
            else if (currentState == ListState.WaitingForFirstNode)
            {
                Debug.Log($"Virtual mode: Already using {virtualObjectType}");
                return;
            }
            else
            {
                return;
            }
        }
        
        bool shouldProcess = false;
        
        switch (currentState)
        {
            case ListState.WaitingForFirstNode:
            case ListState.WaitingForInsertNode:
                shouldProcess = true;
                break;
                
            default:
                shouldProcess = false;
                break;
        }
        
        if (!shouldProcess)
        {
            Debug.Log($"Skipping {imageName} - state: {currentState}");
            return;
        }
        
        // PHYSICAL MODE: Check if QR is already in the list
        if (currentMode == InteractionMode.Physical && currentState == ListState.WaitingForInsertNode)
        {
            bool alreadyUsed = false;
            LinkedListNode current = head;
            
            while (current != null)
            {
                if (current.objectName.ToLower() == imageName)
                {
                    alreadyUsed = true;
                    break;
                }
                current = current.next;
            }
            
            if (alreadyUsed)
            {
                Debug.Log($"Physical mode: {imageName} already used in the list!");
                PlaySound(errorSound);
                
                if (detectionFeedbackText != null)
                {
                    detectionFeedbackText.text = $"{imageName.ToUpper()} already used! Scan different QR";
                    detectionFeedbackText.color = Color.red;
                }
                
                return;
            }
        }
        
        if (currentState == ListState.WaitingForInsertNode)
        {
            Debug.Log($"INSERT MODE: Detected {imageName}");
            PlaySound(scanSound);
            
            if (detectionFeedbackText != null)
            {
                detectionFeedbackText.text = $"Detected: {imageName.ToUpper()}";
                detectionFeedbackText.color = detectingColor;
            }
            
            if (!trackedQRCodes.ContainsKey(imageName))
            {
                trackedQRCodes[imageName] = trackedImage;
            }
            
            ShowInsertNodePreview(imageName, trackedImage);
            return;
        }
        
        if (processedImages.Contains(imageName))
        {
            Debug.Log($"{imageName} already processed");
            return;
        }
        
        Debug.Log($"Detected: {imageName} at {imagePosition}");
        PlaySound(scanSound);
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = $"Detected: {imageName.ToUpper()}";
            detectionFeedbackText.color = detectingColor;
        }
        
        if (!trackedQRCodes.ContainsKey(imageName))
        {
            trackedQRCodes[imageName] = trackedImage;
        }
        
        processedImages.Add(imageName);
        
        if (currentState == ListState.WaitingForFirstNode)
        {
            ShowFirstNodePreview(imageName, trackedImage);
        }
    }
    
    void ShowFirstNodePreview(string objectName, ARTrackedImage trackedImage)
    {
        if (currentPreviewObject != null)
        {
            Destroy(currentPreviewObject);
            currentPreviewObject = null;
        }
        
        pendingFirstImage = trackedImage;
        
        GameObject previewObj;
        
        if (currentMode == InteractionMode.Physical)
        {
            previewObj = InstantiateObject(objectName, trackedImage);
        }
        else
        {
            virtualObjectType = objectName;
            virtualListStartPosition = trackedImage.transform.position;
            previewObj = InstantiateObjectVirtual(objectName, virtualListStartPosition);
            
            Debug.Log($"Virtual mode: Set object type to {virtualObjectType}");
        }
        
        currentPreviewObject = previewObj;
        
        Renderer[] renderers = previewObj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Color color = rend.material.color;
            color.a = 0.7f;
            rend.material.color = color;
        }
        
        currentState = ListState.WaitingToConfirmFirst;
        
        if (instructionText != null)
        {
            if (currentMode == InteractionMode.Physical)
            {
                instructionText.text = $"Tap the {objectName.ToUpper()} QR to confirm HEAD";
            }
            else
            {
                instructionText.text = $"Tap ANYWHERE to confirm {objectName.ToUpper()} as list nodes";
            }
        }
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Tap to confirm";
            detectionFeedbackText.color = Color.yellow;
        }
    }
    
    void ConfirmFirstNode()
    {
        if (pendingFirstImage == null && currentMode == InteractionMode.Physical) return;
        
        string objectName = currentMode == InteractionMode.Physical ? 
            pendingFirstImage.referenceImage.name.ToLower() : virtualObjectType;
        
        PlaySound(confirmSound);
        
        GameObject existingObject = currentPreviewObject;
        Vector3 position;
        
        if (currentMode == InteractionMode.Physical)
        {
            position = pendingFirstImage.transform.position;
            if (existingObject != null)
            {
                MakeOpaque(existingObject);
            }
        }
        else
        {
            if (existingObject != null)
            {
                MakeOpaque(existingObject);
            }
            position = virtualListStartPosition;
            virtualListInitialized = true;
        }
        
        currentPreviewObject = null;
        
        head = CreateNode(objectName, position, pendingFirstImage, true, existingObject);
        
        currentState = ListState.ListReady;
        UpdateInstructions();
        UpdateAllLabels();
        UpdateStatusDisplay();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "List Ready!";
            detectionFeedbackText.color = detectingColor;
        }
        
        Debug.Log($"Linked List initialized - Mode: {currentMode}");
        pendingFirstImage = null;
    }
    
    LinkedListNode CreateNode(string objectName, Vector3 position, ARTrackedImage trackedImage, 
                             bool confirmed, GameObject existingObject = null)
    {
        GameObject objectInstance = existingObject;
        
        if (objectInstance == null)
        {
            if (currentMode == InteractionMode.Physical)
            {
                objectInstance = InstantiateObject(objectName, trackedImage);
            }
            else
            {
                objectInstance = InstantiateObjectVirtual(objectName, position);
            }
        }
        
        ObjectAnimator animator = objectInstance.GetComponent<ObjectAnimator>();
        if (animator == null)
        {
            animator = objectInstance.AddComponent<ObjectAnimator>();
        }
        
        // Disable animator for virtual nodes to prevent position conflicts
        if (currentMode == InteractionMode.Virtual)
        {
            animator.enabled = false;
        }
        
        LinkedListNode node = new LinkedListNode
        {
            position = position,
            objectName = objectName,
            objectInstance = objectInstance,
            trackedImage = trackedImage,
            isConfirmed = confirmed,
            animator = animator,
            isVirtual = (currentMode == InteractionMode.Virtual),
            next = null,
            pointerArrow = null,
            nodeId = nodeIdCounter++
        };
        
        if (nodeLabelPrefab != null)
        {
            node.nodeLabel = Instantiate(nodeLabelPrefab);
            node.nodeLabel.transform.position = position + Vector3.up * 0.25f;
            
            TextMeshPro labelText = node.nodeLabel.GetComponentInChildren<TextMeshPro>();
            if (labelText != null)
            {
                labelText.text = ""; // Will be updated by UpdateAllLabels()
                labelText.color = regularNodeColor;
                labelText.fontSize = 3;
                labelText.alignment = TextAlignmentOptions.Center;
            }
        }
        
        return node;
    }
    
    void ShowInsertNodePreview(string objectName, ARTrackedImage trackedImage)
    {
        if (currentPreviewObject != null)
        {
            Destroy(currentPreviewObject);
            currentPreviewObject = null;
        }
        
        Vector3 position = trackedImage.transform.position;
        
        GameObject previewObj = InstantiateObject(objectName, trackedImage);
        
        if (previewObj == null)
        {
            Debug.LogError("Failed to create preview object!");
            return;
        }
        
        currentPreviewObject = previewObj;
        pendingInsertImage = trackedImage;
        pendingInsertObjectName = objectName;
        
        Renderer[] renderers = previewObj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Color color = rend.material.color;
            color.a = 0.7f;
            rend.material.color = color;
        }
        
        currentState = ListState.WaitingToConfirmInsert;
        
        if (instructionText != null)
        {
            instructionText.text = $"Tap the {objectName.ToUpper()} QR to insert";
        }
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Tap to confirm";
            detectionFeedbackText.color = Color.yellow;
        }
    }
    
    void ConfirmInsertNode()
    {
        PlaySound(insertSound);
        
        currentState = ListState.Animating;
        
        // Clear all indicators
        ClearShiftIndicators();
        ClearInsertPositionIndicator();
        
        GameObject existingObject = currentPreviewObject;
        Vector3 position;
        string objectName;
        
        if (currentMode == InteractionMode.Physical)
        {
            if (pendingInsertImage == null) return;
            
            if (existingObject != null)
            {
                MakeOpaque(existingObject);
            }
            position = pendingInsertImage.transform.position;
            objectName = pendingInsertObjectName;
        }
        else
        {
            objectName = virtualObjectType;
            // Calculate correct position BEFORE creating node
            position = CalculateInsertPosition(insertAtPosition);
            
            if (existingObject != null)
            {
                Destroy(existingObject);
            }
            
            // Don't create object here - let CreateNode handle it with correct position
            existingObject = null;
        }
        
        currentPreviewObject = null;
        ClearShiftIndicators();
        
        LinkedListNode newNode = CreateNode(objectName, position, 
            currentMode == InteractionMode.Physical ? pendingInsertImage : null, 
            true, existingObject);
        
        if (insertAtPosition == 0)
        {
            newNode.next = head;
            head = newNode;
            
            if (currentMode == InteractionMode.Virtual)
            {
                StartCoroutine(ShiftNodesRightWithRepositioning(0));
            }
            else
            {
                UpdateAllPointers();
                UpdateAllLabels();
                UpdateStatusDisplay();
                currentState = ListState.ListReady;
            }
            
            if (analysisManager != null)
            {
                analysisManager.AnalyzeInsertOperation(0, GetListSize());
            }
        }
        else if (insertAtPosition == -1)
        {
            LinkedListNode current = head;
            int count = 1;
            while (current.next != null)
            {
                current = current.next;
                count++;
            }
            current.next = newNode;
            
            if (currentMode == InteractionMode.Virtual)
            {
                // No animation needed for tail insert - just update
                UpdateAllPointers();
                UpdateAllLabels();
                UpdateStatusDisplay();
                currentState = ListState.ListReady;
            }
            else
            {
                UpdateAllPointers();
                UpdateAllLabels();
                UpdateStatusDisplay();
                currentState = ListState.ListReady;
            }
            
            if (analysisManager != null)
            {
                analysisManager.AnalyzeInsertOperation(count, GetListSize());
            }
        }
        else
        {
            LinkedListNode current = head;
            for (int i = 0; i < insertAtPosition - 1; i++)
            {
                if (current.next != null)
                    current = current.next;
            }
            newNode.next = current.next;
            current.next = newNode;
            
            if (currentMode == InteractionMode.Virtual)
            {
                StartCoroutine(ShiftNodesRightWithRepositioning(insertAtPosition));
            }
            else
            {
                UpdateAllPointers();
                UpdateAllLabels();
                UpdateStatusDisplay();
                currentState = ListState.ListReady;
            }
            
            if (analysisManager != null)
            {
                analysisManager.AnalyzeInsertOperation(insertAtPosition, GetListSize());
            }
        }
        
        insertAtPosition = -1;
        pendingInsertObjectName = "";
        pendingInsertImage = null;
    }
    
    System.Collections.IEnumerator ShiftNodesRightWithRepositioning(int startIndex)
    {
        yield return new WaitForSeconds(0.1f);
        
        // Reposition ALL nodes to their correct positions in virtual mode
        LinkedListNode current = head;
        int index = 0;
        
        while (current != null)
        {
            Vector3 targetPos = CalculateNodePosition(index);
            
            if (current.isVirtual)
            {
                StartCoroutine(MoveNode(current, targetPos));
            }
            
            current = current.next;
            index++;
        }
        
        yield return new WaitForSeconds(animationDuration);
        UpdateAllPointers();
        UpdateAllLabels();
        UpdateStatusDisplay();
        currentState = ListState.ListReady;
    }
    
    System.Collections.IEnumerator ShiftNodesLeftWithRepositioning(int startIndex)
    {
        yield return new WaitForSeconds(0.1f);
        
        // Reposition ALL nodes to their correct positions in virtual mode
        LinkedListNode current = head;
        int index = 0;
        
        while (current != null)
        {
            Vector3 targetPos = CalculateNodePosition(index);
            
            if (current.isVirtual)
            {
                StartCoroutine(MoveNode(current, targetPos));
            }
            
            current = current.next;
            index++;
        }
        
        yield return new WaitForSeconds(animationDuration);
        UpdateAllPointers();
        UpdateAllLabels();
        UpdateStatusDisplay();
        currentState = ListState.ListReady;
    }
    
    public void InsertAtHead()
    {
        if (currentState != ListState.ListReady) return;
        
        insertAtPosition = 0;
        
        if (currentMode == InteractionMode.Virtual)
        {
            currentState = ListState.WaitingToConfirmInsert;
            ConfirmInsertNode();
            
            if (instructionText != null)
                instructionText.text = $"Inserted {virtualObjectType.ToUpper()} at HEAD";
        }
        else
        {
            // Physical mode: Show shift indicators
            ShowInsertShiftIndicators(0);
            currentState = ListState.WaitingForInsertShift;
            
            if (instructionText != null)
            {
                int numToMove = GetListSize();
                instructionText.text = $"Move {numToMove} node(s) RIGHT (follow arrows), then CONFIRM";
            }
        }
    }
    
    public void InsertAtTail()
    {
        if (currentState != ListState.ListReady) return;
        
        insertAtPosition = -1;
        
        if (currentMode == InteractionMode.Virtual)
        {
            currentState = ListState.WaitingToConfirmInsert;
            ConfirmInsertNode();
            
            if (instructionText != null)
                instructionText.text = $"Inserted {virtualObjectType.ToUpper()} at TAIL";
        }
        else
        {
            // Physical mode: No shift needed for tail insert
            currentState = ListState.WaitingForInsertNode;
            
            if (instructionText != null)
                instructionText.text = "Scan QR for new TAIL node";
        }
    }
    
    public void InsertAtMiddle()
    {
        if (currentState != ListState.ListReady) return;
        
        int size = GetListSize();
        insertAtPosition = size / 2;
        
        if (currentMode == InteractionMode.Virtual)
        {
            currentState = ListState.WaitingToConfirmInsert;
            ConfirmInsertNode();
            
            if (instructionText != null)
                instructionText.text = $"Inserted {virtualObjectType.ToUpper()} at MIDDLE (position {insertAtPosition})";
        }
        else
        {
            // Physical mode: Show shift indicators
            ShowInsertShiftIndicators(insertAtPosition);
            currentState = ListState.WaitingForInsertShift;
            
            if (instructionText != null)
            {
                int numToMove = GetListSize() - insertAtPosition;
                instructionText.text = $"Move {numToMove} node(s) RIGHT (follow arrows), then CONFIRM";
            }
        }
    }
    
    public void ConfirmShift()
    {
        if (currentState != ListState.WaitingForInsertShift) return;
        
        ClearShiftIndicators();
        currentState = ListState.WaitingForInsertNode;
        
        // Show green indicator where to place the new QR code
        Vector3 insertPosition = CalculateInsertPosition(insertAtPosition);
        CreateInsertPositionIndicator(insertPosition);
        
        if (instructionText != null)
            instructionText.text = $"Scan NEW QR code near the GREEN sphere";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Place QR at green indicator";
            detectionFeedbackText.color = Color.green;
        }
    }
    
    void CreateInsertPositionIndicator(Vector3 position)
    {
        if (insertPositionIndicator != null)
        {
            Destroy(insertPositionIndicator);
        }
        
        insertPositionIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        insertPositionIndicator.name = "InsertPositionIndicator";
        insertPositionIndicator.transform.position = position + Vector3.up * 0.05f;
        insertPositionIndicator.transform.localScale = Vector3.one * 0.08f;
        
        Renderer rend = insertPositionIndicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0, 1, 0, 0.8f); // Bright green
        rend.material = mat;
        
        Collider col = insertPositionIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        // Make it pulse/glow
        StartCoroutine(PulseIndicator(insertPositionIndicator));
    }
    
    System.Collections.IEnumerator PulseIndicator(GameObject indicator)
    {
        if (indicator == null) yield break;
        
        Vector3 baseScale = indicator.transform.localScale;
        float time = 0f;
        
        while (indicator != null)
        {
            time += Time.deltaTime * 2f;
            float scale = 1f + Mathf.Sin(time) * 0.2f;
            indicator.transform.localScale = baseScale * scale;
            yield return null;
        }
    }
    
    void ShowInsertShiftIndicators(int insertIndex)
    {
        ClearShiftIndicators();
        ClearInsertPositionIndicator();
        
        LinkedListNode current = head;
        int index = 0;
        
        while (current != null)
        {
            if (index >= insertIndex)
            {
                Vector3 currentPos = current.position;
                Vector3 newPos = CalculateNodePosition(index + 1);
                
                // Create yellow sphere indicator at target position
                GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                indicator.transform.position = newPos + Vector3.up * 0.02f;
                indicator.transform.localScale = Vector3.one * 0.03f;
                
                Renderer rend = indicator.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = new Color(1, 1, 0, 0.9f);
                rend.material = mat;
                
                Collider col = indicator.GetComponent<Collider>();
                if (col != null) Destroy(col);
                
                shiftIndicators.Add(indicator);
                
                // Create arrow showing movement direction
                CreateShiftArrow(currentPos, newPos, Color.yellow, index);
                
                // Add label showing which node to move
                CreateShiftLabel(currentPos, $"Move [{index}]", Color.yellow);
            }
            
            current = current.next;
            index++;
        }
    }
    
    void CreateShiftArrow(Vector3 from, Vector3 to, Color color, int index)
    {
        GameObject arrow = new GameObject($"ShiftArrow_{index}");
        LineRenderer lr = arrow.AddComponent<LineRenderer>();
        
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;
        
        Vector3 start = from + Vector3.up * 0.08f;
        Vector3 end = to + Vector3.up * 0.08f;
        Vector3 direction = (end - start).normalized;
        Vector3 arrowBase = end - direction * 0.02f;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        
        lr.positionCount = 5;
        lr.SetPosition(0, start);
        lr.SetPosition(1, arrowBase);
        lr.SetPosition(2, arrowBase - perpendicular * 0.015f);
        lr.SetPosition(3, end);
        lr.SetPosition(4, arrowBase + perpendicular * 0.015f);
        
        shiftIndicators.Add(arrow);
    }
    
    void ClearShiftIndicators()
    {
        foreach (GameObject indicator in shiftIndicators)
        {
            if (indicator != null) Destroy(indicator);
        }
        shiftIndicators.Clear();
        ClearInsertPositionIndicator();
    }
    
    void ClearInsertPositionIndicator()
    {
        if (insertPositionIndicator != null)
        {
            Destroy(insertPositionIndicator);
            insertPositionIndicator = null;
        }
    }
    
    void CreateShiftLabel(Vector3 position, string text, Color color)
    {
        GameObject labelObj = new GameObject("ShiftLabel");
        labelObj.transform.position = position + Vector3.up * 0.3f;
        
        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = 50;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.01f;
        
        shiftIndicators.Add(labelObj);
    }
    
    void DestroyNode(LinkedListNode node)
    {
        if (node.nodeLabel != null) Destroy(node.nodeLabel);
        if (node.objectInstance != null) Destroy(node.objectInstance);
        if (node.pointerArrow != null) Destroy(node.pointerArrow);
    }
    
    public void DeleteFromHead()
    {
        if (currentState != ListState.ListReady || head == null) return;
        
        PlaySound(deleteSound);
        currentState = ListState.Animating;
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzeDeleteOperation(0, GetListSize());
        }
        
        LinkedListNode toDelete = head;
        head = head.next;
        
        if (currentMode == InteractionMode.Virtual)
        {
            StartCoroutine(AnimateRemoveNode(toDelete));
            StartCoroutine(ShiftNodesLeftWithRepositioning(0));
        }
        else
        {
            StartCoroutine(AnimateRemoveNodeAndFinish(toDelete));
        }
    }
    
    public void DeleteFromTail()
    {
        if (currentState != ListState.ListReady || head == null) return;
        
        PlaySound(deleteSound);
        currentState = ListState.Animating;
        
        int size = GetListSize();
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzeDeleteOperation(size - 1, size);
        }
        
        if (head.next == null)
        {
            StartCoroutine(AnimateRemoveNodeAndFinish(head));
            head = null;
            return;
        }
        
        LinkedListNode current = head;
        while (current.next.next != null)
        {
            current = current.next;
        }
        
        LinkedListNode toDelete = current.next;
        current.next = null;
        StartCoroutine(AnimateRemoveNodeAndFinish(toDelete));
    }
    
    public void DeleteFromMiddle()
    {
        if (currentState != ListState.ListReady || head == null) return;
        
        int size = GetListSize();
        int position = size / 2;
        
        if (position >= size) position = size - 1;
        
        PlaySound(deleteSound);
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzeDeleteOperation(position, size);
        }
        
        if (currentMode == InteractionMode.Physical)
        {
            // Show indicators for which nodes to shift LEFT
            ShowDeleteShiftIndicators(position);
            currentState = ListState.Animating;
            
            if (instructionText != null)
            {
                int numToMove = size - position - 1;
                if (numToMove > 0)
                {
                    instructionText.text = $"Remove QR at [{position}], move {numToMove} node(s) LEFT (follow arrows)";
                }
                else
                {
                    instructionText.text = $"Remove QR at [{position}]";
                }
            }
            
            // In physical mode, we wait for user to manually move and then they press a button
            // For now, let's auto-complete after 3 seconds for demo
            StartCoroutine(AutoCompleteDeleteMiddle(position));
        }
        else
        {
            // Virtual mode - immediate execution
            currentState = ListState.Animating;
            
            if (position == 0)
            {
                LinkedListNode toDelete = head;
                head = head.next;
                StartCoroutine(AnimateRemoveNode(toDelete));
                StartCoroutine(ShiftNodesLeftWithRepositioning(0));
            }
            else
            {
                LinkedListNode current = head;
                for (int i = 0; i < position - 1; i++)
                {
                    if (current.next != null)
                        current = current.next;
                }
                
                if (current.next != null)
                {
                    LinkedListNode toDelete = current.next;
                    current.next = toDelete.next;
                    StartCoroutine(AnimateRemoveNode(toDelete));
                    StartCoroutine(ShiftNodesLeftWithRepositioning(position));
                }
            }
        }
    }
    
    System.Collections.IEnumerator AutoCompleteDeleteMiddle(int position)
    {
        yield return new WaitForSeconds(3f);
        
        ClearShiftIndicators();
        
        if (position == 0)
        {
            LinkedListNode toDelete = head;
            head = head.next;
            StartCoroutine(AnimateRemoveNodeAndFinish(toDelete));
        }
        else
        {
            LinkedListNode current = head;
            for (int i = 0; i < position - 1; i++)
            {
                if (current.next != null)
                    current = current.next;
            }
            
            if (current.next != null)
            {
                LinkedListNode toDelete = current.next;
                current.next = toDelete.next;
                StartCoroutine(AnimateRemoveNodeAndFinish(toDelete));
            }
        }
    }
    
    void ShowDeleteShiftIndicators(int deleteIndex)
    {
        ClearShiftIndicators();
        
        LinkedListNode current = head;
        int index = 0;
        
        // Highlight the node to DELETE in RED
        while (current != null && index < deleteIndex)
        {
            current = current.next;
            index++;
        }
        
        if (current != null)
        {
            GameObject deleteMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            deleteMarker.transform.position = current.position + Vector3.up * 0.15f;
            deleteMarker.transform.localScale = Vector3.one * 0.06f;
            
            Renderer rend = deleteMarker.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(1, 0, 0, 0.9f); // Red
            rend.material = mat;
            
            Collider col = deleteMarker.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            shiftIndicators.Add(deleteMarker);
            CreateShiftLabel(current.position, "DELETE", Color.red);
        }
        
        // Show arrows for nodes that need to shift LEFT
        current = head;
        index = 0;
        
        while (current != null)
        {
            if (index > deleteIndex)
            {
                Vector3 currentPos = current.position;
                Vector3 newPos = CalculateNodePosition(index - 1);
                
                // Yellow sphere at target position
                GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                indicator.transform.position = newPos + Vector3.up * 0.02f;
                indicator.transform.localScale = Vector3.one * 0.03f;
                
                Renderer rend = indicator.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = new Color(1, 1, 0, 0.9f);
                rend.material = mat;
                
                Collider col = indicator.GetComponent<Collider>();
                if (col != null) Destroy(col);
                
                shiftIndicators.Add(indicator);
                
                // Arrow showing LEFT movement
                CreateShiftArrow(currentPos, newPos, Color.yellow, index);
                CreateShiftLabel(currentPos, $"Move [{index}]", Color.yellow);
            }
            
            current = current.next;
            index++;
        }
    }
    
    System.Collections.IEnumerator AnimateRemoveNodeAndFinish(LinkedListNode node)
    {
        yield return StartCoroutine(AnimateRemoveNode(node));
        
        UpdateAllPointers();
        UpdateAllLabels();
        UpdateStatusDisplay();
        currentState = ListState.ListReady;
    }
    
    System.Collections.IEnumerator ShiftNodesRight(int startIndex)
    {
        yield return new WaitForSeconds(0.1f);
        
        LinkedListNode current = head;
        int index = 0;
        
        while (current != null)
        {
            if (index >= startIndex)
            {
                Vector3 targetPos = CalculateNodePosition(index);
                StartCoroutine(MoveNode(current, targetPos));
            }
            
            current = current.next;
            index++;
        }
        
        yield return new WaitForSeconds(animationDuration);
        UpdateAllPointers();
        UpdateAllLabels();
        UpdateStatusDisplay();
        currentState = ListState.ListReady;
    }
    
    System.Collections.IEnumerator ShiftNodesLeft(int startIndex)
    {
        yield return new WaitForSeconds(0.1f);
        
        LinkedListNode current = head;
        int index = 0;
        
        while (current != null)
        {
            if (index >= startIndex)
            {
                Vector3 targetPos = CalculateNodePosition(index);
                StartCoroutine(MoveNode(current, targetPos));
            }
            
            current = current.next;
            index++;
        }
        
        yield return new WaitForSeconds(animationDuration);
        UpdateAllPointers();
        UpdateAllLabels();
        UpdateStatusDisplay();
        currentState = ListState.ListReady;
    }
    
    System.Collections.IEnumerator MoveNode(LinkedListNode node, Vector3 targetPos)
    {
        Vector3 startPos = node.position;
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, t);
            node.position = newPos;
            
            if (node.objectInstance != null)
            {
                node.objectInstance.transform.position = newPos;
            }
            
            if (node.nodeLabel != null)
            {
                node.nodeLabel.transform.position = newPos + Vector3.up * 0.25f;
            }
            
            yield return null;
        }
        
        node.position = targetPos;
        if (node.objectInstance != null)
        {
            node.objectInstance.transform.position = targetPos;
        }
        if (node.nodeLabel != null)
        {
            node.nodeLabel.transform.position = targetPos + Vector3.up * 0.25f;
        }
    }
    
    System.Collections.IEnumerator AnimateAddNode(LinkedListNode node, Vector3 targetPos)
    {
        if (node.objectInstance == null) yield break;
        
        Vector3 startPos = targetPos + Vector3.up * 0.3f;
        node.objectInstance.transform.position = startPos;
        node.objectInstance.transform.localScale = Vector3.zero;
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - elapsed / animationDuration, 3f);
            
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, t);
            node.position = newPos;
            node.objectInstance.transform.position = newPos;
            node.objectInstance.transform.localScale = GetScaleForObject(node.objectName) * t;
            
            if (node.nodeLabel != null)
            {
                node.nodeLabel.transform.position = newPos + Vector3.up * 0.25f;
            }
            
            yield return null;
        }
        
        node.position = targetPos;
        node.objectInstance.transform.position = targetPos;
        node.objectInstance.transform.localScale = GetScaleForObject(node.objectName);
        
        if (node.nodeLabel != null)
        {
            node.nodeLabel.transform.position = targetPos + Vector3.up * 0.25f;
        }
        
        UpdateAllPointers();
        UpdateAllLabels();
        UpdateStatusDisplay();
        currentState = ListState.ListReady;
    }
    
    System.Collections.IEnumerator AnimateRemoveNode(LinkedListNode node)
    {
        if (node.objectInstance == null) yield break;
        
        float elapsed = 0f;
        Vector3 startPos = node.position;
        Vector3 startScale = node.objectInstance.transform.localScale;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            node.objectInstance.transform.position = startPos + Vector3.up * (0.3f * t);
            node.objectInstance.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
        
        DestroyNode(node);
    }
    
    Vector3 CalculateNodePosition(int index)
    {
        return virtualListStartPosition + listDirection * (nodeSpacing * index);
    }
    
    Vector3 CalculateInsertPosition(int position)
    {
        if (position == 0)
        {
            return virtualListStartPosition;
        }
        else if (position == -1)
        {
            int size = GetListSize();
            return virtualListStartPosition + listDirection * (nodeSpacing * size);
        }
        else
        {
            return virtualListStartPosition + listDirection * (nodeSpacing * position);
        }
    }
    
    void UpdateTrackedNode(ARTrackedImage trackedImage)
    {
        if (currentMode != InteractionMode.Physical) return;
        
        LinkedListNode current = head;
        while (current != null)
        {
            if (current.trackedImage == trackedImage && current.isConfirmed && !current.isVirtual)
            {
                current.position = trackedImage.transform.position;
                
                if (current.nodeLabel != null)
                {
                    current.nodeLabel.transform.position = trackedImage.transform.position + Vector3.up * 0.25f;
                }
                
                break;
            }
            current = current.next;
        }
        
        UpdateAllPointers();
    }
    
    void UpdateAllPointers()
    {
        LinkedListNode current = head;
        while (current != null)
        {
            if (current.pointerArrow != null)
            {
                Destroy(current.pointerArrow);
                current.pointerArrow = null;
            }
            
            if (current.next != null)
            {
                current.pointerArrow = CreatePointerArrow(current.position, current.next.position);
            }
            
            current = current.next;
        }
    }
    
    void UpdateAllLabels()
    {
        // Find head and tail
        LinkedListNode tail = null;
        LinkedListNode current = head;
        int index = 0;
        
        while (current != null)
        {
            if (current.next == null)
                tail = current;
            current = current.next;
        }
        
        // Update all node labels - just show index numbers
        current = head;
        index = 0;
        
        while (current != null)
        {
            if (current.nodeLabel != null)
            {
                TextMeshPro labelText = current.nodeLabel.GetComponentInChildren<TextMeshPro>();
                if (labelText != null)
                {
                    // Just show the index number
                    labelText.text = $"[{index}]";
                    
                    // Color coding based on position
                    if (current == head && current == tail)
                    {
                        labelText.color = new Color(0.7f, 0.4f, 0.7f); // Purple-gold mix (single node)
                    }
                    else if (current == head)
                    {
                        labelText.color = headNodeColor; // Gold for head
                    }
                    else if (current == tail)
                    {
                        labelText.color = tailNodeColor; // Purple for tail
                    }
                    else
                    {
                        labelText.color = regularNodeColor; // Cyan for middle
                    }
                }
            }
            
            current = current.next;
            index++;
        }
    }
    
    GameObject CreatePointerArrow(Vector3 from, Vector3 to)
    {
        GameObject arrow = new GameObject("PointerArrow");
        LineRenderer lr = arrow.AddComponent<LineRenderer>();
        
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.yellow;
        lr.endColor = Color.yellow;
        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;
        
        Vector3 start = from + Vector3.up * 0.08f;
        Vector3 end = to + Vector3.up * 0.08f;
        Vector3 direction = (end - start).normalized;
        Vector3 arrowBase = end - direction * 0.02f;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        
        lr.positionCount = 5;
        lr.SetPosition(0, start);
        lr.SetPosition(1, arrowBase);
        lr.SetPosition(2, arrowBase - perpendicular * 0.015f);
        lr.SetPosition(3, end);
        lr.SetPosition(4, arrowBase + perpendicular * 0.015f);
        
        return arrow;
    }
    
    void MakeOpaque(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Color color = rend.material.color;
            color.a = 1f;
            rend.material.color = color;
        }
    }
    
    GameObject InstantiateObject(string objectName, ARTrackedImage trackedImage)
    {
        GameObject obj = null;
        Vector3 customScale = Vector3.one * 0.05f;
        
        if (objectPrefabs.ContainsKey(objectName))
        {
            obj = Instantiate(objectPrefabs[objectName]);
            customScale = GetScaleForObject(objectName);
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            customScale = Vector3.one * 0.05f;
        }
        
        obj.transform.SetParent(trackedImage.transform);
        obj.transform.localPosition = Vector3.up * 0.08f;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = customScale;
        
        ObjectAnimator animator = obj.GetComponent<ObjectAnimator>();
        if (animator == null)
        {
            obj.AddComponent<ObjectAnimator>();
        }
        
        return obj;
    }
    
    GameObject InstantiateObjectVirtual(string objectName, Vector3 position)
    {
        GameObject obj = null;
        Vector3 customScale = Vector3.one * 0.05f;
        
        if (objectPrefabs.ContainsKey(objectName))
        {
            obj = Instantiate(objectPrefabs[objectName]);
            customScale = GetScaleForObject(objectName);
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            customScale = Vector3.one * 0.05f;
        }
        
        obj.name = $"VirtualNode_{objectName}_{nodeIdCounter}";
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = customScale;
        
        // Disable ObjectAnimator for virtual nodes to prevent position conflicts
        ObjectAnimator animator = obj.GetComponent<ObjectAnimator>();
        if (animator != null)
        {
            animator.enabled = false;
        }
        
        return obj;
    }
    
    Vector3 GetScaleForObject(string objectName)
    {
        switch (objectName.ToLower())
        {
            case "cube": return Vector3.one * 0.03f;
            case "coin": return Vector3.one * 0.08f;
            case "chair": return Vector3.one * 0.06f;
            case "pen": return Vector3.one * 0.08f;
            case "book": return Vector3.one * 0.08f;
            default: return Vector3.one * 0.05f;
        }
    }
    
    public void SimulateTraversal()
    {
        if (head == null) return;
        StartCoroutine(TraversalAnimation());
    }
    
    System.Collections.IEnumerator TraversalAnimation()
    {
        if (instructionText != null)
            instructionText.text = "Traversing list...";
        
        int position = 0;
        LinkedListNode current = head;
        
        while (current != null)
        {
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            highlight.transform.position = current.position + Vector3.up * 0.12f;
            highlight.transform.localScale = Vector3.one * 0.06f;
            
            Renderer rend = highlight.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(1, 1, 0, 0.9f);
            rend.material = mat;
            
            Collider col = highlight.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            yield return new WaitForSeconds(0.5f);
            Destroy(highlight);
            
            current = current.next;
            position++;
        }
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzeTraversalOperation(GetListSize());
        }
        
        if (instructionText != null)
            instructionText.text = "Traversal complete!";
    }
    
    public void SimulateSearch()
    {
        if (head == null) return;
        StartCoroutine(SearchAnimation());
    }
    
    System.Collections.IEnumerator SearchAnimation()
    {
        if (instructionText != null)
            instructionText.text = "Searching list...";
        
        int size = GetListSize();
        int searchForPosition = Random.Range(0, size);
        bool found = false;
        int foundAtPosition = -1;
        
        int position = 0;
        LinkedListNode current = head;
        
        while (current != null)
        {
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            highlight.transform.position = current.position + Vector3.up * 0.12f;
            highlight.transform.localScale = Vector3.one * 0.06f;
            
            Renderer rend = highlight.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Unlit/Color"));
            
            if (position == searchForPosition)
            {
                found = true;
                foundAtPosition = position;
                mat.color = new Color(0, 1, 0, 0.9f);
                
                if (instructionText != null)
                    instructionText.text = $"Found at position {position}!";
            }
            else
            {
                mat.color = new Color(1, 1, 0, 0.7f);
            }
            
            rend.material = mat;
            
            Collider col = highlight.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            yield return new WaitForSeconds(0.5f);
            
            if (!found)
            {
                Destroy(highlight);
            }
            else
            {
                yield return new WaitForSeconds(1f);
                Destroy(highlight);
                break;
            }
            
            current = current.next;
            position++;
        }
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzeSearchOperation(size, found, foundAtPosition);
        }
        
        if (instructionText != null)
            instructionText.text = found ? $"Search complete! Found at position {foundAtPosition}" : "Search complete! Not found";
    }
    
    void UpdateInstructions()
    {
        if (instructionText == null) return;
        
        switch (currentState)
        {
            case ListState.ModeSelection:
                instructionText.text = "Select interaction mode";
                break;
                
            case ListState.WaitingForFirstNode:
                if (currentMode == InteractionMode.Physical)
                    instructionText.text = "Scan FIRST node's QR code";
                else
                    instructionText.text = "Scan ONE QR to define all nodes";
                break;
                
            case ListState.WaitingToConfirmFirst:
                instructionText.text = "TAP to confirm HEAD node";
                break;
                
            case ListState.WaitingForInsertShift:
                instructionText.text = "Move nodes as shown, then press CONFIRM";
                break;
                
            case ListState.WaitingForInsertNode:
                instructionText.text = "Scan QR for new node";
                break;
                
            case ListState.ListReady:
                instructionText.text = "List ready! Use buttons";
                break;
        }
    }
    
    void UpdateStatusDisplay()
    {
        if (statusText != null)
            statusText.text = $"Nodes: {GetListSize()}";
    }
    
    public void ResetList()
    {
        if (currentPreviewObject != null)
        {
            Destroy(currentPreviewObject);
            currentPreviewObject = null;
        }
        
        ClearShiftIndicators();
        
        LinkedListNode current = head;
        while (current != null)
        {
            LinkedListNode next = current.next;
            DestroyNode(current);
            current = next;
        }
        
        head = null;
        trackedQRCodes.Clear();
        processedImages.Clear();
        
        pendingFirstImage = null;
        pendingInsertImage = null;
        
        virtualObjectType = "";
        virtualListInitialized = false;
        nodeIdCounter = 1;
        currentState = ListState.ModeSelection;
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Choose a mode";
            detectionFeedbackText.color = Color.yellow;
        }
        
        if (statusText != null)
            statusText.text = "Nodes: 0";
        
        UpdateInstructions();
        
        if (analysisManager != null)
        {
            analysisManager.ResetCounters();
        }
        
        Debug.Log("Linked List reset");
    }
    
    public int GetListSize()
    {
        int count = 0;
        LinkedListNode current = head;
        while (current != null)
        {
            count++;
            current = current.next;
        }
        return count;
    }
    
    public bool IsListReady()
    {
        return currentState == ListState.ListReady;
    }
    
    public bool IsModeSelected()
    {
        return currentState != ListState.ModeSelection;
    }
    
    public bool IsWaitingForShiftConfirmation()
    {
        return currentState == ListState.WaitingForInsertShift;
    }
    
    public InteractionMode GetCurrentMode()
    {
        return currentMode;
    }
    
    public string GetVirtualObjectType()
    {
        return virtualObjectType;
    }
    
    public string GetNodeValue(int position)
    {
        if (position < 0) return "Invalid";
        
        LinkedListNode current = head;
        int index = 0;
        
        while (current != null && index < position)
        {
            current = current.next;
            index++;
        }
        
        if (current != null)
        {
            return current.nodeId.ToString();
        }
        
        return "Invalid";
    }
}