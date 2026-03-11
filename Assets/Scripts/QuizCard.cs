// ================================================================
//  QuizCard.cs
//  Attach this to the root of your QuizCard prefab.
//  TestsTabController calls Setup() after instantiating each card.
//
//  REQUIRED HIERARCHY inside the prefab:
//
//  QuizCard  (Image + this script)
//  ├── IconBox         (Image – rounded square, colored per quiz)
//  │   └── IconText    (TMP – quiz number emoji/icon e.g. "①")
//  ├── ContentGroup    (child container)
//  │   ├── TitleText   (TMP – "Quiz 1")
//  │   ├── SubtitleText(TMP – question preview)
//  │   └── BadgeText   (TMP – "NEW" or "DONE")
//  └── StartButton     (Button)
//      └── ButtonLabel (TMP – "Start")
//
//  All child names must match exactly (case-sensitive) or
//  assign them manually via the Inspector fields below.
// ================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class QuizCard : MonoBehaviour
{
    // ── Inspector fields (auto-found if left null) ────────────────
    [Header("Card Elements")]
    public Image           iconBox;
    public TextMeshProUGUI iconText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public TextMeshProUGUI badgeText;
    public Image           badgeBackground;
    public Button          startButton;
    public TextMeshProUGUI buttonLabel;

    [Header("Card Background")]
    public Image cardBackground;

    [Header("Colors")]
    public Color cardBgColor      = new Color(1f, 1f, 1f, 1f);
    public Color newBadgeColor    = new Color(0.20f, 0.47f, 0.95f, 1f);
    public Color doneBadgeColor   = new Color(0.22f, 0.68f, 0.38f, 1f);
    public Color startBtnColor    = new Color(0.20f, 0.47f, 0.95f, 1f);
    public Color doneBtnColor     = new Color(0.22f, 0.68f, 0.38f, 1f);
    public Color titleColor       = new Color(0.10f, 0.10f, 0.15f, 1f);
    public Color subtitleColor    = new Color(0.45f, 0.47f, 0.55f, 1f);

    // ── Icons per quiz slot (emoji strings) ───────────────────────
    private static readonly string[] QuizIcons = { "①", "②", "③", "④", "⑤", "⑥", "⑦", "⑧" };

    // ── Callback wired by TestsTabController ──────────────────────
    private Action _onStartClicked;

    // ════════════════════════════════════════════════════════════════
    //  SETUP  –  called by TestsTabController after Instantiate()
    // ════════════════════════════════════════════════════════════════
    /// <summary>
    /// Configure this card.
    /// </summary>
    /// <param name="quizIndex">0-based index</param>
    /// <param name="questionPreview">Full question text (will be trimmed)</param>
    /// <param name="iconColor">Background color for the icon box</param>
    /// <param name="attempted">True if the player already completed this quiz</param>
    /// <param name="onStart">Callback fired when Start is tapped</param>
    public void Setup(
        int    quizIndex,
        string questionPreview,
        Color  iconColor,
        bool   attempted,
        Action onStart)
    {
        _onStartClicked = onStart;

        AutoFindReferences();
        ApplyCardBackground();
        SetIcon(quizIndex, iconColor);
        SetTitle(quizIndex);
        SetSubtitle(questionPreview);
        SetBadge(attempted);
        SetButton(attempted);
        WireButton();
    }

    // ════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ════════════════════════════════════════════════════════════════

    void AutoFindReferences()
    {
        if (cardBackground == null)
            cardBackground = GetComponent<Image>();

        if (iconBox      == null) iconBox      = FindDeep<Image>          ("IconBox");
        if (iconText     == null) iconText     = FindDeep<TextMeshProUGUI>("IconText");
        if (titleText    == null) titleText    = FindDeep<TextMeshProUGUI>("TitleText");
        if (subtitleText == null) subtitleText = FindDeep<TextMeshProUGUI>("SubtitleText");
        if (badgeText    == null) badgeText    = FindDeep<TextMeshProUGUI>("BadgeText");
        if (startButton  == null) startButton  = FindDeep<Button>         ("StartButton");

        if (startButton != null && buttonLabel == null)
            buttonLabel = startButton.GetComponentInChildren<TextMeshProUGUI>();

        if (badgeText != null && badgeBackground == null)
            badgeBackground = badgeText.GetComponentInParent<Image>();
    }

    void ApplyCardBackground()
    {
        if (cardBackground != null)
            cardBackground.color = cardBgColor;
    }

    void SetIcon(int quizIndex, Color iconColor)
    {
        if (iconBox != null)
            iconBox.color = iconColor;

        if (iconText != null)
        {
            iconText.text  = quizIndex < QuizIcons.Length ? QuizIcons[quizIndex] : $"Q{quizIndex + 1}";
            iconText.color = DarkenColor(iconColor, 0.45f);
        }
    }

    void SetTitle(int quizIndex)
    {
        if (titleText != null)
        {
            titleText.text  = $"Quiz {quizIndex + 1}";
            titleText.color = titleColor;
        }
    }

    void SetSubtitle(string questionPreview)
    {
        if (subtitleText == null) return;

        const int MAX = 60;
        string preview = string.IsNullOrEmpty(questionPreview)
            ? "Tap Start to begin"
            : questionPreview.Length > MAX
                ? questionPreview.Substring(0, MAX) + "…"
                : questionPreview;

        subtitleText.text  = preview;
        subtitleText.color = subtitleColor;
    }

    void SetBadge(bool attempted)
    {
        if (badgeText != null)
        {
            badgeText.text  = attempted ? "DONE" : "NEW";
            badgeText.color = Color.white;
        }

        if (badgeBackground != null)
            badgeBackground.color = attempted ? doneBadgeColor : newBadgeColor;
    }

    void SetButton(bool attempted)
    {
        if (startButton == null) return;

        Color btnColor = attempted ? doneBtnColor : startBtnColor;

        ColorBlock cb = startButton.colors;
        cb.normalColor      = btnColor;
        cb.highlightedColor = Color.Lerp(btnColor, Color.white, 0.15f);
        cb.pressedColor     = Color.Lerp(btnColor, Color.black, 0.20f);
        cb.disabledColor    = new Color(0.6f, 0.6f, 0.6f);
        startButton.colors  = cb;
        startButton.interactable = true;

        if (buttonLabel != null)
            buttonLabel.text = attempted ? "Redo" : "Start";
    }

    void WireButton()
    {
        if (startButton == null) return;
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(() => _onStartClicked?.Invoke());
    }

    // ── Darken a color by mixing with black ───────────────────────
    Color DarkenColor(Color c, float amount)
        => new Color(c.r * (1f - amount), c.g * (1f - amount), c.b * (1f - amount), c.a);

    // ── Deep child search by name ─────────────────────────────────
    T FindDeep<T>(string childName) where T : Component
    {
        Transform found = FindChildRecursive(transform, childName);
        return found != null ? found.GetComponent<T>() : null;
    }

    Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform r = FindChildRecursive(child, name);
            if (r != null) return r;
        }
        return null;
    }
}