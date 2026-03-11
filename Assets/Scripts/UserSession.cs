using UnityEngine;

/// <summary>
/// Singleton class to manage user session data across scenes
/// </summary>
public class UserSession : MonoBehaviour
{
    private static UserSession _instance;
    public static UserSession Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("UserSession");
                _instance = go.AddComponent<UserSession>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    // User data
    public string CurrentUserEmail { get; private set; }
    public string UserName { get; private set; }
    public int ProfilePictureIndex { get; private set; }
    public bool IsLoggedIn { get; private set; }
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadSession();
    }
    
    /// <summary>
    /// Load session from PlayerPrefs
    /// </summary>
    public void LoadSession()
    {
        CurrentUserEmail = PlayerPrefs.GetString("CurrentUser", "");
        
        if (!string.IsNullOrEmpty(CurrentUserEmail))
        {
            UserName = PlayerPrefs.GetString("User_" + CurrentUserEmail + "_Name", "Student");
            ProfilePictureIndex = PlayerPrefs.GetInt("User_" + CurrentUserEmail + "_ProfilePic", 0);
            IsLoggedIn = true;
        }
        else
        {
            IsLoggedIn = false;
        }
    }
    
    /// <summary>
    /// Set current user session
    /// </summary>
    public void SetUser(string email, string name, int profilePic)
    {
        CurrentUserEmail = email;
        UserName = name;
        ProfilePictureIndex = profilePic;
        IsLoggedIn = true;
        
        PlayerPrefs.SetString("CurrentUser", email);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Update profile picture
    /// </summary>
    public void UpdateProfilePicture(int index)
    {
        ProfilePictureIndex = index;
        PlayerPrefs.SetInt("User_" + CurrentUserEmail + "_ProfilePic", index);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Get user progress data
    /// </summary>
    public int GetStreak()
    {
        return PlayerPrefs.GetInt("User_" + CurrentUserEmail + "_Streak", 0);
    }
    
    public void SetStreak(int streak)
    {
        PlayerPrefs.SetInt("User_" + CurrentUserEmail + "_Streak", streak);
        PlayerPrefs.Save();
    }
    
    public int GetCompletedTopics()
    {
        return PlayerPrefs.GetInt("User_" + CurrentUserEmail + "_CompletedTopics", 0);
    }
    
    public void SetCompletedTopics(int count)
    {
        PlayerPrefs.SetInt("User_" + CurrentUserEmail + "_CompletedTopics", count);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Logout current user
    /// </summary>
    public void Logout()
    {
        CurrentUserEmail = "";
        UserName = "";
        ProfilePictureIndex = 0;
        IsLoggedIn = false;
        
        PlayerPrefs.DeleteKey("CurrentUser");
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    public bool CheckAuthentication()
    {
        LoadSession();
        return IsLoggedIn;
    }
}