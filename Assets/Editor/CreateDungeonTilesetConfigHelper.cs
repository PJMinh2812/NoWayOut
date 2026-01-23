using UnityEngine;
using UnityEditor;
using GloomCraft.Dungeon;

public static class CreateDungeonTilesetConfigHelper
{
    [MenuItem("Tools/Dungeon/Create DungeonTilesetConfig Asset")]
    public static void CreateDungeonTilesetConfig()
    {
        var config = ScriptableObject.CreateInstance<DungeonTilesetConfig>();
        
        // Đảm bảo folder tồn tại
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }
        
        var path = "Assets/Settings/DungeonTilesetConfig.asset";
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = config;
        
        Debug.Log($"[Dungeon] Created DungeonTilesetConfig at {path}");
    }
}

