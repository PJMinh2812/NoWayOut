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
        private UI.EnemyHealthBarController healthBarController;
        
        // References
        private PlayerController2D player;
        private PlayerHealth2D playerHealth;
        
        // State tracking
        private float lastAttackTime;
        private Color originalColor;
        private Vector2 startPosition;
        
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
            
            // Tạo health bar đơn giản (chỉ dùng HealthBarUI, không cần EnemyHealthBarController)
            if (healthBarPrefab != null)
            {
                GameObject healthBarObj = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
                
                // Đặt làm con của boss để tự động follow
                healthBarObj.transform.SetParent(transform);
                healthBarObj.transform.localPosition = new Vector3(0, 2.0f, 0);
                
                // Lấy HealthBarUI component (có thể ở Canvas hoặc child)
                UI.HealthBarUI healthBarUI = healthBarObj.GetComponentInChildren<UI.HealthBarUI>();
                if (healthBarUI == null)
                {
                    healthBarUI = healthBarObj.GetComponent<UI.HealthBarUI>();
                }
                
                if (healthBarUI != null)
                {
                    healthBarUI.Initialize(currentHealth, maxHealth);
                    Debug.Log("[RatMiniBoss] Health bar created successfully!");
                }
                else
                {
                    Debug.LogWarning("[RatMiniBoss] HealthBarUI component not found in prefab!");
                }
            }
            else
            {
                Debug.LogWarning("[RatMiniBoss] Health Bar Prefab not assigned!");
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
            if (isDead || !isAwake) return;
            
            // Giảm friction để boss di chuyển nhanh hơn
            rb.linearDamping = 0.5f; // Thay vì dùng friction = 8
            
            if (player == null) return;
            
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
            
            // Đuổi theo player
            if (distanceToPlayer <= chaseDistance)
            {
                ChasePlayer(distanceToPlayer);
                
                // Tấn công nếu đủ gần
                if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
                {
                    AttackPlayer();
                }
            }
            else
            {
                // Nếu player chạy xa quá, dừng lại
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 5f);
                
                SafeSetBool("isRunning", false);
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
            
            // Animation
            SafeSetBool("isSleeping", false);
            SafeSetBool("isAwake", true);
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
            if (distance < 0.1f) return;
            
            Vector2 direction = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            
            // Chuyển động realistic với AddForce
            rb.AddForce(direction * moveAcceleration, ForceMode2D.Force);
            
            // Giới hạn tốc độ tối đa
            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
            
            // Flip sprite theo hướng di chuyển
            if (direction.x > 0.1f)
            {
                spriteRenderer.flipX = false;
            }
            else if (direction.x < -0.1f)
            {
                spriteRenderer.flipX = true;
            }
            
            // Animation
            SafeSetBool("isRunning", rb.linearVelocity.magnitude > 0.5f);
        }
        
        private void AttackPlayer()
        {
            lastAttackTime = Time.time;
            
            // Animation
            SafeSetTrigger("attack");
            
            // Sound
            if (attackSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(attackSound);
            }
            
            // Particle effect (optional)
            if (attackEffect != null)
            {
                attackEffect.Play();
            }
            
            // Gây damage cho player
            if (playerHealth != null && player != null)
            {
                Vector2 knockbackDir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                playerHealth.TakeDamage(attackDamage, knockbackDir * knockbackForce, playerRb);
                
                Debug.Log($"[RatMiniBoss] CHUỘT TẤN CÔNG! Gây {attackDamage} damage cho Player");
            }
            else
            {
                Debug.LogWarning("[RatMiniBoss] Không thể tấn công - PlayerHealth hoặc Player null!");
            }
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
            
            // Cập nhật health bar
            UpdateHealthBar();
            
            // Knockback
            rb.AddForce(hitDirection.normalized * knockbackPower, ForceMode2D.Impulse);
            
            // Visual feedback - flash đỏ
            if (spriteRenderer != null)
            {
                StartCoroutine(FlashRed());
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
            
            // Animation
            SafeSetTrigger("die");
            SafeSetBool("isDead", true);
            
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
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
        
        /// <summary>
        /// Cập nhật health bar UI
        /// </summary>
        private void UpdateHealthBar()
        {
            // Tìm HealthBarUI trực tiếp vì EnemyHealthBarController đã bị disable
            UI.HealthBarUI healthBarUI = GetComponentInChildren<UI.HealthBarUI>();
            if (healthBarUI != null)
            {
                healthBarUI.SetHealth(currentHealth, maxHealth);
            }
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
