using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class QRArrayModeManager : MonoBehaviour
{
    public enum InteractionMode
    {
        Physical,  // Move QR codes physically in real world
        Virtual    // Scan QR once, manipulate virtually in app
    }
    
    [Header("Mode Settings")]
    public InteractionMode currentMode = InteractionMode.Physical;
    
    [Header("Array Settings")]
    public int maxCapacity = 8;
    
    [Header("AR References")]
    public ARTrackedImageManager trackedImageManager;
    
    [Header("3D Object Prefabs")]
    public GameObject cubePrefab;
    public GameObject chairPrefab;
    public GameObject coinPrefab;
    public GameObject penPrefab;
    public GameObject bookPrefab;
    
    [Header("Virtual Label Prefabs")]
    public GameObject indexLabelPrefab;

    [Header("Algorithm Analysis")]
    public AlgorithmAnalysisManager analysisManager;

    
    [Header("UI References")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI detectionFeedbackText;
    public TextMeshProUGUI modeIndicatorText;
    
    [Header("Colors")]
    public Color detectingColor = Color.green;
    public Color notDetectingColor = Color.red;
    public Color containerLineColor = new Color(0, 0.8f, 1f, 0.8f);
    
    [Header("Audio")]
    public AudioClip scanSound;
    public AudioClip confirmSound;
    public AudioClip insertSound;
    public AudioClip deleteSound;
    public AudioClip accessSound;
    public AudioClip errorSound;
    private AudioSource audioSource;
    
    private enum ArrayState
    {
        ModeSelection,
        WaitingForFirstObject,
        WaitingToConfirmFirst,
        WaitingForSecondObject,
        WaitingToConfirmSecond,
        ArrayReady,
        WaitingForShiftConfirmation,
        WaitingForInsertObject,
        WaitingToConfirmInsert,
        WaitingForDeleteSelection,
        WaitingForDeleteShiftConfirmation,
        WaitingForAccessSelection
    }
    
    private ArrayState currentState = ArrayState.ModeSelection;
    
    private class ArrayElement
    {
        public Vector3 position;
        public GameObject indexLabel;
        public GameObject objectInstance;
        public int index;
        public string objectName;
        public ARTrackedImage trackedImage;
        public bool isConfirmed;
        public ObjectAnimator animator;
        public bool isVirtual;
    }
    
    private List<ArrayElement> arrayElements = new List<ArrayElement>();
    private Dictionary<string, ARTrackedImage> trackedQRCodes = new Dictionary<string, ARTrackedImage>();
    private Dictionary<string, GameObject> objectPrefabs = new Dictionary<string, GameObject>();
    private HashSet<string> processedImages = new HashSet<string>();
    
    // Virtual mode specific
    private string virtualObjectType = ""; // Store the scanned object type
    private Vector3 virtualArrayStartPosition;
    private bool virtualArrayInitialized = false;
    
    private GameObject leftBoundary;
    private GameObject rightBoundary;
    private GameObject containerTop;
    private GameObject containerBottom;
    
    private Vector3 firstObjectPosition;
    private Vector3 secondObjectPosition;
    private float elementSpacing = 0.15f;
    private Vector3 arrayDirection;
    
    private int insertAtIndex = -1;
    private int deleteAtIndex = -1;
    private GameObject insertIndicator;
    private List<GameObject> shiftIndicators = new List<GameObject>();
    private Vector3 expectedInsertPosition;
    private string pendingInsertObjectName = "";
    
    private ARTrackedImage pendingFirstImage;
    private ARTrackedImage pendingSecondImage;
    private ARTrackedImage pendingInsertImage;
    
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
        
        Debug.Log($"QR Array Manager Initialized - Mode: {currentMode}");
    }
    
    public void SetPhysicalMode()
    {
        currentMode = InteractionMode.Physical;
        currentState = ArrayState.WaitingForFirstObject;
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
        currentState = ArrayState.WaitingForFirstObject;
        UpdateModeIndicator();
        UpdateInstructions();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Virtual Mode: Scan ONE QR to define nodes";
            detectionFeedbackText.color = notDetectingColor;
        }
        
        Debug.Log("Switched to VIRTUAL mode - Single scan system");
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
            case ArrayState.WaitingToConfirmFirst:
                if (currentMode == InteractionMode.Physical)
                {
                    if (pendingFirstImage != null && TapNearTrackedImage(screenPosition, pendingFirstImage))
                    {
                        ConfirmFirstElement();
                    }
                }
                else
                {
                    // Virtual mode: confirm by tapping anywhere
                    ConfirmFirstElement();
                }
                break;
                
            case ArrayState.WaitingToConfirmSecond:
                if (currentMode == InteractionMode.Physical)
                {
                    if (pendingSecondImage != null && TapNearTrackedImage(screenPosition, pendingSecondImage))
                    {
                        ConfirmSecondElement();
                    }
                }
                else
                {
                    ConfirmSecondElement();
                }
                break;
                
            case ArrayState.WaitingToConfirmInsert:
                if (currentMode == InteractionMode.Physical)
                {
                    if (pendingInsertImage != null && TapNearTrackedImage(screenPosition, pendingInsertImage))
                    {
                        ConfirmInsertElement();
                    }
                }
                else
                {
                    ConfirmInsertElement();
                }
                break;
                
            case ArrayState.WaitingForDeleteSelection:
            case ArrayState.WaitingForAccessSelection:
                HandleElementTapSelection(screenPosition);
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
    
    void HandleElementTapSelection(Vector2 screenPosition)
    {
        Camera arCamera = Camera.main;
        if (arCamera == null) return;
        
        float minDist = float.MaxValue;
        int closestIndex = -1;
        
        for (int i = 0; i < arrayElements.Count; i++)
        {
            Vector3 screenPoint = arCamera.WorldToScreenPoint(arrayElements[i].position);
            float distance = Vector2.Distance(screenPosition, new Vector2(screenPoint.x, screenPoint.y));
            
            if (distance < minDist)
            {
                minDist = distance;
                closestIndex = i;
            }
        }
        
        float threshold = Screen.width * 0.15f;
        
        if (closestIndex >= 0 && minDist < threshold)
        {
            if (currentState == ArrayState.WaitingForDeleteSelection)
            {
                HandleDeleteSelection(closestIndex);
            }
            else if (currentState == ArrayState.WaitingForAccessSelection)
            {
                HandleAccessSelection(closestIndex);
            }
        }
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
            else if (currentMode == InteractionMode.Physical)
            {
                UpdateTrackedObject(trackedImage);
            }
        }
    }
    
    void OnImageDetected(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name.ToLower();
        Vector3 imagePosition = trackedImage.transform.position;
        
        // In virtual mode, only process the FIRST scan to set the object type
        if (currentMode == InteractionMode.Virtual)
        {
            if (currentState == ArrayState.WaitingForFirstObject && string.IsNullOrEmpty(virtualObjectType))
            {
                // This is the first scan - accept it
            }
            else if (currentState == ArrayState.WaitingForFirstObject)
            {
                // Already have an object type, ignore additional scans during setup
                Debug.Log($"Virtual mode: Already using {virtualObjectType}. Ignoring {imageName}");
                return;
            }
            else
            {
                // After setup, we don't need any scans for insertions in virtual mode
                return;
            }
        }
        
        bool shouldProcess = false;
        
        switch (currentState)
        {
            case ArrayState.WaitingForFirstObject:
            case ArrayState.WaitingForSecondObject:
                shouldProcess = true;
                break;
                
            case ArrayState.WaitingForInsertObject:
                shouldProcess = (currentMode == InteractionMode.Physical);
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
        
        // Physical mode: check if already in array (BUT NOT during insert - allow duplicate types)
        if (currentMode == InteractionMode.Physical && currentState != ArrayState.WaitingForInsertObject)
        {
            bool alreadyInArray = false;
            foreach (var element in arrayElements)
            {
                if (element.objectName.ToLower() == imageName)
                {
                    alreadyInArray = true;
                    break;
                }
            }
            
            if (alreadyInArray)
            {
                Debug.Log($"{imageName} already in array");
                PlaySound(errorSound);
                return;
            }
        }
        
        // SPECIAL HANDLING for Insert state
        if (currentState == ArrayState.WaitingForInsertObject)
        {
            Debug.Log($"INSERT MODE: Detected {imageName} for insertion at position {imagePosition}");
            PlaySound(scanSound);
            
            if (detectionFeedbackText != null)
            {
                detectionFeedbackText.text = $"Detected: {imageName.ToUpper()}";
                detectionFeedbackText.color = detectingColor;
            }
            
            // Don't add to processedImages - allow scanning same type multiple times
            if (!trackedQRCodes.ContainsKey(imageName))
            {
                trackedQRCodes[imageName] = trackedImage;
            }
            
            ShowInsertObjectPreview(imageName, trackedImage);
            return;
        }
        
        // Normal setup processing
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
        
        switch (currentState)
        {
            case ArrayState.WaitingForFirstObject:
                ShowFirstObjectPreview(imageName, trackedImage);
                break;
                
            case ArrayState.WaitingForSecondObject:
                ShowSecondObjectPreview(imageName, trackedImage);
                break;
        }
    }
    
    void ShowFirstObjectPreview(string objectName, ARTrackedImage trackedImage)
    {
        pendingFirstImage = trackedImage;
        
        GameObject previewObj;
        
        if (currentMode == InteractionMode.Physical)
        {
            previewObj = InstantiateObject(objectName, trackedImage);
        }
        else
        {
            // Virtual mode: set the object type and create first element
            virtualObjectType = objectName;
            virtualArrayStartPosition = trackedImage.transform.position;
            previewObj = InstantiateObjectVirtual(objectName, virtualArrayStartPosition);
            
            Debug.Log($"Virtual mode: Set object type to {virtualObjectType}");
        }
        
        Renderer[] renderers = previewObj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Color color = rend.material.color;
            color.a = 0.7f;
            rend.material.color = color;
        }
        
        currentState = ArrayState.WaitingToConfirmFirst;
        
        if (instructionText != null)
        {
            if (currentMode == InteractionMode.Physical)
            {
                instructionText.text = $"Tap the {objectName.ToUpper()} QR to confirm";
            }
            else
            {
                instructionText.text = $"Tap ANYWHERE to confirm {objectName.ToUpper()} as your array nodes";
            }
        }
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Tap to confirm";
            detectionFeedbackText.color = Color.yellow;
        }
    }

    void ConfirmFirstElement()
    {
        if (pendingFirstImage == null) return;
        
        string objectName;
        
        if (currentMode == InteractionMode.Physical)
        {
            objectName = pendingFirstImage.referenceImage.name.ToLower();
        }
        else
        {
            objectName = virtualObjectType; // Use the stored type
        }
        
        PlaySound(confirmSound);
        
        GameObject existingObject = null;
        
        if (currentMode == InteractionMode.Physical)
        {
            Transform previewTransform = pendingFirstImage.transform;
            foreach (Transform child in previewTransform)
            {
                existingObject = child.gameObject;
                MakeOpaque(existingObject);
                break;
            }
            firstObjectPosition = pendingFirstImage.transform.position;
        }
        else
        {
            existingObject = GameObject.Find($"VirtualPreview_{objectName}");
            if (existingObject != null)
            {
                MakeOpaque(existingObject);
            }
            firstObjectPosition = virtualArrayStartPosition;
        }
        
        CreateArrayElement(pendingFirstImage, 0, objectName, true, existingObject);
        
        if (currentMode == InteractionMode.Virtual)
        {
            // Virtual mode: automatically create second element
            secondObjectPosition = firstObjectPosition + Vector3.right * elementSpacing;
            arrayDirection = Vector3.right;
            
            GameObject secondObj = InstantiateObjectVirtual(virtualObjectType, secondObjectPosition);
            CreateArrayElement(null, 1, virtualObjectType, true, secondObj);
            
            virtualArrayInitialized = true;
            CreateArrayContainer();
            
            currentState = ArrayState.ArrayReady;
            
            if (detectionFeedbackText != null)
            {
                detectionFeedbackText.text = $"Array Ready! Using {virtualObjectType.ToUpper()} nodes";
                detectionFeedbackText.color = detectingColor;
            }
            
            Debug.Log($"Virtual array initialized with {virtualObjectType}");
        }
        else
        {
            currentState = ArrayState.WaitingForSecondObject;
            
            if (detectionFeedbackText != null)
            {
                detectionFeedbackText.text = "Scan second object";
                detectionFeedbackText.color = notDetectingColor;
            }
        }
        
        UpdateInstructions();
        pendingFirstImage = null;
    }
    
    void ShowSecondObjectPreview(string objectName, ARTrackedImage trackedImage)
    {
        pendingSecondImage = trackedImage;
        
        GameObject previewObj;
        
        if (currentMode == InteractionMode.Physical)
        {
            previewObj = InstantiateObject(objectName, trackedImage);
        }
        else
        {
            // This shouldn't happen in virtual mode
            Vector3 secondPos = firstObjectPosition + Vector3.right * elementSpacing;
            previewObj = InstantiateObjectVirtual(objectName, secondPos);
        }
        
        Renderer[] renderers = previewObj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Color color = rend.material.color;
            color.a = 0.7f;
            rend.material.color = color;
        }
        
        currentState = ArrayState.WaitingToConfirmSecond;
        
        if (instructionText != null)
        {
            if (currentMode == InteractionMode.Physical)
            {
                instructionText.text = $"Tap the {objectName.ToUpper()} QR to confirm";
            }
            else
            {
                instructionText.text = $"Tap ANYWHERE to confirm {objectName.ToUpper()}";
            }
        }
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Tap to confirm";
            detectionFeedbackText.color = Color.yellow;
        }
    }

    void ConfirmSecondElement()
    {
        if (currentMode == InteractionMode.Physical && pendingSecondImage == null) return;
        
        string objectName;
        
        if (currentMode == InteractionMode.Physical)
        {
            objectName = pendingSecondImage.referenceImage.name.ToLower();
        }
        else
        {
            objectName = virtualObjectType;
        }
        
        PlaySound(confirmSound);
        
        GameObject existingObject = null;
        
        if (currentMode == InteractionMode.Physical)
        {
            Transform previewTransform = pendingSecondImage.transform;
            foreach (Transform child in previewTransform)
            {
                existingObject = child.gameObject;
                MakeOpaque(existingObject);
                break;
            }
            
            Vector3 currentFirstPos = arrayElements[0].trackedImage.transform.position;
            Vector3 currentSecondPos = pendingSecondImage.transform.position;
            
            secondObjectPosition = currentSecondPos;
            arrayDirection = (currentSecondPos - currentFirstPos).normalized;
            elementSpacing = Vector3.Distance(currentFirstPos, currentSecondPos);
            
            CreateArrayElement(pendingSecondImage, 1, objectName, true, existingObject);
        }
        
        CreateArrayContainer();
        
        currentState = ArrayState.ArrayReady;
        UpdateInstructions();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Array Ready!";
        }
        
        Debug.Log($"Array initialized - Mode: {currentMode}");
        pendingSecondImage = null;
    }

    void ShowInsertObjectPreview(string objectName, ARTrackedImage trackedImage)
    {
        Vector3 position = trackedImage.transform.position;
        float distance = Vector3.Distance(position, expectedInsertPosition);
        
        Debug.Log($"ShowInsertObjectPreview: {objectName} at {position}, distance to target: {distance:F3}m");
        
        // Create the preview object FIRST - always make it visible
        GameObject previewObj = InstantiateObject(objectName, trackedImage);
        
        if (previewObj == null)
        {
            Debug.LogError("Failed to create preview object!");
            return;
        }
        
        // Store the pending info immediately
        pendingInsertImage = trackedImage;
        pendingInsertObjectName = objectName;
        
        // Apply transparency based on distance
        float alpha = 0.7f;
        Color feedbackColor = Color.yellow;
        string instruction = $"Tap the {objectName.ToUpper()} QR to insert";
        
        if (distance > elementSpacing * 0.7f)
        {
            Debug.LogWarning($"Object far from expected position. Distance: {distance:F3}m, Threshold: {elementSpacing * 0.7f:F3}m");
            alpha = 0.5f; // More transparent when far
            feedbackColor = new Color(1f, 0.5f, 0f); // Orange warning
            instruction = $"Move QR closer to GREEN spot\nDistance: {distance:F2}m";
        }
        
        // Apply transparency
        Renderer[] renderers = previewObj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Color color = rend.material.color;
            color.a = alpha;
            rend.material.color = color;
        }
        
        // Always transition to confirm state
        currentState = ArrayState.WaitingToConfirmInsert;
        
        if (instructionText != null)
        {
            instructionText.text = instruction;
        }
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = distance > elementSpacing * 0.7f ? "Too far" : "Tap to confirm";
            detectionFeedbackText.color = feedbackColor;
        }
        
        Debug.Log($"Insert preview created for {objectName}. Waiting for confirmation.");
    }
    
    void ConfirmInsertElement()
    {
        PlaySound(insertSound);
        
        GameObject existingObject = null;
        Vector3 position;
        string objectName;
        
        if (currentMode == InteractionMode.Physical)
        {
            if (pendingInsertImage == null) return;
            
            Transform previewTransform = pendingInsertImage.transform;
            foreach (Transform child in previewTransform)
            {
                existingObject = child.gameObject;
                MakeOpaque(existingObject);
                break;
            }
            position = pendingInsertImage.transform.position;
            objectName = pendingInsertObjectName;
        }
        else
        {
            // Virtual mode: use stored object type
            objectName = virtualObjectType;
            position = expectedInsertPosition;
            existingObject = InstantiateObjectVirtual(virtualObjectType, position);
        }
        
        if (existingObject != null)
        {
            ObjectAnimator animator = existingObject.GetComponent<ObjectAnimator>();
            if (animator == null)
            {
                animator = existingObject.AddComponent<ObjectAnimator>();
            }
            
            ArrayElement newElement = new ArrayElement
            {
                position = position,
                index = insertAtIndex,
                objectName = objectName,
                objectInstance = existingObject,
                trackedImage = (currentMode == InteractionMode.Physical) ? pendingInsertImage : null,
                isConfirmed = true,
                animator = animator,
                isVirtual = (currentMode == InteractionMode.Virtual)
            };
            
            if (indexLabelPrefab != null)
            {
                newElement.indexLabel = Instantiate(indexLabelPrefab);
                newElement.indexLabel.transform.position = position + Vector3.up * 0.2f;
                
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
            
            arrayElements.Insert(insertAtIndex, newElement);
        }

        if (analysisManager != null)
        {
            analysisManager.AnalyzeInsertOperation(insertAtIndex, arrayElements.Count - 1);
        }
        
        if (insertIndicator != null)
        {
            Destroy(insertIndicator);
            insertIndicator = null;
        }
        
        if (currentMode == InteractionMode.Virtual)
        {
            UpdateVirtualPositions();
        }
        
        UpdateAllIndices();
        UpdateArrayContainer();
        
        if (statusText != null)
            statusText.text = $"Elements: {arrayElements.Count}";
        
        currentState = ArrayState.ArrayReady;
        if (instructionText != null)
            instructionText.text = $"Inserted {objectName.ToUpper()} at [{insertAtIndex}]!";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Insert confirmed!";
            detectionFeedbackText.color = detectingColor;
        }
        
        insertAtIndex = -1;
        pendingInsertObjectName = "";
        pendingInsertImage = null;
    }
    
    void UpdateTrackedObject(ARTrackedImage trackedImage)
    {
        if (currentMode != InteractionMode.Physical) return;
        
        foreach (var element in arrayElements)
        {
            if (element.trackedImage == trackedImage && element.isConfirmed && !element.isVirtual)
            {
                element.position = trackedImage.transform.position;
                
                if (element.indexLabel != null)
                {
                    element.indexLabel.transform.position = trackedImage.transform.position + Vector3.up * 0.2f;
                }
                
                break;
            }
        }
    }
    
    void UpdateVirtualPositions()
    {
        for (int i = 0; i < arrayElements.Count; i++)
        {
            if (arrayElements[i].isVirtual && arrayElements[i].objectInstance != null)
            {
                Vector3 newPos = CalculatePositionAtIndex(i);
                arrayElements[i].position = newPos;
                arrayElements[i].objectInstance.transform.position = newPos + Vector3.up * 0.08f;
                
                // IMPORTANT: Update animator's reference position so it doesn't snap back
                if (arrayElements[i].animator != null)
                {
                    arrayElements[i].animator.UpdateStartPosition();
                }
                
                if (arrayElements[i].indexLabel != null)
                {
                    arrayElements[i].indexLabel.transform.position = newPos + Vector3.up * 0.2f;
                }
            }
        }
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
        obj.AddComponent<ObjectAnimator>();
        
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
        
        obj.name = $"VirtualPreview_{objectName}";
        obj.transform.position = position + Vector3.up * 0.08f;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = customScale;
        obj.AddComponent<ObjectAnimator>();
        
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
    
    void HandleDeleteSelection(int index)
    {
        deleteAtIndex = index;
        PlaySound(deleteSound);
        
        if (index < arrayElements.Count - 1)
        {
            if (currentMode == InteractionMode.Virtual)
            {
                ArrayElement element = arrayElements[index];
                if (element.indexLabel != null) Destroy(element.indexLabel);
                if (element.objectInstance != null) Destroy(element.objectInstance);
                
                arrayElements.RemoveAt(index);
                UpdateVirtualPositions();
                UpdateAllIndices();
                UpdateArrayContainer();
                
                currentState = ArrayState.ArrayReady;
                if (instructionText != null)
                    instructionText.text = "Deleted! Array updated automatically";
            }
            else
            {
                ShowDeleteShiftIndicators(index);
                currentState = ArrayState.WaitingForDeleteShiftConfirmation;
                
                if (instructionText != null)
                {
                    int numToMove = arrayElements.Count - index - 1;
                    instructionText.text = $"Move {numToMove} object(s) LEFT, then CONFIRM";
                }
            }
        }
        else
        {
            ArrayElement element = arrayElements[index];
            if (element.indexLabel != null) Destroy(element.indexLabel);
            if (element.objectInstance != null) Destroy(element.objectInstance);
            
            arrayElements.RemoveAt(index);
            UpdateAllIndices();
            UpdateArrayContainer();
            
            currentState = ArrayState.ArrayReady;
            if (instructionText != null)
                instructionText.text = "Deleted!";
        }
        
        if (statusText != null)
            statusText.text = $"Elements: {arrayElements.Count}";
    }
    
    void HandleAccessSelection(int index)
    {
        PlaySound(accessSound);
        
        if (instructionText != null)
            instructionText.text = $"Accessed [{index}]: {arrayElements[index].objectName.ToUpper()}";
        
        StartCoroutine(HighlightElement(index));
        currentState = ArrayState.ArrayReady;
    }
    
    System.Collections.IEnumerator HighlightElement(int index)
    {
        if (index >= 0 && index < arrayElements.Count)
        {
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            highlight.transform.position = arrayElements[index].position + Vector3.up * 0.12f;
            highlight.transform.localScale = Vector3.one * 0.08f;
            
            Renderer rend = highlight.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0, 1, 1, 0.7f);
            rend.material = mat;
            
            Collider col = highlight.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            yield return new WaitForSeconds(1f);
            Destroy(highlight);
        }
    }
    
    void CreateArrayElement(ARTrackedImage trackedImage, int index, string objectName, bool confirmed, GameObject existingObject = null)
    {
        Vector3 position;
        
        if (currentMode == InteractionMode.Physical)
        {
            position = trackedImage.transform.position;
        }
        else
        {
            position = CalculatePositionAtIndex(index);
        }
        
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
        
        ArrayElement element = new ArrayElement
        {
            position = position,
            index = index,
            objectName = objectName,
            objectInstance = objectInstance,
            trackedImage = trackedImage,
            isConfirmed = confirmed,
            animator = animator,
            isVirtual = (currentMode == InteractionMode.Virtual)
        };
        
        if (indexLabelPrefab != null)
        {
            element.indexLabel = Instantiate(indexLabelPrefab);
            element.indexLabel.transform.position = position + Vector3.up * 0.2f;
            
            if (arrayDirection != Vector3.zero)
            {
                Vector3 labelForward = Vector3.Cross(arrayDirection, Vector3.up);
                if (labelForward != Vector3.zero)
                {
                    element.indexLabel.transform.rotation = Quaternion.LookRotation(labelForward);
                }
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

    void CreateArrayContainer()
    {
        if (arrayElements.Count < 2) return;
        
        Vector3 perpendicular = Vector3.Cross(arrayDirection, Vector3.up).normalized;
        float containerWidth = elementSpacing * 0.4f;
        
        Vector3 currentFirstPos;
        if (currentMode == InteractionMode.Physical && arrayElements[0].trackedImage != null)
        {
            currentFirstPos = arrayElements[0].trackedImage.transform.position;
        }
        else
        {
            currentFirstPos = arrayElements[0].position;
        }
        
        float lineHeight = currentFirstPos.y + 0.03f;
        
        Vector3 leftPos = currentFirstPos - arrayDirection * (elementSpacing * 0.5f);
        leftPos.y = lineHeight;
        
        Vector3 rightPos = CalculatePositionAtIndex(1) + arrayDirection * (elementSpacing * 5.5f);
        rightPos.y = lineHeight;
        
        leftBoundary = CreateLine(leftPos, leftPos + Vector3.up * 0.12f, Color.yellow);
        rightBoundary = CreateLine(rightPos, rightPos + Vector3.up * 0.12f, Color.yellow);
        
        Vector3 topStart = leftPos + perpendicular * containerWidth;
        Vector3 topEnd = rightPos + perpendicular * containerWidth;
        containerTop = CreateLine(topStart, topEnd, containerLineColor);
        
        Vector3 bottomStart = leftPos - perpendicular * containerWidth;
        Vector3 bottomEnd = rightPos - perpendicular * containerWidth;
        containerBottom = CreateLine(bottomStart, bottomEnd, containerLineColor);
    }

    void UpdateArrayContainer()
    {
        if (arrayElements.Count < 2) return;
        
        Vector3 perpendicular = Vector3.Cross(arrayDirection, Vector3.up).normalized;
        float containerWidth = elementSpacing * 0.4f;
        
        Vector3 currentFirstPos;
        if (currentMode == InteractionMode.Physical && arrayElements[0].trackedImage != null)
        {
            currentFirstPos = arrayElements[0].trackedImage.transform.position;
        }
        else
        {
            currentFirstPos = arrayElements[0].position;
        }
        
        float lineHeight = currentFirstPos.y + 0.03f;
        
        Vector3 leftPos = currentFirstPos - arrayDirection * (elementSpacing * 0.5f);
        leftPos.y = lineHeight;
        
        Vector3 rightPos = CalculatePositionAtIndex(arrayElements.Count - 1) + arrayDirection * (elementSpacing * 1.5f);
        rightPos.y = lineHeight;
        
        if (rightBoundary != null)
        {
            LineRenderer lr = rightBoundary.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.SetPosition(0, rightPos);
                lr.SetPosition(1, rightPos + Vector3.up * 0.12f);
            }
        }
        
        if (containerTop != null)
        {
            LineRenderer lr = containerTop.GetComponent<LineRenderer>();
            if (lr != null)
            {
                Vector3 topStart = leftPos + perpendicular * containerWidth;
                Vector3 topEnd = rightPos + perpendicular * containerWidth;
                lr.SetPosition(0, topStart);
                lr.SetPosition(1, topEnd);
            }
        }
        
        if (containerBottom != null)
        {
            LineRenderer lr = containerBottom.GetComponent<LineRenderer>();
            if (lr != null)
            {
                Vector3 bottomStart = leftPos - perpendicular * containerWidth;
                Vector3 bottomEnd = rightPos - perpendicular * containerWidth;
                lr.SetPosition(0, bottomStart);
                lr.SetPosition(1, bottomEnd);
            }
        }
    }
    
    GameObject CreateLine(Vector3 start, Vector3 end, Color color)
    {
        GameObject lineObj = new GameObject("ContainerLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        
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
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        
        return lineObj;
    }
    
    Vector3 CalculatePositionAtIndex(int index)
    {
        if (currentMode == InteractionMode.Physical)
        {
            if (arrayElements.Count > 0 && arrayElements[0].trackedImage != null)
            {
                Vector3 currentFirstPos = arrayElements[0].trackedImage.transform.position;
                return currentFirstPos + arrayDirection * (elementSpacing * index);
            }
            return firstObjectPosition + arrayDirection * (elementSpacing * index);
        }
        else
        {
            return firstObjectPosition + arrayDirection * (elementSpacing * index);
        }
    }
    
    public void SimulateInsertAtIndex(int targetIndex)
    {
        if (currentState != ArrayState.ArrayReady) return;
        
        if (arrayElements.Count >= maxCapacity)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "Array is full!";
            return;
        }
        
        if (targetIndex == -1)
        {
            targetIndex = arrayElements.Count;
        }
        
        if (targetIndex < 0 || targetIndex > arrayElements.Count)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = $"Invalid index! Use 0-{arrayElements.Count}";
            return;
        }
        
        insertAtIndex = targetIndex;
        
        if (currentMode == InteractionMode.Virtual)
        {
            // Virtual mode: directly insert without scanning
            expectedInsertPosition = CalculatePositionAtIndex(targetIndex);
            CreateInsertIndicator(expectedInsertPosition);
            
            // Directly confirm insert in virtual mode
            currentState = ArrayState.WaitingToConfirmInsert;
            
            if (instructionText != null)
                instructionText.text = $"Tap to insert {virtualObjectType.ToUpper()} at [{targetIndex}]";
            
            if (detectionFeedbackText != null)
            {
                detectionFeedbackText.text = "Tap to confirm insert";
                detectionFeedbackText.color = Color.yellow;
            }
        }
        else
        {
            // Physical mode: check if shifting needed
            if (targetIndex == arrayElements.Count)
            {
                expectedInsertPosition = CalculatePositionAtIndex(targetIndex);
                CreateInsertIndicator(expectedInsertPosition);
                currentState = ArrayState.WaitingForInsertObject;
                
                if (instructionText != null)
                    instructionText.text = $"Scan object near GREEN spot [{targetIndex}]";
            }
            else
            {
                ShowInsertShiftIndicators(targetIndex);
                currentState = ArrayState.WaitingForShiftConfirmation;
                
                if (instructionText != null)
                {
                    int numToMove = arrayElements.Count - targetIndex;
                    instructionText.text = $"Move {numToMove} object(s) RIGHT, then CONFIRM";
                }
            }
        }
    }
    
    public void ConfirmShift()
    {
        if (currentState == ArrayState.WaitingForShiftConfirmation)
        {
            ClearShiftIndicators();
            expectedInsertPosition = CalculatePositionAtIndex(insertAtIndex);
            CreateInsertIndicator(expectedInsertPosition);
            currentState = ArrayState.WaitingForInsertObject;
            
            if (instructionText != null)
                instructionText.text = $"Scan object near GREEN spot [{insertAtIndex}]";
        }
        else if (currentState == ArrayState.WaitingForDeleteShiftConfirmation)
        {
            if (deleteAtIndex >= 0 && deleteAtIndex < arrayElements.Count)
            {
                ArrayElement element = arrayElements[deleteAtIndex];
                
                if (element.indexLabel != null) Destroy(element.indexLabel);
                if (element.objectInstance != null) Destroy(element.objectInstance);
                
                arrayElements.RemoveAt(deleteAtIndex);
                ClearShiftIndicators();
                UpdateAllIndices();
                UpdateArrayContainer();
                
                currentState = ArrayState.ArrayReady;
                if (instructionText != null)
                    instructionText.text = "Deleted! Array size: " + arrayElements.Count;
                
                if (statusText != null)
                    statusText.text = $"Elements: {arrayElements.Count}";
                
                deleteAtIndex = -1;
            }
        }
    }

    public void SimulateDeleteAtIndex(int targetIndex)
    {
        if (currentState != ArrayState.ArrayReady) return;
        
        if (arrayElements.Count == 0)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "Array is empty!";
            return;
        }
        
        if (targetIndex < 0 || targetIndex >= arrayElements.Count)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = $"Invalid index! Use 0-{arrayElements.Count - 1}";
            return;
        }
        
        deleteAtIndex = targetIndex;
        
         if (analysisManager != null)
        {
            analysisManager.AnalyzeDeleteOperation(targetIndex, arrayElements.Count);
        }
        if (currentMode == InteractionMode.Virtual)
        {
            ArrayElement elementToDelete = arrayElements[targetIndex];
            
            // Destroy the objects immediately
            if (elementToDelete.indexLabel != null) Destroy(elementToDelete.indexLabel);
            if (elementToDelete.objectInstance != null) Destroy(elementToDelete.objectInstance);
            
            // Remove from list
            arrayElements.RemoveAt(targetIndex);
            
            // Recalculate positions for all remaining elements
            UpdateVirtualPositions();
            UpdateAllIndices();
            UpdateArrayContainer();
            
            PlaySound(deleteSound);
            currentState = ArrayState.ArrayReady;
            
            if (instructionText != null)
                instructionText.text = "Deleted! Array updated";
            
            if (statusText != null)
                statusText.text = $"Elements: {arrayElements.Count}";
                
            if (detectionFeedbackText != null)
            {
                detectionFeedbackText.text = "Delete complete!";
                detectionFeedbackText.color = detectingColor;
            }
        }
        else
        {
            // Physical mode logic
            if (targetIndex == arrayElements.Count - 1)
            {
                ArrayElement element = arrayElements[targetIndex];
                
                if (element.indexLabel != null) Destroy(element.indexLabel);
                if (element.objectInstance != null) Destroy(element.objectInstance);
                
                arrayElements.RemoveAt(targetIndex);
                UpdateAllIndices();
                UpdateArrayContainer();
                
                PlaySound(deleteSound);
                currentState = ArrayState.ArrayReady;
                
                if (instructionText != null)
                    instructionText.text = "Deleted!";
                
                if (statusText != null)
                    statusText.text = $"Elements: {arrayElements.Count}";
            }
            else
            {
                ShowDeleteShiftIndicators(targetIndex);
                currentState = ArrayState.WaitingForDeleteShiftConfirmation;
                
                if (instructionText != null)
                {
                    int numToMove = arrayElements.Count - targetIndex - 1;
                    instructionText.text = $"Remove [{targetIndex}], move {numToMove} LEFT, then CONFIRM";
                }
            }
        }
    }
    
    public void SimulateAccessAtIndex(int targetIndex)
    {
        if (currentState != ArrayState.ArrayReady) return;
        
        if (targetIndex < 0 || targetIndex >= arrayElements.Count)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = $"Invalid index! Use 0-{arrayElements.Count - 1}";
            return;
        }
        
        PlaySound(accessSound);
        if (analysisManager != null)
        {
            analysisManager.AnalyzeAccessOperation(targetIndex, arrayElements.Count);
        }
        
        if (instructionText != null)
            instructionText.text = $"Accessed [{targetIndex}]: {arrayElements[targetIndex].objectName.ToUpper()}";
        
        StartCoroutine(HighlightElement(targetIndex));
    }
    
    public void SimulateDelete()
    {
        if (currentState != ArrayState.ArrayReady || arrayElements.Count == 0) return;
        
        currentState = ArrayState.WaitingForDeleteSelection;
        
        if (instructionText != null)
            instructionText.text = "TAP the object to delete";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Tap object";
            detectionFeedbackText.color = notDetectingColor;
        }
    }
    
    public void SimulateAccess()
    {
        if (currentState != ArrayState.ArrayReady || arrayElements.Count == 0) return;
        
        currentState = ArrayState.WaitingForAccessSelection;
        
        if (instructionText != null)
            instructionText.text = "TAP object to access";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Tap object";
            detectionFeedbackText.color = notDetectingColor;
        }
    }
    
    public void SimulateSearch()
    {
        if (arrayElements.Count == 0) return;
        StartCoroutine(LinearSearchAnimation());
    }
    
    System.Collections.IEnumerator LinearSearchAnimation()
{
    if (instructionText != null)
        instructionText.text = "Searching array...";
    
    // 🎯 Pick a random element to "find" during search
    int searchForIndex = Random.Range(0, arrayElements.Count);
    bool found = false;
    int foundAtIndex = -1;
    
    for (int i = 0; i < arrayElements.Count; i++)
    {
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        highlight.transform.position = arrayElements[i].position + Vector3.up * 0.12f;
        highlight.transform.localScale = Vector3.one * 0.06f;
        
        Renderer rend = highlight.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Unlit/Color"));
        
        // Check if this is the element we're "searching for"
        if (i == searchForIndex)
        {
            found = true;
            foundAtIndex = i;
            mat.color = new Color(0, 1, 0, 0.9f); // Green when found!
            
            if (instructionText != null)
                instructionText.text = $"Found at index [{i}]!";
        }
        else
        {
            mat.color = new Color(1, 1, 0, 0.7f); // Yellow while searching
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
            // Keep the found highlight visible longer
            yield return new WaitForSeconds(1f);
            Destroy(highlight);
            break; // Stop searching once found
        }
    }
    
    // 🔍 TRIGGER ALGORITHM ANALYSIS FOR SEARCH
    if (analysisManager != null)
    {
        analysisManager.AnalyzeSearchOperation(arrayElements.Count, found, foundAtIndex);
    }
    
    if (instructionText != null)
        instructionText.text = found ? $"Search complete! Found at [{foundAtIndex}]" : "Search complete! Not found";
}

    
    void ShowDeleteShiftIndicators(int deleteIndex)
    {
        ClearShiftIndicators();
        
        for (int i = deleteIndex + 1; i < arrayElements.Count; i++)
        {
            Vector3 currentPos = arrayElements[i].position;
            Vector3 newPos = CalculatePositionAtIndex(i - 1);
            
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
            CreateImprovedArrow(currentPos, newPos, Color.yellow, i);
        }
    }
    
    void ShowInsertShiftIndicators(int insertIndex)
    {
        ClearShiftIndicators();
        
        for (int i = insertIndex; i < arrayElements.Count; i++)
        {
            Vector3 currentPos = arrayElements[i].position;
            Vector3 newPos = CalculatePositionAtIndex(i + 1);
            
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
        
        lr.positionCount = 5;
        lr.SetPosition(0, start);
        lr.SetPosition(1, arrowBase);
        lr.SetPosition(2, arrowBase - perpendicular * 0.012f);
        lr.SetPosition(3, end);
        lr.SetPosition(4, arrowBase + perpendicular * 0.012f);
        
        shiftIndicators.Add(arrow);
    }
    
    void CreateInsertIndicator(Vector3 position)
    {
        if (insertIndicator != null) Destroy(insertIndicator);
        
        insertIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        insertIndicator.transform.position = position + Vector3.up * 0.03f;
        insertIndicator.transform.localScale = Vector3.one * 0.04f;
        
        Renderer rend = insertIndicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0, 1, 0, 0.95f);
        rend.material = mat;
        
        Collider col = insertIndicator.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col); 
    }
    
    void ClearShiftIndicators()
    {
        foreach (GameObject indicator in shiftIndicators)
        {
            if (indicator != null) Destroy(indicator);
        }
        shiftIndicators.Clear();
    }
    
    void UpdateAllIndices()
    {
        for (int i = 0; i < arrayElements.Count; i++)
        {
            arrayElements[i].index = i;
            
            if (arrayElements[i].indexLabel != null)
            {
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
            case ArrayState.ModeSelection:
                instructionText.text = "Select interaction mode";
                break;
                
            case ArrayState.WaitingForFirstObject:
                if (currentMode == InteractionMode.Physical)
                    instructionText.text = "Scan FIRST object's QR code";
                else
                    instructionText.text = "Scan ONE QR to define all nodes";
                break;
                
            case ArrayState.WaitingToConfirmFirst:
                instructionText.text = "TAP to confirm";
                break;
                
            case ArrayState.WaitingForSecondObject:
                if (currentMode == InteractionMode.Physical)
                    instructionText.text = "Scan SECOND QR (place to RIGHT)";
                else
                    instructionText.text = "Scan SECOND object's QR";
                break;
                
            case ArrayState.WaitingToConfirmSecond:
                instructionText.text = "TAP to confirm second element";
                break;
                
            case ArrayState.ArrayReady:
                instructionText.text = "Array ready! Use buttons";
                break;
        }
    }
    
    public void ResetArray()
    {
        foreach (var element in arrayElements)
        {
            if (element.indexLabel != null) Destroy(element.indexLabel);
            if (element.objectInstance != null) Destroy(element.objectInstance);
        }
        arrayElements.Clear();
        
        if (leftBoundary != null) Destroy(leftBoundary);
        if (rightBoundary != null) Destroy(rightBoundary);
        if (containerTop != null) Destroy(containerTop);
        if (containerBottom != null) Destroy(containerBottom);
        if (insertIndicator != null) Destroy(insertIndicator);
        
        ClearShiftIndicators();
        trackedQRCodes.Clear();
        processedImages.Clear();
        
        pendingFirstImage = null;
        pendingSecondImage = null;
        pendingInsertImage = null;
        
        virtualObjectType = "";
        virtualArrayInitialized = false;
        currentState = ArrayState.ModeSelection;
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Choose a mode";
            detectionFeedbackText.color = Color.yellow;
        }
        
        if (statusText != null)
            statusText.text = "Elements: 0";
        
        UpdateInstructions();
        Debug.Log("Array reset");
         if (analysisManager != null)
        {
            analysisManager.ResetCounters();
        }
        
        Debug.Log("Array reset and analysis cleared");
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
        return currentState == ArrayState.WaitingForShiftConfirmation || 
               currentState == ArrayState.WaitingForDeleteShiftConfirmation;
    }
    
    public bool IsModeSelected()
    {
        return currentState != ArrayState.ModeSelection;
    }
    
    public InteractionMode GetCurrentMode()
    {
        return currentMode;
    }
    
    public string GetVirtualObjectType()
    {
        return virtualObjectType;
    }
}