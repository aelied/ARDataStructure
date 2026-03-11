using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TutorialManager - Drop this on any AR scene's Canvas to get:
///   1. First-launch popup tutorials for every button
///   2. A persistent hint button (top-right circle) for returning users
///
/// HOW TO USE:
///   1. Add this component to a UI Canvas in your scene
///   2. Set sceneKey to a unique string per scene (e.g. "ArrayScene")
///   3. Assign the hintButtonPrefab OR let the script auto-create it
///   4. Register each button via RegisterButton() calls in Awake/Start
///      - or use the Inspector list buttonRegistrations
///
/// TO ADD VIDEO LATER:
///   - Each TutorialStep has a videoClip field (currently unused)
///   - When ready, instantiate a VideoPlayer, assign the clip, and play it
///     inside ShowTutorialPopup() where the comment marker is
/// </summary>
public class TutorialManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // Inspector fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Scene Identity")]
    [Tooltip("Unique key saved to PlayerPrefs. Change per scene.")]
    public string sceneKey = "DefaultScene";

    [Header("Popup Prefab (optional – auto-created if null)")]
    [Tooltip("Drag a custom popup prefab here, or leave null for auto-build.")]
    public GameObject popupPrefab;

    [Header("Hint Circle Prefab (optional – auto-created if null)")]
    [Tooltip("Small circle button shown in top-right corner after first run.")]
    public GameObject hintButtonPrefab;

    [Header("Canvas to parent UI into")]
    [Tooltip("Defaults to the Canvas on this GameObject.")]
    public Canvas targetCanvas;

    [Header("Button Registrations (fill in Inspector OR call RegisterButton() at runtime)")]
    public List<ButtonRegistration> buttonRegistrations = new List<ButtonRegistration>();

    // ─────────────────────────────────────────────────────────────────────────
    // Data types
    // ─────────────────────────────────────────────────────────────────────────

    [System.Serializable]
    public class ButtonRegistration
    {
        [Tooltip("The actual UI Button component")]
        public Button targetButton;

        [Tooltip("Short title shown at the top of the popup")]
        public string tutorialTitle = "Button Tutorial";

        [Tooltip("Description text shown in the popup body")]
        [TextArea(3, 8)]
        public string tutorialDescription = "Tap this button to perform an action.";

        [Tooltip("Icon/emoji shown left of the title (optional)")]
        public string emoji = "ℹ️";

        // ── Video support ──────────────────────────────────────────────────
        // When you have video assets ready:
        //   1. Uncomment the field below
        //   2. Uncomment the VideoPlayer block in ShowTutorialPopup()
        //   3. Assign your clip in the Inspector
        // [Tooltip("Optional video clip – replaces text when assigned")]
        // public UnityEngine.Video.VideoClip videoClip;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private state
    // ─────────────────────────────────────────────────────────────────────────

    private bool isFirstTime = true;
    private GameObject activePopup;
    private GameObject hintCircle;
    private ButtonRegistration currentHintRegistration;

    // Colours / sizes – tweak here if needed
    private readonly Color popupBackground     = new Color(0.08f, 0.08f, 0.12f, 0.96f);
    private readonly Color titleColor          = new Color(1f,    0.85f, 0.3f,  1f);
    private readonly Color bodyColor           = new Color(0.9f,  0.9f,  0.9f,  1f);
    private readonly Color closeButtonColor    = new Color(0.95f, 0.3f,  0.3f,  1f);
    private readonly Color hintCircleColor     = new Color(0.25f, 0.55f, 1f,    0.92f);
    private readonly Color overlayColor        = new Color(0f,    0f,    0f,    0.5f);

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        string prefKey = $"TutorialShown_{sceneKey}";
        isFirstTime = !PlayerPrefs.HasKey(prefKey);
    }

    void Start()
    {
        // Hook all registered buttons
        foreach (var reg in buttonRegistrations)
        {
            if (reg.targetButton != null)
                HookButton(reg);
        }

        // Build the persistent hint circle (hidden until first popup is dismissed)
        BuildHintCircle();

        if (!isFirstTime)
            ShowHintCircle(false); // show immediately on re-visits? Set true if preferred
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Register a button at runtime (e.g. from other MonoBehaviours).
    /// Call this from your scene's Start() after TutorialManager.Start() runs,
    /// or from Awake() if execution order is set correctly.
    /// </summary>
    public void RegisterButton(Button btn, string title, string description, string emoji = "ℹ️")
    {
        if (btn == null) return;

        var reg = new ButtonRegistration
        {
            targetButton    = btn,
            tutorialTitle   = title,
            tutorialDescription = description,
            emoji           = emoji
        };

        buttonRegistrations.Add(reg);
        HookButton(reg);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Internal helpers
    // ─────────────────────────────────────────────────────────────────────────

    void HookButton(ButtonRegistration reg)
    {
        reg.targetButton.onClick.AddListener(() => OnButtonClicked(reg));
    }

    void OnButtonClicked(ButtonRegistration reg)
    {
        if (isFirstTime)
        {
            ShowTutorialPopup(reg, onClose: () =>
            {
                // After closing any popup, mark first-time done and show hint circle
                // (we mark after ALL buttons have been shown once, but showing the
                // circle from the very first close is a better UX trade-off)
                MarkFirstTimeDone();
                ShowHintCircle(true);
            });
        }
        else
        {
            // On subsequent presses just update the hint circle content
            currentHintRegistration = reg;
            // Optionally flash the hint circle to signal "help available"
            if (hintCircle != null)
                StartCoroutine(FlashHintCircle());
        }
    }

    void MarkFirstTimeDone()
    {
        if (!isFirstTime) return;
        isFirstTime = false;
        string prefKey = $"TutorialShown_{sceneKey}";
        PlayerPrefs.SetInt(prefKey, 1);
        PlayerPrefs.Save();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Popup creation & display
    // ─────────────────────────────────────────────────────────────────────────

    void ShowTutorialPopup(ButtonRegistration reg, System.Action onClose = null)
    {
        if (activePopup != null)
            Destroy(activePopup);

        if (targetCanvas == null) return;

        // ── Root container ────────────────────────────────────────────────────
        activePopup = new GameObject("TutorialPopup");
        activePopup.transform.SetParent(targetCanvas.transform, false);
        activePopup.transform.SetAsLastSibling(); // always on top

        RectTransform rootRect = activePopup.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // Dim overlay
        Image overlay = activePopup.AddComponent<Image>();
        overlay.color = overlayColor;
        overlay.raycastTarget = true; // blocks touches behind popup

        // ── Card panel ───────────────────────────────────────────────────────
        GameObject card = new GameObject("Card");
        card.transform.SetParent(activePopup.transform, false);

        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot     = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(620, 420);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = popupBackground;

        // Rounded corners via sprite – use built-in rounded rect if available
        // (works in Unity 2019.4+; silently falls back to square otherwise)
        Sprite roundedSprite = Resources.Load<Sprite>("UI/Rounded");
        if (roundedSprite != null)
        {
            cardBg.sprite = roundedSprite;
            cardBg.type   = Image.Type.Sliced;
        }

        // ── Layout inside card ───────────────────────────────────────────────
        VerticalLayoutGroup vLayout = card.AddComponent<VerticalLayoutGroup>();
        vLayout.padding            = new RectOffset(32, 32, 28, 28);
        vLayout.spacing            = 16;
        vLayout.childAlignment     = TextAnchor.UpperCenter;
        vLayout.childControlWidth  = true;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandWidth  = true;
        vLayout.childForceExpandHeight = false;

        // Emoji + Title row
        GameObject titleRow = CreateHorizontalRow(card.transform, 60);
        AddTextElement(titleRow.transform, reg.emoji, 40, titleColor, false, 80);
        AddTextElement(titleRow.transform, reg.tutorialTitle, 30, titleColor, bold: true);

        // Divider line
        AddDivider(card.transform);

        // ── VIDEO PLACEHOLDER ────────────────────────────────────────────────
        // When you're ready to add video support:
        //
        //   if (reg.videoClip != null)
        //   {
        //       GameObject videoContainer = new GameObject("VideoContainer");
        //       videoContainer.transform.SetParent(card.transform, false);
        //       RectTransform vRect = videoContainer.AddComponent<RectTransform>();
        //       vRect.sizeDelta = new Vector2(0, 200);
        //       RawImage rawImg = videoContainer.AddComponent<RawImage>();
        //
        //       UnityEngine.Video.VideoPlayer vp = videoContainer.AddComponent<UnityEngine.Video.VideoPlayer>();
        //       vp.clip = reg.videoClip;
        //       vp.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
        //       RenderTexture rt = new RenderTexture(560, 200, 0);
        //       vp.targetTexture = rt;
        //       rawImg.texture   = rt;
        //       vp.Play();
        //
        //       LayoutElement le = videoContainer.AddComponent<LayoutElement>();
        //       le.preferredHeight = 200;
        //   }
        //   else { /* show description text below */ }
        //
        // ────────────────────────────────────────────────────────────────────

        // Description text (shown until video is assigned)
        GameObject descObj = AddTextElement(card.transform, reg.tutorialDescription, 22, bodyColor);
        LayoutElement descLE = descObj.AddComponent<LayoutElement>();
        descLE.preferredHeight = 160;
        descLE.flexibleHeight  = 1;

        // Divider line
        AddDivider(card.transform);

        // Close / Got It button
        GameObject closeBtn = CreateButton(
            card.transform,
            "Got it! ✓",
            closeButtonColor,
            Color.white,
            28,
            new Vector2(0, 56)
        );
        LayoutElement closeBtnLE = closeBtn.AddComponent<LayoutElement>();
        closeBtnLE.preferredHeight = 56;

        Button closeBtnComp = closeBtn.GetComponent<Button>();
        closeBtnComp.onClick.AddListener(() =>
        {
            Destroy(activePopup);
            activePopup = null;
            onClose?.Invoke();
        });

        // Animate popup in
        StartCoroutine(AnimatePopupIn(cardRect));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Hint circle (persistent top-right help button)
    // ─────────────────────────────────────────────────────────────────────────

    void BuildHintCircle()
    {
        if (targetCanvas == null) return;

        if (hintButtonPrefab != null)
        {
            hintCircle = Instantiate(hintButtonPrefab, targetCanvas.transform);
        }
        else
        {
            // Auto-create a circular button
            hintCircle = new GameObject("HintCircle");
            hintCircle.transform.SetParent(targetCanvas.transform, false);

            RectTransform rt = hintCircle.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(80, 80);
            rt.anchoredPosition = new Vector2(-20, -20);

            Image bg = hintCircle.AddComponent<Image>();
            bg.color  = hintCircleColor;
            bg.sprite = CreateCircleSprite(80);

            // "?" label
            GameObject label = new GameObject("Label");
            label.transform.SetParent(hintCircle.transform, false);
            RectTransform labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text      = "?";
            tmp.fontSize  = 36;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            Button btn = hintCircle.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.35f, 0.65f, 1f);
            cb.pressedColor     = new Color(0.15f, 0.45f, 0.9f);
            btn.colors = cb;

            btn.onClick.AddListener(OnHintCircleClicked);
        }

        hintCircle.SetActive(false); // hidden until first popup is dismissed
    }

    void ShowHintCircle(bool visible)
    {
        if (hintCircle != null)
            hintCircle.SetActive(visible);
    }

    void OnHintCircleClicked()
    {
        if (currentHintRegistration != null)
            ShowTutorialPopup(currentHintRegistration);
        else if (buttonRegistrations.Count > 0)
            ShowTutorialPopup(buttonRegistrations[0]);
    }

    IEnumerator FlashHintCircle()
    {
        if (hintCircle == null) yield break;
        Image img = hintCircle.GetComponent<Image>();
        if (img == null) yield break;

        Color original = img.color;
        img.color = Color.white;
        yield return new WaitForSeconds(0.15f);
        img.color = original;
        yield return new WaitForSeconds(0.15f);
        img.color = Color.white;
        yield return new WaitForSeconds(0.15f);
        img.color = original;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Popup animation
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator AnimatePopupIn(RectTransform card)
    {
        float duration = 0.22f;
        float elapsed  = 0f;
        card.localScale = Vector3.zero;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic
            card.localScale = Vector3.one * eased;
            yield return null;
        }
        card.localScale = Vector3.one;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI factory helpers
    // ─────────────────────────────────────────────────────────────────────────

    GameObject CreateHorizontalRow(Transform parent, float height)
    {
        GameObject row = new GameObject("Row");
        row.transform.SetParent(parent, false);

        RectTransform rt = row.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, height);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing             = 10;
        hlg.childAlignment      = TextAnchor.MiddleCenter;
        hlg.childControlWidth   = false;
        hlg.childControlHeight  = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        LayoutElement le = row.AddComponent<LayoutElement>();
        le.preferredHeight = height;

        return row;
    }

    GameObject AddTextElement(Transform parent, string text, int fontSize,
                              Color color, bool bold = false, float fixedWidth = 0)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        if (fixedWidth > 0)
        {
            rt.sizeDelta = new Vector2(fixedWidth, 0);
            LayoutElement le = obj.AddComponent<LayoutElement>();
            le.preferredWidth  = fixedWidth;
            le.flexibleWidth   = 0;
        }

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.overflowMode   = TextOverflowModes.Overflow;
        tmp.enableWordWrapping = true;

        return obj;
    }

    void AddDivider(Transform parent)
    {
        GameObject divider = new GameObject("Divider");
        divider.transform.SetParent(parent, false);

        RectTransform rt = divider.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 2);

        Image img = divider.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.12f);

        LayoutElement le = divider.AddComponent<LayoutElement>();
        le.preferredHeight = 2;
        le.minHeight       = 2;
    }

    GameObject CreateButton(Transform parent, string label, Color bgColor,
                            Color textColor, int fontSize, Vector2 sizeDelta)
    {
        GameObject btn = new GameObject("Button");
        btn.transform.SetParent(parent, false);

        RectTransform rt = btn.AddComponent<RectTransform>();
        rt.sizeDelta = sizeDelta;

        Image img = btn.AddComponent<Image>();
        img.color  = bgColor;
        img.sprite = CreateRoundedRectSprite(200, 60, 12);
        img.type   = Image.Type.Sliced;

        Button btnComp = btn.AddComponent<Button>();
        btnComp.targetGraphic = img;
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor      = bgColor;
        cb.highlightedColor = bgColor * 1.15f;
        cb.pressedColor     = bgColor * 0.8f;
        btnComp.colors = cb;

        // Label
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(btn.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.color     = textColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Sprite helpers
    // ─────────────────────────────────────────────────────────────────────────

    Sprite CreateCircleSprite(int size)
    {
        Texture2D tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[]   pixels = new Color[size * size];
        float center = size / 2f;
        float radius = size / 2f - 1f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                pixels[y * size + x] = d <= radius ? Color.white : Color.clear;
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateRoundedRectSprite(int w, int h, int radius)
    {
        Texture2D tex    = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[]   pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool inside = IsInsideRoundedRect(x, y, w, h, radius);
                pixels[y * w + x] = inside ? Color.white : Color.clear;
            }
        tex.SetPixels(pixels);
        tex.Apply();
        // Border = radius so Unity slicing keeps corners intact
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f),
                             100f, 0, SpriteMeshType.FullRect,
                             new Vector4(radius, radius, radius, radius));
    }

    bool IsInsideRoundedRect(int px, int py, int w, int h, int r)
    {
        // Quick inner rect check
        if (px >= r && px < w - r) return true;
        if (py >= r && py < h - r) return true;
        // Corner circles
        int[][] corners = {
            new[] {r,     r    },
            new[] {w - r, r    },
            new[] {r,     h - r},
            new[] {w - r, h - r}
        };
        foreach (var c in corners)
        {
            float d = Vector2.Distance(new Vector2(px, py), new Vector2(c[0], c[1]));
            if (d <= r) return true;
        }
        return false;
    }
}