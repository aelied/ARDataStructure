using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HorizontalGradient : BaseMeshEffect
{
    public Color leftColor = Color.white;
    public Color rightColor = Color.black;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
            return;

        var vertexList = new System.Collections.Generic.List<UIVertex>();
        vh.GetUIVertexStream(vertexList);

        float minX = float.MaxValue;
        float maxX = float.MinValue;

        for (int i = 0; i < vertexList.Count; i++)
        {
            float x = vertexList[i].position.x;
            if (x > maxX) maxX = x;
            if (x < minX) minX = x;
        }

        float width = maxX - minX;

        for (int i = 0; i < vertexList.Count; i++)
        {
            UIVertex vertex = vertexList[i];
            float t = (vertex.position.x - minX) / width;
            vertex.color = Color.Lerp(leftColor, rightColor, t);
            vertexList[i] = vertex;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(vertexList);
    }
}