# Player System Specification

## Overview
The player is the main character who must navigate the office while switching masks to match NPC emotional states.

---

## Components

### PlayerController
Handles movement and basic input.

```csharp
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float acceleration = 10f;
    public float deceleration = 10f;
    
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    void Update()
    {
        // Using new Input System
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
    }
    
    void FixedUpdate()
    {
        Move();
    }
    
    void Move()
    {
        Vector2 targetVelocity = moveInput * moveSpeed;
        
        // Smooth acceleration/deceleration
        if (moveInput.magnitude > 0.1f)
        {
            currentVelocity = Vector2.MoveTowards(
                currentVelocity, 
                targetVelocity, 
                acceleration * Time.fixedDeltaTime
            );
        }
        else
        {
            currentVelocity = Vector2.MoveTowards(
                currentVelocity, 
                Vector2.zero, 
                deceleration * Time.fixedDeltaTime
            );
        }
        
        rb.velocity = currentVelocity;
    }
}
```

---

### PlayerMaskSystem
Manages mask inventory and switching.

```csharp
public enum MaskType
{
    None,
    Happy,
    Sad,
    Angry,
    Neutral
}

public class PlayerMaskSystem : MonoBehaviour
{
    [Header("Masks")]
    public MaskType currentMask = MaskType.Neutral;
    public List<MaskType> availableMasks = new List<MaskType>();
    
    [Header("Switching")]
    public float switchCooldown = 0f; // 0 for instant (game jam)
    private float lastSwitchTime;
    
    [Header("Visuals")]
    public SpriteRenderer maskRenderer;
    public Sprite[] maskSprites; // Index matches MaskType enum
    
    [Header("Events")]
    public UnityEvent<MaskType> onMaskChanged;
    
    void Start()
    {
        // Initialize with starting masks
        if (!availableMasks.Contains(currentMask))
        {
            availableMasks.Add(currentMask);
        }
        UpdateVisuals();
    }
    
    void Update()
    {
        HandleMaskInput();
    }
    
    void HandleMaskInput()
    {
        // Scroll wheel
        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0) CycleMask(1);
        if (scroll < 0) CycleMask(-1);
        
        // Number keys
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetMask(MaskType.Happy);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetMask(MaskType.Sad);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetMask(MaskType.Angry);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetMask(MaskType.Neutral);
    }
    
    public void CycleMask(int direction)
    {
        if (availableMasks.Count <= 1) return;
        if (!CanSwitch()) return;
        
        int currentIndex = availableMasks.IndexOf(currentMask);
        int newIndex = (currentIndex + direction + availableMasks.Count) % availableMasks.Count;
        
        SetMask(availableMasks[newIndex]);
    }
    
    public void SetMask(MaskType mask)
    {
        if (!availableMasks.Contains(mask)) return;
        if (!CanSwitch()) return;
        if (currentMask == mask) return;
        
        currentMask = mask;
        lastSwitchTime = Time.time;
        
        UpdateVisuals();
        onMaskChanged?.Invoke(currentMask);
        
        // Play switch sound
        AudioManager.Instance?.PlaySFX("MaskSwitch");
    }
    
    bool CanSwitch()
    {
        return Time.time >= lastSwitchTime + switchCooldown;
    }
    
    void UpdateVisuals()
    {
        if (maskRenderer != null && maskSprites.Length > (int)currentMask)
        {
            maskRenderer.sprite = maskSprites[(int)currentMask];
        }
    }
    
    public void AddMask(MaskType mask)
    {
        if (!availableMasks.Contains(mask))
        {
            availableMasks.Add(mask);
        }
    }
}
```

---

### Player Detection Collider
Used by NPCs to detect player presence.

```csharp
public class PlayerDetectable : MonoBehaviour
{
    public PlayerMaskSystem maskSystem;
    
    public MaskType GetCurrentMask()
    {
        return maskSystem.currentMask;
    }
}
```

---

## Player Setup

### GameObject Hierarchy
```
Player
├── Body (SpriteRenderer - body sprite)
├── Mask (SpriteRenderer - mask sprite, child of body)
├── DetectionTrigger (CircleCollider2D, isTrigger=true)
└── InteractionTrigger (CircleCollider2D, isTrigger=true)
```

### Required Components
| Component | Settings |
|-----------|----------|
| Rigidbody2D | Body Type: Dynamic, Gravity Scale: 0, Freeze Rotation Z |
| CircleCollider2D | For physics collision |
| PlayerController | Movement script |
| PlayerMaskSystem | Mask management |
| PlayerDetectable | For NPC detection |

---

## Input Actions

Using Unity's new Input System:

```json
{
    "name": "Player",
    "maps": [
        {
            "name": "Player",
            "actions": [
                {
                    "name": "Move",
                    "type": "Value",
                    "expectedControlType": "Vector2"
                },
                {
                    "name": "CycleMaskNext",
                    "type": "Button"
                },
                {
                    "name": "CycleMaskPrev",
                    "type": "Button"
                },
                {
                    "name": "SelectMask1",
                    "type": "Button"
                },
                {
                    "name": "SelectMask2",
                    "type": "Button"
                },
                {
                    "name": "SelectMask3",
                    "type": "Button"
                },
                {
                    "name": "SelectMask4",
                    "type": "Button"
                },
                {
                    "name": "Interact",
                    "type": "Button"
                },
                {
                    "name": "Pause",
                    "type": "Button"
                }
            ]
        }
    ]
}
```

---

## Animation States

### State Machine
```
Idle
├── Idle_Happy
├── Idle_Sad
├── Idle_Angry
└── Idle_Neutral

Walk
├── Walk_Happy
├── Walk_Sad
├── Walk_Angry
└── Walk_Neutral
```

### Animation Parameters
| Parameter | Type | Purpose |
|-----------|------|---------|
| Speed | Float | Blend idle/walk |
| MaskType | Int | Select mask animation set |
| DirectionX | Float | Horizontal facing |
| DirectionY | Float | Vertical facing |

### Simple Implementation (Game Jam)
For time constraints, use single sprite with mask overlay:

```csharp
public class PlayerAnimator : MonoBehaviour
{
    public SpriteRenderer bodyRenderer;
    public Sprite[] walkFrames;
    public float frameRate = 8f;
    
    private int currentFrame;
    private float timer;
    
    void Update()
    {
        if (IsMoving())
        {
            timer += Time.deltaTime;
            if (timer >= 1f / frameRate)
            {
                timer = 0;
                currentFrame = (currentFrame + 1) % walkFrames.Length;
                bodyRenderer.sprite = walkFrames[currentFrame];
            }
        }
    }
}
```

---

## Player Feedback

### Visual Feedback
- **Mask Switch:** Brief flash/pulse effect
- **Detection Warning:** Screen edge vignette
- **Damage/Mismatch:** Screen shake + red flash

### Implementation
```csharp
public class PlayerFeedback : MonoBehaviour
{
    public void OnMaskSwitch()
    {
        // Quick scale punch
        transform.DOPunchScale(Vector3.one * 0.1f, 0.15f);
    }
    
    public void OnDetected()
    {
        // Camera shake
        Camera.main.DOShakePosition(0.3f, 0.2f);
    }
}
```
