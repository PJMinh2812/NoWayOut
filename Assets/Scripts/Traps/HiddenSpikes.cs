using UnityEngine;
using NWO;

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
    [SerializeField] private float retractTime = 0.08f;

    [Tooltip("Thời gian chông giữ ở trạng thái mọc lên")]
    [SerializeField] private float activeDuration = 1.5f;
    
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

    [Header("Animator Mode (Door-style)")]
    [Tooltip("Animator điều khiển chông lên/xuống bằng clip")]
    [SerializeField] private Animator spikeAnimator;

    [Tooltip("Bool parameter trong Animator (giống Door: isOpen)")]
    [SerializeField] private string activeBoolParameter = "isOpen";

    [Tooltip("Bật để dùng Animator điều khiển movement thay cho coroutine")]
    [SerializeField] private bool useAnimatorMovement = true;

    [Tooltip("Chông chỉ gây damage khi đã mọc xong")]
    [SerializeField] private bool damageOnlyWhenFullyRaised = true;
    
    private Vector3 hiddenPosition;
    private Vector3 activePosition;
    private bool isActive = false;
    private bool isTriggered = false;
    private SpriteRenderer spikeSpriteRenderer;
    private int _activeBoolHash;
    private readonly Collider2D[] _overlapResults = new Collider2D[4];
    
    private void Awake()
    {
        AutoAssignReferences();
        EnsureTriggerZone();
        AttachRelayIfNeeded();
        
        if (spikesTransform != null)
        {
            activePosition = spikesTransform.localPosition;
            hiddenPosition = activePosition - new Vector3(0, 1f, 0);
            if (!UseAnimatorMode())
            {
                spikesTransform.localPosition = hiddenPosition;
            }
            
            spikeSpriteRenderer = spikesTransform.GetComponent<SpriteRenderer>();
        }

        if (spikeAnimator == null)
        {
            spikeAnimator = GetComponent<Animator>();
        }

        if (!string.IsNullOrEmpty(activeBoolParameter))
        {
            _activeBoolHash = Animator.StringToHash(activeBoolParameter);
        }

        if (UseAnimatorMode())
        {
            SetAnimatorActive(false);
        }
        else if (useAnimatorMovement)
        {
            Debug.LogWarning("[HiddenSpikes] Animator Mode enabled but Animator/Controller is missing. Falling back to transform animation.");
        }

        if (spikesTransform == null)
        {
            Debug.LogWarning("[HiddenSpikes] spikesTransform is missing. Trap cannot animate visually.");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandlePotentialTrigger(collision);
    }

    private void Update()
    {
        CheckPlayerOverlapFallback();
    }

    public void HandlePotentialTrigger(Collider2D collision)
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
        
        if (warningEffect != null)
        {
            GameObject warning = Instantiate(warningEffect, transform.position, Quaternion.identity);
            Destroy(warning, triggerDelay);
        }
        
        Invoke(nameof(RaiseSpikes), triggerDelay);
    }
    
    private void RaiseSpikes()
    {
        if (spikeRiseSound != null)
        {
            AudioSource.PlayClipAtPoint(spikeRiseSound, transform.position);
        }

        if (UseAnimatorMode())
        {
            SetAnimatorActive(true);

            if (damageOnlyWhenFullyRaised)
            {
                Invoke(nameof(EnableDamageWindow), riseTime);
            }
            else
            {
                EnableDamageWindow();
            }

            Invoke(nameof(RetractSpikes), riseTime + activeDuration);
        }
        else if (spikesTransform != null)
        {
            isActive = true;
            StartCoroutine(SpikeRiseAnimation());
        }
        
        CameraShake cameraShake = Camera.main?.GetComponent<CameraShake>();
        if (cameraShake != null)
        {
            cameraShake.Shake(0.2f, 0.4f);
        }
    }

    private void EnableDamageWindow()
    {
        isActive = true;
    }

    private void RetractSpikes()
    {
        isActive = false;

        if (UseAnimatorMode())
        {
            SetAnimatorActive(false);
            Invoke(nameof(ResetTriggerState), retractTime + resetDelay);
        }
    }

    private void ResetTriggerState()
    {
        isTriggered = false;
    }
    
    private System.Collections.IEnumerator SpikeRiseAnimation()
    {
        float elapsed = 0f;
        
        while (elapsed < riseTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / riseTime;
            
            // Ease out curve
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
        ResetTriggerState();
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
        // Nếu player đang Dash → bất tử, bỏ qua damage
        var playerCtrl = player.GetComponent<NWO.PlayerController2D>();
        if (playerCtrl != null && playerCtrl.IsDashing) return;

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

    private bool UseAnimatorMode()
    {
        return useAnimatorMovement && spikeAnimator != null && spikeAnimator.runtimeAnimatorController != null;
    }

    private void SetAnimatorActive(bool active)
    {
        if (spikeAnimator == null || _activeBoolHash == 0)
        {
            return;
        }
        spikeAnimator.SetBool(_activeBoolHash, active);
    }

    private void EnsureTriggerZone()
    {
        if (triggerZone == null)
        {
            triggerZone = GetComponent<Collider2D>();
        }

        if (triggerZone == null)
        {
            triggerZone = GetComponentInChildren<Collider2D>(true);
        }

        if (triggerZone == null)
        {
            bool has3DCollider = GetComponent<Collider>() != null;
            if (!has3DCollider)
            {
                var autoZone = gameObject.AddComponent<BoxCollider2D>();
                if (autoZone != null)
                {
                    autoZone.isTrigger = true;
                    autoZone.size = new Vector2(1f, 1f);
                    triggerZone = autoZone;
                    Debug.LogWarning("[HiddenSpikes] Missing Collider2D. Auto-created BoxCollider2D trigger.");
                }
            }

            if (triggerZone == null)
            {
                // Fallback when this GameObject has 3D colliders that conflict with 2D collider creation.
                var child = new GameObject("SpikeTrigger2D");
                child.transform.SetParent(transform, false);
                child.transform.localPosition = Vector3.zero;

                var childZone = child.AddComponent<BoxCollider2D>();
                childZone.isTrigger = true;
                childZone.size = new Vector2(1f, 1f);
                triggerZone = childZone;
                Debug.LogWarning("[HiddenSpikes] Created child SpikeTrigger2D because root collider setup conflicts with 2D trigger.");
            }
        }
        else
        {
            triggerZone.isTrigger = true;
        }

        EnsureTriggerRigidbody2D();
    }

    private void AttachRelayIfNeeded()
    {
        if (triggerZone == null) return;
        if (triggerZone.gameObject == gameObject) return;

        var relay = triggerZone.GetComponent<HiddenSpikesTriggerRelay>();
        if (relay == null)
        {
            relay = triggerZone.gameObject.AddComponent<HiddenSpikesTriggerRelay>();
        }

        relay.SetOwner(this);
    }

    private void EnsureTriggerRigidbody2D()
    {
        if (triggerZone == null) return;
        if (triggerZone.attachedRigidbody != null) return;

        var rb2d = triggerZone.gameObject.GetComponent<Rigidbody2D>();
        if (rb2d == null)
        {
            rb2d = triggerZone.gameObject.AddComponent<Rigidbody2D>();
        }

        rb2d.bodyType = RigidbodyType2D.Kinematic;
        rb2d.gravityScale = 0f;
        rb2d.simulated = true;
    }

    private void CheckPlayerOverlapFallback()
    {
        if (triggerZone == null || isTriggered || isActive) return;

        int hitCount = Physics2D.OverlapCollider(triggerZone, new ContactFilter2D().NoFilter(), _overlapResults);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = _overlapResults[i];
            if (col != null && col.CompareTag("Player"))
            {
                TriggerSpikes();
                return;
            }
        }
    }

    private void OnDisable()
    {
        CancelInvoke();
        isActive = false;
        isTriggered = false;

        if (UseAnimatorMode())
        {
            SetAnimatorActive(false);
        }
    }

    private void OnValidate()
    {
        AutoAssignReferences();

        if (spikeAnimator == null)
        {
            spikeAnimator = GetComponent<Animator>();
        }

        if (string.IsNullOrWhiteSpace(activeBoolParameter))
        {
            activeBoolParameter = "isOpen";
        }
    }

    private void AutoAssignReferences()
    {
        if (spikesTransform == null)
        {
            Transform child = transform.Find("SpikesVisual");
            if (child != null)
            {
                spikesTransform = child;
            }
            else if (transform.childCount > 0)
            {
                spikesTransform = transform.GetChild(0);
            }
            else
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    spikesTransform = transform;
                }
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
