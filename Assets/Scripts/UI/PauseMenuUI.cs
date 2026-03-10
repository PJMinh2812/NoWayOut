using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace NWO
{
    /// <summary>
    /// In-game Pause Menu UI.
    /// Nhấn ESC để mở/đóng menu pause khi đang chơi.
    /// Có 3 nút: Resume, Save & Quit, Quit (không save).
    /// </summary>
    public sealed class PauseMenuUI : MonoBehaviour
    {
        /// <summary>
        /// Static flag để các script khác (PlayerSpellController, etc.) biết game đang paused.
        /// </summary>
        public static bool GameIsPaused { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button saveAndQuitButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Settings")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        public bool IsPaused => GameIsPaused;

        private void Start()
        {
            GameIsPaused = false;

            // Ẩn menu pause khi bắt đầu
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            // Setup button listeners
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);

            if (saveAndQuitButton != null)
                saveAndQuitButton.onClick.AddListener(OnSaveAndQuitClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            if (titleText != null)
                titleText.text = "PAUSED";
        }

        private void Update()
        {
            // Không cho mở pause menu khi đã Game Over
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                return;

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            // wasPressedThisFrame vẫn hoạt động khi Time.timeScale=0
            // vì Input System update theo real time
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (GameIsPaused)
                    ResumeGame();
                else
                    PauseGame();
            }
        }

        public void PauseGame()
        {
            GameIsPaused = true;
            Time.timeScale = 0f;

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);

            Debug.Log("[PauseMenuUI] Game Paused");
        }

        public void ResumeGame()
        {
            GameIsPaused = false;
            Time.timeScale = 1f;

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            Debug.Log("[PauseMenuUI] Game Resumed");
        }

        private void OnResumeClicked()
        {
            ResumeGame();
        }

        private void OnSaveAndQuitClicked()
        {
            Debug.Log("[PauseMenuUI] Saving game and quitting to main menu...");

            // Save game trước
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }
            else
            {
                Debug.LogWarning("[PauseMenuUI] SaveManager not found! Game not saved.");
            }

            // Resume time rồi về Main Menu
            GameIsPaused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnQuitClicked()
        {
            Debug.Log("[PauseMenuUI] Quitting to main menu without saving...");

            GameIsPaused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnDestroy()
        {
            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(OnResumeClicked);

            if (saveAndQuitButton != null)
                saveAndQuitButton.onClick.RemoveListener(OnSaveAndQuitClicked);

            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuitClicked);
        }
    }
}
