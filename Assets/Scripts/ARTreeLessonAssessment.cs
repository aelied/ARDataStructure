using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ARTreeLessonAssessment.cs
/// ==========================
/// Mirrors ARGraphLessonAssessment — same flow, same panel structure.
///
/// LESSON ASSESSMENTS:
///   L1 (0): Introduction — place scene, add a child node
///   L2 (1): Terminologies — add child, identify leaf vs internal via height
///   L3 (2): Types of Trees — add nodes, observe left/right BST structure
///   L4 (3): Traversal — run all 3 traversals (In/Pre/Post)
///   L5 (4): Implementation — search, delete, height, full operations
/// </summary>
public class ARTreeLessonAssessment : MonoBehaviour
{
    // ── References ─────────────────────────────────────────────────────────────
    [Header("Sibling Scripts")]
    public ARTreeLessonGuide     lessonGuide;
    public InteractiveFamilyTree treeController;

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
    public float  taskTimeLimit    = 90f;
    public bool   showTimer        = true;
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

    // Per-task flags
    private bool t_childAdded      = false;
    private bool t_inOrderDone     = false;
    private bool t_preOrderDone    = false;
    private bool t_postOrderDone   = false;
    private bool t_searchDone      = false;
    private bool t_searchFound     = false;
    private bool t_deleteDone      = false;
    private bool t_heightDone      = false;
    private int  t_heightValue     = 0;
    private int  t_operationsCount = 0;

    // ──────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        HideAssessment();
        retryAssessmentButton?.onClick.AddListener(RetryAssessment);
        returnButton?.onClick.AddListener(OnReturnClicked);

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

    void OnEnable() { if (!assessmentActive) HideAssessment(); }

    void HideAssessment()
    {
        if (assessmentRoot != null) assessmentRoot.SetActive(false);
        assessmentActive = false;
        StopAllCoroutines();
    }

    // ── PUBLIC ENTRY ───────────────────────────────────────────────────────────
    public void BeginAssessment(int lesson)
    {
        Debug.Log($"[TreeAssessment] BeginAssessment L{lesson}");
        assessmentActive = true;
        if (assessmentRoot != null) assessmentRoot.SetActive(true);

        if (introPanel   == null) introPanel   = GameObject.Find("AssessmentIntroPanel");
        if (taskPanel    == null) taskPanel    = GameObject.Find("AssessmentTaskPanel");
        if (resultsPanel == null) resultsPanel = GameObject.Find("AssessmentResultsPanel");

        lessonIndex = lesson;
        tasks       = BuildTasks(lesson);
        if (tasks == null || tasks.Length == 0) return;

        maxPossibleScore = 0;
        foreach (var t in tasks) maxPossibleScore += t.maxPoints;

        totalScore = 0; currentTaskIndex = 0;
        taskScores.Clear(); taskNames.Clear();
        foreach (var t in tasks) taskNames.Add(t.title);

        ShowIntroPanel();
    }

    // ── INTRO ──────────────────────────────────────────────────────────────────
    void ShowIntroPanel()
    {
        if (assessmentCanvas != null)
        {
            assessmentCanvas.gameObject.SetActive(true);
            assessmentCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
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
            CanvasGroup[] cgs = startAssessmentButton.GetComponentsInParent<CanvasGroup>();
            foreach (var cg in cgs) { cg.interactable = true; cg.blocksRaycasts = true; }
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

        if (treeController != null)
        {
            treeController.mainButtonPanel?.SetActive(true);
            treeController.beginnerButtonPanel?.SetActive(true);
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
            { taskComplete = true; break; }

            if (taskTimeLimit > 0)
            {
                taskTimer -= Time.deltaTime;
                if (taskTimerText != null) taskTimerText.text = $"Time: {Mathf.CeilToInt(taskTimer)}s";
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

        bool passed = earned >= Mathf.CeilToInt(tasks[index].maxPoints * 0.6f);
        ShowFeedback(passed,
            passed ? $"+{earned} pts — Well done!" : $"! +{earned}/{tasks[index].maxPoints} pts");

        yield return new WaitForSeconds(2.0f);
        SetActive(taskFeedbackBanner, false);
        currentTaskIndex++;
        if (currentTaskIndex < tasks.Length) StartTask(currentTaskIndex);
        else ShowResults();
    }

    // ── NOTIFICATION HOOKS ─────────────────────────────────────────────────────
    public void NotifyTraversal(string type)
    {
        switch (type)
        {
            case "inorder":   t_inOrderDone   = true; break;
            case "preorder":  t_preOrderDone  = true; break;
            case "postorder": t_postOrderDone = true; break;
        }
        t_operationsCount++;
    }

    public void NotifySearch(bool found)
    {
        t_searchDone = true;
        if (found) t_searchFound = true;
        t_operationsCount++;
    }

    public void NotifyDelete()
    {
        t_deleteDone = true;
        t_operationsCount++;
    }

    public void NotifyHeight(int height)
    {
        t_heightDone  = true;
        t_heightValue = height;
        t_operationsCount++;
    }

    public void NotifyChildAdded()
    {
        t_childAdded = true;
        t_operationsCount++;
    }

    // ── GRADING ────────────────────────────────────────────────────────────────
    int GradeTask(int taskIndex)
    {
        if (tasks == null || taskIndex >= tasks.Length) return 0;
        var task = tasks[taskIndex];

        switch (lessonIndex)
        {
            // ── L1 ────────────────────────────────────────────────────────────
            case 0:
                switch (taskIndex)
                {
                    case 0: return IsSceneSpawned() ? task.maxPoints : 0;
                    case 1: return t_childAdded ? task.maxPoints : 0;
                }
                break;

            // ── L2 ────────────────────────────────────────────────────────────
            case 1:
                switch (taskIndex)
                {
                    case 0: return IsSceneSpawned() ? task.maxPoints : 0;
                    case 1: return t_childAdded ? task.maxPoints : 0;
                    case 2: return t_heightDone ? task.maxPoints : 0;
                }
                break;

            // ── L3 ────────────────────────────────────────────────────────────
            case 2:
                switch (taskIndex)
                {
                    case 0: return IsSceneSpawned() ? task.maxPoints : 0;
                    case 1: return t_childAdded ? task.maxPoints : 0;
                    case 2:
                        int nodeCount = treeController != null ? treeController.GetNodeCount() : 0;
                        return nodeCount >= 3 ? task.maxPoints :
                               nodeCount >= 2 ? Mathf.RoundToInt(task.maxPoints * 0.6f) : 0;
                }
                break;

            // ── L4 ────────────────────────────────────────────────────────────
            case 3:
                switch (taskIndex)
                {
                    case 0: return t_inOrderDone   ? task.maxPoints : 0;
                    case 1: return t_preOrderDone  ? task.maxPoints : 0;
                    case 2: return t_postOrderDone ? task.maxPoints : 0;
                    case 3:
                        int done = (t_inOrderDone ? 1 : 0) + (t_preOrderDone ? 1 : 0) + (t_postOrderDone ? 1 : 0);
                        return Mathf.RoundToInt(task.maxPoints * (done / 3f));
                }
                break;

            // ── L5 ────────────────────────────────────────────────────────────
            case 4:
                int ops = (t_searchDone    ? 1 : 0) +
                          (t_deleteDone    ? 1 : 0) +
                          (t_heightDone    ? 1 : 0) +
                          (t_inOrderDone   ? 1 : 0) +
                          (t_preOrderDone  ? 1 : 0) +
                          (t_postOrderDone ? 1 : 0) +
                          (t_childAdded    ? 1 : 0);
                return Mathf.RoundToInt(task.maxPoints * (ops / 7f));
        }
        return 0;
    }

    // ── RESULTS ────────────────────────────────────────────────────────────────
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
            int max   = i < tasks.Length ? tasks[i].maxPoints : 0;
            bool pass = taskScores[i] >= Mathf.CeilToInt(max * 0.6f);
            breakdown += $"{(pass ? "Pass" : "Fail")} {taskNames[i]}:  {taskScores[i]}/{max}\n";
        }
        if (resultsBreakdownText != null) resultsBreakdownText.text = breakdown;
        if (resultsTipText       != null) resultsTipText.text       = BuildTip(lessonIndex, pct);

        PlayerPrefs.SetInt   ($"AR_Assessment_Tree_L{lessonIndex}_Score", totalScore);
        PlayerPrefs.SetInt   ($"AR_Assessment_Tree_L{lessonIndex}_Max",   maxPossibleScore);
        PlayerPrefs.SetString($"AR_Assessment_Tree_L{lessonIndex}_Grade", grade);
        PlayerPrefs.Save();
    }

    string BuildTip(int lesson, float pct)
    {
        if (pct >= 0.9f) return "Excellent! You have a strong grasp of Tree data structures.";
        switch (lesson)
        {
            case 0: return "Review: A tree stores hierarchical data using nodes, edges, and a single root. Unlike arrays it is non-linear and organized in levels.";
            case 1: return "Review: Root = topmost node. Leaf = no children. Depth = edges from root. Height = longest root-to-leaf path.";
            case 2: return "Review: BST rule — left subtree < root < right subtree. This gives O(log n) search but degrades to O(n) when unbalanced.";
            case 3: return "Review: In-Order (L->Root->R) gives sorted output. Pre-Order (Root->L->R) for copying. Post-Order (L->R->Root) for deletion.";
            case 4: return "Review: BST Insert/Search/Delete are O(log n) average, O(n) worst case (skewed tree). All traversals are O(n).";
            default: return "Keep practising in AR to reinforce your understanding of Tree data structures!";
        }
    }

    // ── RETRY ──────────────────────────────────────────────────────────────────
    void RetryAssessment()
    {
        SetActive(resultsPanel, false);
        totalScore = 0; currentTaskIndex = 0; taskScores.Clear();
        OnStartButtonClicked();
    }

    // ── RETURN ─────────────────────────────────────────────────────────────────
    void OnReturnClicked()
    {
        HideAssessment();
        PlayerPrefs.SetInt(ARReturnHandler.AR_JUST_RETURNED_KEY, 1);
        PlayerPrefs.SetString(ARReturnHandler.AR_TOPIC_NAME_KEY,
                              PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "trees"));
        PlayerPrefs.Save();
        SceneManager.LoadScene(PlayerPrefs.GetString("AR_MainScene", mainAppSceneName));
    }

    // ── TASK DEFINITIONS ───────────────────────────────────────────────────────
    AssessmentTask[] BuildTasks(int lesson)
    {
        switch (lesson)
        {
            // =================================================================
            // L1 — Introduction to Trees
            // =================================================================
            case 0: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Place the Tree Scene",
                    instruction =
                        "Point your camera at a flat surface\n" +
                        "and tap to place the tree scene.\n\n" +
                        "A tree has:\n" +
                        "  NODES  — the data elements\n" +
                        "  EDGES  — connections\n" +
                        "  ROOT   — the topmost node\n\n" +
                        "You'll see the root appear!",
                    maxPoints       = 30,
                    completionCheck = () => IsSceneSpawned()
                },
                new AssessmentTask
                {
                    title       = "Add a Child Node",
                    instruction =
                        "Use ADD CHILD to add a node\n" +
                        "to the existing tree.\n\n" +
                        "This creates a PARENT-CHILD relationship.\n\n" +
                        "Remember:\n" +
                        "  The root is the PARENT.\n" +
                        "  Your new node is the CHILD.\n\n" +
                        "Snap to a LEFT or RIGHT indicator!",
                    maxPoints       = 70,
                    completionCheck = () => t_childAdded
                },
            };

            // =================================================================
            // L2 — Tree Terminologies
            // =================================================================
            case 1: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Place the Scene",
                    instruction =
                        "Tap a flat surface to place the tree.\n\n" +
                        "Observe the placed scene:\n" +
                        "  ROOT = topmost node\n" +
                        "  EDGES = the connecting branches\n\n" +
                        "Which node is the leaf\n" +
                        "(has no children)?",
                    maxPoints       = 20,
                    completionCheck = () => IsSceneSpawned()
                },
                new AssessmentTask
                {
                    title       = "Expand the Tree",
                    instruction =
                        "Add a child node to the tree.\n\n" +
                        "When you add a node:\n" +
                        "  - The existing node becomes a PARENT\n" +
                        "  - Your new node is a CHILD\n" +
                        "  - Your new node starts as a LEAF\n\n" +
                        "Think: what is the DEPTH of your\n" +
                        "new node from the root?",
                    maxPoints       = 30,
                    completionCheck = () => t_childAdded
                },
                new AssessmentTask
                {
                    title       = "Measure the Tree Height",
                    instruction =
                        "Tap TREE HEIGHT to calculate\n" +
                        "the height of your tree.\n\n" +
                        "HEIGHT = longest path from\n" +
                        "root to any leaf.\n\n" +
                        "After adding one child:\n" +
                        "  Root -> Child = 1 edge\n" +
                        "  So height = 1!\n\n" +
                        "Use INTERMEDIATE mode for HEIGHT button.",
                    maxPoints       = 50,
                    completionCheck = () => t_heightDone
                },
            };

            // =================================================================
            // L3 — Types of Trees
            // =================================================================
            case 2: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Place the Binary Tree Scene",
                    instruction =
                        "Tap a surface to place the tree.\n\n" +
                        "The AR scene is a BINARY TREE:\n" +
                        "  Each node has at most 2 children.\n\n" +
                        "Look for the LEFT (blue) and\n" +
                        "RIGHT (orange) indicators!\n" +
                        "These show where children go.",
                    maxPoints       = 15,
                    completionCheck = () => IsSceneSpawned()
                },
                new AssessmentTask
                {
                    title       = "Add a Left Child",
                    instruction =
                        "Add a child on the LEFT side.\n\n" +
                        "In a BST, the LEFT child has a\n" +
                        "SMALLER value than the parent.\n\n" +
                        "Snap your new node to the\n" +
                        "BLUE (LEFT) indicator!\n\n" +
                        "This demonstrates the binary\n" +
                        "tree LEFT child property.",
                    maxPoints       = 35,
                    completionCheck = () => t_childAdded
                },
                new AssessmentTask
                {
                    title       = "Build a 3-Node Tree",
                    instruction =
                        "Add at least 2 total children\n" +
                        "so you have 3 or more nodes total.\n\n" +
                        "Try to add one LEFT and one RIGHT.\n\n" +
                        "This demonstrates the BST property:\n" +
                        "  LEFT subtree  < Root\n" +
                        "  RIGHT subtree > Root\n\n" +
                        "A balanced 3-node tree has height 1!",
                    maxPoints       = 50,
                    completionCheck = () =>
                        treeController != null && treeController.GetNodeCount() >= 3
                },
            };

            // =================================================================
            // L4 — Tree Traversal
            // =================================================================
            case 3: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Run In-Order Traversal",
                    instruction =
                        "Tap IN-ORDER to traverse the tree.\n\n" +
                        "In-Order visits: Left -> Root -> Right\n\n" +
                        "In a BST, this produces\n" +
                        "SORTED output!\n\n" +
                        "Watch the yellow highlights\n" +
                        "show the visit order.",
                    maxPoints       = 25,
                    completionCheck = () => t_inOrderDone
                },
                new AssessmentTask
                {
                    title       = "Run Pre-Order Traversal",
                    instruction =
                        "Tap PRE-ORDER to traverse the tree.\n\n" +
                        "Pre-Order visits: Root -> Left -> Right\n\n" +
                        "Parent is visited BEFORE children.\n\n" +
                        "Used for: copying a tree,\n" +
                        "serializing a tree structure.",
                    maxPoints       = 25,
                    completionCheck = () => t_preOrderDone
                },
                new AssessmentTask
                {
                    title       = "Run Post-Order Traversal",
                    instruction =
                        "Tap POST-ORDER to traverse the tree.\n\n" +
                        "Post-Order visits: Left -> Right -> Root\n\n" +
                        "Children are visited BEFORE the parent.\n\n" +
                        "Used for: deleting a tree,\n" +
                        "computing folder sizes.",
                    maxPoints       = 25,
                    completionCheck = () => t_postOrderDone
                },
                new AssessmentTask
                {
                    title       = "Complete All 3 Traversals",
                    instruction =
                        "Run ALL THREE traversals:\n" +
                        "  - In-Order\n" +
                        "  - Pre-Order\n" +
                        "  - Post-Order\n\n" +
                        "Compare the visit orders!\n" +
                        "Same nodes — different sequences.\n\n" +
                        "All run in O(n) time.",
                    maxPoints       = 25,
                    completionCheck = () => t_inOrderDone && t_preOrderDone && t_postOrderDone
                },
            };

            // =================================================================
            // L5 — Implementation & Usage
            // =================================================================
            case 4: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Activate the Full Complexity Table",
                    instruction =
                        "Perform as many operations as possible:\n\n" +
                        "   Add a child node    — O(log n)\n" +
                        "   In-Order traversal  — O(n)\n" +
                        "   Pre-Order traversal — O(n)\n" +
                        "   Post-Order traversal— O(n)\n" +
                        "   Search for a node   — O(n)\n" +
                        "   Delete a node       — O(n)\n" +
                        "   Compute Tree Height — O(n)\n\n" +
                        "More operations = more points!",
                    maxPoints       = 100,
                    completionCheck = () =>
                        t_childAdded    && t_inOrderDone  && t_preOrderDone &&
                        t_postOrderDone && t_searchDone   && t_deleteDone   && t_heightDone
                },
            };

            default: return new AssessmentTask[0];
        }
    }

    // ── FEEDBACK ───────────────────────────────────────────────────────────────
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
        t_childAdded      = false;
        t_inOrderDone     = false;
        t_preOrderDone    = false;
        t_postOrderDone   = false;
        t_searchDone      = false;
        t_searchFound     = false;
        t_deleteDone      = false;
        t_heightDone      = false;
        t_heightValue     = 0;
        t_operationsCount = 0;
    }

    bool IsSceneSpawned() =>
        treeController != null && treeController.GetNodeCount() > 0;

    void SetActive(GameObject go, bool state)
    { if (go != null) go.SetActive(state); }
}