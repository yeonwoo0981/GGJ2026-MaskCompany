# Mask Company - Game Design Documentation

> **Global Game Jam 2026** | Theme: Masks

## Quick Links

| Document            | Description                            |
| ------------------- | -------------------------------------- |
| [GDD.md](./GDD.md)     | Main Game Design Document - Start Here |
| [Systems/](./Systems/) | Detailed technical specifications      |

---

## System Documentation Index

### Core Systems

| #  | System                                    | File                                | Priority           |
| -- | ----------------------------------------- | ----------------------------------- | ------------------ |
| 01 | [Menu System](./Systems/01_Menu.md)          | Main menu, pause, game over screens | High               |
| 02 | [Map &amp; Level](./Systems/02_Map_Level.md) | Level structure, camera, tilemaps   | High               |
| 03 | [Player](./Systems/03_Player.md)             | Movement, mask system, input        | **Critical** |
| 04 | [NPC Physical](./Systems/04_NPC_Physical.md) | NPC movement, patrol, states        | **Critical** |
| 05 | [Obstacles](./Systems/05_Obstacles.md)       | Static/dynamic obstacles, hazards   | Medium             |

### Game Flow

| #  | System                                              | File                                 | Priority           |
| -- | --------------------------------------------------- | ------------------------------------ | ------------------ |
| 06 | [Level Flow](./Systems/06_LevelFlow.md)                | Start, win, lose, transitions        | High               |
| 07 | [Level Manager](./Systems/07_LevelManager.md)          | Level data, progression, saves       | High               |
| 08 | [NPC AI &amp; Emotion](./Systems/08_NPC_Emotion_AI.md) | Emotion states, detection, reactions | **Critical** |

### UI Systems

| #  | System                                           | File                             | Priority           |
| -- | ------------------------------------------------ | -------------------------------- | ------------------ |
| 09 | [Mask UI](./Systems/09_UI_Mask.md)                  | Mask display, selector, controls | **Critical** |
| 10 | [NPC UI](./Systems/10_UI_NPC.md)                    | Emotion indicators, alerts       | High               |
| 11 | [Game Progress UI](./Systems/11_UI_GameProgress.md) | HUD, level complete, game over   | High               |

### Advanced Systems

| #  | System                                                          | File                                          | Priority       |
| -- | --------------------------------------------------------------- | --------------------------------------------- | -------------- |
| 12 | [Personality & Interactions](./Systems/12_Personality_Interactions.md) | Mask traits, NPC personalities, compatibility | **Critical** |

---

## MVP Checklist

### Day 1 - Core Gameplay

- [x] Player 8-directional movement (WASD)
- [x] 4 personality masks (Agreeable, Assertive, Analytical, Expressive)
- [x] Mask switching (1, 2, 3, 4 keys)
- [x] Player color changes with mask
- [x] NPC with personality types (Dominant, Submissive, Friendly, Hostile, Neutral)
- [x] NPC detection system (proximity-based with gradient range indicator)
- [x] Compatibility matrix (mask + NPC personality = result)
- [x] **Gradual comfort system** (emotions evolve over time, not instant)
- [x] NPC breathing animation (DOTween, speed based on comfort)
- [x] NPC particles (configurable per emotion state)
- [x] ScriptableObjects: NPCConfig, NPCCollection, ParticleConfig
- [ ] Basic win condition (reach exit)
- [ ] Basic lose condition (bad match detected)

### Day 2 - Polish & Content

- [ ] 3 playable levels
- [ ] Level transitions
- [ ] Main menu
- [ ] Pause menu
- [ ] Level complete screen
- [ ] Game over screen
- [ ] NPC patrol behavior
- [x] Visual feedback (range color tints based on comfort)
- [x] NPC reaction particles (Great, Good, Neutral, Bad, VeryBad states)

### Day 3 - Extra

- [ ] Sound effects
- [ ] More NPC personality variants
- [ ] More levels
- [ ] Score/rating system
- [ ] Mask selection hints (show compatibility)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                       GAME MANAGER                          │
│                   (Persistent Singleton)                    │
├─────────────────────────────────────────────────────────────┤
│  LevelManager  │  AudioManager  │  SceneTransition          │
└────────┬───────┴────────────────┴───────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│                      LEVEL SCENE                            │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │    PLAYER    │  │     NPCs     │  │  ENVIRONMENT │      │
│  ├──────────────┤  ├──────────────┤  ├──────────────┤      │
│  │ Controller   │  │ Controller   │  │ Tilemaps     │      │
│  │ MaskSystem   │  │ EmotionState │  │ Obstacles    │      │
│  │ Detectable   │  │ Detection    │  │ Triggers     │      │
│  └──────────────┘  │ AI Brain     │  └──────────────┘      │
│                    └──────────────┘                         │
├─────────────────────────────────────────────────────────────┤
│                         UI LAYER                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   MaskUI     │  │    NPCUI     │  │  ProgressUI  │      │
│  │  (Controls)  │  │ (Indicators) │  │    (HUD)     │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

---

## Key Mechanics Summary

### Emotion Mask System (Inside Out Inspired)

**Player Masks:**
| Mask | Color | Key |
|------|-------|-----|
| 😄 **Joy** | Yellow | 1 |
| 😐 **Neutral** | Blue | 2 |
| 😠 **Anger** | Red | 3 |
| 😨 **Fear** | Purple | 4 |

**NPC Personalities & Compatibility:**

|  | Joy | Neutral | Anger | Fear |
|--|:---:|:-------:|:-----:|:----:|
| **Angry** | -- | + | o | + |
| **Cool** | ++ | - | - | - |
| **Weird** | + | - | o | - |
| **Loner** | o | + | - | o |
| **Lazy** | - | ++ | - | o |
| **Anxious** | - | + | -- | - |
| **Friendly** | + | - | - | - |
| **Scary** | - | o | + | ++ |

`++` Great (1.5x speed) | `+` Good (0.5x speed) | `o` Neutral (→0) | `-` Bad (0.5x speed) | `--` Very Bad (1.5x speed)

*Note: Neutral mask has half influence speed. Cool nerfed from `o` to `-` for Neutral mask.*

*Full details in [System 12](./Systems/12_Personality_Interactions.md)*

### Interaction Types
- **Proximity** (MVP): Enter NPC detection zone
- **Direct** (Future): Greet, compliment, challenge
- **Indirect** (Future): Overheard, observed

### Detection Flow

```
Player enters NPC detection zone
         │
         ▼
    ┌─────────────┐
    │ Check mask  │
    └──────┬──────┘
           │
    ┌──────┴──────┐
    │             │
    ▼             ▼
  Match        Mismatch
    │             │
    ▼             ▼
 Continue      Alert!
              (Strike)
                 │
                 ▼
           3 Strikes?
            ┌───┴───┐
            │       │
            ▼       ▼
           No    GAME OVER
            │
            ▼
         Continue
```

---

## Folder Structure (Target)

```
Assets/
├── Art/
│   ├── Sprites/
│   │   ├── Player/
│   │   ├── NPCs/
│   │   ├── Masks/
│   │   ├── Environment/
│   │   └── UI/
│   └── Tiles/
├── Audio/
│   ├── Music/
│   └── SFX/
├── Prefabs/
│   ├── Player/
│   ├── NPCs/
│   ├── Obstacles/
│   └── UI/
├── Scenes/
│   ├── MainMenu.unity
│   ├── Levels/
│   │   ├── Level_01.unity
│   │   ├── Level_02.unity
│   │   └── Level_03.unity
│   └── UI/ (additive)
├── Scripts/
│   ├── Core/
│   ├── Player/
│   ├── NPC/
│   ├── Level/
│   ├── UI/
│   └── Utils/
├── ScriptableObjects/
│   ├── LevelData/
│   └── NPCData/
└── Settings/
```

---

## Development Contacts

| Role | Name     | Folder                    |
| ---- | -------- | ------------------------- |
| Dev  | Yeonwoo  | `Assets/Work/Yeonwoo/`  |
| Dev  | barisflo | `Assets/Work/barisflo/` |

---

## External Resources

- **DOTween**: Animation library (already imported)
- **Input System**: Modern Unity input (already imported)
- **URP 2D**: Rendering pipeline (already configured)

---

## Version History

| Version | Date       | Changes             |
| ------- | ---------- | ------------------- |
| 0.1     | 2026-01-30 | Initial GDD created |
| 0.2     | 2026-01-30 | Added personality-based mask system, compatibility matrix, interaction types |
| 0.3     | 2026-01-30 | Implemented: PlayerController, NPCController, PersonalityTypes |
| 0.4     | 2026-01-30 | Added: NPCConfig, NPCCollection, ParticleConfig SOs, gradual comfort system, breathing animation, emotion particles |
