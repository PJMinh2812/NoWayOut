using UnityEngine;
using UnityEngine.UI;
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
        [SerializeField] private Button quitButton;
        [SerializeField] private TextMeshProUGUI gameOverText;

        [Header("Settings")]
        [SerializeField] private string gameOverMessage = "GAME OVER";

        private void Awake()
        {
            // Setup button listeners
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
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

        private void OnQuitClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
        }

        private void OnDestroy()
        {
            // Clean up listeners
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnQuitClicked);
            }
        }
    }
}
