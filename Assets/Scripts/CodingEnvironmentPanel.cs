using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/// <summary>
/// CodingEnvironmentPanel
/// ───────────────────────
/// The "Code" tab in the bottom navigation bar.
///
/// Shows per-TOPIC coding challenges with Easy / Medium / Hard / Mixed difficulties,
/// mirroring the same lock/unlock progression as the current Tests tab (quiz).
///
/// This is SEPARATE from the post-AR lesson quiz.
/// The lesson quiz auto-triggers via ARReturnHandler + ChallengeManager.
/// This panel is always accessible from the bottom nav for deliberate practice.
///
/// SETUP IN UNITY:
///  1. Create a new panel GameObject sibling to HomePanel, LearnPanel, etc.
///     Name it "CodePanel"
///  2. Attach this script to CodePanel
///  3. Build the UI hierarchy described in the header comments
///  4. Add a new NavButton to BottomNavigation (see BottomNavigation_CodeTabAdditions.cs)
///  5. Assign CodePanel to the new NavButton's panel field
///
/// UI HIERARCHY:
///  CodePanel
///  ├─ Header
///  │   ├─ BackButton
///  │   ├─ TitleText          ("Code Challenges")
///  │   └─ TopicNameText      ("Arrays")
///  ├─ TopicSelectionScroll   (shown when no topic selected)
///  │   └─ TopicButtonsContainer
///  │       └─ [topic buttons — one per topic]
///  └─ ChallengeView          (shown when a topic is selected)
///      ├─ TopicLabel         ("Arrays")
///      ├─ DescriptionText
///      ├─ DifficultyPanel
///      │   ├─ EasyButton
///      │   ├─ MediumButton
///      │   ├─ HardButton
///      │   └─ MixedButton
///      └─ BackToTopicsButton
/// </summary>
public class CodingEnvironmentPanel : MonoBehaviour
{
    // ── Topic selection ───────────────────────────────────────────────
    [Header("Topic Selection View")]
    public GameObject          topicSelectionView;
    public Transform           topicButtonsContainer;
    public GameObject          topicButtonPrefab;

    // ── Challenge view ────────────────────────────────────────────────
    [Header("Challenge View")]
    public GameObject          challengeView;
    public TextMeshProUGUI     topicLabel;
    public TextMeshProUGUI     descriptionText;
    public Button              backToTopicsButton;

    [Header("Difficulty Buttons")]
    public Button              easyButton;
    public Button              mediumButton;
    public Button              hardButton;
    public Button              mixedButton;
    public TextMeshProUGUI     easyCountText;
    public TextMeshProUGUI     mediumCountText;
    public TextMeshProUGUI     hardCountText;
    public TextMeshProUGUI     mixedCountText;

    [Header("Difficulty Colors")]
    public Color easyColor      = new Color(0.20f, 0.75f, 0.40f);
    public Color mediumColor    = new Color(0.95f, 0.65f, 0.10f);
    public Color hardColor      = new Color(0.85f, 0.25f, 0.25f);
    public Color mixedColor     = new Color(0.55f, 0.25f, 0.90f);
    public Color completedColor = new Color(0.45f, 0.45f, 0.45f);
    public Color lockedColor    = new Color(0.30f, 0.30f, 0.32f);

    [Header("Status")]
    public TextMeshProUGUI     statusText;
    public GameObject          loadingIndicator;
    public GameObject          lessonsNotCompletePanel;
    public TextMeshProUGUI     lessonsNotCompleteText;

    [Header("Bottom Nav Back Button")]
    public Button              headerBackButton;  // returns to previous panel

    [Header("References")]
    public ChallengeManager    challengeManager;
    public TopicPanelBridge    topicPanelBridge;
    public string              adminApiUrl = "https://structureality-admin.onrender.com/api";

    // ── Topic list ────────────────────────────────────────────────────
    private static readonly string[] ALL_TOPICS = new[]
    {
        TopicNameConstants.ARRAYS,
        TopicNameConstants.STACKS,
        TopicNameConstants.QUEUE,
        TopicNameConstants.LINKED_LISTS,
        TopicNameConstants.TREES,
        TopicNameConstants.GRAPHS,
        TopicNameConstants.HASHMAPS,
        TopicNameConstants.HEAPS,
        TopicNameConstants.DEQUE,
        TopicNameConstants.BINARY_HEAPS
    };

    private static readonly Dictionary<string, string> TOPIC_DISPLAY_NAMES =
        new Dictionary<string, string>
        {
            { TopicNameConstants.ARRAYS,       "Arrays"        },
            { TopicNameConstants.STACKS,       "Stacks"        },
            { TopicNameConstants.QUEUE,        "Queues"        },
            { TopicNameConstants.LINKED_LISTS, "Linked Lists"  },
            { TopicNameConstants.TREES,        "Trees"         },
            { TopicNameConstants.GRAPHS,       "Graphs"        },
            { TopicNameConstants.HASHMAPS,     "Hash Maps"     },
            { TopicNameConstants.HEAPS,        "Heaps"         },
            { TopicNameConstants.DEQUE,        "Deque"         },
            { TopicNameConstants.BINARY_HEAPS, "Binary Heaps"  }
        };

    private string _selectedTopic = "";
    private List<GameObject> _spawnedTopicButtons = new List<GameObject>();

    // ══════════════════════════════════════════════════════════════════
    void Start()
    {
        if (backToTopicsButton != null)
            backToTopicsButton.onClick.AddListener(ShowTopicSelection);

        if (headerBackButton != null)
            headerBackButton.onClick.AddListener(GoBackToLearn);

        WireDifficultyButtons();
        ShowTopicSelection();
    }

    void OnEnable()
    {
        // Refresh scores every time panel becomes visible
        if (!string.IsNullOrEmpty(_selectedTopic))
            RefreshDifficultyStates();
        else
            ShowTopicSelection();
    }

    // ══════════════════════════════════════════════════════════════════
    //  TOPIC SELECTION VIEW
    // ══════════════════════════════════════════════════════════════════
    void ShowTopicSelection()
    {
        _selectedTopic = "";

        if (topicSelectionView != null) topicSelectionView.SetActive(true);
        if (challengeView      != null) challengeView.SetActive(false);

        BuildTopicButtons();
    }

    void BuildTopicButtons()
    {
        // Clear old buttons
        foreach (var btn in _spawnedTopicButtons)
            if (btn != null) Destroy(btn);
        _spawnedTopicButtons.Clear();

        if (topicButtonsContainer == null || topicButtonPrefab == null) return;

        string currentUser = PlayerPrefs.GetString("CurrentUser", "");

        foreach (string topic in ALL_TOPICS)
        {
            GameObject obj = Instantiate(topicButtonPrefab, topicButtonsContainer);
            obj.SetActive(true);

            // Label
            TextMeshProUGUI label = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                string display = TOPIC_DISPLAY_NAMES.ContainsKey(topic)
                    ? TOPIC_DISPLAY_NAMES[topic] : topic;

                // Check if lessons are complete (required to unlock code challenges)
                bool lessonsComplete = AreLessonsComplete(currentUser, topic);
                label.text = lessonsComplete ? display : $"🔒 {display}";
            }

            // Progress indicator (score dots, etc.)
            Button btn = obj.GetComponent<Button>();
            if (btn != null)
            {
                string capturedTopic = topic;
                bool   canOpen       = AreLessonsComplete(currentUser, topic);
                btn.interactable     = canOpen;
                btn.onClick.RemoveAllListeners();
                if (canOpen)
                    btn.onClick.AddListener(() => SelectTopic(capturedTopic));
            }

            _spawnedTopicButtons.Add(obj);
        }
    }

    void SelectTopic(string topicName)
    {
        _selectedTopic = topicName;

        if (topicSelectionView != null) topicSelectionView.SetActive(false);
        if (challengeView      != null) challengeView.SetActive(true);

        string display = TOPIC_DISPLAY_NAMES.ContainsKey(topicName)
            ? TOPIC_DISPLAY_NAMES[topicName] : topicName;

        if (topicLabel      != null) topicLabel.text      = display;
        if (descriptionText != null) descriptionText.text =
            $"Code challenges for {display}.\nComplete difficulties in order to unlock harder ones.";

        StartCoroutine(LoadQuizCountsForTopic(topicName));
        RefreshDifficultyStates();
    }

    // ══════════════════════════════════════════════════════════════════
    //  DIFFICULTY BUTTONS
    // ══════════════════════════════════════════════════════════════════
    void WireDifficultyButtons()
    {
        if (easyButton   != null) easyButton.onClick.AddListener(()   => StartCodeChallenge("easy"));
        if (mediumButton != null) mediumButton.onClick.AddListener(() => StartCodeChallenge("medium"));
        if (hardButton   != null) hardButton.onClick.AddListener(()   => StartCodeChallenge("hard"));
        if (mixedButton  != null) mixedButton.onClick.AddListener(()  => StartCodeChallenge("mixed"));
    }

    void RefreshDifficultyStates()
    {
        if (string.IsNullOrEmpty(_selectedTopic)) return;

        string currentUser = PlayerPrefs.GetString("CurrentUser", "");

        if (!AreLessonsComplete(currentUser, _selectedTopic))
        {
            ShowLessonGate();
            return;
        }

        if (lessonsNotCompletePanel != null) lessonsNotCompletePanel.SetActive(false);
        if (statusText != null) statusText.text = "Choose a difficulty:";

        string t = _selectedTopic;

        bool easyClear   = GetScore(currentUser, t, "easy")   > 0;
        bool mediumClear = GetScore(currentUser, t, "medium") > 0;
        bool hardClear   = GetScore(currentUser, t, "hard")   > 0;
        bool mixedClear  = GetScore(currentUser, t, "mixed")  > 0;

        SetButtonState(easyButton,   easyColor,
            canPlay: !easyClear,           isCompleted: easyClear,   isLocked: false);

        SetButtonState(mediumButton, mediumColor,
            canPlay: easyClear && !mediumClear, isCompleted: mediumClear, isLocked: !easyClear);

        SetButtonState(hardButton,   hardColor,
            canPlay: mediumClear && !hardClear,  isCompleted: hardClear,   isLocked: !mediumClear);

        SetButtonState(mixedButton,  mixedColor,
            canPlay: easyClear && mediumClear && hardClear && !mixedClear,
            isCompleted: mixedClear,
            isLocked: !(easyClear && mediumClear && hardClear));
    }

    void SetButtonState(Button btn, Color activeCol, bool canPlay, bool isCompleted, bool isLocked)
    {
        if (btn == null) return;
        btn.interactable = canPlay;

        ColorBlock cb = btn.colors;
        if      (isCompleted) { cb.normalColor = completedColor; cb.disabledColor = completedColor; }
        else if (isLocked)    { cb.normalColor = lockedColor;    cb.disabledColor = lockedColor;    }
        else                  { cb.normalColor = activeCol; cb.highlightedColor = Color.Lerp(activeCol, Color.white, 0.15f); }
        btn.colors = cb;
    }

    void ShowLessonGate()
    {
        if (easyButton   != null) easyButton.interactable   = false;
        if (mediumButton != null) mediumButton.interactable = false;
        if (hardButton   != null) hardButton.interactable   = false;
        if (mixedButton  != null) mixedButton.interactable  = false;

        string msg = "Complete all lessons for this topic first!";
        if (statusText != null) statusText.text = msg;
        if (lessonsNotCompletePanel != null)
        {
            lessonsNotCompletePanel.SetActive(true);
            if (lessonsNotCompleteText != null) lessonsNotCompleteText.text = msg;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  START CHALLENGE — reuses ChallengeManager
    // ══════════════════════════════════════════════════════════════════
    void StartCodeChallenge(string difficulty)
    {
        if (challengeManager == null)
        {
            Debug.LogError("[CodingEnvPanel] ChallengeManager not assigned!");
            return;
        }

        Debug.Log($"[CodingEnvPanel] Starting code challenge: {_selectedTopic} {difficulty}");

        PlayerPrefs.SetString("SelectedDifficulty", difficulty);
        PlayerPrefs.Save();

        // Hide this panel — ChallengeManager renders on top
        gameObject.SetActive(false);

        // Launch challenge, passing this panel's bridge so BackToMenu comes back here
        challengeManager.StartChallengeFromCodePanel(_selectedTopic, difficulty, this);
    }

    /// <summary>Called by ChallengeManager after a code challenge completes.</summary>
    public void OnChallengeComplete()
    {
        gameObject.SetActive(true);
        RefreshDifficultyStates();
    }

    // ══════════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════════
    bool AreLessonsComplete(string user, string topic)
    {
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(topic)) return false;
        string t = TopicNameConstants.Normalize(topic);
        return PlayerPrefs.GetInt($"TopicReadComplete_{user}_{t}", 0) == 1;
    }

    float GetScore(string user, string topic, string diff)
        => PlayerPrefs.GetFloat($"User_{user}_{topic}_{diff}_Score", 0f);

    void GoBackToLearn()
    {
        BottomNavigation nav = FindObjectOfType<BottomNavigation>();
        if (nav != null)
            nav.SwitchToLearnTab();
    }

    IEnumerator LoadQuizCountsForTopic(string topicName)
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(true);

        string url = $"{adminApiUrl}/quizzes/{topicName}/summary";
        UnityWebRequest req = UnityWebRequest.Get(url);
        req.timeout = 15;
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            try
            {
                QuizSummaryResponse r =
                    JsonUtility.FromJson<QuizSummaryResponse>(req.downloadHandler.text);
                if (r?.success == true)
                {
                    SetCount(easyCountText,   r.summary.easy);
                    SetCount(mediumCountText, r.summary.medium);
                    SetCount(hardCountText,   r.summary.hard);
                    SetCount(mixedCountText,  r.summary.mixed > 0
                        ? r.summary.mixed : r.summary.total);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CodingEnvPanel] Quiz count parse: {e.Message}");
            }
        }

        if (loadingIndicator != null) loadingIndicator.SetActive(false);
    }

    void SetCount(TextMeshProUGUI lbl, int count)
    {
        if (lbl == null) return;
        lbl.text = count > 0 ? $"{count} Questions" : "Coming Soon";
    }

    [System.Serializable] class QuizSummaryResponse { public bool success; public QuizSummary summary; }
    [System.Serializable] class QuizSummary { public int easy; public int medium; public int hard; public int mixed; public int total; }
}