// ================================================================
//  BuildLessonGuideCanvas_LL.cs
//
//  EDITOR UTILITY — Not included in build.
//
//  Mirrors BuildLessonGuideCanvas.cs exactly for the Linked List AR scene.
//  Extends the original by adding the Operation Feedback Panel and
//  Complexity Table panel used by ARLinkedListLessonGuide.
//
//  HOW TO USE:
//  1. Drop this file into any folder named "Editor" inside Assets.
//     e.g.  Assets/Editor/BuildLessonGuideCanvas_LL.cs
//  2. Open your LinkedList_AR scene.
//  3. In the Unity menu bar click:
//         Tools → Build LL Lesson Guide Canvas
//  4. The entire LessonGuideCanvas will be created in your scene.
//  5. Drag the generated components into ARLinkedListLessonGuide's
//     Inspector fields (the script lists every field name clearly).
//  6. Delete or keep this file in Editor/ — it won't ship in builds.
// ================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public static class BuildLessonGuideCanvas_LL
{
    [MenuItem("Tools/Build LL Lesson Guide Canvas")]
    public static void Build()
    {
        // ── Root Canvas ───────────────────────────────────────────
        var canvasGO = new GameObject("LessonGuideCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── 1. GUIDE CARD PANEL ───────────────────────────────────
        var guideCard = MakePanel(canvasGO, "GuideCardPanel",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 0f), new Vector2(0f, 740f),
            new Color(0.06f, 0.06f, 0.12f, 0.96f));

        // Step counter
        MakeTMP(guideCard, "StepCounterText", "Step 1 / 6",
            16, TextAlignmentOptions.TopLeft,
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -16), new Vector2(200, -46));

        // Progress bar BG
        var barBG = MakeImage(guideCard, "ProgressBarBG",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(20, -50), new Vector2(-20, -66),
            new Color(0.2f, 0.2f, 0.2f, 1f));

        // Progress bar fill
        var barFill = MakeImage(barBG, "ProgressBarFill",
            new Vector2(0, 0), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            new Color(0.49f, 0.36f, 0.99f, 1f));   // purple for LL
        barFill.GetComponent<Image>().type       = Image.Type.Filled;
        barFill.GetComponent<Image>().fillMethod  = Image.FillMethod.Horizontal;
        barFill.GetComponent<Image>().fillAmount  = 0.16f;

        // Step title
        var titleTMP = MakeTMP(guideCard, "StepTitleText", "What is a Linked List?",
            28, TextAlignmentOptions.TopLeft,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(20, -74), new Vector2(-20, -120),
            bold: true);
        titleTMP.GetComponent<TextMeshProUGUI>().color = Color.white;

        // Step body
        var bodyTMP = MakeTMP(guideCard, "StepBodyText",
            "Elements are stored in separate nodes connected by pointers.",
            22, TextAlignmentOptions.TopLeft,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(20, -130), new Vector2(-20, -520));
        bodyTMP.GetComponent<TextMeshProUGUI>().color = new Color(0.85f, 0.85f, 0.85f, 1f);

        // Minimise button
        MakeButton(guideCard, "MinimiseButton", "−",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-70, -10), new Vector2(-10, -60),
            new Color(0.25f, 0.25f, 0.3f, 1f));

        // Next button
        MakeButton(guideCard, "NextStepButton", "Next →",
            new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-240, 20), new Vector2(-20, 80),
            new Color(0.49f, 0.36f, 0.99f, 1f));

        // Return button (hidden initially)
        var returnBtn = MakeButton(guideCard, "ReturnButton", "Return to Lesson",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(-160, 20), new Vector2(160, 80),
            new Color(0.2f, 0.7f, 0.4f, 1f));
        returnBtn.SetActive(false);

        // ── 2. COLLAPSED TAB ──────────────────────────────────────
        var collapsedTab = MakePanel(canvasGO, "CollapsedTab",
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(20f, 20f), new Vector2(220f, 90f),
            new Color(0.10f, 0.10f, 0.15f, 0.95f));
        MakeButton(collapsedTab, "RestoreButton", "📖 Guide",
            new Vector2(0, 0), new Vector2(1, 1),
            Vector2.zero, Vector2.zero,
            new Color(0.49f, 0.36f, 0.99f, 1f));
        collapsedTab.SetActive(false);

        // ── 3. OPERATION FEEDBACK PANEL ──────────────────────────
        // Anchored top — shown when student performs an operation in L4
        var opPanel = MakePanel(canvasGO, "OpFeedbackPanel",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -220), new Vector2(0, 0),
            new Color(0.05f, 0.08f, 0.05f, 0.96f));

        var opTitleTMP = MakeTMP(opPanel, "OpFeedbackTitle", "INSERT_HEAD",
            30, TextAlignmentOptions.Left,
            new Vector2(0, 1), new Vector2(0.6f, 1),
            new Vector2(20, -16), new Vector2(0, -60),
            bold: true);
        opTitleTMP.GetComponent<TextMeshProUGUI>().color = new Color(0.49f, 0.99f, 0.5f, 1f);

        MakeTMP(opPanel, "OpFeedbackBody", "new_node.next = head\nhead = new_node\nTime: O(1)",
            20, TextAlignmentOptions.Left,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(20, -64), new Vector2(-20, -160));

        var badgeTMP = MakeTMP(opPanel, "OpComplexityBadge", "O(1)",
            28, TextAlignmentOptions.Right,
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-160, -10), new Vector2(-20, -60),
            bold: true);
        badgeTMP.GetComponent<TextMeshProUGUI>().color = new Color(0.49f, 0.99f, 0.5f, 1f);

        MakeTMP(opPanel, "OperationLogText", "",
            17, TextAlignmentOptions.Left,
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(20, 10), new Vector2(-20, 90));

        opPanel.SetActive(false);

        // ── 4. TOAST PANEL ───────────────────────────────────────
        var toastPanel = MakePanel(canvasGO, "ToastPanel",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(-300, -120), new Vector2(300, -20),
            new Color(0.08f, 0.08f, 0.12f, 0.95f));
        MakeImage(toastPanel, "ToastIcon",
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(10, -30), new Vector2(70, 30),
            Color.white);
        MakeTMP(toastPanel, "ToastText", "Great!",
            22, TextAlignmentOptions.MidlineLeft,
            new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(80, 0), new Vector2(-10, 0));
        toastPanel.SetActive(false);

        // ── 5. COMPLEXITY TABLE PANEL ────────────────────────────
        var tablePanel = MakePanel(canvasGO, "ComplexityTablePanel",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-440, -20), new Vector2(-20, -440),
            new Color(0.05f, 0.05f, 0.10f, 0.95f));
        MakeTMP(tablePanel, "ComplexityTableText",
            "Operation               | Time\n" +
            "──────────────────────────────────\n" +
            "  Access by Index        | O(n)\n" +
            "  Traversal              | O(n)\n" +
            "  Linear Search          | O(n)\n" +
            "  Insert at Beginning    | O(1)\n" +
            "  Insert at End          | O(n)\n" +
            "  Insert at Position     | O(n)\n" +
            "  Delete from Beginning  | O(1)\n" +
            "  Delete from End        | O(n)\n" +
            "  Delete by Value        | O(n)\n" +
            "  Reverse List           | O(n)\n" +
            "  Find Middle (Floyd's)  | O(n)\n" +
            "  Space Complexity       | O(n)",
            16, TextAlignmentOptions.TopLeft,
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(14, -14), new Vector2(-14, -360));
        MakeTMP(tablePanel, "ActiveOperationLabel", "",
            15, TextAlignmentOptions.BottomLeft,
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(14, 8), new Vector2(-14, 38));
        tablePanel.SetActive(false);

        // ── Done ──────────────────────────────────────────────────
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build LL LessonGuideCanvas");
        Selection.activeGameObject = canvasGO;
        Debug.Log("[BuildLessonGuideCanvas_LL] ✅ LessonGuideCanvas created! " +
                  "Now assign the fields to ARLinkedListLessonGuide in the Inspector.");
    }

    // ── Helpers ───────────────────────────────────────────────────

    static GameObject MakePanel(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt  = go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color    = color;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return go;
    }

    static GameObject MakeTMP(GameObject parent, string name, string text,
        float fontSize, TextAlignmentOptions alignment,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, bool bold = false)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt  = go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.fontSize         = fontSize;
        tmp.alignment        = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode     = TextOverflowModes.Overflow;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return go;
    }

    static GameObject MakeButton(GameObject parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
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

        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        var lrt = lblGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var tmp = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return go;
    }

    static GameObject MakeImage(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt  = go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color    = color;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return go;
    }
}
#endif