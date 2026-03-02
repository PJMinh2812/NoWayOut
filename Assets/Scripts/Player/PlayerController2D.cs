using UnityEngine;
using UnityEngine.InputSystem;

namespace NWO
{
    /// <summary>
    /// Trạng thái của Player Controller
    /// </summary>
    public enum PlayerMoveState
    {
        Normal,     // Di chuyển bình thường
        Dashing,    // Đang dash - bất tử, xuyên qua bẫy
        Stunned     // Bị stun/freeze
    }

    // WASD/Arrow di chuyển, Space/Shift dash, Mouse ngắm
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveAcceleration = 45f;
        [SerializeField] private float maxMoveSpeed = 5.0f;
        [SerializeField] private float linearDrag = 8f;

        [Header("Dash")]
        [Tooltip("Tốc độ dash")]
        [SerializeField] private float dashSpeed = 14f;
        [Tooltip("Thời gian dash tối thiểu khi nhấn nhanh")]
        [SerializeField] private float dashMinDuration = 0.20f;
        [Tooltip("Thời gian dash tối đa khi giữ phím")]
        [SerializeField] private float dashMaxDuration = 0.60f;
        [Tooltip("Cooldown sau khi dash xong")]
        [SerializeField] private float dashCooldown = 1f;
        [Tooltip("Stamina tiêu hao mỗi giây khi kéo dài dash")]
        [SerializeField] private float dashExtendStaminaPerSec = 25f;
        [Tooltip("Số lượng afterimage spawn mỗi giây khi dash")]
        [SerializeField] private float afterImageRate = 15f;
        [Tooltip("Màu afterimage")]
        [SerializeField] private Color dashAfterImageColor = new Color(0.3f, 0.8f, 1f, 0.6f);
        [Tooltip("Thời gian afterimage tồn tại")]
        [SerializeField] private float afterImageLifetime = 0.25f;

        [Header("Aim")]
        [SerializeField] private Camera worldCamera;

        [Header("Sprite Flip")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool flipByMovement = true;
        [SerializeField] private bool flipByAim = true;
        [SerializeField] private float flipThreshold = 0.1f;

        // === PUBLIC PROPERTIES ===
        public float AimAngleDeg { get; private set; }
        
        /// <summary>Trạng thái di chuyển hiện tại</summary>
        public PlayerMoveState CurrentState { get; private set; } = PlayerMoveState.Normal;
        
        /// <summary>True khi đang trong trạng thái Dash (bất tử, né bẫy)</summary>
        public bool IsDashing => CurrentState == PlayerMoveState.Dashing;
        
        /// <summary>Backward compat: IsRolling = IsDashing</summary>
        public bool IsRolling => IsDashing;
        
        /// <summary>Hướng dash hiện tại (normalized)</summary>
        public Vector2 DashDirection { get; private set; }
        
        /// <summary>Tiến trình dash (0 = mới bắt đầu, 1 = hết thời gian max)</summary>
        public float DashProgress => _dashMaxDuration > 0f ? Mathf.Clamp01(_dashElapsed / _dashMaxDuration) : 0f;
        
        public bool IsFacingRight { get; private set; } = true;

        // === PRIVATE ===
        private Rigidbody2D _rb;
        private PlayerStamina _stamina;
        private PlayerStatusEffects _statusEffects;
        private Collider2D _playerCollider;

        // Dash state
        private float _dashCooldownRemaining;
        private float _dashElapsed;
        private float _dashMaxDuration;
        private Vector2 _dashVelocity;
        private bool _dashKeyHeld;
        private float _afterImageTimer;

        // Layer mask cho trap ignore
        private int _originalLayer;
        private static int _dashLayer = -1; // Lazy init

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _stamina = GetComponent<PlayerStamina>();
            _statusEffects = GetComponent<PlayerStatusEffects>();
            _playerCollider = GetComponent<Collider2D>();
            if (worldCamera == null) worldCamera = Camera.main;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            _originalLayer = gameObject.layer;
        }

        private void Update()
        {
            UpdateAim();
            UpdateFlip();

            if (_dashCooldownRemaining > 0f) _dashCooldownRemaining -= Time.deltaTime;

            switch (CurrentState)
            {
                case PlayerMoveState.Normal:
                    UpdateNormalState();
                    break;
                case PlayerMoveState.Dashing:
                    UpdateDashState();
                    break;
                case PlayerMoveState.Stunned:
                    // Chờ status effects hết
                    if (_statusEffects == null || !_statusEffects.CannotMove)
                        TransitionToState(PlayerMoveState.Normal);
                    break;
            }
        }

        private void FixedUpdate()
        {
            // Áp dụng slippery effect nếu có
            float effectiveDrag = linearDrag;
            if (_statusEffects != null)
            {
                float slipperyDrag = _statusEffects.GetSlipperyDrag();
                if (slipperyDrag >= 0f)
                {
                    effectiveDrag = slipperyDrag;
                }
            }
            _rb.linearDamping = effectiveDrag;

            if (IsDashing)
            {
                _rb.linearVelocity = _dashVelocity;
                return;
            }

            // Kiểm tra nếu không thể di chuyển (frozen/stunned)
            if (_statusEffects != null && _statusEffects.CannotMove)
            {
                _rb.linearVelocity = Vector2.zero;
                if (CurrentState != PlayerMoveState.Stunned)
                    TransitionToState(PlayerMoveState.Stunned);
                return;
            }

            var input = GetMoveInputWithEffects();
            if (input.sqrMagnitude > 1f) input.Normalize();

            // Áp dụng speed multiplier từ status effects
            float speedMultiplier = _statusEffects != null ? _statusEffects.MoveSpeedMultiplier : 1f;
            
            _rb.AddForce(input * moveAcceleration * speedMultiplier, ForceMode2D.Force);

            var v = _rb.linearVelocity;
            float effectiveMaxSpeed = maxMoveSpeed * speedMultiplier;
            if (v.magnitude > effectiveMaxSpeed)
            {
                _rb.linearVelocity = v.normalized * effectiveMaxSpeed;
            }
        }

        // ============================================================
        //  STATE MACHINE
        // ============================================================

        private void TransitionToState(PlayerMoveState newState)
        {
            // Exit old state
            switch (CurrentState)
            {
                case PlayerMoveState.Dashing:
                    OnDashExit();
                    break;
            }

            CurrentState = newState;

            // Enter new state
            switch (newState)
            {
                case PlayerMoveState.Dashing:
                    OnDashEnter();
                    break;
            }
        }

        // ============================================================
        //  NORMAL STATE
        // ============================================================

        private void UpdateNormalState()
        {
            bool canDashByStatus = _statusEffects == null || !_statusEffects.CannotRoll;
            
            if (_dashCooldownRemaining <= 0f && canDashByStatus && GetDashPressed())
            {
                var input = GetMoveInputWithEffects();
                if (input.sqrMagnitude > 0.0001f)
                {
                    if (_stamina == null || _stamina.CanRoll())
                    {
                        DashDirection = input.normalized;
                        TransitionToState(PlayerMoveState.Dashing);
                    }
                    else
                    {
                        Debug.Log("[Player] Not enough stamina to dash!");
                    }
                }
            }
        }

        // ============================================================
        //  DASH STATE - Bất tử, xuyên qua bẫy, có thể kéo dài
        // ============================================================

        private void OnDashEnter()
        {
            _dashElapsed = 0f;
            _dashMaxDuration = dashMaxDuration;
            _dashVelocity = DashDirection * dashSpeed;
            _dashKeyHeld = true;
            _afterImageTimer = 0f;

            // Tiêu tốn stamina cơ bản
            if (_stamina != null)
            {
                _stamina.TryConsumeRoll();
            }

            // Spawn afterimage ngay lập tức
            SpawnAfterImage();
        }

        private void UpdateDashState()
        {
            _dashElapsed += Time.deltaTime;
            _dashKeyHeld = GetDashHeld();

            // Kiểm tra kết thúc dash
            bool minTimePassed = _dashElapsed >= dashMinDuration;
            bool maxTimeReached = _dashElapsed >= _dashMaxDuration;
            bool outOfStamina = false;

            // Kéo dài dash: tiêu hao stamina mỗi frame nếu giữ phím
            if (_dashKeyHeld && minTimePassed && !maxTimeReached)
            {
                if (_stamina != null)
                {
                    float drainAmount = dashExtendStaminaPerSec * Time.deltaTime;
                    if (_stamina.CurrentStamina >= drainAmount)
                    {
                        _stamina.ConsumeStamina(drainAmount);
                    }
                    else
                    {
                        outOfStamina = true;
                    }
                }
            }

            // Kết thúc dash nếu: hết thời gian max, HOẶC thả phím sau min duration, HOẶC hết stamina
            if (maxTimeReached || (minTimePassed && !_dashKeyHeld) || outOfStamina)
            {
                TransitionToState(PlayerMoveState.Normal);
                return;
            }

            // Spawn afterimage effect
            _afterImageTimer += Time.deltaTime;
            float afterImageInterval = afterImageRate > 0f ? 1f / afterImageRate : 0.1f;
            if (_afterImageTimer >= afterImageInterval)
            {
                _afterImageTimer -= afterImageInterval;
                SpawnAfterImage();
            }
        }

        private void OnDashExit()
        {
            _dashCooldownRemaining = dashCooldown;
            _dashVelocity = Vector2.zero;

            // Giảm vận tốc mượt sau dash
            if (_rb != null)
            {
                _rb.linearVelocity = DashDirection * maxMoveSpeed * 0.5f;
            }
        }

        // ============================================================
        //  AFTERIMAGE EFFECT
        // ============================================================

        private void SpawnAfterImage()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null) return;

            var ghost = new GameObject("DashAfterImage");
            ghost.transform.position = transform.position;
            ghost.transform.rotation = transform.rotation;
            ghost.transform.localScale = transform.localScale;

            var sr = ghost.AddComponent<SpriteRenderer>();
            sr.sprite = spriteRenderer.sprite;
            sr.color = dashAfterImageColor;
            sr.flipX = spriteRenderer.flipX;
            sr.sortingLayerID = spriteRenderer.sortingLayerID;
            sr.sortingOrder = spriteRenderer.sortingOrder - 1;

            var fader = ghost.AddComponent<DashAfterImage>();
            fader.Init(afterImageLifetime, dashAfterImageColor);
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

        /// <summary>
        /// Lấy input với status effects đã áp dụng (confusion, etc.)
        /// </summary>
        private Vector2 GetMoveInputWithEffects()
        {
            var input = GetMoveInput();
            
            // Áp dụng confusion (đảo ngược điều khiển)
            if (_statusEffects != null)
            {
                input = _statusEffects.ApplyConfusion(input);
            }
            
            return input;
        }

        private static bool GetDashPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;

            return keyboard.spaceKey.wasPressedThisFrame
                   || keyboard.leftShiftKey.wasPressedThisFrame
                   || keyboard.rightShiftKey.wasPressedThisFrame;
        }

        private static bool GetDashHeld()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;

            return keyboard.spaceKey.isPressed
                   || keyboard.leftShiftKey.isPressed
                   || keyboard.rightShiftKey.isPressed;
        }
    }
}


