using UnityEngine;
using UnityEditor;
using ProceduralGeneration.Core;
using System.IO;

namespace ProceduralGeneration.Editor
{
    /// <summary>
    /// Editor Window cho Dungeon Generation tools
    /// Cung cấp UI để generate, save, và load dungeons
    /// </summary>
    public class DungeonGeneratorWindow : EditorWindow
    {
        private DungeonManager dungeonManager;
        private int customSeed = 0;
        private bool useCustomSeed = false;
        private string saveFolderPath = "Assets/SavedMaps";
        private Vector2 scrollPosition;
        
        // Style
        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;
        private bool stylesInitialized = false;
        
        [MenuItem("Tools/Procedural Generation/Dungeon Generator")]
        public static void ShowWindow()
        {
            DungeonGeneratorWindow window = GetWindow<DungeonGeneratorWindow>("Dungeon Generator");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            // Tìm DungeonManager trong scene
            FindDungeonManager();
        }
        
        private void InitializeStyles()
        {
            if (stylesInitialized) return;
            
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 35
            };
            
            stylesInitialized = true;
        }
        
        private void OnGUI()
        {
            InitializeStyles();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Header
            DrawHeader();
            
            EditorGUILayout.Space(10);
            
            // Manager Section
            DrawManagerSection();
            
            EditorGUILayout.Space(10);
            
            // Seed Section
            DrawSeedSection();
            
            EditorGUILayout.Space(10);
            
            // Generation Section
            DrawGenerationSection();
            
            EditorGUILayout.Space(10);
            
            // Save/Load Section
            DrawSaveLoadSection();
            
            EditorGUILayout.Space(10);
            
            // Info Section
            DrawInfoSection();
            
            EditorGUILayout.EndScrollView();
        }
        
        #region UI Sections
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("🎲 PROCEDURAL DUNGEON GENERATOR", headerStyle);
            GUILayout.Label("Generate, Save, and Manage Dungeons", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawManagerSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("🎮 DUNGEON MANAGER", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            dungeonManager = (DungeonManager)EditorGUILayout.ObjectField(
                "Dungeon Manager", 
                dungeonManager, 
                typeof(DungeonManager), 
                true);
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString("DungeonGenerator_LastManager", 
                    dungeonManager != null ? AssetDatabase.GetAssetPath(dungeonManager) : "");
            }
            
            EditorGUILayout.Space(5);
            
            if (dungeonManager == null)
            {
                EditorGUILayout.HelpBox(
                    "No DungeonManager found! Please assign one or click Create below.", 
                    MessageType.Warning);
                
                if (GUILayout.Button("🔍 Find in Scene", GUILayout.Height(25)))
                {
                    FindDungeonManager();
                }
                
                if (GUILayout.Button("➕ Create New Manager", GUILayout.Height(25)))
                {
                    CreateDungeonManager();
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"Manager: {dungeonManager.name}", MessageType.Info);
                
                if (GUILayout.Button("⚙️ Select Manager", GUILayout.Height(25)))
                {
                    Selection.activeGameObject = dungeonManager.gameObject;
                    EditorGUIUtility.PingObject(dungeonManager.gameObject);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSeedSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("🌱 SEED CONFIGURATION", EditorStyles.boldLabel);
            
            useCustomSeed = EditorGUILayout.Toggle("Use Custom Seed", useCustomSeed);
            
            if (useCustomSeed)
            {
                customSeed = EditorGUILayout.IntField("Seed Value", customSeed);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🎲 Random Seed", GUILayout.Height(25)))
                {
                    customSeed = Random.Range(1, 999999);
                }
                if (GUILayout.Button("📋 Copy Current", GUILayout.Height(25)))
                {
                    if (dungeonManager != null)
                    {
                        customSeed = dungeonManager.GetCurrentSeed();
                        EditorUtility.DisplayDialog("Seed Copied", 
                            $"Current seed ({customSeed}) copied!", "OK");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Will use random seed on generation", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawGenerationSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("⚡ GENERATION", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(dungeonManager == null);
            
            // Generate Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🏗️ GENERATE DUNGEON", buttonStyle))
            {
                GenerateDungeon();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(5);
            
            // Clear Button
            GUI.backgroundColor = new Color(1f, 0.5f, 0f);
            if (GUILayout.Button("🗑️ CLEAR DUNGEON", buttonStyle))
            {
                ClearDungeon();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSaveLoadSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("💾 SAVE & EXPORT", EditorStyles.boldLabel);
            
            saveFolderPath = EditorGUILayout.TextField("Save Folder", saveFolderPath);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📁 Browse", GUILayout.Height(25)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Save Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    // Convert to relative path
                    if (path.StartsWith(Application.dataPath))
                    {
                        saveFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
            }
            if (GUILayout.Button("📂 Create Folder", GUILayout.Height(25)))
            {
                CreateSaveFolder();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            EditorGUI.BeginDisabledGroup(dungeonManager == null || 
                dungeonManager.transform.childCount == 0);
            
            // Save as Prefab Button
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("💾 SAVE MAP AS PREFAB", buttonStyle))
            {
                SaveMapAsPrefab();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUI.EndDisabledGroup();
            
            if (Directory.Exists(saveFolderPath))
            {
                EditorGUILayout.HelpBox($"Save location: {saveFolderPath}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Save folder doesn't exist! Create it first.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawInfoSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("ℹ️ INFORMATION", EditorStyles.boldLabel);
            
            if (dungeonManager != null)
            {
                EditorGUILayout.LabelField("Current Seed:", dungeonManager.GetCurrentSeed().ToString());
                
                if (dungeonManager.dungeonContainer != null)
                {
                    int roomCount = dungeonManager.dungeonContainer.childCount;
                    EditorGUILayout.LabelField("Room Count:", roomCount.ToString());
                }
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Flow:", "Start → Arch1 → MidBoss → Arch2 → Boss → Goal");
            }
            else
            {
                EditorGUILayout.HelpBox("No manager assigned", MessageType.None);
            }
            
            EditorGUILayout.EndVertical();
            
            // Footer
            EditorGUILayout.Space(10);
            GUILayout.Label("Made with ❤️ for Unity", EditorStyles.centeredGreyMiniLabel);
        }
        
        #endregion
        
        #region Operations
        
        private void FindDungeonManager()
        {
            dungeonManager = FindFirstObjectByType<DungeonManager>();
            
            if (dungeonManager != null)
            {
                Debug.Log($"Found DungeonManager: {dungeonManager.name}");
            }
        }
        
        private void CreateDungeonManager()
        {
            GameObject managerObj = new GameObject("DungeonManager");
            dungeonManager = managerObj.AddComponent<DungeonManager>();
            
            // Create container
            GameObject container = new GameObject("DungeonContainer");
            container.transform.SetParent(managerObj.transform);
            dungeonManager.dungeonContainer = container.transform;
            
            Selection.activeGameObject = managerObj;
            
            EditorUtility.DisplayDialog("Manager Created", 
                "DungeonManager has been created! Please configure room and trap databases.", "OK");
        }
        
        private void GenerateDungeon()
        {
            if (dungeonManager == null)
            {
                EditorUtility.DisplayDialog("Error", "No DungeonManager assigned!", "OK");
                return;
            }
            
            // Apply custom seed if enabled
            if (useCustomSeed)
            {
                dungeonManager.seed = customSeed;
                dungeonManager.useRandomSeed = false;
            }
            else
            {
                dungeonManager.useRandomSeed = true;
            }
            
            // Generate
            dungeonManager.GenerateDungeon();
            
            // Mark scene dirty
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    dungeonManager.gameObject.scene);
            }
            
            EditorUtility.DisplayDialog("Success", 
                $"Dungeon generated successfully!\nSeed: {dungeonManager.GetCurrentSeed()}", "OK");
        }
        
        private void ClearDungeon()
        {
            if (dungeonManager == null) return;
            
            if (EditorUtility.DisplayDialog("Confirm Clear", 
                "Are you sure you want to clear the current dungeon?", "Yes", "No"))
            {
                dungeonManager.ClearDungeon();
                
                if (!Application.isPlaying)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                        dungeonManager.gameObject.scene);
                }
                
                Debug.Log("Dungeon cleared");
            }
        }
        
        private void SaveMapAsPrefab()
        {
            if (dungeonManager == null || dungeonManager.dungeonContainer == null)
            {
                EditorUtility.DisplayDialog("Error", "No dungeon to save!", "OK");
                return;
            }
            
            // Ensure folder exists
            if (!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
                AssetDatabase.Refresh();
            }
            
            // Generate unique name
            int seed = dungeonManager.GetCurrentSeed();
            string prefabName = $"DungeonMap_Seed_{seed}_{System.DateTime.Now:yyyyMMdd_HHmmss}.prefab";
            string prefabPath = Path.Combine(saveFolderPath, prefabName);
            
            // Create prefab
            GameObject dungeonCopy = Instantiate(dungeonManager.dungeonContainer.gameObject);
            dungeonCopy.name = $"DungeonMap_Seed_{seed}";
            
            try
            {
                PrefabUtility.SaveAsPrefabAsset(dungeonCopy, prefabPath);
                DestroyImmediate(dungeonCopy);
                
                AssetDatabase.Refresh();
                
                // Ping the created prefab
                Object prefab = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
                EditorGUIUtility.PingObject(prefab);
                
                EditorUtility.DisplayDialog("Success", 
                    $"Map saved as prefab!\n\nLocation: {prefabPath}\nSeed: {seed}", "OK");
                
                Debug.Log($"<color=green>Map saved successfully!</color> Path: {prefabPath}");
            }
            catch (System.Exception e)
            {
                DestroyImmediate(dungeonCopy);
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to save prefab:\n{e.Message}", "OK");
                Debug.LogError($"Failed to save prefab: {e}");
            }
        }
        
        private void CreateSaveFolder()
        {
            if (!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("Folder Created", 
                    $"Folder created at:\n{saveFolderPath}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Info", 
                    "Folder already exists!", "OK");
            }
        }
        
        #endregion
    }
}
