using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Debug script để hiển thị HP trên đầu Entity (Player/Enemy)
    /// </summary>
    public sealed class DebugHealthDisplay : MonoBehaviour
    {
        [SerializeField] private PlayerHealth2D playerHealth;
        [SerializeField] private Enemy2D enemy;
        [SerializeField] private Vector3 offset = new Vector3(0, 0.5f, 0);
        [SerializeField] private Color healthColor = Color.green;
        [SerializeField] private Color lowHealthColor = Color.red;

        private void OnDrawGizmos()
        {
            Vector3 worldPos = transform.position + offset;
            
            if (playerHealth != null)
            {
                DrawHealthBar(worldPos, playerHealth.CurrentHealth, 100);
                
#if UNITY_EDITOR
                UnityEditor.Handles.Label(worldPos + Vector3.up * 0.3f, 
                    $"HP: {playerHealth.CurrentHealth}/100");
#endif
            }
            else if (enemy != null)
            {
                var health = enemy.GetCurrentHealth();
                var maxHealth = enemy.GetMaxHealth();
                DrawHealthBar(worldPos, health, maxHealth);
                
#if UNITY_EDITOR
                UnityEditor.Handles.Label(worldPos + Vector3.up * 0.3f, 
                    $"HP: {health}/{maxHealth}");
#endif
            }
        }

        private void DrawHealthBar(Vector3 position, int current, int max)
        {
            if (max <= 0) return;
            
            float barWidth = 1f;
            float barHeight = 0.1f;
            float percentage = Mathf.Clamp01((float)current / max);
            
            // Background (black)
            Gizmos.color = Color.black;
            Gizmos.DrawCube(position, new Vector3(barWidth, barHeight, 0));
            
            // Foreground (health)
            Gizmos.color = Color.Lerp(lowHealthColor, healthColor, percentage);
            Gizmos.DrawCube(
                position - Vector3.right * (barWidth * (1 - percentage) / 2),
                new Vector3(barWidth * percentage, barHeight, 0)
            );
        }
    }
}
