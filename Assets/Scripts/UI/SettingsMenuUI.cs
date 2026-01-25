using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class SettingsMenuUI : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private AudioMixer audioMixer;

    [Header("Graphics Settings")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    private Resolution[] resolutions;

    private void Start()
    {
        LoadSettings();
        SetupResolutionDropdown();
        SetupQualityDropdown();
        SetupEventListeners();
    }

    private void SetupEventListeners()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(SetQuality);
    }

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();
        System.Collections.Generic.HashSet<string> uniqueResolutions = new System.Collections.Generic.HashSet<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            
            // Chỉ thêm resolution không trùng lặp
            if (uniqueResolutions.Add(option))
            {
                options.Add(option);
            }

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = options.Count - 1;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        
        Debug.Log($"Loaded {options.Count} unique resolutions. Current: {Screen.currentResolution.width}x{Screen.currentResolution.height}");
    }

    private void SetupQualityDropdown()
    {
        if (qualityDropdown == null) return;

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
    }

    public void SetMasterVolume(float volume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
        
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void SetMusicVolume(float volume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
        
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
        
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutions == null || resolutionIndex >= resolutions.Length) return;

        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode, resolution.refreshRateRatio);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        
        // Convert refreshRateRatio to display value
        double refreshRate = resolution.refreshRateRatio.value;
        Debug.Log($"Resolution changed to: {resolution.width}x{resolution.height} @ {refreshRate:F2}Hz");
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
    }

    private void LoadSettings()
    {
        // Load audio settings
        if (masterVolumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            masterVolumeSlider.value = volume;
            SetMasterVolume(volume);
        }

        if (musicVolumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            musicVolumeSlider.value = volume;
            SetMusicVolume(volume);
        }

        if (sfxVolumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxVolumeSlider.value = volume;
            SetSFXVolume(volume);
        }

        // Load graphics settings
        if (fullscreenToggle != null)
        {
            bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            fullscreenToggle.isOn = isFullscreen;
        }

        if (qualityDropdown != null)
        {
            int qualityLevel = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
            qualityDropdown.value = qualityLevel;
        }
    }

    public void ResetToDefaults()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = 1f;
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = 1f;
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = 1f;
        
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = true;
        
        if (qualityDropdown != null)
            qualityDropdown.value = QualitySettings.GetQualityLevel();
    }
}
