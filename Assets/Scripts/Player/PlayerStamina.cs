using UnityEngine;

namespace NWO
{

    public class PlayerStamina : MonoBehaviour
    {
        [Header("Stamina Settings")]
        [Tooltip("Stamina tá»‘i Ä‘a")]
        [SerializeField] private float maxStamina = 100f;
        
        [Tooltip("Stamina ban Ä‘áº§u khi báº¯t Ä‘áº§u game")]
        [SerializeField] private float startingStamina = 100f;
        
        [Tooltip("Tá»‘c Ä‘á»™ há»“i stamina (má»—i giÃ¢y)")]
        [SerializeField] private float regenPerSecond = 10f;
        
        [Tooltip("Delay trÆ°á»›c khi báº¯t Ä‘áº§u há»“i stamina sau khi dÃ¹ng (giÃ¢y)")]
        [SerializeField] private float regenDelay = 1f;

        [Header("Action Costs")]
        [Tooltip("Stamina tiÃªu tá»‘n khi Roll/Dash")]
        [SerializeField] private float rollCost = 15f;
        
        [Tooltip("Stamina tiÃªu tá»‘n Spell 01")]
        [SerializeField] private float spell01Cost = 10f;
        
        [Tooltip("Stamina tiÃªu tá»‘n Spell 02")]
        [SerializeField] private float spell02Cost = 20f;
        
        [Tooltip("Stamina tiÃªu tá»‘n Spell 03")]
        [SerializeField] private float spell03Cost = 30f;

        [Header("Visual Feedback")]
        [Tooltip("MÃ u stamina bar khi Ä‘áº§y")]
        [SerializeField] private Color fullStaminaColor = new Color(0.3f, 0.8f, 1f); // Xanh dÆ°Æ¡ng
        
        [Tooltip("MÃ u stamina bar khi tháº¥p")]
        [SerializeField] private Color lowStaminaColor = new Color(1f, 0.5f, 0f); // Cam
        
        [Tooltip("NgÆ°á»¡ng stamina tháº¥p (%)")]
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

                return false;
            }
            
            ConsumeStamina(cost);

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

        /// <summary>
        /// Tiêu tốn stamina - có thể gọi từ bên ngoài (melee, ability, etc.)
        /// </summary>
        public void ConsumeStamina(float amount)
        {
            CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
            
            // Reset regen timer
            _regenDelayTimer = regenDelay;
            _isRegenerating = false;
        }

        private void UpdateRegeneration()
        {
            // Náº¿u Ä‘ang delay, chá»
            if (_regenDelayTimer > 0f)
            {
                _regenDelayTimer -= Time.deltaTime;
                if (_regenDelayTimer <= 0f)
                {
                    _isRegenerating = true;
                }
                return;
            }

            // Há»“i stamina
            if (_isRegenerating && CurrentStamina < maxStamina)
            {
                CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + regenPerSecond * Time.deltaTime);
            }
        }

        public void AddStamina(float amount)
        {
            float before = CurrentStamina;
            CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + amount);
            Debug.Log($"[Stamina] Added {amount} stamina ({before:F0} â†’ {CurrentStamina:F0})");
        }

        public void RestoreToFull()
        {
            CurrentStamina = maxStamina;
            _regenDelayTimer = 0f;
            _isRegenerating = true;

        }
        public Color GetStaminaBarColor()
        {
            if (IsLowStamina)
            {
                return lowStaminaColor;
            }
            
            // Lerp giá»¯a low vÃ  full
            float t = (StaminaPercent - lowStaminaThreshold / 100f) / (1f - lowStaminaThreshold / 100f);
            return Color.Lerp(lowStaminaColor, fullStaminaColor, t);
        }

        //private void OnGUI()
        //{
        //    // Debug display (táº¯t náº¿u khÃ´ng cáº§n)
        //    if (!Application.isEditor) return;

        //    GUIStyle style = new GUIStyle(GUI.skin.label);
        //    style.fontSize = 10;
        //    style.normal.textColor = Color.white;

        //    // Stamina info á»Ÿ gÃ³c dÆ°á»›i trÃ¡i
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
