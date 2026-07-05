using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProceduralGeneration.Data;

namespace ProceduralGeneration.Core
{
    /// <summary>
    /// Utility functions cho dungeon generation
    /// </summary>
    public static class DungeonUtils
    {
        /// <summary>
        /// Chuyển đổi DoorDirection thành Vector2Int
        /// </summary>
        public static Vector2Int DirectionToVector(DoorDirection direction)
        {
            switch (direction)
            {
                case DoorDirection.Top: return Vector2Int.up;
                case DoorDirection.Bottom: return Vector2Int.down;
                case DoorDirection.Left: return Vector2Int.left;
                case DoorDirection.Right: return Vector2Int.right;
                default: return Vector2Int.zero;
            }
        }
        
        /// <summary>
        /// Chuyển đổi Vector2Int thành DoorDirection
        /// </summary>
        public static DoorDirection VectorToDirection(Vector2Int vector)
        {
            if (vector == Vector2Int.up) return DoorDirection.Top;
            if (vector == Vector2Int.down) return DoorDirection.Bottom;
            if (vector == Vector2Int.left) return DoorDirection.Left;
            if (vector == Vector2Int.right) return DoorDirection.Right;
            
            Debug.LogWarning($"Invalid direction vector: {vector}");
            return DoorDirection.Top;
        }
        
        /// <summary>
        /// Lấy hướng ngược lại
        /// </summary>
        public static DoorDirection GetOppositeDirection(DoorDirection direction)
        {
            switch (direction)
            {
                case DoorDirection.Top: return DoorDirection.Bottom;
                case DoorDirection.Bottom: return DoorDirection.Top;
                case DoorDirection.Left: return DoorDirection.Right;
                case DoorDirection.Right: return DoorDirection.Left;
                default: return DoorDirection.Top;
            }
        }
        
        /// <summary>
        /// Kiểm tra xem một vị trí grid có trống không (không bị phòng nào chiếm dụng)
        /// </summary>
        public static bool IsGridCellFree(Vector2Int position, Dictionary<Vector2Int, Room> occupiedCells)
        {
            return !occupiedCells.ContainsKey(position);
        }
        
        /// <summary>
        /// Kiểm tra xem một phòng có thể đặt tại vị trí này không (kiểm tra overlap)
        /// </summary>
        public static bool CanPlaceRoomAt(Vector2Int position, RoomData roomData, 
            Dictionary<Vector2Int, Room> occupiedCells)
        {
            return CanPlaceRoomAt(position, roomData.size, occupiedCells);
        }
        
        /// <summary>
        /// Kiểm tra xem một phòng với size cho trước có thể đặt tại vị trí này không
        /// </summary>
        public static bool CanPlaceRoomAt(Vector2Int position, Vector2Int size, 
            Dictionary<Vector2Int, Room> occupiedCells)
        {
            // Kiểm tra tất cả các ô mà phòng này sẽ chiếm dụng
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2Int cellPos = position + new Vector2Int(x, y);
                    if (occupiedCells.ContainsKey(cellPos))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Lấy danh sách các phòng lân cận
        /// </summary>
        public static List<Room> GetNeighborRooms(Room room, Dictionary<Vector2Int, Room> occupiedCells)
        {
            List<Room> neighbors = new List<Room>();
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            foreach (var dir in directions)
            {
                Vector2Int neighborPos = room.gridPosition + dir;
                if (occupiedCells.TryGetValue(neighborPos, out Room neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// Tính khoảng cách Manhattan giữa hai vị trí
        /// </summary>
        public static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
        
        /// <summary>
        /// Tìm đường đi giữa hai phòng (simplified A* for room-to-room)
        /// </summary>
        public static List<Room> FindPath(Room start, Room end, List<Room> allRooms)
        {
            if (start == null || end == null) return null;
            
            Dictionary<Room, Room> cameFrom = new Dictionary<Room, Room>();
            Dictionary<Room, int> gScore = new Dictionary<Room, int>();
            Dictionary<Room, int> fScore = new Dictionary<Room, int>();
            
            List<Room> openSet = new List<Room> { start };
            gScore[start] = 0;
            fScore[start] = ManhattanDistance(start.gridPosition, end.gridPosition);
            
            while (openSet.Count > 0)
            {
                // Lấy phòng có fScore thấp nhất
                Room current = openSet.OrderBy(r => fScore.GetValueOrDefault(r, int.MaxValue)).First();
                
                if (current == end)
                {
                    return ReconstructPath(cameFrom, current);
                }
                
                openSet.Remove(current);
                
                // Kiểm tra các phòng kết nối
                foreach (var neighbor in current.connectedRooms.Values)
                {
                    if (neighbor == null) continue;
                    
                    int tentativeGScore = gScore[current] + 1;
                    
                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + ManhattanDistance(neighbor.gridPosition, end.gridPosition);
                        
                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }
            
            return null; // Không tìm thấy đường
        }
        
        /// <summary>
        /// Reconstruct path từ A* result
        /// </summary>
        private static List<Room> ReconstructPath(Dictionary<Room, Room> cameFrom, Room current)
        {
            List<Room> path = new List<Room> { current };
            
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            
            return path;
        }
        
        /// <summary>
        /// Kiểm tra xem một vị trí có nằm trên đường đi chính không
        /// </summary>
        public static bool IsPositionOnMainPath(Vector3 position, List<Room> mainPath, float threshold = 2f)
        {
            if (mainPath == null || mainPath.Count == 0) return false;
            
            foreach (var room in mainPath)
            {
                if (room.roomInstance != null)
                {
                    float distance = Vector3.Distance(position, room.roomInstance.transform.position);
                    if (distance < threshold)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Shuffle một list (Fisher-Yates)
        /// </summary>
        public static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
        
        /// <summary>
        /// Kiểm tra xem có đủ khoảng trống để đặt trap không
        /// </summary>
        public static bool HasEnoughSpaceForTrap(Vector3 position, float radius, 
            List<Vector3> existingTraps, float minDistance)
        {
            foreach (var trap in existingTraps)
            {
                if (Vector3.Distance(position, trap) < minDistance + radius)
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}
