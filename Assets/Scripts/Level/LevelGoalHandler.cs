using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace MaskCompany
{
    public class LevelGoalHandler : MonoBehaviour
    {
        public static LevelGoalHandler Instance { get; private set; }

        [Header("Lives")]
        [SerializeField] private int maxLives = 3;
        [SerializeField] private int currentLives;

        [Header("NPCs")]
        [SerializeField] private List<NPCController> allNPCs = new List<NPCController>();

        [Header("Goals")]
        [SerializeField] private List<LevelGoal> goals = new List<LevelGoal>();

        [Header("Fired Animation")]
        [SerializeField] private Transform exitDoor; // Optional: NPCs move here when fired
        [SerializeField] private float firedAnimDuration = 1f;

        [Header("Thresholds")]
        [SerializeField] private float firedThreshold = -0.95f; // Comfort below this = fired
        [SerializeField] private float befriendThreshold = 0.9f; // Comfort above this = befriended

        [Header("Events")]
        public Action<int> OnLivesChanged;
        public Action<NPCController> OnNPCFired;
        public Action<NPCController> OnNPCBefriended;
        public Action OnLevelComplete;
        public Action OnLevelFailed;

        [Header("Debug")]
        [SerializeField] private bool levelComplete;
        [SerializeField] private bool levelFailed;
        [SerializeField] private int goalsCompleted;

        private void Awake()
        {
            Instance = this;
            currentLives = maxLives;
        }

        private void Start()
        {
            // Auto-find NPCs if list is empty
            if (allNPCs.Count == 0)
            {
                allNPCs.AddRange(FindObjectsByType<NPCController>(FindObjectsSortMode.None));
            }

            // Subscribe to NPC events
            foreach (var npc in allNPCs)
            {
                // We'll check comfort in Update
            }
        }

        private void Update()
        {
            if (levelComplete || levelFailed) return;

            CheckNPCStates();
            CheckGoalCompletion();
        }

        private void CheckNPCStates()
        {
            foreach (var npc in allNPCs.ToArray()) // ToArray to allow modification during iteration
            {
                if (npc == null) continue;

                float comfort = npc.ComfortLevel;

                // Check if NPC should be fired (too angry)
                if (comfort <= firedThreshold)
                {
                    FireNPC(npc);
                }
            }
        }

        private void FireNPC(NPCController npc)
        {
            Debug.Log($"[LevelGoalHandler] {npc.name} got FIRED!");

            // Check if this NPC was a "don't fire" target
            bool wasProtected = false;
            foreach (var goal in goals)
            {
                if (goal.targetNPC == npc && goal.goalType == GoalType.Befriend)
                {
                    wasProtected = true;
                    goal.failed = true;
                }
                if (goal.targetNPC == npc && goal.goalType == GoalType.Fire)
                {
                    goal.completed = true;
                }
            }

            // Lose a life if we fired someone we shouldn't have
            if (wasProtected)
            {
                LoseLife();
            }

            // Play fired animation
            PlayFiredAnimation(npc);

            OnNPCFired?.Invoke(npc);
        }

        private void PlayFiredAnimation(NPCController npc)
        {
            // Disable NPC interaction and kill existing tweens
            npc.enabled = false;
            DOTween.Kill(npc.transform);
            DOTween.Kill(npc.gameObject);

            Transform npcTransform = npc.transform;
            SpriteRenderer sprite = npc.GetComponent<SpriteRenderer>();
            GameObject npcObject = npc.gameObject;

            // Remove from list immediately to prevent further interaction
            allNPCs.Remove(npc);

            // Determine exit position: toward door if set, otherwise off-screen right
            Vector3 exitPos;
            if (exitDoor != null)
            {
                exitPos = exitDoor.position;
            }
            else
            {
                // Exit to the right side of screen
                exitPos = npcTransform.position + Vector3.right * 8f;
            }

            Sequence seq = DOTween.Sequence();
            seq.SetTarget(npcObject); // Link to gameObject for auto-kill

            // 1. Shake in anger
            seq.Append(npcTransform.DOShakePosition(0.3f, 0.3f, 15));
            
            // 2. Flash red and grow slightly (angry)
            if (sprite != null)
            {
                seq.Join(sprite.DOColor(Color.red, 0.3f));
            }
            seq.Join(npcTransform.DOScale(1.2f, 0.2f));

            // 3. Stomp (small jump)
            seq.Append(npcTransform.DOScale(1f, 0.1f));
            
            // 4. Storm off toward exit
            seq.Append(npcTransform.DOMove(exitPos, firedAnimDuration).SetEase(Ease.InQuad));

            // 5. Fade out as they leave
            if (sprite != null)
            {
                seq.Join(sprite.DOFade(0f, firedAnimDuration * 0.7f).SetDelay(firedAnimDuration * 0.3f));
            }

            // 6. Shrink as they go
            seq.Join(npcTransform.DOScale(0.3f, firedAnimDuration).SetEase(Ease.InQuad));

            // Destroy after complete
            seq.OnKill(() =>
            {
                if (npcObject != null)
                {
                    Destroy(npcObject);
                }
            });
            
            seq.OnComplete(() =>
            {
                if (npcObject != null)
                {
                    Destroy(npcObject);
                }
            });
        }

        public void BefriendNPC(NPCController npc)
        {
            Debug.Log($"[LevelGoalHandler] {npc.name} is now your FRIEND!");

            foreach (var goal in goals)
            {
                if (goal.targetNPC == npc && goal.goalType == GoalType.Befriend)
                {
                    goal.completed = true;
                }
            }

            OnNPCBefriended?.Invoke(npc);
        }

        private void CheckGoalCompletion()
        {
            // Check befriend goals (need sustained high comfort)
            foreach (var goal in goals)
            {
                if (goal.completed || goal.failed) continue;

                if (goal.targetNPC == null) continue;

                float comfort = goal.targetNPC.ComfortLevel;

                if (goal.goalType == GoalType.Befriend && comfort >= befriendThreshold)
                {
                    goal.progressTimer += Time.deltaTime;
                    if (goal.progressTimer >= goal.requiredTime)
                    {
                        goal.completed = true;
                        BefriendNPC(goal.targetNPC);
                    }
                }
                else
                {
                    // Reset timer if comfort drops
                    goal.progressTimer = Mathf.Max(0, goal.progressTimer - Time.deltaTime * 0.5f);
                }
            }

            // Count completed goals
            goalsCompleted = 0;
            int requiredGoals = 0;
            foreach (var goal in goals)
            {
                if (goal.completed) goalsCompleted++;
                if (!goal.optional) requiredGoals++;
            }

            // Check win condition
            int completedRequired = 0;
            foreach (var goal in goals)
            {
                if (!goal.optional && goal.completed) completedRequired++;
            }

            if (completedRequired >= requiredGoals && requiredGoals > 0)
            {
                LevelComplete();
            }
        }

        private void LoseLife()
        {
            currentLives--;
            Debug.Log($"[LevelGoalHandler] Lost a life! Remaining: {currentLives}");
            
            OnLivesChanged?.Invoke(currentLives);

            if (currentLives <= 0)
            {
                LevelFailed();
            }
        }

        private void LevelComplete()
        {
            if (levelComplete) return;
            
            levelComplete = true;
            Debug.Log("[LevelGoalHandler] LEVEL COMPLETE!");
            OnLevelComplete?.Invoke();
        }

        private void LevelFailed()
        {
            if (levelFailed) return;
            
            levelFailed = true;
            Debug.Log("[LevelGoalHandler] LEVEL FAILED!");
            OnLevelFailed?.Invoke();
        }

        // Public API
        public int GetCurrentLives() => currentLives;
        public int GetMaxLives() => maxLives;
        public List<LevelGoal> GetGoals() => goals;
        public List<NPCController> GetAllNPCs() => allNPCs;
        
        public void AddNPC(NPCController npc)
        {
            if (!allNPCs.Contains(npc))
                allNPCs.Add(npc);
        }

        public void RemoveNPC(NPCController npc)
        {
            allNPCs.Remove(npc);
        }
    }

    [Serializable]
    public class LevelGoal
    {
        public string goalName;
        public GoalType goalType;
        public NPCController targetNPC;
        public float requiredTime = 2f; // How long to maintain state
        public bool optional = false;

        [Header("Runtime")]
        public bool completed;
        public bool failed;
        public float progressTimer;

        /// <summary>
        /// Gets sprite from target NPC's SpriteRenderer
        /// </summary>
        public Sprite GetTargetSprite()
        {
            if (targetNPC == null) return null;
            var sr = targetNPC.GetComponent<SpriteRenderer>();
            return sr != null ? sr.sprite : null;
        }

        /// <summary>
        /// Gets name from NPC config or gameObject name
        /// </summary>
        public string GetTargetName()
        {
            if (!string.IsNullOrEmpty(goalName)) return goalName;
            if (targetNPC == null) return "Unknown";
            return targetNPC.gameObject.name;
        }
    }

    public enum GoalType
    {
        Befriend,   // Get NPC to max happiness
        Fire,       // Get NPC to max anger (they leave)
        Survive     // Don't let any NPC get fired (lives system)
    }
}
