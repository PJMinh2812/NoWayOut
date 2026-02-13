using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

namespace NWO.UI
{
    /// <summary>
    /// Lighting-Integrated Item Hotbar UI
    /// - Auto-creates on existing Canvas (GameCanvas) or creates its own
    /// - Glow effect syncs with player light (fragments collected → brighter UI)
    /// - Horror-themed dark styling
    /// - Reacts to DungeonLightingManager state
    /// </summary>
    public class LightingHotbarUI : MonoBehaviour
    {
        [Header("Hotbar Settings")]
        [SerializeField] private int slotCount = 5;
        [SerializeField] private float slotSize = 56f;
        [SerializeField] private float slotSpacing = 4f;
        
        [Header("Lighting Theme Colors")]
        [SerializeField] private Color baseSlotColor = new Color(0.08f, 0.08f, 0.12f, 0.85f);
        [SerializeField] private Color selectedSlotColor = new Color(0.9f, 0.85f, 0.6f, 0.95f);
        [SerializeField] private Color glowColor = new Color(0.9f, 0.85f, 0.7f, 0.4f);
        [SerializeField] private Color borderColor = new Color(0.3f, 0.3f, 0.4f, 0.6f);
        [SerializeField] private Color lockedColor = new Color(0.05f, 0.05f, 0.05f, 0.9f);
        
        [Header("Lighting Integration")]
        [SerializeField] private float baseGlowIntensity = 0.1f;
        [SerializeField] private float maxGlowIntensity = 0.6f;
        [SerializeField] private float glowPulseSpeed = 1.5f;
        
        private Image[] slotBackgrounds;
        private Image[] slotIcons;
        private TextMeshProUGUI[] slotKeyBinds;
        private Image[] slotGlows;
        private Image selectionIndicator;
        
        private CanvasGroup hotbarGroup;
        private RectTransform containerRect;
        
        private int selectedSlot = 0;
        private float currentGlowLevel = 0f;
        private float targetGlowLevel = 0f;
        private InventoryManager inventory;
        
        public static LightingHotbarUI Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private void Start()
        {
            // Find inventory
            inventory = FindFirstObjectByType<InventoryManager>();
            
            CreateHotbarUI();
            
            // Subscribe to lighting changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLightFragmentCollected += OnFragmentCollected;
            }
            
            // Subscribe to inventory events if available
            if (inventory != null)
            {
                inventory.OnSlotChanged += OnSlotSelected;
                inventory.OnItemChanged += OnItemChanged;
            }
            
            // Initial glow level based on current fragments
            UpdateGlowFromFragments();
            
            Debug.Log("[LightingHotbarUI] ★ Hotbar created with lighting integration");
        }
        
        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLightFragmentCollected -= OnFragmentCollected;
            }
            if (inventory != null)
            {
                inventory.OnSlotChanged -= OnSlotSelected;
                inventory.OnItemChanged -= OnItemChanged;
            }
        }
        
        private void Update()
        {
            HandleKeyboardInput();
            UpdateGlowAnimation();
        }
        
        #region UI Creation
        
        private void CreateHotbarUI()
        {
            // Find existing ScreenSpaceOverlay canvas
            Canvas canvas = null;
            var allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas = c;
                    break;
                }
            }
            
            if (canvas == null)
            {
                var canvasObj = new GameObject("HotbarCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 90;
                
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            // === Main Container ===
            var container = new GameObject("ItemHotbar");
            container.transform.SetParent(canvas.transform, false);
            
            containerRect = container.AddComponent<RectTransform>();
            // Bottom-right to avoid overlap with SpellHotbar (bottom-center)
            containerRect.anchorMin = new Vector2(1f, 0f);
            containerRect.anchorMax = new Vector2(1f, 0f);
            containerRect.pivot = new Vector2(1f, 0f);
            
            float totalWidth = slotCount * (slotSize + slotSpacing) + slotSpacing;
            containerRect.sizeDelta = new Vector2(totalWidth, slotSize + slotSpacing * 2 + 16f);
            containerRect.anchoredPosition = new Vector2(-20f, 20f);
            
            // Container background
            var bgImage = container.AddComponent<Image>();
            bgImage.color = new Color(0.03f, 0.03f, 0.06f, 0.7f);
            
            var outline = container.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(1, 1);
            
            hotbarGroup = container.AddComponent<CanvasGroup>();
            
            // === Title Label ===
            var titleObj = new GameObject("HotbarTitle");
            titleObj.transform.SetParent(container.transform, false);
            
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 0f);
            titleRect.anchoredPosition = new Vector2(0, 0);
            titleRect.sizeDelta = new Vector2(0, 14f);
            
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "ITEMS";
            titleText.fontSize = 9f;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.5f, 0.5f, 0.6f, 0.7f);
            titleText.fontStyle = FontStyles.Bold;
            
            // === Create Slots ===
            slotBackgrounds = new Image[slotCount];
            slotIcons = new Image[slotCount];
            slotKeyBinds = new TextMeshProUGUI[slotCount];
            slotGlows = new Image[slotCount];
            
            for (int i = 0; i < slotCount; i++)
            {
                CreateSlot(container.transform, i);
            }
            
            // Select first slot
            SelectSlot(0);
        }
        
        private void CreateSlot(Transform parent, int index)
        {
            // Slot container
            var slotObj = new GameObject($"Slot_{index}");
            slotObj.transform.SetParent(parent, false);
            
            var slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0, 0);
            slotRect.anchorMax = new Vector2(0, 0);
            slotRect.pivot = new Vector2(0, 0);
            slotRect.sizeDelta = new Vector2(slotSize, slotSize);
            
            float xPos = slotSpacing + index * (slotSize + slotSpacing);
            slotRect.anchoredPosition = new Vector2(xPos, slotSpacing);
            
            // Background
            slotBackgrounds[index] = slotObj.AddComponent<Image>();
            slotBackgrounds[index].color = baseSlotColor;
            
            // Glow overlay (for lighting effect)
            var glowObj = new GameObject("Glow");
            glowObj.transform.SetParent(slotObj.transform, false);
            
            var glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-3f, -3f);
            glowRect.offsetMax = new Vector2(3f, 3f);
            
            slotGlows[index] = glowObj.AddComponent<Image>();
            slotGlows[index].color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
            slotGlows[index].raycastTarget = false;
            
            // Icon
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(6f, 6f);
            iconRect.offsetMax = new Vector2(-6f, -6f);
            
            slotIcons[index] = iconObj.AddComponent<Image>();
            slotIcons[index].color = new Color(1f, 1f, 1f, 0.2f);
            slotIcons[index].enabled = false;
            slotIcons[index].raycastTarget = false;
            
            // Key bind text
            var keyObj = new GameObject("KeyBind");
            keyObj.transform.SetParent(slotObj.transform, false);
            
            var keyRect = keyObj.AddComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(0, 1);
            keyRect.anchorMax = new Vector2(0, 1);
            keyRect.pivot = new Vector2(0, 1);
            keyRect.anchoredPosition = new Vector2(2f, -1f);
            keyRect.sizeDelta = new Vector2(20f, 14f);
            
            slotKeyBinds[index] = keyObj.AddComponent<TextMeshProUGUI>();
            slotKeyBinds[index].text = $"{index + 1}";
            slotKeyBinds[index].fontSize = 10f;
            slotKeyBinds[index].alignment = TextAlignmentOptions.TopLeft;
            slotKeyBinds[index].color = new Color(0.5f, 0.5f, 0.6f, 0.5f);
            slotKeyBinds[index].raycastTarget = false;
        }
        
        #endregion
        
        #region Input & Selection
        
        private void HandleKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            
            // Number keys 1-5 (or slotCount)
            for (int i = 0; i < slotCount && i < 9; i++)
            {
                Key key = Key.Digit1 + i;
                if (keyboard[key].wasPressedThisFrame)
                {
                    SelectSlot(i);
                    break;
                }
            }
            
            // Mouse wheel
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.1f)
                    SelectSlot((selectedSlot - 1 + slotCount) % slotCount);
                else if (scroll < -0.1f)
                    SelectSlot((selectedSlot + 1) % slotCount);
            }
        }
        
        private void SelectSlot(int index)
        {
            if (index < 0 || index >= slotCount) return;
            
            // Deselect previous
            if (slotBackgrounds[selectedSlot] != null)
            {
                slotBackgrounds[selectedSlot].color = baseSlotColor;
                if (slotKeyBinds[selectedSlot] != null)
                    slotKeyBinds[selectedSlot].color = new Color(0.5f, 0.5f, 0.6f, 0.5f);
            }
            
            selectedSlot = index;
            
            // Select new
            if (slotBackgrounds[selectedSlot] != null)
            {
                slotBackgrounds[selectedSlot].color = selectedSlotColor;
                if (slotKeyBinds[selectedSlot] != null)
                    slotKeyBinds[selectedSlot].color = Color.white;
            }
            
            // Notify inventory if available
            if (inventory != null)
            {
                inventory.SelectSlot(index);
            }
        }
        
        #endregion
        
        #region Lighting Integration
        
        private void OnFragmentCollected(int current, int total)
        {
            targetGlowLevel = Mathf.Lerp(baseGlowIntensity, maxGlowIntensity, (float)current / Mathf.Max(1, total));
            
            // Flash effect on collection
            StartCoroutine(FlashGlow());
        }
        
        private void UpdateGlowFromFragments()
        {
            if (GameManager.Instance != null)
            {
                int current = GameManager.Instance.LightFragmentsCollected;
                int total = GameManager.Instance.TotalLightFragments;
                targetGlowLevel = Mathf.Lerp(baseGlowIntensity, maxGlowIntensity, (float)current / Mathf.Max(1, total));
                currentGlowLevel = targetGlowLevel;
            }
        }
        
        private void UpdateGlowAnimation()
        {
            // Smooth lerp glow
            currentGlowLevel = Mathf.Lerp(currentGlowLevel, targetGlowLevel, Time.deltaTime * 2f);
            
            // Pulse effect
            float pulse = 1f + Mathf.Sin(Time.time * glowPulseSpeed) * 0.15f;
            float finalGlow = currentGlowLevel * pulse;
            
            // Apply glow to slots
            for (int i = 0; i < slotCount; i++)
            {
                if (slotGlows[i] != null)
                {
                    float slotGlow = (i == selectedSlot) ? finalGlow * 1.5f : finalGlow;
                    slotGlows[i].color = new Color(glowColor.r, glowColor.g, glowColor.b, slotGlow);
                }
            }
        }
        
        private IEnumerator FlashGlow()
        {
            float flashDuration = 0.5f;
            float elapsed = 0f;
            float originalGlow = currentGlowLevel;
            
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flashDuration;
                currentGlowLevel = Mathf.Lerp(maxGlowIntensity, originalGlow, t);
                yield return null;
            }
        }
        
        #endregion
        
        #region Inventory Integration
        
        private void OnSlotSelected(int slotIndex)
        {
            SelectSlot(slotIndex);
        }
        
        private void OnItemChanged(int slotIndex, Item item, int count)
        {
            if (slotIndex < 0 || slotIndex >= slotCount) return;
            
            if (slotIcons[slotIndex] != null)
            {
                if (item != null && item.icon != null)
                {
                    slotIcons[slotIndex].sprite = item.icon;
                    slotIcons[slotIndex].color = Color.white;
                    slotIcons[slotIndex].enabled = true;
                }
                else
                {
                    slotIcons[slotIndex].sprite = null;
                    slotIcons[slotIndex].enabled = false;
                }
            }
        }
        
        #endregion
    }
}
