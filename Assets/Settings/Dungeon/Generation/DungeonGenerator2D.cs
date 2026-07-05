using System;
using System.Collections.Generic;
using UnityEngine;

namespace NWO.Dungeon
{
    /// <summary>
    /// C# port của ms/lib/plasmapuffs/dungeon_generation/dungeon.ms (rooms + tunnels + border + start/finish).
    /// </summary>
    public sealed class DungeonGenerator2D
    {
        [Serializable]
        public sealed class Room
        {
            public int Width;
            public int Height;
            public int Column;
            public int Row;
        }

        public sealed class Result
        {
            public DungeonMap Map;
            public List<Room> Rooms;
            public Room StartRoom;
            public Room FinishRoom;
        }

        public static Result Generate(int columns, int rows, int minimumRoomSize, int maximumRoomSize, int density, int? seed = null)
        {
            if (columns < 8 || rows < 8) throw new ArgumentOutOfRangeException(nameof(columns), "Dungeon too small.");
            if (minimumRoomSize < 2) throw new ArgumentOutOfRangeException(nameof(minimumRoomSize));
            if (maximumRoomSize <= minimumRoomSize) throw new ArgumentOutOfRangeException(nameof(maximumRoomSize));

            var rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

            // Tilemap in MS is created with (columns - 2, rows - 2), then border is added back.
            var innerCols = columns - 2;
            var innerRows = rows - 2;

            var map = new DungeonMap
            {
                Columns = innerCols,
                Rows = innerRows,
                Cells = new DungeonCell[innerCols * innerRows]
            };
            Fill(map, DungeonCell.Wall);

            var rooms = new List<Room>();

            while (true)
            {
                var newRoom = new Room
                {
                    Width = rng.Next(maximumRoomSize - minimumRoomSize) + minimumRoomSize,
                    Height = rng.Next(maximumRoomSize - minimumRoomSize) + minimumRoomSize,
                    Column = 0,
                    Row = 0
                };

                var attempts = 0;
                while (true)
                {
                    newRoom.Column = rng.Next(innerCols - newRoom.Width);
                    newRoom.Row = rng.Next(innerRows - newRoom.Height);

                    if (CanPlaceRoom(map, newRoom)) break;
                    if (attempts > density) break;
                    attempts++;
                }

                if (attempts > density) break;

                rooms.Add(newRoom);
                AddRoom(map, newRoom);

                if (rooms.Count > 1)
                {
                    var prev = rooms[rooms.Count - 2];
                    CreateTunnel(map, rng, newRoom, prev);
                }
            }

            if (rooms.Count < 2)
            {
                // Match microStudio behavior: retry by recursion.
                return Generate(columns, rows, minimumRoomSize, maximumRoomSize, density, seed.HasValue ? seed.Value + 1 : null);
            }

            foreach (var r in rooms) AddRoom(map, r);

            // Add border back (so final size == input columns/rows)
            map = AddBorder(map);
            foreach (var r in rooms)
            {
                r.Column += 1;
                r.Row += 1;
            }

            // Start/finish rooms differ
            Room startRoom;
            Room finishRoom;
            do
            {
                startRoom = rooms[rng.Next(rooms.Count)];
                finishRoom = rooms[rng.Next(rooms.Count)];
            } while (ReferenceEquals(startRoom, finishRoom));

            var start = new Vector2Int(
                rng.Next(startRoom.Column, startRoom.Column + startRoom.Width + 1),
                rng.Next(startRoom.Row, startRoom.Row + startRoom.Height + 1)
            );
            var finish = new Vector2Int(
                rng.Next(finishRoom.Column, finishRoom.Column + finishRoom.Width + 1),
                rng.Next(finishRoom.Row, finishRoom.Row + finishRoom.Height + 1)
            );

            // Clamp just in case RNG hit edge
            start.x = Mathf.Clamp(start.x, 0, map.Columns - 1);
            start.y = Mathf.Clamp(start.y, 0, map.Rows - 1);
            finish.x = Mathf.Clamp(finish.x, 0, map.Columns - 1);
            finish.y = Mathf.Clamp(finish.y, 0, map.Rows - 1);

            map.Start = start;
            map.Finish = finish;
            map.Set(start.x, start.y, DungeonCell.Start);
            map.Set(finish.x, finish.y, DungeonCell.Finish);

            return new Result { Map = map, Rooms = rooms, StartRoom = startRoom, FinishRoom = finishRoom };
        }

        private static void Fill(DungeonMap map, DungeonCell value)
        {
            for (var i = 0; i < map.Cells.Length; i++) map.Cells[i] = value;
        }

        private static void AddRoom(DungeonMap map, Room room)
        {
            for (var r = room.Row; r <= room.Row + room.Height - 1; r++)
            for (var c = room.Column; c <= room.Column + room.Width - 1; c++)
                map.Set(c, r, DungeonCell.Room);
        }

        private static bool CanPlaceRoom(DungeonMap map, Room room)
        {
            for (var r = room.Row - 1; r <= room.Row + room.Height + 1; r++)
            for (var c = room.Column - 1; c <= room.Column + room.Width + 1; c++)
            {
                if (!map.InBounds(c, r)) continue;
                if (map.Get(c, r) != DungeonCell.Wall) return false;
            }
            return true;
        }

        private static void CreateTunnel(DungeonMap map, System.Random rng, Room a, Room b)
        {
            if (rng.NextDouble() < 0.5)
            {
                var startColumn = rng.Next(a.Column, a.Column + a.Width);
                var endColumn = rng.Next(b.Column, b.Column + b.Width);
                var startRow = rng.Next(a.Row, a.Row + a.Height);
                CreateTunnelHorizontally(map, startColumn, endColumn, startRow);

                var middleRow = rng.Next(b.Row, b.Row + b.Height);
                CreateTunnelVertically(map, startRow, middleRow, endColumn);
            }
            else
            {
                var startRow = rng.Next(a.Row, a.Row + a.Height);
                var endRow = rng.Next(b.Row, b.Row + b.Height);
                var startColumn = rng.Next(a.Column, a.Column + a.Width);
                CreateTunnelVertically(map, startRow, endRow, startColumn);

                var middleColumn = rng.Next(b.Column, b.Column + b.Width);
                CreateTunnelHorizontally(map, startColumn, middleColumn, endRow);
            }
        }

        private static void CreateTunnelHorizontally(DungeonMap map, int start, int finish, int row)
        {
            var a = Mathf.Min(start, finish);
            var b = Mathf.Max(start, finish);
            for (var c = a; c <= b; c++) map.Set(c, row, DungeonCell.Tunnel);
        }

        private static void CreateTunnelVertically(DungeonMap map, int start, int finish, int column)
        {
            var a = Mathf.Min(start, finish);
            var b = Mathf.Max(start, finish);
            for (var r = a; r <= b; r++) map.Set(column, r, DungeonCell.Tunnel);
        }

        private static DungeonMap AddBorder(DungeonMap inner)
        {
            var outer = new DungeonMap
            {
                Columns = inner.Columns + 2,
                Rows = inner.Rows + 2,
                Cells = new DungeonCell[(inner.Columns + 2) * (inner.Rows + 2)]
            };
            Fill(outer, DungeonCell.Wall);

            for (var r = 0; r < inner.Rows; r++)
            for (var c = 0; c < inner.Columns; c++)
                outer.Set(c + 1, r + 1, inner.Get(c, r));

            return outer;
        }
    }
}


