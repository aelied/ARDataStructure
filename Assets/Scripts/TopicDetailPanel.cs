// ══════════════════════════════════════════════════════════════════════════════
//  CHANGES vs previous TopicDetailPanel.cs  (search "// CHANGED" to find them)
//
//  1. CompleteCurrentLessonThenLaunchAR()
//       → sets AR_MODE_PRESELECTED = "guided" before loading AR scene.
//
//  2. EnsureLessonsLoadedAndDisplay()  ← NEW CHANGE (speed fix)
//       → Skips FetchUserProgressFromDatabase() when progress is already cached
//         AND we are returning from AR (launchedFromARPanel == true).
//         This removes a full network round-trip that was making the lesson list
//         appear slow every time the user returned from AR.
//       → Added HasProgressData() helper.
//
//  Everything else is identical to the original.
// ══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;

/// <summary>
/// DATABASE-ONLY VERSION WITH 2D INTERACTIVE VISUALIZATIONS
/// SEQUENTIAL LEARNING IMPLEMENTED WITH LESSON COMPLETION DIALOG
/// ENHANCED WITH CARD-STYLE PARAGRAPH DISPLAY
/// FIXED: All content on one page unless explicitly split
/// UPDATED: AR flow — last lesson launches AR scene, returns to lesson quiz
/// UPDATED v2: Sets AR_MODE_PRESELECTED=guided so AR skips mode panel
/// UPDATED v3: Skips redundant progress fetch when returning from AR (speed fix)
/// </summary>
public class TopicDetailPanel : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject topicDetailPanel;
    public GameObject topicsGridPanel;
    public GameObject lessonsScrollView;
    public GameObject lessonContentPanel;
    
    [Header("Lesson Completion Dialog")]
    public GameObject lessonCompletionDialog;
    public TextMeshProUGUI completionDialogTitle;
    public TextMeshProUGUI completionDialogMessage;
    public Button continueToNextButton;
    public Button stayOnLessonButton;
    
    [Header("Header Components")]
    public Button backButton;
    public TextMeshProUGUI titleText;
    
    [Header("Content Page Navigation")]
    public Transform lessonsContainer;
    public GameObject lessonModulePrefab;
    public Button previousPageButton;
    public Button nextPageButton;
    public TextMeshProUGUI nextPageButtonText; 
    public TextMeshProUGUI pageCounterText;
    
    [Header("Topic Icons")]
    public Sprite arraysIcon;
    public Sprite queueIcon;
    public Sprite stacksIcon;
    public Sprite linkedListsIcon;
    public Sprite treesIcon;
    public Sprite graphsIcon;
    public Sprite defaultIcon;
    public Sprite lockedIcon; 
    
    [Header("Lesson Content Components")]
    public TextMeshProUGUI lessonContentTitle;
    public TextMeshProUGUI lessonContentDescription;
    public Transform contentCardsContainer;
    public GameObject paragraphCardPrefab;
    public ScrollRect lessonContentScrollRect;
    public Image topicIconImage;
    
    public Button markCompleteButton;
    public TextMeshProUGUI markCompleteButtonText;
    
    [Header("Interactive Visualization")]
    public Interactive2DVisualizer visualizer;
    public Button toggleVisualizerButton;
    public TextMeshProUGUI toggleVisualizerButtonText;
    private bool isVisualizerActive = false;

    private string userDifficultyLevel = "beginner";

    [Header("AR Integration")]
    public bool   launchedFromARPanel = false;  // set true by TopicPanelBridge when coming from AR panel
    public string arSceneName         = "";     // e.g. "PhysicalArrayScene" — set by TopicPanelBridge
    
    [Header("API Settings")]
    public string adminApiUrl = "https://structureality-admin.onrender.com/api";
    
    [Header("Loading Indicator")]
    public GameObject loadingIndicator;
    
    private string currentTopicName;
    private Dictionary<string, List<LessonModule>> cachedLessons = new Dictionary<string, List<LessonModule>>();
    private LessonModule currentLesson;
    private int currentLessonIndex = 0;
    private int currentPageIndex = 0;
    private List<LessonModule> currentTopicLessons;
    private Dictionary<string, int> completedLessonsCount = new Dictionary<string, int>();
    private string currentUsername;
    public bool isLoadingData = false;
    private bool cameFromLearnPanel = false;
    private float lessonStartTime;
    private float totalLessonTime;

    [HideInInspector] public bool controlledByBridge = false;
    
    [System.Serializable]
    public class LessonModule
    {
        public string title;
        public string description;
        public Sprite icon;
        public bool isCompleted;
        public bool isLocked;
        public string lessonId;
        public string content;
        public List<string> contentPages;
    }
    
    void Awake()
    {
        Debug.Log("🔧 TopicDetailPanel Awake() - DATABASE MODE WITH CARD DISPLAY");
        currentUsername = PlayerPrefs.GetString("CurrentUser", "");
        
        if (string.IsNullOrEmpty(currentUsername))
        {
            Debug.LogError("❌ No user logged in!");
        }
    }
    
    void Start()
    {
        Debug.Log("=== TopicDetailPanel Start() ===");
        FixUIResponsiveness();

        string currentUser = PlayerPrefs.GetString("CurrentUser", "");
        if (!string.IsNullOrEmpty(currentUser))
        {
            userDifficultyLevel = PlayerPrefs.GetString($"User_{currentUser}_DifficultyLevel", "beginner");
            Debug.Log($"📊 User difficulty level: {userDifficultyLevel}");
        }
        
        if (markCompleteButton != null)
        {
            markCompleteButton.gameObject.SetActive(false);
        }
        
        if (previousPageButton != null)
        {
            previousPageButton.onClick.RemoveAllListeners();
            previousPageButton.onClick.AddListener(ShowPreviousPage);
        }
        
        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveAllListeners();
            nextPageButton.onClick.AddListener(HandleNextNavigation);
        }
        
        if (toggleVisualizerButton != null)
        {
            toggleVisualizerButton.onClick.RemoveAllListeners();
            toggleVisualizerButton.onClick.AddListener(ToggleVisualizer);
        }
        
        if (continueToNextButton != null)
        {
            continueToNextButton.onClick.RemoveAllListeners();
            continueToNextButton.onClick.AddListener(OnContinueToNextLesson);
        }
        
        if (stayOnLessonButton != null)
        {
            stayOnLessonButton.onClick.RemoveAllListeners();
            stayOnLessonButton.onClick.AddListener(OnStayOnLesson);
        }
        
        if (topicDetailPanel != null)
        {
            CanvasGroup cg = topicDetailPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = topicDetailPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }
        
        if (lessonContentPanel != null)
            lessonContentPanel.SetActive(false);
        
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
        
        if (lessonCompletionDialog != null)
            lessonCompletionDialog.SetActive(false);
        
        if (visualizer != null && visualizer.visualizationPanel != null)
            visualizer.visualizationPanel.SetActive(false);
        
        Debug.Log("=== TopicDetailPanel Start() Complete ===");
    }
    
    void ToggleVisualizer()
    {
        if (visualizer == null) return;
        
        isVisualizerActive = !isVisualizerActive;
        
        if (isVisualizerActive)
        {
            visualizer.InitializeVisualization(currentTopicName);
            if (toggleVisualizerButtonText != null)
                toggleVisualizerButtonText.text = "Hide Visualizer";
            Debug.Log($"🎨 Showing visualizer for {currentTopicName}");
        }
        else
        {
            visualizer.HideVisualization();
            if (toggleVisualizerButtonText != null)
                toggleVisualizerButtonText.text = "Show Interactive";
            Debug.Log("📖 Hiding visualizer");
        }
    }
    
    void ShowLoadingIndicator(bool show)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(show);
    }
    
    IEnumerator FetchUserProgressFromDatabase()
    {
        if (string.IsNullOrEmpty(currentUsername) || string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.LogWarning("⚠️ Cannot fetch: No username or API URL");
            yield break;
        }
        
        isLoadingData = true;
        ShowLoadingIndicator(true);
        
        string url = $"{adminApiUrl}/progress/{currentUsername}";
        Debug.Log($"🔄 Fetching from Render: {url}");
        
        int maxRetries = 2;
        int attempt = 0;
        bool success = false;
        
        while (attempt < maxRetries && !success)
        {
            attempt++;
            
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 90;
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                success = true;
                Debug.Log("✅ User progress fetched successfully");
                
                try
                {
                    DatabaseProgressResponse response = JsonUtility.FromJson<DatabaseProgressResponse>(request.downloadHandler.text);
                    
                    if (response != null && response.success && response.data != null)
                    {
                        completedLessonsCount.Clear();
                        
                        foreach (var topic in response.data.topics)
                        {
                            string normalizedTopic = TopicNameConstants.Normalize(topic.topicName);
                            completedLessonsCount[normalizedTopic] = topic.lessonsCompleted;
                        }
                        
                        SyncLessonCompletionFlags();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Parse error: {e.Message}");
                }
            }
            else if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogWarning($"⚠️ Connection error on attempt {attempt}: {request.error}");
                yield return new WaitForSeconds(5f);
            }
            else
            {
                Debug.LogError($"❌ Failed to fetch progress: {request.error}");
                break;
            }
        }
        
        ShowLoadingIndicator(false);
        isLoadingData = false;
    }

    // ── CHANGED v3: helper — true when we already have progress data in memory ──
    bool HasProgressData()
    {
        return completedLessonsCount.Count > 0;
    }

    public bool IsLessonCompleted(string topicName, int lessonIndex)
    {
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        
        if (completedLessonsCount.ContainsKey(normalizedTopic))
        {
            return lessonIndex < completedLessonsCount[normalizedTopic];
        }
        
        return false;
    }
    
    public bool IsTopicFullyCompleted(string topicName)
    {
        List<LessonModule> lessons = GetLessonsForTopic(topicName);
        if (lessons.Count == 0) return false;
        
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        
        if (completedLessonsCount.ContainsKey(normalizedTopic))
        {
            return completedLessonsCount[normalizedTopic] >= lessons.Count;
        }
        
        return false;
    }
    
    IEnumerator SyncLessonCompletionToDatabase(string topicName, int lessonsCompleted)
    {
        if (string.IsNullOrEmpty(currentUsername) || string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.LogWarning("⚠️ Cannot sync: No username or API URL");
            yield break;
        }
        
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        
        Debug.Log($"🔄 Syncing to database: {normalizedTopic} = {lessonsCompleted} lessons");
        
        string syncUrl = $"{adminApiUrl}/progress/{currentUsername}/lessons";
        
        string jsonData = $@"{{
        ""topicName"": ""{normalizedTopic}"",
        ""lessonsCompleted"": {lessonsCompleted},
        ""difficultyLevel"": ""{userDifficultyLevel}""
    }}";
        
        Debug.Log($"📤 Sending to: {syncUrl}");
        Debug.Log($"📦 JSON data: {jsonData}");
        
        UnityWebRequest request = new UnityWebRequest(syncUrl, "PUT");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 30;
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"✅ Database updated: {normalizedTopic} = {lessonsCompleted} lessons");
            Debug.Log($"📥 Server response: {request.downloadHandler.text}");
            
            if (UserProgressManager.Instance != null)
            {
                Debug.Log($"🔄 Updating UserProgressManager: {normalizedTopic} = {lessonsCompleted}");
                UserProgressManager.Instance.UpdateLessonCount(normalizedTopic, lessonsCompleted);
                UserProgressManager.Instance.ManualSync();
                Debug.Log("✅ UserProgressManager updated and synced");
            }
            else
            {
                Debug.LogError("❌ UserProgressManager.Instance is NULL!");
            }
            
            MainMenuManager mainMenu = FindObjectOfType<MainMenuManager>();
            if (mainMenu != null && mainMenu.gameObject.activeInHierarchy)
            {
                Debug.Log("🔄 Triggering immediate MainMenu progress update");
                mainMenu.RefreshProgressFromExternal();
            }
        }
        else
        {
            Debug.LogError($"❌ Database sync failed: {request.error}");
            Debug.LogError($"❌ Response code: {request.responseCode}");
            Debug.LogError($"❌ Response: {request.downloadHandler.text}");
        }
    }
    
    void UpdateMarkCompleteButton()
    {
        if (markCompleteButton == null || currentLesson == null) return;
        
        bool isCompleted = IsLessonCompleted(currentTopicName, currentLessonIndex);
        
        if (markCompleteButtonText != null)
        {
            markCompleteButtonText.text = isCompleted ? "✓ Completed" : "Mark as Complete";
        }
        
        markCompleteButton.interactable = !isCompleted;
    }
    
    void NotifyTopicCompletion(string topicName)
    {
        UpdatedLearnPanelController learnPanel = FindObjectOfType<UpdatedLearnPanelController>();
        if (learnPanel != null)
        {
            learnPanel.UpdatePuzzleButtonsAccess();
        }
    }
    
    public int GetTotalLessonCount(string topicName)
    {
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        
        if (cachedLessons.ContainsKey(normalizedTopic))
        {
            int count = cachedLessons[normalizedTopic].Count;
            Debug.Log($"✓ TopicDetailPanel: {topicName} has {count} lessons");
            return count;
        }
        
        Debug.LogWarning($"⚠️ TopicDetailPanel: No cached lessons for {topicName}");
        return 0;
    }

    public void ForceRefreshPuzzleAccess()
    {
        UpdatedLearnPanelController learnPanel = FindObjectOfType<UpdatedLearnPanelController>();
        if (learnPanel != null)
        {
            learnPanel.UpdatePuzzleButtonsAccess();
        }
    }
    
    void ShowPreviousPage()
    {
        if (currentLesson == null || currentLesson.contentPages == null || currentLesson.contentPages.Count == 0)
            return;
        
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdateContentDisplay();
            Debug.Log($"← Previous page: {currentPageIndex + 1}/{currentLesson.contentPages.Count}");
        }
    }
    
    void HandleNextNavigation()
    {
        if (currentLesson == null || currentLesson.contentPages == null || currentLesson.contentPages.Count == 0)
            return;

        if (currentPageIndex < currentLesson.contentPages.Count - 1)
        {
            currentPageIndex++;
            UpdateContentDisplay();
            return;
        }

        // Last page reached
        // ── AR mode: always launch AR, skip completed check ──────────────
        if (launchedFromARPanel)
        {
            StartCoroutine(CompleteCurrentLessonThenLaunchAR());
            return;
        }

        // ── Normal mode ───────────────────────────────────────────────────
        bool isAlreadyCompleted = IsLessonCompleted(currentTopicName, currentLessonIndex);

        if (isAlreadyCompleted)
            ShowAlreadyCompletedDialog();
        else
            ShowLessonCompletionDialog();
    }

    void ShowAlreadyCompletedDialog()
    {
        if (lessonCompletionDialog == null)
        {
            AdvanceToNextLesson();
            return;
        }

        bool isLastLesson = (currentLessonIndex == currentTopicLessons.Count - 1);
        
        if (completionDialogTitle != null)
            completionDialogTitle.text = "Already Completed!";
        
        if (completionDialogMessage != null)
        {
            if (isLastLesson)
            {
                completionDialogMessage.text =
                    $"You've already completed <b>{currentLesson.title}</b>!\n\n" +
                    "This is the last lesson in this topic.";
            }
            else
            {
                string nextLessonTitle = currentTopicLessons[currentLessonIndex + 1].title;
                completionDialogMessage.text =
                    $"You've already completed <b>{currentLesson.title}</b>!\n\n" +
                    $"Next lesson: <b>{nextLessonTitle}</b>";
            }
        }
        
        if (continueToNextButton != null)
        {
            TextMeshProUGUI buttonText = continueToNextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = isLastLesson ? "Back to Topic" : "Continue to Next";
            continueToNextButton.gameObject.SetActive(true);
            continueToNextButton.onClick.RemoveAllListeners();
            continueToNextButton.onClick.AddListener(AdvanceToNextLesson);
        }
        
        if (stayOnLessonButton != null)
            stayOnLessonButton.gameObject.SetActive(false);
        
        lessonCompletionDialog.SetActive(true);
        Debug.Log("📢 Showing already completed dialog");
    }

    void AdvanceToNextLesson()
    {
        if (lessonCompletionDialog != null)
            lessonCompletionDialog.SetActive(false);
        
        if (currentLessonIndex < currentTopicLessons.Count - 1)
        {
            Debug.Log("➡️ Moving to next lesson (already completed, no save)...");
            ShowLessonAtIndex(currentLessonIndex + 1);
        }
        else
        {
            Debug.Log("✅ All lessons reviewed - returning to list");
            OnBackButtonClicked();
        }
    }

    void ShowLessonCompletionDialog()
    {
        // ── AR mode: every lesson goes to AR scene ─────────────────────
        if (launchedFromARPanel)
        {
            StartCoroutine(CompleteCurrentLessonThenLaunchAR());
            return;
        }

        // ── Normal Learn panel flow below ──────────────────────────────
        bool isLastLesson = (currentLessonIndex == currentTopicLessons.Count - 1);

        if (lessonCompletionDialog == null)
        {
            StartCoroutine(CompleteCurrentLessonAndAdvance());
            return;
        }

        if (continueToNextButton != null)
        {
            continueToNextButton.onClick.RemoveAllListeners();
            continueToNextButton.onClick.AddListener(OnContinueToNextLesson);
            continueToNextButton.gameObject.SetActive(true);
        }

        if (stayOnLessonButton != null)
            stayOnLessonButton.gameObject.SetActive(true);

        if (completionDialogTitle != null)
            completionDialogTitle.text = "Lesson Complete!";

        if (completionDialogMessage != null)
        {
            if (isLastLesson)
            {
                completionDialogMessage.text =
                    $"You've finished <b>{currentLesson.title}</b>!\n\n" +
                    "This is the last lesson in this topic.\n" +
                    "Ready to complete the topic?";
            }
            else
            {
                string nextLessonTitle = currentTopicLessons[currentLessonIndex + 1].title;
                completionDialogMessage.text =
                    $"You've finished <b>{currentLesson.title}</b>!\n\n" +
                    $"Next up: <b>{nextLessonTitle}</b>\n\n" +
                    "Would you like to continue?";
            }
        }

        if (continueToNextButton != null)
        {
            TextMeshProUGUI buttonText = continueToNextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = isLastLesson ? "Finish Topic" : "Continue";
        }

        lessonCompletionDialog.SetActive(true);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  AR FLOW: complete lesson → save context → load AR scene
    //
    //  CHANGED v2: Sets AR_MODE_PRESELECTED = "guided" so that
    //  ARModeSelectionManager bypasses the Guided/Sandbox panel and starts
    //  Guided mode immediately when the student arrives from the lessons flow.
    // ══════════════════════════════════════════════════════════════════════
    IEnumerator CompleteCurrentLessonThenLaunchAR()
    {
        // ── 1. Mark lesson complete and sync ─────────────────────────────
        if (!currentLesson.isCompleted)
        {
            currentLesson.isCompleted = true;

            string normalizedTopic = TopicNameConstants.Normalize(currentTopicName);

            if (!completedLessonsCount.ContainsKey(normalizedTopic))
                completedLessonsCount[normalizedTopic] = 0;

            if (currentLessonIndex == completedLessonsCount[normalizedTopic])
            {
                completedLessonsCount[normalizedTopic]++;

                string currentUser = PlayerPrefs.GetString("CurrentUser", "");
                PlayerPrefs.SetInt(
                    $"{currentUser}_{normalizedTopic}_LessonsCompleted",
                    completedLessonsCount[normalizedTopic]);
                PlayerPrefs.SetString("AR_SessionTimestamp", System.DateTime.Now.ToString("yyyyMMddHH"));
                PlayerPrefs.Save();
            }

            yield return StartCoroutine(
                SyncLessonCompletionToDatabase(
                    currentTopicName,
                    completedLessonsCount[TopicNameConstants.Normalize(currentTopicName)]));

            yield return new WaitForSeconds(0.5f);
        }

        // ── 2. Save AR return context ─────────────────────────────────────
        PlayerPrefs.SetInt   (ARReturnHandler.AR_LESSON_INDEX_KEY,  currentLessonIndex);
        PlayerPrefs.SetString(ARReturnHandler.AR_TOPIC_NAME_KEY,
                              TopicNameConstants.Normalize(currentTopicName));
        PlayerPrefs.SetString(ARReturnHandler.AR_LESSON_TITLE_KEY,
                              currentLesson?.title ?? "");

        // ── CHANGED v2: pre-select Guided so ARModeSelectionManager skips its panel ──
        PlayerPrefs.SetString(ARModeSelectionManager.AR_MODE_PRESELECTED_KEY, "guided");
        // ─────────────────────────────────────────────────────────────────────────

        PlayerPrefs.Save();

        Debug.Log($"[TopicDetailPanel] AR context saved → " +
                  $"topic={currentTopicName} lesson={currentLessonIndex} " +
                  $"title={currentLesson?.title} scene={arSceneName} mode=guided");

        // ── 3. Guard ──────────────────────────────────────────────────────
        if (string.IsNullOrEmpty(arSceneName))
        {
            Debug.LogError("[TopicDetailPanel] arSceneName is empty! " +
                           "Assign it in TopicPanelBridge or ARPanelController Inspector.");
            yield break;
        }

        // ── 4. Load AR scene ──────────────────────────────────────────────
        Debug.Log($"[TopicDetailPanel] Loading AR scene (Guided): {arSceneName}");
        UnityEngine.SceneManagement.SceneManager.LoadScene(arSceneName);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  BUTTON DISPLAY — "🚀 Try it in AR!" on last lesson when in AR mode
    // ══════════════════════════════════════════════════════════════════════
    void UpdateContentDisplay()
    {
        if (currentLesson == null) return;
        
        if (contentCardsContainer != null)
        {
            foreach (Transform child in contentCardsContainer)
                Destroy(child.gameObject);
        }
        
        if (currentLesson.contentPages != null && currentLesson.contentPages.Count > 0)
        {
            string pageContent = currentLesson.contentPages[currentPageIndex];
            CreateContentCards(pageContent);
        }
        
        if (pageCounterText != null && currentLesson.contentPages != null)
            pageCounterText.text = $"Page {currentPageIndex + 1} of {currentLesson.contentPages.Count}";
        
        if (previousPageButton != null)
            previousPageButton.interactable = (currentPageIndex > 0);
            
        if (nextPageButton != null && currentLesson.contentPages != null)
        {
            bool isAlreadyCompleted = IsLessonCompleted(currentTopicName, currentLessonIndex);
            bool isLastPage         = (currentPageIndex == currentLesson.contentPages.Count - 1);
            bool isLastLesson       = (currentLessonIndex == currentTopicLessons.Count - 1);
            
            nextPageButton.interactable = true;
            
            if (nextPageButtonText != null)
            {
                if (!isLastPage)
                {
                    nextPageButtonText.text = "Next Page";
                }
                else if (launchedFromARPanel && !string.IsNullOrEmpty(arSceneName))
                {
                    // AR mode — every lesson's last page shows "Try it in AR!"
                    nextPageButtonText.text = isAlreadyCompleted
                        ? "Try it in AR"
                        : "Try it in AR!";
                }
                else if (!isLastLesson)
                {
                    nextPageButtonText.text = isAlreadyCompleted
                        ? "Next Lesson"
                        : "Complete Lesson";
                }
                else
                {
                    nextPageButtonText.text = isAlreadyCompleted
                        ? "Back to Topic"
                        : "Finish Lesson";
                }
            }
        }
        
        StartCoroutine(ResetScrollPosition());
    }

    void CreateContentCards(string content)
    {
        if (contentCardsContainer == null || paragraphCardPrefab == null)
        {
            Debug.LogWarning("⚠️ Content cards container or prefab not assigned!");
            return;
        }

        string[] paragraphs = content.Split(new string[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph)) continue;
            CreateParagraphCard(paragraph.Trim());
        }
    }

    void CreateParagraphCard(string paragraph)
    {
        GameObject cardObj = Instantiate(paragraphCardPrefab, contentCardsContainer);
        
        Image cardBackground = cardObj.GetComponent<Image>();
        
        Transform leftBorderTransform = cardObj.transform.Find("LeftBorder");
        Image leftBorder = leftBorderTransform != null ? leftBorderTransform.GetComponent<Image>() : null;
        
        Transform contentContainer = cardObj.transform.Find("ContentContainer");
        if (contentContainer == null)
        {
            Debug.LogWarning("⚠️ ContentContainer not found in prefab!");
            return;
        }
        
        Transform titleTransform = contentContainer.Find("TitleText");
        TextMeshProUGUI titleText = titleTransform != null ? titleTransform.GetComponent<TextMeshProUGUI>() : null;
        
        Transform bodyTransform = contentContainer.Find("BodyText");
        TextMeshProUGUI bodyText = bodyTransform != null ? bodyTransform.GetComponent<TextMeshProUGUI>() : null;
        
        if (bodyText == null)
        {
            Debug.LogWarning("⚠️ BodyText not found in ContentContainer!");
            return;
        }

        string boxType = DetectBoxType(paragraph, out string title, out string body);
        
        if (!string.IsNullOrEmpty(boxType))
        {
            ApplySpecialBoxStyling(cardBackground, leftBorder, titleText, bodyText, boxType, title, body);
            if (titleText != null)
                titleText.gameObject.SetActive(true);
        }
        else
        {
            if (leftBorder != null)
                leftBorder.gameObject.SetActive(false);
            
            if (titleText != null)
                titleText.gameObject.SetActive(false);
            
            bodyText.text = paragraph;
            bodyText.color = HexToColor("#263238");
            
            if (cardBackground != null)
                cardBackground.color = Color.white;
        }
    }

    string DetectBoxType(string paragraph, out string title, out string body)
    {
        title = "";
        body = paragraph;
        
        string[] tags = { "[INFO]", "[TIP]", "[WARNING]", "[NOTE]", "[IMPORTANT]" };
        string[] types = { "INFO", "TIP", "WARNING", "NOTE", "IMPORTANT" };
        string[] defaultTitles = { "Information", "Tip", "Warning", "Note", "Important" };
        int[] lengths = { 6, 5, 9, 6, 11 };
        
        for (int i = 0; i < tags.Length; i++)
        {
            if (paragraph.StartsWith(tags[i], System.StringComparison.OrdinalIgnoreCase))
            {
                body = paragraph.Substring(lengths[i]).Trim();
                int newlineIndex = body.IndexOf('\n');
                if (newlineIndex > 0)
                {
                    title = body.Substring(0, newlineIndex).Trim();
                    body = body.Substring(newlineIndex + 1).Trim();
                }
                else
                {
                    title = defaultTitles[i];
                }
                return types[i];
            }
        }
        
        return null;
    }

    void ApplySpecialBoxStyling(Image background, Image leftBorder, TextMeshProUGUI titleText,
                                TextMeshProUGUI bodyText, string boxType, string title, string body)
    {
        string icon = "";
        Color bgColor = Color.white;
        Color borderColor = Color.cyan;
        Color titleColor = Color.black;
        Color textColor = Color.black;

        switch (boxType)
        {
            case "INFO":
                bgColor = HexToColor("#E0F7FA"); borderColor = HexToColor("#00BCD4");
                titleColor = HexToColor("#006064"); textColor = HexToColor("#00695C");
                break;
            case "TIP":
                bgColor = HexToColor("#FFF9C4"); borderColor = HexToColor("#FFC107");
                titleColor = HexToColor("#F57F17"); textColor = HexToColor("#F57F17");
                break;
            case "WARNING":
                bgColor = HexToColor("#FFEBEE"); borderColor = HexToColor("#F44336");
                titleColor = HexToColor("#C62828"); textColor = HexToColor("#C62828");
                break;
            case "NOTE":
                bgColor = HexToColor("#F3E5F5"); borderColor = HexToColor("#9C27B0");
                titleColor = HexToColor("#6A1B9A"); textColor = HexToColor("#6A1B9A");
                break;
            case "IMPORTANT":
                bgColor = HexToColor("#E8F5E9"); borderColor = HexToColor("#4CAF50");
                titleColor = HexToColor("#2E7D32"); textColor = HexToColor("#2E7D32");
                break;
        }

        if (background != null) background.color = bgColor;

        if (leftBorder != null)
        {
            leftBorder.gameObject.SetActive(true);
            leftBorder.color = borderColor;
        }

        if (titleText != null)
        {
            titleText.text = $"{icon} <b>{title}</b>";
            titleText.color = titleColor;
        }

        bodyText.text = body;
        bodyText.color = textColor;
    }

    Color HexToColor(string hex)
    {
        hex = hex.Replace("#", "");
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color32(r, g, b, 255);
    }

    void ShowLessonAtIndex(int index)
    {
        if (currentTopicLessons == null || index < 0 || index >= currentTopicLessons.Count)
        {
            Debug.LogWarning($"Invalid lesson index: {index}");
            return;
        }
        
        if (currentLesson != null)
        {
            float lessonTime = Time.time - lessonStartTime;
            totalLessonTime += lessonTime;
            
            if (UserProgressManager.Instance != null)
            {
                UserProgressManager.Instance.AddTimeSpent(currentTopicName, lessonTime);
                Debug.Log($"⏱️ Added {lessonTime:F1}s to {currentTopicName} (Total: {totalLessonTime:F1}s)");
            }
        }
        
        lessonStartTime = Time.time;
        
        if (visualizer != null)
        {
            visualizer.ClearVisualization();
            visualizer.HideVisualization();
        }
        isVisualizerActive = false;
        if (toggleVisualizerButtonText != null)
            toggleVisualizerButtonText.text = "Show Interactive";
        
        currentLessonIndex = index;
        currentLesson = currentTopicLessons[index];
        currentPageIndex = 0;
        
        currentLesson.isCompleted = IsLessonCompleted(currentTopicName, index);
        
        Debug.Log($"Showing lesson {index + 1}/{currentTopicLessons.Count}: {currentLesson.title}");
        
        if (lessonContentTitle != null)
            lessonContentTitle.text = currentLesson.title;
        
        if (lessonContentDescription != null)
            lessonContentDescription.text = currentLesson.description;
        
        if (topicIconImage != null)
        {
            Sprite topicIcon = GetIconForTopic(currentTopicName);
            if (topicIcon != null)
            {
                topicIconImage.sprite = topicIcon;
                topicIconImage.gameObject.SetActive(true);
            }
            else
            {
                topicIconImage.gameObject.SetActive(false);
            }
        }
        
        if (currentLesson.contentPages == null || currentLesson.contentPages.Count == 0)
            SplitContentIntoPages();
        
        UpdateContentDisplay();
        
        if (titleText != null)
            titleText.text = $"{currentTopicName} - {currentLesson.title}";
        
        UpdateMarkCompleteButton();
        
        if (toggleVisualizerButton != null)
            toggleVisualizerButton.gameObject.SetActive(true);
        
        StartCoroutine(ResetScrollPosition());
    }

    void SplitContentIntoPages()
    {
        if (currentLesson == null) return;
        
        string fullContent = currentLesson.content ?? GetLessonContent(currentLesson.title);
        currentLesson.contentPages = new List<string>();
        
        const int MAX_CHARS_PER_PAGE = 2000;
        
        if (fullContent.Contains("<<<PAGE_BREAK>>>"))
        {
            string[] pages = fullContent.Split(new string[] { "<<<PAGE_BREAK>>>" }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string page in pages)
                currentLesson.contentPages.Add(page.Trim());
        }
        else if (fullContent.Length <= MAX_CHARS_PER_PAGE)
        {
            currentLesson.contentPages.Add(fullContent);
        }
        else
        {
            string[] paragraphs = fullContent.Split(new string[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            string currentPage = "";
            
            foreach (string paragraph in paragraphs)
            {
                if (currentPage.Length + paragraph.Length + 4 > MAX_CHARS_PER_PAGE && currentPage.Length > 0)
                {
                    currentLesson.contentPages.Add(currentPage.Trim());
                    currentPage = paragraph + "\n\n";
                }
                else
                {
                    currentPage += paragraph + "\n\n";
                }
            }
            
            if (!string.IsNullOrWhiteSpace(currentPage))
                currentLesson.contentPages.Add(currentPage.Trim());
        }
        
        if (currentLesson.contentPages.Count == 0)
            currentLesson.contentPages.Add("No content available.");
    }
    
    IEnumerator FetchLessonsFromServer()
    {
        Debug.Log("🚀 Fetching lessons from server...");
        
        string url = $"{adminApiUrl}/lessons";
        UnityWebRequest request = UnityWebRequest.Get(url);
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                LessonsResponse response = JsonUtility.FromJson<LessonsResponse>(request.downloadHandler.text);
                
                if (response != null && response.success && response.lessons != null)
                {
                    cachedLessons.Clear();
                    foreach (var lesson in response.lessons)
                    {
                        string lessonDifficulty = lesson.difficultyLevel ?? "beginner";
                        
                        if (lessonDifficulty == userDifficultyLevel)
                        {
                            string normalizedTopic = TopicNameConstants.Normalize(lesson.topicName);
                            
                            if (!cachedLessons.ContainsKey(normalizedTopic))
                                cachedLessons[normalizedTopic] = new List<LessonModule>();
                            
                            cachedLessons[normalizedTopic].Add(new LessonModule
                            {
                                title = lesson.title,
                                description = lesson.description,
                                isCompleted = false,
                                lessonId = lesson._id,
                                content = !string.IsNullOrEmpty(lesson.content) ? lesson.content : GetLessonContent(lesson.title),
                                contentPages = null,
                                icon = GetIconForTopic(normalizedTopic)
                            });
                        }
                    }
                    Debug.Log($"✅ Cached {cachedLessons.Count} topics (filtered by {userDifficultyLevel} level)");
                    
                    foreach (var kvp in cachedLessons)
                    {
                        if (UserProgressManager.Instance != null)
                            UserProgressManager.Instance.SetTotalLessonsForTopic(kvp.Key, kvp.Value.Count);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Parse error: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"❌ Fetch failed: {request.error}");
        }
    }
    
    public void ShowTopicDetail(string topicName, bool fromLearnPanel = false)
    {
        Debug.Log($"=== ShowTopicDetail: {topicName} (Level: {userDifficultyLevel}) ===");
        
        cameFromLearnPanel = fromLearnPanel;
        currentTopicName = topicName;
        currentLessonIndex = 0;
        currentPageIndex = 0;
        currentTopicLessons = null;
        isVisualizerActive = false;
        lessonStartTime = Time.time;
        totalLessonTime = 0f;
        
        if (UserProgressManager.Instance != null)
        {
            UserProgressManager.Instance.StartTopicSession(topicName);
            Debug.Log($"⏱️ Started time tracking for topic: {topicName}");
        }
        
        if (visualizer != null)
        {
            visualizer.ClearVisualization();
            visualizer.HideVisualization();
        }

        if (topicsGridPanel != null) topicsGridPanel.SetActive(false);

        if (topicDetailPanel != null)
        {
            CanvasGroup cg = topicDetailPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = topicDetailPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        if (lessonsScrollView != null) lessonsScrollView.SetActive(true);
        if (lessonContentPanel != null) lessonContentPanel.SetActive(false);
        
        if (titleText != null)
            titleText.text = topicName;
        
        if (toggleVisualizerButton != null)
            toggleVisualizerButton.gameObject.SetActive(false);
        
        StartCoroutine(EnsureLessonsLoadedAndDisplay(topicName));
    }

    // ── CHANGED v3: skip redundant network fetch when returning from AR ───────
    // Previously this ALWAYS called FetchUserProgressFromDatabase(), which is a
    // full HTTP round-trip (up to 90s timeout) every time the lessons panel
    // opened. Now:
    //   • If lessons are not yet cached → fetch both (cold start, normal flow).
    //   • If coming back from AR and progress is already in memory → skip the
    //     progress fetch and go straight to LoadLessons(). This makes the list
    //     appear instantly.
    //   • If lessons are cached but we're NOT from AR (e.g. normal Learn panel
    //     open) → still refresh progress so the completion state is up to date.
    IEnumerator EnsureLessonsLoadedAndDisplay(string topicName)
    {
        ShowLoadingIndicator(true);

        // Always fetch lessons if not yet cached
        if (cachedLessons.Count == 0 && !string.IsNullOrEmpty(adminApiUrl))
            yield return StartCoroutine(FetchLessonsFromServer());

        // Skip progress fetch when returning from AR and we already have data —
        // the completion state was already synced before launching the AR scene.
        bool skipProgressFetch = launchedFromARPanel && HasProgressData();

        if (!skipProgressFetch)
            yield return StartCoroutine(FetchUserProgressFromDatabase());
        else
            Debug.Log("[TopicDetailPanel] Skipping progress fetch (returning from AR, data already cached)");

        ShowLoadingIndicator(false);
        LoadLessons(topicName);
    }
    // ─────────────────────────────────────────────────────────────────────────
    
    void LoadLessons(string topicName)
    {
        Debug.Log($"=== Loading lessons for: {topicName} ===");
        
        ClearLessons();
        currentTopicLessons = GetLessonsForTopic(topicName);
        Debug.Log($"Found {currentTopicLessons.Count} lessons for {topicName}");
        
        if (lessonsContainer == null) return;
        
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        int completedCount = completedLessonsCount.ContainsKey(normalizedTopic) ? completedLessonsCount[normalizedTopic] : 0;

        int createdCount = 0;
        for (int i = 0; i < currentTopicLessons.Count; i++)
        {
            LessonModule lesson = currentTopicLessons[i];
            bool isUnlocked = (i <= completedCount);
            lesson.isCompleted = (i < completedCount);
            lesson.isLocked = !isUnlocked;
            
            if (lesson.icon == null)
                lesson.icon = GetIconForTopic(topicName);
            
            if (CreateLessonModule(lesson, i))
                createdCount++;
        }
        
        Debug.Log($"✓ Created {createdCount} lesson UI elements for {topicName}");
    }
    
    bool CreateLessonModule(LessonModule lesson, int lessonIndex)
    {
        if (lessonModulePrefab == null || lessonsContainer == null)
            return false;
        
        GameObject lessonObj = Instantiate(lessonModulePrefab, lessonsContainer);
        lessonObj.SetActive(true);
        
        TextMeshProUGUI topicText = FindChildComponent<TextMeshProUGUI>(lessonObj.transform,
            new string[] { "TopicText", "Topic", "LessonTopic", "Text_Topic" });
        if (topicText != null)
            topicText.text = currentTopicName;
        
        TextMeshProUGUI titleText = FindChildComponent<TextMeshProUGUI>(lessonObj.transform,
            new string[] { "TitleText", "Title", "LessonTitle", "Text_Title" });
        if (titleText != null)
            titleText.text = lesson.title;
        
        TextMeshProUGUI descText = FindChildComponent<TextMeshProUGUI>(lessonObj.transform,
            new string[] { "DescriptionText", "Description", "LessonDescription", "Text_Description", "Desc" });
        if (descText != null)
            descText.text = lesson.description;
        
        Image iconImage = FindChildComponent<Image>(lessonObj.transform,
            new string[] { "Icon", "LessonIcon", "Image_Icon" });
        if (iconImage != null)
        {
            if (lesson.isLocked && lockedIcon != null)
                iconImage.sprite = lockedIcon;
            else if (lesson.icon != null)
                iconImage.sprite = lesson.icon;
        }
        
        GameObject checkmark = FindChild(lessonObj.transform,
            new string[] { "Checkmark", "CompletedCheckmark", "CheckIcon", "Icon_Complete" });
        if (checkmark != null)
            checkmark.SetActive(lesson.isCompleted);
        
        Button lessonButton = lessonObj.GetComponent<Button>();
        if (lessonButton == null)
            lessonButton = lessonObj.GetComponentInChildren<Button>();
        
        CanvasGroup group = lessonObj.GetComponent<CanvasGroup>();
        if (group == null) group = lessonObj.AddComponent<CanvasGroup>();

        if (lesson.isLocked)
        {
            group.alpha = 0.5f;
            if (lessonButton != null) lessonButton.interactable = false;
            if (titleText != null) titleText.text = "🔒 " + lesson.title;
        }
        else
        {
            group.alpha = 1.0f;
            if (lessonButton != null)
            {
                lessonButton.interactable = true;
                int capturedIndex = lessonIndex;
                lessonButton.onClick.AddListener(() => OnLessonClicked(capturedIndex));
            }
        }
        
        return true;
    }
    
    T FindChildComponent<T>(Transform parent, string[] possibleNames) where T : Component
    {
        foreach (string name in possibleNames)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                T component = child.GetComponent<T>();
                if (component != null)
                    return component;
            }
        }
        return parent.GetComponentInChildren<T>(true);
    }
    
    GameObject FindChild(Transform parent, string[] possibleNames)
    {
        foreach (string name in possibleNames)
        {
            Transform child = parent.Find(name);
            if (child != null)
                return child.gameObject;
        }
        return null;
    }
    
    void ClearLessons()
    {
        if (lessonsContainer == null) return;
        foreach (Transform child in lessonsContainer)
            Destroy(child.gameObject);
    }
    
    void OnLessonClicked(int lessonIndex)
    {
        if (lessonContentPanel == null || currentTopicLessons == null)
            return;
        
        if (lessonsScrollView != null)
            lessonsScrollView.SetActive(false);
        
        lessonContentPanel.SetActive(true);
        ShowLessonAtIndex(lessonIndex);
    }
    
    IEnumerator ResetScrollPosition()
    {
        yield return new WaitForEndOfFrame();
        
        if (lessonContentScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            lessonContentScrollRect.verticalNormalizedPosition = 1f;
            lessonContentScrollRect.velocity = Vector2.zero;
        }
    }
    
    List<LessonModule> GetLessonsForTopic(string topicName)
    {
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        
        if (cachedLessons.ContainsKey(normalizedTopic))
            return cachedLessons[normalizedTopic];
        
        List<LessonModule> lessons = new List<LessonModule>();
        lessons.Add(new LessonModule
        {
            title = "Coming Soon",
            description = "Lessons for this topic are being prepared",
            isCompleted = false,
            content = "This topic is currently under development.",
            icon = GetIconForTopic(normalizedTopic)
        });
        return lessons;
    }
    
    Sprite GetIconForTopic(string topicName)
    {
        string normalized = TopicNameConstants.Normalize(topicName);
        switch (normalized)
        {
            case TopicNameConstants.ARRAYS:       return arraysIcon      != null ? arraysIcon      : defaultIcon;
            case TopicNameConstants.QUEUE:        return queueIcon       != null ? queueIcon       : defaultIcon;
            case TopicNameConstants.STACKS:       return stacksIcon      != null ? stacksIcon      : defaultIcon;
            case TopicNameConstants.LINKED_LISTS: return linkedListsIcon != null ? linkedListsIcon : defaultIcon;
            case TopicNameConstants.TREES:        return treesIcon       != null ? treesIcon       : defaultIcon;
            case TopicNameConstants.GRAPHS:       return graphsIcon      != null ? graphsIcon      : defaultIcon;
            default:                              return defaultIcon;
        }
    }
    
    string GetLessonContent(string lessonTitle)
    {
        return $"<size=24><b>{lessonTitle}</b></size>\n\nThis lesson content is currently being developed.";
    }

    void SyncLessonCompletionFlags()
    {
        string currentUser = PlayerPrefs.GetString("CurrentUser", "");
        if (string.IsNullOrEmpty(currentUser)) return;
        
        foreach (var kvp in completedLessonsCount)
        {
            string topicName = kvp.Key;
            int completedCount = kvp.Value;
            
            List<LessonModule> lessons = GetLessonsForTopic(topicName);
            int totalLessons = lessons.Count;
            if (totalLessons == 0) continue;
            
            bool allComplete = completedCount >= totalLessons;
            string normalizedTopic = TopicNameConstants.Normalize(topicName);
            string flagKey = $"TopicReadComplete_{currentUser}_{normalizedTopic}";
            
            if (allComplete)
                PlayerPrefs.SetInt(flagKey, 1);
        }
        
        PlayerPrefs.Save();
        
        UpdatedLearnPanelController learnPanel = FindObjectOfType<UpdatedLearnPanelController>();
        if (learnPanel != null)
            learnPanel.UpdatePuzzleButtonsAccess();
    }

    public int GetCompletedLessonCountForTopic(string topicName)
    {
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        if (completedLessonsCount.ContainsKey(normalizedTopic))
            return completedLessonsCount[normalizedTopic];
        return 0;
    }

public string GetCurrentTopicName()
{
    return currentTopicName ?? "";
}
    public void SetBackButtonListener(UnityEngine.Events.UnityAction action)
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(action);
            Debug.Log("[TopicDetailPanel] Back button listener set by bridge");
        }
    }

    void OnContinueToNextLesson()
    {
        if (lessonCompletionDialog != null)
            lessonCompletionDialog.SetActive(false);
        
        StartCoroutine(CompleteCurrentLessonAndAdvance());
    }

    void OnStayOnLesson()
    {
        if (lessonCompletionDialog != null)
            lessonCompletionDialog.SetActive(false);
        
        Debug.Log("👤 User chose to stay on current lesson");
    }

    IEnumerator CompleteCurrentLessonAndAdvance()
    {
        if (!currentLesson.isCompleted)
        {
            currentLesson.isCompleted = true;
            
            string normalizedTopic = TopicNameConstants.Normalize(currentTopicName);
            
            if (!completedLessonsCount.ContainsKey(normalizedTopic))
                completedLessonsCount[normalizedTopic] = 0;
            
            if (currentLessonIndex == completedLessonsCount[normalizedTopic])
            {
                completedLessonsCount[normalizedTopic]++;
                
                string currentUser = PlayerPrefs.GetString("CurrentUser", "");
                PlayerPrefs.SetInt($"{currentUser}_{normalizedTopic}_LessonsCompleted", completedLessonsCount[normalizedTopic]);
                PlayerPrefs.Save();
                
                Debug.Log($"✅ Saved to PlayerPrefs: {normalizedTopic} = {completedLessonsCount[normalizedTopic]} lessons");
            }
            
            Debug.Log("⏳ Waiting for database sync...");
            yield return StartCoroutine(SyncLessonCompletionToDatabase(currentTopicName, completedLessonsCount[normalizedTopic]));
            
            Debug.Log("⏳ Waiting 0.5s for database to process...");
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log("✅ Lesson completion fully synced!");
        }

        if (currentLessonIndex < currentTopicLessons.Count - 1)
        {
            Debug.Log("➡️ Auto-advancing to next lesson...");
            ShowLessonAtIndex(currentLessonIndex + 1);
        }
        else
        {
            Debug.Log("🎉 All lessons in topic completed!");
            HandleTopicCompletion();
        }
    }

    void HandleTopicCompletion()
    {
        if (nextPageButtonText != null)
            nextPageButtonText.text = "Completed!";
            
        if (nextPageButton != null)
            nextPageButton.interactable = false;

        string normalizedTopic = TopicNameConstants.Normalize(currentTopicName);
        string flagKey = $"TopicReadComplete_{currentUsername}_{normalizedTopic}";
        PlayerPrefs.SetInt(flagKey, 1);
        PlayerPrefs.Save();
        
        NotifyTopicCompletion(currentTopicName);
        
        if (UserProgressManager.Instance != null)
        {
            int lessonsCompleted = completedLessonsCount.ContainsKey(normalizedTopic) ? completedLessonsCount[normalizedTopic] : 0;
            UserProgressManager.Instance.UpdateLessonCount(normalizedTopic, lessonsCompleted);
            UserProgressManager.Instance.ManualSync();
        }

        StartCoroutine(ReturnToListAfterDelay());
    }

    IEnumerator ReturnToListAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (lessonContentPanel != null)
            lessonContentPanel.SetActive(false);
        if (lessonsScrollView != null)
            lessonsScrollView.SetActive(true);
        if (titleText != null)
            titleText.text = currentTopicName;
        if (toggleVisualizerButton != null)
            toggleVisualizerButton.gameObject.SetActive(false);

        ShowLoadingIndicator(true);
        yield return StartCoroutine(FetchUserProgressFromDatabase());
        ShowLoadingIndicator(false);

        LoadLessons(currentTopicName);
    }

    public void LoadLessonsPublic(string topicName)
    {
        LoadLessons(topicName);
    }

    void OnDisable()
    {
        if (currentLesson != null && UserProgressManager.Instance != null)
        {
            float lessonTime = Time.time - lessonStartTime;
            totalLessonTime += lessonTime;
            UserProgressManager.Instance.AddTimeSpent(currentTopicName, lessonTime);
            Debug.Log($"⏱️ Saved time on disable: {totalLessonTime:F1}s");
        }
    }

    void FixUIResponsiveness()
    {
        Debug.Log("📱 FixUIResponsiveness: Configuring CanvasScalers");
        CanvasScaler[] scalers = FindObjectsOfType<CanvasScaler>();
        foreach (var scaler in scalers)
        {
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
                Debug.Log($"✅ Updated CanvasScaler on {scaler.gameObject.name} to ScaleWithScreenSize");
            }
        }
    }

    void OnBackButtonClicked()
    {
        if (lessonContentPanel != null)
            lessonContentPanel.SetActive(false);

        if (lessonsScrollView != null)
            lessonsScrollView.SetActive(true);

        if (titleText != null)
            titleText.text = currentTopicName;

        if (toggleVisualizerButton != null)
            toggleVisualizerButton.gameObject.SetActive(false);

        if (visualizer != null)
        {
            visualizer.ClearVisualization();
            visualizer.HideVisualization();
        }

        isVisualizerActive = false;

        LoadLessons(currentTopicName);
    }
}