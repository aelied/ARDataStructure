using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

/// <summary>
/// COMBINED ARRAY — Beginner + Intermediate, THREE SCENARIOS
///
/// SCENARIOS:
///   1. Parking Lot    — cars in ground-level parking spots
///   2. Vending Machine — 2×3 grid product slots
///   3. Supermarket Shelf — 2×3 grocery shelf grid  ← NEW
///
/// DYNAMIC BUTTON WIRING:
///   ScenarioManager.cs fetches the active scenarios from the admin server
///   and calls OnScenarioChosen(Scenario) at runtime, so the two buttons
///   in the AR app always reflect whatever the admin chose on scenarios.html.
///   If ScenarioManager is absent the built-in ShowScenarioPanel() fallback
///   shows all three buttons directly.
///
/// SEQUENCE:
///   1. App opens  → Scenario Panel shown (2 buttons, wired by ScenarioManager)
///   2. User picks Scenario
///   3. Difficulty Panel shown
///   4. User picks Difficulty (Beginner / Intermediate)
///   5. User taps a plane to place the EMPTY scene
///   6. Only INSERT (and RESET) are accessible at first.
///      Remove / Access unlock after first insert.
///
/// SUPERMARKET SCENARIO:
///   - 2-row × 3-column wooden grocery shelf (arrayCapacity = 6)
///   - Items are colour-coded grocery boxes sitting at the front of each shelf
///   - Beginner:     STOCK (insert), PULL (remove), CHECK (access)
///   - Intermediate: SORTED STOCK (sorted insert by price), AISLE SEARCH (binary),
///                   REMOVE PRICIEST (remove largest)
///   - Slot addresses: A0..A2 / B0..B2  (shared with Vending Machine grid logic)
///
/// ZOOM FIX:
///   Movement speed divided by zoomController.GetCurrentScale().
///
/// FEEDBACK COLORS:
///   Errors   → Red   instructionText
///   Success  → Green instructionText
///   Neutral  → White instructionText
/// </summary>
public class InteractiveArrayCars : MonoBehaviour
{
    // =========================================================================
    // AR COMPONENTS
    // =========================================================================
    [Header("AR Components")]
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;
    public Camera arCamera;

    [Header("Zoom Controller")]
    public SceneZoomController zoomController;

    [Header("Plane Visualization")]
    public GameObject planePrefab;

    [Header("Custom Assets (Optional)")]
    public GameObject[] carPrefabs;
    public GameObject parkingSpotPrefab;

    // =========================================================================
    // UI REFERENCES
    // =========================================================================
    [Header("UI References")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI detectionText;
    public TextMeshProUGUI operationInfoText;

    public GameObject mainButtonPanel;
    public GameObject indexInputPanel;
    public TMP_InputField indexInputField;
    public GameObject movementControlPanel;
    public GameObject confirmButton;
    public GameObject explanationPanel;

    public GameObject confirmDeleteButton;
    public GameObject confirmAccessButton;

    // =========================================================================
    // SCENARIO PANEL  (fallback — used when ScenarioManager is absent)
    // =========================================================================
    [Header("Scenario Selection (Fallback Panel)")]
    public GameObject scenarioPanel;
    public TextMeshProUGUI scenarioTitleText;
    public UnityEngine.UI.Button btnScenarioParking;
    public UnityEngine.UI.Button btnScenarioVending;
    public UnityEngine.UI.Button btnScenarioSupermarket;   // ← NEW

    // =========================================================================
    // MODE BUTTON PANELS
    // =========================================================================
    [Header("Mode Button Panels")]
    public GameObject beginnerButtonsPanel;
    public GameObject intermediateButtonsPanel;

    // =========================================================================
    // DIFFICULTY PANEL
    // =========================================================================
    [Header("Difficulty Selection")]
    public GameObject difficultyPanel;
    public UnityEngine.UI.Button btnBeginner;
    public UnityEngine.UI.Button btnIntermediate;
    public TextMeshProUGUI difficultyTitleText;

    // =========================================================================
    // INTERMEDIATE-ONLY UI
    // =========================================================================
    [Header("Intermediate UI")]
    public GameObject searchInputPanel;
    public TMP_InputField plateNumberInput;
    public TMP_InputField searchPlateInput;
    public TextMeshProUGUI sortedOrderDisplay;

    // =========================================================================
    // BEGINNER BUTTONS
    // =========================================================================
    [Header("Beginner Action Buttons")]
    public UnityEngine.UI.Button beginnerInsertButton;
    public UnityEngine.UI.Button beginnerRemoveButton;
    public UnityEngine.UI.Button beginnerAccessButton;

    // =========================================================================
    // INTERMEDIATE BUTTONS
    // =========================================================================
    [Header("Intermediate Action Buttons")]
    public UnityEngine.UI.Button intermediateSortedInsertButton;
    public UnityEngine.UI.Button intermediateBinarySearchButton;
    public UnityEngine.UI.Button intermediateRemoveLargestButton;

    // =========================================================================
    // MOVEMENT CONTROL BUTTONS
    // =========================================================================
    [Header("Movement Control Buttons")]
    public UnityEngine.UI.Button moveLeftButton;
    public UnityEngine.UI.Button moveRightButton;
    public UnityEngine.UI.Button moveForwardButton;
    public UnityEngine.UI.Button moveBackButton;
    public UnityEngine.UI.Button moveUpButton;
    public UnityEngine.UI.Button moveDownButton;
    public UnityEngine.UI.Button cancelButton;

    // =========================================================================
    // AUDIO
    // =========================================================================
    [Header("Audio")]
    public AudioClip placeSceneSound;
    public AudioClip carParkSound;
    public AudioClip carLeaveSound;
    public AudioClip accessSound;
    public AudioClip moveSound;
    public AudioClip shiftSound;
    public AudioClip searchSound;
    public AudioClip foundSound;
    public AudioClip bookPlaceSound;
    public AudioClip bookRemoveSound;

    [Header("Swipe Rotation")]
    public SwipeRotation swipeRotation;

    [Header("Tutorial System")]
    public ArrayTutorialIntegration tutorialIntegration;

    // =========================================================================
    // ARRAY SETTINGS
    // =========================================================================
    [Header("Array Settings")]
    public int arrayCapacity = 6;
    public float carSpacing = 0.22f;
    public float moveSpeed = 1.5f;
    public float confirmDistanceThreshold = 0.1f;
    public float sceneHeightOffset = 0.05f;
    public float shiftAnimDuration = 0.4f;

    [Header("Collision Settings")]
    public float collisionCheckDistance = 0.08f;
    public float boundaryMargin = 0.4f;

    // =========================================================================
    // ENUMS  — add new Scenario values here when expanding
    // =========================================================================
    public enum Scenario   { None, Parking, Vending, Supermarket }
    public enum Difficulty { None, Beginner, Intermediate }

    private enum ArrayState
    {
        ChoosingScenario,
        ChoosingDifficulty,
        WaitingForPlane,
        Ready,
        MovingCar,
        SelectingIndex,
        Searching,
        Shifting
    }

    // =========================================================================
    // SHARED GRID CONSTANTS  (Vending Machine & Supermarket both use 2×3 grid)
    // =========================================================================
    private const int   GRID_COLS = 3;
    private const int   GRID_ROWS = 2;

    // ── Vending Machine geometry ──────────────────────────────────────────────
    private const float VM_SLOT_W       = 0.090f;
    private const float VM_SLOT_H       = 0.105f;
    private const float VM_SLOT_GAP     = 0.010f;
    private const float VM_BODY_DEPTH   = 0.10f;
    private const float VM_PRODUCT_Z    = 0.040f;   // in FRONT of machine face
    private const float VM_LABEL_Z      = 0.055f;
    private const float VM_BODY_PANEL_T = 0.006f;

    // ── Supermarket geometry ──────────────────────────────────────────────────
    private const float SM_SLOT_W       = 0.095f;
    private const float SM_SLOT_H       = 0.100f;
    private const float SM_SLOT_GAP     = 0.012f;
    private const float SM_SHELF_DEPTH  = 0.080f;
    private const float SM_ITEM_Z       = 0.038f;   // front of shelf plank — always visible
    private const float SM_LABEL_Z      = 0.055f;

    // Derived vending machine dimensions (filled in BuildVendingMachineScene)
    private float vmTotalW, vmTotalH, vmStartX, vmStartY;

    // Derived supermarket dimensions (filled in BuildSupermarketScene)
    private float smTotalW, smTotalH, smStartX, smStartY;

    // =========================================================================
    // PRIVATE STATE
    // =========================================================================
    private AudioSource audioSource;

    private Scenario   currentScenario   = Scenario.None;
    private Difficulty currentDifficulty = Difficulty.None;
    private ArrayState currentState      = ArrayState.ChoosingScenario;

    private bool hasInsertedAtLeastOne = false;
    private Coroutine pulseCoroutine   = null;

    private Dictionary<UnityEngine.UI.Button, Color> originalButtonColors
        = new Dictionary<UnityEngine.UI.Button, Color>();

    // Shared colour palette
    private Color[] carColors = new Color[]
    {
        new Color(1f,  0.20f, 0.20f),
        new Color(0.2f,0.50f, 1.00f),
        new Color(0.3f,1.00f, 0.30f),
        new Color(1f,  0.80f, 0.20f),
        new Color(0.8f,0.20f, 1.00f),
        new Color(1f,  0.50f, 0.00f)
    };

    // Product names (Vending Machine)
    private string[] productNames = new string[] { "CHIPS", "COLA", "CANDY", "JUICE", "GUMMY", "CRUNCH" };

    // Grocery item names (Supermarket)
    private string[] groceryNames = new string[] { "BREAD", "MILK", "EGGS", "RICE", "PASTA", "SAUCE" };

    private bool _silenceInstructions = false;

/// <summary>
/// Called by ARArrayLessonGuide to stop InteractiveArrayCars from
/// overwriting the guide's own instruction/step text.
/// </summary>
public void SetInstructionSilence(bool silent)
{
    _silenceInstructions = silent;
    if (instructionText    != null) instructionText.gameObject.SetActive(!silent);
    if (operationInfoText  != null) operationInfoText.gameObject.SetActive(!silent);
    if (detectionText      != null) detectionText.gameObject.SetActive(!silent);
    if (statusText         != null) statusText.gameObject.SetActive(!silent);
}

    private class ParkingSpot
    {
        public GameObject spotObject;
        public GameObject carObject;
        public GameObject indexLabel;
        public GameObject plateLabel;
        public int   index;
        public bool  isOccupied;
        public int   plateNumber;   // reused as price in intermediate mode
    }

public GameObject ParkingLot => parkingLot;
    private ParkingSpot[] parkingArray;
    private GameObject    parkingLot;
    private bool          sceneSpawned = false;
    private int           carIdCounter = 1;

    private GameObject movingCar;
    private Vector3    targetPosition;
    private GameObject targetPositionIndicator;
    private bool       isRemovingCar  = false;
    private int        targetIndex    = -1;
    private bool       isInsertMode   = false;
    private bool       isAccessMode   = false;
    private Vector3    currentMovementDirection = Vector3.zero;
    private bool       isMoving       = false;
    private Vector3    originalCarScale = Vector3.one;
    private bool _scenarioAlreadyChosen = false;

    private int occupiedCount = 0;   // used by Intermediate mode

    // =========================================================================
    // GRID HELPERS  — shared by Vending Machine and Supermarket
    // =========================================================================
    /// <summary>Returns "A0", "A1", "B2" etc. for a flat index in any grid scenario.</summary>
    string GridAddress(int flatIndex)
    {
        int row = flatIndex / GRID_COLS;
        int col = flatIndex % GRID_COLS;
        return $"{(char)('A' + row)}{col}";
    }

    /// <summary>
    /// Returns the local-space position where a movable item sits in the
    /// current scenario's grid. Vending and Supermarket share the same
    /// column/row maths but use their own Z depth constants.
    /// </summary>
    Vector3 GridSlotLocalPos(int flatIndex)
    {
        int   col  = flatIndex % GRID_COLS;
        int   row  = flatIndex / GRID_COLS;

        if (currentScenario == Scenario.Vending)
        {
            float x = -(vmStartX + col * (VM_SLOT_W + VM_SLOT_GAP) + VM_SLOT_W * 0.5f);
            float y =  vmStartY  - row * (VM_SLOT_H + VM_SLOT_GAP);
            return new Vector3(x, y, VM_PRODUCT_Z);
        }
        else // Supermarket
        {
            float x = -(smStartX + col * (SM_SLOT_W + SM_SLOT_GAP) + SM_SLOT_W * 0.5f);
            float y =  smStartY  - row * (SM_SLOT_H + SM_SLOT_GAP);
            return new Vector3(x, y, SM_ITEM_Z);
        }
    }

    bool IsGridScenario => currentScenario == Scenario.Vending || currentScenario == Scenario.Supermarket;

    // =========================================================================
    // INSTRUCTION TEXT HELPERS
    // =========================================================================
    // Replace the three instruction helpers with these:
void UpdateInstructions(string msg)
{
    if (_silenceInstructions) return;
    SetInstructionColor(msg, Color.black);
}
void UpdateInstructionsSuccess(string msg)
{
    if (_silenceInstructions) return;
    SetInstructionColor(msg, new Color(0.2f, 1f, 0.3f));
}
void UpdateInstructionsError(string msg)
{
    if (_silenceInstructions) return;
    SetInstructionColor(msg, new Color(1f, 0.25f, 0.25f));
}

    void SetInstructionColor(string msg, Color col)
    {
        if (instructionText == null) return;
        instructionText.text  = msg;
        instructionText.color = col;
    }

    // =========================================================================
    // ZOOM HELPERS
    // =========================================================================
    float ZoomAdjustedMoveSpeed()
    {
        if (zoomController == null || !zoomController.IsInitialized()) return moveSpeed;
        float s = zoomController.GetCurrentScale();
        if (s < 0.01f) s = 0.01f;
        return moveSpeed * s;
    }

    float ZoomAdjustedConfirmThreshold() => confirmDistanceThreshold;

    // =========================================================================
    // SCENARIO VERB HELPERS  (keeps text thematic per scenario)
    // =========================================================================
    string InsertVerb()
    {
        switch (currentScenario)
        {
            case Scenario.Vending:     return "RESTOCK";
            case Scenario.Supermarket: return "STOCK";
            default:                   return "INSERT";
        }
    }
    string RemoveVerb()
    {
        switch (currentScenario)
        {
            case Scenario.Vending:     return "DISPENSE";
            case Scenario.Supermarket: return "PULL";
            default:                   return "REMOVE";
        }
    }
    string AccessVerb()
    {
        switch (currentScenario)
        {
            case Scenario.Vending:     return "INSPECT";
            case Scenario.Supermarket: return "CHECK";
            default:                   return "ACCESS";
        }
    }
    string ScenarioDisplayName()
    {
        switch (currentScenario)
        {
            case Scenario.Vending:     return "Vending Machine";
            case Scenario.Supermarket: return "Supermarket Shelf";
            default:                   return "Parking Lot";
        }
    }
private bool _started = false;

public void SkipToReady(Scenario scenario, Difficulty difficulty)
{
    _scenarioAlreadyChosen = true;
    currentScenario   = scenario;
    currentDifficulty = difficulty;
}
    // =========================================================================
    // START
    // =========================================================================
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

        SetupMovementButtons();
        HideAllPanels();
 if (_scenarioAlreadyChosen)
    {
        // Scene was placed before mode selection — skip straight to AR plane detection
        if (planeManager   != null) planeManager.enabled   = true;
        if (raycastManager != null) raycastManager.enabled = true;
        currentState = ArrayState.WaitingForPlane;
        UpdateInstructions("Point camera at a flat surface and tap to place the scene");
        return;   // ← skip ShowScenarioPanel()
    }
        if (detectionText != null)
        {
            detectionText.text  = "Choose your scenario first";
            detectionText.color = Color.black;
        }

        ShowScenarioPanel();
    }

    // =========================================================================
    // PANEL HELPERS
    // =========================================================================
    void HideAllPanels()
    {
        SetActive(mainButtonPanel,          false);
        SetActive(indexInputPanel,          false);
        SetActive(movementControlPanel,     false);
        SetActive(confirmButton,            false);
        SetActive(explanationPanel,         false);
        SetActive(difficultyPanel,          false);
        SetActive(scenarioPanel,            false);
        SetActive(searchInputPanel,         false);
        SetActive(beginnerButtonsPanel,     false);
        SetActive(intermediateButtonsPanel, false);
        if (sortedOrderDisplay != null) sortedOrderDisplay.gameObject.SetActive(false);
    }

    void ShowModeButtons(Difficulty diff)
    {
        SetActive(beginnerButtonsPanel,     diff == Difficulty.Beginner);
        SetActive(intermediateButtonsPanel, diff == Difficulty.Intermediate);

        if (diff == Difficulty.Beginner)
            RelabelBeginnerButtons();

        RefreshActionButtonStates();
    }

    void RelabelBeginnerButtons()
    {
        SetButtonLabel(beginnerInsertButton, InsertVerb());
        SetButtonLabel(beginnerRemoveButton, RemoveVerb());
        SetButtonLabel(beginnerAccessButton, AccessVerb());
    }

    void RelabelBeginnerButtonsDefault()
    {
        SetButtonLabel(beginnerInsertButton, "INSERT");
        SetButtonLabel(beginnerRemoveButton, "REMOVE");
        SetButtonLabel(beginnerAccessButton, "ACCESS");
    }

    void SetButtonLabel(UnityEngine.UI.Button btn, string label)
    {
        if (btn == null) return;
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = label;
        var legacy = btn.GetComponentInChildren<UnityEngine.UI.Text>();
        if (legacy != null) legacy.text = label;
    }

    // =========================================================================
    // BUTTON UNLOCK / LOCK
    // =========================================================================
    void CheckAndUpdateButtonStates()
    {
        int  count    = (currentDifficulty == Difficulty.Intermediate) ? occupiedCount : CountOccupied();
        bool hasItems = count > 0;
        hasInsertedAtLeastOne = hasItems;

        SetButtonInteractable(beginnerRemoveButton,             hasItems);
        SetButtonInteractable(beginnerAccessButton,             hasItems);
        SetButtonInteractable(intermediateBinarySearchButton,   hasItems);
        SetButtonInteractable(intermediateRemoveLargestButton,  hasItems);

        if (!hasItems)
        {
            if (pulseCoroutine == null && sceneSpawned)
                pulseCoroutine = StartCoroutine(PulseInsertButton());
        }
        else
        {
            if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
            ResetInsertButtonScale();
        }
    }

    void LockSecondaryButtons()   { hasInsertedAtLeastOne = false; CheckAndUpdateButtonStates(); }
    void UnlockSecondaryButtons()                                  { CheckAndUpdateButtonStates(); }
    void RefreshActionButtonStates()                               { CheckAndUpdateButtonStates(); }

    void SetButtonInteractable(UnityEngine.UI.Button btn, bool state)
    {
        if (btn == null) return;
        var img = btn.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            if (!originalButtonColors.ContainsKey(btn))
                originalButtonColors[btn] = img.color;
            img.color = state ? originalButtonColors[btn] : new Color(0.55f, 0.55f, 0.55f, 0.7f);
        }
        btn.interactable = state;
    }

    // =========================================================================
    // INSERT BUTTON PULSE
    // =========================================================================
    static readonly Color BASE_BTN_COLOR  = new Color(0x84 / 255f, 0x69 / 255f, 0xFF / 255f, 1f);
    static readonly Color LIGHT_BTN_COLOR = new Color(0xB2 / 255f, 0xA0 / 255f, 0xFF / 255f, 1f);

    IEnumerator PulseInsertButton()
    {
        float speed = 2.5f, minScale = 0.92f, maxScale = 1.10f, elapsed = 0f;
        while (true)
        {
            elapsed += Time.deltaTime * speed;
            float t     = (Mathf.Sin(elapsed) + 1f) * 0.5f;
            float scale = Mathf.Lerp(minScale, maxScale, t);
            Color col   = Color.Lerp(BASE_BTN_COLOR, LIGHT_BTN_COLOR, t);
            ApplyPulse(beginnerInsertButton,           scale, col);
            ApplyPulse(intermediateSortedInsertButton, scale, col);
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

    void ResetInsertButtonScale()
    {
        ResetPulseBtn(beginnerInsertButton);
        ResetPulseBtn(intermediateSortedInsertButton);
    }

    void ResetPulseBtn(UnityEngine.UI.Button btn)
    {
        if (btn == null) return;
        btn.transform.localScale = Vector3.one;
        var img = btn.GetComponent<UnityEngine.UI.Image>();
        if (img != null) img.color = BASE_BTN_COLOR;
    }

    // =========================================================================
    // UPDATE
    // =========================================================================
    void Update()
    {
        if (currentState == ArrayState.WaitingForPlane)
            DetectPlaneInteraction();
        else if (currentState == ArrayState.MovingCar)
        {
            if (isMoving && movingCar != null) MoveCarContinuous();
            CheckConfirmDistance();
        }
    }

    // =========================================================================
    // SCENARIO PANEL  (fallback shown when ScenarioManager is not present)
    // =========================================================================
    void ShowScenarioPanel()
    {
        currentState = ArrayState.ChoosingScenario;
        HideAllPanels();
        SetActive(scenarioPanel, true);

        if (scenarioTitleText != null)
            scenarioTitleText.text = "ARRAY\nChoose Scenario";

        UpdateInstructions("Choose how you want to explore arrays");

        if (btnScenarioParking != null)
        {
            btnScenarioParking.onClick.RemoveAllListeners();
            btnScenarioParking.onClick.AddListener(() => OnScenarioChosen(Scenario.Parking));
        }
        if (btnScenarioVending != null)
        {
            btnScenarioVending.onClick.RemoveAllListeners();
            btnScenarioVending.onClick.AddListener(() => OnScenarioChosen(Scenario.Vending));
        }
        if (btnScenarioSupermarket != null)
        {
            btnScenarioSupermarket.onClick.RemoveAllListeners();
            btnScenarioSupermarket.onClick.AddListener(() => OnScenarioChosen(Scenario.Supermarket));
        }
    }

    // =========================================================================
    // PUBLIC ENTRY POINT — called by ScenarioManager OR fallback buttons
    // =========================================================================
    /// <summary>
    /// Primary entry point for scenario selection.
    /// ScenarioManager calls this after fetching the active config from the server.
    /// Inspector-wired buttons call the helper methods below.
    /// </summary>
    public void OnScenarioChosen(Scenario chosen)
    {
        currentScenario = chosen;
        SetActive(scenarioPanel, false);
        ShowDifficultyPanel();
    }

    // Inspector / UnityEvent wiring helpers
    public void OnScenarioParking()     => OnScenarioChosen(Scenario.Parking);
    public void OnScenarioVending()     => OnScenarioChosen(Scenario.Vending);
    public void OnScenarioSupermarket() => OnScenarioChosen(Scenario.Supermarket);

    // =========================================================================
    // DIFFICULTY PANEL
    // =========================================================================
    void ShowDifficultyPanel()
    {
        currentState = ArrayState.ChoosingDifficulty;
        HideAllPanels();
        SetActive(difficultyPanel, true);

        if (difficultyTitleText != null)
            difficultyTitleText.text = $"ARRAY — {ScenarioDisplayName()}\nChoose Difficulty";

        UpdateInstructions("Choose your difficulty level");

        if (btnBeginner != null)
        {
            btnBeginner.onClick.RemoveAllListeners();
            btnBeginner.onClick.AddListener(() => OnDifficultyChosen(Difficulty.Beginner));
        }
        if (btnIntermediate != null)
        {
            btnIntermediate.onClick.RemoveAllListeners();
            btnIntermediate.onClick.AddListener(() => OnDifficultyChosen(Difficulty.Intermediate));
        }
    }

   void OnDifficultyChosen(Difficulty chosen)
{
    currentDifficulty = chosen;
    
    // Save so ModeSelectionManager can restore after mode choice
    PlayerPrefs.SetInt("AR_SCENARIO",   (int)currentScenario);
    PlayerPrefs.SetInt("AR_DIFFICULTY", (int)currentDifficulty);
    PlayerPrefs.Save();

    SetActive(difficultyPanel, false);
    if (planeManager   != null) planeManager.enabled   = true;
    if (raycastManager != null) raycastManager.enabled = true;
    currentState = ArrayState.WaitingForPlane;
    UpdateInstructions("Point camera at a flat surface and tap to place the scene");
    if (detectionText != null)
    {
        detectionText.text  = "Looking for surfaces…";
        detectionText.color = Color.yellow;
    }
}
    // =========================================================================
    // PLANE DETECTION
    // =========================================================================
    void DetectPlaneInteraction()
    {
        if (sceneSpawned) return;

        bool    inputReceived  = false;
        Vector2 screenPosition = Vector2.zero;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Touch touch = Input.GetTouch(0);
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;
            screenPosition = touch.position;
            inputReceived  = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;
            screenPosition = Input.mousePosition;
            inputReceived  = true;
        }

        if (!inputReceived || raycastManager == null) return;

        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            SpawnScene(hits[0].pose.position, hits[0].pose.rotation);
    }

    // =========================================================================
    // SPAWN — build scene, lock secondary buttons
    // =========================================================================
    public void SpawnScene(Vector3 position, Quaternion rotation)
    {
        sceneSpawned = true;
        PlaySound(placeSceneSound);

        if (planeManager != null)
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);

        string rootName;
        switch (currentScenario)
        {
            case Scenario.Vending:     rootName = "VendingMachine";    break;
            case Scenario.Supermarket: rootName = "SupermarketShelf";  break;
            default:                   rootName = "ParkingLot";        break;
        }

        parkingLot = new GameObject(rootName);
        parkingLot.transform.position = position + Vector3.up * sceneHeightOffset;
        parkingLot.transform.rotation = rotation;

        // Grid scenes face the camera
        if (IsGridScenario && arCamera != null)
        {
            Vector3 toCamera = arCamera.transform.position - parkingLot.transform.position;
            toCamera.y = 0f;
            if (toCamera.sqrMagnitude > 0.001f)
                parkingLot.transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }

        if (swipeRotation != null) swipeRotation.InitializeRotation(parkingLot.transform);
        if (zoomController != null) zoomController.InitializeZoom(parkingLot.transform);

        if (detectionText != null) { detectionText.text = "Scene Placed!"; detectionText.color = Color.green; }

        switch (currentScenario)
        {
            case Scenario.Vending:     BuildVendingMachineScene();  break;
            case Scenario.Supermarket: BuildSupermarketScene();     break;
            default:                   CreateParkingArray();        break;
        }

        LockSecondaryButtons();

        if (currentDifficulty == Difficulty.Beginner)
            StartBeginnerMode();
        else
            StartIntermediateMode();
    }

    // =========================================================================
    // BEGINNER MODE
    // =========================================================================
    void StartBeginnerMode()
    {
        currentState = ArrayState.Ready;
        SetActive(mainButtonPanel,  true);
        SetActive(explanationPanel, true);
        ShowModeButtons(Difficulty.Beginner);

        string iv = InsertVerb(), rv = RemoveVerb(), av = AccessVerb();

        if (currentScenario == Scenario.Parking)
        {
            UpdateInstructions(" Parking Lot Ready! Tap INSERT to park your first car.");
            if (operationInfoText != null)
                operationInfoText.text =
                    "ARRAY DATA STRUCTURE\n\n" +
                    "INSERT: Park car at specific spot [index]\n" +
                    "REMOVE: Remove car from spot [index]\n" +
                    "ACCESS: View car at spot [index]\n\n" +
                    "Fixed size · Direct access O(1)!\nIndex starts at 0! ";
        }
        else if (currentScenario == Scenario.Vending)
        {
            UpdateInstructions(" Vending Machine Ready! Tap RESTOCK to load your first product.");
            if (operationInfoText != null)
                operationInfoText.text =
                    "ARRAY DATA STRUCTURE\n\n" +
                    $"{iv}: Load product into slot [index]\n" +
                    $"{rv}: Remove product at slot [index]\n" +
                    $"{av}: Check what's in slot [index]\n\n" +
                    "Fixed size · Direct access O(1)!\nGrid address = flat array index ";
        }
        else // Supermarket
        {
            UpdateInstructions(" Supermarket Shelf Ready! Tap STOCK to place your first item.");
            if (operationInfoText != null)
                operationInfoText.text =
                    "ARRAY DATA STRUCTURE\n\n" +
                    $"{iv}: Place grocery in slot [index]\n" +
                    $"{rv}: Remove grocery from slot [index]\n" +
                    $"{av}: Check what's in slot [index]\n\n" +
                    "Fixed size · Direct access O(1)!\nShelf address = flat array index ";
        }

        if (tutorialIntegration != null)
            Invoke(nameof(ShowWelcomeTutorialDelayed), 1f);

        UpdateStatus_Beginner();
    }

    void ShowWelcomeTutorialDelayed()
    {
        if (tutorialIntegration != null) tutorialIntegration.ShowWelcomeTutorial();
    }

    // =========================================================================
    // INSERT  (Beginner)
    // =========================================================================
    public void OnInsertButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnInsertButtonClicked();
        if (currentState != ArrayState.Ready) return;

        currentState = ArrayState.SelectingIndex;
        SetActive(mainButtonPanel, false);
        SetActive(indexInputPanel, true);

        string iv = InsertVerb();

        if (IsGridScenario)
            UpdateInstructions($"Enter slot index (0 to {arrayCapacity - 1})  [A0–A2 = 0–2 | B0–B2 = 3–5]");
        else
            UpdateInstructions($"Enter parking spot index (0 to {arrayCapacity - 1})");

        if (operationInfoText != null)
            operationInfoText.text =
                $"{iv} Operation\n\n" +
                "Choose which slot [index]\nto place the new item.\n\n" +
                "Direct access: array[index] = value\n" +
                "Time Complexity: O(1)";
    }

    public void OnConfirmIndex()
    {
        if (indexInputField == null) return;
        if (int.TryParse(indexInputField.text, out int index))
        {
            if (index >= 0 && index < arrayCapacity)
            {
                if (parkingArray[index].isOccupied)
                {
                    string slotStr = IsGridScenario ? $"Slot {GridAddress(index)}" : "Spot";
                    UpdateInstructionsError($" {slotStr} already occupied! Choose another.");
                    return;
                }
                targetIndex  = index;
                isInsertMode = true;
                isAccessMode = false;
                StartMovingCar();
            }
            else UpdateInstructionsError($" Index out of bounds! Use 0–{arrayCapacity - 1}");
        }
        else UpdateInstructionsError(" Invalid index! Enter a number.");
    }

    void StartMovingCar()
    {
        isRemovingCar = false;
        currentState  = ArrayState.MovingCar;
        if (tutorialIntegration != null) tutorialIntegration.OnMovementStarted();
        SetActive(indexInputPanel, false);

        int colorIndex = carIdCounter - 1;
        if (IsGridScenario)
        {
            movingCar = currentScenario == Scenario.Supermarket
                ? CreateGroceryItem(colorIndex)
                : CreateProduct(colorIndex);
        }
        else
        {
            movingCar = CreateCar_Beginner(colorIndex);
        }

        movingCar.transform.SetParent(parkingLot.transform);
        movingCar.transform.localScale = originalCarScale;

        if (IsGridScenario)
        {
            targetPosition = GridSlotLocalPos(targetIndex);
            Vector3 spawnPos = targetPosition;
            spawnPos.z = (currentScenario == Scenario.Supermarket ? SM_ITEM_Z : VM_PRODUCT_Z) + 0.22f;
            movingCar.transform.localPosition = spawnPos;
        }
        else
        {
            Vector3 tp = parkingArray[targetIndex].spotObject.transform.localPosition;
            tp.y = 0.03f;
            targetPosition = tp;
            Vector3 spawnPos = targetPosition;
            spawnPos.z -= 0.32f;
            movingCar.transform.localPosition = spawnPos;
        }

        movingCar.transform.localRotation = Quaternion.identity;
        CreateTargetIndicator();
        SetupConfirmButton(OnConfirmPlacement);
        SetActive(movementControlPanel, true);

        string addr  = IsGridScenario ? $"slot {GridAddress(targetIndex)} (index [{targetIndex}])" : $"spot [{targetIndex}]";
        string iv    = InsertVerb();
        UpdateInstructions($"Move item into {addr}");
        if (operationInfoText != null)
            operationInfoText.text =
                $" {iv} at {addr}\n\n" +
                "Use arrow buttons to position the item\n" +
                "into the highlighted slot.\n\n" +
                "Direct index access = Instant!\nNo scanning needed.";

        PlaySound(carParkSound);
    }

    public void OnConfirmPlacement()
    {
        if (movingCar == null) return;
        movingCar.transform.localPosition = targetPosition;
        parkingArray[targetIndex].carObject  = movingCar;
        parkingArray[targetIndex].isOccupied = true;
        carIdCounter++;

        string addr = IsGridScenario ? $"{GridAddress(targetIndex)} [index {targetIndex}]" : $"[{targetIndex}]";
        UpdateInstructionsSuccess($"Item placed at slot {addr}!");

        if (operationInfoText != null)
            operationInfoText.text =
                $"{InsertVerb()} Complete!\n\n" +
                $"Item at slot {addr}\n" +
                $"Occupied: {CountOccupied()}/{arrayCapacity}\n\n" +
                "Time Complexity: O(1)\nDirect access — instant!\n\nArrays excel at random access!";

        UnlockSecondaryButtons();
        CleanupMovement();
        UpdateStatus_Beginner();
    }

    // =========================================================================
    // REMOVE  (Beginner)
    // =========================================================================
    public void OnRemoveButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnRemoveButtonClicked();
        if (currentState != ArrayState.Ready) return;

        currentState = ArrayState.SelectingIndex;
        isInsertMode = false;
        SetActive(mainButtonPanel, false);
        SetActive(indexInputPanel, true);

        UpdateInstructions(IsGridScenario
            ? $"Enter slot index to {RemoveVerb().ToLower()} item (0 to {arrayCapacity - 1})"
            : $"Enter parking spot index to remove car (0 to {arrayCapacity - 1})");

        if (operationInfoText != null)
            operationInfoText.text =
                $"{RemoveVerb()} Operation\n\n" +
                "Choose which slot [index]\nto remove the item from.\n\n" +
                "Direct access to any index!\n\nTime Complexity: O(1)";
    }

    public void OnConfirmRemoveIndex()
    {
        if (indexInputField == null) return;
        if (int.TryParse(indexInputField.text, out int index))
        {
            if (index >= 0 && index < arrayCapacity)
            {
                if (!parkingArray[index].isOccupied)
                {
                    string slotStr = IsGridScenario ? $"Slot {GridAddress(index)}" : "Spot";
                    UpdateInstructionsError($" {slotStr} is empty! Nothing to remove.");
                    return;
                }
                targetIndex = index;
                StartRemovingCar();
            }
            else UpdateInstructionsError($" Index out of bounds! Use 0–{arrayCapacity - 1}");
        }
        else UpdateInstructionsError(" Invalid index!");
    }

    void StartRemovingCar()
    {
        isRemovingCar = true;
        currentState  = ArrayState.MovingCar;
        SetActive(indexInputPanel,          false);
        SetActive(mainButtonPanel,          false);
        SetActive(beginnerButtonsPanel,     false);
        SetActive(intermediateButtonsPanel, false);

        movingCar = parkingArray[targetIndex].carObject;
        parkingArray[targetIndex].carObject  = null;
        parkingArray[targetIndex].isOccupied = false;

        targetPosition = movingCar.transform.localPosition;
        if (IsGridScenario)
            targetPosition.z = (currentScenario == Scenario.Supermarket ? SM_ITEM_Z : VM_PRODUCT_Z) + 0.22f;
        else
            targetPosition.z -= 0.38f;

        CreateExitIndicator();
        SetupConfirmButton(OnConfirmRemoval);
        SetActive(movementControlPanel, true);

        string addr = IsGridScenario ? GridAddress(targetIndex) : $"[{targetIndex}]";
        string rv   = RemoveVerb();
        UpdateInstructions($"{rv} item from slot {addr}!");
        if (operationInfoText != null)
            operationInfoText.text =
                $"{rv} from slot {addr} [index {targetIndex}]\n\n" +
                "Move item forward out of the slot!\n\n" +
                "Direct removal at any index!\nSlot becomes empty.";

        PlaySound(carLeaveSound);
    }

    public void OnConfirmRemoval()
    {
        if (movingCar == null) return;
        Destroy(movingCar);
        ClearPlateLabel(targetIndex);

        string addr = IsGridScenario ? GridAddress(targetIndex) : $"[{targetIndex}]";
        UpdateInstructionsSuccess($" Item removed from slot {addr}!");

        if (operationInfoText != null)
            operationInfoText.text =
                $"{RemoveVerb()} Complete!\n\n" +
                $"Removed from slot {addr}\n" +
                $"Occupied: {CountOccupied()}/{arrayCapacity}\n\n" +
                "Time Complexity: O(1)\nSlot is now empty and available.";

        CleanupMovement();
        UpdateStatus_Beginner();
        CheckAndUpdateButtonStates();
    }

    // =========================================================================
    // ACCESS  (Beginner)
    // =========================================================================
    public void OnAccessButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnAccessButtonClicked();
        if (currentState != ArrayState.Ready) return;

        currentState = ArrayState.SelectingIndex;
        isAccessMode = true;
        SetActive(mainButtonPanel, false);
        SetActive(indexInputPanel, true);

        UpdateInstructions(IsGridScenario
            ? $"Enter slot index to {AccessVerb().ToLower()} (0 to {arrayCapacity - 1})"
            : $"Enter spot index to view car (0 to {arrayCapacity - 1})");

        if (operationInfoText != null)
            operationInfoText.text =
                $"{AccessVerb()} Operation\n\n" +
                "Peek at any slot without removing.\n\nJust like: value = array[index]\n" +
                "Time Complexity: O(1)";
    }

    public void OnConfirmAccessIndex()
    {
        if (indexInputField == null) return;
        if (int.TryParse(indexInputField.text, out int index))
        {
            if (index >= 0 && index < arrayCapacity) { targetIndex = index; PerformAccess(); }
            else UpdateInstructionsError($" Index out of bounds! Use 0–{arrayCapacity - 1}");
        }
        else UpdateInstructionsError(" Invalid index!");
    }

    void PerformAccess()
  {
         SetActive(indexInputPanel, false);
         PlaySound(accessSound);

         // Notify the lesson guide so the assessment can track this
         ARArrayLessonGuide guide = FindObjectOfType<ARArrayLessonGuide>();
         if (guide != null) guide.NotifyAccessPerformed();

         StartCoroutine(HighlightAccessedSpot());
     }

    IEnumerator HighlightAccessedSpot()
    {
        bool occupied = parkingArray[targetIndex].isOccupied;

        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        highlight.transform.SetParent(parkingArray[targetIndex].spotObject.transform);
        highlight.transform.localPosition = new Vector3(0, IsGridScenario ? 0f : 0.08f, 0f);
        highlight.transform.localRotation = Quaternion.identity;
        highlight.transform.localScale    = Vector3.one * (IsGridScenario ? 0.6f : 1.5f);

        Renderer rend = highlight.GetComponent<Renderer>();
        Material mat  = new Material(Shader.Find("Unlit/Color"));
        mat.color     = new Color(0, 1, 1, 0.35f);
        rend.material = mat;
        Destroy(highlight.GetComponent<Collider>());

        string addr = IsGridScenario ? $"{GridAddress(targetIndex)} [index {targetIndex}]" : $"[{targetIndex}]";

        if (occupied)
            UpdateInstructionsSuccess($" Slot {addr}: ITEM PRESENT");
        else
            UpdateInstructions($"ℹ Slot {addr}: EMPTY");

        if (operationInfoText != null)
            operationInfoText.text =
                $"{AccessVerb()} Complete!\n\nChecked index [{targetIndex}]\n" +
                $"Address: {(IsGridScenario ? GridAddress(targetIndex) : $"[{targetIndex}]")}\n" +
                $"Status: {(occupied ? "Item present" : "Empty slot")}\n\n" +
                "Time Complexity: O(1)\nInstant access to any index!\n\nThis is the power of arrays!";

        yield return new WaitForSeconds(2.5f);
        Destroy(highlight);
        currentState = ArrayState.Ready;
        SetActive(mainButtonPanel, true);
        ShowModeButtons(currentDifficulty);
        UpdateInstructions("What would you like to do next?");
    }

    // =========================================================================
    // MOVEMENT — all in LOCAL space, zoom-aware speed
    // =========================================================================
    void SetupMovementButtons()
    {
        SetupButton(moveLeftButton,    Vector3.left);
        SetupButton(moveRightButton,   Vector3.right);
        SetupButton(moveForwardButton, Vector3.forward);
        SetupButton(moveBackButton,    Vector3.back);
        SetupButton(moveUpButton,      Vector3.up);
        SetupButton(moveDownButton,    Vector3.down);
        if (cancelButton != null) cancelButton.onClick.AddListener(CancelMovement);
    }

    void SetupButton(UnityEngine.UI.Button button, Vector3 direction)
    {
        if (button == null) return;
        var trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        var down = new UnityEngine.EventSystems.EventTrigger.Entry();
        down.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
        down.callback.AddListener((d) => StartMoving(direction));
        trigger.triggers.Add(down);

        var up = new UnityEngine.EventSystems.EventTrigger.Entry();
        up.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
        up.callback.AddListener((d) => StopMoving());
        trigger.triggers.Add(up);

        var exit = new UnityEngine.EventSystems.EventTrigger.Entry();
        exit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        exit.callback.AddListener((d) => StopMoving());
        trigger.triggers.Add(exit);
    }

    void StartMoving(Vector3 direction) { currentMovementDirection = direction; isMoving = true; PlaySound(moveSound); }
    void StopMoving()                   { isMoving = false; currentMovementDirection = Vector3.zero; }

    void MoveCarContinuous()
    {
        if (movingCar == null) return;
        Vector3 localDelta    = currentMovementDirection.normalized * ZoomAdjustedMoveSpeed() * Time.deltaTime;
        Vector3 intendedLocal = movingCar.transform.localPosition + localDelta;
        if (CanMoveToLocal(intendedLocal))
            movingCar.transform.localPosition = intendedLocal;
        else
            StopMoving();
    }

    bool CanMoveToLocal(Vector3 localPos)
    {
        if (movingCar == null) return false;

        if (currentScenario == Scenario.Vending)
        {
            float halfW = vmTotalW * 0.5f + 0.06f;
            float zMin  = -VM_BODY_DEPTH * 0.5f - 0.02f;
            float zMax  =  VM_PRODUCT_Z + 0.25f;
            float yBase =  vmStartY - vmTotalH - 0.06f;
            float yTop  =  vmStartY + VM_SLOT_H * 0.5f + 0.06f;

            if (Mathf.Abs(localPos.x) > halfW || localPos.z < zMin || localPos.z > zMax ||
                localPos.y < yBase || localPos.y > yTop) return false;
        }
        else if (currentScenario == Scenario.Supermarket)
        {
            float halfW = smTotalW * 0.5f + 0.06f;
            float zMin  = -SM_SHELF_DEPTH * 0.5f - 0.02f;
            float zMax  =  SM_ITEM_Z + 0.25f;
            float yBase =  smStartY - smTotalH - 0.06f;
            float yTop  =  smStartY + SM_SLOT_H * 0.5f + 0.06f;

            if (Mathf.Abs(localPos.x) > halfW || localPos.z < zMin || localPos.z > zMax ||
                localPos.y < yBase || localPos.y > yTop) return false;
        }
        else
        {
            float totalWidth = (arrayCapacity - 1) * carSpacing;
            float halfW      = totalWidth * 0.5f + boundaryMargin;
            if (localPos.x < -halfW || localPos.x > halfW ||
                localPos.z < -0.45f || localPos.z > 0.45f ||
                localPos.y <  0.01f || localPos.y > 0.25f) return false;
        }

        // Collision with other items
        for (int i = 0; i < arrayCapacity; i++)
            if (parkingArray[i].isOccupied && parkingArray[i].carObject != null &&
                parkingArray[i].carObject != movingCar &&
                Vector3.Distance(localPos, parkingArray[i].carObject.transform.localPosition) < collisionCheckDistance)
                return false;

        return true;
    }

    void CheckConfirmDistance()
    {
        if (movingCar == null) return;
        float dist  = Vector3.Distance(movingCar.transform.localPosition, targetPosition);
        bool  close = dist < ZoomAdjustedConfirmThreshold();
        if (confirmButton != null) confirmButton.SetActive(close);

        if (close)
            UpdateInstructionsSuccess(" Perfect position! Tap CONFIRM");
        else
        {
            string addrStr = IsGridScenario ? $"slot {GridAddress(targetIndex)}" : $"spot [{targetIndex}]";
            UpdateInstructions($"Move to {addrStr} (dist: {dist:F3})");
        }
    }

    void SetupConfirmButton(System.Action onConfirm)
    {
        if (confirmButton == null) return;
        var button = confirmButton.GetComponent<UnityEngine.UI.Button>();
        if (button != null) { button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => onConfirm()); }
    }

    public void OnConfirmClick()
    {
        if (isRemovingCar) OnConfirmRemoval(); else OnConfirmPlacement();
    }

    void CancelMovement()
    {
        if (movingCar != null && !isInsertMode)
        {
            Vector3 restorePos = IsGridScenario
                ? GridSlotLocalPos(targetIndex)
                : new Vector3(
                    parkingArray[targetIndex].spotObject.transform.localPosition.x, 0.03f,
                    parkingArray[targetIndex].spotObject.transform.localPosition.z);
            movingCar.transform.localPosition = restorePos;
            parkingArray[targetIndex].carObject  = movingCar;
            parkingArray[targetIndex].isOccupied = true;
            UpdateInstructions("Operation cancelled — item returned");
        }
        else if (movingCar != null)
        {
            Destroy(movingCar);
            UpdateInstructions("Operation cancelled");
        }
        CleanupMovement();
    }

    void CleanupMovement()
    {
        if (targetPositionIndicator != null) { Destroy(targetPositionIndicator); targetPositionIndicator = null; }
        movingCar    = null;
        currentState = ArrayState.Ready;
        targetIndex  = -1;
        StopMoving();
        SetActive(confirmDeleteButton, true);
        SetActive(confirmAccessButton, true);
        SetActive(mainButtonPanel,      true);
        SetActive(indexInputPanel,      false);
        SetActive(movementControlPanel, false);
        if (confirmButton != null) confirmButton.SetActive(false);
        ShowModeButtons(currentDifficulty);
    }

    void CreateTargetIndicator()
    {
        targetPositionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        targetPositionIndicator.transform.SetParent(parkingLot.transform);
        targetPositionIndicator.transform.localPosition = targetPosition;
        targetPositionIndicator.transform.localScale    = new Vector3(0.12f, 0.005f, 0.15f);
        Renderer rend = targetPositionIndicator.GetComponent<Renderer>();
        Material mat  = new Material(Shader.Find("Unlit/Color"));
        mat.color     = new Color(0, 1, 0, 0.5f);
        rend.material = mat;
        Destroy(targetPositionIndicator.GetComponent<Collider>());
        StartCoroutine(PulseIndicator());
    }

    void CreateExitIndicator()
    {
        targetPositionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        targetPositionIndicator.transform.SetParent(parkingLot.transform);
        targetPositionIndicator.transform.localPosition = targetPosition;
        targetPositionIndicator.transform.localScale    = new Vector3(0.15f, 0.005f, 0.18f);
        Renderer rend = targetPositionIndicator.GetComponent<Renderer>();
        Material mat  = new Material(Shader.Find("Unlit/Color"));
        mat.color     = new Color(1, 0, 0, 0.6f);
        rend.material = mat;
        Destroy(targetPositionIndicator.GetComponent<Collider>());
        CreateTextLabel(targetPositionIndicator.transform, "OUT",
            new Vector3(0, 0.08f, 0), Color.red, 50, new Vector3(0.002f, 0.002f, 0.002f));
        StartCoroutine(PulseIndicator());
    }

    IEnumerator PulseIndicator()
    {
        while (targetPositionIndicator != null)
        {
            float s = 0.12f + Mathf.Sin(Time.time * 3f) * 0.02f;
            targetPositionIndicator.transform.localScale = new Vector3(s, 0.005f, 0.15f);
            yield return null;
        }
    }

    void UpdateStatus_Beginner()
    {
        if (statusText == null) return;
        string verb = currentScenario == Scenario.Parking ? "Parked" : "Stocked";
        statusText.text = $"{verb}: {CountOccupied()}/{arrayCapacity}";
    }

    int CountOccupied()
    {
        int n = 0;
        for (int i = 0; i < arrayCapacity; i++) if (parkingArray[i].isOccupied) n++;
        return n;
    }

    // =========================================================================
    // INTERMEDIATE MODE
    // =========================================================================
    void StartIntermediateMode()
    {
        currentState = ArrayState.Ready;
        SetActive(mainButtonPanel,  true);
        SetActive(explanationPanel, true);
        ShowModeButtons(Difficulty.Intermediate);

        string priceOrPlate = (currentScenario == Scenario.Parking) ? "Plate #" : "Price (¢)";
        UpdateInstructions($"Sorted {ScenarioDisplayName()} Ready! Tap SORTED {InsertVerb()} to add first item.");

        if (operationInfoText != null)
            operationInfoText.text =
                "SORTED ARRAY!\n\n" +
                $"Items sorted by {priceOrPlate}\n" +
                "INSERT: Shift items to keep order O(n)\n" +
                "SEARCH: Binary Search O(log n)\n\n" +
                "Sorted = faster search!\nBut slower insert!";

        if (detectionText != null) { detectionText.text = "Scene Placed!"; detectionText.color = Color.green; }
        UpdateSortedDisplay();
        UpdateStatus_Intermediate();
    }

    // ── SORTED INSERT (Intermediate) ──────────────────────────────────────────
    public void OnSortedInsertButton()
    {
        if (currentState != ArrayState.Ready) return;
        if (occupiedCount >= arrayCapacity)
        {
            UpdateInstructionsError($" {ScenarioDisplayName()} FULL! Remove an item first.");
            return;
        }

        currentState = ArrayState.SelectingIndex;
        SetActive(mainButtonPanel,          false);
        SetActive(intermediateButtonsPanel, false);
        SetActive(confirmDeleteButton,      false);
        SetActive(confirmAccessButton,      false);
        SetActive(indexInputPanel,          true);

        bool isParking = currentScenario == Scenario.Parking;
        UpdateInstructions(isParking
            ? "Enter a plate number (1–99) to insert in sorted order"
            : "Enter a price in cents (1–99) to stock in price order");

        if (operationInfoText != null)
            operationInfoText.text =
                $"SORTED {InsertVerb()}\n\n" +
                (isParking
                    ? "Enter any plate number.\nSystem finds the right position\nand SHIFTs items to make room.\n\n"
                    : "Enter any price (1–99 cents).\nSystem finds the right slot\nand SHIFTs items to keep\nprice order.\n\n") +
                "Time Complexity: O(n)";
    }

    public void OnConfirmIndexUnified()
    {
        if (currentDifficulty == Difficulty.Intermediate) OnConfirmInsert();
        else OnConfirmIndex();
    }

    public void OnConfirmInsert()
    {
        TMP_InputField input = plateNumberInput != null ? plateNumberInput : indexInputField;
        if (input == null) return;

        bool isParking = currentScenario == Scenario.Parking;
        string label   = isParking ? "plate" : "price";

        if (int.TryParse(input.text, out int number))
        {
            if (number < 1 || number > 99) { UpdateInstructionsError($" Use {label}s 1–99"); return; }
            for (int i = 0; i < occupiedCount; i++)
                if (parkingArray[i].plateNumber == number)
                {
                    UpdateInstructionsError($" Value {number} already exists!");
                    return;
                }
            SetActive(indexInputPanel, false);
            StartCoroutine(AnimatedSortedInsert(number));
        }
        else UpdateInstructionsError($" Enter a valid {label}!");
    }

    IEnumerator AnimatedSortedInsert(int number)
    {
        currentState = ArrayState.Shifting;
        if (occupiedCount >= arrayCapacity)
        {
            UpdateInstructionsError(" Array is FULL!");
            currentState = ArrayState.Ready;
            yield break;
        }

        bool   isParking = currentScenario == Scenario.Parking;
        string unitStr   = isParking ? "#" : "¢";

        int insertAt = occupiedCount;
        for (int i = 0; i < occupiedCount; i++)
            if (parkingArray[i].plateNumber > number) { insertAt = i; break; }

        string addrStr = IsGridScenario ? GridAddress(insertAt) : $"[{insertAt}]";
        if (operationInfoText != null)
            operationInfoText.text =
                $"INSERT {unitStr}{number}\n\n" +
                $"Correct position: {addrStr}\n" +
                $"Must shift {occupiedCount - insertAt} item(s) right\n\n" +
                "Time: O(n) — shifting is slow!\nCost of staying sorted.";

        UpdateInstructions($"Shifting items right to insert at slot {addrStr}…");

        for (int i = occupiedCount - 1; i >= insertAt; i--)
        {
            PlaySound(shiftSound);
            yield return StartCoroutine(AnimateCarShift(i, i + 1));
        }

        int colorIndex = number % carColors.Length;
        GameObject itemObj;
        if (currentScenario == Scenario.Supermarket)
            itemObj = CreateGroceryItem(colorIndex);
        else if (currentScenario == Scenario.Vending)
            itemObj = CreateProduct(colorIndex);
        else
            itemObj = CreateCar_Beginner(colorIndex);

        itemObj.transform.SetParent(parkingLot.transform);
        itemObj.transform.localScale = originalCarScale;

        Vector3 finalPos = IsGridScenario
            ? GridSlotLocalPos(insertAt)
            : new Vector3(parkingArray[insertAt].spotObject.transform.localPosition.x, 0.03f,
                          parkingArray[insertAt].spotObject.transform.localPosition.z);
        Vector3 spawnPos = finalPos;
        if (IsGridScenario) spawnPos.z += 0.16f; else spawnPos.y += 0.09f;
        itemObj.transform.localPosition = spawnPos;

        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            itemObj.transform.localPosition = Vector3.Lerp(spawnPos, finalPos, elapsed / 0.3f);
            yield return null;
        }
        itemObj.transform.localPosition = finalPos;
        itemObj.transform.localRotation = Quaternion.identity;

        parkingArray[insertAt].carObject   = itemObj;
        parkingArray[insertAt].isOccupied  = true;
        parkingArray[insertAt].plateNumber = number;
        UpdatePlateLabel(insertAt);
        occupiedCount++;
        PlaySound(carParkSound);

        UpdateInstructionsSuccess($" {unitStr}{number} inserted at slot {addrStr}! Array stays sorted.");
        UpdateSortedDisplay();
        UpdateStatus_Intermediate();

        if (operationInfoText != null)
            operationInfoText.text =
                $"{InsertVerb()} Complete!\n\n" +
                $"{unitStr}{number} → slot {addrStr}\n" +
                $"Occupied: {occupiedCount}/{arrayCapacity}\n\n" +
                $"Insert: O(n)  |  Search: O(log n)\nTrade-off of sorted arrays!";

        UnlockSecondaryButtons();
        currentState = ArrayState.Ready;
        SetActive(mainButtonPanel, true);
        ShowModeButtons(Difficulty.Intermediate);
    }

    IEnumerator AnimateCarShift(int fromIndex, int toIndex)
    {
        if (!parkingArray[fromIndex].isOccupied) yield break;

        GameObject item  = parkingArray[fromIndex].carObject;
        Vector3    start = item.transform.localPosition;
        Vector3    end   = IsGridScenario
            ? GridSlotLocalPos(toIndex)
            : new Vector3(parkingArray[toIndex].spotObject.transform.localPosition.x, 0.03f,
                          parkingArray[toIndex].spotObject.transform.localPosition.z);

        float elapsed = 0f;
        while (elapsed < shiftAnimDuration)
        {
            elapsed += Time.deltaTime;
            item.transform.localPosition = Vector3.Lerp(start, end, elapsed / shiftAnimDuration);
            yield return null;
        }
        item.transform.localPosition = end;

        parkingArray[toIndex].carObject   = parkingArray[fromIndex].carObject;
        parkingArray[toIndex].isOccupied  = true;
        parkingArray[toIndex].plateNumber = parkingArray[fromIndex].plateNumber;
        UpdatePlateLabel(toIndex);

        parkingArray[fromIndex].carObject   = null;
        parkingArray[fromIndex].isOccupied  = false;
        parkingArray[fromIndex].plateNumber = 0;
        ClearPlateLabel(fromIndex);
    }

    // =========================================================================
    // BINARY SEARCH  (Intermediate)
    // =========================================================================
    public void OnBinarySearchButton()
    {
        if (currentState != ArrayState.Ready) return;
        if (occupiedCount == 0)
        {
            UpdateInstructionsError(" Nothing to search — add items first!");
            return;
        }

        currentState = ArrayState.SelectingIndex;
        SetActive(mainButtonPanel,          false);
        SetActive(intermediateButtonsPanel, false);
        SetActive(searchInputPanel,         true);

        bool isParking = currentScenario == Scenario.Parking;
        UpdateInstructions(isParking ? "Enter plate # to Binary Search" : "Enter price (¢) to Binary Search");

        if (operationInfoText != null)
            operationInfoText.text =
                "BINARY SEARCH\n\n" +
                "Works ONLY on sorted arrays!\n" +
                "Halves the search space each step.\n\n" +
                $"Current items: {occupiedCount}\n" +
                $"Max steps: {Mathf.CeilToInt(Mathf.Log(occupiedCount + 1, 2))}\n\n" +
                "Time Complexity: O(log n)";
    }

    public void OnConfirmSearch()
    {
        if (searchPlateInput == null) return;
        if (int.TryParse(searchPlateInput.text, out int number))
        {
            SetActive(searchInputPanel, false);
            SetActive(mainButtonPanel,  true);
            ShowModeButtons(Difficulty.Intermediate);
            StartCoroutine(BinarySearch(number));
        }
        else UpdateInstructionsError(" Enter a valid number!");
    }

    IEnumerator BinarySearch(int targetNumber)
    {
        currentState = ArrayState.Searching;
        PlaySound(searchSound);

        bool   isParking = currentScenario == Scenario.Parking;
        string unit      = isParking ? "#" : "¢";

        int left = 0, right = occupiedCount - 1, steps = 0;
        UpdateInstructions($"Binary Search: {unit}{targetNumber}");

        while (left <= right)
        {
            int mid = (left + right) / 2;
            steps++;
            yield return StartCoroutine(HighlightSpot(mid, new Color(1, 1, 0, 0.5f), 0.8f));

            string midAddr = IsGridScenario ? GridAddress(mid) : $"[{mid}]";
            if (operationInfoText != null)
                operationInfoText.text =
                    $"BINARY SEARCH Step {steps}\n\n" +
                    $"Left={left}  Mid={mid}  Right={right}\n" +
                    $"Slot {midAddr}: {unit}{parkingArray[mid].plateNumber}\nvs target {unit}{targetNumber}\n\nEliminating half each step!";

            if (parkingArray[mid].plateNumber == targetNumber)
            {
                yield return StartCoroutine(HighlightSpot(mid, new Color(0, 1, 0, 0.6f), 1.5f));
                PlaySound(foundSound);
                string foundAddr = IsGridScenario ? GridAddress(mid) : $"[{mid}]";
                UpdateInstructionsSuccess($" Found {unit}{targetNumber} at slot {foundAddr} in {steps} step(s)!");

                if (operationInfoText != null)
                    operationInfoText.text =
                        $"FOUND!\n\n" +
                        $"{unit}{targetNumber} at slot {foundAddr} [index {mid}]\n" +
                        $"Steps needed: {steps}\n" +
                        $"Max possible: {Mathf.CeilToInt(Mathf.Log(occupiedCount + 1, 2))}\n\n" +
                        "Binary Search: O(log n)\nMuch faster than O(n) linear!";

                currentState = ArrayState.Ready;
                yield break;
            }
            else if (parkingArray[mid].plateNumber < targetNumber) left  = mid + 1;
            else                                                    right = mid - 1;

            yield return new WaitForSeconds(0.3f);
        }

        UpdateInstructionsError($"❌ {unit}{targetNumber} not found! ({steps} steps)");
        if (operationInfoText != null)
            operationInfoText.text =
                $"NOT FOUND\n\n" +
                $"{unit}{targetNumber} not in array\n" +
                $"Steps searched: {steps}\n\n" +
                "Binary Search: O(log n)\nStill fast even on miss!";

        currentState = ArrayState.Ready;
    }

    // =========================================================================
    // REMOVE LARGEST  (Intermediate)
    // =========================================================================
    public void OnRemoveLargestButton()
    {
        if (currentState != ArrayState.Ready || occupiedCount == 0)
        {
            if (occupiedCount == 0) UpdateInstructionsError(" Nothing to remove!");
            return;
        }

        bool   isParking = currentScenario == Scenario.Parking;
        string unitStr   = isParking ? "#" : "¢";
        int    lastIdx   = occupiedCount - 1;
        int    removed   = parkingArray[lastIdx].plateNumber;

        if (parkingArray[lastIdx].carObject != null) Destroy(parkingArray[lastIdx].carObject);
        parkingArray[lastIdx].carObject   = null;
        parkingArray[lastIdx].isOccupied  = false;
        parkingArray[lastIdx].plateNumber = 0;
        ClearPlateLabel(lastIdx);

        occupiedCount--;
        PlaySound(carLeaveSound);
        UpdateSortedDisplay();
        UpdateStatus_Intermediate();
        CheckAndUpdateButtonStates();

        string addr = IsGridScenario ? GridAddress(lastIdx) : $"[{lastIdx}]";
        string verb = isParking ? "Largest" : "Priciest";
        UpdateInstructionsSuccess($" Removed {verb} ({unitStr}{removed}) from slot {addr} — O(1)!");

        if (operationInfoText != null)
            operationInfoText.text =
                $"REMOVE {verb.ToUpper()} Complete!\n\n" +
                $"Removed {unitStr}{removed} from end [{addr}]\nNo shifting needed!\n\n" +
                "Time Complexity: O(1)\nRemoving from end is always fast!";
    }

    public void OnCancelInput()
    {
        SetActive(indexInputPanel,     false);
        SetActive(searchInputPanel,    false);
        SetActive(confirmDeleteButton, true);
        SetActive(confirmAccessButton, true);
        SetActive(mainButtonPanel,     true);
        ShowModeButtons(currentDifficulty);
        currentState = ArrayState.Ready;
        UpdateInstructions("Operation cancelled");
    }

    // =========================================================================
    // HIGHLIGHT HELPERS
    // =========================================================================
    IEnumerator HighlightSpot(int index, Color color, float duration)
    {
        GameObject h = GameObject.CreatePrimitive(PrimitiveType.Cube);
        h.transform.SetParent(parkingLot.transform);

        if (IsGridScenario)
        {
            Vector3 slotPos = GridSlotLocalPos(index);
            h.transform.localPosition = slotPos;
            float slotW = currentScenario == Scenario.Supermarket ? SM_SLOT_W : VM_SLOT_W;
            float slotH = currentScenario == Scenario.Supermarket ? SM_SLOT_H : VM_SLOT_H;
            h.transform.localScale    = new Vector3(slotW * 0.9f, slotH * 0.9f, 0.003f);
        }
        else
        {
            Vector3 slotPos = parkingArray[index].spotObject.transform.localPosition;
            h.transform.localPosition = new Vector3(slotPos.x, 0.001f, slotPos.z);
            h.transform.localScale    = new Vector3(0.18f, 0.003f, 0.22f);
        }

        Renderer rend = h.GetComponent<Renderer>();
        Material mat  = new Material(Shader.Find("Unlit/Color"));
        mat.color     = color;
        rend.material = mat;
        Destroy(h.GetComponent<Collider>());
        yield return new WaitForSeconds(duration);
        Destroy(h);
    }

    void UpdatePlateLabel(int index)
    {
        if (parkingArray[index].plateLabel != null) Destroy(parkingArray[index].plateLabel);
        if (parkingArray[index].isOccupied)
        {
            bool   isParking = currentScenario == Scenario.Parking;
            string unitStr   = isParking ? "#" : "¢";
            Vector3 slotPos  = IsGridScenario
                ? GridSlotLocalPos(index)
                : parkingArray[index].spotObject.transform.localPosition;

            Vector3 labelPos = slotPos;
            if (IsGridScenario)
            {
                float labelZ = currentScenario == Scenario.Supermarket ? SM_LABEL_Z : VM_LABEL_Z;
                labelPos.z  = labelZ;
                float slotH = currentScenario == Scenario.Supermarket ? SM_SLOT_H : VM_SLOT_H;
                labelPos.y += slotH * 0.42f;
            }
            else
            {
                labelPos.y = 0.09f;
            }

            parkingArray[index].plateLabel = CreateTextLabel(
                parkingLot.transform,
                $"{unitStr}{parkingArray[index].plateNumber}",
                labelPos,
                IsGridScenario ? Color.white : Color.cyan,
                IsGridScenario ? 70 : 90,
                new Vector3(0.005f, 0.005f, 0.005f));
        }
    }

    void ClearPlateLabel(int index)
    {
        if (parkingArray[index].plateLabel != null)
        { Destroy(parkingArray[index].plateLabel); parkingArray[index].plateLabel = null; }
    }

    void UpdateSortedDisplay()
    {
        if (sortedOrderDisplay == null) return;
        bool   isParking = currentScenario == Scenario.Parking;
        string unit      = isParking ? "#" : "¢";
        if (occupiedCount == 0) { sortedOrderDisplay.text = $"Sorted {unit}: [ ]"; return; }

        string display = $"Sorted {unit}: [ ";
        for (int i = 0; i < occupiedCount; i++)
        {
            display += $"{unit}{parkingArray[i].plateNumber}";
            if (i < occupiedCount - 1) display += " , ";
        }
        sortedOrderDisplay.text = display + " ]";
    }

    void UpdateStatus_Intermediate()
    {
        if (statusText == null) return;
        bool   isParking = currentScenario == Scenario.Parking;
        string sort      = isParking ? "Sorted " : "Sorted ¢";
        statusText.text  = $"Items: {occupiedCount}/{arrayCapacity} | {sort}";
    }

    // =========================================================================
    // SCENE BUILDERS
    // =========================================================================

    // ── PARKING LOT ───────────────────────────────────────────────────────────
    void CreateParkingArray()
    {
        parkingArray = new ParkingSpot[arrayCapacity];
        float totalWidth = (arrayCapacity - 1) * carSpacing;
        float startX     = -totalWidth / 2f;

        for (int i = 0; i < arrayCapacity; i++)
        {
            ParkingSpot spot    = new ParkingSpot { index = i, isOccupied = false };
            Vector3     slotPos = new Vector3(startX + (i * carSpacing), 0, 0);

            if (parkingSpotPrefab != null)
            {
                spot.spotObject = Instantiate(parkingSpotPrefab, parkingLot.transform);
                spot.spotObject.transform.localPosition = slotPos;
            }
            else
            {
                spot.spotObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spot.spotObject.transform.SetParent(parkingLot.transform);
                spot.spotObject.transform.localPosition = slotPos;
                spot.spotObject.transform.localRotation = Quaternion.identity;
                spot.spotObject.transform.localScale    = new Vector3(0.17f, 0.01f, 0.22f);

                Renderer rend = spot.spotObject.GetComponent<Renderer>();
                Material mat  = new Material(Shader.Find("Standard"));
                mat.color     = new Color(0.3f, 0.3f, 0.3f);
                rend.material = mat;
                Destroy(spot.spotObject.GetComponent<Collider>());

              for (int s = -1; s <= 1; s += 2)
                    {
                        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        line.transform.SetParent(parkingLot.transform);
                        line.transform.localPosition = new Vector3(
                            slotPos.x + s * (0.17f / 2f + 0.003f),
                            0.005f,   // ground level — flush with spot top surface
                            slotPos.z);
                        line.transform.localScale    = new Vector3(0.005f, 0.010f, 0.22f);
                        line.transform.localRotation = Quaternion.identity;
                        Material lineMat = new Material(Shader.Find("Standard"));
                        lineMat.color = Color.white;
                        line.GetComponent<Renderer>().material = lineMat;
                        Destroy(line.GetComponent<Collider>());
                    }

            }

            spot.indexLabel = CreateTextLabel(spot.spotObject.transform,
                $"[{i}]", new Vector3(0, 0.02f, -0.6f),
                Color.yellow, 100, new Vector3(0.008f, 0.008f, 0.008f));

            spot.spotObject.name = $"ParkingSpot_{i}";
            parkingArray[i] = spot;
        }
    }

    // ── VENDING MACHINE ───────────────────────────────────────────────────────
    void BuildVendingMachineScene()
    {
        parkingArray = new ParkingSpot[arrayCapacity];

        vmTotalW = GRID_COLS * VM_SLOT_W + (GRID_COLS - 1) * VM_SLOT_GAP;
        vmTotalH = GRID_ROWS * VM_SLOT_H + (GRID_ROWS - 1) * VM_SLOT_GAP;
        vmStartX = -vmTotalW * 0.5f;

        float trayZoneH   = 0.028f;
        float gridBottomY = VM_BODY_PANEL_T + trayZoneH;
        float gridTopY    = gridBottomY + vmTotalH;
        vmStartY = gridTopY - VM_SLOT_H * 0.5f;

        float machW       = vmTotalW + 0.030f;
        float machH       = gridTopY + 0.014f;
        float machCentreY = machH * 0.5f;
        float bodyFrontZ  = VM_BODY_DEPTH * 0.5f;

        Color steelDark  = new Color(0.20f, 0.20f, 0.24f);
        Color steelLight = new Color(0.32f, 0.32f, 0.38f);
        Color slotColor  = new Color(0.12f, 0.12f, 0.15f);

        BuildGridPanel("VM_Body",
            new Vector3(0, machCentreY, 0f),
            new Vector3(machW, machH, VM_BODY_DEPTH), steelDark);

        BuildGridPanel("VM_Front",
            new Vector3(0, machCentreY, bodyFrontZ),
            new Vector3(machW, machH, 0.003f), steelLight);

        for (int i = 0; i < arrayCapacity; i++)
        {
            int col = i % GRID_COLS;
            int row = i / GRID_COLS;
            float slotCX = -(vmStartX + col * (VM_SLOT_W + VM_SLOT_GAP) + VM_SLOT_W * 0.5f);
            float slotCY =  vmStartY  - row * (VM_SLOT_H + VM_SLOT_GAP);

            BuildGridPanel($"SlotBG_{i}",
                new Vector3(slotCX, slotCY, bodyFrontZ + 0.002f),
                new Vector3(VM_SLOT_W - 0.003f, VM_SLOT_H - 0.003f, 0.003f), slotColor);
        }

        float trayMidY = VM_BODY_PANEL_T + trayZoneH * 0.5f;
        BuildGridPanel("VM_Tray",
            new Vector3(0, trayMidY, bodyFrontZ + 0.003f),
            new Vector3(machW * 0.75f, trayZoneH - 0.004f, 0.005f),
            new Color(0.08f, 0.08f, 0.10f));
        BuildGridPanel("VM_TrayRim",
            new Vector3(0, gridBottomY - 0.002f, bodyFrontZ + 0.002f),
            new Vector3(machW, 0.003f, 0.004f), steelLight);

        float cpX = -(machW * 0.5f - 0.012f);
        BuildGridPanel("VM_CtrlBg",
            new Vector3(cpX, machCentreY, bodyFrontZ + 0.004f),
            new Vector3(0.018f, machH * 0.55f, 0.004f), new Color(0.12f, 0.12f, 0.16f));
        Color[] cpCol = { new Color(1f,0.2f,0.2f), new Color(0.2f,0.85f,0.2f), new Color(0.2f,0.5f,1f) };
        for (int b = 0; b < 3; b++)
            BuildGridPanel($"VM_Btn_{b}",
                new Vector3(cpX, machCentreY + (b - 1) * 0.018f, bodyFrontZ + 0.007f),
                new Vector3(0.009f, 0.007f, 0.003f), cpCol[b]);

        CreateTextLabel(parkingLot.transform, "ARRAY VENDING",
            new Vector3(0, machH + 0.016f, VM_LABEL_Z),
            Color.cyan, 80, new Vector3(0.0036f, 0.0036f, 0.0036f));

        CreateTextLabel(parkingLot.transform, "[ TRAY ]",
            new Vector3(0, trayMidY, VM_LABEL_Z),
            new Color(1f, 0.85f, 0.2f), 55, new Vector3(0.0029f, 0.0029f, 0.0029f));

        BuildGridSlotAnchors(VM_SLOT_W, VM_SLOT_H, VM_SLOT_GAP, VM_PRODUCT_Z, VM_LABEL_Z, vmStartX, vmStartY);

        // Row headers A / B
        for (int r = 0; r < GRID_ROWS; r++)
        {
            float rowY = vmStartY - r * (VM_SLOT_H + VM_SLOT_GAP);
            CreateTextLabel(parkingLot.transform, $"{(char)('A' + r)}",
                new Vector3(machW * 0.5f + 0.018f, rowY, VM_LABEL_Z),
                Color.white, 80, new Vector3(0.0040f, 0.0040f, 0.0040f));
        }
        // Column headers 0 / 1 / 2
        for (int c = 0; c < GRID_COLS; c++)
        {
            float colX = -(vmStartX + c * (VM_SLOT_W + VM_SLOT_GAP) + VM_SLOT_W * 0.5f);
            CreateTextLabel(parkingLot.transform, $"{c}",
                new Vector3(colX, vmStartY + VM_SLOT_H * 0.5f + 0.016f, VM_LABEL_Z),
                Color.white, 65, new Vector3(0.0027f, 0.0027f, 0.0027f));
        }
    }

    // ── SUPERMARKET SHELF  ────────────────────────────────────────────────────
    /// <summary>
    /// Builds a wooden grocery shelf with:
    ///   - Two horizontal shelf planks (top row & bottom row)
    ///   - Vertical dividers between slots
    ///   - Dark slot backgrounds
    ///   - Price-tag rails below each row
    ///   - Index labels [0]–[5] and grid address labels A0–B2
    ///   - "ARRAY MARKET" title above the shelf
    /// </summary>
    void BuildSupermarketScene()
    {
        parkingArray = new ParkingSpot[arrayCapacity];

        smTotalW = GRID_COLS * SM_SLOT_W + (GRID_COLS - 1) * SM_SLOT_GAP;
        smTotalH = GRID_ROWS * SM_SLOT_H + (GRID_ROWS - 1) * SM_SLOT_GAP;
        smStartX = -smTotalW * 0.5f;

        // Shelf sits just above y=0
        float baseY   = 0.004f;            // bottom of bottom row
        float rowGap  = SM_SLOT_H + SM_SLOT_GAP;
        smStartY = baseY + SM_SLOT_H * 0.5f + rowGap;   // centre of top row

        float shelfW  = smTotalW + 0.040f;   // slightly wider than slot area
        float shelfT  = 0.012f;              // plank thickness
        float bodyD   = SM_SHELF_DEPTH;

        Color woodColor   = new Color(0.55f, 0.36f, 0.18f);
        Color darkWood    = new Color(0.40f, 0.26f, 0.12f);
        Color slotBg      = new Color(0.18f, 0.12f, 0.06f);
        Color tagColor    = new Color(0.92f, 0.90f, 0.82f);   // cream price-tag strip

        // ── Base plank (below row B) ─────────────────────────────────────────
        BuildGridPanel("SM_Base",
            new Vector3(0, baseY - shelfT * 0.5f, 0f),
            new Vector3(shelfW, shelfT, bodyD), darkWood);

        // ── Two horizontal shelf planks ─────────────────────────────────────
        for (int r = 0; r < GRID_ROWS; r++)
        {
            float plankY = smStartY - r * rowGap + SM_SLOT_H * 0.5f + shelfT * 0.5f;
            BuildGridPanel($"SM_Plank_{r}",
                new Vector3(0, plankY, 0f),
                new Vector3(shelfW, shelfT, bodyD), woodColor);
        }

        // ── Back panel ───────────────────────────────────────────────────────
        BuildGridPanel("SM_Back",
            new Vector3(0, smStartY, -bodyD * 0.5f + 0.002f),
            new Vector3(shelfW, smTotalH + shelfT * 3, 0.005f), darkWood);

        // ── Vertical side panels ─────────────────────────────────────────────
        for (int side = -1; side <= 1; side += 2)
        {
            BuildGridPanel($"SM_Side_{(side < 0 ? "L" : "R")}",
                new Vector3(side * shelfW * 0.5f, smStartY, 0f),
                new Vector3(0.010f, smTotalH + shelfT * 3, bodyD), darkWood);
        }

        // ── Slot dividers (vertical, between columns) ────────────────────────
        for (int c = 1; c < GRID_COLS; c++)
        {
            float divX = -(smStartX + c * (SM_SLOT_W + SM_SLOT_GAP) - SM_SLOT_GAP * 0.5f);
            BuildGridPanel($"SM_Div_{c}",
                new Vector3(divX, smStartY, 0f),
                new Vector3(SM_SLOT_GAP * 0.8f, smTotalH, bodyD * 0.9f), darkWood);
        }

        // ── Dark slot backgrounds ────────────────────────────────────────────
        for (int i = 0; i < arrayCapacity; i++)
        {
            int col = i % GRID_COLS;
            int row = i / GRID_COLS;
            float slotCX = -(smStartX + col * (SM_SLOT_W + SM_SLOT_GAP) + SM_SLOT_W * 0.5f);
            float slotCY =  smStartY  - row * rowGap;
            float frontZ = bodyD * 0.5f - 0.002f;

            BuildGridPanel($"SM_SlotBG_{i}",
                new Vector3(slotCX, slotCY, frontZ),
                new Vector3(SM_SLOT_W - 0.004f, SM_SLOT_H - 0.006f, 0.004f), slotBg);
        }

        // ── Price-tag rails below each row ────────────────────────────────────
        for (int r = 0; r < GRID_ROWS; r++)
        {
            float railY = smStartY - r * rowGap - SM_SLOT_H * 0.5f - shelfT * 0.3f;
            BuildGridPanel($"SM_PriceRail_{r}",
                new Vector3(0, railY, bodyD * 0.5f + 0.001f),
                new Vector3(shelfW, 0.014f, 0.003f), tagColor);
        }

        // ── Title label ──────────────────────────────────────────────────────
        float topY = smStartY + SM_SLOT_H * 0.5f + shelfT + 0.018f;
        CreateTextLabel(parkingLot.transform, "ARRAY MARKET",
            new Vector3(0, topY, SM_LABEL_Z),
            new Color(1f, 0.8f, 0.2f), 80, new Vector3(0.0036f, 0.0036f, 0.0036f));

        // ── Slot anchors + index/address labels ─────────────────────────────
        BuildGridSlotAnchors(SM_SLOT_W, SM_SLOT_H, SM_SLOT_GAP, SM_ITEM_Z, SM_LABEL_Z, smStartX, smStartY);

        // Row headers A / B
        for (int r = 0; r < GRID_ROWS; r++)
        {
            float rowY = smStartY - r * rowGap;
            CreateTextLabel(parkingLot.transform, $"{(char)('A' + r)}",
                new Vector3(shelfW * 0.5f + 0.016f, rowY, SM_LABEL_Z),
                Color.white, 75, new Vector3(0.0040f, 0.0040f, 0.0040f));
        }
        // Column headers 0 / 1 / 2
        for (int c = 0; c < GRID_COLS; c++)
        {
            float colX = -(smStartX + c * (SM_SLOT_W + SM_SLOT_GAP) + SM_SLOT_W * 0.5f);
            CreateTextLabel(parkingLot.transform, $"{c}",
                new Vector3(colX, smStartY + SM_SLOT_H * 0.5f + 0.014f, SM_LABEL_Z),
                Color.white, 60, new Vector3(0.0027f, 0.0027f, 0.0027f));
        }
    }

    /// <summary>
    /// Shared helper: creates the invisible ParkingSpot anchor GameObjects and
    /// the index / grid-address text labels for any 2×3 grid scenario.
    /// </summary>
    void BuildGridSlotAnchors(float slotW, float slotH, float slotGap,
                              float itemZ, float labelZ, float startX, float startY)
    {
        float rowGap = slotH + slotGap;

        for (int i = 0; i < arrayCapacity; i++)
        {
            int col = i % GRID_COLS;
            int row = i / GRID_COLS;
            float slotCX = -(startX + col * (slotW + slotGap) + slotW * 0.5f);
            float slotCY =  startY  - row * rowGap;

            GameObject anchor = new GameObject($"GridSlot_{i}");
            anchor.transform.SetParent(parkingLot.transform);
            anchor.transform.localPosition = new Vector3(slotCX, slotCY, itemZ);
            anchor.transform.localRotation = Quaternion.identity;

            ParkingSpot slot = new ParkingSpot { index = i, isOccupied = false, spotObject = anchor };

            // [i] flat index — yellow, below slot
            slot.indexLabel = CreateTextLabel(parkingLot.transform,
                $"[{i}]",
                new Vector3(slotCX, slotCY - slotH * 0.44f, labelZ),
                Color.yellow, 60, new Vector3(0.0027f, 0.0027f, 0.0027f));

            // Grid address (A0, B2…) — light, above slot
            CreateTextLabel(parkingLot.transform,
                GridAddress(i),
                new Vector3(slotCX, slotCY + slotH * 0.44f, labelZ),
                new Color(0.85f, 0.85f, 1.0f), 58, new Vector3(0.0027f, 0.0027f, 0.0027f));

            parkingArray[i] = slot;
        }
    }

    /// <summary>Helper: creates a named solid-colour cube panel parented to parkingLot.</summary>
    GameObject BuildGridPanel(string name, Vector3 localPos, Vector3 localScale, Color color)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parkingLot.transform);
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale    = localScale;
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Glossiness", 0.45f);
        obj.GetComponent<Renderer>().material = mat;
        Destroy(obj.GetComponent<Collider>());
        return obj;
    }

    // =========================================================================
    // ITEM FACTORIES
    // =========================================================================

    // ── Parking Lot: Car ──────────────────────────────────────────────────────
    GameObject CreateCar_Beginner(int colorIndex)
    {
        if (carPrefabs != null && carPrefabs.Length > 0)
        {
            List<GameObject> valid = new List<GameObject>();
            foreach (var p in carPrefabs) if (p != null) valid.Add(p);
            if (valid.Count > 0)
            {
                GameObject inst = Instantiate(valid[Random.Range(0, valid.Count)]);
                ApplyColorToPrefab(inst, carColors[colorIndex % carColors.Length]);
                originalCarScale = inst.transform.localScale;
                return inst;
            }
        }
        GameObject car = CreateProceduralCar(colorIndex);
        originalCarScale = car.transform.localScale;
        return car;
    }

    void ApplyColorToPrefab(GameObject inst, Color color)
    {
        foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
            if (r.material != null) r.material.color = color;

        if (inst.GetComponent<BoxCollider>() == null)
        {
            BoxCollider col = inst.AddComponent<BoxCollider>();
            col.size = new Vector3(0.08f, 0.06f, 0.12f);
        }
        if (inst.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = inst.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
        }
    }

    GameObject CreateProceduralCar(int colorIndex)
    {
        GameObject car      = new GameObject($"Car_{carIdCounter}");
        Color      carColor = carColors[colorIndex % carColors.Length];

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(car.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale    = new Vector3(0.08f, 0.04f, 0.12f);
        body.GetComponent<Renderer>().material.color = carColor;

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(body.transform);
        roof.transform.localPosition = new Vector3(0, 0.7f, -0.15f);
        roof.transform.localScale    = new Vector3(0.9f, 0.6f, 0.6f);
        roof.GetComponent<Renderer>().material.color = carColor * 0.8f;

        CreateWheel(body.transform, new Vector3(-0.4f, -0.7f,  0.5f));
        CreateWheel(body.transform, new Vector3( 0.4f, -0.7f,  0.5f));
        CreateWheel(body.transform, new Vector3(-0.4f, -0.7f, -0.5f));
        CreateWheel(body.transform, new Vector3( 0.4f, -0.7f, -0.5f));

        Destroy(body.GetComponent<Collider>());
        Destroy(roof.GetComponent<Collider>());

        BoxCollider col = car.AddComponent<BoxCollider>();
        col.size = new Vector3(0.08f, 0.06f, 0.12f);
        Rigidbody rb = car.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
        return car;
    }

    void CreateWheel(Transform parent, Vector3 localPos)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.transform.SetParent(parent);
        wheel.transform.localPosition = localPos;
        wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
        wheel.transform.localScale    = new Vector3(0.15f, 0.08f, 0.15f);
        wheel.GetComponent<Renderer>().material.color = Color.black;
        Destroy(wheel.GetComponent<Collider>());
    }

    // ── Vending Machine: Product ──────────────────────────────────────────────
    GameObject CreateProduct(int colorIndex)
    {
        string name  = productNames[colorIndex % productNames.Length];
        Color  col   = carColors[colorIndex % carColors.Length];

        GameObject product = new GameObject($"Product_{name}_{carIdCounter}");

        float pW = VM_SLOT_W  * 0.72f;
        float pH = VM_SLOT_H  * 0.78f;
        float pD = VM_BODY_DEPTH * 0.50f;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(product.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale    = new Vector3(pW, pH, pD);
        Material bodyMat = new Material(Shader.Find("Standard"));
        bodyMat.color = col;
        bodyMat.SetFloat("_Glossiness", 0.55f);
        body.GetComponent<Renderer>().material = bodyMat;
        Destroy(body.GetComponent<Collider>());

        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.transform.SetParent(body.transform);
        top.transform.localPosition = new Vector3(0,  0.50f, 0);
        top.transform.localScale    = new Vector3(0.96f, 0.08f, 0.96f);
        top.GetComponent<Renderer>().material.color = col * 0.6f;
        Destroy(top.GetComponent<Collider>());

        GameObject bottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bottom.transform.SetParent(body.transform);
        bottom.transform.localPosition = new Vector3(0, -0.50f, 0);
        bottom.transform.localScale    = new Vector3(0.96f, 0.08f, 0.96f);
        bottom.GetComponent<Renderer>().material.color = col * 0.6f;
        Destroy(bottom.GetComponent<Collider>());

        GameObject label = GameObject.CreatePrimitive(PrimitiveType.Cube);
        label.transform.SetParent(body.transform);
        label.transform.localPosition = new Vector3(0, 0, 0.51f);
        label.transform.localScale    = new Vector3(0.90f, 0.55f, 0.03f);
        Material labelMat = new Material(Shader.Find("Standard"));
        labelMat.color = Color.white;
        label.GetComponent<Renderer>().material = labelMat;
        Destroy(label.GetComponent<Collider>());

        CreateTextLabel(body.transform, name,
            new Vector3(0, 0.10f, 0.55f), col * 0.6f, 90, new Vector3(0.022f, 0.022f, 0.022f));

        BoxCollider col2 = product.AddComponent<BoxCollider>();
        col2.size = new Vector3(pW, pH, pD);
        Rigidbody rb = product.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;

        originalCarScale = product.transform.localScale;
        return product;
    }

    // ── Supermarket: Grocery Item  ────────────────────────────────────────────
    /// <summary>
    /// A coloured rectangular box with a white brand panel and product name
    /// on the front face, plus a small yellow price-tag strip at the bottom.
    /// Sized to fit snugly in a supermarket shelf slot.
    /// </summary>
    GameObject CreateGroceryItem(int colorIndex)
    {
        string itemName = groceryNames[colorIndex % groceryNames.Length];
        Color  col      = carColors[colorIndex % carColors.Length];

        GameObject item = new GameObject($"Grocery_{itemName}_{carIdCounter}");

        float iW = SM_SLOT_W  * 0.74f;
        float iH = SM_SLOT_H  * 0.80f;
        float iD = SM_SHELF_DEPTH * 0.48f;

        // Main box body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(item.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale    = new Vector3(iW, iH, iD);
        Material bodyMat = new Material(Shader.Find("Standard"));
        bodyMat.color = col;
        bodyMat.SetFloat("_Glossiness", 0.4f);
        body.GetComponent<Renderer>().material = bodyMat;
        Destroy(body.GetComponent<Collider>());

        // Top cap (darker)
        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.transform.SetParent(body.transform);
        top.transform.localPosition = new Vector3(0, 0.49f, 0);
        top.transform.localScale    = new Vector3(0.97f, 0.07f, 0.97f);
        top.GetComponent<Renderer>().material.color = col * 0.65f;
        Destroy(top.GetComponent<Collider>());

        // White brand panel on the front face
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.transform.SetParent(body.transform);
        panel.transform.localPosition = new Vector3(0, 0.05f, 0.51f);
        panel.transform.localScale    = new Vector3(0.88f, 0.60f, 0.035f);
        Material panelMat = new Material(Shader.Find("Standard"));
        panelMat.color = Color.white;
        panel.GetComponent<Renderer>().material = panelMat;
        Destroy(panel.GetComponent<Collider>());

        // Product name text on white panel
        CreateTextLabel(body.transform, itemName,
            new Vector3(0, 0.08f, 0.56f), col * 0.55f, 85, new Vector3(0.020f, 0.020f, 0.020f));

        // Yellow price-tag strip at the bottom of the front face
        GameObject priceTag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        priceTag.transform.SetParent(body.transform);
        priceTag.transform.localPosition = new Vector3(0, -0.37f, 0.51f);
        priceTag.transform.localScale    = new Vector3(0.88f, 0.12f, 0.035f);
        Material tagMat = new Material(Shader.Find("Standard"));
        tagMat.color = new Color(1f, 0.92f, 0.2f);
        priceTag.GetComponent<Renderer>().material = tagMat;
        Destroy(priceTag.GetComponent<Collider>());

        // Collider + Rigidbody on root
        BoxCollider boxCol = item.AddComponent<BoxCollider>();
        boxCol.size = new Vector3(iW, iH, iD);
        Rigidbody rb = item.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;

        originalCarScale = item.transform.localScale;
        return item;
    }

    // =========================================================================
    // SHARED TEXT LABEL
    // =========================================================================
    GameObject CreateTextLabel(Transform parent, string text, Vector3 localPos,
        Color color, int fontSize, Vector3 scale)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(parent);
        labelObj.transform.localPosition = localPos;
        labelObj.transform.localRotation = Quaternion.identity;
        labelObj.transform.localScale    = scale;

        TextMesh textMesh  = labelObj.AddComponent<TextMesh>();
        textMesh.text      = text;
        textMesh.fontSize  = fontSize;
        textMesh.color     = color;
        textMesh.anchor    = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        labelObj.AddComponent<Billboard>();
        return labelObj;
    }

     public void PreFillForLesson(int count)
    {
        if (parkingArray == null || parkingLot == null) return;
        count = Mathf.Clamp(count, 1, arrayCapacity);

        for (int i = 0; i < count; i++)
        {
            if (parkingArray[i].isOccupied) continue;

            int colorIndex = (carIdCounter - 1) % carColors.Length;

            // Use the same factory the normal flow uses — no new code paths
            GameObject item;
            switch (currentScenario)
            {
                case Scenario.Supermarket:
                    item = CreateGroceryItem(colorIndex);
                    break;
                case Scenario.Vending:
                    item = CreateProduct(colorIndex);
                    break;
                default:
                    item = CreateCar_Beginner(colorIndex);
                    break;
            }

            item.transform.SetParent(parkingLot.transform);
            item.transform.localScale    = originalCarScale;
            item.transform.localRotation = Quaternion.identity;

            // Place directly at the slot's final position — no movement required
            if (IsGridScenario)
            {
                item.transform.localPosition = GridSlotLocalPos(i);
            }
            else
            {
                Vector3 slotPos = parkingArray[i].spotObject.transform.localPosition;
                item.transform.localPosition = new Vector3(slotPos.x, 0.03f, slotPos.z);
            }

            // Mark the slot occupied — same fields OnConfirmPlacement() sets
            parkingArray[i].carObject   = item;
            parkingArray[i].isOccupied  = true;
            parkingArray[i].plateNumber = 0;   // not a sorted-mode lesson
            carIdCounter++;
        }

        // Unlock remove/access buttons now that items exist
        CheckAndUpdateButtonStates();
    }

        public bool IsSlotOccupied(int index)
    {
        if (parkingArray == null || index < 0 || index >= arrayCapacity) return false;
        return parkingArray[index].isOccupied;
    }

     /// <summary>
    /// The parkingLot root GameObject — lets ARArrayLessonGuide sync its
    /// spawnedLot reference to the exact object items are parented to.
    /// </summary>
    

    /// <summary>
    /// Returns the carObject (item GameObject) at the given slot index, or null.
    /// Used by ARArrayLessonGuide to build traversal item lists directly from
    /// the controller's parkingArray — avoids name-search / hierarchy issues.
    /// </summary>
    public GameObject GetSlotItem(int index)
    {
        if (parkingArray == null || index < 0 || index >= arrayCapacity) return null;
        return parkingArray[index].carObject;
    }

    // =========================================================================
    // RESET
    // =========================================================================
    public void OnResetButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnResetButtonClicked();
        if (swipeRotation != null) swipeRotation.ResetRotation();
        if (zoomController  != null) zoomController.ResetZoom();

        StopAllCoroutines();
        pulseCoroutine = null;
        ResetInsertButtonScale();

        if (parkingLot != null) { Destroy(parkingLot); parkingLot = null; }
        if (movingCar  != null) { Destroy(movingCar);  movingCar  = null; }
        if (targetPositionIndicator != null) { Destroy(targetPositionIndicator); targetPositionIndicator = null; }

        parkingArray = null; sceneSpawned = false; carIdCounter = 1;
        occupiedCount = 0; targetIndex = -1; isInsertMode = false;
        isAccessMode = false; isRemovingCar = false; isMoving = false;
        hasInsertedAtLeastOne = false;
        currentMovementDirection = Vector3.zero;
        currentScenario   = Scenario.None;
        currentDifficulty = Difficulty.None;

        RelabelBeginnerButtonsDefault();

        if (planeManager   != null) planeManager.enabled   = false;
        if (raycastManager != null) raycastManager.enabled = false;

        if (planeManager != null)
            foreach (var plane in planeManager.trackables)
                if (plane?.gameObject != null) plane.gameObject.SetActive(true);

        HideAllPanels();

        if (detectionText != null) { detectionText.text = "Choose your scenario first"; detectionText.color = Color.white; }
        if (statusText    != null) statusText.text = "Items: 0";
        if (sortedOrderDisplay != null) sortedOrderDisplay.text = "Sorted: [ ]";

        ShowScenarioPanel();
    }

    // =========================================================================
    // UTILITIES
    // =========================================================================
    void PlaySound(AudioClip clip)              { if (audioSource != null && clip != null) audioSource.PlayOneShot(clip); }
    void SetActive(GameObject obj, bool active) { if (obj != null) obj.SetActive(active); }
}