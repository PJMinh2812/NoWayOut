using UnityEngine;

namespace GloomCraft
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

            // Cập nhật Speed parameter
            float speed = rb.linearVelocity.magnitude;
            animator.SetFloat("Speed", speed);

            // Cập nhật IsRolling
            if (playerController != null)
            {
                animator.SetBool("IsRolling", playerController.IsRolling);
            }
        }

        // Gọi từ script khác khi nhận damage
        public void TriggerDamage()
        {
            if (animator != null)
            {
                animator.SetTrigger("TakeDamage");
            }
        }

        // Gọi khi chết
        public void TriggerDeath()
        {
            if (animator != null)
            {
                animator.SetBool("IsDead", true);
            }
        }
    }
}