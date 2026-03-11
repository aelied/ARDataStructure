/// <summary>
/// IARLessonGuide.cs
/// ─────────────────────────────────────────────────────────────
/// Every topic's AR lesson guide script must implement this interface
/// so ARModeSelectionController can talk to any of them without
/// knowing which data structure it is.
///
/// TOPICS TO IMPLEMENT:
///   ARArrayLessonGuide       ← already exists, add interface below
///   ARStackLessonGuide       ← future
///   ARQueueLessonGuide       ← future
///   ARLinkedListLessonGuide  ← future
///   ARTreeLessonGuide        ← future
///   ARGraphLessonGuide       ← future
/// </summary>
public interface IARLessonGuide
{
    /// <summary>
    /// Enable or disable the guide component entirely.
    /// Called by ARModeSelectionController in Awake() to suppress
    /// the guide until the user explicitly picks "Guided AR Lesson".
    /// </summary>
    void SetGuideEnabled(bool enabled);

    /// <summary>
    /// Begin the guided lesson for the given lesson index (0-based).
    /// The guide reads topic name from PlayerPrefs if it needs it.
    /// Called when the user taps "Guided AR Lesson".
    /// </summary>
    void BeginGuidedLesson(int lessonIndex);
}