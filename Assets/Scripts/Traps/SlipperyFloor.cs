using UnityEngine;
using GloomCraft;

/// <summary>
/// Sàn trơn - Người chơi bị trượt không kiểm soát được
/// Sử dụng cho: Phòng Mini-Boss - xung quanh con chuột ngủ
/// </summary>
public class SlipperyFloor : MonoBehaviour
{
    [Header("Slippery Settings")]
    [Tooltip("Lực đẩy khi người chơi bước vào")]
    [SerializeField] private float slideForce = 5f;
    
    [Tooltip("Thời gian trượt (giây)")]
    [SerializeField] private float slideDuration = 1.5f;
    
    [Tooltip("Có thể hãm phanh bằng thùng/tường không?")]
    [SerializeField] private bool canBreakSlide = true;
    
    [Header("Visual")]
    [SerializeField] private Color iceColor = new Color(0.7f, 0.9f, 1f, 0.8f);
    [SerializeField] private bool showIceEffect = true;
    [SerializeField] private AudioClip slideSound;
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null && showIceEffect)
        {
            originalColor = spriteRenderer.color;
            // Tô màu xanh nhạt để người chơi biết đây là sàn băng
            spriteRenderer.color = Color.Lerp(originalColor, iceColor, 0.5f);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StartSliding(collision.gameObject);
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            StartSliding(collision.gameObject);
        }
    }
    
    private void StartSliding(GameObject player)
    {
        PlayerSlideController slideController = player.GetComponent<PlayerSlideController>();
        
        if (slideController == null)
        {
            // Thêm component tạm thời nếu chưa có
            slideController = player.AddComponent<PlayerSlideController>();
        }
        
        // Xác định hướng trượt dựa vào hướng di chuyển hiện tại
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        Vector2 slideDirection = Vector2.zero;
        
        if (rb != null)
        {
            // Lấy hướng velocity hiện tại
            slideDirection = rb.linearVelocity.normalized;
            
            // Nếu đứng yên thì trượt theo hướng đang quay mặt
            if (slideDirection.magnitude < 0.1f)
            {
                PlayerController2D controller = player.GetComponent<PlayerController2D>();
                if (controller != null)
                {
                    // Giả sử có thuộc tính facingRight
                    slideDirection = Vector2.right; // Mặc định
                }
                else
                {
                    slideDirection = Vector2.right;
                }
            }
        }
        
        // Phát âm thanh trượt
        if (slideSound != null)
        {
            AudioSource.PlayClipAtPoint(slideSound, transform.position);
        }
        
        // Bắt đầu trượt
        slideController.StartSlide(slideDirection, slideForce, slideDuration, canBreakSlide);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.4f);
        if (GetComponent<Collider2D>() != null)
        {
            Gizmos.DrawCube(transform.position, GetComponent<Collider2D>().bounds.size);
        }
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position, "SLIPPERY\n(ICE)");
        #endif
    }
}

/// <summary>
/// Component tạm thời để xử lý trượt của người chơi
/// Tự động tách riêng nếu cần logic phức tạp hơn
/// </summary>
public class PlayerSlideController : MonoBehaviour
{
    private bool isSliding = false;
    private Vector2 slideDirection;
    private float slideForce;
    private float slideTimeRemaining;
    private bool canBreak;
    
    private Rigidbody2D rb;
    private PlayerController2D playerController;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController2D>();
    }
    
    public void StartSlide(Vector2 direction, float force, float duration, bool canBreakSlide)
    {
        if (isSliding) return; // Đang trượt rồi thì không trượt thêm
        
        isSliding = true;
        slideDirection = direction.normalized;
        slideForce = force;
        slideTimeRemaining = duration;
        canBreak = canBreakSlide;
        
        // Vô hiệu hóa điều khiển của player
        if (playerController != null)
        {
            playerController.enabled = false;
        }
    }
    
    private void FixedUpdate()
    {
        if (!isSliding) return;
        
        slideTimeRemaining -= Time.fixedDeltaTime;
        
        if (slideTimeRemaining <= 0f)
        {
            StopSlide();
            return;
        }
        
        // Áp lực trượt
        if (rb != null)
        {
            rb.linearVelocity = slideDirection * slideForce;
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isSliding) return;
        
        // Nếu đụng vào thùng hoặc tường thì dừng trượt
        if (canBreak && (collision.gameObject.CompareTag("Obstacle") || 
                         collision.gameObject.CompareTag("Wall") ||
                         collision.gameObject.layer == LayerMask.NameToLayer("Environment")))
        {
            StopSlide();
        }
    }
    
    private void StopSlide()
    {
        isSliding = false;
        slideTimeRemaining = 0f;
        
        // Bật lại điều khiển
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // Dừng velocity
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
