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
    }
}
