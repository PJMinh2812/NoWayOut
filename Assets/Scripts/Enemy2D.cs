using UnityEngine;

namespace GloomCraft
{
    /// <summary>
    /// Bản C# rút gọn từ global.Enemy: di chuyển đuổi theo Player và gây damage + knockback khi va chạm.
    /// Pathfinding chi tiết sẽ port sau; tạm thời là chase trực tiếp.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Enemy2D : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private int maxHealth = 20;
        [SerializeField] private Vector2Int damageRange = new(4, 8);
        [SerializeField] private float knockbackStrength = 4f;
        [SerializeField] private float moveAcceleration = 18f;
        [SerializeField] private float maxMoveSpeed = 3.5f;
        [SerializeField] private float friction = 8f;

        [Header("AI")]
        [SerializeField] private float aggroRadius = 8f;

        private int _currentHealth;
        private Rigidbody2D _rb;
        private PlayerController2D _player;
        private PlayerHealth2D _playerHealth;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _player = FindFirstObjectByType<PlayerController2D>();
            if (_player != null) _playerHealth = _player.GetComponent<PlayerHealth2D>();
            _currentHealth = maxHealth;
        }

        private void FixedUpdate()
        {
            _rb.linearDamping = friction;

            if (_player == null) return;

            var toPlayer = (Vector2)(_player.transform.position - transform.position);
            var dist = toPlayer.magnitude;
            if (dist > aggroRadius) return;

            if (dist > 0.1f)
            {
                var dir = toPlayer / dist;
                _rb.AddForce(dir * moveAcceleration, ForceMode2D.Force);

                var v = _rb.linearVelocity;
                if (v.magnitude > maxMoveSpeed)
                {
                    _rb.linearVelocity = v.normalized * maxMoveSpeed;
                }
            }
        }

        public void TakeDamage(int amount, Vector2 hitDirection, float knockbackPower)
        {
            if (amount <= 0) return;
            _currentHealth = Mathf.Max(0, _currentHealth - amount);

            _rb.AddForce(hitDirection.normalized * knockbackPower, ForceMode2D.Impulse);

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Destroy(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_player == null || _playerHealth == null) return;
            if (!collision.collider.TryGetComponent<PlayerController2D>(out var playerController)) return;

            var dmg = Random.Range(damageRange.x, damageRange.y + 1);
            var dir = (Vector2)(playerController.transform.position - transform.position);
            var knock = dir.normalized * knockbackStrength;

            var rb = playerController.GetComponent<Rigidbody2D>();
            _playerHealth.TakeDamage(dmg, knock, rb);
        }
    }
}


