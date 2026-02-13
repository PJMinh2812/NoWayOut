using UnityEngine;
using ProceduralGeneration.Core;
using ProceduralGeneration.Data;
using Core;

namespace ProceduralGeneration.Components
{
    
    /// Door controller để teleport giữa các phòng.
    /// Tự động mở/đóng khi player lại gần, hỗ trợ lock/unlock cho boss fights.
    /// Merged từ Door.cs và DoorController.cs
    
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
        [SerializeField] private float detectionRange = 4f;
        
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
        [SerializeField] private Color unlockedColor = Color.white;
        
        [Tooltip("Màu khi door khóa")]
        [SerializeField] private Color lockedColor = Color.red;
        
        [Header("Animation & Audio")]
        [Tooltip("Animator cho animation mở/đóng cửa")]
        public Animator doorAnimator;
        
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
            
            // Tự động tìm Animator nếu chưa gán (tìm cả trong children)
            if (doorAnimator == null)
            {
                doorAnimator = GetComponent<Animator>();
                if (doorAnimator == null)
                    doorAnimator = GetComponentInChildren<Animator>();
            }
            
            // Setup audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            
            // Đảm bảo có Rigidbody2D static để collider hoạt động chặn player
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
            }
            
            UpdateVisualFeedback();
            UpdateColliderState(); // CRITICAL: Set collider state ngay từ đầu
        }
        
        void Start()
        {
            // Tự động tìm player nếu chưa gán
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            
            // Tự động tìm Room từ DungeonManager nếu null (map generate trước Play Mode)
            if (currentRoom == null || targetRoom == null)
            {
                TryFindRoomsFromDungeonManager();
            }
            
            // Set collider state dựa vào isOpen
            UpdateColliderState();
        }
        
        
        /// Tìm currentRoom và targetRoom từ DungeonManager khi map generate trước Play Mode
        
        private void TryFindRoomsFromDungeonManager()
        {
            // Tìm DungeonManager
            var dungeonManager = FindFirstObjectByType<ProceduralGeneration.Core.DungeonManager>();
            if (dungeonManager == null)
            {
                Debug.LogWarning($"[DoorTrigger] Cannot find DungeonManager to lookup rooms");
                return;
            }
            
            var allRooms = dungeonManager.GetAllRooms();
            
            // Đi lên hierarchy cho đến khi tìm thấy Room GameObject
            Transform current = transform.parent;
            ProceduralGeneration.Core.Room foundRoom = null;
            
            while (current != null && foundRoom == null)
            {
                foundRoom = dungeonManager.GetRoomByGameObject(current.gameObject);
                if (foundRoom != null) break;
                current = current.parent;
            }
            
            if (foundRoom == null)
            {
                Debug.LogWarning($"[DoorTrigger] Cannot find Room in hierarchy of {gameObject.name}");
                return;
            }
            
            // Tìm currentRoom
            if (currentRoom == null)
            {
                currentRoom = foundRoom;
            }
            
            // Tìm targetRoom từ connectedRooms
            if (currentRoom != null && targetRoom == null)
            {
                if (currentRoom.connectedRooms != null && currentRoom.connectedRooms.ContainsKey(doorDirection))
                {
                    targetRoom = currentRoom.connectedRooms[doorDirection];
                }
                else
                {
                    Debug.LogWarning($"[DoorTrigger] No connected room in direction {doorDirection} for {currentRoom.roomData.roomType}");
                }
            }
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
            // Player collided with closed door - no action needed
        }
        
        
        /// Trigger room transition khi player đi qua door
        
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
        
        
        /// Mở cửa
        
        public void OpenDoor()
        {
            if (isLocked)
            {
                PlaySound(lockedSound);
                return;
            }
            
            isOpen = true;
            UpdateVisualFeedback();
            UpdateColliderState();
            PlayAnimation("Open");
            PlaySound(openSound);
        }
        
        
        /// Đóng cửa
        
        public void CloseDoor()
        {
            isOpen = false;
            UpdateVisualFeedback();
            UpdateColliderState();
            PlayAnimation("Close");
            PlaySound(closeSound);
        }
        
        
        /// Toggle cửa
        
        public void ToggleDoor()
        {
            if (isOpen)
                CloseDoor();
            else
                OpenDoor();
        }
        
        #endregion
        
        #region Lock/Unlock Methods
        
        
        /// Khóa door (dùng cho boss fights)
        
        public void LockDoor()
        {
            isLocked = true;
            CloseDoor(); // Đóng cửa khi khóa
            UpdateVisualFeedback();
        }
        
        
        /// Mở khóa door (sau khi clear room)
        
        public void UnlockDoor()
        {
            isLocked = false;
            UpdateVisualFeedback();
        }
        
        
        /// Check door có bị khóa không
        
        public bool IsLocked()
        {
            return isLocked;
        }
        
        #endregion
        
        
        /// Cập nhật visual feedback — không dùng màu nhận diện, ánh sáng quyết định tầm nhìn
        
        private void UpdateVisualFeedback()
        {
            // Mọi trạng thái đều giữ nguyên màu gốc — không highlight
            // Player phải dùng ánh sáng để phát hiện cửa
            SetDoorColor(Color.white);
            
            // Update visual GameObjects
            if (closedVisual != null)
                closedVisual.SetActive(!isOpen);
            
            if (openVisual != null)
                openVisual.SetActive(isOpen);
        }
        
        /// <summary>
        /// Set màu/alpha cho tất cả SpriteRenderer của door
        /// </summary>
        private void SetDoorColor(Color color)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = color;
            
            var childRenderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in childRenderers)
                sr.color = color;
        }
        
        
        /// Cập nhật collider state (trigger khi mở, solid khi đóng)
        
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
        
        
        /// Play animation
        
        private void PlayAnimation(string triggerName)
        {
            if (doorAnimator != null)
            {
                // Dùng Bool parameter "isOpen" giống Door.cs cũ
                // Thay vì dùng Trigger để tương thích với animation có sẵn
                if (triggerName == "Open")
                {
                    doorAnimator.SetBool("isOpen", true);
                }
                else if (triggerName == "Close")
                {
                    doorAnimator.SetBool("isOpen", false);
                }
            }
        }
        
        
        /// Play sound
        
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
