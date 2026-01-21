using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulKnightClone.Weapons
{
    /// <summary>
    /// Controller cho vũ khí - xử lý xoay theo chuột, bắn đạn, fire rate
    /// Gắn script này vào Weapon Pivot (child object của Player)
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] private WeaponData currentWeapon;
        [SerializeField] private Transform firePoint; // Điểm spawn đạn
        [SerializeField] private SpriteRenderer weaponSprite;

        [Header("References")]
        private Player.PlayerController playerController;
        private Player.PlayerStats playerStats;
        private Camera mainCamera;

        // Fire rate control
        private float nextFireTime = 0f;
        private bool isFiring = false;

        // Audio
        private AudioSource audioSource;

        private void Awake()
        {
            playerController = GetComponentInParent<Player.PlayerController>();
            playerStats = GetComponentInParent<Player.PlayerStats>();
            mainCamera = Camera.main;
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Start()
        {
            if (currentWeapon != null)
            {
                LoadWeapon(currentWeapon);
            }
        }

        private void Update()
        {
            RotateWeaponToMouse();
            HandleFiring();
        }

        #region Weapon Rotation
        private void RotateWeaponToMouse()
        {
            if (playerController == null) return;

            Vector2 mousePos = playerController.GetMouseWorldPosition();
            Vector2 direction = mousePos - (Vector2)transform.position;
            
            // Tính góc xoay
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // Flip weapon sprite khi nhìn sang trái
            if (weaponSprite != null)
            {
                if (direction.x < 0)
                {
                    weaponSprite.flipY = true;
                }
                else
                {
                    weaponSprite.flipY = false;
                }
            }
        }
        #endregion

        #region Firing System
        private void HandleFiring()
        {
            if (currentWeapon == null) return;

            // Kiểm tra fire rate cooldown
            if (Time.time < nextFireTime) return;

            if (currentWeapon.isAutomatic)
            {
                // Automatic: giữ chuột liên tục
                if (isFiring)
                {
                    Fire();
                }
            }
            else
            {
                // Semi-Auto: bắn 1 lần khi nhấn
                // Được xử lý trong OnFire callback
            }
        }

        private void Fire()
        {
            if (currentWeapon == null || firePoint == null) return;

            // Kiểm tra năng lượng
            if (!playerStats.ConsumeEnergy(currentWeapon.energyCost))
            {
                // TODO: Play "out of energy" sound
                return;
            }

            // Spawn bullets
            for (int i = 0; i < currentWeapon.bulletsPerShot; i++)
            {
                SpawnBullet(i);
            }

            // Update fire rate
            nextFireTime = Time.time + currentWeapon.GetFireInterval();

            // Effects
            PlayFireEffects();
        }

        private void SpawnBullet(int bulletIndex)
        {
            // Lấy bullet từ pool
            GameObject bulletObj = Core.ObjectPooler.Instance?.SpawnFromPool(
                currentWeapon.bulletPoolTag,
                firePoint.position,
                firePoint.rotation
            );

            if (bulletObj == null)
            {
                Debug.LogWarning($"Bullet pool '{currentWeapon.bulletPoolTag}' không tồn tại!");
                return;
            }

            // Calculate spread cho shotgun
            float spreadOffset = 0f;
            if (currentWeapon.bulletsPerShot > 1)
            {
                float totalSpread = currentWeapon.spreadAngle;
                float spreadStep = totalSpread / (currentWeapon.bulletsPerShot - 1);
                spreadOffset = -totalSpread / 2f + (spreadStep * bulletIndex);
            }

            // Apply accuracy (random spread)
            float randomSpread = Random.Range(-currentWeapon.accuracy, currentWeapon.accuracy);
            float totalSpread = spreadOffset + randomSpread;

            // Set bullet direction
            Quaternion spreadRotation = Quaternion.Euler(0, 0, totalSpread);
            Vector2 direction = spreadRotation * firePoint.right;

            // Initialize bullet
            Projectile projectile = bulletObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(
                    direction,
                    currentWeapon.bulletSpeed,
                    currentWeapon.damage,
                    currentWeapon.bulletLifetime,
                    "Player" // Tag của người bắn
                );
            }
        }

        private void PlayFireEffects()
        {
            // Sound
            if (currentWeapon.fireSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(currentWeapon.fireSound);
            }

            // Muzzle flash
            if (currentWeapon.muzzleFlashPrefab != null && firePoint != null)
            {
                GameObject flash = Instantiate(
                    currentWeapon.muzzleFlashPrefab,
                    firePoint.position,
                    firePoint.rotation,
                    firePoint
                );
                Destroy(flash, 0.1f);
            }

            // Screen shake
            if (currentWeapon.screenShakeIntensity > 0)
            {
                // TODO: Implement camera shake through Cinemachine
                // CameraShaker.Instance.ShakeCamera(currentWeapon.screenShakeIntensity, 0.1f);
            }

            // TODO: Recoil animation
        }
        #endregion

        #region Input Callbacks
        public void OnFire(InputAction.CallbackContext context)
        {
            if (currentWeapon == null) return;

            if (context.performed)
            {
                isFiring = true;
                
                // Semi-auto: bắn ngay khi nhấn
                if (!currentWeapon.isAutomatic)
                {
                    Fire();
                }
            }
            else if (context.canceled)
            {
                isFiring = false;
            }
        }
        #endregion

        #region Weapon Management
        public void LoadWeapon(WeaponData newWeapon)
        {
            currentWeapon = newWeapon;
            
            if (weaponSprite != null && newWeapon.weaponSprite != null)
            {
                weaponSprite.sprite = newWeapon.weaponSprite;
            }
        }

        public WeaponData GetCurrentWeapon() => currentWeapon;
        #endregion
    }
}
