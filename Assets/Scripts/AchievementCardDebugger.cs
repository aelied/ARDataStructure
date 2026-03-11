using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Reflection;

/// <summary>
/// Attach this to your ProgressPanel to debug achievement cards
/// This will show you exactly what's missing and what needs to be fixed
/// </summary>
public class AchievementCardDebugger : MonoBehaviour
{
    [Header("Drag ProgressPanelController here")]
    public ProgressPanelController progressController;
    
    [Header("Debug Options")]
    public bool runOnStart = true;
    public bool showHierarchy = true;
    
    void Start()
    {
        if (runOnStart)
        {
            Invoke(nameof(RunFullDiagnostic), 1f); // Wait 1 second for everything to load
        }
    }
    
    [ContextMenu("Run Full Diagnostic")]
    public void RunFullDiagnostic()
    {
        Debug.Log("╔════════════════════════════════════════╗");
        Debug.Log("║   ACHIEVEMENT CARD DIAGNOSTIC TOOL    ║");
        Debug.Log("╚════════════════════════════════════════╝");
        
        if (progressController == null)
        {
            Debug.LogError("❌ ProgressPanelController is not assigned!");
            return;
        }
        
        // Check achievement cards array using reflection
        var cardsField = progressController.GetType().GetField("achievementCards", 
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        
        if (cardsField == null)
        {
            Debug.LogError("❌ Cannot find achievementCards field!");
            return;
        }
        
        var cards = cardsField.GetValue(progressController) as System.Collections.IList;
        
        if (cards == null || cards.Count == 0)
        {
            Debug.LogError("❌ Achievement cards list is empty or null!");
            Debug.Log("→ Fix: Add 5 cards in the Inspector (Stacks, Queue, LinkedLists, Trees, Graphs)");
            return;
        }
        
        Debug.Log($"✓ Found {cards.Count} achievement cards\n");
        
        // Check cached progress data
        var cachedDataField = progressController.GetType().GetField("cachedProgressData", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (cachedDataField != null)
        {
            var cachedData = cachedDataField.GetValue(progressController) as DatabaseProgressData;
            if (cachedData == null)
            {
                Debug.LogWarning("⚠️  No cached progress data! Cards won't update until data is loaded.");
            }
            else
            {
                Debug.Log($"✓ Progress data loaded for user: {cachedData.username}");
                Debug.Log($"  Topics in database: {cachedData.topics.Count}");
                foreach (var topic in cachedData.topics)
                {
                    Debug.Log($"    - {topic.topicName}: Tutorial={topic.tutorialCompleted}, Puzzle={topic.puzzleCompleted}, Progress={topic.progressPercentage:F1}%");
                }
                Debug.Log("");
            }
        }
        
        // Check each card
        int cardIndex = 0;
        foreach (var cardObj in cards)
        {
            cardIndex++;
            Debug.Log($"═══ CARD {cardIndex} ═══");
            
            if (cardObj == null)
            {
                Debug.LogError($"❌ Card {cardIndex} is NULL!");
                continue;
            }
            
            // Use reflection to get card properties
            var cardType = cardObj.GetType();
            var topicNameField = cardType.GetField("topicName");
            var displayTitleField = cardType.GetField("displayTitle");
            var titleTextField = cardType.GetField("titleText");
            var trophyField = cardType.GetField("completedBadge");
            var lockField = cardType.GetField("lockIcon");
            var progressField = cardType.GetField("progressBar");
            
            string topicName = topicNameField?.GetValue(cardObj) as string;
            string displayTitle = displayTitleField?.GetValue(cardObj) as string;
            var titleText = titleTextField?.GetValue(cardObj) as TextMeshProUGUI;
            var trophy = trophyField?.GetValue(cardObj) as GameObject;
            var lockIcon = lockField?.GetValue(cardObj) as GameObject;
            var progressBar = progressField?.GetValue(cardObj) as Image;
            
            // Check topic name
            if (string.IsNullOrEmpty(topicName))
            {
                Debug.LogWarning($"⚠️  Topic Name: NOT SET");
                Debug.Log($"   → Fix: Set to 'Stacks', 'Queue', 'LinkedLists', 'Trees', or 'Graphs'");
            }
            else
            {
                Debug.Log($"✓ Topic Name: '{topicName}'");
            }
            
            // Check display title
            if (string.IsNullOrEmpty(displayTitle))
            {
                Debug.LogWarning($"⚠️  Display Title: NOT SET");
            }
            else
            {
                Debug.Log($"✓ Display Title: '{displayTitle}'");
            }
            
            // Check title text
            if (titleText == null)
            {
                Debug.LogError($"❌ Title Text: NOT ASSIGNED");
                Debug.Log($"   → Fix: Drag the TextMeshProUGUI component from your card");
            }
            else
            {
                Debug.Log($"✓ Title Text: Assigned ('{titleText.name}')");
            }
            
            // Check trophy (completedBadge)
            if (trophy == null)
            {
                Debug.LogError($"❌ TROPHY (Completed Badge): NOT ASSIGNED ← THIS IS WHY IT DOESN'T WORK!");
                Debug.Log($"   → Fix: Drag the trophy icon GameObject here");
                Debug.Log($"   → It should be a child GameObject with an Image component");
            }
            else
            {
                Debug.Log($"✓ Trophy: Assigned ('{trophy.name}')");
                Debug.Log($"   Current State: {(trophy.activeSelf ? "VISIBLE ✓" : "HIDDEN")}");
                
                // Check if it has an Image component
                var img = trophy.GetComponent<Image>();
                if (img == null)
                {
                    Debug.LogWarning($"   ⚠️  Trophy has no Image component!");
                }
                else
                {
                    Debug.Log($"   ✓ Has Image component (Sprite: {(img.sprite != null ? img.sprite.name : "NULL")})");
                }
            }
            
            // Check lock
            if (lockIcon == null)
            {
                Debug.LogError($"❌ LOCK ICON: NOT ASSIGNED ← THIS IS WHY IT DOESN'T WORK!");
                Debug.Log($"   → Fix: Drag the lock icon GameObject here");
                Debug.Log($"   → It should be a child GameObject with an Image component");
            }
            else
            {
                Debug.Log($"✓ Lock: Assigned ('{lockIcon.name}')");
                Debug.Log($"   Current State: {(lockIcon.activeSelf ? "VISIBLE (Locked) 🔒" : "HIDDEN (Unlocked) ✓")}");
                
                // Check if it has an Image component
                var img = lockIcon.GetComponent<Image>();
                if (img == null)
                {
                    Debug.LogWarning($"   ⚠️  Lock has no Image component!");
                }
                else
                {
                    Debug.Log($"   ✓ Has Image component (Sprite: {(img.sprite != null ? img.sprite.name : "NULL")})");
                }
            }
            
            // Check progress bar
            if (progressBar == null)
            {
                Debug.LogWarning($"⚠️  Progress Bar: NOT ASSIGNED");
            }
            else
            {
                Debug.Log($"✓ Progress Bar: Assigned (Fill: {progressBar.fillAmount:P0})");
            }
            
            Debug.Log(""); // Empty line between cards
        }
        
        // Show hierarchy if requested
        if (showHierarchy)
        {
            Debug.Log("\n╔════════════════════════════════════════╗");
            Debug.Log("║          HIERARCHY SEARCH             ║");
            Debug.Log("╚════════════════════════════════════════╝");
            Debug.Log("Looking for trophy and lock icons in scene...\n");
            
            // Find all GameObjects with common trophy/lock names
            string[] trophyNames = { "Trophy", "TrophyIcon", "CompletedBadge", "Badge", "Star", "Achievement" };
            string[] lockNames = { "Lock", "LockIcon", "LockedIcon", "Padlock" };
            
            Debug.Log("🏆 TROPHY CANDIDATES:");
            FindObjectsByNames(trophyNames);
            
            Debug.Log("\n🔒 LOCK CANDIDATES:");
            FindObjectsByNames(lockNames);
        }
        
        Debug.Log("\n╔════════════════════════════════════════╗");
        Debug.Log("║         DIAGNOSTIC COMPLETE           ║");
        Debug.Log("╚════════════════════════════════════════╝");
    }
    
    void FindObjectsByNames(string[] searchNames)
    {
        var allObjects = FindObjectsOfType<GameObject>(true); // Include inactive
        bool foundAny = false;
        
        foreach (var name in searchNames)
        {
            foreach (var obj in allObjects)
            {
                if (obj.name.ToLower().Contains(name.ToLower()))
                {
                    foundAny = true;
                    Debug.Log($"  → Found: '{obj.name}' at path: {GetGameObjectPath(obj)}");
                    
                    var img = obj.GetComponent<Image>();
                    if (img != null)
                    {
                        Debug.Log($"     ✓ Has Image (Sprite: {(img.sprite != null ? img.sprite.name : "NULL")})");
                    }
                }
            }
        }
        
        if (!foundAny)
        {
            Debug.LogWarning("  ⚠️  No matching GameObjects found!");
        }
    }
    
    string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    [ContextMenu("Force Show All Trophies")]
    public void ForceShowAllTrophies()
    {
        Debug.Log("🏆 Forcing all trophies to show...");
        
        var cardsField = progressController.GetType().GetField("achievementCards", 
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        var cards = cardsField.GetValue(progressController) as System.Collections.IList;
        
        foreach (var cardObj in cards)
        {
            var cardType = cardObj.GetType();
            var trophyField = cardType.GetField("completedBadge");
            var topicField = cardType.GetField("topicName");
            
            var trophy = trophyField?.GetValue(cardObj) as GameObject;
            var topicName = topicField?.GetValue(cardObj) as string;
            
            if (trophy != null)
            {
                trophy.SetActive(true);
                Debug.Log($"✓ Showed trophy for {topicName}");
            }
        }
    }
    
    [ContextMenu("Force Hide All Locks")]
    public void ForceHideAllLocks()
    {
        Debug.Log("🔓 Forcing all locks to hide...");
        
        var cardsField = progressController.GetType().GetField("achievementCards", 
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        var cards = cardsField.GetValue(progressController) as System.Collections.IList;
        
        foreach (var cardObj in cards)
        {
            var cardType = cardObj.GetType();
            var lockField = cardType.GetField("lockIcon");
            var topicField = cardType.GetField("topicName");
            
            var lockIcon = lockField?.GetValue(cardObj) as GameObject;
            var topicName = topicField?.GetValue(cardObj) as string;
            
            if (lockIcon != null)
            {
                lockIcon.SetActive(false);
                Debug.Log($"✓ Hid lock for {topicName}");
            }
        }
    }
}