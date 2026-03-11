using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ⭐ NEW SCRIPT: Attach this to a GameObject in your MainMenu scene
/// This ensures proper cleanup when switching between AR topics
/// </summary>
public class SceneCleanupManager : MonoBehaviour
{
    private static SceneCleanupManager instance;
    
    void Awake()
    {
        // Singleton pattern - keep this manager alive across scenes
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Subscribe to scene change events
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            
            Debug.Log("✅ SceneCleanupManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🔄 Scene loaded: {scene.name}");
        
        // If loading an AR scene, ensure clean state
        if (IsARScene(scene.name))
        {
            // Give AR system time to initialize
            StartCoroutine(InitializeARScene());
        }
    }
    
    void OnSceneUnloaded(Scene scene)
    {
        Debug.Log($"🗑️ Scene unloaded: {scene.name}");
        
        // Clean up any AR-related objects
        if (IsARScene(scene.name))
        {
            CleanupARScene();
        }
    }
    
    System.Collections.IEnumerator InitializeARScene()
    {
        // Wait for scene to fully load
        yield return new WaitForSeconds(0.2f);
        
        // Find and initialize AR components
        var arOptimizer = FindObjectOfType<UniversalARPlacementOptimizer>();
        if (arOptimizer != null)
        {
            Debug.Log("✅ AR Optimizer found and ready");
        }
        
        // Hide any leftover UI elements from previous scenes
        HideLeftoverUI();
    }
    
    void CleanupARScene()
    {
        // Find AR optimizer and cleanup
        var arOptimizer = FindObjectOfType<UniversalARPlacementOptimizer>();
        if (arOptimizer != null)
        {
            arOptimizer.ManualCleanup();
        }
        
        Debug.Log("🧹 AR Scene cleanup completed");
    }
    
    /// <summary>
    /// ⭐ CRITICAL FIX: Hide leftover UI from previous scenes
    /// </summary>
    void HideLeftoverUI()
    {
        // Find and hide any persistent UI elements
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        
        foreach (GameObject obj in rootObjects)
        {
            // Check for UI panels that might be persistent
            if (obj.name.Contains("Size") || obj.name.Contains("Label") || obj.name.Contains("Info"))
            {
                var canvas = obj.GetComponent<Canvas>();
                if (canvas != null)
                {
                    Debug.Log($"🧹 Hiding leftover UI: {obj.name}");
                    obj.SetActive(false);
                }
            }
        }
    }
    
    bool IsARScene(string sceneName)
    {
        // Check if scene is an AR topic scene
        string lowerName = sceneName.ToLower();
        return lowerName.Contains("stack") || 
               lowerName.Contains("queue") || 
               lowerName.Contains("tree") || 
               lowerName.Contains("graph") ||
               lowerName.Contains("ar");
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
    
    /// <summary>
    /// ⭐ PUBLIC METHOD: Call this before loading a new AR scene
    /// </summary>
    public static void PrepareForNewARScene()
    {
        if (instance != null)
        {
            instance.CleanupARScene();
        }
    }
}