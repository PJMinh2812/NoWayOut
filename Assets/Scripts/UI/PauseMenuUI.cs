using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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
        private GameObject _upgradeRowGo;

        private void Start()
        {
            GameIsPaused = false;

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
                pausedTitle = "DỪNG";

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
                    if (key == Key.None || key == Key.IMESelected) continue;
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
            panelStrokeRt.sizeDelta = new Vector2(980f, 360f);
            panelStrokeRt.anchoredPosition = new Vector2(0f, 40f);
            _mainPanelRoot = panelStrokeGo;

            var panelGo = NewUiImage("Panel", panelStrokeGo.transform, panelColor);
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(960f, 340f);
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

            // Icon row - hiển thị nâng cấp đã chọn
            var rowGo = new GameObject("IconRow");
            rowGo.transform.SetParent(panelGo.transform, false);
            var rowRt = rowGo.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.08f, 0.52f);
            rowRt.anchorMax = new Vector2(0.92f, 0.52f);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.sizeDelta = new Vector2(0f, 90f);
            rowRt.anchoredPosition = new Vector2(0f, 10f);
            _upgradeRowGo = rowGo;

            var h = rowGo.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 18f;
            h.childForceExpandHeight = false;
            h.childForceExpandWidth = false;

            _upgradeSlots.Clear();
            for (int i = 0; i < 9; i++)
            {
                var slotStroke = NewUiImage($"SlotStroke_{i}", rowGo.transform, panelStrokeColor);
                var slotStrokeRt = slotStroke.GetComponent<RectTransform>();
                slotStrokeRt.sizeDelta = new Vector2(66f, 66f);

                var slot = NewUiImage($"Slot_{i}", slotStroke.transform, new Color(0.08f, 0.09f, 0.10f, 1f));
                var slotRt = slot.GetComponent<RectTransform>();
                slotRt.anchorMin = new Vector2(0.5f, 0.5f);
                slotRt.anchorMax = new Vector2(0.5f, 0.5f);
                slotRt.pivot = new Vector2(0.5f, 0.5f);
                slotRt.sizeDelta = new Vector2(58f, 58f);
                slotRt.anchoredPosition = Vector2.zero;

                // Mặc định hiển thị "+"
                var isStar = i == 0;
                var iconSprite = isStar ? slotStarSprite : slotPlusSprite;
                if (iconSprite != null)
                {
                    var iconGo = new GameObject($"SlotIcon_{i}");
                    iconGo.transform.SetParent(slot.transform, false);
                    var icon = iconGo.AddComponent<Image>();
                    icon.sprite = iconSprite;
                    icon.preserveAspect = true;
                    icon.color = Color.white;
                    var rt = icon.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(34f, 34f);
                    rt.anchoredPosition = Vector2.zero;
                }
                else
                {
                    var symbol = isStar ? "★" : "+";
                    var t = NewTmpText($"SlotText_{i}", slot.transform, symbol, 40, new Color(0.95f, 0.95f, 0.95f, 0.85f));
                    t.alignment = TextAlignmentOptions.Center;
                    StretchToFull(t.rectTransform);
                }

                _upgradeSlots.Add(slot);
            }

            // Bottom buttons bar
            var buttonsGo = new GameObject("Buttons");
            buttonsGo.transform.SetParent(panelGo.transform, false);
            var buttonsRt = buttonsGo.AddComponent<RectTransform>();
            buttonsRt.anchorMin = new Vector2(0.12f, 0.10f);
            buttonsRt.anchorMax = new Vector2(0.88f, 0.10f);
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

            void Apply(float v)
            {
                if (audioMixer != null && !string.IsNullOrWhiteSpace(mixerParam))
                    audioMixer.SetFloat(mixerParam, Mathf.Log10(Mathf.Clamp(v, 0.0001f, 1f)) * 20f);
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
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            if (TMP_Settings.defaultFontAsset != null)
                t.font = TMP_Settings.defaultFontAsset;
            return t;
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
