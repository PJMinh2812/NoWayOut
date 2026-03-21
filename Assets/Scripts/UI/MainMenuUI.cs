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
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button backButton;

    [Header("Settings")]
    [SerializeField] private string gameSceneName = "Level_01_TheAwakening";

    [Header("Audio")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField, Range(0f, 1f)] private float menuMusicVolume = 0.6f;

    private AudioSource menuMusicSource;

    private void Start()
    {
        // Setup button listeners
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        
        if (creditsButton != null)
            creditsButton.onClick.AddListener(OnCreditsClicked);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
        
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        // Kiểm tra có save file hay không để hiện/ẩn nút Continue
        UpdateContinueButton();

        // Show main menu panel by default
        ShowMainMenu();

        SetupMenuMusic();
    }

    private void SetupMenuMusic()
    {
        if (menuMusic == null)
            return;

        menuMusicSource = GetComponent<AudioSource>();
        if (menuMusicSource == null)
            menuMusicSource = gameObject.AddComponent<AudioSource>();

        menuMusicSource.clip = menuMusic;
        menuMusicSource.playOnAwake = false;
        menuMusicSource.loop = true;
        menuMusicSource.spatialBlend = 0f;
        menuMusicSource.volume = Mathf.Clamp01(menuMusicVolume);

        if (!menuMusicSource.isPlaying)
            menuMusicSource.Play();
    }

    private void UpdateContinueButton()
    {
        if (continueButton == null) return;

        // Tìm SaveManager hoặc kiểm tra file trực tiếp
        bool hasSave = false;
        if (NWO.SaveManager.Instance != null)
        {
            hasSave = NWO.SaveManager.Instance.HasSaveFile();
        }
        else
        {
            // Fallback: kiểm tra file trực tiếp
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, "savegame.json");
            hasSave = System.IO.File.Exists(savePath);
        }

        continueButton.gameObject.SetActive(hasSave);
    }

    private void OnDestroy()
    {
        // Remove listeners to prevent memory leaks
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayClicked);

        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);
        
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
        
        // New Game: xóa save cũ, generate map mới
        string savePath = System.IO.Path.Combine(Application.persistentDataPath, "savegame.json");
        if (System.IO.File.Exists(savePath))
            System.IO.File.Delete(savePath);
        
        PlayerPrefs.SetInt("GenerateNewMap", 1);
        PlayerPrefs.SetInt("LoadFromSave", 0);
        PlayerPrefs.Save();

        NWO.SceneLoader.LoadScene(gameSceneName);
    }

    public void OnContinueClicked()
    {
        Debug.Log("Continuing from save...");

        // Load scene từ save data
        string savePath = System.IO.Path.Combine(Application.persistentDataPath, "savegame.json");
        if (!System.IO.File.Exists(savePath))
        {
            Debug.LogWarning("No save file found!");
            return;
        }

        string json = System.IO.File.ReadAllText(savePath);
        var data = JsonUtility.FromJson<NWO.SaveData>(json);

        // Đánh dấu load from save
        PlayerPrefs.SetInt("LoadFromSave", 1);
        PlayerPrefs.SetInt("GenerateNewMap", 0);
        PlayerPrefs.Save();

        // Load scene đã save
        string sceneToLoad = !string.IsNullOrEmpty(data.sceneName) ? data.sceneName : gameSceneName;
        NWO.SceneLoader.LoadScene(sceneToLoad);
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
