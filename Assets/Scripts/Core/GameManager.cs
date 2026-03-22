using UnityEngine;
using UnityEngine.SceneManagement;

namespace NWO
{
    /// <summary>
    /// Manages game state, game over, and restart functionality
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private bool isGameOver;
        [SerializeField] private bool isPaused;
        
        [Header("Light Fragments")]
        [SerializeField] private int lightFragmentsCollected = 0;
        [SerializeField] private int totalLightFragments = 3; // Tổng số mảnh cần thu thập
        
        [Header("References")]
        [SerializeField] private GameObject gameOverUI; // GameOverPanel hoặc GameOverCanvas

        [Header("Audio")]
        [SerializeField] private AudioClip gameplayMusic;
        [SerializeField, Range(0f, 1f)] private float gameplayMusicVolume = 0.5f;

        private AudioSource gameplayMusicSource;

        public bool IsGameOver => isGameOver;
        public bool IsPaused => isPaused;
        public int LightFragmentsCollected => lightFragmentsCollected;
        public int TotalLightFragments => totalLightFragments;
        
        // === EVENTS ===
        /// <summary>Event khi nhặt Light Fragment (current, total)</summary>
        public event System.Action<int, int> OnLightFragmentCollected;
        /// <summary>Event khi nhặt đủ tất cả Light Fragments</summary>
        public event System.Action OnAllLightFragmentsCollected;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            if (gameOverUI != null)
            {
                var gameOverUiComp = gameOverUI.GetComponent<GameOverUI>();
                if (gameOverUiComp != null)
                    gameOverUiComp.HideGameOverImmediate();
                else
                    gameOverUI.SetActive(false);
            }

            SetupGameplayMusic();
            
            // Auto-create LightFragmentUI nếu chưa có
            if (FindFirstObjectByType<LightFragmentUI>() == null)
            {
                var uiObj = new GameObject("LightFragmentUI");
                uiObj.AddComponent<LightFragmentUI>();
                Debug.Log("[GameManager] Auto-created LightFragmentUI");
            }
            
            // // Auto-create FlashOfTruthUI nếu chưa có
            // if (FindFirstObjectByType<FlashOfTruthUI>() == null)
            // {
            //     var flashUIObj = new GameObject("FlashOfTruthUI");
            //     flashUIObj.AddComponent<FlashOfTruthUI>();
            //     Debug.Log("[GameManager] Auto-created FlashOfTruthUI");
            // }
            
            // Auto-create DungeonLightingManager nếu chưa có
            if (FindFirstObjectByType<DungeonLightingManager>() == null)
            {
                var lightingObj = new GameObject("DungeonLightingManager");
                lightingObj.AddComponent<DungeonLightingManager>();
                Debug.Log("[GameManager] Auto-created DungeonLightingManager");
            }
            
            // Auto-create MinimapManager nếu chưa có
            if (FindFirstObjectByType<MinimapManager>() == null)
            {
                var minimapObj = new GameObject("MinimapManager");
                minimapObj.AddComponent<MinimapManager>();
                Debug.Log("[GameManager] Auto-created MinimapManager");
            }

            // Auto-create UpgradeManager nếu chưa có
            if (FindFirstObjectByType<UpgradeManager>() == null)
            {
                var upgradeObj = new GameObject("UpgradeManager");
                upgradeObj.AddComponent<UpgradeManager>();
                Debug.Log("[GameManager] Auto-created UpgradeManager");
            }

            // Auto-create UpgradeSelectionUI nếu chưa có
            if (FindFirstObjectByType<UpgradeSelectionUI>() == null)
            {
                var upgradeUIObj = new GameObject("UpgradeSelectionUI");
                upgradeUIObj.AddComponent<UpgradeSelectionUI>();
                Debug.Log("[GameManager] Auto-created UpgradeSelectionUI");
            }

            // Auto-create CoinManager nếu chưa có
            if (FindFirstObjectByType<CoinManager>() == null)
            {
                var coinObj = new GameObject("CoinManager");
                coinObj.AddComponent<CoinManager>();
                Debug.Log("[GameManager] Auto-created CoinManager");
            }

            // Auto-create HeartManager nếu chưa có
            if (FindFirstObjectByType<HeartManager>() == null)
            {
                var heartObj = new GameObject("HeartManager");
                heartObj.AddComponent<HeartManager>();
                Debug.Log("[GameManager] Auto-created HeartManager");
            }

            // Auto-create KeyBindManager nếu chưa có
            if (KeyBindManager.Instance == null && FindFirstObjectByType<KeyBindManager>() == null)
            {
                var kbObj = new GameObject("KeyBindManager");
                kbObj.AddComponent<KeyBindManager>();
            }

            // Auto-create CoinUI nếu chưa có
            if (FindFirstObjectByType<CoinUI>() == null)
            {
                var coinUIObj = new GameObject("CoinUI");
                coinUIObj.AddComponent<CoinUI>();
                Debug.Log("[GameManager] Auto-created CoinUI");
            }

            // Auto-create AnimationPreloader to pre-warm all animation clips
            if (FindFirstObjectByType<AnimationPreloader>() == null)
            {
                var preloaderObj = new GameObject("AnimationPreloader");
                preloaderObj.AddComponent<AnimationPreloader>();
            }
        }

        private void SetupGameplayMusic()
        {
            if (gameplayMusic == null)
                return;

            gameplayMusicSource = GetComponent<AudioSource>();
            if (gameplayMusicSource == null)
                gameplayMusicSource = gameObject.AddComponent<AudioSource>();

            gameplayMusicSource.clip = gameplayMusic;
            gameplayMusicSource.playOnAwake = false;
            gameplayMusicSource.loop = true;
            gameplayMusicSource.spatialBlend = 0f;
            gameplayMusicSource.volume = Mathf.Clamp01(gameplayMusicVolume);

            if (!gameplayMusicSource.isPlaying)
                gameplayMusicSource.Play();
        }


        /// Call this when player dies

        public void TriggerGameOver()
        {
            if (isGameOver) return; // Already game over

            isGameOver = true;
            isPaused = false;
            Time.timeScale = 0f; // Pause game

            if (gameplayMusicSource != null && gameplayMusicSource.isPlaying)
                gameplayMusicSource.Stop();

            if (gameOverUI != null)
            {
                // Nếu gameOverUI là Panel, cần tìm parent Canvas
                var canvas = gameOverUI.GetComponentInParent<Canvas>();
                if (canvas != null && !canvas.gameObject.activeSelf)
                {
                    // Active Canvas trước
                    canvas.gameObject.SetActive(true);
                    Debug.Log("[GameManager] Activated GameOverCanvas");
                }

                // Prefer component flow so GameOverUI can run transitions/conditions
                var gameOverUiComp = gameOverUI.GetComponent<GameOverUI>();
                if (gameOverUiComp != null)
                    gameOverUiComp.ShowGameOver();
                else
                    gameOverUI.SetActive(true);

                Debug.Log("[GameManager] Game Over UI Activated!");
            }
            else
            {
                Debug.LogError("[GameManager] Game Over UI reference is NULL!");
            }

            Debug.Log("[GameManager] Game Over!");
        }

        /// <summary>
        /// Gọi khi người chơi thu thập Light Fragment
        /// </summary>
        public void CollectLightFragment(int fragmentID)
        {
            lightFragmentsCollected++;
            Debug.Log($"[GameManager] Light Fragment #{fragmentID} collected! Total: {lightFragmentsCollected}/{totalLightFragments}");
            
            // Fire event
            OnLightFragmentCollected?.Invoke(lightFragmentsCollected, totalLightFragments);
            
            // Kiểm tra đã thu thập đủ chưa
            if (lightFragmentsCollected >= totalLightFragments)
            {
                OnAllFragmentsCollected();
            }
        }
        
        /// <summary>
        /// Được gọi khi thu thập đủ tất cả Light Fragments
        /// </summary>
        private void OnAllFragmentsCollected()
        {
            Debug.Log("[GameManager] All Light Fragments collected! Unlocking Flash of Truth skill...");
            
            // Fire event
            OnAllLightFragmentsCollected?.Invoke();
        }


        /// Restart current scene

        public void RestartGame()
        {
            Time.timeScale = 1f; // Resume time
            isGameOver = false;
            
            // Hide game over UI first
            if (gameOverUI != null)
            {
                var gameOverUiComp = gameOverUI.GetComponent<GameOverUI>();
                if (gameOverUiComp != null)
                    gameOverUiComp.HideGameOverImmediate();
                else
                    gameOverUI.SetActive(false);
            }
            
            // Reset player health
            var playerHealth = FindFirstObjectByType<PlayerHealth2D>();
            if (playerHealth != null)
            {
                playerHealth.ResetHealth();
            }

            // Reset upgrades
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.ResetUpgrades();
            }

            // Reset coins
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.ResetCoins();
            }

            // Allow animation preloading on next scene
            AnimationPreloader.ResetPreloadFlag();
            
            // Try to regenerate map with new layout instead of reloading scene
            // Support both new and legacy dungeon systems
            var mapManager = FindFirstObjectByType<NWO.Dungeon.MapInitializationManager>();
            if (mapManager != null)
            {
                // New system
                mapManager.RegenerateWithNewSeed();
                Debug.Log("[GameManager] Map regenerated with new layout (new system)!");
                return;
            }
            
            // Try legacy system
            #pragma warning disable CS0618 // Type is obsolete but needed for backward compatibility
            var legacyBuilder = FindFirstObjectByType<NWO.Dungeon.UnityDungeonTilemapBuilder>();
            #pragma warning restore CS0618
            if (legacyBuilder != null)
            {
                // Legacy system
                legacyBuilder.RegenerateWithNewSeed();
                Debug.Log("[GameManager] Map regenerated with new layout (legacy system)!");
                return;
            }
            
            // Fallback: reload scene if no dungeon manager found
            Debug.LogWarning("[GameManager] No dungeon manager found, reloading scene...");
            SceneLoader.LoadScene(SceneManager.GetActiveScene().name);
        }


        /// Load main menu (if you have one)

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            isGameOver = false;
            SceneLoader.LoadScene("MainMenu"); // Change to your menu scene name
        }


        /// Quit game

        public void QuitGame()
        {
            Debug.Log("[GameManager] Quitting game...");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        /// <summary>
        /// Apply save data vào game (gọi sau khi scene load xong)
        /// </summary>
        public void ApplySaveData(SaveData data)
        {
            if (data == null) return;

            // Restore light fragments
            lightFragmentsCollected = data.lightFragmentsCollected;

            // Restore player position
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (data.hasCheckpoint)
                {
                    player.transform.position = new Vector3(data.checkpointPositionX, data.checkpointPositionY, 0f);
                }
                else
                {
                    player.transform.position = new Vector3(data.playerPositionX, data.playerPositionY, 0f);
                }

                // Restore health
                var health = player.GetComponent<PlayerHealth2D>();
                if (health != null)
                {
                    health.SetHealth(data.playerCurrentHealth);
                }

                // Restore stamina
                var stamina = player.GetComponent<PlayerStamina>();
                if (stamina != null)
                {
                    stamina.SetStamina(data.playerCurrentStamina);
                }
            }

            Debug.Log($"[GameManager] Save data applied. Fragments: {lightFragmentsCollected}, Scene: {data.sceneName}");
        }

        /// <summary>
        /// Đặt Light Fragments trực tiếp (dùng khi load save)
        /// </summary>
        public void SetLightFragments(int count)
        {
            lightFragmentsCollected = count;
            OnLightFragmentCollected?.Invoke(lightFragmentsCollected, totalLightFragments);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
