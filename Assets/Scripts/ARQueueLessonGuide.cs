using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ARQueueLessonGuide.cs — SYLLABUS-ALIGNED VERSION
/// ==================================================
/// UI SYNC FIX (mirrors ARArrayLessonGuide exactly):
///   SyncControllerUI(stepIndex) is called on every ShowStep() and again
///   after OnSceneSpawned(). It sets the controller's instructionText /
///   operationInfoText / statusText / detectionText to lesson-relevant
///   content, or hides the root panel when the guide card already covers it.
///   GetPanelRoot() walks two levels up (Text -> Panel -> RootPanel) so the
///   entire card — background, border, children — is hidden, not just text.
///
/// Lesson -> guideMode map:
///   L1 (index 0) -> mode 1   Introduction / FIFO
///   L2 (index 1) -> mode 2   Operations
///   L3 (index 2) -> mode 3   Implementation
///   L4 (index 3) -> mode 4   Applications
///   L5 (index 4) -> mode 5   Complexity Summary
/// </summary>
public class ARQueueLessonGuide : MonoBehaviour
{
    [Header("Your Existing Queue Controller")]
    public InteractiveCoffeeQueue queueController;

    [Header("Existing Canvas Panels")]
    public GameObject beginnerButtonPanel;
    public GameObject intermediateButtonPanel;
    public GameObject movementControlPanel;
    public GameObject confirmButton;

    [Header("Guide Canvas (QueueGuideCanvas)")]
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
    public ARQueueLessonAssessment lessonAssessment;

    [Header("Operation Feedback Panel")]
    public GameObject      opFeedbackPanel;
    public TextMeshProUGUI opFeedbackTitle;
    public TextMeshProUGUI opFeedbackBody;
    public TextMeshProUGUI opComplexityBadge;
    public TextMeshProUGUI operationLogText;

    [Header("Toast Notifications")]
    public GameObject      toastPanel;
    public TextMeshProUGUI toastText;
    public Image           toastIcon;
    public Sprite          checkSprite;
    public Sprite          crossSprite;

    [Header("Complexity Table")]
    public GameObject      complexityTablePanel;
    public TextMeshProUGUI complexityTableText;
    public TextMeshProUGUI activeOperationLabel;

    [Header("Queue Inspect Card")]
    public GameObject      queueInspectCard;
    public TextMeshProUGUI inspectOperationText;
    public TextMeshProUGUI inspectQueueSizeText;
    public TextMeshProUGUI inspectFrontElementText;
    public TextMeshProUGUI inspectComplexityText;
    public Button          inspectCloseButton;

    [Header("Settings")]
    public string mainAppSceneName = "MainScene";

    // ── State ─────────────────────────────────────────────────────────────────
    int  lessonIndex       = -1;
    int  guideMode         = -1;
    int  currentStep       = 0;
    bool nextBlocked       = false;
    bool sceneSpawned      = false;
    bool assessmentStarted = false;

    int  lastQueueCount  = 0;
    bool didEnqueue      = false;
    bool didDequeue      = false;
    bool didPeek         = false;
    bool didMultiDequeue = false;
    bool didReverse      = false;

    readonly List<string> opLog = new List<string>();

    struct Step { public string title, body; public bool waitForAction; }
    Step[] steps;

    // ── Guards ────────────────────────────────────────────────────────────────
    private bool _started        = false;
    private bool _guideInited    = false;
    private bool _nextOnCooldown = false;

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC: Called by ARModeSelectionManager before re-enabling this component
    // ─────────────────────────────────────────────────────────────────────────
    public void ResetInitFlag()
    {
        _guideInited = false;
        _started     = false;
        Debug.Log("[ARQueueLessonGuide] ResetInitFlag — ready for fresh init");
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (_started) return;
        _started = true;

        string topic = PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "");
        lessonIndex  = PlayerPrefs.GetInt(ARReturnHandler.AR_LESSON_INDEX_KEY, -1);

        Debug.Log($"[ARQueueLessonGuide] Start — topic='{topic}' lessonIndex={lessonIndex}");

        if (!topic.ToLower().Contains("queue") || lessonIndex < 0)
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
            lessonAssessment = GetComponent<ARQueueLessonAssessment>();

        Debug.Log($"[ARQueueLessonGuide] Lesson {lessonIndex} -> Mode {guideMode} | Assessment: {(lessonAssessment != null ? "FOUND" : "NULL")}");
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

    // ─────────────────────────────────────────────────────────────────────────
    // InitGuide — called via SendMessage from ARModeSelectionManager ONLY
    // ─────────────────────────────────────────────────────────────────────────
    void InitGuide()
    {
        if (guideCanvas != null) guideCanvas.gameObject.SetActive(true);
        if (_guideInited) return;
        _guideInited = true;

        queueInspectCard?.SetActive(false);
        opFeedbackPanel?.SetActive(false);
        toastPanel?.SetActive(false);
        complexityTablePanel?.SetActive(false);
        collapsedTab?.SetActive(false);
        returnButton?.gameObject.SetActive(false);
        guideCardPanel?.SetActive(true);

        nextStepButton?.onClick.AddListener(OnNext);
        returnButton?.onClick.AddListener(OnReturn);
        minimiseButton?.onClick.AddListener(OnMinimise);
        restoreButton?.onClick.AddListener(OnRestore);
        inspectCloseButton?.onClick.AddListener(() => queueInspectCard?.SetActive(false));

        ResetOperationFlags();

        steps = BuildSteps(guideMode);
        nextBlocked = true;
        ShowStep(0);
    }

    void ResetOperationFlags()
    {
        didEnqueue      = false;
        didDequeue      = false;
        didPeek         = false;
        didMultiDequeue = false;
        didReverse      = false;
        opLog.Clear();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!enabled) return;

        if (!sceneSpawned && queueController != null && queueController.IsReady())
        {
            sceneSpawned = true;
            OnSceneSpawned();
        }

        if (sceneSpawned && guideMode == 1 && !assessmentStarted)
            EnsureActionButtonsHidden();
    }

    void OnSceneSpawned()
    {
        Debug.Log($"[ARQueueLessonGuide] Queue scene detected — guideMode={guideMode} currentStep={currentStep}");

        if (guideMode == 5) { complexityTablePanel?.SetActive(true); UpdateComplexityTable(""); }

        if (guideMode == 1 && queueController != null)
            queueController.PreFillForLesson(4);

        nextBlocked = false;
        UpdateNextButton();
        lastQueueCount = GetCurrentQueueCount();

        if (guideMode >= 2 && currentStep >= 1)
        {
            bool showBeginner     = guideMode <= 3;
            bool showIntermediate = guideMode >= 4;
            beginnerButtonPanel?.SetActive(showBeginner);
            intermediateButtonPanel?.SetActive(showIntermediate);
        }

        // Re-sync controller UI now that scene is placed
        SyncControllerUI(currentStep);
    }

    void EnsureActionButtonsHidden()
    {
        HideGO(beginnerButtonPanel);
        HideGO(intermediateButtonPanel);
        HideGO(movementControlPanel);
        HideGO(confirmButton);
    }

    void HideGO(GameObject go) { if (go != null && go.activeSelf) go.SetActive(false); }
    void ShowGO(GameObject go) { if (go != null) go.SetActive(true); }

    // =========================================================================
    // CONTROLLER UI SYNC
    // Called every ShowStep() and again after OnSceneSpawned().
    // Sets instructionText / operationInfoText / statusText / detectionText
    // to lesson-relevant text, or hides root panel when guide card covers it.
    // =========================================================================
    void SyncControllerUI(int stepIndex)
    {
        if (queueController == null) return;

        // detectionText panel: only useful before scene is placed
        if (sceneSpawned)
            HideControllerPanel(queueController.detectionText);
        else
            ShowControllerPanel(queueController.detectionText);

        switch (guideMode)
        {
            // L1 - Introduction: scene pre-filled, no operations allowed.
            // instructionText -> step-matched hint about FIFO.
            // operationInfoText -> hidden (guide card covers it).
            // statusText -> hidden (no operations tracked).
            case 1:
            {
                string hint = stepIndex switch
                {
                    0 => "Point your camera at a flat surface and tap to place the queue scene.",
                    1 => "Notice the FRONT and REAR markers — items enter at rear, leave from front.",
                    2 => "Only FRONT and REAR access is allowed. No random access in a queue!",
                    3 => "Watch the FIFO order: the first item in is always the first out.",
                    4 => "A queue differs from a stack: queues preserve arrival order (FIFO), stacks reverse it (LIFO).",
                    _ => "Lesson 1 complete. Assessment starting shortly..."
                };
                SetControllerText(queueController.instructionText, hint);
                HideControllerPanel(queueController.operationInfoText);
                HideControllerPanel(queueController.statusText);
                break;
            }

            // L2 - Operations: full enqueue / dequeue / peek.
            // instructionText -> which button to tap next.
            // operationInfoText -> shown (controller writes complexity after each op).
            // statusText -> shown (queue size count).
            case 2:
            {
                string hint = stepIndex switch
                {
                    0 => "Point your camera at a flat surface and tap to place the queue scene.",
                    1 => "Tap  ENQUEUE  to add an item to the rear of the queue.",
                    2 => "Tap  DEQUEUE  to remove the front item from the queue.",
                    3 => "Tap  PEEK  to view the front item without removing it.",
                    _ => "All operations done. Assessment starting shortly..."
                };
                SetControllerText(queueController.instructionText, hint);
                ShowControllerPanel(queueController.operationInfoText);
                ShowControllerPanel(queueController.statusText);
                break;
            }

            // L3 - Implementation: enqueue and dequeue to compare array vs LL.
            // operationInfoText -> shown (implementation notes per op).
            // statusText -> shown.
            case 3:
            {
                string hint = stepIndex switch
                {
                    0 => "Point your camera at a flat surface and tap to place the queue scene.",
                    1 => "Tap  ENQUEUE  to simulate: rear = (rear+1) % size  |  queue[rear] = value",
                    2 => "Tap  DEQUEUE  to simulate: value = queue[front]  |  front = (front+1) % size",
                    3 => "Both array and linked list give O(1) for enqueue and dequeue.",
                    _ => "Implementation comparison done. Assessment starting shortly..."
                };
                SetControllerText(queueController.instructionText, hint);
                ShowControllerPanel(queueController.operationInfoText);
                ShowControllerPanel(queueController.statusText);
                break;
            }

            // L4 - Applications: explore real-world queue use cases.
            // operationInfoText -> shown (toast feedback replaces it per op).
            // statusText -> shown.
            case 4:
            {
                string hint = stepIndex switch
                {
                    0 => "Point your camera at a flat surface and tap to place the queue scene.",
                    1 => "Tap  ENQUEUE  to simulate a process joining the CPU ready queue.",
                    2 => "Tap  DEQUEUE  to simulate the CPU serving the next process in order.",
                    3 => "Tap  ENQUEUE  then  DEQUEUE  to simulate network packet buffering.",
                    _ => "Applications explored. Assessment starting shortly..."
                };
                SetControllerText(queueController.instructionText, hint);
                ShowControllerPanel(queueController.operationInfoText);
                ShowControllerPanel(queueController.statusText);
                break;
            }

            // L5 - Complexity: full operations, complexity table is the star.
            // operationInfoText -> hidden (table replaces it).
            // statusText -> shown.
            case 5:
            {
                string hint = stepIndex switch
                {
                    0 => "Perform any operation — watch its row light up in the Big-O table.",
                    1 => "Try  ENQUEUE,  DEQUEUE,  and  PEEK  to see O(1) rows light up.",
                    2 => "Use INTERMEDIATE mode for Priority Enqueue to see O(n) in action.",
                    _ => "Complexity table complete. Assessment starting shortly..."
                };
                SetControllerText(queueController.instructionText, hint);
                HideControllerPanel(queueController.operationInfoText);
                ShowControllerPanel(queueController.statusText);
                break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PANEL ROOT HELPERS
    // Hierarchy: RootPanel > Panel > Text  (two levels up from the TMP).
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the root panel that should be shown/hidden for a given TMP label.
    /// Walks two levels up from the TMP component.
    /// Falls back gracefully if hierarchy is shallower.
    /// </summary>
    GameObject GetPanelRoot(TextMeshProUGUI tmp)
    {
        if (tmp == null) return null;
        Transform t = tmp.transform.parent;          // Panel
        if (t != null && t.parent != null)
            return t.parent.gameObject;              // RootPanel
        if (t != null)
            return t.gameObject;                     // Panel (fallback)
        return tmp.gameObject;                       // Text itself (last resort)
    }

    /// <summary>Shows the root panel and sets the TMP text. No-ops if null.</summary>
    void SetControllerText(TextMeshProUGUI tmp, string text)
    {
        if (tmp == null) return;
        GetPanelRoot(tmp)?.SetActive(true);
        tmp.text  = text;
        tmp.color = Color.white;
    }

    /// <summary>Shows the root panel that contains the given TMP label.</summary>
    void ShowControllerPanel(TextMeshProUGUI tmp)
    {
        GetPanelRoot(tmp)?.SetActive(true);
    }

    /// <summary>Hides the root panel that contains the given TMP label.</summary>
    void HideControllerPanel(TextMeshProUGUI tmp)
    {
        var root = GetPanelRoot(tmp);
        if (root != null && root.activeSelf) root.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OPERATION CALLBACKS (called by InteractiveCoffeeQueue)
    // ─────────────────────────────────────────────────────────────────────────
    public void OnEnqueueConfirmed()
    {
        Debug.Log($"[QueueGuide] OnEnqueueConfirmed — didEnqueue={didEnqueue}, step={currentStep}");
        lastQueueCount = GetCurrentQueueCount();
        OnOperationDetected("ENQUEUE", true);
        if (!didEnqueue) { didEnqueue = true; TryUnblock(); }

        // Update instructionText live after enqueue
        UpdateInstructionAfterOp("ENQUEUE");
    }

    public void OnDequeueConfirmed()
    {
        lastQueueCount = GetCurrentQueueCount();
        OnOperationDetected("DEQUEUE", true);
        if (!didDequeue) { didDequeue = true; TryUnblock(); }

        UpdateInstructionAfterOp("DEQUEUE");
    }

    int GetCurrentQueueCount()
    {
        if (queueController == null) return 0;
        return queueController.GetQueueSize();
    }

    public void NotifyPeekPerformed()
    {
        if (!didPeek) { didPeek = true; TryUnblock(); }
        ShowQueueInspectCard("PEEK", "O(1) — Read front without removing");

        if (guideMode == 4)
            ShowToast(checkSprite, "PEEK: Viewed front element — O(1), non-destructive!");
        else if (guideMode == 5)
            UpdateComplexityTable("PEEK");

        // Update instructionText after peek
        if (queueController?.instructionText != null)
        {
            GetPanelRoot(queueController.instructionText)?.SetActive(true);
            queueController.instructionText.text  = "PEEK done - queue unchanged! O(1) read-only access. Tap  Next  when ready.";
            queueController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyPriorityEnqueuePerformed(int priority, int insertIndex)
    {
        if (guideMode == 4)
        {
            ShowToast(checkSprite, $"PRIORITY ENQUEUE: P{priority} inserted at [{insertIndex}] — O(n)");
        }
        else if (guideMode == 5)
        {
            UpdateComplexityTable("PRIORITY_ENQUEUE");
        }

        // Update instructionText with result
        if (queueController?.instructionText != null)
        {
            GetPanelRoot(queueController.instructionText)?.SetActive(true);
            queueController.instructionText.text  = $"Priority {priority} inserted at [{insertIndex}] — O(n) because items shifted to maintain order.";
            queueController.instructionText.color = new Color(1f, 0.8f, 0.2f);
        }

        Debug.Log($"[ARQueueLessonGuide] NotifyPriorityEnqueuePerformed priority={priority} index={insertIndex}");
    }

    public void NotifyMultiDequeuePerformed(int count)
    {
        if (!didMultiDequeue) { didMultiDequeue = true; TryUnblock(); }
        if (guideMode == 5)
        {
            UpdateComplexityTable("MULTIDEQUEUE");
            if (queueController?.instructionText != null)
            {
                GetPanelRoot(queueController.instructionText)?.SetActive(true);
                queueController.instructionText.text  = $"Multi-Dequeue({count}) logged — O(n). Removing n items costs linear time.";
                queueController.instructionText.color = new Color(1f, 0.6f, 0.2f);
            }
        }
    }

    public void NotifyReversePerformed()
    {
        if (!didReverse) { didReverse = true; TryUnblock(); }
        if (guideMode == 5)
        {
            UpdateComplexityTable("REVERSE");
            if (queueController?.instructionText != null)
            {
                GetPanelRoot(queueController.instructionText)?.SetActive(true);
                queueController.instructionText.text  = "Reverse logged — O(n). Must visit every element to flip the order.";
                queueController.instructionText.color = new Color(1f, 0.6f, 0.2f);
            }
        }
    }

    /// <summary>
    /// After an operation fires, update instructionText to tell the student
    /// what to try next — mirrors UpdateInstructionAfterOp in ARArrayLessonGuide.
    /// </summary>
    void UpdateInstructionAfterOp(string op)
    {
        if (queueController?.instructionText == null) return;

        string next;

        if (guideMode == 2) // L2 Operations
        {
            next = op switch
            {
                "ENQUEUE" => "ENQUEUE done - O(1)!  Now tap  DEQUEUE  to remove the front item.",
                "DEQUEUE" => "DEQUEUE done - O(1)!  Now tap  PEEK  to view the front without removing.",
                _         => "All three operations done.  Tap  Next  when ready."
            };
        }
        else if (guideMode == 3) // L3 Implementation
        {
            next = op switch
            {
                "ENQUEUE" => "Array: queue[++rear] = value  |  LL: tail.next = new_node  ->  both O(1). Now tap  DEQUEUE.",
                "DEQUEUE" => "Array: return queue[front++]  |  LL: head = head.next  ->  both O(1). Tap  Next  when ready.",
                _         => "Operations compared. Tap  Next  when ready."
            };
        }
        else if (guideMode == 4) // L4 Applications
        {
            next = op switch
            {
                "ENQUEUE" => "Process enqueued to ready queue. Now  DEQUEUE  to simulate the CPU serving it.",
                "DEQUEUE" => "Process served! Next process moves to front — FIFO scheduling in action. Try another  ENQUEUE.",
                _         => "Keep trying operations to explore applications."
            };
        }
        else if (guideMode == 5) // L5 Complexity
        {
            next = $"{op} logged in the table!  Try another operation to light up more rows.";
        }
        else
        {
            return;
        }

        GetPanelRoot(queueController.instructionText)?.SetActive(true);
        queueController.instructionText.text  = next;
        queueController.instructionText.color = new Color(0.4f, 1f, 0.5f);
    }

    void OnOperationDetected(string op, bool success)
    {
        string complexity = op switch
        {
            "ENQUEUE" => "O(1) — insert at rear",
            "DEQUEUE" => "O(1) — remove from front",
            _         => "O(1)"
        };

        if (guideMode == 2)
        {
            opFeedbackPanel?.SetActive(true);
            if (opFeedbackTitle   != null) opFeedbackTitle.text   = op;
            if (opFeedbackBody    != null) opFeedbackBody.text    = OpExplanation(op);
            if (opComplexityBadge != null) opComplexityBadge.text = complexity;
            opLog.Insert(0, $"{op}  {complexity}");
            if (opLog.Count > 4) opLog.RemoveAt(4);
            if (operationLogText  != null) operationLogText.text  = string.Join("\n", opLog);
            StartCoroutine(HideAfter(opFeedbackPanel, 3.5f));
            ShowQueueInspectCard(op, complexity);
        }
        else if (guideMode == 3)
        {
            opFeedbackPanel?.SetActive(true);
            if (opFeedbackTitle   != null) opFeedbackTitle.text   = op;
            if (opFeedbackBody    != null) opFeedbackBody.text    = ImplementationNote(op);
            if (opComplexityBadge != null) opComplexityBadge.text = complexity;
            StartCoroutine(HideAfter(opFeedbackPanel, 3.5f));
        }
        else if (guideMode == 4)
        {
            string msg = op switch
            {
                "ENQUEUE" => "ENQUEUE: Added to rear — like joining the back of a line!",
                "DEQUEUE" => "DEQUEUE: Removed from front — like serving the first customer!",
                _         => "Operation detected"
            };
            ShowToast(success ? checkSprite : crossSprite, msg);
        }
        else if (guideMode == 5)
        {
            UpdateComplexityTable(op);
        }
    }

    string OpExplanation(string op)
    {
        switch (op)
        {
            case "ENQUEUE":
                return "ENQUEUE — Insert at REAR:\n" +
                       "1. Check if queue is full\n" +
                       "2. Increment rear pointer\n" +
                       "3. Place element at rear\n\n" +
                       "Time: O(1) — Always instant!";
            case "DEQUEUE":
                return "DEQUEUE — Remove from FRONT:\n" +
                       "1. Check if queue is empty\n" +
                       "2. Return front element\n" +
                       "3. Increment front pointer\n\n" +
                       "Time: O(1) — Always instant!";
            case "PEEK":
                return "PEEK — View FRONT:\n" +
                       "1. Check if queue is empty\n" +
                       "2. Return queue[front]\n" +
                       "   (without removing)\n\n" +
                       "Time: O(1) — Read-only!";
            default:
                return "Queue operation detected.";
        }
    }

    string ImplementationNote(string op)
    {
        switch (op)
        {
            case "ENQUEUE":
                return "Array Implementation:\n" +
                       "  rear = (rear+1) % size\n" +
                       "  queue[rear] = value\n\n" +
                       "Linked List Implementation:\n" +
                       "  new_node.next = null\n" +
                       "  tail.next = new_node\n" +
                       "  tail = new_node\n\n" +
                       "Both: O(1)";
            case "DEQUEUE":
                return "Array Implementation:\n" +
                       "  value = queue[front]\n" +
                       "  front = (front+1) % size\n\n" +
                       "Linked List Implementation:\n" +
                       "  value = head.data\n" +
                       "  head = head.next\n\n" +
                       "Both: O(1)";
            default:
                return OpExplanation(op);
        }
    }

    void ShowQueueInspectCard(string op, string complexity)
    {
        if (queueInspectCard == null) return;
        queueInspectCard.SetActive(true);
        if (inspectOperationText    != null) inspectOperationText.text    = $"Operation: {op}";
        if (inspectQueueSizeText    != null) inspectQueueSizeText.text    = $"Queue Size: {GetCurrentQueueCount()}";
        if (inspectFrontElementText != null) inspectFrontElementText.text = "Front Index: 0";
        if (inspectComplexityText   != null) inspectComplexityText.text   = $"Time: {complexity}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // STEP MANAGEMENT
    // ─────────────────────────────────────────────────────────────────────────
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

        if (guideMode >= 2 && sceneSpawned)
        {
            bool showBeginner     = index >= 1 && guideMode <= 3;
            bool showIntermediate = index >= 1 && guideMode >= 4;
            beginnerButtonPanel?.SetActive(showBeginner);
            intermediateButtonPanel?.SetActive(showIntermediate);
        }

        nextBlocked = s.waitForAction;
        if (nextBlocked) CheckIfAlreadyDone(index);

        if (!nextBlocked)
            StartCoroutine(EnableNextAfterDelay(0.6f));
        else
            UpdateNextButton();

        // ── Sync controller UI to this step ───────────────────────────────────
        SyncControllerUI(index);

        Debug.Log($"[QueueGuide] ShowStep {index}, isLast={isLast}, assessmentStarted={assessmentStarted}");

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
        opFeedbackPanel?.SetActive(false);
        complexityTablePanel?.SetActive(false);
        toastPanel?.SetActive(false);
        queueInspectCard?.SetActive(false);

        if (lesson <= 2)
        {
            beginnerButtonPanel?.SetActive(true);
            intermediateButtonPanel?.SetActive(false);
        }
        else
        {
            beginnerButtonPanel?.SetActive(false);
            intermediateButtonPanel?.SetActive(true);
        }

        // Hide all controller overlays during assessment — assessment UI takes over
        if (queueController != null)
        {
            HideControllerPanel(queueController.instructionText);
            HideControllerPanel(queueController.operationInfoText);
            HideControllerPanel(queueController.statusText);
            HideControllerPanel(queueController.detectionText);
        }

        if (lesson == 0 && queueController != null)
            queueController.PreFillForLesson(4);

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
                if (stepIndex == 0)               nextBlocked = !sceneSpawned;
                if (stepIndex == 1 && didEnqueue)  nextBlocked = false;
                if (stepIndex == 2 && didDequeue)  nextBlocked = false;
                if (stepIndex == 3 && didPeek)     nextBlocked = false;
                break;
            case 3:
                if (stepIndex == 0)               nextBlocked = !sceneSpawned;
                if (stepIndex == 1 && didEnqueue)  nextBlocked = false;
                if (stepIndex == 2 && didDequeue)  nextBlocked = false;
                break;
            case 4:
                if (stepIndex == 0) nextBlocked = !sceneSpawned;
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
        // Restore all controller UI so sandbox / free-play mode works normally
        if (queueController != null)
        {
            ShowControllerPanel(queueController.instructionText);
            ShowControllerPanel(queueController.operationInfoText);
            ShowControllerPanel(queueController.detectionText);
            ShowControllerPanel(queueController.statusText);
        }

        PlayerPrefs.SetInt(ARReturnHandler.AR_JUST_RETURNED_KEY, 1);
        PlayerPrefs.SetString(ARReturnHandler.AR_TOPIC_NAME_KEY,
                              PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "queues"));
        PlayerPrefs.Save();
        SceneManager.LoadScene(PlayerPrefs.GetString("AR_MainScene", mainAppSceneName));
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

    void UpdateComplexityTable(string activeOp)
    {
        string H(string op, string row) => activeOp == op ? $"> {row}" : $"  {row}";

        string t =
            "Operation        | Time\n" +
            "───────────────────────────\n" +
            H("ENQUEUE",          "Enqueue          | O(1)") + "\n" +
            H("DEQUEUE",          "Dequeue          | O(1)") + "\n" +
            H("PEEK",             "Peek             | O(1)") + "\n" +
            H("MULTIDEQUEUE",     "Multi-Dequeue(n) | O(n)") + "\n" +
            H("REVERSE",          "Reverse          | O(n)") + "\n" +
            H("SEARCH",           "Search           | O(n)") + "\n" +
            H("PRIORITY_ENQUEUE", "Priority Enqueue | O(n)") + "\n" +
            "  Space            | O(n)";

        if (complexityTableText  != null) complexityTableText.text  = t;
        if (activeOperationLabel != null)
            activeOperationLabel.text = string.IsNullOrEmpty(activeOp) ? "" : $"Last: {activeOp}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // STEPS PER LESSON
    // ─────────────────────────────────────────────────────────────────────────
    Step[] BuildSteps(int mode)
    {
        switch (mode)
        {
            case 1: return new Step[]
            {
                new Step {
                    title = "What is a Queue?",
                    body  =
                        "A Queue is a linear data structure that\n" +
                        "follows FIFO — First In, First Out.\n\n" +
                        "Also described as LILO (Last In, Last Out).\n" +
                        "Both mean the same thing:\n\n" +
                        "The FIRST element inserted is the\n" +
                        "FIRST one to be removed.\n\n" +
                        "-> Choose your Scenario and Difficulty.\n" +
                        "-> Tap a flat surface to place the scene.",
                    waitForAction = true
                },
                new Step {
                    title = "The Queue Line Analogy",
                    body  =
                        "Think of people queuing at a counter:\n\n" +
                        "ENQUEUE -> Join at the BACK of the line\n" +
                        "DEQUEUE -> Serve from the FRONT\n\n" +
                        "You CANNOT serve a person from\n" +
                        "the middle of the queue.\n\n" +
                        "Only the FRONT element is removed!\n\n" +
                        "This is why queues are called\n" +
                        "RESTRICTED LINEAR DATA STRUCTURES.",
                    waitForAction = false
                },
                new Step {
                    title = "Characteristics of Queues",
                    body  =
                        "Key properties of a queue:\n\n" +
                        "- Follows FIFO principle\n" +
                        "- Insert ONLY at the REAR\n" +
                        "- Delete ONLY from the FRONT\n" +
                        "- NO random access to elements\n" +
                        "- Only supports Enqueue, Dequeue, Peek\n" +
                        "- Can be built with arrays OR\n" +
                        "  linked lists\n\n" +
                        "Notice the FRONT and REAR markers\n" +
                        "in your AR scene — elements enter\n" +
                        "at one end and leave from the other!",
                    waitForAction = false
                },
                new Step {
                    title = "FIFO in Action",
                    body  =
                        "Watch the queue in your scene.\n" +
                        "Notice the order:\n\n" +
                        "Items were enqueued: 1, 2, 3, 4\n" +
                        "If we dequeue, we get: 1, 2, 3, 4\n\n" +
                        "The FIRST item enqueued (#1)\n" +
                        "is always the FIRST to come out.\n\n" +
                        "This ordering is critical for:\n" +
                        "- CPU task scheduling\n" +
                        "- Print spooling\n" +
                        "- Breadth-First Search (BFS)",
                    waitForAction = false
                },
                new Step {
                    title = "Queue vs Stack",
                    body  =
                        "How does a Queue differ from a Stack?\n\n" +
                        "STACK (LIFO):\n" +
                        "   Insert and remove at the TOP\n" +
                        "   Last in, first out\n\n" +
                        "QUEUE (FIFO):\n" +
                        "   Insert at REAR, remove at FRONT\n" +
                        "   First in, first out\n\n" +
                        "Both restrict access — but queues\n" +
                        "preserve arrival order, while\n" +
                        "stacks reverse it!",
                    waitForAction = false
                },
                new Step {
                    title = "Lesson 1 Complete!",
                    body  =
                        "You now understand:\n" +
                        "- What a queue is (FIFO/LILO)\n" +
                        "- The queue line analogy\n" +
                        "- Front = remove, Rear = insert\n" +
                        "- Queue characteristics\n" +
                        "- Queue vs Stack comparison\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 2: return new Step[]
            {
                new Step {
                    title = "Queue Operations Overview",
                    body  =
                        "A queue supports three core operations:\n\n" +
                        "  ENQUEUE — Insert element at REAR\n" +
                        "  DEQUEUE — Remove element from FRONT\n" +
                        "  PEEK    — View FRONT without removing\n\n" +
                        "All three run in O(1) time!\n\n" +
                        "-> Place the scene to begin,\n" +
                        "   then use the buttons above.",
                    waitForAction = true
                },
                new Step {
                    title = "ENQUEUE Operation",
                    body  =
                        "ENQUEUE inserts an element at the REAR.\n\n" +
                        "Steps:\n" +
                        "  1. Check if queue is FULL\n" +
                        "  2. Increment the rear pointer\n" +
                        "  3. Place element at rear\n\n" +
                        "Python:\n" +
                        "  def enqueue(self, value):\n" +
                        "      if self.rear == self.size-1:\n" +
                        "          print('Overflow')\n" +
                        "          return\n" +
                        "      self.queue.append(value)\n" +
                        "      self.rear += 1\n\n" +
                        "Time: O(1)\n\n" +
                        "-> Tap ENQUEUE and add an item.",
                    waitForAction = true
                },
                new Step {
                    title = "DEQUEUE Operation",
                    body  =
                        "DEQUEUE removes the FRONT element.\n\n" +
                        "Steps:\n" +
                        "  1. Check if queue is EMPTY\n" +
                        "  2. Return the front element\n" +
                        "  3. Increment the front pointer\n\n" +
                        "Python:\n" +
                        "  def dequeue(self):\n" +
                        "      if self.front > self.rear:\n" +
                        "          print('Underflow')\n" +
                        "          return None\n" +
                        "      value = self.queue[self.front]\n" +
                        "      self.front += 1\n" +
                        "      return value\n\n" +
                        "Time: O(1)\n\n" +
                        "-> Tap DEQUEUE to remove the front item.",
                    waitForAction = true
                },
                new Step {
                    title = "PEEK Operation",
                    body  =
                        "PEEK returns the FRONT element\n" +
                        "WITHOUT removing it.\n\n" +
                        "Python:\n" +
                        "  def peek(self):\n" +
                        "      if self.front > self.rear:\n" +
                        "          return None\n" +
                        "      return self.queue[self.front]\n\n" +
                        "No change to the queue!\n" +
                        "Read-only operation.\n\n" +
                        "Time: O(1)\n\n" +
                        "-> Tap PEEK to view the front item.",
                    waitForAction = true
                },
                new Step {
                    title = "Lesson 2 Complete!",
                    body  =
                        "You performed all three operations!\n\n" +
                        "- Enqueue: Insert at rear     O(1)\n" +
                        "- Dequeue: Remove from front  O(1)\n" +
                        "- Peek:    View front (no remove) O(1)\n\n" +
                        "Key rule: Elements always enter\n" +
                        "at REAR and leave from FRONT.\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 3: return new Step[]
            {
                new Step {
                    title = "Implementing a Queue",
                    body  =
                        "A queue can be built two ways:\n\n" +
                        "1. ARRAY-BASED (Circular)\n" +
                        "   Fixed-size array + front/rear indices\n\n" +
                        "2. LINKED LIST-BASED\n" +
                        "   Nodes with head (front) + tail (rear)\n\n" +
                        "Both give O(1) Enqueue, Dequeue, Peek.\n" +
                        "They differ in memory behaviour.\n\n" +
                        "-> Place the scene to explore both!",
                    waitForAction = true
                },
                new Step {
                    title = "Array Implementation (Circular)",
                    body  =
                        "QUEUE USING CIRCULAR ARRAY:\n\n" +
                        "  queue = []   <- fixed-size array\n" +
                        "  front = 0    <- front pointer\n" +
                        "  rear  = -1   <- rear pointer\n\n" +
                        "Enqueue: rear = (rear+1) % size\n" +
                        "         queue[rear] = value\n" +
                        "Dequeue: value = queue[front]\n" +
                        "         front = (front+1) % size\n\n" +
                        "Advantages:\n" +
                        "- Simple, cache-friendly\n" +
                        "- No wasted slots (circular wrap)\n\n" +
                        "Disadvantages:\n" +
                        "! Fixed max capacity\n\n" +
                        "-> Tap ENQUEUE to simulate array enqueue.",
                    waitForAction = true
                },
                new Step {
                    title = "Linked List Implementation",
                    body  =
                        "QUEUE USING LINKED LIST:\n\n" +
                        "  class Node:\n" +
                        "      data, next\n\n" +
                        "  head -> front node\n" +
                        "  tail -> rear node\n\n" +
                        "Enqueue: new_node added at tail\n" +
                        "         tail = new_node\n\n" +
                        "Dequeue: value = head.data\n" +
                        "         head = head.next\n\n" +
                        "Advantages:\n" +
                        "- Dynamic size (no overflow)\n" +
                        "- Memory allocated as needed\n\n" +
                        "Disadvantages:\n" +
                        "! Extra memory for pointers\n\n" +
                        "-> Tap DEQUEUE to simulate LL dequeue.",
                    waitForAction = true
                },
                new Step {
                    title = "Comparing Implementations",
                    body  =
                        "ARRAY vs LINKED LIST:\n\n" +
                        "            Array   |   LL\n" +
                        "Enqueue      O(1)   |  O(1)\n" +
                        "Dequeue      O(1)   |  O(1)\n" +
                        "Peek         O(1)   |  O(1)\n" +
                        "Space        O(n)   |  O(n)\n" +
                        "Memory    Fixed     |  Dynamic\n" +
                        "Overflow  Possible  |  None*\n\n" +
                        "*unless system memory runs out\n\n" +
                        "Circular arrays solve the shifting\n" +
                        "problem of naive array queues —\n" +
                        "no element movement needed!",
                    waitForAction = false
                },
                new Step {
                    title = "Lesson 3 Complete!",
                    body  =
                        "You now understand both implementations:\n\n" +
                        "- Circular Array: fixed size, no shifting\n" +
                        "- Linked List: dynamic, pointer overhead\n\n" +
                        "Both give O(1) for all core operations.\n" +
                        "Space complexity is O(n) for both.\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 4: return new Step[]
            {
                new Step {
                    title = "Where Are Queues Used?",
                    body  =
                        "Queues are fundamental in computing!\n\n" +
                        "Five key applications:\n" +
                        "  1. CPU / Process Scheduling\n" +
                        "  2. Print Spooling\n" +
                        "  3. Breadth-First Search (BFS)\n" +
                        "  4. Network Packet Buffering\n" +
                        "  5. Keyboard Input Buffering\n\n" +
                        "-> Place the scene, then use ENQUEUE\n" +
                        "   and DEQUEUE to explore each one.",
                    waitForAction = true
                },
                new Step {
                    title = "CPU Scheduling & Print Spooling",
                    body  =
                        "CPU SCHEDULING:\n" +
                        "Processes waiting to run are stored\n" +
                        "in a Ready Queue (FIFO):\n" +
                        "  Process A arrives -> Enqueue A\n" +
                        "  Process B arrives -> Enqueue B\n" +
                        "  CPU free -> Dequeue A (runs first)\n" +
                        "  CPU free -> Dequeue B (runs next)\n\n" +
                        "PRINT SPOOLING:\n" +
                        "Print jobs queue up in order.\n" +
                        "First job sent is always printed first!\n\n" +
                        "-> ENQUEUE to simulate adding a process.",
                    waitForAction = false
                },
                new Step {
                    title = "BFS & Network Buffering",
                    body  =
                        "BREADTH-FIRST SEARCH (BFS):\n" +
                        "Explores a graph level by level:\n" +
                        "  1. Enqueue starting node\n" +
                        "  2. Dequeue node, visit it\n" +
                        "  3. Enqueue all unvisited neighbours\n" +
                        "  4. Repeat until queue is empty\n\n" +
                        "NETWORK PACKET BUFFERING:\n" +
                        "Data packets arrive faster than they\n" +
                        "can be processed — they queue up\n" +
                        "in a buffer and are sent in order.\n\n" +
                        "-> Try DEQUEUE to simulate processing.",
                    waitForAction = false
                },
                new Step {
                    title = "Keyboard & I/O Buffering",
                    body  =
                        "KEYBOARD INPUT BUFFER:\n" +
                        "Every key you press is enqueued\n" +
                        "into an input buffer.\n" +
                        "The CPU dequeues each character\n" +
                        "to process it in typing order.\n\n" +
                        "This ensures keystrokes are never\n" +
                        "lost, even if the CPU is busy!\n\n" +
                        "I/O BUFFERING follows the same idea\n" +
                        "for file reads, network streams,\n" +
                        "and audio playback.\n\n" +
                        "-> FIFO order guarantees correctness.",
                    waitForAction = false
                },
                new Step {
                    title = "Lesson 4 Complete!",
                    body  =
                        "You now understand queue applications:\n\n" +
                        "- CPU / process scheduling (ready queue)\n" +
                        "- Print spooling (jobs served in order)\n" +
                        "- BFS graph traversal (level-by-level)\n" +
                        "- Network packet buffering\n" +
                        "- Keyboard & I/O buffering\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 5: return new Step[]
            {
                new Step {
                    title = "Queue Complexity Summary",
                    body  =
                        "Perform any operation — the table\n" +
                        "highlights its complexity in real time.\n\n" +
                        "Try each operation at least once to\n" +
                        "light up every row of the table.\n\n" +
                        "Use INTERMEDIATE mode for Priority\n" +
                        "Enqueue to see O(n) in action!",
                    waitForAction = false
                },
                new Step {
                    title = "O(1) vs O(n)",
                    body  =
                        "O(1) — CONSTANT TIME\n" +
                        "  Instant, regardless of queue size.\n" +
                        "  Examples: Enqueue, Dequeue, Peek\n\n" +
                        "O(n) — LINEAR TIME\n" +
                        "  Work doubles when queue doubles.\n" +
                        "  Examples: Multi-Dequeue n items,\n" +
                        "            Reverse entire queue\n\n" +
                        "The three basic queue operations are\n" +
                        "ALL O(1) — this is the key advantage\n" +
                        "of the queue data structure!",
                    waitForAction = false
                },
                new Step {
                    title = "Full Complexity Table",
                    body  =
                        "From the syllabus — complete summary:\n\n" +
                        "Enqueue:           O(1)\n" +
                        "Dequeue:           O(1)\n" +
                        "Peek:              O(1)\n" +
                        "Multi-Dequeue(n):  O(n)\n" +
                        "Reverse:           O(n)\n" +
                        "Search:            O(n)\n\n" +
                        "Space Complexity:  O(n)\n\n" +
                        "The simplicity of O(1) for core ops\n" +
                        "makes queues highly efficient for\n" +
                        "FIFO-ordered problems.",
                    waitForAction = false
                },
                new Step {
                    title = "Lesson 5 Complete!",
                    body  =
                        "You have mastered Queues!\n\n" +
                        "- FIFO / LILO definition\n" +
                        "- Characteristics & queue line analogy\n" +
                        "- Enqueue, Dequeue, Peek — O(1) each\n" +
                        "- Circular Array vs Linked List impl.\n" +
                        "- CPU scheduling, BFS, buffering\n" +
                        "- Full Big-O complexity table\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            default: return new Step[0];
        }
    }
}