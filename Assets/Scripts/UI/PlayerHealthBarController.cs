using UnityEngine;

namespace NWO.UI
{
    /// <summary>
    /// Controller that connects PlayerHealth2D to HealthBarUI
    /// </summary>
    [RequireComponent(typeof(PlayerHealth2D))]
    public sealed class PlayerHealthBarController : MonoBehaviour
    {
        [SerializeField] private HealthBarUI healthBarUI;
        [SerializeField] private bool autoFindHealthBar = true;
        
        private PlayerHealth2D _playerHealth;
        private int _lastHealth;

        private void Awake()
        {
            _playerHealth = GetComponent<PlayerHealth2D>();
            
            if (healthBarUI == null && autoFindHealthBar)
            {
                healthBarUI = FindFirstObjectByType<HealthBarUI>();
            }
            
            if (healthBarUI == null)
            {
                Debug.LogWarning("[PlayerHealthBarController] HealthBarUI not found!");
            }
        }

        private void Start()
        {
            if (healthBarUI != null && _playerHealth != null)
            {
                healthBarUI.Initialize(_playerHealth.CurrentHealth, 100);
                _lastHealth = _playerHealth.CurrentHealth;
            }
        }

        private void Update()
        {
            if (healthBarUI == null || _playerHealth == null) return;
            
            // Only update when health changes
            if (_playerHealth.CurrentHealth != _lastHealth)
            {
                healthBarUI.SetHealth(_playerHealth.CurrentHealth, 100);
                _lastHealth = _playerHealth.CurrentHealth;
            }
        }

        /// <summary>
        /// Manually set the health bar reference
        /// </summary>
        public void SetHealthBar(HealthBarUI healthBar)
        {
            healthBarUI = healthBar;
            if (_playerHealth != null)
            {
                healthBarUI.Initialize(_playerHealth.CurrentHealth, 100);
            }
        }
    }
}
