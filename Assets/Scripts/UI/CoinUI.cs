using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NWO
{
    /// <summary>
    /// Hiển thị số coin dưới thanh stamina (góc trên bên trái).
    /// Tự động tạo Canvas + UI elements bằng code.
    /// Gán coinIconSprite trong Inspector để thay icon coin.
    /// </summary>
    public sealed class CoinUI : MonoBehaviour
    {
        [Header("Coin Icon")]
        [Tooltip("Kéo sprite coin vào đây để thay icon (vd: coin_anim_f0 từ Assets/Art/Boss/)")]
        [SerializeField] private Sprite coinIconSprite;

        [Header("Display")]
        [SerializeField] private Color textColor = new Color(1f, 0.9f, 0.3f, 1f);
        [SerializeField] private float fontSize = 24f;

        [Header("Position (dưới stamina bar)")]
        [Tooltip("Offset X từ mép trái")]
        [SerializeField] private float offsetX = 20f;
        [Tooltip("Offset Y từ mép trên (giá trị âm = xuống dưới)")]
        [SerializeField] private float offsetY = -130f;

        private TextMeshProUGUI _coinText;
        private GameObject _root;
        private Image _coinIcon;

        private void Start()
        {
            BuildUI();

            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.OnCoinsChanged += UpdateDisplay;
                UpdateDisplay(CoinManager.Instance.CurrentCoins);
            }
        }

        private void OnEnable()
        {
            if (CoinManager.Instance != null)
                CoinManager.Instance.OnCoinsChanged += UpdateDisplay;
        }

        private void OnDisable()
        {
            if (CoinManager.Instance != null)
                CoinManager.Instance.OnCoinsChanged -= UpdateDisplay;
        }

        private void OnDestroy()
        {
            if (CoinManager.Instance != null)
                CoinManager.Instance.OnCoinsChanged -= UpdateDisplay;
        }

        private void UpdateDisplay(int coins)
        {
            if (_coinText != null)
                _coinText.text = coins.ToString();
        }

        /// <summary>
        /// Thay đổi icon coin lúc runtime.
        /// </summary>
        public void SetCoinIcon(Sprite sprite)
        {
            coinIconSprite = sprite;
            if (_coinIcon != null && sprite != null)
            {
                _coinIcon.sprite = sprite;
                _coinIcon.color = Color.white;
            }
        }

        private void BuildUI()
        {
            if (_root != null) return;

            _root = new GameObject("CoinDisplay_UI");
            _root.transform.SetParent(transform, false);

            // Canvas
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 29000;

            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Container - góc trên trái, dưới stamina bar
            var container = new GameObject("CoinContainer");
            container.transform.SetParent(_root.transform, false);
            var containerRt = container.AddComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0f, 1f);
            containerRt.anchorMax = new Vector2(0f, 1f);
            containerRt.pivot = new Vector2(0f, 1f);
            containerRt.anchoredPosition = new Vector2(offsetX, offsetY);
            containerRt.sizeDelta = new Vector2(160f, 40f);

            // Horizontal layout: [icon] [number]
            var hLayout = container.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.spacing = 6f;
            hLayout.padding = new RectOffset(4, 8, 2, 2);
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            // Coin icon
            var iconGo = new GameObject("CoinIcon");
            iconGo.transform.SetParent(container.transform, false);
            _coinIcon = iconGo.AddComponent<Image>();
            _coinIcon.raycastTarget = false;
            _coinIcon.preserveAspect = true;

            if (coinIconSprite != null)
            {
                _coinIcon.sprite = coinIconSprite;
                _coinIcon.color = Color.white;
            }
            else
            {
                // Fallback: vòng tròn vàng nếu chưa gán sprite
                _coinIcon.color = textColor;
            }

            var iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 28f;
            iconLe.preferredHeight = 28f;

            // Coin text
            var textGo = new GameObject("CoinText");
            textGo.transform.SetParent(container.transform, false);
            _coinText = textGo.AddComponent<TextMeshProUGUI>();
            _coinText.text = "0";
            _coinText.fontSize = fontSize;
            _coinText.color = textColor;
            _coinText.fontStyle = FontStyles.Bold;
            _coinText.alignment = TextAlignmentOptions.MidlineLeft;
            _coinText.raycastTarget = false;
            _coinText.enableWordWrapping = false;
            if (TMP_Settings.defaultFontAsset != null)
                _coinText.font = TMP_Settings.defaultFontAsset;

            // Outline cho dễ đọc trên nền tối
            _coinText.outlineWidth = 0.2f;
            _coinText.outlineColor = new Color32(0, 0, 0, 200);

            var textLe = textGo.AddComponent<LayoutElement>();
            textLe.preferredWidth = 80f;
            textLe.preferredHeight = 32f;
        }
    }
}
