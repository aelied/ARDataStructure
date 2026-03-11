// ==================== DatabaseClasses.cs - UPDATED ====================

using System.Collections.Generic;
using System;

// ✅ Difficulty Scores Structure (already exists)
[System.Serializable]
public class DifficultyScoresData
{
    public int easy;
    public int medium;
    public int hard;
    public int mixed;
}

// ... existing database classes ...

// ✅ UPDATED: LessonData to include difficultyLevel
[System.Serializable]
public class LessonData
{
    public string _id;
    public string topicName;
    public string title;
    public string description;
    public string content;
    public int order;
    public string createdAt;
    public string difficultyLevel; // ✅ NEW: "beginner" or "intermediate"
}

// Rest of the existing classes remain the same...
[System.Serializable]
public class DatabaseProgressResponse
{
    public bool success;
    public DatabaseProgressData data;
    public string error;
}

[System.Serializable]
public class DatabaseProgressData
{
    public string username;
    public string name;
    public string email;
    public int streak;
    public int completedTopics;
    public string lastUpdated;
    public string lastActivity;
    public List<DatabaseTopicData> topics;
}

[System.Serializable]
public class DatabaseTopicData
{
    public string topicName;
    public bool tutorialCompleted;
    public bool puzzleCompleted;
    public int score;  
    public int puzzleScore;
    public float progressPercentage;
    public string lastAccessed;
    public float timeSpent;
    public int lessonsCompleted;
    public DifficultyScoresData difficultyScores;
}

[System.Serializable]
public class UserProgressData
{
    public string username;
    public string name;
    public string email;
    public int streak;
    public int completedTopics;
    public string lastUpdated;
    public List<UserTopicData> topics;
}

[System.Serializable]
public class UserTopicData
{
    public string topicName;
    public bool tutorialCompleted;
    public bool puzzleCompleted;
    public int puzzleScore;
    public float progressPercentage;
    public string lastAccessed;
    public float timeSpent;
    public int lessonsCompleted;
    public DifficultyScoresData difficultyScores;
}

[System.Serializable]
public class LessonsResponse
{
    public bool success;
    public int count;
    public List<LessonData> lessons;
    public string error;
}