using UnityEngine;

namespace NWO.UI
{
    /// <summary>
    /// World-space health bar that follows an enemy
    /// Attach this to the health bar UI object, not the enemy
    /// </summary>
    public sealed class EnemyHealthBarController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HealthBarUI healthBarUI;
        [SerializeField] private Enemy2D targetEnemy;
        
        [Header("World Position Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 0.5f, 0);
        [SerializeField] private bool hideWhenFullHealth = true;
        [SerializeField] private bool alwaysFaceCamera = true;
        
        private Camera _mainCamera;
        private int _lastHealth;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _mainCamera = Camera.main;
            
            if (healthBarUI == null)
            {
                healthBarUI = GetComponent<HealthBarUI>();
            }
            
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null && hideWhenFullHealth)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            if (targetEnemy != null && healthBarUI != null)
            {
                int maxHealth = targetEnemy.GetMaxHealth();
                healthBarUI.Initialize(maxHealth, maxHealth);
                _lastHealth = maxHealth;
                
                if (hideWhenFullHealth && _canvasGroup != null)
                {
                    _canvasGroup.alpha = 0;
                }
            }
        }

        private void LateUpdate()
        {
            if (targetEnemy == null)
            {
                Destroy(gameObject);
                return;
            }
            
            // Update position to follow enemy
            transform.position = targetEnemy.transform.position + offset;
            
            // Face camera
            if (alwaysFaceCamera && _mainCamera != null)
            {
                transform.rotation = _mainCamera.transform.rotation;
            }
            
            // Update health value
            int currentHealth = targetEnemy.GetCurrentHealth();
            if (currentHealth != _lastHealth)
            {
                int maxHealth = targetEnemy.GetMaxHealth();
                healthBarUI?.SetHealth(currentHealth, maxHealth);
                _lastHealth = currentHealth;
                
                // Show/hide based on health
                if (hideWhenFullHealth && _canvasGroup != null)
                {
                    _canvasGroup.alpha = currentHealth >= maxHealth ? 0 : 1;
                }
            }
        }

        /// <summary>
        /// Set the enemy to track
        /// </summary>
        public void SetTarget(Enemy2D enemy)
        {
            targetEnemy = enemy;
            if (enemy != null && healthBarUI != null)
            {
                int maxHealth = enemy.GetMaxHealth();
                healthBarUI.Initialize(maxHealth, maxHealth);
                _lastHealth = maxHealth;
            }
        }
    }
}
