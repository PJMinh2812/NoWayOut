using UnityEngine;
using UnityEngine.Tilemaps;
using ProceduralGeneration.Data;

namespace ProceduralGeneration.Components
{
    /// <summary>
    /// Tự động generate visuals cho rooms (floor, walls, doors)
    /// </summary>
    public class RoomVisualGenerator : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Color floorColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Màu xám đậm
        [SerializeField] private Color wallColor = new Color(0.2f, 0.2f, 0.25f, 1f); // Màu xám xanh
        [SerializeField] private Color doorColor = new Color(0.6f, 0.4f, 0.2f, 1f); // Màu nâu
        
        [Header("Tile Settings")]
        [SerializeField] private float tileSize = 10f; // Match room spacing (gridPosition * 10)
        [SerializeField] private float wallThickness = 1.5f; // Increased for better visibility
        
        [Header("Tilemap Settings (Optional)")]
        [SerializeField] private TileBase floorTile; // Assign tile asset in Inspector (optional)
        [SerializeField] private TileBase wallTile; // Assign tile asset in Inspector (optional)
        
        private RoomData roomData;
        private Sprite squareSprite;
        private Tile defaultFloorTile; // Runtime-generated tile if no asset assigned
        
        /// <summary>
        /// Generate visuals cho room
        /// </summary>
        public void GenerateVisuals(RoomData data)
        {
            roomData = data;
            
            Debug.Log($"[RoomVisualGenerator] Generating visuals for room: {roomData.name}");
            
            // Tạo container
            GameObject visualContainer = new GameObject("Visuals");
            visualContainer.transform.SetParent(transform);
            visualContainer.transform.localPosition = Vector3.zero;
            
            // Tạo Tilemap structure cho Floor (empty - để user vẽ)
            CreateEmptyTilemap(visualContainer.transform, "Floor", -10);
            
            // Tạo Tilemap structure cho Walls (empty - để user vẽ)
            CreateEmptyTilemap(visualContainer.transform, "Walls", 0);
            
            // Tạo door markers (để sync với door anchors)
            GenerateDoorMarkers(visualContainer.transform);
            
            Debug.Log($"[RoomVisualGenerator] Visuals generated successfully!");
        }
        
        /// <summary>
        /// Tạo empty Tilemap structure (Grid + Tilemap) - với placeholder tiles
        /// </summary>
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
            int tilesX = roomData.size.x * (int)tileSize; // Ví dụ: 1 * 10 = 10 cells
            int tilesY = roomData.size.y * (int)tileSize; // Ví dụ: 1 * 10 = 10 cells
            
            for (int x = 0; x < tilesX; x++)
            {
                for (int y = 0; y < tilesY; y++)
                {
                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    tilemap.SetTile(tilePos, placeholderTile);
                }
            }
            
            Debug.Log($"[RoomVisualGenerator] Created {name} Tilemap with {tilesX}×{tilesY} cells (total: {tilesX * tilesY} tiles)");
        }
        
        /// <summary>
        /// Tạo door markers để chỉ vị trí doors
        /// </summary>
        private void GenerateDoorMarkers(Transform parent)
        {
            if (roomData.doorAnchors == null || roomData.doorAnchors.Count == 0)
                return;
            
            CreateSquareSprite(); // Tạo sprite cho door markers
            
            GameObject doorContainer = new GameObject("DoorMarkers");
            doorContainer.transform.SetParent(parent);
            doorContainer.transform.localPosition = Vector3.zero;
            
            float roomWidth = roomData.size.x * tileSize;
            float roomHeight = roomData.size.y * tileSize;
            float doorSize = 1.5f;
            
            foreach (var door in roomData.doorAnchors)
            {
                GameObject doorMarker = new GameObject($"DoorMarker_{door.direction}");
                doorMarker.transform.SetParent(doorContainer.transform);
                
                Vector3 doorPos = Vector3.zero;
                Vector3 doorScale = new Vector3(doorSize, doorSize, 1);
                
                // Tính position của door
                switch (door.direction)
                {
                    case DoorDirection.Top:
                        doorPos = new Vector3(roomWidth / 2f + door.localPosition.x * tileSize, roomHeight, -0.1f);
                        doorScale = new Vector3(doorSize, wallThickness, 1);
                        break;
                    case DoorDirection.Bottom:
                        doorPos = new Vector3(roomWidth / 2f + door.localPosition.x * tileSize, 0, -0.1f);
                        doorScale = new Vector3(doorSize, wallThickness, 1);
                        break;
                    case DoorDirection.Left:
                        doorPos = new Vector3(0, roomHeight / 2f + door.localPosition.y * tileSize, -0.1f);
                        doorScale = new Vector3(wallThickness, doorSize, 1);
                        break;
                    case DoorDirection.Right:
                        doorPos = new Vector3(roomWidth, roomHeight / 2f + door.localPosition.y * tileSize, -0.1f);
                        doorScale = new Vector3(wallThickness, doorSize, 1);
                        break;
                }
                
                doorMarker.transform.localPosition = doorPos;
                doorMarker.transform.localScale = doorScale;
                
                SpriteRenderer sr = doorMarker.AddComponent<SpriteRenderer>();
                sr.sprite = squareSprite;
                sr.color = doorColor;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 10;
            }
        }
        
        /// <summary>
        /// Tạo sprite vuông đơn giản - sử dụng Unity built-in sprite
        /// </summary>
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
