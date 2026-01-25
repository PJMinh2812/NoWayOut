using UnityEngine;

namespace GloomCraft.Dungeon
{
    /// <summary>
    /// Handles player spawning and positioning in the dungeon.
    /// </summary>
    public sealed class PlayerSpawner : MonoBehaviour
    {
        [Header("Player Reference")]
        [SerializeField] private Transform player;
        
        [Header("Settings")]
        [SerializeField] private float spawnZOffset = 0f;
        [SerializeField] private bool findPlayerIfNull = true;

        private IDungeonRenderer _renderer;

        public Transform Player => player;

        private void Awake()
        {
            if (player == null && findPlayerIfNull)
            {
                var playerController = FindFirstObjectByType<PlayerController2D>();
                if (playerController != null)
                {
                    player = playerController.transform;
                }
            }
        }

        /// <summary>
        /// Initialize with a renderer for coordinate conversion.
        /// </summary>
        public void Initialize(IDungeonRenderer renderer)
        {
            _renderer = renderer;
        }

        /// <summary>
        /// Spawn player at the dungeon's start position.
        /// </summary>
        public void SpawnAtStart(DungeonMap map)
        {
            if (player == null)
            {
                Debug.LogWarning("[PlayerSpawner] No player assigned!");
                return;
            }

            if (map == null)
            {
                Debug.LogError("[PlayerSpawner] Cannot spawn - map is null!");
                return;
            }

            Vector3 position;
            
            if (_renderer != null)
            {
                position = _renderer.GridToWorld(map.Start.x, map.Start.y);
            }
            else
            {
                // Fallback calculation
                position = new Vector3(map.Start.x + 0.5f, map.Start.y + 0.5f, 0f);
            }

            position.z = spawnZOffset;
            player.position = position;

            DungeonEvents.RaisePlayerSpawned(position);
            Debug.Log($"[PlayerSpawner] Player spawned at grid ({map.Start.x}, {map.Start.y}) -> world {position}");
        }

        /// <summary>
        /// Spawn player at a specific grid position.
        /// </summary>
        public void SpawnAt(int column, int row)
        {
            if (player == null)
            {
                Debug.LogWarning("[PlayerSpawner] No player assigned!");
                return;
            }

            Vector3 position;
            
            if (_renderer != null)
            {
                position = _renderer.GridToWorld(column, row);
            }
            else
            {
                position = new Vector3(column + 0.5f, row + 0.5f, 0f);
            }

            position.z = spawnZOffset;
            player.position = position;

            DungeonEvents.RaisePlayerSpawned(position);
        }

        /// <summary>
        /// Set the player reference at runtime.
        /// </summary>
        public void SetPlayer(Transform playerTransform)
        {
            player = playerTransform;
        }
    }
}
