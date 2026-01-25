using System;
using System.Collections.Generic;
using UnityEngine;

namespace GloomCraft.Dungeon
{
    /// <summary>
    /// Handles spawning of entities (enemies, furniture) in the dungeon.
    /// Refactored from DungeonFurnisher2D with object pooling support.
    /// </summary>
    public sealed class EntitySpawner : MonoBehaviour
    {
        [Serializable]
        public sealed class EntityPrefabs
        {
            public GameObject enemy;
            public GameObject treasureChest;
            public GameObject craftingBench;
            public GameObject shopStand;
        }

        [Serializable]
        public sealed class SpawnChances
        {
            [Range(0, 1)] public float treasureChest = 0.8f;
            [Range(0, 1)] public float enemies = 0.6f;
            [Range(0, 1)] public float craftingBench = 0.4f;
            [Range(0, 1)] public float shopStand = 0.2f;
        }

        [Header("Prefabs")]
        [SerializeField] private EntityPrefabs prefabs = new();

        [Header("Spawn Chances")]
        [SerializeField] private SpawnChances chances = new();

        [Header("Enemy Settings")]
        [SerializeField] private int spotsPerEnemy = 16;

        [Header("Spawn Parents")]
        [SerializeField] private Transform enemiesRoot;
        [SerializeField] private Transform furnitureRoot;

        [Header("Randomization")]
        [SerializeField] private bool useSeed = false;
        [SerializeField] private int seed = 0;

        // Object pools for frequently spawned entities
        private GameObjectPool _enemyPool;
        private readonly List<GameObject> _spawnedEntities = new();

        public IReadOnlyList<GameObject> SpawnedEntities => _spawnedEntities;

        private void Awake()
        {
            EnsureRoots();
            InitializePools();
        }

        private void OnDestroy()
        {
            _enemyPool?.Clear();
        }

        /// <summary>
        /// Apply configuration from DungeonConfig.
        /// </summary>
        public void ApplyConfig(DungeonConfig config)
        {
            if (config == null) return;

            chances.treasureChest = config.treasureChestChance;
            chances.enemies = config.enemyChance;
            chances.craftingBench = config.craftingBenchChance;
            chances.shopStand = config.shopStandChance;
            spotsPerEnemy = config.spotsPerEnemy;
        }

        /// <summary>
        /// Spawn entities in the dungeon based on generation result.
        /// </summary>
        public void SpawnEntities(DungeonGenerator2D.Result result)
        {
            if (result == null || result.Map == null || result.Rooms == null)
            {
                Debug.LogError("[EntitySpawner] Cannot spawn - invalid result!");
                return;
            }

            ClearSpawned();
            EnsureRoots();

            var rng = useSeed ? new System.Random(seed) : new System.Random();

            foreach (var room in result.Rooms)
            {
                SpawnInRoom(result, room, rng);
            }

            DungeonEvents.RaiseEntitiesSpawned();
            Debug.Log($"[EntitySpawner] Spawned {_spawnedEntities.Count} entities");
        }

        /// <summary>
        /// Clear all spawned entities.
        /// </summary>
        public void ClearSpawned()
        {
            // Return pooled enemies
            _enemyPool?.ReturnAll();

            // Destroy non-pooled entities
            for (int i = _spawnedEntities.Count - 1; i >= 0; i--)
            {
                var entity = _spawnedEntities[i];
                if (entity != null && entity.activeInHierarchy)
                {
                    Destroy(entity);
                }
            }
            _spawnedEntities.Clear();
        }

        private void SpawnInRoom(DungeonGenerator2D.Result result, DungeonGenerator2D.Room room, System.Random rng)
        {
            var spots = GetSpots(result.Map, room);
            
            // Copy chances for this room
            var localChances = new SpawnChances
            {
                treasureChest = chances.treasureChest,
                enemies = chances.enemies,
                craftingBench = chances.craftingBench,
                shopStand = chances.shopStand
            };

            // No enemies in start room
            if (ReferenceEquals(room, result.StartRoom))
            {
                localChances.enemies = 0f;
            }

            // Roll for furniture (mutually exclusive priority)
            if (Roll(rng, localChances.shopStand))
            {
                PlaceFurniture(rng, result.Map, spots, prefabs.shopStand, 2, 2);
                localChances.craftingBench = 0f;
                localChances.treasureChest = 0f;
            }

            if (Roll(rng, localChances.craftingBench))
            {
                PlaceFurniture(rng, result.Map, spots, prefabs.craftingBench, 2, 1);
                localChances.treasureChest = 0f;
            }

            if (Roll(rng, localChances.treasureChest))
            {
                PlaceFurniture(rng, result.Map, spots, prefabs.treasureChest, 1, 1);
            }

            // Roll for enemies
            if (Roll(rng, localChances.enemies))
            {
                SpawnEnemies(rng, spots);
            }
        }

        private void SpawnEnemies(System.Random rng, List<Vector2Int> spots)
        {
            if (prefabs.enemy == null) return;
            if (spots == null || spots.Count == 0) return;

            int count = Mathf.CeilToInt((float)spots.Count / Mathf.Max(1, spotsPerEnemy));

            for (int i = 0; i < count; i++)
            {
                if (spots.Count == 0) break;

                int idx = rng.Next(spots.Count);
                var spot = spots[idx];
                spots.RemoveAt(idx);

                var position = TileCenterWorld(spot);
                
                // Use pool for enemies
                var enemy = _enemyPool?.Get(position, Quaternion.identity) 
                    ?? Instantiate(prefabs.enemy, position, Quaternion.identity, enemiesRoot);
                
                _spawnedEntities.Add(enemy);
            }
        }

        private void PlaceFurniture(System.Random rng, DungeonMap map, List<Vector2Int> spots, 
            GameObject prefab, int width, int height)
        {
            if (prefab == null) return;
            if (spots == null || spots.Count == 0) return;

            var fitSpots = GetFitSpots(spots, width, height);
            if (fitSpots.Count == 0) return;

            var spot = fitSpots[rng.Next(fitSpots.Count)];
            ApplyFurnitureToMap(map, spots, spot, width, height);

            // Center of furniture footprint
            var position = new Vector3(spot.x + (width / 2f), spot.y + (height / 2f), 0f);
            var furniture = Instantiate(prefab, position, Quaternion.identity, furnitureRoot);
            _spawnedEntities.Add(furniture);
        }

        private void ApplyFurnitureToMap(DungeonMap map, List<Vector2Int> spots, Vector2Int spot, int width, int height)
        {
            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < width; c++)
                {
                    int cc = spot.x + c;
                    int rr = spot.y + r;

                    if (map.InBounds(cc, rr))
                    {
                        map.Set(cc, rr, DungeonCell.Furniture);
                    }

                    // Remove from available spots
                    for (int i = spots.Count - 1; i >= 0; i--)
                    {
                        if (spots[i].x == cc && spots[i].y == rr)
                        {
                            spots.RemoveAt(i);
                        }
                    }
                }
            }
        }

        private List<Vector2Int> GetSpots(DungeonMap map, DungeonGenerator2D.Room room)
        {
            var spots = new List<Vector2Int>();

            for (int r = 0; r < room.Height; r++)
            {
                for (int c = 0; c < room.Width; c++)
                {
                    int rr = room.Row + r;
                    int cc = room.Column + c;

                    // Skip spots adjacent to tunnels
                    var n1 = map.InBounds(cc, rr - 1) ? map.Get(cc, rr - 1) : DungeonCell.Wall;
                    var n2 = map.InBounds(cc, rr + 1) ? map.Get(cc, rr + 1) : DungeonCell.Wall;
                    var n3 = map.InBounds(cc - 1, rr) ? map.Get(cc - 1, rr) : DungeonCell.Wall;
                    var n4 = map.InBounds(cc + 1, rr) ? map.Get(cc + 1, rr) : DungeonCell.Wall;

                    if (n1 == DungeonCell.Tunnel || n2 == DungeonCell.Tunnel || 
                        n3 == DungeonCell.Tunnel || n4 == DungeonCell.Tunnel)
                    {
                        continue;
                    }

                    if (map.Get(cc, rr) == DungeonCell.Room)
                    {
                        spots.Add(new Vector2Int(cc, rr));
                    }
                }
            }

            return spots;
        }

        private List<Vector2Int> GetFitSpots(List<Vector2Int> spots, int width, int height)
        {
            var fitSpots = new List<Vector2Int>();

            foreach (var s in spots)
            {
                bool valid = true;

                for (int r = 0; r < height && valid; r++)
                {
                    for (int c = 0; c < width && valid; c++)
                    {
                        if (c == 0 && r == 0) continue;

                        var p = new Vector2Int(s.x + c, s.y + r);
                        bool found = spots.Exists(spot => spot.x == p.x && spot.y == p.y);

                        if (!found) valid = false;
                    }
                }

                if (valid) fitSpots.Add(s);
            }

            return fitSpots;
        }

        private void EnsureRoots()
        {
            if (enemiesRoot == null)
            {
                var go = new GameObject("Enemies");
                go.transform.SetParent(transform, false);
                enemiesRoot = go.transform;
            }

            if (furnitureRoot == null)
            {
                var go = new GameObject("Furniture");
                go.transform.SetParent(transform, false);
                furnitureRoot = go.transform;
            }
        }

        private void InitializePools()
        {
            if (prefabs.enemy != null)
            {
                _enemyPool = new GameObjectPool(prefabs.enemy, enemiesRoot, 10, "Enemy");
            }
        }

        private static bool Roll(System.Random rng, float chance)
        {
            if (chance <= 0f) return false;
            if (chance >= 1f) return true;
            return rng.NextDouble() < chance;
        }

        private static Vector3 TileCenterWorld(Vector2Int tile)
        {
            return new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);
        }
    }
}
