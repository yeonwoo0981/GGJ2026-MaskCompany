using System.Collections;
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
        [SerializeField] private MaskType currentMask = MaskType.Joy;
        [SerializeField] private SpriteRenderer maskRenderer; // Child object that displays the mask
        [SerializeField] private float maskChangeAnimDuration = 0.5f; // Duration of mask change animation
        
        [Header("Mask Sprites")]
        [SerializeField] private Sprite joySprite;
        [SerializeField] private Sprite angerSprite;
        [SerializeField] private Sprite fearSprite;
        // Neutral has no sprite (mask hidden)

        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Vector2 moveInput;
        private Animator bodyAnimator;
        private bool isChangingMask; // True while mask change animation is playing

        public MaskType CurrentMask => currentMask;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooth out physics for camera
            
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // Get animator from child (Body)
            bodyAnimator = GetComponentInChildren<Animator>();
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
            // Check if mask changing is allowed (tutorial mode)
            if (TutorialManager.TutoMode && !TutorialManager.Instance.CanChangeMask)
            {
                return;
            }
            
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) SetMask(MaskType.Joy);
            if (kb.digit2Key.wasPressedThisFrame) SetMask(MaskType.Neutral);
            if (kb.digit3Key.wasPressedThisFrame) SetMask(MaskType.Anger);
            if (kb.digit4Key.wasPressedThisFrame) SetMask(MaskType.Fear);
        }

        private void FixedUpdate()
        {
            // Check if movement is allowed (tutorial mode)
            if (TutorialManager.TutoMode && !TutorialManager.Instance.CanPlayerMove)
            {
                rb.linearVelocity = Vector2.zero;
                if (!isChangingMask) ResetAnimatorToFirstFrame();
                return;
            }
            
            // Block movement during mask change animation
            if (isChangingMask)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
            
            rb.linearVelocity = moveInput * moveSpeed;
            
            // Enable animator only when moving
            if (bodyAnimator != null)
            {
                bool isMoving = moveInput.sqrMagnitude > 0.01f;
                if (isMoving)
                {
                    bodyAnimator.enabled = true;
                }
                else
                {
                    ResetAnimatorToFirstFrame();
                }
            }
        }
        
        private void ResetAnimatorToFirstFrame()
        {
            if (bodyAnimator == null) return;
            bodyAnimator.enabled = true;
            bodyAnimator.Play(0, 0, 0f); // Play first state, layer 0, at time 0
            bodyAnimator.Update(0f); // Force update to apply the frame
            bodyAnimator.enabled = false;
        }

        public void SetMask(MaskType mask)
        {
            if (currentMask == mask) return;
            if (isChangingMask) return; // Don't change mask while animating
            
            StartCoroutine(ChangeMaskCoroutine(mask));
        }
        
        private IEnumerator ChangeMaskCoroutine(MaskType newMask)
        {
            isChangingMask = true;
            
            // Stop movement and play mask change animation
            if (bodyAnimator != null)
            {
                bodyAnimator.enabled = true;
                bodyAnimator.SetTrigger("MaskChange");
            }
            
            // Wait for animation to finish
            yield return new WaitForSeconds(maskChangeAnimDuration);
            
            // Now change the mask visual
            currentMask = newMask;
            UpdateMaskVisual();
            
            // Notify tutorial manager if in tutorial mode
            if (TutorialManager.TutoMode && TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnMaskUsed(newMask);
            }
            
            isChangingMask = false;
        }

        private void UpdateMaskVisual()
        {
            if (maskRenderer == null) return;
            
            switch (currentMask)
            {
                case MaskType.Joy:
                    maskRenderer.sprite = joySprite;
                    maskRenderer.enabled = joySprite != null;
                    break;
                case MaskType.Anger:
                    maskRenderer.sprite = angerSprite;
                    maskRenderer.enabled = angerSprite != null;
                    break;
                case MaskType.Fear:
                    maskRenderer.sprite = fearSprite;
                    maskRenderer.enabled = fearSprite != null;
                    break;
                case MaskType.Neutral:
                default:
                    maskRenderer.enabled = false; // No mask for neutral
                    break;
            }
        }
    }
}
