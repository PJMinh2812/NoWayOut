using System.Collections.Generic;
using UnityEngine;

namespace NWO.Dungeon
{
    /// <summary>
    /// Port của global.dungeonWallBitmasks(dungeon, column, row) trong ms/game/dungeon/rendering.ms.
    /// Trả về list index tile (frames index) cần vẽ tại 1 ô wall.
    /// </summary>
    public static class DungeonWallBitmask
    {
        public static List<int> GetWallFrameIndexes(DungeonMap map, int column, int row)
        {
            var indexes = new List<int>(4);

            bool Space(int dc, int dr)
            {
                var c = column + dc;
                var r = row + dr;
                if (!map.InBounds(c, r)) return false;
                return map.Get(c, r) != DungeonCell.Wall;
            }

            if (Space(0, -1))
            {
                if (Space(-1, 0) && Space(1, 0)) indexes.Add(4);
                else if (Space(-1, 0)) indexes.Add(1);
                else if (Space(1, 0)) indexes.Add(3);
                else indexes.Add(2);
                return indexes;
            }

            if (Space(-1, 0)) indexes.Add(8);
            if (Space(1, 0)) indexes.Add(7);
            if (!Space(-1, 0) && Space(-1, -1)) indexes.Add(6);
            if (!Space(1, 0) && Space(1, -1)) indexes.Add(5);

            if (Space(0, 1))
            {
                if (Space(-1, 0) && Space(1, 0)) indexes.Add(10);
                else if (Space(-1, 0)) indexes.Add(9);
                else if (Space(1, 0)) indexes.Add(11);
                else indexes.Add(13);
            }

            if (Space(-1, 1) && !Space(-1, 0) && !Space(0, 1)) indexes.Add(14);
            if (Space(1, 1) && !Space(1, 0) && !Space(0, 1)) indexes.Add(12);

            return indexes;
        }
    }
}


