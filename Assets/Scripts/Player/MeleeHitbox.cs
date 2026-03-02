using UnityEngine;
using System.Collections.Generic;

namespace NWO
{
    /// <summary>
    /// MeleeHitbox - Component để quản lý vùng đánh melee chi tiết hơn
    /// Có thể gắn lên child object của Player với trigger collider
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MeleeHitbox : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool debugMode = false;
        
        [Header("Damage Override (0 = use controller damage)")]
        [SerializeField] private int damageOverride = 0;
        [SerializeField] private float knockbackOverride = 0f;
        
        private PlayerMeleeController _meleeController;
        private Collider2D _collider;
        private HashSet<Enemy2D> _hitEnemies = new HashSet<Enemy2D>();
        private bool _isActive = false;
        
        // Events
        public System.Action<Enemy2D, int> OnEnemyHit;
        
        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
            
            // Tìm controller ở parent
            _meleeController = GetComponentInParent<PlayerMeleeController>();
            
            // Bắt đầu với hitbox disabled
            _collider.enabled = false;
        }
        
        /// <summary>
        /// Kích hoạt hitbox - gọi từ animation event hoặc code
        /// </summary>
        public void ActivateHitbox()
        {
            _hitEnemies.Clear();
            _collider.enabled = true;
            _isActive = true;
            
            if (debugMode)
            {
                Debug.Log("[MeleeHitbox] Activated!");
            }
        }
        
        /// <summary>
        /// Tắt hitbox
        /// </summary>
        public void DeactivateHitbox()
        {
            _collider.enabled = false;
            _isActive = false;
            _hitEnemies.Clear();
            
            if (debugMode)
            {
                Debug.Log("[MeleeHitbox] Deactivated!");
            }
        }
        
        /// <summary>
        /// Kích hoạt hitbox trong khoảng thời gian nhất định
        /// </summary>
        public void ActivateForDuration(float duration)
        {
            ActivateHitbox();
            Invoke(nameof(DeactivateHitbox), duration);
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isActive) return;
            
            // Bỏ qua Player
            if (other.CompareTag("Player")) return;
            
            // Kiểm tra Enemy
            if (other.TryGetComponent<Enemy2D>(out var enemy))
            {
                // Tránh hit cùng enemy nhiều lần trong một swing
                if (_hitEnemies.Contains(enemy)) return;
                _hitEnemies.Add(enemy);
                
                // Tính damage và knockback
                int damage = damageOverride > 0 ? damageOverride : GetDamageFromController();
                float knockback = knockbackOverride > 0 ? knockbackOverride : GetKnockbackFromController();
                
                // Tính hướng knockback
                Vector2 knockDir = (other.transform.position - transform.position).normalized;
                
                // Gây damage
                enemy.TakeDamage(damage, knockDir, knockback);
                
                // Fire event
                OnEnemyHit?.Invoke(enemy, damage);
                
                if (debugMode)
                {
                    Debug.Log($"[MeleeHitbox] Hit {enemy.name} for {damage} damage!");
                }
            }
        }
        
        private int GetDamageFromController()
        {
            // Có thể mở rộng để lấy damage từ controller dựa trên combo
            return 10; // Default damage
        }
        
        private float GetKnockbackFromController()
        {
            return 4f; // Default knockback
        }
        
        private void OnDrawGizmos()
        {
            if (_collider == null) return;
            
            Gizmos.color = _isActive ? Color.red : Color.yellow;
            
            if (_collider is BoxCollider2D box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
            }
            else if (_collider is CircleCollider2D circle)
            {
                Gizmos.DrawWireSphere(transform.position + (Vector3)circle.offset, circle.radius);
            }
        }
    }
}
