using System;
using System.Collections.Generic;
using UnityEngine;

namespace NWO.Dungeon
{
    /// <summary>
    /// Port logic từ ms/game/dungeon/furnishing.ms.
    /// - Lấy "spots" trong mỗi room (không sát tunnel).
    /// - Roll chance để đặt furniture và spawn enemies.
    ///
    /// Lưu ý: Furniture gameplay (shop/craft/chest) sẽ port sau; hiện tại chỉ spawn prefab.
    /// </summary>
    public sealed class DungeonFurnisher2D : MonoBehaviour
    {
        [Serializable]
        public sealed class Chances
        {
            [Range(0, 1)] public float treasureChest = 0.8f;
            [Range(0, 1)] public float enemies = 0.6f;
            [Range(0, 1)] public float craftingBench = 0.4f;
            [Range(0, 1)] public float shopStand = 0.2f;
        }

        [Header("Chances (base)")]
        [SerializeField] private Chances chances = new();

        [Header("Prefabs")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject treasureChestPrefab;
        [SerializeField] private GameObject craftingBenchPrefab;
        [SerializeField] private GameObject shopStandPrefab;

        [Header("Spawn parents")]
        [SerializeField] private Transform enemiesRoot;
        [SerializeField] private Transform furnitureRoot;

        [Header("Enemy spawn")]
        [Tooltip("Approx microStudio: count = ceil(spots.length / 16).")]
        [SerializeField] private int spotsPerEnemy = 16;

        [Header("Random")]
        [SerializeField] private bool useSeed = false;
        [SerializeField] private int seed = 0;

        private readonly List<GameObject> _spawned = new();

        public void Furnish(DungeonGenerator2D.Result result)
        {
            if (result == null || result.Map == null || result.Rooms == null) return;

            ClearSpawned();
            EnsureRoots();

            var rng = useSeed ? new System.Random(seed) : new System.Random();

            foreach (var room in result.Rooms)
            {
                var spots = GetSpots(result.Map, room);

                // Copy chances (and adjust for start room)
                var local = new Chances
                {
                    treasureChest = chances.treasureChest,
                    enemies = chances.enemies,
                    craftingBench = chances.craftingBench,
                    shopStand = chances.shopStand
                };

                if (ReferenceEquals(room, result.StartRoom))
                {
                    local.enemies = 0f;
                }

                if (Roll(rng, local.shopStand))
                {
                    PlaceFurniture(rng, result.Map, spots, shopStandPrefab, 2, 2, furnitureRoot);
                    local.craftingBench = 0f;
                    local.treasureChest = 0f;
                }

                if (Roll(rng, local.craftingBench))
                {
                    PlaceFurniture(rng, result.Map, spots, craftingBenchPrefab, 2, 1, furnitureRoot);
                    local.treasureChest = 0f;
                }

                if (Roll(rng, local.treasureChest))
                {
                    PlaceFurniture(rng, result.Map, spots, treasureChestPrefab, 1, 1, furnitureRoot);
                }

                if (Roll(rng, local.enemies))
                {
                    SpawnEnemies(rng, spots, enemyPrefab, enemiesRoot);
                }
            }
        }

        private void SpawnEnemies(System.Random rng, List<Vector2Int> spots, GameObject prefab, Transform parent)
        {
            if (prefab == null) return;
            if (spots == null || spots.Count == 0) return;

            var count = Mathf.CeilToInt(spots.Count / Mathf.Max(1, spotsPerEnemy));
            for (var i = 0; i < count; i++)
            {
                if (spots.Count == 0) break;
                var idx = rng.Next(spots.Count);
                var spot = spots[idx];
                spots.RemoveAt(idx);

                Spawn(prefab, parent, TileCenterWorld(spot, 0f));
            }
        }

        private void PlaceFurniture(System.Random rng, DungeonMap map, List<Vector2Int> spots, GameObject prefab, int width, int height, Transform parent)
        {
            if (prefab == null) return;
            if (spots == null || spots.Count == 0) return;

            var fit = GetFitSpots(spots, width, height);
            if (fit.Count == 0) return;

            var spot = fit[rng.Next(fit.Count)];
            ApplyFurniture(map, spots, spot, width, height);

            // Center of furniture footprint
            var pos = new Vector3(spot.x + (width / 2f), spot.y + (height / 2f), 0f);
            Spawn(prefab, parent, pos);
        }

        private void ApplyFurniture(DungeonMap map, List<Vector2Int> spots, Vector2Int spot, int width, int height)
        {
            for (var r = 0; r < height; r++)
            for (var c = 0; c < width; c++)
            {
                var cc = spot.x + c;
                var rr = spot.y + r;

                if (map.InBounds(cc, rr))
                {
                    map.Set(cc, rr, DungeonCell.Furniture);
                }

                // remove occupied tiles from spots
                for (var i = spots.Count - 1; i >= 0; i--)
                {
                    if (spots[i].x == cc && spots[i].y == rr)
                        spots.RemoveAt(i);
                }
            }
        }

        private List<Vector2Int> GetFitSpots(List<Vector2Int> spots, int width, int height)
        {
            var fit = new List<Vector2Int>();
            foreach (var s in spots)
            {
                var valid = true;
                for (var r = 0; r < height && valid; r++)
                for (var c = 0; c < width && valid; c++)
                {
                    if (c + r == 0) continue;
                    var p = new Vector2Int(s.x + c, s.y + r);
                    var found = false;
                    for (var i = 0; i < spots.Count; i++)
                    {
                        if (spots[i].x == p.x && spots[i].y == p.y) { found = true; break; }
                    }
                    if (!found) valid = false;
                }

                if (valid) fit.Add(s);
            }
            return fit;
        }

        private List<Vector2Int> GetSpots(DungeonMap map, DungeonGenerator2D.Room room)
        {
            var spots = new List<Vector2Int>();

            for (var r = 0; r < room.Height; r++)
            for (var c = 0; c < room.Width; c++)
            {
                var rr = room.Row + r;
                var cc = room.Column + c;

                // Neighbors: if any is Tunnel, skip (microStudio)
                var n1 = map.InBounds(cc, rr - 1) ? map.Get(cc, rr - 1) : DungeonCell.Wall;
                var n2 = map.InBounds(cc, rr + 1) ? map.Get(cc, rr + 1) : DungeonCell.Wall;
                var n3 = map.InBounds(cc - 1, rr) ? map.Get(cc - 1, rr) : DungeonCell.Wall;
                var n4 = map.InBounds(cc + 1, rr) ? map.Get(cc + 1, rr) : DungeonCell.Wall;

                if (n1 == DungeonCell.Tunnel || n2 == DungeonCell.Tunnel || n3 == DungeonCell.Tunnel || n4 == DungeonCell.Tunnel)
                    continue;

                if (map.Get(cc, rr) == DungeonCell.Room)
                {
                    spots.Add(new Vector2Int(cc, rr));
                }
            }

            return spots;
        }

        private static bool Roll(System.Random rng, float chance)
        {
            if (chance <= 0f) return false;
            if (chance >= 1f) return true;
            return rng.NextDouble() < chance;
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

        private void Spawn(GameObject prefab, Transform parent, Vector3 position)
        {
            var go = Instantiate(prefab, position, Quaternion.identity, parent);
            _spawned.Add(go);
        }

        private void ClearSpawned()
        {
            for (var i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null) DestroyImmediate(_spawned[i]);
            }
            _spawned.Clear();
        }

        private static Vector3 TileCenterWorld(Vector2Int tile, float z) => new(tile.x + 0.5f, tile.y + 0.5f, z);
    }
}


