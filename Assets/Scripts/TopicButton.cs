using UnityEngine;
using UnityEngine.UI;

public class TopicButton : MonoBehaviour
{
    [Header("Topic Configuration")]
    public string topicName; // Set in Inspector: "Stacks", "Queues", "LinkedLists", "Trees", "Graphs"
    
    [Header("References")]
    public TopicDetailPanel detailPanel; // Drag TopicDetailPanel here in Inspector
    
    private Button button;
    
    void Start()
    {
        button = GetComponent<Button>();
        
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogError($"Button component not found on {gameObject.name}");
        }
        
        // Validation
        if (detailPanel == null)
        {
            Debug.LogError($"TopicDetailPanel not assigned on {gameObject.name}! Please assign it in the Inspector.");
        }
        
        if (string.IsNullOrEmpty(topicName))
        {
            Debug.LogWarning($"Topic name not set for {gameObject.name}");
        }
    }
    
    void OnButtonClicked()
    {
        if (detailPanel != null && !string.IsNullOrEmpty(topicName))
        {
            Debug.Log($"Opening topic detail for: {topicName}");
            detailPanel.ShowTopicDetail(topicName);
        }
        else
        {
            Debug.LogError($"Cannot open topic. DetailPanel: {detailPanel != null}, TopicName: {topicName}");
        }
    }
}