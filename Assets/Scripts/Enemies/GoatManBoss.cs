using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Boss Goat Man:
    /// - Cơ chế nhận damage tương tự RatMiniBoss
    /// - Đứng xa: rage -> charge húc player
    /// - Đứng gần: đánh chùy cận chiến
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class GoatManBoss : MonoBehaviour
    {
        [Header("Boss States")]
        [SerializeField] private bool isAwake = false;
        [SerializeField] private bool isDead = false;

        [Header("Stats")]
        [SerializeField] private int maxHealth = 80;
        [SerializeField] private int currentHealth;

        [Header("Detection")]
        [Tooltip("Khoảng cách để thức giấc")]
        [SerializeField] private float wakeUpDistance = 10f;

        [Tooltip("Khoảng cách đuổi theo")]
        [SerializeField] private float chaseDistance = 14f;

        [Header("Movement")]
        [SerializeField] private float moveAcceleration = 120f;
        [SerializeField] private float maxSpeed = 3.5f;
        [SerializeField] private float linearDamping = 1f;

        [Header("Attack - GoatMan")]
        [SerializeField] private float maceAttackRange = 1.8f;
        [SerializeField] private int maceAttackDamage = 1;
        [SerializeField] private float maceAttackCooldown = 1.5f;
        [SerializeField] private float maceKnockbackForce = 5f;
        [SerializeField] private float maceAttackAnimDuration = 0.4f;

        [Tooltip("Khoảng cách tối thiểu để bắt đầu charge")]
        [SerializeField] private float chargeMinDistance = 3.5f;

        [Tooltip("Khoảng cách tối đa để bắt đầu charge (boss sẽ đuổi đến ngưỡng này rồi lao)")]
        [SerializeField] private float chargeMaxDistance = 10f;

        [Tooltip("Thời gian rage trước khi lao")]
        [SerializeField] private float rageDuration = 0.7f;
        [SerializeField] private float chargeSpeed = 10.5f;
        [SerializeField] private float chargeDuration = 0.55f;
        [SerializeField] private float chargeCooldown = 2.2f;
        [SerializeField] private float chargeHitRange = 1.15f;
        [SerializeField] private int chargeDamage = 2;
        [SerializeField] private float chargeKnockbackForce = 8f;

        [Header("Audio")]
        [SerializeField] private AudioClip wakeUpSound;
        [SerializeField] private AudioClip rageSound;
        [SerializeField] private AudioClip chargeSound;
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip hurtSound;
        [SerializeField] private AudioClip deathSound;

        [Header("Visual")]
        [SerializeField] private Color sleepingTint = new Color(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private ParticleSystem wakeUpEffect;
        [SerializeField] private ParticleSystem rageEffect;
        [SerializeField] private ParticleSystem attackEffect;

        [Header("Health Bar")]
        [SerializeField] private GameObject healthBarPrefab;

        [Header("Loot")]
        [SerializeField] private GameObject[] lootDrops;
        [SerializeField] private int minDrops = 1;
        [SerializeField] private int maxDrops = 3;

        private Rigidbody2D rb;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Enable if the sprite originally faces LEFT instead of right")]
        [SerializeField] private bool facingLeftByDefault = false;

        private AudioSource audioSource;
        private UI.Enemy2DHealthBarController _healthBarController;

        private PlayerController2D player;
        private PlayerHealth2D playerHealth;

        private float lastAttackTime;
        private Color originalColor;
        private Vector2 startPosition;
        private Coroutine _flashCoroutine;

        // Combat flags (theo style RatMiniBoss)
        private bool _canAttack = true;
        private bool _isAttacking = false;
        private bool _canCharge = true;
        private bool _isRaging = false;
        private bool _isCharging = false;

        private int _lastSpellHitFrame = -1;

        private static readonly int HashIsSleeping = Animator.StringToHash("isSleeping");
        private static readonly int HashIsAwakeAnim = Animator.StringToHash("isAwake");
        private static readonly int HashIsMoving = Animator.StringToHash("isMoving");
        private static readonly int HashWakeUp = Animator.StringToHash("wakeUp");
        private static readonly int HashIsAttacking = Animator.StringToHash("isAttacking");
        private static readonly int HashIsRaging = Animator.StringToHash("isRaging");
        private static readonly int HashIsCharging = Animator.StringToHash("isCharging");
        private static readonly int HashCharge = Animator.StringToHash("charge");
        private static readonly int HashHurt = Animator.StringToHash("hurt");
        private static readonly int HashDeath = Animator.StringToHash("Death");
        private static readonly int HashIsDead = Animator.StringToHash("isDead");

        private System.Collections.Generic.HashSet<int> _validBoolParams;
        private System.Collections.Generic.HashSet<int> _validTriggerParams;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Cấu hình physics cho chuyển động tự nhiên (giống RatMiniBoss)
            rb.linearDamping = linearDamping;

            if (!isAwake)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            currentHealth = maxHealth;
            originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            startPosition = transform.position;

            _validBoolParams = new System.Collections.Generic.HashSet<int>();
            _validTriggerParams = new System.Collections.Generic.HashSet<int>();
            if (animator != null)
            {
                foreach (var param in animator.parameters)
                {
                    int hash = Animator.StringToHash(param.name);
                    if (param.type == AnimatorControllerParameterType.Bool)
                        _validBoolParams.Add(hash);
                    else if (param.type == AnimatorControllerParameterType.Trigger)
                        _validTriggerParams.Add(hash);
                }
            }

            if (healthBarPrefab != null)
            {
                var healthBarObj = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
                _healthBarController = healthBarObj.GetComponent<UI.Enemy2DHealthBarController>();
                if (_healthBarController != null)
                    _healthBarController.SetTarget(this);
            }
        }

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController2D>();
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth2D>();
            }
            else
            {
                Debug.LogError("[GoatManBoss] KHÔNG TÌM THẤY PLAYER! Kiểm tra PlayerController2D component.");
            }

            if (!isAwake)
            {
                if (spriteRenderer != null)
                    spriteRenderer.color = sleepingTint;

                SafeSetBool(HashIsSleeping, true);
                SafeSetBool(HashIsAwakeAnim, false);
                SafeSetBool(HashIsMoving, false);
            }
        }

        private void Update()
        {
            if (isDead || player == null) return;

            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
            if (!isAwake && distanceToPlayer <= wakeUpDistance)
                WakeUp();
        }

        private void FixedUpdate()
        {
            if (isDead) return;

            rb.linearDamping = isAwake ? 0.5f : linearDamping;

            if (!isAwake)
            {
                SafeSetBool(HashIsMoving, false);
                return;
            }

            if (player == null)
            {
                SafeSetBool(HashIsMoving, false);
                return;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

            // Cận chiến khi gần
            if (distanceToPlayer <= maceAttackRange
                && _canAttack && !_isAttacking && !_isRaging && !_isCharging)
            {
                StartCoroutine(PerformMaceAttack());
                return;
            }

            // Xa trong tầm cho phép thì rage -> charge
            if (distanceToPlayer > maceAttackRange
                && distanceToPlayer >= chargeMinDistance
                && distanceToPlayer <= chargeMaxDistance
                && _canCharge && !_isAttacking && !_isRaging && !_isCharging)
            {
                StartCoroutine(PerformChargeAttack());
                return;
            }

            // Đứng yên khi đang tấn công cận chiến / đang rage
            if (_isAttacking || _isRaging)
            {
                SafeSetBool(HashIsMoving, false);
                rb.linearVelocity = Vector2.zero;
                FacePlayer();
                return;
            }

            // Đang charge thì để coroutine điều khiển velocity
            if (_isCharging)
            {
                SafeSetBool(HashIsMoving, false);
                return;
            }

            if (distanceToPlayer <= chaseDistance)
            {
                ChasePlayer(distanceToPlayer);
            }
            else
            {
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 5f);
                SafeSetBool(HashIsMoving, false);
            }
        }

        private void WakeUp()
        {
            if (isAwake) return;

            isAwake = true;
            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;

            rb.bodyType = RigidbodyType2D.Dynamic;

            SafeSetBool(HashIsSleeping, false);
            SafeSetBool(HashIsAwakeAnim, true);
            SafeSetBool(HashIsMoving, false);
            SafeSetTrigger(HashWakeUp);

            PlaySound(wakeUpSound);
            if (wakeUpEffect != null)
                wakeUpEffect.Play();
        }

        private void ChasePlayer(float distance)
        {
            if (distance > 0.5f)
            {
                Vector2 direction = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
                rb.AddForce(direction * moveAcceleration, ForceMode2D.Force);
                if (rb.linearVelocity.magnitude > maxSpeed)
                    rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }

            bool isMoving = rb.linearVelocity.magnitude > 0.1f;
            SafeSetBool(HashIsMoving, isMoving);

            if (spriteRenderer != null)
            {
                float vx = rb.linearVelocity.x;
                if (Mathf.Abs(vx) > 0.05f)
                {
                    bool faceLeft = vx < 0f;
                    spriteRenderer.flipX = facingLeftByDefault ? !faceLeft : faceLeft;
                }
                else if (player != null)
                {
                    bool faceLeft = (player.transform.position.x - transform.position.x) < 0f;
                    spriteRenderer.flipX = facingLeftByDefault ? !faceLeft : faceLeft;
                }
            }
        }

        private System.Collections.IEnumerator PerformMaceAttack()
        {
            if (isDead || player == null || playerHealth == null) yield break;

            _isAttacking = true;
            _canAttack = false;

            SafeSetBool(HashIsMoving, false);
            SafeSetBool(HashIsAttacking, true);
            FacePlayer();
            PlaySound(attackSound);

            yield return new WaitForSeconds(maceAttackAnimDuration * 0.5f);

            if (!isDead && player != null && playerHealth != null)
            {
                float distance = Vector2.Distance(transform.position, player.transform.position);
                if (distance <= maceAttackRange * 1.2f)
                {
                    Vector2 knockDir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
                    Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                    playerHealth.TakeDamage(maceAttackDamage, knockDir * maceKnockbackForce, playerRb);
                    if (attackEffect != null)
                        attackEffect.Play();
                }
            }

            yield return new WaitForSeconds(maceAttackAnimDuration * 0.5f);

            SafeSetBool(HashIsAttacking, false);
            _isAttacking = false;

            yield return new WaitForSeconds(maceAttackCooldown);
            _canAttack = true;
        }

        private System.Collections.IEnumerator PerformChargeAttack()
        {
            if (isDead || player == null || playerHealth == null) yield break;

            _isRaging = true;
            _canCharge = false;
            rb.linearVelocity = Vector2.zero;
            FacePlayer();

            SafeSetBool(HashIsMoving, false);
            SafeSetBool(HashIsRaging, true);
            PlaySound(rageSound);
            if (rageEffect != null)
                rageEffect.Play();

            yield return new WaitForSeconds(rageDuration);

            if (isDead || player == null || playerHealth == null)
            {
                _isRaging = false;
                SafeSetBool(HashIsRaging, false);
                yield return new WaitForSeconds(chargeCooldown);
                _canCharge = true;
                yield break;
            }

            _isRaging = false;
            _isCharging = true;
            SafeSetBool(HashIsRaging, false);
            SafeSetBool(HashIsMoving, false);
            SafeSetBool(HashIsCharging, true);
            SafeSetTrigger(HashCharge);
            PlaySound(chargeSound);

            Vector2 chargeDirection = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            float elapsed = 0f;
            bool hasHitPlayer = false;

            while (elapsed < chargeDuration)
            {
                if (isDead) break;

                elapsed += Time.deltaTime;
                rb.linearVelocity = chargeDirection * chargeSpeed;

                if (!hasHitPlayer && player != null && playerHealth != null)
                {
                    float distance = Vector2.Distance(transform.position, player.transform.position);
                    if (distance <= chargeHitRange)
                    {
                        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                        playerHealth.TakeDamage(chargeDamage, chargeDirection * chargeKnockbackForce, playerRb);
                        if (attackEffect != null)
                            attackEffect.Play();
                        hasHitPlayer = true;
                    }
                }

                yield return null;
            }

            rb.linearVelocity = Vector2.zero;
            _isCharging = false;
            SafeSetBool(HashIsCharging, false);

            yield return new WaitForSeconds(chargeCooldown);
            _canCharge = true;
        }

        public int GetCurrentHealth() => currentHealth;
        public int GetMaxHealth() => maxHealth;

        public void TakeDamage(int damage, Vector2 hitDirection, float knockbackPower)
        {
            if (isDead) return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            _healthBarController?.OnHealthChanged(currentHealth, maxHealth);

            rb.AddForce(hitDirection.normalized * knockbackPower, ForceMode2D.Impulse);

            if (spriteRenderer != null)
            {
                if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
                spriteRenderer.color = originalColor;
                _flashCoroutine = StartCoroutine(FlashRed());
            }

            PlaySound(hurtSound);
            SafeSetTrigger(HashHurt);

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                if (!isAwake)
                    WakeUp();
            }
        }

        private void Die()
        {
            if (isDead) return;

            isDead = true;
            _healthBarController?.OnEnemyDied();

            SafeSetTrigger(HashDeath);
            SafeSetBool(HashIsDead, true);

            PlaySound(deathSound);

            rb.simulated = false;
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.enabled = false;

            BossManager bossManager = FindFirstObjectByType<BossManager>();
            if (bossManager != null)
                bossManager.OnBossDefeated();

            DropLoot();
            Destroy(gameObject, 2f);
        }

        private System.Collections.IEnumerator FlashRed()
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            spriteRenderer.color = originalColor;
            _flashCoroutine = null;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDead) return;
            if (other.TryGetComponent<SpellProjectile>(out var spell))
                HandleSpellHit(spell, other.transform.position);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isDead) return;

            if (collision.collider.TryGetComponent<SpellProjectile>(out var spell))
            {
                HandleSpellHit(spell, collision.collider.transform.position);
            }
        }

        private void HandleSpellHit(SpellProjectile spell, Vector3 spellPosition)
        {
            if (Time.frameCount == _lastSpellHitFrame) return;
            _lastSpellHitFrame = Time.frameCount;

            int spellDamage = 10;
            float spellKnockback = 3f;

            var damageField = spell.GetType().GetField("damage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (damageField != null) spellDamage = (int)damageField.GetValue(spell);

            var knockbackField = spell.GetType().GetField("knockbackForce",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (knockbackField != null) spellKnockback = (float)knockbackField.GetValue(spell);

            Vector2 hitDir = ((Vector2)transform.position - (Vector2)spellPosition).normalized;
            TakeDamage(spellDamage, hitDir, spellKnockback);
        }

        private void SafeSetBool(int paramHash, bool value)
        {
            if (animator == null || _validBoolParams == null) return;
            if (_validBoolParams.Contains(paramHash))
                animator.SetBool(paramHash, value);
        }

        private void SafeSetTrigger(int paramHash)
        {
            if (animator == null || _validTriggerParams == null) return;
            if (_validTriggerParams.Contains(paramHash))
                animator.SetTrigger(paramHash);
        }

        private void FacePlayer()
        {
            if (spriteRenderer == null || player == null)
                return;

            bool faceLeft = (player.transform.position.x - transform.position.x) < 0f;
            spriteRenderer.flipX = facingLeftByDefault ? !faceLeft : faceLeft;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip);
        }

        private void DropLoot()
        {
            if (lootDrops == null || lootDrops.Length == 0) return;

            int dropCount = Random.Range(minDrops, maxDrops + 1);
            for (int i = 0; i < dropCount; i++)
            {
                GameObject lootPrefab = lootDrops[Random.Range(0, lootDrops.Length)];
                if (lootPrefab == null) continue;

                Vector2 randomOffset = Random.insideUnitCircle * 2f;
                Vector3 dropPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

                GameObject loot = Instantiate(lootPrefab, dropPosition, Quaternion.identity);
                Rigidbody2D lootRb = loot.GetComponent<Rigidbody2D>();
                if (lootRb != null)
                {
                    Vector2 force = randomOffset.normalized * Random.Range(2f, 5f);
                    lootRb.AddForce(force, ForceMode2D.Impulse);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, wakeUpDistance);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, chaseDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, maceAttackRange);

            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, chargeMaxDistance);

            Gizmos.color = new Color(1f, 0.3f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, chargeMinDistance);

            #if UNITY_EDITOR
            string status = isDead ? "DEAD" : (isAwake ? "AWAKE" : "SLEEPING");
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"GOAT MAN BOSS\n{status}\nHP: {currentHealth}/{maxHealth}");
            #endif
        }

        private void OnDestroy()
        {
            if (_healthBarController != null)
                Destroy(_healthBarController.gameObject);
        }
    }
}