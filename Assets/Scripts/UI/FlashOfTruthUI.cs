using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NWO
{
    /// <summary>
    /// UI display for Flash of Truth ability cooldown and status
    /// </summary>
    public class FlashOfTruthUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image cooldownFillImage;
        [SerializeField] private TextMeshProUGUI cooldownText;
        [SerializeField] private TextMeshProUGUI keyBindText;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Colors")]
        [SerializeField] private Color readyColor = new Color(1f, 1f, 0.3f);
        [SerializeField] private Color cooldownColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        
        private FlashOfTruth flashAbility;
        
        private void Start()
        {
            // Find FlashOfTruth component
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                flashAbility = player.GetComponent<FlashOfTruth>();
                if (flashAbility == null)
                {
                    flashAbility = player.AddComponent<FlashOfTruth>();
                }
            }
            
            // Auto-create UI if references are null
            if (cooldownFillImage == null)
            {
                CreateFlashUI();
            }
            
            // Initially hidden if locked
            if (canvasGroup != null && flashAbility != null && !flashAbility.IsUnlocked)
            {
                canvasGroup.alpha = 0.3f;
            }
        }
        
        private void Update()
        {
            if (flashAbility == null) return;
            
            // Show UI when unlocked
            if (flashAbility.IsUnlocked && canvasGroup != null && canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.deltaTime * 3f);
            }
            
            // Update cooldown fill
            if (cooldownFillImage != null)
            {
                cooldownFillImage.fillAmount = flashAbility.CooldownProgress;
                
                if (flashAbility.IsFlashActive)
                {
                    cooldownFillImage.color = Color.white;
                }
                else if (flashAbility.IsOnCooldown)
                {
                    cooldownFillImage.color = cooldownColor;
                }
                else
                {
                    cooldownFillImage.color = readyColor;
                }
            }
            
            // Update cooldown text
            if (cooldownText != null)
            {
                if (!flashAbility.IsUnlocked)
                {
                    cooldownText.text = "LOCKED";
                }
                else if (flashAbility.IsFlashActive)
                {
                    cooldownText.text = "ACTIVE!";
                }
                else if (flashAbility.IsOnCooldown)
                {
                    float remaining = 15f * (1f - flashAbility.CooldownProgress);
                    cooldownText.text = $"{remaining:F1}s";
                }
                else
                {
                    cooldownText.text = "READY";
                }
            }
        }
        
        private void CreateFlashUI()
        {
            // Find or create Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("UICanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            // Container (bottom-center)
            var container = new GameObject("FlashOfTruthUI");
            container.transform.SetParent(canvas.transform, false);
            
            var containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0f);
            containerRect.anchorMax = new Vector2(0.5f, 0f);
            containerRect.pivot = new Vector2(0.5f, 0f);
            containerRect.anchoredPosition = new Vector2(0f, 80f);
            containerRect.sizeDelta = new Vector2(120f, 120f);
            
            canvasGroup = container.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0.3f;
            
            // Background circle
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(container.transform, false);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.7f);
            bgImage.type = Image.Type.Filled;
            bgImage.fillMethod = Image.FillMethod.Radial360;
            
            // Cooldown fill (radial)
            var fillObj = new GameObject("CooldownFill");
            fillObj.transform.SetParent(container.transform, false);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(10f, 10f);
            fillRect.offsetMax = new Vector2(-10f, -10f);
            
            cooldownFillImage = fillObj.AddComponent<Image>();
            cooldownFillImage.color = lockedColor;
            cooldownFillImage.type = Image.Type.Filled;
            cooldownFillImage.fillMethod = Image.FillMethod.Radial360;
            cooldownFillImage.fillOrigin = (int)Image.Origin360.Top;
            cooldownFillImage.fillClockwise = true;
            cooldownFillImage.fillAmount = 0f;
            
            // Key bind text (top)
            var keyObj = new GameObject("KeyBind");
            keyObj.transform.SetParent(container.transform, false);
            var keyRect = keyObj.AddComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(0.5f, 1f);
            keyRect.anchorMax = new Vector2(0.5f, 1f);
            keyRect.pivot = new Vector2(0.5f, 0f);
            keyRect.anchoredPosition = new Vector2(0f, 5f);
            keyRect.sizeDelta = new Vector2(80f, 30f);
            
            keyBindText = keyObj.AddComponent<TextMeshProUGUI>();
            keyBindText.text = "[SPACE]";
            keyBindText.fontSize = 16;
            keyBindText.color = Color.white;
            keyBindText.alignment = TextAlignmentOptions.Center;
            keyBindText.fontStyle = FontStyles.Bold;
            
            // Cooldown text (center)
            var textObj = new GameObject("CooldownText");
            textObj.transform.SetParent(container.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            cooldownText = textObj.AddComponent<TextMeshProUGUI>();
            cooldownText.text = "LOCKED";
            cooldownText.fontSize = 18;
            cooldownText.color = Color.white;
            cooldownText.alignment = TextAlignmentOptions.Center;
            cooldownText.fontStyle = FontStyles.Bold;
        }
    }
}
