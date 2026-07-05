using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace NWO.UI
{
    /// <summary>
    /// Floating damage number - hiển thị số damage bay lên khi quái bị đánh.
    /// Sử dụng object pooling để tránh GC allocation.
    /// Tự động tạo TextMeshPro component, không cần prefab.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float lifetime = 0.8f;
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float spreadRange = 0.3f;
        [SerializeField] private float scaleStart = 0.6f;
        [SerializeField] private float scalePeak = 1.2f;
        [SerializeField] private float scaleEnd = 0.4f;
        [SerializeField] private float peakTime = 0.15f;

        [Header("Colors")]
        [SerializeField] private Color normalDamageColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.85f, 0f, 1f);
        [SerializeField] private int criticalThreshold = 15;

        private TextMeshPro _textMesh;
        private float _elapsed;
        private Vector3 _velocity;
        private Color _startColor;
        private float _currentLifetime;

        // === Object Pool ===
        private static readonly Queue<DamagePopup> _pool = new Queue<DamagePopup>();
        private static bool _poolInitialized;
        private const int POOL_SIZE = 30;

        /// <summary>
        /// Khởi tạo pool. Gọi 1 lần khi game bắt đầu.
        /// </summary>
        public static void WarmPool()
        {
            if (_poolInitialized) return;
            _poolInitialized = true;

            for (int i = 0; i < POOL_SIZE; i++)
            {
                var instance = CreateInstance();
                instance.gameObject.SetActive(false);
                _pool.Enqueue(instance);
            }
        }

        private static DamagePopup CreateInstance()
        {
            var go = new GameObject("DamagePopup", typeof(TextMeshPro), typeof(DamagePopup));
            var popup = go.GetComponent<DamagePopup>();
            popup._textMesh = go.GetComponent<TextMeshPro>();

            // Setup TextMeshPro
            popup._textMesh.alignment = TextAlignmentOptions.Center;
            popup._textMesh.fontSize = 4f;
            popup._textMesh.fontStyle = FontStyles.Bold;
            popup._textMesh.sortingOrder = 100;
            popup._textMesh.textWrappingMode = TextWrappingModes.NoWrap;
            popup._textMesh.overflowMode = TextOverflowModes.Overflow;
            popup._textMesh.raycastTarget = false;

            // Outline for visibility
            popup._textMesh.outlineWidth = 0.25f;
            popup._textMesh.outlineColor = new Color32(0, 0, 0, 180);

            DontDestroyOnLoad(go);
            return popup;
        }

        /// <summary>
        /// Spawn một damage popup tại vị trí world space.
        /// </summary>
        public static void Spawn(Vector3 worldPosition, int damage)
        {
            if (!_poolInitialized) WarmPool();

            DamagePopup instance;
            if (_pool.Count > 0)
            {
                instance = _pool.Dequeue();
                instance.gameObject.SetActive(true);
            }
            else
            {
                instance = CreateInstance();
            }

            instance.Setup(worldPosition, damage);
        }

        private void Setup(Vector3 position, int damage)
        {
            // Vị trí random nhẹ để nhiều hit không chồng lên nhau
            float offsetX = Random.Range(-spreadRange, spreadRange);
            float offsetY = Random.Range(0.3f, 0.6f);
            transform.position = position + new Vector3(offsetX, offsetY, 0f);

            // Hướng bay lên + hơi lệch ngang
            _velocity = new Vector3(Random.Range(-0.5f, 0.5f), floatSpeed, 0f);

            _elapsed = 0f;
            _currentLifetime = lifetime;

            // Màu sắc theo mức damage
            bool isCritical = damage >= criticalThreshold;
            _startColor = isCritical ? criticalColor : normalDamageColor;

            if (_textMesh == null)
                _textMesh = GetComponent<TextMeshPro>();

            _textMesh.text = damage.ToString();
            _textMesh.color = _startColor;
            _textMesh.fontSize = isCritical ? 5.5f : 4f;

            // Scale nhỏ ban đầu
            transform.localScale = Vector3.one * scaleStart;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / _currentLifetime;

            if (t >= 1f)
            {
                ReturnToPool();
                return;
            }

            // Di chuyển lên + giảm tốc dần
            float speedFactor = 1f - t * 0.6f;
            transform.position += _velocity * speedFactor * Time.deltaTime;

            // Scale: nhỏ → lớn → nhỏ (punch effect)
            float peakT = peakTime / _currentLifetime;
            float scale;
            if (t < peakT)
            {
                // Phase 1: grow
                scale = Mathf.Lerp(scaleStart, scalePeak, t / peakT);
            }
            else
            {
                // Phase 2: shrink
                float shrinkT = (t - peakT) / (1f - peakT);
                scale = Mathf.Lerp(scalePeak, scaleEnd, shrinkT);
            }
            transform.localScale = Vector3.one * scale;

            // Fade out trong nửa cuối
            if (t > 0.5f)
            {
                float fadeT = (t - 0.5f) / 0.5f;
                Color c = _startColor;
                c.a = Mathf.Lerp(1f, 0f, fadeT);
                _textMesh.color = c;
            }
        }

        private void ReturnToPool()
        {
            gameObject.SetActive(false);
            _pool.Enqueue(this);
        }
    }
}
