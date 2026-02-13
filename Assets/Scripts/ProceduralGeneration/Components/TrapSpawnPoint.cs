using UnityEngine;
using ProceduralGeneration.Data;

namespace ProceduralGeneration.Components
{
    /// <summary>
    /// Component đánh dấu một spawn point cho traps trong room prefab
    /// </summary>
    public class TrapSpawnPoint : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [Tooltip("Priority của spawn point (cao hơn = ưu tiên hơn)")]
        [Range(0, 10)]
        public int priority = 5;
        
        [Tooltip("Spawn logic ưu tiên cho point này")]
        public TrapSpawnLogic preferredLogic = TrapSpawnLogic.Random;
        
        [Tooltip("Có thể chặn đường đi chính không?")]
        public bool canBlockPath = false;
        
        [Header("Restrictions")]
        [Tooltip("Trap types có thể spawn tại đây")]
        public TrapSpawnRestriction restriction = TrapSpawnRestriction.Any;
        
        [Tooltip("Danger level tối thiểu")]
        public int minDangerLevel = 0;
        
        [Header("Visual")]
        [Tooltip("Hiển thị gizmo trong Scene view")]
        public bool showGizmo = true;
        
        [Tooltip("Màu gizmo")]
        public Color gizmoColor = Color.red;
        
        private void OnDrawGizmos()
        {
            if (!showGizmo) return;
            
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            
            // Draw priority
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
                $"Trap SP (P:{priority})");
            #endif
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!showGizmo) return;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
    
    /// <summary>
    /// Restriction cho trap spawning
    /// </summary>
    public enum TrapSpawnRestriction
    {
        Any,            // Bất kỳ trap nào
        GroundOnly,     // Chỉ trap trên mặt đất
        WallOnly,       // Chỉ trap trên tường
        CeilingOnly,    // Chỉ trap trên trần
        Environmental   // Chỉ trap môi trường
    }
}
