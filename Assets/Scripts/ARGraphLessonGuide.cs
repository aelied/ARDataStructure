using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ARGraphLessonGuide.cs — SYLLABUS-ALIGNED VERSION WITH UI SYNC FIX
/// ===================================================================
/// UI SYNC FIX (mirrors ARArrayLessonGuide / ARStackLessonGuide):
///   SyncControllerUI(stepIndex) is called on every ShowStep() and again
///   after OnSceneSpawned(). It sets instructionText / operationInfoText /
///   statusText / detectionText to lesson-relevant content, or hides the
///   whole root panel when the guide card already covers that information.
///
///   GetPanelRoot(tmp) walks two levels up from the TMP component:
///     Text -> Panel -> RootPanel
///   so the entire card — background, border, text — hides/shows together.
///
/// Per-lesson sync table:
///   Mode 1 (L1 What is a Graph):  instructionText shown (step hint)
///                                  operationInfoText hidden (guide card covers it)
///                                  statusText        hidden
///                                  detectionText     hidden after placement
///
///   Mode 2 (L2 Terminology):      instructionText shown
///                                  operationInfoText shown (degree/path check detail)
///                                  statusText        shown
///                                  detectionText     hidden after placement
///
///   Mode 3 (L3 Representation):   instructionText shown
///                                  operationInfoText shown (add node/edge feedback)
///                                  statusText        shown (node/edge counts useful)
///                                  detectionText     hidden after placement
///
///   Mode 4 (L4 Traversal):        instructionText shown (BFS/DFS step prompt)
///                                  operationInfoText hidden (complexity table replaces)
///                                  statusText        shown
///                                  detectionText     hidden after placement
///
///   Mode 5 (L5 Algorithms):       instructionText shown
///                                  operationInfoText hidden (complexity table replaces)
///                                  statusText        shown
///                                  detectionText     hidden after placement
///
/// Assessment transition: DelayedAssessmentStart hides all controller overlays.
/// OnReturn: restores all four overlays for sandbox / free-play mode.
/// </summary>
public class ARGraphLessonGuide : MonoBehaviour
{
    [Header("Your Existing AR Controller")]
    public InteractiveCityGraph graphController;

    [Header("Existing Canvas Panels")]
    public GameObject mainButtonPanel;
    public GameObject beginnerButtonPanel;
    public GameObject intermediateButtonPanel;
    public GameObject inputPanel;
    public GameObject inputPanelEdge;
    public GameObject movementButtonPanel;
    public GameObject algorithmPanel;
    public GameObject pathCheckInputPanel;
    public GameObject degreeInputPanel;

    [Header("Guide Canvas (LessonGuideCanvas)")]
    public Canvas          guideCanvas;
    public GameObject      guideCardPanel;
    public TextMeshProUGUI stepTitleText;
    public TextMeshProUGUI stepBodyText;
    public TextMeshProUGUI stepCounterText;
    public Image           progressBarFill;
    public Button          nextStepButton;
    public TextMeshProUGUI nextStepButtonLabel;
    public Button          returnButton;
    public Button          minimiseButton;
    public GameObject      collapsedTab;
    public Button          restoreButton;

    [Header("Assessment")]
    public ARGraphLessonAssessment lessonAssessment;

    [Header("L4: Traversal Readout")]
    public TextMeshProUGUI traversalReadout;
    public TextMeshProUGUI codeSnippetLabel;

    [Header("L5: Complexity Table")]
    public GameObject      complexityTablePanel;
    public TextMeshProUGUI complexityTableText;
    public TextMeshProUGUI activeOperationLabel;

    [Header("Toast Notifications")]
    public GameObject      toastPanel;
    public TextMeshProUGUI toastText;
    public Image           toastIcon;
    public Sprite          checkSprite;
    public Sprite          crossSprite;

    [Header("Settings")]
    public string mainAppSceneName = "MainScene";

    // ── State ──────────────────────────────────────────────────────────────────
    int  lessonIndex       = -1;
    int  guideMode         = -1;
    int  currentStep       = 0;
    bool nextBlocked       = false;
    bool sceneSpawned      = false;
    bool assessmentStarted = false;

    bool didBFS      = false;
    bool didDFS      = false;
    bool didDijkstra = false;
    bool didMST      = false;
    bool didAddNode  = false;
    bool didAddEdge  = false;

    int lastNodeCount = 0;
    int lastEdgeCount = 0;

    struct Step { public string title, body; public bool waitForAction; }
    Step[] steps;

    // ── Guards ─────────────────────────────────────────────────────────────────
    private bool _started        = false;
    private bool _guideInited    = false;
    private bool _nextOnCooldown = false;

    // ──────────────────────────────────────────────────────────────────────────
    // PUBLIC: called by ARModeSelectionManager before re-enabling this component
    // ──────────────────────────────────────────────────────────────────────────
    public void ResetInitFlag()
    {
        _guideInited = false;
        _started     = false;
        Debug.Log("[ARGraphLessonGuide] ResetInitFlag — ready for fresh init");
    }

    // ──────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (_started) return;
        _started = true;

        string topic = PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "");
        lessonIndex  = PlayerPrefs.GetInt(ARReturnHandler.AR_LESSON_INDEX_KEY, -1);

        if (!topic.ToLower().Contains("graph") || lessonIndex < 0)
        {
            if (guideCanvas != null) guideCanvas.gameObject.SetActive(false);
            enabled = false;
            return;
        }

        guideMode = LessonIndexToMode(lessonIndex);
        if (guideMode < 0)
        {
            if (guideCanvas != null) guideCanvas.gameObject.SetActive(false);
            enabled = false;
            return;
        }

        if (lessonAssessment == null)
            lessonAssessment = GetComponent<ARGraphLessonAssessment>();

        Debug.Log($"[ARGraphLessonGuide] Lesson {lessonIndex} -> Mode {guideMode} | Assessment: {(lessonAssessment != null ? "FOUND" : "NULL")}");
    }

    int LessonIndexToMode(int i)
    {
        switch (i)
        {
            case 0: return 1;
            case 1: return 2;
            case 2: return 3;
            case 3: return 4;
            case 4: return 5;
            default: return -1;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // InitGuide — called via SendMessage from ARModeSelectionManager ONLY
    // ──────────────────────────────────────────────────────────────────────────
    void InitGuide()
    {
        if (guideCanvas != null) guideCanvas.gameObject.SetActive(true);
        if (_guideInited) return;
        _guideInited = true;

        complexityTablePanel?.SetActive(false);
        collapsedTab?.SetActive(false);
        returnButton?.gameObject.SetActive(false);
        guideCardPanel?.SetActive(true);
        toastPanel?.SetActive(false);

        nextStepButton?.onClick.AddListener(OnNext);
        returnButton?.onClick.AddListener(OnReturn);
        minimiseButton?.onClick.AddListener(OnMinimise);
        restoreButton?.onClick.AddListener(OnRestore);

        // Stop the controller from overwriting the guide's own instruction panels
        graphController?.SetInstructionSilence(true);

        steps = BuildSteps(guideMode);
        nextBlocked = true;
        ShowStep(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  PANEL ROOT HELPERS
    //  Walks two levels up from the TMP component to find the root card panel:
    //    Text -> Panel -> RootPanel
    //  so the entire card hides/shows together.
    // ──────────────────────────────────────────────────────────────────────────
    GameObject GetPanelRoot(TextMeshProUGUI tmp)
    {
        if (tmp == null) return null;
        Transform t = tmp.transform.parent;          // Panel
        if (t != null && t.parent != null)
            return t.parent.gameObject;              // RootPanel
        if (t != null) return t.gameObject;          // Panel (fallback)
        return tmp.gameObject;                       // Text itself (last resort)
    }

    void SetControllerText(TextMeshProUGUI tmp, string text)
    {
        if (tmp == null) return;
        GetPanelRoot(tmp)?.SetActive(true);
        tmp.text  = text;
        tmp.color = Color.white;
    }

    void ShowControllerPanel(TextMeshProUGUI tmp) { GetPanelRoot(tmp)?.SetActive(true); }

    void HideControllerPanel(TextMeshProUGUI tmp)
    {
        var root = GetPanelRoot(tmp);
        if (root != null && root.activeSelf) root.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  SYNC CONTROLLER UI
    //  Called every ShowStep() and again after OnSceneSpawned().
    // ──────────────────────────────────────────────────────────────────────────
    void SyncControllerUI(int stepIndex)
    {
        if (graphController == null) return;

        // detectionText: only useful before the scene is placed
        if (sceneSpawned)
            HideControllerPanel(graphController.detectionText);
        else
            ShowControllerPanel(graphController.detectionText);

        switch (guideMode)
        {
            // L1 - What is a Graph: observe the pre-built network.
            // operationInfoText hidden (guide card explains everything).
            // statusText hidden (no operations to track yet).
            case 1:
            {
                string hint = stepIndex switch
                {
                    0 => "Choose your scenario and difficulty, then tap a flat surface to place the scene.",
                    1 => "Look at the AR scene — nodes (vertices) are connected by edges. This is a graph!",
                    2 => "The AR scene shows an UNDIRECTED WEIGHTED graph. Numbers on edges = distances.",
                    3 => "Graphs appear in maps, social networks, internet routing, and much more.",
                    _ => "Assessment starting shortly..."
                };
                SetControllerText(graphController.instructionText, hint);
                HideControllerPanel(graphController.operationInfoText);
                HideControllerPanel(graphController.statusText);
                break;
            }

            // L2 - Terminology: degree, paths, cycles.
            // operationInfoText shown (controller writes degree/path detail).
            // statusText shown (node/edge count is relevant).
            case 2:
            {
                string hint = stepIndex switch
                {
                    0 => "Tap a flat surface to place the graph scene, then explore.",
                    1 => "Use DEGREE (intermediate) to check how many edges a vertex has.",
                    2 => "Use PATH CHECK to verify whether two vertices are connected.",
                    3 => "A weighted graph has numeric values on each edge — distances here.",
                    _ => "Assessment starting shortly..."
                };
                SetControllerText(graphController.instructionText, hint);
                ShowControllerPanel(graphController.operationInfoText);
                ShowControllerPanel(graphController.statusText);
                break;
            }

            // L3 - Representation: adjacency matrix vs list, add nodes/edges.
            // operationInfoText shown (controller writes add-node/edge feedback).
            // statusText shown (node/edge counts are the whole point here).
            case 3:
            {
                string hint = stepIndex switch
                {
                    0 => "Tap a flat surface to place the graph scene.",
                    1 => "Observe the pre-built adjacency list — each node stores its neighbour list.",
                    2 => "Tap  ADD NODE  to add a new vertex. Watch the node count increase.",
                    3 => "Tap  ADD EDGE  to connect two nodes by name. This is O(1) for adjacency lists.",
                    _ => "Assessment starting shortly..."
                };
                SetControllerText(graphController.instructionText, hint);
                ShowControllerPanel(graphController.operationInfoText);
                ShowControllerPanel(graphController.statusText);
                break;
            }

            // L4 - Traversal: BFS and DFS.
            // instructionText -> which algorithm to run next.
            // operationInfoText hidden (complexity table is the visual focus).
            // statusText shown.
            case 4:
            {
                string hint = stepIndex switch
                {
                    0 => "Tap a flat surface to place the graph scene.",
                    1 => "Tap  BFS  to watch Breadth-First Search visit nodes level by level.",
                    2 => "Tap  DFS  to watch Depth-First Search go deep before backtracking.",
                    3 => "BFS uses a Queue. DFS uses a Stack or recursion. Both run in O(V+E).",
                    _ => "Assessment starting shortly..."
                };
                SetControllerText(graphController.instructionText, hint);
                HideControllerPanel(graphController.operationInfoText);
                ShowControllerPanel(graphController.statusText);
                break;
            }

            // L5 - Algorithms (Dijkstra, Prim's MST, Big-O).
            // instructionText -> what to try next.
            // operationInfoText hidden (complexity table replaces it).
            // statusText shown.
            case 5:
            {
                string hint = stepIndex switch
                {
                    0 => "Perform any operation — watch its row light up in the Big-O table.",
                    1 => "Tap  DIJKSTRA  to find the shortest weighted path between two nodes.",
                    2 => "Compare Dijkstra O((V+E)logV) vs BFS O(V+E) — weighting adds cost!",
                    3 => "Try  MST  (intermediate) to connect all nodes with minimum edge cost.",
                    _ => "Assessment starting shortly..."
                };
                SetControllerText(graphController.instructionText, hint);
                HideControllerPanel(graphController.operationInfoText);
                ShowControllerPanel(graphController.statusText);
                break;
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!enabled) return;

        if (!sceneSpawned && graphController != null)
        {
            if (graphController.GetNodeCount() > 0)
            {
                sceneSpawned = true;
                OnSceneSpawned();
            }
        }

        if (sceneSpawned)
            CheckGraphChanges();
    }

    void OnSceneSpawned()
    {
        Debug.Log("[ARGraphLessonGuide] Graph scene detected.");

        if (guideMode == 4 || guideMode == 5)
        {
            complexityTablePanel?.SetActive(true);
            UpdateComplexityTable("");
        }

        nextBlocked = false;
        UpdateNextButton();
        lastNodeCount = graphController != null ? graphController.GetNodeCount() : 0;
        lastEdgeCount = graphController != null ? graphController.GetEdgeCount() : 0;

        // Sync controller overlays now that the scene is placed
        SyncControllerUI(currentStep);
    }

    void CheckGraphChanges()
    {
        if (graphController == null) return;

        int curNodes = graphController.GetNodeCount();
        int curEdges = graphController.GetEdgeCount();

        if (curNodes > lastNodeCount && !didAddNode)
        {
            didAddNode = true;
            TryUnblock();
        }
        if (curEdges > lastEdgeCount && !didAddEdge)
        {
            didAddEdge = true;
            TryUnblock();
        }

        lastNodeCount = curNodes;
        lastEdgeCount = curEdges;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Notify hooks — called by InteractiveCityGraph
    // ──────────────────────────────────────────────────────────────────────────
    public void NotifyBFSCompleted()
    {
        if (!didBFS) { didBFS = true; TryUnblock(); }
        if (lessonAssessment != null) lessonAssessment.NotifyTraversal("bfs");
        UpdateComplexityTable("BFS");
        ShowToast(checkSprite, "BFS Complete! Time: O(V+E)");

        // Live update instructionText for L4
        if (graphController?.instructionText != null && guideMode == 4)
        {
            GetPanelRoot(graphController.instructionText)?.SetActive(true);
            graphController.instructionText.text  = "BFS done — level-by-level traversal complete! Now try  DFS.";
            graphController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyDFSCompleted()
    {
        if (!didDFS) { didDFS = true; TryUnblock(); }
        if (lessonAssessment != null) lessonAssessment.NotifyTraversal("dfs");
        UpdateComplexityTable("DFS");
        ShowToast(checkSprite, "DFS Complete! Time: O(V+E)");

        if (graphController?.instructionText != null && guideMode == 4)
        {
            GetPanelRoot(graphController.instructionText)?.SetActive(true);
            graphController.instructionText.text  = "DFS done — depth-first traversal complete! Both algorithms run in O(V+E).";
            graphController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyDijkstraCompleted()
    {
        if (!didDijkstra) { didDijkstra = true; TryUnblock(); }
        if (lessonAssessment != null) lessonAssessment.NotifyOperation("dijkstra");
        UpdateComplexityTable("DIJKSTRA");
        ShowToast(checkSprite, "Dijkstra Complete! Time: O((V+E)logV)");

        if (graphController?.instructionText != null && guideMode == 5)
        {
            GetPanelRoot(graphController.instructionText)?.SetActive(true);
            graphController.instructionText.text  = "Dijkstra done — shortest path found! O((V+E)logV). Try  MST  next.";
            graphController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyMSTCompleted()
    {
        if (!didMST) { didMST = true; TryUnblock(); }
        if (lessonAssessment != null) lessonAssessment.NotifyOperation("mst");
        UpdateComplexityTable("MST");
        ShowToast(checkSprite, "Prim's MST Complete! Time: O(E logV)");

        if (graphController?.instructionText != null && guideMode == 5)
        {
            GetPanelRoot(graphController.instructionText)?.SetActive(true);
            graphController.instructionText.text  = "MST done — minimum spanning tree built! Green edges = cheapest connection for all nodes.";
            graphController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyNodeAdded()
    {
        if (lessonAssessment != null) lessonAssessment.NotifyNodeAdded();

        if (graphController?.instructionText != null && guideMode == 3)
        {
            GetPanelRoot(graphController.instructionText)?.SetActive(true);
            graphController.instructionText.text  = "Node added! Now tap  ADD EDGE  to connect it — that's O(1) with an adjacency list.";
            graphController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyEdgeAdded()
    {
        if (lessonAssessment != null) lessonAssessment.NotifyEdgeAdded();

        if (graphController?.instructionText != null && guideMode == 3)
        {
            GetPanelRoot(graphController.instructionText)?.SetActive(true);
            graphController.instructionText.text  = "Edge added! The adjacency list now records this connection. O(1) insert.";
            graphController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyNodeRemoved()
    {
        if (lessonAssessment != null) lessonAssessment.NotifyNodeRemoved();
    }

    public void NotifyPathCheckCompleted(bool pathExists)
    {
        if (lessonAssessment != null) lessonAssessment.NotifyPathCheck(pathExists);

        if (graphController?.instructionText != null && guideMode == 2)
        {
            GetPanelRoot(graphController.instructionText)?.SetActive(true);
            string result = pathExists ? "PATH EXISTS — nodes are connected!" : "NO PATH — nodes are disconnected!";
            graphController.instructionText.text  = result + " Path Check uses BFS internally — O(V+E).";
            graphController.instructionText.color = pathExists ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f);
        }
    }

    public void NotifyDegreeChecked()
    {
        if (lessonAssessment != null) lessonAssessment.NotifyDegreeChecked();

        if (graphController?.instructionText != null && guideMode == 2)
        {
            GetPanelRoot(graphController.instructionText)?.SetActive(true);
            graphController.instructionText.text  = "Degree checked! High degree = hub vertex. Try PATH CHECK next.";
            graphController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // STEP MANAGEMENT
    // ──────────────────────────────────────────────────────────────────────────
    void ShowStep(int index)
    {
        if (steps == null || index >= steps.Length) return;
        currentStep = index;
        var s = steps[index];

        if (stepTitleText   != null) stepTitleText.text   = s.title;
        if (stepBodyText    != null) stepBodyText.text    = s.body;
        if (stepCounterText != null) stepCounterText.text = $"Step {index + 1} / {steps.Length}";
        if (progressBarFill != null) progressBarFill.fillAmount = (float)(index + 1) / steps.Length;

        bool isLast = index == steps.Length - 1;

        nextStepButton?.gameObject.SetActive(!isLast);
        if (returnButton != null)
            returnButton.gameObject.SetActive(isLast && lessonAssessment == null);

        nextBlocked = s.waitForAction;
        if (nextBlocked) CheckIfAlreadyDone(index);

        if (!nextBlocked)
            StartCoroutine(EnableNextAfterDelay(0.6f));
        else
            UpdateNextButton();

        // Sync controller overlays to this step
        SyncControllerUI(index);

        Debug.Log($"[Guide] ShowStep {index}, isLast={isLast}, assessmentStarted={assessmentStarted}");

        if (isLast && lessonAssessment != null && !assessmentStarted)
        {
            assessmentStarted = true;
            StartCoroutine(DelayedAssessmentStart(lessonIndex));
        }
        else if (isLast && lessonAssessment == null)
        {
            returnButton?.gameObject.SetActive(true);
        }
    }

    IEnumerator EnableNextAfterDelay(float delay)
    {
        if (nextStepButton != null) nextStepButton.interactable = false;
        if (nextStepButtonLabel != null) nextStepButtonLabel.text = "Next ->";
        yield return new WaitForSeconds(delay);
        _nextOnCooldown = false;
        if (!nextBlocked && nextStepButton != null)
            nextStepButton.interactable = true;
    }

    IEnumerator DelayedAssessmentStart(int lesson)
    {
        yield return new WaitForSeconds(3.5f);

        guideCardPanel?.SetActive(false);
        collapsedTab?.SetActive(false);
        complexityTablePanel?.SetActive(false);
        toastPanel?.SetActive(false);

        // Hide all controller overlays — assessment UI takes over completely
        if (graphController != null)
        {
            HideControllerPanel(graphController.instructionText);
            HideControllerPanel(graphController.operationInfoText);
            HideControllerPanel(graphController.statusText);
            HideControllerPanel(graphController.detectionText);

            graphController.mainButtonPanel?.SetActive(false);
            graphController.beginnerButtonPanel?.SetActive(false);
            graphController.intermediateButtonPanel?.SetActive(false);
            graphController.inputPanel?.SetActive(false);
            graphController.inputPanelEdge?.SetActive(false);
            graphController.movementButtonPanel?.SetActive(false);
            graphController.algorithmPanel?.SetActive(false);
            graphController.pathCheckInputPanel?.SetActive(false);
            graphController.degreeInputPanel?.SetActive(false);
        }

        if (lessonAssessment.assessmentRoot != null)
            lessonAssessment.assessmentRoot.SetActive(true);

        lessonAssessment.BeginAssessment(lesson);
    }

    void CheckIfAlreadyDone(int stepIndex)
    {
        switch (guideMode)
        {
            case 1:
                if (stepIndex == 0) nextBlocked = !sceneSpawned;
                break;
            case 2:
                if (stepIndex == 0) nextBlocked = !sceneSpawned;
                break;
            case 3:
                if (stepIndex == 0) nextBlocked = !sceneSpawned;
                if (stepIndex == 2 && didAddNode) nextBlocked = false;
                if (stepIndex == 3 && didAddEdge) nextBlocked = false;
                break;
            case 4:
                if (stepIndex == 0) nextBlocked = !sceneSpawned;
                if (stepIndex == 1 && didBFS) nextBlocked = false;
                if (stepIndex == 2 && didDFS) nextBlocked = false;
                break;
            case 5:
                if (stepIndex == 0) nextBlocked = !sceneSpawned;
                break;
        }
        UpdateNextButton();
    }

    void OnNext()
    {
        if (_nextOnCooldown) return;
        if (!nextBlocked && currentStep < steps.Length - 1)
        {
            _nextOnCooldown = true;
            ShowStep(currentStep + 1);
        }
    }

    void TryUnblock()
    {
        if (nextBlocked)
        {
            nextBlocked     = false;
            _nextOnCooldown = false;
            UpdateNextButton();
        }
    }

    void UpdateNextButton()
    {
        if (nextStepButton == null) return;
        nextStepButton.interactable = !nextBlocked;
        if (nextStepButtonLabel != null)
            nextStepButtonLabel.text = nextBlocked ? "Complete the action ->" : "Next ->";
    }

    void OnMinimise() { guideCardPanel?.SetActive(false); collapsedTab?.SetActive(true); }
    void OnRestore()  { guideCardPanel?.SetActive(true);  collapsedTab?.SetActive(false); }

    void OnReturn()
    {
        // Restore controller to full self-managed mode for sandbox / free-play
        graphController?.SetInstructionSilence(false);

        // Panels already restored by SetInstructionSilence(false) above,
        // but also call Show explicitly in case roots were hidden mid-lesson
        if (graphController != null)
        {
            ShowControllerPanel(graphController.instructionText);
            ShowControllerPanel(graphController.operationInfoText);
            ShowControllerPanel(graphController.detectionText);
            ShowControllerPanel(graphController.statusText);
        }

        PlayerPrefs.SetInt(ARReturnHandler.AR_JUST_RETURNED_KEY, 1);
        PlayerPrefs.SetString(ARReturnHandler.AR_TOPIC_NAME_KEY,
                              PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "graphs"));
        PlayerPrefs.Save();
        SceneManager.LoadScene(PlayerPrefs.GetString("AR_MainScene", mainAppSceneName));
    }

    void UpdateComplexityTable(string activeOp)
    {
        string H(string op, string row) => activeOp == op ? $"> {row}" : $"  {row}";

        string t =
            "Operation             | Time\n" +
            "───────────────────────────────\n" +
            H("BFS",      "BFS Traversal         | O(V+E)")       + "\n" +
            H("DFS",      "DFS Traversal         | O(V+E)")       + "\n" +
            H("DIJKSTRA", "Dijkstra Shortest Path| O((V+E)logV)") + "\n" +
            H("MST",      "Prim's MST            | O(E logV)")    + "\n" +
            H("ADD_NODE", "Add Vertex            | O(1)")         + "\n" +
            H("ADD_EDGE", "Add Edge              | O(1)")         + "\n" +
            H("REMOVE",   "Remove Vertex         | O(E)")         + "\n" +
            "  Adjacency Matrix Space  | O(V^2)"                  + "\n" +
            "  Adjacency List Space    | O(V+E)";

        if (complexityTableText  != null) complexityTableText.text  = t;
        if (activeOperationLabel != null)
            activeOperationLabel.text = string.IsNullOrEmpty(activeOp) ? "" : $"Last: {activeOp}";
    }

    IEnumerator HideAfter(GameObject go, float t)
    { yield return new WaitForSeconds(t); go?.SetActive(false); }

    void ShowToast(Sprite icon, string msg)
    {
        if (toastPanel == null) return;
        if (toastText != null) toastText.text = msg;
        if (toastIcon != null && icon != null) toastIcon.sprite = icon;
        toastPanel.SetActive(true);
        StartCoroutine(HideAfter(toastPanel, 3.5f));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // STEPS PER LESSON — identical to original
    // ──────────────────────────────────────────────────────────────────────────
    Step[] BuildSteps(int mode)
    {
        switch (mode)
        {
            case 1: return new Step[]
            {
                new Step {
                    title = "What is a Graph?",
                    body  =
                        "A GRAPH is a non-linear data structure\n" +
                        "consisting of two parts:\n\n" +
                        "  VERTICES (Nodes) -- the data points\n" +
                        "  EDGES (Connections) -- links between them\n\n" +
                        "Graphs represent RELATIONSHIPS between objects.\n\n" +
                        "-> Choose your Scenario and Difficulty.\n" +
                        "-> Tap a flat surface to place the graph scene.",
                    waitForAction = true
                },
                new Step {
                    title = "Graphs vs Trees",
                    body  =
                        "Unlike Trees, Graphs are more flexible:\n\n" +
                        "TREES:                    GRAPHS:\n" +
                        " No cycles                Can have cycles\n" +
                        " Must have a root          No root needed\n" +
                        " Strict hierarchy          Any connection pattern\n" +
                        " Only top-down             Any direction\n\n" +
                        "A tree is actually a SPECIAL CASE of a graph!\n" +
                        "(A connected, acyclic, undirected graph)\n\n" +
                        "Look at the AR scene -- nodes connect freely!",
                    waitForAction = false
                },
                new Step {
                    title = "Types of Graphs",
                    body  =
                        "UNDIRECTED GRAPH:\n" +
                        "  Edges have no direction -- A <-> B\n" +
                        "  Example: Facebook friendships\n\n" +
                        "DIRECTED GRAPH (Digraph):\n" +
                        "  Edges have direction -- A -> B\n" +
                        "  Example: Twitter follows, web links\n\n" +
                        "WEIGHTED GRAPH:\n" +
                        "  Edges have values (cost, distance)\n" +
                        "  Example: Road map with distances\n\n" +
                        "The AR scene shows a WEIGHTED UNDIRECTED graph.\n" +
                        "Numbers on edges = distances!",
                    waitForAction = false
                },
                new Step {
                    title = "Real-Life Graph Examples",
                    body  =
                        "Graphs are everywhere in real life:\n\n" +
                        "SOCIAL NETWORKS\n" +
                        "   Users = vertices, friendships = edges\n\n" +
                        "GOOGLE MAPS\n" +
                        "   Cities = vertices, roads = edges\n\n" +
                        "COMPUTER NETWORKS\n" +
                        "   Devices = vertices, connections = edges\n\n" +
                        "WEB CRAWLING\n" +
                        "   Pages = vertices, hyperlinks = edges\n\n" +
                        "DEPENDENCY RESOLUTION\n" +
                        "   Packages = vertices, requirements = edges",
                    waitForAction = false
                },
                new Step {
                    title = "Lesson 1 Complete!",
                    body  =
                        "You now understand:\n" +
                        "- What a graph is (vertices + edges)\n" +
                        "- How graphs differ from trees\n" +
                        "- Undirected, directed, and weighted graphs\n" +
                        "- Real-world graph applications\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 2: return new Step[]
            {
                new Step {
                    title = "Vertices & Edges",
                    body  =
                        "VERTEX (Node) -- A fundamental unit of a graph.\n" +
                        "  In the AR scene: each building/island/module.\n\n" +
                        "EDGE -- A connection between two vertices.\n" +
                        "  In the AR scene: each road/bridge/tube.\n\n" +
                        "ADJACENT VERTICES:\n" +
                        "  Two vertices connected by an edge.\n" +
                        "  If A-B exists, A and B are adjacent.\n\n" +
                        "-> Place the scene and observe the graph.",
                    waitForAction = true
                },
                new Step {
                    title = "Degree of a Vertex",
                    body  =
                        "The DEGREE of a vertex = number of edges\n" +
                        "connected to that vertex.\n\n" +
                        "UNDIRECTED GRAPH:\n" +
                        "  Degree = total edges at vertex\n" +
                        "  Example: If A connects to B, C, D -> degree = 3\n\n" +
                        "DIRECTED GRAPH:\n" +
                        "  IN-DEGREE  = incoming edges\n" +
                        "  OUT-DEGREE = outgoing edges\n\n" +
                        "A vertex with HIGH degree is called a HUB.\n" +
                        "-> Use the DEGREE button to check vertices!",
                    waitForAction = false
                },
                new Step {
                    title = "Paths & Cycles",
                    body  =
                        "PATH:\n" +
                        "  A sequence of vertices connected by edges.\n" +
                        "  Example: A -> B -> C -> D\n" +
                        "  No vertex repeated (simple path).\n\n" +
                        "CYCLE:\n" +
                        "  A path that starts and ends at the same vertex.\n" +
                        "  Example: A -> B -> C -> A\n\n" +
                        "CONNECTED GRAPH:\n" +
                        "  There is a path between EVERY pair of vertices.\n" +
                        "  All nodes can reach each other!\n\n" +
                        "-> Use PATH CHECK to test connections!",
                    waitForAction = false
                },
                new Step {
                    title = "Weighted & Unweighted",
                    body  =
                        "WEIGHTED GRAPH:\n" +
                        "  Each edge has a numeric value (weight).\n" +
                        "  Represents: cost, distance, time, capacity.\n" +
                        "  Example: Road map -- edges show km.\n\n" +
                        "UNWEIGHTED GRAPH:\n" +
                        "  Edges have no numeric values.\n" +
                        "  Only records if a connection exists.\n" +
                        "  Example: Social network friendships.\n\n" +
                        "The AR scene uses a WEIGHTED graph.\n" +
                        "Numbers on each edge = distance units.",
                    waitForAction = false
                },
                new Step {
                    title = "Lesson 2 Complete!",
                    body  =
                        "You now understand:\n" +
                        "- Vertices, edges, adjacent vertices\n" +
                        "- Degree, in-degree, out-degree\n" +
                        "- Paths and cycles\n" +
                        "- Connected graphs\n" +
                        "- Weighted vs unweighted graphs\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 3: return new Step[]
            {
                new Step {
                    title = "Two Ways to Store a Graph",
                    body  =
                        "Computers store graphs in two main ways:\n\n" +
                        "1. ADJACENCY MATRIX\n" +
                        "   A 2D array (V x V grid of 0s and 1s)\n\n" +
                        "2. ADJACENCY LIST\n" +
                        "   Each vertex stores a list of its neighbours\n\n" +
                        "Both represent the same graph -- just differently!\n\n" +
                        "-> Place the scene, then we'll explore both.",
                    waitForAction = true
                },
                new Step {
                    title = "Adjacency Matrix",
                    body  =
                        "ADJACENCY MATRIX:\n" +
                        "  A V x V 2D array.\n" +
                        "  matrix[i][j] = 1  if edge exists\n" +
                        "  matrix[i][j] = 0  if no edge\n\n" +
                        "Example (A, B, C):\n" +
                        "     A  B  C\n" +
                        "  A[ 0  1  1 ]\n" +
                        "  B[ 1  0  0 ]\n" +
                        "  C[ 1  0  0 ]\n\n" +
                        "+ Easy edge lookup -- O(1)\n" +
                        "+ Simple implementation\n" +
                        "- Uses O(V^2) memory\n" +
                        "- Wastes space for sparse graphs",
                    waitForAction = false
                },
                new Step {
                    title = "Adjacency List",
                    body  =
                        "ADJACENCY LIST:\n" +
                        "  Each vertex stores a list of its neighbours.\n\n" +
                        "Example:\n" +
                        "  A -> [B, C]\n" +
                        "  B -> [A]\n" +
                        "  C -> [A]\n\n" +
                        "+ Memory efficient -- O(V+E)\n" +
                        "+ Great for sparse graphs\n" +
                        "- Edge lookup is slower -- O(degree)\n" +
                        "- More complex to implement\n\n" +
                        "-> Add a new vertex (node) to the graph!\n" +
                        "   Tap ADD NODE and position it.",
                    waitForAction = true
                },
                new Step {
                    title = "Connect Your New Vertex",
                    body  =
                        "Now your new vertex exists -- but it's isolated!\n" +
                        "In the adjacency list, it shows: NewNode -> []\n\n" +
                        "To represent a relationship, add an EDGE.\n\n" +
                        "In memory terms:\n" +
                        "  Adjacency List:   O(1) to add a vertex\n" +
                        "                    O(1) to add an edge\n" +
                        "  Adjacency Matrix: O(V^2) to resize when adding!\n\n" +
                        "-> Tap ADD EDGE and connect two nodes by name.\n" +
                        "   This is why adjacency lists are preferred!",
                    waitForAction = true
                },
                new Step {
                    title = "Lesson 3 Complete!",
                    body  =
                        "You now understand:\n" +
                        "- Adjacency Matrix -- O(V^2) space\n" +
                        "- Adjacency List -- O(V+E) space\n" +
                        "- When to use each representation\n" +
                        "- Adding vertices and edges in practice\n\n" +
                        "Key rule: Sparse graph -> use Adjacency List!\n" +
                        "          Dense graph  -> Matrix may be OK.\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 4: return new Step[]
            {
                new Step {
                    title = "Graph Traversal",
                    body  =
                        "TRAVERSAL means visiting EVERY vertex\n" +
                        "in a graph exactly once.\n\n" +
                        "Unlike arrays, graphs have no single start\n" +
                        "or fixed order -- so we need algorithms!\n\n" +
                        "Two main traversal algorithms:\n" +
                        "  BFS -- Breadth-First Search\n" +
                        "  DFS -- Depth-First Search\n\n" +
                        "Both visit all vertices: Time O(V+E)\n\n" +
                        "-> Place the scene first.",
                    waitForAction = true
                },
                new Step {
                    title = "Breadth-First Search (BFS)",
                    body  =
                        "BFS visits nodes LEVEL BY LEVEL.\n\n" +
                        "Uses a QUEUE (FIFO).\n\n" +
                        "Steps:\n" +
                        "  1. Start at source. Mark visited. Enqueue.\n" +
                        "  2. Dequeue vertex. Visit all unvisited neighbours.\n" +
                        "  3. Mark neighbours visited. Enqueue them.\n" +
                        "  4. Repeat until queue is empty.\n\n" +
                        "Good for: Shortest path (unweighted), level order.\n\n" +
                        "Time: O(V+E)  |  Space: O(V)\n\n" +
                        "-> Tap BFS to watch the animation!",
                    waitForAction = true
                },
                new Step {
                    title = "Depth-First Search (DFS)",
                    body  =
                        "DFS explores as FAR as possible before backtracking.\n\n" +
                        "Uses RECURSION or a STACK (LIFO).\n\n" +
                        "Steps:\n" +
                        "  1. Start at source. Mark visited.\n" +
                        "  2. Recursively visit unvisited neighbours.\n" +
                        "  3. Backtrack when no unvisited neighbours remain.\n\n" +
                        "Good for: Detecting cycles, topological sort,\n" +
                        "          exploring all paths, maze solving.\n\n" +
                        "Time: O(V+E)  |  Space: O(V)\n\n" +
                        "-> Tap DFS to watch the animation!",
                    waitForAction = true
                },
                new Step {
                    title = "BFS vs DFS Comparison",
                    body  =
                        "Feature        | BFS          | DFS\n" +
                        "────────────────────────────────────\n" +
                        "Data struct    | Queue        | Stack/Recursion\n" +
                        "Order          | Level-by-lvl | Branch-first\n" +
                        "Shortest path  | Yes          | No\n" +
                        "Cycle detect   | Yes          | Yes\n" +
                        "Memory (dense) | High O(V)    | Low O(depth)\n" +
                        "Time           | O(V+E)       | O(V+E)\n\n" +
                        "Both track visited nodes to avoid infinite loops!",
                    waitForAction = false
                },
                new Step {
                    title = "Lesson 4 Complete!",
                    body  =
                        "You now understand:\n" +
                        "- BFS -- level-by-level using a Queue\n" +
                        "- DFS -- depth-first using Stack/Recursion\n" +
                        "- Both run in O(V+E) time\n" +
                        "- When to choose BFS vs DFS\n" +
                        "- Visited-node tracking prevents cycles\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 5: return new Step[]
            {
                new Step {
                    title = "Graph Algorithms",
                    body  =
                        "Beyond BFS and DFS, graphs power advanced algorithms:\n\n" +
                        "DIJKSTRA'S ALGORITHM\n" +
                        "   Finds the shortest path in a WEIGHTED graph.\n" +
                        "   Uses a priority queue.\n" +
                        "   Time: O((V+E) log V)\n\n" +
                        "PRIM'S / KRUSKAL'S MST\n" +
                        "   Minimum Spanning Tree -- connect all vertices\n" +
                        "   with minimum total edge weight (cost).\n" +
                        "   Time: O(E log V)\n\n" +
                        "-> Place the scene, then try Dijkstra and MST!",
                    waitForAction = true
                },
                new Step {
                    title = "Dijkstra's Algorithm",
                    body  =
                        "DIJKSTRA finds the SHORTEST WEIGHTED PATH.\n\n" +
                        "How it works:\n" +
                        "  1. Set all distances to infinity.\n" +
                        "  2. Set source distance = 0.\n" +
                        "  3. Pick unvisited vertex with smallest distance.\n" +
                        "  4. Update distances to its neighbours.\n" +
                        "  5. Repeat until all vertices visited.\n\n" +
                        "Result: Minimum cost to reach EVERY vertex.\n\n" +
                        "Used in: GPS, network routing, game pathfinding.\n\n" +
                        "-> Tap DIJKSTRA to see it in action!",
                    waitForAction = false
                },
                new Step {
                    title = "Advantages & Limitations",
                    body  =
                        "ADVANTAGES of Graphs:\n" +
                        "- Represents complex relationships\n" +
                        "- Flexible structure -- any shape\n" +
                        "- Powers real-world network systems\n" +
                        "- Supports advanced algorithms\n" +
                        "- Both directed and undirected use cases\n\n" +
                        "LIMITATIONS:\n" +
                        "- More complex to implement than arrays/trees\n" +
                        "- Memory depends on representation choice\n" +
                        "- Traversal needs visited-node tracking\n" +
                        "- Dense graphs can be memory-expensive",
                    waitForAction = false
                },
                new Step {
                    title = "Full Big-O Summary",
                    body  =
                        "From the syllabus -- complete summary:\n\n" +
                        "BFS Traversal:          O(V+E)\n" +
                        "DFS Traversal:          O(V+E)\n" +
                        "Dijkstra's (w/ PQ):     O((V+E) log V)\n" +
                        "Prim's MST:             O(E log V)\n" +
                        "Add Vertex:             O(1)\n" +
                        "Add Edge:               O(1)\n" +
                        "Remove Vertex:          O(E)\n\n" +
                        "Space Complexity:\n" +
                        "  Adjacency Matrix:     O(V^2)\n" +
                        "  Adjacency List:       O(V+E)\n\n" +
                        "-> Perform operations to light up the table!",
                    waitForAction = false
                },
                new Step {
                    title = "Lesson 5 Complete!",
                    body  =
                        "You have mastered Graphs!\n\n" +
                        "- Graph definition -- vertices + edges\n" +
                        "- Graph vs tree differences\n" +
                        "- Terminologies: degree, path, cycle\n" +
                        "- Adjacency matrix vs adjacency list\n" +
                        "- BFS -- queue, level-by-level\n" +
                        "- DFS -- stack/recursion, depth-first\n" +
                        "- Dijkstra's shortest path\n" +
                        "- Prim's MST\n" +
                        "- Full Big-O complexity table\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            default: return new Step[0];
        }
    }
}