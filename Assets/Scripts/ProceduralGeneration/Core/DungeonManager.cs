using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;
using ProceduralGeneration.Data;
using ProceduralGeneration.Components;
using Core;
using NWO;

namespace ProceduralGeneration.Core
{
    
    /// Main manager class cho procedural dungeon generation
    /// Xử lý toàn bộ flow: Start -> Archetype1 -> MidBoss -> Archetype2 -> Boss -> Goal
    
    public class DungeonManager : MonoBehaviour
    {
        [System.Serializable]
        private class RoomTypePrefabPool
        {
            public RoomType roomType;
            public List<GameObject> prefabs = new List<GameObject>();
        }

        [Header("Generation Settings")]
        [Tooltip("Seed cho random generation (0 = random seed)")]
        public int seed = 0;
        
        [Tooltip("Sử dụng random seed mỗi lần generate")]
        public bool useRandomSeed = true;
        
        [Header("World Scale")]
        [Tooltip("Scale từ grid cells -> world units (1 grid cell = worldScale units)")]
        [Range(0.5f, 2f)]
        public float worldScale = 1f; // Default 1:1 mapping
        
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

        [Header("Room Prefab Pools (Optional)")]
        [Tooltip("Prefab pool theo từng room type. Nếu rỗng sẽ dùng roomData.roomPrefab như cũ")]
        [SerializeField] private List<RoomTypePrefabPool> roomTypePrefabPools = new List<RoomTypePrefabPool>();
        
        [Header("Trap Settings (For RoomVisualGenerator)")]
        [Tooltip("Danh sách TrapData có thể spawn - Assign vào RoomVisualGenerator")]
        public List<TrapData> trapDatabase = new List<TrapData>();
        
        [Tooltip("Enable trap spawning - DEPRECATED: Use RoomVisualGenerator.autoFillTiles")]
        public bool spawnTraps = false;
        
        [Header("Tile Configuration (For Auto-Fill)")]
        [Tooltip("Bật auto-fill tiles (nếu tắt sẽ tạo placeholder để vẽ manual)")]
        [SerializeField] private bool autoFillTiles = true;
        
        [Header("Floor Tiles")]
        [SerializeField] private UnityEngine.Tilemaps.TileBase[] floorTiles;
        
        [Header("Wall Tiles")]
        [SerializeField] private UnityEngine.Tilemaps.TileBase wallCenter;
        [SerializeField] private UnityEngine.Tilemaps.TileBase wallTopLeft;
        [SerializeField] private UnityEngine.Tilemaps.TileBase wallTopRight;
        [SerializeField] private UnityEngine.Tilemaps.TileBase wallBottomLeft;
        [SerializeField] private UnityEngine.Tilemaps.TileBase wallBottomRight;
        [SerializeField] private UnityEngine.Tilemaps.TileBase wallTop;
        [SerializeField] private UnityEngine.Tilemaps.TileBase wallBottom;
        [SerializeField] private UnityEngine.Tilemaps.TileBase wallLeft;
        [SerializeField] private UnityEngine.Tilemaps.TileBase wallRight;
        
        [Header("Wall Fill Tiles (theo hướng)")]
        [Tooltip("Tile trám tường TRÊN khi không có cửa. Null = dùng wallTop.")]
        [SerializeField] private UnityEngine.Tilemaps.TileBase wallFillTop;
        [Tooltip("Tile trám tường DƯỚI. Null = dùng wallBottom.")]
        [SerializeField] private UnityEngine.Tilemaps.TileBase wallFillBottom;
        [Tooltip("Tile trám tường TRÁI. Null = dùng wallLeft.")]
        [SerializeField] private UnityEngine.Tilemaps.TileBase wallFillLeft;
        [Tooltip("Tile trám tường PHẢI. Null = dùng wallRight.")]
        [SerializeField] private UnityEngine.Tilemaps.TileBase wallFillRight;
        
        [Header("Door Prefab")]
        [SerializeField] private GameObject doorPrefab;
        
        [Header("Trap Types")]
        [SerializeField] private Data.TrapData[] trapTypes;
        
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
        
        
        /// Generate dungeon mới
        
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
            
            // Ensure RoomTransitionManager exists
            EnsureTransitionManager();
            
            // Connect doors (tracking only - visuals handled by RoomVisualGenerator)
            // ConnectDoors(); // DEPRECATED: Door visuals now auto-generated
            
            // Spawn traps
            // NOTE: Trap spawning moved to RoomVisualGenerator.CreateAutoFilledFloor()
            // if (spawnTraps)
            // {
            //     SpawnTraps();
            // }
            
            isGenerated = true;
            
            // Light Fragments được đặt tay trong scene (tutorial map)
            // Không auto-spawn nữa - dùng menu: GameObject > No Way Out > Light Fragment
            // SpawnLightFragments();
            
            Debug.Log($"<color=green>Dungeon generated successfully!</color> Seed: {currentSeed}, Rooms: {allRooms.Count}");
        }
        
        
        /// Clear dungeon hiện tại
        
        public void ClearDungeon()
        {
            // Re-find dungeonContainer nếu reference bị mất (sau Play→Stop)
            if (dungeonContainer == null)
            {
                dungeonContainer = transform.Find("DungeonContainer");
                if (dungeonContainer == null)
                {
                    GameObject containerObj = GameObject.Find("DungeonContainer");
                    if (containerObj != null)
                        dungeonContainer = containerObj.transform;
                }
            }
            
            if (dungeonContainer != null)
            {
                // Destroy tất cả children (dùng DestroyImmediate cho Editor mode)
                for (int i = dungeonContainer.childCount - 1; i >= 0; i--)
                {
                    var child = dungeonContainer.GetChild(i).gameObject;
                    if (Application.isPlaying)
                        Destroy(child);
                    else
                        DestroyImmediate(child);
                }
            }
            
            // Clear data - KHÔNG gọi Cleanup vì children đã bị destroy ở trên
            // Room.Cleanup() sẽ lỗi nếu roomInstance đã bị destroy
            
            occupiedCells?.Clear();
            allRooms?.Clear();
            mainPath?.Clear();
            startRoom = null;
            goalRoom = null;
            isGenerated = false;
            
            Debug.Log("[DungeonManager] Dungeon cleared");
        }
        
        
        /// <summary>
        /// Lấy seed hiện tại
        /// </summary>
        public int GetCurrentSeed()
        {
            return currentSeed;
        }
        
        /// <summary>
        /// Tìm Room object từ GameObject instance (dùng khi DoorTrigger cần lookup room)
        /// </summary>
        /// Tìm Room object từ GameObject instance (dùng khi DoorTrigger cần lookup room)
        /// </summary>
        public Room GetRoomByGameObject(GameObject roomGameObject)
        {
            if (allRooms == null || roomGameObject == null)
                return null;
            
            return allRooms.Find(r => r.roomInstance == roomGameObject);
        }
        
        /// <summary>
        /// Lấy tất cả rooms (dùng cho debug hoặc door trigger lookup)
        /// </summary>
        public List<Room> GetAllRooms()
        {
            return allRooms;
        }

        /// <summary>
        /// Lấy start room hiện tại
        /// </summary>
        public Room GetStartRoom()
        {
            return startRoom;
        }

        /// <summary>
        /// Lấy goal room hiện tại
        /// </summary>
        public Room GetGoalRoom()
        {
            return goalRoom;
        }
        
        /// <summary>
        /// Rebuild Room list từ các room GameObjects có sẵn trong scene (khi enter Play Mode)
        /// </summary>
        public void RebuildRoomListFromScene()
        {
            if (allRooms == null)
                allRooms = new List<Room>();
            else
                allRooms.Clear();
            
            if (dungeonContainer == null)
            {
                Debug.LogWarning("[DungeonManager] No DungeonContainer found, cannot rebuild room list");
                return;
            }
            
            // Tìm tất cả room GameObjects trong DungeonContainer
            // Room GameObjects có format: Room_<type>_<x>_<y> (ví dụ: Room_Start_0_0)
            for (int i = 0; i < dungeonContainer.childCount; i++)
            {
                Transform roomTransform = dungeonContainer.GetChild(i);
                GameObject roomObj = roomTransform.gameObject;
                
                // Check xem có phải là room GameObject không (bắt đầu với "Room_")
                if (!roomObj.name.StartsWith("Room_"))
                    continue;
                
                // Parse thông tin từ tên GameObject: Room_<type>_<x>_<y>
                string[] parts = roomObj.name.Split('_');
                if (parts.Length < 4)
                    continue;
                
                string roomType = parts[1];
                if (!int.TryParse(parts[2], out int gridX))
                    continue;
                if (!int.TryParse(parts[3], out int gridY))
                    continue;
                
                // Tìm RoomData tương ứng trong database
                RoomData roomData = roomDatabase.Find(r => r.roomType.ToString() == roomType);
                if (roomData == null)
                {
                    Debug.LogWarning($"[DungeonManager] Cannot find RoomData for type: {roomType}");
                    continue;
                }
                
                // Tạo Room object với constructor
                Room room = new Room(roomData, new Vector2Int(gridX, gridY));
                room.roomInstance = roomObj;
                
                allRooms.Add(room);
            }
            
            // Rebuild connected rooms từ door prefabs
            RebuildRoomConnectionsFromDoors();
            
            // Setup room visibility: chỉ show Start room, ẩn tất cả rooms khác
            Room rebuiltStartRoom = null;
            foreach (var room in allRooms)
            {
                if (room.roomData != null && room.roomData.roomType == RoomType.Start)
                {
                    rebuiltStartRoom = room;
                    break;
                }
            }
            
            // Nếu không tìm thấy Start room, dùng room đầu tiên
            if (rebuiltStartRoom == null && allRooms.Count > 0)
                rebuiltStartRoom = allRooms[0];
            
            foreach (var room in allRooms)
            {
                if (room.roomInstance != null)
                {
                    bool isStart = (room == rebuiltStartRoom);
                    room.roomInstance.SetActive(isStart);
                }
            }

            // Cập nhật Respawn_Point vào trung tâm start room
            if (rebuiltStartRoom != null && rebuiltStartRoom.roomInstance != null)
            {
                Vector3 spawnCenter = rebuiltStartRoom.roomInstance.transform.position + new Vector3(6f, 6f, 0);
                foreach (var tm in rebuiltStartRoom.roomInstance.GetComponentsInChildren<Tilemap>())
                {
                    if (tm.name.Contains("Floor"))
                    {
                        tm.CompressBounds();
                        var b = tm.cellBounds;
                        if (b.size.x > 1)
                        {
                            spawnCenter = tm.CellToWorld(new Vector3Int(
                                b.xMin + b.size.x / 2,
                                b.yMin + b.size.y / 2, 0));
                            break;
                        }
                    }
                }
                UpdateRespawnPoint(spawnCenter);
            }
            
            // Set camera background = đen
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.backgroundColor = Color.black;
                mainCam.clearFlags = CameraClearFlags.SolidColor;
            }
            
            // Auto-create GameManager nếu chưa có
            if (NWO.GameManager.Instance == null && FindFirstObjectByType<NWO.GameManager>() == null)
            {
                var gmObj = new GameObject("GameManager");
                gmObj.AddComponent<NWO.GameManager>();
                Debug.Log("[DungeonManager] Auto-created GameManager");
            }
            
            // Auto-create DungeonLightingManager nếu chưa có
            if (NWO.DungeonLightingManager.Instance == null && FindFirstObjectByType<NWO.DungeonLightingManager>() == null)
            {
                var lightObj = new GameObject("DungeonLightingManager");
                lightObj.AddComponent<NWO.DungeonLightingManager>();
                Debug.Log("[DungeonManager] Auto-created DungeonLightingManager");
            }
            
            Debug.Log($"[DungeonManager] Rebuilt {allRooms.Count} rooms from scene");
        }
        
        /// <summary>
        /// Rebuild room connections bằng cách match doors theo distance và opposite direction
        /// </summary>
        private void RebuildRoomConnectionsFromDoors()
        {
            // Thu thập tất cả doors từ tất cả rooms
            var allDoors = new List<(Room room, DoorTrigger door, DoorDirection direction, Vector3 position)>();
            
            foreach (var room in allRooms)
            {
                Transform doorsContainer = room.roomInstance.transform.Find("Visuals/Doors");
                if (doorsContainer == null)
                    doorsContainer = room.roomInstance.transform.Find("Doors");
                
                if (doorsContainer == null)
                    continue;
                
                for (int i = 0; i < doorsContainer.childCount; i++)
                {
                    Transform doorTransform = doorsContainer.GetChild(i);
                    var doorTrigger = doorTransform.GetComponent<DoorTrigger>();
                    
                    if (doorTrigger != null)
                    {
                        allDoors.Add((room, doorTrigger, doorTrigger.doorDirection, doorTransform.position));
                    }
                }
            }
            
            // Match mỗi door với door đối diện gần nhất
            foreach (var (room, door, direction, position) in allDoors)
            {
                DoorDirection oppositeDir = GetOppositeDirection(direction);
                
                // Tìm door gần nhất với opposite direction và nằm đúng hướng
                Room bestTarget = null;
                float bestDistance = float.MaxValue;
                
                foreach (var (otherRoom, otherDoor, otherDir, otherPos) in allDoors)
                {
                    // Skip same room
                    if (otherRoom == room)
                        continue;
                    
                    // Chỉ match với opposite direction
                    if (otherDir != oppositeDir)
                        continue;
                    
                    // Check nếu door kia nằm đúng hướng
                    if (!IsInCorrectDirection(position, otherPos, direction))
                        continue;
                    
                    float distance = Vector3.Distance(position, otherPos);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestTarget = otherRoom;
                    }
                }
                
                if (bestTarget != null)
                {
                    room.connectedRooms[direction] = bestTarget;
                }
            }
        }
        
        private DoorDirection GetOppositeDirection(DoorDirection dir)
        {
            switch (dir)
            {
                case DoorDirection.Top: return DoorDirection.Bottom;
                case DoorDirection.Bottom: return DoorDirection.Top;
                case DoorDirection.Left: return DoorDirection.Right;
                case DoorDirection.Right: return DoorDirection.Left;
                default: return dir;
            }
        }
        
        private bool IsInCorrectDirection(Vector3 from, Vector3 to, DoorDirection direction)
        {
            Vector3 delta = to - from;
            switch (direction)
            {
                case DoorDirection.Top: return delta.y > 0;
                case DoorDirection.Bottom: return delta.y < 0;
                case DoorDirection.Left: return delta.x < 0;
                case DoorDirection.Right: return delta.x > 0;
                default: return false;
            }
        }
        
        private Vector3 GetRayDirectionFromDoorDirection(DoorDirection direction)
        {
            switch (direction)
            {
                case DoorDirection.Top: return Vector3.up;
                case DoorDirection.Bottom: return Vector3.down;
                case DoorDirection.Left: return Vector3.left;
                case DoorDirection.Right: return Vector3.right;
                default: return Vector3.zero;
            }
        }
        
        private Room FindRoomContaining(Transform transform)
        {
            Transform current = transform;
            while (current != null)
            {
                Room found = allRooms.Find(r => r.roomInstance == current.gameObject);
                if (found != null)
                    return found;
                
                current = current.parent;
            }
            return null;
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Auto-find DungeonContainer nếu chưa assign trong Inspector
            if (dungeonContainer == null)
            {
                // Try tìm as child của DungeonManager
                dungeonContainer = transform.Find("DungeonContainer");
                
                // Nếu không tìm thấy, tìm trong toàn scene
                if (dungeonContainer == null)
                {
                    GameObject containerObj = GameObject.Find("DungeonContainer");
                    if (containerObj != null)
                        dungeonContainer = containerObj.transform;
                }
                
                if (dungeonContainer != null)
                {
                }
            }
            
            // Rebuild room list nếu có rooms sẵn trong scene (Editor-generated dungeon)
            if (dungeonContainer != null && dungeonContainer.childCount > 0)
            {
                RebuildRoomListFromScene();
            }
        }
        
        #endregion
        
        #region Generation Logic
        
        
        /// Initialize data structures
        
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
        
        
        /// Generate dungeon flow theo structure: Start -> Arch1 -> MidBoss -> Arch2 -> Boss -> Goal
        
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
        
        
        /// Tạo start room
        
        private bool CreateStartRoom()
        {
            RoomData startData = GetRoomDataOfType(RoomType.Start);
            if (startData == null)
            {
                Debug.LogError("No Start room data found!");
                return false;
            }
            
            // Tính adjusted size: luôn là số lẻ, START room nhỏ hơn
            Vector2Int adjustedSize = CalculateAdjustedRoomSize(startData);
            
            startRoom = CreateRoomInstance(startData, Vector2Int.zero, adjustedSize);
            startRoom.distanceFromStart = 0;
            startRoom.isMainPath = true;
            
            AddRoomToGrid(startRoom);
            mainPath.Add(startRoom);
            
            if (verboseLogging)
                Debug.Log($"Start room created at {Vector2Int.zero}");
            
            return true;
        }
        
        
        /// Tạo goal room
        
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
        
        
        /// Tạo một sequence các phòng cùng loại
        
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
        
        
        /// Tạo một phòng của loại chỉ định
        
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
        
        
        /// Thử tạo branch room (phòng nhánh)
        
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
        
        
        /// Thử đặt phòng kế bên phòng hiện tại (với backtracking)
        
        private Room TryPlaceRoom(RoomData roomData, Room fromRoom)
        {
            // Lấy các hướng có thể đi
            List<DoorDirection> availableDirections = GetAvailableDirections(fromRoom);
            
            // Shuffle để random
            DungeonUtils.Shuffle(availableDirections);
            
            // Tính adjusted size TRƯỚC KHI check placement
            Vector2Int adjustedSize = CalculateAdjustedRoomSize(roomData);
            
            // Thử từng hướng
            foreach (var direction in availableDirections)
            {
                Vector2Int offset = DungeonUtils.DirectionToVector(direction);
                
                // CRITICAL: Offset phải = size của fromRoom để không overlap
                // Ví dụ: fromRoom.actualSize = (7,7), direction = Right → newPosition = gridPos + (7,0)
                Vector2Int scaledOffset = new Vector2Int(
                    offset.x * fromRoom.actualSize.x,
                    offset.y * fromRoom.actualSize.y
                );
                Vector2Int newPosition = fromRoom.gridPosition + scaledOffset;
                
                // Kiểm tra xem có thể đặt không (dùng adjustedSize)
                if (DungeonUtils.CanPlaceRoomAt(newPosition, adjustedSize, occupiedCells))
                {
                    // Kiểm tra door compatibility
                    DoorDirection requiredDoor = DungeonUtils.GetOppositeDirection(direction);
                    if (roomData.doorAnchors.Any(anchor => anchor.direction == requiredDoor))
                    {
                        Room newRoom = CreateRoomInstance(roomData, newPosition, adjustedSize);
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
        
        
        /// Lấy các hướng có thể đi từ phòng
        
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
        
        
        /// Add room vào grid
        
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
        /// Tạo Room instance với prefab random theo room type (nếu có pool)
        /// </summary>
        private Room CreateRoomInstance(RoomData roomData, Vector2Int position, Vector2Int adjustedSize)
        {
            GameObject selectedPrefab = GetRandomPrefabForType(roomData.roomType, roomData.roomPrefab);
            return new Room(roomData, position, adjustedSize, selectedPrefab);
        }

        /// <summary>
        /// Lấy prefab ngẫu nhiên theo room type, fallback về prefab mặc định của RoomData
        /// </summary>
        private GameObject GetRandomPrefabForType(RoomType roomType, GameObject fallbackPrefab)
        {
            // Ưu tiên prefab gắn trực tiếp trên RoomData.
            // Chỉ dùng pool khi RoomData không có prefab.
            if (fallbackPrefab != null)
                return fallbackPrefab;

            if (roomTypePrefabPools == null || roomTypePrefabPools.Count == 0)
                return fallbackPrefab;

            RoomTypePrefabPool pool = roomTypePrefabPools.Find(p => p.roomType == roomType);
            if (pool == null || pool.prefabs == null || pool.prefabs.Count == 0)
                return fallbackPrefab;

            List<GameObject> validPrefabs = pool.prefabs.Where(p => p != null).ToList();
            if (validPrefabs.Count == 0)
                return fallbackPrefab;

            return validPrefabs[Random.Range(0, validPrefabs.Count)];
        }
        
        
        /// Calculate danger level cho tất cả phòng
        
        private void CalculateDangerLevels()
        {
            int maxDistance = mainPath.Max(r => r.distanceFromStart);
            
            foreach (var room in allRooms)
            {
                float normalizedDistance = (float)room.distanceFromStart / maxDistance;
                room.dangerLevel = Mathf.RoundToInt(dangerCurve.Evaluate(normalizedDistance));
            }
        }
        
        
        /// Adjust room size: Luôn là số LẺ, START/GOAL nhỏ hơn (KHÔNG modify roomData)
        
        private Vector2Int CalculateAdjustedRoomSize(RoomData roomData)
        {
            Vector2Int size = roomData.size;
            
            // Nếu size chẵn -> cộng 1 để thành lẻ
            if (size.x % 2 == 0)
                size.x += 1;
            if (size.y % 2 == 0)
                size.y += 1;
            
            // START/GOAL rooms nhỏ hơn (min 11x11, max 15x15)
            if (roomData.roomType == RoomType.Start || roomData.roomType == RoomType.Goal)
            {
                size.x = Mathf.Max(11, Mathf.Min(size.x, 15));
                size.y = Mathf.Max(11, Mathf.Min(size.y, 15));
            }
            // Các phòng khác (min 15x15, max 25x25)
            else
            {
                size.x = Mathf.Max(15, Mathf.Min(size.x, 25));
                size.y = Mathf.Max(15, Mathf.Min(size.y, 25));
            }
            
            // Đảm bảo vẫn là số lẻ sau khi clamp
            if (size.x % 2 == 0)
                size.x += 1;
            if (size.y % 2 == 0)
                size.y += 1;
            
            return size;
        }
        
        #endregion
        
        #region Room Instantiation
        
        
        /// Instantiate tất cả rooms trong scene
        
        private void InstantiateAllRooms()
        {
            Room startRoom = null;
            
            foreach (var room in allRooms)
            {
                // 1. Instantiate room GameObject
                room.InstantiateRoom(dungeonContainer, worldScale);
                
                // 2. Configure visual generator với tile settings
                room.ConfigureVisualGenerator(autoFillTiles, floorTiles, 
                    wallCenter, wallTopLeft, wallTopRight, wallBottomLeft, wallBottomRight,
                    wallTop, wallBottom, wallLeft, wallRight, doorPrefab, trapTypes,
                    wallFillTop, wallFillBottom, wallFillLeft, wallFillRight);
                
                // 3. Generate visuals (SAU khi đã configure)
                room.GenerateVisuals();
                
                // 4. Tìm start room
                if (room.roomData.roomType == RoomType.Start || room.distanceFromStart == 0)
                {
                    startRoom = room;
                }
            }
            
            // 5. Chỉ activate START ROOM, tất cả rooms khác SetActive(false)
            foreach (var room in allRooms)
            {
                if (room.roomInstance != null)
                {
                    bool isStartRoom = (room == startRoom);
                    room.roomInstance.SetActive(isStartRoom);
                    
                    if (isStartRoom)
                    {
                        Debug.Log($"[DungeonManager] START ROOM activated: {room.roomData.roomType} at {room.gridPosition}");
                    }
                }
            }
            
            // Cập nhật Respawn_Point vào trung tâm start room để player spawn đúng
            if (startRoom != null && startRoom.roomInstance != null)
            {
                float cx = startRoom.roomInstance.transform.position.x + startRoom.actualSize.x * 0.5f;
                float cy = startRoom.roomInstance.transform.position.y + startRoom.actualSize.y * 0.5f;
                UpdateRespawnPoint(new Vector3(cx, cy, 0));
            }

            if (verboseLogging)
                Debug.Log($"Instantiated {allRooms.Count} rooms (only START room active)");
                
            // Set camera background = đen
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.backgroundColor = Color.black;
                mainCam.clearFlags = CameraClearFlags.SolidColor;
            }
        }
        
        #endregion
        
        #region Light Fragment Spawning
        
        /// <summary>
        /// Spawn Light Fragments vào dungeon rooms
        /// </summary>
        private void SpawnLightFragments()
        {
            var spawner = GetComponent<NWO.LightFragmentSpawner>();
            if (spawner == null)
                spawner = gameObject.AddComponent<NWO.LightFragmentSpawner>();
            
            spawner.SpawnFragmentsInDungeon(this);
        }
        
        #endregion
        
        #region Utilities
        
        
        /// Lấy RoomData của loại chỉ định
        
        private RoomData GetRoomDataOfType(RoomType roomType)
        {
            var rooms = roomDatabase.Where(r => r.roomType == roomType).ToList();
            if (rooms.Count == 0) return null;
            
            return rooms[Random.Range(0, rooms.Count)];
        }
        
        
        /// <summary>
        /// Cập nhật Respawn_Point để PlayerSpawnManager tìm đúng vị trí spawn
        /// </summary>
        private void UpdateRespawnPoint(Vector3 position)
        {
            GameObject respawnPoint = GameObject.Find("Respawn_Point");
            if (respawnPoint == null)
                respawnPoint = new GameObject("Respawn_Point");
            respawnPoint.transform.position = position;
            Debug.Log($"[DungeonManager] Respawn_Point → {position}");
        }

        /// Đảm bảo RoomTransitionManager tồn tại trong scene
        
        private void EnsureTransitionManager()
        {
            var existing = FindFirstObjectByType<RoomTransitionManager>();
            if (existing == null)
            {
                GameObject managerObj = new GameObject("RoomTransitionManager");
                managerObj.AddComponent<RoomTransitionManager>();
                Debug.Log("[DungeonManager] Auto-created RoomTransitionManager");
            }
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
