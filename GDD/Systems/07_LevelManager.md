# Level Manager Specification

## Overview
The Level Manager is a persistent singleton that tracks game progress, manages level data, and handles level transitions.

---

## Architecture

```
GameManager (Persistent)
    └── LevelManager
            ├── LevelData[] (ScriptableObjects)
            ├── Current Level Index
            ├── Level Progress Data
            └── Scene Loading Logic
```

---

## Level Data Structure

### LevelData ScriptableObject
```csharp
[CreateAssetMenu(fileName = "LevelData", menuName = "MaskCompany/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Basic Info")]
    public string levelName;
    public string sceneName;
    public int levelIndex;
    
    [Header("Display")]
    public Sprite levelThumbnail;
    [TextArea] public string description;
    public string objectiveText;
    
    [Header("Gameplay")]
    public MaskType[] availableMasks;
    public float parTime; // Optional, for rating
    
    [Header("Unlock")]
    public bool unlockedByDefault = false;
    public LevelData[] prerequisiteLevels;
    
    [Header("NPCs")]
    public NPCSpawnInfo[] npcSpawns;
}

[System.Serializable]
public class NPCSpawnInfo
{
    public NPCType npcType;
    public EmotionType emotion;
    public Vector2 position;
    public Vector2[] patrolPoints;
}
```

---

## Level Manager Implementation

### LevelManager.cs
```csharp
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [Header("Level Database")]
    public LevelData[] allLevels;
    
    [Header("Current State")]
    public int currentLevelIndex = 0;
    public LevelData CurrentLevel => GetLevel(currentLevelIndex);
    
    [Header("Progress")]
    public List<LevelProgress> levelProgress = new List<LevelProgress>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // ===== Level Access =====
    
    public LevelData GetLevel(int index)
    {
        if (index >= 0 && index < allLevels.Length)
        {
            return allLevels[index];
        }
        return null;
    }
    
    public LevelData GetLevelByName(string name)
    {
        return allLevels.FirstOrDefault(l => l.levelName == name);
    }
    
    public int GetLevelCount()
    {
        return allLevels.Length;
    }
    
    // ===== Level Loading =====
    
    public void LoadLevel(int index)
    {
        if (index < 0 || index >= allLevels.Length)
        {
            Debug.LogError($"Invalid level index: {index}");
            return;
        }
        
        currentLevelIndex = index;
        LevelData level = allLevels[index];
        
        SceneTransition.Instance.LoadScene(level.sceneName);
    }
    
    public void LoadLevel(LevelData level)
    {
        int index = System.Array.IndexOf(allLevels, level);
        if (index >= 0)
        {
            LoadLevel(index);
        }
    }
    
    public void LoadNextLevel()
    {
        if (currentLevelIndex + 1 < allLevels.Length)
        {
            LoadLevel(currentLevelIndex + 1);
        }
        else
        {
            // Game complete - return to menu or show credits
            LoadMainMenu();
        }
    }
    
    public void ReloadCurrentLevel()
    {
        LoadLevel(currentLevelIndex);
    }
    
    public void LoadMainMenu()
    {
        SceneTransition.Instance.LoadScene("MainMenu");
    }
    
    // ===== Progress Tracking =====
    
    public void CompleteLevel(float completionTime)
    {
        LevelProgress progress = GetOrCreateProgress(currentLevelIndex);
        progress.completed = true;
        progress.attempts++;
        
        if (progress.bestTime <= 0 || completionTime < progress.bestTime)
        {
            progress.bestTime = completionTime;
        }
        
        // Unlock next level
        if (currentLevelIndex + 1 < allLevels.Length)
        {
            LevelProgress nextProgress = GetOrCreateProgress(currentLevelIndex + 1);
            nextProgress.unlocked = true;
        }
        
        SaveProgress();
    }
    
    public bool IsLevelUnlocked(int index)
    {
        if (index < 0 || index >= allLevels.Length) return false;
        
        // First level always unlocked
        if (index == 0) return true;
        
        // Check if unlocked by default
        if (allLevels[index].unlockedByDefault) return true;
        
        // Check progress
        LevelProgress progress = GetProgress(index);
        return progress?.unlocked ?? false;
    }
    
    public bool IsLevelCompleted(int index)
    {
        LevelProgress progress = GetProgress(index);
        return progress?.completed ?? false;
    }
    
    LevelProgress GetProgress(int levelIndex)
    {
        return levelProgress.FirstOrDefault(p => p.levelIndex == levelIndex);
    }
    
    LevelProgress GetOrCreateProgress(int levelIndex)
    {
        LevelProgress progress = GetProgress(levelIndex);
        if (progress == null)
        {
            progress = new LevelProgress { levelIndex = levelIndex };
            levelProgress.Add(progress);
        }
        return progress;
    }
    
    // ===== Save/Load =====
    
    void SaveProgress()
    {
        string json = JsonUtility.ToJson(new ProgressWrapper { progress = levelProgress });
        PlayerPrefs.SetString("LevelProgress", json);
        PlayerPrefs.Save();
    }
    
    void LoadProgress()
    {
        if (PlayerPrefs.HasKey("LevelProgress"))
        {
            string json = PlayerPrefs.GetString("LevelProgress");
            ProgressWrapper wrapper = JsonUtility.FromJson<ProgressWrapper>(json);
            levelProgress = wrapper.progress;
        }
    }
    
    public void ResetProgress()
    {
        levelProgress.Clear();
        PlayerPrefs.DeleteKey("LevelProgress");
        PlayerPrefs.Save();
    }
}

[System.Serializable]
public class LevelProgress
{
    public int levelIndex;
    public bool unlocked;
    public bool completed;
    public int attempts;
    public float bestTime;
    public int rating; // 1-3 stars
}

[System.Serializable]
public class ProgressWrapper
{
    public List<LevelProgress> progress;
}
```

---

## NPC Spawning from Level Data

### LevelNPCSpawner
```csharp
public class LevelNPCSpawner : MonoBehaviour
{
    public Transform npcContainer;
    
    void Start()
    {
        SpawnNPCsFromLevelData();
    }
    
    void SpawnNPCsFromLevelData()
    {
        LevelData levelData = LevelManager.Instance.CurrentLevel;
        if (levelData == null || levelData.npcSpawns == null) return;
        
        foreach (NPCSpawnInfo spawnInfo in levelData.npcSpawns)
        {
            SpawnNPC(spawnInfo);
        }
    }
    
    void SpawnNPC(NPCSpawnInfo info)
    {
        // Get prefab from database
        GameObject prefab = NPCDatabase.Instance.GetPrefab(info.npcType);
        if (prefab == null) return;
        
        // Instantiate
        GameObject npc = Instantiate(prefab, info.position, Quaternion.identity, npcContainer);
        
        // Configure emotion
        NPCEmotionState emotionState = npc.GetComponent<NPCEmotionState>();
        if (emotionState != null)
        {
            emotionState.SetEmotion(info.emotion);
        }
        
        // Configure patrol
        if (info.patrolPoints != null && info.patrolPoints.Length > 0)
        {
            PatrolBehavior patrol = npc.GetComponent<PatrolBehavior>();
            if (patrol != null)
            {
                // Create patrol path
                GameObject pathObj = new GameObject($"PatrolPath_{npc.name}");
                pathObj.transform.SetParent(npc.transform);
                
                foreach (Vector2 point in info.patrolPoints)
                {
                    GameObject waypoint = new GameObject("Waypoint");
                    waypoint.transform.SetParent(pathObj.transform);
                    waypoint.transform.position = point;
                }
                
                patrol.waypoints = pathObj.GetComponentsInChildren<Transform>()
                    .Where(t => t != pathObj.transform)
                    .ToArray();
            }
        }
    }
}
```

---

## NPC Database

### NPCDatabase.cs
```csharp
[CreateAssetMenu(fileName = "NPCDatabase", menuName = "MaskCompany/NPC Database")]
public class NPCDatabase : ScriptableObject
{
    public static NPCDatabase Instance { get; private set; }
    
    [System.Serializable]
    public class NPCEntry
    {
        public NPCType type;
        public GameObject prefab;
    }
    
    public NPCEntry[] npcs;
    
    public GameObject GetPrefab(NPCType type)
    {
        NPCEntry entry = npcs.FirstOrDefault(n => n.type == type);
        return entry?.prefab;
    }
    
    void OnEnable()
    {
        Instance = this;
    }
}

public enum NPCType
{
    Worker,
    Manager,
    Intern,
    Security,
    Janitor
}
```

---

## Level Selection UI

### LevelSelectUI.cs
```csharp
public class LevelSelectUI : MonoBehaviour
{
    public Transform levelButtonContainer;
    public GameObject levelButtonPrefab;
    
    void Start()
    {
        PopulateLevels();
    }
    
    void PopulateLevels()
    {
        // Clear existing
        foreach (Transform child in levelButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create buttons
        for (int i = 0; i < LevelManager.Instance.GetLevelCount(); i++)
        {
            LevelData level = LevelManager.Instance.GetLevel(i);
            bool unlocked = LevelManager.Instance.IsLevelUnlocked(i);
            bool completed = LevelManager.Instance.IsLevelCompleted(i);
            
            GameObject buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
            LevelButton button = buttonObj.GetComponent<LevelButton>();
            
            button.Setup(level, i, unlocked, completed);
        }
    }
}

public class LevelButton : MonoBehaviour
{
    public TextMeshProUGUI levelNameText;
    public Image thumbnailImage;
    public Image lockIcon;
    public Image completedIcon;
    public Button button;
    
    private int levelIndex;
    
    public void Setup(LevelData level, int index, bool unlocked, bool completed)
    {
        levelIndex = index;
        
        levelNameText.text = level.levelName;
        thumbnailImage.sprite = level.levelThumbnail;
        
        lockIcon.gameObject.SetActive(!unlocked);
        completedIcon.gameObject.SetActive(completed);
        
        button.interactable = unlocked;
        button.onClick.AddListener(OnClick);
    }
    
    void OnClick()
    {
        LevelManager.Instance.LoadLevel(levelIndex);
    }
}
```

---

## Game Manager Integration

### GameManager.cs
```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Components")]
    public LevelManager levelManager;
    public AudioManager audioManager;
    public SceneTransition sceneTransition;
    
    [Header("State")]
    public bool isGamePaused = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void StartNewGame()
    {
        levelManager.LoadLevel(0);
    }
    
    public void ContinueGame()
    {
        // Find first incomplete level
        for (int i = 0; i < levelManager.GetLevelCount(); i++)
        {
            if (!levelManager.IsLevelCompleted(i))
            {
                levelManager.LoadLevel(i);
                return;
            }
        }
        
        // All complete, load last level
        levelManager.LoadLevel(levelManager.GetLevelCount() - 1);
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
```
