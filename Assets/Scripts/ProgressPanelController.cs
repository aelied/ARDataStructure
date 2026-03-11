using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using System.Linq;

/// <summary>
/// DATABASE-ONLY Progress Panel - UPDATED with Difficulty-based Challenge Tracking
/// Trophy shows when progressPercentage >= 100%
/// Lock shows when progressPercentage < 100%
/// Challenge count now tracks EACH difficulty completion (4 max per topic)
/// </summary>
public class ProgressPanelController : MonoBehaviour
{
    [Header("Header Stats")]
    public TextMeshProUGUI lessonsCountText;
    public TextMeshProUGUI challengesCountText;
    public TextMeshProUGUI pointsCountText;
    
    [Header("Analytics Section")]
    public TextMeshProUGUI avgTimeText;
    public TextMeshProUGUI avgTimeSubtitleText;
    public TextMeshProUGUI avgScoreText;
    public TextMeshProUGUI avgScoreSubtitleText;
    public TextMeshProUGUI completionText;
    public TextMeshProUGUI completionSubtitleText;
    public TextMeshProUGUI quizAccuracyText;
    public TextMeshProUGUI quizAccuracySubtitleText;
    
    [Header("Achievement Cards")]
    public List<TopicAchievementCard> achievementCards;
    
    [Header("Learning Streak")]
    public TextMeshProUGUI streakDaysText;
    public TextMeshProUGUI streakMessageText;
    public Transform streakDotsContainer;
    public GameObject streakDotPrefab;
    
    [Header("Colors")]
    public Color completedColor = new Color(1f, 0.95f, 0.6f);
    public Color inProgressColor = new Color(1f, 1f, 0.9f);
    public Color lockedColor = new Color(0.9f, 0.9f, 0.9f);
    public Color streakActiveColor = new Color(0.4f, 0.8f, 0.4f);
    public Color streakInactiveColor = new Color(0.8f, 0.8f, 0.8f);
    
    [Header("API Settings")]
    public string adminApiUrl = "https://structureality-admin.onrender.com/api";
    
    [Header("Loading Indicator")]
    public GameObject loadingIndicator;
    
    private UserProgressManager progressManager;
    private DatabaseProgressData cachedProgressData;
    private bool isDataLoaded = false;
    private Dictionary<string, int> topicLessonCounts = new Dictionary<string, int>();
    
    [System.Serializable]
    public class TopicAchievementCard
    {
        public string topicName;
        public string displayTitle;
        public Image backgroundImage;
        public TextMeshProUGUI titleText;
        public GameObject completedBadge;
        public GameObject lockIcon;
        public Image progressBar;
    }
    
    void Start()
    {
        progressManager = UserProgressManager.Instance;
        
        if (progressManager == null)
        {
            Debug.LogError("UserProgressManager not found!");
            return;
        }
        
        InitializeCards();
        StartCoroutine(FetchAndDisplayProgress());
    }
    
    void OnEnable()
    {
        if (!isDataLoaded)
        {
            StartCoroutine(FetchAndDisplayProgress());
        }
        else
        {
            UpdateProgressDisplay();
        }
    }
    
    void ShowLoadingIndicator(bool show)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(show);
    }
    
    IEnumerator FetchAndDisplayProgress()
    {
        string username = progressManager.GetCurrentUsername();
        
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogError("❌ No username!");
            yield break;
        }
        
        ShowLoadingIndicator(true);
        
        yield return StartCoroutine(FetchLessonCounts());
        
        string url = $"{adminApiUrl}/progress/{username}";
        Debug.Log($"🔄 Fetching progress from: {url}");
        
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Progress fetched");
            Debug.Log($"📥 RAW JSON: {request.downloadHandler.text}");
            
            try
            {
                DatabaseProgressResponse response = JsonUtility.FromJson<DatabaseProgressResponse>(request.downloadHandler.text);
                
                if (response != null && response.success && response.data != null)
                {
                    cachedProgressData = response.data;
                    
                    Debug.Log("=== DIFFICULTY SCORES FROM API ===");
                    foreach (var topic in cachedProgressData.topics)
                    {
                        Debug.Log($"🎯 {topic.topicName}:");
                        if (topic.difficultyScores != null)
                        {
                            Debug.Log($"   Easy: {topic.difficultyScores.easy}%");
                            Debug.Log($"   Medium: {topic.difficultyScores.medium}%");
                            Debug.Log($"   Hard: {topic.difficultyScores.hard}%");
                            Debug.Log($"   Mixed: {topic.difficultyScores.mixed}%");
                        }
                        else
                        {
                            Debug.LogWarning($"   ⚠️ difficultyScores is NULL!");
                        }
                    }
                    
                    RecalculateAllTopicProgress();
                    isDataLoaded = true;
                    
                    Debug.Log($"✅ Loaded data for: {cachedProgressData.username}");
                    Debug.Log($"📊 Topics: {cachedProgressData.topics.Count}");
                    
                    UpdateProgressDisplay();
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
        
        ShowLoadingIndicator(false);
    }
    
    void RecalculateAllTopicProgress()
    {
        if (cachedProgressData == null || cachedProgressData.topics == null)
        {
            Debug.LogWarning("⚠️ Cannot recalculate - no cached data");
            return;
        }
        
        Debug.Log("=== Recalculating Progress in ProgressPanel ===");
        
        foreach (var topic in cachedProgressData.topics)
        {
            int totalLessons = GetTotalLessonCount(topic.topicName);
            
            float lessonProgress = 0f;
            float puzzleProgress = 0f;
            
            if (totalLessons > 0)
            {
                lessonProgress = ((float)topic.lessonsCompleted / totalLessons) * 70f;
            }
            
            if (topic.puzzleCompleted)
            {
                puzzleProgress = 30f;
            }
            
            topic.progressPercentage = lessonProgress + puzzleProgress;
            
            Debug.Log($"📊 {topic.topicName}: {topic.lessonsCompleted}/{totalLessons} lessons = {lessonProgress:F1}%, Puzzle = {puzzleProgress:F1}%, Total = {topic.progressPercentage:F1}%");
        }
        
        Debug.Log("✅ Progress recalculation complete");
    }
    
    IEnumerator FetchLessonCounts()
    {
        if (string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.LogWarning("⚠️ API URL not set");
            yield break;
        }
        
        string url = $"{adminApiUrl}/lessons";
        Debug.Log($"🔄 Fetching lesson counts from: {url}");
        
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
    
    void InitializeCards()
    {
        foreach (var card in achievementCards)
        {
            if (card.completedBadge != null)
                card.completedBadge.SetActive(false);
            
            if (card.lockIcon != null)
                card.lockIcon.SetActive(false);
        }
        
        Debug.Log("✓ Achievement cards initialized");
    }
    
    public void UpdateProgressDisplay()
    {
        if (!isDataLoaded || cachedProgressData == null)
        {
            Debug.LogWarning("⚠️ No data to display");
            return;
        }
        
        UpdateHeaderStats();
        UpdateAnalyticsSection();
        UpdateAchievementCards();
        UpdateStreakDisplay();
    }
    
 void UpdateHeaderStats()
    {
        if (cachedProgressData == null) return;
        
        int totalLessons = 0;
        int challengesCompleted = 0; // Count each difficulty completion (>0%)
        int points = 0;
        
        foreach (var topic in cachedProgressData.topics)
        {
            totalLessons += topic.lessonsCompleted;
            
            // ✅ Count EACH difficulty with score > 0% as a completed challenge
            if (topic.difficultyScores != null)
            {
                if (topic.difficultyScores.easy > 0) challengesCompleted++;
                if (topic.difficultyScores.medium > 0) challengesCompleted++;
                if (topic.difficultyScores.hard > 0) challengesCompleted++;
                if (topic.difficultyScores.mixed > 0) challengesCompleted++;
            }
        }
        
        // ✅ Points calculation
        points += totalLessons * 50;           // 50 points per lesson
        points += challengesCompleted * 100;   // 100 points per difficulty completed
        
        if (lessonsCountText != null)
        {
            lessonsCountText.text = totalLessons.ToString();
            Debug.Log($"✅ Lessons Count: {totalLessons}");
        }
        
        if (challengesCountText != null)
        {
            challengesCountText.text = challengesCompleted.ToString();
            Debug.Log($"✅ Challenges Completed: {challengesCompleted} (counting all difficulties with score > 0)");
        }
        
        if (pointsCountText != null)
        {
            pointsCountText.text = points.ToString();
            Debug.Log($"✅ Total Points: {points}");
        }
    }
    
    void UpdateAnalyticsSection()
    {
        if (cachedProgressData == null || cachedProgressData.topics == null)
        {
            Debug.LogWarning("⚠️ No data for analytics");
            return;
        }
        
        Debug.Log("=== Updating Analytics Section ===");
        
        int totalLessonsAcrossTopics = topicLessonCounts.Values.Sum();
        int completedLessons = 0;
        float totalTimeSpent = 0f;
        float totalScore = 0f;
        int topicsWithData = 0;
        int quizzesCompleted = 0;
        float totalQuizScore = 0f;
        
        foreach (var topic in cachedProgressData.topics)
        {
            completedLessons += topic.lessonsCompleted;
            totalTimeSpent += topic.timeSpent;
            
            if (topic.lessonsCompleted > 0 || topic.puzzleCompleted)
            {
                topicsWithData++;
                totalScore += topic.progressPercentage;
            }
            
            // ✅ NEW: Count EACH difficulty with score > 0
            if (topic.difficultyScores != null)
            {
                if (topic.difficultyScores.easy > 0)
                {
                    quizzesCompleted++;
                    totalQuizScore += topic.difficultyScores.easy;
                }
                if (topic.difficultyScores.medium > 0)
                {
                    quizzesCompleted++;
                    totalQuizScore += topic.difficultyScores.medium;
                }
                if (topic.difficultyScores.hard > 0)
                {
                    quizzesCompleted++;
                    totalQuizScore += topic.difficultyScores.hard;
                }
                if (topic.difficultyScores.mixed > 0)
                {
                    quizzesCompleted++;
                    totalQuizScore += topic.difficultyScores.mixed;
                }
            }
        }
        
        Debug.Log($"🎯 Final Quiz Stats: Total Score = {totalQuizScore}, Completed = {quizzesCompleted}");
        
        // Average time per lesson
        float avgTimePerLesson = completedLessons > 0 ? totalTimeSpent / completedLessons : 0f;
        if (avgTimeText != null)
        {
            avgTimeText.text = FormatTime(avgTimePerLesson);
        }
        if (avgTimeSubtitleText != null)
        {
            avgTimeSubtitleText.text = "Per lesson";
        }
        
        // Average score across topics
        float avgScore = topicsWithData > 0 ? totalScore / topicsWithData : 0f;
        if (avgScoreText != null)
        {
            avgScoreText.text = $"{avgScore:F0}%";
        }
        if (avgScoreSubtitleText != null)
        {
            avgScoreSubtitleText.text = "Across all topics";
        }
        
        // Completion percentage
        float completionPercentage = totalLessonsAcrossTopics > 0 
            ? (float)completedLessons / totalLessonsAcrossTopics * 100f 
            : 0f;
        if (completionText != null)
        {
            completionText.text = $"{completionPercentage:F0}%";
        }
        if (completionSubtitleText != null)
        {
            completionSubtitleText.text = $"{completedLessons} out of {totalLessonsAcrossTopics} lessons";
        }
        
        // ✅ NEW: Quiz accuracy now based on ALL difficulty completions
        float quizAccuracy = quizzesCompleted > 0 ? totalQuizScore / quizzesCompleted : 0f;
        
        if (quizAccuracyText != null)
        {
            quizAccuracyText.text = quizzesCompleted > 0 ? $"{quizAccuracy:F0}%" : "N/A";
        }
        if (quizAccuracySubtitleText != null)
        {
            if (quizzesCompleted == 0)
            {
                quizAccuracySubtitleText.text = "Complete a challenge!";
            }
            else if (quizAccuracy >= 80)
            {
                quizAccuracySubtitleText.text = "Excellent performance!";
            }
            else if (quizAccuracy >= 60)
            {
                quizAccuracySubtitleText.text = "Good job!";
            }
            else
            {
                quizAccuracySubtitleText.text = "Keep practicing!";
            }
        }
        
        Debug.Log($"📊 Analytics Updated:");
        Debug.Log($"   Avg Time: {FormatTime(avgTimePerLesson)}");
        Debug.Log($"   Avg Score: {avgScore:F1}%");
        Debug.Log($"   Completion: {completionPercentage:F1}% ({completedLessons}/{totalLessonsAcrossTopics})");
        Debug.Log($"   Quiz Accuracy: {quizAccuracy:F1}% ({quizzesCompleted} difficulties completed)");
        Debug.Log($"   Total Quiz Score: {totalQuizScore} from {quizzesCompleted} difficulty completions");
    }
    
    string FormatTime(float seconds)
    {
        if (seconds < 60)
        {
            return $"{seconds:F0}s";
        }
        else if (seconds < 3600)
        {
            int minutes = Mathf.FloorToInt(seconds / 60);
            return $"{minutes}m";
        }
        else
        {
            int hours = Mathf.FloorToInt(seconds / 3600);
            int minutes = Mathf.FloorToInt((seconds % 3600) / 60);
            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        }
    }
    
    void UpdateAchievementCards()
    {
        if (cachedProgressData == null || achievementCards == null) return;
        
        Debug.Log("=== Updating Achievement Cards ===");
        
        foreach (var card in achievementCards)
        {
            if (card == null)
            {
                Debug.LogWarning("Card is null in achievementCards list!");
                continue;
            }
            
            var topicName = card.topicName;
            
            if (string.IsNullOrEmpty(topicName))
            {
                Debug.LogWarning("Card has empty topic name!");
                continue;
            }
            
            var topicData = cachedProgressData.topics.Find(t => 
                TopicNameConstants.Normalize(t.topicName) == TopicNameConstants.Normalize(topicName));
            
            if (topicData == null)
            {
                Debug.LogWarning($"No topic data found for: {topicName}");
                continue;
            }
            
            Debug.Log($"📋 Processing card: {topicName}");
            
            if (card.titleText != null)
            {
                card.titleText.text = card.displayTitle;
            }
            
            if (card.progressBar != null)
            {
                float progress = topicData.progressPercentage / 100f;
                card.progressBar.fillAmount = Mathf.Clamp01(progress);
                Debug.Log($"  ✓ Progress: {topicData.progressPercentage:F1}%");
            }
            
            bool isCompleted = topicData.progressPercentage >= 100f;
            
            Debug.Log($"  🔍 Topic '{topicName}':");
            Debug.Log($"     Progress: {topicData.progressPercentage:F1}%");
            Debug.Log($"     → Complete: {isCompleted}");
            
            if (card.completedBadge != null)
            {
                card.completedBadge.SetActive(isCompleted);
                Debug.Log($"  🏆 Trophy: {(isCompleted ? "SHOWN ✓" : "HIDDEN")}");
            }
            else
            {
                Debug.LogWarning($"  ⚠️ Trophy badge is NULL for {topicName}!");
            }
            
            if (card.lockIcon != null)
            {
                card.lockIcon.SetActive(!isCompleted);
                Debug.Log($"  🔒 Lock: {(!isCompleted ? "SHOWN (locked)" : "HIDDEN (unlocked) ✓")}");
            }
            else
            {
                Debug.LogWarning($"  ⚠️ Lock icon is NULL for {topicName}!");
            }
            
            if (card.backgroundImage != null)
            {
                if (isCompleted)
                {
                    card.backgroundImage.color = completedColor;
                }
                else if (topicData.progressPercentage > 0)
                {
                    card.backgroundImage.color = inProgressColor;
                }
                else
                {
                    card.backgroundImage.color = lockedColor;
                }
            }
        }
        
        Debug.Log("=== Achievement Cards Update Complete ===");
    }
    
    int GetTotalLessonCount(string topicName)
    {
        string normalizedTopic = TopicNameConstants.Normalize(topicName);
        
        if (topicLessonCounts.ContainsKey(normalizedTopic))
        {
            return topicLessonCounts[normalizedTopic];
        }
        
        Debug.LogWarning($"⚠️ No lesson count for {topicName}, returning 0");
        return 0;
    }
    
    void UpdateStreakDisplay()
    {
        if (cachedProgressData == null)
        {
            Debug.LogWarning("⚠️ No cached data");
            return;
        }
        
        if (streakDotsContainer == null)
        {
            Debug.LogError("❌ StreakDotsContainer is NULL!");
            return;
        }
        
        if (streakDotPrefab == null)
        {
            Debug.LogError("❌ StreakDotPrefab is NULL!");
            return;
        }
        
        int streak = cachedProgressData.streak;
        Debug.Log($"=== Updating Streak Display: {streak} days ===");
        
        if (streakDaysText != null)
        {
            streakDaysText.text = $"{streak}";
        }
        
        if (streakMessageText != null)
        {
            string message;
            if (streak == 0)
                message = "Start your streak today!";
            else if (streak == 1)
                message = "Great start! Keep it going!";
            else if (streak < 7)
                message = $"Nice! {streak} days in a row!";
            else if (streak < 30)
                message = $"Awesome! {streak} day streak!";
            else
                message = $"Amazing! {streak} days! 🔥";
            
            streakMessageText.text = message;
        }
        
        List<GameObject> dotsToDestroy = new List<GameObject>();
        foreach (Transform child in streakDotsContainer)
        {
            dotsToDestroy.Add(child.gameObject);
        }
        
        foreach (GameObject dot in dotsToDestroy)
        {
            Destroy(dot);
        }
        
        StartCoroutine(CreateActivityDotsNextFrame(streak));
    }
    
    IEnumerator CreateActivityDotsNextFrame(int streak)
    {
        yield return null;
        
        Debug.Log("=== Creating 7-Day Activity Dots ===");
        
        for (int i = 0; i < 7; i++)
        {
            GameObject dot = Instantiate(streakDotPrefab, streakDotsContainer);
            dot.name = $"ActivityDot_{i}";
            
            bool isDayActive = i < streak;
            Color targetColor = isDayActive ? streakActiveColor : streakInactiveColor;
            
            Image dotImage = dot.GetComponent<Image>();
            
            if (dotImage == null)
            {
                dotImage = dot.GetComponentInChildren<Image>(true);
            }
            
            if (dotImage != null)
            {
                dotImage.color = targetColor;
                dotImage.enabled = true;
            }
            
            TextMeshProUGUI label = dot.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = "";
                label.gameObject.SetActive(false);
            }
            
            RectTransform dotRect = dot.GetComponent<RectTransform>();
            if (dotRect != null)
            {
                dotRect.anchorMin = new Vector2(0.5f, 0.5f);
                dotRect.anchorMax = new Vector2(0.5f, 0.5f);
                dotRect.pivot = new Vector2(0.5f, 0.5f);
            }
            
            dot.SetActive(true);
            
            Debug.Log($"  Dot {i}: {(isDayActive ? "ACTIVE ✓" : "inactive")}");
        }
        
        Debug.Log($"✅ Created 7 activity dots (showing {Mathf.Min(streak, 7)} active)");
        
        yield return null;
        Canvas.ForceUpdateCanvases();
        
        if (streakDotsContainer is RectTransform containerRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }
        
        Debug.Log("=== Activity Dots Display Complete ===");
    }
    
    public void RefreshProgress()
    {
        StartCoroutine(FetchAndDisplayProgress());
    }
    
    public static string GetAchievementTitle(string topicName, float progress)
    {
        string normalized = TopicNameConstants.Normalize(topicName);
        
        if (progress >= 100f)
        {
            switch (normalized)
            {
                case TopicNameConstants.ARRAYS:
                    return "Array Master";
                case TopicNameConstants.QUEUE:
                    return "Queue Master";
                case TopicNameConstants.STACKS:
                    return "First Stack";
                case TopicNameConstants.LINKED_LISTS:
                    return "Link Expert";
                case TopicNameConstants.TREES:
                    return "Tree Walker";
                case TopicNameConstants.GRAPHS:
                    return "Graph Pro";
                default:
                    return normalized;
            }
        }
        else if (progress > 0)
        {
            return normalized + " (In Progress)";
        }
        else
        {
            return normalized;
        }
    }
}

public class AchievementCardButton : MonoBehaviour
{
    public string topicName;
    
    public void OnCardClicked()
    {
        Debug.Log($"Achievement card clicked: {topicName}");
        
        if (UserProgressManager.Instance != null)
        {
            bool isUnlocked = UserProgressManager.Instance.IsTopicUnlocked(topicName);
            
            if (isUnlocked)
            {
                PlayerPrefs.SetString("SelectedTopic", topicName);
            }
            else
            {
                Debug.Log("Topic is locked!");
            }
        }
    }
}