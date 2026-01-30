# UI - Game Progress Specification

## Overview
Displays overall game progress, level status, objectives, and scoring information.

---

## HUD Layout

```
┌─────────────────────────────────────────────────────────────┐
│  LEVEL 3: THE MEETING ROOM          ⏱️ 01:23    ❗ 0/3     │
│  └── Reach the exit                                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│                                                             │
│                       GAME VIEW                             │
│                                                             │
│                                                             │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│  [MASK UI]                                      [MINIMAP?]  │
└─────────────────────────────────────────────────────────────┘

Legend:
⏱️ = Timer (if timed levels)
❗ = Detection counter (detections / max allowed)
```

---

## Components

### GameProgressUI
Master controller for all progress-related UI.

```csharp
public class GameProgressUI : MonoBehaviour
{
    [Header("Level Info")]
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI objectiveText;
    
    [Header("Timer")]
    public TextMeshProUGUI timerText;
    public bool showTimer = true;
    
    [Header("Detection Counter")]
    public TextMeshProUGUI detectionText;
    public Image detectionIcon;
    public int maxDetections = 3;
    private int currentDetections = 0;
    
    [Header("Animation")]
    public CanvasGroup headerGroup;
    
    void Start()
    {
        InitializeUI();
        GameEvents.OnPlayerCaught += OnDetection;
    }
    
    void OnDestroy()
    {
        GameEvents.OnPlayerCaught -= OnDetection;
    }
    
    void InitializeUI()
    {
        LevelData currentLevel = LevelManager.Instance.CurrentLevel;
        
        if (currentLevel != null)
        {
            levelNameText.text = $"LEVEL {currentLevel.levelIndex + 1}: {currentLevel.levelName.ToUpper()}";
            objectiveText.text = currentLevel.objectiveText;
        }
        
        UpdateDetectionCounter();
        
        // Fade in
        headerGroup.alpha = 0;
        headerGroup.DOFade(1f, 0.5f);
    }
    
    void Update()
    {
        if (showTimer)
        {
            UpdateTimer();
        }
    }
    
    void UpdateTimer()
    {
        float elapsed = LevelFlowController.Instance?.GetElapsedTime() ?? 0;
        
        int minutes = Mathf.FloorToInt(elapsed / 60);
        int seconds = Mathf.FloorToInt(elapsed % 60);
        
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
    
    void OnDetection()
    {
        currentDetections++;
        UpdateDetectionCounter();
        
        // Animation
        detectionIcon.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
        detectionIcon.DOColor(Color.red, 0.1f)
            .OnComplete(() => detectionIcon.DOColor(Color.white, 0.2f));
        
        if (currentDetections >= maxDetections)
        {
            // Game over handled by LevelFlowController
        }
    }
    
    void UpdateDetectionCounter()
    {
        detectionText.text = $"{currentDetections}/{maxDetections}";
        
        // Color based on remaining chances
        float dangerPercent = (float)currentDetections / maxDetections;
        detectionText.color = Color.Lerp(Color.white, Color.red, dangerPercent);
    }
}
```

---

## Level Complete Screen

### LevelCompleteUI
```csharp
public class LevelCompleteUI : MonoBehaviour
{
    [Header("Panels")]
    public CanvasGroup panelGroup;
    public RectTransform contentPanel;
    
    [Header("Content")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI detectionsText;
    
    [Header("Rating")]
    public Image[] stars;
    public Sprite starFilled;
    public Sprite starEmpty;
    
    [Header("Buttons")]
    public Button nextLevelButton;
    public Button retryButton;
    public Button menuButton;
    
    public void Show(LevelCompleteData data)
    {
        gameObject.SetActive(true);
        
        // Populate data
        levelNameText.text = data.levelName;
        timeText.text = FormatTime(data.completionTime);
        detectionsText.text = $"Detections: {data.detections}";
        
        // Calculate and show rating
        int rating = CalculateRating(data);
        ShowRating(rating);
        
        // Setup buttons
        bool hasNextLevel = LevelManager.Instance.currentLevelIndex + 1 < 
                           LevelManager.Instance.GetLevelCount();
        nextLevelButton.gameObject.SetActive(hasNextLevel);
        
        // Animate in
        AnimateIn();
    }
    
    void AnimateIn()
    {
        panelGroup.alpha = 0;
        contentPanel.localScale = Vector3.one * 0.8f;
        
        Sequence seq = DOTween.Sequence();
        seq.Append(panelGroup.DOFade(1f, 0.3f));
        seq.Join(contentPanel.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
        
        // Animate stars one by one
        for (int i = 0; i < stars.Length; i++)
        {
            int index = i;
            seq.AppendCallback(() => AnimateStar(index));
            seq.AppendInterval(0.2f);
        }
    }
    
    void AnimateStar(int index)
    {
        if (stars[index].sprite == starFilled)
        {
            stars[index].transform.localScale = Vector3.zero;
            stars[index].transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            
            // Particle effect
            // PlayStarParticle(stars[index].transform.position);
        }
    }
    
    int CalculateRating(LevelCompleteData data)
    {
        // 3 stars: No detections, under par time
        // 2 stars: 1 detection or slightly over par
        // 1 star: Completed
        
        LevelData level = LevelManager.Instance.CurrentLevel;
        
        if (data.detections == 0 && data.completionTime <= level.parTime)
            return 3;
        if (data.detections <= 1)
            return 2;
        return 1;
    }
    
    void ShowRating(int rating)
    {
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].sprite = i < rating ? starFilled : starEmpty;
        }
    }
    
    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        int milliseconds = Mathf.FloorToInt((time % 1) * 100);
        
        return $"{minutes:00}:{seconds:00}.{milliseconds:00}";
    }
    
    // Button handlers
    public void OnNextLevel() => LevelManager.Instance.LoadNextLevel();
    public void OnRetry() => LevelManager.Instance.ReloadCurrentLevel();
    public void OnMenu() => LevelManager.Instance.LoadMainMenu();
}

[System.Serializable]
public class LevelCompleteData
{
    public string levelName;
    public float completionTime;
    public int detections;
}
```

---

## Game Over Screen

### GameOverUI
```csharp
public class GameOverUI : MonoBehaviour
{
    [Header("Components")]
    public CanvasGroup panelGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI reasonText;
    public Image maskImage;
    
    [Header("Animation")]
    public float shakeAmount = 10f;
    public Color flashColor = Color.red;
    
    public void Show(string reason)
    {
        gameObject.SetActive(true);
        
        reasonText.text = reason;
        
        AnimateIn();
    }
    
    void AnimateIn()
    {
        // Screen flash
        panelGroup.alpha = 0;
        Image background = GetComponentInChildren<Image>();
        background.color = flashColor;
        
        Sequence seq = DOTween.Sequence();
        seq.Append(panelGroup.DOFade(1f, 0.1f));
        seq.Append(background.DOColor(new Color(0, 0, 0, 0.8f), 0.3f));
        
        // Title slam in
        titleText.transform.localScale = Vector3.one * 3f;
        seq.Append(titleText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBounce));
        
        // Camera shake effect
        Camera.main.DOShakePosition(0.5f, shakeAmount);
    }
    
    public void OnRetry()
    {
        Time.timeScale = 1f;
        LevelManager.Instance.ReloadCurrentLevel();
    }
    
    public void OnMenu()
    {
        Time.timeScale = 1f;
        LevelManager.Instance.LoadMainMenu();
    }
}
```

---

## Pause Menu

### PauseUI
```csharp
public class PauseUI : MonoBehaviour
{
    [Header("Components")]
    public CanvasGroup panelGroup;
    public RectTransform menuPanel;
    
    [Header("Buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button optionsButton;
    public Button menuButton;
    
    private bool isPaused = false;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        
        if (isPaused)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }
    
    void Show()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f;
        
        panelGroup.alpha = 0;
        menuPanel.localScale = Vector3.one * 0.9f;
        
        panelGroup.DOFade(1f, 0.2f).SetUpdate(true);
        menuPanel.DOScale(1f, 0.2f).SetUpdate(true).SetEase(Ease.OutBack);
    }
    
    void Hide()
    {
        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(panelGroup.DOFade(0f, 0.15f));
        seq.OnComplete(() => {
            gameObject.SetActive(false);
            Time.timeScale = 1f;
        });
    }
    
    public void OnResume() => TogglePause();
    public void OnRestart() 
    {
        Time.timeScale = 1f;
        LevelManager.Instance.ReloadCurrentLevel();
    }
    public void OnMenu()
    {
        Time.timeScale = 1f;
        LevelManager.Instance.LoadMainMenu();
    }
}
```

---

## Progress Persistence Display

### OverallProgressUI
For main menu / level select screen.

```csharp
public class OverallProgressUI : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI progressText;
    public Slider progressBar;
    public TextMeshProUGUI totalStarsText;
    
    void Start()
    {
        UpdateProgress();
    }
    
    void UpdateProgress()
    {
        LevelManager lm = LevelManager.Instance;
        
        int totalLevels = lm.GetLevelCount();
        int completedLevels = 0;
        int totalStars = 0;
        int earnedStars = 0;
        
        for (int i = 0; i < totalLevels; i++)
        {
            if (lm.IsLevelCompleted(i))
            {
                completedLevels++;
            }
            
            totalStars += 3;
            // earnedStars += lm.GetLevelRating(i);
        }
        
        // Progress text
        progressText.text = $"{completedLevels}/{totalLevels} Levels Complete";
        
        // Progress bar
        progressBar.value = (float)completedLevels / totalLevels;
        
        // Stars
        totalStarsText.text = $"⭐ {earnedStars}/{totalStars}";
    }
}
```

---

## Objective Tracker (Optional)

### ObjectiveUI
For levels with multiple objectives.

```csharp
public class ObjectiveUI : MonoBehaviour
{
    [Header("Components")]
    public Transform objectiveContainer;
    public GameObject objectivePrefab;
    
    private List<ObjectiveItemUI> objectiveItems = new List<ObjectiveItemUI>();
    
    public void SetObjectives(List<Objective> objectives)
    {
        // Clear existing
        foreach (Transform child in objectiveContainer)
        {
            Destroy(child.gameObject);
        }
        objectiveItems.Clear();
        
        // Create new
        foreach (var obj in objectives)
        {
            GameObject itemObj = Instantiate(objectivePrefab, objectiveContainer);
            ObjectiveItemUI item = itemObj.GetComponent<ObjectiveItemUI>();
            item.Setup(obj);
            objectiveItems.Add(item);
        }
    }
    
    public void UpdateObjective(string id, bool completed)
    {
        ObjectiveItemUI item = objectiveItems.Find(i => i.objectiveId == id);
        if (item != null)
        {
            item.SetCompleted(completed);
        }
    }
}

public class ObjectiveItemUI : MonoBehaviour
{
    public string objectiveId;
    public TextMeshProUGUI text;
    public Image checkmark;
    public Color completedColor = Color.green;
    public Color pendingColor = Color.white;
    
    public void Setup(Objective obj)
    {
        objectiveId = obj.id;
        text.text = obj.description;
        SetCompleted(obj.completed);
    }
    
    public void SetCompleted(bool completed)
    {
        checkmark.gameObject.SetActive(completed);
        text.color = completed ? completedColor : pendingColor;
        
        if (completed)
        {
            // Strikethrough effect
            text.fontStyle = FontStyles.Strikethrough;
            transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
        }
    }
}

[System.Serializable]
public class Objective
{
    public string id;
    public string description;
    public bool completed;
}
```

---

## Animation Timings

| Element | Animation | Duration |
|---------|-----------|----------|
| Level Complete Panel | Fade + Scale | 0.3s |
| Star Reveal | Scale from 0 | 0.3s each |
| Game Over Flash | Color fade | 0.4s |
| Pause Menu | Fade + Scale | 0.2s |
| Detection Warning | Punch scale | 0.3s |
| Objective Complete | Punch + Strikethrough | 0.2s |
