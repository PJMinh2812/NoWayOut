using UnityEngine;
using ProceduralGeneration.Core;
using ProceduralGeneration.Data;
using Core;

namespace ProceduralGeneration.Components
{
    /// <summary>
    /// Door controller để teleport giữa các phòng.
    /// Tự động mở/đóng khi player lại gần, hỗ trợ lock/unlock cho boss fights.
    /// Merged từ Door.cs và DoorController.cs
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class DoorTrigger : MonoBehaviour
    {
        [Header("Door Configuration")]
        [Tooltip("Phòng hiện tại (phòng chứa door này)")]
        public Room currentRoom;
        
        [Tooltip("Phòng đích (phòng mà door này dẫn đến)")]
        public Room targetRoom;
        
        [Tooltip("Hướng của door (Left/Right/Top/Bottom)")]
        public DoorDirection doorDirection;
        
        [Header("Auto Open/Close")]
        [Tooltip("Tự động mở/đóng khi player lại gần")]
        [SerializeField] private bool autoOpenClose = true;
        
        [Tooltip("Khoảng cách phát hiện player để auto open")]
        [SerializeField] private float detectionRange = 2f;
        
        [Header("State")]
        [Tooltip("Door hiện tại có mở không?")]
        [SerializeField] private bool isOpen = false;
        
        [Tooltip("Door có bị khóa không? (dùng cho boss fights)")]
        [SerializeField] private bool isLocked = false;
        
        [Header("Visual")]
        [Tooltip("GameObject hiển thị khi cửa đóng")]
        [SerializeField] private GameObject closedVisual;
        
        [Tooltip("GameObject hiển thị khi cửa mở")]
        [SerializeField] private GameObject openVisual;
        
        [Tooltip("Màu khi door mở")]
        [SerializeField] private Color unlockedColor = Color.green;
        
        [Tooltip("Màu khi door khóa")]
        [SerializeField] private Color lockedColor = Color.red;
        
        [Header("Animation & Audio")]
        [Tooltip("Animator cho animation mở/đóng cửa")]
        [SerializeField] private Animator doorAnimator;
        
        [Tooltip("Sound khi mở cửa")]
        [SerializeField] private AudioClip openSound;
        
        [Tooltip("Sound khi đóng cửa")]
        [SerializeField] private AudioClip closeSound;
        
        [Tooltip("Sound khi cửa bị khóa")]
        [SerializeField] private AudioClip lockedSound;
        
        private BoxCollider2D doorCollider;
        private SpriteRenderer spriteRenderer;
        private AudioSource audioSource;
        private Transform player;
        
        void Awake()
        {
            // Setup collider
            doorCollider = GetComponent<BoxCollider2D>();
            
            // Get sprite renderer để đổi màu
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // Setup audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            
            UpdateVisualFeedback();
        }
        
        void Start()
        {
            // Tự động tìm player nếu chưa gán
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            
            // Set collider state dựa vào isOpen
            UpdateColliderState();
        }
        
        void Update()
        {
            // Auto open/close dựa vào khoảng cách
            if (autoOpenClose && !isLocked && player != null)
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
        
        void OnTriggerEnter2D(Collider2D other)
        {
            // Check nếu là player
            if (other.CompareTag("Player"))
            {
                // Chỉ trigger transition nếu door MỞ và KHÔNG KHÓA
                if (isOpen && !isLocked)
                {
                    TriggerRoomTransition(other.gameObject);
                }
                else if (isLocked)
                {
                    PlaySound(lockedSound);
                    Debug.Log($"[DoorTrigger] Door is LOCKED! Clear room first.");
                }
            }
        }
        
        void OnTriggerExit2D(Collider2D other)
        {
            // Empty - có thể dùng để track player exit sau này
        }
        
        void OnCollisionEnter2D(Collision2D collision)
        {
            // Debug: Player đâm vào door khi đóng
            if (collision.gameObject.CompareTag("Player"))
            {
                Debug.Log($"[DoorTrigger] Player collided with closed door! Locked: {isLocked}");
            }
        }
        
        /// <summary>
        /// Trigger room transition khi player đi qua door
        /// </summary>
        private void TriggerRoomTransition(GameObject player)
        {
            if (targetRoom == null)
            {
                Debug.LogError($"[DoorTrigger] Target room is NULL! Cannot transition.");
                return;
            }
            
            if (targetRoom.roomInstance == null)
            {
                Debug.LogError($"[DoorTrigger] Target room instance not instantiated!");
                return;
            }
            
            Debug.Log($"[DoorTrigger] Transitioning from {currentRoom.roomData.roomType} to {targetRoom.roomData.roomType}");
            
            // Tìm RoomTransitionManager và trigger transition
            var transitionManager = FindFirstObjectByType<RoomTransitionManager>();
            if (transitionManager != null)
            {
                transitionManager.TransitionToRoom(currentRoom, targetRoom, doorDirection, player);
            }
            else
            {
                Debug.LogError("[DoorTrigger] RoomTransitionManager not found in scene!");
            }
        }
        
        #region Open/Close Methods
        
        /// <summary>
        /// Mở cửa
        /// </summary>
        public void OpenDoor()
        {
            if (isLocked)
            {
                PlaySound(lockedSound);
                Debug.Log("[DoorTrigger] Cannot open - Door is locked!");
                return;
            }
            
            isOpen = true;
            UpdateVisualFeedback();
            UpdateColliderState();
            PlayAnimation("Open");
            PlaySound(openSound);
        }
        
        /// <summary>
        /// Đóng cửa
        /// </summary>
        public void CloseDoor()
        {
            isOpen = false;
            UpdateVisualFeedback();
            UpdateColliderState();
            PlayAnimation("Close");
            PlaySound(closeSound);
        }
        
        /// <summary>
        /// Toggle cửa
        /// </summary>
        public void ToggleDoor()
        {
            if (isOpen)
                CloseDoor();
            else
                OpenDoor();
        }
        
        #endregion
        
        #region Lock/Unlock Methods
        
        /// <summary>
        /// Khóa door (dùng cho boss fights)
        /// </summary>
        public void LockDoor()
        {
            isLocked = true;
            CloseDoor(); // Đóng cửa khi khóa
            UpdateVisualFeedback();
            Debug.Log($"[DoorTrigger] Door LOCKED");
        }
        
        /// <summary>
        /// Mở khóa door (sau khi clear room)
        /// </summary>
        public void UnlockDoor()
        {
            isLocked = false;
            UpdateVisualFeedback();
            Debug.Log($"[DoorTrigger] Door UNLOCKED");
        }
        
        /// <summary>
        /// Check door có bị khóa không
        /// </summary>
        public bool IsLocked()
        {
            return isLocked;
        }
        
        #endregion
        
        /// <summary>
        /// Cập nhật visual feedback (đổi màu door + visuals)
        /// </summary>
        private void UpdateVisualFeedback()
        {
            // Update sprite color
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isLocked ? lockedColor : unlockedColor;
            }
            
            // Update visual GameObjects
            if (closedVisual != null)
                closedVisual.SetActive(!isOpen);
            
            if (openVisual != null)
                openVisual.SetActive(isOpen);
        }
        
        /// <summary>
        /// Cập nhật collider state (trigger khi mở, solid khi đóng)
        /// </summary>
        private void UpdateColliderState()
        {
            if (doorCollider != null)
            {
                if (isOpen)
                {
                    // Door mở: Enable trigger để detect player đi qua
                    doorCollider.enabled = true;
                    doorCollider.isTrigger = true;
                }
                else
                {
                    // Door đóng: Solid collider để chặn player
                    doorCollider.enabled = true;
                    doorCollider.isTrigger = false;
                }
            }
        }
        
        /// <summary>
        /// Play animation
        /// </summary>
        private void PlayAnimation(string triggerName)
        {
            if (doorAnimator != null)
            {
                doorAnimator.SetTrigger(triggerName);
            }
        }
        
        /// <summary>
        /// Play sound
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        #region Debug
        
        void OnDrawGizmos()
        {
            // Vẽ arrow chỉ hướng door
            Gizmos.color = isLocked ? Color.red : (isOpen ? Color.green : Color.yellow);
            
            Vector3 arrowDirection = Vector3.zero;
            switch (doorDirection)
            {
                case DoorDirection.Top:
                    arrowDirection = Vector3.up;
                    break;
                case DoorDirection.Bottom:
                    arrowDirection = Vector3.down;
                    break;
                case DoorDirection.Left:
                    arrowDirection = Vector3.left;
                    break;
                case DoorDirection.Right:
                    arrowDirection = Vector3.right;
                    break;
            }
            
            Gizmos.DrawRay(transform.position, arrowDirection * 2f);
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 2f, 0.1f));
        }
        
        void OnDrawGizmosSelected()
        {
            // Vẽ detection range
            if (autoOpenClose)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, detectionRange);
            }
        }
        
        #endregion
    }
}
