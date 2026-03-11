using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ARQueueLessonAssessment.cs
/// ===========================
/// Mirrors ARStackLessonAssessment exactly in architecture.
///
/// LESSON ASSESSMENTS:
///   L1 (index 0): Place scene + observe FIFO order of pre-filled queue
///   L2 (index 1): Perform Enqueue → Dequeue → Peek in sequence
///   L3 (index 2): Use Priority Enqueue; compare with simple enqueue
///   L4 (index 3): Enqueue 2 items (array sim), Dequeue 1 (LL sim), both combined
///   L5 (index 4): Perform every distinct operation to light up complexity table
///
/// GRADING:
///   Score% → Grade:  90+=A  75+=B  60+=C  below=Needs Review
/// </summary>
public class ARQueueLessonAssessment : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────────
    [Header("Sibling Scripts")]
    public ARQueueLessonGuide     lessonGuide;
    public InteractiveCoffeeQueue queueController;

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

    private bool  assessmentActive  = false;
    private float taskTimer         = 0f;
    private bool  taskComplete      = false;

    // Per-task flags
    private bool t_enqueued         = false;
    private bool t_dequeued         = false;
    private bool t_peeked           = false;
    private bool t_priorityEnqueued = false;
    private int  t_enqueueCount     = 0;
    private int  t_dequeueCount     = 0;
    private int  t_operationCount   = 0;
    private int  t_lastPriority     = -1;
    private int  t_lastInsertIndex  = -1;

    private int lastKnownQueueCount = 0;

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
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC ENTRY
    // ─────────────────────────────────────────────────────────────────────────
    public void BeginAssessment(int lesson)
    {
        Debug.Log($"[QueueAssessment] BeginAssessment lesson {lesson}");
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

        totalScore       = 0;
        currentTaskIndex = 0;
        taskScores.Clear();
        taskNames.Clear();
        foreach (var t in tasks) taskNames.Add(t.title);

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

        var rt = introPanel?.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.1f, 0.15f);
            rt.anchorMax = new Vector2(0.9f, 0.85f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

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

    // ─────────────────────────────────────────────────────────────────────────
    // TASK FLOW
    // ─────────────────────────────────────────────────────────────────────────
    void StartTask(int index)
    {
        if (index >= tasks.Length) { ShowResults(); return; }

        SetActive(taskCardPanel,    true);
        SetActive(taskCollapsedTab, false);

        taskComplete        = false;
        taskTimer           = taskTimeLimit > 0 ? taskTimeLimit : float.MaxValue;
        lastKnownQueueCount = GetCurrentQueueCount();
        ResetTaskFlags();

        SetActive(taskPanel,          true);
        SetActive(taskFeedbackBanner, false);

        var task = tasks[index];
        if (taskTitleText       != null) taskTitleText.text       = task.title;
        if (taskInstructionText != null) taskInstructionText.text = task.instruction;
        if (taskProgressText    != null) taskProgressText.text    = $"Task {index + 1} / {tasks.Length}";
        if (taskProgressBarFill != null) taskProgressBarFill.fillAmount = (float)(index + 1) / tasks.Length;
        if (taskTimerText != null)       taskTimerText.gameObject.SetActive(showTimer && taskTimeLimit > 0);

        StartCoroutine(RunTaskLoop(index));
    }

    IEnumerator RunTaskLoop(int index)
    {
        var task = tasks[index];

        while (!taskComplete)
        {
            int cur = GetCurrentQueueCount();
            if (cur != lastKnownQueueCount)
            {
                OnQueueCountChanged(cur > lastKnownQueueCount, cur);
                lastKnownQueueCount = cur;
            }

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

        bool passed = earned >= Mathf.CeilToInt(tasks[index].maxPoints * 0.6f);
        ShowFeedback(passed,
            passed ? $" +{earned} pts — Well done!" : $" +{earned}/{tasks[index].maxPoints} pts");

        yield return new WaitForSeconds(2.0f);
        SetActive(taskFeedbackBanner, false);

        currentTaskIndex++;
        if (currentTaskIndex < tasks.Length)
            StartTask(currentTaskIndex);
        else
            ShowResults();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OPERATION DETECTION
    // ─────────────────────────────────────────────────────────────────────────
    void OnQueueCountChanged(bool enqueued, int newCount)
    {
        if (enqueued)
        {
            t_enqueued = true;
            t_enqueueCount++;
            t_operationCount++;
        }
        else
        {
            t_dequeued = true;
            t_dequeueCount++;
            t_operationCount++;
        }
    }

    public void NotifyEnqueue()
    {
        t_enqueued = true;
        t_enqueueCount++;
        t_operationCount++;
    }

    public void NotifyDequeue()
    {
        t_dequeued = true;
        t_dequeueCount++;
        t_operationCount++;
    }

    public void NotifyPeek()
    {
        t_peeked = true;
        t_operationCount++;
    }

    public void NotifyPriorityEnqueue(int priority, int insertIndex)
    {
        t_priorityEnqueued = true;
        t_enqueued         = true;
        t_enqueueCount++;
        t_operationCount++;
        t_lastPriority    = priority;
        t_lastInsertIndex = insertIndex;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GRADING
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
                    case 0: // Place scene
                        return IsSceneReady() ? task.maxPoints : 0;
                    case 1: // Observe FIFO — dequeue at least one
                        return t_dequeued ? task.maxPoints
                             : t_enqueued ? Mathf.RoundToInt(task.maxPoints * 0.5f) : 0;
                    case 2: // Prove FIFO — enqueue then dequeue
                        bool fifo = t_enqueued && t_dequeued;
                        return fifo ? task.maxPoints
                             : (t_enqueued || t_dequeued)
                                 ? Mathf.RoundToInt(task.maxPoints * 0.5f) : 0;
                }
                break;

            // ── L2: Operations ───────────────────────────────────────────────
            case 1:
                switch (taskIndex)
                {
                    case 0: return t_enqueued ? task.maxPoints : 0;
                    case 1: return t_dequeued ? task.maxPoints : 0;
                    case 2: return t_peeked   ? task.maxPoints : 0;
                    case 3:
                        int ops = (t_enqueued ? 1 : 0) + (t_dequeued ? 1 : 0) + (t_peeked ? 1 : 0);
                        return Mathf.RoundToInt(task.maxPoints * (ops / 3f));
                }
                break;

            // ── L3: Types of Queue ───────────────────────────────────────────
            case 2:
                switch (taskIndex)
                {
                    case 0: // Simple enqueue
                        return t_enqueued ? task.maxPoints : 0;
                    case 1: // Priority enqueue
                        return t_priorityEnqueued ? task.maxPoints : 0;
                    case 2: // Dequeue (FIFO vs priority comparison)
                        return t_dequeued ? task.maxPoints : 0;
                    case 3: // All three
                        int typeOps = (t_enqueued ? 1 : 0) + (t_priorityEnqueued ? 1 : 0) + (t_dequeued ? 1 : 0);
                        return Mathf.RoundToInt(task.maxPoints * (typeOps / 3f));
                }
                break;

            // ── L4: Implementation ───────────────────────────────────────────
            case 3:
                switch (taskIndex)
                {
                    case 0: // Enqueue twice (array sim)
                        int enq2 = Mathf.Clamp(t_enqueueCount, 0, 2);
                        return Mathf.RoundToInt(task.maxPoints * (enq2 / 2f));
                    case 1: // Dequeue once (LL sim)
                        return t_dequeued ? task.maxPoints : 0;
                    case 2: // Both
                        bool both = t_enqueued && t_dequeued;
                        return both ? task.maxPoints
                             : (t_enqueued || t_dequeued)
                                 ? Mathf.RoundToInt(task.maxPoints * 0.5f) : 0;
                }
                break;

            // ── L5: Applications + Complexity ─────────────────────────────────
            case 4:
                int distinct =
                    (t_enqueued         ? 1 : 0) +
                    (t_dequeued         ? 1 : 0) +
                    (t_peeked           ? 1 : 0) +
                    (t_priorityEnqueued ? 1 : 0);
                return Mathf.RoundToInt(task.maxPoints * (distinct / 4f));
        }

        return 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RESULTS
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

        if (resultsTitleText != null) resultsTitleText.text = $"Lesson {lessonIndex + 1} Complete!";
        if (resultsScoreText != null) resultsScoreText.text = $"{totalScore} / {maxPossibleScore}  ({Mathf.RoundToInt(pct * 100)}%)";
        if (resultsGradeText != null) { resultsGradeText.text = grade; resultsGradeText.color = gradeColor; }
        if (gradeRingImage   != null) { gradeRingImage.fillAmount = pct; gradeRingImage.color = gradeColor; }

        string breakdown = "Task Breakdown:\n";
        for (int i = 0; i < taskScores.Count && i < taskNames.Count; i++)
        {
            int    max  = i < tasks.Length ? tasks[i].maxPoints : 0;
            string tick = taskScores[i] >= Mathf.CeilToInt(max * 0.6f) ? "" : "";
            breakdown += $"{tick} {taskNames[i]}:  {taskScores[i]}/{max}\n";
        }
        if (resultsBreakdownText != null) resultsBreakdownText.text = breakdown;
        if (resultsTipText       != null) resultsTipText.text       = BuildTip(lessonIndex, pct);

        PlayerPrefs.SetInt($"AR_Assessment_Queue_L{lessonIndex}_Score", totalScore);
        PlayerPrefs.SetInt($"AR_Assessment_Queue_L{lessonIndex}_Max",   maxPossibleScore);
        PlayerPrefs.SetString($"AR_Assessment_Queue_L{lessonIndex}_Grade", grade);
        PlayerPrefs.Save();
    }

    string BuildTip(int lesson, float pct)
    {
        if (pct >= 0.9f) return "Excellent! You have a strong grasp of this lesson.";
        switch (lesson)
        {
            case 0: return "Review: queues follow FIFO — first in is always first out.";
            case 1: return "Review: Enqueue, Dequeue, and Peek all run in O(1) time.";
            case 2: return "Review: simple FIFO vs circular vs priority vs deque.";
            case 3: return "Review: array queues are fixed-size; linked list queues grow dynamically.";
            case 4: return "Review: core ops are O(1); Priority Enqueue and Traversal are O(n).";
            default: return "Keep practising in AR to reinforce your understanding!";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RETRY / RETURN
    // ─────────────────────────────────────────────────────────────────────────
    void RetryAssessment()
    {
        SetActive(resultsPanel, false);
        totalScore = 0; currentTaskIndex = 0; taskScores.Clear();
        OnStartButtonClicked();
    }

    void OnReturnClicked()
    {
        HideAssessment();
        PlayerPrefs.SetInt(ARReturnHandler.AR_JUST_RETURNED_KEY, 1);
        PlayerPrefs.SetString(ARReturnHandler.AR_TOPIC_NAME_KEY,
                              PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "queue"));
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
            // L1 — Introduction
            // =================================================================
            case 0: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Place the Queue Scene",
                    instruction =
                        "Point your camera at a flat surface\n" +
                        "and tap to place the queue scene.\n\n" +
                        "A queue follows FIFO:\n" +
                        "First In — First Out.\n\n" +
                        "Notice the FRONT and REAR\n" +
                        "markers in the scene.",
                    maxPoints       = 30,
                    completionCheck = () => IsSceneReady()
                },
                new AssessmentTask
                {
                    title       = "Observe FIFO — Dequeue the Front",
                    instruction =
                        "The queue has pre-filled items.\n\n" +
                        "Tap DEQUEUE to remove the FRONT.\n\n" +
                        "Notice: you can ONLY remove from\n" +
                        "the FRONT — never the middle!\n\n" +
                        "Item #1 leaves before #2, #3, #4.\n" +
                        "This is FIFO in action.",
                    maxPoints       = 30,
                    completionCheck = () => t_dequeued
                },
                new AssessmentTask
                {
                    title       = "Prove FIFO Order",
                    instruction =
                        "Now ENQUEUE a new item at the\n" +
                        "rear, then DEQUEUE from the front.\n\n" +
                        "The item you added last should\n" +
                        "wait for everyone ahead of it.\n\n" +
                        "This proves FIFO:\n" +
                        "First In = First Out!",
                    maxPoints       = 40,
                    completionCheck = () => t_enqueued && t_dequeued
                },
            };

            // =================================================================
            // L2 — Operations
            // =================================================================
            case 1: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "ENQUEUE an Item",
                    instruction =
                        "Tap ENQUEUE and move the item\n" +
                        "to the REAR of the queue.\n\n" +
                        "Steps for enqueue:\n" +
                        "1. Check queue is not full\n" +
                        "2. Increment rear pointer\n" +
                        "3. Place element at rear\n\n" +
                        "Time Complexity: O(1)",
                    maxPoints       = 25,
                    completionCheck = () => t_enqueued
                },
                new AssessmentTask
                {
                    title       = "DEQUEUE the Front Item",
                    instruction =
                        "Tap DEQUEUE to serve the person\n" +
                        "at the FRONT of the queue.\n\n" +
                        "Steps for dequeue:\n" +
                        "1. Check queue is not empty\n" +
                        "2. Return front element\n" +
                        "3. Increment front pointer\n\n" +
                        "Time Complexity: O(1)",
                    maxPoints       = 25,
                    completionCheck = () => t_dequeued
                },
                new AssessmentTask
                {
                    title       = "PEEK at the Front",
                    instruction =
                        "Tap the PEEK button.\n\n" +
                        "The front item will glow cyan.\n" +
                        "Notice: the queue is UNCHANGED!\n\n" +
                        "Peek is read-only — it views\n" +
                        "the front without removing it.\n\n" +
                        "Time Complexity: O(1)",
                    maxPoints       = 25,
                    completionCheck = () => t_peeked
                },
                new AssessmentTask
                {
                    title       = "Complete All Operations",
                    instruction =
                        "Perform ALL three operations:\n\n" +
                        "   ENQUEUE  — add to rear\n" +
                        "   DEQUEUE  — remove from front\n" +
                        "   PEEK     — view front (no remove)\n\n" +
                        "All three run in O(1) time!\n\n" +
                        "Can you complete all three?",
                    maxPoints       = 25,
                    completionCheck = () => t_enqueued && t_dequeued && t_peeked
                },
            };

            // =================================================================
            // L3 — Types of Queue
            // =================================================================
            case 2: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Simple Queue — Enqueue",
                    instruction =
                        "Demonstrate the SIMPLE QUEUE:\n\n" +
                        "Tap ENQUEUE to add a person\n" +
                        "to the rear.\n\n" +
                        "This is the basic FIFO queue —\n" +
                        "insertion always at the rear,\n" +
                        "deletion always at the front.",
                    maxPoints       = 25,
                    completionCheck = () => t_enqueued
                },
                new AssessmentTask
                {
                    title       = "Priority Queue — Priority Enqueue",
                    instruction =
                        "Demonstrate the PRIORITY QUEUE:\n\n" +
                        "Switch to INTERMEDIATE mode and\n" +
                        "tap PRIORITY ENQUEUE.\n\n" +
                        "Enter a priority (1=highest).\n" +
                        "Watch the item insert at the\n" +
                        "correct position — not just rear!\n\n" +
                        "Time Complexity: O(n)",
                    maxPoints       = 35,
                    completionCheck = () => t_priorityEnqueued
                },
                new AssessmentTask
                {
                    title       = "Dequeue — FIFO or Priority?",
                    instruction =
                        "Now tap DEQUEUE.\n\n" +
                        "Observe who leaves first:\n\n" +
                        "  Simple Queue: index [0] first\n" +
                        "  Priority Queue: highest priority\n" +
                        "  first (regardless of arrival)\n\n" +
                        "This is the key difference between\n" +
                        "FIFO and Priority ordering!",
                    maxPoints       = 20,
                    completionCheck = () => t_dequeued
                },
                new AssessmentTask
                {
                    title       = "All Queue Type Operations",
                    instruction =
                        "Complete all queue type operations:\n\n" +
                        "   ENQUEUE          — basic FIFO\n" +
                        "   PRIORITY ENQUEUE — by priority\n" +
                        "   DEQUEUE          — from front\n\n" +
                        "Each type serves a different\n" +
                        "computing purpose!",
                    maxPoints       = 20,
                    completionCheck = () => t_enqueued && t_priorityEnqueued && t_dequeued
                },
            };

            // =================================================================
            // L4 — Implementation
            // =================================================================
            case 3: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Array Enqueue — queue[++rear]",
                    instruction =
                        "In an array-based queue:\n\n" +
                        "  enqueue:  queue[++rear] = value\n\n" +
                        "Enqueue at least 2 items to simulate\n" +
                        "filling array slots at indices\n" +
                        "rear=0, rear=1 as rear increments.\n\n" +
                        "Time: O(1)  |  Size: FIXED",
                    maxPoints       = 30,
                    completionCheck = () => t_enqueueCount >= 2
                },
                new AssessmentTask
                {
                    title       = "Linked List Dequeue — front = front.next",
                    instruction =
                        "In a linked list queue:\n\n" +
                        "  dequeue:  value = front.data\n" +
                        "            front = front.next\n\n" +
                        "Tap DEQUEUE to simulate removing\n" +
                        "the head node.\n\n" +
                        "No wasted front slots —\n" +
                        "memory is freed immediately!\n\n" +
                        "Time: O(1)",
                    maxPoints       = 30,
                    completionCheck = () => t_dequeued
                },
                new AssessmentTask
                {
                    title       = "Both Implementations",
                    instruction =
                        "Now combine both:\n\n" +
                        "Array:  queue[++rear] = value\n" +
                        "LL:     rear.next = new_node\n\n" +
                        "Array:  return queue[front++]\n" +
                        "LL:     front = front.next\n\n" +
                        "ENQUEUE and DEQUEUE one more time\n" +
                        "to complete the comparison.\n\n" +
                        "Both give O(1) for core ops!",
                    maxPoints       = 40,
                    completionCheck = () => t_enqueued && t_dequeued
                },
            };

            // =================================================================
            // L5 — Applications + Complexity
            // =================================================================
            case 4: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Light Up the Complexity Table",
                    instruction =
                        "Perform as many DIFFERENT operations\n" +
                        "as you can:\n\n" +
                        "   ENQUEUE          — O(1)\n" +
                        "   DEQUEUE          — O(1)\n" +
                        "   PEEK             — O(1)\n" +
                        "   PRIORITY ENQUEUE — O(n)\n\n" +
                        "Use INTERMEDIATE mode for\n" +
                        "Priority Enqueue!\n\n" +
                        "More distinct operations\n" +
                        "= more points!",
                    maxPoints       = 100,
                    completionCheck = () =>
                        t_enqueued && t_dequeued && t_peeked && t_priorityEnqueued
                },
            };

            default: return new AssessmentTask[0];
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FEEDBACK
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
        t_enqueued         = false;
        t_dequeued         = false;
        t_peeked           = false;
        t_priorityEnqueued = false;
        t_enqueueCount     = 0;
        t_dequeueCount     = 0;
        t_operationCount   = 0;
        t_lastPriority     = -1;
        t_lastInsertIndex  = -1;
    }

    int GetCurrentQueueCount()
    {
        if (queueController == null) return 0;
        return queueController.GetQueueSize();
    }

    bool IsSceneReady()
    {
        return queueController != null && queueController.IsReady();
    }

    void SetActive(GameObject go, bool state)
    {
        if (go != null) go.SetActive(state);
    }
}