using UnityEngine;
using UnityEngine.InputSystem;

namespace NWO
{
    // Báº¯n projectile theo hÆ°á»›ng chuá»™t, táº¡o vÃ  quáº£n lÃ½ muzzle position
    public sealed class PlayerShooter2D : MonoBehaviour
    {
        [SerializeField] private PlayerController2D controller;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private PlayerEquipment equipment;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Projectile2D projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private float fireCooldown = 0.12f;
        [SerializeField] private float muzzleDistance = 0.5f;

        private float _cooldown;
        public float FireCooldown => fireCooldown;
        public float FireRatePerSecond => fireCooldown > 0.0001f ? 1f / fireCooldown : 0f;

        /// <summary>Giảm fire cooldown (tăng tốc độ bắn). Giá trị dương = giảm cooldown.</summary>
        public void AddFireRate(float reductionAmount)
        {
            fireCooldown = Mathf.Max(0.03f, fireCooldown - reductionAmount);
        }

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (equipment == null) equipment = GetComponent<PlayerEquipment>();
            if (worldCamera == null) worldCamera = Camera.main;
            
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
                if (Fire())
                {
                    _cooldown = fireCooldown;
                }
            }
        }

        private void UpdateMuzzlePosition()
        {
            if (muzzle == null || controller == null) return;
            
            float angleRad = controller.AimAngleDeg * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0);
            
            muzzle.localPosition = dir * muzzleDistance;
        }

        private bool Fire()
        {
            if (controller == null) return false;
            
            Projectile2D projToFire = projectilePrefab;
            
            if (equipment != null && equipment.CurrentWeapon != null)
            {
                var weapon = equipment.CurrentWeapon;
                
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

            float angleRad = controller.AimAngleDeg * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));


            
            var proj = Instantiate(projToFire, muzzle.position, Quaternion.identity);
            proj.Fire(dir);
            
            return true;
        }
    }
}


