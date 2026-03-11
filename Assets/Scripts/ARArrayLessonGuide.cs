using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ARArrayLessonGuide.cs — SYLLABUS-ALIGNED VERSION (PROMINENT INSTRUCTIONS)
/// ============================================================================
/// INSTRUCTION PROMINENCE UPDATE:
///   SetActionRequired(tmp, title, body) — shows a bold, color-highlighted
///   call-to-action whenever the step is blocked waiting for user input.
///   Uses bright yellow for "do this now" prompts, cyan for placement,
///   and green for completion confirmations.
///   All notify methods update instructionText with vivid feedback.
/// </summary>
public class ARArrayLessonGuide : MonoBehaviour
{
    [Header("Your Existing AR Controller")]
    public InteractiveArrayCars arrayController;

    [Header("Existing Canvas Panels")]
    public GameObject mainButtonPanel;
    public GameObject beginnerButtonsPanel;
    public GameObject intermediateButtonsPanel;
    public GameObject indexInputPanel;
    public GameObject searchInputPanel;
    public GameObject confirmButton;

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

    [Header("Assessment (add ARArrayLessonAssessment component)")]
    public ARArrayLessonAssessment lessonAssessment;

    [Header("L1-3: Slot Inspect Card")]
    public GameObject      slotInspectCard;
    public TextMeshProUGUI inspectElementText;
    public TextMeshProUGUI inspectIndexText;
    public TextMeshProUGUI inspectMemoryText;
    public TextMeshProUGUI inspectTypeText;
    public Button          inspectCloseButton;

    [Header("L4: Traversal Controls")]
    public GameObject      traversalButtonPanel;
    public Button          playLinearButton;
    public Button          playReverseButton;
    public Button          playForLoopButton;
    public Button          playWhileLoopButton;
    public Button          playForeachButton;
    public TextMeshProUGUI traversalReadout;
    public TextMeshProUGUI codeSnippetLabel;

    [Header("L5-6: Operation Feedback")]
    public GameObject      opFeedbackPanel;
    public TextMeshProUGUI opFeedbackTitle;
    public TextMeshProUGUI opFeedbackBody;
    public TextMeshProUGUI opComplexityBadge;
    public TextMeshProUGUI operationLogText;

    [Header("L7: Toast Notifications")]
    public GameObject      toastPanel;
    public TextMeshProUGUI toastText;
    public Image           toastIcon;
    public Sprite          checkSprite;
    public Sprite          crossSprite;

    [Header("L8-9: Complexity Table")]
    public GameObject      complexityTablePanel;
    public TextMeshProUGUI complexityTableText;
    public TextMeshProUGUI activeOperationLabel;

    [Header("Settings")]
    public string mainAppSceneName = "MainMenu";

    // ── State ─────────────────────────────────────────────────────────────────
    int  lessonIndex        = -1;
    int  guideMode          = -1;
    int  currentStep        = 0;
    bool nextBlocked        = false;
    bool lotSpawned         = false;
    bool assessmentStarted  = false;

    readonly string[] lotNames = { "ParkingLot", "VendingMachine", "SupermarketShelf" };
    GameObject spawnedLot;

    bool didLinear  = false, didReverse  = false;
    bool didFor     = false, didWhile    = false, didForeach = false;
    bool traversalRunning = false;

    int  lastItemCount    = 0;
    bool didInsert        = false;
    bool didRemove        = false;
    bool didInspectSlot   = false;
    bool lastInsertWasMid = false;
    readonly List<string> opLog = new List<string>();

    readonly Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();

    struct Step { public string title, body; public bool waitForAction; }
    Step[] steps;

    // ── Guards ────────────────────────────────────────────────────────────────
    private bool _started        = false;
    private bool _guideInited    = false;
    private bool _nextOnCooldown = false;

    // ── Colour constants ──────────────────────────────────────────────────────
    static readonly Color ColAction   = new Color(1f,    0.85f, 0f);    // bright yellow — "do this"
    static readonly Color ColPlace    = new Color(0f,    0.9f,  1f);    // cyan          — "place scene"
    static readonly Color ColDone     = new Color(0.35f, 1f,    0.45f); // green         — "completed"
    static readonly Color ColInfo     = Color.white;                    // white         — neutral info

    // ─────────────────────────────────────────────────────────────────────────
    public void ResetInitFlag()
    {
        _guideInited = false;
        _started     = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (_started) return;
        _started = true;

        string topic = PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "");
        lessonIndex  = PlayerPrefs.GetInt(ARReturnHandler.AR_LESSON_INDEX_KEY, -1);

        if (!topic.ToLower().Contains("array") || lessonIndex < 0)
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
            lessonAssessment = GetComponent<ARArrayLessonAssessment>();
    }

    int LessonIndexToMode(int i)
    {
        switch (i)
        {
            case 0: return 13;
            case 1: return 4;
            case 2: return 56;
            case 3: return 7;
            case 4: return 89;
            default: return -1;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    void InitGuide()
    {
        if (guideCanvas != null) guideCanvas.gameObject.SetActive(true);
        if (_guideInited) return;
        _guideInited = true;

        slotInspectCard?.SetActive(false);
        traversalButtonPanel?.SetActive(false);
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
        inspectCloseButton?.onClick.AddListener(() => slotInspectCard?.SetActive(false));

        if (playLinearButton    != null) playLinearButton.onClick.AddListener(OnPlayLinear);
        if (playReverseButton   != null) playReverseButton.onClick.AddListener(OnPlayReverse);
        if (playForLoopButton   != null) playForLoopButton.onClick.AddListener(OnPlayFor);
        if (playWhileLoopButton != null) playWhileLoopButton.onClick.AddListener(OnPlayWhile);
        if (playForeachButton   != null) playForeachButton.onClick.AddListener(OnPlayForeach);

        steps = BuildSteps(guideMode);
        nextBlocked = true;
        ShowStep(0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!enabled) return;

        if (!lotSpawned)
        {
            foreach (var name in lotNames)
            {
                var go = GameObject.Find(name);
                if (go != null) { spawnedLot = go; lotSpawned = true; OnLotSpawned(); break; }
            }
        }

        if (lotSpawned && (guideMode == 13 || guideMode == 4))
            EnsureOperationButtonsHidden();

        if (lotSpawned && (guideMode == 56 || guideMode == 7 || guideMode == 89))
            CheckOperationChange();
    }

    // ─────────────────────────────────────────────────────────────────────────
    void OnLotSpawned()
    {
        if (guideMode == 4)  traversalButtonPanel?.SetActive(true);
        if (guideMode == 89) { complexityTablePanel?.SetActive(true); UpdateComplexityTable(""); }

        if (arrayController != null)
        {
            int cap = arrayController.arrayCapacity;
            if (guideMode == 13) arrayController.PreFillForLesson(cap);
            if (guideMode == 4)  arrayController.PreFillForLesson(cap);

            if (arrayController.ParkingLot != null)
                spawnedLot = arrayController.ParkingLot;
        }

        nextBlocked = false;
        UpdateNextButton();
        lastItemCount = CountExistingItems();
        SyncControllerUI(currentStep);
    }

    // ─────────────────────────────────────────────────────────────────────────
    void EnsureOperationButtonsHidden()
    {
        HideGO(mainButtonPanel);
        HideGO(beginnerButtonsPanel);
        HideGO(intermediateButtonsPanel);
        HideGO(indexInputPanel);
        HideGO(searchInputPanel);
        HideGO(confirmButton);
    }

    // =========================================================================
    // CONTROLLER UI SYNC  —  prominent action instructions
    // =========================================================================
    void SyncControllerUI(int stepIndex)
    {
        if (arrayController == null) return;

        if (lotSpawned)
            HideControllerPanel(arrayController.detectionText);
        else
            ShowControllerPanel(arrayController.detectionText);

        switch (guideMode)
        {
            // -----------------------------------------------------------------
            // L1  BASICS  — array pre-filled, observe only
            // -----------------------------------------------------------------
            case 13:
            {
                if (stepIndex == 0)
                {
                    SetActionRequired(arrayController.instructionText,
                        "PLACE THE SCENE",
                        "Point your camera at a flat surface\nand TAP to place the array.");
                }
                else if (stepIndex == 1)
                {
                    SetControllerText(arrayController.instructionText,
                        "OBSERVE\nEach slot is one array element.\nIndex starts at 0 on the left.",
                        ColInfo);
                }
                else if (stepIndex == 2)
                {
                    SetControllerText(arrayController.instructionText,
                        "WATCH THE DEMO\nEach element is accessed directly by index.\naddress = base + index × size  →  O(1)",
                        ColInfo);
                }
                else if (stepIndex == 3)
                {
                    SetControllerText(arrayController.instructionText,
                        "FIXED-SIZE ARRAYS\nCapacity is declared once and never changes.\nToo few slots = overflow.  Too many = wasted RAM.",
                        ColInfo);
                }
                else if (stepIndex == 4)
                {
                    SetControllerText(arrayController.instructionText,
                        "DYNAMIC ARRAYS\nPython lists grow automatically with .append().\nInternally allocates a larger block when full.",
                        ColInfo);
                }
                else
                {
                    SetControllerText(arrayController.instructionText,
                        "Tap  NEXT →  when ready.",
                        ColDone);
                }
                HideControllerPanel(arrayController.operationInfoText);
                HideControllerPanel(arrayController.statusText);
                break;
            }

            // -----------------------------------------------------------------
            // L2  TRAVERSAL  — traversal buttons active
            // -----------------------------------------------------------------
            case 4:
            {
                if (stepIndex == 0)
                {
                    SetActionRequired(arrayController.instructionText,
                        "PLACE THE SCENE",
                        "Point your camera at a flat surface\nand TAP to place the array.");
                }
                else if (stepIndex == 1)
                {
                    SetActionRequired(arrayController.instructionText,
                        "TAP  LINEAR",
                        "Watch elements visited index 0 → last.\nYou must complete this to continue.");
                }
                else if (stepIndex == 2)
                {
                    SetActionRequired(arrayController.instructionText,
                        "TAP  REVERSE",
                        "Watch elements visited last → index 0.\nYou must complete this to continue.");
                }
                else if (stepIndex == 3)
                {
                    SetActionRequired(arrayController.instructionText,
                        "TAP  FOR  /  WHILE  /  FOREACH",
                        "Compare all three loop styles.\nTap any one of them to continue.");
                }
                else
                {
                    SetControllerText(arrayController.instructionText,
                        "All traversals done!  Tap  NEXT →",
                        ColDone);
                }
                ShowControllerPanel(arrayController.operationInfoText);
                HideControllerPanel(arrayController.statusText);
                break;
            }

            // -----------------------------------------------------------------
            // L3  OPERATIONS  — insert / access / remove
            // -----------------------------------------------------------------
            case 56:
            {
                if (stepIndex == 0)
                {
                    SetActionRequired(arrayController.instructionText,
                        "PLACE THE SCENE",
                        "Point your camera at a flat surface\nand TAP to place the array.");
                }
                else if (stepIndex == 1)
                {
                    SetActionRequired(arrayController.instructionText,
                        "TAP  INSERT/STOCK  NOW",
                        "Add your first item to the array.\nWatch where it lands and the complexity shown.");
                }
                else if (stepIndex == 2)
                {
                    SetControllerText(arrayController.instructionText,
                        "TAP  ACCESS/CHECK  →  Enter any index\nThe computer jumps straight to it — O(1)!\nNo scanning needed.",
                        ColAction);
                }
                else if (stepIndex == 3)
                {
                    SetActionRequired(arrayController.instructionText,
                        "TAP  REMOVE/PULL  NOW",
                        "Delete an item — watch elements shift\nleft to fill the gap  →  O(n).");
                }
                else
                {
                    SetControllerText(arrayController.instructionText,
                        "All operations done!  Tap  NEXT →",
                        ColDone);
                }
                ShowControllerPanel(arrayController.operationInfoText);
                ShowControllerPanel(arrayController.statusText);
                break;
            }

            // -----------------------------------------------------------------
            // L4  ADVANTAGES & LIMITATIONS
            // -----------------------------------------------------------------
            case 7:
            {
                if (stepIndex == 0)
                {
                    SetActionRequired(arrayController.instructionText,
                        "TAP  ACCESS/CHECK  FIRST",
                        "Notice it is ALWAYS instant regardless\nof array size  →  O(1) advantage!");
                }
                else if (stepIndex == 1)
                {
                    SetActionRequired(arrayController.instructionText,
                        "TAP  INSERT/STOCK  AT INDEX 0 or 1",
                        "Watch ALL elements shift right to make room.\nThis is the O(n) limitation of arrays!");
                }
                else
                {
                    SetControllerText(arrayController.instructionText,
                        "Advantages & limitations shown.  Tap  NEXT →",
                        ColDone);
                }
                ShowControllerPanel(arrayController.operationInfoText);
                ShowControllerPanel(arrayController.statusText);
                break;
            }

            // -----------------------------------------------------------------
            // L5  BIG-O SUMMARY
            // -----------------------------------------------------------------
            case 89:
            {
                if (stepIndex == 0)
                {
                    SetActionRequired(arrayController.instructionText,
                        "PERFORM ANY OPERATION",
                        "Watch its row light up in the Big-O table.\nTry ACCESS, INSERT, and REMOVE.");
                }
                else if (stepIndex == 1)
                {
                    SetControllerText(arrayController.instructionText,
                        "TRY ALL THREE:\n  ACCESS  →  O(1)\n  INSERT at end  →  O(1)\n  INSERT at mid  →  O(n)\n  REMOVE  →  O(n)",
                        ColAction);
                }
                else if (stepIndex == 2)
                {
                    SetControllerText(arrayController.instructionText,
                        "Light up EVERY row of the table.\nUse Intermediate mode for Search operations.",
                        ColAction);
                }
                else
                {
                    SetControllerText(arrayController.instructionText,
                        "Big-O table complete!  Tap  NEXT →",
                        ColDone);
                }
                HideControllerPanel(arrayController.operationInfoText);
                ShowControllerPanel(arrayController.statusText);
                break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PANEL ROOT HELPERS
    // ─────────────────────────────────────────────────────────────────────────
    GameObject GetPanelRoot(TextMeshProUGUI tmp)
    {
        if (tmp == null) return null;
        Transform t = tmp.transform.parent;
        if (t != null && t.parent != null) return t.parent.gameObject;
        if (t != null) return t.gameObject;
        return tmp.gameObject;
    }

    /// <summary>
    /// Shows the panel and writes text + color. Core helper used for neutral info.
    /// </summary>
    void SetControllerText(TextMeshProUGUI tmp, string text, Color color)
    {
        if (tmp == null) return;
        GetPanelRoot(tmp)?.SetActive(true);
        tmp.text  = text;
        tmp.color = color;
    }

    // Overload kept for legacy call-sites that pass no color
    void SetControllerText(TextMeshProUGUI tmp, string text)
        => SetControllerText(tmp, text, ColInfo);

    /// <summary>
    /// Displays a high-visibility ACTION REQUIRED prompt.
    /// Title is shown in bright yellow; body explains the specific task.
    /// </summary>
    void SetActionRequired(TextMeshProUGUI tmp, string actionTitle, string actionBody)
    {
        if (tmp == null) return;
        GetPanelRoot(tmp)?.SetActive(true);
        tmp.text  = $"ACTION REQUIRED\n{actionTitle}\n\n{actionBody}";
        tmp.color = ColAction;
    }

    void ShowControllerPanel(TextMeshProUGUI tmp) { GetPanelRoot(tmp)?.SetActive(true); }

    void HideControllerPanel(TextMeshProUGUI tmp)
    {
        var root = GetPanelRoot(tmp);
        if (root != null && root.activeSelf) root.SetActive(false);
    }

    void ShowGO(GameObject go) { if (go != null) go.SetActive(true); }
    void HideGO(GameObject go) { if (go != null && go.activeSelf) go.SetActive(false); }

    // ─────────────────────────────────────────────────────────────────────────
    // L1 AUTO-ACCESS DEMO
    // ─────────────────────────────────────────────────────────────────────────
    IEnumerator AutoAccessDemo()
    {
        yield return null;
        yield return new WaitForSeconds(0.5f);

        var items = GetExistingItemsOrdered();
        if (items.Count == 0) yield break;

        int loopCount = 0;
        while (guideMode == 13 && currentStep == 2)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (guideMode != 13 || currentStep != 2) yield break;

                var item = items[i];
                if (item == null) continue;

                ClearHighlights();
                HighlightObject(item, Color.cyan);

                if (inspectElementText != null) inspectElementText.text = $"arr[{i}] = {i * 10 + 10}";
                if (inspectIndexText   != null) inspectIndexText.text   = $"Index: {i}  (0-based)";
                if (inspectMemoryText  != null) inspectMemoryText.text  = $"address = base + {i} × size";
                if (inspectTypeText    != null) inspectTypeText.text    = "Type: int  (contiguous memory)";
                slotInspectCard?.SetActive(true);

                if (arrayController?.instructionText != null)
                {
                    GetPanelRoot(arrayController.instructionText)?.SetActive(true);
                    arrayController.instructionText.text  = $"Accessing arr[{i}]  →  address = base + {i} × size  →  O(1)";
                    arrayController.instructionText.color = ColPlace;
                }

                if (lessonAssessment != null)
                    lessonAssessment.NotifyElementInspected();

                yield return new WaitForSeconds(1.4f);
            }

            slotInspectCard?.SetActive(false);
            ClearHighlights();

            if (arrayController?.instructionText != null)
            {
                arrayController.instructionText.text  = "Watch each element get highlighted — direct O(1) access, no scanning!";
                arrayController.instructionText.color = ColInfo;
            }

            yield return new WaitForSeconds(0.5f);
            loopCount++;
            if (loopCount >= 2) yield return new WaitForSeconds(1.0f);
        }

        slotInspectCard?.SetActive(false);
        ClearHighlights();
    }

    // ─────────────────────────────────────────────────────────────────────────
    bool IsTapOnButton(Vector2 screenPos)
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return false;
        var pointerData = new UnityEngine.EventSystems.PointerEventData(es) { position = screenPos };
        var results = new List<UnityEngine.EventSystems.RaycastResult>();
        es.RaycastAll(pointerData, results);
        foreach (var r in results)
        {
            if (r.gameObject.GetComponent<UnityEngine.UI.Button>() != null ||
                r.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null)
                return true;
        }
        return false;
    }

    int FindNearestSlotIndex(Vector3 worldPos)
    {
        if (spawnedLot == null) return 0;
        float best = float.MaxValue; int bestIdx = 0;
        foreach (Transform child in spawnedLot.transform)
        {
            string n = child.name; int idx = -1;
            if (n.StartsWith("ParkingSpot_") && int.TryParse(n.Replace("ParkingSpot_", ""), out int pi)) idx = pi;
            else if (n.StartsWith("GridSlot_") && int.TryParse(n.Replace("GridSlot_", ""), out int gi)) idx = gi;
            if (idx < 0) continue;
            float d = Vector3.Distance(child.position, worldPos);
            if (d < best) { best = d; bestIdx = idx; }
        }
        return bestIdx;
    }

    void OnSlotTapped(int slotIndex, GameObject obj)
    {
        if (inspectElementText != null) inspectElementText.text = $"Element: arr[{slotIndex}] = {slotIndex * 10 + 10}";
        if (inspectIndexText   != null) inspectIndexText.text   = $"Index:   [{slotIndex}]  (0-based)";
        if (inspectMemoryText  != null) inspectMemoryText.text  = $"Address = base + {slotIndex} × size";
        if (inspectTypeText    != null) inspectTypeText.text    = "Type: int  (same type, contiguous)";
        slotInspectCard?.SetActive(true);
        HighlightObject(obj, Color.cyan);

        if (lessonAssessment != null)
            lessonAssessment.NotifyElementInspected();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // OPERATION DETECTION
    // ─────────────────────────────────────────────────────────────────────────
    void CheckOperationChange()
    {
        int cur = CountExistingItems();
        if (cur == lastItemCount) return;

        bool inserted = cur > lastItemCount;

        if (inserted)
        {
            lastInsertWasMid = WasLastInsertAtMidOrBeginning();
            OnOperationDetected(lastInsertWasMid ? "INSERT_MID" : "INSERT", true);
            if (!didInsert) { didInsert = true; TryUnblock(); }
        }
        else
        {
            OnOperationDetected("REMOVE", true);
            if (!didRemove) { didRemove = true; TryUnblock(); }
        }

        lastItemCount = cur;
    }

    bool WasLastInsertAtMidOrBeginning()
    {
        if (spawnedLot == null) return false;
        var items = GetExistingItemsOrdered();
        if (items.Count <= 1) return false;
        return false;
    }

    int CountExistingItems()
    {
        if (arrayController != null)
        {
            int n = 0;
            int cap = arrayController.arrayCapacity;
            for (int i = 0; i < cap; i++)
                if (arrayController.IsSlotOccupied(i)) n++;
            return n;
        }
        if (spawnedLot == null) return 0;
        int count = 0;
        foreach (Transform c in spawnedLot.GetComponentsInChildren<Transform>())
        {
            string cn = c.name;
            if (cn.StartsWith("Car_") || cn.StartsWith("Product_") || cn.StartsWith("Grocery_")) count++;
        }
        return count;
    }

    void OnOperationDetected(string op, bool success)
    {
        string complexity = op switch
        {
            "INSERT"     => "O(1) — insert at end",
            "INSERT_MID" => "O(n) — elements shift right",
            "REMOVE"     => "O(n) — elements shift left",
            _            => "O(1) — direct address"
        };

        if (guideMode == 56)
        {
            opFeedbackPanel?.SetActive(true);
            if (opFeedbackTitle   != null) opFeedbackTitle.text   = op == "INSERT_MID" ? "INSERT (mid)" : op;
            if (opFeedbackBody    != null) opFeedbackBody.text    = OpExplanation(op);
            if (opComplexityBadge != null) opComplexityBadge.text = complexity;
            opLog.Insert(0, $"{(op == "INSERT_MID" ? "INSERT(mid)" : op)}  {complexity}");
            if (opLog.Count > 4) opLog.RemoveAt(4);
            if (operationLogText  != null) operationLogText.text  = string.Join("\n", opLog);
            StartCoroutine(HideAfter(opFeedbackPanel, 3.5f));
            UpdateInstructionAfterOp(op);
        }
        else if (guideMode == 7)
        {
            string msg = op switch
            {
                "INSERT"     => "Added at end — O(1), no shifting!",
                "INSERT_MID" => "Inserted mid/start — O(n), elements shifted!",
                "REMOVE"     => "Removed — O(n) shift cost demonstrated!",
                _            => "Operation detected"
            };
            ShowToast(success ? checkSprite : crossSprite, msg);
            UpdateInstructionAfterOp(op);
        }
        else if (guideMode == 89)
        {
            UpdateComplexityTable(op);
            if (arrayController?.instructionText != null)
            {
                GetPanelRoot(arrayController.instructionText)?.SetActive(true);
                arrayController.instructionText.text  = $"{op} logged!  Try another to light up more rows.";
                arrayController.instructionText.color = ColDone;
            }
        }
    }

    void UpdateInstructionAfterOp(string op)
    {
        if (arrayController?.instructionText == null) return;

        string next;
        Color  col;

        if (guideMode == 56)
        {
            switch (op)
            {
                case "INSERT":
                case "INSERT_MID":
                    next = didInsert && !didInspectSlot
                        ? "Item added!  Now tap  ACCESS/CHECK  →  enter any index  →  instant O(1) lookup."
                        : "Item added!  Now tap  REMOVE/PULL  to delete and watch elements shift.";
                    col  = ColDone;
                    break;
                case "REMOVE":
                    next = "All three operations complete!  Tap  NEXT →";
                    col  = ColDone;
                    break;
                default:
                    next = "Tap  ACCESS  to retrieve an element by index — O(1) instant lookup!";
                    col  = ColAction;
                    break;
            }
        }
        else // guideMode == 7
        {
            switch (op)
            {
                case "INSERT":
                case "INSERT_MID":
                    next = "INSERT/STOCK shown!  Now tap  REMOVE/PULL  to demonstrate the deletion cost.";
                    col  = ColDone;
                    break;
                case "REMOVE":
                    next = "All operations demonstrated!  Tap  NEXT →";
                    col  = ColDone;
                    break;
                default:
                    next = "Tap  INSERT/STOCK  at index 0 or 1 to show the O(n) shift cost.";
                    col  = ColAction;
                    break;
            }
        }

        GetPanelRoot(arrayController.instructionText)?.SetActive(true);
        arrayController.instructionText.text  = next;
        arrayController.instructionText.color = col;
    }

    string OpExplanation(string op)
    {
        switch (op)
        {
            case "INSERT":
                return "INSERT/STOCK at End:\nPlace element at last index.\nTime: O(1) — no shifting needed.";
            case "INSERT_MID":
                return "INSERT/STOCK at Beginning/Mid:\nAll elements from that position\nshift RIGHT to make room.\nTime: O(n)";
            case "REMOVE":
                return "DELETE/PULL from Position/Beginning:\nAll elements after the removed\nslot shift LEFT to fill the gap.\nTime: O(n)\n\nDELETE from End: O(1) only!";
            default:
                return "Reading arr[i] jumps directly to\nthe memory address — O(1).\naddress = base + index × size";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TRAVERSAL
    // ─────────────────────────────────────────────────────────────────────────
    void OnPlayLinear()  { if (!traversalRunning) StartCoroutine(RunTraversal(false, "linear")); }
    void OnPlayReverse() { if (!traversalRunning) StartCoroutine(RunTraversal(true,  "linear")); }
    void OnPlayFor()     { if (!traversalRunning) StartCoroutine(RunTraversal(false, "for")); }
    void OnPlayWhile()   { if (!traversalRunning) StartCoroutine(RunTraversal(false, "while")); }
    void OnPlayForeach() { if (!traversalRunning) StartCoroutine(RunTraversal(false, "foreach")); }

    IEnumerator RunTraversal(bool reverse, string loopType)
    {
        traversalRunning = true;
        yield return null;
        yield return new WaitForSeconds(0.2f);

        var items = GetExistingItemsOrdered();

        if (items.Count == 0)
        {
            if (traversalReadout != null)
                traversalReadout.text = "No items yet — insert at least one first!";
            traversalRunning = false;
            yield break;
        }

        string code = loopType switch
        {
            "for" =>
                "# For Loop Traversal\narr = [10, 20, 30, 40, 50]\nfor i in range(len(arr)):\n    print(arr[i])\n# Time: O(n)  |  Space: O(1)",
            "while" =>
                "# While Loop Traversal\ni = 0\nwhile i < len(arr):\n    print(arr[i])\n    i += 1\n# Time: O(n)  |  Space: O(1)",
            "foreach" =>
                "# Foreach Traversal\nfor value in arr:\n    print(value)\n# No index management needed\n# Time: O(n)  |  Space: O(1)",
            _ => reverse
                ? "# Reverse Traversal\nfor i in range(len(arr)-1, -1, -1):\n    print(arr[i])\n# Visits last -> first\n# Time: O(n)  |  Space: O(1)"
                : "# Linear Traversal\nfor i in range(len(arr)):\n    print(arr[i])\n# Visits first -> last\n# Time: O(n)  |  Space: O(1)"
        };

        if (arrayController?.operationInfoText != null)
        {
            GetPanelRoot(arrayController.operationInfoText)?.SetActive(true);
            arrayController.operationInfoText.text = code;
        }
        if (codeSnippetLabel != null) codeSnippetLabel.text = code;

        ClearHighlights();
        int count = items.Count;
        for (int n = 0; n < count; n++)
        {
            int i = reverse ? (count - 1 - n) : n;
            HighlightObject(items[i], Color.yellow);

            if (arrayController?.instructionText != null)
            {
                GetPanelRoot(arrayController.instructionText)?.SetActive(true);
                arrayController.instructionText.text  = $"Visiting arr[{i}] = {i * 10 + 10}  …  Time: O(n)";
                arrayController.instructionText.color = Color.yellow;
            }

            if (traversalReadout != null)
                traversalReadout.text = $"Visiting arr[{i}] = {i * 10 + 10}";

            yield return new WaitForSeconds(0.65f);
            HighlightObject(items[i], Color.green);
        }

        if (traversalReadout != null)
            traversalReadout.text = "Traversal complete  —  Time: O(n)";

        SyncControllerUI(currentStep);

        switch (loopType)
        {
            case "linear":
                if (!reverse && !didLinear)  { didLinear  = true; CheckTraversalUnblock(); }
                if (reverse  && !didReverse) { didReverse = true; CheckTraversalUnblock(); }
                break;
            case "for":     if (!didFor)     { didFor     = true; CheckTraversalUnblock(); } break;
            case "while":   if (!didWhile)   { didWhile   = true; CheckTraversalUnblock(); } break;
            case "foreach": if (!didForeach) { didForeach = true; CheckTraversalUnblock(); } break;
        }

        if (lessonAssessment != null)
        {
            string notifyType = (loopType == "linear" && reverse) ? "reverse" : loopType;
            lessonAssessment.NotifyTraversal(notifyType);
        }

        yield return new WaitForSeconds(0.5f);
        ClearHighlights();
        traversalRunning = false;
    }

    void CheckTraversalUnblock()
    {
        if (!nextBlocked) return;
        switch (currentStep)
        {
            case 1: if (didLinear)                        TryUnblock(); break;
            case 2: if (didReverse)                       TryUnblock(); break;
            case 3: if (didFor || didWhile || didForeach) TryUnblock(); break;
        }
    }

    List<GameObject> GetExistingItemsOrdered()
    {
        var items = new List<GameObject>();

        if (arrayController != null)
        {
            int cap = arrayController.arrayCapacity;
            for (int i = 0; i < cap; i++)
            {
                if (!arrayController.IsSlotOccupied(i)) continue;
                GameObject obj = arrayController.GetSlotItem(i);
                if (obj != null) items.Add(obj);
            }
            return items;
        }

        if (spawnedLot == null) return items;
        foreach (Transform c in spawnedLot.GetComponentsInChildren<Transform>())
        {
            string n = c.name;
            if (n.StartsWith("Car_") || n.StartsWith("Product_") || n.StartsWith("Grocery_"))
                items.Add(c.gameObject);
        }
        items.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        return items;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HIGHLIGHTS
    // ─────────────────────────────────────────────────────────────────────────
    void HighlightObject(GameObject go, Color color)
    {
        if (go == null) return;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            if (!originalColors.ContainsKey(r)) originalColors[r] = r.material.color;
            r.material.color = color;
        }
    }

    void ClearHighlights()
    {
        foreach (var kv in originalColors)
            if (kv.Key != null) kv.Key.material.color = kv.Value;
        originalColors.Clear();
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

        if (guideMode == 4 && traversalButtonPanel != null)
            traversalButtonPanel.SetActive(lotSpawned && index >= 1);

        nextBlocked = s.waitForAction;
        if (nextBlocked) CheckIfAlreadyDone(index);

        if (!nextBlocked)
            StartCoroutine(EnableNextAfterDelay(0.6f));
        else
            UpdateNextButton();

        if (guideMode == 13 && index == 2)
            StartCoroutine(AutoAccessDemo());

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
        if (lesson != 1)
            traversalButtonPanel?.SetActive(false);
        else
            traversalButtonPanel?.SetActive(true);
        opFeedbackPanel?.SetActive(false);
        complexityTablePanel?.SetActive(false);
        toastPanel?.SetActive(false);

        if (arrayController != null)
        {
            arrayController.mainButtonPanel?.SetActive(false);
            arrayController.beginnerButtonsPanel?.SetActive(false);
            arrayController.intermediateButtonsPanel?.SetActive(false);
            arrayController.indexInputPanel?.SetActive(false);
            arrayController.searchInputPanel?.SetActive(false);
            arrayController.movementControlPanel?.SetActive(false);

            HideControllerPanel(arrayController.instructionText);
            HideControllerPanel(arrayController.operationInfoText);
            HideControllerPanel(arrayController.statusText);
            HideControllerPanel(arrayController.detectionText);
        }

        if (lesson == 0 && arrayController != null)
            arrayController.PreFillForLesson(arrayController.arrayCapacity);

        if (lessonAssessment.assessmentRoot != null)
            lessonAssessment.assessmentRoot.SetActive(true);

        lessonAssessment.BeginAssessment(lesson);
    }

    void CheckIfAlreadyDone(int stepIndex)
    {
        switch (guideMode)
        {
            case 13:
                if (stepIndex == 0)                                   nextBlocked = !lotSpawned;
                if (stepIndex == 2 && didInspectSlot)                 nextBlocked = false;
                break;
            case 4:
                if (stepIndex == 0)                                           nextBlocked = !lotSpawned;
                if (stepIndex == 1 && didLinear)                              nextBlocked = false;
                if (stepIndex == 2 && didReverse)                             nextBlocked = false;
                if (stepIndex == 3 && (didFor || didWhile || didForeach))     nextBlocked = false;
                break;
            case 56:
                if (stepIndex == 0)                                   nextBlocked = !lotSpawned;
                if (stepIndex == 1 && didInsert)                      nextBlocked = false;
                if (stepIndex == 3 && didRemove)                      nextBlocked = false;
                break;
            case 7:
                if (stepIndex == 0)                                   nextBlocked = !lotSpawned;
                break;
            case 89:
                if (stepIndex == 0)                                   nextBlocked = !lotSpawned;
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
            ClearHighlights();
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
            nextStepButtonLabel.text = nextBlocked ? "Complete the action first" : "Next ->";
    }

    void OnMinimise() { guideCardPanel?.SetActive(false); collapsedTab?.SetActive(true); }
    void OnRestore()  { guideCardPanel?.SetActive(true);  collapsedTab?.SetActive(false); }

    void OnReturn()
    {
        if (arrayController != null)
        {
            ShowControllerPanel(arrayController.instructionText);
            ShowControllerPanel(arrayController.operationInfoText);
            ShowControllerPanel(arrayController.detectionText);
            ShowControllerPanel(arrayController.statusText);
        }

        ClearHighlights();
        PlayerPrefs.SetInt(ARReturnHandler.AR_JUST_RETURNED_KEY, 1);
        PlayerPrefs.SetString(ARReturnHandler.AR_TOPIC_NAME_KEY,
                              PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "arrays"));
        PlayerPrefs.Save();
        SceneManager.LoadScene(PlayerPrefs.GetString("AR_MainScene", mainAppSceneName));
    }

    public void NotifyAccessPerformed()
    {
        if (lessonAssessment != null)
            lessonAssessment.NotifyAccess();

        if (arrayController?.instructionText == null) return;
        if (guideMode == 56)
        {
            GetPanelRoot(arrayController.instructionText)?.SetActive(true);
            arrayController.instructionText.text  = "ACCESS done!  O(1) — jumped straight to the memory address.\n\nNow tap  REMOVE/PULL  to delete an item.";
            arrayController.instructionText.color = ColDone;
            didInspectSlot = true;
        }
        else if (guideMode == 7)
        {
            GetPanelRoot(arrayController.instructionText)?.SetActive(true);
            arrayController.instructionText.text  = "ACCESS done!  O(1) — instant lookup.\n\nNow tap  INSERT/STOCK  at index 0 or 1 to see the O(n) shift cost.";
            arrayController.instructionText.color = ColDone;
        }
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
            "Operation           | Time\n" +
            "─────────────────────────────\n" +
            H("ACCESS",      "Access               | O(1)")     + "\n" +
            H("TRAVERSAL",   "Traversal            | O(n)")     + "\n" +
            H("SEARCH_LIN",  "Linear Search        | O(n)")     + "\n" +
            H("SEARCH_BIN",  "Binary Search        | O(log n)") + "\n" +
            H("INSERT",      "Insert (end)         | O(1)")     + "\n" +
            H("INSERT_MID",  "Insert (beg/mid)     | O(n)")     + "\n" +
            H("REMOVE",      "Delete (beg/mid)     | O(n)")     + "\n" +
            H("REMOVE_END",  "Delete (end)         | O(1)")     + "\n" +
            "  Space                | O(n)";

        if (complexityTableText  != null) complexityTableText.text  = t;
        if (activeOperationLabel != null)
            activeOperationLabel.text = string.IsNullOrEmpty(activeOp) ? "" : $"Last: {activeOp}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // STEPS — unchanged from original
    // ─────────────────────────────────────────────────────────────────────────
    Step[] BuildSteps(int mode)
    {
        switch (mode)
        {
            case 13: return new Step[]
            {
                new Step { title = "What is an Array?", body = "An array stores items of the SAME TYPE in\nCONTIGUOUS MEMORY locations.\n\nBecause elements are stored side-by-side,\nany element's address can be calculated\ndirectly from its index — no searching!\n\n-> Choose your Scenario and Difficulty.\n-> Tap a flat surface to place the scene.", waitForAction = true },
                new Step { title = "Array Elements & Indices", body = "ARRAY ELEMENT — a single value stored in the array.\nExample:  arr = [10, 20, 30]\n10, 20, and 30 are array elements.\n\nARRAY INDEX — position of an element.\nIndexing starts at 0 in most languages.\n  arr[0] -> 10\n  arr[1] -> 20\n  arr[2] -> 30\n\nMemory formula:\n  address = base + index × size", waitForAction = false },
                new Step { title = "Accessing Elements", body = "Watch the computer ACCESS each element\ndirectly by index — no scanning needed!\n\n  address = base + index × size\n\nEach slot lights up in turn, showing its\nindex, value, memory address, and type.\n\nThis is O(1) — instant, regardless of size.", waitForAction = false },
                new Step { title = "Fixed-Size Arrays", body = "In fixed-size arrays, the size is declared\nat creation and CANNOT change later.\n\nMemory is allocated in advance.\n\nPython example:\n  arr = [0] * 5\n  # -> [0, 0, 0, 0, 0]\n\n! Too few slots -> can't store all elements.\n! Too many slots -> memory is wasted.", waitForAction = false },
                new Step { title = "Dynamic-Size Arrays", body = "Dynamic arrays grow automatically when full.\nElements can be added or removed as needed.\n\nPython example:\n  arr = []\n  arr.append(value)  ← grows as needed\n\nInternally: allocates a bigger block and\ncopies all items into it when full.\n\n- Flexible size\n- Grows/shrinks as needed\n! May require resizing internally", waitForAction = false },
                new Step { title = "1D, 2D & 3D Arrays", body = "1D ARRAY — single row of elements:\n  [10][20][30][40]\n  Access: arr[index]\n\n2D ARRAY — rows and columns (matrix):\n  [[1,2,3],[4,5,6]]\n  Access: arr[row][column]\n\n3D ARRAY — stack of 2D arrays (cube):\n  [[[1,2],[3,4]],[[5,6],[7,8]]]\n  Access: arr[layer][row][column]\n\nThe scene shows a 1D array in action!", waitForAction = false },
                new Step { title = " Lesson 1 Complete!", body = "You now understand:\n- What an array is\n- Elements and 0-based indices\n- Direct memory address formula\n- Fixed vs dynamic size\n- 1D, 2D, and 3D arrays\n\nYour assessment will begin shortly…", waitForAction = false },
            };

            case 4: return new Step[]
            {
                new Step { title = "Traversal — Walking the Array", body = "Traversal means visiting EVERY element\none by one to perform an operation\n(search, sort, modify, print, etc.).\n\nBoth main types visit each element once:\n  Time: O(n)  |  Space: O(1)\n\n-> Choose Scenario and Difficulty.\n-> Tap a flat surface to place the scene.", waitForAction = true },
                new Step { title = "Linear Traversal", body = "Visits elements from index 0 to the last.\n\narr = [1, 2, 3, 4, 5]\nfor i in range(len(arr)):\n    print(arr[i])\n# Output: 1 2 3 4 5\n\nEach element is visited exactly once.\nTime: O(n)  |  Space: O(1)\n\n-> Tap LINEAR to watch the animation.", waitForAction = true },
                new Step { title = "Reverse Traversal", body = "Visits elements from the last index to 0.\n\narr = [1, 2, 3, 4, 5]\nfor i in range(len(arr)-1, -1, -1):\n    print(arr[i])\n# Output: 5 4 3 2 1\n\nEach element is still visited once,\nbut in reverse order.\nTime: O(n)  |  Space: O(1)\n\n-> Tap REVERSE to watch.", waitForAction = true },
                new Step { title = "For / While / Foreach Loops", body = "Three loop styles — same O(n) result:\n\nFOR LOOP — controls init, condition, increment.\n  Best when index is needed.\n\nWHILE LOOP — runs while condition is true.\n  Index managed manually. More flexible.\n\nFOREACH — accesses each element directly.\n  No index management. Simple & readable.\n\n-> Tap FOR, WHILE, or FOREACH\n   to compare them side by side.", waitForAction = true },
                new Step { title = " Lesson 2 Complete!", body = "You now understand:\n- Linear traversal (first -> last)\n- Reverse traversal (last -> first)\n- For, While, and Foreach loop styles\n\nAll traversal patterns visit every element.\nTime: O(n)  |  Space: O(1)\n\nYour assessment will begin shortly…", waitForAction = false },
            };

            case 56: return new Step[]
            {
                new Step { title = "Array Operations", body = "Arrays support four core operations:\n\n  ACCESS  — retrieve element by index\n  INSERT  — add a new element\n  DELETE  — remove an element\n  SEARCH  — find an element\n\nThe scene starts empty.\n-> Place the scene, then use the buttons.\nEach operation shows its complexity as you go!", waitForAction = true },
                new Step { title = "Insert an Element", body = "Three insertion cases from the syllabus:\n\nAT THE END:\n  Place at last index.\n  Time: O(1) — no shifting needed!\n\nAT A GIVEN POSITION:\n  Shift elements right from that position.\n  Time: O(n)\n\nAT THE BEGINNING:\n  Shift ALL elements right.\n  Time: O(n)\n\n-> Tap INSERT and add your first item.", waitForAction = true },
                new Step { title = "Access an Element", body = "ACCESS means retrieving an element\nusing its index.\n\n  value = arr[3]\n\nThe computer calculates the exact memory\naddress instantly — no scanning required!\n\n  address = base + index × size\n\nTime: O(1) — direct address calculation.\nThis is the key strength of arrays!\n\n-> Tap ACCESS and enter an index.", waitForAction = false },
                new Step { title = "Delete an Element", body = "Five deletion cases from the syllabus:\n\nFROM THE END:       O(1) — no shifting!\nFROM THE BEGINNING: O(n) — shift left.\nFROM GIVEN POSITION:O(n) — shift left.\nFIRST OCCURRENCE:   O(n) — search + shift.\nALL OCCURRENCES:    O(n) — traverse + remove.\n\nMost deletions cost O(n) because elements\nafter the removed slot must shift left.\n\n-> Tap REMOVE and remove an item.", waitForAction = true },
                new Step { title = " Lesson 3 Complete!", body = "You performed Insert, Access, and Remove.\n\nKey takeaways:\n- Access:              O(1)\n- Insert at end:       O(1)\n- Insert at beg/mid:   O(n)\n- Delete from end:     O(1)\n- Delete from beg/mid: O(n)\n\nYour assessment will begin shortly…", waitForAction = false },
            };

            case 7: return new Step[]
            {
                new Step { title = "Advantages of Arrays", body = "Arrays have four key advantages:\n\n- FAST ACCESS — O(1) random access.\n   Jump to any element instantly via index.\n\n- CACHE-FRIENDLY — contiguous memory means\n   the CPU loads nearby elements together.\n\n- SIMPLE IMPLEMENTATION — easy to use\n   and understand in any language.\n\n- NO POINTER OVERHEAD — unlike linked lists,\n   arrays store only values — no extra pointers.\n\n- BASIS FOR OTHER STRUCTURES — stacks,\n   queues, and hash tables are built on arrays.", waitForAction = false },
                new Step { title = "Limitations of Arrays", body = "Arrays have four key limitations:\n\n! FIXED SIZE — static arrays cannot grow\n   beyond the declared capacity.\n\n! COSTLY MID-ARRAY CHANGES — insert or\n   delete in the middle requires O(n) shifts.\n\n! CONTIGUOUS MEMORY REQUIRED — the entire\n   array must fit in one unbroken memory block.\n\n! SAME DATA TYPE ONLY — all elements must\n   be the same type (int, float, char, etc.).\n\n-> Try inserting at a specific index to see\n   the O(n) shift cost in action.", waitForAction = false },
                new Step { title = " Lesson 4 Complete!", body = "You now understand the trade-offs:\n\n- Arrays excel at random access O(1)\n- Cache-friendly contiguous memory\n\n! Costly mid-array insert/delete O(n)\n! Fixed size — can't grow beyond capacity\n! Requires contiguous memory block\n! Same data type only\n\nYour assessment will begin shortly…", waitForAction = false },
            };

            case 89: return new Step[]
            {
                new Step { title = "Big-O Complexity Summary", body = "Perform any operation — the table on screen\nhighlights its complexity in real time.\n\nTry each operation at least once to see\nevery row of the table light up.\n\nUse the Intermediate mode for Binary Search\nto see O(log n) in action!", waitForAction = false },
                new Step { title = "Why Complexity Matters", body = "O(1) — CONSTANT TIME\n  Instant regardless of array size.\n  Example: arr[i] access\n\nO(log n) — LOGARITHMIC TIME\n  Doubles input -> only +1 step.\n  Example: binary search (sorted arrays)\n\nO(n) — LINEAR TIME\n  Doubles work when array doubles.\n  Example: linear search, insert at mid\n\nChoose the right data structure based on\nwhich operations you perform most often.", waitForAction = false },
                new Step { title = "Full Complexity Table", body = "From the syllabus — complete summary:\n\nAccess:              O(1)\nTraversal:           O(n)\nLinear Search:       O(n)\nBinary Search:       O(log n) ← sorted only\nInsert at End:       O(1)\nInsert at Beg/Mid:   O(n)\nDelete from End:     O(1)\nDelete from Beg/Mid: O(n)\n\nSpace Complexity:    O(n)", waitForAction = false },
                new Step { title = " Lesson 5 Complete!", body = "You have mastered Arrays!\n\n- Definition & contiguous memory\n- Elements, indices, address formula\n- Fixed vs dynamic size\n- 1D, 2D, and 3D arrays\n- Linear, reverse, for/while/foreach traversal\n- Insert, access, and delete operations\n- Advantages and limitations\n- Full Big-O complexity table\n\nYour assessment will begin shortly…", waitForAction = false },
            };

            default: return new Step[0];
        }
    }
}