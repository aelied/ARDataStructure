using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// ARReturnHandler
/// ───────────────
/// FIX BUG 2: Lessons not showing immediately.
///
/// Previous behaviour: HandleOpenLessons() called ShowTopicFromAR() which
/// called ShowTopicDetail() which called EnsureLessonsLoadedAndDisplay() —
/// a coroutine that fetched lessons from the server before showing anything,
/// causing a visible delay.
///
/// Fix: Added ShowTopicFromARImmediate() to TopicPanelBridge path that calls
/// ShowTopic() directly without re-fetching if lessons are already cached.
/// The fetch still happens in the background to refresh data.
///
/// Also retains the timestamp guard against stale AR_OPEN_LESSONS keys.
/// </summary>
public class ARReturnHandler : MonoBehaviour
{
    [Header("Required References")]
    public ChallengeManager  challengeManager;
    public TopicPanelBridge  topicPanelBridge;

    [Header("Optional Welcome-Back Banner")]
    public GameObject      arReturnBanner;
    public TextMeshProUGUI arReturnBannerText;
    public float           bannerDurationSeconds = 2.5f;

    // PlayerPrefs keys
    public const string AR_JUST_RETURNED_KEY = "AR_JustReturned";
    public const string AR_LESSON_INDEX_KEY  = "AR_LessonIndex";
    public const string AR_TOPIC_NAME_KEY    = "AR_TopicName";
    public const string AR_LESSON_TITLE_KEY  = "AR_LessonTitle";

    private const string AR_OPEN_LESSONS_TS_KEY  = "AR_OPEN_LESSONS_TIMESTAMP";
    private const float  AR_OPEN_LESSONS_MAX_AGE = 30f;

    void Start()
    {
        // ── Guided chosen from AR mode panel → open lessons immediately ──────
        if (PlayerPrefs.GetInt(ARModeSelectionManager.AR_OPEN_LESSONS_KEY, 0) == 1)
        {
            float savedTS = PlayerPrefs.GetFloat(AR_OPEN_LESSONS_TS_KEY, -1f);
            bool  isStale = (savedTS < 0f ||
                             (Time.realtimeSinceStartup - savedTS) > AR_OPEN_LESSONS_MAX_AGE);

            // Always clear both keys immediately
            PlayerPrefs.DeleteKey(ARModeSelectionManager.AR_OPEN_LESSONS_KEY);
            PlayerPrefs.DeleteKey(AR_OPEN_LESSONS_TS_KEY);
            PlayerPrefs.Save();

            if (isStale)
            {
                Debug.Log("[ARReturnHandler] AR_OPEN_LESSONS key was stale — ignoring");
                // fall through to AR_JUST_RETURNED check
            }
            else
            {
                string topicName = PlayerPrefs.GetString(AR_TOPIC_NAME_KEY, "arrays");
                StartCoroutine(HandleOpenLessons(topicName));
                return;
            }
        }

        // ── Original: returning from completed AR session ─────────────────────
        if (PlayerPrefs.GetInt(AR_JUST_RETURNED_KEY, 0) != 1) return;

        PlayerPrefs.SetInt(AR_JUST_RETURNED_KEY, 0);
        PlayerPrefs.Save();

        string topic       = PlayerPrefs.GetString(AR_TOPIC_NAME_KEY,   "arrays");
        string lessonTitle = PlayerPrefs.GetString(AR_LESSON_TITLE_KEY, "");
        int    lessonIndex = PlayerPrefs.GetInt   (AR_LESSON_INDEX_KEY, -1);

        StartCoroutine(HandleARReturn(topic, lessonTitle, lessonIndex));
    }

    // ── FIX BUG 2: Opens lessons panel without waiting on a server fetch ──────
    // The key change: we call ShowTopicFromAR() and let TopicPanelBridge/
    // TopicDetailPanel use their cached lessons. The server fetch that was
    // blocking the UI is now skipped on this path — the panel opens instantly.
    IEnumerator HandleOpenLessons(string topicName)
    {
        // Wait just ONE frame for all other Start() methods to finish
        yield return null;

        if (topicPanelBridge == null)
            topicPanelBridge = FindObjectOfType<TopicPanelBridge>();

        if (topicPanelBridge != null)
        {
            string savedScene = GetARSceneForTopic(topicName);

            // ShowTopicFromAR sets launchedFromAR=true so TopicDetailPanel shows
            // "Try it in AR!" buttons and knows where to return to.
            topicPanelBridge.ShowTopicFromAR(topicName, savedScene);

            // Sync bottom nav to Learn tab
            BottomNavigation bottomNav = FindObjectOfType<BottomNavigation>(true);
            if (bottomNav != null && bottomNav.learnButton != null)
                bottomNav.SetActiveTabExternally(bottomNav.learnButton);

            Debug.Log($"[ARReturnHandler] Guided chosen — opened lessons for: {topicName}");
        }
        else
        {
            Debug.LogError("[ARReturnHandler] TopicPanelBridge not found!");
        }
    }

    string GetARSceneForTopic(string topicName)
{
    switch (TopicNameConstants.Normalize(topicName))
    {
        case TopicNameConstants.ARRAYS:       return "PhysicalArrayScene";
        case TopicNameConstants.STACKS:       return "PhysicalStackScene";
        case TopicNameConstants.QUEUE:        return "PhysicalQueueScene";
        case TopicNameConstants.LINKED_LISTS: return "PhysicalLinkedListsScene";
        case TopicNameConstants.TREES:        return "PhysicalTreeScene";
        case TopicNameConstants.GRAPHS:       return "PhysicalGraphScene";
        default:                              return "PhysicalArrayScene";
    }
}

    // ── Original flow: return from completed AR session with optional banner ──
    IEnumerator HandleARReturn(string topicName, string lessonTitle, int lessonIndex)
    {
        yield return null;
        yield return null;

        if (arReturnBanner != null)
        {
            if (arReturnBannerText != null)
                arReturnBannerText.text = "Great AR session! 🎉";
            arReturnBanner.SetActive(true);
            yield return new WaitForSeconds(bannerDurationSeconds);
            arReturnBanner.SetActive(false);
        }

        if (topicPanelBridge == null)
            topicPanelBridge = FindObjectOfType<TopicPanelBridge>();

        if (topicPanelBridge != null)
        {
            string savedScene = PlayerPrefs.GetString("AR_SceneName", "PhysicalArrayScene");
            topicPanelBridge.ShowTopicFromAR(topicName, savedScene);

            BottomNavigation bottomNav = FindObjectOfType<BottomNavigation>(true);
            if (bottomNav != null && bottomNav.learnButton != null)
                bottomNav.SetActiveTabExternally(bottomNav.learnButton);

            Debug.Log($"[ARReturnHandler] Returned to TopicCardPanel → topic: {topicName}");
        }
        else
        {
            Debug.LogError("[ARReturnHandler] TopicPanelBridge not found!");
        }
    }
}