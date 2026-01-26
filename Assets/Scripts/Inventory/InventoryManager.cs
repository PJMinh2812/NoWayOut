using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GloomCraft
{
    /// <summary>
    /// Manages player inventory and hotbar
    /// </summary>
    public sealed class InventoryManager : MonoBehaviour
    {
        [Header("Hotbar Settings")]
        [SerializeField] private int hotbarSize = 9;
        [SerializeField] private int selectedSlot = 0;

        [Header("References")]
        [SerializeField] private PlayerController2D player;
        [SerializeField] private PlayerEquipment equipment;

        private List<ItemSlot> _hotbar;
        private Item[] _hotbarItems; // Simplified array for quick access

        public event Action<int> OnSlotChanged; // Slot index
        public event Action<int, Item, int> OnItemChanged; // Slot, Item, Count

        [Serializable]
        public class ItemSlot
        {
            public Item item;
            public int count;

            public bool IsEmpty => item == null || count <= 0;
        }

        private void Awake()
        {
            if (player == null) player = GetComponent<PlayerController2D>();
            if (equipment == null) equipment = GetComponent<PlayerEquipment>();

            InitializeHotbar();
        }

        private void Update()
        {
            HandleHotbarInput();
        }

        private void InitializeHotbar()
        {
            _hotbar = new List<ItemSlot>();
            _hotbarItems = new Item[hotbarSize];

            for (int i = 0; i < hotbarSize; i++)
            {
                _hotbar.Add(new ItemSlot());
            }
        }

        private void HandleHotbarInput()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            // Number keys 1-9 to select hotbar slots
            for (int i = 0; i < Mathf.Min(hotbarSize, 9); i++)
            {
                Key key = Key.Digit1 + i;
                if (keyboard[key].wasPressedThisFrame)
                {
                    SelectSlot(i);
                }
            }

            // Mouse wheel to cycle through hotbar
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0)
            {
                int direction = scroll > 0 ? -1 : 1;
                int newSlot = (selectedSlot + direction + hotbarSize) % hotbarSize;
                SelectSlot(newSlot);
            }
        }

        /// <summary>
        /// Select a hotbar slot and equip its item
        /// </summary>
        public void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= hotbarSize) return;
            if (slotIndex == selectedSlot) return;

            selectedSlot = slotIndex;
            OnSlotChanged?.Invoke(selectedSlot);

            // Equip item in selected slot
            var slot = _hotbar[slotIndex];
            if (equipment != null && slot.item is WeaponItem weapon)
            {
                weapon.OnEquip(player);
            }
            else if (equipment != null)
            {
                // Unequip if slot is empty or not a weapon
                equipment.UnequipWeapon();
            }

            Debug.Log($"[Inventory] Selected slot {slotIndex}: {(slot.item != null ? slot.item.itemName : "Empty")}");
        }

        /// <summary>
        /// Add item to hotbar
        /// </summary>
        public bool AddItem(Item item, int count = 1)
        {
            if (item == null || count <= 0) return false;

            // Try to stack with existing item
            for (int i = 0; i < hotbarSize; i++)
            {
                var slot = _hotbar[i];
                if (slot.item == item && slot.count < item.maxStackSize)
                {
                    int addAmount = Mathf.Min(count, item.maxStackSize - slot.count);
                    slot.count += addAmount;
                    count -= addAmount;

                    OnItemChanged?.Invoke(i, item, slot.count);

                    if (count <= 0) return true;
                }
            }

            // Find empty slot
            for (int i = 0; i < hotbarSize; i++)
            {
                var slot = _hotbar[i];
                if (slot.IsEmpty)
                {
                    slot.item = item;
                    slot.count = Mathf.Min(count, item.maxStackSize);
                    _hotbarItems[i] = item;

                    OnItemChanged?.Invoke(i, item, slot.count);
                    return true;
                }
            }

            Debug.LogWarning("[Inventory] Hotbar is full!");
            return false;
        }

        /// <summary>
        /// Use item in selected slot
        /// </summary>
        public void UseSelectedItem()
        {
            var slot = _hotbar[selectedSlot];
            if (slot.IsEmpty) return;

            slot.item.Use(player);

            // Decrease count for consumables
            if (slot.item.itemType == Item.ItemType.Consumable)
            {
                slot.count--;
                if (slot.count <= 0)
                {
                    slot.item = null;
                    _hotbarItems[selectedSlot] = null;
                }
                OnItemChanged?.Invoke(selectedSlot, slot.item, slot.count);
            }
        }

        /// <summary>
        /// Get item at specific slot
        /// </summary>
        public Item GetItemAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= hotbarSize) return null;
            return _hotbar[slotIndex].item;
        }

        /// <summary>
        /// Get item count at specific slot
        /// </summary>
        public int GetCountAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= hotbarSize) return 0;
            return _hotbar[slotIndex].count;
        }

        public int SelectedSlot => selectedSlot;
        public int HotbarSize => hotbarSize;
    }
}
