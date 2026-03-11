using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Text.RegularExpressions;

public class AuthenticationManager : MonoBehaviour
{
    [Header("Login Screen")]
    public GameObject loginScreen;
    public TMP_InputField loginEmail;
    public TMP_InputField loginPassword;
    public Button loginButton;
    public Button goToRegisterButton;
    public Button googleSignInButton;
    public TextMeshProUGUI loginErrorText;
    public Toggle rememberMeToggle;
    
    [Header("Register Screen")]
    public GameObject registerScreen;
    public TMP_InputField registerName;
    public TMP_InputField registerEmail;
    public TMP_InputField registerPassword;
    public TMP_InputField registerConfirmPassword;
    public Button registerButton;
    public Button goToLoginButton;
    public TextMeshProUGUI registerErrorText;
    
    [Header("Profile Picture Selection")]
    public GameObject profilePictureScreen;
    public Button[] profilePictureOptions; // Assign 6-8 profile picture buttons
    public Image selectedProfilePreview;
    public Button confirmProfileButton;
    public Button skipProfileButton;
    public Sprite[] profilePictureSprites; // Your profile picture images
    
    [Header("Loading")]
    public GameObject loadingPanel;
    public TextMeshProUGUI loadingText;
    
    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";
    
    private int selectedProfilePictureIndex = 0;
    private string currentUserEmail = "";
    private string currentUserName = "";
    
    void Start()
    {
        InitializeUI();
        CheckRememberedUser();
    }
    
    void InitializeUI()
    {
        // Login buttons
        if (loginButton != null) 
            loginButton.onClick.AddListener(OnLoginButtonClicked);
        if (goToRegisterButton != null) 
            goToRegisterButton.onClick.AddListener(ShowRegisterScreen);
        if (googleSignInButton != null) 
            googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);
        
        // Register buttons
        if (registerButton != null) 
            registerButton.onClick.AddListener(OnRegisterButtonClicked);
        if (goToLoginButton != null) 
            goToLoginButton.onClick.AddListener(ShowLoginScreen);
        
        // Profile picture buttons
        for (int i = 0; i < profilePictureOptions.Length; i++)
        {
            int index = i; // Capture for closure
            if (profilePictureOptions[i] != null)
            {
                profilePictureOptions[i].onClick.AddListener(() => SelectProfilePicture(index));
            }
        }
        
        if (confirmProfileButton != null) 
            confirmProfileButton.onClick.AddListener(ConfirmProfilePicture);
        if (skipProfileButton != null) 
            skipProfileButton.onClick.AddListener(SkipProfilePicture);
        
        // Start with login screen
        ShowLoginScreen();
        HideError();
    }
    
    void CheckRememberedUser()
    {
        if (PlayerPrefs.HasKey("RememberMe") && PlayerPrefs.GetInt("RememberMe") == 1)
        {
            string savedEmail = PlayerPrefs.GetString("SavedEmail", "");
            if (!string.IsNullOrEmpty(savedEmail))
            {
                if (loginEmail != null) loginEmail.text = savedEmail;
                if (rememberMeToggle != null) rememberMeToggle.isOn = true;
            }
        }
    }
    
    // ===== SCREEN NAVIGATION =====
    void ShowLoginScreen()
    {
        HideAllScreens();
        if (loginScreen != null) loginScreen.SetActive(true);
        HideError();
    }
    
    void ShowRegisterScreen()
    {
        HideAllScreens();
        if (registerScreen != null) registerScreen.SetActive(true);
        HideError();
    }
    
    void ShowProfilePictureScreen()
    {
        HideAllScreens();
        if (profilePictureScreen != null) profilePictureScreen.SetActive(true);
        SelectProfilePicture(0); // Select first option by default
    }
    
    void HideAllScreens()
    {
        if (loginScreen != null) loginScreen.SetActive(false);
        if (registerScreen != null) registerScreen.SetActive(false);
        if (profilePictureScreen != null) profilePictureScreen.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }
    
    // ===== LOGIN =====
    void OnLoginButtonClicked()
    {
        string email = loginEmail != null ? loginEmail.text.Trim() : "";
        string password = loginPassword != null ? loginPassword.text : "";
        
        // Validate
        if (string.IsNullOrEmpty(email))
        {
            ShowLoginError("Please enter your email");
            return;
        }
        
        if (!IsValidEmail(email))
        {
            ShowLoginError("Please enter a valid email address");
            return;
        }
        
        if (string.IsNullOrEmpty(password))
        {
            ShowLoginError("Please enter your password");
            return;
        }
        
        // Check credentials
        if (PlayerPrefs.HasKey("User_" + email))
        {
            string savedPassword = PlayerPrefs.GetString("User_" + email + "_Password");
            
            if (password == savedPassword)
            {
                // Success!
                currentUserEmail = email;
                currentUserName = PlayerPrefs.GetString("User_" + email + "_Name", "Student");
                
                // Remember me
                if (rememberMeToggle != null && rememberMeToggle.isOn)
                {
                    PlayerPrefs.SetInt("RememberMe", 1);
                    PlayerPrefs.SetString("SavedEmail", email);
                }
                else
                {
                    PlayerPrefs.SetInt("RememberMe", 0);
                    PlayerPrefs.DeleteKey("SavedEmail");
                }
                
                // Set current user
                PlayerPrefs.SetString("CurrentUser", email);
                PlayerPrefs.Save();
                
                LoginSuccess();
            }
            else
            {
                ShowLoginError("Incorrect password");
            }
        }
        else
        {
            ShowLoginError("No account found with this email");
        }
    }
    
    void OnGoogleSignInClicked()
    {
        ShowLoadingScreen("Connecting to Google...");
        
        // IMPORTANT: For real Google Sign-In, you'll need to:
        // 1. Install Google Sign-In Unity Plugin
        // 2. Set up Google Cloud Console
        // 3. Implement actual Google authentication
        
        // For now, we'll simulate it (remove this in production)
        StartCoroutine(SimulateGoogleSignIn());
    }
    
    System.Collections.IEnumerator SimulateGoogleSignIn()
    {
        yield return new WaitForSeconds(1.5f);
        
        // Simulate successful Google sign-in
        currentUserEmail = "user@gmail.com";
        currentUserName = "Google User";
        
        // Check if user exists
        if (!PlayerPrefs.HasKey("User_" + currentUserEmail))
        {
            // New Google user - show profile picture selection
            PlayerPrefs.SetString("User_" + currentUserEmail + "_Name", currentUserName);
            PlayerPrefs.SetString("User_" + currentUserEmail + "_Password", "google_auth");
            PlayerPrefs.SetString("User_" + currentUserEmail + "_AuthType", "Google");
            PlayerPrefs.SetString("CurrentUser", currentUserEmail);
            PlayerPrefs.Save();
            
            ShowProfilePictureScreen();
        }
        else
        {
            // Existing Google user
            PlayerPrefs.SetString("CurrentUser", currentUserEmail);
            PlayerPrefs.Save();
            LoginSuccess();
        }
    }
    
    // ===== REGISTER =====
    void OnRegisterButtonClicked()
    {
        string name = registerName != null ? registerName.text.Trim() : "";
        string email = registerEmail != null ? registerEmail.text.Trim() : "";
        string password = registerPassword != null ? registerPassword.text : "";
        string confirmPassword = registerConfirmPassword != null ? registerConfirmPassword.text : "";
        
        // Validate
        if (string.IsNullOrEmpty(name))
        {
            ShowRegisterError("Please enter your name");
            return;
        }
        
        if (name.Length < 2)
        {
            ShowRegisterError("Name must be at least 2 characters");
            return;
        }
        
        if (string.IsNullOrEmpty(email))
        {
            ShowRegisterError("Please enter your email");
            return;
        }
        
        if (!IsValidEmail(email))
        {
            ShowRegisterError("Please enter a valid email address");
            return;
        }
        
        if (string.IsNullOrEmpty(password))
        {
            ShowRegisterError("Please enter a password");
            return;
        }
        
        if (password.Length < 6)
        {
            ShowRegisterError("Password must be at least 6 characters");
            return;
        }
        
        if (password != confirmPassword)
        {
            ShowRegisterError("Passwords do not match");
            return;
        }
        
        // Check if user already exists
        if (PlayerPrefs.HasKey("User_" + email))
        {
            ShowRegisterError("An account with this email already exists");
            return;
        }
        
        // Register user
        PlayerPrefs.SetString("User_" + email + "_Name", name);
        PlayerPrefs.SetString("User_" + email + "_Password", password);
        PlayerPrefs.SetString("User_" + email + "_AuthType", "Email");
        PlayerPrefs.SetString("CurrentUser", email);
        PlayerPrefs.Save();
        
        currentUserEmail = email;
        currentUserName = name;
        
        // Show profile picture selection
        ShowProfilePictureScreen();
    }
    
    // ===== PROFILE PICTURE SELECTION =====
    void SelectProfilePicture(int index)
    {
        selectedProfilePictureIndex = index;
        
        // Update preview
        if (selectedProfilePreview != null && profilePictureSprites != null && 
            index < profilePictureSprites.Length)
        {
            selectedProfilePreview.sprite = profilePictureSprites[index];
        }
        
        // Visual feedback on buttons (optional - add highlight effect)
        for (int i = 0; i < profilePictureOptions.Length; i++)
        {
            if (profilePictureOptions[i] != null)
            {
                // You can add a border or scale effect here
                Transform border = profilePictureOptions[i].transform.Find("SelectedBorder");
                if (border != null)
                {
                    border.gameObject.SetActive(i == index);
                }
            }
        }
    }
    
    void ConfirmProfilePicture()
    {
        // Save profile picture choice
        PlayerPrefs.SetInt("User_" + currentUserEmail + "_ProfilePic", selectedProfilePictureIndex);
        PlayerPrefs.Save();
        
        LoginSuccess();
    }
    
    void SkipProfilePicture()
    {
        // Use default profile picture (index 0)
        PlayerPrefs.SetInt("User_" + currentUserEmail + "_ProfilePic", 0);
        PlayerPrefs.Save();
        
        LoginSuccess();
    }
    
    // ===== SUCCESS =====
    void LoginSuccess()
    {
        ShowLoadingScreen("Loading your workspace...");
        
        // Load main menu after short delay
        StartCoroutine(LoadMainMenu());
    }
    
    System.Collections.IEnumerator LoadMainMenu()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    // ===== UTILITIES =====
    bool IsValidEmail(string email)
    {
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }
    
    void ShowLoginError(string message)
    {
        if (loginErrorText != null)
        {
            loginErrorText.text = message;
            loginErrorText.gameObject.SetActive(true);
        }
    }
    
    void ShowRegisterError(string message)
    {
        if (registerErrorText != null)
        {
            registerErrorText.text = message;
            registerErrorText.gameObject.SetActive(true);
        }
    }
    
    void HideError()
    {
        if (loginErrorText != null) loginErrorText.gameObject.SetActive(false);
        if (registerErrorText != null) registerErrorText.gameObject.SetActive(false);
    }
    
    void ShowLoadingScreen(string message)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (loadingText != null) loadingText.text = message;
        }
    }
}