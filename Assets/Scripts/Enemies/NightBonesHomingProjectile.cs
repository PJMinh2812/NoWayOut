using UnityEngine;

namespace NWO
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class NightBonesHomingProjectile : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float speed = 5f;
        [SerializeField] private int damage = 2;
        [SerializeField] private float knockbackForce = 3.5f;
        [SerializeField] private float lifetime = 6f;

        [Header("Homing")]
        [SerializeField] private float homingStrength = 6f;

        private Rigidbody2D rb;
        private Transform target;
        private Vector2 direction;
        private NightBonesBoss owner;
        private bool isDestroyed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        public void Fire(Transform targetTransform, Vector2 initialDirection, NightBonesBoss projectileOwner = null, float speedOverride = -1f, int damageOverride = -1)
        {
            if (speedOverride > 0f) speed = speedOverride;
            if (damageOverride > 0) damage = damageOverride;

            target = targetTransform;
            owner = projectileOwner;
            IgnoreOwnerCollision();

            direction = initialDirection.sqrMagnitude > 0.001f ? initialDirection.normalized : Vector2.right;
            rb.linearVelocity = direction * speed;
            RotateToDirection(direction);

            Destroy(gameObject, lifetime);
        }

        private void FixedUpdate()
        {
            if (isDestroyed) return;

            if (target != null)
            {
                Vector2 desired = ((Vector2)target.position - (Vector2)transform.position).normalized;
                if (desired.sqrMagnitude > 0.0001f)
                    direction = Vector2.Lerp(direction, desired, homingStrength * Time.fixedDeltaTime).normalized;
            }

            rb.linearVelocity = direction * speed;
            RotateToDirection(direction);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDestroyed) return;
            if (ShouldIgnore(other.gameObject)) return;

            TryDamagePlayer(other.gameObject);
            Explode();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isDestroyed) return;
            if (ShouldIgnore(collision.collider.gameObject)) return;

            TryDamagePlayer(collision.collider.gameObject);
            Explode();
        }

        private bool ShouldIgnore(GameObject targetObject)
        {
            if (targetObject == null) return true;
            if (owner != null && targetObject == owner.gameObject) return true;
            if (targetObject.GetComponent<NightBonesBoss>() != null) return true;
            if (targetObject.GetComponent<GoatManBoss>() != null) return true;
            if (targetObject.GetComponent<RatMiniBoss>() != null) return true;
            if (targetObject.GetComponent<NightBonesPoisonProjectile>() != null) return true;
            if (targetObject.GetComponent<NightBonesHomingProjectile>() != null) return true;
            if (targetObject.GetComponent<BossFireball>() != null) return true;
            return false;
        }

        private void TryDamagePlayer(GameObject targetObject)
        {
            var playerHealth = targetObject.GetComponent<PlayerHealth2D>();
            if (playerHealth == null) return;

            Rigidbody2D playerRb = targetObject.GetComponent<Rigidbody2D>();
            playerHealth.TakeDamage(damage, direction * knockbackForce, playerRb);
        }

        private void RotateToDirection(Vector2 dir)
        {
            if (dir.sqrMagnitude <= 0.0001f) return;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void IgnoreOwnerCollision()
        {
            if (owner == null) return;

            var myCollider = GetComponent<Collider2D>();
            if (myCollider == null) return;

            var ownerColliders = owner.GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < ownerColliders.Length; i++)
            {
                if (ownerColliders[i] != null)
                    Physics2D.IgnoreCollision(myCollider, ownerColliders[i], true);
            }
        }

        private void Explode()
        {
            if (isDestroyed) return;
            isDestroyed = true;

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            rb.linearVelocity = Vector2.zero;

            Destroy(gameObject);
        }
    }
}
