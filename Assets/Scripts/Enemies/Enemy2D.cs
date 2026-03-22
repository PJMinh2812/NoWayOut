using UnityEngine;
using System.Collections;

namespace NWO
{
    /// <summary>

    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Enemy2D : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private int maxHealth = 20;
        [SerializeField] private Vector2Int damageRange = new(4, 8);
        [SerializeField] private float knockbackStrength = 4f;
        [SerializeField] private float moveAcceleration = 18f;
        [SerializeField] private float maxMoveSpeed = 3.5f;
        [SerializeField] private float friction = 8f;

        [Header("AI")]
        [SerializeField] private float aggroRadius = 8f;
        
        [Header("UI (Optional)")]
        [SerializeField] private GameObject healthBarPrefab;

        [Header("Audio")]
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip hurtSound;
        [SerializeField] private AudioClip deathSound;

        private int _currentHealth;
        private Rigidbody2D _rb;
        private AudioSource _audioSource;
        private PlayerController2D _player;
        private PlayerHealth2D _playerHealth;
        private UI.Enemy2DHealthBarController _healthBarController;
        private BossManager _bossManager;

        // Cached WaitForSeconds to avoid GC allocation each coroutine call
        private WaitForSeconds _waitAttackHalf;
        private WaitForSeconds _waitAttackCooldown;
        private WaitForSeconds _waitDeathAnim;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [Tooltip("Seconds to show attack animation before resetting isAttacking")]
        [SerializeField] private float attackAnimDuration = 0.25f;
        [Tooltip("Delay before destroying enemy to allow death animation to play. Increase if animation is cut off.")]
        [SerializeField] private float deathAnimDuration = 2f;

        // Sprite flip for facing direction
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Enable if the sprite originally faces LEFT instead of right")]
        [SerializeField] private bool facingLeftByDefault = false;

        [Header("Combat")]
        [Tooltip("Distance within which enemy will attempt to attack")]
        [SerializeField] private float attackRange = 1f;
        [Tooltip("Seconds between consecutive attacks")]
        [SerializeField] private float attackCooldown = 0.8f;

        private bool _canAttack = true;
        private bool _isAttacking = false;
        private bool _isDead = false;

        // Cached animator hashes
        private static readonly int HashIsMoving = Animator.StringToHash("isMoving");
        private static readonly int HashIsAttacking = Animator.StringToHash("isAttacking");
        private static readonly int HashDeath = Animator.StringToHash("Death");
        private static readonly int HashIsDead = Animator.StringToHash("isDead");

        public int GetCurrentHealth() => _currentHealth;
        public int GetMaxHealth() => maxHealth;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _player = FindFirstObjectByType<PlayerController2D>();
            
            if (_player != null)
            {
                _playerHealth = _player.GetComponent<PlayerHealth2D>();
            }
            
            _currentHealth = maxHealth;
            
            // Cache WaitForSeconds (reusable, zero GC per coroutine)
            _waitAttackHalf = new WaitForSeconds(attackAnimDuration * 0.5f);
            _waitAttackCooldown = new WaitForSeconds(attackCooldown);
            _waitDeathAnim = new WaitForSeconds(deathAnimDuration);

            // Cache BossManager once instead of FindFirstObjectByType in Die()
            _bossManager = FindFirstObjectByType<BossManager>();
            
            // Spawn health bar if prefab assigned
            if (healthBarPrefab != null)
            {
                var healthBarObj = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
                _healthBarController = healthBarObj.GetComponent<UI.Enemy2DHealthBarController>();
                if (_healthBarController != null)
                {
                    _healthBarController.SetTarget(this);
                }
            }

            // Auto-assign animator if not set in inspector
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            // Auto-assign sprite renderer if not set in inspector
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void FixedUpdate()
        {
            if (_isDead) return;
            
            _rb.linearDamping = friction;

            if (_player == null)
            {
                if (animator != null) animator.SetBool(HashIsMoving, false);
                return;
            }

            var toPlayer = (Vector2)(_player.transform.position - transform.position);
            var dist = toPlayer.magnitude;
            
            if (dist > aggroRadius)
            {
                if (animator != null) animator.SetBool(HashIsMoving, false);
                return;
            }

            // Start attack if within range and ready
            if (_player != null && _playerHealth != null && dist <= attackRange && _canAttack && !_isAttacking)
            {
                StartCoroutine(PerformAttack());
            }

            // Don't move during attack - stop and play attack animation
            if (_isAttacking)
            {
                if (animator != null) animator.SetBool(HashIsMoving, false);
                _rb.linearVelocity = Vector2.zero;
                
                // Flip sprite to face player during attack
                if (spriteRenderer != null)
                {
                    bool faceLeft = (_player.transform.position.x - transform.position.x) < 0f;
                    spriteRenderer.flipX = facingLeftByDefault ? !faceLeft : faceLeft;
                }
                return;
            }

            // Move towards player (even if in attack range but on cooldown)
            // Stop only when very close to avoid jittering
            if (dist > 0.5f)
            {
                var dir = toPlayer / dist;
                _rb.AddForce(dir * moveAcceleration, ForceMode2D.Force);

                var v = _rb.linearVelocity;
                if (v.magnitude > maxMoveSpeed)
                {
                    _rb.linearVelocity = v.normalized * maxMoveSpeed;
                }
            }

            // Update animator isMoving param
            if (animator != null)
            {
                bool isMoving = _rb.linearVelocity.magnitude > 0.1f;
                animator.SetBool(HashIsMoving, isMoving);
            }

            // Flip sprite to face movement/player
            if (spriteRenderer != null)
            {
                float vx = _rb.linearVelocity.x;
                if (Mathf.Abs(vx) > 0.05f)
                {
                    bool faceLeft = vx < 0f;
                    spriteRenderer.flipX = facingLeftByDefault ? !faceLeft : faceLeft;
                }
                else if (_player != null)
                {
                    bool faceLeft = (_player.transform.position.x - transform.position.x) < 0f;
                    spriteRenderer.flipX = facingLeftByDefault ? !faceLeft : faceLeft;
                }
            }
        }

        public void TakeDamage(int amount, Vector2 hitDirection, float knockbackPower)
        {
            if (amount <= 0) return;
            _currentHealth = Mathf.Max(0, _currentHealth - amount);

            _rb.AddForce(hitDirection.normalized * knockbackPower, ForceMode2D.Impulse);

            // Notify health bar immediately on damage (don't wait for next LateUpdate poll)
            _healthBarController?.OnHealthChanged(_currentHealth, maxHealth);

            // Show floating damage number
            UI.DamagePopup.Spawn(transform.position, amount);

            if (_currentHealth > 0)
            {
                PlaySound(hurtSound);
            }
            else
            {
                Die();
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            // Drop coin vật lý khi chết
            if (CoinManager.Instance != null)
                CoinManager.Instance.SpawnCoinsFromEnemy(transform.position);

            // Hide health bar immediately when enemy dies
            _healthBarController?.OnEnemyDied();

            // Play death animation - use Trigger to avoid re-triggering from Any State
            if (animator != null)
            {
                animator.SetTrigger(HashDeath);
                animator.SetBool(HashIsDead, true);
            }

            PlaySound(deathSound);

            // disable physics and collisions
            var cols = GetComponents<Collider2D>();
            foreach (var c in cols) c.enabled = false;
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Kinematic;

            // Check if this is a boss and notify BossManager (cached in Awake)
            if (_bossManager != null)
            {
                _bossManager.OnBossDefeated();
            }

            // Delay destroy so death animation can play
            StartCoroutine(DelayedDestroy(deathAnimDuration));
        }

        private IEnumerator DelayedDestroy(float delay)
        {
            yield return _waitDeathAnim;
            Destroy(gameObject);
        }

        /// <summary>
        /// Perform complete attack sequence: animation -> deal damage -> cooldown
        /// </summary>
        private IEnumerator PerformAttack()
        {
            if (_isDead || _player == null || _playerHealth == null) yield break;

            _isAttacking = true;
            _canAttack = false;

            // Start attack animation
            if (animator != null)
            {
                animator.SetBool(HashIsAttacking, true);
            }

            PlaySound(attackSound);

            // Wait for attack animation to reach hit frame (deal damage halfway through animation)
            yield return _waitAttackHalf;

            // Deal damage only after animation has progressed
            if (_player != null && _playerHealth != null)
            {
                float dist = Vector2.Distance(transform.position, _player.transform.position);
                if (dist <= attackRange * 1.2f)
                {
                    var dmg = Random.Range(damageRange.x, damageRange.y + 1);
                    var dir = (Vector2)(_player.transform.position - transform.position);
                    var knock = dir.normalized * knockbackStrength;
                    var rb = _player.GetComponent<Rigidbody2D>();

                    _playerHealth.TakeDamage(dmg, knock, rb);
                }
            }

            // Wait for rest of attack animation
            yield return _waitAttackHalf;

            // Reset attack animation
            if (animator != null)
            {
                animator.SetBool(HashIsAttacking, false);
            }
            _isAttacking = false;

            // Start cooldown before next attack
            yield return _waitAttackCooldown;
            _canAttack = true;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
                _audioSource.PlayOneShot(clip);
        }

        private void OnDestroy()
        {
            // Clean up health bar if enemy destroyed externally
            if (_healthBarController != null)
            {
                Destroy(_healthBarController.gameObject);
            }
        }
    }
}


