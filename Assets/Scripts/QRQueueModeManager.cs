using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class QRQueueModeManager : MonoBehaviour
{
    public enum InteractionMode
    {
        Physical,
        Virtual
    }
    
    [Header("Mode Settings")]
    public InteractionMode currentMode = InteractionMode.Physical;
    
    [Header("Queue Settings")]
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
    public QueueAnalysisManager analysisManager;
    
    [Header("UI References")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI detectionFeedbackText;
    public TextMeshProUGUI modeIndicatorText;
    
    [Header("Colors")]
    public Color detectingColor = Color.green;
    public Color notDetectingColor = Color.red;
    public Color frontMarkerColor = new Color(0, 1, 0, 0.9f); // Green for FRONT
    public Color rearMarkerColor = new Color(1, 0.5f, 0, 0.9f); // Orange for REAR
    
    [Header("Audio")]
    public AudioClip scanSound;
    public AudioClip confirmSound;
    public AudioClip enqueueSound;
    public AudioClip dequeueSound;
    public AudioClip peekSound;
    public AudioClip errorSound;
    private AudioSource audioSource;
    
    private enum QueueState
    {
        ModeSelection,
        WaitingForFirstObject,
        WaitingToConfirmFirst,
        WaitingForSecondObject,
        WaitingToConfirmSecond,
        QueueReady,
        WaitingForEnqueueObject,
        WaitingToConfirmEnqueue
    }
    
    private QueueState currentState = QueueState.ModeSelection;
    
    private class QueueElement
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
    
    private List<QueueElement> queueElements = new List<QueueElement>(); // FIFO: index 0 = FRONT, last = REAR
    private Dictionary<string, ARTrackedImage> trackedQRCodes = new Dictionary<string, ARTrackedImage>();
    private Dictionary<string, GameObject> objectPrefabs = new Dictionary<string, GameObject>();
    private HashSet<string> processedImages = new HashSet<string>();
    
    private string virtualObjectType = "";
    private Vector3 virtualQueueStartPosition;
    private bool virtualQueueInitialized = false;
    
    private GameObject frontMarker;
    private GameObject rearMarker;
    private GameObject queueContainer;
    
    private Vector3 firstObjectPosition;
    private Vector3 secondObjectPosition;
    private float elementSpacing = 0.15f;
    private Vector3 queueDirection;
    
    private ARTrackedImage pendingFirstImage;
    private ARTrackedImage pendingSecondImage;
    private ARTrackedImage pendingEnqueueImage;
    private string pendingEnqueueObjectName = "";
    
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
        
        Debug.Log($"QR Queue Manager Initialized - Mode: {currentMode}");
    }
    
    public void SetPhysicalMode()
    {
        currentMode = InteractionMode.Physical;
        currentState = QueueState.WaitingForFirstObject;
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
        currentState = QueueState.WaitingForFirstObject;
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
                modeIndicatorText.text = "PHYSICAL MODE - QUEUE";
                modeIndicatorText.color = new Color(0.2f, 0.8f, 1f);
            }
            else
            {
                modeIndicatorText.text = "VIRTUAL MODE - QUEUE";
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
            case QueueState.WaitingToConfirmFirst:
                if (currentMode == InteractionMode.Physical)
                {
                    if (pendingFirstImage != null && TapNearTrackedImage(screenPosition, pendingFirstImage))
                    {
                        ConfirmFirstElement();
                    }
                }
                else
                {
                    ConfirmFirstElement();
                }
                break;
                
            case QueueState.WaitingToConfirmSecond:
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
                
            case QueueState.WaitingToConfirmEnqueue:
                ConfirmEnqueueElement();
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
        
        if (currentMode == InteractionMode.Virtual)
        {
            if (currentState == QueueState.WaitingForFirstObject && string.IsNullOrEmpty(virtualObjectType))
            {
                // First scan - accept it
            }
            else if (currentState == QueueState.WaitingForFirstObject)
            {
                Debug.Log($"Virtual mode: Already using {virtualObjectType}. Ignoring {imageName}");
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
            case QueueState.WaitingForFirstObject:
            case QueueState.WaitingForSecondObject:
                shouldProcess = true;
                break;
                
            case QueueState.WaitingForEnqueueObject:
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
        
        if (currentMode == InteractionMode.Physical && currentState != QueueState.WaitingForEnqueueObject)
        {
            bool alreadyInQueue = false;
            foreach (var element in queueElements)
            {
                if (element.objectName.ToLower() == imageName)
                {
                    alreadyInQueue = true;
                    break;
                }
            }
            
            if (alreadyInQueue)
            {
                Debug.Log($"{imageName} already in queue");
                PlaySound(errorSound);
                return;
            }
        }
        
        if (currentState == QueueState.WaitingForEnqueueObject)
        {
            Debug.Log($"ENQUEUE: Detected {imageName} at {imagePosition}");
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
            
            ShowEnqueueObjectPreview(imageName, trackedImage);
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
        
        switch (currentState)
        {
            case QueueState.WaitingForFirstObject:
                ShowFirstObjectPreview(imageName, trackedImage);
                break;
                
            case QueueState.WaitingForSecondObject:
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
            virtualObjectType = objectName;
            virtualQueueStartPosition = trackedImage.transform.position;
            previewObj = InstantiateObjectVirtual(objectName, virtualQueueStartPosition);
            
            Debug.Log($"Virtual mode: Set object type to {virtualObjectType}");
        }
        
        Renderer[] renderers = previewObj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Color color = rend.material.color;
            color.a = 0.7f;
            rend.material.color = color;
        }
        
        currentState = QueueState.WaitingToConfirmFirst;
        
        if (instructionText != null)
        {
            if (currentMode == InteractionMode.Physical)
            {
                instructionText.text = $"Tap the {objectName.ToUpper()} QR to confirm FRONT";
            }
            else
            {
                instructionText.text = $"Tap ANYWHERE to confirm {objectName.ToUpper()} as queue nodes";
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
            objectName = virtualObjectType;
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
            firstObjectPosition = virtualQueueStartPosition;
        }
        
        CreateQueueElement(pendingFirstImage, 0, objectName, true, existingObject);
        
        if (currentMode == InteractionMode.Virtual)
        {
            secondObjectPosition = firstObjectPosition + Vector3.right * elementSpacing;
            queueDirection = Vector3.right;
            
            GameObject secondObj = InstantiateObjectVirtual(virtualObjectType, secondObjectPosition);
            CreateQueueElement(null, 1, virtualObjectType, true, secondObj);
            
            virtualQueueInitialized = true;
            CreateQueueVisuals();
            
            currentState = QueueState.QueueReady;
            
            if (detectionFeedbackText != null)
            {
                detectionFeedbackText.text = $"Queue Ready! Using {virtualObjectType.ToUpper()} nodes";
                detectionFeedbackText.color = detectingColor;
            }
            
            Debug.Log($"Virtual queue initialized with {virtualObjectType}");
        }
        else
        {
            currentState = QueueState.WaitingForSecondObject;
            
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
        
        currentState = QueueState.WaitingToConfirmSecond;
        
        if (instructionText != null)
        {
            if (currentMode == InteractionMode.Physical)
            {
                instructionText.text = $"Tap the {objectName.ToUpper()} QR to confirm REAR";
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
            
            Vector3 currentFirstPos = queueElements[0].trackedImage.transform.position;
            Vector3 currentSecondPos = pendingSecondImage.transform.position;
            
            secondObjectPosition = currentSecondPos;
            queueDirection = (currentSecondPos - currentFirstPos).normalized;
            elementSpacing = Vector3.Distance(currentFirstPos, currentSecondPos);
            
            CreateQueueElement(pendingSecondImage, 1, objectName, true, existingObject);
        }
        
        CreateQueueVisuals();
        
        currentState = QueueState.QueueReady;
        UpdateInstructions();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Queue Ready!";
        }
        
        Debug.Log($"Queue initialized - Mode: {currentMode}");
        pendingSecondImage = null;
    }

    void ShowEnqueueObjectPreview(string objectName, ARTrackedImage trackedImage)
    {
        Vector3 expectedPosition = CalculatePositionAtIndex(queueElements.Count);
        Vector3 position = trackedImage.transform.position;
        float distance = Vector3.Distance(position, expectedPosition);
        
        Debug.Log($"ShowEnqueueObjectPreview: {objectName} at {position}, distance to REAR: {distance:F3}m");
        
        GameObject previewObj = InstantiateObject(objectName, trackedImage);
        
        if (previewObj == null)
        {
            Debug.LogError("Failed to create preview object!");
            return;
        }
        
        pendingEnqueueImage = trackedImage;
        pendingEnqueueObjectName = objectName;
        
        float alpha = 0.7f;
        Color feedbackColor = Color.yellow;
        string instruction = $"Tap the {objectName.ToUpper()} QR to ENQUEUE at REAR";
        
        if (distance > elementSpacing * 0.7f)
        {
            Debug.LogWarning($"Object far from expected REAR position. Distance: {distance:F3}m");
            alpha = 0.5f;
            feedbackColor = new Color(1f, 0.5f, 0f);
            instruction = $"Move QR closer to REAR (orange marker)\nDistance: {distance:F2}m";
        }
        
        Renderer[] renderers = previewObj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Color color = rend.material.color;
            color.a = alpha;
            rend.material.color = color;
        }
        
        currentState = QueueState.WaitingToConfirmEnqueue;
        
        if (instructionText != null)
        {
            instructionText.text = instruction;
        }
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = distance > elementSpacing * 0.7f ? "Too far from REAR" : "Tap to confirm";
            detectionFeedbackText.color = feedbackColor;
        }
        
        Debug.Log($"Enqueue preview created for {objectName}. Waiting for confirmation.");
    }
    
    void ConfirmEnqueueElement()
    {
        PlaySound(enqueueSound);
        
        GameObject existingObject = null;
        Vector3 position;
        string objectName;
        
        if (currentMode == InteractionMode.Physical)
        {
            if (pendingEnqueueImage == null) return;
            
            Transform previewTransform = pendingEnqueueImage.transform;
            foreach (Transform child in previewTransform)
            {
                existingObject = child.gameObject;
                MakeOpaque(existingObject);
                break;
            }
            position = pendingEnqueueImage.transform.position;
            objectName = pendingEnqueueObjectName;
        }
        else
        {
            objectName = virtualObjectType;
            position = CalculatePositionAtIndex(queueElements.Count);
            existingObject = InstantiateObjectVirtual(virtualObjectType, position);
        }
        
        if (existingObject != null)
        {
            ObjectAnimator animator = existingObject.GetComponent<ObjectAnimator>();
            if (animator == null)
            {
                animator = existingObject.AddComponent<ObjectAnimator>();
            }
            
            QueueElement newElement = new QueueElement
            {
                position = position,
                index = queueElements.Count,
                objectName = objectName,
                objectInstance = existingObject,
                trackedImage = (currentMode == InteractionMode.Physical) ? pendingEnqueueImage : null,
                isConfirmed = true,
                animator = animator,
                isVirtual = (currentMode == InteractionMode.Virtual)
            };
            
            if (indexLabelPrefab != null)
            {
                newElement.indexLabel = Instantiate(indexLabelPrefab);
                newElement.indexLabel.transform.position = position + Vector3.up * 0.2f;
                
                Vector3 labelForward = Vector3.Cross(queueDirection, Vector3.up);
                if (labelForward != Vector3.zero)
                {
                    newElement.indexLabel.transform.rotation = Quaternion.LookRotation(labelForward);
                }
                
                TextMeshPro labelText = newElement.indexLabel.GetComponentInChildren<TextMeshPro>();
                if (labelText != null)
                {
                    labelText.text = $"[{queueElements.Count}]";
                }
            }
            
            queueElements.Add(newElement);
        }

        if (analysisManager != null)
        {
            analysisManager.AnalyzeEnqueueOperation(queueElements.Count);
        }
        
        UpdateQueueVisuals();
        
        if (statusText != null)
            statusText.text = $"Queue Size: {queueElements.Count}";
        
        currentState = QueueState.QueueReady;
        if (instructionText != null)
            instructionText.text = $"ENQUEUED {objectName.ToUpper()} at REAR!";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Enqueue confirmed!";
            detectionFeedbackText.color = detectingColor;
        }
        
        pendingEnqueueObjectName = "";
        pendingEnqueueImage = null;
    }
    
    void UpdateTrackedObject(ARTrackedImage trackedImage)
    {
        if (currentMode != InteractionMode.Physical) return;
        
        foreach (var element in queueElements)
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
    
    void CreateQueueElement(ARTrackedImage trackedImage, int index, string objectName, bool confirmed, GameObject existingObject = null)
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
        
        QueueElement element = new QueueElement
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
            
            if (queueDirection != Vector3.zero)
            {
                Vector3 labelForward = Vector3.Cross(queueDirection, Vector3.up);
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
        
        queueElements.Add(element);
        
        if (statusText != null)
            statusText.text = $"Queue Size: {queueElements.Count}";
    }

    void CreateQueueVisuals()
    {
        if (queueElements.Count < 2) return;
        
        Vector3 currentFirstPos;
        if (currentMode == InteractionMode.Physical && queueElements[0].trackedImage != null)
        {
            currentFirstPos = queueElements[0].trackedImage.transform.position;
        }
        else
        {
            currentFirstPos = queueElements[0].position;
        }
        
        float lineHeight = currentFirstPos.y + 0.03f;
        
        // FRONT marker (Green) at index 0
        Vector3 frontPos = currentFirstPos - queueDirection * (elementSpacing * 0.3f);
        frontPos.y = lineHeight;
        frontMarker = CreateMarker(frontPos, frontMarkerColor, "FRONT");
        
        // REAR marker (Orange) at last element
        Vector3 rearPos = CalculatePositionAtIndex(queueElements.Count - 1) + queueDirection * (elementSpacing * 0.3f);
        rearPos.y = lineHeight;
        rearMarker = CreateMarker(rearPos, rearMarkerColor, "REAR");
        
        // Container line
        queueContainer = CreateLine(frontPos, rearPos, new Color(0, 0.8f, 1f, 0.8f));
    }

    void UpdateQueueVisuals()
    {
        if (queueElements.Count < 2) return;
        
        Vector3 currentFirstPos;
        if (currentMode == InteractionMode.Physical && queueElements[0].trackedImage != null)
        {
            currentFirstPos = queueElements[0].trackedImage.transform.position;
        }
        else
        {
            currentFirstPos = queueElements[0].position;
        }
        
        float lineHeight = currentFirstPos.y + 0.03f;
        
        // Update FRONT marker
        if (frontMarker != null)
        {
            Vector3 frontPos = currentFirstPos - queueDirection * (elementSpacing * 0.3f);
            frontPos.y = lineHeight;
            frontMarker.transform.position = frontPos;
        }
        
        // Update REAR marker
        if (rearMarker != null)
        {
            Vector3 rearPos = CalculatePositionAtIndex(queueElements.Count - 1) + queueDirection * (elementSpacing * 0.3f);
            rearPos.y = lineHeight;
            rearMarker.transform.position = rearPos;
        }
        
        // Update container line
        if (queueContainer != null)
        {
            LineRenderer lr = queueContainer.GetComponent<LineRenderer>();
            if (lr != null)
            {
                Vector3 frontPos = currentFirstPos - queueDirection * (elementSpacing * 0.3f);
                frontPos.y = lineHeight;
                Vector3 rearPos = CalculatePositionAtIndex(queueElements.Count - 1) + queueDirection * (elementSpacing * 0.3f);
                rearPos.y = lineHeight;
                
                lr.SetPosition(0, frontPos);
                lr.SetPosition(1, rearPos);
            }
        }
    }
    
    GameObject CreateMarker(Vector3 position, Color color, string label)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.transform.position = position;
        marker.transform.localScale = new Vector3(0.04f, 0.06f, 0.04f);
        
        Renderer rend = marker.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        rend.material = mat;
        
        Collider col = marker.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        // Add text label
        GameObject labelObj = new GameObject($"{label}Label");
        labelObj.transform.SetParent(marker.transform);
        labelObj.transform.localPosition = Vector3.up * 0.15f;
        
        TextMeshPro text = labelObj.AddComponent<TextMeshPro>();
        text.text = label;
        text.fontSize = 3;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        
        return marker;
    }
    
    GameObject CreateLine(Vector3 start, Vector3 end, Color color)
    {
        GameObject lineObj = new GameObject("QueueLine");
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
        
        return lineObj;
    }
    
    Vector3 CalculatePositionAtIndex(int index)
    {
        if (currentMode == InteractionMode.Physical)
        {
            if (queueElements.Count > 0 && queueElements[0].trackedImage != null)
            {
                Vector3 currentFirstPos = queueElements[0].trackedImage.transform.position;
                return currentFirstPos + queueDirection * (elementSpacing * index);
            }
            return firstObjectPosition + queueDirection * (elementSpacing * index);
        }
        else
        {
            return firstObjectPosition + queueDirection * (elementSpacing * index);
        }
    }
    
    public void SimulateEnqueue()
    {
        if (currentState != QueueState.QueueReady) return;
        
        if (queueElements.Count >= maxCapacity)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "Queue is full!";
            return;
        }
        
        if (currentMode == InteractionMode.Virtual)
        {
            // Virtual mode: directly enqueue
            currentState = QueueState.WaitingToConfirmEnqueue;
            
            if (instructionText != null)
                instructionText.text = $"Tap to ENQUEUE {virtualObjectType.ToUpper()} at REAR";
            
            if (detectionFeedbackText != null)
            {
                detectionFeedbackText.text = "Tap to confirm enqueue";
                detectionFeedbackText.color = Color.yellow;
            }
        }
        else
        {
            // Physical mode: wait for scan
            currentState = QueueState.WaitingForEnqueueObject;
            
            if (instructionText != null)
                instructionText.text = "Scan object QR near REAR (orange marker)";
            
            if (detectionFeedbackText != null)
            {
                detectionFeedbackText.text = "Waiting for scan...";
                detectionFeedbackText.color = notDetectingColor;
            }
        }
    }
    
    public void SimulateDequeue()
    {
        if (currentState != QueueState.QueueReady || queueElements.Count == 0) return;
        
        PlaySound(dequeueSound);
        
        QueueElement frontElement = queueElements[0];
        string dequeuedName = frontElement.objectName.ToUpper();
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzeDequeueOperation(queueElements.Count);
        }
        
        if (frontElement.indexLabel != null) Destroy(frontElement.indexLabel);
        if (frontElement.objectInstance != null) Destroy(frontElement.objectInstance);
        
        queueElements.RemoveAt(0);
        
        if (currentMode == InteractionMode.Virtual)
        {
            UpdateVirtualPositions();
        }
        
        UpdateAllIndices();
        UpdateQueueVisuals();
        
        if (instructionText != null)
            instructionText.text = $"DEQUEUED {dequeuedName} from FRONT!";
        
        if (statusText != null)
            statusText.text = $"Queue Size: {queueElements.Count}";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Dequeue complete!";
            detectionFeedbackText.color = detectingColor;
        }
    }
    
    public void SimulatePeek()
    {
        if (currentState != QueueState.QueueReady || queueElements.Count == 0) return;
        
        PlaySound(peekSound);
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzePeekOperation();
        }
        
        if (instructionText != null)
            instructionText.text = $"FRONT element: {queueElements[0].objectName.ToUpper()}";
        
        StartCoroutine(HighlightElement(0));
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Peek complete!";
            detectionFeedbackText.color = detectingColor;
        }
    }
    
    System.Collections.IEnumerator HighlightElement(int index)
    {
        if (index >= 0 && index < queueElements.Count)
        {
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            highlight.transform.position = queueElements[index].position + Vector3.up * 0.12f;
            highlight.transform.localScale = Vector3.one * 0.08f;
            
            Renderer rend = highlight.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0, 1, 1, 0.7f);
            rend.material = mat;
            
            Collider col = highlight.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            yield return new WaitForSeconds(1.5f);
            Destroy(highlight);
        }
    }
    
    void UpdateVirtualPositions()
    {
        for (int i = 0; i < queueElements.Count; i++)
        {
            if (queueElements[i].isVirtual && queueElements[i].objectInstance != null)
            {
                Vector3 newPos = CalculatePositionAtIndex(i);
                queueElements[i].position = newPos;
                queueElements[i].objectInstance.transform.position = newPos + Vector3.up * 0.08f;
                
                if (queueElements[i].animator != null)
                {
                    queueElements[i].animator.UpdateStartPosition();
                }
                
                if (queueElements[i].indexLabel != null)
                {
                    queueElements[i].indexLabel.transform.position = newPos + Vector3.up * 0.2f;
                }
            }
        }
    }
    
    void UpdateAllIndices()
    {
        for (int i = 0; i < queueElements.Count; i++)
        {
            queueElements[i].index = i;
            
            if (queueElements[i].indexLabel != null)
            {
                TextMeshPro labelText = queueElements[i].indexLabel.GetComponentInChildren<TextMeshPro>();
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
            case QueueState.ModeSelection:
                instructionText.text = "Select interaction mode";
                break;
                
            case QueueState.WaitingForFirstObject:
                if (currentMode == InteractionMode.Physical)
                    instructionText.text = "Scan FIRST object's QR code (FRONT)";
                else
                    instructionText.text = "Scan ONE QR to define all queue nodes";
                break;
                
            case QueueState.WaitingToConfirmFirst:
                instructionText.text = "TAP to confirm FRONT";
                break;
                
            case QueueState.WaitingForSecondObject:
                if (currentMode == InteractionMode.Physical)
                    instructionText.text = "Scan SECOND QR (place to RIGHT for REAR)";
                else
                    instructionText.text = "Scan SECOND object's QR";
                break;
                
            case QueueState.WaitingToConfirmSecond:
                instructionText.text = "TAP to confirm REAR";
                break;
                
            case QueueState.QueueReady:
                instructionText.text = "Queue ready! ENQUEUE adds to REAR, DEQUEUE removes from FRONT";
                break;
        }
    }
    
    public void ResetQueue()
    {
        foreach (var element in queueElements)
        {
            if (element.indexLabel != null) Destroy(element.indexLabel);
            if (element.objectInstance != null) Destroy(element.objectInstance);
        }
        queueElements.Clear();
        
        if (frontMarker != null) Destroy(frontMarker);
        if (rearMarker != null) Destroy(rearMarker);
        if (queueContainer != null) Destroy(queueContainer);
        
        trackedQRCodes.Clear();
        processedImages.Clear();
        
        pendingFirstImage = null;
        pendingSecondImage = null;
        pendingEnqueueImage = null;
        
        virtualObjectType = "";
        virtualQueueInitialized = false;
        currentState = QueueState.ModeSelection;
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Choose a mode";
            detectionFeedbackText.color = Color.yellow;
        }
        
        if (statusText != null)
            statusText.text = "Queue Size: 0";
        
        UpdateInstructions();
        
        if (analysisManager != null)
        {
            analysisManager.ResetCounters();
        }
        
        Debug.Log("Queue reset");
    }
    
    public int GetQueueSize()
    {
        return queueElements.Count;
    }
    
    public bool IsQueueReady()
    {
        return currentState == QueueState.QueueReady;
    }
    
    public bool IsModeSelected()
    {
        return currentState != QueueState.ModeSelection;
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