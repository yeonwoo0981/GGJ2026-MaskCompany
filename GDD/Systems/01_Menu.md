# Menu System Specification

## Overview
The menu system handles all non-gameplay UI screens and navigation between them.

---

## Screens

### Main Menu
**Scene:** `MainMenu.unity`

| Element | Type | Action |
|---------|------|--------|
| Title | Text | "MASK COMPANY" |
| Start Game | Button | Load Level 1 |
| Options | Button | Open Options Panel |
| Credits | Button | Open Credits Panel |
| Quit | Button | Exit Application |

**Visual Style:**
- Office desk background with masks scattered
- Neon accent lighting
- Animated mask floating/rotating

---

### Pause Menu
**Implementation:** Overlay panel (not separate scene)

| Element | Type | Action |
|---------|------|--------|
| "PAUSED" | Text | Header |
| Resume | Button | Close pause, Time.timeScale = 1 |
| Restart | Button | Reload current level |
| Options | Button | Open Options Panel |
| Main Menu | Button | Load MainMenu scene |

**Trigger:** ESC key or Start button  
**Behavior:** 
- `Time.timeScale = 0` when opened
- Disable player input
- Show semi-transparent overlay

---

### Options Panel
**Shared between Main Menu and Pause**

| Setting | Type | Default |
|---------|------|---------|
| Master Volume | Slider | 100% |
| Music Volume | Slider | 80% |
| SFX Volume | Slider | 100% |
| Screen Mode | Dropdown | Fullscreen |

**Note:** Keep minimal for game jam. Volume controls only if audio is implemented.

---

### Level Complete Screen
**Implementation:** Overlay panel

| Element | Type | Action |
|---------|------|--------|
| "LEVEL COMPLETE" | Text | Header |
| Level Name | Text | Dynamic |
| Time | Text | Completion time |
| Next Level | Button | Load next level |
| Retry | Button | Reload current |
| Main Menu | Button | Return to menu |

---

### Game Over Screen
**Implementation:** Overlay panel

| Element | Type | Action |
|---------|------|--------|
| "GAME OVER" | Text | Header |
| Reason | Text | "Wrong mask detected!" |
| Retry | Button | Reload current level |
| Main Menu | Button | Return to menu |

---

## Implementation

### MenuManager.cs
```csharp
public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject pausePanel;
    public GameObject optionsPanel;
    public GameObject levelCompletePanel;
    public GameObject gameOverPanel;
    
    private bool isPaused = false;
    
    public void StartGame()
    {
        SceneManager.LoadScene("Level_01");
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }
    
    public void ShowLevelComplete()
    {
        levelCompletePanel.SetActive(true);
        Time.timeScale = 0f;
    }
    
    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }
    
    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        LevelManager.Instance.LoadNextLevel();
    }
    
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
}
```

---

## UI Layout Guidelines

### Button Styling
- Minimum size: 200x50 pixels
- Font: Bold, readable at small sizes
- Hover state: Slight scale up (1.05x) + color shift
- Click state: Scale down (0.95x)
- Use DOTween for smooth transitions

### Animation
```csharp
// Button hover example
button.transform.DOScale(1.05f, 0.1f);

// Panel fade in
panel.GetComponent<CanvasGroup>().DOFade(1f, 0.3f);
```

### Responsive Design
- Use Canvas Scaler with "Scale With Screen Size"
- Reference resolution: 1920x1080
- Match Width or Height: 0.5
