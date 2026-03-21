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

        // Cached animator hashes
        private static readonly int HashSpellType = Animator.StringToHash("SpellType");
        private static readonly int HashCastSpell = Animator.StringToHash("CastSpell");

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
            _animator.SetInteger(HashSpellType, 0);
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
            // Không xử lý input khi game đang paused
            if (NWO.PauseMenuUI.GameIsPaused) return;

            var kb = KeyBindManager.Instance;
            if (kb != null)
            {
                if (kb.WasPressedThisFrame(KeyBindManager.ACT_SPELL0))
                    SwitchToSpell(0);
                else if (kb.WasPressedThisFrame(KeyBindManager.ACT_SPELL1))
                    SwitchToSpell(1);
                else if (kb.WasPressedThisFrame(KeyBindManager.ACT_SPELL2))
                    SwitchToSpell(2);
                else if (kb.WasPressedThisFrame(KeyBindManager.ACT_SPELL3))
                    SwitchToSpell(3);
                return;
            }

            // Fallback
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.digit0Key.wasPressedThisFrame)
                SwitchToSpell(0);
            else if (keyboard.digit1Key.wasPressedThisFrame)
                SwitchToSpell(1);
            else if (keyboard.digit2Key.wasPressedThisFrame)
                SwitchToSpell(2);
            else if (keyboard.digit3Key.wasPressedThisFrame)
                SwitchToSpell(3);
        }

        private void SwitchToSpell(int spellNumber)
        {
            if (_currentSpell == spellNumber) return;

            _currentSpell = spellNumber;
            _spellBeforeDamage = spellNumber;
            _animator.SetInteger(HashSpellType, spellNumber);

            if (_isCasting)
                _isCasting = false;
        }

    private void HandleSpellCast()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            bool castInput = mouse.leftButton.wasPressedThisFrame;

            // Also check keybind for Attack/Cast
            var kb = KeyBindManager.Instance;
            if (kb != null)
                castInput = castInput || kb.WasPressedThisFrame(KeyBindManager.ACT_ATTACK);
            else if (Keyboard.current != null)
                castInput = castInput || Keyboard.current.qKey.wasPressedThisFrame;

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

            _animator.SetTrigger(HashCastSpell);
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
                _animator.SetInteger(HashSpellType, _spellBeforeDamage);
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

        // === Upgrade System Modifiers ===

        /// <summary>Tăng damage tất cả spell</summary>
        public void AddAllSpellDamage(int amount)
        {
            spell01Damage += amount;
            spell02Damage += amount;
            spell03Damage += amount;
        }

        /// <summary>Tăng range tất cả spell</summary>
        public void AddAllSpellRange(float amount)
        {
            spell01Range += amount;
            spell02Range += amount;
            spell03Range += amount;
        }

        /// <summary>Giảm cooldown tất cả spell (giá trị dương = giảm cooldown)</summary>
        public void AddAllSpellCooldownReduction(float amount)
        {
            spell01Cooldown = Mathf.Max(0.3f, spell01Cooldown - amount);
            spell02Cooldown = Mathf.Max(0.5f, spell02Cooldown - amount);
            spell03Cooldown = Mathf.Max(0.8f, spell03Cooldown - amount);
        }

        /// <summary>Tăng damage spell cụ thể</summary>
        public void AddSpellDamage(int spellNumber, int amount)
        {
            switch (spellNumber)
            {
                case 1: spell01Damage += amount; break;
                case 2: spell02Damage += amount; break;
                case 3: spell03Damage += amount; break;
            }
        }

        /// <summary>Giảm cooldown spell cụ thể</summary>
        public void AddSpellCooldownReduction(int spellNumber, float amount)
        {
            switch (spellNumber)
            {
                case 1: spell01Cooldown = Mathf.Max(0.3f, spell01Cooldown - amount); break;
                case 2: spell02Cooldown = Mathf.Max(0.5f, spell02Cooldown - amount); break;
                case 3: spell03Cooldown = Mathf.Max(0.8f, spell03Cooldown - amount); break;
            }
        }
    }
}
