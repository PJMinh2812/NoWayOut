using UnityEngine;
using UnityEngine.Events;

namespace SoulKnightClone.Player
{
    /// <summary>
    /// Quản lý các chỉ số của Player: Health, Armor, Energy
    /// Armor tự hồi phục sau khoảng thời gian không nhận sát thương
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;

        [Header("Armor (Auto-Regenerating)")]
        [SerializeField] private int maxArmor = 50;
        [SerializeField] private int currentArmor;
        [SerializeField] private float armorRegenDelay = 3f; // Thời gian chờ trước khi hồi giáp
        [SerializeField] private float armorRegenRate = 10f; // Giáp hồi mỗi giây
        private float lastDamageTime;
        private bool isRegeneratingArmor = false;

        [Header("Energy")]
        [SerializeField] private int maxEnergy = 200;
        [SerializeField] private int currentEnergy;
        [SerializeField] private float energyRegenRate = 20f; // Energy hồi mỗi giây

        [Header("Invincibility (I-Frames)")]
        [SerializeField] private bool isInvincible = false;
        private float invincibilityTimer = 0f;

        // Events
        [System.Serializable]
        public class StatsEvent : UnityEvent<int, int> { } // current, max

        public StatsEvent OnHealthChanged;
        public StatsEvent OnArmorChanged;
        public StatsEvent OnEnergyChanged;
        public UnityEvent OnPlayerDeath;
        public UnityEvent OnDamageTaken;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            // Initialize stats
            currentHealth = maxHealth;
            currentArmor = maxArmor;
            currentEnergy = maxEnergy;
        }

        private void Start()
        {
            UpdateUI();
        }

        private void Update()
        {
            HandleArmorRegeneration();
            HandleEnergyRegeneration();
            HandleInvincibility();
        }

        #region Armor System
        private void HandleArmorRegeneration()
        {
            if (currentArmor < maxArmor && Time.time - lastDamageTime >= armorRegenDelay)
            {
                if (!isRegeneratingArmor)
                {
                    isRegeneratingArmor = true;
                }

                currentArmor += Mathf.CeilToInt(armorRegenRate * Time.deltaTime);
                currentArmor = Mathf.Min(currentArmor, maxArmor);
                OnArmorChanged?.Invoke(currentArmor, maxArmor);
            }
        }
        #endregion

        #region Energy System
        private void HandleEnergyRegeneration()
        {
            if (currentEnergy < maxEnergy)
            {
                currentEnergy += Mathf.CeilToInt(energyRegenRate * Time.deltaTime);
                currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
                OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            }
        }

        public bool ConsumeEnergy(int amount)
        {
            if (currentEnergy >= amount)
            {
                currentEnergy -= amount;
                OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
                return true;
            }
            return false;
        }
        #endregion

        #region Damage System
        public void TakeDamage(int damage)
        {
            if (isInvincible || currentHealth <= 0)
                return;

            isRegeneratingArmor = false;
            lastDamageTime = Time.time;

            // Giáp hấp thụ sát thương trước
            int remainingDamage = damage;

            if (currentArmor > 0)
            {
                int armorDamage = Mathf.Min(currentArmor, remainingDamage);
                currentArmor -= armorDamage;
                remainingDamage -= armorDamage;
                OnArmorChanged?.Invoke(currentArmor, maxArmor);
            }

            // Sát thương còn lại trừ vào máu
            if (remainingDamage > 0)
            {
                currentHealth -= remainingDamage;
                currentHealth = Mathf.Max(currentHealth, 0);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }

            OnDamageTaken?.Invoke();

            // Visual feedback
            StartCoroutine(DamageFlash());

            // Check death
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private System.Collections.IEnumerator DamageFlash()
        {
            if (spriteRenderer != null)
            {
                Color originalColor = spriteRenderer.color;
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = originalColor;
            }
        }

        public void Heal(int amount)
        {
            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void RestoreArmor(int amount)
        {
            currentArmor += amount;
            currentArmor = Mathf.Min(currentArmor, maxArmor);
            OnArmorChanged?.Invoke(currentArmor, maxArmor);
        }
        #endregion

        #region Invincibility (I-Frames)
        public void SetInvincible(float duration)
        {
            isInvincible = true;
            invincibilityTimer = duration;
        }

        private void HandleInvincibility()
        {
            if (isInvincible)
            {
                invincibilityTimer -= Time.deltaTime;
                if (invincibilityTimer <= 0)
                {
                    isInvincible = false;
                }
            }
        }
        #endregion

        #region Death
        private void Die()
        {
            OnPlayerDeath?.Invoke();
            Debug.Log("Player Died!");
            // TODO: Trigger death animation, game over screen
        }
        #endregion

        private void UpdateUI()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnArmorChanged?.Invoke(currentArmor, maxArmor);
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        }

        // Getters
        public int GetCurrentHealth() => currentHealth;
        public int GetMaxHealth() => maxHealth;
        public int GetCurrentArmor() => currentArmor;
        public int GetCurrentEnergy() => currentEnergy;
        public bool IsInvincible() => isInvincible;
    }
}
