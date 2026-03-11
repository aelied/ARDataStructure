using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static string targetPanel = "";

    // Load scene by name
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Load MainMenu specifically
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // ── Back button from AR scene ─────────────────────────────────────────
    // Sets AR_JustReturned so ARReturnHandler reopens TopicCardPanel
    // with the lessons list and starts the Easy quiz.
    public void LoadARPanel()
    {
        // Signal to ARReturnHandler that we just came back from AR
        PlayerPrefs.SetInt(ARReturnHandler.AR_JUST_RETURNED_KEY, 1);

        // Save the current scene name so ARReturnHandler can restore AR mode
        PlayerPrefs.SetString("AR_SceneName", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();

        targetPanel = "AR";
        SceneManager.LoadScene("MainMenu");
    }

    // Reload current scene
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Quit application
    public void QuitApp()
    {
        Application.Quit();
        Debug.Log("App quit!");
    }
}