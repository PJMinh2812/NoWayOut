using UnityEngine;
using System.Collections;

namespace NWO
{
    /// <summary>
    /// Tự động load save data khi scene bắt đầu (nếu có flag LoadFromSave).
    /// Attach vào cùng GameObject với GameManager hoặc tạo riêng.
    /// </summary>
    public sealed class SaveGameLoader : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Delay trước khi apply save data (chờ player spawn xong)")]
        [SerializeField] private float applyDelay = 0.5f;

        private void Start()
        {
            if (PlayerPrefs.GetInt("LoadFromSave", 0) == 1)
            {
                PlayerPrefs.SetInt("LoadFromSave", 0);
                PlayerPrefs.Save();
                SceneLoader.BeginBlocking("LOADING SAVE...");
                StartCoroutine(LoadSaveDataCoroutine());
            }
        }

        private IEnumerator LoadSaveDataCoroutine()
        {
            // Chờ để player và các system khác khởi tạo xong
            yield return new WaitForSeconds(applyDelay);

            if (SaveManager.Instance == null)
            {
                Debug.LogWarning("[SaveGameLoader] SaveManager not found!");
                SceneLoader.EndBlocking();
                yield break;
            }

            var data = SaveManager.Instance.LoadSaveData();
            if (data == null)
            {
                Debug.LogWarning("[SaveGameLoader] No save data found!");
                SceneLoader.EndBlocking();
                yield break;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ApplySaveData(data);
            }

            Debug.Log("[SaveGameLoader] Save data loaded and applied successfully!");
            yield return null; // allow a frame for spawned objects to settle
            SceneLoader.EndBlocking();
        }
    }
}
