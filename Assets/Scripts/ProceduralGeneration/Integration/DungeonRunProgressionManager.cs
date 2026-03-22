using UnityEngine;
using ProceduralGeneration.Core;
using NWO;
using Core;
using System.Collections;
using System.IO;

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
        [SerializeField] private Transform player;

        [Header("Run Structure")]
        [SerializeField] private int totalRounds = 3;
        [SerializeField] private int mapsPerRound = 5;

        [Header("Difficulty Scaling")]
        [SerializeField] private int baseArchetypeRoomCount = 4;
        [SerializeField] private int roomIncreasePerRound = 1;
        [SerializeField] private float baseBranchProbability = 0.1f;
        [SerializeField] private float branchIncreasePerRound = 0.05f;

        [Header("Completion")]
        [SerializeField] private bool autoCompleteWhenNoEnemiesAlive = true;
        [SerializeField] private float minAutoCompleteDelay = 2f;
        [SerializeField] private float portalSpawnRetryInterval = 0.5f;
        [SerializeField] private int portalSpawnMaxRetries = 6;

        [Header("Spawn Override")]
        [Tooltip("Ep player ve toa do co dinh moi khi tao map moi")]
        [SerializeField] private bool forceFixedSpawnOnNewMap = true;
        [SerializeField] private Vector2 fixedSpawnPosition = new Vector2(6f, 6f);

        private int currentRound = 1;
        private int currentMap = 1;
        private bool currentMapCompleted;
        private float mapGeneratedAt;
        private GameObject spawnedPortal;
        private Coroutine portalSpawnRetryCoroutine;
        private Coroutine movePlayerRetryCoroutine;
        private bool hasForcedSeedForCurrentGenerate;
        private int forcedSeedForCurrentGenerate;

        public int CurrentRound => currentRound;
        public int CurrentMap => currentMap;
        public bool IsRunFinished => currentRound > totalRounds;

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
            GenerateCurrentMap();
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
            if (IsRunFinished || currentMapCompleted)
                return;

            if (!autoCompleteWhenNoEnemiesAlive)
                return;

            if (Time.time - mapGeneratedAt < minAutoCompleteDelay)
                return;

            bool noEnemyManager = EnemySpawnManager.Instance == null;
            bool allEnemiesDead = EnemySpawnManager.Instance != null && EnemySpawnManager.Instance.AliveEnemyCount <= 0;
            if (noEnemyManager || allEnemiesDead)
            {
                MarkCurrentMapCompleted();
            }
        }

        /// <summary>
        /// Đánh dấu map hiện tại đã hoàn thành và spawn portal ở Goal room.
        /// </summary>
        public void MarkCurrentMapCompleted()
        {
            if (currentMapCompleted)
                return;

            currentMapCompleted = true;
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

            if (currentMap < mapsPerRound)
            {
                currentMap++;
            }
            else
            {
                currentRound++;
                currentMap = 1;
            }

            if (currentRound > totalRounds)
            {
                DestroySpawnedPortal();
                Debug.Log("[RunProgression] Completed all maps (3-5). Run finished!");
                return true;
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

            DestroySpawnedPortal();
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

            MovePlayerToRespawnPoint();
            movePlayerRetryCoroutine = StartCoroutine(MovePlayerToRespawnPointReliable());

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
            Debug.Log($"[RunProgression] Generated map {currentRound}-{currentMap} | rooms={roomCount}, branch={branchProb:0.00}");
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
            Debug.Log($"[RunProgression] Portal spawned at goal room for map {currentRound}-{currentMap}.");
            return true;
        }

        private System.Collections.IEnumerator RetrySpawnPortalAtGoalRoom()
        {
            int retries = Mathf.Max(1, portalSpawnMaxRetries);
            float delay = Mathf.Max(0.1f, portalSpawnRetryInterval);

            for (int i = 0; i < retries && spawnedPortal == null; i++)
            {
                yield return new WaitForSeconds(delay);
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
    }
}
