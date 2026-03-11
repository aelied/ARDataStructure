using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// AUTOMATIC PREFAB CREATOR - Creates ParagraphCard prefab with rounded corners and wider border
/// </summary>
public class ParagraphCardPrefabCreator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Create Paragraph Card Prefab")]
    public static void CreatePrefab()
    {
        Debug.Log("🎨 Creating ParagraphCard prefab with rounded corners...");
        
        // Create root card object
        GameObject cardRoot = new GameObject("ParagraphCard");
        
        // Add Image component with rounded corners material
        Image cardImage = cardRoot.AddComponent<Image>();
        cardImage.color = Color.white;
        cardImage.raycastTarget = false;
        // Note: You'll need to manually assign the UI/RoundedCorners material
        
        // Add Vertical Layout Group
        VerticalLayoutGroup rootLayout = cardRoot.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(0, 0, 0, 0);
        rootLayout.spacing = 0;
        rootLayout.childAlignment = TextAnchor.UpperLeft;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;
        
        // Add Content Size Fitter
        ContentSizeFitter rootFitter = cardRoot.AddComponent<ContentSizeFitter>();
        rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // Set RectTransform
        RectTransform cardRect = cardRoot.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(600, 100);
        
        // === CREATE LEFT BORDER (WIDER 25px) ===
        GameObject leftBorder = new GameObject("LeftBorder");
        leftBorder.transform.SetParent(cardRoot.transform);
        
        Image borderImage = leftBorder.AddComponent<Image>();
        borderImage.color = new Color32(0, 188, 212, 255); // Cyan #00BCD4
        borderImage.raycastTarget = false;
        // Note: You'll need to manually assign the UI/RoundedCorners material
        
        RectTransform borderRect = leftBorder.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0, 0);
        borderRect.anchorMax = new Vector2(0, 1); // This makes it stretch vertically
        borderRect.pivot = new Vector2(0, 0.5f);
        borderRect.anchoredPosition = new Vector2(0, 0);
        borderRect.sizeDelta = new Vector2(25, 0); // 25px width, 0 height = stretch
        
        // Add Layout Element - Ignore Layout to prevent interference
        LayoutElement borderLayout = leftBorder.AddComponent<LayoutElement>();
        borderLayout.ignoreLayout = true;
        borderLayout.layoutPriority = 1;
        
        // === CREATE CONTENT CONTAINER ===
        GameObject contentContainer = new GameObject("ContentContainer");
        contentContainer.transform.SetParent(cardRoot.transform);
        
        RectTransform containerRect = contentContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.offsetMin = new Vector2(35, 16); // Left (25 border + 10 padding), Bottom
        containerRect.offsetMax = new Vector2(-20, -16); // Right, Top
        
        // Add Vertical Layout Group to container
        VerticalLayoutGroup containerLayout = contentContainer.AddComponent<VerticalLayoutGroup>();
        containerLayout.padding = new RectOffset(0, 0, 0, 0);
        containerLayout.spacing = 8;
        containerLayout.childAlignment = TextAnchor.UpperLeft;
        containerLayout.childControlWidth = true;
        containerLayout.childControlHeight = true;
        containerLayout.childForceExpandWidth = true;
        containerLayout.childForceExpandHeight = false;
        
        // Add Content Size Fitter to container
        ContentSizeFitter containerFitter = contentContainer.AddComponent<ContentSizeFitter>();
        containerFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        containerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // === CREATE TITLE TEXT ===
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(contentContainer.transform);
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "ℹ️ Information";
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.black;
        titleText.alignment = TextAlignmentOptions.Left;
        titleText.enableWordWrapping = true;
        titleText.overflowMode = TextOverflowModes.Overflow;
        
        // CRITICAL FIX FOR VERTICAL TEXT
        titleText.horizontalMapping = TextureMappingOptions.Paragraph;
        titleText.verticalMapping = TextureMappingOptions.Line;
        
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, 30);
        
        // Add Layout Element
        LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.minHeight = 30;
        titleLayout.preferredHeight = -1;
        titleLayout.flexibleHeight = 0;
        
        // === CREATE BODY TEXT ===
        GameObject bodyObj = new GameObject("BodyText");
        bodyObj.transform.SetParent(contentContainer.transform);
        
        TextMeshProUGUI bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
        bodyText.text = "This is the body content of the card. It will wrap to multiple lines as needed.";
        bodyText.fontSize = 16;
        bodyText.fontStyle = FontStyles.Normal;
        bodyText.color = new Color32(38, 50, 56, 255); // Dark gray #263238
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.enableWordWrapping = true;
        bodyText.overflowMode = TextOverflowModes.Overflow;
        
        // CRITICAL FIX FOR VERTICAL TEXT
        bodyText.horizontalMapping = TextureMappingOptions.Paragraph;
        bodyText.verticalMapping = TextureMappingOptions.Line;
        
        RectTransform bodyRect = bodyText.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0, 1);
        bodyRect.anchorMax = new Vector2(1, 1);
        bodyRect.pivot = new Vector2(0.5f, 1);
        bodyRect.sizeDelta = new Vector2(0, 50);
        
        // Add Layout Element
        LayoutElement bodyLayout = bodyObj.AddComponent<LayoutElement>();
        bodyLayout.flexibleHeight = 1;
        bodyLayout.flexibleWidth = 1;
        
        // === SAVE AS PREFAB ===
        string prefabFolder = "Assets/Prefabs/UI";
        string prefabPath = prefabFolder + "/ParagraphCard.prefab";
        
        // Create folders if they don't exist
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder(prefabFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }
        
        // Save prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cardRoot, prefabPath);
        
        // Clean up temporary object
        DestroyImmediate(cardRoot);
        
        // Ping the prefab in project
        EditorGUIUtility.PingObject(prefab);
        Selection.activeObject = prefab;
        
        Debug.Log("✅ SUCCESS! ParagraphCard prefab created at: " + prefabPath);
        Debug.Log("⚠️ IMPORTANT: You need to manually assign:");
        Debug.Log("   1. UI/RoundedCorners material to the root Image");
        Debug.Log("   2. UI/RoundedCorners material to the LeftBorder Image");
        Debug.Log("   3. Configure the ImageWithRoundedCorners script settings if needed");
        
        EditorUtility.DisplayDialog(
            "Prefab Created!",
            "ParagraphCard prefab has been created successfully!\n\n" +
            "⚠️ MANUAL STEPS REQUIRED:\n" +
            "1. Select the ParagraphCard prefab in Project window\n" +
            "2. Assign 'UI/RoundedCorners' material to root Image component\n" +
            "3. Assign 'UI/RoundedCorners' material to LeftBorder Image\n" +
            "4. Add and configure ImageWithRoundedCorners script\n\n" +
            "Changes from original:\n" +
            "• Border width: 6px → 25px\n" +
            "• Border uses stretch anchors (no fixed height)\n" +
            "• Border has LayoutElement with ignoreLayout = true\n" +
            "• Content padding adjusted for wider border",
            "Got it!"
        );
    }
#endif
}