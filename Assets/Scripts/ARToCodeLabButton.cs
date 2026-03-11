using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// LessonCodeButton
/// ────────────────
/// Attach to the single "Code" button inside the lesson content view.
/// It reads the current topic name directly from TopicDetailPanel so
/// you never need to set anything manually — just drop this on the button.
///
/// SETUP:
///   1. Add this component to your lesson content's "Code" Button GameObject.
///   2. That's it. No Inspector fields need to be filled.
/// </summary>
[RequireComponent(typeof(Button))]
public class LessonCodeButton : MonoBehaviour
{
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnCodeButtonPressed);
    }

    void OnCodeButtonPressed()
    {
        // Get the current topic name from whatever lesson is open
        TopicDetailPanel detailPanel = FindObjectOfType<TopicDetailPanel>(true);
        string topicName = (detailPanel != null) ? detailPanel.GetCurrentTopicName() : "";

        CodeLabPanel codeLab = FindObjectOfType<CodeLabPanel>(true);
        if (codeLab != null)
            codeLab.OpenChallenge(topicName);
        else
            Debug.LogError("[LessonCodeButton] CodeLabPanel not found in scene.");
    }
}