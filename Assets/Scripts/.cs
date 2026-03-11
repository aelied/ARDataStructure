using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Automatically creates Physical Array AR Scene
/// Usage: Tools > AR Data Structures > Create Array Scene
/// </summary>
public class PhysicalArraySceneCreator
{
    private static GameObject physicalArrayManager;
    private static GameObject uiController;
    private static GameObject canvasObj;
    
    [MenuItem("Tools/AR Data Structures/Create Array Scene")]
    public static void CreateArrayScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // Remove default camera
        var defaultCamera = GameObject.Find("Main Camera");
        if (defaultCamera != null)
            Object.DestroyImmediate(defaultCamera);
        
        Debug.Log("🚀 Creating Physical Array Scene...");
        
        var xrOrigin = CreateXROrigin();
        CreateARSession();
        CreateCanvas();
        CreateManagers();
        CreatePrefabFolders();
        CreatePrefabs();
        WireReferences(xrOrigin);
        
        Debug.Log("✅ Physical Array Scene Created Successfully!");
        Debug.Log("📍 Scene saved to: Assets/Scenes/PhysicalArrayScene.unity");
        
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/PhysicalArrayScene.unity");
    }
    
    static GameObject CreateXROrigin()
    {
        GameObject xrOrigin = new GameObject("XR Origin");
        
        var arSessionOrigin = xrOrigin.AddComponent<ARSessionOrigin>();
        xrOrigin.AddComponent<ARPlaneManager>();
        xrOrigin.AddComponent<ARRaycastManager>();
        
        // Create Camera Offset
        GameObject cameraOffset = new GameObject("Camera Offset");
        cameraOffset.transform.SetParent(xrOrigin.transform);
        
        // Create AR Camera
        GameObject arCamera = new GameObject("AR Camera");
        arCamera.transform.SetParent(cameraOffset.transform);
        arCamera.transform.localPosition = Vector3.zero;
        
        var camera = arCamera.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 20f;
        
        arCamera.AddComponent<ARCameraManager>();
        arCamera.AddComponent<ARCameraBackground>();
        arCamera.tag = "MainCamera";
        
        arSessionOrigin.camera = camera;
        
        Debug.Log("✅ XR Origin with AR Camera created");
        return xrOrigin;
    }
    
    static void CreateARSession()
    {
        GameObject arSession = new GameObject("AR Session");
        arSession.AddComponent<ARSession>();
        arSession.AddComponent<ARInputManager>();
        
        Debug.Log("✅ AR Session created");
    }
    
    static void CreateCanvas()
    {
        canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        
        // Create EventSystem
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        
        // Header Panel
        GameObject headerPanel = CreatePanel(canvasObj.transform, "HeaderPanel",
            new Vector2(0, -100), new Vector2(1000, 150),
            new Color(0.2f, 0.2f, 0.3f, 0.9f));
        
        CreateTextUI(headerPanel.transform, "TitleText",
            Vector2.zero, new Vector2(900, 100),
            "AR ARRAY VISUALIZER", 48, TextAlignmentOptions.Center);
        
        // Instruction Card
        GameObject instructionCard = CreatePanel(canvasObj.transform, "InstructionCard",
            new Vector2(0, -300), new Vector2(900, 200),
            new Color(0.1f, 0.1f, 0.15f, 0.85f));
        
        CreateTextUI(instructionCard.transform, "InstructionText",
            Vector2.zero, new Vector2(850, 180),
            "🔍 Move phone to scan surfaces", 26, TextAlignmentOptions.Center);
        
        // Detection Feedback
        GameObject detectionFeedback = CreatePanel(canvasObj.transform, "DetectionFeedback",
            new Vector2(0, -550), new Vector2(800, 120),
            new Color(0.15f, 0.15f, 0.2f, 0.8f));
        
        CreateTextUI(detectionFeedback.transform, "DetectionText",
            Vector2.zero, new Vector2(750, 100),
            "🔍 Scan environment slowly", 24, TextAlignmentOptions.Center);
        
        // Status Panel (Top Right)
        GameObject statusPanel = CreatePanel(canvasObj.transform, "StatusPanel",
            new Vector2(400, -100), new Vector2(300, 80),
            new Color(0.2f, 0.2f, 0.3f, 0.9f));
        statusPanel.GetComponent<RectTransform>().anchorMin = new Vector2(1f, 1f);
        statusPanel.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
        statusPanel.GetComponent<RectTransform>().pivot = new Vector2(1f, 1f);
        statusPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, -20);
        
        CreateTextUI(statusPanel.transform, "StatusText",
            Vector2.zero, new Vector2(280, 60),
            "Elements: 0/8", 22, TextAlignmentOptions.Center);
        
        // Button Panel (Initially Hidden)
        GameObject buttonPanel = CreatePanel(canvasObj.transform, "ButtonPanel",
            new Vector2(0, 200), new Vector2(1000, 150),
            new Color(0.1f, 0.1f, 0.15f, 0.9f));
        buttonPanel.SetActive(false);
        
        CreateButton(buttonPanel.transform, "InsertButton",
            new Vector2(-360, 0), new Vector2(220, 100),
            "➕ INSERT", new Color(0.2f, 0.4f, 1f));
        
        CreateButton(buttonPanel.transform, "AccessButton",
            new Vector2(-120, 0), new Vector2(220, 100),
            "👁️ ACCESS", new Color(1f, 0.8f, 0.2f));
        
        CreateButton(buttonPanel.transform, "DeleteButton",
            new Vector2(120, 0), new Vector2(220, 100),
            "🗑️ DELETE", new Color(1f, 0.3f, 0.3f));
        
        CreateButton(buttonPanel.transform, "ResetButton",
            new Vector2(360, 0), new Vector2(220, 100),
            "🔄 RESET", new Color(0.5f, 0.5f, 0.5f));
        
        // Explanation Panel
        GameObject explanationPanel = CreatePanel(canvasObj.transform, "ExplanationPanel",
            new Vector2(0, 380), new Vector2(950, 120),
            new Color(0.15f, 0.15f, 0.2f, 0.85f));
        explanationPanel.SetActive(false);
        
        CreateTextUI(explanationPanel.transform, "ExplanationText",
            Vector2.zero, new Vector2(900, 100),
            "", 24, TextAlignmentOptions.Center);
        
        // Help Panel (Bottom)
        GameObject helpPanel = CreatePanel(canvasObj.transform, "HelpPanel",
            new Vector2(0, 150), new Vector2(900, 250),
            new Color(0.1f, 0.1f, 0.15f, 0.8f));
        helpPanel.SetActive(false);
        helpPanel.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
        helpPanel.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
        helpPanel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
        helpPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);
        
        CreateTextUI(helpPanel.transform, "HelpText",
            Vector2.zero, new Vector2(850, 230),
            "📚 ARRAY RULES:\n\n• Insert: Add at next index\n• Access: Read element at index\n• Delete: Remove at specific index\n• Random access: O(1)\n\nPlace physical coins and tap them!",
            20, TextAlignmentOptions.Center);
        
        // Index Input Panel (Modal - Initially Hidden)
        GameObject indexInputPanel = CreatePanel(canvasObj.transform, "IndexInputPanel",
            Vector2.zero, new Vector2(600, 400),
            new Color(0.1f, 0.1f, 0.15f, 0.95f));
        indexInputPanel.SetActive(false);
        indexInputPanel.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        indexInputPanel.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        indexInputPanel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        
        CreateTextUI(indexInputPanel.transform, "PromptText",
            new Vector2(0, 120), new Vector2(550, 80),
            "Enter Index:", 28, TextAlignmentOptions.Center);
        
        CreateInputField(indexInputPanel.transform, "IndexInput",
            new Vector2(0, 20), new Vector2(400, 80));
        
        CreateButton(indexInputPanel.transform, "ConfirmButton",
            new Vector2(0, -100), new Vector2(350, 90),
            "✓ CONFIRM", new Color(0.2f, 0.7f, 0.3f));
        
        Debug.Log("✅ Canvas UI created with all panels");
    }
    
    static GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent);
        
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        
        Image img = panel.AddComponent<Image>();
        img.color = color;
        
        return panel;
    }
    
    static GameObject CreateTextUI(Transform parent, string name, Vector2 position, Vector2 size,
        string text, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent);
        
        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        tmp.fontStyle = FontStyles.Bold;
        
        return textObj;
    }
    
    static GameObject CreateButton(Transform parent, string name, Vector2 position, Vector2 size,
        string text, Color color)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent);
        
        RectTransform rt = buttonObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        
        Image img = buttonObj.AddComponent<Image>();
        img.color = color;
        Button btn = buttonObj.AddComponent<Button>();
        
        ColorBlock colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        btn.colors = colors;
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 26;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        
        return buttonObj;
    }
    
    static GameObject CreateInputField(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject inputFieldObj = new GameObject(name);
        inputFieldObj.transform.SetParent(parent);
        
        RectTransform rt = inputFieldObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        
        Image img = inputFieldObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        
        TMP_InputField inputField = inputFieldObj.AddComponent<TMP_InputField>();
        
        // Text Area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputFieldObj.transform);
        RectTransform textAreaRt = textArea.AddComponent<RectTransform>();
        textAreaRt.anchorMin = Vector2.zero;
        textAreaRt.anchorMax = Vector2.one;
        textAreaRt.offsetMin = new Vector2(10, 0);
        textAreaRt.offsetMax = new Vector2(-10, 0);
        
        inputField.textViewport = textAreaRt;
        
        // Text Component
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform);
        
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = 32;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;
        
        inputField.textComponent = textComponent;
        
        // Placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textArea.transform);
        
        RectTransform placeholderRt = placeholderObj.AddComponent<RectTransform>();
        placeholderRt.anchorMin = Vector2.zero;
        placeholderRt.anchorMax = Vector2.one;
        placeholderRt.offsetMin = Vector2.zero;
        placeholderRt.offsetMax = Vector2.zero;
        
        TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderText.text = "0";
        placeholderText.fontSize = 30;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        placeholderText.alignment = TextAlignmentOptions.Center;
        placeholderText.fontStyle = FontStyles.Italic;
        
        inputField.placeholder = placeholderText;
        
        return inputFieldObj;
    }
    
    static void CreateManagers()
    {
        // PhysicalArrayManager
        physicalArrayManager = new GameObject("PhysicalArrayManager");
        physicalArrayManager.AddComponent<PhysicalArrayManager>();
        
        // UIController
        uiController = new GameObject("UIController");
        uiController.AddComponent<PhysicalArrayUI>();
        
        Debug.Log("✅ Manager scripts added");
    }
    
    static void WireReferences(GameObject xrOrigin)
    {
        var manager = physicalArrayManager.GetComponent<PhysicalArrayManager>();
        var ui = uiController.GetComponent<PhysicalArrayUI>();
        
        // Manager References
        manager.planeManager = xrOrigin.GetComponent<ARPlaneManager>();
        manager.raycastManager = xrOrigin.GetComponent<ARRaycastManager>();
        manager.instructionText = GameObject.Find("InstructionText").GetComponent<TextMeshProUGUI>();
        manager.statusText = GameObject.Find("StatusText").GetComponent<TextMeshProUGUI>();
        manager.detectionFeedbackText = GameObject.Find("DetectionText").GetComponent<TextMeshProUGUI>();
        
        // UI References
        ui.arrayManager = manager;
        ui.insertButton = GameObject.Find("InsertButton").GetComponent<Button>();
        ui.accessButton = GameObject.Find("AccessButton").GetComponent<Button>();
        ui.deleteButton = GameObject.Find("DeleteButton").GetComponent<Button>();
        ui.resetButton = GameObject.Find("ResetButton").GetComponent<Button>();
        
        ui.headerPanel = GameObject.Find("HeaderPanel");
        ui.instructionCard = GameObject.Find("InstructionCard");
        ui.buttonPanel = GameObject.Find("ButtonPanel");
        ui.explanationPanel = GameObject.Find("ExplanationPanel");
        ui.helpPanel = GameObject.Find("HelpPanel");
        ui.indexInputPanel = GameObject.Find("IndexInputPanel");
        
        ui.explanationText = GameObject.Find("ExplanationText").GetComponent<TextMeshProUGUI>();
        ui.helpText = GameObject.Find("HelpText").GetComponent<TextMeshProUGUI>();
        ui.indexInput = GameObject.Find("IndexInput").GetComponent<TMP_InputField>();
        ui.confirmIndexButton = GameObject.Find("ConfirmButton").GetComponent<Button>();
        
        Debug.Log("✅ All references wired!");
    }
    
    static void CreatePrefabFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Array"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Array");
        
        Debug.Log("✅ Prefab folders created");
    }
    
    static void CreatePrefabs()
    {
        CreateIndexLabelPrefab();
        CreateArrayBracketPrefab();
        
        // Assign prefabs to manager
        var manager = physicalArrayManager.GetComponent<PhysicalArrayManager>();
        manager.indexLabelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Array/IndexLabel.prefab");
        manager.arrayBracketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Array/ArrayBracket.prefab");
        
        Debug.Log("✅ Prefabs created and assigned");
    }
    
    static void CreateIndexLabelPrefab()
    {
        GameObject label = new GameObject("IndexLabel");
        
        Canvas canvas = label.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform rt = label.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0.3f, 0.2f);
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(label.transform);
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "[0]";
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = false;
        
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        
        // Add outline
        var outline = tmp.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, 2);
        
        string path = "Assets/Prefabs/Array/IndexLabel.prefab";
        PrefabUtility.SaveAsPrefabAsset(label, path);
        Object.DestroyImmediate(label);
        
        Debug.Log("   ✅ IndexLabel.prefab created");
    }
    
    static void CreateArrayBracketPrefab()
    {
        GameObject bracket = new GameObject("ArrayBracket");
        
        TextMeshPro tmp = bracket.AddComponent<TextMeshPro>();
        tmp.text = "[";
        tmp.fontSize = 0.4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.yellow;
        tmp.fontStyle = FontStyles.Bold;
        
        RectTransform rt = bracket.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0.2f, 0.3f);
        
        string path = "Assets/Prefabs/Array/ArrayBracket.prefab";
        PrefabUtility.SaveAsPrefabAsset(bracket, path);
        Object.DestroyImmediate(bracket);
        
        Debug.Log("   ✅ ArrayBracket.prefab created");
    }
}
#endif