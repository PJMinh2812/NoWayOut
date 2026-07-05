using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Test script to add weapons to player inventory on start
    /// </summary>
    public sealed class TestInventorySetup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryManager inventory;
        
        [Header("Test Weapons")]
        [SerializeField] private WeaponItem[] testWeapons;
        
        [Header("Auto-Create Test Weapons")]
        [SerializeField] private bool createDefaultWeapons = true;

        private void Start()
        {
            if (inventory == null)
            {
                inventory = FindFirstObjectByType<InventoryManager>();
            }

            if (inventory == null)
            {
                Debug.LogWarning("[TestInventorySetup] No InventoryManager found!");
                return;
            }

            // Add assigned weapons first
            if (testWeapons != null && testWeapons.Length > 0)
            {
                foreach (var weapon in testWeapons)
                {
                    if (weapon != null)
                    {
                        inventory.AddItem(weapon, 1);
                        Debug.Log($"[TestInventorySetup] Added: {weapon.itemName}");
                    }
                }
            }

            // Create default weapons if enabled and no weapons assigned
            if (createDefaultWeapons && (testWeapons == null || testWeapons.Length == 0))
            {
                CreateAndAddDefaultWeapons();
            }
        }

        private void CreateAndAddDefaultWeapons()
        {
            // Create basic projectile weapon
            var basicGun = ScriptableObject.CreateInstance<WeaponItem>();
            basicGun.itemName = "Basic Gun";
            basicGun.description = "A simple projectile weapon";
            basicGun.maxStackSize = 1;
            basicGun.damage = 10;
            basicGun.attackSpeed = 1f;
            basicGun.attackRange = 10f;
            basicGun.isRanged = true;
            basicGun.holdOffset = new Vector2(0.25f, 0f); // Sát player
            // Note: weaponSprite and icon will be null (no visual)
            // You can assign sprites later in Inspector or create sprite assets

            inventory.AddItem(basicGun, 1);
            Debug.Log("[TestInventorySetup] Created and added: Basic Gun (slot 1)");

            // Create magic staff
            var magicStaff = ScriptableObject.CreateInstance<WeaponItem>();
            magicStaff.itemName = "Magic Staff";
            magicStaff.description = "Shoots magic projectiles";
            magicStaff.maxStackSize = 1;
            magicStaff.damage = 15;
            magicStaff.attackSpeed = 0.8f;
            magicStaff.attackRange = 12f;
            magicStaff.isRanged = true;
            magicStaff.holdOffset = new Vector2(0.25f, 0f); // Sát player

            inventory.AddItem(magicStaff, 1);
            Debug.Log("[TestInventorySetup] Created and added: Magic Staff (slot 2)");

            Debug.Log("[TestInventorySetup] Press 1 or 2 to switch weapons!");
        }
    }
}
