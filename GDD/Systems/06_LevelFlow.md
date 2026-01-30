# Level Flow System Specification

## Overview
Manages level progression from start to completion, including transitions and state changes.

---

## Level Flow Diagram

```
┌─────────────┐
│  Level Load │
└──────┬──────┘
       ▼
┌─────────────┐
│ Initialize  │ ← Spawn player, NPCs, setup UI
└──────┬──────┘
       ▼
┌─────────────┐
│ Level Start │ ← Brief countdown/fade-in
└──────┬──────┘
       ▼
┌─────────────┐
│  Gameplay   │◄─────────────┐
└──────┬──────┘              │
       │                     │
       ├───► Goal Reached ───┤
       │         │           │
       │         ▼           │
       │  ┌─────────────┐    │
       │  │Level Complete│    │
       │  └──────┬──────┘    │
       │         │           │
       │         ▼           │
       │  ┌─────────────┐    │
       │  │ Next Level  │    │
       │  └─────────────┘    │
       │                     │
       └───► Game Over ──────┘
                 │
                 ▼
          ┌─────────────┐
          │   Retry /   │
          │  Main Menu  │
          └─────────────┘
```

---

## Level States

```csharp
public enum LevelState
{
    Loading,      // Scene loading, initialization
    Starting,     // Countdown, intro animation
    Playing,      // Active gameplay
    Paused,       // Game paused
    Complete,     // Level finished successfully
    Failed,       // Game over
    Transitioning // Loading next level
}
```

---

## Core Components

### LevelFlowController
```csharp
public class LevelFlowController : MonoBehaviour
{
    public static LevelFlowController Instance { get; private set; }
    
    [Header("State")]
    public LevelState currentState = LevelState.Loading;
    
    [Header("Timing")]
    public float startDelay = 1f;
    public float endDelay = 2f;
    
    [Header("References")]
    public PlayerController player;
    public LevelGoal levelGoal;
    public UIManager uiManager;
    
    [Header("Events")]
    public UnityEvent onLevelStart;
    public UnityEvent onLevelComplete;
    public UnityEvent onLevelFailed;
    
    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        StartCoroutine(InitializeLevel());
    }
    
    IEnumerator InitializeLevel()
    {
        currentState = LevelState.Loading;
        
        // Disable player input
        player.enabled = false;
        
        // Setup level
        SpawnEntities();
        
        // Fade in
        yield return uiManager.FadeIn(0.5f);
        
        // Start sequence
        currentState = LevelState.Starting;
        yield return new WaitForSeconds(startDelay);
        
        // Begin gameplay
        currentState = LevelState.Playing;
        player.enabled = true;
        onLevelStart?.Invoke();
    }
    
    void SpawnEntities()
    {
        // Find spawn point
        PlayerSpawnPoint spawnPoint = FindObjectOfType<PlayerSpawnPoint>();
        if (spawnPoint != null)
        {
            player.transform.position = spawnPoint.transform.position;
            player.GetComponent<PlayerMaskSystem>().SetMask(spawnPoint.startingMask);
        }
        
        // Spawn NPCs from spawn points
        NPCSpawnPoint[] npcSpawns = FindObjectsOfType<NPCSpawnPoint>();
        foreach (var spawn in npcSpawns)
        {
            SpawnNPC(spawn);
        }
    }
    
    void SpawnNPC(NPCSpawnPoint spawnData)
    {
        // Instantiate NPC prefab based on type
        GameObject npcPrefab = NPCDatabase.Instance.GetPrefab(spawnData.npcType);
        GameObject npc = Instantiate(npcPrefab, spawnData.transform.position, Quaternion.identity);
        
        // Setup emotion
        NPCEmotionState emotion = npc.GetComponent<NPCEmotionState>();
        emotion.SetEmotion(spawnData.emotion);
        
        // Setup patrol if available
        if (spawnData.patrolPath != null)
        {
            PatrolBehavior patrol = npc.GetComponent<PatrolBehavior>();
            if (patrol != null)
            {
                patrol.waypoints = spawnData.patrolPath.GetWaypoints();
                patrol.SetState(NPCPhysicalState.Patrol);
            }
        }
    }
    
    public void OnGoalReached()
    {
        if (currentState != LevelState.Playing) return;
        
        StartCoroutine(CompleteLevel());
    }
    
    IEnumerator CompleteLevel()
    {
        currentState = LevelState.Complete;
        
        // Disable player
        player.enabled = false;
        
        // Trigger events
        onLevelComplete?.Invoke();
        
        // Show completion UI
        uiManager.ShowLevelComplete();
        
        // Wait
        yield return new WaitForSeconds(endDelay);
        
        // Transition ready
        currentState = LevelState.Transitioning;
    }
    
    public void OnPlayerCaught()
    {
        if (currentState != LevelState.Playing) return;
        
        StartCoroutine(FailLevel());
    }
    
    IEnumerator FailLevel()
    {
        currentState = LevelState.Failed;
        
        // Disable player
        player.enabled = false;
        
        // Trigger events
        onLevelFailed?.Invoke();
        
        // Camera shake, effects
        Camera.main.DOShakePosition(0.5f, 0.3f);
        
        yield return new WaitForSeconds(1f);
        
        // Show game over UI
        uiManager.ShowGameOver();
    }
    
    public void TogglePause()
    {
        if (currentState == LevelState.Playing)
        {
            currentState = LevelState.Paused;
            Time.timeScale = 0f;
            uiManager.ShowPause();
        }
        else if (currentState == LevelState.Paused)
        {
            currentState = LevelState.Playing;
            Time.timeScale = 1f;
            uiManager.HidePause();
        }
    }
    
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        LevelManager.Instance.LoadNextLevel();
    }
    
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
```

---

## Scene Transitions

### SceneTransition
```csharp
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }
    
    public CanvasGroup fadePanel;
    public float fadeDuration = 0.5f;
    
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
    
    public void LoadScene(string sceneName)
    {
        StartCoroutine(TransitionToScene(sceneName));
    }
    
    IEnumerator TransitionToScene(string sceneName)
    {
        // Fade out
        yield return FadeOut();
        
        // Load scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        // Fade in
        yield return FadeIn();
    }
    
    IEnumerator FadeOut()
    {
        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 0;
        
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadePanel.alpha = elapsed / fadeDuration;
            yield return null;
        }
        
        fadePanel.alpha = 1;
    }
    
    IEnumerator FadeIn()
    {
        fadePanel.alpha = 1;
        
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadePanel.alpha = 1 - (elapsed / fadeDuration);
            yield return null;
        }
        
        fadePanel.alpha = 0;
        fadePanel.gameObject.SetActive(false);
    }
}
```

---

## Level Goal Variants

### AreaGoal (Reach a location)
```csharp
public class AreaGoal : MonoBehaviour
{
    public UnityEvent onReached;
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            onReached?.Invoke();
            LevelFlowController.Instance.OnGoalReached();
        }
    }
}
```

### CollectGoal (Collect all items)
```csharp
public class CollectGoal : MonoBehaviour
{
    public int totalItems;
    private int collectedItems = 0;
    
    public void OnItemCollected()
    {
        collectedItems++;
        
        if (collectedItems >= totalItems)
        {
            LevelFlowController.Instance.OnGoalReached();
        }
    }
}
```

### SurvivalGoal (Survive for time)
```csharp
public class SurvivalGoal : MonoBehaviour
{
    public float surviveTime = 60f;
    private float timer;
    
    void Update()
    {
        if (LevelFlowController.Instance.currentState != LevelState.Playing) return;
        
        timer += Time.deltaTime;
        
        if (timer >= surviveTime)
        {
            LevelFlowController.Instance.OnGoalReached();
        }
    }
}
```

---

## Failure Conditions

### Detection Threshold
```csharp
public class DetectionFailure : MonoBehaviour
{
    public int maxDetections = 3;
    private int currentDetections = 0;
    
    public void OnPlayerDetected()
    {
        currentDetections++;
        
        if (currentDetections >= maxDetections)
        {
            LevelFlowController.Instance.OnPlayerCaught();
        }
    }
}
```

### Instant Failure
```csharp
public class InstantFailZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            LevelFlowController.Instance.OnPlayerCaught();
        }
    }
}
```

---

## Level Intro/Outro

### Level Intro Sequence
```csharp
public class LevelIntro : MonoBehaviour
{
    public string levelName;
    public string objective;
    public float displayDuration = 2f;
    
    [Header("UI References")]
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI objectiveText;
    public CanvasGroup introPanel;
    
    public IEnumerator PlayIntro()
    {
        levelNameText.text = levelName;
        objectiveText.text = objective;
        
        // Fade in
        introPanel.alpha = 0;
        introPanel.gameObject.SetActive(true);
        yield return introPanel.DOFade(1, 0.3f).WaitForCompletion();
        
        // Display
        yield return new WaitForSeconds(displayDuration);
        
        // Fade out
        yield return introPanel.DOFade(0, 0.3f).WaitForCompletion();
        introPanel.gameObject.SetActive(false);
    }
}
```

---

## Checkpoint System (Optional)

```csharp
public class Checkpoint : MonoBehaviour
{
    public int checkpointIndex;
    private bool activated = false;
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !activated)
        {
            activated = true;
            CheckpointManager.Instance.SetCheckpoint(this);
        }
    }
}

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }
    
    private Checkpoint currentCheckpoint;
    
    public void SetCheckpoint(Checkpoint checkpoint)
    {
        currentCheckpoint = checkpoint;
    }
    
    public Vector2 GetRespawnPosition()
    {
        if (currentCheckpoint != null)
        {
            return currentCheckpoint.transform.position;
        }
        
        // Default to spawn point
        PlayerSpawnPoint spawn = FindObjectOfType<PlayerSpawnPoint>();
        return spawn != null ? (Vector2)spawn.transform.position : Vector2.zero;
    }
}
```
