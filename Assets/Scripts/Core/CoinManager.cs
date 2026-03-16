using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Singleton quản lý hệ thống coin.
    /// Enemy drop coin vật lý khi chết, coin dùng để reroll upgrade cards.
    /// </summary>
    public sealed class CoinManager : MonoBehaviour
    {
        public static CoinManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Chi phí coin để reroll bộ thẻ nâng cấp")]
        [SerializeField] private int rerollCost = 3;

        [Header("Drop Settings")]
        [Tooltip("Số coin drop khi giết enemy thường")]
        [SerializeField] private int normalEnemyMinDrop = 1;
        [SerializeField] private int normalEnemyMaxDrop = 2;
        [Tooltip("Số coin drop khi giết boss")]
        [SerializeField] private int bossMinDrop = 3;
        [SerializeField] private int bossMaxDrop = 6;

        [Header("Coin Sprites (Optional - assign in Inspector)")]
        [Tooltip("4 frames: coin_anim_f0..f3 từ Assets/Art/Boss/")]
        [SerializeField] private Sprite[] coinAnimFrames;

        [Header("Coin Sound (Optional)")]
        [Tooltip("coin.wav từ Assets/Audio/SFX/sounds/")]
        [SerializeField] private AudioClip coinPickupSound;

        /// <summary>Số coin hiện tại</summary>
        public int CurrentCoins { get; private set; }

        /// <summary>Chi phí reroll</summary>
        public int RerollCost => rerollCost;

        /// <summary>Event khi coin thay đổi (currentCoins)</summary>
        public event System.Action<int> OnCoinsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Pass sprites and sound to CoinPickup pool
            if (coinAnimFrames != null && coinAnimFrames.Length > 0)
                CoinPickup.SetCoinFrames(coinAnimFrames);

            if (coinPickupSound != null)
                CoinPickup.SetCoinSound(coinPickupSound);

            // Pre-warm pool
            CoinPickup.WarmPool();
        }

        /// <summary>Thêm coin (khi player nhặt coin pickup)</summary>
        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            CurrentCoins += amount;
            OnCoinsChanged?.Invoke(CurrentCoins);
        }

        /// <summary>Spawn coin vật lý từ enemy thường chết</summary>
        public void SpawnCoinsFromEnemy(Vector3 position)
        {
            int count = Random.Range(normalEnemyMinDrop, normalEnemyMaxDrop + 1);
            CoinPickup.SpawnMultiple(position, count);
        }

        /// <summary>Spawn coin vật lý từ boss chết</summary>
        public void SpawnCoinsFromBoss(Vector3 position)
        {
            int count = Random.Range(bossMinDrop, bossMaxDrop + 1);
            CoinPickup.SpawnMultiple(position, count);
        }

        /// <summary>Kiểm tra có đủ coin để reroll không</summary>
        public bool CanReroll()
        {
            return CurrentCoins >= rerollCost;
        }

        /// <summary>Tiêu coin để reroll. Trả về true nếu thành công.</summary>
        public bool TryReroll()
        {
            if (!CanReroll())
            {
                Debug.Log($"[CoinManager] Không đủ coin để reroll! ({CurrentCoins}/{rerollCost})");
                return false;
            }

            CurrentCoins -= rerollCost;
            OnCoinsChanged?.Invoke(CurrentCoins);
            Debug.Log($"[CoinManager] Reroll! -{rerollCost} coins. Remaining: {CurrentCoins}");
            return true;
        }

        /// <summary>Reset coin về 0 (restart game)</summary>
        public void ResetCoins()
        {
            CurrentCoins = 0;
            OnCoinsChanged?.Invoke(CurrentCoins);
        }

        /// <summary>Set coin trực tiếp (load save)</summary>
        public void SetCoins(int amount)
        {
            CurrentCoins = Mathf.Max(0, amount);
            OnCoinsChanged?.Invoke(CurrentCoins);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
