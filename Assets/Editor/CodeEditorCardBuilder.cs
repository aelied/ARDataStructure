// ================================================================
//  CodeEditorCardBuilder.cs
//  Place in:  Assets/Editor/CodeEditorCardBuilder.cs
//
//  Unity menu:  Tools → Rebuild Code Editor Card
//
//  Select the existing "CodeEditorCard" GameObject in the
//  Hierarchy before running, OR it will search for one
//  automatically. The old card is REPLACED with a scrollable
//  version that matches the rest of the CodeLab theme.
//
//  After building, re-wire the three fields on CodeLabPanel:
//    • codeInputField   → CodeEditorCard/Viewport/Content/CodeInputField
//    • lineNumbersText  → CodeEditorCard/LineNumberStrip/LineNumbersText
//    • codeScrollRect   → CodeEditorCard   (the ScrollRect component)
// ================================================================
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class CodeEditorCardBuilder : EditorWindow
{
    // ── Colours (same palette as CodeLabPanelBuilder) ─────────
    static readonly Color CodeBg       = new Color(0.031f, 0.039f, 0.055f, 1f); // #080A0E
    static readonly Color LineNumBg    = new Color(0.024f, 0.031f, 0.047f, 1f); // #060810
    static readonly Color BorderColor  = new Color(0.145f, 0.165f, 0.227f, 1f); // #252A3A
    static readonly Color AccentBlue   = new Color(0.290f, 0.486f, 1.000f, 0.5f);
    static readonly Color TextWhite    = new Color(0.933f, 0.941f, 0.973f, 1f); // #EEF0F8
    static readonly Color TextDim      = new Color(0.290f, 0.318f, 0.408f, 1f); // #4A5168

    // ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Rebuild Code Editor Card")]
    public static void Build()
    {
        // Find the existing CodeEditorCard
        GameObject existing = null;

        if (Selection.activeGameObject != null &&
            Selection.activeGameObject.name == "CodeEditorCard")
        {
            existing = Selection.activeGameObject;
        }
        else
        {
            // Search entire scene
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.name == "CodeEditorCard") { existing = go; break; }
            }
        }

        if (existing == null)
        {
            Debug.LogError("[CodeEditorCardBuilder] Could not find a GameObject named 'CodeEditorCard' in the scene. " +
                           "Select it in the Hierarchy and try again.");
            return;
        }

        Transform parent    = existing.transform.parent;
        int       sibIndex  = existing.transform.GetSiblingIndex();

        // Record for undo, then destroy old card
        Undo.DestroyObjectImmediate(existing);

        // Build the replacement
        GameObject newCard = BuildScrollableEditorCard(parent);
        newCard.transform.SetSiblingIndex(sibIndex);

        Undo.RegisterCreatedObjectUndo(newCard, "Rebuild Code Editor Card");
        Selection.activeGameObject = newCard;

        Debug.Log("✅ CodeEditorCard rebuilt as scrollable. " +
                  "Re-wire codeInputField, lineNumbersText, and codeScrollRect on CodeLabPanel.");
    }

    // ─────────────────────────────────────────────────────────────
    static GameObject BuildScrollableEditorCard(Transform parent)
    {
        // ── Root: CodeEditorCard ──────────────────────────────────
        GameObject card = MakeRect("CodeEditorCard", parent);
        var cardLE      = card.AddComponent<LayoutElement>();
        cardLE.flexibleWidth    = 1;
        cardLE.preferredHeight  = 300;   // visible window height — adjust freely

        // Background + clipping mask
        AddImage(card, CodeBg);
        card.AddComponent<RectMask2D>();

        // Horizontal layout: [LineNumberStrip | vertical divider | ScrollView]
        var hLayout                   = card.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing               = 0;
        hLayout.childAlignment        = TextAnchor.UpperLeft;
        hLayout.childControlWidth     = true;
        hLayout.childControlHeight    = true;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight= true;
        hLayout.padding               = new RectOffset(0, 0, 0, 0);

        // ── Line number strip (fixed 44 px wide) ─────────────────
        GameObject lineStrip = MakeRect("LineNumberStrip", card.transform);
        var stripLE          = lineStrip.AddComponent<LayoutElement>();
        stripLE.minWidth     = 44;
        stripLE.preferredWidth = 44;
        stripLE.flexibleWidth  = 0;
        AddImage(lineStrip, LineNumBg);

        GameObject lineNumsGO  = MakeTMP("LineNumbersText", lineStrip.transform,
                                         "1\n2\n3\n4\n5", 20, TextDim, FontStyles.Normal);
        Stretch(lineNumsGO);
        var lineTmp             = lineNumsGO.GetComponent<TextMeshProUGUI>();
        lineTmp.alignment       = TextAlignmentOptions.TopRight;
        lineTmp.margin          = new Vector4(0, 12, 6, 12);
        lineTmp.enableWordWrapping = false;

        // ── Vertical divider ─────────────────────────────────────
        GameObject divider    = MakeRect("LineNumBorder", card.transform);
        var divLE             = divider.AddComponent<LayoutElement>();
        divLE.minWidth        = 1;
        divLE.preferredWidth  = 1;
        divLE.flexibleWidth   = 0;
        AddImage(divider, BorderColor);

        // ── ScrollRect for code area ──────────────────────────────
        GameObject srGO = MakeRect("CodeScrollView", card.transform);
        var srLE        = srGO.AddComponent<LayoutElement>();
        srLE.flexibleWidth  = 1;
        srLE.flexibleHeight = 1;
        AddImage(srGO, Color.clear);

        ScrollRect sr   = srGO.AddComponent<ScrollRect>();
        sr.horizontal        = false;
        sr.vertical          = true;
        sr.scrollSensitivity = 40;
        sr.movementType      = ScrollRect.MovementType.Clamped;
        sr.inertia           = true;
        sr.decelerationRate  = 0.135f;

        // Viewport
        GameObject viewport = MakeRect("Viewport", srGO.transform);
        Stretch(viewport);
        viewport.AddComponent<RectMask2D>();
        sr.viewport = viewport.GetComponent<RectTransform>();

        // Content (grows with text)
        GameObject content = MakeRect("Content", viewport.transform);
        var cRT             = content.GetComponent<RectTransform>();
        cRT.anchorMin       = new Vector2(0f, 1f);
        cRT.anchorMax       = new Vector2(1f, 1f);
        cRT.pivot           = new Vector2(0.5f, 1f);
        cRT.offsetMin       = cRT.offsetMax = Vector2.zero;
        sr.content          = cRT;

        var csf             = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit     = ContentSizeFitter.FitMode.PreferredSize;

        var vLayout                    = content.AddComponent<VerticalLayoutGroup>();
        vLayout.childControlWidth      = true;
        vLayout.childControlHeight     = true;
        vLayout.childForceExpandWidth  = true;
        vLayout.childForceExpandHeight = false;
        vLayout.padding                = new RectOffset(0, 0, 0, 0);

        // ── TMP_InputField inside Content ─────────────────────────
        GameObject inputGO = MakeRect("CodeInputField", content.transform);
        var inputLE        = inputGO.AddComponent<LayoutElement>();
        inputLE.flexibleWidth   = 1;
        inputLE.minHeight       = 300;   // never collapses smaller than the card
        inputLE.preferredHeight = -1;    // let TMP drive actual height
        AddImage(inputGO, Color.clear);

        TMP_InputField field  = inputGO.AddComponent<TMP_InputField>();
        field.lineType         = TMP_InputField.LineType.MultiLineNewline;
        field.contentType      = TMP_InputField.ContentType.Standard;

        // Text Area (viewport for the input field itself)
        GameObject textArea = MakeRect("Text Area", inputGO.transform);
        Stretch(textArea);
        textArea.AddComponent<RectMask2D>();
        field.textViewport = textArea.GetComponent<RectTransform>();

        // Placeholder
        GameObject ph = MakeTMP("Placeholder", textArea.transform,
                                "// write your solution here…", 24,
                                new Color(AccentBlue.r, AccentBlue.g, AccentBlue.b, 0.45f),
                                FontStyles.Italic);
        Stretch(ph);
        var phTmp             = ph.GetComponent<TextMeshProUGUI>();
        phTmp.margin          = new Vector4(10, 12, 10, 12);
        phTmp.alignment       = TextAlignmentOptions.TopLeft;
        phTmp.enableWordWrapping = false;
        field.placeholder     = phTmp;

        // Input text component
        GameObject inputText = MakeTMP("Text", textArea.transform, "", 24, TextWhite, FontStyles.Normal);
        Stretch(inputText);
        var inputTmp          = inputText.GetComponent<TextMeshProUGUI>();
        inputTmp.margin       = new Vector4(10, 12, 10, 12);
        inputTmp.alignment    = TextAlignmentOptions.TopLeft;
        inputTmp.enableWordWrapping = false;  // horizontal scroll if line is long
        field.textComponent   = inputTmp;

        return card;
    }

    // ─────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────

    static GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rt       = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Image AddImage(GameObject go, Color color)
    {
        Image img         = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color         = color;
        img.raycastTarget = false;
        return img;
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