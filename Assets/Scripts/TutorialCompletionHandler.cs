using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Add this script to your tutorial/interactive learning scenes
/// Call CompleteTutorial() when the user finishes the lesson
/// </summary>
public class TutorialCompletionHandler : MonoBehaviour
{
    [Header("Configuration")]
    public string currentTopic = ""; // Auto-detected if empty
    
    [Header("Completion UI")]
    public GameObject completionPanel;
    public TextMeshProUGUI completionTitleText;
    public TextMeshProUGUI completionMessageText;
    public TextMeshProUGUI progressUpdateText;
    public Button continueButton;
    public Button retryButton;
    
    [Header("Completion Criteria")]
    public bool requireAllStepsCompleted = true;
    public int totalSteps = 5;
    
    private int completedSteps = 0;
    private bool hasCompleted = false;
    
    void Start()
    {
        // Auto-detect topic if not set
        if (string.IsNullOrEmpty(currentTopic))
        {
            currentTopic = PlayerPrefs.GetString("SelectedTopic", "");
        }
        
        if (string.IsNullOrEmpty(currentTopic))
        {
            Debug.LogError("No topic specified for tutorial!");
        }
        
        // Setup UI
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);
        
        if (completionPanel != null)
            completionPanel.SetActive(false);
        
        Debug.Log($"Tutorial started: {currentTopic}");
    }
    
    /// <summary>
    /// Call this when user completes a step in the tutorial
    /// </summary>
    public void CompleteStep()
    {
        completedSteps++;
        Debug.Log($"Tutorial step {completedSteps}/{totalSteps} completed");
        
        // Check if all steps are done
        if (requireAllStepsCompleted && completedSteps >= totalSteps)
        {
            CompleteTutorial();
        }
    }
    
    /// <summary>
    /// Call this to manually mark the tutorial as complete
    /// </summary>
    public void CompleteTutorial()
    {
        if (hasCompleted) return;
        
        hasCompleted = true;
        
        Debug.Log($"✓ Tutorial completed: {currentTopic}");
        
        // Update progress system
        if (UserProgressManager.Instance != null)
        {
            UserProgressManager.Instance.CompleteTutorial(currentTopic);
            
            float progress = UserProgressManager.Instance.GetTopicProgress(currentTopic);
            Debug.Log($"Topic progress: {progress}%");
            
            ShowCompletionUI(progress);
        }
        else
        {
            Debug.LogError("UserProgressManager not found!");
            ShowCompletionUI(0f);
        }
    }
    
    void ShowCompletionUI(float progressPercentage)
    {
        if (completionPanel == null) return;
        
        completionPanel.SetActive(true);
        
        if (completionTitleText != null)
        {
            completionTitleText.text = "🎉 Tutorial Complete!";
        }
        
        if (completionMessageText != null)
        {
            completionMessageText.text = $"Great job learning about {currentTopic}!\n\n" +
                "You've completed the interactive tutorial.";
        }
        
        if (progressUpdateText != null && UserProgressManager.Instance != null)
        {
            progressUpdateText.text = $"Topic Progress: {progressPercentage}%\n\n";
            
            bool puzzleCompleted = UserProgressManager.Instance.IsPuzzleCompleted(currentTopic);
            
            if (!puzzleCompleted)
            {
                progressUpdateText.text += "• Try the Puzzle Challenge next to unlock 100%!\n";
            }
            else
            {
                progressUpdateText.text += "• You've mastered this topic! ✓\n";
            }
        }
    }
    
    void OnContinueClicked()
    {
        // Return to main menu
        SceneManager.LoadScene("MainMenu");
    }
    
    void OnRetryClicked()
    {
        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public int GetCompletedSteps()
    {
        return completedSteps;
    }
    
    public float GetProgressPercentage()
    {
        if (totalSteps == 0) return 0f;
        return (float)completedSteps / totalSteps * 100f;
    }
}