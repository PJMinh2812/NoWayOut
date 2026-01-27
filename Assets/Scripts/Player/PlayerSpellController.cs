using UnityEngine;
using UnityEngine.InputSystem;

namespace GloomCraft
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

        [Header("Spell Range")]
        [SerializeField] private float spell01Range = 5f;
        [SerializeField] private float spell02Range = 7f;
        [SerializeField] private float spell03Range = 10f;

        [Header("Spell Prefabs (Optional)")]
        [SerializeField] private GameObject spell01Projectile;
        [SerializeField] private GameObject spell02Projectile;
        [SerializeField] private GameObject spell03Projectile;

        private Animator _animator;
        private PlayerController2D _controller;

        private float _spell01CooldownRemaining;
        private float _spell02CooldownRemaining;
        private float _spell03CooldownRemaining;

        private int _currentSpell = 1; // Default spell 1
        private bool _isCasting;

        public int CurrentSpell => _currentSpell;
        public bool IsCasting => _isCasting;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _controller = GetComponent<PlayerController2D>();
        }

        private void Update()
        {
            // Cooldown timers
            if (_spell01CooldownRemaining > 0f) _spell01CooldownRemaining -= Time.deltaTime;
            if (_spell02CooldownRemaining > 0f) _spell02CooldownRemaining -= Time.deltaTime;
            if (_spell03CooldownRemaining > 0f) _spell03CooldownRemaining -= Time.deltaTime;

            // Không cast khi đang dash hoặc đang cast
            if (_controller != null && (_controller.IsRolling || _isCasting))
                return;

            // Switch spell type
            HandleSpellSwitch();

            // Cast spell
            HandleSpellCast();
        }

        private void HandleSpellSwitch()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.digit1Key.wasPressedThisFrame)
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
            _animator.SetInteger("SpellType", spellNumber);

            Debug.Log($"[Spell] Switched to Spell {spellNumber}");
        }

        private void HandleSpellCast()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // Cast spell với Left Click hoặc Q key
            bool castInput = mouse.leftButton.wasPressedThisFrame
                          || Keyboard.current.qKey.wasPressedThisFrame;

            if (!castInput) return;

            // Check cooldown
            bool canCast = _currentSpell switch
            {
                1 => _spell01CooldownRemaining <= 0f,
                2 => _spell02CooldownRemaining <= 0f,
                3 => _spell03CooldownRemaining <= 0f,
                _ => false
            };

            if (canCast)
            {
                CastCurrentSpell();
            }
            else
            {
                Debug.Log($"[Spell] Spell {_currentSpell} on cooldown!");
            }
        }

        private void CastCurrentSpell()
        {
            // Trigger animation
            _animator.SetTrigger("CastSpell");
            _isCasting = true;

            // Set cooldown
            switch (_currentSpell)
            {
                case 1:
                    _spell01CooldownRemaining = spell01Cooldown;
                    Debug.Log($"[Spell] Cast Spell 01 - Damage: {spell01Damage}");
                    break;
                case 2:
                    _spell02CooldownRemaining = spell02Cooldown;
                    Debug.Log($"[Spell] Cast Spell 02 - Damage: {spell02Damage}");
                    break;
                case 3:
                    _spell03CooldownRemaining = spell03Cooldown;
                    Debug.Log($"[Spell] Cast Spell 03 - Damage: {spell03Damage}");
                    break;
            }

            // Spawn projectile sẽ được gọi từ Animation Event
        }

        /// <summary>
        /// Được gọi từ Animation Event khi spell animation kết thúc
        /// </summary>
        public void OnSpellCastComplete()
        {
            _isCasting = false;
            Debug.Log("[Spell] Cast complete!");
        }

        /// <summary>
        /// Được gọi từ Animation Event tại frame spawn projectile
        /// </summary>
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

            // Get aim direction
            Vector2 aimDirection = Vector2.right; // Default
            if (_controller != null)
            {
                float aimAngle = _controller.AimAngleDeg;
                aimDirection = new Vector2(
                    Mathf.Cos(aimAngle * Mathf.Deg2Rad),
                    Mathf.Sin(aimAngle * Mathf.Deg2Rad)
                );
            }

            // Spawn projectile
            var projectile = Instantiate(
                projectilePrefab,
                transform.position,
                Quaternion.identity
            );

            // Set projectile direction
            var proj = projectile.GetComponent<Projectile2D>();
            if (proj != null)
            {
                proj.Fire(aimDirection);
            }

            Debug.Log($"[Spell] Spawned projectile for Spell {_currentSpell}");
        }

        // UI Helper - Get cooldown percentage
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
    }
}