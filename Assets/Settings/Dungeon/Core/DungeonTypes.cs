using System;
using UnityEngine;

namespace GloomCraft.Dungeon
{
    public enum DungeonCell
    {
        Wall,
        Room,
        Tunnel,
        Furniture,
        Start,
        Finish
    }

    [Serializable]
    public sealed class DungeonMap
    {
        public int Columns;
        public int Rows;
        public DungeonCell[] Cells; // row-major: index = row * Columns + column
        public Vector2Int Start;
        public Vector2Int Finish;

        public DungeonCell Get(int c, int r) => Cells[r * Columns + c];
        public void Set(int c, int r, DungeonCell v) => Cells[r * Columns + c] = v;

        public bool InBounds(int c, int r) => c >= 0 && r >= 0 && c < Columns && r < Rows;
        public bool IsSpace(int c, int r)
        {
            if (!InBounds(c, r)) return false;
            var v = Get(c, r);
            return v is DungeonCell.Room or DungeonCell.Tunnel or DungeonCell.Start or DungeonCell.Finish;
        }
    }
}


