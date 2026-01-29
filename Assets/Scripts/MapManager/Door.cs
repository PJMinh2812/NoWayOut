using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private float detectionRange = 2f;
    
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private BoxCollider2D doorCollider;
    
    private Animator animator;
    private bool isOpen = false;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        doorCollider = GetComponent<BoxCollider2D>();
        
        // ĐẢM BẢO collider KHÔNG phải trigger (để chặn player)
        if (doorCollider != null)
        {
            // Khi đóng cửa, collider phải là solid (không phải trigger)
            doorCollider.isTrigger = false;
        }
        
        // Tự động tìm player nếu chưa gán
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }
    
    void Update()
    {
        if (isLocked) return;
        
        // Kiểm tra khoảng cách đến player
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            
            if (distance <= detectionRange && !isOpen)
            {
                OpenDoor();
            }
            else if (distance > detectionRange && isOpen)
            {
                CloseDoor();
            }
        }
    }
    
    public void OpenDoor()
    {
        if (isLocked) return;
        
        isOpen = true;
        
        if (animator != null)
            animator.SetBool("isOpen", true);
        
        // Tắt collider để player đi qua được
        if (doorCollider != null)
            doorCollider.enabled = false;
    }
    
    public void CloseDoor()
    {
        isOpen = false;
        
        if (animator != null)
            animator.SetBool("isOpen", false);
        
        // Bật lại collider và đảm bảo không phải trigger
        if (doorCollider != null)
        {
            doorCollider.enabled = true;
            doorCollider.isTrigger = false;
        }
    }
    
    public void LockDoor()
    {
        isLocked = true;
        
        // Đóng cửa và bật collider ngay lập tức
        isOpen = false;
        
        if (animator != null)
            animator.SetBool("isOpen", false);
        
        // BẮT BUỘC bật collider và set solid (không phải trigger)
        if (doorCollider != null)
        {
            doorCollider.enabled = true;
            doorCollider.isTrigger = false; // Đảm bảo chặn vật lý
        }
        
        Debug.Log($"[Door] Locked! Collider enabled: {doorCollider?.enabled}, IsTrigger: {doorCollider?.isTrigger}");
    }
    
    public void UnlockDoor()
    {
        isLocked = false;
        Debug.Log("[Door] Unlocked!");
    }
    
    // Vẽ detection range trong Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
    
    // DEBUG: Kiểm tra va chạm
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"[Door] Player đâm vào cửa! Locked: {isLocked}, Collider enabled: {doorCollider?.enabled}");
        }
    }
}
