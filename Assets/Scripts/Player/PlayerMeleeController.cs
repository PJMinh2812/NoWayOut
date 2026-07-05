using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

namespace NWO
{
    /// <summary>
    /// Melee Combat Controller cho Dage
    /// - Click chuột trái để tấn công gậy
    /// - Hệ thống combo 3 đòn
    /// - Tiêu tốn stamina mỗi đòn
    /// - Tích hợp với status effects
    /// </summary>
    [RequireComponent(typeof(PlayerController2D))]
    public class PlayerMeleeController : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private int baseDamage = 8;
        [SerializeField] private float attackRange = 1.2f;
        [SerializeField] private float attackRadius = 0.6f;
        [SerializeField] private float knockbackForce = 4f;
        
        [Header("Combo System")]
        [SerializeField] private int maxComboHits = 3;
        [SerializeField] private float attackDuration = 0.35f; // Thời gian animation mỗi đòn
        [SerializeField] private float comboResetDelay = 1.5f; // Thời gian reset combo sau khi không đánh
        
        [Header("Damage Multipliers")]
        [SerializeField] private float combo1Multiplier = 1.0f;
        [SerializeField] private float combo2Multiplier = 1.2f;
        [SerializeField] private float combo3Multiplier = 1.5f;
        
        [Header("Stamina")]
        [SerializeField] private float staminaCostPerAttack = 10f;
        
        [Header("Visual")]
        [SerializeField] private Transform attackPoint;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private bool showDebugGizmos = true;
        
        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip[] swingSounds;
        [SerializeField] private AudioClip hitSound;
        private AudioSource _audioSource;
        
        [Header("Effects (Optional)")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float hitEffectDuration = 0.5f;

        // State
        private int _currentCombo = 0;
        private float _comboTimer = 0f;
        private bool _isAttacking = false;
        private bool _attackQueued = false;
        private float _attackTimer = 0f;
        
        // Components
        private PlayerController2D _controller;
        private PlayerStamina _stamina;
        private PlayerStatusEffects _statusEffects;
        private PlayerSpellController _spellController;
        private Animator _animator;
        
        // Events
        public System.Action<int> OnComboHit; // comboNumber (1-3)
        public System.Action OnComboComplete;
        public System.Action OnComboReset;
        
        // Properties
        public bool IsAttacking => _isAttacking;
        public int CurrentCombo => _currentCombo;
        public bool CanAttack => !_isAttacking && CanAttackByStatus();
        public int BaseDamage => baseDamage;
        public float AttackRange => attackRange;
        public float KnockbackForce => knockbackForce;

        // Cached animator hashes
        private static readonly int HashIsAttacking = Animator.StringToHash("IsAttacking");
        private static readonly int HashMeleeAttack = Animator.StringToHash("MeleeAttack");
        private static readonly int HashComboHit = Animator.StringToHash("ComboHit");

        // Pre-allocated buffer for physics overlap (avoid GC alloc every attack)
        private static readonly Collider2D[] _hitBuffer = new Collider2D[16];
        
        private void Awake()
        {
            _controller = GetComponent<PlayerController2D>();
            _stamina = GetComponent<PlayerStamina>();
            _statusEffects = GetComponent<PlayerStatusEffects>();
            _spellController = GetComponent<PlayerSpellController>();
            _animator = GetComponent<Animator>();
            _audioSource = GetComponent<AudioSource>();
            
            // Tự động tạo attack point nếu không có
            if (attackPoint == null)
            {
                var attackPointObj = new GameObject("MeleeAttackPoint");
                attackPointObj.transform.SetParent(transform);
                attackPointObj.transform.localPosition = new Vector3(0.8f, 0f, 0f);
                attackPoint = attackPointObj.transform;
            }
        }
        
        private void Update()
        {
            UpdateComboTimer();
            UpdateAttack();
            HandleInput();
        }
        
        private void UpdateComboTimer()
        {
            if (_currentCombo > 0 && !_isAttacking)
            {
                _comboTimer += Time.deltaTime;
                
                // Reset combo nếu quá lâu không tiếp tục
                if (_comboTimer >= comboResetDelay)
                {
                    ResetCombo();
                }
            }
        }
        
        private void UpdateAttack()
        {
            if (_isAttacking)
            {
                _attackTimer += Time.deltaTime;
                
                // Kết thúc attack
                if (_attackTimer >= attackDuration)
                {
                    EndAttack();
                }
            }
        }
        
        private void HandleInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            
            // Không tấn công melee khi đang ở spell mode (SpellType > 0)
            // Tránh conflict input với PlayerSpellController
            if (_spellController != null && _spellController.CurrentSpell > 0) return;
            
            // Click trái để tấn công
            if (mouse.leftButton.wasPressedThisFrame)
            {
                TryAttack();
            }
        }
        
        public void TryAttack()
        {
            // Không tấn công khi đang roll
            if (_controller != null && _controller.IsRolling) return;
            
            // Không tấn công khi đang cast spell
            if (_spellController != null && _spellController.IsCasting) return;
            
            // Không tấn công nếu đang bị CC
            if (!CanAttackByStatus()) return;
            
            // Nếu đang attack, queue attack tiếp theo
            if (_isAttacking)
            {
                // Chỉ queue nếu còn trong combo window và chưa đạt max combo
                if (_attackTimer >= attackDuration * 0.5f && _currentCombo < maxComboHits)
                {
                    _attackQueued = true;
                }
                return;
            }
            
            // Kiểm tra stamina
            if (_stamina != null && _stamina.CurrentStamina < staminaCostPerAttack)
            {
                Debug.Log("[Melee] Không đủ stamina để tấn công!");
                return;
            }
            
            // Bắt đầu attack
            StartAttack();
        }
        
        private bool CanAttackByStatus()
        {
            if (_statusEffects == null) return true;
            
            // Không thể tấn công khi frozen, stunned, silenced
            return !_statusEffects.CannotMove && !_statusEffects.CannotCast;
        }
        
        private void StartAttack()
        {
            _isAttacking = true;
            _attackTimer = 0f;
            _currentCombo++;
            _comboTimer = 0f;
            
            // Tiêu tốn stamina
            if (_stamina != null)
            {
                _stamina.ConsumeStamina(staminaCostPerAttack);
            }
            
            // Cập nhật hướng attack point theo hướng nhìn
            UpdateAttackPointPosition();
            
            // Trigger animation
            TriggerAttackAnimation();
            
            // Gây damage
            PerformAttack();
            
            // Play sound
            PlaySwingSound();
            
            // Fire event
            OnComboHit?.Invoke(_currentCombo);
            
            Debug.Log($"[Melee] Combo {_currentCombo}! Damage: {GetCurrentDamage()}");
        }
        
        private void EndAttack()
        {
            _isAttacking = false;
            _attackTimer = 0f;
            
            // ★ CRITICAL FIX: Reset animator IsAttacking parameter
            // Without this, the animator stays stuck in MeleeAttack state forever
            // because OnMeleeAnimationEnd() requires Animation Events which are not configured
            if (_animator != null)
            {
                _animator.SetBool(HashIsAttacking, false);
            }
            
            // Kiểm tra xem có combo đã queue không
            if (_attackQueued && _currentCombo < maxComboHits)
            {
                _attackQueued = false;
                
                // Kiểm tra stamina cho hit tiếp theo
                if (_stamina == null || _stamina.CurrentStamina >= staminaCostPerAttack)
                {
                    StartAttack();
                    return;
                }
            }
            
            _attackQueued = false;
            
            // Nếu đạt max combo, reset
            if (_currentCombo >= maxComboHits)
            {
                OnComboComplete?.Invoke();
                // Delay một chút trước khi reset để có cooldown
                StartCoroutine(DelayedComboReset());
            }
        }
        
        private IEnumerator DelayedComboReset()
        {
            yield return new WaitForSeconds(0.3f);
            ResetCombo();
        }
        
        private void ResetCombo()
        {
            if (_currentCombo > 0)
            {
                _currentCombo = 0;
                _comboTimer = 0f;
                
                // Safety: ensure animator IsAttacking is reset
                if (_animator != null)
                {
                    _animator.SetBool(HashIsAttacking, false);
                }
                
                OnComboReset?.Invoke();
                Debug.Log("[Melee] Combo reset!");
            }
        }
        
        private void UpdateAttackPointPosition()
        {
            if (attackPoint == null) return;
            
            // Lấy hướng aim từ controller
            float aimAngle = _controller != null ? _controller.AimAngleDeg : 0f;
            float rad = aimAngle * Mathf.Deg2Rad;
            
            // Tính vị trí attack point
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * attackRange;
            attackPoint.localPosition = offset;
        }
        
        private void TriggerAttackAnimation()
        {
            if (_animator == null) return;
            
            _animator.SetTrigger(HashMeleeAttack);
            _animator.SetInteger(HashComboHit, _currentCombo);
            _animator.SetBool(HashIsAttacking, true);
        }
        
        private void PerformAttack()
        {
            // Detect enemies trong range (NonAlloc - zero GC allocation)
            Vector2 attackPos = attackPoint != null ? 
                (Vector2)attackPoint.position : 
                (Vector2)transform.position + GetAttackDirection() * attackRange;
            
            var filter = new ContactFilter2D();
            filter.SetLayerMask(enemyLayer);
            filter.useTriggers = false;
            int hitCount = Physics2D.OverlapCircle(attackPos, attackRadius, filter, _hitBuffer);
            
            int damage = GetCurrentDamage();
            Vector2 knockDir = GetAttackDirection();
            
            for (int i = 0; i < hitCount; i++)
            {
                var hit = _hitBuffer[i];
                if (hit.TryGetComponent<Enemy2D>(out var enemy))
                {
                    enemy.TakeDamage(damage, knockDir, knockbackForce);
                    SpawnHitEffect(hit.transform.position);
                    PlayHitSound();
                    
                    Debug.Log($"[Melee] Hit {hit.name} for {damage} damage!");
                }
            }
        }
        
        private int GetCurrentDamage()
        {
            float multiplier = _currentCombo switch
            {
                1 => combo1Multiplier,
                2 => combo2Multiplier,
                3 => combo3Multiplier,
                _ => 1f
            };
            
            return Mathf.RoundToInt(baseDamage * multiplier);
        }
        
        private Vector2 GetAttackDirection()
        {
            if (_controller == null) return Vector2.right;
            
            float aimAngle = _controller.AimAngleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(aimAngle), Mathf.Sin(aimAngle));
        }
        
        private void PlaySwingSound()
        {
            if (_audioSource == null || swingSounds == null || swingSounds.Length == 0) return;
            
            var clip = swingSounds[Random.Range(0, swingSounds.Length)];
            if (clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
        
        private void PlayHitSound()
        {
            if (_audioSource == null || hitSound == null) return;
            _audioSource.PlayOneShot(hitSound);
        }
        
        private void SpawnHitEffect(Vector3 position)
        {
            if (hitEffectPrefab == null) return;
            
            var effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            Destroy(effect, hitEffectDuration);
        }
        
        /// <summary>
        /// Gọi từ Animation Event khi đòn đánh đến frame gây damage
        /// </summary>
        public void OnMeleeHitFrame()
        {
            // Có thể dùng để trigger damage tại frame cụ thể trong animation
            // Hiện tại damage được gây ngay khi bắt đầu attack
        }
        
        /// <summary>Tăng sát thương cơ bản (upgrade system)</summary>
        public void AddBaseDamage(int amount) => baseDamage += amount;

        /// <summary>Tăng tầm đánh (upgrade system)</summary>
        public void AddAttackRange(float amount) => attackRange += amount;

        /// <summary>Tăng lực đẩy lùi (upgrade system)</summary>
        public void AddKnockbackForce(float amount) => knockbackForce += amount;

        /// <summary>
        /// Gọi từ Animation Event khi animation kết thúc
        /// </summary>
        public void OnMeleeAnimationEnd()
        {
            if (_animator != null)
            {
                _animator.SetBool(HashIsAttacking, false);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;
            
            // Vẽ attack range
            Gizmos.color = Color.red;
            Vector3 attackPos = attackPoint != null ? 
                attackPoint.position : 
                transform.position + (Vector3)GetAttackDirection() * attackRange;
            
            Gizmos.DrawWireSphere(attackPos, attackRadius);
            
            // Vẽ line từ player đến attack point
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, attackPos);
        }
    }
}
