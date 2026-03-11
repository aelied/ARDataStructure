// ================================================================
//  QuizCardBuilder.cs
//  Place this file inside any folder named "Editor" in your project.
//  e.g.  Assets/Editor/QuizCardBuilder.cs
//
//  Usage:
//    Unity menu bar → Tools → StructuReality → Build QuizCard Prefab
//
//  What it creates:
//    Assets/Prefabs/QuizCard.prefab  (fully wired, ready to assign
//    in TestsTabController → quizCardPrefab)
// ================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using System.IO;

public class QuizCardBuilder : EditorWindow
{
    // ── Tweakable dimensions ──────────────────────────────────────
    private float cardWidth      = 680f;
    private float cardHeight     = 110f;
    private float iconSize       = 72f;
    private float padding        = 16f;
    private float buttonWidth    = 100f;
    private float buttonHeight   = 44f;
    private float badgeWidth     = 60f;
    private float badgeHeight    = 24f;
    private float cornerRadius   = 18f;

    // ── Colors ────────────────────────────────────────────────────
    private Color cardBg         = new Color(1f,    1f,    1f,    1f);
    private Color iconBg         = new Color(0.67f, 0.84f, 0.90f, 1f);
    private Color titleCol       = new Color(0.10f, 0.10f, 0.15f, 1f);
    private Color subtitleCol    = new Color(0.45f, 0.47f, 0.55f, 1f);
    private Color badgeBg        = new Color(0.20f, 0.47f, 0.95f, 1f);
    private Color badgeTextCol   = Color.white;
    private Color btnBg          = new Color(0.20f, 0.47f, 0.95f, 1f);
    private Color btnTextCol     = Color.white;

    private string savePath      = "Assets/Prefabs";
    private string prefabName    = "QuizCard";

    // ── Window ────────────────────────────────────────────────────
    [MenuItem("Tools/StructuReality/Build QuizCard Prefab")]
    public static void ShowWindow()
    {
        QuizCardBuilder w = GetWindow<QuizCardBuilder>("QuizCard Builder");
        w.minSize = new Vector2(420, 560);
    }

    void OnGUI()
    {
        GUIStyle header = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 15,
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("QuizCard Prefab Builder", header);
        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Builds a fully wired QuizCard prefab and saves it to the path below.\n" +
            "Assign the result to TestsTabController → Quiz Card Prefab.",
            MessageType.Info);
        EditorGUILayout.Space(10);

        // ── Save location ─────────────────────────────────────────
        EditorGUILayout.LabelField("Save Settings", EditorStyles.boldLabel);
        savePath   = EditorGUILayout.TextField("Save Folder",  savePath);
        prefabName = EditorGUILayout.TextField("Prefab Name",  prefabName);
        EditorGUILayout.Space(8);

        // ── Card size ─────────────────────────────────────────────
        EditorGUILayout.LabelField("Card Dimensions", EditorStyles.boldLabel);
        cardWidth    = EditorGUILayout.FloatField("Card Width",    cardWidth);
        cardHeight   = EditorGUILayout.FloatField("Card Height",   cardHeight);
        iconSize     = EditorGUILayout.FloatField("Icon Size",     iconSize);
        padding      = EditorGUILayout.FloatField("Padding",       padding);
        buttonWidth  = EditorGUILayout.FloatField("Button Width",  buttonWidth);
        buttonHeight = EditorGUILayout.FloatField("Button Height", buttonHeight);
        EditorGUILayout.Space(8);

        // ── Colors ────────────────────────────────────────────────
        EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
        cardBg      = EditorGUILayout.ColorField("Card Background", cardBg);
        iconBg      = EditorGUILayout.ColorField("Icon Background", iconBg);
        titleCol    = EditorGUILayout.ColorField("Title Text",      titleCol);
        subtitleCol = EditorGUILayout.ColorField("Subtitle Text",   subtitleCol);
        badgeBg     = EditorGUILayout.ColorField("Badge Background",badgeBg);
        btnBg       = EditorGUILayout.ColorField("Button Color",    btnBg);
        EditorGUILayout.Space(12);

        // ── Build button ──────────────────────────────────────────
        GUI.backgroundColor = new Color(0.25f, 0.55f, 1f);
        if (GUILayout.Button("Build QuizCard Prefab", GUILayout.Height(44)))
            BuildPrefab();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "After building, make sure TextMeshPro is imported.\n" +
            "If fonts appear pink, assign a TMP font in each TMP component.",
            MessageType.Warning);
    }

    // ════════════════════════════════════════════════════════════════
    //  BUILD
    // ════════════════════════════════════════════════════════════════
    void BuildPrefab()
    {
        // ── Ensure save folder exists ─────────────────────────────
        if (!AssetDatabase.IsValidFolder(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        // ── Root: QuizCard ────────────────────────────────────────
        GameObject root = new GameObject(prefabName);
        RectTransform rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(cardWidth, cardHeight);

        // Card background image
        Image cardImg = root.AddComponent<Image>();
        cardImg.color = cardBg;
        // Soft shadow via Outline (simple; swap for a Shadow component if preferred)
        Shadow shadow = root.AddComponent<Shadow>();
        shadow.effectColor    = new Color(0f, 0f, 0f, 0.12f);
        shadow.effectDistance = new Vector2(0f, -3f);

        // ── Layout: HorizontalLayoutGroup on root ─────────────────
        HorizontalLayoutGroup hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.padding            = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
        hlg.spacing            = padding;
        hlg.childAlignment     = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = false;
        hlg.childControlHeight     = false;

        // Content size fitter so the card grows with content
        ContentSizeFitter csf = root.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Attach QuizCard behaviour
        root.AddComponent<QuizCard>();

        // ── 1. IconBox ────────────────────────────────────────────
        GameObject iconBox = MakeChild(root, "IconBox");
        SetRectSize(iconBox, iconSize, iconSize);
        Image iconImg = iconBox.AddComponent<Image>();
        iconImg.color = iconBg;
        AddLayoutElement(iconBox, iconSize, iconSize);

        // Rounded look via outline
        Outline iconOutline = iconBox.AddComponent<Outline>();
        iconOutline.effectColor    = new Color(0f, 0f, 0f, 0.06f);
        iconOutline.effectDistance = new Vector2(1f, -1f);

        // IconText (circled number)
        GameObject iconTextGO = MakeChild(iconBox, "IconText");
        StretchFill(iconTextGO);
        TextMeshProUGUI iconTMP = iconTextGO.AddComponent<TextMeshProUGUI>();
        iconTMP.text      = "①";
        iconTMP.fontSize  = 32;
        iconTMP.alignment = TextAlignmentOptions.Center;
        iconTMP.color     = new Color(0.15f, 0.35f, 0.65f);

        // ── 2. ContentGroup (fills remaining width) ───────────────
        GameObject content = MakeChild(root, "ContentGroup");
        LayoutElement contentLE = content.AddComponent<LayoutElement>();
        contentLE.flexibleWidth = 1f;
        contentLE.minHeight     = cardHeight - padding * 2f;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment        = TextAnchor.MiddleLeft;
        vlg.spacing               = 3f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight     = false;
        vlg.childControlWidth      = true;

        // ── 2a. Top row: TitleText + BadgeBackground ──────────────
        GameObject topRow = MakeChild(content, "TopRow");
        HorizontalLayoutGroup topHLG = topRow.AddComponent<HorizontalLayoutGroup>();
        topHLG.childAlignment        = TextAnchor.MiddleLeft;
        topHLG.spacing               = 8f;
        topHLG.childForceExpandWidth  = false;
        topHLG.childForceExpandHeight = false;
        topHLG.childControlWidth      = false;
        topHLG.childControlHeight     = false;
        AddLayoutElement(topRow, -1, 28f);

        // TitleText
        GameObject titleGO = MakeChild(topRow, "TitleText");
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "Quiz 1";
        titleTMP.fontSize  = 18;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = titleCol;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        AddLayoutElement(titleGO, 120f, 28f);

        // BadgeBackground (small pill)
        GameObject badgeBgGO = MakeChild(topRow, "BadgeBackground");
        Image badgeImg = badgeBgGO.AddComponent<Image>();
        badgeImg.color = badgeBg;
        Outline badgeOutline = badgeBgGO.AddComponent<Outline>();
        badgeOutline.effectColor    = new Color(0f, 0f, 0f, 0.1f);
        badgeOutline.effectDistance = new Vector2(1f, -1f);
        AddLayoutElement(badgeBgGO, badgeWidth, badgeHeight);

        HorizontalLayoutGroup badgeHLG = badgeBgGO.AddComponent<HorizontalLayoutGroup>();
        badgeHLG.childAlignment = TextAnchor.MiddleCenter;
        badgeHLG.padding        = new RectOffset(6, 6, 2, 2);
        badgeHLG.childForceExpandWidth  = false;
        badgeHLG.childForceExpandHeight = false;

        // BadgeText inside pill
        GameObject badgeTextGO = MakeChild(badgeBgGO, "BadgeText");
        TextMeshProUGUI badgeTMP = badgeTextGO.AddComponent<TextMeshProUGUI>();
        badgeTMP.text      = "NEW";
        badgeTMP.fontSize  = 11;
        badgeTMP.fontStyle = FontStyles.Bold;
        badgeTMP.color     = badgeTextCol;
        badgeTMP.alignment = TextAlignmentOptions.Center;
        AddLayoutElement(badgeTextGO, badgeWidth - 12f, badgeHeight - 4f);

        // ── 2b. SubtitleText ──────────────────────────────────────
        GameObject subtitleGO = MakeChild(content, "SubtitleText");
        TextMeshProUGUI subtitleTMP = subtitleGO.AddComponent<TextMeshProUGUI>();
        subtitleTMP.text      = "Question preview appears here…";
        subtitleTMP.fontSize  = 13;
        subtitleTMP.color     = subtitleCol;
        subtitleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        subtitleTMP.enableWordWrapping = false;
        subtitleTMP.overflowMode       = TextOverflowModes.Ellipsis;
        AddLayoutElement(subtitleGO, -1, 22f);

        // ── 3. StartButton ────────────────────────────────────────
        GameObject btnGO = MakeChild(root, "StartButton");
        AddLayoutElement(btnGO, buttonWidth, buttonHeight);

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = btnBg;

        Outline btnOutline = btnGO.AddComponent<Outline>();
        btnOutline.effectColor    = new Color(0f, 0f, 0f, 0.15f);
        btnOutline.effectDistance = new Vector2(0f, -2f);

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = btnBg;
        cb.highlightedColor = Color.Lerp(btnBg, Color.white, 0.15f);
        cb.pressedColor     = Color.Lerp(btnBg, Color.black, 0.20f);
        cb.disabledColor    = new Color(0.6f, 0.6f, 0.6f);
        btn.colors = cb;

        // Button label
        GameObject btnLabelGO = MakeChild(btnGO, "ButtonLabel");
        StretchFill(btnLabelGO);
        TextMeshProUGUI btnTMP = btnLabelGO.AddComponent<TextMeshProUGUI>();
        btnTMP.text      = "Start";
        btnTMP.fontSize  = 15;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.color     = btnTextCol;
        btnTMP.alignment = TextAlignmentOptions.Center;
        // Don't block raycasts on label — button handles it
        btnTMP.raycastTarget = false;

        // ── Save as prefab ────────────────────────────────────────
        string fullPath = $"{savePath}/{prefabName}.prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, fullPath);
        DestroyImmediate(root);

        if (saved != null)
        {
            AssetDatabase.Refresh();
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            Debug.Log($"[QuizCardBuilder] ✅ Prefab saved to: {fullPath}");
            EditorUtility.DisplayDialog(
                "QuizCard Built!",
                $"Prefab saved to:\n{fullPath}\n\nNow assign it to:\nTestsTabController → Quiz Card Prefab",
                "OK");
        }
        else
        {
            Debug.LogError($"[QuizCardBuilder] ❌ Failed to save prefab to {fullPath}");
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════

    static GameObject MakeChild(GameObject parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void SetRectSize(GameObject go, float w, float h)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
    }

    /// <summary>Stretch to fill parent (anchors 0,0 → 1,1, offsets 0).</summary>
    static void StretchFill(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }

    static void AddLayoutElement(GameObject go, float preferredWidth, float preferredHeight)
    {
        LayoutElement le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (preferredWidth  > 0) le.preferredWidth  = preferredWidth;
        if (preferredHeight > 0) le.preferredHeight = preferredHeight;
        le.minHeight = preferredHeight > 0 ? preferredHeight : -1;
    }
}
#endif