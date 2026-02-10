using UnityEngine;
using System.Collections;
using ProceduralGeneration.Core;
using ProceduralGeneration.Data;

namespace Core
{
    /// <summary>
    /// Quản lý room transitions: fade in/out, activate/deactivate rooms, teleport player.
    /// Singleton pattern để DoorTrigger dễ dàng truy cập.
    /// </summary>
    public class RoomTransitionManager : MonoBehaviour
    {
        public static RoomTransitionManager Instance { get; private set; }
        
        [Header("Transition Settings")]
        [Tooltip("Thời gian fade out/in (giây)")]
        [SerializeField] private float transitionDuration = 0.5f;
        
        [Tooltip("Màu fade (thường là đen)")]
        [SerializeField] private Color fadeColor = Color.black;
        
        [Header("References")]
        [Tooltip("Canvas chứa fade panel (tự động tìm nếu null)")]
        [SerializeField] private CanvasGroup fadePanel;
        
        [Tooltip("Player GameObject (tự động tìm nếu null)")]
        [SerializeField] private GameObject player;
        
        private Room currentActiveRoom;
        private bool isTransitioning = false;
        
        void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        void Start()
        {
            // Auto-find player
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }
            
            // TODO: Auto-create fade panel if null
            if (fadePanel == null)
            {
                Debug.LogWarning("[RoomTransitionManager] Fade panel not assigned! Transitions will be instant.");
            }
        }
        
        /// <summary>
        /// Transition từ currentRoom sang targetRoom
        /// </summary>
        public void TransitionToRoom(Room fromRoom, Room toRoom, DoorDirection doorDirection, GameObject player)
        {
            if (isTransitioning)
            {
                Debug.LogWarning("[RoomTransitionManager] Already transitioning! Ignoring.");
                return;
            }
            
            if (toRoom == null || toRoom.roomInstance == null)
            {
                Debug.LogError("[RoomTransitionManager] Target room or instance is NULL!");
                return;
            }
            
            StartCoroutine(TransitionCoroutine(fromRoom, toRoom, doorDirection, player));
        }
        
        private IEnumerator TransitionCoroutine(Room fromRoom, Room toRoom, DoorDirection doorDirection, GameObject player)
        {
            isTransitioning = true;
            
            // 1. FADE OUT
            yield return StartCoroutine(FadeOut());
            
            // 2. DEACTIVATE current room
            if (fromRoom != null && fromRoom.roomInstance != null)
            {
                fromRoom.roomInstance.SetActive(false);
                Debug.Log($"[RoomTransition] Deactivated: {fromRoom.roomData.roomType}");
            }
            
            // 3. ACTIVATE target room
            toRoom.roomInstance.SetActive(true);
            currentActiveRoom = toRoom;
            Debug.Log($"[RoomTransition] Activated: {toRoom.roomData.roomType}");
            
            // 4. TELEPORT player
            Vector3 spawnPosition = CalculatePlayerSpawnPosition(toRoom, doorDirection);
            if (player != null)
            {
                player.transform.position = spawnPosition;
                Debug.Log($"[RoomTransition] Player teleported to {spawnPosition}");
            }
            
            // 5. FADE IN
            yield return StartCoroutine(FadeIn());
            
            isTransitioning = false;
        }
        
        /// <summary>
        /// Tính spawn position của player trong target room (đối diện với cửa vào)
        /// </summary>
        private Vector3 CalculatePlayerSpawnPosition(Room room, DoorDirection enteredFrom)
        {
            // Lấy center của room
            Vector3 roomCenter = room.roomInstance.transform.position;
            int roomTilesX = room.roomData.size.x * 1; // tileSize = 1
            int roomTilesY = room.roomData.size.y * 1;
            
            Vector3 centerOffset = new Vector3(roomTilesX / 2f, roomTilesY / 2f, 0);
            Vector3 spawnPos = roomCenter + centerOffset;
            
            // Offset player về phía đối diện với cửa vào
            float offsetDistance = 3f; // 3 tiles từ edge
            
            switch (enteredFrom)
            {
                case DoorDirection.Top:
                    // Vào từ trên → spawn phía dưới
                    spawnPos.y = roomCenter.y + offsetDistance;
                    break;
                case DoorDirection.Bottom:
                    // Vào từ dưới → spawn phía trên
                    spawnPos.y = roomCenter.y + roomTilesY - offsetDistance;
                    break;
                case DoorDirection.Left:
                    // Vào từ trái → spawn phía phải
                    spawnPos.x = roomCenter.x + offsetDistance;
                    break;
                case DoorDirection.Right:
                    // Vào từ phải → spawn phía trái
                    spawnPos.x = roomCenter.x + roomTilesX - offsetDistance;
                    break;
            }
            
            return spawnPos;
        }
        
        #region Fade Effects
        
        private IEnumerator FadeOut()
        {
            if (fadePanel == null)
            {
                yield break; // Instant transition
            }
            
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                fadePanel.alpha = Mathf.Lerp(0f, 1f, elapsed / transitionDuration);
                yield return null;
            }
            
            fadePanel.alpha = 1f;
        }
        
        private IEnumerator FadeIn()
        {
            if (fadePanel == null)
            {
                yield break; // Instant transition
            }
            
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                fadePanel.alpha = Mathf.Lerp(1f, 0f, elapsed / transitionDuration);
                yield return null;
            }
            
            fadePanel.alpha = 0f;
        }
        
        #endregion
        
        /// <summary>
        /// Get current active room
        /// </summary>
        public Room GetCurrentRoom()
        {
            return currentActiveRoom;
        }
    }
}
