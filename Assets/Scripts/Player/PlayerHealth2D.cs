using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Quáº£n lÃ½ HP, regeneration vÃ  animation damage/death cho Player
    /// </summary>
    public sealed class PlayerHealth2D : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float regenerationPerSecond = 0.5f;
        [SerializeField] private float invincibleDuration = 0.5f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        
        [Tooltip("TÃªn parameter trigger cho animation bá»‹ damage")]
        [SerializeField] private string hurtTrigger = "Hurt";
        
        [Tooltip("TÃªn parameter bool cho tráº¡ng thÃ¡i cháº¿t")]
        [SerializeField] private string isDeadParameter = "isDead";
        
        [Header("Death Settings")]
        [Tooltip("Delay trÆ°á»›c khi disable controller sau khi cháº¿t (Ä‘á»ƒ animation death cháº¡y)")]
        [SerializeField] private float deathDisableDelay = 0.6f;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsDead { get; private set; }

        private float _invincibleTimer;
        private PlayerSpellController _spellController;
        
        // Animation parameter hashes (tá»‘i Æ°u performance)
        private int _hurtHash;
        private int _isDeadHash;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            
            // Auto-find animator náº¿u chÆ°a gÃ¡n
            if (animator == null)
                animator = GetComponent<Animator>();
            
            // Get spell controller reference
            _spellController = GetComponent<PlayerSpellController>();
            
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

            _invincibleTimer = invincibleDuration;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);



            // â­ TRIGGER ANIMATION DAMAGE (CHá»ˆ khi KHÃ”NG á»Ÿ spell state)
            // Náº¿u Ä‘ang á»Ÿ spell state (1/2/3), giá»¯ nguyÃªn animation, khÃ´ng trigger Hurt
            bool isInSpellState = _spellController != null && _spellController.CurrentSpell > 0;
            
            if (animator != null && CurrentHealth > 0 && _hurtHash != 0 && !isInSpellState)
            {
                animator.SetTrigger(_hurtHash);

            }
            else if (isInSpellState)
            {
                // Äang á»Ÿ spell state, giá»¯ nguyÃªn spell animation, chá»‰ hiá»ƒn thá»‹ damage effect

                // TODO: ThÃªm damage flash effect á»Ÿ Ä‘Ã¢y (change sprite color briefly)
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

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;

            
            // â­ TRIGGER ANIMATION DEATH
            if (animator != null)
            {
                animator.SetBool(_isDeadHash, true);

            }
            
            // Disable controller/shooter SAU khi animation death báº¯t Ä‘áº§u
            // Ä‘á»ƒ animation cÃ³ thá»i gian cháº¡y
            StartCoroutine(DisableControlsAfterDeath());
            
            // Trigger Game Over sau delay Ä‘á»ƒ tháº¥y animation
            if (GameManager.Instance != null)
            {
                StartCoroutine(TriggerGameOverAfterDelay());
            }
        }

        /// <summary>
        /// Hiá»ƒn thá»‹ damage flash effect khi bá»‹ Ä‘Ã¡nh trong spell state (khÃ´ng trigger Hurt animation)
        /// </summary>
        private System.Collections.IEnumerator DamageFlashEffect()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) yield break;
            
            Color originalColor = spriteRenderer.color;
            
            // Flash Ä‘á» 3 láº§n nhanh
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
            // Chá» animation death cháº¡y
            yield return new WaitForSeconds(deathDisableDelay);
            
            var controller = GetComponent<PlayerController2D>();
            if (controller != null) controller.enabled = false;
            
            var shooter = GetComponent<PlayerShooter2D>();
            if (shooter != null) shooter.enabled = false;
            
            // Dá»«ng velocity Ä‘á»ƒ khÃ´ng trÆ°á»£t
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            

        }

        private System.Collections.IEnumerator TriggerGameOverAfterDelay()
        {
            // Delay Ä‘á»ƒ xem animation death
            yield return new WaitForSeconds(deathDisableDelay + 0.5f);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
            }
        }
    }
}


