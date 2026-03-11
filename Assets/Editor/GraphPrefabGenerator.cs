using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor utility to generate all required prefabs for AR Graph scene
/// Usage: Tools > AR Graph > Generate All Prefabs
/// IMPORTANT: This script MUST be placed in an "Editor" folder!
/// Create: Assets/Editor/GraphPrefabGenerator.cs
/// </summary>
public static class GraphPrefabGenerator
{
    [MenuItem("Tools/AR Graph/Generate All Prefabs")]
    public static void GenerateAllPrefabs()
    {
        CreateNodeLabelPrefab();
        CreateEdgeLineMaterial();
        CreateDirectedEdgeMaterial();
        
        Debug.Log("✅ All Graph prefabs and materials generated successfully!");
        AssetDatabase.Refresh();
    }
    
    [MenuItem("Tools/AR Graph/1. Generate Node Label Prefab")]
    public static void CreateNodeLabelPrefab()
    {
        // Create the root GameObject
        GameObject nodeLabel = new GameObject("NodeLabel");
        
        // Add TextMeshPro 3D component
        TextMeshPro textMesh = nodeLabel.AddComponent<TextMeshPro>();
        
        // Configure text properties
        textMesh.text = "N0";
        textMesh.fontSize = 0.5f;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = Color.white;
        
        // Make text face camera
        textMesh.transform.rotation = Quaternion.Euler(0, 180, 0);
        
        // Set text bounds
        RectTransform rectTransform = textMesh.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(1f, 1f);
        
        // Add a background quad for better visibility
        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "Background";
        background.transform.SetParent(nodeLabel.transform);
        background.transform.localPosition = new Vector3(0, 0, 0.01f);
        background.transform.localRotation = Quaternion.identity;
        background.transform.localScale = new Vector3(0.3f, 0.15f, 1f);
        
        // Create material for background
        Material bgMaterial = new Material(Shader.Find("Unlit/Color"));
        bgMaterial.color = new Color(0, 0, 0, 0.7f); // Semi-transparent black
        background.GetComponent<Renderer>().material = bgMaterial;
        
        // Remove collider from background
        Object.DestroyImmediate(background.GetComponent<Collider>());
        
        // Create prefabs folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Graph"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Graph");
        
        // Save as prefab
        string prefabPath = "Assets/Prefabs/Graph/NodeLabel.prefab";
        PrefabUtility.SaveAsPrefabAsset(nodeLabel, prefabPath);
        
        // Cleanup scene
        Object.DestroyImmediate(nodeLabel);
        
        Debug.Log($"✅ Node Label Prefab created at: {prefabPath}");
    }
    
    [MenuItem("Tools/AR Graph/2. Generate Edge Line Material")]
    public static void CreateEdgeLineMaterial()
    {
        // Create materials folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Graph"))
            AssetDatabase.CreateFolder("Assets/Materials", "Graph");
        
        // Create edge line material
        Material edgeMaterial = new Material(Shader.Find("Unlit/Color"));
        edgeMaterial.name = "EdgeLineMaterial";
        
        // Set cyan color matching your design
        edgeMaterial.color = new Color(0f, 1f, 1f, 1f); // Cyan (0, 255, 255)
        
        // Make it render on top
        edgeMaterial.renderQueue = 3000;
        
        // Save material
        string materialPath = "Assets/Materials/Graph/EdgeLineMaterial.mat";
        AssetDatabase.CreateAsset(edgeMaterial, materialPath);
        
        Debug.Log($"✅ Edge Line Material created at: {materialPath}");
    }
    
    [MenuItem("Tools/AR Graph/3. Generate Directed Edge Material")]
    public static void CreateDirectedEdgeMaterial()
    {
        // Create materials folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Graph"))
            AssetDatabase.CreateFolder("Assets/Materials", "Graph");
        
        // Create directed edge material with arrow
        Material directedMaterial = new Material(Shader.Find("Unlit/Color"));
        directedMaterial.name = "DirectedEdgeMaterial";
        
        // Set cyan color with slight transparency
        directedMaterial.color = new Color(0f, 1f, 1f, 0.9f); // Cyan
        
        // Make it render on top
        directedMaterial.renderQueue = 3000;
        
        // Save material
        string materialPath = "Assets/Materials/Graph/DirectedEdgeMaterial.mat";
        AssetDatabase.CreateAsset(directedMaterial, materialPath);
        
        Debug.Log($"✅ Directed Edge Material created at: {materialPath}");
        
        // Also create arrow head prefab for directed edges
        CreateArrowHeadPrefab();
    }
    
    private static void CreateArrowHeadPrefab()
    {
        // Create arrow head using a cylinder (rotated to point forward)
        GameObject arrowHead = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arrowHead.name = "ArrowHead";
        
        // Scale and rotate to look like an arrow pointing in the Z direction
        arrowHead.transform.localScale = new Vector3(0.03f, 0.08f, 0.03f);
        arrowHead.transform.rotation = Quaternion.Euler(0, 0, 90); // Point along Z-axis
        
        // Apply cyan material
        Material arrowMaterial = new Material(Shader.Find("Unlit/Color"));
        arrowMaterial.color = new Color(0f, 1f, 1f, 1f); // Cyan
        arrowHead.GetComponent<Renderer>().material = arrowMaterial;
        
        // Remove collider
        Object.DestroyImmediate(arrowHead.GetComponent<Collider>());
        
        // Save as prefab
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Graph"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Graph");
        
        string prefabPath = "Assets/Prefabs/Graph/ArrowHead.prefab";
        PrefabUtility.SaveAsPrefabAsset(arrowHead, prefabPath);
        
        // Cleanup
        Object.DestroyImmediate(arrowHead);
        
        Debug.Log($"✅ Arrow Head Prefab created at: {prefabPath}");
    }
}