using UnityEngine;
using UnityEngine.SceneManagement;

public class TopicCompletionTracker : MonoBehaviour
{
    // Call this when user completes the interactive tutorial
    public void OnTutorialComplete()
    {
        string currentTopic = PlayerPrefs.GetString("SelectedTopic", "");
        
        if (string.IsNullOrEmpty(currentTopic))
        {
            Debug.LogError("No topic selected!");
            return;
        }
        
        if (UserProgressManager.Instance != null)
        {
            UserProgressManager.Instance.CompleteTutorial(currentTopic);
            ShowCompletionMessage("Tutorial Completed! ✓");
        }
    }
    
    // Call this when user completes the puzzle challenge
    // score should be between 0-100
    public void OnPuzzleComplete(int score)
    {
        string currentTopic = PlayerPrefs.GetString("SelectedTopic", "");
        
        if (string.IsNullOrEmpty(currentTopic))
        {
            Debug.LogError("No topic selected!");
            return;
        }
        
        if (UserProgressManager.Instance != null)
        {
            UserProgressManager.Instance.CompletePuzzle(currentTopic, score);
            ShowCompletionMessage($"Puzzle Completed! Score: {score}%");
        }
    }
    
    void ShowCompletionMessage(string message)
    {
        // You can implement a UI popup here
        Debug.Log(message);
        
        // Optional: Show a completion panel
        // completionPanel.SetActive(true);
        // completionText.text = message;
    }
    
    public void ReturnToTopicSelection()
    {
        SceneManager.LoadScene("TopicSelection");
    }
    
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
