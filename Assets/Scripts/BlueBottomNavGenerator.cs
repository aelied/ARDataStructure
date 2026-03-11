using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BlueBottomNavGenerator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("GameObject/UI/Create Blue Bottom Navigation", false, 10)]
    static void CreateBlueBottomNavigation(MenuCommand menuCommand)
    {
        // Create Canvas if it doesn't exist
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Create main bottom navigation bar
        GameObject bottomNavBar = new GameObject("BottomNavigationBar");
        RectTransform barRect = bottomNavBar.AddComponent<RectTransform>();
        bottomNavBar.transform.SetParent(canvas.transform, false);
        
        // Set anchor to bottom stretch
        barRect.anchorMin = new Vector2(0, 0);
        barRect.anchorMax = new Vector2(1, 0);
        barRect.pivot = new Vector2(0.5f, 0);
        barRect.anchoredPosition = new Vector2(0, 0);
        barRect.sizeDelta = new Vector2(0, 100);

        // Create background with gradient
        GameObject bgObject = new GameObject("Background");
        RectTransform bgRect = bgObject.AddComponent<RectTransform>();
        bgObject.transform.SetParent(bottomNavBar.transform, false);
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        Image bgImage = bgObject.AddComponent<Image>();
        Texture2D gradientTexture = CreateSimpleGradientTexture();
        Sprite gradientSprite = Sprite.Create(
            gradientTexture,
            new Rect(0, 0, gradientTexture.width, gradientTexture.height),
            new Vector2(0.5f, 0.5f)
        );
        bgImage.sprite = gradientSprite;

        // Create the curved mask
        GameObject maskObject = new GameObject("CurveMask");
        RectTransform maskRect = maskObject.AddComponent<RectTransform>();
        maskObject.transform.SetParent(bottomNavBar.transform, false);
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.sizeDelta = Vector2.zero;
        maskRect.anchoredPosition = Vector2.zero;
        
        Image maskImage = maskObject.AddComponent<Image>();
        Texture2D maskTexture = CreateCurvedMaskTexture();
        maskTexture.alphaIsTransparency = true;
        Sprite maskSprite = Sprite.Create(
            maskTexture,
            new Rect(0, 0, maskTexture.width, maskTexture.height),
            new Vector2(0.5f, 0.5f)
        );
        maskImage.sprite = maskSprite;
        
        Mask mask = maskObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        
        // Move background under mask
        bgObject.transform.SetParent(maskObject.transform, true);

        // Create buttons
        GameObject homeBtn = CreateNavButton(bottomNavBar, "HomeButton", "Home", new Vector2(-320, 25), false);
        GameObject learnBtn = CreateNavButton(bottomNavBar, "LearnButton", "Learn", new Vector2(-160, 25), false);
        GameObject arBtn = CreateARButton(bottomNavBar, "ARButton", "3D\nAR", new Vector2(0, 50));
        GameObject progressBtn = CreateNavButton(bottomNavBar, "ProgressButton", "Progress", new Vector2(160, 25), false);
        GameObject profileBtn = CreateNavButton(bottomNavBar, "ProfileButton", "Profile", new Vector2(320, 25), false);

        // Add BottomNavigation script
        BottomNavigation navScript = bottomNavBar.AddComponent<BottomNavigation>();
        
        // Assign buttons to script
        navScript.homeButton = CreateNavButtonData(homeBtn);
        navScript.learnButton = CreateNavButtonData(learnBtn);
        navScript.arButton = CreateNavButtonData(arBtn);
        navScript.progressButton = CreateNavButtonData(progressBtn);
        navScript.profileButton = CreateNavButtonData(profileBtn);

        // Set colors
        navScript.activeColor = new Color(1f, 1f, 1f, 1f);
        navScript.inactiveColor = new Color(1f, 1f, 1f, 0.6f);

        // Select the created object
        Selection.activeGameObject = bottomNavBar;
        
        Debug.Log("✅ Blue Bottom Navigation with curved dip created successfully!");
    }

    static Texture2D CreateSimpleGradientTexture()
    {
        int width = 256;
        int height = 1;
        Texture2D texture = new Texture2D(width, height);
        
        Color purpleColor = new Color(0.39f, 0.39f, 0.9f); // #6464E5
        Color blueColor = new Color(0.23f, 0.49f, 0.96f);  // #3B7EF5
        
        for (int x = 0; x < width; x++)
        {
            float t = (float)x / width;
            Color color = Color.Lerp(purpleColor, blueColor, t);
            texture.SetPixel(x, 0, color);
        }
        
        texture.Apply();
        return texture;
    }

    static Texture2D CreateCurvedMaskTexture()
    {
        int width = 1024;
        int height = 256;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        float centerX = width / 2f;
        float dipRadius = 70f;  // Radius of the circular cutout
        float dipTop = height * 0.7f;  // Where the dip starts from top
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = Color.white;
                
                float distFromCenter = Mathf.Abs(x - centerX);
                
                // Create circular cutout from top
                if (distFromCenter <= dipRadius)
                {
                    float yThreshold = dipTop - Mathf.Sqrt(dipRadius * dipRadius - distFromCenter * distFromCenter);
                    
                    if (y >= yThreshold)
                    {
                        color = new Color(0, 0, 0, 0); // Transparent in cutout area
                    }
                }
                
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return texture;
    }


    static GameObject CreateNavButton(GameObject parent, string name, string label, Vector2 position, bool isCenter)
    {
        GameObject button = new GameObject(name);
        RectTransform btnRect = button.AddComponent<RectTransform>();
        button.transform.SetParent(parent.transform, false);
        
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = position;
        btnRect.sizeDelta = new Vector2(80, 80);
        
        Button btn = button.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;

        // Create icon
        GameObject icon = new GameObject("Icon");
        RectTransform iconRect = icon.AddComponent<RectTransform>();
        icon.transform.SetParent(button.transform, false);
        
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0, 10);
        iconRect.sizeDelta = new Vector2(32, 32);
        
        Image iconImage = icon.AddComponent<Image>();
        iconImage.color = new Color(1f, 1f, 1f, 0.6f);
        
        // Create placeholder icon sprite
        iconImage.sprite = CreateIconSprite(name);

        // Create label
        GameObject labelObj = new GameObject("Label");
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelObj.transform.SetParent(button.transform, false);
        
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0, -25);
        labelRect.sizeDelta = new Vector2(80, 30);
        
        TextMeshProUGUI text = labelObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 12;
        text.color = new Color(1f, 1f, 1f, 0.6f);
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Normal;

        return button;
    }

    static GameObject CreateARButton(GameObject parent, string name, string label, Vector2 position)
    {
        // Create container for AR button with glow
        GameObject arContainer = new GameObject(name + "_Container");
        RectTransform containerRect = arContainer.AddComponent<RectTransform>();
        arContainer.transform.SetParent(parent.transform, false);
        
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = position;
        containerRect.sizeDelta = new Vector2(100, 100);

        // Create outer glow
        GameObject outerGlow = new GameObject("OuterGlow");
        RectTransform glowRect = outerGlow.AddComponent<RectTransform>();
        outerGlow.transform.SetParent(arContainer.transform, false);
        
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.anchoredPosition = Vector2.zero;
        glowRect.sizeDelta = new Vector2(100, 100);
        
        Image glowImage = outerGlow.AddComponent<Image>();
        glowImage.sprite = CreateCircleSprite();
        glowImage.color = new Color(0.53f, 0.71f, 1f, 0.4f); // Light blue glow

        // Create inner circle
        GameObject innerCircle = new GameObject("InnerCircle");
        RectTransform innerRect = innerCircle.AddComponent<RectTransform>();
        innerCircle.transform.SetParent(arContainer.transform, false);
        
        innerRect.anchorMin = new Vector2(0.5f, 0.5f);
        innerRect.anchorMax = new Vector2(0.5f, 0.5f);
        innerRect.pivot = new Vector2(0.5f, 0.5f);
        innerRect.anchoredPosition = Vector2.zero;
        innerRect.sizeDelta = new Vector2(70, 70);
        
        Image innerImage = innerCircle.AddComponent<Image>();
        innerImage.sprite = CreateCircleSprite();
        innerImage.color = new Color(0.36f, 0.48f, 0.96f); // #5B7BF5

        // Create button
        GameObject button = new GameObject(name);
        RectTransform btnRect = button.AddComponent<RectTransform>();
        button.transform.SetParent(arContainer.transform, false);
        
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = Vector2.zero;
        btnRect.sizeDelta = new Vector2(70, 70);
        
        Button btn = button.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        
        Image btnImage = button.AddComponent<Image>();
        btnImage.color = new Color(0, 0, 0, 0); // Transparent

        // Create icon
        GameObject icon = new GameObject("Icon");
        RectTransform iconRect = icon.AddComponent<RectTransform>();
        icon.transform.SetParent(button.transform, false);
        
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(40, 40);
        
        Image iconImage = icon.AddComponent<Image>();
        iconImage.color = Color.white;
        iconImage.sprite = CreateCameraIconSprite();

        // Create label (3D AR text below)
        GameObject labelObj = new GameObject("Label");
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelObj.transform.SetParent(arContainer.transform, false);
        
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0, -55);
        labelRect.sizeDelta = new Vector2(80, 30);
        
        TextMeshProUGUI text = labelObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 11;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Normal;

        return button;
    }

    static Sprite CreateCircleSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = distance < radius ? 1f : 0f;
                
                // Smooth edges
                if (distance > radius - 2)
                {
                    alpha = Mathf.Max(0, (radius - distance) / 2f);
                }
                
                colors[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    static Sprite CreateIconSprite(string buttonName)
    {
        // Create simple placeholder sprites
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        // Fill with white
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    static Sprite CreateCameraIconSprite()
    {
        // Create simple camera icon
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        // Fill with white (placeholder)
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    static BottomNavigation.NavButton CreateNavButtonData(GameObject buttonObj)
    {
        BottomNavigation.NavButton navBtn = new BottomNavigation.NavButton();
        navBtn.button = buttonObj.GetComponent<Button>();
        navBtn.iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
        navBtn.label = buttonObj.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        
        // Create placeholder sprites for active/inactive states
        navBtn.inactiveIcon = CreateIconSprite(buttonObj.name);
        navBtn.activeIcon = CreateIconSprite(buttonObj.name + "_Active");
        
        return navBtn;
    }
#endif
}   