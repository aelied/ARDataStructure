using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;
using System;

public class ProgressSceneManager : MonoBehaviour
{
    [System.Serializable]
    public class TopicProgressCard
    {
        public string topicName;
        public GameObject cardObject;
        public TextMeshProUGUI topicNameText;
        public Image progressBarFill;
        public TextMeshProUGUI progressPercentageText;
        public TextMeshProUGUI statusText;
        public Image statusIcon;
        public Button viewDetailsButton;
        
        [Header("Detailed Stats")]
        public TextMeshProUGUI tutorialStatusText;
        public TextMeshProUGUI puzzleStatusText;
        public TextMeshProUGUI puzzleScoreText;
        public TextMeshProUGUI lastAccessedText;
        public Image[] starImages;
    }
    
    [Header("UI References")]
    public GameObject progressPanel;
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI overallProgressText;
    public Image overallProgressBar;
    public TextMeshProUGUI streakText;
    public TextMeshProUGUI totalTopicsCompletedText;
    
    [Header("Topic Cards")]
    public TopicProgressCard queuesCard;
    public TopicProgressCard stacksCard;
    public TopicProgressCard linkedListsCard;
    public TopicProgressCard treesCard;
    public TopicProgressCard graphsCard;
    
    [Header("Detailed View")]
    public GameObject detailedViewPanel;
    public TextMeshProUGUI detailedTopicName;
    public TextMeshProUGUI detailedProgressText;
    public Image detailedProgressBar;
    public TextMeshProUGUI tutorialCompletedText;
    public TextMeshProUGUI puzzleCompletedText;
    public TextMeshProUGUI puzzleScoreDetailText;
    public TextMeshProUGUI lastAccessedDetailText;
    public TextMeshProUGUI timeSpentText;
    public TextMeshProUGUI lessonsCompletedText;
    public Image[] detailedStarImages;
    public Button closeDetailButton;
    public Button retryTopicButton;
    
    [Header("Statistics")]
    public TextMeshProUGUI totalTimeSpentText;
    public TextMeshProUGUI averageScoreText;
    public TextMeshProUGUI bestTopicText;
    public TextMeshProUGUI totalLessonsCompletedText;
    
    [Header("Navigation")]
    public Button backButton;
    public Button refreshButton;
    public Button syncButton;
    
    [Header("Visual Assets")]
    public Sprite completedIcon;
    public Sprite inProgressIcon;
    public Sprite notStartedIcon;
    public Sprite starFilledSprite;
    public Sprite starEmptySprite;
    public Color completedColor = new Color(0.3f, 0.8f, 0.3f);
    public Color inProgressColor = new Color(1f, 0.8f, 0.2f);
    public Color notStartedColor = new Color(0.7f, 0.7f, 0.7f);
    
    [Header("Database Settings")]
    public string adminApiUrl = "https://structureality-admin.onrender.com/api";
    public bool autoSyncEnabled = true;
    public float syncInterval = 30f;
    
    private string currentUsername;
    private Dictionary<string, TopicProgressCard> topicCards;
    private List<string> allTopics = new List<string> { "Queue", "Stacks", "LinkedLists", "Trees", "Graphs" };
    
    private DatabaseProgressData cachedProgressData;
    private bool isDataLoaded = false;
    private float nextSyncTime;
    
    void Start()
    {
        InitializeTopicCards();
        LoadUserData();
        SetupButtons();
        
        StartCoroutine(InitializeProgress());
    }
    
    IEnumerator InitializeProgress()
    {
        Debug.Log("🔄 Initializing progress screen...");
        
        yield return StartCoroutine(FetchProgressFromDatabase());
        
        if (isDataLoaded)
        {
            Debug.Log("✅ Data loaded, refreshing display...");
            RefreshAllProgress();
        }
        else
        {
            Debug.LogWarning("⚠️ No data loaded, showing empty state");
        }
    }
    
    void InitializeTopicCards()
    {
        topicCards = new Dictionary<string, TopicProgressCard>
        {
            { "Queue", queuesCard },
            { "Stacks", stacksCard },
            { "LinkedLists", linkedListsCard },
            { "Trees", treesCard },
            { "Graphs", graphsCard }
        };
        
        foreach (var kvp in topicCards)
        {
            string topicName = kvp.Key;
            TopicProgressCard card = kvp.Value;
            
            if (card.viewDetailsButton != null)
            {
                card.viewDetailsButton.onClick.AddListener(() => ShowDetailedView(topicName));
            }
        }
    }
    
    void LoadUserData()
    {
        currentUsername = PlayerPrefs.GetString("CurrentUser", "");
        
        if (string.IsNullOrEmpty(currentUsername))
        {
            Debug.LogError("❌ No user logged in!");
            SceneManager.LoadScene("LoginRegister");
            return;
        }
        
        Debug.Log($"📝 Current user: {currentUsername}");
        
        if (headerText != null)
        {
            string userName = PlayerPrefs.GetString("User_" + currentUsername + "_Name", "Student");
            headerText.text = $"{userName}'s Progress";
        }
    }
    
    void SetupButtons()
    {
        if (backButton != null)
            backButton.onClick.AddListener(BackToMainMenu);
        
        if (refreshButton != null)
            refreshButton.onClick.AddListener(() => StartCoroutine(FetchProgressFromDatabase()));
        
        if (syncButton != null)
            syncButton.onClick.AddListener(() => StartCoroutine(FetchProgressFromDatabase()));
        
        if (closeDetailButton != null)
            closeDetailButton.onClick.AddListener(CloseDetailedView);
        
        if (retryTopicButton != null)
            retryTopicButton.onClick.AddListener(RetryCurrentTopic);
    }
    
    IEnumerator FetchProgressFromDatabase()
    {
        if (string.IsNullOrEmpty(currentUsername) || string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.LogWarning("⚠️ Cannot fetch: No user or API URL");
            yield break;
        }
        
        string url = $"{adminApiUrl}/progress/{currentUsername}";
        Debug.Log($"🔄 Fetching from: {url}");
        
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Progress fetched successfully");
            Debug.Log($"📦 Raw response: {request.downloadHandler.text}");
            
            try
            {
                DatabaseProgressResponse response = JsonUtility.FromJson<DatabaseProgressResponse>(request.downloadHandler.text);
                
                if (response != null && response.success && response.data != null)
                {
                    cachedProgressData = response.data;
                    isDataLoaded = true;
                    
                    Debug.Log($"✅ Parsed data for: {cachedProgressData.username}");
                    Debug.Log($"📊 Topics in response: {cachedProgressData.topics.Count}");
                    
                    foreach (var topic in cachedProgressData.topics)
                    {
                        Debug.Log($"  📚 {topic.topicName}: {topic.lessonsCompleted} lessons completed");
                    }
                    
                    RefreshAllProgress();
                }
                else
                {
                    Debug.LogError("❌ Invalid response structure");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Parse error: {e.Message}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }
        }
        else
        {
            Debug.LogError($"❌ Request failed: {request.error}");
            Debug.LogError($"Response code: {request.responseCode}");
            Debug.LogError($"Response: {request.downloadHandler.text}");
        }
    }
    
    public void RefreshAllProgress()
    {
        if (!isDataLoaded || cachedProgressData == null)
        {
            Debug.LogWarning("⚠️ No data to refresh");
            return;
        }
        
        Debug.Log("🔄 Refreshing all progress displays...");
        UpdateOverallProgress();
        UpdateTopicCards();
        UpdateStatistics();
        Debug.Log("✅ Refresh complete");
    }
    
    void UpdateOverallProgress()
    {
        if (cachedProgressData == null) return;
        
        Debug.Log("=== Updating Overall Progress ===");
        
        float totalProgress = 0f;
        int completedCount = 0;
        int totalLessonsCompleted = 0;
        
        foreach (var topicData in cachedProgressData.topics)
        {
            float topicProgress = topicData.progressPercentage;
            totalProgress += topicProgress;
            
            if (topicProgress >= 100f)
                completedCount++;
            
            totalLessonsCompleted += topicData.lessonsCompleted;
            
            Debug.Log($"  {topicData.topicName}: {topicData.lessonsCompleted} lessons, {topicProgress}%");
        }
        
        float overallPercentage = cachedProgressData.topics.Count > 0 
            ? totalProgress / cachedProgressData.topics.Count 
            : 0f;
        
        if (overallProgressText != null)
            overallProgressText.text = $"{overallPercentage:F0}%";
        
        if (overallProgressBar != null)
            overallProgressBar.fillAmount = overallPercentage / 100f;
        
        if (totalTopicsCompletedText != null)
            totalTopicsCompletedText.text = $"{completedCount}/{cachedProgressData.topics.Count} Topics";
        
        if (streakText != null)
            streakText.text = $"🔥 {cachedProgressData.streak} Day Streak";
        
        if (totalLessonsCompletedText != null)
        {
            totalLessonsCompletedText.text = $"📚 {totalLessonsCompleted} Lessons Completed";
            Debug.Log($"✅ Lessons display updated: {totalLessonsCompleted}");
        }
        else
        {
            Debug.LogError("❌ totalLessonsCompletedText is NULL!");
        }
        
        Debug.Log($"Overall: {overallPercentage:F0}%, {completedCount} topics, {totalLessonsCompleted} lessons");
    }
    
    void UpdateTopicCards()
    {
        if (cachedProgressData == null) return;
        
        Debug.Log("=== Updating Topic Cards ===");
        
        foreach (var topicData in cachedProgressData.topics)
        {
            string topicName = TopicNameConstants.Normalize(topicData.topicName);
            
            if (!topicCards.ContainsKey(topicName))
            {
                Debug.LogWarning($"⚠️ No card for: {topicName}");
                continue;
            }
            
            TopicProgressCard card = topicCards[topicName];
            
            if (card == null || card.cardObject == null) continue;
            
            float progress = topicData.progressPercentage;
            bool tutorialComplete = topicData.tutorialCompleted;
            bool puzzleComplete = topicData.puzzleCompleted;
            int puzzleScore = topicData.puzzleScore;
            
            if (card.topicNameText != null)
                card.topicNameText.text = topicName;
            
            if (card.progressBarFill != null)
                card.progressBarFill.fillAmount = progress / 100f;
            
            if (card.progressPercentageText != null)
                card.progressPercentageText.text = $"{progress:F0}%";
            
            string status = "Not Started";
            Color statusColor = notStartedColor;
            Sprite statusIconSprite = notStartedIcon;
            
            if (progress >= 100f)
            {
                status = "Completed ✓";
                statusColor = completedColor;
                statusIconSprite = completedIcon;
            }
            else if (progress > 0f)
            {
                status = "In Progress";
                statusColor = inProgressColor;
                statusIconSprite = inProgressIcon;
            }
            
            if (card.statusText != null)
            {
                card.statusText.text = status;
                card.statusText.color = statusColor;
            }
            
            if (card.statusIcon != null && statusIconSprite != null)
            {
                card.statusIcon.sprite = statusIconSprite;
                card.statusIcon.enabled = true;
                card.statusIcon.color = Color.white;
            }
            
            if (card.tutorialStatusText != null)
                card.tutorialStatusText.text = tutorialComplete ? "✓" : "○";
            
            if (card.puzzleStatusText != null)
                card.puzzleStatusText.text = puzzleComplete ? "✓" : "○";
            
            if (card.puzzleScoreText != null)
                card.puzzleScoreText.text = puzzleComplete ? $"{puzzleScore}%" : "N/A";
            
            if (card.lastAccessedText != null)
                card.lastAccessedText.text = FormatLastAccessed(topicData.lastAccessed);
            
            UpdateStars(card.starImages, puzzleScore);
            
            Debug.Log($"  ✅ {topicName}: {progress}%, {topicData.lessonsCompleted} lessons");
        }
    }
    
    void UpdateStatistics()
    {
        if (cachedProgressData == null) return;
        
        Debug.Log("=== Updating Statistics ===");
        
        float totalTime = 0f;
        float totalScore = 0f;
        int completedPuzzles = 0;
        string bestTopic = "None";
        float bestScore = 0f;
        int totalLessons = 0;
        
        foreach (var topicData in cachedProgressData.topics)
        {
            totalTime += topicData.timeSpent;
            totalLessons += topicData.lessonsCompleted;
            
            if (topicData.puzzleCompleted)
            {
                int score = topicData.puzzleScore;
                totalScore += score;
                completedPuzzles++;
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTopic = topicData.topicName;
                }
            }
        }
        
        if (totalTimeSpentText != null)
        {
            int hours = Mathf.FloorToInt(totalTime / 3600f);
            int minutes = Mathf.FloorToInt((totalTime % 3600f) / 60f);
            totalTimeSpentText.text = $"{hours}h {minutes}m";
        }
        
        if (averageScoreText != null)
        {
            float avgScore = completedPuzzles > 0 ? totalScore / completedPuzzles : 0f;
            averageScoreText.text = $"{avgScore:F0}%";
        }
        
        if (bestTopicText != null)
            bestTopicText.text = bestTopic;
        
        if (totalLessonsCompletedText != null)
        {
            totalLessonsCompletedText.text = $"{totalLessons} Lessons";
            Debug.Log($"✅ Stats lessons: {totalLessons}");
        }
        
        Debug.Log($"Stats: {totalLessons} lessons, {completedPuzzles} puzzles, best: {bestTopic}");
    }
    
    void ShowDetailedView(string topicName)
    {
        if (detailedViewPanel == null || cachedProgressData == null) return;
        
        detailedViewPanel.SetActive(true);
        
        DatabaseTopicData topicData = cachedProgressData.topics.Find(t => 
            TopicNameConstants.Normalize(t.topicName) == topicName);
        
        if (topicData == null)
        {
            Debug.LogWarning($"No data for: {topicName}");
            return;
        }
        
        if (detailedTopicName != null)
            detailedTopicName.text = topicName;
        
        if (detailedProgressText != null)
            detailedProgressText.text = $"{topicData.progressPercentage:F0}% Complete";
        
        if (detailedProgressBar != null)
            detailedProgressBar.fillAmount = topicData.progressPercentage / 100f;
        
        if (tutorialCompletedText != null)
            tutorialCompletedText.text = topicData.tutorialCompleted ? "✓ Completed" : "○ Not Completed";
        
        if (puzzleCompletedText != null)
            puzzleCompletedText.text = topicData.puzzleCompleted ? "✓ Completed" : "○ Not Completed";
        
        if (puzzleScoreDetailText != null)
            puzzleScoreDetailText.text = topicData.puzzleCompleted ? $"{topicData.puzzleScore}%" : "Not attempted";
        
        if (lessonsCompletedText != null)
            lessonsCompletedText.text = $"📚 {topicData.lessonsCompleted} Lessons Completed";
        
        if (lastAccessedDetailText != null)
            lastAccessedDetailText.text = FormatLastAccessed(topicData.lastAccessed);
        
        if (timeSpentText != null)
        {
            int minutes = Mathf.FloorToInt(topicData.timeSpent / 60f);
            int seconds = Mathf.FloorToInt(topicData.timeSpent % 60f);
            timeSpentText.text = $"{minutes}m {seconds}s";
        }
        
        UpdateStars(detailedStarImages, topicData.puzzleScore);
        
        PlayerPrefs.SetString("DetailViewTopic", topicName);
    }
    
    void CloseDetailedView()
    {
        if (detailedViewPanel != null)
            detailedViewPanel.SetActive(false);
    }
    
    void RetryCurrentTopic()
    {
        string topic = PlayerPrefs.GetString("DetailViewTopic", "");
        if (string.IsNullOrEmpty(topic)) return;
        
        PlayerPrefs.SetString("SelectedTopic", topic);
        
        string sceneName = topic.Replace(" ", "") + "Scene";
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning($"Scene not found: {sceneName}");
        }
    }
    
    void UpdateStars(Image[] starImages, int score)
    {
        if (starImages == null || starImages.Length == 0) return;
        
        int filledStars = 0;
        if (score >= 90) filledStars = 3;
        else if (score >= 70) filledStars = 2;
        else if (score >= 50) filledStars = 1;
        
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                if (i < filledStars && starFilledSprite != null)
                {
                    starImages[i].sprite = starFilledSprite;
                    starImages[i].enabled = true;
                }
                else if (starEmptySprite != null)
                {
                    starImages[i].sprite = starEmptySprite;
                    starImages[i].enabled = true;
                }
            }
        }
    }
    
    string FormatLastAccessed(string dateString)
    {
        if (string.IsNullOrEmpty(dateString) || dateString == "Never") return "Never";
        
        try
        {
            DateTime date = DateTime.Parse(dateString);
            TimeSpan timeSince = DateTime.Now - date;
            
            if (timeSince.TotalMinutes < 60)
                return $"{(int)timeSince.TotalMinutes}m ago";
            else if (timeSince.TotalHours < 24)
                return $"{(int)timeSince.TotalHours}h ago";
            else if (timeSince.TotalDays < 7)
                return $"{(int)timeSince.TotalDays}d ago";
            else
                return date.ToString("MMM dd, yyyy");
        }
        catch
        {
            return dateString;
        }
    }
    
    void Update()
    {
        if (autoSyncEnabled && !string.IsNullOrEmpty(adminApiUrl) && Time.time >= nextSyncTime)
        {
            nextSyncTime = Time.time + syncInterval;
            StartCoroutine(FetchProgressFromDatabase());
        }
    }
    
    void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    
    void OnApplicationQuit()
    {
        // Data is read from database
    }
}

// NOTE: Database classes are now in DatabaseClasses.cs - No duplicates!