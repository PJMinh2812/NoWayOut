using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Spell Hotbar UI - Hiển thị 4 spell slots (0=Idle, 1/2/3=Spells)
    /// Tự động lấy icon từ spell projectile prefabs nếu không gán thủ công.
    /// </summary>
    public sealed class SpellHotbarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSpellController spellController;
        [SerializeField] private SpellHotbarSlot[] spellSlots; // 4 slots: 0, 1, 2, 3

        [Header("Spell Icons (Tự động lấy từ prefab nếu để trống)")]
        [Tooltip("Icon cho Idle state (0) - nếu null sẽ tạo placeholder")]
        [SerializeField] private Sprite idleIcon;
        
        [Tooltip("Icon cho Spell 1 - nếu null sẽ lấy từ Spell01 Projectile prefab")]
        [SerializeField] private Sprite spell01Icon;
        
        [Tooltip("Icon cho Spell 2 - nếu null sẽ lấy từ Spell02 Projectile prefab")]
        [SerializeField] private Sprite spell02Icon;
        
        [Tooltip("Icon cho Spell 3 - nếu null sẽ lấy từ Spell03 Projectile prefab")]
        [SerializeField] private Sprite spell03Icon;

        [Header("Fallback Colors (dùng khi không có icon)")]
        [SerializeField] private Color spell01Color = new Color(0.3f, 0.8f, 1f);   // Xanh dương
        [SerializeField] private Color spell02Color = new Color(1f, 0.5f, 0.2f);   // Cam
        [SerializeField] private Color spell03Color = new Color(0.8f, 0.3f, 1f);   // Tím
        [SerializeField] private Color idleColor   = new Color(1f, 0.9f, 0.2f);    // Vàng

        [Header("Key Bindings Display")]
        [SerializeField] private string[] keyLabels = { "0", "1", "2", "3" };

        private void Awake()
        {
            // Auto-find spell controller nếu chưa gán
            if (spellController == null)
            {
                spellController = FindFirstObjectByType<PlayerSpellController>();
            }

            if (spellController == null)
            {
                Debug.LogError("[SpellHotbarUI] PlayerSpellController not found!");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            if (spellSlots == null || spellSlots.Length == 0)
            {
                Debug.LogError("[SpellHotbarUI] ⚠️ SpellSlots array is EMPTY! Please drag 4 SpellSlot GameObjects into the array in Inspector.");
                enabled = false;
                return;
            }

            // === AUTO-LOAD ICONS từ spell projectile prefabs nếu chưa gán ===
            AutoLoadSpellIcons();
            
            InitializeSlots();
            UpdateSelection(0); // Start with Idle selected
            Debug.Log($"[SpellHotbarUI] ✅ Initialized {spellSlots.Length} spell slots");
        }

        /// <summary>
        /// Tự động lấy icon từ spell projectile prefabs nếu không gán trong Inspector.
        /// Nếu prefab cũng không có sprite, tạo placeholder màu.
        /// </summary>
        private void AutoLoadSpellIcons()
        {
            if (spellController == null) return;

            // Idle icon - tạo placeholder nếu null
            if (idleIcon == null)
            {
                idleIcon = CreateColorSprite(idleColor, "IdleIcon");
                Debug.Log("[SpellHotbarUI] Auto-generated Idle icon (placeholder)");
            }

            // Spell 1
            if (spell01Icon == null)
            {
                spell01Icon = spellController.GetSpellIcon(1);
                if (spell01Icon != null)
                    Debug.Log($"[SpellHotbarUI] Auto-loaded Spell 1 icon from prefab: {spell01Icon.name}");
                else
                {
                    spell01Icon = CreateColorSprite(spell01Color, "Spell01Icon");
                    Debug.Log("[SpellHotbarUI] Auto-generated Spell 1 icon (placeholder)");
                }
            }

            // Spell 2
            if (spell02Icon == null)
            {
                spell02Icon = spellController.GetSpellIcon(2);
                if (spell02Icon != null)
                    Debug.Log($"[SpellHotbarUI] Auto-loaded Spell 2 icon from prefab: {spell02Icon.name}");
                else
                {
                    spell02Icon = CreateColorSprite(spell02Color, "Spell02Icon");
                    Debug.Log("[SpellHotbarUI] Auto-generated Spell 2 icon (placeholder)");
                }
            }

            // Spell 3
            if (spell03Icon == null)
            {
                spell03Icon = spellController.GetSpellIcon(3);
                if (spell03Icon != null)
                    Debug.Log($"[SpellHotbarUI] Auto-loaded Spell 3 icon from prefab: {spell03Icon.name}");
                else
                {
                    spell03Icon = CreateColorSprite(spell03Color, "Spell03Icon");
                    Debug.Log("[SpellHotbarUI] Auto-generated Spell 3 icon (placeholder)");
                }
            }
        }

        /// <summary>
        /// Tạo sprite placeholder màu đơn sắc (16x16)
        /// </summary>
        private static Sprite CreateColorSprite(Color color, string name)
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
            sprite.name = name;
            return sprite;
        }

        private void Update()
        {
            if (spellController == null) return;

            // Update selection khi spell thay đổi
            int currentSpell = spellController.CurrentSpell;
            UpdateSelection(currentSpell);

            // Update cooldown displays
            UpdateCooldowns();
        }

        /// <summary>
        /// Khởi tạo tất cả spell slots
        /// </summary>
        private void InitializeSlots()
        {
            if (spellSlots == null || spellSlots.Length != 4)
            {
                Debug.LogError($"[SpellHotbarUI] ❌ SpellSlots array must have exactly 4 elements! Current: {(spellSlots == null ? "null" : spellSlots.Length.ToString())}");
                return;
            }
            
            Debug.Log("[SpellHotbarUI] Initializing spell slots...");

            // Setup slot 0 - Idle
            if (spellSlots[0] != null)
            {
                spellSlots[0].Initialize(0, idleIcon, keyLabels[0], "Idle");
                Debug.Log($"[SpellHotbarUI] ✅ Slot 0 (Idle) initialized. Icon: {(idleIcon != null ? idleIcon.name : "null")}");
            }
            else
            {
                Debug.LogError("[SpellHotbarUI] ❌ Slot 0 is NULL!");
            }

            // Setup slot 1 - Spell 1
            if (spellSlots[1] != null)
            {
                spellSlots[1].Initialize(1, spell01Icon, keyLabels[1], "Spell 1");
                Debug.Log($"[SpellHotbarUI] ✅ Slot 1 (Spell 1) initialized. Icon: {(spell01Icon != null ? spell01Icon.name : "null")}");
            }
            else
            {
                Debug.LogError("[SpellHotbarUI] ❌ Slot 1 is NULL!");
            }

            // Setup slot 2 - Spell 2
            if (spellSlots[2] != null)
            {
                spellSlots[2].Initialize(2, spell02Icon, keyLabels[2], "Spell 2");
                Debug.Log($"[SpellHotbarUI] ✅ Slot 2 (Spell 2) initialized. Icon: {(spell02Icon != null ? spell02Icon.name : "null")}");
            }
            else
            {
                Debug.LogError("[SpellHotbarUI] ❌ Slot 2 is NULL!");
            }

            // Setup slot 3 - Spell 3
            if (spellSlots[3] != null)
            {
                spellSlots[3].Initialize(3, spell03Icon, keyLabels[3], "Spell 3");
                Debug.Log($"[SpellHotbarUI] ✅ Slot 3 (Spell 3) initialized. Icon: {(spell03Icon != null ? spell03Icon.name : "null")}");
            }
            else
            {
                Debug.LogError("[SpellHotbarUI] ❌ Slot 3 is NULL!");
            }
        }

        /// <summary>
        /// Cập nhật visual selection
        /// </summary>
        private void UpdateSelection(int selectedSpell)
        {
            for (int i = 0; i < spellSlots.Length; i++)
            {
                if (spellSlots[i] != null)
                {
                    spellSlots[i].SetSelected(i == selectedSpell);
                }
            }
        }

        /// <summary>
        /// Cập nhật cooldown display cho các spells
        /// </summary>
        private void UpdateCooldowns()
        {
            if (spellController == null) return;

            // Slot 0 (Idle) không có cooldown
            if (spellSlots[0] != null)
            {
                spellSlots[0].SetCooldown(0f);
            }

            // Spell 1/2/3 cooldowns
            for (int i = 1; i <= 3; i++)
            {
                if (spellSlots[i] != null)
                {
                    float cooldownPercent = spellController.GetSpellCooldownPercent(i);
                    spellSlots[i].SetCooldown(cooldownPercent);
                }
            }
        }

        /// <summary>
        /// Gọi từ button click nếu muốn dùng UI để chọn spell
        /// </summary>
        public void OnSlotClicked(int slotIndex)
        {
            if (slotIndex == 0)
            {
                // Simulate pressing 0 key
                // spellController sẽ tự handle via keyboard input
                Debug.Log("[SpellHotbarUI] Clicked Idle slot - press 0 to switch");
            }
            else if (slotIndex >= 1 && slotIndex <= 3)
            {
                Debug.Log($"[SpellHotbarUI] Clicked Spell {slotIndex} slot - press {slotIndex} to switch");
            }
        }
    }
}
