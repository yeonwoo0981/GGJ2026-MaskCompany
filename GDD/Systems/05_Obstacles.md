# Obstacles System Specification

## Overview
Obstacles define the physical layout of levels and create navigation challenges.

---

## Obstacle Types

### Static Obstacles
Immovable objects that block movement.

| Type | Description | Collision |
|------|-------------|-----------|
| Walls | Room boundaries | Full block |
| Desks | Office furniture | Full block |
| Filing Cabinets | Storage | Full block |
| Cubicle Walls | Partial walls | Full block |
| Plants | Decorative | Small radius |
| Water Cooler | Break room | Small radius |

### Dynamic Obstacles
Objects that can change state.

| Type | Description | Interaction |
|------|-------------|-------------|
| Doors | Open/Close | E key or automatic |
| Sliding Doors | Auto-open when near | Proximity trigger |
| Swivel Chairs | Pushable | Physics push |

### Hazard Zones
Areas with special effects (no physical collision).

| Type | Effect | Visual |
|------|--------|--------|
| Slow Zone | Reduces player speed | Floor pattern |
| Noise Zone | Alerts nearby NPCs | Subtle highlight |
| Safe Zone | NPCs ignore player | Green tint |

---

## Implementation

### BaseObstacle
```csharp
public abstract class BaseObstacle : MonoBehaviour
{
    [Header("Obstacle Settings")]
    public bool blocksMovement = true;
    public bool blocksVision = true; // For NPC detection
    
    protected Collider2D obstacleCollider;
    
    protected virtual void Awake()
    {
        obstacleCollider = GetComponent<Collider2D>();
    }
}
```

### StaticObstacle
```csharp
public class StaticObstacle : BaseObstacle
{
    // Just uses collider for blocking
    // No additional logic needed
}
```

### Door
```csharp
public class Door : BaseObstacle
{
    [Header("Door Settings")]
    public bool isOpen = false;
    public bool autoClose = true;
    public float autoCloseDelay = 2f;
    public bool requiresInteraction = true;
    
    [Header("Animation")]
    public Sprite openSprite;
    public Sprite closedSprite;
    public float animationDuration = 0.2f;
    
    private SpriteRenderer spriteRenderer;
    private float closeTimer;
    
    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisual();
    }
    
    void Update()
    {
        if (autoClose && isOpen)
        {
            closeTimer -= Time.deltaTime;
            if (closeTimer <= 0)
            {
                Close();
            }
        }
    }
    
    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }
    
    public void Open()
    {
        if (isOpen) return;
        
        isOpen = true;
        closeTimer = autoCloseDelay;
        
        // Disable collision
        obstacleCollider.enabled = false;
        
        // Animate
        spriteRenderer.sprite = openSprite;
        transform.DOPunchScale(Vector3.one * 0.1f, animationDuration);
        
        AudioManager.Instance?.PlaySFX("DoorOpen");
    }
    
    public void Close()
    {
        if (!isOpen) return;
        
        isOpen = false;
        
        // Enable collision
        obstacleCollider.enabled = true;
        
        // Animate
        spriteRenderer.sprite = closedSprite;
        
        AudioManager.Instance?.PlaySFX("DoorClose");
    }
    
    void UpdateVisual()
    {
        spriteRenderer.sprite = isOpen ? openSprite : closedSprite;
        obstacleCollider.enabled = !isOpen;
    }
}
```

### AutoDoor (Proximity-based)
```csharp
public class AutoDoor : Door
{
    [Header("Auto Door")]
    public float detectionRadius = 1.5f;
    public LayerMask detectLayers;
    
    void Update()
    {
        // Check for nearby entities
        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            transform.position, 
            detectionRadius, 
            detectLayers
        );
        
        if (nearby.Length > 0 && !isOpen)
        {
            Open();
        }
        else if (nearby.Length == 0 && isOpen && autoClose)
        {
            Close();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
```

### HazardZone
```csharp
public abstract class HazardZone : MonoBehaviour
{
    public Color gizmoColor = Color.red;
    
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            OnPlayerEnter(other.GetComponent<PlayerController>());
        }
    }
    
    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            OnPlayerExit(other.GetComponent<PlayerController>());
        }
    }
    
    protected abstract void OnPlayerEnter(PlayerController player);
    protected abstract void OnPlayerExit(PlayerController player);
    
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}
```

### SlowZone
```csharp
public class SlowZone : HazardZone
{
    public float speedMultiplier = 0.5f;
    
    protected override void OnPlayerEnter(PlayerController player)
    {
        player.moveSpeed *= speedMultiplier;
    }
    
    protected override void OnPlayerExit(PlayerController player)
    {
        player.moveSpeed /= speedMultiplier;
    }
}
```

### SafeZone
```csharp
public class SafeZone : HazardZone
{
    protected override void OnPlayerEnter(PlayerController player)
    {
        player.GetComponent<PlayerDetectable>().isHidden = true;
    }
    
    protected override void OnPlayerExit(PlayerController player)
    {
        player.GetComponent<PlayerDetectable>().isHidden = false;
    }
}
```

---

## Interactable System

### IInteractable Interface
```csharp
public interface IInteractable
{
    void Interact(PlayerController player);
    string GetInteractPrompt();
    bool CanInteract(PlayerController player);
}
```

### Door with IInteractable
```csharp
public class InteractableDoor : Door, IInteractable
{
    public void Interact(PlayerController player)
    {
        Toggle();
    }
    
    public string GetInteractPrompt()
    {
        return isOpen ? "Close Door [E]" : "Open Door [E]";
    }
    
    public bool CanInteract(PlayerController player)
    {
        return requiresInteraction;
    }
}
```

### Player Interaction System
```csharp
public class PlayerInteraction : MonoBehaviour
{
    public float interactRadius = 1f;
    public LayerMask interactableLayers;
    
    private IInteractable currentInteractable;
    
    void Update()
    {
        FindNearbyInteractable();
        
        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            currentInteractable.Interact(GetComponent<PlayerController>());
        }
    }
    
    void FindNearbyInteractable()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(
            transform.position, 
            interactRadius, 
            interactableLayers
        );
        
        IInteractable closest = null;
        float closestDist = float.MaxValue;
        
        foreach (var col in nearby)
        {
            IInteractable interactable = col.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteract(GetComponent<PlayerController>()))
            {
                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = interactable;
                }
            }
        }
        
        currentInteractable = closest;
    }
}
```

---

## Tilemap Integration

### Obstacle Tiles
Use Rule Tiles for automatic wall/furniture placement.

```
Tiles/
├── Walls/
│   ├── Wall_RuleTile.asset
│   └── Wall_Sprites/
├── Furniture/
│   ├── Desk.png
│   ├── Chair.png
│   └── Cabinet.png
└── Floors/
    ├── Carpet.png
    └── Tile.png
```

### Tilemap Collider Setup
```csharp
// On the Walls tilemap:
// 1. Add TilemapCollider2D
// 2. Add CompositeCollider2D
// 3. On TilemapCollider2D, set "Used By Composite" = true
// 4. On Rigidbody2D, set Body Type = Static
```

---

## Prefab Organization

```
Prefabs/
├── Obstacles/
│   ├── Static/
│   │   ├── Desk.prefab
│   │   ├── FilingCabinet.prefab
│   │   └── Plant.prefab
│   ├── Dynamic/
│   │   ├── Door.prefab
│   │   ├── AutoDoor.prefab
│   │   └── PushableChair.prefab
│   └── Hazards/
│       ├── SlowZone.prefab
│       └── SafeZone.prefab
```
