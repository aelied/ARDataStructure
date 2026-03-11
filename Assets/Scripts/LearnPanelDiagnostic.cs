using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to any persistent GameObject in the scene (e.g. the Canvas).
/// It hooks into BottomNavigation's Learn tab click and dumps a full
/// visibility report 1 frame after the tab activates — exactly when the
/// blank screen appears.
///
/// READ THE LOGS TAGGED [NAV-DEBUG] to find the root cause.
/// </summary>
public class LearnPanelDiagnostic : MonoBehaviour
{
    [Header("Assign these in Inspector")]
    public GameObject learnPanelRoot;          // the LearnPanel root GO
    public GameObject topicSelectionPanel;     // TopicSelectionPanel inside LearnPanel
    public GameObject topicCardPanelRoot;      // TopicCardPanel (TopicPanelBridge.myPanelRoot)

    private BottomNavigation bottomNav;

    void Start()
    {
        bottomNav = FindObjectOfType<BottomNavigation>();

        if (bottomNav == null)
        {
            Debug.LogError("[NAV-DEBUG] BottomNavigation not found!");
            return;
        }

        // Hook the Learn button
        if (bottomNav.learnButton?.button != null)
        {
            bottomNav.learnButton.button.onClick.AddListener(OnLearnTabClicked);
            Debug.Log("[NAV-DEBUG] ✅ Hooked Learn tab button");
        }
        else
        {
            Debug.LogError("[NAV-DEBUG] learnButton or its Button component is null!");
        }
    }

    void OnLearnTabClicked()
    {
        Debug.Log("[NAV-DEBUG] ═══════════════════════════════════════════");
        Debug.Log("[NAV-DEBUG] Learn tab CLICKED — dumping state NOW (before BottomNav processes it)");
        DumpFullState("BEFORE");

        // Dump again after 2 frames (same delay BottomNavigation uses)
        StartCoroutine(DumpAfterDelay());
    }

    System.Collections.IEnumerator DumpAfterDelay()
    {
        yield return null;
        yield return null;
        yield return null; // 3 frames — after ResetLearnPanelNextFrame completes

        Debug.Log("[NAV-DEBUG] ═══════════════════════════════════════════");
        Debug.Log("[NAV-DEBUG] 3 frames AFTER Learn tab click — this is what you see on screen");
        DumpFullState("AFTER");
    }

    void DumpFullState(string label)
    {
        Debug.Log($"[NAV-DEBUG] ─── {label} ───────────────────────────────");

        // ── LearnPanel root ──────────────────────────────────────────
        DumpGO("LearnPanelRoot", learnPanelRoot);

        // ── TopicSelectionPanel ──────────────────────────────────────
        DumpGO("TopicSelectionPanel", topicSelectionPanel);

        // Walk every CanvasGroup from TopicSelectionPanel up to root
        if (topicSelectionPanel != null)
        {
            Debug.Log("[NAV-DEBUG]   Ancestor CanvasGroups of TopicSelectionPanel:");
            Transform t = topicSelectionPanel.transform;
            while (t != null)
            {
                CanvasGroup cg = t.GetComponent<CanvasGroup>();
                if (cg != null)
                    Debug.Log($"[NAV-DEBUG]     {t.name} → alpha={cg.alpha:F2}  blocksRaycasts={cg.blocksRaycasts}  interactable={cg.interactable}  active={t.gameObject.activeSelf}");
                else
                    Debug.Log($"[NAV-DEBUG]     {t.name} → (no CanvasGroup)  active={t.gameObject.activeSelf}");
                t = t.parent;
            }
        }

        // ── All CanvasGroups INSIDE TopicSelectionPanel ──────────────
        if (topicSelectionPanel != null)
        {
            Debug.Log("[NAV-DEBUG]   CanvasGroups INSIDE TopicSelectionPanel:");
            foreach (CanvasGroup cg in topicSelectionPanel.GetComponentsInChildren<CanvasGroup>(true))
            {
                Debug.Log($"[NAV-DEBUG]     {cg.gameObject.name} → alpha={cg.alpha:F2}  active={cg.gameObject.activeSelf}  activeInHierarchy={cg.gameObject.activeInHierarchy}");
            }
        }

        // ── TopicCardPanel (Bridge root) ─────────────────────────────
        DumpGO("TopicCardPanelRoot", topicCardPanelRoot);

        // ── UpdatedLearnPanelController state ────────────────────────
        UpdatedLearnPanelController lp = FindObjectOfType<UpdatedLearnPanelController>(true);
        if (lp != null)
        {
            Debug.Log($"[NAV-DEBUG]   LearnPanelController: enabled={lp.enabled}  gameObject.active={lp.gameObject.activeSelf}");
            Debug.Log($"[NAV-DEBUG]   lp.topicSelectionPanel active={lp.topicSelectionPanel?.activeSelf}");

            // Check for PanelAnimator on topicSelectionPanel
            if (lp.topicSelectionPanel != null)
            {
                foreach (var comp in lp.topicSelectionPanel.GetComponents<MonoBehaviour>())
                {
                    string typeName = comp.GetType().Name;
                    Debug.Log($"[NAV-DEBUG]   TopicSelectionPanel component: {typeName}  enabled={comp.enabled}");
                }
            }
        }
        else
        {
            Debug.LogWarning("[NAV-DEBUG]   UpdatedLearnPanelController NOT FOUND");
        }

        // ── TopicPanelBridge state ───────────────────────────────────
        TopicPanelBridge bridge = FindObjectOfType<TopicPanelBridge>(true);
        if (bridge != null)
        {
            Debug.Log($"[NAV-DEBUG]   Bridge.myPanelRoot active={bridge.myPanelRoot?.activeSelf}");
            Debug.Log($"[NAV-DEBUG]   Bridge.controlledByBridge — (check ForceClose log above)");
        }

        Debug.Log($"[NAV-DEBUG] ─── END {label} ──────────────────────────");
    }

    void DumpGO(string label, GameObject go)
    {
        if (go == null)
        {
            Debug.LogWarning($"[NAV-DEBUG]   {label}: NOT ASSIGNED in Inspector!");
            return;
        }

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        Canvas canvas   = go.GetComponent<Canvas>();

        Debug.Log($"[NAV-DEBUG]   {label}:" +
                  $"  activeSelf={go.activeSelf}" +
                  $"  activeInHierarchy={go.activeInHierarchy}" +
                  (cg != null ? $"  CG.alpha={cg.alpha:F2}  blocksRaycasts={cg.blocksRaycasts}" : "  (no CanvasGroup)") +
                  (canvas != null ? $"  Canvas.enabled={canvas.enabled}" : ""));
    }
}