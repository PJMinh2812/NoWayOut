using UnityEngine;
using UnityEngine.Tilemaps;
using ProceduralGeneration.Data;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Rendering;
#endif

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
        [SerializeField, Min(1)] private int activeDoorOpeningDepth = 3; // Số ô mở vào trong cho cửa đang dùng
        
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
        
        [Header("Wall Fill Tiles (trám tường không có cửa, theo hướng)")]
        [Tooltip("Tile trám tường TRÊN. Nếu null sẽ dùng wallTop.")]
        [SerializeField] private TileBase wallFillTop;
        [Tooltip("Tile trám tường DƯỚI. Nếu null sẽ dùng wallBottom.")]
        [SerializeField] private TileBase wallFillBottom;
        [Tooltip("Tile trám tường TRÁI. Nếu null sẽ dùng wallLeft.")]
        [SerializeField] private TileBase wallFillLeft;
        [Tooltip("Tile trám tường PHẢI. Nếu null sẽ dùng wallRight.")]
        [SerializeField] private TileBase wallFillRight;
        
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
        private int cachedTilesX; // Cache tile dimensions for consistent door/wall alignment
        private int cachedTilesY;

        private void OnEnable()
        {
            // Auto-fix materials khi prefab được load (scene load, edit prefab, instantiate, etc.)
            NormalizeTilemapMaterials();
        }

        /// <summary>
        /// Normalize (fix pink material) cho toàn bộ TilemapRenderer khi load từ prefab
        /// (Gọi OnEnable để auto-fix khi prefab instantiate hoặc edit prefab)
        /// </summary>
        private void NormalizeTilemapMaterials()
        {
            #if UNITY_EDITOR
            foreach (var renderer in GetComponentsInChildren<TilemapRenderer>(true))
            {
                if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null)
                {
                    Material spriteDefault = GetSpriteDefaultMaterial();
                    if (spriteDefault != null)
                    {
                        renderer.sharedMaterial = spriteDefault;
                    }
                }
            }
            #endif
        }
        
        /// <summary>
        /// Configure tiles từ DungeonManager (vì component được add runtime)
        /// </summary>
        public void ConfigureTiles(bool autoFill, TileBase[] floors, TileBase center,
            TileBase topL, TileBase topR, TileBase botL, TileBase botR,
            TileBase top, TileBase bottom, TileBase left, TileBase right,
            GameObject door, Data.TrapData[] traps,
            TileBase fillTop = null, TileBase fillBottom = null,
            TileBase fillLeft = null, TileBase fillRight = null)
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
            if (fillTop != null) wallFillTop = fillTop;
            if (fillBottom != null) wallFillBottom = fillBottom;
            if (fillLeft != null) wallFillLeft = fillLeft;
            if (fillRight != null) wallFillRight = fillRight;
        }
        
        /// <summary>
        /// Set current room reference (for DoorTrigger)
        /// </summary>
        public void SetCurrentRoom(Core.Room room)
        {
            currentRoom = room;
        }
        
        /// <summary>
        /// Force Sprites-Default material trên tất cả TilemapRenderer (dùng trong editor để fix pink preview)
        /// </summary>
        public void ForceSpriteDefaultMaterial()
        {
            #if UNITY_EDITOR
            Material spriteDefault = GetSpriteDefaultMaterial();
            if (spriteDefault == null)
            {
                // Fallback: set material = null để dùng default
                foreach (var renderer in GetComponentsInChildren<TilemapRenderer>(true))
                {
                    renderer.sharedMaterial = null;
                }
                return;
            }

            foreach (var renderer in GetComponentsInChildren<TilemapRenderer>(true))
            {
                renderer.sharedMaterial = spriteDefault;
            }
            #endif
        }

        /// <summary>
        /// Get material an toàn cho Sprite/Tilemap theo render pipeline hiện tại.
        /// </summary>
        private Material GetSpriteDefaultMaterial()
        {
            #if UNITY_EDITOR
            // URP 2D: ưu tiên Sprite-Unlit-Default để tránh magenta do shader mismatch.
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                Material urpSpriteMat = AssetDatabase.LoadAssetAtPath<Material>(
                    "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
                if (urpSpriteMat != null)
                    return urpSpriteMat;
            }

            // Built-in fallback.
            return AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            #else
            return null;
            #endif
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
        
        /// <summary>
        /// Generate visuals cho phòng prefab: tạo đầy đủ floor + walls + doors
        /// (prefab chỉ cung cấp spawn points và collider structure, visuals hoàn toàn do code tạo)
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

            // Xóa door cũ để regenerate theo connections hiện tại.
            foreach (string n in new[] { "Doors", "DoorMarkers" })
            {
                Transform old = transform.Find(n);
                if (old != null)
                {
                    if (Application.isPlaying) Destroy(old.gameObject);
                    else DestroyImmediate(old.gameObject);
                }
            }

            // Nếu prefab đã có visuals thì giữ nguyên, chỉ thêm doors.
            bool hasExistingVisuals = HasExistingVisuals();
            Transform visualContainer = transform.Find("Visuals");
            if (visualContainer == null)
            {
                GameObject newContainer = new GameObject("Visuals");
                newContainer.transform.SetParent(transform);
                newContainer.transform.localPosition = Vector3.zero;
                visualContainer = newContainer.transform;
            }

            if (!hasExistingVisuals)
            {
                // Prefab không có visuals: fallback về auto-generate như trước.
                if (autoFillTiles && floorTiles != null && floorTiles.Length > 0)
                    CreateAutoFilledFloor(visualContainer);
                else
                    CreateEmptyTilemap(visualContainer, "Floor", -10);

                if (autoFillTiles && wallCenter != null)
                    CreateAutoFilledWalls(visualContainer);
                else
                    CreateEmptyTilemap(visualContainer, "Walls", 0);
            }
            else
            {
                // Prefab có visuals sẵn: đồng bộ trạng thái lỗ cửa trên wall tilemap.
                // Hướng có kết nối thì mở, hướng không có kết nối thì trám lại bằng wall tile.
                SyncDoorGapsOnExistingWalls();
            }

            // Tạo Doors
            if (doorPrefab != null)
                GenerateDoorPrefabs(visualContainer);
            else
                GenerateDoorMarkers(visualContainer);
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
            
            // Set material immediately to prevent pink preview
            Material spriteDefault = GetSpriteDefaultMaterial();
            if (spriteDefault != null)
                tilemapRenderer.sharedMaterial = spriteDefault;
            
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
            
            GetDoorPlacementRect(out float minX, out float maxX, out float minY, out float maxY);
            float roomWidth = maxX - minX;
            float roomHeight = maxY - minY;
            
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
                        doorPos = new Vector3(minX + roomWidth / 2f + door.localPosition.x * tileSize, maxY - doorInset, -0.1f);
                        doorScale = new Vector3(doorSize, scaledWallThickness, 1);
                        break;
                    case DoorDirection.Bottom:
                        doorPos = new Vector3(minX + roomWidth / 2f + door.localPosition.x * tileSize, minY + doorInset, -0.1f);
                        doorScale = new Vector3(doorSize, scaledWallThickness, 1);
                        break;
                    case DoorDirection.Left:
                        doorPos = new Vector3(minX + doorInset, minY + roomHeight / 2f + door.localPosition.y * tileSize, -0.1f);
                        doorScale = new Vector3(scaledWallThickness, doorSize, 1);
                        break;
                    case DoorDirection.Right:
                        doorPos = new Vector3(maxX - doorInset, minY + roomHeight / 2f + door.localPosition.y * tileSize, -0.1f);
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

            // Base floor tilemap: luôn fill kín cell để sprite không full ô vẫn không lộ nền.
            GameObject baseTilemapObj = new GameObject("FloorBaseTilemap");
            baseTilemapObj.transform.SetParent(gridObj.transform);
            baseTilemapObj.transform.localPosition = Vector3.zero;

            Tilemap baseTilemap = baseTilemapObj.AddComponent<Tilemap>();
            TilemapRenderer baseRenderer = baseTilemapObj.AddComponent<TilemapRenderer>();
            baseRenderer.sortingOrder = -11;

            Material baseSpriteDefault = GetSpriteDefaultMaterial();
            if (baseSpriteDefault != null)
                baseRenderer.sharedMaterial = baseSpriteDefault;
            
            GameObject tilemapObj = new GameObject("FloorTilemap");
            tilemapObj.transform.SetParent(gridObj.transform);
            tilemapObj.transform.localPosition = Vector3.zero;
            
            Tilemap tilemap = tilemapObj.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObj.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = -10;
            
            // Set material immediately to prevent pink preview
            Material spriteDefault = GetSpriteDefaultMaterial();
            if (spriteDefault != null)
                renderer.sharedMaterial = spriteDefault;
            
            int tilesX = currentRoom.actualSize.x * (int)tileSize;
            int tilesY = currentRoom.actualSize.y * (int)tileSize;

            Tile floorBaseTile = CreateSolidColorTile(floorColor);
            
            // Fill sàn CHỈ interior (bỏ edges vì đó là chỗ có wall)
            for (int x = 1; x < tilesX - 1; x++)
            {
                for (int y = 1; y < tilesY - 1; y++)
                {
                    baseTilemap.SetTile(new Vector3Int(x, y, 0), floorBaseTile);
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

            // Base wall tilemap: fill nền màu tường dưới sprite tường để tránh lộ nền.
            GameObject baseTilemapObj = new GameObject("WallsBaseTilemap");
            baseTilemapObj.transform.SetParent(gridObj.transform);
            baseTilemapObj.transform.localPosition = Vector3.zero;

            Tilemap baseTilemap = baseTilemapObj.AddComponent<Tilemap>();
            TilemapRenderer baseRenderer = baseTilemapObj.AddComponent<TilemapRenderer>();
            baseRenderer.sortingOrder = -1;

            Material baseSpriteDefault = GetSpriteDefaultMaterial();
            if (baseSpriteDefault != null)
                baseRenderer.sharedMaterial = baseSpriteDefault;
            
            GameObject tilemapObj = new GameObject("WallsTilemap");
            tilemapObj.transform.SetParent(gridObj.transform);
            tilemapObj.transform.localPosition = Vector3.zero;
            
            Tilemap tilemap = tilemapObj.AddComponent<Tilemap>();
            TilemapRenderer renderer = tilemapObj.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = 0;
            
            // Set material immediately to prevent pink preview
            Material spriteDefault = GetSpriteDefaultMaterial();
            if (spriteDefault != null)
                renderer.sharedMaterial = spriteDefault;
            
            // Add collision for walls
            TilemapCollider2D tilemapCollider = tilemapObj.AddComponent<TilemapCollider2D>();
            Rigidbody2D rb = tilemapObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static; // Walls don't move
            
            CompositeCollider2D compositeCollider = tilemapObj.AddComponent<CompositeCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge; // Optimize collision with CompositeCollider2D
            
            int tilesX = currentRoom.actualSize.x * (int)tileSize;
            int tilesY = currentRoom.actualSize.y * (int)tileSize;
            cachedTilesX = tilesX;
            cachedTilesY = tilesY;
            Tile wallBaseTile = CreateSolidColorTile(wallColor);
            
            // Tính door tile positions - xóa CHỈ 1 tile tại vị trí door (cả edge lẫn inner layer)
            var doorTilePositions = new System.Collections.Generic.HashSet<Vector2Int>();
            
            foreach (var door in roomData.doorAnchors)
            {
                if (activeDirections != null && !activeDirections.Contains(door.direction))
                    continue;

                Vector2Int doorCell = GetDoorCenterCell(door, tilesX, tilesY);

                foreach (var openingCell in GetDoorOpeningCells(door.direction, doorCell, tilesX, tilesY, GetActiveOpeningDepth()))
                    doorTilePositions.Add(openingCell);
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
                        baseTilemap.SetTile(new Vector3Int(x, y, 0), wallBaseTile);
                        tilemap.SetTile(new Vector3Int(x, y, 0), wallTileToUse);
                    }
                }
            }
            
            // KEEP DOOR GAPS OPEN: do not auto-fill door cells back with wall tiles.
        }

        private Tile CreateSolidColorTile(Color color)
        {
            CreateSquareSprite();
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = squareSprite;
            tile.color = color;
            return tile;
        }

        /// <summary>
        /// Chọn wall tile phù hợp dựa vào vị trí
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
        /// Lấy wall fill tile theo hướng, fallback về wall tile của hướng đó, rồi wallCenter
        /// </summary>
        private TileBase GetWallFillTileForDirection(DoorDirection direction)
        {
            switch (direction)
            {
                case DoorDirection.Top:    return wallFillTop ?? wallTop ?? wallCenter;
                case DoorDirection.Bottom: return wallFillBottom ?? wallBottom ?? wallCenter;
                case DoorDirection.Left:   return wallFillLeft ?? wallLeft ?? wallCenter;
                case DoorDirection.Right:  return wallFillRight ?? wallRight ?? wallCenter;
                default:                   return wallCenter;
            }
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
            
            GetDoorPlacementRect(out float minX, out float maxX, out float minY, out float maxY);
            
            foreach (var door in roomData.doorAnchors)
            {
                // Chỉ tạo door nếu hướng này có room kế bên
                if (activeDirections != null && !activeDirections.Contains(door.direction))
                    continue;
                
                GameObject doorObj = Instantiate(doorPrefab, doorContainer.transform);
                doorObj.name = $"Door_{door.direction}";
                
                Vector3 doorPos = GetDoorPosition(door, minX, maxX, minY, maxY);
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
        private Vector3 GetDoorPosition(DoorAnchor door, float minX, float maxX, float minY, float maxY)
        {
            if (TryGetDoorPositionFromWallTilemap(door, out Vector3 alignedPos))
                return alignedPos;

            float roomWidth = maxX - minX;
            float roomHeight = maxY - minY;
            // Dùng cached dimensions nếu có để đồng bộ với wall cutout
            int tilesX = cachedTilesX > 0 ? cachedTilesX : Mathf.Max(1, Mathf.RoundToInt(roomWidth / tileSize));
            int tilesY = cachedTilesY > 0 ? cachedTilesY : Mathf.Max(1, Mathf.RoundToInt(roomHeight / tileSize));
            Vector2Int doorCell = GetDoorCenterCell(door, tilesX, tilesY);
            float centerX = minX + (doorCell.x + 0.5f) * tileSize;
            float centerY = minY + (doorCell.y + 0.5f) * tileSize;
            
            switch (door.direction)
            {
                case DoorDirection.Top:
                    return new Vector3(centerX, maxY - tileSize * 0.5f, 0);
                case DoorDirection.Bottom:
                    return new Vector3(centerX, minY + tileSize * 0.5f, 0);
                case DoorDirection.Left:
                    return new Vector3(minX + tileSize * 0.5f, centerY, 0);
                case DoorDirection.Right:
                    return new Vector3(maxX - tileSize * 0.5f, centerY, 0);
                default:
                    return Vector3.zero;
            }
        }

        private bool TryGetDoorPositionFromWallTilemap(DoorAnchor door, out Vector3 localPosition)
        {
            localPosition = Vector3.zero;

            if (!TryGetWallTilemaps(out var wallTilemaps, out _))
                return false;

            Tilemap wallTilemap = GetReferenceWallTilemap(wallTilemaps);
            if (wallTilemap == null)
                return false;

            BoundsInt bounds = wallTilemap.cellBounds;
            if (bounds.size.x <= 0 || bounds.size.y <= 0)
                return false;

            int tilesX = bounds.size.x;
            int tilesY = bounds.size.y;
            Vector2Int doorCell = GetDoorCenterCell(door, tilesX, tilesY);

            Vector2Int edgeCell = door.direction switch
            {
                DoorDirection.Top => new Vector2Int(doorCell.x, tilesY - 1),
                DoorDirection.Bottom => new Vector2Int(doorCell.x, 0),
                DoorDirection.Left => new Vector2Int(0, doorCell.y),
                DoorDirection.Right => new Vector2Int(tilesX - 1, doorCell.y),
                _ => doorCell
            };

            Vector3Int mapCell = new Vector3Int(bounds.xMin + edgeCell.x, bounds.yMin + edgeCell.y, 0);
            Vector3 wallLocal = wallTilemap.GetCellCenterLocal(mapCell);
            Vector3 world = wallTilemap.transform.TransformPoint(wallLocal);
            localPosition = transform.InverseTransformPoint(world);
            return true;
        }

        private Vector2Int GetDoorCenterCell(DoorAnchor door, int tilesX, int tilesY)
        {
            Vector2 snappedOffset = GetSnappedDoorOffset(door);

            int cx = Mathf.Clamp(tilesX / 2 + Mathf.RoundToInt(snappedOffset.x), 0, tilesX - 1);
            int cy = Mathf.Clamp(tilesY / 2 + Mathf.RoundToInt(snappedOffset.y), 0, tilesY - 1);

            switch (door.direction)
            {
                case DoorDirection.Top:
                    return new Vector2Int(cx, tilesY - 1);
                case DoorDirection.Bottom:
                    return new Vector2Int(cx, 0);
                case DoorDirection.Left:
                    return new Vector2Int(0, cy);
                case DoorDirection.Right:
                    return new Vector2Int(tilesX - 1, cy);
                default:
                    return new Vector2Int(cx, cy);
            }
        }

        private Vector2 GetSnappedDoorOffset(DoorAnchor door)
        {
            // Đồng bộ offset giữa wall cutout và door placement để tránh lệch theo nửa ô.
            return new Vector2(
                Mathf.Round(door.localPosition.x),
                Mathf.Round(door.localPosition.y)
            );
        }

        private void SyncDoorGapsOnExistingWalls()
        {
            if (roomData == null || roomData.doorAnchors == null || roomData.doorAnchors.Count == 0)
                return;

            if (!TryGetWallTilemaps(out var wallTilemaps, out Tilemap baseWallTilemap))
                return;

            Tilemap referenceWallTilemap = GetReferenceWallTilemap(wallTilemaps);
            if (referenceWallTilemap == null)
                return;

            BoundsInt bounds = referenceWallTilemap.cellBounds;
            if (bounds.size.x <= 0 || bounds.size.y <= 0)
                return;

            int tilesX = bounds.size.x;
            int tilesY = bounds.size.y;
            foreach (var door in roomData.doorAnchors)
            {
                Vector2Int doorCell = GetDoorCenterCell(door, tilesX, tilesY);
                int openingDepth = GetActiveOpeningDepth();

                foreach (var localCell in GetDoorOpeningCells(door.direction, doorCell, tilesX, tilesY, openingDepth))
                {
                    Vector3Int cell = new Vector3Int(bounds.xMin + localCell.x, bounds.yMin + localCell.y, 0);

                    foreach (var wallTilemap in wallTilemaps)
                        wallTilemap.SetTile(cell, null);

                    if (baseWallTilemap != null)
                        baseWallTilemap.SetTile(cell, null);
                }
            }
        }

        private System.Collections.Generic.List<Vector2Int> GetDoorOpeningCells(DoorDirection direction, Vector2Int doorCell, int tilesX, int tilesY, int openingDepth)
        {
            var cells = new System.Collections.Generic.List<Vector2Int>(Mathf.Max(1, openingDepth));
            openingDepth = Mathf.Max(1, openingDepth);

            Vector2Int edgeCell;
            Vector2Int inward;

            switch (direction)
            {
                case DoorDirection.Top:
                    edgeCell = new Vector2Int(doorCell.x, tilesY - 1);
                    inward = Vector2Int.down;
                    break;
                case DoorDirection.Bottom:
                    edgeCell = new Vector2Int(doorCell.x, 0);
                    inward = Vector2Int.up;
                    break;
                case DoorDirection.Left:
                    edgeCell = new Vector2Int(0, doorCell.y);
                    inward = Vector2Int.right;
                    break;
                case DoorDirection.Right:
                    edgeCell = new Vector2Int(tilesX - 1, doorCell.y);
                    inward = Vector2Int.left;
                    break;
                default:
                    return cells;
            }

            for (int i = 0; i < openingDepth; i++)
            {
                Vector2Int c = edgeCell + inward * i;
                if (c.x < 0 || c.x >= tilesX || c.y < 0 || c.y >= tilesY)
                    break;
                cells.Add(c);
            }

            return cells;
        }

        private int GetActiveOpeningDepth()
        {
            return Mathf.Max(2, activeDoorOpeningDepth);
        }

        private Tilemap GetReferenceWallTilemap(System.Collections.Generic.List<Tilemap> wallTilemaps)
        {
            if (wallTilemaps == null || wallTilemaps.Count == 0)
                return null;

            Tilemap withCollider = wallTilemaps.Find(t => t != null && t.GetComponent<TilemapCollider2D>() != null);
            if (withCollider != null)
                return withCollider;

            Tilemap selected = null;
            int bestArea = -1;
            foreach (var tilemap in wallTilemaps)
            {
                if (tilemap == null) continue;
                BoundsInt b = tilemap.cellBounds;
                int area = b.size.x * b.size.y;
                if (area > bestArea)
                {
                    bestArea = area;
                    selected = tilemap;
                }
            }

            return selected;
        }

        private bool TryGetWallTilemaps(out System.Collections.Generic.List<Tilemap> wallTilemaps, out Tilemap baseWallTilemap)
        {
            wallTilemaps = new System.Collections.Generic.List<Tilemap>();
            baseWallTilemap = null;

            foreach (var tilemap in GetComponentsInChildren<Tilemap>(true))
            {
                if (tilemap == null) continue;

                string n = tilemap.gameObject.name;
                if (!n.Contains("Wall"))
                    continue;

                if (n.Contains("Base"))
                {
                    if (baseWallTilemap == null)
                        baseWallTilemap = tilemap;
                    continue;
                }

                wallTilemaps.Add(tilemap);
            }

            return wallTilemaps.Count > 0;
        }

        private bool TryGetPrimaryWallTilemaps(out Tilemap wallTilemap, out Tilemap baseWallTilemap)
        {
            wallTilemap = null;
            if (!TryGetWallTilemaps(out var wallTilemaps, out baseWallTilemap))
                return false;

            wallTilemap = GetReferenceWallTilemap(wallTilemaps);
            return wallTilemap != null;
        }

        private bool HasExistingVisuals()
        {
            foreach (var tilemap in GetComponentsInChildren<Tilemap>(true))
            {
                if (tilemap != null)
                    return true;
            }

            foreach (var spriteRenderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (spriteRenderer == null) continue;
                if (spriteRenderer.gameObject.name.StartsWith("Door")) continue;
                return true;
            }

            return false;
        }

        private void GetDoorPlacementRect(out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = 0f;
            minY = 0f;
            maxX = currentRoom.actualSize.x * tileSize;
            maxY = currentRoom.actualSize.y * tileSize;

            if (TryGetRoomRectFromTilemaps(out Bounds localBounds) || TryGetLocalVisualBounds(out localBounds))
            {
                minX = localBounds.min.x;
                minY = localBounds.min.y;
                maxX = localBounds.max.x;
                maxY = localBounds.max.y;
            }
        }

        private bool TryGetRoomRectFromTilemaps(out Bounds localBounds)
        {
            bool hasBounds = false;
            localBounds = new Bounds();

            foreach (var tilemap in GetComponentsInChildren<Tilemap>(true))
            {
                if (tilemap == null) continue;

                string n = tilemap.gameObject.name;
                bool isStructural = n.Contains("Wall") || n.Contains("Floor");
                if (!isStructural) continue;

                BoundsInt cells = tilemap.cellBounds;
                if (cells.size.x <= 0 || cells.size.y <= 0) continue;

                Vector3 localMinOnTilemap = tilemap.CellToLocalInterpolated((Vector3)cells.min);
                Vector3 localMaxOnTilemap = tilemap.CellToLocalInterpolated((Vector3)cells.max);

                Vector3 worldMin = tilemap.transform.TransformPoint(localMinOnTilemap);
                Vector3 worldMax = tilemap.transform.TransformPoint(localMaxOnTilemap);

                Vector3 roomLocalMin = transform.InverseTransformPoint(worldMin);
                Vector3 roomLocalMax = transform.InverseTransformPoint(worldMax);

                Vector3 min = Vector3.Min(roomLocalMin, roomLocalMax);
                Vector3 max = Vector3.Max(roomLocalMin, roomLocalMax);

                if (!hasBounds)
                {
                    localBounds = new Bounds((min + max) * 0.5f, max - min);
                    hasBounds = true;
                }
                else
                {
                    localBounds.Encapsulate(min);
                    localBounds.Encapsulate(max);
                }
            }

            return hasBounds;
        }

        private bool TryGetLocalVisualBounds(out Bounds localBounds)
        {
            bool hasBounds = false;
            localBounds = new Bounds();

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                string n = renderer.gameObject.name;
                if (n.StartsWith("Door")) continue;

                Bounds wb = renderer.bounds;
                Vector3[] corners = new Vector3[]
                {
                    new Vector3(wb.min.x, wb.min.y, wb.center.z),
                    new Vector3(wb.min.x, wb.max.y, wb.center.z),
                    new Vector3(wb.max.x, wb.min.y, wb.center.z),
                    new Vector3(wb.max.x, wb.max.y, wb.center.z),
                };

                foreach (var corner in corners)
                {
                    Vector3 local = transform.InverseTransformPoint(corner);
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(local, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(local);
                    }
                }
            }

            return hasBounds;
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



        // Decor removed - map được trang trí thủ công trong Editor
        /*
        private void GenerateDecorations_REMOVED(Transform parent, DecorData decorData)
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
        */
    }
}
