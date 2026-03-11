using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProfilePictureManager : MonoBehaviour
{
    [Header("Profile Picture UI")]
    public Image profilePictureDisplay;
    public Button changeProfilePictureButton;
    
    [Header("Avatar Selection Panel")]
    public GameObject avatarSelectionPanel;
    public Transform avatarGridContainer;
    public GameObject avatarButtonPrefab;
    public Button closeAvatarPanelButton;
    
    [Header("Preset Avatars")]
    public Sprite[] presetAvatarSprites;
    
    [Header("Settings")]
    public Color defaultProfileColor = new Color(0.4f, 0.8f, 1f);
    public Sprite defaultProfileSprite;
    
    [Header("References")]
    public ProfilePanelManager profilePanelManager;
    
    private int selectedAvatarIndex = -1;
    
    void Start()
    {
        ValidateReferences();
        SetupButtons();
        
        // Load avatar for current user or temp selection
        string currentUser = PlayerPrefs.GetString("CurrentUser", "");
        if (!string.IsNullOrEmpty(currentUser))
        {
            LoadProfilePicture(currentUser);
        }
        else
        {
            LoadProfilePicture(""); // This will check for temp selection
        }
        
        if (avatarSelectionPanel != null)
            avatarSelectionPanel.SetActive(false);
    }
    
    void ValidateReferences()
    {
        bool hasErrors = false;
        
        if (profilePictureDisplay == null)
        {
            Debug.LogError("ProfilePictureDisplay is not assigned!");
            hasErrors = true;
        }
        
        if (changeProfilePictureButton == null)
        {
            Debug.LogError("ChangeProfilePictureButton is not assigned!");
            hasErrors = true;
        }
        
        if (avatarSelectionPanel == null)
        {
            Debug.LogError("AvatarSelectionPanel is not assigned!");
            hasErrors = true;
        }
        
        if (avatarGridContainer == null)
        {
            Debug.LogError("AvatarGridContainer is not assigned!");
            hasErrors = true;
        }
        
        if (avatarButtonPrefab == null)
        {
            Debug.LogError("AvatarButtonPrefab is not assigned!");
            hasErrors = true;
        }
        
        if (closeAvatarPanelButton == null)
        {
            Debug.LogError("CloseAvatarPanelButton is not assigned!");
            hasErrors = true;
        }
        
        if (presetAvatarSprites == null || presetAvatarSprites.Length == 0)
        {
            Debug.LogError("No preset avatar sprites assigned!");
            hasErrors = true;
        }
        
        if (!hasErrors)
        {
            Debug.Log("✓ All ProfilePictureManager references validated successfully!");
        }
    }
    
    void SetupButtons()
    {
        if (changeProfilePictureButton != null)
        {
            changeProfilePictureButton.onClick.RemoveAllListeners();
            changeProfilePictureButton.onClick.AddListener(OpenAvatarSelection);
            Debug.Log("✓ Change profile picture button configured");
        }
            
        if (closeAvatarPanelButton != null)
        {
            closeAvatarPanelButton.onClick.RemoveAllListeners();
            closeAvatarPanelButton.onClick.AddListener(CloseAvatarSelection);
            Debug.Log("✓ Close avatar panel button configured");
        }
    }
    
    void SetupDefaultProfile()
    {
        if (profilePictureDisplay != null)
        {
            if (defaultProfileSprite != null)
            {
                profilePictureDisplay.sprite = defaultProfileSprite;
            }
            profilePictureDisplay.color = defaultProfileColor;
            Debug.Log("✓ Default profile picture set");
        }
    }
    
    public void OpenAvatarSelection()
    {
        Debug.Log("OpenAvatarSelection called");
        
        if (avatarSelectionPanel != null)
        {
            avatarSelectionPanel.SetActive(true);
            Debug.Log("✓ Avatar panel opened");
            GenerateAvatarButtons();
        }
        else
        {
            Debug.LogError("Avatar Selection Panel is not assigned!");
        }
    }
    
    public void CloseAvatarSelection()
    {
        Debug.Log("CloseAvatarSelection called");
        
        if (avatarSelectionPanel != null)
        {
            avatarSelectionPanel.SetActive(false);
            Debug.Log("✓ Avatar panel closed");
        }
        else
        {
            Debug.LogError("Avatar Selection Panel is not assigned!");
        }
    }
    
    void GenerateAvatarButtons()
    {
        Debug.Log("=== GENERATE AVATAR BUTTONS START ===");
        
        if (avatarGridContainer == null)
        {
            Debug.LogError("Avatar Grid Container is not assigned!");
            return;
        }
        
        if (avatarButtonPrefab == null)
        {
            Debug.LogError("Avatar Button Prefab is not assigned!");
            return;
        }
        
        if (presetAvatarSprites == null || presetAvatarSprites.Length == 0)
        {
            Debug.LogError("No avatar sprites assigned!");
            return;
        }
        
        Debug.Log($"Container: {avatarGridContainer.name}, Active: {avatarGridContainer.gameObject.activeInHierarchy}");
        Debug.Log($"Generating {presetAvatarSprites.Length} avatar buttons...");
            
        // Clear existing buttons
        int childrenBefore = avatarGridContainer.childCount;
        foreach (Transform child in avatarGridContainer)
        {
            Destroy(child.gameObject);
        }
        Debug.Log($"Cleared {childrenBefore} existing children");
        
        // Create button for each preset avatar
        int successCount = 0;
        for (int i = 0; i < presetAvatarSprites.Length; i++)
        {
            if (presetAvatarSprites[i] == null)
            {
                Debug.LogWarning($"Avatar sprite at index {i} is null, skipping...");
                continue;
            }
            
            int index = i;
            GameObject avatarBtn = Instantiate(avatarButtonPrefab, avatarGridContainer);
            avatarBtn.name = $"AvatarButton_{i}";
            
            Image avatarImage = avatarBtn.GetComponentInChildren<Image>();
            if (avatarImage != null)
            {
                avatarImage.sprite = presetAvatarSprites[i];
                avatarImage.preserveAspect = true;
                avatarImage.color = Color.white;
                avatarImage.enabled = true;
                Debug.Log($"✓ Button {i} Image configured");
                successCount++;
            }
            else
            {
                Debug.LogError($"Button {i} has no Image component!");
            }
            
            Button button = avatarBtn.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectAvatar(index));
                Debug.Log($"✓ Added click listener to button {i}");
            }
            else
            {
                Debug.LogError($"Button {i} has no Button component!");
            }
            
            // Highlight if currently selected
            if (index == selectedAvatarIndex)
            {
                Outline outline = avatarBtn.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = avatarBtn.AddComponent<Outline>();
                }
                outline.enabled = true;
                outline.effectColor = Color.yellow;
                outline.effectDistance = new Vector2(3, 3);
            }
        }
        
        Debug.Log($"✓ Successfully created {successCount} avatar buttons");
        
        // Force layout update
        Canvas.ForceUpdateCanvases();
        RectTransform containerRect = avatarGridContainer.GetComponent<RectTransform>();
        if (containerRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            Debug.Log("✓ Layout rebuilt");
        }
        
        Debug.Log("=== GENERATE AVATAR BUTTONS END ===");
    }
    
    public void SelectAvatar(int avatarIndex)
    {
        Debug.Log($"SelectAvatar called with index: {avatarIndex}");
        
        if (avatarIndex < 0 || avatarIndex >= presetAvatarSprites.Length)
        {
            Debug.LogError($"Invalid avatar index: {avatarIndex}");
            return;
        }
            
        selectedAvatarIndex = avatarIndex;
        
        // Update display
        if (profilePictureDisplay != null)
        {
            profilePictureDisplay.sprite = presetAvatarSprites[avatarIndex];
            profilePictureDisplay.color = Color.white;
            Debug.Log($"✓ Profile picture updated to avatar {avatarIndex}");
        }
        
        // Save selection
        SaveProfilePicture();
        
        // Notify ProfilePanelManager if it exists
        if (profilePanelManager != null)
        {
            profilePanelManager.OnAvatarChanged();
        }
        
        // Close panel
        CloseAvatarSelection();
    }
    
    void SaveProfilePicture()
    {
        string username = PlayerPrefs.GetString("CurrentUser", "");
        
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("No user logged in - saving as temporary selection for registration");
            PlayerPrefs.SetInt("TempProfilePicIndex", selectedAvatarIndex);
            PlayerPrefs.Save();
            Debug.Log($"✓ Temporary avatar saved: {selectedAvatarIndex}");
            return;
        }
        
        // Save avatar index for this user
        PlayerPrefs.SetInt($"ProfilePic_{username}", selectedAvatarIndex);
        PlayerPrefs.Save();
        
        Debug.Log($"✓ Profile picture saved for {username}: Avatar {selectedAvatarIndex}");
    }
    
    public void LoadProfilePicture(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            // Try to load temporary selection (for registration screen)
            int tempIndex = PlayerPrefs.GetInt("TempProfilePicIndex", -1);
            if (tempIndex >= 0 && tempIndex < presetAvatarSprites.Length)
            {
                selectedAvatarIndex = tempIndex;
                if (profilePictureDisplay != null)
                {
                    profilePictureDisplay.sprite = presetAvatarSprites[tempIndex];
                    profilePictureDisplay.color = Color.white;
                }
                Debug.Log($"✓ Loaded temporary profile picture: Avatar {tempIndex}");
            }
            else
            {
                SetupDefaultProfile();
            }
            return;
        }
        
        // Load avatar for specific user
        int avatarIndex = PlayerPrefs.GetInt($"ProfilePic_{username}", -1);
        
        if (avatarIndex >= 0 && avatarIndex < presetAvatarSprites.Length)
        {
            selectedAvatarIndex = avatarIndex;
            if (profilePictureDisplay != null)
            {
                profilePictureDisplay.sprite = presetAvatarSprites[avatarIndex];
                profilePictureDisplay.color = Color.white;
            }
            Debug.Log($"✓ Loaded profile picture for {username}: Avatar {avatarIndex}");
        }
        else
        {
            SetupDefaultProfile();
            Debug.Log($"No profile picture found for {username}, using default");
        }
    }
    
    public void ResetToDefault()
    {
        selectedAvatarIndex = -1;
        SetupDefaultProfile();
        
        // Clear temporary selection
        PlayerPrefs.DeleteKey("TempProfilePicIndex");
        PlayerPrefs.Save();
        
        Debug.Log("✓ Profile picture reset to default");
    }
    
    public int GetSelectedAvatarIndex()
    {
        return selectedAvatarIndex;
    }
    
    public Sprite GetCurrentProfileSprite()
    {
        return profilePictureDisplay?.sprite;
    }
    
    // Transfer temp selection to user account after registration
    public void SaveTempSelectionToUser(string username)
    {
        if (selectedAvatarIndex >= 0)
        {
            PlayerPrefs.SetInt($"ProfilePic_{username}", selectedAvatarIndex);
            PlayerPrefs.DeleteKey("TempProfilePicIndex");
            PlayerPrefs.Save();
            Debug.Log($"✓ Profile picture transferred to new user: {username}");
        }
    }
    
    // NEW: Public method to refresh the display
    public void RefreshDisplay()
    {
        string currentUser = PlayerPrefs.GetString("CurrentUser", "");
        LoadProfilePicture(currentUser);
    }
}