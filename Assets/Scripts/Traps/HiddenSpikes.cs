using UnityEngine;
using GloomCraft;

/// <summary>
/// Chông ẩn - Mọc lên khi người chơi đứng vào vị trí kích hoạt
/// Sử dụng cho: Phòng Goal - trước Bức Tường Ảo Ảnh
/// </summary>
public class HiddenSpikes : MonoBehaviour
{
    [Header("Spike Settings")]
    [Tooltip("Thời gian delay trước khi chông mọc (giây)")]
    [SerializeField] private float triggerDelay = 0.2f;
    
    [Tooltip("Thời gian chông mọc lên (animation)")]
    [SerializeField] private float riseTime = 0.3f;
    
    [Tooltip("Thời gian chông thu xuống")]
    [SerializeField] private float retractTime = 1f;
    
    [Tooltip("Thời gian chờ trước khi reset")]
    [SerializeField] private float resetDelay = 3f;
    
    [Tooltip("Sát thương gây ra")]
    [SerializeField] private int damage = 2;
    
    [Tooltip("Có giết tức thì không? (insta-kill)")]
    [SerializeField] private bool instantKill = false;
    
    [Header("Trigger Zone")]
    [Tooltip("Collider kích hoạt (đặt tách riêng hoặc dùng chính GameObject này)")]
    [SerializeField] private Collider2D triggerZone;
    
    [Header("Visual")]
    [SerializeField] private Transform spikesTransform; // Transform của sprite chông
    [SerializeField] private AudioClip spikeRiseSound;
    [SerializeField] private AudioClip spikeHitSound;
    [SerializeField] private GameObject warningEffect; // Hiệu ứng cảnh báo (vòng tròn đỏ chớp chớp)
    
    private Vector3 hiddenPosition;
    private Vector3 activePosition;
    private bool isActive = false;
    private bool isTriggered = false;
    private SpriteRenderer spikeSpriteRenderer;
    
    private void Awake()
    {
        // Nếu không có trigger zone được gán, dùng collider của chính object này
        if (triggerZone == null)
        {
            triggerZone = GetComponent<Collider2D>();
            if (triggerZone != null)
            {
                triggerZone.isTrigger = true;
            }
        }
        
        // Setup vị trí ẩn và hiện của chông
        if (spikesTransform != null)
        {
            activePosition = spikesTransform.localPosition;
            hiddenPosition = activePosition - new Vector3(0, 1f, 0); // Ẩn xuống dưới 1 unit
            spikesTransform.localPosition = hiddenPosition;
            
            spikeSpriteRenderer = spikesTransform.GetComponent<SpriteRenderer>();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered || isActive) return;
        
        if (collision.CompareTag("Player"))
        {
            TriggerSpikes();
        }
    }
    
    private void TriggerSpikes()
    {
        isTriggered = true;
        
        // Hiệu ứng cảnh báo
        if (warningEffect != null)
        {
            GameObject warning = Instantiate(warningEffect, transform.position, Quaternion.identity);
            Destroy(warning, triggerDelay);
        }
        
        // Delay rồi mới mọc chông
        Invoke(nameof(RaiseSpikes), triggerDelay);
    }
    
    private void RaiseSpikes()
    {
        isActive = true;
        
        // Phát âm thanh chông mọc
        if (spikeRiseSound != null)
        {
            AudioSource.PlayClipAtPoint(spikeRiseSound, transform.position);
        }
        
        // Animation chông mọc lên
        if (spikesTransform != null)
        {
            StartCoroutine(SpikeRiseAnimation());
        }
        
        // Camera shake
        CameraShake cameraShake = Camera.main?.GetComponent<CameraShake>();
        if (cameraShake != null)
        {
            cameraShake.Shake(0.2f, 0.4f);
        }
    }
    
    private System.Collections.IEnumerator SpikeRiseAnimation()
    {
        float elapsed = 0f;
        
        while (elapsed < riseTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / riseTime;
            
            // Ease out curve để chông mọc nhanh dần
            t = 1f - Mathf.Pow(1f - t, 3f);
            
            spikesTransform.localPosition = Vector3.Lerp(hiddenPosition, activePosition, t);
            yield return null;
        }
        
        spikesTransform.localPosition = activePosition;
        
        // Giữ chông ở trên một chút rồi thu xuống
        yield return new WaitForSeconds(0.5f);
        
        StartCoroutine(SpikeRetractAnimation());
    }
    
    private System.Collections.IEnumerator SpikeRetractAnimation()
    {
        float elapsed = 0f;
        
        while (elapsed < retractTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / retractTime;
            spikesTransform.localPosition = Vector3.Lerp(activePosition, hiddenPosition, t);
            yield return null;
        }
        
        spikesTransform.localPosition = hiddenPosition;
        isActive = false;
        
        // Reset sau một khoảng thời gian
        yield return new WaitForSeconds(resetDelay);
        isTriggered = false;
    }
    
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isActive) return;
        
        if (collision.CompareTag("Player"))
        {
            DamagePlayer(collision.gameObject);
        }
    }
    
    private void DamagePlayer(GameObject player)
    {
        PlayerHealth2D playerHealth = player.GetComponent<PlayerHealth2D>();
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        
        if (playerHealth != null)
        {
            // Đẩy người chơi ra khỏi chông
            Vector2 knockbackDir = (player.transform.position - transform.position).normalized;
            Vector2 knockbackForce = knockbackDir * 5f;
            
            if (instantKill)
            {
                playerHealth.TakeDamage(999, knockbackForce, playerRb); // Giết luôn
            }
            else
            {
                playerHealth.TakeDamage(damage, knockbackForce, playerRb);
            }
            
            // Phát âm thanh trúng chông
            if (spikeHitSound != null)
            {
                AudioSource.PlayClipAtPoint(spikeHitSound, transform.position);
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        
        // Vẽ khu vực trigger
        if (triggerZone != null)
        {
            Gizmos.DrawCube(triggerZone.bounds.center, triggerZone.bounds.size);
        }
        else if (GetComponent<Collider2D>() != null)
        {
            Gizmos.DrawCube(transform.position, GetComponent<Collider2D>().bounds.size);
        }
        
        // Vẽ vị trí chông khi mọc lên
        Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
        Vector3 spikePos = transform.position + Vector3.up * 0.5f;
        Gizmos.DrawLine(transform.position, spikePos);
        Gizmos.DrawWireSphere(spikePos, 0.2f);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up, "HIDDEN SPIKES\n☠");
        #endif
    }
}
