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
        
        [Header("Light Fragments")]
        [SerializeField] private int lightFragmentsCollected = 0;
        [SerializeField] private int totalLightFragments = 3; // Tổng số mảnh cần thu thập
        
        [Header("References")]
        [SerializeField] private GameObject gameOverUI; // GameOverPanel hoặc GameOverCanvas

        public bool IsGameOver => isGameOver;
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
                gameOverUI.SetActive(false);
            }
            
            // Auto-create LightFragmentUI nếu chưa có
            if (FindFirstObjectByType<LightFragmentUI>() == null)
            {
                var uiObj = new GameObject("LightFragmentUI");
                uiObj.AddComponent<LightFragmentUI>();
                Debug.Log("[GameManager] Auto-created LightFragmentUI");
            }
            
            // Auto-create DungeonLightingManager nếu chưa có
            if (FindFirstObjectByType<DungeonLightingManager>() == null)
            {
                var lightingObj = new GameObject("DungeonLightingManager");
                lightingObj.AddComponent<DungeonLightingManager>();
                Debug.Log("[GameManager] Auto-created DungeonLightingManager");
            }
        }


        /// Call this when player dies

        public void TriggerGameOver()
        {
            if (isGameOver) return; // Already game over

            isGameOver = true;
            Time.timeScale = 0f; // Pause game

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
                
                // Active panel/UI
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
                gameOverUI.SetActive(false);
            }
            
            // Reset player health
            var playerHealth = FindFirstObjectByType<PlayerHealth2D>();
            if (playerHealth != null)
            {
                playerHealth.ResetHealth();
            }
            
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }


        /// Load main menu (if you have one)

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            isGameOver = false;
            SceneManager.LoadScene("MainMenu"); // Change to your menu scene name
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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
