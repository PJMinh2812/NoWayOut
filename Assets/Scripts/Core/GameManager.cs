using UnityEngine;
using UnityEngine.SceneManagement;
using ProceduralGeneration.Core;

namespace NWO
{
    /// <summary>
    /// Manages game state, game over, and restart functionality
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private bool isGameOver;
        [SerializeField] private bool isPaused;
        
        [Header("Light Fragments")]
        [SerializeField] private int lightFragmentsCollected = 0;
        [SerializeField] private int totalLightFragments = 3; // Tổng số mảnh cần thu thập
        
        [Header("References")]
        [SerializeField] private GameObject gameOverUI; // GameOverPanel hoặc GameOverCanvas

        [Header("Audio")]
        [SerializeField] private AudioClip gameplayMusic;
        [SerializeField, Range(0f, 1f)] private float gameplayMusicVolume = 0.5f;

        private AudioSource gameplayMusicSource;

        public bool IsGameOver => isGameOver;
        public bool IsPaused => isPaused;
        public int LightFragmentsCollected => lightFragmentsCollected;
        public int TotalLightFragments => totalLightFragments;
        
        // === EVENTS ===
        /// <summary>Event khi nhặt Light Fragment (current, total)</summary>
        public event System.Action<int, int> OnLightFragmentCollected;
        /// <summary>Event khi nhặt đủ tất cả Light Fragments</summary>
        public event System.Action OnAllLightFragmentsCollected;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            if (gameOverUI != null)
            {
                var gameOverUiComp = gameOverUI.GetComponent<GameOverUI>();
                if (gameOverUiComp != null)
                    gameOverUiComp.HideGameOverImmediate();
                else
                    gameOverUI.SetActive(false);
            }

            SetupGameplayMusic();
            
            // Auto-create LightFragmentUI nếu chưa có
            if (FindFirstObjectByType<LightFragmentUI>() == null)
            {
                var uiObj = new GameObject("LightFragmentUI");
                uiObj.AddComponent<LightFragmentUI>();
                Debug.Log("[GameManager] Auto-created LightFragmentUI");
            }
            
            // // Auto-create FlashOfTruthUI nếu chưa có
            // if (FindFirstObjectByType<FlashOfTruthUI>() == null)
            // {
            //     var flashUIObj = new GameObject("FlashOfTruthUI");
            //     flashUIObj.AddComponent<FlashOfTruthUI>();
            //     Debug.Log("[GameManager] Auto-created FlashOfTruthUI");
            // }
            
            // Auto-create DungeonLightingManager nếu chưa có
            if (FindFirstObjectByType<DungeonLightingManager>() == null)
            {
                var lightingObj = new GameObject("DungeonLightingManager");
                lightingObj.AddComponent<DungeonLightingManager>();
                Debug.Log("[GameManager] Auto-created DungeonLightingManager");
            }
            
            // Auto-create MinimapManager nếu chưa có
            if (FindFirstObjectByType<MinimapManager>() == null)
            {
                var minimapObj = new GameObject("MinimapManager");
                minimapObj.AddComponent<MinimapManager>();
                Debug.Log("[GameManager] Auto-created MinimapManager");
            }

            // Auto-create UpgradeManager nếu chưa có
            if (FindFirstObjectByType<UpgradeManager>() == null)
            {
                var upgradeObj = new GameObject("UpgradeManager");
                upgradeObj.AddComponent<UpgradeManager>();
                Debug.Log("[GameManager] Auto-created UpgradeManager");
            }

            // Auto-create UpgradeSelectionUI nếu chưa có
            if (FindFirstObjectByType<UpgradeSelectionUI>() == null)
            {
                var upgradeUIObj = new GameObject("UpgradeSelectionUI");
                upgradeUIObj.AddComponent<UpgradeSelectionUI>();
                Debug.Log("[GameManager] Auto-created UpgradeSelectionUI");
            }

            // Auto-create CoinManager nếu chưa có
            if (FindFirstObjectByType<CoinManager>() == null)
            {
                var coinObj = new GameObject("CoinManager");
                coinObj.AddComponent<CoinManager>();
                Debug.Log("[GameManager] Auto-created CoinManager");
            }

            // Auto-create HeartManager nếu chưa có
            if (FindFirstObjectByType<HeartManager>() == null)
            {
                var heartObj = new GameObject("HeartManager");
                heartObj.AddComponent<HeartManager>();
                Debug.Log("[GameManager] Auto-created HeartManager");
            }

            // Auto-create KeyBindManager nếu chưa có
            if (KeyBindManager.Instance == null && FindFirstObjectByType<KeyBindManager>() == null)
            {
                var kbObj = new GameObject("KeyBindManager");
                kbObj.AddComponent<KeyBindManager>();
            }

            // Auto-create CoinUI nếu chưa có
            if (FindFirstObjectByType<CoinUI>() == null)
            {
                var coinUIObj = new GameObject("CoinUI");
                coinUIObj.AddComponent<CoinUI>();
                Debug.Log("[GameManager] Auto-created CoinUI");
            }

            // Auto-create SaveManager nếu chưa có
            if (SaveManager.Instance == null && FindFirstObjectByType<SaveManager>() == null)
            {
                var saveObj = new GameObject("SaveManager");
                saveObj.AddComponent<SaveManager>();
                Debug.Log("[GameManager] Auto-created SaveManager");
            }

            // Auto-create AnimationPreloader to pre-warm all animation clips
            if (FindFirstObjectByType<AnimationPreloader>() == null)
            {
                var preloaderObj = new GameObject("AnimationPreloader");
                preloaderObj.AddComponent<AnimationPreloader>();
            }
        }

        private void SetupGameplayMusic()
        {
            if (gameplayMusic == null)
                return;

            gameplayMusicSource = GetComponent<AudioSource>();
            if (gameplayMusicSource == null)
                gameplayMusicSource = gameObject.AddComponent<AudioSource>();

            gameplayMusicSource.clip = gameplayMusic;
            gameplayMusicSource.playOnAwake = false;
            gameplayMusicSource.loop = true;
            gameplayMusicSource.spatialBlend = 0f;
            gameplayMusicSource.volume = Mathf.Clamp01(gameplayMusicVolume);

            if (!gameplayMusicSource.isPlaying)
                gameplayMusicSource.Play();
        }


        /// Call this when player dies

        public void TriggerGameOver()
        {
            if (isGameOver) return; // Already game over

            isGameOver = true;
            isPaused = false;
            Time.timeScale = 0f; // Pause game

            if (gameplayMusicSource != null && gameplayMusicSource.isPlaying)
                gameplayMusicSource.Stop();

            if (gameOverUI != null)
            {
                // Nếu gameOverUI là Panel, cần tìm parent Canvas
                var canvas = gameOverUI.GetComponentInParent<Canvas>();
                if (canvas != null && !canvas.gameObject.activeSelf)
                {
                    // Active Canvas trước
                    canvas.gameObject.SetActive(true);
                    Debug.Log("[GameManager] Activated GameOverCanvas");
                }

                // Prefer component flow so GameOverUI can run transitions/conditions
                var gameOverUiComp = gameOverUI.GetComponent<GameOverUI>();
                if (gameOverUiComp != null)
                    gameOverUiComp.ShowGameOver();
                else
                    gameOverUI.SetActive(true);

                Debug.Log("[GameManager] Game Over UI Activated!");
            }
            else
            {
                Debug.LogError("[GameManager] Game Over UI reference is NULL!");
            }

            Debug.Log("[GameManager] Game Over!");
        }

        /// <summary>
        /// Gọi khi người chơi thu thập Light Fragment
        /// </summary>
        public void CollectLightFragment(int fragmentID)
        {
            lightFragmentsCollected++;
            Debug.Log($"[GameManager] Light Fragment #{fragmentID} collected! Total: {lightFragmentsCollected}/{totalLightFragments}");
            
            // Fire event
            OnLightFragmentCollected?.Invoke(lightFragmentsCollected, totalLightFragments);
            
            // Kiểm tra đã thu thập đủ chưa
            if (lightFragmentsCollected >= totalLightFragments)
            {
                OnAllFragmentsCollected();
            }
        }
        
        /// <summary>
        /// Được gọi khi thu thập đủ tất cả Light Fragments
        /// </summary>
        private void OnAllFragmentsCollected()
        {
            Debug.Log("[GameManager] All Light Fragments collected! Unlocking Flash of Truth skill...");
            
            // Fire event
            OnAllLightFragmentsCollected?.Invoke();
        }


        /// Restart current scene

        public void RestartGame()
        {
            Time.timeScale = 1f; // Resume time
            isGameOver = false;
            
            // Hide game over UI first
            if (gameOverUI != null)
            {
                var gameOverUiComp = gameOverUI.GetComponent<GameOverUI>();
                if (gameOverUiComp != null)
                    gameOverUiComp.HideGameOverImmediate();
                else
                    gameOverUI.SetActive(false);
            }
            
            // Reset player health
            var playerHealth = FindFirstObjectByType<PlayerHealth2D>();
            if (playerHealth != null)
            {
                playerHealth.ResetHealth();
            }

            // Reset upgrades
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.ResetUpgrades();
            }

            // Reset coins
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.ResetCoins();
            }

            // Allow animation preloading on next scene
            AnimationPreloader.ResetPreloadFlag();
            
            // Try to regenerate map with new layout instead of reloading scene
            // Support both new and legacy dungeon systems
            var mapManager = FindFirstObjectByType<NWO.Dungeon.MapInitializationManager>();
            if (mapManager != null)
            {
                // New system
                mapManager.RegenerateWithNewSeed();
                Debug.Log("[GameManager] Map regenerated with new layout (new system)!");
                return;
            }
            
            // Try legacy system
            #pragma warning disable CS0618 // Type is obsolete but needed for backward compatibility
            var legacyBuilder = FindFirstObjectByType<NWO.Dungeon.UnityDungeonTilemapBuilder>();
            #pragma warning restore CS0618
            if (legacyBuilder != null)
            {
                // Legacy system
                legacyBuilder.RegenerateWithNewSeed();
                Debug.Log("[GameManager] Map regenerated with new layout (legacy system)!");
                return;
            }
            
            // Fallback: reload scene if no dungeon manager found
            Debug.LogWarning("[GameManager] No dungeon manager found, reloading scene...");
            SceneLoader.LoadScene(SceneManager.GetActiveScene().name);
        }


        /// Load main menu (if you have one)

        public void LoadMainMenu()
        {
            TryAutoSaveProgress();
            Time.timeScale = 1f;
            isGameOver = false;
            SceneLoader.LoadScene("MainMenu"); // Change to your menu scene name
        }


        /// Quit game

        public void QuitGame()
        {
            TryAutoSaveProgress();
            Debug.Log("[GameManager] Quitting game...");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void OnApplicationQuit()
        {
            TryAutoSaveProgress();
        }

        private void TryAutoSaveProgress()
        {
            // Không save trong main menu
            string activeScene = SceneManager.GetActiveScene().name;
            if (string.Equals(activeScene, "MainMenu", System.StringComparison.OrdinalIgnoreCase))
                return;

            // Chỉ save khi có player trong scene gameplay
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
                return;
            }

            SaveManager saveManager = FindFirstObjectByType<SaveManager>();
            if (saveManager != null)
            {
                saveManager.SaveGame();
            }
        }

        /// <summary>
        /// Apply save data vào game (gọi sau khi scene load xong)
        /// </summary>
        public void ApplySaveData(SaveData data)
        {
            if (data == null) return;

            // Restore light fragments
            lightFragmentsCollected = data.lightFragmentsCollected;

            bool isProceduralRunActive =
                FindFirstObjectByType<ProceduralGeneration.Integration.DungeonRunProgressionManager>() != null;

            Vector3 savedPosition = ResolveSavedPlayerPosition(data);

            // Activate the correct room FIRST (before setting player position)
            bool roomActivated = false;
            Room restoredRoom = null;
            var dungeonManager = ResolveDungeonManager(preferWithRooms: true);
            if (data.hasCurrentRoom && dungeonManager != null)
            {
                roomActivated = dungeonManager.ActivateRoomByGridPosition(
                    data.currentRoomGridX, data.currentRoomGridY);

                if (!roomActivated)
                {
                    roomActivated = TryActivateRoomByGridFallback(dungeonManager, data.currentRoomGridX, data.currentRoomGridY);
                }

                if (roomActivated)
                    restoredRoom = FindRoomByGridPosition(dungeonManager, data.currentRoomGridX, data.currentRoomGridY);
                Debug.Log($"[GameManager] Restored room by grid ({data.currentRoomGridX},{data.currentRoomGridY}): {roomActivated}");
            }

            // If saved grid points to the wrong room (commonly stale Start room), prefer room containing saved player position.
            if (roomActivated && restoredRoom != null && !IsRoomContainingPosition(restoredRoom, savedPosition))
            {
                Room positionRoom = ActivateProceduralRoomAtPosition(savedPosition);
                if (positionRoom != null)
                {
                    restoredRoom = positionRoom;
                    roomActivated = true;
                    Debug.Log($"[GameManager] Grid-restored room did not contain saved position. Overrode by position to room ({positionRoom.gridPosition.x},{positionRoom.gridPosition.y}).");
                }
            }

            // Restore player position
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                ApplyPlayerPosition(player, savedPosition, "ApplySaveData-initial");
                UpdateRespawnPointToSavedPosition(savedPosition);

                // Fallback: if room wasn't activated by grid, try by position
                if (!roomActivated && isProceduralRunActive)
                {
                    restoredRoom = ActivateProceduralRoomAtPosition(savedPosition);
                    roomActivated = restoredRoom != null || TryActivateRoomByPositionFallback(dungeonManager, savedPosition);
                }

                // Restore health
                var health = player.GetComponent<PlayerHealth2D>();
                if (health != null)
                {
                    health.SetHealth(data.playerCurrentHealth);
                }

                // Restore stamina
                var stamina = player.GetComponent<PlayerStamina>();
                if (stamina != null)
                {
                    stamina.SetStamina(data.playerCurrentStamina);
                }
            }

            SyncRoomTransitionManager(restoredRoom, savedPosition);
            LogRoomRestoreState("ApplySaveData-immediate", data, savedPosition);

            // Retry vài lần để cover trường hợp dungeon generate xong trễ hơn save loader.
            StartCoroutine(EnsureRoomRestoreAfterLoad(data, savedPosition));

            // Refresh special room lights after room activation change
            if (DungeonLightingManager.Instance != null)
            {
                DungeonLightingManager.Instance.RefreshSpecialRoomLights();
            }

            // Clear the restore flag so future RebuildRoomListFromScene calls work normally
            PlayerPrefs.SetInt("RestoreDungeonFromSave", 0);
            PlayerPrefs.Save();

            Debug.Log($"[GameManager] Save data applied. Fragments: {lightFragmentsCollected}, Scene: {data.sceneName}");
        }

        private System.Collections.IEnumerator EnsureRoomRestoreAfterLoad(SaveData data, Vector3 savedPosition)
        {
            const int maxAttempts = 12;
            const float retryDelay = 0.2f;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var dungeonManager = ResolveDungeonManager(preferWithRooms: true);
                if (dungeonManager != null)
                {
                    bool activatedByGrid = false;
                    Room activeRoom = null;

                    if (data.hasCurrentRoom)
                    {
                        activatedByGrid = dungeonManager.ActivateRoomByGridPosition(data.currentRoomGridX, data.currentRoomGridY);
                        if (!activatedByGrid)
                            activatedByGrid = TryActivateRoomByGridFallback(dungeonManager, data.currentRoomGridX, data.currentRoomGridY);

                        if (activatedByGrid)
                            activeRoom = FindRoomByGridPosition(dungeonManager, data.currentRoomGridX, data.currentRoomGridY);

                        if (activatedByGrid && activeRoom != null && !IsRoomContainingPosition(activeRoom, savedPosition))
                        {
                            Room positionRoom = ActivateProceduralRoomAtPosition(savedPosition);
                            if (positionRoom != null)
                            {
                                activeRoom = positionRoom;
                                activatedByGrid = true;
                            }
                        }
                    }

                    if (!activatedByGrid)
                    {
                        activeRoom = ActivateProceduralRoomAtPosition(savedPosition);
                        if (activeRoom == null)
                            activatedByGrid = TryActivateRoomByPositionFallback(dungeonManager, savedPosition);
                    }

                    if (activeRoom != null || activatedByGrid)
                    {
                        var playerObj = GameObject.FindGameObjectWithTag("Player");
                        if (playerObj != null)
                        {
                            ApplyPlayerPosition(playerObj, savedPosition, $"EnsureRoomRestoreAfterLoad-attempt-{attempt + 1}");
                        }
                        UpdateRespawnPointToSavedPosition(savedPosition);

                        SyncRoomTransitionManager(activeRoom, savedPosition);
                        LogRoomRestoreState($"EnsureRoomRestoreAfterLoad-success-attempt-{attempt + 1}", data, savedPosition);

                        if (DungeonLightingManager.Instance != null)
                            DungeonLightingManager.Instance.RefreshSpecialRoomLights();

                        yield break;
                    }
                }

                yield return new WaitForSeconds(retryDelay);
            }

            LogRoomRestoreState("EnsureRoomRestoreAfterLoad-failed", data, savedPosition);
        }

        private void ApplyPlayerPosition(GameObject player, Vector3 position, string phase)
        {
            if (player == null)
                return;

            Vector3 before = player.transform.position;
            Debug.Log($"[ContinueDebug] {phase} | ApplyPlayerPosition BEFORE={before} TARGET={position}");

            player.transform.position = position;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            Vector3 after = player.transform.position;
            Debug.Log($"[ContinueDebug] {phase} | ApplyPlayerPosition AFTER={after}");
        }

        private void UpdateRespawnPointToSavedPosition(Vector3 position)
        {
            GameObject respawnPoint = GameObject.Find("Respawn_Point");
            if (respawnPoint == null)
                respawnPoint = new GameObject("Respawn_Point");

            respawnPoint.transform.position = position;
        }

        private Room FindRoomByGridPosition(DungeonManager dungeonManager, int gridX, int gridY)
        {
            if (dungeonManager == null)
                return null;

            var allRooms = dungeonManager.GetAllRooms();
            if (allRooms == null)
                return null;

            foreach (var room in allRooms)
            {
                if (room != null && room.gridPosition.x == gridX && room.gridPosition.y == gridY)
                    return room;
            }

            return null;
        }

        private Vector3 ResolveSavedPlayerPosition(SaveData data)
        {
            // Continue should prioritize the exact player location at save-time.
            if (data != null && data.hasPlayerPosition)
            {
                return new Vector3(data.playerPositionX, data.playerPositionY, 0f);
            }

            // Backward compatibility for old saves without hasPlayerPosition.
            bool hasLegacyPlayerPosition = data != null
                && (Mathf.Abs(data.playerPositionX) > 0.001f || Mathf.Abs(data.playerPositionY) > 0.001f);
            if (hasLegacyPlayerPosition)
            {
                return new Vector3(data.playerPositionX, data.playerPositionY, 0f);
            }

            if (data != null && data.hasCheckpoint)
            {
                return new Vector3(data.checkpointPositionX, data.checkpointPositionY, 0f);
            }

            return Vector3.zero;
        }

        private Room ActivateProceduralRoomAtPosition(Vector3 worldPos)
        {
            var dungeonManager = ResolveDungeonManager(preferWithRooms: true);
            if (dungeonManager == null)
                return null;

            var allRooms = dungeonManager.GetAllRooms();
            if (allRooms == null || allRooms.Count == 0)
            {
                TryActivateRoomByPositionFallback(dungeonManager, worldPos);
                return null;
            }

            Room targetRoom = null;
            float bestDistance = float.MaxValue;

            foreach (var room in allRooms)
            {
                if (room == null || room.roomInstance == null)
                    continue;

                var renderers = room.roomInstance.GetComponentsInChildren<Renderer>(true);
                if (renderers != null && renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                        bounds.Encapsulate(renderers[i].bounds);

                    // Ưu tiên tuyệt đối room có bounds chứa vị trí save.
                    if (bounds.Contains(worldPos))
                    {
                        targetRoom = room;
                        bestDistance = -1f;
                        break;
                    }
                }

                Vector3 center = room.roomInstance.transform.position +
                                 new Vector3(room.actualSize.x * 0.5f, room.actualSize.y * 0.5f, 0f);
                float dist = Vector2.Distance(new Vector2(worldPos.x, worldPos.y), new Vector2(center.x, center.y));

                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    targetRoom = room;
                }
            }

            if (targetRoom == null)
                return null;

            foreach (var room in allRooms)
            {
                if (room != null && room.roomInstance != null)
                    room.roomInstance.SetActive(room == targetRoom);
            }

            GameObject respawnPoint = GameObject.Find("Respawn_Point");
            if (respawnPoint == null)
                respawnPoint = new GameObject("Respawn_Point");

            respawnPoint.transform.position = worldPos;

            SyncRoomTransitionManager(targetRoom, worldPos);
            return targetRoom;
        }

        private void SyncRoomTransitionManager(Room preferredRoom, Vector3 playerPosition)
        {
            var transitionManager = Core.RoomTransitionManager.Instance;
            if (transitionManager == null)
                return;

            Room roomToSet = preferredRoom ?? FindRoomContainingPosition(playerPosition);
            if (roomToSet != null)
            {
                transitionManager.SetCurrentRoom(roomToSet);
            }
        }

        private Room FindRoomContainingPosition(Vector3 worldPos)
        {
            var dungeonManager = ResolveDungeonManager(preferWithRooms: true);
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

        private bool IsRoomContainingPosition(Room room, Vector3 worldPos)
        {
            if (room == null || room.roomInstance == null)
                return false;

            var renderers = room.roomInstance.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return false;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds.Contains(worldPos);
        }

        private bool TryActivateRoomByGridFallback(DungeonManager dungeonManager, int gridX, int gridY)
        {
            if (dungeonManager == null || dungeonManager.dungeonContainer == null)
                return false;

            Transform target = null;
            var container = dungeonManager.dungeonContainer;
            for (int i = 0; i < container.childCount; i++)
            {
                Transform child = container.GetChild(i);
                if (!TryParseRoomGridFromName(child.name, out int roomX, out int roomY))
                    continue;

                if (roomX == gridX && roomY == gridY)
                {
                    target = child;
                    break;
                }
            }

            if (target == null)
                return false;

            ActivateOnlyRoomObject(container, target);
            return true;
        }

        private bool TryActivateRoomByPositionFallback(DungeonManager dungeonManager, Vector3 worldPos)
        {
            if (dungeonManager == null || dungeonManager.dungeonContainer == null)
                return false;

            Transform target = null;
            float bestDistance = float.MaxValue;
            var container = dungeonManager.dungeonContainer;

            for (int i = 0; i < container.childCount; i++)
            {
                Transform child = container.GetChild(i);
                if (!TryParseRoomGridFromName(child.name, out _, out _))
                    continue;

                var renderers = child.GetComponentsInChildren<Renderer>(true);
                if (renderers != null && renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int r = 1; r < renderers.Length; r++)
                        bounds.Encapsulate(renderers[r].bounds);

                    if (bounds.Contains(worldPos))
                    {
                        target = child;
                        bestDistance = -1f;
                        break;
                    }

                    float dist = Vector2.Distance(new Vector2(worldPos.x, worldPos.y), new Vector2(bounds.center.x, bounds.center.y));
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        target = child;
                    }
                }
            }

            if (target == null)
                return false;

            ActivateOnlyRoomObject(container, target);
            return true;
        }

        private void ActivateOnlyRoomObject(Transform container, Transform target)
        {
            if (container == null || target == null)
                return;

            for (int i = 0; i < container.childCount; i++)
            {
                Transform child = container.GetChild(i);
                if (TryParseRoomGridFromName(child.name, out _, out _))
                {
                    child.gameObject.SetActive(child == target);
                }
            }
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

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void LogRoomRestoreState(string phase, SaveData data, Vector3 savedPosition)
        {
            var dungeonManager = ResolveDungeonManager(preferWithRooms: true);
            if (dungeonManager == null)
            {
                var allManagers = FindObjectsByType<DungeonManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Debug.Log($"[ContinueDebug] {phase} | DungeonManager missing/invalid. managerCount={allManagers.Length}");
                return;
            }

            var allRooms = dungeonManager.GetAllRooms();
            if (allRooms == null || allRooms.Count == 0)
            {
                var allManagers = FindObjectsByType<DungeonManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                int containerRoomCount = 0;
                int containerActiveCount = 0;
                if (dungeonManager.dungeonContainer != null)
                {
                    for (int i = 0; i < dungeonManager.dungeonContainer.childCount; i++)
                    {
                        var child = dungeonManager.dungeonContainer.GetChild(i);
                        if (!TryParseRoomGridFromName(child.name, out _, out _))
                            continue;

                        containerRoomCount++;
                        if (child.gameObject.activeSelf)
                            containerActiveCount++;
                    }
                }

                Debug.Log($"[ContinueDebug] {phase} | No rooms in selected DungeonManager. managerCount={allManagers.Length}, containerRooms={containerRoomCount}, containerActive={containerActiveCount}");
                return;
            }

            Room activeRoom = null;
            int activeCount = 0;
            for (int i = 0; i < allRooms.Count; i++)
            {
                var room = allRooms[i];
                if (room?.roomInstance != null && room.roomInstance.activeSelf)
                {
                    activeCount++;
                    if (activeRoom == null)
                        activeRoom = room;
                }
            }

            Room containingRoom = FindRoomContainingPosition(savedPosition);
            var transitionManager = Core.RoomTransitionManager.Instance;
            Room transitionRoom = transitionManager != null ? transitionManager.GetCurrentRoom() : null;

            string wantedGrid = data != null && data.hasCurrentRoom
                ? $"({data.currentRoomGridX},{data.currentRoomGridY})"
                : "none";
            string activeGrid = activeRoom != null ? $"({activeRoom.gridPosition.x},{activeRoom.gridPosition.y})" : "none";
            string transitionGrid = transitionRoom != null ? $"({transitionRoom.gridPosition.x},{transitionRoom.gridPosition.y})" : "none";
            string containingGrid = containingRoom != null ? $"({containingRoom.gridPosition.x},{containingRoom.gridPosition.y})" : "none";

            Debug.Log(
                $"[ContinueDebug] {phase} | wanted={wantedGrid} active={activeGrid} transition={transitionGrid} containsSavedPos={containingGrid} activeCount={activeCount} savedPos={savedPosition}");
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

        /// <summary>
        /// Đặt Light Fragments trực tiếp (dùng khi load save)
        /// </summary>
        public void SetLightFragments(int count)
        {
            lightFragmentsCollected = count;
            OnLightFragmentCollected?.Invoke(lightFragmentsCollected, totalLightFragments);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
