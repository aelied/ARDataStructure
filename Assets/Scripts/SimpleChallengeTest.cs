using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simplified Challenge Manager for testing - Only requires minimal UI setup
/// Use this to test if the challenge logic works before building the full UI
/// </summary>
public class SimpleChallengeTest : MonoBehaviour
{
    [Header("REQUIRED - Assign These")]
    public GameObject challengePanel;
    public TextMeshProUGUI questionText;
    public Button answer1Button;
    public Button answer2Button;
    public Button answer3Button;
    public Button answer4Button;
    
    [Header("OPTIONAL - For Better Display")]
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI feedbackText;
    public Button nextButton;
    
    private ChallengeManager.TopicChallenge currentChallenge;
    private int currentQuestionIndex = 0;
    private int correctAnswers = 0;
    private float timeRemaining = 30f;
    private bool answered = false;
    
    void Start()
    {
        // Setup button listeners
        if (answer1Button != null) answer1Button.onClick.AddListener(() => CheckAnswer(0));
        if (answer2Button != null) answer2Button.onClick.AddListener(() => CheckAnswer(1));
        if (answer3Button != null) answer3Button.onClick.AddListener(() => CheckAnswer(2));
        if (answer4Button != null) answer4Button.onClick.AddListener(() => CheckAnswer(3));
        if (nextButton != null) nextButton.onClick.AddListener(NextQuestion);
        
        Debug.Log("SimpleChallengeTest initialized - Call StartTest() from LearnPanelController");
    }
    
    public void StartTest(string topicName)
    {
        Debug.Log($"=== Starting Simple Challenge Test for: {topicName} ===");
        
        // Check required components
        if (challengePanel == null)
        {
            Debug.LogError("❌ challengePanel is NOT assigned!");
            return;
        }
        if (questionText == null)
        {
            Debug.LogError("❌ questionText is NOT assigned!");
            return;
        }
        if (answer1Button == null || answer2Button == null)
        {
            Debug.LogError("❌ Answer buttons are NOT assigned!");
            return;
        }
        
        Debug.Log("✓ All required components assigned");
        
        // Load questions for this topic
        currentChallenge = GetTestChallenge(topicName);
        
        if (currentChallenge == null || currentChallenge.questions.Count == 0)
        {
            Debug.LogError($"❌ No questions found for topic: {topicName}");
            return;
        }
        
        Debug.Log($"✓ Loaded {currentChallenge.questions.Count} questions");
        
        // Reset
        currentQuestionIndex = 0;
        correctAnswers = 0;
        
        // Show panel
        challengePanel.SetActive(true);
        Debug.Log("✓ Challenge panel activated");
        
        // Show first question
        ShowQuestion();
    }
    
    void ShowQuestion()
    {
        if (currentQuestionIndex >= currentChallenge.questions.Count)
        {
            ShowResults();
            return;
        }
        
        answered = false;
        timeRemaining = 30f;
        
        var q = currentChallenge.questions[currentQuestionIndex];
        
        Debug.Log($"--- Question {currentQuestionIndex + 1} ---");
        Debug.Log($"Q: {q.questionText}");
        
        // Update UI
        if (headerText != null)
            headerText.text = $"{currentChallenge.topicName} - Q{currentQuestionIndex + 1}";
        
        if (questionText != null)
            questionText.text = q.questionText;
        
        if (scoreText != null)
            scoreText.text = $"Score: {correctAnswers}/{currentQuestionIndex}";
        
        // Setup answers
        SetupAnswerButton(answer1Button, q.answerOptions.Length > 0 ? q.answerOptions[0] : "");
        SetupAnswerButton(answer2Button, q.answerOptions.Length > 1 ? q.answerOptions[1] : "");
        SetupAnswerButton(answer3Button, q.answerOptions.Length > 2 ? q.answerOptions[2] : "");
        SetupAnswerButton(answer4Button, q.answerOptions.Length > 3 ? q.answerOptions[3] : "");
        
        if (feedbackText != null)
            feedbackText.text = "";
        
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);
        
        Debug.Log("✓ Question displayed");
    }
    
    void SetupAnswerButton(Button btn, string text)
    {
        if (btn == null) return;
        
        if (string.IsNullOrEmpty(text))
        {
            btn.gameObject.SetActive(false);
        }
        else
        {
            btn.gameObject.SetActive(true);
            btn.interactable = true;
            
            var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = text;
            
            // Reset color
            var colors = btn.colors;
            colors.normalColor = Color.white;
            btn.colors = colors;
        }
    }
    
    void CheckAnswer(int selectedIndex)
    {
        if (answered) return;
        
        answered = true;
        
        var q = currentChallenge.questions[currentQuestionIndex];
        bool correct = (selectedIndex == q.correctAnswerIndex);
        
        Debug.Log($"Selected: {selectedIndex}, Correct: {q.correctAnswerIndex}");
        Debug.Log(correct ? "✓ CORRECT!" : "✗ WRONG");
        Debug.Log($"Explanation: {q.explanation}");
        
        if (correct)
            correctAnswers++;
        
        // Color the buttons
        ColorButton(answer1Button, 0, q.correctAnswerIndex, selectedIndex);
        ColorButton(answer2Button, 1, q.correctAnswerIndex, selectedIndex);
        ColorButton(answer3Button, 2, q.correctAnswerIndex, selectedIndex);
        ColorButton(answer4Button, 3, q.correctAnswerIndex, selectedIndex);
        
        // Show feedback
        if (feedbackText != null)
        {
            feedbackText.text = correct ? "✓ Correct!\n" + q.explanation : "✗ Wrong\n" + q.explanation;
            feedbackText.color = correct ? Color.green : Color.red;
        }
        
        if (nextButton != null)
            nextButton.gameObject.SetActive(true);
    }
    
    void ColorButton(Button btn, int index, int correctIndex, int selectedIndex)
    {
        if (btn == null || !btn.gameObject.activeSelf) return;
        
        btn.interactable = false;
        var colors = btn.colors;
        
        if (index == correctIndex)
            colors.disabledColor = Color.green;
        else if (index == selectedIndex)
            colors.disabledColor = Color.red;
        else
            colors.disabledColor = Color.gray;
        
        btn.colors = colors;
    }
    
    void NextQuestion()
    {
        currentQuestionIndex++;
        ShowQuestion();
    }
    
    void ShowResults()
    {
        float accuracy = ((float)correctAnswers / currentChallenge.questions.Count) * 100f;
        
        Debug.Log("=== CHALLENGE COMPLETE ===");
        Debug.Log($"Score: {correctAnswers}/{currentChallenge.questions.Count}");
        Debug.Log($"Accuracy: {accuracy:F1}%");
        
        if (questionText != null)
            questionText.text = $"Challenge Complete!\n\nScore: {correctAnswers}/{currentChallenge.questions.Count}\nAccuracy: {accuracy:F0}%";
        
        // Hide answer buttons
        if (answer1Button != null) answer1Button.gameObject.SetActive(false);
        if (answer2Button != null) answer2Button.gameObject.SetActive(false);
        if (answer3Button != null) answer3Button.gameObject.SetActive(false);
        if (answer4Button != null) answer4Button.gameObject.SetActive(false);
        
        if (feedbackText != null)
            feedbackText.text = accuracy >= 70 ? "Great job! 🎉" : "Keep practicing!";
    }
    
    void Update()
    {
        if (!answered && currentChallenge != null && currentQuestionIndex < currentChallenge.questions.Count)
        {
            timeRemaining -= Time.deltaTime;
            
            if (timerText != null)
            {
                timerText.text = $"{Mathf.CeilToInt(timeRemaining)}s";
                timerText.color = timeRemaining <= 10f ? Color.red : Color.white;
            }
            
            if (timeRemaining <= 0)
            {
                Debug.Log("⏰ TIME'S UP!");
                CheckAnswer(-1); // Wrong answer
            }
        }
    }
    
    // Copy questions from ChallengeManager
    ChallengeManager.TopicChallenge GetTestChallenge(string topicName)
    {
        var challenge = new ChallengeManager.TopicChallenge { topicName = topicName };
        
        if (topicName == "Queues")
        {
            challenge.questions = new System.Collections.Generic.List<ChallengeManager.ChallengeQuestion>
            {
                new ChallengeManager.ChallengeQuestion
                {
                    questionText = "Given the queue [A, B, C], perform:\n• Enqueue D\n• Dequeue twice\n• Enqueue E\n\nWhat is the final queue?",
                    answerOptions = new string[] { "[C, D, E]", "[B, C, D, E]", "[D, E]", "[A, B, C, D, E]" },
                    correctAnswerIndex = 0,
                    explanation = "Starting with [A,B,C], enqueue D → [A,B,C,D]. Dequeue twice removes A and B → [C,D]. Enqueue E → [C,D,E]."
                },
                new ChallengeManager.ChallengeQuestion
                {
                    questionText = "What is the time complexity of enqueue and dequeue?",
                    answerOptions = new string[] { "O(1)", "O(n)", "O(log n)", "O(n²)" },
                    correctAnswerIndex = 0,
                    explanation = "Both are O(1) operations as they only modify front or rear."
                }
            };
        }
        else if (topicName == "Stacks")
        {
            challenge.questions = new System.Collections.Generic.List<ChallengeManager.ChallengeQuestion>
            {
                new ChallengeManager.ChallengeQuestion
                {
                    questionText = "What principle does a stack follow?",
                    answerOptions = new string[] { "LIFO", "FIFO", "Priority-based", "Random" },
                    correctAnswerIndex = 0,
                    explanation = "Stack follows Last-In-First-Out (LIFO)."
                }
            };
        }
        
        return challenge;
    }
}