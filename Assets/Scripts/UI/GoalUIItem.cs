using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace MaskCompany
{
    public class GoalUIItem : MonoBehaviour
    {
        [Header("Images")]
        [SerializeField] private Image npcFaceImage;
        [SerializeField] private Image progressRing;        // Filled Radial 360
        [SerializeField] private Image goalIcon;            // Shows goal type, then done icon

        [Header("Sprites")]
        [SerializeField] private Sprite befriendSprite;     // heart.png
        [SerializeField] private Sprite fireSprite;         // firedDoor.png
        [SerializeField] private Sprite completedSprite;    // Checked.png
        [SerializeField] private Sprite failedSprite;       // failed.png

        [Header("Colors")]
        [SerializeField] private Color positiveColor = new Color(0.4f, 0.9f, 0.5f);   // Green
        [SerializeField] private Color negativeColor = new Color(0.6f, 0.3f, 0.7f);   // Purple
        [SerializeField] private Color completedColor = Color.green;
        [SerializeField] private Color failedColor = Color.red;

        [Header("Optional")]
        [SerializeField] private TextMeshProUGUI goalText;

        private LevelGoal goal;
        private bool isDone;

        public void Setup(LevelGoal goal)
        {
            this.goal = goal;
            isDone = false;

            // NPC Face
            if (npcFaceImage != null)
            {
                Sprite sprite = goal.GetTargetSprite();
                if (sprite != null)
                {
                    npcFaceImage.sprite = sprite;
                }
            }

            // Goal icon (heart or door)
            if (goalIcon != null)
            {
                goalIcon.sprite = goal.goalType == GoalType.Befriend ? befriendSprite : fireSprite;
                goalIcon.color = Color.white;
            }

            // Progress ring - starts empty
            if (progressRing != null)
            {
                progressRing.fillAmount = 0f;
                progressRing.fillClockwise = true;
                progressRing.color = positiveColor;
            }

            // Optional text
            if (goalText != null)
            {
                goalText.text = goal.GetTargetName();
            }
        }

        public void UpdateProgress(LevelGoal goal)
        {
            // Progress ring based on NPC comfort level
            if (progressRing != null && goal.targetNPC != null)
            {
                float comfort = goal.targetNPC.ComfortLevel;
                
                // Fill amount is absolute value (0 to 1)
                progressRing.fillAmount = Mathf.Abs(comfort);
                
                // Positive = clockwise green, Negative = counter-clockwise purple
                if (comfort >= 0)
                {
                    progressRing.fillClockwise = true;
                    progressRing.color = positiveColor;
                }
                else
                {
                    progressRing.fillClockwise = false;
                    progressRing.color = negativeColor;
                }
            }

            // Switch to done icon when complete/failed
            if (!isDone && (goal.completed || goal.failed))
            {
                isDone = true;
                
                if (goalIcon != null)
                {
                    // Swap to completed or failed sprite
                    goalIcon.sprite = goal.completed ? completedSprite : failedSprite;
                    goalIcon.color = goal.completed ? completedColor : failedColor;
                    
                    // Pop animation
                    goalIcon.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
                }

                // Dim the face if failed
                if (goal.failed && npcFaceImage != null)
                {
                    npcFaceImage.DOColor(Color.gray, 0.3f);
                }
            }
        }
    }
}
