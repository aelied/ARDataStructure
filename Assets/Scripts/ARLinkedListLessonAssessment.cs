using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ARLinkedListLessonAssessment.cs — FIXED VERSION (modelled on ARArrayLessonAssessment)
/// =========================================================================================
/// FIXES APPLIED vs previous version:
///   FIX 1  — BeginAssessment grabs listController from lessonGuide if its own
///             slot is null — prevents silent null failures on ShowButtonsForLesson.
///   FIX 2  — ResetFlagForTask resets ONLY the flag the current task needs,
///             not all flags. Prevents later tasks wiping earlier completed work.
///   FIX 3  — OnTaskMinimise hides taskPanel (not just taskCardPanel) so
///             OnTaskRestore actually has something to re-show.
///   FIX 4  — ShowButtonsForLesson has debug logging so you can confirm panels
///             are found at runtime.
///   FIX 5  — L1 auto-demo is now per-task (mirrors ARArrayLessonAssessment
///             L1AutoInspectDemo).
public class ARLinkedListLessonAssessment : MonoBehaviour
{
    [Header("Sibling Scripts")]
    public ARLinkedListLessonGuide lessonGuide;
    public InteractiveTrainList    listController;

    [Header("AR Button Panels (wire these if listController panels are null)")]
    public GameObject mainButtonPanel;
    public GameObject beginnerButtonPanel;
    public GameObject intermediateButtonPanel;

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

    public GameObject      resultsPanel;
    public TextMeshProUGUI resultsTitleText;
    public TextMeshProUGUI resultsScoreText;
    public TextMeshProUGUI resultsGradeText;
    public TextMeshProUGUI resultsBreakdownText;
    public TextMeshProUGUI resultsTipText;
    public Button          retryAssessmentButton;
    public Button          returnButton;
    public Image           gradeRingImage;

    public GameObject taskMinimiseButton;
    public GameObject taskCollapsedTab;
    public GameObject taskCardPanel;

    [Header("Settings")]
    public float  taskTimeLimit    = 60f;
    public bool   showTimer        = true;
    public string mainAppSceneName = "MainScene";

    // ── Internal ──────────────────────────────────────────────────────────────
    struct AssessmentTask
    {
        public string title;
        public string instruction;
        public int    maxPoints;
        public System.Func<bool> completionCheck;
    }

    int              lessonIndex      = -1;
    AssessmentTask[] tasks;
    int              currentTaskIndex = 0;
    int              totalScore       = 0;
    int              maxPossibleScore = 0;
    List<int>        taskScores       = new List<int>();
    List<string>     taskNames        = new List<string>();

    bool  assessmentActive = false;
    float taskTimer        = 0f;
    bool  taskComplete     = false;

    // Action flags — only the flag for the current task is reset between tasks
    bool t_addedHead      = false;
    bool t_addedTail      = false;
    bool t_removedHead    = false;
    bool t_traversed      = false;
    bool t_insertedAt     = false;
    bool t_deletedByValue = false;
    bool t_reversed       = false;
    bool t_foundMiddle    = false;
    int  t_nodesInspected = 0;

    // ─────────────────────────────────────────────────────────────────────────
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

    // FIX 3: hide taskPanel too so Restore has something real to re-show
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

    // ─────────────────────────────────────────────────────────────────────────
    public void BeginAssessment(int lesson)
    {
        Debug.Log($"[LLAssessment] BeginAssessment lesson {lesson}");
        assessmentActive = true;

        // FIX 1: if listController wasn't wired in the Inspector, borrow it
        // from the guide (which is always wired correctly)
        if (listController == null && lessonGuide != null)
            listController = lessonGuide.listController;

        if (assessmentRoot != null) assessmentRoot.SetActive(true);

        // Auto-resolve panels if not wired in Inspector
        if (introPanel   == null) introPanel   = GameObject.Find("AssessmentIntroPanel");
        if (taskPanel    == null) taskPanel    = GameObject.Find("AssessmentTaskPanel");
        if (resultsPanel == null) resultsPanel = GameObject.Find("AssessmentResultsPanel");

        lessonIndex      = lesson;
        tasks            = BuildTasks(lesson);
        maxPossibleScore = 0;
        foreach (var t in tasks) maxPossibleScore += t.maxPoints;

        totalScore       = 0;
        currentTaskIndex = 0;
        taskScores.Clear();
        taskNames.Clear();
        foreach (var t in tasks) taskNames.Add(t.title);

        ClearAllFlags();
        ShowIntroPanel();
    }

    // ─────────────────────────────────────────────────────────────────────────
    void ShowIntroPanel()
    {
        // FIX 7: canvas setup mirrors array version
        if (assessmentCanvas != null)
        {
            assessmentCanvas.gameObject.SetActive(true);
            assessmentCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            assessmentCanvas.sortingOrder = 100;
            var sc = assessmentCanvas.GetComponent<CanvasScaler>();
            if (sc != null)
            {
                sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                sc.referenceResolution = new Vector2(1080, 1920);
                sc.matchWidthOrHeight  = 0.5f;
            }
        }

        SetActive(introPanel,   true);
        SetActive(taskPanel,    false);
        SetActive(resultsPanel, false);

        if (introPanel == null) { Debug.LogError("[LLAssessment] introPanel NULL"); return; }

        var rt = introPanel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.1f, 0.15f);
            rt.anchorMax = new Vector2(0.9f, 0.85f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        if (introTitleText != null) introTitleText.text = $"Lesson {lessonIndex + 1} Assessment";
        if (introBodyText  != null)
            introBodyText.text =
                "Lesson guide complete!\n\n" +
                "Now show what you've learnt.\n" +
                $"Tasks: {tasks.Length}   Points: {maxPossibleScore}\n\n" +
                "Tap START when ready.";

        // Wire the start button — search in children if not set
        if (startAssessmentButton == null)
            startAssessmentButton = introPanel.GetComponentInChildren<Button>(true);

        if (startAssessmentButton != null)
        {
            startAssessmentButton.onClick.RemoveAllListeners();
            startAssessmentButton.onClick.AddListener(OnStartButtonClicked);
            startAssessmentButton.interactable = true;
            foreach (var cg in startAssessmentButton.GetComponentsInParent<CanvasGroup>())
            { cg.interactable = true; cg.blocksRaycasts = true; }
        }
    }

    void OnStartButtonClicked()
    {
        SetActive(introPanel, false);
        assessmentActive  = true;
        currentTaskIndex  = 0;
        ClearAllFlags();
        StartTask(0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    void StartTask(int index)
    {
        if (index >= tasks.Length) { ShowResults(); return; }

        SetActive(taskCardPanel,      true);
        SetActive(taskCollapsedTab,   false);
        SetActive(taskFeedbackBanner, false);

        taskComplete = false;
        taskTimer    = taskTimeLimit > 0 ? taskTimeLimit : float.MaxValue;

        // FIX 2: reset only the flag this specific task checks
        ResetFlagForTask(lessonIndex, index);

        SetActive(taskPanel, true);
        var task = tasks[index];
        if (taskTitleText       != null) taskTitleText.text       = task.title;
        if (taskInstructionText != null) taskInstructionText.text = task.instruction;
        if (taskProgressText    != null) taskProgressText.text    = $"Task {index + 1} / {tasks.Length}";
        if (taskProgressBarFill != null) taskProgressBarFill.fillAmount = (float)(index + 1) / tasks.Length;
        if (taskTimerText       != null) taskTimerText.gameObject.SetActive(showTimer && taskTimeLimit > 0);

        ShowButtonsForLesson(lessonIndex, index);

        // L1: start a fresh per-task demo for tasks 1 and 2.
        // Task 0 (Place Scene) is purely a placement check — no demo needed.
        if (lessonIndex == 0 && index >= 1)
            StartCoroutine(L1AutoDemo(index));

        StartCoroutine(RunTaskLoop(index));
    }

    // ─────────────────────────────────────────────────────────────────────────
    void ShowButtonsForLesson(int lesson, int taskIndex)
    {
        // Resolve panel refs — check in priority order:
        // 1. Assessment's own directly-wired fields
        // 2. listController's fields
        // 3. lessonGuide's fields
        GameObject mainPanel = mainButtonPanel
                            ?? listController?.mainButtonPanel
                            ?? lessonGuide?.mainButtonPanel;
        GameObject begPanel  = beginnerButtonPanel
                            ?? listController?.beginnerButtonPanel
                            ?? lessonGuide?.beginnerButtonPanel;
        GameObject intPanel  = intermediateButtonPanel
                            ?? listController?.intermediateButtonPanel
                            ?? lessonGuide?.intermediateButtonPanel;

        if (mainPanel == null && begPanel == null)
        {
            Debug.LogError("[LLAssessment] ShowButtonsForLesson: ALL panel refs null. " +
                           "Wire mainButtonPanel / beginnerButtonPanel in the Inspector " +
                           "on this Assessment or the LessonGuide component.");
            return;
        }

        bool main         = false;
        bool beginner     = false;
        bool intermediate = false;

        switch (lesson)
        {
            case 0: break; // L1 passive — no buttons
            case 1: main = true; beginner = true; break;  // L2
            case 2: main = true; beginner = true; break;  // L3
            case 3:
                main         = true;
                beginner     = taskIndex <= 4;
                intermediate = taskIndex >= 3 && taskIndex <= 6;
                break;
        }

        Debug.Log($"[LLAssessment] ShowButtons L{lesson} T{taskIndex}: " +
                  $"main={main} beg={beginner} int={intermediate} | " +
                  $"mainPanel={mainPanel?.name ?? "NULL"} " +
                  $"begPanel={begPanel?.name  ?? "NULL"}");

        mainPanel?.SetActive(main);
        begPanel?.SetActive(beginner);
        intPanel?.SetActive(intermediate);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FIX 2: reset ONLY the flag the current task checks, not all flags.
    void ResetFlagForTask(int lesson, int taskIndex)
    {
        switch (lesson)
        {
            case 1:
                if (taskIndex == 1) t_addedHead  = false;
                if (taskIndex == 2) t_addedTail  = false;
                if (taskIndex == 3) t_traversed  = false;
                break;
            case 2:
                if (taskIndex == 1) t_traversed  = false;
                if (taskIndex == 2) t_addedHead  = false;
                if (taskIndex == 3) t_addedTail  = false;
                break;
            case 3:
                if (taskIndex == 0) t_traversed      = false;
                if (taskIndex == 1) t_addedHead      = false;
                if (taskIndex == 2) t_addedTail      = false;
                if (taskIndex == 3) t_insertedAt     = false;
                if (taskIndex == 4) t_removedHead    = false;
                if (taskIndex == 5) t_deletedByValue = false;
                if (taskIndex == 6) t_deletedByValue = false;
                if (taskIndex == 7) { t_reversed = false; t_foundMiddle = false; }
                break;
            // L1 (lesson 0): all passive, no flags to selectively reset
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // L1 AUTO-DEMO — called once per task (index 1 and 2).
    // Mirrors ARArrayLessonAssessment.L1AutoInspectDemo exactly:
    //   • highlights each node's renderer directly (no spawned geometry)
    //   • cyan while visiting → green when done → restored after
    //   • calls NotifyNodeInspected() for each node visited
    //   • loops until the task's required count is reached
    // taskIndex 1 → needs 6 inspections (visits list once or twice)
    // taskIndex 2 → needs 12 inspections (visits list ~2–3 times)
    IEnumerator L1AutoDemo(int taskIndex)
    {
        if (listController == null) yield break;

        int targetCount = taskIndex == 1 ? 6 : 12;
        Debug.Log($"[LLAssessment] L1AutoDemo task={taskIndex} target={targetCount}");

        // Small delay so the task card is readable before demo starts
        yield return new WaitForSeconds(1.5f);

        int len = listController.GetPublicListLength();
        if (len == 0)
        {
            Debug.LogWarning("[LLAssessment] L1AutoDemo: 0 nodes found — granting credit automatically");
            while (t_nodesInspected < targetCount) { NotifyNodeInspected(); yield return null; }
            yield break;
        }

        // Cache original renderer colors so we can restore them
        var originalColors = new Dictionary<Renderer, Color>();

        // Keep looping through the list until we hit the target count
        while (t_nodesInspected < targetCount && assessmentActive)
        {
            for (int i = 0; i < len && assessmentActive && t_nodesInspected < targetCount; i++)
            {
                GameObject nodeObj = listController.GetNodeObjectAt(i);
                if (nodeObj == null) continue;

                // ── Highlight: paint all renderers on this node cyan ──────────
                foreach (Renderer r in nodeObj.GetComponentsInChildren<Renderer>())
                {
                    if (!originalColors.ContainsKey(r))
                        originalColors[r] = r.material.color;
                    r.material.color = Color.cyan;
                }

                // ── Update instruction text to show what's being visited ──────
                if (taskInstructionText != null)
                {
                    string cargo = listController.GetNodeCargoAt(i);
                    taskInstructionText.text =
                        $"Visiting Node [{i}]\n\n" +
                        $"  data  = '{cargo}'\n" +
                        $"  next  → {(i < len - 1 ? $"Node [{i + 1}]" : "NULL")}\n\n" +
                        $"Inspected: {t_nodesInspected + 1} / {targetCount}\n\n" +
                        "This is traversal — following\neach next pointer in order!";
                }

                NotifyNodeInspected();
                yield return new WaitForSeconds(1.2f);

                // ── Visited: turn green briefly ───────────────────────────────
                foreach (Renderer r in nodeObj.GetComponentsInChildren<Renderer>())
                    if (originalColors.ContainsKey(r))
                        r.material.color = Color.green;

                yield return new WaitForSeconds(0.3f);

                // ── Restore original color ────────────────────────────────────
                foreach (Renderer r in nodeObj.GetComponentsInChildren<Renderer>())
                    if (originalColors.ContainsKey(r))
                        r.material.color = originalColors[r];
            }

            // Brief pause between full list passes
            yield return new WaitForSeconds(0.8f);
        }

        // Restore all colors in case anything got stuck highlighted
        foreach (var kv in originalColors)
            if (kv.Key != null) kv.Key.material.color = kv.Value;
        originalColors.Clear();

        Debug.Log($"[LLAssessment] L1AutoDemo task={taskIndex} complete, inspected={t_nodesInspected}");
    }

    // ─────────────────────────────────────────────────────────────────────────
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
                    ShowFeedback(false, "Time's up! Moving on…");
                    yield return new WaitForSeconds(1.5f);
                    break;
                }
            }

            yield return null;
        }

        int  earned = GradeTask(index);
        taskScores.Add(earned);
        totalScore += earned;

        bool passed = earned >= Mathf.CeilToInt(task.maxPoints * 0.6f);
        ShowFeedback(passed,
            passed
                ? $" +{earned} pts — Great work!"
                : $" +{earned}/{task.maxPoints} pts — Keep practising!");

        yield return new WaitForSeconds(2f);
        SetActive(taskFeedbackBanner, false);

        currentTaskIndex++;
        if (currentTaskIndex < tasks.Length) StartTask(currentTaskIndex);
        else                                 ShowResults();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // NOTIFY METHODS — called by ARLinkedListLessonGuide (always, no guard there)
    public void NotifyAddHead()       { t_addedHead      = true; }
    public void NotifyAddTail()       { t_addedTail      = true; }
    public void NotifyRemoveHead()    { t_removedHead    = true; }
    public void NotifyTraverse()      { t_traversed      = true; }
    public void NotifyInsertAt()      { t_insertedAt     = true; }
    public void NotifyDeleteByValue() { t_deletedByValue = true; }
    public void NotifyReverse()       { t_reversed       = true; }
    public void NotifyFindMiddle()    { t_foundMiddle    = true; }
    public void NotifyNodeInspected() { t_nodesInspected++;      }

    // ─────────────────────────────────────────────────────────────────────────
    int GradeTask(int taskIndex)
    {
        if (tasks == null || taskIndex >= tasks.Length) return 0;
        var task = tasks[taskIndex];

        switch (lessonIndex)
        {
            case 0: // L1 — passive
                if (taskIndex == 0) return IsSceneSpawned() ? task.maxPoints : 0;
                if (taskIndex == 1) return Mathf.RoundToInt(task.maxPoints * Mathf.Clamp01(t_nodesInspected / 6f));
                if (taskIndex == 2) return Mathf.RoundToInt(task.maxPoints * Mathf.Clamp01(t_nodesInspected / 12f));
                break;

            case 1: // L2
                if (taskIndex == 0) return IsSceneSpawned() ? task.maxPoints : 0;
                if (taskIndex == 1) return t_addedHead  ? task.maxPoints : 0;
                if (taskIndex == 2) return t_addedTail  ? task.maxPoints : 0;
                if (taskIndex == 3) return t_traversed  ? task.maxPoints : 0;
                break;

            case 2: // L3
                if (taskIndex == 0) return IsSceneSpawned() ? task.maxPoints : 0;
                if (taskIndex == 1) return t_traversed  ? task.maxPoints : 0;
                if (taskIndex == 2) return t_addedHead  ? task.maxPoints : 0;
                if (taskIndex == 3) return t_addedTail  ? task.maxPoints : 0;
                break;

            case 3: // L4
                if (taskIndex == 0) return t_traversed      ? task.maxPoints : 0;
                if (taskIndex == 1) return t_addedHead      ? task.maxPoints : 0;
                if (taskIndex == 2) return t_addedTail      ? task.maxPoints : 0;
                if (taskIndex == 3) return t_insertedAt     ? task.maxPoints : 0;
                if (taskIndex == 4) return t_removedHead    ? task.maxPoints : 0;
                if (taskIndex == 5) return t_deletedByValue ? task.maxPoints : 0;
                if (taskIndex == 6) return t_deletedByValue ? task.maxPoints : 0;
                if (taskIndex == 7)
                {
                    int b = (t_reversed ? 1 : 0) + (t_foundMiddle ? 1 : 0);
                    return Mathf.RoundToInt(task.maxPoints * (b / 2f));
                }
                break;
        }
        return 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    void ShowResults()
    {
        assessmentActive = false;

        // Hide all AR buttons when showing results
        GameObject _main = mainButtonPanel ?? listController?.mainButtonPanel ?? lessonGuide?.mainButtonPanel;
        GameObject _beg  = beginnerButtonPanel ?? listController?.beginnerButtonPanel ?? lessonGuide?.beginnerButtonPanel;
        GameObject _int  = intermediateButtonPanel ?? listController?.intermediateButtonPanel ?? lessonGuide?.intermediateButtonPanel;
        _main?.SetActive(false);
        _beg?.SetActive(false);
        _int?.SetActive(false);

        SetActive(taskPanel,    false);
        SetActive(resultsPanel, true);

        float  pct = maxPossibleScore > 0 ? (float)totalScore / maxPossibleScore : 0f;
        string grade;
        Color  col;

        if      (pct >= 0.90f) { grade = "A";            col = new Color(0.2f, 0.9f, 0.3f); }
        else if (pct >= 0.75f) { grade = "B";            col = new Color(0.4f, 0.7f, 1.0f); }
        else if (pct >= 0.60f) { grade = "C";            col = new Color(1.0f, 0.8f, 0.2f); }
        else                   { grade = "Needs Review"; col = new Color(1.0f, 0.3f, 0.3f); }

        if (resultsTitleText  != null) resultsTitleText.text  = $"Lesson {lessonIndex + 1} Complete!";
        if (resultsScoreText  != null) resultsScoreText.text  = $"{totalScore} / {maxPossibleScore}  ({Mathf.RoundToInt(pct * 100)}%)";
        if (resultsGradeText  != null) { resultsGradeText.text = grade; resultsGradeText.color = col; }
        if (gradeRingImage    != null) { gradeRingImage.fillAmount = pct; gradeRingImage.color = col; }

        string bd = "Task Breakdown:\n";
        for (int i = 0; i < taskScores.Count && i < taskNames.Count; i++)
        {
            int    max  = i < tasks.Length ? tasks[i].maxPoints : 0;
            string tick = taskScores[i] >= Mathf.CeilToInt(max * 0.6f) ? "" : "";
            bd += $"{tick} {taskNames[i]}:  {taskScores[i]}/{max}\n";
        }
        if (resultsBreakdownText != null) resultsBreakdownText.text = bd;
        if (resultsTipText       != null) resultsTipText.text       = BuildTip(lessonIndex, pct);

        PlayerPrefs.SetInt   ($"AR_Assessment_LL_L{lessonIndex}_Score", totalScore);
        PlayerPrefs.SetInt   ($"AR_Assessment_LL_L{lessonIndex}_Max",   maxPossibleScore);
        PlayerPrefs.SetString($"AR_Assessment_LL_L{lessonIndex}_Grade", grade);
        PlayerPrefs.Save();
    }

    string BuildTip(int lesson, float pct)
    {
        if (pct >= 0.9f) return "Excellent! You have a strong grasp of this lesson.";
        switch (lesson)
        {
            case 0: return "Review: each node holds data + a next pointer. Nodes are NOT stored contiguously!";
            case 1: return "Review: HEAD = first node entry point. TAIL.next = NULL. NULL marks the end.";
            case 2: return "Review: SLL (forward only), DLL (bidirectional), CSLL/CDLL (circular variants).";
            case 3: return "Review: Insert/Delete at HEAD = O(1). All other positions = O(n) traversal first.";
            default: return "Keep practising to reinforce your understanding!";
        }
    }

    // FIX 6: retry calls OnStartButtonClicked so flags reset properly
    void RetryAssessment()
    {
        SetActive(resultsPanel, false);
        totalScore       = 0;
        currentTaskIndex = 0;
        taskScores.Clear();
        ClearAllFlags();
        OnStartButtonClicked();
    }

    void OnReturnClicked()
    {
        HideAssessment();
        PlayerPrefs.SetInt(ARReturnHandler.AR_JUST_RETURNED_KEY, 1);
        PlayerPrefs.SetString(ARReturnHandler.AR_TOPIC_NAME_KEY,
            PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "linkedlist"));
        PlayerPrefs.Save();
        SceneManager.LoadScene(PlayerPrefs.GetString("AR_MainScene", mainAppSceneName));
    }

    // ─────────────────────────────────────────────────────────────────────────
    AssessmentTask[] BuildTasks(int lesson)
    {
        switch (lesson)
        {
            // =================================================================
            case 0: return new AssessmentTask[]    // L1 — Introduction (passive)
            {
                new AssessmentTask {
                    title       = "Place the Scene",
                    instruction = "Point your camera at a flat surface\nand tap to place the scene.\n\n" +
                                  "A Linked List stores nodes in\nNON-CONTIGUOUS memory\nconnected by POINTERS.\n\n" +
                                  "Waiting for placement…",
                    maxPoints       = 25,
                    completionCheck = () => IsSceneSpawned()
                },
                new AssessmentTask {
                    title       = "Observe Node Structure",
                    instruction = "Watch the demo highlight each node.\n\n" +
                                  "Each node contains:\n  DATA  — the stored value\n  NEXT  — pointer to next node\n\n" +
                                  "The last node's NEXT → NULL\n\nAuto-demo running…",
                    maxPoints       = 40,
                    completionCheck = () => t_nodesInspected >= 6
                },
                new AssessmentTask {
                    title       = "Understand the Full Chain",
                    instruction = "Watch the full traversal demo.\n\n" +
                                  "Following next pointers from HEAD\nto NULL is called TRAVERSAL.\n\n" +
                                  "Array access → O(1) direct jump\nLL traversal  → O(n) follow chain\n\n" +
                                  "Auto-demo running…",
                    maxPoints       = 35,
                    completionCheck = () => t_nodesInspected >= 12
                },
            };

            // =================================================================
            case 1: return new AssessmentTask[]    // L2 — Terminologies
            {
                new AssessmentTask {
                    title       = "Place the Scene",
                    instruction = "Tap a flat surface to place\nthe linked list scene.\n\n" +
                                  "You will identify HEAD, TAIL\nand NULL by performing actions.",
                    maxPoints       = 20,
                    completionCheck = () => IsSceneSpawned()
                },
                new AssessmentTask {
                    title       = "Demonstrate HEAD",
                    instruction = "HEAD = the FIRST node.\nAll traversals start here.\n\n" +
                                  "Tap ADD HEAD to insert a node\nat the FRONT of the list.\n\n" +
                                  "Watch the orange HEAD label\nmove to your new node!",
                    maxPoints       = 25,
                    completionCheck = () => t_addedHead
                },
                new AssessmentTask {
                    title       = "Demonstrate TAIL",
                    instruction = "TAIL = the LAST node.\nIts NEXT pointer = NULL.\n\n" +
                                  "Tap ADD TAIL to insert a node\nat the END of the list.\n\n" +
                                  "The new node's connector\npoints to NULL.",
                    maxPoints       = 25,
                    completionCheck = () => t_addedTail
                },
                new AssessmentTask {
                    title       = "Observe NULL — Traverse to End",
                    instruction = "NULL marks the END of the list.\nTraversal stops when next == NULL.\n\n" +
                                  "Tap TRAVERSE to walk the list\nfrom HEAD until TAIL (next=NULL).\n\nTime: O(n)",
                    maxPoints       = 30,
                    completionCheck = () => t_traversed
                },
            };

            // =================================================================
            case 2: return new AssessmentTask[]    // L3 — Types
            {
                new AssessmentTask {
                    title       = "Place the Scene",
                    instruction = "Tap a flat surface to place\nthe scene.\n\n" +
                                  "You will demonstrate pointer\nbehaviour for different LL types.",
                    maxPoints       = 15,
                    completionCheck = () => IsSceneSpawned()
                },
                new AssessmentTask {
                    title       = "SLL — Forward Traversal Only",
                    instruction = "Singly Linked List (SLL):\nEach node has ONE next pointer.\nTraversal: forward only.\n\n" +
                                  "Tap TRAVERSE to demonstrate\none-directional movement.\n\nTime: O(n)",
                    maxPoints       = 25,
                    completionCheck = () => t_traversed
                },
                new AssessmentTask {
                    title       = "O(1) Head Insert — Any LL Type",
                    instruction = "In ALL linked list types:\n  new_node.next = old_head\n  head = new_node\n\n" +
                                  "Time: O(1) — no traversal!\n\nTap ADD HEAD to demonstrate.",
                    maxPoints       = 25,
                    completionCheck = () => t_addedHead
                },
                new AssessmentTask {
                    title       = "SLL Tail Insert — O(n)",
                    instruction = "SLL without tail pointer:\n  Insert at tail = O(n)\n\n" +
                                  "DLL with tail pointer:\n  Insert at tail = O(1)\n\n" +
                                  "Tap ADD TAIL to see the\nO(n) traversal cost.",
                    maxPoints       = 35,
                    completionCheck = () => t_addedTail
                },
            };

            // =================================================================
            case 3: return new AssessmentTask[]    // L4 — Operations
            {
                new AssessmentTask {
                    title       = "1. Traversal  O(n)",
                    instruction = "Visit each node from HEAD to NULL.\n\n" +
                                  "  current = head\n  while current is not None:\n      print(current.data)\n      current = current.next\n\n" +
                                  "Time: O(n)   Space: O(1)\n\n→ Tap TRAVERSE.",
                    maxPoints       = 12,
                    completionCheck = () => t_traversed
                },
                new AssessmentTask {
                    title       = "2A. Insert at Beginning  O(1)",
                    instruction = "  1. new_node.next = head\n  2. head = new_node\n\n" +
                                  "Time: O(1) — no traversal!\nLL advantage over arrays.\n\n→ Tap ADD HEAD.",
                    maxPoints       = 12,
                    completionCheck = () => t_addedHead
                },
                new AssessmentTask {
                    title       = "2B. Insert at End  O(n)",
                    instruction = "  1. Traverse to last node\n  2. last_node.next = new_node\n\n" +
                                  "Time: O(n) without tail pointer.\nO(1) if tail pointer maintained.\n\n→ Tap ADD TAIL.",
                    maxPoints       = 12,
                    completionCheck = () => t_addedTail
                },
                new AssessmentTask {
                    title       = "2C. Insert at Position  O(n)",
                    instruction = "  1. Traverse to position − 1\n  2. new_node.next = current.next\n  3. current.next = new_node\n\n" +
                                  "Time: O(n)  No element shifting!\n\n→ INTERMEDIATE → INSERT AT.",
                    maxPoints       = 12,
                    completionCheck = () => t_insertedAt
                },
                new AssessmentTask {
                    title       = "3A. Delete from Beginning  O(1)",
                    instruction = "  head = head.next\n\n" +
                                  "Time: O(1) — fastest deletion!\nArrays need O(n) for same op.\n\n→ Tap REMOVE HEAD.",
                    maxPoints       = 12,
                    completionCheck = () => t_removedHead
                },
                new AssessmentTask {
                    title       = "3D. Delete First Occurrence  O(n)",
                    instruction = "Find and remove first match:\n  1. Search for the key\n  2. prev.next = current.next\n\n" +
                                  "Time: O(n) — search + re-link.\n\n→ INTERMEDIATE → DELETE BY VALUE.",
                    maxPoints       = 12,
                    completionCheck = () => t_deletedByValue
                },
                new AssessmentTask {
                    title       = "4. Searching  O(n)",
                    instruction = "Linear search — the ONLY option!\nStart at head, compare each node.\nStop when found or NULL reached.\n\n" +
                                  " Binary Search NOT possible —\n  cannot access middle directly!\n\n→ Do another DELETE BY VALUE.",
                    maxPoints       = 13,
                    completionCheck = () => t_deletedByValue
                },
                new AssessmentTask {
                    title       = "Bonus: Reverse + Find Middle",
                    instruction = "REVERSE  O(n)\n  Three-pointer technique.\n  Flips all next pointers.\n\n" +
                                  "FIND MIDDLE  O(n)\n  Floyd's two-pointer:\n  Slow=1 step, Fast=2 steps.\n\nDo BOTH for full points!",
                    maxPoints       = 15,
                    completionCheck = () => t_reversed && t_foundMiddle
                },
            };

            default: return new AssessmentTask[0];
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    void ShowFeedback(bool success, string msg)
    {
        if (taskFeedbackBanner == null) return;
        SetActive(taskCardPanel,      true);
        SetActive(taskCollapsedTab,   false);
        SetActive(taskFeedbackBanner, true);
        if (taskFeedbackText != null) taskFeedbackText.text = msg;
        Image bg = taskFeedbackBanner.GetComponent<Image>();
        if (bg != null)
            bg.color = success
                ? new Color(0.10f, 0.75f, 0.30f, 0.92f)
                : new Color(0.80f, 0.20f, 0.20f, 0.92f);
    }

    void ClearAllFlags()
    {
        t_addedHead = t_addedTail = t_removedHead = t_traversed =
        t_insertedAt = t_deletedByValue = t_reversed = t_foundMiddle = false;
        t_nodesInspected = 0;
    }

    bool IsSceneSpawned() => GameObject.Find("LinkedListScene") != null;
    void SetActive(GameObject go, bool s) { if (go != null) go.SetActive(s); }
}