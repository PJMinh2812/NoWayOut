using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using System.Collections;
using TMPro;

namespace NWO.UI
{
    /// <summary>
    /// Circular minimap using a second Camera + RenderTexture.
    /// Shows the REAL tilemap/scene from above inside a circular frame.
    /// The minimap camera follows the player and renders to a RawImage clipped by a circle mask.
    /// </summary>
    public class MinimapManager : MonoBehaviour
    {
        [Header("Minimap Settings")]
        [SerializeField] private bool showMinimapOnStart = true;
        [SerializeField] private float minimapDiameter = 200f;
        [SerializeField] private Vector2 minimapOffset = new Vector2(-16f, -16f);
        [SerializeField] private int renderTextureSize = 512;

        [Header("Camera")]
        [Tooltip("How much world area the minimap camera sees (orthographic half-size)")]
        [SerializeField] private float cameraOrthoSize = 25f;
        [Tooltip("Smooth follow speed for minimap camera")]
        [SerializeField] private float cameraSmoothSpeed = 8f;

        [Header("Minimap Light")]
        [Tooltip("Extra light intensity for minimap camera so the map is visible")]
        [SerializeField] private float minimapLightIntensity = 0.35f;
        [SerializeField] private Color minimapLightColor = new Color(0.3f, 0.35f, 0.5f);

        [Header("Frame Colors")]
        [SerializeField] private Color frameBgColor = new Color(0.03f, 0.03f, 0.06f, 0.92f);
        [SerializeField] private Color ringColor = new Color(0.4f, 0.45f, 0.65f, 0.8f);
        [SerializeField] private Color ringGlowColor = new Color(0.45f, 0.5f, 0.75f, 0.2f);

        [Header("Player Marker")]
        [SerializeField] private Color playerMarkerColor = new Color(0f, 0.95f, 1f, 1f);
        [SerializeField] private float playerMarkerSize = 12f;

        // Internal references
        private Canvas minimapCanvas;
        private CanvasGroup minimapCanvasGroup;
        private RawImage mapRawImage;
        private Image playerMarkerImage;
        private Camera minimapCamera;
        private RenderTexture minimapRT;
        private Light2D minimapLight;
        private Transform playerTransform;
        private bool isMinimapVisible = true;
        private bool isInitialized = false;

        // Sweep line
        private RectTransform sweepRect;

        public static MinimapManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start() => StartCoroutine(InitializeWhenReady());

        private void Update()
        {
            // Toggle with Tab
            var kb = Keyboard.current;
            if (kb != null && kb.tabKey.wasPressedThisFrame) ToggleMinimap();

            if (!isInitialized || playerTransform == null) return;

            // Smooth follow player
            UpdateCameraPosition();

            // Rotate sweep line
            if (sweepRect != null)
                sweepRect.Rotate(0, 0, -25f * Time.deltaTime);
        }

        // ───────────────────── INITIALIZATION ─────────────────────

        private IEnumerator InitializeWhenReady()
        {
            yield return null;
            yield return null;

            // Wait for player
            float elapsed = 0f;
            while (playerTransform == null && elapsed < 5f)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) playerTransform = p.transform;
                else { yield return new WaitForSeconds(0.3f); elapsed += 0.3f; }
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("[MinimapManager] Player not found, minimap disabled.");
                yield break;
            }

            // Create everything
            CreateRenderTexture();
            CreateMinimapCamera();
            CreateMinimapUI();

            isInitialized = true;
            isMinimapVisible = showMinimapOnStart;
            SetMinimapVisibility(isMinimapVisible);

            Debug.Log("[MinimapManager] ★ Camera-based circular minimap ready!");
        }

        // ───────────────────── RENDER TEXTURE ─────────────────────

        private void CreateRenderTexture()
        {
            minimapRT = new RenderTexture(renderTextureSize, renderTextureSize, 24, RenderTextureFormat.ARGB32);
            minimapRT.antiAliasing = 2;
            minimapRT.filterMode = FilterMode.Bilinear;
            minimapRT.Create();
        }

        // ───────────────────── MINIMAP CAMERA ─────────────────────

        private void CreateMinimapCamera()
        {
            var camObj = new GameObject("MinimapCamera");
            camObj.transform.SetParent(transform);

            // Position above player
            Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
            camObj.transform.position = new Vector3(playerPos.x, playerPos.y, -50f);

            minimapCamera = camObj.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = cameraOrthoSize;
            minimapCamera.targetTexture = minimapRT;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.02f, 0.02f, 0.04f, 1f);
            minimapCamera.depth = -10; // Render before main camera
            minimapCamera.cullingMask = ~(1 << 5); // Everything except UI layer

            // URP: add UniversalAdditionalCameraData
            var urpData = camObj.GetComponent<UniversalAdditionalCameraData>();
            if (urpData == null) urpData = camObj.AddComponent<UniversalAdditionalCameraData>();
            urpData.renderType = CameraRenderType.Base;

            // Add a dedicated light so the minimap isn't pitch-black
            CreateMinimapLight(camObj.transform);
        }

        private void CreateMinimapLight(Transform parent)
        {
            // Use a large Point light instead of Global to avoid leaking light to the main camera.
            // The point light follows the minimap camera (which follows the player),
            // illuminating the area visible on the minimap without affecting the main dark scene.
            var lightObj = new GameObject("MinimapAreaLight");
            lightObj.transform.SetParent(parent, false);
            lightObj.transform.localPosition = Vector3.zero;

            minimapLight = lightObj.AddComponent<Light2D>();
            minimapLight.lightType = Light2D.LightType.Point;
            minimapLight.pointLightOuterRadius = cameraOrthoSize * 2.5f;
            minimapLight.pointLightInnerRadius = cameraOrthoSize * 1.5f;
            minimapLight.pointLightInnerAngle = 360f;
            minimapLight.pointLightOuterAngle = 360f;
            minimapLight.intensity = minimapLightIntensity;
            minimapLight.color = minimapLightColor;
            minimapLight.falloffIntensity = 0.3f;
        }

        private void UpdateCameraPosition()
        {
            if (minimapCamera == null || playerTransform == null) return;
            Vector3 target = new Vector3(playerTransform.position.x, playerTransform.position.y, -50f);
            minimapCamera.transform.position = Vector3.Lerp(
                minimapCamera.transform.position, target, cameraSmoothSpeed * Time.deltaTime);
        }

        // ───────────────────── UI SETUP ─────────────────────

        private void CreateMinimapUI()
        {
            float d = minimapDiameter;
            float ringWidth = 3f;

            // ── Canvas ──
            var canvasObj = new GameObject("MinimapCanvas");
            canvasObj.transform.SetParent(transform);
            minimapCanvas = canvasObj.AddComponent<Canvas>();
            minimapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            minimapCanvas.sortingOrder = 100;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // ── Frame (main container — CanvasGroup here) ──
            var frameObj = MakeUI("MinimapFrame", canvasObj.transform);
            var frameRT = AnchorTopRight(frameObj, d, d, minimapOffset);
            var frameBg = frameObj.AddComponent<Image>();
            frameBg.sprite = CreateCircleSprite(128);
            frameBg.color = frameBgColor;
            frameBg.raycastTarget = true;
            minimapCanvasGroup = frameObj.AddComponent<CanvasGroup>();

            // ── Circle Mask (clips content to circle) ──
            var maskObj = MakeUI("CircleMask", frameObj.transform);
            var maskRT = maskObj.AddComponent<RectTransform>();
            maskRT.anchorMin = Vector2.zero; maskRT.anchorMax = Vector2.one;
            maskRT.offsetMin = new Vector2(ringWidth + 2, ringWidth + 2);
            maskRT.offsetMax = new Vector2(-ringWidth - 2, -ringWidth - 2);
            var maskImg = maskObj.AddComponent<Image>();
            maskImg.sprite = CreateCircleSprite(128);
            maskImg.color = Color.white;
            maskImg.raycastTarget = false;
            var mask = maskObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // ── RawImage (shows the RenderTexture inside the mask) ──
            var rawObj = MakeUI("MapView", maskObj.transform);
            var rawRT = rawObj.AddComponent<RectTransform>();
            rawRT.anchorMin = Vector2.zero; rawRT.anchorMax = Vector2.one;
            rawRT.offsetMin = Vector2.zero; rawRT.offsetMax = Vector2.zero;
            mapRawImage = rawObj.AddComponent<RawImage>();
            mapRawImage.texture = minimapRT;
            mapRawImage.color = Color.white;
            mapRawImage.raycastTarget = false;

            // ── Player marker dot (center of minimap, always visible) ──
            var markerObj = MakeUI("PlayerMarker", maskObj.transform);
            var markerRT = markerObj.AddComponent<RectTransform>();
            markerRT.anchorMin = new Vector2(0.5f, 0.5f);
            markerRT.anchorMax = new Vector2(0.5f, 0.5f);
            markerRT.pivot = new Vector2(0.5f, 0.5f);
            markerRT.sizeDelta = new Vector2(playerMarkerSize, playerMarkerSize);
            markerRT.anchoredPosition = Vector2.zero;
            playerMarkerImage = markerObj.AddComponent<Image>();
            playerMarkerImage.sprite = CreateCircleSprite(64);
            playerMarkerImage.color = playerMarkerColor;
            playerMarkerImage.raycastTarget = false;

            // ── Player marker glow (larger, softer) ──
            var glowMarkerObj = MakeUI("MarkerGlow", maskObj.transform);
            var glowMarkerRT = glowMarkerObj.AddComponent<RectTransform>();
            glowMarkerRT.anchorMin = new Vector2(0.5f, 0.5f);
            glowMarkerRT.anchorMax = new Vector2(0.5f, 0.5f);
            glowMarkerRT.pivot = new Vector2(0.5f, 0.5f);
            glowMarkerRT.sizeDelta = new Vector2(playerMarkerSize * 2.5f, playerMarkerSize * 2.5f);
            glowMarkerRT.anchoredPosition = Vector2.zero;
            var glowMarkerImg = glowMarkerObj.AddComponent<Image>();
            glowMarkerImg.sprite = CreateCircleSprite(64);
            glowMarkerImg.color = new Color(playerMarkerColor.r, playerMarkerColor.g, playerMarkerColor.b, 0.15f);
            glowMarkerImg.raycastTarget = false;
            // Move glow behind marker
            glowMarkerObj.transform.SetSiblingIndex(markerObj.transform.GetSiblingIndex());

            // ── Radar sweep line ──
            var sweepObj = MakeUI("Sweep", maskObj.transform);
            sweepRect = sweepObj.AddComponent<RectTransform>();
            sweepRect.anchorMin = new Vector2(0.5f, 0.5f);
            sweepRect.anchorMax = new Vector2(0.5f, 0.5f);
            sweepRect.pivot = new Vector2(0.5f, 0f);
            sweepRect.sizeDelta = new Vector2(2f, d * 0.45f);
            sweepRect.anchoredPosition = Vector2.zero;
            var sweepImg = sweepObj.AddComponent<Image>();
            sweepImg.color = new Color(0.4f, 0.6f, 1f, 0.08f);
            sweepImg.raycastTarget = false;

            // ── Ring border removed per user request ──

            // ── Compass labels ──
            AddCompass(frameObj.transform, "N", new Vector2(0.5f, 1f), new Vector2(0, -4f));
            AddCompass(frameObj.transform, "S", new Vector2(0.5f, 0f), new Vector2(0, 4f));
            AddCompass(frameObj.transform, "E", new Vector2(1f, 0.5f), new Vector2(-6f, 0));
            AddCompass(frameObj.transform, "W", new Vector2(0f, 0.5f), new Vector2(6f, 0));

            // ── "MAP" title ──
            var titleObj = MakeUI("Title", frameObj.transform);
            var titleRT = titleObj.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f);
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 0f);
            titleRT.anchoredPosition = new Vector2(0, 6f);
            titleRT.sizeDelta = new Vector2(60, 16);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "MAP";
            titleTmp.fontSize = 10f;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.55f, 0.6f, 0.8f, 0.7f);
            titleTmp.fontStyle = FontStyles.Bold;

            // ── Tab hint ──
            var hintObj = MakeUI("Hint", frameObj.transform);
            var hintRT = hintObj.AddComponent<RectTransform>();
            hintRT.anchorMin = new Vector2(0.5f, 0f);
            hintRT.anchorMax = new Vector2(0.5f, 0f);
            hintRT.pivot = new Vector2(0.5f, 1f);
            hintRT.anchoredPosition = new Vector2(0, -4f);
            hintRT.sizeDelta = new Vector2(80, 14);
            var hintTmp = hintObj.AddComponent<TextMeshProUGUI>();
            hintTmp.text = "[TAB]";
            hintTmp.fontSize = 8f;
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.color = new Color(0.45f, 0.5f, 0.65f, 0.45f);
        }

        // ───────────────────── UI HELPERS ─────────────────────

        private GameObject MakeUI(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = 5;
            return go;
        }

        private RectTransform AnchorTopRight(GameObject go, float w, float h, Vector2 offset)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = offset;
            return rt;
        }

        private void AddCompass(Transform parent, string label, Vector2 anchor, Vector2 offset)
        {
            var go = MakeUI($"Compass_{label}", parent);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(16, 14);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 8f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.5f, 0.55f, 0.7f, 0.45f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
        }

        // ───────────────────── SPRITE GENERATION ─────────────────────

        private static Sprite _cachedCircle;
        private Sprite CreateCircleSprite(int res)
        {
            if (_cachedCircle != null) return _cachedCircle;

            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float c = res * 0.5f;
            float r = c - 1f;

            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - dist + 0.5f)));
                }
            tex.Apply();
            _cachedCircle = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
            return _cachedCircle;
        }

        private static Sprite _cachedRing;
        private Sprite CreateRingSprite(int res, float thickness)
        {
            if (_cachedRing != null) return _cachedRing;

            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float c = res * 0.5f;
            float outerR = c - 1f;
            float innerR = outerR * (1f - Mathf.Clamp(thickness, 0.03f, 0.3f));

            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    float outer = Mathf.Clamp01(outerR - dist + 0.5f);
                    float inner = Mathf.Clamp01(dist - innerR + 0.5f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, outer * inner));
                }
            tex.Apply();
            _cachedRing = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
            return _cachedRing;
        }

        // ───────────────────── PUBLIC API ─────────────────────

        public void ToggleMinimap()
        {
            isMinimapVisible = !isMinimapVisible;
            SetMinimapVisibility(isMinimapVisible);
        }

        private void SetMinimapVisibility(bool visible)
        {
            if (minimapCanvasGroup != null)
            {
                minimapCanvasGroup.alpha = visible ? 1f : 0f;
                minimapCanvasGroup.interactable = visible;
                minimapCanvasGroup.blocksRaycasts = visible;
            }
            if (minimapCamera != null)
                minimapCamera.enabled = visible;
        }

        /// <summary>
        /// Zoom in/out the minimap view.
        /// </summary>
        public void SetZoom(float orthoSize)
        {
            cameraOrthoSize = Mathf.Clamp(orthoSize, 8f, 60f);
            if (minimapCamera != null)
                minimapCamera.orthographicSize = cameraOrthoSize;
        }

        /// <summary>
        /// Increase minimap light when fragments are collected (map becomes clearer).
        /// </summary>
        public void OnFragmentCollected(int totalFragments)
        {
            if (minimapLight != null)
            {
                // Each fragment makes the minimap brighter and wider
                minimapLight.intensity = minimapLightIntensity + totalFragments * 0.15f;
                minimapLight.pointLightOuterRadius = cameraOrthoSize * 2.5f + totalFragments * 5f;
            }
        }

        // Legacy API compatibility
        public void MarkFragmentRoom(Vector2Int coord) { }
        public void RevealAllRooms()
        {
            // Make minimap fully bright and cover entire view
            if (minimapLight != null)
            {
                minimapLight.intensity = 1.0f;
                minimapLight.pointLightOuterRadius = cameraOrthoSize * 4f;
                minimapLight.pointLightInnerRadius = cameraOrthoSize * 3f;
            }
        }

        private void OnDestroy()
        {
            if (minimapRT != null)
            {
                minimapRT.Release();
                Destroy(minimapRT);
            }
        }
    }
}
