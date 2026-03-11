using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Text;
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    [Header("Screens")]
    public GameObject homeScreen;
    public GameObject topicSelectionScreen;
    public GameObject queueModesScreen;
    public GameObject arPanel;
   [Header("Coming Soon Visual Settings")]
   
    [Tooltip("Gray color for 'coming soon' topic text and icons")]
    public Color comingSoonColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    
    [Header("Home Screen Elements")]
    public TextMeshProUGUI welcomeText;
    public TextMeshProUGUI streakText;
    public TextMeshProUGUI completedTopicsText;
    public TextMeshProUGUI overallProgressText;
    public Slider overallProgressBar;
    public Image profilePictureImage;
    public Button profilePictureButton;
    
    [Header("Continue Learning Card")]
    public GameObject continueLearningCard;
    public Image continueCardIcon;
    public TextMeshProUGUI continueCardTitle;
    public TextMeshProUGUI continueCardSubtitle;
    public TextMeshProUGUI continueCardProgress;
    public Slider continueCardProgressSlider;
    public Button continueLearningButton;

    [Header("Topic Detail Panel")]
    public TopicDetailPanel topicDetailPanel;

    [Header("Topic Cards - Existing")]
    public GameObject arraysCard;
    public GameObject stacksCard;
    public GameObject queuesCard;
    public GameObject linkedListsCard;
    public GameObject treesCard;
    public GameObject graphsCard;
    
    [Header("Topic Cards - NEW TOPICS")]
    public GameObject hashmapsCard;
    public GameObject heapsCard;
    public GameObject dequeCard;
    public GameObject binaryHeapsCard;
    
    // Topic Card Components for Arrays
    public TextMeshProUGUI arraysProgressText;
    public TextMeshProUGUI arraysLessonsText;
    public Slider arraysProgressBar;

    // Topic Card Components for Stacks
    public TextMeshProUGUI stacksProgressText;
    public TextMeshProUGUI stacksLessonsText;
    public Slider stacksProgressBar;
    
    // Topic Card Components for Queues
    public TextMeshProUGUI queuesProgressText;
    public TextMeshProUGUI queuesLessonsText;
    public Slider queuesProgressBar;
    
    // Topic Card Components for Linked Lists
    public TextMeshProUGUI linkedListsProgressText;
    public TextMeshProUGUI linkedListsLessonsText;
    public Slider linkedListsProgressBar;
    
    // Topic Card Components for Trees
    public TextMeshProUGUI treesProgressText;
    public TextMeshProUGUI treesLessonsText;
    public Slider treesProgressBar;
    
    // Topic Card Components for Graphs
    public TextMeshProUGUI graphsProgressText;
    public TextMeshProUGUI graphsLessonsText;
    public Slider graphsProgressBar;
    
    // Topic Card Components for Hashmaps
    public TextMeshProUGUI hashmapsProgressText;
    public TextMeshProUGUI hashmapsLessonsText;
    public Slider hashmapsProgressBar;
    
    // Topic Card Components for Heaps
    public TextMeshProUGUI heapsProgressText;
    public TextMeshProUGUI heapsLessonsText;
    public Slider heapsProgressBar;
    
    // Topic Card Components for Deque
    public TextMeshProUGUI dequeProgressText;
    public TextMeshProUGUI dequeLessonsText;
    public Slider dequeProgressBar;
    
    // Topic Card Components for Binary Heaps
    public TextMeshProUGUI binaryHeapsProgressText;
    public TextMeshProUGUI binaryHeapsLessonsText;
    public Slider binaryHeapsProgressBar;
    
    [Header("Navigation Bar")]
    public Button homeButton;
    public Button learnButton;
    public Button progressButton;
    public Button profileButton;
    
    [Header("Quick Access Cards")]
    public Button continueQueueButton;
    public Button startNewTopicButton;
    public Button viewProgressButton;
    
    [Header("Topic Cards - Buttons (Existing)")]
    public Button arraysTopicButton;
    public Button queueTopicButton;
    public Button stackTopicButton;
    public Button linkedListTopicButton;
    public Button treeTopicButton;
    public Button graphTopicButton;
    
    [Header("Topic Cards - Buttons (NEW)")]
    public Button hashmapsTopicButton;
    public Button heapsTopicButton;
    public Button dequeTopicButton;
    public Button binaryHeapsTopicButton;
    
    [Header("Queue Mode Buttons")]
    public Button tutorialModeButton;
    public Button guidedModeButton;
    public Button challengeModeButton;
    public Button backToTopicsButton;
    
    [Header("Profile Picture Assets")]
    public Sprite[] profilePictureSprites;
    
    [Header("Topic Icons - Existing")]
    public Sprite arraysIcon;
    public Sprite queueIcon;
    public Sprite stackIcon;
    public Sprite linkedListIcon;
    public Sprite treeIcon;
    public Sprite graphIcon;
    
    [Header("Topic Icons - NEW")]
    public Sprite hashmapsIcon;
    public Sprite heapsIcon;
    public Sprite dequeIcon;
    public Sprite binaryHeapsIcon;
    
    [Header("Profile Screen")]
    public GameObject profileEditScreen;
    public Image profileEditImage;
    public Button[] profileEditOptions;
    public Button saveProfileButton;
    public Button cancelProfileButton;
    public Button logoutButton;
    public TextMeshProUGUI profileNameText;
    public TextMeshProUGUI profileUsernameText;
    
    [Header("Settings")]
    public string queueSceneName = "QueueScene";
    public string loginSceneName = "LoginRegister";
    [Tooltip("Leave empty to disable cloud sync")]
    public string adminApiUrl = "https://structureality-admin.onrender.com/api";
    
    [Header("Loading")]
    public GameObject loadingIndicator;
    
    // User data
    private string currentUsername;
    private string studentName;
    private int currentProfilePictureIndex;
    private int currentStreak;
    private int completedTopics;
    private int tempSelectedProfilePic;
    private bool isDataLoaded = false;
    private DatabaseProgressData cachedProgressData;
    
    // ✅ Cache lesson counts from database
    private Dictionary<string, int> topicLessonCounts = new Dictionary<string, int>();
    
   void Start()
{
    FixUIResponsiveness();
    LoadUserData();
    InitializeUI();
    
    ForceEnableTopicCardsParent();
    ForceEnableTopicCards();

    // Check if we should open AR Panel instead of Home
    if (SceneLoader.targetPanel == "AR")
    {
        SceneLoader.targetPanel = "";
        ShowARPanel();
    }
    else
    {
        ShowHomeScreen();
    }
    
    StartCoroutine(WaitForDataSyncAndUpdate());
    
    if (!string.IsNullOrEmpty(adminApiUrl))
        StartCoroutine(UpdateUserActivity());
}

    void OnEnable()
    {
        Debug.Log("🔄 MainMenuManager OnEnable - Refreshing data");
        
        if (isDataLoaded)
        {
            StartCoroutine(RefreshDataFromDatabase());
        }
    }

    IEnumerator RefreshDataFromDatabase()
    {
        Debug.Log("🔄 Refreshing data from database...");
        
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
        
        yield return StartCoroutine(FetchProgressFromDatabase());
        yield return StartCoroutine(FetchLessonCounts());
        
        // ✅ CRITICAL: Recalculate progress percentages after fetching data
        RecalculateAllTopicProgress();
        
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
        
        UpdateHomeScreenData();
        UpdateContinueLearningCard();
        UpdateTopicCards();
        
        Debug.Log("✅ Data refresh complete");
    }
    
    IEnumerator WaitForDataSyncAndUpdate()
    {
        Debug.Log("⏳ MainMenu: Waiting for UserProgressManager to sync data...");
        
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
        
        while (UserProgressManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("✓ UserProgressManager found");
        
        yield return StartCoroutine(FetchProgressFromDatabase());
        yield return StartCoroutine(FetchLessonCounts());
        
        // ✅ CRITICAL: Recalculate progress after data loads
        RecalculateAllTopicProgress();
        
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
        
        isDataLoaded = true;
        UpdateHomeScreenData();
        UpdateContinueLearningCard();
        UpdateTopicCards();
    }

    // ✅ NEW: Recalculate progress percentages for all topics
void RecalculateAllTopicProgress()
{
    if (cachedProgressData == null || cachedProgressData.topics == null)
    {
        Debug.LogWarning("⚠️ Cannot recalculate - no cached data");
        return;
    }
    
    Debug.Log("=== Using Database Progress (NO LOCAL RECALCULATION) ===");
    
    foreach (var topic in cachedProgressData.topics)
    {
        // ✅ CRITICAL FIX: Use the progressPercentage FROM DATABASE
        // DO NOT recalculate locally - trust the server calculation
        
        Debug.Log($"📊 {topic.topicName}: {topic.progressPercentage:F1}% (from database)");
        Debug.Log($"   - Lessons: {topic.lessonsCompleted}");
        Debug.Log($"   - Puzzle Scores: Easy={topic.difficultyScores?.easy ?? 0}, " +
                  $"Medium={topic.difficultyScores?.medium ?? 0}, " +
                  $"Hard={topic.difficultyScores?.hard ?? 0}, " +
                  $"Mixed={topic.difficultyScores?.mixed ?? 0}");
    }
    
    Debug.Log("✅ Using database progress values (no recalculation needed)");
}

    IEnumerator FetchLessonCounts()
    {
        if (string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.LogWarning("⚠️ API URL not set, cannot fetch lesson counts");
            yield break;
        }
        
        string url = $"{adminApiUrl}/lessons";
        Debug.Log($"🔄 Fetching lesson counts from: {url}");
        
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 90;
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Lessons fetched successfully");
            
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
                        {
                            topicLessonCounts[normalizedTopic] = 0;
                        }
                        topicLessonCounts[normalizedTopic]++;
                    }
                    
                    Debug.Log($"✅ Lesson counts loaded:");
                    foreach (var kvp in topicLessonCounts)
                    {
                        Debug.Log($"  📚 {kvp.Key}: {kvp.Value} lessons");
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
            Debug.LogError($"❌ Failed to fetch lessons: {request.error}");
        }
    }
    
    IEnumerator FetchProgressFromDatabase()
    {
        string username = PlayerPrefs.GetString("User_" + currentUsername + "_Username", "");
        
        if (string.IsNullOrEmpty(username))
        {
            username = currentUsername.Split('@')[0];
        }
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.LogWarning("⚠️ Cannot fetch: No username or API URL");
            yield break;
        }
        
        string url = $"{adminApiUrl}/progress/{username}";
        Debug.Log($"🔄 Fetching progress from: {url}");
        
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 90;
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Progress fetched successfully");
            
            try
            {
                DatabaseProgressResponse response = JsonUtility.FromJson<DatabaseProgressResponse>(request.downloadHandler.text);
                
                if (response != null && response.success && response.data != null)
                {
                    cachedProgressData = response.data;
                    Debug.Log($"✅ Loaded progress data for: {cachedProgressData.username}");
                    Debug.Log($"📊 Topics count: {cachedProgressData.topics.Count}");
                    
                    foreach (var topic in cachedProgressData.topics)
                    {
                        Debug.Log($"  📚 {topic.topicName}: {topic.lessonsCompleted} lessons, {topic.progressPercentage}% progress (from DB)");
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
    
    void LoadUserData()
    {
        currentUsername = PlayerPrefs.GetString("CurrentUser", "");
        
        if (string.IsNullOrEmpty(currentUsername))
        {
            Debug.Log("No user logged in - redirecting to login");
            SceneManager.LoadScene(loginSceneName);
            return;
        }
        
        studentName = PlayerPrefs.GetString("User_" + currentUsername + "_Name", "Student");
        currentProfilePictureIndex = PlayerPrefs.GetInt("User_" + currentUsername + "_ProfilePic", 0);
        
        if (UserProgressManager.Instance != null)
        {
            currentStreak = UserProgressManager.Instance.GetStreak();
        }
        else
        {
            currentStreak = PlayerPrefs.GetInt("User_" + currentUsername + "_Streak", 0);
        }
        
        completedTopics = PlayerPrefs.GetInt("User_" + currentUsername + "_CompletedTopics", 0);
        
        Debug.Log($"Loaded user: {studentName} (Username: {currentUsername})");
    }
    
void InitializeUI()
{
    // Navigation bar
    if (homeButton != null) homeButton.onClick.AddListener(ShowHomeScreen);
    if (learnButton != null) learnButton.onClick.AddListener(ShowTopicSelection);
    if (progressButton != null) progressButton.onClick.AddListener(ShowProgress);
    if (profileButton != null) profileButton.onClick.AddListener(ShowProfile);
    
    // Home screen quick access
    if (continueQueueButton != null) continueQueueButton.onClick.AddListener(ContinueQueue);
    if (startNewTopicButton != null) startNewTopicButton.onClick.AddListener(ShowTopicSelection);
    if (viewProgressButton != null) viewProgressButton.onClick.AddListener(ShowProgress);
    
    // Continue Learning Card
    if (continueLearningButton != null) continueLearningButton.onClick.AddListener(ContinueLastTopic);
    
    // Profile picture button
    if (profilePictureButton != null) 
        profilePictureButton.onClick.AddListener(ShowProfileEdit);
    
    // ✅ Make ALL topic cards non-interactable (Existing Topics)
    if (arraysTopicButton != null)
    {
        arraysTopicButton.onClick.RemoveAllListeners();
        arraysTopicButton.interactable = false;
    }
    if (stackTopicButton != null) 
    {
        stackTopicButton.onClick.RemoveAllListeners();
        stackTopicButton.interactable = false;
    }
    if (queueTopicButton != null) 
    {
        queueTopicButton.onClick.RemoveAllListeners();
        queueTopicButton.interactable = false;
    }
    if (linkedListTopicButton != null) 
    {
        linkedListTopicButton.onClick.RemoveAllListeners();
        linkedListTopicButton.interactable = false;
    }
    if (treeTopicButton != null) 
    {
        treeTopicButton.onClick.RemoveAllListeners();
        treeTopicButton.interactable = false;
    }
    if (graphTopicButton != null) 
    {
        graphTopicButton.onClick.RemoveAllListeners();
        graphTopicButton.interactable = false;
    }
    
    // ✅ Make ALL topic cards non-interactable (New Topics)
    if (hashmapsTopicButton != null)
    {
        hashmapsTopicButton.onClick.RemoveAllListeners();
        hashmapsTopicButton.interactable = false;
    }
    if (heapsTopicButton != null)
    {
        heapsTopicButton.onClick.RemoveAllListeners();
        heapsTopicButton.interactable = false;
    }
    if (dequeTopicButton != null)
    {
        dequeTopicButton.onClick.RemoveAllListeners();
        dequeTopicButton.interactable = false;
    }
    if (binaryHeapsTopicButton != null)
    {
        binaryHeapsTopicButton.onClick.RemoveAllListeners();
        binaryHeapsTopicButton.interactable = false;
    }
    
    // ✅ Apply styling only to new topics
    ApplyComingSoonStyling();
    
    // Queue modes
    if (tutorialModeButton != null) tutorialModeButton.onClick.AddListener(StartTutorialMode);
    if (guidedModeButton != null) guidedModeButton.onClick.AddListener(StartGuidedMode);
    if (challengeModeButton != null) challengeModeButton.onClick.AddListener(StartChallengeMode);
    if (backToTopicsButton != null) backToTopicsButton.onClick.AddListener(ShowTopicSelection);
    
    // Profile edit buttons
    if (saveProfileButton != null) saveProfileButton.onClick.AddListener(SaveProfileChanges);
    if (cancelProfileButton != null) cancelProfileButton.onClick.AddListener(CancelProfileEdit);
    if (logoutButton != null) logoutButton.onClick.AddListener(Logout);
    
    // Profile picture selection in edit screen
    for (int i = 0; i < profileEditOptions.Length; i++)
    {
        int index = i;
        if (profileEditOptions[i] != null)
        {
            profileEditOptions[i].onClick.AddListener(() => SelectTempProfilePicture(index));
        }
    }
    
    UpdateHomeScreenData();
    UpdateProfilePicture();
}

void UpdateQuizDifficultyStats()
{
    if (cachedProgressData == null || cachedProgressData.topics == null)
        return;
    
    int easyCompleted = 0;
    int mediumCompleted = 0;
    int hardCompleted = 0;
    int mixedCompleted = 0;
    
    foreach (var topic in cachedProgressData.topics)
    {
        if (topic.difficultyScores != null)
        {
            if (topic.difficultyScores.easy >= 70) easyCompleted++;
            if (topic.difficultyScores.medium >= 70) mediumCompleted++;
            if (topic.difficultyScores.hard >= 70) hardCompleted++;
            if (topic.difficultyScores.mixed >= 70) mixedCompleted++;
        }
    }
    
    Debug.Log($"📊 Difficulty Completion:");
    Debug.Log($"   Easy: {easyCompleted}/10");
    Debug.Log($"   Medium: {mediumCompleted}/10");
    Debug.Log($"   Hard: {hardCompleted}/10");
    Debug.Log($"   Mixed: {mixedCompleted}/10");
}
/// <summary>
/// Apply gray styling ONLY to new "coming soon" topics
/// </summary>
void ApplyComingSoonStyling()
{
    Debug.Log("🎨 Applying 'Coming Soon' styling to new topics...");
    
    // ✅ Only gray out NEW topics (keep card backgrounds normal)
    ApplyGrayToCard(hashmapsCard);
    ApplyGrayToCard(heapsCard);
    ApplyGrayToCard(dequeCard);
    ApplyGrayToCard(binaryHeapsCard);
    
    Debug.Log("✅ 'Coming Soon' styling applied");
    Debug.Log("   → Hashmaps, Heaps, Deque, Binary Heaps: Grayed out");
    Debug.Log("   → Arrays, Stacks, Queues, Linked Lists, Trees, Graphs: Normal colors");
}

/// <summary>
/// Apply GRAY color to show card is "coming soon"
/// </summary>
void ApplyGrayToCard(GameObject card)
{
    if (card == null)
    {
        Debug.LogWarning("⚠️ Card is null, skipping gray styling");
        return;
    }
    
    Debug.Log($"🎨 Applying GRAY to: {card.name}");
    
    // Gray out all Image components (except fill bars)
    Image[] images = card.GetComponentsInChildren<Image>(true);
    foreach (var img in images)
    {
        // Skip fill images to keep progress bars visible
        if (img.name.Contains("Fill"))
            continue;
            
        img.color = comingSoonColor;
    }
    
    // Gray out all TextMeshProUGUI components
    TextMeshProUGUI[] texts = card.GetComponentsInChildren<TextMeshProUGUI>(true);
    foreach (var text in texts)
    {
        text.color = comingSoonColor;
    }
    
    // Gray out the button's image component
    Button btn = card.GetComponentInChildren<Button>();
    if (btn != null)
    {
        Image btnImage = btn.GetComponent<Image>();
        if (btnImage != null && !btnImage.name.Contains("Fill"))
        {
            btnImage.color = comingSoonColor;
        }
    }
    
    Debug.Log($"✓ Applied GRAY styling to {card.name}");
}

    void UpdateHomeScreenData()
    {
        if (welcomeText != null)
            welcomeText.text = $"Welcome, {studentName}!";
        
        if (streakText != null)
            streakText.text = $"{currentStreak} Days";
        
        if (cachedProgressData != null)
        {
            completedTopics = 0;
            float totalProgress = 0f;
            
            foreach (var topic in cachedProgressData.topics)
            {
                // Use the already-calculated progressPercentage
                totalProgress += topic.progressPercentage;
                
                if (topic.progressPercentage >= 100f)
                {
                    completedTopics++;
                }
            }
            
            float overallProgress = cachedProgressData.topics.Count > 0 ? totalProgress / cachedProgressData.topics.Count : 0f;
            
            if (completedTopicsText != null)
                completedTopicsText.text = $"{completedTopics}/10 Topics";
            
            if (overallProgressText != null)
            {
                overallProgressText.text = $"{overallProgress:F0}%";
            }
            
            if (overallProgressBar != null)
            {
                overallProgressBar.value = overallProgress / 100f;
                overallProgressBar.interactable = false;
                
                Image fillImage = overallProgressBar.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.enabled = true;
                    
                    if (fillImage.color.a < 0.1f)
                    {
                        Color fillColor = fillImage.color;
                        fillColor.a = 1f;
                        fillImage.color = fillColor;
                    }
                }
            }
            
            Debug.Log($"✅ Overall Progress Updated: {overallProgress:F0}% ({completedTopics}/10 topics complete)");
        }
        else
        {
            if (overallProgressText != null)
                overallProgressText.text = "0%";
            
            if (completedTopicsText != null)
                completedTopicsText.text = "0/10 Topics";
            
            if (overallProgressBar != null)
            {
                overallProgressBar.value = 0f;
                overallProgressBar.interactable = false;
            }
        }
    }

    int GetTotalLessonCount(string topicName)
    {
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        
        if (topicLessonCounts.ContainsKey(normalizedTopic))
        {
            int count = topicLessonCounts[normalizedTopic];
            Debug.Log($"✓ {topicName}: {count} lessons (from database cache)");
            return count;
        }
        
        if (topicDetailPanel != null)
        {
            int count = topicDetailPanel.GetTotalLessonCount(topicName);
            if (count > 0)
            {
                Debug.Log($"✓ {topicName}: {count} lessons (from TopicDetailPanel)");
                return count;
            }
        }
        
        Debug.LogWarning($"⚠️ {topicName}: No lessons in database");
        return 0;
    }

    public void RefreshProgressFromExternal()
    {
        Debug.Log("🔄 External refresh triggered - reloading data from database");
        StartCoroutine(RefreshDataFromDatabase());
    }

void UpdateContinueLearningCard()
{
    if (continueLearningCard == null) return;
    
    // ✅ NEW: Check if this is a new user with no progress
    if (cachedProgressData == null || cachedProgressData.topics == null || cachedProgressData.topics.Count == 0)
    {
        Debug.Log("🆕 NEW USER - Showing Arrays as starting topic");
        
        // Show Arrays as the starting topic (ALWAYS unlocked)
        if (continueCardTitle != null)
            continueCardTitle.text = "Arrays • Start learning";
        
        if (continueCardSubtitle != null)
            continueCardSubtitle.text = "Begin your journey with data structures";
        
        if (continueCardProgress != null)
            continueCardProgress.text = "0%";
        
        if (continueCardProgressSlider != null)
        {
            continueCardProgressSlider.value = 0f;
            continueCardProgressSlider.interactable = false;
        }
        
        if (continueCardIcon != null)
        {
            Sprite icon = GetIconForTopic(TopicNameConstants.ARRAYS);
            if (icon != null)
                continueCardIcon.sprite = icon;
        }
        
        PlayerPrefs.SetString("User_" + currentUsername + "_LastTopic", TopicNameConstants.ARRAYS);
        PlayerPrefs.Save();
        
        continueLearningCard.SetActive(true);
        return;
    }
    
    // ✅ FIX: Find the FIRST UNLOCKED INCOMPLETE topic in learning sequence
    string[] topicOrder = new string[]
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
    
    DatabaseTopicData nextTopic = null;
    
    // ✅ CRITICAL FIX: Find FIRST UNLOCKED topic that's incomplete
    foreach (string topic in topicOrder)
    {
        // Check if topic is unlocked
        bool isUnlocked = IsTopicUnlockedForContinue(topic);
        
        if (!isUnlocked)
        {
            Debug.Log($"🔒 {topic} is locked - skipping");
            continue; // Skip locked topics
        }
        
        var topicData = cachedProgressData.topics.Find(t => 
            TopicNameConstants.Normalize(t.topicName) == topic);
        
        // If topic has no data yet, it's the next one to start
        if (topicData == null)
        {
            // Create placeholder data for unlocked topic with no progress
            nextTopic = new DatabaseTopicData
            {
                topicName = topic,
                progressPercentage = 0f,
                lessonsCompleted = 0,
                lastAccessed = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            Debug.Log($"📚 Next topic (no data): {topic} (unlocked)");
            break;
        }
        
        // If topic is incomplete, this is the one to continue
        if (topicData.progressPercentage < 100f)
        {
            nextTopic = topicData;
            Debug.Log($"📚 Next topic to continue: {topic} ({topicData.progressPercentage:F0}%)");
            break;
        }
        
        Debug.Log($"✅ {topic} is complete ({topicData.progressPercentage:F0}%), checking next...");
    }
    
    // ✅ If all unlocked topics are complete, show most recently accessed
    if (nextTopic == null)
    {
        System.DateTime latestAccess = System.DateTime.MinValue;
        
        foreach (var topic in cachedProgressData.topics)
        {
            System.DateTime accessTime;
            if (System.DateTime.TryParse(topic.lastAccessed, out accessTime))
            {
                if (accessTime > latestAccess)
                {
                    latestAccess = accessTime;
                    nextTopic = topic;
                }
            }
        }
        
        if (nextTopic != null)
        {
            Debug.Log($"🎉 All unlocked topics complete! Showing most recent: {nextTopic.topicName}");
        }
    }
    
    if (nextTopic == null)
    {
        continueLearningCard.SetActive(false);
        Debug.LogWarning("⚠️ No topics found to display in continue learning card");
        return;
    }
    
    continueLearningCard.SetActive(true);
    
    string topicName = nextTopic.topicName;
    
    if (continueCardTitle != null)
    {
        string action = nextTopic.progressPercentage >= 100f 
            ? "Review this completed topic" 
            : nextTopic.lessonsCompleted > 0 
                ? "Continue learning" 
                : "Start learning";
        
        continueCardTitle.text = $"{topicName} • {action}";
    }
    
    if (continueCardSubtitle != null)
    {
        if (nextTopic.progressPercentage >= 100f)
        {
            continueCardSubtitle.text = "✓ Completed - Review anytime";
        }
        else if (nextTopic.lessonsCompleted > 0)
        {
            continueCardSubtitle.text = $"{nextTopic.lessonsCompleted} lessons completed";
        }
        else
        {
            continueCardSubtitle.text = "Ready to start";
        }
    }
    
    if (continueCardProgress != null)
    {
        continueCardProgress.text = $"{nextTopic.progressPercentage:F0}%";
    }
    
    if (continueCardProgressSlider != null)
    {
        continueCardProgressSlider.value = nextTopic.progressPercentage / 100f;
        continueCardProgressSlider.interactable = false;
        
        Image fillImage = continueCardProgressSlider.fillRect?.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.enabled = true;
            if (fillImage.color.a < 0.1f)
            {
                Color fillColor = fillImage.color;
                fillColor.a = 1f;
                fillImage.color = fillColor;
            }
        }
    }
    
    if (continueCardIcon != null)
    {
        Sprite icon = GetIconForTopic(topicName);
        if (icon != null)
        {
            continueCardIcon.sprite = icon;
        }
    }
    
    PlayerPrefs.SetString("User_" + currentUsername + "_LastTopic", topicName);
    PlayerPrefs.Save();
    
    Debug.Log($"✅ Continue Learning Card updated: {topicName} ({nextTopic.progressPercentage:F0}%)");
}

bool IsTopicUnlockedForContinue(string topicName)
{
    string normalized = TopicNameConstants.Normalize(topicName);
    
    // Arrays is ALWAYS unlocked
    if (normalized == TopicNameConstants.ARRAYS)
        return true;
    
    // If no data loaded yet, only Arrays is unlocked
    if (cachedProgressData == null || cachedProgressData.topics == null)
        return false;
    
    // Check previous topic completion using the SAME logic as LearnPanel
    string previousTopic = GetPreviousTopic(normalized);
    
    if (string.IsNullOrEmpty(previousTopic))
        return false;
    
    return IsTopicReadingCompleteForContinue(previousTopic);
}

// ✅ NEW: Get previous topic in sequence
string GetPreviousTopic(string topicName)
{
    string normalized = TopicNameConstants.Normalize(topicName);
    
    switch (normalized)
    {
        case TopicNameConstants.STACKS: return TopicNameConstants.ARRAYS;
        case TopicNameConstants.QUEUE: return TopicNameConstants.STACKS;
        case TopicNameConstants.LINKED_LISTS: return TopicNameConstants.QUEUE;
        case TopicNameConstants.TREES: return TopicNameConstants.LINKED_LISTS;
        case TopicNameConstants.GRAPHS: return TopicNameConstants.TREES;
        case TopicNameConstants.HASHMAPS: return TopicNameConstants.GRAPHS;
        case TopicNameConstants.HEAPS: return TopicNameConstants.HASHMAPS;
        case TopicNameConstants.DEQUE: return TopicNameConstants.HEAPS;
        case TopicNameConstants.BINARY_HEAPS: return TopicNameConstants.DEQUE;
        default: return "";
    }
}

// ✅ NEW: Check if topic lessons are complete (for unlock logic)
bool IsTopicReadingCompleteForContinue(string topicName)
{
    string normalizedTopic = TopicNameConstants.Normalize(topicName);
    
    if (cachedProgressData == null || cachedProgressData.topics == null)
        return false;
    
    var topicData = cachedProgressData.topics.Find(t => 
        TopicNameConstants.Normalize(t.topicName) == normalizedTopic);
    
    if (topicData == null)
        return false;
    
    // Check if lessons are complete
    int totalLessons = GetTotalLessonCount(topicName);
    
    if (totalLessons == 0)
        return false;
    
    bool complete = topicData.lessonsCompleted >= totalLessons;
    
    Debug.Log($"🔍 {topicName} lessons: {topicData.lessonsCompleted}/{totalLessons} = {complete}");
    
    return complete;
}
    
    string GetContinueSubtitle(DatabaseTopicData topicData)
    {
        if (topicData.progressPercentage >= 100f)
            return "Complete! Review or take the challenge";
        
        if (topicData.lessonsCompleted > 0)
            return $"{topicData.lessonsCompleted} lessons completed - Keep going!";
        
        return "Start learning this topic";
    }
    
    void UpdateTopicCards()
    {
        if (cachedProgressData == null)
        {
            Debug.LogWarning("⚠️ No cached data - skipping topic card update");
            return;
        }
        
        Debug.Log("=== Updating Topic Cards on Home Screen ===");
        
        // Update existing topics
        UpdateSingleTopicCard(TopicNameConstants.ARRAYS, arraysProgressText, arraysLessonsText, arraysProgressBar);
        UpdateSingleTopicCard(TopicNameConstants.STACKS, stacksProgressText, stacksLessonsText, stacksProgressBar);
        UpdateSingleTopicCard(TopicNameConstants.QUEUE, queuesProgressText, queuesLessonsText, queuesProgressBar);
        UpdateSingleTopicCard(TopicNameConstants.LINKED_LISTS, linkedListsProgressText, linkedListsLessonsText, linkedListsProgressBar);
        UpdateSingleTopicCard(TopicNameConstants.TREES, treesProgressText, treesLessonsText, treesProgressBar);
        UpdateSingleTopicCard(TopicNameConstants.GRAPHS, graphsProgressText, graphsLessonsText, graphsProgressBar);
        
        // Update new topics
        UpdateSingleTopicCard(TopicNameConstants.HASHMAPS, hashmapsProgressText, hashmapsLessonsText, hashmapsProgressBar);
        UpdateSingleTopicCard(TopicNameConstants.HEAPS, heapsProgressText, heapsLessonsText, heapsProgressBar);
        UpdateSingleTopicCard(TopicNameConstants.DEQUE, dequeProgressText, dequeLessonsText, dequeProgressBar);
        UpdateSingleTopicCard(TopicNameConstants.BINARY_HEAPS, binaryHeapsProgressText, binaryHeapsLessonsText, binaryHeapsProgressBar);
        
        Debug.Log("=== Topic Cards Update Complete ===");
    }

 void UpdateSingleTopicCard(string topicName, TextMeshProUGUI progressText, TextMeshProUGUI lessonsText, Slider progressBar)
{
    var topicData = cachedProgressData.topics.Find(t => 
        TopicNameConstants.Normalize(t.topicName) == TopicNameConstants.Normalize(topicName));
    
    if (topicData != null)
    {
        // ✅ CRITICAL FIX: Use progressPercentage FROM DATABASE
        float progress = topicData.progressPercentage;
        
        if (progressText != null)
        {
            progressText.text = $"{progress:F0}%";
            Debug.Log($"✓ {topicName}: {progress:F0}% (from database)");
        }
        
        if (lessonsText != null)
        {
            int totalLessons = GetTotalLessonCount(topicName);
            lessonsText.text = $"{totalLessons} lessons";
        }
        
        if (progressBar != null)
        {
            progressBar.value = progress / 100f;
            progressBar.interactable = false;
            
            // ✅ Ensure fill is visible
            Image fillImage = progressBar.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.enabled = true;
                if (fillImage.color.a < 0.1f)
                {
                    Color fillColor = fillImage.color;
                    fillColor.a = 1f;
                    fillImage.color = fillColor;
                }
            }
        }
    }
    else
    {
        Debug.LogWarning($"⚠️ No data for {topicName}");
        
        if (progressText != null)
            progressText.text = "0%";
        
        if (lessonsText != null)
        {
            int totalLessons = GetTotalLessonCount(topicName);
            lessonsText.text = $"{totalLessons} lessons";
        }
        
        if (progressBar != null)
        {
            progressBar.value = 0f;
            progressBar.interactable = false;
        }
    }
}


    
    Sprite GetIconForTopic(string topicName)
    {
        string normalized = TopicNameConstants.Normalize(topicName);
        
        switch (normalized)
        {
            case TopicNameConstants.ARRAYS: return arraysIcon;
            case TopicNameConstants.QUEUE: return queueIcon;
            case TopicNameConstants.STACKS: return stackIcon;
            case TopicNameConstants.LINKED_LISTS: return linkedListIcon;
            case TopicNameConstants.TREES: return treeIcon;
            case TopicNameConstants.GRAPHS: return graphIcon;
            case TopicNameConstants.HASHMAPS: return hashmapsIcon;
            case TopicNameConstants.HEAPS: return heapsIcon;
            case TopicNameConstants.DEQUE: return dequeIcon;
            case TopicNameConstants.BINARY_HEAPS: return binaryHeapsIcon;
            default: return null;
        }
    }
    
    void UpdateProfilePicture()
    {
        if (profilePictureImage != null && profilePictureSprites != null && 
            currentProfilePictureIndex < profilePictureSprites.Length)
        {
            profilePictureImage.sprite = profilePictureSprites[currentProfilePictureIndex];
        }
    }
    
    IEnumerator UpdateUserActivity()
    {
        if (string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.Log("Cloud sync disabled - no admin API URL set");
            yield break;
        }

        string jsonData = JsonUtility.ToJson(new UserActivityData
        {
            username = currentUsername,
            name = studentName,
            lastLogin = System.DateTime.Now.ToString(),
            streak = currentStreak,
            completedTopics = completedTopics
        });
        
        UnityWebRequest request = new UnityWebRequest(adminApiUrl + "/users/" + currentUsername, "PUT");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ User activity updated to cloud");
        }
        else
        {
            Debug.LogWarning($"Failed to update user activity: {request.error}");
        }
    }
    
    public void ShowHomeScreen()
    {
        Debug.Log("=== ShowHomeScreen Called ===");
        
        HideAllScreens();
        
        if (homeScreen != null)
        {
            homeScreen.SetActive(true);
        }
        
        ForceEnableTopicCardsParent();
        ForceEnableTopicCards();
        
        if (isDataLoaded)
        {
            UpdateHomeScreenData();
            UpdateContinueLearningCard();
            UpdateTopicCards();
        }
       else
        {
            Debug.LogWarning("⚠️ Data not loaded yet, refreshing...");
            StartCoroutine(WaitForDataSyncAndUpdate());
        }
        
        Debug.Log("✓ HomeScreen displayed with topic cards");
    }

   void ForceEnableTopicCardsParent()
{
    // Walk up the parent chain from arraysCard and enable any inactive ancestors
    // (stops at the root Canvas to avoid enabling unrelated panels)
    GameObject probe = arraysCard;
    if (probe == null) return;

    Transform t = probe.transform.parent;
    while (t != null)
    {
        // Stop if we hit the Canvas root
        if (t.GetComponent<Canvas>() != null) break;

        if (!t.gameObject.activeSelf)
        {
            Debug.LogWarning($"⚠️ Enabling inactive parent: {t.gameObject.name}");
            t.gameObject.SetActive(true);
        }
        t = t.parent;
    }
}

void ForceEnableTopicCards()
{
    Debug.Log("=== Force Enabling Topic Cards ===");
    
    // Existing topics - ENABLE and keep WHITE
    if (arraysCard != null)
    {
        arraysCard.SetActive(true);
        Debug.Log($"✓ Arrays card enabled - Active: {arraysCard.activeInHierarchy}");
    }
    
    if (stacksCard != null)
    {
        stacksCard.SetActive(true);
        Debug.Log($"✓ Stacks card enabled - Active: {stacksCard.activeInHierarchy}");
    }
    
    if (queuesCard != null)
    {
        queuesCard.SetActive(true);
        Debug.Log($"✓ Queues card enabled - Active: {queuesCard.activeInHierarchy}");
    }
    
    if (linkedListsCard != null)
    {
        linkedListsCard.SetActive(true);
        Debug.Log($"✓ LinkedLists card enabled - Active: {linkedListsCard.activeInHierarchy}");
    }
    
    if (treesCard != null)
    {
        treesCard.SetActive(true);
        Debug.Log($"✓ Trees card enabled - Active: {treesCard.activeInHierarchy}");
    }
    
    if (graphsCard != null)
    {
        graphsCard.SetActive(true);
        Debug.Log($"✓ Graphs card enabled - Active: {graphsCard.activeInHierarchy}");
    }
    
    // New topics - ENABLE but will be GRAYED
    if (hashmapsCard != null)
    {
        hashmapsCard.SetActive(true);
        Debug.Log($"✓ Hashmaps card enabled - Active: {hashmapsCard.activeInHierarchy}");
    }
    else
    {
        Debug.LogError("❌ Hashmaps card is NULL!");
    }
    
    if (heapsCard != null)
    {
        heapsCard.SetActive(true);
        Debug.Log($"✓ Heaps card enabled - Active: {heapsCard.activeInHierarchy}");
    }
    else
    {
        Debug.LogError("❌ Heaps card is NULL!");
    }
    
    if (dequeCard != null)
    {
        dequeCard.SetActive(true);
        Debug.Log($"✓ Deque card enabled - Active: {dequeCard.activeInHierarchy}");
    }
    else
    {
        Debug.LogError("❌ Deque card is NULL!");
    }
    
    if (binaryHeapsCard != null)
    {
        binaryHeapsCard.SetActive(true);
        Debug.Log($"✓ Binary Heaps card enabled - Active: {binaryHeapsCard.activeInHierarchy}");
    }
    else
    {
        Debug.LogError("❌ Binary Heaps card is NULL!");
    }
    
    // ✅ Reapply styling after enabling cards
     ApplyComingSoonStyling();
    
    Debug.Log("=== Topic Cards Force Enable Complete ===");
}

    void ShowTopicSelection()
    {
        HideAllScreens();
        if (topicSelectionScreen != null) topicSelectionScreen.SetActive(true);
    }

    void ShowQueueModes()
    {
        HideAllScreens();
        if (queueModesScreen != null) queueModesScreen.SetActive(true);
    }

    void ShowProgress()
    {
        Debug.Log("Progress screen - Coming soon!");
    }

    void ShowProfile()
    {
        ShowProfileEdit();
    }

    void ShowProfileEdit()
    {
        HideAllScreens();
        if (profileEditScreen != null) 
        {
            profileEditScreen.SetActive(true);
            
            if (profileNameText != null) profileNameText.text = studentName;
            if (profileUsernameText != null) profileUsernameText.text = currentUsername;
            
            tempSelectedProfilePic = currentProfilePictureIndex;
            UpdateProfileEditDisplay();
        }
    }

  public void ShowARPanel()
{
    HideAllScreens();
    if (arPanel != null)
        arPanel.SetActive(true);
    else
        Debug.LogWarning("⚠️ AR Panel not assigned in MainMenuManager!");
}

void HideAllScreens()
{
    if (homeScreen != null) homeScreen.SetActive(false);
    if (topicSelectionScreen != null) topicSelectionScreen.SetActive(false);
    if (queueModesScreen != null) queueModesScreen.SetActive(false);
    if (profileEditScreen != null) profileEditScreen.SetActive(false);
    if (arPanel != null) arPanel.SetActive(false); // ADD THIS

    Debug.Log("📋 HideAllScreens called");
}
    void SelectTempProfilePicture(int index)
    {
        tempSelectedProfilePic = index;
        UpdateProfileEditDisplay();
    }

    void UpdateProfileEditDisplay()
    {
        if (profileEditImage != null && profilePictureSprites != null && 
            tempSelectedProfilePic < profilePictureSprites.Length)
        {
            profileEditImage.sprite = profilePictureSprites[tempSelectedProfilePic];
        }
        
        for (int i = 0; i < profileEditOptions.Length; i++)
        {
            if (profileEditOptions[i] != null)
            {
                Transform border = profileEditOptions[i].transform.Find("SelectedBorder");
                if (border != null)
                {
                    border.gameObject.SetActive(i == tempSelectedProfilePic);
                }
            }
        }
    }

    void SaveProfileChanges()
    {
        currentProfilePictureIndex = tempSelectedProfilePic;
        PlayerPrefs.SetInt("User_" + currentUsername + "_ProfilePic", currentProfilePictureIndex);
        PlayerPrefs.Save();
        
        UpdateProfilePicture();
        ShowHomeScreen();
    }

    void CancelProfileEdit()
    {
        ShowHomeScreen();
    }

    void Logout()
    {
        PlayerPrefs.DeleteKey("CurrentUser");
        PlayerPrefs.DeleteKey("RememberMe");
        PlayerPrefs.DeleteKey("SavedUsername");
        PlayerPrefs.Save();
        
        Debug.Log("User logged out successfully");
        SceneManager.LoadScene(loginSceneName);
    }

    void ContinueQueue()
    {
        LoadQueueScene();
    }

    void ContinueLastTopic()
    {
        string lastTopic = PlayerPrefs.GetString("User_" + currentUsername + "_LastTopic", "");
        
        if (string.IsNullOrEmpty(lastTopic))
        {
            ShowTopicSelection();
            return;
        }
        
        Debug.Log($"Continuing with topic: {lastTopic}");
        
        if (TopicNameConstants.Normalize(lastTopic) == TopicNameConstants.QUEUE)
        {
            LoadQueueScene();
        }
        else
        {
            ShowComingSoon(lastTopic);
        }
    }

    void StartTutorialMode()
    {
        PlayerPrefs.SetString("QueueMode", "Tutorial");
        LoadQueueScene();
    }

    void StartGuidedMode()
    {
        PlayerPrefs.SetString("QueueMode", "Guided");
        LoadQueueScene();
    }

    void StartChallengeMode()
    {
        PlayerPrefs.SetString("QueueMode", "Challenge");
        LoadQueueScene();
    }

    void LoadQueueScene()
    {
        if (Application.CanStreamedLevelBeLoaded(queueSceneName))
        {
            SceneManager.LoadScene(queueSceneName);
        }
        else
        {
            Debug.LogWarning($"Scene '{queueSceneName}' not found in build settings!");
        }
    }

    void ShowComingSoon(string topicName)
    {
        Debug.Log($"{topicName} - Coming Soon!");
    }

    public void RefreshProgress()
    {
        Debug.Log("🔄 Manual refresh triggered");
        StartCoroutine(WaitForDataSyncAndUpdate());
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
}

[System.Serializable]
public class UserActivityData
{
    public string username;
    public string name;
    public string lastLogin;
    public int streak;
    public int completedTopics;
}