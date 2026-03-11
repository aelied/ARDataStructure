using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// OpenCodeLabButton
/// ─────────────────
/// Attach this to any Button inside a lesson card / lesson content view.
/// It opens the CodeLabPanel as a full-screen overlay without closing the
/// lesson panels underneath.
///
/// SETUP:
///   1. Add this component to the lesson's "Code Challenge" button GameObject.
///   2. Set topicName in the Inspector, OR call SetTopic() from code before
///      the button is shown (e.g. from TopicDetailPanel when building the lesson).
///   3. Optionally set templateIndex to pre-select a starter template.
/// </summary>
[RequireComponent(typeof(Button))]
public class OpenCodeLabButton : MonoBehaviour
{
    [Tooltip("The topic name to pass to CodeLabPanel (e.g. 'Arrays', 'Stacks')")]
    public string topicName = "";

    [Tooltip("Which starter template to load (0 = Hello World, 1 = Fibonacci, etc.)")]
    public int templateIndex = 0;

    private CodeLabPanel codeLabPanel;

    void Start()
    {
        // Find the CodeLabPanel in the scene (works even if on a disabled GameObject)
        codeLabPanel = FindObjectOfType<CodeLabPanel>(true);

        if (codeLabPanel == null)
        {
            Debug.LogWarning("[OpenCodeLabButton] CodeLabPanel not found in scene!");
            return;
        }

        GetComponent<Button>().onClick.AddListener(OpenCodeLab);
    }

    /// <summary>
    /// Call this from TopicDetailPanel when spawning / configuring a lesson card
    /// so the button knows which topic it belongs to.
    /// </summary>
    public void SetTopic(string topic, int tmplIndex = 0)
    {
        topicName     = topic;
        templateIndex = tmplIndex;
    }

    void OpenCodeLab()
    {
        if (codeLabPanel == null)
            codeLabPanel = FindObjectOfType<CodeLabPanel>(true);

        if (codeLabPanel != null)
            codeLabPanel.OpenChallenge(topicName, templateIndex);
        else
            Debug.LogError("[OpenCodeLabButton] Cannot open CodeLab — panel not found.");
    }
}