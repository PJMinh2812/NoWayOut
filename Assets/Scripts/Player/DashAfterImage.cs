using UnityEngine;
using System.Collections.Generic;

namespace NWO
{
    /// <summary>
    /// Afterimage (bóng ma) tạo ra khi Dash.
    /// Sử dụng object pooling thay vì Instantiate/Destroy để giảm GC.
    /// </summary>
    public class DashAfterImage : MonoBehaviour
    {
        private float _lifetime;
        private float _elapsed;
        private Color _startColor;
        private SpriteRenderer _sr;
        private Vector3 _initialScale;

        // === Object Pool ===
        private static readonly Queue<DashAfterImage> _pool = new Queue<DashAfterImage>();
        private const int POOL_INITIAL_SIZE = 20;
        private static bool _poolInitialized = false;

        public static void WarmPool()
        {
            if (_poolInitialized) return;
            _poolInitialized = true;
            for (int i = 0; i < POOL_INITIAL_SIZE; i++)
            {
                var go = new GameObject("DashAfterImage_Pooled");
                var sr = go.AddComponent<SpriteRenderer>();
                var dai = go.AddComponent<DashAfterImage>();
                dai._sr = sr;
                go.SetActive(false);
                _pool.Enqueue(dai);
            }
        }

        public static DashAfterImage GetFromPool(Vector3 position, Quaternion rotation, Vector3 scale,
            Sprite sprite, Color color, bool flipX, int sortingLayerID, int sortingOrder, float lifetime)
        {
            DashAfterImage instance;
            if (_pool.Count > 0)
            {
                instance = _pool.Dequeue();
                instance.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("DashAfterImage_Pooled");
                var sr = go.AddComponent<SpriteRenderer>();
                instance = go.AddComponent<DashAfterImage>();
                instance._sr = sr;
            }

            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = scale;
            instance._initialScale = scale;

            instance._sr.sprite = sprite;
            instance._sr.color = color;
            instance._sr.flipX = flipX;
            instance._sr.sortingLayerID = sortingLayerID;
            instance._sr.sortingOrder = sortingOrder;

            instance._lifetime = lifetime;
            instance._startColor = color;
            instance._elapsed = 0f;
            return instance;
        }

        private void ReturnToPool()
        {
            gameObject.SetActive(false);
            _pool.Enqueue(this);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _lifetime);

            if (_sr != null)
            {
                var c = _startColor;
                c.a = Mathf.Lerp(_startColor.a, 0f, t);
                _sr.color = c;

                float scale = Mathf.Lerp(1f, 0.85f, t);
                transform.localScale = _initialScale * scale;
            }

            if (_elapsed >= _lifetime)
            {
                ReturnToPool();
            }
        }
    }
}
