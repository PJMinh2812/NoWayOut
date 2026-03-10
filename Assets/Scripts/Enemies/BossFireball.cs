using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Fireball do boss bắn ra – gây damage cho Player khi chạm, tự hủy khi va tường.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BossFireball : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float speed    = 6f;
        [SerializeField] private int   damage   = 8;
        [SerializeField] private float knockbackForce = 4f;
        [SerializeField] private float lifetime = 4f;

        private Rigidbody2D _rb;
        private Vector2     _direction;
        private bool        _isDestroyed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale  = 0f;
            _rb.constraints   = RigidbodyConstraints2D.FreezeRotation;
        }

        /// <summary>
        /// Khởi động projectile theo hướng chỉ định.
        /// </summary>
        public void Fire(Vector2 direction, float speedOverride = -1f, int damageOverride = -1)
        {
            if (speedOverride > 0f) speed  = speedOverride;
            if (damageOverride  > 0) damage = damageOverride;

            _direction          = direction.normalized;
            _rb.linearVelocity  = _direction * speed;

            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation  = Quaternion.Euler(0f, 0f, angle);

            Destroy(gameObject, lifetime);
        }

        // ---------- Collision ----------

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isDestroyed) return;

            // Bỏ qua bản thân boss và fireball khác
            if (other.GetComponent<RatMiniBoss>() != null)  return;
            if (other.GetComponent<BossFireball>()  != null) return;

            // Gây damage player
            if (TryDamagePlayer(other.gameObject)) { Explode(); return; }

            // Va tường / tilemap → hủy
            Explode();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isDestroyed) return;

            if (collision.collider.GetComponent<RatMiniBoss>() != null) return;
            if (collision.collider.GetComponent<BossFireball>() != null) return;

            TryDamagePlayer(collision.collider.gameObject);
            Explode();
        }

        // ---------- Helpers ----------

        private bool TryDamagePlayer(GameObject target)
        {
            var ph = target.GetComponent<PlayerHealth2D>();
            if (ph == null) return false;

            Rigidbody2D playerRb = target.GetComponent<Rigidbody2D>();
            ph.TakeDamage(damage, _direction * knockbackForce, playerRb);
            return true;
        }

        private void Explode()
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            _rb.linearVelocity = Vector2.zero;

            Destroy(gameObject);
        }
    }
}
