using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NWO
{
    /// <summary>
    /// Individual Spell Hotbar Slot UI
    /// </summary>
    public sealed class SpellHotbarSlot : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image selectionBorder;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private TextMeshProUGUI keyBindText;
        [SerializeField] private TextMeshProUGUI spellNameText;

        [Header("Visual Settings")]
        [SerializeField] private Color normalBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color selectedBackgroundColor = new Color(0.4f, 0.6f, 1f, 1f);
        [SerializeField] private Color normalBorderColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color selectedBorderColor = new Color(1f, 1f, 0f, 1f);
        [SerializeField] private Color cooldownColor = new Color(0f, 0f, 0f, 0.7f);

        private int _slotIndex;
        private bool _isSelected;

        /// <summary>
        /// Khởi tạo slot với thông tin spell
        /// </summary>
        public void Initialize(int slotIndex, Sprite icon, string keyBind, string spellName)
        {
            _slotIndex = slotIndex;

            // === FIX RENDER ORDER ===
            // Unity UI renders children in sibling order: later = on top.
            // SelectionBorder phải render DƯỚI Icon, CooldownOverlay render TRÊN Icon.
            // Thứ tự đúng: SelectionBorder(0) → Icon(1) → CooldownOverlay(2) → Text(3,4)
            if (selectionBorder != null) selectionBorder.transform.SetAsFirstSibling();
            if (iconImage != null) iconImage.transform.SetSiblingIndex(1);
            if (cooldownOverlay != null) cooldownOverlay.transform.SetSiblingIndex(2);

            if (iconImage != null)
            {
                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.enabled = true;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                }
                else
                {
                    iconImage.enabled = false;
                }
            }

            // Filled type for cooldown fillAmount
            if (cooldownOverlay != null)
            {
                cooldownOverlay.type = Image.Type.Filled;
                cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
                cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
                cooldownOverlay.fillClockwise = false;
            }

            if (keyBindText != null)
            {
                keyBindText.text = keyBind;
            }

            if (spellNameText != null)
            {
                spellNameText.text = spellName;
            }

            // Set initial state
            SetSelected(false);
            SetCooldown(0f);
        }

        /// <summary>
        /// Highlight slot khi được chọn
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            if (backgroundImage != null)
            {
                backgroundImage.color = selected ? selectedBackgroundColor : normalBackgroundColor;
            }

            if (selectionBorder != null)
            {
                selectionBorder.color = selected ? selectedBorderColor : normalBorderColor;
                selectionBorder.enabled = true; // Always show border, change color
            }
        }

        /// <summary>
        /// Hiển thị cooldown overlay (0 = ready, 1 = full cooldown)
        /// </summary>
        public void SetCooldown(float cooldownPercent)
        {
            if (cooldownOverlay != null)
            {
                if (cooldownPercent > 0f)
                {
                    cooldownOverlay.enabled = true;
                    cooldownOverlay.color = cooldownColor;
                    cooldownOverlay.fillAmount = cooldownPercent;
                }
                else
                {
                    cooldownOverlay.enabled = false;
                }
            }
        }

        /// <summary>
        /// Pulse animation khi spell available
        /// </summary>
        public void PlayAvailablePulse()
        {
            // TODO: Add subtle pulse animation when spell becomes available
            // Can use DOTween or simple coroutine
        }
    }
}
