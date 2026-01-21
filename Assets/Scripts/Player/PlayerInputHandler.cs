using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulKnightClone.Player
{
    /// <summary>
    /// Bridge script để kết nối Input System với PlayerController và WeaponController
    /// Gắn script này cùng GameObject với PlayerController
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInputHandler : MonoBehaviour
    {
        private PlayerController playerController;
        private Weapons.WeaponController weaponController;
        private PlayerInput playerInput;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            weaponController = GetComponentInChildren<Weapons.WeaponController>();
            playerInput = GetComponent<PlayerInput>();

            if (playerInput == null)
            {
                playerInput = gameObject.AddComponent<PlayerInput>();
            }
        }

        private void Start()
        {
            // Setup Input Actions
            if (playerInput.actions == null)
            {
                Debug.LogWarning("Input Actions chưa được assign! Hãy tạo Input Actions asset.");
            }
        }

        #region Input Callbacks
        // Các callback này sẽ được gọi tự động từ Input System
        // Đảm bảo tên method khớp với Action name trong Input Actions asset

        public void OnMove(InputAction.CallbackContext context)
        {
            if (playerController != null)
            {
                playerController.OnMove(context);
            }
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if (playerController != null)
            {
                playerController.OnDash(context);
            }
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            if (weaponController != null)
            {
                weaponController.OnFire(context);
            }
        }
        #endregion
    }
}
