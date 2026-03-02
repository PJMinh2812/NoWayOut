using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace NWO.UI
{
    public class Enemy2DHealthBarController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private Image ghostImage;

        [Header("Position")]
        [Tooltip("Offset so voi vi tri enemy (world space)")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, 0f);

        [Header("Settings")]
        [SerializeField] private float fillSpeed = 6f;
        [SerializeField] private float ghostSpeed = 2f;
        [SerializeField] private float ghostDelay = 0.4f;
        [SerializeField] private float visibleDuration = 3f;

        private Transform _targetTransform;
        private float _targetFill;
        private float _currentFill;
        private float _ghostFill;
        private float _ghostDelayTimer;
        private Coroutine _hideCoroutine;

        private void Awake()
        {
            if (fillImage == null)
            {
                var images = GetComponentsInChildren<Image>(includeInactive: true);
                foreach (var img in images)
                {
                    if (img.gameObject.name.ToLower().Contains("fill"))
                        fillImage = img;
                    else if (img.gameObject.name.ToLower().Contains("ghost"))
                        ghostImage = img;
                }
            }
        }

        public void SetTarget(NWO.Enemy2D enemy)
        {
            InitBar(enemy.transform);
        }

        public void SetTarget(NWO.RatMiniBoss boss)
        {
            InitBar(boss.transform);
        }

        private void InitBar(Transform target)
        {
            _targetTransform = target;

            if (fillImage == null)
            {
                Debug.LogError("[Enemy2DHealthBarController] Fill Image chua duoc gan!", this);
                return;
            }

            _targetFill = 1f;
            _currentFill = 1f;
            _ghostFill = 1f;
            fillImage.fillAmount = 1f;
            if (ghostImage != null) ghostImage.fillAmount = 1f;

            // Dat vi tri ngay lap tuc truoc khi an
            transform.position = _targetTransform.position + offset;
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_targetTransform == null)
            {
                Destroy(gameObject);
                return;
            }

            // Luon cap nhat vi tri theo enemy (world space) - hoat dong ca khi la child
            transform.position = _targetTransform.position + offset;

            if (fillImage == null) return;

            // Thanh do tut nhanh
            _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * fillSpeed);
            fillImage.fillAmount = _currentFill;

            // Thanh ghost tut cham sau delay
            if (ghostImage != null)
            {
                if (_ghostDelayTimer > 0f)
                    _ghostDelayTimer -= Time.deltaTime;
                else
                    _ghostFill = Mathf.Lerp(_ghostFill, _targetFill, Time.deltaTime * ghostSpeed);

                ghostImage.fillAmount = _ghostFill;
            }
        }

        public void OnHealthChanged(int current, int max)
        {
            if (fillImage == null) return;

            _targetFill = (float)current / max;
            _ghostDelayTimer = ghostDelay;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (_hideCoroutine != null)
                StopCoroutine(_hideCoroutine);
            _hideCoroutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(visibleDuration);
            gameObject.SetActive(false);
        }

        public void OnEnemyDied()
        {
            Destroy(gameObject);
        }
    }
}
