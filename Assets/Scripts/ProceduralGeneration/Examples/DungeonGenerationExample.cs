using UnityEngine;
using ProceduralGeneration.Core;
using ProceduralGeneration.Data;

namespace ProceduralGeneration.Examples
{
    /// <summary>
    /// Example script demonstrating how to use the Dungeon Generation system
    /// Ví dụ về cách tích hợp hệ thống vào game của bạn
    /// </summary>
    public class DungeonGenerationExample : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DungeonManager dungeonManager;
        
        [Header("Game Settings")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private GameObject playerPrefab;
        
        [Header("Level Progression")]
        [SerializeField] private int baseRoomCount = 5;
        [SerializeField] private int roomsPerLevel = 2;
        
        private GameObject currentPlayer;
        
        #region Unity Callbacks
        
        private void Start()
        {
            // Example 1: Generate dungeon at start
            GenerateLevelForCurrentStage();
        }
        
        private void Update()
        {
            // Example: Regenerate với phím R
            if (Input.GetKeyDown(KeyCode.R))
            {
                RegenerateDungeon();
            }
            
            // Example: Next level với phím N
            if (Input.GetKeyDown(KeyCode.N))
            {
                LoadNextLevel();
            }
            
            // Example: Load specific seed với phím L
            if (Input.GetKeyDown(KeyCode.L))
            {
                LoadDungeonFromSeed(12345);
            }
        }
        
        #endregion
        
        #region Example: Basic Generation
        
        /// <summary>
        /// EXAMPLE 1: Generate dungeon cơ bản
        /// </summary>
        public void GenerateBasicDungeon()
        {
            if (dungeonManager == null)
            {
                Debug.LogError("DungeonManager not assigned!");
                return;
            }
            
            // Enable random seed
            dungeonManager.useRandomSeed = true;
            
            // Set room counts
            dungeonManager.archetype1RoomCount = 5;
            dungeonManager.archetype2RoomCount = 5;
            
            // Generate
            dungeonManager.GenerateDungeon();
            
            Debug.Log($"Dungeon generated with seed: {dungeonManager.GetCurrentSeed()}");
        }
        
        #endregion
        
        #region Example: Level Progression
        
        /// <summary>
        /// EXAMPLE 2: Generate dungeon với difficulty scaling theo level
        /// </summary>
        public void GenerateLevelForCurrentStage()
        {
            if (dungeonManager == null) return;
            
            // Calculate room counts dựa trên level
            int roomCount1 = baseRoomCount + (currentLevel * roomsPerLevel);
            int roomCount2 = baseRoomCount + (currentLevel * roomsPerLevel);
            
            // Clamp values
            roomCount1 = Mathf.Clamp(roomCount1, 3, 15);
            roomCount2 = Mathf.Clamp(roomCount2, 3, 15);
            
            // Configure manager
            dungeonManager.archetype1RoomCount = roomCount1;
            dungeonManager.archetype2RoomCount = roomCount2;
            dungeonManager.branchProbability = 0.1f + (currentLevel * 0.05f);
            
            // Generate
            dungeonManager.useRandomSeed = true;
            dungeonManager.GenerateDungeon();
            
            // Post-generation
            OnDungeonGeneratedForLevel();
            
            Debug.Log($"Level {currentLevel} generated: {roomCount1 + roomCount2 + 4} total rooms");
        }
        
        /// <summary>
        /// Callback sau khi dungeon được generate
        /// </summary>
        private void OnDungeonGeneratedForLevel()
        {
            // Spawn player tại start room
            SpawnPlayerAtStart();
            
            // Setup camera
            SetupCamera();
            
            // Enable UI
            EnableGameplayUI();
            
            // Save seed for this level
            SaveLevelSeed(dungeonManager.GetCurrentSeed());
        }
        
        #endregion
        
        #region Example: Seed Management
        
        /// <summary>
        /// EXAMPLE 3: Load dungeon từ seed cụ thể
        /// Hữu ích cho daily dungeons, shared challenges, debug, etc.
        /// </summary>
        public void LoadDungeonFromSeed(int seed)
        {
            if (dungeonManager == null) return;
            
            // Clear old dungeon
            dungeonManager.ClearDungeon();
            
            // Set seed
            dungeonManager.seed = seed;
            dungeonManager.useRandomSeed = false;
            
            // Generate
            dungeonManager.GenerateDungeon();
            
            Debug.Log($"Loaded dungeon from seed: {seed}");
        }
        
        /// <summary>
        /// EXAMPLE 4: Daily dungeon system
        /// Sử dụng date làm seed để tạo dungeon duy nhất mỗi ngày
        /// </summary>
        public void GenerateDailyDungeon()
        {
            // Tạo seed từ current date
            System.DateTime today = System.DateTime.Today;
            int dailySeed = today.Year * 10000 + today.Month * 100 + today.Day;
            
            // Load dungeon với seed này
            LoadDungeonFromSeed(dailySeed);
            
            Debug.Log($"Daily dungeon generated for {today:yyyy-MM-dd}");
        }
        
        /// <summary>
        /// EXAMPLE 5: Lưu seed của level hiện tại
        /// </summary>
        private void SaveLevelSeed(int seed)
        {
            PlayerPrefs.SetInt($"Level_{currentLevel}_Seed", seed);
            PlayerPrefs.Save();
            
            Debug.Log($"Saved level {currentLevel} seed: {seed}");
        }
        
        /// <summary>
        /// EXAMPLE 6: Load lại level với seed đã lưu
        /// </summary>
        public void ReplayCurrentLevel()
        {
            int savedSeed = PlayerPrefs.GetInt($"Level_{currentLevel}_Seed", 0);
            
            if (savedSeed != 0)
            {
                LoadDungeonFromSeed(savedSeed);
                Debug.Log($"Replaying level {currentLevel}");
            }
            else
            {
                Debug.LogWarning("No saved seed for current level");
                GenerateLevelForCurrentStage();
            }
        }
        
        #endregion
        
        #region Example: Runtime Operations
        
        /// <summary>
        /// EXAMPLE 7: Regenerate dungeon (ví dụ: restart level)
        /// </summary>
        public void RegenerateDungeon()
        {
            // Cleanup
            CleanupCurrentLevel();
            
            // Clear dungeon
            dungeonManager.ClearDungeon();
            
            // Generate new
            GenerateLevelForCurrentStage();
        }
        
        /// <summary>
        /// EXAMPLE 8: Load next level
        /// </summary>
        public void LoadNextLevel()
        {
            currentLevel++;
            RegenerateDungeon();
        }
        
        /// <summary>
        /// EXAMPLE 9: Cleanup level
        /// </summary>
        private void CleanupCurrentLevel()
        {
            // Destroy player
            if (currentPlayer != null)
            {
                Destroy(currentPlayer);
            }
            
            // Clear enemies, items, etc.
            // Your cleanup code here
        }
        
        #endregion
        
        #region Example: Player & Camera Setup
        
        /// <summary>
        /// EXAMPLE 10: Spawn player tại start room
        /// </summary>
        private void SpawnPlayerAtStart()
        {
            if (playerPrefab == null)
            {
                Debug.LogWarning("Player prefab not assigned");
                return;
            }
            
            // Tìm start room (phòng đầu tiên trong dungeon container)
            if (dungeonManager.dungeonContainer == null) return;
            
            Transform startRoom = FindStartRoom();
            
            if (startRoom != null)
            {
                // Spawn player tại center của start room
                Vector3 spawnPosition = startRoom.position;
                currentPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
                
                Debug.Log($"Player spawned at: {spawnPosition}");
            }
            else
            {
                Debug.LogError("Could not find start room!");
            }
        }
        
        /// <summary>
        /// Tìm start room trong dungeon
        /// </summary>
        private Transform FindStartRoom()
        {
            // Start room thường là phòng đầu tiên
            if (dungeonManager.dungeonContainer.childCount > 0)
            {
                return dungeonManager.dungeonContainer.GetChild(0);
            }
            
            return null;
        }
        
        /// <summary>
        /// Setup camera follow player
        /// </summary>
        private void SetupCamera()
        {
            if (currentPlayer != null)
            {
                // Setup camera to follow player
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    // Add camera follow script or set position
                    mainCamera.transform.position = new Vector3(
                        currentPlayer.transform.position.x,
                        currentPlayer.transform.position.y,
                        -10f
                    );
                }
            }
        }
        
        #endregion
        
        #region Example: UI Integration
        
        /// <summary>
        /// Enable gameplay UI
        /// </summary>
        private void EnableGameplayUI()
        {
            // Show HUD, minimap, etc.
            // Your UI code here
            
            Debug.Log("Gameplay UI enabled");
        }
        
        /// <summary>
        /// EXAMPLE 11: Display dungeon info trong UI
        /// </summary>
        public string GetDungeonInfoText()
        {
            if (dungeonManager == null) return "No dungeon";
            
            int roomCount = dungeonManager.dungeonContainer != null ? 
                dungeonManager.dungeonContainer.childCount : 0;
            
            return $"Level {currentLevel}\n" +
                   $"Seed: {dungeonManager.GetCurrentSeed()}\n" +
                   $"Rooms: {roomCount}";
        }
        
        #endregion
        
        #region Example: Advanced Features
        
        /// <summary>
        /// EXAMPLE 12: Generate dungeon với custom parameters
        /// </summary>
        public void GenerateCustomDungeon(int seed, int roomCount1, int roomCount2, float branchProb)
        {
            dungeonManager.seed = seed;
            dungeonManager.useRandomSeed = false;
            dungeonManager.archetype1RoomCount = roomCount1;
            dungeonManager.archetype2RoomCount = roomCount2;
            dungeonManager.branchProbability = branchProb;
            
            dungeonManager.GenerateDungeon();
        }
        
        /// <summary>
        /// EXAMPLE 13: Challenge mode với custom difficulty
        /// </summary>
        public void GenerateChallengeDungeon()
        {
            // Maximum rooms
            dungeonManager.archetype1RoomCount = 10;
            dungeonManager.archetype2RoomCount = 10;
            
            // More branches
            dungeonManager.branchProbability = 0.4f;
            
            // Enable all traps
            dungeonManager.spawnTraps = true;
            
            // Generate
            dungeonManager.useRandomSeed = true;
            dungeonManager.GenerateDungeon();
            
            Debug.Log("Challenge dungeon generated!");
        }
        
        /// <summary>
        /// EXAMPLE 14: Tutorial/Easy mode
        /// </summary>
        public void GenerateTutorialDungeon()
        {
            // Minimum rooms
            dungeonManager.archetype1RoomCount = 3;
            dungeonManager.archetype2RoomCount = 3;
            
            // No branches
            dungeonManager.branchProbability = 0f;
            
            // Disable traps
            dungeonManager.spawnTraps = false;
            
            // Generate
            dungeonManager.useRandomSeed = true;
            dungeonManager.GenerateDungeon();
            
            Debug.Log("Tutorial dungeon generated!");
        }
        
        #endregion
        
        #region Context Menu (for testing in Editor)
        
        [ContextMenu("Test: Generate Basic")]
        private void Test_GenerateBasic()
        {
            GenerateBasicDungeon();
        }
        
        [ContextMenu("Test: Generate Level")]
        private void Test_GenerateLevel()
        {
            GenerateLevelForCurrentStage();
        }
        
        [ContextMenu("Test: Daily Dungeon")]
        private void Test_DailyDungeon()
        {
            GenerateDailyDungeon();
        }
        
        [ContextMenu("Test: Challenge Mode")]
        private void Test_ChallengeMode()
        {
            GenerateChallengeDungeon();
        }
        
        [ContextMenu("Test: Tutorial Mode")]
        private void Test_TutorialMode()
        {
            GenerateTutorialDungeon();
        }
        
        #endregion
    }
}
