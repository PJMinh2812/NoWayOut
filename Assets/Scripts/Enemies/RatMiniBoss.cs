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

        // Flash red coroutine tracking
        private Coroutine _flashCoroutine;
        
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
                SafeSetBool("isSleeping", true);
                SafeSetBool("isAwake", false);
                SafeSetBool("isMoving", false); // Đảm bảo không di chuyển khi ngủ
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
                SafeSetBool("isMoving", false);
                return;
            }
            
            if (player == null)
            {
                SafeSetBool("isMoving", false);
                return;
            }
            
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
            
            // Start attack if within range and ready
            if (distanceToPlayer <= attackRange && _canAttack && !_isAttacking)
            {
                StartCoroutine(PerformAttack());
            }
            
            // Don't move during attack - stop and play attack animation
            if (_isAttacking)
            {
                SafeSetBool("isMoving", false);
                rb.linearVelocity = Vector2.zero;
                
                // Flip sprite to face player during attack
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
                SafeSetBool("isMoving", false);
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
            SafeSetBool("isSleeping", false);
            SafeSetBool("isAwake", true);
            SafeSetBool("isMoving", false);
            SafeSetTrigger("wakeUp");
            
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
                SafeSetBool("isMoving", isMoving);
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
        
        /// <summary>
        /// Perform complete attack sequence: animation -> deal damage -> cooldown (giống Enemy2D)
        /// </summary>
        private System.Collections.IEnumerator PerformAttack()
        {
            if (isDead || player == null || playerHealth == null) yield break;
            
            _isAttacking = true;
            _canAttack = false;
            
            // Start attack animation
            SafeSetBool("isAttacking", true);
            
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
            SafeSetBool("isAttacking", false);
            _isAttacking = false;
            
            // Start cooldown before next attack
            yield return new WaitForSeconds(attackCooldown);
            _canAttack = true;
        }
        
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
            SafeSetTrigger("hurt");
            
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

            // Ẩn health bar ngay khi chết (giống Enemy2D)
            _healthBarController?.OnEnemyDied();

            // Animation - use Trigger to avoid re-triggering from Any State (giống Enemy2D)
            SafeSetTrigger("Death"); // Use Trigger instead of "die"
            SafeSetBool("isDead", true); // Optional: can be used for conditions
            
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
        /// Getter cho health (tương thích với Enemy2D)
        /// </summary>
        public int GetCurrentHealth() => currentHealth;
        public int GetMaxHealth() => maxHealth;
        
        /// <summary>
        /// Helper: Set animator parameter an toàn (kiểm tra tồn tại trước)
        /// </summary>
        private void SafeSetBool(string paramName, bool value)
        {
            if (animator == null) return;
            
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool(paramName, value);
                    return;
                }
            }
        }
        
        private void SafeSetTrigger(string paramName)
        {
            if (animator == null) return;
            
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName && param.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger(paramName);
                    return;
                }
            }
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
