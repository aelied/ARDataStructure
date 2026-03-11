using UnityEngine;
using TMPro;

/// <summary>
/// Applies a horizontal (left-to-right) color gradient to a TextMeshPro text component.
/// Attach this script to any GameObject that has a TMP_Text component.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class HorizontalTextGradient : MonoBehaviour
{
    [Header("Gradient Colors")]
    [Tooltip("Color on the left side of the text")]
    public Color leftColor = Color.red;

    [Tooltip("Color on the right side of the text")]
    public Color rightColor = Color.blue;

    [Header("Settings")]
    [Tooltip("If true, gradient is based on each character's local position. If false, it spans the full text bounds.")]
    public bool perCharacter = false;

    [Tooltip("Update the gradient every frame (useful for animated colors)")]
    public bool updateEveryFrame = false;

    private TMP_Text _tmpText;
    private bool _isDirty = true;

    private void Awake()
    {
        _tmpText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        _isDirty = true;
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    private void OnTextChanged(Object obj)
    {
        if (obj == _tmpText)
            _isDirty = true;
    }

    private void LateUpdate()
    {
        if (_isDirty || updateEveryFrame)
        {
            ApplyGradient();
            _isDirty = false;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _isDirty = true;
    }
#endif

    /// <summary>
    /// Force a refresh of the gradient. Call this if you change leftColor or rightColor from code.
    /// </summary>
    public void Refresh()
    {
        _isDirty = true;
    }

    private void ApplyGradient()
    {
        if (_tmpText == null) return;

        _tmpText.ForceMeshUpdate();

        TMP_TextInfo textInfo = _tmpText.textInfo;
        if (textInfo.characterCount == 0) return;

        // Get the full text bounds for global gradient mode
        Bounds textBounds = _tmpText.textBounds;
        float textMinX = textBounds.min.x;
        float textMaxX = textBounds.max.x;
        float textWidth = textMaxX - textMinX;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible) continue;

            int meshIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Color32[] vertexColors = textInfo.meshInfo[meshIndex].colors32;

            // The 4 vertices of a character quad:
            // 0 = bottom-left, 1 = top-left, 2 = top-right, 3 = bottom-right
            Vector3[] vertices = textInfo.meshInfo[meshIndex].vertices;

            if (perCharacter)
            {
                // Gradient spans each character individually
                vertexColors[vertexIndex + 0] = Color.Lerp(leftColor, rightColor, 0f); // bottom-left
                vertexColors[vertexIndex + 1] = Color.Lerp(leftColor, rightColor, 0f); // top-left
                vertexColors[vertexIndex + 2] = Color.Lerp(leftColor, rightColor, 1f); // top-right
                vertexColors[vertexIndex + 3] = Color.Lerp(leftColor, rightColor, 1f); // bottom-right
            }
            else
            {
                // Gradient spans the full text width
                if (textWidth <= 0f)
                {
                    // Fallback if text width is zero
                    for (int v = 0; v < 4; v++)
                        vertexColors[vertexIndex + v] = leftColor;
                }
                else
                {
                    for (int v = 0; v < 4; v++)
                    {
                        float t = Mathf.InverseLerp(textMinX, textMaxX, vertices[vertexIndex + v].x);
                        vertexColors[vertexIndex + v] = Color.Lerp(leftColor, rightColor, t);
                    }
                }
            }
        }

        // Push the updated vertex colors back to the mesh
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
            _tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}