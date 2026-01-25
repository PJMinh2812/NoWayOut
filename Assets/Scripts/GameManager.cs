using UnityEngine;
using UnityEngine.SceneManagement;

namespace GloomCraft
{
    /// <summary>
    /// Manages game state, game over, and restart functionality
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private bool isGameOver;
        
        [Header("References")]
        [SerializeField] private GameObject gameOverUI;

        public bool IsGameOver => isGameOver;

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
        }


        /// Call this when player dies

        public void TriggerGameOver()
        {
            if (isGameOver) return; // Already game over

            isGameOver = true;
            Time.timeScale = 0f; // Pause game

            if (gameOverUI != null)
            {
                gameOverUI.SetActive(true);
            }

            Debug.Log("[GameManager] Game Over!");
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
            var mapManager = FindFirstObjectByType<GloomCraft.Dungeon.MapInitializationManager>();
            if (mapManager != null)
            {
                // New system
                mapManager.RegenerateWithNewSeed();
                Debug.Log("[GameManager] Map regenerated with new layout (new system)!");
                return;
            }
            
            // Try legacy system
            #pragma warning disable CS0618 // Type is obsolete but needed for backward compatibility
            var legacyBuilder = FindFirstObjectByType<GloomCraft.Dungeon.UnityDungeonTilemapBuilder>();
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
