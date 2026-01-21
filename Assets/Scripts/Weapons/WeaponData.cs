using UnityEngine;

namespace SoulKnightClone.Weapons
{
    /// <summary>
    /// ScriptableObject chứa dữ liệu của vũ khí
    /// Cho phép tạo nhiều loại vũ khí khác nhau mà không cần viết lại code
    /// </summary>
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Soul Knight/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Weapon Info")]
        public string weaponName = "Pistol";
        public Core.WeaponType weaponType = Core.WeaponType.Pistol;
        public Sprite weaponSprite;

        [Header("Stats")]
        [Tooltip("Sát thương mỗi viên đạn")]
        public int damage = 10;
        
        [Tooltip("Năng lượng tiêu tốn mỗi lần bắn")]
        public int energyCost = 5;

        [Header("Fire Rate")]
        [Tooltip("Số viên bắn mỗi giây")]
        public float fireRate = 5f;
        
        [Tooltip("True = Automatic (giữ chuột), False = Semi-Auto (nhấn từng lần)")]
        public bool isAutomatic = false;

        [Header("Projectile")]
        [Tooltip("Tốc độ đạn")]
        public float bulletSpeed = 20f;
        
        [Tooltip("Thời gian sống của đạn (giây)")]
        public float bulletLifetime = 3f;
        
        [Tooltip("Tag pool để spawn đạn")]
        public string bulletPoolTag = "BulletPistol";

        [Header("Accuracy")]
        [Tooltip("Độ chính xác (0 = hoàn hảo, 10 = rất lệch)")]
        [Range(0f, 20f)]
        public float accuracy = 2f;
        
        [Tooltip("Số viên đạn mỗi lần bắn (Shotgun = nhiều viên)")]
        public int bulletsPerShot = 1;
        
        [Tooltip("Góc spread cho shotgun (độ)")]
        [Range(0f, 90f)]
        public float spreadAngle = 10f;

        [Header("Recoil")]
        [Tooltip("Lực giật ngược khi bắn")]
        public float recoilForce = 0.5f;

        [Header("Effects")]
        public AudioClip fireSound;
        public GameObject muzzleFlashPrefab;
        
        [Tooltip("Độ rung màn hình khi bắn")]
        [Range(0f, 1f)]
        public float screenShakeIntensity = 0.1f;

        // Helper method
        public float GetFireInterval()
        {
            return 1f / fireRate;
        }
    }
}
