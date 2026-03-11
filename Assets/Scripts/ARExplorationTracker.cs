using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ARExplorationTracker : MonoBehaviour
{
    [Header("AR Tracking")]
    public string currentTopic = "Queues"; // Set this per scene
    
    [Header("Completion Criteria")]
    public bool requireInteraction = true;
    public int minimumInteractions = 5;
    public float minimumTimeSpent = 60f; // seconds
    
    [Header("UI")]
    public GameObject completionPopup;
    public TextMeshProUGUI completionMessageText;
    public Button continueButton;
    
    private int interactionCount = 0;
    private float timeSpent = 0f;
    private bool hasCompleted = false;
    private bool isExploring = false;
    
    void Start()
    {
        // Get current topic from PlayerPrefs if not set
        if (string.IsNullOrEmpty(currentTopic))
        {
            currentTopic = PlayerPrefs.GetString("SelectedTopic", "Queues");
        }
        
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        if (completionPopup != null)
        {
            completionPopup.SetActive(false);
        }
        
        StartExploration();
    }
    
    void Update()
    {
        if (isExploring && !hasCompleted)
        {
            timeSpent += Time.deltaTime;
            
            // Auto-check completion
            if (CheckCompletionCriteria())
            {
                CompleteARExploration();
            }
        }
    }
    
    public void StartExploration()
    {
        isExploring = true;
        timeSpent = 0f;
        interactionCount = 0;
        hasCompleted = false;
        
        Debug.Log($"Started AR exploration: {currentTopic}");
    }
    
    // Call this method whenever user interacts with AR objects
    public void RecordInteraction()
    {
        if (!isExploring || hasCompleted) return;
        
        interactionCount++;
        Debug.Log($"AR Interaction #{interactionCount}");
        
        // Check if completed after interaction
        if (CheckCompletionCriteria())
        {
            CompleteARExploration();
        }
    }
    
    bool CheckCompletionCriteria()
    {
        bool timeRequirementMet = timeSpent >= minimumTimeSpent;
        bool interactionRequirementMet = !requireInteraction || interactionCount >= minimumInteractions;
        
        return timeRequirementMet && interactionRequirementMet;
    }
    
    void CompleteARExploration()
    {
        if (hasCompleted) return;
        
        hasCompleted = true;
        isExploring = false;
        
        // Update progress - use UserProgressManager singleton
        if (UserProgressManager.Instance != null)
        {
            UserProgressManager.Instance.CompleteARExploration(currentTopic);
            Debug.Log($"✓ AR Exploration completed: {currentTopic}");
        }
        else
        {
            Debug.LogError("UserProgressManager.Instance is null!");
        }
        
        ShowCompletionPopup();
    }
    
    void ShowCompletionPopup()
    {
        if (completionPopup != null)
        {
            completionPopup.SetActive(true);
            
            if (completionMessageText != null)
            {
                float percentage = 0;
                
                // Get progress from UserProgressManager
                if (UserProgressManager.Instance != null)
                {
                    percentage = UserProgressManager.Instance.GetTopicProgress(currentTopic);
                }
                
                completionMessageText.text = $"🎉 AR Exploration Complete!\n\n" +
                    $"{currentTopic} progress: {percentage:F0}%\n\n" +
                    $"Time spent: {Mathf.RoundToInt(timeSpent)}s\n" +
                    $"Interactions: {interactionCount}";
            }
        }
    }
    
    void OnContinueClicked()
    {
        if (completionPopup != null)
        {
            completionPopup.SetActive(false);
        }
        
        // Return to topic selection or main menu
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    
    // Public methods for manual completion
    public void ForceCompleteExploration()
    {
        CompleteARExploration();
    }
    
    public int GetInteractionCount()
    {
        return interactionCount;
    }
    
    public float GetTimeSpent()
    {
        return timeSpent;
    }
}