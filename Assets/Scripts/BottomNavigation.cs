using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BottomNavigation : MonoBehaviour
{
    [System.Serializable]
    public class NavButton
    {
        public Button button;
        public Image iconImage;
        public TextMeshProUGUI label;
        public Sprite inactiveIcon;
        public Sprite activeIcon;
        public GameObject panel;

        [HideInInspector] public RectTransform iconTransform;
    }

    [Header("Navigation Buttons")]
    public NavButton homeButton;
    public NavButton learnButton;
    public NavButton progressButton;
    public NavButton arButton;
    public NavButton profileButton;

    [Header("Label Colors")]
    public Color activeColor   = new Color(0.3f, 0.6f, 1f);
    public Color inactiveColor = new Color(0.6f, 0.6f, 0.6f);

    [Header("Animation Settings")]
    public float scaleAmount       = 1.15f;
    public float animationDuration = 0.2f;

    private NavButton currentActiveButton;
    private NavButton previousActiveButton;

    // ─────────────────────────────────────────────────────────────────
    void Start()
    {
        CacheIconTransform(homeButton);
        CacheIconTransform(learnButton);
        CacheIconTransform(progressButton);
        CacheIconTransform(arButton);
        CacheIconTransform(profileButton);

        if (homeButton.button     != null) homeButton.button.onClick.AddListener(()     => SwitchToTab(homeButton));
        if (learnButton.button    != null) learnButton.button.onClick.AddListener(()    => SwitchToTab(learnButton));
        if (progressButton.button != null) progressButton.button.onClick.AddListener(() => SwitchToTab(progressButton));
        if (arButton.button       != null) arButton.button.onClick.AddListener(()       => SwitchToTab(arButton));
        if (profileButton.button  != null) profileButton.button.onClick.AddListener(()  => SwitchToTab(profileButton));

        SwitchToTab(homeButton);
    }

    void CacheIconTransform(NavButton navButton)
    {
        if (navButton != null && navButton.iconImage != null)
            navButton.iconTransform = navButton.iconImage.GetComponent<RectTransform>();
    }

    // ─────────────────────────────────────────────────────────────────
    void SwitchToTab(NavButton navButton)
    {
        if (navButton == null || navButton.button == null) return;

        previousActiveButton = currentActiveButton;

        CloseAnyOpenPanels();

        DeactivateButton(homeButton);
        DeactivateButton(learnButton);
        DeactivateButton(progressButton);
        DeactivateButton(arButton);
        DeactivateButton(profileButton);

        ActivateButton(navButton);
        currentActiveButton = navButton;

        HideAllPanels();

        if (navButton.panel != null)
            navButton.panel.SetActive(true);

        // If switching to learn tab, reset the CodeLabPanel
        if (navButton == learnButton && navButton.panel != null)
            StartCoroutine(ResetLearnPanelNextFrame(navButton.panel));
    }

   IEnumerator ResetLearnPanelNextFrame(GameObject learnPanelRoot)
{
    yield return null;
    yield return null;

    TopicPanelBridge bridge = FindObjectOfType<TopicPanelBridge>();
    if (bridge != null) bridge.ForceClose();

    CodeLabPanel codeLab = FindObjectOfType<CodeLabPanel>(true);
    if (codeLab != null) codeLab.HideResultPanel();

    UpdatedLearnPanelController lp = FindObjectOfType<UpdatedLearnPanelController>(true);
    if (lp == null) yield break;

    // Only reset to topic selection if we're coming FROM another tab
    // If previousActiveButton was also learnButton, we're navigating
    // within the learn tab — don't reset
    bool returningFromOtherTab = (previousActiveButton != learnButton);

    if (returningFromOtherTab && !lp.isCodeLabOpen)
    {
        Debug.Log("[BottomNav] Returning from other tab — resetting to topic selection");
        lp.CloseCodeLab();
        lp.ResetTopicCardVisibility();
        lp.ShowTopicSelection();
    }
    else
    {
        Debug.Log("[BottomNav] Staying within learn tab or CodeLab open — no reset");
    }

}

    // ─────────────────────────────────────────────────────────────────
   void CloseAnyOpenPanels()
{
    // ── CodeLab overlay ───────────────────────────────────────────────────────
    CodeLabPanel codeLab = FindObjectOfType<CodeLabPanel>(true);
    if (codeLab != null)
        codeLab.ForceClose();   // hides overlay + notifies learnPanelController

    // ── TopicPanelBridge ──────────────────────────────────────────────────────
    TopicPanelBridge bridge = FindObjectOfType<TopicPanelBridge>();
    if (bridge != null)
    {
        bridge.ForceClose();
        Debug.Log("[BottomNav] Closed TopicPanelBridge via ForceClose()");
    }

    // ── LessonContentPanel inside TopicDetailPanel ────────────────────────────
    TopicDetailPanel detailPanel = FindObjectOfType<TopicDetailPanel>();
    if (detailPanel != null && detailPanel.lessonContentPanel != null
                            && detailPanel.lessonContentPanel.activeSelf)
    {
        detailPanel.lessonContentPanel.SetActive(false);
        Debug.Log("[BottomNav] Hid lessonContentPanel");
    }

    // ── Challenge panels ──────────────────────────────────────────────────────
    ChallengeManager cm = FindObjectOfType<ChallengeManager>();
    if (cm != null)
    {
        cm.ForceHideAllPanels();
        Debug.Log("[BottomNav] Hid challenge panels");
    }

    // ── Interactive visualizer ────────────────────────────────────────────────
    Interactive2DVisualizer visualizer = FindObjectOfType<Interactive2DVisualizer>();
    if (visualizer != null)
    {
        visualizer.HideVisualization();
        Debug.Log("[BottomNav] Hid visualizer");
    }

    Debug.Log("[BottomNav] CloseAnyOpenPanels complete");
}


    // ─────────────────────────────────────────────────────────────────
    void ActivateButton(NavButton navButton)
    {
        if (navButton == null) return;
        if (navButton.iconImage != null && navButton.activeIcon != null)
        {
            navButton.iconImage.sprite = navButton.activeIcon;
            navButton.iconImage.color  = Color.white;
        }
        if (navButton.label != null)
            navButton.label.color = activeColor;
        if (navButton.iconTransform != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateIconScale(navButton.iconTransform, scaleAmount));
        }
    }

    void DeactivateButton(NavButton navButton)
    {
        if (navButton == null) return;
        if (navButton.iconImage != null && navButton.inactiveIcon != null)
        {
            navButton.iconImage.sprite = navButton.inactiveIcon;
            navButton.iconImage.color  = Color.white;
        }
        if (navButton.label != null)
            navButton.label.color = inactiveColor;
        if (navButton.iconTransform != null)
            StartCoroutine(AnimateIconScale(navButton.iconTransform, 1f));
    }

    public void SetActiveTabExternally(NavButton navButton)
    {
        if (navButton == null) return;

        DeactivateButton(homeButton);
        DeactivateButton(learnButton);
        DeactivateButton(progressButton);
        DeactivateButton(arButton);
        DeactivateButton(profileButton);

        ActivateButton(navButton);
        currentActiveButton = navButton;

        Debug.Log($"[BottomNav] SetActiveTabExternally: {navButton.button?.gameObject.name}");
    }

    public void SwitchToLearnTab()
    {
        SwitchToTab(learnButton);
    }

    public void SwitchToARTab()
{
    SwitchToTab(arButton);
}

    IEnumerator AnimateIconScale(RectTransform iconTransform, float targetScale)
    {
        Vector3 startScale = iconTransform.localScale;
        Vector3 endScale   = Vector3.one * targetScale;
        float   elapsed    = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / animationDuration;
            t = 1f - Mathf.Pow(1f - t, 3f);
            iconTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        iconTransform.localScale = endScale;
    }

    void HideAllPanels()
    {
        if (homeButton.panel     != null) homeButton.panel.SetActive(false);
        if (learnButton.panel    != null) learnButton.panel.SetActive(false);
        if (progressButton.panel != null) progressButton.panel.SetActive(false);
        if (arButton.panel       != null) arButton.panel.SetActive(false);
        if (profileButton.panel  != null) profileButton.panel.SetActive(false);
    }
}