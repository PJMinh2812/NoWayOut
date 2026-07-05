using UnityEngine;
using NWO;

/// <summary>
/// Vùng chết - Kill player và respawn về checkpoint
/// </summary>
public class DeathZone : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private int damage = 999; // Giết tức thì
    
    [Header("Effects")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private GameObject deathEffect;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("DeathZone: Player entered death zone!");
            
            // Gây sát thương/giết player
            PlayerHealth2D playerHealth = collision.GetComponent<PlayerHealth2D>();
            Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
            if (playerHealth != null)
            {
                Debug.Log("DeathZone: Damaging player...");
                Vector2 knockback = Vector2.down * 2f;
                playerHealth.TakeDamage(damage, knockback, playerRb);
            }
            
            // Phát âm thanh chết
            if (deathSound != null)
            {
                AudioSource.PlayClipAtPoint(deathSound, transform.position);
            }
            
            // Hiệu ứng chết
            if (deathEffect != null)
            {
                Instantiate(deathEffect, collision.transform.position, Quaternion.identity);
            }
            
            // Respawn (nếu có respawn point)
            if (respawnPoint != null && playerHealth != null && playerHealth.CurrentHealth <= 0)
            {
                Debug.Log($"DeathZone: Respawning player at {respawnPoint.position}");
                Invoke(nameof(RespawnPlayer), 1f);
            }
        }
    }
    
    private void RespawnPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && respawnPoint != null)
        {
            player.transform.position = respawnPoint.position;
            
            PlayerHealth2D playerHealth = player.GetComponent<PlayerHealth2D>();
            if (playerHealth != null)
            {
                playerHealth.ResetHealth(); // Hồi đầy máu
            }
            
            Debug.Log("DeathZone: Player respawned!");
        }
    }
    
    // Vẽ death zone trong Scene view
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.DrawCube(transform.position, col.size);
        }
    }
}
