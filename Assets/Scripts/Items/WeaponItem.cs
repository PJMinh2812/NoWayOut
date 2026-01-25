using UnityEngine;

namespace GloomCraft
{
    /// <summary>
    /// Weapon item that can be equipped and displayed on player
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "GloomCraft/Items/Weapon")]
    public class WeaponItem : Item
    {
        [Header("Weapon Stats")]
        public int damage = 10;
        public float attackSpeed = 1f;
        public float attackRange = 1.5f;

        [Header("Visuals")]
        public Sprite weaponSprite; // Sprite hiển thị khi cầm
        public Vector2 holdOffset = new Vector2(0.25f, 0f); // Offset bên cạnh player - sát gần
        public float holdRotation = 0f; // Không dùng cho 2D top-down

        [Header("Attack Settings")]
        public bool isRanged = false;
        public Projectile2D projectilePrefab; // Nếu là ranged weapon

        public WeaponItem()
        {
            itemType = ItemType.Weapon;
            maxStackSize = 1;
        }

        public override void Use(PlayerController2D player)
        {
            // Weapons attack instead of being "used"
            Attack(player);
        }

        public override void OnEquip(PlayerController2D player)
        {
            base.OnEquip(player);
            
            var equipment = player.GetComponent<PlayerEquipment>();
            if (equipment != null)
            {
                equipment.EquipWeapon(this);
            }
        }

        public override void OnUnequip(PlayerController2D player)
        {
            base.OnUnequip(player);
            
            var equipment = player.GetComponent<PlayerEquipment>();
            if (equipment != null)
            {
                equipment.UnequipWeapon();
            }
        }

        private void Attack(PlayerController2D player)
        {
            if (isRanged)
            {
                // Ranged attack - fire projectile
                Debug.Log($"[Weapon] Firing {itemName}");
                // Implementation in PlayerShooter2D
            }
            else
            {
                // Melee attack
                Debug.Log($"[Weapon] Melee attack with {itemName}");
                // Implementation for melee combat
            }
        }
    }
}
