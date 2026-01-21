using UnityEngine;
using Cinemachine;
using System.Collections;

namespace SoulKnightClone.Core
{
    /// <summary>
    /// Utility class để tạo hiệu ứng rung màn hình khi bắn hoặc nhận sát thương
    /// Sử dụng Cinemachine Impulse Source
    /// </summary>
    public class CameraShaker : MonoBehaviour
    {
        public static CameraShaker Instance { get; private set; }

        [Header("Cinemachine")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        private CinemachineBasicMultiChannelPerlin noise;

        [Header("Shake Settings")]
        [SerializeField] private float defaultIntensity = 1f;
        [SerializeField] private float defaultDuration = 0.1f;

        private Coroutine shakeCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Lấy Cinemachine noise component
            if (virtualCamera != null)
            {
                noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            }
        }

        /// <summary>
        /// Rung màn hình với intensity và duration tùy chỉnh
        /// </summary>
        public void ShakeCamera(float intensity, float duration)
        {
            if (noise == null) return;

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }

            shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
        }

        /// <summary>
        /// Rung màn hình với giá trị mặc định
        /// </summary>
        public void ShakeCamera()
        {
            ShakeCamera(defaultIntensity, defaultDuration);
        }

        private IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            noise.m_AmplitudeGain = intensity;
            yield return new WaitForSeconds(duration);
            noise.m_AmplitudeGain = 0f;
            shakeCoroutine = null;
        }

        /// <summary>
        /// Dừng shake ngay lập tức
        /// </summary>
        public void StopShake()
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }

            if (noise != null)
            {
                noise.m_AmplitudeGain = 0f;
            }
        }
    }
}
