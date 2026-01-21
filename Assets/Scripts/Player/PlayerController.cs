using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulKnightClone.Player
{
    /// <summary>
    /// Controller chính cho Player - xử lý di chuyển 8 hướng, Dash với I-frames, và sprite flip
    /// Sử dụng Input System mới của Unity để hỗ trợ cả WASD và Joystick
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float acceleration = 50f;
        [SerializeField] private float deceleration = 50f;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 15f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 1f;
        [SerializeField] private TrailRenderer dashTrail;
        
        private bool canDash = true;
        private bool isDashing = false;
        private float dashTimer = 0f;
        private Vector2 dashDirection;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer characterSprite;
        [SerializeField] private Transform weaponPivot;

        // Components
        private Rigidbody2D rb;
        private PlayerStats stats;
        private Camera mainCamera;

        // Input
        private Vector2 moveInput;
        private Vector2 mouseWorldPos;
        private bool dashPressed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stats = GetComponent<PlayerStats>();
            mainCamera = Camera.main;

            // Configure Rigidbody2D
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void Start()
        {
            if (dashTrail != null)
            {
                dashTrail.emitting = false;
            }
        }

        private void Update()
        {
            if (isDashing)
            {
                HandleDashDuration();
            }
            else
            {
                HandleMouseLook();
                HandleSpriteFlip();
            }

            HandleDashCooldown();
        }

        private void FixedUpdate()
        {
            if (isDashing)
            {
                PerformDash();
            }
            else
            {
                PerformMovement();
            }
        }

        #region Movement
        private void PerformMovement()
        {
            Vector2 targetVelocity = moveInput * moveSpeed;

            // Smooth acceleration/deceleration
            float accelRate = (moveInput.magnitude > 0) ? acceleration : deceleration;
            rb.velocity = Vector2.MoveTowards(rb.velocity, targetVelocity, accelRate * Time.fixedDeltaTime);
        }
        #endregion

        #region Dash System
        public void OnDash(InputAction.CallbackContext context)
        {
            if (context.performed && canDash && !isDashing)
            {
                StartDash();
            }
        }

        private void StartDash()
        {
            isDashing = true;
            canDash = false;
            dashTimer = dashDuration;

            // Dash theo hướng di chuyển, nếu không di chuyển thì dash theo hướng nhìn
            if (moveInput.magnitude > 0.1f)
            {
                dashDirection = moveInput.normalized;
            }
            else
            {
                // Dash về phía chuột
                Vector2 directionToMouse = (mouseWorldPos - (Vector2)transform.position).normalized;
                dashDirection = directionToMouse;
            }

            // Kích hoạt I-frames
            stats.SetInvincible(dashDuration);

            // Visual effect
            if (dashTrail != null)
            {
                dashTrail.emitting = true;
            }
        }

        private void PerformDash()
        {
            rb.velocity = dashDirection * dashSpeed;
        }

        private void HandleDashDuration()
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                EndDash();
            }
        }

        private void EndDash()
        {
            isDashing = false;
            rb.velocity = Vector2.zero;

            if (dashTrail != null)
            {
                dashTrail.emitting = false;
            }
        }

        private void HandleDashCooldown()
        {
            if (!canDash && !isDashing)
            {
                dashCooldown -= Time.deltaTime;
                if (dashCooldown <= 0)
                {
                    canDash = true;
                    dashCooldown = 1f; // Reset cooldown
                }
            }
        }
        #endregion

        #region Mouse Look & Sprite Flip
        private void HandleMouseLook()
        {
            // Lấy vị trí chuột trong world space
            mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }

        private void HandleSpriteFlip()
        {
            if (characterSprite == null) return;

            // Flip sprite dựa vào vị trí chuột
            Vector2 directionToMouse = mouseWorldPos - (Vector2)transform.position;
            
            if (directionToMouse.x > 0)
            {
                characterSprite.flipX = false;
                if (weaponPivot != null)
                {
                    weaponPivot.localScale = new Vector3(1, 1, 1);
                }
            }
            else if (directionToMouse.x < 0)
            {
                characterSprite.flipX = true;
                if (weaponPivot != null)
                {
                    weaponPivot.localScale = new Vector3(1, -1, 1);
                }
            }
        }
        #endregion

        #region Input System Callbacks
        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        // Dash đã được xử lý ở trên trong OnDash()
        #endregion

        #region Getters
        public Vector2 GetMoveInput() => moveInput;
        public Vector2 GetMouseWorldPosition() => mouseWorldPos;
        public bool IsDashing() => isDashing;
        #endregion

        private void OnDrawGizmos()
        {
            // Debug visualization
            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, mouseWorldPos);
            }
        }
    }
}
