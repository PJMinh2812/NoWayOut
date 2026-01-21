namespace SoulKnightClone.Core
{
    /// <summary>
    /// Constants và Enums cho toàn bộ game
    /// </summary>
    public static class GameConstants
    {
        // Layers
        public const string LAYER_PLAYER = "Player";
        public const string LAYER_ENEMY = "Enemy";
        public const string LAYER_WALL = "Wall";
        public const string LAYER_OBSTACLE = "Obstacle";
        public const string LAYER_PROJECTILE = "Projectile";

        // Tags
        public const string TAG_PLAYER = "Player";
        public const string TAG_ENEMY = "Enemy";
        public const string TAG_WALL = "Wall";
        public const string TAG_BULLET = "Bullet";

        // Pool Tags
        public const string POOL_BULLET_PISTOL = "BulletPistol";
        public const string POOL_BULLET_SHOTGUN = "BulletShotgun";
        public const string POOL_BULLET_RIFLE = "BulletRifle";
        public const string POOL_HIT_EFFECT = "HitEffect";
        public const string POOL_MUZZLE_FLASH = "MuzzleFlash";

        // Animation Parameters
        public const string ANIM_MOVE_X = "MoveX";
        public const string ANIM_MOVE_Y = "MoveY";
        public const string ANIM_IS_MOVING = "IsMoving";
        public const string ANIM_IS_DASHING = "IsDashing";
        public const string ANIM_SHOOT = "Shoot";
        public const string ANIM_HURT = "Hurt";
        public const string ANIM_DEATH = "Death";

        // Player Stats Default
        public const float DEFAULT_MOVE_SPEED = 5f;
        public const float DEFAULT_DASH_SPEED = 15f;
        public const float DEFAULT_DASH_DURATION = 0.2f;
        public const float DEFAULT_DASH_COOLDOWN = 1f;
        public const int DEFAULT_MAX_HEALTH = 100;
        public const int DEFAULT_MAX_ARMOR = 50;
        public const int DEFAULT_MAX_ENERGY = 200;
        public const float DEFAULT_ARMOR_REGEN_DELAY = 3f;
        public const float DEFAULT_ARMOR_REGEN_RATE = 10f;

        // Camera
        public const float CAMERA_SMOOTHING = 5f;
        public const float SCREEN_SHAKE_INTENSITY = 0.3f;
        public const float SCREEN_SHAKE_DURATION = 0.1f;
    }

    public enum WeaponType
    {
        Pistol,
        Shotgun,
        Rifle,
        SMG,
        Sniper
    }

    public enum ProjectileType
    {
        Normal,
        Explosive,
        Piercing,
        Homing
    }

    public enum EnemyState
    {
        Idle,
        Wander,
        Chase,
        Attack,
        Hurt,
        Death
    }

    public enum RoomType
    {
        Start,
        Combat,
        Treasure,
        Boss,
        Shop
    }
}
