using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace GloomCraft
{
    /// <summary>
    /// Controls Game Over screen UI
    /// </summary>
    public sealed class GameOverUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
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
        }
    }
}
