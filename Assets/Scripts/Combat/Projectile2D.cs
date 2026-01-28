using UnityEngine;

namespace GloomCraft
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class Projectile2D : MonoBehaviour
    {
        [SerializeField] private float lifetime = 2.0f;
        [SerializeField] private float speed = 12f;
        [SerializeField] private float damage = 5f;

        private Rigidbody2D _rb;
        private float _t;

        public void Fire(Vector2 direction)
        {
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            _rb.linearVelocity = direction * speed;
            
            // Rotate projectile to face direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            
            Debug.Log($"[Projectile] Fired! Dir: {direction}, Speed: {speed}, Velocity: {_rb.linearVelocity}");
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
            // Check for Enemy2D
            if (other.TryGetComponent<Enemy2D>(out var enemy))
            {
                var dir = (Vector2)enemy.transform.position - (Vector2)transform.position;
                Debug.Log($"[Projectile] Hit Enemy2D! Dealing {damage} damage");
                enemy.TakeDamage(Mathf.RoundToInt(damage), dir, 4f);
                Destroy(gameObject);
                return;
            }
            
            // Check for RatMiniBoss
            if (other.TryGetComponent<RatMiniBoss>(out var boss))
            {
                var dir = (Vector2)boss.transform.position - (Vector2)transform.position;
                Debug.Log($"[Projectile] Hit RatMiniBoss! Dealing {damage} damage");
                boss.TakeDamage(Mathf.RoundToInt(damage), dir, 4f);
                Destroy(gameObject);
                return;
            }

            // Destroy on any other collision (wall, etc)
            if (!other.CompareTag("Player")) // Không destroy khi chạm player
            {
                Destroy(gameObject);
            }
        }
    }
}


