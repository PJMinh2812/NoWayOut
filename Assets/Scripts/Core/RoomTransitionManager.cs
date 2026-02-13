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
            if (isTransitioning)
            {
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
            }
            
            // 3. ACTIVATE target room
            toRoom.roomInstance.SetActive(true);
            currentActiveRoom = toRoom;
            
            // 3.5 Convert sprites mới sang Lit material (cho URP 2D lighting)
            ConvertRoomSpritesToLit(toRoom);
            
            // 4. TELEPORT player
            Vector3 spawnPosition = CalculatePlayerSpawnPosition(toRoom, doorDirection);
            if (player != null)
            {
                player.transform.position = spawnPosition;
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
            // Lấy ACTUAL world bounds từ Renderer thay vì dùng actualSize (grid size)
            Vector3 roomOrigin;
            float roomWidth;
            float roomHeight;
            
            // Tìm bounds thực tế từ children Renderers
            Renderer[] renderers = room.roomInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
                roomOrigin = combinedBounds.min;
                roomWidth = combinedBounds.size.x;
                roomHeight = combinedBounds.size.y;
            }
            else
            {
                // Fallback: dùng transform position và actualSize * assumed tileSize
                roomOrigin = room.roomInstance.transform.position;
                float tileSize = 11f; // Default tile size
                roomWidth = room.actualSize.x * tileSize;
                roomHeight = room.actualSize.y * tileSize;
            }
            
            // Spawn ngay cạnh cửa đối diện (center x hoặc y, offset từ wall)
            // Wall thickness = 1 tile, spawn buffer = 1.5 tile từ wall
            float spawnOffset = 2.5f;
            
            Vector3 spawnPos;
            
            switch (enteredFrom)
            {
                case DoorDirection.Top:
                    // Đi vào từ cửa TOP của phòng cũ → spawn ở cửa BOTTOM của phòng mới
                    // Vị trí: center X, gần bottom
                    spawnPos = new Vector3(
                        roomOrigin.x + roomWidth / 2f,   // Center X
                        roomOrigin.y + spawnOffset,       // Gần bottom wall
                        0
                    );
                    break;
                case DoorDirection.Bottom:
                    // Đi vào từ cửa BOTTOM của phòng cũ → spawn ở cửa TOP của phòng mới
                    // Vị trí: center X, gần top
                    spawnPos = new Vector3(
                        roomOrigin.x + roomWidth / 2f,   // Center X
                        roomOrigin.y + roomHeight - spawnOffset, // Gần top wall
                        0
                    );
                    break;
                case DoorDirection.Left:
                    // Đi vào từ cửa LEFT của phòng cũ → spawn ở cửa RIGHT của phòng mới
                    // Vị trí: gần right, center Y
                    spawnPos = new Vector3(
                        roomOrigin.x + roomWidth - spawnOffset, // Gần right wall
                        roomOrigin.y + roomHeight / 2f,         // Center Y
                        0
                    );
                    break;
                case DoorDirection.Right:
                    // Đi vào từ cửa RIGHT của phòng cũ → spawn ở cửa LEFT của phòng mới
                    // Vị trí: gần left, center Y
                    spawnPos = new Vector3(
                        roomOrigin.x + spawnOffset,      // Gần left wall
                        roomOrigin.y + roomHeight / 2f,  // Center Y
                        0
                    );
                    break;
                default:
                    // Fallback: spawn ở center
                    spawnPos = roomOrigin + new Vector3(roomWidth / 2f, roomHeight / 2f, 0);
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
        
        /// <summary>
        /// Convert tất cả SpriteRenderer + TilemapRenderer trong room sang Sprite-Lit-Default
        /// Cần để URP 2D Light ảnh hưởng lên sprites
        /// </summary>
        private void ConvertRoomSpritesToLit(Room room)
        {
            if (room?.roomInstance == null) return;
            
            Shader litShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (litShader == null) return;
            
            Material litMat = new Material(litShader);
            
            var spriteRenderers = room.roomInstance.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in spriteRenderers)
            {
                if (sr.sharedMaterial == null || !sr.sharedMaterial.name.Contains("Lit"))
                    sr.sharedMaterial = litMat;
            }
            
            var tilemapRenderers = room.roomInstance.GetComponentsInChildren<UnityEngine.Tilemaps.TilemapRenderer>(true);
            foreach (var tr in tilemapRenderers)
            {
                if (tr.sharedMaterial == null || !tr.sharedMaterial.name.Contains("Lit"))
                    tr.sharedMaterial = litMat;
            }
        }
    }
}
