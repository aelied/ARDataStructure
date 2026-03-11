using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ARStackLessonGuide.cs — SYLLABUS-ALIGNED VERSION
/// ==================================================
/// UI SYNC FIX (mirrors ARArrayLessonGuide):
///   SyncControllerUI(stepIndex) is called on every ShowStep() and again
///   after OnSceneSpawned(). It sets instructionText / operationInfoText /
///   statusText / detectionText to lesson-relevant content, or hides the
///   whole root panel when the guide card already covers that information.
///
///   GetPanelRoot(tmp) walks two levels up from the TMP component:
///     Text -> Panel -> RootPanel (InstructionCard / DetectionFeedback / etc.)
///   so the entire card — background, border, text — hides/shows together.
///
/// Per-lesson sync table:
///   Mode 1 (L1 Intro):        instructionText shown (step hint)
///                              operationInfoText hidden (guide card covers it)
///                              statusText        hidden (no operations yet)
///                              detectionText     hidden after placement
///
///   Mode 2 (L2 Operations):   instructionText shown (which button to tap)
///                              operationInfoText shown (controller writes op detail)
///                              statusText        shown
///                              detectionText     hidden after placement
///
///   Mode 3 (L3 Implementation):instructionText shown
///                              operationInfoText shown (impl notes per op)
///                              statusText        shown
///                              detectionText     hidden after placement
///
///   Mode 4 (L4 Applications):  instructionText shown
///                              operationInfoText shown (toast replaces some)
///                              statusText        shown
///                              detectionText     hidden after placement
///
///   Mode 5 (L5 Complexity):    instructionText shown
///                              operationInfoText hidden (complexity table replaces it)
///                              statusText        shown
///                              detectionText     hidden after placement
///
/// Assessment transition: DelayedAssessmentStart hides all four controller
///   overlays since the assessment UI fully takes over.
/// OnReturn: restores all four overlays for sandbox / free-play mode.
/// </summary>
public class ARStackLessonGuide : MonoBehaviour
{
    [Header("Your Existing Stack Controller")]
    public InteractiveStackPlates stackController;

    [Header("Existing Canvas Panels")]
    public GameObject beginnerButtonPanel;
    public GameObject intermediateButtonPanel;
    public GameObject movementControlPanel;
    public GameObject confirmButton;

    [Header("Guide Canvas (StackGuideCanvas)")]
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
    public ARStackLessonAssessment lessonAssessment;

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

    [Header("Stack Inspect Card")]
    public GameObject      stackInspectCard;
    public TextMeshProUGUI inspectOperationText;
    public TextMeshProUGUI inspectStackSizeText;
    public TextMeshProUGUI inspectTopElementText;
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

    int  lastStackCount = 0;
    bool didPush        = false;
    bool didPop         = false;
    bool didPeek        = false;
    bool didMultiPop    = false;
    bool didReverse     = false;

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
        Debug.Log("[ARStackLessonGuide] ResetInitFlag — ready for fresh init");
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (_started) return;
        _started = true;

        string topic = PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "");
        lessonIndex  = PlayerPrefs.GetInt(ARReturnHandler.AR_LESSON_INDEX_KEY, -1);

        Debug.Log($"[ARStackLessonGuide] Start — topic='{topic}' lessonIndex={lessonIndex}");

        if (!topic.ToLower().Contains("stack") || lessonIndex < 0)
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
            lessonAssessment = GetComponent<ARStackLessonAssessment>();

        Debug.Log($"[ARStackLessonGuide] Lesson {lessonIndex} -> Mode {guideMode} | Assessment: {(lessonAssessment != null ? "FOUND" : "NULL")}");
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

        stackInspectCard?.SetActive(false);
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
        inspectCloseButton?.onClick.AddListener(() => stackInspectCard?.SetActive(false));

        ResetOperationFlags();

        // Stop the controller from overwriting the guide's own instruction panels
        stackController?.SetInstructionSilence(true);

        steps = BuildSteps(guideMode);
        nextBlocked = true;
        ShowStep(0);
    }

    void ResetOperationFlags()
    {
        didPush     = false;
        didPop      = false;
        didPeek     = false;
        didMultiPop = false;
        didReverse  = false;
        opLog.Clear();
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!enabled) return;

        if (!sceneSpawned && stackController != null && stackController.ParkingLot != null)
        {
            sceneSpawned = true;
            OnSceneSpawned();
        }

        if (sceneSpawned && guideMode == 1 && !assessmentStarted)
            EnsureActionButtonsHidden();
    }

    void OnSceneSpawned()
    {
        Debug.Log($"[ARStackLessonGuide] Stack scene detected — guideMode={guideMode} currentStep={currentStep}");

        if (guideMode == 5) { complexityTablePanel?.SetActive(true); UpdateComplexityTable(""); }

        if (guideMode == 1 && stackController != null)
            stackController.PreFillForLesson(4);

        nextBlocked = false;
        UpdateNextButton();
        lastStackCount = GetCurrentStackCount();

        if (guideMode >= 2 && currentStep >= 1)
        {
            bool showBeginner     = guideMode <= 3;
            bool showIntermediate = guideMode >= 4;
            beginnerButtonPanel?.SetActive(showBeginner);
            intermediateButtonPanel?.SetActive(showIntermediate);
        }

        // Sync controller UI now that the scene is placed
        SyncControllerUI(currentStep);
    }

    void EnsureActionButtonsHidden()
    {
        HideGO(beginnerButtonPanel);
        HideGO(intermediateButtonPanel);
        HideGO(movementControlPanel);
        HideGO(confirmButton);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CONTROLLER UI SYNC
    // Called every ShowStep() and again after OnSceneSpawned().
    // Sets instructionText / operationInfoText / statusText / detectionText
    // to lesson-relevant text, or hides the root panel when redundant.
    // ─────────────────────────────────────────────────────────────────────────
    void SyncControllerUI(int stepIndex)
    {
        if (stackController == null) return;

        // detectionText panel: only useful before the scene is placed
        if (sceneSpawned)
            HideControllerPanel(stackController.detectionText);
        else
            ShowControllerPanel(stackController.detectionText);

        switch (guideMode)
        {
            // L1 - Introduction: stack pre-filled, no operations allowed.
            // instructionText -> step-matched hint.
            // operationInfoText -> hidden (guide card covers it).
            // statusText -> hidden (no operations to track).
            case 1:
            {
                string hint = stepIndex switch
                {
                    0 => "Point your camera at a flat surface and tap to place the scene.",
                    1 => "Look at the stack — only the TOP plate is accessible. This is LIFO.",
                    2 => "Notice the TOP marker above the stack — only that position can be pushed or popped.",
                    3 => "The last item pushed will always be the first one popped out.",
                    4 => "Unlike an array, you cannot access any element by index — only the top!",
                    _ => "Assessment starting shortly..."
                };
                SetControllerText(stackController.instructionText, hint);
                HideControllerPanel(stackController.operationInfoText);
                HideControllerPanel(stackController.statusText);
                break;
            }

            // L2 - Operations: push / pop / peek.
            // instructionText -> which button to tap next.
            // operationInfoText -> shown (controller writes op detail after each op).
            // statusText -> shown (stack count useful).
            case 2:
            {
                string hint = stepIndex switch
                {
                    0 => "Point your camera at a flat surface and tap to place the scene.",
                    1 => "Tap  PUSH  to add a new item to the TOP of the stack.",
                    2 => "Tap  POP  to remove the TOP item from the stack.",
                    3 => "Tap  PEEK  to view the TOP item without removing it.",
                    _ => "All operations done. Assessment starting shortly..."
                };
                SetControllerText(stackController.instructionText, hint);
                ShowControllerPanel(stackController.operationInfoText);
                ShowControllerPanel(stackController.statusText);
                break;
            }

            // L3 - Implementation: array vs linked list.
            // instructionText -> which operation to perform.
            // operationInfoText -> shown (controller writes impl notes per op).
            // statusText -> shown.
            case 3:
            {
                string hint = stepIndex switch
                {
                    0 => "Point your camera at a flat surface and tap to place the scene.",
                    1 => "Tap  PUSH  to simulate  stack[++top] = value  (array implementation).",
                    2 => "Tap  POP  to simulate  return stack[top--]  (array implementation).",
                    3 => "Both array and linked list give O(1) for Push, Pop, and Peek.",
                    _ => "Implementations compared. Assessment starting shortly..."
                };
                SetControllerText(stackController.instructionText, hint);
                ShowControllerPanel(stackController.operationInfoText);
                ShowControllerPanel(stackController.statusText);
                break;
            }

            // L4 - Applications: function calls, recursion, parsing.
            // instructionText -> what to simulate.
            // operationInfoText -> shown (toast supplements it).
            // statusText -> shown.
            case 4:
            {
                string hint = stepIndex switch
                {
                    0 => "Point your camera at a flat surface and tap to place the scene.",
                    1 => "Tap  PUSH  to simulate a function call being added to the call stack.",
                    2 => "Tap  POP  to simulate a function returning — control goes back to the caller.",
                    3 => "Stacks also handle expression evaluation and balanced symbol checking.",
                    _ => "Applications demonstrated. Assessment starting shortly..."
                };
                SetControllerText(stackController.instructionText, hint);
                ShowControllerPanel(stackController.operationInfoText);
                ShowControllerPanel(stackController.statusText);
                break;
            }

            // L5 - Big-O Complexity: complexity table is the star.
            // instructionText -> encourage trying different ops.
            // operationInfoText -> hidden (complexity table replaces it).
            // statusText -> shown.
            case 5:
            {
                string hint = stepIndex switch
                {
                    0 => "Perform any operation — watch its row light up in the Big-O table.",
                    1 => "Push, Pop, and Peek are O(1). Try them to see the table highlight.",
                    2 => "Use  INTERMEDIATE  mode for Multi-Pop and Reverse to see O(n) rows.",
                    _ => "Big-O table complete. Assessment starting shortly..."
                };
                SetControllerText(stackController.instructionText, hint);
                HideControllerPanel(stackController.operationInfoText);
                ShowControllerPanel(stackController.statusText);
                break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PANEL ROOT HELPERS
    // Walks two levels up from the TMP component to find the root card panel:
    //   Text -> Panel -> RootPanel (InstructionCard / StatusPanel / etc.)
    // ─────────────────────────────────────────────────────────────────────────
    GameObject GetPanelRoot(TextMeshProUGUI tmp)
    {
        if (tmp == null) return null;
        Transform t = tmp.transform.parent; // Panel
        if (t != null && t.parent != null)
            return t.parent.gameObject;     // RootPanel
        if (t != null)
            return t.gameObject;            // Panel (fallback)
        return tmp.gameObject;              // Text itself (last resort)
    }

    /// <summary>Shows the root panel and sets the TMP text + color to white.</summary>
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

    // Keep these for non-TMP GameObjects wired directly
    void ShowGO(GameObject go) { if (go != null) go.SetActive(true); }
    void HideGO(GameObject go) { if (go != null && go.activeSelf) go.SetActive(false); }

    // ─────────────────────────────────────────────────────────────────────────
    // OPERATION CALLBACKS (called by InteractiveStackPlates)
    // ─────────────────────────────────────────────────────────────────────────
    public void OnPushConfirmed()
    {
        Debug.Log($"[StackGuide] OnPushConfirmed — didPush={didPush}, step={currentStep}");
        lastStackCount = GetCurrentStackCount();
        OnOperationDetected("PUSH", true);
        if (!didPush) { didPush = true; TryUnblock(); }
    }

    public void OnPopConfirmed()
    {
        lastStackCount = GetCurrentStackCount();
        OnOperationDetected("POP", true);
        if (!didPop) { didPop = true; TryUnblock(); }
    }

    int GetCurrentStackCount()
    {
        if (stackController == null) return 0;
        return stackController.CurrentStackCount;
    }

    public void NotifyPeekPerformed()
    {
        if (!didPeek) { didPeek = true; TryUnblock(); }
        ShowStackInspectCard("PEEK", "O(1) — Read top without removing");

        // Update instructionText live for L2 and L4
        if (stackController?.instructionText != null)
        {
            if (guideMode == 2 || guideMode == 4)
            {
                GetPanelRoot(stackController.instructionText)?.SetActive(true);
                stackController.instructionText.text  = "PEEK done — top item viewed without removing it. O(1)!";
                stackController.instructionText.color = new Color(0.4f, 1f, 0.5f);
            }
        }

        if (guideMode == 4)
            ShowToast(checkSprite, "PEEK: Viewed top element — O(1), non-destructive!");
        else if (guideMode == 5)
            UpdateComplexityTable("PEEK");
    }

    public void NotifyMultiPopPerformed(int count)
    {
        if (!didMultiPop) { didMultiPop = true; TryUnblock(); }

        if (stackController?.instructionText != null && guideMode == 5)
        {
            GetPanelRoot(stackController.instructionText)?.SetActive(true);
            stackController.instructionText.text  = $"MULTI-POP logged — popped {count} item(s), O(n) cost shown in table!";
            stackController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }

        if (guideMode == 5) UpdateComplexityTable("MULTIPOP");
    }

    public void NotifyReversePerformed()
    {
        if (!didReverse) { didReverse = true; TryUnblock(); }

        if (stackController?.instructionText != null && guideMode == 5)
        {
            GetPanelRoot(stackController.instructionText)?.SetActive(true);
            stackController.instructionText.text  = "REVERSE logged — O(n) row lit in the table!";
            stackController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }

        if (guideMode == 5) UpdateComplexityTable("REVERSE");
    }

    void OnOperationDetected(string op, bool success)
    {
        string complexity = op switch
        {
            "PUSH" => "O(1) — insert at top",
            "POP"  => "O(1) — remove from top",
            _      => "O(1)"
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
            ShowStackInspectCard(op, complexity);

            // Update instructionText to next logical step
            UpdateInstructionAfterOp(op);
        }
        else if (guideMode == 3)
        {
            opFeedbackPanel?.SetActive(true);
            if (opFeedbackTitle   != null) opFeedbackTitle.text   = op;
            if (opFeedbackBody    != null) opFeedbackBody.text    = ImplementationNote(op);
            if (opComplexityBadge != null) opComplexityBadge.text = complexity;
            StartCoroutine(HideAfter(opFeedbackPanel, 3.5f));

            UpdateInstructionAfterOp(op);
        }
        else if (guideMode == 4)
        {
            string msg = op switch
            {
                "PUSH" => "PUSH: Added to top — like a function call pushed to call stack!",
                "POP"  => "POP: Removed from top — like a function returning!",
                _      => "Operation detected"
            };
            ShowToast(success ? checkSprite : crossSprite, msg);
            UpdateInstructionAfterOp(op);
        }
        else if (guideMode == 5)
        {
            UpdateComplexityTable(op);

            if (stackController?.instructionText != null)
            {
                GetPanelRoot(stackController.instructionText)?.SetActive(true);
                stackController.instructionText.text  = $"{op} logged in the table!  Try another operation to light up more rows.";
                stackController.instructionText.color = new Color(0.4f, 1f, 0.5f);
            }
        }
    }

    /// <summary>
    /// After an operation fires, update instructionText to tell the student
    /// what to try next rather than leaving stale text.
    /// </summary>
    void UpdateInstructionAfterOp(string op)
    {
        if (stackController?.instructionText == null) return;

        string next;
        if (guideMode == 2) // L2 Operations
        {
            next = op switch
            {
                "PUSH" => "PUSH done — item added to top.  Now tap  POP  to remove it.",
                "POP"  => "POP done — top item removed.  Now tap  PEEK  to view the top without removing.",
                "POP_EMPTY" => "Stack is empty — tap  PUSH  to add an item first.",
                _      => "PEEK done — stack unchanged.  All three operations complete!"
            };
        }
        else if (guideMode == 3) // L3 Implementation
        {
            next = op switch
            {
                "PUSH" => "Array PUSH done: stack[++top] = value.  Now tap  POP  to simulate stack[top--].",
                "POP"  => "Array POP done: return stack[top--].  Both implementations give O(1)!",
                _      => "Operation complete."
            };
        }
        else // L4 Applications
        {
            next = op switch
            {
                "PUSH" => "Function PUSHED — it is now active on the call stack.  Tap  POP  to simulate it returning.",
                "POP"  => "Function POPPED — control returned to the previous caller.  Try  PEEK  to inspect the stack.",
                _      => "All application scenarios demonstrated!"
            };
        }

        GetPanelRoot(stackController.instructionText)?.SetActive(true);
        stackController.instructionText.text  = next;
        stackController.instructionText.color = new Color(0.4f, 1f, 0.5f);
    }

    string OpExplanation(string op)
    {
        switch (op)
        {
            case "PUSH":
                return "PUSH — Insert at TOP:\n" +
                       "1. Check if stack is full\n" +
                       "2. Increment top pointer\n" +
                       "3. Place element at top\n\n" +
                       "Time: O(1) — Always instant!";
            case "POP":
                return "POP — Remove from TOP:\n" +
                       "1. Check if stack is empty\n" +
                       "2. Return top element\n" +
                       "3. Decrement top pointer\n\n" +
                       "Time: O(1) — Always instant!";
            case "PEEK":
                return "PEEK — View TOP:\n" +
                       "1. Check if stack is empty\n" +
                       "2. Return stack[top]\n" +
                       "   (without removing)\n\n" +
                       "Time: O(1) — Read-only!";
            default:
                return "Stack operation detected.";
        }
    }

    string ImplementationNote(string op)
    {
        switch (op)
        {
            case "PUSH":
                return "Array Implementation:\n" +
                       "  stack[++top] = value\n\n" +
                       "Linked List Implementation:\n" +
                       "  new_node.next = top\n" +
                       "  top = new_node\n\n" +
                       "Both: O(1)";
            case "POP":
                return "Array Implementation:\n" +
                       "  return stack[top--]\n\n" +
                       "Linked List Implementation:\n" +
                       "  value = top.data\n" +
                       "  top = top.next\n\n" +
                       "Both: O(1)";
            default:
                return OpExplanation(op);
        }
    }

    void ShowStackInspectCard(string op, string complexity)
    {
        if (stackInspectCard == null) return;
        stackInspectCard.SetActive(true);
        if (inspectOperationText  != null) inspectOperationText.text  = $"Operation: {op}";
        if (inspectStackSizeText  != null) inspectStackSizeText.text  = $"Stack Size: {GetCurrentStackCount()}";
        if (inspectTopElementText != null) inspectTopElementText.text = $"Top Index: {GetCurrentStackCount() - 1}";
        if (inspectComplexityText != null) inspectComplexityText.text = $"Time: {complexity}";
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

        // Sync controller overlays to this step
        SyncControllerUI(index);

        Debug.Log($"[StackGuide] ShowStep {index}, isLast={isLast}, assessmentStarted={assessmentStarted}");

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
        stackInspectCard?.SetActive(false);

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

        // Hide all controller overlays — assessment UI takes over completely
        if (stackController != null)
        {
            HideControllerPanel(stackController.instructionText);
            HideControllerPanel(stackController.operationInfoText);
            HideControllerPanel(stackController.statusText);
            HideControllerPanel(stackController.detectionText);
        }

        if (lesson == 0 && stackController != null)
            stackController.PreFillForLesson(4);

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
                if (stepIndex == 0)                nextBlocked = !sceneSpawned;
                if (stepIndex == 1 && didPush)     nextBlocked = false;
                if (stepIndex == 2 && didPop)      nextBlocked = false;
                if (stepIndex == 3 && didPeek)     nextBlocked = false;
                break;
            case 3:
                if (stepIndex == 0)            nextBlocked = !sceneSpawned;
                if (stepIndex == 1 && didPush) nextBlocked = false;
                if (stepIndex == 2 && didPop)  nextBlocked = false;
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
        // Restore controller to full self-managed mode for sandbox / free-play
        stackController?.SetInstructionSilence(false);

        // Panels are already restored by SetInstructionSilence(false) above,
        // but call ShowControllerPanel too in case roots were hidden mid-lesson
        if (stackController != null)
        {
            ShowControllerPanel(stackController.instructionText);
            ShowControllerPanel(stackController.operationInfoText);
            ShowControllerPanel(stackController.detectionText);
            ShowControllerPanel(stackController.statusText);
        }

        PlayerPrefs.SetInt(ARReturnHandler.AR_JUST_RETURNED_KEY, 1);
        PlayerPrefs.SetString(ARReturnHandler.AR_TOPIC_NAME_KEY,
                              PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "stacks"));
        PlayerPrefs.Save();
        SceneManager.LoadScene(PlayerPrefs.GetString("AR_MainScene", mainAppSceneName));
    }

    // ─────────────────────────────────────────────────────────────────────────
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
            "Operation      | Time\n" +
            "─────────────────────────\n" +
            H("PUSH",     "Push           | O(1)") + "\n" +
            H("POP",      "Pop            | O(1)") + "\n" +
            H("PEEK",     "Peek           | O(1)") + "\n" +
            H("MULTIPOP", "Multi-Pop (n)  | O(n)") + "\n" +
            H("REVERSE",  "Reverse        | O(n)") + "\n" +
            H("SEARCH",   "Search         | O(n)") + "\n" +
            "  Space          | O(n)";

        if (complexityTableText  != null) complexityTableText.text  = t;
        if (activeOperationLabel != null)
            activeOperationLabel.text = string.IsNullOrEmpty(activeOp) ? "" : $"Last: {activeOp}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // STEPS — identical to original
    // ─────────────────────────────────────────────────────────────────────────
    Step[] BuildSteps(int mode)
    {
        switch (mode)
        {
            case 1: return new Step[]
            {
                new Step {
                    title = "What is a Stack?",
                    body  =
                        "A Stack is a linear data structure that\n" +
                        "follows LIFO — Last In, First Out.\n\n" +
                        "Also described as FILO (First In, Last Out).\n" +
                        "Both mean the same thing:\n\n" +
                        "The LAST element inserted is the\n" +
                        "FIRST one to be removed.\n\n" +
                        "-> Choose your Scenario and Difficulty.\n" +
                        "-> Tap a flat surface to place the scene.",
                    waitForAction = true
                },
                new Step {
                    title = "The Plate Stack Analogy",
                    body  =
                        "Think of a stack of dinner plates:\n\n" +
                        "PUSH -> Add a new plate on TOP\n" +
                        "POP  -> Remove the TOP plate\n\n" +
                        "You CANNOT remove a plate from\n" +
                        "the middle of the stack.\n\n" +
                        "Only the TOP element is accessible!\n\n" +
                        "This is why stacks are called\n" +
                        "RESTRICTED LINEAR DATA STRUCTURES.",
                    waitForAction = false
                },
                new Step {
                    title = "Characteristics of Stacks",
                    body  =
                        "Key properties of a stack:\n\n" +
                        "- Follows LIFO principle\n" +
                        "- Insert & delete ONLY at the top\n" +
                        "- NO random access to elements\n" +
                        "- Only supports Push, Pop, Peek\n" +
                        "- Can be built with arrays OR\n" +
                        "  linked lists\n\n" +
                        "Notice the TOP marker above the\n" +
                        "stack in your AR scene — only that\n" +
                        "position can be pushed or popped!",
                    waitForAction = false
                },
                new Step {
                    title = "LIFO in Action",
                    body  =
                        "Watch the stack in your scene.\n" +
                        "Notice the order:\n\n" +
                        "Items were pushed: 1, 2, 3, 4\n" +
                        "If we pop, we get: 4, 3, 2, 1\n\n" +
                        "The LAST item pushed (#4)\n" +
                        "is always the FIRST to come out.\n\n" +
                        "This ordering is critical for:\n" +
                        "- Function call management\n" +
                        "- Undo/redo operations\n" +
                        "- Browser back navigation",
                    waitForAction = false
                },
                new Step {
                    title = "Stack vs Array",
                    body  =
                        "How does a Stack differ from an Array?\n\n" +
                        "ARRAY:\n" +
                        "   Access any index directly O(1)\n" +
                        "   Insert / delete anywhere\n\n" +
                        "STACK:\n" +
                        "   Access TOP only\n" +
                        "   Cannot access middle elements\n" +
                        "   No random access by index\n\n" +
                        "Stacks RESTRICT access on purpose —\n" +
                        "this simplicity is their strength!",
                    waitForAction = false
                },
                new Step {
                    title = "Lesson 1 Complete!",
                    body  =
                        "You now understand:\n" +
                        "- What a stack is (LIFO/FILO)\n" +
                        "- The plate analogy\n" +
                        "- Why only the TOP is accessible\n" +
                        "- Stack characteristics\n" +
                        "- Stack vs Array comparison\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 2: return new Step[]
            {
                new Step {
                    title = "Stack Operations Overview",
                    body  =
                        "A stack supports three core operations:\n\n" +
                        "  PUSH  — Insert element at TOP\n" +
                        "  POP   — Remove element from TOP\n" +
                        "  PEEK  — View TOP without removing\n\n" +
                        "All three run in O(1) time!\n\n" +
                        "-> Place the scene to begin,\n" +
                        "  then use the buttons above.",
                    waitForAction = true
                },
                new Step {
                    title = "PUSH Operation",
                    body  =
                        "PUSH inserts an element onto the TOP.\n\n" +
                        "Steps:\n" +
                        "  1. Check if stack is FULL\n" +
                        "  2. Increment the top pointer\n" +
                        "  3. Place element at top\n\n" +
                        "Python:\n" +
                        "  def push(self, value):\n" +
                        "      if self.top == self.size-1:\n" +
                        "          print('Overflow')\n" +
                        "          return\n" +
                        "      self.stack.append(value)\n" +
                        "      self.top += 1\n\n" +
                        "Time: O(1)\n\n" +
                        "-> Tap PUSH and add an item.",
                    waitForAction = true
                },
                new Step {
                    title = "POP Operation",
                    body  =
                        "POP removes the TOP element.\n\n" +
                        "Steps:\n" +
                        "  1. Check if stack is EMPTY\n" +
                        "  2. Return the top element\n" +
                        "  3. Decrement the top pointer\n\n" +
                        "Python:\n" +
                        "  def pop(self):\n" +
                        "      if self.top == -1:\n" +
                        "          print('Underflow')\n" +
                        "          return None\n" +
                        "      value = self.stack.pop()\n" +
                        "      self.top -= 1\n" +
                        "      return value\n\n" +
                        "Time: O(1)\n\n" +
                        "-> Tap POP to remove the top item.",
                    waitForAction = true
                },
                new Step {
                    title = "PEEK Operation",
                    body  =
                        "PEEK returns the TOP element\n" +
                        "WITHOUT removing it.\n\n" +
                        "Python:\n" +
                        "  def peek(self):\n" +
                        "      if self.top == -1:\n" +
                        "          return None\n" +
                        "      return self.stack[self.top]\n\n" +
                        "No change to the stack!\n" +
                        "Read-only operation.\n\n" +
                        "Time: O(1)\n\n" +
                        "-> Tap PEEK to view the top item.",
                    waitForAction = true
                },
                new Step {
                    title = "Lesson 2 Complete!",
                    body  =
                        "You performed all three operations!\n\n" +
                        "- Push:  Insert at top       O(1)\n" +
                        "- Pop:   Remove from top     O(1)\n" +
                        "- Peek:  View top (no remove) O(1)\n\n" +
                        "Key rule: You can ONLY interact\n" +
                        "with the TOP of the stack.\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 3: return new Step[]
            {
                new Step {
                    title = "Implementing a Stack",
                    body  =
                        "A stack can be built two ways:\n\n" +
                        "1. ARRAY-BASED\n" +
                        "   Fixed-size array + top index\n\n" +
                        "2. LINKED LIST-BASED\n" +
                        "   Nodes with data + next pointer\n\n" +
                        "Both give O(1) Push, Pop, Peek.\n" +
                        "They differ in memory behaviour.\n\n" +
                        "-> Place the scene to explore both!",
                    waitForAction = true
                },
                new Step {
                    title = "Array Implementation",
                    body  =
                        "STACK USING ARRAYS:\n\n" +
                        "  stack = []   <- fixed-size array\n" +
                        "  top = -1     <- top pointer\n\n" +
                        "Push:  stack[++top] = value\n" +
                        "Pop:   return stack[top--]\n" +
                        "Peek:  return stack[top]\n\n" +
                        "Advantages:\n" +
                        "   Simple to implement\n" +
                        "   Fast access (cache-friendly)\n\n" +
                        "Disadvantages:\n" +
                        "   Fixed size (overflow risk)\n" +
                        "   Wasted memory if under-used\n\n" +
                        "-> Tap PUSH to simulate array push.",
                    waitForAction = true
                },
                new Step {
                    title = "Linked List Implementation",
                    body  =
                        "STACK USING LINKED LIST:\n\n" +
                        "  class Node:\n" +
                        "      data, next\n\n" +
                        "  top -> head node\n\n" +
                        "Push:  new_node.next = top\n" +
                        "       top = new_node\n\n" +
                        "Pop:   value = top.data\n" +
                        "       top = top.next\n\n" +
                        "Advantages:\n" +
                        "   Dynamic size (no overflow)\n" +
                        "   Memory allocated as needed\n\n" +
                        "Disadvantages:\n" +
                        "   Extra memory for pointers\n" +
                        "   Slightly slower than arrays\n\n" +
                        "-> Tap POP to simulate LL pop.",
                    waitForAction = true
                },
                new Step {
                    title = "Comparing Implementations",
                    body  =
                        "ARRAY vs LINKED LIST:\n\n" +
                        "            Array   |   LL\n" +
                        "Push         O(1)   |  O(1)\n" +
                        "Pop          O(1)   |  O(1)\n" +
                        "Peek         O(1)   |  O(1)\n" +
                        "Space        O(n)   |  O(n)\n" +
                        "Memory    Fixed     |  Dynamic\n" +
                        "Overflow  Possible  |  None*\n\n" +
                        "*unless system memory runs out\n\n" +
                        "Both are valid — choose based on\n" +
                        "whether you know the max size!",
                    waitForAction = false
                },
                new Step {
                    title = "Lesson 3 Complete!",
                    body  =
                        "You now understand both implementations:\n\n" +
                        "- Array: simple, fixed size, fast\n" +
                        "- Linked List: dynamic, pointer cost\n\n" +
                        "Both give O(1) for all operations.\n" +
                        "Space complexity is O(n) for both.\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 4: return new Step[]
            {
                new Step {
                    title = "Where Are Stacks Used?",
                    body  =
                        "Stacks are fundamental in computing!\n\n" +
                        "Five key applications:\n" +
                        "  1. Function Call Management\n" +
                        "  2. Recursion\n" +
                        "  3. Expression Evaluation\n" +
                        "  4. Syntax Parsing\n" +
                        "  5. Memory Management\n\n" +
                        "-> Place the scene, then use PUSH\n" +
                        "  and POP to explore each one.",
                    waitForAction = true
                },
                new Step {
                    title = "Function Calls & Recursion",
                    body  =
                        "FUNCTION CALL STACK:\n" +
                        "When function A calls B calls C:\n" +
                        "  Push A's state -> stack\n" +
                        "  Push B's state -> stack\n" +
                        "  Push C's state -> stack\n" +
                        "  C returns -> Pop C\n" +
                        "  B returns -> Pop B\n" +
                        "  A returns -> Pop A\n\n" +
                        "RECURSION works the same way!\n" +
                        "Each call stores local variables\n" +
                        "and its return address on the stack.\n\n" +
                        "-> PUSH to simulate a function call.",
                    waitForAction = false
                },
                new Step {
                    title = "Expression Evaluation",
                    body  =
                        "Stacks evaluate math expressions!\n\n" +
                        "INFIX:   3 + 4 x 2\n" +
                        "POSTFIX: 3 4 2 x +  <- stack-friendly\n\n" +
                        "Evaluating postfix '3 4 2 x +':\n" +
                        "  Push 3 -> stack: [3]\n" +
                        "  Push 4 -> stack: [3, 4]\n" +
                        "  Push 2 -> stack: [3, 4, 2]\n" +
                        "  x : Pop 2, Pop 4, push 4x2=8\n" +
                        "      stack: [3, 8]\n" +
                        "  + : Pop 8, Pop 3, push 3+8=11\n" +
                        "      stack: [11]\n\n" +
                        "Result = 11",
                    waitForAction = false
                },
                new Step {
                    title = "Syntax Parsing & Memory",
                    body  =
                        "BALANCED SYMBOLS CHECK:\n" +
                        "Stacks verify { ( [ ] ) }:\n" +
                        "  Open bracket  -> Push\n" +
                        "  Close bracket -> Pop and match\n" +
                        "  If mismatch -> syntax error!\n\n" +
                        "MEMORY MANAGEMENT:\n" +
                        "The call stack is a region of RAM.\n" +
                        "Local variables live here and are\n" +
                        "automatically removed (popped) when\n" +
                        "their function returns.\n\n" +
                        "-> Try POP to simulate returning\n" +
                        "  from a function or closing a bracket.",
                    waitForAction = false
                },
                new Step {
                    title = "Lesson 4 Complete!",
                    body  =
                        "You now understand stack applications:\n\n" +
                        "- Function call management (call stack)\n" +
                        "- Recursion (each call = one push)\n" +
                        "- Postfix expression evaluation\n" +
                        "- Balanced symbol checking\n" +
                        "- Memory management (stack region)\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 5: return new Step[]
            {
                new Step {
                    title = "Stack Complexity Summary",
                    body  =
                        "Perform any operation — the table\n" +
                        "highlights its complexity in real time.\n\n" +
                        "Try each operation at least once to\n" +
                        "light up every row of the table.\n\n" +
                        "Use INTERMEDIATE mode for Multi-Pop\n" +
                        "and Reverse to see O(n) in action!",
                    waitForAction = false
                },
                new Step {
                    title = "O(1) vs O(n)",
                    body  =
                        "O(1) — CONSTANT TIME\n" +
                        "  Instant, regardless of stack size.\n" +
                        "  Examples: Push, Pop, Peek\n\n" +
                        "O(n) — LINEAR TIME\n" +
                        "  Work doubles when stack doubles.\n" +
                        "  Examples: Multi-Pop n items,\n" +
                        "            Reverse entire stack\n\n" +
                        "The three basic stack operations are\n" +
                        "ALL O(1) — this is the key advantage\n" +
                        "of the stack data structure!",
                    waitForAction = false
                },
                new Step {
                    title = "Full Complexity Table",
                    body  =
                        "From the syllabus — complete summary:\n\n" +
                        "Push:         O(1)\n" +
                        "Pop:          O(1)\n" +
                        "Peek:         O(1)\n" +
                        "Multi-Pop(n): O(n)\n" +
                        "Reverse:      O(n)\n" +
                        "Search:       O(n)\n\n" +
                        "Space Complexity:  O(n)\n\n" +
                        "The simplicity of O(1) for core ops\n" +
                        "makes stacks highly efficient for\n" +
                        "LIFO-ordered problems.",
                    waitForAction = false
                },
                new Step {
                    title = "Lesson 5 Complete!",
                    body  =
                        "You have mastered Stacks!\n\n" +
                        "- LIFO / FILO definition\n" +
                        "- Characteristics & plate analogy\n" +
                        "- Push, Pop, Peek — O(1) each\n" +
                        "- Array vs Linked List implementation\n" +
                        "- Function calls, recursion, parsing\n" +
                        "- Full Big-O complexity table\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            default: return new Step[0];
        }
    }
}