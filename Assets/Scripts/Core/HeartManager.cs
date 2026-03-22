using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Singleton quản lý hệ thống drop heart hồi máu.
    /// Enemy/Boss chết có thể rơi heart vật lý để player nhặt.
    /// </summary>
    public sealed class HeartManager : MonoBehaviour
    {
        public static HeartManager Instance { get; private set; }

        [Header("Drop Chance")]
        [Tooltip("Tỉ lệ enemy thường rơi heart (0..1)")]
        [Range(0f, 1f)]
        [SerializeField] private float normalEnemyDropChance = 0.25f;

        [Tooltip("Tỉ lệ boss rơi heart (0..1)")]
        [Range(0f, 1f)]
        [SerializeField] private float bossDropChance = 1f;

        [Header("Drop Count")]
        [SerializeField] private int normalEnemyMinDrop = 1;
        [SerializeField] private int normalEnemyMaxDrop = 1;
        [SerializeField] private int bossMinDrop = 1;
        [SerializeField] private int bossMaxDrop = 2;

        [Header("Heal")]
        [Tooltip("Lượng máu hồi cho mỗi heart")]
        [SerializeField] private int healPerHeart = 12;

        [Header("Heart Sprites (Optional - assign in Inspector)")]
        [SerializeField] private Sprite[] heartAnimFrames;

        [Header("Heart Sound (Optional)")]
        [SerializeField] private AudioClip heartPickupSound;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (heartAnimFrames != null && heartAnimFrames.Length > 0)
                HeartPickup.SetHeartFrames(heartAnimFrames);

            if (heartPickupSound != null)
                HeartPickup.SetHeartSound(heartPickupSound);

            HeartPickup.WarmPool();
        }

        public void SpawnHeartsFromEnemy(Vector3 position)
        {
            TrySpawn(position, normalEnemyDropChance, normalEnemyMinDrop, normalEnemyMaxDrop);
        }

        public void SpawnHeartsFromBoss(Vector3 position)
        {
            TrySpawn(position, bossDropChance, bossMinDrop, bossMaxDrop);
        }

        private void TrySpawn(Vector3 position, float chance, int minDrop, int maxDrop)
        {
            if (Random.value > Mathf.Clamp01(chance)) return;

            int min = Mathf.Max(0, minDrop);
            int max = Mathf.Max(min, maxDrop);
            int count = Random.Range(min, max + 1);
            if (count <= 0) return;

            int healAmount = Mathf.Max(1, healPerHeart);
            HeartPickup.SpawnMultiple(position, count, healAmount);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
