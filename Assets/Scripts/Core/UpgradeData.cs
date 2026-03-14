using UnityEngine;

namespace NWO
{
    public enum UpgradeType
    {
        // === Base Stats ===
        MaxHealth,
        HealthRegen,
        MaxStamina,
        StaminaRegen,
        MoveSpeed,
        DashSpeed,
        InvincibleDuration,

        // === Spell Stats ===
        AllSpellDamage,
        AllSpellRange,
        AllSpellCooldown,
        Spell01Damage,
        Spell02Damage,
        Spell03Damage,
        Spell01Cooldown,
        Spell02Cooldown,
        Spell03Cooldown,

        // === Ranged Stats ===
        FireRate,

        // === Melee (giữ lại cho tương lai) ===
        MeleeDamage,
        AttackRange,
        KnockbackForce
    }

    /// <summary>
    /// ScriptableObject chứa thông tin 1 loại nâng cấp.
    /// Hỗ trợ trade-off: tăng stat chính + giảm stat phụ (penalty).
    /// Tạo trong Editor: Assets > Create > NWO > Upgrade Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "NWO/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [Header("Info")]
        public string upgradeName = "New Upgrade";
        [TextArea(1, 3)]
        public string description = "";
        public Sprite icon;
        public Color cardColor = new(0.18f, 0.22f, 0.28f, 1f);

        [Header("Buff (tăng)")]
        public UpgradeType upgradeType;
        public float value = 1f;

        [Header("Secondary Buff (tùy chọn)")]
        [Tooltip("Buff phụ đi kèm (nếu có)")]
        public bool hasSecondaryBuff = false;
        public UpgradeType secondaryBuffType;
        public float secondaryBuffValue = 0f;

        [Header("Penalty (giảm - trade-off)")]
        [Tooltip("Loại stat bị giảm. Để None-like = không có penalty")]
        public bool hasPenalty = false;
        public UpgradeType penaltyType;
        public float penaltyValue = 0f;

        [Header("Display")]
        [Tooltip("Ký hiệu hiển thị trên thẻ (VD: ♥, ⚔, ⚡)")]
        public string glyphSymbol = "★";
        public Color glyphColor = Color.white;

        [Header("Rarity")]
        public UpgradeRarity rarity = UpgradeRarity.Common;
    }

    public enum UpgradeRarity
    {
        Common,
        Rare,
        Epic
    }
}
