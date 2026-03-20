using UnityEngine;
using ProceduralGeneration.Core;
using NWO;

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

        private int currentRound = 1;
        private int currentMap = 1;
        private bool currentMapCompleted;
        private float mapGeneratedAt;
        private GameObject spawnedPortal;
        private Coroutine portalSpawnRetryCoroutine;

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
            GenerateCurrentMap();
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
            dungeonManager.useRandomSeed = true;
            dungeonManager.GenerateDungeon();

            MovePlayerToRespawnPoint();

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

            GameObject respawnPoint = GameObject.Find("Respawn_Point");
            if (respawnPoint != null)
                player.position = respawnPoint.transform.position;
        }

        private void DestroySpawnedPortal()
        {
            if (spawnedPortal != null)
                Destroy(spawnedPortal);
            spawnedPortal = null;
        }
    }
}
