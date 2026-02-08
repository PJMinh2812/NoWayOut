using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NWO
{
    /// <summary>
    /// PlayerHUD - Quản lý hiển thị Health và Stamina bar cho Player
    /// Sử dụng sprites từ Assets/Art/UI (ValueRed, ValueBlue, etc.)
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [Header("References (Auto-find nếu để trống)")]
        [SerializeField] private PlayerHealth2D healthComponent;
        [SerializeField] private PlayerStamina staminaComponent;

        [Header("Health UI")]
        [Tooltip("Background Image cho health bar (dùng AttributesBar hoặc HealthBarPanel)")]
        [SerializeField] private Image healthBarBackground;
        
        [Tooltip("Image fill cho health bar (dùng ValueRed sprite)")]
        [SerializeField] private Image healthBarFill;
        
        [Tooltip("Text hiển thị HP (optional)")]
        [SerializeField] private TextMeshProUGUI healthText;
        
        [Tooltip("Màu health bar đầy")]
        [SerializeField] private Color fullHealthColor = new Color(0.8f, 0.2f, 0.2f); // Red
        
        [Tooltip("Màu health bar thấp")]
        [SerializeField] private Color lowHealthColor = new Color(0.4f, 0.1f, 0.1f); // Dark red
        
        [Tooltip("Ngưỡng health thấp (%)")]
        [SerializeField] private float lowHealthThreshold = 30f;

        [Header("Stamina UI")]
        [Tooltip("Background Image cho stamina bar (dùng AttributesBar)")]
        [SerializeField] private Image staminaBarBackground;
        
        [Tooltip("Image fill cho stamina bar (dùng ValueBlue sprite)")]
        [SerializeField] private Image staminaBarFill;
        
        [Tooltip("Text hiển thị stamina (optional)")]
        [SerializeField] private TextMeshProUGUI staminaText;
        
        [Tooltip("Màu stamina bar đầy")]
        [SerializeField] private Color fullStaminaColor = new Color(0.3f, 0.8f, 1f); // Blue
        
        [Tooltip("Màu stamina bar thấp")]
        [SerializeField] private Color lowStaminaColor = new Color(1f, 0.5f, 0f); // Orange

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
            // Auto-find components nếu chưa gán
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
                else
                {
                    Debug.LogWarning("[PlayerHUD] Player GameObject not found! Make sure Player has 'Player' tag.");
                }
            }

            // Initialize smooth values
            if (healthComponent != null)
                _currentHealthFill = (float)healthComponent.CurrentHealth / healthComponent.MaxHealth;
            else
                Debug.LogError("[PlayerHUD] PlayerHealth2D component not found!");
            
            if (staminaComponent != null)
                _currentStaminaFill = staminaComponent.StaminaPercent;
            else
                Debug.LogError("[PlayerHUD] PlayerStamina component not found!");
            
            // Validate UI references
            ValidateUIReferences();
        }

        private void Update()
        {
            UpdateHealthUI();
            UpdateStaminaUI();
        }

        /// <summary>
        /// Validate tất cả UI references và log warnings nếu thiếu
        /// </summary>
        private void ValidateUIReferences()
        {
            if (healthBarFill == null)
                Debug.LogWarning("[PlayerHUD] Health Bar Fill Image not assigned! Drag ValueRed sprite here.");
            
            if (staminaBarFill == null)
                Debug.LogWarning("[PlayerHUD] Stamina Bar Fill Image not assigned! Drag ValueBlue sprite here.");
            
            // Background images are optional but recommended
            if (healthBarBackground == null)
                Debug.Log("[PlayerHUD] Health Bar Background not assigned (optional). Use AttributesBar or HealthBarPanel sprite.");
            
            if (staminaBarBackground == null)
                Debug.Log("[PlayerHUD] Stamina Bar Background not assigned (optional). Use AttributesBar sprite.");
        }

        /// <summary>
        /// Update Health bar UI mỗi frame
        /// </summary>
        private void UpdateHealthUI()
        {
            if (healthComponent == null || healthBarFill == null) return;

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

            // Update text nếu có
            if (healthText != null)
            {
                healthText.text = $"{healthComponent.CurrentHealth} / {healthComponent.MaxHealth}";
            }
        }

        /// <summary>
        /// Update Stamina bar UI mỗi frame
        /// </summary>
        private void UpdateStaminaUI()
        {
            if (staminaComponent == null || staminaBarFill == null) return;

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
            staminaBarFill.fillAmount = _currentStaminaFill;
            
            // Update color - dùng color từ PlayerStamina component
            staminaBarFill.color = staminaComponent.GetStaminaBarColor();

            // Update text nếu có
            if (staminaText != null)
            {
                staminaText.text = $"{Mathf.RoundToInt(staminaComponent.CurrentStamina)} / {staminaComponent.MaxStamina}";
            }
        }

        /// <summary>
        /// Set manual references cho health và stamina components
        /// Dùng khi muốn override auto-find
        /// </summary>
        public void SetReferences(PlayerHealth2D health, PlayerStamina stamina)
        {
            healthComponent = health;
            staminaComponent = stamina;
            
            // Re-initialize smooth values
            if (healthComponent != null)
                _currentHealthFill = (float)healthComponent.CurrentHealth / healthComponent.MaxHealth;
            
            if (staminaComponent != null)
                _currentStaminaFill = staminaComponent.StaminaPercent;
        }
    }
}
