using UnityEngine;

namespace NWO
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class NightBonesPoisonProjectile : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float speed = 6f;
        [SerializeField] private int impactDamage = 1;
        [SerializeField] private float knockbackForce = 2.5f;
        [SerializeField] private float lifetime = 5f;

        [Header("Poison")]
        [SerializeField] private float poisonDuration = 4f;
        [SerializeField] private float poisonDamagePerTick = 1f;
        [SerializeField] private float poisonTickInterval = 1f;

        private Rigidbody2D rb;
        private Vector2 direction;
        private bool isDestroyed;
        private NightBonesBoss owner;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        public void Fire(Vector2 fireDirection, NightBonesBoss projectileOwner = null, float speedOverride = -1f, int impactDamageOverride = -1)
        {
            if (speedOverride > 0f) speed = speedOverride;
            if (impactDamageOverride > 0) impactDamage = impactDamageOverride;

            owner = projectileOwner;
            IgnoreOwnerCollision();

            direction = fireDirection.normalized;
            rb.linearVelocity = direction * speed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDestroyed) return;
            if (ShouldIgnore(other.gameObject)) return;

            TryAffectPlayer(other.gameObject);
            Explode();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isDestroyed) return;
            if (ShouldIgnore(collision.collider.gameObject)) return;

            TryAffectPlayer(collision.collider.gameObject);
            Explode();
        }

        private bool ShouldIgnore(GameObject target)
        {
            if (target == null) return true;
            if (owner != null && target == owner.gameObject) return true;
            if (target.GetComponent<NightBonesBoss>() != null) return true;
            if (target.GetComponent<GoatManBoss>() != null) return true;
            if (target.GetComponent<RatMiniBoss>() != null) return true;
            if (target.GetComponent<NightBonesPoisonProjectile>() != null) return true;
            if (target.GetComponent<NightBonesHomingProjectile>() != null) return true;
            if (target.GetComponent<BossFireball>() != null) return true;
            return false;
        }

        private void TryAffectPlayer(GameObject target)
        {
            var playerHealth = target.GetComponent<PlayerHealth2D>();
            if (playerHealth == null) return;

            var playerRb = target.GetComponent<Rigidbody2D>();
            playerHealth.TakeDamage(impactDamage, direction * knockbackForce, playerRb);

            var statusEffects = target.GetComponent<PlayerStatusEffects>();
            if (statusEffects != null)
            {
                statusEffects.ApplyDoT(StatusEffectType.Poison, poisonDuration, poisonDamagePerTick, poisonTickInterval);
            }
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
