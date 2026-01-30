# UI - NPC State Display Specification

## Overview
Visual indicators that help players understand NPC emotions and detection states.

---

## UI Elements

### 1. Emotion Indicator (Above NPC)
Floating indicator showing NPC's current emotional state.

```
        ┌─────┐
        │ 😊  │  ← Emotion bubble
        └──┬──┘
           │
        ┌──┴──┐
        │ NPC │
        └─────┘
```

### 2. Detection Warning (Screen Edge)
Visual warning when player is about to be detected.

```
┌─────────────────────────────────────┐
│ ▓▓                              ▓▓  │ ← Red vignette
│ ▓                                ▓  │
│                                     │
│              PLAYER                 │
│                                     │
│ ▓                                ▓  │
│ ▓▓                              ▓▓  │
└─────────────────────────────────────┘
```

### 3. Alert State Indicator
Shows when NPC has detected wrong mask.

```
        ┌─────┐
        │ ❗  │  ← Alert icon
        └──┬──┘
           │
        ┌──┴──┐
        │ NPC │  (highlighted red)
        └─────┘
```

---

## Emotion Indicator Implementation

### NPCEmotionIndicator
World-space UI element above NPC.

```csharp
public class NPCEmotionIndicator : MonoBehaviour
{
    [Header("Components")]
    public SpriteRenderer emotionSprite;
    public SpriteRenderer backgroundSprite;
    public SpriteRenderer alertSprite;
    
    [Header("Sprites")]
    public Sprite[] emotionSprites; // Indexed by EmotionType
    public Sprite alertIcon;
    public Sprite suspiciousIcon;
    
    [Header("Colors")]
    public Color normalBackground = new Color(1, 1, 1, 0.8f);
    public Color alertBackground = new Color(1, 0.3f, 0.3f, 0.9f);
    public Color suspiciousBackground = new Color(1, 1, 0.3f, 0.9f);
    
    [Header("Animation")]
    public float bobAmount = 0.1f;
    public float bobSpeed = 2f;
    
    [Header("References")]
    public NPCEmotionState emotionState;
    public NPCBrain npcBrain;
    
    private Vector3 startLocalPos;
    private Camera mainCam;
    
    void Start()
    {
        startLocalPos = transform.localPosition;
        mainCam = Camera.main;
        
        emotionState.onEmotionChanged.AddListener(OnEmotionChanged);
        UpdateEmotion(emotionState.currentEmotion);
    }
    
    void Update()
    {
        // Bob animation
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.localPosition = startLocalPos + Vector3.up * bob;
        
        // Always face camera (billboard)
        transform.rotation = Quaternion.identity;
        
        // Update alert state
        UpdateAlertState();
    }
    
    void OnEmotionChanged(EmotionType emotion)
    {
        UpdateEmotion(emotion);
    }
    
    void UpdateEmotion(EmotionType emotion)
    {
        if (emotionSprites.Length > (int)emotion)
        {
            emotionSprite.sprite = emotionSprites[(int)emotion];
        }
        
        // Animation
        transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
    }
    
    void UpdateAlertState()
    {
        if (npcBrain == null) return;
        
        switch (npcBrain.currentAIState)
        {
            case NPCAIState.Alert:
                ShowAlert();
                break;
            case NPCAIState.Suspicious:
                ShowSuspicious();
                break;
            default:
                ShowNormal();
                break;
        }
    }
    
    void ShowAlert()
    {
        alertSprite.gameObject.SetActive(true);
        alertSprite.sprite = alertIcon;
        backgroundSprite.color = alertBackground;
        
        // Shake animation
        transform.DOShakePosition(0.5f, 0.1f).SetLoops(-1);
    }
    
    void ShowSuspicious()
    {
        alertSprite.gameObject.SetActive(true);
        alertSprite.sprite = suspiciousIcon;
        backgroundSprite.color = suspiciousBackground;
    }
    
    void ShowNormal()
    {
        alertSprite.gameObject.SetActive(false);
        backgroundSprite.color = normalBackground;
        transform.DOKill();
    }
}
```

---

## Detection Warning System

### DetectionWarningUI
Screen overlay that shows when detection is imminent.

```csharp
public class DetectionWarningUI : MonoBehaviour
{
    [Header("Components")]
    public Image vignetteImage;
    public Image directionIndicator;
    public AudioSource warningSound;
    
    [Header("Settings")]
    public float maxOpacity = 0.5f;
    public Color warningColor = new Color(1, 0, 0, 0.5f);
    public float pulseSpeed = 2f;
    
    [Header("References")]
    public PlayerController player;
    
    private float currentThreatLevel = 0f;
    private Vector2 threatDirection;
    private bool isWarning = false;
    
    void Start()
    {
        vignetteImage.color = new Color(warningColor.r, warningColor.g, warningColor.b, 0);
    }
    
    void Update()
    {
        UpdateThreatLevel();
        UpdateVisuals();
    }
    
    void UpdateThreatLevel()
    {
        // Find all NPCs that might detect player
        NPCDetection[] allNPCs = FindObjectsOfType<NPCDetection>();
        
        float maxThreat = 0f;
        Vector2 maxThreatDir = Vector2.zero;
        
        foreach (var npc in allNPCs)
        {
            if (npc.playerInRange && !npc.playerDetected)
            {
                // Calculate threat based on distance and angle
                float distance = Vector2.Distance(player.transform.position, npc.transform.position);
                float normalizedDist = 1f - (distance / npc.detectionRadius);
                
                if (normalizedDist > maxThreat)
                {
                    maxThreat = normalizedDist;
                    maxThreatDir = (npc.transform.position - player.transform.position).normalized;
                }
            }
        }
        
        currentThreatLevel = maxThreat;
        threatDirection = maxThreatDir;
        isWarning = currentThreatLevel > 0.3f;
    }
    
    void UpdateVisuals()
    {
        // Vignette opacity based on threat
        float targetOpacity = currentThreatLevel * maxOpacity;
        
        // Pulse effect when warning
        if (isWarning)
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            targetOpacity *= (0.7f + pulse * 0.3f);
        }
        
        Color newColor = warningColor;
        newColor.a = targetOpacity;
        vignetteImage.color = Color.Lerp(vignetteImage.color, newColor, Time.deltaTime * 10f);
        
        // Direction indicator
        if (isWarning && threatDirection != Vector2.zero)
        {
            directionIndicator.gameObject.SetActive(true);
            float angle = Mathf.Atan2(threatDirection.y, threatDirection.x) * Mathf.Rad2Deg;
            directionIndicator.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        }
        else
        {
            directionIndicator.gameObject.SetActive(false);
        }
        
        // Warning sound
        if (isWarning && !warningSound.isPlaying)
        {
            warningSound.Play();
        }
        else if (!isWarning && warningSound.isPlaying)
        {
            warningSound.Stop();
        }
    }
}
```

---

## NPC Highlight System

### NPCHighlight
Highlights NPC when player is looking at them or nearby.

```csharp
public class NPCHighlight : MonoBehaviour
{
    [Header("Components")]
    public SpriteRenderer[] sprites;
    public Material highlightMaterial;
    public Material defaultMaterial;
    
    [Header("Colors")]
    public Color matchHighlight = Color.green;
    public Color mismatchHighlight = Color.red;
    public Color neutralHighlight = Color.yellow;
    
    [Header("Settings")]
    public float highlightRange = 2f;
    
    private Transform player;
    private PlayerMaskSystem playerMask;
    private NPCEmotionState emotionState;
    
    void Start()
    {
        player = FindObjectOfType<PlayerController>().transform;
        playerMask = player.GetComponent<PlayerMaskSystem>();
        emotionState = GetComponent<NPCEmotionState>();
    }
    
    void Update()
    {
        float distance = Vector2.Distance(transform.position, player.position);
        
        if (distance < highlightRange)
        {
            ApplyHighlight();
        }
        else
        {
            RemoveHighlight();
        }
    }
    
    void ApplyHighlight()
    {
        MaskType required = emotionState.GetRequiredMask();
        MaskType current = playerMask.currentMask;
        
        Color highlightColor;
        
        if (required == MaskType.None)
        {
            highlightColor = neutralHighlight;
        }
        else if (current == required)
        {
            highlightColor = matchHighlight;
        }
        else
        {
            highlightColor = mismatchHighlight;
        }
        
        foreach (var sprite in sprites)
        {
            sprite.material = highlightMaterial;
            sprite.material.SetColor("_OutlineColor", highlightColor);
        }
    }
    
    void RemoveHighlight()
    {
        foreach (var sprite in sprites)
        {
            sprite.material = defaultMaterial;
        }
    }
}
```

---

## Minimap NPC Indicators (Optional)

### MinimapNPCMarker
```csharp
public class MinimapNPCMarker : MonoBehaviour
{
    public Image markerImage;
    public NPCEmotionState emotionState;
    public NPCBrain brain;
    
    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color alertColor = Color.red;
    
    [Header("Icons")]
    public Sprite normalIcon;
    public Sprite alertIcon;
    
    void Update()
    {
        // Position relative to minimap
        // ...
        
        // State-based appearance
        if (brain.currentAIState == NPCAIState.Alert)
        {
            markerImage.color = alertColor;
            markerImage.sprite = alertIcon;
        }
        else
        {
            markerImage.color = normalColor;
            markerImage.sprite = normalIcon;
        }
    }
}
```

---

## Visual Design Specs

### Emotion Icons
| Emotion | Icon | Animation |
|---------|------|-----------|
| Happy | 😊 | Gentle bounce |
| Sad | 😢 | Slow droop |
| Angry | 😠 | Shake/vibrate |
| Neutral | 😐 | Minimal movement |

### Alert Indicators
| State | Visual | Sound |
|-------|--------|-------|
| Normal | White bubble | None |
| Suspicious | Yellow bubble + ? | Soft chime |
| Alert | Red bubble + ! | Alarm |
| Searching | Orange bubble + ? | Ambient tension |

### Color Coding
```csharp
public static class NPCUIColors
{
    // Emotion bubble backgrounds
    public static Color Happy = new Color(1f, 0.95f, 0.7f);
    public static Color Sad = new Color(0.7f, 0.8f, 1f);
    public static Color Angry = new Color(1f, 0.75f, 0.75f);
    public static Color Neutral = new Color(0.9f, 0.9f, 0.9f);
    
    // Alert states
    public static Color Suspicious = new Color(1f, 0.9f, 0.3f);
    public static Color Alert = new Color(1f, 0.3f, 0.3f);
    public static Color Searching = new Color(1f, 0.6f, 0.3f);
    
    // Mask match highlighting
    public static Color MatchOutline = new Color(0.3f, 1f, 0.3f);
    public static Color MismatchOutline = new Color(1f, 0.3f, 0.3f);
}
```

---

## Accessibility Considerations

### Colorblind Support
- Use shapes in addition to colors
- Provide pattern fills option
- High contrast mode

```csharp
public class AccessibilitySettings
{
    public bool useShapesForEmotions = false;
    public bool highContrastMode = false;
    
    // Shapes for emotions
    public Sprite happyShape;   // Circle
    public Sprite sadShape;     // Triangle down
    public Sprite angryShape;   // Diamond
    public Sprite neutralShape; // Square
}
```

### Screen Reader Support (Optional)
- Audio cues for emotion changes
- Proximity warnings

```csharp
public class NPCAudioCues : MonoBehaviour
{
    public AudioClip[] emotionSounds; // Different tone for each emotion
    
    void OnEmotionVisible(EmotionType emotion)
    {
        // Play distinctive sound for each emotion type
        AudioSource.PlayClipAtPoint(emotionSounds[(int)emotion], transform.position);
    }
}
```
