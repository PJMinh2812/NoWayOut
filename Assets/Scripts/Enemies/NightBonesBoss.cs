using UnityEngine;

namespace NWO
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class NightBonesBoss : MonoBehaviour
    {
        [Header("Boss States")]
        [SerializeField] private bool isAwake = false;
        [SerializeField] private bool isDead = false;

        [Header("Stats")]
        [SerializeField] private int maxHealth = 65;
        [SerializeField] private int currentHealth;

        [Header("Detection")]
        [SerializeField] private float wakeUpDistance = 9f;
        [SerializeField] private float chaseDistance = 12f;

        [Header("Movement")]
        [SerializeField] private float moveAcceleration = 120f;
        [SerializeField] private float maxSpeed = 3.5f;
        [SerializeField] private float linearDamping = 1f;

        [Header("Attack")]
        [SerializeField] private float attackRange = 1.6f;
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private float attackCooldown = 1.4f;
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private float attackAnimDuration = 0.4f;

        [Header("Ranged Attack - NightBones")]
        [SerializeField] private float shootRange = 10f;
        [SerializeField] private float shootCooldown = 2.2f;
        [SerializeField] private float shootWindup = 0.2f;
        [SerializeField] private float projectileSpawnOffset = 0.6f;
        [SerializeField] private GameObject poisonProjectilePrefab;
        [SerializeField] private GameObject homingProjectilePrefab;
        [SerializeField] private float poisonProjectileSpeed = 6f;
        [SerializeField] private float homingProjectileSpeed = 5f;
        [SerializeField] private int poisonImpactDamage = 1;
        [SerializeField] private int homingImpactDamage = 2;
        [SerializeField] private int poisonCircleCount = 8;
        [SerializeField] private float poisonTripleSpreadAngle = 25f;

        [Header("Audio")]
        [SerializeField] private AudioClip wakeUpSound;
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip hurtSound;
        [SerializeField] private AudioClip deathSound;

        [Header("Visual")]
        [SerializeField] private Color sleepingTint = new Color(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private ParticleSystem wakeUpEffect;
        [SerializeField] private ParticleSystem attackEffect;

        [Header("Health Bar")]
        [SerializeField] private GameObject healthBarPrefab;

        [Header("Loot")]
        [SerializeField] private GameObject[] lootDrops;
        [SerializeField] private int minDrops = 1;
        [SerializeField] private int maxDrops = 3;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool facingLeftByDefault = false;
        [SerializeField] private float hurtTriggerMinInterval = 0.45f;
        [Tooltip("Tên state hurt trong Animator (ví dụ: nightbornehurt)")]
        [SerializeField] private string hurtStateName = "nightbornehurt";

        private Rigidbody2D rb;
        private AudioSource audioSource;
        private UI.Enemy2DHealthBarController healthBarController;

        private PlayerController2D player;
        private PlayerHealth2D playerHealth;
        private Color originalColor;
        private Coroutine flashCoroutine;

        private bool canAttack = true;
        private bool isAttacking = false;
        private bool canShoot = true;
        private bool isShooting = false;
        private float lastHurtTriggerTime = -999f;
        private int hurtStateHash;
        private int hurtStateFullPathHash;
        private int lastDamageFrame = -1;

        private int lastSpellHitFrame = -1;

        private static readonly int HashIsSleeping = Animator.StringToHash("isSleeping");
        private static readonly int HashIsAwakeAnim = Animator.StringToHash("isAwake");
        private static readonly int HashIsMoving = Animator.StringToHash("isMoving");
        private static readonly int HashWakeUp = Animator.StringToHash("wakeUp");
        private static readonly int HashIsAttacking = Animator.StringToHash("isAttacking");
        private static readonly int HashHurt = Animator.StringToHash("hurt");
        private static readonly int HashDeath = Animator.StringToHash("Death");
        private static readonly int HashIsDead = Animator.StringToHash("isDead");

        private System.Collections.Generic.HashSet<int> validBoolParams;
        private System.Collections.Generic.HashSet<int> validTriggerParams;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            rb.linearDamping = linearDamping;

            if (!isAwake)
                rb.bodyType = RigidbodyType2D.Kinematic;

            currentHealth = maxHealth;
            originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            validBoolParams = new System.Collections.Generic.HashSet<int>();
            validTriggerParams = new System.Collections.Generic.HashSet<int>();
            hurtStateHash = Animator.StringToHash(hurtStateName);
            hurtStateFullPathHash = Animator.StringToHash($"Base Layer.{hurtStateName}");
            if (animator != null)
            {
                foreach (var param in animator.parameters)
                {
                    int hash = Animator.StringToHash(param.name);
                    if (param.type == AnimatorControllerParameterType.Bool)
                        validBoolParams.Add(hash);
                    else if (param.type == AnimatorControllerParameterType.Trigger)
                        validTriggerParams.Add(hash);
                }
            }

            if (healthBarPrefab != null)
            {
                var healthBarObj = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
                healthBarController = healthBarObj.GetComponent<UI.Enemy2DHealthBarController>();
                if (healthBarController != null)
                    healthBarController.SetTarget(this);
            }
        }

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController2D>();
            if (player != null)
                playerHealth = player.GetComponent<PlayerHealth2D>();

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

            if (!isAwake || player == null)
            {
                SafeSetBool(HashIsMoving, false);
                return;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

            if (distanceToPlayer <= attackRange && canAttack && !isAttacking)
            {
                StartCoroutine(PerformAttack());
                return;
            }

            if (distanceToPlayer > attackRange
                && distanceToPlayer <= shootRange
                && canShoot && !isShooting && !isAttacking)
            {
                StartCoroutine(ShootPoisonPattern());
                return;
            }

            if (isAttacking || isShooting)
            {
                SafeSetBool(HashIsMoving, false);
                rb.linearVelocity = Vector2.zero;
                FacePlayer();
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

            SafeSetBool(HashIsMoving, rb.linearVelocity.magnitude > 0.1f);

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

        private System.Collections.IEnumerator PerformAttack()
        {
            if (isDead || player == null || playerHealth == null) yield break;

            isAttacking = true;
            canAttack = false;

            SafeSetBool(HashIsMoving, false);
            SafeSetBool(HashIsAttacking, true);
            FacePlayer();
            PlaySound(attackSound);

            yield return new WaitForSeconds(attackAnimDuration);

            if (!isDead && player != null && playerHealth != null)
            {
                float distance = Vector2.Distance(transform.position, player.transform.position);
                if (distance <= attackRange * 1.2f)
                {
                    Vector2 knockDir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
                    Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                    playerHealth.TakeDamage(attackDamage, knockDir * knockbackForce, playerRb);
                    if (attackEffect != null)
                        attackEffect.Play();
                }
            }

            SafeSetBool(HashIsAttacking, false);
            isAttacking = false;

            yield return new WaitForSeconds(attackCooldown);
            canAttack = true;
        }

        private System.Collections.IEnumerator ShootPoisonPattern()
        {
            if (isDead || player == null) yield break;

            isShooting = true;
            canShoot = false;

            SafeSetBool(HashIsMoving, false);
            rb.linearVelocity = Vector2.zero;
            FacePlayer();

            yield return new WaitForSeconds(shootWindup);

            if (!isDead && player != null)
            {
                int pattern = Random.Range(0, 4); // 0: poison single, 1: poison triple, 2: poison circle, 3: homing
                switch (pattern)
                {
                    case 0:
                        FirePoisonSingle();
                        break;
                    case 1:
                        FirePoisonTriple();
                        break;
                    case 2:
                        FirePoisonCircle();
                        break;
                    case 3:
                        FireHomingSingle();
                        break;
                }

                PlaySound(attackSound);
            }

            isShooting = false;

            yield return new WaitForSeconds(shootCooldown);
            canShoot = true;
        }

        private void FirePoisonSingle()
        {
            SpawnPoisonProjectile(GetDirectionToPlayer());
        }

        private void FirePoisonTriple()
        {
            Vector2 baseDir = GetDirectionToPlayer();
            SpawnPoisonProjectile(Rotate(baseDir, -poisonTripleSpreadAngle));
            SpawnPoisonProjectile(baseDir);
            SpawnPoisonProjectile(Rotate(baseDir, poisonTripleSpreadAngle));
        }

        private void FirePoisonCircle()
        {
            int count = Mathf.Max(1, poisonCircleCount);
            float step = 360f / count;
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Rotate(Vector2.right, step * i);
                SpawnPoisonProjectile(dir);
            }
        }

        private void FireHomingSingle()
        {
            SpawnHomingProjectile(GetDirectionToPlayer());
        }

        private void SpawnPoisonProjectile(Vector2 direction)
        {
            if (poisonProjectilePrefab == null)
                return;

            Vector2 shootDir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            Vector3 spawnPos = transform.position + (Vector3)(shootDir * projectileSpawnOffset);
            GameObject projectile = Instantiate(poisonProjectilePrefab, spawnPos, Quaternion.identity);

            if (projectile.TryGetComponent<NightBonesPoisonProjectile>(out var poisonProjectile))
            {
                poisonProjectile.Fire(shootDir, this, poisonProjectileSpeed, poisonImpactDamage);
            }
            else if (projectile.TryGetComponent<BossFireball>(out var fallbackFireball))
            {
                fallbackFireball.Fire(shootDir, poisonProjectileSpeed, poisonImpactDamage);
            }
        }

        private void SpawnHomingProjectile(Vector2 direction)
        {
            if (homingProjectilePrefab == null)
            {
                SpawnPoisonProjectile(direction);
                return;
            }

            Vector2 shootDir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            Vector3 spawnPos = transform.position + (Vector3)(shootDir * projectileSpawnOffset);
            GameObject projectile = Instantiate(homingProjectilePrefab, spawnPos, Quaternion.identity);

            if (projectile.TryGetComponent<NightBonesHomingProjectile>(out var homingProjectile))
            {
                homingProjectile.Fire(player != null ? player.transform : null, shootDir, this, homingProjectileSpeed, homingImpactDamage);
            }
            else if (projectile.TryGetComponent<BossFireball>(out var fallbackFireball))
            {
                fallbackFireball.Fire(shootDir, homingProjectileSpeed, homingImpactDamage);
            }
        }

        private Vector2 GetDirectionToPlayer()
        {
            if (player == null) return Vector2.right;
            return ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(cos * vector.x - sin * vector.y, sin * vector.x + cos * vector.y);
        }

        public int GetCurrentHealth() => currentHealth;
        public int GetMaxHealth() => maxHealth;

        public void TakeDamage(int damage, Vector2 hitDirection, float knockbackPower)
        {
            if (isDead) return;
            if (Time.frameCount == lastDamageFrame) return;
            lastDamageFrame = Time.frameCount;

            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            healthBarController?.OnHealthChanged(currentHealth, maxHealth);

            rb.AddForce(hitDirection.normalized * knockbackPower, ForceMode2D.Impulse);

            if (spriteRenderer != null)
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                spriteRenderer.color = originalColor;
                flashCoroutine = StartCoroutine(FlashRed());
            }

            PlaySound(hurtSound);
            TryTriggerHurt();

            if (currentHealth <= 0)
            {
                Die();
            }
            else if (!isAwake)
            {
                WakeUp();
            }
        }

        private void Die()
        {
            if (isDead) return;

            isDead = true;

            // Drop heart boss vật lý khi chết
            if (HeartManager.Instance != null)
                HeartManager.Instance.SpawnHeartsFromBoss(transform.position);

            healthBarController?.OnEnemyDied();

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
            if (spriteRenderer == null) yield break;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            spriteRenderer.color = originalColor;
            flashCoroutine = null;
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
                HandleSpellHit(spell, collision.collider.transform.position);
        }

        private void HandleSpellHit(SpellProjectile spell, Vector3 spellPosition)
        {
            if (Time.frameCount == lastSpellHitFrame) return;
            lastSpellHitFrame = Time.frameCount;

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
            if (animator == null || validBoolParams == null) return;
            if (validBoolParams.Contains(paramHash))
                animator.SetBool(paramHash, value);
        }

        private void SafeSetTrigger(int paramHash)
        {
            if (animator == null || validTriggerParams == null) return;
            if (validTriggerParams.Contains(paramHash))
                animator.SetTrigger(paramHash);
        }

        private void TryTriggerHurt()
        {
            if (IsInHurtState())
                return;

            if (Time.time - lastHurtTriggerTime < hurtTriggerMinInterval)
                return;

            lastHurtTriggerTime = Time.time;
            if (animator != null)
                animator.ResetTrigger(HashHurt);
            SafeSetTrigger(HashHurt);
        }

        private bool IsInHurtState()
        {
            if (animator == null)
                return false;

            var current = animator.GetCurrentAnimatorStateInfo(0);
            if (current.shortNameHash == hurtStateHash
                || current.fullPathHash == hurtStateFullPathHash
                || current.IsName(hurtStateName)
                || current.IsName($"Base Layer.{hurtStateName}"))
                return true;

            if (animator.IsInTransition(0))
            {
                var next = animator.GetNextAnimatorStateInfo(0);
                if (next.shortNameHash == hurtStateHash
                    || next.fullPathHash == hurtStateFullPathHash
                    || next.IsName(hurtStateName)
                    || next.IsName($"Base Layer.{hurtStateName}"))
                    return true;
            }

            return false;
        }

        private void FacePlayer()
        {
            if (spriteRenderer == null || player == null) return;

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

        private void OnDestroy()
        {
            if (healthBarController != null)
                Destroy(healthBarController.gameObject);
        }
    }
}
