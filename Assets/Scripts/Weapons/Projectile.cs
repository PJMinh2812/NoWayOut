using UnityEngine;

namespace SoulKnightClone.Weapons
{
    /// <summary>
    /// Script cho đạn - sử dụng Object Pooling, tự động hủy khi va chạm
    /// Implement IPooledObject để tương thích với ObjectPooler
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour, Core.IPooledObject
    {
        [Header("Visual")]
        [SerializeField] private SpriteRenderer bulletSprite;
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private GameObject hitEffectPrefab;

        // Bullet properties
        private Vector2 direction;
        private float speed;
        private int damage;
        private float lifetime;
        private string shooterTag; // "Player" hoặc "Enemy"

        // Components
        private Rigidbody2D rb;
        private float spawnTime;
        private bool isInitialized = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            
            // Configure Rigidbody2D
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Đảm bảo bullet không bị rotation
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        /// <summary>
        /// Khởi tạo đạn với các thông số
        /// </summary>
        public void Initialize(Vector2 dir, float spd, int dmg, float life, string shooter)
        {
            direction = dir.normalized;
            speed = spd;
            damage = dmg;
            lifetime = life;
            shooterTag = shooter;
            spawnTime = Time.time;
            isInitialized = true;

            // Set rotation theo hướng bay
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // Set velocity
            rb.velocity = direction * speed;
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Tự hủy sau lifetime
            if (Time.time - spawnTime >= lifetime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Ignore collision với người bắn
            if (collision.CompareTag(shooterTag))
                return;

            // Va chạm với tường
            if (collision.CompareTag(Core.GameConstants.TAG_WALL))
            {
                SpawnHitEffect();
                ReturnToPool();
                return;
            }

            // Va chạm với Enemy (nếu player bắn)
            if (shooterTag == "Player" && collision.CompareTag(Core.GameConstants.TAG_ENEMY))
            {
                // TODO: Deal damage to enemy
                var enemyHealth = collision.GetComponent<Player.PlayerStats>(); // Placeholder
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage);
                }

                SpawnHitEffect();
                ReturnToPool();
                return;
            }

            // Va chạm với Player (nếu enemy bắn)
            if (shooterTag == "Enemy" && collision.CompareTag(Core.GameConstants.TAG_PLAYER))
            {
                var playerHealth = collision.GetComponent<Player.PlayerStats>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }

                SpawnHitEffect();
                ReturnToPool();
                return;
            }
        }

        private void SpawnHitEffect()
        {
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 0.5f);
            }
        }

        private void ReturnToPool()
        {
            isInitialized = false;
            rb.velocity = Vector2.zero;

            if (trail != null)
            {
                trail.Clear();
            }

            // Trả về pool
            if (Core.ObjectPooler.Instance != null)
            {
                Core.ObjectPooler.Instance.ReturnToPool(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        #region IPooledObject Implementation
        public void OnObjectSpawn()
        {
            // Reset khi được spawn từ pool
            if (trail != null)
            {
                trail.Clear();
            }
        }
        #endregion
    }
}
