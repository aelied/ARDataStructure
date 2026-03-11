using System.Collections.Generic;

/// <summary>
/// Quizzes Response from API
/// </summary>
[System.Serializable]
public class QuizzesResponse
{
    public bool success;
    public int count;
    public List<QuizData> quizzes;
    public string error;
}

/// <summary>
/// Individual Quiz Data from API
/// </summary>
[System.Serializable]
public class QuizData
{
    public string _id;
    public string topicName;
    public string questionText;
    public string[] answerOptions;
    public int correctAnswerIndex;
    public string explanation;
    public string difficulty;  // ✅ ADDED: Difficulty property
    public int order;
    public string createdAt;
}

/// <summary>
/// Alternative Quiz Question Class (if you're using this instead)
/// </summary>
[System.Serializable]
public class QuizQuestion
{
    public string questionText;
    public string[] answerOptions;
    public int correctAnswerIndex;
    public string explanation;
    public string difficulty;  // ✅ ADDED: Difficulty property
}