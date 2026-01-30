# Map & Level System Specification

## Overview
Levels are top-down office environments where players navigate while managing their masks.

---

## Level Structure

### Visual Style
- **Perspective:** Pure top-down (90° angle)
- **Art Style:** Pixel art inspired by Hotline Miami
- **Tile Size:** 32x32 or 16x16 pixels
- **Camera:** Orthographic, follows player with slight smoothing

### Environment Elements

| Layer | Sorting Order | Contents |
|-------|---------------|----------|
| Floor | 0 | Carpet, tiles, floor patterns |
| Floor Details | 1 | Shadows, stains, cables |
| Walls/Furniture | 10 | Desks, walls, cabinets |
| Characters | 20 | Player, NPCs |
| Overhead | 30 | Ceiling lights, signs (optional) |
| UI | 100 | World-space UI elements |

---

## Tilemap Setup

### Required Tilemaps
```
Grid (parent)
├── Floor (Tilemap)
├── FloorDecor (Tilemap)
├── Walls (Tilemap + TilemapCollider2D)
├── Furniture (Tilemap + TilemapCollider2D)
└── Triggers (Tilemap - invisible)
```

### Collision Setup
- Walls: `TilemapCollider2D` + `CompositeCollider2D` (for performance)
- Furniture: Same as walls
- Player/NPC: `Rigidbody2D` + `CircleCollider2D`

---

## Level Components

### Level Boundaries
```csharp
public class LevelBounds : MonoBehaviour
{
    public Collider2D boundsCollider;
    
    // Used by camera to clamp position
    public Bounds GetBounds()
    {
        return boundsCollider.bounds;
    }
}
```

### Player Spawn Point
```csharp
public class PlayerSpawnPoint : MonoBehaviour
{
    public MaskType startingMask = MaskType.Neutral;
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
```

### Level Goal/Exit
```csharp
public class LevelGoal : MonoBehaviour
{
    public UnityEvent onGoalReached;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            onGoalReached?.Invoke();
            GameManager.Instance.LevelComplete();
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one);
    }
}
```

### NPC Spawn Points
```csharp
public class NPCSpawnPoint : MonoBehaviour
{
    public NPCType npcType;
    public EmotionType emotion;
    public PatrolPath patrolPath; // Optional
    
    void OnDrawGizmos()
    {
        Gizmos.color = GetEmotionColor(emotion);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
```

---

## Camera System

### CameraFollow.cs
```csharp
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Bounds")]
    public bool useBounds = true;
    public LevelBounds levelBounds;
    
    private Camera cam;
    
    void Start()
    {
        cam = GetComponent<Camera>();
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position, 
            desiredPosition, 
            smoothSpeed * Time.deltaTime
        );
        
        if (useBounds && levelBounds != null)
        {
            smoothedPosition = ClampToBounds(smoothedPosition);
        }
        
        transform.position = smoothedPosition;
    }
    
    Vector3 ClampToBounds(Vector3 pos)
    {
        Bounds bounds = levelBounds.GetBounds();
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;
        
        pos.x = Mathf.Clamp(pos.x, bounds.min.x + camWidth, bounds.max.x - camWidth);
        pos.y = Mathf.Clamp(pos.y, bounds.min.y + camHeight, bounds.max.y - camHeight);
        
        return pos;
    }
}
```

---

## Level Design Guidelines

### Office Room Types

| Room | Purpose | NPCs | Difficulty |
|------|---------|------|------------|
| Cubicle Area | Main navigation | 2-4 | Medium |
| Hallway | Connections | 0-2 | Easy |
| Meeting Room | Bottleneck | 1-2 | Hard |
| Break Room | Rest area | 1-3 | Medium |
| Boss Office | End goal | 1 | Hard |
| Copy Room | Small space | 1 | Easy |

### Level Progression

**Level 1: Tutorial**
- Single room, 1 NPC
- Clear path to exit
- Teaches mask switching

**Level 2: Introduction**
- 2 rooms, 2-3 NPCs
- Multiple emotion types
- Simple patrol

**Level 3+: Full Gameplay**
- Multiple rooms
- Various NPC types
- Patrol paths
- Strategic mask management

---

## Level Data (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "LevelData", menuName = "MaskCompany/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Info")]
    public string levelName;
    public int levelIndex;
    public string sceneName;
    
    [Header("Requirements")]
    public MaskType[] availableMasks;
    
    [Header("Optional")]
    public float parTime; // For scoring
    public string hint;
}
```

---

## Scene Hierarchy Template

```
Level_01
├── --- SETUP ---
├── GameManager (or reference)
├── LevelManager
├── EventSystem
│
├── --- ENVIRONMENT ---
├── Grid
│   ├── Floor
│   ├── Walls
│   └── Furniture
├── LevelBounds
│
├── --- ENTITIES ---
├── Player
├── NPCs
│   ├── NPC_01
│   └── NPC_02
│
├── --- TRIGGERS ---
├── PlayerSpawn
├── LevelGoal
│
├── --- CAMERA ---
├── Main Camera
│
└── --- UI ---
    └── Canvas (or loaded additively)
```
