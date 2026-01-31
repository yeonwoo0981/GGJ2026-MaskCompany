using UnityEngine;
using UnityEngine.InputSystem;

namespace MaskCompany
{
    [RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Mask")]
        [SerializeField] private MaskType currentMask = MaskType.Agreeable;

        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Vector2 moveInput;

        public MaskType CurrentMask => currentMask;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            UpdateMaskVisual();
        }

        private void Update()
        {
            HandleMovementInput();
            HandleMaskInput();
        }

        private void HandleMovementInput()
        {
            Vector2 input = Vector2.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1;
            }
            moveInput = input.normalized;
        }

        private void HandleMaskInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) SetMask(MaskType.Agreeable);
            if (kb.digit2Key.wasPressedThisFrame) SetMask(MaskType.Assertive);
            if (kb.digit3Key.wasPressedThisFrame) SetMask(MaskType.Analytical);
            if (kb.digit4Key.wasPressedThisFrame) SetMask(MaskType.Expressive);
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }

        public void SetMask(MaskType mask)
        {
            if (currentMask == mask) return;
            currentMask = mask;
            UpdateMaskVisual();
        }

        private void UpdateMaskVisual()
        {
            if (spriteRenderer == null) return;
            spriteRenderer.color = GetMaskColor(currentMask);
        }

        public static Color GetMaskColor(MaskType mask)
        {
            return mask switch
            {
                MaskType.Agreeable => new Color(0.4f, 0.8f, 0.4f),   // Green
                MaskType.Assertive => new Color(0.9f, 0.4f, 0.3f),   // Red-orange
                MaskType.Analytical => new Color(0.3f, 0.5f, 0.9f),  // Blue
                MaskType.Expressive => new Color(0.9f, 0.8f, 0.2f),  // Yellow
                _ => Color.white
            };
        }
    }
}
