using UnityEngine;
using NWO;

/// <summary>
/// Mảnh Sáng - Item người chơi cần thu thập để mở rộng tầm nhìn
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
    
    [Header("Light Settings")]
    // Standard Light (không cần URP)
    [SerializeField] private Light itemLight;
    // URP Light2D - chỉ dùng nếu project có URP:
    // [SerializeField] private UnityEngine.Rendering.Universal.Light2D itemLight;
    [SerializeField] private float lightPulseSpeed = 2f;
    [SerializeField] private float lightMinIntensity = 0.5f;
    [SerializeField] private float lightMaxIntensity = 1.2f;
    
    private Vector3 startPosition;
    private bool isCollected = false;
    
    private void Start()
    {
        startPosition = transform.position;
    }
    
    private void Update()
    {
        if (isCollected) return;
        
        // Xoay liên tục
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        
        // Bay lên xuống
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        
        // Nhấp nháy ánh sáng
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
        
        // Thông báo cho GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectLightFragment(fragmentID);
        }
        
        // Hiệu ứng particle
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }
        
        // Âm thanh
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        // Hiệu ứng UI (optional)
        ShowCollectNotification();
        
        // Mở rộng tầm nhìn của player
        ExpandPlayerVision(player);
        
        // Destroy với animation
        StartCoroutine(CollectAnimation());
    }
    
    private void ExpandPlayerVision(GameObject player)
    {
        // Tìm Light component của player (Standard Light)
        var playerLight = player.GetComponentInChildren<Light>();
        if (playerLight != null)
        {
            // Tăng range của ánh sáng
            playerLight.range += 2f;
            playerLight.intensity += 0.2f;
        }
        
        // URP Light2D - chỉ dùng nếu project có URP:
        // var playerLight = player.GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>();
        // if (playerLight != null)
        // {
        //     playerLight.pointLightOuterRadius += 2f;
        //     playerLight.intensity += 0.2f;
        // }
    }
    
    private void ShowCollectNotification()
    {
        // TODO: Hiển thị UI thông báo "Light Fragment Collected!"
        Debug.Log($"{fragmentName} #{fragmentID} collected!");
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
            
            // Scale lên và fade out
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
