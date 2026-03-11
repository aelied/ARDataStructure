using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[AddComponentMenu("UI/Gradient Shadow", 15)]
public class GradientShadow : Shadow
{
    public Color topColor = Color.black;
    public Color bottomColor = Color.clear;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        List<UIVertex> verts = new List<UIVertex>();
        vh.GetUIVertexStream(verts);

        int start = 0;
        int end = verts.Count;
        ApplyShadowWithGradient(verts, effectColor, start, end);

        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
    }

    void ApplyShadowWithGradient(List<UIVertex> verts, Color baseColor, int start, int end)
    {
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = start; i < end; i++)
        {
            float y = verts[i].position.y;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        for (int i = start; i < end; i++)
        {
            var v = verts[i];
            float t = Mathf.InverseLerp(minY, maxY, v.position.y);
            v.color = Color.Lerp(bottomColor, topColor, t);
            verts[i] = v;
        }
    }
}
