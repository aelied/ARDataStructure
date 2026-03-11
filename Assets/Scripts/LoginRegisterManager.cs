using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Text;
using System.Collections.Generic;
public class LoginRegisterManager : MonoBehaviour
{
    [Header("Login Panel")]
    public GameObject loginPanel;
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public Button togglePasswordButton;
    public Button loginButton;
    public Button forgotPasswordButton;
    public Button goToRegisterButton;
    public TextMeshProUGUI errorText;


    [Header("Forgot Password UI")]
    public GameObject forgotPasswordPanel;
    public TMP_InputField forgotEmailField;
    public Button sendResetButton;
    public Button backToLoginFromForgotButton;
    public TextMeshProUGUI forgotPasswordMessageText;


    [Header("Register Panel")]
    public GameObject registerPanel;
    public TMP_InputField registerNameField;
    public TMP_InputField registerUsernameField;
    public TMP_InputField registerEmailField;
    public TMP_InputField registerPasswordField;
    public Button toggleRegisterPasswordButton;
    public TMP_InputField confirmPasswordField;
    public Button toggleConfirmPasswordButton;
    public TMP_Dropdown instructorDropdown;
    public Button registerButton;
    public Button goToLoginButton;
    public TextMeshProUGUI registerErrorText;
    
    [Header("Profile Picture (NEW)")]
    public ProfilePictureManager profilePictureManager;

    [Header("Register Panel - Difficulty Level")]
    public Button beginnerButton;
    public Button intermediateButton;
    private string selectedDifficulty = "beginner";

    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";
    public int minimumPasswordLength = 6;
    
    [Header("Instructor Data")]
    private List<InstructorData> availableInstructors = new List<InstructorData>();

    [Header("Server Settings")]
    [Tooltip("Your server URL - deployed server or local testing")]
    public string serverUrl = "https://structureality-admin.onrender.com";
    public bool enableDebugMode = true;

    private const string CURRENT_USER_KEY = "CurrentUser";

    void Awake()
    {
        selectedDifficulty = "beginner";
    }

    void Start()
    {
        PlayerPrefs.DeleteKey("CurrentUser");
        PlayerPrefs.Save();
        
        ClearAllInputFields();
        HideAllErrors();
        HideAllPanels();
        
        if (!ValidateRequiredFields())
        {
            Debug.LogError("LoginRegisterManager: Missing required UI references!");
            return;
        }

        ShowLoginPanel();
        SetupButtonListeners();
        SetupEnterKeySupport();
        SetupPasswordFields();
        SetupInputFieldListeners();

        if (enableDebugMode)
            Debug.Log("LoginRegisterManager initialized - Database Mode");

        if (instructorDropdown != null)
            StartCoroutine(LoadInstructors());

        selectedDifficulty = "beginner";
        SetupDifficultyButtons();
    }

    // ─── Difficulty Buttons ───────────────────────────────────────────────────

    void SetupDifficultyButtons()
    {
        if (beginnerButton != null)
            beginnerButton.onClick.AddListener(() => SelectDifficulty("beginner"));

        if (intermediateButton != null)
            intermediateButton.onClick.AddListener(() => SelectDifficulty("intermediate"));
    }

    void SelectDifficulty(string difficulty)
    {
        selectedDifficulty = difficulty;
        UpdateDifficultyButtonVisuals();
        Debug.Log($"✓ Difficulty selected: {selectedDifficulty}");
    }

    void UpdateDifficultyButtonVisuals()
    {
        if (beginnerButton != null)
        {
            var colors = beginnerButton.colors;
            colors.normalColor = selectedDifficulty == "beginner"
                ? new Color(0.45f, 0.25f, 1f, 1f)
                : new Color(0.6f, 0.6f, 0.6f, 0.5f);
            beginnerButton.colors = colors;
        }

        if (intermediateButton != null)
        {
            var colors = intermediateButton.colors;
            colors.normalColor = selectedDifficulty == "intermediate"
                ? new Color(0.45f, 0.25f, 1f, 1f)
                : new Color(0.6f, 0.6f, 0.6f, 0.5f);
            intermediateButton.colors = colors;
        }
    }

    // ─── Instructors ──────────────────────────────────────────────────────────

    IEnumerator LoadInstructors()
    {
        string url = serverUrl + "/api/instructors";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    InstructorsResponse response = JsonUtility.FromJson<InstructorsResponse>(request.downloadHandler.text);
                    
                    if (response.success && response.instructors != null)
                    {
                        availableInstructors = response.instructors;
                        PopulateInstructorDropdown();
                        Debug.Log($"✅ Loaded {availableInstructors.Count} instructors");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to parse instructors: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Failed to load instructors: {request.error}");
            }
        }
    }

    void PopulateInstructorDropdown()
    {
        if (instructorDropdown == null) return;
        
        instructorDropdown.ClearOptions();
        
        List<string> options = new List<string> { "No Instructor" };
        
        foreach (var instructor in availableInstructors)
            options.Add(instructor.name);
        
        instructorDropdown.AddOptions(options);
        instructorDropdown.value = 0;
    }

    // ─── Input Field Listeners ────────────────────────────────────────────────

    void SetupInputFieldListeners()
    {
        if (emailField != null)
            emailField.onValueChanged.AddListener(delegate { HideAllErrors(); });
        if (passwordField != null)
            passwordField.onValueChanged.AddListener(delegate { HideAllErrors(); });
        if (registerNameField != null)
            registerNameField.onValueChanged.AddListener(delegate { HideAllErrors(); });
        if (registerUsernameField != null)
            registerUsernameField.onValueChanged.AddListener(delegate { HideAllErrors(); });
        if (registerEmailField != null)
            registerEmailField.onValueChanged.AddListener(delegate { HideAllErrors(); });
        if (registerPasswordField != null)
            registerPasswordField.onValueChanged.AddListener(delegate { HideAllErrors(); });
        if (confirmPasswordField != null)
            confirmPasswordField.onValueChanged.AddListener(delegate { HideAllErrors(); });
        if (forgotEmailField != null)
            forgotEmailField.onValueChanged.AddListener(delegate { HideForgotPasswordMessage(); });
    }

    void HideLoginError()
    {
        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }

    void HideRegisterError()
    {
        if (registerErrorText != null)
            registerErrorText.gameObject.SetActive(false);
    }

    void HideForgotPasswordMessage()
    {
        if (forgotPasswordMessageText != null)
            forgotPasswordMessageText.gameObject.SetActive(false);
    }

    void HideAllPanels()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(false);
    }

    void HideAllErrors()
    {
        if (errorText != null) errorText.gameObject.SetActive(false);
        if (registerErrorText != null) registerErrorText.gameObject.SetActive(false);
        if (forgotPasswordMessageText != null) forgotPasswordMessageText.gameObject.SetActive(false);
    }

    void ClearAllInputFields()
    {
        if (emailField != null) emailField.text = "";
        if (passwordField != null) passwordField.text = "";
        if (registerNameField != null) registerNameField.text = "";
        if (registerUsernameField != null) registerUsernameField.text = "";
        if (registerEmailField != null) registerEmailField.text = "";
        if (registerPasswordField != null) registerPasswordField.text = "";
        if (confirmPasswordField != null) confirmPasswordField.text = "";
    }

    bool ValidateRequiredFields()
    {
        return loginPanel != null && registerPanel != null && 
               emailField != null && passwordField != null;
    }

    void SetupButtonListeners()
    {
        if (loginButton != null)
            loginButton.onClick.AddListener(AttemptLogin);
        if (forgotPasswordButton != null)
            forgotPasswordButton.onClick.AddListener(OnForgotPassword);
        if (registerButton != null)
            registerButton.onClick.AddListener(AttemptRegister);
        if (goToRegisterButton != null)
            goToRegisterButton.onClick.AddListener(ShowRegisterPanel);
        if (goToLoginButton != null)
            goToLoginButton.onClick.AddListener(ShowLoginPanel);
        if (togglePasswordButton != null)
            togglePasswordButton.onClick.AddListener(() => TogglePasswordVisibility(passwordField, togglePasswordButton));
        if (toggleRegisterPasswordButton != null)
            toggleRegisterPasswordButton.onClick.AddListener(() => TogglePasswordVisibility(registerPasswordField, toggleRegisterPasswordButton));
        if (toggleConfirmPasswordButton != null)
            toggleConfirmPasswordButton.onClick.AddListener(() => TogglePasswordVisibility(confirmPasswordField, toggleConfirmPasswordButton));
        if (sendResetButton != null)
            sendResetButton.onClick.AddListener(SendPasswordResetEmail);
        if (backToLoginFromForgotButton != null)
            backToLoginFromForgotButton.onClick.AddListener(HideForgotPasswordPanel);
    }

    // ─── Forgot Password ──────────────────────────────────────────────────────

    public void SendPasswordResetEmail()
    {
        string email = forgotEmailField?.text.Trim() ?? "";

        if (string.IsNullOrEmpty(email))
        {
            ShowForgotPasswordMessage("Please enter your email address", true);
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowForgotPasswordMessage("Please enter a valid email address", true);
            return;
        }

        if (sendResetButton != null)
        {
            var buttonText = sendResetButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "Sending...";
            sendResetButton.interactable = false;
        }

        StartCoroutine(RequestPasswordReset(email));
    }

    IEnumerator RequestPasswordReset(string email)
    {
        string url = serverUrl + "/api/forgot-password";
        string jsonData = $@"{{""email"": ""{email}""}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 30;

            yield return request.SendWebRequest();

            if (sendResetButton != null)
            {
                var buttonText = sendResetButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "Send Reset Link";
                sendResetButton.interactable = true;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                ShowForgotPasswordMessage("Check your email for password reset instructions", false);
                if (forgotEmailField != null) forgotEmailField.text = "";
                StartCoroutine(ReturnToLoginAfterDelay(3f));
            }
            else
            {
                if (request.error.Contains("timeout") || request.error.Contains("Timeout"))
                    ShowForgotPasswordMessage("Request timed out. Please check your internet connection and try again.", true);
                else if (request.responseCode == 500)
                    ShowForgotPasswordMessage("Server error. Please try again later.", true);
                else
                    ShowForgotPasswordMessage("Failed to send reset email. Please try again.", true);
            }
        }
    }

    IEnumerator ReturnToLoginAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideForgotPasswordPanel();
    }

    void ShowForgotPasswordMessage(string message, bool isError)
    {
        if (forgotPasswordMessageText != null)
        {
            forgotPasswordMessageText.text = message;
            forgotPasswordMessageText.color = isError ? Color.red : Color.green;
            forgotPasswordMessageText.gameObject.SetActive(true);
        }
        
        if (isError) Debug.LogWarning("Forgot Password: " + message);
        else Debug.Log("Forgot Password: " + message);
    }

    void SetupEnterKeySupport()
    {
        if (emailField != null)
            emailField.onSubmit.AddListener(delegate { AttemptLogin(); });
        if (passwordField != null)
            passwordField.onSubmit.AddListener(delegate { AttemptLogin(); });
        if (confirmPasswordField != null)
            confirmPasswordField.onSubmit.AddListener(delegate { AttemptRegister(); });
    }

    void SetupPasswordFields()
    {
        if (passwordField != null)
            passwordField.contentType = TMP_InputField.ContentType.Password;
        if (registerPasswordField != null)
            registerPasswordField.contentType = TMP_InputField.ContentType.Password;
        if (confirmPasswordField != null)
            confirmPasswordField.contentType = TMP_InputField.ContentType.Password;

        UpdatePasswordButtonIcon(togglePasswordButton, false);
        UpdatePasswordButtonIcon(toggleRegisterPasswordButton, false);
        UpdatePasswordButtonIcon(toggleConfirmPasswordButton, false);
    }

    void TogglePasswordVisibility(TMP_InputField inputField, Button toggleButton)
    {
        if (inputField == null) return;
        bool isVisible = inputField.contentType == TMP_InputField.ContentType.Standard;
        inputField.contentType = isVisible ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        inputField.ForceLabelUpdate();
        UpdatePasswordButtonIcon(toggleButton, !isVisible);
    }

    void UpdatePasswordButtonIcon(Button button, bool isVisible)
    {
        if (button == null) return;
        var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
            buttonText.text = isVisible ? "" : "";
    }

    // ─── Panel Navigation ─────────────────────────────────────────────────────

    public void ShowLoginPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(false);
        HideAllErrors();
    }

    public void ShowRegisterPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
        if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(false);
        HideAllErrors();
    }

    // ─── Login ────────────────────────────────────────────────────────────────

    void AttemptLogin()
    {
        string emailOrUsername = emailField?.text.Trim() ?? "";
        string password = passwordField?.text ?? "";

        if (string.IsNullOrEmpty(emailOrUsername)) { ShowLoginError("Please enter your email or username"); return; }
        if (string.IsNullOrEmpty(password)) { ShowLoginError("Please enter your password"); return; }
        if (string.IsNullOrEmpty(serverUrl)) { ShowLoginError("Server URL not configured!"); return; }

        if (loginButton != null)
        {
            var buttonText = loginButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "Logging in...";
            loginButton.interactable = false;
        }

        StartCoroutine(CloudLogin(emailOrUsername, password));
    }

    IEnumerator CloudLogin(string emailOrUsername, string password)
    {
        string url = serverUrl + "/api/login";
        string jsonData = $@"{{""loginIdentifier"": ""{emailOrUsername}"",""password"": ""{password}""}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (loginButton != null)
            {
                var buttonText = loginButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "Log In";
                loginButton.interactable = true;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string rawResponse = request.downloadHandler.text;
                Debug.Log("🔍 RAW LOGIN RESPONSE: " + rawResponse);
                
                var response = JsonUtility.FromJson<LoginResponse>(rawResponse);
                
                if (response.success)
                {
                    string username = response.user.username;
                    string userEmail = response.user.email;
                    string userName = response.user.name;
                    
                    bool hasValidEmail = !string.IsNullOrEmpty(userEmail) && 
                                        userEmail != username && 
                                        userEmail.Contains("@") && 
                                        !userEmail.Contains("@example.com");
                    
                    if (!hasValidEmail) userEmail = "";
                    
                    PlayerPrefs.DeleteKey(CURRENT_USER_KEY);
                    PlayerPrefs.SetString(CURRENT_USER_KEY, username);
                    PlayerPrefs.SetString("User_" + username + "_Name", userName);
                    PlayerPrefs.SetString("User_" + username + "_Email", userEmail);
                    PlayerPrefs.SetInt("User_" + username + "_Streak", response.user.streak);
                    PlayerPrefs.SetInt("User_" + username + "_CompletedTopics", response.user.completedTopics);
                    PlayerPrefs.Save();

                    if (UserProgressManager.Instance != null)
                        UserProgressManager.Instance.InitializeForUser(username);

                    if (enableDebugMode)
                        Debug.Log("✅ Login successful: " + username);

                    SceneManager.LoadScene(mainMenuSceneName);
                }
                else
                {
                    ShowLoginError("Login failed. Please try again.");
                }
            }
            else
            {
                string errorMsg = request.downloadHandler.text;
                if (errorMsg.Contains("User not found"))
                    ShowLoginError("Account not found. Please register first.");
                else if (errorMsg.Contains("Incorrect password"))
                    ShowLoginError("Incorrect password. Please try again.");
                else
                    ShowLoginError("Login failed. Check your internet connection.");
                
                if (enableDebugMode)
                    Debug.LogWarning("Login error: " + request.error);
            }
        }
    }

    void ClearAllUserData()
    {
        Debug.Log("🧹 Clearing session data only");
        PlayerPrefs.DeleteKey(CURRENT_USER_KEY);
        PlayerPrefs.Save();
    }

    void ClearUserSpecificData(string username)
    {
        if (string.IsNullOrEmpty(username)) return;
        
        PlayerPrefs.DeleteKey("User_" + username + "_Name");
        PlayerPrefs.DeleteKey("User_" + username + "_Email");
        PlayerPrefs.DeleteKey("User_" + username + "_Password");
        PlayerPrefs.DeleteKey("User_" + username + "_Streak");
        PlayerPrefs.DeleteKey("User_" + username + "_CompletedTopics");
        PlayerPrefs.DeleteKey("User_" + username + "_LastActivity");
        PlayerPrefs.DeleteKey("User_" + username + "_Username");
        PlayerPrefs.DeleteKey($"User_{username}_ProfilePic");
        PlayerPrefs.DeleteKey($"ProfilePic_{username}");
        PlayerPrefs.DeleteKey($"UserProgressInitialized_{username}");
        PlayerPrefs.DeleteKey($"User_{username}_LastTopic");
        PlayerPrefs.DeleteKey($"CompletedLessons_{username}");
        
        string[] allTopics = { 
            "Arrays", "Array", "Queue", "Queues", "Stacks", "Stack", 
            "LinkedLists", "Linked Lists", "LinkedList", "Linked List",
            "Trees", "Tree", "Graphs", "Graph",
            "Hashmaps", "Hashmap", "Hash Maps", "Hash Map",
            "Heaps", "Heap", "Deque", "Deques",
            "BinaryHeaps", "Binary Heaps", "BinaryHeap", "Binary Heap"
        };
        
        foreach (string topic in allTopics)
        {
            PlayerPrefs.DeleteKey($"{username}_{topic}_Tutorial");
            PlayerPrefs.DeleteKey($"{username}_{topic}_Puzzle");
            PlayerPrefs.DeleteKey($"{username}_{topic}_Score");
            PlayerPrefs.DeleteKey($"{username}_{topic}_TimeSpent");
            PlayerPrefs.DeleteKey($"{username}_{topic}_LastAccessed");
            PlayerPrefs.DeleteKey($"{username}_{topic}_LessonsCompleted");
            PlayerPrefs.DeleteKey($"User_{username}_{topic}_TutorialCompleted");
            PlayerPrefs.DeleteKey($"User_{username}_{topic}_PuzzleCompleted");
            PlayerPrefs.DeleteKey($"User_{username}_{topic}_Score");
            PlayerPrefs.DeleteKey($"TopicReadComplete_{username}_{topic}");
            PlayerPrefs.DeleteKey($"User_{username}_{topic}_easy_Score");
            PlayerPrefs.DeleteKey($"User_{username}_{topic}_medium_Score");
            PlayerPrefs.DeleteKey($"User_{username}_{topic}_hard_Score");
            PlayerPrefs.DeleteKey($"User_{username}_{topic}_mixed_Score");
            PlayerPrefs.DeleteKey($"CompletedLessons_{username}_{topic}");
        }
        
        PlayerPrefs.Save();
        Debug.Log($"✅ DEEP CLEAN complete for: {username}");
    }

    void ShowLoginError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.red;
            errorText.gameObject.SetActive(true);
        }
        Debug.LogWarning("Login Error: " + message);
    }

    void OnForgotPassword()
    {
        if (forgotPasswordPanel != null)
            ShowForgotPasswordPanel();
        else
            ShowLoginError("Forgot password feature requires setup. Please contact support.");
    }

    public void ShowForgotPasswordPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (forgotPasswordPanel != null)
        {
            forgotPasswordPanel.SetActive(true);
            if (forgotEmailField != null) forgotEmailField.text = "";
        }
        HideAllErrors();
    }

    public void HideForgotPasswordPanel()
    {
        if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(false);
        ShowLoginPanel();
    }

    // ─── Registration ─────────────────────────────────────────────────────────

    void AttemptRegister()
    {
        string name = registerNameField?.text.Trim() ?? "";
        string username = registerUsernameField?.text.Trim() ?? "";
        string email = registerEmailField?.text.Trim() ?? "";
        string password = registerPasswordField?.text ?? "";
        string confirmPassword = confirmPasswordField?.text ?? "";

        if (string.IsNullOrEmpty(name)) { ShowRegisterError("Please enter your full name"); return; }
        if (string.IsNullOrEmpty(username) || username.Length < 3) { ShowRegisterError("Username must be at least 3 characters"); return; }
        if (!System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-zA-Z0-9_]+$")) { ShowRegisterError("Username: letters, numbers, and underscores only"); return; }
        if (string.IsNullOrEmpty(email) || !IsValidEmail(email)) { ShowRegisterError("Please enter a valid email address"); return; }
        if (string.IsNullOrEmpty(password) || password.Length < minimumPasswordLength) { ShowRegisterError($"Password must be at least {minimumPasswordLength} characters"); return; }
        if (password != confirmPassword) { ShowRegisterError("Passwords do not match"); return; }
        if (string.IsNullOrEmpty(serverUrl)) { ShowRegisterError("Server URL not configured!"); return; }

        if (registerButton != null)
        {
            var buttonText = registerButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = "Creating...";
            registerButton.interactable = false;
        }

        StartCoroutine(CloudRegister(username, name, email, password));
    }

    IEnumerator CloudRegister(string username, string name, string email, string password)
    {
        string url = serverUrl + "/api/users";
        string registrationDate = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        
        string selectedInstructor = null;
        if (instructorDropdown != null && instructorDropdown.value > 0)
        {
            int instructorIndex = instructorDropdown.value - 1;
            selectedInstructor = availableInstructors[instructorIndex].username;
        }
        
        // Use the selectedDifficulty set by the buttons
        string difficultyLevel = selectedDifficulty;
        
        string jsonData = $@"{{
            ""username"": ""{username}"",
            ""name"": ""{name}"",
            ""email"": ""{email}"",
            ""password"": ""{password}"",
            ""instructor"": {(selectedInstructor != null ? $"\"{selectedInstructor}\"" : "null")},
            ""difficultyLevel"": ""{difficultyLevel}"",
            ""registerDate"": ""{registrationDate}"",
            ""lastLogin"": ""{registrationDate}"",
            ""streak"": 0,
            ""completedTopics"": 0,
            ""progress"": {{}}
        }}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (registerButton != null)
            {
                var buttonText = registerButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "Create Account";
                registerButton.interactable = true;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (enableDebugMode)
                    Debug.Log($"✅ Registration successful: {username} (Level: {difficultyLevel}, Instructor: {selectedInstructor ?? "None"})");

                ClearUserSpecificData(username);
                
                PlayerPrefs.SetString($"User_{username}_DifficultyLevel", difficultyLevel);
                PlayerPrefs.Save();
                
                if (UserProgressManager.Instance != null)
                    UserProgressManager.Instance.ClearUserData(username);

                int tempAvatarIndex = PlayerPrefs.GetInt("TempProfilePicIndex", -1);
                if (tempAvatarIndex >= 0)
                {
                    PlayerPrefs.SetInt($"ProfilePic_{username}", tempAvatarIndex);
                    PlayerPrefs.DeleteKey("TempProfilePicIndex");
                    PlayerPrefs.Save();
                }

                StartCoroutine(ShowRegistrationSuccess(email));
            }
            else
            {
                string errorMsg = request.downloadHandler.text;
                if (errorMsg.Contains("already exists"))
                {
                    if (errorMsg.Contains("username"))
                        ShowRegisterError("Username already taken");
                    else
                        ShowRegisterError("Email already registered");
                }
                else
                {
                    ShowRegisterError("Registration failed. Check internet connection.");
                }
                
                if (enableDebugMode)
                    Debug.LogWarning("Registration error: " + request.error);
            }
        }
    }

    IEnumerator FetchUserDifficultyLevel(string username)
    {
        string url = $"{serverUrl}/api/users/{username}/difficulty";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 5;
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<DifficultyLevelResponse>(request.downloadHandler.text);
                    if (response.success)
                    {
                        PlayerPrefs.SetString($"User_{username}_DifficultyLevel", response.difficultyLevel);
                        PlayerPrefs.Save();
                        Debug.Log($"✅ Difficulty level fetched: {response.difficultyLevel}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"⚠️ Failed to parse difficulty level: {e.Message}");
                    PlayerPrefs.SetString($"User_{username}_DifficultyLevel", "beginner");
                    PlayerPrefs.Save();
                }
            }
            else
            {
                PlayerPrefs.SetString($"User_{username}_DifficultyLevel", "beginner");
                PlayerPrefs.Save();
            }
        }
    }

    IEnumerator ShowRegistrationSuccess(string email)
    {
        if (registerErrorText != null)
        {
            registerErrorText.color = Color.green;
            registerErrorText.text = "Account created successfully!";
            registerErrorText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(1.5f);

        if (registerNameField != null) registerNameField.text = "";
        if (registerUsernameField != null) registerUsernameField.text = "";
        if (registerEmailField != null) registerEmailField.text = "";
        if (registerPasswordField != null) registerPasswordField.text = "";
        if (confirmPasswordField != null) confirmPasswordField.text = "";

        // Reset difficulty back to beginner for next registration
        selectedDifficulty = "beginner";
        UpdateDifficultyButtonVisuals();

        ShowLoginPanel();

        if (registerErrorText != null)
            registerErrorText.color = Color.red;
    }

    void ShowRegisterError(string message)
    {
        if (registerErrorText != null)
        {
            registerErrorText.text = message;
            registerErrorText.color = Color.red;
            registerErrorText.gameObject.SetActive(true);
        }
        Debug.LogWarning("Registration Error: " + message);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email && email.Contains("@") && email.Contains(".");
        }
        catch { return false; }
    }

    // ─── JSON Classes ─────────────────────────────────────────────────────────

    [System.Serializable]
    public class LoginResponse
    {
        public bool success;
        public UserData user;
    }

    [System.Serializable]
    public class UserData
    {
        public string username;
        public string name;
        public string email;
        public int streak;
        public int completedTopics;
    }

    [System.Serializable]
    public class ForgotPasswordResponse
    {
        public bool success;
        public string message;
        public string error;
    }

    [System.Serializable]
    public class ResetPasswordResponse
    {
        public bool success;
        public string message;
        public string error;
    }

    [System.Serializable]
    public class DifficultyLevelResponse
    {
        public bool success;
        public string difficultyLevel;
        public string error;
    }

    [System.Serializable]
    public class InstructorData
    {
        public string username;
        public string name;
        public string email;
    }

    [System.Serializable]
    public class InstructorsResponse
    {
        public bool success;
        public int count;
        public List<InstructorData> instructors;
    }
}