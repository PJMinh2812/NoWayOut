using UnityEngine;

namespace GloomCraft.Dungeon
{
    /// <summary>
    /// Interface for dungeon rendering implementations.
    /// Allows for different rendering strategies (Tilemap, Sprites, 3D, etc.)
    /// </summary>
    public interface IDungeonRenderer
    {
        /// <summary>
        /// Render the dungeon map to the scene.
        /// </summary>
        void Render(DungeonMap map);
        
        /// <summary>
        /// Clear all rendered content.
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Get world position from grid coordinates.
        /// </summary>
        Vector3 GridToWorld(int column, int row);
        
        /// <summary>
        /// Get grid coordinates from world position.
        /// </summary>
        Vector2Int WorldToGrid(Vector3 worldPosition);
    }
}
