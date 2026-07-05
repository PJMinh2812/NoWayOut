using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NWO
{
    /// <summary>
    /// Individual hotbar slot UI
    /// </summary>
    public sealed class HotbarSlot : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image selectionHighlight;
        [SerializeField] private TextMeshProUGUI countText;

        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        [SerializeField] private Color selectedColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color emptyIconColor = new Color(1f, 1f, 1f, 0.3f);

        private int _slotIndex;
        private Item _currentItem;

        public void Initialize(int slotIndex)
        {
            _slotIndex = slotIndex;
            SetSelected(false);
            SetEmpty();
        }

        /// <summary>
        /// Update slot with item data
        /// </summary>
        public void SetItem(Item item, int count)
        {
            _currentItem = item;

            if (item == null || count <= 0)
            {
                SetEmpty();
                return;
            }

            if (iconImage != null)
            {
                iconImage.sprite = item.icon;
                iconImage.enabled = item.icon != null;
                iconImage.color = Color.white;
            }

            if (countText != null)
            {
                if (count > 1)
                {
                    countText.text = count.ToString();
                    countText.enabled = true;
                }
                else
                {
                    countText.enabled = false;
                }
            }
        }

        /// <summary>
        /// Set slot to empty state
        /// </summary>
        public void SetEmpty()
        {
            _currentItem = null;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (countText != null)
            {
                countText.enabled = false;
            }
        }

        /// <summary>
        /// Highlight slot when selected
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
            {
                selectionHighlight.enabled = selected;
                selectionHighlight.color = selectedColor;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = selected ? selectedColor : normalColor;
            }
        }
    }
}
