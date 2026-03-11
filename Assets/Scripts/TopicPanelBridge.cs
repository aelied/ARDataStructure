// ══════════════════════════════════════════════════════════════════════════════
//  CHANGES vs original TopicPanelBridge.cs  (search "// CHANGED" to find them)
//
//  1. Added ShowTopicFromARDirect(topicName, arSceneName, lessonIndex)
//       → Called by ARPanelManager when the user picks a topic from the AR panel.
//       → Skips the lessons reading flow entirely.
//       → Writes PlayerPrefs (AR_TOPIC_NAME_KEY, AR_LESSON_INDEX_KEY) and loads
//         the AR scene immediately.
//       → Does NOT set AR_MODE_PRESELECTED so the AR scene shows the normal
//         Guided / Sandbox selection panel.
//
//  2. ShowTopicFromAR() is kept unchanged — it is still used when the user
//     finishes a lesson and taps "Try it in AR!" from the learn panel.
//     TopicDetailPanel will have already set AR_MODE_PRESELECTED=guided.
//
//  Everything else is identical to the original.
// ══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TopicPanelBridge : MonoBehaviour
{
    [Header("Panel Roots")]
    public GameObject myPanelRoot;
    public GameObject topicsGridPanel;
    public GameObject oldChooseTopicPanel;
    public GameObject learnPanelRoot;
    public GameObject topicSelectionPanel;

    [Header("Existing Scripts")]
    public TopicDetailPanel topicDetailPanel;

    [Header("Header")]
    public TextMeshProUGUI headerTitleText;
    public TextMeshProUGUI headerSubtitleText;
    public Button          backButton;

    [Header("Tabs")]
    public Button          lessonsTabButton;
    public Button          testsTabButton;
    public TextMeshProUGUI lessonsTabText;
    public TextMeshProUGUI testsTabText;
    public GameObject      lessonsTabUnderline;
    public GameObject      testsTabUnderline;

    [Header("Tab Panels")]
    public GameObject lessonsPanelGO;
    public GameObject testsPanelGO;

    [Header("Legacy Difficulty Buttons (now handled by TestsTabController)")]
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;
    public Button mixedButton;

    [Header("Tests Tab Controller")]
    public TestsTabController testsTabController;

    [Header("Colors")]
    public Color activeTabColor   = new Color(0.18f, 0.35f, 0.90f);
    public Color inactiveTabColor = new Color(0.55f, 0.55f, 0.65f);

    [HideInInspector] public bool   launchedFromAR     = false;
    [HideInInspector] public string pendingARSceneName = "";

    private string currentTopicName;
    private bool   cameFromLearnPanel;
    private ChallengeManager challengeManager;

    void Awake()
    {
        if (myPanelRoot != null) myPanelRoot.SetActive(false);

        if (topicDetailPanel != null && topicDetailPanel.topicDetailPanel != null)
        {
            CanvasGroup cg = topicDetailPanel.topicDetailPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = topicDetailPanel.topicDetailPanel.AddComponent<CanvasGroup>();
            cg.alpha          = 0f;
            cg.blocksRaycasts = false;
            cg.interactable   = false;
        }

        if (lessonsTabButton != null) lessonsTabButton.onClick.AddListener(() => SwitchTab(true));
        if (testsTabButton   != null) testsTabButton.onClick.AddListener(() => SwitchTab(false));
        if (backButton       != null) backButton.onClick.AddListener(OnBackClicked);

        if (topicDetailPanel != null)
            topicDetailPanel.SetBackButtonListener(OnBackClicked);

        challengeManager = FindObjectOfType<ChallengeManager>();
    }

    // ─────────────────────────────────────────────────────────────────────
    public void ShowTopic(string topicName, bool fromLearnPanel = false)
    {
        if (topicDetailPanel != null && topicDetailPanel.lessonContentPanel != null)
            topicDetailPanel.lessonContentPanel.SetActive(false);

        currentTopicName   = topicName;
        cameFromLearnPanel = fromLearnPanel;

        if (topicsGridPanel     != null) topicsGridPanel.SetActive(false);
        if (oldChooseTopicPanel != null) oldChooseTopicPanel.SetActive(false);

        if (topicDetailPanel != null && topicDetailPanel.topicDetailPanel != null)
        {
            topicDetailPanel.topicDetailPanel.SetActive(true);
            CanvasGroup cg = topicDetailPanel.topicDetailPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = topicDetailPanel.topicDetailPanel.AddComponent<CanvasGroup>();
            cg.alpha          = 0f;
            cg.blocksRaycasts = false;
            cg.interactable   = false;
        }

        if (myPanelRoot != null) myPanelRoot.SetActive(true);

        if (headerTitleText    != null) headerTitleText.text    = topicName;
        if (headerSubtitleText != null) headerSubtitleText.text = "Master the topic";

        SwitchTab(true);

        if (topicDetailPanel != null && lessonsPanelGO != null)
        {
            Transform container = FindDeep(lessonsPanelGO.transform, "LessonsContainer");
            if (container != null)
            {
                topicDetailPanel.lessonsContainer  = container;
                topicDetailPanel.lessonsScrollView = lessonsPanelGO;
            }
            else
            {
                Debug.LogError("[Bridge] LessonsContainer not found inside lessonsPanelGO!");
            }

            topicDetailPanel.controlledByBridge  = true;
            topicDetailPanel.launchedFromARPanel  = launchedFromAR;
            topicDetailPanel.arSceneName          = pendingARSceneName;

            Debug.Log($"[Bridge] Passed AR flags: launchedFromAR={launchedFromAR} scene={pendingARSceneName}");

            topicDetailPanel.ShowTopicDetail(topicName, fromLearnPanel);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Called by ARPanelManager when the student returns from the AR scene
    /// (Guided mode) and needs to land on the lessons panel for this topic.
    /// This is the existing path — TopicDetailPanel already has AR_MODE_PRESELECTED
    /// set to "guided" by this point.
    /// </summary>
    public void ShowTopicFromAR(string topicName, string arSceneName)
    {
        launchedFromAR     = true;
        pendingARSceneName = arSceneName;

        Debug.Log($"[Bridge] ShowTopicFromAR: {topicName} → {arSceneName}");
        ShowTopic(topicName, fromLearnPanel: false);
    }

    // ─────────────────────────────────────────────────────────────────────
    // CHANGED: New method — AR Panel → AR scene directly, no lesson reading.
    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Called by ARPanelManager when the student picks a topic from the AR
    /// panel.  Skips the lessons screen entirely and loads the AR scene
    /// immediately.  The AR scene will show the Guided / Sandbox panel as
    /// normal (AR_MODE_PRESELECTED is NOT set here).
    ///
    /// <param name="topicName">Normalized topic name (e.g. "arrays")</param>
    /// <param name="arSceneName">Unity scene to load (e.g. "PhysicalArrayScene")</param>
    /// <param name="lessonIndex">Which lesson to use in AR (default 0)</param>
    /// </summary>
  public void ShowTopicFromARDirect(string topicName, string arSceneName, int lessonIndex = 0)
{
    Debug.Log($"[Bridge] ShowTopicFromARDirect: topic={topicName} scene={arSceneName} lesson={lessonIndex}");

    PlayerPrefs.SetString(ARReturnHandler.AR_TOPIC_NAME_KEY,   TopicNameConstants.Normalize(topicName));
    PlayerPrefs.SetInt   (ARReturnHandler.AR_LESSON_INDEX_KEY, lessonIndex);
    PlayerPrefs.SetString(ARReturnHandler.AR_LESSON_TITLE_KEY, "");

    // ── FIX: Explicitly clear AR_MODE_PRESELECTED so the AR scene shows
    // the Guided/Sandbox panel instead of auto-launching Guided mode.
    // A stale "guided" value from a previous session causes this bug.
    PlayerPrefs.DeleteKey(ARModeSelectionManager.AR_MODE_PRESELECTED_KEY);

    PlayerPrefs.Save();

    if (string.IsNullOrEmpty(arSceneName))
    {
        Debug.LogError("[Bridge] ShowTopicFromARDirect: arSceneName is empty!");
        return;
    }

    UnityEngine.SceneManagement.SceneManager.LoadScene(arSceneName);
}
    // ─────────────────────────────────────────────────────────────────────

    // ─────────────────────────────────────────────────────────────────────
    void SwitchTab(bool showLessons)
    {
        if (topicDetailPanel != null && topicDetailPanel.lessonContentPanel != null)
            topicDetailPanel.lessonContentPanel.SetActive(false);

        if (lessonsPanelGO != null) lessonsPanelGO.SetActive(showLessons);
        if (testsPanelGO   != null) testsPanelGO.SetActive(!showLessons);

        if (!showLessons && testsTabController != null)
        {
            if (challengeManager == null) challengeManager = FindObjectOfType<ChallengeManager>();
            testsTabController.Initialize(currentTopicName, challengeManager, this);
        }

        if (lessonsTabText != null)
        {
            lessonsTabText.color     = showLessons ? activeTabColor : inactiveTabColor;
            lessonsTabText.fontStyle = showLessons ? FontStyles.Bold : FontStyles.Normal;
        }
        if (lessonsTabUnderline != null) lessonsTabUnderline.SetActive(showLessons);

        if (testsTabText != null)
        {
            testsTabText.color     = showLessons ? inactiveTabColor : activeTabColor;
            testsTabText.fontStyle = showLessons ? FontStyles.Normal : FontStyles.Bold;
        }
        if (testsTabUnderline != null) testsTabUnderline.SetActive(!showLessons);
    }

    // ─────────────────────────────────────────────────────────────────────
    public void ReturnToLessonsList()
    {
        Debug.Log("[Bridge] ReturnToLessonsList()");

        if (topicDetailPanel != null && topicDetailPanel.lessonContentPanel != null)
            topicDetailPanel.lessonContentPanel.SetActive(false);

        if (lessonsPanelGO != null) lessonsPanelGO.SetActive(true);
        if (myPanelRoot    != null) myPanelRoot.SetActive(true);

        SwitchTab(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    public void HidePanel()
    {
        if (lessonsPanelGO != null) lessonsPanelGO.SetActive(false);
        if (testsPanelGO   != null) testsPanelGO.SetActive(false);
        if (topicDetailPanel != null && topicDetailPanel.lessonContentPanel != null)
            topicDetailPanel.lessonContentPanel.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    public void ForceClose()
    {
        if (myPanelRoot != null) myPanelRoot.SetActive(false);

        if (topicDetailPanel != null)
        {
            topicDetailPanel.controlledByBridge = false;

            if (topicDetailPanel.topicDetailPanel != null)
            {
                topicDetailPanel.topicDetailPanel.SetActive(false);

                CanvasGroup cg = topicDetailPanel.topicDetailPanel.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha          = 0f;
                    cg.blocksRaycasts = false;
                    cg.interactable   = false;
                }
            }

            if (topicDetailPanel.lessonContentPanel != null)
                topicDetailPanel.lessonContentPanel.SetActive(false);
        }

        // Restore AR panel grid BEFORE clearing the flag
        if (launchedFromAR)
        {
            ARPanelManager arManager = FindObjectOfType<ARPanelManager>(true);
            if (arManager != null)
                arManager.RestoreTopicGrid();
        }

        // Clear AR flags
        launchedFromAR     = false;
        pendingARSceneName = "";

        Debug.Log("[Bridge] ForceClose()");
    }

    // ─────────────────────────────────────────────────────────────────────
    public void HidePanelAndRestoreGrid()
    {
        if (myPanelRoot      != null) myPanelRoot.SetActive(false);
        if (topicsGridPanel  != null) topicsGridPanel.SetActive(true);
        if (topicDetailPanel != null) topicDetailPanel.controlledByBridge = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    public void ShowAfterChallenge()
    {
        if (challengeManager != null && challengeManager.challengePanel != null)
            challengeManager.challengePanel.SetActive(false);

        if (topicDetailPanel != null && topicDetailPanel.lessonContentPanel != null)
            topicDetailPanel.lessonContentPanel.SetActive(false);

        if (topicsGridPanel != null) topicsGridPanel.SetActive(false);
        if (myPanelRoot     != null) myPanelRoot.SetActive(true);

        SwitchTab(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnBackClicked()
    {
        Debug.Log($"[Bridge] OnBackClicked - lessonContent active=" +
                  $"{topicDetailPanel?.lessonContentPanel?.activeSelf}, " +
                  $"launchedFromAR={launchedFromAR}");

        // If reading a lesson, go back to lesson list
        if (topicDetailPanel != null &&
            topicDetailPanel.lessonContentPanel != null &&
            topicDetailPanel.lessonContentPanel.activeSelf)
        {
            ReturnToLessonsList();
            topicDetailPanel.LoadLessonsPublic(currentTopicName);
            return;
        }

        // Close TopicCardPanel
        if (topicDetailPanel != null && topicDetailPanel.topicDetailPanel != null)
            topicDetailPanel.topicDetailPanel.SetActive(false);

        if (myPanelRoot      != null) myPanelRoot.SetActive(false);
        if (topicDetailPanel != null) topicDetailPanel.controlledByBridge = false;

        if (launchedFromAR)
        {
            ARPanelManager arManager = FindObjectOfType<ARPanelManager>(true);
            Debug.Log($"[Bridge] AR back — arManager found: {arManager != null}");
            if (arManager != null)
            {
                if (arManager.arPanel != null) arManager.arPanel.SetActive(true);
                arManager.RestoreTopicGrid();
            }

            launchedFromAR     = false;
            pendingARSceneName = "";
            return;
        }

        // ── Came from Learn panel ─────────────────────────────────────
        if (cameFromLearnPanel)
        {
            if (learnPanelRoot != null) learnPanelRoot.SetActive(true);

            UpdatedLearnPanelController lp = FindObjectOfType<UpdatedLearnPanelController>(true);
            if (lp != null)
            {
                if (lp.topicSelectionPanel != null)
                {
                    lp.topicSelectionPanel.SetActive(false);
                    CanvasGroup cg = lp.topicSelectionPanel.GetComponent<CanvasGroup>();
                    if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
                }

                BottomNavigation bottomNav = FindObjectOfType<BottomNavigation>(true);
                if (bottomNav != null) bottomNav.SetActiveTabExternally(bottomNav.learnButton);

                lp.ShowTopicSelection();
                lp.RefreshAfterTopicView();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform r = FindDeep(child, name);
            if (r != null) return r;
        }
        return null;
    }
}