using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// Automatically builds the entire Tutorial UI at runtime - BIGGER + ROUNDED + PAGINATED (no scroll)
/// </summary>
public class TutorialUIBuilder : MonoBehaviour
{
    [Header("Auto-Build Settings")]
    public bool buildOnStart = true;
    public Canvas targetCanvas;

    [Header("Colors")]
    public Color backgroundColor = new Color(0, 0, 0, 0.85f);
    public Color popupColor = Color.white;
    public Color headerColor = new Color(0.2f, 0.5f, 0.9f);
    public Color buttonColor = new Color(0.3f, 0.6f, 1f);
    public Color hintButtonColor = new Color(0.102f, 0.451f, 0.910f, 1f);

    [Header("Text Size Settings - LARGER")]
    public int titleFontSize = 40;
    public int bodyFontSize = 28;
    public int buttonFontSize = 22;
    public int pageFontSize = 24;

    [Header("Popup Size")]
    public float popupWidth = 850f;
    public float popupHeight = 1050f;
    public int cornerRadius = 32;

    [Header("Generated References (Auto-filled)")]
    public GameObject tutorialPopupPanel;
    public TextMeshProUGUI tutorialTitle;
    public TextMeshProUGUI tutorialText;
    public GameObject videoPlayerObject;
    public VideoPlayer videoPlayer;
    public RawImage videoDisplay;
    public Image tutorialImage;
    public Button closeButton;
    public Button nextButton;
    public Button previousButton;
    public GameObject pageIndicator;
    public TextMeshProUGUI pageText;
    public GameObject hintButtonPrefab;
    public Transform hintButtonContainer;

    private bool isBuilt = false;

    void Start()
    {
        if (buildOnStart && !isBuilt)
            BuildCompleteUI();
    }

    public void BuildCompleteUI()
    {
        if (isBuilt)
        {
            Debug.LogWarning("UI already built! Skipping to prevent duplicates.");
            return;
        }

        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogError("No Canvas found! Please assign or create a Canvas first.");
                return;
            }
        }

        BuildTutorialPopup();
        BuildHintButtonSystem();

        TutorialSystem tutorialSystem = GetComponent<TutorialSystem>();
        if (tutorialSystem != null)
            AssignToTutorialSystem(tutorialSystem);

        isBuilt = true;
        Debug.Log("✅ Tutorial UI built: Bigger + Rounded + Paginated!");
    }

    // ─────────────────────────────────────────────────────────────
    //  POPUP
    // ─────────────────────────────────────────────────────────────
    void BuildTutorialPopup()
    {
        // Full-screen dim background
        tutorialPopupPanel = new GameObject("TutorialPopupPanel");
        tutorialPopupPanel.transform.SetParent(targetCanvas.transform, false);

        RectTransform popupRect = tutorialPopupPanel.AddComponent<RectTransform>();
        popupRect.anchorMin = Vector2.zero;
        popupRect.anchorMax = Vector2.one;
        popupRect.offsetMin = Vector2.zero;
        popupRect.offsetMax = Vector2.zero;

        Image popupBg = tutorialPopupPanel.AddComponent<Image>();
        popupBg.color = backgroundColor;

        CanvasGroup popupGroup = tutorialPopupPanel.AddComponent<CanvasGroup>();
        popupGroup.alpha = 1f;
        popupGroup.interactable = true;
        popupGroup.blocksRaycasts = true;

        tutorialPopupPanel.SetActive(false);

        // Centered card container
        GameObject container = new GameObject("PopupContainer");
        container.transform.SetParent(tutorialPopupPanel.transform, false);

        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(popupWidth, popupHeight);

        Image containerBg = container.AddComponent<Image>();
        containerBg.color = popupColor;
        containerBg.sprite = CreateRoundedSprite(cornerRadius);
        containerBg.type = Image.Type.Sliced;
        containerBg.pixelsPerUnitMultiplier = 1f;

        Shadow shadow = container.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.65f);
        shadow.effectDistance = new Vector2(8, -8);

        // Build inner layout
        CreateHeader(container.transform);
        CreateContentArea(container.transform);   // No scroll — plain text area
        CreateNavigationPanel(container.transform);
    }

    // ─────────────────────────────────────────────────────────────
    //  HEADER
    // ─────────────────────────────────────────────────────────────
    GameObject CreateHeader(Transform parent)
    {
        GameObject header = new GameObject("HeaderPanel");
        header.transform.SetParent(parent, false);

        RectTransform headerRect = header.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.sizeDelta = new Vector2(0, 100);
        headerRect.anchoredPosition = Vector2.zero;

        // Rounded top using the same sprite but clipped at bottom
        Image headerBg = header.AddComponent<Image>();
        headerBg.color = headerColor;
        headerBg.sprite = CreateRoundedTopSprite(cornerRadius);
        headerBg.type = Image.Type.Sliced;

        // Title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(header.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(28, 12);
        titleRect.offsetMax = new Vector2(-90, -12);

        tutorialTitle = titleObj.AddComponent<TextMeshProUGUI>();
        tutorialTitle.text = "Tutorial";
        tutorialTitle.fontSize = titleFontSize;
        tutorialTitle.fontStyle = FontStyles.Bold;
        tutorialTitle.alignment = TextAlignmentOptions.Left;
        tutorialTitle.color = Color.white;

        CreateCloseButton(header.transform);

        return header;
    }

    void CreateCloseButton(Transform parent)
    {
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(parent, false);

        RectTransform closeRect = closeObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 0.5f);
        closeRect.anchorMax = new Vector2(1, 0.5f);
        closeRect.pivot = new Vector2(1, 0.5f);
        closeRect.sizeDelta = new Vector2(72, 72);
        closeRect.anchoredPosition = new Vector2(-14, 0);

        closeButton = closeObj.AddComponent<Button>();
        Image closeBg = closeObj.AddComponent<Image>();
        closeBg.color = new Color(1, 1, 1, 0.18f);
        closeBg.sprite = CreateCircleSprite();

        ColorBlock colors = closeButton.colors;
        colors.normalColor = new Color(1, 1, 1, 0.18f);
        colors.highlightedColor = new Color(1, 0.3f, 0.3f, 0.45f);
        colors.pressedColor = new Color(1, 0, 0, 0.65f);
        closeButton.colors = colors;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(closeObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI closeText = textObj.AddComponent<TextMeshProUGUI>();
        closeText.text = "✕";
        closeText.fontSize = 34;
        closeText.fontStyle = FontStyles.Bold;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.color = Color.white;
    }

    // ─────────────────────────────────────────────────────────────
    //  CONTENT AREA — NO SCROLL, text fills the space directly
    // ─────────────────────────────────────────────────────────────
    void CreateContentArea(Transform parent)
    {
        GameObject contentArea = new GameObject("ContentArea");
        contentArea.transform.SetParent(parent, false);

        RectTransform areaRect = contentArea.AddComponent<RectTransform>();
        areaRect.anchorMin = Vector2.zero;
        areaRect.anchorMax = Vector2.one;
        // Leave room for header (top 100px) and nav panel (bottom 100px)
        areaRect.offsetMin = new Vector2(0, 100);
        areaRect.offsetMax = new Vector2(0, -100);

        // Optional subtle background tint for the content area
        Image areaBg = contentArea.AddComponent<Image>();
        areaBg.color = new Color(0.97f, 0.97f, 0.98f, 1f);

        // Tutorial text — fills the area with padding
        GameObject textObj = new GameObject("TutorialText");
        textObj.transform.SetParent(contentArea.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(36, 28);
        textRect.offsetMax = new Vector2(-36, -28);

        tutorialText = textObj.AddComponent<TextMeshProUGUI>();
        tutorialText.text = "Tutorial content will appear here...";
        tutorialText.fontSize = bodyFontSize;
        tutorialText.alignment = TextAlignmentOptions.TopLeft;
        tutorialText.color = new Color(0.13f, 0.13f, 0.13f);
        tutorialText.enableWordWrapping = true;
        tutorialText.lineSpacing = 8;
        tutorialText.overflowMode = TextOverflowModes.Overflow;

        // Tutorial image (hidden by default)
        GameObject imageObj = new GameObject("TutorialImage");
        imageObj.transform.SetParent(contentArea.transform, false);

        RectTransform imageRect = imageObj.AddComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0, 0);
        imageRect.anchorMax = new Vector2(1, 0);
        imageRect.pivot = new Vector2(0.5f, 0);
        imageRect.sizeDelta = new Vector2(0, 280);
        imageRect.anchoredPosition = new Vector2(0, 36);

        tutorialImage = imageObj.AddComponent<Image>();
        tutorialImage.color = Color.white;
        tutorialImage.preserveAspect = true;
        imageObj.SetActive(false);

        // Video player (hidden by default)
        videoPlayerObject = new GameObject("VideoPlayerObject");
        videoPlayerObject.transform.SetParent(contentArea.transform, false);

        RectTransform videoRect = videoPlayerObject.AddComponent<RectTransform>();
        videoRect.anchorMin = new Vector2(0, 0);
        videoRect.anchorMax = new Vector2(1, 0);
        videoRect.pivot = new Vector2(0.5f, 0);
        videoRect.sizeDelta = new Vector2(0, 280);
        videoRect.anchoredPosition = new Vector2(0, 36);

        videoDisplay = videoPlayerObject.AddComponent<RawImage>();
        videoDisplay.color = Color.black;

        videoPlayer = videoPlayerObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.isLooping = true;

        RenderTexture renderTexture = new RenderTexture(1920, 1080, 24);
        videoPlayer.targetTexture = renderTexture;
        videoDisplay.texture = renderTexture;

        videoPlayerObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    //  NAVIGATION PANEL  (Previous | Page X/Y | Next)
    // ─────────────────────────────────────────────────────────────
    void CreateNavigationPanel(Transform parent)
    {
        GameObject navPanel = new GameObject("NavigationPanel");
        navPanel.transform.SetParent(parent, false);

        RectTransform navRect = navPanel.AddComponent<RectTransform>();
        navRect.anchorMin = new Vector2(0, 0);
        navRect.anchorMax = new Vector2(1, 0);
        navRect.pivot = new Vector2(0.5f, 0);
        navRect.sizeDelta = new Vector2(0, 100);
        navRect.anchoredPosition = Vector2.zero;

        Image navBg = navPanel.AddComponent<Image>();
        navBg.color = new Color(0.93f, 0.93f, 0.95f);
        navBg.sprite = CreateRoundedBottomSprite(cornerRadius);
        navBg.type = Image.Type.Sliced;

        // Previous button — left side
        previousButton = CreateNavButton(navPanel.transform, "◀  Previous",
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(95, 0));

        // Page indicator — center
        CreatePageIndicator(navPanel.transform);

        // Next button — right side
        nextButton = CreateNavButton(navPanel.transform, "Next  ▶",
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-95, 0));
    }

    Button CreateNavButton(Transform parent, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position)
    {
        string safeName = label.Replace(" ", "").Replace("◀", "").Replace("▶", "") + "Button";
        GameObject btnObj = new GameObject(safeName);
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = anchorMin;
        btnRect.anchorMax = anchorMax;
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(160, 62);
        btnRect.anchoredPosition = position;

        Button button = btnObj.AddComponent<Button>();
        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = buttonColor;
        btnBg.sprite = CreateRoundedSprite(14);
        btnBg.type = Image.Type.Sliced;

        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonColor * 1.15f;
        colors.pressedColor = buttonColor * 0.82f;
        colors.disabledColor = new Color(0.72f, 0.72f, 0.72f);
        button.colors = colors;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(6, 4);
        textRect.offsetMax = new Vector2(-6, -4);

        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = label;
        buttonText.fontSize = buttonFontSize;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        return button;
    }

    void CreatePageIndicator(Transform parent)
    {
        pageIndicator = new GameObject("PageIndicator");
        pageIndicator.transform.SetParent(parent, false);

        RectTransform indicatorRect = pageIndicator.AddComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
        indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorRect.pivot = new Vector2(0.5f, 0.5f);
        indicatorRect.sizeDelta = new Vector2(130, 55);
        indicatorRect.anchoredPosition = Vector2.zero;

        GameObject textObj = new GameObject("PageText");
        textObj.transform.SetParent(pageIndicator.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        pageText = textObj.AddComponent<TextMeshProUGUI>();
        pageText.text = "1 / 1";
        pageText.fontSize = pageFontSize;
        pageText.fontStyle = FontStyles.Bold;
        pageText.alignment = TextAlignmentOptions.Center;
        pageText.color = new Color(0.22f, 0.22f, 0.22f);
    }

    // ─────────────────────────────────────────────────────────────
    //  HINT BUTTON SYSTEM
    // ─────────────────────────────────────────────────────────────
    void BuildHintButtonSystem()
    {
        GameObject container = new GameObject("HintButtonContainer");
        container.transform.SetParent(targetCanvas.transform, false);

        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(1, 1);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.pivot = new Vector2(1, 1);
        containerRect.sizeDelta = new Vector2(75, 650); // same as button width — no room to stretch
        containerRect.anchoredPosition = new Vector2(-18, -22);

        VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        hintButtonContainer = container.transform;
        hintButtonPrefab = CreateHintButtonPrefab();
    }

    GameObject CreateHintButtonPrefab()
    {
        GameObject prefab = new GameObject("HintButtonPrefab");

        RectTransform prefabRect = prefab.AddComponent<RectTransform>();
        prefabRect.sizeDelta = new Vector2(75, 75);

        // Force the layout system to keep this exactly 75x75 — prevents stretching into an oval
        LayoutElement le = prefab.AddComponent<LayoutElement>();
        le.minWidth       = 75;
        le.minHeight      = 75;
        le.preferredWidth = 75;
        le.preferredHeight= 75;

        Image buttonBg = prefab.AddComponent<Image>();
        buttonBg.sprite = CreateCircleSprite();
        buttonBg.color = hintButtonColor;
        buttonBg.preserveAspect = true;   // belt-and-suspenders

        Button button = prefab.AddComponent<Button>();
        button.targetGraphic = buttonBg;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        button.colors = colors;

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(prefab.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
        iconText.text = "?";
        iconText.fontSize = 44;
        iconText.fontStyle = FontStyles.Bold;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.color = Color.white;

        HintButtonVisuals visuals = prefab.AddComponent<HintButtonVisuals>();
        visuals.normalColor = hintButtonColor;
        visuals.highlightColor = new Color(0.4f, 0.65f, 0.95f, 1f);
        visuals.pulseSpeed = 1f;
        visuals.pulseAmount = 0.15f;
        visuals.autoStart = true;

        Shadow shadow = prefab.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(4, -4);

        return prefab;
    }

    // ─────────────────────────────────────────────────────────────
    //  SPRITE HELPERS
    // ─────────────────────────────────────────────────────────────

    Sprite CreateCircleSprite()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = (size / 2f) - 4;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius - 1)
                    pixels[y * size + x] = Color.white;
                else if (dist <= radius + 1)
                    pixels[y * size + x] = new Color(1, 1, 1, 1f - Mathf.Clamp01((dist - (radius - 1)) / 2f));
                else
                    pixels[y * size + x] = Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
    }

    /// <summary>Full rounded rect — all four corners rounded.</summary>
    Sprite CreateRoundedSprite(int r)
    {
        int size = 128;
        r = Mathf.Clamp(r, 2, size / 2);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixels[y * size + x] = SampleRounded(x, y, size, size, r,
                    roundTL: true, roundTR: true, roundBL: true, roundBR: true);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        // Use a 9-slice border equal to the corner radius
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100,
            0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }

    /// <summary>Rounded only on TOP two corners — for the header strip.</summary>
    Sprite CreateRoundedTopSprite(int r)
    {
        int size = 128;
        r = Mathf.Clamp(r, 2, size / 2);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixels[y * size + x] = SampleRounded(x, y, size, size, r,
                    roundTL: true, roundTR: true, roundBL: false, roundBR: false);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100,
            0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }

    /// <summary>Rounded only on BOTTOM two corners — for the nav strip.</summary>
    Sprite CreateRoundedBottomSprite(int r)
    {
        int size = 128;
        r = Mathf.Clamp(r, 2, size / 2);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixels[y * size + x] = SampleRounded(x, y, size, size, r,
                    roundTL: false, roundTR: false, roundBL: true, roundBR: true);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100,
            0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }

    /// <summary>
    /// Returns white (with anti-aliased edge) or clear for a pixel at (x,y)
    /// inside a rect of (w,h) with selectively rounded corners of radius r.
    /// </summary>
    Color SampleRounded(int x, int y, int w, int h, int r,
        bool roundTL, bool roundTR, bool roundBL, bool roundBR)
    {
        // Check which corner quadrant we're in
        bool inLeft   = x < r;
        bool inRight  = x >= w - r;
        bool inBottom = y < r;
        bool inTop    = y >= h - r;

        Vector2 cornerCenter = Vector2.zero;
        bool isCorner = false;
        bool doRound  = false;

        if (inLeft && inTop)       { isCorner = true; doRound = roundTL; cornerCenter = new Vector2(r, h - r); }
        else if (inRight && inTop) { isCorner = true; doRound = roundTR; cornerCenter = new Vector2(w - r, h - r); }
        else if (inLeft && inBottom)  { isCorner = true; doRound = roundBL; cornerCenter = new Vector2(r, r); }
        else if (inRight && inBottom) { isCorner = true; doRound = roundBR; cornerCenter = new Vector2(w - r, r); }

        if (!isCorner || !doRound)
            return Color.white;

        float dist = Vector2.Distance(new Vector2(x, y), cornerCenter);
        if (dist <= r - 1)        return Color.white;
        if (dist <= r + 1)        return new Color(1, 1, 1, 1f - Mathf.Clamp01((dist - (r - 1)) / 2f));
        return Color.clear;
    }

    // ─────────────────────────────────────────────────────────────
    //  ASSIGN TO TUTORIAL SYSTEM
    // ─────────────────────────────────────────────────────────────
    void AssignToTutorialSystem(TutorialSystem system)
    {
        system.tutorialPopupPanel = tutorialPopupPanel;
        system.tutorialTitle      = tutorialTitle;
        system.tutorialText       = tutorialText;
        system.videoPlayerObject  = videoPlayerObject;
        system.videoPlayer        = videoPlayer;
        system.videoDisplay       = videoDisplay;
        system.tutorialImage      = tutorialImage;
        system.closeButton        = closeButton;
        system.nextButton         = nextButton;
        system.previousButton     = previousButton;
        system.pageIndicator      = pageIndicator;
        system.pageText           = pageText;
        system.hintButtonPrefab   = hintButtonPrefab;
        system.hintButtonContainer = hintButtonContainer;

        Debug.Log("✅ Tutorial UI (Bigger + Rounded + Paginated) assigned to TutorialSystem!");
    }

    void OnDestroy()
    {
        isBuilt = false;
    }
}