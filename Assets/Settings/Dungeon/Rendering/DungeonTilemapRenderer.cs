using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace NWO.Dungeon
{
    /// <summary>
    /// Tilemap-based dungeon renderer with object pooling for overlays.
    /// Implements IDungeonRenderer for separation of concerns.
    /// </summary>
    public sealed class DungeonTilemapRenderer : MonoBehaviour, IDungeonRenderer
    {
        [Header("Tilemap References")]
        [SerializeField] private Grid grid;
        [SerializeField] private Tilemap floorTilemap;
        [SerializeField] private Tilemap wallTilemap;
        
        [Header("Tileset Configuration")]
        [SerializeField] private DungeonTilesetConfig tilesetConfig;
        [SerializeField] private int floorFrameIndex = 0;
        
        [Header("Wall Overlays")]
        [SerializeField] private Transform overlayContainer;
        [SerializeField] private string overlaySortingLayer = "Default";
        [SerializeField] private int overlaySortingOrder = 1;
        [SerializeField] private int initialPoolSize = 100;

        // Tile cache for performance
        private readonly Dictionary<int, Tile> _tileCache = new();
        
        // Object pool for wall overlays
        private SpriteOverlayPool _overlayPool;
        private readonly List<GameObject> _activeOverlays = new();

        public Grid GridComponent => grid;
        public Tilemap FloorTilemap => floorTilemap;
        public Tilemap WallTilemap => wallTilemap;

        private void Awake()
        {
            EnsureInfrastructure();
            InitializePool();
        }

        private void OnDestroy()
        {
            _overlayPool?.Clear();
        }

        /// <summary>
        /// Render the dungeon map to tilemaps.
        /// </summary>
        public void Render(DungeonMap map)
        {
            if (map == null)
            {
                Debug.LogError("[DungeonTilemapRenderer] Cannot render null map!");
                return;
            }

            EnsureInfrastructure();
            Clear();

            for (int r = 0; r < map.Rows; r++)
            {
                for (int c = 0; c < map.Columns; c++)
                {
                    var cell = map.Get(c, r);

                    if (cell != DungeonCell.Wall)
                    {
                        RenderFloor(c, r);
                    }
                    else
                    {
                        RenderWall(map, c, r);
                    }
                }
            }

            Debug.Log($"[DungeonTilemapRenderer] Rendered {map.Columns}x{map.Rows} dungeon. Active overlays: {_activeOverlays.Count}");
        }

        /// <summary>
        /// Clear all rendered content.
        /// </summary>
        public void Clear()
        {
            // Return overlays to pool (NOT destroy!)
            foreach (var overlay in _activeOverlays)
            {
                if (overlay != null)
                {
                    overlay.SetActive(false);
                }
            }
            _activeOverlays.Clear();
            _overlayPool?.ReturnAll();

            // Clear tilemaps
            floorTilemap?.ClearAllTiles();
            wallTilemap?.ClearAllTiles();
        }

        /// <summary>
        /// Convert grid coordinates to world position (center of tile).
        /// </summary>
        public Vector3 GridToWorld(int column, int row)
        {
            return new Vector3(column + 0.5f, row + 0.5f, 0f);
        }

        /// <summary>
        /// Convert world position to grid coordinates.
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x),
                Mathf.FloorToInt(worldPosition.y)
            );
        }

        private void RenderFloor(int column, int row)
        {
            var tile = GetTile(floorFrameIndex);
            if (tile != null)
            {
                floorTilemap.SetTile(new Vector3Int(column, row, 0), tile);
            }
        }

        private void RenderWall(DungeonMap map, int column, int row)
        {
            var frames = DungeonWallBitmask.GetWallFrameIndexes(map, column, row);
            
            if (frames.Count == 0) return;

            // First frame goes to tilemap (has collider)
            var tile = GetTile(frames[0]);
            if (tile != null)
            {
                wallTilemap.SetTile(new Vector3Int(column, row, 0), tile);
            }

            // Additional frames use pooled overlays
            for (int i = 1; i < frames.Count; i++)
            {
                var frameIndex = frames[i];
                var sprite = GetSprite(frameIndex);
                
                if (sprite == null) continue;

                var position = GridToWorld(column, row);
                var overlay = _overlayPool.Get(position, sprite, i);
                _activeOverlays.Add(overlay);
            }
        }

        private void EnsureInfrastructure()
        {
            // Find or create Grid
            if (grid == null)
            {
                grid = GetComponentInChildren<Grid>();
            }
            if (grid == null)
            {
                grid = FindFirstObjectByType<Grid>();
            }
            if (grid == null)
            {
                var go = new GameObject("Grid");
                go.transform.SetParent(transform, false);
                grid = go.AddComponent<Grid>();
                grid.cellSize = Vector3.one;
            }

            // Find or create Floor tilemap
            if (floorTilemap == null)
            {
                floorTilemap = grid.transform.Find("Floor")?.GetComponent<Tilemap>();
            }
            if (floorTilemap == null)
            {
                var go = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer));
                go.transform.SetParent(grid.transform, false);
                floorTilemap = go.GetComponent<Tilemap>();
                
                var renderer = go.GetComponent<TilemapRenderer>();
                renderer.sortingOrder = 0;
            }

            // Find or create Walls tilemap with colliders
            if (wallTilemap == null)
            {
                wallTilemap = grid.transform.Find("Walls")?.GetComponent<Tilemap>();
            }
            if (wallTilemap == null)
            {
                var go = new GameObject("Walls");
                go.transform.SetParent(grid.transform, false);
                
                wallTilemap = go.AddComponent<Tilemap>();
                var renderer = go.AddComponent<TilemapRenderer>();
                renderer.sortingOrder = 1;
                
                var collider = go.AddComponent<TilemapCollider2D>();
                collider.compositeOperation = Collider2D.CompositeOperation.Merge;
                
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
                
                var composite = go.AddComponent<CompositeCollider2D>();
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
            }

            // Find or create overlay container
            if (overlayContainer == null)
            {
                var existing = grid.transform.Find("WallOverlays");
                if (existing != null)
                {
                    overlayContainer = existing;
                }
                else
                {
                    var go = new GameObject("WallOverlays");
                    go.transform.SetParent(grid.transform, false);
                    overlayContainer = go.transform;
                }
            }
        }

        private void InitializePool()
        {
            if (_overlayPool != null) return;
            
            EnsureInfrastructure();
            _overlayPool = new SpriteOverlayPool(
                overlayContainer, 
                initialPoolSize, 
                overlaySortingLayer, 
                overlaySortingOrder
            );
        }

        private Tile GetTile(int frameIndex)
        {
            if (frameIndex < 0) return null;
            if (_tileCache.TryGetValue(frameIndex, out var cached)) return cached;
            
            var sprite = GetSprite(frameIndex);
            if (sprite == null) return null;

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            _tileCache[frameIndex] = tile;
            return tile;
        }

        private Sprite GetSprite(int frameIndex)
        {
            if (tilesetConfig == null || tilesetConfig.frames == null) return null;
            if (frameIndex < 0 || frameIndex >= tilesetConfig.frames.Length) return null;
            return tilesetConfig.frames[frameIndex];
        }
    }
}
