using UnityEngine;
using ProceduralGeneration.Core;
using NWO;
using Core;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Rendering.Universal;

namespace ProceduralGeneration.Integration
{
    /// <summary>
    /// Quản lý run progression: 3 vòng x 5 map nhỏ.
    /// Khi hoàn thành map sẽ spawn portal ở Goal room để qua map tiếp theo.
    /// </summary>
    public class DungeonRunProgressionManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DungeonManager dungeonManager;
        [SerializeField] private GameObject portalPrefab;
        [SerializeField] private GameObject goalChestPrefab;
        [SerializeField] private Transform player;

        [Header("Run Structure")]
        [SerializeField] private int totalRounds = 3;
        [SerializeField] private int mapsPerRound = 5;

        [Header("Difficulty Scaling")]
        [SerializeField] private int baseArchetypeRoomCount = 4;
        [SerializeField] private int roomIncreasePerRound = 1;
        [SerializeField] private float baseBranchProbability = 0.1f;
        [SerializeField] private float branchIncreasePerRound = 0.05f;

        [Header("AI Director")]
        [SerializeField] private bool enableAIDirector = true;
        [SerializeField] private float targetClearTimeSeconds = 75f;
        [SerializeField] private float clearTimeToleranceSeconds = 20f;
        [Range(0f, 1f)]
        [SerializeField] private float targetEndHealthRatio = 0.65f;
        [Range(0.05f, 1f)]
        [SerializeField] private float endHealthTolerance = 0.2f;
        [SerializeField] private int targetDamageTaken = 20;
        [SerializeField] private int damageTakenTolerance = 12;
        [SerializeField] private int targetTrapTriggers = 2;
        [SerializeField] private int trapTriggerTolerance = 2;
        [Range(0.05f, 0.5f)]
        [SerializeField] private float aiDifficultyStep = 0.15f;
        [SerializeField] private float aiRoomScale = 3f;
        [SerializeField] private float aiBranchScale = 0.18f;
        [SerializeField] private float minAiDifficultyBias = -0.35f;
        [SerializeField] private float maxAiDifficultyBias = 0.35f;
        [SerializeField] private float trapIntensityScale = 0.6f;

        [Header("Completion")]
        [SerializeField] private bool autoCompleteWhenNoEnemiesAlive = true;
        [SerializeField] private float minAutoCompleteDelay = 2f;
        [SerializeField] private float portalSpawnRetryInterval = 0.5f;
        [SerializeField] private int portalSpawnMaxRetries = 6;

        [Header("Goal Chest")]
        [SerializeField] private int totalGoalChests = 3;
        [SerializeField] private Vector2 goalChestOffsetDirection = new Vector2(1f, 0f);
        [SerializeField] private float goalChestDistanceFromPortal = 0.6f;
        [SerializeField] private bool syncGoalChestLightWithPortal = true;

        [Header("Run Endings")]
        [SerializeField] private bool loadEndingSceneOnRunFinish = true;
        [SerializeField] private string goodEndingSceneName = "Ending_Good";
        [SerializeField] private string badEndingSceneName = "Ending_Bad";

        [Header("Spawn Override")]
        [Tooltip("Ep player ve toa do co dinh moi khi tao map moi")]
        [SerializeField] private bool forceFixedSpawnOnNewMap = true;
        [SerializeField] private Vector2 fixedSpawnPosition = new Vector2(6f, 6f);
        [Tooltip("Neu bat, map moi se duoc tao tai vi tri hien tai cua player khi qua portal")]
        [SerializeField] private bool spawnMapAtPlayerCurrentPosition = true;

        private int currentRound = 1;
        private int currentMap = 1;
        private bool currentMapCompleted;
        private float mapGeneratedAt;
        private GameObject spawnedPortal;
        private Coroutine portalSpawnRetryCoroutine;
        private Coroutine movePlayerRetryCoroutine;
        private bool hasForcedSeedForCurrentGenerate;
        private int forcedSeedForCurrentGenerate;
        private bool hasPendingMapSpawnOverride;
        private Vector3 pendingMapSpawnPosition;
        private bool isRestoringMapFromSave;
        private int openedGoalChestMask;
        private int openedGoalChestCount;
        private GameObject spawnedGoalChest;
        private float aiDifficultyBias;
        private float trapIntensityMultiplier = 1f;
        private bool hasLastMapPerformance;
        private MapPerformanceSnapshot lastMapPerformance;
        private bool mapPerformanceFinalized;
        private int mapStartHealth;
        private int mapStartMaxHealth;
        private int mapStartDamageTaken;
        private int mapStartDeaths;
        private int mapStartTrapTriggers;

        public int CurrentRound => currentRound;
        public int CurrentMap => currentMap;
        public bool IsRunFinished => currentRound > totalRounds;
        public bool IsCurrentMapCompleted => currentMapCompleted;
        public int OpenedGoalChestMask => openedGoalChestMask;
        public int OpenedGoalChestCount => openedGoalChestCount;
        public int TotalGoalChests => Mathf.Max(1, totalGoalChests);
        public float CurrentAIDifficultyBias => aiDifficultyBias;
        public float CurrentTrapIntensityMultiplier => trapIntensityMultiplier;

        private struct MapPerformanceSnapshot
        {
            public int round;
            public int map;
            public float clearTime;
            public int deaths;
            public int endHealth;
            public int maxHealth;
            public int damageTaken;
            public int trapTriggers;

            public float EndHealthRatio => maxHealth <= 0 ? 0f : (float)endHealth / maxHealth;
        }

        private void Awake()
        {
            if (dungeonManager == null)
                dungeonManager = FindFirstObjectByType<DungeonManager>();

            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }
        }

        private void Start()
        {
            if (!TryRestoreMapFromSave())
            {
                GenerateCurrentMap();
            }
        }

        private bool TryRestoreMapFromSave()
        {
            bool restoreFromSave = PlayerPrefs.GetInt("RestoreDungeonFromSave", 0) == 1 ||
                                   PlayerPrefs.GetInt("LoadFromSave", 0) == 1;
            if (!restoreFromSave)
                return false;

            SaveData data = LoadSaveDataDirectly();
            if (data == null)
                return false;

            if (data.hasRunProgressionState)
            {
                currentRound = Mathf.Clamp(data.runCurrentRound, 1, Mathf.Max(1, totalRounds));
                currentMap = Mathf.Clamp(data.runCurrentMap, 1, Mathf.Max(1, mapsPerRound));
                SetOpenedGoalChestMask(data.runOpenedGoalChestMask);
            }

            bool shouldRestoreMapCompleted = data.hasRunCurrentMapCompleted && data.runCurrentMapCompleted;

            if (data.hasMapAnchor)
            {
                hasPendingMapSpawnOverride = true;
                pendingMapSpawnPosition = new Vector3(data.mapAnchorX, data.mapAnchorY, 0f);
                Debug.Log($"[RunProgression] Restoring map anchor from save at {pendingMapSpawnPosition}");
            }

            string seedSource = "none";
            if (data.hasDungeonSeed && data.dungeonSeed != 0)
            {
                hasForcedSeedForCurrentGenerate = true;
                forcedSeedForCurrentGenerate = data.dungeonSeed;
                seedSource = "save";
            }
            else
            {
                int fallbackSeed = PlayerPrefs.GetInt("LastDungeonSeed", 0);
                if (fallbackSeed != 0)
                {
                    hasForcedSeedForCurrentGenerate = true;
                    forcedSeedForCurrentGenerate = fallbackSeed;
                    seedSource = "playerprefs";
                }
            }

            PlayerPrefs.SetInt("RestoreDungeonFromSave", 0);
            PlayerPrefs.Save();

            int requestedSeed = hasForcedSeedForCurrentGenerate ? forcedSeedForCurrentGenerate : 0;
            isRestoringMapFromSave = true;
            GenerateCurrentMap();
            if (shouldRestoreMapCompleted)
            {
                MarkCurrentMapCompleted();
                Debug.Log($"[RunProgression] Restored current map completion state for {currentRound}-{currentMap}.");
            }
            isRestoringMapFromSave = false;
            int generatedSeed = dungeonManager != null ? dungeonManager.GetCurrentSeed() : 0;
            Debug.Log($"[RunProgression] Restored saved map seedSource={seedSource}, requestedSeed={requestedSeed}, generatedSeed={generatedSeed}, round={currentRound}, map={currentMap}");
            return true;
        }

        private SaveData LoadSaveDataDirectly()
        {
            if (SaveManager.Instance != null)
            {
                return SaveManager.Instance.LoadSaveData();
            }

            string savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
            if (!File.Exists(savePath))
                return null;

            try
            {
                string json = File.ReadAllText(savePath);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch
            {
                return null;
            }
        }

        private void Update()
        {
            if (IsRunFinished)
                return;

            if (currentMapCompleted)
            {
                EnsureCompletionObjectsPresent();
                return;
            }

            if (!autoCompleteWhenNoEnemiesAlive)
                return;

            if (Time.time - mapGeneratedAt < minAutoCompleteDelay)
                return;

            // Only auto-complete if the enemy manager exists AND all spawned enemies are dead
            bool allEnemiesDead = EnemySpawnManager.Instance != null && EnemySpawnManager.Instance.AliveEnemyCount <= 0;
            if (allEnemiesDead)
            {
                MarkCurrentMapCompleted();
            }
        }

        private void EnsureCompletionObjectsPresent()
        {
            if (ShouldSpawnGoalChestOnCurrentMap() && spawnedGoalChest == null)
            {
                SpawnGoalChestAtGoalRoom();
            }

            if (spawnedPortal == null)
            {
                bool spawned = SpawnPortalAtGoalRoom();
                if (!spawned && portalSpawnRetryCoroutine == null)
                    portalSpawnRetryCoroutine = StartCoroutine(RetrySpawnPortalAtGoalRoom());
            }
        }

        /// <summary>
        /// Đánh dấu map hiện tại đã hoàn thành và spawn portal ở Goal room.
        /// </summary>
        public void MarkCurrentMapCompleted()
        {
            if (currentMapCompleted)
                return;

            FinalizeMapPerformanceIfNeeded();
            currentMapCompleted = true;
            SpawnGoalChestAtGoalRoom();
            bool spawned = SpawnPortalAtGoalRoom();
            if (!spawned && portalSpawnRetryCoroutine == null)
                portalSpawnRetryCoroutine = StartCoroutine(RetrySpawnPortalAtGoalRoom());

            Debug.Log($"[RunProgression] Map completed: {currentRound}-{currentMap}");
        }

        /// <summary>
        /// Gọi từ portal khi player xác nhận qua map tiếp theo.
        /// </summary>
        public bool TryAdvanceToNextMap()
        {
            return TryAdvanceToNextMap(null);
        }

        /// <summary>
        /// Gọi từ portal khi player xác nhận qua map tiếp theo, có thể truyền vị trí neo spawn.
        /// </summary>
        public bool TryAdvanceToNextMap(Vector3? spawnAnchorWorldPosition)
        {
            if (!currentMapCompleted)
            {
                // Fallback: in scenes without enemy system, allow portal to complete map on confirm.
                if (autoCompleteWhenNoEnemiesAlive)
                {
                    bool noEnemyManager = EnemySpawnManager.Instance == null;
                    bool allEnemiesDead = EnemySpawnManager.Instance != null && EnemySpawnManager.Instance.AliveEnemyCount <= 0;

                    if (noEnemyManager || allEnemiesDead)
                    {
                        MarkCurrentMapCompleted();
                    }
                }

                if (!currentMapCompleted)
                {
                    Debug.Log("[RunProgression] Cannot advance: current map is not completed yet.");
                    return false;
                }
            }

            bool isFinalMap = currentRound >= totalRounds && currentMap >= mapsPerRound;
            if (isFinalMap)
            {
                bool finished = HandleRunFinished();
                if (!finished)
                {
                    // Ending scene is not configured, keep current map state (e.g. 3-5)
                    // and ensure completion objects remain visible.
                    EnsureCompletionObjectsPresent();
                }

                return finished;
            }

            if (spawnAnchorWorldPosition.HasValue)
            {
                hasPendingMapSpawnOverride = true;
                pendingMapSpawnPosition = new Vector3(
                    spawnAnchorWorldPosition.Value.x,
                    spawnAnchorWorldPosition.Value.y,
                    0f);
                Debug.Log($"[RunProgression] Captured portal spawn anchor (explicit) at {pendingMapSpawnPosition}");
            }
            else
            {
                CaptureSpawnAnchorFromPlayer();
            }

            if (currentMap < mapsPerRound)
            {
                currentMap++;
            }
            else
            {
                currentRound++;
                currentMap = 1;
            }

            GenerateCurrentMap();
            return true;
        }

        private void GenerateCurrentMap()
        {
            if (dungeonManager == null)
            {
                Debug.LogError("[RunProgression] DungeonManager not found.");
                return;
            }

            Vector3 desiredSpawnPosition = ResolveDesiredSpawnPosition();

            DestroySpawnedPortal();
            DestroySpawnedGoalChest();
            currentMapCompleted = false;

            if (portalSpawnRetryCoroutine != null)
            {
                StopCoroutine(portalSpawnRetryCoroutine);
                portalSpawnRetryCoroutine = null;
            }

            if (EnemySpawnManager.Instance != null)
                EnemySpawnManager.Instance.DespawnAll();

            int roundIndex = Mathf.Max(0, currentRound - 1);
            int roomCount = Mathf.Clamp(baseArchetypeRoomCount + roundIndex * roomIncreasePerRound, 3, 10);
            float branchProb = Mathf.Clamp01(baseBranchProbability + roundIndex * branchIncreasePerRound);
            ApplyAIDirectorAdjustments(ref roomCount, ref branchProb);

            dungeonManager.archetype1RoomCount = roomCount;
            dungeonManager.archetype2RoomCount = roomCount;
            dungeonManager.branchProbability = branchProb;

            if (hasForcedSeedForCurrentGenerate && forcedSeedForCurrentGenerate != 0)
            {
                dungeonManager.seed = forcedSeedForCurrentGenerate;
                dungeonManager.useRandomSeed = false;
            }
            else
            {
                dungeonManager.useRandomSeed = true;
            }

            dungeonManager.GenerateDungeon();

            hasForcedSeedForCurrentGenerate = false;
            forcedSeedForCurrentGenerate = 0;

            if (movePlayerRetryCoroutine != null)
            {
                StopCoroutine(movePlayerRetryCoroutine);
                movePlayerRetryCoroutine = null;
            }

            AlignGeneratedDungeonToSpawnPosition(desiredSpawnPosition);
            TrySpawnRunStartFragments();

            // Update RoomTransitionManager để biết player ở phòng start
            Room startRoom = dungeonManager.GetStartRoom();
            if (startRoom != null)
            {
                var transitionManager = FindFirstObjectByType<RoomTransitionManager>();
                if (transitionManager != null)
                {
                    transitionManager.SetCurrentRoom(startRoom);
                    Debug.Log($"[RunProgression] Set current room to start room after map advance");
                }
            }

            if (DungeonLightingManager.Instance != null)
            {
                DungeonLightingManager.Instance.RefreshSpecialRoomLights();
            }

            mapGeneratedAt = Time.time;
            BeginMapPerformanceTracking();
            Debug.Log($"[RunProgression] Generated map {currentRound}-{currentMap} | rooms={roomCount}, branch={branchProb:0.00}");
        }

        private void BeginMapPerformanceTracking()
        {
            mapPerformanceFinalized = false;

            PlayerHealth2D health = ResolvePlayerHealth();
            mapStartHealth = health != null ? health.CurrentHealth : 0;
            mapStartMaxHealth = health != null ? Mathf.Max(1, health.MaxHealth) : 1;
            mapStartDamageTaken = RunAIDirectorTelemetry.TotalDamageTaken;
            mapStartDeaths = RunAIDirectorTelemetry.TotalDeaths;
            mapStartTrapTriggers = RunAIDirectorTelemetry.TotalTrapTriggers;
        }

        private void FinalizeMapPerformanceIfNeeded()
        {
            if (mapPerformanceFinalized)
                return;

            mapPerformanceFinalized = true;

            PlayerHealth2D health = ResolvePlayerHealth();
            int endHealth = health != null ? health.CurrentHealth : mapStartHealth;
            int maxHealth = health != null ? Mathf.Max(1, health.MaxHealth) : Mathf.Max(1, mapStartMaxHealth);

            MapPerformanceSnapshot snapshot = new MapPerformanceSnapshot
            {
                round = currentRound,
                map = currentMap,
                clearTime = Mathf.Max(0f, Time.time - mapGeneratedAt),
                deaths = Mathf.Max(0, RunAIDirectorTelemetry.TotalDeaths - mapStartDeaths),
                endHealth = endHealth,
                maxHealth = maxHealth,
                damageTaken = Mathf.Max(0, RunAIDirectorTelemetry.TotalDamageTaken - mapStartDamageTaken),
                trapTriggers = Mathf.Max(0, RunAIDirectorTelemetry.TotalTrapTriggers - mapStartTrapTriggers)
            };

            lastMapPerformance = snapshot;
            hasLastMapPerformance = true;

            Debug.Log($"[RunProgression][AIDirector] Map performance R{snapshot.round}-M{snapshot.map}: clear={snapshot.clearTime:0.0}s, deaths={snapshot.deaths}, endHP={snapshot.endHealth}/{snapshot.maxHealth}, dmgTaken={snapshot.damageTaken}, trapTriggers={snapshot.trapTriggers}");
            
            // Reset telemetry để map kế tiếp tracked từ 0
            RunAIDirectorTelemetry.ResetAll();
        }

        private PlayerHealth2D ResolvePlayerHealth()
        {
            if (player != null)
                return player.GetComponent<PlayerHealth2D>();

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                return playerObj.GetComponent<PlayerHealth2D>();
            }

            return null;
        }

        private void ApplyAIDirectorAdjustments(ref int roomCount, ref float branchProb)
        {
            Debug.Log($"[RunProgression][AIDirector] Called ApplyAIDirectorAdjustments: roomCount={roomCount}, branchProb={branchProb:0.00}, enableAI={enableAIDirector}, hasData={hasLastMapPerformance}");
            
            trapIntensityMultiplier = Mathf.Clamp(1f + aiDifficultyBias * trapIntensityScale, 0.7f, 1.4f);

            if (!enableAIDirector)
            {
                Debug.Log($"[RunProgression][AIDirector] Skipped: enableAIDirector={enableAIDirector}");
                return;
            }

            if (!hasLastMapPerformance)
            {
                Debug.Log($"[RunProgression][AIDirector] Skipped: No last map performance data (first map of run)");
                return;
            }

            float clearTimeSignal = GetCenteredSignal(targetClearTimeSeconds, clearTimeToleranceSeconds, lastMapPerformance.clearTime, true);
            float endHealthSignal = GetCenteredSignal(targetEndHealthRatio, endHealthTolerance, lastMapPerformance.EndHealthRatio, false);
            float damageSignal = GetCenteredSignal(targetDamageTaken, damageTakenTolerance, lastMapPerformance.damageTaken, true);
            float trapSignal = GetCenteredSignal(targetTrapTriggers, trapTriggerTolerance, lastMapPerformance.trapTriggers, true);
            float deathSignal = -Mathf.Clamp01(lastMapPerformance.deaths / 2f);

            float combinedSignal =
                clearTimeSignal * 0.35f +
                endHealthSignal * 0.25f +
                damageSignal * 0.20f +
                trapSignal * 0.10f +
                deathSignal * 0.10f;

            aiDifficultyBias = Mathf.Clamp(
                aiDifficultyBias + combinedSignal * aiDifficultyStep,
                minAiDifficultyBias,
                maxAiDifficultyBias);

            int aiRoomDelta = Mathf.RoundToInt(aiDifficultyBias * aiRoomScale);
            float aiBranchDelta = aiDifficultyBias * aiBranchScale;

            roomCount = Mathf.Clamp(roomCount + aiRoomDelta, 3, 10);
            branchProb = Mathf.Clamp01(branchProb + aiBranchDelta);
            trapIntensityMultiplier = Mathf.Clamp(1f + aiDifficultyBias * trapIntensityScale, 0.7f, 1.4f);

            Debug.Log($"[RunProgression][AIDirector] Applied bias={aiDifficultyBias:0.000}, signals(clear={clearTimeSignal:0.00}, hp={endHealthSignal:0.00}, dmg={damageSignal:0.00}, trap={trapSignal:0.00}, death={deathSignal:0.00}), roomDelta={aiRoomDelta}, branchDelta={aiBranchDelta:0.000}, trapIntensity={trapIntensityMultiplier:0.00}");
        }

        private static float GetCenteredSignal(float target, float tolerance, float actual, bool inverted)
        {
            float safeTolerance = Mathf.Max(0.0001f, tolerance);
            float normalized = Mathf.Clamp((actual - target) / safeTolerance, -1f, 1f);
            return inverted ? -normalized : normalized;
        }

        private void TrySpawnRunStartFragments()
        {
            // Chỉ spawn fragment cố định ở run mới map 1-1.
            if (isRestoringMapFromSave)
                return;

            if (currentRound != 1 || currentMap != 1)
                return;

            LightFragmentSpawner spawner = dungeonManager != null
                ? dungeonManager.GetComponent<LightFragmentSpawner>()
                : null;

            if (spawner == null && dungeonManager != null)
                spawner = dungeonManager.gameObject.AddComponent<LightFragmentSpawner>();

            if (spawner == null)
                return;

            spawner.SpawnFragmentsForRunStart(dungeonManager);
        }

        private Vector3 ResolveDesiredSpawnPosition()
        {
            if (hasPendingMapSpawnOverride)
                return pendingMapSpawnPosition;

            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            if (spawnMapAtPlayerCurrentPosition && player != null)
                return new Vector3(player.position.x, player.position.y, 0f);

            if (forceFixedSpawnOnNewMap)
                return new Vector3(fixedSpawnPosition.x, fixedSpawnPosition.y, 0f);

            GameObject respawnPoint = GameObject.Find("Respawn_Point");
            if (respawnPoint != null)
                return respawnPoint.transform.position;

            return Vector3.zero;
        }

        private void CaptureSpawnAnchorFromPlayer()
        {
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            if (player == null)
            {
                hasPendingMapSpawnOverride = false;
                return;
            }

            hasPendingMapSpawnOverride = true;
            pendingMapSpawnPosition = new Vector3(player.position.x, player.position.y, 0f);
            Debug.Log($"[RunProgression] Captured portal spawn anchor at {pendingMapSpawnPosition}");
        }

        /// <summary>
        /// Dịch toàn bộ dungeon sao cho Respawn_Point (start room center) trùng với vị trí mong muốn.
        /// Cách này ổn định hơn so với tự tính center bằng actualSize.
        /// </summary>
        private void AlignGeneratedDungeonToSpawnPosition(Vector3 desiredSpawnPosition)
        {
            GameObject respawnPoint = GameObject.Find("Respawn_Point");
            if (respawnPoint == null)
            {
                Debug.LogWarning("[RunProgression] Respawn_Point not found after generation. Cannot align dungeon.");
                return;
            }

            Vector3 currentSpawnPosition = respawnPoint.transform.position;
            Vector3 offset = desiredSpawnPosition - currentSpawnPosition;

            if (offset.sqrMagnitude > 0.0001f)
            {
                if (dungeonManager.dungeonContainer != null)
                {
                    dungeonManager.dungeonContainer.position += offset;
                }
                else
                {
                    List<Room> allRooms = dungeonManager.GetAllRooms();
                    if (allRooms != null)
                    {
                        foreach (var room in allRooms)
                        {
                            if (room.roomInstance != null)
                                room.roomInstance.transform.position += offset;
                        }
                    }
                }
            }

            respawnPoint.transform.position = desiredSpawnPosition;

            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            var rb = player != null ? player.GetComponent<Rigidbody2D>() : null;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            hasPendingMapSpawnOverride = false;

            Debug.Log($"[RunProgression] Aligned dungeon. desiredSpawn={desiredSpawnPosition}, previousSpawn={currentSpawnPosition}, offset={offset}");
        }

        private bool SpawnPortalAtGoalRoom()
        {
            if (portalPrefab == null)
            {
                Debug.LogWarning("[RunProgression] portalPrefab chưa gán.");
                return false;
            }

            if (spawnedPortal != null)
                return true;

            Room goalRoom = dungeonManager.GetGoalRoom();
            if (goalRoom == null || goalRoom.roomInstance == null)
            {
                Debug.LogWarning("[RunProgression] Goal room chưa sẵn sàng.");
                return false;
            }

            Vector3 goalCenter = goalRoom.roomInstance.transform.position +
                                 new Vector3(goalRoom.actualSize.x * 0.5f, goalRoom.actualSize.y * 0.5f, 0f);

            spawnedPortal = Instantiate(portalPrefab, goalCenter, Quaternion.identity, goalRoom.roomInstance.transform);
            spawnedPortal.name = $"Portal_{currentRound}_{currentMap}";

            GoalPortal portal = spawnedPortal.GetComponent<GoalPortal>();
            if (portal == null)
                portal = spawnedPortal.AddComponent<GoalPortal>();

            portal.Setup(this);
            TrySyncGoalChestLightWithPortal();
            Debug.Log($"[RunProgression] Portal spawned at goal room for map {currentRound}-{currentMap}.");
            return true;
        }

        private bool SpawnGoalChestAtGoalRoom()
        {
            if (!ShouldSpawnGoalChestOnCurrentMap())
                return false;

            if (goalChestPrefab == null)
            {
                Debug.LogWarning("[RunProgression] goalChestPrefab chua gan.");
                return false;
            }

            if (spawnedGoalChest != null)
                return true;

            Room goalRoom = dungeonManager.GetGoalRoom();
            if (goalRoom == null || goalRoom.roomInstance == null)
            {
                Debug.LogWarning("[RunProgression] Goal room chua san sang de spawn chest.");
                return false;
            }

            Vector3 goalCenter = goalRoom.roomInstance.transform.position +
                                 new Vector3(goalRoom.actualSize.x * 0.5f, goalRoom.actualSize.y * 0.5f, 0f);

            Vector2 direction = goalChestOffsetDirection.sqrMagnitude > 0.001f
                ? goalChestOffsetDirection.normalized
                : Vector2.right;
            float distance = Mathf.Max(0.5f, goalChestDistanceFromPortal);
            Vector3 chestSpawnPosition = goalCenter + new Vector3(direction.x, direction.y, 0f) * distance;

            spawnedGoalChest = Instantiate(goalChestPrefab, chestSpawnPosition, Quaternion.identity, goalRoom.roomInstance.transform);
            spawnedGoalChest.name = $"GoalChest_{currentRound}_{currentMap}";

            GoalChest chest = spawnedGoalChest.GetComponent<GoalChest>();
            if (chest == null)
                chest = spawnedGoalChest.AddComponent<GoalChest>();

            chest.Setup(this);
            TrySyncGoalChestLightWithPortal();
            Debug.Log($"[RunProgression] Goal chest spawned at map {currentRound}-{currentMap}. pos={chestSpawnPosition}");
            return true;
        }

        private void TrySyncGoalChestLightWithPortal()
        {
            if (!syncGoalChestLightWithPortal)
                return;

            if (spawnedGoalChest == null || spawnedPortal == null)
                return;

            Light2D[] portalLights = spawnedPortal.GetComponentsInChildren<Light2D>(true);
            if (portalLights == null || portalLights.Length == 0)
            {
                Debug.Log("[RunProgression] Portal has no Light2D to sync for chest.");
                return;
            }

            Light2D source = portalLights[0];
            Light2D target = spawnedGoalChest.GetComponentInChildren<Light2D>(true);
            if (target == null)
                target = spawnedGoalChest.AddComponent<Light2D>();

            CopyLight2D(source, target);
            Debug.Log("[RunProgression] Synced chest Light2D from portal.");
        }

        private static void CopyLight2D(Light2D source, Light2D target)
        {
            if (source == null || target == null)
                return;

            target.lightType = source.lightType;
            target.color = source.color;
            target.intensity = source.intensity;
            target.pointLightInnerRadius = source.pointLightInnerRadius;
            target.pointLightOuterRadius = source.pointLightOuterRadius;
            target.pointLightInnerAngle = source.pointLightInnerAngle;
            target.pointLightOuterAngle = source.pointLightOuterAngle;
            target.falloffIntensity = source.falloffIntensity;
            target.volumeIntensity = source.volumeIntensity;
            target.lightOrder = source.lightOrder;
            target.blendStyleIndex = source.blendStyleIndex;
            target.shadowsEnabled = source.shadowsEnabled;
            target.shadowIntensity = source.shadowIntensity;
            target.shadowVolumeIntensity = source.shadowVolumeIntensity;
            target.enabled = source.enabled;
        }

        private System.Collections.IEnumerator RetrySpawnPortalAtGoalRoom()
        {
            int retries = Mathf.Max(1, portalSpawnMaxRetries);
            float delay = Mathf.Max(0.1f, portalSpawnRetryInterval);

            for (int i = 0; i < retries && spawnedPortal == null; i++)
            {
                yield return new WaitForSeconds(delay);
                SpawnGoalChestAtGoalRoom();
                if (SpawnPortalAtGoalRoom())
                    break;
            }

            if (spawnedPortal == null)
                Debug.LogWarning("[RunProgression] Portal spawn retry exhausted. Goal room might not be ready.");

            portalSpawnRetryCoroutine = null;
        }

        private void MovePlayerToRespawnPoint()
        {
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            if (player == null)
                return;

            if (forceFixedSpawnOnNewMap)
            {
                Vector3 fixedPos = new Vector3(fixedSpawnPosition.x, fixedSpawnPosition.y, player.position.z);
                player.position = fixedPos;

                GameObject forcedRespawnPoint = GameObject.Find("Respawn_Point");
                if (forcedRespawnPoint == null)
                    forcedRespawnPoint = new GameObject("Respawn_Point");

                forcedRespawnPoint.transform.position = fixedPos;

                var forcedRb = player.GetComponent<Rigidbody2D>();
                if (forcedRb != null)
                {
                    forcedRb.linearVelocity = Vector2.zero;
                    forcedRb.angularVelocity = 0f;
                }

                return;
            }

            GameObject respawnPoint = GameObject.Find("Respawn_Point");
            if (respawnPoint != null)
            {
                player.position = respawnPoint.transform.position;

                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
            }
        }

        private IEnumerator MovePlayerToRespawnPointReliable()
        {
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForEndOfFrame();
                MovePlayerToRespawnPoint();
            }

            movePlayerRetryCoroutine = null;
        }

        private void DestroySpawnedPortal()
        {
            if (spawnedPortal != null)
                Destroy(spawnedPortal);
            spawnedPortal = null;
        }

        private void DestroySpawnedGoalChest()
        {
            if (spawnedGoalChest != null)
                Destroy(spawnedGoalChest);
            spawnedGoalChest = null;
        }

        private bool ShouldSpawnGoalChestOnCurrentMap()
        {
            if (currentMap != mapsPerRound)
                return false;

            if (currentRound < 1 || currentRound > TotalGoalChests)
                return false;

            return !IsChestOpenedForRound(currentRound);
        }

        public bool TryOpenGoalChest()
        {
            if (!ShouldSpawnGoalChestOnCurrentMap())
                return false;

            if (IsChestOpenedForRound(currentRound))
                return false;

            openedGoalChestMask |= 1 << (currentRound - 1);
            openedGoalChestCount = CountSetBits(openedGoalChestMask);

            Debug.Log($"[RunProgression] Opened goal chest {openedGoalChestCount}/{TotalGoalChests} at map {currentRound}-{currentMap}.");
            return true;
        }

        public bool IsCurrentRoundGoalChestOpened()
        {
            return IsChestOpenedForRound(currentRound);
        }

        private bool IsChestOpenedForRound(int round)
        {
            if (round < 1 || round > TotalGoalChests)
                return false;

            int bit = 1 << (round - 1);
            return (openedGoalChestMask & bit) != 0;
        }

        private void SetOpenedGoalChestMask(int mask)
        {
            int maxMask = (1 << Mathf.Min(31, TotalGoalChests)) - 1;
            openedGoalChestMask = mask & maxMask;
            openedGoalChestCount = CountSetBits(openedGoalChestMask);
        }

        private static int CountSetBits(int value)
        {
            int count = 0;
            int v = value;
            while (v != 0)
            {
                count += v & 1;
                v >>= 1;
            }

            return count;
        }

        private bool HandleRunFinished()
        {
            Debug.Log($"[RunProgression] Completed all maps (3-5). Chests {openedGoalChestCount}/{TotalGoalChests}.");

            if (!loadEndingSceneOnRunFinish)
            {
                Debug.LogWarning("[RunProgression] loadEndingSceneOnRunFinish=false. Keeping current final map state.");
                return false;
            }

            string endingScene = openedGoalChestCount >= TotalGoalChests
                ? goodEndingSceneName
                : badEndingSceneName;

            if (string.IsNullOrWhiteSpace(endingScene))
            {
                Debug.LogWarning("[RunProgression] Ending scene name trong. Bo qua chuyen scene ending.");
                return false;
            }

            SceneLoader.LoadScene(endingScene);
            return true;
        }
    }
}
