using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NWO
{
    /// <summary>
    /// PLAYER HUD - UI hiển thị Health và Stamina
    /// 
    /// HIỂN THỊ:
    /// 1. Health Bar - Thanh máu (hearts hoặc bar)
    /// 2. Stamina Bar - Thanh năng lượng
    /// 3. Current values (số)
    /// 
    /// AUTO-SETUP:
    /// - Tự động tìm PlayerHealth2D và PlayerStamina
    /// - Cập nhật real-time
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [Header("References (Auto-find nếu để trống)")]
        [SerializeField] private PlayerHealth2D healthComponent;
        [SerializeField] private PlayerStamina staminaComponent;

        [Header("Health UI")]
        [Tooltip("Image fill cho health bar")]
        [SerializeField] private Image healthBarFill;
        
        [Tooltip("Text hiển thị HP (optional)")]
        [SerializeField] private TextMeshProUGUI healthText;
        
        [Tooltip("Màu health bar đầy")]
        [SerializeField] private Color fullHealthColor = Color.green;
        
        [Tooltip("Màu health bar thấp")]
        [SerializeField] private Color lowHealthColor = Color.red;
        
        [Tooltip("Ngưỡng health thấp (%)")]
        [SerializeField] private float lowHealthThreshold = 30f;

        [Header("Stamina UI")]
        [Tooltip("Image fill cho stamina bar")]
        [SerializeField] private Image staminaBarFill;
        
        [Tooltip("Text hiển thị stamina (optional)")]
        [SerializeField] private TextMeshProUGUI staminaText;

        [Header("Animation")]
        [Tooltip("Smooth lerp cho bars")]
        [SerializeField] private bool smoothBars = true;
        
        [Tooltip("Tốc độ lerp")]
        [SerializeField] private float lerpSpeed = 10f;

        // Smooth values
        private float _currentHealthFill;
        private float _currentStaminaFill;

        private void Awake()
        {
            // Auto-find components
            if (healthComponent == null || staminaComponent == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    if (healthComponent == null)
                        healthComponent = player.GetComponent<PlayerHealth2D>();
                    
                    if (staminaComponent == null)
                        staminaComponent = player.GetComponent<PlayerStamina>();
                }
            }

            // Initialize smooth values
            if (healthComponent != null)
                _currentHealthFill = (float)healthComponent.CurrentHealth / healthComponent.MaxHealth;
            
            if (staminaComponent != null)
                _currentStaminaFill = staminaComponent.StaminaPercent;
        }

        private void Update()
        {
            UpdateHealthUI();
            UpdateStaminaUI();
        }

        /// <summary>
        /// Cập nhật UI health bar
        /// </summary>
        private void UpdateHealthUI()
        {
            if (healthComponent == null) return;

            float targetFill = (float)healthComponent.CurrentHealth / healthComponent.MaxHealth;

            // Smooth lerp hoặc instant
            if (smoothBars)
            {
                _currentHealthFill = Mathf.Lerp(_currentHealthFill, targetFill, Time.deltaTime * lerpSpeed);
            }
            else
            {
                _currentHealthFill = targetFill;
            }

            // Update fill amount
            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = _currentHealthFill;

                // Color based on health %
                float healthPercent = _currentHealthFill * 100f;
                if (healthPercent <= lowHealthThreshold)
                {
                    healthBarFill.color = lowHealthColor;
                }
                else
                {
                    // Lerp từ low → full color
                    float t = (healthPercent - lowHealthThreshold) / (100f - lowHealthThreshold);
                    healthBarFill.color = Color.Lerp(lowHealthColor, fullHealthColor, t);
                }
            }

            // Update text
            if (healthText != null)
            {
                healthText.text = $"{healthComponent.CurrentHealth} / {healthComponent.MaxHealth}";
            }
        }

        /// <summary>
        /// Cập nhật UI stamina bar
        /// </summary>
        private void UpdateStaminaUI()
        {
            if (staminaComponent == null) return;

            float targetFill = staminaComponent.StaminaPercent;

            // Smooth lerp hoặc instant
            if (smoothBars)
            {
                _currentStaminaFill = Mathf.Lerp(_currentStaminaFill, targetFill, Time.deltaTime * lerpSpeed);
            }
            else
            {
                _currentStaminaFill = targetFill;
            }

            // Update fill amount
            if (staminaBarFill != null)
            {
                staminaBarFill.fillAmount = _currentStaminaFill;
                staminaBarFill.color = staminaComponent.GetStaminaBarColor();
            }

            // Update text
            if (staminaText != null)
            {
                staminaText.text = $"{Mathf.RoundToInt(staminaComponent.CurrentStamina)} / {staminaComponent.MaxStamina}";
            }
        }

        // === PUBLIC METHODS ===

        /// <summary>
        /// Set references manually (nếu cần)
        /// </summary>
        public void SetReferences(PlayerHealth2D health, PlayerStamina stamina)
        {
            healthComponent = health;
            staminaComponent = stamina;
        }
    }
}
