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

        // Cached animator parameter hashes - avoid string lookup every frame
        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashIsDashing = Animator.StringToHash("IsDashing");
        private static readonly int HashIsRolling = Animator.StringToHash("IsRolling");
        private static readonly int HashDashProgress = Animator.StringToHash("DashProgress");
        private static readonly int HashHorizontal = Animator.StringToHash("Horizontal");
        private static readonly int HashVertical = Animator.StringToHash("Vertical");

        // Previous frame values for dirty checking
        private float _prevSpeed;
        private bool _prevIsDashing;
        private float _prevDashProgress;
        private float _prevHorizontal;
        private float _prevVertical;

        private const float VALUE_CHANGE_THRESHOLD = 0.01f;

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

            // Only update animator parameters when values actually change
            if (Mathf.Abs(speed - _prevSpeed) > VALUE_CHANGE_THRESHOLD)
            {
                animator.SetFloat(HashSpeed, speed);
                _prevSpeed = speed;
            }

            if (playerController != null)
            {
                bool isDashing = playerController.IsDashing;
                
                if (isDashing != _prevIsDashing)
                {
                    animator.SetBool(HashIsDashing, isDashing);
                    animator.SetBool(HashIsRolling, isDashing);
                    _prevIsDashing = isDashing;
                }

                float dashProgress = playerController.DashProgress;
                if (Mathf.Abs(dashProgress - _prevDashProgress) > VALUE_CHANGE_THRESHOLD)
                {
                    animator.SetFloat(HashDashProgress, dashProgress);
                    _prevDashProgress = dashProgress;
                }

                float h, v;
                if (isDashing)
                {
                    Vector2 dashDir = playerController.DashDirection;
                    h = dashDir.x;
                    v = dashDir.y;
                }
                else if (speed > 0.1f)
                {
                    Vector2 direction = rb.linearVelocity.normalized;
                    h = direction.x;
                    v = direction.y;
                }
                else
                {
                    return;
                }

                if (Mathf.Abs(h - _prevHorizontal) > VALUE_CHANGE_THRESHOLD)
                {
                    animator.SetFloat(HashHorizontal, h);
                    _prevHorizontal = h;
                }
                if (Mathf.Abs(v - _prevVertical) > VALUE_CHANGE_THRESHOLD)
                {
                    animator.SetFloat(HashVertical, v);
                    _prevVertical = v;
                }
            }
        }
    }
}