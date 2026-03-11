// ================================================================
//  TestsTabController.cs – QUIZ CARDS VERSION
//  Shows one card per quiz question fetched from the database.
//  Each card has: icon, "Quiz N" title, question preview, Start button.
//  Replaces the old Easy / Medium / Hard / Mixed difficulty buttons.
// ================================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class TestsTabController : MonoBehaviour
{
    [Header("API")]
    public string adminApiUrl = "https://structureality-admin.onrender.com/api";

    [Header("Quiz Card Prefab & Container")]
    [Tooltip("Prefab with: IconImage, TitleText, SubtitleText, BadgeText, StartButton")]
    public GameObject quizCardPrefab;
    public Transform  quizCardsContainer;   // Vertical Layout Group

    [Header("Lesson Gate UI")]
    public GameObject          lessonsNotCompletePanel;
    public TextMeshProUGUI     lessonsNotCompleteText;

    [Header("Status / Loading")]
    public TextMeshProUGUI statusText;
    public GameObject      loadingIndicator;

    [Header("Card Icon Colors (one per quiz slot)")]
    public Color quiz1Color = new Color(0.67f, 0.84f, 0.90f); // light blue
    public Color quiz2Color = new Color(0.67f, 0.90f, 0.75f); // light green
    public Color quiz3Color = new Color(0.98f, 0.93f, 0.67f); // light yellow
    public Color quiz4Color = new Color(0.90f, 0.75f, 0.90f); // light purple

    [Header("Start Button Color")]
    public Color startButtonColor = new Color(0.20f, 0.47f, 0.95f);

    // ── Runtime state ─────────────────────────────────────────────
    [HideInInspector] public string           topicName;
    [HideInInspector] public ChallengeManager challengeManager;
    [HideInInspector] public TopicPanelBridge bridge;

    private List<QuizData>    loadedQuizzes = new List<QuizData>();
    private List<GameObject>  spawnedCards  = new List<GameObject>();

    // ── Called by TopicPanelBridge when Tests tab opens ───────────
    public void Initialize(string topic, ChallengeManager cm, TopicPanelBridge b)
    {
        topicName        = topic;
        challengeManager = cm;
        bridge           = b;

        Debug.Log($"[TestsTab] Initialize: topic={topic}");
        StartCoroutine(LoadAndShowQuizCards());
    }

    public void RefreshAfterChallenge()
    {
        Debug.Log("[TestsTab] RefreshAfterChallenge");
        StartCoroutine(LoadAndShowQuizCards());
    }

    // ── Main coroutine: fetch progress + quizzes, build cards ─────
    IEnumerator LoadAndShowQuizCards()
    {
        ShowLoading(true);
        ClearCards();

        // 1. Check lesson gate via DB progress
        bool lessonsComplete = false;
        yield return StartCoroutine(CheckLessonsComplete(result => lessonsComplete = result));

        if (!lessonsComplete)
        {
            ShowLoading(false);
            ShowLessonGate();
            yield break;
        }

        if (lessonsNotCompletePanel != null) lessonsNotCompletePanel.SetActive(false);

        // 2. Fetch all quizzes for this topic
        string url = $"{adminApiUrl}/quizzes/{topicName}";
        Debug.Log($"[TestsTab] Fetching quizzes: {url}");

        UnityWebRequest req = UnityWebRequest.Get(url);
        req.timeout = 15;
        yield return req.SendWebRequest();

        ShowLoading(false);

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[TestsTab] Quiz fetch failed: {req.error}");
            SetStatus("Could not load quizzes. Check connection.");
            yield break;
        }

        try
        {
            QuizzesResponse resp = JsonUtility.FromJson<QuizzesResponse>(req.downloadHandler.text);

            if (resp != null && resp.success && resp.quizzes != null && resp.quizzes.Count > 0)
            {
                // Sort by order field
                resp.quizzes.Sort((a, b) => a.order.CompareTo(b.order));
                loadedQuizzes = resp.quizzes;
                BuildQuizCards();
                SetStatus("");
            }
            else
            {
                SetStatus("No quizzes available yet for this topic.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TestsTab] Parse error: {e.Message}");
            SetStatus("Error loading quizzes.");
        }
    }

    // ── 5 questions per quiz card ─────────────────────────────────
    private const int QUESTIONS_PER_QUIZ = 5;

    void BuildQuizCards()
    {
        if (quizCardPrefab == null || quizCardsContainer == null)
        {
            Debug.LogError("[TestsTab] quizCardPrefab or quizCardsContainer not assigned!");
            return;
        }

        Color[] iconColors      = { quiz1Color, quiz2Color, quiz3Color, quiz4Color };
        string  currentUser     = PlayerPrefs.GetString("CurrentUser", "");
        string  normalizedTopic = TopicNameConstants.Normalize(topicName);

        int cardCount = Mathf.CeilToInt((float)loadedQuizzes.Count / QUESTIONS_PER_QUIZ);

        for (int cardIndex = 0; cardIndex < cardCount; cardIndex++)
        {
            int startQ        = cardIndex * QUESTIONS_PER_QUIZ;
            int endQ          = Mathf.Min(startQ + QUESTIONS_PER_QUIZ, loadedQuizzes.Count);
            int questionCount = endQ - startQ;
            int capturedCard  = cardIndex;

            GameObject card = Instantiate(quizCardPrefab, quizCardsContainer);
            card.SetActive(true);
            spawnedCards.Add(card);

            string scoreKey  = $"User_{currentUser}_{normalizedTopic}_Quiz{cardIndex}_Score";
            bool   attempted = PlayerPrefs.GetFloat(scoreKey, -1f) >= 0f;

            QuizCard quizCard = card.GetComponent<QuizCard>();
            if (quizCard != null)
            {
                quizCard.Setup(
                    quizIndex:       cardIndex,
                    questionPreview: $"{questionCount} questions",
                    iconColor:       iconColors[cardIndex % iconColors.Length],
                    attempted:       attempted,
                    onStart:         () => OnQuizCardClicked(capturedCard)
                );
            }
            else
            {
                SetupCardManually(card, cardIndex, loadedQuizzes[startQ],
                                  iconColors[cardIndex % iconColors.Length],
                                  attempted, capturedCard);
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(quizCardsContainer as RectTransform);
    }

    void SetupCardManually(GameObject card, int index, QuizData quiz,
                           Color iconColor, bool attempted, int capturedIndex)
    {
        Image iconImg = FindDeep<Image>(card, "IconBox");
        if (iconImg != null) iconImg.color = iconColor;

        TextMeshProUGUI titleTMP = FindDeep<TextMeshProUGUI>(card, "TitleText");
        if (titleTMP != null) titleTMP.text = $"Quiz {index + 1}";

        TextMeshProUGUI subtitleTMP = FindDeep<TextMeshProUGUI>(card, "SubtitleText");
        if (subtitleTMP != null)
        {
            string preview = quiz.questionText;
            if (preview.Length > 60) preview = preview.Substring(0, 60) + "…";
            subtitleTMP.text = preview;
        }

        TextMeshProUGUI badgeTMP = FindDeep<TextMeshProUGUI>(card, "BadgeText");
        if (badgeTMP != null)
        {
            badgeTMP.text  = attempted ? "DONE" : "NEW";
            badgeTMP.color = attempted ? new Color(0.22f, 0.68f, 0.38f) : new Color(0.20f, 0.47f, 0.95f);
        }

        Button startBtn = FindDeep<Button>(card, "StartButton");
        if (startBtn != null)
        {
            ColorBlock cb       = startBtn.colors;
            cb.normalColor      = startButtonColor;
            cb.highlightedColor = Color.Lerp(startButtonColor, Color.white, 0.15f);
            cb.pressedColor     = Color.Lerp(startButtonColor, Color.black, 0.15f);
            startBtn.colors     = cb;

            TextMeshProUGUI btnLabel = startBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnLabel != null) btnLabel.text = attempted ? "Redo" : "Start";

            startBtn.onClick.RemoveAllListeners();
            startBtn.onClick.AddListener(() => OnQuizCardClicked(capturedIndex));
        }
    }

    // ── User taps Start on a quiz card ───────────────────────────
    void OnQuizCardClicked(int cardIndex)
    {
        if (challengeManager == null)
        {
            Debug.LogError("[TestsTab] ChallengeManager is null!");
            return;
        }

        int startQ = cardIndex * QUESTIONS_PER_QUIZ;
        int endQ   = Mathf.Min(startQ + QUESTIONS_PER_QUIZ, loadedQuizzes.Count);

        if (startQ >= loadedQuizzes.Count)
        {
            Debug.LogError($"[TestsTab] Invalid card index: {cardIndex}");
            return;
        }

        Debug.Log($"[TestsTab] Starting Quiz {cardIndex + 1} ({endQ - startQ} questions) for {topicName}");

        if (bridge != null) bridge.HidePanel();

        // Build the question list for this card (up to 5 questions)
        var questions = new List<ChallengeManager.QuizDataPublic>();
        for (int i = startQ; i < endQ; i++)
        {
            QuizData q = loadedQuizzes[i];
            questions.Add(new ChallengeManager.QuizDataPublic
            {
                _id                = q._id,
                topicName          = q.topicName,
                questionText       = q.questionText,
                answerOptions      = q.answerOptions,
                correctAnswerIndex = q.correctAnswerIndex,
                explanation        = q.explanation,
                difficulty         = q.difficulty,
                order              = q.order,
                lessonTitle        = $"Quiz {cardIndex + 1}",
                lessonIndex        = i - startQ
            });
        }

        challengeManager.StartLessonQuiz(
            topicName,
            $"Quiz {cardIndex + 1}",
            cardIndex,
            questions);
    }

    // ── Lesson gate check via DB ──────────────────────────────────
    IEnumerator CheckLessonsComplete(System.Action<bool> callback)
    {
        string currentUser = PlayerPrefs.GetString("CurrentUser", "");
        string t           = TopicNameConstants.Normalize(topicName);

        // Permanent local flag
        if (PlayerPrefs.GetInt($"TopicReadComplete_{currentUser}_{t}", 0) == 1)
        {
            callback(true);
            yield break;
        }

        // Ask DB
        if (string.IsNullOrEmpty(currentUser) || string.IsNullOrEmpty(adminApiUrl))
        {
            callback(false);
            yield break;
        }

        string url = $"{adminApiUrl}/progress/{currentUser}";
        UnityWebRequest req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 15;
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            // Fallback to local if network fails
            int dbDone = PlayerPrefs.GetInt($"DB_{currentUser}_{t}_LessonsCompleted", 0);
            callback(dbDone > 0);
            yield break;
        }

        try
        {
            DatabaseProgressResponse resp =
                JsonUtility.FromJson<DatabaseProgressResponse>(req.downloadHandler.text);

            if (resp != null && resp.success && resp.data != null)
            {
                foreach (var topic in resp.data.topics)
                {
                    if (TopicNameConstants.Normalize(topic.topicName) == t)
                    {
                        // Cache for offline use
                        PlayerPrefs.SetInt($"DB_{currentUser}_{t}_LessonsCompleted",
                                           topic.lessonsCompleted);

                        if (topic.tutorialCompleted)
                        {
                            PlayerPrefs.SetInt($"TopicReadComplete_{currentUser}_{t}", 1);
                            PlayerPrefs.Save();
                            callback(true);
                            yield break;
                        }

                        // Check lesson count
                        int total = UserProgressManager.Instance != null
                            ? UserProgressManager.Instance.GetTotalLessonsForTopic(t)
                            : 0;

                        bool passed = total > 0
                            ? topic.lessonsCompleted >= total
                            : topic.lessonsCompleted > 0;

                        if (passed)
                        {
                            PlayerPrefs.SetInt($"TopicReadComplete_{currentUser}_{t}", 1);
                            PlayerPrefs.Save();
                        }

                        callback(passed);
                        yield break;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TestsTab] Progress parse error: {e.Message}");
        }

        callback(false);
    }

    // ── Lesson gate message ───────────────────────────────────────
    void ShowLessonGate()
    {
        string currentUser = PlayerPrefs.GetString("CurrentUser", "");
        string t           = TopicNameConstants.Normalize(topicName);

        int dbDone = PlayerPrefs.GetInt($"DB_{currentUser}_{t}_LessonsCompleted", 0);
        int total  = UserProgressManager.Instance != null
                   ? UserProgressManager.Instance.GetTotalLessonsForTopic(t)
                   : 0;

        string msg = total > 0
            ? $"Complete all lessons first!\n({dbDone}/{total} done)"
            : "Complete all lessons first!";

        SetStatus(msg);

        if (lessonsNotCompletePanel != null)
        {
            lessonsNotCompletePanel.SetActive(true);
            if (lessonsNotCompleteText != null) lessonsNotCompleteText.text = msg;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────
    void ClearCards()
    {
        foreach (var c in spawnedCards)
            if (c != null) Destroy(c);
        spawnedCards.Clear();
        loadedQuizzes.Clear();

        if (quizCardsContainer != null)
            foreach (Transform child in quizCardsContainer)
                Destroy(child.gameObject);
    }

    void ShowLoading(bool show)
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(show);
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    T FindDeep<T>(GameObject root, string childName) where T : Component
    {
        Transform found = FindChildRecursive(root.transform, childName);
        return found != null ? found.GetComponent<T>() : null;
    }

    Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform r = FindChildRecursive(child, name);
            if (r != null) return r;
        }
        return null;
    }

    // ── Serialization ─────────────────────────────────────────────
    [System.Serializable]
    private class QuizData
    {
        public string   _id;
        public string   topicName;
        public string   questionText;
        public string[] answerOptions;
        public int      correctAnswerIndex;
        public string   explanation;
        public string   difficulty;
        public int      order;
    }

    [System.Serializable]
    private class QuizzesResponse
    {
        public bool           success;
        public string         topicName;
        public int            count;
        public List<QuizData> quizzes;
    }

    [System.Serializable]
    private class DatabaseProgressResponse
    {
        public bool         success;
        public ProgressData data;

        [System.Serializable]
        public class ProgressData
        {
            public string           username;
            public List<TopicEntry> topics;
        }

        [System.Serializable]
        public class TopicEntry
        {
            public string topicName;
            public int    lessonsCompleted;
            public bool   tutorialCompleted;
            public Scores difficultyScores;
        }

        [System.Serializable]
        public class Scores
        {
            public int easy, medium, hard, mixed;
        }
    }
}