using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace NWO
{
    /// <summary>
    /// Controls Game Over screen UI
    /// </summary>
    public sealed class GameOverUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private TextMeshProUGUI gameOverText;

        [Header("Settings")]
        [SerializeField] private string gameOverMessage = "GAME OVER";
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private void Awake()
        {
            // Setup button listeners
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.AddListener(OnExitClicked);
            }

            // Set game over text
            if (gameOverText != null)
            {
                gameOverText.text = gameOverMessage;
            }
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }

        private void OnMainMenuClicked()
        {
            // Load Main Menu scene
            Time.timeScale = 1f; // Reset time scale in case game was paused
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnExitClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
            else
            {
                // Fallback nếu không có GameManager
                Debug.Log("[GameOverUI] Quitting game...");
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
        }

        private void OnDestroy()
        {
            // Clean up listeners
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitClicked);
            }
        }
    }
}
