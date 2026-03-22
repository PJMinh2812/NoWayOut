using System;
using System.IO;
using UnityEngine;
using ProceduralGeneration.Core;
using ProceduralGeneration.Data;
using Core;

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
                data.hasPlayerPosition = true;
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

            // Map anchor (dungeon world offset): capture Respawn_Point so Continue can rebuild map at same place.
            GameObject respawnPointObj = GameObject.Find("Respawn_Point");
            if (respawnPointObj != null)
            {
                data.hasMapAnchor = true;
                data.mapAnchorX = respawnPointObj.transform.position.x;
                data.mapAnchorY = respawnPointObj.transform.position.y;
            }

            // Dungeon state (để Continue giữ nguyên map)
            var dungeonManager = ResolveDungeonManager(preferWithRooms: true);
            if (dungeonManager != null)
            {
                data.dungeonSeed = dungeonManager.GetCurrentSeed();
                data.hasDungeonSeed = data.dungeonSeed != 0;

                if (!data.hasDungeonSeed)
                {
                    int fallbackSeed = PlayerPrefs.GetInt("LastDungeonSeed", 0);
                    if (fallbackSeed != 0)
                    {
                        data.dungeonSeed = fallbackSeed;
                        data.hasDungeonSeed = true;
                    }
                }

                // Save current active room grid position
                Room activeRoom = null;
                var transitionMgr = RoomTransitionManager.Instance;
                if (transitionMgr != null)
                    activeRoom = transitionMgr.GetCurrentRoom();

                // Fallback: find the active room from DungeonManager
                if (activeRoom == null)
                {
                    var allRooms = dungeonManager.GetAllRooms();
                    if (allRooms != null)
                    {
                        foreach (var room in allRooms)
                        {
                            if (room != null && room.roomInstance != null && room.roomInstance.activeSelf)
                            {
                                activeRoom = room;
                                break;
                            }
                        }
                    }
                }

                if (activeRoom == null)
                {
                    var playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null)
                    {
                        activeRoom = FindRoomContainingPosition(dungeonManager, playerObj.transform.position);
                    }
                }

                if (activeRoom != null)
                {
                    data.hasCurrentRoom = true;
                    data.currentRoomGridX = activeRoom.gridPosition.x;
                    data.currentRoomGridY = activeRoom.gridPosition.y;
                }
                else if (TryFindActiveRoomGridFromContainer(dungeonManager, out int activeGridX, out int activeGridY))
                {
                    data.hasCurrentRoom = true;
                    data.currentRoomGridX = activeGridX;
                    data.currentRoomGridY = activeGridY;
                }
            }
            else
            {
                int fallbackSeed = PlayerPrefs.GetInt("LastDungeonSeed", 0);
                if (fallbackSeed != 0)
                {
                    data.dungeonSeed = fallbackSeed;
                    data.hasDungeonSeed = true;
                }
            }

            var runProgression = FindFirstObjectByType<ProceduralGeneration.Integration.DungeonRunProgressionManager>();
            if (runProgression != null)
            {
                data.hasRunProgressionState = true;
                data.runCurrentRound = runProgression.CurrentRound;
                data.runCurrentMap = runProgression.CurrentMap;
            }

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

        private DungeonManager ResolveDungeonManager(bool preferWithRooms)
        {
            DungeonManager fallback = null;
            DungeonManager bestWithRooms = null;

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
                {
                    bestWithRooms = manager;
                    if (manager.dungeonContainer != null)
                        return manager;
                }
            }

            if (preferWithRooms && bestWithRooms != null)
                return bestWithRooms;

            return fallback;
        }

        private Room FindRoomContainingPosition(DungeonManager dungeonManager, Vector3 worldPos)
        {
            if (dungeonManager == null)
                return null;

            var allRooms = dungeonManager.GetAllRooms();
            if (allRooms == null)
                return null;

            foreach (var room in allRooms)
            {
                if (room == null || room.roomInstance == null)
                    continue;

                var renderers = room.roomInstance.GetComponentsInChildren<Renderer>(true);
                if (renderers == null || renderers.Length == 0)
                    continue;

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                if (bounds.Contains(worldPos))
                    return room;
            }

            return null;
        }

        private bool TryFindActiveRoomGridFromContainer(DungeonManager dungeonManager, out int gridX, out int gridY)
        {
            gridX = 0;
            gridY = 0;

            if (dungeonManager == null || dungeonManager.dungeonContainer == null)
                return false;

            var container = dungeonManager.dungeonContainer;
            for (int i = 0; i < container.childCount; i++)
            {
                var child = container.GetChild(i);
                if (!child.gameObject.activeSelf)
                    continue;

                if (TryParseRoomGridFromName(child.name, out gridX, out gridY))
                    return true;
            }

            return false;
        }

        private bool TryParseRoomGridFromName(string roomName, out int gridX, out int gridY)
        {
            gridX = 0;
            gridY = 0;

            if (string.IsNullOrEmpty(roomName) || !roomName.StartsWith("Room_"))
                return false;

            string[] parts = roomName.Split('_');
            if (parts.Length < 4)
                return false;

            return int.TryParse(parts[2], out gridX) && int.TryParse(parts[3], out gridY);
        }
    }

    /// <summary>
    /// Data class chứa tất cả thông tin cần save
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // Player
        public bool hasPlayerPosition;
        public float playerPositionX;
        public float playerPositionY;
        public int playerCurrentHealth;
        public int playerMaxHealth;
        public float playerCurrentStamina;

        // Progress
        public int lightFragmentsCollected;
        public string sceneName;

        // Dungeon state
        public bool hasDungeonSeed;
        public int dungeonSeed;
        public bool hasMapAnchor;
        public float mapAnchorX;
        public float mapAnchorY;
        public bool hasRunProgressionState;
        public int runCurrentRound;
        public int runCurrentMap;

        // Checkpoint
        public bool hasCheckpoint;
        public float checkpointPositionX;
        public float checkpointPositionY;

        // Current room (để Continue spawn đúng phòng)
        public bool hasCurrentRoom;
        public int currentRoomGridX;
        public int currentRoomGridY;

        // Meta
        public string saveTimestamp;
    }
}
