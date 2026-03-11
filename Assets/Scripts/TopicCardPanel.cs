using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// =====================================================================
///  TopicCardPanel.cs  –  "Topics" card list panel
///  Matches the dark-navy / purple-gradient design.
/// =====================================================================
///
///  REQUIRED HIERARCHY  (build this in Unity, then assign in Inspector)
/// ─────────────────────────────────────────────────────────────────────
///  TopicCardPanel  (this script lives here)
///  ├─ Header
///  │   ├─ BackButton            (Button)
///  │   ├─ HeaderLabel           (TMP – "CHALLENGE")
///  │   ├─ TopicTitleText        (TMP – "Stacks")
///  │   ├─ TopicSubtitleText     (TMP – "Master the topic")
///  │   └─ HeaderDecoImage       (Image – isometric art)
///  ├─ Body  (ScrollRect or plain RectTransform)
///  │   ├─ SectionIcon           (TMP – emoji bookmark)
///  │   ├─ SectionTitleText      (TMP – "Topics")
///  │   └─ TopicCardsContainer   (Vertical Layout Group)
///  │       └─ [TopicCardPrefab instantiated here]
///  └─ BottomNav
///      ├─ HomeNavItem           (Button)
///      ├─ LearnNavItem          (Button)  ← active
///      ├─ ARButton              (Button)  ← big floating circle
///      ├─ ProgressNavItem       (Button)
///      └─ ProfileNavItem        (Button)
///
///  TOPIC CARD PREFAB  (create as a Prefab, assign to topicCardPrefab)
///  ─────────────────────────────────────────────────────────────────
///  TopicCardRoot  (Image – card background, Button, HorizontalLayoutGroup)
///  ├─ IconBox     (Image – rounded square)
///  │   └─ IconImage (Image – topic icon sprite)
///  ├─ ContentGroup (Vertical Layout Group)
///  │   ├─ TitleText      (TMP)
///  │   ├─ DescText       (TMP)
///  │   └─ PillGroup      (HorizontalLayoutGroup)
///  │       ├─ PillBg     (Image)
///  │       └─ PillText   (TMP – "5 Lessons")
///  ├─ ArrowText   (TMP – "›")
///  └─ ProgressCircle  (custom or Image with filled radial)
///      └─ ProgressPctText (TMP – "75%")
/// =====================================================================
public class TopicCardPanel : MonoBehaviour
{
    // ── Inspector references ─────────────────────────────────────────
    [Header("Panel Root")]
    public GameObject panelRoot;

    [Header("Header")]
    public Button            backButton;
    public TextMeshProUGUI   headerLabelText;     // "CHALLENGE"
    public TextMeshProUGUI   topicTitleText;      // "Stacks"
    public TextMeshProUGUI   topicSubtitleText;   // "Master the topic"
    public Image             headerDecoImage;

    [Header("Body")]
    public TextMeshProUGUI   sectionTitleText;    // "Topics"
    public Transform         cardsContainer;      // Vertical layout group
    public GameObject        topicCardPrefab;     // Prefab to instantiate

    [Header("Bottom Nav")]
    public Button  homeNavButton;
    public Button  learnNavButton;
    public Button  arNavButton;
    public Button  progressNavButton;
    public Button  profileNavButton;
    public Image   learnNavActiveIndicator;       // underline / highlight

    [Header("Colors – tweak here!")]
    public Color cardBgColorA  = new Color(0.11f, 0.13f, 0.38f); // dark blue
    public Color cardBgColorB  = new Color(0.16f, 0.18f, 0.50f); // mid blue
    public Color pillBgColor   = new Color(0.31f, 0.39f, 1.00f, 0.25f);
    public Color progressColor = new Color(0.49f, 0.36f, 0.99f); // purple
    public Color progressBgColor = new Color(1f, 1f, 1f, 0.08f);
    public Color cardTextWhite = new Color(0.94f, 0.95f, 1.00f);
    public Color cardTextMuted = new Color(0.53f, 0.55f, 0.75f);

    [Header("Header Gradient")]
    public Color headerColorA = new Color(0.10f, 0.06f, 0.31f);
    public Color headerColorB = new Color(0.42f, 0.25f, 0.78f);

    // ── Data model ───────────────────────────────────────────────────
    [System.Serializable]
    public class TopicCardData
    {
        public string topicName;
        public string title;
        public string description;
        public int    lessonCount;
        [Range(0f, 1f)]
        public float  progressPercent;   // 0 → 1
        public Sprite iconSprite;        // assign in code or inspector
    }

    // ── Private state ────────────────────────────────────────────────
    private string              currentContextName;   // e.g. "Stacks"
    private List<TopicCardData> currentCards;
    private List<GameObject>    spawnedCards = new List<GameObject>();

    // Callbacks
    public System.Action<TopicCardData> onTopicCardClicked;
    public System.Action               onBackClicked;
    public System.Action               onARClicked;

    // ════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ════════════════════════════════════════════════════════════════
    void Awake()
    {
        if (backButton         != null) backButton.onClick.AddListener(HandleBack);
        if (homeNavButton      != null) homeNavButton.onClick.AddListener(()=>SetActiveNav(0));
        if (learnNavButton     != null) learnNavButton.onClick.AddListener(()=>SetActiveNav(1));
        if (arNavButton        != null) arNavButton.onClick.AddListener(HandleAR);
        if (progressNavButton  != null) progressNavButton.onClick.AddListener(()=>SetActiveNav(3));
        if (profileNavButton   != null) profileNavButton.onClick.AddListener(()=>SetActiveNav(4));

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════
    //  PUBLIC API  –  call these from UpdatedLearnPanelController
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Show the panel with a context name (e.g. "Stacks") and a list of cards.
    /// </summary>
    public void ShowPanel(string contextName, List<TopicCardData> cards)
    {
        currentContextName = contextName;
        currentCards       = cards;

        if (panelRoot != null) panelRoot.SetActive(true);

        // Header
        if (headerLabelText   != null) headerLabelText.text   = "CHALLENGE";
        if (topicTitleText    != null) topicTitleText.text    = contextName;
        if (topicSubtitleText != null) topicSubtitleText.text = "Master the topic";
        if (sectionTitleText  != null) sectionTitleText.text  = "Topics";

        RebuildCards();
        SetActiveNav(1); // Learn is active by default
    }

    /// <summary>Hide and clean up.</summary>
    public void HidePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        ClearCards();
    }

    /// <summary>Refresh progress values without rebuilding the whole list.</summary>
    public void RefreshProgress(List<TopicCardData> updatedCards)
    {
        currentCards = updatedCards;
        for (int i = 0; i < spawnedCards.Count && i < updatedCards.Count; i++)
        {
            UpdateCardProgress(spawnedCards[i], updatedCards[i].progressPercent);
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  CARD BUILDING
    // ════════════════════════════════════════════════════════════════
    void RebuildCards()
    {
        ClearCards();
        if (cardsContainer == null || topicCardPrefab == null || currentCards == null) return;

        for (int i = 0; i < currentCards.Count; i++)
        {
            GameObject card = Instantiate(topicCardPrefab, cardsContainer);
            card.SetActive(true);
            ConfigureCard(card, currentCards[i], i);
            spawnedCards.Add(card);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardsContainer as RectTransform);
    }

    void ClearCards()
    {
        foreach (var c in spawnedCards)
            if (c != null) Destroy(c);
        spawnedCards.Clear();

        // Also destroy any leftover children
        if (cardsContainer != null)
            foreach (Transform t in cardsContainer)
                Destroy(t.gameObject);
    }

    void ConfigureCard(GameObject card, TopicCardData data, int index)
    {
        // ── Background gradient simulation (use a single Image + script) ──
        Image cardBg = card.GetComponent<Image>();
        if (cardBg != null)
            cardBg.color = Color.Lerp(cardBgColorA, cardBgColorB, (index % 2) * 0.5f);

        // ── Icon ──────────────────────────────────────────────────────────
        Image iconImg = FindDeep<Image>(card, "IconImage");
        if (iconImg != null && data.iconSprite != null)
            iconImg.sprite = data.iconSprite;

        // ── Title ─────────────────────────────────────────────────────────
        TextMeshProUGUI titleTMP = FindDeep<TextMeshProUGUI>(card, "TitleText");
        if (titleTMP != null)
        {
            titleTMP.text  = data.title;
            titleTMP.color = cardTextWhite;
        }

        // ── Description ───────────────────────────────────────────────────
        TextMeshProUGUI descTMP = FindDeep<TextMeshProUGUI>(card, "DescText");
        if (descTMP != null)
        {
            descTMP.text  = data.description;
            descTMP.color = cardTextMuted;
        }

        // ── Pill (lesson count) ───────────────────────────────────────────
        TextMeshProUGUI pillTMP = FindDeep<TextMeshProUGUI>(card, "PillText");
        if (pillTMP != null)
            pillTMP.text = $"📚 {data.lessonCount} Lessons";

        Image pillBg = FindDeep<Image>(card, "PillBg");
        if (pillBg != null)
            pillBg.color = pillBgColor;

        // ── Progress circle ───────────────────────────────────────────────
        UpdateCardProgress(card, data.progressPercent);

        // ── Click handler ─────────────────────────────────────────────────
        Button btn = card.GetComponent<Button>();
        if (btn != null)
        {
            var capturedData = data;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onTopicCardClicked?.Invoke(capturedData));
        }
    }

    void UpdateCardProgress(GameObject card, float progress01)
    {
        // Progress label (percentage text)
        TextMeshProUGUI pctTMP = FindDeep<TextMeshProUGUI>(card, "ProgressPctText");
        if (pctTMP != null)
            pctTMP.text = $"{Mathf.RoundToInt(progress01 * 100f)}%";

        // Radial fill – works with a UI Image set to "Filled" > "Radial 360"
        Image radial = FindDeep<Image>(card, "ProgressFill");
        if (radial != null)
        {
            radial.fillAmount = progress01;
            radial.color      = progressColor;
        }

        // Background ring color
        Image radialBg = FindDeep<Image>(card, "ProgressBg");
        if (radialBg != null)
            radialBg.color = progressBgColor;
    }

    // ════════════════════════════════════════════════════════════════
    //  NAV BAR
    // ════════════════════════════════════════════════════════════════
    Button[] NavButtons => new[]
        { homeNavButton, learnNavButton, null, progressNavButton, profileNavButton };

    void SetActiveNav(int activeIndex)
    {
        // You can expand this to visually highlight the active item.
        // For now it fires the callbacks and highlights learnNavActiveIndicator.
        bool learnActive = (activeIndex == 1);
        if (learnNavActiveIndicator != null)
            learnNavActiveIndicator.gameObject.SetActive(learnActive);
    }

    // ════════════════════════════════════════════════════════════════
    //  BUTTON HANDLERS
    // ════════════════════════════════════════════════════════════════
    void HandleBack()
    {
        HidePanel();
        onBackClicked?.Invoke();
    }

    void HandleAR()
    {
        onARClicked?.Invoke();
        Debug.Log("📷 AR button tapped");
    }

    // ════════════════════════════════════════════════════════════════
    //  UTILITY: deep child search by name
    // ════════════════════════════════════════════════════════════════
    T FindDeep<T>(GameObject root, string childName) where T : Component
    {
        Transform found = FindChildRecursive(root.transform, childName);
        return found != null ? found.GetComponent<T>() : null;
    }

    Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}