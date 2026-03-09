using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NWO
{
    /// <summary>
    /// Tự động spawn 4-5 con enemy (Marshmallow + Zombie) tại các gate/cửa khi player tiếp cận.
    /// Gắn script này vào GameObject đại diện cho gate.
    /// </summary>
    public class GateEnemySpawner : MonoBehaviour
    {
        [Header("Enemy Prefabs")]
        [Tooltip("Prefab con Marshmallow")]
        [SerializeField] private GameObject marshmallowPrefab;

        [Tooltip("Prefab con Zombie")]
        [SerializeField] private GameObject zombiePrefab;

        [Header("Spawn Settings")]
        [Tooltip("Số lượng enemy tối thiểu mỗi lần spawn")]
        [SerializeField, Range(1, 10)] private int minSpawnCount = 4;

        [Tooltip("Số lượng enemy tối đa mỗi lần spawn")]
        [SerializeField, Range(1, 10)] private int maxSpawnCount = 5;

        [Tooltip("Bán kính spawn quanh gate")]
        [SerializeField] private float spawnRadius = 2f;

        [Tooltip("Khoảng cách để trigger spawn khi player đi vào vùng này")]
        [SerializeField] private float triggerRadius = 5f;

        [Tooltip("Độ trễ giữa mỗi enemy khi spawn (giây)")]
        [SerializeField] private float spawnDelay = 0.2f;

        [Tooltip("Chỉ spawn một lần duy nhất")]
        [SerializeField] private bool spawnOnce = true;

        [Header("Gate Lock (Tuỳ chọn)")]
        [Tooltip("Khi còn enemy sống, gate này sẽ bị khoá lại")]
        [SerializeField] private ProceduralGeneration.Components.DoorController linkedDoor;

        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;

        // ----- State -----
        private bool _hasSpawned = false;
        private bool _playerInRange = false;
        private readonly List<GameObject> _spawnedEnemies = new();

        private PlayerController2D _player;

        // -------------------------------------------------------

        private void Start()
        {
            _player = FindFirstObjectByType<PlayerController2D>();
        }

        private void Update()
        {
            if (_player == null) return;

            float dist = Vector2.Distance(transform.position, _player.transform.position);
            bool inRange = dist <= triggerRadius;

            if (inRange && !_playerInRange)
            {
                _playerInRange = true;
                TrySpawn();
            }
            else if (!inRange && _playerInRange)
            {
                _playerInRange = false;
            }

            // Cập nhật trạng thái cửa dựa trên enemy còn sống
            if (linkedDoor != null)
            {
                bool anyAlive = HasLivingEnemies();
                if (anyAlive && linkedDoor.isOpen)
                    linkedDoor.CloseDoor();
                else if (!anyAlive && !linkedDoor.isOpen && _hasSpawned)
                    linkedDoor.OpenDoor();
            }
        }

        // -------------------------------------------------------

        private void TrySpawn()
        {
            if (spawnOnce && _hasSpawned) return;
            if (marshmallowPrefab == null && zombiePrefab == null)
            {
                Debug.LogWarning($"[GateEnemySpawner] '{name}': Chưa gán prefab Marshmallow hoặc Zombie!");
                return;
            }

            _hasSpawned = true;
            StartCoroutine(SpawnEnemies());
        }

        private IEnumerator SpawnEnemies()
        {
            // Xây danh sách prefab có sẵn để random
            var availablePrefabs = new List<GameObject>();
            if (marshmallowPrefab != null) availablePrefabs.Add(marshmallowPrefab);
            if (zombiePrefab != null) availablePrefabs.Add(zombiePrefab);

            int count = Random.Range(minSpawnCount, maxSpawnCount + 1);

            for (int i = 0; i < count; i++)
            {
                GameObject prefab = availablePrefabs[Random.Range(0, availablePrefabs.Count)];
               Vector2 offset = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPos = new Vector3(transform.position.x + offset.x, transform.position.y + offset.y, -0.01f);


                GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
                _spawnedEnemies.Add(enemy);

                yield return new WaitForSeconds(spawnDelay);
            }
        }

        // -------------------------------------------------------

        private bool HasLivingEnemies()
        {
            _spawnedEnemies.RemoveAll(e => e == null); // Dọn null (enemy đã chết/bị huỷ)
            return _spawnedEnemies.Count > 0;
        }

        // -------------------------------------------------------

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            // Vòng trigger
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, triggerRadius);

            // Vòng spawn
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, spawnRadius);

            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (triggerRadius + 0.3f),
                $"Gate Spawner\n{minSpawnCount}-{maxSpawnCount} enemies");
        }
        #endif
    }
}
