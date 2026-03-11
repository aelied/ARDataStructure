using UnityEngine;

/// <summary>
/// ARSessionCleaner.cs
/// ====================
/// Attach to ANY GameObject in your main scene (e.g. your MainMenuManager).
/// No inspector wiring needed.
///
/// WHY: PlayerPrefs persists between app sessions. If the user killed the app
/// while AR_JustReturned=1 was still set, the next cold launch finds that
/// stale key and ARReturnHandler reopens the lesson panel immediately.
///
/// This clears all AR return flags on cold launch BEFORE ARReturnHandler.Start() runs.
/// </summary>
public class ARSessionCleaner : MonoBehaviour
{
    void Awake()
    {
        // Time.realtimeSinceStartup resets to near-zero on every cold app launch.
        // If it's under 5 seconds, this is a fresh launch — clear stale flags.
        bool isColdLaunch = Time.realtimeSinceStartup < 5f;

        if (isColdLaunch)
        {
            PlayerPrefs.SetInt(ARReturnHandler.AR_JUST_RETURNED_KEY, 0);
            PlayerPrefs.DeleteKey("AR_SessionID");
            PlayerPrefs.Save();
            Debug.Log("[ARSessionCleaner] Cold launch — cleared stale AR return flags.");
        }
    }
}