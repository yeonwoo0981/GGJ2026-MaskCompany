# UI - Mask Controls & State Specification

## Overview
The mask UI shows the player's current mask, available masks, and provides controls for switching.

---

## UI Layout

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│                                                             │
│                                                             │
│                       GAME VIEW                             │
│                                                             │
│                                                             │
│                                                             │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│  [1]😊  [2]😢  [3]😠  [4]😐           ◄ CURRENT: 😊 ►      │
│                                        [Scroll to change]   │
└─────────────────────────────────────────────────────────────┘
```

---

## Components

### MaskUI
Main controller for mask-related UI elements.

```csharp
public class MaskUI : MonoBehaviour
{
    [Header("References")]
    public PlayerMaskSystem playerMaskSystem;
    
    [Header("Current Mask Display")]
    public Image currentMaskImage;
    public TextMeshProUGUI currentMaskName;
    public Transform maskFrame;
    
    [Header("Mask Selector")]
    public Transform maskSelectorContainer;
    public GameObject maskSlotPrefab;
    public Color selectedColor = Color.white;
    public Color unselectedColor = new Color(1, 1, 1, 0.5f);
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    
    [Header("Sprites")]
    public Sprite[] maskSprites; // Indexed by MaskType
    public string[] maskNames;
    
    [Header("Animation")]
    public float switchAnimDuration = 0.2f;
    
    private List<MaskSlotUI> maskSlots = new List<MaskSlotUI>();
    
    void Start()
    {
        if (playerMaskSystem == null)
        {
            playerMaskSystem = FindObjectOfType<PlayerMaskSystem>();
        }
        
        playerMaskSystem.onMaskChanged.AddListener(OnMaskChanged);
        
        CreateMaskSlots();
        UpdateUI();
    }
    
    void CreateMaskSlots()
    {
        // Clear existing
        foreach (Transform child in maskSelectorContainer)
        {
            Destroy(child.gameObject);
        }
        maskSlots.Clear();
        
        // Create slot for each possible mask
        for (int i = 1; i < System.Enum.GetValues(typeof(MaskType)).Length; i++)
        {
            MaskType mask = (MaskType)i;
            
            GameObject slotObj = Instantiate(maskSlotPrefab, maskSelectorContainer);
            MaskSlotUI slot = slotObj.GetComponent<MaskSlotUI>();
            
            slot.Setup(mask, i, maskSprites[i], this);
            maskSlots.Add(slot);
        }
    }
    
    void OnMaskChanged(MaskType newMask)
    {
        UpdateUI();
        PlaySwitchAnimation();
    }
    
    void UpdateUI()
    {
        MaskType current = playerMaskSystem.currentMask;
        
        // Update current mask display
        if (currentMaskImage != null && maskSprites.Length > (int)current)
        {
            currentMaskImage.sprite = maskSprites[(int)current];
        }
        
        if (currentMaskName != null && maskNames.Length > (int)current)
        {
            currentMaskName.text = maskNames[(int)current];
        }
        
        // Update slots
        foreach (var slot in maskSlots)
        {
            bool isAvailable = playerMaskSystem.availableMasks.Contains(slot.maskType);
            bool isSelected = slot.maskType == current;
            
            slot.UpdateState(isAvailable, isSelected, selectedColor, unselectedColor, lockedColor);
        }
    }
    
    void PlaySwitchAnimation()
    {
        // Punch scale on current mask
        maskFrame.DOKill();
        maskFrame.localScale = Vector3.one;
        maskFrame.DOPunchScale(Vector3.one * 0.2f, switchAnimDuration);
        
        // Flash effect
        currentMaskImage.DOColor(Color.white, switchAnimDuration / 2)
            .OnComplete(() => currentMaskImage.DOColor(Color.white, switchAnimDuration / 2));
    }
    
    public void OnSlotClicked(MaskType mask)
    {
        playerMaskSystem.SetMask(mask);
    }
}
```

### MaskSlotUI
Individual mask slot in the selector.

```csharp
public class MaskSlotUI : MonoBehaviour
{
    [Header("Components")]
    public Image maskImage;
    public Image backgroundImage;
    public Image selectionFrame;
    public TextMeshProUGUI hotkeyText;
    public Image lockIcon;
    
    [HideInInspector] public MaskType maskType;
    
    private MaskUI parentUI;
    private Button button;
    
    public void Setup(MaskType type, int hotkey, Sprite sprite, MaskUI parent)
    {
        maskType = type;
        parentUI = parent;
        
        maskImage.sprite = sprite;
        hotkeyText.text = hotkey.ToString();
        
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }
    
    public void UpdateState(bool isAvailable, bool isSelected, Color selectedCol, Color unselectedCol, Color lockedCol)
    {
        button.interactable = isAvailable;
        
        if (!isAvailable)
        {
            backgroundImage.color = lockedCol;
            lockIcon.gameObject.SetActive(true);
            selectionFrame.gameObject.SetActive(false);
        }
        else
        {
            backgroundImage.color = isSelected ? selectedCol : unselectedCol;
            lockIcon.gameObject.SetActive(false);
            selectionFrame.gameObject.SetActive(isSelected);
            
            if (isSelected)
            {
                selectionFrame.DOKill();
                selectionFrame.transform.localScale = Vector3.one;
            }
        }
    }
    
    void OnClick()
    {
        parentUI.OnSlotClicked(maskType);
    }
    
    // Hover effects
    public void OnPointerEnter()
    {
        if (button.interactable)
        {
            transform.DOScale(1.1f, 0.1f);
        }
    }
    
    public void OnPointerExit()
    {
        transform.DOScale(1f, 0.1f);
    }
}
```

---

## Mask Wheel (Alternative)

Radial selector activated by holding a key.

```csharp
public class MaskWheel : MonoBehaviour
{
    [Header("Activation")]
    public KeyCode activationKey = KeyCode.Tab;
    public bool slowTimeWhileOpen = true;
    public float slowTimeScale = 0.3f;
    
    [Header("Wheel")]
    public RectTransform wheelContainer;
    public float wheelRadius = 150f;
    public GameObject wheelSlotPrefab;
    
    [Header("Selection")]
    public Image centerPreview;
    public float selectionDeadzone = 30f;
    
    private bool isOpen = false;
    private List<MaskWheelSlot> slots = new List<MaskWheelSlot>();
    private MaskType selectedMask;
    private PlayerMaskSystem maskSystem;
    
    void Update()
    {
        if (Input.GetKeyDown(activationKey))
        {
            OpenWheel();
        }
        
        if (Input.GetKeyUp(activationKey))
        {
            CloseWheel();
        }
        
        if (isOpen)
        {
            UpdateSelection();
        }
    }
    
    void OpenWheel()
    {
        isOpen = true;
        wheelContainer.gameObject.SetActive(true);
        
        if (slowTimeWhileOpen)
        {
            Time.timeScale = slowTimeScale;
        }
        
        // Animate open
        wheelContainer.localScale = Vector3.zero;
        wheelContainer.DOScale(1f, 0.15f).SetUpdate(true);
        
        CreateSlots();
    }
    
    void CloseWheel()
    {
        isOpen = false;
        
        // Apply selection
        if (selectedMask != MaskType.None)
        {
            maskSystem.SetMask(selectedMask);
        }
        
        // Animate close
        wheelContainer.DOScale(0f, 0.1f)
            .SetUpdate(true)
            .OnComplete(() => wheelContainer.gameObject.SetActive(false));
        
        Time.timeScale = 1f;
    }
    
    void CreateSlots()
    {
        // Clear existing
        foreach (var slot in slots)
        {
            Destroy(slot.gameObject);
        }
        slots.Clear();
        
        List<MaskType> available = maskSystem.availableMasks;
        float angleStep = 360f / available.Count;
        
        for (int i = 0; i < available.Count; i++)
        {
            float angle = i * angleStep - 90f; // Start from top
            Vector2 position = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad) * wheelRadius,
                Mathf.Sin(angle * Mathf.Deg2Rad) * wheelRadius
            );
            
            GameObject slotObj = Instantiate(wheelSlotPrefab, wheelContainer);
            slotObj.GetComponent<RectTransform>().anchoredPosition = position;
            
            MaskWheelSlot slot = slotObj.GetComponent<MaskWheelSlot>();
            slot.Setup(available[i], angle);
            slots.Add(slot);
        }
    }
    
    void UpdateSelection()
    {
        Vector2 mousePos = Input.mousePosition;
        Vector2 center = wheelContainer.position;
        Vector2 direction = mousePos - center;
        
        if (direction.magnitude < selectionDeadzone)
        {
            // In deadzone - no selection
            selectedMask = maskSystem.currentMask;
            HighlightSlot(null);
            return;
        }
        
        // Find closest slot
        float mouseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        MaskWheelSlot closest = null;
        float closestDist = float.MaxValue;
        
        foreach (var slot in slots)
        {
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(mouseAngle, slot.angle));
            if (angleDiff < closestDist)
            {
                closestDist = angleDiff;
                closest = slot;
            }
        }
        
        if (closest != null)
        {
            selectedMask = closest.maskType;
            HighlightSlot(closest);
            centerPreview.sprite = GetMaskSprite(selectedMask);
        }
    }
    
    void HighlightSlot(MaskWheelSlot selected)
    {
        foreach (var slot in slots)
        {
            slot.SetHighlighted(slot == selected);
        }
    }
}
```

---

## Mask Cooldown Display (If Implemented)

```csharp
public class MaskCooldownUI : MonoBehaviour
{
    public Image cooldownFill;
    public Image cooldownIcon;
    public PlayerMaskSystem maskSystem;
    
    void Update()
    {
        float cooldownPercent = maskSystem.GetCooldownPercent();
        
        cooldownFill.fillAmount = cooldownPercent;
        cooldownIcon.gameObject.SetActive(cooldownPercent > 0);
    }
}
```

---

## Input Hints

```csharp
public class MaskInputHints : MonoBehaviour
{
    public TextMeshProUGUI hintText;
    public CanvasGroup canvasGroup;
    
    public float showDuration = 3f;
    public float fadeDuration = 0.5f;
    
    private float showTimer;
    
    void Start()
    {
        // Show hints at level start
        ShowHints();
    }
    
    void ShowHints()
    {
        string hints = "Scroll or 1-4 to change masks";
        hintText.text = hints;
        
        canvasGroup.alpha = 0;
        canvasGroup.DOFade(1f, fadeDuration);
        
        showTimer = showDuration;
    }
    
    void Update()
    {
        if (showTimer > 0)
        {
            showTimer -= Time.deltaTime;
            
            if (showTimer <= 0)
            {
                canvasGroup.DOFade(0f, fadeDuration);
            }
        }
    }
}
```

---

## Visual Design

### Mask Icons
| Mask | Icon | Color Accent |
|------|------|--------------|
| Happy | 😊 Smiling face | Yellow #FFD700 |
| Sad | 😢 Crying face | Blue #4169E1 |
| Angry | 😠 Angry face | Red #DC143C |
| Neutral | 😐 Blank face | Gray #808080 |

### UI Color Scheme
```csharp
// Suggested colors
public static class MaskColors
{
    public static Color Happy = new Color(1f, 0.84f, 0f);    // Gold
    public static Color Sad = new Color(0.25f, 0.41f, 0.88f); // Royal Blue
    public static Color Angry = new Color(0.86f, 0.08f, 0.24f); // Crimson
    public static Color Neutral = new Color(0.5f, 0.5f, 0.5f);  // Gray
}
```

### Animation Specs
| Action | Animation | Duration |
|--------|-----------|----------|
| Mask Switch | Scale punch + flash | 0.2s |
| Slot Hover | Scale up to 1.1x | 0.1s |
| Slot Select | Frame pulse | 0.3s |
| Wheel Open | Scale from 0 | 0.15s |
| Wheel Close | Scale to 0 | 0.1s |
