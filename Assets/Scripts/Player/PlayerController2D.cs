using UnityEngine;
using UnityEngine.InputSystem;

namespace NWO
{
    // WASD/Arrow di chuyển, Space/Shift dash, Mouse ngắm
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveAcceleration = 45f;
        [SerializeField] private float maxMoveSpeed = 5.0f;
        [SerializeField] private float linearDrag = 8f;

        [Header("Roll")]
        [SerializeField] private float rollSpeed = 10f;
        [SerializeField] private float rollDuration = 0.20f;
        [SerializeField] private float rollCooldown = 0.50f;

        [Header("Aim")]
        [SerializeField] private Camera worldCamera;

        [Header("Sprite Flip")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool flipByMovement = true;
        [SerializeField] private bool flipByAim = true;
        [SerializeField] private float flipThreshold = 0.1f; // Ngưỡng để tránh flip liên tục

        public float AimAngleDeg { get; private set; }
        public bool IsRolling => _rollingTimeRemaining > 0f;
        public Vector2 DashDirection { get; private set; }
        public bool IsFacingRight { get; private set; } = true;

        private Rigidbody2D _rb;
        private PlayerStamina _stamina;
        private float _cooldownRemaining;
        private float _rollingTimeRemaining;
        private Vector2 _rollVelocity;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _stamina = GetComponent<PlayerStamina>();
            if (worldCamera == null) worldCamera = Camera.main;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            UpdateAim();
            UpdateFlip();

            if (_cooldownRemaining > 0f) _cooldownRemaining -= Time.deltaTime;
            if (_rollingTimeRemaining > 0f) _rollingTimeRemaining -= Time.deltaTime;

            if (!IsRolling && _cooldownRemaining <= 0f && GetRollPressed())
            {
                var input = GetMoveInput();
                if (input.sqrMagnitude > 0.0001f)
                {
                    // Kiểm tra stamina trước khi roll
                    if (_stamina == null || _stamina.CanRoll())
                    {
                        StartRoll(input.normalized);
                    }
                    else
                    {
                        Debug.Log("[Player] Not enough stamina to roll!");
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            _rb.linearDamping = linearDrag;

            if (IsRolling)
            {
                _rb.linearVelocity = _rollVelocity;
                return;
            }

            var input = GetMoveInput();
            if (input.sqrMagnitude > 1f) input.Normalize();

            _rb.AddForce(input * moveAcceleration, ForceMode2D.Force);

            var v = _rb.linearVelocity;
            if (v.magnitude > maxMoveSpeed)
            {
                _rb.linearVelocity = v.normalized * maxMoveSpeed;
            }
        }

        private void StartRoll(Vector2 dir)
        {
            _rollingTimeRemaining = rollDuration;
            _cooldownRemaining = rollCooldown;
            _rollVelocity = dir * rollSpeed;
            DashDirection = dir.normalized;
            
            // Tiêu tốn stamina
            if (_stamina != null)
            {
                _stamina.TryConsumeRoll();
            }
        }

        private void UpdateFlip()
        {
            if (spriteRenderer == null) return;

            bool shouldFlip = false;
            bool faceRight = IsFacingRight;

            // Ưu tiên flip theo aim (khi cast spell)
            if (flipByAim)
            {
                // Aim angle: 0° = right, 180° = left, ±90° = up/down
                float aimX = Mathf.Cos(AimAngleDeg * Mathf.Deg2Rad);
                
                if (Mathf.Abs(aimX) > flipThreshold)
                {
                    faceRight = aimX > 0f;
                    shouldFlip = true;
                }
            }

            // Nếu không aim hoặc aim thẳng đứng, flip theo movement
            if (!shouldFlip && flipByMovement)
            {
                var input = GetMoveInput();
                
                if (Mathf.Abs(input.x) > flipThreshold)
                {
                    faceRight = input.x > 0f;
                    shouldFlip = true;
                }
            }

            // Thực hiện flip nếu hướng thay đổi
            if (shouldFlip && faceRight != IsFacingRight)
            {
                IsFacingRight = faceRight;
                spriteRenderer.flipX = !IsFacingRight;
            }
        }

        private void UpdateAim()
        {
            if (worldCamera == null) return;
            var mouse = Mouse.current.position.ReadValue();
            var mousePos = new Vector3(mouse.x, mouse.y, worldCamera.nearClipPlane);
            var world = worldCamera.ScreenToWorldPoint(mousePos);
            var delta = (Vector2)world - (Vector2)transform.position;
            AimAngleDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        }

        private static Vector2 GetMoveInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            var x = 0f;
            var y = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;

            return new Vector2(x, y);
        }

        private static bool GetRollPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;

            return keyboard.spaceKey.wasPressedThisFrame
                   || keyboard.leftShiftKey.wasPressedThisFrame
                   || keyboard.rightShiftKey.wasPressedThisFrame;
        }
    }
}


