using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ARPanelManager : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject arPanel;          // Parent panel (must stay active)
    public GameObject arPanelContent;   // Scroll View / topic grid
    public GameObject arLearningPanel;  // Intro panel shown first

    [Header("Toggle Button")]
    public Button startButton;          // Button on ARLearningPanel to proceed
    public string startButtonText = "Start Learning";

    [Header("Bridge Reference")]
    public TopicPanelBridge topicPanelBridge;

    [Header("UI References")]
    public GameObject      loadingPanel;
    public TextMeshProUGUI loadingText;

    private static readonly System.Collections.Generic.Dictionary<string, string> TOPIC_SCENE_MAP =
        new System.Collections.Generic.Dictionary<string, string>
        {
            { TopicNameConstants.ARRAYS,       "PhysicalArrayScene" },
            { TopicNameConstants.STACKS,       "PhysicalStackScene"          },
            { TopicNameConstants.QUEUE,        "PhysicalQueueScene"          },
            { TopicNameConstants.LINKED_LISTS, "PhysicalLinkedListsScene"     },
            { TopicNameConstants.TREES,        "PhysicalTreeScene"           },
            { TopicNameConstants.GRAPHS,       "PhysicalGraphScene"          },
            { TopicNameConstants.HASHMAPS,     "HashMaps_Learn"     },
            { TopicNameConstants.HEAPS,        "Heaps_Learn"        },
            { TopicNameConstants.DEQUE,        "Deque_Learn"        },
            { TopicNameConstants.BINARY_HEAPS, "BinaryHeaps_Learn"  },
        };

    private static readonly System.Collections.Generic.Dictionary<string, string> BUTTON_NAME_TO_TOPIC =
        new System.Collections.Generic.Dictionary<string, string>
        {
            { "Array_AR",          TopicNameConstants.ARRAYS       },
            { "Stacks_AR",         TopicNameConstants.STACKS       },
            { "Queues_AR",         TopicNameConstants.QUEUE        },
            { "LinkedLists_AR",    TopicNameConstants.LINKED_LISTS },
            { "Trees_AR",          TopicNameConstants.TREES        },
            { "Graphs_AR",         TopicNameConstants.GRAPHS       },
            { "HashMaps_Learn",    TopicNameConstants.HASHMAPS     },
            { "Heaps_Learn",       TopicNameConstants.HEAPS        },
            { "Deque_Learn",       TopicNameConstants.DEQUE        },
            { "BinaryHeaps_Learn", TopicNameConstants.BINARY_HEAPS },
        };

    private bool isInitialized = false;

    void Start()
    {
        if (topicPanelBridge == null)
            topicPanelBridge = FindObjectOfType<TopicPanelBridge>();

        if (loadingPanel != null) loadingPanel.SetActive(false);

        // Wire start button on learning panel
        if (startButton != null)
        {
            TextMeshProUGUI btnLabel = startButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnLabel != null) btnLabel.text = startButtonText;
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(ShowTopicGrid);
        }

        WireTopicButtons();
        ResetToLearningPanel();
        isInitialized = true;
    }

    void OnEnable()
    {
        // Always return to learning panel intro when tab is opened
        if (isInitialized)
            ResetToLearningPanel();
    }

    // Show intro panel, hide topic grid
    void ResetToLearningPanel()
    {
        if (arPanel         != null) arPanel.SetActive(true);
        if (arLearningPanel != null) arLearningPanel.SetActive(true);
        if (arPanelContent  != null) arPanelContent.SetActive(false);
    }

    // Hide intro panel, show topic grid
    public void ShowTopicGrid()
    {
        if (arLearningPanel != null) arLearningPanel.SetActive(false);
        if (arPanelContent  != null) arPanelContent.SetActive(true);
    }

    public void RestoreTopicGrid()
    {
        if (arLearningPanel != null) arLearningPanel.SetActive(false);
        if (arPanelContent  != null) arPanelContent.SetActive(true);
        if (arPanel         != null) arPanel.SetActive(true);

        // Tell BottomNav that AR tab is active so it doesn't override our panel state
        BottomNavigation bottomNav = FindObjectOfType<BottomNavigation>(true);
        if (bottomNav != null && bottomNav.arButton != null)
            bottomNav.SetActiveTabExternally(bottomNav.arButton);

        Debug.Log("[ARPanelManager] RestoreTopicGrid — panel restored via BottomNav");
    }

    void WireTopicButtons()
    {
        if (arPanelContent == null) return;

        Button[] buttons = arPanelContent.GetComponentsInChildren<Button>(true);
        int wired = 0;

        foreach (Button btn in buttons)
        {
            string btnName = btn.gameObject.name;

            if (BUTTON_NAME_TO_TOPIC.TryGetValue(btnName, out string topicKey))
            {
                if (TOPIC_SCENE_MAP.TryGetValue(topicKey, out string sceneName))
                {
                    string capturedTopic = topicKey;
                    string capturedScene = sceneName;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OpenTopicInBridge(capturedTopic, capturedScene));
                    wired++;
                    Debug.Log($"[ARPanelManager] Wired '{btnName}' → {capturedTopic} / {capturedScene}");

                    // Make the entire card clickable — disable nested child buttons
                    // (e.g. the "Continue" button) and child raycast targets so every
                    // tap anywhere on the card fires only this parent button's onClick.
                    foreach (Button child in btn.GetComponentsInChildren<Button>(true))
                    {
                        if (child == btn) continue;
                        child.onClick.RemoveAllListeners();
                        child.enabled = false;
                    }
                    foreach (Graphic g in btn.GetComponentsInChildren<Graphic>(true))
                    {
                        if (g.gameObject == btn.gameObject) continue;
                        g.raycastTarget = false;
                    }
                }
            }
        }

        Debug.Log($"[ARPanelManager] Total buttons wired: {wired}");
    }

    void OpenTopicInBridge(string topicKey, string arSceneName)
    {
        Debug.Log($"[ARPanelManager] Opening topic={topicKey} scene={arSceneName}");

        if (topicPanelBridge == null)
            topicPanelBridge = FindObjectOfType<TopicPanelBridge>();

        if (topicPanelBridge == null)
        {
            Debug.LogError("[ARPanelManager] TopicPanelBridge not found!");
            return;
        }

        topicPanelBridge.ShowTopicFromARDirect(topicKey, arSceneName, lessonIndex: 0);
    }

    public void LoadARScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (loadingText != null) loadingText.text = $"Loading {sceneName}...";
        }
        SceneManager.LoadScene(sceneName);
    }
}