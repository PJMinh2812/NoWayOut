using UnityEngine;
using NWO;

/// <summary>
/// Lò xo troll - Bật người chơi bay ngược về phòng trước
/// Sử dụng cho: Khu Vực 2 - trước Mảnh Sáng #3
/// </summary>
public class SpringTrap : MonoBehaviour
{
    [Header("Spring Settings")]
    [Tooltip("Lực bật (càng cao bay càng xa)")]
    [SerializeField] private float springForce = 20f;
    
    [Tooltip("Hướng bật (thường là Vector2.left để bật ngược lại)")]
    [SerializeField] private Vector2 pushDirection = Vector2.left;
    
    [Tooltip("Có hiện lò xo ra không?")]
    [SerializeField] private bool revealSpring = true;
    
    [Tooltip("Thời gian animation lò xo nảy")]
    [SerializeField] private float springAnimationTime = 0.3f;
    
    [Header("Cooldown")]
    [Tooltip("Thời gian hồi chiêu")]
    [SerializeField] private float cooldown = 2f;
    
    [Header("Audio & Visual")]
    [SerializeField] private AudioClip springSound;
    [SerializeField] private GameObject springEffect; // Hiệu ứng hơi/khói
    [SerializeField] private Sprite springCompressedSprite;
    [SerializeField] private Sprite springExtendedSprite;
    
    private SpriteRenderer spriteRenderer;
    private bool isOnCooldown = false;
    private Vector3 originalScale;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        
        // Ẩn sprite ban đầu nếu không muốn hiện
        if (spriteRenderer != null && !revealSpring)
        {
            spriteRenderer.enabled = false;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isOnCooldown) return;
        
        if (collision.CompareTag("Player"))
        {
            ActivateSpring(collision.gameObject);
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isOnCooldown) return;
        
        if (collision.gameObject.CompareTag("Player"))
        {
            ActivateSpring(collision.gameObject);
        }
    }
    
    private void ActivateSpring(GameObject player)
    {
        // Nếu player đang Dash → bất tử, không bị bật
        var playerCtrl = player.GetComponent<NWO.PlayerController2D>();
        if (playerCtrl != null && playerCtrl.IsDashing) return;

        isOnCooldown = true;
        
        // Phát âm thanh lò xo "BOING!"
        if (springSound != null)
        {
            AudioSource.PlayClipAtPoint(springSound, transform.position, 1f);
        }
        
        // Hiệu ứng visual
        if (springEffect != null)
        {
            Instantiate(springEffect, transform.position, Quaternion.identity);
        }
        
        // Hiện lò xo và animation
        if (spriteRenderer != null && !spriteRenderer.enabled)
        {
            spriteRenderer.enabled = true;
        }
        
        StartCoroutine(SpringAnimation());
        
        // Bật người chơi bay đi
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            // Normalize hướng và áp lực
            Vector2 force = pushDirection.normalized * springForce;
            playerRb.linearVelocity = force;
            
            // Tắt điều khiển tạm thời để người chơi không can thiệp được
            PlayerController2D controller = player.GetComponent<PlayerController2D>();
            if (controller != null)
            {
                StartCoroutine(DisableControlTemporarily(controller, 1f));
            }
        }
        
        // Camera shake
        CameraShake cameraShake = Camera.main?.GetComponent<CameraShake>();
        if (cameraShake != null)
        {
            cameraShake.Shake(0.3f, 0.5f);
        }
        
        // Reset cooldown
        Invoke(nameof(ResetCooldown), cooldown);
    }
    
    private System.Collections.IEnumerator SpringAnimation()
    {
        float elapsed = 0f;
        float halfTime = springAnimationTime / 2f;
        
        // Nén lò xo
        while (elapsed < halfTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfTime;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.5f, t);
            yield return null;
        }
        
        // Đổi sprite nếu có
        if (springCompressedSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = springCompressedSprite;
        }
        
        // Nảy lò xo
        elapsed = 0f;
        while (elapsed < halfTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfTime;
            transform.localScale = Vector3.Lerp(originalScale * 0.5f, originalScale * 1.2f, t);
            yield return null;
        }
        
        // Đổi sprite nếu có
        if (springExtendedSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = springExtendedSprite;
        }
        
        // Trở về kích thước ban đầu
        elapsed = 0f;
        while (elapsed < halfTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfTime;
            transform.localScale = Vector3.Lerp(originalScale * 1.2f, originalScale, t);
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    private System.Collections.IEnumerator DisableControlTemporarily(PlayerController2D controller, float duration)
    {
        controller.enabled = false;
        yield return new WaitForSeconds(duration);
        controller.enabled = true;
    }
    
    private void ResetCooldown()
    {
        isOnCooldown = false;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        
        // Vẽ vị trí lò xo
        if (GetComponent<Collider2D>() != null)
        {
            Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>().bounds.size);
        }
        
        // Vẽ mũi tên chỉ hướng bật
        Vector3 arrowEnd = transform.position + (Vector3)(pushDirection.normalized * 2f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, arrowEnd);
        
        // Vẽ đầu mũi tên
        Vector3 arrowLeft = arrowEnd + (Quaternion.Euler(0, 0, 135) * pushDirection.normalized * 0.5f);
        Vector3 arrowRight = arrowEnd + (Quaternion.Euler(0, 0, -135) * pushDirection.normalized * 0.5f);
        Gizmos.DrawLine(arrowEnd, arrowLeft);
        Gizmos.DrawLine(arrowEnd, arrowRight);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "SPRING TRAP\n(BOING!)");
        #endif
    }
    
    private void OnValidate()
    {
        // Normalize push direction trong editor
        if (pushDirection.magnitude > 0)
        {
            pushDirection = pushDirection.normalized;
        }
    }
}
