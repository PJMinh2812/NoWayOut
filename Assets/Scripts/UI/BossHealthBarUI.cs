using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NWO.UI
{
    /// <summary>
    /// Screen-space health bar dành riêng cho boss.
    /// Xuất hiện bằng hiệu ứng fade-in trong cinematic, theo dõi máu boss trong suốt trận đấu.
    ///
    /// SETUP INSPECTOR:
    ///   1. Tạo Canvas (Screen Space - Overlay) → thêm một GameObject con tên "BossHealthBar"
    ///   2. Gắn script này vào "BossHealthBar"
    ///   3. Kéo CanvasGroup, FillImage, GhostImage, BossNameText vào đây
    ///   4. Kéo BossHealthBarUI này vào BossCinematicController.bossHealthBarUI
    /// </summary>
    public class BossHealthBarUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image ghostImage;        // thanh bóng trắng đỏ sau khi mất máu
        [SerializeField] private Text bossNameText;

        [Header("Fill Settings")]
        [SerializeField] private float fillSpeed  = 8f;
        [SerializeField] private float ghostSpeed = 2f;
        [SerializeField] private float ghostDelay = 0.5f;

        [Header("Colors")]
        [SerializeField] private Color fillHighColor = new Color(0.85f, 0.15f, 0.15f);   // đỏ tươi
        [SerializeField] private Color fillLowColor  = new Color(0.40f, 0.05f, 0.05f);   // đỏ tối khi máu thấp
        [SerializeField] private float lowHealthThreshold = 0.3f;                         // dưới 30% đổi màu

        [Header("Fade")]
        [SerializeField] private float fadeInDuration  = 0.8f;
        [SerializeField] private float fadeOutDuration = 1.0f;

        // ------------------------------------------------------------------ //
        //  Private state                                                      //
        // ------------------------------------------------------------------ //

        private float _targetFill  = 1f;
        private float _currentFill = 1f;
        private float _ghostFill   = 1f;
        private float _ghostDelayTimer;

        // Delegate để lấy health của boss (dù là Enemy2D hay RatMiniBoss)
        private Func<int> _getHealth;
        private Func<int> _getMaxHealth;

        // ------------------------------------------------------------------ //
        //  Lifecycle                                                          //
        // ------------------------------------------------------------------ //

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            // Đảm bảo ẩn ngay khi khởi động
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private void Update()
        {
            // Poll health từ boss nếu còn sống
            PollBossHealth();

            // Smooth fill animation
            _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * fillSpeed);
            if (fillImage != null)
            {
                fillImage.fillAmount = _currentFill;
                fillImage.color = Color.Lerp(fillLowColor, fillHighColor,
                    Mathf.InverseLerp(0f, lowHealthThreshold, _currentFill));
            }

            // Ghost bar
            if (ghostImage != null)
            {
                if (_ghostDelayTimer > 0f)
                    _ghostDelayTimer -= Time.deltaTime;
                else
                    _ghostFill = Mathf.Lerp(_ghostFill, _targetFill, Time.deltaTime * ghostSpeed);

                ghostImage.fillAmount = _ghostFill;
            }
        }

        // ------------------------------------------------------------------ //
        //  Public API                                                         //
        // ------------------------------------------------------------------ //

        /// <summary>Liên kết boss loại Enemy2D với health bar này.</summary>
        public void AttachBoss(Enemy2D boss)
        {
            _getHealth    = () => boss.GetCurrentHealth();
            _getMaxHealth = () => boss.GetMaxHealth();
            ResetBar();
        }

        /// <summary>Liên kết boss loại RatMiniBoss với health bar này.</summary>
        public void AttachBoss(RatMiniBoss boss)
        {
            _getHealth    = () => boss.GetCurrentHealth();
            _getMaxHealth = () => boss.GetMaxHealth();
            ResetBar();
        }

        /// <summary>Hiển thị health bar với hiệu ứng fade-in.</summary>
        public void Show(string displayName = "")
        {
            if (!string.IsNullOrEmpty(displayName) && bossNameText != null)
                bossNameText.text = displayName;

            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(0f, 1f, fadeInDuration));
        }

        /// <summary>Ẩn health bar với hiệu ứng fade-out (gọi khi boss chết).</summary>
        public void Hide()
        {
            StopAllCoroutines();
            StartCoroutine(HideCoroutine());
        }

        // ------------------------------------------------------------------ //
        //  Private helpers                                                    //
        // ------------------------------------------------------------------ //

        private void PollBossHealth()
        {
            if (_getHealth == null || _getMaxHealth == null) return;

            int max = _getMaxHealth();
            if (max <= 0) return;

            float normalized = Mathf.Clamp01((float)_getHealth() / max);

            // Chỉ update ghost timer khi máu giảm
            if (normalized < _targetFill)
                _ghostDelayTimer = ghostDelay;

            _targetFill = normalized;
        }

        private void ResetBar()
        {
            _targetFill  = 1f;
            _currentFill = 1f;
            _ghostFill   = 1f;
            if (fillImage  != null) fillImage.fillAmount  = 1f;
            if (ghostImage != null) ghostImage.fillAmount = 1f;
        }

        private IEnumerator FadeCanvasGroup(float from, float to, float duration)
        {
            if (canvasGroup == null) yield break;
            canvasGroup.alpha = from;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = to;
        }

        private IEnumerator HideCoroutine()
        {
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup != null ? canvasGroup.alpha : 1f, 0f, fadeOutDuration));
            gameObject.SetActive(false);
        }
    }
}
