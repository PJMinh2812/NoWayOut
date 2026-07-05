using UnityEngine;
using System;
using System.Collections.Generic;

namespace NWO
{
    /// <summary>
    /// Enum định nghĩa các loại status effect
    /// </summary>
    public enum StatusEffectType
    {
        None = 0,
        
        // Debuffs
        Slow,           // Giảm tốc độ di chuyển
        Confusion,      // Đảo ngược điều khiển (W<->S, A<->D)
        Freeze,         // Không thể di chuyển
        Burn,           // Damage over time
        Poison,         // Damage over time (chậm hơn burn)
        Silence,        // Không thể cast spell
        Blind,          // Giảm tầm nhìn
        Slippery,       // Trượt thêm sau khi dừng
        Stun,           // Không thể làm gì
        WeakKnees,      // Không thể roll/dash
        
        // Buffs
        SpeedBoost,     // Tăng tốc độ
        Shield,         // Chặn 1 đòn damage
        Invincible,     // Bất tử tạm thời
        Regeneration,   // Hồi máu theo thời gian
        StaminaBoost,   // Hồi stamina nhanh hơn
        DamageBoost,    // Tăng damage
        LightAura       // Mở rộng tầm nhìn
    }

    /// <summary>
    /// Data class lưu thông tin một status effect đang active
    /// </summary>
    [Serializable]
    public class ActiveStatusEffect
    {
        public StatusEffectType Type;
        public float Duration;          // Tổng thời gian
        public float RemainingTime;     // Thời gian còn lại
        public float Intensity;         // Độ mạnh (vd: slow 50% = 0.5)
        public float TickInterval;      // Cho DoT effects
        public float NextTickTime;      // Thời điểm tick tiếp theo
        public object Source;           // Nguồn gây effect (trap, enemy, item...)
        
        public float Progress => Duration > 0 ? (1f - RemainingTime / Duration) : 1f;
        public bool IsExpired => RemainingTime <= 0f;
    }

    /// <summary>
    /// Quản lý tất cả status effects trên Player
    /// </summary>
    public class PlayerStatusEffects : MonoBehaviour
    {
        [Header("Status Effect Settings")]
        [Tooltip("Có cho phép stack cùng loại effect không")]
        [SerializeField] private bool allowStacking = false;
        
        [Tooltip("Khi không stack, có refresh duration không")]
        [SerializeField] private bool refreshDurationOnReapply = true;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer playerSprite;
        [SerializeField] private Color slowColor = new Color(0.5f, 0.8f, 1f);      // Xanh nhạt
        [SerializeField] private Color confusionColor = new Color(1f, 0.5f, 1f);   // Hồng
        [SerializeField] private Color freezeColor = new Color(0.3f, 0.6f, 1f);    // Xanh đậm
        [SerializeField] private Color burnColor = new Color(1f, 0.4f, 0.2f);      // Cam
        [SerializeField] private Color poisonColor = new Color(0.5f, 1f, 0.3f);    // Xanh lá
        [SerializeField] private Color speedBoostColor = new Color(1f, 1f, 0.5f);  // Vàng nhạt
        [SerializeField] private Color shieldColor = new Color(0.8f, 0.8f, 1f);    // Trắng xanh

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip debuffApplySound;
        [SerializeField] private AudioClip buffApplySound;
        [SerializeField] private AudioClip effectExpireSound;

        // === EVENTS ===
        public event Action<StatusEffectType, float> OnEffectApplied;
        public event Action<StatusEffectType> OnEffectExpired;
        public event Action<StatusEffectType, float, float> OnEffectTick; // type, damage, remaining

        // === PROPERTIES ===
        public IReadOnlyList<ActiveStatusEffect> ActiveEffects => _activeEffects;
        public bool HasAnyDebuff => HasEffect(e => IsDebuff(e.Type));
        public bool HasAnyBuff => HasEffect(e => !IsDebuff(e.Type));
        
        // Quick access properties
        public bool IsSlowed => HasEffect(StatusEffectType.Slow);
        public bool IsConfused => HasEffect(StatusEffectType.Confusion);
        public bool IsFrozen => HasEffect(StatusEffectType.Freeze);
        public bool IsStunned => HasEffect(StatusEffectType.Stun);
        public bool IsSilenced => HasEffect(StatusEffectType.Silence);
        public bool IsSlippery => HasEffect(StatusEffectType.Slippery);
        public bool CannotRoll => HasEffect(StatusEffectType.WeakKnees) || IsStunned || IsFrozen;
        public bool CannotMove => IsFrozen || IsStunned;
        public bool CannotCast => IsSilenced || IsStunned || IsFrozen;
        public bool HasShield => HasEffect(StatusEffectType.Shield);
        public bool IsInvincible => HasEffect(StatusEffectType.Invincible);

        // Calculated modifiers (cached, only recalculated when effects change)
        public float MoveSpeedMultiplier { get { if (_effectsDirty) RecalculateModifiers(); return _cachedMoveSpeedMultiplier; } }
        public float DamageMultiplier { get { if (_effectsDirty) RecalculateModifiers(); return _cachedDamageMultiplier; } }
        public float StaminaRegenMultiplier { get { if (_effectsDirty) RecalculateModifiers(); return _cachedStaminaRegenMultiplier; } }

        // Internal
        private List<ActiveStatusEffect> _activeEffects = new List<ActiveStatusEffect>();
        private HashSet<StatusEffectType> _activeTypes = new HashSet<StatusEffectType>();
        private bool _effectsDirty = true; // marks when effect list changes
        private PlayerHealth2D _health;
        private PlayerStamina _stamina;
        private AudioSource _audioSource;
        private Color _originalColor = Color.white;
        private bool _colorCached = false;

        // Cached multipliers - recalculated only when effects change
        private float _cachedMoveSpeedMultiplier = 1f;
        private float _cachedDamageMultiplier = 1f;
        private float _cachedStaminaRegenMultiplier = 1f;

        private void Awake()
        {
            _health = GetComponent<PlayerHealth2D>();
            _stamina = GetComponent<PlayerStamina>();
            _audioSource = GetComponent<AudioSource>();
            
            if (playerSprite == null)
                playerSprite = GetComponent<SpriteRenderer>();
                
            if (playerSprite != null && !_colorCached)
            {
                _originalColor = playerSprite.color;
                _colorCached = true;
            }
        }

        private void Update()
        {
            UpdateEffects();
            UpdateVisuals();
        }

        #region Public API

        /// <summary>
        /// Áp dụng một status effect lên player
        /// </summary>
        public void ApplyEffect(StatusEffectType type, float duration, float intensity = 1f, object source = null)
        {
            if (type == StatusEffectType.None) return;
            
            // Kiểm tra immunity
            if (IsInvincible && IsDebuff(type))
            {
                Debug.Log($"[StatusEffects] {type} blocked by Invincibility!");
                return;
            }

            var existing = _activeEffects.Find(e => e.Type == type);
            
            if (existing != null && !allowStacking)
            {
                if (refreshDurationOnReapply)
                {
                    existing.RemainingTime = Mathf.Max(existing.RemainingTime, duration);
                    existing.Duration = existing.RemainingTime;
                    existing.Intensity = Mathf.Max(existing.Intensity, intensity);
                    Debug.Log($"[StatusEffects] Refreshed {type} - Duration: {duration}s, Intensity: {intensity}");
                }
                return;
            }

            var newEffect = new ActiveStatusEffect
            {
                Type = type,
                Duration = duration,
                RemainingTime = duration,
                Intensity = intensity,
                TickInterval = GetTickInterval(type),
                NextTickTime = Time.time + GetTickInterval(type),
                Source = source
            };

            _activeEffects.Add(newEffect);
            RebuildActiveTypes();
            
            PlayApplySound(type);
            OnEffectApplied?.Invoke(type, duration);
            
            Debug.Log($"[StatusEffects] Applied {type} - Duration: {duration}s, Intensity: {intensity}");
        }

        /// <summary>
        /// Áp dụng DoT effect (Burn, Poison)
        /// </summary>
        public void ApplyDoT(StatusEffectType type, float duration, float damagePerTick, float tickInterval = 0.5f)
        {
            if (type != StatusEffectType.Burn && type != StatusEffectType.Poison)
            {
                Debug.LogWarning($"[StatusEffects] {type} is not a DoT effect!");
                return;
            }

            var effect = new ActiveStatusEffect
            {
                Type = type,
                Duration = duration,
                RemainingTime = duration,
                Intensity = damagePerTick,
                TickInterval = tickInterval,
                NextTickTime = Time.time + tickInterval
            };

            // DoT có thể stack
            _activeEffects.Add(effect);
            RebuildActiveTypes();
            PlayApplySound(type);
            OnEffectApplied?.Invoke(type, duration);
            
            Debug.Log($"[StatusEffects] Applied {type} DoT - {damagePerTick} damage every {tickInterval}s for {duration}s");
        }

        /// <summary>
        /// Xóa một loại status effect
        /// </summary>
        public void RemoveEffect(StatusEffectType type)
        {
            int removed = _activeEffects.RemoveAll(e => e.Type == type);
            if (removed > 0)
            {
                RebuildActiveTypes();
                OnEffectExpired?.Invoke(type);
                PlayExpireSound();
                Debug.Log($"[StatusEffects] Removed {removed} instance(s) of {type}");
            }
        }

        /// <summary>
        /// Xóa tất cả debuffs
        /// </summary>
        public void ClearAllDebuffs()
        {
            var debuffs = _activeEffects.FindAll(e => IsDebuff(e.Type));
            foreach (var debuff in debuffs)
            {
                _activeEffects.Remove(debuff);
                OnEffectExpired?.Invoke(debuff.Type);
            }
            
            if (debuffs.Count > 0)
            {
                RebuildActiveTypes();
                PlayExpireSound();
                Debug.Log($"[StatusEffects] Cleared {debuffs.Count} debuffs");
            }
        }

        /// <summary>
        /// Xóa tất cả effects
        /// </summary>
        public void ClearAllEffects()
        {
            foreach (var effect in _activeEffects)
            {
                OnEffectExpired?.Invoke(effect.Type);
            }
            _activeEffects.Clear();
            _activeTypes.Clear();
            _effectsDirty = true;
            Debug.Log("[StatusEffects] Cleared all effects");
        }

        /// <summary>
        /// Kiểm tra có effect cụ thể không (O(1) via HashSet)
        /// </summary>
        public bool HasEffect(StatusEffectType type)
        {
            return _activeTypes.Contains(type);
        }

        /// <summary>
        /// Kiểm tra với predicate
        /// </summary>
        public bool HasEffect(Predicate<ActiveStatusEffect> predicate)
        {
            return _activeEffects.Exists(predicate);
        }

        /// <summary>
        /// Lấy intensity của effect (0 nếu không có)
        /// </summary>
        public float GetEffectIntensity(StatusEffectType type)
        {
            var effect = _activeEffects.Find(e => e.Type == type);
            return effect?.Intensity ?? 0f;
        }

        /// <summary>
        /// Lấy thời gian còn lại của effect
        /// </summary>
        public float GetRemainingTime(StatusEffectType type)
        {
            var effect = _activeEffects.Find(e => e.Type == type);
            return effect?.RemainingTime ?? 0f;
        }

        /// <summary>
        /// Tiêu thụ Shield (gọi khi bị damage)
        /// </summary>
        public bool ConsumeShield()
        {
            if (!HasShield) return false;
            
            RemoveEffect(StatusEffectType.Shield);
            Debug.Log("[StatusEffects] Shield consumed!");
            return true;
        }

        /// <summary>
        /// Áp dụng input confusion (đảo ngược điều khiển)
        /// </summary>
        public Vector2 ApplyConfusion(Vector2 input)
        {
            if (!IsConfused) return input;
            
            // Đảo ngược cả X và Y
            return -input;
        }

        /// <summary>
        /// Áp dụng slippery effect (trượt thêm)
        /// </summary>
        public float GetSlipperyDrag()
        {
            if (!IsSlippery) return -1f; // -1 = không thay đổi
            
            float intensity = GetEffectIntensity(StatusEffectType.Slippery);
            return Mathf.Lerp(8f, 1f, intensity); // Giảm drag từ 8 xuống 1
        }

        #endregion

        #region Private Methods

        private void UpdateEffects()
        {
            bool anyExpired = false;
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                effect.RemainingTime -= Time.deltaTime;

                // Xử lý DoT tick
                if (effect.TickInterval > 0 && Time.time >= effect.NextTickTime)
                {
                    ProcessTick(effect);
                    effect.NextTickTime = Time.time + effect.TickInterval;
                }

                // Xử lý Regeneration
                if (effect.Type == StatusEffectType.Regeneration)
                {
                    ProcessRegeneration(effect);
                }

                // Xóa effect hết hạn
                if (effect.IsExpired)
                {
                    OnEffectExpired?.Invoke(effect.Type);
                    PlayExpireSound();
                    _activeEffects.RemoveAt(i);
                    anyExpired = true;
                    Debug.Log($"[StatusEffects] {effect.Type} expired");
                }
            }

            if (anyExpired)
            {
                RebuildActiveTypes();
            }
        }

        private void ProcessTick(ActiveStatusEffect effect)
        {
            if (effect.Type == StatusEffectType.Burn || effect.Type == StatusEffectType.Poison)
            {
                int damage = Mathf.RoundToInt(effect.Intensity);
                if (_health != null && damage > 0)
                {
                    _health.TakeDamage(damage, Vector2.zero, null);
                    OnEffectTick?.Invoke(effect.Type, damage, effect.RemainingTime);
                    Debug.Log($"[StatusEffects] {effect.Type} tick: {damage} damage");
                }
            }
        }

        private void ProcessRegeneration(ActiveStatusEffect effect)
        {
            // Hồi máu mỗi frame theo intensity
            if (_health != null)
            {
                float healPerSecond = effect.Intensity;
                // Note: PlayerHealth2D có thể cần method Heal()
            }
        }

        private void UpdateVisuals()
        {
            if (playerSprite == null) return;

            // Skip expensive color lerp when no effects are active and color is already original
            if (_activeTypes.Count == 0)
            {
                if (playerSprite.color != _originalColor)
                    playerSprite.color = Color.Lerp(playerSprite.color, _originalColor, Time.deltaTime * 5f);
                return;
            }

            // Priority: Freeze > Stun > Burn > Poison > Confusion > Slow > Buff
            Color targetColor = _originalColor;

            if (IsFrozen)
                targetColor = freezeColor;
            else if (IsStunned)
                targetColor = Color.gray;
            else if (_activeTypes.Contains(StatusEffectType.Burn))
                targetColor = burnColor;
            else if (_activeTypes.Contains(StatusEffectType.Poison))
                targetColor = poisonColor;
            else if (IsConfused)
                targetColor = confusionColor;
            else if (IsSlowed || IsSlippery)
                targetColor = slowColor;
            else if (_activeTypes.Contains(StatusEffectType.SpeedBoost))
                targetColor = speedBoostColor;
            else if (HasShield)
                targetColor = shieldColor;

            // Smooth transition
            playerSprite.color = Color.Lerp(playerSprite.color, targetColor, Time.deltaTime * 5f);
        }

        /// <summary>
        /// Rebuild the O(1) type lookup set and mark modifiers as dirty
        /// </summary>
        private void RebuildActiveTypes()
        {
            _activeTypes.Clear();
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                _activeTypes.Add(_activeEffects[i].Type);
            }
            _effectsDirty = true;
        }

        /// <summary>
        /// Recalculate cached modifier values
        /// </summary>
        private void RecalculateModifiers()
        {
            _cachedMoveSpeedMultiplier = CalculateMoveSpeedMultiplier();
            _cachedDamageMultiplier = CalculateDamageMultiplier();
            _cachedStaminaRegenMultiplier = CalculateStaminaRegenMultiplier();
            _effectsDirty = false;
        }

        private float CalculateMoveSpeedMultiplier()
        {
            float multiplier = 1f;

            if (CannotMove) return 0f;

            // Debuffs
            if (IsSlowed)
            {
                float slowIntensity = GetEffectIntensity(StatusEffectType.Slow);
                multiplier *= (1f - slowIntensity);
            }

            // Buffs
            if (HasEffect(StatusEffectType.SpeedBoost))
            {
                float boostIntensity = GetEffectIntensity(StatusEffectType.SpeedBoost);
                multiplier *= (1f + boostIntensity);
            }

            return Mathf.Clamp(multiplier, 0.1f, 3f);
        }

        private float CalculateDamageMultiplier()
        {
            float multiplier = 1f;

            if (HasEffect(StatusEffectType.DamageBoost))
            {
                multiplier += GetEffectIntensity(StatusEffectType.DamageBoost);
            }

            return multiplier;
        }

        private float CalculateStaminaRegenMultiplier()
        {
            float multiplier = 1f;

            if (HasEffect(StatusEffectType.StaminaBoost))
            {
                multiplier += GetEffectIntensity(StatusEffectType.StaminaBoost);
            }

            return multiplier;
        }

        private float GetTickInterval(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Burn => 0.3f,
                StatusEffectType.Poison => 1f,
                _ => 0f
            };
        }

        private bool IsDebuff(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Slow => true,
                StatusEffectType.Confusion => true,
                StatusEffectType.Freeze => true,
                StatusEffectType.Burn => true,
                StatusEffectType.Poison => true,
                StatusEffectType.Silence => true,
                StatusEffectType.Blind => true,
                StatusEffectType.Slippery => true,
                StatusEffectType.Stun => true,
                StatusEffectType.WeakKnees => true,
                _ => false
            };
        }

        private void PlayApplySound(StatusEffectType type)
        {
            if (_audioSource == null) return;
            
            var clip = IsDebuff(type) ? debuffApplySound : buffApplySound;
            if (clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        private void PlayExpireSound()
        {
            if (_audioSource != null && effectExpireSound != null)
            {
                _audioSource.PlayOneShot(effectExpireSound);
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Apply Slow (3s, 50%)")]
        private void DebugApplySlow()
        {
            ApplyEffect(StatusEffectType.Slow, 3f, 0.5f);
        }

        [ContextMenu("Debug: Apply Confusion (5s)")]
        private void DebugApplyConfusion()
        {
            ApplyEffect(StatusEffectType.Confusion, 5f);
        }

        [ContextMenu("Debug: Apply Burn DoT (5s)")]
        private void DebugApplyBurn()
        {
            ApplyDoT(StatusEffectType.Burn, 5f, 2f, 0.5f);
        }

        [ContextMenu("Debug: Apply Speed Boost (3s, +50%)")]
        private void DebugApplySpeedBoost()
        {
            ApplyEffect(StatusEffectType.SpeedBoost, 3f, 0.5f);
        }

        [ContextMenu("Debug: Clear All")]
        private void DebugClearAll()
        {
            ClearAllEffects();
        }

        #endregion
    }
}
