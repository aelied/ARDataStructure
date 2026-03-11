using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ARLessonLauncher.cs
/// ═══════════════════
/// Put this on any persistent GameObject in your MAIN app scene.
/// Call the static methods from TopicDetailPanel when the student
/// taps the AR button for a specific lesson.
/// </summary>
public class ARLessonLauncher : MonoBehaviour
{
    // ── Lesson mode constants ──────────────────────────────────────
    public const int ARRAYS_L13 = 13;   // Lessons 1, 2, 3
    public const int ARRAYS_L4  = 4;    // Lesson 4  – Traversal
    public const int ARRAYS_L56 = 56;   // Lessons 5, 6 – Operations
    public const int ARRAYS_L7  = 7;    // Lesson 7  – Advantages / Limitations
    public const int ARRAYS_L89 = 89;   // Lessons 8, 9 – Complexity

    // ── Static launcher ───────────────────────────────────────────
    /// <summary>
    /// Call this when a lesson's AR button is tapped.
    /// </summary>
    public static void LaunchLesson(
        string topicName,
        int    lessonNumber,
        string lessonTitle,
        int    lessonIndex,
        string arSceneName)
    {
        PlayerPrefs.SetInt   ("AR_IsLessonMode", 1);
        PlayerPrefs.SetInt   ("AR_LessonNumber", lessonNumber);
        PlayerPrefs.SetString("AR_TopicName",    topicName);
        PlayerPrefs.SetString("AR_LessonTitle",  lessonTitle);
        PlayerPrefs.SetInt   ("AR_LessonIndex",  lessonIndex);
        PlayerPrefs.SetString("AR_SceneName",    arSceneName);
        PlayerPrefs.Save();
        SceneManager.LoadScene(arSceneName);
    }

    /// <summary>
    /// Call this from the AR Panel topic grid — no lesson mode, original AR only.
    /// </summary>
    public static void LaunchStandalone(string arSceneName)
    {
        PlayerPrefs.SetInt("AR_IsLessonMode", 0);
        PlayerPrefs.DeleteKey("AR_LessonNumber");
        PlayerPrefs.Save();
        SceneManager.LoadScene(arSceneName);
    }

    // ── Instance method for Inspector UnityEvents ─────────────────
    /// <summary>
    /// Inspector-friendly version. Drag this component into a Button's
    /// onClick event, then pick LaunchLessonAR and fill in the params.
    /// </summary>
    public void LaunchLessonAR(string packed)
    {
        // packed format: "topicName|lessonNumber|lessonTitle|lessonIndex|arSceneName"
        var parts = packed.Split('|');
        if (parts.Length < 5) { Debug.LogError("[ARLessonLauncher] Bad packed string: " + packed); return; }
        LaunchLesson(parts[0], int.Parse(parts[1]), parts[2], int.Parse(parts[3]), parts[4]);
    }
}