using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoulKnightClone.Core
{
    /// <summary>
    /// Singleton GameManager quản lý trạng thái game, scene loading, và các hệ thống global
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private bool isPaused = false;
        [SerializeField] private int currentLevel = 1;

        [Header("Player Reference")]
        public Transform playerTransform;
        public Player.PlayerController playerController;

        // Events
        public System.Action OnGamePaused;
        public System.Action OnGameResumed;
        public System.Action<int> OnLevelChanged;

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeGame();
        }

        private void InitializeGame()
        {
            Application.targetFrameRate = 60;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            FindPlayerReferences();
        }

        private void FindPlayerReferences()
        {
            if (playerController == null)
            {
                playerController = FindObjectOfType<Player.PlayerController>();
                if (playerController != null)
                {
                    playerTransform = playerController.transform;
                }
            }
        }

        public void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;
            OnGamePaused?.Invoke();
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
            OnGameResumed?.Invoke();
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadNextLevel()
        {
            currentLevel++;
            OnLevelChanged?.Invoke(currentLevel);
            // TODO: Implement dungeon regeneration
        }

        public bool IsPaused() => isPaused;

        private void OnApplicationQuit()
        {
            Time.timeScale = 1f;
        }
    }
}
