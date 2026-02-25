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

        [Header("Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private bool destroyOnHit = true;

        private Rigidbody2D _rb;
        private Animator _animator;
        private Vector2 _direction;
        private float _timeAlive;

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
            // Bỏ qua Player
            if (other.CompareTag("Player"))
            {
                return;
            }

            // Check for Enemy2D
            if (other.TryGetComponent<Enemy2D>(out var enemy))
            {
                var hitDir = (Vector2)other.transform.position - (Vector2)transform.position;
                enemy.TakeDamage(damage, hitDir.normalized, knockbackForce);

                Debug.Log($"[SpellProjectile] Hit Enemy2D! Damage: {damage}");

                if (destroyOnHit)
                {
                    SpawnHitEffect();
                    DestroyProjectile();
                }
                return;
            }

            // Check for RatMiniBoss (giống Projectile2D)
            if (other.TryGetComponent<RatMiniBoss>(out var boss))
            {
                var hitDir = (Vector2)other.transform.position - (Vector2)transform.position;
                boss.TakeDamage(damage, hitDir.normalized, knockbackForce);

                Debug.Log($"[SpellProjectile] Hit RatMiniBoss! Damage: {damage}");

                if (destroyOnHit)
                {
                    SpawnHitEffect();
                    DestroyProjectile();
                }
                return;
            }

            // Destroy on Wall collision
            if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
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
            Destroy(gameObject);
        }
    }
}