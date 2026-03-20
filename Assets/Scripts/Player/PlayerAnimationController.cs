using UnityEngine;
using System.Collections.Generic;

namespace NWO
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerController2D playerController;
        [SerializeField] private Rigidbody2D rb;

        private readonly HashSet<string> availableParams = new HashSet<string>();

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (playerController == null) playerController = GetComponent<PlayerController2D>();
            if (rb == null) rb = GetComponent<Rigidbody2D>();

            CacheAnimatorParameters();
        }

        private void Update()
        {
            UpdateAnimations();
        }

        private void UpdateAnimations()
        {
            if (animator == null || rb == null) return;

            float speed = rb.linearVelocity.magnitude;
            SetFloatIfExists("Speed", speed);

            if (playerController != null)
            {
                bool isDashing = playerController.IsDashing;
                
                SetBoolIfExists("IsDashing", isDashing);
                SetBoolIfExists("IsRolling", isDashing);
                SetFloatIfExists("DashProgress", playerController.DashProgress);

                if (isDashing)
                {
                    Vector2 dashDir = playerController.DashDirection;
                    SetFloatIfExists("Horizontal", dashDir.x);
                    SetFloatIfExists("Vertical", dashDir.y);
                }
                else if (speed > 0.1f)
                {
                    Vector2 direction = rb.linearVelocity.normalized;
                    SetFloatIfExists("Horizontal", direction.x);
                    SetFloatIfExists("Vertical", direction.y);
                }
            }
        }

        private void CacheAnimatorParameters()
        {
            availableParams.Clear();
            if (animator == null) return;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
                availableParams.Add(parameter.name);
        }

        private void SetFloatIfExists(string parameterName, float value)
        {
            if (availableParams.Contains(parameterName))
                animator.SetFloat(parameterName, value);
        }

        private void SetBoolIfExists(string parameterName, bool value)
        {
            if (availableParams.Contains(parameterName))
                animator.SetBool(parameterName, value);
        }
    }
}