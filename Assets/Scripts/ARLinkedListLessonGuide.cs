using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ARLinkedListLessonGuide.cs — SYNCED VERSION
/// =============================================================================
/// Adds SyncControllerUI() — same pattern as ARArrayLessonGuide:
///   GetPanelRoot(tmp)       walks two levels up to find the root panel.
///   SetControllerText()     shows the panel and sets the TMP text.
///   ShowControllerPanel()   shows the root panel.
///   HideControllerPanel()   hides the root panel.
///
/// SyncControllerUI(stepIndex) is called on every ShowStep() and after
/// OnSceneSpawned(). Per lesson mode it either sets lesson-relevant text
/// or hides the panel entirely when the guide card already covers it.
///
/// DelayedAssessmentStart hides all four controller overlays because the
/// assessment UI takes over completely.
///
/// OnReturn restores all four panels for sandbox / free-play use.
///
/// All other fixes (FIX 1-11) from the previous version are preserved.
/// </summary>
public class ARLinkedListLessonGuide : MonoBehaviour
{
    // ── Existing AR Controller ────────────────────────────────────────────────
    [Header("Your Existing LL Controller")]
    public InteractiveTrainList listController;

    // ── Existing panels to hide during passive lesson phases ──────────────────
    [Header("Existing Canvas Panels")]
    public GameObject mainButtonPanel;
    public GameObject beginnerButtonPanel;
    public GameObject intermediateButtonPanel;
    public GameObject carInputPanel;
    public GameObject insertAtInputPanel;
    public GameObject deleteValueInputPanel;
    public GameObject movementControlPanel;
    public GameObject confirmButton;

    // ── Guide Canvas ──────────────────────────────────────────────────────────
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

    // ── Assessment ────────────────────────────────────────────────────────────
    [Header("Assessment (ARLinkedListLessonAssessment component)")]
    public ARLinkedListLessonAssessment lessonAssessment;

    // ── Operation Feedback Panel ──────────────────────────────────────────────
    [Header("Operation Feedback Panel")]
    public GameObject      opFeedbackPanel;
    public TextMeshProUGUI opFeedbackTitle;
    public TextMeshProUGUI opFeedbackBody;
    public TextMeshProUGUI opComplexityBadge;
    public TextMeshProUGUI operationLogText;

    // ── Toast Notifications ───────────────────────────────────────────────────
    [Header("Toast Notifications")]
    public GameObject      toastPanel;
    public TextMeshProUGUI toastText;
    public Image           toastIcon;
    public Sprite          checkSprite;
    public Sprite          crossSprite;

    // ── Complexity Table ──────────────────────────────────────────────────────
    [Header("Complexity Table")]
    public GameObject      complexityTablePanel;
    public TextMeshProUGUI complexityTableText;
    public TextMeshProUGUI activeOperationLabel;

    // ── Settings ──────────────────────────────────────────────────────────────
    [Header("Settings")]
    public string mainAppSceneName = "MainScene";

    // ── State ─────────────────────────────────────────────────────────────────
    int  lessonIndex       = -1;
    int  guideMode         = -1;
    int  currentStep       = 0;
    bool nextBlocked       = false;
    bool sceneSpawned      = false;
    bool assessmentStarted = false;

    bool didAddHead     = false;
    bool didAddTail     = false;
    bool didRemoveHead  = false;
    bool didTraverse    = false;
    bool didInsertAt    = false;
    bool didDeleteValue = false;
    bool didReverse     = false;
    bool didFindMiddle  = false;

    int lastNodeCount = 0;

    readonly List<string> opLog = new List<string>();

    readonly string[] sceneRootNames = { "LinkedListScene" };
    GameObject spawnedScene;

    struct Step { public string title, body; public bool waitForAction; }
    Step[] steps;

    // ── Guards ────────────────────────────────────────────────────────────────
    private bool _started        = false;
    private bool _guideInited    = false;
    private bool _nextOnCooldown = false;

    // ─────────────────────────────────────────────────────────────────────────
    public void ResetInitFlag()
    {
        _guideInited = false;
        _started     = false;
        Debug.Log("[ARLinkedListLessonGuide] ResetInitFlag — ready for fresh init");
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (_started) return;
        _started = true;

        string topic = PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "");
        lessonIndex  = PlayerPrefs.GetInt(ARReturnHandler.AR_LESSON_INDEX_KEY, -1);

        if (!topic.ToLower().Contains("linked") || lessonIndex < 0)
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
            lessonAssessment = GetComponent<ARLinkedListLessonAssessment>();

        Debug.Log($"[LLGuide] Lesson {lessonIndex} -> Mode {guideMode} | Assessment: {(lessonAssessment != null ? "FOUND" : "NULL")}");
    }

    int LessonIndexToMode(int i)
    {
        switch (i)
        {
            case 0: return 12;
            case 1: return 12;
            case 2: return 3;
            case 3: return 4;
            default: return -1;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // InitGuide — called via SendMessage from ARModeSelectionManager
    // ─────────────────────────────────────────────────────────────────────────
    void InitGuide()
    {
        if (guideCanvas != null) guideCanvas.gameObject.SetActive(true);

        if (_guideInited) return;
        _guideInited = true;

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

        steps = BuildSteps(guideMode, lessonIndex);
        nextBlocked = true;

        // Silence the controller's own UI writes from the start — guide owns the text now
        listController?.SetInstructionSilence(true);

        ShowStep(0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!enabled) return;

        if (!sceneSpawned)
        {
            foreach (var name in sceneRootNames)
            {
                var go = GameObject.Find(name);
                if (go != null)
                {
                    spawnedScene = go;
                    sceneSpawned = true;
                    OnSceneSpawned();
                    break;
                }
            }
        }

        if (sceneSpawned && !assessmentStarted && (guideMode == 12 || guideMode == 3))
            EnsureOperationButtonsHidden();

        if (sceneSpawned && guideMode == 4)
            CheckOperationChange();
    }

    // ─────────────────────────────────────────────────────────────────────────
    void OnSceneSpawned()
    {
        Debug.Log($"[LLGuide] Scene detected: {spawnedScene.name}");

        if (guideMode == 4)
        {
            complexityTablePanel?.SetActive(true);
            UpdateComplexityTable("");
        }

        if (guideMode == 12 || guideMode == 3)
        {
            if (listController != null)
                listController.PreFillForLesson(4);
        }

        nextBlocked = false;
        UpdateNextButton();
        lastNodeCount = CountNodes();

        // Sync UI now that scene is placed
        SyncControllerUI(currentStep);
    }

    // ─────────────────────────────────────────────────────────────────────────
    void EnsureOperationButtonsHidden()
    {
        Hide(mainButtonPanel);
        Hide(beginnerButtonPanel);
        Hide(intermediateButtonPanel);
        Hide(carInputPanel);
        Hide(insertAtInputPanel);
        Hide(deleteValueInputPanel);
        Hide(movementControlPanel);
        Hide(confirmButton);
    }

    void Hide(GameObject go) { if (go != null && go.activeSelf) go.SetActive(false); }

    // =========================================================================
    // CONTROLLER UI SYNC
    // Called every ShowStep() and after OnSceneSpawned().
    // Sets instructionText / operationInfoText / statusText / detectionText
    // to lesson-relevant text, or hides their root panel when the guide covers it.
    // =========================================================================
    void SyncControllerUI(int stepIndex)
    {
        if (listController == null) return;

        // detectionText: only needed before scene is placed
        if (sceneSpawned)
            HideControllerPanel(listController.detectionText);
        else
            ShowControllerPanel(listController.detectionText);

        switch (guideMode)
        {
            // -----------------------------------------------------------------
            // Modes 12 — L1 Introduction & L2 Terminologies (passive)
            // Array is pre-filled, no operations allowed.
            // instructionText -> step-matched context hint.
            // operationInfoText -> hidden (guide card covers the theory).
            // statusText -> hidden (no operations being tracked).
            // -----------------------------------------------------------------
            case 12:
            {
                string hint;
                if (lessonIndex == 0) // L1 Introduction
                {
                    hint = stepIndex switch
                    {
                        0 => "Point your camera at a flat surface and tap to place the scene.",
                        1 => "Look at the nodes — each one holds DATA and a NEXT pointer.",
                        2 => "The nodes are NOT stored side-by-side in memory — they are linked by pointers.",
                        3 => "The list can grow and shrink at runtime — no fixed size needed.",
                        _ => "Assessment starting shortly..."
                    };
                }
                else // L2 Terminologies
                {
                    hint = stepIndex switch
                    {
                        0 => "Point your camera at a flat surface and tap to place the scene.",
                        1 => "The orange label marks the HEAD — the first node and entry point.",
                        2 => "The last node is the TAIL — its NEXT pointer equals NULL.",
                        3 => "NULL marks the end of the list — traversal stops here.",
                        4 => "Each node has DATA + NEXT (SLL) or DATA + NEXT + PREV (DLL).",
                        _ => "Assessment starting shortly..."
                    };
                }
                SetControllerText(listController.instructionText, hint);
                HideControllerPanel(listController.operationInfoText);
                HideControllerPanel(listController.statusText);
                break;
            }

            // -----------------------------------------------------------------
            // Mode 3 — L3 Types of Linked Lists
            // Pre-filled, traverse button active.
            // instructionText -> step context.
            // operationInfoText -> shown after traversal (controller fills it).
            // statusText -> hidden.
            // -----------------------------------------------------------------
            case 3:
            {
                string hint = stepIndex switch
                {
                    0 => "Point your camera at a flat surface and tap to place the scene.",
                    1 => "This scene shows a Singly Linked List — tap TRAVERSE to follow each NEXT pointer.",
                    2 => "Doubly Linked List adds a PREV pointer — each node links both ways.",
                    3 => "Circular SLL — the tail's NEXT points back to HEAD instead of NULL.",
                    4 => "Circular DLL — both NEXT and PREV wrap around. Navigate forwards and backwards.",
                    5 => "Choose the type based on whether you need backward traversal or circular access.",
                    _ => "Assessment starting shortly..."
                };
                SetControllerText(listController.instructionText, hint);
                ShowControllerPanel(listController.operationInfoText); // controller fills on traverse
                HideControllerPanel(listController.statusText);
                break;
            }

            // -----------------------------------------------------------------
            // Mode 4 — L4 Operations (full interactive)
            // instructionText -> mirrors the step's action prompt.
            // operationInfoText -> shown (complexity explanation per op).
            // statusText -> shown (node count useful here).
            // -----------------------------------------------------------------
            case 4:
            {
                string hint = stepIndex switch
                {
                    0  => "Point your camera at a flat surface and tap to place the scene.",
                    1  => "Tap TRAVERSE to visit every node from HEAD to NULL — O(n).",
                    2  => "Tap ADD HEAD to insert a new node at the front — O(1), no traversal!",
                    3  => "Tap ADD TAIL to insert at the end — O(n) without a tail pointer.",
                    4  => "INTERMEDIATE: tap INSERT AT and enter a position — O(n) traversal first.",
                    5  => "Tap REMOVE HEAD to delete the first node — O(1), just update the pointer.",
                    6  => "Delete from end requires traversal to second-last node — O(n).",
                    7  => "Delete at a given position: traverse to it, then re-link — O(n).",
                    8  => "INTERMEDIATE: tap DELETE BY VALUE to find and remove by content — O(n).",
                    9  => "Searching is always linear — you CANNOT binary search a linked list.",
                    10 => "Review the complexity table on screen — two O(1) ops are the LL's key strength.",
                    _ => "Assessment starting shortly..."
                };
                SetControllerText(listController.instructionText, hint);
                ShowControllerPanel(listController.operationInfoText);
                ShowControllerPanel(listController.statusText);
                break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PANEL ROOT HELPERS  (mirrors ARArrayLessonGuide exactly)
    // Hierarchy: RootPanel > Panel > TMP Text  (two levels up from the TMP)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the root panel that should be shown/hidden for a given TMP label.
    /// Walks two levels up from the TMP component. Falls back gracefully.
    /// </summary>
    GameObject GetPanelRoot(TextMeshProUGUI tmp)
    {
        if (tmp == null) return null;
        Transform t = tmp.transform.parent;       // Panel
        if (t != null && t.parent != null)
            return t.parent.gameObject;           // RootPanel
        if (t != null)
            return t.gameObject;                  // Panel (fallback)
        return tmp.gameObject;                    // Text itself (last resort)
    }

    /// <summary>Shows the root panel and writes text into the TMP. No-ops if null.</summary>
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
    // OPERATION DETECTION (L4 only)
    // ─────────────────────────────────────────────────────────────────────────
    void CheckOperationChange()
    {
        int cur = CountNodes();
        if (cur == lastNodeCount) return;
        bool inserted = cur > lastNodeCount;
        OnOperationDetected(inserted ? "INSERT" : "REMOVE");
        lastNodeCount = cur;
    }

    int CountNodes()
    {
        if (listController == null) return 0;
        return listController.GetPublicListLength();
    }

    void OnOperationDetected(string op)
    {
        string complexity = op switch
        {
            "INSERT_HEAD"  => "O(1) — update head pointer only",
            "INSERT_TAIL"  => "O(n) — traverse to find tail",
            "INSERT_AT"    => "O(n) — traverse to position",
            "REMOVE_HEAD"  => "O(1) — update head pointer only",
            "REMOVE_END"   => "O(n) — traverse to second-last",
            "REMOVE"       => "O(n) — search + re-link pointers",
            _              => "O(n)"
        };

        if (guideMode == 4)
        {
            opFeedbackPanel?.SetActive(true);
            if (opFeedbackTitle   != null) opFeedbackTitle.text   = op;
            if (opFeedbackBody    != null) opFeedbackBody.text    = OpExplanation(op);
            if (opComplexityBadge != null) opComplexityBadge.text = complexity;
            opLog.Insert(0, $"{op}  {complexity}");
            if (opLog.Count > 4) opLog.RemoveAt(4);
            if (operationLogText  != null) operationLogText.text  = string.Join("\n", opLog);
            StartCoroutine(HideAfter(opFeedbackPanel, 3.5f));
            UpdateComplexityTable(op);
        }
    }

    string OpExplanation(string op)
    {
        switch (op)
        {
            case "INSERT_HEAD":  return "Insert at Beginning:\nnew_node.next = head\nhead = new_node\nTime: O(1) — no traversal!";
            case "INSERT_TAIL":  return "Insert at End:\nTraverse until current.next == null\ncurrent.next = new_node\nTime: O(n) if no tail pointer";
            case "INSERT_AT":    return "Insert at Given Position:\nTraverse to position - 1\nnew_node.next = current.next\ncurrent.next = new_node\nTime: O(n)";
            case "REMOVE_HEAD":  return "Delete from Beginning:\nhead = head.next\nTime: O(1) — instant!";
            case "REMOVE_END":   return "Delete from End:\nTraverse to second-last node\nsecond_last.next = None\nTime: O(n)";
            case "REMOVE":       return "Delete by Value:\nSearch for node with matching value\nprev.next = current.next\nTime: O(n)";
            default:             return "Operation detected.";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC NOTIFY METHODS — called by InteractiveTrainList
    // Assessment forwarding is ALWAYS done outside the didXxx guard.
    // The guard only protects TryUnblock() (guide-phase step unlocking).
    // ─────────────────────────────────────────────────────────────────────────
    public void NotifyAddHead()
    {
        if (!didAddHead) { didAddHead = true; TryUnblock(); }
        lessonAssessment?.NotifyAddHead();
        OnOperationDetected("INSERT_HEAD");
        ShowToast(checkSprite, "Added at HEAD — O(1)!");

        // Live update instructionText for L4
        if (guideMode == 4 && listController?.instructionText != null)
        {
            GetPanelRoot(listController.instructionText)?.SetActive(true);
            listController.instructionText.text  = "ADD HEAD done — O(1)! No traversal needed. Now try ADD TAIL.";
            listController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyAddTail()
    {
        if (!didAddTail) { didAddTail = true; TryUnblock(); }
        lessonAssessment?.NotifyAddTail();
        OnOperationDetected("INSERT_TAIL");
        ShowToast(checkSprite, "Added at TAIL — O(n)");

        if (guideMode == 4 && listController?.instructionText != null)
        {
            GetPanelRoot(listController.instructionText)?.SetActive(true);
            listController.instructionText.text  = "ADD TAIL done — O(n) traversal to find end. Now try INSERT AT.";
            listController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyRemoveHead()
    {
        if (!didRemoveHead) { didRemoveHead = true; TryUnblock(); }
        lessonAssessment?.NotifyRemoveHead();
        OnOperationDetected("REMOVE_HEAD");
        ShowToast(checkSprite, "Removed HEAD — O(1)!");

        if (guideMode == 4 && listController?.instructionText != null)
        {
            GetPanelRoot(listController.instructionText)?.SetActive(true);
            listController.instructionText.text  = "REMOVE HEAD done — O(1)! Now try DELETE BY VALUE.";
            listController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyTraverse()
    {
        if (!didTraverse) { didTraverse = true; TryUnblock(); }
        lessonAssessment?.NotifyTraverse();
        OnOperationDetected("TRAVERSE");
        UpdateComplexityTable("TRAVERSE");

        if (guideMode == 4 && listController?.instructionText != null)
        {
            GetPanelRoot(listController.instructionText)?.SetActive(true);
            listController.instructionText.text  = "TRAVERSE done — visited every node O(n). Now try ADD HEAD.";
            listController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyInsertAt()
    {
        if (!didInsertAt) { didInsertAt = true; TryUnblock(); }
        lessonAssessment?.NotifyInsertAt();
        OnOperationDetected("INSERT_AT");

        if (guideMode == 4 && listController?.instructionText != null)
        {
            GetPanelRoot(listController.instructionText)?.SetActive(true);
            listController.instructionText.text  = "INSERT AT done — traversal to position, then re-link O(n). Now try REMOVE HEAD.";
            listController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyDeleteByValue()
    {
        if (!didDeleteValue) { didDeleteValue = true; TryUnblock(); }
        lessonAssessment?.NotifyDeleteByValue();
        OnOperationDetected("REMOVE");

        if (guideMode == 4 && listController?.instructionText != null)
        {
            GetPanelRoot(listController.instructionText)?.SetActive(true);
            listController.instructionText.text  = "DELETE BY VALUE done — searched list, re-linked pointers O(n). Try REVERSE or FIND MIDDLE!";
            listController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyReverse()
    {
        if (!didReverse) { didReverse = true; TryUnblock(); }
        lessonAssessment?.NotifyReverse();
        UpdateComplexityTable("REVERSE");
        ShowToast(checkSprite, "List Reversed — O(n)");

        if (guideMode == 4 && listController?.instructionText != null)
        {
            GetPanelRoot(listController.instructionText)?.SetActive(true);
            listController.instructionText.text  = "REVERSE done — all pointers flipped O(n). Now try FIND MIDDLE!";
            listController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyFindMiddle()
    {
        if (!didFindMiddle) { didFindMiddle = true; TryUnblock(); }
        lessonAssessment?.NotifyFindMiddle();
        UpdateComplexityTable("FIND_MID");
        ShowToast(checkSprite, "Middle Found — Floyd's O(n)");

        if (guideMode == 4 && listController?.instructionText != null)
        {
            GetPanelRoot(listController.instructionText)?.SetActive(true);
            listController.instructionText.text  = "FIND MIDDLE done — Floyd's two-pointer, O(n) time O(1) space!";
            listController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
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

        nextBlocked = s.waitForAction;
        if (nextBlocked) CheckIfAlreadyDone(index);

        if (!nextBlocked)
            StartCoroutine(EnableNextAfterDelay(0.6f));
        else
            UpdateNextButton();

        // Sync controller UI panels to this step
        SyncControllerUI(index);

        Debug.Log($"[LLGuide] ShowStep {index}, isLast={isLast}, assessmentStarted={assessmentStarted}");

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

        // enabled=false stops the button-hiding loop from racing the assessment
        enabled = false;

        // Un-silence the controller so assessment UI can write freely if it needs to
        listController?.SetInstructionSilence(false);

        guideCardPanel?.SetActive(false);
        collapsedTab?.SetActive(false);
        opFeedbackPanel?.SetActive(false);
        complexityTablePanel?.SetActive(false);
        toastPanel?.SetActive(false);

        if (listController != null)
        {
            listController.mainButtonPanel?.SetActive(false);
            listController.beginnerButtonPanel?.SetActive(false);
            listController.intermediateButtonPanel?.SetActive(false);
            listController.carInputPanel?.SetActive(false);
            listController.insertAtInputPanel?.SetActive(false);
            listController.deleteValueInputPanel?.SetActive(false);
            listController.movementControlPanel?.SetActive(false);

            // Hide all four controller overlays — assessment UI takes over
            HideControllerPanel(listController.instructionText);
            HideControllerPanel(listController.operationInfoText);
            HideControllerPanel(listController.statusText);
            HideControllerPanel(listController.detectionText);
        }

        if ((lesson == 0 || lesson == 1 || lesson == 2) && listController != null)
            listController.PreFillForLesson(4);

        if (lessonAssessment.assessmentRoot != null)
            lessonAssessment.assessmentRoot.SetActive(true);

        lessonAssessment.BeginAssessment(lesson);
    }

    void CheckIfAlreadyDone(int stepIndex)
    {
        switch (guideMode)
        {
            case 12:
                if (stepIndex == 0) nextBlocked = !sceneSpawned;
                break;

            case 3:
                if (stepIndex == 0) nextBlocked = !sceneSpawned;
                if (stepIndex == 1 && didTraverse) nextBlocked = false;
                break;

            case 4:
                if (stepIndex == 0)                   nextBlocked = !sceneSpawned;
                if (stepIndex == 1 && didTraverse)    nextBlocked = false;
                if (stepIndex == 2 && didAddHead)     nextBlocked = false;
                if (stepIndex == 3 && didAddTail)     nextBlocked = false;
                if (stepIndex == 4 && didInsertAt)    nextBlocked = false;
                if (stepIndex == 5 && didRemoveHead)  nextBlocked = false;
                if (stepIndex == 8 && didDeleteValue) nextBlocked = false;
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

    void TryUnblock() { if (nextBlocked) { nextBlocked = false; _nextOnCooldown = false; UpdateNextButton(); } }

    void UpdateNextButton()
    {
        if (nextStepButton == null) return;
        nextStepButton.interactable = !nextBlocked;
        if (nextStepButtonLabel != null)
            nextStepButtonLabel.text = nextBlocked ? "Complete the action above" : "Next ->";
    }

    void OnMinimise() { guideCardPanel?.SetActive(false); collapsedTab?.SetActive(true); }
    void OnRestore()  { guideCardPanel?.SetActive(true);  collapsedTab?.SetActive(false); }

    void OnReturn()
    {
        // Restore silence=false and show all controller panels for sandbox / free-play
        listController?.SetInstructionSilence(false);
        if (listController != null)
        {
            ShowControllerPanel(listController.instructionText);
            ShowControllerPanel(listController.operationInfoText);
            ShowControllerPanel(listController.detectionText);
            ShowControllerPanel(listController.statusText);
        }

        PlayerPrefs.SetInt(ARReturnHandler.AR_JUST_RETURNED_KEY, 1);
        PlayerPrefs.SetString(ARReturnHandler.AR_TOPIC_NAME_KEY,
                              PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "linkedlist"));
        PlayerPrefs.Save();
        SceneManager.LoadScene(PlayerPrefs.GetString("AR_MainScene", mainAppSceneName));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COMPLEXITY TABLE
    // ─────────────────────────────────────────────────────────────────────────
    void UpdateComplexityTable(string activeOp)
    {
        string H(string op, string row) => activeOp == op ? $"> {row}" : $"  {row}";

        string t =
            "Operation               | Time\n" +
            "──────────────────────────────────\n" +
            H("ACCESS",      "Access by Index        | O(n)") + "\n" +
            H("TRAVERSE",    "Traversal              | O(n)") + "\n" +
            H("SEARCH",      "Linear Search          | O(n)") + "\n" +
            H("INSERT_HEAD", "Insert at Beginning    | O(1)") + "\n" +
            H("INSERT_TAIL", "Insert at End          | O(n)") + "\n" +
            H("INSERT_AT",   "Insert at Position     | O(n)") + "\n" +
            H("REMOVE_HEAD", "Delete from Beginning  | O(1)") + "\n" +
            H("REMOVE_END",  "Delete from End        | O(n)") + "\n" +
            H("REMOVE",      "Delete by Value        | O(n)") + "\n" +
            H("REVERSE",     "Reverse List           | O(n)") + "\n" +
            H("FIND_MID",    "Find Middle (Floyd's)  | O(n)") + "\n" +
            "  Space Complexity      | O(n)";

        if (complexityTableText  != null) complexityTableText.text  = t;
        if (activeOperationLabel != null)
            activeOperationLabel.text = string.IsNullOrEmpty(activeOp) ? "" : $"Last: {activeOp}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TOAST & HELPERS
    // ─────────────────────────────────────────────────────────────────────────
    void ShowToast(Sprite icon, string msg)
    {
        if (toastPanel == null) return;
        if (toastText != null) toastText.text = msg;
        if (toastIcon != null && icon != null) toastIcon.sprite = icon;
        toastPanel.SetActive(true);
        StartCoroutine(HideAfter(toastPanel, 3.5f));
    }

    IEnumerator HideAfter(GameObject go, float t)
    { yield return new WaitForSeconds(t); go?.SetActive(false); }

    // ─────────────────────────────────────────────────────────────────────────
    // LESSON STEPS
    // ─────────────────────────────────────────────────────────────────────────
    Step[] BuildSteps(int mode, int lesson)
    {
        switch (mode)
        {
            case 12: return lesson == 0 ? BuildL1Steps() : BuildL2Steps();
            case 3:  return BuildL3Steps();
            case 4:  return BuildL4Steps();
            default: return new Step[0];
        }
    }

    Step[] BuildL1Steps() => new Step[]
    {
        new Step {
            title = "What is a Linked List?",
            body  =
                "A Linked List is a LINEAR data structure\n" +
                "where elements are stored in separate objects\n" +
                "called NODES.\n\n" +
                "Unlike arrays, nodes do NOT sit in\n" +
                "contiguous memory — they are spread out\n" +
                "and connected using POINTERS.\n\n" +
                "-> Choose your Scenario and Difficulty.\n" +
                "-> Tap a flat surface to place the scene.",
            waitForAction = true
        },
        new Step {
            title = "Node Structure",
            body  =
                "Every node contains exactly two parts:\n\n" +
                "  DATA  — the value stored in the node\n" +
                "  NEXT  — a reference (pointer) to the\n" +
                "           next node in the sequence\n\n" +
                "Structure diagram:\n" +
                "  [ data | next ] -> [ data | next ] -> NULL\n\n" +
                "The last node's NEXT pointer is NULL,\n" +
                "marking the end of the list.",
            waitForAction = false
        },
        new Step {
            title = "Linked List vs Array",
            body  =
                "Feature        Array     Linked List\n" +
                "─────────────────────────────────────\n" +
                "Memory layout  Contiguous  Non-contiguous\n" +
                "Access by [i]  O(1)        O(n)\n" +
                "Insert at beg  O(n)        O(1)\n" +
                "Delete at beg  O(n)        O(1)\n" +
                "Size           Fixed       Dynamic\n" +
                "Memory extra   Low         High (pointers)\n\n" +
                "Key trade-off:\n" +
                "Arrays = fast access, slow insert/delete.\n" +
                "Linked Lists = slow access, fast insert/delete.",
            waitForAction = false
        },
        new Step {
            title = "Dynamic Size",
            body  =
                "Arrays must declare a fixed size up front.\n" +
                "A linked list GROWS and SHRINKS at runtime:\n\n" +
                "  - No pre-allocated block needed.\n" +
                "  - Each node is created on demand.\n" +
                "  - Memory is freed when nodes are deleted.\n\n" +
                "Python example:\n" +
                "  class Node:\n" +
                "      def __init__(self, data):\n" +
                "          self.data = data\n" +
                "          self.next = None\n\n" +
                "The scene shows dynamic linking in action!",
            waitForAction = false
        },
        new Step {
            title = "Lesson 1 Complete!",
            body  =
                "You now understand:\n" +
                "- What a Linked List is\n" +
                "- Each node holds data + a next pointer\n" +
                "- Nodes are NOT stored contiguously\n" +
                "- Dynamic size — no fixed capacity\n" +
                "- Key differences vs arrays\n\n" +
                "Your assessment will begin shortly...",
            waitForAction = false
        },
    };

    Step[] BuildL2Steps() => new Step[]
    {
        new Step {
            title = "Key Terminologies",
            body  =
                "Before exploring operations, learn the\n" +
                "four essential linked list terms:\n\n" +
                "  HEAD, TAIL, NULL, NODE\n\n" +
                "-> Choose your Scenario and Difficulty.\n" +
                "-> Tap a flat surface to place the scene.\n\n" +
                "Watch the labels appear on each node\n" +
                "as you interact with the list.",
            waitForAction = true
        },
        new Step {
            title = "HEAD",
            body  =
                "HEAD — the FIRST node of the linked list.\n\n" +
                "  - All traversals begin here.\n" +
                "  - The list can only be accessed from head.\n" +
                "  - If head is NULL -> the list is EMPTY.\n\n" +
                "In the scene, the HEAD node is labelled\n" +
                "in orange above the first node.\n\n" +
                "Python example:\n" +
                "  self.head = None      # empty list\n" +
                "  self.head = Node(10)  # one-node list",
            waitForAction = false
        },
        new Step {
            title = "TAIL",
            body  =
                "TAIL — the LAST node of the linked list.\n\n" +
                "  - Its NEXT pointer equals NULL.\n" +
                "  - Adding a tail pointer lets us insert\n" +
                "    at the end in O(1) instead of O(n).\n\n" +
                "Without a tail pointer, you must traverse\n" +
                "the whole list to find the last node.\n\n" +
                "  current = head\n" +
                "  while current.next:\n" +
                "      current = current.next\n" +
                "  # current is now the tail",
            waitForAction = false
        },
        new Step {
            title = "NULL (End Marker)",
            body  =
                "NULL — marks the END of the list.\n\n" +
                "  - The tail node's next pointer = NULL.\n" +
                "  - Traversal stops when next == NULL.\n" +
                "  - Circular lists do NOT use NULL —\n" +
                "    the tail points back to the head instead.\n\n" +
                "In Python: None\n" +
                "In Java/C#: null\n" +
                "In C/C++: NULL\n\n" +
                "Dereferencing NULL causes a crash —\n" +
                "always check before moving to next!",
            waitForAction = false
        },
        new Step {
            title = "NODE Anatomy",
            body  =
                "A NODE is the building block of a linked list.\n" +
                "Each node stores two things:\n\n" +
                "  DATA  — the value (int, string, object, etc.)\n" +
                "  NEXT  — pointer to the next node\n\n" +
                "Doubly linked lists add a PREV pointer too:\n\n" +
                "  PREV  — pointer to the previous node\n\n" +
                "Memory:\n" +
                "  SLL node = data + 1 pointer\n" +
                "  DLL node = data + 2 pointers (more memory!)\n\n" +
                "The scenes show node labels [index]\n" +
                "and cargo (data) on each node.",
            waitForAction = false
        },
        new Step {
            title = "Lesson 2 Complete!",
            body  =
                "You now understand:\n" +
                "- HEAD — first node, entry point\n" +
                "- TAIL — last node, next = NULL\n" +
                "- NULL — end-of-list sentinel\n" +
                "- NODE — data + next pointer(s)\n\n" +
                "These four terms are the foundation\n" +
                "for all linked list operations!\n\n" +
                "Your assessment will begin shortly...",
            waitForAction = false
        },
    };

    Step[] BuildL3Steps() => new Step[]
    {
        new Step {
            title = "Types of Linked Lists",
            body  =
                "There are four types of linked lists,\n" +
                "each with different pointer structures:\n\n" +
                "  1. Singly Linked List  (SLL)\n" +
                "  2. Doubly Linked List  (DLL)\n" +
                "  3. Circular Singly     (CSLL)\n" +
                "  4. Circular Doubly     (CDLL)\n\n" +
                "-> Choose your Scenario and Difficulty.\n" +
                "-> Tap a flat surface to place the scene.\n\n" +
                "Try TRAVERSE to watch each connection!",
            waitForAction = true
        },
        new Step {
            title = "1. Singly Linked List (SLL)",
            body  =
                "The SIMPLEST type — each node has ONE pointer.\n\n" +
                "Structure:\n" +
                "  HEAD->[data|next]->[data|next]->NULL\n\n" +
                "Characteristics:\n" +
                "- Traversal: forward only\n" +
                "- Less memory (one pointer per node)\n" +
                "- Simple to implement\n" +
                "- Cannot go backwards\n\n" +
                "Used in: stacks, queues, basic dynamic lists.\n\n" +
                "-> Tap TRAVERSE to watch the scene.",
            waitForAction = true
        },
        new Step {
            title = "2. Doubly Linked List (DLL)",
            body  =
                "Each node has TWO pointers: next AND prev.\n\n" +
                "Structure:\n" +
                "  NULL<-[prev|data|next]<->[prev|data|next]->NULL\n\n" +
                "Characteristics:\n" +
                "- Traverse FORWARD and BACKWARD\n" +
                "- Easier deletion (no need for prev node ref)\n" +
                "- More memory — extra pointer per node\n" +
                "- More complex pointer management\n\n" +
                "Used in: browser history, music players,\n" +
                "         undo/redo systems.",
            waitForAction = false
        },
        new Step {
            title = "3. Circular Singly Linked List (CSLL)",
            body  =
                "Like SLL but the TAIL points back to HEAD.\n" +
                "There is NO NULL at the end.\n\n" +
                "Structure:\n" +
                "  HEAD -> A -> B -> C\n" +
                "   ^________________|\n\n" +
                "Traversal stops when you return to HEAD.\n\n" +
                "Advantages:\n" +
                "- No NULL pointer — no end-of-list check\n" +
                "- Great for round-robin scheduling\n" +
                "- Cannot move backwards\n" +
                "- Harder to delete previous nodes",
            waitForAction = false
        },
        new Step {
            title = "4. Circular Doubly Linked List (CDLL)",
            body  =
                "Combines DLL + Circular — both next AND prev,\n" +
                "and the list wraps around in BOTH directions.\n\n" +
                "Structure:\n" +
                "  tail.next -> head\n" +
                "  head.prev -> tail\n\n" +
                "Advantages:\n" +
                "- Traverse forward AND backward\n" +
                "- Easier insertion/deletion from both ends\n" +
                "- Useful for navigation systems\n" +
                "- Most memory usage (2 pointers per node)\n" +
                "- Most complex pointer management",
            waitForAction = false
        },
        new Step {
            title = "Choosing the Right Type",
            body  =
                "Quick reference — which type to use:\n\n" +
                "SLL  — Simple lists, stacks, queues.\n" +
                "       Low memory, forward-only.\n\n" +
                "DLL  — Need backward traversal or\n" +
                "       frequent deletion by reference.\n\n" +
                "CSLL — Circular processes (round-robin,\n" +
                "       CPU scheduling, media loops).\n\n" +
                "CDLL — Navigation systems (browser tabs,\n" +
                "       music playlists with prev/next).",
            waitForAction = false
        },
        new Step {
            title = "Lesson 3 Complete!",
            body  =
                "You now understand all four types:\n" +
                "- SLL  — one pointer, forward only\n" +
                "- DLL  — two pointers, bidirectional\n" +
                "- CSLL — circular, no NULL, forward only\n" +
                "- CDLL — circular + bidirectional\n\n" +
                "Trade-off summary:\n" +
                "  More pointers = more flexibility,\n" +
                "  but more memory and complexity.\n\n" +
                "Your assessment will begin shortly...",
            waitForAction = false
        },
    };

    Step[] BuildL4Steps() => new Step[]
    {
        new Step {
            title = "Section C: Operations Overview",
            body  =
                "Linked lists support four operations:\n\n" +
                "  1. TRAVERSAL  O(n)\n" +
                "  2. INSERTION  — 3 cases  O(1) or O(n)\n" +
                "  3. DELETION   — 4 cases  O(1) or O(n)\n" +
                "  4. SEARCHING  O(n) linear only\n\n" +
                "The complexity table updates live as\n" +
                "you perform each operation.\n\n" +
                "-> Place the scene, then follow each step!",
            waitForAction = true
        },
        new Step {
            title = "1. Traversal  O(n)",
            body  =
                "Visit each node starting from the head.\n\n" +
                "  current = head\n" +
                "  while current is not None:\n" +
                "      print(current.data)\n" +
                "      current = current.next\n\n" +
                "Time: O(n)    Space: O(1)\n\n" +
                "-> Tap TRAVERSE to demonstrate.",
            waitForAction = true
        },
        new Step {
            title = "2A. Insert at Beginning  O(1)",
            body  =
                "  1. new_node.next = head\n" +
                "  2. head = new_node\n\n" +
                "Time: O(1) — no traversal needed!\n\n" +
                "-> Tap ADD HEAD to demonstrate.",
            waitForAction = true
        },
        new Step {
            title = "2B. Insert at End  O(n) / O(1)",
            body  =
                "  1. Traverse to the last node\n" +
                "  2. Set last_node.next = new_node\n\n" +
                "Time: O(n) — no tail pointer\n" +
                "      O(1) — if tail pointer maintained\n\n" +
                "-> Tap ADD TAIL to demonstrate.",
            waitForAction = true
        },
        new Step {
            title = "2C. Insert at Given Position  O(n)",
            body  =
                "  1. Traverse to position - 1\n" +
                "  2. new_node.next = current.next\n" +
                "  3. current.next = new_node\n\n" +
                "Time: O(n)  No element shifting!\n\n" +
                "-> INTERMEDIATE -> INSERT AT.",
            waitForAction = true
        },
        new Step {
            title = "3A. Delete from Beginning  O(1)",
            body  =
                "  head = head.next\n\n" +
                "Time: O(1) — fastest deletion!\n" +
                "Arrays need O(n) for same op.\n\n" +
                "-> Tap REMOVE HEAD to demonstrate.",
            waitForAction = true
        },
        new Step {
            title = "3B. Delete from End  O(n)",
            body  =
                "  1. Traverse to the second-last node\n" +
                "  2. Set second_last.next = NULL\n\n" +
                "Time: O(n) — must traverse to find tail.\n\n" +
                "No memory shifting — only pointer updates!",
            waitForAction = false
        },
        new Step {
            title = "3C. Delete at Given Position  O(n)",
            body  =
                "  1. Traverse to position - 1\n" +
                "  2. prev.next = current.next\n\n" +
                "This skips the target node entirely.\n" +
                "Time: O(n) — traversal to position.\n\n" +
                "No element shifting unlike arrays!",
            waitForAction = false
        },
        new Step {
            title = "3D. Delete First Occurrence  O(n)",
            body  =
                "  1. Search for the key\n" +
                "  2. prev.next = current.next\n\n" +
                "Time: O(n) — must search the list.\n\n" +
                "-> INTERMEDIATE -> DELETE BY VALUE.",
            waitForAction = true
        },
        new Step {
            title = "4. Searching  O(n)",
            body  =
                "Linked lists use LINEAR SEARCH only.\n\n" +
                "  current = self.head\n" +
                "  while current:\n" +
                "      if current.data == key: return True\n" +
                "      current = current.next\n" +
                "  return False\n\n" +
                "Binary Search NOT possible —\n" +
                "  cannot access middle directly!",
            waitForAction = false
        },
        new Step {
            title = "Complexity Summary",
            body  =
                "Operation              Time\n" +
                "──────────────────────────────\n" +
                "Access by index        O(n)\n" +
                "Traversal              O(n)\n" +
                "Insert at beginning    O(1)\n" +
                "Insert at end          O(n)\n" +
                "Delete at beginning    O(1)\n" +
                "Delete at end          O(n)\n" +
                "Searching              O(n)\n" +
                "Space Complexity       O(n)\n\n" +
                "Two O(1) ops are LL's key strength:\n" +
                "  -> Insert at beginning\n" +
                "  -> Delete at beginning",
            waitForAction = false
        },
        new Step {
            title = "Section C Complete!",
            body  =
                "You performed all operations!\n\n" +
                "Traversal:            O(n)\n" +
                "Insert at beginning:  O(1) — LL win!\n" +
                "Insert at end:        O(n)\n" +
                "Insert at position:   O(n)\n" +
                "Delete at beginning:  O(1) — LL win!\n" +
                "Delete at end:        O(n)\n" +
                "Delete at position:   O(n)\n" +
                "Delete first occur.:  O(n)\n" +
                "Searching:            O(n)\n" +
                "Space:                O(n)\n\n" +
                "Your assessment will begin shortly...",
            waitForAction = false
        },
    };
}