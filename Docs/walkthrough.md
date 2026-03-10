# Hướng dẫn hoàn thiện Map Decoration — Step by Step

## Tổng quan thay đổi code

Đã sửa 2 file:

| File | Thay đổi |
|------|----------|
| [DecorData.cs](file:///d:/%21course/FPT/7.SP26/PRU/noWayOut/Assets/Scripts/ProceduralGeneration/Data/DecorData.cs) | Thêm `DecorPlacement` enum, `allowRandomFlip`, `allowRandomRotation`, `placement` |
| [RoomVisualGenerator.cs](file:///d:/%21course/FPT/7.SP26/PRU/noWayOut/Assets/Scripts/ProceduralGeneration/Components/RoomVisualGenerator.cs) | Rewrite [GenerateDecorations()](file:///d:/%21course/FPT/7.SP26/PRU/noWayOut/Assets/Scripts/ProceduralGeneration/Components/RoomVisualGenerator.cs#644-786) + thêm [CanPlaceDecorAt()](file:///d:/%21course/FPT/7.SP26/PRU/noWayOut/Assets/Scripts/ProceduralGeneration/Components/RoomVisualGenerator.cs#787-804), [PlaceDecor()](file:///d:/%21course/FPT/7.SP26/PRU/noWayOut/Assets/Scripts/ProceduralGeneration/Components/RoomVisualGenerator.cs#805-851) |

```diff:DecorData.cs
using UnityEngine;

namespace ProceduralGeneration.Data
{
    /// <summary>
    /// ScriptableObject chứa pool đồ trang trí ngẫu nhiên cho phòng
    /// </summary>
    [CreateAssetMenu(fileName = "New Decor Data", menuName = "Procedural Generation/Decor Data")]
    public class DecorData : ScriptableObject
    {
        [Header("Decor Pool")]
        public DecorItem[] items;

        [Header("Density")]
        [Range(0f, 1f)]
        [Tooltip("Tỷ lệ ô interior sẽ có decor (0.1 = 10%)")]
        public float density = 0.15f;

        [Header("Constraints")]
        [Tooltip("Khoảng cách tối thiểu giữa 2 decor (tiles)")]
        public int minSpacing = 2;

        [Tooltip("Khoảng cách tối thiểu từ door (tiles)")]
        public int doorClearance = 3;
    }

    [System.Serializable]
    public class DecorItem
    {
        public GameObject prefab;

        [Tooltip("Xác suất chọn (weight)")]
        public float weight = 1f;

        [Tooltip("Đồ vật chiếm bao nhiêu ô")]
        public Vector2Int size = Vector2Int.one;

        [Tooltip("Có chặn di chuyển player không")]
        public bool blocksMovement = false;

        [Tooltip("Sorting order offset")]
        public int sortingOrderOffset = 0;
    }
}
===
using UnityEngine;

namespace ProceduralGeneration.Data
{
    /// <summary>
    /// Loại vị trí đặt decor trong phòng
    /// </summary>
    public enum DecorPlacement
    {
        Interior,       // Đặt tự do trong interior (cách wall 2 tile)
        WallAligned,    // Đặt sát tường (row/column thứ 2)
        Corner          // Đặt ở 4 góc interior
    }

    /// <summary>
    /// ScriptableObject chứa pool đồ trang trí ngẫu nhiên cho phòng
    /// </summary>
    [CreateAssetMenu(fileName = "New Decor Data", menuName = "Procedural Generation/Decor Data")]
    public class DecorData : ScriptableObject
    {
        [Header("Decor Pool")]
        public DecorItem[] items;

        [Header("Density")]
        [Range(0f, 1f)]
        [Tooltip("Tỷ lệ ô interior sẽ có decor (0.1 = 10%)")]
        public float density = 0.15f;

        [Header("Constraints")]
        [Tooltip("Khoảng cách tối thiểu giữa 2 decor (tiles)")]
        public int minSpacing = 2;

        [Tooltip("Khoảng cách tối thiểu từ door (tiles)")]
        public int doorClearance = 3;
    }

    [System.Serializable]
    public class DecorItem
    {
        public GameObject prefab;

        [Tooltip("Xác suất chọn (weight)")]
        public float weight = 1f;

        [Tooltip("Đồ vật chiếm bao nhiêu ô")]
        public Vector2Int size = Vector2Int.one;

        [Tooltip("Có chặn di chuyển player không")]
        public bool blocksMovement = false;

        [Tooltip("Sorting order offset")]
        public int sortingOrderOffset = 0;

        [Header("Placement")]
        [Tooltip("Loại vị trí đặt: Interior (giữa phòng), WallAligned (sát tường), Corner (góc)")]
        public DecorPlacement placement = DecorPlacement.Interior;

        [Header("Visual Variety")]
        [Tooltip("Cho phép random flip horizontal")]
        public bool allowRandomFlip = false;

        [Tooltip("Cho phép random rotation (0/90/180/270)")]
        public bool allowRandomRotation = false;
    }
}
```

```diff:RoomVisualGenerator.cs
using UnityEngine;
using UnityEngine.Tilemaps;
using ProceduralGeneration.Data;

namespace ProceduralGeneration.Components
{
    
    /// Tự động generate visuals cho rooms (floor, walls, doors)
    
    public class RoomVisualGenerator : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Color floorColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Màu xám đậm
        [SerializeField] private Color wallColor = new Color(0.2f, 0.2f, 0.25f, 1f); // Màu xám xanh
        [SerializeField] private Color doorColor = new Color(0.6f, 0.4f, 0.2f, 1f); // Màu nâu
        
        [Header("Tile Settings")]
        [SerializeField] private float tileSize = 1f; // 1 grid cell = 1 world unit
        [SerializeField] private float wallThickness = 0.15f; // Thin for door markers
        
        [Header("Auto-Fill Tiles")]
        [Tooltip("Bật auto-fill tiles (nếu tắt sẽ tạo placeholder để vẽ manual)")]
        [SerializeField] private bool autoFillTiles = true;
        
        [Header("Floor Tiles")]
        [SerializeField] private TileBase[] floorTiles; // Random chọn từ mảng này
        
        [Header("Wall Tiles")]
        [SerializeField] private TileBase wallCenter; // Wall ở giữa
        [SerializeField] private TileBase wallTopLeft; // Góc trên trái
        [SerializeField] private TileBase wallTopRight; // Góc trên phải
        [SerializeField] private TileBase wallBottomLeft; // Góc dưới trái
        [SerializeField] private TileBase wallBottomRight; // Góc dưới phải
        [SerializeField] private TileBase wallTop; // Tường trên (horizontal)
        [SerializeField] private TileBase wallBottom; // Tường dưới (horizontal)
        [SerializeField] private TileBase wallLeft; // Tường trái (vertical)
        [SerializeField] private TileBase wallRight; // Tường phải (vertical)
        
        [Header("Door Prefab")]
        [SerializeField] private GameObject doorPrefab; // Prefab có animation mở/đóng
        
        [Header("Trap Settings")]
        [SerializeField] private Data.TrapData[] trapTypes; // Loại trap có thể spawn
        
        private RoomData roomData;
        private Sprite squareSprite;
        private Tile defaultFloorTile; // Runtime-generated tile if no asset assigned
        private System.Collections.Generic.HashSet<DoorDirection> activeDirections; // Hướng có room kế bên
        private Core.Room currentRoom; // Room hiện tại (for DoorTrigger)
        private System.Collections.Generic.Dictionary<DoorDirection, Core.Room> connectedRooms; // Rooms kế bên
        
        /// <summary>
        /// Configure tiles từ DungeonManager (vì component được add runtime)
        /// </summary>
        public void ConfigureTiles(bool autoFill, TileBase[] floors, TileBase center,
            TileBase topL, TileBase topR, TileBase botL, TileBase botR,
            TileBase top, TileBase bottom, TileBase left, TileBase right,
            GameObject door, Data.TrapData[] traps)
        {
            autoFillTiles = autoFill;
            floorTiles = floors;
            wallCenter = center;
            wallTopLeft = topL;
            wallTopRight = topR;
            wallBottomLeft = botL;
            wallBottomRight = botR;
            wallTop = top;
            wallBottom = bottom;
            wallLeft = left;
            wallRight = right;
            doorPrefab = door;
            trapTypes = traps;
        }
        
        /// <summary>
        /// Set current room reference (for DoorTrigger)
        /// </summary>
        public void SetCurrentRoom(Core.Room room)
        {
            currentRoom = room;
        }
        
        
        /// Generate visuals cho room
        
        public void GenerateVisuals(RoomData data, System.Collections.Generic.Dictionary<DoorDirection, Core.Room> connections = null)
        {
            roomData = data;
            connectedRooms = connections;
            
            // Lưu các hướng có room kế bên
            activeDirections = new System.Collections.Generic.HashSet<DoorDirection>();
            if (connections != null)
            {
                foreach (var kvp in connections)
                {
                    activeDirections.Add(kvp.Key);
                }
            }
            
            // Tạo container
            GameObject visualContainer = new GameObject("Visuals");
            visualContainer.transform.SetParent(transform);
            visualContainer.transform.localPosition = Vector3.zero;
            
            // worldScale đã được apply trong Room.InstantiateRoom, không cần scale lại
            
            // Tạo Floor Tilemap
            if (autoFillTiles && floorTiles != null && floorTiles.Length > 0)
            {
                CreateAutoFilledFloor(visualContainer.transform);
            }
            else
            {
                CreateEmptyTilemap(visualContainer.transform, "Floor", -10);
            }
            
            // Tạo Walls Tilemap
            if (autoFillTiles && wallCenter != null)
            {
                CreateAutoFilledWalls(visualContainer.transform);
            }
            else
            {
                CreateEmptyTilemap(visualContainer.transform, "Walls", 0);
            }
            
            // Tạo doors (prefab với animation hoặc markers)
            if (doorPrefab != null)
            {
                GenerateDoorPrefabs(visualContainer.transform);
            }
            else
            {
                GenerateDoorMarkers(visualContainer.transform);
            }

            // Spawn decorations nếu có cấu hình
            if (roomData.decorData != null)
            {
                GenerateDecorations(visualContainer.transform, roomData.decorData);
            }
        }
        
        /// <summary>
        /// Generate chỉ doors cho phòng prefab (đã có floor/wall sẵn)
        /// </summary>
        public void GenerateDoorsOnly(RoomData data, System.Collections.Generic.Dictionary<DoorDirection, Core.Room> connections = null)
        {
            roomData = data;
            connectedRooms = connections;

            activeDirections = new System.Collections.Generic.HashSet<DoorDirection>();
            if (connections != null)
            {
                foreach (var kvp in connections)
                    activeDirections.Add(kvp.Key);
            }

            // Tìm hoặc tạo container cho doors
            Transform doorParent = transform.Find("Doors");
            if (doorParent == null)
            {
                GameObject doorsGo = new GameObject("Doors");
                doorsGo.transform.SetParent(transform);
                doorsGo.transform.localPosition = Vector3.zero;
                doorParent = doorsGo.transform;
            }

            if (doorPrefab != null)
                GenerateDoorPrefabs(doorParent);
            else
                GenerateDoorMarkers(doorParent);
        }

        
        /// Tạo empty Tilemap structure (Grid + Tilemap) - với placeholder tiles
        
        private void CreateEmptyTilemap(Transform parent, string name, int sortingOrder)
        {
            // Tạo Grid object
            GameObject gridObj = new GameObject($"{name}Grid");
            gridObj.transform.SetParent(parent);
            gridObj.transform.localPosition = Vector3.zero;
            
            Grid grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0); // Cell size = 1 unit (match sprite size 16px/16PPU)
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            
            // Tạo Tilemap
            GameObject tilemapObj = new GameObject($"{name}Tilemap");
            tilemapObj.transform.SetParent(gridObj.transform);
            tilemapObj.transform.localPosition = Vector3.zero;
            
            Tilemap tilemap = tilemapObj.AddComponent<Tilemap>();
            TilemapRenderer tilemapRenderer = tilemapObj.AddComponent<TilemapRenderer>();
            
            // Setup renderer
            tilemapRenderer.sortingOrder = sortingOrder;
            tilemapRenderer.sortingLayerName = "Default";
            tilemapRenderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Auto;
            
            // Pre-fill với placeholder tiles để show bounds
            CreateSquareSprite();
            Tile placeholderTile = ScriptableObject.CreateInstance<Tile>();
            placeholderTile.sprite = squareSprite;
            placeholderTile.color = name == "Floor" ? new Color(0.3f, 0.3f, 0.3f, 0.3f) : new Color(0.5f, 0.5f, 0.5f, 0.3f); // Semi-transparent
            
            // Fill tiles theo room size (in world units, not room units)
            // Room size 1×1 = 10×10 world units = 10×10 grid cells (vì cell = 1 unit)
            int tilesX = currentRoom.actualSize.x * (int)tileSize; // Ví dụ: 1 * 10 = 10 cells
            int tilesY = currentRoom.actualSize.y * (int)tileSize; // Ví dụ: 1 * 10 = 10 cells
            
            for (int x = 0; x < tilesX; x++)
            {
                for (int y = 0; y < tilesY; y++)
                {
                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    tilemap.SetTile(tilePos, placeholderTile);
                }
            }
        }
        
        
        /// Tạo door markers để chỉ vị trí doors (chỉ cho các hướng có room kế bên)
        
        private void GenerateDoorMarkers(Transform parent)
        {
            if (roomData.doorAnchors == null || roomData.doorAnchors.Count == 0)
                return;
            
            CreateSquareSprite(); // Tạo sprite cho door markers
            
            GameObject doorContainer = new GameObject("DoorMarkers");
            doorContainer.transform.SetParent(parent);
            doorContainer.transform.localPosition = Vector3.zero;
            
            float roomWidth = currentRoom.actualSize.x * tileSize;
            float roomHeight = currentRoom.actualSize.y * tileSize;
            
            // Scale door markers theo worldScale
            float doorSize = 1.5f * currentRoom.worldScale;
            float scaledWallThickness = wallThickness * currentRoom.worldScale;
            
            foreach (var door in roomData.doorAnchors)
            {
                // Chỉ tạo door marker nếu hướng này có room kế bên
                if (activeDirections != null && !activeDirections.Contains(door.direction))
                {
                    continue;
                }
                GameObject doorMarker = new GameObject($"DoorMarker_{door.direction}");
                doorMarker.transform.SetParent(doorContainer.transform);
                
                float doorInset = tileSize; // Door markers cũng lùi vào trong 1 tile
                Vector3 doorPos = Vector3.zero;
                Vector3 doorScale = new Vector3(doorSize, doorSize, 1);
                
                // Tính position của door - offset vào trong
                switch (door.direction)
                {
                    case DoorDirection.Top:
                        doorPos = new Vector3(roomWidth / 2f + door.localPosition.x * tileSize, roomHeight - doorInset, -0.1f);
                        doorScale = new Vector3(doorSize, scaledWallThickness, 1);
                        break;
                    case DoorDirection.Bottom:
                        doorPos = new Vector3(roomWidth / 2f + door.localPosition.x * tileSize, doorInset, -0.1f);
                        doorScale = new Vector3(doorSize, scaledWallThickness, 1);
                        break;
                    case DoorDirection.Left:
                        doorPos = new Vector3(doorInset, roomHeight / 2f + door.localPosition.y * tileSize, -0.1f);
                        doorScale = new Vector3(scaledWallThickness, doorSize, 1);
                        break;
                    case DoorDirection.Right:
                        doorPos = new Vector3(roomWidth - doorInset, roomHeight / 2f + door.localPosition.y * tileSize, -0.1f);
                        doorScale = new Vector3(scaledWallThickness, doorSize, 1);
                        break;
                }
                
                doorMarker.transform.localPosition = doorPos;
                doorMarker.transform.localScale = doorScale;
                
                SpriteRenderer sr = doorMarker.AddComponent<SpriteRenderer>();
                sr.sprite = squareSprite;
                sr.color = doorColor; // Cửa hiển thị bình thường, hệ thống ánh sáng quyết định tầm nhìn
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 10;
            }
        }
        
        /// <summary>
        /// Tạo floor với auto-fill random tiles
        /// </summary>
        private void CreateAutoFilledFloor(Transform parent)
        {
            GameObject gridObj = new GameObject("FloorGrid");
            gridObj.transform.SetParent(parent);
            gridObj.transform.localPosition = Vector3.zero;
            
            Grid grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0);
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            
            GameObject tilemapObj = new GameObject("FloorTilemap");
            tilemapObj.transform.SetParent(gridObj.transform);
            tilemapObj.transform.localPosition = Vector3.zero;
            
            Tilemap tilemap = tilemapObj.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObj.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = -10;
            
            int tilesX = currentRoom.actualSize.x * (int)tileSize;
            int tilesY = currentRoom.actualSize.y * (int)tileSize;
            
            // Fill sàn CHỈ interior (bỏ edges vì đó là chỗ có wall)
            for (int x = 1; x < tilesX - 1; x++)
            {
                for (int y = 1; y < tilesY - 1; y++)
                {
                    TileBase randomTile = floorTiles[Random.Range(0, floorTiles.Length)];
                    tilemap.SetTile(new Vector3Int(x, y, 0), randomTile);
                }
            }
        }

        /// <summary>
        /// Tạo walls với proper corners và edges
        /// </summary>
        private void CreateAutoFilledWalls(Transform parent)
        {
            GameObject gridObj = new GameObject("WallsGrid");
            gridObj.transform.SetParent(parent);
            gridObj.transform.localPosition = Vector3.zero;
            
            Grid grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0);
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            
            GameObject tilemapObj = new GameObject("WallsTilemap");
            tilemapObj.transform.SetParent(gridObj.transform);
            tilemapObj.transform.localPosition = Vector3.zero;
            
            Tilemap tilemap = tilemapObj.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObj.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = 0;
            
            // Add collision for walls
            TilemapCollider2D tilemapCollider = tilemapObj.AddComponent<TilemapCollider2D>();
            Rigidbody2D rb = tilemapObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static; // Walls don't move
            
            CompositeCollider2D compositeCollider = tilemapObj.AddComponent<CompositeCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge; // Optimize collision with CompositeCollider2D
            
            int tilesX = currentRoom.actualSize.x * (int)tileSize;
            int tilesY = currentRoom.actualSize.y * (int)tileSize;
            
            // Tính door tile positions - xóa CHỈ 1 tile tại vị trí door (cả edge lẫn inner layer)
            var doorTilePositions = new System.Collections.Generic.HashSet<Vector2Int>();
            
            foreach (var door in roomData.doorAnchors)
            {
                if (activeDirections != null && !activeDirections.Contains(door.direction))
                    continue;
                
                switch (door.direction)
                {
                    case DoorDirection.Top:
                    {
                        int cx = tilesX / 2 + (int)(door.localPosition.x);
                        doorTilePositions.Add(new Vector2Int(cx, tilesY - 1)); // Edge tile
                        doorTilePositions.Add(new Vector2Int(cx, tilesY - 2)); // Inner tile
                        break;
                    }
                    case DoorDirection.Bottom:
                    {
                        int cx = tilesX / 2 + (int)(door.localPosition.x);
                        doorTilePositions.Add(new Vector2Int(cx, 0));  // Edge tile
                        doorTilePositions.Add(new Vector2Int(cx, 1));  // Inner tile
                        break;
                    }
                    case DoorDirection.Left:
                    {
                        int cy = tilesY / 2 + (int)(door.localPosition.y);
                        doorTilePositions.Add(new Vector2Int(0, cy));  // Edge tile
                        doorTilePositions.Add(new Vector2Int(1, cy));  // Inner tile
                        break;
                    }
                    case DoorDirection.Right:
                    {
                        int cy = tilesY / 2 + (int)(door.localPosition.y);
                        doorTilePositions.Add(new Vector2Int(tilesX - 1, cy)); // Edge tile
                        doorTilePositions.Add(new Vector2Int(tilesX - 2, cy)); // Inner tile
                        break;
                    }
                }
            }
            
            // Fill walls với proper tiles dựa vào vị trí
            for (int x = 0; x < tilesX; x++)
            {
                for (int y = 0; y < tilesY; y++)
                {
                    bool isLeft = x == 0;
                    bool isRight = x == tilesX - 1;
                    bool isTop = y == tilesY - 1;
                    bool isBottom = y == 0;
                    
                    bool isEdge = (isLeft || isRight || isTop || isBottom);
                    if (!isEdge) continue; // Chỉ fill edges
                    
                    // Skip wall tile nếu trùng với door position
                    if (doorTilePositions.Contains(new Vector2Int(x, y)))
                        continue;
                    
                    TileBase wallTileToUse = GetWallTileForPosition(x, y, tilesX, tilesY);
                    if (wallTileToUse != null)
                    {
                        tilemap.SetTile(new Vector3Int(x, y, 0), wallTileToUse);
                    }
                }
            }
        }

        /// <summary>
        /// Chọn wall tilephù hợp dựa vào vị trí
        /// </summary>
        private TileBase GetWallTileForPosition(int x, int y, int maxX, int maxY)
        {
            bool isLeft = x == 0;
            bool isRight = x == maxX - 1;
            bool isTop = y == maxY - 1;
            bool isBottom = y == 0;
            
            // Corners
            if (isTop && isLeft) return wallTopLeft ?? wallCenter;
            if (isTop && isRight) return wallTopRight ?? wallCenter;
            if (isBottom && isLeft) return wallBottomLeft ?? wallCenter;
            if (isBottom && isRight) return wallBottomRight ?? wallCenter;
            
            // Edges
            if (isTop) return wallTop ?? wallCenter;
            if (isBottom) return wallBottom ?? wallCenter;
            if (isLeft) return wallLeft ?? wallCenter;
            if (isRight) return wallRight ?? wallCenter;
            
            return wallCenter;
        }
        
        /// <summary>
        /// Tạo door prefabs (với animation) thay vì markers
        /// </summary>
        private void GenerateDoorPrefabs(Transform parent)
        {
            if (roomData.doorAnchors == null || roomData.doorAnchors.Count == 0)
                return;
            
            GameObject doorContainer = new GameObject("Doors");
            doorContainer.transform.SetParent(parent);
            doorContainer.transform.localPosition = Vector3.zero;
            
            float roomWidth = currentRoom.actualSize.x * tileSize;
            float roomHeight = currentRoom.actualSize.y * tileSize;
            
            foreach (var door in roomData.doorAnchors)
            {
                // Chỉ tạo door nếu hướng này có room kế bên
                if (activeDirections != null && !activeDirections.Contains(door.direction))
                    continue;
                
                GameObject doorObj = Instantiate(doorPrefab, doorContainer.transform);
                doorObj.name = $"Door_{door.direction}";
                
                Vector3 doorPos = GetDoorPosition(door, roomWidth, roomHeight);
                Quaternion doorRot = GetDoorRotation(door.direction);
                Vector3 doorScale = GetDoorScale(door.direction);
                
                doorObj.transform.localPosition = doorPos;
                doorObj.transform.localRotation = doorRot;
                doorObj.transform.localScale = doorScale;
                
                // ADD DoorTrigger component và configure
                var doorTrigger = doorObj.GetComponent<DoorTrigger>();
                if (doorTrigger == null)
                    doorTrigger = doorObj.AddComponent<DoorTrigger>();
                
                // Ensure BoxCollider2D exists for trigger detection
                var boxCollider = doorObj.GetComponent<BoxCollider2D>();
                if (boxCollider == null)
                {
                    boxCollider = doorObj.AddComponent<BoxCollider2D>();
                    boxCollider.size = new Vector2(2f, 2f); // Door trigger area
                }
                // KHÔNG set isTrigger ở đây - DoorTrigger.Awake() sẽ quyết định
                // Door bắt đầu ĐÓNG (solid wall) → chỉ thành trigger khi mở
                boxCollider.isTrigger = false;
                
                // Thêm Rigidbody2D static để collider chặn player khi đóng
                var doorRb = doorObj.GetComponent<Rigidbody2D>();
                if (doorRb == null)
                {
                    doorRb = doorObj.AddComponent<Rigidbody2D>();
                    doorRb.bodyType = RigidbodyType2D.Static;
                }
                
                // Configure DoorTrigger
                doorTrigger.doorDirection = door.direction;
                doorTrigger.currentRoom = currentRoom;
                
                // Get target room from connectedRooms
                if (connectedRooms != null && connectedRooms.ContainsKey(door.direction))
                {
                    doorTrigger.targetRoom = connectedRooms[door.direction];
                }
                else
                {
                    Debug.LogWarning($"[RoomVisualGenerator] No connected room for door direction {door.direction}");
                }
                
                doorObj.transform.localPosition = doorPos;
                doorObj.transform.localRotation = doorRot;
            }
        }
        
        /// <summary>
        /// Tính position cho door - door nằm ngay trên center của wall tile
        /// </summary>
        private Vector3 GetDoorPosition(DoorAnchor door, float roomWidth, float roomHeight)
        {
            // Door nằm ngay trên wall edge (center của wall tile đầu tiên/cuối)
            // tileSize/2 để đặt vào center của wall tile
            float wallCenterOffset = tileSize / 2f;
            
            switch (door.direction)
            {
                case DoorDirection.Top:
                    // Top wall: y = roomHeight - tileSize/2 (center của top wall tile row)
                    return new Vector3(roomWidth / 2f + door.localPosition.x * tileSize, roomHeight - wallCenterOffset, 0);
                case DoorDirection.Bottom:
                    // Bottom wall: y = tileSize/2 (center của bottom wall tile row)
                    return new Vector3(roomWidth / 2f + door.localPosition.x * tileSize, wallCenterOffset, 0);
                case DoorDirection.Left:
                    // Left wall: x = tileSize/2 (center của left wall tile column)
                    return new Vector3(wallCenterOffset, roomHeight / 2f + door.localPosition.y * tileSize, 0);
                case DoorDirection.Right:
                    // Right wall: x = roomWidth - tileSize/2 (center của right wall tile column)
                    return new Vector3(roomWidth - wallCenterOffset, roomHeight / 2f + door.localPosition.y * tileSize, 0);
                default:
                    return Vector3.zero;
            }
        }
        
        /// <summary>
        /// Tính rotation cho door
        /// Top/Bottom: sprite giữ nguyên vertical
        /// Left/Right: rotate 90 degrees để horizontal
        /// </summary>
        private Quaternion GetDoorRotation(DoorDirection direction)
        {
            switch (direction)
            {
                case DoorDirection.Top:
                    return Quaternion.identity; // Vertical, mở lên trên
                case DoorDirection.Bottom:
                    return Quaternion.identity; // Vertical, giữ nguyên (không flip)
                case DoorDirection.Left:
                    return Quaternion.Euler(0, 0, 90); // Rotate 90 để horizontal
                case DoorDirection.Right:
                    return Quaternion.Euler(0, 0, -90); // Rotate -90 để horizontal  
                default:
                    return Quaternion.identity;
            }
        }
        
        /// <summary>
        /// Tính scale cho door - flip horizontal cho Left/Right
        /// </summary>
        private Vector3 GetDoorScale(DoorDirection direction)
        {
            switch (direction)
            {
                case DoorDirection.Left:
                    return new Vector3(-1, 1, 1); // Flip horizontal
                case DoorDirection.Right:
                    return new Vector3(1, 1, 1); // Normal
                case DoorDirection.Top:
                case DoorDirection.Bottom:
                default:
                    return new Vector3(1, 1, 1); // Normal
            }
        }
        
        
        /// Tạo sprite vuông đơn giản - sử dụng Unity built-in sprite
        
        private void CreateSquareSprite()
        {
            if (squareSprite != null) return;
            
            // Tạo texture 16x16 filled hoàn toàn
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            
            // Fill toàn bộ texture với màu trắng opaque
            Color32[] pixels = new Color32[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255); // White, fully opaque
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true); // Apply and make read-only
            
            squareSprite = Sprite.Create(
                texture, 
                new Rect(0, 0, 16, 16), 
                new Vector2(0.5f, 0.5f), 
                16f, // pixels per unit
                0, 
                SpriteMeshType.FullRect
            );
        }

        /// <summary>
        /// Lấy vị trí tile trung tâm của door theo hướng
        /// </summary>
        private Vector2Int GetDoorTileCenter(DoorAnchor door, int tilesX, int tilesY)
        {
            switch (door.direction)
            {
                case DoorDirection.Top:
                    return new Vector2Int(tilesX / 2 + (int)(door.localPosition.x), tilesY - 1);
                case DoorDirection.Bottom:
                    return new Vector2Int(tilesX / 2 + (int)(door.localPosition.x), 0);
                case DoorDirection.Left:
                    return new Vector2Int(0, tilesY / 2 + (int)(door.localPosition.y));
                case DoorDirection.Right:
                    return new Vector2Int(tilesX - 1, tilesY / 2 + (int)(door.localPosition.y));
                default:
                    return Vector2Int.zero;
            }
        }

        /// <summary>
        /// Spawn decor items sau khi floor+wall đã xong
        /// </summary>
        private void GenerateDecorations(Transform parent, DecorData decorData)
        {
            if (decorData == null || decorData.items == null || decorData.items.Length == 0) return;

            GameObject decorContainer = new GameObject("Decorations");
            decorContainer.transform.SetParent(parent);
            decorContainer.transform.localPosition = Vector3.zero;

            int tilesX = currentRoom.actualSize.x * (int)tileSize;
            int tilesY = currentRoom.actualSize.y * (int)tileSize;

            // BƯỜC 1: Tạo grid vị trí hợp lệ (interior, cách wall 2 tile)
            bool[,] validPositions = new bool[tilesX, tilesY];
            for (int x = 2; x < tilesX - 2; x++)
                for (int y = 2; y < tilesY - 2; y++)
                    validPositions[x, y] = true;

            // BƯỜC 2: Loại bỏ vùng gần cửa
            foreach (var door in roomData.doorAnchors)
            {
                if (!activeDirections.Contains(door.direction)) continue;

                Vector2Int doorCenter = GetDoorTileCenter(door, tilesX, tilesY);
                for (int dx = -decorData.doorClearance; dx <= decorData.doorClearance; dx++)
                {
                    for (int dy = -decorData.doorClearance; dy <= decorData.doorClearance; dy++)
                    {
                        int cx = doorCenter.x + dx;
                        int cy = doorCenter.y + dy;
                        if (cx >= 0 && cx < tilesX && cy >= 0 && cy < tilesY)
                            validPositions[cx, cy] = false;
                    }
                }
            }

            // BƯỜC 3: Chọn random vị trí + spawn
            var candidates = new System.Collections.Generic.List<Vector2Int>();
            for (int x = 0; x < tilesX; x++)
                for (int y = 0; y < tilesY; y++)
                    if (validPositions[x, y])
                        candidates.Add(new Vector2Int(x, y));

            Core.DungeonUtils.Shuffle(candidates);

            int maxDecors = Mathf.RoundToInt(candidates.Count * decorData.density);
            int placed = 0;

            foreach (var pos in candidates)
            {
                if (placed >= maxDecors) break;
                if (!validPositions[pos.x, pos.y]) continue;

                DecorItem item = WeightedRandomSelect(decorData.items);
                if (item.prefab == null) continue;

                Vector3 worldPos = new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0);
                GameObject decor = Instantiate(item.prefab, decorContainer.transform);
                decor.transform.localPosition = worldPos;

                // Invalidate vùng xung quanh (minSpacing)
                for (int dx = -decorData.minSpacing; dx <= decorData.minSpacing; dx++)
                    for (int dy = -decorData.minSpacing; dy <= decorData.minSpacing; dy++)
                    {
                        int nx = pos.x + dx;
                        int ny = pos.y + dy;
                        if (nx >= 0 && nx < tilesX && ny >= 0 && ny < tilesY)
                            validPositions[nx, ny] = false;
                    }

                placed++;
            }
        }

        /// <summary>
        /// Chọn ngẫu nhiên có trọng số (weighted random)
        /// </summary>
        private DecorItem WeightedRandomSelect(DecorItem[] items)
        {
            float totalWeight = 0;
            foreach (var item in items) totalWeight += item.weight;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0;

            foreach (var item in items)
            {
                cumulative += item.weight;
                if (roll <= cumulative) return item;
            }
            return items[items.Length - 1];
        }
    }
}
===
using UnityEngine;
using UnityEngine.Tilemaps;
using ProceduralGeneration.Data;

namespace ProceduralGeneration.Components
{
    
    /// Tự động generate visuals cho rooms (floor, walls, doors)
    
    public class RoomVisualGenerator : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Color floorColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Màu xám đậm
        [SerializeField] private Color wallColor = new Color(0.2f, 0.2f, 0.25f, 1f); // Màu xám xanh
        [SerializeField] private Color doorColor = new Color(0.6f, 0.4f, 0.2f, 1f); // Màu nâu
        
        [Header("Tile Settings")]
        [SerializeField] private float tileSize = 1f; // 1 grid cell = 1 world unit
        [SerializeField] private float wallThickness = 0.15f; // Thin for door markers
        
        [Header("Auto-Fill Tiles")]
        [Tooltip("Bật auto-fill tiles (nếu tắt sẽ tạo placeholder để vẽ manual)")]
        [SerializeField] private bool autoFillTiles = true;
        
        [Header("Floor Tiles")]
        [SerializeField] private TileBase[] floorTiles; // Random chọn từ mảng này
        
        [Header("Wall Tiles")]
        [SerializeField] private TileBase wallCenter; // Wall ở giữa
        [SerializeField] private TileBase wallTopLeft; // Góc trên trái
        [SerializeField] private TileBase wallTopRight; // Góc trên phải
        [SerializeField] private TileBase wallBottomLeft; // Góc dưới trái
        [SerializeField] private TileBase wallBottomRight; // Góc dưới phải
        [SerializeField] private TileBase wallTop; // Tường trên (horizontal)
        [SerializeField] private TileBase wallBottom; // Tường dưới (horizontal)
        [SerializeField] private TileBase wallLeft; // Tường trái (vertical)
        [SerializeField] private TileBase wallRight; // Tường phải (vertical)
        
        [Header("Door Prefab")]
        [SerializeField] private GameObject doorPrefab; // Prefab có animation mở/đóng
        
        [Header("Trap Settings")]
        [SerializeField] private Data.TrapData[] trapTypes; // Loại trap có thể spawn
        
        private RoomData roomData;
        private Sprite squareSprite;
        private Tile defaultFloorTile; // Runtime-generated tile if no asset assigned
        private System.Collections.Generic.HashSet<DoorDirection> activeDirections; // Hướng có room kế bên
        private Core.Room currentRoom; // Room hiện tại (for DoorTrigger)
        private System.Collections.Generic.Dictionary<DoorDirection, Core.Room> connectedRooms; // Rooms kế bên
        
        /// <summary>
        /// Configure tiles từ DungeonManager (vì component được add runtime)
        /// </summary>
        public void ConfigureTiles(bool autoFill, TileBase[] floors, TileBase center,
            TileBase topL, TileBase topR, TileBase botL, TileBase botR,
            TileBase top, TileBase bottom, TileBase left, TileBase right,
            GameObject door, Data.TrapData[] traps)
        {
            autoFillTiles = autoFill;
            floorTiles = floors;
            wallCenter = center;
            wallTopLeft = topL;
            wallTopRight = topR;
            wallBottomLeft = botL;
            wallBottomRight = botR;
            wallTop = top;
            wallBottom = bottom;
            wallLeft = left;
            wallRight = right;
            doorPrefab = door;
            trapTypes = traps;
        }
        
        /// <summary>
        /// Set current room reference (for DoorTrigger)
        /// </summary>
        public void SetCurrentRoom(Core.Room room)
        {
            currentRoom = room;
        }
        
        
        /// Generate visuals cho room
        
        public void GenerateVisuals(RoomData data, System.Collections.Generic.Dictionary<DoorDirection, Core.Room> connections = null)
        {
            roomData = data;
            connectedRooms = connections;
            
            // Lưu các hướng có room kế bên
            activeDirections = new System.Collections.Generic.HashSet<DoorDirection>();
            if (connections != null)
            {
                foreach (var kvp in connections)
                {
                    activeDirections.Add(kvp.Key);
                }
            }
            
            // Tạo container
            GameObject visualContainer = new GameObject("Visuals");
            visualContainer.transform.SetParent(transform);
            visualContainer.transform.localPosition = Vector3.zero;
            
            // worldScale đã được apply trong Room.InstantiateRoom, không cần scale lại
            
            // Tạo Floor Tilemap
            if (autoFillTiles && floorTiles != null && floorTiles.Length > 0)
            {
                CreateAutoFilledFloor(visualContainer.transform);
            }
            else
            {
                CreateEmptyTilemap(visualContainer.transform, "Floor", -10);
            }
            
            // Tạo Walls Tilemap
            if (autoFillTiles && wallCenter != null)
            {
                CreateAutoFilledWalls(visualContainer.transform);
            }
            else
            {
                CreateEmptyTilemap(visualContainer.transform, "Walls", 0);
            }
            
            // Tạo doors (prefab với animation hoặc markers)
            if (doorPrefab != null)
            {
                GenerateDoorPrefabs(visualContainer.transform);
            }
            else
            {
                GenerateDoorMarkers(visualContainer.transform);
            }

            // Spawn decorations nếu có cấu hình
            if (roomData.decorData != null)
            {
                GenerateDecorations(visualContainer.transform, roomData.decorData);
            }
        }
        
        /// <summary>
        /// Generate chỉ doors cho phòng prefab (đã có floor/wall sẵn)
        /// </summary>
        public void GenerateDoorsOnly(RoomData data, System.Collections.Generic.Dictionary<DoorDirection, Core.Room> connections = null)
        {
            roomData = data;
            connectedRooms = connections;

            activeDirections = new System.Collections.Generic.HashSet<DoorDirection>();
            if (connections != null)
            {
                foreach (var kvp in connections)
                    activeDirections.Add(kvp.Key);
            }

            // Tìm hoặc tạo container cho doors
            Transform doorParent = transform.Find("Doors");
            if (doorParent == null)
            {
                GameObject doorsGo = new GameObject("Doors");
                doorsGo.transform.SetParent(transform);
                doorsGo.transform.localPosition = Vector3.zero;
                doorParent = doorsGo.transform;
            }

            if (doorPrefab != null)
                GenerateDoorPrefabs(doorParent);
            else
                GenerateDoorMarkers(doorParent);
        }

        
        /// Tạo empty Tilemap structure (Grid + Tilemap) - với placeholder tiles
        
        private void CreateEmptyTilemap(Transform parent, string name, int sortingOrder)
        {
            // Tạo Grid object
            GameObject gridObj = new GameObject($"{name}Grid");
            gridObj.transform.SetParent(parent);
            gridObj.transform.localPosition = Vector3.zero;
            
            Grid grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0); // Cell size = 1 unit (match sprite size 16px/16PPU)
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            
            // Tạo Tilemap
            GameObject tilemapObj = new GameObject($"{name}Tilemap");
            tilemapObj.transform.SetParent(gridObj.transform);
            tilemapObj.transform.localPosition = Vector3.zero;
            
            Tilemap tilemap = tilemapObj.AddComponent<Tilemap>();
            TilemapRenderer tilemapRenderer = tilemapObj.AddComponent<TilemapRenderer>();
            
            // Setup renderer
            tilemapRenderer.sortingOrder = sortingOrder;
            tilemapRenderer.sortingLayerName = "Default";
            tilemapRenderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Auto;
            
            // Pre-fill với placeholder tiles để show bounds
            CreateSquareSprite();
            Tile placeholderTile = ScriptableObject.CreateInstance<Tile>();
            placeholderTile.sprite = squareSprite;
            placeholderTile.color = name == "Floor" ? new Color(0.3f, 0.3f, 0.3f, 0.3f) : new Color(0.5f, 0.5f, 0.5f, 0.3f); // Semi-transparent
            
            // Fill tiles theo room size (in world units, not room units)
            // Room size 1×1 = 10×10 world units = 10×10 grid cells (vì cell = 1 unit)
            int tilesX = currentRoom.actualSize.x * (int)tileSize; // Ví dụ: 1 * 10 = 10 cells
            int tilesY = currentRoom.actualSize.y * (int)tileSize; // Ví dụ: 1 * 10 = 10 cells
            
            for (int x = 0; x < tilesX; x++)
            {
                for (int y = 0; y < tilesY; y++)
                {
                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    tilemap.SetTile(tilePos, placeholderTile);
                }
            }
        }
        
        
        /// Tạo door markers để chỉ vị trí doors (chỉ cho các hướng có room kế bên)
        
        private void GenerateDoorMarkers(Transform parent)
        {
            if (roomData.doorAnchors == null || roomData.doorAnchors.Count == 0)
                return;
            
            CreateSquareSprite(); // Tạo sprite cho door markers
            
            GameObject doorContainer = new GameObject("DoorMarkers");
            doorContainer.transform.SetParent(parent);
            doorContainer.transform.localPosition = Vector3.zero;
            
            float roomWidth = currentRoom.actualSize.x * tileSize;
            float roomHeight = currentRoom.actualSize.y * tileSize;
            
            // Scale door markers theo worldScale
            float doorSize = 1.5f * currentRoom.worldScale;
            float scaledWallThickness = wallThickness * currentRoom.worldScale;
            
            foreach (var door in roomData.doorAnchors)
            {
                // Chỉ tạo door marker nếu hướng này có room kế bên
                if (activeDirections != null && !activeDirections.Contains(door.direction))
                {
                    continue;
                }
                GameObject doorMarker = new GameObject($"DoorMarker_{door.direction}");
                doorMarker.transform.SetParent(doorContainer.transform);
                
                float doorInset = tileSize; // Door markers cũng lùi vào trong 1 tile
                Vector3 doorPos = Vector3.zero;
                Vector3 doorScale = new Vector3(doorSize, doorSize, 1);
                
                // Tính position của door - offset vào trong
                switch (door.direction)
                {
                    case DoorDirection.Top:
                        doorPos = new Vector3(roomWidth / 2f + door.localPosition.x * tileSize, roomHeight - doorInset, -0.1f);
                        doorScale = new Vector3(doorSize, scaledWallThickness, 1);
                        break;
                    case DoorDirection.Bottom:
                        doorPos = new Vector3(roomWidth / 2f + door.localPosition.x * tileSize, doorInset, -0.1f);
                        doorScale = new Vector3(doorSize, scaledWallThickness, 1);
                        break;
                    case DoorDirection.Left:
                        doorPos = new Vector3(doorInset, roomHeight / 2f + door.localPosition.y * tileSize, -0.1f);
                        doorScale = new Vector3(scaledWallThickness, doorSize, 1);
                        break;
                    case DoorDirection.Right:
                        doorPos = new Vector3(roomWidth - doorInset, roomHeight / 2f + door.localPosition.y * tileSize, -0.1f);
                        doorScale = new Vector3(scaledWallThickness, doorSize, 1);
                        break;
                }
                
                doorMarker.transform.localPosition = doorPos;
                doorMarker.transform.localScale = doorScale;
                
                SpriteRenderer sr = doorMarker.AddComponent<SpriteRenderer>();
                sr.sprite = squareSprite;
                sr.color = doorColor; // Cửa hiển thị bình thường, hệ thống ánh sáng quyết định tầm nhìn
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 10;
            }
        }
        
        /// <summary>
        /// Tạo floor với auto-fill random tiles
        /// </summary>
        private void CreateAutoFilledFloor(Transform parent)
        {
            GameObject gridObj = new GameObject("FloorGrid");
            gridObj.transform.SetParent(parent);
            gridObj.transform.localPosition = Vector3.zero;
            
            Grid grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0);
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            
            GameObject tilemapObj = new GameObject("FloorTilemap");
            tilemapObj.transform.SetParent(gridObj.transform);
            tilemapObj.transform.localPosition = Vector3.zero;
            
            Tilemap tilemap = tilemapObj.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObj.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = -10;
            
            int tilesX = currentRoom.actualSize.x * (int)tileSize;
            int tilesY = currentRoom.actualSize.y * (int)tileSize;
            
            // Fill sàn CHỈ interior (bỏ edges vì đó là chỗ có wall)
            for (int x = 1; x < tilesX - 1; x++)
            {
                for (int y = 1; y < tilesY - 1; y++)
                {
                    TileBase randomTile = floorTiles[Random.Range(0, floorTiles.Length)];
                    tilemap.SetTile(new Vector3Int(x, y, 0), randomTile);
                }
            }
        }

        /// <summary>
        /// Tạo walls với proper corners và edges
        /// </summary>
        private void CreateAutoFilledWalls(Transform parent)
        {
            GameObject gridObj = new GameObject("WallsGrid");
            gridObj.transform.SetParent(parent);
            gridObj.transform.localPosition = Vector3.zero;
            
            Grid grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0);
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            
            GameObject tilemapObj = new GameObject("WallsTilemap");
            tilemapObj.transform.SetParent(gridObj.transform);
            tilemapObj.transform.localPosition = Vector3.zero;
            
            Tilemap tilemap = tilemapObj.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObj.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = 0;
            
            // Add collision for walls
            TilemapCollider2D tilemapCollider = tilemapObj.AddComponent<TilemapCollider2D>();
            Rigidbody2D rb = tilemapObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static; // Walls don't move
            
            CompositeCollider2D compositeCollider = tilemapObj.AddComponent<CompositeCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge; // Optimize collision with CompositeCollider2D
            
            int tilesX = currentRoom.actualSize.x * (int)tileSize;
            int tilesY = currentRoom.actualSize.y * (int)tileSize;
            
            // Tính door tile positions - xóa CHỈ 1 tile tại vị trí door (cả edge lẫn inner layer)
            var doorTilePositions = new System.Collections.Generic.HashSet<Vector2Int>();
            
            foreach (var door in roomData.doorAnchors)
            {
                if (activeDirections != null && !activeDirections.Contains(door.direction))
                    continue;
                
                switch (door.direction)
                {
                    case DoorDirection.Top:
                    {
                        int cx = tilesX / 2 + (int)(door.localPosition.x);
                        doorTilePositions.Add(new Vector2Int(cx, tilesY - 1)); // Edge tile
                        doorTilePositions.Add(new Vector2Int(cx, tilesY - 2)); // Inner tile
                        break;
                    }
                    case DoorDirection.Bottom:
                    {
                        int cx = tilesX / 2 + (int)(door.localPosition.x);
                        doorTilePositions.Add(new Vector2Int(cx, 0));  // Edge tile
                        doorTilePositions.Add(new Vector2Int(cx, 1));  // Inner tile
                        break;
                    }
                    case DoorDirection.Left:
                    {
                        int cy = tilesY / 2 + (int)(door.localPosition.y);
                        doorTilePositions.Add(new Vector2Int(0, cy));  // Edge tile
                        doorTilePositions.Add(new Vector2Int(1, cy));  // Inner tile
                        break;
                    }
                    case DoorDirection.Right:
                    {
                        int cy = tilesY / 2 + (int)(door.localPosition.y);
                        doorTilePositions.Add(new Vector2Int(tilesX - 1, cy)); // Edge tile
                        doorTilePositions.Add(new Vector2Int(tilesX - 2, cy)); // Inner tile
                        break;
                    }
                }
            }
            
            // Fill walls với proper tiles dựa vào vị trí
            for (int x = 0; x < tilesX; x++)
            {
                for (int y = 0; y < tilesY; y++)
                {
                    bool isLeft = x == 0;
                    bool isRight = x == tilesX - 1;
                    bool isTop = y == tilesY - 1;
                    bool isBottom = y == 0;
                    
                    bool isEdge = (isLeft || isRight || isTop || isBottom);
                    if (!isEdge) continue; // Chỉ fill edges
                    
                    // Skip wall tile nếu trùng với door position
                    if (doorTilePositions.Contains(new Vector2Int(x, y)))
                        continue;
                    
                    TileBase wallTileToUse = GetWallTileForPosition(x, y, tilesX, tilesY);
                    if (wallTileToUse != null)
                    {
                        tilemap.SetTile(new Vector3Int(x, y, 0), wallTileToUse);
                    }
                }
            }
        }

        /// <summary>
        /// Chọn wall tilephù hợp dựa vào vị trí
        /// </summary>
        private TileBase GetWallTileForPosition(int x, int y, int maxX, int maxY)
        {
            bool isLeft = x == 0;
            bool isRight = x == maxX - 1;
            bool isTop = y == maxY - 1;
            bool isBottom = y == 0;
            
            // Corners
            if (isTop && isLeft) return wallTopLeft ?? wallCenter;
            if (isTop && isRight) return wallTopRight ?? wallCenter;
            if (isBottom && isLeft) return wallBottomLeft ?? wallCenter;
            if (isBottom && isRight) return wallBottomRight ?? wallCenter;
            
            // Edges
            if (isTop) return wallTop ?? wallCenter;
            if (isBottom) return wallBottom ?? wallCenter;
            if (isLeft) return wallLeft ?? wallCenter;
            if (isRight) return wallRight ?? wallCenter;
            
            return wallCenter;
        }
        
        /// <summary>
        /// Tạo door prefabs (với animation) thay vì markers
        /// </summary>
        private void GenerateDoorPrefabs(Transform parent)
        {
            if (roomData.doorAnchors == null || roomData.doorAnchors.Count == 0)
                return;
            
            GameObject doorContainer = new GameObject("Doors");
            doorContainer.transform.SetParent(parent);
            doorContainer.transform.localPosition = Vector3.zero;
            
            float roomWidth = currentRoom.actualSize.x * tileSize;
            float roomHeight = currentRoom.actualSize.y * tileSize;
            
            foreach (var door in roomData.doorAnchors)
            {
                // Chỉ tạo door nếu hướng này có room kế bên
                if (activeDirections != null && !activeDirections.Contains(door.direction))
                    continue;
                
                GameObject doorObj = Instantiate(doorPrefab, doorContainer.transform);
                doorObj.name = $"Door_{door.direction}";
                
                Vector3 doorPos = GetDoorPosition(door, roomWidth, roomHeight);
                Quaternion doorRot = GetDoorRotation(door.direction);
                Vector3 doorScale = GetDoorScale(door.direction);
                
                doorObj.transform.localPosition = doorPos;
                doorObj.transform.localRotation = doorRot;
                doorObj.transform.localScale = doorScale;
                
                // ADD DoorTrigger component và configure
                var doorTrigger = doorObj.GetComponent<DoorTrigger>();
                if (doorTrigger == null)
                    doorTrigger = doorObj.AddComponent<DoorTrigger>();
                
                // Ensure BoxCollider2D exists for trigger detection
                var boxCollider = doorObj.GetComponent<BoxCollider2D>();
                if (boxCollider == null)
                {
                    boxCollider = doorObj.AddComponent<BoxCollider2D>();
                    boxCollider.size = new Vector2(2f, 2f); // Door trigger area
                }
                // KHÔNG set isTrigger ở đây - DoorTrigger.Awake() sẽ quyết định
                // Door bắt đầu ĐÓNG (solid wall) → chỉ thành trigger khi mở
                boxCollider.isTrigger = false;
                
                // Thêm Rigidbody2D static để collider chặn player khi đóng
                var doorRb = doorObj.GetComponent<Rigidbody2D>();
                if (doorRb == null)
                {
                    doorRb = doorObj.AddComponent<Rigidbody2D>();
                    doorRb.bodyType = RigidbodyType2D.Static;
                }
                
                // Configure DoorTrigger
                doorTrigger.doorDirection = door.direction;
                doorTrigger.currentRoom = currentRoom;
                
                // Get target room from connectedRooms
                if (connectedRooms != null && connectedRooms.ContainsKey(door.direction))
                {
                    doorTrigger.targetRoom = connectedRooms[door.direction];
                }
                else
                {
                    Debug.LogWarning($"[RoomVisualGenerator] No connected room for door direction {door.direction}");
                }
                
                doorObj.transform.localPosition = doorPos;
                doorObj.transform.localRotation = doorRot;
            }
        }
        
        /// <summary>
        /// Tính position cho door - door nằm ngay trên center của wall tile
        /// </summary>
        private Vector3 GetDoorPosition(DoorAnchor door, float roomWidth, float roomHeight)
        {
            // Door nằm ngay trên wall edge (center của wall tile đầu tiên/cuối)
            // tileSize/2 để đặt vào center của wall tile
            float wallCenterOffset = tileSize / 2f;
            
            switch (door.direction)
            {
                case DoorDirection.Top:
                    // Top wall: y = roomHeight - tileSize/2 (center của top wall tile row)
                    return new Vector3(roomWidth / 2f + door.localPosition.x * tileSize, roomHeight - wallCenterOffset, 0);
                case DoorDirection.Bottom:
                    // Bottom wall: y = tileSize/2 (center của bottom wall tile row)
                    return new Vector3(roomWidth / 2f + door.localPosition.x * tileSize, wallCenterOffset, 0);
                case DoorDirection.Left:
                    // Left wall: x = tileSize/2 (center của left wall tile column)
                    return new Vector3(wallCenterOffset, roomHeight / 2f + door.localPosition.y * tileSize, 0);
                case DoorDirection.Right:
                    // Right wall: x = roomWidth - tileSize/2 (center của right wall tile column)
                    return new Vector3(roomWidth - wallCenterOffset, roomHeight / 2f + door.localPosition.y * tileSize, 0);
                default:
                    return Vector3.zero;
            }
        }
        
        /// <summary>
        /// Tính rotation cho door
        /// Top/Bottom: sprite giữ nguyên vertical
        /// Left/Right: rotate 90 degrees để horizontal
        /// </summary>
        private Quaternion GetDoorRotation(DoorDirection direction)
        {
            switch (direction)
            {
                case DoorDirection.Top:
                    return Quaternion.identity; // Vertical, mở lên trên
                case DoorDirection.Bottom:
                    return Quaternion.identity; // Vertical, giữ nguyên (không flip)
                case DoorDirection.Left:
                    return Quaternion.Euler(0, 0, 90); // Rotate 90 để horizontal
                case DoorDirection.Right:
                    return Quaternion.Euler(0, 0, -90); // Rotate -90 để horizontal  
                default:
                    return Quaternion.identity;
            }
        }
        
        /// <summary>
        /// Tính scale cho door - flip horizontal cho Left/Right
        /// </summary>
        private Vector3 GetDoorScale(DoorDirection direction)
        {
            switch (direction)
            {
                case DoorDirection.Left:
                    return new Vector3(-1, 1, 1); // Flip horizontal
                case DoorDirection.Right:
                    return new Vector3(1, 1, 1); // Normal
                case DoorDirection.Top:
                case DoorDirection.Bottom:
                default:
                    return new Vector3(1, 1, 1); // Normal
            }
        }
        
        
        /// Tạo sprite vuông đơn giản - sử dụng Unity built-in sprite
        
        private void CreateSquareSprite()
        {
            if (squareSprite != null) return;
            
            // Tạo texture 16x16 filled hoàn toàn
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            
            // Fill toàn bộ texture với màu trắng opaque
            Color32[] pixels = new Color32[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255); // White, fully opaque
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true); // Apply and make read-only
            
            squareSprite = Sprite.Create(
                texture, 
                new Rect(0, 0, 16, 16), 
                new Vector2(0.5f, 0.5f), 
                16f, // pixels per unit
                0, 
                SpriteMeshType.FullRect
            );
        }

        /// <summary>
        /// Lấy vị trí tile trung tâm của door theo hướng
        /// </summary>
        private Vector2Int GetDoorTileCenter(DoorAnchor door, int tilesX, int tilesY)
        {
            switch (door.direction)
            {
                case DoorDirection.Top:
                    return new Vector2Int(tilesX / 2 + (int)(door.localPosition.x), tilesY - 1);
                case DoorDirection.Bottom:
                    return new Vector2Int(tilesX / 2 + (int)(door.localPosition.x), 0);
                case DoorDirection.Left:
                    return new Vector2Int(0, tilesY / 2 + (int)(door.localPosition.y));
                case DoorDirection.Right:
                    return new Vector2Int(tilesX - 1, tilesY / 2 + (int)(door.localPosition.y));
                default:
                    return Vector2Int.zero;
            }
        }

        /// <summary>
        /// Spawn decor items sau khi floor+wall đã xong
        /// Hỗ trợ: multi-tile size, blocksMovement collider, wall-aligned/corner placement,
        /// random flip/rotation, sorting order offset
        /// </summary>
        private void GenerateDecorations(Transform parent, DecorData decorData)
        {
            if (decorData == null || decorData.items == null || decorData.items.Length == 0) return;

            GameObject decorContainer = new GameObject("Decorations");
            decorContainer.transform.SetParent(parent);
            decorContainer.transform.localPosition = Vector3.zero;

            int tilesX = currentRoom.actualSize.x * (int)tileSize;
            int tilesY = currentRoom.actualSize.y * (int)tileSize;

            // BƯỚC 1: Tạo grid vị trí hợp lệ (interior, cách wall 2 tile)
            bool[,] validPositions = new bool[tilesX, tilesY];
            for (int x = 2; x < tilesX - 2; x++)
                for (int y = 2; y < tilesY - 2; y++)
                    validPositions[x, y] = true;

            // BƯỚC 2: Loại bỏ vùng gần cửa
            foreach (var door in roomData.doorAnchors)
            {
                if (!activeDirections.Contains(door.direction)) continue;

                Vector2Int doorCenter = GetDoorTileCenter(door, tilesX, tilesY);
                for (int dx = -decorData.doorClearance; dx <= decorData.doorClearance; dx++)
                {
                    for (int dy = -decorData.doorClearance; dy <= decorData.doorClearance; dy++)
                    {
                        int cx = doorCenter.x + dx;
                        int cy = doorCenter.y + dy;
                        if (cx >= 0 && cx < tilesX && cy >= 0 && cy < tilesY)
                            validPositions[cx, cy] = false;
                    }
                }
            }

            // BƯỚC 3: Phân nhóm items theo placement type
            var interiorItems = new System.Collections.Generic.List<DecorItem>();
            var wallItems = new System.Collections.Generic.List<DecorItem>();
            var cornerItems = new System.Collections.Generic.List<DecorItem>();

            foreach (var item in decorData.items)
            {
                if (item.prefab == null) continue;
                switch (item.placement)
                {
                    case DecorPlacement.WallAligned: wallItems.Add(item); break;
                    case DecorPlacement.Corner: cornerItems.Add(item); break;
                    default: interiorItems.Add(item); break;
                }
            }

            int maxDecors = 0;
            int placed = 0;

            // BƯỚC 4A: Spawn CORNER items (4 góc interior)
            if (cornerItems.Count > 0)
            {
                var cornerPositions = new Vector2Int[]
                {
                    new Vector2Int(2, 2),                           // Bottom-left
                    new Vector2Int(tilesX - 3, 2),                  // Bottom-right
                    new Vector2Int(2, tilesY - 3),                  // Top-left
                    new Vector2Int(tilesX - 3, tilesY - 3)          // Top-right
                };

                foreach (var pos in cornerPositions)
                {
                    if (!validPositions[pos.x, pos.y]) continue;

                    DecorItem item = WeightedRandomSelect(cornerItems.ToArray());
                    if (!CanPlaceDecorAt(pos, item, tilesX, tilesY, validPositions)) continue;

                    PlaceDecor(decorContainer.transform, pos, item, tilesX, tilesY, validPositions, decorData.minSpacing);
                }
            }

            // BƯỚC 4B: Spawn WALL-ALIGNED items (sát tường, row/column thứ 2)
            if (wallItems.Count > 0)
            {
                var wallCandidates = new System.Collections.Generic.List<Vector2Int>();

                // Row sát tường trên (y = tilesY - 3)
                for (int x = 2; x < tilesX - 2; x++)
                    if (validPositions[x, tilesY - 3]) wallCandidates.Add(new Vector2Int(x, tilesY - 3));
                // Row sát tường dưới (y = 2)
                for (int x = 2; x < tilesX - 2; x++)
                    if (validPositions[x, 2]) wallCandidates.Add(new Vector2Int(x, 2));
                // Column sát tường trái (x = 2)
                for (int y = 3; y < tilesY - 3; y++)
                    if (validPositions[2, y]) wallCandidates.Add(new Vector2Int(2, y));
                // Column sát tường phải (x = tilesX - 3)
                for (int y = 3; y < tilesY - 3; y++)
                    if (validPositions[tilesX - 3, y]) wallCandidates.Add(new Vector2Int(tilesX - 3, y));

                Core.DungeonUtils.Shuffle(wallCandidates);
                maxDecors = Mathf.RoundToInt(wallCandidates.Count * decorData.density);
                placed = 0;

                foreach (var pos in wallCandidates)
                {
                    if (placed >= maxDecors) break;
                    if (!validPositions[pos.x, pos.y]) continue;

                    DecorItem item = WeightedRandomSelect(wallItems.ToArray());
                    if (!CanPlaceDecorAt(pos, item, tilesX, tilesY, validPositions)) continue;

                    PlaceDecor(decorContainer.transform, pos, item, tilesX, tilesY, validPositions, decorData.minSpacing);
                    placed++;
                }
            }

            // BƯỚC 4C: Spawn INTERIOR items (giữa phòng)
            if (interiorItems.Count > 0)
            {
                var candidates = new System.Collections.Generic.List<Vector2Int>();
                for (int x = 0; x < tilesX; x++)
                    for (int y = 0; y < tilesY; y++)
                        if (validPositions[x, y])
                            candidates.Add(new Vector2Int(x, y));

                Core.DungeonUtils.Shuffle(candidates);
                maxDecors = Mathf.RoundToInt(candidates.Count * decorData.density);
                placed = 0;

                foreach (var pos in candidates)
                {
                    if (placed >= maxDecors) break;
                    if (!validPositions[pos.x, pos.y]) continue;

                    DecorItem item = WeightedRandomSelect(interiorItems.ToArray());
                    if (!CanPlaceDecorAt(pos, item, tilesX, tilesY, validPositions)) continue;

                    PlaceDecor(decorContainer.transform, pos, item, tilesX, tilesY, validPositions, decorData.minSpacing);
                    placed++;
                }
            }
        }

        /// <summary>
        /// Kiểm tra xem decor item (có thể multi-tile) có thể đặt tại vị trí này không
        /// </summary>
        private bool CanPlaceDecorAt(Vector2Int pos, DecorItem item, int tilesX, int tilesY, bool[,] validPositions)
        {
            for (int sx = 0; sx < item.size.x; sx++)
            {
                for (int sy = 0; sy < item.size.y; sy++)
                {
                    int fx = pos.x + sx;
                    int fy = pos.y + sy;
                    if (fx >= tilesX || fy >= tilesY || !validPositions[fx, fy])
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Thực sự instantiate decor, apply transforms, collider, sorting, rồi invalidate vùng xung quanh
        /// </summary>
        private void PlaceDecor(Transform container, Vector2Int pos, DecorItem item,
            int tilesX, int tilesY, bool[,] validPositions, int minSpacing)
        {
            // Tính world position (center of footprint)
            float worldX = pos.x + item.size.x * 0.5f;
            float worldY = pos.y + item.size.y * 0.5f;
            Vector3 worldPos = new Vector3(worldX, worldY, 0);

            GameObject decor = Instantiate(item.prefab, container);
            decor.transform.localPosition = worldPos;

            // Random flip horizontal
            if (item.allowRandomFlip && Random.value > 0.5f)
                decor.transform.localScale = new Vector3(-1, 1, 1);

            // Random rotation (0, 90, 180, 270)
            if (item.allowRandomRotation)
                decor.transform.localRotation = Quaternion.Euler(0, 0, 90f * Random.Range(0, 4));

            // BlocksMovement → add collider nếu chưa có
            if (item.blocksMovement && decor.GetComponent<Collider2D>() == null)
            {
                var box = decor.AddComponent<BoxCollider2D>();
                box.size = new Vector2(item.size.x, item.size.y);
            }

            // Apply sorting order offset
            var sr = decor.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder += item.sortingOrderOffset;

            // Invalidate vùng xung quanh (footprint + minSpacing buffer)
            for (int dx = -minSpacing; dx < item.size.x + minSpacing; dx++)
            {
                for (int dy = -minSpacing; dy < item.size.y + minSpacing; dy++)
                {
                    int nx = pos.x + dx;
                    int ny = pos.y + dy;
                    if (nx >= 0 && nx < tilesX && ny >= 0 && ny < tilesY)
                        validPositions[nx, ny] = false;
                }
            }
        }

        /// <summary>
        /// Chọn ngẫu nhiên có trọng số (weighted random)
        /// </summary>
        private DecorItem WeightedRandomSelect(DecorItem[] items)
        {
            float totalWeight = 0;
            foreach (var item in items) totalWeight += item.weight;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0;

            foreach (var item in items)
            {
                cumulative += item.weight;
                if (roll <= cumulative) return item;
            }
            return items[items.Length - 1];
        }
    }
}
```

---

## Hướng dẫn sử dụng trong Unity Editor

### Bước 1: Mở Project → Kiểm tra Compile

1. Mở Unity project
2. Chờ compile xong → **Console** không có error đỏ là OK
3. Nếu có error, xem section "Troubleshooting" ở cuối

---

### Bước 2: Tạo Decor Prefabs

Mỗi decor item cần 1 prefab. Tạo từ sprite bạn đã có:

1. **Kéo sprite** vào Scene (ví dụ: torch, barrel, skull, vase...)
2. Chỉnh position, scale cho vừa 1 tile (hoặc 2×1 nếu lớn)
3. **Drag từ Hierarchy → Project** để tạo Prefab
4. Lưu vào folder `Assets/Prefabs/Decor/` (tạo folder nếu chưa có)
5. Xóa object khỏi Scene

> [!TIP]
> Nếu chưa có sprite, dùng tạm bất kỳ sprite nào trong project. Ví dụ: tạo 1 empty GameObject + SpriteRenderer + gán sprite.

---

### Bước 3: Tạo DecorData ScriptableObject

1. Trong **Project window**, click phải vào folder `Assets/ScriptableObjects/` (tạo nếu chưa có)
2. Chọn **Create → Procedural Generation → Decor Data**
3. Đặt tên: `Arch1_DecorData` (cho phòng Archetype1) hoặc tên phù hợp

---

### Bước 4: Configure DecorData trong Inspector

Click vào file DecorData vừa tạo, trong **Inspector**:

#### 4.1 — Settings chung

| Field | Giá trị gợi ý | Ý nghĩa |
|-------|---------------|---------|
| **Density** | `0.10` – `0.20` | 10-20% số ô interior sẽ có decor |
| **Min Spacing** | `2` | Khoảng cách tối thiểu giữa 2 decor (tiles) |
| **Door Clearance** | `3` | Không đặt decor gần cửa 3 tiles |

#### 4.2 — Thêm Items vào pool

Click nút **+** ở **Items** array. Mỗi item cấu hình:

**Ví dụ 1: Torch (sát tường)**
| Field | Giá trị |
|-------|---------|
| Prefab | `Torch_Prefab` |
| Weight | `3` (xuất hiện nhiều) |
| Size | [(1, 1)](file:///d:/%21course/FPT/7.SP26/PRU/noWayOut/Assets/Scripts/ProceduralGeneration/Core/Room.cs#34-47) |
| Blocks Movement | ❌ |
| Sorting Order Offset | `5` (hiển thị trên floor) |
| **Placement** | **`WallAligned`** ← sát tường |
| Allow Random Flip | ✅ |
| Allow Random Rotation | ❌ |

**Ví dụ 2: Barrel (giữa phòng, chắn đường)**
| Field | Giá trị |
|-------|---------|
| Prefab | `Barrel_Prefab` |
| Weight | `2` |
| Size | [(1, 1)](file:///d:/%21course/FPT/7.SP26/PRU/noWayOut/Assets/Scripts/ProceduralGeneration/Core/Room.cs#34-47) |
| **Blocks Movement** | **✅** ← tự add BoxCollider2D |
| Sorting Order Offset | `1` |
| Placement | `Interior` |
| Allow Random Flip | ✅ |
| Allow Random Rotation | ❌ |

**Ví dụ 3: Bàn lớn 2×1 (giữa phòng)**
| Field | Giá trị |
|-------|---------|
| Prefab | `LargeTable_Prefab` |
| Weight | `0.5` (hiếm) |
| **Size** | **[(2, 1)](file:///d:/%21course/FPT/7.SP26/PRU/noWayOut/Assets/Scripts/ProceduralGeneration/Core/Room.cs#34-47)** ← chiếm 2 ô ngang |
| Blocks Movement | ✅ |
| Sorting Order Offset | `2` |
| Placement | `Interior` |
| Allow Random Flip | ✅ |
| Allow Random Rotation | ✅ |

**Ví dụ 4: Skull pile (góc phòng)**
| Field | Giá trị |
|-------|---------|
| Prefab | `SkullPile_Prefab` |
| Weight | `1` |
| Size | [(1, 1)](file:///d:/%21course/FPT/7.SP26/PRU/noWayOut/Assets/Scripts/ProceduralGeneration/Core/Room.cs#34-47) |
| Blocks Movement | ❌ |
| Sorting Order Offset | `0` |
| **Placement** | **`Corner`** ← chỉ đặt ở 4 góc |
| Allow Random Flip | ✅ |
| Allow Random Rotation | ❌ |

---

### Bước 5: Assign DecorData vào RoomData

1. Tìm các file **RoomData** ScriptableObject (ví dụ: `Start_RoomData`, `Arch1_RoomData`, ...)
2. Trong Inspector, tìm section **Decoration**
3. Kéo file `Arch1_DecorData` vào field **Decor Data**

> [!IMPORTANT]
> Mỗi RoomType có thể dùng DecorData khác nhau. Ví dụ:
> - Start room → decor nhẹ nhàng (torches only)
> - Archetype2 → nhiều barrels + skulls (nguy hiểm hơn)
> - Boss room → `null` (không decor, prefab riêng)

---

### Bước 6: Test trong Editor

1. Chọn **DungeonManager** trong Hierarchy
2. Dùng **Editor menu hoặc Inspector button** để Generate Dungeon
3. Xem rooms → kiểm tra:
   - ✅ Decor xuất hiện trong interior
   - ✅ WallAligned items nằm sát tường
   - ✅ Corner items ở 4 góc
   - ✅ Không có decor chặn cửa
   - ✅ Items lớn hơn 1×1 không overlap
   - ✅ Các decor có flip random (nhìn không lặp)
   - ✅ blocksMovement items có BoxCollider2D (check Inspector)

---

## Placement Type cheat sheet

```
┌─────────────────────────────┐
│  C   W   W   W   W   W   C │   C = Corner
│  W                       W │   W = WallAligned
│  W                       W │   I = Interior
│  W       I   I   I       W │   · = Floor (no decor)
│  W       I   I   I       W │
│  W                       W │   Door clearance zone
│  W           ☐           W │   ☐ = Door (3 tiles clear)
│  C   W   W   ·   W   W   C │
└─────────────────────────────┘
```

---

## Troubleshooting

| Vấn đề | Giải pháp |
|--------|-----------|
| Không thấy decor nào | Kiểm tra [DecorData](file:///d:/%21course/FPT/7.SP26/PRU/noWayOut/Assets/Scripts/ProceduralGeneration/Data/DecorData.cs#18-36) đã assign vào `RoomData.decorData` chưa |
| Compile error `DecorPlacement` | Chắc chắn [DecorData.cs](file:///d:/%21course/FPT/7.SP26/PRU/noWayOut/Assets/Scripts/ProceduralGeneration/Data/DecorData.cs) đã save đúng |
| Decor spawn ngoài phòng | Kiểm tra `Room.actualSize` có đúng không |
| Decor quá dày/thưa | Chỉnh `density` (0.05 = rất ít, 0.3 = rất nhiều) |
| Decor chặn cửa | Tăng `doorClearance` lên 4-5 |
