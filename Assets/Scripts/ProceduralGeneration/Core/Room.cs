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
        
        // Kết nối
        public Dictionary<DoorDirection, Room> connectedRooms;
        public List<DoorInstance> doors;
        
        // Spawn points
        public List<Transform> trapSpawnPoints;
        public List<Transform> enemySpawnPoints;
        
        // Meta info
        public int distanceFromStart;
        public int dangerLevel;
        public bool isMainPath;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public Room(RoomData data, Vector2Int position)
        {
            roomData = data;
            gridPosition = position;
            connectedRooms = new Dictionary<DoorDirection, Room>();
            doors = new List<DoorInstance>();
            trapSpawnPoints = new List<Transform>();
            enemySpawnPoints = new List<Transform>();
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
            
            for (int x = 0; x < roomData.size.x; x++)
            {
                for (int y = 0; y < roomData.size.y; y++)
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
        public void InstantiateRoom(Transform parent)
        {
            // Tạo empty room container thay vì instantiate prefab
            Vector3 worldPosition = new Vector3(gridPosition.x * 10f, gridPosition.y * 10f, 0);
            roomInstance = new GameObject($"Room_{roomData.roomType}_{gridPosition.x}_{gridPosition.y}");
            roomInstance.transform.SetParent(parent);
            roomInstance.transform.position = worldPosition;
            
            // Generate visuals (Tilemaps)
            GenerateVisuals();
            
            // Comment out prefab-based features - user sẽ vẽ manual
            // FindSpawnPoints();
            // SetupDoors();
        }
        
        /// <summary>
        /// Generate visuals cho room
        /// </summary>
        private void GenerateVisuals()
        {
            if (roomInstance == null) return;
            
            var visualGen = roomInstance.GetComponent<Components.RoomVisualGenerator>();
            if (visualGen == null)
            {
                visualGen = roomInstance.AddComponent<Components.RoomVisualGenerator>();
            }
            
            visualGen.GenerateVisuals(roomData);
        }
        
        /// <summary>
        /// Tìm các spawn points trong prefab
        /// </summary>
        private void FindSpawnPoints()
        {
            if (roomInstance == null) return;
            
            // Tìm trap spawn points
            Transform trapParent = roomInstance.transform.Find("TrapSpawnPoints");
            if (trapParent != null)
            {
                foreach (Transform child in trapParent)
                {
                    trapSpawnPoints.Add(child);
                }
            }
            
            // Tìm enemy spawn points
            Transform enemyParent = roomInstance.transform.Find("EnemySpawnPoints");
            if (enemyParent != null)
            {
                foreach (Transform child in enemyParent)
                {
                    enemySpawnPoints.Add(child);
                }
            }
        }
        
        /// <summary>
        /// Setup door instances
        /// </summary>
        private void SetupDoors()
        {
            if (roomInstance == null) return;
            
            foreach (var anchor in roomData.doorAnchors)
            {
                DoorInstance doorInstance = new DoorInstance
                {
                    direction = anchor.direction,
                    anchorData = anchor,
                    isOpen = false,
                    connectedRoom = null
                };
                
                doors.Add(doorInstance);
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

    /// <summary>
    /// Instance của một cửa trong phòng
    /// </summary>
    public class DoorInstance
    {
        public DoorDirection direction;
        public DoorAnchor anchorData;
        public bool isOpen;
        public Room connectedRoom;
        public GameObject spawnedDoor;
        public GameObject spawnedWall;
        
        /// <summary>
        /// Mở cửa (enable door, disable wall)
        /// </summary>
        public void OpenDoor()
        {
            isOpen = true;
            if (anchorData.doorObject != null)
                anchorData.doorObject.SetActive(true);
            if (anchorData.wallObject != null)
                anchorData.wallObject.SetActive(false);
        }
        
        /// <summary>
        /// Đóng cửa (disable door, enable wall)
        /// </summary>
        public void CloseDoor()
        {
            isOpen = false;
            if (anchorData.doorObject != null)
                anchorData.doorObject.SetActive(false);
            if (anchorData.wallObject != null)
                anchorData.wallObject.SetActive(true);
        }
    }
}
