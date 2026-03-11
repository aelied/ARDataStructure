using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public class GlowCleaner : MonoBehaviour
{
    [ContextMenu("Clean Glow Ghosts")]
    void Clean()
    {
        var all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        foreach (var t in all)
        {
            if (t != null && t.name.StartsWith("__SoftGlow"))
            {
                DestroyImmediate(t.gameObject);
                count++;
            }
        }
        Debug.Log($"Cleaned {count} ghost glow objects.");
    }
}
#endif