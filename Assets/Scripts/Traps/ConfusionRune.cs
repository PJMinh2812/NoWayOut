using UnityEngine;
using NWO;

namespace NWO
{
    /// <summary>
    /// Gạch Đảo Ngược (Confusion Rune) - Đảo ngược điều khiển trong thời gian ngắn
    /// Triết lý: "Phản xạ quen tay khiến người chơi tự lao xuống hố"
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ConfusionRune : MonoBehaviour
    {
        [Header("Confusion Settings")]
        [Tooltip("Thời gian đảo ngược điều khiển (giây)")]
        [SerializeField] private float confusionDuration = 5f;
        
        [Tooltip("Có one-time trigger không? (false = trigger mỗi lần đạp vào)")]
        [SerializeField] private bool oneTimeUse = false;
        
        [Tooltip("Cooldown giữa các lần trigger (nếu không phải one-time)")]
        [SerializeField] private float triggerCooldown = 3f;

        [Header("Visual Effects")]
        [SerializeField] private Color runeColor = new Color(1f, 0.5f, 1f, 0.6f);
        [SerializeField] private bool pulseEffect = true;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private GameObject activationVFX;

        [Header("Audio")]
        [SerializeField] private AudioClip activationSound;

        private SpriteRenderer _spriteRenderer;
        private AudioSource _audioSource;
        private Color _originalColor;
        private bool _isUsed = false;
        private float _cooldownTimer = 0f;
        private float _pulseTime = 0f;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _audioSource = GetComponent<AudioSource>();
            
            // Đảm bảo collider là trigger
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
                _spriteRenderer.color = Color.Lerp(_originalColor, runeColor, 0.7f);
            }
        }

        private void Update()
        {
            // Update cooldown
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }

            // Pulse effect
            if (pulseEffect && _spriteRenderer != null && !_isUsed)
            {
                _pulseTime += Time.deltaTime * pulseSpeed;
                float pulse = (Mathf.Sin(_pulseTime) + 1f) / 2f; // 0-1
                float alpha = Mathf.Lerp(0.4f, 0.8f, pulse);
                var color = _spriteRenderer.color;
                color.a = alpha;
                _spriteRenderer.color = color;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_isUsed) return;
            if (_cooldownTimer > 0f) return;

            if (collision.CompareTag("Player"))
            {
                ApplyConfusion(collision.gameObject);
            }
        }

        private void ApplyConfusion(GameObject player)
        {
            // Nếu player đang Dash → bất tử, bỏ qua confusion
            var playerCtrl = player.GetComponent<PlayerController2D>();
            if (playerCtrl != null && playerCtrl.IsDashing) return;

            var statusEffects = player.GetComponent<PlayerStatusEffects>();
            
            if (statusEffects == null)
            {
                Debug.LogWarning("[ConfusionRune] Player doesn't have PlayerStatusEffects component!");
                return;
            }

            RunAIDirectorTelemetry.RecordTrapTriggered(this);

            // Áp dụng confusion effect
            statusEffects.ApplyEffect(StatusEffectType.Confusion, confusionDuration, 1f, this);
            
            Debug.Log($"[ConfusionRune] Applied confusion for {confusionDuration}s!");

            // Play effects
            PlayActivationEffects();

            // Handle one-time use
            if (oneTimeUse)
            {
                _isUsed = true;
                
                // Fade out hoặc disable
                if (_spriteRenderer != null)
                {
                    var color = _spriteRenderer.color;
                    color.a = 0.2f;
                    _spriteRenderer.color = color;
                }
            }
            else
            {
                _cooldownTimer = triggerCooldown;
            }
        }

        private void PlayActivationEffects()
        {
            // Sound
            if (_audioSource != null && activationSound != null)
            {
                _audioSource.PlayOneShot(activationSound);
            }
            else if (activationSound != null)
            {
                AudioSource.PlayClipAtPoint(activationSound, transform.position);
            }

            // VFX
            if (activationVFX != null)
            {
                Instantiate(activationVFX, transform.position, Quaternion.identity);
            }
        }

        /// <summary>
        /// Reset trap về trạng thái ban đầu
        /// </summary>
        public void ResetTrap()
        {
            _isUsed = false;
            _cooldownTimer = 0f;
            
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.Lerp(_originalColor, runeColor, 0.7f);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 1f, 0.3f);
            
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                Gizmos.DrawCube(collider.bounds.center, collider.bounds.size);
            }
            else
            {
                Gizmos.DrawCube(transform.position, Vector3.one);
            }
        }
    }
}
