using UnityEngine;

namespace NWO
{
    // Xoay/flip sprite Player theo hướng ngắm
    public sealed class PlayerSpriteController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController2D controller;
        [SerializeField] private SpriteRenderer spriteRenderer;
        
        [Header("Rotation Settings")]
        [SerializeField] private RotationMode rotationMode = RotationMode.FlipOnly;
        [SerializeField] private bool smoothRotation = true;
        [SerializeField] private float rotationSpeed = 720f;
        
        [Header("Flip Settings")]
        [SerializeField] private float flipThreshold = 90f;
        
        private float _currentRotation;

        public enum RotationMode
        {
            None,
            FlipOnly,
            FullRotation,
            EightDirection,
            RotateToAim
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

            float normalizedAngle = Mathf.DeltaAngle(0, aimAngle);
            
            if (normalizedAngle > flipThreshold)
            {
                spriteRenderer.flipX = true;
            }
            else if (normalizedAngle < -flipThreshold)
            {
                spriteRenderer.flipX = false;
            }
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
            if (spriteRenderer == null) return;

            float normalizedAngle = Mathf.DeltaAngle(0, aimAngle);
            
            if (normalizedAngle > 90f || normalizedAngle < -90f)
            {
                spriteRenderer.flipX = true;
            }
            else
            {
                spriteRenderer.flipX = false;
            }
            
            transform.rotation = Quaternion.identity;
        }

        public void SetRotationMode(RotationMode mode)
        {
            rotationMode = mode;
            if (mode == RotationMode.FlipOnly || mode == RotationMode.None)
            {
                transform.rotation = Quaternion.identity;
                _currentRotation = 0;
            }
        }
    }
}
