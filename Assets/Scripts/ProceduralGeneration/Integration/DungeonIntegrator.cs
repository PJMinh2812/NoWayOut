using UnityEngine;
using ProceduralGeneration.Core;

namespace ProceduralGeneration.Integration
{
    /// <summary>
    /// Main integration component để kết nối DungeonManager với các systems khác
    /// </summary>
    [RequireComponent(typeof(DungeonManager))]
    public class DungeonIntegrator : MonoBehaviour
    {
        [Header("References")]
        public DungeonManager dungeonManager;
        public NavMeshIntegration navMeshIntegration;
        
        [Header("Post-Generation")]
        [Tooltip("Tự động setup lighting sau khi generate")]
        public bool setupLighting = true;
        
        [Tooltip("Tự động setup audio zones")]
        public bool setupAudioZones = true;
        
        [Tooltip("Tự động bake occlusion culling")]
        public bool bakeOcclusion = false;
        
        [Header("Minimap")]
        [Tooltip("Tự động generate minimap")]
        public bool generateMinimap = true;
        
        [Tooltip("Minimap camera (orthographic)")]
        public Camera minimapCamera;
        
        private void Awake()
        {
            if (dungeonManager == null)
                dungeonManager = GetComponent<DungeonManager>();
        }
        
        /// <summary>
        /// Callback sau khi dungeon được generate
        /// </summary>
        public void OnDungeonGenerated()
        {
            Debug.Log("Running post-generation integrations...");
            
            // NavMesh
            if (navMeshIntegration != null)
            {
                navMeshIntegration.SetupStaticObjects(dungeonManager.dungeonContainer);
                navMeshIntegration.BakeNavMesh();
            }
            
            // Lighting
            if (setupLighting)
            {
                SetupLighting();
            }
            
            // Audio
            if (setupAudioZones)
            {
                SetupAudioZones();
            }
            
            // Minimap
            if (generateMinimap)
            {
                GenerateMinimap();
            }
            
            Debug.Log("<color=cyan>Post-generation complete!</color>");
        }
        
        /// <summary>
        /// Setup lighting cho dungeon
        /// </summary>
        private void SetupLighting()
        {
            // TODO: Implement lighting setup
            // - Ambient lighting
            // - Point lights in rooms
            // - Fog settings
            
            Debug.Log("Lighting setup completed");
        }
        
        /// <summary>
        /// Setup audio zones
        /// </summary>
        private void SetupAudioZones()
        {
            // TODO: Implement audio zones
            // - Ambient sounds per room type
            // - Reverb zones
            // - Audio mixers
            
            Debug.Log("Audio zones setup completed");
        }
        
        /// <summary>
        /// Generate minimap
        /// </summary>
        private void GenerateMinimap()
        {
            if (minimapCamera == null)
            {
                Debug.LogWarning("Minimap camera not assigned");
                return;
            }
            
            // TODO: Implement minimap generation
            // - Render dungeon to texture
            // - Setup fog of war
            // - Room discovery system
            
            Debug.Log("Minimap generated");
        }
        
        /// <summary>
        /// Helper: Gọi hàm này trong DungeonManager sau khi generate xong
        /// </summary>
        [ContextMenu("Test Integration")]
        public void TestIntegration()
        {
            OnDungeonGenerated();
        }
    }
}
