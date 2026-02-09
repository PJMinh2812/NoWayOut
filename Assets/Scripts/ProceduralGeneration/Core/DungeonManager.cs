using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProceduralGeneration.Data;

namespace ProceduralGeneration.Core
{
    /// <summary>
    /// Main manager class cho procedural dungeon generation
    /// Xử lý toàn bộ flow: Start -> Archetype1 -> MidBoss -> Archetype2 -> Boss -> Goal
    /// </summary>
    public class DungeonManager : MonoBehaviour
    {
        [Header("Generation Settings")]
        [Tooltip("Seed cho random generation (0 = random seed)")]
        public int seed = 0;
        
        [Tooltip("Sử dụng random seed mỗi lần generate")]
        public bool useRandomSeed = true;
        
        [Header("Dungeon Flow Configuration")]
        [Tooltip("Số phòng Archetype 1 (trước mid-boss)")]
        [Range(3, 10)]
        public int archetype1RoomCount = 5;
        
        [Tooltip("Số phòng Archetype 2 (sau mid-boss)")]
        [Range(3, 10)]
        public int archetype2RoomCount = 5;
        
        [Tooltip("Xác suất tạo phòng nhánh (0-1)")]
        [Range(0f, 0.5f)]
        public float branchProbability = 0.2f;
        
        [Header("Room Data")]
        [Tooltip("Danh sách RoomData có thể sử dụng")]
        public List<RoomData> roomDatabase = new List<RoomData>();
        
        [Header("Trap Settings")]
        [Tooltip("Danh sách TrapData có thể spawn")]
        public List<TrapData> trapDatabase = new List<TrapData>();
        
        [Tooltip("Enable trap spawning")]
        public bool spawnTraps = true;
        
        [Header("Danger Scaling")]
        [Tooltip("Danger level tăng theo khoảng cách từ start")]
        public AnimationCurve dangerCurve = AnimationCurve.Linear(0, 1, 1, 10);
        
        [Header("References")]
        [Tooltip("Container chứa tất cả rooms")]
        public Transform dungeonContainer;
        
        [Header("Debug")]
        [Tooltip("Hiển thị debug info trong Scene view")]
        public bool showDebugGizmos = true;
        
        [Tooltip("Log chi tiết quá trình generation")]
        public bool verboseLogging = false;
        
        // Internal data
        private Dictionary<Vector2Int, Room> occupiedCells;
        private List<Room> allRooms;
        private List<Room> mainPath;
        private Room startRoom;
        private Room goalRoom;
        private int currentSeed;
        
        // Generation state
        private bool isGenerated = false;
        private int maxBacktrackAttempts = 10;
        
        #region Public API
        
        /// <summary>
        /// Generate dungeon mới
        /// </summary>
        public void GenerateDungeon()
        {
            // Setup seed
            if (useRandomSeed || seed == 0)
            {
                currentSeed = System.DateTime.Now.Millisecond + Random.Range(0, 10000);
            }
            else
            {
                currentSeed = seed;
            }
            
            Random.InitState(currentSeed);
            
            if (verboseLogging)
                Debug.Log($"Starting dungeon generation with seed: {currentSeed}");
            
            // Clear old dungeon
            ClearDungeon();
            
            // Initialize
            Initialize();
            
            // Generate dungeon flow
            if (!GenerateDungeonFlow())
            {
                Debug.LogError("Failed to generate dungeon flow!");
                return;
            }
            
            // Instantiate rooms
            InstantiateAllRooms();
            
            // Connect doors
            ConnectDoors();
            
            // Spawn traps
            if (spawnTraps)
            {
                SpawnTraps();
            }
            
            isGenerated = true;
            
            Debug.Log($"<color=green>Dungeon generated successfully!</color> Seed: {currentSeed}, Rooms: {allRooms.Count}");
        }
        
        /// <summary>
        /// Clear dungeon hiện tại
        /// </summary>
        public void ClearDungeon()
        {
            if (dungeonContainer != null)
            {
                // Destroy tất cả children
                for (int i = dungeonContainer.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(dungeonContainer.GetChild(i).gameObject);
                }
            }
            
            // Clear data
            if (allRooms != null)
            {
                foreach (var room in allRooms)
                {
                    room.Cleanup();
                }
            }
            
            occupiedCells?.Clear();
            allRooms?.Clear();
            mainPath?.Clear();
            startRoom = null;
            goalRoom = null;
            isGenerated = false;
            
            if (verboseLogging)
                Debug.Log("Dungeon cleared");
        }
        
        /// <summary>
        /// Lấy seed hiện tại
        /// </summary>
        public int GetCurrentSeed()
        {
            return currentSeed;
        }
        
        #endregion
        
        #region Generation Logic
        
        /// <summary>
        /// Initialize data structures
        /// </summary>
        private void Initialize()
        {
            occupiedCells = new Dictionary<Vector2Int, Room>();
            allRooms = new List<Room>();
            mainPath = new List<Room>();
            
            // Create container if not exists
            if (dungeonContainer == null)
            {
                GameObject container = new GameObject("DungeonContainer");
                dungeonContainer = container.transform;
                dungeonContainer.SetParent(this.transform);
            }
            
            // Validate room database
            if (roomDatabase == null || roomDatabase.Count == 0)
            {
                Debug.LogError("Room database is empty!");
            }
        }
        
        /// <summary>
        /// Generate dungeon flow theo structure: Start -> Arch1 -> MidBoss -> Arch2 -> Boss -> Goal
        /// </summary>
        private bool GenerateDungeonFlow()
        {
            // 1. Tạo Start room
            if (!CreateStartRoom())
            {
                Debug.LogError("Failed to create start room");
                return false;
            }
            
            // 2. Tạo Archetype 1 rooms
            if (!CreateRoomSequence(RoomType.Archetype1, archetype1RoomCount))
            {
                Debug.LogError("Failed to create Archetype 1 sequence");
                return false;
            }
            
            // 3. Tạo MidBoss room
            if (!CreateRoomOfType(RoomType.MidBoss))
            {
                Debug.LogError("Failed to create MidBoss room");
                return false;
            }
            
            // 4. Tạo Archetype 2 rooms
            if (!CreateRoomSequence(RoomType.Archetype2, archetype2RoomCount))
            {
                Debug.LogError("Failed to create Archetype 2 sequence");
                return false;
            }
            
            // 5. Tạo Boss room
            if (!CreateRoomOfType(RoomType.Boss))
            {
                Debug.LogError("Failed to create Boss room");
                return false;
            }
            
            // 6. Tạo Goal room
            if (!CreateGoalRoom())
            {
                Debug.LogError("Failed to create Goal room");
                return false;
            }
            
            // 7. Calculate danger levels
            CalculateDangerLevels();
            
            if (verboseLogging)
                Debug.Log($"Dungeon flow completed: {allRooms.Count} rooms, {mainPath.Count} on main path");
            
            return true;
        }
        
        /// <summary>
        /// Tạo start room
        /// </summary>
        private bool CreateStartRoom()
        {
            RoomData startData = GetRoomDataOfType(RoomType.Start);
            if (startData == null)
            {
                Debug.LogError("No Start room data found!");
                return false;
            }
            
            startRoom = new Room(startData, Vector2Int.zero);
            startRoom.distanceFromStart = 0;
            startRoom.isMainPath = true;
            
            AddRoomToGrid(startRoom);
            mainPath.Add(startRoom);
            
            if (verboseLogging)
                Debug.Log($"Start room created at {Vector2Int.zero}");
            
            return true;
        }
        
        /// <summary>
        /// Tạo goal room
        /// </summary>
        private bool CreateGoalRoom()
        {
            RoomData goalData = GetRoomDataOfType(RoomType.Goal);
            if (goalData == null)
            {
                Debug.LogError("No Goal room data found!");
                return false;
            }
            
            Room lastRoom = mainPath[mainPath.Count - 1];
            Room newRoom = TryPlaceRoom(goalData, lastRoom);
            
            if (newRoom == null)
            {
                Debug.LogError("Failed to place Goal room");
                return false;
            }
            
            goalRoom = newRoom;
            goalRoom.isMainPath = true;
            mainPath.Add(goalRoom);
            
            if (verboseLogging)
                Debug.Log($"Goal room created at {goalRoom.gridPosition}");
            
            return true;
        }
        
        /// <summary>
        /// Tạo một sequence các phòng cùng loại
        /// </summary>
        private bool CreateRoomSequence(RoomType roomType, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!CreateRoomOfType(roomType))
                {
                    return false;
                }
                
                // Có thể tạo branch room
                if (Random.value < branchProbability)
                {
                    TryCreateBranchRoom(roomType);
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Tạo một phòng của loại chỉ định
        /// </summary>
        private bool CreateRoomOfType(RoomType roomType)
        {
            RoomData roomData = GetRoomDataOfType(roomType);
            if (roomData == null)
            {
                Debug.LogWarning($"No room data found for type {roomType}");
                return false;
            }
            
            Room lastRoom = mainPath[mainPath.Count - 1];
            Room newRoom = TryPlaceRoom(roomData, lastRoom);
            
            if (newRoom == null)
            {
                Debug.LogWarning($"Failed to place room of type {roomType}");
                return false;
            }
            
            newRoom.isMainPath = true;
            mainPath.Add(newRoom);
            
            if (verboseLogging)
                Debug.Log($"Created {roomType} room at {newRoom.gridPosition}");
            
            return true;
        }
        
        /// <summary>
        /// Thử tạo branch room (phòng nhánh)
        /// </summary>
        private void TryCreateBranchRoom(RoomType roomType)
        {
            // Lấy một phòng ngẫu nhiên từ main path (không phải Start hoặc special rooms)
            var eligibleRooms = mainPath.Where(r => 
                r.roomData.roomType != RoomType.Start && 
                r.roomData.roomType != RoomType.Boss &&
                r.roomData.roomType != RoomType.MidBoss).ToList();
            
            if (eligibleRooms.Count == 0) return;
            
            Room branchFrom = eligibleRooms[Random.Range(0, eligibleRooms.Count)];
            RoomData branchRoomData = GetRoomDataOfType(roomType);
            
            if (branchRoomData != null)
            {
                Room branchRoom = TryPlaceRoom(branchRoomData, branchFrom);
                if (branchRoom != null)
                {
                    branchRoom.isMainPath = false;
                    
                    if (verboseLogging)
                        Debug.Log($"Created branch room at {branchRoom.gridPosition}");
                }
            }
        }
        
        /// <summary>
        /// Thử đặt phòng kế bên phòng hiện tại (với backtracking)
        /// </summary>
        private Room TryPlaceRoom(RoomData roomData, Room fromRoom)
        {
            // Lấy các hướng có thể đi
            List<DoorDirection> availableDirections = GetAvailableDirections(fromRoom);
            
            // Shuffle để random
            DungeonUtils.Shuffle(availableDirections);
            
            // Thử từng hướng
            foreach (var direction in availableDirections)
            {
                Vector2Int offset = DungeonUtils.DirectionToVector(direction);
                Vector2Int newPosition = fromRoom.gridPosition + offset;
                
                // Kiểm tra xem có thể đặt không
                if (DungeonUtils.CanPlaceRoomAt(newPosition, roomData, occupiedCells))
                {
                    // Kiểm tra door compatibility
                    DoorDirection requiredDoor = DungeonUtils.GetOppositeDirection(direction);
                    if (roomData.doorAnchors.Any(anchor => anchor.direction == requiredDoor))
                    {
                        Room newRoom = new Room(roomData, newPosition);
                        newRoom.distanceFromStart = fromRoom.distanceFromStart + 1;
                        
                        AddRoomToGrid(newRoom);
                        
                        // Connect rooms
                        fromRoom.connectedRooms[direction] = newRoom;
                        newRoom.connectedRooms[requiredDoor] = fromRoom;
                        
                        return newRoom;
                    }
                }
            }
            
            // Backtracking: thử các phòng khác trong main path
            for (int i = mainPath.Count - 2; i >= Mathf.Max(0, mainPath.Count - maxBacktrackAttempts); i--)
            {
                Room backtrackRoom = mainPath[i];
                Room result = TryPlaceRoom(roomData, backtrackRoom);
                if (result != null)
                {
                    if (verboseLogging)
                        Debug.Log($"Backtracked to room at {backtrackRoom.gridPosition}");
                    return result;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Lấy các hướng có thể đi từ phòng
        /// </summary>
        private List<DoorDirection> GetAvailableDirections(Room room)
        {
            List<DoorDirection> directions = new List<DoorDirection>();
            
            foreach (var anchor in room.roomData.doorAnchors)
            {
                // Chỉ thêm nếu chưa có phòng kết nối
                if (!room.connectedRooms.ContainsKey(anchor.direction) || 
                    room.connectedRooms[anchor.direction] == null)
                {
                    directions.Add(anchor.direction);
                }
            }
            
            return directions;
        }
        
        /// <summary>
        /// Add room vào grid
        /// </summary>
        private void AddRoomToGrid(Room room)
        {
            // Đánh dấu tất cả các ô mà phòng này chiếm
            foreach (var cell in room.GetOccupiedGridCells())
            {
                occupiedCells[cell] = room;
            }
            
            allRooms.Add(room);
        }
        
        /// <summary>
        /// Calculate danger level cho tất cả phòng
        /// </summary>
        private void CalculateDangerLevels()
        {
            int maxDistance = mainPath.Max(r => r.distanceFromStart);
            
            foreach (var room in allRooms)
            {
                float normalizedDistance = (float)room.distanceFromStart / maxDistance;
                room.dangerLevel = Mathf.RoundToInt(dangerCurve.Evaluate(normalizedDistance));
            }
        }
        
        #endregion
        
        #region Room Instantiation
        
        /// <summary>
        /// Instantiate tất cả rooms trong scene
        /// </summary>
        private void InstantiateAllRooms()
        {
            foreach (var room in allRooms)
            {
                room.InstantiateRoom(dungeonContainer);
            }
            
            if (verboseLogging)
                Debug.Log($"Instantiated {allRooms.Count} rooms");
        }
        
        #endregion
        
        #region Door Connection
        
        /// <summary>
        /// Kết nối doors giữa các phòng
        /// </summary>
        private void ConnectDoors()
        {
            foreach (var room in allRooms)
            {
                // Kiểm tra từng door
                foreach (var door in room.doors)
                {
                    DoorDirection direction = door.direction;
                    
                    // Kiểm tra xem có phòng kết nối không
                    if (room.connectedRooms.TryGetValue(direction, out Room connectedRoom) && 
                        connectedRoom != null)
                    {
                        // Có phòng kết nối -> mở cửa
                        door.OpenDoor();
                        door.connectedRoom = connectedRoom;
                        
                        if (verboseLogging)
                            Debug.Log($"Opened door at {room.gridPosition} -> {direction}");
                    }
                    else
                    {
                        // Không có phòng -> đóng cửa (hiển thị tường)
                        door.CloseDoor();
                    }
                }
            }
            
            Debug.Log($"Connected {allRooms.Sum(r => r.doors.Count(d => d.isOpen))} doors");
        }
        
        #endregion
        
        #region Trap Spawning
        
        /// <summary>
        /// Spawn traps vào các phòng
        /// </summary>
        private void SpawnTraps()
        {
            if (trapDatabase == null || trapDatabase.Count == 0)
            {
                Debug.LogWarning("Trap database is empty!");
                return;
            }
            
            int totalTrapsSpawned = 0;
            
            foreach (var room in allRooms)
            {
                // Skip start và goal room
                if (room.roomData.roomType == RoomType.Start || 
                    room.roomData.roomType == RoomType.Goal)
                    continue;
                
                int trapsSpawned = SpawnTrapsInRoom(room);
                totalTrapsSpawned += trapsSpawned;
            }
            
            Debug.Log($"Spawned {totalTrapsSpawned} traps across {allRooms.Count} rooms");
        }
        
        /// <summary>
        /// Spawn traps trong một phòng
        /// </summary>
        private int SpawnTrapsInRoom(Room room)
        {
            if (room.trapSpawnPoints.Count == 0) return 0;
            
            int trapsSpawned = 0;
            int maxTraps = Mathf.Min(room.roomData.maxTrapSpawnPoints, room.trapSpawnPoints.Count);
            
            // Lấy traps phù hợp với danger level
            var eligibleTraps = trapDatabase.Where(t => 
                t.minDangerLevel <= room.dangerLevel).ToList();
            
            if (eligibleTraps.Count == 0) return 0;
            
            // Shuffle spawn points
            List<Transform> availablePoints = new List<Transform>(room.trapSpawnPoints);
            DungeonUtils.Shuffle(availablePoints);
            
            Dictionary<TrapData, int> spawnedCount = new Dictionary<TrapData, int>();
            List<Vector3> spawnedPositions = new List<Vector3>();
            
            foreach (var spawnPoint in availablePoints)
            {
                if (trapsSpawned >= maxTraps) break;
                
                // Validate spawn point
                if (!DungeonUtils.ValidateSpawnPoint(spawnPoint, room.doors, 2f))
                    continue;
                
                // Chọn trap ngẫu nhiên
                TrapData trapData = eligibleTraps[Random.Range(0, eligibleTraps.Count)];
                
                // Kiểm tra max per room
                int currentCount = spawnedCount.GetValueOrDefault(trapData, 0);
                if (currentCount >= trapData.maxPerRoom)
                    continue;
                
                // Kiểm tra probability
                if (Random.value > trapData.spawnProbability)
                    continue;
                
                // Kiểm tra khoảng cách với traps khác
                if (!DungeonUtils.HasEnoughSpaceForTrap(spawnPoint.position, 
                    trapData.occupiedArea.magnitude / 2f, spawnedPositions, 1f))
                    continue;
                
                // Spawn trap
                GameObject trap = Instantiate(trapData.trapPrefab, spawnPoint.position, 
                    spawnPoint.rotation, room.roomInstance.transform);
                trap.name = $"Trap_{trapData.name}";
                
                spawnedPositions.Add(spawnPoint.position);
                spawnedCount[trapData] = currentCount + 1;
                trapsSpawned++;
            }
            
            return trapsSpawned;
        }
        
        #endregion
        
        #region Utilities
        
        /// <summary>
        /// Lấy RoomData của loại chỉ định
        /// </summary>
        private RoomData GetRoomDataOfType(RoomType roomType)
        {
            var rooms = roomDatabase.Where(r => r.roomType == roomType).ToList();
            if (rooms.Count == 0) return null;
            
            return rooms[Random.Range(0, rooms.Count)];
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || !isGenerated || allRooms == null) return;
            
            // Draw grid
            foreach (var room in allRooms)
            {
                Vector3 center = new Vector3(
                    room.gridPosition.x * 10f + (room.roomData.size.x * 5f),
                    room.gridPosition.y * 10f + (room.roomData.size.y * 5f),
                    0);
                
                Vector3 size = new Vector3(room.roomData.size.x * 10f, room.roomData.size.y * 10f, 0.1f);
                
                // Color dựa trên room type
                Gizmos.color = room.isMainPath ? Color.green : Color.yellow;
                Gizmos.DrawWireCube(center, size);
                
                // Draw danger level
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(center, $"D:{room.dangerLevel}");
                #endif
            }
            
            // Draw connections
            Gizmos.color = Color.cyan;
            foreach (var room in allRooms)
            {
                Vector3 roomCenter = new Vector3(room.gridPosition.x * 10f, room.gridPosition.y * 10f, 0);
                
                foreach (var connection in room.connectedRooms.Values)
                {
                    if (connection != null)
                    {
                        Vector3 targetCenter = new Vector3(connection.gridPosition.x * 10f, 
                            connection.gridPosition.y * 10f, 0);
                        Gizmos.DrawLine(roomCenter, targetCenter);
                    }
                }
            }
        }
        
        #endregion
    }
}
