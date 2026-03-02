using UnityEngine;

namespace NWO
{
    /// <summary>
    /// MeleeAnimatorEvents - Component để nhận Animation Events từ staff attack animation
    /// Gắn lên cùng GameObject có Animator
    /// </summary>
    public class MeleeAnimatorEvents : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMeleeController meleeController;
        [SerializeField] private MeleeHitbox meleeHitbox;
        
        [Header("Hitbox Timing")]
        [Tooltip("Thời gian hitbox active (giây)")]
        [SerializeField] private float hitboxDuration = 0.15f;
        
        private void Awake()
        {
            // Auto-find nếu không set
            if (meleeController == null)
                meleeController = GetComponentInParent<PlayerMeleeController>();
            
            if (meleeHitbox == null)
                meleeHitbox = GetComponentInChildren<MeleeHitbox>();
        }
        
        /// <summary>
        /// Animation Event - Gọi khi animation đến frame swing
        /// Sử dụng trong Unity Animation window
        /// </summary>
        public void OnSwingStart()
        {
            Debug.Log("[MeleeAnimEvent] Swing started!");
            
            // Kích hoạt hitbox
            if (meleeHitbox != null)
            {
                meleeHitbox.ActivateForDuration(hitboxDuration);
            }
        }
        
        /// <summary>
        /// Animation Event - Gọi khi đòn đánh chạm (active frame)
        /// </summary>
        public void OnHitFrame()
        {
            Debug.Log("[MeleeAnimEvent] Hit frame!");
            
            if (meleeController != null)
            {
                meleeController.OnMeleeHitFrame();
            }
        }
        
        /// <summary>
        /// Animation Event - Gọi khi animation kết thúc
        /// </summary>
        public void OnSwingEnd()
        {
            Debug.Log("[MeleeAnimEvent] Swing ended!");
            
            // Tắt hitbox
            if (meleeHitbox != null)
            {
                meleeHitbox.DeactivateHitbox();
            }
            
            if (meleeController != null)
            {
                meleeController.OnMeleeAnimationEnd();
            }
        }
        
        /// <summary>
        /// Animation Event - Cho phép combo input
        /// </summary>
        public void OnComboWindow()
        {
            Debug.Log("[MeleeAnimEvent] Combo window open!");
            // Combo window được handle trong PlayerMeleeController
        }
    }
}
