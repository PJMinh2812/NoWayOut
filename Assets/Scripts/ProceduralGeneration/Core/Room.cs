using UnityEngine;
using System.Collections.Generic;
using ProceduralGeneration.Data;

namespace ProceduralGeneration.Core
{
    /// <summary>
    /// Class đại diện cho một instance của phòng trong dungeon
    /// </summary>
    public class Room
    {
        // Thông tin cơ bản
        public RoomData roomData;
        public Vector2Int gridPosition;
        public GameObject roomInstance;
        
        // Override size (để không modify ScriptableObject)
        public Vector2Int actualSize;
        
        // World scale for proper positioning
        public float worldScale = 1f;
        
        // Kết nối
        public Dictionary<DoorDirection, Room> connectedRooms;
        
        // Meta info
        public int distanceFromStart;
        public int dangerLevel;
        public bool isMainPath;

        // Prefab mode flag
        private bool usingPrefab;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public Room(RoomData data, Vector2Int position, Vector2Int? overrideSize = null)
        {
            roomData = data;
            gridPosition = position;
            actualSize = overrideSize ?? data.size; // Sử dụng override hoặc default
            connectedRooms = new Dictionary<DoorDirection, Room>();
            distanceFromStart = 0;
            dangerLevel = 0;
            isMainPath = false;
        }
        
        /// <summary>
        /// Kiểm tra xem phòng có cửa theo hướng chỉ định không
        /// </summary>
        public bool HasDoorInDirection(DoorDirection direction)
        {
            return roomData.doorAnchors.Exists(anchor => anchor.direction == direction);
        }
        
        /// <summary>
        /// Lấy tất cả các ô grid mà phòng này chiếm dụng
        /// </summary>
        public List<Vector2Int> GetOccupiedGridCells()
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            
            for (int x = 0; x < actualSize.x; x++)
            {
                for (int y = 0; y < actualSize.y; y++)
                {
                    cells.Add(gridPosition + new Vector2Int(x, y));
                }
            }
            
            return cells;
        }
        
        /// <summary>
        /// Kiểm tra xem phòng có thể kết nối với phòng khác không
        /// </summary>
        public bool CanConnectTo(Room otherRoom)
        {
            if (otherRoom == null) return false;
            
            // Kiểm tra xem có ở cạnh nhau không
            Vector2Int diff = otherRoom.gridPosition - gridPosition;
            
            // Kiểm tra các hướng
            if (diff == Vector2Int.up && HasDoorInDirection(DoorDirection.Top) && 
                otherRoom.HasDoorInDirection(DoorDirection.Bottom))
                return true;
                
            if (diff == Vector2Int.down && HasDoorInDirection(DoorDirection.Bottom) && 
                otherRoom.HasDoorInDirection(DoorDirection.Top))
                return true;
                
            if (diff == Vector2Int.left && HasDoorInDirection(DoorDirection.Left) && 
                otherRoom.HasDoorInDirection(DoorDirection.Right))
                return true;
                
            if (diff == Vector2Int.right && HasDoorInDirection(DoorDirection.Right) && 
                otherRoom.HasDoorInDirection(DoorDirection.Left))
                return true;
            
            return false;
        }
        
        /// <summary>
        /// Lấy hướng đến phòng khác
        /// </summary>
        public DoorDirection? GetDirectionTo(Room otherRoom)
        {
            if (otherRoom == null) return null;
            
            Vector2Int diff = otherRoom.gridPosition - gridPosition;
            
            if (diff == Vector2Int.up) return DoorDirection.Top;
            if (diff == Vector2Int.down) return DoorDirection.Bottom;
            if (diff == Vector2Int.left) return DoorDirection.Left;
            if (diff == Vector2Int.right) return DoorDirection.Right;
            
            return null;
        }
        
        /// <summary>
        /// Instantiate phòng trong scene
        /// </summary>
        public void InstantiateRoom(Transform parent, float worldScale = 1f)
        {
            this.worldScale = worldScale;
            Vector3 worldPosition = new Vector3(gridPosition.x * worldScale, gridPosition.y * worldScale, 0);

            if (roomData.roomPrefab != null)
            {
                // ===== CHẾ ĐỘ PREFAB: Dùng phòng đã decor sẵn =====
                roomInstance = Object.Instantiate(roomData.roomPrefab, parent);
                roomInstance.name = $"Room_{roomData.roomType}_{gridPosition.x}_{gridPosition.y}";
                roomInstance.transform.position = worldPosition;

                // Prefab phải có RoomVisualGenerator sẵn (hoặc add nếu thiếu)
                var visualGen = roomInstance.GetComponent<Components.RoomVisualGenerator>();
                if (visualGen == null)
                    visualGen = roomInstance.AddComponent<Components.RoomVisualGenerator>();

                usingPrefab = true;
            }
            else
            {
                // ===== CHẾ ĐỘ AUTO-GEN: Như cũ =====
                roomInstance = new GameObject($"Room_{roomData.roomType}_{gridPosition.x}_{gridPosition.y}");
                roomInstance.transform.SetParent(parent);
                roomInstance.transform.position = worldPosition;

                roomInstance.AddComponent<Components.RoomVisualGenerator>();
                usingPrefab = false;
            }
        }
        
        /// <summary>
        /// Generate visuals cho room (gọi SAU khi đã configure tiles)
        /// </summary>
        public void GenerateVisuals()
        {
            if (roomInstance == null) return;

            var visualGen = roomInstance.GetComponent<Components.RoomVisualGenerator>();
            if (visualGen == null)
            {
                Debug.LogError("RoomVisualGenerator not found!");
                return;
            }

            // Set currentRoom reference (for DoorTrigger)
            visualGen.SetCurrentRoom(this);

            if (usingPrefab)
            {
                // Prefab đã có floor/wall → CHỈ cần generate doors
                visualGen.GenerateDoorsOnly(roomData, connectedRooms);
            }
            else
            {
                // Auto-gen full: floor + walls + doors
                visualGen.GenerateVisuals(roomData, connectedRooms);
            }
        }
        
        /// <summary>
        /// Configure visual generator với tile settings từ DungeonManager
        /// </summary>
        public void ConfigureVisualGenerator(bool autoFill, UnityEngine.Tilemaps.TileBase[] floors,
            UnityEngine.Tilemaps.TileBase center, UnityEngine.Tilemaps.TileBase topL, UnityEngine.Tilemaps.TileBase topR,
            UnityEngine.Tilemaps.TileBase botL, UnityEngine.Tilemaps.TileBase botR,
            UnityEngine.Tilemaps.TileBase top, UnityEngine.Tilemaps.TileBase bottom,
            UnityEngine.Tilemaps.TileBase left, UnityEngine.Tilemaps.TileBase right,
            GameObject door, Data.TrapData[] traps)
        {
            if (roomInstance == null) return;
            
            var visualGen = roomInstance.GetComponent<Components.RoomVisualGenerator>();
            if (visualGen != null)
            {
                visualGen.ConfigureTiles(autoFill, floors, center, topL, topR, botL, botR,
                    top, bottom, left, right, door, traps);
            }
        }
        
        /// <summary>
        /// Cleanup khi destroy
        /// </summary>
        public void Cleanup()
        {
            if (roomInstance != null)
            {
                GameObject.Destroy(roomInstance);
            }
        }
    }
}
