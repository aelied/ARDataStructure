using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;

public class UpdatedLearnPanelController : MonoBehaviour
{
    [Header("Header")]
    public GameObject loadingIndicator;
    public TextMeshProUGUI topicNameHeader;
    public TextMeshProUGUI lessonCounterText;
    
    [Header("Panels")]
    public GameObject topicSelectionPanel;
    public GameObject learningModesPanel;
    
    [Header("Mode Buttons")]
    public Button guidedBuildingButton;
    public Button lessonsButton;
    public Button lessonsInnerButton;
    public Button puzzleChallengeButton;
    public Button puzzleChallengeInnerButton;
    public Button backToTopicsButton;
    
    [Header("Puzzle Lock UI")]
    public GameObject puzzleLockOverlay;
    public TextMeshProUGUI puzzleLockMessage;
    
    [Header("Topic Cards with Progress - Original 6")]
    public TopicCard arraysCard;
    public TopicCard stacksCard;
    public TopicCard queuesCard;
    public TopicCard linkedListsCard;
    public TopicCard treesCard;
    public TopicCard graphsCard;
    
    [Header("Topic Cards with Progress - NEW 4")]
    public TopicCard hashmapsCard;
    public TopicCard heapsCard;
    public TopicCard dequeCard;
    public TopicCard binaryHeapsCard;
    
    [Header("TopicDetailPanel Reference")]
    public TopicDetailPanel topicDetailPanel;
    
    [Header("Challenge Manager")]
    public ChallengeManager challengeManager;
    
    [Header("API Settings")]
    public string adminApiUrl = "https://structureality-admin.onrender.com/api";

    public TopicPanelBridge topicPanelBridge;
    
    [System.Serializable]
    public class TopicCard
    {
        public Button button;
        public string topicName;
        public TextMeshProUGUI progressText;

        [Header("Progress Bar (Linear - optional)")]
        public Image progressBar;

        [Header("Circular Progress")]
        // Assign a Radial 360 filled Image here in the Inspector
        // Image Type: Filled | Fill Method: Radial 360 | Fill Origin: Top | Clockwise: true
        public Image circularProgressFill;
        // Background ring image (optional, purely visual)
        public Image circularProgressBg;

        public GameObject completedBadge;
        public Image lockIcon;
        public Image keyIcon;
        public TextMeshProUGUI lessonCountText;
    }
    
    private string currentSelectedTopic = "";
    private Dictionary<string, TopicCard> topicCards;
    private UserProgressManager progressManager;
    private Dictionary<string, int> topicLessonCounts = new Dictionary<string, int>();
    private float topicSessionStartTime;
    private DatabaseProgressData cachedProgressData;
    private bool isProgressDataLoaded = false;
    private bool isFetchingProgress = false;  
    public bool isCodeLabOpen = false;
    
    void Start()
    {
        
        Debug.Log("=== UpdatedLearnPanelController Start() ===");
        
        progressManager = UserProgressManager.Instance;
        if (progressManager == null)
            Debug.LogError("UserProgressManager not found in scene!");
        
        VerifyButtonReferences();
        InitializeTopicCards();
        SetupButtons();
        
        StartCoroutine(FetchProgressFromDatabase());
        StartCoroutine(FetchLessonCounts());
        
        Debug.Log("=== LearnPanelController initialization complete ===");
    }
    public void OpenCodeLab()
{
    isCodeLabOpen = true;
}

public void CloseCodeLab()
{
    isCodeLabOpen = false;
}

    IEnumerator FetchProgressFromDatabase()
{
    if (isFetchingProgress) yield break;
    isFetchingProgress = true;

    string username = PlayerPrefs.GetString("CurrentUser", "");

    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(adminApiUrl))
    {
        Debug.LogWarning("⚠️ Cannot fetch progress: No username or API URL");
        isProgressDataLoaded = false;
        UpdateAllTopicProgress();
        UpdateTopicLockStates();
        isFetchingProgress = false;  // ← reset before yield break
        yield break;
    }

    if (loadingIndicator != null)
        loadingIndicator.SetActive(true);

    string url = $"{adminApiUrl}/progress/{username}";
    Debug.Log($"🔄 [LearnPanel] Fetching progress from: {url}");

    UnityWebRequest request = UnityWebRequest.Get(url);
    request.SetRequestHeader("Content-Type", "application/json");
    request.timeout = 30;

    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
        Debug.Log("✅ [LearnPanel] Progress fetched successfully");

        try
        {
            DatabaseProgressResponse response = JsonUtility.FromJson<DatabaseProgressResponse>(request.downloadHandler.text);

            if (response != null && response.success && response.data != null)
            {
                cachedProgressData = response.data;
                isProgressDataLoaded = true;

                Debug.Log($"✅ [LearnPanel] Loaded progress data for: {cachedProgressData.username}");
                Debug.Log($"📊 [LearnPanel] Topics count: {cachedProgressData.topics.Count}");

                foreach (var topic in cachedProgressData.topics)
                    Debug.Log($"  📚 {topic.topicName}: {topic.progressPercentage:F1}% (Lessons: {topic.lessonsCompleted})");

                UpdateAllTopicProgress();
                UpdateTopicLockStates();
            }
            else
            {
                Debug.LogError("❌ [LearnPanel] Invalid response structure");
                isProgressDataLoaded = false;
                UpdateTopicLockStates();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ [LearnPanel] Parse error: {e.Message}");
            isProgressDataLoaded = false;
            UpdateTopicLockStates();
        }
    }
    else
    {
        Debug.LogError($"❌ [LearnPanel] Fetch failed: {request.error}");
        isProgressDataLoaded = false;
        UpdateTopicLockStates();
    }

    if (loadingIndicator != null)
        loadingIndicator.SetActive(false);

    isFetchingProgress = false;  // ← correctly INSIDE the method, OUTSIDE all blocks
}

    void VerifyButtonReferences()
    {
        Debug.Log("=== VERIFYING LEARN PANEL BUTTON REFERENCES ===");
        
        void Check(TopicCard card, string name)
        {
            if (card.button != null) Debug.Log($" {name} button: {card.button.gameObject.name}");
            else Debug.LogError($"✗ {name} button is NULL!");
        }
        
        Check(arraysCard, "Arrays");
        Check(stacksCard, "Stacks");
        Check(queuesCard, "Queues");
        Check(linkedListsCard, "LinkedLists");
        Check(treesCard, "Trees");
        Check(graphsCard, "Graphs");
        Check(hashmapsCard, "Hashmaps");
        Check(heapsCard, "Heaps");
        Check(dequeCard, "Deque");
        Check(binaryHeapsCard, "BinaryHeaps");
        
        Debug.Log("=== END BUTTON VERIFICATION ===");
    }

    void InitializeTopicCards()
    {
        Debug.Log("=== Initializing LearnPanel Topic Cards (10 Topics) ===");
        
        topicCards = new Dictionary<string, TopicCard>
        {
            { TopicNameConstants.ARRAYS,       arraysCard },
            { TopicNameConstants.STACKS,       stacksCard },
            { TopicNameConstants.QUEUE,        queuesCard },
            { TopicNameConstants.LINKED_LISTS, linkedListsCard },
            { TopicNameConstants.TREES,        treesCard },
            { TopicNameConstants.GRAPHS,       graphsCard },
            { TopicNameConstants.HASHMAPS,     hashmapsCard },
            { TopicNameConstants.HEAPS,        heapsCard },
            { TopicNameConstants.DEQUE,        dequeCard },
            { TopicNameConstants.BINARY_HEAPS, binaryHeapsCard }
        };

        foreach (var kvp in topicCards)
        {
            string topicName = kvp.Key;
            TopicCard card = kvp.Value;
            
            if (card.button != null)
            {
                card.button.onClick.RemoveAllListeners();
                card.button.onClick.AddListener(() => SelectTopic(topicName));
                Debug.Log($"[LEARN] Listener added to {card.button.gameObject.name} → {topicName}");
            }
            else
            {
                Debug.LogError($"[LEARN] ✗ Button is NULL for topic: {topicName}!");
            }
        }
        
        UpdateTopicLockStates();
        
        Debug.Log($"=== {topicCards.Count} LearnPanel topic cards initialized ===");
    }
    
    void SetupButtons()
    {
        if (guidedBuildingButton != null)
            guidedBuildingButton.onClick.AddListener(StartTutorialMode);
        
        if (lessonsButton != null)
        {
            lessonsButton.onClick.RemoveAllListeners();
            lessonsButton.onClick.AddListener(StartLessonsMode);
        }
        
        if (lessonsInnerButton != null)
        {
            lessonsInnerButton.onClick.RemoveAllListeners();
            lessonsInnerButton.onClick.AddListener(StartLessonsMode);
        }
        
        if (puzzleChallengeButton != null)
        {
            puzzleChallengeButton.onClick.RemoveAllListeners();
            puzzleChallengeButton.onClick.AddListener(StartChallengeMode);
        }
        
        if (puzzleChallengeInnerButton != null)
        {
            puzzleChallengeInnerButton.onClick.RemoveAllListeners();
            puzzleChallengeInnerButton.onClick.AddListener(StartChallengeMode);
        }
        
        if (backToTopicsButton != null)
            backToTopicsButton.onClick.AddListener(ShowTopicSelection);
    }
    
    IEnumerator FetchLessonCounts()
    {
        if (string.IsNullOrEmpty(adminApiUrl)) yield break;
        
        string url = $"{adminApiUrl}/lessons";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                LessonsResponse response = JsonUtility.FromJson<LessonsResponse>(request.downloadHandler.text);
                
                if (response != null && response.success && response.lessons != null)
                {
                    topicLessonCounts.Clear();
                    
                    foreach (var lesson in response.lessons)
                    {
                        string normalizedTopic = TopicNameConstants.Normalize(lesson.topicName);
                        if (!topicLessonCounts.ContainsKey(normalizedTopic))
                            topicLessonCounts[normalizedTopic] = 0;
                        topicLessonCounts[normalizedTopic]++;
                    }
                    
                    UpdateLessonCountsOnCards();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Parse error: {e.Message}");
            }
        }
    }
    
    void UpdateLessonCountsOnCards()
    {
        foreach (var kvp in topicCards)
        {
            string topicName = kvp.Key;
            TopicCard card = kvp.Value;
            
            if (card.lessonCountText != null)
            {
                int lessonCount = GetLessonCount(topicName);
                card.lessonCountText.text = lessonCount > 0 
                    ? $"📚 {lessonCount} Lesson{(lessonCount != 1 ? "s" : "")}"
                    : "Coming Soon";
            }
        }
    }
    
    int GetLessonCount(string topicName)
    {
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        return topicLessonCounts.ContainsKey(normalizedTopic) ? topicLessonCounts[normalizedTopic] : 0;
    }
    
    void OnEnable()
    {
        Debug.Log("📋 LearnPanel OnEnable called");
        
        if (challengeManager != null)
        {
            challengeManager.ForceHideAllPanels();
            Debug.Log("✅ Forced challenge panels to hide");
        }
        
        if (topicCards == null || topicCards.Count == 0)
            InitializeTopicCards();

    }

    IEnumerator RefreshProgressData()
    {
        Debug.Log("🔄 [LearnPanel] Refreshing progress data...");
        yield return StartCoroutine(FetchProgressFromDatabase());
        UpdateLessonCountsOnCards();
        UpdateTopicLockStates();
        Debug.Log("✅ [LearnPanel] Progress refresh complete");
    }

    IEnumerator RefreshProgressBarsOnly()
    {
        yield return StartCoroutine(FetchProgressFromDatabase());
        UpdateAllTopicProgress();
        UpdateLessonCountsOnCards();
        Debug.Log("✅ [LearnPanel] Progress bars refreshed (lock states preserved)");
    }

    void UpdateAllTopicProgress()
    {
        Debug.Log("=== [LearnPanel] UpdateAllTopicProgress() ===");
        
        if (topicCards == null) return;
        
        if (isProgressDataLoaded && cachedProgressData != null)
        {
            if (cachedProgressData.topics == null || cachedProgressData.topics.Count == 0)
            {
                foreach (var kvp in topicCards)
                {
                    SetCardProgress(kvp.Value, 0f);
                    Debug.Log($"  📊 {kvp.Key}: 0% (new user)");
                }
            }
            else
            {
                Debug.Log("✅ Using progress from DATABASE");
                
                foreach (var kvp in topicCards)
                {
                    string topicName = kvp.Key;
                    var topicData = cachedProgressData.topics.Find(t =>
                        TopicNameConstants.Normalize(t.topicName) == topicName);
                    
                    if (topicData != null)
                    {
                        Debug.Log($"  📊 {topicName}: {topicData.progressPercentage:F1}% (from DB)");
                        SetCardProgress(kvp.Value, topicData.progressPercentage);
                    }
                    else
                    {
                        Debug.LogWarning($"  ⚠️ No database data for {topicName}");
                        SetCardProgress(kvp.Value, 0f);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Using FALLBACK progress (database not loaded yet)");
            
            if (progressManager == null)
                progressManager = UserProgressManager.Instance;
            
            if (progressManager == null) return;
            
            foreach (var kvp in topicCards)
            {
                float progress = progressManager.GetTopicProgress(kvp.Key);
                SetCardProgress(kvp.Value, progress);
                Debug.Log($"  📊 {kvp.Key}: {progress:F1}% (from local)");
            }
        }
        
        Debug.Log("=== Progress Update Complete ===");
    }

    // ─────────────────────────────────────────────────────────────
    //  UPDATED: SetCardProgress now drives both linear AND circular
    // ─────────────────────────────────────────────────────────────
    void SetCardProgress(TopicCard card, float progress)
    {
        bool isCompleted = progress >= 100f;
        float fill = Mathf.Clamp01(progress / 100f);

        // ── Linear progress bar (optional, keep if you still use it) ──
        if (card.progressBar != null)
            card.progressBar.fillAmount = fill;

        // ── Circular progress ring ──
        if (card.circularProgressFill != null)
        {
            card.circularProgressFill.fillAmount = fill;

            // Colour the ring: grey → blue → green
            if (progress <= 0f)
                card.circularProgressFill.color = new Color(0.78f, 0.78f, 0.78f); // grey
            else if (progress < 100f)
                card.circularProgressFill.color = new Color(0.20f, 0.60f, 1.00f); // blue
            else
                card.circularProgressFill.color = new Color(0.18f, 0.80f, 0.44f); // green
        }

        // ── Percentage text ──
        if (card.progressText != null)
            card.progressText.text = $"{progress:F0}%";

        // ── Completed badge ──
        if (card.completedBadge != null)
            card.completedBadge.SetActive(isCompleted);
    }
    
   bool IsTopicUnlocked(string topicName)
{
    return true;
}
    
    void UpdateTopicLockStates()
    {
        if (topicCards == null) return;
        
        Debug.Log("=== Updating Topic Lock States (10 Topics) ===");
        
        foreach (var kvp in topicCards)
        {
            string topicName = kvp.Key;
            TopicCard card = kvp.Value;
            bool isUnlocked = IsTopicUnlocked(topicName);
            
            if (card.button != null)
                card.button.interactable = isUnlocked;
            
            if (card.lockIcon != null)
            {
                card.lockIcon.gameObject.SetActive(!isUnlocked);
                Debug.Log($"  {topicName} Lock Icon: {(!isUnlocked ? "VISIBLE" : "HIDDEN")}");
            }
            
            if (card.keyIcon != null)
            {
                card.keyIcon.gameObject.SetActive(isUnlocked);
                Debug.Log($"  {topicName} Key Icon: {(isUnlocked ? "VISIBLE" : "HIDDEN")}");
            }
            
            CanvasGroup canvasGroup = card.button?.GetComponent<CanvasGroup>();
            if (canvasGroup == null && card.button != null)
                canvasGroup = card.button.gameObject.AddComponent<CanvasGroup>();
            
            if (canvasGroup != null)
                canvasGroup.alpha = isUnlocked ? 1.0f : 0.5f;
            
            Debug.Log($"  {topicName}: {(isUnlocked ? "🔓 UNLOCKED" : "🔒 LOCKED")}");
        }
        
        Debug.Log("=== Lock States Updated ===");
    }
    
    IEnumerator ResetLearnPanelNextFrame(GameObject learnPanelRoot)
{
    yield return null;
    yield return null;

    TopicPanelBridge bridge = FindObjectOfType<TopicPanelBridge>();
    if (bridge != null) bridge.ForceClose();

    // Search the whole scene since they're siblings, not parent/child
    CodeLabPanel codeLab = FindObjectOfType<CodeLabPanel>(true);
    if (codeLab != null) codeLab.HideResultPanel();

    UpdatedLearnPanelController lp = FindObjectOfType<UpdatedLearnPanelController>(true);

    if (lp != null)
    {
        if (!lp.isCodeLabOpen)
        {
            if (lp.topicSelectionPanel != null)
            {
                CanvasGroup cg = lp.topicSelectionPanel.GetComponent<CanvasGroup>();
                if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
            }
            lp.ResetTopicCardVisibility();
            lp.ShowTopicSelection();
        }
        else
        {
            Debug.Log("[BottomNav] CodeLab is open — skipping ShowTopicSelection");
        }
    }
}
    public void ShowTopicSelection()
    {
        isCodeLabOpen = false;
         if (isCodeLabOpen)
    {
        Debug.Log("⛔ ShowTopicSelection suppressed — CodeLab is open");
        return;
    }
        Debug.Log("ShowTopicSelection called");

        if (topicSelectionPanel != null)
        {
            foreach (Transform child in topicSelectionPanel.GetComponentsInChildren<Transform>(true))
                child.gameObject.SetActive(true);

            MonoBehaviour[] components = topicSelectionPanel.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp.GetType().Name == "PanelAnimator")
                {
                    comp.enabled = false;
                    break;
                }
            }

            topicSelectionPanel.SetActive(false);
            CanvasGroup cg = topicSelectionPanel.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
            topicSelectionPanel.SetActive(true);
        }

        if (learningModesPanel != null) learningModesPanel.SetActive(false);
        if (topicPanelBridge != null && topicPanelBridge.myPanelRoot != null)
            topicPanelBridge.myPanelRoot.SetActive(false);

        if (challengeManager != null) challengeManager.ForceHideAllPanels();

        if (progressManager != null && !string.IsNullOrEmpty(currentSelectedTopic))
        {
            float sessionTime = Time.time - topicSessionStartTime;
            progressManager.AddTimeSpent(currentSelectedTopic, sessionTime);
            progressManager.EndTopicSession();
            Debug.Log($"⏱️ Ended session for {currentSelectedTopic}. Time: {sessionTime:F1}s");
        }
        currentSelectedTopic = "";

        if (topicNameHeader != null) topicNameHeader.text = "Choose Topic";
        if (lessonCounterText != null) lessonCounterText.gameObject.SetActive(false);

        ResetTopicCardVisibility();
        UpdateTopicLockStates();
        StartCoroutine(RefreshProgressBarsOnly());

        Debug.Log("✅ Returned to topic selection");
    }
  
    public void ResetTopicCardVisibility()
    {
        if (topicCards == null) return;

        Debug.Log("[LearnPanel] ResetTopicCardVisibility — resetting all card alphas");

        foreach (var kvp in topicCards)
        {
            TopicCard card = kvp.Value;
            if (card == null || card.button == null) continue;

            CanvasGroup cg = card.button.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                if (cg.alpha == 0f)
                    cg.alpha = 1f;
            }

            card.button.gameObject.SetActive(true);

            Transform t = card.button.transform.parent;
            while (t != null && t.gameObject != topicSelectionPanel)
            {
                CanvasGroup parentCg = t.GetComponent<CanvasGroup>();
                if (parentCg != null && parentCg.alpha == 0f)
                    parentCg.alpha = 1f;
                t = t.parent;
            }
        }

        if (topicSelectionPanel != null)
        {
            MonoBehaviour[] components = topicSelectionPanel.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp.GetType().Name == "PanelAnimator")
                {
                    comp.enabled = false;
                    break;
                }
            }

            CanvasGroup panelCg = topicSelectionPanel.GetComponent<CanvasGroup>();
            if (panelCg != null)
            {
                panelCg.alpha          = 1f;
                panelCg.blocksRaycasts = true;
                panelCg.interactable   = true;
            }
            topicSelectionPanel.SetActive(true);
        }

        Debug.Log("[LearnPanel] ResetTopicCardVisibility done");
    }

    public void ForceRefreshFromDatabase()
    {
        Debug.Log("🔄 [LearnPanel] Force refresh triggered by external script");
        StartCoroutine(FetchProgressFromDatabase());
    }
    
    void OnDisable()
    {
        isFetchingProgress = false;
        if (progressManager != null && !string.IsNullOrEmpty(currentSelectedTopic))
        {
            float sessionTime = Time.time - topicSessionStartTime;
            progressManager.AddTimeSpent(currentSelectedTopic, sessionTime);
            Debug.Log($"⏱️ Saved time on disable: {sessionTime:F1}s");
        }
    }
    
    public void SelectTopic(string topicName)
    {
        if (!IsTopicUnlocked(topicName))
        {
            Debug.LogWarning($"🔒 Topic {topicName} is LOCKED");
            return;
        }
        
        currentSelectedTopic = topicName;
        
        Debug.Log("=== Topic Selected: " + topicName + " ===");
        
        PlayerPrefs.SetString("SelectedTopic", topicName);
        PlayerPrefs.Save();
        
        if (progressManager != null)
        {
            progressManager.StartTopicSession(topicName);
            topicSessionStartTime = Time.time;
        }
        
        if (topicNameHeader != null)
            topicNameHeader.text = topicName;
        
        if (lessonCounterText != null)
        {
            int lessonCount = GetLessonCount(topicName);
            lessonCounterText.text = lessonCount > 0 
                ? $"📚 {lessonCount} Lesson{(lessonCount != 1 ? "s" : "")} Available"
                : "Coming Soon";
            lessonCounterText.gameObject.SetActive(true);
        }
        
        if (topicSelectionPanel != null)
            topicSelectionPanel.SetActive(false);

        if (topicPanelBridge != null)
            topicPanelBridge.ShowTopic(currentSelectedTopic, true);
        else
            Debug.LogError("❌ topicPanelBridge not assigned on UpdatedLearnPanelController!");
    }
    
    void UpdateModeButtons()
    {
        if (progressManager == null) return;
        
        bool allLessonsRead = IsTopicReadingComplete(currentSelectedTopic);
        bool puzzleCompleted = progressManager.IsPuzzleCompleted(currentSelectedTopic);
        
        if (guidedBuildingButton != null)
            guidedBuildingButton.interactable = true;
        
        if (lessonsButton != null)
        {
            lessonsButton.interactable = true;
            TextMeshProUGUI buttonText = lessonsButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = allLessonsRead ? "✓ Review Lessons" : "Start Lessons";
        }
        
        if (lessonsInnerButton != null)
            lessonsInnerButton.interactable = true;
        
        bool puzzleEnabled = allLessonsRead;
        
        if (puzzleChallengeButton != null)
        {
            puzzleChallengeButton.interactable = puzzleEnabled;
            TextMeshProUGUI buttonText = puzzleChallengeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (!allLessonsRead)
                    buttonText.text = "🔒 Complete Lessons First";
                else if (puzzleCompleted)
                    buttonText.text = "✓ Puzzle Challenge";
                else
                    buttonText.text = "Puzzle Challenge";
            }
            
            if (puzzleLockOverlay != null)
                puzzleLockOverlay.SetActive(!allLessonsRead);
        }
        
        if (puzzleChallengeInnerButton != null)
            puzzleChallengeInnerButton.interactable = puzzleEnabled;
    }

    bool IsTopicReadingComplete(string topicName)
    {
        string currentUser = PlayerPrefs.GetString("CurrentUser", "");
        if (string.IsNullOrEmpty(currentUser)) return false;
        
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        
        Debug.Log($"🔍 Checking if {topicName} lessons are complete...");
        
        string flagKey = $"TopicReadComplete_{currentUser}_{normalizedTopic}";
        if (PlayerPrefs.GetInt(flagKey, 0) == 1)
        {
            Debug.Log($"   ✅ PERMANENT FLAG: Lessons marked complete");
            return true;
        }
        
        if (isProgressDataLoaded && cachedProgressData != null)
        {
            if (cachedProgressData.topics == null || cachedProgressData.topics.Count == 0)
            {
                Debug.Log($"   ❌ NEW USER - No progress data");
                return false;
            }
            
            var topicData = cachedProgressData.topics.Find(t =>
                TopicNameConstants.Normalize(t.topicName) == normalizedTopic);
            
            if (topicData != null)
            {
                int dbCompletedLessons = topicData.lessonsCompleted;
                int dbTotalLessons = topicLessonCounts.ContainsKey(normalizedTopic)
                    ? topicLessonCounts[normalizedTopic]
                    : 0;
                
                if (dbTotalLessons > 0)
                {
                    bool complete = dbCompletedLessons >= dbTotalLessons;
                    Debug.Log($"   ✅ DATABASE CHECK: {dbCompletedLessons}/{dbTotalLessons} = {complete}");
                    
                    if (complete)
                    {
                        PlayerPrefs.SetInt(flagKey, 1);
                        PlayerPrefs.Save();
                    }
                    
                    return complete;
                }
            }
            else
            {
                Debug.Log($"   ❌ Topic '{topicName}' not found in database");
                return false;
            }
        }
        
        TopicDetailPanel detailPanel = topicDetailPanel != null
            ? topicDetailPanel
            : FindObjectOfType<TopicDetailPanel>();
        
        if (detailPanel != null)
        {
            int localCompleted = detailPanel.GetCompletedLessonCountForTopic(topicName);
            int localTotal = detailPanel.GetTotalLessonCount(topicName);
            
            if (localTotal > 0)
            {
                bool complete = localCompleted >= localTotal;
                Debug.Log($"   ✅ TOPICDETAILPANEL: {localCompleted}/{localTotal} = {complete}");
                
                if (complete)
                {
                    PlayerPrefs.SetInt(flagKey, 1);
                    PlayerPrefs.Save();
                }
                
                return complete;
            }
        }
        
        Debug.Log($"   ❌ FINAL RESULT: Not complete");
        return false;
    }

    int GetCompletedLessonCount(string topicName)
    {
        TopicDetailPanel detailPanel = topicDetailPanel != null
            ? topicDetailPanel
            : FindObjectOfType<TopicDetailPanel>();
        
        if (detailPanel != null)
            return detailPanel.GetCompletedLessonCountForTopic(topicName);
        
        string currentUser = PlayerPrefs.GetString("CurrentUser", "");
        if (!string.IsNullOrEmpty(currentUser))
        {
            string normalizedTopic = TopicNameConstants.Normalize(topicName);
            return PlayerPrefs.GetInt($"{currentUser}_{normalizedTopic}_LessonsCompleted", 0);
        }
        
        return 0;
    }

    public void UpdatePuzzleButtonsAccess()
    {
        Debug.Log("=== UpdatePuzzleButtonsAccess called ===");
        
        if (!string.IsNullOrEmpty(currentSelectedTopic))
            UpdateModeButtons();
        
        UpdateTopicLockStates();
    }

    public void RefreshAfterTopicView()
    {
        Debug.Log("🔄 [LearnPanel] Refreshing after topic view - FETCHING FROM DATABASE");
        StartCoroutine(RefreshAfterTopicViewCoroutine());
    }

    private IEnumerator RefreshAfterTopicViewCoroutine()
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
        
        yield return new WaitForEndOfFrame();
        yield return StartCoroutine(FetchProgressFromDatabase());
        yield return StartCoroutine(FetchLessonCounts());
        
        UpdateAllTopicProgress();
        UpdateTopicLockStates();
        
        if (!string.IsNullOrEmpty(currentSelectedTopic))
        {
            bool lessonsComplete = IsTopicReadingComplete(currentSelectedTopic);
            if (lessonsComplete)
            {
                string currentUser = PlayerPrefs.GetString("CurrentUser", "");
                string normalizedTopic = TopicNameConstants.Normalize(currentSelectedTopic);
                PlayerPrefs.SetInt($"TopicReadComplete_{currentUser}_{normalizedTopic}", 1);
                PlayerPrefs.Save();
            }
            UpdateModeButtons();
        }
        
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
        
        Debug.Log("✅ [LearnPanel] Refresh complete with fresh database data");
    }

    public void ForceCheckTopicCompletion(string topicName)
    {
        bool isComplete = IsTopicReadingComplete(topicName);
        
        if (isComplete)
        {
            UpdateTopicLockStates();
            if (currentSelectedTopic == topicName)
                UpdateModeButtons();
        }
    }
    
    public void StartLessonsMode()
    {
        Debug.Log("=== START LESSONS MODE ===");
        
        if (string.IsNullOrEmpty(currentSelectedTopic))
        {
            Debug.LogError("❌ No topic selected!");
            return;
        }
        
        if (topicPanelBridge == null)
        {
            Debug.LogError("❌ TopicPanelBridge not assigned!");
            return;
        }
        
        topicPanelBridge.ShowTopic(currentSelectedTopic, true);
    }
    
    public void StartTutorialMode()
    {
        if (string.IsNullOrEmpty(currentSelectedTopic)) return;
        
        PlayerPrefs.SetString("QueueMode", "Tutorial");
        PlayerPrefs.Save();
        
        string sceneName = GetSceneName(currentSelectedTopic);
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogWarning("No scene found for topic: " + currentSelectedTopic);
    }
    
    public void StartChallengeMode()
    {
        Debug.Log("=== START CHALLENGE MODE CLICKED ===");
        
        if (string.IsNullOrEmpty(currentSelectedTopic))
        {
            Debug.LogError("❌ No topic selected!");
            return;
        }
        
        if (!IsTopicReadingComplete(currentSelectedTopic))
        {
            Debug.LogWarning("⚠️ Cannot start challenge - lessons not completed!");
            
            if (puzzleLockMessage != null)
            {
                puzzleLockMessage.text = "Please complete all lessons before attempting puzzles!";
                if (puzzleLockOverlay != null)
                    puzzleLockOverlay.SetActive(true);
            }
            return;
        }
        
        if (challengeManager == null)
        {
            Debug.LogError("❌ ChallengeManager is NOT assigned!");
            return;
        }
        
        PlayerPrefs.SetString("QueueMode", "Challenge");
        PlayerPrefs.Save();
        
        if (learningModesPanel != null)
            learningModesPanel.SetActive(false);
        
        challengeManager.StartChallenge(currentSelectedTopic);
    }

    public void ShowLearningModesPanel()
    {
        Debug.Log("=== ShowLearningModesPanel called ===");
        
        if (topicPanelBridge != null)
            topicPanelBridge.ReturnToLessonsList();
        else
            Debug.LogError("❌ topicPanelBridge not assigned!");
    }
    
    string GetSceneName(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);

        switch (topicName)
        {
            case TopicNameConstants.ARRAYS:       return "PhysicalArrayScene";
            case TopicNameConstants.STACKS:       return "PhysicalStackScene";
            case TopicNameConstants.QUEUE:        return "PhysicalQueueScene";
            case TopicNameConstants.LINKED_LISTS: return "LinkedListScene";
            case TopicNameConstants.TREES:        return "TreeScene";
            case TopicNameConstants.GRAPHS:       return "GraphScene";
            case TopicNameConstants.HASHMAPS:     return "HashmapScene";
            case TopicNameConstants.HEAPS:        return "HeapScene";
            case TopicNameConstants.DEQUE:        return "DequeScene";
            case TopicNameConstants.BINARY_HEAPS: return "BinaryHeapScene";
            default:
                Debug.LogWarning($"⚠️ No scene mapping for topic: {topicName}");
                return "";
        }
    }
}