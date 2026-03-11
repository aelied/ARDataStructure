using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// FIXED: 
/// 1. currentSceneKey is now set BEFORE LoadTutorialProgress() runs in Start(),
///    so PlayerPrefs keys are consistent between saves and loads.
/// 2. SetSceneKey() no longer double-loads and overrides already-seen status.
/// 3. Hint button duplication on app restart is prevented.
/// </summary>
public class TutorialSystem : MonoBehaviour
{
    [System.Serializable]
    public class TutorialStep
    {
        public string stepID;
        public string title;
        [TextArea(3, 10)]
        public string textContent;
        public VideoClip videoClip;
        public Sprite imageSprite;
    }

    [Header("UI References")]
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

    [Header("Hint Button")]
    public GameObject hintButtonPrefab;
    public Transform hintButtonContainer;

    [Header("Scene Key — set this in the Inspector (unique per scene)")]
    [Tooltip("Set this in the Inspector so it is correct BEFORE Start() runs. " +
             "E.g. 'CityGraph'. Only call SetSceneKey() at runtime if you need " +
             "to change it dynamically after the scene has loaded.")]
    public string sceneKey = "CityGraph";

    [Header("Tutorial Steps")]
    public List<TutorialStep> tutorialSteps = new List<TutorialStep>();

    // ── Private state ──────────────────────────────────────────────────────
    private string currentSceneKey = "";
    private Dictionary<string, bool> tutorialShownStatus = new Dictionary<string, bool>();
    private Dictionary<string, GameObject> hintButtons   = new Dictionary<string, GameObject>();
    private int currentStepIndex = 0;
    private List<TutorialStep> currentTutorialSequence   = new List<TutorialStep>();

    // ── Unity lifecycle ────────────────────────────────────────────────────

    void Awake()
    {
        // FIX: assign the key here, before Start() / any external SetSceneKey() call,
        // so that LoadTutorialProgress() always uses the correct prefixed key.
        currentSceneKey = sceneKey;
    }

    void Start()
    {
        if (tutorialPopupPanel != null)
            tutorialPopupPanel.SetActive(false);

        if (closeButton   != null) closeButton.onClick.AddListener(CloseTutorial);
        if (nextButton     != null) nextButton.onClick.AddListener(ShowNextStep);
        if (previousButton != null) previousButton.onClick.AddListener(ShowPreviousStep);

        // Load progress now that currentSceneKey is guaranteed to be set.
        LoadTutorialProgress();

        // Clear any stale hint buttons left over from a previous run.
        ClearAllHintButtons();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Call this ONLY if you need to change the scene key at runtime after
    /// the scene has already loaded (rare). In most cases just set the
    /// sceneKey field in the Inspector instead.
    /// </summary>
    public void SetSceneKey(string key)
    {
        if (currentSceneKey == key) return; // nothing changed — skip reload
        currentSceneKey = key;
        LoadTutorialProgress();
    }

    public void ShowTutorial(string stepID, bool forceShow = false)
    {
        TutorialStep step = tutorialSteps.Find(s => s.stepID == stepID);
        if (step == null)
        {
            Debug.LogWarning($"[TutorialSystem] Tutorial step '{stepID}' not found!");
            return;
        }

        bool isFirstTime = !tutorialShownStatus.ContainsKey(stepID) || !tutorialShownStatus[stepID];

        if (isFirstTime || forceShow)
        {
            ClearAllHintButtons();

            currentTutorialSequence = new List<TutorialStep> { step };
            currentStepIndex = 0;
            DisplayCurrentStep();

            if (isFirstTime)
                SaveTutorialProgress(stepID);
        }
        else
        {
            // Already seen — show a hint button instead of repeating the popup.
            ClearAllHintButtons();
            ShowHintButton(stepID);
        }
    }

    public void ShowTutorialSequence(List<string> stepIDs, bool forceShow = false)
    {
        currentTutorialSequence.Clear();
        bool anyFirstTime = false;

        foreach (string id in stepIDs)
        {
            TutorialStep step = tutorialSteps.Find(s => s.stepID == id);
            if (step != null)
            {
                currentTutorialSequence.Add(step);
                bool isFirstTime = !tutorialShownStatus.ContainsKey(id) || !tutorialShownStatus[id];
                if (isFirstTime) anyFirstTime = true;
            }
        }

        if (currentTutorialSequence.Count > 0 && (anyFirstTime || forceShow))
        {
            currentStepIndex = 0;
            DisplayCurrentStep();

            foreach (var step in currentTutorialSequence)
                SaveTutorialProgress(step.stepID);
        }
    }

    public void ResetAllTutorials()
    {
        foreach (var step in tutorialSteps)
        {
            string key = BuildPrefKey(step.stepID);
            PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();

        tutorialShownStatus.Clear();
        LoadTutorialProgress();
        ClearAllHintButtons();
    }

    // ── Internal helpers ───────────────────────────────────────────────────

    /// <summary>Returns the PlayerPrefs key for a given stepID.</summary>
    string BuildPrefKey(string stepID) => $"Tutorial_{currentSceneKey}_{stepID}";

    void LoadTutorialProgress()
    {
        tutorialShownStatus.Clear();
        foreach (var step in tutorialSteps)
            tutorialShownStatus[step.stepID] = PlayerPrefs.GetInt(BuildPrefKey(step.stepID), 0) == 1;
    }

    void SaveTutorialProgress(string stepID)
    {
        PlayerPrefs.SetInt(BuildPrefKey(stepID), 1);
        PlayerPrefs.Save();
        tutorialShownStatus[stepID] = true;
    }

    void DisplayCurrentStep()
    {
        if (currentStepIndex < 0 || currentStepIndex >= currentTutorialSequence.Count)
            return;

        TutorialStep step = currentTutorialSequence[currentStepIndex];

        if (tutorialPopupPanel != null)
            tutorialPopupPanel.SetActive(true);

        if (tutorialTitle != null) tutorialTitle.text = step.title;
        if (tutorialText  != null) tutorialText.text  = step.textContent;

        // Handle video
        if (step.videoClip != null && videoPlayer != null && videoPlayerObject != null)
        {
            videoPlayerObject.SetActive(true);
            videoPlayer.clip = step.videoClip;
            videoPlayer.Play();
            if (tutorialImage != null) tutorialImage.gameObject.SetActive(false);
        }
        else
        {
            if (videoPlayerObject != null) videoPlayerObject.SetActive(false);

            if (tutorialImage != null)
            {
                tutorialImage.gameObject.SetActive(step.imageSprite != null);
                if (step.imageSprite != null) tutorialImage.sprite = step.imageSprite;
            }
        }

        UpdateNavigationButtons();

        if (pageIndicator != null)
        {
            bool multiStep = currentTutorialSequence.Count > 1;
            pageIndicator.SetActive(multiStep);
            if (multiStep && pageText != null)
                pageText.text = $"{currentStepIndex + 1} / {currentTutorialSequence.Count}";
        }
    }

    void UpdateNavigationButtons()
    {
        if (previousButton != null)
            previousButton.gameObject.SetActive(currentStepIndex > 0);

        if (nextButton != null)
            nextButton.gameObject.SetActive(currentStepIndex < currentTutorialSequence.Count - 1);
    }

    void ShowNextStep()
    {
        if (currentStepIndex < currentTutorialSequence.Count - 1)
        {
            currentStepIndex++;
            DisplayCurrentStep();
        }
    }

    void ShowPreviousStep()
    {
        if (currentStepIndex > 0)
        {
            currentStepIndex--;
            DisplayCurrentStep();
        }
    }

    void CloseTutorial()
    {
        if (tutorialPopupPanel != null)
            tutorialPopupPanel.SetActive(false);

        if (videoPlayer != null)
            videoPlayer.Stop();

        currentTutorialSequence.Clear();
        currentStepIndex = 0;
    }

    // ── Hint buttons ───────────────────────────────────────────────────────

    void ShowHintButton(string stepID)
    {
        // Reuse existing button if it's still alive.
        if (hintButtons.TryGetValue(stepID, out GameObject existing) && existing != null)
        {
            StartCoroutine(PulseHintButton(existing));
            return;
        }

        if (hintButtonPrefab == null || hintButtonContainer == null) return;

        // Prune dead entries.
        var dead = new List<string>();
        foreach (var kvp in hintButtons)
            if (kvp.Value == null) dead.Add(kvp.Key);
        foreach (var k in dead) hintButtons.Remove(k);

        GameObject hintButton = Instantiate(hintButtonPrefab, hintButtonContainer);
        hintButton.name = $"HintButton_{stepID}";

        Button btn = hintButton.GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(() => ShowTutorial(stepID, true));

        hintButtons[stepID] = hintButton;
        StartCoroutine(PulseHintButton(hintButton));
    }

    IEnumerator PulseHintButton(GameObject button)
    {
        if (button == null) yield break;
        Image img = button.GetComponent<Image>();
        if (img == null) yield break;

        Color original = img.color;
        for (int i = 0; i < 3; i++)
        {
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                if (button == null || img == null) yield break;
                img.color = new Color(original.r, original.g, original.b,
                                      Mathf.Lerp(original.a * 0.7f, original.a, t / 0.5f));
                yield return null;
            }
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                if (button == null || img == null) yield break;
                img.color = new Color(original.r, original.g, original.b,
                                      Mathf.Lerp(original.a, original.a * 0.7f, t / 0.5f));
                yield return null;
            }
        }
        if (img != null) img.color = original;
    }

    public void ClearHintButton(string stepID)
    {
        if (hintButtons.TryGetValue(stepID, out GameObject btn))
        {
            if (btn != null) Destroy(btn);
            hintButtons.Remove(stepID);
        }
    }

    public void ClearAllHintButtons()
    {
        if (hintButtonContainer != null)
            foreach (Transform child in hintButtonContainer)
                Destroy(child.gameObject);

        hintButtons.Clear();
    }

    void OnDestroy()
    {
        ClearAllHintButtons();
    }
}