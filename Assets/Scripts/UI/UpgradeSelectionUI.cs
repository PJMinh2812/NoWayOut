using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace NWO
{
    /// <summary>
    /// UI hiển thị 3 thẻ nâng cấp sau khi đánh xong quái ở gate.
    /// Tự động build UI bằng code (không cần prefab).
    /// Pause game khi hiện, resume khi chọn xong.
    /// </summary>
    public sealed class UpgradeSelectionUI : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color overlayColor = new(0f, 0f, 0f, 0.70f);
        [SerializeField] private Color cardBorderColor = new(0.86f, 0.78f, 0.60f, 1f);
        [SerializeField] private Color cardBgColor = new(0.14f, 0.16f, 0.20f, 0.95f);
        [SerializeField] private Color titleColor = new(0.98f, 0.96f, 0.92f, 1f);
        [SerializeField] private Color descColor = new(0.80f, 0.78f, 0.74f, 1f);
        [SerializeField] private Color valueColor = new(0.40f, 1f, 0.60f, 1f);
        [SerializeField] private Color penaltyColor = new(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color ribbonColor = new(0.65f, 0.12f, 0.12f, 1f);
        [SerializeField] private Color rareBorderColor = new(0.3f, 0.5f, 1f, 1f);
        [SerializeField] private Color epicBorderColor = new(0.75f, 0.3f, 1f, 1f);

        [Header("Reroll")]
        [SerializeField] private Color rerollColor = new(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color rerollDisabledColor = new(0.4f, 0.4f, 0.4f, 1f);

        private GameObject _root;
        private bool _isShowing;
        private Transform _cardContainer;
        private TextMeshProUGUI _rerollText;
        private Button _rerollBtn;
        private List<UpgradeData> _currentOptions;

        private void Awake()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnShowUpgradeSelection += ShowSelection;
        }

        private void OnEnable()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnShowUpgradeSelection -= ShowSelection;

            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnShowUpgradeSelection += ShowSelection;
        }

        private void OnDisable()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnShowUpgradeSelection -= ShowSelection;
        }

        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnShowUpgradeSelection -= ShowSelection;
        }

        private void ShowSelection(List<UpgradeData> options)
        {
            if (_isShowing) return;
            _isShowing = true;
            _currentOptions = options;

            // Pause game
            Time.timeScale = 0f;

            BuildUI(options);
        }

        private void OnCardClicked(UpgradeData upgrade)
        {
            if (!_isShowing) return;
            _isShowing = false;

            // Áp dụng nâng cấp
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.SelectUpgrade(upgrade);

            // Destroy UI
            if (_root != null)
                Destroy(_root);
            _root = null;

            // Resume game
            Time.timeScale = 1f;
        }

        private void BuildUI(List<UpgradeData> options)
        {
            if (_root != null)
                Destroy(_root);

            _root = new GameObject("UpgradeSelection_UI");
            _root.transform.SetParent(transform, false);

            // Canvas
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 31000; // Dưới pause menu nhưng trên HUD
            _root.AddComponent<GraphicRaycaster>();

            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Overlay tối
            var overlay = NewImg("Overlay", _root.transform, overlayColor);
            Stretch(overlay.GetComponent<RectTransform>());

            // Title ribbon
            var ribbonGo = NewImg("Ribbon", overlay.transform, ribbonColor);
            var ribbonRt = ribbonGo.GetComponent<RectTransform>();
            ribbonRt.anchorMin = new Vector2(0.5f, 1f);
            ribbonRt.anchorMax = new Vector2(0.5f, 1f);
            ribbonRt.pivot = new Vector2(0.5f, 1f);
            ribbonRt.sizeDelta = new Vector2(600f, 80f);
            ribbonRt.anchoredPosition = new Vector2(0f, -60f);

            var titleTmp = NewText("Title", ribbonGo.transform, "CHỌN NÂNG CẤP", 42, titleColor);
            titleTmp.alignment = TextAlignmentOptions.Center;
            Stretch(titleTmp.rectTransform);

            // Card container
            var container = new GameObject("Cards");
            container.transform.SetParent(overlay.transform, false);
            var containerRt = container.AddComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.pivot = new Vector2(0.5f, 0.5f);
            containerRt.sizeDelta = new Vector2(1100f, 420f);
            containerRt.anchoredPosition = new Vector2(0f, -20f);
            _cardContainer = container.transform;

            var hLayout = container.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.spacing = 40f;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;

            // Build 3 cards
            for (int i = 0; i < options.Count; i++)
            {
                BuildCard(container.transform, options[i]);
            }

            // Reroll button (dưới cards)
            BuildRerollButton(overlay.transform);
        }

        private void BuildRerollButton(Transform parent)
        {
            int cost = CoinManager.Instance != null ? CoinManager.Instance.RerollCost : 3;
            bool canAfford = CoinManager.Instance != null && CoinManager.Instance.CanReroll();

            var btnGo = NewImg("RerollBtn", parent, canAfford ? new Color(0.2f, 0.2f, 0.2f, 0.8f) : new Color(0.15f, 0.15f, 0.15f, 0.6f));
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.sizeDelta = new Vector2(280f, 55f);
            btnRt.anchoredPosition = new Vector2(0f, 40f);

            _rerollBtn = btnGo.AddComponent<Button>();
            _rerollBtn.interactable = canAfford;
            _rerollBtn.onClick.AddListener(OnRerollClicked);

            var colors = _rerollBtn.colors;
            colors.highlightedColor = new Color(1f, 0.9f, 0.3f, 0.2f);
            colors.pressedColor = new Color(1f, 0.9f, 0.3f, 0.1f);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
            _rerollBtn.colors = colors;

            _rerollText = NewText("RerollText", btnGo.transform,
                $"🔄 ĐỔI THẺ ({cost} 🪙)", 24f,
                canAfford ? rerollColor : rerollDisabledColor);
            _rerollText.alignment = TextAlignmentOptions.Center;
            Stretch(_rerollText.rectTransform);
        }

        private void OnRerollClicked()
        {
            if (UpgradeManager.Instance == null) return;

            var newOptions = UpgradeManager.Instance.TryReroll();
            if (newOptions == null) return;

            _currentOptions = newOptions;

            // Xóa cards cũ và build lại
            if (_cardContainer != null)
            {
                for (int i = _cardContainer.childCount - 1; i >= 0; i--)
                    Destroy(_cardContainer.GetChild(i).gameObject);

                for (int i = 0; i < newOptions.Count; i++)
                    BuildCard(_cardContainer, newOptions[i]);
            }

            // Update reroll button
            UpdateRerollButton();
        }

        private void UpdateRerollButton()
        {
            if (_rerollBtn == null || _rerollText == null) return;

            int cost = CoinManager.Instance != null ? CoinManager.Instance.RerollCost : 3;
            bool canAfford = CoinManager.Instance != null && CoinManager.Instance.CanReroll();
            _rerollBtn.interactable = canAfford;
            _rerollText.text = $"🔄 ĐỔI THẺ ({cost} 🪙)";
            _rerollText.color = canAfford ? rerollColor : rerollDisabledColor;
        }

        private void BuildCard(Transform parent, UpgradeData data)
        {
            // Border color based on rarity
            Color borderCol = data.rarity switch
            {
                UpgradeRarity.Rare => rareBorderColor,
                UpgradeRarity.Epic => epicBorderColor,
                _ => cardBorderColor
            };

            var borderGo = NewImg($"Card_{data.upgradeName}", parent, borderCol);
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.sizeDelta = new Vector2(300f, 400f);

            var le = borderGo.AddComponent<LayoutElement>();
            le.preferredWidth = 300f;
            le.preferredHeight = 400f;

            // Card background
            var cardBg = NewImg("CardBg", borderGo.transform, data.cardColor.a > 0 ? data.cardColor : cardBgColor);
            var cardRt = cardBg.GetComponent<RectTransform>();
            Stretch(cardRt);
            cardRt.offsetMin = new Vector2(5f, 5f);
            cardRt.offsetMax = new Vector2(-5f, -5f);

            // Button trên toàn card
            var btn = cardBg.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.08f);
            btn.colors = colors;

            var capturedData = data;
            btn.onClick.AddListener(() => OnCardClicked(capturedData));

            // Rarity label (góc trên phải)
            if (data.rarity != UpgradeRarity.Common)
            {
                string rarityText = data.rarity == UpgradeRarity.Rare ? "RARE" : "EPIC";
                Color rarityCol = data.rarity == UpgradeRarity.Rare ? rareBorderColor : epicBorderColor;
                var rarityTmp = NewText("Rarity", cardBg.transform, rarityText, 16, rarityCol);
                rarityTmp.alignment = TextAlignmentOptions.Right;
                rarityTmp.fontStyle = FontStyles.Bold;
                var rarRt = rarityTmp.rectTransform;
                rarRt.anchorMin = new Vector2(0.55f, 0.92f);
                rarRt.anchorMax = new Vector2(0.95f, 1f);
                rarRt.offsetMin = Vector2.zero;
                rarRt.offsetMax = Vector2.zero;
            }

            // Glyph/Icon lớn
            if (data.icon != null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(cardBg.transform, false);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = data.icon;
                iconImg.preserveAspect = true;
                iconImg.color = Color.white;
                iconImg.raycastTarget = false;
                var iconRt = iconImg.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 0.70f);
                iconRt.anchorMax = new Vector2(0.5f, 0.70f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.sizeDelta = new Vector2(80f, 80f);
                iconRt.anchoredPosition = Vector2.zero;
            }
            else
            {
                var glyph = NewText("Glyph", cardBg.transform, data.glyphSymbol, 72, data.glyphColor);
                glyph.alignment = TextAlignmentOptions.Center;
                var glyphRt = glyph.rectTransform;
                glyphRt.anchorMin = new Vector2(0.1f, 0.58f);
                glyphRt.anchorMax = new Vector2(0.9f, 0.92f);
                glyphRt.offsetMin = Vector2.zero;
                glyphRt.offsetMax = Vector2.zero;
            }

            // Tên nâng cấp
            var nameTmp = NewText("Name", cardBg.transform, data.upgradeName, 26, titleColor);
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.fontStyle = FontStyles.Bold;
            var nameRt = nameTmp.rectTransform;
            nameRt.anchorMin = new Vector2(0.05f, 0.42f);
            nameRt.anchorMax = new Vector2(0.95f, 0.56f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;

            // Buff value (xanh)
            string buffStr = $"+{data.value:G} {GetStatShortName(data.upgradeType)}";
            var buffTmp = NewText("Buff", cardBg.transform, buffStr, 22, valueColor);
            buffTmp.alignment = TextAlignmentOptions.Center;
            buffTmp.fontStyle = FontStyles.Bold;
            var buffRt = buffTmp.rectTransform;
            buffRt.anchorMin = new Vector2(0.05f, 0.28f);
            buffRt.anchorMax = new Vector2(0.95f, 0.40f);
            buffRt.offsetMin = Vector2.zero;
            buffRt.offsetMax = Vector2.zero;

            // Penalty value (đỏ) - chỉ hiện nếu có trade-off
            if (data.hasPenalty && data.penaltyValue > 0f)
            {
                string penStr = $"-{data.penaltyValue:G} {GetStatShortName(data.penaltyType)}";
                var penTmp = NewText("Penalty", cardBg.transform, penStr, 20, penaltyColor);
                penTmp.alignment = TextAlignmentOptions.Center;
                penTmp.fontStyle = FontStyles.Bold;
                var penRt = penTmp.rectTransform;
                penRt.anchorMin = new Vector2(0.05f, 0.17f);
                penRt.anchorMax = new Vector2(0.95f, 0.28f);
                penRt.offsetMin = Vector2.zero;
                penRt.offsetMax = Vector2.zero;
            }

            // Mô tả ngắn (dưới cùng)
            var descTmp = NewText("Desc", cardBg.transform, data.description, 16, descColor);
            descTmp.alignment = TextAlignmentOptions.Center;
            descTmp.textWrappingMode = TextWrappingModes.Normal;
            var descRt = descTmp.rectTransform;
            descRt.anchorMin = new Vector2(0.06f, 0.02f);
            descRt.anchorMax = new Vector2(0.94f, 0.18f);
            descRt.offsetMin = Vector2.zero;
            descRt.offsetMax = Vector2.zero;
        }

        private static string GetStatShortName(UpgradeType type) => type switch
        {
            UpgradeType.MaxHealth => "HP",
            UpgradeType.HealthRegen => "HP/s",
            UpgradeType.MaxStamina => "Stamina",
            UpgradeType.StaminaRegen => "Stamina/s",
            UpgradeType.MoveSpeed => "Speed",
            UpgradeType.DashSpeed => "Dash",
            UpgradeType.InvincibleDuration => "i-Frame",
            UpgradeType.AllSpellDamage => "Spell DMG",
            UpgradeType.AllSpellRange => "Spell Range",
            UpgradeType.AllSpellCooldown => "Spell CD",
            UpgradeType.Spell01Damage => "Spell 1 DMG",
            UpgradeType.Spell02Damage => "Spell 2 DMG",
            UpgradeType.Spell03Damage => "Spell 3 DMG",
            UpgradeType.Spell01Cooldown => "Spell 1 CD",
            UpgradeType.Spell02Cooldown => "Spell 2 CD",
            UpgradeType.Spell03Cooldown => "Spell 3 CD",
            UpgradeType.FireRate => "Fire Rate",
            UpgradeType.MeleeDamage => "Melee DMG",
            UpgradeType.AttackRange => "Range",
            UpgradeType.KnockbackForce => "Knockback",
            _ => type.ToString()
        };

        // ---- Helpers giống PauseMenuUI ----

        private static GameObject NewImg(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, string text, float fontSize, Color color)
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

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
