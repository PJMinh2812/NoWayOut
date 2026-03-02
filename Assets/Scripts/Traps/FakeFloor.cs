using UnityEngine;
using NWO;

/// <summary>
/// Gáº¡ch rÆ¡i giáº£ - TrÃ´ng giá»‘ng sÃ n thÆ°á»ng nhÆ°ng sáº½ biáº¿n máº¥t khi ngÆ°á»i chÆ¡i bÆ°á»›c vÃ o
/// Sá»­ dá»¥ng cho: PhÃ²ng Start - cÃ¡i báº«y Ä‘áº§u tiÃªn ngay sau táº¥m biá»ƒn cáº£nh bÃ¡o
/// </summary>
public class FakeFloor : MonoBehaviour
{
    [Header("Fake Floor Settings")]
    [Tooltip("Thá»i gian delay trÆ°á»›c khi gáº¡ch rÆ¡i (giÃ¢y)")]
    [SerializeField] private float fallDelay = 0.1f;
    
    [Tooltip("Thá»i gian gáº¡ch tá»± há»“i phá»¥c (0 = khÃ´ng há»“i phá»¥c)")]
    [SerializeField] private float respawnTime = 5f;
    
    [Tooltip("CÃ³ phÃ¡t hiá»‡u á»©ng cáº£nh bÃ¡o khÃ´ng?")]
    [SerializeField] private bool showWarning = true;
    
    [Tooltip("MÃ u nháº¥p nhÃ¡y cáº£nh bÃ¡o")]
    [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0.8f, 1f);
    
    [Header("Audio")]
    [SerializeField] private AudioClip crackSound;
    [SerializeField] private AudioClip fallSound;
    
    [Header("Damage Settings")]
    [Tooltip("GÃ¢y sÃ¡t thÆ°Æ¡ng cho player khi rÆ¡i? (0 = khÃ´ng gÃ¢y damage)")]
    [SerializeField] private int damage = 0;
    
    [Tooltip("Giáº¿t player ngay láº­p tá»©c?")]
    [SerializeField] private bool instantKill = false;
    
    [Tooltip("Respawn point khi player cháº¿t (náº¿u instant kill)")]
    [SerializeField] private Transform respawnPoint;
    
    private SpriteRenderer spriteRenderer;
    private Collider2D floorCollider;
    private Color originalColor;
    private bool isTriggered = false;
    private bool isFalling = false;
    private GameObject fallingPlayer; // LÆ°u player Ä‘ang rÆ¡i
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        floorCollider = GetComponent<Collider2D>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            
            // LÃ m mÃ u nháº¡t hÆ¡n má»™t chÃºt Ä‘á»ƒ ngÆ°á»i chÆ¡i cáº©n tháº­n cÃ³ thá»ƒ nháº­n ra
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
        
        // Chá»‰ kÃ­ch hoáº¡t khi player bÆ°á»›c vÃ o
        if (collision.CompareTag("Player"))
        {
            // Nếu player đang Dash → bất tử, không kích hoạt gạch rơi
            var playerCtrl = collision.GetComponent<NWO.PlayerController2D>();
            if (playerCtrl != null && playerCtrl.IsDashing) return;

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

        isTriggered = true;
        
        // PhÃ¡t Ã¢m thanh ná»©t
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
        
        // PhÃ¡t Ã¢m thanh rÆ¡i
        if (fallSound != null)
        {
            AudioSource.PlayClipAtPoint(fallSound, transform.position);
        }
        
        // Táº¯t collider Ä‘á»ƒ ngÆ°á»i chÆ¡i rÆ¡i xuyÃªn qua
        if (floorCollider != null)
        {

            floorCollider.enabled = false;
        }
        
        // LÃ m sprite biáº¿n máº¥t dáº§n
        if (spriteRenderer != null)
        {

            StartCoroutine(FadeOut());
        }
        
        // GÃ¢y damage hoáº·c kill player
        DamagePlayer();
        
        // Há»“i phá»¥c gáº¡ch sau má»™t thá»i gian
        if (respawnTime > 0)
        {

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
            Vector2 knockbackForce = Vector2.down * 5f; // Äáº©y xuá»‘ng
            
            if (instantKill)
            {

                playerHealth.TakeDamage(999, knockbackForce, playerRb);
                
                // Respawn player sau 1.5s
                if (respawnPoint != null)
                {

                    Invoke(nameof(RespawnPlayer), 1.5f);
                }
            }
            else if (damage > 0)
            {

                playerHealth.TakeDamage(damage, knockbackForce, playerRb);
            }
        }
    }
    
    private void RespawnPlayer()
    {
        if (fallingPlayer != null && respawnPoint != null)
        {

            fallingPlayer.transform.position = respawnPoint.position;
            
            PlayerHealth2D playerHealth = fallingPlayer.GetComponent<PlayerHealth2D>();
            if (playerHealth != null)
            {
                playerHealth.ResetHealth(); // Reset vá» full HP
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
    
    // Äá»ƒ debug trong editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0.5f, 0.5f);
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}
