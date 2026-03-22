using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace NWO
{
    /// <summary>
    /// Controls Game Over screen UI
    /// </summary>
    public sealed class GameOverUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private TextMeshProUGUI gameOverText;

        [Header("Auto UI")]
        [SerializeField] private bool autoBuildUiIfMissing = true;
        [SerializeField] private bool autoReplaceExistingUi = true;
        [SerializeField] private Color overlayColor = new(0f, 0f, 0f, 0.62f);
        [SerializeField] private Color panelColor = new(0.12f, 0.14f, 0.16f, 0.95f);
        [SerializeField] private Color panelStrokeColor = new(0.86f, 0.78f, 0.60f, 1f);
        [SerializeField] private Color ribbonColor = new(0.65f, 0.12f, 0.12f, 1f);
        [SerializeField] private Color buttonColor = new(0.60f, 0.44f, 0.23f, 1f);
        [SerializeField] private Color buttonStrokeColor = new(0.86f, 0.78f, 0.60f, 1f);

        [Header("Button Sprites (optional)")]
        [SerializeField] private Sprite restartButtonSprite;
        [SerializeField] private Sprite homeButtonSprite;
        [SerializeField] private Sprite quitButtonSprite;

        [Header("Messages")]
        [SerializeField] private string subtitleMessage = "Bạn đã gục ngã trong bóng tối";
        [SerializeField] private string hintMessage = "Chọn một nút để tiếp tục";

        [Header("Flow")]
        [SerializeField, Min(0f)] private float showDelay = 0.08f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.18f;
        [SerializeField, Min(0.01f)] private float panelPopDuration = 0.16f;
        [SerializeField, Min(0f)] private float buttonUnlockDelay = 0.05f;
        [SerializeField] private bool showOnEnableForPreview = false;

        [Header("SFX")]
        [SerializeField] private AudioClip gameOverSfx;
        [SerializeField] private AudioClip buttonClickSfx;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        [Header("Settings")]
        [SerializeField] private string gameOverMessage = "GAME OVER";
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private AudioSource _audioSource;
        private bool _listenersBound;
        private bool _actionTaken;
        private bool _buttonsUnlocked;
        private bool _isShowing;
        private Coroutine _showRoutine;
        private bool _autoUiBuilt;

        private CanvasGroup _canvasGroup;
        private RectTransform _panelRect;
        private TextMeshProUGUI _subtitleText;
        private TextMeshProUGUI _hintText;

        private void Awake()
        {
            BuildUiIfNeeded();
            EnsureAudioSource();
            BindButtonListeners();
            ApplyTexts();

            if (gameOverPanel == null)
                gameOverPanel = gameObject;

            EnsureVisualReferences();
        }

        private void OnEnable()
        {
            BuildUiIfNeeded();
            EnsureAudioSource();
            BindButtonListeners();
            ApplyTexts();
            EnsureVisualReferences();

            bool shouldShow = showOnEnableForPreview || (GameManager.Instance != null && GameManager.Instance.IsGameOver);
            if (shouldShow)
            {
                StartShowSequence();
            }
            else
            {
                HideVisualState();
                SetButtonsInteractable(false);
                _isShowing = false;
                _buttonsUnlocked = false;
                _actionTaken = false;
            }
        }

        private void BuildUiIfNeeded()
        {
            if (autoReplaceExistingUi)
            {
                if (!_autoUiBuilt)
                {
                    BuildAutoUi();
                    _autoUiBuilt = true;
                }
                return;
            }

            if (autoBuildUiIfMissing && (restartButton == null || mainMenuButton == null || exitButton == null || gameOverText == null))
            {
                BuildAutoUi();
                _autoUiBuilt = true;
            }
        }

        private void BuildAutoUi()
        {
            _listenersBound = false;

            // Clear old generated root if it exists
            var existing = transform.Find("GameOver_Auto");
            if (existing != null)
                Destroy(existing.gameObject);

            // Hide legacy children to avoid duplicate UI
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(false);

            var root = new GameObject("GameOver_Auto");
            root.transform.SetParent(transform, false);
            root.layer = gameObject.layer;
            root.SetActive(true);

            var rootRt = root.AddComponent<RectTransform>();
            StretchToFull(rootRt);

            gameOverPanel = root;
            _canvasGroup = root.AddComponent<CanvasGroup>();

            var overlay = NewUiImage("Overlay", root.transform, overlayColor);
            StretchToFull(overlay.GetComponent<RectTransform>());

            var panelStroke = NewUiImage("PanelStroke", overlay.transform, panelStrokeColor);
            var panelStrokeRt = panelStroke.GetComponent<RectTransform>();
            panelStrokeRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelStrokeRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelStrokeRt.pivot = new Vector2(0.5f, 0.5f);
            panelStrokeRt.sizeDelta = new Vector2(980f, 380f);
            panelStrokeRt.anchoredPosition = new Vector2(0f, 20f);
            _panelRect = panelStrokeRt;

            var panel = NewUiImage("Panel", panelStroke.transform, panelColor);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(960f, 360f);
            panelRt.anchoredPosition = Vector2.zero;

            var ribbonStroke = NewUiImage("RibbonStroke", panelStroke.transform, panelStrokeColor);
            var ribbonStrokeRt = ribbonStroke.GetComponent<RectTransform>();
            ribbonStrokeRt.anchorMin = new Vector2(0.5f, 1f);
            ribbonStrokeRt.anchorMax = new Vector2(0.5f, 1f);
            ribbonStrokeRt.pivot = new Vector2(0.5f, 0.5f);
            ribbonStrokeRt.sizeDelta = new Vector2(480f, 90f);
            ribbonStrokeRt.anchoredPosition = new Vector2(0f, -24f);

            var ribbon = NewUiImage("Ribbon", ribbonStroke.transform, ribbonColor);
            var ribbonRt = ribbon.GetComponent<RectTransform>();
            ribbonRt.anchorMin = new Vector2(0.5f, 0.5f);
            ribbonRt.anchorMax = new Vector2(0.5f, 0.5f);
            ribbonRt.pivot = new Vector2(0.5f, 0.5f);
            ribbonRt.sizeDelta = new Vector2(464f, 74f);
            ribbonRt.anchoredPosition = Vector2.zero;

            gameOverText = NewTmpText("Title", ribbon.transform, gameOverMessage, 56, new Color(0.98f, 0.96f, 0.92f, 1f));
            gameOverText.alignment = TextAlignmentOptions.Center;
            StretchToFull(gameOverText.rectTransform);

            _subtitleText = NewTmpText("Subtitle", panel.transform, subtitleMessage, 30, new Color(0.86f, 0.87f, 0.89f, 0.96f));
            _subtitleText.alignment = TextAlignmentOptions.Center;
            var subtitleRt = _subtitleText.rectTransform;
            subtitleRt.anchorMin = new Vector2(0.1f, 0.60f);
            subtitleRt.anchorMax = new Vector2(0.9f, 0.60f);
            subtitleRt.pivot = new Vector2(0.5f, 0.5f);
            subtitleRt.sizeDelta = new Vector2(0f, 60f);
            subtitleRt.anchoredPosition = Vector2.zero;

            _hintText = NewTmpText("Hint", panel.transform, hintMessage, 24, new Color(0.80f, 0.84f, 0.88f, 0.95f));
            _hintText.alignment = TextAlignmentOptions.Center;
            var hintRt = _hintText.rectTransform;
            hintRt.anchorMin = new Vector2(0.15f, 0.45f);
            hintRt.anchorMax = new Vector2(0.85f, 0.45f);
            hintRt.pivot = new Vector2(0.5f, 0.5f);
            hintRt.sizeDelta = new Vector2(0f, 48f);
            hintRt.anchoredPosition = Vector2.zero;

            var buttonsGo = new GameObject("Buttons");
            buttonsGo.transform.SetParent(panel.transform, false);
            var buttonsRt = buttonsGo.AddComponent<RectTransform>();
            buttonsRt.anchorMin = new Vector2(0.10f, 0.12f);
            buttonsRt.anchorMax = new Vector2(0.90f, 0.12f);
            buttonsRt.pivot = new Vector2(0.5f, 0f);
            buttonsRt.sizeDelta = new Vector2(0f, 92f);
            buttonsRt.anchoredPosition = Vector2.zero;

            var h = buttonsGo.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.LowerCenter;
            h.spacing = 26f;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = false;

            restartButton = NewBigButton(buttonsGo.transform, "RESTART", "↻", restartButtonSprite);
            mainMenuButton = NewBigButton(buttonsGo.transform, "HOME", "⌂", homeButtonSprite);
            exitButton = NewBigButton(buttonsGo.transform, "QUIT", "✕", quitButtonSprite);
        }

        private IEnumerator ShowSequence()
        {
            _isShowing = true;
            _buttonsUnlocked = false;
            _actionTaken = false;

            SetButtonsInteractable(false);
            ResetVisualState();

            if (showDelay > 0f)
                yield return new WaitForSecondsRealtime(showDelay);

            PlaySfx(gameOverSfx);

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / fadeDuration);

                if (_canvasGroup != null)
                    _canvasGroup.alpha = n;

                if (_panelRect != null)
                {
                    float popN = Mathf.Clamp01(t / Mathf.Max(0.01f, panelPopDuration));
                    float eased = 1f - Mathf.Pow(1f - popN, 3f);
                    _panelRect.localScale = Vector3.LerpUnclamped(new Vector3(0.92f, 0.92f, 1f), Vector3.one, eased);
                }

                yield return null;
            }

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;

            if (_panelRect != null)
                _panelRect.localScale = Vector3.one;

            if (buttonUnlockDelay > 0f)
                yield return new WaitForSecondsRealtime(buttonUnlockDelay);

            _buttonsUnlocked = true;
            SetButtonsInteractable(true);
        }

        public void ShowGameOver()
        {
            if (gameObject != null && !gameObject.activeSelf)
                gameObject.SetActive(true);

            StartShowSequence();
        }

        public void HideGameOverImmediate()
        {
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }

            _isShowing = false;
            _buttonsUnlocked = false;
            _actionTaken = false;
            SetButtonsInteractable(false);
            HideVisualState();

            if (gameObject != null)
                gameObject.SetActive(false);
        }

        private void StartShowSequence()
        {
            if (_showRoutine != null)
                StopCoroutine(_showRoutine);

            _showRoutine = StartCoroutine(ShowSequence());
        }

        private void HideVisualState()
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            if (_panelRect != null)
                _panelRect.localScale = Vector3.one;
        }

        private void ResetVisualState()
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            if (_panelRect != null)
                _panelRect.localScale = new Vector3(0.92f, 0.92f, 1f);
        }

        private void EnsureVisualReferences()
        {
            if (gameOverPanel == null)
                gameOverPanel = gameObject;

            if (_canvasGroup == null)
                _canvasGroup = gameOverPanel.GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
                _canvasGroup = gameOverPanel.AddComponent<CanvasGroup>();

            if (_panelRect == null)
            {
                var panelStroke = gameOverPanel.transform.Find("GameOver_Auto/Overlay/PanelStroke");
                if (panelStroke != null)
                    _panelRect = panelStroke as RectTransform;
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (restartButton != null)
                restartButton.interactable = interactable;

            if (mainMenuButton != null)
                mainMenuButton.interactable = interactable;

            if (exitButton != null)
                exitButton.interactable = interactable;
        }

        private void EnsureAudioSource()
        {
            if (_audioSource != null) return;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f;
            _audioSource.volume = Mathf.Clamp01(sfxVolume);
        }

        private void BindButtonListeners()
        {
            if (_listenersBound) return;

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);

            _listenersBound = true;
        }

        private void ApplyTexts()
        {
            if (gameOverText != null)
                gameOverText.text = gameOverMessage;

            if (_subtitleText != null)
                _subtitleText.text = subtitleMessage;

            if (_hintText != null)
                _hintText.text = hintMessage;
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null || _audioSource == null)
                return;

            _audioSource.volume = Mathf.Clamp01(sfxVolume);
            _audioSource.PlayOneShot(clip);
        }

        private void OnRestartClicked()
        {
            if (_actionTaken || !_buttonsUnlocked)
                return;

            _actionTaken = true;
            SetButtonsInteractable(false);
            PlaySfx(buttonClickSfx);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }

        private void OnMainMenuClicked()
        {
            if (_actionTaken || !_buttonsUnlocked)
                return;

            _actionTaken = true;
            SetButtonsInteractable(false);
            PlaySfx(buttonClickSfx);

            // Load Main Menu scene
            Time.timeScale = 1f; // Reset time scale in case game was paused
            SceneLoader.LoadScene(mainMenuSceneName);
        }

        private void OnExitClicked()
        {
            if (_actionTaken || !_buttonsUnlocked)
                return;

            _actionTaken = true;
            SetButtonsInteractable(false);
            PlaySfx(buttonClickSfx);

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

        private Button NewBigButton(Transform parent, string label, string glyph, Sprite iconSprite)
        {
            var strokeGo = NewUiImage($"{label}_Stroke", parent, buttonStrokeColor);
            var strokeRt = strokeGo.GetComponent<RectTransform>();
            strokeRt.sizeDelta = new Vector2(0f, 84f);

            var le = strokeGo.AddComponent<LayoutElement>();
            le.preferredHeight = 84f;
            le.minHeight = 84f;

            var bgGo = NewUiImage($"{label}_Bg", strokeGo.transform, buttonColor);
            var bgRt = bgGo.GetComponent<RectTransform>();
            StretchToFull(bgRt);
            bgRt.offsetMin = new Vector2(6f, 6f);
            bgRt.offsetMax = new Vector2(-6f, -6f);

            var btn = bgGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(buttonColor.r * 1.06f, buttonColor.g * 1.06f, buttonColor.b * 1.06f, 1f);
            colors.pressedColor = new Color(buttonColor.r * 0.92f, buttonColor.g * 0.92f, buttonColor.b * 0.92f, 1f);
            btn.colors = colors;

            if (iconSprite != null)
            {
                var iconGo = new GameObject($"{label}_Icon");
                iconGo.transform.SetParent(bgGo.transform, false);
                var icon = iconGo.AddComponent<Image>();
                icon.sprite = iconSprite;
                icon.preserveAspect = true;
                icon.color = Color.white;
                icon.raycastTarget = false;

                var iconRt = icon.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.sizeDelta = new Vector2(48f, 48f);
                iconRt.anchoredPosition = Vector2.zero;
            }
            else
            {
                var txt = NewTmpText($"{label}_Text", bgGo.transform, glyph, 44, new Color(0.20f, 0.12f, 0.06f, 0.95f));
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

        private static void StretchToFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
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

            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }

            _listenersBound = false;
        }
    }
}
