using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NWO
{
    /// <summary>
    /// Điều phối toàn bộ cinematic intro boss:
    ///   1. Đóng băng player
    ///   2. Camera pan + zoom về phía boss
    ///   3. Boss phát animation Roar
    ///   4. Camera rung + Boss health bar xuất hiện
    ///   5. Camera trở về player, bắt đầu chiến đấu
    ///
    /// SETUP INSPECTOR:
    ///   - Gắn script này vào một GameObject trống trong boss room
    ///   - Kéo các tham chiếu cần thiết vào: bossTransform, bossAnimator, bossHealthBarUI
    ///   - Thêm BossRoomTrigger vào một Collider2D (IsTrigger = true) ở cửa phòng boss
    /// </summary>
    public class BossCinematicController : MonoBehaviour
    {
        // ------------------------------------------------------------------ //
        //  Inspector references                                               //
        // ------------------------------------------------------------------ //

        [Header("Boss")]
        [Tooltip("Transform của boss (dùng để camera pan tới)")]
        [SerializeField] private Transform bossTransform;

        [Tooltip("Animator của boss (để trigger Roar)")]
        [SerializeField] private Animator bossAnimator;

        [Tooltip("Tên trigger trong Animator của boss")]
        [SerializeField] private string roarTriggerName = "Roar";

        [Tooltip("Component Enemy2D của boss (nếu boss dùng Enemy2D)")]
        [SerializeField] private Enemy2D enemy2DBoss;

        [Tooltip("Component RatMiniBoss của boss (nếu boss dùng RatMiniBoss)")]
        [SerializeField] private RatMiniBoss ratMiniBoss;

        [Tooltip("Component GoatManBoss của boss (nếu boss dùng GoatManBoss)")]
        [SerializeField] private GoatManBoss goatManBoss;

        [Tooltip("Tên hiển thị của boss trên health bar")]
        [SerializeField] private string bossDisplayName = "Boss";

        [Header("UI")]
        [Tooltip("Boss health bar screen-space (BossHealthBarUI trên Canvas)")]
        [SerializeField] private UI.BossHealthBarUI bossHealthBarUI;

        [Tooltip("(Tùy chọn) Hai thanh đen letterbox trên/dưới (Image)")]
        [SerializeField] private CanvasGroup letterboxCanvasGroup;

        [Header("Camera Settings")]
        [Tooltip("Orthographic size khi zoom vào boss (nhỏ = zoom nhiều hơn)")]
        [SerializeField] private float bossZoomSize = 3f;

        [Tooltip("Orthographic size bình thường khi chiến đấu")]
        [SerializeField] private float normalZoomSize = 5f;

        [Tooltip("Thời gian camera di chuyển từ player đến boss (giây)")]
        [SerializeField] private float panToBossDuration = 1.5f;

        [Tooltip("Thời gian giữ camera trên boss sau khi roar")]
        [SerializeField] private float holdOnBossDuration = 2.0f;

        [Tooltip("Thời gian camera trở về player (giây)")]
        [SerializeField] private float panToPlayerDuration = 1.2f;

        [Tooltip("Letterbox fade duration (giây)")]
        [SerializeField] private float letterboxFadeDuration = 0.4f;

        [Header("Camera Shake on Roar")]
        [SerializeField] private float roarShakeDuration = 0.7f;
        [SerializeField] private float roarShakeMagnitude = 0.25f;

        // ------------------------------------------------------------------ //
        //  Private state                                                      //
        // ------------------------------------------------------------------ //

        private bool _played = false;

        private CameraFollow2D _cameraFollow;
        private CameraShake _cameraShake;
        private PlayerController2D _player;
        private PlayerMeleeController _playerMelee;
        private PlayerShooter2D _playerShooter;
        private PlayerSpellController _playerSpell;

        // ------------------------------------------------------------------ //
        //  Lifecycle                                                          //
        // ------------------------------------------------------------------ //

        private void Start()
        {
            _cameraFollow = FindFirstObjectByType<CameraFollow2D>();
            _cameraShake  = FindFirstObjectByType<CameraShake>();
            _player       = FindFirstObjectByType<PlayerController2D>();

            if (_player != null)
            {
                _playerMelee  = _player.GetComponent<PlayerMeleeController>();
                _playerShooter = _player.GetComponent<PlayerShooter2D>();
                _playerSpell  = _player.GetComponent<PlayerSpellController>();
            }

            // Ẩn health bar ngay từ đầu
            if (bossHealthBarUI != null)
                bossHealthBarUI.gameObject.SetActive(false);

            // Ẩn letterbox
            if (letterboxCanvasGroup != null)
                letterboxCanvasGroup.alpha = 0f;
        }

        // ------------------------------------------------------------------ //
        //  Public API – gọi từ BossRoomTrigger                               //
        // ------------------------------------------------------------------ //

        /// <summary>Bắt đầu chuỗi cinematic intro boss.</summary>
        public void PlayCinematic()
        {
            if (_played) return;
            _played = true;
            StartCoroutine(CinematicSequence());
        }

        // ------------------------------------------------------------------ //
        //  Cinematic coroutine                                                //
        // ------------------------------------------------------------------ //

        private IEnumerator CinematicSequence()
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;

            // 1 ── Đóng băng player ─────────────────────────────────────────
            SetPlayerEnabled(false);

            // Dừng chuyển động
            var rb = _player != null ? _player.GetComponent<Rigidbody2D>() : null;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            // 2 ── Letterbox fade in ────────────────────────────────────────
            if (letterboxCanvasGroup != null)
                yield return StartCoroutine(FadeCanvasGroup(letterboxCanvasGroup, 0f, 1f, letterboxFadeDuration));

            // 3 ── Camera ngắt follow, pan + zoom về boss ───────────────────
            if (_cameraFollow != null) _cameraFollow.enabled = false;

            Vector3 bossViewPos = new Vector3(
                bossTransform.position.x,
                bossTransform.position.y,
                cam.transform.position.z   // giữ nguyên Z (depth)
            );
            yield return StartCoroutine(MoveCamera(cam, bossViewPos, bossZoomSize, panToBossDuration));

            // 4 ── Boss roar animation ──────────────────────────────────────
            if (bossAnimator != null && !string.IsNullOrEmpty(roarTriggerName))
                bossAnimator.SetTrigger(roarTriggerName);

            // 5 ── Camera shake ─────────────────────────────────────────────
            _cameraShake?.Shake(roarShakeDuration, roarShakeMagnitude);

            // 6 ── Boss health bar xuất hiện ────────────────────────────────
            WireHealthBar();
            bossHealthBarUI?.Show(bossDisplayName);

            yield return new WaitForSeconds(holdOnBossDuration);

            // 7 ── Camera pan trở về player ─────────────────────────────────
            if (_player != null)
            {
                Vector3 playerViewPos = new Vector3(
                    _player.transform.position.x,
                    _player.transform.position.y,
                    cam.transform.position.z
                );
                yield return StartCoroutine(MoveCamera(cam, playerViewPos, normalZoomSize, panToPlayerDuration));
            }

            // Bật lại camera follow hướng tới player
            if (_cameraFollow != null && _player != null)
            {
                _cameraFollow.SetTarget(_player.transform);
                _cameraFollow.enabled = true;
            }

            // 8 ── Letterbox fade out ───────────────────────────────────────
            if (letterboxCanvasGroup != null)
                yield return StartCoroutine(FadeCanvasGroup(letterboxCanvasGroup, 1f, 0f, letterboxFadeDuration));

            // 9 ── Mở khoá player, bắt đầu chiến đấu ──────────────────────
            SetPlayerEnabled(true);
        }

        // ------------------------------------------------------------------ //
        //  Camera helpers                                                     //
        // ------------------------------------------------------------------ //

        private IEnumerator MoveCamera(Camera cam, Vector3 targetPos, float targetZoom, float duration)
        {
            float startZoom = cam.orthographicSize;
            Vector3 startPos = cam.transform.position;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
                cam.orthographicSize    = Mathf.Lerp(startZoom, targetZoom, t);
                yield return null;
            }

            cam.transform.position = targetPos;
            cam.orthographicSize    = targetZoom;
        }

        // ------------------------------------------------------------------ //
        //  UI helpers                                                         //
        // ------------------------------------------------------------------ //

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            cg.alpha = from;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            cg.alpha = to;
        }

        /// <summary>Kết nối boss health bar với đúng loại boss.</summary>
        private void WireHealthBar()
        {
            if (bossHealthBarUI == null) return;

            if (enemy2DBoss != null)
                bossHealthBarUI.AttachBoss(enemy2DBoss);
            else if (ratMiniBoss != null)
                bossHealthBarUI.AttachBoss(ratMiniBoss);
            else if (goatManBoss != null)
                bossHealthBarUI.AttachBoss(goatManBoss);
        }

        // ------------------------------------------------------------------ //
        //  Player enable / disable                                            //
        // ------------------------------------------------------------------ //

        private void SetPlayerEnabled(bool value)
        {
            if (_player != null)        _player.enabled        = value;
            if (_playerMelee != null)   _playerMelee.enabled   = value;
            if (_playerShooter != null) _playerShooter.enabled = value;
            if (_playerSpell != null)   _playerSpell.enabled   = value;
        }
    }
}
