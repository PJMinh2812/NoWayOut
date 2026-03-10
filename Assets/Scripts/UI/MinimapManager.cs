using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

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

        // Runtime references
        private Camera minimapCamera;
        private RenderTexture minimapRT;
        private Light2D minimapLight;
        private Canvas minimapCanvas;
        private RawImage mapRawImage;
        private Image playerMarkerImage;
        private RectTransform sweepRect;
        private Transform playerTransform;
        private CanvasGroup minimapCanvasGroup;
        private bool isInitialized = false;

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
            Debug.Log("[MinimapManager] Minimap initialized successfully!");
        }

        private void Update()
        {
            // TAB key toggle minimap
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame)
                ToggleMinimap();

            if (!isInitialized || playerTransform == null) return;

            UpdateCameraPosition();
            AnimateRadarSweep();
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

            // ── PLAYER MARKER (cyan dot, center) ──
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

            // ── MARKER GLOW ──
            var glowObj = new GameObject("MarkerGlow");
            glowObj.transform.SetParent(maskObj.transform, false);
            var glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(playerMarkerSize * 2.5f, playerMarkerSize * 2.5f);
            glowRect.anchoredPosition = Vector2.zero;
            // Glow behind marker
            glowObj.transform.SetSiblingIndex(markerObj.transform.GetSiblingIndex());

            var glowImg = glowObj.AddComponent<Image>();
            glowImg.sprite = CreateCircleSprite(64);
            glowImg.color = new Color(playerMarkerColor.r, playerMarkerColor.g, playerMarkerColor.b, 0.15f);

            // ── RADAR SWEEP LINE ──
            var sweepObj = new GameObject("RadarSweep");
            sweepObj.transform.SetParent(maskObj.transform, false);
            sweepRect = sweepObj.AddComponent<RectTransform>();
            sweepRect.anchorMin = new Vector2(0.5f, 0.5f);
            sweepRect.anchorMax = new Vector2(0.5f, 0.5f);
            sweepRect.pivot = new Vector2(0.5f, 0f); // Pivot ở đáy → xoay quanh tâm
            sweepRect.sizeDelta = new Vector2(2f, d * 0.45f);
            sweepRect.anchoredPosition = Vector2.zero;

            var sweepImg = sweepObj.AddComponent<Image>();
            sweepImg.color = new Color(0.4f, 0.6f, 1f, 0.08f);

            // ── COMPASS LABELS (N/S/E/W) ──
            CreateCompassLabel(maskObj.transform, "N", new Vector2(0.5f, 1f), new Vector2(0, -6f));
            CreateCompassLabel(maskObj.transform, "S", new Vector2(0.5f, 0f), new Vector2(0, 6f));
            CreateCompassLabel(maskObj.transform, "E", new Vector2(1f, 0.5f), new Vector2(-8f, 0));
            CreateCompassLabel(maskObj.transform, "W", new Vector2(0f, 0.5f), new Vector2(8f, 0));
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
            tmp.enableWordWrapping = false;
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
            tmp.enableWordWrapping = false;
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

        #region Update Logic

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

        private void AnimateRadarSweep()
        {
            if (sweepRect != null)
                sweepRect.Rotate(0, 0, -25f * Time.deltaTime);
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

        #endregion
    }
}
