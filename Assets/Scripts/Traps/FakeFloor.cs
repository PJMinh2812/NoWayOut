using UnityEngine;
using NWO;

/// <summary>
/// Gạch rơi giả - Trông giống sàn thường nhưng sẽ biến mất khi người chơi bước vào
/// Sử dụng cho: Phòng Start - cái bẫy đầu tiên ngay sau tấm biển cảnh báo
/// </summary>
public class FakeFloor : MonoBehaviour
{
    [Header("Fake Floor Settings")]
    [Tooltip("Thời gian delay trước khi gạch rơi (giây)")]
    [SerializeField] private float fallDelay = 0.1f;
    
    [Tooltip("Thời gian gạch tự hồi phục (0 = không hồi phục)")]
    [SerializeField] private float respawnTime = 5f;
    
    [Tooltip("Có phát hiệu ứng cảnh báo không?")]
    [SerializeField] private bool showWarning = true;
    
    [Tooltip("Màu nhấp nháy cảnh báo")]
    [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0.8f, 1f);
    
    [Header("Audio")]
    [SerializeField] private AudioClip crackSound;
    [SerializeField] private AudioClip fallSound;
    
    [Header("Damage Settings")]
    [Tooltip("Gây sát thương cho player khi rơi? (0 = không gây damage)")]
    [SerializeField] private int damage = 0;
    
    [Tooltip("Giết player ngay lập tức?")]
    [SerializeField] private bool instantKill = false;
    
    [Tooltip("Respawn point khi player chết (nếu instant kill)")]
    [SerializeField] private Transform respawnPoint;
    
    private SpriteRenderer spriteRenderer;
    private Collider2D floorCollider;
    private Color originalColor;
    private bool isTriggered = false;
    private bool isFalling = false;
    private GameObject fallingPlayer; // Lưu player đang rơi
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        floorCollider = GetComponent<Collider2D>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            
            // Làm màu nhạt hơn một chút để người chơi cẩn thận có thể nhận ra
            if (showWarning)
            {
                spriteRenderer.color = new Color(
                    originalColor.r * 0.95f,
                    originalColor.g * 0.95f,
                    originalColor.b * 0.95f,
                    originalColor.a
                );
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"FakeFloor: OnTriggerEnter2D - Object: {collision.gameObject.name}, Tag: {collision.tag}");
        
        if (isTriggered || isFalling) return;
        
        // Chỉ kích hoạt khi player bước vào
        if (collision.CompareTag("Player"))
        {
            Debug.Log("FakeFloor: Player detected! Triggering fall...");
            fallingPlayer = collision.gameObject;
            TriggerFall();
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isTriggered || isFalling) return;
        
        if (collision.gameObject.CompareTag("Player"))
        {
            fallingPlayer = collision.gameObject;
            TriggerFall();
        }
    }
    
    private void TriggerFall()
    {
        Debug.Log("FakeFloor: TriggerFall() called!");
        isTriggered = true;
        
        // Phát âm thanh nứt
        if (crackSound != null)
        {
            AudioSource.PlayClipAtPoint(crackSound, transform.position);
        }
        
        if (showWarning && spriteRenderer != null)
        {
            Debug.Log("FakeFloor: Starting warning flash...");
            StartCoroutine(WarningFlash());
        }
        else
        {
            Debug.Log($"FakeFloor: Invoking Fall() with delay {fallDelay}s");
            Invoke(nameof(Fall), fallDelay);
        }
    }
    
    private System.Collections.IEnumerator WarningFlash()
    {
        float elapsed = 0f;
        
        while (elapsed < fallDelay)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 10f, 1f);
            spriteRenderer.color = Color.Lerp(originalColor, warningColor, t);
            yield return null;
        }
        
        Fall();
    }
    
    private void Fall()
    {
        Debug.Log("FakeFloor: Fall() - Starting fall sequence!");
        isFalling = true;
        
        // Phát âm thanh rơi
        if (fallSound != null)
        {
            AudioSource.PlayClipAtPoint(fallSound, transform.position);
        }
        
        // Tắt collider để người chơi rơi xuyên qua
        if (floorCollider != null)
        {
            Debug.Log("FakeFloor: Disabling collider...");
            floorCollider.enabled = false;
        }
        
        // Làm sprite biến mất dần
        if (spriteRenderer != null)
        {
            Debug.Log("FakeFloor: Starting fade out...");
            StartCoroutine(FadeOut());
        }
        
        // Gây damage hoặc kill player
        DamagePlayer();
        
        // Hồi phục gạch sau một thời gian
        if (respawnTime > 0)
        {
            Debug.Log($"FakeFloor: Floor will respawn in {respawnTime}s");
            Invoke(nameof(Respawn), respawnTime);
        }
    }
    
    private void DamagePlayer()
    {
        if (fallingPlayer == null) return;
        
        PlayerHealth2D playerHealth = fallingPlayer.GetComponent<PlayerHealth2D>();
        if (playerHealth != null)
        {
            Rigidbody2D playerRb = fallingPlayer.GetComponent<Rigidbody2D>();
            Vector2 knockbackForce = Vector2.down * 5f; // Đẩy xuống
            
            if (instantKill)
            {
                Debug.Log("FakeFloor: Instant killing player!");
                playerHealth.TakeDamage(999, knockbackForce, playerRb);
                
                // Respawn player sau 1.5s
                if (respawnPoint != null)
                {
                    Debug.Log("FakeFloor: Player will respawn in 1.5s...");
                    Invoke(nameof(RespawnPlayer), 1.5f);
                }
            }
            else if (damage > 0)
            {
                Debug.Log($"FakeFloor: Damaging player for {damage} HP");
                playerHealth.TakeDamage(damage, knockbackForce, playerRb);
            }
        }
    }
    
    private void RespawnPlayer()
    {
        if (fallingPlayer != null && respawnPoint != null)
        {
            Debug.Log($"FakeFloor: Respawning player at {respawnPoint.position}");
            fallingPlayer.transform.position = respawnPoint.position;
            
            PlayerHealth2D playerHealth = fallingPlayer.GetComponent<PlayerHealth2D>();
            if (playerHealth != null)
            {
                playerHealth.ResetHealth(); // Reset về full HP
            }
        }
    }
    
    private System.Collections.IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float duration = 0.3f;
        Color startColor = spriteRenderer.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        
        spriteRenderer.enabled = false;
    }
    
    private void Respawn()
    {
        isTriggered = false;
        isFalling = false;
        fallingPlayer = null;
        
        if (floorCollider != null)
        {
            floorCollider.enabled = true;
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
        }
    }
    
    // Để debug trong editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0.5f, 0.5f);
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}
