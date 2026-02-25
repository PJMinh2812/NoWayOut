using UnityEngine;
using System.Collections;

namespace NWO
{
    /// <summary>
    /// Level Manager cho Level_01_TheAwakening.
    /// Tự động setup Player spawn + Enemy spawn khi scene load.
    /// 
    /// CÁCH SỬ DỤNG:
    /// 1. Tạo Empty GameObject "LevelManager_TheAwakening"
    /// 2. Attach script này
    /// 3. Kéo Player.prefab và Enemy prefabs vào Inspector
    /// 4. Tùy chỉnh vị trí spawn trong Inspector hoặc dùng giá trị mặc định
    /// 
    /// Script sẽ tự động tạo PlayerSpawnManager và EnemySpawnManager
    /// nếu chúng chưa có trong scene.
    /// </summary>
    public class LevelManager_TheAwakening : MonoBehaviour
    {
        [Header("=== PLAYER SETUP ===")]
        [Tooltip("Player prefab (Assets/Data/Prefabs/Player.prefab)")]
        [SerializeField] private GameObject playerPrefab;

        [Tooltip("Vị trí spawn Player (Phòng Start)")]
        [SerializeField] private Vector2 playerSpawnPosition = new Vector2(-4f, 3f);

        [Header("=== ENEMY PREFABS ===")]
        [Tooltip("Marshmallow enemy prefab")]
        [SerializeField] private GameObject marshmallowPrefab;

        [Tooltip("Zombie enemy prefab")]
        [SerializeField] private GameObject zombiePrefab;

        [Tooltip("Rat Mini Boss prefab")]
        [SerializeField] private GameObject ratMiniBossPrefab;

        [Tooltip("Dage enemy prefab")]
        [SerializeField] private GameObject dagePrefab;

        [Header("=== PHÒNG START (Đỏ) 10x8 ===")]
        [Tooltip("Spawn enemies trong phòng start")]
        [SerializeField] private bool enableStartRoom = true;
        [SerializeField] private Vector2[] startRoomEnemyPositions = new Vector2[]
        {
            new Vector2(-7f, 1f),
            new Vector2(-2f, 5f),
        };

        [Header("=== HÀNH LANG VÀNG 30x6 ===")]
        [Tooltip("Spawn enemies trong hành lang")]
        [SerializeField] private bool enableHallway = true;
        [SerializeField] private Vector2[] hallwayEnemyPositions = new Vector2[]
        {
            new Vector2(5f, 3f),
            new Vector2(10f, 4f),
            new Vector2(15f, 2f),
            new Vector2(20f, 3f),
            new Vector2(25f, 5f),
        };

        [Header("=== PHÒNG MINI-BOSS (Xanh) 12x12 ===")]
        [Tooltip("Spawn Rat Mini Boss")]
        [SerializeField] private bool enableBossRoom = true;
        [SerializeField] private Vector2 bossRoomCenter = new Vector2(35f, 3f);
        [SerializeField] private Vector2[] bossRoomGuardPositions = new Vector2[]
        {
            new Vector2(32f, 5f),
            new Vector2(38f, 5f),
            new Vector2(32f, 1f),
            new Vector2(38f, 1f),
        };

        [Header("=== KHU VỰC CAM 20x8 ===")]
        [Tooltip("Spawn enemies trong khu vực cam")]
        [SerializeField] private bool enableOrangeZone = true;
        [SerializeField] private Vector2[] orangeZoneEnemyPositions = new Vector2[]
        {
            new Vector2(45f, 3f),
            new Vector2(48f, 5f),
            new Vector2(52f, 2f),
            new Vector2(55f, 4f),
        };

        [Header("=== PHÒNG GOAL (Tím) 10x10 ===")]
        [Tooltip("Spawn enemies trong phòng goal")]
        [SerializeField] private bool enableGoalRoom = true;
        [SerializeField] private Vector2[] goalRoomEnemyPositions = new Vector2[]
        {
            new Vector2(62f, 3f),
            new Vector2(65f, 5f),
            new Vector2(68f, 2f),
        };

        [Header("=== ROOM TRIGGERS ===")]
        [Tooltip("Kích hoạt spawn khi player vào phòng (thay vì spawn tất cả ngay)")]
        [SerializeField] private bool useRoomTriggers = false;

        // Cached references
        private PlayerSpawnManager _playerSpawnMgr;
        private EnemySpawnManager _enemySpawnMgr;

        private void Awake()
        {
            SetupPlayerSpawnManager();
            SetupEnemySpawnManager();
        }

        // =============================================
        //  PLAYER SETUP
        // =============================================

        private void SetupPlayerSpawnManager()
        {
            _playerSpawnMgr = FindFirstObjectByType<PlayerSpawnManager>();
            if (_playerSpawnMgr == null)
            {
                var obj = new GameObject("PlayerSpawnManager");
                _playerSpawnMgr = obj.AddComponent<PlayerSpawnManager>();
                Debug.Log("[LevelManager] Auto-created PlayerSpawnManager");
            }

            // Inject settings via serialized fields nếu cần
            // (PlayerSpawnManager sẽ tự tìm Respawn_Point)
            // Gán playerPrefab nếu có
            if (playerPrefab != null)
            {
                // Dùng reflection hoặc public setter để gán prefab
                SetPlayerPrefab(_playerSpawnMgr, playerPrefab, playerSpawnPosition);
            }
        }

        /// <summary>
        /// Gán player prefab cho PlayerSpawnManager qua reflection
        /// (vì field là private serialized)
        /// </summary>
        private void SetPlayerPrefab(PlayerSpawnManager mgr, GameObject prefab, Vector2 pos)
        {
            var type = typeof(PlayerSpawnManager);

            var prefabField = type.GetField("playerPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prefabField != null)
            {
                prefabField.SetValue(mgr, prefab);
            }

            var posField = type.GetField("spawnPosition",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (posField != null)
            {
                posField.SetValue(mgr, pos);
            }
        }

        // =============================================
        //  ENEMY SETUP
        // =============================================

        private void SetupEnemySpawnManager()
        {
            _enemySpawnMgr = FindFirstObjectByType<EnemySpawnManager>();
            if (_enemySpawnMgr == null)
            {
                var obj = new GameObject("EnemySpawnManager");
                _enemySpawnMgr = obj.AddComponent<EnemySpawnManager>();
                Debug.Log("[LevelManager] Auto-created EnemySpawnManager");
            }

            // Inject enemy prefabs & spawn groups
            ConfigureEnemySpawnManager();
        }

        private void ConfigureEnemySpawnManager()
        {
            var type = typeof(EnemySpawnManager);

            // === Cấu hình Enemy Prefabs ===
            var prefabs = new System.Collections.Generic.List<EnemyPrefabEntry>();

            if (marshmallowPrefab != null)
                prefabs.Add(new EnemyPrefabEntry { typeId = "marshmallow", prefab = marshmallowPrefab });
            if (zombiePrefab != null)
                prefabs.Add(new EnemyPrefabEntry { typeId = "zombie", prefab = zombiePrefab });
            if (ratMiniBossPrefab != null)
                prefabs.Add(new EnemyPrefabEntry { typeId = "rat_boss", prefab = ratMiniBossPrefab });
            if (dagePrefab != null)
                prefabs.Add(new EnemyPrefabEntry { typeId = "dage", prefab = dagePrefab });

            // Inject enemyPrefabs
            var prefabsField = type.GetField("enemyPrefabs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prefabsField != null)
            {
                prefabsField.SetValue(_enemySpawnMgr, prefabs.ToArray());
            }

            // === Cấu hình Spawn Groups ===
            var groups = new System.Collections.Generic.List<SpawnGroup>();

            // --- Phòng Start ---
            if (enableStartRoom && startRoomEnemyPositions.Length > 0)
            {
                groups.Add(CreateSpawnGroup(
                    "Room_Start",
                    startRoomEnemyPositions,
                    "marshmallow",
                    new Color(1f, 0.3f, 0.3f, 0.8f), // Đỏ
                    spawnOnStart: !useRoomTriggers
                ));
            }

            // --- Hành Lang Vàng ---
            if (enableHallway && hallwayEnemyPositions.Length > 0)
            {
                groups.Add(CreateSpawnGroup(
                    "Hallway_Yellow",
                    hallwayEnemyPositions,
                    "zombie",
                    new Color(1f, 1f, 0.3f, 0.8f), // Vàng
                    spawnOnStart: !useRoomTriggers
                ));
            }

            // --- Phòng Mini-Boss ---
            if (enableBossRoom)
            {
                var bossPoints = new SpawnPointConfig[]
                {
                    // Rat Mini Boss ở giữa phòng
                    new SpawnPointConfig
                    {
                        enemyTypeId = "rat_boss",
                        position = bossRoomCenter,
                        spawnChance = 1f,
                        randomOffset = 0f
                    }
                };

                // Guard enemies xung quanh boss
                var guardPoints = new System.Collections.Generic.List<SpawnPointConfig>(bossPoints);
                foreach (var gPos in bossRoomGuardPositions)
                {
                    guardPoints.Add(new SpawnPointConfig
                    {
                        enemyTypeId = "marshmallow",
                        position = gPos,
                        spawnChance = 0.8f,
                        randomOffset = 0.5f
                    });
                }

                groups.Add(new SpawnGroup
                {
                    groupName = "Boss_Room",
                    spawnPoints = guardPoints.ToArray(),
                    gizmoColor = new Color(0.3f, 0.3f, 1f, 0.8f), // Xanh dương
                    spawnOnStart = !useRoomTriggers,
                    initialDelay = 0.5f,
                    respawnWhenAllDead = false
                });
            }

            // --- Khu Vực Cam ---
            if (enableOrangeZone && orangeZoneEnemyPositions.Length > 0)
            {
                groups.Add(CreateSpawnGroup(
                    "Zone_Orange",
                    orangeZoneEnemyPositions,
                    "zombie",
                    new Color(1f, 0.6f, 0f, 0.8f), // Cam
                    spawnOnStart: !useRoomTriggers,
                    respawn: true,
                    respawnDelay: 15f
                ));
            }

            // --- Phòng Goal ---
            if (enableGoalRoom && goalRoomEnemyPositions.Length > 0)
            {
                groups.Add(CreateSpawnGroup(
                    "Room_Goal",
                    goalRoomEnemyPositions,
                    "marshmallow",
                    new Color(0.8f, 0.3f, 1f, 0.8f), // Tím
                    spawnOnStart: !useRoomTriggers
                ));
            }

            // Inject spawnGroups
            var groupsField = type.GetField("spawnGroups",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (groupsField != null)
            {
                groupsField.SetValue(_enemySpawnMgr, groups.ToArray());
            }

            Debug.Log($"[LevelManager] Configured {groups.Count} spawn groups with {prefabs.Count} enemy types");
        }

        // =============================================
        //  HELPER
        // =============================================

        private SpawnGroup CreateSpawnGroup(string name, Vector2[] positions, string enemyType,
            Color color, bool spawnOnStart = true, bool respawn = false, float respawnDelay = 10f)
        {
            var points = new SpawnPointConfig[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                points[i] = new SpawnPointConfig
                {
                    enemyTypeId = enemyType,
                    position = positions[i],
                    spawnChance = 1f,
                    randomOffset = 0.5f
                };
            }

            return new SpawnGroup
            {
                groupName = name,
                spawnPoints = points,
                gizmoColor = color,
                spawnOnStart = spawnOnStart,
                respawnWhenAllDead = respawn,
                respawnDelay = respawnDelay
            };
        }

        // =============================================
        //  ROOM TRIGGER API (cho RoomTriggerZone)
        // =============================================

        /// <summary>
        /// Gọi khi player bước vào phòng - kích hoạt spawn group tương ứng
        /// </summary>
        public void OnPlayerEnterRoom(string roomName)
        {
            Debug.Log($"[LevelManager] Player entered {roomName}");
            if (_enemySpawnMgr != null)
            {
                _enemySpawnMgr.ActivateGroup(roomName);
            }
        }

        // =============================================
        //  GIZMOS - Vẽ layout tổng thể
        // =============================================

        private void OnDrawGizmos()
        {
            // Player Spawn
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector3(playerSpawnPosition.x, playerSpawnPosition.y, 0f), 0.6f);
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(
                new Vector3(playerSpawnPosition.x, playerSpawnPosition.y + 1f, 0f),
                "★ PLAYER SPAWN");
#endif

            // Phòng Start
            if (enableStartRoom)
            {
                DrawRoomGizmo("PHÒNG START", new Vector2(-5f, 3f), new Vector2(10f, 8f),
                    new Color(1f, 0.3f, 0.3f, 0.2f), startRoomEnemyPositions);
            }

            // Hành Lang
            if (enableHallway)
            {
                DrawRoomGizmo("HÀNH LANG VÀNG", new Vector2(15f, 3f), new Vector2(30f, 6f),
                    new Color(1f, 1f, 0.3f, 0.2f), hallwayEnemyPositions);
            }

            // Phòng Boss
            if (enableBossRoom)
            {
                DrawRoomGizmo("PHÒNG BOSS", bossRoomCenter, new Vector2(12f, 12f),
                    new Color(0.3f, 0.3f, 1f, 0.2f), bossRoomGuardPositions);

                // Boss marker
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(new Vector3(bossRoomCenter.x, bossRoomCenter.y, 0f), 0.8f);
#if UNITY_EDITOR
                UnityEditor.Handles.color = Color.red;
                UnityEditor.Handles.Label(
                    new Vector3(bossRoomCenter.x, bossRoomCenter.y + 1f, 0f),
                    "🐀 RAT BOSS");
#endif
            }

            // Khu Vực Cam
            if (enableOrangeZone)
            {
                DrawRoomGizmo("KHU VỰC CAM", new Vector2(50f, 3f), new Vector2(20f, 8f),
                    new Color(1f, 0.6f, 0f, 0.2f), orangeZoneEnemyPositions);
            }

            // Phòng Goal
            if (enableGoalRoom)
            {
                DrawRoomGizmo("PHÒNG GOAL", new Vector2(65f, 3f), new Vector2(10f, 10f),
                    new Color(0.8f, 0.3f, 1f, 0.2f), goalRoomEnemyPositions);
            }
        }

        private void DrawRoomGizmo(string label, Vector2 center, Vector2 size, Color color, Vector2[] enemyPositions)
        {
            Gizmos.color = color;
            Gizmos.DrawCube(new Vector3(center.x, center.y, 0f), new Vector3(size.x, size.y, 0f));

            Gizmos.color = new Color(color.r, color.g, color.b, 0.8f);
            Gizmos.DrawWireCube(new Vector3(center.x, center.y, 0f), new Vector3(size.x, size.y, 0f));

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(
                new Vector3(center.x - size.x / 4f, center.y + size.y / 2f + 0.5f, 0f),
                label);
#endif

            // Vẽ enemy positions
            if (enemyPositions != null)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
                foreach (var pos in enemyPositions)
                {
                    Gizmos.DrawWireCube(new Vector3(pos.x, pos.y, 0f), Vector3.one * 0.5f);
                }
            }
        }
    }
}
