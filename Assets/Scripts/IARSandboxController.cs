/// <summary>
/// IARSandboxController.cs
/// ─────────────────────────────────────────────────────────────
/// Every topic's interactive AR controller must implement this interface
/// so ARModeSelectionController can activate sandbox mode on any of them.
///
/// TOPICS TO IMPLEMENT:
///   InteractiveArrayCars         ← already exists, add interface below
///   InteractiveStackScene        ← future
///   InteractiveQueueScene        ← future
///   InteractiveLinkedListScene   ← future
///   InteractiveTreeScene         ← future
///   InteractiveGraphScene        ← future
/// </summary>
public interface IARSandboxController
{
    /// <summary>
    /// Activate full sandbox mode:
    ///   - Show the scenario / difficulty selection as normal.
    ///   - After scene placement ALL action buttons are immediately
    ///     unlocked — no "insert first" restriction.
    ///   - No lesson guide overlay runs.
    /// </summary>
    void StartSandboxMode();
}