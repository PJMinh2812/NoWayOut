using UnityEngine;

namespace GloomCraft
{
    /// <summary>
    /// Base class for all items in the game
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "GloomCraft/Items/Item")]
    public class Item : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemName = "New Item";
        [TextArea(3, 5)]
        public string description = "Item description";
        public Sprite icon;
        public int maxStackSize = 1;

        [Header("Item Type")]
        public ItemType itemType = ItemType.Consumable;

        public enum ItemType
        {
            Consumable,
            Weapon,
            Tool,
            Material,
            QuestItem
        }

        /// <summary>
        /// Called when item is used/activated
        /// </summary>
        public virtual void Use(PlayerController2D player)
        {
            Debug.Log($"[Item] Used: {itemName}");
        }

        /// <summary>
        /// Called when item is equipped (for weapons/tools)
        /// </summary>
        public virtual void OnEquip(PlayerController2D player)
        {
            Debug.Log($"[Item] Equipped: {itemName}");
        }

        /// <summary>
        /// Called when item is unequipped
        /// </summary>
        public virtual void OnUnequip(PlayerController2D player)
        {
            Debug.Log($"[Item] Unequipped: {itemName}");
        }
    }
}
