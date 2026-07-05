using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace NWO.UI
{
    /// <summary>
    /// Hiển thị các status effect đang active trên player
    /// Có thể hiện dạng icons hoặc text
    /// </summary>
    public class StatusEffectsUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStatusEffects playerStatusEffects;
        
        [Header("UI Mode")]
        [SerializeField] private DisplayMode displayMode = DisplayMode.IconsAndText;
        
        [Header("Layout")]
        [SerializeField] private Transform effectIconsContainer;
        [SerializeField] private GameObject effectIconPrefab;
        [SerializeField] private float iconSpacing = 5f;
        [SerializeField] private int maxIconsDisplayed = 8;

        [Header("Text Display")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private bool showDuration = true;
        
        [Header("Icon Sprites")]
        [SerializeField] private Sprite slowIcon;
        [SerializeField] private Sprite confusionIcon;
        [SerializeField] private Sprite freezeIcon;
        [SerializeField] private Sprite burnIcon;
        [SerializeField] private Sprite poisonIcon;
        [SerializeField] private Sprite silenceIcon;
        [SerializeField] private Sprite blindIcon;
        [SerializeField] private Sprite stunIcon;
        [SerializeField] private Sprite speedBoostIcon;
        [SerializeField] private Sprite shieldIcon;
        [SerializeField] private Sprite invincibleIcon;
        [SerializeField] private Sprite regenIcon;
        [SerializeField] private Sprite defaultIcon;

        [Header("Colors")]
        [SerializeField] private Color debuffTextColor = new Color(1f, 0.5f, 0.5f);
        [SerializeField] private Color buffTextColor = new Color(0.5f, 1f, 0.5f);

        public enum DisplayMode
        {
            IconsOnly,
            TextOnly,
            IconsAndText
        }

        private Dictionary<StatusEffectType, StatusEffectIconUI> _activeIcons = new Dictionary<StatusEffectType, StatusEffectIconUI>();
        private List<StatusEffectType> _displayedEffects = new List<StatusEffectType>();

        private void Awake()
        {
            // Auto-find player status effects nếu chưa assign
            if (playerStatusEffects == null)
            {
                var player = FindFirstObjectByType<PlayerController2D>();
                if (player != null)
                {
                    playerStatusEffects = player.GetComponent<PlayerStatusEffects>();
                }
            }
        }

        private void Start()
        {
            if (playerStatusEffects != null)
            {
                playerStatusEffects.OnEffectApplied += OnEffectApplied;
                playerStatusEffects.OnEffectExpired += OnEffectExpired;
            }
            else
            {
                Debug.LogWarning("[StatusEffectsUI] PlayerStatusEffects not found!");
            }
            
            // Clear initial UI
            ClearAllIcons();
            UpdateTextDisplay();
        }

        private void OnDestroy()
        {
            if (playerStatusEffects != null)
            {
                playerStatusEffects.OnEffectApplied -= OnEffectApplied;
                playerStatusEffects.OnEffectExpired -= OnEffectExpired;
            }
        }

        private void Update()
        {
            UpdateIconsProgress();
            UpdateTextDisplay();
        }

        private void OnEffectApplied(StatusEffectType type, float duration)
        {
            if (displayMode != DisplayMode.TextOnly)
            {
                ShowEffectIcon(type, duration);
            }
        }

        private void OnEffectExpired(StatusEffectType type)
        {
            if (displayMode != DisplayMode.TextOnly)
            {
                HideEffectIcon(type);
            }
        }

        private void ShowEffectIcon(StatusEffectType type, float duration)
        {
            if (effectIconsContainer == null || effectIconPrefab == null) return;
            
            // Nếu đã có icon cho effect này, chỉ cần refresh
            if (_activeIcons.ContainsKey(type))
            {
                _activeIcons[type].RefreshDuration(duration);
                return;
            }

            // Giới hạn số icon
            if (_activeIcons.Count >= maxIconsDisplayed) return;

            // Tạo icon mới
            var iconObj = Instantiate(effectIconPrefab, effectIconsContainer);
            var iconUI = iconObj.GetComponent<StatusEffectIconUI>();
            
            if (iconUI == null)
            {
                iconUI = iconObj.AddComponent<StatusEffectIconUI>();
            }

            iconUI.Setup(type, GetIconForEffect(type), duration, IsDebuff(type));
            _activeIcons[type] = iconUI;
            _displayedEffects.Add(type);
            
            RefreshIconLayout();
        }

        private void HideEffectIcon(StatusEffectType type)
        {
            if (_activeIcons.ContainsKey(type))
            {
                var icon = _activeIcons[type];
                _activeIcons.Remove(type);
                _displayedEffects.Remove(type);
                
                if (icon != null && icon.gameObject != null)
                {
                    Destroy(icon.gameObject);
                }
                
                RefreshIconLayout();
            }
        }

        private void ClearAllIcons()
        {
            foreach (var icon in _activeIcons.Values)
            {
                if (icon != null && icon.gameObject != null)
                {
                    Destroy(icon.gameObject);
                }
            }
            _activeIcons.Clear();
            _displayedEffects.Clear();
        }

        private void UpdateIconsProgress()
        {
            if (playerStatusEffects == null) return;

            foreach (var kvp in _activeIcons)
            {
                float remaining = playerStatusEffects.GetRemainingTime(kvp.Key);
                kvp.Value.UpdateProgress(remaining);
            }
        }

        private void RefreshIconLayout()
        {
            if (effectIconsContainer == null) return;

            // Layout icons horizontally
            float currentX = 0f;
            foreach (var type in _displayedEffects)
            {
                if (_activeIcons.TryGetValue(type, out var icon) && icon != null)
                {
                    var rect = icon.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchoredPosition = new Vector2(currentX, 0f);
                        currentX += rect.sizeDelta.x + iconSpacing;
                    }
                }
            }
        }

        private void UpdateTextDisplay()
        {
            if (statusText == null || displayMode == DisplayMode.IconsOnly) return;
            if (playerStatusEffects == null) return;

            var effects = playerStatusEffects.ActiveEffects;
            
            if (effects.Count == 0)
            {
                statusText.text = "";
                return;
            }

            var sb = new System.Text.StringBuilder();
            
            foreach (var effect in effects)
            {
                string colorHex = IsDebuff(effect.Type) 
                    ? ColorUtility.ToHtmlStringRGB(debuffTextColor)
                    : ColorUtility.ToHtmlStringRGB(buffTextColor);
                
                string effectName = GetEffectDisplayName(effect.Type);
                
                if (showDuration)
                {
                    sb.AppendLine($"<color=#{colorHex}>{effectName}</color> ({effect.RemainingTime:F1}s)");
                }
                else
                {
                    sb.AppendLine($"<color=#{colorHex}>{effectName}</color>");
                }
            }

            statusText.text = sb.ToString().TrimEnd();
        }

        private Sprite GetIconForEffect(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Slow => slowIcon ?? defaultIcon,
                StatusEffectType.Confusion => confusionIcon ?? defaultIcon,
                StatusEffectType.Freeze => freezeIcon ?? defaultIcon,
                StatusEffectType.Burn => burnIcon ?? defaultIcon,
                StatusEffectType.Poison => poisonIcon ?? defaultIcon,
                StatusEffectType.Silence => silenceIcon ?? defaultIcon,
                StatusEffectType.Blind => blindIcon ?? defaultIcon,
                StatusEffectType.Stun => stunIcon ?? defaultIcon,
                StatusEffectType.SpeedBoost => speedBoostIcon ?? defaultIcon,
                StatusEffectType.Shield => shieldIcon ?? defaultIcon,
                StatusEffectType.Invincible => invincibleIcon ?? defaultIcon,
                StatusEffectType.Regeneration => regenIcon ?? defaultIcon,
                _ => defaultIcon
            };
        }

        private string GetEffectDisplayName(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Slow => "Chậm",
                StatusEffectType.Confusion => "Loạn Hướng",
                StatusEffectType.Freeze => "Đóng Băng",
                StatusEffectType.Burn => "Bỏng",
                StatusEffectType.Poison => "Độc",
                StatusEffectType.Silence => "Câm Lặng",
                StatusEffectType.Blind => "Mù",
                StatusEffectType.Slippery => "Trượt",
                StatusEffectType.Stun => "Choáng",
                StatusEffectType.WeakKnees => "Yếu Chân",
                StatusEffectType.SpeedBoost => "Tăng Tốc",
                StatusEffectType.Shield => "Khiên",
                StatusEffectType.Invincible => "Bất Tử",
                StatusEffectType.Regeneration => "Hồi Phục",
                StatusEffectType.StaminaBoost => "Hồi Thể Lực",
                StatusEffectType.DamageBoost => "Tăng Sát Thương",
                StatusEffectType.LightAura => "Hào Quang",
                _ => type.ToString()
            };
        }

        private bool IsDebuff(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Slow => true,
                StatusEffectType.Confusion => true,
                StatusEffectType.Freeze => true,
                StatusEffectType.Burn => true,
                StatusEffectType.Poison => true,
                StatusEffectType.Silence => true,
                StatusEffectType.Blind => true,
                StatusEffectType.Slippery => true,
                StatusEffectType.Stun => true,
                StatusEffectType.WeakKnees => true,
                _ => false
            };
        }
    }

    /// <summary>
    /// Component cho mỗi icon status effect riêng lẻ
    /// </summary>
    public class StatusEffectIconUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image progressFill;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI durationText;

        private StatusEffectType _effectType;
        private float _totalDuration;
        private bool _isDebuff;
        private Color _debuffBorderColor = new Color(1f, 0.3f, 0.3f);
        private Color _buffBorderColor = new Color(0.3f, 1f, 0.5f);

        public void Setup(StatusEffectType type, Sprite icon, float duration, bool isDebuff)
        {
            _effectType = type;
            _totalDuration = duration;
            _isDebuff = isDebuff;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
            }

            if (borderImage != null)
            {
                borderImage.color = isDebuff ? _debuffBorderColor : _buffBorderColor;
            }

            UpdateProgress(duration);
        }

        public void RefreshDuration(float newDuration)
        {
            _totalDuration = newDuration;
        }

        public void UpdateProgress(float remainingTime)
        {
            if (progressFill != null && _totalDuration > 0)
            {
                progressFill.fillAmount = remainingTime / _totalDuration;
            }

            if (durationText != null)
            {
                if (remainingTime > 0)
                {
                    durationText.text = remainingTime >= 10 
                        ? Mathf.CeilToInt(remainingTime).ToString()
                        : remainingTime.ToString("F1");
                }
                else
                {
                    durationText.text = "";
                }
            }
        }
    }
}
