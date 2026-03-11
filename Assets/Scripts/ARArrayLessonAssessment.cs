using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ARArrayLessonAssessment.cs
/// ===========================
/// Attaches to the same GameObject as ARArrayLessonGuide.
///
/// FLOW:
///   1. ARArrayLessonGuide finishes its last step -> calls
///      ARArrayLessonAssessment.BeginAssessment(lessonIndex)
///   2. Assessment panel fades in with instructions
///   3. Student performs AR actions; each action is graded
///   4. Results panel shown (score / grade / breakdown)
///   5. Student taps "Return" -> normal OnReturn() back to app
///
/// LESSON ASSESSMENTS:
///   L1 (index 0): Tap 3 elements in order -> check index knowledge
///   L2 (index 1): Run Linear -> Reverse -> any loop-type traversal
///   L3 (index 2): Insert at end, insert at position, then remove
///   L4 (index 3): Insert mid-array item + identify advantage/limitation
///   L5 (index 4): Perform one of each operation to light up complexity table
///
/// GRADING:
///   Each task is worth a fixed point value.
///   Score% -> Grade:  90+=A  75+=B  60+=C  below=Needs Review
/// </summary>
public class ARArrayLessonAssessment : MonoBehaviour
{
    // -- References -----------------------------------------------------------
    [Header("Sibling Scripts")]
    public ARArrayLessonGuide    lessonGuide;
    public InteractiveArrayCars  arrayController;

    // -- Assessment Canvas ----------------------------------------------------
    [Header("Assessment Canvas")]
    public Canvas          assessmentCanvas;
    public GameObject      assessmentRoot;

    // Intro / transition panel
    public GameObject      introPanel;
    public TextMeshProUGUI introTitleText;
    public TextMeshProUGUI introBodyText;
    public Button          startAssessmentButton;

    // Live task panel (shown during assessment)
    public GameObject      taskPanel;
    public TextMeshProUGUI taskTitleText;
    public TextMeshProUGUI taskInstructionText;
    public TextMeshProUGUI taskProgressText;
    public TextMeshProUGUI taskTimerText;
    public Image           taskProgressBarFill;
    public GameObject      taskFeedbackBanner;
    public TextMeshProUGUI taskFeedbackText;

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
    public GameObject      taskMinimiseButton;
    public GameObject      taskCollapsedTab;
    public GameObject      taskCardPanel;

    // -- Settings -------------------------------------------------------------
    [Header("Settings")]
    public float taskTimeLimit    = 60f;
    public bool  showTimer        = true;
    public string mainAppSceneName = "MainScene";

    // -- Internal State -------------------------------------------------------
    struct AssessmentTask
    {
        public string title;
        public string instruction;
        public int    maxPoints;
        public System.Func<bool> completionCheck;
    }

    private int            lessonIndex       = -1;
    private AssessmentTask[] tasks;
    private int            currentTaskIndex  = 0;
    private int            totalScore        = 0;
    private int            maxPossibleScore  = 0;
    private List<int>      taskScores        = new List<int>();
    private List<string>   taskNames         = new List<string>();

    private bool           assessmentActive  = false;
    private float          taskTimer         = 0f;
    private bool           taskComplete      = false;

    // Per-task tracking flags (reset between tasks)
    private bool t_insertedAtEnd     = false;
    private bool t_insertedAtMid     = false;
    private bool t_removed           = false;
    private bool t_accessPerformed   = false;
    private bool t_linearDone        = false;
    private bool t_reverseDone       = false;
    private bool t_loopTypeDone      = false;
    private int  t_elementsInspected = 0;
    private int  t_operationsCount   = 0;
    private int  lastKnownItemCount  = 0;

    private int  preTaskItemCount    = 0;

    // -------------------------------------------------------------------------
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

    void OnEnable() { }

    void HideAssessment()
    {
        if (assessmentRoot != null) assessmentRoot.SetActive(false);
        assessmentActive = false;
        StopAllCoroutines();
        Debug.Log("[Assessment] HideAssessment — root hidden, state reset");
    }

    // -------------------------------------------------------------------------
    // PUBLIC ENTRY — call from ARArrayLessonGuide when its last step is shown
    // -------------------------------------------------------------------------
    public void BeginAssessment(int lesson)
    {
        Debug.Log($"[Assessment] BeginAssessment called for lesson {lesson}");

        assessmentActive = true;

        if (assessmentRoot != null)
            assessmentRoot.SetActive(true);

        if (introPanel == null)   introPanel   = GameObject.Find("AssessmentIntroPanel");
        if (taskPanel == null)    taskPanel    = GameObject.Find("AssessmentTaskPanel");
        if (resultsPanel == null) resultsPanel = GameObject.Find("AssessmentResultsPanel");

        if (introPanel == null)
        {
            var found = GameObject.Find("AssessmentIntroPanel");
            if (found != null) introPanel = found;
            Debug.Log($"[Assessment] introPanel auto-resolved: {(introPanel != null ? "FOUND" : "NULL")}");
        }
        if (taskPanel == null)
        {
            var found = GameObject.Find("AssessmentTaskPanel");
            if (found != null) taskPanel = found;
        }
        if (resultsPanel == null)
        {
            var found = GameObject.Find("AssessmentResultsPanel");
            if (found != null) resultsPanel = found;
        }

        lessonIndex = lesson;
        tasks       = BuildTasks(lesson);
        if (tasks == null || tasks.Length == 0)
        {
            Debug.LogWarning($"[Assessment] No tasks for lesson {lesson}");
            return;
        }

        maxPossibleScore = 0;
        foreach (var t in tasks) maxPossibleScore += t.maxPoints;

        totalScore       = 0;
        currentTaskIndex = 0;
        taskScores.Clear();
        taskNames.Clear();
        foreach (var t in tasks) taskNames.Add(t.title);

        Debug.Log($"[Assessment] Starting {tasks.Length} tasks, max={maxPossibleScore}pts");
        ShowIntroPanel();
    }

    // -------------------------------------------------------------------------
    // INTRO PANEL
    // -------------------------------------------------------------------------
    void ShowIntroPanel()
    {
        if (assessmentCanvas != null)
        {
            assessmentCanvas.gameObject.SetActive(true);
            assessmentCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
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
            Debug.LogError("[Assessment] introPanel is NULL");
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

        string body = $"You completed the lesson guide!\n\n" +
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

            var btnRect = startAssessmentButton.GetComponent<RectTransform>();
            if (btnRect != null)
            {
                Vector3[] corners = new Vector3[4];
                btnRect.GetWorldCorners(corners);
                Debug.Log($"[Assessment] Button screen corners: BL={corners[0]} TR={corners[2]}");
            }

            CanvasGroup[] groups = startAssessmentButton.GetComponentsInParent<CanvasGroup>();
            foreach (var cg in groups)
            {
                cg.interactable   = true;
                cg.blocksRaycasts = true;
            }

            Debug.Log($"[Assessment] Button wired. " +
                      $"Interactable={startAssessmentButton.interactable}, " +
                      $"Active={startAssessmentButton.gameObject.activeInHierarchy}");
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"[Assessment] OnApplicationFocus: {hasFocus}, assessmentActive={assessmentActive}");
        if (hasFocus && assessmentActive && introPanel != null && introPanel.activeSelf)
        {
            Debug.Log("[Assessment] Regained focus during intro — re-wiring button");
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

    // -------------------------------------------------------------------------
    // TASK FLOW
    // -------------------------------------------------------------------------
    void StartTask(int index)
    {
        if (index >= tasks.Length) { ShowResults(); return; }

        SetActive(taskCardPanel,    true);
        SetActive(taskCollapsedTab, false);

        taskComplete       = false;
        taskTimer          = taskTimeLimit > 0 ? taskTimeLimit : float.MaxValue;
        preTaskItemCount   = CountItems();
        lastKnownItemCount = preTaskItemCount;
        ResetTaskFlags();

        SetActive(taskPanel,          true);
        SetActive(taskFeedbackBanner, false);

        var task = tasks[index];
        if (taskTitleText       != null) taskTitleText.text       = task.title;
        if (taskInstructionText != null) taskInstructionText.text = task.instruction;
        if (taskProgressText    != null)
            taskProgressText.text = $"Task {index + 1} / {tasks.Length}";
        if (taskProgressBarFill != null)
            taskProgressBarFill.fillAmount = (float)(index + 1) / tasks.Length;
        if (taskTimerText != null)
            taskTimerText.gameObject.SetActive(showTimer && taskTimeLimit > 0);

        if (lessonIndex == 0 && (index == 1 || index == 2))
        {
            t_elementsInspected = 0;
            StartCoroutine(L1AutoInspectDemo(index));
        }

        if (lessonIndex != 0 && arrayController != null)
        {
            arrayController.mainButtonPanel?.SetActive(true);
            arrayController.beginnerButtonsPanel?.SetActive(true);
        }

        StartCoroutine(RunTaskLoop(index));
    }

    IEnumerator L1AutoInspectDemo(int taskIndex)
    {
        if (arrayController == null) yield break;

        yield return new WaitForSeconds(1.5f);

        int capacity    = arrayController.arrayCapacity;
        int targetCount = (taskIndex == 1) ? 3 : capacity;

        Debug.Log($"[Assessment] L1AutoInspectDemo starting, target={targetCount}");

        for (int i = 0; i < capacity && t_elementsInspected < targetCount; i++)
        {
            if (!arrayController.IsSlotOccupied(i)) continue;

            GameObject slotItem = arrayController.GetSlotItem(i);
            if (slotItem != null)
            {
                GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                highlight.transform.SetParent(slotItem.transform);
                highlight.transform.localPosition = Vector3.zero;
                highlight.transform.localScale    = Vector3.one * 0.1f;

                Renderer rend = highlight.GetComponent<Renderer>();
                Material mat  = new Material(Shader.Find("Unlit/Color"));
                mat.color     = new Color(0f, 1f, 1f, 0.4f);
                rend.material = mat;
                Destroy(highlight.GetComponent<Collider>());

                if (taskInstructionText != null)
                    taskInstructionText.text =
                        $"Accessing index [{i}]...\n\n" +
                        $"address = base + {i} x size\n\n" +
                        $"O(1) — instant!\n\n" +
                        $"Inspected: {t_elementsInspected + 1} / {targetCount}";

                NotifyElementInspected();

                yield return new WaitForSeconds(1.2f);
                Destroy(highlight);
                yield return new WaitForSeconds(0.3f);
            }
        }

        while (t_elementsInspected < targetCount)
        {
            NotifyElementInspected();
            yield return null;
        }

        Debug.Log($"[Assessment] L1AutoInspectDemo done, inspected={t_elementsInspected}");
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

            int cur = CountItems();
            if (cur != lastKnownItemCount)
            {
                bool inserted = cur > lastKnownItemCount;
                OnItemCountChanged(inserted, cur);
                lastKnownItemCount = cur;
            }

            if (taskTimeLimit > 0)
            {
                taskTimer -= Time.deltaTime;
                if (taskTimerText != null)
                    taskTimerText.text = $"Time: {Mathf.CeilToInt(taskTimer)}s";

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

        int earned  = GradeTask(index);
        taskScores.Add(earned);
        totalScore += earned;

        bool passed = earned >= Mathf.CeilToInt(task.maxPoints * 0.6f);
        ShowFeedback(passed,
            passed ? $"+{earned} pts — Well done!" : $"+{earned}/{task.maxPoints} pts");

        yield return new WaitForSeconds(2.0f);
        SetActive(taskFeedbackBanner, false);

        currentTaskIndex++;
        if (currentTaskIndex < tasks.Length)
            StartTask(currentTaskIndex);
        else
            ShowResults();
    }

    // -------------------------------------------------------------------------
    // OPERATION DETECTION
    // -------------------------------------------------------------------------
    void OnItemCountChanged(bool inserted, int newCount)
    {
        if (inserted)
        {
            bool wasMid = (preTaskItemCount > 0) && (newCount - 1 < preTaskItemCount);
            if (wasMid) t_insertedAtMid = true;
            else        t_insertedAtEnd = true;
            t_operationsCount++;
        }
        else
        {
            t_removed = true;
            t_operationsCount++;
        }
    }

    public void NotifyTraversal(string type)
    {
        switch (type)
        {
            case "linear":  t_linearDone   = true; break;
            case "reverse": t_reverseDone  = true; break;
            case "for":
            case "while":
            case "foreach": t_loopTypeDone = true; break;
        }
        t_operationsCount++;
    }

    public void NotifyAccess()
    {
        t_accessPerformed = true;
        t_operationsCount++;
    }

    public void NotifyElementInspected()
    {
        t_elementsInspected++;
    }

    // -------------------------------------------------------------------------
    // GRADING PER TASK
    // -------------------------------------------------------------------------
    int GradeTask(int taskIndex)
    {
        if (tasks == null || taskIndex >= tasks.Length) return 0;
        var task = tasks[taskIndex];

        switch (lessonIndex)
        {
            case 0:
                switch (taskIndex)
                {
                    case 0:
                        return IsSceneSpawned() ? task.maxPoints : 0;
                    case 1:
                        int seen = Mathf.Clamp(t_elementsInspected, 0, 3);
                        return Mathf.RoundToInt(task.maxPoints * (seen / 3f));
                    case 2:
                        int allSeen = Mathf.Clamp(t_elementsInspected, 0, arrayController.arrayCapacity);
                        return Mathf.RoundToInt(task.maxPoints * (allSeen / (float)arrayController.arrayCapacity));
                }
                break;

            case 1:
                switch (taskIndex)
                {
                    case 0: return t_linearDone   ? task.maxPoints : 0;
                    case 1: return t_reverseDone  ? task.maxPoints : 0;
                    case 2: return t_loopTypeDone ? task.maxPoints : 0;
                    case 3:
                        int done = (t_linearDone   ? 1 : 0) +
                                   (t_reverseDone  ? 1 : 0) +
                                   (t_loopTypeDone ? 1 : 0);
                        return Mathf.RoundToInt(task.maxPoints * (done / 3f));
                }
                break;

            case 2:
                switch (taskIndex)
                {
                    case 0: return t_insertedAtEnd                        ? task.maxPoints : 0;
                    case 1: return (t_insertedAtMid || t_insertedAtEnd)   ? task.maxPoints : 0;
                    case 2: return t_accessPerformed                      ? task.maxPoints : 0;
                    case 3: return t_removed                              ? task.maxPoints : 0;
                }
                break;

            case 3:
                switch (taskIndex)
                {
                    case 0: return t_accessPerformed                      ? task.maxPoints : 0;
                    case 1: return (t_insertedAtMid || t_insertedAtEnd)   ? task.maxPoints : 0;
                    case 2: return t_removed                              ? task.maxPoints : 0;
                }
                break;

            case 4:
                int ops = (t_insertedAtEnd   ? 1 : 0) +
                          (t_insertedAtMid   ? 1 : 0) +
                          (t_removed         ? 1 : 0) +
                          (t_accessPerformed ? 1 : 0) +
                          (t_linearDone      ? 1 : 0) +
                          (t_reverseDone     ? 1 : 0) +
                          (t_loopTypeDone    ? 1 : 0);
                return Mathf.RoundToInt(task.maxPoints * (ops / 7f));
        }

        return 0;
    }

    // -------------------------------------------------------------------------
    // RESULTS PANEL
    // -------------------------------------------------------------------------
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
            string tick = taskScores[i] >= Mathf.CeilToInt(max * 0.6f) ? "Pass" : "Fail";
            breakdown += $"{tick}  {taskNames[i]}:  {taskScores[i]}/{max}\n";
        }
        if (resultsBreakdownText != null) resultsBreakdownText.text = breakdown;

        string tip = BuildTip(lessonIndex, pct);
        if (resultsTipText != null) resultsTipText.text = tip;

        PlayerPrefs.SetInt($"AR_Assessment_L{lessonIndex}_Score", totalScore);
        PlayerPrefs.SetInt($"AR_Assessment_L{lessonIndex}_Max",   maxPossibleScore);
        PlayerPrefs.SetString($"AR_Assessment_L{lessonIndex}_Grade", grade);
        PlayerPrefs.Save();
    }

    string BuildTip(int lesson, float pct)
    {
        if (pct >= 0.9f)
            return "Excellent work! You have a strong grasp of this lesson.";

        switch (lesson)
        {
            case 0: return "Review: arrays use 0-based indices and O(1) direct access.";
            case 1: return "Review: all traversal types visit every element — O(n).";
            case 2: return "Review: insert/delete at end = O(1); at mid/beg = O(n).";
            case 3: return "Review: arrays are fast at access (O(1)) but slow at mid-changes (O(n)).";
            case 4: return "Review: memorise the full Big-O table — access O(1), traversal O(n), binary search O(log n).";
            default: return "Keep practising in AR to reinforce your understanding!";
        }
    }

    // -------------------------------------------------------------------------
    // RETRY
    // -------------------------------------------------------------------------
    void RetryAssessment()
    {
        SetActive(resultsPanel, false);
        totalScore       = 0;
        currentTaskIndex = 0;
        taskScores.Clear();
        OnStartButtonClicked();
    }

    // -------------------------------------------------------------------------
    // RETURN
    // -------------------------------------------------------------------------
    void OnReturnClicked()
    {
        HideAssessment();

        PlayerPrefs.SetInt(ARReturnHandler.AR_JUST_RETURNED_KEY, 1);
        PlayerPrefs.SetString(ARReturnHandler.AR_TOPIC_NAME_KEY,
                              PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "arrays"));
        PlayerPrefs.Save();
        SceneManager.LoadScene(PlayerPrefs.GetString("AR_MainScene", mainAppSceneName));
    }

    // -------------------------------------------------------------------------
    // TASK DEFINITIONS
    // -------------------------------------------------------------------------
    AssessmentTask[] BuildTasks(int lesson)
    {
        switch (lesson)
        {
            // =================================================================
            // L1 — Array Basics & Memory
            // =================================================================
            case 0: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Place the Array Scene",
                    instruction =
                        "Point your camera at a flat surface\n" +
                        "and tap to place the array scene.\n\n" +
                        "Arrays store elements in fixed,\n" +
                        "contiguous memory slots.",
                    maxPoints       = 30,
                    completionCheck = () => arrayController != null && IsSceneSpawned()
                },
                new AssessmentTask
                {
                    title       = "Watch Element Access",
                    instruction =
                        "The demo will automatically highlight\n" +
                        "each element in the array.\n\n" +
                        "Each access uses:\n" +
                        "address = base + index x size\n\n" +
                        "This is O(1) — instant, no scanning!\n\n" +
                        "Watch all elements get highlighted...",
                    maxPoints       = 40,
                    completionCheck = () => t_elementsInspected >= 3
                },
                new AssessmentTask
                {
                    title       = "Identify the Access Pattern",
                    instruction =
                        "The demo highlighted elements\n" +
                        "at index [0], [1], [2]...\n\n" +
                        "Each index maps directly to a\n" +
                        "memory address — no searching!\n\n" +
                        "This is why array access is O(1).\n\n" +
                        "Watching the full demo completes this.",
                    maxPoints       = 30,
                    completionCheck = () => t_elementsInspected >= arrayController.arrayCapacity
                },
            };

            // =================================================================
            // L2 — Traversal
            // =================================================================
            case 1: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Forward Traversal",
                    instruction =
                        "Use the traversal buttons above.\n\n" +
                        "Which button visits elements\n" +
                        "from index 0 to the end?\n\n" +
                        "Tap it to demonstrate!",
                    maxPoints       = 25,
                    completionCheck = () => t_linearDone
                },
                new AssessmentTask
                {
                    title       = "Backward Traversal",
                    instruction =
                        "Use the traversal buttons above.\n\n" +
                        "Which button visits elements\n" +
                        "from the last index back to 0?\n\n" +
                        "Tap it to demonstrate!",
                    maxPoints       = 25,
                    completionCheck = () => t_reverseDone
                },
                new AssessmentTask
                {
                    title       = "Choose a Loop Style",
                    instruction =
                        "Three of the buttons use different\n" +
                        "loop styles to traverse the array.\n\n" +
                        "Pick any one and tap it.\n\n" +
                        "What do all three have in common?",
                    maxPoints       = 25,
                    completionCheck = () => t_loopTypeDone
                },
                new AssessmentTask
                {
                    title       = "Complete All Traversals",
                    instruction =
                        "Now run ALL traversal types\n" +
                        "using the buttons above.\n\n" +
                        "Can you light up every button\n" +
                        "before time runs out?",
                    maxPoints       = 25,
                    completionCheck = () => t_linearDone && t_reverseDone && t_loopTypeDone
                },
            };

            // =================================================================
            // L3 — Operations
            // =================================================================
            case 2: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Insert at the End  (O(1))",
                    instruction =
                        "Use INSERT and add an item\n" +
                        "to the LAST available slot.\n\n" +
                        "No shifting needed — this is O(1).\n" +
                        "The fastest kind of insert!",
                    maxPoints       = 25,
                    completionCheck = () => t_insertedAtEnd
                },
                new AssessmentTask
                {
                    title       = "Insert at Index 0 or 1",
                    instruction =
                        "Use INSERT and place an item\n" +
                        "at index 0 or index 1.\n\n" +
                        "In a real array, inserting here\n" +
                        "would shift all other elements right — O(n).\n\n" +
                        "This is slower than inserting at the end!",
                    maxPoints       = 25,
                    completionCheck = () => t_insertedAtMid || t_insertedAtEnd
                },
                new AssessmentTask
                {
                    title       = "Access an Element  (O(1))",
                    instruction =
                        "Use the ACCESS button and enter\n" +
                        "any valid index.\n\n" +
                        "The computer jumps directly to that\n" +
                        "memory address — O(1), no scanning.",
                    maxPoints       = 25,
                    completionCheck = () => t_accessPerformed
                },
                new AssessmentTask
                {
                    title       = "Remove an Element  (O(n))",
                    instruction =
                        "Use the REMOVE button to delete\n" +
                        "an item from the array.\n\n" +
                        "Elements after the gap shift left.\n" +
                        "Time: O(n) — unless removing from end!",
                    maxPoints       = 25,
                    completionCheck = () => t_removed
                },
            };

            // =================================================================
            // L4 — Advantages & Limitations
            // =================================================================
            case 3: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Demonstrate Fast Access",
                    instruction =
                        "Use ACCESS to retrieve two different\n" +
                        "elements by index.\n\n" +
                        "This demonstrates the key ADVANTAGE:\n" +
                        "O(1) random access — instant lookup!",
                    maxPoints       = 30,
                    completionCheck = () => t_accessPerformed
                },
                new AssessmentTask
                {
                    title       = "Demonstrate Costly Mid-Insert",
                    instruction =
                        "Insert an item at index 0 or 1.\n\n" +
                        "In a real array, all elements after\n" +
                        "that position must shift right.\n\n" +
                        "This is the key LIMITATION:\n" +
                        "O(n) cost for mid-array changes.",
                    maxPoints       = 40,
                    completionCheck = () => t_insertedAtMid || t_insertedAtEnd
                },
                new AssessmentTask
                {
                    title       = "Demonstrate Deletion Cost",
                    instruction =
                        "Use REMOVE to delete any item.\n\n" +
                        "In a real array, elements after\n" +
                        "the removed slot shift left to fill the gap.\n\n" +
                        "This proves the trade-off:\n" +
                        "Great access speed, but costly mid-ops.",
                    maxPoints       = 30,
                    completionCheck = () => t_removed
                },
            };

            // =================================================================
            // L5 — Big-O Complexity Summary
            // =================================================================
            case 4: return new AssessmentTask[]
            {
                new AssessmentTask
                {
                    title       = "Activate All Rows of the Complexity Table",
                    instruction =
                        "Perform as many DIFFERENT operations\n" +
                        "as you can to light up each row:\n\n" +
                        "  * Access (O(1))\n" +
                        "  * Linear + Reverse Traversal (O(n))\n" +
                        "  * For / While / Foreach Loop (O(n))\n" +
                        "  * Insert at End (O(1))\n" +
                        "  * Insert at Mid (O(n))\n" +
                        "  * Delete (O(n))\n\n" +
                        "More rows lit = more points!",
                    maxPoints       = 100,
                    completionCheck = () =>
                        t_insertedAtEnd && t_insertedAtMid &&
                        t_removed       && t_accessPerformed &&
                        t_linearDone    && t_reverseDone     && t_loopTypeDone
                },
            };

            default:
                return new AssessmentTask[0];
        }
    }

    // -------------------------------------------------------------------------
    // FEEDBACK BANNER
    // -------------------------------------------------------------------------
    void ShowFeedback(bool success, string message)
    {
        if (taskFeedbackBanner == null) return;
        SetActive(taskCardPanel,    true);
        SetActive(taskCollapsedTab, false);
        SetActive(taskFeedbackBanner, true);
        if (taskFeedbackText != null) taskFeedbackText.text = message;

        Image bg = taskFeedbackBanner.GetComponent<Image>();
        if (bg != null)
            bg.color = success
                ? new Color(0.10f, 0.75f, 0.30f, 0.92f)
                : new Color(0.80f, 0.20f, 0.20f, 0.92f);
    }

    // -------------------------------------------------------------------------
    // HELPERS
    // -------------------------------------------------------------------------
    void ResetTaskFlags()
    {
        t_insertedAtEnd     = false;
        t_insertedAtMid     = false;
        t_removed           = false;
        t_accessPerformed   = false;
        t_linearDone        = false;
        t_reverseDone       = false;
        t_loopTypeDone      = false;
        t_elementsInspected = 0;
        t_operationsCount   = 0;
    }

    int CountItems()
    {
        if (arrayController == null) return 0;
        int n = 0;
        for (int i = 0; i < arrayController.arrayCapacity; i++)
            if (arrayController.IsSlotOccupied(i)) n++;
        return n;
    }

    bool IsSceneSpawned()
    {
        return arrayController != null && arrayController.ParkingLot != null;
    }

    void SetActive(GameObject go, bool state)
    {
        if (go != null) go.SetActive(state);
    }
}