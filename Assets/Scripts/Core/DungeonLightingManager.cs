using UnityEngine;
using UnityEngine.Rendering.Universal;
using ProceduralGeneration.Core;

namespace NWO
{
    /// <summary>
    /// Quản lý hệ thống ánh sáng dungeon:
    /// - Global Light tối (ambient gần 0) → dungeon mặc định tối đen
    /// - Player có Light2D nhỏ (1 ô) → mở rộng khi nhặt Light Fragment
    /// - Start Room sáng hoàn toàn (full visibility)
    /// - Doors, items trong Start Room đều visible
    /// </summary>
    public class DungeonLightingManager : MonoBehaviour
    {
        public static DungeonLightingManager Instance { get; private set; }
        
        [Header("Global Darkness")]
        [Tooltip("Ambient light gần 0 → dungeon tối hoàn toàn, kinh dị")]
        [SerializeField] private float globalLightIntensity = 0.005f;
        [SerializeField] private Color globalLightColor = new Color(0.05f, 0.05f, 0.1f); // Gần đen hoàn toàn
        
        [Header("Player Light")]
        [Tooltip("Radius ánh sáng player — rất nhỏ, sát người, kinh dị")]
        [SerializeField] private float playerDefaultLightRadius = 2.5f;
        [Tooltip("Radius tăng thêm mỗi lần nhặt Fragment")]
        [SerializeField] private float radiusPerFragment = 1.25f;
        [Tooltip("Radius tối đa sau khi nhặt đủ 3 Fragment")]
        [SerializeField] private float playerMaxLightRadius = 6.5f;
        [SerializeField] private float playerLightIntensity = 0.8f;
        [SerializeField] private Color playerLightColor = new Color(0.9f, 0.85f, 0.7f); // Vàng nhạt yếu
        
        [Header("Start Room Light")]
        [Tooltip("Intensity ánh sáng phòng Start (full sáng)")]
        [SerializeField] private float startRoomLightIntensity = 1.2f;
        [SerializeField] private Color startRoomLightColor = Color.white;
        
        [Header("References (auto-find)")]
        [SerializeField] private Light2D globalLight;
        [SerializeField] private Light2D playerLight;
        
        private GameObject playerObj;
        private int fragmentsCollected = 0;
        private bool startRoomLightCreated = false;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private void Start()
        {
            SetupGlobalLight();
            SetupPlayerLight();
            
            // Start Room light dùng coroutine để đợi rooms sẵn sàng
            StartCoroutine(SetupStartRoomLightWhenReady());
            
            // Convert tất cả sprites sang Lit material để phản ứng với ánh sáng
            StartCoroutine(ConvertSpritesToLitMaterial());
            
            // Đăng ký event nhặt fragment
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLightFragmentCollected += OnFragmentCollected;
            }
        }
        
        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLightFragmentCollected -= OnFragmentCollected;
            }
        }
        
        #region Global Light Setup
        
        /// <summary>
        /// Tạo Global Light 2D với intensity rất thấp → dungeon tối đen
        /// </summary>
        private void SetupGlobalLight()
        {
            if (globalLight == null)
            {
                // Tìm global light đã có trong scene
                var existingLights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
                foreach (var light in existingLights)
                {
                    if (light.lightType == Light2D.LightType.Global)
                    {
                        globalLight = light;
                        break;
                    }
                }
            }
            
            // Tạo mới nếu chưa có
            if (globalLight == null)
            {
                var globalLightObj = new GameObject("GlobalLight2D");
                globalLightObj.transform.SetParent(transform);
                globalLight = globalLightObj.AddComponent<Light2D>();
                globalLight.lightType = Light2D.LightType.Global;
            }
            
            globalLight.intensity = globalLightIntensity;
            globalLight.color = globalLightColor;
        }
        
        #endregion
        
        #region Player Light Setup
        
        /// <summary>
        /// Thêm Light2D vào Player — radius mặc định ≈ 1 ô nhìn quanh
        /// </summary>
        private void SetupPlayerLight()
        {
            // Tìm Player
            playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogWarning("[DungeonLighting] Player not found! Will retry...");
                StartCoroutine(RetrySetupPlayerLight());
                return;
            }
            
            CreatePlayerLight();
        }
        
        private void CreatePlayerLight()
        {
            if (playerObj == null) return;
            
            // === Convert player sprite sang Lit material ===
            var playerSR = playerObj.GetComponent<SpriteRenderer>();
            if (playerSR != null)
            {
                Material litMat = FindLitMaterial();
                if (litMat != null && (playerSR.sharedMaterial == null || !playerSR.sharedMaterial.name.Contains("Lit")))
                {
                    playerSR.sharedMaterial = litMat;
                }
            }
            
            // Kiểm tra đã có Light2D chưa
            playerLight = playerObj.GetComponentInChildren<Light2D>();
            
            if (playerLight == null)
            {
                // Tạo child object cho light
                var lightObj = new GameObject("PlayerLight");
                lightObj.transform.SetParent(playerObj.transform);
                lightObj.transform.localPosition = Vector3.zero;
                
                playerLight = lightObj.AddComponent<Light2D>();
                playerLight.lightType = Light2D.LightType.Point;
            }
            
            // Setup ánh sáng player — cực nhỏ, sát người, horror feel
            playerLight.pointLightOuterRadius = playerDefaultLightRadius;
            playerLight.pointLightInnerRadius = playerDefaultLightRadius * 0.2f; // Lõi sáng rất nhỏ
            playerLight.intensity = playerLightIntensity;
            playerLight.color = playerLightColor;
            playerLight.shadowIntensity = 0.9f; // Bóng đậm
            playerLight.falloffIntensity = 0.8f; // Tắt rất nhanh ở rìa
        }
        
        private System.Collections.IEnumerator RetrySetupPlayerLight()
        {
            // Đợi tối đa 3 giây cho player xuất hiện
            float timeout = 3f;
            float elapsed = 0f;
            while (playerObj == null && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;
                playerObj = GameObject.FindGameObjectWithTag("Player");
            }
            
            if (playerObj != null)
                CreatePlayerLight();
            else
                Debug.LogError("[DungeonLighting] Player not found after 3s timeout!");
        }
        
        #endregion
        
        #region Start Room Light
        
        /// <summary>
        /// Đợi cho rooms sẵn sàng rồi mới tạo Start Room light
        /// </summary>
        private System.Collections.IEnumerator SetupStartRoomLightWhenReady()
        {
            // Đợi 1 frame để DungeonManager.Awake() chạy xong
            yield return null;
            yield return null;
            
            // Thử tối đa 10 lần, mỗi lần cách 0.3s
            for (int attempt = 0; attempt < 10; attempt++)
            {
                if (TrySetupStartRoomLight())
                {
                    startRoomLightCreated = true;
                    yield break;
                }
                
                yield return new WaitForSeconds(0.3f);
            }
            
            Debug.LogError("[DungeonLighting] Failed to create Start Room light after 10 attempts!");
        }
        
        /// <summary>
        /// Thử tạo Start Room light. Return true nếu thành công.
        /// </summary>
        private bool TrySetupStartRoomLight()
        {
            var dungeonManager = FindFirstObjectByType<DungeonManager>();
            if (dungeonManager == null)
            {
                return false;
            }
            
            var allRooms = dungeonManager.GetAllRooms();
            if (allRooms == null || allRooms.Count == 0)
            {
                return false;
            }
            
            Room startRoom = null;
            foreach (var room in allRooms)
            {
                if (room.roomData != null && 
                    room.roomData.roomType == ProceduralGeneration.Data.RoomType.Start)
                {
                    startRoom = room;
                    break;
                }
            }
            
            if (startRoom == null || startRoom.roomInstance == null)
            {
                return false;
            }
            
            // Kiểm tra đã có RoomLight chưa
            var existing = startRoom.roomInstance.transform.Find("StartRoomLight");
            if (existing != null)
            {
                return true;
            }
            
            // Tính kích thước room từ renderer bounds
            float roomRadius = 10f; // Default lớn hơn
            Vector3 center = startRoom.roomInstance.transform.position;
            
            var renderers = startRoom.roomInstance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers)
                    bounds.Encapsulate(r.bounds);
                
                // Radius = đường chéo / 2 + buffer
                roomRadius = Mathf.Max(bounds.size.x, bounds.size.y) * 0.75f;
                center = bounds.center;
            }
            
            // Tạo Light2D Point cho Start Room
            var lightObj = new GameObject("StartRoomLight");
            lightObj.transform.SetParent(startRoom.roomInstance.transform);
            lightObj.transform.position = new Vector3(center.x, center.y, 0);
            
            var roomLight = lightObj.AddComponent<Light2D>();
            roomLight.lightType = Light2D.LightType.Point;
            roomLight.pointLightOuterRadius = roomRadius;
            roomLight.pointLightInnerRadius = roomRadius * 0.7f;
            roomLight.intensity = startRoomLightIntensity;
            roomLight.color = startRoomLightColor;
            roomLight.shadowIntensity = 0f; // Không đổ bóng trong start room
            
            Debug.Log($"[DungeonLighting] ★ Start Room light CREATED! radius={roomRadius}, center={center}");
            return true;
        }
        
        #endregion
        
        #region Fragment Collection → Expand Light
        
        /// <summary>
        /// Khi nhặt fragment → tăng radius ánh sáng player
        /// 0 fragment: 6 units (≈1 ô quanh player)
        /// 1 fragment: 10 units (≈2 ô)
        /// 2 fragment: 14 units (≈2.5 ô)
        /// 3 fragment: 18 units (≈3 ô)
        /// </summary>
        private void OnFragmentCollected(int current, int total)
        {
            fragmentsCollected = current;
            ExpandPlayerLight();
        }
        
        /// <summary>
        /// Mở rộng ánh sáng player (với animation mượt)
        /// </summary>
        public void ExpandPlayerLight()
        {
            if (playerLight == null) return;
            
            float targetRadius = Mathf.Min(
                playerDefaultLightRadius + fragmentsCollected * radiusPerFragment,
                playerMaxLightRadius
            );
            
            StartCoroutine(AnimateRadiusExpand(targetRadius));
        }
        
        private System.Collections.IEnumerator AnimateRadiusExpand(float targetRadius)
        {
            float startRadius = playerLight.pointLightOuterRadius;
            float startInner = playerLight.pointLightInnerRadius;
            float targetInner = targetRadius * 0.4f;
            float duration = 1f;
            float elapsed = 0f;
            
            // Flash sáng lên khi nhặt
            float originalIntensity = playerLight.intensity;
            playerLight.intensity = originalIntensity * 2f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float smooth = Mathf.SmoothStep(0f, 1f, t);
                
                playerLight.pointLightOuterRadius = Mathf.Lerp(startRadius, targetRadius, smooth);
                playerLight.pointLightInnerRadius = Mathf.Lerp(startInner, targetInner, smooth);
                playerLight.intensity = Mathf.Lerp(originalIntensity * 2f, originalIntensity, t);
                
                yield return null;
            }
            
            playerLight.pointLightOuterRadius = targetRadius;
            playerLight.pointLightInnerRadius = targetInner;
            playerLight.intensity = originalIntensity;
        }
        
        #endregion
        
        #region Public API
        
        public Light2D GetPlayerLight() => playerLight;
        public float GetCurrentPlayerRadius() => playerLight != null ? playerLight.pointLightOuterRadius : 0f;
        
        public void OnPlayerEnterRoom(Room room)
        {
            // Có thể dùng để thay đổi lighting theo room type sau này
        }
        
        #endregion
        
        #region Sprite Material Conversion
        
        /// <summary>
        /// Convert tất cả SpriteRenderer sang Sprite-Lit-Default material
        /// Sprites phải dùng Lit material mới phản ứng với URP 2D Light
        /// Nếu dùng Sprites-Default (unlit) thì ánh sáng không có tác dụng!
        /// </summary>
        private System.Collections.IEnumerator ConvertSpritesToLitMaterial()
        {
            // Đợi vài frame để scene load xong
            yield return null;
            yield return null;
            yield return null;
            
            // Tìm Sprite-Lit-Default material
            Material litMaterial = FindLitMaterial();
            if (litMaterial == null)
            {
                Debug.LogError("[DungeonLighting] Cannot find Sprite-Lit-Default material! Sprites sẽ không phản ứng với ánh sáng.");
                yield break;
            }
            
            // Convert tất cả SpriteRenderer trong scene
            int converted = 0;
            var allRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var sr in allRenderers)
            {
                if (sr.sharedMaterial != null && sr.sharedMaterial.name.Contains("Default") 
                    && !sr.sharedMaterial.name.Contains("Lit"))
                {
                    sr.sharedMaterial = litMaterial;
                    converted++;
                }
                // Cũng convert nếu material null
                else if (sr.sharedMaterial == null)
                {
                    sr.sharedMaterial = litMaterial;
                    converted++;
                }
            }
            
            // Convert TilemapRenderer
            var tilemapRenderers = FindObjectsByType<UnityEngine.Tilemaps.TilemapRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var tr in tilemapRenderers)
            {
                if (tr.sharedMaterial != null && tr.sharedMaterial.name.Contains("Default")
                    && !tr.sharedMaterial.name.Contains("Lit"))
                {
                    tr.sharedMaterial = litMaterial;
                    converted++;
                }
            }
            
            Debug.Log($"[DungeonLighting] ★ Converted {converted} renderers to Sprite-Lit-Default material");
        }
        
        /// <summary>
        /// Tìm Sprite-Lit-Default material từ nhiều nguồn
        /// </summary>
        private Material FindLitMaterial()
        {
            // Cách 1: Load từ built-in resources
            Material mat = Resources.Load<Material>("Sprite-Lit-Default");
            if (mat != null) return mat;
            
            // Cách 2: Tìm bằng Shader
            Shader litShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (litShader != null)
            {
                mat = new Material(litShader);
                mat.name = "Sprite-Lit-Default (Auto)";
                return mat;
            }
            
            // Cách 3: Thử shader name khác
            litShader = Shader.Find("Sprites/Lit");
            if (litShader != null)
            {
                mat = new Material(litShader);
                return mat;
            }
            
            return null;
        }
        
        #endregion
    }
}
