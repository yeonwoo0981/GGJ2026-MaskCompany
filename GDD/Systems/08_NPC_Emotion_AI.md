# NPC Emotion & AI System Specification

## Overview
NPCs have emotional states that determine how they react to the player's mask. The AI system processes these emotions to drive behavior.

---

## Emotion System

### Emotion Types
```csharp
public enum EmotionType
{
    Neutral,    // Accepts any mask (safe)
    Happy,      // Requires Happy mask
    Sad,        // Requires Sad mask
    Angry,      // Requires Angry mask
    Fearful,    // Future: Requires Calm mask
    Suspicious  // Future: More complex logic
}
```

### Emotion-Mask Compatibility Matrix

| NPC Emotion | Happy Mask | Sad Mask | Angry Mask | Neutral Mask |
|-------------|------------|----------|------------|--------------|
| Neutral     | ✓ OK       | ✓ OK     | ✓ OK       | ✓ OK         |
| Happy       | ✓ Match    | ✗ Fail   | ✗ Fail     | ~ Warning    |
| Sad         | ✗ Fail     | ✓ Match  | ✗ Fail     | ~ Warning    |
| Angry       | ✗ Fail     | ✗ Fail   | ✓ Match    | ~ Warning    |

---

## Core Components

### NPCEmotionState
```csharp
public class NPCEmotionState : MonoBehaviour
{
    [Header("Emotion")]
    public EmotionType currentEmotion = EmotionType.Neutral;
    
    [Header("Modifiers")]
    public bool canChangeEmotion = false;
    public float emotionChangeInterval = 10f;
    public EmotionType[] possibleEmotions;
    
    [Header("Visual")]
    public SpriteRenderer emotionIndicator;
    public Sprite[] emotionSprites; // Indexed by EmotionType
    public Color[] emotionColors;
    
    [Header("Events")]
    public UnityEvent<EmotionType> onEmotionChanged;
    
    private float emotionTimer;
    
    void Start()
    {
        UpdateVisuals();
        emotionTimer = emotionChangeInterval;
    }
    
    void Update()
    {
        if (canChangeEmotion)
        {
            emotionTimer -= Time.deltaTime;
            if (emotionTimer <= 0)
            {
                RandomizeEmotion();
                emotionTimer = emotionChangeInterval;
            }
        }
    }
    
    public void SetEmotion(EmotionType emotion)
    {
        if (currentEmotion == emotion) return;
        
        currentEmotion = emotion;
        UpdateVisuals();
        onEmotionChanged?.Invoke(emotion);
    }
    
    void RandomizeEmotion()
    {
        if (possibleEmotions == null || possibleEmotions.Length == 0) return;
        
        EmotionType newEmotion = possibleEmotions[Random.Range(0, possibleEmotions.Length)];
        SetEmotion(newEmotion);
    }
    
    void UpdateVisuals()
    {
        if (emotionIndicator != null && emotionSprites.Length > (int)currentEmotion)
        {
            emotionIndicator.sprite = emotionSprites[(int)currentEmotion];
        }
        
        if (emotionColors.Length > (int)currentEmotion)
        {
            emotionIndicator.color = emotionColors[(int)currentEmotion];
        }
    }
    
    public MaskType GetRequiredMask()
    {
        return currentEmotion switch
        {
            EmotionType.Happy => MaskType.Happy,
            EmotionType.Sad => MaskType.Sad,
            EmotionType.Angry => MaskType.Angry,
            EmotionType.Neutral => MaskType.None, // Any mask works
            _ => MaskType.None
        };
    }
}
```

---

## Detection System

### NPCDetection
```csharp
public class NPCDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 3f;
    public float detectionAngle = 90f; // Cone angle
    public LayerMask playerLayer;
    
    [Header("Timing")]
    public float detectionDelay = 0.5f; // Time before reacting
    public float forgetTime = 2f; // Time to forget player
    
    [Header("State")]
    public bool playerDetected = false;
    public bool playerInRange = false;
    
    [Header("References")]
    public NPCEmotionState emotionState;
    public NPCController controller;
    public Transform eyePoint; // Detection origin
    
    [Header("Events")]
    public UnityEvent<MaskMatchResult> onPlayerDetected;
    public UnityEvent onPlayerLost;
    
    private PlayerDetectable detectedPlayer;
    private float detectionTimer;
    private float forgetTimer;
    
    void Update()
    {
        CheckForPlayer();
        ProcessDetection();
    }
    
    void CheckForPlayer()
    {
        Vector2 origin = eyePoint != null ? eyePoint.position : transform.position;
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, detectionRadius, playerLayer);
        
        playerInRange = false;
        detectedPlayer = null;
        
        foreach (var hit in hits)
        {
            PlayerDetectable player = hit.GetComponent<PlayerDetectable>();
            if (player == null) continue;
            if (player.isHidden) continue;
            
            // Check if in cone
            Vector2 toPlayer = (hit.transform.position - transform.position).normalized;
            float angle = Vector2.Angle(controller.facingDirection, toPlayer);
            
            if (angle <= detectionAngle / 2f)
            {
                // Check line of sight
                if (HasLineOfSight(origin, hit.transform.position))
                {
                    playerInRange = true;
                    detectedPlayer = player;
                    break;
                }
            }
        }
    }
    
    bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        RaycastHit2D hit = Physics2D.Linecast(from, to, ~playerLayer);
        return hit.collider == null;
    }
    
    void ProcessDetection()
    {
        if (playerInRange && detectedPlayer != null)
        {
            forgetTimer = forgetTime;
            
            if (!playerDetected)
            {
                detectionTimer += Time.deltaTime;
                
                if (detectionTimer >= detectionDelay)
                {
                    // Player detected!
                    playerDetected = true;
                    EvaluateMask();
                }
            }
        }
        else
        {
            detectionTimer = 0;
            
            if (playerDetected)
            {
                forgetTimer -= Time.deltaTime;
                
                if (forgetTimer <= 0)
                {
                    playerDetected = false;
                    onPlayerLost?.Invoke();
                }
            }
        }
    }
    
    void EvaluateMask()
    {
        if (detectedPlayer == null) return;
        
        MaskType playerMask = detectedPlayer.GetCurrentMask();
        MaskType requiredMask = emotionState.GetRequiredMask();
        
        MaskMatchResult result = EvaluateMatch(playerMask, requiredMask);
        onPlayerDetected?.Invoke(result);
    }
    
    MaskMatchResult EvaluateMatch(MaskType playerMask, MaskType required)
    {
        // Neutral NPC accepts any mask
        if (required == MaskType.None)
        {
            return MaskMatchResult.Match;
        }
        
        // Neutral mask is risky but not instant fail
        if (playerMask == MaskType.Neutral)
        {
            return MaskMatchResult.Warning;
        }
        
        // Direct match
        if (playerMask == required)
        {
            return MaskMatchResult.Match;
        }
        
        // Mismatch
        return MaskMatchResult.Mismatch;
    }
    
    void OnDrawGizmosSelected()
    {
        Vector3 origin = eyePoint != null ? eyePoint.position : transform.position;
        Vector2 facing = controller != null ? controller.facingDirection : Vector2.down;
        
        // Detection radius
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(origin, detectionRadius);
        
        // Detection cone
        Gizmos.color = Color.yellow;
        Vector3 leftDir = Quaternion.Euler(0, 0, detectionAngle / 2) * facing;
        Vector3 rightDir = Quaternion.Euler(0, 0, -detectionAngle / 2) * facing;
        
        Gizmos.DrawLine(origin, origin + leftDir * detectionRadius);
        Gizmos.DrawLine(origin, origin + rightDir * detectionRadius);
    }
}

public enum MaskMatchResult
{
    Match,    // Correct mask
    Mismatch, // Wrong mask - alert!
    Warning   // Risky (neutral mask with emotional NPC)
}
```

---

## AI Behavior State Machine

### NPCBrain
```csharp
public class NPCBrain : MonoBehaviour
{
    [Header("Components")]
    public NPCController controller;
    public NPCEmotionState emotionState;
    public NPCDetection detection;
    
    [Header("AI State")]
    public NPCAIState currentAIState = NPCAIState.Idle;
    
    [Header("Alert Settings")]
    public float alertDuration = 3f;
    public float searchDuration = 5f;
    
    private Vector2 lastKnownPlayerPos;
    private float stateTimer;
    
    void Start()
    {
        detection.onPlayerDetected.AddListener(OnPlayerDetected);
        detection.onPlayerLost.AddListener(OnPlayerLost);
    }
    
    void Update()
    {
        UpdateAIState();
    }
    
    void UpdateAIState()
    {
        switch (currentAIState)
        {
            case NPCAIState.Idle:
                // Controller handles idle behavior
                break;
                
            case NPCAIState.Patrol:
                // Controller handles patrol behavior
                break;
                
            case NPCAIState.Suspicious:
                HandleSuspicious();
                break;
                
            case NPCAIState.Alert:
                HandleAlert();
                break;
                
            case NPCAIState.Searching:
                HandleSearching();
                break;
                
            case NPCAIState.Returning:
                HandleReturning();
                break;
        }
    }
    
    void OnPlayerDetected(MaskMatchResult result)
    {
        switch (result)
        {
            case MaskMatchResult.Match:
                // Player has correct mask - maybe acknowledge
                PlayReaction(ReactionType.Acknowledge);
                break;
                
            case MaskMatchResult.Warning:
                // Suspicious but not hostile
                SetAIState(NPCAIState.Suspicious);
                break;
                
            case MaskMatchResult.Mismatch:
                // Wrong mask - ALERT!
                SetAIState(NPCAIState.Alert);
                lastKnownPlayerPos = detection.detectedPlayer.transform.position;
                
                // Notify game
                GameEvents.OnPlayerCaught?.Invoke();
                break;
        }
    }
    
    void OnPlayerLost()
    {
        if (currentAIState == NPCAIState.Alert)
        {
            SetAIState(NPCAIState.Searching);
        }
        else if (currentAIState == NPCAIState.Suspicious)
        {
            SetAIState(NPCAIState.Returning);
        }
    }
    
    void HandleSuspicious()
    {
        stateTimer -= Time.deltaTime;
        
        // Slow down, look around
        controller.moveSpeed *= 0.5f;
        
        if (stateTimer <= 0 || !detection.playerInRange)
        {
            SetAIState(NPCAIState.Returning);
        }
    }
    
    void HandleAlert()
    {
        // Face player, maybe move toward them
        if (detection.detectedPlayer != null)
        {
            Vector2 toPlayer = detection.detectedPlayer.transform.position - transform.position;
            controller.facingDirection = toPlayer.normalized;
        }
        
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            SetAIState(NPCAIState.Searching);
        }
    }
    
    void HandleSearching()
    {
        // Move toward last known position
        Vector2 toLastKnown = lastKnownPlayerPos - (Vector2)transform.position;
        
        if (toLastKnown.magnitude > 0.5f)
        {
            controller.moveDirection = toLastKnown.normalized;
        }
        else
        {
            // Reached last known position, look around
            controller.moveDirection = Vector2.zero;
        }
        
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            SetAIState(NPCAIState.Returning);
        }
    }
    
    void HandleReturning()
    {
        // Controller handles returning to patrol
        if (controller.currentState == NPCPhysicalState.Patrol || 
            controller.currentState == NPCPhysicalState.Idle)
        {
            SetAIState(NPCAIState.Patrol);
        }
    }
    
    void SetAIState(NPCAIState newState)
    {
        currentAIState = newState;
        
        switch (newState)
        {
            case NPCAIState.Suspicious:
                stateTimer = 3f;
                controller.SetState(NPCPhysicalState.Suspicious);
                break;
                
            case NPCAIState.Alert:
                stateTimer = alertDuration;
                controller.SetState(NPCPhysicalState.Alert);
                PlayReaction(ReactionType.Alert);
                break;
                
            case NPCAIState.Searching:
                stateTimer = searchDuration;
                break;
                
            case NPCAIState.Returning:
                controller.SetState(NPCPhysicalState.Returning);
                break;
        }
    }
    
    void PlayReaction(ReactionType type)
    {
        // Show reaction bubble/animation
        // Play sound
    }
}

public enum NPCAIState
{
    Idle,
    Patrol,
    Suspicious,
    Alert,
    Searching,
    Returning
}

public enum ReactionType
{
    Acknowledge,
    Suspicious,
    Alert,
    Confused
}
```

---

## Emotion Visual Feedback

### EmotionBubble
```csharp
public class EmotionBubble : MonoBehaviour
{
    public SpriteRenderer bubbleRenderer;
    public SpriteRenderer emotionIconRenderer;
    
    public Sprite[] emotionIcons; // Indexed by EmotionType
    public float bobAmount = 0.1f;
    public float bobSpeed = 2f;
    
    private Vector3 startPos;
    
    void Start()
    {
        startPos = transform.localPosition;
    }
    
    void Update()
    {
        // Gentle bob animation
        float offset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.localPosition = startPos + Vector3.up * offset;
    }
    
    public void SetEmotion(EmotionType emotion)
    {
        if (emotionIcons.Length > (int)emotion)
        {
            emotionIconRenderer.sprite = emotionIcons[(int)emotion];
        }
    }
    
    public void ShowReaction(ReactionType reaction)
    {
        // Pop animation
        transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
    }
}
```

---

## Event System

### GameEvents
```csharp
public static class GameEvents
{
    public static System.Action OnPlayerCaught;
    public static System.Action<MaskType> OnMaskChanged;
    public static System.Action<int> OnDetectionLevelChanged;
    
    public static void Reset()
    {
        OnPlayerCaught = null;
        OnMaskChanged = null;
        OnDetectionLevelChanged = null;
    }
}
```

---

## NPC Emotion Difficulty Scaling

| Level | Emotions Used | Change Speed | Detection |
|-------|---------------|--------------|-----------|
| 1-2   | Neutral, Happy | Static | Slow |
| 3-4   | + Sad | Slow changes | Normal |
| 5-6   | + Angry | Medium changes | Fast |
| 7+    | All, Mixed | Fast changes | Very fast |
