using UnityEngine;

namespace GloomCraft.Dungeon
{
    /// <summary>
    /// ScriptableObject chứa danh sách sprites cho dungeon/tileset (theo thứ tự frame microStudio).
    /// Được sinh/tự cập nhật bởi MicroStudioImportConfigurator.
    /// </summary>
    public sealed class DungeonTilesetConfig : ScriptableObject
    {
        public Sprite[] frames;
    }
}


