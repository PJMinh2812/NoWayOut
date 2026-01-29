using UnityEngine;
using NWO;

/// <summary>
/// Gạch tàng hình - Khối vô hình trên không trung đập đầu người chơi khi nhảy
/// Sử dụng cho: Khu Vực 1 - Hành Lang màu vàng, trên hố nhỏ
/// </summary>
public class InvisibleBlock : MonoBehaviour
{
    [Header("Invisible Block Settings")]
    [Tooltip("Có hiện ra sau khi đập đầu không?")]
    [SerializeField] private bool revealOnHit = true;
    
    [Tooltip("Thời gian hiện lên")]
    [SerializeField] private float revealDuration = 0.5f;
    
    [Tooltip("Có biến mất sau khi hiện không?")]
    [SerializeField] private bool fadeAfterReveal = true;
    
    [Tooltip("Thời gian trước khi biến mất")]
    [SerializeField] private float fadeDelay = 2f;
    
    [Header("Knockback")]
    [Tooltip("Lực đẩy người chơi xuống dưới")]
    [SerializeField] private float knockbackForce = 10f;
    
    [Tooltip("Có gây sát thương không?")]
    [SerializeField] private bool dealDamage = false;
    
    [SerializeField] private int damageAmount = 1;
    
    [Header("Audio & Visual")]
    [SerializeField] private AudioClip bonkSound;
    [SerializeField] private GameObject bonkEffect; // Hiệu ứng dấu sao hoặc chữ "BONK!"
    [SerializeField] private Color revealColor = Color.white;
    
    private SpriteRenderer spriteRenderer;
    private Collider2D blockCollider;
    private bool hasBeenHit = false;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        blockCollider = GetComponent<Collider2D>();
        
        // Ẩn sprite ban đầu
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        // Đảm bảo collider vẫn hoạt động dù sprite ẩn
        if (blockCollider == null)
        {
            blockCollider = gameObject.AddComponent<BoxCollider2D>();
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasBeenHit) return;
        
        if (collision.gameObject.CompareTag("Player"))
        {
            // Kiểm tra xem người chơi đang đập đầu vào (va chạm từ dưới lên)
            Vector2 contactNormal = collision.contacts[0].normal;
            
            // Normal hướng xuống nghĩa là người chơi đang đập đầu vào
            if (contactNormal.y < -0.5f)
            {
                OnPlayerHit(collision.gameObject);
            }
        }
    }
    
    private void OnPlayerHit(GameObject player)
    {
        hasBeenHit = true;
        
        // Phát âm thanh "BONK!"
        if (bonkSound != null)
        {
            AudioSource.PlayClipAtPoint(bonkSound, transform.position, 1f);
        }
        
        // Hiệu ứng visual
        if (bonkEffect != null)
        {
            Instantiate(bonkEffect, transform.position, Quaternion.identity);
        }
        
        // Hiện khối ra để người chơi biết mình đập vào gì
        if (revealOnHit)
        {
            RevealBlock();
        }
        
        // Đẩy người chơi xuống dưới (knockback)
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        Vector2 knockbackDirection = Vector2.down * knockbackForce;
        
        if (playerRb != null)
        {
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, -knockbackForce);
        }
        
        // Gây sát thương nếu cần
        if (dealDamage)
        {
            PlayerHealth2D playerHealth = player.GetComponent<PlayerHealth2D>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount, knockbackDirection, playerRb);
            }
        }
        
        // Camera shake (nếu có)
        CameraShake cameraShake = Camera.main?.GetComponent<CameraShake>();
        if (cameraShake != null)
        {
            cameraShake.Shake(0.2f, 0.3f);
        }
    }
    
    private void RevealBlock()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            StartCoroutine(RevealAnimation());
        }
    }
    
    private System.Collections.IEnumerator RevealAnimation()
    {
        float elapsed = 0f;
        Color startColor = new Color(revealColor.r, revealColor.g, revealColor.b, 0f);
        Color endColor = revealColor;
        
        // Fade in
        while (elapsed < revealDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / revealDuration;
            spriteRenderer.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        
        spriteRenderer.color = endColor;
        
        // Đợi một chút rồi fade out nếu cần
        if (fadeAfterReveal)
        {
            yield return new WaitForSeconds(fadeDelay);
            
            elapsed = 0f;
            while (elapsed < revealDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / revealDuration;
                spriteRenderer.color = Color.Lerp(endColor, startColor, t);
                yield return null;
            }
            
            spriteRenderer.enabled = false;
            hasBeenHit = false; // Reset để có thể troll lại
        }
    }
    
    // Hiển thị trong editor để dễ đặt vị trí
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        if (GetComponent<Collider2D>() != null)
        {
            Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>().bounds.size);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
        
        // Vẽ text "BONK!" trong Scene view
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "INVISIBLE BLOCK\n(BONK!)");
        #endif
    }
}
