using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Linq;

/// <summary>
/// CRITICAL: Standardize all topic names across the application
/// Updated to include 10 topics total (6 original + 4 new)
/// </summary>
public static class TopicNameConstants
{
    // Original 6 topics
    public const string ARRAYS = "Arrays";
    public const string QUEUE = "Queue";
    public const string STACKS = "Stacks";
    public const string LINKED_LISTS = "LinkedLists";
    public const string TREES = "Trees";
    public const string GRAPHS = "Graphs";
    
    // NEW 4 topics - using consistent capitalization
    public const string HASHMAPS = "Hashmaps";
    public const string HEAPS = "Heaps";
    public const string DEQUE = "Deque";
    public const string BINARY_HEAPS = "BinaryHeaps";


    
    public static readonly List<string> ALL_TOPICS = new List<string>
    {
        ARRAYS,
        QUEUE,
        STACKS,
        LINKED_LISTS,
        TREES,
        GRAPHS,
        HASHMAPS,
        HEAPS,
        DEQUE,
        BINARY_HEAPS
    };
    
    public static string Normalize(string topicName)
    {
        if (string.IsNullOrEmpty(topicName))
            return "";
        
        string cleaned = topicName.Trim();
        
        switch (cleaned.ToLower())
        {
            case "arrays":
            case "array":
                return ARRAYS;

            case "queues":
            case "queue":
                return QUEUE;
                
            case "stacks":
            case "stack":
                return STACKS;
                
            case "linkedlists":
            case "linked lists":
            case "linkedlist":
            case "linked list":
                return LINKED_LISTS;
                
            case "trees":
            case "tree":
                return TREES;
                
            case "graphs":
            case "graph":
                return GRAPHS;
                
            case "hashmaps":
            case "hashmap":
            case "hash maps":
            case "hash map":
                return HASHMAPS;
                
            case "heaps":
            case "heap":
                return HEAPS;
                
            case "deque":
            case "deques":
            case "double ended queue":
                return DEQUE;
                
            case "binaryheaps":
            case "binary heaps":
            case "binaryheap":
            case "binary heap":
                return BINARY_HEAPS;
                
            default:
                Debug.LogWarning($"⚠️ Unknown topic name: {topicName}");
                return cleaned;
        }
    }
    
    /// <summary>
    /// Gets the display name for a topic (properly formatted)
    /// </summary>
    public static string GetDisplayName(string topicName)
    {
        string normalized = Normalize(topicName);
        
        switch (normalized)
        {
            case ARRAYS: return "Arrays";
            case QUEUE: return "Queue";
            case STACKS: return "Stacks";
            case LINKED_LISTS: return "Linked Lists";
            case TREES: return "Trees";
            case GRAPHS: return "Graphs";
            case HASHMAPS: return "Hashmaps";
            case HEAPS: return "Heaps";
            case DEQUE: return "Deque";
            case BINARY_HEAPS: return "Binary Heaps";
            default: return topicName;
        }
    }
}

/// <summary>
/// Complete User Progress Manager with CONTINUOUS TIME TRACKING
/// Tracks all student progress locally and syncs to database
/// NOW SUPPORTS 10 TOPICS WITH FIXED EMAIL HANDLING
/// </summary>
public class UserProgressManager : MonoBehaviour
{
    public static UserProgressManager Instance { get; private set; }
    
    [System.Serializable]
    public class TopicProgress
    {
        public string topicName;
        public bool tutorialCompleted;
        public bool puzzleCompleted;
        public int puzzleScore;
        public DateTime lastAccessed;
        public float timeSpent;
        public int lessonsCompleted;
        public DifficultyScoresData difficultyScores; 
        
  public bool IsCompleted()
    {
        return puzzleCompleted && lessonsCompleted > 0;
    }
    
 public float GetCompletionPercentage()
{
    float lessonProgress = 0f;
    float puzzleProgress = 0f;

    int totalLessons = UserProgressManager.Instance?.GetTotalLessonsForTopic(topicName) ?? 0;

    if (lessonsCompleted > 0)
    {
        if (totalLessons <= 0 || lessonsCompleted >= totalLessons)
        {
            // ✅ If total is unknown but we have completions, OR all done → full 50%
            lessonProgress = 50f;
        }
        else
        {
            lessonProgress = Mathf.Min(50f, (lessonsCompleted / (float)totalLessons) * 50f);
        }
    }

    // Puzzles = 50% (12.5% each difficulty)
    if (difficultyScores != null)
    {
        if (difficultyScores.easy   > 0) puzzleProgress += 12.5f;
        if (difficultyScores.medium > 0) puzzleProgress += 12.5f;
        if (difficultyScores.hard   > 0) puzzleProgress += 12.5f;
        if (difficultyScores.mixed  > 0) puzzleProgress += 12.5f;
    }

    return Mathf.Min(100f, lessonProgress + puzzleProgress);
}

}
    
    [Header("Database Settings (Optional)")]
    [Tooltip("Leave empty to use local-only mode")]
    public string adminApiUrl = "https://structureality-admin.onrender.com/api";
    [Tooltip("Enable to sync progress to database automatically")]
    public bool autoSync = true;
    
    [Header("Time Tracking Settings")]
    [Tooltip("How often to update time tracking (seconds)")]
    public float timeTrackingInterval = 10f;
    [Tooltip("How often to sync to database (seconds)")]
    public float syncInterval = 60f;
    
    private string currentUsername;
    private string currentUserEmail;
    private Dictionary<string, TopicProgress> userProgress;
    private Dictionary<string, int> topicTotalLessons = new Dictionary<string, int>();
    private float sessionStartTime;
    private string currentTopic;
    private bool isInitialized = false;
    private float applicationStartTime;
    private float lastTimeUpdate;
    private float lastSyncTime;
    private float timeSinceLastSync;
    
    public List<string> topicOrder = TopicNameConstants.ALL_TOPICS;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            applicationStartTime = Time.time;
            lastTimeUpdate = Time.time;
            lastSyncTime = Time.time;
            timeSinceLastSync = 0f;
            sessionStartTime = Time.time;
            
            string storedUser = PlayerPrefs.GetString("CurrentUser", "");
            if (!string.IsNullOrEmpty(storedUser))
            {
                Debug.Log($"✓ Found stored user: {storedUser}, loading progress...");
                LoadUserProgress();
            }
            else
            {
                Debug.Log("✓ UserProgressManager initialized - waiting for login");
                userProgress = new Dictionary<string, TopicProgress>();
            }
            
            Debug.Log($"✓ Auto Sync: {autoSync}, API URL: {adminApiUrl}");
            Debug.Log($"⏱️ Time tracking enabled - Update interval: {timeTrackingInterval}s, Sync interval: {syncInterval}s");
            Debug.Log($"📚 Total Topics: {TopicNameConstants.ALL_TOPICS.Count}");
            
            StartCoroutine(ContinuousTimeTracking());
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private IEnumerator ContinuousTimeTracking()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeTrackingInterval);
            
            if (!string.IsNullOrEmpty(currentUsername) && isInitialized && userProgress.Count > 0)
            {
                float deltaTime = Time.time - lastTimeUpdate;
                timeSinceLastSync += deltaTime;
                lastTimeUpdate = Time.time;
                
                string topicToTrack = GetActiveOrRecentTopic();
                
                if (!string.IsNullOrEmpty(topicToTrack))
                {
                    AddTimeSpent(topicToTrack, deltaTime);
                    Debug.Log($"⏱️ Tracked {deltaTime:F1}s → {topicToTrack} (Total: {GetTimeSpent(topicToTrack):F1}s)");
                }
                
                if (timeSinceLastSync >= syncInterval && autoSync && !string.IsNullOrEmpty(adminApiUrl))
                {
                    Debug.Log($"🔄 Auto-syncing time data ({timeSinceLastSync:F1}s accumulated)");
                    StartCoroutine(SyncToDatabase());
                    timeSinceLastSync = 0f;
                }
            }
            else
            {
                lastTimeUpdate = Time.time;
            }
        }
    }
    
    private string GetActiveOrRecentTopic()
    {
        if (!string.IsNullOrEmpty(currentTopic))
        {
            return currentTopic;
        }
        
        if (userProgress.Count > 0)
        {
            var mostRecent = userProgress.Values
                .OrderByDescending(p => p.lastAccessed)
                .FirstOrDefault();
            
            if (mostRecent != null)
            {
                return mostRecent.topicName;
            }
        }
        
        return "";
    }

    public void SetTotalLessonsForTopic(string topicName, int total)
{
    topicName = TopicNameConstants.Normalize(topicName);
    topicTotalLessons[topicName] = total;
}

public int GetTotalLessonsForTopic(string topicName)
{
    topicName = TopicNameConstants.Normalize(topicName);
    return topicTotalLessons.ContainsKey(topicName) ? topicTotalLessons[topicName] : 0;
}
    
    public float GetTotalSessionTime()
    {
        return Time.time - applicationStartTime;
    }
    
    public float GetTotalTrackedTime()
    {
        float total = 0f;
        foreach (var progress in userProgress.Values)
        {
            total += progress.timeSpent;
        }
        return total;
    }
    
    public string GetFormattedTotalTime()
    {
        float totalTime = GetTotalTrackedTime();
        int hours = Mathf.FloorToInt(totalTime / 3600f);
        int minutes = Mathf.FloorToInt((totalTime % 3600f) / 60f);
        
        if (hours > 0)
        {
            return $"{hours}h {minutes}m";
        }
        return $"{minutes}m";
    }
    
public void LoadUserProgress()
{
    currentUsername = PlayerPrefs.GetString("CurrentUser", "");
    userProgress = new Dictionary<string, TopicProgress>();
    
    if (string.IsNullOrEmpty(currentUsername))
    {
        Debug.LogWarning("No user logged in");
        isInitialized = false;
        return;
    }
    
    currentUserEmail = PlayerPrefs.GetString("User_" + currentUsername + "_Email", "");
    
    Debug.Log($"✓ Current User - Username: {currentUsername}, Email: {currentUserEmail}");
    Debug.Log($"📚 Loading progress for {TopicNameConstants.ALL_TOPICS.Count} topics...");
    
    bool shouldFetchFromDB = !PlayerPrefs.HasKey($"UserProgressInitialized_{currentUsername}");
    
    if (shouldFetchFromDB)
    {
        Debug.Log("🔄 Fresh login detected - will fetch from database");
        foreach (string topic in TopicNameConstants.ALL_TOPICS)
        {
            userProgress[topic] = new TopicProgress { 
                topicName = topic, 
                lastAccessed = DateTime.Now,
                difficultyScores = new DifficultyScoresData() // ✅ Initialize
            };
        }
        
        PlayerPrefs.SetInt($"UserProgressInitialized_{currentUsername}", 1);
        PlayerPrefs.Save();
        
        isInitialized = true;
        
        if (autoSync && !string.IsNullOrEmpty(adminApiUrl))
        {
            StartCoroutine(FetchAndMergeFromDatabase());
        }
        return;
    }
    
    foreach (string topic in TopicNameConstants.ALL_TOPICS)
    {
        // ✅ FIX: Define legacyTopic here
        string legacyTopic = topic == TopicNameConstants.QUEUE ? "Queues" : topic;
        
        TopicProgress progress = new TopicProgress
        {
            topicName = topic,
            tutorialCompleted = PlayerPrefs.GetInt($"{currentUsername}_{topic}_Tutorial", 0) == 1 ||
                               PlayerPrefs.GetInt($"{currentUsername}_{legacyTopic}_Tutorial", 0) == 1,
            puzzleCompleted = PlayerPrefs.GetInt($"{currentUsername}_{topic}_Puzzle", 0) == 1 ||
                             PlayerPrefs.GetInt($"{currentUsername}_{legacyTopic}_Puzzle", 0) == 1,
            puzzleScore = Mathf.Max(
                PlayerPrefs.GetInt($"{currentUsername}_{topic}_Score", 0),
                PlayerPrefs.GetInt($"{currentUsername}_{legacyTopic}_Score", 0)
            ),
            timeSpent = PlayerPrefs.GetFloat($"{currentUsername}_{topic}_TimeSpent", 0f) +
                       PlayerPrefs.GetFloat($"{currentUsername}_{legacyTopic}_TimeSpent", 0f),
            lessonsCompleted = PlayerPrefs.GetInt($"{currentUsername}_{topic}_LessonsCompleted", 0),
            // ✅ NEW: Load difficulty scores
            difficultyScores = new DifficultyScoresData
            {
                easy = PlayerPrefs.GetInt($"User_{currentUsername}_{topic}_easy_Score", 0),
                medium = PlayerPrefs.GetInt($"User_{currentUsername}_{topic}_medium_Score", 0),
                hard = PlayerPrefs.GetInt($"User_{currentUsername}_{topic}_hard_Score", 0),
                mixed = PlayerPrefs.GetInt($"User_{currentUsername}_{topic}_mixed_Score", 0)
            }
        };
        
        string lastAccessedStr = PlayerPrefs.GetString($"{currentUsername}_{topic}_LastAccessed", "");
        if (string.IsNullOrEmpty(lastAccessedStr))
        {
            lastAccessedStr = PlayerPrefs.GetString($"{currentUsername}_{legacyTopic}_LastAccessed", "");
        }
        
        if (!string.IsNullOrEmpty(lastAccessedStr))
        {
            try
            {
                progress.lastAccessed = DateTime.Parse(lastAccessedStr);
            }
            catch
            {
                progress.lastAccessed = DateTime.Now;
            }
        }
        else
        {
            progress.lastAccessed = DateTime.Now;
        }
        
        userProgress[topic] = progress;
        
        // Migration cleanup for old "Queues" data
        if (topic == TopicNameConstants.QUEUE && PlayerPrefs.HasKey($"{currentUsername}_Queues_Score"))
        {
            Debug.Log("🔄 Migrating old 'Queues' data to 'Queue'");
            SaveProgress(topic);
            
            PlayerPrefs.DeleteKey($"{currentUsername}_Queues_Tutorial");
            PlayerPrefs.DeleteKey($"{currentUsername}_Queues_Puzzle");
            PlayerPrefs.DeleteKey($"{currentUsername}_Queues_Score");
            PlayerPrefs.DeleteKey($"{currentUsername}_Queues_TimeSpent");
            PlayerPrefs.DeleteKey($"{currentUsername}_Queues_LastAccessed");
            
            Debug.Log("✓ Migration complete - old 'Queues' keys removed");
        }
    }
    
    PlayerPrefs.Save();
    isInitialized = true;
    
    float totalTime = GetTotalTrackedTime();
    Debug.Log($"✓ Progress loaded for: {currentUsername} (Total time: {totalTime:F1}s / {totalTime/60:F1}m)");
    Debug.Log($"✓ Loaded {userProgress.Count} topics successfully");
    
    if (autoSync && !string.IsNullOrEmpty(adminApiUrl))
    {
        StartCoroutine(FetchAndMergeFromDatabase());
    }
}

    void UpdateStreak()
    {
        string lastActivityStr = PlayerPrefs.GetString($"User_{currentUsername}_LastActivity", "");
        DateTime now = DateTime.Now;
        
        if (string.IsNullOrEmpty(lastActivityStr))
        {
            PlayerPrefs.SetInt($"User_{currentUsername}_Streak", 1);
            PlayerPrefs.SetString($"User_{currentUsername}_LastActivity", now.ToString("o"));
            Debug.Log($"🔥 First activity! Streak started: 1");
        }
        else
        {
            try
            {
                DateTime lastActivity = DateTime.Parse(lastActivityStr);
                DateTime today = now.Date;
                DateTime lastDate = lastActivity.Date;
                
                double daysDiff = (today - lastDate).TotalDays;
                int currentStreak = PlayerPrefs.GetInt($"User_{currentUsername}_Streak", 0);
                
                if (daysDiff == 1)
                {
                    currentStreak++;
                    PlayerPrefs.SetInt($"User_{currentUsername}_Streak", currentStreak);
                    PlayerPrefs.SetString($"User_{currentUsername}_LastActivity", now.ToString("o"));
                    Debug.Log($"🔥 Streak continued! New streak: {currentStreak}");
                }
                else if (daysDiff == 0)
                {
                    Debug.Log($"✓ Activity already recorded for today. Streak: {currentStreak}");
                    PlayerPrefs.SetString($"User_{currentUsername}_LastActivity", now.ToString("o"));
                }
                else
                {
                    Debug.Log($"❌ Streak broken! Resetting to 1");
                    PlayerPrefs.SetInt($"User_{currentUsername}_Streak", 1);
                    PlayerPrefs.SetString($"User_{currentUsername}_LastActivity", now.ToString("o"));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing last activity date: {e.Message}");
                PlayerPrefs.SetInt($"User_{currentUsername}_Streak", 1);
                PlayerPrefs.SetString($"User_{currentUsername}_LastActivity", now.ToString("o"));
            }
        }
        
        PlayerPrefs.Save();
    }

    IEnumerator FetchAndMergeFromDatabase()
    {
        string url = $"{adminApiUrl}/progress/{currentUsername}";
        Debug.Log($"🔄 Fetching progress from database: {url}");
        
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                DatabaseProgressResponse response = JsonUtility.FromJson<DatabaseProgressResponse>(request.downloadHandler.text);

                if (response != null && response.success && response.data != null)
                {
                    Debug.Log("✅ Database progress fetched successfully");
                    
                    foreach (var topicData in response.data.topics)
                    {
                        string normalizedTopic = TopicNameConstants.Normalize(topicData.topicName);
                        
                        if (userProgress.ContainsKey(normalizedTopic))
                        {
                            userProgress[normalizedTopic].tutorialCompleted = topicData.tutorialCompleted;
                            userProgress[normalizedTopic].puzzleCompleted = topicData.puzzleCompleted;
                            userProgress[normalizedTopic].puzzleScore = topicData.puzzleScore;
                            userProgress[normalizedTopic].lessonsCompleted = topicData.lessonsCompleted;
                            userProgress[normalizedTopic].timeSpent = topicData.timeSpent;
                            
                            try
                            {
                                userProgress[normalizedTopic].lastAccessed = DateTime.Parse(topicData.lastAccessed);
                            }
                            catch
                            {
                                userProgress[normalizedTopic].lastAccessed = DateTime.Now;
                            }
                            
                            SaveProgress(normalizedTopic);
                            
                            Debug.Log($"✓ Loaded {normalizedTopic}: {topicData.lessonsCompleted} lessons, {topicData.timeSpent:F1}s");
                        }
                    }
                    
                    PlayerPrefs.Save();
                    Debug.Log($"✅ Database merge complete - Total tracked time: {GetFormattedTotalTime()}");
                }
                else
                {
                    Debug.Log("⚠️ No progress found in database - starting fresh");
                    ClearAllLocalProgress();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ Failed to parse database data: {e.Message}");
                ClearAllLocalProgress();
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ Database fetch failed: {request.error}");
            ClearAllLocalProgress();
        }
    }
    
    void ClearAllLocalProgress()
    {
        Debug.Log("🧹 Clearing all local progress data");
        
        foreach (string topic in TopicNameConstants.ALL_TOPICS)
        {
            if (userProgress.ContainsKey(topic))
            {
                userProgress[topic] = new TopicProgress { topicName = topic, lastAccessed = DateTime.Now };
            }
            
            PlayerPrefs.DeleteKey($"{currentUsername}_{topic}_Tutorial");
            PlayerPrefs.DeleteKey($"{currentUsername}_{topic}_Puzzle");
            PlayerPrefs.DeleteKey($"{currentUsername}_{topic}_Score");
            PlayerPrefs.DeleteKey($"{currentUsername}_{topic}_TimeSpent");
            PlayerPrefs.DeleteKey($"{currentUsername}_{topic}_LastAccessed");
            PlayerPrefs.DeleteKey($"{currentUsername}_{topic}_LessonsCompleted");
        }
        
        PlayerPrefs.Save();
        Debug.Log("✓ Local progress cleared");
    }
    
    public void InitializeForUser(string username)
    {
        currentUsername = username;
        currentUserEmail = PlayerPrefs.GetString($"User_{username}_Email", "");
        
        PlayerPrefs.DeleteKey($"UserProgressInitialized_{username}");
        PlayerPrefs.Save();
        
        lastTimeUpdate = Time.time;
        lastSyncTime = Time.time;
        timeSinceLastSync = 0f;
        
        LoadUserProgress();
        Debug.Log($"✓ UserProgressManager initialized for: {username}");
        Debug.Log($"⏱️ Time tracking started");
    }
    
    public void StartTopicSession(string topicName)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("⚠️ UserProgressManager not initialized - no user logged in");
            return;
        }
        
        currentTopic = TopicNameConstants.Normalize(topicName);
        sessionStartTime = Time.time;
        lastTimeUpdate = Time.time;
        PlayerPrefs.SetString("CurrentTopic", currentTopic);
        Debug.Log($"⏱️ Started session for: {currentTopic} (normalized from: {topicName})");
    }
    
    public void EndTopicSession()
    {
        if (string.IsNullOrEmpty(currentTopic)) return;
        
        float sessionTime = Time.time - sessionStartTime;
        AddTimeSpent(currentTopic, sessionTime);
        
        Debug.Log($"⏱️ Ended session for {currentTopic}. Time: {sessionTime:F1}s");
        
        if (autoSync && !string.IsNullOrEmpty(adminApiUrl))
        {
            StartCoroutine(SyncToDatabase());
        }
        
        currentTopic = "";
    }
    
    public void CompleteTutorial(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        
        if (string.IsNullOrEmpty(currentUsername)) 
        {
            Debug.LogError("❌ No user logged in!");
            return;
        }
        
        if (!userProgress.ContainsKey(topicName))
        {
            userProgress[topicName] = new TopicProgress { topicName = topicName, lastAccessed = DateTime.Now };
        }
        
        userProgress[topicName].tutorialCompleted = true;
        userProgress[topicName].lastAccessed = DateTime.Now;
        
        SaveProgress(topicName);
        UpdateOverallProgress();
        
        Debug.Log($"✓ Tutorial completed for {topicName}");
        
        if (autoSync && !string.IsNullOrEmpty(adminApiUrl))
        {
            StartCoroutine(SyncToDatabase());
        }
    }
    
public void CompletePuzzle(string topicName, int score)
{
    topicName = TopicNameConstants.Normalize(topicName);
    
    if (string.IsNullOrEmpty(currentUsername)) 
    {
        Debug.LogError("❌ No user logged in!");
        return;
    }
    
    if (!userProgress.ContainsKey(topicName))
    {
        userProgress[topicName] = new TopicProgress { 
            topicName = topicName, 
            lastAccessed = DateTime.Now,
            difficultyScores = new DifficultyScoresData()
        };
    }
    
    // Update overall puzzle score (highest score across all difficulties)
    if (score > userProgress[topicName].puzzleScore)
    {
        userProgress[topicName].puzzleScore = score;
    }
    
    // Mark as puzzle completed if score is high enough
    userProgress[topicName].puzzleCompleted = true;
    userProgress[topicName].lastAccessed = DateTime.Now;
    
    SaveProgress(topicName);
    UpdateOverallProgress();
    
    Debug.Log($"✓ Puzzle completed for {topicName} with score: {score}%");
    
  
}
    
    public void UpdateLessonCount(string topicName, int lessonCount)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        
        if (string.IsNullOrEmpty(currentUsername)) 
        {
            Debug.LogError("❌ No user logged in!");
            return;
        }
        
        if (!userProgress.ContainsKey(topicName))
        {
            userProgress[topicName] = new TopicProgress { topicName = topicName, lastAccessed = DateTime.Now };
        }
        
        userProgress[topicName].lessonsCompleted = lessonCount;
        userProgress[topicName].lastAccessed = DateTime.Now;
        
        SaveProgress(topicName);
        
        Debug.Log($"✓ Lesson count updated for {topicName}: {lessonCount}");
        
        if (autoSync && !string.IsNullOrEmpty(adminApiUrl))
        {
            StartCoroutine(SyncToDatabase());
        }
    }
    
    public void AddTimeSpent(string topicName, float seconds)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        
        if (string.IsNullOrEmpty(currentUsername)) return;
        
        if (!userProgress.ContainsKey(topicName))
        {
            userProgress[topicName] = new TopicProgress { topicName = topicName, lastAccessed = DateTime.Now };
        }
        
        userProgress[topicName].timeSpent += seconds;
        userProgress[topicName].lastAccessed = DateTime.Now;
        SaveProgress(topicName);
    }
    
    void SaveProgress(string topicName)
    {
        if (!userProgress.ContainsKey(topicName)) return;
        
        TopicProgress progress = userProgress[topicName];
        
        PlayerPrefs.SetInt($"{currentUsername}_{topicName}_Tutorial", progress.tutorialCompleted ? 1 : 0);
        PlayerPrefs.SetInt($"{currentUsername}_{topicName}_Puzzle", progress.puzzleCompleted ? 1 : 0);
        PlayerPrefs.SetInt($"{currentUsername}_{topicName}_Score", progress.puzzleScore);
        PlayerPrefs.SetFloat($"{currentUsername}_{topicName}_TimeSpent", progress.timeSpent);
        PlayerPrefs.SetString($"{currentUsername}_{topicName}_LastAccessed", progress.lastAccessed.ToString());
        PlayerPrefs.SetInt($"{currentUsername}_{topicName}_LessonsCompleted", progress.lessonsCompleted);
        PlayerPrefs.Save();
    }
    
    void UpdateOverallProgress()
    {
        int totalCompleted = 0;
        foreach (var progress in userProgress.Values)
        {
            if (progress.IsCompleted()) totalCompleted++;
        }
        
        PlayerPrefs.SetInt($"User_{currentUsername}_CompletedTopics", totalCompleted);
        UpdateStreak();
        PlayerPrefs.Save();
    }
    
    IEnumerator SyncToDatabase()
    {
        if (string.IsNullOrEmpty(currentUsername) || string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.LogWarning("⚠️ Cannot sync: username or API URL is empty");
            yield break;
        }
        
        Debug.Log($"🔄 Syncing progress for: {currentUsername}");
        
        string userName = PlayerPrefs.GetString("User_" + currentUsername + "_Name", currentUsername);
        string userEmail = PlayerPrefs.GetString("User_" + currentUsername + "_Email", "");
        
        if (string.IsNullOrEmpty(userEmail) || 
            userEmail == currentUsername || 
            !userEmail.Contains("@"))
        {
            Debug.LogWarning($"⚠️ Invalid or missing email for {currentUsername}, will not sync email field");
            userEmail = "";
        }
        
        Debug.Log($"📧 Sync data - Name: '{userName}', Email: '{userEmail}'");
        
        UserProgressData progressData = new UserProgressData
        {
            username = currentUsername,
            name = userName,
            email = userEmail,
            streak = PlayerPrefs.GetInt($"User_{currentUsername}_Streak", 0),
            completedTopics = PlayerPrefs.GetInt("User_" + currentUsername + "_CompletedTopics", 0),
            lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            topics = new List<UserTopicData>()
        };
        
        float totalTime = 0f;
        foreach (var kvp in userProgress)
    {
        progressData.topics.Add(new UserTopicData
        {
            topicName = kvp.Value.topicName,
            tutorialCompleted = kvp.Value.tutorialCompleted,
            puzzleCompleted = kvp.Value.puzzleCompleted,
            puzzleScore = kvp.Value.puzzleScore,
            progressPercentage = kvp.Value.GetCompletionPercentage(),
            lastAccessed = kvp.Value.lastAccessed.ToString("yyyy-MM-dd HH:mm:ss"),
            timeSpent = kvp.Value.timeSpent,
            lessonsCompleted = kvp.Value.lessonsCompleted,
            difficultyScores = kvp.Value.difficultyScores // ✅ NEW
        });
    }
        
        Debug.Log($"📊 Syncing {progressData.topics.Count} topics - Total time: {totalTime:F1}s ({totalTime/60:F1}m)");
        
        string jsonData = JsonUtility.ToJson(progressData, true);
        
        UnityWebRequest request = new UnityWebRequest(adminApiUrl + "/progress/" + currentUsername, "PUT");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Progress synced to database successfully!");
        }
        else
        {
            Debug.LogError($"❌ Database sync failed: {request.error}");
            Debug.LogError($"❌ Response: {request.downloadHandler.text}");
        }
    }
    
    void Start()
    {
        if (autoSync && !string.IsNullOrEmpty(adminApiUrl))
        {
            InvokeRepeating("PeriodicSync", 300f, 300f);
        }
    }
    
    void PeriodicSync()
    {
        if (!string.IsNullOrEmpty(currentUsername))
        {
            Debug.Log("🔄 Periodic sync (5min) triggered");
            StartCoroutine(SyncToDatabase());
        }
    }
    
    public float GetOverallProgress()
    {
        if (userProgress.Count == 0) return 0f;
        float total = 0f;
        foreach (var p in userProgress.Values) total += p.GetCompletionPercentage();
        return total / userProgress.Count;
    }
    
    public float GetTopicProgress(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        return userProgress.ContainsKey(topicName) ? userProgress[topicName].GetCompletionPercentage() : 0f;
    }
    
    public bool IsTutorialCompleted(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        return userProgress.ContainsKey(topicName) && userProgress[topicName].tutorialCompleted;
    }
    
    public bool IsPuzzleCompleted(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        return userProgress.ContainsKey(topicName) && userProgress[topicName].puzzleCompleted;
    }
    
    public int GetPuzzleScore(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        return userProgress.ContainsKey(topicName) ? userProgress[topicName].puzzleScore : 0;
    }
    
    public float GetTimeSpent(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        return userProgress.ContainsKey(topicName) ? userProgress[topicName].timeSpent : 0f;
    }
    
    public int GetLessonCount(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        return userProgress.ContainsKey(topicName) ? userProgress[topicName].lessonsCompleted : 0;
    }
    
    public bool IsTopicUnlocked(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        
        if (topicOrder.Count == 0 || topicName == topicOrder[0]) return true;
        int index = topicOrder.IndexOf(topicName);
        if (index <= 0) return true;
        return GetTopicProgress(topicOrder[index - 1]) >= 50f;
    }
    
    public string GetCurrentUsername() => currentUsername;
    
    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(currentUsername) ? "Student" : 
            PlayerPrefs.GetString($"User_{currentUsername}_Name", currentUsername);
    }
        
    public int GetStreak()
    {
        return PlayerPrefs.GetInt($"User_{currentUsername}_Streak", 0);
    }
    
    public void ManualSync()
    {
        if (!string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.Log("🔄 Manual sync triggered");
            StartCoroutine(SyncToDatabase());
        }
        else
        {
            Debug.LogWarning("⚠️ Cannot sync: API URL is empty");
        }
    }
    
    public void ClearUserData(string userIdentifier)
{
    Debug.Log($"🧹 UserProgressManager: Clearing data for {userIdentifier}");
    
    PlayerPrefs.DeleteKey($"UserProgressInitialized_{userIdentifier}");
    
    // ✅ ADD: Clear all completion flags
    foreach (string topic in TopicNameConstants.ALL_TOPICS)
    {
        PlayerPrefs.DeleteKey($"{userIdentifier}_{topic}_Tutorial");
        PlayerPrefs.DeleteKey($"{userIdentifier}_{topic}_Puzzle");
        PlayerPrefs.DeleteKey($"{userIdentifier}_{topic}_Score");
        PlayerPrefs.DeleteKey($"{userIdentifier}_{topic}_TimeSpent");
        PlayerPrefs.DeleteKey($"{userIdentifier}_{topic}_LastAccessed");
        PlayerPrefs.DeleteKey($"{userIdentifier}_{topic}_LessonsCompleted");
        
        // ✅ CRITICAL: Clear the "reading complete" flags
        PlayerPrefs.DeleteKey($"TopicReadComplete_{userIdentifier}_{topic}");
    }
    
    // ✅ ALSO: Clear last topic
    PlayerPrefs.DeleteKey($"User_{userIdentifier}_LastTopic");
    
    PlayerPrefs.DeleteKey($"User_{userIdentifier}_CompletedTopics");
    PlayerPrefs.DeleteKey($"User_{userIdentifier}_Streak");
    PlayerPrefs.DeleteKey($"User_{userIdentifier}_LastActivity");
    
    PlayerPrefs.Save();
    
    Debug.Log($"✅ UserProgressManager data cleared for {userIdentifier}");
    
    if (userIdentifier == currentUsername)
    {
        userProgress.Clear();
        currentUsername = "";
        currentUserEmail = "";
        isInitialized = false;
        Debug.Log("✅ Internal state reset");
    }
}

    public void CompleteARExploration(string topicName)
    {
        topicName = TopicNameConstants.Normalize(topicName);
        
        if (string.IsNullOrEmpty(currentUsername)) 
        {
            Debug.LogError("❌ No user logged in!");
            return;
        }
        
        if (!userProgress.ContainsKey(topicName))
        {
            userProgress[topicName] = new TopicProgress { topicName = topicName, lastAccessed = DateTime.Now };
        }
        
        TopicProgress progress = userProgress[topicName];
        progress.lastAccessed = DateTime.Now;
        
        SaveProgress(topicName);
        UpdateOverallProgress();
        
        Debug.Log($"✓ AR Exploration completed for {topicName}");
        
        if (autoSync && !string.IsNullOrEmpty(adminApiUrl))
        {
            StartCoroutine(SyncToDatabase());
        }
    }
    
    void OnApplicationQuit()
    {
        Debug.Log("🔄 Application quitting - performing final sync...");
        
        EndTopicSession();
        
        if (autoSync && !string.IsNullOrEmpty(adminApiUrl))
        {
            SyncToDatabase_Synchronous();
        }
    }

    void SyncToDatabase_Synchronous()
    {
        if (string.IsNullOrEmpty(currentUsername) || string.IsNullOrEmpty(adminApiUrl))
        {
            Debug.LogWarning("⚠️ Cannot sync: username or API URL is empty");
            return;
        }
        
        Debug.Log($"🔄 FINAL SYNC for: {currentUsername}");
        
        string userName = PlayerPrefs.GetString("User_" + currentUsername + "_Name", currentUsername);
        string userEmail = PlayerPrefs.GetString("User_" + currentUsername + "_Email", "");
        
        if (string.IsNullOrEmpty(userEmail) || 
            userEmail == currentUsername || 
            !userEmail.Contains("@"))
        {
            userEmail = "";
        }
        
        UserProgressData progressData = new UserProgressData
        {
            username = currentUsername,
            name = userName,
            email = userEmail,
            streak = PlayerPrefs.GetInt($"User_{currentUsername}_Streak", 0),
            completedTopics = PlayerPrefs.GetInt("User_" + currentUsername + "_CompletedTopics", 0),
            lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            topics = new List<UserTopicData>()
        };
        
        foreach (var kvp in userProgress)
        {
            progressData.topics.Add(new UserTopicData
            {
                topicName = kvp.Value.topicName,
                tutorialCompleted = kvp.Value.tutorialCompleted,
                puzzleCompleted = kvp.Value.puzzleCompleted,
                puzzleScore = kvp.Value.puzzleScore,
                progressPercentage = kvp.Value.GetCompletionPercentage(),
                lastAccessed = kvp.Value.lastAccessed.ToString("yyyy-MM-dd HH:mm:ss"),
                timeSpent = kvp.Value.timeSpent,
                lessonsCompleted = kvp.Value.lessonsCompleted
            });
        }
        
        string jsonData = JsonUtility.ToJson(progressData, true);
        
        UnityWebRequest request = new UnityWebRequest(adminApiUrl + "/progress/" + currentUsername, "PUT");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 5;
        
        var operation = request.SendWebRequest();
        
        float startTime = Time.realtimeSinceStartup;
        while (!operation.isDone && Time.realtimeSinceStartup - startTime < 5f)
        {
            System.Threading.Thread.Sleep(10);
        }
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ FINAL SYNC COMPLETED!");
        }
        else
        {
            Debug.LogError($"❌ FINAL SYNC FAILED: {request.error}");
        }
        
        request.Dispose();
    }
    
    public void TrackGeneralAppTime(float seconds)
    {
        string topicToTrack = GetActiveOrRecentTopic();
        
        if (!string.IsNullOrEmpty(topicToTrack))
        {
            AddTimeSpent(topicToTrack, seconds);
        }
    }
    
    public void LogTimeTrackingStats()
    {
        Debug.Log("=== TIME TRACKING STATS ===");
        Debug.Log($"User: {currentUsername}");
        Debug.Log($"Current Topic: {currentTopic}");
        Debug.Log($"Session Duration: {GetTotalSessionTime():F1}s ({GetTotalSessionTime()/60:F1}m)");
        Debug.Log($"Total Tracked Time: {GetTotalTrackedTime():F1}s ({GetFormattedTotalTime()})");
        Debug.Log($"Time Since Last Sync: {timeSinceLastSync:F1}s");
        Debug.Log($"Total Topics: {userProgress.Count}");
        
        foreach (var kvp in userProgress)
        {
            float mins = kvp.Value.timeSpent / 60f;
            Debug.Log($"  {kvp.Key}: {kvp.Value.timeSpent:F1}s ({mins:F1}m)");
        }
        Debug.Log("===========================");
    }
}

// NOTE: Database classes should be in DatabaseClasses.cs - NOT HERE to avoid duplicates!