using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class HazyOuterShadow : MonoBehaviour
{
    public Color shadowColor = new Color(0.75f, 0.45f, 1.0f, 0.4f);
    [Range(0f, 120f)] public float size = 40f;
    public Vector2 offset = new Vector2(0f, -6f);

    private Image _shadow;

    void OnEnable() => Rebuild();
    void OnDisable() => Clear();
    void OnDestroy() => Clear();

#if UNITY_EDITOR
    void OnValidate() =>
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Rebuild(); };
#endif

    void LateUpdate()
    {
        if (_shadow == null) Rebuild();
        Sync();
    }

    void Rebuild()
    {
        Clear();

        Image src = GetComponent<Image>();
        GameObject go = new GameObject("Shadow");
        go.transform.SetParent(transform.parent, false);
        go.transform.SetSiblingIndex(transform.GetSiblingIndex());

        _shadow = go.AddComponent<Image>();
        _shadow.sprite = src.sprite;
        _shadow.type = src.type;
        _shadow.raycastTarget = false;

        Sync();
    }

    void Sync()
    {
        if (_shadow == null) return;

        Image src = GetComponent<Image>();
        RectTransform srcRT = src.rectTransform;
        RectTransform sRT = _shadow.rectTransform;

        sRT.anchorMin = srcRT.anchorMin;
        sRT.anchorMax = srcRT.anchorMax;
        sRT.pivot = srcRT.pivot;
        sRT.anchoredPosition = srcRT.anchoredPosition + offset;
        sRT.sizeDelta = srcRT.sizeDelta + Vector2.one * size * 2f;
        sRT.rotation = srcRT.rotation;

        _shadow.color = shadowColor;
        _shadow.transform.SetSiblingIndex(transform.GetSiblingIndex());
    }

    void Clear()
    {
        if (_shadow == null) return;
#if UNITY_EDITOR
        DestroyImmediate(_shadow.gameObject);
#else
        Destroy(_shadow.gameObject);
#endif
        _shadow = null;
    }
}