using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Text;

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

        [Header("Auto UI (match screenshot)")]
        [SerializeField] private bool autoBuildUiIfMissing = true;
        [SerializeField] private bool autoReplaceExistingUi = true;
        [SerializeField] private string pausedTitle = "PAUSE";
        [SerializeField] private Color overlayColor = new(0f, 0f, 0f, 0.55f);
        [SerializeField] private Color panelColor = new(0.12f, 0.14f, 0.16f, 0.95f);
        [SerializeField] private Color panelStrokeColor = new(0.86f, 0.78f, 0.60f, 1f);
        [SerializeField] private Color ribbonColor = new(0.65f, 0.12f, 0.12f, 1f);
        [SerializeField] private Color buttonColor = new(0.60f, 0.44f, 0.23f, 1f);
        [SerializeField] private Color buttonStrokeColor = new(0.86f, 0.78f, 0.60f, 1f);

        [Header("Sprites (optional)")]
        [SerializeField] private Sprite homeButtonSprite;
        [SerializeField] private Sprite resumeButtonSprite;
        [SerializeField] private Sprite quitButtonSprite;
        [SerializeField] private Sprite slotStarSprite;
        [SerializeField] private Sprite slotPlusSprite;
        [SerializeField] private Sprite avatarSprite;
        [SerializeField] private Sprite settingsButtonSprite;

        [Header("Avatar (optional)")]
        [SerializeField] private Vector2 avatarSize = new(84f, 84f);
        [SerializeField] private Vector2 avatarOffset = new(24f, -28f);

        [Header("Settings (audio)")]
        [SerializeField] private AudioMixer audioMixer;
        [Tooltip("Nếu AudioMixer chưa kéo vào Inspector, thử load từ Resources (đường dẫn không có .mixer). Ví dụ: Audio/MainMixer")]
        [SerializeField] private string mixerResourcesPath = "";

        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string musicVolumeParam = "MusicVolume";
        [SerializeField] private string sfxVolumeParam = "SFXVolume";

        [Header("Settings")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        public bool IsPaused => GameIsPaused;

        private GameObject _settingsPanel;
        private Button _settingsButton;
        private GameObject _mainPanelRoot;
        private readonly List<GameObject> _upgradeSlots = new();
        private TextMeshProUGUI _characterStatsText;

        private PlayerController2D _playerController;
        private PlayerHealth2D _playerHealth;
        private PlayerStamina _playerStamina;
        private PlayerMeleeController _playerMelee;
        private PlayerSpellController _playerSpell;
        private PlayerShooter2D _playerShooter;
        private PlayerStatusEffects _playerStatusEffects;

        private readonly StringBuilder _statsBuilder = new(256);
        private float _statsRefreshTimer;
        private const float StatsRefreshInterval = 0.10f;

        private void Start()
        {
            GameIsPaused = false;

            if (audioMixer == null && !string.IsNullOrWhiteSpace(mixerResourcesPath))
            {
                audioMixer = Resources.Load<AudioMixer>(mixerResourcesPath.Trim());
                if (audioMixer == null)
                    Debug.LogWarning($"[PauseMenuUI] Không tìm thấy AudioMixer tại Resources/{mixerResourcesPath}. Kéo mixer vào field Audio Mixer hoặc tạo file trong thư mục Resources.");
            }

            // Many scenes already have a legacy pause menu wired in the inspector.
            // If enabled, replace it at runtime with the new "screenshot" UI.
            if (autoReplaceExistingUi && pauseMenuPanel != null)
            {
                var legacyPanel = pauseMenuPanel;
                BuildUi();
                if (legacyPanel != null)
                    legacyPanel.SetActive(false);
            }
            else if (autoBuildUiIfMissing && pauseMenuPanel == null)
            {
                BuildUi();
            }

            if (string.IsNullOrWhiteSpace(pausedTitle) || pausedTitle.Trim().ToUpperInvariant() == "PAUSED")
                pausedTitle = "PAUSE";

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
                titleText.text = pausedTitle;

            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(ToggleSettings);

            EnsureLegacyStatsText();
            TryBindPlayerReferences();
            RefreshCharacterStatsUI();
        }

        private string _rebindActionName;
        private TextMeshProUGUI _rebindBindingText;
        private bool _isRebinding;

        private void Update()
        {
            // Không cho mở pause menu khi đã Game Over
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
                return;

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            // If rebinding, capture the next key press instead of normal ESC handling
            if (_isRebinding)
            {
                // Cancel on Escape
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    _isRebinding = false;
                    if (_rebindBindingText != null)
                        _rebindBindingText.text = GetBindingDisplay(_rebindActionName);
                    return;
                }

                // Check all keys
                foreach (Key key in System.Enum.GetValues(typeof(Key)))
                {
                    if (key == Key.None) continue;
                    try
                    {
                        if (keyboard[key].wasPressedThisFrame)
                        {
                            var kb = KeyBindManager.Instance;
                            if (kb != null)
                                kb.SetKey(_rebindActionName, key);

                            if (_rebindBindingText != null)
                                _rebindBindingText.text = GetBindingDisplay(_rebindActionName);

                            _isRebinding = false;
                            return;
                        }
                    }
                    catch { /* Some Key enum values may not be valid */ }
                }
                return;
            }

            // wasPressedThisFrame vẫn hoạt động khi Time.timeScale=0
            // vì Input System update theo real time
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (GameIsPaused)
                    ResumeGame();
                else
                    PauseGame();
            }

            if (GameIsPaused)
            {
                _statsRefreshTimer -= Time.unscaledDeltaTime;
                if (_statsRefreshTimer <= 0f)
                {
                    _statsRefreshTimer = StatsRefreshInterval;
                    RefreshCharacterStatsUI();
                }
            }
        }

        public void PauseGame()
        {
            GameIsPaused = true;
            Time.timeScale = 0f;

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);

            // Always return to main pause view when pausing
            ShowSettings(false);

            // Cập nhật slot hiển thị nâng cấp đã chọn
            RefreshUpgradeSlots();
            EnsureLegacyStatsText();
            _statsRefreshTimer = 0f;
            RefreshCharacterStatsUI();

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
            SceneLoader.LoadScene(mainMenuSceneName);
        }

        private void OnQuitClicked()
        {
            Debug.Log("[PauseMenuUI] Quitting to main menu with auto-save...");

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }
            else
            {
                var saveManager = FindFirstObjectByType<SaveManager>();
                if (saveManager != null)
                {
                    saveManager.SaveGame();
                }
            }

            GameIsPaused = false;
            Time.timeScale = 1f;
            SceneLoader.LoadScene(mainMenuSceneName);
        }

        private void OnDestroy()
        {
            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(OnResumeClicked);

            if (saveAndQuitButton != null)
                saveAndQuitButton.onClick.RemoveListener(OnSaveAndQuitClicked);

            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuitClicked);

            if (_settingsButton != null)
                _settingsButton.onClick.RemoveListener(ToggleSettings);
        }

        private void ToggleSettings()
        {
            if (_settingsPanel == null) return;
            ShowSettings(!_settingsPanel.activeSelf);
        }

        private void ShowSettings(bool show)
        {
            if (_settingsPanel == null) return;
            if (_mainPanelRoot != null) _mainPanelRoot.SetActive(!show);
            _settingsPanel.SetActive(show);
        }

        private void BuildUi()
        {
            var root = new GameObject("PauseMenu_Auto");
            root.transform.SetParent(transform, false);

            // Canvas
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            root.AddComponent<GraphicRaycaster>();

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Overlay
            var overlayGo = NewUiImage("Overlay", root.transform, overlayColor);
            StretchToFull(overlayGo.GetComponent<RectTransform>());

            // Panel (with stroke)
            var panelStrokeGo = NewUiImage("PanelStroke", overlayGo.transform, panelStrokeColor);
            var panelStrokeRt = panelStrokeGo.GetComponent<RectTransform>();
            panelStrokeRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelStrokeRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelStrokeRt.pivot = new Vector2(0.5f, 0.5f);
            panelStrokeRt.sizeDelta = new Vector2(980f, 520f);
            panelStrokeRt.anchoredPosition = new Vector2(0f, 10f);
            _mainPanelRoot = panelStrokeGo;

            var panelGo = NewUiImage("Panel", panelStrokeGo.transform, panelColor);
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(960f, 500f);
            panelRt.anchoredPosition = Vector2.zero;

            // Ribbon title
            var ribbonStrokeGo = NewUiImage("RibbonStroke", panelStrokeGo.transform, panelStrokeColor);
            var ribbonStrokeRt = ribbonStrokeGo.GetComponent<RectTransform>();
            ribbonStrokeRt.anchorMin = new Vector2(0.5f, 1f);
            ribbonStrokeRt.anchorMax = new Vector2(0.5f, 1f);
            ribbonStrokeRt.pivot = new Vector2(0.5f, 0.5f);
            ribbonStrokeRt.sizeDelta = new Vector2(420f, 86f);
            ribbonStrokeRt.anchoredPosition = new Vector2(0f, -26f);

            var ribbonGo = NewUiImage("Ribbon", ribbonStrokeGo.transform, ribbonColor);
            var ribbonRt = ribbonGo.GetComponent<RectTransform>();
            ribbonRt.anchorMin = new Vector2(0.5f, 0.5f);
            ribbonRt.anchorMax = new Vector2(0.5f, 0.5f);
            ribbonRt.pivot = new Vector2(0.5f, 0.5f);
            ribbonRt.sizeDelta = new Vector2(404f, 70f);
            ribbonRt.anchoredPosition = Vector2.zero;

            titleText = NewTmpText("Title", ribbonGo.transform, pausedTitle, 54, new Color(0.98f, 0.96f, 0.92f, 1f));
            titleText.alignment = TextAlignmentOptions.Center;
            StretchToFull(titleText.rectTransform);

            // Avatar slot (top-left)
            var avatarStrokeGo = NewUiImage("AvatarStroke", panelStrokeGo.transform, panelStrokeColor);
            var avatarStrokeRt = avatarStrokeGo.GetComponent<RectTransform>();
            avatarStrokeRt.anchorMin = new Vector2(0f, 1f);
            avatarStrokeRt.anchorMax = new Vector2(0f, 1f);
            avatarStrokeRt.pivot = new Vector2(0f, 1f);
            avatarStrokeRt.sizeDelta = avatarSize;
            avatarStrokeRt.anchoredPosition = avatarOffset;

            var avatarBgGo = NewUiImage("AvatarBg", avatarStrokeGo.transform, new Color(0.08f, 0.09f, 0.10f, 1f));
            var avatarBgRt = avatarBgGo.GetComponent<RectTransform>();
            StretchToFull(avatarBgRt);
            avatarBgRt.offsetMin = new Vector2(4f, 4f);
            avatarBgRt.offsetMax = new Vector2(-4f, -4f);

            if (avatarSprite != null)
            {
                var avatarGo = new GameObject("Avatar");
                avatarGo.transform.SetParent(avatarBgGo.transform, false);
                var avatarImg = avatarGo.AddComponent<Image>();
                avatarImg.sprite = avatarSprite;
                avatarImg.preserveAspect = true;
                avatarImg.color = Color.white;
                var avatarRt = avatarImg.GetComponent<RectTransform>();
                StretchToFull(avatarRt);
                avatarRt.offsetMin = new Vector2(6f, 6f);
                avatarRt.offsetMax = new Vector2(-6f, -6f);
            }

            // Settings button (top-right, symmetric to avatar)
            var settingsStrokeGo = NewUiImage("SettingsStroke", panelStrokeGo.transform, panelStrokeColor);
            var settingsStrokeRt = settingsStrokeGo.GetComponent<RectTransform>();
            settingsStrokeRt.anchorMin = new Vector2(1f, 1f);
            settingsStrokeRt.anchorMax = new Vector2(1f, 1f);
            settingsStrokeRt.pivot = new Vector2(1f, 1f);
            settingsStrokeRt.sizeDelta = avatarSize;
            settingsStrokeRt.anchoredPosition = new Vector2(-avatarOffset.x, avatarOffset.y);

            var settingsBgGo = NewUiImage("SettingsBg", settingsStrokeGo.transform, new Color(0.08f, 0.09f, 0.10f, 1f));
            var settingsBgRt = settingsBgGo.GetComponent<RectTransform>();
            StretchToFull(settingsBgRt);
            settingsBgRt.offsetMin = new Vector2(4f, 4f);
            settingsBgRt.offsetMax = new Vector2(-4f, -4f);

            _settingsButton = settingsBgGo.AddComponent<Button>();
            var sbc = _settingsButton.colors;
            sbc.highlightedColor = new Color(1f, 1f, 1f, 0.12f);
            sbc.pressedColor = new Color(1f, 1f, 1f, 0.08f);
            _settingsButton.colors = sbc;

            if (settingsButtonSprite != null)
            {
                var iconGo = new GameObject("SettingsIcon");
                iconGo.transform.SetParent(settingsBgGo.transform, false);
                var icon = iconGo.AddComponent<Image>();
                icon.sprite = settingsButtonSprite;
                icon.preserveAspect = true;
                icon.color = Color.white;
                var rt = icon.GetComponent<RectTransform>();
                StretchToFull(rt);
                rt.offsetMin = new Vector2(10f, 10f);
                rt.offsetMax = new Vector2(-10f, -10f);
            }
            else
            {
                var t = NewTmpText("SettingsGlyph", settingsBgGo.transform, "⚙", 44, new Color(0.95f, 0.95f, 0.95f, 0.90f));
                t.alignment = TextAlignmentOptions.Center;
                StretchToFull(t.rectTransform);
            }

            // Settings panel should be visible alone (hide main panel when open)
            _settingsPanel = BuildSettingsPanel(overlayGo.transform);
            _settingsPanel.SetActive(false);

            // Không hiển thị row icon nâng cấp trong pause menu
            _upgradeSlots.Clear();

            // Character stats panel
            var statsStrokeGo = NewUiImage("CharacterStatsStroke", panelGo.transform, panelStrokeColor);
            var statsStrokeRt = statsStrokeGo.GetComponent<RectTransform>();
            statsStrokeRt.anchorMin = new Vector2(0.06f, 0.16f);
            statsStrokeRt.anchorMax = new Vector2(0.94f, 0.76f);
            statsStrokeRt.pivot = new Vector2(0.5f, 0.5f);
            statsStrokeRt.offsetMin = Vector2.zero;
            statsStrokeRt.offsetMax = Vector2.zero;

            var statsViewportGo = NewUiImage("CharacterStatsViewport", statsStrokeGo.transform, new Color(0.08f, 0.09f, 0.10f, 1f));
            var statsViewportRt = statsViewportGo.GetComponent<RectTransform>();
            StretchToFull(statsViewportRt);
            statsViewportRt.offsetMin = new Vector2(4f, 4f);
            statsViewportRt.offsetMax = new Vector2(-20f, -4f);

            var statsMask = statsViewportGo.AddComponent<Mask>();
            statsMask.showMaskGraphic = true;

            var statsScrollRect = statsViewportGo.AddComponent<ScrollRect>();
            statsScrollRect.horizontal = false;
            statsScrollRect.vertical = true;
            statsScrollRect.movementType = ScrollRect.MovementType.Clamped;
            statsScrollRect.scrollSensitivity = 30f;

            var statsContentGo = new GameObject("CharacterStatsContent");
            statsContentGo.transform.SetParent(statsViewportGo.transform, false);
            var statsContentRt = statsContentGo.AddComponent<RectTransform>();
            statsContentRt.anchorMin = new Vector2(0f, 1f);
            statsContentRt.anchorMax = new Vector2(1f, 1f);
            statsContentRt.pivot = new Vector2(0.5f, 1f);
            statsContentRt.offsetMin = new Vector2(0f, 0f);
            statsContentRt.offsetMax = new Vector2(0f, 0f);

            _characterStatsText = NewTmpText("CharacterStatsText", statsContentGo.transform, "", 18f, new Color(0.98f, 0.96f, 0.92f, 0.96f));
            _characterStatsText.alignment = TextAlignmentOptions.TopLeft;
            _characterStatsText.textWrappingMode = TextWrappingModes.Normal;
            _characterStatsText.overflowMode = TextOverflowModes.Overflow;
            _characterStatsText.lineSpacing = 2f;
            var statsTextRt = _characterStatsText.rectTransform;
            statsTextRt.anchorMin = new Vector2(0f, 1f);
            statsTextRt.anchorMax = new Vector2(1f, 1f);
            statsTextRt.pivot = new Vector2(0.5f, 1f);
            statsTextRt.offsetMin = new Vector2(12f, 0f);
            statsTextRt.offsetMax = new Vector2(-20f, 0f);

            var statsFitter = _characterStatsText.gameObject.AddComponent<ContentSizeFitter>();
            statsFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            statsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var contentFitter = statsContentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var statsScrollbar = NewVerticalScrollbar(
                "CharacterStatsScrollbar",
                statsStrokeGo.transform,
                new Color(1f, 1f, 1f, 0.16f),
                new Color(0.90f, 0.82f, 0.63f, 0.95f));
            var statsScrollbarRt = statsScrollbar.GetComponent<RectTransform>();
            statsScrollbarRt.anchorMin = new Vector2(1f, 0f);
            statsScrollbarRt.anchorMax = new Vector2(1f, 1f);
            statsScrollbarRt.pivot = new Vector2(1f, 0.5f);
            statsScrollbarRt.sizeDelta = new Vector2(12f, 0f);
            statsScrollbarRt.anchoredPosition = new Vector2(-4f, 0f);

            statsScrollRect.viewport = statsViewportRt;
            statsScrollRect.content = statsContentRt;
            statsScrollRect.verticalScrollbar = statsScrollbar;
            statsScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            statsScrollRect.verticalScrollbarSpacing = 2f;

            // Bottom buttons bar
            var buttonsGo = new GameObject("Buttons");
            buttonsGo.transform.SetParent(panelGo.transform, false);
            var buttonsRt = buttonsGo.AddComponent<RectTransform>();
            buttonsRt.anchorMin = new Vector2(0.12f, 0.04f);
            buttonsRt.anchorMax = new Vector2(0.88f, 0.04f);
            buttonsRt.pivot = new Vector2(0.5f, 0f);
            buttonsRt.sizeDelta = new Vector2(0f, 90f);
            buttonsRt.anchoredPosition = new Vector2(0f, 0f);

            var hb = buttonsGo.AddComponent<HorizontalLayoutGroup>();
            hb.childAlignment = TextAnchor.LowerCenter;
            hb.spacing = 26f;
            hb.childForceExpandWidth = true;
            hb.childForceExpandHeight = false;

            saveAndQuitButton = NewBigButton(buttonsGo.transform, "HOME", "⌂", homeButtonSprite);
            resumeButton = NewBigButton(buttonsGo.transform, "RESUME", "▶", resumeButtonSprite);
            quitButton = NewBigButton(buttonsGo.transform, "QUIT", "⚙", quitButtonSprite);

            pauseMenuPanel = root;
        }

        private void TryBindPlayerReferences()
        {
            if (_playerController == null)
                _playerController = FindFirstObjectByType<PlayerController2D>();

            if (_playerController == null)
                return;

            if (_playerHealth == null)
                _playerHealth = _playerController.GetComponent<PlayerHealth2D>();
            if (_playerStamina == null)
                _playerStamina = _playerController.GetComponent<PlayerStamina>();
            if (_playerMelee == null)
                _playerMelee = _playerController.GetComponent<PlayerMeleeController>();
            if (_playerSpell == null)
                _playerSpell = _playerController.GetComponent<PlayerSpellController>();
            if (_playerShooter == null)
                _playerShooter = _playerController.GetComponent<PlayerShooter2D>();
            if (_playerStatusEffects == null)
                _playerStatusEffects = _playerController.GetComponent<PlayerStatusEffects>();
        }

        private void EnsureLegacyStatsText()
        {
            if (_characterStatsText != null || pauseMenuPanel == null)
                return;

            var statsBgGo = NewUiImage("CharacterStatsLegacyBg", pauseMenuPanel.transform, new Color(0.08f, 0.09f, 0.10f, 0.82f));
            var rt = statsBgGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.04f, 0.04f);
            rt.anchorMax = new Vector2(0.52f, 0.40f);
            rt.pivot = new Vector2(0f, 0f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            statsBgGo.AddComponent<Mask>().showMaskGraphic = true;

            var scroll = statsBgGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var contentGo = new GameObject("CharacterStatsLegacyContent");
            contentGo.transform.SetParent(statsBgGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            _characterStatsText = NewTmpText("CharacterStatsLegacyText", contentGo.transform, "", 16f, new Color(0.98f, 0.96f, 0.92f, 0.96f));
            _characterStatsText.alignment = TextAlignmentOptions.TopLeft;
            _characterStatsText.textWrappingMode = TextWrappingModes.Normal;
            _characterStatsText.overflowMode = TextOverflowModes.Overflow;
            _characterStatsText.lineSpacing = 1f;
            var textRt = _characterStatsText.rectTransform;
            textRt.anchorMin = new Vector2(0f, 1f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot = new Vector2(0.5f, 1f);
            textRt.offsetMin = new Vector2(12f, 0f);
            textRt.offsetMax = new Vector2(-20f, 0f);

            var textFitter = _characterStatsText.gameObject.AddComponent<ContentSizeFitter>();
            textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var legacyScrollbar = NewVerticalScrollbar(
                "CharacterStatsLegacyScrollbar",
                statsBgGo.transform,
                new Color(1f, 1f, 1f, 0.16f),
                new Color(0.90f, 0.82f, 0.63f, 0.95f));
            var legacyScrollbarRt = legacyScrollbar.GetComponent<RectTransform>();
            legacyScrollbarRt.anchorMin = new Vector2(1f, 0f);
            legacyScrollbarRt.anchorMax = new Vector2(1f, 1f);
            legacyScrollbarRt.pivot = new Vector2(1f, 0.5f);
            legacyScrollbarRt.sizeDelta = new Vector2(12f, 0f);
            legacyScrollbarRt.anchoredPosition = new Vector2(-4f, 0f);

            scroll.viewport = rt;
            scroll.content = contentRt;
            scroll.verticalScrollbar = legacyScrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.verticalScrollbarSpacing = 2f;
        }

        private void RefreshCharacterStatsUI()
        {
            if (_characterStatsText == null)
                return;

            TryBindPlayerReferences();

            if (_playerHealth == null)
            {
                _characterStatsText.text = "KHONG TIM THAY PLAYER";
                return;
            }

            float damageMul = _playerStatusEffects != null ? _playerStatusEffects.DamageMultiplier : 1f;
            float moveMul = _playerStatusEffects != null ? _playerStatusEffects.MoveSpeedMultiplier : 1f;

            int meleeBase = _playerMelee != null ? _playerMelee.BaseDamage : 0;
            int meleeEffective = Mathf.RoundToInt(meleeBase * damageMul);

            int spell1Base = _playerSpell != null ? _playerSpell.GetSpellDamage(1) : 0;
            int spell2Base = _playerSpell != null ? _playerSpell.GetSpellDamage(2) : 0;
            int spell3Base = _playerSpell != null ? _playerSpell.GetSpellDamage(3) : 0;
            int spell1Effective = Mathf.RoundToInt(spell1Base * damageMul);
            int spell2Effective = Mathf.RoundToInt(spell2Base * damageMul);
            int spell3Effective = Mathf.RoundToInt(spell3Base * damageMul);

            float spell1Cd = _playerSpell != null ? _playerSpell.GetSpellCooldown(1) : 0f;
            float spell2Cd = _playerSpell != null ? _playerSpell.GetSpellCooldown(2) : 0f;
            float spell3Cd = _playerSpell != null ? _playerSpell.GetSpellCooldown(3) : 0f;
            float spell1Range = _playerSpell != null ? _playerSpell.GetSpellRange(1) : 0f;
            float spell2Range = _playerSpell != null ? _playerSpell.GetSpellRange(2) : 0f;
            float spell3Range = _playerSpell != null ? _playerSpell.GetSpellRange(3) : 0f;

            float moveSpeed = _playerController != null ? _playerController.MaxMoveSpeed : 0f;
            float dashSpeed = _playerController != null ? _playerController.DashSpeed : 0f;
            float effectiveMoveSpeed = moveSpeed * moveMul;

            float staminaRegen = _playerStamina != null ? _playerStamina.RegenPerSecond : 0f;
            float healthRegen = _playerHealth.RegenerationPerSecond;
            float invincibleWindow = _playerHealth.InvincibleDuration;
            float meleeRange = _playerMelee != null ? _playerMelee.AttackRange : 0f;
            float knockback = _playerMelee != null ? _playerMelee.KnockbackForce : 0f;
            float fireRate = _playerShooter != null ? _playerShooter.FireRatePerSecond : 0f;

            _statsBuilder.Clear();
            _statsBuilder.Append("THONG SO NHAN VAT\n\n");
            _statsBuilder.Append("SINH TON\n");
            _statsBuilder.Append("  HP: ").Append(_playerHealth.CurrentHealth).Append('/').Append(_playerHealth.MaxHealth)
                .Append("    STA: ");

            if (_playerStamina != null)
            {
                _statsBuilder.Append(Mathf.RoundToInt(_playerStamina.CurrentStamina))
                    .Append('/')
                    .Append(Mathf.RoundToInt(_playerStamina.MaxStamina))
                    .Append("\n  Regen HP: ").Append(healthRegen.ToString("0.##")).Append("/s")
                    .Append("    Regen STA: ").Append(staminaRegen.ToString("0.##")).Append("/s")
                    .Append("    I-Frame: ").Append(invincibleWindow.ToString("0.##")).Append("s");
            }
            else
            {
                _statsBuilder.Append("--")
                    .Append("\n  Regen HP: ").Append(healthRegen.ToString("0.##")).Append("/s")
                    .Append("    I-Frame: ").Append(invincibleWindow.ToString("0.##")).Append("s");
            }

            _statsBuilder.Append("\n\nCHI SO CHIEN DAU\n");
            

            _statsBuilder.Append("\n  Spell DMG: ")
                .Append(spell1Effective).Append('/').Append(spell2Effective).Append('/').Append(spell3Effective)
                .Append("\n  Spell CD: ").Append(spell1Cd.ToString("0.##")).Append('/').Append(spell2Cd.ToString("0.##")).Append('/').Append(spell3Cd.ToString("0.##"))
                .Append("    Spell RNG: ").Append(spell1Range.ToString("0.##")).Append('/').Append(spell2Range.ToString("0.##")).Append('/').Append(spell3Range.ToString("0.##"));

            _statsBuilder.Append("\n  Fire Rate: ")
                .Append(fireRate.ToString("0.##"))
                .Append("/s")
                .Append("    Move: ").Append(effectiveMoveSpeed.ToString("0.00"))
                .Append(" (Base ").Append(moveSpeed.ToString("0.00")).Append(")")
                .Append("    Dash: ").Append(dashSpeed.ToString("0.00"));

            if (Mathf.Abs(damageMul - 1f) > 0.001f || Mathf.Abs(moveMul - 1f) > 0.001f)
            {
                _statsBuilder.Append("\n\nBUFF\n  DMG x").Append(damageMul.ToString("0.##"))
                    .Append("    MOVE x").Append(moveMul.ToString("0.##"));
            }

            _characterStatsText.text = _statsBuilder.ToString();
        }

        private GameObject BuildSettingsPanel(Transform parent)
        {
            var strokeGo = NewUiImage("SettingsPanelStroke", parent, panelStrokeColor);
            var strokeRt = strokeGo.GetComponent<RectTransform>();
            // Fixed-size centered panel to avoid layout collapse on odd aspect ratios
            strokeRt.anchorMin = new Vector2(0.5f, 0.5f);
            strokeRt.anchorMax = new Vector2(0.5f, 0.5f);
            strokeRt.pivot = new Vector2(0.5f, 0.5f);
            strokeRt.sizeDelta = new Vector2(1600f, 900f);
            strokeRt.anchoredPosition = Vector2.zero;

            var panelGo = NewUiImage("SettingsPanel", strokeGo.transform, new Color(panelColor.r, panelColor.g, panelColor.b, 0.98f));
            var panelRt = panelGo.GetComponent<RectTransform>();
            StretchToFull(panelRt);
            panelRt.offsetMin = new Vector2(10f, 10f);
            panelRt.offsetMax = new Vector2(-10f, -10f);

            var header = NewTmpText("Header", panelGo.transform, "CÀI ĐẶT", 54, new Color(0.98f, 0.96f, 0.92f, 1f));
            header.alignment = TextAlignmentOptions.Left;
            var headerRt = header.rectTransform;
            headerRt.anchorMin = new Vector2(0.06f, 0.86f);
            headerRt.anchorMax = new Vector2(0.94f, 0.99f);
            headerRt.pivot = new Vector2(0f, 1f);
            headerRt.offsetMin = Vector2.zero;
            headerRt.offsetMax = Vector2.zero;

            // Close button (X)
            var closeStroke = NewUiImage("CloseStroke", panelGo.transform, panelStrokeColor);
            var closeStrokeRt = closeStroke.GetComponent<RectTransform>();
            closeStrokeRt.anchorMin = new Vector2(1f, 1f);
            closeStrokeRt.anchorMax = new Vector2(1f, 1f);
            closeStrokeRt.pivot = new Vector2(1f, 1f);
            closeStrokeRt.sizeDelta = new Vector2(80f, 80f);
            closeStrokeRt.anchoredPosition = new Vector2(-14f, -14f);

            var closeBg = NewUiImage("CloseBg", closeStroke.transform, new Color(0.08f, 0.09f, 0.10f, 1f));
            var closeBgRt = closeBg.GetComponent<RectTransform>();
            StretchToFull(closeBgRt);
            closeBgRt.offsetMin = new Vector2(4f, 4f);
            closeBgRt.offsetMax = new Vector2(-4f, -4f);

            var closeBtn = closeBg.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => ShowSettings(false));
            var closeTxt = NewTmpText("CloseX", closeBg.transform, "✕", 44, new Color(0.98f, 0.96f, 0.92f, 0.95f));
            closeTxt.alignment = TextAlignmentOptions.Center;
            StretchToFull(closeTxt.rectTransform);

            // ScrollView for content (many rows)
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(panelGo.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.06f, 0.06f);
            scrollRt.anchorMax = new Vector2(0.94f, 0.84f);
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;

            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f;

            // Mask so content clips properly (alpha must be >0 for mask to work)
            var maskImg = scrollGo.AddComponent<Image>();
            maskImg.color = Color.white;
            scrollGo.AddComponent<Mask>().showMaskGraphic = false;

            // Content container (inside scroll)
            var content = new GameObject("Content");
            content.transform.SetParent(scrollGo.transform, false);
            var contentRt = content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var v = content.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperLeft;
            v.spacing = 18f;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRt;

            // Audio section
            BuildAudioRow(content.transform, "Master", "MasterVolume", masterVolumeParam);
            BuildAudioRow(content.transform, "Music", "MusicVolume", musicVolumeParam);
            BuildAudioRow(content.transform, "SFX", "SFXVolume", sfxVolumeParam);

            // Divider
            var divider = NewUiImage("Divider", content.transform, new Color(1f, 1f, 1f, 0.08f));
            divider.AddComponent<LayoutElement>().preferredHeight = 12f;
            divider.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 3f);

            // Keybinds — uses KeyBindManager (real rebinding)
            string[] rebindActions = {
                KeyBindManager.ACT_MOVE_UP, KeyBindManager.ACT_MOVE_DOWN,
                KeyBindManager.ACT_MOVE_LEFT, KeyBindManager.ACT_MOVE_RIGHT,
                KeyBindManager.ACT_DASH, KeyBindManager.ACT_ATTACK,
                KeyBindManager.ACT_SPELL1, KeyBindManager.ACT_SPELL2, KeyBindManager.ACT_SPELL3,
                KeyBindManager.ACT_INTERACT
            };
            foreach (var action in rebindActions)
            {
                BuildRebindRow(content.transform, KeyBindManager.GetActionLabel(action), action);
            }

            return strokeGo;
        }

        private void BuildAudioRow(Transform parent, string label, string prefsKey, string mixerParam)
        {
            var row = new GameObject($"Audio_{label}");
            row.transform.SetParent(parent, false);
            row.AddComponent<LayoutElement>().preferredHeight = 76f;

            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 16f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandHeight = true;
            h.childForceExpandWidth = false;

            var t = NewTmpText("Label", row.transform, label.ToUpperInvariant(), 34, new Color(0.98f, 0.96f, 0.92f, 0.95f));
            t.alignment = TextAlignmentOptions.Left;
            t.rectTransform.sizeDelta = new Vector2(220f, 0f);

            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(row.transform, false);
            var sLe = sliderGo.AddComponent<LayoutElement>();
            sLe.preferredWidth = 720f;
            sLe.preferredHeight = 56f;

            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0.0001f;
            slider.maxValue = 1f;
            slider.value = PlayerPrefs.GetFloat(prefsKey, 1f);

            var bg = NewUiImage("Bg", sliderGo.transform, new Color(1f, 1f, 1f, 0.10f));
            StretchToFull(bg.GetComponent<RectTransform>());

            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var faRt = fillArea.AddComponent<RectTransform>();
            StretchToFull(faRt);
            faRt.offsetMin = new Vector2(12f, 14f);
            faRt.offsetMax = new Vector2(-12f, -14f);

            var fill = NewUiImage("Fill", fillArea.transform, new Color(0.50f, 0.86f, 1.00f, 0.95f));
            var fillRt = fill.GetComponent<RectTransform>();
            StretchToFull(fillRt);
            slider.fillRect = fillRt;
            slider.targetGraphic = fill.GetComponent<Image>();

            // Handle để dễ kéo slider (Unity Slider cần handleRect để drag)
            var handleArea = new GameObject("HandleSlideArea");
            handleArea.transform.SetParent(sliderGo.transform, false);
            var haRt = handleArea.AddComponent<RectTransform>();
            StretchToFull(haRt);
            var handle = NewUiImage("Handle", handleArea.transform, new Color(1f, 1f, 1f, 0.9f));
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.anchorMin = new Vector2(0f, 0.5f);
            handleRt.anchorMax = new Vector2(0f, 0.5f);
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.sizeDelta = new Vector2(24f, 36f);
            handleRt.anchoredPosition = Vector2.zero;
            slider.handleRect = handleRt;
            slider.direction = Slider.Direction.LeftToRight;

            void Apply(float v)
            {
                AudioVolumeHelper.ApplyLinearToMixer(audioMixer, mixerParam, v);
                PlayerPrefs.SetFloat(prefsKey, v);
            }

            slider.onValueChanged.AddListener(Apply);
            Apply(slider.value);
        }

        private void BuildRebindRow(Transform parent, string label, string actionName)
        {
            var row = new GameObject($"Rebind_{label}");
            row.transform.SetParent(parent, false);
            row.AddComponent<LayoutElement>().preferredHeight = 76f;

            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 16f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandHeight = true;
            h.childForceExpandWidth = false;

            var t = NewTmpText("Label", row.transform, label, 34, new Color(0.98f, 0.96f, 0.92f, 0.95f));
            t.alignment = TextAlignmentOptions.Left;
            t.rectTransform.sizeDelta = new Vector2(260f, 0f);

            var bindingText = NewTmpText("Binding", row.transform, GetBindingDisplay(actionName), 32, new Color(0.75f, 0.86f, 1f, 0.95f));
            bindingText.alignment = TextAlignmentOptions.Left;
            bindingText.rectTransform.sizeDelta = new Vector2(300f, 0f);

            var btn = NewSmallButton(row.transform, "REBIND");
            btn.onClick.AddListener(() => StartRebind(actionName, bindingText));
        }

        private Button NewSmallButton(Transform parent, string text)
        {
            var strokeGo = NewUiImage("BtnStroke", parent, buttonStrokeColor);
            var le = strokeGo.AddComponent<LayoutElement>();
            le.preferredWidth = 240f;
            le.preferredHeight = 62f;

            var bgGo = NewUiImage("BtnBg", strokeGo.transform, buttonColor);
            var bgRt = bgGo.GetComponent<RectTransform>();
            StretchToFull(bgRt);
            bgRt.offsetMin = new Vector2(6f, 6f);
            bgRt.offsetMax = new Vector2(-6f, -6f);

            var btn = bgGo.AddComponent<Button>();
            var txt = NewTmpText("BtnText", bgGo.transform, text, 28, new Color(0.20f, 0.12f, 0.06f, 0.95f));
            txt.alignment = TextAlignmentOptions.Center;
            StretchToFull(txt.rectTransform);
            return btn;
        }

        private string GetBindingDisplay(string actionName)
        {
            var kb = KeyBindManager.Instance;
            if (kb == null) return KeyBindManager.GetKeyDisplayName(KeyBindManager.GetDefault(actionName));
            return KeyBindManager.GetKeyDisplayName(kb.GetKey(actionName));
        }

        private void StartRebind(string actionName, TextMeshProUGUI bindingText)
        {
            var kb = KeyBindManager.Instance;
            if (kb == null) return;

            bindingText.text = "Nhấn phím...";

            // Listen for any key press next frame
            _rebindActionName = actionName;
            _rebindBindingText = bindingText;
            _isRebinding = true;
        }

        private Button NewBigButton(Transform parent, string name, string glyph, Sprite sprite)
        {
            var strokeGo = NewUiImage($"{name}_Stroke", parent, buttonStrokeColor);
            var strokeRt = strokeGo.GetComponent<RectTransform>();
            strokeRt.sizeDelta = new Vector2(0f, 84f);

            // Let layout control width; keep a stable height
            var le = strokeGo.AddComponent<LayoutElement>();
            le.preferredHeight = 84f;
            le.minHeight = 84f;

            var bgGo = NewUiImage($"{name}_Bg", strokeGo.transform, buttonColor);
            var bgRt = bgGo.GetComponent<RectTransform>();
            StretchToFull(bgRt);
            bgRt.offsetMin = new Vector2(6f, 6f);
            bgRt.offsetMax = new Vector2(-6f, -6f);

            var btn = bgGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(buttonColor.r * 1.06f, buttonColor.g * 1.06f, buttonColor.b * 1.06f, 1f);
            colors.pressedColor = new Color(buttonColor.r * 0.92f, buttonColor.g * 0.92f, buttonColor.b * 0.92f, 1f);
            btn.colors = colors;

            if (sprite != null)
            {
                var iconGo = new GameObject($"{name}_Icon");
                iconGo.transform.SetParent(bgGo.transform, false);
                var icon = iconGo.AddComponent<Image>();
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.color = Color.white;
                var rt = icon.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(46f, 46f);
                rt.anchoredPosition = Vector2.zero;
            }
            else
            {
                var txt = NewTmpText($"{name}_Text", bgGo.transform, glyph, 46, new Color(0.20f, 0.12f, 0.06f, 0.95f));
                txt.alignment = TextAlignmentOptions.Center;
                StretchToFull(txt.rectTransform);
            }

            return btn;
        }

        private static GameObject NewUiImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static TextMeshProUGUI NewTmpText(string name, Transform parent, string text, float fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            if (TMP_Settings.defaultFontAsset != null)
                t.font = TMP_Settings.defaultFontAsset;
            return t;
        }

        private static Scrollbar NewVerticalScrollbar(string name, Transform parent, Color trackColor, Color handleColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var trackImage = go.AddComponent<Image>();
            trackImage.color = trackColor;

            var scrollbar = go.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var slidingAreaGo = new GameObject("SlidingArea");
            slidingAreaGo.transform.SetParent(go.transform, false);
            var slidingAreaRt = slidingAreaGo.AddComponent<RectTransform>();
            StretchToFull(slidingAreaRt);
            slidingAreaRt.offsetMin = new Vector2(2f, 2f);
            slidingAreaRt.offsetMax = new Vector2(-2f, -2f);

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(slidingAreaGo.transform, false);
            var handleImage = handleGo.AddComponent<Image>();
            handleImage.color = handleColor;
            var handleRt = handleImage.GetComponent<RectTransform>();
            StretchToFull(handleRt);

            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handleRt;
            return scrollbar;
        }

        /// <summary>
        /// Cập nhật 9 slot icon trong pause menu để hiển thị nâng cấp đã chọn.
        /// Slot có nâng cấp sẽ hiện glyph + màu của upgrade; slot trống hiện "+".
        /// </summary>
        private void RefreshUpgradeSlots()
        {
            if (_upgradeSlots.Count == 0) return;

            var upgrades = UpgradeManager.Instance != null
                ? UpgradeManager.Instance.ChosenUpgrades
                : (IReadOnlyList<UpgradeData>)new List<UpgradeData>();

            for (int i = 0; i < _upgradeSlots.Count; i++)
            {
                var slot = _upgradeSlots[i];
                if (slot == null) continue;

                // Xóa nội dung cũ trong slot
                for (int c = slot.transform.childCount - 1; c >= 0; c--)
                    Destroy(slot.transform.GetChild(c).gameObject);

                if (i < upgrades.Count)
                {
                    var upgrade = upgrades[i];

                    // Đổi màu nền slot theo upgrade
                    var slotImg = slot.GetComponent<Image>();
                    if (slotImg != null)
                        slotImg.color = new Color(upgrade.glyphColor.r * 0.2f, upgrade.glyphColor.g * 0.2f, upgrade.glyphColor.b * 0.2f, 1f);

                    if (upgrade.icon != null)
                    {
                        var iconGo = new GameObject("UpgradeIcon");
                        iconGo.transform.SetParent(slot.transform, false);
                        var icon = iconGo.AddComponent<Image>();
                        icon.sprite = upgrade.icon;
                        icon.preserveAspect = true;
                        icon.color = Color.white;
                        icon.raycastTarget = false;
                        var rt = icon.GetComponent<RectTransform>();
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        rt.sizeDelta = new Vector2(38f, 38f);
                        rt.anchoredPosition = Vector2.zero;
                    }
                    else
                    {
                        var t = NewTmpText($"UpgradeGlyph_{i}", slot.transform, upgrade.glyphSymbol, 34, upgrade.glyphColor);
                        t.alignment = TextAlignmentOptions.Center;
                        StretchToFull(t.rectTransform);
                    }
                }
                else
                {
                    // Slot trống - hiện "+"
                    var slotImg = slot.GetComponent<Image>();
                    if (slotImg != null)
                        slotImg.color = new Color(0.08f, 0.09f, 0.10f, 1f);

                    var t = NewTmpText($"SlotEmpty_{i}", slot.transform, "+", 40, new Color(0.95f, 0.95f, 0.95f, 0.85f));
                    t.alignment = TextAlignmentOptions.Center;
                    StretchToFull(t.rectTransform);
                }
            }
        }

        private static void StretchToFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
