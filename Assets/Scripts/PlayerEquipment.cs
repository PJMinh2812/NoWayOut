using UnityEngine;

namespace GloomCraft
{
    /// <summary>
    /// Manages equipped items and displays them visually on player
    /// </summary>
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController2D controller;
        [SerializeField] private Transform handTransform; // Transform cho tay cầm vũ khí
        
        [Header("Equipment")]
        [SerializeField] private SpriteRenderer weaponRenderer;
        [SerializeField] private WeaponItem currentWeapon;

        private GameObject _weaponHolder;

        public WeaponItem CurrentWeapon => currentWeapon;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController2D>();
            
            // Auto-create weapon holder if not assigned
            if (handTransform == null)
            {
                var holder = new GameObject("WeaponHolder");
                handTransform = holder.transform;
                handTransform.SetParent(transform);
                handTransform.localPosition = Vector3.zero;
                _weaponHolder = holder;
            }

            // Auto-create weapon sprite renderer
            if (weaponRenderer == null && handTransform != null)
            {
                var rendererGo = new GameObject("WeaponSprite");
                rendererGo.transform.SetParent(handTransform);
                rendererGo.transform.localPosition = Vector3.zero;
                weaponRenderer = rendererGo.AddComponent<SpriteRenderer>();
                weaponRenderer.sortingLayerName = "Default";
                weaponRenderer.sortingOrder = 1; // Render above player
            }

            if (weaponRenderer != null)
            {
                weaponRenderer.enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (currentWeapon != null && handTransform != null && controller != null)
            {
                UpdateWeaponTransform();
            }
        }

        /// <summary>
        /// Equip a weapon and display its sprite
        /// </summary>
        public void EquipWeapon(WeaponItem weapon)
        {
            if (weapon == null) return;

            // Unequip current weapon first
            if (currentWeapon != null)
            {
                UnequipWeapon();
            }

            currentWeapon = weapon;

            if (weaponRenderer != null)
            {
                weaponRenderer.sprite = weapon.weaponSprite;
                weaponRenderer.enabled = weapon.weaponSprite != null;
            }

            Debug.Log($"[PlayerEquipment] Equipped: {weapon.itemName}");
        }

        /// <summary>
        /// Unequip current weapon
        /// </summary>
        public void UnequipWeapon()
        {
            if (currentWeapon == null) return;

            Debug.Log($"[PlayerEquipment] Unequipped: {currentWeapon.itemName}");
            
            currentWeapon = null;

            if (weaponRenderer != null)
            {
                weaponRenderer.sprite = null;
                weaponRenderer.enabled = false;
            }
        }

        private void UpdateWeaponTransform()
        {
            if (handTransform == null || currentWeapon == null || controller == null) return;

            float aimAngle = controller.AimAngleDeg;
            float normalizedAngle = Mathf.DeltaAngle(0, aimAngle);
            
            // Keep weapon at fixed position on player's side (like muzzle)
            // Just flip left/right based on aim direction
            bool aimingLeft = normalizedAngle > 90f || normalizedAngle < -90f;
            
            Vector2 offset = currentWeapon.holdOffset;
            
            if (aimingLeft)
            {
                // Flip offset to left side
                handTransform.localPosition = new Vector2(-offset.x, offset.y);
            }
            else
            {
                // Keep offset on right side
                handTransform.localPosition = new Vector2(offset.x, offset.y);
            }

            // Don't rotate the weapon transform - keep it upright
            handTransform.rotation = Quaternion.identity;

            // Flip weapon sprite left/right
            if (weaponRenderer != null)
            {
                weaponRenderer.flipX = aimingLeft;
                
                // Weapon sorting: always in front for 2D top-down
                weaponRenderer.sortingOrder = 1;
            }
        }
    }
}
