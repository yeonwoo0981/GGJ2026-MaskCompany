using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace MaskCompany
{
    public class MaskDisplayUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController player;

        [Header("Current Mask Display")]
        [SerializeField] private Image currentMaskImage;
        [SerializeField] private TextMeshProUGUI currentMaskText;

        [Header("Mask Slots")]
        [SerializeField] private MaskSlot[] maskSlots;

        [Header("Sprites")]
        [SerializeField] private Sprite joySprite;
        [SerializeField] private Sprite neutralSprite;
        [SerializeField] private Sprite angerSprite;
        [SerializeField] private Sprite fearSprite;

        private MaskType lastMask;

        [System.Serializable]
        public class MaskSlot
        {
            public MaskType maskType;
            public Image image;
            public Image highlight;
            public TextMeshProUGUI keyText;
        }

        private void Start()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }

            InitializeSlots();
            UpdateDisplay();
        }

        private void InitializeSlots()
        {
            for (int i = 0; i < maskSlots.Length; i++)
            {
                var slot = maskSlots[i];
                if (slot.image != null)
                {
                    slot.image.sprite = GetMaskSprite(slot.maskType);
                    slot.image.color = PlayerController.GetMaskColor(slot.maskType);
                }
                if (slot.keyText != null)
                {
                    slot.keyText.text = (i + 1).ToString();
                }
                if (slot.highlight != null)
                {
                    slot.highlight.gameObject.SetActive(false);
                }
            }
        }

        private void Update()
        {
            if (player == null) return;

            if (player.CurrentMask != lastMask)
            {
                lastMask = player.CurrentMask;
                UpdateDisplay();
                AnimateMaskChange();
            }
        }

        private void UpdateDisplay()
        {
            if (player == null) return;

            MaskType current = player.CurrentMask;

            // Update current mask display
            if (currentMaskImage != null)
            {
                currentMaskImage.sprite = GetMaskSprite(current);
                currentMaskImage.color = PlayerController.GetMaskColor(current);
            }

            if (currentMaskText != null)
            {
                currentMaskText.text = current.ToString();
            }

            // Update slot highlights
            foreach (var slot in maskSlots)
            {
                bool isSelected = slot.maskType == current;
                if (slot.highlight != null)
                {
                    slot.highlight.gameObject.SetActive(isSelected);
                }
                if (slot.image != null)
                {
                    slot.image.transform.localScale = isSelected ? Vector3.one * 1.1f : Vector3.one;
                }
            }
        }

        private void AnimateMaskChange()
        {
            if (currentMaskImage != null)
            {
                currentMaskImage.transform.DOKill();
                currentMaskImage.transform.localScale = Vector3.one;
                currentMaskImage.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
            }
        }

        private Sprite GetMaskSprite(MaskType mask)
        {
            return mask switch
            {
                MaskType.Joy => joySprite,
                MaskType.Neutral => neutralSprite,
                MaskType.Anger => angerSprite,
                MaskType.Fear => fearSprite,
                _ => null
            };
        }
    }
}
