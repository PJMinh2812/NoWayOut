using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NWO
{
    /// <summary>
    /// Quản lý spawn quái vật tại nhiều vị trí trên map.
    /// Hỗ trợ:
    /// - Spawn theo wave (đợt)
    /// - Spawn theo vùng (khi player vào phòng)
    /// - Spawn ngẫu nhiên hoặc cố định
    /// - Respawn sau khi quái chết
    ///
    /// SETUP trong Unity Editor:
    /// 1. Tạo Empty GameObject "EnemySpawnManager"
    /// 2. Attach script này
    /// 3. Cấu hình spawnGroups trong Inspector
    /// </summary>
    public class EnemySpawnManager : MonoBehaviour
    {
        public static EnemySpawnManager Instance { get; private set; }

        [Header("Enemy Prefabs")]
        [Tooltip("Danh sách enemy prefab có thể spawn")]
        [SerializeField] private EnemyPrefabEntry[] enemyPrefabs;

        [Header("Spawn Groups")]
        [Tooltip("Nhóm spawn - mỗi nhóm là một khu vực trên map")]
        [SerializeField] private SpawnGroup[] spawnGroups;

        [Header("Global Settings")]
        [Tooltip("Delay giữa mỗi lần spawn enemy trong cùng group (giây)")]
        [SerializeField] private float spawnInterval = 0.3f;

        [Tooltip("Tổng số enemy tối đa cùng tồn tại")]
        [SerializeField] private int maxEnemiesAlive = 20;

        [Tooltip("Spawn tất cả khi scene load")]
        [SerializeField] private bool spawnOnStart = true;

        // Tracking
        private List<GameObject> _aliveEnemies = new();
        private Dictionary<string, bool> _activatedGroups = new();

        /// <summary>Tổng số enemy đang sống</summary>
        public int AliveEnemyCount => _aliveEnemies.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnAllGroups();
            }
        }

        private void Update()
        {
            // Cleanup destroyed enemies
            _aliveEnemies.RemoveAll(e => e == null);
        }

        // =============================================
        //  PUBLIC API
        // =============================================

        /// <summary>
        /// Spawn tất cả các group được đánh dấu spawnOnStart
        /// </summary>
        public void SpawnAllGroups()
        {
            if (spawnGroups == null) return;

            for (int i = 0; i < spawnGroups.Length; i++)
            {
                var group = spawnGroups[i];
                if (group.spawnOnStart)
                {
                    StartCoroutine(SpawnGroupCoroutine(group));
                }
            }
        }

        /// <summary>
        /// Spawn một group cụ thể theo tên
        /// </summary>
        public void ActivateGroup(string groupName)
        {
            if (_activatedGroups.ContainsKey(groupName) && _activatedGroups[groupName])
            {
                Debug.Log($"[EnemySpawnManager] Group '{groupName}' already activated");
                return;
            }

            foreach (var group in spawnGroups)
            {
                if (group.groupName == groupName)
                {
                    _activatedGroups[groupName] = true;
                    StartCoroutine(SpawnGroupCoroutine(group));
                    return;
                }
            }

            Debug.LogWarning($"[EnemySpawnManager] Group '{groupName}' not found!");
        }

        /// <summary>
        /// Despawn tất cả enemies
        /// </summary>
        public void DespawnAll()
        {
            foreach (var enemy in _aliveEnemies)
            {
                if (enemy != null) Destroy(enemy);
            }
            _aliveEnemies.Clear();
            _activatedGroups.Clear();
            Debug.Log("[EnemySpawnManager] All enemies despawned");
        }

        // =============================================
        //  SPAWN LOGIC
        // =============================================

        private IEnumerator SpawnGroupCoroutine(SpawnGroup group)
        {
            if (group.spawnPoints == null || group.spawnPoints.Length == 0)
            {
                Debug.LogWarning($"[EnemySpawnManager] Group '{group.groupName}' has no spawn points!");
                yield break;
            }

            Debug.Log($"[EnemySpawnManager] Spawning group '{group.groupName}' ({group.spawnPoints.Length} points)");

            // Delay trước khi bắt đầu spawn (nếu có)
            if (group.initialDelay > 0f)
            {
                yield return new WaitForSeconds(group.initialDelay);
            }

            foreach (var point in group.spawnPoints)
            {
                if (_aliveEnemies.Count >= maxEnemiesAlive)
                {
                    Debug.LogWarning("[EnemySpawnManager] Max enemies reached, stopping spawn");
                    yield break;
                }

                SpawnEnemyAtPoint(point, group);

                if (spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
            }

            // Wave respawn
            if (group.respawnWhenAllDead)
            {
                StartCoroutine(WatchGroupForRespawn(group));
            }
        }

        private void SpawnEnemyAtPoint(SpawnPointConfig point, SpawnGroup group)
        {
            // Lấy prefab
            GameObject prefab = GetEnemyPrefab(point.enemyTypeId);
            if (prefab == null)
            {
                Debug.LogWarning($"[EnemySpawnManager] Enemy prefab '{point.enemyTypeId}' not found!");
                return;
            }

            // Xác suất spawn
            if (point.spawnChance < 1f && Random.value > point.spawnChance)
            {
                return;
            }

            // Vị trí spawn
            Vector3 spawnPos;
            if (point.spawnTransform != null)
            {
                spawnPos = point.spawnTransform.position;
            }
            else
            {
                spawnPos = new Vector3(point.position.x, point.position.y, 0f);

                // Offset ngẫu nhiên
                if (point.randomOffset > 0f)
                {
                    spawnPos += (Vector3)(Random.insideUnitCircle * point.randomOffset);
                }
            }

            // Spawn
            var enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            enemy.name = $"{prefab.name}_{group.groupName}_{_aliveEnemies.Count}";

            // Parent vào group parent nếu có
            if (group.parentContainer != null)
            {
                enemy.transform.SetParent(group.parentContainer);
            }

            _aliveEnemies.Add(enemy);

            Debug.Log($"[EnemySpawnManager] Spawned {enemy.name} at {spawnPos}");
        }

        private GameObject GetEnemyPrefab(string typeId)
        {
            if (enemyPrefabs == null) return null;

            foreach (var entry in enemyPrefabs)
            {
                if (entry.typeId == typeId)
                {
                    return entry.prefab;
                }
            }

            // Fallback: trả về prefab đầu tiên
            if (enemyPrefabs.Length > 0)
            {
                return enemyPrefabs[0].prefab;
            }

            return null;
        }

        private IEnumerator WatchGroupForRespawn(SpawnGroup group)
        {
            yield return new WaitForSeconds(5f); // Chờ enemy đã spawn xong

            while (true)
            {
                yield return new WaitForSeconds(2f);

                // Kiểm tra nếu tất cả enemy trong group đã chết
                bool allDead = true;
                foreach (var enemy in _aliveEnemies)
                {
                    if (enemy != null && enemy.name.Contains(group.groupName))
                    {
                        allDead = false;
                        break;
                    }
                }

                if (allDead)
                {
                    Debug.Log($"[EnemySpawnManager] All enemies in '{group.groupName}' dead, respawning after {group.respawnDelay}s");
                    yield return new WaitForSeconds(group.respawnDelay);
                    StartCoroutine(SpawnGroupCoroutine(group));
                    yield break;
                }
            }
        }

        // =============================================
        //  GIZMOS
        // =============================================

        private void OnDrawGizmos()
        {
            if (spawnGroups == null) return;

            foreach (var group in spawnGroups)
            {
                if (group.spawnPoints == null) continue;

                Gizmos.color = group.gizmoColor;

                foreach (var point in group.spawnPoints)
                {
                    Vector3 pos;
                    if (point.spawnTransform != null)
                        pos = point.spawnTransform.position;
                    else
                        pos = new Vector3(point.position.x, point.position.y, 0f);

                    // Vẽ vị trí spawn
                    Gizmos.DrawWireCube(pos, Vector3.one * 0.6f);

                    // Vẽ radius ngẫu nhiên
                    if (point.randomOffset > 0f)
                    {
                        Gizmos.color = new Color(group.gizmoColor.r, group.gizmoColor.g, group.gizmoColor.b, 0.3f);
                        Gizmos.DrawWireSphere(pos, point.randomOffset);
                        Gizmos.color = group.gizmoColor;
                    }

#if UNITY_EDITOR
                    UnityEditor.Handles.color = group.gizmoColor;
                    UnityEditor.Handles.Label(pos + Vector3.up * 0.6f,
                        $"{group.groupName}\n{point.enemyTypeId}");
#endif
                }
            }
        }
    }

    // =============================================
    //  DATA CLASSES
    // =============================================

    /// <summary>
    /// Entry cho mỗi loại enemy prefab
    /// </summary>
    [System.Serializable]
    public class EnemyPrefabEntry
    {
        [Tooltip("ID định danh (ví dụ: 'zombie', 'marshmallow', 'rat_boss')")]
        public string typeId = "default";

        [Tooltip("Prefab của enemy")]
        public GameObject prefab;
    }

    /// <summary>
    /// Một nhóm spawn points (thường là 1 phòng/khu vực)
    /// </summary>
    [System.Serializable]
    public class SpawnGroup
    {
        [Header("Group Info")]
        [Tooltip("Tên nhóm (ví dụ: 'Room_Start', 'Hallway', 'Boss_Room')")]
        public string groupName = "Default";

        [Tooltip("Màu gizmo trong Scene view")]
        public Color gizmoColor = new Color(1f, 0.3f, 0f, 0.8f);

        [Header("Spawn Points")]
        [Tooltip("Danh sách các điểm spawn trong nhóm này")]
        public SpawnPointConfig[] spawnPoints;

        [Header("Spawn Behavior")]
        [Tooltip("Spawn khi scene load")]
        public bool spawnOnStart = true;

        [Tooltip("Delay trước khi bắt đầu spawn (giây)")]
        public float initialDelay = 0f;

        [Tooltip("Respawn tất cả khi cả group chết")]
        public bool respawnWhenAllDead = false;

        [Tooltip("Delay trước khi respawn group (giây)")]
        public float respawnDelay = 10f;

        [Header("Container")]
        [Tooltip("Parent transform chứa enemy đã spawn (để gọn hierarchy)")]
        public Transform parentContainer;
    }

    /// <summary>
    /// Cấu hình cho một điểm spawn cụ thể
    /// </summary>
    [System.Serializable]
    public class SpawnPointConfig
    {
        [Header("Enemy")]
        [Tooltip("ID loại enemy (khớp với EnemyPrefabEntry.typeId)")]
        public string enemyTypeId = "default";

        [Header("Position")]
        [Tooltip("Transform đánh dấu vị trí (ưu tiên hơn position)")]
        public Transform spawnTransform;

        [Tooltip("Vị trí spawn nếu không dùng Transform")]
        public Vector2 position;

        [Tooltip("Offset ngẫu nhiên xung quanh vị trí (0 = chính xác)")]
        [Range(0f, 5f)]
        public float randomOffset = 0f;

        [Header("Spawn Chance")]
        [Tooltip("Xác suất spawn tại điểm này (0-1)")]
        [Range(0f, 1f)]
        public float spawnChance = 1f;
    }
}
