using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

namespace NWO
{
    /// <summary>
    /// Đồng bộ âm lượng UI (slider 0..1) với <see cref="AudioMixer"/> exposed parameters.
    /// Dùng cùng quy tắc với thang tuyến tính của <see cref="AudioSource.volume"/> (0 = tắt, 1 = max).
    /// Khi không có AudioMixer, fallback: chỉnh trực tiếp <see cref="AudioSource.volume"/> của tất cả nguồn âm.
    /// </summary>
    public static class AudioVolumeHelper
    {
        private static readonly Dictionary<int, float> _baseVolumes = new Dictionary<int, float>();
        private static float _fallbackMaster = 1f;
        private static float _fallbackMusic = 1f;
        private static float _fallbackSfx = 1f;

        [UnityEngine.RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplySavedPrefsOnStartup()
        {
            _fallbackMaster = Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 1f));
            _fallbackMusic = Mathf.Clamp01(PlayerPrefs.GetFloat("MusicVolume", 1f));
            _fallbackSfx = Mathf.Clamp01(PlayerPrefs.GetFloat("SFXVolume", 1f));
            ApplyFallbackToAllSources();
        }
        /// <summary>Giá trị dB dùng khi âm lượng tuyến tính ≈ 0 (tránh Log10(0) = -∞ và khớp mixer Unity).</summary>
        public const float MixerSilenceDb = -80f;

        /// <summary>
        /// Chuyển âm lượng tuyến tính 0..1 (giống AudioSource.volume) sang decibel cho AudioMixer.
        /// </summary>
        public static float LinearToMixerDecibels(float linear01)
        {
            linear01 = Mathf.Clamp01(linear01);
            if (linear01 <= 0.0001f)
                return MixerSilenceDb;
            return 20f * Mathf.Log10(linear01);
        }

        /// <summary>Gọi SetFloat trên mixer với tham số exposed (vd: MasterVolume). Nếu mixer null → fallback chỉnh AudioSource.</summary>
        public static void ApplyLinearToMixer(AudioMixer mixer, string exposedParameterName, float linear01)
        {
            if (mixer != null && !string.IsNullOrWhiteSpace(exposedParameterName))
            {
                mixer.SetFloat(exposedParameterName, LinearToMixerDecibels(linear01));
                return;
            }
            linear01 = Mathf.Clamp01(linear01);
            if (exposedParameterName.Contains("Master")) _fallbackMaster = linear01;
            else if (exposedParameterName.Contains("Music")) _fallbackMusic = linear01;
            else if (exposedParameterName.Contains("SFX")) _fallbackSfx = linear01;
            ApplyFallbackToAllSources();
        }

        /// <summary>Áp dụng âm lượng (Master×Music×SFX) lên tất cả AudioSource khi không có mixer.</summary>
        private static void ApplyFallbackToAllSources()
        {
            float combined = _fallbackMaster * _fallbackMusic * _fallbackSfx;
            var sources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            var validIds = new HashSet<int>();
            foreach (var src in sources)
            {
                if (src == null) continue;
                int id = src.GetInstanceID();
                validIds.Add(id);
                if (!_baseVolumes.TryGetValue(id, out float baseVol))
                {
                    baseVol = src.volume;
                    _baseVolumes[id] = baseVol;
                }
                src.volume = baseVol * combined;
            }
            var toRemove = new List<int>();
            foreach (int k in _baseVolumes.Keys)
            {
                if (!validIds.Contains(k)) toRemove.Add(k);
            }
            foreach (int k in toRemove) _baseVolumes.Remove(k);
        }

        /// <summary>
        /// Đọc dB từ mixer (nếu có) và chuyển về 0..1 cho slider. Dùng khi cần đồng bộ UI với mixer đã chỉnh tay.
        /// </summary>
        public static bool TryGetLinearFromMixer(AudioMixer mixer, string exposedParameterName, out float linear01)
        {
            linear01 = 1f;
            if (mixer == null || string.IsNullOrWhiteSpace(exposedParameterName))
                return false;
            if (!mixer.GetFloat(exposedParameterName, out float db))
                return false;
            if (db <= MixerSilenceDb + 0.1f)
            {
                linear01 = 0f;
                return true;
            }
            linear01 = Mathf.Clamp01(Mathf.Pow(10f, db / 20f));
            return true;
        }
    }
}
