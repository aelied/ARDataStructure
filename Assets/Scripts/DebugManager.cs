using UnityEngine;

/// <summary>
/// Debug helper for testing and monitoring during development
/// </summary>
public class DebugManager : MonoBehaviour
{
    void Update()
    {
        // Press T to see time tracking stats
        if (Input.GetKeyDown(KeyCode.T))
        {
            UserProgressManager.Instance?.LogTimeTrackingStats();
        }
        
        // Press S to force sync to database
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (UserProgressManager.Instance != null)
            {
                Debug.Log("🔄 Manual sync triggered via keyboard");
                UserProgressManager.Instance.ManualSync();
            }
        }
        
        // Press P to show current user info
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (UserProgressManager.Instance != null)
            {
                Debug.Log($"👤 User: {UserProgressManager.Instance.GetCurrentUsername()}");
                Debug.Log($"📊 Overall Progress: {UserProgressManager.Instance.GetOverallProgress():F1}%");
                Debug.Log($"🔥 Streak: {UserProgressManager.Instance.GetStreak()} days");
            }
        }
    }
}