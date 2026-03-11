using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ARModeSelectionManager.cs
/// =========================
/// THREE BUGS FIXED in this version:
///
/// BUG 1 — Guide panels showing in Sandbox mode:
///   Awake() hides guide canvases. But ARArrayLessonGuide.Start() was calling
///   InitGuide() which re-showed them. Fix: ARArrayLessonGuide.Start() no longer
///   calls InitGuide(). Only EnableAndStart() (Guided path) sends that message.
///   OnSandboxChosen() also explicitly re-hides all canvases after disabling.
///
/// BUG 2 — Lessons not showing immediately after "Guided" chosen in AR:
///   HandleOpenLessons() in ARReturnHandler waited on a server fetch.
///   Fix: AR_OPEN_LESSONS flow is unchanged here, but the lessons delay is
///   addressed in ARReturnHandler by skipping the fetch when cache exists.
///
/// BUG 3 — "Try it in AR" goes back to lessons instead of starting Guided AR:
///   When AR_MODE_PRESELECTED = "guided", Start() was calling OnGuidedChosen()
///   which routes back to MainMenu with AR_OPEN_LESSONS=1 — wrong!
///   Fix: Added StartGuidedARDirectly() which is called instead. It skips the
///   mode panel and starts Guided AR in the current scene immediately.
/// </summary>
public class ARModeSelectionManager : MonoBehaviour
{
    // =========================================================================
    // PLAYERPREFS KEYS
    // =========================================================================
    public const string AR_MODE_PRESELECTED_KEY = "AR_MODE_PRESELECTED";
    public const string AR_OPEN_LESSONS_KEY     = "AR_OPEN_LESSONS";

    // =========================================================================
    // DATA STRUCTURE PROFILE
    // =========================================================================
    [System.Serializable]
    public class DataStructureProfile
    {
        [Tooltip("Matched case-insensitively against AR_TOPIC_NAME_KEY.")]
        public string topicKeyword = "";

        [Tooltip("Lesson guide MonoBehaviours for this topic.")]
        public List<MonoBehaviour> lessonGuides = new List<MonoBehaviour>();

        [Tooltip("Assessment MonoBehaviours for this topic.")]
        public List<MonoBehaviour> assessments = new List<MonoBehaviour>();

        [Tooltip("Interactive controller MonoBehaviours for this topic.")]
        public List<MonoBehaviour> controllers = new List<MonoBehaviour>();

        [Tooltip("One label per lesson index.")]
        public List<string> lessonLabels = new List<string>();

        [Tooltip("Extra GameObjects that belong to the guide only. Hidden in Sandbox.")]
        public List<GameObject> guideOnlyObjects = new List<GameObject>();
    }

    // =========================================================================
    // INSPECTOR FIELDS
    // =========================================================================
    [Header("Mode Selection Canvas")]
    public Canvas     modeCanvas;
    public GameObject modeRootPanel;

    [Header("Texts")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subText;
    public TextMeshProUGUI guidedDescText;
    public TextMeshProUGUI sandboxDescText;

    [Header("Buttons")]
    public Button guidedButton;
    public Button sandboxButton;

    [Header("Data Structure Profiles")]
    public List<DataStructureProfile> profiles = new List<DataStructureProfile>();

    [Header("Scene Names")]
    public string mainAppSceneName = "MainMenu";

    // =========================================================================
    // PRIVATE STATE
    // =========================================================================
    private DataStructureProfile activeProfile = null;
    private bool   comingFromLesson = false;
    private int    lessonIndex      = -1;
    private string topicName        = "";

    // =========================================================================
    // AWAKE — disable all guides/controllers, hide all canvases
    // =========================================================================
    void Awake()
    {
        topicName   = PlayerPrefs.GetString(ARReturnHandler.AR_TOPIC_NAME_KEY, "");
        lessonIndex = PlayerPrefs.GetInt(ARReturnHandler.AR_LESSON_INDEX_KEY, -1);

        activeProfile    = FindProfile(topicName);
        comingFromLesson = activeProfile != null && lessonIndex >= 0;

        if (!comingFromLesson)
        {
            HideModePanel();
            enabled = false;
            return;
        }

        // Disable all managed components so their Start() fires but does nothing harmful
        DisableAll(activeProfile.lessonGuides);
        DisableAll(activeProfile.assessments);
        DisableAll(activeProfile.controllers);

        // Hide all guide canvases — must happen in Awake BEFORE any Start() runs
        HideAllGuideCanvases(activeProfile);

        // Hide guide-only objects
        foreach (var go in activeProfile.guideOnlyObjects)
            if (go != null) go.SetActive(false);

        // Hide all controller UI panels
        HideAllControllerPanels(activeProfile);

        Debug.Log($"[ModeSelection] Awake — topic='{topicName}' " +
                  $"lesson={lessonIndex} profile='{activeProfile.topicKeyword}'");
    }

    // =========================================================================
    // START
    // =========================================================================
    void Start()
    {
        if (!comingFromLesson) return;

        // ── Check if Guided mode was pre-selected by the lessons flow ──────
        string preSelected = PlayerPrefs.GetString(AR_MODE_PRESELECTED_KEY, "");
        if (preSelected == "guided")
        {
            PlayerPrefs.DeleteKey(AR_MODE_PRESELECTED_KEY);
            PlayerPrefs.Save();

            Debug.Log("[ModeSelection] AR_MODE_PRESELECTED=guided — auto-launching Guided AR");

            // FIX BUG 3: Call StartGuidedARDirectly(), NOT OnGuidedChosen().
            // OnGuidedChosen() routes back to MainMenu — that's for when the user
            // picks Guided from the AR panel (meaning "take me to lessons first").
            // StartGuidedARDirectly() starts the AR guide in the current scene.
            HideModePanel();
            StartGuidedARDirectly();
            return;
        }

        // Normal path: show the Guided / Sandbox selection panel
        ShowModePanel();
    }

    // =========================================================================
    // SHOW / HIDE PANEL
    // =========================================================================
    void ShowModePanel()
    {
        if (modeRootPanel != null) modeRootPanel.SetActive(true);

        string topicDisplay  = FormatTopicName(activeProfile.topicKeyword);
        string lessonDisplay = GetLessonLabel(activeProfile, lessonIndex);

        if (titleText    != null) titleText.text    = "Choose Your Mode";
        if (subText      != null) subText.text      = $"{topicDisplay}  ·  {lessonDisplay}";

        if (guidedDescText != null)
            guidedDescText.text =
                "Step-by-step guidance through the lesson\n" +
                "with an assessment at the end.";

        if (sandboxDescText != null)
            sandboxDescText.text =
                "Free exploration — use all operations\n" +
                "at your own pace, no assessment.";

        if (guidedButton != null)
        {
            guidedButton.onClick.RemoveAllListeners();
            guidedButton.onClick.AddListener(OnGuidedChosen);
        }

        if (sandboxButton != null)
        {
            sandboxButton.onClick.RemoveAllListeners();
            sandboxButton.onClick.AddListener(OnSandboxChosen);
        }

        Debug.Log($"[ModeSelection] Panel shown — {topicDisplay} / {lessonDisplay}");
    }

    void HideModePanel()
    {
        if (modeRootPanel != null) modeRootPanel.SetActive(false);
    }

    // =========================================================================
    // GUIDED CHOSEN FROM THE AR MODE PANEL
    // (User picked Guided from the AR panel → go back to lessons first)
    // =========================================================================
    void OnGuidedChosen()
    {
        Debug.Log("[ModeSelection] Guided chosen — returning to lessons panel");
        HideModePanel();

        PlayerPrefs.SetInt   (AR_OPEN_LESSONS_KEY,                    1);
        PlayerPrefs.SetFloat ("AR_OPEN_LESSONS_TIMESTAMP",            Time.realtimeSinceStartup);
        PlayerPrefs.SetString(ARReturnHandler.AR_TOPIC_NAME_KEY,      topicName);
        PlayerPrefs.SetInt   (ARReturnHandler.AR_LESSON_INDEX_KEY,    lessonIndex);
        PlayerPrefs.Save();

        Debug.Log($"[ModeSelection] Loading main scene: {mainAppSceneName}");
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainAppSceneName);
    }

    // =========================================================================
    // START GUIDED AR DIRECTLY
    // (Called when AR_MODE_PRESELECTED=guided — user already read lessons,
    //  now they clicked "Try it in AR!" and the AR scene should go straight
    //  into Guided mode without showing the mode panel.)
    // =========================================================================
   void StartGuidedARDirectly()
{
    Debug.Log($"[ModeSelection] StartGuidedARDirectly — topic={topicName} lesson={lessonIndex}");

    EnableAndStartGuide(activeProfile.lessonGuides);
    EnableAndStartAssessmentsOnly(activeProfile.assessments);

    // FIX: Do NOT disable controllers — Update() must run for plane tap detection.
    // Just hide all their UI panels so they don't show in Guided mode.
    HideAllControllerPanels(activeProfile);

    // Re-enable controllers so Update() fires (plane tapping needs it)
    foreach (var mb in activeProfile.controllers)
    {
        if (mb == null) continue;
        mb.gameObject.SetActive(true);
        mb.enabled = true;
    }

    ShowGuidedScenarioPanels(activeProfile);

    Debug.Log("[ModeSelection] Guided AR started directly (skipped mode panel)");
}
void EnableAndStartAssessmentsOnly(List<MonoBehaviour> list)
{
    if (list == null) return;
    var seen = new HashSet<int>();
    foreach (var mb in list)
    {
        if (mb == null) continue;
        if (!seen.Add(mb.GetInstanceID())) continue;

        mb.gameObject.SetActive(true);
        mb.enabled = true;
        mb.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        // NOTE: No ResetInitFlag, no InitGuide — those are guide-only messages
    }
}

    void ShowGuidedScenarioPanels(DataStructureProfile profile)
{
    foreach (var mb in profile.controllers)
    {
        if (mb == null) continue;

        var arrayCtrl = mb as InteractiveArrayCars;
        if (arrayCtrl != null)
        {
            if (arrayCtrl.scenarioPanel != null) arrayCtrl.scenarioPanel.SetActive(true);
            continue;
        }

        var stackCtrl = mb as InteractiveStackPlates;
        if (stackCtrl != null)
        {
            if (stackCtrl.scenarioPanel != null) stackCtrl.scenarioPanel.SetActive(true);
            continue;
        }

        var queueCtrl = mb as InteractiveCoffeeQueue;
        if (queueCtrl != null)
        {
            if (queueCtrl.scenarioPanel != null) queueCtrl.scenarioPanel.SetActive(true);
            continue;
        }

        var llCtrl = mb as InteractiveTrainList;
        if (llCtrl != null)
        {
            if (llCtrl.scenarioPanel != null) llCtrl.scenarioPanel.SetActive(true);
            continue;
        }

        var graphCtrl = mb as InteractiveCityGraph;
        if (graphCtrl != null)
        {
            if (graphCtrl.scenarioPanel != null) graphCtrl.scenarioPanel.SetActive(true);
            continue;
        }

        var treeCtrl = mb as InteractiveFamilyTree;
        if (treeCtrl != null)
        {
            if (treeCtrl.scenarioPanel != null) treeCtrl.scenarioPanel.SetActive(true);
        }
    }
}

    // =========================================================================
    // SANDBOX
    // =========================================================================
    void OnSandboxChosen()
    {
        Debug.Log("[ModeSelection] Sandbox chosen");
        HideModePanel();

        // Ensure guides/assessments are disabled
        DisableAll(activeProfile.lessonGuides);
        DisableAll(activeProfile.assessments);

        // FIX BUG 1: Explicitly hide all guide canvases AGAIN here.
        // Even though Awake() hid them, ARArrayLessonGuide.Start() may have
        // been called between Awake and OnSandboxChosen. With the fixed
        // ARArrayLessonGuide (Start no longer calls InitGuide), this is a
        // belt-and-suspenders safety net.
        HideAllGuideCanvases(activeProfile);

        // Hide guide-only objects
        foreach (var go in activeProfile.guideOnlyObjects)
            if (go != null) go.SetActive(false);

        // Start controller only
        EnableControllers();
    }

    // =========================================================================
    // HIDE ALL GUIDE CANVASES (extracted so both Awake and OnSandboxChosen use it)
    // =========================================================================
    void HideAllGuideCanvases(DataStructureProfile profile)
    {
        foreach (var mb in profile.lessonGuides)
        {
            if (mb == null) continue;

            var arrayGuide = mb as ARArrayLessonGuide;
            if (arrayGuide != null && arrayGuide.guideCanvas != null)
            { arrayGuide.guideCanvas.gameObject.SetActive(false); continue; }

            var stackGuide = mb as ARStackLessonGuide;
            if (stackGuide != null && stackGuide.guideCanvas != null)
            { stackGuide.guideCanvas.gameObject.SetActive(false); continue; }

            var queueGuide = mb as ARQueueLessonGuide;
            if (queueGuide != null && queueGuide.guideCanvas != null)
            { queueGuide.guideCanvas.gameObject.SetActive(false); continue; }

            var llGuide = mb as ARLinkedListLessonGuide;
            if (llGuide != null && llGuide.guideCanvas != null)
            { llGuide.guideCanvas.gameObject.SetActive(false); continue; }

            var graphGuide = mb as ARGraphLessonGuide;
            if (graphGuide != null && graphGuide.guideCanvas != null)
            { graphGuide.guideCanvas.gameObject.SetActive(false); continue; }

            var treeGuide = mb as ARTreeLessonGuide;
            if (treeGuide != null && treeGuide.guideCanvas != null)
                treeGuide.guideCanvas.gameObject.SetActive(false);
        }
    }

    // =========================================================================
    // ENABLE CONTROLLERS
    // =========================================================================
    void EnableControllers()
    {
        if (activeProfile.controllers == null) return;
        var seen = new HashSet<int>();
        foreach (var mb in activeProfile.controllers)
        {
            if (mb == null) continue;
            if (!seen.Add(mb.GetInstanceID())) continue;
            mb.gameObject.SetActive(true);
            mb.enabled = true;
            mb.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }
    }

    // =========================================================================
    // HIDE ALL CONTROLLER PANELS
    // =========================================================================
    void HideAllControllerPanels(DataStructureProfile profile)
    {
        foreach (var mb in profile.controllers)
        {
            var arrayCtrl = mb as InteractiveArrayCars;
            if (arrayCtrl != null)
            {
                if (arrayCtrl.scenarioPanel            != null) arrayCtrl.scenarioPanel.SetActive(false);
                if (arrayCtrl.difficultyPanel          != null) arrayCtrl.difficultyPanel.SetActive(false);
                if (arrayCtrl.mainButtonPanel          != null) arrayCtrl.mainButtonPanel.SetActive(false);
                if (arrayCtrl.beginnerButtonsPanel     != null) arrayCtrl.beginnerButtonsPanel.SetActive(false);
                if (arrayCtrl.intermediateButtonsPanel != null) arrayCtrl.intermediateButtonsPanel.SetActive(false);
                continue;
            }

            var stackCtrl = mb as InteractiveStackPlates;
            if (stackCtrl != null)
            {
                if (stackCtrl.scenarioPanel           != null) stackCtrl.scenarioPanel.SetActive(false);
                if (stackCtrl.difficultyPanel         != null) stackCtrl.difficultyPanel.SetActive(false);
                if (stackCtrl.beginnerButtonPanel     != null) stackCtrl.beginnerButtonPanel.SetActive(false);
                if (stackCtrl.intermediateButtonPanel != null) stackCtrl.intermediateButtonPanel.SetActive(false);
                if (stackCtrl.movementControlPanel    != null) stackCtrl.movementControlPanel.SetActive(false);
                if (stackCtrl.confirmButton           != null) stackCtrl.confirmButton.SetActive(false);
                continue;
            }

            var queueCtrl = mb as InteractiveCoffeeQueue;
            if (queueCtrl != null)
            {
                if (queueCtrl.scenarioPanel           != null) queueCtrl.scenarioPanel.SetActive(false);
                if (queueCtrl.difficultyPanel         != null) queueCtrl.difficultyPanel.SetActive(false);
                if (queueCtrl.beginnerButtonPanel     != null) queueCtrl.beginnerButtonPanel.SetActive(false);
                if (queueCtrl.intermediateButtonPanel != null) queueCtrl.intermediateButtonPanel.SetActive(false);
                if (queueCtrl.movementControlPanel    != null) queueCtrl.movementControlPanel.SetActive(false);
                if (queueCtrl.confirmButton           != null) queueCtrl.confirmButton.SetActive(false);
                continue;
            }

            var llCtrl = mb as InteractiveTrainList;
            if (llCtrl != null)
            {
                if (llCtrl.scenarioPanel           != null) llCtrl.scenarioPanel.SetActive(false);
                if (llCtrl.difficultyPanel         != null) llCtrl.difficultyPanel.SetActive(false);
                if (llCtrl.mainButtonPanel         != null) llCtrl.mainButtonPanel.SetActive(false);
                if (llCtrl.beginnerButtonPanel     != null) llCtrl.beginnerButtonPanel.SetActive(false);
                if (llCtrl.intermediateButtonPanel != null) llCtrl.intermediateButtonPanel.SetActive(false);
                if (llCtrl.carInputPanel           != null) llCtrl.carInputPanel.SetActive(false);
                if (llCtrl.insertAtInputPanel      != null) llCtrl.insertAtInputPanel.SetActive(false);
                if (llCtrl.deleteValueInputPanel   != null) llCtrl.deleteValueInputPanel.SetActive(false);
                if (llCtrl.movementControlPanel    != null) llCtrl.movementControlPanel.SetActive(false);
                if (llCtrl.confirmButton           != null) llCtrl.confirmButton.SetActive(false);
                continue;
            }

            var graphCtrl = mb as InteractiveCityGraph;
            if (graphCtrl != null)
            {
                if (graphCtrl.scenarioPanel           != null) graphCtrl.scenarioPanel.SetActive(false);
                if (graphCtrl.difficultyPanel         != null) graphCtrl.difficultyPanel.SetActive(false);
                if (graphCtrl.mainButtonPanel         != null) graphCtrl.mainButtonPanel.SetActive(false);
                if (graphCtrl.beginnerButtonPanel     != null) graphCtrl.beginnerButtonPanel.SetActive(false);
                if (graphCtrl.intermediateButtonPanel != null) graphCtrl.intermediateButtonPanel.SetActive(false);
                if (graphCtrl.movementButtonPanel     != null) graphCtrl.movementButtonPanel.SetActive(false);
                if (graphCtrl.algorithmPanel          != null) graphCtrl.algorithmPanel.SetActive(false);
                if (graphCtrl.pathCheckInputPanel     != null) graphCtrl.pathCheckInputPanel.SetActive(false);
                if (graphCtrl.degreeInputPanel        != null) graphCtrl.degreeInputPanel.SetActive(false);
                continue;
            }

            var treeCtrl = mb as InteractiveFamilyTree;
            if (treeCtrl != null)
            {
                if (treeCtrl.scenarioPanel           != null) treeCtrl.scenarioPanel.SetActive(false);
                if (treeCtrl.difficultyPanel         != null) treeCtrl.difficultyPanel.SetActive(false);
                if (treeCtrl.mainButtonPanel         != null) treeCtrl.mainButtonPanel.SetActive(false);
                if (treeCtrl.beginnerButtonPanel     != null) treeCtrl.beginnerButtonPanel.SetActive(false);
                if (treeCtrl.intermediateButtonPanel != null) treeCtrl.intermediateButtonPanel.SetActive(false);
                if (treeCtrl.traversalPanel          != null) treeCtrl.traversalPanel.SetActive(false);
                if (treeCtrl.personInputPanel        != null) treeCtrl.personInputPanel.SetActive(false);
                if (treeCtrl.directionPanel          != null) treeCtrl.directionPanel.SetActive(false);
                if (treeCtrl.confirmButton           != null) treeCtrl.confirmButton.SetActive(false);
                if (treeCtrl.searchInputPanel        != null) treeCtrl.searchInputPanel.SetActive(false);
                if (treeCtrl.deleteInputPanel        != null) treeCtrl.deleteInputPanel.SetActive(false);
            }
        }
    }

    // =========================================================================
    // HELPERS
    // =========================================================================
    DataStructureProfile FindProfile(string topic)
    {
        if (string.IsNullOrEmpty(topic)) return null;
        string t = Normalise(topic);
        foreach (var p in profiles)
        {
            if (string.IsNullOrEmpty(p.topicKeyword)) continue;
            string k = Normalise(p.topicKeyword);
            if (t.Contains(k) || k.Contains(t)) return p;
        }
        return null;
    }

    static string Normalise(string s) =>
        s.ToLower().Replace(" ", "").Replace("-", "").Replace("_", "");

    void DisableAll(List<MonoBehaviour> list)
    {
        if (list == null) return;
        foreach (var mb in list)
            if (mb != null) mb.enabled = false;
    }

   // Rename existing EnableAndStart to this — only call for lessonGuides
void EnableAndStartGuide(List<MonoBehaviour> list)
{
    if (list == null) return;
    var seen = new HashSet<int>();
    foreach (var mb in list)
    {
        if (mb == null) continue;
        if (!seen.Add(mb.GetInstanceID())) continue;

        mb.SendMessage("ResetInitFlag", SendMessageOptions.DontRequireReceiver);
        mb.gameObject.SetActive(true);
        mb.enabled = true;
        mb.SendMessage("Start",     SendMessageOptions.DontRequireReceiver);
        mb.SendMessage("InitGuide", SendMessageOptions.DontRequireReceiver);
    }
}
    string FormatTopicName(string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) return "Data Structure";
        switch (Normalise(keyword))
        {
            case "array":              return "Arrays";
            case "stack":              return "Stacks";
            case "queue":              return "Queues";
            case "linkedlist":         return "Linked Lists";
            case "tree":               return "Trees";
            case "graph":              return "Graphs";
            default:
                return char.ToUpper(keyword[0]) + keyword.Substring(1);
        }
    }

    string GetLessonLabel(DataStructureProfile profile, int index)
    {
        if (profile.lessonLabels != null &&
            index >= 0 && index < profile.lessonLabels.Count &&
            !string.IsNullOrEmpty(profile.lessonLabels[index]))
            return profile.lessonLabels[index];
        return $"Lesson {index + 1}";
    }
}