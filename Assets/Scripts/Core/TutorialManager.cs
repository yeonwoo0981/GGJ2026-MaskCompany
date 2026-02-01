using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

namespace MaskCompany
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }
        public static bool TutoMode => Instance != null && Instance.enabled;

        [Header("UI References")]
        [SerializeField] private GameObject titleObject;
        [SerializeField] private GameObject inputHintObject;
        [SerializeField] private GameObject hint2Object; // Room 2 mask hint (1-2-3-4)
        [SerializeField] private GameObject tutorialTextObject; // Also contains step children
        [SerializeField] private TextMeshProUGUI tutorialText;
        
        [Header("Game UI (fade in with NPCs)")]
        [SerializeField] private GameObject goalUI;
        [SerializeField] private GameObject livesUI;
        [SerializeField] private GameObject doorUI;
        
        [Header("Scene Transition")]
        [SerializeField] private Image fadeImage; // Full screen fade image
        [SerializeField] private float sceneFadeDuration = 1f;

        [Header("Room 1")]
        [SerializeField] private GameObject room1Parent; // All decor as children
        [SerializeField] private List<NPCController> room1NPCs; // Assign manually OR leave empty to auto-find from parent
        [SerializeField] private NPCController room1GoalTarget;
        [SerializeField] private GoalType room1GoalType = GoalType.Befriend;

        [Header("Room 2")]
        [SerializeField] private GameObject room2Parent; // All decor as children
        [SerializeField] private List<NPCController> room2NPCs; // Assign manually OR leave empty to auto-find from parent
        [SerializeField] private NPCController room2GoalTarget;
        [SerializeField] private GoalType room2GoalType = GoalType.Befriend;

        [Header("Timing")]
        [SerializeField] private float titleFadeInDuration = 1f;
        #pragma warning disable CS0414 // Field is assigned but never used (kept for inspector tweaking)
        [SerializeField] private float titleDisplayTime = 2f;
        #pragma warning restore CS0414
        [SerializeField] private float inputHintDelay = 1f;
        [SerializeField] private float decorFadeInterval = 0.1f;  // Delay between each object appearing
        [SerializeField] private float npcDelayAfterDecor = 1f;
        [SerializeField] private float objectFadeDuration = 0.4f; // How long each object takes to fade in

        [Header("State")]
        [SerializeField] private bool canPlayerMove;
        [SerializeField] private bool canChangeMask;
        [SerializeField] private int currentStep;

        public bool CanPlayerMove => canPlayerMove;
        public bool CanChangeMask => canChangeMask;

        private HashSet<NPCController> completedNPCs = new HashSet<NPCController>();
        private int currentStepChildIndex = -1; // Track which tutorial step child is currently shown
        private GameObject currentStepChild; // Currently active step child
        private bool isNPCBeingFired; // Track if a fired animation is playing
        private float firedAnimationDuration = 2f; // How long to wait for fired animation
        private bool hasShownSecondText; // Track if we've shown the "Be careful" text
        private Tweener textWobbleTween; // Wobble animation for text
        private HashSet<MaskType> usedMasks = new HashSet<MaskType>(); // Track masks used in room 2

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Initialize - hide everything
            canPlayerMove = false;
            currentStep = 0;

            // Hide UI elements (set alpha to 0 first, then disable)
            if (titleObject != null)
            {
                HideUIAlpha(titleObject);
                titleObject.SetActive(false);
            }
            if (inputHintObject != null)
            {
                HideUIAlpha(inputHintObject);
                inputHintObject.SetActive(false);
            }
            if (tutorialTextObject != null)
            {
                HideUIAlpha(tutorialTextObject);
                tutorialTextObject.SetActive(false);
            }
            if (hint2Object != null)
            {
                HideUIAlpha(hint2Object);
                hint2Object.SetActive(false);
            }
            
            // Hide all tutorial step children at start (children of tutorialTextObject)
            if (tutorialTextObject != null)
            {
                foreach (Transform child in tutorialTextObject.transform)
                {
                    child.gameObject.SetActive(false);
                }
            }
            
            // Hide game UI at start (they fade in later with NPCs)
            // Set alpha to 0 first to prevent flash when enabled
            if (goalUI != null)
            {
                HideUIAlpha(goalUI);
                goalUI.SetActive(false);
            }
            if (livesUI != null)
            {
                HideUIAlpha(livesUI);
                livesUI.SetActive(false);
            }
            if (doorUI != null)
            {
                HideUIAlpha(doorUI);
                doorUI.SetActive(false);
            }

            // Hide room objects initially
            if (room1Parent != null)
            {
                HideRoomChildren(room1Parent);
            }
            if (room2Parent != null) room2Parent.SetActive(false);

            StartCoroutine(RunTutorial());
        }

        /// <summary>
        /// Hide all sprites and disable colliders in parent hierarchy (including sub-groups).
        /// NPCs are handled separately.
        /// </summary>
        private void HideRoomChildren(GameObject parent)
        {
            // Hide all sprite renderers in hierarchy (decor, sub-groups, etc.)
            foreach (var sr in parent.GetComponentsInChildren<SpriteRenderer>(true))
            {
                // Skip if this is an NPC - they're handled separately
                if (sr.GetComponent<NPCController>() != null) continue;
                
                Color c = sr.color;
                c.a = 0f;
                sr.color = c;
            }
            
            // Disable all colliders initially (except NPC colliders)
            foreach (var col in parent.GetComponentsInChildren<Collider2D>(true))
            {
                if (col.GetComponent<NPCController>() != null) continue;
                col.enabled = false;
            }
            
            // Disable all NPCs
            foreach (var npc in parent.GetComponentsInChildren<NPCController>(true))
            {
                HideNPC(npc);
            }
        }
        
        /// <summary>
        /// Hide a single NPC (disable + set alpha to 0 for all child sprites too)
        /// </summary>
        private void HideNPC(NPCController npc)
        {
            if (npc == null) return;
            
            npc.enabled = false;
            
            // Hide all SpriteRenderers including children (body, etc.)
            foreach (var sr in npc.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Color c = sr.color;
                c.a = 0f;
                sr.color = c;
            }
        }

        /// <summary>
        /// Get all decor SpriteRenderers in hierarchy (excludes NPCs).
        /// Returns the GameObjects that have SpriteRenderers.
        /// </summary>
        private List<GameObject> GetDecorObjects(GameObject parent)
        {
            var decor = new List<GameObject>();
            foreach (var sr in parent.GetComponentsInChildren<SpriteRenderer>(true))
            {
                // Skip NPCs - they're handled separately
                if (sr.GetComponent<NPCController>() != null) continue;
                
                // Avoid duplicates (in case of nested SpriteRenderers)
                if (!decor.Contains(sr.gameObject))
                {
                    decor.Add(sr.gameObject);
                }
            }
            return decor;
        }

        /// <summary>
        /// Get all NPCs in hierarchy (including sub-groups).
        /// </summary>
        private List<NPCController> GetNPCs(GameObject parent)
        {
            var npcs = new List<NPCController>();
            if (parent != null)
            {
                npcs.AddRange(parent.GetComponentsInChildren<NPCController>(true));
            }
            return npcs;
        }
        
        /// <summary>
        /// Get room 1 NPCs - uses manual list if assigned, otherwise auto-finds from parent.
        /// </summary>
        private List<NPCController> GetRoom1NPCs()
        {
            if (room1NPCs != null && room1NPCs.Count > 0)
            {
                return room1NPCs;
            }
            return GetNPCs(room1Parent);
        }
        
        /// <summary>
        /// Get room 2 NPCs - uses manual list if assigned, otherwise auto-finds from parent.
        /// </summary>
        private List<NPCController> GetRoom2NPCs()
        {
            if (room2NPCs != null && room2NPCs.Count > 0)
            {
                return room2NPCs;
            }
            return GetNPCs(room2Parent);
        }

        private IEnumerator RunTutorial()
        {
            // === SETUP LevelGoalHandler ===
            SetupLevelGoalHandler();

            // === STEP 1: Title + Input Hint (stay until player moves) ===
            yield return StartCoroutine(ShowTitleAndWaitForInput());

            // === STEP 3: Enable and fade in decor ===
            yield return new WaitForSeconds(0.5f); // Brief pause before level appears
            
            if (room1Parent != null)
            {
                room1Parent.SetActive(true);
                HideRoomChildren(room1Parent); // Ensure all hidden before fade
            }
            
            // Also hide manually assigned NPCs (in case they're not children of room1Parent)
            var npcs = GetRoom1NPCs();
            foreach (var npc in npcs)
            {
                if (npc != null)
                {
                    HideNPC(npc);
                }
            }
            
            var decorObjects = GetDecorObjects(room1Parent);
            Debug.Log($"[Tutorial] Fading in {decorObjects.Count} decor objects");
            yield return StartCoroutine(FadeInObjects(decorObjects));

            // === STEP 4: Fade in NPCs after delay ===
            yield return new WaitForSeconds(npcDelayAfterDecor);
            
            Debug.Log($"[Tutorial] Fading in {npcs.Count} NPCs from room 1");
            foreach (var npc in npcs)
            {
                Debug.Log($"[Tutorial] NPC: {npc?.name ?? "null"}");
            }
            yield return StartCoroutine(FadeInNPCs(npcs));

            // Setup room 1 goal
            SetupRoom1Goal();
            
            // Verify goals were added
            var handler = LevelGoalHandler.Instance;
            if (handler != null)
            {
                Debug.Log($"[Tutorial] After SetupRoom1Goal: {handler.GetGoals().Count} goals in handler");
            }
            
            // Fade in game UI (goal, lives)
            FadeInGameUI();

            // Show first tutorial text with step child 0 (first)
            ShowText("Match your mask to befriend coworkers!", 0);

            // === STEP 5: Wait for room 1 completion ===
            currentStep = 1;
            yield return StartCoroutine(WaitForRoom1Completion());
            
            // Hide NPC interaction visuals (range, particles) after room 1 is done
            foreach (var npc in GetRoom1NPCs())
            {
                if (npc != null)
                {
                    npc.HideInteractionVisuals();
                }
            }
            
            // Hide text and all step children before transitioning
            HideText();
            HideAllStepChildren();

            // === STEP 6: Transition to Room 2 (decor only, no NPCs yet) ===
            yield return StartCoroutine(TransitionToRoom2DecorOnly());
            
            // Clear goals for room 2 and refresh UI
            var handler2 = LevelGoalHandler.Instance;
            if (handler2 != null)
            {
                handler2.ClearGoals();
                handler2.GetAllNPCs().Clear();
                handler2.ResetForNewRoom();
            }
            
            // Refresh goals UI to clear the old goals display
            RefreshGoalsUI();
            
            // Enable mask changing for room 2
            canChangeMask = true;
            usedMasks.Clear();
            usedMasks.Add(MaskType.Joy); // Player starts with Joy mask, count it as used
            
            // Show hint2 (mask switching hint) AND text with step child 2 (3rd sub-image)
            if (hint2Object != null)
            {
                HideUIAlpha(hint2Object);
                hint2Object.SetActive(true);
                FadeInUI(hint2Object, 0.5f);
            }
            ShowText("Switch masks with 1-2-3-4", 2);
            
            // === STEP 7: Wait for player to use all 4 masks ===
            currentStep = 2;
            yield return StartCoroutine(WaitForAllMasksUsed());
            
            // Hide hint2 and current text
            if (hint2Object != null)
            {
                FadeOutUI(hint2Object, 0.3f, () => hint2Object.SetActive(false));
            }
            HideText();
            
            yield return new WaitForSeconds(0.5f);
            
            // Spawn NPCs first
            var room2NPCsList = GetRoom2NPCs();
            yield return StartCoroutine(FadeInNPCs(room2NPCsList));
            
            // Setup room 2 goal and refresh UI
            SetupRoom2Goal();
            RefreshGoalsUI();
            
            // NOW show goal explanation with step child 3 (the 4th one, index 3)
            ShowText("Get him fired!", 3);

            // === STEP 8: Wait for room 2 completion ===
            yield return StartCoroutine(WaitForRoom2Completion());

            // Tutorial complete - fade out and load Game scene
            Debug.Log("[Tutorial] Complete! Fading to GameNew scene...");
            yield return StartCoroutine(FadeAndLoadScene("GameNew"));
        }

        private void SetupLevelGoalHandler()
        {
            var handler = LevelGoalHandler.Instance;
            if (handler == null)
            {
                Debug.LogWarning("[Tutorial] No LevelGoalHandler found!");
                return;
            }

            // Lives enabled in tutorial
            handler.SetUseLives(true);
            
            // Clear any existing goals
            handler.ClearGoals();
            
            // Subscribe to events for tutorial progression
            handler.OnNPCBefriended += OnGoalNPCBefriended;
            handler.OnNPCFired += OnGoalNPCFired;
            
            // Don't initialize yet - we'll add NPCs as they appear
        }
        
        private void OnGoalNPCBefriended(NPCController npc)
        {
            Debug.Log($"[Tutorial] NPC befriended: {npc.name}");
            
            // Only advance step children ONCE during room 1 (when showing second text)
            if (currentStep == 1 && !hasShownSecondText)
            {
                hasShownSecondText = true;
                ShowNextStepChild();
                UpdateText("Wrong mask = they get fired!");
            }
        }
        
        private void OnGoalNPCFired(NPCController npc)
        {
            Debug.Log($"[Tutorial] NPC fired: {npc.name}");
            isNPCBeingFired = true;
            
            // Only advance step children ONCE during room 1 (when showing second text)
            if (currentStep == 1 && !hasShownSecondText)
            {
                hasShownSecondText = true;
                ShowNextStepChild();
                UpdateText("Wrong mask = they get fired!");
            }
            
            // Reset flag after animation completes
            DOVirtual.DelayedCall(firedAnimationDuration, () => isNPCBeingFired = false);
        }
        
        /// <summary>
        /// Update just the text content without triggering step child changes
        /// </summary>
        private void UpdateText(string text)
        {
            if (tutorialText != null)
            {
                tutorialText.text = text;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            var handler = LevelGoalHandler.Instance;
            if (handler != null)
            {
                handler.OnNPCBefriended -= OnGoalNPCBefriended;
                handler.OnNPCFired -= OnGoalNPCFired;
            }
        }

        private void SetupRoom1Goal()
        {
            var handler = LevelGoalHandler.Instance;
            if (handler == null || room1GoalTarget == null) return;

            // Add room 1 NPCs to handler
            var npcs = GetRoom1NPCs();
            Debug.Log($"[Tutorial] SetupRoom1Goal adding {npcs.Count} NPCs to LevelGoalHandler");
            foreach (var npc in npcs)
            {
                if (npc != null)
                {
                    handler.AddNPC(npc);
                }
            }

            // Add goal
            handler.AddGoal(new LevelGoal
            {
                goalName = "Tutorial Goal 1",
                goalType = room1GoalType,
                targetNPC = room1GoalTarget,
                requiredTime = 1f // Shorter for tutorial
            });

            // Initialize if not already
            handler.Initialize();

            Debug.Log($"[Tutorial] Room 1 goal set: {room1GoalType} {room1GoalTarget.name}");
        }

        private void SetupRoom2Goal()
        {
            var handler = LevelGoalHandler.Instance;
            if (handler == null || room2GoalTarget == null) return;

            // Add room 2 NPCs
            var npcs = GetRoom2NPCs();
            Debug.Log($"[Tutorial] SetupRoom2Goal adding {npcs.Count} NPCs to LevelGoalHandler");
            foreach (var npc in npcs)
            {
                if (npc != null)
                {
                    handler.AddNPC(npc);
                }
            }

            // Add goal
            handler.AddGoal(new LevelGoal
            {
                goalName = "Tutorial Goal 2",
                goalType = room2GoalType,
                targetNPC = room2GoalTarget,
                requiredTime = 1f
            });

            // Initialize handler for room 2
            handler.Initialize();

            Debug.Log($"[Tutorial] Room 2 goal set: {room2GoalType} {room2GoalTarget.name}");
        }
        
        /// <summary>
        /// Refresh the goals UI display
        /// </summary>
        private void RefreshGoalsUI()
        {
            LevelGoalUI levelGoalUI = null;
            if (goalUI != null)
            {
                levelGoalUI = goalUI.GetComponent<LevelGoalUI>();
                if (levelGoalUI == null)
                {
                    levelGoalUI = goalUI.GetComponentInChildren<LevelGoalUI>();
                }
            }
            if (levelGoalUI == null)
            {
                levelGoalUI = FindFirstObjectByType<LevelGoalUI>();
            }
            
            if (levelGoalUI != null)
            {
                levelGoalUI.RefreshGoalsDisplay();
            }
        }

        #region Title & Hints

        private IEnumerator ShowTitleAndWaitForInput()
        {
            // Show title
            if (titleObject != null)
            {
                HideUIAlpha(titleObject);
                titleObject.SetActive(true);
                FadeInUI(titleObject, titleFadeInDuration);
            }
            
            yield return new WaitForSeconds(titleFadeInDuration + inputHintDelay);
            
            // Show input hint
            if (inputHintObject != null)
            {
                HideUIAlpha(inputHintObject);
                inputHintObject.SetActive(true);
                FadeInUI(inputHintObject, 0.5f);
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // Enable movement and wait for player to move
            canPlayerMove = true;
            
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                Vector3 startPos = player.transform.position;
                while (Vector3.Distance(player.transform.position, startPos) < 0.1f)
                {
                    yield return null;
                }
            }
            
            // Player moved! Fade out title and hint
            if (titleObject != null)
            {
                FadeOutUI(titleObject, 0.5f, () => titleObject.SetActive(false));
            }
            if (inputHintObject != null)
            {
                FadeOutUI(inputHintObject, 0.5f, () => inputHintObject.SetActive(false));
            }
            
            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>
        /// Set all Image, TextMeshProUGUI, and SpriteRenderer alphas to 0 (for clean fade-in)
        /// </summary>
        private void HideUIAlpha(GameObject obj)
        {
            foreach (var img in obj.GetComponentsInChildren<Image>(true))
            {
                Color c = img.color;
                c.a = 0f;
                img.color = c;
            }
            foreach (var txt in obj.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                Color c = txt.color;
                c.a = 0f;
                txt.color = c;
            }
            foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Color c = sr.color;
                c.a = 0f;
                sr.color = c;
            }
        }

        private void FadeInGameUI()
        {
            // Start coroutine to wait a frame for UI to be instantiated
            StartCoroutine(FadeInGameUIDelayed());
        }

        private IEnumerator FadeInGameUIDelayed()
        {
            // Wait a frame for any dynamic UI instantiation
            yield return null;
            
            // Enable UI first (this triggers LevelGoalUI.Start which creates goal items)
            if (goalUI != null) goalUI.SetActive(true);
            if (livesUI != null) livesUI.SetActive(true);
            if (doorUI != null) doorUI.SetActive(true);
            
            // Wait another frame for Start() to run and create child elements
            yield return null;
            
            // Force refresh goals display in case it didn't catch the goals
            // Try to find LevelGoalUI on goalUI or its children, or anywhere in scene
            LevelGoalUI levelGoalUI = null;
            if (goalUI != null)
            {
                levelGoalUI = goalUI.GetComponent<LevelGoalUI>();
                if (levelGoalUI == null)
                {
                    levelGoalUI = goalUI.GetComponentInChildren<LevelGoalUI>();
                }
            }
            if (levelGoalUI == null)
            {
                levelGoalUI = FindFirstObjectByType<LevelGoalUI>();
            }
            
            if (levelGoalUI != null)
            {
                Debug.Log($"[Tutorial] Calling RefreshGoalsDisplay on LevelGoalUI (found on {levelGoalUI.gameObject.name})");
                levelGoalUI.RefreshGoalsDisplay();
            }
            else
            {
                Debug.LogWarning($"[Tutorial] LevelGoalUI not found anywhere!");
            }
            
            // Wait another frame for goal items to be created
            yield return null;
            
            // Now hide alpha on everything (including newly created items)
            if (goalUI != null) HideUIAlpha(goalUI);
            if (livesUI != null) HideUIAlpha(livesUI);
            if (doorUI != null) HideUIAlpha(doorUI);
            
            // Now fade in
            if (goalUI != null) FadeInUI(goalUI, 0.5f);
            if (livesUI != null) FadeInUI(livesUI, 0.5f);
            if (doorUI != null) FadeInUI(doorUI, 0.5f);
        }

        /// <summary>
        /// Show tutorial text. Optionally show a specific step child by index, or advance to next if index is -1.
        /// </summary>
        /// <param name="text">Text to display</param>
        /// <param name="stepChildIndex">-1 = no step child, -2 = advance to next, 0+ = specific index</param>
        private void ShowText(string text, int stepChildIndex = -1)
        {
            if (tutorialText != null)
            {
                tutorialText.text = text;
            }
            if (tutorialTextObject != null)
            {
                HideUIAlpha(tutorialTextObject); // Ensure alpha is 0 before showing
                tutorialTextObject.SetActive(true);
                FadeInUI(tutorialTextObject, 0.3f);
                
                // Start subtle horizontal wobble animation
                StartTextWobble();
            }
            
            // Handle step child display
            if (stepChildIndex == -2)
            {
                ShowNextStepChild();
            }
            else if (stepChildIndex >= 0)
            {
                ShowStepChildAt(stepChildIndex);
            }
        }
        
        private void StartTextWobble()
        {
            if (tutorialTextObject == null) return;
            
            // Kill any existing wobble
            textWobbleTween?.Kill();
            
            // Reset position
            var rt = tutorialTextObject.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector2 originalPos = rt.anchoredPosition;
                
                // Subtle horizontal wobble (5 units left/right)
                textWobbleTween = rt.DOAnchorPosX(originalPos.x + 5f, 0.8f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }
        
        private void StopTextWobble()
        {
            textWobbleTween?.Kill();
            textWobbleTween = null;
        }

        private void HideText()
        {
            // Stop wobble animation
            StopTextWobble();
            
            if (tutorialTextObject != null)
            {
                FadeOutUI(tutorialTextObject, 0.3f, () => tutorialTextObject.SetActive(false));
            }
            
            // Also hide current step child
            HideCurrentStepChild();
        }
        
        /// <summary>
        /// Hide all step children and reset index (call when room ends)
        /// </summary>
        private void HideAllStepChildren()
        {
            if (tutorialTextObject == null) return;
            
            foreach (Transform child in tutorialTextObject.transform)
            {
                var childObj = child.gameObject;
                FadeOutUI(childObj, 0.3f, () => childObj.SetActive(false));
            }
            
            currentStepChildIndex = -1;
            currentStepChild = null;
        }
        
        /// <summary>
        /// Show the next step child (fade in), hide the current one (fade out)
        /// </summary>
        private void ShowNextStepChild()
        {
            if (tutorialTextObject == null) return;
            
            // Hide current child first
            if (currentStepChild != null)
            {
                var toHide = currentStepChild;
                FadeOutUI(toHide, 0.3f, () => toHide.SetActive(false));
            }
            
            // Move to next child
            currentStepChildIndex++;
            
            if (currentStepChildIndex < tutorialTextObject.transform.childCount)
            {
                currentStepChild = tutorialTextObject.transform.GetChild(currentStepChildIndex).gameObject;
                HideUIAlpha(currentStepChild);
                currentStepChild.SetActive(true);
                FadeInUI(currentStepChild, 0.3f);
            }
            else
            {
                currentStepChild = null;
            }
        }
        
        /// <summary>
        /// Show a specific step child by index (0-based)
        /// </summary>
        private void ShowStepChildAt(int index)
        {
            if (tutorialTextObject == null) return;
            if (index < 0 || index >= tutorialTextObject.transform.childCount) return;
            
            // Hide current child first
            if (currentStepChild != null)
            {
                var toHide = currentStepChild;
                FadeOutUI(toHide, 0.3f, () => toHide.SetActive(false));
            }
            
            currentStepChildIndex = index;
            currentStepChild = tutorialTextObject.transform.GetChild(index).gameObject;
            HideUIAlpha(currentStepChild);
            currentStepChild.SetActive(true);
            FadeInUI(currentStepChild, 0.3f);
        }
        
        /// <summary>
        /// Hide the current step child
        /// </summary>
        private void HideCurrentStepChild()
        {
            if (currentStepChild != null)
            {
                var toHide = currentStepChild;
                FadeOutUI(toHide, 0.3f, () => toHide.SetActive(false));
                currentStepChild = null;
            }
        }

        /// <summary>
        /// Fade in all Image, TextMeshProUGUI, and SpriteRenderer components in the object
        /// </summary>
        private void FadeInUI(GameObject obj, float duration)
        {
            // Fade images
            foreach (var img in obj.GetComponentsInChildren<Image>(true))
            {
                Color c = img.color;
                c.a = 0f;
                img.color = c;
                img.DOFade(1f, duration);
            }
            // Fade text
            foreach (var txt in obj.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                Color c = txt.color;
                c.a = 0f;
                txt.color = c;
                txt.DOFade(1f, duration);
            }
            // Fade sprite renderers
            foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Color c = sr.color;
                c.a = 0f;
                sr.color = c;
                sr.DOFade(1f, duration);
            }
        }

        /// <summary>
        /// Fade out all Image, TextMeshProUGUI, and SpriteRenderer components in the object
        /// </summary>
        private void FadeOutUI(GameObject obj, float duration, System.Action onComplete = null)
        {
            int count = 0;
            int total = obj.GetComponentsInChildren<Image>(true).Length + 
                        obj.GetComponentsInChildren<TextMeshProUGUI>(true).Length +
                        obj.GetComponentsInChildren<SpriteRenderer>(true).Length;
            
            if (total == 0)
            {
                onComplete?.Invoke();
                return;
            }

            // Fade images
            foreach (var img in obj.GetComponentsInChildren<Image>(true))
            {
                img.DOFade(0f, duration).OnComplete(() =>
                {
                    count++;
                    if (count >= total) onComplete?.Invoke();
                });
            }
            // Fade text
            foreach (var txt in obj.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                txt.DOFade(0f, duration).OnComplete(() =>
                {
                    count++;
                    if (count >= total) onComplete?.Invoke();
                });
            }
            // Fade sprite renderers
            foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.DOFade(0f, duration).OnComplete(() =>
                {
                    count++;
                    if (count >= total) onComplete?.Invoke();
                });
            }
        }

        #endregion

        #region Wait Conditions

        private IEnumerator WaitForRoom1Completion()
        {
            // Wait until all room 1 NPCs are fully happy or fully sad
            while (!AreAllNPCsComplete(GetRoom1NPCs()))
            {
                yield return null;
            }

            // CRITICAL: Force LevelGoalHandler to check NPC states NOW
            // This ensures NPCs that just reached extreme comfort get properly fired/befriended
            var handler = LevelGoalHandler.Instance;
            if (handler != null)
            {
                handler.ForceCheckNPCStates();
            }
            
            // Wait a frame for the fired event to propagate
            yield return null;
            
            // Then wait for any ongoing fired animation to complete
            while (isNPCBeingFired)
            {
                yield return null;
            }
        }

        private IEnumerator WaitForRoom2Completion()
        {
            // Just wait for the goal to be completed (target NPC fired/befriended)
            var handler = LevelGoalHandler.Instance;
            while (handler != null)
            {
                var goals = handler.GetGoals();
                bool anyGoalComplete = false;
                foreach (var goal in goals)
                {
                    if (goal.completed)
                    {
                        anyGoalComplete = true;
                        break;
                    }
                }
                
                if (anyGoalComplete) break;
                yield return null;
            }
            
            // Wait for any ongoing fired animation to complete
            while (isNPCBeingFired)
            {
                yield return null;
            }
        }

        private bool AreAllNPCsComplete(List<NPCController> npcs)
        {
            int enabledCount = 0;
            int completeCount = 0;
            
            foreach (var npc in npcs)
            {
                if (npc == null || !npc.enabled) continue;
                
                enabledCount++;
                
                // Check if NPC reached extreme
                // Happy >= 1 (befriend threshold) or Sad <= -0.95 (fired threshold)
                float comfort = npc.ComfortLevel;
                if (comfort >= 1f || comfort <= -0.95f)
                {
                    completeCount++;
                }
            }
            
            // Need at least one enabled NPC and all must be complete
            return enabledCount > 0 && completeCount >= enabledCount;
        }

        #endregion

        #region Object Animations

        private IEnumerator FadeInObjects(List<GameObject> objects)
        {
            foreach (var obj in objects)
            {
                if (obj == null) continue;

                var sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color targetColor = sr.color;
                    targetColor.a = 1f;
                    sr.DOColor(targetColor, objectFadeDuration);
                }

                // Enable collider after fade starts
                var col = obj.GetComponent<Collider2D>();
                if (col != null)
                {
                    DOVirtual.DelayedCall(objectFadeDuration * 0.5f, () => {
                        if (col != null) col.enabled = true;
                    });
                }

                // Small pop effect
                obj.transform.DOPunchScale(Vector3.one * 0.1f, objectFadeDuration, 5);

                yield return new WaitForSeconds(decorFadeInterval);
            }

            // Wait for last object to finish
            yield return new WaitForSeconds(objectFadeDuration);
        }

        private IEnumerator FadeInNPCs(List<NPCController> npcs)
        {
            foreach (var npc in npcs)
            {
                if (npc == null) continue;

                // Enable the GameObject first
                npc.gameObject.SetActive(true);

                // Fade in all SpriteRenderers including children (body, etc.)
                foreach (var sr in npc.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    Color targetColor = sr.color;
                    targetColor.a = 1f;
                    sr.DOColor(targetColor, objectFadeDuration);
                }

                // Enable NPC behavior (the MonoBehaviour component)
                npc.enabled = true;

                // Pop effect
                npc.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 5);

                yield return new WaitForSeconds(decorFadeInterval);
            }

            yield return new WaitForSeconds(objectFadeDuration);
        }

        private IEnumerator FadeOutObjects(List<GameObject> objects)
        {
            foreach (var obj in objects)
            {
                if (obj == null) continue;

                var sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color targetColor = sr.color;
                    targetColor.a = 0f;
                    sr.DOColor(targetColor, objectFadeDuration);
                }

                var col = obj.GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
            }

            yield return new WaitForSeconds(objectFadeDuration);
        }

        private IEnumerator FadeOutNPCs(List<NPCController> npcs)
        {
            foreach (var npc in npcs)
            {
                if (npc == null) continue;

                npc.enabled = false;

                // Fade out all SpriteRenderers including children (body, etc.)
                foreach (var sr in npc.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    Color targetColor = sr.color;
                    targetColor.a = 0f;
                    sr.DOColor(targetColor, objectFadeDuration);
                }
            }

            yield return new WaitForSeconds(objectFadeDuration);
        }

        #endregion

        #region Room Transitions

        private IEnumerator TransitionToRoom2()
        {
            HideText();

            // Fade out room 1
            yield return StartCoroutine(FadeOutNPCs(GetRoom1NPCs()));
            yield return StartCoroutine(FadeOutObjects(GetDecorObjects(room1Parent)));

            if (room1Parent != null) room1Parent.SetActive(false);

            // Activate room 2
            if (room2Parent != null)
            {
                room2Parent.SetActive(true);
                HideRoomChildren(room2Parent);
            }
            
            // Also hide manually assigned room 2 NPCs
            var room2NPCsList = GetRoom2NPCs();
            foreach (var npc in room2NPCsList)
            {
                if (npc != null)
                {
                    HideNPC(npc);
                }
            }

            yield return new WaitForSeconds(0.5f);

            // Fade in room 2
            yield return StartCoroutine(FadeInObjects(GetDecorObjects(room2Parent)));
            yield return new WaitForSeconds(npcDelayAfterDecor);
            yield return StartCoroutine(FadeInNPCs(room2NPCsList));
        }
        
        /// <summary>
        /// Transition to room 2 but only show decor, not NPCs (they spawn after mask tutorial)
        /// </summary>
        private IEnumerator TransitionToRoom2DecorOnly()
        {
            HideText();

            // Fade out room 1
            yield return StartCoroutine(FadeOutNPCs(GetRoom1NPCs()));
            yield return StartCoroutine(FadeOutObjects(GetDecorObjects(room1Parent)));

            if (room1Parent != null) room1Parent.SetActive(false);

            // Activate room 2
            if (room2Parent != null)
            {
                room2Parent.SetActive(true);
                HideRoomChildren(room2Parent);
            }
            
            // Hide manually assigned room 2 NPCs (they spawn later)
            var room2NPCsList = GetRoom2NPCs();
            foreach (var npc in room2NPCsList)
            {
                if (npc != null)
                {
                    HideNPC(npc);
                }
            }

            yield return new WaitForSeconds(0.5f);

            // Fade in room 2 decor ONLY (no NPCs yet)
            yield return StartCoroutine(FadeInObjects(GetDecorObjects(room2Parent)));
        }
        
        /// <summary>
        /// Wait until player has used all 4 mask types
        /// </summary>
        private IEnumerator WaitForAllMasksUsed()
        {
            // Need to use Joy, Neutral, Anger, Fear
            while (usedMasks.Count < 4)
            {
                yield return null;
            }
            Debug.Log("[Tutorial] All masks used!");
        }
        
        /// <summary>
        /// Called by PlayerController when mask is changed
        /// </summary>
        public void OnMaskUsed(MaskType mask)
        {
            if (currentStep == 2 && canChangeMask)
            {
                usedMasks.Add(mask);
                Debug.Log($"[Tutorial] Mask used: {mask}. Total: {usedMasks.Count}/4");
            }
        }
        
        /// <summary>
        /// Fade screen to black and load a scene
        /// </summary>
        private IEnumerator FadeAndLoadScene(string sceneName)
        {
            if (fadeImage != null)
            {
                // Enable and set alpha to 0
                fadeImage.gameObject.SetActive(true);
                Color c = fadeImage.color;
                c.a = 0f;
                fadeImage.color = c;
                
                // Fade to black
                fadeImage.DOFade(1f, sceneFadeDuration);
                yield return new WaitForSeconds(sceneFadeDuration);
            }
            
            SceneManager.LoadScene(sceneName);
        }

        #endregion

        /// <summary>
        /// Called by NPCController when an NPC reaches an extreme comfort state
        /// </summary>
        public void OnNPCReachedExtreme(NPCController npc)
        {
            if (!completedNPCs.Contains(npc))
            {
                completedNPCs.Add(npc);
                Debug.Log($"[Tutorial] NPC {npc.name} completed!");
            }
        }
    }
}
