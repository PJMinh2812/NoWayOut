using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Spell Hotbar UI - Hiển thị 4 spell slots (0=Idle, 1/2/3=Spells)
    /// </summary>
    public sealed class SpellHotbarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSpellController spellController;
        [SerializeField] private SpellHotbarSlot[] spellSlots; // 4 slots: 0, 1, 2, 3

        [Header("Spell Icons (Auto-load từ prefab nếu để trống)")]
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
                Debug.LogError("[SpellHotbarUI] SpellSlots array is empty!");
                enabled = false;
                return;
            }

            AutoLoadSpellIcons();
            InitializeSlots();
            UpdateSelection(0);
        }

        /// <summary>
        /// Auto-load icons từ spell projectile prefabs nếu chưa gán trong Inspector.
        /// </summary>
        private void AutoLoadSpellIcons()
        {
            if (spellController == null) return;

            if (idleIcon == null)
                idleIcon = CreateColorSprite(idleColor, "IdleIcon");

            if (spell01Icon == null)
                spell01Icon = spellController.GetSpellIcon(1) ?? CreateColorSprite(spell01Color, "Spell01Icon");

            if (spell02Icon == null)
                spell02Icon = spellController.GetSpellIcon(2) ?? CreateColorSprite(spell02Color, "Spell02Icon");

            if (spell03Icon == null)
                spell03Icon = spellController.GetSpellIcon(3) ?? CreateColorSprite(spell03Color, "Spell03Icon");
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
                Debug.LogError("[SpellHotbarUI] SpellSlots array must have exactly 4 elements!");
                return;
            }

            string[] names = { "Idle", "Spell 1", "Spell 2", "Spell 3" };
            Sprite[] icons = { idleIcon, spell01Icon, spell02Icon, spell03Icon };

            for (int i = 0; i < 4; i++)
            {
                if (spellSlots[i] != null)
                    spellSlots[i].Initialize(i, icons[i], keyLabels[i], names[i]);
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
            // UI slot click - spell switching handled by keyboard input
        }
    }
}
