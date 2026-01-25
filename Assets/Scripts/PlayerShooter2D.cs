using UnityEngine;
using UnityEngine.InputSystem;

namespace GloomCraft
{
    /// <summary>
    /// Minimal shooting scaffold: left click spawns a Projectile2D prefab and fires toward mouse.
    /// Automatically creates and manages muzzle position.
    /// </summary>
    public sealed class PlayerShooter2D : MonoBehaviour
    {
        [SerializeField] private PlayerController2D controller;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private PlayerEquipment equipment;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Projectile2D projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private float fireCooldown = 0.12f;
        [SerializeField] private float muzzleDistance = 0.5f; // Distance from player center to edge

        private float _cooldown;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (equipment == null) equipment = GetComponent<PlayerEquipment>();
            if (worldCamera == null) worldCamera = Camera.main;
            
            // Auto-create muzzle if not assigned
            if (muzzle == null)
            {
                var muzzleGo = new GameObject("Muzzle");
                muzzle = muzzleGo.transform;
                muzzle.SetParent(transform);
                muzzle.localPosition = Vector3.right * muzzleDistance;
            }
        }

        private void Update()
        {
            UpdateMuzzlePosition();
            
            if (_cooldown > 0f) _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                if (Fire()) // Only set cooldown if Fire() succeeded
                {
                    _cooldown = fireCooldown;
                }
            }
        }

        private void UpdateMuzzlePosition()
        {
            if (muzzle == null || controller == null) return;
            
            // Position muzzle in front of player based on aim angle
            var angleRad = controller.AimAngleDeg * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0);
            
            // Adjust for sprite flip - when sprite is flipped, mirror the X position
            if (spriteRenderer != null && spriteRenderer.flipX)
            {
                direction.x = -direction.x;
            }
            
            muzzle.localPosition = direction * muzzleDistance;
        }

        private bool Fire()
        {
            // Check if player has equipped weapon
            Projectile2D projToFire = projectilePrefab;
            
            if (equipment != null && equipment.CurrentWeapon != null)
            {
                var weapon = equipment.CurrentWeapon;
                
                // Use weapon's projectile if it's a ranged weapon
                if (weapon.isRanged && weapon.projectilePrefab != null)
                {
                    projToFire = weapon.projectilePrefab;
                }
                
                Debug.Log($"[PlayerShooter2D] Firing {weapon.itemName} (Damage: {weapon.damage})");
            }
            
            if (projToFire == null)
            {
                Debug.LogWarning("[PlayerShooter2D] No projectile! Assign Projectile Prefab or equip a weapon.");
                return false;
            }
            
            if (worldCamera == null)
            {
                Debug.LogWarning("[PlayerShooter2D] World Camera not assigned!");
                return false;
            }

            var mouse = Mouse.current;
            if (mouse == null) return false;

            var mousePos = mouse.position.ReadValue();
            var world = worldCamera.ScreenToWorldPoint(mousePos);
            var dir = (Vector2)world - (Vector2)transform.position; // Direction from player center, not muzzle!

            var proj = Instantiate(projToFire, muzzle.position, Quaternion.identity);
            proj.Fire(dir);
            
            return true; // Success!
        }
    }
}


