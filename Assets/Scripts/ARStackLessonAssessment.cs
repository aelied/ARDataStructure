using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ARStackLessonAssessment.cs
/// ===========================
/// Mirrors ARArrayLessonAssessment exactly in architecture.
/// Attaches to the same GameObject as ARStackLessonGuide.
///
/// FLOW:
///   1. ARStackLessonGuide finishes its last step →
///      calls ARStackLessonAssessment.BeginAssessment(lessonIndex)
///   2. Assessment panel fades in with instructions
///   3. Student performs AR stack actions; each action is graded
///   4. Results panel shown (score / grade / breakdown)
///   5. Student taps "Return" → OnReturnClicked() back to app
///
/// LESSON ASSESSMENTS:
///   L1 (index 0): Place scene + observe LIFO order of pre-filled stack
///   L2 (index 1): Perform Push → Pop → Peek in sequence
///   L3 (index 2): Push 2 items, then Pop 1 — compare array vs LL behaviour
///   L4 (index 3): Push to simulate function call, Pop to simulate return,
///                 then identify at least one application
///   L5 (index 4): Perform every distinct operation to light up complexity table
///
/// GRADING:
///   Each task is worth a fixed point value.
///   Score% → Grade:  90+=A  75+=B  60+=C  below=Needs Review
///
/// FIXES APPLIED:
///   1. All Notify* methods guard on assessmentActive — guide-phase actions
///      no longer pollute assessment flags.
///   2. RunTaskLoop yields FIRST then checks completion — same-frame
///      notifications are always caught before the check runs.
///   3. Duplicate NotifyPush/Pop calls removed (guard makes them safe anyway,
///      but belt-and-suspenders).
///
/// WIRING (Inspector):
///   • lessonGuide       → drag ARStackLessonGuide component
///   • stackController   → drag InteractiveStackPlates component
///   • assessmentCanvas  → the assessment Canvas
///   • assessmentRoot    → the root GameObject inside that Canvas
///   All text/button fields follow the same naming as ARArrayLessonAssessment.
/// </summary>
public class ARStackLessonAssessment : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────────
    [Header("Sibling Scripts")]
    public ARStackLessonGuide     lessonGuide;
    public InteractiveStackPlates stackController;

    // ── Assessment Canvas ─────────────────────────────────────────────────────
    [Header("Assessment Canvas")]
    public Canvas     assessmentCanvas;
    public GameObject assessmentRoot;

    // Intro panel
    public GameObject      introPanel;
    public TextMeshProUGUI introTitleText;
    public TextMeshProUGUI introBodyText;
    public Button          startAssessmentButton;

    // Live task panel
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

    // Results panel
    public GameObject      resultsPanel;
    public TextMeshProUGUI resultsTitleText;
    public TextMeshProUGUI resultsScoreText;
    public TextMeshProUGUI resultsGradeText;
    public TextMeshProUGUI resultsBreakdownText;
    public TextMeshProUGUI resultsTipText;
    public Button          retryAssessmentButton;
    public Button          returnButton;
    public Image           gradeRingImage;

    // ── Settings ──────────────────────────────────────────────────────────────
    [Header("Settings")]
    public float  taskTimeLimit    = 60f;
    public bool   showTimer        = true;
    public string mainAppSceneName = "MainScene";

    // ── Internal State ────────────────────────────────────────────────────────
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

    private bool  assessmentActive = false;
    private float taskTimer        = 0f;
    private bool  taskComplete     = false;

    // Per-task tracking flags (reset between tasks)
    private bool t_pushed        = false;
    private bool t_popped        = false;
    private bool t_peeked        = false;
    private bool t_multiPopped   = false;
    private bool t_reversed      = false;
    private int  t_pushCount     = 0;
    private int  t_popCount      = 0;
    private int  t_multiPopCount = 0;
    private int  t_operationCount = 0;

    // ─────────────────────────────────────────────────────────────────────────
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
        SetActive(taskCollapsedTab, false);
        SetActive(taskPanel,        true);
        SetActive(taskCardPanel,    true);
    }

    void OnEnable()
    {
        if (!assessmentActive)
            HideAssessment();
    }

    void HideAssessment()
    {
        if (assessmentRoot != null) assessmentRoot.SetActive(false);
        assessmentActive = false;
        StopAllCoroutines();
        Debug.Log("[StackAssessment] HideAssessment — root hidden, state reset");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC ENTRY — called from ARStackLessonGuide when last step is shown
    // ─────────────────────────────────────────────────────────────────────────
    public void BeginAssessment(int lesson)
    {
        Debug.Log($"[StackAssessment] BeginAssessment called for lesson {lesson}");

        assessmentActive = true;

        if (assessmentRoot != null)
            assessmentRoot.SetActive(true);

        // Auto-resolve panels if not wired in Inspector
        if (introPanel   == null) introPanel   = GameObject.Find("AssessmentIntroPanel");
        if (taskPanel    == null) taskPanel    = GameObject.Find("AssessmentTaskPanel");
        if (resultsPanel == null) resultsPanel = GameObject.Find("AssessmentResultsPanel");

        lessonIndex = lesson;
        tasks       = BuildTasks(lesson);
        if (tasks == null || tasks.Length == 0)
        {
            Debug.LogWarning($"[StackAssessment] No tasks for lesson {lesson}");
            return;
        }

        maxPossibleScore = 0;
        foreach (var t in tasks) maxPossibleScore += t.maxPoints;

        totalScore       = 0;
        currentTaskIndex = 0;
        taskScores.Clear();
        taskNames.Clear();
        foreach (var t in tasks) taskNames.Add(t.title);

        Debug.Log($"[StackAssessment] Starting {tasks.Length} tasks, max={maxPossibleScore}pts");
        ShowIntroPanel();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INTRO PANEL
    // ─────────────────────────────────────────────────────────────────────────
    void ShowIntroPanel()
    {
        if (assessmentCanvas != null)
        {
            assessmentCanvas.gameObject.SetActive(true);
            assessmentCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            assessmentCanvas.sortingOrder = 100;

            var scaler = assessmentCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode         = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight  = 0.5f;
            }
        }

        SetActive(introPanel,   true);
        SetActive(taskPanel,    false);
        SetActive(resultsPanel, false);

        if (introPanel == null)
        {
            Debug.LogError("[StackAssessment] introPanel is NULL");
            return;
        }

        var rectTransform = introPanel.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.1f, 0.15f);
            rectTransform.anchorMax = new Vector2(0.9f, 0.85f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        if (introTitleText != null)
            introTitleText.text = $"Lesson {lessonIndex + 1} Assessment";

        string body =
            $"You completed the lesson guide!\n\n" +
            $"Now prove your understanding.\n" +
            $"You have {tasks.Length} task(s) to complete.\n" +
            $"Total points available: {maxPossibleScore}\n\n" +
            "Tap START when ready.";

        if (introBodyText != null)
            introBodyText.text = body;

        if (startAssessmentButton == null)
            startAssessmentButton = introPanel.GetComponentInChildren<Button>(true);

        if (startAssessmentButton != null)
        {
            startAssessmentButton.onClick.RemoveAllListeners();
            startAssessmentButton.onClick.AddListener(OnStartButtonClicked);
            startAssessmentButton.interactable = true;

            CanvasGroup[] groups = startAssessmentButton.GetComponentsInParent<CanvasGroup>();
            foreach (var cg in groups) { cg.interactable = true; cg.blocksRaycasts = true; }

            Debug.Log($"[StackAssessment] Button wired. Active={startAssessmentButton.gameObject.activeInHierarchy}");
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

    // ─────────────────────────────────────────────────────────────────────────
    // TASK FLOW
    // ─────────────────────────────────────────────────────────────────────────
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

        var task = tasks[index];
        if (taskTitleText       != null) taskTitleText.text       = task.title;
        if (taskInstructionText != null) taskInstructionText.text = task.instruction;
        if (taskProgressText    != null) taskProgressText.text    = $"Task {index + 1} / {tasks.Length}";
        if (taskProgressBarFill != null) taskProgressBarFill.fillAmount = (float)(index + 1) / tasks.Length;
        if (taskTimerText       != null) taskTimerText.gameObject.SetActive(showTimer && taskTimeLimit > 0);

        StartCoroutine(RunTaskLoop(index));
    }

    // FIX 2: yield FIRST so same-frame Notify* calls are always visible
    //        before the completion check evaluates.
    IEnumerator RunTaskLoop(int index)
    {
        var task = tasks[index];

        while (!taskComplete)
        {
            // Yield first — ensures any Notify* call that fired this frame
            // has already set its flag before we evaluate completionCheck.
            yield return null;

            // Completion check
            if (task.completionCheck != null && task.completionCheck())
            {
                taskComplete = true;
                break;
            }

            // Timer
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

    // ─────────────────────────────────────────────────────────────────────────
    // OPERATION DETECTION — relay calls from InteractiveStackPlates
    // FIX 1: All methods guard on assessmentActive so guide-phase actions
    //        never pollute assessment flags.
    // ─────────────────────────────────────────────────────────────────────────
    public void NotifyPush()
    {
        if (!assessmentActive) return;
        t_pushed = true;
        t_pushCount++;
        t_operationCount++;
    }

    public void NotifyPop()
    {
        if (!assessmentActive) return;
        t_popped = true;
        t_popCount++;
        t_operationCount++;
    }

    public void NotifyPeek()
    {
        if (!assessmentActive) return;
        t_peeked = true;
        t_operationCount++;
    }

    public void NotifyMultiPop(int count)
    {
        if (!assessmentActive) return;
        t_multiPopped    = true;
        t_multiPopCount += count;
        t_operationCount++;
    }

    public void NotifyReverse()
    {
        if (!assessmentActive) return;
        t_reversed = true;
        t_operationCount++;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GRADING PER TASK
    // ─────────────────────────────────────────────────────────────────────────
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
                    case 0: // Place the scene
                        return IsSceneSpawned() ? task.maxPoints : 0;

                    case 1: // Observe LIFO — pop at least one item
                        return t_popped ? task.maxPoints
                             : t_pushed ? Mathf.RoundToInt(task.maxPoints * 0.5f) : 0;

                    case 2: // Demonstrate LIFO — push then pop
                        bool lifo = t_pushed && t_popped;
                        return lifo ? task.maxPoints : t_pushed || t_popped
                            ? Mathf.RoundToInt(task.maxPoints * 0.5f) : 0;
                }
                break;

            // ── L2: Operations ───────────────────────────────────────────────
            case 1:
                switch (taskIndex)
                {
                    case 0: // Push
                        return t_pushed ? task.maxPoints : 0;

                    case 1: // Pop
                        return t_popped ? task.maxPoints : 0;

                    case 2: // Peek
                        return t_peeked ? task.maxPoints : 0;

                    case 3: // All three
                        int ops = (t_pushed ? 1 : 0) + (t_popped ? 1 : 0) + (t_peeked ? 1 : 0);
                        return Mathf.RoundToInt(task.maxPoints * (ops / 3f));
                }
                break;

            // ── L3: Implementation ───────────────────────────────────────────
            case 2:
                switch (taskIndex)
                {
                    case 0: // Push twice (simulating array push)
                        int pushed2 = Mathf.Clamp(t_pushCount, 0, 2);
                        return Mathf.RoundToInt(task.maxPoints * (pushed2 / 2f));

                    case 1: // Pop once (simulating array pop)
                        return t_popped ? task.maxPoints : 0;

                    case 2: // Push + Pop (both implementations tested)
                        bool both = t_pushed && t_popped;
                        return both ? task.maxPoints
                             : (t_pushed || t_popped)
                                 ? Mathf.RoundToInt(task.maxPoints * 0.5f) : 0;
                }
                break;

            // ── L4: Applications ─────────────────────────────────────────────
            case 3:
                switch (taskIndex)
                {
                    case 0: // Push to simulate function call
                        return t_pushed ? task.maxPoints : 0;

                    case 1: // Pop to simulate function return
                        return t_popped ? task.maxPoints : 0;

                    case 2: // Peek to inspect call stack
                        return t_peeked ? task.maxPoints : 0;

                    case 3: // All three operations (full simulation)
                        int appOps = (t_pushed ? 1 : 0) + (t_popped ? 1 : 0) + (t_peeked ? 1 : 0);
                        return Mathf.RoundToInt(task.maxPoints * (appOps / 3f));
                }
                break;

            // ── L5: Complexity Summary ────────────────────────────────────────
            case 4:
                int distinctOps =
                    (t_pushed      ? 1 : 0) +
                    (t_popped      ? 1 : 0) +
                    (t_peeked      ? 1 : 0) +
                    (t_multiPopped ? 1 : 0) +
                    (t_reversed    ? 1 : 0);
                return Mathf.RoundToInt(task.maxPoints * (distinctOps / 5f));
        }

        return 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RESULTS PANEL
    // ─────────────────────────────────────────────────────────────────────────
    void ShowResults()
    {
        assessmentActive = false;
        SetActive(taskPanel,    false);
        SetActive(resultsPanel, true);

        float  pct = maxPossibleScore > 0 ? (float)totalScore / maxPossibleScore : 0f;
        string grade;
        Color  gradeColor;

        if      (pct >= 0.90f) { grade = "A";            gradeColor = new Color(0.2f, 0.9f, 0.3f); }
        else if (pct >= 0.75f) { grade = "B";            gradeColor = new Color(0.4f, 0.7f, 1.0f); }
        else if (pct >= 0.60f) { grade = "C";            gradeColor = new Color(1.0f, 0.8f, 0.2f); }
        else                   { grade = "Needs Review"; gradeColor = new Color(1.0f, 0.3f, 0.3f); }

        if (resultsTitleText != null)
            resultsTitleText.text = $"Lesson {lessonIndex + 1} Complete!";
        if (resultsScoreText != null)
            resultsScoreText.text = $"{totalScore} / {maxPossibleScore}  ({Mathf.RoundToInt(pct * 100)}%)";
        if (resultsGradeText != null)
        {
            resultsGradeText.text  = grade;
            resultsGradeText.color = gradeColor;
        }
        if (gradeRingImage != null)
        {
            gradeRingImage.fillAmount = pct;
            gradeRingImage.color      = gradeColor;
        }

        string breakdown = "Task Breakdown:\n";
        for (int i = 0; i < taskScores.Count && i < taskNames.Count; i++)
        {
            int    max  = i < tasks.Length ? tasks[i].maxPoints : 0;
            string tick = taskScores[i] >= Mathf.CeilToInt(max * 0.6f) ? "" : "";
            breakdown += $"{tick} {taskNames[i]}:  {taskScores[i]}/{max}\n";
        }
        if (resultsBreakdownText != null) resultsBreakdownText.text = breakdown;

        string tip = BuildTip(lessonIndex, pct);
        if (resultsTipText != null) resultsTipText.text = tip;

        PlayerPrefs.SetInt($"AR_Assessment_Stack_L{lessonIndex}_Score", totalScore);
        PlayerPrefs.SetInt($"AR_Assessment_Stack_L{lessonIndex}_Max",   maxPossibleScore);
        PlayerPrefs.SetString($"AR_Assessment_Stack_L{lessonIndex}_Grade", grade);
        PlayerPrefs.Save();
    }

    string BuildTip(int lesson, float pct)
    {
        if (pct >= 0.9f) return "Excellent work! You have a strong grasp of this lesson.";
        switch (lesson)
        {
            case 0: return "Review: stacks follow LIFO — last in is always first out.";
            case 1: return "Review: Push, Pop, and Peek all run in O(1) constant time.";
            case 2: return "Review: array stacks are fixed-size; linked list stacks grow dynamically.";
            case 3: return "Review: stacks manage function calls, recursion, and expression evaluation.";
            case 4: return "Review: core ops are O(1); Multi-Pop and Reverse are O(n).";
            default: return "Keep practising in AR to reinforce your understanding!";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RETRY
    // ─────────────────────────────────────────────────────────────────────────
    void RetryAssessment()
    {
        SetActive(resultsPanel, false);
        totalScore       = 0;
        currentTaskIndex = 0;
        taskScores.Clear();
        OnStartButtonClicked();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RETURN
    // ─────────────────────────────────────────────────────────────────────────
    void OnReturnClicked()
    {
        HideAssessment();
        PlayerPrefs.SetInt(ARReturnHandler.AR_JUST_RETURNED_KEY, 1);
        PlayerPrefs.SetString(ARReturnHandler.AR_TOPIC_NAME_KEY,
                              PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "stacks"));
        PlayerPrefs.Save();
        SceneManager.LoadScene(PlayerPrefs.GetString("AR_MainScene", mainAppSceneName));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TASK DEFINITIONS
    // ─────────────────────────────────────────────────────────────────────────
    AssessmentTask[] BuildTasks(int lesson)
    {
        switch (lesson)
        {
            // =================================================================
            // L1 — Introduction to Stack
            // =================================================================
            case 0: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Place the Stack Scene",
                    instruction =
                        "Point your camera at a flat surface\n" +
                        "and tap to place the stack scene.\n\n" +
                        "A stack follows LIFO:\n" +
                        "Last In — First Out.\n\n" +
                        "The pre-filled items show the order\n" +
                        "in which they were pushed.",
                    maxPoints       = 30,
                    completionCheck = () => IsSceneSpawned()
                },
                new AssessmentTask
                {
                    title       = "Observe LIFO — Pop the Top",
                    instruction =
                        "The stack has pre-filled items.\n\n" +
                        "Tap POP to remove the top item.\n\n" +
                        "Notice: you can ONLY remove\n" +
                        "from the TOP — never the middle!\n\n" +
                        "This is LIFO in action.",
                    maxPoints       = 30,
                    completionCheck = () => t_popped
                },
                new AssessmentTask
                {
                    title       = "Demonstrate LIFO Order",
                    instruction =
                        "Now PUSH a new item onto the stack,\n" +
                        "then POP it immediately.\n\n" +
                        "The item you just pushed should be\n" +
                        "the first one to come back out.\n\n" +
                        "This proves LIFO:\n" +
                        "Last In = First Out!",
                    maxPoints       = 40,
                    completionCheck = () => t_pushed && t_popped
                },
            };

            // =================================================================
            // L2 — Stack Operations
            // =================================================================
            case 1: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "PUSH an Item",
                    instruction =
                        "Tap the PUSH button and move\n" +
                        "the item to the TOP of the stack.\n\n" +
                        "Steps for push:\n" +
                        "1. Check stack is not full\n" +
                        "2. Increment top pointer\n" +
                        "3. Place element at top\n\n" +
                        "Time Complexity: O(1)",
                    maxPoints       = 25,
                    completionCheck = () => t_pushed
                },
                new AssessmentTask
                {
                    title       = "POP the Top Item",
                    instruction =
                        "Tap the POP button and move\n" +
                        "the top item away from the stack.\n\n" +
                        "Steps for pop:\n" +
                        "1. Check stack is not empty\n" +
                        "2. Return top element\n" +
                        "3. Decrement top pointer\n\n" +
                        "Time Complexity: O(1)",
                    maxPoints       = 25,
                    completionCheck = () => t_popped
                },
                new AssessmentTask
                {
                    title       = "PEEK at the Top",
                    instruction =
                        "Tap the PEEK button.\n\n" +
                        "The top item will glow cyan.\n" +
                        "Notice: the stack is UNCHANGED!\n\n" +
                        "Peek is read-only — it views\n" +
                        "the top without removing it.\n\n" +
                        "Time Complexity: O(1)",
                    maxPoints       = 25,
                    completionCheck = () => t_peeked
                },
                new AssessmentTask
                {
                    title       = "Complete All Operations",
                    instruction =
                        "Now perform ALL three operations:\n\n" +
                        "   PUSH  — add to top\n" +
                        "   POP   — remove from top\n" +
                        "   PEEK  — view top (no remove)\n\n" +
                        "All three run in O(1) time!\n\n" +
                        "Can you light up all three?",
                    maxPoints       = 25,
                    completionCheck = () => t_pushed && t_popped && t_peeked
                },
            };

            // =================================================================
            // L3 — Stack Implementation
            // =================================================================
            case 2: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Array Push — Simulate stack[++top]",
                    instruction =
                        "In an array-based stack:\n\n" +
                        "  push:  stack[++top] = value\n\n" +
                        "Push at least 2 items to simulate\n" +
                        "filling array slots at indices\n" +
                        "0, 1, 2... as top increments.\n\n" +
                        "Time: O(1)  |  Size: FIXED",
                    maxPoints       = 30,
                    completionCheck = () => t_pushCount >= 2
                },
                new AssessmentTask
                {
                    title       = "Array Pop — Simulate stack[top--]",
                    instruction =
                        "In an array-based stack:\n\n" +
                        "  pop:  return stack[top--]\n\n" +
                        "Tap POP to remove the top item.\n\n" +
                        "The top pointer decrements.\n" +
                        "The slot is not truly erased —\n" +
                        "just marked as unused.\n\n" +
                        "Time: O(1)",
                    maxPoints       = 30,
                    completionCheck = () => t_popped
                },
                new AssessmentTask
                {
                    title       = "Both Implementations Compared",
                    instruction =
                        "You just experienced array-based ops.\n\n" +
                        "In a Linked List stack:\n" +
                        "  push:  new_node.next = top\n" +
                        "         top = new_node\n" +
                        "  pop:   top = top.next\n\n" +
                        "To demonstrate: PUSH one more item,\n" +
                        "then POP it.\n\n" +
                        "Both give O(1) — but LL\n" +
                        "grows dynamically, arrays don't!",
                    maxPoints       = 40,
                    completionCheck = () => t_pushed && t_popped
                },
            };

            // =================================================================
            // L4 — Applications of Stacks
            // =================================================================
            case 3: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Simulate a Function Call",
                    instruction =
                        "When a function is called,\n" +
                        "its state is PUSHED onto\n" +
                        "the call stack.\n\n" +
                        "Tap PUSH to simulate calling\n" +
                        "a function — each item on the\n" +
                        "stack represents an active function.",
                    maxPoints       = 25,
                    completionCheck = () => t_pushed
                },
                new AssessmentTask
                {
                    title       = "Simulate a Function Return",
                    instruction =
                        "When a function RETURNS,\n" +
                        "its state is POPPED from the\n" +
                        "call stack.\n\n" +
                        "Tap POP to simulate a function\n" +
                        "returning — control goes back\n" +
                        "to the previous function!\n\n" +
                        "This is the call stack in action.",
                    maxPoints       = 25,
                    completionCheck = () => t_popped
                },
                new AssessmentTask
                {
                    title       = "Inspect the Call Stack",
                    instruction =
                        "In a debugger, you can PEEK at\n" +
                        "the call stack to see which\n" +
                        "function is currently running.\n\n" +
                        "Tap PEEK to simulate inspecting\n" +
                        "the top of the call stack\n" +
                        "without disrupting execution.",
                    maxPoints       = 25,
                    completionCheck = () => t_peeked
                },
                new AssessmentTask
                {
                    title       = "Full Application Simulation",
                    instruction =
                        "Simulate the FULL function\n" +
                        "call lifecycle:\n\n" +
                        "  1. PUSH — function called\n" +
                        "  2. PEEK — inspect stack frame\n" +
                        "  3. POP  — function returns\n\n" +
                        "This is exactly how the CPU\n" +
                        "manages your program!",
                    maxPoints       = 25,
                    completionCheck = () => t_pushed && t_popped && t_peeked
                },
            };

            // =================================================================
            // L5 — Big-O Complexity Summary
            // =================================================================
            case 4: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Light Up the Complexity Table",
                    instruction =
                        "Perform as many DIFFERENT operations\n" +
                        "as you can to light up each row:\n\n" +
                        "   PUSH           — O(1)\n" +
                        "   POP            — O(1)\n" +
                        "   PEEK           — O(1)\n" +
                        "   MULTI-POP (n)  — O(n)\n" +
                        "   REVERSE        — O(n)\n\n" +
                        "Use INTERMEDIATE mode for\n" +
                        "Multi-Pop and Reverse!\n\n" +
                        "More rows lit = more points!",
                    maxPoints       = 100,
                    completionCheck = () =>
                        t_pushed && t_popped && t_peeked &&
                        t_multiPopped && t_reversed
                },
            };

            default:
                return new AssessmentTask[0];
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FEEDBACK BANNER
    // ─────────────────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────
    void ResetTaskFlags()
    {
        t_pushed         = false;
        t_popped         = false;
        t_peeked         = false;
        t_multiPopped    = false;
        t_reversed       = false;
        t_pushCount      = 0;
        t_popCount       = 0;
        t_multiPopCount  = 0;
        t_operationCount = 0;
    }

    int GetCurrentStackCount()
    {
        if (stackController == null) return 0;
        return stackController.CurrentStackCount;
    }

    bool IsSceneSpawned()
    {
        return stackController != null && stackController.ParkingLot != null;
    }

    void SetActive(GameObject go, bool state)
    {
        if (go != null) go.SetActive(state);
    }
}