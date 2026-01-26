using UnityEngine;

namespace GloomCraft
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class Projectile2D : MonoBehaviour
    {
        [SerializeField] private float lifetime = 2.0f;
        [SerializeField] private float speed = 12f;
        [SerializeField] private float damage = 10f;

        private Rigidbody2D _rb;
        private float _t;

        public void Fire(Vector2 direction)
        {
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            _rb.linearVelocity = direction * speed;
            transform.right = direction;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            _t += Time.deltaTime;
            if (_t >= lifetime) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<Enemy2D>(out var enemy))
            {
                var dir = (Vector2)enemy.transform.position - (Vector2)transform.position;
                Debug.Log($"[Projectile] Hit enemy! Dealing {damage} damage");
                enemy.TakeDamage(Mathf.RoundToInt(damage), dir, 4f);
                Destroy(gameObject);
                return;
            }

            // Destroy on any collision
            Destroy(gameObject);
        }
    }
}


