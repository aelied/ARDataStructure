using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class SoftUIGlow : MonoBehaviour
{
    [Header("Glow Color")]
    public Color glowColor = new Color(0.75f, 0.45f, 1.0f, 0.4f);

    [Header("Glow Shape")]
    [Range(0f, 120f)]
    public float glowSize = 40f;
    public Vector2 glowOffset = new Vector2(0f, -6f);

    [Header("Layering")]
    [Range(1, 6)]
    public int layers = 4;
    [Range(0.3f, 1.0f)]
    public float falloff = 0.72f;

    private const string TAG = "__SoftGlow";

    void OnEnable() => Rebuild();
    void OnDisable() => Clear();
    void OnDestroy() => Clear();

#if UNITY_EDITOR
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Rebuild(); };
    }
#endif

    void LateUpdate() => UpdateLayers();

    void Rebuild()
    {
        Clear();

        Image src = GetComponent<Image>();
        if (src == null) return;

        for (int i = 0; i < layers; i++)
        {
            GameObject go = new GameObject(TAG + "_" + i);
            // Normal parenting — NO HideAndDontSave
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();

            Image img = go.AddComponent<Image>();
            img.sprite = src.sprite;
            img.type = src.type;
            img.raycastTarget = false;
        }

        UpdateLayers();
    }

    void UpdateLayers()
    {
        Image src = GetComponent<Image>();
        if (src == null) return;

        int layerIndex = 0;
        foreach (Transform child in transform)
        {
            if (!child.name.StartsWith(TAG)) continue;
            if (layerIndex >= layers) break;

            float t = (float)(layerIndex + 1) / layers;
            float expansion = glowSize * t;

            RectTransform rt = child.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = glowOffset * t;
            rt.sizeDelta = src.rectTransform.sizeDelta + Vector2.one * expansion * 2f;

            float alpha = glowColor.a * Mathf.Pow(falloff, layerIndex) * (1f - t * 0.35f);
            child.GetComponent<Image>().color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);

            child.SetSiblingIndex(layerIndex);
            layerIndex++;
        }
    }

    void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith(TAG)) continue;
#if UNITY_EDITOR
            DestroyImmediate(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
    }

    // Call this from the Inspector context menu to nuke ALL ghost layers
    // anywhere in the scene — including leftovers from the old version
    [ContextMenu("Force Clean All Ghost Layers")]
    void ForceCleanAll()
    {
        var all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        foreach (var t in all)
        {
            if (t != null && t.name.StartsWith(TAG))
            {
#if UNITY_EDITOR
                DestroyImmediate(t.gameObject);
#else
                Destroy(t.gameObject);
#endif
                count++;
            }
        }
        Debug.Log($"[SoftUIGlow] Removed {count} ghost layer(s).");
        Rebuild();
    }
}