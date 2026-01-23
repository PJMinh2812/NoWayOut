using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class MicroStudioImportConfigurator
{
    private const int DefaultPixelsPerUnit = 16;

    [MenuItem("Tools/MicroStudio/Configure Imports")]
    public static void ConfigureImports()
    {
        var jsonPath = FindProjectJsonPath();
        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            EditorUtility.DisplayDialog(
                "MicroStudio Import",
                "Couldn't find Assets/MicroStudio/project.json.\n\nCopy your repo's project.json into Unity at:\nAssets/MicroStudio/project.json",
                "OK"
            );
            return;
        }

        JObject root;
        try
        {
            root = JObject.Parse(File.ReadAllText(jsonPath));
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("MicroStudio Import", "Failed to parse project.json:\n" + ex.Message, "OK");
            return;
        }

        var filesToken = root["files"] as JObject;
        if (filesToken == null)
        {
            EditorUtility.DisplayDialog("MicroStudio Import", "project.json has no 'files' object.", "OK");
            return;
        }

        try
        {
            ConfigureSprites(filesToken);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("MicroStudio Import", "Import configuration complete.", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("MicroStudio Import", "Error while configuring imports:\n" + ex.Message, "OK");
        }
    }

    private static void ConfigureSprites(JObject files)
    {
        var entries = files.Properties()
            .Where(p => p.Name.StartsWith("sprites/", StringComparison.OrdinalIgnoreCase))
            .Where(p => p.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var configured = 0;

        foreach (var prop in entries)
        {
            var microKey = prop.Name;
            var fileObj = prop.Value as JObject;
            var properties = fileObj?["properties"] as JObject;
            var frames = properties?["frames"]?.Value<int?>() ?? 1;
            // fps nếu cần: var fps = properties?["fps"]?.Value<int?>() ?? 0;

            var unityAssetPath = "Assets/Art/Sprites/" + microKey.Substring("sprites/".Length);
            var importer = AssetImporter.GetAtPath(unityAssetPath) as TextureImporter;
            if (importer == null) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = DefaultPixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;

            if (frames <= 1)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
                configured++;
                continue;
            }

            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(unityAssetPath);
            if (tex == null) continue;

            var frameWidth = tex.width / frames;
            var frameHeight = tex.height;
            if (frameWidth <= 0 || frameWidth * frames != tex.width) continue;

            var metas = new List<SpriteMetaData>(frames);
            for (var i = 0; i < frames; i++)
            {
                metas.Add(new SpriteMetaData
                {
                    name = Path.GetFileNameWithoutExtension(unityAssetPath) + "_" + i,
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight)
                });
            }

            // Use ISpriteEditorDataProvider instead of deprecated spritesheet property
            var dataProvider = GetSpriteEditorDataProvider(importer);
            if (dataProvider != null)
            {
                // Convert SpriteMetaData to SpriteRect
                var spriteRects = new List<SpriteRect>(frames);
                for (var i = 0; i < metas.Count; i++)
                {
                    var meta = metas[i];
                    var spriteRect = new SpriteRect
                    {
                        name = meta.name,
                        rect = meta.rect,
                        alignment = (SpriteAlignment)meta.alignment,
                        pivot = meta.pivot,
                        border = meta.border
                    };
                    spriteRects.Add(spriteRect);
                }
                
                dataProvider.SetSpriteRects(spriteRects.ToArray());
                dataProvider.Apply();
            }
            else
            {
                // Fallback for older Unity versions
                #pragma warning disable CS0618 // Type or member is obsolete
                importer.spritesheet = metas.ToArray();
                #pragma warning restore CS0618
            }
            
            importer.SaveAndReimport();
            configured++;
        }

        Debug.Log($"[MicroStudio] Configured {configured} sprite import(s). Check that sprites/ is under Assets/Art/Sprites/.");
    }

    private static string FindProjectJsonPath()
    {
        var candidates = AssetDatabase.FindAssets("project t:TextAsset")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.Equals("Assets/MicroStudio/project.json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return candidates.Count > 0 ? candidates[0] : null;
    }

    private static ISpriteEditorDataProvider GetSpriteEditorDataProvider(TextureImporter importer)
    {
        try
        {
            // Try to get ISpriteEditorDataProvider using SpriteDataProviderFactories
            var factoryType = typeof(SpriteDataProviderFactories);
            var factories = Activator.CreateInstance(factoryType);
            var initMethod = factoryType.GetMethod("Init");
            initMethod?.Invoke(factories, null);
            
            var getProviderMethod = factoryType.GetMethod("GetSpriteEditorDataProviderFromObject");
            if (getProviderMethod != null)
            {
                var provider = getProviderMethod.Invoke(factories, new object[] { importer }) as ISpriteEditorDataProvider;
                return provider;
            }
        }
        catch
        {
            // Fallback: return null to use deprecated API
        }
        return null;
    }
}