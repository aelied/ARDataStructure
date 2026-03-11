using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach this to your Back Button or any button that exits the AR scene
/// Cleans up ALL data structure visualizers before leaving
/// </summary>
public class BackButtonHandler : MonoBehaviour
{
    [Header("Scene to Load")]
    [Tooltip("Name of the scene to return to (e.g., 'MainMenu')")]
    public string targetSceneName = "MainMenu";
    
    /// <summary>
    /// Call this method from your Back Button's OnClick() event
    /// </summary>
    public void OnBackButtonClicked()
    {
        Debug.Log("🔙 Back button clicked - cleaning up AR and data structures...");
        
        // Cleanup AR Optimizer
        UniversalARPlacementOptimizer optimizer = FindObjectOfType<UniversalARPlacementOptimizer>();
        if (optimizer != null)
        {
            optimizer.ManualCleanup();
            Debug.Log("✅ AR Optimizer cleaned up");
        }
        
        // Cleanup Queue Manager
        QueueManager queueManager = FindObjectOfType<QueueManager>();
        if (queueManager != null)
        {
            queueManager.Clear();
            Debug.Log("✅ Queue Manager cleaned up");
        }
        
        // Cleanup Stack Visualizer
        StackVisualizer stackVisualizer = FindObjectOfType<StackVisualizer>();
        if (stackVisualizer != null)
        {
            stackVisualizer.Clear();
            Debug.Log("✅ Stack Visualizer cleaned up");
        }
        
        // Cleanup Tree Visualizer
        TreeVisualizer treeVisualizer = FindObjectOfType<TreeVisualizer>();
        if (treeVisualizer != null)
        {
            treeVisualizer.Clear();
            Debug.Log("✅ Tree Visualizer cleaned up");
        }
        
        // Cleanup Graph Visualizer
        GraphVisualizer graphVisualizer = FindObjectOfType<GraphVisualizer>();
        if (graphVisualizer != null)
        {
            graphVisualizer.Clear();
            Debug.Log("✅ Graph Visualizer cleaned up");
        }
        
        // Cleanup Linked List Visualizer
        LinkedListVisualizer linkedListVisualizer = FindObjectOfType<LinkedListVisualizer>();
        if (linkedListVisualizer != null)
        {
            linkedListVisualizer.Clear();
            Debug.Log("✅ Linked List Visualizer cleaned up");
        }
        
        // Load the target scene
        Debug.Log($"🔄 Loading scene: {targetSceneName}");
        SceneManager.LoadScene(targetSceneName);
    }
    
    /// <summary>
    /// Alternative method if you want to specify scene name directly
    /// </summary>
    public void LoadScene(string sceneName)
    {
        targetSceneName = sceneName;
        OnBackButtonClicked();
    }
    
    /// <summary>
    /// Quick exit without specifying scene (uses default targetSceneName)
    /// </summary>
    public void ExitToMainMenu()
    {
        OnBackButtonClicked();
    }
}