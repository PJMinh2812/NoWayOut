using System;
using UnityEngine;

namespace NWO.Dungeon
{
    /// <summary>
    /// ScriptableObject containing dungeon generation parameters.
    /// Allows for easy difficulty scaling and level design.
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonConfig", menuName = "NWO/Dungeon Config")]
    public sealed class DungeonConfig : ScriptableObject
    {
        [Header("Map Size")]
        [Range(16, 100)] public int columns = 30;
        [Range(16, 100)] public int rows = 30;
        
        [Header("Room Settings")]
        [Range(3, 10)] public int minRoomSize = 7;
        [Range(5, 20)] public int maxRoomSize = 15;
        [Range(10, 100)] public int density = 30;
        
        [Header("Randomization")]
        public bool useSeed = false;
        public int seed = 0;
        
        [Header("Spawn Chances")]
        [Range(0, 1)] public float treasureChestChance = 0.8f;
        [Range(0, 1)] public float enemyChance = 0.6f;
        [Range(0, 1)] public float craftingBenchChance = 0.4f;
        [Range(0, 1)] public float shopStandChance = 0.2f;
        
        [Header("Enemy Settings")]
        [Tooltip("Number of spots per enemy spawn")]
        public int spotsPerEnemy = 16;

        /// <summary>
        /// Create a copy with modified difficulty.
        /// </summary>
        public DungeonConfig WithDifficulty(float multiplier)
        {
            var copy = Instantiate(this);
            copy.enemyChance = Mathf.Clamp01(enemyChance * multiplier);
            copy.spotsPerEnemy = Mathf.Max(4, Mathf.RoundToInt(spotsPerEnemy / multiplier));
            return copy;
        }
        
        /// <summary>
        /// Validate configuration values.
        /// </summary>
        private void OnValidate()
        {
            if (maxRoomSize <= minRoomSize)
                maxRoomSize = minRoomSize + 1;
            
            if (columns < 16) columns = 16;
            if (rows < 16) rows = 16;
        }
    }
}
