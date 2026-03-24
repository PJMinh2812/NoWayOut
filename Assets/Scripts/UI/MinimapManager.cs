using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using ProceduralGeneration.Core;
using ProceduralGeneration.Data;
using Core;

namespace NWO
{
    /// <summary>
    /// MinimapManager — Minimap hình tròn dùng RenderTexture
    /// Tạo hoàn toàn bằng code: Camera, Light, Canvas, RawImage, Mask, Player Marker, Radar Sweep
    /// </summary>
    public class MinimapManager : MonoBehaviour
    {
        public static MinimapManager Instance { get; private set; }

        [Header("Minimap Settings")]
        [SerializeField] private float minimapDiameter = 200f;
        [SerializeField] private int renderTextureSize = 512;
        [SerializeField] private float cameraOrthoSize = 25f;
        [SerializeField] private Vector2 minimapOffset = new Vector2(-16f, -50f);

        [Header("Player Marker")]
        [SerializeField] private float playerMarkerSize = 10f;
        [SerializeField] private Color playerMarkerColor = new Color(0f, 0.9f, 1f, 1f); // Cyan

        [Header("Minimap Light")]
        [SerializeField] private float minimapLightIntensity = 0.35f;

        [Header("Frame Colors")]
        [SerializeField] private Color frameBgColor = new Color(0.03f, 0.03f, 0.05f, 0.92f);
        [SerializeField] private Color frameBorderColor = new Color(0.2f, 0.25f, 0.4f, 0.6f);

        [Header("Camera")]
        [SerializeField] private float cameraSmoothSpeed = 8f;

        [Header("Room Layout Minimap (Soul Knight Style)")]
        [SerializeField] private bool useRoomLayoutMinimap = true;
        [SerializeField] private float roomNodeSize = 16f;
        [SerializeField] private float roomSpacing = 28f;
        [SerializeField] private float connectionThickness = 3f;
        [SerializeField] private float mapPaddingRatio = 0.14f;
        [SerializeField] private float roomNodeFrameSize = 3f;
        [SerializeField] private Color roomNodeFrameColor = new Color(0.08f, 0.12f, 0.2f, 0.95f);
        [SerializeField] private Color roomDefaultColor = new Color(0.78f, 0.82f, 0.9f, 0.95f);
        [SerializeField] private Color startRoomColor = new Color(0.3f, 0.95f, 0.55f, 1f);
        [SerializeField] private Color goalRoomColor = new Color(1f, 0.72f, 0.22f, 1f);
        [SerializeField] private Color bossRoomColor = new Color(1f, 0.4f, 0.35f, 1f);
        [SerializeField] private Color currentRoomColor = new Color(0.2f, 0.95f, 1f, 1f);
        [SerializeField] private Color connectionColor = new Color(0.7f, 0.78f, 0.92f, 0.45f);

        // Runtime references
        private Camera minimapCamera;
        private RenderTexture minimapRT;
        private Light2D minimapLight;
        private Canvas minimapCanvas;
        private RawImage mapRawImage;
        private Image playerMarkerImage;
        private Transform playerTransform;
        private CanvasGroup minimapCanvasGroup;
        private TextMeshProUGUI roundMapLabel;
        private bool isInitialized = false;

        private RectTransform roomLayoutRoot;
        private RectTransform roomConnectionRoot;
        private readonly Dictionary<Room, Image> roomNodes = new Dictionary<Room, Image>();
        private readonly Dictionary<Room, RectTransform> roomNodeRects = new Dictionary<Room, RectTransform>();
        private readonly Dictionary<string, Image> fallbackNodes = new Dictionary<string, Image>();
        private readonly Dictionary<string, RectTransform> fallbackNodeRects = new Dictionary<string, RectTransform>();
        private readonly Dictionary<string, Vector3> fallbackRoomCenters = new Dictionary<string, Vector3>();
        private readonly Dictionary<string, RoomType> fallbackRoomTypes = new Dictionary<string, RoomType>();
        private readonly Dictionary<string, Transform> fallbackRoomTransforms = new Dictionary<string, Transform>();
        private DungeonManager dungeonManager;
        private Room currentTrackedRoom;
        private string currentTrackedFallbackRoom;
        private float nextLayoutRefreshAt;
        private string cachedLayoutSignature;
        private bool usingFallbackLayout;

        private static Sprite _cachedSolid;

        // Cached circle sprite
        private static Sprite _cachedCircle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Minimap mới luôn ưu tiên layout phòng kiểu Soul Knight.
            useRoomLayoutMinimap = true;
        }

        private void Start()
        {
            StartCoroutine(InitializeWhenReady());
        }

        private System.Collections.IEnumerator InitializeWhenReady()
        {
            yield return null; // Đợi frame 1 — Awake() tất cả objects xong
            yield return null; // Đợi frame 2 — Start() tất cả objects xong

            // Tìm player với timeout 5 giây
            float elapsed = 0f;
            while (playerTransform == null && elapsed < 5f)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                    playerTransform = p.transform;
                else
                {
                    yield return new WaitForSeconds(0.3f);
                    elapsed += 0.3f;
                }
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("[MinimapManager] Player not found after 5s timeout, minimap disabled.");
                yield break;
            }

            CreateRenderTexture();
            CreateMinimapCamera();
            CreateMinimapUI();

            // Subscribe fragment events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLightFragmentCollected += OnFragmentCollected;
                GameManager.Instance.OnAllLightFragmentsCollected += RevealAllRooms;
            }

            isInitialized = true;
            RefreshRoomLayoutIfNeeded(true);
            UpdateRoundMapLabel();
            Debug.Log("[MinimapManager] Minimap initialized successfully!");
        }

        private void Update()
        {
            // TAB key toggle minimap
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame)
                ToggleMinimap();

            if (!isInitialized || playerTransform == null) return;

            if (!useRoomLayoutMinimap)
                UpdateCameraPosition();

            RefreshRoomLayoutIfNeeded();
            UpdateCurrentRoomHighlight();
            UpdateRoundMapLabel();
        }

        private void ToggleMinimap()
        {
            if (minimapCanvasGroup == null) return;
            bool isVisible = minimapCanvasGroup.alpha > 0.5f;
            minimapCanvasGroup.alpha = isVisible ? 0f : 1f;
        }

        private void OnDestroy()
        {
            // Cleanup GPU resources
            if (minimapRT != null)
            {
                minimapRT.Release();
                Destroy(minimapRT);
                minimapRT = null;
            }

            if (_cachedCircle != null)
            {
                Destroy(_cachedCircle.texture);
                Destroy(_cachedCircle);
                _cachedCircle = null;
            }

            if (_cachedSolid != null)
            {
                Destroy(_cachedSolid.texture);
                Destroy(_cachedSolid);
                _cachedSolid = null;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLightFragmentCollected -= OnFragmentCollected;
                GameManager.Instance.OnAllLightFragmentsCollected -= RevealAllRooms;
            }

            if (Instance == this)
                Instance = null;
        }

        #region RenderTexture & Camera

        private void CreateRenderTexture()
        {
            minimapRT = new RenderTexture(
                renderTextureSize,
                renderTextureSize,
                24,
                RenderTextureFormat.ARGB32
            );
            minimapRT.antiAliasing = 2;
            minimapRT.filterMode = FilterMode.Bilinear;
            minimapRT.Create();
        }

        private void CreateMinimapCamera()
        {
            Vector3 startPos = playerTransform != null
                ? new Vector3(playerTransform.position.x, playerTransform.position.y, -50f)
                : new Vector3(0, 0, -50f);

            var camObj = new GameObject("MinimapCamera");
            camObj.transform.SetParent(transform);
            camObj.transform.position = startPos;

            minimapCamera = camObj.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = cameraOrthoSize;
            minimapCamera.targetTexture = minimapRT;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.02f, 0.02f, 0.04f);
            minimapCamera.depth = -10;
            minimapCamera.cullingMask = ~(1 << 5); // Tất cả trừ UI layer

            // URP camera data
            var urpData = camObj.AddComponent<UniversalAdditionalCameraData>();
            urpData.renderType = CameraRenderType.Base;

            CreateMinimapLight(camObj.transform);
        }

        private void CreateMinimapLight(Transform parent)
        {
            var lightObj = new GameObject("MinimapLight");
            lightObj.transform.SetParent(parent);
            lightObj.transform.localPosition = Vector3.zero;

            minimapLight = lightObj.AddComponent<Light2D>();
            minimapLight.lightType = Light2D.LightType.Point;
            minimapLight.pointLightOuterRadius = cameraOrthoSize * 2.5f;
            minimapLight.pointLightInnerRadius = cameraOrthoSize * 1.5f;
            minimapLight.intensity = minimapLightIntensity;
            minimapLight.color = new Color(0.3f, 0.35f, 0.5f);
            minimapLight.falloffIntensity = 0.3f;
        }

        #endregion

        #region UI Creation

        private void CreateMinimapUI()
        {
            float d = minimapDiameter;

            // ── CANVAS ──
            var canvasObj = new GameObject("MinimapCanvas");
            canvasObj.transform.SetParent(transform);
            minimapCanvas = canvasObj.AddComponent<Canvas>();
            minimapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            minimapCanvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            // ── FRAME (container, top-right) ──
            var frameObj = new GameObject("MinimapFrame");
            frameObj.transform.SetParent(canvasObj.transform, false);
            var frameRT = AnchorTopRight(frameObj, d, d, minimapOffset);

            var frameBg = frameObj.AddComponent<Image>();
            frameBg.sprite = CreateCircleSprite(128);
            frameBg.color = frameBgColor;

            // CanvasGroup — dùng cho TAB toggle alpha
            minimapCanvasGroup = frameObj.AddComponent<CanvasGroup>();
            minimapCanvasGroup.alpha = 1f;

            // ── TITLE: "MAP" + Hint "TAB" ──
            CreateLabel(frameObj.transform, "MAP",
                new Vector2(0.5f, 0f), new Vector2(0, -14f),
                new Vector2(60f, 14f), 8f,
                new Color(0.5f, 0.6f, 0.8f, 0.6f));
            CreateLabel(frameObj.transform, "[TAB]",
                new Vector2(0.5f, 0f), new Vector2(0, -24f),
                new Vector2(60f, 12f), 7f,
                new Color(0.35f, 0.4f, 0.55f, 0.4f));

            // ── ROUND/MAP LABEL ──
            var roundMapObj = new GameObject("RoundMapLabel");
            roundMapObj.transform.SetParent(frameObj.transform, false);
            var roundMapRect = roundMapObj.AddComponent<RectTransform>();
            roundMapRect.anchorMin = new Vector2(0.5f, 0f);
            roundMapRect.anchorMax = new Vector2(0.5f, 0f);
            roundMapRect.sizeDelta = new Vector2(140f, 28f);
            roundMapRect.anchoredPosition = new Vector2(0, -38f);
            roundMapLabel = roundMapObj.AddComponent<TextMeshProUGUI>();
            roundMapLabel.text = "1-1";
            roundMapLabel.fontSize = 24;
            roundMapLabel.alignment = TMPro.TextAlignmentOptions.Center;
            roundMapLabel.color = Color.white;

            // ── BORDER ──
            var borderObj = new GameObject("MinimapBorder");
            borderObj.transform.SetParent(frameObj.transform, false);
            var borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-3f, -3f);
            borderRect.offsetMax = new Vector2(3f, 3f);
            var borderImg = borderObj.AddComponent<Image>();
            borderImg.sprite = CreateCircleSprite(128);
            borderImg.color = frameBorderColor;
            // Đặt border DƯỚI frame
            borderObj.transform.SetAsFirstSibling();

            // ── CIRCLE MASK ──
            var maskObj = new GameObject("MinimapMask");
            maskObj.transform.SetParent(frameObj.transform, false);
            var maskRect = maskObj.AddComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = Vector2.zero;
            maskRect.offsetMax = Vector2.zero;

            var maskImg = maskObj.AddComponent<Image>();
            maskImg.sprite = CreateCircleSprite(128);
            var mask = maskObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // ── MAP VIEW (RawImage) ──
            var rawObj = new GameObject("MinimapView");
            rawObj.transform.SetParent(maskObj.transform, false);
            var rawRect = rawObj.AddComponent<RectTransform>();
            rawRect.anchorMin = Vector2.zero;
            rawRect.anchorMax = Vector2.one;
            rawRect.offsetMin = Vector2.zero;
            rawRect.offsetMax = Vector2.zero;

            mapRawImage = rawObj.AddComponent<RawImage>();
            mapRawImage.texture = minimapRT;
            mapRawImage.enabled = !useRoomLayoutMinimap;

            if (useRoomLayoutMinimap)
                CreateRoomLayoutOverlay(maskObj.transform);

            // Layout mode không dùng dot giữa; camera mode mới dùng.
            if (!useRoomLayoutMinimap)
            {
                var markerObj = new GameObject("PlayerMarker");
                markerObj.transform.SetParent(maskObj.transform, false);
                var markerRect = markerObj.AddComponent<RectTransform>();
                markerRect.anchorMin = new Vector2(0.5f, 0.5f);
                markerRect.anchorMax = new Vector2(0.5f, 0.5f);
                markerRect.sizeDelta = new Vector2(playerMarkerSize, playerMarkerSize);
                markerRect.anchoredPosition = Vector2.zero;

                playerMarkerImage = markerObj.AddComponent<Image>();
                playerMarkerImage.sprite = CreateCircleSprite(64);
                playerMarkerImage.color = playerMarkerColor;

                var glowObj = new GameObject("MarkerGlow");
                glowObj.transform.SetParent(maskObj.transform, false);
                var glowRect = glowObj.AddComponent<RectTransform>();
                glowRect.anchorMin = new Vector2(0.5f, 0.5f);
                glowRect.anchorMax = new Vector2(0.5f, 0.5f);
                glowRect.sizeDelta = new Vector2(playerMarkerSize * 2.5f, playerMarkerSize * 2.5f);
                glowRect.anchoredPosition = Vector2.zero;
                glowObj.transform.SetSiblingIndex(markerObj.transform.GetSiblingIndex());

                var glowImg = glowObj.AddComponent<Image>();
                glowImg.sprite = CreateCircleSprite(64);
                glowImg.color = new Color(playerMarkerColor.r, playerMarkerColor.g, playerMarkerColor.b, 0.15f);
            }

            // ── COMPASS LABELS (N/S/E/W) ──
            CreateCompassLabel(maskObj.transform, "N", new Vector2(0.5f, 1f), new Vector2(0, -6f));
            CreateCompassLabel(maskObj.transform, "S", new Vector2(0.5f, 0f), new Vector2(0, 6f));
            CreateCompassLabel(maskObj.transform, "E", new Vector2(1f, 0.5f), new Vector2(-8f, 0));
            CreateCompassLabel(maskObj.transform, "W", new Vector2(0f, 0.5f), new Vector2(8f, 0));
        }

        private void CreateRoomLayoutOverlay(Transform parent)
        {
            var layoutObj = new GameObject("RoomLayoutRoot");
            layoutObj.transform.SetParent(parent, false);
            roomLayoutRoot = layoutObj.AddComponent<RectTransform>();
            roomLayoutRoot.anchorMin = Vector2.zero;
            roomLayoutRoot.anchorMax = Vector2.one;
            roomLayoutRoot.offsetMin = Vector2.zero;
            roomLayoutRoot.offsetMax = Vector2.zero;

            var connectionObj = new GameObject("RoomConnections");
            connectionObj.transform.SetParent(roomLayoutRoot, false);
            roomConnectionRoot = connectionObj.AddComponent<RectTransform>();
            roomConnectionRoot.anchorMin = Vector2.zero;
            roomConnectionRoot.anchorMax = Vector2.one;
            roomConnectionRoot.offsetMin = Vector2.zero;
            roomConnectionRoot.offsetMax = Vector2.zero;
        }

        private void CreateCompassLabel(Transform parent, string text, Vector2 anchor, Vector2 offset)
        {
            var obj = new GameObject($"Compass_{text}");
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(20f, 14f);
            rect.anchoredPosition = offset;

            var tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 8f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = new Color(0.4f, 0.5f, 0.7f, 0.5f);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private void CreateLabel(Transform parent, string text, Vector2 anchor, Vector2 offset,
            Vector2 size, float fontSize, Color color)
        {
            var obj = new GameObject($"Label_{text}");
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;

            var tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private RectTransform AnchorTopRight(GameObject obj, float width, float height, Vector2 offset)
        {
            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = offset;
            return rt;
        }

        #endregion

        #region Room Layout Minimap

        private void RefreshRoomLayoutIfNeeded(bool force = false)
        {
            if (!useRoomLayoutMinimap || roomLayoutRoot == null)
                return;

            if (!force && Time.time < nextLayoutRefreshAt)
                return;

            nextLayoutRefreshAt = Time.time + 0.35f;

            dungeonManager = ResolveDungeonManagerWithRooms();

            var rooms = dungeonManager != null ? dungeonManager.GetAllRooms() : null;
            if (rooms != null && rooms.Count > 0)
            {
                usingFallbackLayout = false;
                string signature = BuildLayoutSignature(rooms);
                if (!force && signature == cachedLayoutSignature)
                    return;

                BuildRoomLayoutMinimap(rooms);
                cachedLayoutSignature = signature;
                return;
            }

            Transform dungeonContainer = ResolveDungeonContainer();
            if (dungeonContainer == null)
            {
                if (force)
                    Debug.LogWarning("[MinimapManager] Cannot render layout minimap: no rooms and no DungeonContainer found.");
                return;
            }

            usingFallbackLayout = true;
            string fallbackSignature = BuildFallbackLayoutSignature(dungeonContainer);
            if (!force && fallbackSignature == cachedLayoutSignature)
                return;

            BuildFallbackLayoutFromContainer(dungeonContainer);
            cachedLayoutSignature = fallbackSignature;
        }

        private Transform ResolveDungeonContainer()
        {
            if (dungeonManager != null && dungeonManager.dungeonContainer != null)
                return dungeonManager.dungeonContainer;

            var foundManager = FindFirstObjectByType<DungeonManager>();
            if (foundManager != null && foundManager.dungeonContainer != null)
                return foundManager.dungeonContainer;

            var containerObj = GameObject.Find("DungeonContainer");
            return containerObj != null ? containerObj.transform : null;
        }

        private string BuildFallbackLayoutSignature(Transform container)
        {
            if (container == null)
                return "fallback:none";

            List<string> keys = new List<string>();
            for (int i = 0; i < container.childCount; i++)
            {
                Transform child = container.GetChild(i);
                if (!TryParseRoomFromName(child.name, out RoomType roomType, out Vector2Int gridPos))
                    continue;

                keys.Add($"{gridPos.x},{gridPos.y},{(int)roomType},{(child.gameObject.activeSelf ? 1 : 0)}");
            }

            keys.Sort();
            return "fallback:" + string.Join("|", keys);
        }

        private void BuildFallbackLayoutFromContainer(Transform container)
        {
            ClearRoomLayoutVisuals();

            List<(string key, Vector2Int gridPos, RoomType roomType, Transform roomTransform)> rooms =
                new List<(string key, Vector2Int gridPos, RoomType roomType, Transform roomTransform)>();

            for (int i = 0; i < container.childCount; i++)
            {
                Transform child = container.GetChild(i);
                if (!TryParseRoomFromName(child.name, out RoomType roomType, out Vector2Int gridPos))
                    continue;

                string key = $"{gridPos.x}_{gridPos.y}";
                rooms.Add((key, gridPos, roomType, child));
            }

            if (rooms.Count == 0)
                return;

            int minX = rooms.Min(r => r.gridPos.x);
            int maxX = rooms.Max(r => r.gridPos.x);
            int minY = rooms.Min(r => r.gridPos.y);
            int maxY = rooms.Max(r => r.gridPos.y);

            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;

            float mapSize = minimapDiameter * (1f - mapPaddingRatio * 2f);
            float gridWidth = Mathf.Max(1f, maxX - minX + 1f);
            float gridHeight = Mathf.Max(1f, maxY - minY + 1f);
            float maxAxis = Mathf.Max(gridWidth, gridHeight);
            float adaptiveSpacing = maxAxis <= 1f ? roomSpacing : mapSize / maxAxis;
            float nodeSpacing = Mathf.Min(roomSpacing, adaptiveSpacing);

            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                Vector2 roomPos = new Vector2(
                    (room.gridPos.x - centerX) * nodeSpacing,
                    (room.gridPos.y - centerY) * nodeSpacing);

                var nodeObj = new GameObject($"FallbackRoomNode_{room.key}", typeof(RectTransform));
                nodeObj.transform.SetParent(roomLayoutRoot, false);

                var rect = nodeObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(roomNodeSize, roomNodeSize);
                rect.anchoredPosition = roomPos;

                var frame = nodeObj.AddComponent<Image>();
                frame.sprite = CreateSolidSprite();
                frame.type = Image.Type.Simple;
                frame.color = roomNodeFrameColor;

                var innerObj = new GameObject("Fill");
                innerObj.transform.SetParent(nodeObj.transform, false);
                var innerRect = innerObj.AddComponent<RectTransform>();
                innerRect.anchorMin = new Vector2(0.5f, 0.5f);
                innerRect.anchorMax = new Vector2(0.5f, 0.5f);
                float innerSize = Mathf.Max(4f, roomNodeSize - roomNodeFrameSize * 2f);
                innerRect.sizeDelta = new Vector2(innerSize, innerSize);
                innerRect.anchoredPosition = Vector2.zero;

                var img = innerObj.AddComponent<Image>();
                img.sprite = CreateSolidSprite();
                img.type = Image.Type.Simple;
                img.color = GetRoomColor(room.roomType);

                fallbackNodes[room.key] = img;
                fallbackNodeRects[room.key] = rect;
                fallbackRoomTypes[room.key] = room.roomType;
                fallbackRoomTransforms[room.key] = room.roomTransform;
                fallbackRoomCenters[room.key] = GetRoomCenter(room.roomTransform);
            }

            DrawFallbackConnections(rooms);
            currentTrackedFallbackRoom = null;
            UpdateCurrentRoomHighlight();
        }

        private void DrawFallbackConnections(List<(string key, Vector2Int gridPos, RoomType roomType, Transform roomTransform)> rooms)
        {
            HashSet<string> roomKeys = new HashSet<string>();
            for (int i = 0; i < rooms.Count; i++)
                roomKeys.Add(rooms[i].key);

            HashSet<string> processedConnections = new HashSet<string>();
            Vector2Int[] dirs =
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                if (!fallbackNodeRects.TryGetValue(room.key, out RectTransform fromRect))
                    continue;

                for (int d = 0; d < dirs.Length; d++)
                {
                    Vector2Int neighborPos = room.gridPos + dirs[d];
                    string neighborKey = $"{neighborPos.x}_{neighborPos.y}";
                    if (!roomKeys.Contains(neighborKey) || !fallbackNodeRects.TryGetValue(neighborKey, out RectTransform toRect))
                        continue;

                    string connKey = room.key.CompareTo(neighborKey) < 0
                        ? room.key + "-" + neighborKey
                        : neighborKey + "-" + room.key;
                    if (!processedConnections.Add(connKey))
                        continue;

                    Vector2 from = fromRect.anchoredPosition;
                    Vector2 to = toRect.anchoredPosition;
                    Vector2 delta = to - from;
                    float distance = delta.magnitude;
                    if (distance <= 0.001f)
                        continue;

                    var lineObj = new GameObject($"FallbackConnection_{connKey}");
                    lineObj.transform.SetParent(roomConnectionRoot, false);

                    var lineRect = lineObj.AddComponent<RectTransform>();
                    lineRect.anchorMin = new Vector2(0.5f, 0.5f);
                    lineRect.anchorMax = new Vector2(0.5f, 0.5f);
                    lineRect.sizeDelta = new Vector2(distance, connectionThickness);
                    lineRect.anchoredPosition = (from + to) * 0.5f;
                    lineRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

                    var lineImage = lineObj.AddComponent<Image>();
                    lineImage.sprite = CreateSolidSprite();
                    lineImage.color = connectionColor;
                }
            }
        }

        private bool TryParseRoomFromName(string roomName, out RoomType roomType, out Vector2Int gridPos)
        {
            roomType = RoomType.Archetype1;
            gridPos = Vector2Int.zero;

            if (string.IsNullOrEmpty(roomName) || !roomName.StartsWith("Room_"))
                return false;

            string[] parts = roomName.Split('_');
            if (parts.Length < 4)
                return false;

            if (!System.Enum.TryParse(parts[1], true, out roomType))
                roomType = RoomType.Archetype1;

            if (!int.TryParse(parts[2], out int x) || !int.TryParse(parts[3], out int y))
                return false;

            gridPos = new Vector2Int(x, y);
            return true;
        }

        private Vector3 GetRoomCenter(Transform roomTransform)
        {
            if (roomTransform == null)
                return Vector3.zero;

            var renderers = roomTransform.GetComponentsInChildren<Renderer>(true);
            if (renderers != null && renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                return bounds.center;
            }

            return roomTransform.position;
        }

        private string BuildLayoutSignature(List<Room> rooms)
        {
            var sorted = new List<Room>(rooms);
            sorted.Sort((a, b) =>
            {
                int xCompare = a.gridPosition.x.CompareTo(b.gridPosition.x);
                if (xCompare != 0) return xCompare;
                int yCompare = a.gridPosition.y.CompareTo(b.gridPosition.y);
                if (yCompare != 0) return yCompare;

                RoomType aType = a.roomData != null ? a.roomData.roomType : RoomType.Archetype1;
                RoomType bType = b.roomData != null ? b.roomData.roomType : RoomType.Archetype1;
                return aType.CompareTo(bType);
            });

            System.Text.StringBuilder sb = new System.Text.StringBuilder(sorted.Count * 10 + 8);
            sb.Append(sorted.Count);
            for (int i = 0; i < sorted.Count; i++)
            {
                var room = sorted[i];
                RoomType roomType = room.roomData != null ? room.roomData.roomType : RoomType.Archetype1;
                sb.Append('|');
                sb.Append(room.gridPosition.x);
                sb.Append(',');
                sb.Append(room.gridPosition.y);
                sb.Append(',');
                sb.Append((int)roomType);
            }
            return sb.ToString();
        }

        private void BuildRoomLayoutMinimap(List<Room> rooms)
        {
            ClearRoomLayoutVisuals();

            if (rooms == null || rooms.Count == 0)
                return;

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            for (int i = 0; i < rooms.Count; i++)
            {
                Vector2Int gp = rooms[i].gridPosition;
                if (gp.x < minX) minX = gp.x;
                if (gp.x > maxX) maxX = gp.x;
                if (gp.y < minY) minY = gp.y;
                if (gp.y > maxY) maxY = gp.y;
            }

            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;

            float mapSize = minimapDiameter * (1f - mapPaddingRatio * 2f);
            float gridWidth = Mathf.Max(1f, maxX - minX + 1f);
            float gridHeight = Mathf.Max(1f, maxY - minY + 1f);
            float maxAxis = Mathf.Max(gridWidth, gridHeight);
            float adaptiveSpacing = maxAxis <= 1f ? roomSpacing : mapSize / maxAxis;
            float nodeSpacing = Mathf.Min(roomSpacing, adaptiveSpacing);

            for (int i = 0; i < rooms.Count; i++)
            {
                Room room = rooms[i];
                Vector2 roomPos = new Vector2(
                    (room.gridPosition.x - centerX) * nodeSpacing,
                    (room.gridPosition.y - centerY) * nodeSpacing);

                var nodeObj = new GameObject($"RoomNode_{room.gridPosition.x}_{room.gridPosition.y}", typeof(RectTransform));
                nodeObj.transform.SetParent(roomLayoutRoot, false);

                var rect = nodeObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(roomNodeSize, roomNodeSize);
                rect.anchoredPosition = roomPos;

                var frame = nodeObj.AddComponent<Image>();
                frame.sprite = CreateSolidSprite();
                frame.type = Image.Type.Simple;
                frame.color = roomNodeFrameColor;

                var innerObj = new GameObject("Fill");
                innerObj.transform.SetParent(nodeObj.transform, false);
                var innerRect = innerObj.AddComponent<RectTransform>();
                innerRect.anchorMin = new Vector2(0.5f, 0.5f);
                innerRect.anchorMax = new Vector2(0.5f, 0.5f);
                float innerSize = Mathf.Max(4f, roomNodeSize - roomNodeFrameSize * 2f);
                innerRect.sizeDelta = new Vector2(innerSize, innerSize);
                innerRect.anchoredPosition = Vector2.zero;

                var img = innerObj.AddComponent<Image>();
                img.sprite = CreateSolidSprite();
                img.type = Image.Type.Simple;
                img.color = GetRoomColor(room);

                roomNodes[room] = img;
                roomNodeRects[room] = rect;
            }

            DrawRoomConnections(rooms);
            currentTrackedRoom = null;
            UpdateCurrentRoomHighlight();
        }

        private void DrawRoomConnections(List<Room> rooms)
        {
            HashSet<string> processedConnections = new HashSet<string>();

            for (int i = 0; i < rooms.Count; i++)
            {
                Room room = rooms[i];
                if (!roomNodeRects.TryGetValue(room, out RectTransform fromRect))
                    continue;

                foreach (var pair in room.connectedRooms)
                {
                    Room target = pair.Value;
                    if (target == null || !roomNodeRects.TryGetValue(target, out RectTransform toRect))
                        continue;

                    string key = BuildConnectionKey(room, target);
                    if (!processedConnections.Add(key))
                        continue;

                    Vector2 from = fromRect.anchoredPosition;
                    Vector2 to = toRect.anchoredPosition;
                    Vector2 delta = to - from;
                    float distance = delta.magnitude;

                    if (distance <= 0.001f)
                        continue;

                    var lineObj = new GameObject($"RoomConnection_{key}");
                    lineObj.transform.SetParent(roomConnectionRoot, false);

                    var lineRect = lineObj.AddComponent<RectTransform>();
                    lineRect.anchorMin = new Vector2(0.5f, 0.5f);
                    lineRect.anchorMax = new Vector2(0.5f, 0.5f);
                    lineRect.sizeDelta = new Vector2(distance, connectionThickness);
                    lineRect.anchoredPosition = (from + to) * 0.5f;
                    lineRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

                    var lineImage = lineObj.AddComponent<Image>();
                    lineImage.sprite = CreateSolidSprite();
                    lineImage.color = connectionColor;
                }
            }
        }

        private string BuildConnectionKey(Room a, Room b)
        {
            int ax = a.gridPosition.x;
            int ay = a.gridPosition.y;
            int bx = b.gridPosition.x;
            int by = b.gridPosition.y;

            if (ax < bx || (ax == bx && ay <= by))
                return $"{ax}_{ay}-{bx}_{by}";

            return $"{bx}_{by}-{ax}_{ay}";
        }

        private void UpdateCurrentRoomHighlight()
        {
            if (!useRoomLayoutMinimap)
                return;

            if (usingFallbackLayout)
            {
                UpdateFallbackCurrentRoomHighlight();
                return;
            }

            if (roomNodes.Count == 0)
                return;

            Room resolvedCurrentRoom = ResolveCurrentRoom();

            if (resolvedCurrentRoom == currentTrackedRoom)
                return;

            currentTrackedRoom = resolvedCurrentRoom;

            foreach (var pair in roomNodes)
            {
                bool isCurrent = pair.Key == currentTrackedRoom;
                pair.Value.color = isCurrent ? currentRoomColor : GetRoomColor(pair.Key);
                pair.Value.rectTransform.localScale = isCurrent ? Vector3.one * 1.2f : Vector3.one;
            }
        }

        private Room ResolveCurrentRoom()
        {
            if (RoomTransitionManager.Instance != null)
            {
                Room current = RoomTransitionManager.Instance.GetCurrentRoom();
                if (current != null && roomNodes.ContainsKey(current))
                    return current;
            }

            if (playerTransform == null)
                return null;

            Room best = null;
            float bestDistance = float.MaxValue;

            foreach (var pair in roomNodes)
            {
                Room room = pair.Key;
                if (room == null || room.roomInstance == null)
                    continue;

                Vector3 center = room.roomInstance.transform.position +
                                 new Vector3(room.actualSize.x * room.worldScale * 0.5f,
                                             room.actualSize.y * room.worldScale * 0.5f,
                                             0f);

                float dist = Vector2.Distance(playerTransform.position, center);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    best = room;
                }
            }

            return best;
        }

        private Color GetRoomColor(Room room)
        {
            if (room == null || room.roomData == null)
                return roomDefaultColor;

            switch (room.roomData.roomType)
            {
                case RoomType.Start:
                    return startRoomColor;
                case RoomType.Goal:
                    return goalRoomColor;
                case RoomType.Boss:
                case RoomType.MidBoss:
                    return bossRoomColor;
                default:
                    return roomDefaultColor;
            }
        }

        private Color GetRoomColor(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Start:
                    return startRoomColor;
                case RoomType.Goal:
                    return goalRoomColor;
                case RoomType.Boss:
                case RoomType.MidBoss:
                    return bossRoomColor;
                default:
                    return roomDefaultColor;
            }
        }

        private void UpdateFallbackCurrentRoomHighlight()
        {
            if (fallbackNodes.Count == 0)
                return;

            string resolvedCurrentKey = ResolveCurrentFallbackRoomKey();
            if (resolvedCurrentKey == currentTrackedFallbackRoom)
                return;

            currentTrackedFallbackRoom = resolvedCurrentKey;

            foreach (var pair in fallbackNodes)
            {
                bool isCurrent = pair.Key == currentTrackedFallbackRoom;
                RoomType roomType = fallbackRoomTypes.TryGetValue(pair.Key, out var value)
                    ? value
                    : RoomType.Archetype1;

                pair.Value.color = isCurrent ? currentRoomColor : GetRoomColor(roomType);
                pair.Value.rectTransform.localScale = isCurrent ? Vector3.one * 1.2f : Vector3.one;
            }
        }

        private string ResolveCurrentFallbackRoomKey()
        {
            foreach (var pair in fallbackRoomTransforms)
            {
                if (pair.Value != null && pair.Value.gameObject.activeSelf)
                    return pair.Key;
            }

            if (playerTransform == null)
                return null;

            string bestKey = null;
            float bestDistance = float.MaxValue;
            foreach (var pair in fallbackRoomCenters)
            {
                float distance = Vector2.Distance(playerTransform.position, pair.Value);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestKey = pair.Key;
                }
            }

            return bestKey;
        }

        private void ClearRoomLayoutVisuals()
        {
            roomNodes.Clear();
            roomNodeRects.Clear();
            fallbackNodes.Clear();
            fallbackNodeRects.Clear();
            fallbackRoomCenters.Clear();
            fallbackRoomTypes.Clear();
            fallbackRoomTransforms.Clear();
            currentTrackedFallbackRoom = null;

            if (roomConnectionRoot != null)
            {
                for (int i = roomConnectionRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(roomConnectionRoot.GetChild(i).gameObject);
                }
            }

            if (roomLayoutRoot != null)
            {
                for (int i = roomLayoutRoot.childCount - 1; i >= 0; i--)
                {
                    Transform child = roomLayoutRoot.GetChild(i);
                    if (child == roomConnectionRoot)
                        continue;

                    Destroy(child.gameObject);
                }
            }
        }

        #endregion

        #region Update Logic

        private DungeonManager ResolveDungeonManagerWithRooms()
        {
            DungeonManager fallback = dungeonManager;

            var managers = FindObjectsByType<DungeonManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < managers.Length; i++)
            {
                var manager = managers[i];
                if (manager == null)
                    continue;

                if (fallback == null)
                    fallback = manager;

                var rooms = manager.GetAllRooms();
                if (rooms != null && rooms.Count > 0)
                    return manager;
            }

            return fallback;
        }

        private void UpdateRoundMapLabel()
        {
            if (roundMapLabel == null)
                return;

            var progressionManager = FindFirstObjectByType<ProceduralGeneration.Integration.DungeonRunProgressionManager>();
            if (progressionManager == null)
            {
                roundMapLabel.text = "?-?";
                return;
            }

            int round = progressionManager.CurrentRound;
            int map = progressionManager.CurrentMap;
            roundMapLabel.text = $"{round}-{map}";
        }

        private void UpdateCameraPosition()
        {
            if (minimapCamera == null) return;

            Vector3 target = new Vector3(
                playerTransform.position.x,
                playerTransform.position.y,
                -50f);

            minimapCamera.transform.position = Vector3.Lerp(
                minimapCamera.transform.position,
                target,
                cameraSmoothSpeed * Time.deltaTime);
        }

        #endregion

        #region Fragment Events

        /// <summary>
        /// Khi nhặt fragment → minimap sáng hơn, rộng hơn
        /// </summary>
        private void OnFragmentCollected(int current, int total)
        {
            if (minimapLight != null)
            {
                minimapLight.intensity = minimapLightIntensity + current * 0.15f;
                minimapLight.pointLightOuterRadius = cameraOrthoSize * 2.5f + current * 5f;
            }
            Debug.Log($"[MinimapManager] Fragment {current}/{total} → minimap light updated");
        }

        /// <summary>
        /// Full brightness khi nhặt đủ fragments
        /// </summary>
        public void RevealAllRooms()
        {
            if (minimapLight != null)
            {
                minimapLight.intensity = 1.0f;
                minimapLight.pointLightOuterRadius = cameraOrthoSize * 4f;
                minimapLight.pointLightInnerRadius = cameraOrthoSize * 3f;
            }
            Debug.Log("[MinimapManager] All rooms revealed!");
        }

        #endregion

        #region Utility

        private Sprite CreateCircleSprite(int res)
        {
            if (_cachedCircle != null) return _cachedCircle;

            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            float c = res * 0.5f;
            float r = c - 1f;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - dist + 0.5f)));
                }
            }

            tex.Apply();
            _cachedCircle = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
            return _cachedCircle;
        }

        private Sprite CreateSolidSprite()
        {
            if (_cachedSolid != null) return _cachedSolid;

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.SetPixel(1, 0, Color.white);
            tex.SetPixel(0, 1, Color.white);
            tex.SetPixel(1, 1, Color.white);
            tex.Apply();

            _cachedSolid = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f);
            return _cachedSolid;
        }

        #endregion
    }
}
