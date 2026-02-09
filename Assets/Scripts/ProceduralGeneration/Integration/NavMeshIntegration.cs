using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProceduralGeneration.Integration
{
    /// <summary>
    /// Integration với NavMesh (hỗ trợ NavMeshPlus cho 2D)
    /// Tự động bake NavMesh sau khi dungeon được generate
    /// </summary>
    public class NavMeshIntegration : MonoBehaviour
    {
        [Header("NavMesh Settings")]
        [Tooltip("Tự động bake NavMesh sau khi generate")]
        public bool autoBake = true;
        
        [Tooltip("NavMesh Surface component (NavMeshPlus)")]
        public Component navMeshSurface;
        
        [Header("Layer Settings")]
        [Tooltip("Layer cho walkable surfaces")]
        public LayerMask walkableLayer;
        
        [Tooltip("Layer cho obstacles")]
        public LayerMask obstacleLayer;
        
        [Header("Debug")]
        public bool showDebugInfo = true;
        
        /// <summary>
        /// Bake NavMesh
        /// </summary>
        public void BakeNavMesh()
        {
            if (!autoBake)
            {
                Debug.Log("Auto bake is disabled");
                return;
            }
            
            // Kiểm tra NavMeshPlus (using reflection để không phụ thuộc vào package)
            if (navMeshSurface != null)
            {
                var buildMethod = navMeshSurface.GetType().GetMethod("BuildNavMesh");
                if (buildMethod != null)
                {
                    buildMethod.Invoke(navMeshSurface, null);
                    Debug.Log("<color=green>NavMesh baked successfully!</color>");
                }
                else
                {
                    Debug.LogWarning("BuildNavMesh method not found on NavMeshSurface");
                }
            }
            else
            {
                Debug.LogWarning("NavMeshSurface not assigned. Please install NavMeshPlus and assign the component.");
            }
        }
        
        /// <summary>
        /// Clear NavMesh
        /// </summary>
        public void ClearNavMesh()
        {
            if (navMeshSurface != null)
            {
                var clearMethod = navMeshSurface.GetType().GetMethod("RemoveData");
                if (clearMethod != null)
                {
                    clearMethod.Invoke(navMeshSurface, null);
                    Debug.Log("NavMesh cleared");
                }
            }
        }
        
        /// <summary>
        /// Setup static flags cho các objects
        /// </summary>
        public void SetupStaticObjects(Transform dungeonContainer)
        {
            if (dungeonContainer == null) return;
            
            int staticCount = 0;
            
            // Tìm tất cả renderers
            Renderer[] renderers = dungeonContainer.GetComponentsInChildren<Renderer>();
            
            foreach (var renderer in renderers)
            {
                // Check nếu là static geometry (walls, floors, etc.)
                if (IsStaticObject(renderer.gameObject))
                {
                    // Set static flags
                    #if UNITY_EDITOR
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, 
                        StaticEditorFlags.ContributeGI | 
                        StaticEditorFlags.OccluderStatic | 
                        StaticEditorFlags.BatchingStatic);
                    #endif
                    
                    staticCount++;
                }
            }
            
            if (showDebugInfo)
                Debug.Log($"Set {staticCount} objects as static");
        }
        
        /// <summary>
        /// Kiểm tra xem object có phải static không
        /// </summary>
        private bool IsStaticObject(GameObject obj)
        {
            string name = obj.name.ToLower();
            
            // Check keywords
            return name.Contains("wall") || 
                   name.Contains("floor") || 
                   name.Contains("ceiling") ||
                   name.Contains("ground") ||
                   name.Contains("static");
        }
    }
}
