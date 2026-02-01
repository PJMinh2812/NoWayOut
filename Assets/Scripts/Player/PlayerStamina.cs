using UnityEngine;

namespace NWO
{

    public class PlayerStamina : MonoBehaviour
    {
        [Header("Stamina Settings")]
        [Tooltip("Stamina tối đa")]
        [SerializeField] private float maxStamina = 100f;
        
        [Tooltip("Stamina ban đầu khi bắt đầu game")]
        [SerializeField] private float startingStamina = 100f;
        
        [Tooltip("Tốc độ hồi stamina (mỗi giây)")]
        [SerializeField] private float regenPerSecond = 10f;
        
        [Tooltip("Delay trước khi bắt đầu hồi stamina sau khi dùng (giây)")]
        [SerializeField] private float regenDelay = 1f;

        [Header("Action Costs")]
        [Tooltip("Stamina tiêu tốn khi Roll/Dash")]
        [SerializeField] private float rollCost = 15f;
        
        [Tooltip("Stamina tiêu tốn Spell 01")]
        [SerializeField] private float spell01Cost = 10f;
        
        [Tooltip("Stamina tiêu tốn Spell 02")]
        [SerializeField] private float spell02Cost = 20f;
        
        [Tooltip("Stamina tiêu tốn Spell 03")]
        [SerializeField] private float spell03Cost = 30f;

        [Header("Visual Feedback")]
        [Tooltip("Màu stamina bar khi đầy")]
        [SerializeField] private Color fullStaminaColor = new Color(0.3f, 0.8f, 1f); // Xanh dương
        
        [Tooltip("Màu stamina bar khi thấp")]
        [SerializeField] private Color lowStaminaColor = new Color(1f, 0.5f, 0f); // Cam
        
        [Tooltip("Ngưỡng stamina thấp (%)")]
        [SerializeField] private float lowStaminaThreshold = 30f;


        public float CurrentStamina { get; private set; }

        public float MaxStamina => maxStamina;

        public float StaminaPercent => CurrentStamina / maxStamina;

        public bool IsLowStamina => StaminaPercent <= (lowStaminaThreshold / 100f);

        
        private float _regenDelayTimer;
        private bool _isRegenerating = true;



        private void Awake()
        {
            CurrentStamina = startingStamina;
        }

        private void Update()
        {
            UpdateRegeneration();
        }

        public bool CanRoll()
        {
            return CurrentStamina >= rollCost;
        }

        public bool TryConsumeRoll()
        {
            if (!CanRoll()) return false;
            
            ConsumeStamina(rollCost);
            Debug.Log($"[Stamina] Roll consumed {rollCost} stamina. Remaining: {CurrentStamina:F0}/{maxStamina}");
            return true;
        }

        public bool CanCastSpell(int spellNumber)
        {
            float cost = GetSpellCost(spellNumber);
            return CurrentStamina >= cost;
        }


        public bool TryConsumeSpell(int spellNumber)
        {
            float cost = GetSpellCost(spellNumber);
            
            if (CurrentStamina < cost)
            {
                Debug.Log($"[Stamina] Not enough stamina for Spell {spellNumber}! Need {cost}, have {CurrentStamina:F0}");
                return false;
            }
            
            ConsumeStamina(cost);
            Debug.Log($"[Stamina] Spell {spellNumber} consumed {cost} stamina. Remaining: {CurrentStamina:F0}/{maxStamina}");
            return true;
        }

        private float GetSpellCost(int spellNumber)
        {
            return spellNumber switch
            {
                1 => spell01Cost,
                2 => spell02Cost,
                3 => spell03Cost,
                _ => 0f
            };
        }

        private void ConsumeStamina(float amount)
        {
            CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
            
            // Reset regen timer
            _regenDelayTimer = regenDelay;
            _isRegenerating = false;
        }

        private void UpdateRegeneration()
        {
            // Nếu đang delay, chờ
            if (_regenDelayTimer > 0f)
            {
                _regenDelayTimer -= Time.deltaTime;
                if (_regenDelayTimer <= 0f)
                {
                    _isRegenerating = true;
                }
                return;
            }

            // Hồi stamina
            if (_isRegenerating && CurrentStamina < maxStamina)
            {
                CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + regenPerSecond * Time.deltaTime);
            }
        }

        public void AddStamina(float amount)
        {
            float before = CurrentStamina;
            CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + amount);
            Debug.Log($"[Stamina] Added {amount} stamina ({before:F0} → {CurrentStamina:F0})");
        }

        public void RestoreToFull()
        {
            CurrentStamina = maxStamina;
            _regenDelayTimer = 0f;
            _isRegenerating = true;
            Debug.Log("[Stamina] Restored to full!");
        }
        public Color GetStaminaBarColor()
        {
            if (IsLowStamina)
            {
                return lowStaminaColor;
            }
            
            // Lerp giữa low và full
            float t = (StaminaPercent - lowStaminaThreshold / 100f) / (1f - lowStaminaThreshold / 100f);
            return Color.Lerp(lowStaminaColor, fullStaminaColor, t);
        }

        //private void OnGUI()
        //{
        //    // Debug display (tắt nếu không cần)
        //    if (!Application.isEditor) return;

        //    GUIStyle style = new GUIStyle(GUI.skin.label);
        //    style.fontSize = 10;
        //    style.normal.textColor = Color.white;

        //    // Stamina info ở góc dưới trái
        //    GUI.Label(new Rect(10, Screen.height - 40, 200, 20), 
        //        $"Stamina: {CurrentStamina:F0}/{maxStamina}", style);
            
        //    if (!_isRegenerating && _regenDelayTimer > 0f)
        //    {
        //        GUI.Label(new Rect(10, Screen.height - 25, 200, 20), 
        //            $"Regen in: {_regenDelayTimer:F1}s", style);
        //    }
        //}
    }
}
