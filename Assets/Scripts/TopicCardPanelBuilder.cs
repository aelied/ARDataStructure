// ================================================================
//  TopicCardPanelBuilder.cs
//  Place this file in:  Assets/Editor/TopicCardPanelBuilder.cs
//
//  Then in Unity:  top menu  →  Tools  →  Build Topic Card Panel
//  It will generate the full panel inside your active Canvas.
// ================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class TopicCardPanelBuilder : EditorWindow
{
    // ── Tweakable colours (edit before building) ──────────────────
    static Color NavyBg        = new Color(0.05f, 0.06f, 0.17f, 1f);
    static Color HeaderA       = new Color(0.10f, 0.06f, 0.31f, 1f);
    static Color HeaderB       = new Color(0.42f, 0.25f, 0.78f, 1f);
    static Color CardBg        = new Color(0.11f, 0.13f, 0.38f, 1f);
    static Color CardBorder    = new Color(0.39f, 0.47f, 1.00f, 0.22f);
    static Color IconBoxBg     = new Color(0.20f, 0.16f, 0.55f, 0.70f);
    static Color PurpleAccent  = new Color(0.49f, 0.36f, 0.99f, 1f);
    static Color TextWhite     = new Color(0.94f, 0.95f, 1.00f, 1f);
    static Color TextMuted     = new Color(0.53f, 0.55f, 0.75f, 1f);
    static Color PillBg        = new Color(0.31f, 0.39f, 1.00f, 0.25f);
    static Color ProgressBg    = new Color(1f,   1f,   1f,   0.08f);
    static Color NavActiveTint = new Color(0.66f, 0.55f, 1.00f, 1f);
    static Color NavInactive   = new Color(0.53f, 0.55f, 0.75f, 1f);
    static Color BodyWhite     = new Color(0.05f, 0.06f, 0.17f, 1f);
    static Color BottomNavBg   = new Color(0.05f, 0.06f, 0.17f, 0.97f);

    // ── Reference resolution (match your CanvasScaler) ────────────
    const float REF_W = 1080f;
    const float REF_H = 1920f;

    // ── Menu item ─────────────────────────────────────────────────
    [MenuItem("Tools/Build Topic Card Panel")]
    public static void Build()
    {
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject cGO = new GameObject("Canvas");
            canvas = cGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(REF_W, REF_H);
            cGO.AddComponent<GraphicRaycaster>();
        }

        BuildPanel(canvas.transform);
        Debug.Log("✅ Topic Card Panel built! Check your Canvas.");
    }

    // ═════════════════════════════════════════════════════════════
    static void BuildPanel(Transform canvasRoot)
    {
        // ── Root panel ──────────────────────────────────────────
        GameObject root = MakeRect("TopicCardPanel", canvasRoot);
        Stretch(root);
        AddImage(root, NavyBg);

        // Attach the runtime controller script (if it exists)
        // root.AddComponent<TopicCardPanel>();  ← uncomment after adding TopicCardPanel.cs

        // ── HEADER ──────────────────────────────────────────────
        BuildHeader(root.transform);

        // ── BODY (scroll + cards) ────────────────────────────────
        BuildBody(root.transform);

        // ── BOTTOM NAV ───────────────────────────────────────────
        BuildBottomNav(root.transform);

        // Select in hierarchy
        Selection.activeGameObject = root;
        Undo.RegisterCreatedObjectUndo(root, "Build Topic Card Panel");
    }

    // ─────────────────────────────────────────────────────────────
    // HEADER
    // ─────────────────────────────────────────────────────────────
    static void BuildHeader(Transform parent)
    {
        GameObject header = MakeRect("Header", parent);
        SetAnchors(header, 0f, 0.72f, 1f, 1f);   // top 28% of screen
        SetOffsets(header, 0, 0, 0, 0);
        AddImage(header, HeaderA);                  // solid fallback; add gradient in editor

        // Back button
        GameObject backBtn = MakeRect("BackButton", header.transform);
        SetSize(backBtn, 80, 80);
        SetAnchoredPos(backBtn, 70, -90);
        SetPivot(backBtn, 0.5f, 1f);
        SetAnchorsPoint(backBtn, 0f, 1f);
        AddImage(backBtn, new Color(1, 1, 1, 0.15f));
        RoundCorners(backBtn, 20);
        Button backBtnComp = backBtn.AddComponent<Button>();
        // Arrow text
        GameObject backArrow = MakeTMP("BackArrowText", backBtn.transform, "←", 50, TextWhite, FontStyles.Bold);
        Stretch(backArrow);
        backArrow.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // "CHALLENGE" label
        GameObject label = MakeTMP("HeaderLabelText", header.transform, "CHALLENGE", 28, new Color(1,1,1,0.5f), FontStyles.Bold);
        SetAnchorsPoint(label, 0f, 1f);
        SetAnchoredPos(label, 50, -175);
        SetSize(label, 400, 40);
        label.GetComponent<TextMeshProUGUI>().characterSpacing = 8;

        // Topic title
        GameObject title = MakeTMP("TopicTitleText", header.transform, "Stacks", 80, TextWhite, FontStyles.Bold);
        SetAnchorsPoint(title, 0f, 1f);
        SetAnchoredPos(title, 50, -220);
        SetSize(title, 500, 95);

        // Subtitle
        GameObject sub = MakeTMP("TopicSubtitleText", header.transform, "Master the topic", 36, new Color(1,1,1,0.65f), FontStyles.Normal);
        SetAnchorsPoint(sub, 0f, 1f);
        SetAnchoredPos(sub, 52, -310);
        SetSize(sub, 500, 50);

        // Decorative deco image placeholder (top-right)
        GameObject deco = MakeRect("HeaderDecoImage", header.transform);
        SetSize(deco, 180, 180);
        SetAnchorsPoint(deco, 1f, 1f);
        SetAnchoredPos(deco, -30, -30);
        AddImage(deco, new Color(1, 1, 1, 0.08f));
        // Replace HeaderDecoImage's sprite with your isometric art in the Inspector
    }

    // ─────────────────────────────────────────────────────────────
    // BODY  (white card with tab bar + lesson list)
    // ─────────────────────────────────────────────────────────────
    static void BuildBody(Transform parent)
    {
        // White rounded body that overlaps the header slightly
        GameObject body = MakeRect("Body", parent);
        SetAnchors(body, 0f, 0f, 1f, 0.76f);
        SetOffsets(body, 0, 150, 0, 0);   // 150px gap = nav bar height
        AddImage(body, Color.white);

        // ── TAB BAR ──────────────────────────────────────────────
        GameObject tabBar = MakeRect("TabBar", body.transform);
        SetAnchors(tabBar, 0f, 1f, 1f, 1f);
        tabBar.GetComponent<RectTransform>().sizeDelta        = new Vector2(0, 100);
        tabBar.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        tabBar.GetComponent<RectTransform>().pivot            = new Vector2(0.5f, 1f);
        AddImage(tabBar, Color.white);

        // Bottom divider line under tab bar
        GameObject divider = MakeRect("TabDivider", tabBar.transform);
        SetAnchors(divider, 0f, 0f, 1f, 0f);
        divider.GetComponent<RectTransform>().sizeDelta        = new Vector2(0, 3);
        divider.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        divider.GetComponent<RectTransform>().pivot            = new Vector2(0.5f, 0f);
        AddImage(divider, new Color(0.90f, 0.90f, 0.93f));

        HorizLayout(tabBar, 0, TextAnchor.MiddleCenter);

        // Lessons tab (active)
        BuildTab(tabBar.transform, "LessonsTab", "📖  Lessons", true);
        // Tests tab (inactive)
        BuildTab(tabBar.transform, "TestsTab",   "🧠  Tests",   false);

        // ── SCROLL VIEW (lessons list) ────────────────────────────
        GameObject scrollGO = MakeRect("LessonsScrollView", body.transform);
        SetAnchors(scrollGO, 0f, 0f, 1f, 1f);
        SetOffsets(scrollGO, 0, 0, 0, -100);   // sits below tab bar

        ScrollRect scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal        = false;
        scroll.vertical          = true;
        scroll.scrollSensitivity = 40;

        GameObject viewport = MakeRect("Viewport", scrollGO.transform);
        Stretch(viewport);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = MakeRect("LessonsContainer", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;
        scroll.content = contentRT;

        VertLayout(content, 16, TextAnchor.UpperCenter, 20, 20, 20, 20);
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── TESTS PANEL (hidden by default) ──────────────────────
        GameObject testsPanel = MakeRect("TestsPanel", body.transform);
        SetAnchors(testsPanel, 0f, 0f, 1f, 1f);
        SetOffsets(testsPanel, 0, 0, 0, -100);
        AddImage(testsPanel, Color.white);
        testsPanel.SetActive(false);   // hide – swap via TabSwitcher script

        BuildTestsPanel(testsPanel.transform);

        // ── LESSON ROWS ───────────────────────────────────────────
        // LessonsContainer is intentionally left EMPTY here.
        //
        // Your existing TopicDetailPanel.cs → LoadLessons() already
        // calls CreateLessonModule() which instantiates your
        // lessonModulePrefab into this container at runtime.
        //
        // AFTER BUILDING, wire these up in the Inspector:
        //   TopicDetailPanel.lessonsContainer   → drag "LessonsContainer" here
        //   TopicDetailPanel.lessonModulePrefab → drag your existing prefab here
        //   TopicDetailPanel.lessonsScrollView  → drag "LessonsScrollView" here
        //
        // Your prefab child names TopicDetailPanel.cs already looks for:
        //   "TopicText"       – small topic label  (TMP)
        //   "TitleText"       – lesson title        (TMP)
        //   "DescriptionText" – lesson description  (TMP)
        //   "Icon"            – icon image          (Image)
        //   "Checkmark"       – completion tick     (GameObject)
        // ─────────────────────────────────────────────────────────
    }

    // ── Single tab button ─────────────────────────────────────────
    static void BuildTab(Transform parent, string goName, string label, bool active)
    {
        GameObject tab = MakeRect(goName, parent);
        LayoutElement le = tab.AddComponent<LayoutElement>();
        le.flexibleWidth   = 1;
        le.preferredHeight = 100;

        AddImage(tab, Color.white);
        tab.AddComponent<Button>();

        // Label
        Color labelCol = active
            ? new Color(0.18f, 0.35f, 0.90f)   // blue when active
            : new Color(0.55f, 0.55f, 0.65f);   // grey when inactive

        GameObject labelGO = MakeTMP($"{goName}Text", tab.transform, label, 36, labelCol,
                                      active ? FontStyles.Bold : FontStyles.Normal);
        Stretch(labelGO);
        labelGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Active underline bar
        if (active)
        {
            GameObject bar = MakeRect($"{goName}Underline", tab.transform);
            SetAnchors(bar, 0.1f, 0f, 0.9f, 0f);
            bar.GetComponent<RectTransform>().sizeDelta        = new Vector2(0, 4);
            bar.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            bar.GetComponent<RectTransform>().pivot            = new Vector2(0.5f, 0f);
            AddImage(bar, new Color(0.18f, 0.35f, 0.90f));
        }
    }

    // ─────────────────────────────────────────────────────────────
    // TESTS PANEL  (difficulty buttons shown when Tests tab active)
    // ─────────────────────────────────────────────────────────────
    static void BuildTestsPanel(Transform parent)
    {
        // Vertical stack of difficulty buttons
        VertLayout(parent.gameObject, 20, TextAnchor.UpperCenter, 30, 30, 30, 30);
        ContentSizeFitter tf = parent.gameObject.AddComponent<ContentSizeFitter>();
        tf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        string[]   diffNames  = { "Easy",   "Medium", "Hard",   "Mixed"  };
        string[]   diffIcons  = { "⭐",     "💎",     "🔥",     "🌀"     };
        string[]   diffDescs  = { "Foundation questions", "Application questions",
                                   "Advanced problems",   "All difficulties combined" };
        Color[]    diffColors = {
            new Color(0.20f, 0.75f, 0.40f),
            new Color(0.95f, 0.65f, 0.10f),
            new Color(0.85f, 0.25f, 0.25f),
            new Color(0.55f, 0.25f, 0.90f)
        };
        bool[] locked = { false, true, true, true };

        for (int i = 0; i < 4; i++)
        {
            GameObject btn = MakeRect($"{diffNames[i]}DiffButton", parent);
            LayoutElement le = btn.AddComponent<LayoutElement>();
            le.preferredHeight = 130;
            le.flexibleWidth   = 1;

            Color bg = locked[i] ? new Color(0.40f, 0.40f, 0.45f) : diffColors[i];
            AddImage(btn, bg);
            btn.AddComponent<Button>();

            HorizLayout(btn, 18, TextAnchor.MiddleLeft, 24, 24, 18, 18);

            // Icon
            GameObject ico = MakeTMP($"{diffNames[i]}Icon", btn.transform,
                                      diffIcons[i], 52, Color.white, FontStyles.Normal);
            LayoutElement il = ico.AddComponent<LayoutElement>();
            il.preferredWidth = 60;

            // Labels
            GameObject lblGrp = MakeRect($"{diffNames[i]}Labels", btn.transform);
            LayoutElement ll = lblGrp.AddComponent<LayoutElement>();
            ll.flexibleWidth = 1;
            VertLayout(lblGrp, 4);

            MakeTMP($"{diffNames[i]}Name", lblGrp.transform, diffNames[i], 40, Color.white, FontStyles.Bold);
            MakeTMP($"{diffNames[i]}Desc", lblGrp.transform, diffDescs[i], 28,
                    new Color(1f, 1f, 1f, 0.80f), FontStyles.Normal);

            // Lock / check icon
            string statusIcon = locked[i] ? "🔒" : "▶";
            GameObject status = MakeTMP($"{diffNames[i]}Status", btn.transform,
                                         statusIcon, 40, Color.white, FontStyles.Normal);
            LayoutElement sl = status.AddComponent<LayoutElement>();
            sl.preferredWidth = 50;

            CanvasGroup cg = btn.AddComponent<CanvasGroup>();
            cg.alpha             = locked[i] ? 0.55f : 1f;
            cg.interactable      = !locked[i];
            cg.blocksRaycasts    = !locked[i];
        }
    }

    // ─────────────────────────────────────────────────────────────
    // BOTTOM NAV
    // ─────────────────────────────────────────────────────────────
    static void BuildBottomNav(Transform parent)
    {
        GameObject nav = MakeRect("BottomNav", parent);
        SetAnchors(nav, 0f, 0f, 1f, 0f);
        nav.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 160);
        nav.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        AddImage(nav, BottomNavBg);

        HorizLayout(nav, 0, TextAnchor.MiddleCenter);

        string[] labels = { "Home", "Learn", "", "Progress", "Profile" };
        string[] icons  = { "🏠",   "📖",  " ",  "🏆",     "👤"     };

        for (int i = 0; i < 5; i++)
        {
            if (i == 2)
            {
                // AR floating button slot
                BuildARButton(nav.transform);
                continue;
            }

            bool isActive = (i == 1);
            BuildNavItem(nav.transform, labels[i], icons[i], isActive);
        }
    }

    static void BuildNavItem(Transform parent, string label, string icon, bool active)
    {
        GameObject item = MakeRect($"{label}NavItem", parent);
        LayoutElement le = item.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.preferredHeight = 140;

        VertLayout(item, 6, TextAnchor.MiddleCenter);
        item.AddComponent<Button>();

        Color col = active ? NavActiveTint : NavInactive;

        GameObject iconGO = MakeTMP($"{label}NavIcon", item.transform, icon, 50, col, FontStyles.Normal);
        SetSize(FindChild(item, $"{label}NavIcon"), 70, 60);
        iconGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        GameObject labelGO = MakeTMP($"{label}NavLabel", item.transform, label, 26, col, active ? FontStyles.Bold : FontStyles.Normal);
        SetSize(FindChild(item, $"{label}NavLabel"), 140, 36);
        labelGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        if (active)
        {
            // Active underline indicator
            GameObject underline = MakeRect($"{label}ActiveBar", item.transform);
            SetSize(underline, 60, 5);
            AddImage(underline, NavActiveTint);
        }
    }

    static void BuildARButton(Transform parent)
    {
        GameObject slot = MakeRect("ARSlot", parent);
        LayoutElement le = slot.AddComponent<LayoutElement>();
        le.flexibleWidth  = 1;
        le.preferredHeight = 140;

        // Circle button (positioned to float upward)
        GameObject bubble = MakeRect("ARButton", slot.transform);
        SetSize(bubble, 120, 120);
        SetAnchoredPos(bubble, 0, 30);    // floats 30px above nav
        SetAnchorsPoint(bubble, 0.5f, 1f);
        bubble.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
        AddImage(bubble, PurpleAccent);

        // Make it a circle by adjusting sprite or using a circle image
        var img = bubble.GetComponent<Image>();
        img.sprite = GetDefaultCircleSprite();
        img.type   = Image.Type.Simple;
        img.preserveAspect = true;

        bubble.AddComponent<Button>();

        // Glow ring
        GameObject glow = MakeRect("ARGlow", bubble.transform);
        Stretch(glow);
        SetOffsets(glow, -14, -14, 14, 14);
        Image glowImg = glow.AddComponent<Image>();
        glowImg.color  = new Color(0.49f, 0.36f, 0.99f, 0.20f);
        glowImg.sprite = GetDefaultCircleSprite();

        // Camera emoji
        MakeTMP("ARIcon", bubble.transform, "📷", 52, Color.white, FontStyles.Normal);
        Stretch(FindChild(bubble, "ARIcon"));
        FindChild(bubble, "ARIcon").GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Label below
        GameObject arLabel = MakeRect("ARLabel", slot.transform);
        SetSize(arLabel, 120, 40);
        SetAnchoredPos(arLabel, 0, -60);
        SetAnchorsPoint(arLabel, 0.5f, 1f);
        MakeTMP("ARLabelText", arLabel.transform, "3D AR", 24, NavInactive, FontStyles.Bold);
        Stretch(FindChild(arLabel, "ARLabelText"));
        FindChild(arLabel, "ARLabelText").GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
    }

    // ═════════════════════════════════════════════════════════════
    //  HELPER METHODS
    // ═════════════════════════════════════════════════════════════

    static GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetAnchors(GameObject go, float minX, float minY, float maxX, float maxY)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
    }

    static void SetAnchorsPoint(GameObject go, float x, float y)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x, y);
        rt.anchorMax = new Vector2(x, y);
    }

    static void SetOffsets(GameObject go, float left, float bottom, float right, float top)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.offsetMin = new Vector2(left,  bottom);
        rt.offsetMax = new Vector2(right, top);
    }

    static void SetSize(GameObject go, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
    }

    static void SetAnchoredPos(GameObject go, float x, float y)
    {
        go.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
    }

    static void SetPivot(GameObject go, float x, float y)
    {
        go.GetComponent<RectTransform>().pivot = new Vector2(x, y);
    }

    static Image AddImage(GameObject go, Color color)
    {
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static void RoundCorners(GameObject go, float radius)
    {
        // Unity doesn't have built-in corner radius on Image.
        // Tag the object so you know to add a rounded sprite or UIRoundedCorners component later.
        go.name += "_Rounded";
    }

    static void HorizLayout(GameObject go, float spacing,
        TextAnchor childAlign = TextAnchor.MiddleLeft,
        float padL = 0, float padR = 0, float padT = 0, float padB = 0)
    {
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing            = spacing;
        hlg.childAlignment     = childAlign;
        hlg.childControlWidth  = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset((int)padL, (int)padR, (int)padT, (int)padB);
    }

    static void VertLayout(GameObject go, float spacing,
        TextAnchor childAlign = TextAnchor.UpperLeft,
        float padL = 0, float padR = 0, float padT = 0, float padB = 0)
    {
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.spacing            = spacing;
        vlg.childAlignment     = childAlign;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth  = false;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset((int)padL, (int)padR, (int)padT, (int)padB);
    }

    static GameObject MakeTMP(string name, Transform parent, string text, float fontSize,
                               Color color, FontStyles style)
    {
        GameObject go = MakeRect(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return go;
    }

    static GameObject FindChild(GameObject go, string name)
    {
        Transform t = go.transform.Find(name);
        return t != null ? t.gameObject : go;
    }

    static Sprite GetDefaultCircleSprite()
    {
        // Returns Unity's built-in "UISprite" circle sprite
        return Resources.Load<Sprite>("UI/Skin/UISprite");
    }
}
#endif