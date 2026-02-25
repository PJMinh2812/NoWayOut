using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NWO
{
    /// <summary>
    /// UI hiá»ƒn thá»‹ sá»‘ Light Fragment Ä‘Ã£ thu tháº­p.
    /// Tá»± Ä‘á»™ng táº¡o UI náº¿u chÆ°a cÃ³.
    /// </summary>
    public class LightFragmentUI : MonoBehaviour
    {
        [Header("UI References (auto-create náº¿u null)")]
        [SerializeField] private TextMeshProUGUI fragmentCountText;
        [SerializeField] private Image fragmentIcon;
        
        [Header("Settings")]
        [SerializeField] private Color normalColor = new Color(1f, 0.95f, 0.6f);
        [SerializeField] private Color completeColor = new Color(0.5f, 1f, 0.5f);
        [SerializeField] private float pulseSpeed = 2f;
        
        [Header("Notification")]
        [SerializeField] private float notificationDuration = 2f;
        
        private CanvasGroup notificationGroup;
        private TextMeshProUGUI notificationText;
        private float notificationTimer = 0f;
        private bool allCollected = false;
        
        private void Start()
        {
            // Auto-create UI náº¿u chÆ°a cÃ³ references
            if (fragmentCountText == null)
            {
                CreateFragmentUI();
            }
            
            // Subscribe to events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLightFragmentCollected += OnFragmentCollected;
                GameManager.Instance.OnAllLightFragmentsCollected += OnAllCollected;
                
                // Update initial state
                UpdateDisplay(GameManager.Instance.LightFragmentsCollected, GameManager.Instance.TotalLightFragments);
            }
        }
        
        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLightFragmentCollected -= OnFragmentCollected;
                GameManager.Instance.OnAllLightFragmentsCollected -= OnAllCollected;
            }
        }
        
        private void Update()
        {
            // Pulse animation khi chÆ°a thu tháº­p Ä‘á»§
            if (!allCollected && fragmentCountText != null)
            {
                float alpha = 0.7f + 0.3f * Mathf.Sin(Time.time * pulseSpeed);
                fragmentCountText.alpha = alpha;
            }
            
            // Notification fade out
            if (notificationGroup != null && notificationTimer > 0)
            {
                notificationTimer -= Time.deltaTime;
                if (notificationTimer <= 0.5f)
                {
                    notificationGroup.alpha = notificationTimer / 0.5f;
                }
                
                if (notificationTimer <= 0)
                {
                    notificationGroup.alpha = 0;
                }
            }
        }
        
        private void OnFragmentCollected(int current, int total)
        {
            UpdateDisplay(current, total);
            ShowNotification($"Light Fragment  {current}/{total}");
            
            // Pulse effect
            if (fragmentCountText != null)
            {
                StartCoroutine(PulseEffect());
            }
        }
        
        private void OnAllCollected()
        {
            allCollected = true;
            if (fragmentCountText != null)
            {
                fragmentCountText.color = completeColor;
                fragmentCountText.alpha = 1f;
            }
            ShowNotification("All Fragments Collected!\nFlash of Truth Unlocked!");
        }
        
        private void UpdateDisplay(int current, int total)
        {
            if (fragmentCountText != null)
            {
                fragmentCountText.text = $"<sprite=0> {current}/{total}";
                // Fallback if no sprite asset
                if (fragmentCountText.spriteAsset == null)
                {
                    fragmentCountText.text = $"* {current}/{total}";
                }
            }
        }
        
        private void ShowNotification(string message)
        {
            if (notificationText != null)
            {
                notificationText.text = message;
                notificationTimer = notificationDuration;
                if (notificationGroup != null)
                    notificationGroup.alpha = 1f;
            }
            

        }
        
        private System.Collections.IEnumerator PulseEffect()
        {
            Vector3 originalScale = fragmentCountText.transform.localScale;
            Vector3 targetScale = originalScale * 1.3f;
            float duration = 0.3f;
            float elapsed = 0f;
            
            // Scale up
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2f);
                fragmentCountText.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }
            
            // Scale down
            elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2f);
                fragmentCountText.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }
            
            fragmentCountText.transform.localScale = originalScale;
        }
        
        /// <summary>
        /// Táº¡o UI elements tá»± Ä‘á»™ng
        /// </summary>
        private void CreateFragmentUI()
        {
            // TÃ¬m hoáº·c táº¡o Canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }
            if (canvas == null)
            {
                var canvasObj = new GameObject("UICanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            // Container cho fragment counter (top-right)
            var container = new GameObject("LightFragmentCounter");
            container.transform.SetParent(canvas.transform, false);
            
            var containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1f, 1f); // Top-right
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.pivot = new Vector2(1f, 1f);
            containerRect.anchoredPosition = new Vector2(-20f, -20f);
            containerRect.sizeDelta = new Vector2(200f, 50f);
            
            // Background
            var bg = container.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.5f);
            
            // Fragment count text
            var textObj = new GameObject("FragmentText");
            textObj.transform.SetParent(container.transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 5f);
            textRect.offsetMax = new Vector2(-10f, -5f);
            
            fragmentCountText = textObj.AddComponent<TextMeshProUGUI>();
            fragmentCountText.text = "* 0/3";
            fragmentCountText.fontSize = 24;
            fragmentCountText.color = normalColor;
            fragmentCountText.alignment = TextAlignmentOptions.MidlineRight;
            fragmentCountText.fontStyle = FontStyles.Bold;
            
            // Notification (center-top)
            var notifObj = new GameObject("FragmentNotification");
            notifObj.transform.SetParent(canvas.transform, false);
            
            var notifRect = notifObj.AddComponent<RectTransform>();
            notifRect.anchorMin = new Vector2(0.5f, 0.8f);
            notifRect.anchorMax = new Vector2(0.5f, 0.8f);
            notifRect.pivot = new Vector2(0.5f, 0.5f);
            notifRect.anchoredPosition = Vector2.zero;
            notifRect.sizeDelta = new Vector2(400f, 60f);
            
            notificationGroup = notifObj.AddComponent<CanvasGroup>();
            notificationGroup.alpha = 0;
            
            var notifBg = notifObj.AddComponent<Image>();
            notifBg.color = new Color(0, 0, 0, 0.7f);
            
            var notifTextObj = new GameObject("NotifText");
            notifTextObj.transform.SetParent(notifObj.transform, false);
            
            var notifTextRect = notifTextObj.AddComponent<RectTransform>();
            notifTextRect.anchorMin = Vector2.zero;
            notifTextRect.anchorMax = Vector2.one;
            notifTextRect.offsetMin = new Vector2(10f, 5f);
            notifTextRect.offsetMax = new Vector2(-10f, -5f);
            
            notificationText = notifTextObj.AddComponent<TextMeshProUGUI>();
            notificationText.text = "";
            notificationText.fontSize = 20;
            notificationText.color = normalColor;
            notificationText.alignment = TextAlignmentOptions.Center;
            

        }
    }
}
