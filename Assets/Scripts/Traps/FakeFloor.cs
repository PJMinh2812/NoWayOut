using UnityEngine;

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
    
    private SpriteRenderer spriteRenderer;
    private Collider2D floorCollider;
    private Color originalColor;
    private bool isTriggered = false;
    private bool isFalling = false;
    
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
        if (isTriggered || isFalling) return;
        
        // Chỉ kích hoạt khi player bước vào
        if (collision.CompareTag("Player"))
        {
            TriggerFall();
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isTriggered || isFalling) return;
        
        if (collision.gameObject.CompareTag("Player"))
        {
            TriggerFall();
        }
    }
    
    private void TriggerFall()
    {
        isTriggered = true;
        
        // Phát âm thanh nứt
        if (crackSound != null)
        {
            AudioSource.PlayClipAtPoint(crackSound, transform.position);
        }
        
        if (showWarning && spriteRenderer != null)
        {
            StartCoroutine(WarningFlash());
        }
        else
        {
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
        isFalling = true;
        
        // Phát âm thanh rơi
        if (fallSound != null)
        {
            AudioSource.PlayClipAtPoint(fallSound, transform.position);
        }
        
        // Tắt collider để người chơi rơi xuyên qua
        if (floorCollider != null)
        {
            floorCollider.enabled = false;
        }
        
        // Làm sprite biến mất dần
        if (spriteRenderer != null)
        {
            StartCoroutine(FadeOut());
        }
        
        // Hồi phục sau một thời gian
        if (respawnTime > 0)
        {
            Invoke(nameof(Respawn), respawnTime);
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
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        if (GetComponent<Collider2D>() != null)
        {
            Gizmos.DrawCube(transform.position, GetComponent<Collider2D>().bounds.size);
        }
    }
}
