using UnityEngine;
using System.Collections;

namespace GloomCraft
{
    /// <summary>
    /// Camera shake effect - Hiệu ứng rung camera khi có va chạm hoặc bẫy kích hoạt
    /// Attach script này vào Main Camera
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        private Vector3 originalPosition;
        private bool isShaking = false;
        
        private void Start()
        {
            originalPosition = transform.localPosition;
        }
        
        /// <summary>
        /// Kích hoạt hiệu ứng rung camera
        /// </summary>
        /// <param name="duration">Thời gian rung (giây)</param>
        /// <param name="magnitude">Cường độ rung (0.1-1.0)</param>
        public void Shake(float duration, float magnitude)
        {
            if (!isShaking)
            {
                StartCoroutine(ShakeCoroutine(duration, magnitude));
            }
        }
        
        private IEnumerator ShakeCoroutine(float duration, float magnitude)
        {
            isShaking = true;
            originalPosition = transform.localPosition;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                
                transform.localPosition = new Vector3(
                    originalPosition.x + x, 
                    originalPosition.y + y, 
                    originalPosition.z
                );
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.localPosition = originalPosition;
            isShaking = false;
        }
        
        /// <summary>
        /// Rung camera với intensity giảm dần (trauma-based shake)
        /// </summary>
        public void ShakeWithTrauma(float trauma)
        {
            float duration = trauma * 0.5f; // Trauma 1.0 = rung 0.5 giây
            float magnitude = trauma * 0.3f; // Trauma 1.0 = magnitude 0.3
            Shake(duration, magnitude);
        }
    }
}
