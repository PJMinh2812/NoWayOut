using UnityEngine;
using UnityEngine.UI;

namespace NWO.UI
{
    /// <summary>
    /// Mana bar UI - similar to health bar but with different color scheme
    /// </summary>
    public sealed class ManaBarUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Text manaText; // Optional
        
        [Header("Visual Settings")]
        [SerializeField] private Color manaColor = new Color(0.2f, 0.4f, 0.9f); // Blue
        [SerializeField] private bool smoothTransition = true;
        [SerializeField] private float transitionSpeed = 5f;
        
        private float _targetFillAmount;
        private float _currentFillAmount;

        private void Awake()
        {
            if (fillImage == null)
            {
                Debug.LogError("[ManaBarUI] Fill Image not assigned!");
            }
            else
            {
                fillImage.color = manaColor;
            }
        }

        private void Update()
        {
            if (!smoothTransition || fillImage == null) return;
            
            _currentFillAmount = Mathf.Lerp(_currentFillAmount, _targetFillAmount, Time.deltaTime * transitionSpeed);
            fillImage.fillAmount = _currentFillAmount;
        }

        public void SetMana(int current, int max)
        {
            if (max <= 0) return;
            
            float percentage = Mathf.Clamp01((float)current / max);
            _targetFillAmount = percentage;
            
            if (!smoothTransition && fillImage != null)
            {
                fillImage.fillAmount = percentage;
                _currentFillAmount = percentage;
            }
            
            if (manaText != null)
            {
                manaText.text = $"{current}/{max}";
            }
        }

        public void Initialize(int current, int max)
        {
            if (max <= 0) return;
            
            float percentage = Mathf.Clamp01((float)current / max);
            _targetFillAmount = percentage;
            _currentFillAmount = percentage;
            
            if (fillImage != null)
            {
                fillImage.fillAmount = percentage;
            }
            
            if (manaText != null)
            {
                manaText.text = $"{current}/{max}";
            }
        }
    }
}
