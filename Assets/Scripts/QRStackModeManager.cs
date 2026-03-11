using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class QRStackModeManager : MonoBehaviour
{
    public enum InteractionMode
    {
        Physical,  // Move QR codes physically in real world
        Virtual    // Scan QR once, manipulate virtually in app
    }
    
    [Header("Mode Settings")]
    public InteractionMode currentMode = InteractionMode.Physical;
    
    [Header("Stack Settings")]
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
    public StackAnalysisManager analysisManager;
    
    [Header("UI References")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI detectionFeedbackText;
    public TextMeshProUGUI modeIndicatorText;
    
    [Header("Colors")]
    public Color detectingColor = Color.green;
    public Color notDetectingColor = Color.red;
    public Color topIndicatorColor = new Color(1f, 0.84f, 0f, 0.9f); // Gold
    
    [Header("Audio")]
    public AudioClip scanSound;
    public AudioClip confirmSound;
    public AudioClip pushSound;
    public AudioClip popSound;
    public AudioClip peekSound;
    public AudioClip errorSound;
    private AudioSource audioSource;
    
    private enum StackState
    {
        ModeSelection,
        WaitingForBaseObject,
        WaitingToConfirmBase,
        StackReady,
        WaitingForPushObject,
        WaitingToConfirmPush,
        AnimatingPush,
        AnimatingPop
    }
    
    private StackState currentState = StackState.ModeSelection;
    
    private class StackElement
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
    
    private Stack<StackElement> stackElements = new Stack<StackElement>();
    private List<StackElement> allElements = new List<StackElement>(); // For rendering
    private Dictionary<string, ARTrackedImage> trackedQRCodes = new Dictionary<string, ARTrackedImage>();
    private Dictionary<string, GameObject> objectPrefabs = new Dictionary<string, GameObject>();
    private HashSet<string> processedImages = new HashSet<string>();
    
    // Virtual mode specific
    private string virtualObjectType = "";
    private Vector3 virtualStackBasePosition;
    private bool virtualStackInitialized = false;
    
    private GameObject topIndicator;
    private GameObject stackBase;
    private GameObject leftBoundary;
    private GameObject rightBoundary;
    
    private Vector3 basePosition;
    private float elementSpacing = 0.15f; // Horizontal spacing between elements
    
    private ARTrackedImage pendingBaseImage;
    private ARTrackedImage pendingPushImage;
    private string pendingPushObjectName = "";
    
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
        
        Debug.Log($"QR Stack Manager Initialized - Mode: {currentMode}");
    }
    
    public void SetPhysicalMode()
    {
        currentMode = InteractionMode.Physical;
        currentState = StackState.WaitingForBaseObject;
        UpdateModeIndicator();
        UpdateInstructions();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Physical Mode: Scan base QR";
            detectionFeedbackText.color = notDetectingColor;
        }
        
        Debug.Log("Switched to PHYSICAL mode");
    }
    
    public void SetVirtualMode()
    {
        currentMode = InteractionMode.Virtual;
        currentState = StackState.WaitingForBaseObject;
        UpdateModeIndicator();
        UpdateInstructions();
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Virtual Mode: Scan ONE QR to define stack nodes";
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
            case StackState.WaitingToConfirmBase:
                if (currentMode == InteractionMode.Physical)
                {
                    if (pendingBaseImage != null && TapNearTrackedImage(screenPosition, pendingBaseImage))
                    {
                        ConfirmBaseElement();
                    }
                }
                else
                {
                    ConfirmBaseElement();
                }
                break;
                
            case StackState.WaitingToConfirmPush:
                if (currentMode == InteractionMode.Physical)
                {
                    if (pendingPushImage != null && TapNearTrackedImage(screenPosition, pendingPushImage))
                    {
                        ConfirmPushElement();
                    }
                }
                else
                {
                    ConfirmPushElement();
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
            if (currentState == StackState.WaitingForBaseObject && string.IsNullOrEmpty(virtualObjectType))
            {
                // First scan in virtual mode
            }
            else if (currentState == StackState.WaitingForBaseObject)
            {
                Debug.Log($"Virtual mode: Already using {virtualObjectType}. Ignoring {imageName}");
                return;
            }
            else
            {
                // After setup, no scans needed for push in virtual mode
                return;
            }
        }
        
        bool shouldProcess = false;
        
        switch (currentState)
        {
            case StackState.WaitingForBaseObject:
                shouldProcess = true;
                break;
                
            case StackState.WaitingForPushObject:
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
        
        // Allow duplicate types for push operations in physical mode
        if (currentMode == InteractionMode.Physical && currentState != StackState.WaitingForPushObject)
        {
            if (processedImages.Contains(imageName))
            {
                Debug.Log($"{imageName} already processed");
                return;
            }
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
        
        if (currentState != StackState.WaitingForPushObject)
        {
            processedImages.Add(imageName);
        }
        
        switch (currentState)
        {
            case StackState.WaitingForBaseObject:
                ShowBaseObjectPreview(imageName, trackedImage);
                break;
                
            case StackState.WaitingForPushObject:
                ShowPushObjectPreview(imageName, trackedImage);
                break;
        }
    }
    
    void ShowBaseObjectPreview(string objectName, ARTrackedImage trackedImage)
    {
        pendingBaseImage = trackedImage;
        
        GameObject previewObj;
        
        if (currentMode == InteractionMode.Physical)
        {
            previewObj = InstantiateObject(objectName, trackedImage);
        }
        else
        {
            virtualObjectType = objectName;
            virtualStackBasePosition = trackedImage.transform.position;
            previewObj = InstantiateObjectVirtual(objectName, virtualStackBasePosition);
            
            Debug.Log($"Virtual mode: Set object type to {virtualObjectType}");
        }
        
        Renderer[] renderers = previewObj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Color color = rend.material.color;
            color.a = 0.7f;
            rend.material.color = color;
        }
        
        currentState = StackState.WaitingToConfirmBase;
        
        if (instructionText != null)
        {
            if (currentMode == InteractionMode.Physical)
            {
                instructionText.text = $"Tap the {objectName.ToUpper()} QR to confirm as base";
            }
            else
            {
                instructionText.text = $"Tap ANYWHERE to confirm {objectName.ToUpper()} as stack nodes";
            }
        }
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Tap to confirm";
            detectionFeedbackText.color = Color.yellow;
        }
    }
    
    void ConfirmBaseElement()
    {
        if (pendingBaseImage == null && currentMode == InteractionMode.Physical) return;
        
        string objectName;
        
        if (currentMode == InteractionMode.Physical)
        {
            objectName = pendingBaseImage.referenceImage.name.ToLower();
        }
        else
        {
            objectName = virtualObjectType;
        }
        
        PlaySound(confirmSound);
        
        GameObject existingObject = null;
        
        if (currentMode == InteractionMode.Physical)
        {
            Transform previewTransform = pendingBaseImage.transform;
            foreach (Transform child in previewTransform)
            {
                existingObject = child.gameObject;
                MakeOpaque(existingObject);
                break;
            }
            basePosition = pendingBaseImage.transform.position;
        }
        else
        {
            existingObject = GameObject.Find($"VirtualPreview_{objectName}");
            if (existingObject != null)
            {
                MakeOpaque(existingObject);
            }
            basePosition = virtualStackBasePosition;
            virtualStackInitialized = true;
        }
        
        CreateStackElement(pendingBaseImage, 0, objectName, true, existingObject);
        CreateContainerBoundaries();
        
        currentState = StackState.StackReady;
        
        if (detectionFeedbackText != null)
        {
            if (currentMode == InteractionMode.Virtual)
            {
                detectionFeedbackText.text = $"Stack Ready! Using {virtualObjectType.ToUpper()} nodes";
            }
            else
            {
                detectionFeedbackText.text = "Stack Ready!";
            }
            detectionFeedbackText.color = detectingColor;
        }
        
        UpdateInstructions();
        pendingBaseImage = null;
    }
    
    void ShowPushObjectPreview(string objectName, ARTrackedImage trackedImage)
    {
        pendingPushImage = trackedImage;
        pendingPushObjectName = objectName;
        
        Vector3 expectedPosition = CalculatePositionAtTop();
        GameObject previewObj = InstantiateObject(objectName, trackedImage);
        
        Renderer[] renderers = previewObj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Color color = rend.material.color;
            color.a = 0.7f;
            rend.material.color = color;
        }
        
        currentState = StackState.WaitingToConfirmPush;
        
        if (instructionText != null)
        {
            instructionText.text = $"Tap the {objectName.ToUpper()} QR to PUSH";
        }
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Tap to confirm";
            detectionFeedbackText.color = Color.yellow;
        }
    }
    
    void ConfirmPushElement()
    {
        currentState = StackState.AnimatingPush;
        ShowTopIndicator();
        PlaySound(pushSound);
        
        GameObject existingObject = null;
        Vector3 position;
        string objectName;
        
        if (currentMode == InteractionMode.Physical)
        {
            if (pendingPushImage == null) return;
            
            Transform previewTransform = pendingPushImage.transform;
            foreach (Transform child in previewTransform)
            {
                existingObject = child.gameObject;
                MakeOpaque(existingObject);
                break;
            }
            position = pendingPushImage.transform.position;
            objectName = pendingPushObjectName;
        }
        else
        {
            objectName = virtualObjectType;
            position = CalculatePositionAtTop();
            existingObject = InstantiateObjectVirtual(virtualObjectType, position);
        }
        
        int newIndex = allElements.Count;
        
        // Animate shift for virtual mode
        if (currentMode == InteractionMode.Virtual && allElements.Count > 0)
        {
            StartCoroutine(AnimateStackShift(() => {
                CreateStackElement(null, newIndex, objectName, true, existingObject);
                CompletePushOperation();
            }));
        }
        else
        {
            CreateStackElement(
                (currentMode == InteractionMode.Physical) ? pendingPushImage : null,
                newIndex,
                objectName,
                true,
                existingObject
            );
            CompletePushOperation();
        }
    }
    
    void CompletePushOperation()
    {
        if (analysisManager != null)
        {
            analysisManager.AnalyzePushOperation(allElements.Count);
        }
        
        UpdateContainerBoundaries();
        
        if (statusText != null)
            statusText.text = $"Stack Size: {allElements.Count}";
        
        currentState = StackState.StackReady;
        if (instructionText != null)
            instructionText.text = $"Pushed {(currentMode == InteractionMode.Physical ? pendingPushObjectName : virtualObjectType).ToUpper()} onto stack!";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Push confirmed!";
            detectionFeedbackText.color = detectingColor;
        }
        
        StartCoroutine(HideTopIndicatorAfterDelay(1.5f));
        
        pendingPushObjectName = "";
        pendingPushImage = null;
    }
    
    IEnumerator AnimateStackShift(System.Action onComplete)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> endPositions = new List<Vector3>();
        
        // Store start positions
        for (int i = 0; i < allElements.Count; i++)
        {
            startPositions.Add(allElements[i].position);
            endPositions.Add(CalculatePositionAtIndex(i));
        }
        
        // Animate
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth easing
            
            for (int i = 0; i < allElements.Count; i++)
            {
                Vector3 newPos = Vector3.Lerp(startPositions[i], endPositions[i], t);
                allElements[i].position = newPos;
                
                if (allElements[i].objectInstance != null)
                {
                    allElements[i].objectInstance.transform.position = newPos + Vector3.up * 0.08f;
                }
                
                if (allElements[i].indexLabel != null)
                {
                    allElements[i].indexLabel.transform.position = newPos + Vector3.up * 0.25f;
                }
            }
            
            yield return null;
        }
        
        // Ensure final positions
        for (int i = 0; i < allElements.Count; i++)
        {
            allElements[i].position = endPositions[i];
            if (allElements[i].objectInstance != null)
            {
                allElements[i].objectInstance.transform.position = endPositions[i] + Vector3.up * 0.08f;
            }
            if (allElements[i].indexLabel != null)
            {
                allElements[i].indexLabel.transform.position = endPositions[i] + Vector3.up * 0.25f;
            }
        }
        
        onComplete?.Invoke();
    }
    
    void UpdateTrackedObject(ARTrackedImage trackedImage)
    {
        if (currentMode != InteractionMode.Physical) return;
        
        foreach (var element in allElements)
        {
            if (element.trackedImage == trackedImage && element.isConfirmed && !element.isVirtual)
            {
                // Update the base position if this is the base element
                if (element.index == 0)
                {
                    basePosition = trackedImage.transform.position;
                    
                    // Recalculate all positions when base moves
                    for (int i = 0; i < allElements.Count; i++)
                    {
                        Vector3 newPos = CalculatePositionAtIndex(i);
                        allElements[i].position = newPos;
                        
                        if (allElements[i].objectInstance != null)
                        {
                            allElements[i].objectInstance.transform.position = newPos + Vector3.up * 0.05f;
                        }
                        
                        if (allElements[i].indexLabel != null)
                        {
                            allElements[i].indexLabel.transform.position = newPos + Vector3.up * 0.25f;
                        }
                    }
                    
                    UpdateContainerBoundaries();
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
        
        // Don't parent to tracked image - position independently
        obj.transform.position = trackedImage.transform.position + Vector3.up * 0.05f;
        obj.transform.rotation = Quaternion.identity;
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
    
    void CreateStackElement(ARTrackedImage trackedImage, int index, string objectName, bool confirmed, GameObject existingObject = null)
    {
        Vector3 position;
        
        if (currentMode == InteractionMode.Physical)
        {
            position = trackedImage != null ? trackedImage.transform.position : CalculatePositionAtIndex(index);
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
        
        StackElement element = new StackElement
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
            element.indexLabel.transform.position = position + Vector3.up * 0.25f;
            
            TextMeshPro labelText = element.indexLabel.GetComponentInChildren<TextMeshPro>();
            if (labelText != null)
            {
                labelText.text = $"[{index}]";
            }
        }
        
        stackElements.Push(element);
        allElements.Add(element);
        
        if (statusText != null)
            statusText.text = $"Stack Size: {allElements.Count}";
    }
    
    
    void CreateContainerBoundaries()
    {
        // Clean up old boundaries
        if (leftBoundary != null) Destroy(leftBoundary);
        if (rightBoundary != null) Destroy(rightBoundary);
        
        if (allElements.Count == 0) return;
        
        float containerHeight = (currentMode == InteractionMode.Physical) ? 0.2f : 0.3f;
        
        // Calculate boundary positions
        Vector3 leftPos, rightPos;
        
        if (currentMode == InteractionMode.Physical)
        {
            // Horizontal stack - left and right boundaries
            leftPos = basePosition + Camera.main.transform.right * (-0.08f);
            rightPos = CalculatePositionAtTop() + Camera.main.transform.right * 0.08f;
        }
        else
        {
            // Vertical stack - left and right boundaries around the column
            leftPos = basePosition + Camera.main.transform.right * (-0.08f);
            rightPos = basePosition + Camera.main.transform.right * 0.08f;
        }
        
        // Create left boundary
        leftBoundary = CreateBoundaryLine(leftPos, containerHeight);
        
        // Create right boundary
        rightBoundary = CreateBoundaryLine(rightPos, containerHeight);
    }
    
    GameObject CreateBoundaryLine(Vector3 position, float height)
    {
        GameObject lineObj = new GameObject("BoundaryLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        
        lr.material = new Material(Shader.Find("Unlit/Color"));
        lr.material.color = new Color(0, 0.8f, 1f, 0.6f);
        lr.startColor = new Color(0, 0.8f, 1f, 0.6f);
        lr.endColor = new Color(0, 0.8f, 1f, 0.6f);
        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;
        lr.positionCount = 2;
        
        float heightOffset = (currentMode == InteractionMode.Physical) ? 0.01f : 0.08f;
        
        lr.SetPosition(0, position + Vector3.up * heightOffset);
        lr.SetPosition(1, position + Vector3.up * (heightOffset + height));
        lr.useWorldSpace = true;
        
        return lineObj;
    }
    
    void UpdateContainerBoundaries()
    {
        CreateContainerBoundaries();
    }
    
    void ShowTopIndicator()
    {
        if (topIndicator == null)
        {
            topIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            topIndicator.transform.localScale = Vector3.one * 0.05f;
            
            Renderer rend = topIndicator.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = topIndicatorColor;
            rend.material = mat;
            
            Collider col = topIndicator.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }
        
        topIndicator.SetActive(true);
        UpdateTopIndicatorPosition();
    }
    
    void HideTopIndicator()
    {
        if (topIndicator != null)
        {
            topIndicator.SetActive(false);
        }
    }
    
    IEnumerator HideTopIndicatorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideTopIndicator();
    }
    
    void UpdateTopIndicatorPosition()
    {
        if (topIndicator == null) return;
        
        if (allElements.Count > 0)
        {
            Vector3 topPos = allElements[allElements.Count - 1].position;
            
            if (currentMode == InteractionMode.Physical)
            {
                // Place indicator to the RIGHT of the last element
                topIndicator.transform.position = CalculatePositionAtTop() + Vector3.up * 0.15f;
            }
            else
            {
                // Place indicator above the virtual object
                topIndicator.transform.position = topPos + Vector3.up * 0.15f;
            }
        }
        else
        {
            // If empty, show at base position
            topIndicator.transform.position = basePosition + Vector3.up * 0.15f;
        }
    }
    
    Vector3 CalculatePositionAtIndex(int index)
    {
        if (currentMode == InteractionMode.Physical)
        {
            // Physical mode: Stack horizontally to the RIGHT
            return basePosition + Camera.main.transform.right * (elementSpacing * index);
        }
        else
        {
            // Virtual mode: Stack vertically upward
            return basePosition + Vector3.up * (0.12f * index);
        }
    }
    
    Vector3 CalculatePositionAtTop()
    {
        return CalculatePositionAtIndex(allElements.Count);
    }
    
    public void SimulatePush()
    {
        if (currentState != StackState.StackReady) return;
        
        if (allElements.Count >= maxCapacity)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "Stack is full! (Stack Overflow)";
            return;
        }
        
        if (currentMode == InteractionMode.Virtual)
        {
            currentState = StackState.WaitingToConfirmPush;
            ShowTopIndicator();
            
            if (instructionText != null)
                instructionText.text = $"Tap to PUSH {virtualObjectType.ToUpper()} onto stack";
            
            if (detectionFeedbackText != null)
            {
                detectionFeedbackText.text = "Tap to confirm push";
                detectionFeedbackText.color = Color.yellow;
            }
        }
        else
        {
            currentState = StackState.WaitingForPushObject;
            ShowTopIndicator();
            
            if (instructionText != null)
                instructionText.text = "Place new QR code to the RIGHT of the last QR code";
            
            if (detectionFeedbackText != null)
            {
                detectionFeedbackText.text = "Scan QR code";
                detectionFeedbackText.color = notDetectingColor;
            }
        }
    }
    
    public void SimulatePop()
    {
        if (currentState != StackState.StackReady) return;
        
        if (allElements.Count == 0)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "Stack is empty! (Stack Underflow)";
            return;
        }
        
        currentState = StackState.AnimatingPop;
        ShowTopIndicator();
        PlaySound(popSound);
        
        StackElement topElement = stackElements.Pop();
        allElements.RemoveAt(allElements.Count - 1);
        
        if (topElement.indexLabel != null) Destroy(topElement.indexLabel);
        if (topElement.objectInstance != null) Destroy(topElement.objectInstance);
        
        // Animate shift for virtual mode
        if (currentMode == InteractionMode.Virtual && allElements.Count > 0)
        {
            StartCoroutine(AnimateStackShift(() => {
                CompletePopOperation();
            }));
        }
        else
        {
            CompletePopOperation();
        }
    }
    
    void CompletePopOperation()
    {
        if (analysisManager != null)
        {
            analysisManager.AnalyzePopOperation(allElements.Count + 1);
        }
        
        UpdateContainerBoundaries();
        
        if (statusText != null)
            statusText.text = $"Stack Size: {allElements.Count}";
        
        currentState = StackState.StackReady;
        
        if (instructionText != null)
            instructionText.text = $"Popped element from stack!";
        
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Pop complete!";
            detectionFeedbackText.color = detectingColor;
        }
        
        StartCoroutine(HideTopIndicatorAfterDelay(1.5f));
    }
    
    public void SimulatePeek()
    {
        if (currentState != StackState.StackReady) return;
        
        if (allElements.Count == 0)
        {
            PlaySound(errorSound);
            if (instructionText != null)
                instructionText.text = "Stack is empty!";
            return;
        }
        
        PlaySound(peekSound);
        
        StackElement topElement = stackElements.Peek();
        
        if (analysisManager != null)
        {
            analysisManager.AnalyzePeekOperation(allElements.Count);
        }
        
        if (instructionText != null)
            instructionText.text = $"Top element: {topElement.objectName.ToUpper()} at [{topElement.index}]";
        
        StartCoroutine(HighlightElement(allElements.Count - 1));
    }
    
    System.Collections.IEnumerator HighlightElement(int index)
    {
        if (index >= 0 && index < allElements.Count)
        {
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            highlight.transform.position = allElements[index].position + Vector3.up * 0.12f;
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
    
    void UpdateInstructions()
    {
        if (instructionText == null) return;
        
        switch (currentState)
        {
            case StackState.ModeSelection:
                instructionText.text = "Select interaction mode";
                break;
                
            case StackState.WaitingForBaseObject:
                if (currentMode == InteractionMode.Physical)
                    instructionText.text = "Scan BASE object's QR code (place on floor)";
                else
                    instructionText.text = "Scan ONE QR to define all stack nodes";
                break;
                
            case StackState.WaitingToConfirmBase:
                instructionText.text = "TAP to confirm base";
                break;
                
            case StackState.StackReady:
                if (currentMode == InteractionMode.Physical)
                    instructionText.text = "Stack ready! Place QRs to the RIGHT of each other";
                else
                    instructionText.text = "Stack ready! Use PUSH, POP, or PEEK";
                break;
                
            case StackState.WaitingForPushObject:
                instructionText.text = "Place new QR to the RIGHT of the last QR";
                break;
                
            case StackState.WaitingToConfirmPush:
                instructionText.text = "TAP to confirm PUSH";
                break;
        }
    }
    
    public void ResetStack()
    {
        // Destroy all stack elements thoroughly
        foreach (var element in allElements)
        {
            if (element.indexLabel != null)
            {
                Destroy(element.indexLabel);
                element.indexLabel = null;
            }
            
            if (element.objectInstance != null)
            {
                Destroy(element.objectInstance);
                element.objectInstance = null;
            }
        }
        
        // Clear collections
        stackElements.Clear();
        allElements.Clear();
        
        // Destroy visual elements
        if (stackBase != null)
        {
            Destroy(stackBase);
            stackBase = null;
        }
        
        if (topIndicator != null)
        {
            Destroy(topIndicator);
            topIndicator = null;
        }
        
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
        
        // Clean up any preview objects that might be attached to tracked images
        if (pendingBaseImage != null)
        {
            foreach (Transform child in pendingBaseImage.transform)
            {
                Destroy(child.gameObject);
            }
        }
        
        if (pendingPushImage != null)
        {
            foreach (Transform child in pendingPushImage.transform)
            {
                Destroy(child.gameObject);
            }
        }
        
        // Clean up ALL tracked image children (in case any objects are still parented)
        foreach (var kvp in trackedQRCodes)
        {
            if (kvp.Value != null && kvp.Value.transform != null)
            {
                // Destroy all children of tracked images
                foreach (Transform child in kvp.Value.transform)
                {
                    if (child != null && child.gameObject != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }
        
        // Find and destroy any orphaned virtual objects
        GameObject[] virtualObjects = GameObject.FindGameObjectsWithTag("Untagged");
        foreach (GameObject obj in virtualObjects)
        {
            if (obj.name.Contains("VirtualPreview_") || 
                obj.name.Contains("BoundaryLine") ||
                obj.name.Contains("IndexLabel") ||
                obj.name == "PointerArrow" ||
                obj.name == "TopIndicator")
            {
                Destroy(obj);
            }
        }
        
        // Clear tracking data
        trackedQRCodes.Clear();
        processedImages.Clear();
        
        // Reset pending references
        pendingBaseImage = null;
        pendingPushImage = null;
        pendingPushObjectName = "";
        
        // Reset virtual mode data
        virtualObjectType = "";
        virtualStackInitialized = false;
        
        // Reset state
        currentState = StackState.ModeSelection;
        
        // Update UI
        if (detectionFeedbackText != null)
        {
            detectionFeedbackText.text = "Choose a mode";
            detectionFeedbackText.color = Color.yellow;
        }
        
        if (statusText != null)
            statusText.text = "Stack Size: 0";
        
        UpdateInstructions();
        
        // Reset analysis
        if (analysisManager != null)
        {
            analysisManager.ResetCounters();
        }
        
        Debug.Log("Stack reset completed - all objects destroyed");
    }
    
    public int GetStackSize()
    {
        return allElements.Count;
    }
    
    public bool IsStackReady()
    {
        return currentState == StackState.StackReady;
    }
    
    public bool IsModeSelected()
    {
        return currentState != StackState.ModeSelection;
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