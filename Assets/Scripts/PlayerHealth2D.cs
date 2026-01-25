using UnityEngine;

namespace GloomCraft
{
    /// <summary>
    /// Đơn giản hóa hệ thống Bar/HP của microStudio thành component sức khỏe cho Player.
    /// </summary>
    public sealed class PlayerHealth2D : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float regenerationPerSecond = 0.5f;
        [SerializeField] private float invincibleDuration = 0.5f;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsDead { get; private set; }

        private float _invincibleTimer;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        private void Update()
        {
            if (IsDead) return;

            if (_invincibleTimer > 0f) _invincibleTimer -= Time.deltaTime;

            if (regenerationPerSecond > 0f && CurrentHealth > 0 && CurrentHealth < maxHealth)
            {
                var regen = regenerationPerSecond * Time.deltaTime;
                var newValue = Mathf.Min(maxHealth, CurrentHealth + regen);
                CurrentHealth = Mathf.RoundToInt(newValue);
            }
        }

        public void TakeDamage(int amount, Vector2 knockback, Rigidbody2D rb)
        {
            if (IsDead) return;
            if (amount <= 0) return;
            if (_invincibleTimer > 0f)
            {
                Debug.Log("[Player] Still invincible!");
                return;
            }

            _invincibleTimer = invincibleDuration;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

            Debug.Log($"[Player] Took {amount} damage! HP: {CurrentHealth}/{maxHealth}");

            if (rb != null)
            {
                rb.AddForce(knockback, ForceMode2D.Impulse);
            }

            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;
            Debug.Log("[Player] DIED!");
            // Sau này có thể gọi GameStateMachine để chuyển sang GameOver.
        }
    }
}


