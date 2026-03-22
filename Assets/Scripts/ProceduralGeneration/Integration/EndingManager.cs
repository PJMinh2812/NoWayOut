using System.Collections;
using System.Collections.Generic;
using System.IO;
using NWO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ProceduralGeneration.Integration
{
    /// <summary>
    /// Plays ending video (good/bad) before loading ending scene.
    /// Attach this to MapSetUp and assign video clips in Inspector.
    /// </summary>
    public class EndingManager : MonoBehaviour
    {
        [Header("Ending Videos")]
        [SerializeField] private VideoClip goodEndingVideo;
        [SerializeField] private VideoClip badEndingVideo;

        [Header("Playback")]
        [SerializeField] private bool enableEndingVideo = true;
        [SerializeField] private bool allowSkip = true;
        [SerializeField] private Key skipKey = Key.Space;

        [Header("Post Ending")]
        [SerializeField] private bool returnToMainMenuAfterVideo = true;
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private bool clearSaveAndProgressAfterVideo = true;

        [Header("Audio")]
        [SerializeField] private AudioSource externalAudioSource;

        private const int OverlaySortOrder = 30000;

        private bool isPlaying;
        private bool isWaitingForFinish;
        private string pendingSceneName;
        private VideoPlayer runtimeVideoPlayer;
        private AudioSource runtimeVideoAudioSource;
        private RenderTexture runtimeRenderTexture;
        private GameObject overlayRoot;
        private readonly List<AudioSource> pausedAudioSources = new List<AudioSource>();
        private float previousTimeScale = 1f;

        public bool PlayEndingVideoThenLoad(bool isGoodEnding, string sceneToLoad)
        {
            if (!enableEndingVideo)
                return false;

            if (isPlaying)
                return true;

            if (string.IsNullOrWhiteSpace(sceneToLoad))
                return false;

            VideoClip clip = isGoodEnding ? goodEndingVideo : badEndingVideo;
            if (clip == null)
                return false;

            StartCoroutine(PlayAndLoadCoroutine(clip, sceneToLoad));
            return true;
        }

        private IEnumerator PlayAndLoadCoroutine(VideoClip clip, string sceneToLoad)
        {
            isPlaying = true;
            pendingSceneName = sceneToLoad;
            isWaitingForFinish = true;

            BuildOverlayIfNeeded();
            SetupVideoPlayerIfNeeded();

            runtimeVideoPlayer.Stop();
            runtimeVideoPlayer.clip = clip;
            runtimeVideoPlayer.isLooping = false;

            runtimeVideoPlayer.loopPointReached -= OnLoopPointReached;
            runtimeVideoPlayer.loopPointReached += OnLoopPointReached;

            runtimeVideoPlayer.Prepare();
            while (!runtimeVideoPlayer.isPrepared)
                yield return null;

            runtimeVideoPlayer.Play();
            EnterVideoPlaybackState();

            while (isWaitingForFinish)
            {
                if (allowSkip && WasSkipPressedThisFrame())
                    isWaitingForFinish = false;

                yield return null;
            }

            runtimeVideoPlayer.Stop();
            ExitVideoPlaybackState();
            CleanupOverlay();

            string targetScene = pendingSceneName;
            pendingSceneName = null;
            isPlaying = false;

            if (clearSaveAndProgressAfterVideo)
                ClearRunProgressAndSave();

            if (returnToMainMenuAfterVideo && !string.IsNullOrWhiteSpace(mainMenuSceneName))
                targetScene = mainMenuSceneName;

            SceneLoader.LoadScene(targetScene);
        }

        private void ClearRunProgressAndSave()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.DeleteSave();
            }
            else
            {
                string savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
                if (File.Exists(savePath))
                    File.Delete(savePath);
            }

            PlayerPrefs.SetInt("LoadFromSave", 0);
            PlayerPrefs.SetInt("RestoreDungeonFromSave", 0);
            PlayerPrefs.SetInt("GenerateNewMap", 1);
            PlayerPrefs.DeleteKey("LastDungeonSeed");
            PlayerPrefs.DeleteKey("RunEndingIsGood");
            PlayerPrefs.DeleteKey("RunEndingChestCount");
            PlayerPrefs.Save();
        }

        private void OnLoopPointReached(VideoPlayer _)
        {
            isWaitingForFinish = false;
        }

        private bool WasSkipPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            if (skipKey != Key.None && keyboard[skipKey].wasPressedThisFrame)
                return true;

            return keyboard.spaceKey.wasPressedThisFrame;
        }

        private void BuildOverlayIfNeeded()
        {
            if (overlayRoot != null)
                return;

            overlayRoot = new GameObject("EndingVideoOverlay");
            DontDestroyOnLoad(overlayRoot);

            var canvas = overlayRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortOrder;

            overlayRoot.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            overlayRoot.AddComponent<GraphicRaycaster>();

            var imageObject = new GameObject("VideoFrame");
            imageObject.transform.SetParent(overlayRoot.transform, false);

            var rawImage = imageObject.AddComponent<RawImage>();
            rawImage.color = Color.white;

            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            runtimeRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            runtimeRenderTexture.Create();
            rawImage.texture = runtimeRenderTexture;
        }

        private void SetupVideoPlayerIfNeeded()
        {
            if (runtimeVideoPlayer != null)
                return;

            var playerObject = new GameObject("EndingVideoPlayer");
            playerObject.transform.SetParent(overlayRoot.transform, false);

            runtimeVideoPlayer = playerObject.AddComponent<VideoPlayer>();
            runtimeVideoPlayer.playOnAwake = false;
            runtimeVideoPlayer.waitForFirstFrame = true;
            runtimeVideoPlayer.skipOnDrop = true;
            runtimeVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            runtimeVideoPlayer.targetTexture = runtimeRenderTexture;

            runtimeVideoAudioSource = externalAudioSource;
            if (runtimeVideoAudioSource == null)
                runtimeVideoAudioSource = playerObject.AddComponent<AudioSource>();

            runtimeVideoAudioSource.playOnAwake = false;
            runtimeVideoAudioSource.loop = false;
            runtimeVideoAudioSource.spatialBlend = 0f;

            if (runtimeVideoAudioSource != null)
            {
                runtimeVideoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                runtimeVideoPlayer.EnableAudioTrack(0, true);
                runtimeVideoPlayer.SetTargetAudioSource(0, runtimeVideoAudioSource);
            }
            else
            {
                runtimeVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            }
        }

        private void EnterVideoPlaybackState()
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            pausedAudioSources.Clear();
            AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allAudioSources.Length; i++)
            {
                AudioSource source = allAudioSources[i];
                if (source == null)
                    continue;

                if (runtimeVideoAudioSource != null && source == runtimeVideoAudioSource)
                    continue;

                if (source.isPlaying)
                {
                    source.Pause();
                    pausedAudioSources.Add(source);
                }
            }
        }

        private void ExitVideoPlaybackState()
        {
            Time.timeScale = previousTimeScale;

            for (int i = 0; i < pausedAudioSources.Count; i++)
            {
                AudioSource source = pausedAudioSources[i];
                if (source != null)
                    source.UnPause();
            }

            pausedAudioSources.Clear();
        }

        private void CleanupOverlay()
        {
            if (runtimeVideoPlayer != null)
            {
                runtimeVideoPlayer.loopPointReached -= OnLoopPointReached;
                Destroy(runtimeVideoPlayer.gameObject);
                runtimeVideoPlayer = null;
            }

            if (runtimeRenderTexture != null)
            {
                runtimeRenderTexture.Release();
                Destroy(runtimeRenderTexture);
                runtimeRenderTexture = null;
            }

            if (overlayRoot != null)
            {
                Destroy(overlayRoot);
                overlayRoot = null;
            }
        }

        private void OnDestroy()
        {
            ExitVideoPlaybackState();
            CleanupOverlay();
        }
    }
}
