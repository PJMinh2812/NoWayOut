using UnityEngine;

namespace ProceduralGeneration.Components
{
    /// <summary>
    /// Component đánh dấu spawn point cho enemies trong room prefab
    /// </summary>
    public class EnemySpawnPoint : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [Tooltip("Loại enemy có thể spawn")]
        public EnemyType enemyType = EnemyType.Any;
        
        [Tooltip("Priority của spawn point")]
        [Range(0, 10)]
        public int priority = 5;
        
        [Tooltip("Xác suất spawn enemy tại đây (0-1)")]
        [Range(0f, 1f)]
        public float spawnProbability = 1f;
        
        [Header("Restrictions")]
        [Tooltip("Danger level tối thiểu")]
        public int minDangerLevel = 0;
        
        [Tooltip("Khoảng cách tối thiểu từ cửa")]
        public float minDistanceFromDoor = 2f;
        
        [Header("Behavior")]
        [Tooltip("Enemy có patrol không?")]
        public bool shouldPatrol = true;
        
        [Tooltip("Patrol points (nếu có)")]
        public Transform[] patrolPoints;
        
        [Header("Visual")]
        public bool showGizmo = true;
        public Color gizmoColor = new Color(1f, 0.5f, 0f);
        
        private void OnDrawGizmos()
        {
            if (!showGizmo) return;
            
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, 
                $"Enemy SP\n({enemyType})");
            #endif
            
            // Draw patrol path
            if (shouldPatrol && patrolPoints != null && patrolPoints.Length > 0)
            {
                Gizmos.color = Color.yellow;
                Vector3 prevPoint = transform.position;
                
                foreach (var point in patrolPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawLine(prevPoint, point.position);
                        Gizmos.DrawWireSphere(point.position, 0.2f);
                        prevPoint = point.position;
                    }
                }
                
                // Close the loop
                if (patrolPoints.Length > 0 && patrolPoints[0] != null)
                {
                    Gizmos.DrawLine(prevPoint, transform.position);
                }
            }
        }
    }
    
    /// <summary>
    /// Loại enemy
    /// </summary>
    public enum EnemyType
    {
        Any,        // Bất kỳ
        Melee,      // Cận chiến
        Ranged,     // Tầm xa
        Flying,     // Bay
        Elite,      // Elite/Champion
        Boss        // Boss
    }
}
