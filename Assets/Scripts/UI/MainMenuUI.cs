using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button backButton;

    [Header("Settings")]
    [SerializeField] private string gameSceneName = "GameScene";

    private void Start()
    {
        // Setup button listeners
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        
        if (creditsButton != null)
            creditsButton.onClick.AddListener(OnCreditsClicked);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
        
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        // Show main menu panel by default
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        // Remove listeners to prevent memory leaks
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsClicked);
        
        if (creditsButton != null)
            creditsButton.onClick.RemoveListener(OnCreditsClicked);
        
        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);
        
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackClicked);
    }

    public void OnPlayClicked()
    {
        Debug.Log("Loading game scene: " + gameSceneName);
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnSettingsClicked()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void OnCreditsClicked()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        
        if (creditsPanel != null)
            creditsPanel.SetActive(true);
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quitting game...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void OnBackClicked()
    {
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
    }
}
