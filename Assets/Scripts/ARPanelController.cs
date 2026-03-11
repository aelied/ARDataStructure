// ================================================================
//  ARPanelController.cs
//
//  Attach this to your ARPanel GameObject.
//
//  What it does:
//  - Each topic card in the AR grid calls OpenTopicFromAR(topicName)
//  - That opens the SAME TopicCardPanel used by Learn, but sets
//    launchedFromARPanel = true and arSceneName on TopicDetailPanel
//  - When the player finishes the last lesson, TopicDetailPanel
//    loads the AR scene instead of showing the completion dialog
//
//  ── SETUP ───────────────────────────────────────────────────────
//  1. Add this component to ARPanel
//  2. Assign all Inspector fields
//  3. For each topic card in ARPanel → ARTopicsView (Array_AR etc.):
//       - Remove any old onClick that loaded the AR scene directly
//       - Add onClick → ARPanelController.OpenTopicFromAR("<TopicName>")
//         e.g. "Stacks", "Queue", "Arrays", "LinkedLists", "Trees", "Graphs"
// ================================================================
using UnityEngine;
using System.Collections.Generic;

public class ARPanelController : MonoBehaviour
{
    [Header("The shared TopicCardPanel (same one Learn uses)")]
    public TopicPanelBridge topicPanelBridge;

    [Header("TopicDetailPanel reference")]
    public TopicDetailPanel topicDetailPanel;

    [Header("AR Scene Names — must match Build Settings exactly")]
    public string arraysARScene      = "Arrays_AR";
    public string stacksARScene      = "Stacks_AR";
    public string queueARScene       = "Queue_AR";
    public string linkedListsARScene = "LinkedLists_AR";
    public string treesARScene       = "Trees_AR";
    public string graphsARScene      = "Graphs_AR";

    // Map topic name → AR scene name
    private Dictionary<string, string> _sceneMap;

    void Awake()
    {
        _sceneMap = new Dictionary<string, string>
        {
            { "Arrays",       arraysARScene      },
            { "Stacks",       stacksARScene      },
            { "Queue",        queueARScene       },
            { "LinkedLists",  linkedListsARScene },
            { "Trees",        treesARScene       },
            { "Graphs",       graphsARScene      },
        };

        // Auto-find references if not assigned
        if (topicPanelBridge  == null) topicPanelBridge  = FindObjectOfType<TopicPanelBridge>();
        if (topicDetailPanel  == null) topicDetailPanel  = FindObjectOfType<TopicDetailPanel>();
    }

    // ── Called by each AR topic card button ──────────────────────
   public void OpenTopicFromAR(string topicName)
{
    Debug.Log($"[ARPanelController] OpenTopicFromAR: {topicName}");

    if (topicPanelBridge == null || topicDetailPanel == null)
    {
        Debug.LogError("[ARPanelController] Missing references!");
        return;
    }

    if (!_sceneMap.TryGetValue(topicName, out string sceneName))
    {
        Debug.LogError($"[ARPanelController] No AR scene mapped for topic: {topicName}");
        return;
    }

    // Use ShowTopicFromAR so launchedFromAR=true is set on the bridge
    // This ensures back button returns to AR topic grid, not Home
    topicPanelBridge.ShowTopicFromAR(topicName, sceneName);
}
    // ── Called when returning from AR panel (e.g. BottomNav) ─────
    //    Resets the flag so normal lesson browsing isn't affected
    public void OnARPanelHidden()
    {
        if (topicDetailPanel != null)
        {
            topicDetailPanel.launchedFromARPanel = false;
            topicDetailPanel.arSceneName         = "";
        }
    }

    void OnDisable()
    {
        OnARPanelHidden();
    }
}