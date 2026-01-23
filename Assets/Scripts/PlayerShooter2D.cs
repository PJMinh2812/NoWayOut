using UnityEngine;
using UnityEngine.InputSystem;

namespace GloomCraft
{
    /// <summary>
    /// Minimal shooting scaffold: left click spawns a Projectile2D prefab and fires toward mouse.
    /// </summary>
    public sealed class PlayerShooter2D : MonoBehaviour
    {
        [SerializeField] private PlayerController2D controller;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Projectile2D projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private float fireCooldown = 0.12f;

        private float _cooldown;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController2D>();
            if (worldCamera == null) worldCamera = Camera.main;
            if (muzzle == null) muzzle = transform;
        }

        private void Update()
        {
            if (_cooldown > 0f) _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                Fire();
                _cooldown = fireCooldown;
            }
        }

        private void Fire()
        {
            if (projectilePrefab == null || worldCamera == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            var mousePos = mouse.position.ReadValue();
            var world = worldCamera.ScreenToWorldPoint(mousePos);
            var dir = (Vector2)world - (Vector2)muzzle.position;

            var proj = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
            proj.Fire(dir);
        }
    }
}


