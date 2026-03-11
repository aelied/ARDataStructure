// ================================================================
//  CodeLabPanelBuilder.cs
//  Place in:  Assets/Editor/CodeLabPanelBuilder.cs
//
//  Unity menu:  Tools → Build Code Lab Panel
//  Builds the full Code Lab UI hierarchy inside whichever
//  Canvas is currently selected, OR inside the first Canvas
//  found in the scene.
//
//  After building, wire the GameObjects to the fields on
//  your CodeLabPanel MonoBehaviour in the Inspector.
// ================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class CodeLabPanelBuilder : EditorWindow
{
    // ── Colour palette (matches Structureality dark theme) ────────
    static Color DarkBg       = new Color(0.051f, 0.059f, 0.078f, 1.00f); // #0D0F14
    static Color SurfaceBg    = new Color(0.082f, 0.094f, 0.125f, 1.00f); // #151820
    static Color CardBg       = new Color(0.110f, 0.125f, 0.188f, 1.00f); // #1C2030
    static Color CodeBg       = new Color(0.031f, 0.039f, 0.055f, 1.00f); // #080A0E
    static Color BorderColor  = new Color(0.145f, 0.165f, 0.227f, 1.00f); // #252A3A
    static Color AccentBlue   = new Color(0.290f, 0.486f, 1.000f, 1.00f); // #4A7CFF
    static Color AccentOrange = new Color(1.000f, 0.420f, 0.208f, 1.00f); // #FF6B35
    static Color AccentGreen  = new Color(0.180f, 0.800f, 0.443f, 1.00f); // #2ECC71
    static Color AccentTeal   = new Color(0.102f, 0.737f, 0.612f, 1.00f); // #1ABC9C
    static Color TextWhite    = new Color(0.933f, 0.941f, 0.973f, 1.00f); // #EEF0F8
    static Color TextMuted    = new Color(0.533f, 0.573f, 0.643f, 1.00f); // #8892A4
    static Color TextDim      = new Color(0.290f, 0.318f, 0.408f, 1.00f); // #4A5168

    // ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Build Code Lab Panel")]
    public static void Build()
    {
        Canvas canvas = null;

        if (Selection.activeGameObject != null)
            canvas = Selection.activeGameObject.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CodeLabPanelBuilder] No Canvas found in scene.");
            return;
        }

        BuildCodeLabPanel(canvas.transform);
        Debug.Log("✅ Code Lab Panel built inside '" + canvas.name + "'. " +
                  "Assign the generated GameObjects to CodeLabPanel fields in the Inspector.");
    }

    // ─────────────────────────────────────────────────────────────
    static void BuildCodeLabPanel(Transform canvasRoot)
    {
        // ── Root panel ────────────────────────────────────────────
        GameObject root = MakeRect("CodeLabPanel", canvasRoot);
        Stretch(root);
        AddImage(root, DarkBg);
        root.SetActive(false); // activated by BottomNavigation at runtime

        // 1. Header bar
        BuildHeader(root.transform);

        // 2. Scrollable body
        GameObject scrollView = BuildScrollView(root.transform);
        Transform  body       = scrollView.transform.Find("Viewport/Content");

        // 3. Problem statement card
        BuildProblemCard(body);

        // 4. Language toggle
        BuildLanguageToggle(body);

        // 5. Template dropdown
        BuildTemplateDropdown(body);

        // 6. Code editor
        BuildCodeEditor(body);

        // 7. Run / Submit buttons
        BuildActionButtons(body);

        // 8. Result panel (hidden by default)
        BuildResultPanel(body);

        // 9. Loading spinner overlay
        BuildLoadingSpinner(root.transform);

        Undo.RegisterCreatedObjectUndo(root, "Build Code Lab Panel");
        Selection.activeGameObject = root;
        Debug.Log("Hierarchy created. Assign fields in the CodeLabPanel component.");
    }

    // ─────────────────────────────────────────────────────────────
    // 1. HEADER
    // ─────────────────────────────────────────────────────────────
    static void BuildHeader(Transform parent)
    {
        GameObject header = MakeRect("Header", parent);
        SetAnchors(header, 0f, 1f, 1f, 1f);
        var hRT = header.GetComponent<RectTransform>();
        hRT.pivot            = new Vector2(0.5f, 1f);
        hRT.anchoredPosition = Vector2.zero;
        hRT.sizeDelta        = new Vector2(0, 88);
        AddImage(header, SurfaceBg);

        // Bottom border
        GameObject border = MakeRect("BorderBottom", header.transform);
        SetAnchors(border, 0f, 0f, 1f, 0f);
        var bRT = border.GetComponent<RectTransform>();
        bRT.pivot     = new Vector2(0.5f, 0f);
        bRT.sizeDelta = new Vector2(0, 1);
        AddImage(border, BorderColor);

        // Back button
        GameObject backBtn = MakeRect("BackButton", header.transform);
        SetAnchors(backBtn, 0f, 0f, 0f, 1f);
        var bbRT = backBtn.GetComponent<RectTransform>();
        bbRT.pivot            = new Vector2(0f, 0.5f);
        bbRT.anchoredPosition = new Vector2(16f, 0f);
        bbRT.sizeDelta        = new Vector2(90f, 0f);
        AddImage(backBtn, Color.clear);
        backBtn.AddComponent<Button>();
        GameObject backLabel = MakeTMP("BackLabel", backBtn.transform, "\u2190 Back", 26, TextMuted, FontStyles.Normal);
        Stretch(backLabel);
        backLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

        // Title group (centre)
        GameObject titleGroup = MakeRect("TitleGroup", header.transform);
        SetAnchors(titleGroup, 0.15f, 0f, 0.75f, 1f);
        VertLayout(titleGroup, 2, TextAnchor.MiddleCenter, 0, 0, 12, 12);

        GameObject titleText = MakeTMP("PanelTitleText", titleGroup.transform, "Code Lab", 30, TextWhite, FontStyles.Bold);
        LE(titleText, 1, 36);
        titleText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        GameObject subText = MakeTMP("ChallengeSubtitleText", titleGroup.transform, "Coding Challenge", 22, TextMuted, FontStyles.Normal);
        LE(subText, 1, 28);
        subText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Language pill (right)
        GameObject langPill = MakeRect("LanguagePill", header.transform);
        SetAnchors(langPill, 1f, 0.5f, 1f, 0.5f);
        var lpRT = langPill.GetComponent<RectTransform>();
        lpRT.pivot            = new Vector2(1f, 0.5f);
        lpRT.anchoredPosition = new Vector2(-16f, 0f);
        lpRT.sizeDelta        = new Vector2(168f, 52f);
        AddImage(langPill, CardBg);
        HorizLayout(langPill, 0, TextAnchor.MiddleCenter, 4, 4, 4, 4);

        GameObject pyBtn = MakeRect("PythonLanguageButton", langPill.transform);
        LE(pyBtn, 1, 44);
        AddImage(pyBtn, AccentBlue);
        pyBtn.AddComponent<Button>();
        GameObject pyLabel = MakeTMP("PythonButtonText", pyBtn.transform, "Py", 22, TextWhite, FontStyles.Bold);
        Stretch(pyLabel);
        pyLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        GameObject javaBtn = MakeRect("JavaLanguageButton", langPill.transform);
        LE(javaBtn, 1, 44);
        AddImage(javaBtn, Color.clear);
        javaBtn.AddComponent<Button>();
        GameObject javaLabel = MakeTMP("JavaButtonText", javaBtn.transform, "Java", 22, TextMuted, FontStyles.Normal);
        Stretch(javaLabel);
        javaLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
    }

    // ─────────────────────────────────────────────────────────────
    // SCROLL VIEW
    // ─────────────────────────────────────────────────────────────
    static GameObject BuildScrollView(Transform parent)
    {
        GameObject sv = MakeRect("ScrollView", parent);
        SetAnchors(sv, 0f, 0f, 1f, 1f);
        sv.GetComponent<RectTransform>().offsetMin = new Vector2(0,   0);
        sv.GetComponent<RectTransform>().offsetMax = new Vector2(0, -88);
        AddImage(sv, Color.clear);

        ScrollRect sr        = sv.AddComponent<ScrollRect>();
        sr.horizontal        = false;
        sr.vertical          = true;
        sr.scrollSensitivity = 40;

        GameObject viewport = MakeRect("Viewport", sv.transform);
        Stretch(viewport);
        viewport.AddComponent<RectMask2D>();
        sr.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = MakeRect("Content", viewport.transform);
        var crt            = content.GetComponent<RectTransform>();
        crt.anchorMin      = new Vector2(0f, 1f);
        crt.anchorMax      = new Vector2(1f, 1f);
        crt.pivot          = new Vector2(0.5f, 1f);
        crt.offsetMin      = crt.offsetMax = Vector2.zero;
        sr.content         = crt;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit       = ContentSizeFitter.FitMode.PreferredSize;
        VertLayout(content, 12, TextAnchor.UpperCenter, 16, 16, 16, 24);

        return sv;
    }

    // ─────────────────────────────────────────────────────────────
    // 3. PROBLEM STATEMENT CARD
    // ─────────────────────────────────────────────────────────────
    static void BuildProblemCard(Transform body)
    {
        GameObject card = MakeRect("ProblemStatementCard", body);
        LE(card, 1, 120);
        AddImage(card, CardBg);
        VertLayout(card, 6, TextAnchor.UpperLeft, 16, 16, 14, 14);

        GameObject label = MakeTMP("ProblemLabel", card.transform,
            "PROBLEM STATEMENT", 18, TextMuted, FontStyles.Bold);
        LE(label, 1, 22);
        label.GetComponent<TextMeshProUGUI>().characterSpacing = 3;

        GameObject text = MakeTMP("ProblemStatementText", card.transform,
            "Write a program that outputs a greeting message to the console.", 26, TextWhite, FontStyles.Normal);
        LE(text, 1, 64);
        text.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;
    }

    // ─────────────────────────────────────────────────────────────
    // 4. LANGUAGE TOGGLE (body-level)
    // ─────────────────────────────────────────────────────────────
    static void BuildLanguageToggle(Transform body)
    {
        GameObject card = MakeRect("LanguageToggleCard", body);
        LE(card, 1, 56);
        AddImage(card, CardBg);
        HorizLayout(card, 0, TextAnchor.MiddleCenter, 6, 6, 6, 6);

        GameObject pyBtn = MakeRect("PythonLanguageButton", card.transform);
        LE(pyBtn, 1, 44);
        AddImage(pyBtn, AccentBlue);
        pyBtn.AddComponent<Button>();
        GameObject pyLbl = MakeTMP("PythonButtonText", pyBtn.transform, "Python", 26, TextWhite, FontStyles.Bold);
        Stretch(pyLbl);
        pyLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        GameObject jBtn = MakeRect("JavaLanguageButton", card.transform);
        LE(jBtn, 1, 44);
        AddImage(jBtn, Color.clear);
        jBtn.AddComponent<Button>();
        GameObject jLbl = MakeTMP("JavaButtonText", jBtn.transform, "Java", 26, TextMuted, FontStyles.Normal);
        Stretch(jLbl);
        jLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
    }

    // ─────────────────────────────────────────────────────────────
    // 5. TEMPLATE DROPDOWN
    // ─────────────────────────────────────────────────────────────
    static void BuildTemplateDropdown(Transform body)
    {
        // Trigger row
        GameObject row = MakeRect("TemplateDropdownRow", body);
        LE(row, 1, 52);
        AddImage(row, CardBg);
        HorizLayout(row, 8, TextAnchor.MiddleCenter, 16, 16, 0, 0);

        GameObject folderIcon = MakeTMP("FolderIcon", row.transform, "\U0001F4C1", 26, TextMuted, FontStyles.Normal);
        LE(folderIcon, 0, 36);

        GameObject labelRow = MakeTMP("TemplateDropdownLabel", row.transform,
            "Template:  Hello World", 24, TextWhite, FontStyles.Normal);
        LE(labelRow, 1, 36);

        GameObject arrow = MakeTMP("DropdownArrow", row.transform, "\u25BE", 24, TextMuted, FontStyles.Normal);
        LE(arrow, 0, 36);
        arrow.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;

        row.AddComponent<Button>();

        // Dropdown panel (hidden by default)
        GameObject ddPanel = MakeRect("TemplateDropdownPanel", body);
        LE(ddPanel, 1, 260);
        AddImage(ddPanel, SurfaceBg);
        ddPanel.SetActive(false);
        VertLayout(ddPanel, 0, TextAnchor.UpperCenter);

        string[] templates = { "Hello World", "Fibonacci", "Linked List", "Stack", "Queue" };
        foreach (string t in templates)
        {
            GameObject item = MakeRect("Template_" + t.Replace(" ", ""), ddPanel.transform);
            LE(item, 1, 52);
            AddImage(item, Color.clear);
            item.AddComponent<Button>();
            GameObject itemLabel = MakeTMP("Label", item.transform, t, 26, TextWhite, FontStyles.Normal);
            Stretch(itemLabel);
            itemLabel.GetComponent<TextMeshProUGUI>().margin    = new Vector4(20, 0, 0, 0);
            itemLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 6. CODE EDITOR
    // ─────────────────────────────────────────────────────────────
    static void BuildCodeEditor(Transform body)
    {
        GameObject editorCard = MakeRect("CodeEditorCard", body);
        LE(editorCard, 1, 280);
        AddImage(editorCard, CodeBg);

        // Line numbers strip
        GameObject lineNumStrip = MakeRect("LineNumberStrip", editorCard.transform);
        SetAnchors(lineNumStrip, 0f, 0f, 0f, 1f);
        var lsRT = lineNumStrip.GetComponent<RectTransform>();
        lsRT.pivot            = new Vector2(0f, 0.5f);
        lsRT.anchoredPosition = Vector2.zero;
        lsRT.sizeDelta        = new Vector2(44f, 0f);
        AddImage(lineNumStrip, new Color(0.024f, 0.031f, 0.047f, 1f));

        // Line number border
        GameObject lineNumBorder = MakeRect("LineNumBorder", editorCard.transform);
        SetAnchors(lineNumBorder, 0f, 0f, 0f, 1f);
        var lbRT = lineNumBorder.GetComponent<RectTransform>();
        lbRT.pivot            = new Vector2(0f, 0.5f);
        lbRT.anchoredPosition = new Vector2(44f, 0f);
        lbRT.sizeDelta        = new Vector2(1f, 0f);
        AddImage(lineNumBorder, BorderColor);

        // Line numbers text
        GameObject lineNums = MakeTMP("LineNumbersText", lineNumStrip.transform,
            "1\n2\n3\n4\n5\n6\n7\n8\n9\n10", 20, TextDim, FontStyles.Normal);
        Stretch(lineNums);
        var lineTmp = lineNums.GetComponent<TextMeshProUGUI>();
        lineTmp.alignment = TextAlignmentOptions.TopRight;
        lineTmp.margin    = new Vector4(0, 14, 6, 14);

        // Code input field
        GameObject inputField = MakeRect("CodeInputField", editorCard.transform);
        SetAnchors(inputField, 0f, 0f, 1f, 1f);
        inputField.GetComponent<RectTransform>().offsetMin = new Vector2(45f, 0f);
        inputField.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        AddImage(inputField, Color.clear);

        TMP_InputField field = inputField.AddComponent<TMP_InputField>();
        field.lineType    = TMP_InputField.LineType.MultiLineNewline;
        field.contentType = TMP_InputField.ContentType.Standard;

        // Text area
        GameObject textArea = MakeRect("Text Area", inputField.transform);
        Stretch(textArea);
        textArea.AddComponent<RectMask2D>();
        field.textViewport = textArea.GetComponent<RectTransform>();

        // Placeholder
        GameObject ph = MakeTMP("Placeholder", textArea.transform,
            "// write solution", 24, new Color(0.290f, 0.486f, 1f, 0.5f), FontStyles.Italic);
        Stretch(ph);
        ph.GetComponent<TextMeshProUGUI>().margin    = new Vector4(10, 14, 10, 14);
        ph.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
        field.placeholder = ph.GetComponent<TextMeshProUGUI>();

        // Input text
        GameObject inputText = MakeTMP("Text", textArea.transform, "", 24, TextWhite, FontStyles.Normal);
        Stretch(inputText);
        inputText.GetComponent<TextMeshProUGUI>().margin    = new Vector4(10, 14, 10, 14);
        inputText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
        field.textComponent = inputText.GetComponent<TextMeshProUGUI>();
    }

    // ─────────────────────────────────────────────────────────────
    // 7. RUN / SUBMIT BUTTONS
    // ─────────────────────────────────────────────────────────────
    static void BuildActionButtons(Transform body)
    {
        GameObject row = MakeRect("ActionButtonRow", body);
        LE(row, 1, 60);
        AddImage(row, Color.clear);
        HorizLayout(row, 10, TextAnchor.MiddleCenter);

        // Run
        GameObject runBtn = MakeRect("RunButton", row.transform);
        LE(runBtn, 1, 56);
        AddImage(runBtn, CardBg);
        runBtn.AddComponent<Button>();
        GameObject runLabel = MakeTMP("RunButtonText", runBtn.transform, "\u25B6  Run", 28, TextWhite, FontStyles.Bold);
        Stretch(runLabel);
        runLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Submit
        GameObject subBtn = MakeRect("SubmitButton", row.transform);
        LE(subBtn, 2, 56);
        AddImage(subBtn, AccentOrange);
        subBtn.AddComponent<Button>();
        GameObject subLabel = MakeTMP("SubmitButtonText", subBtn.transform, "Submit \u2713", 28, TextWhite, FontStyles.Bold);
        Stretch(subLabel);
        subLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
    }

    // ─────────────────────────────────────────────────────────────
    // 8. RESULT PANEL
    // ─────────────────────────────────────────────────────────────
    static void BuildResultPanel(Transform body)
    {
        GameObject resultPanel = MakeRect("ResultPanel", body);
        LE(resultPanel, 1, 220);
        AddImage(resultPanel, CardBg);
        resultPanel.SetActive(false);
        VertLayout(resultPanel, 10, TextAnchor.UpperCenter, 16, 16, 14, 14);

        // Console label
        GameObject consoleLabel = MakeTMP("ConsoleLabel", resultPanel.transform,
            "OUTPUT", 18, TextMuted, FontStyles.Bold);
        LE(consoleLabel, 1, 22);
        consoleLabel.GetComponent<TextMeshProUGUI>().characterSpacing = 3;

        // Console output box
        GameObject consoleBox = MakeRect("ConsoleBox", resultPanel.transform);
        LE(consoleBox, 1, 64);
        AddImage(consoleBox, CodeBg);
        GameObject consoleOut = MakeTMP("ConsoleOutputText", consoleBox.transform,
            "No output", 24, AccentGreen, FontStyles.Normal);
        Stretch(consoleOut);
        consoleOut.GetComponent<TextMeshProUGUI>().margin           = new Vector4(10, 8, 10, 8);
        consoleOut.GetComponent<TextMeshProUGUI>().alignment        = TextAlignmentOptions.TopLeft;
        consoleOut.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;

        // Pass / Fail badges
        GameObject badgeRow = MakeRect("PassFailRow", resultPanel.transform);
        LE(badgeRow, 1, 44);
        AddImage(badgeRow, Color.clear);
        HorizLayout(badgeRow, 10, TextAnchor.MiddleLeft);

        GameObject passPanel = MakeRect("PassResultPanel", badgeRow.transform);
        LE(passPanel, 0, 38);
        passPanel.GetComponent<LayoutElement>().preferredWidth = 100;
        AddImage(passPanel, AccentGreen);
        GameObject passLabel = MakeTMP("PassLabel", passPanel.transform, "Pass", 24, TextWhite, FontStyles.Bold);
        Stretch(passLabel);
        passLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        GameObject failPanel = MakeRect("FailResultPanel", badgeRow.transform);
        LE(failPanel, 0, 38);
        failPanel.GetComponent<LayoutElement>().preferredWidth = 100;
        AddImage(failPanel, new Color(0.2f, 0.2f, 0.2f, 1f));
        GameObject failLabel = MakeTMP("FailLabel", failPanel.transform, "Fail", 24, TextWhite, FontStyles.Bold);
        Stretch(failLabel);
        failLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Status text
        GameObject statusText = MakeTMP("ResultStatusText", resultPanel.transform,
            "", 28, AccentGreen, FontStyles.Bold);
        LE(statusText, 1, 34);

        // After-submit row
        GameObject afterRow = MakeRect("AfterSubmitRow", resultPanel.transform);
        LE(afterRow, 1, 52);
        AddImage(afterRow, Color.clear);
        HorizLayout(afterRow, 8, TextAnchor.MiddleCenter);

        // Lesson button
        GameObject lessonBtn = MakeRect("LessonButton", afterRow.transform);
        LE(lessonBtn, 1, 48);
        AddImage(lessonBtn, SurfaceBg);
        lessonBtn.AddComponent<Button>();
        GameObject lessonLabel = MakeTMP("LessonLabel", lessonBtn.transform, "\u2190 Lesson", 24, TextMuted, FontStyles.Bold);
        Stretch(lessonLabel);
        lessonLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Try AR button
        GameObject arBtn = MakeRect("TryARButton", afterRow.transform);
        LE(arBtn, 1, 48);
        AddImage(arBtn, AccentTeal);
        arBtn.AddComponent<Button>();
        GameObject arLabel = MakeTMP("ARLabel", arBtn.transform, "Try AR", 24, TextWhite, FontStyles.Bold);
        Stretch(arLabel);
        arLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Next button
        GameObject nextBtn = MakeRect("NextButton", afterRow.transform);
        LE(nextBtn, 1, 48);
        AddImage(nextBtn, AccentBlue);
        nextBtn.AddComponent<Button>();
        GameObject nextLabel = MakeTMP("NextLabel", nextBtn.transform, "Next \u2192", 24, TextWhite, FontStyles.Bold);
        Stretch(nextLabel);
        nextLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
    }

    // ─────────────────────────────────────────────────────────────
    // 9. LOADING SPINNER OVERLAY
    // ─────────────────────────────────────────────────────────────
    static void BuildLoadingSpinner(Transform parent)
    {
        GameObject spinner = MakeRect("LoadingSpinner", parent);
        Stretch(spinner);
        AddImage(spinner, new Color(0f, 0f, 0f, 0.65f));
        spinner.SetActive(false);

        GameObject spinnerText = MakeTMP("SpinnerText", spinner.transform,
            "Running...", 36, TextWhite, FontStyles.Bold);
        SetAnchorsPoint(spinnerText, 0.5f, 0.5f);
        spinnerText.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 50);
        spinnerText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
    }

    // ─────────────────────────────────────────────────────────────
    // HELPERS  (mirrors ARAssessmentPanelBuilder exactly)
    // ─────────────────────────────────────────────────────────────

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

    static Image AddImage(GameObject go, Color color)
    {
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;
        return img;
    }

    static void HorizLayout(GameObject go, float spacing,
        TextAnchor align = TextAnchor.MiddleCenter,
        float padL = 0, float padR = 0, float padT = 0, float padB = 0)
    {
        var h = go.AddComponent<HorizontalLayoutGroup>();
        h.spacing                = spacing;
        h.childAlignment         = align;
        h.childControlWidth      = true;
        h.childControlHeight     = true;
        h.childForceExpandWidth  = false;
        h.childForceExpandHeight = false;
        h.padding                = new RectOffset((int)padL, (int)padR, (int)padT, (int)padB);
    }

    static void VertLayout(GameObject go, float spacing,
        TextAnchor align = TextAnchor.UpperCenter,
        float padL = 0, float padR = 0, float padT = 0, float padB = 0)
    {
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing                = spacing;
        v.childAlignment         = align;
        v.childControlWidth      = true;
        v.childControlHeight     = true;
        v.childForceExpandWidth  = false;
        v.childForceExpandHeight = false;
        v.padding                = new RectOffset((int)padL, (int)padR, (int)padT, (int)padB);
    }

    static void LE(GameObject go, float flexW, float prefH)
    {
        var le           = go.AddComponent<LayoutElement>();
        le.flexibleWidth = flexW;
        le.preferredHeight = prefH;
    }

    static GameObject MakeTMP(string name, Transform parent, string text,
                               float fontSize, Color color, FontStyles style)
    {
        GameObject go = MakeRect(name, parent);
        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text          = text;
        tmp.fontSize      = fontSize;
        tmp.color         = color;
        tmp.fontStyle     = style;
        tmp.raycastTarget = false;
        return go;
    }
}
#endif