using UnityEngine;
using UnityEngine.UI;

namespace NWO.UI
{
    /// <summary>
    /// UI Health Bar component - works with both HUD (screen space) and world space health bars
    /// </summary>
    public sealed class HealthBarUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text healthText; // Optional
        
        [Header("Visual Settings")]
        [SerializeField] private Color fullHealthColor = new Color(0.2f, 0.8f, 0.2f); // Green
        [SerializeField] private Color lowHealthColor = new Color(0.8f, 0.2f, 0.2f); // Red
        [SerializeField] private float lowHealthThreshold = 0.3f; // 30%
        [SerializeField] private bool smoothTransition = true;
        [SerializeField] private float transitionSpeed = 5f;
        
        private float _targetFillAmount;
        private float _currentFillAmount;

        private void Awake()
        {
            if (fillImage == null)
            {
                Debug.LogError("[HealthBarUI] Fill Image not assigned!");
            }
        }

        private void Update()
        {
            if (!smoothTransition || fillImage == null) return;
            
            // Smooth transition
            _currentFillAmount = Mathf.Lerp(_currentFillAmount, _targetFillAmount, Time.deltaTime * transitionSpeed);
            fillImage.fillAmount = _currentFillAmount;
        }

        /// <summary>
        /// Update health bar value
        /// </summary>
        public void SetHealth(int current, int max)
        {
            if (max <= 0) return;
            
            float percentage = Mathf.Clamp01((float)current / max);
            _targetFillAmount = percentage;
            
            if (!smoothTransition && fillImage != null)
            {
                fillImage.fillAmount = percentage;
                _currentFillAmount = percentage;
            }
            
            // Update color based on health percentage
            if (fillImage != null)
            {
                fillImage.color = percentage <= lowHealthThreshold 
                    ? lowHealthColor 
                    : Color.Lerp(lowHealthColor, fullHealthColor, (percentage - lowHealthThreshold) / (1f - lowHealthThreshold));
            }
            
            // Update text if available
            if (healthText != null)
            {
                healthText.text = $"{current}/{max}";
            }
        }

        /// <summary>
        /// Initialize health bar (sets immediately without transition)
        /// </summary>
        public void Initialize(int current, int max)
        {
            if (max <= 0) return;
            
            float percentage = Mathf.Clamp01((float)current / max);
            _targetFillAmount = percentage;
            _currentFillAmount = percentage;
            
            if (fillImage != null)
            {
                fillImage.fillAmount = percentage;
                fillImage.color = percentage <= lowHealthThreshold ? lowHealthColor : fullHealthColor;
            }
            
            if (healthText != null)
            {
                healthText.text = $"{current}/{max}";
            }
        }
    }
}
