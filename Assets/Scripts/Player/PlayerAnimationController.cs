using UnityEngine;

namespace NWO
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerController2D playerController;
        [SerializeField] private Rigidbody2D rb;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (playerController == null) playerController = GetComponent<PlayerController2D>();
            if (rb == null) rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            UpdateAnimations();
        }

        private void UpdateAnimations()
        {
            if (animator == null || rb == null) return;

            float speed = rb.linearVelocity.magnitude;
            animator.SetFloat("Speed", speed);

            if (playerController != null)
            {
                if (playerController.IsRolling)
                {
                    Vector2 dashDir = playerController.DashDirection;
                    animator.SetFloat("Horizontal", dashDir.x);
                    animator.SetFloat("Vertical", dashDir.y);
                    animator.SetBool("IsRolling", true);
                }
                else if (speed > 0.1f)
                {
                    Vector2 direction = rb.linearVelocity.normalized;
                    animator.SetFloat("Horizontal", direction.x);
                    animator.SetFloat("Vertical", direction.y);
                    animator.SetBool("IsRolling", false);
                }
                else
                {
                    animator.SetBool("IsRolling", false);
                }
            }
        }

        public void TriggerDamage()
        {
            if (animator != null)
            {
                animator.SetTrigger("TakeDamage");
            }
        }

        public void TriggerDeath()
        {
            if (animator != null)
            {
                animator.SetBool("IsDead", true);
            }
        }
    }
}