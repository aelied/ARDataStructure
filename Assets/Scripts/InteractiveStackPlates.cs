using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

/// <summary>
/// STACK — Three Scenarios + Two Difficulties + Server-Driven Scenario Visibility
///
/// GUIDE MODE FIX:
///   SetInstructionSilence(bool silent) is called by ARStackLessonGuide on
///   InitGuide() to stop this controller from overwriting the guide's own
///   instructionText / operationInfoText / statusText / detectionText panels.
///
///   When silenced:
///     - UpdateInstructions / UpdateInstructionsSuccess / UpdateInstructionsError
///       are all no-ops, so movement prompts and ready messages don't clobber
///       the step text set by SyncControllerUI().
///     - operationInfoText, statusText, detectionText panels are hidden.
///     - instructionText panel is hidden (guide manages it directly).
///
///   SetInstructionSilence(false) is called by ARStackLessonGuide.OnReturn()
///   to restore all panels for sandbox / free-play use after the lesson ends.
/// </summary>
public class InteractiveStackPlates : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  AR COMPONENTS
    // ─────────────────────────────────────────────────────────────────────────
    [Header("AR Components")]
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;
    public Camera arCamera;

    [Header("Zoom Controller")]
    public SceneZoomController zoomController;

    [Header("Plane Visualization")]
    public GameObject planePrefab;

    [Header("Custom Assets (Optional)")]
    public GameObject platePrefab;
    public GameObject tablePrefab;

    // ─────────────────────────────────────────────────────────────────────────
    //  SERVER CONFIG
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Server Config")]
    [Tooltip("Base URL of the StructuReality backend, no trailing slash.")]
    public string serverUrl = "https://structureality-admin.onrender.com";

    [Tooltip("Data structure key used when querying the server.")]
    public string dataStructure = "Stacks";

    [Header("Offline Fallback Scenarios")]
    [Tooltip("Scenario IDs used when the server cannot be reached. Must match known IDs.")]
    public string[] fallbackScenarios = new string[] { "Plates", "Warehouse" };

    // ─────────────────────────────────────────────────────────────────────────
    //  UI — SHARED
    // ─────────────────────────────────────────────────────────────────────────
    [Header("UI References – Shared")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI detectionText;
    public TextMeshProUGUI operationInfoText;
    public GameObject explanationPanel;

    // ─────────────────────────────────────────────────────────────────────────
    //  UI — SCENARIO PANEL
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Scenario Panel")]
    public GameObject scenarioPanel;
    public TextMeshProUGUI scenarioTitleText;
    public UnityEngine.UI.Button btnScenarioPlates;
    public UnityEngine.UI.Button btnScenarioWarehouse;
    public UnityEngine.UI.Button btnScenarioKitchen;

    // ─────────────────────────────────────────────────────────────────────────
    //  UI — DIFFICULTY PANEL
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Difficulty Panel")]
    public GameObject difficultyPanel;
    public TextMeshProUGUI difficultyTitleText;
    public UnityEngine.UI.Button beginnerBtn;
    public UnityEngine.UI.Button intermediateBtn;

    // ─────────────────────────────────────────────────────────────────────────
    //  UI — BUTTON PANELS
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Beginner Button Panel")]
    public GameObject beginnerButtonPanel;

    [Header("Intermediate Button Panel")]
    public GameObject intermediateButtonPanel;
    public TMP_InputField multiPopInputField;

    // ─────────────────────────────────────────────────────────────────────────
    //  ACTION BUTTONS
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Beginner Action Buttons")]
    public UnityEngine.UI.Button beginnerPushButton;
    public UnityEngine.UI.Button beginnerPopButton;
    public UnityEngine.UI.Button beginnerPeekButton;

    [Header("Intermediate Action Buttons")]
    public UnityEngine.UI.Button intermediatePushButton;
    public UnityEngine.UI.Button intermediatePopButton;
    public UnityEngine.UI.Button intermediateMultiPopButton;
    public UnityEngine.UI.Button intermediateReverseButton;

    // ─────────────────────────────────────────────────────────────────────────
    //  UI — MOVEMENT
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Movement Control Panel")]
    public GameObject movementControlPanel;
    public GameObject confirmButton;

    [Header("Movement Control Buttons")]
    public UnityEngine.UI.Button moveLeftButton;
    public UnityEngine.UI.Button moveRightButton;
    public UnityEngine.UI.Button moveForwardButton;
    public UnityEngine.UI.Button moveBackButton;
    public UnityEngine.UI.Button cancelButton;

    // ─────────────────────────────────────────────────────────────────────────
    //  AUDIO
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Audio")]
    public AudioClip placeSceneSound;
    public AudioClip platePlaceSound;
    public AudioClip plateRemoveSound;
    public AudioClip peekSound;
    public AudioClip moveSound;
    public AudioClip boxPlaceSound;
    public AudioClip boxRemoveSound;
    public AudioClip potPlaceSound;
    public AudioClip potRemoveSound;

    // ─────────────────────────────────────────────────────────────────────────
    //  STACK SETTINGS
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Stack Settings")]
    public int   maxStackSize             = 10;
    public float plateHeight              = 0.015f;
    public float boxHeight                = 0.055f;
    public float potHeight                = 0.045f;
    public float moveSpeed                = 1.5f;
    public float confirmDistanceThreshold = 0.1f;
    public float sceneHeightOffset        = 0.05f;
    public int CurrentStackCount => itemStack.Count;

    [Header("Tutorial System")]
    public StackTutorialIntegration tutorialIntegration;

    [Header("Swipe Rotation")]
    public SwipeRotation swipeRotation;

    public GameObject ParkingLot => stackScene;

    // ─────────────────────────────────────────────────────────────────────────
    //  ENUMS
    // ─────────────────────────────────────────────────────────────────────────
    public enum Scenario { None, Plates, Warehouse, Kitchen }
    private enum Difficulty { None, Beginner, Intermediate }

    private enum StackState
    {
        ChoosingScenario,
        ChoosingDifficulty,
        WaitingForPlane,
        Ready,
        PushingItem,
        PoppingItem,
        MultiPopping,
        ReversingStack
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────────────────────────────────
    private AudioSource audioSource;
    private Scenario   currentScenario   = Scenario.None;
    private Difficulty currentDifficulty = Difficulty.None;
    private StackState currentState      = StackState.ChoosingScenario;

    private string[] activeScenarioIds = null;

    private Coroutine pulseCoroutine = null;
    private Dictionary<UnityEngine.UI.Button, Color> originalButtonColors
        = new Dictionary<UnityEngine.UI.Button, Color>();

    private ARStackLessonGuide      cachedGuide;
    private ARStackLessonAssessment cachedAssessment;

    // ── GUIDE MODE SILENCE FLAG ───────────────────────────────────────────────
    private bool _silenceInstructions = false;

    /// <summary>
    /// Called by ARStackLessonGuide to stop this controller from overwriting
    /// the guide's own instruction / status / detection text panels.
    /// Pass true on InitGuide(), false on OnReturn().
    /// </summary>
    public void SetInstructionSilence(bool silent)
    {
        _silenceInstructions = silent;

        // Hide or restore the root panel for each shared TMP field
        SetPanelActive(instructionText,   !silent);
        SetPanelActive(operationInfoText, !silent);
        SetPanelActive(detectionText,     !silent);
        SetPanelActive(statusText,        !silent);
    }

    /// <summary>
    /// Walks up to the root panel (grandparent of the TMP) and sets its
    /// active state — same two-level walk used in ARStackLessonGuide.
    /// </summary>
    void SetPanelActive(TextMeshProUGUI tmp, bool active)
    {
        if (tmp == null) return;
        Transform t = tmp.transform.parent;          // Panel
        GameObject root = (t != null && t.parent != null)
            ? t.parent.gameObject                    // RootPanel
            : (t != null ? t.gameObject : tmp.gameObject);
        if (root != null) root.SetActive(active);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  INSTRUCTION TEXT HELPERS  — all gated by _silenceInstructions
    // ─────────────────────────────────────────────────────────────────────────
    void UpdateInstructions(string msg)
    {
        if (_silenceInstructions) return;
        SetInstructionColor(msg, Color.white);
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

    void SetInstructionColor(string msg, Color color)
    {
        if (instructionText == null) return;
        instructionText.text  = msg;
        instructionText.color = color;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SERVER RESPONSE MODEL
    // ─────────────────────────────────────────────────────────────────────────
    [Serializable]
    private class ScenarioResponse
    {
        public bool     success;
        public string[] scenarios;
    }

    static readonly Color BASE_BTN_COLOR   = new Color(0x84 / 255f, 0x69 / 255f, 0xFF / 255f, 1f);
    static readonly Color LIGHT_BTN_COLOR  = new Color(0xB2 / 255f, 0xA0 / 255f, 0xFF / 255f, 1f);
    static readonly Color LOCKED_BTN_COLOR = new Color(0.55f, 0.55f, 0.55f, 0.7f);

    private class StackItem
    {
        public GameObject gameObject;
        public int        stackPosition;
        public string     itemId;
        public GameObject numberLabel;
    }

    private List<StackItem> itemStack = new List<StackItem>();

    private GameObject stackScene;
    private GameObject baseStand;
    private GameObject topMarker;
    private GameObject targetPositionIndicator;

    private GameObject movingItem;
    private Vector3    targetPosition;
    private bool       isPushMode = false;
    private Vector3    currentMovementDirection = Vector3.zero;
    private bool       isMoving = false;
    private bool       hasShownMovementTutorial = false;

    private Vector3 originalItemScale = Vector3.one;

    private bool sceneSpawned  = false;
    private int  itemIdCounter = 1;

    private Color[] plateColors =
    {
        new Color(1f,    1f,    1f),
        new Color(0.9f,  0.85f, 0.7f),
        new Color(0.8f,  0.9f,  1f),
        new Color(1f,    0.9f,  0.8f),
        new Color(0.9f,  1f,    0.9f),
        new Color(1f,    0.95f, 0.8f),
        new Color(0.95f, 0.8f,  0.9f),
        new Color(0.85f, 0.95f, 1f)
    };

    private string[] boxLabels =
        { "FRAGILE", "HEAVY", "THIS SIDE UP", "TOOLS", "BOOKS", "PARTS", "STOCK", "MISC" };

    private Color[] potColors =
    {
        new Color(0.20f, 0.20f, 0.22f),
        new Color(0.72f, 0.15f, 0.10f),
        new Color(0.10f, 0.30f, 0.55f),
        new Color(0.65f, 0.65f, 0.60f),
        new Color(0.15f, 0.45f, 0.25f),
        new Color(0.80f, 0.55f, 0.10f),
        new Color(0.25f, 0.25f, 0.28f),
        new Color(0.85f, 0.30f, 0.20f),
    };
    private string[] potLabels = { "SOUP POT", "FRY PAN", "SAUCEPAN", "STOCK POT", "WOK", "CASSEROLE", "SAUTE PAN", "DUTCH OVEN" };

    // ─────────────────────────────────────────────────────────────────────────
    //  ZOOM HELPERS
    // ─────────────────────────────────────────────────────────────────────────
    float ZoomAdjustedMoveSpeed()
    {
        if (zoomController == null || !zoomController.IsInitialized()) return moveSpeed;
        float s = zoomController.GetCurrentScale();
        if (s < 0.01f) s = 0.01f;
        return moveSpeed * s;
    }

    float ZoomAdjustedConfirmThreshold() => confirmDistanceThreshold;

    // ─────────────────────────────────────────────────────────────────────────
    //  PRE-CACHE BUTTON COLORS
    // ─────────────────────────────────────────────────────────────────────────
    void CacheAllButtonColors(bool forceOverwrite = false)
    {
        CacheButtonColor(beginnerPushButton,         forceOverwrite);
        CacheButtonColor(beginnerPopButton,          forceOverwrite);
        CacheButtonColor(beginnerPeekButton,         forceOverwrite);
        CacheButtonColor(intermediatePushButton,     forceOverwrite);
        CacheButtonColor(intermediatePopButton,      forceOverwrite);
        CacheButtonColor(intermediateMultiPopButton, forceOverwrite);
        CacheButtonColor(intermediateReverseButton,  forceOverwrite);
    }

    void CacheButtonColor(UnityEngine.UI.Button btn, bool forceOverwrite = false)
    {
        if (btn == null) return;
        var img = btn.GetComponent<UnityEngine.UI.Image>();
        if (img == null) return;
        if (forceOverwrite || !originalButtonColors.ContainsKey(btn))
        {
            Color c = img.color;
            bool looksLocked = (c.r < 0.7f && c.g < 0.7f && c.b < 0.7f);
            originalButtonColors[btn] = looksLocked ? BASE_BTN_COLOR : c;
        }
        img.color = originalButtonColors[btn];
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  BUTTON LOCK / UNLOCK
    // ─────────────────────────────────────────────────────────────────────────
    void CheckAndUpdateButtonStates()
    {
        bool hasItems = itemStack.Count > 0;
        SetButtonInteractable(beginnerPopButton,          hasItems);
        SetButtonInteractable(beginnerPeekButton,         hasItems);
        SetButtonInteractable(intermediatePopButton,      hasItems);
        SetButtonInteractable(intermediateMultiPopButton, hasItems);
        SetButtonInteractable(intermediateReverseButton,  hasItems);

        if (!hasItems)
        {
            if (pulseCoroutine == null && sceneSpawned)
                pulseCoroutine = StartCoroutine(PulsePushButton());
        }
        else
        {
            if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
            ResetPushButtonVisual();
        }
    }

    void SetButtonInteractable(UnityEngine.UI.Button btn, bool state)
    {
        if (btn == null) return;
        var img = btn.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            Color target = state
                ? (originalButtonColors.ContainsKey(btn) ? originalButtonColors[btn] : BASE_BTN_COLOR)
                : LOCKED_BTN_COLOR;
            img.color = target;
        }
        btn.interactable = state;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PUSH BUTTON PULSE
    // ─────────────────────────────────────────────────────────────────────────
    IEnumerator PulsePushButton()
    {
        float speed = 2.5f, minScale = 0.92f, maxScale = 1.10f, elapsed = 0f;
        while (true)
        {
            elapsed += Time.deltaTime * speed;
            float t     = (Mathf.Sin(elapsed) + 1f) * 0.5f;
            float scale = Mathf.Lerp(minScale, maxScale, t);
            Color col   = Color.Lerp(BASE_BTN_COLOR, LIGHT_BTN_COLOR, t);
            ApplyPulse(beginnerPushButton,     scale, col);
            ApplyPulse(intermediatePushButton, scale, col);
            yield return null;
        }
    }

    public void PreFillForLesson(int count)
    {
        if (stackScene == null || !sceneSpawned) return;
        count = Mathf.Clamp(count, 1, maxStackSize);

        int toAdd = count - itemStack.Count;
        if (toAdd <= 0) return;

        for (int i = 0; i < toAdd; i++)
        {
            int colorIndex = (itemIdCounter - 1) % 8;

            GameObject itemObj = CreateItemForCurrentScenario(colorIndex);
            itemObj.transform.SetParent(stackScene.transform);
            itemObj.transform.localScale    = originalItemScale;
            itemObj.transform.localRotation = Quaternion.identity;

            float posY = GetStackBaseY() + itemStack.Count * ItemHeight;
            itemObj.transform.localPosition = new Vector3(0, posY, 0);

            float labelOffsetY = currentScenario switch
            {
                Scenario.Warehouse => boxHeight * 0.5f + 0.01f,
                Scenario.Kitchen   => potHeight + 0.02f,
                _                  => plateHeight * 8f
            };

            StackItem newItem = new StackItem
            {
                gameObject    = itemObj,
                stackPosition = itemStack.Count,
                itemId        = $"I{itemIdCounter++}"
            };
            newItem.numberLabel = CreateTextLabel(itemObj.transform,
                $"#{itemStack.Count + 1}",
                new Vector3(0, labelOffsetY, 0),
                Color.white, 60, new Vector3(0.002f, 0.002f, 0.002f));

            itemStack.Add(newItem);
        }

        UpdateTopMarkerPosition();
        CheckAndUpdateButtonStates();
        UpdateStatus();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  GUIDE / ASSESSMENT NOTIFICATION RELAYS
    // ─────────────────────────────────────────────────────────────────────────
    public void NotifyGuideOfPush()
    {
        if (cachedGuide      != null) cachedGuide.OnPushConfirmed();
        if (cachedAssessment != null) cachedAssessment.NotifyPush();
    }

    public void NotifyGuideOfPop()
    {
        if (cachedGuide      != null) cachedGuide.OnPopConfirmed();
        if (cachedAssessment != null) cachedAssessment.NotifyPop();
    }

    public void NotifyGuideOfPeek()
    {
        if (cachedGuide      != null) cachedGuide.NotifyPeekPerformed();
        if (cachedAssessment != null) cachedAssessment.NotifyPeek();
    }

    public void NotifyGuideOfMultiPop(int count)
    {
        if (cachedGuide      != null) cachedGuide.NotifyMultiPopPerformed(count);
        if (cachedAssessment != null) cachedAssessment.NotifyMultiPop(count);
    }

    public void NotifyGuideOfReverse()
    {
        if (cachedGuide      != null) cachedGuide.NotifyReversePerformed();
        if (cachedAssessment != null) cachedAssessment.NotifyReverse();
    }

    void ApplyPulse(UnityEngine.UI.Button btn, float scale, Color col)
    {
        if (btn == null || !btn.gameObject.activeInHierarchy) return;
        btn.transform.localScale = new Vector3(scale, scale, 1f);
        var img = btn.GetComponent<UnityEngine.UI.Image>();
        if (img != null) img.color = col;
    }

    void ResetPushButtonVisual()
    {
        ResetPulseBtn(beginnerPushButton);
        ResetPulseBtn(intermediatePushButton);
    }

    void ResetPulseBtn(UnityEngine.UI.Button btn)
    {
        if (btn == null) return;
        btn.transform.localScale = Vector3.one;
        var img = btn.GetComponent<UnityEngine.UI.Image>();
        if (img != null) img.color = BASE_BTN_COLOR;
    }

    private bool _started = false;

    // ─────────────────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (_started) return;
        _started = true;

        audioSource = gameObject.AddComponent<AudioSource>();
        cachedGuide      = FindObjectOfType<ARStackLessonGuide>();
        cachedAssessment = FindObjectOfType<ARStackLessonAssessment>();
        Debug.Log($"[ISP] Guide: {(cachedGuide != null ? "FOUND" : "NULL")}, Assessment: {(cachedAssessment != null ? "FOUND" : "NULL")}");

        audioSource.playOnAwake  = false;
        audioSource.spatialBlend = 0f;

        if (arCamera == null) arCamera = Camera.main;
        if (planeManager != null && planePrefab != null)
            planeManager.planePrefab = planePrefab;

        if (planeManager   != null) planeManager.enabled   = false;
        if (raycastManager != null) raycastManager.enabled = false;

        CacheAllButtonColors(forceOverwrite: false);
        HideAllPanels();
        SetupMovementButtons();

        StartCoroutine(LoadActiveScenariosAndShowPanel());
    }

    void Update()
    {
        if (currentState == StackState.WaitingForPlane)
            DetectPlaneInteraction();
        else if (currentState == StackState.PushingItem || currentState == StackState.PoppingItem)
        {
            if (isMoving && movingItem != null) MoveItemContinuous();
            CheckConfirmDistance();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SERVER — FETCH ACTIVE SCENARIOS
    // ─────────────────────────────────────────────────────────────────────────
    IEnumerator LoadActiveScenariosAndShowPanel()
    {
        string url = $"{serverUrl}/api/scenarios/active?ds={dataStructure}";
        Debug.Log($"[InteractiveStackPlates] Fetching active scenarios: {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    ScenarioResponse resp = JsonUtility.FromJson<ScenarioResponse>(req.downloadHandler.text);
                    if (resp != null && resp.success && resp.scenarios != null && resp.scenarios.Length > 0)
                    {
                        activeScenarioIds = resp.scenarios;
                        Debug.Log($"[InteractiveStackPlates] Server: [{string.Join(", ", activeScenarioIds)}]");
                    }
                    else
                    {
                        Debug.LogWarning("[InteractiveStackPlates] Bad server response — using fallback.");
                        activeScenarioIds = fallbackScenarios;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[InteractiveStackPlates] Parse error — fallback. {ex.Message}");
                    activeScenarioIds = fallbackScenarios;
                }
            }
            else
            {
                Debug.LogWarning($"[InteractiveStackPlates] Network error ({req.error}) — fallback.");
                activeScenarioIds = fallbackScenarios;
            }
        }

        ApplyScenarioButtonVisibility();
        ShowScenarioPanel();
    }

    void ApplyScenarioButtonVisibility()
    {
        ApplyButtonVisibility(btnScenarioPlates,    "Plates");
        ApplyButtonVisibility(btnScenarioWarehouse, "Warehouse");
        ApplyButtonVisibility(btnScenarioKitchen,   "Kitchen");
    }

    void ApplyButtonVisibility(UnityEngine.UI.Button btn, string scenarioId)
    {
        if (btn == null) return;
        bool isActive = Array.IndexOf(activeScenarioIds, scenarioId) >= 0;
        btn.gameObject.SetActive(isActive);
        Debug.Log($"[InteractiveStackPlates] Button '{scenarioId}' -> {(isActive ? "VISIBLE" : "HIDDEN")}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PANEL HELPERS
    // ─────────────────────────────────────────────────────────────────────────
    void HideAllPanels()
    {
        SetActive(scenarioPanel,           false);
        SetActive(difficultyPanel,         false);
        SetActive(beginnerButtonPanel,     false);
        SetActive(intermediateButtonPanel, false);
        SetActive(movementControlPanel,    false);
        SetActive(confirmButton,           false);
        SetActive(explanationPanel,        false);
    }

    void RefreshModePanel()
    {
        SetActive(beginnerButtonPanel,     currentDifficulty == Difficulty.Beginner);
        SetActive(intermediateButtonPanel, currentDifficulty == Difficulty.Intermediate);
        CheckAndUpdateButtonStates();
    }

    void SwitchToMovementUI()
    {
        SetActive(beginnerButtonPanel,     false);
        SetActive(intermediateButtonPanel, false);
        SetActive(movementControlPanel,    true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SCENARIO PANEL
    // ─────────────────────────────────────────────────────────────────────────
    void ShowScenarioPanel()
    {
        currentState = StackState.ChoosingScenario;
        HideAllPanels();
        SetActive(scenarioPanel, true);

        ApplyScenarioButtonVisibility();

        if (scenarioTitleText != null)
            scenarioTitleText.text = "STACK\nChoose Your World";

        UpdateInstructions("Pick a scenario to get started!");

        if (detectionText != null && !_silenceInstructions)
        { detectionText.text = "Choose a scenario first"; detectionText.color = Color.white; }

        if (btnScenarioPlates != null)
        {
            btnScenarioPlates.onClick.RemoveAllListeners();
            btnScenarioPlates.onClick.AddListener(() => OnScenarioChosen(Scenario.Plates));
        }
        if (btnScenarioWarehouse != null)
        {
            btnScenarioWarehouse.onClick.RemoveAllListeners();
            btnScenarioWarehouse.onClick.AddListener(() => OnScenarioChosen(Scenario.Warehouse));
        }
        if (btnScenarioKitchen != null)
        {
            btnScenarioKitchen.onClick.RemoveAllListeners();
            btnScenarioKitchen.onClick.AddListener(() => OnScenarioChosen(Scenario.Kitchen));
        }
    }

    public void OnScenarioChosen(Scenario chosen)
    {
        currentScenario = chosen;
        SetActive(scenarioPanel, false);
        ShowDifficultyPanel();
    }

    public void OnScenarioPlates()    => OnScenarioChosen(Scenario.Plates);
    public void OnScenarioWarehouse() => OnScenarioChosen(Scenario.Warehouse);
    public void OnScenarioKitchen()   => OnScenarioChosen(Scenario.Kitchen);

    // ─────────────────────────────────────────────────────────────────────────
    //  DIFFICULTY PANEL
    // ─────────────────────────────────────────────────────────────────────────
    void ShowDifficultyPanel()
    {
        currentState = StackState.ChoosingDifficulty;
        HideAllPanels();
        SetActive(difficultyPanel, true);

        string sceneName = currentScenario switch
        {
            Scenario.Warehouse => "Warehouse",
            Scenario.Kitchen   => "Kitchen",
            _                  => "Plates"
        };
        if (difficultyTitleText != null)
            difficultyTitleText.text = $"STACK — {sceneName}\nChoose Difficulty";

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

        currentState = StackState.WaitingForPlane;
        UpdateInstructions("Point camera at a flat surface and tap to place!");

        if (detectionText != null && !_silenceInstructions)
        { detectionText.text = "Looking for surfaces..."; detectionText.color = Color.yellow; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  AR PLANE TAP
    // ─────────────────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────────
    //  SPAWN SCENE
    // ─────────────────────────────────────────────────────────────────────────
    void SpawnScene(Vector3 position, Quaternion rotation)
    {
        sceneSpawned = true;
        PlaySound(placeSceneSound);

        if (planeManager != null)
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);

        stackScene = new GameObject("StackScene");
        stackScene.transform.position = position + Vector3.up * sceneHeightOffset;
        stackScene.transform.rotation = rotation;

        if (swipeRotation  != null) swipeRotation.InitializeRotation(stackScene.transform);
        if (zoomController != null) zoomController.InitializeZoom(stackScene.transform);

        // Only write to detectionText when not silenced
        if (detectionText != null && !_silenceInstructions)
        { detectionText.text = "Scene Placed!"; detectionText.color = Color.green; }

        BuildScene();
        StartGame();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  BUILD SCENE
    // ─────────────────────────────────────────────────────────────────────────
    void BuildScene()
    {
        if (currentScenario == Scenario.Warehouse)
        {
            if (arCamera != null)
            {
                Vector3 toCamera = arCamera.transform.position - stackScene.transform.position;
                toCamera.y = 0f;
                if (toCamera.sqrMagnitude > 0.01f)
                    stackScene.transform.rotation =
                        Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
            }
            if (swipeRotation != null) swipeRotation.InitializeRotation(stackScene.transform);
            BuildWarehouseScene();
        }
        else if (currentScenario == Scenario.Kitchen)
        {
            if (arCamera != null)
            {
                Vector3 toCamera = arCamera.transform.position - stackScene.transform.position;
                toCamera.y = 0f;
                if (toCamera.sqrMagnitude > 0.01f)
                    stackScene.transform.rotation =
                        Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
            }
            if (swipeRotation != null) swipeRotation.InitializeRotation(stackScene.transform);
            BuildKitchenScene();
        }
        else
        {
            BuildPlatesScene();
        }

        CreateTopMarker();
        AddInitialItems();
    }

    // ── PLATES SCENE ─────────────────────────────────────────────────────────
    void BuildPlatesScene()
    {
        GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        table.name = "Table";
        table.transform.SetParent(stackScene.transform);
        table.transform.localPosition = new Vector3(0, -0.02f, 0);
        table.transform.localScale    = new Vector3(0.3f, 0.02f, 0.3f);
        table.transform.localRotation = Quaternion.identity;
        SetColor(table, new Color(0.55f, 0.27f, 0.07f), 0.2f);
        Destroy(table.GetComponent<Collider>());

        baseStand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseStand.name = "BaseStand";
        baseStand.transform.SetParent(stackScene.transform);
        baseStand.transform.localPosition = new Vector3(0, 0.005f, 0);
        baseStand.transform.localScale    = new Vector3(0.08f, 0.01f, 0.08f);
        baseStand.transform.localRotation = Quaternion.identity;
        SetColor(baseStand, new Color(0.7f, 0.7f, 0.7f), 0.1f);
        Destroy(baseStand.GetComponent<Collider>());
        CreateTextLabel(baseStand.transform, "BASE", new Vector3(0, 0.05f, 0.12f),
            Color.white, 40, new Vector3(0.002f, 0.002f, 0.002f));
    }

    // ── WAREHOUSE SCENE ───────────────────────────────────────────────────────
    void BuildWarehouseScene()
    {
        Color palletYellow  = new Color(0.95f, 0.78f, 0.10f);
        Color palletYellowD = new Color(0.75f, 0.60f, 0.08f);
        Color concreteGrey  = new Color(0.50f, 0.50f, 0.52f);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "FloorSlab";
        floor.transform.SetParent(stackScene.transform);
        floor.transform.localPosition = new Vector3(0, -0.008f, 0);
        floor.transform.localScale    = new Vector3(0.38f, 0.006f, 0.28f);
        SetColor(floor, concreteGrey, 0.05f);
        Destroy(floor.GetComponent<Collider>());

        MakeWoodPlank("Pallet_BottomRail",
            new Vector3(0, 0.004f, 0), new Vector3(0.30f, 0.008f, 0.22f), palletYellowD);

        float[] feetX = { -0.105f, 0f, 0.105f };
        foreach (float fx in feetX)
            MakeWoodPlank($"Pallet_Foot_{fx}",
                new Vector3(fx, 0.014f, 0), new Vector3(0.055f, 0.016f, 0.22f), palletYellow);

        float   deckY = 0.025f;
        float[] deckZ = { -0.088f, -0.044f, 0f, 0.044f, 0.088f };
        foreach (float dz in deckZ)
            MakeWoodPlank("Pallet_Deck",
                new Vector3(0, deckY, dz), new Vector3(0.30f, 0.009f, 0.033f), palletYellow);

        MakeWoodPlank("Pallet_FrontTrim", new Vector3(0, deckY, -0.108f), new Vector3(0.30f, 0.009f, 0.008f), palletYellowD);
        MakeWoodPlank("Pallet_BackTrim",  new Vector3(0, deckY,  0.108f), new Vector3(0.30f, 0.009f, 0.008f), palletYellowD);

        CreateTextLabel(stackScene.transform, "PALLET",
            new Vector3(0, 0.012f, -0.115f), palletYellowD, 55, new Vector3(0.005f, 0.005f, 0.005f));

        baseStand = new GameObject("BaseStand");
        baseStand.transform.SetParent(stackScene.transform);
        baseStand.transform.localPosition = new Vector3(0, PalletTopY, 0);
    }

    // ── KITCHEN SCENE ─────────────────────────────────────────────────────────
    void BuildKitchenScene()
    {
        Color stoveBody   = new Color(0.18f, 0.18f, 0.20f);
        Color stoveAccent = new Color(0.30f, 0.30f, 0.32f);
        Color knobColor   = new Color(0.85f, 0.85f, 0.80f);
        Color burnerColor = new Color(0.25f, 0.25f, 0.28f);
        Color grillColor  = new Color(0.15f, 0.15f, 0.17f);

        GameObject stove = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stove.name = "Stove";
        stove.transform.SetParent(stackScene.transform);
        stove.transform.localPosition = new Vector3(0, -0.01f, 0);
        stove.transform.localScale    = new Vector3(0.36f, 0.06f, 0.28f);
        stove.transform.localRotation = Quaternion.identity;
        SetColor(stove, stoveBody, 0.4f);
        Destroy(stove.GetComponent<Collider>());

        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.name = "StoveTop";
        top.transform.SetParent(stackScene.transform);
        top.transform.localPosition = new Vector3(0, 0.022f, 0);
        top.transform.localScale    = new Vector3(0.35f, 0.005f, 0.27f);
        SetColor(top, stoveAccent, 0.6f);
        Destroy(top.GetComponent<Collider>());

        GameObject burnerBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        burnerBase.name = "BurnerBase";
        burnerBase.transform.SetParent(stackScene.transform);
        burnerBase.transform.localPosition = new Vector3(0, 0.025f, 0);
        burnerBase.transform.localScale    = new Vector3(0.12f, 0.003f, 0.12f);
        SetColor(burnerBase, burnerColor, 0.3f);
        Destroy(burnerBase.GetComponent<Collider>());

        float[] armAngles = { 0f, 90f, 45f, 135f };
        foreach (float ang in armAngles)
        {
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "GrillArm";
            arm.transform.SetParent(stackScene.transform);
            arm.transform.localPosition = new Vector3(0, 0.0285f, 0);
            arm.transform.localScale    = new Vector3(0.10f, 0.004f, 0.018f);
            arm.transform.localRotation = Quaternion.Euler(0, ang, 0);
            SetColor(arm, grillColor, 0.1f);
            Destroy(arm.GetComponent<Collider>());
        }

        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "ControlPanel";
        panel.transform.SetParent(stackScene.transform);
        panel.transform.localPosition = new Vector3(0, 0.022f, -0.145f);
        panel.transform.localScale    = new Vector3(0.36f, 0.035f, 0.015f);
        SetColor(panel, new Color(0.22f, 0.22f, 0.24f), 0.3f);
        Destroy(panel.GetComponent<Collider>());

        float[] knobX = { -0.10f, 0f, 0.10f };
        foreach (float kx in knobX)
        {
            GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            knob.name = "Knob";
            knob.transform.SetParent(stackScene.transform);
            knob.transform.localPosition = new Vector3(kx, 0.027f, -0.148f);
            knob.transform.localScale    = new Vector3(0.018f, 0.006f, 0.018f);
            knob.transform.localRotation = Quaternion.Euler(90, 0, 0);
            SetColor(knob, knobColor, 0.4f);
            Destroy(knob.GetComponent<Collider>());
        }

        GameObject backsplash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backsplash.name = "Backsplash";
        backsplash.transform.SetParent(stackScene.transform);
        backsplash.transform.localPosition = new Vector3(0, 0.055f, 0.140f);
        backsplash.transform.localScale    = new Vector3(0.36f, 0.06f, 0.012f);
        SetColor(backsplash, new Color(0.88f, 0.88f, 0.85f), 0.2f);
        Destroy(backsplash.GetComponent<Collider>());

        CreateTextLabel(stackScene.transform, "BURNER",
            new Vector3(0, 0.018f, -0.075f),
            new Color(0.6f, 0.6f, 0.6f), 40, new Vector3(0.003f, 0.003f, 0.003f));

        baseStand = new GameObject("BaseStand");
        baseStand.transform.SetParent(stackScene.transform);
        baseStand.transform.localPosition = new Vector3(0, 0.032f, 0);
    }

    GameObject MakeWoodPlank(string name, Vector3 localPos, Vector3 localScale, Color color)
    {
        GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plank.name = name;
        plank.transform.SetParent(stackScene.transform);
        plank.transform.localPosition = localPos;
        plank.transform.localRotation = Quaternion.identity;
        plank.transform.localScale    = localScale;
        SetColor(plank, color, 0.1f);
        Destroy(plank.GetComponent<Collider>());
        return plank;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SCENE HEIGHT HELPERS
    // ─────────────────────────────────────────────────────────────────────────
    float PalletTopY  => currentScenario == Scenario.Warehouse ? 0.030f : 0f;
    float KitchenTopY => currentScenario == Scenario.Kitchen ? 0.032f : 0f;

    float ItemHeight => currentScenario switch
    {
        Scenario.Warehouse => boxHeight,
        Scenario.Kitchen   => potHeight,
        _                  => plateHeight
    };

    float GetStackBaseY() => currentScenario switch
    {
        Scenario.Warehouse => PalletTopY + 0.005f,
        Scenario.Kitchen   => 0.032f,
        _                  => 0.005f
    };

    float GetStackTopY() => GetStackBaseY() + itemStack.Count * ItemHeight;

    // ─────────────────────────────────────────────────────────────────────────
    //  TOP MARKER
    // ─────────────────────────────────────────────────────────────────────────
    void CreateTopMarker()
    {
        topMarker = new GameObject("TopMarker");
        topMarker.transform.SetParent(stackScene.transform);
        UpdateTopMarkerPosition();

        GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arrow.transform.SetParent(topMarker.transform);
        arrow.transform.localPosition = new Vector3(0, -0.02f, 0);
        arrow.transform.localRotation = Quaternion.Euler(45, 0, 0);
        arrow.transform.localScale    = new Vector3(0.05f, 0.02f, 0.05f);
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(1, 0.5f, 0, 0.8f);
        arrow.GetComponent<Renderer>().material = mat;
        Destroy(arrow.GetComponent<Collider>());

        CreateTextLabel(topMarker.transform, "TOP", new Vector3(0, 0.08f, 0),
            new Color(1, 0.5f, 0), 50, new Vector3(0.002f, 0.002f, 0.002f));

        StartCoroutine(BobTopMarker());
    }

    IEnumerator BobTopMarker()
    {
        while (topMarker != null)
        {
            float offset = Mathf.Sin(Time.time * 2f) * 0.015f;
            topMarker.transform.localPosition = new Vector3(0, GetStackTopY() + 0.08f + offset, 0);
            yield return null;
        }
    }

    void UpdateTopMarkerPosition()
    {
        if (topMarker != null)
            topMarker.transform.localPosition = new Vector3(0, GetStackTopY() + 0.08f, 0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  INITIAL ITEMS — stack starts EMPTY
    // ─────────────────────────────────────────────────────────────────────────
    void AddInitialItems()
    {
        CacheAllButtonColors(forceOverwrite: true);
        CheckAndUpdateButtonStates();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  START GAME
    // ─────────────────────────────────────────────────────────────────────────
    void StartGame()
    {
        currentState = StackState.Ready;
        SetActive(explanationPanel, true);
        RefreshModePanel();

        bool isWH = currentScenario == Scenario.Warehouse;
        bool isKT = currentScenario == Scenario.Kitchen;

        string itemName = isWH ? "box" : isKT ? "pot" : "plate";
        string verb     = isWH ? "stack" : isKT ? "stack" : "place";

        // Only write to these panels when not in guided lesson mode
        if (!_silenceInstructions)
        {
            if (currentDifficulty == Difficulty.Beginner)
            {
                UpdateInstructions($"Ready! Tap PUSH to {verb} your first {itemName}.");
                if (operationInfoText != null)
                    operationInfoText.text =
                        "BEGINNER MODE\n\n" +
                        $"PUSH - Stack a {itemName} on top\nPOP  - Remove top {itemName}\nPEEK - Check top {itemName}\n\n" +
                        "LIFO: Last In, First Out!";
            }
            else
            {
                UpdateInstructions($"Ready! Tap PUSH to {verb} your first {itemName}.");
                if (operationInfoText != null)
                    operationInfoText.text =
                        "INTERMEDIATE MODE\n\n" +
                        "PUSH       - Add to TOP        O(1)\n" +
                        "POP        - Remove from TOP   O(1)\n" +
                        "MULTI-POP  - Pop N items       O(n)\n" +
                        "REVERSE    - Flip stack        O(n)\n\n" +
                        "Think about time complexity!";
            }
        }

        if (tutorialIntegration != null)
            Invoke(nameof(ShowWelcomeTutorialDelayed), 1f);

        UpdateStatus();
    }

    void ShowWelcomeTutorialDelayed()
    {
        if (tutorialIntegration != null) tutorialIntegration.ShowWelcomeTutorial();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ITEM FACTORIES
    // ─────────────────────────────────────────────────────────────────────────
    GameObject CreateItemForCurrentScenario(int colorIndex)
    {
        return currentScenario switch
        {
            Scenario.Warehouse => CreateBox(colorIndex),
            Scenario.Kitchen   => CreatePot(colorIndex),
            _                  => CreatePlate(colorIndex)
        };
    }

    GameObject CreatePlate(int colorIndex)
    {
        if (platePrefab != null)
        {
            GameObject inst = Instantiate(platePrefab);
            originalItemScale = inst.transform.localScale;
            return inst;
        }

        GameObject plate = new GameObject($"Plate_{itemIdCounter}");
        Color col = plateColors[colorIndex % plateColors.Length];

        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.transform.SetParent(plate.transform);
        disc.transform.localPosition = new Vector3(0, plateHeight * 0.5f, 0);
        disc.transform.localScale    = new Vector3(0.12f, plateHeight, 0.12f);
        SetColor(disc, col, 0.3f);
        Destroy(disc.GetComponent<Collider>());

        GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.transform.SetParent(disc.transform);
        rim.transform.localPosition = new Vector3(0, 0.8f, 0);
        rim.transform.localScale    = new Vector3(1.1f, 0.15f, 1.1f);
        SetColor(rim, col * 0.9f, 0.1f);
        Destroy(rim.GetComponent<Collider>());

        plate.AddComponent<BoxCollider>().size = new Vector3(0.12f, plateHeight, 0.12f);
        originalItemScale = plate.transform.localScale;
        return plate;
    }

    GameObject CreateBox(int colorIndex)
    {
        GameObject box = new GameObject($"Box_{itemIdCounter}");

        Color cardboard     = new Color(0.76f, 0.60f, 0.36f);
        Color cardboardDark = new Color(0.55f, 0.42f, 0.22f);
        Color tapeColor     = new Color(0.55f, 0.70f, 0.85f);
        Color labelColor    = new Color(0.95f, 0.93f, 0.85f);

        float bW = 0.10f, bH = boxHeight, bD = 0.10f;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "BoxBody";
        body.transform.SetParent(box.transform);
        body.transform.localPosition = new Vector3(0, bH * 0.5f, 0);
        body.transform.localScale    = new Vector3(bW, bH, bD);
        SetColor(body, cardboard, 0.05f);
        Destroy(body.GetComponent<Collider>());

        for (int s = -1; s <= 1; s += 2)
        {
            GameObject crease = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crease.transform.SetParent(body.transform);
            crease.transform.localPosition = new Vector3(s * 0.25f, 0.46f, 0);
            crease.transform.localScale    = new Vector3(0.02f, 0.05f, 1.01f);
            SetColor(crease, cardboardDark, 0.0f);
            Destroy(crease.GetComponent<Collider>());
        }

        GameObject tape = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tape.transform.SetParent(body.transform);
        tape.transform.localPosition = new Vector3(0, 0.48f, 0);
        tape.transform.localScale    = new Vector3(0.18f, 0.04f, 1.02f);
        SetColor(tape, tapeColor, 0.3f);
        Destroy(tape.GetComponent<Collider>());

        GameObject frontLabel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frontLabel.transform.SetParent(body.transform);
        frontLabel.transform.localPosition = new Vector3(0, -0.05f, -0.51f);
        frontLabel.transform.localScale    = new Vector3(0.65f, 0.45f, 0.02f);
        SetColor(frontLabel, labelColor, 0.0f);
        Destroy(frontLabel.GetComponent<Collider>());

        string labelText = boxLabels[colorIndex % boxLabels.Length];
        CreateTextLabel(box.transform, labelText,
            new Vector3(0, bH * 0.45f, -bD * 0.52f),
            new Color(0.2f, 0.1f, 0.0f), 55, new Vector3(0.004f, 0.004f, 0.004f));

        GameObject bottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bottom.transform.SetParent(body.transform);
        bottom.transform.localPosition = new Vector3(0, -0.46f, 0);
        bottom.transform.localScale    = new Vector3(1.02f, 0.08f, 1.02f);
        SetColor(bottom, cardboardDark, 0.0f);
        Destroy(bottom.GetComponent<Collider>());

        BoxCollider col = box.AddComponent<BoxCollider>();
        col.center = new Vector3(0, bH * 0.5f, 0);
        col.size   = new Vector3(bW, bH, bD);

        Rigidbody rb = box.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        originalItemScale = box.transform.localScale;
        return box;
    }

    GameObject CreatePot(int colorIndex)
    {
        GameObject pot  = new GameObject($"Pot_{itemIdCounter}");
        Color potCol    = potColors[colorIndex % potColors.Length];
        Color potDark   = potCol * 0.75f;
        string potLabel = potLabels[colorIndex % potLabels.Length];
        float pH        = potHeight;
        float pR        = 0.08f;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "PotBody";
        body.transform.SetParent(pot.transform);
        body.transform.localPosition = new Vector3(0, pH * 0.5f, 0);
        body.transform.localScale    = new Vector3(pR * 2f, pH * 0.5f, pR * 2f);
        SetColor(body, potCol, 0.6f);
        Destroy(body.GetComponent<Collider>());

        GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "PotRim";
        rim.transform.SetParent(pot.transform);
        rim.transform.localPosition = new Vector3(0, pH, 0);
        rim.transform.localScale    = new Vector3(pR * 2.12f, 0.004f, pR * 2.12f);
        SetColor(rim, potDark, 0.7f);
        Destroy(rim.GetComponent<Collider>());

        GameObject bottom = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bottom.name = "PotBottom";
        bottom.transform.SetParent(pot.transform);
        bottom.transform.localPosition = new Vector3(0, 0.003f, 0);
        bottom.transform.localScale    = new Vector3(pR * 1.90f, 0.004f, pR * 1.90f);
        SetColor(bottom, potDark, 0.5f);
        Destroy(bottom.GetComponent<Collider>());

        GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lid.name = "Lid";
        lid.transform.SetParent(pot.transform);
        lid.transform.localPosition = new Vector3(0, pH + 0.005f, 0);
        lid.transform.localScale    = new Vector3(pR * 1.95f, 0.007f, pR * 1.95f);
        SetColor(lid, potCol * 0.9f, 0.7f);
        Destroy(lid.GetComponent<Collider>());

        GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        knob.name = "LidKnob";
        knob.transform.SetParent(pot.transform);
        knob.transform.localPosition = new Vector3(0, pH + 0.020f, 0);
        knob.transform.localScale    = new Vector3(0.014f, 0.014f, 0.014f);
        SetColor(knob, new Color(0.85f, 0.85f, 0.80f), 0.6f);
        Destroy(knob.GetComponent<Collider>());

        for (int side = -1; side <= 1; side += 2)
        {
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "Handle";
            handle.transform.SetParent(pot.transform);
            handle.transform.localPosition = new Vector3(side * (pR + 0.018f), pH * 0.55f, 0);
            handle.transform.localScale    = new Vector3(0.025f, 0.016f, 0.040f);
            SetColor(handle, potDark, 0.1f);
            Destroy(handle.GetComponent<Collider>());
        }

        CreateTextLabel(pot.transform, potLabel,
            new Vector3(0, pH * 0.55f, -(pR + 0.005f)),
            Color.white, 45, new Vector3(0.003f, 0.003f, 0.003f));

        BoxCollider bc = pot.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, pH * 0.5f, 0);
        bc.size   = new Vector3(pR * 2f, pH, pR * 2f);

        Rigidbody rb = pot.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        originalItemScale = pot.transform.localScale;
        return pot;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  MOVEMENT SETUP
    // ─────────────────────────────────────────────────────────────────────────
    void SetupMovementButtons()
    {
        AddHoldListeners(moveLeftButton,    Vector3.left);
        AddHoldListeners(moveRightButton,   Vector3.right);
        AddHoldListeners(moveForwardButton, Vector3.forward);
        AddHoldListeners(moveBackButton,    Vector3.back);
        if (cancelButton != null) cancelButton.onClick.AddListener(CancelMovement);
    }

    void AddHoldListeners(UnityEngine.UI.Button btn, Vector3 dir)
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

    // ─────────────────────────────────────────────────────────────────────────
    //  MOVEMENT
    // ─────────────────────────────────────────────────────────────────────────
    void MoveItemContinuous()
    {
        if (movingItem == null) return;
        if (!hasShownMovementTutorial && tutorialIntegration != null)
        {
            tutorialIntegration.OnMovementStarted();
            hasShownMovementTutorial = true;
        }
        Vector3 localDelta = currentMovementDirection.normalized
                             * ZoomAdjustedMoveSpeed()
                             * Time.deltaTime;
        movingItem.transform.localPosition += localDelta;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  CONFIRM DISTANCE
    // ─────────────────────────────────────────────────────────────────────────
    void CheckConfirmDistance()
    {
        if (movingItem == null) return;
        float dist  = Vector3.Distance(movingItem.transform.localPosition, targetPosition);
        bool  inPos = dist < ZoomAdjustedConfirmThreshold();
        SetActive(confirmButton, inPos);

        // Only write movement prompts when not in guided lesson mode
        if (!_silenceInstructions)
        {
            string itemName = GetItemName();
            if (inPos) UpdateInstructionsSuccess("Perfect! Tap CONFIRM to place");
            else       UpdateInstructions(isPushMode
                ? $"Move {itemName} to stack TOP (dist: {dist:F3})"
                : $"Move {itemName} away (dist: {dist:F3})");
        }
    }

    string GetItemName() => currentScenario switch
    {
        Scenario.Warehouse => "box",
        Scenario.Kitchen   => "pot",
        _                  => "plate"
    };

    // ─────────────────────────────────────────────────────────────────────────
    //  PUSH
    // ─────────────────────────────────────────────────────────────────────────
    public void OnPushButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnPushButtonClicked();
        if (currentState != StackState.Ready) return;
        if (itemStack.Count >= maxStackSize)
        {
            UpdateInstructionsError(currentScenario == Scenario.Warehouse
                ? "Pallet is FULL!" : currentScenario == Scenario.Kitchen
                ? "Stove is FULL!" : "Stack is FULL!");
            return;
        }

        isPushMode   = true;
        currentState = StackState.PushingItem;

        movingItem = CreateItemForCurrentScenario(itemStack.Count);
        movingItem.transform.SetParent(stackScene.transform);
        movingItem.transform.localScale    = originalItemScale;
        movingItem.transform.localRotation = Quaternion.identity;

        float spawnY = GetStackTopY() + ItemHeight * 0.5f + 0.05f;
        movingItem.transform.localPosition = new Vector3(0.22f, spawnY, 0);
        targetPosition = new Vector3(0, GetStackTopY(), 0);

        CreateTargetIndicator();
        SwitchToMovementUI();

        // Only set instructionText / operationInfoText when not silenced
        if (!_silenceInstructions)
        {
            string itemName = GetItemName();
            UpdateInstructions($"Move the {itemName} to the TOP of the stack");
            if (operationInfoText != null)
                operationInfoText.text =
                    "PUSH Operation!\n\n" +
                    $"Use arrows to move the {itemName}\n" +
                    "Place it on TOP of the stack\n\n" +
                    "New items always go on TOP!\nThis is LIFO in action!";
        }

        AudioClip clip = currentScenario switch
        {
            Scenario.Warehouse => boxPlaceSound ?? platePlaceSound,
            Scenario.Kitchen   => potPlaceSound ?? platePlaceSound,
            _                  => platePlaceSound
        };
        PlaySound(clip);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  POP
    // ─────────────────────────────────────────────────────────────────────────
    public void OnPopButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnPopButtonClicked();
        if (currentState != StackState.Ready || itemStack.Count == 0)
        {
            UpdateInstructionsError(currentScenario == Scenario.Warehouse
                ? "Pallet is EMPTY!" : currentScenario == Scenario.Kitchen
                ? "Stove is EMPTY!" : "Stack is EMPTY!");
            return;
        }

        isPushMode   = false;
        currentState = StackState.PoppingItem;

        StackItem top = itemStack[itemStack.Count - 1];
        movingItem = top.gameObject;
        itemStack.RemoveAt(itemStack.Count - 1);
        CheckAndUpdateButtonStates();

        targetPosition = new Vector3(0.25f, movingItem.transform.localPosition.y, 0);
        CreateExitIndicator();
        SwitchToMovementUI();

        // Only set instructionText / operationInfoText when not silenced
        if (!_silenceInstructions)
        {
            string itemName = GetItemName();
            UpdateInstructions($"Move the TOP {itemName} away from stack");
            if (operationInfoText != null)
                operationInfoText.text =
                    "POP Operation!\n\n" +
                    $"Use arrows to remove the {itemName}\n" +
                    "Move it away from the stack\n\n" +
                    "Can only remove from TOP!\nThis is how stacks work!";
        }

        AudioClip clip = currentScenario switch
        {
            Scenario.Warehouse => boxRemoveSound ?? plateRemoveSound,
            Scenario.Kitchen   => potRemoveSound ?? plateRemoveSound,
            _                  => plateRemoveSound
        };
        PlaySound(clip);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PEEK
    // ─────────────────────────────────────────────────────────────────────────
    public void OnPeekButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnPeekButtonClicked();
        if (currentState != StackState.Ready || itemStack.Count == 0)
        {
            UpdateInstructionsError("Stack is EMPTY!");
            return;
        }
        PlaySound(peekSound);
        StartCoroutine(PeekTopItem());
    }

    IEnumerator PeekTopItem()
    {
        StackItem top      = itemStack[itemStack.Count - 1];
        string    itemName = GetItemName();
        float     offsetY  = currentScenario switch
        {
            Scenario.Warehouse => boxHeight * 0.5f,
            Scenario.Kitchen   => potHeight * 0.5f,
            _                  => 0f
        };

        NotifyGuideOfPeek();

        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        highlight.transform.SetParent(top.gameObject.transform);
        highlight.transform.localPosition = new Vector3(0, offsetY, 0);
        highlight.transform.localScale    = Vector3.one * 2f;
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0, 1, 1, 0.3f);
        highlight.GetComponent<Renderer>().material = mat;
        Destroy(highlight.GetComponent<Collider>());

        // Only write success text when not silenced —
        // the guide's NotifyPeekPerformed() handles its own instructionText update
        if (!_silenceInstructions)
        {
            UpdateInstructionsSuccess($"PEEK: Top {itemName} is #{itemStack.Count}");
            if (operationInfoText != null)
                operationInfoText.text =
                    "PEEK Complete!\n\n" +
                    $"Viewed TOP {itemName} (cyan glow)\n" +
                    "Stack unchanged - Read-only!\n\n" +
                    "Time Complexity: O(1)\n\nCheck top without removing it!";
        }

        yield return new WaitForSeconds(2.5f);
        Destroy(highlight);

        if (!_silenceInstructions)
            UpdateInstructions("What would you like to do next?");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  MULTI-POP  (Intermediate)
    // ─────────────────────────────────────────────────────────────────────────
    public void OnMultiPopButton()
    {
        if (currentState != StackState.Ready) return;
        if (itemStack.Count == 0) { UpdateInstructionsError("Stack is EMPTY!"); return; }

        int n = 1;
        if (multiPopInputField != null && !string.IsNullOrEmpty(multiPopInputField.text))
            int.TryParse(multiPopInputField.text, out n);
        n = Mathf.Clamp(n, 1, itemStack.Count);

        currentState = StackState.MultiPopping;
        StartCoroutine(MultiPopCoroutine(n));
    }

    IEnumerator MultiPopCoroutine(int count)
    {
        string itemName = GetItemName();

        if (!_silenceInstructions && operationInfoText != null)
            operationInfoText.text =
                $"MULTI-POP  O(n)\n\nPopping {count} {itemName}(es)...\n\n" +
                $"Each pop is O(1)\n{count} pops = O({count}) total";

        for (int i = 0; i < count; i++)
        {
            if (itemStack.Count == 0) break;
            StackItem top = itemStack[itemStack.Count - 1];
            itemStack.RemoveAt(itemStack.Count - 1);

            if (!_silenceInstructions)
                UpdateInstructions($"Multi-Pop: removing {itemName} {i + 1} of {count}...");

            AudioClip clip = currentScenario switch
            {
                Scenario.Warehouse => boxRemoveSound ?? plateRemoveSound,
                Scenario.Kitchen   => potRemoveSound ?? plateRemoveSound,
                _                  => plateRemoveSound
            };
            PlaySound(clip);

            yield return StartCoroutine(AnimateItemOut(top.gameObject));
            UpdateTopMarkerPosition();
            UpdateStatus();
            CheckAndUpdateButtonStates();
            yield return new WaitForSeconds(0.15f);
        }

        if (!_silenceInstructions)
        {
            if (operationInfoText != null)
                operationInfoText.text =
                    $"MULTI-POP Complete!\n\nRemoved {count} {itemName}(es)\n" +
                    $"Stack Size: {itemStack.Count}/{maxStackSize}\n\n" +
                    $"Time Complexity: O({count})\n\nEach pop is O(1) but\n{count} of them = O(n)!";
            UpdateInstructionsSuccess($"Multi-Pop done! Removed {count} {GetItemName()}(es).");
        }

        NotifyGuideOfMultiPop(count);

        currentState = StackState.Ready;
        RefreshModePanel();
    }

    IEnumerator AnimateItemOut(GameObject itemObj)
    {
        float elapsed = 0f, duration = 0.35f;
        Vector3 start = itemObj.transform.localPosition;
        Vector3 end   = start + new Vector3(0.3f, 0.1f, 0);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            itemObj.transform.localPosition = Vector3.Lerp(start, end, t);
            foreach (Renderer r in itemObj.GetComponentsInChildren<Renderer>())
            {
                Color c = r.material.color; c.a = Mathf.Lerp(1f, 0f, t);
                r.material.color = c;
            }
            yield return null;
        }
        Destroy(itemObj);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  REVERSE STACK  (Intermediate)
    // ─────────────────────────────────────────────────────────────────────────
    public void OnReverseStackButton()
    {
        if (currentState != StackState.Ready) return;
        if (itemStack.Count <= 1)
        {
            UpdateInstructionsError("Need at least 2 items to reverse!");
            return;
        }
        currentState = StackState.ReversingStack;
        StartCoroutine(ReverseStackCoroutine());
    }

    IEnumerator ReverseStackCoroutine()
    {
        string itemsName = currentScenario switch
        {
            Scenario.Warehouse => "boxes",
            Scenario.Kitchen   => "pots",
            _                  => "plates"
        };
        int total = itemStack.Count;

        if (!_silenceInstructions)
        {
            UpdateInstructions("Reversing Stack...");
            if (operationInfoText != null)
                operationInfoText.text =
                    "REVERSE  O(n)\n\nStep 1: Pop all to temp stack\n\n" +
                    "Step 2: Pop temp back\n= reversed order!\n\nTime Complexity: O(n)";
        }

        float stageX = 0.22f;
        for (int i = itemStack.Count - 1; i >= 0; i--)
        {
            GameObject go     = itemStack[i].gameObject;
            float      stageY = GetStackBaseY() + (itemStack.Count - 1 - i) * ItemHeight;
            yield return StartCoroutine(SmoothMove(go, new Vector3(stageX, stageY, 0), 0.3f));

            if (!_silenceInstructions)
                UpdateInstructions($"Moving {itemsName[..^1]} {total - i} of {total} to temp area...");

            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.4f);
        itemStack.Reverse();

        for (int i = 0; i < itemStack.Count; i++)
        {
            itemStack[i].stackPosition = i;
            Vector3 newPos = new Vector3(0, GetStackBaseY() + i * ItemHeight, 0);
            yield return StartCoroutine(SmoothMove(itemStack[i].gameObject, newPos, 0.3f));

            if (itemStack[i].numberLabel != null)
            {
                TextMesh tm = itemStack[i].numberLabel.GetComponent<TextMesh>();
                if (tm != null) tm.text = $"#{i + 1}";
            }

            if (!_silenceInstructions)
                UpdateInstructions($"Placing {itemsName[..^1]} {i + 1} of {total} back...");

            yield return new WaitForSeconds(0.1f);
        }

        UpdateTopMarkerPosition();
        UpdateStatus();

        if (!_silenceInstructions)
        {
            if (operationInfoText != null)
                operationInfoText.text =
                    $"REVERSE Complete!\n\nAll {total} {itemsName} reversed!\n\n" +
                    "Time Complexity: O(n)\n\nUsed a temp stack - classic CS!";
            UpdateInstructionsSuccess($"Stack reversed! Order is now flipped.");
        }

        NotifyGuideOfReverse();

        currentState = StackState.Ready;
        RefreshModePanel();
    }

    IEnumerator SmoothMove(GameObject go, Vector3 targetLocal, float duration)
    {
        if (go == null) yield break;
        Vector3 start = go.transform.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            go.transform.localPosition = Vector3.Lerp(start, targetLocal, elapsed / duration);
            yield return null;
        }
        go.transform.localPosition = targetLocal;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  CONFIRM / CANCEL
    // ─────────────────────────────────────────────────────────────────────────
    public void OnConfirmPlacement()
    {
        if (movingItem == null) return;

        string itemName = char.ToUpper(GetItemName()[0]) + GetItemName()[1..];

        if (isPushMode)
        {
            movingItem.transform.localPosition = targetPosition;
            movingItem.transform.localRotation = Quaternion.identity;

            float labelOffsetY = currentScenario switch
            {
                Scenario.Warehouse => boxHeight * 0.5f + 0.01f,
                Scenario.Kitchen   => potHeight + 0.02f,
                _                  => plateHeight * 8f
            };

            StackItem newItem = new StackItem
            {
                gameObject    = movingItem,
                stackPosition = itemStack.Count,
                itemId        = $"I{itemIdCounter++}"
            };
            newItem.numberLabel = CreateTextLabel(movingItem.transform,
                $"#{itemStack.Count + 1}",
                new Vector3(0, labelOffsetY, 0),
                Color.white, 60, new Vector3(0.002f, 0.002f, 0.002f));
            itemStack.Add(newItem);

            NotifyGuideOfPush();

            if (!_silenceInstructions)
            {
                UpdateInstructionsSuccess($"{itemName} PUSHED! Stack size: {itemStack.Count}");
                if (operationInfoText != null)
                    operationInfoText.text =
                        $"PUSH Complete!\n\n{itemName} added to TOP\n" +
                        $"Stack Size: {itemStack.Count}/{maxStackSize}\n\n" +
                        "Time Complexity: O(1)\nLIFO: Last In, First Out!";
            }
        }
        else
        {
            Destroy(movingItem);
            NotifyGuideOfPop();

            if (!_silenceInstructions)
            {
                UpdateInstructionsSuccess($"{itemName} POPPED! Stack size: {itemStack.Count}");
                if (operationInfoText != null)
                    operationInfoText.text =
                        $"POP Complete!\n\nRemoved TOP {itemName}\n" +
                        $"Stack Size: {itemStack.Count}/{maxStackSize}\n\n" +
                        "Time Complexity: O(1)\nYou can only access the TOP!";
            }
        }

        CleanupMovement();
        UpdateTopMarkerPosition();
        UpdateStatus();
        CheckAndUpdateButtonStates();
    }

    void CancelMovement()
    {
        if (movingItem != null)
        {
            if (isPushMode)
            {
                Destroy(movingItem);
                if (!_silenceInstructions) UpdateInstructions("Push cancelled");
            }
            else
            {
                movingItem.transform.localPosition = new Vector3(0, GetStackTopY(), 0);
                movingItem.transform.localRotation = Quaternion.identity;

                float labelOffsetY = currentScenario switch
                {
                    Scenario.Warehouse => boxHeight * 0.5f + 0.01f,
                    Scenario.Kitchen   => potHeight + 0.02f,
                    _                  => plateHeight * 8f
                };
                StackItem returned = new StackItem
                {
                    gameObject    = movingItem,
                    stackPosition = itemStack.Count,
                    itemId        = $"I{itemIdCounter++}"
                };
                returned.numberLabel = CreateTextLabel(movingItem.transform,
                    $"#{itemStack.Count + 1}",
                    new Vector3(0, labelOffsetY, 0),
                    Color.white, 60, new Vector3(0.002f, 0.002f, 0.002f));
                itemStack.Add(returned);

                if (!_silenceInstructions) UpdateInstructions("Pop cancelled - item returned");
            }
        }
        CleanupMovement();
        UpdateTopMarkerPosition();
        CheckAndUpdateButtonStates();
    }

    void CleanupMovement()
    {
        if (targetPositionIndicator != null)
        { Destroy(targetPositionIndicator); targetPositionIndicator = null; }
        movingItem   = null;
        currentState = StackState.Ready;
        StopMoving();
        hasShownMovementTutorial = false;
        RefreshModePanel();
        SetActive(movementControlPanel, false);
        SetActive(confirmButton,        false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  INDICATORS
    // ─────────────────────────────────────────────────────────────────────────
    void CreateTargetIndicator()
    {
        targetPositionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        targetPositionIndicator.transform.SetParent(stackScene.transform);
        targetPositionIndicator.transform.localPosition = targetPosition;
        targetPositionIndicator.transform.localScale    = new Vector3(0.14f, 0.002f, 0.14f);
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0, 1, 0, 0.5f);
        targetPositionIndicator.GetComponent<Renderer>().material = mat;
        Destroy(targetPositionIndicator.GetComponent<Collider>());
        StartCoroutine(PulseIndicator());
    }

    void CreateExitIndicator()
    {
        targetPositionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        targetPositionIndicator.transform.SetParent(stackScene.transform);
        targetPositionIndicator.transform.localPosition = targetPosition;
        targetPositionIndicator.transform.localScale    = new Vector3(0.15f, 0.002f, 0.15f);
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(1, 0, 0, 0.6f);
        targetPositionIndicator.GetComponent<Renderer>().material = mat;
        Destroy(targetPositionIndicator.GetComponent<Collider>());
        CreateTextLabel(targetPositionIndicator.transform, "REMOVE",
            new Vector3(0, 0.05f, 0), Color.red, 40, new Vector3(0.002f, 0.002f, 0.002f));
        StartCoroutine(PulseIndicator());
    }

    IEnumerator PulseIndicator()
    {
        while (targetPositionIndicator != null)
        {
            float s = 0.14f + Mathf.Sin(Time.time * 3f) * 0.02f;
            targetPositionIndicator.transform.localScale = new Vector3(s, 0.002f, s);
            yield return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  RESET
    // ─────────────────────────────────────────────────────────────────────────
    public void OnResetButton()
    {
        if (swipeRotation       != null) swipeRotation.ResetRotation();
        if (tutorialIntegration != null) tutorialIntegration.OnResetButtonClicked();
        if (zoomController      != null) zoomController.ResetZoom();

        StopAllCoroutines();
        pulseCoroutine = null;
        ResetPushButtonVisual();

        originalButtonColors.Clear();
        CacheAllButtonColors(forceOverwrite: true);

        if (stackScene              != null) { Destroy(stackScene);              stackScene              = null; }
        if (movingItem              != null) { Destroy(movingItem);              movingItem              = null; }
        if (targetPositionIndicator != null) { Destroy(targetPositionIndicator); targetPositionIndicator = null; }
        topMarker = null;
        baseStand = null;

        itemStack.Clear();
        sceneSpawned             = false;
        itemIdCounter            = 1;
        isPushMode               = false;
        isMoving                 = false;
        currentMovementDirection = Vector3.zero;
        currentScenario          = Scenario.None;
        currentDifficulty        = Difficulty.None;
        _silenceInstructions     = false;   // always restore on reset

        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
                if (plane?.gameObject != null) plane.gameObject.SetActive(false);
        }
        if (raycastManager != null) raycastManager.enabled = false;

        HideAllPanels();
        if (statusText != null) statusText.text = "Stack: 0";

        StartCoroutine(LoadActiveScenariosAndShowPanel());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SHARED HELPERS
    // ─────────────────────────────────────────────────────────────────────────
    void SetColor(GameObject obj, Color color, float glossiness)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend == null) return;
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Glossiness", glossiness);
        rend.material = mat;
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

    void UpdateStatus()
    {
        if (statusText == null || _silenceInstructions) return;
        statusText.text = $"Stack: {itemStack.Count}/{maxStackSize}";
    }

    void PlaySound(AudioClip clip)              { if (audioSource != null && clip != null) audioSource.PlayOneShot(clip); }
    void SetActive(GameObject obj, bool active) { if (obj != null) obj.SetActive(active); }
}