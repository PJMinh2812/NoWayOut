using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Afterimage (bóng ma) tạo ra khi Dash.
    /// Tự fade out rồi destroy.
    /// </summary>
    public class DashAfterImage : MonoBehaviour
    {
        private float _lifetime;
        private float _elapsed;
        private Color _startColor;
        private SpriteRenderer _sr;

        /// <summary>Gọi ngay sau AddComponent</summary>
        public void Init(float lifetime, Color startColor)
        {
            _lifetime = lifetime;
            _startColor = startColor;
            _elapsed = 0f;
            _sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _lifetime);

            if (_sr != null)
            {
                // Fade alpha từ startColor -> 0
                var c = _startColor;
                c.a = Mathf.Lerp(_startColor.a, 0f, t);
                _sr.color = c;

                // Scale nhỏ dần nhẹ
                float scale = Mathf.Lerp(1f, 0.85f, t);
                transform.localScale = transform.localScale.normalized * scale;
            }

            if (_elapsed >= _lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
