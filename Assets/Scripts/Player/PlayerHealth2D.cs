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
        [Tooltip("Delay trước khi disable controller sau khi chết")]
        [SerializeField] private float deathDisableDelay = 0.6f;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsDead { get; private set; }
        public float RegenerationPerSecond => regenerationPerSecond;
        public float InvincibleDuration => invincibleDuration;

        private float _invincibleTimer;
        private PlayerSpellController _spellController;
        private PlayerStatusEffects _statusEffects;
        private PlayerController2D _playerController;
        private int _hurtHash;
        private int _isDeadHash;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            
            if (animator == null)
                animator = GetComponent<Animator>();
            
            _spellController = GetComponent<PlayerSpellController>();
            _statusEffects = GetComponent<PlayerStatusEffects>();
            _playerController = GetComponent<PlayerController2D>();
            
            // Cache animation parameter hashes
            if (animator != null)
            {
                if (!string.IsNullOrEmpty(hurtTrigger))
                    _hurtHash = Animator.StringToHash(hurtTrigger);
                if (!string.IsNullOrEmpty(isDeadParameter))
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

                return;
            }
            
            // === DASH INVINCIBILITY ===
            if (_playerController != null && _playerController.IsDashing)
            {
                return;
            }
            
            if (_statusEffects != null)
            {
                if (_statusEffects.ConsumeShield())
                {
                    _invincibleTimer = invincibleDuration;
                    return;
                }
                
                if (_statusEffects.IsInvincible)
                {
                    return;
                }
            }

            _invincibleTimer = invincibleDuration;
            int healthBeforeDamage = CurrentHealth;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            int damageApplied = Mathf.Max(0, healthBeforeDamage - CurrentHealth);
            RunAIDirectorTelemetry.RecordPlayerDamageTaken(damageApplied);



            // Trigger Hurt animation only when NOT in spell state
            bool isInSpellState = _spellController != null && _spellController.CurrentSpell > 0;
            
            if (animator != null && CurrentHealth > 0 && _hurtHash != 0 && !isInSpellState)
            {
                animator.SetTrigger(_hurtHash);

            }
            else if (isInSpellState)
            {

                StartCoroutine(DamageFlashEffect());
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
        }

        /// <summary>
        /// Đặt HP cụ thể (dùng khi load save game)
        /// </summary>
        public void SetHealth(int value)
        {
            CurrentHealth = Mathf.Clamp(value, 0, maxHealth);
        }

        /// <summary>Hồi máu trực tiếp (dùng cho heart pickup)</summary>
        public void Heal(int amount)
        {
            if (IsDead) return;
            if (amount <= 0) return;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }

        /// <summary>Tăng HP tối đa (upgrade system). Cũng hồi thêm lượng HP tương ứng.</summary>
        public void AddMaxHealth(int amount)
        {
            maxHealth += amount;
            CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        }

        /// <summary>Tăng tốc độ hồi máu (upgrade system)</summary>
        public void AddRegeneration(float amount)
        {
            regenerationPerSecond += amount;
        }

        /// <summary>Tăng/giảm thời gian bất tử sau khi bị đánh (upgrade system)</summary>
        public void AddInvincibleDuration(float amount)
        {
            invincibleDuration = Mathf.Max(0.1f, invincibleDuration + amount);
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;
            RunAIDirectorTelemetry.RecordPlayerDeath();


            if (animator != null)
            {
                animator.SetBool(_isDeadHash, true);

            }
            
            // Disable controller after death animation starts
            StartCoroutine(DisableControlsAfterDeath());
            

            if (GameManager.Instance != null)
            {
                StartCoroutine(TriggerGameOverAfterDelay());
            }
        }

        /// <summary>
        /// Damage flash effect khi bị đánh trong spell state
        /// </summary>
        private System.Collections.IEnumerator DamageFlashEffect()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) yield break;
            
            Color originalColor = spriteRenderer.color;
            
            // Flash red 3 times
            for (int i = 0; i < 3; i++)
            {
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(0.05f);
                spriteRenderer.color = originalColor;
                yield return new WaitForSeconds(0.05f);
            }
        }

        private System.Collections.IEnumerator DisableControlsAfterDeath()
        {
            yield return new WaitForSeconds(deathDisableDelay);
            
            var controller = GetComponent<PlayerController2D>();
            if (controller != null) controller.enabled = false;
            
            var shooter = GetComponent<PlayerShooter2D>();
            if (shooter != null) shooter.enabled = false;
            
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        private System.Collections.IEnumerator TriggerGameOverAfterDelay()
        {
            yield return new WaitForSeconds(deathDisableDelay + 0.5f);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
            }
        }
    }
}
