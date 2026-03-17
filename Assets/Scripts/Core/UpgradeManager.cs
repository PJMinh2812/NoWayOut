using UnityEngine;
using System.Collections.Generic;

namespace NWO
{
    /// <summary>
    /// Singleton quản lý hệ thống nâng cấp.
    /// Lưu trữ danh sách nâng cấp đã chọn và áp dụng lên player.
    /// </summary>
    public sealed class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [Header("Upgrade Pool")]
        [Tooltip("Danh sách tất cả nâng cấp có thể xuất hiện")]
        [SerializeField] private List<UpgradeData> upgradePool = new();

        /// <summary>Danh sách nâng cấp đã được người chơi chọn (theo thứ tự)</summary>
        public IReadOnlyList<UpgradeData> ChosenUpgrades => _chosenUpgrades;
        private readonly List<UpgradeData> _chosenUpgrades = new();

        /// <summary>Event khi nâng cấp mới được chọn</summary>
        public event System.Action<UpgradeData> OnUpgradeChosen;

        /// <summary>Event yêu cầu hiển thị UI chọn nâng cấp</summary>
        public event System.Action<List<UpgradeData>> OnShowUpgradeSelection;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (upgradePool.Count == 0)
                BuildDefaultPool();
        }

        /// <summary>
        /// Gọi khi tất cả enemy trong gate bị tiêu diệt.
        /// Chọn 3 nâng cấp ngẫu nhiên và hiển thị UI.
        /// </summary>
        public void TriggerUpgradeSelection()
        {
            var options = PickRandomUpgrades(3);
            if (options.Count == 0)
            {
                Debug.LogWarning("[UpgradeManager] Không có upgrade nào trong pool!");
                return;
            }

            OnShowUpgradeSelection?.Invoke(options);
        }

        /// <summary>
        /// Người chơi chọn 1 nâng cấp. Áp dụng buff + penalty lên player.
        /// </summary>
        public void SelectUpgrade(UpgradeData upgrade)
        {
            if (upgrade == null) return;

            _chosenUpgrades.Add(upgrade);

            // Áp dụng buff chính
            ApplyStat(upgrade.upgradeType, upgrade.value);

            // Áp dụng buff phụ (nếu có)
            if (upgrade.hasSecondaryBuff && upgrade.secondaryBuffValue != 0f)
                ApplyStat(upgrade.secondaryBuffType, upgrade.secondaryBuffValue);

            // Áp dụng penalty (trade-off)
            if (upgrade.hasPenalty && upgrade.penaltyValue != 0f)
                ApplyStat(upgrade.penaltyType, -Mathf.Abs(upgrade.penaltyValue));

            OnUpgradeChosen?.Invoke(upgrade);

            Debug.Log($"[UpgradeManager] Đã chọn: {upgrade.upgradeName} (+{upgrade.value} {upgrade.upgradeType}" +
                      (upgrade.hasPenalty ? $", -{upgrade.penaltyValue} {upgrade.penaltyType})" : ")"));
        }

        /// <summary>Reset tất cả nâng cấp (khi restart game)</summary>
        public void ResetUpgrades()
        {
            _chosenUpgrades.Clear();
        }

        private List<UpgradeData> PickRandomUpgrades(int count)
        {
            var result = new List<UpgradeData>();
            if (upgradePool.Count == 0) return result;

            var pool = new List<UpgradeData>(upgradePool);

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            return result;
        }

        /// <summary>Áp dụng 1 stat thay đổi (dương = tăng, âm = giảm)</summary>
        private void ApplyStat(UpgradeType type, float amount)
        {
            var player = FindFirstObjectByType<PlayerController2D>();
            if (player == null) return;

            var health = player.GetComponent<PlayerHealth2D>();
            var stamina = player.GetComponent<PlayerStamina>();
            var melee = player.GetComponent<PlayerMeleeController>();
            var spell = player.GetComponent<PlayerSpellController>();
            var shooter = player.GetComponent<PlayerShooter2D>();

            switch (type)
            {
                // Base stats
                case UpgradeType.MaxHealth:
                    if (health != null) health.AddMaxHealth(Mathf.RoundToInt(amount));
                    break;
                case UpgradeType.HealthRegen:
                    if (health != null) health.AddRegeneration(amount);
                    break;
                case UpgradeType.MaxStamina:
                    if (stamina != null) stamina.AddMaxStamina(amount);
                    break;
                case UpgradeType.StaminaRegen:
                    if (stamina != null) stamina.AddRegenPerSecond(amount);
                    break;
                case UpgradeType.MoveSpeed:
                    player.AddMaxMoveSpeed(amount);
                    break;
                case UpgradeType.DashSpeed:
                    player.AddDashSpeed(amount);
                    break;
                case UpgradeType.InvincibleDuration:
                    if (health != null) health.AddInvincibleDuration(amount);
                    break;

                // Spell stats
                case UpgradeType.AllSpellDamage:
                    if (spell != null) spell.AddAllSpellDamage(Mathf.RoundToInt(amount));
                    break;
                case UpgradeType.AllSpellRange:
                    if (spell != null) spell.AddAllSpellRange(amount);
                    break;
                case UpgradeType.AllSpellCooldown:
                    if (spell != null) spell.AddAllSpellCooldownReduction(amount);
                    break;
                case UpgradeType.Spell01Damage:
                    if (spell != null) spell.AddSpellDamage(1, Mathf.RoundToInt(amount));
                    break;
                case UpgradeType.Spell02Damage:
                    if (spell != null) spell.AddSpellDamage(2, Mathf.RoundToInt(amount));
                    break;
                case UpgradeType.Spell03Damage:
                    if (spell != null) spell.AddSpellDamage(3, Mathf.RoundToInt(amount));
                    break;
                case UpgradeType.Spell01Cooldown:
                    if (spell != null) spell.AddSpellCooldownReduction(1, amount);
                    break;
                case UpgradeType.Spell02Cooldown:
                    if (spell != null) spell.AddSpellCooldownReduction(2, amount);
                    break;
                case UpgradeType.Spell03Cooldown:
                    if (spell != null) spell.AddSpellCooldownReduction(3, amount);
                    break;

                // Ranged
                case UpgradeType.FireRate:
                    if (shooter != null) shooter.AddFireRate(amount);
                    break;

                // Melee (giữ lại)
                case UpgradeType.MeleeDamage:
                    if (melee != null) melee.AddBaseDamage(Mathf.RoundToInt(amount));
                    break;
                case UpgradeType.AttackRange:
                    if (melee != null) melee.AddAttackRange(amount);
                    break;
                case UpgradeType.KnockbackForce:
                    if (melee != null) melee.AddKnockbackForce(amount);
                    break;
            }
        }

        // =============================================
        // DEFAULT POOL - Đa dạng với trade-off
        // =============================================
        private void BuildDefaultPool()
        {
            // ──── COMMON: Pure buffs (không penalty) ────
            Add("Máu Dẻo", "Tăng máu tối đa +15", UpgradeType.MaxHealth, 15f,
                "♥", new Color(1f, 0.35f, 0.35f), UpgradeRarity.Common);

            Add("Tái Tạo", "Tăng hồi máu +0.3/s", UpgradeType.HealthRegen, 0.3f,
                "♥", new Color(0.3f, 1f, 0.5f), UpgradeRarity.Common);

            Add("Năng Lượng", "Tăng stamina tối đa +12", UpgradeType.MaxStamina, 12f,
                "⚡", new Color(0.3f, 0.8f, 1f), UpgradeRarity.Common);

            Add("Phục Hồi", "Tăng hồi stamina +2/s", UpgradeType.StaminaRegen, 2f,
                "⚡", new Color(0.5f, 0.9f, 1f), UpgradeRarity.Common);

            Add("Chân Nhanh", "Tăng tốc độ +0.4", UpgradeType.MoveSpeed, 0.4f,
                "➤", new Color(0.4f, 1f, 0.4f), UpgradeRarity.Common);

            Add("Phép Thuật Mạnh", "Tăng sát thương phép +4", UpgradeType.AllSpellDamage, 4f,
                "✦", new Color(0.7f, 0.4f, 1f), UpgradeRarity.Common);

            Add("Tốc Bắn", "Tăng tốc độ bắn", UpgradeType.FireRate, 0.02f,
                "◉", new Color(1f, 0.8f, 0.3f), UpgradeRarity.Common);

            // ──── RARE: Trade-off cards ────
            AddTradeOff("Pháo Thủy Tinh", "Phép mạnh hơn +8\nNhưng máu giảm -15",
                UpgradeType.AllSpellDamage, 8f,
                UpgradeType.MaxHealth, 15f,
                "💀", new Color(1f, 0.3f, 0.6f), UpgradeRarity.Rare);

            AddTradeOff("Kiên Cường", "Máu +25, Hồi máu +0.5\nNhưng chậm hơn -0.4",
                UpgradeType.MaxHealth, 25f,
                UpgradeType.MoveSpeed, 0.4f,
                "🛡", new Color(0.6f, 0.8f, 0.3f), UpgradeRarity.Rare,
                UpgradeType.HealthRegen, 0.5f);

            AddTradeOff("Tia Chớp", "Tốc độ +0.8, Dash +2\nNhưng máu giảm -10",
                UpgradeType.MoveSpeed, 0.8f,
                UpgradeType.MaxHealth, 10f,
                "⚡", new Color(1f, 1f, 0.3f), UpgradeRarity.Rare,
                UpgradeType.DashSpeed, 2f);

            AddTradeOff("Cuồng Phép", "Sát thương phép +6\nNhưng stamina giảm -10",
                UpgradeType.AllSpellDamage, 6f,
                UpgradeType.MaxStamina, 10f,
                "🔥", new Color(1f, 0.5f, 0.1f), UpgradeRarity.Rare);

            AddTradeOff("Mana Dồi Dào", "Stamina +20\nNhưng hồi máu giảm -0.3",
                UpgradeType.MaxStamina, 20f,
                UpgradeType.HealthRegen, 0.3f,
                "💧", new Color(0.3f, 0.6f, 1f), UpgradeRarity.Rare);

            AddTradeOff("Phản Xạ Nhanh", "Giảm cooldown phép -0.5s\nNhưng tầm phép giảm -1",
                UpgradeType.AllSpellCooldown, 0.5f,
                UpgradeType.AllSpellRange, 1f,
                "⏱", new Color(0.8f, 0.8f, 0.2f), UpgradeRarity.Rare);

            AddTradeOff("Bắn Tỉa", "Tầm phép +2.5\nNhưng tốc độ giảm -0.3",
                UpgradeType.AllSpellRange, 2.5f,
                UpgradeType.MoveSpeed, 0.3f,
                "🎯", new Color(0.2f, 0.9f, 0.7f), UpgradeRarity.Rare);

            AddTradeOff("Liều Mạng", "Tốc bắn tăng mạnh\nNhưng bất tử giảm -0.2s",
                UpgradeType.FireRate, 0.04f,
                UpgradeType.InvincibleDuration, 0.15f,
                "☠", new Color(1f, 0.2f, 0.2f), UpgradeRarity.Rare);

            AddTradeOff("Lướt Gió", "Dash nhanh +3\nNhưng stamina giảm -8",
                UpgradeType.DashSpeed, 3f,
                UpgradeType.MaxStamina, 8f,
                "💨", new Color(0.6f, 0.9f, 1f), UpgradeRarity.Rare);

            // ──── EPIC: Spell-specific (mạnh nhưng đánh đổi lớn) ────
            AddTradeOff("Chuyên Gia Spell 1", "Spell 1 damage +12\nNhưng Spell 2,3 damage -4",
                UpgradeType.Spell01Damage, 12f,
                UpgradeType.Spell02Damage, 4f,
                "①", new Color(1f, 0.7f, 0.3f), UpgradeRarity.Epic);

            AddTradeOff("Chuyên Gia Spell 2", "Spell 2 damage +15\nNhưng Spell 1,3 damage -4",
                UpgradeType.Spell02Damage, 15f,
                UpgradeType.Spell01Damage, 4f,
                "②", new Color(0.3f, 0.7f, 1f), UpgradeRarity.Epic);

            AddTradeOff("Chuyên Gia Spell 3", "Spell 3 damage +20\nNhưng Spell 1,2 damage -5",
                UpgradeType.Spell03Damage, 20f,
                UpgradeType.Spell01Damage, 5f,
                "③", new Color(0.9f, 0.3f, 1f), UpgradeRarity.Epic);

            AddTradeOff("Thần Tốc", "Tốc độ +1.0, Dash +3\nNhưng máu giảm -20",
                UpgradeType.MoveSpeed, 1.0f,
                UpgradeType.MaxHealth, 20f,
                "⚡", new Color(1f, 1f, 0f), UpgradeRarity.Epic,
                UpgradeType.DashSpeed, 3f);

            AddTradeOff("Bất Tử", "Bất tử +0.4s, Máu +20\nNhưng tốc độ giảm -0.5",
                UpgradeType.InvincibleDuration, 0.4f,
                UpgradeType.MoveSpeed, 0.5f,
                "✟", new Color(1f, 0.9f, 0.5f), UpgradeRarity.Epic,
                UpgradeType.MaxHealth, 20f);
        }

        private void Add(string name, string desc, UpgradeType type, float val,
            string glyph, Color glyphCol, UpgradeRarity rarity)
        {
            var d = ScriptableObject.CreateInstance<UpgradeData>();
            d.upgradeName = name;
            d.description = desc;
            d.upgradeType = type;
            d.value = val;
            d.hasPenalty = false;
            d.glyphSymbol = glyph;
            d.glyphColor = glyphCol;
            d.rarity = rarity;
            d.cardColor = RarityColor(rarity);
            upgradePool.Add(d);
        }

        private void AddTradeOff(string name, string desc,
            UpgradeType buffType, float buffVal,
            UpgradeType penType, float penVal,
            string glyph, Color glyphCol, UpgradeRarity rarity,
            UpgradeType secBuffType = default, float secBuffVal = 0f)
        {
            var d = ScriptableObject.CreateInstance<UpgradeData>();
            d.upgradeName = name;
            d.description = desc;
            d.upgradeType = buffType;
            d.value = buffVal;
            d.hasPenalty = true;
            d.penaltyType = penType;
            d.penaltyValue = penVal;
            d.hasSecondaryBuff = secBuffVal != 0f;
            d.secondaryBuffType = secBuffType;
            d.secondaryBuffValue = secBuffVal;
            d.glyphSymbol = glyph;
            d.glyphColor = glyphCol;
            d.rarity = rarity;
            d.cardColor = RarityColor(rarity);
            upgradePool.Add(d);
        }

        private static Color RarityColor(UpgradeRarity rarity) => rarity switch
        {
            UpgradeRarity.Common => new Color(0.14f, 0.16f, 0.20f, 0.95f),
            UpgradeRarity.Rare => new Color(0.12f, 0.15f, 0.25f, 0.95f),
            UpgradeRarity.Epic => new Color(0.20f, 0.10f, 0.22f, 0.95f),
            _ => new Color(0.14f, 0.16f, 0.20f, 0.95f)
        };

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
