using System.Collections.Generic;
using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Hotbar UI controller
    /// </summary>
    public sealed class HotbarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryManager inventory;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Transform slotsContainer;

        private List<HotbarSlot> _slots = new List<HotbarSlot>();

        private void Awake()
        {
            // Find inventory if not assigned
            if (inventory == null)
            {
                inventory = FindFirstObjectByType<InventoryManager>();
            }
        }

        private void Start()
        {
            if (inventory != null)
            {
                CreateSlots();
                
                // Subscribe to inventory events
                inventory.OnSlotChanged += OnSlotSelected;
                inventory.OnItemChanged += OnItemChanged;
                
                // Select first slot
                OnSlotSelected(0);
            }
        }

        private void OnDestroy()
        {
            if (inventory != null)
            {
                inventory.OnSlotChanged -= OnSlotSelected;
                inventory.OnItemChanged -= OnItemChanged;
            }
        }

        private void CreateSlots()
        {
            // Clear existing slots
            foreach (Transform child in slotsContainer)
            {
                Destroy(child.gameObject);
            }
            _slots.Clear();

            // Create new slots
            for (int i = 0; i < inventory.HotbarSize; i++)
            {
                GameObject slotGo;
                
                if (slotPrefab != null)
                {
                    slotGo = Instantiate(slotPrefab, slotsContainer);
                }
                else
                {
                    slotGo = CreateDefaultSlot(i);
                }

                var slot = slotGo.GetComponent<HotbarSlot>();
                if (slot == null)
                {
                    slot = slotGo.AddComponent<HotbarSlot>();
                }

                slot.Initialize(i);
                _slots.Add(slot);

                // Set initial item
                var item = inventory.GetItemAt(i);
                var count = inventory.GetCountAt(i);
                slot.SetItem(item, count);
            }
        }

        private GameObject CreateDefaultSlot(int index)
        {
            // Create basic slot if no prefab provided
            var slotGo = new GameObject($"Slot_{index}");
            slotGo.transform.SetParent(slotsContainer);
            
            var rect = slotGo.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(64, 64);
            
            return slotGo;
        }

        private void OnSlotSelected(int slotIndex)
        {
            // Update visual selection
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].SetSelected(i == slotIndex);
            }
        }

        private void OnItemChanged(int slotIndex, Item item, int count)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return;
            
            _slots[slotIndex].SetItem(item, count);
        }
    }
}
