# Mask Company - Game Design Document

> **Game Jam:** Global Game Jam 2026  
> **Theme:** Masks  
> **Genre:** Top-down stealth/social simulation  
> **Inspiration:** Hotline Miami (visual style, top-down perspective)

---

## 1. Game Overview

### 1.1 Concept
You are an employee navigating a dystopian office where everyone wears masks to survive. Your survival depends on wearing the right mask for the right person—match their emotional state or face the consequences.

### 1.2 Core Loop
1. Enter a room/area
2. Observe NPCs and their emotional states
3. Switch to the appropriate mask before interacting
4. Navigate through obstacles to reach the exit
5. Complete the level and advance

### 1.3 Win/Lose Conditions
- **Win:** Reach the level exit without being "caught" (emotional mismatch detected)
- **Lose:** Too many emotional mismatches trigger game over

---

## 2. Systems Overview

### 2.1 Menu System
| Screen | Description |
|--------|-------------|
| Main Menu | Start Game, Options, Credits, Quit |
| Pause Menu | Resume, Restart Level, Options, Main Menu |
| Level Select | (Optional) Unlock-based level selection |
| Game Over | Retry, Main Menu |
| Level Complete | Next Level, Main Menu |

### 2.2 Map/Level System
- **Perspective:** Top-down 2D (Hotline Miami style)
- **Environment:** Office setting (cubicles, hallways, meeting rooms, break rooms)
- **Tilemap-based** level design for rapid iteration
- Each level is a self-contained office floor/section

### 2.3 Player System
| Component | Description |
|-----------|-------------|
| Movement | 8-directional movement using WASD/Arrow keys |
| Speed | Base speed with potential mask modifiers |
| Current Mask | Tracks which mask the player is wearing |
| Detection Radius | Area where NPCs can "see" the player's mask |

### 2.4 NPC System
| Component | Description |
|-----------|-------------|
| Patrol/Idle | Basic physical behavior (waypoints, idle zones) |
| Emotion State | Current emotional state (Happy, Sad, Angry, Neutral) |
| Detection | Cone or radius-based player detection |
| Reaction | Response to mask match/mismatch |

### 2.5 Obstacle System
| Type | Description |
|------|-------------|
| Static | Desks, walls, filing cabinets (block movement) |
| Dynamic | Doors (openable), moving obstacles |
| Hazards | Areas that affect player (slow zones, etc.) |

### 2.6 Level Flow System
```
Level Start → Gameplay → Goal Reached → Level Complete → Load Next Level
                ↓
           Game Over → Retry/Menu
```

### 2.7 Level Manager
- Defines level structure (boundaries, spawn points, exit location)
- Manages NPC spawning (which NPC types, where, with what emotions)
- Handles level transitions
- Stores level metadata (par time, difficulty, etc.)

### 2.8 NPC AI/Behavior System
| Emotion | Behavior | Required Player Mask |
|---------|----------|---------------------|
| Happy | Friendly patrol, wider tolerance | Happy Mask |
| Sad | Slow movement, narrow detection | Sad Mask |
| Angry | Aggressive patrol, fast, wide detection | Angry Mask |
| Neutral | Standard behavior | Any mask (safe) |

### 2.9 UI Systems

#### Mask Controls & State
- Current mask indicator (visual icon)
- Mask wheel/selector (mouse wheel or number keys)
- Available masks inventory
- Cooldown indicator (if mask switching has cooldown)

#### NPC State Display
- Emotion indicators above NPCs (subtle icons)
- Detection warning (when player is about to be seen)
- Alert state indicator

#### Game Progress UI
- Current level indicator
- Timer (optional)
- Score/rating system
- Objective tracker

---

## 3. Masks

### 3.1 Mask Types
| Mask | Visual | Effect |
|------|--------|--------|
| Happy | 😊 Smiling | Matches happy NPCs |
| Sad | 😢 Crying | Matches sad NPCs |
| Angry | 😠 Frowning | Matches angry NPCs |
| Neutral | 😐 Blank | Safe with neutral NPCs, risky with others |

### 3.2 Mask Mechanics
- **Instant Switch:** Player can change masks at any time
- **No Cooldown (MVP):** For game jam simplicity
- **Visual Feedback:** Player sprite changes to show current mask

---

## 4. Technical Architecture

### 4.1 Scene Structure
```
Scenes/
├── MainMenu.unity
├── Game/
│   ├── Level_01.unity
│   ├── Level_02.unity
│   └── ...
└── UI/
    └── SharedUI.unity (additive loading)
```

### 4.2 Core Scripts Structure
```
Scripts/
├── Core/
│   ├── GameManager.cs
│   ├── LevelManager.cs
│   └── SceneLoader.cs
├── Player/
│   ├── PlayerController.cs
│   ├── PlayerMaskSystem.cs
│   └── PlayerDetection.cs
├── NPC/
│   ├── NPCController.cs
│   ├── NPCBehavior.cs
│   ├── NPCEmotionState.cs
│   └── NPCDetectionSystem.cs
├── Level/
│   ├── LevelData.cs (ScriptableObject)
│   ├── LevelGoal.cs
│   └── SpawnPoint.cs
├── UI/
│   ├── MaskUI.cs
│   ├── NPCUI.cs
│   ├── GameProgressUI.cs
│   └── MenuUI.cs
└── Obstacles/
    ├── Obstacle.cs
    └── Door.cs
```

### 4.3 Data Structures

#### LevelData (ScriptableObject)
```csharp
[CreateAssetMenu]
public class LevelData : ScriptableObject
{
    public string levelName;
    public int levelIndex;
    public List<NPCSpawnData> npcSpawns;
    public Vector2 playerSpawnPoint;
    public Vector2 goalPosition;
}
```

#### NPCSpawnData
```csharp
[System.Serializable]
public class NPCSpawnData
{
    public NPCType npcType;
    public EmotionType initialEmotion;
    public Vector2 spawnPosition;
    public List<Vector2> patrolPoints;
}
```

---

## 5. Art Style

### 5.1 Visual Direction
- **Hotline Miami inspired:** Gritty, neon-accented, top-down pixel art
- **Color Palette:** Muted office colors with vibrant mask accents
- **Perspective:** Strict top-down (no perspective tilt)

### 5.2 Key Visual Elements
| Element | Style |
|---------|-------|
| Player | Simple humanoid, mask is prominent |
| NPCs | Varied office workers, distinct emotions via body language + mask |
| Environment | Cubicles, desks, plants, water coolers |
| UI | Clean, minimalist with mask iconography |

---

## 6. Audio (Scope Dependent)

### 6.1 Sound Effects
- Mask switching sound
- Footsteps
- NPC alert/detection
- Level complete jingle
- Game over sting

### 6.2 Music
- Tense ambient office music
- Hotline Miami-esque synth (if time permits)

---

## 7. MVP Scope (Game Jam Priority)

### Must Have (Day 1-2)
- [ ] Player movement (top-down)
- [ ] Basic mask switching (2-3 masks)
- [ ] NPC with emotion state
- [ ] NPC detection of player mask
- [ ] Match/mismatch feedback
- [ ] One complete level
- [ ] Win/lose conditions
- [ ] Basic UI (current mask, level complete)

### Should Have (Day 2-3)
- [ ] Multiple levels (3+)
- [ ] Level Manager with transitions
- [ ] Multiple NPC types
- [ ] Obstacles (static)
- [ ] Main Menu
- [ ] Pause Menu
- [ ] Polish (visual feedback, particles)

### Nice to Have (If Time)
- [ ] NPC patrol paths
- [ ] Dynamic obstacles
- [ ] Sound effects
- [ ] Music
- [ ] Score system
- [ ] Level select

---

## 8. Controls

### Keyboard + Mouse
| Input | Action |
|-------|--------|
| WASD / Arrows | Move |
| Mouse Wheel | Cycle masks |
| 1, 2, 3, 4 | Direct mask selection |
| ESC | Pause menu |
| E / Space | Interact (doors, etc.) |

### Gamepad (Optional)
| Input | Action |
|-------|--------|
| Left Stick | Move |
| Bumpers (L/R) | Cycle masks |
| Start | Pause |
| A | Interact |

---

## 9. Development Notes

### Dependencies
- Unity 2D URP (already configured)
- DOTween (already imported) - for smooth animations
- Input System (already imported) - for modern input handling

### Team Workflow
- Work folders per team member (`Assets/Work/[Name]/`)
- Shared scripts in `Assets/Scripts/`
- Shared art in `Assets/Art/`
- Levels in `Assets/Scenes/Levels/`

---

## 10. Future Considerations (Post-Jam)

- Multiple mask abilities (not just emotion matching)
- Boss NPCs with complex patterns
- Story/narrative elements
- Combo systems for fast mask switching
- Multiplayer (co-op office infiltration)
