using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Quản lý HP, regeneration và animation damage/death cho Player
    /// </summary>
    public sealed class PlayerHealth2D : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float regenerationPerSecond = 0.5f;
        [SerializeField] private float invincibleDuration = 0.5f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        
        [Tooltip("Tên parameter trigger cho animation bị damage")]
        [SerializeField] private string hurtTrigger = "Hurt";
        
        [Tooltip("Tên parameter bool cho trạng thái chết")]
        [SerializeField] private string isDeadParameter = "isDead";
        
        [Header("Death Settings")]
        [Tooltip("Delay trước khi disable controller sau khi chết (để animation death chạy)")]
        [SerializeField] private float deathDisableDelay = 0.6f;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsDead { get; private set; }

        private float _invincibleTimer;
        
        // Animation parameter hashes (tối ưu performance)
        private int _hurtHash;
        private int _isDeadHash;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            
            // Auto-find animator nếu chưa gán
            if (animator == null)
                animator = GetComponent<Animator>();
            
            // Cache animation parameter hashes
            if (animator != null)
            {
                _hurtHash = Animator.StringToHash(hurtTrigger);
                _isDeadHash = Animator.StringToHash(isDeadParameter);
            }
            else
            {
                Debug.LogWarning("[PlayerHealth2D] Animator not found! Damage/death animations will not play.");
            }
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

            // ⭐ TRIGGER ANIMATION DAMAGE
            if (animator != null && CurrentHealth > 0)
            {
                animator.SetTrigger(_hurtHash);
                Debug.Log("[PlayerHealth2D] Playing Hurt animation");
            }

            // Apply knockback
            if (rb != null)
            {
                rb.AddForce(knockback, ForceMode2D.Impulse);
            }

            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            IsDead = false;
            _invincibleTimer = 0f;
            
            // Reset animation state
            if (animator != null)
            {
                animator.SetBool(_isDeadHash, false);
            }
            
            var controller = GetComponent<PlayerController2D>();
            if (controller != null) controller.enabled = true;
            
            Debug.Log("[Player] Health reset!");
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;
            Debug.Log("[Player] DIED!");
            
            // ⭐ TRIGGER ANIMATION DEATH
            if (animator != null)
            {
                animator.SetBool(_isDeadHash, true);
                Debug.Log("[PlayerHealth2D] Playing Death animation");
            }
            
            // Disable controller/shooter SAU khi animation death bắt đầu
            // để animation có thời gian chạy
            StartCoroutine(DisableControlsAfterDeath());
            
            // Trigger Game Over sau delay để thấy animation
            if (GameManager.Instance != null)
            {
                StartCoroutine(TriggerGameOverAfterDelay());
            }
        }

        private System.Collections.IEnumerator DisableControlsAfterDeath()
        {
            // Chờ animation death chạy
            yield return new WaitForSeconds(deathDisableDelay);
            
            var controller = GetComponent<PlayerController2D>();
            if (controller != null) controller.enabled = false;
            
            var shooter = GetComponent<PlayerShooter2D>();
            if (shooter != null) shooter.enabled = false;
            
            // Dừng velocity để không trượt
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            
            Debug.Log("[PlayerHealth2D] Controls disabled after death animation");
        }

        private System.Collections.IEnumerator TriggerGameOverAfterDelay()
        {
            // Delay để xem animation death
            yield return new WaitForSeconds(deathDisableDelay + 0.5f);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
            }
        }
    }
}


