using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Networking;
using TMPro;

public class InteractiveCoffeeQueue : MonoBehaviour
{
    [Header("AR Components")]
    public ARPlaneManager    planeManager;
    public ARRaycastManager  raycastManager;
    public Camera            arCamera;

    [Header("Zoom Controller")]
    public SceneZoomController zoomController;

    [Header("Plane Visualization")]
    public GameObject planePrefab;

    [Header("Custom Assets - Coffee Shop (Optional)")]
    public GameObject counterPrefab;
    public GameObject baristaPrefab;
    public GameObject customerPrefab;

    [Header("Custom Assets - Conveyor Belt (Optional)")]
    public GameObject scannerPrefab;
    public GameObject workerPrefab;
    public GameObject packagePrefab;

    [Header("Custom Assets - Hospital ER (Optional)")]
    public GameObject receptionDeskPrefab;
    public GameObject nursePrefab;
    public GameObject patientPrefab;

    [Header("UI References - Shared")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI detectionText;
    public TextMeshProUGUI operationInfoText;
    public GameObject      explanationPanel;

    [Header("Scenario Panel")]
    public GameObject scenarioPanel;
    public UnityEngine.UI.Button scenarioButton1;
    public UnityEngine.UI.Button scenarioButton2;

    [Header("Difficulty Panel")]
    public GameObject            difficultyPanel;
    public UnityEngine.UI.Button beginnerBtn;
    public UnityEngine.UI.Button intermediateBtn;

    [Header("Beginner Button Panel")]
    public GameObject beginnerButtonPanel;

    [Header("Intermediate Button Panel")]
    public GameObject     intermediateButtonPanel;
    public TMP_InputField priorityInputField;

    [Header("Beginner Action Buttons")]
    public UnityEngine.UI.Button beginnerEnqueueButton;
    public UnityEngine.UI.Button beginnerDequeueButton;
    public UnityEngine.UI.Button beginnerPeekButton;

    [Header("Intermediate Action Buttons")]
    public UnityEngine.UI.Button intermediateEnqueueButton;
    public UnityEngine.UI.Button intermediateDequeueButton;
    public UnityEngine.UI.Button intermediatePriorityEnqueueButton;

    [Header("Movement Control Panel")]
    public GameObject            movementControlPanel;
    public GameObject            confirmButton;

    [Header("Movement Control - Joystick")]
    public RectTransform         joystickContainer;
    public UnityEngine.UI.Button cancelButton;

    [Header("Audio")]
    public AudioClip placeSceneSound;
    public AudioClip customerJoinSound;
    public AudioClip customerServedSound;
    public AudioClip peekSound;
    public AudioClip moveSound;

    [Header("Swipe Rotation")]
    public SwipeRotation swipeRotation;

    [Header("Tutorial System")]
    public QueueTutorialIntegration tutorialIntegration;

    [Header("Server Config")]
    public string serverUrl = "https://structureality-admin.onrender.com";
    public string dataStructure = "Queue";
    public string[] fallbackScenarios = new string[] { "CoffeeShop", "ConveyorBelt" };

    // =========================================================================
    // SCENARIO ENUM
    // =========================================================================
    public enum ScenarioMode { None, CoffeeShop, ConveyorBelt, Hospital }

    private static readonly (string id, string label, ScenarioMode mode)[] ScenarioMeta =
    {
        ("CoffeeShop",   "Coffee Shop",   ScenarioMode.CoffeeShop),
        ("ConveyorBelt", "Conveyor Belt", ScenarioMode.ConveyorBelt),
        ("Hospital",     "Hospital ER",   ScenarioMode.Hospital),
    };

    // =========================================================================
    // SETTINGS
    // =========================================================================
    [Header("Queue Settings")]
    public int   maxQueueSize             = 8;
    public float customerSpacing          = 0.10f;
    public float moveSpeed                = 1.5f;
    public float confirmDistanceThreshold = 0.12f;
    public float sceneHeightOffset        = 0.05f;

    private const float Z_SLOT_ZERO = 0.00f;
    private const float Z_SCANNER   = -0.14f;
    private const float QUEUE_Y     =  0.005f;
    private const float WORKER_GAP  =  0.08f;

    // =========================================================================
    // PRIVATE ENUMS & STATE
    // =========================================================================
    private enum DifficultyMode { None, Beginner, Intermediate }
    private enum QueueState
    {
        ChoosingScenario, ChoosingDifficulty, WaitingForPlane,
        Ready, MovingNewCustomer, MovingCustomerOut, PriorityEnqueuing
    }

    private AudioSource    audioSource;
    private ScenarioMode   currentScenario   = ScenarioMode.None;
    private DifficultyMode currentDifficulty = DifficultyMode.None;
    private QueueState     currentState      = QueueState.ChoosingScenario;
    private string[]       activeScenarioIds = null;

    // =========================================================================
    // UI SYNC FIX: silence flag
    // =========================================================================
    private bool _silenceInstructions = false;

    /// <summary>
    /// Called by ARQueueLessonGuide to stop the controller overwriting
    /// the guide card's own text during guided lessons.
    /// Hides/shows the root panels (two levels up from each TMP).
    /// </summary>
    public void SetInstructionSilence(bool silent)
    {
        _silenceInstructions = silent;
        SetTMPPanelActive(instructionText,   !silent);
        SetTMPPanelActive(operationInfoText, !silent);
        SetTMPPanelActive(detectionText,     !silent);
        SetTMPPanelActive(statusText,        !silent);
    }

    void SetTMPPanelActive(TextMeshProUGUI tmp, bool active)
    {
        if (tmp == null) return;
        Transform t = tmp.transform.parent;
        GameObject root = (t != null && t.parent != null) ? t.parent.gameObject
                        : (t != null)                     ? t.gameObject
                                                          : tmp.gameObject;
        if (root != null) root.SetActive(active);
    }

    // =========================================================================
    // BUTTON COLOUR MANAGEMENT
    // =========================================================================
    private Coroutine pulseCoroutine = null;
    private readonly Dictionary<UnityEngine.UI.Button, Color> originalButtonColors
        = new Dictionary<UnityEngine.UI.Button, Color>();
    private static readonly Color BASE_BTN_COLOR  = new Color(0x84/255f, 0x69/255f, 0xFF/255f, 1f);
    private static readonly Color LIGHT_BTN_COLOR = new Color(0xB2/255f, 0xA0/255f, 0xFF/255f, 1f);

    // Scene objects
    private Vector3    sceneSpawnPosition;
    private GameObject coffeeShopScene;
    private GameObject counter;
    private GameObject barista;
    private GameObject frontMarker, rearMarker;
    private GameObject targetPositionIndicator;

    // Movement
    private GameObject movingCustomer;
    private Vector3    targetPosition;
    private bool       isEnqueueMode           = false;
    private Vector3    currentMovementDirection = Vector3.zero;
    private bool       isMoving                 = false;

    // Joystick
    private UnityEngine.UI.Image joystickBackground, joystickHandle;
    private Vector2 joystickInputVector = Vector2.zero;
    private bool    isDraggingJoystick  = false;
    private float   joystickRadius      = 75f;

    // Misc
    private bool    sceneSpawned             = false;
    private int     customerIdCounter        = 1;
    private bool    hasShownJoystickTutorial = false;
    private Vector3 originalEntityScale      = Vector3.one;

    private class Customer
    {
        public GameObject                  gameObject;
        public int                         queuePosition;
        public string                      customerId;
        public int                         priority;
        public GameObject                  thoughtBubble;
        public GameObject                  numberLabel;
        public CustomerAnimationController animController;
    }
    private readonly List<Customer> customerQueue = new List<Customer>();

    private readonly Color[] coffeeShopColors = {
        new Color(0.2f,0.5f,1f),  new Color(1f,0.3f,0.3f), new Color(0.3f,1f,0.3f),  new Color(1f,0.8f,0.2f),
        new Color(0.8f,0.2f,1f),  new Color(1f,0.5f,0f),   new Color(0f,0.8f,0.8f),  new Color(1f,0.4f,0.7f)
    };
    private readonly Color[] conveyorBeltColors = {
        new Color(0.76f,0.57f,0.32f), new Color(0.25f,0.55f,0.90f), new Color(0.90f,0.25f,0.25f), new Color(0.25f,0.80f,0.35f),
        new Color(0.95f,0.75f,0.10f), new Color(0.60f,0.35f,0.70f), new Color(0.95f,0.50f,0.10f), new Color(0.85f,0.85f,0.85f)
    };
    private readonly Color[] hospitalColors = {
        new Color(0.90f,0.10f,0.10f), new Color(0.95f,0.48f,0.05f), new Color(0.95f,0.85f,0.10f), new Color(0.15f,0.78f,0.25f),
        new Color(0.20f,0.55f,0.90f), new Color(0.68f,0.28f,0.80f), new Color(0.85f,0.85f,0.85f), new Color(0.95f,0.60f,0.70f)
    };

    // =========================================================================
    // SCENARIO HELPERS
    // =========================================================================
    bool IsConveyor() => currentScenario == ScenarioMode.ConveyorBelt;
    bool IsHospital() => currentScenario == ScenarioMode.Hospital;
    bool IsInstant()  => IsConveyor() || IsHospital();

    string PersonLabel()  => IsHospital() ? "Patient"          : IsConveyor() ? "Package"        : "Customer";
    string ServiceLabel() => IsHospital() ? "Treat"            : IsConveyor() ? "Scan & dispatch" : "Serve coffee";
    string FrontLabel()   => IsHospital() ? "TREATMENT"        : IsConveyor() ? "SCAN"            : "FRONT";
    string RearLabel()    => IsHospital() ? "TRIAGE"           : IsConveyor() ? "INTAKE"          : "REAR";
    string ScenarioName() => IsHospital() ? "Hospital ER"      : IsConveyor() ? "Conveyor Belt"   : "Coffee Shop";
    string ScenarioTag()  => IsHospital() ? "Hospital"         : IsConveyor() ? "Conveyor"        : "Coffee";

    string[] ThoughtPhrases() =>
        IsHospital() ? new[] { "Help me!", "I'll be okay...", "Is it serious?"   } :
        IsConveyor() ? new[] { "Handle with care!", "Fragile!", "Express!"        } :
                       new[] { "Almost there!", "Need coffee...", "Worth the wait!" };

    string EnqueuedThought() =>
        IsHospital() ? "I'm registered!" : IsConveyor() ? "Ready to ship!" : "Finally here!";

    float LabelY()        => IsInstant() ? 0.10f : 0.22f;
    float ThoughtLabelY() => IsInstant() ? 0.17f : 0.30f;

    Vector3 GetQueuePosition(int i) => new Vector3(0f, QUEUE_Y, Z_SLOT_ZERO + i * customerSpacing);

    float ZoomAdjustedMoveSpeed()
    { float s = coffeeShopScene ? coffeeShopScene.transform.localScale.x : 1f; return moveSpeed / Mathf.Max(s, 0.01f); }
    float ZoomAdjustedThreshold()
    { float s = coffeeShopScene ? coffeeShopScene.transform.localScale.x : 1f; return confirmDistanceThreshold / Mathf.Max(s, 0.01f); }

    // GATED instruction helpers — no-op while _silenceInstructions is true
    void UpdateInstructions(string m)        { if (_silenceInstructions) return; SetIC(m, Color.white); }
    void UpdateInstructionsSuccess(string m) { if (_silenceInstructions) return; SetIC(m, new Color(0.2f, 1f, 0.3f)); }
    void UpdateInstructionsError(string m)   { if (_silenceInstructions) return; SetIC(m, new Color(1f, 0.25f, 0.25f)); }
    void SetIC(string m, Color c) { if (instructionText) { instructionText.text = m; instructionText.color = c; } }

    void CacheAllButtonColors()
    {
        UnityEngine.UI.Button[] btns = {
            beginnerEnqueueButton, beginnerDequeueButton, beginnerPeekButton,
            intermediateEnqueueButton, intermediateDequeueButton, intermediatePriorityEnqueueButton
        };
        foreach (var b in btns)
        {
            if (!b) continue;
            var img = b.GetComponent<UnityEngine.UI.Image>();
            if (img && !originalButtonColors.ContainsKey(b)) originalButtonColors[b] = img.color;
        }
    }

    void CheckAndUpdateButtonStates()
    {
        bool has = customerQueue.Count > 0;
        if (has && pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; ResetEnqueueButtonVisual(); }
        SetBtnState(beginnerDequeueButton,             has);
        SetBtnState(beginnerPeekButton,                has);
        SetBtnState(intermediateDequeueButton,         has);
        SetBtnState(intermediatePriorityEnqueueButton, has);
        if (!has && pulseCoroutine == null && sceneSpawned)
            pulseCoroutine = StartCoroutine(PulseEnqueueButton());
    }

    void SetBtnState(UnityEngine.UI.Button b, bool on)
    {
        if (!b) return;
        var img = b.GetComponent<UnityEngine.UI.Image>();
        if (img && originalButtonColors.ContainsKey(b))
            img.color = on ? originalButtonColors[b] : new Color(0.55f, 0.55f, 0.55f, 0.7f);
        b.interactable = on;
    }

    IEnumerator PulseEnqueueButton()
    {
        float speed = 2.5f, elapsed = 0f;
        while (true)
        {
            elapsed += Time.deltaTime * speed;
            float t = (Mathf.Sin(elapsed) + 1f) * 0.5f;
            Pulse(beginnerEnqueueButton,     t);
            Pulse(intermediateEnqueueButton, t);
            yield return null;
        }
    }
    void Pulse(UnityEngine.UI.Button b, float t)
    {
        if (!b || !b.gameObject.activeInHierarchy) return;
        b.transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.10f, t);
        var img = b.GetComponent<UnityEngine.UI.Image>();
        if (img) img.color = Color.Lerp(BASE_BTN_COLOR, LIGHT_BTN_COLOR, t);
    }
    void ResetEnqueueButtonVisual() { ResetPBtn(beginnerEnqueueButton); ResetPBtn(intermediateEnqueueButton); }
    void ResetPBtn(UnityEngine.UI.Button b)
    {
        if (!b) return; b.transform.localScale = Vector3.one;
        var img = b.GetComponent<UnityEngine.UI.Image>();
        if (img) img.color = originalButtonColors.ContainsKey(b) ? originalButtonColors[b] : BASE_BTN_COLOR;
    }

    private bool _started = false;

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================
    void Start()
    {
        if (_started) return;
        _started = true;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false; audioSource.spatialBlend = 0f;
        if (!arCamera) arCamera = Camera.main;
        if (planeManager && planePrefab) planeManager.planePrefab = planePrefab;

        CacheAllButtonColors();
        SetAllPanelsHidden();
        SetupJoystick();
        WireUpDifficultyButtons();

        if (cancelButton)   cancelButton  .onClick.AddListener(CancelMovement);
        if (planeManager)   planeManager  .enabled = false;
        if (raycastManager) raycastManager.enabled = false;

        if (scenarioPanel)   scenarioPanel  .SetActive(true);
        if (scenarioButton1) scenarioButton1.gameObject.SetActive(false);
        if (scenarioButton2) scenarioButton2.gameObject.SetActive(false);

        UpdateInstructions("Loading scenarios...");
        StartCoroutine(FetchAndApplyScenarios());
    }

    void Update()
    {
        if      (currentState == QueueState.WaitingForPlane)
            DetectPlaneInteraction();
        else if (currentState == QueueState.MovingNewCustomer ||
                 currentState == QueueState.MovingCustomerOut  ||
                 currentState == QueueState.PriorityEnqueuing)
        {
            if (isMoving && currentMovementDirection != Vector3.zero) MoveCustomerContinuous();
            CheckConfirmDistance();
        }
    }

    // =========================================================================
    // SERVER FETCH
    // =========================================================================
    [Serializable] private class ScenarioResponse { public bool success; public string[] scenarios; }

    IEnumerator FetchAndApplyScenarios()
    {
        string url = $"{serverUrl}/api/scenarios/active?ds={dataStructure}";
        Debug.Log($"[InteractiveCoffeeQueue] Fetching: {url}");
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var r = JsonUtility.FromJson<ScenarioResponse>(req.downloadHandler.text);
                    activeScenarioIds = (r != null && r.success && r.scenarios?.Length > 0) ? r.scenarios : fallbackScenarios;
                }
                catch { activeScenarioIds = fallbackScenarios; }
            }
            else { Debug.LogWarning($"[InteractiveCoffeeQueue] {req.error} - using fallback"); activeScenarioIds = fallbackScenarios; }
        }
        ConfigureScenarioButtons();
    }

    void ConfigureScenarioButtons()
    {
        if (activeScenarioIds.Length >= 1) SetupScenarioButton(scenarioButton1, activeScenarioIds[0]);
        if (activeScenarioIds.Length >= 2) SetupScenarioButton(scenarioButton2, activeScenarioIds[1]);
        else if (scenarioButton2) scenarioButton2.gameObject.SetActive(false);
        UpdateInstructions("Choose your queue scenario!");
        if (detectionText) { detectionText.text = "Select a scenario first"; detectionText.color = Color.white; }
        Debug.Log($"[InteractiveCoffeeQueue] Buttons ready: [{string.Join(", ", activeScenarioIds)}]");
    }

    void SetupScenarioButton(UnityEngine.UI.Button btn, string id)
    {
        if (!btn) return;
        btn.gameObject.SetActive(true);
        string label = id; ScenarioMode mode = ScenarioMode.None;
        foreach (var m in ScenarioMeta) if (m.id == id) { label = m.label; mode = m.mode; break; }
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(); if (tmp) tmp.text = label;
        var tm  = btn.GetComponentInChildren<TextMesh>();        if (tm)  tm.text  = label;
        ScenarioMode captured = mode;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnScenarioChosen(captured));
        Debug.Log($"[InteractiveCoffeeQueue] Button wired: '{label}' -> {mode}");
    }

    public void OnScenarioChosen(ScenarioMode mode)
    {
        currentScenario = mode;
        if (scenarioPanel) scenarioPanel.SetActive(false);
        currentState = QueueState.ChoosingDifficulty;
        if (difficultyPanel) difficultyPanel.SetActive(true);
        UpdateInstructions("Choose your difficulty level!");
    }

    void SetAllPanelsHidden()
    {
        foreach (var g in new[] { scenarioPanel, difficultyPanel, beginnerButtonPanel,
                                   intermediateButtonPanel, movementControlPanel, confirmButton, explanationPanel })
            if (g) g.SetActive(false);
    }

    void WireUpDifficultyButtons()
    {
        if (beginnerBtn)     beginnerBtn    .onClick.AddListener(OnSelectBeginner);
        if (intermediateBtn) intermediateBtn.onClick.AddListener(OnSelectIntermediate);
    }

    // =========================================================================
    // PRE-FILL FOR LESSON
    // =========================================================================
    public void PreFillForLesson(int count)
    {
        if (coffeeShopScene == null || !sceneSpawned) return;
        count = Mathf.Clamp(count, 1, maxQueueSize);
        int toAdd = count - customerQueue.Count;
        if (toAdd <= 0) return;
        for (int i = 0; i < toAdd; i++)
        {
            int ci = customerQueue.Count % 8;
            GameObject e = CreateEntity(ci);
            e.transform.SetParent(coffeeShopScene.transform);
            e.transform.localScale    = originalEntityScale;
            e.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            e.transform.localPosition = GetQueuePosition(customerQueue.Count);
            var c = new Customer
            {
                gameObject    = e, queuePosition = customerQueue.Count,
                customerId    = $"C{customerIdCounter++}", priority = 5,
                animController = e.GetComponent<CustomerAnimationController>()
            };
            c.numberLabel   = CreateTextLabel(e.transform, $"[{customerQueue.Count}]",
                new Vector3(0, LabelY(), 0), Color.white, 50, new Vector3(0.002f, 0.002f, 0.002f));
            c.thoughtBubble = CreateTextLabel(e.transform, ThoughtPhrases()[customerQueue.Count % 3],
                new Vector3(0, ThoughtLabelY(), 0), Color.white, 35, new Vector3(0.002f, 0.002f, 0.002f));
            customerQueue.Add(c);
        }
        UpdateQueueMarkers(); CheckAndUpdateButtonStates(); UpdateStatus();
    }

    // =========================================================================
    // GUIDE NOTIFICATION HELPERS
    // =========================================================================
    public void NotifyGuideOfPeek()
    {
        ARQueueLessonGuide guide = FindObjectOfType<ARQueueLessonGuide>();
        if (guide != null) guide.NotifyPeekPerformed();
    }

    public void NotifyGuideOfPriorityEnqueue(int priority, int insertIndex)
    {
        ARQueueLessonGuide guide = FindObjectOfType<ARQueueLessonGuide>();
        if (guide != null) guide.NotifyPriorityEnqueuePerformed(priority, insertIndex);
    }

    void NotifyGuideOfEnqueue()
    {
        ARQueueLessonGuide guide = FindObjectOfType<ARQueueLessonGuide>();
        if (guide != null) guide.OnEnqueueConfirmed();
    }

    void NotifyGuideOfDequeue()
    {
        ARQueueLessonGuide guide = FindObjectOfType<ARQueueLessonGuide>();
        if (guide != null) guide.OnDequeueConfirmed();
    }

    // =========================================================================
    // DIFFICULTY SELECTION
    // =========================================================================
    void ShowScenarioPanelAgain()
    {
        currentState = QueueState.ChoosingScenario;
        if (scenarioPanel) scenarioPanel.SetActive(true);
        UpdateInstructions("Choose your queue scenario!");
        if (detectionText) { detectionText.text = "Select a scenario first"; detectionText.color = Color.white; }
    }

    public void OnSelectBeginner()
    {
        currentDifficulty = DifficultyMode.Beginner;
        if (difficultyPanel) difficultyPanel.SetActive(false);
        ActivatePlaneDetection();
        UpdateInstructions($"Beginner - {ScenarioName()}! Point camera at a flat surface.");
        if (operationInfoText)
            operationInfoText.text =
                $"BEGINNER  {ScenarioTag()}\n\n" +
                $"ENQUEUE - Add {PersonLabel()} to {RearLabel()}\n" +
                $"DEQUEUE - Remove from {FrontLabel()}\n" +
                $"PEEK    - View next\n\nFIFO: First In, First Out!";
    }

    public void OnSelectIntermediate()
    {
        currentDifficulty = DifficultyMode.Intermediate;
        if (difficultyPanel) difficultyPanel.SetActive(false);
        ActivatePlaneDetection();
        UpdateInstructions($"Intermediate - {ScenarioName()}! Point camera at a flat surface.");
        if (operationInfoText)
            operationInfoText.text =
                $"INTERMEDIATE  {ScenarioTag()}\n\n" +
                $"ENQUEUE          - O(1)\nDEQUEUE          - O(1)\n" +
                $"PRIORITY ENQUEUE - O(n)\n\n" +
                (IsHospital() ? "1=Critical, 9=Minor" : IsConveyor() ? "1=Urgent, 9=Standard" : "1=VIP, 9=Last");
    }

    void ActivatePlaneDetection()
    {
        if (planeManager)   planeManager  .enabled = true;
        if (raycastManager) raycastManager.enabled = true;
        currentState = QueueState.WaitingForPlane;
        if (detectionText) { detectionText.text = "Looking for surfaces..."; detectionText.color = Color.yellow; }
    }

    // =========================================================================
    // PLANE DETECTION & SCENE SPAWN
    // =========================================================================
    void DetectPlaneInteraction()
    {
        if (sceneSpawned) return;
        bool touched = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        if (!touched && !Input.GetMouseButtonDown(0)) return;
        Vector2 screenPos = touched ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
        var hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
            SpawnScene(hits[0].pose.position);
    }

    void FaceSceneTowardCamera()
    {
        if (!coffeeShopScene || !arCamera) return;
        Vector3 dir = arCamera.transform.position - coffeeShopScene.transform.position; dir.y = 0;
        if (dir.sqrMagnitude > 0.0001f) coffeeShopScene.transform.rotation = Quaternion.LookRotation(-dir.normalized, Vector3.up);
    }

    void SpawnScene(Vector3 position)
    {
        sceneSpawned = true; sceneSpawnPosition = position + Vector3.up * sceneHeightOffset;
        PlaySound(placeSceneSound);
        if (planeManager != null) foreach (var p in planeManager.trackables) p.gameObject.SetActive(false);
        coffeeShopScene = new GameObject("QueueScene");
        coffeeShopScene.transform.position = sceneSpawnPosition;
        FaceSceneTowardCamera();
        if (swipeRotation)  swipeRotation .InitializeRotation(coffeeShopScene.transform);
        if (zoomController) zoomController.InitializeZoom   (coffeeShopScene.transform);
        if      (IsHospital()) BuildHospitalERScene();
        else if (IsConveyor()) BuildConveyorBeltScene();
        else                   BuildCoffeeShopScene();
        bool beg = currentDifficulty == DifficultyMode.Beginner;
        if (beginnerButtonPanel)     beginnerButtonPanel    .SetActive(beg);
        if (intermediateButtonPanel) intermediateButtonPanel.SetActive(!beg);
        if (explanationPanel)        explanationPanel       .SetActive(true);
        currentState = QueueState.Ready;
        CheckAndUpdateButtonStates();
        UpdateInstructions($"{ScenarioName()} ready! Tap ENQUEUE to add your first {PersonLabel().ToLower()}.");
        if (detectionText) { detectionText.text = "Scene Placed!"; detectionText.color = Color.green; }
        if (tutorialIntegration) Invoke(nameof(DelayedTutorial), 1f);
    }
    void DelayedTutorial() { if (tutorialIntegration) tutorialIntegration.ShowWelcomeTutorial(); }

    // =========================================================================
    // BUILD SCENES
    // =========================================================================
    void BuildCoffeeShopScene()   { CreateQueueMarkers(); CreateCounter();        CreateBarista();       UpdateStatus(); }
    void BuildConveyorBeltScene() { FaceSceneTowardCamera(); CreateQueueMarkers(); CreateScannerStation(); CreateConveyorBelt(); CreateWarehouseWorker(); UpdateStatus(); }
    void BuildHospitalERScene()   { FaceSceneTowardCamera(); CreateQueueMarkers(); CreateReceptionDesk(); CreateTriageNurse();  CreateWaitingBench();    UpdateStatus(); }

    void CreateQueueMarkers()
    {
        frontMarker = CreateMarker(new Vector3(0, 0.02f, Z_SLOT_ZERO - 0.01f), FrontLabel(), Color.green, new Color(0,1,0,0.8f));
        rearMarker  = CreateMarker(Vector3.zero, RearLabel(), new Color(1,0.55f,0), new Color(1,0.55f,0,0.8f));
        UpdateQueueMarkers();
    }
    void UpdateQueueMarkers()
    { if (rearMarker) rearMarker.transform.localPosition = new Vector3(0, 0.02f, Z_SLOT_ZERO + customerQueue.Count * customerSpacing); }

    GameObject CreateMarker(Vector3 lp, string label, Color tc, Color dc)
    {
        var m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        m.transform.SetParent(coffeeShopScene.transform); m.transform.localPosition = lp; m.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
        var mat = new Material(Shader.Find("Unlit/Color")); mat.color = dc; m.GetComponent<Renderer>().material = mat; Destroy(m.GetComponent<Collider>());
        CreateTextLabel(m.transform, label, new Vector3(0, 0.08f, 0), tc, 40, new Vector3(0.002f, 0.002f, 0.002f)); m.name = label + "Marker"; return m;
    }

    void CreateCounter()
    {
        if (counterPrefab) { counter = Instantiate(counterPrefab, coffeeShopScene.transform); counter.transform.localPosition = new Vector3(0, 0.01f, Z_SCANNER); }
        else
        {
            counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.transform.SetParent(coffeeShopScene.transform); counter.transform.localPosition = new Vector3(0, 0.01f, Z_SCANNER); counter.transform.localScale = new Vector3(0.08f, 0.10f, 0.20f);
            var mat = new Material(Shader.Find("Standard")); mat.color = new Color(0.55f, 0.27f, 0.07f); counter.GetComponent<Renderer>().material = mat; Destroy(counter.GetComponent<Collider>());
            Prim(counter.transform, PrimitiveType.Cube, new Vector3(0, 0.12f, 0), new Vector3(0.5f, 0.35f, 0.12f), new Color(0.2f, 0.2f, 0.2f));
        }
        counter.name = "Counter";
    }

    void CreateBarista()
    {
        float bz = Z_SCANNER - 0.10f;
        if (baristaPrefab) { barista = Instantiate(baristaPrefab, coffeeShopScene.transform); barista.transform.localPosition = new Vector3(0, 0, bz); }
        else
        {
            barista = new GameObject("Barista"); barista.transform.SetParent(coffeeShopScene.transform); barista.transform.localPosition = new Vector3(0, 0, bz);
            var body = Prim(barista.transform, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.08f, 0.12f, 0.08f), new Color(0.18f, 0.49f, 0.2f));
            Prim(body.transform, PrimitiveType.Sphere,   new Vector3(0, 0.15f, 0), new Vector3(0.45f, 0.45f, 0.45f), new Color(0.95f, 0.87f, 0.73f));
            Prim(body.transform, PrimitiveType.Cylinder, new Vector3(0.08f, 0, 0), new Vector3(0.15f, 0.2f, 0.15f),  new Color(0.82f, 0.41f, 0.12f));
            CreateTextLabel(barista.transform, "BARISTA", new Vector3(0, 0.25f, 0.05f), Color.white, 30, new Vector3(0.002f, 0.002f, 0.002f));
        }
    }

    void CreateScannerStation()
    {
        if (scannerPrefab) { counter = Instantiate(scannerPrefab, coffeeShopScene.transform); counter.transform.localPosition = new Vector3(0, 0, Z_SCANNER); }
        else
        {
            counter = new GameObject("ScannerStation"); counter.transform.SetParent(coffeeShopScene.transform); counter.transform.localPosition = new Vector3(0, 0, Z_SCANNER);
            Prim(counter.transform, PrimitiveType.Cube, new Vector3(-0.042f, 0.075f, 0), new Vector3(0.010f, 0.15f, 0.010f), new Color(0.15f, 0.15f, 0.15f));
            Prim(counter.transform, PrimitiveType.Cube, new Vector3( 0.042f, 0.075f, 0), new Vector3(0.010f, 0.15f, 0.010f), new Color(0.15f, 0.15f, 0.15f));
            Prim(counter.transform, PrimitiveType.Cube, new Vector3(0, 0.152f, 0), new Vector3(0.094f, 0.010f, 0.010f), new Color(0.15f, 0.15f, 0.15f));
            Prim(counter.transform, PrimitiveType.Cube, new Vector3(0, 0.055f, 0), new Vector3(0.080f, 0.003f, 0.003f), Color.red, unlit:true);
            Prim(counter.transform, PrimitiveType.Cube, new Vector3(0.068f, 0.10f, -0.015f), new Vector3(0.030f, 0.040f, 0.008f), new Color(0.05f, 0.55f, 0.95f));
            CreateTextLabel(counter.transform, "SCANNER", new Vector3(0, 0.19f, 0), new Color(0.05f, 0.85f, 0.95f), 32, new Vector3(0.002f, 0.002f, 0.002f));
        }
        counter.name = "ScannerStation";
    }

    void CreateConveyorBelt()
    {
        float zStart = Z_SCANNER - 0.02f, zEnd = Z_SLOT_ZERO + maxQueueSize * customerSpacing + 0.06f;
        float zLen = zEnd - zStart, zCenter = zStart + zLen * 0.5f;
        Prim(coffeeShopScene.transform, PrimitiveType.Cube, new Vector3(0, -0.005f, zCenter), new Vector3(0.07f, 0.008f, zLen), new Color(0.18f, 0.18f, 0.18f));
        foreach (float xOff in new[] { -0.038f, 0.038f })
            Prim(coffeeShopScene.transform, PrimitiveType.Cube, new Vector3(xOff, 0.002f, zCenter), new Vector3(0.008f, 0.014f, zLen), new Color(0.40f, 0.40f, 0.42f));
        int n = Mathf.Max(4, Mathf.RoundToInt(zLen / 0.04f));
        for (int i = 0; i < n; i++)
        {
            var r = Prim(coffeeShopScene.transform, PrimitiveType.Cylinder, new Vector3(0, 0, zStart + (i / (float)(n - 1)) * zLen), new Vector3(0.005f, 0.038f, 0.005f), new Color(0.5f, 0.5f, 0.5f));
            r.transform.localRotation = Quaternion.Euler(0, 0, 90);
        }
    }

    void CreateWarehouseWorker()
    {
        float wz = Z_SLOT_ZERO + maxQueueSize * customerSpacing + WORKER_GAP;
        if (workerPrefab) { barista = Instantiate(workerPrefab, coffeeShopScene.transform); barista.transform.localPosition = new Vector3(0, 0, wz); }
        else
        {
            barista = new GameObject("WarehouseWorker"); barista.transform.SetParent(coffeeShopScene.transform); barista.transform.localPosition = new Vector3(0, 0, wz);
            Prim(barista.transform, PrimitiveType.Capsule, new Vector3(0, 0.06f, 0),      new Vector3(0.07f, 0.12f, 0.07f),    new Color(0.95f, 0.80f, 0.05f));
            Prim(barista.transform, PrimitiveType.Sphere,  new Vector3(0, 0.19f, 0),      new Vector3(0.055f, 0.055f, 0.055f), new Color(0.95f, 0.87f, 0.73f));
            Prim(barista.transform, PrimitiveType.Sphere,  new Vector3(0, 0.215f, 0),     new Vector3(0.065f, 0.040f, 0.065f), new Color(0.95f, 0.95f, 0.95f));
            Prim(barista.transform, PrimitiveType.Cube,    new Vector3(0.045f, 0.08f, 0.02f), new Vector3(0.025f, 0.035f, 0.005f), new Color(0.90f, 0.88f, 0.75f));
            CreateTextLabel(barista.transform, "WORKER", new Vector3(0, 0.27f, 0), Color.white, 30, new Vector3(0.002f, 0.002f, 0.002f));
        }
    }

    void CreateReceptionDesk()
    {
        if (receptionDeskPrefab) { counter = Instantiate(receptionDeskPrefab, coffeeShopScene.transform); counter.transform.localPosition = new Vector3(0, 0, Z_SCANNER); }
        else
        {
            counter = new GameObject("ReceptionDesk"); counter.transform.SetParent(coffeeShopScene.transform); counter.transform.localPosition = new Vector3(0, 0, Z_SCANNER);
            Prim(counter.transform, PrimitiveType.Cube, new Vector3(0, 0.025f, 0), new Vector3(0.14f, 0.05f, 0.06f), new Color(0.70f, 0.70f, 0.82f));
            Prim(counter.transform, PrimitiveType.Cube, new Vector3(0, 0.055f, 0), new Vector3(0.14f, 0.01f, 0.06f), new Color(0.88f, 0.88f, 0.93f));
            Prim(counter.transform, PrimitiveType.Cube, new Vector3(-0.04f, 0.057f, -0.025f), new Vector3(0.010f, 0.002f, 0.028f), Color.red, unlit:true);
            Prim(counter.transform, PrimitiveType.Cube, new Vector3(-0.04f, 0.057f, -0.025f), new Vector3(0.028f, 0.002f, 0.010f), Color.red, unlit:true);
            Prim(counter.transform, PrimitiveType.Cube, new Vector3(0.04f, 0.09f, -0.010f),  new Vector3(0.040f, 0.030f, 0.005f), new Color(0.1f, 0.1f, 0.15f));
            Prim(counter.transform, PrimitiveType.Cube, new Vector3(0.04f, 0.09f, -0.0075f), new Vector3(0.032f, 0.022f, 0.001f), new Color(0.05f, 0.55f, 0.95f), unlit:true);
            CreateTextLabel(counter.transform, "RECEPTION", new Vector3(0, 0.105f, 0), new Color(0.05f, 0.70f, 1.00f), 32, new Vector3(0.002f, 0.002f, 0.002f));
        }
        counter.name = "ReceptionDesk";
    }

    void CreateTriageNurse()
    {
        float nz = Z_SLOT_ZERO + maxQueueSize * customerSpacing + WORKER_GAP;
        if (nursePrefab) { barista = Instantiate(nursePrefab, coffeeShopScene.transform); barista.transform.localPosition = new Vector3(0, 0, nz); }
        else
        {
            barista = new GameObject("TriageNurse"); barista.transform.SetParent(coffeeShopScene.transform); barista.transform.localPosition = new Vector3(0, 0, nz);
            Prim(barista.transform, PrimitiveType.Capsule, new Vector3(0, 0.06f, 0), new Vector3(0.07f, 0.12f, 0.07f),   Color.white);
            Prim(barista.transform, PrimitiveType.Sphere,  new Vector3(0, 0.20f, 0), new Vector3(0.055f, 0.055f, 0.055f), new Color(0.95f, 0.87f, 0.73f));
            Prim(barista.transform, PrimitiveType.Cube,    new Vector3(0, 0.229f, 0), new Vector3(0.052f, 0.010f, 0.042f), Color.white);
            Prim(barista.transform, PrimitiveType.Cube,    new Vector3(0, 0.235f, 0), new Vector3(0.008f, 0.001f, 0.022f), Color.red, unlit:true);
            Prim(barista.transform, PrimitiveType.Cube,    new Vector3(0, 0.235f, 0), new Vector3(0.022f, 0.001f, 0.008f), Color.red, unlit:true);
            Prim(barista.transform, PrimitiveType.Cube,    new Vector3(0.045f, 0.09f, 0.02f), new Vector3(0.025f, 0.035f, 0.005f), new Color(0.90f, 0.88f, 0.75f));
            CreateTextLabel(barista.transform, "TRIAGE", new Vector3(0, 0.29f, 0), Color.white, 30, new Vector3(0.002f, 0.002f, 0.002f));
        }
    }

    void CreateWaitingBench()
    {
        var bench = new GameObject("WaitingBench"); bench.transform.SetParent(coffeeShopScene.transform);
        float cz = Z_SLOT_ZERO + (maxQueueSize / 2f) * customerSpacing;
        bench.transform.localPosition = new Vector3(-0.09f, 0, cz);
        float bLen = maxQueueSize * customerSpacing * 0.85f;
        Prim(bench.transform, PrimitiveType.Cube, new Vector3(0, 0.024f, 0), new Vector3(0.018f, 0.005f, bLen), new Color(0.55f, 0.38f, 0.18f));
        for (int i = 0; i < 3; i++)
        {
            float lz = Mathf.Lerp(-bLen * 0.4f, bLen * 0.4f, i / 2f);
            Prim(bench.transform, PrimitiveType.Cube, new Vector3(-0.007f, 0.011f, lz), new Vector3(0.004f, 0.022f, 0.004f), new Color(0.38f, 0.25f, 0.10f));
            Prim(bench.transform, PrimitiveType.Cube, new Vector3( 0.007f, 0.011f, lz), new Vector3(0.004f, 0.022f, 0.004f), new Color(0.38f, 0.25f, 0.10f));
        }
        var sign = new GameObject("ERSign"); sign.transform.SetParent(coffeeShopScene.transform);
        sign.transform.localPosition = new Vector3(-0.12f, 0.07f, cz);
        Prim(sign.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.001f, 0.038f, 0.022f), Color.red, unlit:true);
        Prim(sign.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.001f, 0.014f, 0.038f), Color.red, unlit:true);
    }

    // =========================================================================
    // PRIMITIVE FACTORY
    // =========================================================================
    GameObject Prim(Transform parent, PrimitiveType type, Vector3 lp, Vector3 ls, Color col, bool unlit = false)
    {
        var g = GameObject.CreatePrimitive(type); g.transform.SetParent(parent);
        g.transform.localPosition = lp; g.transform.localRotation = Quaternion.identity; g.transform.localScale = ls;
        var mat = new Material(Shader.Find(unlit ? "Unlit/Color" : "Standard")); mat.color = col;
        g.GetComponent<Renderer>().material = mat; Destroy(g.GetComponent<Collider>()); return g;
    }

    // =========================================================================
    // ENTITY FACTORIES
    // =========================================================================
    GameObject CreateEntity(int ci) =>
        IsHospital() ? CreatePatient(ci) : IsConveyor() ? CreatePackage(ci) : CreateCoffeeCustomer(ci);

    GameObject CreateCoffeeCustomer(int ci)
    {
        GameObject c = customerPrefab ? Instantiate(customerPrefab) : new GameObject("Customer");
        if (!c.GetComponent<CustomerAnimationController>()) c.AddComponent<CustomerAnimationController>();
        if (!customerPrefab)
        {
            Prim(c.transform, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.06f, 0.10f, 0.06f), coffeeShopColors[ci % coffeeShopColors.Length]);
            var bt = c.transform.GetChild(0);
            Prim(bt, PrimitiveType.Sphere,   new Vector3(0, 0.12f, 0), new Vector3(0.45f, 0.45f, 0.45f), new Color(0.95f, 0.87f, 0.73f));
            Prim(bt, PrimitiveType.Cylinder, new Vector3(0.08f, 0, 0), new Vector3(0.15f, 0.2f, 0.15f),  new Color(0.82f, 0.41f, 0.12f));
            var col = c.AddComponent<BoxCollider>(); col.size = new Vector3(0.12f, 0.25f, 0.12f); col.center = new Vector3(0, 0.05f, 0);
        }
        originalEntityScale = c.transform.localScale; return c;
    }

    GameObject CreatePackage(int ci)
    {
        GameObject pkg = packagePrefab ? Instantiate(packagePrefab) : new GameObject("Package");
        if (!pkg.GetComponent<CustomerAnimationController>()) pkg.AddComponent<CustomerAnimationController>();
        if (!packagePrefab)
        {
            float w = 0.055f + (ci%3)*0.008f, h = 0.048f + (ci%4)*0.007f, d = 0.050f + (ci%2)*0.010f;
            Prim(pkg.transform, PrimitiveType.Cube, new Vector3(0, h*0.5f, 0),        new Vector3(w, h, d),              new Color(0.76f, 0.57f, 0.32f));
            Prim(pkg.transform, PrimitiveType.Cube, new Vector3(0, h*0.5f, -d*0.51f), new Vector3(w*0.70f, h*0.40f, 0.001f), conveyorBeltColors[ci % conveyorBeltColors.Length], unlit:true);
            Prim(pkg.transform, PrimitiveType.Cube, new Vector3(0, h+0.001f, 0),      new Vector3(w*0.25f, 0.001f, d),   new Color(0.95f, 0.90f, 0.50f), unlit:true);
            var col = pkg.AddComponent<BoxCollider>(); col.size = new Vector3(w, h, d); col.center = new Vector3(0, h*0.5f, 0);
        }
        originalEntityScale = pkg.transform.localScale; return pkg;
    }

    GameObject CreatePatient(int ci)
    {
        GameObject pat = patientPrefab ? Instantiate(patientPrefab) : new GameObject("Patient");
        if (!pat.GetComponent<CustomerAnimationController>()) pat.AddComponent<CustomerAnimationController>();
        if (!patientPrefab)
        {
            Prim(pat.transform, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.06f, 0.10f, 0.06f), new Color(0.72f, 0.87f, 0.95f));
            var bt = pat.transform.GetChild(0);
            Prim(bt, PrimitiveType.Sphere,   new Vector3(0, 0.12f, 0),        new Vector3(0.45f, 0.45f, 0.45f), new Color(0.95f, 0.87f, 0.73f));
            Prim(bt, PrimitiveType.Cylinder, new Vector3(0.065f, -0.05f, 0),  new Vector3(0.08f, 0.02f, 0.08f), hospitalColors[ci % hospitalColors.Length], unlit:true);
            var col = pat.AddComponent<BoxCollider>(); col.size = new Vector3(0.12f, 0.25f, 0.12f); col.center = new Vector3(0, 0.05f, 0);
        }
        originalEntityScale = pat.transform.localScale; return pat;
    }

    // =========================================================================
    // TEXT LABEL
    // =========================================================================
    GameObject CreateTextLabel(Transform parent, string text, Vector3 lp, Color color, int fontSize, Vector3 scale)
    {
        var obj = new GameObject("Label"); obj.transform.SetParent(parent);
        obj.transform.localPosition = lp; obj.transform.localRotation = Quaternion.identity; obj.transform.localScale = scale;
        var tm = obj.AddComponent<TextMesh>(); tm.text = text; tm.fontSize = fontSize; tm.color = color;
        tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; obj.AddComponent<Billboard>(); return obj;
    }

    // =========================================================================
    // ENQUEUE
    // =========================================================================
    public void OnEnqueueButton()
    {
        if (tutorialIntegration) tutorialIntegration.OnEnqueueButtonClicked();
        if (currentState != QueueState.Ready) return;
        if (customerQueue.Count >= maxQueueSize) { UpdateInstructionsError("! Queue is FULL!"); return; }
        if (IsInstant()) { StartCoroutine(InstantEnqueue()); return; }

        isEnqueueMode  = true; currentState = QueueState.MovingNewCustomer;
        movingCustomer = CreateEntity(customerQueue.Count);
        movingCustomer.transform.SetParent(coffeeShopScene.transform);
        movingCustomer.transform.localScale    = originalEntityScale;
        targetPosition = GetQueuePosition(customerQueue.Count);
        movingCustomer.transform.localPosition = targetPosition + new Vector3(0.20f, 0, 0);
        movingCustomer.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        CreateTargetIndicator(); SwitchToMovementUI();
        UpdateInstructions($"Move {PersonLabel()} to {RearLabel()} marker"); PlaySound(customerJoinSound);
    }

    IEnumerator InstantEnqueue()
    {
        int ip = customerQueue.Count; Vector3 fp = GetQueuePosition(ip), sp = fp + new Vector3(0, 0, 0.22f);
        var e = CreateEntity(ip); e.transform.SetParent(coffeeShopScene.transform); e.transform.localScale = originalEntityScale;
        e.transform.localPosition = sp; e.transform.localRotation = Quaternion.Euler(0, 180f, 0); PlaySound(customerJoinSound);
        yield return SlideLocal(e, sp, fp, 0.35f);
        var c = new Customer { gameObject = e, queuePosition = ip, customerId = $"C{customerIdCounter++}", priority = 5, animController = e.GetComponent<CustomerAnimationController>() };
        c.numberLabel   = CreateTextLabel(e.transform, $"[{ip}]",        new Vector3(0, LabelY(), 0),        Color.white, 50, new Vector3(0.002f, 0.002f, 0.002f));
        c.thoughtBubble = CreateTextLabel(e.transform, EnqueuedThought(), new Vector3(0, ThoughtLabelY(), 0), Color.white, 35, new Vector3(0.002f, 0.002f, 0.002f));
        customerQueue.Add(c); UpdateQueueMarkers(); UpdateStatus(); CheckAndUpdateButtonStates();
        UpdateInstructionsSuccess($"Enqueued at [{ip}]! Queue: {customerQueue.Count}/{maxQueueSize}");
        if (operationInfoText) operationInfoText.text = $"ENQUEUE O(1)\n\nAdded at rear [{ip}]\nQueue: {customerQueue.Count}/{maxQueueSize}";
        NotifyGuideOfEnqueue(); // GUIDE NOTIFICATION
    }

    // =========================================================================
    // DEQUEUE
    // =========================================================================
    public void OnDequeueButton()
    {
        if (tutorialIntegration) tutorialIntegration.OnDequeueButtonClicked();
        if (currentState != QueueState.Ready || customerQueue.Count == 0) { UpdateInstructionsError("! Queue is EMPTY!"); return; }
        if (IsInstant()) { StartCoroutine(InstantDequeue()); return; }

        isEnqueueMode  = false; currentState = QueueState.MovingCustomerOut;
        var front = customerQueue[0]; movingCustomer = front.gameObject;
        customerQueue.RemoveAt(0); CheckAndUpdateButtonStates();
        targetPosition = GetQueuePosition(0) + new Vector3(0, 0, -0.20f);
        CreateExitIndicator(); SwitchToMovementUI();
        UpdateInstructions($"Move {PersonLabel()} to EXIT"); PlaySound(customerServedSound);
    }

    IEnumerator InstantDequeue()
    {
        var front = customerQueue[0]; customerQueue.RemoveAt(0); CheckAndUpdateButtonStates();
        var e = front.gameObject; var s = e.transform.localPosition; var x = s + new Vector3(0, 0, -0.22f);
        PlaySound(customerServedSound); yield return SlideLocal(e, s, x, 0.35f); Destroy(e);
        yield return StartCoroutine(AdvanceQueue());
        UpdateInstructionsSuccess($"{ServiceLabel()}d! Queue: {customerQueue.Count}/{maxQueueSize}");
        if (operationInfoText) operationInfoText.text = $"DEQUEUE O(1)\n\nRemoved from front\nQueue: {customerQueue.Count}/{maxQueueSize}";
        NotifyGuideOfDequeue(); // GUIDE NOTIFICATION
    }

    // =========================================================================
    // PEEK
    // =========================================================================
    public void OnPeekButton()
    {
        if (tutorialIntegration) tutorialIntegration.OnPeekButtonClicked();
        if (currentState != QueueState.Ready || customerQueue.Count == 0) { UpdateInstructionsError("! Queue is EMPTY!"); return; }
        PlaySound(peekSound); StartCoroutine(PeekFront());
    }

    IEnumerator PeekFront()
    {
        var front = customerQueue[0];
        var hl = MakeHighlight(front.gameObject.transform, new Color(0, 1, 1, 0.3f));
        if (front.thoughtBubble) { var tm = front.thoughtBubble.GetComponent<TextMesh>(); if (tm) tm.text = IsHospital() ? "Treat me next!" : IsConveyor() ? "Scan me next!" : "I'm next!"; }
        UpdateInstructionsSuccess($"PEEK: [{PersonLabel()} at 0] - O(1)");
        if (operationInfoText) operationInfoText.text = "PEEK O(1)\n\nViewed front!\nQueue unchanged.";
        NotifyGuideOfPeek(); // GUIDE NOTIFICATION
        yield return new WaitForSeconds(2f); Destroy(hl);
        if (front.thoughtBubble) { var tm = front.thoughtBubble.GetComponent<TextMesh>(); if (tm) tm.text = ThoughtPhrases()[0]; }
        UpdateInstructions("What would you like to do next?");
    }

    // =========================================================================
    // PRIORITY ENQUEUE
    // =========================================================================
    public void OnPriorityEnqueueButton()
    {
        if (currentState != QueueState.Ready) return;
        if (customerQueue.Count >= maxQueueSize) { UpdateInstructionsError("! Queue is FULL!"); return; }
        int priority = 5;
        if (priorityInputField && !string.IsNullOrEmpty(priorityInputField.text)) int.TryParse(priorityInputField.text, out priority);
        priority = Mathf.Clamp(priority, 1, 9);
        int insertIndex = customerQueue.Count;
        for (int i = 0; i < customerQueue.Count; i++) if (priority < customerQueue[i].priority) { insertIndex = i; break; }
        if (IsInstant()) { StartCoroutine(InstantPriorityEnqueue(priority, insertIndex)); return; }

        isEnqueueMode = true; currentState = QueueState.PriorityEnqueuing;
        movingCustomer = CreateEntity(customerQueue.Count);
        movingCustomer.transform.SetParent(coffeeShopScene.transform); movingCustomer.transform.localScale = originalEntityScale;
        targetPosition = GetQueuePosition(insertIndex);
        movingCustomer.transform.localPosition = targetPosition + new Vector3(0.25f, 0, 0); movingCustomer.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        CreateTextLabel(movingCustomer.transform, $"P{priority}", new Vector3(0, 0.18f, 0), priority == 1 ? Color.yellow : Color.white, 55, new Vector3(0.002f, 0.002f, 0.002f));
        CreatePriorityTargetIndicator(insertIndex); SwitchToMovementUI();
        UpdateInstructions($"Move P{priority} to slot [{insertIndex}]");
        var tmp = movingCustomer.AddComponent<TempPriorityHolder>(); tmp.priority = priority; tmp.insertIndex = insertIndex;
        PlaySound(customerJoinSound);
    }

    IEnumerator InstantPriorityEnqueue(int priority, int insertIndex)
    {
        Vector3 fp = GetQueuePosition(insertIndex), sp = new Vector3(0.25f, fp.y, fp.z);
        var e = CreateEntity(insertIndex); e.transform.SetParent(coffeeShopScene.transform); e.transform.localScale = originalEntityScale;
        e.transform.localPosition = sp; e.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        string pLabel = IsHospital() ? $"P{priority}\n{TriageLabel(priority)}" : $"P{priority}";
        Color  pColor = priority == 1 ? Color.red : priority <= 3 ? Color.yellow : Color.white;
        CreateTextLabel(e.transform, pLabel, new Vector3(0, LabelY() + 0.02f, 0), pColor, 40, new Vector3(0.002f, 0.002f, 0.002f));
        PlaySound(customerJoinSound); yield return SlideLocal(e, sp, fp, 0.35f);
        var c = new Customer { gameObject = e, queuePosition = insertIndex, customerId = $"C{customerIdCounter++}", priority = priority, animController = e.GetComponent<CustomerAnimationController>() };
        c.numberLabel = CreateTextLabel(e.transform, $"[{insertIndex}]", new Vector3(0, LabelY() - 0.03f, 0), Color.white, 50, new Vector3(0.002f, 0.002f, 0.002f));
        customerQueue.Insert(insertIndex, c);
        NotifyGuideOfPriorityEnqueue(priority, insertIndex); // GUIDE NOTIFICATION
        yield return StartCoroutine(AdvanceQueue());
        UpdateStatus(); CheckAndUpdateButtonStates();
        UpdateInstructionsSuccess($"P{priority} at [{insertIndex}]! Queue: {customerQueue.Count}/{maxQueueSize}");
        if (operationInfoText) operationInfoText.text = $"PRIORITY ENQUEUE O(n)\n\nP{priority} -> slot [{insertIndex}]\nQueue: {customerQueue.Count}/{maxQueueSize}";
    }

    string TriageLabel(int p) => p == 1 ? "CRITICAL" : p <= 2 ? "URGENT" : p <= 4 ? "SEMI-URG" : p <= 6 ? "MODERATE" : "MINOR";

    // =========================================================================
    // CONFIRM / CANCEL
    // =========================================================================
    public void OnConfirmPlacement()
    {
        if (!movingCustomer) return;
        StopMoving(); var anim = movingCustomer.GetComponent<CustomerAnimationController>(); if (anim) anim.ForceIdle();

        if (isEnqueueMode)
        {
            var tmp = movingCustomer.GetComponent<TempPriorityHolder>();
            if (currentState == QueueState.PriorityEnqueuing && tmp != null)
            {
                movingCustomer.transform.localPosition = GetQueuePosition(tmp.insertIndex);
                movingCustomer.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                var nc = new Customer { gameObject = movingCustomer, queuePosition = tmp.insertIndex, customerId = $"C{customerIdCounter++}", priority = tmp.priority, animController = anim };
                nc.numberLabel = CreateTextLabel(movingCustomer.transform, $"[{tmp.insertIndex}]", new Vector3(0, 0.22f, 0), Color.white, 50, new Vector3(0.002f, 0.002f, 0.002f));
                customerQueue.Insert(tmp.insertIndex, nc);
                StartCoroutine(AdvanceQueue());
                UpdateInstructionsSuccess($"P{tmp.priority} inserted at [{tmp.insertIndex}]!");
                NotifyGuideOfPriorityEnqueue(tmp.priority, tmp.insertIndex); // GUIDE NOTIFICATION
            }
            else
            {
                movingCustomer.transform.localPosition = targetPosition;
                movingCustomer.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                var nc = new Customer { gameObject = movingCustomer, queuePosition = customerQueue.Count, customerId = $"C{customerIdCounter++}", priority = 5, animController = anim };
                nc.numberLabel   = CreateTextLabel(movingCustomer.transform, $"[{customerQueue.Count}]", new Vector3(0, 0.22f, 0), Color.white, 50, new Vector3(0.002f, 0.002f, 0.002f));
                nc.thoughtBubble = CreateTextLabel(movingCustomer.transform, EnqueuedThought(),            new Vector3(0, 0.30f, 0), Color.white, 35, new Vector3(0.002f, 0.002f, 0.002f));
                customerQueue.Add(nc);
                UpdateInstructionsSuccess($"Enqueued! Size: {customerQueue.Count}");
                NotifyGuideOfEnqueue(); // GUIDE NOTIFICATION
            }
        }
        else
        {
            Destroy(movingCustomer);
            UpdateInstructionsSuccess($"{ServiceLabel()}d - FIFO!");
            StartCoroutine(AdvanceQueue());
            NotifyGuideOfDequeue(); // GUIDE NOTIFICATION
        }

        CleanupMovement(); UpdateQueueMarkers(); UpdateStatus(); CheckAndUpdateButtonStates();
    }

    void CancelMovement()
    {
        if (movingCustomer)
        {
            StopMoving(); var anim = movingCustomer.GetComponent<CustomerAnimationController>(); if (anim) anim.ForceIdle();
            if (isEnqueueMode) { Destroy(movingCustomer); UpdateInstructions("Cancelled."); }
            else
            {
                var rc = new Customer { gameObject = movingCustomer, queuePosition = 0, customerId = $"C{customerIdCounter++}", animController = movingCustomer.GetComponent<CustomerAnimationController>() };
                movingCustomer.transform.localPosition = GetQueuePosition(0); movingCustomer.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                customerQueue.Insert(0, rc); UpdateInstructions("Returned to queue.");
            }
        }
        CleanupMovement(); CheckAndUpdateButtonStates();
    }

    void CleanupMovement()
    {
        if (targetPositionIndicator) { Destroy(targetPositionIndicator); targetPositionIndicator = null; }
        movingCustomer = null; currentState = QueueState.Ready; StopMoving();
        hasShownJoystickTutorial = false; RefreshModePanel();
        if (movementControlPanel) movementControlPanel.SetActive(false);
        if (confirmButton)        confirmButton       .SetActive(false);
    }

    IEnumerator AdvanceQueue()
    {
        if (tutorialIntegration) tutorialIntegration.OnQueueAdvanced();
        for (int i = 0; i < customerQueue.Count; i++)
        {
            customerQueue[i].queuePosition = i; var np = GetQueuePosition(i);
            if (customerQueue[i].animController) customerQueue[i].animController.PlayWalk();
            StartCoroutine(SmoothMove(customerQueue[i].gameObject, np, customerQueue[i].animController));
            if (customerQueue[i].numberLabel) { var tm = customerQueue[i].numberLabel.GetComponent<TextMesh>(); if (tm) tm.text = $"[{i}]"; }
        }
        yield return new WaitForSeconds(0.6f);
        foreach (var c in customerQueue) if (c.animController) c.animController.ForceIdle();
        UpdateQueueMarkers(); CheckAndUpdateButtonStates();
    }

    // =========================================================================
    // RESET
    // =========================================================================
    public void OnResetButton()
    {
        if (swipeRotation)       swipeRotation      .ResetRotation();
        if (tutorialIntegration) tutorialIntegration.OnResetButtonClicked();
        if (zoomController)      zoomController     .ResetZoom();
        StopAllCoroutines(); pulseCoroutine = null; ResetEnqueueButtonVisual();
        originalButtonColors.Clear(); CacheAllButtonColors();
        if (coffeeShopScene)         Destroy(coffeeShopScene);
        if (movingCustomer)          Destroy(movingCustomer);
        if (targetPositionIndicator) Destroy(targetPositionIndicator);
        coffeeShopScene = movingCustomer = targetPositionIndicator = null;
        customerQueue.Clear(); sceneSpawned = false; customerIdCounter = 1;
        isEnqueueMode = false; isMoving = false; currentMovementDirection = Vector3.zero;
        currentDifficulty = DifficultyMode.None; currentScenario = ScenarioMode.None;
        _silenceInstructions = false; // restore on reset
        if (planeManager) { planeManager.enabled = false; foreach (var p in planeManager.trackables) if (p?.gameObject) p.gameObject.SetActive(true); }
        if (raycastManager) raycastManager.enabled = false;
        SetAllPanelsHidden(); if (statusText) statusText.text = "Queue Size: 0"; ShowScenarioPanelAgain();
    }

    // =========================================================================
    // UI HELPERS
    // =========================================================================
    void SwitchToMovementUI()
    {
        if (beginnerButtonPanel)     beginnerButtonPanel    .SetActive(false);
        if (intermediateButtonPanel) intermediateButtonPanel.SetActive(false);
        if (movementControlPanel)    movementControlPanel   .SetActive(true);
    }
    void RefreshModePanel()
    {
        if (beginnerButtonPanel)     beginnerButtonPanel    .SetActive(currentDifficulty == DifficultyMode.Beginner);
        if (intermediateButtonPanel) intermediateButtonPanel.SetActive(currentDifficulty == DifficultyMode.Intermediate);
        CheckAndUpdateButtonStates();
    }
    void UpdateStatus() { if (statusText) statusText.text = $"Queue: {customerQueue.Count}/{maxQueueSize}"; }
    void PlaySound(AudioClip c) { if (audioSource && c) audioSource.PlayOneShot(c); }
    public int  GetQueueSize() => customerQueue.Count;
    public bool IsReady()      => currentState == QueueState.Ready;

    // =========================================================================
    // MOVEMENT
    // =========================================================================
    void MoveCustomerContinuous()
    {
        if (!movingCustomer) return;
        if (!hasShownJoystickTutorial && tutorialIntegration) { tutorialIntegration.OnJoystickUsed(); hasShownJoystickTutorial = true; }
        var ac = movingCustomer.GetComponent<CustomerAnimationController>(); if (ac) ac.PlayWalk();
        Vector3 wd = coffeeShopScene.transform.TransformDirection(currentMovementDirection); wd.y = 0;
        if (wd.sqrMagnitude > 0.001f)
        {
            movingCustomer.transform.rotation = Quaternion.Slerp(movingCustomer.transform.rotation, Quaternion.LookRotation(wd), Time.deltaTime * 8f);
            movingCustomer.transform.position += wd.normalized * moveSpeed * Time.deltaTime;
        }
    }
    void StartMoving(Vector3 dir) { currentMovementDirection = dir; isMoving = true; PlaySound(moveSound); }
    void StopMoving() { isMoving = false; currentMovementDirection = Vector3.zero; if (movingCustomer) { var ac = movingCustomer.GetComponent<CustomerAnimationController>(); if (ac) ac.ForceIdle(); } }

    void CheckConfirmDistance()
    {
        if (!movingCustomer) return;
        float d = Vector3.Distance(movingCustomer.transform.localPosition, targetPosition);
        bool near = d < ZoomAdjustedThreshold();
        if (confirmButton) confirmButton.SetActive(near);
        if (near) UpdateInstructionsSuccess("Tap CONFIRM!"); else UpdateInstructions(isEnqueueMode ? $"-> target ({d:F2})" : $"-> EXIT ({d:F2})");
    }

    // =========================================================================
    // INDICATORS
    // =========================================================================
    void CreateTargetIndicator()
    {
        targetPositionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        targetPositionIndicator.transform.SetParent(coffeeShopScene.transform); targetPositionIndicator.transform.localPosition = targetPosition; targetPositionIndicator.transform.localScale = new Vector3(0.12f, 0.005f, 0.12f);
        var m = new Material(Shader.Find("Unlit/Color")); m.color = new Color(0, 1, 0, 0.5f); targetPositionIndicator.GetComponent<Renderer>().material = m; Destroy(targetPositionIndicator.GetComponent<Collider>()); StartCoroutine(PulseIndicator());
    }
    void CreateExitIndicator()
    {
        targetPositionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        targetPositionIndicator.transform.SetParent(coffeeShopScene.transform); targetPositionIndicator.transform.localPosition = targetPosition; targetPositionIndicator.transform.localScale = new Vector3(0.15f, 0.005f, 0.15f);
        var m = new Material(Shader.Find("Unlit/Color")); m.color = new Color(1, 0, 0, 0.6f); targetPositionIndicator.GetComponent<Renderer>().material = m; Destroy(targetPositionIndicator.GetComponent<Collider>());
        CreateTextLabel(targetPositionIndicator.transform, "EXIT", new Vector3(0, 0.08f, 0), Color.red, 50, new Vector3(0.002f, 0.002f, 0.002f)); StartCoroutine(PulseIndicator());
    }
    void CreatePriorityTargetIndicator(int idx)
    {
        targetPositionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        targetPositionIndicator.transform.SetParent(coffeeShopScene.transform); targetPositionIndicator.transform.localPosition = GetQueuePosition(idx); targetPositionIndicator.transform.localScale = new Vector3(0.13f, 0.005f, 0.13f);
        var m = new Material(Shader.Find("Unlit/Color")); m.color = new Color(1, 0.85f, 0, 0.7f); targetPositionIndicator.GetComponent<Renderer>().material = m; Destroy(targetPositionIndicator.GetComponent<Collider>());
        CreateTextLabel(targetPositionIndicator.transform, "INSERT", new Vector3(0, 0.08f, 0), Color.yellow, 35, new Vector3(0.002f, 0.002f, 0.002f)); StartCoroutine(PulseIndicator());
    }
    IEnumerator PulseIndicator() { while (targetPositionIndicator) { float s = 0.12f + Mathf.Sin(Time.time * 3f) * 0.02f; targetPositionIndicator.transform.localScale = new Vector3(s, 0.005f, s); yield return null; } }
    GameObject MakeHighlight(Transform parent, Color col)
    {
        var h = GameObject.CreatePrimitive(PrimitiveType.Sphere); h.transform.SetParent(parent); h.transform.localPosition = new Vector3(0, 0.05f, 0); h.transform.localScale = Vector3.one * 1.8f;
        var m = new Material(Shader.Find("Unlit/Color")); m.color = col; h.GetComponent<Renderer>().material = m; Destroy(h.GetComponent<Collider>()); return h;
    }

    // =========================================================================
    // COROUTINES
    // =========================================================================
    IEnumerator SmoothMove(GameObject obj, Vector3 tgt, CustomerAnimationController ac)
    {
        if (!obj) yield break; var s = obj.transform.localPosition; float e = 0, d = 0.5f;
        while (e < d) { e += Time.deltaTime; obj.transform.localPosition = Vector3.Lerp(s, tgt, e / d); yield return null; }
        obj.transform.localPosition = tgt; obj.transform.localRotation = Quaternion.Euler(0, 180f, 0); if (ac) ac.ForceIdle();
    }
    IEnumerator SlideLocal(GameObject obj, Vector3 from, Vector3 to, float dur)
    { float e = 0; while (e < dur) { e += Time.deltaTime; obj.transform.localPosition = Vector3.Lerp(from, to, e / dur); yield return null; } obj.transform.localPosition = to; }

    // =========================================================================
    // JOYSTICK
    // =========================================================================
    void SetupJoystick()
    {
        if (!joystickContainer) return;
        var tz = new GameObject("JoystickTouchZone"); tz.transform.SetParent(joystickContainer, false);
        var tzR = tz.AddComponent<RectTransform>(); tzR.sizeDelta = new Vector2(300, 300); tzR.anchoredPosition = Vector2.zero;
        tz.AddComponent<UnityEngine.UI.Image>().color = Color.clear;
        var bg = new GameObject("JoystickBG"); bg.transform.SetParent(tz.transform, false);
        joystickBackground = bg.AddComponent<UnityEngine.UI.Image>();
        var bgR = bg.GetComponent<RectTransform>(); bgR.sizeDelta = new Vector2(150, 150); bgR.anchoredPosition = Vector2.zero;
        joystickBackground.color = new Color(1,1,1,0.3f); joystickBackground.sprite = MakeCircle(150); joystickRadius = 75f;
        var hObj = new GameObject("JoystickHandle"); hObj.transform.SetParent(bg.transform, false);
        joystickHandle = hObj.AddComponent<UnityEngine.UI.Image>();
        var hR = hObj.GetComponent<RectTransform>(); hR.sizeDelta = new Vector2(60, 60); hR.anchoredPosition = Vector2.zero;
        joystickHandle.color = new Color(0.8f, 0.8f, 0.8f, 0.8f); joystickHandle.sprite = MakeCircle(60);
        var et = tz.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        JoyEntry(et, UnityEngine.EventSystems.EventTriggerType.PointerDown, d => OnJoyDown((UnityEngine.EventSystems.PointerEventData)d));
        JoyEntry(et, UnityEngine.EventSystems.EventTriggerType.Drag,        d => OnJoyDrag((UnityEngine.EventSystems.PointerEventData)d));
        JoyEntry(et, UnityEngine.EventSystems.EventTriggerType.PointerUp,   d => OnJoyUp ((UnityEngine.EventSystems.PointerEventData)d));
    }
    void JoyEntry(UnityEngine.EventSystems.EventTrigger et, UnityEngine.EventSystems.EventTriggerType t, UnityEngine.Events.UnityAction<UnityEngine.EventSystems.BaseEventData> cb)
    { var e = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = t }; e.callback.AddListener(cb); et.triggers.Add(e); }
    Sprite MakeCircle(int size)
    {
        var tex = new Texture2D(size, size); var pix = new Color[size * size]; float c = size / 2f;
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) pix[y * size + x] = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) < c ? Color.white : Color.clear;
        tex.SetPixels(pix); tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    void OnJoyDown(UnityEngine.EventSystems.PointerEventData d) { isDraggingJoystick = true; OnJoyDrag(d); }
    void OnJoyDrag(UnityEngine.EventSystems.PointerEventData d)
    {
        if (!isDraggingJoystick) return;
        if (!hasShownJoystickTutorial && tutorialIntegration) { tutorialIntegration.OnJoystickUsed(); hasShownJoystickTutorial = true; }
        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBackground.GetComponent<RectTransform>(), d.position, d.pressEventCamera, out pos))
        {
            if (pos.magnitude > joystickRadius) pos = pos.normalized * joystickRadius;
            joystickInputVector = pos / joystickRadius; joystickHandle.GetComponent<RectTransform>().anchoredPosition = pos;
            var dir = new Vector3(joystickInputVector.x, 0, joystickInputVector.y);
            if (dir.magnitude > 0.1f) { if (!isMoving) PlaySound(moveSound); StartMoving(dir); } else StopMoving();
        }
    }
    void OnJoyUp(UnityEngine.EventSystems.PointerEventData d) { isDraggingJoystick = false; joystickInputVector = Vector2.zero; joystickHandle.GetComponent<RectTransform>().anchoredPosition = Vector2.zero; StopMoving(); }
}

// =============================================================================
// HELPER CLASSES
// =============================================================================
public class TempPriorityHolder : MonoBehaviour { public int priority; public int insertIndex; }

public class CustomerAnimationController : MonoBehaviour
{
    private Animator anim;
    void Awake() { anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(); }
    public void PlayWalk()  { if (!anim) return; try { anim.SetBool("IsWalking", true);  } catch {} try { anim.SetFloat("Speed", 1f); } catch {} }
    public void ForceIdle() { if (!anim) return; try { anim.SetBool("IsWalking", false); } catch {} try { anim.SetFloat("Speed", 0f); } catch {} try { anim.ResetTrigger("Walk"); } catch {} try { anim.Play("Idle", 0, 0f); } catch {} }
    public void PlayIdle()  => ForceIdle();
    public bool IsWalking() => anim != null && anim.GetFloat("Speed") > 0.1f;
}