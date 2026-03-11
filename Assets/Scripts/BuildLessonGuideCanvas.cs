// ================================================================
//  BuildLessonGuideCanvas.cs
//
//  EDITOR UTILITY — Not included in your build.
//
//  HOW TO USE:
//  1. Drop this file into any folder named "Editor" inside your
//     Assets folder. e.g.  Assets/Editor/BuildLessonGuideCanvas.cs
//  2. Open your Arrays_AR scene.
//  3. In the Unity menu bar click:
//         Tools → Build Lesson Guide Canvas
//  4. The entire LessonGuideCanvas will be created in your scene.
//  5. Then drag the generated components into ARArrayLessonGuide's
//     Inspector fields (the script lists every field name clearly).
//  6. Delete this file (or keep it in Editor/) — it won't ship in build.
// ================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public static class BuildLessonGuideCanvas
{
    [MenuItem("Tools/Build Lesson Guide Canvas")]
    public static void Build()
    {
        // ── Root Canvas ───────────────────────────────────────────
        var canvasGO = new GameObject("LessonGuideCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // draws on top of your existing Canvas

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── 1. GUIDE CARD PANEL ───────────────────────────────────
        // Anchored to bottom, ~40% screen height
        var guideCard = MakePanel(canvasGO, "GuideCardPanel",
            new Vector2(0f, 0f), new Vector2(1f, 0f),        // anchor min/max
            new Vector2(0f, 0f), new Vector2(0f, 740f),      // offset min/max
            new Color(0.08f, 0.08f, 0.12f, 0.95f));

        // Step counter  — top-left of card
        var counter = MakeTMP(guideCard, "StepCounterText", "Step 1 / 9",
            16, TextAlignmentOptions.TopLeft,
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -16), new Vector2(200, -46));

        // Progress bar background
        var barBG = MakeImage(guideCard, "ProgressBarBG",
            new Vector2(0,1), new Vector2(1,1),
            new Vector2(20,-50), new Vector2(-20,-66),
            new Color(0.2f,0.2f,0.2f,1f));

        // Progress bar fill (child of BG)
        var barFill = MakeImage(barBG, "ProgressBarFill",
            new Vector2(0,0), new Vector2(1,1),
            Vector2.zero, Vector2.zero,
            new Color(0.2f, 0.6f, 1f, 1f));
        barFill.GetComponent<Image>().type = Image.Type.Filled;
        barFill.GetComponent<Image>().fillMethod = Image.FillMethod.Horizontal;
        barFill.GetComponent<Image>().fillAmount = 0.11f;

        // Step title
        var titleTMP = MakeTMP(guideCard, "StepTitleText", "What is an Array?",
            28, TextAlignmentOptions.TopLeft,
            new Vector2(0,1), new Vector2(1,1),
            new Vector2(20,-74), new Vector2(-20,-120),
            bold: true);
        titleTMP.GetComponent<TextMeshProUGUI>().color = Color.white;

        // Step body
        var bodyTMP = MakeTMP(guideCard, "StepBodyText",
            "An array stores items of the same type in contiguous memory slots.",
            22, TextAlignmentOptions.TopLeft,
            new Vector2(0,1), new Vector2(1,1),
            new Vector2(20,-130), new Vector2(-20,-520));
        bodyTMP.GetComponent<TextMeshProUGUI>().color = new Color(0.85f,0.85f,0.85f,1f);

        // Minimise button — top-right corner of card
        MakeButton(guideCard, "MinimiseButton", "−",
            new Vector2(1,1), new Vector2(1,1),
            new Vector2(-70,-10), new Vector2(-10,-60),
            new Color(0.25f,0.25f,0.3f,1f));

        // Next button — bottom right
        MakeButton(guideCard, "NextStepButton", "Next →",
            new Vector2(1,0), new Vector2(1,0),
            new Vector2(-240,20), new Vector2(-20,80),
            new Color(0.2f,0.55f,1f,1f));

        // Return button — same spot, hidden initially
        var returnBtn = MakeButton(guideCard, "ReturnButton", "Return to Lesson",
            new Vector2(0.5f,0), new Vector2(0.5f,0),
            new Vector2(-160,20), new Vector2(160,80),
            new Color(0.2f,0.7f,0.4f,1f));
        returnBtn.SetActive(false);

        // ── 2. COLLAPSED TAB ──────────────────────────────────────
        var collapsedTab = MakePanel(canvasGO, "CollapsedTab",
            new Vector2(0f,0f), new Vector2(0f,0f),
            new Vector2(20f,20f), new Vector2(220f,90f),
            new Color(0.1f,0.1f,0.15f,0.95f));
        MakeButton(collapsedTab, "RestoreButton", "📖 Guide",
            new Vector2(0,0), new Vector2(1,1),
            Vector2.zero, Vector2.zero,
            new Color(0.2f,0.55f,1f,1f));
        collapsedTab.SetActive(false);

        // ── 3. SLOT INSPECT CARD (L1–3) ──────────────────────────
        var inspectCard = MakePanel(canvasGO, "SlotInspectCard",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            new Vector2(-220f,-160f), new Vector2(220f,160f),
            new Color(0.06f,0.06f,0.10f,0.97f));
        MakeTMP(inspectCard, "InspectElementText", "Element: 10",
            24, TextAlignmentOptions.Left,
            new Vector2(0,1), new Vector2(1,1),
            new Vector2(20,-20), new Vector2(-20,-60));
        MakeTMP(inspectCard, "InspectIndexText", "Index: 0",
            24, TextAlignmentOptions.Left,
            new Vector2(0,1), new Vector2(1,1),
            new Vector2(20,-64), new Vector2(-20,-104));
        MakeTMP(inspectCard, "InspectMemoryText", "Address = base + 0 × size",
            20, TextAlignmentOptions.Left,
            new Vector2(0,1), new Vector2(1,1),
            new Vector2(20,-108), new Vector2(-20,-148));
        MakeTMP(inspectCard, "InspectTypeText", "Type: int",
            20, TextAlignmentOptions.Left,
            new Vector2(0,1), new Vector2(1,1),
            new Vector2(20,-152), new Vector2(-20,-192));
        MakeButton(inspectCard, "CloseButton", "✕ Close",
            new Vector2(0.5f,0), new Vector2(0.5f,0),
            new Vector2(-80,10), new Vector2(80,55),
            new Color(0.6f,0.1f,0.1f,1f));
        inspectCard.SetActive(false);

        // ── 4. TRAVERSAL BUTTON PANEL (L4) ───────────────────────
        var traversalPanel = MakePanel(canvasGO, "TraversalButtonPanel",
            new Vector2(0,0), new Vector2(1,0),
            new Vector2(0,740), new Vector2(0,880),
            new Color(0.06f,0.06f,0.10f,0.93f));

        // 5 traversal buttons in a row
        string[] travLabels = { "▶ Linear","▶ Reverse","▶ For","▶ While","▶ Foreach" };
        string[] travNames  = { "PlayLinearButton","PlayReverseButton","PlayForLoopButton","PlayWhileLoopButton","PlayForeachButton" };
        float btnW = 180f, gap = 10f, startX = 20f;
        for (int i = 0; i < travLabels.Length; i++)
        {
            float x = startX + i * (btnW + gap);
            MakeButton(traversalPanel, travNames[i], travLabels[i],
                new Vector2(0,0.5f), new Vector2(0,0.5f),
                new Vector2(x,-35), new Vector2(x+btnW,35),
                new Color(0.15f,0.4f,0.8f,1f));
        }

        MakeTMP(traversalPanel, "TraversalReadout", "Tap a traversal type to begin",
            20, TextAlignmentOptions.Right,
            new Vector2(1,0.5f), new Vector2(1,0.5f),
            new Vector2(-400,-20), new Vector2(-20,20));

        MakeTMP(traversalPanel, "CodeSnippetLabel", "",
            18, TextAlignmentOptions.Right,
            new Vector2(1,1), new Vector2(1,1),
            new Vector2(-420,-10), new Vector2(-20,-60));

        traversalPanel.SetActive(false);

        // ── 5. OPERATION FEEDBACK PANEL (L5–6) ───────────────────
        var opPanel = MakePanel(canvasGO, "OpFeedbackPanel",
            new Vector2(0,1), new Vector2(1,1),
            new Vector2(0,-220), new Vector2(0,0),
            new Color(0.05f,0.08f,0.05f,0.96f));
        MakeTMP(opPanel, "OpFeedbackTitle", "INSERT",
            30, TextAlignmentOptions.Left,
            new Vector2(0,1), new Vector2(0.6f,1),
            new Vector2(20,-16), new Vector2(0,-60),
            bold: true).GetComponent<TextMeshProUGUI>().color = new Color(0.3f,1f,0.5f,1f);
        MakeTMP(opPanel, "OpFeedbackBody", "Adding an element at the end is O(1).",
            20, TextAlignmentOptions.Left,
            new Vector2(0,1), new Vector2(1,1),
            new Vector2(20,-64), new Vector2(-20,-160));
        MakeTMP(opPanel, "OpComplexityBadge", "O(1)",
            28, TextAlignmentOptions.Right,
            new Vector2(1,1), new Vector2(1,1),
            new Vector2(-160,-10), new Vector2(-20,-60),
            bold: true).GetComponent<TextMeshProUGUI>().color = new Color(0.3f,1f,0.5f,1f);
        MakeTMP(opPanel, "OperationLogText", "",
            17, TextAlignmentOptions.Left,
            new Vector2(0,0), new Vector2(1,0),
            new Vector2(20,10), new Vector2(-20,90));
        opPanel.SetActive(false);

        // ── 6. TOAST PANEL (L7) ──────────────────────────────────
        var toastPanel = MakePanel(canvasGO, "ToastPanel",
            new Vector2(0.5f,1), new Vector2(0.5f,1),
            new Vector2(-300,-120), new Vector2(300,-20),
            new Color(0.08f,0.08f,0.12f,0.95f));
        var toastIcon = MakeImage(toastPanel, "ToastIcon",
            new Vector2(0,0.5f), new Vector2(0,0.5f),
            new Vector2(10,-30), new Vector2(70,30),
            Color.white);
        MakeTMP(toastPanel, "ToastText", "Great job!",
            22, TextAlignmentOptions.MidlineLeft,
            new Vector2(0,0), new Vector2(1,1),
            new Vector2(80,0), new Vector2(-10,0));
        toastPanel.SetActive(false);

        // ── 7. COMPLEXITY TABLE PANEL (L8–9) ─────────────────────
        var tablePanel = MakePanel(canvasGO, "ComplexityTablePanel",
            new Vector2(1,1), new Vector2(1,1),
            new Vector2(-380,-20), new Vector2(-20,-340),
            new Color(0.05f,0.05f,0.10f,0.95f));
        MakeTMP(tablePanel, "ComplexityTableText",
            "Operation      | Time\n─────────────────────\nAccess         | O(1)\nInsert (end)   | O(1)\nInsert (mid)   | O(n)\nRemove         | O(n)\nLinear Search  | O(n)\nSpace          | O(n)",
            18, TextAlignmentOptions.TopLeft,
            new Vector2(0,1), new Vector2(1,1),
            new Vector2(16,-16), new Vector2(-16,-260));
        MakeTMP(tablePanel, "ActiveOperationLabel", "",
            16, TextAlignmentOptions.BottomLeft,
            new Vector2(0,0), new Vector2(1,0),
            new Vector2(16,10), new Vector2(-16,40));
        tablePanel.SetActive(false);

        // ── Done ──────────────────────────────────────────────────
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build LessonGuideCanvas");
        Selection.activeGameObject = canvasGO;
        Debug.Log("[BuildLessonGuideCanvas] ✅ LessonGuideCanvas created! " +
                  "Now assign the fields to ARArrayLessonGuide in the Inspector.");
    }

    // ── Helpers ───────────────────────────────────────────────────

    static GameObject MakePanel(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax,
        Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt  = go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return go;
    }

    static GameObject MakeTMP(GameObject parent, string name, string text,
        float fontSize, TextAlignmentOptions alignment,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax,
        bool bold = false)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt  = go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();

        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        if (bold) tmp.fontStyle = FontStyles.Bold;

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return go;
    }

    static GameObject MakeButton(GameObject parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax,
        Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt  = go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        go.AddComponent<Button>();

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        // Label child
        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        var lrt = lblGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        var tmp = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return go;
    }

    static GameObject MakeImage(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax,
        Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt  = go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return go;
    }
}
#endif