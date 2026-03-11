using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PrefabGenerator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Generate Linked List Prefabs")]
    public static void GenerateAllPrefabs()
    {
        CreateNodeLabelPrefab();
        CreatePointerArrowPrefab();
        
        Debug.Log("✅ Successfully generated NodeLabelPrefab and PointerArrowPrefab!");
        AssetDatabase.Refresh();
    }
    
    // ═══════════════════════════════════════════════════════════
    // NODE LABEL PREFAB GENERATOR
    // ═══════════════════════════════════════════════════════════
    
    static void CreateNodeLabelPrefab()
    {
        // Create root GameObject
        GameObject nodeLabel = new GameObject("NodeLabel");
        
        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(nodeLabel.transform);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.scaleFactor = 1f;
        scaler.dynamicPixelsPerUnit = 10f;
        
        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 80);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        canvasRect.localPosition = Vector3.zero;
        
        // Create Background Image
        GameObject backgroundObj = new GameObject("LabelBackground");
        backgroundObj.transform.SetParent(canvasObj.transform);
        
        RectTransform bgRect = backgroundObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        Image bgImage = backgroundObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f); // Semi-transparent black
        bgImage.raycastTarget = true;
        
        // Add outline to background
        Outline outline = backgroundObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.84f, 0f, 1f); // Gold
        outline.effectDistance = new Vector2(2, -2);
        outline.useGraphicAlpha = true;
        
        // Create Text
        GameObject textObj = new GameObject("LabelText");
        textObj.transform.SetParent(backgroundObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-10, -10); // 5px padding on each side
        textRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "HEAD";
        text.fontSize = 36;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, 0.84f, 0f, 1f); // Gold
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18;
        text.fontSizeMax = 48;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        
        // Add text outline for better visibility
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        
        // Ensure the prefabs folder exists
        string folderPath = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        // Save as prefab
        string prefabPath = folderPath + "/NodeLabelPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(nodeLabel, prefabPath);
        
        // Clean up scene
        DestroyImmediate(nodeLabel);
        
        Debug.Log($"✅ NodeLabelPrefab created at: {prefabPath}");
    }
    
    // ═══════════════════════════════════════════════════════════
    // POINTER ARROW PREFAB GENERATOR
    // ═══════════════════════════════════════════════════════════
    
    static void CreatePointerArrowPrefab()
    {
        // Create root GameObject
        GameObject pointerArrow = new GameObject("PointerArrow");
        
        // Add LineRenderer
        LineRenderer lr = pointerArrow.AddComponent<LineRenderer>();
        
        // Create material
        Material arrowMaterial = new Material(Shader.Find("Sprites/Default"));
        arrowMaterial.color = Color.yellow;
        arrowMaterial.name = "ArrowMaterial";
        
        // Save material to assets
        string materialFolder = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(materialFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        string materialPath = materialFolder + "/ArrowMaterial.mat";
        AssetDatabase.CreateAsset(arrowMaterial, materialPath);
        
        // Configure LineRenderer
        lr.material = arrowMaterial;
        lr.startColor = Color.yellow;
        lr.endColor = Color.yellow;
        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;
        lr.positionCount = 5;
        
        // Set default arrow shape (horizontal, 15cm long)
        lr.SetPosition(0, new Vector3(0, 0, 0));           // Start
        lr.SetPosition(1, new Vector3(0.15f, 0, 0));       // Base of arrow
        lr.SetPosition(2, new Vector3(0.13f, 0.015f, 0));  // Top wing
        lr.SetPosition(3, new Vector3(0.15f, 0, 0));       // Tip
        lr.SetPosition(4, new Vector3(0.13f, -0.015f, 0)); // Bottom wing
        
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
        lr.alignment = LineAlignment.View; // Face camera
        lr.textureMode = LineTextureMode.Stretch;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.useWorldSpace = true;
        lr.loop = false;
        
        // Ensure the prefabs folder exists
        string folderPath = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        // Save as prefab
        string prefabPath = folderPath + "/PointerArrowPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(pointerArrow, prefabPath);
        
        // Clean up scene
        DestroyImmediate(pointerArrow);
        
        Debug.Log($"✅ PointerArrowPrefab created at: {prefabPath}");
        Debug.Log($"✅ ArrowMaterial created at: {materialPath}");
    }
    
    // ═══════════════════════════════════════════════════════════
    // INDIVIDUAL GENERATORS (Optional - for menu flexibility)
    // ═══════════════════════════════════════════════════════════
    
    [MenuItem("Tools/Generate Node Label Prefab Only")]
    public static void GenerateNodeLabelOnly()
    {
        CreateNodeLabelPrefab();
        Debug.Log("✅ NodeLabelPrefab generated!");
        AssetDatabase.Refresh();
    }
    
    [MenuItem("Tools/Generate Pointer Arrow Prefab Only")]
    public static void GeneratePointerArrowOnly()
    {
        CreatePointerArrowPrefab();
        Debug.Log("✅ PointerArrowPrefab generated!");
        AssetDatabase.Refresh();
    }
    
    // ═══════════════════════════════════════════════════════════
    // UTILITY: Generate Index Label for Arrays (Bonus)
    // ═══════════════════════════════════════════════════════════
    
    [MenuItem("Tools/Generate Array Index Label Prefab")]
    public static void GenerateArrayIndexLabel()
    {
        // Create root GameObject
        GameObject indexLabel = new GameObject("IndexLabel");
        
        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(indexLabel.transform);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.scaleFactor = 1f;
        scaler.dynamicPixelsPerUnit = 10f;
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(150, 60);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        canvasRect.localPosition = Vector3.zero;
        
        // Create Background
        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(canvasObj.transform);
        
        RectTransform bgRect = backgroundObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        Image bgImage = backgroundObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0.5f, 1f, 0.8f); // Cyan
        bgImage.raycastTarget = true;
        
        // Create Text
        GameObject textObj = new GameObject("IndexText");
        textObj.transform.SetParent(backgroundObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-10, -10);
        textRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "[0]";
        text.fontSize = 32;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16;
        text.fontSizeMax = 40;
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        
        // Save as prefab
        string folderPath = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        string prefabPath = folderPath + "/IndexLabelPrefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(indexLabel, prefabPath);
        
        DestroyImmediate(indexLabel);
        
        Debug.Log($"✅ IndexLabelPrefab created at: {prefabPath}");
        AssetDatabase.Refresh();
    }
#endif
}

// ═══════════════════════════════════════════════════════════
// OPTIONAL: Runtime Prefab Generator (if you need it at runtime)
// ═══════════════════════════════════════════════════════════

public class RuntimePrefabHelper : MonoBehaviour
{
    public static GameObject CreateNodeLabelRuntime()
    {
        GameObject nodeLabel = new GameObject("NodeLabel");
        
        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(nodeLabel.transform);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 80);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        
        GameObject backgroundObj = new GameObject("LabelBackground");
        backgroundObj.transform.SetParent(canvasObj.transform);
        
        RectTransform bgRect = backgroundObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        Image bgImage = backgroundObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);
        
        Outline outline = backgroundObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.84f, 0f, 1f);
        outline.effectDistance = new Vector2(2, -2);
        
        GameObject textObj = new GameObject("LabelText");
        textObj.transform.SetParent(backgroundObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-10, -10);
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "HEAD";
        text.fontSize = 36;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, 0.84f, 0f, 1f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18;
        text.fontSizeMax = 48;
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        
        return nodeLabel;
    }
    
    public static GameObject CreatePointerArrowRuntime()
    {
        GameObject arrow = new GameObject("PointerArrow");
        LineRenderer lr = arrow.AddComponent<LineRenderer>();
        
        Material arrowMaterial = new Material(Shader.Find("Sprites/Default"));
        arrowMaterial.color = Color.yellow;
        
        lr.material = arrowMaterial;
        lr.startColor = Color.yellow;
        lr.endColor = Color.yellow;
        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;
        lr.positionCount = 5;
        
        lr.SetPosition(0, new Vector3(0, 0, 0));
        lr.SetPosition(1, new Vector3(0.15f, 0, 0));
        lr.SetPosition(2, new Vector3(0.13f, 0.015f, 0));
        lr.SetPosition(3, new Vector3(0.15f, 0, 0));
        lr.SetPosition(4, new Vector3(0.13f, -0.015f, 0));
        
        lr.alignment = LineAlignment.View;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.useWorldSpace = true;
        
        return arrow;
    }
}