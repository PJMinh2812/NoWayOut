using UnityEngine;

namespace NWO
{
    // Projectile bay, gây damage, tự hủy khi đạt phạm vi tối đa hoặc va chạm
    // - Va chạm vật thể/quái: hủy ngay lập tức
    // - Bay trong không gian: fade-out tan biến khi đạt max range
    [RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Collider2D))]
    public class SpellProjectile : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed = 8f;

        [Header("Range (phạm vi bay)")]
        [Tooltip("Khoảng cách tối đa spell có thể bay (units). Được set từ PlayerSpellController theo loại spell.")]
        [SerializeField] private float maxRange = 5f;
        [Tooltip("Phần trăm range cuối cùng bắt đầu fade-out (0.0 - 1.0)")]
        [SerializeField] private float fadeStartPercent = 0.8f;

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
        private SpriteRenderer _spriteRenderer;
        private Vector2 _direction;
        private Vector3 _spawnPosition;
        private float _distanceTraveled;
        private bool _isDestroyed = false;
        private bool _isFading = false;
        private float _fadeProgress = 0f;
        private Color _originalColor;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }
        }

        public void Fire(Vector2 direction)
        {
            _direction = direction.normalized;
            _spawnPosition = transform.position;
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

        /// <summary>
        /// Set phạm vi bay tối đa từ bên ngoài (PlayerSpellController truyền range theo loại spell)
        /// </summary>
        public void SetMaxRange(float range)
        {
            maxRange = range;
        }

        private void Update()
        {
            if (_isDestroyed) return;

            // Tính khoảng cách đã bay từ điểm spawn
            _distanceTraveled = Vector3.Distance(_spawnPosition, transform.position);

            // Tính fade dựa trên khoảng cách
            float fadeStartDistance = maxRange * fadeStartPercent;
            float fadeDistance = maxRange - fadeStartDistance; // khoảng cách fade

            if (!_isFading && _distanceTraveled >= fadeStartDistance)
            {
                StartFadeOut();
            }

            // Xử lý fade-out (tan biến dần) dựa trên khoảng cách
            if (_isFading)
            {
                _fadeProgress = Mathf.Clamp01((_distanceTraveled - fadeStartDistance) / fadeDistance);

                if (_spriteRenderer != null)
                {
                    Color c = _originalColor;
                    c.a = Mathf.Lerp(1f, 0f, _fadeProgress);
                    _spriteRenderer.color = c;
                }

                // Giảm tốc dần khi đang tan biến
                if (_rb != null)
                {
                    _rb.linearVelocity = _direction * speed * (1f - _fadeProgress * 0.7f);
                }

                // Scale nhỏ dần
                float scaleMultiplier = Mathf.Lerp(1f, 0.3f, _fadeProgress);
                transform.localScale = Vector3.one * scaleMultiplier;
            }

            // Hủy khi đạt phạm vi tối đa
            if (_distanceTraveled >= maxRange)
            {
                DestroyProjectile();
            }
        }

        /// <summary>
        /// Bắt đầu hiệu ứng tan biến (fade-out) khi sắp đạt max range
        /// </summary>
        private void StartFadeOut()
        {
            _isFading = true;

            // Tắt collider để không gây damage trong lúc tan biến
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            Debug.Log($"[SpellProjectile] Starting fade-out at distance {_distanceTraveled:F1}/{maxRange} units");
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
            else if (other.TryGetComponent<GoatManBoss>(out var goatBoss))
            {
                var hitDir = (Vector2)other.transform.position - (Vector2)transform.position;
                goatBoss.TakeDamage(damage, hitDir.normalized, knockbackForce);
                Debug.Log($"[SpellProjectile] Hit GoatManBoss {other.name}! Damage: {damage}");
            }
            else if (other.TryGetComponent<NightBonesBoss>(out var nightBonesBoss))
            {
                var hitDir = (Vector2)other.transform.position - (Vector2)transform.position;
                nightBonesBoss.TakeDamage(damage, hitDir.normalized, knockbackForce);
                Debug.Log($"[SpellProjectile] Hit NightBonesBoss {other.name}! Damage: {damage}");
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
            else if (collision.collider.TryGetComponent<GoatManBoss>(out var goatBoss))
            {
                var hitDir = (Vector2)collision.transform.position - (Vector2)transform.position;
                goatBoss.TakeDamage(damage, hitDir.normalized, knockbackForce);
                Debug.Log($"[SpellProjectile] Hit GoatManBoss {collision.collider.name}! Damage: {damage}");
            }
            else if (collision.collider.TryGetComponent<NightBonesBoss>(out var nightBonesBoss))
            {
                var hitDir = (Vector2)collision.transform.position - (Vector2)transform.position;
                nightBonesBoss.TakeDamage(damage, hitDir.normalized, knockbackForce);
                Debug.Log($"[SpellProjectile] Hit NightBonesBoss {collision.collider.name}! Damage: {damage}");
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
            if (_rb != null) _rb.linearVelocity = Vector2.zero;

            // Reset sprite về transparent hoàn toàn trước khi destroy
            if (_spriteRenderer != null)
            {
                Color c = _spriteRenderer.color;
                c.a = 0f;
                _spriteRenderer.color = c;
            }
            
            Destroy(gameObject);
        }
    }
}