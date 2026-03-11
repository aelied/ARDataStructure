using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using System.Linq;

/// <summary>
/// GRAPH — Three Scenarios + Two Difficulties
///
/// GUIDE MODE FIX:
///   SetInstructionSilence(bool silent) is called by ARGraphLessonGuide on
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
///   SetInstructionSilence(false) is called by ARGraphLessonGuide.OnReturn()
///   to restore all panels for sandbox / free-play use after the lesson ends.
/// </summary>
public class InteractiveCityGraph : MonoBehaviour
{
    [Header("AR Components")]
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;
    public Camera arCamera;

    [Header("Zoom Controller")]
    public SceneZoomController zoomController;

    [Header("Plane Visualization")]
    public GameObject planePrefab;

    [Header("Custom Assets – City Map (Optional)")]
    public GameObject[] buildingPrefabs;

    [Header("Custom Assets – Island Network (Optional)")]
    public GameObject[] islandPrefabs;
    public GameObject bridgePrefab;

    [Header("Custom Assets – Space Station (Optional)")]
    public GameObject[] stationModulePrefabs;
    public GameObject dockingTubePrefab;

    [Header("UI References")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI detectionText;
    public TextMeshProUGUI operationInfoText;
    public GameObject confirmButton;

    public GameObject mainButtonPanel;
    public GameObject movementButtonPanel;
    public GameObject inputPanel;
    public TMP_InputField inputField;
    public GameObject inputPanelEdge;
    public TMP_InputField inputFieldFrom;
    public TMP_InputField inputFieldTo;
    public GameObject algorithmPanel;
    public GameObject explanationPanel;

    [Header("Scenario Panel")]
    public GameObject scenarioPanel;
    public UnityEngine.UI.Button cityMapScenarioBtn;
    public UnityEngine.UI.Button islandNetworkScenarioBtn;
    public UnityEngine.UI.Button spaceStationScenarioBtn;

    [Header("Difficulty Panel")]
    public GameObject difficultyPanel;
    public UnityEngine.UI.Button beginnerBtn;
    public UnityEngine.UI.Button intermediateBtn;

    [Header("Mode Button Panels")]
    public GameObject beginnerButtonPanel;
    public GameObject intermediateButtonPanel;

    [Header("Intermediate UI")]
    public GameObject pathCheckInputPanel;
    public TMP_InputField pathCheckFrom;
    public TMP_InputField pathCheckTo;
    public GameObject degreeInputPanel;
    public TMP_InputField degreeInputField;

    [Header("Audio")]
    public AudioClip placeSceneSound;
    public AudioClip addNodeSound;
    public AudioClip addEdgeSound;
    public AudioClip traverseSound;
    public AudioClip pathFoundSound;

    [Header("Beginner Action Buttons")]
    public UnityEngine.UI.Button beginnerAddNodeButton;
    public UnityEngine.UI.Button beginnerAddEdgeButton;
    public UnityEngine.UI.Button beginnerRemoveNodeButton;
    public UnityEngine.UI.Button beginnerBFSButton;
    public UnityEngine.UI.Button beginnerDFSButton;
    public UnityEngine.UI.Button beginnerDijkstraButton;

    [Header("Intermediate Action Buttons")]
    public UnityEngine.UI.Button intermediateAddNodeButton;
    public UnityEngine.UI.Button intermediateAddEdgeButton;
    public UnityEngine.UI.Button intermediateRemoveNodeButton;
    public UnityEngine.UI.Button intermediateMSTButton;
    public UnityEngine.UI.Button intermediatePathCheckButton;
    public UnityEngine.UI.Button intermediateDegreeButton;

    private AudioSource audioSource;

    [Header("Graph Settings")]
    public float nodeSize = 0.04f;
    public float roadWidth = 0.008f;
    public float sceneHeightOffset = 0.05f;
    public float pathAnimSpeed = 0.5f;
    public float moveStep = 0.05f;
    public float buildingScaleMultiplier = 1.0f;

    [Header("Tutorial System")]
    public GraphTutorialIntegration tutorialIntegration;

    [Header("Swipe Rotation")]
    public SwipeRotation swipeRotation;

    // ─────────────────────────────────────────────
    //  SCENARIO
    // ─────────────────────────────────────────────
    public enum ScenarioMode { None, CityMap, IslandNetwork, SpaceStation }
    private ScenarioMode currentScenario = ScenarioMode.None;

    public void OnScenarioChosen(ScenarioMode scenario)
    {
        currentScenario = scenario;
        SetActive(scenarioPanel, false);
        AdvanceToDifficulty();
    }

    // ─────────────────────────────────────────────
    //  DIFFICULTY
    // ─────────────────────────────────────────────
    private enum DifficultyMode { None, Beginner, Intermediate }
    private DifficultyMode currentDifficulty = DifficultyMode.None;

    // ─────────────────────────────────────────────
    //  COLOUR PALETTES
    // ─────────────────────────────────────────────
    private Color[] cityColors = new Color[]
    {
        new Color(0.8f, 0.3f, 0.3f), new Color(0.3f, 0.5f, 0.9f),
        new Color(0.3f, 0.8f, 0.4f), new Color(1f,   0.7f, 0.3f),
        new Color(0.9f, 0.4f, 0.9f), new Color(0.4f, 0.9f, 0.9f),
        new Color(1f,   0.9f, 0.3f), new Color(0.7f, 0.5f, 0.3f)
    };

    private Color[] islandColors = new Color[]
    {
        new Color(0.15f, 0.65f, 0.35f), new Color(0.96f, 0.75f, 0.18f),
        new Color(0.20f, 0.55f, 0.90f), new Color(0.85f, 0.35f, 0.20f),
        new Color(0.60f, 0.85f, 0.30f), new Color(0.95f, 0.55f, 0.15f),
        new Color(0.30f, 0.80f, 0.75f), new Color(0.80f, 0.60f, 0.90f)
    };

    private Color[] spaceColors = new Color[]
    {
        new Color(0.70f, 0.75f, 0.80f), new Color(0.25f, 0.55f, 0.95f),
        new Color(0.90f, 0.60f, 0.10f), new Color(0.20f, 0.85f, 0.75f),
        new Color(0.85f, 0.25f, 0.55f), new Color(0.55f, 0.85f, 0.25f),
        new Color(0.95f, 0.45f, 0.10f), new Color(0.45f, 0.25f, 0.90f)
    };

    private enum GraphState
    {
        ChoosingScenario, ChoosingDifficulty, WaitingForPlane,
        Ready, PositioningNode, AddingEdge, RunningAlgorithm
    }

    private enum InputPanelMode { AddNode, RemoveNode }

    private GraphState currentState = GraphState.ChoosingScenario;

    private class GraphNode
    {
        public GameObject nodeObject;
        public string cityName;
        public Vector3 position;
        public List<GraphEdge> edges = new List<GraphEdge>();
        public GameObject nameLabel;
        public int colorIndex;
        public bool visited;
        public float distance;
        public GraphNode previous;
    }

    private class GraphEdge
    {
        public GameObject roadObject;
        public GraphNode fromNode;
        public GraphNode toNode;
        public float weight;
        public GameObject weightLabel;
        public bool isDirected;
        public bool inMST;
    }

    private List<GraphNode> nodes = new List<GraphNode>();
    private List<GraphEdge> edges = new List<GraphEdge>();
    private InputPanelMode currentInputMode = InputPanelMode.AddNode;
    private GameObject graphScene;
    private GameObject cityBase;
    private GameObject previewNode;
    private GameObject previewBeacon;
    private Vector3 currentPreviewPosition = Vector3.zero;
    private bool sceneSpawned = false;
    private int nodeIdCounter = 1;
    private string pendingCityName = "";
    private Vector3 canonicalPrefabLocalScale = Vector3.zero;

    private Dictionary<UnityEngine.UI.Button, Color> originalButtonColors
        = new Dictionary<UnityEngine.UI.Button, Color>();
    private Coroutine pulseCoroutine = null;

    private static readonly Color PulseBaseColor  = new Color(0.518f, 0.412f, 1.000f);
    private static readonly Color PulseLightColor = new Color(0.698f, 0.627f, 1.000f);

    // ─────────────────────────────────────────────
    //  GUIDE MODE SILENCE FLAG
    // ─────────────────────────────────────────────
    private bool _silenceInstructions = false;

    /// <summary>
    /// Called by ARGraphLessonGuide to stop this controller from overwriting
    /// the guide's own instruction / status / detection text panels.
    /// Pass true on InitGuide(), false on OnReturn().
    /// </summary>
    public void SetInstructionSilence(bool silent)
    {
        _silenceInstructions = silent;

        SetPanelActive(instructionText,   !silent);
        SetPanelActive(operationInfoText, !silent);
        SetPanelActive(detectionText,     !silent);
        SetPanelActive(statusText,        !silent);
    }

    /// <summary>
    /// Walks two levels up from the TMP component to find the root card panel
    /// (Text -> Panel -> RootPanel) and sets its active state.
    /// </summary>
    void SetPanelActive(TextMeshProUGUI tmp, bool active)
    {
        if (tmp == null) return;
        Transform t = tmp.transform.parent;
        GameObject root = (t != null && t.parent != null)
            ? t.parent.gameObject
            : (t != null ? t.gameObject : tmp.gameObject);
        if (root != null) root.SetActive(active);
    }

    // ─────────────────────────────────────────────
    //  INSTRUCTION HELPERS — all gated by _silenceInstructions
    // ─────────────────────────────────────────────
    void UpdateInstructions(string msg)
    {
        if (_silenceInstructions) return;
        if (instructionText != null) { instructionText.text = msg; instructionText.color = Color.white; }
    }

    void UpdateInstructionsSuccess(string msg)
    {
        if (_silenceInstructions) return;
        if (instructionText != null) { instructionText.text = " " + msg; instructionText.color = new Color(0.2f, 1f, 0.4f); }
    }

    void UpdateInstructionsError(string msg)
    {
        if (_silenceInstructions) return;
        if (instructionText != null) { instructionText.text = " " + msg; instructionText.color = new Color(1f, 0.3f, 0.3f); }
    }

    // ═════════════════════════════════════════════
    //  SCENARIO HELPERS
    // ═════════════════════════════════════════════
    float CurrentSceneScale() =>
        graphScene == null ? 1f : Mathf.Max(graphScene.transform.localScale.x, 0.0001f);

    float ZoomAdjustedMoveStep() => moveStep * CurrentSceneScale();

    string NodeLabel()  =>
        currentScenario == ScenarioMode.IslandNetwork ? "Island"
        : currentScenario == ScenarioMode.SpaceStation ? "Module"
        : "City";

    string EdgeLabel()  =>
        currentScenario == ScenarioMode.IslandNetwork ? "Bridge"
        : currentScenario == ScenarioMode.SpaceStation ? "Docking Tube"
        : "Road";

    string EdgeUnit()   =>
        currentScenario == ScenarioMode.IslandNetwork ? "nm"
        : currentScenario == ScenarioMode.SpaceStation ? "m"
        : "km";

    string GraphLabel() =>
        currentScenario == ScenarioMode.IslandNetwork ? "Archipelago"
        : currentScenario == ScenarioMode.SpaceStation ? "Space Station"
        : "City Map";

    string BaseEmoji()  =>
        currentScenario == ScenarioMode.IslandNetwork ? ""
        : currentScenario == ScenarioMode.SpaceStation ? ""
        : "";

    string BFSFlavour() =>
        currentScenario == ScenarioMode.IslandNetwork
            ? "Explore nearby islands first\nLike sending boats outward!"
        : currentScenario == ScenarioMode.SpaceStation
            ? "Scan adjacent modules first\nLike spreading a signal ring!"
        : "Visit neighbors level by level\nLike exploring nearby areas!";

    string DFSFlavour() =>
        currentScenario == ScenarioMode.IslandNetwork
            ? "Follow one chain of islands deep\nLike sailing one route fully!"
        : currentScenario == ScenarioMode.SpaceStation
            ? "Probe one corridor to its end\nLike a maintenance walk-through!"
        : "Go deep before going wide\nLike exploring one path fully!";

    string DijkstraFlavour() =>
        currentScenario == ScenarioMode.IslandNetwork
            ? "Finds shortest sailing route\nLike charting the fastest voyage!"
        : currentScenario == ScenarioMode.SpaceStation
            ? "Finds fastest EVA route\nLike mission-critical path planning!"
        : "Finds fastest route\nLike GPS navigation!";

    string MSTFlavour() =>
        currentScenario == ScenarioMode.IslandNetwork
            ? "Connects ALL islands\nwith minimum total bridge cost!\n\nGreedy: always pick shortest bridge."
        : currentScenario == ScenarioMode.SpaceStation
            ? "Links ALL modules\nwith minimum total tube length!\n\nGreedy: always pick shortest tube."
        : "Connects ALL cities\nwith minimum total road cost!\n\nGreedy: always pick cheapest edge.";

    string[] InitialNames() =>
        currentScenario == ScenarioMode.IslandNetwork
            ? new[] { "Coral Isle", "Palm Cay", "Driftwood", "Volcano Isle", "Lagoon Atoll" }
        : currentScenario == ScenarioMode.SpaceStation
            ? new[] { "Command", "Lab Alpha", "Cargo Bay", "Airlock", "Solar Wing" }
        : new[] { "Plaza", "Market", "Park", "School", "Library" };

    Color[] ActiveColors() =>
        currentScenario == ScenarioMode.IslandNetwork ? islandColors
        : currentScenario == ScenarioMode.SpaceStation ? spaceColors
        : cityColors;

    private bool _started = false;

    // ═════════════════════════════════════════════
    //  START
    // ═════════════════════════════════════════════
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

        HideAllPanels();
        WireUpScenarioButtons();
        WireUpDifficultyButtons();
        ShowScenarioPanel();

        if (detectionText != null && !_silenceInstructions)
        { detectionText.text = "Choose a scenario first!"; detectionText.color = Color.white; }
        if (statusText    != null && !_silenceInstructions)
            statusText.text = "Nodes: 0 | Edges: 0";
    }

    void HideAllPanels()
    {
        SetActive(scenarioPanel,           false);
        SetActive(difficultyPanel,         false);
        SetActive(mainButtonPanel,         false);
        SetActive(beginnerButtonPanel,     false);
        SetActive(intermediateButtonPanel, false);
        SetActive(movementButtonPanel,     false);
        SetActive(inputPanel,              false);
        SetActive(inputPanelEdge,          false);
        SetActive(algorithmPanel,          false);
        SetActive(explanationPanel,        false);
        SetActive(confirmButton,           false);
        SetActive(pathCheckInputPanel,     false);
        SetActive(degreeInputPanel,        false);
    }

    void WireUpScenarioButtons()
    {
        if (cityMapScenarioBtn       != null) cityMapScenarioBtn.onClick.AddListener(OnSelectCityMap);
        if (islandNetworkScenarioBtn != null) islandNetworkScenarioBtn.onClick.AddListener(OnSelectIslandNetwork);
        if (spaceStationScenarioBtn  != null) spaceStationScenarioBtn.onClick.AddListener(OnSelectSpaceStation);
    }

    void WireUpDifficultyButtons()
    {
        if (beginnerBtn     != null) beginnerBtn.onClick.AddListener(OnSelectBeginner);
        if (intermediateBtn != null) intermediateBtn.onClick.AddListener(OnSelectIntermediate);
    }

    void ShowScenarioPanel()
    {
        currentState = GraphState.ChoosingScenario;
        SetActive(scenarioPanel, true);
        UpdateInstructions("Choose your graph scenario!");
    }

    /// <summary>Returns current number of nodes in the graph.</summary>
    public int GetNodeCount() => nodes.Count;

    /// <summary>Returns current number of edges in the graph.</summary>
    public int GetEdgeCount() => edges.Count;

    /// <summary>Returns node names as a read-only list.</summary>
    public IReadOnlyList<string> GetNodeNames()
    {
        var names = new List<string>();
        foreach (var n in nodes) names.Add(n.cityName);
        return names;
    }

    void ShowWelcomeTutorialDelayed()
    {
        if (tutorialIntegration != null) tutorialIntegration.ShowWelcomeTutorial();
    }

    // ═════════════════════════════════════════════
    //  UPDATE
    // ═════════════════════════════════════════════
    void Update()
    {
        if (currentState == GraphState.WaitingForPlane) DetectPlaneInteraction();
        if (previewBeacon != null && arCamera != null)
            previewBeacon.transform.rotation =
                Quaternion.LookRotation(previewBeacon.transform.position - arCamera.transform.position);
    }

    // ═════════════════════════════════════════════
    //  SCENARIO SELECTION
    // ═════════════════════════════════════════════
    public void OnSelectCityMap()       { currentScenario = ScenarioMode.CityMap;       SetActive(scenarioPanel, false); AdvanceToDifficulty(); }
    public void OnSelectIslandNetwork() { currentScenario = ScenarioMode.IslandNetwork; SetActive(scenarioPanel, false); AdvanceToDifficulty(); }
    public void OnSelectSpaceStation()  { currentScenario = ScenarioMode.SpaceStation;  SetActive(scenarioPanel, false); AdvanceToDifficulty(); }

    void AdvanceToDifficulty()
    {
        currentState = GraphState.ChoosingDifficulty;
        SetActive(difficultyPanel, true);
        UpdateInstructions("Choose your difficulty level!");
    }

    // ═════════════════════════════════════════════
    //  DIFFICULTY SELECTION
    // ═════════════════════════════════════════════
    public void OnSelectBeginner()     { currentDifficulty = DifficultyMode.Beginner;     SetActive(difficultyPanel, false); EnableARAndWaitForTap(); }
    public void OnSelectIntermediate() { currentDifficulty = DifficultyMode.Intermediate; SetActive(difficultyPanel, false); EnableARAndWaitForTap(); }

    void EnableARAndWaitForTap()
    {
        if (planeManager   != null) planeManager.enabled   = true;
        if (raycastManager != null) raycastManager.enabled = true;
        currentState = GraphState.WaitingForPlane;
        UpdateInstructions("Point camera at a flat surface and tap to place!");
        if (detectionText != null && !_silenceInstructions)
        { detectionText.text = "Looking for surfaces..."; detectionText.color = Color.yellow; }
    }

    // ═════════════════════════════════════════════
    //  PLANE DETECTION & SCENE SPAWN
    // ═════════════════════════════════════════════
    void DetectPlaneInteraction()
    {
        if (sceneSpawned) return;
        bool inputReceived = false;
        Vector2 screenPosition = Vector2.zero;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Touch touch = Input.GetTouch(0);
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;
            screenPosition = touch.position; inputReceived = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            screenPosition = Input.mousePosition; inputReceived = true;
        }

        if (!inputReceived || raycastManager == null) return;
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            SpawnGraphScene(hits[0].pose.position, hits[0].pose.rotation);
    }

    void SpawnGraphScene(Vector3 position, Quaternion rotation)
    {
        sceneSpawned = true;
        PlaySound(placeSceneSound);

        if (planeManager != null)
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);

        graphScene = new GameObject("GraphScene");
        graphScene.transform.position = position + Vector3.up * sceneHeightOffset;
        graphScene.transform.rotation = rotation;

        if (swipeRotation  != null) swipeRotation.InitializeRotation(graphScene.transform);
        if (zoomController != null) zoomController.InitializeZoom(graphScene.transform);

        BuildSceneForScenario();
        ShowDifficultyButtons();

        // Only write to detectionText when not silenced
        if (detectionText != null && !_silenceInstructions)
        { detectionText.text = "Scene Placed!"; detectionText.color = Color.green; }

        if (tutorialIntegration != null) Invoke(nameof(ShowWelcomeTutorialDelayed), 1f);
    }

    void ShowDifficultyButtons()
    {
        string em = BaseEmoji(); string gl = GraphLabel();
        string nl = NodeLabel(); string el = EdgeLabel();

        if (currentDifficulty == DifficultyMode.Beginner)
        {
            SetActive(mainButtonPanel,         true);
            SetActive(beginnerButtonPanel,     true);
            SetActive(intermediateButtonPanel, false);
            SetActive(explanationPanel,        true);

            // Only write these when not silenced — guide owns them in lesson mode
            if (!_silenceInstructions)
            {
                UpdateInstructions($"Beginner Mode – {gl}! Add {nl}s, {el}s, BFS, DFS, Dijkstra!");
                if (operationInfoText != null)
                    operationInfoText.text =
                        $"BEGINNER MODE  {em}\n\nADD {nl.ToUpper()}    Add vertex       O(1)\n" +
                        $"ADD {el.ToUpper()}    Add edge         O(1)\nREMOVE      Remove vertex    O(E)\n" +
                        $"BFS         Breadth-First    O(V+E)\nDFS         Depth-First      O(V+E)\n" +
                        $"DIJKSTRA    Shortest path    O((V+E)logV)\n\nLike a {gl.ToLower()}!";
            }
        }
        else
        {
            SetActive(mainButtonPanel,         true);
            SetActive(beginnerButtonPanel,     false);
            SetActive(intermediateButtonPanel, true);
            SetActive(explanationPanel,        true);

            if (!_silenceInstructions)
            {
                UpdateInstructions($"Intermediate Mode – {gl}! MST, Path Check, Degree!");
                if (operationInfoText != null)
                    operationInfoText.text =
                        $"INTERMEDIATE MODE  {em}\n\nADD {nl.ToUpper()}    Add vertex       O(1)\n" +
                        $"ADD {el.ToUpper()}    Add edge         O(1)\nREMOVE      Remove vertex    O(E)\n" +
                        $"MST (PRIM)  Minimum tree     O(E logV)\nPATH CHECK  Does path exist? O(V+E)\n" +
                        $"DEGREE      Count edges      O(degree)\n\nAdvanced graph theory!";
            }
        }

        currentState = GraphState.Ready;
        UpdateStatus();
        CheckAndUpdateButtonStates();
    }

    // ═════════════════════════════════════════════
    //  SPACE STATION NODE — BuildSpaceModule
    // ═════════════════════════════════════════════
    GameObject BuildSpaceModule(string name, int colorIndex)
    {
        GameObject module = new GameObject($"Module_{name}");
        module.transform.SetParent(graphScene.transform, worldPositionStays: false);

        Color moduleColor = spaceColors[colorIndex % spaceColors.Length];
        float r = nodeSize;

        GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hull.transform.SetParent(module.transform);
        hull.transform.localPosition = Vector3.zero;
        hull.transform.localScale    = new Vector3(r * 2f, r * 0.55f, r * 2f);
        var hullMat = new Material(Shader.Find("Standard"));
        hullMat.color = moduleColor;
        hullMat.SetFloat("_Metallic",   0.75f);
        hullMat.SetFloat("_Smoothness", 0.65f);
        hull.GetComponent<Renderer>().material = hullMat;
        Destroy(hull.GetComponent<Collider>());

        GameObject topCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        topCap.transform.SetParent(module.transform);
        topCap.transform.localPosition = new Vector3(0, r * 0.55f, 0);
        topCap.transform.localScale    = new Vector3(r * 1.6f, r * 0.15f, r * 1.6f);
        var capMat = new Material(Shader.Find("Standard"));
        capMat.color = moduleColor * 0.75f;
        capMat.SetFloat("_Metallic", 0.9f);
        topCap.GetComponent<Renderer>().material = capMat;
        Destroy(topCap.GetComponent<Collider>());

        GameObject botCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        botCap.transform.SetParent(module.transform);
        botCap.transform.localPosition = new Vector3(0, -r * 0.55f, 0);
        botCap.transform.localScale    = new Vector3(r * 1.6f, r * 0.15f, r * 1.6f);
        var botMat = new Material(Shader.Find("Standard"));
        botMat.color = moduleColor * 0.75f;
        botMat.SetFloat("_Metallic", 0.9f);
        botCap.GetComponent<Renderer>().material = botMat;
        Destroy(botCap.GetComponent<Collider>());

        for (int side = -1; side <= 1; side += 2)
        {
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.transform.SetParent(module.transform);
            arm.transform.localPosition = new Vector3(side * r * 1.8f, 0, 0);
            arm.transform.localScale    = new Vector3(r * 1.5f, r * 0.08f, r * 0.12f);
            var armMat = new Material(Shader.Find("Standard"));
            armMat.color = new Color(0.55f, 0.55f, 0.55f);
            arm.GetComponent<Renderer>().material = armMat;
            Destroy(arm.GetComponent<Collider>());

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.transform.SetParent(module.transform);
            panel.transform.localPosition = new Vector3(side * r * 3.2f, 0, 0);
            panel.transform.localScale    = new Vector3(r * 1.8f, r * 0.04f, r * 1.2f);
            var panelMat = new Material(Shader.Find("Standard"));
            panelMat.color = new Color(0.10f, 0.15f, 0.65f);
            panelMat.SetFloat("_Metallic",   0.3f);
            panelMat.SetFloat("_Smoothness", 0.8f);
            panel.GetComponent<Renderer>().material = panelMat;
            Destroy(panel.GetComponent<Collider>());
        }

        GameObject light = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        light.transform.SetParent(module.transform);
        light.transform.localPosition = new Vector3(0, r * 0.80f, 0);
        light.transform.localScale    = Vector3.one * r * 0.30f;
        var lightMat = new Material(Shader.Find("Standard"));
        lightMat.color = new Color(0f, 1f, 0.5f);
        lightMat.SetFloat("_Metallic",   0f);
        lightMat.SetFloat("_Smoothness", 1f);
        lightMat.EnableKeyword("_EMISSION");
        lightMat.SetColor("_EmissionColor", new Color(0f, 1f, 0.4f) * 2.0f);
        light.GetComponent<Renderer>().material = lightMat;
        Destroy(light.GetComponent<Collider>());

        for (int w = 0; w < 4; w++)
        {
            float angle  = w * 90f * Mathf.Deg2Rad;
            float px     = Mathf.Cos(angle) * r * 1.9f;
            float pz     = Mathf.Sin(angle) * r * 1.9f;
            GameObject porthole = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            porthole.transform.SetParent(module.transform);
            porthole.transform.localPosition = new Vector3(px, r * 0.10f, pz);
            porthole.transform.localScale    = Vector3.one * r * 0.25f;
            var portMat = new Material(Shader.Find("Standard"));
            portMat.color = new Color(0.6f, 0.85f, 1.0f, 0.7f);
            portMat.SetFloat("_Mode", 3);
            portMat.EnableKeyword("_ALPHABLEND_ON");
            portMat.SetFloat("_Metallic",   0.1f);
            portMat.SetFloat("_Smoothness", 0.95f);
            porthole.GetComponent<Renderer>().material = portMat;
            Destroy(porthole.GetComponent<Collider>());
        }

        module.AddComponent<SphereCollider>().radius = r * 3.5f;
        return module;
    }

    // ═════════════════════════════════════════════
    //  DOCKING TUBE EDGE
    // ═════════════════════════════════════════════
    GameObject CreateDockingTube(Vector3 localStart, Vector3 localEnd)
    {
        if (dockingTubePrefab != null)
        {
            GameObject tube = Instantiate(dockingTubePrefab);
            tube.transform.SetParent(graphScene.transform, worldPositionStays: false);
            Vector3 direction = localEnd - localStart;
            tube.transform.localPosition = localStart + direction * 0.5f;
            tube.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            tube.transform.localScale    = new Vector3(roadWidth * 2f, direction.magnitude * 0.5f, roadWidth * 2f);
            Destroy(tube.GetComponent<Collider>());
            return tube;
        }

        GameObject container = new GameObject("DockingTube");
        container.transform.SetParent(graphScene.transform, worldPositionStays: false);

        Vector3 dir  = localEnd - localStart;
        float   dist = dir.magnitude;

        GameObject sleeve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        sleeve.transform.SetParent(container.transform);
        sleeve.transform.localPosition = localStart + dir * 0.5f;
        sleeve.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        sleeve.transform.localScale    = new Vector3(roadWidth * 2.2f, dist * 0.5f, roadWidth * 2.2f);
        var sleeveMat = new Material(Shader.Find("Standard"));
        sleeveMat.color = new Color(0.55f, 0.60f, 0.65f);
        sleeveMat.SetFloat("_Metallic",   0.85f);
        sleeveMat.SetFloat("_Smoothness", 0.55f);
        sleeve.GetComponent<Renderer>().material = sleeveMat;
        Destroy(sleeve.GetComponent<Collider>());

        for (float t = 0.25f; t <= 0.76f; t += 0.25f)
        {
            GameObject rib = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rib.transform.SetParent(container.transform);
            rib.transform.localPosition = localStart + dir * t;
            rib.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            rib.transform.localScale    = new Vector3(roadWidth * 3.0f, dist * 0.025f, roadWidth * 3.0f);
            var ribMat = new Material(Shader.Find("Standard"));
            ribMat.color = new Color(0.35f, 0.38f, 0.42f);
            ribMat.SetFloat("_Metallic", 0.9f);
            rib.GetComponent<Renderer>().material = ribMat;
            Destroy(rib.GetComponent<Collider>());
        }

        return container;
    }

    // ═════════════════════════════════════════════
    //  BASE PLATFORM
    // ═════════════════════════════════════════════
    void CreateBase()
    {
        if (cityBase != null) Destroy(cityBase);
        cityBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cityBase.transform.SetParent(graphScene.transform);
        cityBase.transform.localPosition = new Vector3(0, -0.02f, 0);
        cityBase.transform.localRotation = Quaternion.identity;
        cityBase.transform.localScale    = new Vector3(0.6f, 0.02f, 0.6f);
        Destroy(cityBase.GetComponent<Collider>());
        cityBase.name = "SceneBase";

        var mat = new Material(Shader.Find("Standard"));

        if (currentScenario == ScenarioMode.IslandNetwork)
        {
            mat.color = new Color(0.05f, 0.25f, 0.55f);
            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            surface.transform.SetParent(cityBase.transform);
            surface.transform.localPosition = new Vector3(0, 0.6f, 0);
            surface.transform.localScale    = new Vector3(0.98f, 0.2f, 0.98f);
            Destroy(surface.GetComponent<Collider>());
            var sMat = new Material(Shader.Find("Standard"));
            sMat.color = new Color(0.10f, 0.40f, 0.70f);
            surface.GetComponent<Renderer>().material = sMat;
        }
        else if (currentScenario == ScenarioMode.SpaceStation)
        {
            mat.color = new Color(0.04f, 0.04f, 0.08f);
            mat.SetFloat("_Metallic",   0.0f);
            mat.SetFloat("_Smoothness", 0.0f);

            GameObject nebulaRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            nebulaRing.transform.SetParent(cityBase.transform);
            nebulaRing.transform.localPosition = new Vector3(0, 0.5f, 0);
            nebulaRing.transform.localScale    = new Vector3(0.95f, 0.05f, 0.95f);
            Destroy(nebulaRing.GetComponent<Collider>());
            var nebMat = new Material(Shader.Find("Standard"));
            nebMat.color = new Color(0.10f, 0.08f, 0.22f);
            nebMat.SetFloat("_Metallic",   0.0f);
            nebMat.SetFloat("_Smoothness", 0.0f);
            nebulaRing.GetComponent<Renderer>().material = nebMat;
        }
        else
        {
            mat.color = new Color(0.6f, 0.6f, 0.6f);
        }

        cityBase.GetComponent<Renderer>().material = mat;
    }

    // ═════════════════════════════════════════════
    //  SCENE CONSTRUCTION
    // ═════════════════════════════════════════════
    void BuildSceneForScenario()
    {
        CreateBase();
        CreateInitialNetwork();
        UpdateStatus();
    }

    void CreateInitialNetwork()
    {
        Vector3[] positions = {
            new Vector3( 0f,    0, 0f),
            new Vector3( 0.12f, 0, 0.08f),
            new Vector3(-0.10f, 0, 0.10f),
            new Vector3( 0.08f, 0,-0.12f),
            new Vector3(-0.12f, 0,-0.08f)
        };
        string[] names = InitialNames();
        for (int i = 0; i < positions.Length; i++)
            CreateNodeDirect(names[i], positions[i], i);

        AddEdgeDirect(nodes[0], nodes[1], 5f, false);
        AddEdgeDirect(nodes[0], nodes[2], 7f, false);
        AddEdgeDirect(nodes[1], nodes[3], 3f, false);
        AddEdgeDirect(nodes[2], nodes[4], 4f, false);
        AddEdgeDirect(nodes[3], nodes[4], 6f, false);
    }

    // ─────────────────────────────────────────────
    //  NODE FACTORY
    // ─────────────────────────────────────────────
    GameObject CreateSceneNode(string name, int colorIndex)
    {
        if (currentScenario == ScenarioMode.SpaceStation)
        {
            if (stationModulePrefabs != null && stationModulePrefabs.Length > 0)
            {
                var valid = stationModulePrefabs.Where(p => p != null).ToArray();
                if (valid.Length > 0)
                {
                    GameObject node = Instantiate(valid[Random.Range(0, valid.Length)]);
                    node.transform.SetParent(graphScene.transform, worldPositionStays: false);
                    if (canonicalPrefabLocalScale == Vector3.zero)
                        canonicalPrefabLocalScale = node.transform.localScale * buildingScaleMultiplier;
                    node.transform.localScale = canonicalPrefabLocalScale;
                    if (node.GetComponent<Collider>() == null)
                        node.AddComponent<SphereCollider>().radius = nodeSize * 3.5f;
                    return node;
                }
            }
            return BuildSpaceModule(name, colorIndex);
        }

        if (currentScenario == ScenarioMode.IslandNetwork)
        {
            if (islandPrefabs != null && islandPrefabs.Length > 0)
            {
                var valid = islandPrefabs.Where(p => p != null).ToArray();
                if (valid.Length > 0)
                {
                    GameObject node = Instantiate(valid[Random.Range(0, valid.Length)]);
                    node.transform.SetParent(graphScene.transform, worldPositionStays: false);
                    if (canonicalPrefabLocalScale == Vector3.zero)
                        canonicalPrefabLocalScale = node.transform.localScale * buildingScaleMultiplier;
                    node.transform.localScale = canonicalPrefabLocalScale;
                    if (node.GetComponent<Collider>() == null)
                        node.AddComponent<SphereCollider>().radius = nodeSize * 1.5f;
                    return node;
                }
            }
            return CreateProceduralIsland(name, colorIndex);
        }

        if (buildingPrefabs != null && buildingPrefabs.Length > 0)
        {
            var valid = buildingPrefabs.Where(p => p != null).ToArray();
            if (valid.Length > 0)
            {
                GameObject node = Instantiate(valid[Random.Range(0, valid.Length)]);
                node.transform.SetParent(graphScene.transform, worldPositionStays: false);
                if (canonicalPrefabLocalScale == Vector3.zero)
                    canonicalPrefabLocalScale = node.transform.localScale * buildingScaleMultiplier;
                node.transform.localScale = canonicalPrefabLocalScale;
                if (node.GetComponent<Collider>() == null)
                    node.AddComponent<SphereCollider>().radius = nodeSize * 1.5f;
                return node;
            }
        }
        return CreateProceduralBuilding(name, colorIndex);
    }

    // ─────────────────────────────────────────────
    //  EDGE FACTORY
    // ─────────────────────────────────────────────
    GameObject CreateRoad(Vector3 localStart, Vector3 localEnd, bool isDirected)
    {
        if (currentScenario == ScenarioMode.SpaceStation)
        {
            GameObject tube = CreateDockingTube(localStart, localEnd);
            if (isDirected) AttachDirectionArrow(tube, localStart, localEnd);
            return tube;
        }

        GameObject road;
        if (currentScenario == ScenarioMode.IslandNetwork && bridgePrefab != null)
            road = Instantiate(bridgePrefab);
        else
            road = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        road.transform.SetParent(graphScene.transform, worldPositionStays: false);
        Vector3 direction = localEnd - localStart;
        float   distance  = direction.magnitude;
        road.transform.localPosition = localStart + direction * 0.5f;
        road.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
        road.transform.localScale    = new Vector3(roadWidth, distance * 0.5f, roadWidth);

        var mat = new Material(Shader.Find("Standard"));
        mat.color = currentScenario == ScenarioMode.IslandNetwork
            ? new Color(0.55f, 0.40f, 0.18f)
            : new Color(0.30f, 0.30f, 0.30f);
        road.GetComponent<Renderer>().material = mat;
        Destroy(road.GetComponent<Collider>());

        if (isDirected) AttachDirectionArrow(road, localStart, localEnd);
        return road;
    }

    void AttachDirectionArrow(GameObject parent, Vector3 localStart, Vector3 localEnd)
    {
        GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arrow.transform.SetParent(parent.transform);
        arrow.transform.localPosition = new Vector3(0, 0.7f, 0);
        arrow.transform.localRotation = Quaternion.Euler(45, 0, 0);
        arrow.transform.localScale    = new Vector3(3f, 0.5f, 3f);
        arrow.GetComponent<Renderer>().material.color = Color.yellow;
        Destroy(arrow.GetComponent<Collider>());
    }

    void AddEdgeDirect(GraphNode from, GraphNode to, float weight, bool isDirected)
    {
        GameObject roadObj = CreateRoad(from.position, to.position, isDirected);

        GraphEdge newEdge = new GraphEdge
        {
            roadObject = roadObj, fromNode = from, toNode = to,
            weight = weight, isDirected = isDirected
        };

        Vector3 midPoint = (from.position + to.position) * 0.5f;
        newEdge.weightLabel = CreateTextLabel(graphScene.transform,
            $"{weight:F0}{EdgeUnit()}",
            midPoint + new Vector3(0, 0.03f, 0),
            Color.yellow, 25, new Vector3(0.003f, 0.003f, 0.003f));

        from.edges.Add(newEdge);
        if (!isDirected) to.edges.Add(newEdge);
        edges.Add(newEdge);
    }

    // ─────────────────────────────────────────────
    //  PROCEDURAL FALLBACKS
    // ─────────────────────────────────────────────
    GameObject CreateProceduralBuilding(string name, int colorIndex)
    {
        GameObject fallback = new GameObject($"City_{name}");
        fallback.transform.SetParent(graphScene.transform, worldPositionStays: false);
        GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building.transform.SetParent(fallback.transform);
        building.transform.localPosition = Vector3.zero;
        float height = Random.Range(0.03f, 0.06f);
        building.transform.localScale = new Vector3(nodeSize, height, nodeSize);
        var mat = new Material(Shader.Find("Standard"));
        mat.color = cityColors[colorIndex % cityColors.Length];
        building.GetComponent<Renderer>().material = mat;
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(fallback.transform);
        roof.transform.localPosition = new Vector3(0, height + 0.005f, 0);
        roof.transform.localScale    = new Vector3(nodeSize * 1.1f, 0.005f, nodeSize * 1.1f);
        roof.GetComponent<Renderer>().material.color = cityColors[colorIndex % cityColors.Length] * 0.7f;
        Destroy(roof.GetComponent<Collider>());
        Destroy(building.GetComponent<Collider>());
        fallback.AddComponent<SphereCollider>().radius = nodeSize * 2f;
        return fallback;
    }

    GameObject CreateProceduralIsland(string name, int colorIndex)
    {
        GameObject island = new GameObject($"Island_{name}");
        island.transform.SetParent(graphScene.transform, worldPositionStays: false);

        GameObject mound = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mound.transform.SetParent(island.transform);
        mound.transform.localPosition = Vector3.zero;
        mound.transform.localScale    = new Vector3(nodeSize * 2.2f, nodeSize * 0.7f, nodeSize * 2.2f);
        var sandMat = new Material(Shader.Find("Standard"));
        sandMat.color = new Color(0.94f, 0.84f, 0.55f);
        mound.GetComponent<Renderer>().material = sandMat;
        Destroy(mound.GetComponent<Collider>());

        GameObject vegDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vegDome.transform.SetParent(island.transform);
        vegDome.transform.localPosition = new Vector3(0, nodeSize * 0.6f, 0);
        vegDome.transform.localScale    = new Vector3(nodeSize * 1.4f, nodeSize * 1.0f, nodeSize * 1.4f);
        var vegMat = new Material(Shader.Find("Standard"));
        vegMat.color = islandColors[colorIndex % islandColors.Length];
        vegDome.GetComponent<Renderer>().material = vegMat;
        Destroy(vegDome.GetComponent<Collider>());

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(island.transform);
        trunk.transform.localPosition = new Vector3(nodeSize * 0.4f, nodeSize * 1.0f, 0);
        trunk.transform.localScale    = new Vector3(nodeSize * 0.15f, nodeSize * 0.8f, nodeSize * 0.15f);
        trunk.GetComponent<Renderer>().material.color = new Color(0.55f, 0.35f, 0.12f);
        Destroy(trunk.GetComponent<Collider>());

        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.transform.SetParent(island.transform);
        crown.transform.localPosition = new Vector3(nodeSize * 0.4f, nodeSize * 1.9f, 0);
        crown.transform.localScale    = new Vector3(nodeSize * 0.7f, nodeSize * 0.35f, nodeSize * 0.7f);
        crown.GetComponent<Renderer>().material.color = new Color(0.15f, 0.70f, 0.20f);
        Destroy(crown.GetComponent<Collider>());

        island.AddComponent<SphereCollider>().radius = nodeSize * 2f;
        return island;
    }

    // ─────────────────────────────────────────────
    //  SHARED NODE CREATION
    // ─────────────────────────────────────────────
    void CreateNodeDirect(string name, Vector3 localPosition, int colorIndex)
    {
        GameObject nodeObj = CreateSceneNode(name, colorIndex);
        if (nodeObj.transform.parent != graphScene.transform)
            nodeObj.transform.SetParent(graphScene.transform, worldPositionStays: false);
        nodeObj.transform.localPosition = localPosition;
        nodeObj.transform.localRotation = Quaternion.identity;
        GraphNode newNode = new GraphNode
        {
            nodeObject = nodeObj, cityName = name, position = localPosition, colorIndex = colorIndex
        };
        StartCoroutine(CreateLabelAfterFrame(newNode));
        nodes.Add(newNode);
    }

    IEnumerator CreateLabelAfterFrame(GraphNode node)
    {
        yield return null;
        node.nameLabel = CreateTextLabel(node.nodeObject.transform, node.cityName,
            new Vector3(0, 0.15f, 0), Color.white, 60, new Vector3(0.003f, 0.003f, 0.003f));
    }

    // ─────────────────────────────────────────────
    //  PREVIEW NODE
    // ─────────────────────────────────────────────
    void CreatePreviewNode(string name)
    {
        if (previewNode   != null) { Destroy(previewNode);   previewNode   = null; }
        if (previewBeacon != null) { Destroy(previewBeacon); previewBeacon = null; }

        previewNode = new GameObject($"Preview_{name}");
        previewNode.transform.SetParent(graphScene.transform, worldPositionStays: false);
        previewNode.transform.localPosition = currentPreviewPosition;
        previewNode.transform.localRotation = Quaternion.identity;

        Color previewColor = currentScenario == ScenarioMode.SpaceStation
            ? new Color(0f, 0.8f, 1f)
            : currentScenario == ScenarioMode.IslandNetwork
                ? new Color(1f, 0.5f, 0.2f)
                : new Color(0f, 1f, 0.9f);

        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.transform.SetParent(previewNode.transform);
        box.transform.localPosition = new Vector3(0, nodeSize * 0.5f, 0);
        box.transform.localScale    = new Vector3(nodeSize * 0.9f, nodeSize * 0.9f, nodeSize * 0.9f);
        Destroy(box.GetComponent<Collider>());
        var boxMat = new Material(Shader.Find("Unlit/Color"));
        boxMat.color = previewColor;
        box.GetComponent<Renderer>().material = boxMat;
    }

    void UpdateBeaconPosition() { }

    // ─────────────────────────────────────────────
    //  TEXT LABEL
    // ─────────────────────────────────────────────
    GameObject CreateTextLabel(Transform parent, string text, Vector3 localPos,
                               Color color, int fontSize, Vector3 scale)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(parent);
        labelObj.transform.localPosition = localPos;
        labelObj.transform.localRotation = Quaternion.identity;
        labelObj.transform.localScale    = scale;
        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text      = text;
        textMesh.fontSize  = fontSize;
        textMesh.color     = color;
        textMesh.anchor    = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        labelObj.AddComponent<Billboard>();
        return labelObj;
    }

    // ═════════════════════════════════════════════
    //  BUTTON STATE MANAGEMENT
    // ═════════════════════════════════════════════
    void CheckAndUpdateButtonStates()
    {
        bool hasNodes = nodes.Count > 0;
        bool hasEdges = edges.Count > 0;

        if (currentDifficulty == DifficultyMode.Beginner)
        {
            SetButtonInteractable(beginnerRemoveNodeButton, hasNodes);
            SetButtonInteractable(beginnerBFSButton,        hasNodes);
            SetButtonInteractable(beginnerDFSButton,        hasNodes);
            SetButtonInteractable(beginnerDijkstraButton,   hasNodes);
            SetButtonInteractable(beginnerAddEdgeButton,    nodes.Count >= 2);
        }
        else if (currentDifficulty == DifficultyMode.Intermediate)
        {
            SetButtonInteractable(intermediateRemoveNodeButton,  hasNodes);
            SetButtonInteractable(intermediateMSTButton,         nodes.Count >= 2 && hasEdges);
            SetButtonInteractable(intermediatePathCheckButton,   nodes.Count >= 2);
            SetButtonInteractable(intermediateDegreeButton,      hasNodes);
            SetButtonInteractable(intermediateAddEdgeButton,     nodes.Count >= 2);
        }

        if (!hasNodes)
        {
            if (pulseCoroutine == null)
            {
                UnityEngine.UI.Button addBtn = currentDifficulty == DifficultyMode.Beginner
                    ? beginnerAddNodeButton : intermediateAddNodeButton;
                if (addBtn != null) pulseCoroutine = StartCoroutine(PulseAddNodeButton(addBtn));
            }
        }
        else
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
                ResetPulseButton(beginnerAddNodeButton);
                ResetPulseButton(intermediateAddNodeButton);
            }
        }
    }

    void ResetPulseButton(UnityEngine.UI.Button btn)
    {
        if (btn == null) return;
        btn.transform.localScale = Vector3.one;
        var img = btn.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
            img.color = originalButtonColors.ContainsKey(btn) ? originalButtonColors[btn] : PulseBaseColor;
    }

    IEnumerator PulseAddNodeButton(UnityEngine.UI.Button btn)
    {
        if (btn == null) yield break;
        var img = btn.GetComponent<UnityEngine.UI.Image>();
        if (img != null && !originalButtonColors.ContainsKey(btn))
            originalButtonColors[btn] = img.color;
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * 2.2f;
            float s = 0.92f + 0.18f * ((Mathf.Sin(t) + 1f) * 0.5f);
            btn.transform.localScale = new Vector3(s, s, 1f);
            if (img != null)
            {
                float lerpT = (Mathf.Sin(t) + 1f) * 0.5f;
                img.color = Color.Lerp(PulseBaseColor, PulseLightColor, lerpT);
            }
            yield return null;
        }
    }

    void SetButtonInteractable(UnityEngine.UI.Button btn, bool interactable)
    {
        if (btn == null) return;
        var img = btn.GetComponent<UnityEngine.UI.Image>();
        if (img != null && !originalButtonColors.ContainsKey(btn))
            originalButtonColors[btn] = img.color;
        btn.interactable = interactable;
        if (img != null)
            img.color = !interactable
                ? new Color(0.35f, 0.35f, 0.35f, 0.6f)
                : (originalButtonColors.ContainsKey(btn) ? originalButtonColors[btn] : Color.white);
    }

    // ═════════════════════════════════════════════
    //  ADD NODE
    // ═════════════════════════════════════════════
    public void OnAddNodeButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnAddNodeButtonClicked();
        if (currentState != GraphState.Ready) return;
        currentInputMode = InputPanelMode.AddNode;
        SetActive(inputPanel, true);
        SetActive(mainButtonPanel, false);
        SetActive(beginnerButtonPanel, false);
        SetActive(intermediateButtonPanel, false);
        UpdateInstructions($"Enter {NodeLabel().ToLower()} name then position with arrows");
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                $"ADD {NodeLabel().ToUpper()}!\n\nAdding a new {NodeLabel().ToLower()}\n" +
                $"Time: O(1) - Constant!\n\nEnter name, then position\nwith arrow buttons!";
    }

    public void OnButton1InputPanel()
    {
        if (currentInputMode == InputPanelMode.RemoveNode) { OnConfirmRemoveNode(); return; }
        pendingCityName = (inputField != null && !string.IsNullOrEmpty(inputField.text))
            ? inputField.text : $"{NodeLabel()}{nodeIdCounter}";
        SetActive(inputPanel, false);
        SetActive(movementButtonPanel, true);
        currentState = GraphState.PositioningNode;
        if (tutorialIntegration != null) tutorialIntegration.OnMovementStarted();
        currentPreviewPosition = Vector3.zero;
        CreatePreviewNode(pendingCityName);
        UpdateInstructions($"Position {pendingCityName} using arrow buttons, then confirm");
        SetActive(confirmButton, true);
    }

    public void OnMoveLeftButton()  { if (currentState != GraphState.PositioningNode) return; currentPreviewPosition += new Vector3(-ZoomAdjustedMoveStep(), 0, 0); UpdatePreviewPosition(); }
    public void OnMoveRightButton() { if (currentState != GraphState.PositioningNode) return; currentPreviewPosition += new Vector3( ZoomAdjustedMoveStep(), 0, 0); UpdatePreviewPosition(); }
    public void OnMoveUpButton()    { if (currentState != GraphState.PositioningNode) return; currentPreviewPosition += new Vector3(0, 0,  ZoomAdjustedMoveStep()); UpdatePreviewPosition(); }
    public void OnMoveDownButton()  { if (currentState != GraphState.PositioningNode) return; currentPreviewPosition += new Vector3(0, 0, -ZoomAdjustedMoveStep()); UpdatePreviewPosition(); }

    void UpdatePreviewPosition()
    {
        float maxDist = 0.25f;
        currentPreviewPosition.x = Mathf.Clamp(currentPreviewPosition.x, -maxDist, maxDist);
        currentPreviewPosition.z = Mathf.Clamp(currentPreviewPosition.z, -maxDist, maxDist);
        currentPreviewPosition.y = 0;
        if (previewNode != null) previewNode.transform.localPosition = currentPreviewPosition;
        UpdateBeaconPosition();
    }

    public void OnConfirmButton()
    {
        if (currentState != GraphState.PositioningNode) return;
        string cityName = !string.IsNullOrEmpty(pendingCityName) ? pendingCityName : $"{NodeLabel()}{nodeIdCounter}";
        if (previewNode   != null) { Destroy(previewNode);   previewNode   = null; }
        if (previewBeacon != null) { Destroy(previewBeacon); previewBeacon = null; }
        CreateNodeDirect(cityName, currentPreviewPosition, nodes.Count);
        ARGraphLessonGuide guide = FindObjectOfType<ARGraphLessonGuide>();
        if (guide != null) guide.NotifyNodeAdded();
        PlaySound(addNodeSound);
        SetActive(movementButtonPanel, false);
        ShowModeButtons();
        currentState = GraphState.Ready;
        UpdateInstructionsSuccess($"Added {NodeLabel().ToLower()}: {cityName}");
        UpdateStatus();
        if (inputField != null) inputField.text = "";
        nodeIdCounter++;
        SetActive(confirmButton, false);
        pendingCityName = "";
        CheckAndUpdateButtonStates();
    }

    // ═════════════════════════════════════════════
    //  ADD EDGE
    // ═════════════════════════════════════════════
    public void OnAddEdgeButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnAddEdgeButtonClicked();
        if (currentState != GraphState.Ready || nodes.Count < 2) return;
        SetActive(inputPanelEdge, true);
        SetActive(mainButtonPanel, false);
        SetActive(beginnerButtonPanel, false);
        SetActive(intermediateButtonPanel, false);
        currentState = GraphState.AddingEdge;
        UpdateInstructions($"Enter names of two {NodeLabel().ToLower()}s to connect");
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                $"ADD {EdgeLabel().ToUpper()}!\n\nConnect two {NodeLabel().ToLower()}s\n" +
                $"Time: O(1) - Constant!\n\nEnter {NodeLabel().ToLower()} names\nto create a {EdgeLabel().ToLower()}!";
    }

    public void OnButton1InputPanel1()
    {
        if (currentState != GraphState.AddingEdge) return;
        string fromName = inputFieldFrom != null ? inputFieldFrom.text.Trim() : "";
        string toName   = inputFieldTo   != null ? inputFieldTo.text.Trim()   : "";
        if (string.IsNullOrEmpty(fromName) || string.IsNullOrEmpty(toName))
        { UpdateInstructionsError($"Please enter both {NodeLabel().ToLower()} names!"); return; }

        GraphNode fromNode = nodes.Find(n => n.cityName.Equals(fromName, System.StringComparison.OrdinalIgnoreCase));
        GraphNode toNode   = nodes.Find(n => n.cityName.Equals(toName,   System.StringComparison.OrdinalIgnoreCase));
        if (fromNode == null) { UpdateInstructionsError($"{NodeLabel()} '{fromName}' not found!"); return; }
        if (toNode   == null) { UpdateInstructionsError($"{NodeLabel()} '{toName}' not found!");   return; }
        if (fromNode == toNode) { UpdateInstructionsError($"Cannot connect {NodeLabel().ToLower()} to itself!"); return; }

        bool exists = fromNode.edges.Any(e =>
            (e.fromNode == fromNode && e.toNode == toNode) ||
            (e.fromNode == toNode   && e.toNode == fromNode));
        if (exists) { UpdateInstructionsError($"{EdgeLabel()} already exists!"); return; }

        float distance = Vector3.Distance(fromNode.position, toNode.position) * 100f;
        AddEdgeDirect(fromNode, toNode, distance, false);
        ARGraphLessonGuide guide = FindObjectOfType<ARGraphLessonGuide>();
        if (guide != null) guide.NotifyEdgeAdded();
        PlaySound(addEdgeSound);
        SetActive(inputPanelEdge, false);
        ShowModeButtons();
        currentState = GraphState.Ready;
        UpdateInstructionsSuccess($"{EdgeLabel()} added: {fromName} <-> {toName} ({distance:F0}{EdgeUnit()})");
        UpdateStatus();
        if (inputFieldFrom != null) inputFieldFrom.text = "";
        if (inputFieldTo   != null) inputFieldTo.text   = "";
        CheckAndUpdateButtonStates();
    }

    // ═════════════════════════════════════════════
    //  REMOVE NODE
    // ═════════════════════════════════════════════
    public void OnRemoveNodeButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnRemoveNodeButtonClicked();
        if (currentState != GraphState.Ready || nodes.Count == 0) return;
        currentInputMode = InputPanelMode.RemoveNode;
        SetActive(inputPanel, true);
        SetActive(mainButtonPanel, false);
        SetActive(beginnerButtonPanel, false);
        SetActive(intermediateButtonPanel, false);
        UpdateInstructions($"Enter name of {NodeLabel().ToLower()} to remove");
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                $"REMOVE {NodeLabel().ToUpper()}!\n\nDelete a {NodeLabel().ToLower()} node\n" +
                $"Time: O(E) - Check edges!\n\nAll connected {EdgeLabel().ToLower()}s\nwill be removed too!";
    }

    public void OnConfirmRemoveNode()
    {
        string cityName = inputField != null ? inputField.text.Trim() : "";
        if (string.IsNullOrEmpty(cityName)) { UpdateInstructionsError($"Please enter a {NodeLabel().ToLower()} name!"); return; }
        GraphNode nodeToRemove = nodes.Find(n => n.cityName.Equals(cityName, System.StringComparison.OrdinalIgnoreCase));
        if (nodeToRemove == null) { UpdateInstructionsError($"{NodeLabel()} '{cityName}' not found!"); return; }

        List<GraphEdge> edgesToRemove = edges.Where(e => e.fromNode == nodeToRemove || e.toNode == nodeToRemove).ToList();
        foreach (var edge in edgesToRemove)
        {
            if (edge.fromNode != nodeToRemove) edge.fromNode.edges.Remove(edge);
            if (edge.toNode   != nodeToRemove) edge.toNode.edges.Remove(edge);
            if (edge.roadObject  != null) Destroy(edge.roadObject);
            if (edge.weightLabel != null) Destroy(edge.weightLabel);
            edges.Remove(edge);
        }
        if (nodeToRemove.nodeObject != null) Destroy(nodeToRemove.nodeObject);
        if (nodeToRemove.nameLabel  != null) Destroy(nodeToRemove.nameLabel);
        nodes.Remove(nodeToRemove);
        ARGraphLessonGuide guide = FindObjectOfType<ARGraphLessonGuide>();
        if (guide != null) guide.NotifyNodeRemoved();

        SetActive(inputPanel, false);
        ShowModeButtons();
        currentState = GraphState.Ready;
        UpdateInstructionsSuccess($"Removed {NodeLabel().ToLower()}: {cityName}");
        UpdateStatus();
        if (inputField != null) inputField.text = "";
        CheckAndUpdateButtonStates();
    }

    public void OnCancelButton()
    {
        if (previewNode   != null) { Destroy(previewNode);   previewNode   = null; }
        if (previewBeacon != null) { Destroy(previewBeacon); previewBeacon = null; }
        SetActive(movementButtonPanel,    false);
        SetActive(inputPanel,             false);
        SetActive(inputPanelEdge,         false);
        SetActive(pathCheckInputPanel,    false);
        SetActive(degreeInputPanel,       false);
        ShowModeButtons();
        currentState = GraphState.Ready;
        UpdateInstructions("Operation cancelled");
        if (inputField     != null) inputField.text     = "";
        if (inputFieldFrom != null) inputFieldFrom.text = "";
        if (inputFieldTo   != null) inputFieldTo.text   = "";
        SetActive(confirmButton, false);
        pendingCityName = "";
        CheckAndUpdateButtonStates();
    }

    void ShowModeButtons()
    {
        SetActive(mainButtonPanel, true);
        SetActive(beginnerButtonPanel,     currentDifficulty == DifficultyMode.Beginner);
        SetActive(intermediateButtonPanel, currentDifficulty == DifficultyMode.Intermediate);
        CheckAndUpdateButtonStates();
    }

    // ═════════════════════════════════════════════
    //  BFS
    // ═════════════════════════════════════════════
    public void OnBFSButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnBFSButtonClicked();
        if (currentState != GraphState.Ready || nodes.Count == 0) return;
        StartCoroutine(BreadthFirstSearch(nodes[0]));
    }

    IEnumerator BreadthFirstSearch(GraphNode start)
    {
        ResetNodeStates();
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                $"BFS Algorithm!\n\nBreadth-First Search\nTime: O(V + E)\n\n{BFSFlavour()}";
        Queue<GraphNode> queue = new Queue<GraphNode>();
        queue.Enqueue(start); start.visited = true;
        while (queue.Count > 0)
        {
            GraphNode current = queue.Dequeue();
            yield return StartCoroutine(AnimateNodeVisit(current));
            foreach (var edge in current.edges)
            {
                GraphNode neighbor = edge.toNode == current ? edge.fromNode : edge.toNode;
                if (!neighbor.visited)
                {
                    neighbor.visited = true;
                    queue.Enqueue(neighbor);
                    yield return StartCoroutine(AnimateEdgeTraversal(edge));
                }
            }
        }
        UpdateInstructionsSuccess("BFS Complete!");
        ARGraphLessonGuide guide = FindObjectOfType<ARGraphLessonGuide>();
        if (guide != null) guide.NotifyBFSCompleted();
    }

    // ═════════════════════════════════════════════
    //  DFS
    // ═════════════════════════════════════════════
    public void OnDFSButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnDFSButtonClicked();
        if (currentState != GraphState.Ready || nodes.Count == 0) return;
        StartCoroutine(DepthFirstSearch(nodes[0]));
    }

    IEnumerator DepthFirstSearch(GraphNode start)
    {
        ResetNodeStates();
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                $"DFS Algorithm!\n\nDepth-First Search\nTime: O(V + E)\n\n{DFSFlavour()}";
        yield return StartCoroutine(DFSRecursive(start));
        UpdateInstructionsSuccess("DFS Complete!");
        ARGraphLessonGuide guide = FindObjectOfType<ARGraphLessonGuide>();
        if (guide != null) guide.NotifyDFSCompleted();
    }

    IEnumerator DFSRecursive(GraphNode node)
    {
        node.visited = true;
        yield return StartCoroutine(AnimateNodeVisit(node));
        foreach (var edge in node.edges)
        {
            GraphNode neighbor = edge.toNode == node ? edge.fromNode : edge.toNode;
            if (!neighbor.visited)
            {
                yield return StartCoroutine(AnimateEdgeTraversal(edge));
                yield return StartCoroutine(DFSRecursive(neighbor));
            }
        }
    }

    // ═════════════════════════════════════════════
    //  DIJKSTRA
    // ═════════════════════════════════════════════
    public void OnDijkstraButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnDijkstraButtonClicked();
        if (currentState != GraphState.Ready || nodes.Count < 2) return;
        StartCoroutine(DijkstraShortestPath(nodes[0], nodes[nodes.Count - 1]));
    }

    IEnumerator DijkstraShortestPath(GraphNode start, GraphNode end)
    {
        ResetNodeStates();
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                $"Dijkstra's Algorithm!\n\nShortest Path Finding\nTime: O((V+E) log V)\n\n{DijkstraFlavour()}";

        foreach (var node in nodes) { node.distance = float.MaxValue; node.previous = null; }
        start.distance = 0;
        var unvisited = new List<GraphNode>(nodes);

        while (unvisited.Count > 0)
        {
            var current = unvisited.OrderBy(n => n.distance).First();
            unvisited.Remove(current); current.visited = true;
            yield return StartCoroutine(AnimateNodeVisit(current));
            if (current == end) break;
            if (current.distance == float.MaxValue) break;
            foreach (var edge in current.edges)
            {
                GraphNode neighbor = edge.toNode == current ? edge.fromNode : edge.toNode;
                if (!neighbor.visited)
                {
                    float alt = current.distance + edge.weight;
                    if (alt < neighbor.distance)
                    {
                        neighbor.distance = alt;
                        neighbor.previous = current;
                        yield return StartCoroutine(AnimateEdgeTraversal(edge));
                    }
                }
            }
        }
        yield return StartCoroutine(HighlightShortestPath(start, end));
        UpdateInstructionsSuccess($"Shortest path: {end.distance:F1}{EdgeUnit()}");
        ARGraphLessonGuide guide = FindObjectOfType<ARGraphLessonGuide>();
        if (guide != null) guide.NotifyDijkstraCompleted();
        PlaySound(pathFoundSound);
    }

    IEnumerator HighlightShortestPath(GraphNode start, GraphNode end)
    {
        List<GraphNode> path = new List<GraphNode>();
        GraphNode current = end;
        while (current != null) { path.Insert(0, current); current = current.previous; }
        foreach (var node in path)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "PathMarker";
            marker.transform.SetParent(node.nodeObject.transform);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale    = Vector3.one * 2.5f;
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0, 1, 0, 0.4f);
            marker.GetComponent<Renderer>().material = mat;
            Destroy(marker.GetComponent<Collider>());
            yield return new WaitForSeconds(pathAnimSpeed);
        }
    }

    // ═════════════════════════════════════════════
    //  PRIM'S MST
    // ═════════════════════════════════════════════
    public void OnMSTButton()
    {
        if (currentState != GraphState.Ready || nodes.Count < 2) return;
        if (edges.Count == 0) { UpdateInstructionsError($"No {EdgeLabel().ToLower()}s to build MST from!"); return; }
        StartCoroutine(PrimsMST());
    }

    IEnumerator PrimsMST()
    {
        currentState = GraphState.RunningAlgorithm;
        ResetNodeStates();
        foreach (var e in edges) e.inMST = false;
        string el = EdgeLabel(); string nl = NodeLabel();

        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                $"PRIM'S MST\n\nMinimum Spanning Tree\nTime: O(E log V)\n\n{MSTFlavour()}";

        UpdateInstructions("Running Prim's MST from: " + nodes[0].cityName);
        ARGraphLessonGuide guide = FindObjectOfType<ARGraphLessonGuide>();
        if (guide != null) guide.NotifyMSTCompleted();
        HashSet<GraphNode> inTree = new HashSet<GraphNode>();
        inTree.Add(nodes[0]); nodes[0].visited = true;
        yield return StartCoroutine(AnimateNodeVisit(nodes[0]));

        float totalCost = 0f; int edgesAdded = 0;
        while (inTree.Count < nodes.Count)
        {
            GraphEdge cheapest = null; float cheapestWeight = float.MaxValue;
            foreach (GraphNode treeNode in inTree)
                foreach (GraphEdge edge in treeNode.edges)
                {
                    GraphNode neighbor = edge.toNode == treeNode ? edge.fromNode : edge.toNode;
                    if (!inTree.Contains(neighbor) && edge.weight < cheapestWeight)
                    { cheapestWeight = edge.weight; cheapest = edge; }
                }
            if (cheapest == null) break;
            GraphNode newNode = inTree.Contains(cheapest.fromNode) ? cheapest.toNode : cheapest.fromNode;
            inTree.Add(newNode); newNode.visited = true;
            cheapest.inMST = true; totalCost += cheapest.weight; edgesAdded++;
            cheapest.roadObject.GetComponent<Renderer>().material.color = new Color(0, 0.8f, 0);
            UpdateInstructions($"Adding '{newNode.cityName}' - cost: {cheapest.weight:F0}{EdgeUnit()}");
            PlaySound(addEdgeSound);
            yield return StartCoroutine(AnimateNodeVisit(newNode));
            yield return new WaitForSeconds(0.4f);
        }

        foreach (var e in edges)
            if (!e.inMST && e.roadObject != null)
                e.roadObject.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);

        UpdateInstructionsSuccess($"MST Complete! Total cost: {totalCost:F0}{EdgeUnit()}, {edgesAdded} {el.ToLower()}s");
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                $"MST Complete!\n\nGreen {el.ToLower()}s = MST\nGrey {el.ToLower()}s = excluded\n\n" +
                $"Total cost: {totalCost:F0}{EdgeUnit()}\n{el}s used: {edgesAdded}\n\n" +
                $"Minimum cost to connect\nall {nodes.Count} {nl.ToLower()}s!";

        PlaySound(pathFoundSound);
        currentState = GraphState.Ready;
        ShowModeButtons();
    }

    // ═════════════════════════════════════════════
    //  PATH CHECK
    // ═════════════════════════════════════════════
    public void OnPathCheckButton()
    {
        if (currentState != GraphState.Ready || nodes.Count < 2) return;
        SetActive(mainButtonPanel, false);
        SetActive(intermediateButtonPanel, false);
        SetActive(pathCheckInputPanel, true);
        UpdateInstructions($"Enter two {NodeLabel().ToLower()} names to check if a path exists");
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                $"PATH CHECK  O(V+E)\n\nDoes a route exist\nbetween two {NodeLabel().ToLower()}s?\n\nUses BFS internally.\nResult: Yes or No!";
    }

    public void OnConfirmPathCheck()
    {
        if (pathCheckFrom == null || pathCheckTo == null) return;
        string fromName = pathCheckFrom.text.Trim();
        string toName   = pathCheckTo.text.Trim();
        if (string.IsNullOrEmpty(fromName) || string.IsNullOrEmpty(toName))
        { UpdateInstructionsError($"Enter both {NodeLabel().ToLower()} names!"); return; }

        GraphNode fromNode = nodes.Find(n => n.cityName.Equals(fromName, System.StringComparison.OrdinalIgnoreCase));
        GraphNode toNode   = nodes.Find(n => n.cityName.Equals(toName,   System.StringComparison.OrdinalIgnoreCase));
        if (fromNode == null) { UpdateInstructionsError($"'{fromName}' not found!"); return; }
        if (toNode   == null) { UpdateInstructionsError($"'{toName}' not found!");   return; }

        SetActive(pathCheckInputPanel, false);
        if (pathCheckFrom != null) pathCheckFrom.text = "";
        if (pathCheckTo   != null) pathCheckTo.text   = "";
        StartCoroutine(AnimatedPathCheck(fromNode, toNode));
    }

    IEnumerator AnimatedPathCheck(GraphNode start, GraphNode end)
    {
        currentState = GraphState.RunningAlgorithm;
        ResetNodeStates();
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                $"PATH CHECK\n\nFrom: {start.cityName}\nTo: {end.cityName}\n\nRunning BFS...\nLooking for connection!";
        UpdateInstructions($"Checking: Is there a route from {start.cityName} to {end.cityName}?");
        yield return new WaitForSeconds(0.5f);

        Queue<GraphNode> queue = new Queue<GraphNode>();
        queue.Enqueue(start); start.visited = true;
        bool found = false;

        while (queue.Count > 0)
        {
            GraphNode current = queue.Dequeue();
            GameObject highlight = CreatePulseMarker(current, new Color(0.5f, 0.5f, 1f, 0.4f));
            UpdateInstructions($"Visiting: {current.cityName}...");
            PlaySound(traverseSound);
            yield return new WaitForSeconds(0.5f);
            Destroy(highlight);
            if (current == end) { found = true; break; }
            foreach (var edge in current.edges)
            {
                GraphNode neighbor = edge.toNode == current ? edge.fromNode : edge.toNode;
                if (!neighbor.visited)
                {
                    neighbor.visited = true;
                    queue.Enqueue(neighbor);
                    yield return StartCoroutine(AnimateEdgeTraversal(edge));
                }
            }
        }

        if (found)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject g = CreatePulseMarker(end, new Color(0, 1, 0, 0.6f));
                PlaySound(pathFoundSound);
                yield return new WaitForSeconds(0.3f);
                Destroy(g);
                yield return new WaitForSeconds(0.15f);
            }
            UpdateInstructionsSuccess($"PATH EXISTS: {start.cityName} -> {end.cityName}!");
            ARGraphLessonGuide guide = FindObjectOfType<ARGraphLessonGuide>();
            if (guide != null) guide.NotifyPathCheckCompleted(true);
            if (operationInfoText != null && !_silenceInstructions)
                operationInfoText.text =
                    $"PATH EXISTS!\n\n{start.cityName} can reach\n{end.cityName}\n\nThey are CONNECTED.\nTime: O(V+E)";
        }
        else
        {
            UpdateInstructionsError($"NO PATH: {start.cityName} cannot reach {end.cityName}");
            ARGraphLessonGuide guide = FindObjectOfType<ARGraphLessonGuide>();
            if (guide != null) guide.NotifyPathCheckCompleted(false);
            if (operationInfoText != null && !_silenceInstructions)
                operationInfoText.text =
                    $"NO PATH!\n\n{start.cityName} CANNOT reach\n{end.cityName}\n\nThey are DISCONNECTED.\nTime: O(V+E)";
        }

        currentState = GraphState.Ready;
        ShowModeButtons();
    }

    // ═════════════════════════════════════════════
    //  NODE DEGREE
    // ═════════════════════════════════════════════
    public void OnDegreeButton()
    {
        if (currentState != GraphState.Ready || nodes.Count == 0) return;
        SetActive(mainButtonPanel, false);
        SetActive(intermediateButtonPanel, false);
        SetActive(degreeInputPanel, true);
        UpdateInstructions($"Enter a {NodeLabel().ToLower()} name to see its degree");
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                $"NODE DEGREE\n\nDegree = number of {EdgeLabel().ToLower()}s\nconnected to a vertex.\n\n" +
                $"High degree = hub {NodeLabel().ToLower()}!\nLow degree = peripheral.";
    }

    public void OnConfirmDegree()
    {
        if (degreeInputField == null || string.IsNullOrEmpty(degreeInputField.text))
        { UpdateInstructionsError($"Enter a {NodeLabel().ToLower()} name!"); return; }
        string name = degreeInputField.text.Trim();
        GraphNode node = nodes.Find(n => n.cityName.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        if (node == null) { UpdateInstructionsError($"{NodeLabel()} '{name}' not found!"); return; }
        SetActive(degreeInputPanel, false);
        if (degreeInputField != null) degreeInputField.text = "";
        StartCoroutine(AnimatedDegree(node));
    }

    IEnumerator AnimatedDegree(GraphNode node)
    {
        currentState = GraphState.RunningAlgorithm;
        GameObject mainH = CreatePulseMarker(node, new Color(1, 0.8f, 0, 0.6f));
        PlaySound(traverseSound);
        yield return new WaitForSeconds(0.5f);

        int degree = 0;
        List<string> neighbours = new List<string>();
        HashSet<GraphEdge> uniqueEdges = new HashSet<GraphEdge>(node.edges);
        foreach (GraphEdge edge in uniqueEdges)
        {
            degree++;
            GraphNode neighbor = edge.toNode == node ? edge.fromNode : edge.toNode;
            neighbours.Add(neighbor.cityName);
            Color orig = edge.roadObject.GetComponent<Renderer>().material.color;
            edge.roadObject.GetComponent<Renderer>().material.color = new Color(1, 0.8f, 0);
            GameObject nH = CreatePulseMarker(neighbor, new Color(1, 0.5f, 0, 0.4f));
            UpdateInstructions($"{EdgeLabel()} to: {neighbor.cityName}");
            yield return new WaitForSeconds(0.6f);
            edge.roadObject.GetComponent<Renderer>().material.color = orig;
            Destroy(nH);
        }
        Destroy(mainH);

        string neighbourList = string.Join(", ", neighbours);
        string nl = NodeLabel();
        UpdateInstructionsSuccess($"'{node.cityName}' has degree {degree} - connected to: {neighbourList}");
        ARGraphLessonGuide guide = FindObjectOfType<ARGraphLessonGuide>();
        if (guide != null) guide.NotifyDegreeChecked();
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                $"DEGREE of '{node.cityName}'\n\n" +
                $"Degree: {degree}\nNeighbours: {neighbourList}\n\n" +
                $"Degree > avg = hub {nl.ToLower()}!\nTime: O(degree) = O(E)";

        currentState = GraphState.Ready;
        ShowModeButtons();
    }

    // ═════════════════════════════════════════════
    //  ANIMATION HELPERS
    // ═════════════════════════════════════════════
    IEnumerator AnimateNodeVisit(GraphNode node)
    {
        GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pulse.transform.SetParent(node.nodeObject.transform);
        pulse.transform.localPosition = Vector3.zero;
        pulse.transform.localScale    = Vector3.one * 2f;
        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(1, 1, 0, 0.5f);
        pulse.GetComponent<Renderer>().material = mat;
        Destroy(pulse.GetComponent<Collider>());
        PlaySound(traverseSound);
        // Only update live traversal text when not silenced
        if (!_silenceInstructions) UpdateInstructions($"Visiting: {node.cityName}");
        yield return new WaitForSeconds(0.8f);
        Destroy(pulse);
    }

    IEnumerator AnimateEdgeTraversal(GraphEdge edge)
    {
        Color orig = edge.roadObject.GetComponent<Renderer>().material.color;
        edge.roadObject.GetComponent<Renderer>().material.color = Color.yellow;
        yield return new WaitForSeconds(0.3f);
        edge.roadObject.GetComponent<Renderer>().material.color = orig;
    }

    GameObject CreatePulseMarker(GraphNode node, Color color)
    {
        GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pulse.transform.SetParent(node.nodeObject.transform);
        pulse.transform.localPosition = Vector3.zero;
        pulse.transform.localScale    = Vector3.one * 2.2f;
        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        pulse.GetComponent<Renderer>().material = mat;
        Destroy(pulse.GetComponent<Collider>());
        return pulse;
    }

    void ResetNodeStates()
    {
        foreach (var node in nodes)
        {
            node.visited  = false;
            node.distance = float.MaxValue;
            node.previous = null;
            Transform pm = node.nodeObject.transform.Find("PathMarker");
            if (pm != null) Destroy(pm.gameObject);
        }
    }

    // ═════════════════════════════════════════════
    //  ANALYSIS TOGGLE
    // ═════════════════════════════════════════════
    public void OnAnalysisToggleButton()
    {
        if (algorithmPanel != null) algorithmPanel.SetActive(!algorithmPanel.activeSelf);
    }

    // ═════════════════════════════════════════════
    //  RESET
    // ═════════════════════════════════════════════
    public void OnResetButton()
    {
        if (swipeRotation       != null) swipeRotation.ResetRotation();
        if (tutorialIntegration != null) tutorialIntegration.OnResetButtonClicked();
        if (zoomController      != null) zoomController.ResetZoom();
        StopAllCoroutines();
        pulseCoroutine = null;

        if (graphScene    != null) { Destroy(graphScene);    graphScene    = null; }
        if (previewNode   != null) { Destroy(previewNode);   previewNode   = null; }
        if (previewBeacon != null) { Destroy(previewBeacon); previewBeacon = null; }

        nodes.Clear(); edges.Clear(); originalButtonColors.Clear();
        sceneSpawned              = false;
        nodeIdCounter             = 1;
        canonicalPrefabLocalScale = Vector3.zero;
        currentDifficulty         = DifficultyMode.None;
        currentScenario           = ScenarioMode.None;
        pendingCityName           = "";
        _silenceInstructions      = false;  // always restore on reset

        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
                if (plane?.gameObject != null) plane.gameObject.SetActive(false);
        }
        if (raycastManager != null) raycastManager.enabled = false;

        HideAllPanels();
        UpdateStatus();
        ShowScenarioPanel();
    }

    // ═════════════════════════════════════════════
    //  UTILITIES
    // ═════════════════════════════════════════════
    void UpdateStatus()
    {
        if (statusText != null && !_silenceInstructions)
            statusText.text = $"{NodeLabel()}s: {nodes.Count} | {EdgeLabel()}s: {edges.Count}";
    }

    void PlaySound(AudioClip clip) { if (audioSource != null && clip != null) audioSource.PlayOneShot(clip); }
    void SetActive(GameObject obj, bool active) { if (obj != null) obj.SetActive(active); }
}