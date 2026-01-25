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

        /// <summary>
        /// Call this when player dies
        /// </summary>
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

        /// <summary>
        /// Restart current scene
        /// </summary>
        public void RestartGame()
        {
            Time.timeScale = 1f; // Resume time
            isGameOver = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Load main menu (if you have one)
        /// </summary>
        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            isGameOver = false;
            SceneManager.LoadScene("MainMenu"); // Change to your menu scene name
        }

        /// <summary>
        /// Quit game
        /// </summary>
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
