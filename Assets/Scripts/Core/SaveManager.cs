using System;
using System.IO;
using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Quản lý Save/Load game data sử dụng JSON file.
    /// Lưu tại Application.persistentDataPath/savegame.json
    /// </summary>
    public sealed class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SAVE_FILE_NAME = "savegame.json";

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Thu thập dữ liệu game hiện tại và lưu vào file
        /// </summary>
        public void SaveGame()
        {
            var data = new SaveData();

            // Player position
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                data.playerPositionX = player.transform.position.x;
                data.playerPositionY = player.transform.position.y;
            }

            // Player health
            var health = FindFirstObjectByType<PlayerHealth2D>();
            if (health != null)
            {
                data.playerCurrentHealth = health.CurrentHealth;
                data.playerMaxHealth = health.MaxHealth;
            }

            // Player stamina
            var stamina = FindFirstObjectByType<PlayerStamina>();
            if (stamina != null)
            {
                data.playerCurrentStamina = stamina.CurrentStamina;
            }

            // Light fragments
            if (GameManager.Instance != null)
            {
                data.lightFragmentsCollected = GameManager.Instance.LightFragmentsCollected;
            }

            // Scene name
            data.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // Checkpoint
            var spawnMgr = FindFirstObjectByType<PlayerSpawnManager>();
            if (spawnMgr != null)
            {
                var checkpoint = spawnMgr.GetCurrentCheckpoint();
                if (checkpoint != null)
                {
                    data.checkpointPositionX = checkpoint.position.x;
                    data.checkpointPositionY = checkpoint.position.y;
                    data.hasCheckpoint = true;
                }
            }

            // Save timestamp
            data.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Write to file
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[SaveManager] Game saved to {SaveFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to save: {e.Message}");
            }
        }

        /// <summary>
        /// Load save data từ file (không apply, chỉ trả dữ liệu)
        /// </summary>
        public SaveData LoadSaveData()
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.Log("[SaveManager] No save file found.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[SaveManager] Save loaded from {SaveFilePath}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra có save file hay không
        /// </summary>
        public bool HasSaveFile()
        {
            return File.Exists(SaveFilePath);
        }

        /// <summary>
        /// Xóa save file
        /// </summary>
        public void DeleteSave()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log("[SaveManager] Save file deleted.");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }

    /// <summary>
    /// Data class chứa tất cả thông tin cần save
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // Player
        public float playerPositionX;
        public float playerPositionY;
        public int playerCurrentHealth;
        public int playerMaxHealth;
        public float playerCurrentStamina;

        // Progress
        public int lightFragmentsCollected;
        public string sceneName;

        // Checkpoint
        public bool hasCheckpoint;
        public float checkpointPositionX;
        public float checkpointPositionY;

        // Meta
        public string saveTimestamp;
    }
}
