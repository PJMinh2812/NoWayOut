using UnityEngine;

namespace GloomCraft
{
    /// <summary>
    /// Controls player sprite rotation/flip based on aim direction
    /// </summary>
    public sealed class PlayerSpriteController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController2D controller;
        [SerializeField] private SpriteRenderer spriteRenderer;
        
        [Header("Rotation Settings")]
        [SerializeField] private RotationMode rotationMode = RotationMode.FlipOnly;
        [SerializeField] private bool smoothRotation = true;
        [SerializeField] private float rotationSpeed = 720f; // degrees per second
        
        [Header("Flip Settings")]
        [SerializeField] private float flipThreshold = 90f; // Angle threshold for flipping
        
        private float _currentRotation;

        public enum RotationMode
        {
            None,           // No rotation
            FlipOnly,       // Only flip sprite left/right (best for top-down)
            FullRotation,   // Full 360° rotation (sprite xoay theo aim)
            EightDirection, // Snap to 8 directions (0, 45, 90, 135, 180, 225, 270, 315)
            RotateToAim     // Full rotation + sprite always points in aim direction (best for shooting)
        }

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (controller == null) return;

            float aimAngle = controller.AimAngleDeg;

            switch (rotationMode)
            {
                case RotationMode.None:
                    break;

                case RotationMode.FlipOnly:
                    UpdateFlip(aimAngle);
                    break;

                case RotationMode.FullRotation:
                    UpdateFullRotation(aimAngle);
                    break;

                case RotationMode.EightDirection:
                    UpdateEightDirection(aimAngle);
                    break;

                case RotationMode.RotateToAim:
                    UpdateRotateToAim(aimAngle);
                    break;
            }
        }

        private void UpdateFlip(float aimAngle)
        {
            if (spriteRenderer == null) return;

            // Flip sprite based on aim direction
            // Aim to right (0° ± threshold): face right
            // Aim to left (180° ± threshold): face left
            
            // Normalize angle to -180 to 180 range
            float normalizedAngle = Mathf.DeltaAngle(0, aimAngle);
            
            if (normalizedAngle > flipThreshold)
            {
                // Aim to left
                spriteRenderer.flipX = true;
            }
            else if (normalizedAngle < -flipThreshold)
            {
                // Aim to right
                spriteRenderer.flipX = false;
            }
            // Don't flip if within threshold (avoids jitter)
        }

        private void UpdateFullRotation(float aimAngle)
        {
            float targetRotation = aimAngle;

            if (smoothRotation)
            {
                _currentRotation = Mathf.MoveTowardsAngle(_currentRotation, targetRotation, rotationSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0, 0, _currentRotation);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, targetRotation);
                _currentRotation = targetRotation;
            }
        }

        private void UpdateEightDirection(float aimAngle)
        {
            // Snap to nearest 45-degree angle
            float snappedAngle = Mathf.Round(aimAngle / 45f) * 45f;

            if (smoothRotation)
            {
                _currentRotation = Mathf.MoveTowardsAngle(_currentRotation, snappedAngle, rotationSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0, 0, _currentRotation);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, snappedAngle);
                _currentRotation = snappedAngle;
            }
        }

        private void UpdateRotateToAim(float aimAngle)
        {
            // For 2D top-down: Only flip sprite left/right, don't rotate transform
            // This keeps character upright while still facing the aim direction
            if (spriteRenderer == null) return;

            // Normalize angle to -180 to 180 range
            float normalizedAngle = Mathf.DeltaAngle(0, aimAngle);
            
            // Flip based on which side the mouse is on
            // Right side (-90° to +90°): face right (flipX = false)
            // Left side (90° to 270°): face left (flipX = true)
            if (normalizedAngle > 90f || normalizedAngle < -90f)
            {
                // Aim to left
                spriteRenderer.flipX = true;
            }
            else
            {
                // Aim to right
                spriteRenderer.flipX = false;
            }
            
            // Keep transform rotation at zero (upright)
            transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// Set rotation mode at runtime
        /// </summary>
        public void SetRotationMode(RotationMode mode)
        {
            rotationMode = mode;
            if (mode == RotationMode.FlipOnly || mode == RotationMode.None)
            {
                // Reset rotation when switching to flip-only
                transform.rotation = Quaternion.identity;
                _currentRotation = 0;
            }
        }
    }
}
