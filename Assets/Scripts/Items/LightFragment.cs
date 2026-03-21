using UnityEngine;
using UnityEngine.Rendering.Universal;
using NWO;

/// <summary>
/// Máº£nh SÃ¡ng - Item ngÆ°á»i chÆ¡i cáº§n thu tháº­p Ä‘á»ƒ má»Ÿ rá»™ng táº§m nhÃ¬n
/// Sá»­ dá»¥ng URP Light2D cho hiá»‡u á»©ng Ã¡nh sÃ¡ng 2D thá»±c sá»±
/// </summary>
public class LightFragment : MonoBehaviour
{
    [Header("Fragment Info")]
    [SerializeField] private int fragmentID = 1;
    [SerializeField] private string fragmentName = "Light Fragment";
    
    [Header("Visual Effects")]
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private ParticleSystem collectEffect;
    
    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;
    
    [Header("Light Settings (URP 2D)")]
    [SerializeField] private Light2D itemLight;
    [SerializeField] private float lightPulseSpeed = 2f;
    [SerializeField] private float lightMinIntensity = 0.5f;
    [SerializeField] private float lightMaxIntensity = 1.2f;
    [SerializeField] private float lightRadius = 3f;
    [SerializeField] private Color lightColor = new Color(1f, 0.95f, 0.6f); // VÃ ng áº¥m
    
    private Vector3 startPosition;
    private bool isCollected = false;
    
    private void Awake()
    {
        // Auto-add CircleCollider2D náº¿u chÆ°a cÃ³
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = 0.8f;
            circle.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }
        
        // Auto-add Light2D náº¿u chÆ°a cÃ³
        if (itemLight == null)
        {
            itemLight = GetComponentInChildren<Light2D>();
            if (itemLight == null)
            {
                var lightObj = new GameObject("FragmentLight");
                lightObj.transform.SetParent(transform);
                lightObj.transform.localPosition = Vector3.zero;
                itemLight = lightObj.AddComponent<Light2D>();
                itemLight.lightType = Light2D.LightType.Point;
                itemLight.pointLightOuterRadius = lightRadius;
                itemLight.pointLightInnerRadius = lightRadius * 0.3f;
                itemLight.intensity = lightMaxIntensity;
                itemLight.color = lightColor;
            }
        }
        
        // Auto-add SpriteRenderer náº¿u chÆ°a cÃ³
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.color = lightColor;
            sr.sortingOrder = 5;
        }
    }
    
    private void Start()
    {
        startPosition = transform.position;
    }
    
    private void Update()
    {
        if (isCollected) return;
        
        // Xoay liÃªn tá»¥c
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        
        // Bay lÃªn xuá»‘ng
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        
        // Nháº¥p nhÃ¡y Ã¡nh sÃ¡ng (URP Light2D)
        if (itemLight != null)
        {
            float intensity = Mathf.Lerp(lightMinIntensity, lightMaxIntensity, 
                                        (Mathf.Sin(Time.time * lightPulseSpeed) + 1f) / 2f);
            itemLight.intensity = intensity;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;
        
        if (collision.CompareTag("Player"))
        {
            CollectFragment(collision.gameObject);
        }
    }
    
    private void CollectFragment(GameObject player)
    {
        isCollected = true;
        bool collectedViaGameManager = false;
        
        // ThÃ´ng bÃ¡o cho GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectLightFragment(fragmentID);
            collectedViaGameManager = true;
        }

        // Đảm bảo ánh sáng luôn tăng ngay cả khi event hookup bị trễ.
        if (DungeonLightingManager.Instance != null)
        {
            if (collectedViaGameManager && GameManager.Instance != null)
                DungeonLightingManager.Instance.ApplyFragmentProgress(GameManager.Instance.LightFragmentsCollected);
            else
                DungeonLightingManager.Instance.NotifyFragmentCollectedFallback();
        }
        
        // Hiá»‡u á»©ng particle
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }
        
        // Ã‚m thanh
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        // Hiá»‡u á»©ng UI (optional)
        ShowCollectNotification();
        
        // Má»Ÿ rá»™ng táº§m nhÃ¬n cá»§a player
        ExpandPlayerVision(player);
        
        // Destroy vá»›i animation
        StartCoroutine(CollectAnimation());
    }
    
    private void ExpandPlayerVision(GameObject player)
    {
        // DungeonLightingManager xá»­ lÃ½ viá»‡c má»Ÿ rá»™ng Ã¡nh sÃ¡ng (vá»›i animation)
        // NÃ³ Ä‘Ã£ Ä‘Äƒng kÃ½ event OnLightFragmentCollected tá»« GameManager
        // NÃªn chá»‰ cáº§n log á»Ÿ Ä‘Ã¢y
        if (NWO.DungeonLightingManager.Instance == null)
        {
            // Fallback: tá»± tÄƒng náº¿u khÃ´ng cÃ³ manager
            var playerLight = player.GetComponentInChildren<Light2D>();
            if (playerLight != null)
            {
                playerLight.pointLightOuterRadius += 1f;

            }
        }
    }
    
    private void ShowCollectNotification()
    {
        // TODO: Hiá»ƒn thá»‹ UI thÃ´ng bÃ¡o "Light Fragment Collected!"
        Debug.Log($"[LightFragment] Collected: {fragmentName}");
    }
    
    private System.Collections.IEnumerator CollectAnimation()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        Vector3 startScale = transform.localScale;
        Vector3 targetPos = transform.position + Vector3.up * 2f;
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Scale lÃªn vÃ  fade out
            transform.localScale = Vector3.Lerp(startScale, startScale * 1.5f, t);
            transform.position = Vector3.Lerp(transform.position, targetPos, t);
            
            if (sprite != null)
            {
                Color color = sprite.color;
                color.a = 1f - t;
                sprite.color = color;
            }
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, 
                                 $"Light Fragment #{fragmentID}");
        #endif
    }
}
