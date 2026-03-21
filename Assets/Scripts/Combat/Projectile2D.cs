using UnityEngine;

namespace NWO
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class Projectile2D : MonoBehaviour
    {
        [SerializeField] private float lifetime = 1.5f;
        [SerializeField] private float speed = 12f;
        [SerializeField] private float damage = 5f;
        [SerializeField] private float fadeOutDuration = 0.4f;

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private Collider2D _collider;
        private float _t;
        private bool _isFading = false;
        private bool _isDestroyed = false;
        private float _fadeTimer = 0f;
        private Color _originalColor;
        private Vector2 _direction;

        public void Fire(Vector2 direction)
        {
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            _direction = direction;
            _rb.linearVelocity = direction * speed;
            
            // Rotate projectile to face direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            
            Debug.Log($"[Projectile] Fired! Dir: {direction}, Speed: {speed}, Velocity: {_rb.linearVelocity}");
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }
        }

        private void Update()
        {
            if (_isDestroyed) return;

            _t += Time.deltaTime;

            // Bắt đầu fade-out khi gần hết lifetime
            float fadeStartTime = lifetime - fadeOutDuration;
            if (!_isFading && _t >= fadeStartTime)
            {
                _isFading = true;
                _fadeTimer = 0f;
                if (_collider != null) _collider.enabled = false;
            }

            // Xử lý fade-out (tan biến dần)
            if (_isFading)
            {
                _fadeTimer += Time.deltaTime;
                float fadeProgress = Mathf.Clamp01(_fadeTimer / fadeOutDuration);

                if (_spriteRenderer != null)
                {
                    Color c = _originalColor;
                    c.a = Mathf.Lerp(1f, 0f, fadeProgress);
                    _spriteRenderer.color = c;
                }

                // Giảm tốc dần và scale nhỏ dần
                if (_rb != null)
                {
                    _rb.linearVelocity = _direction * speed * (1f - fadeProgress * 0.7f);
                }
                float scaleMultiplier = Mathf.Lerp(1f, 0.3f, fadeProgress);
                transform.localScale = Vector3.one * scaleMultiplier;
            }

            if (_t >= lifetime)
            {
                _isDestroyed = true;
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isDestroyed) return;

            // Check for Enemy2D
            if (other.TryGetComponent<Enemy2D>(out var enemy))
            {
                var dir = (Vector2)enemy.transform.position - (Vector2)transform.position;
                Debug.Log($"[Projectile] Hit Enemy2D! Dealing {damage} damage");
                enemy.TakeDamage(Mathf.RoundToInt(damage), dir, 4f);
                DestroyImmediate();
                return;
            }
            
            // Check for RatMiniBoss
            if (other.TryGetComponent<RatMiniBoss>(out var boss))
            {
                var dir = (Vector2)boss.transform.position - (Vector2)transform.position;
                Debug.Log($"[Projectile] Hit RatMiniBoss! Dealing {damage} damage");
                boss.TakeDamage(Mathf.RoundToInt(damage), dir, 4f);
                DestroyImmediate();
                return;
            }

            // Check for GoatManBoss
            if (other.TryGetComponent<GoatManBoss>(out var goatBoss))
            {
                var dir = (Vector2)goatBoss.transform.position - (Vector2)transform.position;
                Debug.Log($"[Projectile] Hit GoatManBoss! Dealing {damage} damage");
                goatBoss.TakeDamage(Mathf.RoundToInt(damage), dir, 4f);
                DestroyImmediate();
                return;
            }

            // Check for NightBonesBoss
            if (other.TryGetComponent<NightBonesBoss>(out var nightBonesBoss))
            {
                var dir = (Vector2)nightBonesBoss.transform.position - (Vector2)transform.position;
                Debug.Log($"[Projectile] Hit NightBonesBoss! Dealing {damage} damage");
                nightBonesBoss.TakeDamage(Mathf.RoundToInt(damage), dir, 4f);
                DestroyImmediate();
                return;
            }

            // Destroy on any other collision (wall, etc) - va chạm vật thể -> hủy ngay
            if (!other.CompareTag("Player"))
            {
                DestroyImmediate();
            }
        }

        /// <summary>
        /// Hủy ngay lập tức khi va chạm (không fade)
        /// </summary>
        private void DestroyImmediate()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;
            if (_collider != null) _collider.enabled = false;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            Destroy(gameObject);
        }
    }
}


