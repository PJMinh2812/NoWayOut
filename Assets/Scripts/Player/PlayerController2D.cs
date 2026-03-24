using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace NWO
{
    public enum PlayerMoveState
    {
        Normal,
        Dashing,
        Stunned
    }

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

        [Header("Fallback Light")]
        [SerializeField] private bool autoCreateFallbackLight = true;
        [SerializeField] private float fallbackLightRadius = 1.5f;
        [SerializeField] private float fallbackLightIntensity = 1.1f;
        [SerializeField] private Color fallbackLightColor = new Color(0.95f, 0.9f, 0.75f);

        public float AimAngleDeg { get; private set; }
        public PlayerMoveState CurrentState { get; private set; } = PlayerMoveState.Normal;
        public bool IsDashing => CurrentState == PlayerMoveState.Dashing;
        public bool IsRolling => IsDashing;
        public Vector2 DashDirection { get; private set; }
        public float DashProgress => _dashMaxDuration > 0f ? Mathf.Clamp01(_dashElapsed / _dashMaxDuration) : 0f;
        public bool IsFacingRight { get; private set; } = true;
        public float MaxMoveSpeed => maxMoveSpeed;
        public float DashSpeed => dashSpeed;

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

        private int _originalLayer;
        private Light2D _fallbackPlayerLight;

        /// <summary>Tăng tốc độ di chuyển tối đa (upgrade system)</summary>
        public void AddMaxMoveSpeed(float amount) => maxMoveSpeed += amount;

        /// <summary>Tăng tốc độ dash (upgrade system)</summary>
        public void AddDashSpeed(float amount) => dashSpeed += amount;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _stamina = GetComponent<PlayerStamina>();
            _statusEffects = GetComponent<PlayerStatusEffects>();
            _playerCollider = GetComponent<Collider2D>();
            if (worldCamera == null) worldCamera = Camera.main;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            _originalLayer = gameObject.layer;

            // Warm afterimage pool for zero-alloc dash visuals
            DashAfterImage.WarmPool();
            EnsureFallbackPlayerLight();
        }

        private void LateUpdate()
        {
            if (!autoCreateFallbackLight)
                return;

            if (_fallbackPlayerLight == null)
            {
                EnsureFallbackPlayerLight();
            }
            else if (!_fallbackPlayerLight.enabled)
            {
                _fallbackPlayerLight.enabled = true;
            }

            if (DungeonLightingManager.Instance == null && _fallbackPlayerLight != null)
            {
                if (_fallbackPlayerLight.pointLightOuterRadius > fallbackLightRadius)
                    _fallbackPlayerLight.pointLightOuterRadius = fallbackLightRadius;

                if (_fallbackPlayerLight.pointLightInnerRadius > fallbackLightRadius * 0.25f)
                    _fallbackPlayerLight.pointLightInnerRadius = fallbackLightRadius * 0.25f;
            }
        }

        private void EnsureFallbackPlayerLight()
        {
            if (!autoCreateFallbackLight)
                return;

            // If DungeonLightingManager exists, let it own the light setup.
            if (DungeonLightingManager.Instance != null)
                return;

            Transform existing = transform.Find("PlayerLight");
            if (existing != null)
            {
                _fallbackPlayerLight = existing.GetComponent<Light2D>();
            }

            if (_fallbackPlayerLight == null)
            {
                GameObject lightObj = new GameObject("PlayerLight");
                lightObj.transform.SetParent(transform);
                lightObj.transform.localPosition = Vector3.zero;

                _fallbackPlayerLight = lightObj.AddComponent<Light2D>();
                _fallbackPlayerLight.lightType = Light2D.LightType.Point;
            }

            _fallbackPlayerLight.enabled = true;
            _fallbackPlayerLight.pointLightOuterRadius = fallbackLightRadius;
            _fallbackPlayerLight.pointLightInnerRadius = fallbackLightRadius * 0.25f;
            _fallbackPlayerLight.intensity = fallbackLightIntensity;
            _fallbackPlayerLight.color = fallbackLightColor;
            _fallbackPlayerLight.shadowIntensity = 0.25f;
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
                    if (_statusEffects == null || !_statusEffects.CannotMove)
                        TransitionToState(PlayerMoveState.Normal);
                    break;
            }
        }

        private void FixedUpdate()
        {
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

            if (_statusEffects != null && _statusEffects.CannotMove)
            {
                _rb.linearVelocity = Vector2.zero;
                if (CurrentState != PlayerMoveState.Stunned)
                    TransitionToState(PlayerMoveState.Stunned);
                return;
            }

            var input = GetMoveInputWithEffects();
            if (input.sqrMagnitude > 1f) input.Normalize();

            float speedMultiplier = _statusEffects != null ? _statusEffects.MoveSpeedMultiplier : 1f;
            
            _rb.AddForce(input * moveAcceleration * speedMultiplier, ForceMode2D.Force);

            var v = _rb.linearVelocity;
            float effectiveMaxSpeed = maxMoveSpeed * speedMultiplier;
            if (v.magnitude > effectiveMaxSpeed)
            {
                _rb.linearVelocity = v.normalized * effectiveMaxSpeed;
            }
        }

        private void TransitionToState(PlayerMoveState newState)
        {
            switch (CurrentState)
            {
                case PlayerMoveState.Dashing:
                    OnDashExit();
                    break;
            }

            CurrentState = newState;

            switch (newState)
            {
                case PlayerMoveState.Dashing:
                    OnDashEnter();
                    break;
            }
        }

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

        private void OnDashEnter()
        {
            _dashElapsed = 0f;
            _dashMaxDuration = dashMaxDuration;
            _dashVelocity = DashDirection * dashSpeed;
            _dashKeyHeld = true;
            _afterImageTimer = 0f;

            if (_stamina != null)
            {
                _stamina.TryConsumeRoll();
            }

            SpawnAfterImage();
        }

        private void UpdateDashState()
        {
            _dashElapsed += Time.deltaTime;
            _dashKeyHeld = GetDashHeld();

            bool minTimePassed = _dashElapsed >= dashMinDuration;
            bool maxTimeReached = _dashElapsed >= _dashMaxDuration;
            bool outOfStamina = false;

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

            if (maxTimeReached || (minTimePassed && !_dashKeyHeld) || outOfStamina)
            {
                TransitionToState(PlayerMoveState.Normal);
                return;
            }

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

            if (_rb != null)
            {
                _rb.linearVelocity = DashDirection * maxMoveSpeed * 0.5f;
            }
        }

        private void SpawnAfterImage()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null) return;

            DashAfterImage.GetFromPool(
                transform.position,
                transform.rotation,
                transform.localScale,
                spriteRenderer.sprite,
                dashAfterImageColor,
                spriteRenderer.flipX,
                spriteRenderer.sortingLayerID,
                spriteRenderer.sortingOrder - 1,
                afterImageLifetime
            );
        }

        private void UpdateFlip()
        {
            if (spriteRenderer == null) return;

            bool shouldFlip = false;
            bool faceRight = IsFacingRight;

            if (flipByAim)
            {
                float aimX = Mathf.Cos(AimAngleDeg * Mathf.Deg2Rad);
                
                if (Mathf.Abs(aimX) > flipThreshold)
                {
                    faceRight = aimX > 0f;
                    shouldFlip = true;
                }
            }

            if (!shouldFlip && flipByMovement)
            {
                var input = GetMoveInput();
                
                if (Mathf.Abs(input.x) > flipThreshold)
                {
                    faceRight = input.x > 0f;
                    shouldFlip = true;
                }
            }

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
            var kb = KeyBindManager.Instance;
            if (kb != null)
            {
                var x = 0f;
                var y = 0f;
                if (kb.IsPressed(KeyBindManager.ACT_MOVE_LEFT))  x -= 1f;
                if (kb.IsPressed(KeyBindManager.ACT_MOVE_RIGHT)) x += 1f;
                if (kb.IsPressed(KeyBindManager.ACT_MOVE_UP))    y += 1f;
                if (kb.IsPressed(KeyBindManager.ACT_MOVE_DOWN))  y -= 1f;
                return new Vector2(x, y);
            }

            // Fallback if KeyBindManager not ready
            var keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            var fx = 0f;
            var fy = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) fx -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) fx += 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) fy += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) fy -= 1f;

            return new Vector2(fx, fy);
        }

        private Vector2 GetMoveInputWithEffects()
        {
            var input = GetMoveInput();
            if (_statusEffects != null)
            {
                input = _statusEffects.ApplyConfusion(input);
            }
            
            return input;
        }

        private static bool GetDashPressed()
        {
            var kb = KeyBindManager.Instance;
            if (kb != null)
                return kb.WasPressedThisFrame(KeyBindManager.ACT_DASH);

            var keyboard = Keyboard.current;
            if (keyboard == null) return false;

            return keyboard.leftShiftKey.wasPressedThisFrame
                   || keyboard.rightShiftKey.wasPressedThisFrame;

        }

        private static bool GetDashHeld()
        {
            var kb = KeyBindManager.Instance;
            if (kb != null)
                return kb.IsPressed(KeyBindManager.ACT_DASH);

            var keyboard = Keyboard.current;
            if (keyboard == null) return false;

            return keyboard.leftShiftKey.isPressed
                   || keyboard.rightShiftKey.isPressed;
                   
        }
    }
}
