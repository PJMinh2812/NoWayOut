using UnityEngine;

namespace GloomCraft
{
    // Quản lý vũ khí và hiển thị trên Player
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController2D controller;
        [SerializeField] private Transform handTransform;
        
        [Header("Equipment")]
        [SerializeField] private SpriteRenderer weaponRenderer;
        [SerializeField] private WeaponItem currentWeapon;

        private GameObject _weaponHolder;

        public WeaponItem CurrentWeapon => currentWeapon;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController2D>();
            
            if (handTransform == null)
            {
                var holder = new GameObject("WeaponHolder");
                handTransform = holder.transform;
                handTransform.SetParent(transform);
                handTransform.localPosition = Vector3.zero;
                _weaponHolder = holder;
            }

            if (weaponRenderer == null && handTransform != null)
            {
                var rendererGo = new GameObject("WeaponSprite");
                rendererGo.transform.SetParent(handTransform);
                rendererGo.transform.localPosition = Vector3.zero;
                weaponRenderer = rendererGo.AddComponent<SpriteRenderer>();
                weaponRenderer.sortingLayerName = "Default";
                weaponRenderer.sortingOrder = 1;
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

        public void EquipWeapon(WeaponItem weapon)
        {
            if (weapon == null) return;

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
            
            bool aimingLeft = normalizedAngle > 90f || normalizedAngle < -90f;
            
            Vector2 offset = currentWeapon.holdOffset;
            
            if (aimingLeft)
            {
                handTransform.localPosition = new Vector2(-offset.x, offset.y);
            }
            else
            {
                handTransform.localPosition = new Vector2(offset.x, offset.y);
            }

            handTransform.rotation = Quaternion.identity;

            if (weaponRenderer != null)
            {
                weaponRenderer.flipX = aimingLeft;
                
                weaponRenderer.sortingOrder = 1;
            }
        }
    }
}
