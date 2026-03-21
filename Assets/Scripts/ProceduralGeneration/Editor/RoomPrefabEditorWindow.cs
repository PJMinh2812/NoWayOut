using UnityEngine;
using UnityEditor;
using System.IO;
using ProceduralGeneration.Data;
using ProceduralGeneration.Components;
using ProceduralGeneration.Core;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering;

namespace ProceduralGeneration.Editor
{
    /// <summary>
    /// Editor Window để thiết kế room prefab thủ công rồi gắn vào RoomData.
    /// Workflow: Chọn RoomData → Spawn template → Vẽ decor → Save & Assign.
    /// </summary>
    public class RoomPrefabEditorWindow : EditorWindow
    {
        private RoomData targetRoomData;
        private GameObject spawnedTemplate;
        private string saveFolderPath = "Assets/Data/Prefabs/Rooms";
        private string prefabName = "Room_Custom";
        private Vector2Int roomSizeOverride;
        private Vector2 scrollPosition;

        private GUIStyle headerStyle;
        private bool stylesInitialized;

        [MenuItem("Tools/Procedural Generation/Room Prefab Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<RoomPrefabEditorWindow>("Room Prefab Editor");
            window.minSize = new Vector2(380, 580);
            window.Show();
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(8);
            DrawRoomDataSection();
            EditorGUILayout.Space(8);
            DrawTemplateSection();
            EditorGUILayout.Space(8);
            DrawSaveSection();
            EditorGUILayout.Space(8);
            DrawCurrentPrefabSection();
            EditorGUILayout.Space(8);
            DrawHelpSection();

            EditorGUILayout.EndScrollView();
        }

        #region UI Sections

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("ROOM PREFAB EDITOR", headerStyle);
            GUILayout.Label("Vẽ room thủ công → Save prefab → Gắn vào tool", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawRoomDataSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("ROOM DATA", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            targetRoomData = (RoomData)EditorGUILayout.ObjectField("Room Data", targetRoomData, typeof(RoomData), false);
            if (EditorGUI.EndChangeCheck() && targetRoomData != null)
            {
                prefabName = $"Room_{targetRoomData.roomType}_Custom";
                roomSizeOverride = targetRoomData.size;
            }

            if (targetRoomData != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Type", targetRoomData.roomType.ToString());
                EditorGUILayout.LabelField("Base Size", $"{targetRoomData.size.x} x {targetRoomData.size.y} tiles");
                EditorGUILayout.LabelField("Door Anchors", targetRoomData.doorAnchors?.Count.ToString() ?? "0");
                EditorGUILayout.LabelField("Current Prefab", targetRoomData.roomPrefab != null ? targetRoomData.roomPrefab.name : "(None)");
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("Chọn một RoomData ScriptableObject để bắt đầu.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawTemplateSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("TEMPLATE", EditorStyles.boldLabel);

            roomSizeOverride = EditorGUILayout.Vector2IntField("Size Override (tiles)", roomSizeOverride);
            EditorGUILayout.HelpBox("Gizmos hiển thị bounds và door positions trong Scene view.", MessageType.None);
            EditorGUILayout.Space(4);

            EditorGUI.BeginDisabledGroup(targetRoomData == null);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Spawn Template in Scene", GUILayout.Height(32)))
                SpawnTemplate();
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);

            if (spawnedTemplate != null)
            {
                EditorGUILayout.LabelField("Spawned:", spawnedTemplate.name);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select in Scene", GUILayout.Height(25)))
                {
                    Selection.activeGameObject = spawnedTemplate;
                    SceneView.lastActiveSceneView?.FrameSelected();
                }
                GUI.backgroundColor = new Color(1f, 0.55f, 0.3f);
                if (GUILayout.Button("Clear Template", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Clear Template",
                        $"Xóa '{spawnedTemplate.name}' khỏi scene?", "Yes", "No"))
                    {
                        DestroyImmediate(spawnedTemplate);
                        spawnedTemplate = null;
                    }
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Chưa có template. Bấm Spawn để tạo hoặc chọn GO trong scene.", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSaveSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("SAVE AS PREFAB", EditorStyles.boldLabel);

            // Source: ưu tiên spawnedTemplate, fallback Selection
            GameObject saveSource = spawnedTemplate != null ? spawnedTemplate : Selection.activeGameObject;
            if (saveSource != null)
                EditorGUILayout.LabelField("Source:", saveSource.name);
            else
                EditorGUILayout.HelpBox("Không có GO nào. Spawn template hoặc chọn GO trong scene.", MessageType.Warning);

            EditorGUILayout.Space(4);
            saveFolderPath = EditorGUILayout.TextField("Save Folder", saveFolderPath);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse", GUILayout.Width(70), GUILayout.Height(22)))
            {
                string path = EditorUtility.OpenFolderPanel("Chọn thư mục lưu prefab", "Assets", "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                    saveFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
            }
            if (GUILayout.Button("Create Folder", GUILayout.Height(22)))
            {
                if (!Directory.Exists(saveFolderPath))
                {
                    Directory.CreateDirectory(saveFolderPath);
                    AssetDatabase.Refresh();
                }
            }
            EditorGUILayout.EndHorizontal();

            prefabName = EditorGUILayout.TextField("Prefab Name", prefabName);

            bool canSave = targetRoomData != null && saveSource != null && !string.IsNullOrEmpty(prefabName);
            EditorGUI.BeginDisabledGroup(!canSave);
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Save & Assign to RoomData", GUILayout.Height(32)))
                SaveAndAssign(saveSource);
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void DrawCurrentPrefabSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("CURRENT PREFAB", EditorStyles.boldLabel);

            if (targetRoomData != null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("roomPrefab", targetRoomData.roomPrefab, typeof(GameObject), false);
                EditorGUI.EndDisabledGroup();

                if (targetRoomData.roomPrefab != null)
                {
                    GUI.backgroundColor = new Color(1f, 0.55f, 0.3f);
                    if (GUILayout.Button("Clear roomPrefab", GUILayout.Height(25)))
                    {
                        if (EditorUtility.DisplayDialog("Clear Prefab",
                            $"Xóa prefab '{targetRoomData.roomPrefab.name}' khỏi {targetRoomData.name}?", "Yes", "No"))
                            SetRoomPrefab(null);
                    }
                    GUI.backgroundColor = Color.white;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Chưa chọn RoomData.", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawHelpSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("HƯỚNG DẪN", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Workflow:\n" +
                "1. Chọn RoomData  →  2. Spawn Template  →  3. Vẽ decor trong child 'Decor'  →  4. Save & Assign\n\n" +
                "Quy tắc đặt tên child GO:\n" +
                "  ✓ 'Decor', 'Props', 'Obstacles' → tồn tại khi runtime\n" +
                "  ✗ 'Background', 'Walls', 'Doors' → bị xóa tự động khi runtime!\n\n" +
                "Sorting Order gợi ý:\n" +
                "  Floor (auto-gen) = -10  |  Walls (auto-gen) = 0  |  Decor của bạn ≥ 1",
                MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Operations

        private void SpawnTemplate()
        {
            if (targetRoomData == null) return;

            if (spawnedTemplate != null)
            {
                DestroyImmediate(spawnedTemplate);
                spawnedTemplate = null;
            }

            Vector2Int size = (roomSizeOverride.x > 0 && roomSizeOverride.y > 0)
                ? roomSizeOverride
                : targetRoomData.size;

            // Root GO
            var root = new GameObject($"Room_{targetRoomData.roomType}_Template");

            // Gizmos marker (editor only, stripped on save)
            var marker = root.AddComponent<RoomTemplateMarker>();
            marker.roomData = targetRoomData;
            marker.roomSize = size;

            // RoomVisualGenerator - needed at runtime in prefab
            var visualGenerator = root.AddComponent<RoomVisualGenerator>();

            // Tạo room data giả để preview giống map tool (có floor/walls/doors placeholder)
            var previewRoom = new Room(targetRoomData, Vector2Int.zero, size);
            visualGenerator.SetCurrentRoom(previewRoom);

            // Cho phép hiện cửa ở tất cả door anchors để bạn căn decor dễ hơn
            var previewConnections = new System.Collections.Generic.Dictionary<DoorDirection, Room>();
            if (targetRoomData.doorAnchors != null)
            {
                foreach (var anchor in targetRoomData.doorAnchors)
                {
                    if (!previewConnections.ContainsKey(anchor.direction))
                        previewConnections.Add(anchor.direction, previewRoom);
                }
            }

            visualGenerator.GenerateVisuals(targetRoomData, previewConnections);
            visualGenerator.ForceSpriteDefaultMaterial();
            ApplySafePreviewMaterials(root);

            // Empty Decor container - user đặt decor vào đây
            var decor = new GameObject("Decor");
            decor.transform.SetParent(root.transform);
            decor.transform.localPosition = Vector3.zero;

            spawnedTemplate = root;

            Selection.activeGameObject = root;
            SceneView.lastActiveSceneView?.FrameSelected();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);

            Debug.Log($"[RoomPrefabEditor] Spawned template: {root.name} ({size.x}x{size.y} tiles)");
        }

        private void ApplySafePreviewMaterials(GameObject root)
        {
            if (root == null) return;

            Material spriteDefault = ResolveSafeSpriteMaterial();
            if (spriteDefault == null)
            {
                Debug.LogWarning("[RoomPrefabEditor] Không tìm được material sprite phù hợp, dùng material mặc định của renderer.");
                return;
            }

            // Apply cho ALL TilemapRenderer (Floor, Walls, etc.)
            foreach (var tilemapRenderer in root.GetComponentsInChildren<TilemapRenderer>(true))
            {
                tilemapRenderer.sharedMaterial = spriteDefault;
                Debug.Log($"[RoomPrefabEditor] Fixed tilemap material: {tilemapRenderer.gameObject.name}");
            }

            // Apply cho ALL SpriteRenderer
            foreach (var spriteRenderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (spriteRenderer.sharedMaterial == null || 
                    spriteRenderer.sharedMaterial.name.Contains("Lit") ||
                    spriteRenderer.sharedMaterial.name.Contains("Default-Material"))
                {
                    spriteRenderer.sharedMaterial = spriteDefault;
                    Debug.Log($"[RoomPrefabEditor] Fixed sprite material: {spriteRenderer.gameObject.name}");
                }
            }
        }

        private void SaveAndAssign(GameObject source)
        {
            if (targetRoomData == null || source == null) return;

            if (!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
                AssetDatabase.Refresh();
            }

            string cleanName = string.IsNullOrWhiteSpace(prefabName)
                ? $"Room_{targetRoomData.roomType}_Custom"
                : prefabName;
            if (!cleanName.EndsWith(".prefab")) cleanName += ".prefab";
            string prefabPath = Path.Combine(saveFolderPath, cleanName).Replace('\\', '/');

            if (File.Exists(prefabPath))
            {
                bool overwrite = EditorUtility.DisplayDialog("File đã tồn tại",
                    $"'{cleanName}' đã tồn tại.\nGhi đè?", "Ghi đè", "Hủy");
                if (!overwrite) return;
            }

            // Tạo copy để save, giữ nguyên original trong scene
            GameObject copy = Instantiate(source);
            copy.name = source.name.Replace("_Template", "");

            try
            {
                // Xóa marker khỏi copy - không cần trong prefab
                var markerOnCopy = copy.GetComponent<RoomTemplateMarker>();
                if (markerOnCopy != null) DestroyImmediate(markerOnCopy);

                // FIX MATERIAL trước khi save để ensure material được persist vào prefab
                ApplySafePreviewMaterials(copy);

                // Tạo material asset (.mat) để persist material vào prefab
                string materialName = $"{cleanName.Replace(".prefab", "")}_SpriteMaterial.mat";
                string materialPath = Path.Combine(saveFolderPath, materialName).Replace('\\', '/');
                CreateAndAssignSpriteMaterial(copy, materialPath);

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(copy, prefabPath);
                DestroyImmediate(copy);

                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                SetRoomPrefab(savedPrefab);

                EditorGUIUtility.PingObject(savedPrefab);
                EditorUtility.DisplayDialog("Thành công",
                    $"Đã lưu và gắn vào {targetRoomData.name}!\n\nPath: {prefabPath}", "OK");

                Debug.Log($"[RoomPrefabEditor] Saved: {prefabPath}  →  Assigned to {targetRoomData.name}");
            }
            catch (System.Exception e)
            {
                if (copy != null) DestroyImmediate(copy);
                EditorUtility.DisplayDialog("Lỗi", $"Không thể lưu prefab:\n{e.Message}", "OK");
                Debug.LogError($"[RoomPrefabEditor] Save failed: {e}");
            }
        }

        private void SetRoomPrefab(GameObject prefab)
        {
            if (targetRoomData == null) return;
            var so = new SerializedObject(targetRoomData);
            so.FindProperty("roomPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Tạo material asset (.mat) và assign cho toàn bộ TilemapRenderer/SpriteRenderer trong GameObject
        /// </summary>
        private void CreateAndAssignSpriteMaterial(GameObject root, string materialPath)
        {
            try
            {
                // Lấy material sprite an toàn theo render pipeline hiện tại.
                Material spriteDefault = ResolveSafeSpriteMaterial();
                
                if (spriteDefault != null)
                {
                    // Assign material cho toàn bộ renderers
                    foreach (var renderer in root.GetComponentsInChildren<TilemapRenderer>(true))
                    {
                        renderer.sharedMaterial = spriteDefault;
                    }
                    foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (renderer.sharedMaterial == null || 
                            renderer.sharedMaterial.name.Contains("Lit") ||
                            renderer.sharedMaterial.name.Contains("Default-Material"))
                        {
                            renderer.sharedMaterial = spriteDefault;
                        }
                    }
                    
                    Debug.Log($"[RoomPrefabEditor] Assigned Sprites-Default material to {root.name}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RoomPrefabEditor] Could not create material asset: {e.Message}");
            }
        }

        private Material ResolveSafeSpriteMaterial()
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                Material urpSpriteMat = AssetDatabase.LoadAssetAtPath<Material>(
                    "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
                if (urpSpriteMat != null)
                    return urpSpriteMat;
            }

            return AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        }

        #endregion
    }
}
