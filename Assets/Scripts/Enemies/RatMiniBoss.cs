using UnityEngine;
using NWO;

namespace NWO
{
    /// <summary>
    /// Mini-Boss: Con chuột trong phòng băng (Phòng 3)
    /// - Ban đầu ngủ
    /// - Thức giấc khi player đến gần
    /// - Đuổi theo và tấn công player
    /// - Có thể bị thùng gỗ chặn đường
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class RatMiniBoss : MonoBehaviour
    {
        [Header("Boss States")]
        [SerializeField] private bool isAwake = false;
        [SerializeField] private bool isDead = false;
        
        [Header("Stats")]
        [SerializeField] private int maxHealth = 50;
        [SerializeField] private int currentHealth;
        
        [Header("Detection")]
        [Tooltip("Khoảng cách để thức giấc")]
        [SerializeField] private float wakeUpDistance = 8f;
        
        [Tooltip("Khoảng cách đuổi theo")]
        [SerializeField] private float chaseDistance = 12f;
        
        [Header("Movement")]
        [SerializeField] private float moveAcceleration = 120f; // Lực đẩy cao để tăng tốc nhanh
        [SerializeField] private float maxSpeed = 3.5f; // Tốc độ tối đa
        [SerializeField] private float linearDamping = 1f; // Lực cản (set trong Awake)
        
        [Header("Attack")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float knockbackForce = 5f;
        
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
        
        [Header("Fireball")]
        [Tooltip("Prefab BossFireball để spawn (cần gắn component BossFireball)")]
        [SerializeField] private GameObject fireballPrefab;
        [Tooltip("Khoảng cách tối đa để bắn fireball")]
        [SerializeField] private float fireballShootRange = 10f;
        [Tooltip("Thời gian hồi chiêu giữa các lần bắn (giây)")]
        [SerializeField] private float fireballCooldown = 3f;
        [Tooltip("Damage mỗi viên fireball")]
        [SerializeField] private int fireballDamage = 8;
        [Tooltip("Tốc độ bay của fireball")]
        [SerializeField] private float fireballSpeed = 6f;
        [Tooltip("Số lượng viên trong pattern Circle (mặc định 8)")]
        [SerializeField] private int circleCount = 8;
        [Tooltip("Góc toả của Triple shot (độ)")]
        [SerializeField] private float tripleSpreadAngle = 30f;
        [Tooltip("Âm thanh khi bắn fireball")]
        [SerializeField] private AudioClip fireballSound;

        [Header("Loot")]
        [SerializeField] private GameObject[] lootDrops; // Các item rơi ra khi chết
        [SerializeField] private int minDrops = 1;
        [SerializeField] private int maxDrops = 3;
        
        // Components
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private AudioSource audioSource;
        private UI.Enemy2DHealthBarController _healthBarController;
        
        // References
        private PlayerController2D player;
        private PlayerHealth2D playerHealth;
        
        // State tracking
        private float lastAttackTime;
        private Color originalColor;
        private Vector2 startPosition;
        
        // Combat flags (giống Enemy2D)
        private bool _canAttack = true;
        private bool _isAttacking = false;

        // Fireball state
        private bool _isShooting   = false;
        private bool _canShoot     = true;

        // Flash red coroutine tracking
        private Coroutine _flashCoroutine;

        // Cached animator hashes
        private static readonly int HashIsSleeping = Animator.StringToHash("isSleeping");
        private static readonly int HashIsAwakeAnim = Animator.StringToHash("isAwake");
        private static readonly int HashIsMoving = Animator.StringToHash("isMoving");
        private static readonly int HashWakeUp = Animator.StringToHash("wakeUp");
        private static readonly int HashIsAttacking = Animator.StringToHash("isAttacking");
        private static readonly int HashHurt = Animator.StringToHash("hurt");
        private static readonly int HashDeath = Animator.StringToHash("Death");
        private static readonly int HashIsDead = Animator.StringToHash("isDead");

        // Cached set of valid animator parameter hashes for safe setting
        private System.Collections.Generic.HashSet<int> _validBoolParams;
        private System.Collections.Generic.HashSet<int> _validTriggerParams;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Cấu hình physics cho chuyển động tự nhiên
            rb.linearDamping = linearDamping; // Lực cản tự động làm chậm
            
            // Boss ngủ thì không chịu gravity (không rơi xuống)
            if (!isAwake)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                Debug.Log($"[RatMiniBoss] Awake - Position: {transform.position}, BodyType: Kinematic (ngủ)");
            }
            
            currentHealth = maxHealth;
            originalColor = spriteRenderer.color;
            startPosition = transform.position;

            // Cache valid animator parameters for O(1) lookup instead of iterating every call
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
            
            // Spawn health bar (giống Enemy2D)
            if (healthBarPrefab != null)
            {
                var healthBarObj = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
                _healthBarController = healthBarObj.GetComponent<UI.Enemy2DHealthBarController>();
                if (_healthBarController != null)
                {
                    _healthBarController.SetTarget(this);
                }
            }
        }
        
        private void Start()
        {
            // Tìm player
            player = FindFirstObjectByType<PlayerController2D>();
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth2D>();
                Debug.Log($"[RatMiniBoss] Found Player: {player.name}");
            }
            else
            {
                Debug.LogError("[RatMiniBoss] KHÔNG TÌM THẤY PLAYER! Kiểm tra PlayerController2D component.");
            }
            
            // DEBUG: FORCE WAKE UP NGAY (xóa sau khi test xong)
            // WakeUp();
            
            // Nếu đang ngủ, đổi màu sprite
            if (!isAwake)
            {
                spriteRenderer.color = sleepingTint;
                
                // Set animator parameters (nếu có)
                SafeSetBool(HashIsSleeping, true);
                SafeSetBool(HashIsAwakeAnim, false);
                SafeSetBool(HashIsMoving, false); // Đảm bảo không di chuyển khi ngủ
            }
            
            Debug.Log($"[RatMiniBoss] Setup complete. Awake: {isAwake}, Dead: {isDead}");
        }
        
        private void Update()
        {
            if (isDead || player == null) return;
            
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
            
            // Thức giấc khi player đến gần
            if (!isAwake && distanceToPlayer <= wakeUpDistance)
            {
                WakeUp();
            }
        }
        
        private void FixedUpdate()
        {
            if (isDead) return;
            
            // Set damping
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
            
            // Start attack if within range and ready
            if (distanceToPlayer <= attackRange && _canAttack && !_isAttacking && !_isShooting)
            {
                StartCoroutine(PerformAttack());
            }

            // Bắn fireball khi player trong tầm, không đang tấn công, không đang bắn
            if (fireballPrefab != null
                && distanceToPlayer > attackRange
                && distanceToPlayer <= fireballShootRange
                && _canShoot && !_isShooting && !_isAttacking)
            {
                StartCoroutine(ShootFireball());
            }

            // Đứng yên khi đang tấn công cận chiến hoặc đang bắn
            if (_isAttacking || _isShooting)
            {
                SafeSetBool(HashIsMoving, false);
                rb.linearVelocity = Vector2.zero;

                // Flip sprite về phía player
                if (spriteRenderer != null && player != null)
                {
                    spriteRenderer.flipX = (player.transform.position.x - transform.position.x) < 0f;
                }
                return;
            }
            
            // Đuổi theo player nếu trong chase distance
            if (distanceToPlayer <= chaseDistance)
            {
                ChasePlayer(distanceToPlayer);
            }
            else
            {
                // Nếu player chạy xa quá, dừng lại
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 5f);
                SafeSetBool(HashIsMoving, false);
            }
        }
        
        private void WakeUp()
        {
            if (isAwake) return;
            
            isAwake = true;
            spriteRenderer.color = originalColor;
            
            // Bật physics khi thức giấc (chuyển từ Kinematic → Dynamic)
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                Debug.Log($"[RatMiniBoss] Wake Up! Position: {transform.position}, BodyType: Dynamic");
            }
            
            // Animation - thức giấc nhưng vẫn idle trước
            SafeSetBool(HashIsSleeping, false);
            SafeSetBool(HashIsAwakeAnim, true);
            SafeSetBool(HashIsMoving, false);
            SafeSetTrigger(HashWakeUp);
            
            // Sound effect
            if (wakeUpSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(wakeUpSound);
            }
            
            // Particle effect
            if (wakeUpEffect != null)
            {
                wakeUpEffect.Play();
            }
            
            Debug.Log("[RatMiniBoss] CON CHUỘT ĐÃ THỨC GIẤC! 🐭");
        }
        
        private void ChasePlayer(float distance)
        {
            // Move towards player (even if in attack range but on cooldown)
            // Stop only when very close to avoid jittering
            if (distance > 0.5f)
            {
                Vector2 direction = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
                
                // Chuyển động realistic với AddForce
                rb.AddForce(direction * moveAcceleration, ForceMode2D.Force);
                
                // Giới hạn tốc độ tối đa
                if (rb.linearVelocity.magnitude > maxSpeed)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
                }
            }
            
            // Update animator isMoving param
            if (animator != null)
            {
                bool isMoving = rb.linearVelocity.magnitude > 0.1f;
                SafeSetBool(HashIsMoving, isMoving);
            }
            
            // Flip sprite to face movement/player (giống Enemy2D)
            if (spriteRenderer != null)
            {
                float vx = rb.linearVelocity.x;
                if (Mathf.Abs(vx) > 0.05f)
                {
                    spriteRenderer.flipX = vx < 0f; // flip when moving left
                }
                else if (player != null)
                {
                    spriteRenderer.flipX = (player.transform.position.x - transform.position.x) < 0f;
                }
            }
        }
        
        // ============================================================
        //  FIREBALL SHOOTING
        // ============================================================

        /// <summary>
        /// Chọn ngẫu nhiên một trong 4 pattern và thực hiện bắn.
        /// 0 = Single | 1 = Triple | 2 = Circle | 3 = Rain
        /// </summary>
        private System.Collections.IEnumerator ShootFireball()
        {
            if (isDead || player == null) yield break;

            _isShooting = true;
            _canShoot   = false;

            // Flip về phía player
            if (spriteRenderer != null)
                spriteRenderer.flipX = (player.transform.position.x - transform.position.x) < 0f;

            int pattern = Random.Range(0, 4); // 0-3
            Debug.Log($"[RatMiniBoss] Fireball pattern: {pattern}");

            switch (pattern)
            {
                case 0: yield return StartCoroutine(FireSingle());  break;
                case 1: yield return StartCoroutine(FireTriple());  break;
                case 2: yield return StartCoroutine(FireCircle());  break;
                case 3: yield return StartCoroutine(FireRain());    break;
            }

            _isShooting = false;

            // Hồi chiêu
            yield return new WaitForSeconds(fireballCooldown);
            _canShoot = true;
        }

        /// <summary>Pattern 0 – Single: một viên thẳng vào player.</summary>
        private System.Collections.IEnumerator FireSingle()
        {
            yield return new WaitForSeconds(0.2f); // windup
            SpawnFireball(GetDirectionToPlayer());
        }

        /// <summary>Pattern 1 – Triple: 3 viên toả ra góc ±tripleSpreadAngle.</summary>
        private System.Collections.IEnumerator FireTriple()
        {
            yield return new WaitForSeconds(0.2f);
            Vector2 baseDir = GetDirectionToPlayer();
            SpawnFireball(Rotate(baseDir, -tripleSpreadAngle));
            SpawnFireball(baseDir);
            SpawnFireball(Rotate(baseDir,  tripleSpreadAngle));
        }

        /// <summary>Pattern 2 – Circle: bắn đều circleCount viên quanh 360°.</summary>
        private System.Collections.IEnumerator FireCircle()
        {
            yield return new WaitForSeconds(0.2f);
            float step = 360f / circleCount;
            for (int i = 0; i < circleCount; i++)
            {
                Vector2 dir = Rotate(Vector2.right, step * i);
                SpawnFireball(dir);
            }
        }

        /// <summary>
        /// Pattern 3 – Rain: bắn 6 viên lần lượt, xen kẽ 0.18 giây, mỗi viên
        /// ngắm vào vị trí player có offset ngẫu nhiên nhỏ (giả lập mưa lửa).
        /// </summary>
        private System.Collections.IEnumerator FireRain()
        {
            int rainCount = 6;
            float spreadRadius = 1.5f;
            for (int i = 0; i < rainCount; i++)
            {
                if (isDead || player == null) yield break;

                Vector2 offset   = Random.insideUnitCircle * spreadRadius;
                Vector2 target   = (Vector2)player.transform.position + offset;
                Vector2 dir      = (target - (Vector2)transform.position).normalized;
                SpawnFireball(dir);
                yield return new WaitForSeconds(0.18f);
            }
        }

        // ---------- Spawn helper ----------

        private void SpawnFireball(Vector2 direction)
        {
            if (fireballPrefab == null) return;

            GameObject fb = Instantiate(fireballPrefab, transform.position, Quaternion.identity);
            BossFireball script = fb.GetComponent<BossFireball>();
            if (script != null)
            {
                script.Fire(direction, fireballSpeed, fireballDamage);
            }
            else
            {
                // Fallback: dùng SpellProjectile nếu prefab không có BossFireball
                SpellProjectile sp = fb.GetComponent<SpellProjectile>();
                if (sp != null)
                {
                    sp.SetDamage(fireballDamage);
                    sp.Fire(direction);
                }
            }

            PlaySound(fireballSound);
        }

        private Vector2 GetDirectionToPlayer()
        {
            if (player == null) return Vector2.right;
            return ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        }

        /// <summary>Xoay vector 2D theo góc (độ).</summary>
        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip);
        }

        // ============================================================

        /// <summary>
        /// Perform complete attack sequence: animation -> deal damage -> cooldown (giống Enemy2D)
        /// </summary>
        private System.Collections.IEnumerator PerformAttack()
        {
            if (isDead || player == null || playerHealth == null) yield break;
            
            _isAttacking = true;
            _canAttack = false;
            
            // Start attack animation
            SafeSetBool(HashIsAttacking, true);
            
            // Sound
            if (attackSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(attackSound);
            }
            
            // Wait for attack animation to reach hit frame (deal damage halfway through)
            float attackAnimDuration = 0.4f; // Duration of attack animation
            yield return new WaitForSeconds(attackAnimDuration * 0.5f);
            
            // Deal damage only after animation has progressed
            if (player != null && playerHealth != null)
            {
                float dist = Vector2.Distance(transform.position, player.transform.position);
                if (dist <= attackRange * 1.2f) // Slight tolerance
                {
                    Vector2 knockbackDir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
                    Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                    playerHealth.TakeDamage(attackDamage, knockbackDir * knockbackForce, playerRb);
                    
                    // Particle effect at hit moment
                    if (attackEffect != null)
                    {
                        attackEffect.Play();
                    }
                    
                    Debug.Log($"[RatMiniBoss] CHUỘT TẤN CÔNG! Gây {attackDamage} damage cho Player");
                }
            }
            
            // Wait for rest of attack animation
            yield return new WaitForSeconds(attackAnimDuration * 0.5f);
            
            // Reset attack animation
            SafeSetBool(HashIsAttacking, false);
            _isAttacking = false;
            
            // Start cooldown before next attack
            yield return new WaitForSeconds(attackCooldown);
            _canAttack = true;
        }
        
        // ------------------------------------------------------------------ //
        //  Public health accessors (dùng cho BossHealthBarUI cinematic)     //
        // ------------------------------------------------------------------ //

        public int GetCurrentHealth() => currentHealth;
        public int GetMaxHealth()     => maxHealth;

        /// <summary>
        /// Nhận damage từ bên ngoài (vũ khí player)
        /// </summary>
        public void TakeDamage(int damage, Vector2 hitDirection, float knockbackPower)
        {
            if (isDead) return;
            
            Debug.Log($"[RatMiniBoss] TakeDamage called! Damage: {damage}, HP before: {currentHealth}");
            
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            // Cập nhật health bar ngay lập tức (giống Enemy2D)
            _healthBarController?.OnHealthChanged(currentHealth, maxHealth);

            // Show floating damage number
            UI.DamagePopup.Spawn(transform.position, damage);
            
            // Knockback
            rb.AddForce(hitDirection.normalized * knockbackPower, ForceMode2D.Impulse);
            
            // Visual feedback - flash đỏ (dừng coroutine cũ trước để tránh stuck màu đỏ)
            if (spriteRenderer != null)
            {
                if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
                spriteRenderer.color = originalColor; // reset về màu gốc trước
                _flashCoroutine = StartCoroutine(FlashRed());
            }
            
            // Sound
            if (hurtSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hurtSound);
            }
            
            // Animation
            SafeSetTrigger(HashHurt);
            
            Debug.Log($"[RatMiniBoss] Nhận {damage} damage! HP: {currentHealth}/{maxHealth}");
            
            // Chết nếu hết máu
            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                // Thức giấc nếu bị đánh khi đang ngủ
                if (!isAwake)
                {
                    WakeUp();
                }
            }
        }
        
        private void Die()
        {
            if (isDead) return;

            isDead = true;

            // Drop coin boss vật lý khi chết
            if (CoinManager.Instance != null)
                CoinManager.Instance.SpawnCoinsFromBoss(transform.position);

            // Ẩn health bar ngay khi chết (giống Enemy2D)
            _healthBarController?.OnEnemyDied();

            // Animation - use Trigger to avoid re-triggering from Any State (giống Enemy2D)
            SafeSetTrigger(HashDeath); // Use Trigger instead of "die"
            SafeSetBool(HashIsDead, true); // Optional: can be used for conditions
            
            // Sound
            if (deathSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(deathSound);
            }
            
            // Vô hiệu hóa physics
            rb.simulated = false;
            
            // Tắt collider
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
            }
            
            // Thông báo boss manager (nếu có)
            BossManager bossManager = FindFirstObjectByType<BossManager>();
            if (bossManager != null)
            {
                bossManager.OnBossDefeated();
            }
            
            Debug.Log("[RatMiniBoss] CHUỘT ĐÃ CHẾT! ☠️");
            
            // Rơi loot
            DropLoot();
            
            // Destroy sau 2 giây (cho animation chết)
            Destroy(gameObject, 2f);
        }
        
        private System.Collections.IEnumerator FlashRed()
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            spriteRenderer.color = originalColor; // dùng field originalColor đã lưu trong Awake
            _flashCoroutine = null;
        }
        
        /// <summary>
        /// Helper: Set animator parameter an toàn (kiểm tra tồn tại trước) - O(1) with cached HashSet
        /// </summary>
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
        
        /// <summary>
        /// Rơi loot khi chết
        /// </summary>
        private void DropLoot()
        {
            if (lootDrops == null || lootDrops.Length == 0) return;
            
            int dropCount = Random.Range(minDrops, maxDrops + 1);
            
            for (int i = 0; i < dropCount; i++)
            {
                // Chọn ngẫu nhiên một item
                GameObject lootPrefab = lootDrops[Random.Range(0, lootDrops.Length)];
                
                if (lootPrefab != null)
                {
                    // Rơi ra xung quanh vị trí boss
                    Vector2 randomOffset = Random.insideUnitCircle * 2f;
                    Vector3 dropPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);
                    
                    GameObject loot = Instantiate(lootPrefab, dropPosition, Quaternion.identity);
                    
                    // Thêm lực đẩy nhẹ
                    Rigidbody2D lootRb = loot.GetComponent<Rigidbody2D>();
                    if (lootRb != null)
                    {
                        Vector2 force = randomOffset.normalized * Random.Range(2f, 5f);
                        lootRb.AddForce(force, ForceMode2D.Impulse);
                    }
                }
            }
        }
        
        /// <summary>
        /// Tự nhận damage khi spell/projectile chạm vào (trigger collider)
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDead) return;
            if (other.TryGetComponent<SpellProjectile>(out var spell))
            {
                HandleSpellHit(spell, other.transform.position);
            }
        }

        /// <summary>
        /// Tự nhận damage khi spell/projectile chạm vào (non-trigger collider)
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isDead) return;
            if (collision.collider.TryGetComponent<SpellProjectile>(out var spell))
            {
                HandleSpellHit(spell, collision.collider.transform.position);
            }
        }

        // Frame lock để tránh double-damage khi cả SpellProjectile lẫn RatMiniBoss đều detect hit cùng lúc
        private int _lastSpellHitFrame = -1;

        private void HandleSpellHit(SpellProjectile spell, Vector3 spellPosition)
        {
            if (Time.frameCount == _lastSpellHitFrame) return; // đã nhận damage frame này rồi
            _lastSpellHitFrame = Time.frameCount;

            // Lấy damage từ SpellProjectile bằng reflection (vì field là private)
            int spellDamage = 10; // fallback mặc định
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

        // Gizmos để debug
        private void OnDrawGizmosSelected()
        {
            // Wake up range (vàng)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, wakeUpDistance);
            
            // Chase range (xanh)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, chaseDistance);
            
            // Attack range (đỏ)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            #if UNITY_EDITOR
            // Label
            string status = isDead ? "DEAD" : (isAwake ? "AWAKE" : "SLEEPING");
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                $"RAT BOSS\n{status}\nHP: {currentHealth}/{maxHealth}");
            #endif
        }
    }
}
