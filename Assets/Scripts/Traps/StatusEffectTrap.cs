using UnityEngine;
using NWO;

namespace NWO
{
    /// <summary>
    /// Generic trap có thể áp dụng bất kỳ status effect nào
    /// Có thể config trong Inspector để tạo nhiều loại bẫy khác nhau
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class StatusEffectTrap : MonoBehaviour
    {
        [Header("Effect Configuration")]
        [Tooltip("Loại status effect sẽ áp dụng")]
        [SerializeField] private StatusEffectType effectType = StatusEffectType.Slow;
        
        [Tooltip("Thời gian effect kéo dài (giây)")]
        [SerializeField] private float effectDuration = 3f;
        
        [Tooltip("Cường độ effect (0-1 cho debuff, >0 cho buff)")]
        [Range(0f, 2f)]
        [SerializeField] private float effectIntensity = 0.5f;

        [Header("DoT Settings (Burn/Poison only)")]
        [Tooltip("Damage mỗi tick (chỉ cho Burn/Poison)")]
        [SerializeField] private float dotDamagePerTick = 5f;
        
        [Tooltip("Thời gian giữa các tick")]
        [SerializeField] private float dotTickInterval = 0.5f;

        [Header("Trigger Settings")]
        [Tooltip("Trigger type")]
        [SerializeField] private TriggerMode triggerMode = TriggerMode.OnEnter;
        
        [Tooltip("Có phải one-time use không")]
        [SerializeField] private bool oneTimeUse = false;
        
        [Tooltip("Cooldown giữa các lần trigger")]
        [SerializeField] private float triggerCooldown = 1f;
        
        [Tooltip("Áp dụng lại effect khi ở trong vùng (Stay mode)")]
        [SerializeField] private float reapplyInterval = 1f;

        [Header("Visual")]
        [SerializeField] private Color trapColor = Color.red;
        [SerializeField] private bool changeColorOnDisabled = true;
        [SerializeField] private GameObject activationVFX;
        
        [Header("Audio")]
        [SerializeField] private AudioClip activationSound;
        [SerializeField] private AudioClip ambientSound;

        public enum TriggerMode
        {
            OnEnter,    // Trigger khi bước vào
            OnStay,     // Trigger liên tục khi ở trong
            OnExit      // Trigger khi rời khỏi
        }

        private SpriteRenderer _spriteRenderer;
        private AudioSource _audioSource;
        private Color _originalColor;
        private bool _isDisabled = false;
        private float _cooldownTimer = 0f;
        private float _stayTimer = 0f;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _audioSource = GetComponent<AudioSource>();
            
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            if (_spriteRenderer != null)
            {
                _originalColor = _spriteRenderer.color;
            }

            // Play ambient sound nếu có
            if (ambientSound != null && _audioSource != null)
            {
                _audioSource.clip = ambientSound;
                _audioSource.loop = true;
                _audioSource.Play();
            }
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_isDisabled || _cooldownTimer > 0f) return;
            
            if (triggerMode == TriggerMode.OnEnter && collision.CompareTag("Player"))
            {
                ApplyEffectToPlayer(collision.gameObject);
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (_isDisabled || _cooldownTimer > 0f) return;
            
            if (triggerMode == TriggerMode.OnStay && collision.CompareTag("Player"))
            {
                _stayTimer += Time.deltaTime;
                
                if (_stayTimer >= reapplyInterval)
                {
                    ApplyEffectToPlayer(collision.gameObject);
                    _stayTimer = 0f;
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            _stayTimer = 0f;
            
            if (_isDisabled || _cooldownTimer > 0f) return;
            
            if (triggerMode == TriggerMode.OnExit && collision.CompareTag("Player"))
            {
                ApplyEffectToPlayer(collision.gameObject);
            }
        }

        private void ApplyEffectToPlayer(GameObject player)
        {
            var statusEffects = player.GetComponent<PlayerStatusEffects>();
            
            if (statusEffects == null)
            {
                Debug.LogWarning($"[StatusEffectTrap] Player doesn't have PlayerStatusEffects! Trap: {gameObject.name}");
                return;
            }

            // Áp dụng effect dựa vào loại
            if (effectType == StatusEffectType.Burn || effectType == StatusEffectType.Poison)
            {
                statusEffects.ApplyDoT(effectType, effectDuration, dotDamagePerTick, dotTickInterval);
            }
            else
            {
                statusEffects.ApplyEffect(effectType, effectDuration, effectIntensity, this);
            }

            Debug.Log($"[StatusEffectTrap] Applied {effectType} to player");

            PlayActivationEffects();

            // Handle state
            if (oneTimeUse)
            {
                DisableTrap();
            }
            else
            {
                _cooldownTimer = triggerCooldown;
            }
        }

        private void PlayActivationEffects()
        {
            if (activationSound != null)
            {
                if (_audioSource != null)
                {
                    _audioSource.PlayOneShot(activationSound);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(activationSound, transform.position);
                }
            }

            if (activationVFX != null)
            {
                Instantiate(activationVFX, transform.position, Quaternion.identity);
            }
        }

        private void DisableTrap()
        {
            _isDisabled = true;
            
            if (changeColorOnDisabled && _spriteRenderer != null)
            {
                var color = _spriteRenderer.color;
                color.a = 0.3f;
                _spriteRenderer.color = color;
            }

            // Stop ambient sound
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }

        /// <summary>
        /// Reset trap về trạng thái ban đầu
        /// </summary>
        public void ResetTrap()
        {
            _isDisabled = false;
            _cooldownTimer = 0f;
            _stayTimer = 0f;
            
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalColor;
            }

            // Restart ambient sound
            if (ambientSound != null && _audioSource != null)
            {
                _audioSource.Play();
            }
        }

        private void OnDrawGizmos()
        {
            Color gizmoColor = effectType switch
            {
                StatusEffectType.Slow => new Color(0.5f, 0.8f, 1f, 0.3f),
                StatusEffectType.Confusion => new Color(1f, 0.5f, 1f, 0.3f),
                StatusEffectType.Freeze => new Color(0.3f, 0.6f, 1f, 0.3f),
                StatusEffectType.Burn => new Color(1f, 0.4f, 0.2f, 0.3f),
                StatusEffectType.Poison => new Color(0.5f, 1f, 0.3f, 0.3f),
                StatusEffectType.Slippery => new Color(0.7f, 0.9f, 1f, 0.3f),
                _ => new Color(0.8f, 0.8f, 0.8f, 0.3f)
            };
            
            Gizmos.color = gizmoColor;
            
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                Gizmos.DrawCube(collider.bounds.center, collider.bounds.size);
            }
            else
            {
                Gizmos.DrawCube(transform.position, Vector3.one);
            }

            // Draw effect type label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, effectType.ToString());
            #endif
        }
    }
}
