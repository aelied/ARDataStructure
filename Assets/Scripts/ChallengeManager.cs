using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class ChallengeManager : MonoBehaviour
{
    [System.Serializable]
    public class ChallengeQuestion
    {
        public string questionText;
        public string[] answerOptions;
        public int correctAnswerIndex;
        public string explanation;
    }
    
    [System.Serializable]
    public class PuzzleData
    {
        public string description;
        public string hint;
        public string[] items;
        public string[] correctOrder;
    }
    
    [System.Serializable]
    public class TopicChallenge
    {
        public string topicName;
        public List<ChallengeQuestion> questions;
        public PuzzleData puzzle;
    }
    
    [Header("UI References")]
    public GameObject challengePanel;
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI questionNumberText;
    
    [Header("Question Display")]
    public GameObject questionPanel;
    public TextMeshProUGUI questionText;
    public Button[] answerButtons;
    public TextMeshProUGUI[] answerTexts;
    
    [Header("Puzzle UI")]
    public GameObject puzzlePanel;
    public TextMeshProUGUI puzzleDescriptionText;
    public TextMeshProUGUI puzzleHintText;
    public Transform puzzleItemsContainer;
    public GameObject puzzleItemPrefab;
    public Button checkPuzzleButton;
    public Button skipPuzzleButton;
    public Button nextPuzzleButton;
    public TextMeshProUGUI puzzleFeedbackText;
    
    [Header("Feedback")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI explanationText;
    public Button nextQuestionButton;
    
    [Header("Results Screen")]
    public GameObject resultsPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI timeUsedText;
    public TextMeshProUGUI topicCompletedText;
    public Image resultStarImage;
    public Sprite[] starSprites;
    public Button retryButton;
    public Button backToMenuButton;
    
    [Header("Loading")]
    public GameObject loadingPanel;
    public TextMeshProUGUI loadingText;
    
    [Header("Timer Settings")]
    public float timePerQuestion = 30f;
    public float timePerPuzzle = 60f;
    public Color normalTimeColor = Color.white;
    public Color warningTimeColor = Color.red;
    public float warningTimeThreshold = 10f;
    
    [Header("Puzzle Visual Styles")]
    public Color queueNodeColor      = new Color(0.2f, 0.6f, 0.9f);
    public Color stackNodeColor      = new Color(0.9f, 0.4f, 0.2f);
    public Color linkedListNodeColor = new Color(0.3f, 0.8f, 0.4f);
    public Color treeNodeColor       = new Color(0.8f, 0.3f, 0.8f);
    public Color graphNodeColor      = new Color(0.9f, 0.7f, 0.2f);
    public Color arrayNodeColor      = new Color(0.2f, 0.8f, 0.8f);
    
    [Header("Answer Button Colors")]
    public Color correctColor   = new Color(0.3f, 0.8f, 0.3f);
    public Color incorrectColor = new Color(0.8f, 0.3f, 0.3f);
    
    private string API_URL = "https://structureality-admin.onrender.com/api";
    
    private TopicChallenge currentChallenge;
    private int    currentQuestionIndex = 0;
    private int    correctAnswers       = 0;
    private int    totalQuestions       = 0;
    private float  currentTimeRemaining;
    private bool   questionAnswered     = false;
    private float  totalTimeUsed        = 0f;
    private bool   isTimerRunning       = false;
    private bool   puzzleCompleted      = false;
    private bool   showingPuzzle        = false;
    private string currentTopic;
    private List<GameObject> currentPuzzleItems = new List<GameObject>();

    // ── Which bridge / panel launched this challenge ──────────────────────
    private TopicPanelBridge      _callingBridge    = null;
    private CodingEnvironmentPanel _callingCodePanel = null;

    // ── Lesson quiz mode (post-AR quiz) ───────────────────────────────────
    private bool   _lessonQuizMode = false;
    private string _lessonTitle    = "";
    private int    _lessonIndex    = 0;
    
    void Start()
    {
        Debug.Log("🚀 ChallengeManager Start() called");
        
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            if (answerButtons[i] != null)
                answerButtons[i].onClick.AddListener(() => SelectAnswer(index));
        }
        
        if (nextQuestionButton != null)   nextQuestionButton.onClick.AddListener(ShowNextQuestion);
        if (checkPuzzleButton  != null)   checkPuzzleButton.onClick.AddListener(CheckPuzzleAnswer);
        if (skipPuzzleButton   != null)   skipPuzzleButton.onClick.AddListener(SkipPuzzle);
        if (nextPuzzleButton   != null)   nextPuzzleButton.onClick.AddListener(ShowNextQuestion);
        if (retryButton        != null)   retryButton.onClick.AddListener(RetryChallenge);
        if (backToMenuButton   != null)   backToMenuButton.onClick.AddListener(BackToMenu);
        
        HideAllPanels();
    }
    
    public void ForceHideAllPanels()
    {
        Debug.Log("🚫 [ChallengeManager] Force hiding all panels");
        if (challengePanel  != null) challengePanel.SetActive(false);
        if (resultsPanel    != null) resultsPanel.SetActive(false);
        if (questionPanel   != null) questionPanel.SetActive(false);
        if (puzzlePanel     != null) puzzlePanel.SetActive(false);
        if (feedbackPanel   != null) feedbackPanel.SetActive(false);
        if (loadingPanel    != null) loadingPanel.SetActive(false);
    }

    // ── Called by TestsTabController ──────────────────────────────────────
    public void StartChallengeFromBridge(string topicName, TopicPanelBridge bridge)
    {
        _callingBridge    = bridge;
        _callingCodePanel = null;
        _lessonQuizMode   = false;
        StartChallenge(topicName);
    }

    // ── Called by CodingEnvironmentPanel ──────────────────────────────────
    public void StartChallengeFromCodePanel(string topicName, string difficulty, CodingEnvironmentPanel codePanel)
    {
        _callingCodePanel = codePanel;
        _callingBridge    = null;
        _lessonQuizMode   = false;

        Debug.Log($"[ChallengeManager] StartChallengeFromCodePanel: {topicName}");
        StartChallenge(topicName);
    }

    // ── Called by ARReturnHandler after user returns from AR scene ─────────
    public void StartLessonQuiz(
        string topicName,
        string lessonTitle,
        int    lessonIndex,
        List<QuizDataPublic> questions)
    {
        Debug.Log($"[ChallengeManager] StartLessonQuiz: {topicName} — {lessonTitle}");

        _lessonQuizMode   = true;
        _lessonTitle      = lessonTitle;
        _lessonIndex      = lessonIndex;
        _callingCodePanel = null;
        currentTopic      = topicName;

        currentChallenge = new TopicChallenge
        {
            topicName = topicName,
            questions = new List<ChallengeQuestion>(),
            puzzle    = GetPuzzleForTopic(topicName)
        };

        foreach (var q in questions)
        {
            currentChallenge.questions.Add(new ChallengeQuestion
            {
                questionText       = q.questionText,
                answerOptions      = q.answerOptions,
                correctAnswerIndex = q.correctAnswerIndex,
                explanation        = q.explanation
            });
        }

        currentQuestionIndex = 0;
        correctAnswers       = 0;
        totalQuestions       = currentChallenge.questions.Count + 1; // +1 for puzzle
        totalTimeUsed        = 0f;
        puzzleCompleted      = false;

        ShowPuzzle();
    }

    // ── Public struct so ARReturnHandler can pass questions in ────────────
    [System.Serializable]
    public class QuizDataPublic
    {
        public string   _id;
        public string   topicName;
        public string   questionText;
        public string[] answerOptions;
        public int      correctAnswerIndex;
        public string   explanation;
        public string   difficulty;
        public int      order;
        public string   lessonTitle;
        public int      lessonIndex;
    }

    // ── Main entry point: load all quizzes for the topic ──────────────────
    public void StartChallenge(string topicName)
    {
        Debug.Log($"🎯 StartChallenge: {topicName}");
        currentTopic = topicName;
        StartCoroutine(LoadChallengeFromDatabase(topicName));
    }
    
    IEnumerator LoadChallengeFromDatabase(string topicName)
    {
        ShowLoading($"Loading questions for {topicName}...");
        
        // Fetch ALL quizzes for this topic (no difficulty filter)
        string url = $"{API_URL}/quizzes/{topicName}";
        Debug.Log($"🌐 Fetching from: {url}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    QuizzesResponse response = JsonUtility.FromJson<QuizzesResponse>(request.downloadHandler.text);
                    
                    if (response.success && response.quizzes != null && response.quizzes.Count > 0)
                    {
                        currentChallenge = new TopicChallenge
                        {
                            topicName = topicName,
                            questions = new List<ChallengeQuestion>(),
                            puzzle    = GetPuzzleForTopic(topicName)
                        };
                        
                        // Sort by order field only
                        response.quizzes.Sort((a, b) => a.order.CompareTo(b.order));
                        
                        foreach (var quiz in response.quizzes)
                        {
                            currentChallenge.questions.Add(new ChallengeQuestion
                            {
                                questionText       = quiz.questionText,
                                answerOptions      = quiz.answerOptions,
                                correctAnswerIndex = quiz.correctAnswerIndex,
                                explanation        = quiz.explanation
                            });
                        }
                        
                        Debug.Log($"✅ Loaded {currentChallenge.questions.Count} questions for {topicName}");
                        
                        currentQuestionIndex = 0;
                        correctAnswers       = 0;
                        totalQuestions       = currentChallenge.questions.Count + 1; // +1 for puzzle
                        totalTimeUsed        = 0f;
                        puzzleCompleted      = false;
                        
                        HideLoading();
                        ShowPuzzle();
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ No questions found, using fallback");
                        LoadFallbackChallenge(topicName);
                        HideLoading();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Parse error: {e.Message}");
                    LoadFallbackChallenge(topicName);
                    HideLoading();
                }
            }
            else
            {
                Debug.LogError($"Request failed: {request.error}");
                LoadFallbackChallenge(topicName);
                HideLoading();
            }
        }
    }
    
    void LoadFallbackChallenge(string topicName)
    {
        currentChallenge = new TopicChallenge
        {
            topicName = topicName,
            questions = new List<ChallengeQuestion>
            {
                new ChallengeQuestion
                {
                    questionText       = $"What is the main characteristic of {topicName}?",
                    answerOptions      = new string[] { "Dynamic", "Fixed", "No order", "Random" },
                    correctAnswerIndex = 0,
                    explanation        = "Fallback question. Add real questions in admin panel."
                }
            },
            puzzle = GetPuzzleForTopic(topicName)
        };
        
        currentQuestionIndex = 0;
        correctAnswers       = 0;
        totalQuestions       = currentChallenge.questions.Count + 1;
        totalTimeUsed        = 0f;
        puzzleCompleted      = false;
        
        ShowPuzzle();
    }
    
    PuzzleData GetPuzzleForTopic(string topicName)
    {
        switch (topicName)
        {
            case "Queue":
                return new PuzzleData
                {
                    description  = "🎫 Queue Challenge: Customer Service Line\nArrange customers in the order they will be SERVED (First-In-First-Out)",
                    hint         = "💡 The person who arrived FIRST gets served FIRST. Check the arrival times!",
                    items        = new string[] { " David (Arrived 4th)", " Carol (Arrived 3rd)", " Alice (Arrived 1st)", " Bob (Arrived 2nd)" },
                    correctOrder = new string[] { " Alice (Arrived 1st)", " Bob (Arrived 2nd)", " Carol (Arrived 3rd)", " David (Arrived 4th)" }
                };
            case "Stacks":
                return new PuzzleData
                {
                    description  = "Stack Challenge: Washing Dishes\nArrange plates in the order they will be PICKED UP (Last-In-First-Out)",
                    hint         = "The plate on TOP gets picked first. Which plate was placed last?",
                    items        = new string[] { " Blue Plate (1st washed)", " Purple Plate (4th washed)", " Green Plate (2nd washed)", " Yellow Plate (3rd washed)" },
                    correctOrder = new string[] { " Purple Plate (4th washed)", " Yellow Plate (3rd washed)", " Green Plate (2nd washed)", " Blue Plate (1st washed)" }
                };
            case "LinkedLists":
                return new PuzzleData
                {
                    description  = "Linked List Challenge: Follow the Chain\nEach node points to the NEXT node. Arrange them in traversal order.",
                    hint         = "Start from HEAD and follow the 'next' pointers. A→B means A points to B.",
                    items        = new string[] { " Node D (next: NULL)", " Node B (next: C)", " HEAD → A (next: B)", " Node C (next: D)" },
                    correctOrder = new string[] { " HEAD → A (next: B)", " Node B (next: C)", " Node C (next: D)", " Node D (next: NULL)" }
                };
            case "Trees":
                return new PuzzleData
                {
                    description  = "Binary Search Tree Challenge: Inorder Traversal\nArrange values in INORDER sequence (Left → Root → Right)",
                    hint         = "BST Inorder = sorted order! Visit LEFT subtree, then ROOT, then RIGHT subtree.",
                    items        = new string[] { " 50 (Root)", " 30 (Left child)", " 20 (Leftmost)", " 40 (Right of 30)" },
                    correctOrder = new string[] { " 20 (Leftmost)", " 30 (Left child)", " 40 (Right of 30)", " 50 (Root)" }
                };
            case "Graphs":
                return new PuzzleData
                {
                    description  = "Graph Challenge: Breadth-First Search (BFS)\nArrange nodes in BFS order starting from node A.",
                    hint         = "BFS visits all neighbors first! From A→(B,C)→D. Visit level by level.",
                    items        = new string[] { " Node D (Level 3)", " Node A (START)", " Node C (Level 2)", " Node B (Level 2)" },
                    correctOrder = new string[] { " Node A (START)", " Node B (Level 2)", " Node C (Level 2)", " Node D (Level 3)" }
                };
            case "Arrays":
                return new PuzzleData
                {
                    description  = "Array Challenge: Sort by Index\nArrange elements by their array index positions (0-based indexing)",
                    hint         = "Arrays start at index 0! Remember: [0], [1], [2], [3]...",
                    items        = new string[] { " arr[3] = 'Delta'", " arr[1] = 'Bravo'", " arr[0] = 'Alpha'", " arr[2] = 'Charlie'" },
                    correctOrder = new string[] { " arr[0] = 'Alpha'", " arr[1] = 'Bravo'", " arr[2] = 'Charlie'", " arr[3] = 'Delta'" }
                };
            default:
                return new PuzzleData
                {
                    description  = "Data Structure Challenge: Arrange in logical order",
                    hint         = "Think about how this data structure processes elements",
                    items        = new string[] { " Item 1", " Item 2", " Item 3", " Item 4" },
                    correctOrder = new string[] { " Item 1", " Item 2", " Item 3", " Item 4" }
                };
        }
    }
    
    void ShowLoading(string message)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (loadingText != null) loadingText.text = message;
        }
    }
    
    void HideLoading()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }
    
    void ShowPuzzle()
    {
        showingPuzzle        = true;
        questionAnswered     = false;
        currentTimeRemaining = timePerPuzzle;
        isTimerRunning       = true;
        
        HideAllPanels();
        
        if (challengePanel != null) challengePanel.SetActive(true);
        if (puzzlePanel    != null) puzzlePanel.SetActive(true);
        
        if (headerText       != null) headerText.text       = $"{currentChallenge.topicName} — Quiz";
        if (questionNumberText != null) questionNumberText.text = $"PUZZLE: 1/{totalQuestions}";
        if (scoreText        != null) scoreText.text        = "0/0";
        
        if (puzzleDescriptionText != null) puzzleDescriptionText.text = currentChallenge.puzzle.description;
        if (puzzleHintText        != null) puzzleHintText.text        = currentChallenge.puzzle.hint;
        if (puzzleFeedbackText    != null) puzzleFeedbackText.text    = "";
        
        ClearPuzzleItems();
        CreatePuzzleItems(currentChallenge.puzzle.items);
        
        if (checkPuzzleButton != null) { checkPuzzleButton.gameObject.SetActive(true);  checkPuzzleButton.interactable = true; }
        if (skipPuzzleButton  != null) { skipPuzzleButton.gameObject.SetActive(true);   skipPuzzleButton.interactable  = true; }
        if (nextPuzzleButton  != null)   nextPuzzleButton.gameObject.SetActive(false);
    }
    
    void ClearPuzzleItems()
    {
        foreach (GameObject item in currentPuzzleItems)
            if (item != null) Destroy(item);
        currentPuzzleItems.Clear();
    }
    
    Color GetTopicColor()
    {
        switch (currentTopic)
        {
            case "Queue":       return queueNodeColor;
            case "Stacks":      return stackNodeColor;
            case "LinkedLists": return linkedListNodeColor;
            case "Trees":       return treeNodeColor;
            case "Graphs":      return graphNodeColor;
            case "Arrays":      return arrayNodeColor;
            default:            return Color.white;
        }
    }
    
    void CreatePuzzleItems(string[] items)
    {
        if (puzzleItemsContainer == null) return;
        
        List<string> shuffled = new List<string>(items);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int r = Random.Range(i, shuffled.Count);
            string temp = shuffled[i]; shuffled[i] = shuffled[r]; shuffled[r] = temp;
        }
        
        Color topicColor = GetTopicColor();
        
        foreach (string itemText in shuffled)
        {
            GameObject item = puzzleItemPrefab != null
                ? Instantiate(puzzleItemPrefab, puzzleItemsContainer)
                : CreateStyledPuzzleItem();
            
            if (puzzleItemPrefab == null)
                item.transform.SetParent(puzzleItemsContainer, false);
            
            Image img = item.GetComponent<Image>();
            if (img != null) img.color = topicColor;
            
            Outline outline = item.GetComponent<Outline>() ?? item.AddComponent<Outline>();
            outline.effectColor    = Color.white;
            outline.effectDistance = new Vector2(3, 3);
            
            Shadow shadow = item.GetComponent<Shadow>() ?? item.AddComponent<Shadow>();
            shadow.effectColor    = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(2, -2);
            
            TextMeshProUGUI text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text      = itemText;
                text.color     = Color.black;
                text.fontStyle = FontStyles.Bold;
                text.fontSize  = 24;
                text.alignment = TextAlignmentOptions.Center;
            }
            
            LayoutElement le = item.GetComponent<LayoutElement>() ?? item.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
            le.minHeight       = 70;
            
            PuzzleItemDragHandler drag = item.GetComponent<PuzzleItemDragHandler>() ?? item.AddComponent<PuzzleItemDragHandler>();
            drag.SetContainer(puzzleItemsContainer);
            
            currentPuzzleItems.Add(item);
        }
        
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(puzzleItemsContainer as RectTransform);
    }
    
    GameObject CreateStyledPuzzleItem()
    {
        GameObject item = new GameObject("PuzzleItem");
        RectTransform rt = item.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 80);
        
        Image img = item.AddComponent<Image>();
        img.color = Color.white;
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(item.transform);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize  = 24;
        text.fontStyle = FontStyles.Bold;
        text.color     = Color.white;
        
        return item;
    }
    
    void CheckPuzzleAnswer()
    {
        if (questionAnswered) return;
        questionAnswered = true;
        isTimerRunning   = false;
        
        List<string> currentOrder = new List<string>();
        foreach (Transform child in puzzleItemsContainer)
        {
            TextMeshProUGUI text = child.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) currentOrder.Add(text.text);
        }
        
        bool isCorrect = currentOrder.Count == currentChallenge.puzzle.correctOrder.Length;
        if (isCorrect)
        {
            for (int i = 0; i < currentOrder.Count; i++)
            {
                if (currentOrder[i] != currentChallenge.puzzle.correctOrder[i])
                {
                    isCorrect = false;
                    break;
                }
            }
        }
        
        if (isCorrect) { correctAnswers++; puzzleCompleted = true; }
        ShowPuzzleFeedback(isCorrect);
    }
    
    void ShowPuzzleFeedback(bool correct)
    {
        if (checkPuzzleButton != null) checkPuzzleButton.gameObject.SetActive(false);
        if (skipPuzzleButton  != null) skipPuzzleButton.gameObject.SetActive(false);
        
        foreach (GameObject item in currentPuzzleItems)
        {
            PuzzleItemDragHandler drag = item.GetComponent<PuzzleItemDragHandler>();
            if (drag != null) drag.enabled = false;
        }
        
        if (puzzleFeedbackText != null)
        {
            string msg = correct ? "Perfect!\n\n" : "Not quite.\n\n";
            msg += "Correct order:\n";
            for (int i = 0; i < currentChallenge.puzzle.correctOrder.Length; i++)
                msg += $"{i + 1}. {currentChallenge.puzzle.correctOrder[i]}\n";
            puzzleFeedbackText.text  = msg;
            puzzleFeedbackText.color = correct ? correctColor : incorrectColor;
        }
        
        if (scoreText       != null) scoreText.text = $"{correctAnswers}/1";
        if (nextPuzzleButton != null) nextPuzzleButton.gameObject.SetActive(true);
    }
    
    void SkipPuzzle()
    {
        if (questionAnswered) return;
        questionAnswered = true;
        isTimerRunning   = false;
        ShowPuzzleFeedback(false);
    }
    
    void ShowQuestion()
    {
        if (currentQuestionIndex >= currentChallenge.questions.Count)
        {
            ShowResults();
            return;
        }
        
        showingPuzzle        = false;
        questionAnswered     = false;
        currentTimeRemaining = timePerQuestion;
        isTimerRunning       = true;
        
        HideAllPanels();
        if (challengePanel != null) challengePanel.SetActive(true);
        if (questionPanel  != null) questionPanel.SetActive(true);
        
        ChallengeQuestion question = currentChallenge.questions[currentQuestionIndex];
        
        if (headerText != null)
        {
            headerText.text = _lessonQuizMode
                ? $"{currentChallenge.topicName} — 📖 Lesson Quiz"
                : $"{currentChallenge.topicName} — Quiz";
        }

        if (questionNumberText != null)
            questionNumberText.text = $"Q{currentQuestionIndex + 1}: {currentQuestionIndex + 2}/{totalQuestions}";
        
        int totalAnswered = currentQuestionIndex + (puzzleCompleted ? 1 : 0);
        if (scoreText    != null) scoreText.text    = $"{correctAnswers}/{totalAnswered + 1}";
        if (questionText != null) questionText.text = question.questionText;
        
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < question.answerOptions.Length)
            {
                if (answerButtons[i] != null) { answerButtons[i].gameObject.SetActive(true); answerButtons[i].interactable = true; }
                if (answerTexts[i]   != null)   answerTexts[i].text = question.answerOptions[i];

                // Reset button color
                if (answerButtons[i] != null)
                {
                    ColorBlock cb = answerButtons[i].colors;
                    cb.normalColor   = Color.white;
                    cb.disabledColor = new Color(0.9f, 0.9f, 0.9f);
                    answerButtons[i].colors = cb;
                }
            }
            else
            {
                if (answerButtons[i] != null) answerButtons[i].gameObject.SetActive(false);
            }
        }
    }
    
    void SelectAnswer(int answerIndex)
    {
        if (questionAnswered) return;
        questionAnswered = true;
        isTimerRunning   = false;
        
        ChallengeQuestion question = currentChallenge.questions[currentQuestionIndex];
        bool isCorrect = (answerIndex == question.correctAnswerIndex);
        if (isCorrect) correctAnswers++;
        ShowQuestionFeedback(isCorrect, answerIndex, question);
    }
    
    void ShowQuestionFeedback(bool isCorrect, int selectedIndex, ChallengeQuestion question)
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null && answerButtons[i].gameObject.activeSelf)
            {
                answerButtons[i].interactable = false;
                ColorBlock colors = answerButtons[i].colors;
                if      (i == question.correctAnswerIndex)        colors.disabledColor = correctColor;
                else if (i == selectedIndex && !isCorrect)        colors.disabledColor = incorrectColor;
                else                                              colors.disabledColor = new Color(0.7f, 0.7f, 0.7f);
                answerButtons[i].colors = colors;
            }
        }
        
        if (feedbackPanel  != null) feedbackPanel.SetActive(true);
        if (feedbackText   != null)
        {
            feedbackText.text  = isCorrect ? "✓ Correct!" : "✗ Incorrect";
            feedbackText.color = isCorrect ? correctColor : incorrectColor;
        }
        if (explanationText != null) explanationText.text = question.explanation;
        
        int totalAnswered = currentQuestionIndex + (puzzleCompleted ? 1 : 0) + 1;
        if (scoreText != null) scoreText.text = $"{correctAnswers}/{totalAnswered}";
    }
    
    void TimeUp()
    {
        if (questionAnswered) return;
        questionAnswered = true;
        isTimerRunning   = false;
        
        if (showingPuzzle)
            ShowPuzzleFeedback(false);
        else
        {
            ChallengeQuestion question = currentChallenge.questions[currentQuestionIndex];
            ShowQuestionFeedback(false, -1, question);
            if (feedbackText != null) feedbackText.text = "⏰ Time's Up!";
        }
    }
    
    void ShowNextQuestion()
    {
        if (showingPuzzle) ShowQuestion();
        else { currentQuestionIndex++; ShowQuestion(); }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  RESULTS
    // ═══════════════════════════════════════════════════════════════════════
    void ShowResults()
    {
        if (questionPanel  != null) questionPanel.SetActive(false);
        if (puzzlePanel    != null) puzzlePanel.SetActive(false);
        if (feedbackPanel  != null) feedbackPanel.SetActive(false);
        if (loadingPanel   != null) loadingPanel.SetActive(false);

        if (challengePanel != null) challengePanel.SetActive(true);
        if (resultsPanel   != null) resultsPanel.SetActive(true);

        float accuracy = (float)correctAnswers / totalQuestions * 100f;
        int   stars    = GetStarRating(accuracy);
        
        if (finalScoreText  != null) finalScoreText.text  = $"{correctAnswers}/{totalQuestions}";
        if (accuracyText    != null) accuracyText.text    = $"Accuracy: {accuracy:F0}%";
        if (timeUsedText    != null)
        {
            int min = Mathf.FloorToInt(totalTimeUsed / 60f);
            int sec = Mathf.FloorToInt(totalTimeUsed % 60f);
            timeUsedText.text = $"Time: {min:D2}:{sec:D2}";
        }

        if (topicCompletedText != null)
        {
            topicCompletedText.text  = _lessonQuizMode
                ? $"📖 Lesson {_lessonIndex + 1}: {_lessonTitle}"
                : $"📚 {currentChallenge.topicName}";
        }

        if (resultStarImage != null && starSprites != null && stars < starSprites.Length)
            resultStarImage.sprite = starSprites[stars];
        
        SaveChallengeProgress(accuracy);
    }
    
    int GetStarRating(float accuracy)
    {
        if (accuracy >= 90f) return 3;
        if (accuracy >= 70f) return 2;
        if (accuracy >= 50f) return 1;
        return 0;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  BACK TO MENU
    // ═══════════════════════════════════════════════════════════════════════
    void BackToMenu()
    {
        HideAllPanels();

        // a) Post-AR lesson quiz
        if (_lessonQuizMode)
        {
            _lessonQuizMode = false;

            TopicPanelBridge bridge = _callingBridge != null
                ? _callingBridge
                : FindObjectOfType<TopicPanelBridge>();

            if (bridge != null)
                bridge.ReturnToLessonsList();
            else
            {
                UpdatedLearnPanelController lp = FindObjectOfType<UpdatedLearnPanelController>();
                if (lp != null) lp.ShowTopicSelection();
            }
            return;
        }

        // b) Code panel challenge
        if (_callingCodePanel != null)
        {
            CodingEnvironmentPanel panel = _callingCodePanel;
            _callingCodePanel = null;
            panel.OnChallengeComplete();
            return;
        }

        // c) Normal Tests tab challenge
        TopicPanelBridge normalBridge = _callingBridge != null
            ? _callingBridge
            : FindObjectOfType<TopicPanelBridge>();

        if (normalBridge != null)
            normalBridge.ShowAfterChallenge();
        else
        {
            UpdatedLearnPanelController learnPanel = FindObjectOfType<UpdatedLearnPanelController>();
            if (learnPanel != null) StartCoroutine(ShowTopicSelectionAfterDelay(learnPanel));
        }
    }

    public void BackToTopicSelection()
    {
        BackToMenu();
    }

    IEnumerator ShowTopicSelectionAfterDelay(UpdatedLearnPanelController learnPanel)
    {
        yield return new WaitForSeconds(0.5f);
        learnPanel.ShowTopicSelection();
    }
    
    void RetryChallenge()
    {
        _lessonQuizMode = false;
        StartChallenge(currentTopic);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SAVE PROGRESS  (simplified: saves a single score per topic)
    // ═══════════════════════════════════════════════════════════════════════
    void SaveChallengeProgress(float accuracy)
    {
        string username = PlayerPrefs.GetString("CurrentUser", "");
        if (string.IsNullOrEmpty(username)) return;
        
        string normalizedTopic = TopicNameConstants.Normalize(currentTopic);

        // Lesson quiz: save under a lesson-specific key
        if (_lessonQuizMode)
        {
            string key  = $"User_{username}_{normalizedTopic}_LessonQuiz_{_lessonIndex}_Score";
            float  best = PlayerPrefs.GetFloat(key, 0f);
            if (accuracy > best)
            {
                PlayerPrefs.SetFloat(key, accuracy);
                PlayerPrefs.Save();
            }
            // Sync using "lesson_N" as the difficulty label so server accepts it
            StartCoroutine(SyncScoreToDatabase(normalizedTopic, $"lesson_{_lessonIndex}", (int)accuracy));
            return;
        }
        
        // Normal quiz: save under a single topic score key
        string localKey    = $"User_{username}_{normalizedTopic}_Score";
        float  currentBest = PlayerPrefs.GetFloat(localKey, 0f);
        
        if (accuracy > currentBest)
        {
            PlayerPrefs.SetFloat(localKey, accuracy);
            PlayerPrefs.Save();
            Debug.Log($"✅ Score saved: {normalizedTopic} = {accuracy}%");
        }

        // Sync to database — use "mixed" so existing server endpoint accepts it
        StartCoroutine(SyncScoreToDatabase(normalizedTopic, "mixed", (int)accuracy));
    }

    IEnumerator SyncScoreToDatabase(string topicName, string difficulty, int score)
    {
        string username = PlayerPrefs.GetString("CurrentUser", "");
        if (string.IsNullOrEmpty(username)) yield break;
        
        string jsonData = $"{{\"topicName\":\"{topicName}\",\"difficulty\":\"{difficulty}\",\"score\":{score}}}";
        string url      = $"{API_URL}/progress/{username}/difficulty";
        
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 30;
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log($"✅ Score synced: {topicName} = {score}%");
        else
            Debug.LogError($"❌ Sync failed: {request.error}");
    }
    
    void OnDisable()
    {
        HideAllPanels();
    }
    
    void HideAllPanels()
    {
        if (challengePanel != null) challengePanel.SetActive(false);
        if (questionPanel  != null) questionPanel.SetActive(false);
        if (puzzlePanel    != null) puzzlePanel.SetActive(false);
        if (feedbackPanel  != null) feedbackPanel.SetActive(false);
        if (resultsPanel   != null) resultsPanel.SetActive(false);
        if (loadingPanel   != null) loadingPanel.SetActive(false);
    }
    
    void Update()
    {
        if (!isTimerRunning) return;
        
        currentTimeRemaining -= Time.deltaTime;
        totalTimeUsed        += Time.deltaTime;
        
        if (currentTimeRemaining <= 0)
        {
            currentTimeRemaining = 0;
            TimeUp();
        }
        
        UpdateTimerDisplay();
    }
    
    void UpdateTimerDisplay()
    {
        if (timerText == null) return;
        int min = Mathf.FloorToInt(currentTimeRemaining / 60f);
        int sec = Mathf.FloorToInt(currentTimeRemaining % 60f);
        timerText.text  = $"{min:D2}:{sec:D2}";
        timerText.color = currentTimeRemaining <= warningTimeThreshold ? warningTimeColor : normalTimeColor;
    }

    // ── Compatibility stub (called by TestsTabController) ─────────────────
    public void StartEasyChallenge(string topicName, TopicPanelBridge bridge)
    {
        _callingBridge    = bridge;
        _callingCodePanel = null;
        _lessonQuizMode   = false;
        StartChallenge(topicName);
    }

    // ── Serialization helpers ─────────────────────────────────────────────
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
        public string   createdAt;
        public string   lessonTitle;
        public int      lessonIndex;
    }
    
    [System.Serializable]
    private class QuizzesResponse
    {
        public bool           success;
        public string         topicName;
        public string         difficulty;
        public int            count;
        public List<QuizData> quizzes;
    }

    [System.Serializable]
    private class DatabaseProgressResponse
    {
        public bool          success;
        public ProgressData  data;

        [System.Serializable]
        public class ProgressData
        {
            public string           username;
            public List<TopicEntry> topics;
        }

        [System.Serializable]
        public class TopicEntry
        {
            public string          topicName;
            public int             lessonsCompleted;
            public DiffScores      difficultyScores;
        }

        [System.Serializable]
        public class DiffScores
        {
            public int easy;
            public int medium;
            public int hard;
            public int mixed;
        }
    }
}