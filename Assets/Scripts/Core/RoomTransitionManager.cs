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
                if (player != null)
                {
                    Debug.Log($"[RoomTransitionManager] Found player: {player.name}");
                }
                else
                {
                    Debug.LogError("[RoomTransitionManager] Player NOT found! Make sure player has 'Player' tag");
                }
            }
            
            // Auto-create fade panel if null
            if (fadePanel == null)
            {
                fadePanel = CreateFadePanel();
                Debug.Log("[RoomTransitionManager] Auto-created fade panel");
            }
        }
        
        /// <summary>
        /// Tạo fade panel UI với CanvasGroup
        /// </summary>
        private CanvasGroup CreateFadePanel()
        {
            // Tìm hoặc tạo Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("TransitionCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999; // Top layer
                
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            // Tạo fade panel
            GameObject panelObj = new GameObject("FadePanel");
            panelObj.transform.SetParent(canvas.transform, false);
            
            // Full screen RectTransform
            RectTransform rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Black image
            UnityEngine.UI.Image image = panelObj.AddComponent<UnityEngine.UI.Image>();
            image.color = fadeColor;
            
            // CanvasGroup for fade
            CanvasGroup canvasGroup = panelObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0; // Start invisible
            canvasGroup.blocksRaycasts = false;
            
            return canvasGroup;
        }
        
        /// <summary>
        /// Transition từ currentRoom sang targetRoom
        /// </summary>
        public void TransitionToRoom(Room fromRoom, Room toRoom, DoorDirection doorDirection, GameObject player)
        {
            Debug.Log($"[RoomTransitionManager] TransitionToRoom called: {fromRoom?.roomData?.roomType} -> {toRoom?.roomData?.roomType}");
            
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
            
            Debug.Log("[RoomTransitionManager] Starting transition coroutine...");
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
            // Lấy bottom-left corner của room
            Vector3 roomOrigin = room.roomInstance.transform.position;
            
            // SỬ DỤNG actualSize (runtime size) thay vì roomData.size (ScriptableObject default)
            int roomTilesX = room.actualSize.x;
            int roomTilesY = room.actualSize.y;
            
            // Tính center thực của room
            Vector3 roomCenter = roomOrigin + new Vector3(roomTilesX / 2f, roomTilesY / 2f, 0);
            
            // Tính vị trí cửa (ở giữa edge), sau đó spawn 1 tile vào trong từ cửa
            Vector3 doorPos = roomCenter; // Bắt đầu từ center
            Vector3 spawnPos;
            
            switch (enteredFrom)
            {
                case DoorDirection.Top:
                    // Player đi qua cửa TOP của phòng cũ → spawn ở BOTTOM của phòng mới
                    // Spawn 1 tile bên trong từ bottom edge
                    spawnPos = roomCenter;
                    spawnPos.y = roomOrigin.y + 1f; // 1 tile lên từ bottom edge
                    break;
                case DoorDirection.Bottom:
                    // Player đi qua cửa BOTTOM của phòng cũ → spawn ở TOP của phòng mới
                    // Spawn 1 tile bên trong từ top edge
                    spawnPos = roomCenter;
                    spawnPos.y = roomOrigin.y + roomTilesY - 2f; // 1 tile xuống từ top edge
                    break;
                case DoorDirection.Left:
                    // Player đi qua cửa LEFT của phòng cũ → spawn ở RIGHT của phòng mới
                    // Spawn 1 tile bên trong từ right edge
                    spawnPos = roomCenter;
                    spawnPos.x = roomOrigin.x + roomTilesX - 2f; // 1 tile sang trái từ right edge
                    break;
                case DoorDirection.Right:
                    // Player đi qua cửa RIGHT của phòng cũ → spawn ở LEFT của phòng mới
                    // Spawn 1 tile bên trong từ left edge
                    spawnPos = roomCenter;
                    spawnPos.x = roomOrigin.x + 1f; // 1 tile sang phải từ left edge
                    break;
                default:
                    // Fallback: spawn ở center
                    spawnPos = roomCenter;
                    break;
            }
            
            Debug.Log($"[RoomTransition] Spawn calculated: Room origin={roomOrigin}, size=({roomTilesX},{roomTilesY}), enteredFrom={enteredFrom}, spawn={spawnPos}");
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
