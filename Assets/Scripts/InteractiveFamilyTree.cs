using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class TreeNodeIdentifier : MonoBehaviour
{
    public object nodeReference;
}

/// <summary>
/// InteractiveFamilyTree v7.0
/// THREE scenarios (admin-selectable via scenarios.html):
///   SCENARIO A - Family Tree   (coloured person spheres)
///   SCENARIO B - Fruit Tree    (fruit-shaped nodes on a brown trunk)
///   SCENARIO C - Forest Trail  (amber waypoints + wooden signposts on mossy ground)
/// Flow: AR Placement -> Scenario Panel -> Difficulty Panel -> Play
/// ScenarioManager calls OnScenarioChosen(ScenarioMode) to select programmatically.
/// </summary>
public class InteractiveFamilyTree : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  AR COMPONENTS
    // ─────────────────────────────────────────────
    [Header("AR Components")]
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;
    public Camera arCamera;

    [Header("Zoom Controller")]
    public SceneZoomController zoomController;

    [Header("Plane Visualization")]
    public GameObject planePrefab;

    [Header("Custom Assets (Optional)")]
    public GameObject personPrefab;

    // ─────────────────────────────────────────────
    //  UI – CORE
    // ─────────────────────────────────────────────
    [Header("UI References")]
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI detectionText;
    public TextMeshProUGUI operationInfoText;

    public GameObject mainButtonPanel;
    public GameObject personInputPanel;
    public TMP_InputField nameInputField;
    public GameObject traversalPanel;
    public GameObject confirmButton;
    public GameObject explanationPanel;
    public GameObject directionPanel;

    // ─────────────────────────────────────────────
    //  UI – SCENARIO PANEL
    // ─────────────────────────────────────────────
    [Header("Scenario Panel")]
    public GameObject scenarioPanel;
    public UnityEngine.UI.Button familyTreeBtn;
    public UnityEngine.UI.Button fruitTreeBtn;
    public UnityEngine.UI.Button forestTrailBtn;

    // ─────────────────────────────────────────────
    //  UI – DIFFICULTY PANEL
    // ─────────────────────────────────────────────
    [Header("Difficulty Panel")]
    public GameObject difficultyPanel;
    public UnityEngine.UI.Button beginnerBtn;
    public UnityEngine.UI.Button intermediateBtn;

    // ─────────────────────────────────────────────
    //  UI – MODE PANELS
    // ─────────────────────────────────────────────
    [Header("Mode Button Panels")]
    public GameObject beginnerButtonPanel;
    public GameObject intermediateButtonPanel;

    // ─────────────────────────────────────────────
    //  UI – INTERMEDIATE
    // ─────────────────────────────────────────────
    [Header("Intermediate UI")]
    public GameObject searchInputPanel;
    public TMP_InputField searchNameField;
    public GameObject deleteInputPanel;
    public TMP_InputField deleteNameField;

    // ─────────────────────────────────────────────
    //  ACTION BUTTONS — Beginner
    // ─────────────────────────────────────────────
    [Header("Beginner Action Buttons")]
    public UnityEngine.UI.Button beginnerAddChildButton;
    public UnityEngine.UI.Button beginnerInOrderButton;
    public UnityEngine.UI.Button beginnerPreOrderButton;
    public UnityEngine.UI.Button beginnerPostOrderButton;

    // ─────────────────────────────────────────────
    //  ACTION BUTTONS — Intermediate
    // ─────────────────────────────────────────────
    [Header("Intermediate Action Buttons")]
    public UnityEngine.UI.Button intermediateAddChildButton;
    public UnityEngine.UI.Button intermediateSearchButton;
    public UnityEngine.UI.Button intermediateDeleteButton;
    public UnityEngine.UI.Button intermediateHeightButton;

    // ─────────────────────────────────────────────
    //  AUDIO
    // ─────────────────────────────────────────────
    [Header("Audio")]
    public AudioClip placeSceneSound;
    public AudioClip addPersonSound;
    public AudioClip traverseSound;
    public AudioClip highlightSound;
    public AudioClip foundSound;

    private AudioSource audioSource;

    // ─────────────────────────────────────────────
    //  TREE SETTINGS
    // ─────────────────────────────────────────────
    [Header("Tree Settings")]
    public float verticalSpacing   = 0.15f;
    public float horizontalSpacing = 0.12f;
    public float personSize        = 0.05f;
    public float branchThickness   = 0.008f;
    public float sceneHeightOffset = 0.3f;
    public float moveIncrement     = 0.05f;
    public float snapDistance      = 0.03f;

    // ─────────────────────────────────────────────
    //  OTHER COMPONENTS
    // ─────────────────────────────────────────────
    [Header("Tutorial System")]
    public TreeTutorialIntegration tutorialIntegration;

    [Header("Swipe Rotation")]
    public SwipeRotation swipeRotation;

    // =========================================================================
    //  ENUMS & STATE
    // =========================================================================

    public enum ScenarioMode { None, FamilyTree, FruitTree, ForestTrail }

    private enum DifficultyMode { None, Beginner, Intermediate }

    private enum TreeState
    {
        WaitingForPlane,
        ChoosingScenario,
        ChoosingDifficulty,
        Ready,
        PositioningChild,
        Searching,
        Deleting,
        ComputingHeight
    }

    private ScenarioMode   currentScenario   = ScenarioMode.None;
    private DifficultyMode currentDifficulty = DifficultyMode.None;
    private TreeState      currentState      = TreeState.WaitingForPlane;

    // ─────────────────────────────────────────────
    //  COLOUR PALETTES
    // ─────────────────────────────────────────────
    private Color[] familyColors = new Color[]
    {
        new Color(0.8f,  0.3f,  0.3f),
        new Color(0.3f,  0.6f,  0.9f),
        new Color(0.3f,  0.8f,  0.4f),
        new Color(1f,    0.8f,  0.3f),
        new Color(0.9f,  0.5f,  0.9f)
    };

    private Color[] fruitColors = new Color[]
    {
        new Color(0.9f,  0.15f, 0.15f),
        new Color(1f,    0.6f,  0.0f),
        new Color(0.9f,  0.85f, 0.1f),
        new Color(0.55f, 0.15f, 0.7f),
        new Color(0.2f,  0.75f, 0.2f),
    };

    private Color[] trailColors = new Color[]
    {
        new Color(0.90f, 0.65f, 0.10f),
        new Color(0.95f, 0.40f, 0.10f),
        new Color(0.80f, 0.20f, 0.20f),
        new Color(0.60f, 0.35f, 0.10f),
        new Color(0.95f, 0.85f, 0.20f),
    };

    // ─────────────────────────────────────────────
    //  TREE DATA STRUCTURE
    // ─────────────────────────────────────────────
    private class TreeNode
    {
        public GameObject personObject;
        public GameObject branchToParent;
        public string     name;
        public TreeNode   leftChild;
        public TreeNode   rightChild;
        public TreeNode   parent;
        public int        level;
        public float      xPosition;
        public GameObject nameLabel;
        public GameObject leftIndicator;
        public GameObject rightIndicator;
    }

    // ─────────────────────────────────────────────
    //  RUNTIME FIELDS
    // ─────────────────────────────────────────────
    private TreeNode   root;
    private GameObject treeScene;
    private GameObject trunkObject;
    private GameObject trailObject;
    private GameObject previewNode;
    private Vector3    previewPosition;
    private string     pendingChildName;
    private TreeNode   selectedParentNode;
    private bool       placingLeftChild;
    private bool       sceneSpawned = false;
    private TreeNode   snappedParent = null;
    private bool       snappedIsLeft = false;

    // ─────────────────────────────────────────────
    //  BUTTON STATE TRACKING
    // ─────────────────────────────────────────────
    private Dictionary<UnityEngine.UI.Button, Color> originalButtonColors
        = new Dictionary<UnityEngine.UI.Button, Color>();
    private Coroutine pulseCoroutine = null;

    private static readonly Color PulseBaseColor  = new Color(0.518f, 0.412f, 1.000f);
    private static readonly Color PulseLightColor = new Color(0.698f, 0.627f, 1.000f);

    private bool _started = false;

    // ─────────────────────────────────────────────
    //  INSTRUCTION SILENCE (used by ARTreeLessonGuide)
    // ─────────────────────────────────────────────
    private bool _silenceInstructions = false;

    /// <summary>
    /// Called by ARTreeLessonGuide to stop InteractiveFamilyTree from
    /// overwriting the guide's own instruction/step text.
    /// Pass true to hide the panels; false to restore them for sandbox use.
    /// </summary>
    public void SetInstructionSilence(bool silent)
    {
        _silenceInstructions = silent;
        if (instructionText   != null) instructionText.gameObject.SetActive(!silent);
        if (operationInfoText != null) operationInfoText.gameObject.SetActive(!silent);
        if (detectionText     != null) detectionText.gameObject.SetActive(!silent);
        if (statusText        != null) statusText.gameObject.SetActive(!silent);
    }

    // =========================================================================
    //  START
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

        if (planeManager   != null) planeManager.enabled   = true;
        if (raycastManager != null) raycastManager.enabled = true;

        HideAllPanels();
        WireUpButtons();

        UpdateInstructions("Point camera at a flat surface (floor or table)");

        if (detectionText != null)
        {
            detectionText.text  = "Looking for surfaces...";
            detectionText.color = Color.yellow;
        }

        Debug.Log("InteractiveFamilyTree v7.0 (FamilyTree + FruitTree + ForestTrail) Loaded!");
    }

    // ─────────────────────────────────────────────
    //  PUBLIC API — used by ARTreeLessonGuide & ARTreeLessonAssessment
    // ─────────────────────────────────────────────
    public int GetNodeCount() => CountNodes(root);
    public int GetTreeHeight() => ComputeHeight(root);

    int ComputeHeight(TreeNode node)
    {
        if (node == null) return 0;
        return 1 + Mathf.Max(ComputeHeight(node.leftChild), ComputeHeight(node.rightChild));
    }

    // ─────────────────────────────────────────────
    //  BUTTON WIRING
    // ─────────────────────────────────────────────
    void WireUpButtons()
    {
        if (familyTreeBtn  != null) familyTreeBtn.onClick.AddListener(OnSelectFamilyTree);
        if (fruitTreeBtn   != null) fruitTreeBtn.onClick.AddListener(OnSelectFruitTree);
        if (forestTrailBtn != null) forestTrailBtn.onClick.AddListener(OnSelectForestTrail);
        if (beginnerBtn    != null) beginnerBtn.onClick.AddListener(OnSelectBeginner);
        if (intermediateBtn != null) intermediateBtn.onClick.AddListener(OnSelectIntermediate);
    }

    // ─────────────────────────────────────────────
    //  PANEL HELPERS
    // ─────────────────────────────────────────────
    void HideAllPanels()
    {
        SetActive(scenarioPanel,           false);
        SetActive(difficultyPanel,         false);
        SetActive(mainButtonPanel,         false);
        SetActive(beginnerButtonPanel,     false);
        SetActive(intermediateButtonPanel, false);
        SetActive(personInputPanel,        false);
        SetActive(traversalPanel,          false);
        SetActive(confirmButton,           false);
        SetActive(explanationPanel,        false);
        SetActive(directionPanel,          false);
        SetActive(searchInputPanel,        false);
        SetActive(deleteInputPanel,        false);
    }

    void ShowModeButtons()
    {
        SetActive(mainButtonPanel,         true);
        SetActive(beginnerButtonPanel,     currentDifficulty == DifficultyMode.Beginner);
        SetActive(intermediateButtonPanel, currentDifficulty == DifficultyMode.Intermediate);
        CheckAndUpdateButtonStates();
    }

    // =========================================================================
    //  BUTTON STATE MANAGEMENT
    // =========================================================================
    void CheckAndUpdateButtonStates()
    {
        bool hasChildren = CountNodes(root) > 1;
        bool canDelete   = CountNodes(root) > 1;

        if (currentDifficulty == DifficultyMode.Beginner)
        {
            SetButtonInteractable(beginnerInOrderButton,   hasChildren);
            SetButtonInteractable(beginnerPreOrderButton,  hasChildren);
            SetButtonInteractable(beginnerPostOrderButton, hasChildren);
        }
        else if (currentDifficulty == DifficultyMode.Intermediate)
        {
            SetButtonInteractable(intermediateSearchButton, hasChildren);
            SetButtonInteractable(intermediateDeleteButton, canDelete);
            SetButtonInteractable(intermediateHeightButton, hasChildren);
        }

        if (!hasChildren)
        {
            if (pulseCoroutine == null)
            {
                UnityEngine.UI.Button addBtn = currentDifficulty == DifficultyMode.Beginner
                    ? beginnerAddChildButton
                    : intermediateAddChildButton;
                if (addBtn != null)
                    pulseCoroutine = StartCoroutine(PulseAddButton(addBtn));
            }
        }
        else
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
                ResetPulseButton(beginnerAddChildButton);
                ResetPulseButton(intermediateAddChildButton);
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

    IEnumerator PulseAddButton(UnityEngine.UI.Button btn)
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

    // =========================================================================
    //  INSTRUCTION HELPERS — all gated by _silenceInstructions
    // =========================================================================
    void UpdateInstructions(string message)
    {
        if (_silenceInstructions) return;
        if (instructionText != null) { instructionText.text = message; instructionText.color = Color.white; }
    }

    void UpdateInstructionsSuccess(string message)
    {
        if (_silenceInstructions) return;
        if (instructionText != null) { instructionText.text = message; instructionText.color = new Color(0.2f, 1f, 0.4f); }
    }

    void UpdateInstructionsError(string message)
    {
        if (_silenceInstructions) return;
        if (instructionText != null) { instructionText.text = message; instructionText.color = new Color(1f, 0.3f, 0.3f); }
    }

    // =========================================================================
    //  UPDATE
    // =========================================================================
    void Update()
    {
        if (currentState == TreeState.WaitingForPlane)
            DetectPlaneInteraction();
        else if (currentState == TreeState.PositioningChild)
            UpdatePreviewNodeSnapping();
    }

    // =========================================================================
    //  PUBLIC ENTRY POINT — called by ScenarioManager
    // =========================================================================
    public void OnScenarioChosen(ScenarioMode mode)
    {
        switch (mode)
        {
            case ScenarioMode.FamilyTree:  OnSelectFamilyTree();  break;
            case ScenarioMode.FruitTree:   OnSelectFruitTree();   break;
            case ScenarioMode.ForestTrail: OnSelectForestTrail(); break;
            default:
                Debug.LogWarning($"[FamilyTree] Unknown ScenarioMode '{mode}' — defaulting to FamilyTree.");
                OnSelectFamilyTree();
                break;
        }
    }

    // =========================================================================
    //  SCENARIO SELECTION
    // =========================================================================
    public void OnSelectFamilyTree()
    {
        currentScenario = ScenarioMode.FamilyTree;
        SetActive(scenarioPanel, false);
        if (trunkObject != null) trunkObject.SetActive(false);
        if (trailObject != null) trailObject.SetActive(false);
        RefreshRootNodeAppearance();
        SetActive(difficultyPanel, true);
        UpdateInstructions("Family Tree selected! Choose difficulty.");
        if (operationInfoText != null)
            operationInfoText.text =
                "FAMILY TREE\n\n" +
                "Each sphere = a family member.\n" +
                "Left child = younger sibling.\n" +
                "Right child = older sibling.\n\n" +
                "Colour = generation level!";
    }

    public void OnSelectFruitTree()
    {
        currentScenario = ScenarioMode.FruitTree;
        SetActive(scenarioPanel, false);
        if (trunkObject != null) trunkObject.SetActive(true);
        if (trailObject != null) trailObject.SetActive(false);
        RefreshRootNodeAppearance();
        SetActive(difficultyPanel, true);
        UpdateInstructions("Fruit Tree selected! Choose difficulty.");
        if (operationInfoText != null)
            operationInfoText.text =
                "FRUIT TREE\n\n" +
                "Each fruit = a tree node.\n" +
                "Grows downward from trunk.\n" +
                "Left / Right branches!\n\n" +
                "Colour = fruit type / level!";
    }

    public void OnSelectForestTrail()
    {
        currentScenario = ScenarioMode.ForestTrail;
        SetActive(scenarioPanel, false);
        if (trunkObject != null) trunkObject.SetActive(false);
        if (trailObject != null) trailObject.SetActive(true);
        RefreshRootNodeAppearance();
        SetActive(difficultyPanel, true);
        UpdateInstructions("Forest Trail selected! Choose difficulty.");
        if (operationInfoText != null)
            operationInfoText.text =
                "FOREST TRAIL\n\n" +
                "Waypoints = BST nodes.\n" +
                "Signpost arrow = child pointer.\n" +
                "Plan your hiking route!\n\n" +
                "Left = west fork  |  Right = east fork";
    }

    void RefreshRootNodeAppearance()
    {
        if (root == null || treeScene == null) return;
        foreach (Transform child in root.personObject.transform)
        {
            if (child.name == "VisualSphere" || child.name == "VisualFruit" ||
                child.name == "VisualWaypoint" || child.name == "Post" ||
                child.name == "Sign" || child.name == "Stem" || child.name == "Leaf")
                Destroy(child.gameObject);
        }
        AddVisualToPersonObject(root.personObject, root.name, root.level);
    }

    // =========================================================================
    //  DIFFICULTY SELECTION
    // =========================================================================
    public void OnSelectBeginner()
    {
        currentDifficulty = DifficultyMode.Beginner;
        SetActive(difficultyPanel,         false);
        SetActive(mainButtonPanel,         true);
        SetActive(beginnerButtonPanel,     true);
        SetActive(intermediateButtonPanel, false);
        SetActive(explanationPanel,        true);
        currentState = TreeState.Ready;

        string sn = ScenarioDisplayName();
        UpdateInstructions($"Beginner - {sn} - Add and traverse!");
        if (operationInfoText != null)
            operationInfoText.text =
                $"BEGINNER - {sn}\n\n" +
                "ADD CHILD  - Place node    O(log n)\n" +
                "IN-ORDER   - Left->Root->Right\n" +
                "PRE-ORDER  - Root->Left->Right\n" +
                "POST-ORDER - Left->Right->Root\n\n" +
                "Each node can have up to 2 children!";
        CheckAndUpdateButtonStates();
    }

    public void OnSelectIntermediate()
    {
        currentDifficulty = DifficultyMode.Intermediate;
        SetActive(difficultyPanel,         false);
        SetActive(mainButtonPanel,         true);
        SetActive(beginnerButtonPanel,     false);
        SetActive(intermediateButtonPanel, true);
        SetActive(explanationPanel,        true);
        currentState = TreeState.Ready;

        string sn = ScenarioDisplayName();
        UpdateInstructions($"Intermediate - {sn} - Search, Delete, Height!");
        if (operationInfoText != null)
            operationInfoText.text =
                $"INTERMEDIATE - {sn}\n\n" +
                "ADD CHILD  - Place node    O(log n)\n" +
                "SEARCH     - Find by name  O(n)\n" +
                "DELETE     - Remove node   O(n)\n" +
                "HEIGHT     - Tree height   O(n)\n\n" +
                "Think recursion and tree properties!";
        CheckAndUpdateButtonStates();
    }

    string ScenarioDisplayName()
    {
        switch (currentScenario)
        {
            case ScenarioMode.FruitTree:   return "Fruit Tree";
            case ScenarioMode.ForestTrail: return "Forest Trail";
            default:                       return "Family Tree";
        }
    }

    // =========================================================================
    //  AR PLANE DETECTION & SCENE SPAWN
    // =========================================================================
    void DetectPlaneInteraction()
    {
        if (sceneSpawned) return;
        bool inputReceived = false;
        Vector2 screenPosition = Vector2.zero;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Touch touch = Input.GetTouch(0);
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;
            screenPosition = touch.position;
            inputReceived = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;
            screenPosition = Input.mousePosition;
            inputReceived = true;
        }

        if (!inputReceived || raycastManager == null) return;

        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            SpawnTreeScene(hits[0].pose.position, hits[0].pose.rotation);
    }

    void SpawnTreeScene(Vector3 position, Quaternion rotation)
    {
        sceneSpawned = true;
        PlaySound(placeSceneSound);

        if (planeManager != null)
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);

        treeScene = new GameObject("FamilyTree");
        treeScene.transform.position = position + Vector3.up * sceneHeightOffset;
        treeScene.transform.rotation = rotation;

        if (swipeRotation  != null) swipeRotation.InitializeRotation(treeScene.transform);
        if (zoomController != null) zoomController.InitializeZoom(treeScene.transform);

        CreateSceneryDecoration();
        CreateInitialRoot();

        currentState = TreeState.ChoosingScenario;
        SetActive(scenarioPanel, true);
        UpdateInstructions("Choose your scenario!");

        if (detectionText != null)
        {
            detectionText.text  = "Scene Placed!";
            detectionText.color = Color.green;
        }

        if (tutorialIntegration != null)
            Invoke(nameof(ShowWelcomeTutorialDelayed), 1f);

        UpdateStatus();
    }

    void ShowWelcomeTutorialDelayed()
    {
        if (tutorialIntegration != null) tutorialIntegration.ShowWelcomeTutorial();
    }

    // ─────────────────────────────────────────────
    //  SCENERY
    // ─────────────────────────────────────────────
    void CreateSceneryDecoration()
    {
        // Fruit Tree trunk
        trunkObject = new GameObject("TreeTrunk");
        trunkObject.transform.SetParent(treeScene.transform);

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(trunkObject.transform);
        trunk.transform.localPosition = new Vector3(0, 0.28f, 0);
        trunk.transform.localScale    = new Vector3(0.03f, 0.3f, 0.03f);
        Destroy(trunk.GetComponent<Collider>());
        var trunkMat = new Material(Shader.Find("Standard"));
        trunkMat.color = new Color(0.4f, 0.22f, 0.06f);
        trunk.GetComponent<Renderer>().material = trunkMat;

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.transform.SetParent(trunkObject.transform);
        canopy.transform.localPosition = new Vector3(0, 0.62f, 0);
        canopy.transform.localScale    = Vector3.one * 0.18f;
        Destroy(canopy.GetComponent<Collider>());
        var canopyMat = new Material(Shader.Find("Standard"));
        canopyMat.color = new Color(0.15f, 0.55f, 0.1f, 0.7f);
        canopy.GetComponent<Renderer>().material = canopyMat;

        trunkObject.SetActive(false);

        CreateForestTrailDecoration();
    }

    void CreateForestTrailDecoration()
    {
        trailObject = new GameObject("ForestTrail");
        trailObject.transform.SetParent(treeScene.transform);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ground.transform.SetParent(trailObject.transform);
        ground.transform.localPosition = new Vector3(0, -0.01f, 0);
        ground.transform.localScale    = new Vector3(0.55f, 0.004f, 0.55f);
        Destroy(ground.GetComponent<Collider>());
        var gmat = new Material(Shader.Find("Standard"));
        gmat.color = new Color(0.22f, 0.52f, 0.18f);
        ground.GetComponent<Renderer>().material = gmat;

        Vector3[] pinePos = {
            new Vector3(-0.22f, 0f, -0.18f),
            new Vector3( 0.22f, 0f, -0.18f),
            new Vector3( 0f,    0f,  0.24f),
        };
        foreach (var pos in pinePos)
        {
            GameObject pine = new GameObject("Pine");
            pine.transform.SetParent(trailObject.transform);
            pine.transform.localPosition = pos;

            GameObject pt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pt.transform.SetParent(pine.transform);
            pt.transform.localPosition = new Vector3(0, 0.04f, 0);
            pt.transform.localScale    = new Vector3(0.012f, 0.04f, 0.012f);
            Destroy(pt.GetComponent<Collider>());
            var tmat = new Material(Shader.Find("Standard"));
            tmat.color = new Color(0.38f, 0.20f, 0.07f);
            pt.GetComponent<Renderer>().material = tmat;

            GameObject pc = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pc.transform.SetParent(pine.transform);
            pc.transform.localPosition = new Vector3(0, 0.11f, 0);
            pc.transform.localScale    = new Vector3(0.06f, 0.09f, 0.06f);
            Destroy(pc.GetComponent<Collider>());
            var cmat = new Material(Shader.Find("Standard"));
            cmat.color = new Color(0.12f, 0.42f, 0.12f);
            pc.GetComponent<Renderer>().material = cmat;
        }

        trailObject.SetActive(false);
    }

    void CreateInitialRoot()
    {
        root = new TreeNode { name = "Root", level = 0, xPosition = 0 };
        root.personObject = BuildPersonObject(root.name, root.level);
        var id = root.personObject.GetComponent<TreeNodeIdentifier>();
        if (id != null) id.nodeReference = root;
        root.personObject.transform.SetParent(treeScene.transform);
        root.personObject.transform.localPosition = new Vector3(0, 0.5f, 0);
        root.personObject.transform.localRotation = Quaternion.identity;
        root.nameLabel = CreateTextLabel(root.personObject.transform, root.name,
            new Vector3(0, personSize * 2, 0), Color.white, 30,
            new Vector3(0.003f, 0.003f, 0.003f));
    }

    // =========================================================================
    //  NODE CREATION — scenario-aware
    // =========================================================================
    GameObject BuildPersonObject(string name, int level)
    {
        if (personPrefab != null)
        {
            GameObject p = Instantiate(personPrefab);
            if (p.GetComponent<TreeNodeIdentifier>() == null)
                p.AddComponent<TreeNodeIdentifier>();
            if (p.GetComponent<Collider>() == null)
            {
                var col = p.AddComponent<SphereCollider>();
                col.radius    = personSize * 6f;
                col.isTrigger = false;
            }
            return p;
        }
        GameObject person = new GameObject($"Node_{name}");
        person.AddComponent<TreeNodeIdentifier>();
        AddVisualToPersonObject(person, name, level);
        var collider = person.AddComponent<SphereCollider>();
        collider.radius    = personSize * 8f;
        collider.center    = Vector3.zero;
        collider.isTrigger = false;
        return person;
    }

    void AddVisualToPersonObject(GameObject person, string name, int level)
    {
        switch (currentScenario)
        {
            case ScenarioMode.FruitTree:   AddFruitVisual(person, level);       break;
            case ScenarioMode.ForestTrail: AddForestTrailVisual(person, level); break;
            default:                       AddFamilyVisual(person, level);      break;
        }
    }

    void AddFamilyVisual(GameObject person, int level)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "VisualSphere";
        sphere.transform.SetParent(person.transform);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale    = Vector3.one * personSize;
        Destroy(sphere.GetComponent<Collider>());
        var mat = new Material(Shader.Find("Standard"));
        mat.color = familyColors[level % familyColors.Length];
        sphere.GetComponent<Renderer>().material = mat;
    }

    void AddFruitVisual(GameObject person, int level)
    {
        GameObject fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fruit.name = "VisualFruit";
        fruit.transform.SetParent(person.transform);
        fruit.transform.localPosition = Vector3.zero;
        fruit.transform.localScale    = new Vector3(personSize, personSize * 1.15f, personSize);
        Destroy(fruit.GetComponent<Collider>());
        var fmat = new Material(Shader.Find("Standard"));
        fmat.color = fruitColors[level % fruitColors.Length];
        fruit.GetComponent<Renderer>().material = fmat;

        GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stem.name = "Stem";
        stem.transform.SetParent(person.transform);
        stem.transform.localPosition = new Vector3(0, personSize * 0.9f, 0);
        stem.transform.localScale    = new Vector3(0.003f, 0.008f, 0.003f);
        Destroy(stem.GetComponent<Collider>());
        var smat = new Material(Shader.Find("Standard"));
        smat.color = new Color(0.2f, 0.55f, 0.1f);
        stem.GetComponent<Renderer>().material = smat;

        GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leaf.name = "Leaf";
        leaf.transform.SetParent(person.transform);
        leaf.transform.localPosition = new Vector3(0.007f, personSize * 1.1f, 0);
        leaf.transform.localScale    = new Vector3(0.012f, 0.005f, 0.008f);
        Destroy(leaf.GetComponent<Collider>());
        var lmat = new Material(Shader.Find("Standard"));
        lmat.color = new Color(0.2f, 0.65f, 0.15f);
        leaf.GetComponent<Renderer>().material = lmat;
    }

    void AddForestTrailVisual(GameObject person, int level)
    {
        GameObject waypoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        waypoint.name = "VisualWaypoint";
        waypoint.transform.SetParent(person.transform);
        waypoint.transform.localPosition = Vector3.zero;
        waypoint.transform.localScale    = Vector3.one * personSize;
        Destroy(waypoint.GetComponent<Collider>());
        var wmat = new Material(Shader.Find("Standard"));
        wmat.color = trailColors[level % trailColors.Length];
        waypoint.GetComponent<Renderer>().material = wmat;

        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Post";
        post.transform.SetParent(person.transform);
        post.transform.localPosition = new Vector3(0, -personSize * 0.9f, 0);
        post.transform.localScale    = new Vector3(0.004f, 0.012f, 0.004f);
        Destroy(post.GetComponent<Collider>());
        var pmat = new Material(Shader.Find("Standard"));
        pmat.color = new Color(0.55f, 0.32f, 0.10f);
        post.GetComponent<Renderer>().material = pmat;

        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.name = "Sign";
        sign.transform.SetParent(person.transform);
        sign.transform.localPosition = new Vector3(0, personSize * 1.1f, 0);
        sign.transform.localScale    = new Vector3(0.025f, 0.008f, 0.006f);
        sign.transform.localRotation = Quaternion.Euler(0, 45f * (level % 4), 0);
        Destroy(sign.GetComponent<Collider>());
        var signMat = new Material(Shader.Find("Standard"));
        signMat.color = new Color(0.72f, 0.50f, 0.20f);
        sign.GetComponent<Renderer>().material = signMat;
    }

    GameObject CreatePreviewNode(string name, int level)
    {
        GameObject person = new GameObject($"Preview_{name}");

        Color previewColor;
        if (currentScenario == ScenarioMode.FruitTree)
        {
            previewColor = fruitColors[level % fruitColors.Length];
            GameObject fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fruit.transform.SetParent(person.transform);
            fruit.transform.localScale = new Vector3(personSize, personSize * 1.15f, personSize);
            Destroy(fruit.GetComponent<Collider>());
            previewColor.a = 0.75f;
            var mat = new Material(Shader.Find("Standard")); mat.color = previewColor;
            SetTransparent(mat);
            fruit.GetComponent<Renderer>().material = mat;
        }
        else if (currentScenario == ScenarioMode.ForestTrail)
        {
            previewColor = trailColors[level % trailColors.Length];
            GameObject waypoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            waypoint.transform.SetParent(person.transform);
            waypoint.transform.localScale = Vector3.one * personSize;
            Destroy(waypoint.GetComponent<Collider>());
            previewColor.a = 0.75f;
            var mat = new Material(Shader.Find("Standard")); mat.color = previewColor;
            SetTransparent(mat);
            waypoint.GetComponent<Renderer>().material = mat;
        }
        else
        {
            previewColor = familyColors[level % familyColors.Length];
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(person.transform);
            sphere.transform.localScale = Vector3.one * personSize;
            Destroy(sphere.GetComponent<Collider>());
            previewColor.a = 0.8f;
            var mat = new Material(Shader.Find("Standard")); mat.color = previewColor;
            SetTransparent(mat);
            sphere.GetComponent<Renderer>().material = mat;
        }

        CreateTextLabel(person.transform, name,
            new Vector3(0, personSize * 2, 0), Color.white, 30,
            new Vector3(0.003f, 0.003f, 0.003f));

        return person;
    }

    void SetTransparent(Material mat)
    {
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    // ─────────────────────────────────────────────
    //  INDICATOR CIRCLES
    // ─────────────────────────────────────────────
    GameObject CreateIndicatorCircle(Vector3 localPos, Color color, string label)
    {
        GameObject indicator = new GameObject($"Indicator_{label}");
        indicator.transform.SetParent(treeScene.transform);
        indicator.transform.localPosition = localPos;
        indicator.transform.localRotation = Quaternion.identity;

        GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        circle.transform.SetParent(indicator.transform);
        circle.transform.localPosition = Vector3.zero;
        circle.transform.localRotation = Quaternion.identity;
        circle.transform.localScale    = new Vector3(personSize * 1.5f, 0.002f, personSize * 1.5f);
        Destroy(circle.GetComponent<Collider>());
        var mat = new Material(Shader.Find("Standard")); mat.color = color;
        SetTransparent(mat);
        circle.GetComponent<Renderer>().material = mat;

        SphereCollider col = indicator.AddComponent<SphereCollider>();
        col.radius    = personSize * 2f;
        col.isTrigger = false;
        indicator.AddComponent<TreeNodeIdentifier>();

        CreateTextLabel(indicator.transform, label,
            new Vector3(0, personSize * 2, 0), color, 25,
            new Vector3(0.003f, 0.003f, 0.003f));

        return indicator;
    }

    // ─────────────────────────────────────────────
    //  BRANCHES
    // ─────────────────────────────────────────────
    GameObject CreateBranch(Vector3 start, Vector3 end)
    {
        GameObject branch    = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Vector3    direction = end - start;
        float      distance  = direction.magnitude;

        branch.transform.position   = treeScene.transform.TransformPoint(start + direction / 2f);
        branch.transform.up         = treeScene.transform.TransformDirection(direction.normalized);
        branch.transform.localScale = new Vector3(branchThickness, distance / 2f, branchThickness);

        var mat = new Material(Shader.Find("Standard"));
        mat.color = (currentScenario == ScenarioMode.FruitTree || currentScenario == ScenarioMode.ForestTrail)
            ? new Color(0.55f, 0.32f, 0.10f)
            : new Color(0.40f, 0.25f, 0.10f);
        branch.GetComponent<Renderer>().material = mat;
        Destroy(branch.GetComponent<Collider>());
        return branch;
    }

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
        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text      = text;
        tm.fontSize  = fontSize;
        tm.color     = color;
        tm.anchor    = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        labelObj.AddComponent<Billboard>();
        return labelObj;
    }

    // =========================================================================
    //  SNAPPING / PREVIEW UPDATE
    // =========================================================================
    void UpdatePreviewNodeSnapping()
    {
        if (previewNode == null) return;

        TreeNode closestParent = null;
        bool     isLeft        = true;
        float    closestDist   = float.MaxValue;

        FindClosestIndicator(root, ref closestParent, ref isLeft, ref closestDist);

        if (closestParent != null && closestDist < snapDistance)
        {
            Vector3 targetPos = isLeft
                ? closestParent.leftIndicator.transform.localPosition
                : closestParent.rightIndicator.transform.localPosition;

            previewPosition                     = targetPos;
            previewNode.transform.localPosition = previewPosition;

            if (tutorialIntegration != null && snappedParent != closestParent)
                tutorialIntegration.OnSnappingDetected();

            snappedParent = closestParent;
            snappedIsLeft = isLeft;

            UpdateInstructions($"Snapped to {(isLeft ? "LEFT" : "RIGHT")} of {closestParent.name} - Tap CONFIRM!");
            HighlightIndicator(isLeft ? closestParent.leftIndicator  : closestParent.rightIndicator, true);
            HighlightIndicator(isLeft ? closestParent.rightIndicator : closestParent.leftIndicator,  false);
        }
        else
        {
            snappedParent = null;
            UpdateInstructions($"Move {pendingChildName} closer to a circle indicator");
            if (closestParent != null)
            {
                HighlightIndicator(closestParent.leftIndicator,  false);
                HighlightIndicator(closestParent.rightIndicator, false);
            }
        }
    }

    void HighlightIndicator(GameObject indicator, bool highlight)
    {
        if (indicator == null) return;
        Transform circleT = indicator.transform.GetChild(0);
        if (circleT == null) return;
        Renderer rend = circleT.GetComponent<Renderer>();
        if (rend == null) return;
        if (highlight)
        {
            rend.material.color = new Color(0f, 1f, 0f, 0.9f);
        }
        else
        {
            TextMesh label = indicator.GetComponentInChildren<TextMesh>();
            rend.material.color = (label != null && label.text == "LEFT")
                ? new Color(0.3f, 0.8f, 1f,  0.6f)
                : new Color(1f,  0.5f, 0.3f, 0.6f);
        }
    }

    // =========================================================================
    //  ADD CHILD
    // =========================================================================
    public void OnAddChildButton()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnAddChildButtonClicked();
        if (currentState != TreeState.Ready) return;

        SetActive(mainButtonPanel,         false);
        SetActive(beginnerButtonPanel,     false);
        SetActive(intermediateButtonPanel, false);
        SetActive(personInputPanel,        true);

        string nodeWord = currentScenario == ScenarioMode.FruitTree   ? "fruit/node"
                        : currentScenario == ScenarioMode.ForestTrail ? "waypoint"
                        : "person";
        UpdateInstructions($"Enter the {nodeWord}'s name");
        if (operationInfoText != null)
            operationInfoText.text =
                "ADD NODE Operation!\n\n" +
                $"1. Enter {nodeWord}'s name\n" +
                "2. Move preview with arrows\n" +
                "3. Node will SNAP to indicators\n" +
                "4. Confirm when snapped\n\n" +
                "Time: O(log n) average";
    }

    public void OnSelectChildButton()
    {
        if (string.IsNullOrEmpty(nameInputField.text))
        { UpdateInstructionsError("Please enter a name first!"); return; }
        pendingChildName = nameInputField.text;
        SetActive(personInputPanel, false);
        ShowAllIndicators();
        StartPositioningDirectly();
    }

    void StartPositioningDirectly()
    {
        currentState = TreeState.PositioningChild;
        if (tutorialIntegration != null) tutorialIntegration.OnPositioningStarted();

        Vector3 rootPos = root.personObject.transform.localPosition;
        previewPosition = rootPos + new Vector3(-horizontalSpacing / 2, -verticalSpacing, 0);

        previewNode = CreatePreviewNode(pendingChildName, 1);
        previewNode.transform.SetParent(treeScene.transform);
        previewNode.transform.localPosition = previewPosition;
        previewNode.transform.localRotation = Quaternion.identity;

        SetActive(directionPanel, true);
        SetActive(confirmButton,  true);
        UpdateInstructions($"Move {pendingChildName} to snap to a circle indicator");
        if (operationInfoText != null)
            operationInfoText.text =
                "POSITION NODE\n\n" +
                "Use directional buttons:\n" +
                "Up  Down\n" +
                "Left  Right\n\n" +
                "Node SNAPS when near indicator!\n\n" +
                "CONFIRM when indicator turns green!";
    }

    void ShowAllIndicators() { ShowIndicatorsForNode(root); }
    void HideAllIndicators() { HideIndicatorsForNode(root); }

    void ShowIndicatorsForNode(TreeNode node)
    {
        if (node == null) return;
        Vector3 nodePos = node.personObject.transform.localPosition;
        if (node.leftChild == null)
        {
            Vector3 leftPos = nodePos + new Vector3(-horizontalSpacing / 2, -verticalSpacing, 0);
            node.leftIndicator = CreateIndicatorCircle(leftPos, new Color(0.3f, 0.8f, 1f, 0.6f), "LEFT");
            node.leftIndicator.GetComponent<TreeNodeIdentifier>().nodeReference = node;
        }
        if (node.rightChild == null)
        {
            Vector3 rightPos = nodePos + new Vector3(horizontalSpacing / 2, -verticalSpacing, 0);
            node.rightIndicator = CreateIndicatorCircle(rightPos, new Color(1f, 0.5f, 0.3f, 0.6f), "RIGHT");
            node.rightIndicator.GetComponent<TreeNodeIdentifier>().nodeReference = node;
        }
        ShowIndicatorsForNode(node.leftChild);
        ShowIndicatorsForNode(node.rightChild);
    }

    void HideIndicatorsForNode(TreeNode node)
    {
        if (node == null) return;
        if (node.leftIndicator  != null) { Destroy(node.leftIndicator);  node.leftIndicator  = null; }
        if (node.rightIndicator != null) { Destroy(node.rightIndicator); node.rightIndicator = null; }
        HideIndicatorsForNode(node.leftChild);
        HideIndicatorsForNode(node.rightChild);
    }

    public void OnMoveUp()    { if (currentState == TreeState.PositioningChild && previewNode) { previewPosition.y += moveIncrement; previewNode.transform.localPosition = previewPosition; } }
    public void OnMoveDown()  { if (currentState == TreeState.PositioningChild && previewNode) { previewPosition.y -= moveIncrement; previewNode.transform.localPosition = previewPosition; } }
    public void OnMoveLeft()  { if (currentState == TreeState.PositioningChild && previewNode) { previewPosition.x -= moveIncrement; previewNode.transform.localPosition = previewPosition; } }
    public void OnMoveRight() { if (currentState == TreeState.PositioningChild && previewNode) { previewPosition.x += moveIncrement; previewNode.transform.localPosition = previewPosition; } }

    public void OnConfirmPlacement()
    {
        if (currentState != TreeState.PositioningChild) return;

        TreeNode targetParent = snappedParent;
        bool     isLeftChild  = snappedIsLeft;

        if (targetParent == null)
        {
            float closestDist = float.MaxValue;
            FindClosestIndicator(root, ref targetParent, ref isLeftChild, ref closestDist);
            if (targetParent == null || closestDist > snapDistance)
            {
                UpdateInstructionsError("Not close enough! Move closer until it SNAPS (turns green)");
                return;
            }
        }

        if (previewNode != null) Destroy(previewNode);

        TreeNode newNode = new TreeNode
        {
            name      = pendingChildName,
            parent    = targetParent,
            level     = targetParent.level + 1,
            xPosition = previewPosition.x
        };

        newNode.personObject = BuildPersonObject(newNode.name, newNode.level);
        var id = newNode.personObject.GetComponent<TreeNodeIdentifier>();
        if (id != null) id.nodeReference = newNode;
        newNode.personObject.transform.SetParent(treeScene.transform);
        newNode.personObject.transform.localScale    = Vector3.one;
        newNode.personObject.transform.localPosition = previewPosition;
        newNode.personObject.transform.localRotation = Quaternion.identity;

        newNode.nameLabel = CreateTextLabel(newNode.personObject.transform, newNode.name,
            new Vector3(0, personSize * 2, 0), Color.white, 30,
            new Vector3(0.003f, 0.003f, 0.003f));

        newNode.branchToParent = CreateBranch(
            targetParent.personObject.transform.localPosition, previewPosition);
        newNode.branchToParent.transform.SetParent(treeScene.transform);

        if (isLeftChild) targetParent.leftChild  = newNode;
        else             targetParent.rightChild = newNode;

        PlaySound(addPersonSound);
        UpdateInstructionsSuccess($"Added {pendingChildName} as {(isLeftChild ? "LEFT" : "RIGHT")} child of {targetParent.name}!");

        ARTreeLessonGuide guide = FindObjectOfType<ARTreeLessonGuide>();
        if (guide != null) guide.NotifyChildAdded();

        UpdateStatus();
        ResetToReady();
    }

    void FindClosestIndicator(TreeNode node, ref TreeNode closestParent, ref bool isLeft, ref float closestDist)
    {
        if (node == null) return;
        if (node.leftIndicator != null)
        {
            float d = Vector3.Distance(previewPosition, node.leftIndicator.transform.localPosition);
            if (d < closestDist) { closestDist = d; closestParent = node; isLeft = true; }
        }
        if (node.rightIndicator != null)
        {
            float d = Vector3.Distance(previewPosition, node.rightIndicator.transform.localPosition);
            if (d < closestDist) { closestDist = d; closestParent = node; isLeft = false; }
        }
        FindClosestIndicator(node.leftChild,  ref closestParent, ref isLeft, ref closestDist);
        FindClosestIndicator(node.rightChild, ref closestParent, ref isLeft, ref closestDist);
    }

    void ResetToReady()
    {
        currentState       = TreeState.Ready;
        selectedParentNode = null;
        pendingChildName   = "";
        snappedParent      = null;
        if (previewNode != null) { Destroy(previewNode); previewNode = null; }
        HideAllIndicators();
        SetActive(directionPanel,   false);
        SetActive(confirmButton,    false);
        SetActive(personInputPanel, false);
        if (nameInputField != null) nameInputField.text = "";
        ShowModeButtons();
    }

    public void OnCancelAddPerson()
    {
        if (previewNode != null) { Destroy(previewNode); previewNode = null; }
        HideAllIndicators();
        ResetToReady();
        UpdateInstructions("Operation cancelled");
    }

    // =========================================================================
    //  BEGINNER TRAVERSALS
    // =========================================================================
    public void OnInOrderTraversal()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnInOrderButtonClicked();
        StartCoroutine(InOrderWithNotify());
    }

    IEnumerator InOrderWithNotify()
    {
        yield return StartCoroutine(InOrderTraversal(root));
        ARTreeLessonGuide guide = FindObjectOfType<ARTreeLessonGuide>();
        if (guide != null) guide.NotifyInOrderCompleted();
    }

    IEnumerator InOrderTraversal(TreeNode node)
    {
        if (node == null) yield break;
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                "IN-ORDER Traversal\n\nOrder: Left -> Root -> Right\n\n" +
                "Visits nodes in sorted order!\n\nUse: Sorted output";
        yield return StartCoroutine(InOrderTraversal(node.leftChild));
        yield return StartCoroutine(HighlightNode(node));
        yield return StartCoroutine(InOrderTraversal(node.rightChild));
    }

    public void OnPreOrderTraversal()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnPreOrderButtonClicked();
        StartCoroutine(PreOrderWithNotify());
    }

    IEnumerator PreOrderWithNotify()
    {
        yield return StartCoroutine(PreOrderTraversal(root));
        ARTreeLessonGuide guide = FindObjectOfType<ARTreeLessonGuide>();
        if (guide != null) guide.NotifyPreOrderCompleted();
    }

    IEnumerator PreOrderTraversal(TreeNode node)
    {
        if (node == null) yield break;
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                "PRE-ORDER Traversal\n\nOrder: Root -> Left -> Right\n\n" +
                "Visits parent before children!\n\nUse: Copy tree structure";
        yield return StartCoroutine(HighlightNode(node));
        yield return StartCoroutine(PreOrderTraversal(node.leftChild));
        yield return StartCoroutine(PreOrderTraversal(node.rightChild));
    }

    public void OnPostOrderTraversal()
    {
        if (tutorialIntegration != null) tutorialIntegration.OnPostOrderButtonClicked();
        StartCoroutine(PostOrderWithNotify());
    }

    IEnumerator PostOrderWithNotify()
    {
        yield return StartCoroutine(PostOrderTraversal(root));
        ARTreeLessonGuide guide = FindObjectOfType<ARTreeLessonGuide>();
        if (guide != null) guide.NotifyPostOrderCompleted();
    }

    IEnumerator PostOrderTraversal(TreeNode node)
    {
        if (node == null) yield break;
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                "POST-ORDER Traversal\n\nOrder: Left -> Right -> Root\n\n" +
                "Children before parent!\n\nUse: Delete tree, calculate";
        yield return StartCoroutine(PostOrderTraversal(node.leftChild));
        yield return StartCoroutine(PostOrderTraversal(node.rightChild));
        yield return StartCoroutine(HighlightNode(node));
    }

    IEnumerator HighlightNode(TreeNode node)
    {
        GameObject h = CreateNodeHighlight(node, new Color(1, 1, 0, 0.4f));
        PlaySound(highlightSound);
        UpdateInstructions($"Visiting: {node.name}");
        yield return new WaitForSeconds(1f);
        Destroy(h);
    }

    // =========================================================================
    //  INTERMEDIATE – SEARCH
    // =========================================================================
    public void OnSearchNodeButton()
    {
        if (currentState != TreeState.Ready) return;
        SetActive(mainButtonPanel,         false);
        SetActive(intermediateButtonPanel, false);
        SetActive(searchInputPanel,        true);
        UpdateInstructions("Enter the name of the node to find");
        if (operationInfoText != null)
            operationInfoText.text =
                "SEARCH NODE  O(n)\n\n" +
                "Traverse tree looking for name.\n" +
                "Must check every node in worst case.\n\n" +
                "Note: BST search is O(log n)\nbut this is a general tree!";
    }

    public void OnConfirmSearch()
    {
        if (searchNameField == null || string.IsNullOrEmpty(searchNameField.text))
        { UpdateInstructionsError("Enter a name to search!"); return; }
        string target = searchNameField.text.Trim();
        SetActive(searchInputPanel, false);
        StartCoroutine(AnimatedSearch(target));
    }

    IEnumerator AnimatedSearch(string targetName)
    {
        currentState = TreeState.Searching;
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                $"Searching for '{targetName}'...\n\nUsing DFS (pre-order)\n\nTime: O(n) worst case";

        bool found = false;
        yield return StartCoroutine(DFSSearch(root, targetName, r => found = r));

        ARTreeLessonGuide guide = FindObjectOfType<ARTreeLessonGuide>();
        if (guide != null) guide.NotifySearchCompleted(found);

        if (!found)
        {
            UpdateInstructionsError($"'{targetName}' not found in the tree!");
            if (operationInfoText != null && !_silenceInstructions)
                operationInfoText.text =
                    $"NOT FOUND\n\n'{targetName}' is not in the tree.\n\n" +
                    $"Searched all {CountNodes(root)} nodes.\nTime: O(n)";
        }

        currentState = TreeState.Ready;
        ShowModeButtons();
        if (searchNameField != null) searchNameField.text = "";
    }

    IEnumerator DFSSearch(TreeNode node, string targetName, System.Action<bool> onResult)
    {
        if (node == null) { onResult(false); yield break; }

        GameObject h = CreateNodeHighlight(node, new Color(1, 1, 0, 0.4f));
        PlaySound(traverseSound);
        UpdateInstructions($"Checking: '{node.name}'  vs  '{targetName}'");
        yield return new WaitForSeconds(0.6f);
        Destroy(h);

        if (node.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject g = CreateNodeHighlight(node, new Color(0, 1, 0, 0.6f));
                PlaySound(foundSound);
                yield return new WaitForSeconds(0.3f);
                Destroy(g);
                yield return new WaitForSeconds(0.15f);
            }
            UpdateInstructionsSuccess($"Found '{targetName}'! Level: {node.level}");
            if (operationInfoText != null && !_silenceInstructions)
                operationInfoText.text =
                    $"FOUND!\n\n'{node.name}'\nLevel: {node.level}\n" +
                    $"Children: {(node.leftChild != null ? 1 : 0) + (node.rightChild != null ? 1 : 0)}\n\nTime: O(n)";
            onResult(true);
            yield break;
        }

        bool fL = false, fR = false;
        yield return StartCoroutine(DFSSearch(node.leftChild,  targetName, r => fL = r));
        if (fL) { onResult(true); yield break; }
        yield return StartCoroutine(DFSSearch(node.rightChild, targetName, r => fR = r));
        onResult(fR);
    }

    // =========================================================================
    //  INTERMEDIATE – DELETE
    // =========================================================================
    public void OnDeleteNodeButton()
    {
        if (currentState != TreeState.Ready) return;
        if (CountNodes(root) <= 1) { UpdateInstructionsError("Need more than 1 node to delete!"); return; }
        SetActive(mainButtonPanel,         false);
        SetActive(intermediateButtonPanel, false);
        SetActive(deleteInputPanel,        true);
        UpdateInstructions("Enter the name of the node to remove");
        if (operationInfoText != null)
            operationInfoText.text =
                "DELETE NODE  O(n)\n\n" +
                "Find and remove a node.\n\n" +
                "If it has children, they\nalso get removed (subtree).\n\n" +
                "Time: O(n) to search first.";
    }

    public void OnConfirmDelete()
    {
        if (deleteNameField == null || string.IsNullOrEmpty(deleteNameField.text))
        { UpdateInstructionsError("Enter a name!"); return; }
        string target = deleteNameField.text.Trim();
        if (root != null && root.name.Equals(target, System.StringComparison.OrdinalIgnoreCase))
        { UpdateInstructionsError("Cannot delete the root node!"); return; }
        SetActive(deleteInputPanel, false);
        StartCoroutine(AnimatedDelete(target));
    }

    IEnumerator AnimatedDelete(string targetName)
    {
        currentState = TreeState.Deleting;
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text = $"Finding '{targetName}'...\nSearching with DFS...\nTime: O(n)";

        TreeNode found = null, foundParent = null;
        bool isLeftChild = false;

        yield return StartCoroutine(FindNode(root, null, targetName, (n, p, l) =>
            { found = n; foundParent = p; isLeftChild = l; }));

        if (found == null)
        {
            UpdateInstructionsError($"'{targetName}' not found!");
            currentState = TreeState.Ready;
            ShowModeButtons();
            if (deleteNameField != null) deleteNameField.text = "";
            yield break;
        }

        for (int i = 0; i < 2; i++)
        {
            GameObject rh = CreateNodeHighlight(found, new Color(1, 0, 0, 0.6f));
            yield return new WaitForSeconds(0.3f);
            Destroy(rh);
            yield return new WaitForSeconds(0.15f);
        }

        int removed = CountNodes(found);
        DestroySubtree(found);
        if (foundParent != null)
        {
            if (isLeftChild) foundParent.leftChild  = null;
            else             foundParent.rightChild = null;
        }

        UpdateInstructionsSuccess($"Deleted '{targetName}' and {removed - 1} child node(s)!");
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                $"DELETED '{targetName}'\n\n" +
                $"Subtree removed: {removed} node(s)\n\n" +
                "Pointer updated in parent.\nTime: O(n)";

        ARTreeLessonGuide guide = FindObjectOfType<ARTreeLessonGuide>();
        if (guide != null) guide.NotifyDeleteCompleted();

        UpdateStatus();
        currentState = TreeState.Ready;
        ShowModeButtons();
        if (deleteNameField != null) deleteNameField.text = "";
        CheckAndUpdateButtonStates();
    }

    IEnumerator FindNode(TreeNode node, TreeNode parent, string targetName,
        System.Action<TreeNode, TreeNode, bool> onFound)
    {
        if (node == null) { onFound(null, null, false); yield break; }

        GameObject h = CreateNodeHighlight(node, new Color(1, 1, 0, 0.3f));
        UpdateInstructions($"Searching: '{node.name}'");
        PlaySound(traverseSound);
        yield return new WaitForSeconds(0.5f);
        Destroy(h);

        if (node.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
        {
            bool isLeft = (parent != null && parent.leftChild == node);
            onFound(node, parent, isLeft);
            yield break;
        }

        TreeNode fr = null, fp = null; bool fl = false;
        yield return StartCoroutine(FindNode(node.leftChild,  node, targetName, (n, p, l) => { fr = n; fp = p; fl = l; }));
        if (fr != null) { onFound(fr, fp, fl); yield break; }
        yield return StartCoroutine(FindNode(node.rightChild, node, targetName, (n, p, l) => { fr = n; fp = p; fl = l; }));
        onFound(fr, fp, fl);
    }

    void DestroySubtree(TreeNode node)
    {
        if (node == null) return;
        DestroySubtree(node.leftChild);
        DestroySubtree(node.rightChild);
        if (node.branchToParent != null) Destroy(node.branchToParent);
        if (node.personObject   != null) Destroy(node.personObject);
    }

    // =========================================================================
    //  INTERMEDIATE – TREE HEIGHT
    // =========================================================================
    public void OnTreeHeightButton()
    {
        if (currentState != TreeState.Ready) return;
        currentState = TreeState.ComputingHeight;
        StartCoroutine(AnimatedTreeHeight());
    }

    IEnumerator AnimatedTreeHeight()
    {
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                "TREE HEIGHT  O(n)\n\n" +
                "Height = longest path\nfrom root to a leaf.\n\n" +
                "Recursive formula:\nheight = 1 + max(left, right)";
        UpdateInstructions("Computing tree height recursively...");
        yield return new WaitForSeconds(0.5f);

        int height = 0;
        yield return StartCoroutine(ComputeHeightAnimated(root, 0, h => height = h));

        UpdateInstructionsSuccess($"Tree Height: {height} levels");
        if (operationInfoText != null && !_silenceInstructions)
            operationInfoText.text =
                $"HEIGHT = {height}\n\n" +
                "Longest root-to-leaf path.\n\n" +
                $"Visited all {CountNodes(root)} nodes.\n" +
                "Time: O(n) | Space: O(h)";

        ARTreeLessonGuide guide = FindObjectOfType<ARTreeLessonGuide>();
        if (guide != null) guide.NotifyHeightCompleted(height);

        currentState = TreeState.Ready;
        ShowModeButtons();
    }

    IEnumerator ComputeHeightAnimated(TreeNode node, int depth, System.Action<int> onResult)
    {
        if (node == null) { onResult(0); yield break; }

        Color c = new Color(Mathf.Lerp(1f, 0f, depth / 5f), Mathf.Lerp(0.5f, 1f, depth / 5f), 0.2f, 0.5f);
        GameObject h = CreateNodeHighlight(node, c);
        UpdateInstructions($"Level {depth}: '{node.name}'");
        PlaySound(traverseSound);
        yield return new WaitForSeconds(0.5f);
        Destroy(h);

        int lH = 0, rH = 0;
        yield return StartCoroutine(ComputeHeightAnimated(node.leftChild,  depth + 1, r => lH = r));
        yield return StartCoroutine(ComputeHeightAnimated(node.rightChild, depth + 1, r => rH = r));
        onResult(1 + Mathf.Max(lH, rH));
    }

    // =========================================================================
    //  SHARED HIGHLIGHT HELPER
    // =========================================================================
    GameObject CreateNodeHighlight(TreeNode node, Color color)
    {
        GameObject h = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        h.transform.SetParent(node.personObject.transform);
        h.transform.localPosition = Vector3.zero;
        h.transform.localScale    = Vector3.one * 2f;
        var mat = new Material(Shader.Find("Unlit/Color")); mat.color = color;
        h.GetComponent<Renderer>().material = mat;
        Destroy(h.GetComponent<Collider>());
        return h;
    }

    // =========================================================================
    //  RESET
    // =========================================================================
    public void OnResetButton()
    {
        if (swipeRotation       != null) swipeRotation.ResetRotation();
        if (tutorialIntegration != null) tutorialIntegration.OnResetButtonClicked();
        if (zoomController      != null) zoomController.ResetZoom();

        StopAllCoroutines();
        pulseCoroutine = null;

        if (treeScene   != null) { Destroy(treeScene);   treeScene   = null; }
        if (previewNode != null) { Destroy(previewNode); previewNode = null; }

        root               = null;
        trunkObject        = null;
        trailObject        = null;
        sceneSpawned       = false;
        selectedParentNode = null;
        snappedParent      = null;
        pendingChildName   = "";
        currentDifficulty  = DifficultyMode.None;
        currentScenario    = ScenarioMode.None;
        currentState       = TreeState.WaitingForPlane;
        originalButtonColors.Clear();

        if (planeManager != null)
        {
            planeManager.enabled = true;
            foreach (var plane in planeManager.trackables)
                if (plane != null && plane.gameObject != null)
                    plane.gameObject.SetActive(true);
        }

        if (raycastManager != null) raycastManager.enabled = true;

        HideAllPanels();
        UpdateInstructions("Point camera at a flat surface");

        if (detectionText != null)
        {
            detectionText.text  = "Looking for surfaces...";
            detectionText.color = Color.yellow;
        }

        if (statusText != null) statusText.text = "Nodes: 0";
        Debug.Log("RESET COMPLETE");
    }

    // =========================================================================
    //  UTILITIES
    // =========================================================================
    void UpdateStatus()
    {
        int n = CountNodes(root);
        if (statusText != null) statusText.text = $"Tree Nodes: {n}";
    }

    int CountNodes(TreeNode node)
    { return node == null ? 0 : 1 + CountNodes(node.leftChild) + CountNodes(node.rightChild); }

    void PlaySound(AudioClip clip)
    { if (audioSource != null && clip != null) audioSource.PlayOneShot(clip); }

    void SetActive(GameObject obj, bool active)
    { if (obj != null) obj.SetActive(active); }
}