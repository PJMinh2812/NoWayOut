using UnityEngine;
using System.Collections;

namespace NWO
{
    /// <summary>
    /// Báº£n C# rÃºt gá»n tá»« global.Enemy: di chuyá»ƒn Ä‘uá»•i theo Player vÃ  gÃ¢y damage + knockback khi va cháº¡m.
    /// Pathfinding chi tiáº¿t sáº½ port sau; táº¡m thá»i lÃ  chase trá»±c tiáº¿p.
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

        private int _currentHealth;
        private Rigidbody2D _rb;
        private PlayerController2D _player;
        private PlayerHealth2D _playerHealth;
        private UI.EnemyHealthBarController _healthBarController;

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

        public int GetCurrentHealth() => _currentHealth;
        public int GetMaxHealth() => maxHealth;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _player = FindFirstObjectByType<PlayerController2D>();
            
            if (_player != null)
            {
                _playerHealth = _player.GetComponent<PlayerHealth2D>();
            }
            
            _currentHealth = maxHealth;
            
            // Spawn health bar if prefab assigned
            if (healthBarPrefab != null)
            {
                var healthBarObj = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
                _healthBarController = healthBarObj.GetComponent<UI.EnemyHealthBarController>();
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
                if (animator != null) animator.SetBool("isMoving", false);
                return;
            }

            var toPlayer = (Vector2)(_player.transform.position - transform.position);
            var dist = toPlayer.magnitude;
            
            if (dist > aggroRadius)
            {
                if (animator != null) animator.SetBool("isMoving", false);
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
                if (animator != null) animator.SetBool("isMoving", false);
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
                animator.SetBool("isMoving", isMoving);
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

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            // Hide health bar immediately when enemy dies
            _healthBarController?.OnEnemyDied();

            // Play death animation - use Trigger to avoid re-triggering from Any State
            if (animator != null)
            {
                animator.SetTrigger("Death"); // Use Trigger instead of Bool
                animator.SetBool("isDead", true); // Optional: can be used for conditions
            }

            // disable physics and collisions
            var cols = GetComponents<Collider2D>();
            foreach (var c in cols) c.enabled = false;
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Kinematic;

            // Check if this is a boss and notify BossManager
            BossManager bossManager = FindFirstObjectByType<BossManager>();
            if (bossManager != null)
            {
                bossManager.OnBossDefeated();
            }

            // Delay destroy so death animation can play
            StartCoroutine(DelayedDestroy(deathAnimDuration));
        }

        private IEnumerator DelayedDestroy(float delay)
        {
            yield return new WaitForSeconds(delay);
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
                animator.SetBool("isAttacking", true);
            }

            // Wait for attack animation to reach hit frame (deal damage halfway through animation)
            yield return new WaitForSeconds(attackAnimDuration * 0.5f);

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
            yield return new WaitForSeconds(attackAnimDuration * 0.5f);

            // Reset attack animation
            if (animator != null)
            {
                animator.SetBool("isAttacking", false);
            }
            _isAttacking = false;

            // Start cooldown before next attack
            yield return new WaitForSeconds(attackCooldown);
            _canAttack = true;
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


