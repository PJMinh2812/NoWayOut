using UnityEngine;
using UnityEngine.InputSystem;

namespace GloomCraft
{
    /// <summary>
    /// Script test animation tạm thời - XÓA sau khi test xong
    /// Nhấn T = Damage, Y = Death, R = Reset
    /// </summary>
    public class AnimationTester : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerAnimationController animController;
        
        [Header("Test Keys")]
        [SerializeField] private Key damageKey = Key.T;
        [SerializeField] private Key deathKey = Key.Y;
        [SerializeField] private Key resetKey = Key.R;
        
        [Header("Debug Info")]
        [SerializeField] private bool showDebugInfo = true;
        
        private Animator animator;
        private Rigidbody2D rb;
        
        private void Awake()
        {
            if (animController == null)
                animController = GetComponent<PlayerAnimationController>();
                
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
        }
        
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            
            // Test Damage Animation
            if (keyboard[damageKey].wasPressedThisFrame)
            {
                if (animController != null)
                {
                    animController.TriggerDamage();
                    Debug.Log("🩸 [TEST] Damage animation triggered!");
                }
            }
            
            // Test Death Animation
            if (keyboard[deathKey].wasPressedThisFrame)
            {
                if (animController != null)
                {
                    animController.TriggerDeath();
                    Debug.Log("💀 [TEST] Death animation triggered!");
                }
            }
            
            // Reset (revive from death)
            if (keyboard[resetKey].wasPressedThisFrame)
            {
                if (animator != null)
                {
                    animator.SetBool("IsDead", false);
                    Debug.Log("♻️ [TEST] Reset - Revived!");
                }
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 14;
            style.alignment = TextAnchor.UpperLeft;
            style.normal.textColor = Color.white;
            
            string debugText = "=== ANIMATION TESTER ===\n";
            debugText += $"[{damageKey}] - Trigger Damage\n";
            debugText += $"[{deathKey}] - Trigger Death\n";
            debugText += $"[{resetKey}] - Reset/Revive\n";
            debugText += "\n=== CURRENT STATE ===\n";
            
            if (animator != null)
            {
                debugText += $"Speed: {animator.GetFloat("Speed"):F2}\n";
                debugText += $"IsRolling: {animator.GetBool("IsRolling")}\n";
                debugText += $"IsDead: {animator.GetBool("IsDead")}\n";
                
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                debugText += $"State: {GetStateName(state)}\n";
            }
            
            if (rb != null)
            {
                debugText += $"Velocity: {rb.linearVelocity.magnitude:F2}\n";
            }
            
            GUI.Box(new Rect(10, 10, 280, 200), debugText, style);
        }
        
        private string GetStateName(AnimatorStateInfo state)
        {
            if (state.IsName("Dage_Idle")) return "Idle 🧍";
            if (state.IsName("Dage_Walk")) return "Walking 🚶";
            if (state.IsName("Dage_Dash")) return "Dashing 💨";
            if (state.IsName("Dage_Damage")) return "Damaged 🩸";
            if (state.IsName("Dage_Death")) return "Dead 💀";
            if (state.IsName("playeridle")) return "Old Idle";
            if (state.IsName("playerrun")) return "Old Run";
            if (state.IsName("playerrolling")) return "Old Rolling";
            if (state.IsName("playerdying")) return "Old Dying";
            return "Unknown";
        }
    }
}
