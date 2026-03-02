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
                bool isDashing = playerController.IsDashing;
                
                // Set cả IsDashing (mới) và IsRolling (backward compat)
                animator.SetBool("IsDashing", isDashing);
                animator.SetBool("IsRolling", isDashing);
                
                // Tiến trình dash (0-1) để Animator blend
                animator.SetFloat("DashProgress", playerController.DashProgress);

                if (isDashing)
                {
                    Vector2 dashDir = playerController.DashDirection;
                    animator.SetFloat("Horizontal", dashDir.x);
                    animator.SetFloat("Vertical", dashDir.y);
                }
                else if (speed > 0.1f)
                {
                    Vector2 direction = rb.linearVelocity.normalized;
                    animator.SetFloat("Horizontal", direction.x);
                    animator.SetFloat("Vertical", direction.y);
                }
            }
        }

        //public void TriggerDamage()
        //{
        //    if (animator != null)
        //    {
        //        animator.SetTrigger("Hurt");
        //    }
        //}

        //public void TriggerDeath()
        //{
        //    if (animator != null)
        //    {
        //        animator.SetBool("IsDead", true);
        //    }
        //}
    }
}