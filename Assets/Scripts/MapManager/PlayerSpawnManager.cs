using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Quản lý spawn và respawn Player trong scene.
    /// - Spawn Player prefab tại vị trí chỉ định khi scene load
    /// - Respawn player khi chết (về checkpoint hoặc spawn point)
    /// 
    /// SETUP trong Unity Editor:
    /// 1. Tạo Empty GameObject "PlayerSpawnManager"
    /// 2. Attach script này
    /// 3. Kéo Player.prefab vào playerPrefab
    /// 4. Đặt spawnPosition hoặc kéo spawnPoint Transform
    /// </summary>
    public class PlayerSpawnManager : MonoBehaviour
    {
        public static PlayerSpawnManager Instance { get; private set; }

        [Header("Player Prefab")]
        [Tooltip("Player prefab từ Assets/Data/Prefabs/Player.prefab")]
        [SerializeField] private GameObject playerPrefab;

        [Header("Spawn Settings")]
        [Tooltip("Vị trí spawn mặc định (dùng nếu spawnPoint == null)")]
        [SerializeField] private Vector2 spawnPosition = new Vector2(-4f, 3f);

        [Tooltip("Transform đánh dấu vị trí spawn (ưu tiên hơn spawnPosition)")]
        [SerializeField] private Transform spawnPoint;

        [Header("Respawn Settings")]
        [Tooltip("Thời gian delay trước khi respawn (giây)")]
        [SerializeField] private float respawnDelay = 1.5f;

        [Tooltip("Checkpoint hiện tại (thay đổi khi player đi qua checkpoint)")]
        [SerializeField] private Transform currentCheckpoint;

        [Header("Auto-Find Settings")]
        [Tooltip("Tự động tìm Respawn_Point trong scene nếu spawnPoint chưa gán")]
        [SerializeField] private bool autoFindRespawnPoint = true;

        /// <summary>Reference tới Player đã spawn</summary>
        public GameObject SpawnedPlayer { get; private set; }

        /// <summary>Vị trí spawn hiện tại (checkpoint hoặc spawn point)</summary>
        public Vector2 CurrentSpawnPosition
        {
            get
            {
                if (currentCheckpoint != null) return currentCheckpoint.position;
                if (spawnPoint != null) return spawnPoint.position;
                return spawnPosition;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-find Respawn_Point nếu chưa gán
            if (autoFindRespawnPoint && spawnPoint == null)
            {
                var respawnObj = GameObject.Find("Respawn_Point");
                if (respawnObj != null)
                {
                    spawnPoint = respawnObj.transform;
                    Debug.Log($"[PlayerSpawnManager] Auto-found Respawn_Point at {spawnPoint.position}");
                }
            }
        }

        private void Start()
        {
            // Kiểm tra nếu Player đã có trong scene (đặt thủ công)
            var existingPlayer = FindFirstObjectByType<PlayerController2D>();
            if (existingPlayer != null)
            {
                SpawnedPlayer = existingPlayer.gameObject;
                Debug.Log($"[PlayerSpawnManager] Found existing Player at {SpawnedPlayer.transform.position}");
                return;
            }

            // Spawn Player
            SpawnPlayer();
        }

        /// <summary>
        /// Spawn Player prefab tại vị trí spawn
        /// </summary>
        public void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawnManager] playerPrefab chưa được gán! Kéo Player.prefab vào Inspector.");
                return;
            }

            Vector3 pos = new Vector3(CurrentSpawnPosition.x, CurrentSpawnPosition.y, 0f);
            SpawnedPlayer = Instantiate(playerPrefab, pos, Quaternion.identity);
            SpawnedPlayer.name = "Player";

            Debug.Log($"[PlayerSpawnManager] Player spawned at {pos}");

            // Đăng ký sự kiện chết để auto respawn
            var health = SpawnedPlayer.GetComponent<PlayerHealth2D>();
            if (health != null)
            {
                // Lắng nghe khi player chết (qua GameManager)
                // Respawn sẽ được gọi từ RespawnPlayer()
            }
        }

        /// <summary>
        /// Respawn player về checkpoint/spawn point hiện tại.
        /// Gọi từ GameManager.RestartGame() hoặc DeathZone.
        /// </summary>
        public void RespawnPlayer()
        {
            if (SpawnedPlayer == null)
            {
                // Player bị destroy, spawn lại
                SpawnPlayer();
                return;
            }

            Vector3 pos = new Vector3(CurrentSpawnPosition.x, CurrentSpawnPosition.y, 0f);
            SpawnedPlayer.transform.position = pos;

            // Reset health
            var health = SpawnedPlayer.GetComponent<PlayerHealth2D>();
            if (health != null)
            {
                health.ResetHealth();
            }

            // Reset velocity
            var rb = SpawnedPlayer.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            Debug.Log($"[PlayerSpawnManager] Player respawned at {pos}");
        }

        /// <summary>
        /// Cập nhật checkpoint mới (gọi khi player đi qua checkpoint)
        /// </summary>
        public void SetCheckpoint(Transform checkpoint)
        {
            currentCheckpoint = checkpoint;
            Debug.Log($"[PlayerSpawnManager] Checkpoint updated: {checkpoint.position}");
        }

        // === GIZMOS ===
        private void OnDrawGizmos()
        {
            // Vẽ vị trí spawn
            Vector3 drawPos;
            if (spawnPoint != null)
                drawPos = spawnPoint.position;
            else
                drawPos = new Vector3(spawnPosition.x, spawnPosition.y, 0f);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(drawPos, 0.5f);
            Gizmos.DrawIcon(drawPos, "d_PositionAsUV1@2x", true);

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(drawPos + Vector3.up * 0.8f, "PLAYER SPAWN");
#endif

            // Vẽ checkpoint nếu có
            if (currentCheckpoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(currentCheckpoint.position, 0.4f);
#if UNITY_EDITOR
                UnityEditor.Handles.color = Color.cyan;
                UnityEditor.Handles.Label(currentCheckpoint.position + Vector3.up * 0.8f, "CHECKPOINT");
#endif
            }
        }
    }
}
