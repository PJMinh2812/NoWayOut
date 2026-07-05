using UnityEngine;

/// <summary>
/// Bức tường ảo ảnh - Chỉ hiện ra khi người chơi dùng Flash of Truth (Space)
/// </summary>
public class IllusionWall : MonoBehaviour
{
    [Header("Wall Settings")]
    [Tooltip("Có thể đi xuyên qua sau khi phát hiện không?")]
    [SerializeField] private bool canPassThrough = true;
    
    [Tooltip("Tự động biến mất sau khi phát hiện?")]
    [SerializeField] private bool autoDisappear = true;
    
    [Tooltip("Thời gian biến mất (giây)")]
    [SerializeField] private float disappearTime = 2f;
    
    [Header("Visual")]
    [SerializeField] private float normalAlpha = 0.8f;
    [SerializeField] private float revealedAlpha = 0.3f;
    [SerializeField] private float fadeSpeed = 2f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip revealSound;
    [SerializeField] private AudioClip disappearSound;
    
    private SpriteRenderer spriteRenderer;
    private Collider2D wallCollider;
    private bool isRevealed = false;
    private bool isDisappearing = false;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        wallCollider = GetComponent<Collider2D>();
        
        // Set alpha ban đầu
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = normalAlpha;
            spriteRenderer.color = color;
        }
    }
    
    private void Update()
    {
        // Kiểm tra xem player có bấm Space gần đây không
        if (!isRevealed && !isDisappearing)
        {
            CheckForFlashOfTruth();
        }
    }
    
    private void CheckForFlashOfTruth()
    {
        // Tìm player trong phạm vi
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 5f);
        
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                // Kiểm tra xem player có đang dùng Flash of Truth không
                // Giả sử có script PlayerLantern hoặc check Input trực tiếp
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    RevealWall();
                }
                break;
            }
        }
    }
    
    private void RevealWall()
    {
        if (isRevealed) return;
        
        isRevealed = true;
        
        // Phát âm thanh
        if (revealSound != null)
        {
            AudioSource.PlayClipAtPoint(revealSound, transform.position);
        }
        
        // Hiệu ứng visual
        StartCoroutine(RevealAnimation());
        
        // Cho phép đi xuyên qua
        if (canPassThrough && wallCollider != null)
        {
            wallCollider.isTrigger = true;
        }
        
        // Tự động biến mất
        if (autoDisappear)
        {
            Invoke(nameof(StartDisappear), disappearTime);
        }
    }
    
    private System.Collections.IEnumerator RevealAnimation()
    {
        if (spriteRenderer == null) yield break;
        
        Color startColor = spriteRenderer.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, revealedAlpha);
        
        float elapsed = 0f;
        float duration = 1f / fadeSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            spriteRenderer.color = Color.Lerp(startColor, targetColor, t);
            
            // Nhấp nháy
            float flicker = Mathf.Sin(elapsed * 20f) * 0.1f;
            spriteRenderer.color = new Color(
                targetColor.r + flicker,
                targetColor.g + flicker,
                targetColor.b + flicker,
                targetColor.a
            );
            
            yield return null;
        }
        
        spriteRenderer.color = targetColor;
    }
    
    private void StartDisappear()
    {
        isDisappearing = true;
        
        if (disappearSound != null)
        {
            AudioSource.PlayClipAtPoint(disappearSound, transform.position);
        }
        
        StartCoroutine(DisappearAnimation());
    }
    
    private System.Collections.IEnumerator DisappearAnimation()
    {
        if (spriteRenderer == null) yield break;
        
        Color startColor = spriteRenderer.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        float elapsed = 0f;
        float duration = 1.5f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            spriteRenderer.color = Color.Lerp(startColor, targetColor, t);
            
            // Particle effect (tùy chọn)
            if (elapsed > duration * 0.5f && Random.value > 0.8f)
            {
                // Spawn dust particles
            }
            
            yield return null;
        }
        
        // Tắt hẳn
        if (wallCollider != null)
        {
            wallCollider.enabled = false;
        }
        
        gameObject.SetActive(false);
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isRevealed) return;
        
        if (collision.CompareTag("Player") && canPassThrough)
        {
            // Player đi xuyên qua được
            Debug.Log("Player passed through illusion wall!");
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0f, 1f, 0.3f);
        if (GetComponent<Collider2D>() != null)
        {
            Gizmos.DrawCube(transform.position, GetComponent<Collider2D>().bounds.size);
        }
        
        // Vẽ phạm vi phát hiện Flash
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, 5f);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up, "ILLUSION WALL\n(Press Space)");
        #endif
    }
}
