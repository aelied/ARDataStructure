using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class ProfilePanelManager : MonoBehaviour
{
    [Header("Profile Panel References")]
    public GameObject profilePanel;
    public GameObject homePanel;
    
    [Header("Profile Header")]
    public Image profilePictureDisplay;
    public Button avatarButton;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI streakText;
    public TextMeshProUGUI completedTopicsText;

    [Header("Header Info Cards (Top)")]
    public TextMeshProUGUI headerNameText;
    public TextMeshProUGUI headerEmailText;
    
    [Header("Profile Info Cards")]
    public TextMeshProUGUI nameValueText;
    public TextMeshProUGUI usernameValueText;
    public TextMeshProUGUI emailValueText;
    
    [Header("Progress Display")]
    public TextMeshProUGUI queueProgressText;
    public TextMeshProUGUI stacksProgressText;
    public TextMeshProUGUI linkedListsProgressText;
    public TextMeshProUGUI treesProgressText;
    public TextMeshProUGUI graphsProgressText;
    
    [Header("Buttons")]
    public Button logoutButton;
    public Button backButton;
    public Button changePasswordButton;
    public Button editProfileButton;
    
    [Header("Change Password Panel")]
    public GameObject changePasswordPanel;
    public TMP_InputField currentPasswordInput;
    public TMP_InputField newPasswordInput;
    public TMP_InputField confirmPasswordInput;
    public Button confirmChangePasswordButton;
    public Button cancelChangePasswordButton;
    public TextMeshProUGUI passwordErrorText;
    
    [Header("Edit Profile Panel")]
    public GameObject editProfilePanel;
    public TMP_InputField editNameInput;
    public TMP_InputField editUsernameInput;
    public TMP_InputField editEmailInput;
    public Button confirmEditProfileButton;
    public Button cancelEditProfileButton;
    public TextMeshProUGUI editProfileErrorText;
    
    [Header("Settings")]
    public string loginSceneName = "LoginRegister";
    public string serverUrl = "https://structureality-admin.onrender.com";
    
    [Header("Avatar Selection (Built-in)")]
    public GameObject avatarSelectionPanel;
    public Transform avatarGridContainer;
    public GameObject avatarButtonPrefab;
    public Button closeAvatarPanelButton;
    
    [Header("Preset Avatars")]
    public Sprite[] presetAvatarSprites;
    public Sprite defaultProfileSprite;
    public Color defaultProfileColor = new Color(0.4f, 0.8f, 1f);
    
    // User data
    private string currentUsername;
    private string studentName;
    private string email;
    private int currentStreak;
    private int completedTopics;
    
    // Track if server data has been loaded
    private bool serverDataLoaded = false;
    
    void Start()
    {
        LoadUserData();
        SetupButtons();
        
        // Hide panels initially
        if (avatarSelectionPanel != null)
        {
            avatarSelectionPanel.SetActive(false);
        }
        
        if (changePasswordPanel != null)
        {
            changePasswordPanel.SetActive(false);
        }
        
        if (editProfilePanel != null)
        {
            editProfilePanel.SetActive(false);
        }

         if (editUsernameInput != null)
        {
            editUsernameInput.interactable = false;
        }
        // Update display with placeholder data first
        UpdateProfileDisplay();
        
        // Then immediately fetch fresh data from server
        StartCoroutine(FetchUserDataFromServer());
    }
    
    void LoadUserData()
    {
        currentUsername = PlayerPrefs.GetString("CurrentUser", "");
        
        if (string.IsNullOrEmpty(currentUsername))
        {
            Debug.LogWarning("No user logged in!");
            SceneManager.LoadScene(loginSceneName);
            return;
        }
        
        // Load temporary local data (will be overwritten by server data)
        studentName = PlayerPrefs.GetString("User_" + currentUsername + "_Name", currentUsername);
        email = PlayerPrefs.GetString("User_" + currentUsername + "_Email", "");
        currentStreak = PlayerPrefs.GetInt("User_" + currentUsername + "_Streak", 0);
        completedTopics = PlayerPrefs.GetInt("User_" + currentUsername + "_CompletedTopics", 0);
        
        Debug.Log($"📦 LOCAL DATA loaded for {currentUsername}:");
        Debug.Log($"   Name: '{studentName}'");
        Debug.Log($"   Email: '{email}' (will be refreshed from server)");
    }
    
    void SetupButtons()
    {
        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveAllListeners();
            logoutButton.onClick.AddListener(OnLogoutClicked);
        }
        
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }
        
        if (avatarButton != null)
        {
            avatarButton.onClick.RemoveAllListeners();
            avatarButton.onClick.AddListener(OnAvatarClicked);
        }
        
        if (closeAvatarPanelButton != null)
        {
            closeAvatarPanelButton.onClick.RemoveAllListeners();
            closeAvatarPanelButton.onClick.AddListener(CloseAvatarSelection);
        }
        
        if (changePasswordButton != null)
        {
            changePasswordButton.onClick.RemoveAllListeners();
            changePasswordButton.onClick.AddListener(OnChangePasswordClicked);
        }
        
        if (confirmChangePasswordButton != null)
        {
            confirmChangePasswordButton.onClick.RemoveAllListeners();
            confirmChangePasswordButton.onClick.AddListener(OnConfirmChangePassword);
        }
        
        if (cancelChangePasswordButton != null)
        {
            cancelChangePasswordButton.onClick.RemoveAllListeners();
            cancelChangePasswordButton.onClick.AddListener(OnCancelChangePassword);
        }
        
        if (editProfileButton != null)
        {
            editProfileButton.onClick.RemoveAllListeners();
            editProfileButton.onClick.AddListener(OnEditProfileClicked);
        }
        
        if (confirmEditProfileButton != null)
        {
            confirmEditProfileButton.onClick.RemoveAllListeners();
            confirmEditProfileButton.onClick.AddListener(OnConfirmEditProfile);
        }
        
        if (cancelEditProfileButton != null)
        {
            cancelEditProfileButton.onClick.RemoveAllListeners();
            cancelEditProfileButton.onClick.AddListener(OnCancelEditProfile);
        }
    }
    
    void UpdateProfileDisplay()
    {
        // Update name and username in header
        if (nameText != null)
        {
            nameText.text = studentName;
        }
        
        if (usernameText != null)
        {
            usernameText.text = currentUsername;
        }
        
        // Update header stats
        if (streakText != null)
        {
            streakText.text = $"{currentStreak} Days";
        }
        
        if (completedTopicsText != null)
        {
            completedTopicsText.text = $"{completedTopics}/5 Topics";
        }
        
        // Update top info cards
        if (headerNameText != null)
        {
            headerNameText.text = studentName;
        }
        
        if (headerEmailText != null)
        {
            // Show loading indicator if server data not loaded yet
            if (!serverDataLoaded && string.IsNullOrEmpty(email))
            {
                headerEmailText.text = "Loading...";
            }
            else
            {
                headerEmailText.text = email;
            }
        }
        
        // Update bottom profile info cards
        if (nameValueText != null)
        {
            nameValueText.text = studentName;
        }
        
        if (usernameValueText != null)
        {
            usernameValueText.text = currentUsername;
        }
        
        if (emailValueText != null)
        {
            if (!serverDataLoaded && string.IsNullOrEmpty(email))
            {
                emailValueText.text = "Loading...";
            }
            else
            {
                emailValueText.text = email;
            }
        }
        
        // Load profile picture
        LoadProfilePicture();
        
        // Update progress
        UpdateTopicProgress("Queue", queueProgressText);
        UpdateTopicProgress("Stacks", stacksProgressText);
        UpdateTopicProgress("LinkedLists", linkedListsProgressText);
        UpdateTopicProgress("Trees", treesProgressText);
        UpdateTopicProgress("Graphs", graphsProgressText);
    }
    
    void LoadProfilePicture()
    {
        if (profilePictureDisplay == null)
        {
            Debug.LogWarning("Profile picture display not assigned!");
            return;
        }
        
        int avatarIndex = PlayerPrefs.GetInt($"ProfilePic_{currentUsername}", -1);
        
        if (avatarIndex >= 0 && presetAvatarSprites != null && avatarIndex < presetAvatarSprites.Length)
        {
            profilePictureDisplay.sprite = presetAvatarSprites[avatarIndex];
            profilePictureDisplay.color = Color.white;
        }
        else
        {
            if (defaultProfileSprite != null)
            {
                profilePictureDisplay.sprite = defaultProfileSprite;
            }
            profilePictureDisplay.color = defaultProfileColor;
        }
    }
    
    void OnAvatarClicked()
    {
        if (avatarSelectionPanel != null)
        {
            avatarSelectionPanel.SetActive(true);
            GenerateAvatarButtons();
        }
    }
    
    void CloseAvatarSelection()
    {
        if (avatarSelectionPanel != null)
        {
            avatarSelectionPanel.SetActive(false);
        }
    }
    
    void GenerateAvatarButtons()
    {
        if (avatarGridContainer == null || avatarButtonPrefab == null || presetAvatarSprites == null)
        {
            return;
        }
        
        foreach (Transform child in avatarGridContainer)
        {
            Destroy(child.gameObject);
        }
        
        int currentAvatarIndex = PlayerPrefs.GetInt($"ProfilePic_{currentUsername}", -1);
        
        for (int i = 0; i < presetAvatarSprites.Length; i++)
        {
            if (presetAvatarSprites[i] == null) continue;
            
            int index = i;
            GameObject avatarBtn = Instantiate(avatarButtonPrefab, avatarGridContainer);
            avatarBtn.name = $"AvatarButton_{i}";
            
            Image avatarImage = avatarBtn.GetComponentInChildren<Image>();
            if (avatarImage != null)
            {
                avatarImage.sprite = presetAvatarSprites[i];
                avatarImage.preserveAspect = true;
                avatarImage.color = Color.white;
            }
            
            Button button = avatarBtn.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectAvatar(index));
            }
            
            if (index == currentAvatarIndex)
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
        
        Canvas.ForceUpdateCanvases();
    }
    
    void SelectAvatar(int avatarIndex)
    {
        if (avatarIndex < 0 || avatarIndex >= presetAvatarSprites.Length)
        {
            return;
        }
        
        PlayerPrefs.SetInt($"ProfilePic_{currentUsername}", avatarIndex);
        PlayerPrefs.Save();
        
        LoadProfilePicture();
        CloseAvatarSelection();
    }
    
    void OnEditProfileClicked()
{
    if (editProfilePanel != null)
        {
            editProfilePanel.SetActive(true);
            
            if (editNameInput != null) editNameInput.text = studentName;
            if (editUsernameInput != null) 
            {
                editUsernameInput.text = currentUsername;
                editUsernameInput.interactable = false; // Ensure it's non-editable
            }
            if (editEmailInput != null) editEmailInput.text = email;
            if (editProfileErrorText != null) editProfileErrorText.text = "";
        }
    }
    
void OnConfirmEditProfile()
{
    if (editProfileErrorText != null)
    {
        editProfileErrorText.text = "";
    }
    
    string newName = editNameInput?.text.Trim() ?? "";
    string newEmail = editEmailInput?.text.Trim() ?? "";
    // Username is no longer editable, use current username
    string username = currentUsername;
    
    if (string.IsNullOrEmpty(newName))
    {
        ShowEditProfileError("Name is required!");
        return;
    }
    
    if (string.IsNullOrEmpty(newEmail))
    {
        ShowEditProfileError("Email is required!");
        return;
    }
    
    if (!IsValidEmail(newEmail))
    {
        ShowEditProfileError("Please enter a valid email address!");
        return;
    }
    
    if (newName == studentName && newEmail == email)
    {
        ShowEditProfileError("No changes detected!");
        return;
    }
    
    if (confirmEditProfileButton != null)
    {
        confirmEditProfileButton.interactable = false;
    }
    
    // Pass current username (unchanged)
    StartCoroutine(UpdateProfileOnServer(newName, username, newEmail));
}
    IEnumerator UpdateProfileOnServer(string newName, string username, string newEmail)
{
    string url = $"{serverUrl}/api/users/{currentUsername}/update-profile";
    
    // Only send name and email in the request
    string jsonData = $@"{{
        ""name"": ""{newName}"",
        ""email"": ""{newEmail}""
    }}";
    
    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
    
    using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
    {
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 10;
        
        yield return request.SendWebRequest();
        
        if (confirmEditProfileButton != null)
        {
            confirmEditProfileButton.interactable = true;
        }
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"✓ Profile updated successfully");
            
            // Update only name and email (username stays the same)
            studentName = newName;
            email = newEmail;
            
            PlayerPrefs.SetString("User_" + currentUsername + "_Name", newName);
            PlayerPrefs.SetString("User_" + currentUsername + "_Email", newEmail);
            PlayerPrefs.Save();
            
            if (editProfileErrorText != null)
            {
                editProfileErrorText.text = "<color=green>Profile updated successfully!</color>";
            }
            
            UpdateProfileDisplay();
            Invoke("OnCancelEditProfile", 1.5f);
        }
        else
        {
            string responseText = request.downloadHandler.text;
            Debug.LogError($"❌ Profile update failed: {responseText}");
            
            string errorMsg = "Failed to update profile";
            
            try
            {
                ServerResponse response = JsonUtility.FromJson<ServerResponse>(responseText);
                if (!string.IsNullOrEmpty(response.error))
                {
                    errorMsg = response.error;
                }
            }
            catch
            {
                if (responseText.ToLower().Contains("email") && responseText.ToLower().Contains("exists"))
                {
                    errorMsg = "Email is already registered";
                }
            }
            
            ShowEditProfileError(errorMsg);
        }
    }
}

    
    void MigrateUserData(string oldUsername, string newUsername)
    {
        Debug.Log($"🔄 Migrating user data from {oldUsername} to {newUsername}");
        
        if (PlayerPrefs.HasKey("User_" + oldUsername + "_Name"))
        {
            PlayerPrefs.SetString("User_" + newUsername + "_Name", PlayerPrefs.GetString("User_" + oldUsername + "_Name"));
        }
        if (PlayerPrefs.HasKey("User_" + oldUsername + "_Email"))
        {
            PlayerPrefs.SetString("User_" + newUsername + "_Email", PlayerPrefs.GetString("User_" + oldUsername + "_Email"));
        }
        if (PlayerPrefs.HasKey("User_" + oldUsername + "_Streak"))
        {
            PlayerPrefs.SetInt("User_" + newUsername + "_Streak", PlayerPrefs.GetInt("User_" + oldUsername + "_Streak"));
        }
        if (PlayerPrefs.HasKey("User_" + oldUsername + "_CompletedTopics"))
        {
            PlayerPrefs.SetInt("User_" + newUsername + "_CompletedTopics", PlayerPrefs.GetInt("User_" + oldUsername + "_CompletedTopics"));
        }
        
        if (PlayerPrefs.HasKey($"ProfilePic_{oldUsername}"))
        {
            PlayerPrefs.SetInt($"ProfilePic_{newUsername}", PlayerPrefs.GetInt($"ProfilePic_{oldUsername}"));
        }
        
        string[] topics = { "Queue", "Stacks", "LinkedLists", "Trees", "Graphs", "Queues" };
        
        foreach (string topic in topics)
        {
            if (PlayerPrefs.HasKey($"User_{oldUsername}_{topic}_TutorialCompleted"))
            {
                PlayerPrefs.SetInt($"User_{newUsername}_{topic}_TutorialCompleted", 
                    PlayerPrefs.GetInt($"User_{oldUsername}_{topic}_TutorialCompleted"));
            }
            if (PlayerPrefs.HasKey($"User_{oldUsername}_{topic}_PuzzleCompleted"))
            {
                PlayerPrefs.SetInt($"User_{newUsername}_{topic}_PuzzleCompleted", 
                    PlayerPrefs.GetInt($"User_{oldUsername}_{topic}_PuzzleCompleted"));
            }
            if (PlayerPrefs.HasKey($"User_{oldUsername}_{topic}_Score"))
            {
                PlayerPrefs.SetInt($"User_{newUsername}_{topic}_Score", 
                    PlayerPrefs.GetInt($"User_{oldUsername}_{topic}_Score"));
            }
        }
        
        PlayerPrefs.Save();
        Debug.Log($"✓ User data migrated successfully");
    }
    
    void OnCancelEditProfile()
    {
        if (editProfilePanel != null)
        {
            editProfilePanel.SetActive(false);
        }
        
        if (editNameInput != null) editNameInput.text = "";
        if (editUsernameInput != null) editUsernameInput.text = "";
        if (editEmailInput != null) editEmailInput.text = "";
        if (editProfileErrorText != null) editProfileErrorText.text = "";
    }
    
    void ShowEditProfileError(string message)
    {
        if (editProfileErrorText != null)
        {
            editProfileErrorText.text = $"<color=red>{message}</color>";
        }
    }
    
    bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
    
    void OnChangePasswordClicked()
    {
        if (changePasswordPanel != null)
        {
            changePasswordPanel.SetActive(true);
            
            if (currentPasswordInput != null) currentPasswordInput.text = "";
            if (newPasswordInput != null) newPasswordInput.text = "";
            if (confirmPasswordInput != null) confirmPasswordInput.text = "";
            if (passwordErrorText != null) passwordErrorText.text = "";
        }
    }
    
    void OnConfirmChangePassword()
    {
        if (passwordErrorText != null)
        {
            passwordErrorText.text = "";
        }
        
        string currentPassword = currentPasswordInput?.text ?? "";
        string newPassword = newPasswordInput?.text ?? "";
        string confirmPassword = confirmPasswordInput?.text ?? "";
        
        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            ShowPasswordError("All fields are required!");
            return;
        }
        
        if (newPassword != confirmPassword)
        {
            ShowPasswordError("New passwords do not match!");
            return;
        }
        
        if (newPassword.Length < 6)
        {
            ShowPasswordError("Password must be at least 6 characters!");
            return;
        }
        
        if (newPassword == currentPassword)
        {
            ShowPasswordError("New password must be different from current password!");
            return;
        }
        
        if (confirmChangePasswordButton != null)
        {
            confirmChangePasswordButton.interactable = false;
        }
        
        StartCoroutine(ChangePasswordOnServer(currentPassword, newPassword));
    }
    
    IEnumerator ChangePasswordOnServer(string currentPassword, string newPassword)
    {
        string url = $"{serverUrl}/api/users/{currentUsername}/change-password";
        
        string jsonData = $@"{{
            ""currentPassword"": ""{currentPassword}"",
            ""newPassword"": ""{newPassword}""
        }}";
        
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        
        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;
            
            yield return request.SendWebRequest();
            
            if (confirmChangePasswordButton != null)
            {
                confirmChangePasswordButton.interactable = true;
            }
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✓ Password changed successfully");
                
                if (passwordErrorText != null)
                {
                    passwordErrorText.text = "<color=green>Password changed successfully!</color>";
                }
                
                Invoke("OnCancelChangePassword", 1.5f);
            }
            else
            {
                string responseText = request.downloadHandler.text;
                string errorMsg = "Failed to change password";
                
                try
                {
                    ServerResponse response = JsonUtility.FromJson<ServerResponse>(responseText);
                    if (!string.IsNullOrEmpty(response.error))
                    {
                        errorMsg = response.error;
                    }
                }
                catch
                {
                    if (responseText.ToLower().Contains("incorrect"))
                    {
                        errorMsg = "Current password is incorrect";
                    }
                }
                
                ShowPasswordError(errorMsg);
            }
        }
    }
    
    void OnCancelChangePassword()
    {
        if (changePasswordPanel != null)
        {
            changePasswordPanel.SetActive(false);
        }
        
        if (currentPasswordInput != null) currentPasswordInput.text = "";
        if (newPasswordInput != null) newPasswordInput.text = "";
        if (confirmPasswordInput != null) confirmPasswordInput.text = "";
        if (passwordErrorText != null) passwordErrorText.text = "";
    }
    
    void ShowPasswordError(string message)
    {
        if (passwordErrorText != null)
        {
            passwordErrorText.text = $"<color=red>{message}</color>";
        }
    }
    
    void UpdateTopicProgress(string topic, TextMeshProUGUI progressText)
    {
        if (progressText == null) return;
        
        bool tutorialCompleted = PlayerPrefs.GetInt($"User_{currentUsername}_{topic}_TutorialCompleted", 0) == 1;
        bool puzzleCompleted = PlayerPrefs.GetInt($"User_{currentUsername}_{topic}_PuzzleCompleted", 0) == 1;
        int score = PlayerPrefs.GetInt($"User_{currentUsername}_{topic}_Score", 0);
        
        string status = "";
        if (puzzleCompleted)
        {
            status = $"✓ Completed - Score: {score}";
        }
        else if (tutorialCompleted)
        {
            status = $"Tutorial Done - Score: {score}";
        }
        else
        {
            status = "Not Started";
        }
        
        progressText.text = $"{topic}: {status}";
    }
    
    void OnLogoutClicked()
    {
        Debug.Log("Logging out user: " + currentUsername);
        
        PlayerPrefs.DeleteKey("CurrentUser");
        
        string deviceId = GetDeviceId();
        PlayerPrefs.DeleteKey("RememberMeEnabled_" + deviceId);
        PlayerPrefs.DeleteKey("RememberedEmail_" + deviceId);
        
        PlayerPrefs.Save();
        
        SceneManager.LoadScene(loginSceneName);
    }
    
    void OnBackClicked()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(false);
        }
        
        if (homePanel != null)
        {
            homePanel.SetActive(true);
        }
    }
    
    string GetDeviceId()
    {
        string key = "DeviceUniqueID";
        
        if (!PlayerPrefs.HasKey(key))
        {
            string newId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString(key, newId);
            PlayerPrefs.Save();
        }
        
        return PlayerPrefs.GetString(key);
    }
    
    public void ShowProfile()
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(true);
        }
        
        if (homePanel != null)
        {
            homePanel.SetActive(false);
        }
        
        // Always fetch fresh data when showing profile
        StartCoroutine(FetchUserDataFromServer());
    }
    
    public void OnAvatarChanged()
    {
        LoadProfilePicture();
    }
    
    public int GetCurrentAvatarIndex()
    {
        return PlayerPrefs.GetInt($"ProfilePic_{currentUsername}", -1);
    }
    
    // ===== CRITICAL: ALWAYS FETCH REAL EMAIL FROM SERVER =====
IEnumerator FetchUserDataFromServer()
{
    string url = $"{serverUrl}/api/users/{currentUsername}";
    
    Debug.Log($"🔄 Fetching fresh user data from: {url}");
    
    using (UnityWebRequest request = UnityWebRequest.Get(url))
    {
        request.timeout = 10;
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            string rawResponse = request.downloadHandler.text;
            Debug.Log($"📥 SERVER RESPONSE: {rawResponse}");
            
            // Parse without try-catch to avoid yield issues
            UserDataResponse userData = JsonUtility.FromJson<UserDataResponse>(rawResponse);
            
            if (userData != null && !string.IsNullOrEmpty(userData.username))
            {
                Debug.Log($"✅ FRESH SERVER DATA:");
                Debug.Log($"   Username: '{userData.username}'");
                Debug.Log($"   Name: '{userData.name}'");
                Debug.Log($"   Email: '{userData.email}'");
                Debug.Log($"   Streak: {userData.streak}");
                
                // Update with REAL server data
                studentName = userData.name;
                email = userData.email;
                currentStreak = userData.streak;
                completedTopics = userData.completedTopics;
                
                // Save REAL data locally
                PlayerPrefs.SetString("User_" + currentUsername + "_Name", userData.name);
                PlayerPrefs.SetString("User_" + currentUsername + "_Email", userData.email);
                PlayerPrefs.SetInt("User_" + currentUsername + "_Streak", userData.streak);
                PlayerPrefs.SetInt("User_" + currentUsername + "_CompletedTopics", userData.completedTopics);
                PlayerPrefs.Save();
                
                serverDataLoaded = true;
                
                // Update display with fresh data
                UpdateProfileDisplay();
                
                // Also fetch progress
                yield return FetchProgressFromServer();
            }
            else
            {
                Debug.LogError($"❌ Failed to parse user data: userData is null or invalid");
                Debug.LogError($"Raw response: {rawResponse}");
            }
        }
        else
        {
            Debug.LogError($"❌ Failed to fetch user data from server");
            Debug.LogError($"   Error: {request.error}");
            Debug.LogError($"   Response Code: {request.responseCode}");
            Debug.LogError($"   Response: {request.downloadHandler.text}");
        }
    }
}
    
    IEnumerator FetchProgressFromServer()
    {
        string url = $"{serverUrl}/api/progress/{currentUsername}";
        
        Debug.Log($"🔄 Fetching progress data from: {url}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var progressData = JsonUtility.FromJson<ProgressDataResponse>(request.downloadHandler.text);
                    
                    if (progressData != null && progressData.success && progressData.data != null)
                    {
                        foreach (var topic in progressData.data.topics)
                        {
                            PlayerPrefs.SetInt($"User_{currentUsername}_{topic.topicName}_TutorialCompleted", topic.tutorialCompleted ? 1 : 0);
                            PlayerPrefs.SetInt($"User_{currentUsername}_{topic.topicName}_PuzzleCompleted", topic.puzzleCompleted ? 1 : 0);
                            PlayerPrefs.SetInt($"User_{currentUsername}_{topic.topicName}_Score", topic.puzzleScore);
                        }
                        
                        PlayerPrefs.Save();
                        Debug.Log($"✓ Progress synced from server");
                        
                        UpdateProfileDisplay();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Error parsing progress data: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Failed to fetch progress: {request.error}");
            }
        }
    }
    
    // JSON Response Classes
    [System.Serializable]
    public class UserDataResponse
    {
        public string username;
        public string name;
        public string email;
        public int streak;
        public int completedTopics;
    }
    
    [System.Serializable]
    public class ProgressDataResponse
    {
        public bool success;
        public ProgressData data;
    }
    
    [System.Serializable]
    public class ProgressData
    {
        public string username;
        public TopicProgress[] topics;
    }
    
    [System.Serializable]
    public class TopicProgress
    {
        public string topicName;
        public bool tutorialCompleted;
        public bool puzzleCompleted;
        public int puzzleScore;
        public float progressPercentage;
    }
    
    [System.Serializable]
    public class ServerResponse
    {
        public bool success;
        public string message;
        public string error;
    }
}