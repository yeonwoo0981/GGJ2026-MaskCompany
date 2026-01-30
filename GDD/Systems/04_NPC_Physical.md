# NPC Physical Behavior Specification

## Overview
NPCs are office workers that patrol or idle in the level. Their physical behavior is separate from their emotional AI system.

---

## NPC States (Physical)

```csharp
public enum NPCPhysicalState
{
    Idle,       // Standing still
    Patrol,     // Walking between points
    Suspicious, // Slowed, looking around
    Alert,      // Reacting to player
    Returning   // Going back to patrol
}
```

---

## Core Components

### NPCController
Base controller for all NPC physical behavior.

```csharp
public class NPCController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 5f;
    
    [Header("State")]
    public NPCPhysicalState currentState = NPCPhysicalState.Idle;
    
    [Header("Components")]
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Animator animator;
    
    protected Vector2 moveDirection;
    protected Vector2 facingDirection = Vector2.down;
    
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }
    
    protected virtual void Update()
    {
        UpdateBehavior();
    }
    
    protected virtual void FixedUpdate()
    {
        ApplyMovement();
    }
    
    protected virtual void UpdateBehavior()
    {
        switch (currentState)
        {
            case NPCPhysicalState.Idle:
                HandleIdle();
                break;
            case NPCPhysicalState.Patrol:
                HandlePatrol();
                break;
            case NPCPhysicalState.Suspicious:
                HandleSuspicious();
                break;
            case NPCPhysicalState.Alert:
                HandleAlert();
                break;
            case NPCPhysicalState.Returning:
                HandleReturning();
                break;
        }
    }
    
    protected virtual void ApplyMovement()
    {
        rb.velocity = moveDirection * moveSpeed;
        
        if (moveDirection.magnitude > 0.1f)
        {
            facingDirection = moveDirection.normalized;
        }
    }
    
    protected virtual void HandleIdle()
    {
        moveDirection = Vector2.zero;
    }
    
    protected virtual void HandlePatrol() { }
    protected virtual void HandleSuspicious() { }
    protected virtual void HandleAlert() { }
    protected virtual void HandleReturning() { }
    
    public void SetState(NPCPhysicalState newState)
    {
        if (currentState == newState) return;
        
        OnExitState(currentState);
        currentState = newState;
        OnEnterState(newState);
    }
    
    protected virtual void OnEnterState(NPCPhysicalState state) { }
    protected virtual void OnExitState(NPCPhysicalState state) { }
}
```

---

### PatrolBehavior
Handles waypoint-based patrol movement.

```csharp
public class PatrolBehavior : NPCController
{
    [Header("Patrol")]
    public Transform[] waypoints;
    public float waypointThreshold = 0.2f;
    public float waitTimeAtWaypoint = 1f;
    public bool loop = true;
    public bool pingPong = false;
    
    private int currentWaypointIndex = 0;
    private int direction = 1;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    
    protected override void HandlePatrol()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            SetState(NPCPhysicalState.Idle);
            return;
        }
        
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                MoveToNextWaypoint();
            }
            moveDirection = Vector2.zero;
            return;
        }
        
        Transform target = waypoints[currentWaypointIndex];
        Vector2 toTarget = (target.position - transform.position);
        
        if (toTarget.magnitude < waypointThreshold)
        {
            // Reached waypoint
            isWaiting = true;
            waitTimer = waitTimeAtWaypoint;
            moveDirection = Vector2.zero;
        }
        else
        {
            moveDirection = toTarget.normalized;
        }
    }
    
    void MoveToNextWaypoint()
    {
        if (pingPong)
        {
            currentWaypointIndex += direction;
            if (currentWaypointIndex >= waypoints.Length - 1 || currentWaypointIndex <= 0)
            {
                direction *= -1;
            }
        }
        else if (loop)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
        else
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
            {
                SetState(NPCPhysicalState.Idle);
            }
        }
    }
}
```

---

### IdleBehavior
Handles stationary NPCs with optional look-around.

```csharp
public class IdleBehavior : NPCController
{
    [Header("Idle")]
    public bool lookAround = true;
    public float lookInterval = 3f;
    public float lookDuration = 1f;
    
    private float lookTimer;
    private Vector2 originalFacing;
    
    protected override void Start()
    {
        base.Start();
        originalFacing = facingDirection;
        lookTimer = lookInterval;
    }
    
    protected override void HandleIdle()
    {
        base.HandleIdle();
        
        if (!lookAround) return;
        
        lookTimer -= Time.deltaTime;
        if (lookTimer <= 0)
        {
            StartCoroutine(LookAround());
            lookTimer = lookInterval + lookDuration;
        }
    }
    
    IEnumerator LookAround()
    {
        // Turn to random direction
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        facingDirection = randomDir;
        
        yield return new WaitForSeconds(lookDuration);
        
        // Return to original
        facingDirection = originalFacing;
    }
}
```

---

## Patrol Path Component

```csharp
public class PatrolPath : MonoBehaviour
{
    public Color gizmoColor = Color.yellow;
    public bool showInGame = false;
    
    public Transform[] GetWaypoints()
    {
        Transform[] waypoints = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            waypoints[i] = transform.GetChild(i);
        }
        return waypoints;
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        
        Transform[] waypoints = GetWaypoints();
        for (int i = 0; i < waypoints.Length; i++)
        {
            Gizmos.DrawWireSphere(waypoints[i].position, 0.2f);
            
            if (i < waypoints.Length - 1)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
    }
}
```

---

## NPC Types

### Office Worker (Standard)
- **Behavior:** Patrol between desk and break room
- **Speed:** Medium
- **Detection:** Standard radius

### Manager
- **Behavior:** Patrol wider area, longer looks
- **Speed:** Slow
- **Detection:** Larger radius

### Intern
- **Behavior:** Quick movement, less observant
- **Speed:** Fast
- **Detection:** Smaller radius, easily distracted

### Security (Boss?)
- **Behavior:** Methodical patrol, high alertness
- **Speed:** Slow but steady
- **Detection:** Large cone, immediate reaction

---

## NPC Prefab Setup

### Hierarchy
```
NPC_Worker
├── Body (SpriteRenderer)
├── EmotionIndicator (SpriteRenderer - shows current emotion)
├── DetectionZone (Collider2D, trigger)
│   └── DetectionVisual (Optional - shows cone in debug)
└── PatrolPath (Optional - can be external)
```

### Required Components
| Component | Purpose |
|-----------|---------|
| Rigidbody2D | Physics (Dynamic, Gravity 0, Freeze Z) |
| CircleCollider2D | Physical collision |
| NPCController | Base behavior |
| PatrolBehavior or IdleBehavior | Movement pattern |
| NPCEmotionState | Emotion system (see emotion doc) |
| NPCDetection | Player detection |

---

## Collision Layers

| Layer | Collides With |
|-------|---------------|
| Player | Walls, Furniture, NPCs |
| NPC | Walls, Furniture, Player |
| Walls | Everything |
| Triggers | Player only |

### Layer Setup
```csharp
// In Project Settings > Tags and Layers
Layer 8: Player
Layer 9: NPC
Layer 10: Walls
Layer 11: Triggers
Layer 12: Furniture
```

---

## Animation

### Simple 4-Direction
```csharp
public class NPCAnimator : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Sprite[] idleSprites; // Down, Up, Left, Right
    public Sprite[] walkSprites; // Same order, multiple frames
    
    private NPCController controller;
    
    void Update()
    {
        int dirIndex = GetDirectionIndex(controller.facingDirection);
        
        if (controller.moveDirection.magnitude > 0.1f)
        {
            // Walking animation
            AnimateWalk(dirIndex);
        }
        else
        {
            spriteRenderer.sprite = idleSprites[dirIndex];
        }
    }
    
    int GetDirectionIndex(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            return dir.x > 0 ? 3 : 2; // Right : Left
        }
        else
        {
            return dir.y > 0 ? 1 : 0; // Up : Down
        }
    }
}
```
