using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace MaskCompany
{
    public class LevelGoalUI : MonoBehaviour
    {
        [Header("Lives Display")]
        [SerializeField] private Transform livesContainer;
        [SerializeField] private GameObject lifeIconPrefab;
        [SerializeField] private Sprite lifeFullSprite;
        [SerializeField] private Sprite lifeEmptySprite;

        [Header("Goals Display")]
        [SerializeField] private Transform goalsContainer;
        [SerializeField] private GameObject goalItemPrefab;

        [Header("Messages")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private float messageDuration = 2f;

        [Header("Level End")]
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private GameObject levelFailedPanel;

        private List<Image> lifeIcons = new List<Image>();
        private List<GoalUIItem> goalItems = new List<GoalUIItem>();
        private LevelGoalHandler goalHandler;

        private void Start()
        {
            goalHandler = LevelGoalHandler.Instance;
            if (goalHandler == null)
            {
                Debug.LogWarning("LevelGoalUI: No LevelGoalHandler found!");
                return;
            }

            // Subscribe to events
            goalHandler.OnLivesChanged += UpdateLivesDisplay;
            goalHandler.OnNPCFired += OnNPCFired;
            goalHandler.OnNPCBefriended += OnNPCBefriended;
            goalHandler.OnLevelComplete += ShowLevelComplete;
            goalHandler.OnLevelFailed += ShowLevelFailed;

            // Initialize UI
            CreateLivesDisplay();
            CreateGoalsDisplay();

            // Hide end panels
            if (levelCompletePanel) levelCompletePanel.SetActive(false);
            if (levelFailedPanel) levelFailedPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (goalHandler != null)
            {
                goalHandler.OnLivesChanged -= UpdateLivesDisplay;
                goalHandler.OnNPCFired -= OnNPCFired;
                goalHandler.OnNPCBefriended -= OnNPCBefriended;
                goalHandler.OnLevelComplete -= ShowLevelComplete;
                goalHandler.OnLevelFailed -= ShowLevelFailed;
            }
        }

        private void CreateLivesDisplay()
        {
            if (livesContainer == null || lifeIconPrefab == null) return;

            // Clear existing
            foreach (Transform child in livesContainer)
            {
                Destroy(child.gameObject);
            }
            lifeIcons.Clear();

            // Create life icons
            int maxLives = goalHandler.GetMaxLives();
            for (int i = 0; i < maxLives; i++)
            {
                GameObject icon = Instantiate(lifeIconPrefab, livesContainer);
                Image img = icon.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = lifeFullSprite;
                    lifeIcons.Add(img);
                }
            }
        }

        private void UpdateLivesDisplay(int currentLives)
        {
            for (int i = 0; i < lifeIcons.Count; i++)
            {
                bool hasLife = i < currentLives;
                lifeIcons[i].sprite = hasLife ? lifeFullSprite : lifeEmptySprite;

                // Animate lost life
                if (!hasLife && i == currentLives)
                {
                    lifeIcons[i].transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
                    lifeIcons[i].DOColor(Color.gray, 0.3f);
                }
            }
        }

        private void CreateGoalsDisplay()
        {
            if (goalsContainer == null || goalItemPrefab == null)
            {
                Debug.LogWarning($"[LevelGoalUI] CreateGoalsDisplay: goalsContainer={goalsContainer}, goalItemPrefab={goalItemPrefab}");
                return;
            }

            // Clear existing
            foreach (Transform child in goalsContainer)
            {
                Destroy(child.gameObject);
            }
            goalItems.Clear();

            // Create goal items
            var goals = goalHandler.GetGoals();
            Debug.Log($"[LevelGoalUI] CreateGoalsDisplay: {goals.Count} goals to display");
            
            foreach (var goal in goals)
            {
                GameObject item = Instantiate(goalItemPrefab, goalsContainer);
                GoalUIItem uiItem = item.GetComponent<GoalUIItem>();
                if (uiItem != null)
                {
                    uiItem.Setup(goal);
                    goalItems.Add(uiItem);
                    Debug.Log($"[LevelGoalUI] Created goal item for: {goal.goalName}");
                }
            }
        }
        
        /// <summary>
        /// Call this to refresh the goals display after goals have been changed
        /// </summary>
        public void RefreshGoalsDisplay()
        {
            if (goalHandler == null)
            {
                goalHandler = LevelGoalHandler.Instance;
                Debug.Log($"[LevelGoalUI] RefreshGoalsDisplay: got handler from Instance = {goalHandler}");
            }
            
            if (goalHandler == null)
            {
                Debug.LogWarning("[LevelGoalUI] RefreshGoalsDisplay: No LevelGoalHandler found!");
                return;
            }
            
            var goals = goalHandler.GetGoals();
            Debug.Log($"[LevelGoalUI] RefreshGoalsDisplay: Handler has {goals.Count} goals");
            
            CreateGoalsDisplay();
        }

        private void Update()
        {
            // Update goal progress
            if (goalHandler == null) return;

            var goals = goalHandler.GetGoals();
            for (int i = 0; i < goalItems.Count && i < goals.Count; i++)
            {
                goalItems[i].UpdateProgress(goals[i]);
            }
        }

        private void OnNPCFired(NPCController npc)
        {
            ShowMessage($"{npc.name} was FIRED!", Color.red);
        }

        private void OnNPCBefriended(NPCController npc)
        {
            ShowMessage($"{npc.name} is now your friend!", Color.green);
        }

        private void ShowMessage(string text, Color color)
        {
            if (messageText == null) return;

            messageText.text = text;
            messageText.color = color;
            messageText.gameObject.SetActive(true);

            // Animate
            messageText.transform.localScale = Vector3.zero;
            messageText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

            // Hide after duration
            DOVirtual.DelayedCall(messageDuration, () =>
            {
                messageText.DOFade(0f, 0.3f).OnComplete(() =>
                {
                    messageText.gameObject.SetActive(false);
                    messageText.alpha = 1f;
                });
            });
        }

        private void ShowLevelComplete()
        {
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(true);
                levelCompletePanel.transform.localScale = Vector3.zero;
                levelCompletePanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            }
        }

        private void ShowLevelFailed()
        {
            if (levelFailedPanel != null)
            {
                levelFailedPanel.SetActive(true);
                levelFailedPanel.transform.localScale = Vector3.zero;
                levelFailedPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            }
        }
    }
}
