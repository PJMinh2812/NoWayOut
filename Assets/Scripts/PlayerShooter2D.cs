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
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[PlayerShooter2D] Projectile Prefab not assigned!");
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
            var dir = (Vector2)world - (Vector2)muzzle.position;

            var proj = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity); // Spawn at edge
            proj.Fire(dir);
            
            return true; // Success!
        }
    }
}


