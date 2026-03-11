// ================================================================
//  ARAssessmentPanelBuilder_LL.cs
//  Place in:  Assets/Editor/ARAssessmentPanelBuilder_LL.cs
//
//  Mirrors ARAssessmentPanelBuilder.cs for the Linked List AR scene.
//  Reuses the same three-panel layout (Intro / Task / Results)
//  with LL-appropriate purple accent colours.
//
//  Unity menu:  Tools → Build LL AR Assessment Panels
//  Adds panels inside "LessonGuideCanvas" (or selected Canvas).
// ================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class ARAssessmentPanelBuilder_LL : EditorWindow
{
    // ── Colour palette (purple accent for Linked List) ─────────────
    static Color DarkBg       = new Color(0.06f, 0.05f, 0.14f, 0.97f);
    static Color CardBg       = new Color(0.10f, 0.09f, 0.28f, 1.00f);
    static Color AccentPurple = new Color(0.49f, 0.36f, 0.99f, 1.00f);
    static Color AccentGreen  = new Color(0.14f, 0.82f, 0.38f, 1.00f);
    static Color AccentRed    = new Color(0.90f, 0.22f, 0.22f, 1.00f);
    static Color AccentYellow = new Color(1.00f, 0.85f, 0.20f, 1.00f);
    static Color TextWhite    = new Color(0.95f, 0.96f, 1.00f, 1.00f);
    static Color TextMuted    = new Color(0.55f, 0.57f, 0.78f, 1.00f);
    static Color BarBg        = new Color(1f, 1f, 1f, 0.10f);

    [MenuItem("Tools/Build LL AR Assessment Panels")]
    public static void Build()
    {
        Canvas canvas = null;
        if (Selection.activeGameObject != null)
            canvas = Selection.activeGameObject.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject cgo = GameObject.Find("LessonGuideCanvas");
            if (cgo != null) canvas = cgo.GetComponent<Canvas>();
        }
        if (canvas == null) canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ARAssessmentPanelBuilder_LL] No Canvas found in scene.");
            return;
        }

        BuildAssessmentPanels(canvas.transform);
        Debug.Log($"✅ LL Assessment panels built inside '{canvas.name}'. " +
                  "Drag the component references in the Inspector.");
    }

    static void BuildAssessmentPanels(Transform canvasRoot)
    {
        GameObject root = MakeRect("AssessmentRoot", canvasRoot);
        Stretch(root);
        root.SetActive(false);

        BuildIntroPanel(root.transform);
        BuildTaskPanel(root.transform);
        BuildResultsPanel(root.transform);

        Selection.activeGameObject = root;
        Undo.RegisterCreatedObjectUndo(root, "Build LL AR Assessment Panels");
        Debug.Log("Hierarchy created. Assign fields in ARLinkedListLessonAssessment.");
    }

    // ── 1. INTRO PANEL ────────────────────────────────────────────
    static void BuildIntroPanel(Transform parent)
    {
        GameObject panel = MakeRect("AssessmentIntroPanel", parent);
        Stretch(panel);
        AddImage(panel, DarkBg);

        GameObject card = MakeRect("IntroCard", panel.transform);
        SetSize(card, 900, 1000);
        SetAnchoredPos(card, 0, 0);
        SetAnchorsPoint(card, 0.5f, 0.5f);
        AddImage(card, CardBg);
        VertLayout(card, 28, TextAnchor.UpperCenter, 60, 60, 70, 60);

        // Linked list icon
        GameObject icon = MakeTMP("IntroIcon", card.transform, "🔗", 110, TextWhite, FontStyles.Normal);
        LE(icon, 0, 120);

        GameObject title = MakeTMP("IntroTitleText", card.transform, "Linked List Assessment",
            62, TextWhite, FontStyles.Bold);
        LE(title, 0, 80);
        title.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        GameObject body = MakeTMP("IntroBodyText", card.transform,
            "You completed the lesson guide!\nNow prove your understanding.", 38, TextMuted, FontStyles.Normal);
        LE(body, 0, 260);
        var bodyTmp = body.GetComponent<TextMeshProUGUI>();
        bodyTmp.alignment        = TextAlignmentOptions.Center;
        bodyTmp.enableWordWrapping = true;

        GameObject spacer = MakeRect("Spacer", card.transform);
        LE(spacer, 0, 30);

        GameObject startBtn = MakeRect("StartAssessmentButton", card.transform);
        LE(startBtn, 0, 110);
        AddImage(startBtn, AccentPurple);
        startBtn.AddComponent<Button>();
        GameObject startLabel = MakeTMP("StartLabel", startBtn.transform,
            "START ASSESSMENT  →", 44, TextWhite, FontStyles.Bold);
        Stretch(startLabel);
        startLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
    }

    // ── 2. LIVE TASK PANEL ────────────────────────────────────────
    static void BuildTaskPanel(Transform parent)
    {
        GameObject panel = MakeRect("AssessmentTaskPanel", parent);
        Stretch(panel);
        AddImage(panel, new Color(0, 0, 0, 0));
        panel.SetActive(false);

        // Top bar
        GameObject topBar = MakeRect("TaskTopBar", panel.transform);
        SetAnchors(topBar, 0f, 1f, 1f, 1f);
        topBar.GetComponent<RectTransform>().sizeDelta        = new Vector2(0, 130);
        topBar.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        topBar.GetComponent<RectTransform>().pivot            = new Vector2(0.5f, 1f);
        AddImage(topBar, new Color(0.06f, 0.05f, 0.14f, 0.95f));

        // Progress bar BG
        GameObject barBg = MakeRect("TaskProgressBarBg", topBar.transform);
        SetAnchors(barBg, 0.04f, 0f, 0.96f, 0f);
        barBg.GetComponent<RectTransform>().sizeDelta        = new Vector2(0, 8);
        barBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 12);
        barBg.GetComponent<RectTransform>().pivot            = new Vector2(0.5f, 0f);
        AddImage(barBg, BarBg);

        // Progress fill
        GameObject barFill = MakeRect("TaskProgressBarFill", barBg.transform);
        SetAnchors(barFill, 0f, 0f, 1f, 1f);
        barFill.GetComponent<RectTransform>().pivot    = new Vector2(0f, 0.5f);
        barFill.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 1f);
        Image fillImg = barFill.AddComponent<Image>();
        fillImg.color      = AccentPurple;
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0.25f;

        HorizLayout(topBar, 0, TextAnchor.MiddleCenter, 40, 40, 22, 30);

        GameObject counter = MakeTMP("TaskProgressText", topBar.transform,
            "Task 1 / 4", 36, TextMuted, FontStyles.Normal);
        LE(counter, 1, 0);

        GameObject timer = MakeTMP("TaskTimerText", topBar.transform,
            "⏱ 60s", 36, AccentYellow, FontStyles.Bold);
        LE(timer, 0, 80);
        timer.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;

        // Task card (bottom)
        GameObject card = MakeRect("TaskCard", panel.transform);
        SetAnchors(card, 0f, 0f, 1f, 0f);
        card.GetComponent<RectTransform>().sizeDelta        = new Vector2(0, 580);
        card.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 180);
        card.GetComponent<RectTransform>().pivot            = new Vector2(0.5f, 0f);
        AddImage(card, new Color(0.06f, 0.05f, 0.16f, 0.97f));
        VertLayout(card, 20, TextAnchor.UpperCenter, 50, 50, 44, 40);

        // "ASSESSMENT" badge
        GameObject badge = MakeTMP("AssessmentBadge", card.transform,
            "ASSESSMENT", 28, AccentPurple, FontStyles.Bold);
        LE(badge, 0, 36);
        badge.GetComponent<TextMeshProUGUI>().characterSpacing = 6;
        badge.GetComponent<TextMeshProUGUI>().alignment        = TextAlignmentOptions.Center;

        GameObject taskTitle = MakeTMP("TaskTitleText", card.transform,
            "Task Title", 52, TextWhite, FontStyles.Bold);
        LE(taskTitle, 0, 72);
        taskTitle.GetComponent<TextMeshProUGUI>().alignment        = TextAlignmentOptions.Center;
        taskTitle.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;

        GameObject div = MakeRect("Divider", card.transform);
        LE(div, 1, 3);
        div.GetComponent<LayoutElement>().minHeight = 3;
        AddImage(div, new Color(1f, 1f, 1f, 0.08f));

        GameObject instr = MakeTMP("TaskInstructionText", card.transform,
            "Instruction goes here…", 34, TextMuted, FontStyles.Normal);
        LE(instr, 1, 0);
        instr.GetComponent<TextMeshProUGUI>().alignment        = TextAlignmentOptions.Center;
        instr.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;

        // Feedback banner
        GameObject fbBanner = MakeRect("TaskFeedbackBanner", panel.transform);
        SetAnchors(fbBanner, 0.05f, 0.5f, 0.95f, 0.5f);
        fbBanner.GetComponent<RectTransform>().sizeDelta        = new Vector2(0, 110);
        fbBanner.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        AddImage(fbBanner, AccentGreen);
        fbBanner.SetActive(false);

        GameObject fbText = MakeTMP("TaskFeedbackText", fbBanner.transform,
            "✅ Well done!", 46, TextWhite, FontStyles.Bold);
        Stretch(fbText);
        fbText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
    }

    // ── 3. RESULTS PANEL ─────────────────────────────────────────
    static void BuildResultsPanel(Transform parent)
    {
        GameObject panel = MakeRect("AssessmentResultsPanel", parent);
        Stretch(panel);
        AddImage(panel, DarkBg);
        panel.SetActive(false);

        GameObject scroll = MakeRect("ResultsScroll", panel.transform);
        Stretch(scroll);
        ScrollRect sr = scroll.AddComponent<ScrollRect>();
        sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 40;

        GameObject viewport = MakeRect("Viewport", scroll.transform);
        Stretch(viewport);
        viewport.AddComponent<RectMask2D>();
        sr.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = MakeRect("ResultsContent", viewport.transform);
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot     = new Vector2(0.5f, 1f);
        crt.offsetMin = crt.offsetMax = Vector2.zero;
        sr.content    = crt;
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VertLayout(content, 24, TextAnchor.UpperCenter, 60, 60, 80, 60);

        // Title
        GameObject title = MakeTMP("ResultsTitleText", content.transform,
            "Assessment Complete!", 64, TextWhite, FontStyles.Bold);
        LE(title, 0, 85);
        title.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Grade ring card
        GameObject gradeCard = MakeRect("GradeCard", content.transform);
        LE(gradeCard, 0, 280);
        AddImage(gradeCard, CardBg);

        GameObject ring = MakeRect("GradeRingImage", gradeCard.transform);
        SetSize(ring, 200, 200);
        SetAnchoredPos(ring, 0, 0);
        SetAnchorsPoint(ring, 0.5f, 0.5f);
        Image ringImg      = ring.AddComponent<Image>();
        ringImg.color      = AccentGreen;
        ringImg.type       = Image.Type.Filled;
        ringImg.fillMethod = Image.FillMethod.Radial360;
        ringImg.fillAmount = 0.85f;

        GameObject scoreText = MakeTMP("ResultsScoreText", gradeCard.transform,
            "85 / 100  (85%)", 40, TextWhite, FontStyles.Bold);
        SetAnchorsPoint(scoreText, 0.5f, 0.5f);
        SetAnchoredPos(scoreText, 0, -85);
        SetSize(scoreText, 700, 55);
        scoreText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        GameObject grade = MakeTMP("ResultsGradeText", gradeCard.transform,
            "A", 110, AccentGreen, FontStyles.Bold);
        SetAnchorsPoint(grade, 0.5f, 0.5f);
        SetAnchoredPos(grade, 0, 30);
        SetSize(grade, 200, 140);
        grade.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Breakdown card
        GameObject bkCard = MakeRect("BreakdownCard", content.transform);
        LE(bkCard, 0, 500);
        AddImage(bkCard, CardBg);

        GameObject bkTitle = MakeTMP("BreakdownTitle", bkCard.transform,
            "Task Breakdown", 42, AccentPurple, FontStyles.Bold);
        SetAnchorsPoint(bkTitle, 0.5f, 1f);
        SetAnchoredPos(bkTitle, 0, -36);
        SetSize(bkTitle, 760, 55);
        bkTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        GameObject bkText = MakeTMP("ResultsBreakdownText", bkCard.transform,
            "✅ Task 1:  15/15\n❌ Task 2:  10/15", 34, TextMuted, FontStyles.Normal);
        SetAnchors(bkText, 0.05f, 0.05f, 0.95f, 0.88f);
        bkText.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;

        // Tip card
        GameObject tipCard = MakeRect("TipCard", content.transform);
        LE(tipCard, 0, 200);
        AddImage(tipCard, new Color(0.49f, 0.36f, 0.99f, 0.15f));

        MakeTMP("TipIcon", tipCard.transform, "💡", 50, TextWhite, FontStyles.Normal);

        GameObject tipText = MakeTMP("ResultsTipText", tipCard.transform,
            "Review: insert/delete at head = O(1). All other positions = O(n).",
            34, TextMuted, FontStyles.Normal);
        SetAnchors(tipText, 0.12f, 0.08f, 0.96f, 0.92f);
        tipText.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;

        // Buttons row
        GameObject btnRow = MakeRect("ButtonRow", content.transform);
        LE(btnRow, 0, 120);
        HorizLayout(btnRow, 30, TextAnchor.MiddleCenter);

        // Retry
        GameObject retryBtn = MakeRect("RetryAssessmentButton", btnRow.transform);
        LE(retryBtn, 0, 120); retryBtn.GetComponent<LayoutElement>().preferredWidth = 380;
        AddImage(retryBtn, new Color(0.36f, 0.26f, 0.75f, 1f));
        retryBtn.AddComponent<Button>();
        GameObject retryLabel = MakeTMP("RetryLabel", retryBtn.transform,
            "↩  Retry", 40, TextWhite, FontStyles.Bold);
        Stretch(retryLabel);
        retryLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Return
        GameObject returnBtn = MakeRect("ReturnButton", btnRow.transform);
        LE(returnBtn, 0, 120); returnBtn.GetComponent<LayoutElement>().preferredWidth = 380;
        AddImage(returnBtn, AccentGreen);
        returnBtn.AddComponent<Button>();
        GameObject returnLabel = MakeTMP("ReturnLabel", returnBtn.transform,
            "✓  Return to App", 40, TextWhite, FontStyles.Bold);
        Stretch(returnLabel);
        returnLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Bottom padding
        MakeRect("BottomPad", content.transform).AddComponent<LayoutElement>().preferredHeight = 60;
    }

    // ── HELPERS ──────────────────────────────────────────────────
    static GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
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
        rt.anchorMin = rt.anchorMax = new Vector2(x, y);
    }

    static void SetSize(GameObject go, float w, float h) =>
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);

    static void SetAnchoredPos(GameObject go, float x, float y) =>
        go.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);

    static Image AddImage(GameObject go, Color color)
    {
        Image img = go.AddComponent<Image>();
        img.color = color; img.raycastTarget = false;
        return img;
    }

    static void HorizLayout(GameObject go, float spacing,
        TextAnchor align = TextAnchor.MiddleCenter,
        float padL = 0, float padR = 0, float padT = 0, float padB = 0)
    {
        var h = go.AddComponent<HorizontalLayoutGroup>();
        h.spacing = spacing; h.childAlignment = align;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        h.padding = new RectOffset((int)padL, (int)padR, (int)padT, (int)padB);
    }

    static void VertLayout(GameObject go, float spacing,
        TextAnchor align = TextAnchor.UpperCenter,
        float padL = 0, float padR = 0, float padT = 0, float padB = 0)
    {
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = spacing; v.childAlignment = align;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = v.childForceExpandHeight = false;
        v.padding = new RectOffset((int)padL, (int)padR, (int)padT, (int)padB);
    }

    static void LE(GameObject go, float flexW, float prefH)
    {
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth   = flexW;
        le.preferredHeight = prefH;
    }

    static GameObject MakeTMP(string name, Transform parent, string text,
        float fontSize, Color color, FontStyles style)
    {
        GameObject go = MakeRect(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize;
        tmp.color = color; tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return go;
    }
}
#endif