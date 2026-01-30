# Personality & Interaction System Specification

## Overview
The mask system represents **personality traits** rather than simple emotions. NPCs have their own personality types, and compatibility between player mask and NPC personality determines the interaction outcome.

---

## Core Concept

```
┌─────────────────┐         ┌─────────────────┐
│  PLAYER MASK    │         │  NPC PERSONALITY│
│  (Personality)  │◄───────►│  (Trait Type)   │
└────────┬────────┘         └────────┬────────┘
         │                           │
         └───────────┬───────────────┘
                     │
                     ▼
            ┌────────────────┐
            │  COMPATIBILITY │
            │     MATRIX     │
            └────────┬───────┘
                     │
         ┌───────────┼───────────┐
         ▼           ▼           ▼
      POSITIVE    NEUTRAL    NEGATIVE
      (Happy)     (Okay)     (Upset)
```

---

## Player Personality Masks

### Core Masks (MVP)
| Mask | Trait | Description | Visual |
|------|-------|-------------|--------|
| **Agreeable** | Accommodating, friendly | Goes along with others, non-confrontational | Soft smile, open eyes |
| **Assertive** | Confident, direct | Takes charge, speaks mind | Strong expression, forward stance |
| **Analytical** | Logical, reserved | Thinks before acting, observes | Neutral, contemplative |
| **Expressive** | Emotional, enthusiastic | Shows feelings openly, energetic | Wide smile/frown, animated |

### Future Masks (Post-MVP)
| Mask | Trait | Unlocked By |
|------|-------|-------------|
| **Charming** | Persuasive, charismatic | Complete Level 5 |
| **Stoic** | Unreadable, calm | Complete with 0 detections |
| **Empathetic** | Understanding, supportive | Help 10 NPCs |
| **Intimidating** | Commanding, scary | Confront 5 Aggressive NPCs |

---

## NPC Personality Types

### Core Types (MVP)
| Type | Behavior | Preferred Interactions |
|------|----------|----------------------|
| **Dominant** | Assertive, controlling, takes charge | Wants agreement OR equal challenge |
| **Submissive** | Passive, follower, avoids conflict | Needs gentle guidance |
| **Friendly** | Warm, talkative, social | Appreciates openness |
| **Hostile** | Aggressive, distrustful, confrontational | Respects strength |
| **Neutral** | Indifferent, professional | Works with anyone |

### Future Types (Post-MVP)
| Type | Notes |
|------|-------|
| **Anxious** | Needs reassurance |
| **Narcissistic** | Needs admiration |
| **Curious** | Engages with analytical types |
| **Melancholic** | Connects with empathetic types |

---

## Compatibility Matrix

### Primary Matrix (MVP)

|  | Agreeable | Assertive | Analytical | Expressive |
|--|:---------:|:---------:|:----------:|:----------:|
| **Dominant** | ✓✓ Great | ✓ Okay | ✗ Bad | ~ Risky |
| **Submissive** | ✓✓ Great | ✗ Bad | ✓ Okay | ~ Risky |
| **Friendly** | ✓ Okay | ✓ Okay | ~ Risky | ✓✓ Great |
| **Hostile** | ✗ Bad | ✓✓ Great | ✓ Okay | ✗ Bad |
| **Neutral** | ✓ Okay | ✓ Okay | ✓✓ Great | ✓ Okay |

### Legend
| Symbol | Result | NPC Reaction | Effect |
|--------|--------|--------------|--------|
| ✓✓ | Great Match | Happy, cooperative | Pass freely |
| ✓ | Good Match | Neutral, accepts | Pass with brief pause |
| ~ | Risky | Suspicious | May trigger warning |
| ✗ | Bad Match | Upset, alert | Triggers detection |

---

## Interaction Types

### 1. Proximity (Range-Based) - MVP
The current system. Player enters NPC detection zone.

```csharp
public enum ProximityResult
{
    Ignored,      // Outside range
    Noticed,      // In range, being evaluated
    Accepted,     // Good match, can pass
    Suspicious,   // Risky match, warning
    Rejected      // Bad match, alert triggered
}
```

**Flow:**
```
Player enters range → NPC evaluates mask → Result applied
```

### 2. Direct Interaction (Future)
Player initiates conversation/action with NPC.

| Action | Description | Risk/Reward |
|--------|-------------|-------------|
| **Greet** | Friendly acknowledgment | Low risk, small positive |
| **Compliment** | Praise the NPC | Medium risk, high positive if matched |
| **Challenge** | Confront the NPC | High risk, required for some NPCs |
| **Ignore** | Walk past without engaging | Varies by NPC type |

### 3. Indirect Interaction (Future)
Actions that affect NPCs without direct contact.

| Action | Description | Effect |
|--------|-------------|--------|
| **Overheard** | NPC hears player talking to others | Reputation spreads |
| **Observed** | NPC sees player's actions | Affects future interactions |
| **Environmental** | Player affects NPC's space | Positive or negative based on action |

---

## Implementation

### PersonalityMask.cs
```csharp
public enum PersonalityMask
{
    None,
    Agreeable,
    Assertive,
    Analytical,
    Expressive,
    // Future
    Charming,
    Stoic,
    Empathetic,
    Intimidating
}

[CreateAssetMenu(fileName = "MaskData", menuName = "MaskCompany/Mask Data")]
public class MaskData : ScriptableObject
{
    public PersonalityMask maskType;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public Color accentColor;
    
    [Header("Compatibility")]
    public PersonalityCompatibility[] compatibilities;
}

[System.Serializable]
public class PersonalityCompatibility
{
    public NPCPersonalityType npcType;
    public CompatibilityLevel level;
}

public enum CompatibilityLevel
{
    Great,    // ✓✓
    Good,     // ✓
    Risky,    // ~
    Bad       // ✗
}
```

### NPCPersonality.cs
```csharp
public enum NPCPersonalityType
{
    Neutral,
    Dominant,
    Submissive,
    Friendly,
    Hostile,
    // Future
    Anxious,
    Narcissistic,
    Curious,
    Melancholic
}

[CreateAssetMenu(fileName = "NPCPersonality", menuName = "MaskCompany/NPC Personality")]
public class NPCPersonalityData : ScriptableObject
{
    public NPCPersonalityType personalityType;
    public string displayName;
    [TextArea] public string description;
    
    [Header("Behavior Modifiers")]
    public float detectionRadius = 3f;
    public float suspicionDuration = 2f;
    public float forgetDuration = 5f;
    
    [Header("Visual")]
    public Color auraColor;
    public Sprite personalityIcon;
}
```

### CompatibilityMatrix.cs
```csharp
[CreateAssetMenu(fileName = "CompatibilityMatrix", menuName = "MaskCompany/Compatibility Matrix")]
public class CompatibilityMatrix : ScriptableObject
{
    [System.Serializable]
    public class MatrixEntry
    {
        public PersonalityMask mask;
        public NPCPersonalityType npcType;
        public CompatibilityLevel result;
    }
    
    public MatrixEntry[] entries;
    
    private Dictionary<(PersonalityMask, NPCPersonalityType), CompatibilityLevel> _lookup;
    
    public void Initialize()
    {
        _lookup = new Dictionary<(PersonalityMask, NPCPersonalityType), CompatibilityLevel>();
        foreach (var entry in entries)
        {
            _lookup[(entry.mask, entry.npcType)] = entry.result;
        }
    }
    
    public CompatibilityLevel GetCompatibility(PersonalityMask mask, NPCPersonalityType npcType)
    {
        if (_lookup == null) Initialize();
        
        var key = (mask, npcType);
        if (_lookup.TryGetValue(key, out var result))
        {
            return result;
        }
        
        // Default: Neutral NPCs accept anyone
        if (npcType == NPCPersonalityType.Neutral)
            return CompatibilityLevel.Good;
            
        return CompatibilityLevel.Risky;
    }
}
```

### InteractionEvaluator.cs
```csharp
public class InteractionEvaluator : MonoBehaviour
{
    public static InteractionEvaluator Instance { get; private set; }
    
    public CompatibilityMatrix compatibilityMatrix;
    
    void Awake()
    {
        Instance = this;
        compatibilityMatrix.Initialize();
    }
    
    public InteractionResult Evaluate(
        PersonalityMask playerMask, 
        NPCPersonalityType npcPersonality,
        InteractionType interactionType = InteractionType.Proximity)
    {
        CompatibilityLevel baseLevel = compatibilityMatrix.GetCompatibility(playerMask, npcPersonality);
        
        // Modify based on interaction type (future feature)
        // Direct interactions might have higher stakes
        // Indirect might be more forgiving
        
        return new InteractionResult
        {
            compatibility = baseLevel,
            npcReaction = GetReaction(baseLevel),
            shouldAlert = baseLevel == CompatibilityLevel.Bad,
            shouldWarn = baseLevel == CompatibilityLevel.Risky
        };
    }
    
    NPCReaction GetReaction(CompatibilityLevel level)
    {
        return level switch
        {
            CompatibilityLevel.Great => NPCReaction.Happy,
            CompatibilityLevel.Good => NPCReaction.Neutral,
            CompatibilityLevel.Risky => NPCReaction.Suspicious,
            CompatibilityLevel.Bad => NPCReaction.Upset,
            _ => NPCReaction.Neutral
        };
    }
}

public struct InteractionResult
{
    public CompatibilityLevel compatibility;
    public NPCReaction npcReaction;
    public bool shouldAlert;
    public bool shouldWarn;
}

public enum NPCReaction
{
    Happy,
    Neutral,
    Suspicious,
    Upset
}

public enum InteractionType
{
    Proximity,
    Direct,
    Indirect
}
```

---

## NPC Reaction Behaviors

### Happy (Great Match)
```csharp
void OnGreatMatch()
{
    // Visual: Smile, positive emote
    ShowEmote(EmoteType.Heart);
    
    // Behavior: Move aside, wave
    StartCoroutine(FriendlyBehavior());
    
    // Audio: Positive sound
    AudioManager.Play("npc_happy");
    
    // Effect: Might help player (future)
}
```

### Neutral (Good Match)
```csharp
void OnGoodMatch()
{
    // Visual: Nod, brief acknowledgment
    ShowEmote(EmoteType.Nod);
    
    // Behavior: Continue normal activity
    // No change to patrol
    
    // Audio: Neutral sound
    AudioManager.Play("npc_acknowledge");
}
```

### Suspicious (Risky Match)
```csharp
void OnRiskyMatch()
{
    // Visual: Question mark, narrowed eyes
    ShowEmote(EmoteType.Question);
    
    // Behavior: Stop, observe player
    PausePatrol(suspicionDuration);
    FacePlayer();
    
    // Audio: Suspicious sound
    AudioManager.Play("npc_suspicious");
    
    // Effect: Extended detection if player lingers
}
```

### Upset (Bad Match)
```csharp
void OnBadMatch()
{
    // Visual: Exclamation, angry emote
    ShowEmote(EmoteType.Exclamation);
    
    // Behavior: Alert state
    TriggerAlert();
    
    // Audio: Alert sound
    AudioManager.Play("npc_alert");
    
    // Effect: Detection strike
    GameEvents.OnPlayerDetected?.Invoke();
}
```

---

## UI Considerations

### Mask Selection Hints
Show what personality types each mask works well with:

```
┌─────────────────────────────────────┐
│  AGREEABLE                          │
│  "Goes along with others"           │
│                                     │
│  ✓✓ Dominant, Submissive            │
│  ✓  Friendly, Neutral               │
│  ✗  Hostile                         │
└─────────────────────────────────────┘
```

### NPC Personality Indicator
Subtle visual cue showing NPC's personality:

| Personality | Visual Hint |
|-------------|-------------|
| Dominant | Stands tall, bold colors |
| Submissive | Hunched, muted colors |
| Friendly | Open posture, warm colors |
| Hostile | Crossed arms, sharp features |
| Neutral | Standard appearance |

---

## Gameplay Examples

### Scenario 1: The Dominant Boss
```
NPC: Manager with Dominant personality
Location: Corner office

Agreeable Mask: ✓✓ Great
  "Yes sir, right away!" - Boss feels respected, waves you through

Assertive Mask: ✓ Good  
  "I've got this handled." - Boss respects confidence, brief nod

Analytical Mask: ✗ Bad
  *Silence, calculating look* - Boss feels challenged, calls security

Expressive Mask: ~ Risky
  "Oh wow, your office is amazing!" - Boss is unsure, suspicious
```

### Scenario 2: The Anxious Intern (Future)
```
NPC: Intern with Submissive personality
Location: Copy room

Agreeable Mask: ✓✓ Great
  Gentle and supportive - Intern relaxes, moves aside

Assertive Mask: ✗ Bad
  Too intimidating - Intern panics, drops papers, attracts attention

Empathetic Mask: ✓✓ Great (Future)
  Understanding and kind - Intern becomes helpful ally
```

---

## Data-Driven Design

The compatibility matrix should be a **ScriptableObject** so designers can tweak relationships without code changes:

```
Assets/
└── ScriptableObjects/
    ├── Masks/
    │   ├── Mask_Agreeable.asset
    │   ├── Mask_Assertive.asset
    │   ├── Mask_Analytical.asset
    │   └── Mask_Expressive.asset
    ├── Personalities/
    │   ├── Personality_Dominant.asset
    │   ├── Personality_Submissive.asset
    │   ├── Personality_Friendly.asset
    │   ├── Personality_Hostile.asset
    │   └── Personality_Neutral.asset
    └── CompatibilityMatrix.asset
```

---

## Evolution Path

### Phase 1: MVP
- 4 masks (Agreeable, Assertive, Analytical, Expressive)
- 5 NPC types (Dominant, Submissive, Friendly, Hostile, Neutral)
- Proximity interaction only
- Binary outcome (pass/fail with warnings)

### Phase 2: Depth
- Add reputation system
- Direct interactions (talk, challenge)
- NPC relationships (some NPCs talk to each other)

### Phase 3: Complexity
- Indirect interactions
- Mask combinations
- NPC mood changes based on environment
- Multiple masks per level required

---

## Notes for Iteration

> "The matrix can be rebalanced anytime through the ScriptableObject. If Agreeable feels too powerful, reduce some ✓✓ to ✓. If Hostile NPCs are too hard, add more Good matches."

> "Interaction types are designed for expansion. Start with proximity, add direct when core loop is solid."
