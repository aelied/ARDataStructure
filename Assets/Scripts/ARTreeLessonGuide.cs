using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ARTreeLessonGuide.cs — SYLLABUS-ALIGNED VERSION
/// =================================================
/// UI SYNC FIX:
/// SyncControllerUI(stepIndex) is called on every ShowStep() and again after
/// OnSceneSpawned(). It sets instructionText / operationInfoText / statusText /
/// detectionText to lesson-relevant content, or hides the whole panel when the
/// guide card already covers that information.
///
/// Panel hierarchy assumed: RootPanel > Panel > TMP
/// GetPanelRoot() walks two levels up from the TMP to find the root panel so
/// the entire card (background, border, children) hides/shows together.
/// </summary>
public class ARTreeLessonGuide : MonoBehaviour
{
    [Header("Your Existing AR Controller")]
    public InteractiveFamilyTree treeController;

    [Header("Existing Canvas Panels")]
    public GameObject mainButtonPanel;
    public GameObject beginnerButtonPanel;
    public GameObject intermediateButtonPanel;
    public GameObject personInputPanel;
    public GameObject traversalPanel;
    public GameObject directionPanel;
    public GameObject searchInputPanel;
    public GameObject deleteInputPanel;

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
    public ARTreeLessonAssessment lessonAssessment;

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

    bool didInOrder    = false;
    bool didPreOrder   = false;
    bool didPostOrder  = false;
    bool didSearch     = false;
    bool didDelete     = false;
    bool didHeight     = false;
    bool didAddChild   = false;

    int  lastNodeCount = 0;

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
        Debug.Log("[ARTreeLessonGuide] ResetInitFlag — ready for fresh init");
    }

    // ──────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (_started) return;
        _started = true;

        string topic = PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "");
        lessonIndex  = PlayerPrefs.GetInt(ARReturnHandler.AR_LESSON_INDEX_KEY, -1);

        if (!topic.ToLower().Contains("tree") || lessonIndex < 0)
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
            lessonAssessment = GetComponent<ARTreeLessonAssessment>();

        Debug.Log($"[ARTreeLessonGuide] Lesson {lessonIndex} -> Mode {guideMode}");
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
    // InitGuide — called via SendMessage from ARModeSelectionManager
    // ──────────────────────────────────────────────────────────────────────────
    void InitGuide()
    {
        if (guideCanvas != null) guideCanvas.gameObject.SetActive(true);
        if (_guideInited) return;
        _guideInited = true;

        // Silence controller overlays immediately — guide card takes over
        if (treeController != null)
            treeController.SetInstructionSilence(true);

        complexityTablePanel?.SetActive(false);
        collapsedTab?.SetActive(false);
        returnButton?.gameObject.SetActive(false);
        guideCardPanel?.SetActive(true);
        toastPanel?.SetActive(false);

        nextStepButton?.onClick.AddListener(OnNext);
        returnButton?.onClick.AddListener(OnReturn);
        minimiseButton?.onClick.AddListener(OnMinimise);
        restoreButton?.onClick.AddListener(OnRestore);

        steps = BuildSteps(guideMode);
        nextBlocked = true;
        ShowStep(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!enabled) return;

        if (!sceneSpawned && treeController != null)
        {
            if (treeController.GetNodeCount() > 0)
            {
                sceneSpawned = true;
                OnSceneSpawned();
            }
        }

        if (sceneSpawned) CheckTreeChanges();
    }

    void OnSceneSpawned()
    {
        Debug.Log("[ARTreeLessonGuide] Tree scene detected.");

        if (guideMode == 5)
        {
            complexityTablePanel?.SetActive(true);
            UpdateComplexityTable("");
        }

        nextBlocked = false;
        UpdateNextButton();
        lastNodeCount = treeController != null ? treeController.GetNodeCount() : 0;

        // Re-sync controller UI now that the scene is placed
        SyncControllerUI(currentStep);
    }

    void CheckTreeChanges()
    {
        if (treeController == null) return;
        int cur = treeController.GetNodeCount();
        if (cur > lastNodeCount && !didAddChild)
        {
            didAddChild = true;
            TryUnblock();
        }
        lastNodeCount = cur;
    }

    // ── Notification hooks ─────────────────────────────────────────────────────
    public void NotifyInOrderCompleted()
    {
        if (!didInOrder) { didInOrder = true; TryUnblock(); }
        if (lessonAssessment != null) lessonAssessment.NotifyTraversal("inorder");
        UpdateComplexityTable("INORDER");
        ShowToast(checkSprite, "In-Order Complete! Left->Root->Right");

        // Update instructionText live to prompt next action
        if (treeController?.instructionText != null)
        {
            GetPanelRoot(treeController.instructionText)?.SetActive(true);
            treeController.instructionText.text  = "In-Order done. Try PRE-ORDER next to compare visit order.";
            treeController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyPreOrderCompleted()
    {
        if (!didPreOrder) { didPreOrder = true; TryUnblock(); }
        if (lessonAssessment != null) lessonAssessment.NotifyTraversal("preorder");
        UpdateComplexityTable("PREORDER");
        ShowToast(checkSprite, "Pre-Order Complete! Root->Left->Right");

        if (treeController?.instructionText != null)
        {
            GetPanelRoot(treeController.instructionText)?.SetActive(true);
            treeController.instructionText.text  = "Pre-Order done. Try POST-ORDER to complete all three.";
            treeController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyPostOrderCompleted()
    {
        if (!didPostOrder) { didPostOrder = true; TryUnblock(); }
        if (lessonAssessment != null) lessonAssessment.NotifyTraversal("postorder");
        UpdateComplexityTable("POSTORDER");
        ShowToast(checkSprite, "Post-Order Complete! Left->Right->Root");

        if (treeController?.instructionText != null)
        {
            GetPanelRoot(treeController.instructionText)?.SetActive(true);
            treeController.instructionText.text  = "All traversals done. Tap Next when ready.";
            treeController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifySearchCompleted(bool found)
    {
        if (!didSearch) { didSearch = true; TryUnblock(); }
        if (lessonAssessment != null) lessonAssessment.NotifySearch(found);
        UpdateComplexityTable("SEARCH");

        if (treeController?.instructionText != null)
        {
            GetPanelRoot(treeController.instructionText)?.SetActive(true);
            treeController.instructionText.text  = found
                ? "Search done. Now tap DELETE to remove a non-root node."
                : "Not found. Try searching for a name that exists, then tap DELETE.";
            treeController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyDeleteCompleted()
    {
        if (!didDelete) { didDelete = true; TryUnblock(); }
        if (lessonAssessment != null) lessonAssessment.NotifyDelete();
        UpdateComplexityTable("DELETE");

        if (treeController?.instructionText != null)
        {
            GetPanelRoot(treeController.instructionText)?.SetActive(true);
            treeController.instructionText.text  = "Delete done. Now tap TREE HEIGHT to compute height.";
            treeController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyHeightCompleted(int height)
    {
        if (!didHeight) { didHeight = true; TryUnblock(); }
        if (lessonAssessment != null) lessonAssessment.NotifyHeight(height);
        UpdateComplexityTable("HEIGHT");
        ShowToast(checkSprite, $"Tree Height: {height} levels - O(n)");

        if (treeController?.instructionText != null)
        {
            GetPanelRoot(treeController.instructionText)?.SetActive(true);
            treeController.instructionText.text  = $"Height = {height}. All operations done. Tap Next when ready.";
            treeController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    public void NotifyChildAdded()
    {
        if (lessonAssessment != null) lessonAssessment.NotifyChildAdded();

        // Update instructionText live after a node is placed
        if (treeController?.instructionText != null)
        {
            GetPanelRoot(treeController.instructionText)?.SetActive(true);
            treeController.instructionText.text  = "Node added! Tap Next to continue.";
            treeController.instructionText.color = new Color(0.4f, 1f, 0.5f);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CONTROLLER UI SYNC
    // Called on every ShowStep() and again after OnSceneSpawned().
    // Sets instructionText / operationInfoText / statusText / detectionText
    // to lesson-relevant text, or hides the panel when the guide card covers it.
    // ──────────────────────────────────────────────────────────────────────────
    void SyncControllerUI(int stepIndex)
    {
        if (treeController == null) return;

        // detectionText: only useful before scene is placed
        if (sceneSpawned)
            HideControllerPanel(treeController.detectionText);
        else
            ShowControllerPanel(treeController.detectionText);

        switch (guideMode)
        {
            // L1 - Introduction: read-only, no operations.
            // instructionText -> step-matched hint.
            // operationInfoText -> hidden (guide card covers it).
            // statusText -> hidden (no operations yet).
            case 1:
            {
                string hint = stepIndex switch
                {
                    0 => "Point your camera at a flat surface and tap to place the tree scene.",
                    1 => "Look at the AR scene - each sphere is a node. The top sphere is the root.",
                    2 => "Trees are used in file systems, databases, compilers, and AI decision trees.",
                    _ => "Assessment starting shortly..."
                };
                SetControllerText(treeController.instructionText, hint);
                HideControllerPanel(treeController.operationInfoText);
                HideControllerPanel(treeController.statusText);
                break;
            }

            // L2 - Terminologies: place scene, add child, measure height.
            // instructionText -> step-matched hint.
            // operationInfoText -> shown for L2 (height feedback useful).
            // statusText -> shown (node count is relevant).
            case 2:
            {
                string hint = stepIndex switch
                {
                    0 => "Point your camera at a flat surface and tap to place the tree scene.",
                    1 => "Look at the root node at the top. The branches are edges connecting nodes.",
                    2 => "Add a child - it becomes a LEAF (no children) and the root becomes a PARENT.",
                    3 => "Switch to Intermediate mode and tap TREE HEIGHT to measure the tree.",
                    _ => "Assessment starting shortly..."
                };
                SetControllerText(treeController.instructionText, hint);
                ShowControllerPanel(treeController.operationInfoText);
                ShowControllerPanel(treeController.statusText);
                break;
            }

            // L3 - Types of Trees: place scene, add children, build 3-node tree.
            // instructionText -> step-matched hint.
            // operationInfoText -> shown (explains BST property per action).
            // statusText -> shown (node count important for task tracking).
            case 3:
            {
                string hint = stepIndex switch
                {
                    0 => "Point your camera at a flat surface and tap to place the tree scene.",
                    1 => "Look at the LEFT (blue) and RIGHT (orange) indicators - these are the child slots.",
                    2 => "Tap ADD CHILD and snap your node to the LEFT (blue) indicator.",
                    3 => "Add one more node to either side to build a 3-node binary tree.",
                    _ => "Assessment starting shortly..."
                };
                SetControllerText(treeController.instructionText, hint);
                ShowControllerPanel(treeController.operationInfoText);
                ShowControllerPanel(treeController.statusText);
                break;
            }

            // L4 - Traversal: run all 3 traversals.
            // instructionText -> which traversal to run next.
            // operationInfoText -> shown (controller writes traversal order info).
            // statusText -> hidden (count not relevant during traversal lesson).
            case 4:
            {
                string hint = stepIndex switch
                {
                    0 => "Point your camera at a flat surface and tap to place the tree scene.",
                    1 => "Tap  IN-ORDER  to visit nodes: Left -> Root -> Right (sorted output in BST).",
                    2 => "Tap  PRE-ORDER  to visit nodes: Root -> Left -> Right (parent before children).",
                    3 => "Tap  POST-ORDER  to visit nodes: Left -> Right -> Root (children before parent).",
                    4 => "Run all three traversals to compare their visit orders.",
                    _ => "All traversals done. Assessment starting shortly..."
                };
                SetControllerText(treeController.instructionText, hint);
                ShowControllerPanel(treeController.operationInfoText);
                HideControllerPanel(treeController.statusText);
                break;
            }

            // L5 - Implementation: search, delete, height, complexity table.
            // operationInfoText -> hidden (complexity table panel replaces it).
            // statusText -> shown.
            case 5:
            {
                string hint = stepIndex switch
                {
                    0 => "Point your camera at a flat surface and tap to place the tree scene.",
                    1 => "Tap SEARCH and enter a node name to find - watch how DFS traversal works.",
                    2 => "Tap DELETE and enter a non-root node name to remove it from the tree.",
                    3 => "Tap TREE HEIGHT to compute the longest root-to-leaf path recursively.",
                    _ => "All operations done. Assessment starting shortly..."
                };
                SetControllerText(treeController.instructionText, hint);
                HideControllerPanel(treeController.operationInfoText);  // table replaces it
                ShowControllerPanel(treeController.statusText);
                break;
            }
        }
    }

    /// <summary>
    /// Returns the root panel that should be shown/hidden for a given TMP label.
    /// Hierarchy: RootPanel > Panel > Text (two levels up from the TMP).
    /// Falls back gracefully if the hierarchy is shallower.
    /// </summary>
    GameObject GetPanelRoot(TextMeshProUGUI tmp)
    {
        if (tmp == null) return null;
        Transform t = tmp.transform.parent;   // Panel
        if (t != null && t.parent != null)
            return t.parent.gameObject;        // RootPanel
        if (t != null)
            return t.gameObject;               // Panel fallback
        return tmp.gameObject;                 // last resort
    }

    /// <summary>Shows the root panel and writes text into the TMP. No-ops if null.</summary>
    void SetControllerText(TextMeshProUGUI tmp, string text)
    {
        if (tmp == null) return;
        GetPanelRoot(tmp)?.SetActive(true);
        tmp.text  = text;
        tmp.color = Color.white;
    }

    /// <summary>Shows the root panel that wraps the given TMP label.</summary>
    void ShowControllerPanel(TextMeshProUGUI tmp)
    {
        GetPanelRoot(tmp)?.SetActive(true);
    }

    /// <summary>Hides the root panel that wraps the given TMP label.</summary>
    void HideControllerPanel(TextMeshProUGUI tmp)
    {
        var root = GetPanelRoot(tmp);
        if (root != null && root.activeSelf) root.SetActive(false);
    }

    // Keep these for non-TMP GameObjects wired directly
    void ShowGO(GameObject go) { if (go != null) go.SetActive(true); }
    void HideGO(GameObject go) { if (go != null && go.activeSelf) go.SetActive(false); }

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

        if (treeController != null)
        {
            treeController.mainButtonPanel?.SetActive(false);
            treeController.beginnerButtonPanel?.SetActive(false);
            treeController.intermediateButtonPanel?.SetActive(false);
            treeController.personInputPanel?.SetActive(false);
            treeController.traversalPanel?.SetActive(false);
            treeController.directionPanel?.SetActive(false);
            treeController.searchInputPanel?.SetActive(false);
            treeController.deleteInputPanel?.SetActive(false);

            // Hide all controller overlays — assessment UI takes over entirely
            HideControllerPanel(treeController.instructionText);
            HideControllerPanel(treeController.operationInfoText);
            HideControllerPanel(treeController.statusText);
            HideControllerPanel(treeController.detectionText);
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
                if (stepIndex == 2 && didAddChild) nextBlocked = false;
                break;
            case 4:
                if (stepIndex == 0) nextBlocked = !sceneSpawned;
                if (stepIndex == 1 && didInOrder)   nextBlocked = false;
                if (stepIndex == 2 && didPreOrder)  nextBlocked = false;
                if (stepIndex == 3 && didPostOrder) nextBlocked = false;
                break;
            case 5:
                if (stepIndex == 0) nextBlocked = !sceneSpawned;
                if (stepIndex == 1 && didSearch)  nextBlocked = false;
                if (stepIndex == 2 && didDelete)  nextBlocked = false;
                if (stepIndex == 3 && didHeight)  nextBlocked = false;
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
            nextStepButtonLabel.text = nextBlocked ? "Complete the action above" : "Next ->";
    }

    void OnMinimise() { guideCardPanel?.SetActive(false); collapsedTab?.SetActive(true); }
    void OnRestore()  { guideCardPanel?.SetActive(true);  collapsedTab?.SetActive(false); }

    void OnReturn()
    {
        // Restore all controller UI so sandbox / free-play works normally after
        if (treeController != null)
        {
            treeController.SetInstructionSilence(false);
        }

        PlayerPrefs.SetInt(ARReturnHandler.AR_JUST_RETURNED_KEY, 1);
        PlayerPrefs.SetString(ARReturnHandler.AR_TOPIC_NAME_KEY,
                              PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "trees"));
        PlayerPrefs.Save();
        SceneManager.LoadScene(PlayerPrefs.GetString("AR_MainScene", mainAppSceneName));
    }

    void UpdateComplexityTable(string activeOp)
    {
        string H(string op, string row) => activeOp == op ? $"> {row}" : $"  {row}";

        string t =
            "Operation          | Avg      | Worst\n" +
            "--------------------------------------------\n" +
            H("SEARCH",    "Search (BST)       | O(log n) | O(n)")      + "\n" +
            H("INSERT",    "Insert (BST)       | O(log n) | O(n)")      + "\n" +
            H("DELETE",    "Delete (BST)       | O(log n) | O(n)")      + "\n" +
            H("INORDER",   "In-Order Traversal | O(n)     | O(n)")      + "\n" +
            H("PREORDER",  "Pre-Order Traversal| O(n)     | O(n)")      + "\n" +
            H("POSTORDER", "Post-Order Traversal| O(n)    | O(n)")      + "\n" +
            H("HEIGHT",    "Tree Height        | O(n)     | O(n)")      + "\n" +
            "  Space Complexity     | O(n)     | O(n)";

        if (complexityTableText  != null) complexityTableText.text  = t;
        if (activeOperationLabel != null)
            activeOperationLabel.text = string.IsNullOrEmpty(activeOp) ? "" : $"Last: {activeOp}";
    }

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

    // ──────────────────────────────────────────────────────────────────────────
    // STEPS PER LESSON
    // ──────────────────────────────────────────────────────────────────────────
    Step[] BuildSteps(int mode)
    {
        switch (mode)
        {
            case 1: return new Step[]
            {
                new Step {
                    title = "What is a Tree?",
                    body  =
                        "A TREE is a non-linear data structure\n" +
                        "used to represent HIERARCHICAL data.\n\n" +
                        "Unlike arrays, stacks, and queues —\n" +
                        "trees organize data in LEVELS.\n\n" +
                        "A tree consists of:\n" +
                        "  NODES  — the data elements\n" +
                        "  EDGES  — connections between nodes\n" +
                        "  ROOT   — the single topmost node\n\n" +
                        "-> Choose your Scenario and Difficulty.\n" +
                        "-> Tap a flat surface to place the scene.",
                    waitForAction = true
                },
                new Step {
                    title = "Trees vs Linear Structures",
                    body  =
                        "LINEAR STRUCTURES:          TREES:\n" +
                        "  Array  [1,2,3,4]          Hierarchical\n" +
                        "  Stack  (LIFO)              Non-linear\n" +
                        "  Queue  (FIFO)              Multi-level\n\n" +
                        "Why use a tree?\n" +
                        "- Efficient searching — O(log n) in BST\n" +
                        "- Represents parent-child relationships\n" +
                        "- Dynamic, no fixed size\n\n" +
                        "A tree LOOKS like a real-life tree:\n" +
                        "  One root -> branches -> leaves",
                    waitForAction = false
                },
                new Step {
                    title = "Real-Life Tree Examples",
                    body  =
                        "Trees are everywhere in computing:\n\n" +
                        "FILE SYSTEM\n" +
                        "  Folders and subfolders = tree hierarchy\n\n" +
                        "DATABASE INDEXING\n" +
                        "  B-Trees used for fast data lookup\n\n" +
                        "COMPILERS\n" +
                        "  Expression trees evaluate math/code\n\n" +
                        "MACHINE LEARNING\n" +
                        "  Decision trees for classification\n\n" +
                        "NETWORK ROUTING\n" +
                        "  Routing tables use tree structures",
                    waitForAction = false
                },
                new Step {
                    title = "Lesson 1 Complete!",
                    body  =
                        "You now understand:\n" +
                        "- A tree stores hierarchical data\n" +
                        "- Made of nodes, edges, and a root\n" +
                        "- Non-linear — data organized in levels\n" +
                        "- Different from arrays, stacks, queues\n" +
                        "- Used in file systems, databases, AI\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 2: return new Step[]
            {
                new Step {
                    title = "Nodes, Roots and Edges",
                    body  =
                        "NODE — A basic unit containing data.\n" +
                        "  In the AR scene: each sphere/fruit/waypoint.\n\n" +
                        "ROOT — The topmost node.\n" +
                        "  Every tree has exactly ONE root.\n" +
                        "  In the AR scene: the node at the top.\n\n" +
                        "EDGE — A connection between two nodes.\n" +
                        "  In the AR scene: the branches/rods.\n\n" +
                        "-> Place the scene and identify\n" +
                        "   the root, nodes, and edges.",
                    waitForAction = true
                },
                new Step {
                    title = "Parent, Child and Leaf",
                    body  =
                        "PARENT — A node with one or more children.\n" +
                        "  The root is the parent of all top children.\n\n" +
                        "CHILD — A node directly below another node.\n" +
                        "  Connected via an edge from a parent.\n\n" +
                        "LEAF (External Node) — A node with NO children.\n" +
                        "  The endpoints of the tree.\n\n" +
                        "INTERNAL NODE — Has at least one child.\n" +
                        "  Every non-leaf node is internal.\n\n" +
                        "In the AR scene: root = parent,\n" +
                        "bottom nodes = leaf nodes.",
                    waitForAction = false
                },
                new Step {
                    title = "Depth, Height and Subtree",
                    body  =
                        "DEPTH of a Node:\n" +
                        "  Number of edges from the ROOT to that node.\n" +
                        "  Root has depth 0.\n" +
                        "  Root's children have depth 1.\n\n" +
                        "HEIGHT of the Tree:\n" +
                        "  Longest path from root to any LEAF.\n" +
                        "  A single-node tree has height 0.\n\n" +
                        "SUBTREE:\n" +
                        "  A node + ALL its descendants.\n" +
                        "  Every node can be seen as a\n" +
                        "  root of its own subtree.",
                    waitForAction = false
                },
                new Step {
                    title = "Lesson 2 Complete!",
                    body  =
                        "You now know all key terminologies:\n" +
                        "- Node — data unit\n" +
                        "- Root — topmost node (single)\n" +
                        "- Edge — connection between nodes\n" +
                        "- Parent / Child relationship\n" +
                        "- Leaf — node with no children\n" +
                        "- Internal node — has children\n" +
                        "- Depth — edges from root to node\n" +
                        "- Height — longest root-to-leaf path\n" +
                        "- Subtree — node + descendants\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 3: return new Step[]
            {
                new Step {
                    title = "Binary Tree",
                    body  =
                        "A BINARY TREE is a tree where each\n" +
                        "node has AT MOST 2 children:\n\n" +
                        "  LEFT child\n" +
                        "  RIGHT child\n\n" +
                        "Example:\n" +
                        "       10\n" +
                        "      /  \\\n" +
                        "     5    20\n\n" +
                        "The AR scene uses a Binary Tree!\n" +
                        "Each node has a LEFT and RIGHT slot.\n\n" +
                        "-> Place the scene and observe\n" +
                        "   the LEFT and RIGHT indicators.",
                    waitForAction = true
                },
                new Step {
                    title = "Binary Search Tree (BST)",
                    body  =
                        "A BST follows a strict ordering rule:\n\n" +
                        "  LEFT subtree  < Root\n" +
                        "  RIGHT subtree > Root\n\n" +
                        "Example:\n" +
                        "       15\n" +
                        "      /  \\\n" +
                        "    10    20\n" +
                        "   / \\\n" +
                        "  5   12\n\n" +
                        "This rule makes SEARCH efficient!\n\n" +
                        "Average case: O(log n)\n" +
                        "Worst case (skewed): O(n)",
                    waitForAction = false
                },
                new Step {
                    title = "Other Binary Tree Types",
                    body  =
                        "FULL BINARY TREE:\n" +
                        "  Every node has 0 OR 2 children.\n" +
                        "  No node has exactly 1 child.\n\n" +
                        "COMPLETE BINARY TREE:\n" +
                        "  All levels filled except the last.\n" +
                        "  Last level filled LEFT to RIGHT.\n\n" +
                        "BALANCED BINARY TREE:\n" +
                        "  Height difference between left and right\n" +
                        "  subtrees is minimal (1 or less).\n" +
                        "  Ensures O(log n) operations!\n\n" +
                        "-> Add a child node to the AR scene\n" +
                        "   and observe the binary structure.",
                    waitForAction = true
                },
                new Step {
                    title = "Lesson 3 Complete!",
                    body  =
                        "You now understand:\n" +
                        "- Binary Tree — max 2 children per node\n" +
                        "- BST — left < root < right rule\n" +
                        "- BST search: O(log n) avg, O(n) worst\n" +
                        "- Full Binary Tree — 0 or 2 children\n" +
                        "- Complete — all levels filled L->R\n" +
                        "- Balanced — minimal height difference\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 4: return new Step[]
            {
                new Step {
                    title = "Tree Traversal",
                    body  =
                        "TRAVERSAL means visiting EVERY node\n" +
                        "in a tree EXACTLY ONCE.\n\n" +
                        "Four main traversal methods:\n\n" +
                        "  Pre-Order   Root -> Left -> Right\n" +
                        "  In-Order    Left -> Root -> Right\n" +
                        "  Post-Order  Left -> Right -> Root\n" +
                        "  Level-Order Level by level (BFS)\n\n" +
                        "All traversals: Time O(n)  |  Space O(n)\n\n" +
                        "-> Place the scene, then try each traversal!",
                    waitForAction = true
                },
                new Step {
                    title = "In-Order Traversal",
                    body  =
                        "IN-ORDER:  Left -> Root -> Right\n\n" +
                        "For this tree:\n" +
                        "       A\n" +
                        "      / \\\n" +
                        "     B   C\n\n" +
                        "In-Order visits:  B  A  C\n\n" +
                        "KEY FACT: In a BST, In-Order traversal\n" +
                        "produces SORTED output!\n\n" +
                        "Use cases:\n" +
                        "- Sorting BST values\n" +
                        "- Expression evaluation\n\n" +
                        "-> Tap IN-ORDER to watch the animation!",
                    waitForAction = true
                },
                new Step {
                    title = "Pre-Order Traversal",
                    body  =
                        "PRE-ORDER:  Root -> Left -> Right\n\n" +
                        "For the same tree:\n" +
                        "       A\n" +
                        "      / \\\n" +
                        "     B   C\n\n" +
                        "Pre-Order visits:  A  B  C\n\n" +
                        "Parent is visited BEFORE its children.\n\n" +
                        "Use cases:\n" +
                        "- Copying a tree structure\n" +
                        "- Prefix expression (Polish notation)\n" +
                        "- Serializing a tree\n\n" +
                        "-> Tap PRE-ORDER to watch the animation!",
                    waitForAction = true
                },
                new Step {
                    title = "Post-Order Traversal",
                    body  =
                        "POST-ORDER:  Left -> Right -> Root\n\n" +
                        "For the same tree:\n" +
                        "       A\n" +
                        "      / \\\n" +
                        "     B   C\n\n" +
                        "Post-Order visits:  B  C  A\n\n" +
                        "Children are visited BEFORE the parent.\n\n" +
                        "Use cases:\n" +
                        "- Deleting a tree (free children first)\n" +
                        "- Postfix expression evaluation\n" +
                        "- Computing directory sizes\n\n" +
                        "-> Tap POST-ORDER to watch the animation!",
                    waitForAction = true
                },
                new Step {
                    title = "Lesson 4 Complete!",
                    body  =
                        "You now know all 4 traversal methods:\n\n" +
                        "- In-Order:   L -> Root -> R  (sorted output)\n" +
                        "- Pre-Order:  Root -> L -> R  (copy tree)\n" +
                        "- Post-Order: L -> R -> Root  (delete tree)\n" +
                        "- Level-Order: Level by level (BFS)\n\n" +
                        "All run in O(n) time — visiting every node.\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            case 5: return new Step[]
            {
                new Step {
                    title = "BST Insertion",
                    body  =
                        "INSERTING into a BST:\n\n" +
                        "  1. Start at root.\n" +
                        "  2. If new value < current -> go LEFT.\n" +
                        "  3. If new value > current -> go RIGHT.\n" +
                        "  4. Insert when null slot found.\n\n" +
                        "Python:\n" +
                        "  def insert(root, key):\n" +
                        "    if root is None: return Node(key)\n" +
                        "    if key < root.data:\n" +
                        "      root.left = insert(root.left, key)\n" +
                        "    else:\n" +
                        "      root.right = insert(root.right, key)\n\n" +
                        "Time: O(log n) avg | O(n) worst\n\n" +
                        "-> Place scene. Use SEARCH to find a node!",
                    waitForAction = true
                },
                new Step {
                    title = "Search in a BST",
                    body  =
                        "SEARCHING in a BST — O(log n) avg:\n\n" +
                        "  1. Start at root.\n" +
                        "  2. If target = current -> FOUND!\n" +
                        "  3. If target < current -> search LEFT.\n" +
                        "  4. If target > current -> search RIGHT.\n" +
                        "  5. Return null if not found.\n\n" +
                        "Each step cuts the tree in HALF.\n" +
                        "That's why it's O(log n)!\n\n" +
                        "BUT: if tree is SKEWED (like a linked list),\n" +
                        "search degrades to O(n).\n\n" +
                        "-> Tap SEARCH and find a node by name!",
                    waitForAction = true
                },
                new Step {
                    title = "Delete a Node",
                    body  =
                        "DELETION in a BST — 3 cases:\n\n" +
                        "Case 1: Leaf node — simply remove it.\n\n" +
                        "Case 2: One child — replace with child.\n\n" +
                        "Case 3: Two children — replace with\n" +
                        "  the IN-ORDER SUCCESSOR\n" +
                        "  (smallest value in right subtree).\n\n" +
                        "Time: O(log n) avg | O(n) worst\n\n" +
                        "In the AR scene, deleting removes\n" +
                        "the node AND its entire subtree.\n\n" +
                        "-> Tap DELETE and remove a non-root node!",
                    waitForAction = true
                },
                new Step {
                    title = "Advantages and Limitations",
                    body  =
                        "ADVANTAGES:\n" +
                        "- Efficient searching — O(log n) in BST\n" +
                        "- Represents hierarchical relationships\n" +
                        "- Dynamic — no fixed size like arrays\n" +
                        "- Used in many advanced algorithms\n\n" +
                        "LIMITATIONS:\n" +
                        "- More complex to implement than arrays\n" +
                        "- Can become UNBALANCED -> O(n) operations\n" +
                        "- Requires extra memory for pointers\n" +
                        "- No random access like arrays\n\n" +
                        "-> Tap TREE HEIGHT to compute the height!",
                    waitForAction = true
                },
                new Step {
                    title = "Lesson 5 Complete!",
                    body  =
                        "You have mastered Trees!\n\n" +
                        "- Introduction — hierarchical structure\n" +
                        "- Terminologies — root, leaf, depth, height\n" +
                        "- Types — Binary, BST, Full, Complete, Balanced\n" +
                        "- Traversals — In/Pre/Post/Level-Order\n" +
                        "- BST Insert, Search, Delete — O(log n) avg\n" +
                        "- Advantages and Limitations\n\n" +
                        "Full Big-O Summary:\n" +
                        "  Search/Insert/Delete: O(log n) avg, O(n) worst\n" +
                        "  All Traversals:       O(n)\n" +
                        "  Space:                O(n)\n\n" +
                        "Your assessment will begin shortly...",
                    waitForAction = false
                },
            };

            default: return new Step[0];
        }
    }
}