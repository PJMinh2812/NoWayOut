using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NWO
{
    /// <summary>
    /// Minimal async scene loader with a runtime-built loading UI (no prefab/scene needed).
    /// Call <see cref="LoadScene"/> instead of SceneManager.LoadScene.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        private static SceneLoader _instance;

        [Header("Loading UI")]
        [SerializeField] private float minVisibleSeconds = 0.4f;
        [SerializeField] private string loadingLabel = "LOADING...";
        [SerializeField] private string blockingLabel = "LOADING...";
        [SerializeField] private float indeterminateSpeed = 1.25f;

        private Canvas _canvas;
        private Image _progressFill;
        private TextMeshProUGUI _label;

        private int _blockCount;
        private bool _isLoadingScene;
        private bool _indeterminate;
        private float _indeterminateT;

        public static void LoadScene(string sceneName)
        {
            EnsureInstance();
            _instance.StartCoroutine(_instance.LoadSceneCoroutine(sceneName));
        }

        /// <summary>
        /// Show loading overlay while you do work after the scene is loaded (e.g. apply save data).
        /// Call <see cref="EndBlocking"/> when finished.
        /// </summary>
        public static void BeginBlocking(string label = null)
        {
            EnsureInstance();
            _instance.BuildUiIfNeeded();
            _instance._blockCount++;
            _instance._indeterminate = true;
            _instance._indeterminateT = 0f;
            _instance.SetUiVisible(true);
            _instance.SetLabel(string.IsNullOrWhiteSpace(label) ? _instance.blockingLabel : label);
        }

        public static void EndBlocking()
        {
            if (_instance == null) return;
            if (_instance._blockCount > 0) _instance._blockCount--;
            if (_instance._blockCount == 0)
            {
                _instance._indeterminate = false;
                if (!_instance._isLoadingScene)
                    _instance.SetUiVisible(false);
            }
        }

        private static void EnsureInstance()
        {
            if (_instance != null) return;

            var go = new GameObject("SceneLoader");
            _instance = go.AddComponent<SceneLoader>();
            DontDestroyOnLoad(go);
        }

        private void Update()
        {
            if (_canvas == null || !_canvas.gameObject.activeSelf) return;
            if (!_indeterminate) return;

            _indeterminateT += Time.unscaledDeltaTime * Mathf.Max(0.01f, indeterminateSpeed);
            var v = Mathf.PingPong(_indeterminateT, 1f);
            SetProgress(Mathf.Lerp(0.15f, 0.90f, v));
        }

        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            BuildUiIfNeeded();
            _isLoadingScene = true;
            _indeterminate = false;
            SetUiVisible(true);
            SetProgress(0f);
            SetLabel(loadingLabel);

            var startTime = Time.unscaledTime;

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"[SceneLoader] LoadSceneAsync returned null for scene '{sceneName}'. Is it added to Build Settings?");
                SetUiVisible(false);
                yield break;
            }

            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                var normalized = Mathf.Clamp01(op.progress / 0.9f);
                SetProgress(normalized);
                yield return null;
            }

            SetProgress(1f);

            var remaining = minVisibleSeconds - (Time.unscaledTime - startTime);
            if (remaining > 0f)
                yield return new WaitForSecondsRealtime(remaining);

            op.allowSceneActivation = true;
            while (!op.isDone)
                yield return null;

            _isLoadingScene = false;
            if (_blockCount <= 0)
                SetUiVisible(false);
        }

        private void BuildUiIfNeeded()
        {
            if (_canvas != null) return;

            var canvasGo = new GameObject("LoadingCanvas");
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = short.MaxValue;

            canvasGo.AddComponent<GraphicRaycaster>();

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.03f, 0.05f, 0.92f);
            StretchToFull(bgGo.GetComponent<RectTransform>());

            var containerGo = new GameObject("Container");
            containerGo.transform.SetParent(bgGo.transform, false);
            var containerRt = containerGo.AddComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.pivot = new Vector2(0.5f, 0.5f);
            containerRt.sizeDelta = new Vector2(900f, 220f);
            containerRt.anchoredPosition = new Vector2(0f, -120f);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(containerGo.transform, false);
            _label = labelGo.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 54;
            _label.color = new Color(0.95f, 0.95f, 0.98f, 1f);
            var labelRt = _label.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot = new Vector2(0.5f, 1f);
            labelRt.sizeDelta = new Vector2(0f, 80f);
            labelRt.anchoredPosition = new Vector2(0f, 0f);

            var barGo = new GameObject("ProgressBar");
            barGo.transform.SetParent(containerGo.transform, false);
            var barBg = barGo.AddComponent<Image>();
            barBg.color = new Color(1f, 1f, 1f, 0.10f);
            var barRt = barGo.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.08f, 0f);
            barRt.anchorMax = new Vector2(0.92f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.sizeDelta = new Vector2(0f, 24f);
            barRt.anchoredPosition = new Vector2(0f, 40f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(barGo.transform, false);
            var fill = fillGo.AddComponent<Image>();
            fill.color = new Color(0.50f, 0.86f, 1.00f, 0.95f);
            _progressFill = fill;
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.sizeDelta = new Vector2(0f, 0f);
        }

        private static void StretchToFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void SetUiVisible(bool visible)
        {
            if (_canvas == null) return;
            _canvas.gameObject.SetActive(visible);
        }

        private void SetProgress(float value01)
        {
            if (_progressFill == null) return;
            var parent = _progressFill.transform.parent as RectTransform;
            if (parent == null) return;

            var parentWidth = parent.rect.width;
            var fillRt = _progressFill.rectTransform;
            fillRt.sizeDelta = new Vector2(parentWidth * Mathf.Clamp01(value01), 0f);
        }

        private void SetLabel(string text)
        {
            if (_label == null) return;
            _label.text = text ?? string.Empty;
        }
    }
}

