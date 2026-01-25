using System;
using System.Collections;
using UnityEngine;

namespace GloomCraft.Dungeon
{
    /// <summary>
    /// Central orchestrator for dungeon map initialization.
    /// Coordinates generation, rendering, spawning in proper sequence.
    /// </summary>
    public sealed class MapInitializationManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private DungeonConfig config;
        
        [Header("Components")]
        [SerializeField] private DungeonTilemapRenderer tilemapRenderer;
        [SerializeField] private PlayerSpawner playerSpawner;
        [SerializeField] private EntitySpawner entitySpawner;
        
        [Header("Options")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool useCoroutineSequence = false;
        [SerializeField] private float stepDelay = 0.1f;

        // Current dungeon state
        private DungeonGenerator2D.Result _currentResult;
        private bool _isInitialized;

        public DungeonGenerator2D.Result CurrentResult => _currentResult;
        public DungeonMap CurrentMap => _currentResult?.Map;
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Event fired when map initialization is complete.
        /// </summary>
        public event Action OnInitializationComplete;

        private void Start()
        {
            if (initializeOnStart)
            {
                // Check if we should generate a new random map (from Main Menu Play button)
                if (PlayerPrefs.GetInt("GenerateNewMap", 0) == 1)
                {
                    PlayerPrefs.DeleteKey("GenerateNewMap");
                    PlayerPrefs.Save();
                    
                    if (config != null)
                    {
                        // Generate new random seed for different map
                        config.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                        config.useSeed = true;
                        Debug.Log($"[MapInitializationManager] New game - generating with seed: {config.seed}");
                    }
                }
                
                InitializeMap();
            }
        }

        private void OnDestroy()
        {
            DungeonEvents.ClearAll();
        }

        /// <summary>
        /// Initialize the map with current configuration.
        /// </summary>
        [ContextMenu("Initialize Map")]
        public void InitializeMap()
        {
            if (config == null)
            {
                Debug.LogError("[MapInitializationManager] No DungeonConfig assigned!");
                return;
            }

            InitializeMap(config);
        }

        /// <summary>
        /// Initialize the map with specific configuration.
        /// </summary>
        public void InitializeMap(DungeonConfig dungeonConfig)
        {
            if (dungeonConfig == null)
            {
                Debug.LogError("[MapInitializationManager] DungeonConfig is null!");
                return;
            }

            config = dungeonConfig;
            FindComponents();

            if (useCoroutineSequence)
            {
                StartCoroutine(InitializeSequenceCoroutine());
            }
            else
            {
                InitializeSequenceImmediate();
            }
        }

        /// <summary>
        /// Regenerate the map (useful for restart/new level).
        /// </summary>
        [ContextMenu("Regenerate Map")]
        public void RegenerateMap()
        {
            _isInitialized = false;
            InitializeMap();
        }

        /// <summary>
        /// Regenerate the map with a new random seed.
        /// This ensures a completely different layout each time.
        /// </summary>
        public void RegenerateWithNewSeed()
        {
            if (config == null)
            {
                Debug.LogError("[MapInitializationManager] No DungeonConfig assigned!");
                return;
            }

            // Generate new random seed
            config.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            config.useSeed = true;
            
            Debug.Log($"[MapInitializationManager] Regenerating map with new seed: {config.seed}");
            
            // Clear and regenerate
            ClearMap();
            _isInitialized = false;
            InitializeMap();
        }

        /// <summary>
        /// Clear all dungeon content.
        /// </summary>
        [ContextMenu("Clear Map")]
        public void ClearMap()
        {
            tilemapRenderer?.Clear();
            entitySpawner?.ClearSpawned();
            _currentResult = null;
            _isInitialized = false;
        }

        private void InitializeSequenceImmediate()
        {
            try
            {
                // Step 1: Generate dungeon data
                GenerateDungeon();

                // Step 2: Render tilemap
                RenderDungeon();

                // Step 3: Spawn player
                SpawnPlayer();

                // Step 4: Spawn entities
                SpawnEntities();

                // Step 5: Complete
                CompleteInitialization();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MapInitializationManager] Initialization failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private IEnumerator InitializeSequenceCoroutine()
        {
            // Step 1: Generate dungeon data
            GenerateDungeon();
            yield return new WaitForSeconds(stepDelay);

            // Step 2: Render tilemap
            RenderDungeon();
            yield return new WaitForSeconds(stepDelay);

            // Step 3: Spawn player
            SpawnPlayer();
            yield return new WaitForSeconds(stepDelay);

            // Step 4: Spawn entities
            SpawnEntities();
            yield return new WaitForSeconds(stepDelay);

            // Step 5: Complete
            CompleteInitialization();
        }

        private void GenerateDungeon()
        {
            Debug.Log("[MapInitializationManager] Step 1: Generating dungeon...");

            int? seed = config.useSeed ? config.seed : null;
            
            _currentResult = DungeonGenerator2D.Generate(
                config.columns,
                config.rows,
                config.minRoomSize,
                config.maxRoomSize,
                config.density,
                seed
            );

            DungeonEvents.RaiseDungeonGenerated(_currentResult);
            Debug.Log($"[MapInitializationManager] Generated dungeon with {_currentResult.Rooms.Count} rooms");
        }

        private void RenderDungeon()
        {
            Debug.Log("[MapInitializationManager] Step 2: Rendering dungeon...");

            if (tilemapRenderer == null)
            {
                Debug.LogError("[MapInitializationManager] No DungeonTilemapRenderer found!");
                return;
            }

            tilemapRenderer.Render(_currentResult.Map);
            DungeonEvents.RaiseDungeonRendered(_currentResult.Map);
        }

        private void SpawnPlayer()
        {
            Debug.Log("[MapInitializationManager] Step 3: Spawning player...");

            if (playerSpawner == null)
            {
                Debug.LogWarning("[MapInitializationManager] No PlayerSpawner found - skipping player spawn");
                return;
            }

            playerSpawner.Initialize(tilemapRenderer);
            playerSpawner.SpawnAtStart(_currentResult.Map);
        }

        private void SpawnEntities()
        {
            Debug.Log("[MapInitializationManager] Step 4: Spawning entities...");

            if (entitySpawner == null)
            {
                Debug.LogWarning("[MapInitializationManager] No EntitySpawner found - skipping entity spawn");
                return;
            }

            entitySpawner.ApplyConfig(config);
            entitySpawner.SpawnEntities(_currentResult);
        }

        private void CompleteInitialization()
        {
            _isInitialized = true;
            
            DungeonEvents.RaiseMapReady();
            OnInitializationComplete?.Invoke();
            
            Debug.Log("[MapInitializationManager] ✅ Map initialization complete!");
        }

        private void FindComponents()
        {
            if (tilemapRenderer == null)
            {
                tilemapRenderer = GetComponentInChildren<DungeonTilemapRenderer>();
                if (tilemapRenderer == null)
                {
                    tilemapRenderer = FindFirstObjectByType<DungeonTilemapRenderer>();
                }
            }

            if (playerSpawner == null)
            {
                playerSpawner = GetComponentInChildren<PlayerSpawner>();
                if (playerSpawner == null)
                {
                    playerSpawner = FindFirstObjectByType<PlayerSpawner>();
                }
            }

            if (entitySpawner == null)
            {
                entitySpawner = GetComponentInChildren<EntitySpawner>();
                if (entitySpawner == null)
                {
                    entitySpawner = FindFirstObjectByType<EntitySpawner>();
                }
            }
        }

        #if UNITY_EDITOR
        [ContextMenu("Create Default Config")]
        private void CreateDefaultConfig()
        {
            if (config != null) return;
            
            config = ScriptableObject.CreateInstance<DungeonConfig>();
            Debug.Log("[MapInitializationManager] Created default DungeonConfig (not saved to disk)");
        }
        #endif
    }
}
