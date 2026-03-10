using UnityEngine;
using UnityEngine.InputSystem;

namespace NWO
{

    [RequireComponent(typeof(Animator))]
    public class PlayerSpellController : MonoBehaviour
    {
        [Header("Spell Settings")]
        [SerializeField] private float spell01Cooldown = 2f;
        [SerializeField] private float spell02Cooldown = 3f;
        [SerializeField] private float spell03Cooldown = 5f;

        [Header("Spell Damage")]
        [SerializeField] private int spell01Damage = 10;
        [SerializeField] private int spell02Damage = 20;
        [SerializeField] private int spell03Damage = 35;

        [Header("Spell Range (phạm vi bay của từng spell)")]
        [SerializeField] private float spell01Range = 5f;
        [SerializeField] private float spell02Range = 7f;
        [SerializeField] private float spell03Range = 10f;

        [Header("Spell Prefabs (Optional)")]
        [SerializeField] private GameObject spell01Projectile;
        [SerializeField] private GameObject spell02Projectile;
        [SerializeField] private GameObject spell03Projectile;

        private Animator _animator;
        private PlayerController2D _controller;
        private PlayerStamina _stamina;
        private PlayerStatusEffects _statusEffects;
        private PlayerMeleeController _meleeController;

        private float _spell01CooldownRemaining;
        private float _spell02CooldownRemaining;
        private float _spell03CooldownRemaining;

        private int _currentSpell = 0; // 0=Idle, 1-3=Spell
        private int _spellBeforeDamage = 0;
        private bool _isCasting;
        private float _castingTime = 0f;
        private const float MAX_CAST_TIME = 2f;

        public int CurrentSpell => _currentSpell;
        public bool IsCasting => _isCasting;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _controller = GetComponent<PlayerController2D>();
            _stamina = GetComponent<PlayerStamina>();
            _statusEffects = GetComponent<PlayerStatusEffects>();
            _meleeController = GetComponent<PlayerMeleeController>();
        }

        private void Start()
        {
            _animator.SetInteger("SpellType", 0);
        }

        private void Update()
        {
            // Giảm cooldown
            if (_spell01CooldownRemaining > 0f) _spell01CooldownRemaining -= Time.deltaTime;
            if (_spell02CooldownRemaining > 0f) _spell02CooldownRemaining -= Time.deltaTime;
            if (_spell03CooldownRemaining > 0f) _spell03CooldownRemaining -= Time.deltaTime;

            // Timeout nếu animation event không gọi
            if (_isCasting)
            {
                _castingTime += Time.deltaTime;
                if (_castingTime >= MAX_CAST_TIME)
                {
                    Debug.LogWarning("[Spell] Casting timeout! Animation event may be missing.");
                    _isCasting = false;
                    _castingTime = 0f;
                }
            }

            HandleSpellSwitch();

            bool canCastByStatus = _statusEffects == null || !_statusEffects.CannotCast;
            
            if (_controller != null && (_controller.IsRolling || _isCasting))
                return;
            
            if (_meleeController != null && _meleeController.IsAttacking)
                return;

            if (!canCastByStatus)
                return;

            HandleSpellCast();
        }

        private void HandleSpellSwitch()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Không xử lý input khi game đang paused
            if (NWO.PauseMenuUI.GameIsPaused) return;

            // 0: Idle, ESC không dùng ở đây nữa (dành cho PauseMenu)
            if (keyboard.digit0Key.wasPressedThisFrame)
            {
                SwitchToSpell(0);
            }
            else if (keyboard.digit1Key.wasPressedThisFrame)
            {
                SwitchToSpell(1);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                SwitchToSpell(2);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                SwitchToSpell(3);
            }
        }

        private void SwitchToSpell(int spellNumber)
        {
            if (_currentSpell == spellNumber) return;

            _currentSpell = spellNumber;
            _spellBeforeDamage = spellNumber;
            _animator.SetInteger("SpellType", spellNumber);

            if (_isCasting)
                _isCasting = false;
        }

    private void HandleSpellCast()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            bool castInput = mouse.leftButton.wasPressedThisFrame
                          || Keyboard.current.qKey.wasPressedThisFrame;

            if (!castInput) return;
            if (_currentSpell == 0) return;

            bool canCast = _currentSpell switch
            {
                1 => _spell01CooldownRemaining <= 0f && (_stamina == null || _stamina.CanCastSpell(1)),
                2 => _spell02CooldownRemaining <= 0f && (_stamina == null || _stamina.CanCastSpell(2)),
                3 => _spell03CooldownRemaining <= 0f && (_stamina == null || _stamina.CanCastSpell(3)),
                _ => false
            };

            if (canCast)
            {
                CastCurrentSpell();
            }
        }

        private void CastCurrentSpell()
        {
            if (_stamina != null && !_stamina.TryConsumeSpell(_currentSpell))
                return;

            _animator.SetTrigger("CastSpell");
            _isCasting = true;
            _castingTime = 0f;

            switch (_currentSpell)
            {
                case 1: _spell01CooldownRemaining = spell01Cooldown; break;
                case 2: _spell02CooldownRemaining = spell02Cooldown; break;
                case 3: _spell03CooldownRemaining = spell03Cooldown; break;
            }

            OnSpawnSpellProjectile();
            Invoke(nameof(OnSpellCastComplete), 0.3f);
        }

        public void OnSpellCastComplete()
        {
            _isCasting = false;
            _castingTime = 0f;
        }

        public void OnSpawnSpellProjectile()
        {
            GameObject projectilePrefab = _currentSpell switch
            {
                1 => spell01Projectile,
                2 => spell02Projectile,
                3 => spell03Projectile,
                _ => null
            };

            if (projectilePrefab == null) return;

            // Tính hướng bắn
            Vector2 aimDirection = Vector2.right;
            if (_controller != null)
            {
                float aimAngle = _controller.AimAngleDeg;
                aimDirection = new Vector2(
                    Mathf.Cos(aimAngle * Mathf.Deg2Rad),
                    Mathf.Sin(aimAngle * Mathf.Deg2Rad)
                );
            }

            // Spawn offset from player
            float spawnOffset = 0.5f;
            Vector2 spawnPosition = (Vector2)transform.position + (aimDirection * spawnOffset);
            
            var projectile = Instantiate(
                projectilePrefab,
                spawnPosition,
                Quaternion.identity
            );

            // Ignore collision with player
            var projectileCollider = projectile.GetComponent<Collider2D>();
            var playerCollider = GetComponent<Collider2D>();
            if (projectileCollider != null && playerCollider != null)
            {
                Physics2D.IgnoreCollision(projectileCollider, playerCollider);
            }

            var proj = projectile.GetComponent<SpellProjectile>();
            if (proj != null)
            {
                float spellRange = _currentSpell switch
                {
                    1 => spell01Range,
                    2 => spell02Range,
                    3 => spell03Range,
                    _ => 5f
                };
                proj.SetMaxRange(spellRange);
                proj.Fire(aimDirection);
            }
            else
            {
                var proj2D = projectile.GetComponent<Projectile2D>();
                if (proj2D != null)
                {
                    proj2D.Fire(aimDirection);
                }
            }
        }

        public void SaveSpellState()
        {
            _spellBeforeDamage = _currentSpell;
        }

        /// <summary>
        /// Restore spell state sau khi damage
        /// </summary>
        public void RestoreSpellState()
        {
            if (_spellBeforeDamage > 0 && _spellBeforeDamage != _currentSpell)
            {
                _currentSpell = _spellBeforeDamage;
                _animator.SetInteger("SpellType", _spellBeforeDamage);
            }
        }

        public float GetSpellCooldownPercent(int spellNumber)
        {
            return spellNumber switch
            {
                1 => Mathf.Clamp01(_spell01CooldownRemaining / spell01Cooldown),
                2 => Mathf.Clamp01(_spell02CooldownRemaining / spell02Cooldown),
                3 => Mathf.Clamp01(_spell03CooldownRemaining / spell03Cooldown),
                _ => 0f
            };
        }

        /// <summary>
        /// Lấy projectile prefab theo spell number (để UI lấy icon)
        /// </summary>
        public GameObject GetSpellProjectilePrefab(int spellNumber)
        {
            return spellNumber switch
            {
                1 => spell01Projectile,
                2 => spell02Projectile,
                3 => spell03Projectile,
                _ => null
            };
        }

        /// <summary>
        /// Lấy sprite icon cho spell (từ projectile prefab's SpriteRenderer)
        /// </summary>
        public Sprite GetSpellIcon(int spellNumber)
        {
            var prefab = GetSpellProjectilePrefab(spellNumber);
            if (prefab == null) return null;

            var sr = prefab.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null) return sr.sprite;

            // Thử tìm trong children
            sr = prefab.GetComponentInChildren<SpriteRenderer>();
            return sr != null ? sr.sprite : null;
        }
    }
}
