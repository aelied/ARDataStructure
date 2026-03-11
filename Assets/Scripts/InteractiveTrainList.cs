using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

/// <summary>
/// LINKED LIST — Three Scenarios + Two Difficulties
///
/// Flow: App Launch -> Scenario Panel -> Difficulty Panel -> AR Tap to Place -> Game
///
/// TRAIN      — train cars on tracks linked by metal coupling rods
/// SOLAR      — planets in space linked by glowing orbital bead-paths
/// CITY METRO — underground carriages linked by neon tunnel connectors
///
/// BEGINNER     -> Add Head / Add Tail / Remove Head / Traverse
/// INTERMEDIATE -> Insert At / Delete By Value / Reverse / Find Middle
///
/// BUTTON LOCKING:
///   On scene spawn the list is EMPTY so secondary buttons start LOCKED.
///   Only Add Head / Insert At (and Reset) are accessible.
///   Add Head / Insert At pulse to guide the user.
///   Secondary buttons unlock once the first node exists.
///   If all nodes are removed the buttons re-lock and the pulse restarts.
///
/// PULSE: #8469FF -> #B2A0FF  scale 0.92 -> 1.10, sine-wave in sync.
///
/// FEEDBACK COLORS:
///   Errors  -> Red   (#FF4040)
///   Success -> Green (#33FF4D)
///   Neutral -> White
///
/// SILENCE FLAG:
///   _silenceInstructions — set by ARLinkedListLessonGuide via SetInstructionSilence().
///   When true, UpdateInstructions*, UpdateStatus, and direct operationInfoText /
///   detectionText writes are suppressed so the guide's SyncControllerUI text wins.
/// </summary>
public class InteractiveTrainList : MonoBehaviour
{
    // -------------------------------------------------------------------------
    //  AR COMPONENTS
    // -------------------------------------------------------------------------
    [Header("AR Components")]
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;
    public Camera arCamera;

    [Header("Zoom Controller")]
    public SceneZoomController zoomController;

    [Header("Plane Visualization")]
    public GameObject planePrefab;

    [Header("Custom Assets (Optional)")]
    public GameObject trainCarPrefab;
    public GameObject trackPrefab;

    // -------------------------------------------------------------------------
    //  UI — SHARED
    // -------------------------------------------------------------------------
    [Header("UI References - Shared")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI detectionText;
    public TextMeshProUGUI operationInfoText;
    public GameObject explanationPanel;

    public GameObject mainButtonPanel;
    public GameObject carInputPanel;
    public TMP_InputField cargoInputField;
    public GameObject movementControlPanel;
    public GameObject confirmButton;

    // -------------------------------------------------------------------------
    //  UI — SCENARIO PANEL
    // -------------------------------------------------------------------------
    [Header("Scenario Panel")]
    public GameObject scenarioPanel;
    public TextMeshProUGUI scenarioTitleText;
    public UnityEngine.UI.Button btnScenarioTrain;
    public UnityEngine.UI.Button btnScenarioSolar;
    public UnityEngine.UI.Button btnScenarioCityMetro;

    // -------------------------------------------------------------------------
    //  UI — DIFFICULTY PANEL
    // -------------------------------------------------------------------------
    [Header("Difficulty Panel")]
    public GameObject difficultyPanel;
    public TextMeshProUGUI difficultyTitleText;
    public UnityEngine.UI.Button beginnerBtn;
    public UnityEngine.UI.Button intermediateBtn;

    // -------------------------------------------------------------------------
    //  UI — MODE BUTTON PANELS
    // -------------------------------------------------------------------------
    [Header("Mode Button Panels")]
    public GameObject beginnerButtonPanel;
    public GameObject intermediateButtonPanel;

    // -------------------------------------------------------------------------
    //  UI — INTERMEDIATE INPUTS
    // -------------------------------------------------------------------------
    [Header("Intermediate UI")]
    public GameObject insertAtInputPanel;
    public TMP_InputField insertPositionField;
    public TMP_InputField insertCargoField;
    public GameObject deleteValueInputPanel;
    public TMP_InputField deleteValueField;

    // -------------------------------------------------------------------------
    //  UI — MOVEMENT BUTTONS
    // -------------------------------------------------------------------------
    [Header("Movement Control Buttons")]
    public UnityEngine.UI.Button moveLeftButton;
    public UnityEngine.UI.Button moveRightButton;
    public UnityEngine.UI.Button cancelButton;

    // -------------------------------------------------------------------------
    //  ACTION BUTTONS
    // -------------------------------------------------------------------------
    [Header("Beginner Action Buttons")]
    public UnityEngine.UI.Button beginnerAddHeadButton;
    public UnityEngine.UI.Button beginnerAddTailButton;
    public UnityEngine.UI.Button beginnerRemoveHeadButton;
    public UnityEngine.UI.Button beginnerTraverseButton;

    [Header("Intermediate Action Buttons")]
    public UnityEngine.UI.Button intermediateInsertAtButton;
    public UnityEngine.UI.Button intermediateDeleteByValueButton;
    public UnityEngine.UI.Button intermediateReverseButton;
    public UnityEngine.UI.Button intermediateFindMiddleButton;

    // -------------------------------------------------------------------------
    //  AUDIO
    // -------------------------------------------------------------------------
    [Header("Audio")]
    public AudioClip placeSceneSound;
    public AudioClip addCarSound;
    public AudioClip removeCarSound;
    public AudioClip traverseSound;
    public AudioClip moveSound;
    public AudioClip orbitSound;

    // -------------------------------------------------------------------------
    //  SETTINGS
    // -------------------------------------------------------------------------
    [Header("Train Settings")]
    public float carLength  = 0.08f;
    public float carHeight  = 0.04f;
    public float carWidth   = 0.05f;
    public float carSpacing = 0.01f;

    [Header("Solar Settings")]
    public float planetRadius  = 0.032f;
    public float planetSpacing = 0.03f;

    [Header("Shared Settings")]
    public float moveSpeed                = 1f;
    public float confirmDistanceThreshold = 0.08f;
    public float sceneHeightOffset        = 0.05f;

    [Header("Tutorial System")]
    public LinkedListTutorialIntegration tutorialIntegration;

    [Header("Swipe Rotation")]
    public SwipeRotation swipeRotation;

    // -------------------------------------------------------------------------
    //  ENUMS
    // -------------------------------------------------------------------------
    public enum Scenario { None, Train, Solar, CityMetro }
    private enum Difficulty { None, Beginner, Intermediate }
    private enum ListState
    {
        ChoosingScenario, ChoosingDifficulty, WaitingForPlane, Ready,
        AddingNode, RemovingNode, Traversing, InsertingAt,
        DeletingByValue, Reversing, FindingMiddle
    }

    // -------------------------------------------------------------------------
    //  PRIVATE STATE
    // -------------------------------------------------------------------------
    private ARLinkedListLessonGuide      cachedLLGuide;
    private ARLinkedListLessonAssessment cachedLLAssessment;
    private AudioSource audioSource;
    private Scenario   currentScenario   = Scenario.None;
    private Difficulty currentDifficulty = Difficulty.None;
    private ListState  currentState      = ListState.ChoosingScenario;

    // --- Silence flag (set by ARLinkedListLessonGuide.SetInstructionSilence) ---
    private bool _silenceInstructions = false;

    /// <summary>
    /// Called by ARLinkedListLessonGuide to suppress / restore controller UI writes.
    /// When silent, UpdateInstructions*, UpdateStatus, operationInfoText and
    /// detectionText are all no-ops so the guide's SyncControllerUI text wins.
    /// </summary>
    public void SetInstructionSilence(bool silent) => _silenceInstructions = silent;

    // --- Button pulse / lock ---
    private Coroutine pulseCoroutine = null;
    private Dictionary<UnityEngine.UI.Button, Color> originalButtonColors
        = new Dictionary<UnityEngine.UI.Button, Color>();

    static readonly Color BASE_BTN_COLOR  = new Color(0x84 / 255f, 0x69 / 255f, 0xFF / 255f, 1f);
    static readonly Color LIGHT_BTN_COLOR = new Color(0xB2 / 255f, 0xA0 / 255f, 0xFF / 255f, 1f);

    private class ListNode
    {
        public GameObject nodeObject;
        public string     cargo;
        public int        position;
        public ListNode   nextNode;
        public GameObject connector;
        public GameObject cargoLabel;
        public GameObject positionLabel;
        public GameObject headLabel;
    }

    private ListNode   headNode;
    private GameObject listScene;
    private GameObject solarBackground;
    private GameObject metroBackground;
    private GameObject movingNode;
    private Vector3    targetPosition;
    private GameObject targetIndicator;
    private Vector3    originalNodeScale = Vector3.one;

    private bool isAddingAtHead              = true;
    private Vector3 currentMovementDirection = Vector3.zero;
    private bool isMoving                    = false;
    private bool sceneSpawned               = false;
    private int  nodeIdCounter              = 1;
    private bool hasShownMovementTutorial   = false;

    // -------------------------------------------------------------------------
    //  COLOUR PALETTES
    // -------------------------------------------------------------------------
    private Color[] carColors = new Color[]
    {
        new Color(0.8f, 0.2f, 0.2f), new Color(0.2f, 0.5f, 0.9f),
        new Color(0.3f, 0.8f, 0.3f), new Color(1f,   0.7f, 0.2f),
        new Color(0.9f, 0.3f, 0.9f), new Color(0.3f, 0.9f, 0.9f),
        new Color(1f,   0.9f, 0.3f), new Color(0.6f, 0.4f, 0.2f)
    };

    private Color[] planetColors = new Color[]
    {
        new Color(0.95f, 0.65f, 0.30f), new Color(0.90f, 0.75f, 0.45f),
        new Color(0.25f, 0.55f, 0.90f), new Color(0.80f, 0.35f, 0.20f),
        new Color(0.85f, 0.70f, 0.50f), new Color(0.90f, 0.82f, 0.55f),
        new Color(0.45f, 0.80f, 0.90f), new Color(0.30f, 0.40f, 0.90f),
    };

    private Color[] ringColors = new Color[]
    {
        new Color(1.0f, 0.9f, 0.3f), new Color(1.0f, 0.8f, 0.4f),
        new Color(0.3f, 0.8f, 1.0f), new Color(1.0f, 0.4f, 0.3f),
        new Color(1.0f, 0.7f, 0.3f), new Color(0.9f, 0.9f, 0.6f),
        new Color(0.4f, 0.9f, 0.9f), new Color(0.5f, 0.5f, 1.0f),
    };

    private Color[] metroColors = new Color[]
    {
        new Color(0.05f, 0.45f, 0.85f), new Color(0.85f, 0.15f, 0.15f),
        new Color(0.10f, 0.72f, 0.36f), new Color(0.90f, 0.55f, 0.05f),
        new Color(0.55f, 0.10f, 0.75f), new Color(0.05f, 0.75f, 0.82f),
        new Color(0.90f, 0.85f, 0.05f), new Color(0.80f, 0.40f, 0.10f),
    };

    private string[] planetNames =
        { "Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune" };

    private string[] metroCargoDefaults =
        { "Line A", "Line B", "Line C", "Line D", "Express", "Local", "Night", "Shuttle" };

    // -------------------------------------------------------------------------
    //  NODE GEOMETRY HELPERS
    // -------------------------------------------------------------------------
    float NodeHalfExtent => currentScenario == Scenario.Solar ? planetRadius : carLength * 0.5f;
    float NodeGap        => currentScenario == Scenario.Solar ? planetSpacing : carSpacing;
    float NodeStep       => NodeHalfExtent * 2f + NodeGap;
    float NodeYOffset    => currentScenario == Scenario.Solar ? planetRadius : carHeight * 0.5f;

    // -------------------------------------------------------------------------
    //  ZOOM HELPERS
    // -------------------------------------------------------------------------
    float ZoomAdjustedMoveSpeed()
    {
        if (zoomController == null || !zoomController.IsInitialized()) return moveSpeed;
        float s = zoomController.GetCurrentScale();
        if (s < 0.01f) s = 0.01f;
        return moveSpeed * s;
    }

    // -------------------------------------------------------------------------
    //  INSTRUCTION TEXT HELPERS
    //  All gated by _silenceInstructions so the lesson guide's text wins.
    // -------------------------------------------------------------------------
    void UpdateInstructions(string msg)
    {
        if (_silenceInstructions) return;
        SetIC(msg, Color.white);
    }

    void UpdateInstructionsSuccess(string msg)
    {
        if (_silenceInstructions) return;
        SetIC(msg, new Color(0.2f, 1f, 0.3f));
    }

    void UpdateInstructionsError(string msg)
    {
        if (_silenceInstructions) return;
        SetIC(msg, new Color(1f, 0.25f, 0.25f));
    }

    void SetIC(string msg, Color color)
    {
        if (instructionText == null) return;
        instructionText.text  = msg;
        instructionText.color = color;
    }

    // -------------------------------------------------------------------------
    //  OPERATION INFO TEXT HELPER (gated)
    // -------------------------------------------------------------------------
    void SetOperationInfo(string msg)
    {
        if (_silenceInstructions) return;
        if (operationInfoText != null) operationInfoText.text = msg;
    }

    // -------------------------------------------------------------------------
    //  DETECTION TEXT HELPER (gated)
    // -------------------------------------------------------------------------
    void SetDetectionText(string msg, Color color)
    {
        if (_silenceInstructions) return;
        if (detectionText == null) return;
        detectionText.text  = msg;
        detectionText.color = color;
    }

    // -------------------------------------------------------------------------
    //  STATUS TEXT HELPER (gated)
    // -------------------------------------------------------------------------
    void UpdateStatus()
    {
        if (_silenceInstructions) return;
        if (statusText == null) return;
        int len = GetListLength();
        switch (currentScenario)
        {
            case Scenario.Solar:     statusText.text = $"System: {len} planets";  break;
            case Scenario.CityMetro: statusText.text = $"Metro: {len} carriages"; break;
            default:                 statusText.text = $"Train: {len} cars";       break;
        }
    }

    // -------------------------------------------------------------------------
    //  SCENARIO DISPLAY HELPERS
    // -------------------------------------------------------------------------
    string ScenarioEmoji()
    {
        switch (currentScenario)
        {
            case Scenario.Solar:     return "Solar";
            case Scenario.CityMetro: return "Metro";
            default:                 return "Train";
        }
    }

    string NodeWord()
    {
        switch (currentScenario)
        {
            case Scenario.Solar:     return "planet";
            case Scenario.CityMetro: return "carriage";
            default:                 return "car";
        }
    }

    string ScenarioDisplayName()
    {
        switch (currentScenario)
        {
            case Scenario.Solar:     return "Solar System";
            case Scenario.CityMetro: return "City Metro";
            default:                 return "Train";
        }
    }

    // -------------------------------------------------------------------------
    //  BUTTON COLOR CACHE
    // -------------------------------------------------------------------------
    void CacheAllButtonColors()
    {
        CacheButtonColor(beginnerAddHeadButton);
        CacheButtonColor(beginnerAddTailButton);
        CacheButtonColor(beginnerRemoveHeadButton);
        CacheButtonColor(beginnerTraverseButton);
        CacheButtonColor(intermediateInsertAtButton);
        CacheButtonColor(intermediateDeleteByValueButton);
        CacheButtonColor(intermediateReverseButton);
        CacheButtonColor(intermediateFindMiddleButton);
    }

    void CacheButtonColor(UnityEngine.UI.Button btn)
    {
        if (btn == null) return;
        var img = btn.GetComponent<UnityEngine.UI.Image>();
        if (img != null && !originalButtonColors.ContainsKey(btn))
            originalButtonColors[btn] = img.color;
    }

    // -------------------------------------------------------------------------
    //  BUTTON LOCK / UNLOCK
    // -------------------------------------------------------------------------
    void CheckAndUpdateButtonStates()
    {
        bool hasNodes = GetListLength() > 0;

        if (hasNodes && pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
            ResetPrimaryButtonVisual();
        }

        SetButtonInteractable(beginnerRemoveHeadButton, hasNodes);
        SetButtonInteractable(beginnerTraverseButton,   hasNodes);
        SetButtonInteractable(intermediateDeleteByValueButton, hasNodes);
        SetButtonInteractable(intermediateReverseButton,       hasNodes);
        SetButtonInteractable(intermediateFindMiddleButton,    hasNodes);

        if (!hasNodes && pulseCoroutine == null && sceneSpawned)
            pulseCoroutine = StartCoroutine(PulsePrimaryButton());
    }

    void SetButtonInteractable(UnityEngine.UI.Button btn, bool state)
    {
        if (btn == null) return;
        var img = btn.GetComponent<UnityEngine.UI.Image>();
        if (img != null && originalButtonColors.ContainsKey(btn))
            img.color = state ? originalButtonColors[btn] : new Color(0.55f, 0.55f, 0.55f, 0.7f);
        btn.interactable = state;
    }

    // -------------------------------------------------------------------------
    //  PRIMARY BUTTON PULSE
    // -------------------------------------------------------------------------
    IEnumerator PulsePrimaryButton()
    {
        float speed    = 2.5f;
        float minScale = 0.92f;
        float maxScale = 1.10f;
        float elapsed  = 0f;

        while (true)
        {
            elapsed += Time.deltaTime * speed;
            float t     = (Mathf.Sin(elapsed) + 1f) * 0.5f;
            float scale = Mathf.Lerp(minScale, maxScale, t);
            Color col   = Color.Lerp(BASE_BTN_COLOR, LIGHT_BTN_COLOR, t);

            ApplyPulse(beginnerAddHeadButton,      scale, col);
            ApplyPulse(intermediateInsertAtButton, scale, col);

            yield return null;
        }
    }

    void ApplyPulse(UnityEngine.UI.Button btn, float scale, Color col)
    {
        if (btn == null || !btn.gameObject.activeInHierarchy) return;
        btn.transform.localScale = new Vector3(scale, scale, 1f);
        var img = btn.GetComponent<UnityEngine.UI.Image>();
        if (img != null) img.color = col;
    }

    void ResetPrimaryButtonVisual()
    {
        ResetPulseBtn(beginnerAddHeadButton);
        ResetPulseBtn(intermediateInsertAtButton);
    }

    void ResetPulseBtn(UnityEngine.UI.Button btn)
    {
        if (btn == null) return;
        btn.transform.localScale = Vector3.one;
        var img = btn.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
            img.color = originalButtonColors.ContainsKey(btn) ? originalButtonColors[btn] : BASE_BTN_COLOR;
    }

    private bool _started = false;

    // -------------------------------------------------------------------------
    //  UNITY LIFECYCLE
    // -------------------------------------------------------------------------
    void Start()
    {
        if (_started) return;
        _started = true;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake  = false;
        audioSource.spatialBlend = 0f;

        if (arCamera == null) arCamera = Camera.main;
        if (planeManager != null && planePrefab != null)
            planeManager.planePrefab = planePrefab;

        if (planeManager   != null) planeManager.enabled   = false;
        if (raycastManager != null) raycastManager.enabled = false;

        CacheAllButtonColors();
        CacheLLComponents();

        HideAllPanels();
        SetupMovementButtons();

        if (detectionText != null)
        {
            detectionText.text  = "Choose a scenario first";
            detectionText.color = Color.white;
        }

        ShowScenarioPanel();
    }

    void Update()
    {
        if (currentState == ListState.WaitingForPlane)
            DetectPlaneInteraction();
        else if (currentDifficulty == Difficulty.Beginner &&
                 (currentState == ListState.AddingNode || currentState == ListState.RemovingNode))
        {
            if (isMoving && movingNode != null) MoveContinuous();
            CheckConfirmDistance();
        }

        if (currentScenario == Scenario.Solar && currentState == ListState.Ready)
            SpinPlanets();
    }

    // -------------------------------------------------------------------------
    //  PANEL HELPERS
    // -------------------------------------------------------------------------
    void HideAllPanels()
    {
        SetActive(scenarioPanel,           false);
        SetActive(difficultyPanel,         false);
        SetActive(mainButtonPanel,         false);
        SetActive(beginnerButtonPanel,     false);
        SetActive(intermediateButtonPanel, false);
        SetActive(carInputPanel,           false);
        SetActive(insertAtInputPanel,      false);
        SetActive(deleteValueInputPanel,   false);
        SetActive(movementControlPanel,    false);
        SetActive(confirmButton,           false);
        SetActive(explanationPanel,        false);
    }

    void ShowModeButtons()
    {
        SetActive(mainButtonPanel,         true);
        SetActive(beginnerButtonPanel,     currentDifficulty == Difficulty.Beginner);
        SetActive(intermediateButtonPanel, currentDifficulty == Difficulty.Intermediate);
        CheckAndUpdateButtonStates();
    }

    // -------------------------------------------------------------------------
    //  SCENARIO PANEL
    // -------------------------------------------------------------------------
    void ShowScenarioPanel()
    {
        currentState = ListState.ChoosingScenario;
        HideAllPanels();
        SetActive(scenarioPanel, true);

        if (scenarioTitleText != null)
            scenarioTitleText.text = "LINKED LIST\nChoose Your World";

        UpdateInstructions("Pick a scenario to get started!");

        if (detectionText != null)
        {
            detectionText.text  = "Choose a scenario first";
            detectionText.color = Color.white;
        }

        if (btnScenarioTrain != null)
        {
            btnScenarioTrain.onClick.RemoveAllListeners();
            btnScenarioTrain.onClick.AddListener(() => OnScenarioChosen(Scenario.Train));
        }
        if (btnScenarioSolar != null)
        {
            btnScenarioSolar.onClick.RemoveAllListeners();
            btnScenarioSolar.onClick.AddListener(() => OnScenarioChosen(Scenario.Solar));
        }
        if (btnScenarioCityMetro != null)
        {
            btnScenarioCityMetro.onClick.RemoveAllListeners();
            btnScenarioCityMetro.onClick.AddListener(() => OnScenarioChosen(Scenario.CityMetro));
        }
    }

    public void OnScenarioChosen(Scenario chosen)
    {
        currentScenario = chosen;
        SetActive(scenarioPanel, false);
        ShowDifficultyPanel();
    }

    public void OnScenarioTrain()     => OnScenarioChosen(Scenario.Train);
    public void OnScenarioSolar()     => OnScenarioChosen(Scenario.Solar);
    public void OnScenarioCityMetro() => OnScenarioChosen(Scenario.CityMetro);

    // -------------------------------------------------------------------------
    //  DIFFICULTY PANEL
    // -------------------------------------------------------------------------
    void ShowDifficultyPanel()
    {
        currentState = ListState.ChoosingDifficulty;
        HideAllPanels();
        SetActive(difficultyPanel, true);

        string sName;
        switch (currentScenario)
        {
            case Scenario.Solar:     sName = "Solar System"; break;
            case Scenario.CityMetro: sName = "City Metro";   break;
            default:                 sName = "Train";         break;
        }

        if (difficultyTitleText != null)
            difficultyTitleText.text = $"LINKED LIST - {sName}\nChoose Difficulty";

        UpdateInstructions("Choose your difficulty level");

        if (beginnerBtn != null)
        {
            beginnerBtn.onClick.RemoveAllListeners();
            beginnerBtn.onClick.AddListener(() => OnDifficultyChosen(Difficulty.Beginner));
        }
        if (intermediateBtn != null)
        {
            intermediateBtn.onClick.RemoveAllListeners();
            intermediateBtn.onClick.AddListener(() => OnDifficultyChosen(Difficulty.Intermediate));
        }
    }

    void OnDifficultyChosen(Difficulty chosen)
    {
        currentDifficulty = chosen;
        SetActive(difficultyPanel, false);

        if (planeManager   != null) planeManager.enabled   = true;
        if (raycastManager != null) raycastManager.enabled = true;

        currentState = ListState.WaitingForPlane;
        UpdateInstructions("Point camera at a flat surface and tap to place!");

        if (detectionText != null && !_silenceInstructions)
        {
            detectionText.text  = "Looking for surfaces...";
            detectionText.color = Color.yellow;
        }
    }

    // -------------------------------------------------------------------------
    //  AR PLANE TAP
    // -------------------------------------------------------------------------
    void DetectPlaneInteraction()
    {
        if (sceneSpawned) return;
        bool inputReceived = false;
        Vector2 screenPos  = Vector2.zero;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Touch t = Input.GetTouch(0);
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(t.fingerId)) return;
            screenPos = t.position; inputReceived = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            screenPos = Input.mousePosition; inputReceived = true;
        }

        if (!inputReceived || raycastManager == null) return;
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
            SpawnScene(hits[0].pose.position, hits[0].pose.rotation);
    }

    // -------------------------------------------------------------------------
    //  SPAWN SCENE
    // -------------------------------------------------------------------------
    void SpawnScene(Vector3 position, Quaternion rotation)
    {
        sceneSpawned = true;
        PlaySound(placeSceneSound);

        if (planeManager != null)
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);

        listScene = new GameObject("LinkedListScene");
        listScene.transform.position = position + Vector3.up * sceneHeightOffset;
        float yaw = rotation.eulerAngles.y;
        listScene.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (swipeRotation  != null) swipeRotation.InitializeRotation(listScene.transform);
        if (zoomController != null) zoomController.InitializeZoom(listScene.transform);

        if (detectionText != null && !_silenceInstructions)
        { detectionText.text = "Scene Placed!"; detectionText.color = Color.green; }

        switch (currentScenario)
        {
            case Scenario.Solar:     BuildSolarScene();  break;
            case Scenario.CityMetro: BuildMetroScene();  break;
            default:                 BuildTrainScene();  break;
        }

        StartGame();
    }

    // -------------------------------------------------------------------------
    //  BUILD SCENES
    // -------------------------------------------------------------------------
    void BuildTrainScene()
    {
        if (trackPrefab != null)
        {
            GameObject tp = Instantiate(trackPrefab, listScene.transform);
            tp.transform.localPosition = Vector3.zero;
            tp.transform.localRotation = Quaternion.identity;
        }
        else
        {
            GameObject tracks = new GameObject("Tracks");
            tracks.transform.SetParent(listScene.transform);
            tracks.transform.localPosition = Vector3.zero;
            tracks.transform.localRotation = Quaternion.identity;
            for (int i = -5; i < 5; i++)
            {
                CreateRailSegment(tracks.transform, i, -0.02f);
                CreateRailSegment(tracks.transform, i,  0.02f);
            }
        }

        GameObject station = GameObject.CreatePrimitive(PrimitiveType.Cube);
        station.name = "Station";
        station.transform.SetParent(listScene.transform);
        station.transform.localPosition = new Vector3(-0.3f, 0.03f, 0);
        station.transform.localScale    = new Vector3(0.1f, 0.06f, 0.1f);
        station.transform.localRotation = Quaternion.identity;
        SetMatColor(station, new Color(0.7f, 0.5f, 0.3f));
        Destroy(station.GetComponent<Collider>());
        CreateTextLabel(station.transform, "STATION",
            new Vector3(0, 0.05f, 0), Color.white, 30, new Vector3(0.003f, 0.003f, 0.003f));
    }

    void CreateRailSegment(Transform parent, int i, float zOffset)
    {
        GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.transform.SetParent(parent);
        rail.transform.localPosition = new Vector3(i * 0.1f, -0.01f, zOffset);
        rail.transform.localScale    = new Vector3(0.08f, 0.005f, 0.01f);
        rail.transform.localRotation = Quaternion.identity;
        SetMatColor(rail, new Color(0.3f, 0.3f, 0.3f));
        Destroy(rail.GetComponent<Collider>());
    }

    void BuildSolarScene()
    {
        const float laneHalfWidth = 0.42f;
        const float laneWidth     = laneHalfWidth * 2f;

        solarBackground = new GameObject("SolarRoot");
        solarBackground.transform.SetParent(listScene.transform);
        solarBackground.transform.localPosition = Vector3.zero;
        solarBackground.transform.localRotation = Quaternion.identity;

        GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lane.name = "OrbitalLane";
        lane.transform.SetParent(listScene.transform);
        lane.transform.localPosition = new Vector3(0f, -0.001f, 0f);
        lane.transform.localScale    = new Vector3(laneWidth, 0.003f, 0.022f);
        lane.transform.localRotation = Quaternion.identity;
        Destroy(lane.GetComponent<Collider>());
        Renderer lrend = lane.GetComponent<Renderer>();
        Material lmat  = new Material(Shader.Find("Unlit/Color"));
        lmat.color     = new Color(0.2f, 0.6f, 0.9f, 0.9f);
        lrend.material = lmat;

        for (int e = 0; e < 2; e++)
        {
            float ez = e == 0 ? 0.013f : -0.013f;
            GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.transform.SetParent(listScene.transform);
            edge.transform.localPosition = new Vector3(0f, 0.001f, ez);
            edge.transform.localScale    = new Vector3(laneWidth, 0.003f, 0.003f);
            edge.transform.localRotation = Quaternion.identity;
            Destroy(edge.GetComponent<Collider>());
            Renderer er = edge.GetComponent<Renderer>();
            Material em = new Material(Shader.Find("Unlit/Color"));
            em.color    = new Color(0.4f, 0.85f, 1.0f);
            er.material = em;
        }

        System.Random rng = new System.Random(99);
        for (int d = 0; d < 28; d++)
        {
            float dx = (float)(rng.NextDouble() - 0.5) * laneWidth * 1.1f;
            float dz = (float)(rng.NextDouble() > 0.5 ? 1 : -1)
                     * ((float)(rng.NextDouble() * 0.06f) + 0.018f);
            float dy = (float)(rng.NextDouble() * planetRadius * 3f) + 0.002f;

            GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debris.name = "Debris";
            debris.transform.SetParent(solarBackground.transform);
            debris.transform.localPosition = new Vector3(dx, dy, dz);

            float bx = (float)(rng.NextDouble() * 0.007f + 0.003f);
            float by = (float)(rng.NextDouble() * 0.004f + 0.002f);
            float bz2 = (float)(rng.NextDouble() * 0.007f + 0.003f);
            debris.transform.localScale    = new Vector3(bx, by, bz2);
            debris.transform.localRotation = Quaternion.Euler(
                (float)(rng.NextDouble() * 360f),
                (float)(rng.NextDouble() * 360f),
                (float)(rng.NextDouble() * 360f));

            Renderer dr = debris.GetComponent<Renderer>();
            Material dm = new Material(Shader.Find("Standard"));
            float rv = (float)(rng.NextDouble() * 0.25f + 0.30f);
            float gv = (float)(rng.NextDouble() * 0.20f + 0.25f);
            float bv = (float)(rng.NextDouble() * 0.15f + 0.20f);
            dm.color  = new Color(rv, gv, bv);
            dm.SetFloat("_Glossiness", 0.05f);
            dr.material = dm;
            Destroy(debris.GetComponent<Collider>());
        }

        GameObject labelAnchor = new GameObject("SolarLabel");
        labelAnchor.transform.SetParent(listScene.transform);
        labelAnchor.transform.localPosition = new Vector3(-0.3f, planetRadius * 1.8f, 0f);
        CreateTextLabel(labelAnchor.transform, "SOLAR\nSYSTEM",
            Vector3.zero, new Color(0.4f, 0.85f, 1.0f), 28,
            new Vector3(0.003f, 0.003f, 0.003f));
    }

    void BuildMetroScene()
    {
        metroBackground = new GameObject("MetroRoot");
        metroBackground.transform.SetParent(listScene.transform);
        metroBackground.transform.localPosition = Vector3.zero;
        metroBackground.transform.localRotation = Quaternion.identity;

        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "Platform";
        platform.transform.SetParent(metroBackground.transform);
        platform.transform.localPosition = new Vector3(0f, -0.005f, 0f);
        platform.transform.localScale    = new Vector3(0.85f, 0.004f, 0.14f);
        platform.transform.localRotation = Quaternion.identity;
        Destroy(platform.GetComponent<Collider>());
        Renderer pfRend = platform.GetComponent<Renderer>();
        Material pfMat  = new Material(Shader.Find("Standard"));
        pfMat.color = new Color(0.18f, 0.18f, 0.22f);
        pfMat.SetFloat("_Glossiness", 0.6f);
        pfRend.material = pfMat;

        for (int t = -4; t <= 4; t++)
        {
            GameObject seam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seam.transform.SetParent(metroBackground.transform);
            seam.transform.localPosition = new Vector3(t * 0.10f, 0.0f, 0f);
            seam.transform.localScale    = new Vector3(0.002f, 0.002f, 0.14f);
            seam.transform.localRotation = Quaternion.identity;
            Destroy(seam.GetComponent<Collider>());
            Renderer sr = seam.GetComponent<Renderer>();
            Material sm = new Material(Shader.Find("Unlit/Color"));
            sm.color = new Color(0.30f, 0.30f, 0.35f);
            sr.material = sm;
        }

        for (int side = 0; side < 2; side++)
        {
            float ez = side == 0 ? 0.065f : -0.065f;
            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "SafetyStrip";
            strip.transform.SetParent(metroBackground.transform);
            strip.transform.localPosition = new Vector3(0f, 0.001f, ez);
            strip.transform.localScale    = new Vector3(0.85f, 0.003f, 0.008f);
            strip.transform.localRotation = Quaternion.identity;
            Destroy(strip.GetComponent<Collider>());
            Renderer stR = strip.GetComponent<Renderer>();
            Material stM = new Material(Shader.Find("Unlit/Color"));
            stM.color    = new Color(1.0f, 0.85f, 0.0f);
            stR.material = stM;
        }

        for (int side = 0; side < 2; side++)
        {
            float wz = side == 0 ? 0.09f : -0.09f;
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "TunnelWall";
            wall.transform.SetParent(metroBackground.transform);
            wall.transform.localPosition = new Vector3(0f, 0.045f, wz);
            wall.transform.localScale    = new Vector3(0.85f, 0.09f, 0.005f);
            wall.transform.localRotation = Quaternion.identity;
            Destroy(wall.GetComponent<Collider>());
            Renderer wr = wall.GetComponent<Renderer>();
            Material wm = new Material(Shader.Find("Standard"));
            wm.color = new Color(0.12f, 0.12f, 0.16f);
            wm.SetFloat("_Glossiness", 0.2f);
            wr.material = wm;
        }

        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Ceiling";
        ceiling.transform.SetParent(metroBackground.transform);
        ceiling.transform.localPosition = new Vector3(0f, 0.092f, 0f);
        ceiling.transform.localScale    = new Vector3(0.85f, 0.005f, 0.185f);
        ceiling.transform.localRotation = Quaternion.identity;
        Destroy(ceiling.GetComponent<Collider>());
        Renderer cR = ceiling.GetComponent<Renderer>();
        Material cM = new Material(Shader.Find("Standard"));
        cM.color = new Color(0.10f, 0.10f, 0.14f);
        cM.SetFloat("_Glossiness", 0.15f);
        cR.material = cM;

        Color neonBlue = new Color(0.2f, 0.7f, 1.0f);
        for (int n = -3; n <= 3; n++)
        {
            GameObject neon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            neon.name = "NeonStrip";
            neon.transform.SetParent(metroBackground.transform);
            neon.transform.localPosition = new Vector3(n * 0.13f, 0.088f, 0f);
            neon.transform.localScale    = new Vector3(0.085f, 0.003f, 0.005f);
            neon.transform.localRotation = Quaternion.identity;
            Destroy(neon.GetComponent<Collider>());
            Renderer nr = neon.GetComponent<Renderer>();
            Material nm = new Material(Shader.Find("Unlit/Color"));
            nm.color    = neonBlue;
            nr.material = nm;
        }

        for (int rail = 0; rail < 2; rail++)
        {
            float rz = rail == 0 ? 0.018f : -0.018f;
            GameObject track = GameObject.CreatePrimitive(PrimitiveType.Cube);
            track.transform.SetParent(metroBackground.transform);
            track.transform.localPosition = new Vector3(0f, -0.003f, rz);
            track.transform.localScale    = new Vector3(0.85f, 0.005f, 0.006f);
            track.transform.localRotation = Quaternion.identity;
            Destroy(track.GetComponent<Collider>());
            Renderer trR = track.GetComponent<Renderer>();
            Material trM = new Material(Shader.Find("Standard"));
            trM.color = new Color(0.45f, 0.45f, 0.50f);
            trM.SetFloat("_Glossiness", 0.8f);
            trR.material = trM;
        }

        GameObject boardAnchor = new GameObject("DepartureBoard");
        boardAnchor.transform.SetParent(metroBackground.transform);
        boardAnchor.transform.localPosition = new Vector3(-0.3f, 0.075f, 0.07f);
        CreateTextLabel(boardAnchor.transform, "CITY\nMETRO",
            Vector3.zero, new Color(0.25f, 0.80f, 1.0f), 28,
            new Vector3(0.003f, 0.003f, 0.003f));
    }

    // -------------------------------------------------------------------------
    //  NODE FACTORIES
    // -------------------------------------------------------------------------
    GameObject CreateNode(string cargo, int colorIndex)
    {
        switch (currentScenario)
        {
            case Scenario.Solar:     return CreatePlanet(cargo, colorIndex);
            case Scenario.CityMetro: return CreateMetroCarriage(cargo, colorIndex);
            default:                 return CreateTrainCar(cargo, colorIndex);
        }
    }

    GameObject CreateTrainCar(string cargo, int colorIndex)
    {
        if (trainCarPrefab != null)
        {
            GameObject inst = Instantiate(trainCarPrefab);
            originalNodeScale = inst.transform.localScale;
            return inst;
        }

        GameObject car = new GameObject($"Car_{nodeIdCounter++}");
        Color col = carColors[colorIndex % carColors.Length];

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(car.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale    = new Vector3(carLength, carHeight, carWidth);
        SetMatColor(body, col);
        Destroy(body.GetComponent<Collider>());

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(car.transform);
        roof.transform.localPosition = new Vector3(0, carHeight * 0.6f, 0);
        roof.transform.localScale    = new Vector3(carLength * 0.9f, carHeight * 0.2f, carWidth * 0.9f);
        SetMatColor(roof, col * 0.8f);
        Destroy(roof.GetComponent<Collider>());

        for (int i = 0; i < 2; i++)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.transform.SetParent(car.transform);
            wheel.transform.localPosition = new Vector3(
                i == 0 ? -carLength * 0.3f : carLength * 0.3f, -carHeight * 0.6f, 0);
            wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
            wheel.transform.localScale    = new Vector3(carWidth * 0.4f, 0.01f, carWidth * 0.4f);
            SetMatColor(wheel, Color.black);
            Destroy(wheel.GetComponent<Collider>());
        }

        car.AddComponent<BoxCollider>().size = new Vector3(carLength, carHeight, carWidth);
        originalNodeScale = car.transform.localScale;
        return car;
    }

    GameObject CreatePlanet(string cargo, int colorIndex)
    {
        GameObject planet = new GameObject($"Planet_{nodeIdCounter++}");
        float pr   = planetRadius;
        Color col  = planetColors[colorIndex % planetColors.Length];
        Color ring = ringColors[colorIndex  % ringColors.Length];

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.SetParent(planet.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale    = Vector3.one * pr * 2f;
        SetMatColor(body, col, 0.3f);
        Destroy(body.GetComponent<Collider>());

        GameObject atmo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        atmo.transform.SetParent(planet.transform);
        atmo.transform.localPosition = Vector3.zero;
        atmo.transform.localScale    = Vector3.one * pr * 2.25f;
        Renderer ar2 = atmo.GetComponent<Renderer>();
        Material am  = new Material(Shader.Find("Standard"));
        Color ac = col; ac.a = 0.18f;
        am.color = ac;
        am.SetFloat("_Mode", 3);
        am.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        am.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        am.SetInt("_ZWrite", 0);
        am.EnableKeyword("_ALPHABLEND_ON");
        am.renderQueue = 3000;
        ar2.material = am;
        Destroy(atmo.GetComponent<Collider>());

        for (int d = 0; d < 2; d++)
        {
            float angle = d * 140f + 30f;
            float rad   = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad) * 0.4f, 0.3f).normalized * pr * 0.95f;
            GameObject detail = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            detail.transform.SetParent(planet.transform);
            detail.transform.localPosition = offset;
            detail.transform.localScale    = Vector3.one * pr * 0.35f;
            SetMatColor(detail, col * (d == 0 ? 0.75f : 0.85f), 0.1f);
            Destroy(detail.GetComponent<Collider>());
        }

        if (colorIndex % 3 == 2)
        {
            GameObject ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringObj.transform.SetParent(planet.transform);
            ringObj.transform.localPosition = Vector3.zero;
            ringObj.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
            ringObj.transform.localScale    = new Vector3(pr * 3.2f, pr * 0.04f, pr * 3.2f);
            Renderer rr = ringObj.GetComponent<Renderer>();
            Material rm = new Material(Shader.Find("Standard"));
            Color rc = ring; rc.a = 0.55f;
            rm.color = rc;
            rm.SetFloat("_Mode", 3);
            rm.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            rm.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            rm.SetInt("_ZWrite", 0);
            rm.EnableKeyword("_ALPHABLEND_ON");
            rm.renderQueue = 3001;
            rr.material = rm;
            Destroy(ringObj.GetComponent<Collider>());
        }

        GameObject orbitDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        orbitDisc.transform.SetParent(planet.transform);
        orbitDisc.transform.localPosition = new Vector3(0, -pr * 1.05f, 0);
        orbitDisc.transform.localScale    = new Vector3(pr * 2.8f, 0.002f, pr * 2.8f);
        Renderer od = orbitDisc.GetComponent<Renderer>();
        Material om = new Material(Shader.Find("Unlit/Color"));
        om.color = new Color(ring.r, ring.g, ring.b, 0.6f);
        od.material = om;
        Destroy(orbitDisc.GetComponent<Collider>());

        SphereCollider sc = planet.AddComponent<SphereCollider>();
        sc.radius = pr * 1.1f;

        originalNodeScale = planet.transform.localScale;
        return planet;
    }

    GameObject CreateMetroCarriage(string cargo, int colorIndex)
    {
        GameObject car = new GameObject($"Metro_{nodeIdCounter++}");
        Color lineCol  = metroColors[colorIndex % metroColors.Length];

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(car.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale    = new Vector3(carLength, carHeight, carWidth);
        Destroy(body.GetComponent<Collider>());
        Renderer bRend = body.GetComponent<Renderer>();
        Material bMat  = new Material(Shader.Find("Standard"));
        bMat.color = new Color(0.14f, 0.14f, 0.18f);
        bMat.SetFloat("_Glossiness", 0.7f);
        bRend.material = bMat;

        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.transform.SetParent(car.transform);
        stripe.transform.localPosition = new Vector3(0f, carHeight * 0.15f, carWidth * 0.501f);
        stripe.transform.localScale    = new Vector3(carLength, carHeight * 0.22f, 0.001f);
        stripe.transform.localRotation = Quaternion.identity;
        Destroy(stripe.GetComponent<Collider>());
        Renderer stRend = stripe.GetComponent<Renderer>();
        Material stMat  = new Material(Shader.Find("Unlit/Color"));
        stMat.color     = lineCol;
        stRend.material = stMat;

        for (int w = 0; w < 3; w++)
        {
            float wx = (w - 1) * carLength * 0.28f;
            GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
            win.transform.SetParent(car.transform);
            win.transform.localPosition = new Vector3(wx, carHeight * 0.18f, carWidth * 0.502f);
            win.transform.localScale    = new Vector3(carLength * 0.18f, carHeight * 0.26f, 0.001f);
            win.transform.localRotation = Quaternion.identity;
            Destroy(win.GetComponent<Collider>());
            Renderer wR = win.GetComponent<Renderer>();
            Material wM = new Material(Shader.Find("Standard"));
            wM.color = new Color(0.35f, 0.70f, 0.95f, 0.8f);
            wM.SetFloat("_Glossiness", 0.9f);
            wR.material = wM;
        }

        GameObject led = GameObject.CreatePrimitive(PrimitiveType.Cube);
        led.transform.SetParent(car.transform);
        led.transform.localPosition = new Vector3(carLength * 0.502f, carHeight * 0.25f, 0f);
        led.transform.localScale    = new Vector3(0.001f, carHeight * 0.22f, carWidth * 0.55f);
        led.transform.localRotation = Quaternion.identity;
        Destroy(led.GetComponent<Collider>());
        Renderer lR = led.GetComponent<Renderer>();
        Material lM = new Material(Shader.Find("Unlit/Color"));
        lM.color    = new Color(lineCol.r * 0.5f, lineCol.g * 0.5f, lineCol.b * 0.5f);
        lR.material = lM;

        for (int b = 0; b < 2; b++)
        {
            float bx = b == 0 ? -carLength * 0.3f : carLength * 0.3f;
            for (int side = 0; side < 2; side++)
            {
                float bz = side == 0 ? carWidth * 0.52f : -carWidth * 0.52f;
                GameObject bogie = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bogie.transform.SetParent(car.transform);
                bogie.transform.localPosition = new Vector3(bx, -carHeight * 0.55f, bz);
                bogie.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                bogie.transform.localScale    = new Vector3(carWidth * 0.18f, 0.007f, carWidth * 0.18f);
                Destroy(bogie.GetComponent<Collider>());
                Renderer bgR = bogie.GetComponent<Renderer>();
                Material bgM = new Material(Shader.Find("Standard"));
                bgM.color = new Color(0.12f, 0.12f, 0.12f);
                bgM.SetFloat("_Glossiness", 0.5f);
                bgR.material = bgM;
            }
        }

        car.AddComponent<BoxCollider>().size = new Vector3(carLength, carHeight, carWidth);
        originalNodeScale = car.transform.localScale;
        return car;
    }

    // -------------------------------------------------------------------------
    //  CONNECTORS
    // -------------------------------------------------------------------------
    GameObject CreateConnector(Vector3 startLocal, Vector3 endLocal)
    {
        switch (currentScenario)
        {
            case Scenario.Solar:     return CreateOrbitalConnector(startLocal, endLocal);
            case Scenario.CityMetro: return CreateTunnelConnector(startLocal, endLocal);
            default:                 return CreateRodConnector(startLocal, endLocal);
        }
    }

    GameObject CreateRodConnector(Vector3 startLocal, Vector3 endLocal)
    {
        GameObject rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rod.transform.SetParent(listScene.transform);

        Vector3 dir  = endLocal - startLocal;
        float   dist = dir.magnitude;

        rod.transform.localPosition = startLocal + dir * 0.5f;
        rod.transform.up            = listScene.transform.TransformDirection(dir.normalized);
        rod.transform.localScale    = new Vector3(0.005f, dist * 0.5f, 0.005f);
        SetMatColor(rod, new Color(0.2f, 0.2f, 0.2f));
        Destroy(rod.GetComponent<Collider>());
        return rod;
    }

    GameObject CreateOrbitalConnector(Vector3 startLocal, Vector3 endLocal)
    {
        GameObject conn = new GameObject("OrbitalPath");
        conn.transform.SetParent(listScene.transform);
        conn.transform.localPosition = Vector3.zero;
        conn.transform.localRotation = Quaternion.identity;

        Vector3 dir   = endLocal - startLocal;
        float   dist  = dir.magnitude;
        int     beads = Mathf.Max(4, Mathf.RoundToInt(dist / 0.012f));
        Color beadColor = new Color(0.35f, 0.85f, 1.0f);

        for (int i = 1; i < beads; i++)
        {
            float   t     = (float)i / beads;
            float   scale = (Mathf.Abs(t - 0.5f) < 0.15f) ? 0.006f : 0.004f;
            GameObject bead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bead.transform.SetParent(conn.transform);
            bead.transform.localPosition = startLocal + dir * t;
            bead.transform.localScale    = Vector3.one * scale;
            Renderer br = bead.GetComponent<Renderer>();
            Material bm = new Material(Shader.Find("Unlit/Color"));
            float alpha = Mathf.Lerp(0.4f, 1.0f, 1f - Mathf.Abs(t - 0.5f) * 1.8f);
            bm.color    = new Color(beadColor.r, beadColor.g, beadColor.b, Mathf.Clamp01(alpha));
            br.material = bm;
            Destroy(bead.GetComponent<Collider>());
        }

        Vector3 arrowPos = startLocal + dir * 0.55f;
        GameObject arrow = new GameObject("Arrow");
        arrow.transform.SetParent(conn.transform);
        arrow.transform.localPosition = arrowPos;
        for (int a = 0; a < 2; a++)
        {
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.transform.SetParent(arrow.transform);
            arm.transform.localRotation = Quaternion.Euler(0, 0, a == 0 ? 35f : -35f);
            arm.transform.localPosition = new Vector3(0.003f, a == 0 ? 0.003f : -0.003f, 0);
            arm.transform.localScale    = new Vector3(0.003f, 0.009f, 0.002f);
            Renderer armR = arm.GetComponent<Renderer>();
            Material armM = new Material(Shader.Find("Unlit/Color"));
            armM.color    = new Color(0.9f, 0.9f, 0.3f);
            armR.material = armM;
            Destroy(arm.GetComponent<Collider>());
        }
        return conn;
    }

    GameObject CreateTunnelConnector(Vector3 startLocal, Vector3 endLocal)
    {
        GameObject conn = new GameObject("TunnelConnector");
        conn.transform.SetParent(listScene.transform);
        conn.transform.localPosition = Vector3.zero;
        conn.transform.localRotation = Quaternion.identity;

        Vector3 dir  = endLocal - startLocal;
        float   dist = dir.magnitude;

        Color neonCyan = new Color(0.2f, 0.8f, 1.0f);
        int   segs     = Mathf.Max(3, Mathf.RoundToInt(dist / 0.01f));

        for (int i = 1; i < segs; i++)
        {
            float   t     = (float)i / segs;
            float   brite = 0.6f + 0.4f * Mathf.Sin(t * Mathf.PI);
            float   sz    = 0.005f * brite;

            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            seg.transform.SetParent(conn.transform);
            seg.transform.localPosition = startLocal + dir * t;
            seg.transform.localScale    = Vector3.one * sz;
            Destroy(seg.GetComponent<Collider>());
            Renderer sR = seg.GetComponent<Renderer>();
            Material sM = new Material(Shader.Find("Unlit/Color"));
            sM.color    = new Color(neonCyan.r, neonCyan.g, neonCyan.b, brite);
            sR.material = sM;
        }

        Vector3 arrowPos  = startLocal + dir * 0.52f;
        Vector3 arrowFwd  = dir.normalized;
        Vector3 arrowPerp = Vector3.Cross(arrowFwd, Vector3.up).normalized;
        if (arrowPerp == Vector3.zero) arrowPerp = Vector3.right;

        for (int a = 0; a < 2; a++)
        {
            float   sign  = a == 0 ? 1f : -1f;
            Vector3 tipA  = arrowPos + arrowFwd * 0.007f;
            Vector3 baseA = arrowPos + arrowPerp * sign * 0.006f - arrowFwd * 0.007f;
            Vector3 armDir = (tipA - baseA).normalized;
            float   armLen = Vector3.Distance(tipA, baseA);

            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.transform.SetParent(conn.transform);
            arm.transform.localPosition = (tipA + baseA) * 0.5f;
            arm.transform.up            = listScene.transform.TransformDirection(armDir);
            arm.transform.localScale    = new Vector3(0.002f, armLen * 0.5f, 0.002f);
            Destroy(arm.GetComponent<Collider>());
            Renderer aR = arm.GetComponent<Renderer>();
            Material aM = new Material(Shader.Find("Unlit/Color"));
            aM.color    = new Color(1.0f, 0.90f, 0.0f);
            aR.material = aM;
        }

        return conn;
    }

    // -------------------------------------------------------------------------
    //  AMBIENT PLANET SPIN (Solar only)
    // -------------------------------------------------------------------------
    void SpinPlanets()
    {
        ListNode cur = headNode;
        while (cur != null)
        {
            if (cur.nodeObject != null)
                cur.nodeObject.transform.Rotate(Vector3.up, 8f * Time.deltaTime, Space.Self);
            cur = cur.nextNode;
        }
    }

    // -------------------------------------------------------------------------
    //  ADD NODE DIRECT (used by PreFillForLesson)
    // -------------------------------------------------------------------------
    void AddNodeDirect(string cargo, bool atTail)
    {
        int colorIndex     = GetListLength();
        GameObject nodeObj = CreateNode(cargo, colorIndex);

        nodeObj.transform.SetParent(listScene.transform);
        nodeObj.transform.localScale    = originalNodeScale;
        nodeObj.transform.localRotation = Quaternion.identity;

        ListNode newNode = new ListNode { nodeObject = nodeObj, cargo = cargo };
        float yOff = NodeYOffset;

        if (headNode == null)
        {
            headNode = newNode;
            nodeObj.transform.localPosition = new Vector3(0, yOff, 0);
            newNode.headLabel = CreateTextLabel(nodeObj.transform, HeadLabel,
                new Vector3(0, HeadLabelYOffset, 0),
                new Color(1, 0.5f, 0), 40, new Vector3(0.003f, 0.003f, 0.003f));
        }
        else if (!atTail)
        {
            float newX = headNode.nodeObject.transform.localPosition.x - NodeStep;
            nodeObj.transform.localPosition = new Vector3(newX, yOff, 0);
            newNode.nextNode  = headNode;
            newNode.connector = CreateConnector(
                nodeObj.transform.localPosition   + new Vector3(NodeHalfExtent, 0, 0),
                headNode.nodeObject.transform.localPosition - new Vector3(NodeHalfExtent, 0, 0));
            newNode.connector.transform.SetParent(listScene.transform);
            ClearHeadLabel(headNode);
            headNode = newNode;
            newNode.headLabel = CreateTextLabel(nodeObj.transform, HeadLabel,
                new Vector3(0, HeadLabelYOffset, 0),
                new Color(1, 0.5f, 0), 40, new Vector3(0.003f, 0.003f, 0.003f));
        }
        else
        {
            ListNode tail = GetTailNode();
            float newX = tail.nodeObject.transform.localPosition.x + NodeStep;
            nodeObj.transform.localPosition = new Vector3(newX, yOff, 0);
            tail.nextNode     = newNode;
            newNode.connector = CreateConnector(
                tail.nodeObject.transform.localPosition + new Vector3(NodeHalfExtent, 0, 0),
                nodeObj.transform.localPosition         - new Vector3(NodeHalfExtent, 0, 0));
            newNode.connector.transform.SetParent(listScene.transform);
        }

        newNode.cargoLabel = CreateTextLabel(nodeObj.transform,
            cargo, new Vector3(0, CargoLabelYOffset, 0),
            Color.white, 30, new Vector3(0.003f, 0.003f, 0.003f));

        UpdatePositionLabels();
        UpdateStatus();
    }

    // -------------------------------------------------------------------------
    //  GUIDE / ASSESSMENT NOTIFY WIRING
    // -------------------------------------------------------------------------
    void CacheLLComponents()
    {
        if (cachedLLGuide      == null) cachedLLGuide      = FindObjectOfType<ARLinkedListLessonGuide>();
        if (cachedLLAssessment == null) cachedLLAssessment = FindObjectOfType<ARLinkedListLessonAssessment>();
        Debug.Log($"[ITL] Guide: {(cachedLLGuide != null ? "FOUND" : "NULL")}, " +
                  $"Assessment: {(cachedLLAssessment != null ? "FOUND" : "NULL")}");
    }

    public void NotifyGuideAndAssessmentAddHead()     { cachedLLGuide?.NotifyAddHead();       cachedLLAssessment?.NotifyAddHead(); }
    public void NotifyGuideAndAssessmentAddTail()     { cachedLLGuide?.NotifyAddTail();       cachedLLAssessment?.NotifyAddTail(); }
    public void NotifyGuideAndAssessmentRemoveHead()  { cachedLLGuide?.NotifyRemoveHead();    cachedLLAssessment?.NotifyRemoveHead(); }
    public void NotifyGuideAndAssessmentTraverse()    { cachedLLGuide?.NotifyTraverse();      cachedLLAssessment?.NotifyTraverse(); }
    public void NotifyGuideAndAssessmentInsertAt()    { cachedLLGuide?.NotifyInsertAt();      cachedLLAssessment?.NotifyInsertAt(); }
    public void NotifyGuideAndAssessmentDeleteByValue(){ cachedLLGuide?.NotifyDeleteByValue();cachedLLAssessment?.NotifyDeleteByValue(); }
    public void NotifyGuideAndAssessmentReverse()     { cachedLLGuide?.NotifyReverse();       cachedLLAssessment?.NotifyReverse(); }
    public void NotifyGuideAndAssessmentFindMiddle()  { cachedLLGuide?.NotifyFindMiddle();    cachedLLAssessment?.NotifyFindMiddle(); }

    // -------------------------------------------------------------------------
    //  LABEL Y-OFFSETS
    // -------------------------------------------------------------------------
    string HeadLabel        => currentScenario == Scenario.Solar ? "HEAD" : "HEAD";
    float  HeadLabelYOffset => currentScenario == Scenario.Solar ? planetRadius * 1.8f  : carHeight * 1.5f;
    float  CargoLabelYOffset=> currentScenario == Scenario.Solar ? planetRadius * 0.5f  : carHeight * 0.8f;
    float  PosLabelYOffset  => currentScenario == Scenario.Solar ? -planetRadius * 1.6f : -carHeight * 1.2f;

    void ClearHeadLabel(ListNode node)
    {
        if (node?.headLabel != null) { Destroy(node.headLabel); node.headLabel = null; }
    }

    void UpdatePositionLabels()
    {
        ListNode cur = headNode;
        int pos = 0;
        while (cur != null)
        {
            cur.position = pos;
            if (cur.positionLabel != null) Destroy(cur.positionLabel);
            cur.positionLabel = CreateTextLabel(cur.nodeObject.transform,
                $"[{pos}]", new Vector3(0, PosLabelYOffset, 0),
                Color.yellow, 25, new Vector3(0.003f, 0.003f, 0.003f));
            cur = cur.nextNode;
            pos++;
        }
    }

    int GetListLength()
    {
        int count = 0;
        ListNode cur = headNode;
        while (cur != null) { count++; cur = cur.nextNode; }
        return count;
    }

    ListNode GetTailNode()
    {
        if (headNode == null) return null;
        ListNode cur = headNode;
        while (cur.nextNode != null) cur = cur.nextNode;
        return cur;
    }

    ListNode GetNodeAt(int index)
    {
        ListNode cur = headNode;
        for (int i = 0; i < index && cur != null; i++) cur = cur.nextNode;
        return cur;
    }

    // -------------------------------------------------------------------------
    //  START GAME
    // -------------------------------------------------------------------------
    void StartGame()
    {
        currentState = ListState.Ready;
        SetActive(explanationPanel, true);
        ShowModeButtons();

        string thing = NodeWord();
        string sName = ScenarioDisplayName();

        if (currentDifficulty == Difficulty.Beginner)
        {
            UpdateInstructions($"{sName} Ready! Tap ADD HEAD to add your first {thing}.");

            string infoText;
            if (currentScenario == Scenario.Solar)
                infoText = "BEGINNER MODE\n\nADD HEAD  - Add planet to front  O(1)\nADD TAIL  - Add planet to back   O(n)\nREMOVE   - Remove first planet  O(1)\nTRAVERSE - Visit all planets     O(n)\n\nEach node points to the next!";
            else if (currentScenario == Scenario.CityMetro)
                infoText = "BEGINNER MODE\n\nADD HEAD  - Add carriage to front O(1)\nADD TAIL  - Add carriage to back  O(n)\nREMOVE   - Remove front carriage O(1)\nTRAVERSE - Visit all carriages   O(n)\n\nEach carriage links to the next!";
            else
                infoText = "BEGINNER MODE\n\nADD HEAD  - Insert at front  O(1)\nADD TAIL  - Insert at back   O(n)\nREMOVE   - Remove from front O(1)\nTRAVERSE - Visit all cars    O(n)\n\nEach node points to the next!";

            SetOperationInfo(infoText);
        }
        else
        {
            UpdateInstructions($"{sName} Ready! Tap INSERT AT to add your first {thing}.");
            SetOperationInfo(
                "INTERMEDIATE MODE\n\n" +
                "INSERT AT  - Insert at position  O(n)\n" +
                "DELETE VAL - Delete by value     O(n)\n" +
                "REVERSE    - Flip entire list    O(n)\n" +
                "FIND MID   - Floyd's two-pointer O(n)\n\n" +
                "Think about pointer manipulation!");
        }

        if (tutorialIntegration != null)
            Invoke(nameof(ShowWelcomeTutorialDelayed), 1f);

        UpdateStatus();
    }

    void ShowWelcomeTutorialDelayed()
    {
        if (tutorialIntegration != null) tutorialIntegration.ShowWelcomeTutorial();
    }

    // -------------------------------------------------------------------------
    //  BEGINNER — ADD HEAD
    // -------------------------------------------------------------------------
    public void OnAddHeadButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnAddHeadButtonClicked();
        if (currentState != ListState.Ready) return;
        isAddingAtHead = true;
        SetActive(carInputPanel,       true);
        SetActive(mainButtonPanel,     false);
        SetActive(beginnerButtonPanel, false);

        UpdateInstructions($"Enter {NodeWord()} name for new HEAD");

        if (cargoInputField != null)
        {
            string placeholder;
            switch (currentScenario)
            {
                case Scenario.Solar:     placeholder = "e.g. Mars";   break;
                case Scenario.CityMetro: placeholder = "e.g. Line A"; break;
                default:                 placeholder = "e.g. Coal";   break;
            }
            cargoInputField.placeholder.GetComponent<TMP_Text>().text = placeholder;
        }

        SetOperationInfo("INSERT AT HEAD!\n\nAdding to the front\nTime: O(1) - Constant!\n\nJust update head pointer\nVery fast operation!");
    }

    // -------------------------------------------------------------------------
    //  BEGINNER — ADD TAIL
    // -------------------------------------------------------------------------
    public void OnAddTailButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnAddTailButtonClicked();
        if (currentState != ListState.Ready) return;
        isAddingAtHead = false;
        SetActive(carInputPanel,       true);
        SetActive(mainButtonPanel,     false);
        SetActive(beginnerButtonPanel, false);

        UpdateInstructions($"Enter {NodeWord()} name for new TAIL");
        SetOperationInfo("INSERT AT TAIL!\n\nAdding to the end\nTime: O(n) - Linear!\n\nMust traverse entire list\nto find the tail!");
    }

    // -------------------------------------------------------------------------
    //  BEGINNER — CONFIRM CARGO INPUT
    // -------------------------------------------------------------------------
    public void OnConfirmCargoInput()
    {
        string defaultCargo;
        int len = GetListLength();
        switch (currentScenario)
        {
            case Scenario.Solar:     defaultCargo = planetNames[len % planetNames.Length];              break;
            case Scenario.CityMetro: defaultCargo = metroCargoDefaults[len % metroCargoDefaults.Length]; break;
            default:                 defaultCargo = "Cargo"; break;
        }

        string cargo = (cargoInputField != null && !string.IsNullOrEmpty(cargoInputField.text))
            ? cargoInputField.text
            : defaultCargo;

        SetActive(carInputPanel,        false);
        SetActive(movementControlPanel, true);
        currentState = ListState.AddingNode;

        int colorIndex = len;
        movingNode = CreateNode(cargo, colorIndex);

        movingNode.transform.SetParent(listScene.transform);
        movingNode.transform.localScale    = originalNodeScale;
        movingNode.transform.localRotation = Quaternion.identity;

        float yOff = NodeYOffset;

        if (headNode == null)
        {
            targetPosition = new Vector3(0, yOff, 0);
            movingNode.transform.localPosition = new Vector3(0, yOff + 0.12f, 0);
        }
        else if (isAddingAtHead)
        {
            float spawnX = headNode.nodeObject.transform.localPosition.x - NodeStep - 0.15f;
            movingNode.transform.localPosition = new Vector3(spawnX, yOff + 0.05f, 0);
            targetPosition = new Vector3(
                headNode.nodeObject.transform.localPosition.x - NodeStep, yOff, 0);
        }
        else
        {
            ListNode tail = GetTailNode();
            float spawnX = tail.nodeObject.transform.localPosition.x + NodeStep + 0.15f;
            movingNode.transform.localPosition = new Vector3(spawnX, yOff + 0.05f, 0);
            targetPosition = new Vector3(
                tail.nodeObject.transform.localPosition.x + NodeStep, yOff, 0);
        }

        CreateTargetIndicator();
        UpdateInstructions($"Move {NodeWord()} to {(isAddingAtHead ? "HEAD" : "TAIL")} position");
        PlaySound(addCarSound);
    }

    // -------------------------------------------------------------------------
    //  BEGINNER — CONFIRM PLACEMENT
    // -------------------------------------------------------------------------
    public void OnConfirmPlacement()
    {
        if (movingNode == null) return;

        int len = GetListLength();
        string defaultCargo;
        switch (currentScenario)
        {
            case Scenario.Solar:     defaultCargo = "Planet"; break;
            case Scenario.CityMetro: defaultCargo = "Line";   break;
            default:                 defaultCargo = "Cargo";  break;
        }

        string cargo = (cargoInputField != null && !string.IsNullOrEmpty(cargoInputField.text))
            ? cargoInputField.text
            : defaultCargo;

        movingNode.transform.localPosition = targetPosition;
        movingNode.transform.localRotation = Quaternion.identity;

        ListNode newNode = new ListNode { nodeObject = movingNode, cargo = cargo };

        if (headNode == null || isAddingAtHead)
        {
            if (headNode != null)
            {
                newNode.nextNode  = headNode;
                newNode.connector = CreateConnector(
                    movingNode.transform.localPosition + new Vector3(NodeHalfExtent, 0, 0),
                    headNode.nodeObject.transform.localPosition - new Vector3(NodeHalfExtent, 0, 0));
                newNode.connector.transform.SetParent(listScene.transform);
                ClearHeadLabel(headNode);
            }
            headNode = newNode;
            newNode.headLabel = CreateTextLabel(movingNode.transform, HeadLabel,
                new Vector3(0, HeadLabelYOffset, 0),
                new Color(1, 0.5f, 0), 40, new Vector3(0.003f, 0.003f, 0.003f));
            NotifyGuideAndAssessmentAddHead();
            UpdateInstructionsSuccess($"Node added at HEAD!");
        }
        else
        {
            ListNode tail = GetTailNode();
            tail.nextNode     = newNode;
            newNode.connector = CreateConnector(
                tail.nodeObject.transform.localPosition  + new Vector3(NodeHalfExtent, 0, 0),
                movingNode.transform.localPosition       - new Vector3(NodeHalfExtent, 0, 0));
            newNode.connector.transform.SetParent(listScene.transform);
            NotifyGuideAndAssessmentAddTail();
            UpdateInstructionsSuccess($"Node added at TAIL!");
        }

        newNode.cargoLabel = CreateTextLabel(movingNode.transform,
            cargo, new Vector3(0, CargoLabelYOffset, 0),
            Color.white, 30, new Vector3(0.003f, 0.003f, 0.003f));

        CleanupOperation();
        UpdatePositionLabels();
        UpdateStatus();
        CheckAndUpdateButtonStates();
        if (cargoInputField != null) cargoInputField.text = "";
    }

    // -------------------------------------------------------------------------
    //  BEGINNER — REMOVE HEAD
    // -------------------------------------------------------------------------
    public void OnRemoveHeadButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnRemoveHeadButtonClicked();
        if (currentState != ListState.Ready || headNode == null)
        {
            UpdateInstructionsError($"{ScenarioDisplayName()} is EMPTY!");
            return;
        }

        currentState = ListState.RemovingNode;
        movingNode   = headNode.nodeObject;
        ListNode oldHead = headNode;
        headNode = headNode.nextNode;
        if (oldHead.connector != null) Destroy(oldHead.connector);

        if (headNode != null)
        {
            ClearHeadLabel(headNode);
            headNode.headLabel = CreateTextLabel(headNode.nodeObject.transform, HeadLabel,
                new Vector3(0, HeadLabelYOffset, 0),
                new Color(1, 0.5f, 0), 40, new Vector3(0.003f, 0.003f, 0.003f));
        }

        CheckAndUpdateButtonStates();

        targetPosition = new Vector3(
            movingNode.transform.localPosition.x,
            movingNode.transform.localPosition.y + 0.08f, 0);

        CreateExitIndicator();
        SetActive(mainButtonPanel,      false);
        SetActive(beginnerButtonPanel,  false);
        SetActive(movementControlPanel, true);

        UpdateInstructions($"Move removed {NodeWord()} away");
        PlaySound(removeCarSound);

        SetOperationInfo("REMOVE HEAD!\n\nRemoving from front\nTime: O(1) - Constant!\n\nJust update head pointer!");
    }

    public void OnConfirmRemoval()
    {
        if (movingNode == null) return;
        Destroy(movingNode);
        NotifyGuideAndAssessmentRemoveHead();
        CleanupOperation();
        UpdatePositionLabels();
        UpdateStatus();
        CheckAndUpdateButtonStates();
        UpdateInstructionsSuccess($"{NodeWord()} removed!");
    }

    public void OnConfirmButton()
    {
        if (currentState == ListState.AddingNode)        OnConfirmPlacement();
        else if (currentState == ListState.RemovingNode) OnConfirmRemoval();
    }

    // -------------------------------------------------------------------------
    //  BEGINNER — TRAVERSE
    // -------------------------------------------------------------------------
    public void OnTraverseButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnTraverseButtonClicked();
        if (currentState != ListState.Ready || headNode == null)
        {
            UpdateInstructionsError("List is EMPTY - nothing to traverse!");
            return;
        }
        StartCoroutine(TraverseList());
    }

    IEnumerator TraverseList()
    {
        SetOperationInfo("TRAVERSE!\n\nVisit each node in order\nTime: O(n) - Linear!\n\nMust follow NEXT pointers!");

        string thing = NodeWord();
        string sName = ScenarioDisplayName();

        ListNode cur = headNode;
        while (cur != null)
        {
            GameObject h = CreateHighlight(cur.nodeObject.transform);
            PlaySound(currentScenario == Scenario.Solar && orbitSound != null ? orbitSound : traverseSound);
            UpdateInstructions($"Visiting: {cur.cargo} [Position {cur.position}]");
            yield return new WaitForSeconds(1f);
            Destroy(h);
            cur = cur.nextNode;
        }
        UpdateInstructionsSuccess($"{sName} traversal complete!");
        NotifyGuideAndAssessmentTraverse();
    }

    // -------------------------------------------------------------------------
    //  INTERMEDIATE — INSERT AT POSITION
    // -------------------------------------------------------------------------
    public void OnInsertAtButton()
    {
        if (currentState != ListState.Ready) return;
        SetActive(mainButtonPanel,         false);
        SetActive(intermediateButtonPanel, false);
        SetActive(insertAtInputPanel,      true);

        UpdateInstructions($"Enter position and {NodeWord()} name");
        SetOperationInfo("INSERT AT POSITION\n\nMust traverse to that index\nTime: O(n) - Linear!\n\nprev.next = new\nnew.next = curr");
    }

    public void OnConfirmInsertAt()
    {
        if (insertPositionField == null || insertCargoField == null) return;
        if (!int.TryParse(insertPositionField.text, out int pos))
        {
            UpdateInstructionsError("Enter a valid number for position!");
            return;
        }

        int len = GetListLength();
        if (pos < 0 || pos > len)
        {
            UpdateInstructionsError($"Position must be 0-{len}");
            return;
        }

        string defaultCargo;
        switch (currentScenario)
        {
            case Scenario.Solar:     defaultCargo = planetNames[len % planetNames.Length];              break;
            case Scenario.CityMetro: defaultCargo = metroCargoDefaults[len % metroCargoDefaults.Length]; break;
            default:                 defaultCargo = "Cargo"; break;
        }

        string cargo = string.IsNullOrEmpty(insertCargoField.text) ? defaultCargo : insertCargoField.text;

        SetActive(insertAtInputPanel, false);
        StartCoroutine(AnimatedInsertAt(pos, cargo));
    }

    IEnumerator AnimatedInsertAt(int insertPos, string cargo)
    {
        currentState = ListState.InsertingAt;
        string thing = NodeWord();

        SetOperationInfo($"TRAVERSING to [{insertPos}]...\n\nThis is why it's O(n)!");

        ListNode cur = headNode;
        int idx = 0;
        while (cur != null && idx < insertPos)
        {
            GameObject h = CreateHighlight(cur.nodeObject.transform);
            UpdateInstructions($"Traversing: [{idx}] -> target [{insertPos}]");
            PlaySound(traverseSound);
            yield return new WaitForSeconds(0.7f);
            Destroy(h);
            cur = cur.nextNode;
            idx++;
        }

        float targetX;
        if (insertPos == 0 || headNode == null)
            targetX = headNode == null ? 0f : headNode.nodeObject.transform.localPosition.x - NodeStep;
        else if (cur == null)
            targetX = GetTailNode().nodeObject.transform.localPosition.x + NodeStep;
        else
        {
            ListNode prev = GetNodeAt(insertPos - 1);
            targetX = (prev.nodeObject.transform.localPosition.x
                     + cur.nodeObject.transform.localPosition.x) * 0.5f;
        }

        targetPosition = new Vector3(targetX, NodeYOffset, 0);

        int colorIndex = GetListLength();
        movingNode = CreateNode(cargo, colorIndex);

        movingNode.transform.SetParent(listScene.transform);
        movingNode.transform.localScale    = originalNodeScale;
        movingNode.transform.localRotation = Quaternion.identity;
        movingNode.transform.localPosition = new Vector3(targetX, 0.18f, 0);

        UpdateInstructions($"Placing {thing} at position [{insertPos}]...");
        SetOperationInfo($"PLACING AT [{insertPos}]\nRe-linking pointers!");

        PlaySound(addCarSound);
        yield return StartCoroutine(SmoothMove(movingNode, targetPosition, 0.35f));

        ListNode newNode = new ListNode { nodeObject = movingNode, cargo = cargo };

        if (insertPos == 0 || headNode == null)
        {
            newNode.nextNode  = headNode;
            if (headNode != null)
            {
                newNode.connector = CreateConnector(
                    movingNode.transform.localPosition + new Vector3(NodeHalfExtent, 0, 0),
                    headNode.nodeObject.transform.localPosition - new Vector3(NodeHalfExtent, 0, 0));
                newNode.connector.transform.SetParent(listScene.transform);
                ClearHeadLabel(headNode);
            }
            headNode = newNode;
            newNode.headLabel = CreateTextLabel(movingNode.transform, HeadLabel,
                new Vector3(0, HeadLabelYOffset, 0),
                new Color(1, 0.5f, 0), 40, new Vector3(0.003f, 0.003f, 0.003f));
        }
        else
        {
            ListNode prev = GetNodeAt(insertPos - 1);
            newNode.nextNode = prev.nextNode;
            if (prev.connector != null) { Destroy(prev.connector); prev.connector = null; }
            if (newNode.nextNode != null)
            {
                newNode.connector = CreateConnector(
                    movingNode.transform.localPosition + new Vector3(NodeHalfExtent, 0, 0),
                    newNode.nextNode.nodeObject.transform.localPosition - new Vector3(NodeHalfExtent, 0, 0));
                newNode.connector.transform.SetParent(listScene.transform);
            }
            prev.connector = CreateConnector(
                prev.nodeObject.transform.localPosition + new Vector3(NodeHalfExtent, 0, 0),
                movingNode.transform.localPosition      - new Vector3(NodeHalfExtent, 0, 0));
            prev.connector.transform.SetParent(listScene.transform);
            prev.nextNode = newNode;
        }

        newNode.cargoLabel = CreateTextLabel(movingNode.transform,
            cargo, new Vector3(0, CargoLabelYOffset, 0),
            Color.white, 30, new Vector3(0.003f, 0.003f, 0.003f));

        movingNode = null;

        yield return StartCoroutine(RepositionAllNodesCoroutine());
        UpdatePositionLabels();
        UpdateStatus();
        CheckAndUpdateButtonStates();

        UpdateInstructionsSuccess($"{thing} inserted at position [{insertPos}]!");
        SetOperationInfo($"INSERT AT [{insertPos}] Done!\n\nPointers re-linked!\n\nO(n) traversal + O(1) linking.");

        if (insertCargoField    != null) insertCargoField.text    = "";
        if (insertPositionField != null) insertPositionField.text = "";
        NotifyGuideAndAssessmentInsertAt();
        currentState = ListState.Ready;
        ShowModeButtons();
    }

    // -------------------------------------------------------------------------
    //  INTERMEDIATE — DELETE BY VALUE
    // -------------------------------------------------------------------------
    public void OnDeleteByValueButton()
    {
        if (currentState != ListState.Ready) return;
        if (GetListLength() == 0)
        {
            UpdateInstructionsError("List is EMPTY - nothing to delete!");
            return;
        }
        SetActive(mainButtonPanel,         false);
        SetActive(intermediateButtonPanel, false);
        SetActive(deleteValueInputPanel,   true);

        UpdateInstructions($"Enter {NodeWord()} name to remove");
        SetOperationInfo("DELETE BY VALUE\n\nSearch for node by value\nTime: O(n) - Linear!\n\nMust traverse list\nuntil value is found!");
    }

    public void OnConfirmDeleteByValue()
    {
        if (deleteValueField == null || string.IsNullOrEmpty(deleteValueField.text))
        {
            UpdateInstructionsError("Enter a value to search for!");
            return;
        }

        string target = deleteValueField.text.Trim();
        SetActive(deleteValueInputPanel, false);
        StartCoroutine(AnimatedDeleteByValue(target));
    }

    IEnumerator AnimatedDeleteByValue(string targetValue)
    {
        currentState = ListState.DeletingByValue;
        string thing = NodeWord();

        SetOperationInfo($"SEARCHING for '{targetValue}'...\nTime: O(n) - Linear!");

        ListNode prev = null, cur = headNode;

        while (cur != null)
        {
            GameObject h = CreateHighlight(cur.nodeObject.transform);
            UpdateInstructions($"Checking: [{cur.cargo}]  vs  [{targetValue}]");
            PlaySound(traverseSound);
            yield return new WaitForSeconds(0.7f);

            if (cur.cargo.Equals(targetValue, System.StringComparison.OrdinalIgnoreCase))
            {
                Destroy(h);
                yield return StartCoroutine(FlashHighlight(cur.nodeObject.transform, Color.red, 0.5f));

                if (prev == null)
                {
                    headNode = cur.nextNode;
                    if (headNode != null)
                    {
                        ClearHeadLabel(headNode);
                        headNode.headLabel = CreateTextLabel(headNode.nodeObject.transform, HeadLabel,
                            new Vector3(0, HeadLabelYOffset, 0),
                            new Color(1, 0.5f, 0), 40, new Vector3(0.003f, 0.003f, 0.003f));
                    }
                }
                else
                {
                    if (prev.connector != null) { Destroy(prev.connector); prev.connector = null; }
                    prev.nextNode = cur.nextNode;
                    if (cur.nextNode != null)
                    {
                        prev.connector = CreateConnector(
                            prev.nodeObject.transform.localPosition + new Vector3(NodeHalfExtent, 0, 0),
                            cur.nextNode.nodeObject.transform.localPosition - new Vector3(NodeHalfExtent, 0, 0));
                        prev.connector.transform.SetParent(listScene.transform);
                    }
                }

                if (cur.connector != null) Destroy(cur.connector);
                Destroy(cur.nodeObject);

                UpdateInstructionsSuccess($"Deleted {thing} '{targetValue}'!");
                SetOperationInfo($"DELETED '{targetValue}'\n\nPointer updated!\nO(n) - searched full list.");

                yield return StartCoroutine(RepositionAllNodesCoroutine());
                UpdatePositionLabels();
                UpdateStatus();
                CheckAndUpdateButtonStates();
                currentState = ListState.Ready;
                NotifyGuideAndAssessmentDeleteByValue();
                ShowModeButtons();
                if (deleteValueField != null) deleteValueField.text = "";
                yield break;
            }

            Destroy(h);
            prev = cur;
            cur  = cur.nextNode;
        }

        UpdateInstructionsError($"'{targetValue}' not found in list!");
        SetOperationInfo($"NOT FOUND\n\n'{targetValue}' not in list.\nSearched full list: O(n).");

        currentState = ListState.Ready;
        ShowModeButtons();
        if (deleteValueField != null) deleteValueField.text = "";
    }

    // -------------------------------------------------------------------------
    //  INTERMEDIATE — REVERSE
    // -------------------------------------------------------------------------
    public void OnReverseListButton()
    {
        if (currentState != ListState.Ready) return;
        if (headNode == null || GetListLength() < 2)
        {
            UpdateInstructionsError("Need at least 2 nodes to reverse!");
            return;
        }
        currentState = ListState.Reversing;
        StartCoroutine(AnimatedReverseList());
    }

    IEnumerator AnimatedReverseList()
    {
        SetOperationInfo("REVERSE LIST  O(n)\n\nprev=null, curr=head\nnext=curr.next\n\nIterate and flip!");

        UpdateInstructions("Reversing list...");
        yield return StartCoroutine(HighlightAllNodes(new Color(0.5f, 0.5f, 1f, 0.4f), 0.6f));

        ListNode prev = null, curr = headNode;
        while (curr != null)
        {
            GameObject h = CreateHighlight(curr.nodeObject.transform);
            UpdateInstructions($"Flipping pointer at '{curr.cargo}'...");
            yield return new WaitForSeconds(0.5f);
            Destroy(h);

            ListNode next = curr.nextNode;
            curr.nextNode = prev;
            prev = curr;
            curr = next;
        }
        headNode = prev;

        ListNode n = headNode;
        while (n != null) { if (n.connector != null) { Destroy(n.connector); n.connector = null; } n = n.nextNode; }

        yield return StartCoroutine(RepositionAllNodesCoroutine());

        n = headNode;
        while (n != null) { ClearHeadLabel(n); n = n.nextNode; }
        headNode.headLabel = CreateTextLabel(headNode.nodeObject.transform, HeadLabel,
            new Vector3(0, HeadLabelYOffset, 0),
            new Color(1, 0.5f, 0), 40, new Vector3(0.003f, 0.003f, 0.003f));

        UpdatePositionLabels();
        UpdateStatus();

        UpdateInstructionsSuccess($"{ScenarioDisplayName()} reversed!");
        SetOperationInfo("REVERSE Complete!\n\nAll pointers flipped!\nTime: O(n)  Space: O(1)\n\nThree-pointer technique!");
        NotifyGuideAndAssessmentReverse();
        currentState = ListState.Ready;
        ShowModeButtons();
    }

    // -------------------------------------------------------------------------
    //  INTERMEDIATE — FIND MIDDLE
    // -------------------------------------------------------------------------
    public void OnFindMiddleButton()
    {
        if (currentState != ListState.Ready) return;
        if (headNode == null)
        {
            UpdateInstructionsError("List is EMPTY - nothing to find!");
            return;
        }
        currentState = ListState.FindingMiddle;
        StartCoroutine(AnimatedFindMiddle());
    }

    IEnumerator AnimatedFindMiddle()
    {
        SetOperationInfo("FIND MIDDLE  O(n)\n\nSLOW: 1 step\nFAST: 2 steps\n\nWhen fast=end,\nslow=middle!");

        UpdateInstructions("Finding middle (Floyd's Two-Pointer)...");
        yield return new WaitForSeconds(1f);

        ListNode slow = headNode, fast = headNode;
        int step = 0;

        while (fast != null && fast.nextNode != null)
        {
            GameObject slowH = CreateColorHighlight(slow.nodeObject.transform, new Color(0, 1, 1, 0.4f));
            GameObject fastH = CreateColorHighlight(fast.nodeObject.transform, new Color(1, 1, 0, 0.4f));
            UpdateInstructions($"Step {step + 1}: SLOW=[{slow.cargo}]  FAST=[{fast.cargo}]");
            yield return new WaitForSeconds(0.8f);
            Destroy(slowH); Destroy(fastH);

            slow = slow.nextNode;
            fast = fast.nextNode?.nextNode;
            step++;
        }

        yield return StartCoroutine(FlashHighlight(slow.nodeObject.transform, Color.green, 1.5f));

        UpdateInstructionsSuccess($"Middle: '{slow.cargo}' [Position {slow.position}]!");
        SetOperationInfo($"MIDDLE FOUND!\n\n'{slow.cargo}' @ [{slow.position}]\n\nTime: O(n)  Space: O(1)\nFloyd's algorithm!");
        NotifyGuideAndAssessmentFindMiddle();
        currentState = ListState.Ready;
        ShowModeButtons();
    }

    // -------------------------------------------------------------------------
    //  REPOSITION ALL NODES
    // -------------------------------------------------------------------------
    IEnumerator RepositionAllNodesCoroutine()
    {
        ListNode cur = headNode;
        int idx = 0;
        while (cur != null)
        {
            Vector3 target = new Vector3(idx * NodeStep, NodeYOffset, 0);
            yield return StartCoroutine(SmoothMove(cur.nodeObject, target, 0.3f));

            if (cur.connector != null) { Destroy(cur.connector); cur.connector = null; }
            if (cur.nextNode != null)
            {
                cur.connector = CreateConnector(
                    cur.nodeObject.transform.localPosition  + new Vector3(NodeHalfExtent, 0, 0),
                    cur.nextNode.nodeObject.transform.localPosition - new Vector3(NodeHalfExtent, 0, 0));
                cur.connector.transform.SetParent(listScene.transform);
            }
            cur = cur.nextNode;
            idx++;
        }
    }

    IEnumerator SmoothMove(GameObject go, Vector3 targetLocal, float duration)
    {
        if (go == null) yield break;
        Vector3 start   = go.transform.localPosition;
        float   elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            go.transform.localPosition = Vector3.Lerp(start, targetLocal, elapsed / duration);
            yield return null;
        }
        go.transform.localPosition = targetLocal;
    }

    // -------------------------------------------------------------------------
    //  MOVEMENT
    // -------------------------------------------------------------------------
    void SetupMovementButtons()
    {
        AddHold(moveLeftButton,  Vector3.left);
        AddHold(moveRightButton, Vector3.right);
        if (cancelButton != null) cancelButton.onClick.AddListener(CancelOperation);
    }

    void AddHold(UnityEngine.UI.Button btn, Vector3 dir)
    {
        if (btn == null) return;
        var et = btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        AddEntry(et, UnityEngine.EventSystems.EventTriggerType.PointerDown, _ => StartMoving(dir));
        AddEntry(et, UnityEngine.EventSystems.EventTriggerType.PointerUp,   _ => StopMoving());
        AddEntry(et, UnityEngine.EventSystems.EventTriggerType.PointerExit, _ => StopMoving());
    }

    void AddEntry(UnityEngine.EventSystems.EventTrigger et,
        UnityEngine.EventSystems.EventTriggerType type,
        UnityEngine.Events.UnityAction<UnityEngine.EventSystems.BaseEventData> call)
    {
        var entry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(call);
        et.triggers.Add(entry);
    }

    void StartMoving(Vector3 dir) { currentMovementDirection = dir; isMoving = true; PlaySound(moveSound); }
    void StopMoving()             { isMoving = false; currentMovementDirection = Vector3.zero; }

    void MoveContinuous()
    {
        if (movingNode == null) return;
        if (!hasShownMovementTutorial && tutorialIntegration != null)
        {
            tutorialIntegration.OnMovementStarted();
            hasShownMovementTutorial = true;
        }

        Vector3 localDelta = currentMovementDirection.normalized
                             * ZoomAdjustedMoveSpeed()
                             * Time.deltaTime;
        movingNode.transform.localPosition += localDelta;
    }

    void CheckConfirmDistance()
    {
        if (movingNode == null) return;

        float dist = Vector3.Distance(
            new Vector3(movingNode.transform.localPosition.x, 0, movingNode.transform.localPosition.z),
            new Vector3(targetPosition.x, 0, targetPosition.z));

        bool inPos = dist < confirmDistanceThreshold;
        SetActive(confirmButton, inPos);
        if (inPos)
            UpdateInstructionsSuccess("Perfect! Tap CONFIRM to attach");
        else
            UpdateInstructions($"Move {NodeWord()} to target (dist: {dist:F2})");
    }

    // -------------------------------------------------------------------------
    //  INDICATORS
    // -------------------------------------------------------------------------
    void CreateTargetIndicator()
    {
        float w = currentScenario == Scenario.Solar ? planetRadius * 2.5f : carLength * 1.2f;
        float d = currentScenario == Scenario.Solar ? planetRadius * 2.5f : carWidth  * 1.2f;

        targetIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        targetIndicator.transform.SetParent(listScene.transform);
        targetIndicator.transform.localPosition = targetPosition;
        targetIndicator.transform.localRotation = Quaternion.identity;
        targetIndicator.transform.localScale    = new Vector3(w, 0.002f, d);
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0, 1, 0, 0.5f);
        targetIndicator.GetComponent<Renderer>().material = mat;
        Destroy(targetIndicator.GetComponent<Collider>());
        StartCoroutine(PulseIndicator());
    }

    void CreateExitIndicator()
    {
        float w = currentScenario == Scenario.Solar ? planetRadius * 2.5f : carLength * 1.2f;
        float d = currentScenario == Scenario.Solar ? planetRadius * 2.5f : carWidth  * 1.2f;

        targetIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        targetIndicator.transform.SetParent(listScene.transform);
        targetIndicator.transform.localPosition = targetPosition;
        targetIndicator.transform.localRotation = Quaternion.identity;
        targetIndicator.transform.localScale    = new Vector3(w, 0.002f, d);
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(1, 0, 0, 0.6f);
        targetIndicator.GetComponent<Renderer>().material = mat;
        Destroy(targetIndicator.GetComponent<Collider>());
        CreateTextLabel(targetIndicator.transform, "REMOVE",
            new Vector3(0, 0.05f, 0), Color.red, 40, new Vector3(0.003f, 0.003f, 0.003f));
        StartCoroutine(PulseIndicator());
    }

    IEnumerator PulseIndicator()
    {
        float baseW = targetIndicator.transform.localScale.x;
        while (targetIndicator != null)
        {
            float s = baseW + Mathf.Sin(Time.time * 3f) * 0.008f;
            targetIndicator.transform.localScale =
                new Vector3(s, 0.002f, targetIndicator.transform.localScale.z);
            yield return null;
        }
    }

    // -------------------------------------------------------------------------
    //  HIGHLIGHT HELPERS
    // -------------------------------------------------------------------------
    GameObject CreateHighlight(Transform parent)
        => CreateColorHighlight(parent, new Color(1, 1, 0, 0.3f));

    GameObject CreateColorHighlight(Transform parent, Color color)
    {
        GameObject h = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        h.transform.SetParent(parent);
        h.transform.localPosition = Vector3.zero;
        h.transform.localRotation = Quaternion.identity;

        float worldSize = currentScenario == Scenario.Solar
            ? planetRadius * 2.8f
            : carLength    * 1.3f;

        float px = Mathf.Max(parent.lossyScale.x, 0.0001f);
        h.transform.localScale = Vector3.one * (worldSize / px);

        Renderer rend = h.GetComponent<Renderer>();
        Material mat  = new Material(Shader.Find("Unlit/Color"));
        mat.color     = color;
        rend.material = mat;
        Destroy(h.GetComponent<Collider>());
        return h;
    }

    IEnumerator FlashHighlight(Transform parent, Color color, float duration)
    {
        GameObject h = CreateColorHighlight(parent, color);
        yield return new WaitForSeconds(duration);
        Destroy(h);
    }

    IEnumerator HighlightAllNodes(Color color, float duration)
    {
        List<GameObject> hs = new List<GameObject>();
        ListNode n = headNode;
        while (n != null) { hs.Add(CreateColorHighlight(n.nodeObject.transform, color)); n = n.nextNode; }
        yield return new WaitForSeconds(duration);
        foreach (var h in hs) Destroy(h);
    }

    // -------------------------------------------------------------------------
    //  CLEANUP / CANCEL
    // -------------------------------------------------------------------------
    public void CancelOperation()
    {
        if (movingNode != null) { Destroy(movingNode); movingNode = null; }
        CleanupOperation();
        UpdateInstructions("Operation cancelled");
    }

    void CleanupOperation()
    {
        if (targetIndicator != null) { Destroy(targetIndicator); targetIndicator = null; }
        movingNode   = null;
        currentState = ListState.Ready;
        StopMoving();
        hasShownMovementTutorial = false;
        SetActive(carInputPanel,         false);
        SetActive(insertAtInputPanel,    false);
        SetActive(deleteValueInputPanel, false);
        SetActive(movementControlPanel,  false);
        SetActive(confirmButton,         false);
        ShowModeButtons();
    }

    // -------------------------------------------------------------------------
    //  RESET
    // -------------------------------------------------------------------------
    public void OnResetButton()
    {
        if (swipeRotation       != null) swipeRotation.ResetRotation();
        if (tutorialIntegration != null) tutorialIntegration.OnResetButtonClicked();
        if (zoomController      != null) zoomController.ResetZoom();

        StopAllCoroutines();

        pulseCoroutine = null;
        ResetPrimaryButtonVisual();

        originalButtonColors.Clear();
        CacheAllButtonColors();

        if (listScene       != null) { Destroy(listScene);       listScene       = null; }
        if (movingNode      != null) { Destroy(movingNode);      movingNode      = null; }
        if (targetIndicator != null) { Destroy(targetIndicator); targetIndicator = null; }
        solarBackground = null;
        metroBackground = null;

        headNode                 = null;
        sceneSpawned             = false;
        nodeIdCounter            = 1;
        isAddingAtHead           = true;
        isMoving                 = false;
        currentMovementDirection = Vector3.zero;
        currentScenario          = Scenario.None;
        currentDifficulty        = Difficulty.None;
        _silenceInstructions     = false;

        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
                if (plane?.gameObject != null) plane.gameObject.SetActive(true);
        }
        if (raycastManager != null) raycastManager.enabled = false;

        HideAllPanels();
        if (statusText != null) statusText.text = "List: 0 nodes";

        ShowScenarioPanel();
    }

    // -------------------------------------------------------------------------
    //  SHARED HELPERS
    // -------------------------------------------------------------------------
    void SetMatColor(GameObject obj, Color color, float gloss = 0.1f)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return;
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Glossiness", gloss);
        r.material = mat;
    }

    GameObject CreateTextLabel(Transform parent, string text, Vector3 localPos,
        Color color, int fontSize, Vector3 scale)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale    = scale;

        TextMesh tm  = obj.AddComponent<TextMesh>();
        tm.text      = text;
        tm.fontSize  = fontSize;
        tm.color     = color;
        tm.anchor    = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;

        obj.AddComponent<Billboard>();
        return obj;
    }

    void PlaySound(AudioClip clip)
    { if (audioSource != null && clip != null) audioSource.PlayOneShot(clip); }

    void SetActive(GameObject obj, bool active)
    { if (obj != null) obj.SetActive(active); }

    // -------------------------------------------------------------------------
    //  PUBLIC API (used by guide & assessment)
    // -------------------------------------------------------------------------
    public void PreFillForLesson(int count)
    {
        if (listScene == null)
        {
            Debug.LogWarning("[InteractiveTrainList] PreFillForLesson: listScene is null.");
            return;
        }
        count = Mathf.Clamp(count, 1, 8);
        for (int i = 0; i < count; i++)
        {
            string defaultCargo;
            switch (currentScenario)
            {
                case Scenario.Solar:
                    defaultCargo = planetNames[i % planetNames.Length];
                    break;
                case Scenario.CityMetro:
                    defaultCargo = metroCargoDefaults[i % metroCargoDefaults.Length];
                    break;
                default:
                    defaultCargo = $"Car {i + 1}";
                    break;
            }
            AddNodeDirect(defaultCargo, atTail: true);
        }
        CheckAndUpdateButtonStates();
    }

    /// <summary>Returns the number of nodes currently in the list.</summary>
    public int GetPublicListLength() => GetListLength();

    /// <summary>Returns the GameObject of the node at the given index (0 = head).</summary>
    public GameObject GetNodeObjectAt(int index)
    {
        ListNode node = GetNodeAt(index);
        return node?.nodeObject;
    }

    /// <summary>Returns the cargo (data) string of the node at the given index.</summary>
    public string GetNodeCargoAt(int index)
    {
        ListNode node = GetNodeAt(index);
        return node?.cargo ?? "";
    }
}