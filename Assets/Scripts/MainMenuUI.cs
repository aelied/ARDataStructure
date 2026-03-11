using UnityEngine;
using UnityEngine.UI;
using TMPro;

// This component works WITH your existing MainMenuManager
// Add this to your Canvas or a separate GameObject
public class MainMenuProgressDisplay : MonoBehaviour
{
    [Header("Progress Bar (Optional - if you want to add one)")]
    public Slider overallProgressBar;
    public TextMeshProUGUI progressPercentText;
    
    [Header("Topic Progress Texts (From your topic cards)")]
    public TextMeshProUGUI queueProgressText;
    public TextMeshProUGUI stacksProgressText;
    public TextMeshProUGUI linkedListsProgressText;
    public TextMeshProUGUI treesProgressText;
    public TextMeshProUGUI graphsProgressText;
    
    void Start()
    {
        UpdateProgressDisplay();
    }
    
    void OnEnable()
    {
        UpdateProgressDisplay();
    }
    
    public void UpdateProgressDisplay()
    {
        if (UserProgressManager.Instance == null)
        {
            Debug.LogWarning("UserProgressManager not found!");
            return;
        }
        
        // Update overall progress bar (if you add one)
        float overallProgress = UserProgressManager.Instance.GetOverallProgress();
        if (overallProgressBar != null)
        {
            overallProgressBar.value = overallProgress / 100f;
        }
        
        if (progressPercentText != null)
        {
            progressPercentText.text = $"{overallProgress:F0}%";
        }
        
        // Update individual topic progress percentages
        UpdateTopicProgress("Queue", queueProgressText);
        UpdateTopicProgress("Stacks", stacksProgressText);
        UpdateTopicProgress("LinkedLists", linkedListsProgressText);
        UpdateTopicProgress("Trees", treesProgressText);
        UpdateTopicProgress("Graphs", graphsProgressText);
    }
    
    void UpdateTopicProgress(string topicName, TextMeshProUGUI progressText)
    {
        if (progressText == null) return;
        
        float progress = UserProgressManager.Instance.GetTopicProgress(topicName);
        progressText.text = $"{progress:F0}%";
        
        // Optional: Change color based on completion
        if (progress >= 100f)
        {
            progressText.color = new Color(0.2f, 0.8f, 0.2f); // Green
        }
        else if (progress >= 50f)
        {
            progressText.color = new Color(1f, 0.8f, 0.2f); // Yellow/Orange
        }
        else
        {
            progressText.color = Color.white;
        }
    }
}