using UnityEngine;
using UnityEngine.InputSystem;

namespace GloomCraft
{
    /// <summary>
    /// First port target: microStudio Player movement + roll using Rigidbody2D.
    /// - WASD/Arrow to move
    /// - Space/Shift to roll (impulse), with cooldown
    /// - Mouse to aim angle (exposed)
    /// </summary>
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

        public float AimAngleDeg { get; private set; }
        public bool IsRolling => _rollingTimeRemaining > 0f;
        public Vector2 DashDirection { get; private set; }

        private Rigidbody2D _rb;
        private float _cooldownRemaining;
        private float _rollingTimeRemaining;
        private Vector2 _rollVelocity;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (worldCamera == null) worldCamera = Camera.main;
        }

        private void Update()
        {
            UpdateAim();

            if (_cooldownRemaining > 0f) _cooldownRemaining -= Time.deltaTime;
            if (_rollingTimeRemaining > 0f) _rollingTimeRemaining -= Time.deltaTime;

            if (!IsRolling && _cooldownRemaining <= 0f && GetRollPressed())
            {
                var input = GetMoveInput();
                if (input.sqrMagnitude > 0.0001f)
                {
                    StartRoll(input.normalized);
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
            DashDirection = dir.normalized; // Lưu hướng dash cho Animator
        }

        private void UpdateAim()
        {
            if (worldCamera == null) return;
            var mouse = Mouse.current.position.ReadValue();
            var world = worldCamera.ScreenToWorldPoint(mouse);
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


