using UnityEngine;

namespace NWO
{
    // Projectile bay, gây damage, tự hủy sau thời gian hoặc va chạm
    [RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Collider2D))]
    public class SpellProjectile : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed = 8f;
        [SerializeField] private float lifetime = 3f;

        [Header("Damage")]
        [SerializeField] private int damage = 10;
        [SerializeField] private float knockbackForce = 3f;

        [Header("Collision")]
        [Tooltip("Các layer sẽ bị ignore khi va chạm")]
        [SerializeField] private LayerMask ignoreLayerMask;
        [Tooltip("Destroy khi chạm bất kỳ object nào (trừ Player và ignore layers)")]
        [SerializeField] private bool destroyOnAnyHit = true;

        [Header("Effects")]
        [SerializeField] private GameObject hitEffectPrefab;

        private Rigidbody2D _rb;
        private Animator _animator;
        private Vector2 _direction;
        private float _timeAlive;
        private bool _isDestroyed = false;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();

            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        public void Fire(Vector2 direction)
        {
            _direction = direction.normalized;
            _rb.linearVelocity = _direction * speed;

            // Xoay và flip sprite theo hướng bay
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && (angle > 90f || angle < -90f))
            {
                spriteRenderer.flipY = true;
            }
        }

        /// <summary>
        /// Set damage từ bên ngoài (PlayerSpellController)
        /// </summary>
        public void SetDamage(int newDamage)
        {
            damage = newDamage;
        }

        private void Update()
        {
            _timeAlive += Time.deltaTime;

            if (_timeAlive >= lifetime)
            {
                DestroyProjectile();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isDestroyed) return;

            // Bỏ qua Player
            if (other.CompareTag("Player"))
            {
                return;
            }

            // Bỏ qua các layer được chỉ định
            if (ignoreLayerMask != 0 && ((1 << other.gameObject.layer) & ignoreLayerMask) != 0)
            {
                return;
            }

            // Bỏ qua các projectile khác
            if (other.GetComponent<SpellProjectile>() != null)
            {
                return;
            }

            // Chỉ gây damage cho Enemy - KHÔNG phá map/tilemap/wall
            if (other.TryGetComponent<Enemy2D>(out var enemy))
            {
                var hitDir = (Vector2)other.transform.position - (Vector2)transform.position;
                enemy.TakeDamage(damage, hitDir.normalized, knockbackForce);
                Debug.Log($"[SpellProjectile] Hit enemy {other.name}! Damage: {damage}");
            }
            else if (other.TryGetComponent<RatMiniBoss>(out var boss))
            {
                var hitDir = (Vector2)other.transform.position - (Vector2)transform.position;
                boss.TakeDamage(damage, hitDir.normalized, knockbackForce);
                Debug.Log($"[SpellProjectile] Hit boss {other.name}! Damage: {damage}");
            }
            // Tilemap, Wall, Ground - chỉ destroy spell, KHÔNG gây damage

            // Destroy khi chạm bất kỳ vật thể nào
            if (destroyOnAnyHit)
            {
                SpawnHitEffect();
                DestroyProjectile();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isDestroyed) return;

            // Bỏ qua Player
            if (collision.collider.CompareTag("Player"))
            {
                return;
            }

            // Bỏ qua các layer được chỉ định
            if (ignoreLayerMask != 0 && ((1 << collision.gameObject.layer) & ignoreLayerMask) != 0)
            {
                return;
            }

            // Chỉ gây damage cho Enemy - KHÔNG phá map/tilemap/wall
            if (collision.collider.TryGetComponent<Enemy2D>(out var enemy))
            {
                var hitDir = (Vector2)collision.transform.position - (Vector2)transform.position;
                enemy.TakeDamage(damage, hitDir.normalized, knockbackForce);
                Debug.Log($"[SpellProjectile] Hit enemy {collision.collider.name}! Damage: {damage}");
            }
            else if (collision.collider.TryGetComponent<RatMiniBoss>(out var boss))
            {
                var hitDir = (Vector2)collision.transform.position - (Vector2)transform.position;
                boss.TakeDamage(damage, hitDir.normalized, knockbackForce);
                Debug.Log($"[SpellProjectile] Hit boss {collision.collider.name}! Damage: {damage}");
            }
            // Tilemap, Wall, Ground - chỉ destroy spell, KHÔNG gây damage

            // Destroy khi collision
            if (destroyOnAnyHit)
            {
                SpawnHitEffect();
                DestroyProjectile();
            }
        }

        private void SpawnHitEffect()
        {
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }
        }

        private void DestroyProjectile()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;
            
            // Tắt collider và velocity ngay lập tức
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            _rb.linearVelocity = Vector2.zero;
            
            Destroy(gameObject);
        }
    }
}