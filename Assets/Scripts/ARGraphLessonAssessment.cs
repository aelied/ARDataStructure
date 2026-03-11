using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ARGraphLessonAssessment.cs
/// ===========================
/// Attaches to the same GameObject as ARGraphLessonGuide.
///
/// FLOW:
///   1. ARGraphLessonGuide finishes its last step →
///      calls ARGraphLessonAssessment.BeginAssessment(lessonIndex)
///   2. Assessment panel fades in with instructions
///   3. Student performs AR actions; each action is graded
///   4. Results panel shown (score / grade / breakdown)
///   5. Student taps "Return" → back to main app
///
/// LESSON ASSESSMENTS:
///   L1 (0): Identify graph components — add a node, observe the scene
///   L2 (1): Use Degree check, Path Check — demonstrate terminologies
///   L3 (2): Add node + Add edge — demonstrate both representations in action
///   L4 (3): Run BFS, run DFS, compare — demonstrate both traversals
///   L5 (4): Run Dijkstra + MST + operations — light up complexity table
///
/// WIRING (Inspector):
///   • lessonGuide      → drag ARGraphLessonGuide component
///   • graphController  → drag InteractiveCityGraph component
///   • assessmentCanvas → Canvas
///   All text/button fields below.
/// </summary>
public class ARGraphLessonAssessment : MonoBehaviour
{
    // ── References ─────────────────────────────────────────────────────────────
    [Header("Sibling Scripts")]
    public ARGraphLessonGuide    lessonGuide;
    public InteractiveCityGraph  graphController;

    // ── Assessment Canvas ──────────────────────────────────────────────────────
    [Header("Assessment Canvas")]
    public Canvas          assessmentCanvas;
    public GameObject      assessmentRoot;

    public GameObject      introPanel;
    public TextMeshProUGUI introTitleText;
    public TextMeshProUGUI introBodyText;
    public Button          startAssessmentButton;

    public GameObject      taskPanel;
    public TextMeshProUGUI taskTitleText;
    public TextMeshProUGUI taskInstructionText;
    public TextMeshProUGUI taskProgressText;
    public TextMeshProUGUI taskTimerText;
    public Image           taskProgressBarFill;
    public GameObject      taskFeedbackBanner;
    public TextMeshProUGUI taskFeedbackText;
    public GameObject      taskMinimiseButton;
    public GameObject      taskCollapsedTab;
    public GameObject      taskCardPanel;

    public GameObject      resultsPanel;
    public TextMeshProUGUI resultsTitleText;
    public TextMeshProUGUI resultsScoreText;
    public TextMeshProUGUI resultsGradeText;
    public TextMeshProUGUI resultsBreakdownText;
    public TextMeshProUGUI resultsTipText;
    public Button          retryAssessmentButton;
    public Button          returnButton;
    public Image           gradeRingImage;

    // ── Settings ───────────────────────────────────────────────────────────────
    [Header("Settings")]
    public float taskTimeLimit     = 90f;
    public bool  showTimer         = true;
    public string mainAppSceneName = "MainScene";

    // ── Internal State ─────────────────────────────────────────────────────────
    struct AssessmentTask
    {
        public string title;
        public string instruction;
        public int    maxPoints;
        public System.Func<bool> completionCheck;
    }

    private int              lessonIndex      = -1;
    private AssessmentTask[] tasks;
    private int              currentTaskIndex = 0;
    private int              totalScore       = 0;
    private int              maxPossibleScore = 0;
    private List<int>        taskScores       = new List<int>();
    private List<string>     taskNames        = new List<string>();

    private bool             assessmentActive = false;
    private float            taskTimer        = 0f;
    private bool             taskComplete     = false;

    // Per-task tracking flags
    private bool t_nodeAdded       = false;
    private bool t_edgeAdded       = false;
    private bool t_nodeRemoved     = false;
    private bool t_bfsDone         = false;
    private bool t_dfsDone         = false;
    private bool t_dijkstraDone    = false;
    private bool t_mstDone         = false;
    private bool t_degreeChecked   = false;
    private bool t_pathCheckDone   = false;
    private bool t_pathExistsFound = false;
    private int  t_operationsCount = 0;

    // ──────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        HideAssessment();

        if (retryAssessmentButton != null)
            retryAssessmentButton.onClick.AddListener(RetryAssessment);
        if (returnButton != null)
            returnButton.onClick.AddListener(OnReturnClicked);
       if (taskMinimiseButton != null)
{
    var btn = taskMinimiseButton.GetComponent<Button>()
           ?? taskMinimiseButton.GetComponentInChildren<Button>();
    if (btn != null) btn.onClick.AddListener(OnTaskMinimise);
}
if (taskCollapsedTab != null)
{
    var btn = taskCollapsedTab.GetComponent<Button>()
           ?? taskCollapsedTab.GetComponentInChildren<Button>();
    if (btn != null) btn.onClick.AddListener(OnTaskRestore);
}
    }

    void OnTaskMinimise()
    {
        SetActive(taskPanel,          false);
        SetActive(taskCardPanel,      false);
        SetActive(taskFeedbackBanner, false);
        SetActive(taskCollapsedTab,   true);
    }

    void OnTaskRestore()
    {
        SetActive(taskPanel,        true);
        SetActive(taskCardPanel,    true);
        SetActive(taskCollapsedTab, false);
    }

    void OnEnable()
    {
        if (!assessmentActive) HideAssessment();
    }

    void HideAssessment()
    {
        if (assessmentRoot != null) assessmentRoot.SetActive(false);
        assessmentActive = false;
        StopAllCoroutines();
        Debug.Log("[GraphAssessment] HideAssessment — root hidden");
    }

    // ── PUBLIC ENTRY ───────────────────────────────────────────────────────────
    public void BeginAssessment(int lesson)
    {
        Debug.Log($"[GraphAssessment] BeginAssessment called for lesson {lesson}");
        assessmentActive = true;

        if (assessmentRoot != null) assessmentRoot.SetActive(true);

        if (introPanel   == null) introPanel   = GameObject.Find("AssessmentIntroPanel");
        if (taskPanel    == null) taskPanel    = GameObject.Find("AssessmentTaskPanel");
        if (resultsPanel == null) resultsPanel = GameObject.Find("AssessmentResultsPanel");

        lessonIndex = lesson;
        tasks       = BuildTasks(lesson);
        if (tasks == null || tasks.Length == 0)
        {
            Debug.LogWarning($"[GraphAssessment] No tasks for lesson {lesson}");
            return;
        }

        maxPossibleScore = 0;
        foreach (var t in tasks) maxPossibleScore += t.maxPoints;

        totalScore       = 0;
        currentTaskIndex = 0;
        taskScores.Clear();
        taskNames.Clear();
        foreach (var t in tasks) taskNames.Add(t.title);

        ShowIntroPanel();
    }

    // ── INTRO PANEL ────────────────────────────────────────────────────────────
    void ShowIntroPanel()
    {
        if (assessmentCanvas != null)
        {
            assessmentCanvas.gameObject.SetActive(true);
            assessmentCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            assessmentCanvas.sortingOrder = 100;
        }

        SetActive(introPanel,   true);
        SetActive(taskPanel,    false);
        SetActive(resultsPanel, false);

        if (introTitleText != null)
            introTitleText.text = $"Lesson {lessonIndex + 1} Assessment";

        if (introBodyText != null)
            introBodyText.text =
                $"You completed the lesson guide!\n\n" +
                $"Now prove your understanding.\n" +
                $"You have {tasks.Length} task(s) to complete.\n" +
                $"Total points available: {maxPossibleScore}\n\n" +
                "Tap START when ready.";

        if (startAssessmentButton == null)
            startAssessmentButton = introPanel?.GetComponentInChildren<Button>(true);

        if (startAssessmentButton != null)
        {
            startAssessmentButton.onClick.RemoveAllListeners();
            startAssessmentButton.onClick.AddListener(OnStartButtonClicked);
            startAssessmentButton.interactable = true;

            CanvasGroup[] groups = startAssessmentButton.GetComponentsInParent<CanvasGroup>();
            foreach (var cg in groups) { cg.interactable = true; cg.blocksRaycasts = true; }
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && assessmentActive && introPanel != null && introPanel.activeSelf)
        {
            if (startAssessmentButton != null)
            {
                startAssessmentButton.onClick.RemoveAllListeners();
                startAssessmentButton.onClick.AddListener(OnStartButtonClicked);
            }
        }
    }

    void OnStartButtonClicked()
    {
        SetActive(introPanel, false);
        assessmentActive  = true;
        currentTaskIndex  = 0;
        ResetTaskFlags();
        StartTask(0);
    }

    // ── TASK FLOW ──────────────────────────────────────────────────────────────
    void StartTask(int index)
    {
        if (index >= tasks.Length) { ShowResults(); return; }

        SetActive(taskCardPanel,    true);
        SetActive(taskCollapsedTab, false);

        taskComplete = false;
        taskTimer    = taskTimeLimit > 0 ? taskTimeLimit : float.MaxValue;
        ResetTaskFlags();

        SetActive(taskPanel,          true);
        SetActive(taskFeedbackBanner, false);

        // Re-enable graph controller buttons for assessment
        if (graphController != null)
        {
            graphController.mainButtonPanel?.SetActive(true);
            graphController.beginnerButtonPanel?.SetActive(true);
        }

        var task = tasks[index];
        if (taskTitleText       != null) taskTitleText.text       = task.title;
        if (taskInstructionText != null) taskInstructionText.text = task.instruction;
        if (taskProgressText    != null) taskProgressText.text    = $"Task {index + 1} / {tasks.Length}";
        if (taskProgressBarFill != null) taskProgressBarFill.fillAmount = (float)(index + 1) / tasks.Length;
        if (taskTimerText       != null) taskTimerText.gameObject.SetActive(showTimer && taskTimeLimit > 0);

        StartCoroutine(RunTaskLoop(index));
    }

    IEnumerator RunTaskLoop(int index)
    {
        var task = tasks[index];

        while (!taskComplete)
        {
            if (task.completionCheck != null && task.completionCheck())
            {
                taskComplete = true;
                break;
            }

            if (taskTimeLimit > 0)
            {
                taskTimer -= Time.deltaTime;
                if (taskTimerText != null)
                    taskTimerText.text = $"⏱ {Mathf.CeilToInt(taskTimer)}s";

                if (taskTimer <= 0f)
                {
                    taskComplete = true;
                    ShowFeedback(false, "Time's up!");
                    yield return new WaitForSeconds(1.5f);
                    break;
                }
            }

            yield return null;
        }

        int earned = GradeTask(index);
        taskScores.Add(earned);
        totalScore += earned;

        bool passed = earned >= Mathf.CeilToInt(task.maxPoints * 0.6f);
        ShowFeedback(passed,
            passed ? $" +{earned} pts — Well done!" : $" +{earned}/{task.maxPoints} pts");

        yield return new WaitForSeconds(2.0f);
        SetActive(taskFeedbackBanner, false);

        currentTaskIndex++;
        if (currentTaskIndex < tasks.Length)
            StartTask(currentTaskIndex);
        else
            ShowResults();
    }

    // ── NOTIFICATION METHODS (called from ARGraphLessonGuide) ─────────────────
    public void NotifyTraversal(string type)
    {
        switch (type)
        {
            case "bfs": t_bfsDone  = true; break;
            case "dfs": t_dfsDone  = true; break;
        }
        t_operationsCount++;
    }

    public void NotifyOperation(string type)
    {
        switch (type)
        {
            case "dijkstra": t_dijkstraDone = true; break;
            case "mst":      t_mstDone      = true; break;
        }
        t_operationsCount++;
    }

    public void NotifyNodeAdded()
    {
        t_nodeAdded = true;
        t_operationsCount++;
    }

    public void NotifyEdgeAdded()
    {
        t_edgeAdded = true;
        t_operationsCount++;
    }

    public void NotifyNodeRemoved()
    {
        t_nodeRemoved = true;
        t_operationsCount++;
    }

    public void NotifyPathCheck(bool pathExists)
    {
        t_pathCheckDone   = true;
        if (pathExists) t_pathExistsFound = true;
        t_operationsCount++;
    }

    public void NotifyDegreeChecked()
    {
        t_degreeChecked = true;
        t_operationsCount++;
    }

    // ── GRADING ────────────────────────────────────────────────────────────────
    int GradeTask(int taskIndex)
    {
        if (tasks == null || taskIndex >= tasks.Length) return 0;
        var task = tasks[taskIndex];

        switch (lessonIndex)
        {
            // ── L1: Introduction ─────────────────────────────────────────────
            case 0:
                switch (taskIndex)
                {
                    case 0: return IsSceneSpawned() ? task.maxPoints : 0;
                    case 1: return t_nodeAdded ? task.maxPoints : 0;
                    case 2: return t_edgeAdded ? task.maxPoints : 0;
                }
                break;

            // ── L2: Terminologies ────────────────────────────────────────────
            case 1:
                switch (taskIndex)
                {
                    case 0: return IsSceneSpawned() ? task.maxPoints : 0;
                    case 1: return t_degreeChecked ? task.maxPoints : 0;
                    case 2: return t_pathCheckDone ? task.maxPoints : 0;
                    case 3: return t_pathExistsFound ? task.maxPoints :
                                   t_pathCheckDone   ? Mathf.RoundToInt(task.maxPoints * 0.5f) : 0;
                }
                break;

            // ── L3: Representation ───────────────────────────────────────────
            case 2:
                switch (taskIndex)
                {
                    case 0: return IsSceneSpawned() ? task.maxPoints : 0;
                    case 1: return t_nodeAdded ? task.maxPoints : 0;
                    case 2: return t_edgeAdded ? task.maxPoints : 0;
                    case 3:
                        int repScore = (t_nodeAdded ? 1 : 0) + (t_edgeAdded ? 1 : 0);
                        return Mathf.RoundToInt(task.maxPoints * (repScore / 2f));
                }
                break;

            // ── L4: Traversal ────────────────────────────────────────────────
            case 3:
                switch (taskIndex)
                {
                    case 0: return t_bfsDone ? task.maxPoints : 0;
                    case 1: return t_dfsDone ? task.maxPoints : 0;
                    case 2:
                        int travScore = (t_bfsDone ? 1 : 0) + (t_dfsDone ? 1 : 0);
                        return Mathf.RoundToInt(task.maxPoints * (travScore / 2f));
                }
                break;

            // ── L5: Big-O Summary ────────────────────────────────────────────
            case 4:
                int ops = (t_bfsDone      ? 1 : 0) +
                          (t_dfsDone      ? 1 : 0) +
                          (t_dijkstraDone ? 1 : 0) +
                          (t_mstDone      ? 1 : 0) +
                          (t_nodeAdded    ? 1 : 0) +
                          (t_edgeAdded    ? 1 : 0) +
                          (t_nodeRemoved  ? 1 : 0);
                return Mathf.RoundToInt(task.maxPoints * (ops / 7f));
        }

        return 0;
    }

    // ── RESULTS PANEL ──────────────────────────────────────────────────────────
    void ShowResults()
    {
        assessmentActive = false;
        SetActive(taskPanel,    false);
        SetActive(resultsPanel, true);

        float pct = maxPossibleScore > 0 ? (float)totalScore / maxPossibleScore : 0f;
        string grade; Color gradeColor;

        if      (pct >= 0.90f) { grade = "A";            gradeColor = new Color(0.2f, 0.9f, 0.3f); }
        else if (pct >= 0.75f) { grade = "B";            gradeColor = new Color(0.4f, 0.7f, 1.0f); }
        else if (pct >= 0.60f) { grade = "C";            gradeColor = new Color(1.0f, 0.8f, 0.2f); }
        else                   { grade = "Needs Review"; gradeColor = new Color(1.0f, 0.3f, 0.3f); }

        if (resultsTitleText != null) resultsTitleText.text = $"Lesson {lessonIndex + 1} Complete!";
        if (resultsScoreText != null) resultsScoreText.text = $"{totalScore} / {maxPossibleScore}  ({Mathf.RoundToInt(pct * 100)}%)";
        if (resultsGradeText != null) { resultsGradeText.text = grade; resultsGradeText.color = gradeColor; }
        if (gradeRingImage   != null) { gradeRingImage.fillAmount = pct; gradeRingImage.color  = gradeColor; }

        string breakdown = "Task Breakdown:\n";
        for (int i = 0; i < taskScores.Count && i < taskNames.Count; i++)
        {
            int max  = i < tasks.Length ? tasks[i].maxPoints : 0;
            string tick = taskScores[i] >= Mathf.CeilToInt(max * 0.6f) ? "" : "";
            breakdown += $"{tick} {taskNames[i]}:  {taskScores[i]}/{max}\n";
        }
        if (resultsBreakdownText != null) resultsBreakdownText.text = breakdown;
        if (resultsTipText       != null) resultsTipText.text       = BuildTip(lessonIndex, pct);

        PlayerPrefs.SetInt   ($"AR_Assessment_Graph_L{lessonIndex}_Score", totalScore);
        PlayerPrefs.SetInt   ($"AR_Assessment_Graph_L{lessonIndex}_Max",   maxPossibleScore);
        PlayerPrefs.SetString($"AR_Assessment_Graph_L{lessonIndex}_Grade", grade);
        PlayerPrefs.Save();
    }

    string BuildTip(int lesson, float pct)
    {
        if (pct >= 0.9f) return "Excellent work! You have a strong grasp of Graph data structures.";
        switch (lesson)
        {
            case 0: return "Review: Graphs have vertices (nodes) and edges (connections). Unlike trees, graphs have no required root and can have cycles.";
            case 1: return "Review: Degree = number of edges at a vertex. A path is a sequence of connected vertices. A cycle starts and ends at the same vertex.";
            case 2: return "Review: Adjacency Matrix uses O(V²) space. Adjacency List uses O(V+E) — better for sparse graphs!";
            case 3: return "Review: BFS uses a Queue and visits level-by-level O(V+E). DFS uses recursion/Stack and explores depth-first O(V+E).";
            case 4: return "Review: Dijkstra finds shortest weighted path O((V+E)logV). Prim's MST connects all nodes with minimum total edge cost O(E logV).";
            default: return "Keep practising in AR to reinforce your understanding of Graphs!";
        }
    }

    // ── RETRY ──────────────────────────────────────────────────────────────────
    void RetryAssessment()
    {
        SetActive(resultsPanel, false);
        totalScore       = 0;
        currentTaskIndex = 0;
        taskScores.Clear();
        OnStartButtonClicked();
    }

    // ── RETURN ─────────────────────────────────────────────────────────────────
    void OnReturnClicked()
    {
        HideAssessment();
        PlayerPrefs.SetInt(ARReturnHandler.AR_JUST_RETURNED_KEY, 1);
        PlayerPrefs.SetString(ARReturnHandler.AR_TOPIC_NAME_KEY,
                              PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "graphs"));
        PlayerPrefs.Save();
        SceneManager.LoadScene(PlayerPrefs.GetString("AR_MainScene", mainAppSceneName));
    }

    // ── TASK DEFINITIONS ───────────────────────────────────────────────────────
    AssessmentTask[] BuildTasks(int lesson)
    {
        switch (lesson)
        {
            // =================================================================
            // L1 — Introduction to Graphs
            // =================================================================
            case 0: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Place the Graph Scene",
                    instruction =
                        "Point your camera at a flat surface\n" +
                        "and tap to place the graph scene.\n\n" +
                        "A graph has VERTICES (nodes)\n" +
                        "and EDGES (connections between them).\n\n" +
                        "You'll see nodes and roads appear!",
                    maxPoints       = 25,
                    completionCheck = () => IsSceneSpawned()
                },
                new AssessmentTask
                {
                    title       = "Add a New Vertex",
                    instruction =
                        "Use ADD NODE to add a new vertex\n" +
                        "to the existing graph.\n\n" +
                        "Remember:\n" +
                        "  Vertices = the data points\n" +
                        "  (cities, islands, or modules)\n\n" +
                        "This demonstrates that adding\n" +
                        "a vertex is O(1) — constant time!",
                    maxPoints       = 35,
                    completionCheck = () => t_nodeAdded
                },
                new AssessmentTask
                {
                    title       = "Connect Your Vertex",
                    instruction =
                        "Use ADD EDGE to connect\n" +
                        "your new vertex to an existing one.\n\n" +
                        "This creates a RELATIONSHIP\n" +
                        "between two vertices.\n\n" +
                        "Adding an edge is also O(1)!\n\n" +
                        "Enter two vertex names to connect.",
                    maxPoints       = 40,
                    completionCheck = () => t_edgeAdded
                },
            };

            // =================================================================
            // L2 — Graph Terminologies
            // =================================================================
            case 1: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Place the Graph Scene",
                    instruction =
                        "Point your camera at a flat surface\n" +
                        "and tap to place the graph scene.\n\n" +
                        "You'll use this scene to demonstrate\n" +
                        "graph terminologies!",
                    maxPoints       = 15,
                    completionCheck = () => IsSceneSpawned()
                },
                new AssessmentTask
                {
                    title       = "Check a Vertex's Degree",
                    instruction =
                        "Use the DEGREE button to check\n" +
                        "any vertex in the graph.\n\n" +
                        "Degree = number of edges\n" +
                        "connected to that vertex.\n\n" +
                        "Which vertex has the highest degree?\n" +
                        "That's the HUB of this graph!",
                    maxPoints       = 25,
                    completionCheck = () => t_degreeChecked
                },
                new AssessmentTask
                {
                    title       = "Check if a Path Exists",
                    instruction =
                        "Use PATH CHECK and enter\n" +
                        "two vertex names.\n\n" +
                        "A PATH is a sequence of connected\n" +
                        "vertices between two nodes.\n\n" +
                        "If a path exists → CONNECTED!\n" +
                        "If not → they are DISCONNECTED.\n\n" +
                        "Try two vertices that should be connected.",
                    maxPoints       = 30,
                    completionCheck = () => t_pathCheckDone
                },
                new AssessmentTask
                {
                    title       = "Find a Connected Pair",
                    instruction =
                        "Use PATH CHECK again and find\n" +
                        "a pair that HAS a path.\n\n" +
                        "In a CONNECTED GRAPH, every\n" +
                        "pair of vertices has a path.\n\n" +
                        "The initial scene is fully connected —\n" +
                        "can you verify this?",
                    maxPoints       = 30,
                    completionCheck = () => t_pathExistsFound
                },
            };

            // =================================================================
            // L3 — Graph Representation
            // =================================================================
            case 2: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Place the Graph Scene",
                    instruction =
                        "Tap a flat surface to place the scene.\n\n" +
                        "The scene demonstrates a graph stored\n" +
                        "using an ADJACENCY LIST:\n\n" +
                        "Each vertex keeps a list of its neighbours.",
                    maxPoints       = 15,
                    completionCheck = () => IsSceneSpawned()
                },
                new AssessmentTask
                {
                    title       = "Add a Vertex (Adjacency List)",
                    instruction =
                        "Use ADD NODE to add a new vertex.\n\n" +
                        "In an ADJACENCY LIST:\n" +
                        "  Adding a vertex = O(1)\n" +
                        "  New vertex starts with empty list: []\n\n" +
                        "In an ADJACENCY MATRIX:\n" +
                        "  Adding a vertex = O(V²) to resize!\n\n" +
                        "This is why Adjacency List is preferred\n" +
                        "for dynamic graphs.",
                    maxPoints       = 25,
                    completionCheck = () => t_nodeAdded
                },
                new AssessmentTask
                {
                    title       = "Add an Edge (Update Adjacency List)",
                    instruction =
                        "Use ADD EDGE to connect two vertices.\n\n" +
                        "In an ADJACENCY LIST:\n" +
                        "  Adding an edge = O(1)\n" +
                        "  Add neighbour to both lists.\n\n" +
                        "In an ADJACENCY MATRIX:\n" +
                        "  Set matrix[i][j] = 1 — also O(1)\n" +
                        "  But uses O(V²) memory overall!\n\n" +
                        "Enter two vertex names to connect.",
                    maxPoints       = 30,
                    completionCheck = () => t_edgeAdded
                },
                new AssessmentTask
                {
                    title       = "Demonstrate Both Representations",
                    instruction =
                        "Add at least one more node\n" +
                        "AND connect it with an edge.\n\n" +
                        "This mirrors what the ADJACENCY LIST\n" +
                        "looks like in memory:\n\n" +
                        "  NewNode → [ConnectedNode]\n" +
                        "  ConnectedNode → [..., NewNode]\n\n" +
                        "Space: O(V+E) — efficient!",
                    maxPoints       = 30,
                    completionCheck = () => t_nodeAdded && t_edgeAdded
                },
            };

            // =================================================================
            // L4 — Graph Traversal
            // =================================================================
            case 3: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Run BFS",
                    instruction =
                        "Tap the BFS button to run\n" +
                        "Breadth-First Search.\n\n" +
                        "BFS visits nodes LEVEL BY LEVEL\n" +
                        "using a QUEUE.\n\n" +
                        "Watch: nodes closest to the start\n" +
                        "are visited first!\n\n" +
                        "Time: O(V+E)  |  Space: O(V)",
                    maxPoints       = 30,
                    completionCheck = () => t_bfsDone
                },
                new AssessmentTask
                {
                    title       = "Run DFS",
                    instruction =
                        "Tap the DFS button to run\n" +
                        "Depth-First Search.\n\n" +
                        "DFS explores one BRANCH FULLY\n" +
                        "before backtracking. Uses RECURSION.\n\n" +
                        "Watch: DFS goes deep before going wide!\n\n" +
                        "Compare the order with BFS —\n" +
                        "same nodes, different order!\n\n" +
                        "Time: O(V+E)  |  Space: O(V)",
                    maxPoints       = 30,
                    completionCheck = () => t_dfsDone
                },
                new AssessmentTask
                {
                    title       = "Run Both Traversals",
                    instruction =
                        "Run BOTH BFS and DFS\n" +
                        "to compare them side by side.\n\n" +
                        "Key differences:\n" +
                        "  BFS: Queue, level-by-level\n" +
                        "  DFS: Stack/Recursion, depth-first\n\n" +
                        "Both visit ALL vertices — O(V+E)\n" +
                        "but in DIFFERENT orders!\n\n" +
                        "Which one lit up nodes near\n" +
                        "the start first?",
                    maxPoints       = 40,
                    completionCheck = () => t_bfsDone && t_dfsDone
                },
            };

            // =================================================================
            // L5 — Usage & Big-O Summary
            // =================================================================
            case 4: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Activate the Full Complexity Table",
                    instruction =
                        "Perform as many DIFFERENT operations\n" +
                        "as you can to light up the table:\n\n" +
                        "   BFS Traversal    — O(V+E)\n" +
                        "   DFS Traversal    — O(V+E)\n" +
                        "   Dijkstra         — O((V+E)logV)\n" +
                        "   Prim's MST       — O(E logV)\n" +
                        "   Add Node         — O(1)\n" +
                        "   Add Edge         — O(1)\n" +
                        "   Remove Node      — O(E)\n\n" +
                        "More rows lit = more points!",
                    maxPoints       = 100,
                    completionCheck = () =>
                        t_bfsDone    && t_dfsDone      &&
                        t_dijkstraDone && t_mstDone    &&
                        t_nodeAdded  && t_edgeAdded    && t_nodeRemoved
                },
            };

            default:
                return new AssessmentTask[0];
        }
    }

    // ── FEEDBACK BANNER ────────────────────────────────────────────────────────
    void ShowFeedback(bool success, string message)
    {
        if (taskFeedbackBanner == null) return;
        SetActive(taskCardPanel,      true);
        SetActive(taskCollapsedTab,   false);
        SetActive(taskFeedbackBanner, true);
        if (taskFeedbackText != null) taskFeedbackText.text = message;
        Image bg = taskFeedbackBanner.GetComponent<Image>();
        if (bg != null)
            bg.color = success
                ? new Color(0.10f, 0.75f, 0.30f, 0.92f)
                : new Color(0.80f, 0.20f, 0.20f, 0.92f);
    }

    // ── HELPERS ────────────────────────────────────────────────────────────────
    void ResetTaskFlags()
    {
        t_nodeAdded       = false;
        t_edgeAdded       = false;
        t_nodeRemoved     = false;
        t_bfsDone         = false;
        t_dfsDone         = false;
        t_dijkstraDone    = false;
        t_mstDone         = false;
        t_degreeChecked   = false;
        t_pathCheckDone   = false;
        t_pathExistsFound = false;
        t_operationsCount = 0;
    }

    bool IsSceneSpawned()
    {
        return graphController != null && graphController.GetNodeCount() > 0;
    }

    void SetActive(GameObject go, bool state)
    {
        if (go != null) go.SetActive(state);
    }
}